using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagosController(PagoService pagoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PagoDto>>> GetPagos(DateOnly? fecha, int? citaId)
    {
        var desde = fecha?.ToDateTime(TimeOnly.MinValue);
        var hasta = desde?.AddDays(1);

        var result = await pagoService.GetList(p =>
            (citaId == null || p.CitaId == citaId) &&
            (desde == null || (p.FechaPago >= desde && p.FechaPago < hasta)));

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PagoDto>> GetPago(int id)
    {
        var result = await pagoService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<PagoDto>> PostPago(PagoDto pago)
    {
        pago.PagoId = 0;

        var result = await pagoService.Guardar(pago);

        if (result.IsFailure)
            return BadRequest(result.Error);

        var creado = await pagoService.Buscar(pago.PagoId);

        return CreatedAtAction(nameof(GetPago), new { id = pago.PagoId }, creado.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutPago(int id, PagoDto pago)
    {
        if (id != pago.PagoId)
            return BadRequest("El id de la URL no coincide con el del pago.");

        var existe = await pagoService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await pagoService.Guardar(pago);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePago(int id)
    {
        var existe = await pagoService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await pagoService.Eliminar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }
}
