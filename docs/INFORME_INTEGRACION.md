# Informe breve de integración y contingencia

## Mecanismo elegido

La venta se registra con `pos.order.sync_from_ui(orders)` por JSON-2, el mismo método que usa el POS nativo de Odoo para sincronizar órdenes. Cada orden lleva:

- `uuid` = `clientOperationId` del frontend (idempotencia, RN07)
- `session_id` de una sesión abierta (RN04)
- `lines` con `product_id`, `qty`, `price_unit`, subtotales e impuestos calculados por Odoo (`get_product_info_pos`) y redondeados a los decimales de la moneda (RN06)
- `payment_ids` con un pago en el método de tipo `cash` configurado en el POS (RN05)

`amount_total`/`amount_paid` solo viajan dentro del payload estándar de la orden, como en el POS nativo. **No** se hace `create` + `write` manual de importes ni `write` de `amount_paid` (la ruta diagnóstica del documento docente). Tras el envío, la orden se vuelve a leer de Odoo y solo se informa éxito si está `paid`/`done` y `amount_paid >= amount_total` (RN08).

## Manejo de fallas

| Falla | Comportamiento |
|---|---|
| Odoo caído / 5xx en lecturas | Reintento (`Odoo:MaxRetries`) y luego 503 `ODOO_UNAVAILABLE` |
| Timeout en lecturas | Reintento y luego 504 `ODOO_TIMEOUT` |
| Timeout o caída al enviar la venta | Sin reenvío automático. Se busca la orden por `uuid`: si existe y está pagada → `replay: true`; si no está pagada → 409 `SALE_NOT_VERIFIED`; si no existe → 504 `SALE_RESULT_UNCERTAIN` y el cajero reintenta con el mismo id |
| API key inválida / sin permisos | 502 `ODOO_AUTHENTICATION` / `ODOO_PERMISSION` con mensaje claro |
| Datos rechazados por Odoo | 422 `ODOO_REJECTED` |
| Doble clic / dos pestañas | Botón deshabilitado + lock por `clientOperationId` en la API + conciliación por `uuid` |

## Contingencia

1. **`sync_from_ui` rechaza el formato en Odoo 20** (422 o 500 al vender): revisar el log de la API (`Odoo pos.order.sync_from_ui -> HTTP ...`), comparar el payload con el que envía el POS nativo (herramientas del navegador en la interfaz POS de Odoo, pestaña Red, llamada `sync_from_ui`) y ajustar solo `OdooPosGateway.BuildOrderPayload`. Ningún otro componente cambia.
2. **El resultado queda incierto**: el cajero consulta el historial (`/ventas`) o `GET /api/sales/by-operation/{id}`; reintentar siempre con el mismo id, nunca con uno nuevo.
3. **Odoo no disponible durante la demo**: la API responde 503 con mensaje claro y no registra nada; levantar Docker (`docker compose -f docker-compose.odoo.yml up -d`) y verificar que la sesión siga abierta.
4. **Sesión cerrada**: la API responde `POS_SESSION_NOT_OPEN`/`POS_SESSION_NOT_FOUND`; abrir la sesión en Odoo (Punto de Venta → Abrir sesión) o volver a correr `scripts/bootstrap_odoo.py`.
5. **API key comprometida**: revocarla en Odoo (Preferencias → Seguridad → Claves API), crear otra y guardarla con `dotnet user-secrets`.

No se usan simuladores como entrega: los dobles de prueba solo existen dentro de los proyectos de pruebas automatizadas.
