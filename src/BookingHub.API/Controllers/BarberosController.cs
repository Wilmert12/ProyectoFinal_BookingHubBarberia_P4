using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarberosController(
    BarberoService barberoService,
    DisponibilidadService disponibilidadService,
    AgendaBarberoService agendaService) : ControllerBase
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

    [HttpGet]
    public async Task<ActionResult<List<BarberoDto>>> GetBarberos(bool incluirInactivos = false)
    {
        var result = await barberoService.GetList(b => incluirInactivos || b.Activo);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BarberoDto>> GetBarbero(int id)
    {
        var result = await barberoService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    /// <summary>Horas en las que el barbero puede empezar una cita de la duración indicada.</summary>
    [HttpGet("{id}/disponibilidad")]
    public async Task<ActionResult<DisponibilidadDto>> GetDisponibilidad(int id, DateOnly fecha, int duracionMin = DisponibilidadService.IntervaloMin)
    {
        var result = await disponibilidadService.Calcular(id, fecha, duracionMin);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Admin)]
    [HttpPost]
    public async Task<ActionResult<BarberoDto>> PostBarbero(BarberoDto barbero)
    {
        barbero.BarberoId = 0;

        var result = await barberoService.Guardar(barbero);

        if (result.IsFailure)
            return BadRequest(result.Error);

        var creado = await barberoService.Buscar(barbero.BarberoId);

        return CreatedAtAction(nameof(GetBarbero), new { id = barbero.BarberoId }, creado.Value);
    }

    [Authorize(Roles = RolesSistema.Admin)]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutBarbero(int id, BarberoDto barbero)
    {
        if (id != barbero.BarberoId)
            return BadRequest("El id de la URL no coincide con el del registro.");

        var existe = await barberoService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await barberoService.Guardar(barbero);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    [Authorize(Roles = RolesSistema.Admin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBarbero(int id)
    {
        var existe = await barberoService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await barberoService.Eliminar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }
}
