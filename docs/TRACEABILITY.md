# Matriz de trazabilidad

| Requisito | Componente | Evidencia |
|---|---|---|
| RF01 | AuthController + JWT + operadores configurados | POST /api/auth/login, tests auth |
| RF02 | ProductsController + Odoo product.template | GET /api/products?search= |
| RF03 | Frontend Cart + validaciones dominio | Componentes carrito, P07 |
| RF04 | PosSessionsController + PosSessionService | GET /api/pos/sessions/active |
| RF05 | SaleService + OdooPosGateway.sync_from_ui | POST /api/sales, P09 evidencia |
| RF06 | Pago efectivo + validación estado/pago | Verificación en SaleService y mapeo Odoo |
| RF07 | SalesController GET list/detail | GET /api/sales, /api/sales/{id} |
| RF08 | ApiExceptionFilter + manejo timeout/uncertain | P05, P06, mensajes ProblemDetails |
| RN01-RN02 | SaleDraft/SaleLine (Domain) | P01, P02 |
| RN03 | Validación product available_in_pos | P04 |
| RN04-RN05 | PosSessionService validación sesión y cash method | P03 |
| RN06 | Precios desde Odoo (get_product_info_pos) | Gateway |
| RN07 | clientOperationId + búsqueda por uuid + conciliación | P06, timeout handling |
| RN08 | Verificación estado paid/done y paid>=total | SaleService.CreateAsync |
| RNF01 | API key solo backend | Configuración, sin claves en frontend/repo |
| RNF02 | Clean Architecture + capas | Estructura proyectos |
| RNF04 | Idempotencia + conciliación | Implementado |
| RNF05 | Integración encapsulada en Infrastructure | OdooPosGateway |
| P01-P06 | Pos.Application.Tests | dotnet test OK |
| P07-P08 | frontend tests | npm test OK |
| P09 | Integración real Odoo | docs/INTEGRATION_EVIDENCE.md |
