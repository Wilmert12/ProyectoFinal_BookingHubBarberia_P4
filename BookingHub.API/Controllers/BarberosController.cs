using BookingHub.API.Models;
using BookingHub.API.Services;
using Microsoft.AspNetCore.Mvc;
using BookingHub.API.DTOs;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarberosController(BarberoService barberoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Barbero>>> GetBarberos(bool incluirInactivos = false)
    {
        var result = await barberoService.GetList(b => incluirInactivos || b.Activo);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BarberoDto>> GetBarbero(int id)
    {
        var result = await barberoService.BuscarDto(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<Barbero>> PostBarbero(Barbero barbero)
    {
        barbero.BarberoId = 0;

        var result = await barberoService.Guardar(barbero);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetBarbero), new { id = barbero.BarberoId }, barbero);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutBarbero(int id, Barbero barbero)
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
