namespace BookingHub.Blazor.Models;

public record EstadoCitaDto(int EstadoCitaId, string Descripcion, bool LiberaHorario);

public record CambioEstadoDto(int EstadoCitaId);
