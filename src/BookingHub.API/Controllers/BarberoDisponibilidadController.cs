using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

/// <summary>Horas libres de cada barbero (misma ruta que /api/Barberos).</summary>
[ApiController]
[Route("api/Barberos")]
public class BarberoDisponibilidadController(DisponibilidadService disponibilidadService) : ControllerBase
{
    /// <summary>Horas en las que el barbero puede empezar una cita de la duración indicada.</summary>
    [HttpGet("{id}/disponibilidad")]
    public async Task<ActionResult<DisponibilidadDto>> GetDisponibilidad(int id, DateOnly fecha, int duracionMin = DisponibilidadService.IntervaloMin)
    {
        var result = await disponibilidadService.Calcular(id, fecha, duracionMin);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }
}
