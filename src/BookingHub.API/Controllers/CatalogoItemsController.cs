using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogoItemsController(CatalogoItemService catalogoItemService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CatalogoItemDto>>> GetCatalogoItems(bool incluirInactivos = false)
    {
        var result = await catalogoItemService.GetList(i => incluirInactivos || i.Activo);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CatalogoItemDto>> GetCatalogoItem(int id)
    {
        var result = await catalogoItemService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<CatalogoItemDto>> PostCatalogoItem(CatalogoItemDto catalogoItem)
    {
        catalogoItem.CatalogoItemId = 0;

        var result = await catalogoItemService.Guardar(catalogoItem);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetCatalogoItem), new { id = catalogoItem.CatalogoItemId }, catalogoItem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutCatalogoItem(int id, CatalogoItemDto catalogoItem)
    {
        if (id != catalogoItem.CatalogoItemId)
            return BadRequest("El id de la URL no coincide con el del registro.");

        var existe = await catalogoItemService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await catalogoItemService.Guardar(catalogoItem);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCatalogoItem(int id)
    {
        var existe = await catalogoItemService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await catalogoItemService.Eliminar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }
}
