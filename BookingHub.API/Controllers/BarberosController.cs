using BookingHub.API.DTOs;
using BookingHub.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarberosController(BarberoService barberoService) : ControllerBase
{
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
