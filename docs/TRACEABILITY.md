# Matriz de trazabilidad

| Requisito | Componente | Evidencia / prueba |
|---|---|---|
| RF01 | `AuthController` → `LoginCommand` (+Validator) → `LoginCommandHandler`, JWT, PBKDF2; `[Authorize]` | `LoginCommandHandlerTests`, `ApiHttpTests` (401 credenciales, 401 sin token, 400 vacío), `LoginPage.test.tsx` |
| RF02 | `ProductsController` → `SearchProductsQuery` → `OdooPosGateway` (`product.product`, `qty_available`, `is_storable`) | GET /api/products?search=, `PosPage.test.tsx` (disponibilidad y precio) |
| RF03 | `domain/cart.ts`, `Cart.tsx`, `QuantityInput.tsx` | P07: `cart.test.ts`, `Cart.test.tsx` |
| RF04 | `PosSessionsController` → `GetActiveSessionQuery` / `GetOpenSessionsQuery`, `PosSessionService`, `Odoo:PosConfigId`, `SessionPicker` | GET /api/pos/sessions/active, GET /api/pos/sessions, P03 |
| RF05 | `CreateSaleCommandHandler` → `OdooPosGateway.SubmitSaleAsync` (`pos.order.sync_from_ui`) | P09 `P09RealOdooTests` |
| RF06 | Pago efectivo con método `type = cash` leído de Odoo; `SaleVerifier`; `SalePaymentInfo.IsCash` | P05, P09 (`isCash`, `paymentMethodId = 1`) |
| RF07 | `SalesController` → `GetSalesQuery`, `GetSaleByIdQuery`, `GetSaleByOperationIdQuery` | GET /api/sales, /api/sales/{id}, P09 |
| RF08 | `ApiExceptionFilter` (503/504/502/422/409/400/401/404), resultado incierto | `ApiExceptionFilterTests`, `ApiHttpTests`, P05, P06, P08 |
| RN01–RN02 | `CreateSaleCommandValidator` + `SaleDraft`/`SaleLine` (Domain) | P01, P02, `ValidationAndMediatorTests` |
| RN03 | `SaleSubmissionBuilder` (`sale_ok` + `available_in_pos`, id inexistente) | P04 |
| RN04–RN05 | `PosSessionService` (estado `opened`, método cash) | P03 |
| RN06 | Precios de `get_product_info_pos`, redondeo a `decimal_places`, totales releídos de Odoo | `SaleSubmissionTests`, P09 (total = suma de líneas de Odoo) |
| RN07 | `clientOperationId` = `pos.order.uuid`, conciliación antes y después, `SaleOperationLock` | P06 (3 casos), concurrencia, P09 (reintento y doble clic) |
| RN08 | `SaleVerifier.EnsurePaid` en envío, existente, Timeout y Unavailable | P05, `RN08_*` |
| RNF01 | user-secrets / variables de entorno; nada en frontend ni repo | README, `.gitignore` |
| RNF02 | 4 proyectos + 3 de pruebas, dependencias hacia adentro | `NorthWindPOS.slnx`, build sin advertencias |
| RNF03 | Mensajes en español, estados traducidos, flujo de 1 pantalla | `PosPage`, `saleStatus.ts` |
| RNF04 | Botón deshabilitado al cobrar, uuid, lock por operación | `ConcurrentRequestsWithSameOperation_SubmitOnlyOnce`, P09 doble POST |
| RNF05 | Integración encapsulada en Infrastructure (`IPosGateway`) | `OdooPosGateway` |
| RNF06 | README con versiones, configuración, arranque y pruebas | `README.md`, `frontend/README.md` |
| RNF07 | Etiquetas visibles, `aria-label`, `role=alert/status`, foco visible, `scope` en tablas | componentes de `presentation/` |
| P01–P06 | `backend/tests/Pos.Application.Tests` | `dotnet test` |
| P07–P08 | `frontend/src/test` | `npm test` |
| P09 | `backend/tests/Pos.Integration.Tests` | `docs/INTEGRATION_EVIDENCE.md`, `docs/evidence/` |
