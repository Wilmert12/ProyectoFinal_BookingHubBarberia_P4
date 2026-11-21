namespace BookingHub.Application.DTOs;

/// <summary>Bloque de trabajo de un barbero. DiaSemana: 0 = domingo ... 6 = sábado.</summary>
public record HorarioDto(byte DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);

public record TipoIndisponibilidadDto(int TipoIndisponibilidadId, string Descripcion);

/// <summary>Día libre, vacaciones o cierre. Sin barbero aplica a toda la barbería.</summary>
public record IndisponibilidadDto
{
    public int IndisponibilidadId { get; set; }
    public int? BarberoId { get; set; }
    public string? BarberoNombre { get; set; }
    public int TipoIndisponibilidadId { get; set; }
    public string? Tipo { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public record CambioEstadoDto(int EstadoCitaId);
