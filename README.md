# Bellas Boutique (ASP.NET Core MVC + SQLite)

## Cómo ejecutar en Visual Studio 2022

1. Instale Visual Studio 2022 con la carga de trabajo **ASP.NET y desarrollo web** y el SDK de **.NET 8**.
2. Abra `BellasBoutique.csproj`.
3. Presione **F5**. La base `bellas.db` se crea sola en la carpeta del proyecto con datos de ejemplo.

## Cuentas de prueba

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | admin@bellas.cr | Admin123! |
| Vendedor | vendedor@bellas.cr | Vendedor123! |
| Cliente | cliente@ejemplo.com | Cliente123! |

## Notas de implementación

- **Datos:** SQLite con EF Core (`EnsureCreated`). Si cambia el modelo, borre `bellas.db` para regenerarla.
- **Correos:** simulados. Se guardan en la tabla `EmailsEnviados` (contraseñas temporales, facturas y comprobantes de entrega).
- **Pagos:** simulados. Tarjeta, SINPE y transferencia no guardan datos sensibles. Las tarjetas que terminan en `0002` se rechazan para probar el flujo de error. SINPE y transferencia quedan en estado *Pendiente*.
- **Producto más cotizado:** cada vez que un cliente agrega un producto al carrito, se incrementa `Producto.VecesAgregadoCarrito`.
- **Sesión:** cookie con expiración deslizante de 5 minutos. El cliente muestra un aviso al minuto 4 y cierra sesión al minuto 5.
- **Chat:** SignalR (`/hubs/chat`). Los mensajes se persisten en `MensajesChat`.
- **Accesibilidad:** animaciones de aparición desactivadas con `prefers-reduced-motion`.
