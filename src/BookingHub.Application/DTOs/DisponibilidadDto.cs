namespace BookingHub.Application.DTOs;

public record DisponibilidadDto(int BarberoId, DateOnly Fecha, int DuracionMin, List<HorarioLibreDto> Horarios);

public record HorarioLibreDto(DateTime Inicio, DateTime Fin);
