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

    /// <summary>El cliente cancela una cita suya que todavía no empieza.</summary>
    [Post("/api/Citas/{id}/cancelar")]
    Task CancelarCita(int id, [Header("X-Cliente-Telefono")] string telefono);

    /// <summary>El barbero marca una de sus citas.</summary>
    [Put("/api/Citas/{id}/estado")]
    Task CambiarEstado(int id, [Body] CambioEstadoDto cambio);

    [Get("/api/EstadosCita")]
    Task<List<EstadoCitaDto>> GetEstados();
}
