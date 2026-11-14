using BookingHub.Blazor.Models;
using Refit;

namespace BookingHub.Blazor.Services;

public interface ICitaApi
{
    [Get("/api/Citas")]
    Task<List<CitaDto>> GetCitas(int? clienteId = null, int? barberoId = null);

    [Get("/api/Citas/{id}")]
    Task<CitaDto> GetCita(int id);
}
