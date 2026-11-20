using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Authorize(Roles = RolesSistema.Cliente)]
[Route("api/Resenas")]
public sealed class ResenasController(ExperienciaClienteService experienciaClienteService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ResenaDto>> Post(CrearResenaRequest request)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        var result = await experienciaClienteService.CrearResena(clienteId, request);
        return result.IsFailure ? BadRequest(result.Error) : CreatedAtAction(
            nameof(BarberosController.GetResenas), "Barberos", new { id = result.Value!.BarberoId }, result.Value);
    }
}
