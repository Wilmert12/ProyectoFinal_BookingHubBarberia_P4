using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.Admin)]
public class UsuariosController(UsuarioService usuarioService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> GetUsuarios(bool incluirInactivos = false)
    {
        var result = await usuarioService.GetList(u => incluirInactivos || u.Activo);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioDto>> GetUsuario(int id)
    {
        var result = await usuarioService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> PostUsuario(UsuarioDto usuario)
    {
        usuario.UsuarioId = 0;

        var result = await usuarioService.Guardar(usuario);

        if (result.IsFailure)
            return BadRequest(result.Error);

        var creado = await usuarioService.Buscar(usuario.UsuarioId);

        return CreatedAtAction(nameof(GetUsuario), new { id = usuario.UsuarioId }, creado.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutUsuario(int id, UsuarioDto usuario)
    {
        if (id != usuario.UsuarioId)
            return BadRequest("El id de la URL no coincide con el del usuario.");

        var existe = await usuarioService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await usuarioService.Guardar(usuario);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUsuario(int id)
    {
        var existe = await usuarioService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await usuarioService.Eliminar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }
}
