using BookingHub.API.Models;
using BookingHub.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitasController(CitaService citaService) : ControllerBase
{
    // GET: api/Citas?clienteId=1&barberoId=2&fecha=2026-11-10
    [HttpGet]
    public async Task<ActionResult<List<Cita>>> GetCitas(int? clienteId, int? barberoId, DateOnly? fecha)
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

    // GET: api/Citas/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Cita>> GetCita(int id)
    {
        var result = await citaService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    // POST: api/Citas
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

    // PUT: api/Citas/5
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

    // DELETE: api/Citas/5
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
