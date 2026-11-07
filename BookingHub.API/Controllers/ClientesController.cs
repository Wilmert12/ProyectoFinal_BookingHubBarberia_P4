using BookingHub.API.Models;
using BookingHub.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController(ClienteService clienteService) : ControllerBase
{
    // GET: api/Clientes?incluirInactivos=true
    [HttpGet]
    public async Task<ActionResult<List<Cliente>>> GetClientes(bool incluirInactivos = false)
    {
        var result = await clienteService.GetList(c => incluirInactivos || c.Activo);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    // GET: api/Clientes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Cliente>> GetCliente(int id)
    {
        var result = await clienteService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    // POST: api/Clientes
    [HttpPost]
    public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
    {
        cliente.ClienteId = 0;

        var result = await clienteService.Guardar(cliente);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetCliente), new { id = cliente.ClienteId }, cliente);
    }

    // PUT: api/Clientes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCliente(int id, Cliente cliente)
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

    // DELETE: api/Clientes/5  (baja lógica: Activo = false)
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
