using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitasController(
    CitaService citaService,
    BarberoService barberoService,
    ReservaCitaService reservaCitaService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CitaDto>>> GetCitas(int? clienteId, int? barberoId, DateOnly? fecha)
    {
        // La consulta antigua sin JWT se conserva para Blazor; cualquier JWT de cliente
        // queda restringido a su propio clienteId. Android nuevo usa /mias.
        if (User.IsInRole(RolesSistema.Cliente))
        {
            if (!int.TryParse(User.FindFirst("sub")?.Value, out var idCliente)) return Unauthorized();
            if (clienteId is not null && clienteId != idCliente) return Forbid();
            clienteId = idCliente;
        }

        if (clienteId is null && !User.IsInRole(RolesSistema.Admin) && !User.IsInRole(RolesSistema.Recepcion))
            return Unauthorized("Para ver la agenda completa hay que iniciar sesión como personal.");

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
    [Authorize(Roles = RolesSistema.Barbero + "," + RolesSistema.Cliente)]
    [HttpGet("mias")]
    public async Task<ActionResult<List<CitaDto>>> GetMisCitas(DateOnly? fecha, bool proximas = false)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId))
            return Unauthorized();

        if (User.IsInRole(RolesSistema.Cliente))
        {
            var desdeCliente = fecha?.ToDateTime(TimeOnly.MinValue);
            var hastaCliente = desdeCliente?.AddDays(1);
            var ahoraCliente = HoraLocal.Ahora;
            var clienteResult = await citaService.GetList(c => c.ClienteId == usuarioId &&
                (desdeCliente == null || (c.FechaInicio >= desdeCliente && c.FechaInicio < hastaCliente)) &&
                (!proximas || (c.FechaInicio >= ahoraCliente && c.Estado != "Cancelada" &&
                    c.Estado != "Completada" && c.Estado != "NoShow")));
            if (clienteResult.IsFailure) return BadRequest(clienteResult.Error);
            return Ok(clienteResult.Value);
        }

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

    /// <summary>Devuelve el historial del cliente autenticado en páginas acotadas.</summary>
    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpGet("mias/historial")]
    public async Task<ActionResult<PaginaDto<CitaDto>>> GetHistorial(int pagina = 1, int tamano = 10)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 20);
        var ahora = HoraLocal.Ahora;
        var estadosCerrados = new[] { "Cancelada", "Completada", "NoShow" };
        var result = await citaService.GetPage(
            c => c.ClienteId == clienteId && (c.FechaInicio < ahora || estadosCerrados.Contains(c.Estado ?? "")),
            pagina,
            tamano);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Personal + "," + RolesSistema.Barbero + "," + RolesSistema.Cliente)]
    [HttpGet("{id}")]
    public async Task<ActionResult<CitaDto>> GetCita(int id)
    {
        if (User.IsInRole(RolesSistema.Cliente))
        {
            if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
            if (!await reservaCitaService.PerteneceACliente(clienteId, id)) return NotFound();
        }
        else if (User.IsInRole(RolesSistema.Barbero))
        {
            if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId)) return Unauthorized();
            if (!await reservaCitaService.PerteneceAAgendaBarbero(usuarioId, id)) return NotFound();
        }

        var result = await citaService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<CitaDto>> PostCita(CitaDto cita)
    {
        cita.CitaId = 0;
        if (User.IsInRole(RolesSistema.Cliente))
        {
            if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
            var reserva = await reservaCitaService.Crear(clienteId, cita);
            if (reserva.IsFailure) return BadRequest(reserva.Error);
            return CreatedAtAction(nameof(GetCita), new { id = reserva.Value!.CitaId }, reserva.Value);
        }

        var result = await citaService.Guardar(cita);

        if (result.IsFailure)
            return BadRequest(result.Error);

        var creada = await citaService.Buscar(cita.CitaId);

        return CreatedAtAction(nameof(GetCita), new { id = cita.CitaId }, creada.Value);
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpPut("{id}/cancelar")]
    public async Task<ActionResult<CitaDto>> CancelarCita(int id, CancelarCitaRequest request)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        var result = await reservaCitaService.Cancelar(clienteId, id, request.Motivo);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpPut("{id}/reprogramar")]
    public async Task<ActionResult<CitaDto>> ReprogramarCita(int id, ReprogramarCitaRequest request)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        var result = await reservaCitaService.Reprogramar(clienteId, id, request.NuevaFechaInicio);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Barbero)]
    [HttpPut("{id}/estado")]
    public async Task<ActionResult<CitaDto>> CambiarEstadoBarbero(int id, CambiarEstadoCitaRequest request)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId)) return Unauthorized();
        var result = await reservaCitaService.CambiarEstadoBarbero(usuarioId, id, request.EstadoCitaId);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
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
