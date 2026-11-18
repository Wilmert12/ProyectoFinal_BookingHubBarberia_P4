using System.Text.Json.Serialization;

namespace BookingHub.Application.DTOs;

public record UsuarioDto
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int RolId { get; set; }
    public string? Rol { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime? UltimoLogin { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Password { get; set; }
}

public record RolDto(int RolId, string Descripcion);

public record LoginDto(string Email, string Password);

public record UsuarioSesionDto(int UsuarioId, string Nombre, string Email, string Rol);

public record SesionDto(string Token, DateTime Expira, int UsuarioId, string Nombre, string Email, string Rol);
