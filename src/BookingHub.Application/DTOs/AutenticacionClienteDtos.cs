namespace BookingHub.Application.DTOs;

public sealed record RegistroClienteRequest(string Nombre, string Telefono, string Email, string Password);

public sealed record RestablecerContrasenaRequest(string Token, string NuevaContrasena);

public sealed record CambiarContrasenaRequest(string ContrasenaActual, string NuevaContrasena);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record SolicitarRestablecimientoRequest(string Email, string Telefono);

public sealed record PerfilClienteDto(int ClienteId, string Nombre, string Email, string Rol);

public sealed record SesionClienteDto(
    string Token,
    DateTime Expira,
    int ClienteId,
    string Nombre,
    string Email,
    string Rol,
    string RefreshToken);

public sealed record IdentidadAutenticada(
    int? UsuarioId,
    int? ClienteId,
    string Nombre,
    string Email,
    string Rol);

public sealed record RefreshRotado(IdentidadAutenticada Identidad, string RefreshToken);
