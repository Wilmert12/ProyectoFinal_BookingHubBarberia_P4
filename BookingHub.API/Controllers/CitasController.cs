using BookingHub.API.DTOs;
using BookingHub.API.Models;
using BookingHub.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitasController(CitaService citaService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CitaDto>>> GetCitas(int? clienteId, int? barberoId, DateOnly? fecha)
    {
        var desde = fecha?.ToDateTime(TimeOnly.MinValue);
        var hasta = desde?.AddDays(1);

        var result = await citaService.GetList(c =>
            (clienteId == null || c.ClienteId == clienteId) &&
            (barberoId == null || c.BarberoId == barberoId) &&
            (desde == null || (c.FechaInicio >= desde && c.FechaInicio < hasta)));

        if (result.IsFailure)
            return BadRequest(result.Error);

        var citasDto = result.Value
            .Select(c => new CitaDto
            {
                CitaId = c.CitaId,
                Notas = c.Notas
            })
            .ToList();

        return Ok(citasDto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CitaDto>> GetCita(int id)
    {
        var result = await citaService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        var citaDto = new CitaDto
        {
            CitaId = result.Value.CitaId,
            Notas = result.Value.Notas
        };

        return Ok(citaDto);
    }

    [HttpPost]
    public async Task<ActionResult<Cita>> PostCita(Cita cita)
    {
        cita.CitaId = 0;
        cita.FechaRegistro = DateTime.Now;

        var result = await citaService.Guardar(cita);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetCita), new { id = cita.CitaId }, cita);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutCita(int id, Cita cita)
    {
        if (id != cita.CitaId)
            return BadRequest("El id de la URL no coincide con el de la cita.");

        var existe = await citaService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await citaService.Guardar(cita);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCita(int id)
    {
        var existe = await citaService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await citaService.Eliminar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }
}