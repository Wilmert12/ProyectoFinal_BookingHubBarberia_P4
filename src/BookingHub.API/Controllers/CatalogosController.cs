using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api")]
public class CatalogosController(CatalogosService catalogosService) : ControllerBase
{
    [HttpGet("EstadosCita")]
    public async Task<ActionResult<List<EstadoCitaDto>>> GetEstadosCita()
    {
        var result = await catalogosService.GetEstadosCita();

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("MetodosPago")]
    public async Task<ActionResult<List<MetodoPagoDto>>> GetMetodosPago()
    {
        var result = await catalogosService.GetMetodosPago();

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
