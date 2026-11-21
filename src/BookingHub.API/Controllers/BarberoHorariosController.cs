using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

/// <summary>Horario semanal de cada barbero (misma ruta que /api/Barberos).</summary>
[ApiController]
[Route("api/Barberos")]
public class BarberoHorariosController(AgendaBarberoService agendaService) : ControllerBase
{
    /// <summary>Horario semanal del barbero. Vacío = horario por defecto (lunes a sábado, 9:00 a 18:00).</summary>
    [Authorize(Roles = RolesSistema.Personal)]
    [HttpGet("{id}/horarios")]
    public async Task<ActionResult<List<HorarioDto>>> GetHorarios(int id)
    {
        var result = await agendaService.GetHorarios(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    /// <summary>Reemplaza el horario semanal completo del barbero.</summary>
    [Authorize(Roles = RolesSistema.Admin)]
    [HttpPut("{id}/horarios")]
    public async Task<IActionResult> PutHorarios(int id, List<HorarioDto> horarios)
    {
        var result = await agendaService.GuardarHorarios(id, horarios);

        if (result.IsFailure)
            return result.Error.Code == "BARBERO_NO_EXISTE" ? NotFound() : BadRequest(result.Error);

        return NoContent();
    }
}
