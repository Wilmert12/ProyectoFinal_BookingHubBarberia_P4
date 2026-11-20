using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace BookingHub.Application.Services;

public sealed class ServicioAutenticacionCliente(
    IDbContextFactory<BookingHubContext> contextFactory,
    ICorreoTransaccional correo,
    ILogger<ServicioAutenticacionCliente> logger)
{
    public const string RolCliente = "Cliente";
    private const string TipoRestablecimiento = "PasswordReset";
    private static readonly PasswordHasher<Cliente> Hasher = new();
    private static readonly TimeSpan VigenciaRestablecimiento = TimeSpan.FromMinutes(30);
    public bool PuedeEnviarCorreo => correo.EstaConfigurado;

    public async Task<(Cliente? Cliente, string? Error)> Registrar(
        RegistroClienteRequest solicitud, CancellationToken cancellationToken)
    {
        var nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        var email = NormalizarEmail(solicitud.Email);
        var telefono = solicitud.Telefono?.Trim() ?? string.Empty;
        var error = ValidarDatos(nombre, telefono, email, solicitud.Password);
        if (error is not null) return (null, error);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existeEnClientes = await db.Clientes.AnyAsync(c => c.EmailNormalizado == email, cancellationToken);
        var existeEnPersonal = await db.Usuarios.AnyAsync(u => u.Email.ToLower() == email, cancellationToken);
        if (existeEnClientes || existeEnPersonal)
            return (null, "Ya existe una cuenta con ese correo. Si ya eres cliente, usa 'Olvidé mi contraseña'.");

        var cliente = new Cliente
        {
            Nombre = nombre,
            Telefono = telefono,
            Email = email,
            EmailNormalizado = email,
            Activo = true,
            FechaRegistro = DateTime.Now
        };
        cliente.PasswordHash = Hasher.HashPassword(cliente, solicitud.Password);
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(cancellationToken);
        return (cliente, null);
    }

    public async Task<Cliente?> IniciarSesion(string email, string password, CancellationToken cancellationToken)
    {
        var normalizado = NormalizarEmail(email);
        if (normalizado is null || string.IsNullOrEmpty(password)) return null;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.EmailNormalizado == normalizado, cancellationToken);
        if (cliente is null || !cliente.Activo || cliente.PasswordHash is null) return null;

        PasswordVerificationResult validacion;
        try { validacion = Hasher.VerifyHashedPassword(cliente, cliente.PasswordHash, password); }
        catch (FormatException) { return null; }
        if (validacion == PasswordVerificationResult.Failed) return null;
        if (validacion == PasswordVerificationResult.SuccessRehashNeeded)
        {
            cliente.PasswordHash = Hasher.HashPassword(cliente, password);
            await db.SaveChangesAsync(cancellationToken);
        }
        return cliente;
    }

    public async Task SolicitarRestablecimiento(
        string email, string telefono, CancellationToken cancellationToken)
    {
        if (!correo.EstaConfigurado) return;
        var normalizado = NormalizarEmail(email);
        var telefonoNormalizado = NormalizarTelefono(telefono);
        if (normalizado is null || telefonoNormalizado.Length != 10) return;

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var cliente = await db.Clientes.FirstOrDefaultAsync(c =>
            c.EmailNormalizado == normalizado && c.Activo && c.Email != null, cancellationToken);
        if (cliente is null || NormalizarTelefono(cliente.Telefono) != telefonoNormalizado) return;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var ahora = DateTime.UtcNow;
        db.AuthTokens.Add(new AuthToken
        {
            ClienteId = cliente.ClienteId,
            Tipo = TipoRestablecimiento,
            TokenHash = HashTokenAuth.Calcular(token),
            FechaCreacion = ahora,
            FechaExpiracion = ahora.Add(VigenciaRestablecimiento)
        });
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            await correo.Enviar(cliente.Email!, "Restablecer contraseña de CerquilloApp",
                $"Usa este código en CerquilloApp para crear una contraseña nueva. Vence en 30 minutos.\n\n{token}\n\nSi no solicitaste este cambio, ignora este mensaje.",
                cancellationToken);
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "No se pudo enviar un correo de restablecimiento de contraseña.");
            await db.AuthTokens.Where(t => t.TokenHash == HashTokenAuth.Calcular(token) && t.FechaRevocacion == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.FechaRevocacion, DateTime.UtcNow), cancellationToken);
        }
    }

    public async Task<(bool Hecho, string? Error)> RestablecerContrasena(
        RestablecerContrasenaRequest solicitud, CancellationToken cancellationToken)
    {
        var error = ValidarContrasena(solicitud.NuevaContrasena);
        if (error is not null) return (false, error);
        var hash = HashTokenAuth.Calcular(solicitud.Token ?? string.Empty);
        var ahora = DateTime.UtcNow;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var registro = await db.AuthTokens.AsNoTracking().SingleOrDefaultAsync(t =>
            t.TokenHash == hash && t.Tipo == TipoRestablecimiento && t.FechaRevocacion == null &&
            t.FechaExpiracion > ahora, cancellationToken);
        if (registro?.ClienteId is not int clienteId)
            return (false, "El código venció o ya fue utilizado. Solicita otro restablecimiento.");

        var consumido = await db.AuthTokens
            .Where(t => t.AuthTokenId == registro.AuthTokenId && t.FechaRevocacion == null && t.FechaExpiracion > ahora)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.FechaRevocacion, ahora), cancellationToken);
        if (consumido != 1)
            return (false, "El código venció o ya fue utilizado. Solicita otro restablecimiento.");

        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.Activo, cancellationToken);
        if (cliente is null)
            return (false, "El código venció o ya fue utilizado. Solicita otro restablecimiento.");

        cliente.PasswordHash = Hasher.HashPassword(cliente, solicitud.NuevaContrasena);
        await db.AuthTokens.Where(t => t.ClienteId == cliente.ClienteId && t.Tipo == "Refresh" && t.FechaRevocacion == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.FechaRevocacion, ahora), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> CambiarContrasena(
        int clienteId, CambiarContrasenaRequest solicitud, CancellationToken cancellationToken)
    {
        if (ValidarContrasena(solicitud.NuevaContrasena) is not null) return false;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.Activo, cancellationToken);
        if (cliente?.PasswordHash is null) return false;
        try
        {
            if (Hasher.VerifyHashedPassword(cliente, cliente.PasswordHash, solicitud.ContrasenaActual) == PasswordVerificationResult.Failed)
                return false;
        }
        catch (FormatException) { return false; }

        cliente.PasswordHash = Hasher.HashPassword(cliente, solicitud.NuevaContrasena);
        var ahora = DateTime.UtcNow;
        await db.AuthTokens.Where(t => t.ClienteId == clienteId && t.Tipo == "Refresh" && t.FechaRevocacion == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.FechaRevocacion, ahora), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> EliminarCuenta(int clienteId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.Activo, cancellationToken);
        if (cliente is null) return false;

        var ahora = DateTime.UtcNow;
        cliente.Nombre = "Cliente eliminado";
        cliente.Telefono = $"ELIMINADO-{cliente.ClienteId}";
        cliente.Email = null;
        cliente.EmailNormalizado = null;
        cliente.PasswordHash = null;
        cliente.FotoPerfil = null;
        cliente.Notas = null;
        cliente.Activo = false;
        await db.Citas.Where(c => c.ClienteId == clienteId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Notas, (string?)null), cancellationToken);
        await db.FavoritosBarberos.Where(f => f.ClienteId == clienteId).ExecuteDeleteAsync(cancellationToken);
        await db.AuthTokens.Where(t => t.ClienteId == clienteId && t.FechaRevocacion == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.FechaRevocacion, ahora), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public static string? NormalizarEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static string? ValidarDatos(string nombre, string telefono, string? email, string password)
    {
        if (nombre.Length is < 2 or > 100) return "El nombre debe tener entre 2 y 100 caracteres.";
        if (email is null || email.Length > 255 || !System.Net.Mail.MailAddress.TryCreate(email, out _))
            return "Ingresa un correo electrónico válido.";
        var digitos = SoloDigitos(telefono);
        var telefonoNacional = digitos.Length == 11 && digitos.StartsWith("1") ? digitos[1..] : digitos;
        if (!Regex.IsMatch(telefono, @"^\+?[0-9 ()-]{10,20}$") || telefonoNacional.Length != 10 ||
            telefonoNacional[..3] is not ("809" or "829" or "849"))
            return "Ingresa un teléfono dominicano válido, con código de país si corresponde.";
        return ValidarContrasena(password);
    }

    private static string? ValidarContrasena(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres.";
        if (password.Length > 128) return "La contraseña no puede superar los 128 caracteres.";
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            return "Incluye al menos una mayúscula, una minúscula y un número.";
        return null;
    }

    private static string SoloDigitos(string? valor) => new((valor ?? string.Empty).Where(char.IsDigit).ToArray());

    private static string NormalizarTelefono(string? valor)
    {
        var digitos = SoloDigitos(valor);
        return digitos.Length == 11 && digitos.StartsWith("1") ? digitos[1..] : digitos;
    }
}
