# NorthWindPOS · Frontend

Caja web en **React 19.3 + TypeScript 6.0 + Vite 8.3**, organizada con Clean Architecture adaptada. Solo habla con la API .NET (`/api/...`, con proxy de Vite a `http://127.0.0.1:5000`); **nunca** llama a Odoo ni conoce su API key.

## Capas

```
src/
  domain/            Sin React ni fetch.
    models.ts        Tipos: Product, PosSession, Sale, SaleSummary...
    cart.ts          Cart, CartLine y funciones puras: addProduct, increase, decrease,
                     changeQuantity, removeLine, lineSubtotal, cartTotal, toSaleLines.
    money.ts         Formato de importes.
    saleStatus.ts    Estados de Odoo en español y texto de disponibilidad.
  application/       Casos de uso; dependen de puertos, no de fetch.
    ports.ts         PosApiPort, SessionStorePort.
    errors.ts        ApiError, describeError, isUncertain.
    useCases/        login/logout, loadCatalog/searchProducts, checkout, loadSales/loadSaleDetail.
  infrastructure/    Adaptadores que implementan los puertos.
    http/httpPosApi.ts                 Cliente REST de la API .NET (solo /api/...).
    storage/browserSessionStore.ts     JWT en sessionStorage.
    container.ts                       Composición por defecto.
  presentation/      React.
    App.tsx, ServicesContext.tsx (inyección de dependencias), pages/, components/.
  test/              Vitest + Testing Library.
```

Dependencias: `presentation → application → domain` e `infrastructure → application/domain`. El dominio no importa nada de las otras capas.

## Flujo del cajero

Login (JWT de la API) → sesión POS activa (si hay varias abiertas, selector) → catálogo con precio sin impuestos y disponibilidad → carrito (− / +1 / campo numérico / Quitar, subtotal por línea, total provisional sin impuestos) → **Cobrar en efectivo**.

- El cobro envía solo `productId` y `quantity`; los precios e impuestos los define Odoo.
- Cada cobro lleva un `clientOperationId`. Si la respuesta es incierta (504 `SALE_RESULT_UNCERTAIN` o red caída), se conserva el mismo id para reintentar sin duplicar; se genera uno nuevo solo después de una respuesta 200.
- El mensaje de éxito aparece solo con 200, cuando Odoo confirmó la orden pagada.

## Comandos

```bash
npm ci
npm run dev      # http://127.0.0.1:5173
npm test         # P07 (carrito), P08 (fallos de la API con fetch simulado), login, casos de uso
npm run build    # tsc + vite build
npm run lint     # oxlint
```
