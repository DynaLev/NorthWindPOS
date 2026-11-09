# Evidencia de integración real (P09)

**Estado:** PENDIENTE - Requiere instancia Odoo 20 Community local con sesión POS abierta.

## Checklist
- [ ] Odoo 20 Community corriendo con PostgreSQL
- [ ] Módulo `point_of_sale` instalado y configurado
- [ ] Punto de venta con sesión **abierta**
- [ ] Al menos 2 productos vendibles con precio
- [ ] Método de pago **Efectivo** configurado
- [ ] Usuario de integración con API key
- [ ] Backend configurado con `Odoo:BaseUrl`, `Database`, `ApiKey`

## Prueba reproducible
1. Iniciar backend y frontend
2. Login con operador configurado
3. Verificar sesión activa (`/api/pos/sessions/active`)
4. Buscar productos (`/api/products`)
5. Agregar 2 productos al carrito, cobrar en efectivo
6. Capturar: request/response POST /api/sales, respuesta Odoo (si visible), ID de orden Odoo, estado, importe pagado
7. Verificar en interfaz POS de Odoo la orden creada y pago registrado
8. Probar reenvío con mismo `clientOperationId` → debe retornar orden existente sin duplicado

## Capturas/evidencia a adjuntar
- `evidence/odoo-session-open.png` - Sesión POS abierta
- `evidence/odoo-products.png` - Productos configurados
- `evidence/odoo-payment-method-cash.png` - Método efectivo
- `evidence/api-sale-request.json` - Request POST /api/sales
- `evidence/api-sale-response.json` - Response con orderId/operationId/state
- `evidence/odoo-order-created.png` - Orden en Odoo con líneas/pagos/importe
- `evidence/retry-same-operation.json` - Reintento mostrando Replay=true / misma orden

## Notas
- No se ha ejecutado contra Odoo local en este entorno; la implementación sigue el contrato `sync_from_ui` según código fuente Odoo 20.
- Ante cualquier bloqueo, documentar logs HTTP, permisos y traza según instrucción del proyecto.
