using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookingHub.API.Controllers;

/// <summary>"¿Olvidaste tu número?" para clientes (misma ruta que /api/Clientes).</summary>
[ApiController]
[Route("api/Clientes")]
public class ClienteRecuperarController(ReservaService reservaService) : ControllerBase
{
    /// <summary>"¿Olvidaste tu número?": con el nombre y el teléfono del registro devuelve el número de cliente.</summary>
    [EnableRateLimiting(LimiteDeIntentos.Clientes)]
    [HttpPost("recuperar")]
    public async Task<ActionResult<NumeroClienteDto>> Recuperar(RecuperarNumeroDto datos)
    {
        var numero = await reservaService.RecuperarNumero(datos.Nombre, datos.Telefono);
        if (numero is null)
            return NotFound(new Aplicada1.Core.Error("CLIENTE_NO_ENCONTRADO", "No encontramos un cliente con ese nombre y teléfono."));

        return Ok(numero);
    }
}
