namespace BookingHub.Application.DTOs;

public sealed record CancelarCitaRequest(string? Motivo);

public sealed record ReprogramarCitaRequest(DateTime NuevaFechaInicio);

public sealed record CambiarEstadoCitaRequest(int EstadoCitaId);

public sealed record CrearBloqueoAgendaRequest(
    DateTime FechaInicio,
    DateTime FechaFin,
    string Tipo,
    string? Descripcion);

public sealed record BloqueoAgendaDto(
    int IndisponibilidadId,
    DateTime FechaInicio,
    DateTime FechaFin,
    string Tipo,
    string? Descripcion);
