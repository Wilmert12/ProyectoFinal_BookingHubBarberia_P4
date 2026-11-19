namespace BookingHub.Blazor.Models;

public record LoginDto(string Email, string Password);

public record SesionDto(string Token, DateTime Expira, int UsuarioId, string Nombre, string Email, string Rol);
