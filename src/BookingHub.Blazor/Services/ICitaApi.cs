using BookingHub.Blazor.Models;
using Refit;

namespace BookingHub.Blazor.Services;

public interface ICitaApi
{
    /// <summary>Citas de un cliente; la API pide su teléfono para confirmar que es él.</summary>
    [Get("/api/Citas")]
    Task<List<CitaDto>> GetCitasCliente(int clienteId, [Header("X-Cliente-Telefono")] string telefono);

    [Get("/api/Citas/mias")]
    Task<List<CitaDto>> GetMisCitas([Query(Format = "yyyy-MM-dd")] DateOnly fecha);

    [Get("/api/Citas/{id}")]
    Task<CitaDto> GetCita(int id);

    [Post("/api/Citas")]
    Task<CitaDto> CrearCita([Body] CitaDto cita, [Header("X-Cliente-Telefono")] string telefono);
}
