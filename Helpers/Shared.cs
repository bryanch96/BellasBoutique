using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace BellasBoutique.Helpers;

public static class Roles
{
    public const string Cliente = "Cliente";
    public const string Vendedor = "Vendedor";
    public const string Administrador = "Administrador";
    public const string Personal = "Vendedor,Administrador";
}

public static class Negocio
{
    public const decimal Impuesto = 0.13m;
    public const int StockBajo = 5;

    public static decimal CalcularImpuesto(decimal subtotal) =>
        Math.Round(subtotal * Impuesto, 0, MidpointRounding.AwayFromZero);
}

public static class Estados
{
    public const string Exitoso = "Exitoso";
    public const string Fallido = "Fallido";
    public const string Pendiente = "Pendiente";
}

public static class Metodos
{
    public const string Tarjeta = "Tarjeta";
    public const string Sinpe = "SINPE";
    public const string Transferencia = "Transferencia";
    public static readonly string[] Todos = { Tarjeta, Sinpe, Transferencia };
}

public static class Money
{
    // Formato de los wireframes: ₡18 500 (colones, sin decimales, espacio como separador).
    public static string Fmt(decimal valor) =>
        "₡" + valor.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");
}

public static class ReglasPassword
{
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!#$%&*?";

    // Mínimo 8 caracteres con mayúscula, minúscula, número y carácter especial.
    public static string? Validar(string p)
    {
        if (p.Length < 8) return "La contraseña debe tener al menos 8 caracteres.";
        if (!Regex.IsMatch(p, "[a-z]")) return "Falta una letra minúscula.";
        if (!Regex.IsMatch(p, "[A-Z]")) return "Falta una letra mayúscula.";
        if (!Regex.IsMatch(p, "[0-9]")) return "Falta un número.";
        if (!Regex.IsMatch(p, "[^a-zA-Z0-9]")) return "Falta un carácter especial.";
        return null;
    }

    public static string Generar()
    {
        while (true)
        {
            var candidata = RandomNumberGenerator.GetString(Alfabeto, 12);
            if (Validar(candidata) is null) return candidata;
        }
    }
}

public static class ReglasPago
{
    public static bool TarjetaValida(string numero)
    {
        var limpio = numero.Replace(" ", "").Replace("-", "");
        return Regex.IsMatch(limpio, "^\\d{13,19}$");
    }

    public static bool VencimientoValido(string valor)
    {
        var m = Regex.Match(valor ?? "", "^(0[1-9]|1[0-2])/(\\d{2})$");
        if (!m.Success) return false;
        int mes = int.Parse(m.Groups[1].Value);
        int anio = 2000 + int.Parse(m.Groups[2].Value);
        var fin = new DateTime(anio, mes, 1).AddMonths(1);
        return fin > DateTime.Today;
    }
}

public static class ClaimsExtensions
{
    public static int UsuarioId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
