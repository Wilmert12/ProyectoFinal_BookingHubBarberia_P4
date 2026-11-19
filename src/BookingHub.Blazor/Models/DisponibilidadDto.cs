namespace BookingHub.Blazor.Models;

public record DisponibilidadDto(int BarberoId, DateOnly Fecha, int DuracionMin, List<HorarioLibreDto> Horarios);

public record HorarioLibreDto(DateTime Inicio, DateTime Fin);
