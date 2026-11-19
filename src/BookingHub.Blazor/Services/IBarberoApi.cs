using BookingHub.Blazor.Models;
using Refit;

namespace BookingHub.Blazor.Services;

public interface IBarberoApi
{
    [Get("/api/Barberos")]
    Task<List<BarberoDto>> GetBarberos();

    [Get("/api/Barberos/{id}/disponibilidad")]
    Task<DisponibilidadDto> GetDisponibilidad(int id, [Query(Format = "yyyy-MM-dd")] DateOnly fecha, int duracionMin);
}
