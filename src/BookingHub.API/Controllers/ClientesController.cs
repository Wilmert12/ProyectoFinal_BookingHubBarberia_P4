using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController(ClienteService clienteService, ReservaService reservaService) : ControllerBase
{
    /// <summary>"¿Olvidaste tu número?": con el nombre y el teléfono del registro devuelve el número de cliente.</summary>
    [EnableRateLimiting(LimiteDeIntentos.Clientes)]
    [HttpPost("recuperar")]
    public async Task<ActionResult<NumeroClienteDto>> Recuperar(RecuperarNumeroDto datos)
    {
        var numero = await reservaService.RecuperarNumero(datos.Nombre, datos.Telefono);
        if (numero is null)
            return NotFound(new Aplicada1.Core.Error("CLIENTE_NO_ENCONTRADO", "No encontramos un cliente con ese nombre y teléfono."));

        return Ok(numero);
    }

    [Authorize(Roles = RolesSistema.Personal)]
    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> GetClientes(bool incluirInactivos = false)
    {
        var result = await clienteService.GetList(c => incluirInactivos || c.Activo);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Personal)]
    [HttpGet("{id}")]
    public async Task<ActionResult<ClienteDto>> GetCliente(int id)
    {
        var result = await clienteService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    [EnableRateLimiting(LimiteDeIntentos.Clientes)]
    [HttpPost]
    public async Task<ActionResult<ClienteDto>> PostCliente(ClienteDto cliente)
    {
        cliente.ClienteId = 0;

        var result = await clienteService.Guardar(cliente);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetCliente), new { id = cliente.ClienteId }, cliente);
    }

    [Authorize(Roles = RolesSistema.Personal)]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCliente(int id, ClienteDto cliente)
    {
        if (id != cliente.ClienteId)
            return BadRequest("El id de la URL no coincide con el del registro.");

        var existe = await clienteService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await clienteService.Guardar(cliente);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    [Authorize(Roles = RolesSistema.Personal)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCliente(int id)
    {
        var existe = await clienteService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await clienteService.Eliminar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }
}
