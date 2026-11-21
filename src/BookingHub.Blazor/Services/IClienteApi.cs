using BookingHub.Blazor.Models;
using Refit;

namespace BookingHub.Blazor.Services;

public interface IClienteApi
{
    [Get("/api/Clientes/{id}")]
    Task<ClienteDto> GetCliente(int id);

    [Post("/api/Clientes")]
    Task<ClienteDto> CrearCliente([Body] ClienteDto cliente);

    /// <summary>"¿Olvidaste tu número?": con nombre y teléfono del registro.</summary>
    [Post("/api/Clientes/recuperar")]
    Task<NumeroClienteDto> Recuperar([Body] RecuperarNumeroDto datos);
}
