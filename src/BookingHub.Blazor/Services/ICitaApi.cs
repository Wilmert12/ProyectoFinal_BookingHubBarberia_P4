using BookingHub.Blazor.Models;
using Refit;

namespace BookingHub.Blazor.Services;

public interface ICitaApi
{
    [Get("/api/Citas")]
    Task<List<CitaDto>> GetCitas(int? clienteId = null, int? barberoId = null);

    [Get("/api/Citas/mias")]
    Task<List<CitaDto>> GetMisCitas([Query(Format = "yyyy-MM-dd")] DateOnly fecha);

    [Get("/api/Citas/{id}")]
    Task<CitaDto> GetCita(int id);

    [Post("/api/Citas")]
    Task<CitaDto> CrearCita([Body] CitaDto cita);
}
