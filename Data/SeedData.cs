using BellasBoutique.Helpers;
using BellasBoutique.Models;
using Microsoft.AspNetCore.Identity;

namespace BellasBoutique.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        if (db.Usuarios.Any()) return;

        var usuarios = new[]
        {
            new Usuario { Cedula = "1-0000-0001", Nombre = "Ana", Apellidos = "Administradora", Email = "admin@bellas.cr", Telefono = "8888-0001", Direccion = "San José, Centro", Rol = Roles.Administrador },
            new Usuario { Cedula = "1-0000-0002", Nombre = "Luis", Apellidos = "Vendedor", Email = "vendedor@bellas.cr", Telefono = "8888-0002", Direccion = "Heredia, Centro", Rol = Roles.Vendedor },
            new Usuario { Cedula = "1-1234-5678", Nombre = "Nombre", Apellidos = "Apellidos", Email = "cliente@ejemplo.com", Telefono = "8888-8888", Direccion = "Provincia, cantón, distrito", Rol = Roles.Cliente },
        };
        var claves = new[] { "Admin123!", "Vendedor123!", "Cliente123!" };
        for (int i = 0; i < usuarios.Length; i++)
            usuarios[i].PasswordHash = hasher.HashPassword(usuarios[i], claves[i]);
        db.Usuarios.AddRange(usuarios);

        var ropa = new Categoria { Nombre = "Ropa" };
        var calzado = new Categoria { Nombre = "Calzado" };
        var accesorios = new Categoria { Nombre = "Accesorios" };
        db.Categorias.AddRange(ropa, calzado, accesorios);

        var textiles = new Proveedor { Nombre = "Textiles Central" };
        var calzadoSur = new Proveedor { Nombre = "Calzado Sur" };
        db.Proveedores.AddRange(textiles, calzadoSur);
        db.SaveChanges();

        // Productos y stock tomados de los wireframes (Catálogo y Carrito).
        var productos = new List<Producto>
        {
            new() { Nombre = "Blusa de lino", Descripcion = "Corte holgado en lino suave.", Precio = 18500, Stock = 14, CategoriaId = ropa.Id, ProveedorId = textiles.Id, VecesAgregadoCarrito = 14 },
            new() { Nombre = "Vestido midi", Descripcion = "Largo a media pierna, tela fluida.", Precio = 32000, Stock = 8, CategoriaId = ropa.Id, ProveedorId = textiles.Id, VecesAgregadoCarrito = 9 },
            new() { Nombre = "Jeans recto", Descripcion = "Tiro alto y lavado medio.", Precio = 24900, Stock = 20, CategoriaId = ropa.Id, ProveedorId = textiles.Id, VecesAgregadoCarrito = 22 },
            new() { Nombre = "Camisa blanca", Descripcion = "Algodón, cuello clásico.", Precio = 19800, Stock = 11, CategoriaId = ropa.Id, ProveedorId = textiles.Id, VecesAgregadoCarrito = 5 },
            new() { Nombre = "Sandalias de cuero", Descripcion = "Suela flexible, hebilla ajustable.", Precio = 27500, Stock = 9, CategoriaId = calzado.Id, ProveedorId = calzadoSur.Id, VecesAgregadoCarrito = 17 },
            new() { Nombre = "Botines negros", Descripcion = "Tacón bajo, cierre lateral.", Precio = 41000, Stock = 6, CategoriaId = calzado.Id, ProveedorId = calzadoSur.Id, VecesAgregadoCarrito = 3 },
            new() { Nombre = "Cartera de mano", Descripcion = "Cierre magnético, forro interior.", Precio = 29900, Stock = 12, CategoriaId = accesorios.Id, ProveedorId = calzadoSur.Id, VecesAgregadoCarrito = 11 },
            new() { Nombre = "Cinturón de cuero", Descripcion = "Hebilla metálica, ancho medio.", Precio = 12500, Stock = 18, CategoriaId = accesorios.Id, ProveedorId = calzadoSur.Id, VecesAgregadoCarrito = 8 },
        };
        db.Productos.AddRange(productos);
        db.SaveChanges();

        // Ventas de ejemplo para que los reportes y gráficos tengan datos.
        var ejemplos = new (int idx, int cant, int diasAtras)[]
        {
            (0, 2, 1), (2, 1, 1), (4, 1, 2), (2, 3, 3), (6, 1, 5), (7, 2, 6), (0, 1, 8),
        };
        foreach (var (idx, cant, dias) in ejemplos)
        {
            var p = productos[idx];
            db.Ventas.Add(new Venta { ProductoId = p.Id, Cantidad = cant, PrecioUnitario = p.Precio, FechaVenta = DateTime.Now.AddDays(-dias) });
        }

        db.Faqs.AddRange(
            new Faq { Orden = 1, Pregunta = "¿Cómo puedo recuperar mi contraseña?", Respuesta = "Desde el inicio de sesión, seleccione «¿Olvidó su contraseña?» e ingrese su correo electrónico. Recibirá una nueva contraseña generada automáticamente." },
            new Faq { Orden = 2, Pregunta = "¿Qué métodos de pago aceptan?", Respuesta = "Tarjeta de crédito o débito, SINPE Móvil y transferencia bancaria." },
            new Faq { Orden = 3, Pregunta = "¿Cómo recibo la factura de mi compra?", Respuesta = "La factura se envía a su correo electrónico y también puede consultarla en la confirmación de compra." },
            new Faq { Orden = 4, Pregunta = "¿Puedo cambiar mi dirección de entrega?", Respuesta = "Sí. Puede indicar otra dirección en el paso de pago, antes de confirmar la compra." }
        );

        db.SaveChanges();
    }
}
