using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

/// <summary>Días libres, vacaciones y cierres. Sin barbero aplica a toda la barbería.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.Personal)]
public class IndisponibilidadesController(AgendaBarberoService agendaService) : ControllerBase
{
    /// <summary>Días libres que terminan después de <paramref name="desde"/> (hoy si no se indica).</summary>
    [HttpGet]
    public async Task<ActionResult<List<IndisponibilidadDto>>> GetIndisponibilidades(DateTime? desde, int? barberoId)
    {
        var result = await agendaService.GetIndisponibilidades(desde ?? HoraLocal.Ahora.Date, barberoId);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("tipos")]
    public async Task<ActionResult<List<TipoIndisponibilidadDto>>> GetTipos()
    {
        var result = await agendaService.GetTipos();

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Admin)]
    [HttpPost]
    public async Task<ActionResult<IndisponibilidadDto>> PostIndisponibilidad(IndisponibilidadDto indisponibilidad)
    {
        var result = await agendaService.CrearIndisponibilidad(indisponibilidad);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(indisponibilidad);
    }

    [Authorize(Roles = RolesSistema.Admin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIndisponibilidad(int id)
    {
        var result = await agendaService.EliminarIndisponibilidad(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }
}
