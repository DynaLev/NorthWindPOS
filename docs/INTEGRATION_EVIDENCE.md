# Evidencia de integración real (P09)

**Estado:** EJECUTADA Y APROBADA el 11 de noviembre de 2026, 23:20 (America/Guayaquil), contra Odoo 20 Community local.

Las dos pruebas de `backend/tests/Pos.Integration.Tests/P09RealOdooTests.cs` pasaron con `RUN_ODOO_INTEGRATION=1`. No se subieron secretos (ni JWT ni API key) a estos archivos.

## Datos del entorno

| Dato | Valor |
|---|---|
| Odoo | 20.0 Community, http://127.0.0.1:8070 (contenedores `northwind-odoo20-odoo-1` y `northwind-odoo20-db-1` en estado Up; `/web/login` respondió 200) |
| Base de datos | `odoo20_northwind` |
| Punto de venta | `pos.config` id 1 — Northwind Caja |
| Sesión | `pos.session` id 1 — `Northwind Caja/00001`, estado `opened` (abierta desde 2026-11-06 15:10) |
| Productos de la prueba | `product.product` id 3 Cafe Norte (`NW-CAFE`, 2.50) e id 4 Sandwich Norte (`NW-SAND`, 4.00). Tipo `consu`, sin control de stock. Impuesto 15% (`price_include = false`). No se usó id 2 (Service Fee). |
| Método de pago | `pos.payment.method` id 1 — Cash (`type = cash`) |
| Totales de la venta de prueba | 2 × Cafe Norte + 1 × Sandwich Norte: neto 9.00, IVA 1.35, **total y pagado 10.35 USD** |

## Checklist previa

- [x] Docker con Odoo 20 y PostgreSQL 16 en marcha
- [x] Sesión `Northwind Caja/00001` abierta en Odoo
- [x] API key de Odoo y `Jwt:SigningKey` configuradas en `appsettings.Development.json` local (gitignored)
- [x] `Odoo:PosConfigId = 1` en esa config local
- [x] `dotnet build` (0 errores, 0 advertencias) y `dotnet test` en verde: 55 pruebas OK y, sin la variable de entorno, 2 de P09 omitidas

## Ejecución automatizada

```powershell
$env:RUN_ODOO_INTEGRATION = "1"
$env:P09_EVIDENCE_DIR = "$PWD\docs\evidence\p09"
dotnet test backend\tests\Pos.Integration.Tests --logger "console;verbosity=detailed"
```

Resultado: **2 correctas, 0 errores**, exit code 0.

### `P09_SaleWithTwoProductsAndCash_IsVerifiedInOdoo_AndRetryDoesNotDuplicate`

1. `POST /api/auth/login` (cajero) → 200, sin guardar el token (`p09-00-login.json`).
2. `GET /api/products` → incluye ids 3 y 4 (`p09-01-products.json`).
3. `GET /api/pos/sessions/active` → `state = opened`, método Cash id 1 (`p09-02-session.json`).
4. `POST /api/sales` con `clientOperationId` `5db73bda-969e-496c-aa88-0abd2efbeab0`, `sessionId` 1, 2 × producto 3 y 1 × producto 4 → 200, `replay = false`.
   - Orden Odoo: **id 10**, nombre `Northwind Caja - 000007`, estado `paid`.
   - Líneas: Cafe Norte qty 2, subtotal 5.00 / con IVA 5.75; Sandwich Norte qty 1, subtotal 4.00 / con IVA 4.60.
   - Pago Cash id 1 por **10.35**; `tax` 1.35; `paid` 10.35; `returned` 0.
   - Request y response: `p09-03-sale-request.json`, `p09-04-sale-response.json`.
5. `GET /api/sales/10` → mismo total 10.35, 2 líneas, estado `paid` (`p09-05-sale-detail.json`). Esta lectura sale de Odoo (no es un cache local).
6. `POST /api/sales` otra vez con el **mismo** `clientOperationId` → `replay = true`, mismo `orderId` 10 (`p09-06-retry-same-operation.json`).
7. `GET /api/sales?limit=200` → una sola orden con ese `operationId` (`p09-07-history.json`).

### `P09_TwoSimultaneousPostsWithSameOperation_CreateOneOrder`

Dos POST simultáneos con el mismo `clientOperationId` `27e136ab-6b51-49f2-b1c6-d8d18a331081` crearon **una sola** orden: id 9, `Northwind Caja - 000006`, total 10.35, estado `paid`. Una respuesta con `replay = false` y la otra con `replay = true` (`p09-08-double-click.json`).

## Verificación en Odoo

La orden se comprobó leyéndola de nuevo desde Odoo por la API (JSON-2 / `GET /api/sales/{id}`), no solo con la respuesta de `sync_from_ui`:

- [x] Orden `Northwind Caja - 000007` (id 10) con 2 líneas, total 10.35 y estado `paid`
- [x] Pago Cash por 10.35 (`paymentMethodId` 1, `isCash` true)
- [x] `operationId` / uuid igual a `5db73bda-969e-496c-aa88-0abd2efbeab0`
- [x] Tras el reintento, una sola orden con ese uuid
- [x] Sesión `Northwind Caja/00001` (id 1) sigue `opened` y aparece en la orden

No se adjuntaron capturas PNG de la interfaz de Odoo: la evidencia reproducible está en los JSON de `docs/evidence/p09/` (sin secretos). La preparación previa del entorno quedó en `docs/evidence/odoo-session-open.json`, `odoo-products.json` y `odoo-payment-method-cash.json`.

## Resultado

| Paso | Resultado | Archivo |
|---|---|---|
| Login | 200, operador Cajero Northwind | `evidence/p09/p09-00-login.json` |
| Productos | ids 3 y 4 presentes | `evidence/p09/p09-01-products.json` |
| Sesión activa | id 1, `opened`, Cash id 1 | `evidence/p09/p09-02-session.json` |
| Venta (request/response) | orden 10, `paid`, total 10.35, `replay: false` | `p09-03-sale-request.json`, `p09-04-sale-response.json` |
| Detalle | mismo total, 2 líneas, pago Cash 10.35 | `p09-05-sale-detail.json` |
| Reintento mismo id | `replay: true`, mismo `orderId` 10 | `p09-06-retry-same-operation.json` |
| Historial sin duplicado | una sola entrada para ese `operationId` | `p09-07-history.json` |
| Doble POST simultáneo | una sola orden (id 9), una respuesta es replay | `p09-08-double-click.json` |
| Orden en Odoo | verificada por API: `paid`, 10.35 pagados en Cash | `p09-04` y `p09-05` |

Fecha de ejecución: **11 de noviembre de 2026, 23:20 (America/Guayaquil)**. Orden Odoo: **id 10, `Northwind Caja - 000007`**. `amount_total`: **10.35**. `amount_paid`: **10.35**. Estado: **paid**.
