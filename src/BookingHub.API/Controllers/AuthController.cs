using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(UsuarioService usuarioService, GeneradorToken generadorToken) : ControllerBase
{
    [EnableRateLimiting(LimiteDeIntentos.Login)]
    [HttpPost("login")]
    public async Task<ActionResult<SesionDto>> Login(LoginDto login)
    {
        var result = await usuarioService.IniciarSesion(login);

        if (result.IsFailure)
            return Unauthorized(result.Error);

        return Ok(generadorToken.Crear(result.Value!));
    }

    [Authorize]
    [HttpGet("yo")]
    public ActionResult<UsuarioSesionDto> Yo()
    {
        return Ok(new UsuarioSesionDto(
            int.Parse(User.FindFirst("sub")!.Value),
            User.FindFirst("name")?.Value ?? string.Empty,
            User.FindFirst("email")?.Value ?? string.Empty,
            User.FindFirst("role")?.Value ?? string.Empty));
    }
}
