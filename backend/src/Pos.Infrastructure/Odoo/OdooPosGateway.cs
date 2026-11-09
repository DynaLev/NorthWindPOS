using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pos.Infrastructure.Odoo;

public sealed class OdooPosGateway : IPosGateway
{
    private readonly OdooJson2Client _client;

    public OdooPosGateway(OdooJson2Client client)
    {
        _client = client;
    }

    public async Task<PosSessionInfo?> FindActiveSessionAsync(CancellationToken cancellationToken)
    {
        var result = await _client.CallAsync<JsonElement>(
            "pos.session",
            "search_read",
            new
            {
                domain = new object[] { new object[] { "state", "=", "opened" } },
                fields = new[] { "id", "name", "state", "config_id", "start_at", "payment_method_ids", "company_id" },
                limit = 10,
                order = "id desc"
            },
            cancellationToken);

        if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
            return null;

        var session = result[0];
        return await MapSessionAsync(session, cancellationToken);
    }

    public async Task<PosSessionInfo?> FindSessionAsync(long sessionId, CancellationToken cancellationToken)
    {
        var result = await _client.CallAsync<JsonElement>(
            "pos.session",
            "read",
            new
            {
                ids = new[] { sessionId },
                fields = new[] { "id", "name", "state", "config_id", "start_at", "payment_method_ids", "company_id" }
            },
            cancellationToken);

        if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
            return null;

        return await MapSessionAsync(result[0], cancellationToken);
    }

    private async Task<PosSessionInfo> MapSessionAsync(JsonElement session, CancellationToken cancellationToken)
    {
        var id = session.GetProperty("id").GetInt64();
        var name = session.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        var state = session.TryGetProperty("state", out var s) ? s.GetString() ?? "" : "";

        long configId = 0;
        string configName = "";
        if (session.TryGetProperty("config_id", out var cid) && cid.ValueKind == JsonValueKind.Array && cid.GetArrayLength() >= 2)
        {
            configId = cid[0].GetInt64();
            configName = cid[1].GetString() ?? "";
        }

        long companyId = 0;
        string currency = "";
        int decimalPlaces = 2;
        long? cashMethodId = null;
        string? cashMethodName = null;

        if (configId > 0)
        {
            try
            {
                var cfg = await _client.CallAsync<JsonElement>(
                    "pos.config",
                    "read",
                    new
                    {
                        ids = new[] { configId },
                        fields = new[] { "id", "name", "currency_id", "payment_method_ids" }
                    },
                    cancellationToken);
                if (cfg.ValueKind == JsonValueKind.Array && cfg.GetArrayLength() > 0)
                {
                    var c = cfg[0];
                    if (c.TryGetProperty("currency_id", out var cur) && cur.ValueKind == JsonValueKind.Array && cur.GetArrayLength() >= 2)
                    {
                        currency = cur[1].GetString() ?? "";
                    }

                    if (c.TryGetProperty("payment_method_ids", out var pms) && pms.ValueKind == JsonValueKind.Array)
                    {
                        var pmIds = pms.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.Number)
                            .Select(x => x.GetInt64())
                            .ToArray();
                        if (pmIds.Length > 0)
                        {
                            var methods = await _client.CallAsync<JsonElement>(
                                "pos.payment.method",
                                "read",
                                new { ids = pmIds, fields = new[] { "id", "name", "type" } },
                                cancellationToken);
                            if (methods.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var m in methods.EnumerateArray())
                                {
                                    if (m.TryGetProperty("type", out var t) &&
                                        string.Equals(t.GetString(), "cash", StringComparison.OrdinalIgnoreCase))
                                    {
                                        cashMethodId = m.GetProperty("id").GetInt64();
                                        cashMethodName = m.TryGetProperty("name", out var mn) ? mn.GetString() : null;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (PosIntegrationException)
            {
                // Se valida luego en Application
            }
        }

        if (session.TryGetProperty("company_id", out var comp) && comp.ValueKind == JsonValueKind.Array && comp.GetArrayLength() >= 1)
            companyId = comp[0].GetInt64();

        DateTimeOffset? openedAt = null;
        if (session.TryGetProperty("start_at", out var sa) && sa.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(sa.GetString(), out var parsed))
            openedAt = parsed;

        return new PosSessionInfo(id, name, configId, configName, state, companyId, currency, decimalPlaces,
            cashMethodId, cashMethodName, openedAt);
    }

    public async Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string? search, CancellationToken cancellationToken)
    {
        object[] domain = new object[]
        {
            new object[] { "sale_ok", "=", true },
            new object[] { "available_in_pos", "=", true }
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var orDomain = new object[]
            {
                new object[] { "name", "ilike", term },
                new object[] { "default_code", "ilike", term },
                new object[] { "barcode", "ilike", term }
            };
            // Odoo domain: ["sale_ok", "=", true], ["available_in_pos", "=", true], "|", "|", [...]
            // Build as AND with OR
            var full = new List<object> { domain[0], domain[1], "|", "|", orDomain[0], orDomain[1], orDomain[2] };
            domain = full.ToArray();
        }

        // product.product: el POS guarda el id de la variante, no el de la plantilla.
        var result = await _client.CallAsync<JsonElement>(
            "product.product",
            "search_read",
            new
            {
                domain,
                fields = new[] { "id", "name", "default_code", "barcode", "lst_price", "uom_id" },
                limit = 100
            },
            cancellationToken);

        var list = new List<ProductInfo>();
        if (result.ValueKind != JsonValueKind.Array) return list;

        foreach (var p in result.EnumerateArray())
        {
            var id = p.GetProperty("id").GetInt64();
            var name = ReadText(p, "name");
            var code = p.TryGetProperty("default_code", out var pc) && pc.ValueKind == JsonValueKind.String ? pc.GetString() : null;
            var barcode = p.TryGetProperty("barcode", out var pb) && pb.ValueKind == JsonValueKind.String ? pb.GetString() : null;
            var price = p.TryGetProperty("lst_price", out var pl) && pl.ValueKind == JsonValueKind.Number ? pl.GetDecimal() : 0m;
            var unit = "Unit";
            if (p.TryGetProperty("uom_id", out var uom) && uom.ValueKind == JsonValueKind.Array && uom.GetArrayLength() >= 2)
                unit = uom[1].GetString() ?? "Unit";

            list.Add(new ProductInfo(id, name, code, barcode, price, "", null, false, unit));
        }

        return list;
    }

    public async Task<IReadOnlyList<SaleProduct>> GetSaleProductsAsync(
        IReadOnlyCollection<long> productIds,
        PosSessionInfo session,
        CancellationToken cancellationToken)
    {
        var result = new List<SaleProduct>();
        var products = await _client.CallAsync<JsonElement>(
            "product.product",
            "read",
            new
            {
                ids = productIds.ToArray(),
                fields = new[] { "id", "name", "available_in_pos", "lst_price", "taxes_id", "uom_id", "product_tmpl_id" }
            },
            cancellationToken);

        if (products.ValueKind != JsonValueKind.Array) return result;

        foreach (var p in products.EnumerateArray())
        {
            var id = p.GetProperty("id").GetInt64();
            var name = ReadText(p, "name");
            var available = p.TryGetProperty("available_in_pos", out var av) && av.ValueKind == JsonValueKind.True;
            var listPrice = p.TryGetProperty("lst_price", out var lp) && lp.ValueKind == JsonValueKind.Number ? lp.GetDecimal() : 0m;
            long templateId = id;
            if (p.TryGetProperty("product_tmpl_id", out var tmpl) && tmpl.ValueKind == JsonValueKind.Array && tmpl.GetArrayLength() >= 1)
                templateId = tmpl[0].GetInt64();

            var taxIds = new List<long>();
            if (p.TryGetProperty("taxes_id", out var tx) && tx.ValueKind == JsonValueKind.Array)
                taxIds = tx.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt64()).ToList();

            var unit = "Unit";
            if (p.TryGetProperty("uom_id", out var uom) && uom.ValueKind == JsonValueKind.Array && uom.GetArrayLength() >= 2)
                unit = uom[1].GetString() ?? unit;

            decimal priceExcl = listPrice;
            decimal priceIncl = listPrice;
            if (session.ConfigId > 0 && available)
            {
                var info = await _client.CallAsync<JsonElement>(
                    "product.template",
                    "get_product_info_pos",
                    new
                    {
                        ids = new[] { templateId },
                        price = listPrice,
                        quantity = 1,
                        pos_config_id = session.ConfigId,
                        product_variant_id = id
                    },
                    cancellationToken);
                if (info.ValueKind == JsonValueKind.Array && info.GetArrayLength() > 0)
                    info = info[0];
                if (info.ValueKind == JsonValueKind.Object && info.TryGetProperty("all_prices", out var ap))
                {
                    if (ap.TryGetProperty("price_without_tax", out var pet) && pet.ValueKind == JsonValueKind.Number)
                        priceExcl = pet.GetDecimal();
                    if (ap.TryGetProperty("price_with_tax", out var pwt) && pwt.ValueKind == JsonValueKind.Number)
                        priceIncl = pwt.GetDecimal();
                }
            }

            result.Add(new SaleProduct(id, name, available, priceExcl, priceIncl, taxIds, null, false, unit));
        }

        return result;
    }

    public async Task<SaleInfo?> FindSaleByOperationIdAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var result = await _client.CallAsync<JsonElement>(
            "pos.order",
            "search_read",
            new
            {
                domain = new object[] { new object[] { "uuid", "=", operationId.ToString() } },
                fields = new[] { "id", "name", "uuid", "state", "date_order", "amount_total", "amount_tax", "amount_paid", "amount_return", "currency_id", "session_id", "config_id" },
                limit = 10,
                order = "id desc"
            },
            cancellationToken);

        if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0) return null;
        return await MapOrderAsync(result[0], cancellationToken);
    }

    public async Task<SaleInfo?> FindSaleByIdAsync(long orderId, CancellationToken cancellationToken)
    {
        var result = await _client.CallAsync<JsonElement>(
            "pos.order",
            "read",
            new
            {
                ids = new[] { orderId },
                fields = new[] { "id", "name", "uuid", "state", "date_order", "amount_total", "amount_tax", "amount_paid", "amount_return", "currency_id", "session_id", "config_id" }
            },
            cancellationToken);

        if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0) return null;
        return await MapOrderAsync(result[0], cancellationToken);
    }

    public async Task<IReadOnlyList<SaleInfo>> ListSalesAsync(int limit, CancellationToken cancellationToken)
    {
        var result = await _client.CallAsync<JsonElement>(
            "pos.order",
            "search_read",
            new
            {
                domain = Array.Empty<object>(),
                fields = new[] { "id", "name", "uuid", "state", "date_order", "amount_total", "amount_tax", "amount_paid", "amount_return", "currency_id", "session_id", "config_id" },
                limit = limit,
                order = "id desc"
            },
            cancellationToken);

        var list = new List<SaleInfo>();
        if (result.ValueKind != JsonValueKind.Array) return list;
        foreach (var o in result.EnumerateArray())
        {
            var mapped = await MapOrderAsync(o, cancellationToken);
            list.Add(mapped);
        }
        return list;
    }

    private async Task<SaleInfo> MapOrderAsync(JsonElement order, CancellationToken cancellationToken)
    {
        var id = order.GetProperty("id").GetInt64();
        var name = ReadText(order, "name");
        var uuidStr = ReadText(order, "uuid");
        var state = ReadText(order, "state");
        var total = order.TryGetProperty("amount_total", out var ot) && ot.ValueKind == JsonValueKind.Number ? ot.GetDecimal() : 0m;
        var tax = order.TryGetProperty("amount_tax", out var ota) && ota.ValueKind == JsonValueKind.Number ? ota.GetDecimal() : 0m;
        var paid = order.TryGetProperty("amount_paid", out var op) && op.ValueKind == JsonValueKind.Number ? op.GetDecimal() : 0m;
        var returned = order.TryGetProperty("amount_return", out var or2) && or2.ValueKind == JsonValueKind.Number ? or2.GetDecimal() : 0m;

        Guid opId = Guid.TryParse(uuidStr, out var g) ? g : Guid.Empty;
        string currency = "";
        if (order.TryGetProperty("currency_id", out var cur) && cur.ValueKind == JsonValueKind.Array && cur.GetArrayLength() >= 2)
            currency = ReadTextValue(cur[1]);

        long sessionId = 0;
        if (order.TryGetProperty("session_id", out var sid) && sid.ValueKind == JsonValueKind.Array && sid.GetArrayLength() >= 1)
            sessionId = sid[0].GetInt64();

        long configId = 0;
        if (order.TryGetProperty("config_id", out var cid) && cid.ValueKind == JsonValueKind.Array && cid.GetArrayLength() >= 1)
            configId = cid[0].GetInt64();

        DateTimeOffset? date = null;
        if (order.TryGetProperty("date_order", out var d) && d.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(d.GetString(), out var pd))
            date = pd;

        // Leer líneas y pagos
        var orderFull = await _client.CallAsync<JsonElement>(
            "pos.order",
            "read",
            new
            {
                ids = new[] { id },
                fields = new[] { "lines", "payment_ids" }
            },
            cancellationToken);

        var lines = new List<SaleLineInfo>();
        var payments = new List<SalePaymentInfo>();
        string paymentMethodName = "";

        if (orderFull.ValueKind == JsonValueKind.Array && orderFull.GetArrayLength() > 0)
        {
            var of = orderFull[0];
            if (of.TryGetProperty("lines", out var lns) && lns.ValueKind == JsonValueKind.Array && lns.GetArrayLength() > 0)
            {
                var lineIds = lns.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt64()).ToArray();
                if (lineIds.Length > 0)
                {
                    var lineRows = await _client.CallAsync<JsonElement>(
                        "pos.order.line",
                        "read",
                        new { ids = lineIds, fields = new[] { "product_id", "qty", "price_unit", "price_subtotal", "price_subtotal_incl", "full_product_name" } },
                        cancellationToken);
                    if (lineRows.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var lr in lineRows.EnumerateArray())
                        {
                            var pid = 0L;
                            var pname = ReadText(lr, "full_product_name");
                            if (lr.TryGetProperty("product_id", out var pr) && pr.ValueKind == JsonValueKind.Array && pr.GetArrayLength() >= 2)
                            {
                                pid = pr[0].GetInt64();
                                if (string.IsNullOrEmpty(pname)) pname = ReadTextValue(pr[1]);
                            }
                            var qty = lr.TryGetProperty("qty", out var qq) && qq.ValueKind == JsonValueKind.Number ? qq.GetDecimal() : 0m;
                            var pu = lr.TryGetProperty("price_unit", out var ppu) && ppu.ValueKind == JsonValueKind.Number ? ppu.GetDecimal() : 0m;
                            var ps = lr.TryGetProperty("price_subtotal", out var pps) && pps.ValueKind == JsonValueKind.Number ? pps.GetDecimal() : 0m;
                            var psi = lr.TryGetProperty("price_subtotal_incl", out var ppsi) && ppsi.ValueKind == JsonValueKind.Number ? ppsi.GetDecimal() : ps;
                            lines.Add(new SaleLineInfo(pid, pname, qty, pu, ps, psi));
                        }
                    }
                }
            }

            if (of.TryGetProperty("payment_ids", out var pays) && pays.ValueKind == JsonValueKind.Array && pays.GetArrayLength() > 0)
            {
                var payIds = pays.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt64()).ToArray();
                if (payIds.Length > 0)
                {
                    var payRows = await _client.CallAsync<JsonElement>(
                        "pos.payment",
                        "read",
                        new { ids = payIds, fields = new[] { "id", "amount", "payment_method_id", "is_change" } },
                        cancellationToken);
                    if (payRows.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var pr2 in payRows.EnumerateArray())
                        {
                            var pid2 = pr2.GetProperty("id").GetInt64();
                            var pamt = pr2.TryGetProperty("amount", out var pamt2) && pamt2.ValueKind == JsonValueKind.Number ? pamt2.GetDecimal() : 0m;
                            var isChange = pr2.TryGetProperty("is_change", out var ich) && ich.ValueKind == JsonValueKind.True;
                            long pmid = 0;
                            string pmname = "";
                            if (pr2.TryGetProperty("payment_method_id", out var pm) && pm.ValueKind == JsonValueKind.Array && pm.GetArrayLength() >= 2)
                            {
                                pmid = pm[0].GetInt64();
                                pmname = ReadTextValue(pm[1]);
                                if (string.IsNullOrEmpty(paymentMethodName) && !isChange) paymentMethodName = pmname;
                            }
                            payments.Add(new SalePaymentInfo(pid2, pmid, pmname, pamt, false));
                        }
                    }
                }
            }
        }

        return new SaleInfo(id, name, opId, state, date, total, tax, paid, returned, currency, sessionId, configId,
            paymentMethodName, lines, payments);
    }

    public async Task<SaleInfo> SubmitSaleAsync(SaleSubmission submission, CancellationToken cancellationToken)
    {
        var lines = new List<object>();
        foreach (var line in submission.Lines)
        {
            lines.Add(new object[]
            {
                0, 0,
                new
                {
                    uuid = Guid.NewGuid().ToString(),
                    product_id = line.ProductId,
                    qty = line.Quantity,
                    price_unit = line.UnitPriceWithoutTax,
                    price_subtotal = line.SubtotalWithoutTax,
                    price_subtotal_incl = line.SubtotalWithTax,
                    discount = 0,
                    tax_ids = new object[] { new object[] { 6, 0, line.TaxIds } }
                }
            });
        }

        var payments = new List<object>
        {
            new object[]
            {
                0, 0,
                new
                {
                    amount = submission.TotalWithTax,
                    payment_method_id = submission.CashPaymentMethodId
                }
            }
        };

        var order = new
        {
            uuid = submission.OperationId.ToString(),
            session_id = submission.SessionId,
            lines,
            payment_ids = payments,
            amount_total = submission.TotalWithTax,
            amount_tax = submission.TotalTax,
            amount_paid = submission.TotalWithTax,
            amount_return = 0.0m
        };

        try
        {
            var result = await _client.CallAsync<JsonElement>(
                "pos.order",
                "sync_from_ui",
                new { orders = new[] { order } },
                cancellationToken);

            if (TryReadSyncedOrderId(result, out var orderId))
            {
                var found = await FindSaleByIdAsync(orderId, cancellationToken);
                if (found is not null) return found;
            }
        }
        catch (PosIntegrationException)
        {
            // Si falla, el caller hará conciliación por uuid
            throw;
        }

        var reconciled = await FindSaleByOperationIdAsync(submission.OperationId, cancellationToken);
        if (reconciled is not null) return reconciled;

        throw new PosIntegrationException(
            PosIntegrationFailure.InvalidResponse,
            "Odoo no devolvió una orden válida tras sync_from_ui");
    }

    private static bool TryReadSyncedOrderId(JsonElement result, out long orderId)
    {
        orderId = 0;
        JsonElement orders = default;
        if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("pos.order", out var direct))
            orders = direct;
        else if (result.ValueKind == JsonValueKind.Array && result.GetArrayLength() > 0
                 && result[0].ValueKind == JsonValueKind.Object
                 && result[0].TryGetProperty("pos.order", out var nested))
            orders = nested;

        if (orders.ValueKind != JsonValueKind.Array || orders.GetArrayLength() == 0)
            return false;
        var first = orders[0];
        if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("id", out var id) && id.TryGetInt64(out orderId))
            return true;
        if (first.ValueKind == JsonValueKind.Number && first.TryGetInt64(out orderId))
            return true;
        return false;
    }

    private static string ReadText(JsonElement record, string property)
    {
        if (!record.TryGetProperty(property, out var value))
            return "";
        if (value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? "";
        return "";
    }

    private static string ReadTextValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? "";
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in value.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(prop.Value.GetString()))
                    return prop.Value.GetString() ?? "";
            }
        }
        return "";
    }
}
