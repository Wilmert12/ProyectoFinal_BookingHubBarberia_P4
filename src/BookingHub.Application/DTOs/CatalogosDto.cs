namespace BookingHub.Application.DTOs;

public record EstadoCitaDto(int EstadoCitaId, string Descripcion, bool LiberaHorario);

public record MetodoPagoDto(int MetodoPagoId, string Descripcion);
