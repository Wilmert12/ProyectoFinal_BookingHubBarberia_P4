using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.Admin)]
public class ReportesController(ReporteService reporteService) : ControllerBase
{
    [HttpGet("ingresos")]
    public async Task<ActionResult<ReporteIngresosDto>> GetIngresos(DateOnly desde, DateOnly hasta)
    {
        var result = await reporteService.GetIngresos(desde, hasta);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
