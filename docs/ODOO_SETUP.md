# Configuración Odoo 20 Community

## 1) Instalación
- Instalar Odoo 20 Community + PostgreSQL (directo o Docker)
- Crear base de datos de prueba
- Activar módulo `Punto de Venta` (point_of_sale)

## 2) Configuración POS
1. Punto de Venta > Configuración > Puntos de venta → crear/configurar
2. Agregar al menos 2 productos vendibles con precio
3. Configurar método de pago **Efectivo** (Cash) vinculado a diario/caja
4. Abrir sesión POS (`Abrir sesión`) → debe quedar en estado **Abierto**

## 3) Usuario de integración
1. Ajustes > Usuarios → crear usuario (o usar existente)
2. Dar permisos necesarios para POS/Productos
3. Preferencias > Seguridad de la cuenta > Claves API → generar API key (copiar y guardar en backend, **nunca** en repo)
4. Anotar URL, BD, API key

## 4) Prueba mínima (semana 1)
1. Desde backend/test: llamar `/json/2/pos.session/search_read` con dominio `[["state","=","opened"]]`
2. Llamar `/json/2/product.template/search_read` con `[["sale_ok","=",true],["available_in_pos","=",true]]` (limitado)
3. Probar `pos.order.sync_from_ui` con 2 líneas + pago en efectivo, `uuid` único
4. Verificar orden creada/pagada en Odoo POS
5. Reenviar mismo `uuid` → debe devolver orden existente (sin duplicado)

## 5) Docker de este equipo
Stack `northwind-odoo20` (`docker-compose.odoo.yml`):

- Imagen `odoo:20.0` y PostgreSQL 16. Postgres no se publica: el puerto 5432 del equipo ya está en uso.
- Base de datos: `odoo20_northwind`.
- Módulo `point_of_sale` instalado. Punto de venta `Northwind Caja`, sesión abierta, método Cash y productos `NW-CAFE` y `NW-SAND`.
- Web en este equipo: [http://127.0.0.1:8070](http://127.0.0.1:8070).
- Web por Tailscale (con Tailscale encendido): [http://100.112.190.88:8070](http://100.112.190.88:8070). Usuario Odoo `admin` / `admin`.
- El puerto 8069 lo ocupa otra instalación y no debe usarse para este proyecto.
- Backend: `Odoo:BaseUrl` = `http://127.0.0.1:8070`, `Odoo:Database` = `odoo20_northwind`. La API key vive solo en `appsettings.Development.json`.
- Cajero de la aplicación web: usuario `cajero`, contraseña `Cajero123!`. La caja está en [http://127.0.0.1:5173](http://127.0.0.1:5173).

Tailscale tiene que estar encendido para abrir la IP `100.112.190.88`. Tailscale Serve (HTTPS en `*.ts.net`) sigue sin estar habilitado; el acceso es HTTP por esa IP.

## Notas
- Si Odoo y backend están en contenedores diferentes, usar nombre/host accesible entre ellos
- No modificar código fuente de Odoo
- No crear módulos Python
- Los IDs varían por instalación (consultarlos desde API)
