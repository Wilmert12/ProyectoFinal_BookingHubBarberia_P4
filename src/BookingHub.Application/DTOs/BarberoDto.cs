using System.Text.Json.Serialization;

namespace BookingHub.Application.DTOs;

public record BarberoDto
{
    public int BarberoId { get; set; }
    public int UsuarioId { get; set; }
    public string? Nombre { get; set; }
    public string? Apodo { get; set; }
    public string ColorHex { get; set; } = "#000000";
    public bool Activo { get; set; } = true;
    public decimal CalificacionPromedio { get; set; }
    public int TotalResenas { get; set; }
    public string? Especialidades { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Cedula { get; set; }
}
