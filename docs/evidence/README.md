# Evidencia P09

Coloque aquí la evidencia de la integración real con Odoo 20. No guarde tokens JWT, API keys ni capturas donde se vean.

## Generado automáticamente

`dotnet test backend/tests/Pos.Integration.Tests` con `RUN_ODOO_INTEGRATION=1` y `P09_EVIDENCE_DIR=docs/evidence/p09` crea:

| Archivo | Contenido |
|---|---|
| `p09/p09-00-login.json` | Estado 200 y datos del cajero (sin token) |
| `p09/p09-01-products.json` | Respuesta de `GET /api/products` |
| `p09/p09-02-session.json` | Sesión POS activa |
| `p09/p09-03-sale-request.json` | Cuerpo de `POST /api/sales` |
| `p09/p09-04-sale-response.json` | Orden creada (orderId, uuid, estado, totales, pagos) |
| `p09/p09-05-sale-detail.json` | `GET /api/sales/{id}` |
| `p09/p09-06-retry-same-operation.json` | Reintento con el mismo id (`replay: true`) |
| `p09/p09-07-history.json` | Historial (una sola orden con ese uuid) |
| `p09/p09-08-double-click.json` | Dos POST simultáneos, misma orden |
| `p09-dotnet-test.txt` | Salida de la consola de la prueba |

## Capturas manuales

| Archivo | Qué mostrar |
|---|---|
| `odoo-session-open.png` | Sesión `Northwind Caja/00001` en estado Abierta |
| `odoo-products.png` | Cafe Norte y Sandwich Norte disponibles en POS |
| `odoo-payment-method-cash.png` | Método Cash (tipo efectivo) en Northwind Caja |
| `web-cart.png` | Carrito con 2 productos en la caja web |
| `web-sale-success.png` | Mensaje de venta registrada |
| `odoo-order-created.png` | Orden en Odoo con líneas, total y estado Pagado |
| `odoo-order-payment.png` | Pago Cash de la orden |
| `odoo-no-duplicate.png` | Lista de órdenes filtrada por el uuid: una sola |
| `api-log.txt` | Log de la API con las llamadas `Odoo modelo.método -> HTTP estado en N ms` |
