using System.Text.Json;
using Microsoft.Extensions.Options;
using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Models;

namespace Pos.Infrastructure.Odoo;

/// <summary>
/// Adaptador de Odoo 20 POS por JSON-2. Traduce los modelos de Application al formato
/// de Odoo (pos.session, product.product, pos.order.sync_from_ui) y de vuelta.
/// </summary>
public sealed class OdooPosGateway : IPosGateway
{
    private static readonly string[] SessionFields = ["id", "name", "state", "config_id", "start_at", "company_id"];

    private static readonly string[] OrderFields =
    [
        "id", "name", "uuid", "state", "date_order", "amount_total", "amount_tax", "amount_paid",
        "amount_return", "currency_id", "session_id", "config_id", "lines", "payment_ids"
    ];

    private static readonly string[] OptionalProductFields = ["qty_available", "is_storable", "currency_id"];
    private static string[]? _supportedOptionalProductFields;

    private readonly OdooJson2Client _client;
    private readonly OdooOptions _options;

    /// <summary>Crea el adaptador.</summary>
    public OdooPosGateway(OdooJson2Client client, IOptions<OdooOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    // ---------- Sesiones (RF04, RN04, RN05) ----------

    /// <inheritdoc />
    public async Task<PosSessionInfo?> FindActiveSessionAsync(CancellationToken cancellationToken)
    {
        var rows = await SearchOpenSessionRowsAsync(1, cancellationToken);
        return rows.Count == 0 ? null : await MapSessionAsync(rows[0], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PosSessionInfo>> ListOpenSessionsAsync(CancellationToken cancellationToken)
    {
        var rows = await SearchOpenSessionRowsAsync(20, cancellationToken);
        var sessions = new List<PosSessionInfo>(rows.Count);
        foreach (var row in rows)
            sessions.Add(await MapSessionAsync(row, cancellationToken));
        return sessions;
    }

    /// <inheritdoc />
    public async Task<PosSessionInfo?> FindSessionAsync(long sessionId, CancellationToken cancellationToken)
    {
        var result = await _client.CallAsync<JsonElement>(
            "pos.session", "search_read",
            new { domain = new object[] { new object[] { "id", "=", sessionId } }, fields = SessionFields, limit = 1 },
            cancellationToken);
        var rows = OdooJson.Rows(result);
        if (rows.Count == 0) return null;

        var session = await MapSessionAsync(rows[0], cancellationToken);
        return _options.PosConfigId > 0 && session.ConfigId != _options.PosConfigId ? null : session;
    }

    private async Task<IReadOnlyList<JsonElement>> SearchOpenSessionRowsAsync(int limit, CancellationToken cancellationToken)
    {
        var domain = new List<object> { new object[] { "state", "=", "opened" } };
        if (_options.PosConfigId > 0)
            domain.Add(new object[] { "config_id", "=", _options.PosConfigId });

        var result = await _client.CallAsync<JsonElement>(
            "pos.session", "search_read",
            new { domain, fields = SessionFields, limit, order = "id desc" },
            cancellationToken);
        return OdooJson.Rows(result);
    }

    private async Task<PosSessionInfo> MapSessionAsync(JsonElement session, CancellationToken cancellationToken)
    {
        var configId = OdooJson.Many2OneId(session, "config_id");
        var config = configId > 0 ? await ReadPosConfigAsync(configId, cancellationToken) : PosConfigData.Empty;

        return new PosSessionInfo(
            OdooJson.Id(session),
            OdooJson.Text(session, "name"),
            configId,
            OdooJson.Many2OneName(session, "config_id"),
            OdooJson.Text(session, "state"),
            OdooJson.Many2OneId(session, "company_id"),
            config.Currency,
            config.DecimalPlaces,
            config.CashMethodId,
            config.CashMethodName,
            OdooJson.Date(session, "start_at"));
    }

    private async Task<PosConfigData> ReadPosConfigAsync(long configId, CancellationToken cancellationToken)
    {
        var cfg = await _client.CallAsync<JsonElement>(
            "pos.config", "read",
            new { ids = new[] { configId }, fields = new[] { "id", "name", "currency_id", "payment_method_ids" } },
            cancellationToken);
        var rows = OdooJson.Rows(cfg);
        if (rows.Count == 0) return PosConfigData.Empty;

        var config = rows[0];
        var currencyId = OdooJson.Many2OneId(config, "currency_id");
        var (currency, decimals) = currencyId > 0
            ? await ReadCurrencyAsync(currencyId, OdooJson.Many2OneName(config, "currency_id"), cancellationToken)
            : ("", 2);
        var (cashId, cashName) = await FindCashMethodAsync(OdooJson.Ids(config, "payment_method_ids"), cancellationToken);
        return new PosConfigData(currency, decimals, cashId, cashName);
    }

    private async Task<(string Name, int DecimalPlaces)> ReadCurrencyAsync(long currencyId, string fallbackName, CancellationToken cancellationToken)
    {
        var result = await _client.CallAsync<JsonElement>(
            "res.currency", "read",
            new { ids = new[] { currencyId }, fields = new[] { "name", "decimal_places" } },
            cancellationToken);
        var rows = OdooJson.Rows(result);
        if (rows.Count == 0) return (fallbackName, 2);
        var name = OdooJson.Text(rows[0], "name");
        var decimals = (int)OdooJson.Decimal(rows[0], "decimal_places", 2m);
        return (string.IsNullOrEmpty(name) ? fallbackName : name, decimals);
    }

    /// <summary>RN05: el método de efectivo es el de tipo "cash" configurado en el POS, nunca un id fijo.</summary>
    private async Task<(long? Id, string? Name)> FindCashMethodAsync(long[] methodIds, CancellationToken cancellationToken)
    {
        if (methodIds.Length == 0) return (null, null);
        var methods = await ReadPaymentMethodsAsync(methodIds, cancellationToken);
        var cash = methods.Values.FirstOrDefault(m => m.IsCash);
        return cash is null ? (null, null) : (cash.Id, cash.Name);
    }

    private async Task<Dictionary<long, PaymentMethodData>> ReadPaymentMethodsAsync(long[] methodIds, CancellationToken cancellationToken)
    {
        if (methodIds.Length == 0) return [];
        var result = await _client.CallAsync<JsonElement>(
            "pos.payment.method", "read",
            new { ids = methodIds, fields = new[] { "id", "name", "type" } },
            cancellationToken);
        return OdooJson.Rows(result)
            .Select(m => new PaymentMethodData(
                OdooJson.Id(m),
                OdooJson.Text(m, "name"),
                string.Equals(OdooJson.Text(m, "type"), "cash", StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(m => m.Id);
    }

    // ---------- Productos (RF02, RN03, RN06) ----------

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string? search, CancellationToken cancellationToken)
    {
        var domain = new List<object>
        {
            new object[] { "sale_ok", "=", true },
            new object[] { "available_in_pos", "=", true }
        };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            domain.AddRange(["|", "|",
                new object[] { "name", "ilike", term },
                new object[] { "default_code", "ilike", term },
                new object[] { "barcode", "ilike", term }]);
        }

        // product.product: el POS guarda el id de la variante, no el de la plantilla.
        var fields = new[] { "id", "name", "default_code", "barcode", "lst_price", "uom_id" }
            .Concat(await GetOptionalProductFieldsAsync(cancellationToken)).ToArray();
        var result = await _client.CallAsync<JsonElement>(
            "product.product", "search_read",
            new { domain, fields, limit = 100, order = "name" },
            cancellationToken);

        return OdooJson.Rows(result).Select(MapProduct).ToList();
    }

    private static ProductInfo MapProduct(JsonElement p)
    {
        var (available, tracks) = ReadStock(p);
        return new ProductInfo(
            OdooJson.Id(p),
            OdooJson.Text(p, "name"),
            OdooJson.OptionalText(p, "default_code"),
            OdooJson.OptionalText(p, "barcode"),
            OdooJson.Decimal(p, "lst_price"),
            OdooJson.Many2OneName(p, "currency_id"),
            available,
            tracks,
            UnitOf(p));
    }

    private static (decimal? Available, bool Tracks) ReadStock(JsonElement p)
    {
        var tracks = OdooJson.Bool(p, "is_storable");
        return (tracks ? OdooJson.OptionalDecimal(p, "qty_available") : null, tracks);
    }

    private static string UnitOf(JsonElement p)
    {
        var unit = OdooJson.Many2OneName(p, "uom_id");
        return string.IsNullOrEmpty(unit) ? "Unidad" : unit;
    }

    /// <summary>
    /// Campos de stock y moneda que existen en esta instalación (si no está el módulo stock,
    /// qty_available no existe). Se consulta una vez con fields_get y se guarda en memoria.
    /// </summary>
    private async Task<string[]> GetOptionalProductFieldsAsync(CancellationToken cancellationToken)
    {
        if (_supportedOptionalProductFields is { } cached) return cached;
        try
        {
            var fields = await _client.CallAsync<JsonElement>(
                "product.product", "fields_get",
                new { allfields = OptionalProductFields, attributes = new[] { "type" } },
                cancellationToken);
            cached = fields.ValueKind == JsonValueKind.Object
                ? OptionalProductFields.Where(f => fields.TryGetProperty(f, out _)).ToArray()
                : [];
        }
        catch (PosIntegrationException ex) when (ex.Failure is PosIntegrationFailure.Rejected or PosIntegrationFailure.InvalidResponse)
        {
            cached = [];
        }
        _supportedOptionalProductFields = cached;
        return cached;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SaleProduct>> GetSaleProductsAsync(
        IReadOnlyCollection<long> productIds,
        PosSessionInfo session,
        IReadOnlyDictionary<long, decimal> quantities,
        CancellationToken cancellationToken)
    {
        // search_read (no read) para que un id inexistente simplemente no vuelva y se informe PRODUCT_NOT_VENDIBLE.
        var fields = new[] { "id", "name", "sale_ok", "available_in_pos", "lst_price", "taxes_id", "uom_id", "product_tmpl_id" }
            .Concat(await GetOptionalProductFieldsAsync(cancellationToken)).ToArray();
        var products = await _client.CallAsync<JsonElement>(
            "product.product", "search_read",
            new { domain = new object[] { new object[] { "id", "in", productIds.ToArray() } }, fields },
            cancellationToken);

        var result = new List<SaleProduct>();
        foreach (var p in OdooJson.Rows(products))
        {
            var id = OdooJson.Id(p);
            var qty = quantities.TryGetValue(id, out var q) && q > 0 ? q : 1m;
            result.Add(await MapSaleProductAsync(p, session, qty, cancellationToken));
        }
        return result;
    }

    private async Task<SaleProduct> MapSaleProductAsync(JsonElement p, PosSessionInfo session, decimal quantity, CancellationToken cancellationToken)
    {
        var id = OdooJson.Id(p);
        var vendible = OdooJson.Bool(p, "sale_ok") && OdooJson.Bool(p, "available_in_pos");
        var listPrice = OdooJson.Decimal(p, "lst_price");
        var (priceExcl, priceIncl) = vendible && session.ConfigId > 0
            ? await GetPosPricesAsync(p, id, listPrice, session.ConfigId, quantity, cancellationToken)
            : (listPrice, listPrice);
        var (available, tracks) = ReadStock(p);

        return new SaleProduct(id, OdooJson.Text(p, "name"), vendible, priceExcl, priceIncl,
            OdooJson.Ids(p, "taxes_id"), available, tracks, UnitOf(p));
    }

    /// <summary>
    /// RN06: precios unitarios con y sin impuestos según Odoo (get_product_info_pos).
    /// Se llama con la cantidad de la línea: con quantity=1 Odoo 20 redondea 2.875 a 2.88,
    /// y 2.88×2=5.76 no coincide con el impuesto que Odoo recalcula sobre 5.00 (5.75).
    /// Con la cantidad real, el unitario queda 2.875 y el total de línea es el correcto.
    /// </summary>
    private async Task<(decimal Excl, decimal Incl)> GetPosPricesAsync(
        JsonElement p, long variantId, decimal listPrice, long configId, decimal quantity, CancellationToken cancellationToken)
    {
        var templateId = OdooJson.Many2OneId(p, "product_tmpl_id");
        var info = await _client.CallAsync<JsonElement>(
            "product.template", "get_product_info_pos",
            new
            {
                ids = new[] { templateId > 0 ? templateId : variantId },
                price = listPrice,
                quantity = quantity,
                pos_config_id = configId,
                product_variant_id = variantId
            },
            cancellationToken);
        if (info.ValueKind == JsonValueKind.Array && info.GetArrayLength() > 0)
            info = info[0];
        if (info.ValueKind != JsonValueKind.Object || !info.TryGetProperty("all_prices", out var prices))
            return (listPrice, listPrice);
        return (OdooJson.Decimal(prices, "price_without_tax", listPrice), OdooJson.Decimal(prices, "price_with_tax", listPrice));
    }

    // ---------- Ventas (RF05, RF06, RF07, RN07) ----------

    /// <inheritdoc />
    public async Task<SaleInfo?> FindSaleByOperationIdAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var orders = await SearchOrdersAsync(
            new object[] { new object[] { "uuid", "=", operationId.ToString() } }, 1, cancellationToken);
        return orders.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<SaleInfo?> FindSaleByIdAsync(long orderId, CancellationToken cancellationToken)
    {
        var orders = await SearchOrdersAsync(new object[] { new object[] { "id", "=", orderId } }, 1, cancellationToken);
        return orders.FirstOrDefault();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SaleInfo>> ListSalesAsync(int limit, CancellationToken cancellationToken)
    {
        var domain = _options.PosConfigId > 0
            ? new object[] { new object[] { "config_id", "=", _options.PosConfigId } }
            : [];
        return SearchOrdersAsync(domain, limit, cancellationToken);
    }

    /// <summary>
    /// Lee órdenes con sus líneas, pagos y métodos de pago en 4 llamadas en total
    /// (sin N+1, sin importar cuántas órdenes haya).
    /// </summary>
    private async Task<IReadOnlyList<SaleInfo>> SearchOrdersAsync(object[] domain, int limit, CancellationToken cancellationToken)
    {
        var result = await _client.CallAsync<JsonElement>(
            "pos.order", "search_read",
            new { domain, fields = OrderFields, limit, order = "id desc" },
            cancellationToken);
        var orders = OdooJson.Rows(result);
        if (orders.Count == 0) return [];

        var lines = await ReadOrderLinesAsync(orders.SelectMany(o => OdooJson.Ids(o, "lines")).ToArray(), cancellationToken);
        var payments = await ReadPaymentsAsync(orders.SelectMany(o => OdooJson.Ids(o, "payment_ids")).ToArray(), cancellationToken);
        return orders.Select(o => MapOrder(o, lines, payments)).ToList();
    }

    private async Task<ILookup<long, SaleLineInfo>> ReadOrderLinesAsync(long[] lineIds, CancellationToken cancellationToken)
    {
        if (lineIds.Length == 0) return Array.Empty<(long, SaleLineInfo)>().ToLookup(x => x.Item1, x => x.Item2);
        var rows = await _client.CallAsync<JsonElement>(
            "pos.order.line", "read",
            new { ids = lineIds, fields = new[] { "order_id", "product_id", "qty", "price_unit", "price_subtotal", "price_subtotal_incl", "full_product_name" } },
            cancellationToken);
        return OdooJson.Rows(rows).ToLookup(r => OdooJson.Many2OneId(r, "order_id"), MapLine);
    }

    private static SaleLineInfo MapLine(JsonElement row)
    {
        var name = OdooJson.Text(row, "full_product_name");
        if (string.IsNullOrEmpty(name)) name = OdooJson.Many2OneName(row, "product_id");
        var subtotal = OdooJson.Decimal(row, "price_subtotal");
        return new SaleLineInfo(
            OdooJson.Many2OneId(row, "product_id"),
            name,
            OdooJson.Decimal(row, "qty"),
            OdooJson.Decimal(row, "price_unit"),
            subtotal,
            OdooJson.Decimal(row, "price_subtotal_incl", subtotal));
    }

    private async Task<ILookup<long, PaymentRow>> ReadPaymentsAsync(long[] paymentIds, CancellationToken cancellationToken)
    {
        if (paymentIds.Length == 0) return Array.Empty<PaymentRow>().ToLookup(p => p.OrderId);
        var rows = OdooJson.Rows(await _client.CallAsync<JsonElement>(
            "pos.payment", "read",
            new { ids = paymentIds, fields = new[] { "id", "pos_order_id", "amount", "payment_method_id", "is_change" } },
            cancellationToken));

        var methodIds = rows.Select(r => OdooJson.Many2OneId(r, "payment_method_id")).Where(id => id > 0).Distinct().ToArray();
        var methods = await ReadPaymentMethodsAsync(methodIds, cancellationToken);
        return rows.Select(r => MapPayment(r, methods)).ToLookup(p => p.OrderId);
    }

    private static PaymentRow MapPayment(JsonElement row, Dictionary<long, PaymentMethodData> methods)
    {
        var methodId = OdooJson.Many2OneId(row, "payment_method_id");
        var isCash = methods.TryGetValue(methodId, out var method) && method.IsCash;
        var info = new SalePaymentInfo(
            OdooJson.Id(row), methodId, OdooJson.Many2OneName(row, "payment_method_id"), OdooJson.Decimal(row, "amount"), isCash);
        return new PaymentRow(OdooJson.Many2OneId(row, "pos_order_id"), info, OdooJson.Bool(row, "is_change"));
    }

    private static SaleInfo MapOrder(JsonElement order, ILookup<long, SaleLineInfo> lines, ILookup<long, PaymentRow> payments)
    {
        var id = OdooJson.Id(order);
        var orderPayments = payments[id].ToList();
        var mainMethod = orderPayments.FirstOrDefault(p => !p.IsChange)?.Info.PaymentMethodName ?? "";

        return new SaleInfo(
            id,
            OdooJson.Text(order, "name"),
            Guid.TryParse(OdooJson.Text(order, "uuid"), out var opId) ? opId : Guid.Empty,
            OdooJson.Text(order, "state"),
            OdooJson.Date(order, "date_order"),
            OdooJson.Decimal(order, "amount_total"),
            OdooJson.Decimal(order, "amount_tax"),
            OdooJson.Decimal(order, "amount_paid"),
            OdooJson.Decimal(order, "amount_return"),
            OdooJson.Many2OneName(order, "currency_id"),
            OdooJson.Many2OneId(order, "session_id"),
            OdooJson.Many2OneId(order, "config_id"),
            mainMethod,
            lines[id].ToList(),
            orderPayments.Select(p => p.Info).ToList());
    }

    /// <inheritdoc />
    public async Task<SaleInfo> SubmitSaleAsync(SaleSubmission submission, CancellationToken cancellationToken)
    {
        // sync_from_ui es el mismo mecanismo del POS nativo. amount_total/amount_paid viajan dentro
        // del payload estándar de la orden; no se hace ningún write manual de importes.
        var result = await _client.CallAsync<JsonElement>(
            "pos.order", "sync_from_ui",
            new { orders = new[] { BuildOrderPayload(submission) } },
            cancellationToken);

        if (TryReadSyncedOrderId(result, out var orderId))
        {
            var found = await FindSaleByIdAsync(orderId, cancellationToken);
            if (found is not null) return found;
        }

        return await FindSaleByOperationIdAsync(submission.OperationId, cancellationToken)
            ?? throw new PosIntegrationException(
                PosIntegrationFailure.InvalidResponse,
                "Odoo no devolvió una orden válida tras sync_from_ui");
    }

    private static object BuildOrderPayload(SaleSubmission submission)
    {
        var lines = submission.Lines.Select(line => (object)new object[]
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
        }).ToList();

        var payments = new List<object>
        {
            new object[] { 0, 0, new { amount = submission.TotalWithTax, payment_method_id = submission.CashPaymentMethodId } }
        };

        return new
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
        return first.ValueKind == JsonValueKind.Number && first.TryGetInt64(out orderId);
    }

    private sealed record PosConfigData(string Currency, int DecimalPlaces, long? CashMethodId, string? CashMethodName)
    {
        public static readonly PosConfigData Empty = new("", 2, null, null);
    }

    private sealed record PaymentMethodData(long Id, string Name, bool IsCash);

    private sealed record PaymentRow(long OrderId, SalePaymentInfo Info, bool IsChange);
}
