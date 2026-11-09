# Arquitectura NorthWindPOS

## Capas backend (Clean Architecture)
```
Pos.Domain (entidades/reglas propias)
    ↑
Pos.Application (casos de uso, puertos, excepciones, servicios)
    ↑
Pos.Infrastructure (adaptadores HTTP/Odoo, auth, DI)
    ↑
Pos.WebApi (controllers, filtros, configuración)
```
Las dependencias apuntan hacia las capas internas.

## Integración con Odoo
- Protocolo: JSON-2 `POST /json/2/<model>/<method>` con `Authorization: bearer <api_key>`, opcional `X-Odoo-Database`
- Método de venta: `pos.order.sync_from_ui` con `orders[]` conteniendo `uuid`, `session_id`, `lines`, `payment_ids`
- Búsqueda productos: `product.template.search_read` con `sale_ok=true`, `available_in_pos=true`
- Info fiscal/precios: `product.template.get_product_info_pos` (contexto fiscal/precios)
- Sesión activa: `pos.session.search_read` estado `opened`
- Métodos de pago: desde `pos.config.payment_method_ids` / `pos.session.payment_method_ids`; validar tipo `cash`
- Conciliación: búsqueda por `uuid` tras timeout/fallo de conexión

## Frontend (adaptado)
- Capas: `api/` (cliente REST), `auth/` (token), `pages/` (Login, POS, Historial, Detalle), `components/` (carrito/errores), `types/`, `utils/`
- Flujo: Login JWT → Sesión activa → Catálogo → Carrito → Cobro (efectivo) → Confirmación / Error incierto con reintento por `clientOperationId`

## Reglas implementadas
RN01–RN08 cubiertas en Domain/Application. Odoo es fuente de verdad para precios/totales/estado.
