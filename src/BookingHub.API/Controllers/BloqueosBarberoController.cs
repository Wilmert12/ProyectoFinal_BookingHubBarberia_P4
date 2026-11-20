using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Authorize(Roles = RolesSistema.Barbero)]
[Route("api/Barberos/me/bloqueos")]
public sealed class BloqueosBarberoController(AgendaBarberoService agendaBarberoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BloqueoAgendaDto>>> Get(DateOnly? desde, DateOnly? hasta)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId)) return Unauthorized();
        var result = await agendaBarberoService.Listar(usuarioId, desde, hasta);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<BloqueoAgendaDto>> Post(CrearBloqueoAgendaRequest request)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId)) return Unauthorized();
        var result = await agendaBarberoService.Crear(usuarioId, request);
        if (result.IsFailure) return BadRequest(result.Error);
        return CreatedAtAction(nameof(Get), new { desde = DateOnly.FromDateTime(result.Value!.FechaInicio), hasta = DateOnly.FromDateTime(result.Value.FechaFin) }, result.Value);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId)) return Unauthorized();
        var result = await agendaBarberoService.Eliminar(usuarioId, id);
        return result.IsFailure ? BadRequest(result.Error) : NoContent();
    }
}
