using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogoItemsController(CatalogoItemService catalogoItemService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CatalogoItemDto>>> GetCatalogoItems(
        bool incluirInactivos = false,
        string? q = null,
        bool? esServicio = null,
        string? ordenar = null)
    {
        var result = await catalogoItemService.GetList(i => (incluirInactivos || i.Activo) &&
            (esServicio == null || i.EsServicio == esServicio) &&
            (string.IsNullOrWhiteSpace(q) || i.Nombre.Contains(q) || (i.Descripcion != null && i.Descripcion.Contains(q)) ||
                (i.Categoria != null && i.Categoria.Contains(q))));

        if (result.IsFailure)
            return BadRequest(result.Error);

        var items = result.Value ?? [];
        items = ordenar?.Trim().ToLowerInvariant() switch
        {
            "precio" => items.OrderBy(i => i.Precio).ToList(),
            "duracion" => items.OrderBy(i => i.DuracionMin ?? int.MaxValue).ToList(),
            "nombre" => items.OrderBy(i => i.Nombre).ToList(),
            "precio-desc" => items.OrderByDescending(i => i.Precio).ToList(),
            _ => items.OrderBy(i => i.Nombre).ToList()
        };
        return Ok(items);
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

    [Authorize(Roles = RolesSistema.Admin)]
    [HttpPost]
    public async Task<ActionResult<CatalogoItemDto>> PostCatalogoItem(CatalogoItemDto catalogoItem)
    {
        catalogoItem.CatalogoItemId = 0;

        var result = await catalogoItemService.Guardar(catalogoItem);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetCatalogoItem), new { id = catalogoItem.CatalogoItemId }, catalogoItem);
    }

    [Authorize(Roles = RolesSistema.Admin)]
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

    [Authorize(Roles = RolesSistema.Admin)]
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
