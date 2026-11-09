# NorthWindPOS

Punto de venta web integrado con Odoo 20 Community.

- Frontend: React + TypeScript (Vite)
- Backend: ASP.NET Core, Clean Architecture (`Pos.Domain`, `Pos.Application`, `Pos.Infrastructure`, `Pos.WebApi`)
- ERP: Odoo 20 Community y PostgreSQL 16 en Docker
- Integración: el navegador llama a la API .NET y la API llama a Odoo por JSON-2 (`pos.order.sync_from_ui`). La API no entra a PostgreSQL y no modifica Odoo.

## Requisitos

- Docker Desktop
- .NET SDK 10
- Node.js 22 o superior y npm
- Tailscale, solo si vas a abrir Odoo por la IP de la tailnet

## 1. Odoo

En la raíz del proyecto:

```powershell
docker compose -f docker-compose.odoo.yml up -d
```

Odoo queda en:

- Este equipo: http://127.0.0.1:8070
- Tailscale: http://100.112.190.88:8070 (Tailscale encendido). Si tu IP es otra, cámbiala en `docker-compose.odoo.yml` con `tailscale ip -4`.

Usuario inicial de Odoo: `admin` / `admin`. Cámbiala antes de compartir el equipo.

La base de datos es `odoo20_northwind`. El contenedor ya arranca con el módulo Punto de venta. En una instalación nueva, la primera vez hay que crear los datos de caja. Con los contenedores en marcha:

```powershell
Get-Content -Raw scripts\bootstrap_odoo.py | docker exec -i northwind-odoo20-odoo-1 odoo shell -d odoo20_northwind --no-http --db_host=db --db_user=odoo --db_password=odoo
```

Eso deja:

- Punto de venta `Northwind Caja` con sesión abierta
- Método de pago Cash
- Productos `Cafe Norte` (`NW-CAFE`) y `Sandwich Norte` (`NW-SAND`)
- Una API key de Odoo, impresa una sola vez al final como `APIKEY`

Si la base se crea vacía y Odoo no tiene el módulo, detén el contenedor, añade `-i point_of_sale` al `command` de `docker-compose.odoo.yml`, levántalo una vez y vuelve a quitar esa opción.

No uses el puerto 8069 si en el equipo ya hay otra instalación de Odoo.

## 2. API .NET

Copia la configuración de ejemplo y pon la API key que imprimió el script. No subas ese archivo a Git.

```powershell
Copy-Item backend\src\Pos.WebApi\appsettings.Development.json.example backend\src\Pos.WebApi\appsettings.Development.json
```

En `appsettings.Development.json`:

- `Odoo:BaseUrl`: `http://127.0.0.1:8070`
- `Odoo:Database`: `odoo20_northwind`
- `Odoo:ApiKey`: la clave impresa por el script
- `Jwt:SigningKey`: una frase larga propia

El ejemplo ya trae el cajero de demostración:

- Usuario: `cajero`
- Contraseña: `Cajero123!`

Arranque:

```powershell
dotnet restore NorthWindPOS.slnx
dotnet run --project backend\src\Pos.WebApi --launch-profile http
```

La API escucha en http://localhost:5000. Swagger está en http://localhost:5000/swagger.

## 3. Página web

```powershell
cd frontend
npm ci
npm run dev
```

Abre http://127.0.0.1:5173 e inicia sesión con `cajero` / `Cajero123!`.

Para cobrar: busca un producto, agrégalo al carrito y pulsa **Cobrar en efectivo**. El total de la pantalla es provisional; el importe pagado lo confirma Odoo. La venta queda en **Ventas**.

## Pruebas

```powershell
dotnet test NorthWindPOS.slnx
cd frontend
npm test
```

## Qué no se sube a Git

- `appsettings.Development.json` (API key, JWT y operadores reales)
- `bin/`, `obj/`, `node_modules/`, `dist/` y `.vs/`
- `.env` y `docker-compose.override.yml`

## Documentación

- `docs/ARQUITECTURA.md`
- `docs/ODOO_SETUP.md`
- `docs/TRACEABILITY.md`
- `docs/INTEGRATION_EVIDENCE.md`
