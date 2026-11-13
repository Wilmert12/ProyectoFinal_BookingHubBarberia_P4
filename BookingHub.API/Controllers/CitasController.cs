using BookingHub.API.DTOs;
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

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CitaDto>> GetCita(int id)
    {
        var result = await citaService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<CitaDto>> PostCita(CitaDto cita)
    {
        cita.CitaId = 0;

        var result = await citaService.Guardar(cita);

        if (result.IsFailure)
            return BadRequest(result.Error);

        var creada = await citaService.Buscar(cita.CitaId);

        return CreatedAtAction(nameof(GetCita), new { id = cita.CitaId }, creada.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutCita(int id, CitaDto cita)
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
