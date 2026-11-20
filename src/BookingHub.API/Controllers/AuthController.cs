using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UsuarioService usuarioService,
    ServicioAutenticacionCliente autenticacionCliente,
    ServicioTokensAuth tokensAuth,
    GeneradorToken generadorToken) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult> Login(LoginDto login, CancellationToken cancellationToken)
    {
        var result = await usuarioService.IniciarSesion(login);
        if (result.IsSuccess)
        {
            var refresh = await tokensAuth.CrearParaUsuario(result.Value!.UsuarioId, cancellationToken);
            return Ok(generadorToken.Crear(result.Value, refresh));
        }

        var cliente = await autenticacionCliente.IniciarSesion(login.Email, login.Password, cancellationToken);
        if (cliente is null)
            return Unauthorized("Correo o contraseña incorrectos.");

        var refreshCliente = await tokensAuth.CrearParaCliente(cliente.ClienteId, cancellationToken);
        return Ok(generadorToken.CrearCliente(cliente, refreshCliente));
    }

    [HttpPost("registro-cliente")]
    [EnableRateLimiting("auth-register")]
    public async Task<ActionResult<SesionClienteDto>> RegistrarCliente(
        RegistroClienteRequest solicitud, CancellationToken cancellationToken)
    {
        var (cliente, error) = await autenticacionCliente.Registrar(solicitud, cancellationToken);
        if (cliente is null) return BadRequest(error);
        var refreshToken = await tokensAuth.CrearParaCliente(cliente.ClienteId, cancellationToken);
        var sesion = generadorToken.CrearCliente(cliente, refreshToken);
        return Created("/api/Auth/yo", sesion);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult> Refresh(RefreshTokenRequest solicitud, CancellationToken cancellationToken)
    {
        var rotado = await tokensAuth.Rotar(solicitud.RefreshToken, cancellationToken);
        if (rotado is null) return Unauthorized("La sesión venció. Inicia sesión nuevamente.");

        var accessToken = generadorToken.CrearToken(rotado.Identidad, out var expira);
        if (rotado.Identidad.ClienteId is int clienteId)
            return Ok(new SesionClienteDto(accessToken, expira, clienteId, rotado.Identidad.Nombre,
                rotado.Identidad.Email, rotado.Identidad.Rol, rotado.RefreshToken));

        return Ok(new SesionDto(accessToken, expira, rotado.Identidad.UsuarioId!.Value,
            rotado.Identidad.Nombre, rotado.Identidad.Email, rotado.Identidad.Rol, rotado.RefreshToken));
    }

    [HttpPost("olvide-contrasena")]
    [EnableRateLimiting("auth-register")]
    public async Task<IActionResult> OlvideContrasena(
        SolicitarRestablecimientoRequest solicitud, CancellationToken cancellationToken)
    {
        if (!autenticacionCliente.PuedeEnviarCorreo)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "El servicio de recuperación por correo no está configurado. Contacta a la barbería.");
        await autenticacionCliente.SolicitarRestablecimiento(solicitud.Email, solicitud.Telefono, cancellationToken);
        return Ok(new { mensaje = "Si los datos corresponden a una cuenta, recibirás instrucciones por correo." });
    }

    [HttpPost("restablecer-contrasena")]
    [EnableRateLimiting("auth-register")]
    public async Task<IActionResult> RestablecerContrasena(
        RestablecerContrasenaRequest solicitud, CancellationToken cancellationToken)
    {
        var resultado = await autenticacionCliente.RestablecerContrasena(solicitud, cancellationToken);
        return resultado.Hecho ? NoContent() : BadRequest(resultado.Error);
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpPost("cambiar-contrasena")]
    public async Task<IActionResult> CambiarContrasena(
        CambiarContrasenaRequest solicitud, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        if (!await autenticacionCliente.CambiarContrasena(clienteId, solicitud, cancellationToken))
            return BadRequest("La contraseña actual no coincide o la nueva contraseña no cumple los requisitos.");
        return NoContent();
    }

    [Authorize]
    [HttpGet("yo")]
    public ActionResult Yo()
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var id)) return Unauthorized();
        var nombre = User.FindFirst("name")?.Value ?? string.Empty;
        var email = User.FindFirst("email")?.Value ?? string.Empty;
        var rol = User.FindFirst("role")?.Value ?? string.Empty;
        return rol.Equals(RolesSistema.Cliente, StringComparison.OrdinalIgnoreCase)
            ? Ok(new PerfilClienteDto(id, nombre, email, rol))
            : Ok(new UsuarioSesionDto(id, nombre, email, rol));
    }
}
