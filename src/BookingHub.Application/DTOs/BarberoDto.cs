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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Cedula { get; set; }

    /// <summary>Solo lo ve el personal; en la web pública llega vacío. Null al guardar = no cambiar.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? ComisionPorcentaje { get; set; }
}
