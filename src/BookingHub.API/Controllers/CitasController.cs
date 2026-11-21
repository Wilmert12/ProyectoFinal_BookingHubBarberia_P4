using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitasController(CitaService citaService, BarberoService barberoService, ReservaService reservaService) : ControllerBase
{
    private bool EsPersonal => User.IsInRole(RolesSistema.Admin) || User.IsInRole(RolesSistema.Recepcion);

    private string? TelefonoCliente => Request.Headers[ReservaService.EncabezadoTelefono].FirstOrDefault();

    /// <summary>
    /// El personal ve cualquier agenda. Un cliente solo ve sus citas y tiene que mandar
    /// su teléfono en el encabezado X-Cliente-Telefono.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CitaDto>>> GetCitas(int? clienteId, int? barberoId, DateOnly? fecha)
    {
        if (!EsPersonal)
        {
            if (clienteId is null)
                return Unauthorized("Para ver la agenda completa hay que iniciar sesión como personal.");
            if (!await reservaService.TelefonoCoincide(clienteId.Value, TelefonoCliente))
                return Unauthorized(new Aplicada1.Core.Error(ReservaService.ErrorNoVerificado, "El número de cliente y el teléfono no coinciden."));
        }

        var desde = fecha?.ToDateTime(TimeOnly.MinValue);
        var hasta = desde?.AddDays(1);

        var result = await citaService.GetList(c =>
            (clienteId == null || c.ClienteId == clienteId) &&
            (barberoId == null || c.BarberoId == barberoId) &&
            (desde == null || (c.FechaInicio >= desde && c.FechaInicio < hasta)));

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>Agenda del barbero que inició sesión para un día (hoy si no se indica).</summary>
    [Authorize(Roles = RolesSistema.Barbero)]
    [HttpGet("mias")]
    public async Task<ActionResult<List<CitaDto>>> GetMisCitas(DateOnly? fecha)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId))
            return Unauthorized();

        var barbero = await barberoService.GetList(b => b.UsuarioId == usuarioId);
        if (barbero.IsFailure)
            return BadRequest(barbero.Error);

        var barberoId = barbero.Value!.FirstOrDefault()?.BarberoId;
        if (barberoId is null)
            return NotFound("Tu usuario no tiene un perfil de barbero.");

        var desde = (fecha ?? DateOnly.FromDateTime(HoraLocal.Ahora)).ToDateTime(TimeOnly.MinValue);
        var hasta = desde.AddDays(1);

        var result = await citaService.GetList(c =>
            c.BarberoId == barberoId && c.FechaInicio >= desde && c.FechaInicio < hasta);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Personal)]
    [HttpGet("{id}")]
    public async Task<ActionResult<CitaDto>> GetCita(int id)
    {
        var result = await citaService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    /// <summary>
    /// El personal crea citas libremente. Un cliente manda su teléfono en X-Cliente-Telefono;
    /// el servidor pone los precios, la duración y el estado Pendiente, y valida la hora.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CitaDto>> PostCita(CitaDto cita)
    {
        cita.CitaId = 0;

        var result = EsPersonal
            ? await citaService.Guardar(cita)
            : await reservaService.Reservar(cita, TelefonoCliente);

        if (result.IsFailure)
            return result.Error.Code == ReservaService.ErrorNoVerificado
                ? Unauthorized(result.Error)
                : BadRequest(result.Error);

        var creada = await citaService.Buscar(cita.CitaId);

        return CreatedAtAction(nameof(GetCita), new { id = cita.CitaId }, creada.Value);
    }

    /// <summary>El cliente cancela una cita suya; manda su teléfono en X-Cliente-Telefono.</summary>
    [HttpPost("{id}/cancelar")]
    public async Task<IActionResult> CancelarCita(int id)
    {
        var result = await reservaService.Cancelar(id, TelefonoCliente);

        if (result.IsFailure)
            return result.Error.Code == ReservaService.ErrorNoVerificado
                ? Unauthorized(result.Error)
                : BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return NoContent();
    }

    /// <summary>El barbero marca una de sus citas (Confirmada, EnProceso, Completada o NoShow).</summary>
    [Authorize(Roles = RolesSistema.Barbero)]
    [HttpPut("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, CambioEstadoDto cambio)
    {
        if (await BarberoDelUsuario() is not { } barberoId)
            return NotFound("Tu usuario no tiene un perfil de barbero.");

        var result = await citaService.CambiarEstadoPorBarbero(id, barberoId, cambio.EstadoCitaId);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return NoContent();
    }

    private async Task<int?> BarberoDelUsuario()
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId))
            return null;

        var barbero = await barberoService.GetList(b => b.UsuarioId == usuarioId);
        return barbero.IsSuccess ? barbero.Value!.FirstOrDefault()?.BarberoId : null;
    }

    [Authorize(Roles = RolesSistema.Personal)]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCita(int id, CitaDto cita)
    {
        if (id != cita.CitaId)
            return BadRequest("El id de la URL no coincide con el de la cita.");

        var existe = await citaService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await citaService.Guardar(cita);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    [Authorize(Roles = RolesSistema.Personal)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCita(int id)
    {
        var existe = await citaService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await citaService.Eliminar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }
}
