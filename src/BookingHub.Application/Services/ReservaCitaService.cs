using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;

namespace BookingHub.Application.Services;

/// <summary>Aplica las reglas de reservas de clientes dentro de transacciones serializables.</summary>
public sealed class ReservaCitaService(
    IDbContextFactory<BookingHubContext> contextFactory,
    IOptions<ReglasReservaOptions> reglas)
{
    private static readonly TimeOnly AperturaPorDefecto = new(9, 0);
    private static readonly TimeOnly CierrePorDefecto = new(18, 0);
    private ReglasReservaOptions Reglas => reglas.Value;

    public async Task<Result<CitaDto?>> Crear(int clienteId, CitaDto solicitud)
    {
        if (solicitud.Detalles is not { Count: > 0 })
            return Fallo<CitaDto?>("RESERVA_SERVICIOS", "Selecciona al menos un servicio.");
        if (solicitud.Detalles.Count > 10 || solicitud.Detalles.Any(d => d.Cantidad != 1))
            return Fallo<CitaDto?>("RESERVA_SERVICIOS", "Cada reserva admite hasta 10 servicios, una unidad por servicio.");

        await using var db = await contextFactory.CreateDbContextAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.Activo);
        if (cliente is null)
            return Fallo<CitaDto?>("CLIENTE_NO_EXISTE", "No se encontró una cuenta activa.");

        var barbero = await db.Barberos.FirstOrDefaultAsync(b => b.BarberoId == solicitud.BarberoId && b.Activo);
        if (barbero is null)
            return Fallo<CitaDto?>("BARBERO_NO_DISPONIBLE", "El barbero seleccionado no está disponible.");

        var ids = solicitud.Detalles.Select(d => d.CatalogoItemId).Distinct().ToList();
        if (ids.Count != solicitud.Detalles.Count)
            return Fallo<CitaDto?>("RESERVA_SERVICIOS", "No repitas el mismo servicio en la reserva.");

        var servicios = await db.CatalogoItems
            .Where(i => ids.Contains(i.CatalogoItemId) && i.Activo && i.EsServicio)
            .ToDictionaryAsync(i => i.CatalogoItemId);
        if (servicios.Count != ids.Count)
            return Fallo<CitaDto?>("RESERVA_SERVICIOS", "Uno de los servicios dejó de estar disponible.");

        var duracion = servicios.Values.Sum(s => s.DuracionMin ?? 30);
        if (duracion is < DisponibilidadService.DuracionMinima or > DisponibilidadService.DuracionMaxima)
            return Fallo<CitaDto?>("RESERVA_DURACION", "La duración combinada no es válida.");

        var inicio = solicitud.FechaInicio;
        var fin = inicio.AddMinutes(duracion);
        var horarioError = ValidarVentana(inicio);
        if (horarioError is not null) return Fallo<CitaDto?>("RESERVA_FECHA", horarioError);
        if (!EntraEnHorario(inicio, fin, await ObtenerHorarios(db, barbero.BarberoId, DateOnly.FromDateTime(inicio))))
            return Fallo<CitaDto?>("RESERVA_HORARIO", "La hora seleccionada queda fuera del horario del barbero.");

        var bloqueado = await EstaBloqueado(db, barbero.BarberoId, inicio, fin, citaExcluir: null);
        if (bloqueado)
            return Fallo<CitaDto?>("CITA_CHOQUE", "El horario ya fue tomado o está bloqueado. Actualiza la disponibilidad.");

        var citasActivas = await db.Citas.CountAsync(c => c.ClienteId == clienteId &&
            !c.EstadoCita.LiberaHorario && c.FechaInicio >= HoraLocal.Ahora);
        if (citasActivas >= Reglas.CitasActivasMaximas)
            return Fallo<CitaDto?>("RESERVA_LIMITE", $"Puedes mantener hasta {Reglas.CitasActivasMaximas} citas futuras activas.");

        var pendienteId = await ObtenerEstado(db, "Pendiente");
        if (pendienteId is null)
            return Fallo<CitaDto?>("RESERVA_CONFIGURACION", "El estado Pendiente no está configurado en la API.");

        var detalles = servicios.Values.OrderBy(s => ids.IndexOf(s.CatalogoItemId)).Select(s => new CitaDetalle
        {
            CatalogoItemId = s.CatalogoItemId,
            Cantidad = 1,
            PrecioUnitario = s.Precio,
            Descuento = 0
        }).ToList();
        var cita = new Cita
        {
            ClienteId = clienteId,
            BarberoId = barbero.BarberoId,
            EstadoCitaId = pendienteId.Value,
            FechaInicio = inicio,
            FechaFin = fin,
            Total = detalles.Sum(d => d.PrecioUnitario),
            Notas = Limitar(solicitud.Notas, 500),
            CitaDetalles = detalles
        };
        db.Citas.Add(cita);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return Result.Success<CitaDto?>(await ProyectarCita(cita.CitaId));
    }

    public async Task<Result<CitaDto?>> Cancelar(int clienteId, int citaId, string? motivo)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var cita = await db.Citas.Include(c => c.EstadoCita)
            .FirstOrDefaultAsync(c => c.CitaId == citaId && c.ClienteId == clienteId);
        if (cita is null) return Fallo<CitaDto?>("CITA_NO_EXISTE", "No se encontró la cita.");
        if (cita.EstadoCita.LiberaHorario)
            return Fallo<CitaDto?>("CITA_ESTADO", "La cita ya fue cancelada o cerrada.");
        if ((cita.FechaInicio - HoraLocal.Ahora).TotalHours < Reglas.HorasMinimasParaCancelar)
            return Fallo<CitaDto?>("CITA_CANCELACION_TARDE", $"Solo puedes cancelar con {Reglas.HorasMinimasParaCancelar} horas de anticipación.");

        var canceladaId = await ObtenerEstado(db, "Cancelada");
        if (canceladaId is null) return Fallo<CitaDto?>("RESERVA_CONFIGURACION", "El estado Cancelada no está configurado.");
        cita.EstadoCitaId = canceladaId.Value;
        var motivoLimpio = Limitar(motivo, 300);
        if (!string.IsNullOrWhiteSpace(motivoLimpio))
        {
            var texto = $"Cancelación del cliente: {motivoLimpio}";
            cita.Notas = string.IsNullOrWhiteSpace(cita.Notas)
                ? Limitar(texto, 500)
                : Limitar($"{cita.Notas}\n{texto}", 500);
        }
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return Result.Success<CitaDto?>(await ProyectarCita(citaId));
    }

    public async Task<Result<CitaDto?>> Reprogramar(int clienteId, int citaId, DateTime nuevoInicio)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var cita = await db.Citas.Include(c => c.EstadoCita).Include(c => c.CitaDetalles)
            .FirstOrDefaultAsync(c => c.CitaId == citaId && c.ClienteId == clienteId);
        if (cita is null) return Fallo<CitaDto?>("CITA_NO_EXISTE", "No se encontró la cita.");
        if (cita.EstadoCita.LiberaHorario)
            return Fallo<CitaDto?>("CITA_ESTADO", "Esta cita ya no se puede reprogramar.");
        if (cita.FechaInicio <= HoraLocal.Ahora)
            return Fallo<CitaDto?>("CITA_ESTADO", "No se puede reprogramar una cita que ya comenzó.");

        var ids = cita.CitaDetalles.Select(d => d.CatalogoItemId).ToList();
        var servicios = await db.CatalogoItems.Where(s => ids.Contains(s.CatalogoItemId) && s.Activo && s.EsServicio)
            .ToDictionaryAsync(s => s.CatalogoItemId);
        if (servicios.Count != ids.Count)
            return Fallo<CitaDto?>("RESERVA_SERVICIOS", "Un servicio de esta cita dejó de estar disponible.");
        var duracion = cita.CitaDetalles.Sum(d => servicios[d.CatalogoItemId].DuracionMin ?? 30);
        var nuevoFin = nuevoInicio.AddMinutes(duracion);
        var horarioError = ValidarVentana(nuevoInicio);
        if (horarioError is not null) return Fallo<CitaDto?>("RESERVA_FECHA", horarioError);
        if (!EntraEnHorario(nuevoInicio, nuevoFin, await ObtenerHorarios(db, cita.BarberoId, DateOnly.FromDateTime(nuevoInicio))))
            return Fallo<CitaDto?>("RESERVA_HORARIO", "La hora seleccionada queda fuera del horario del barbero.");
        if (await EstaBloqueado(db, cita.BarberoId, nuevoInicio, nuevoFin, citaId))
            return Fallo<CitaDto?>("CITA_CHOQUE", "El horario ya fue tomado o está bloqueado. Actualiza la disponibilidad.");

        cita.FechaInicio = nuevoInicio;
        cita.FechaFin = nuevoFin;
        foreach (var detalle in cita.CitaDetalles)
            detalle.PrecioUnitario = servicios[detalle.CatalogoItemId].Precio;
        cita.Total = cita.CitaDetalles.Sum(d => d.Cantidad * d.PrecioUnitario - d.Descuento);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return Result.Success<CitaDto?>(await ProyectarCita(citaId));
    }

    public async Task<Result<CitaDto?>> CambiarEstadoBarbero(int usuarioId, int citaId, int estadoNuevoId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var barberoId = await db.Barberos.Where(b => b.UsuarioId == usuarioId && b.Activo)
            .Select(b => (int?)b.BarberoId).FirstOrDefaultAsync();
        if (barberoId is null) return Fallo<CitaDto?>("BARBERO_NO_EXISTE", "Tu usuario no tiene un perfil activo de barbero.");

        var cita = await db.Citas.Include(c => c.EstadoCita)
            .FirstOrDefaultAsync(c => c.CitaId == citaId && c.BarberoId == barberoId);
        if (cita is null) return Fallo<CitaDto?>("CITA_NO_EXISTE", "No se encontró la cita en tu agenda.");
        var nuevo = await db.EstadosCita.FirstOrDefaultAsync(e => e.EstadoCitaId == estadoNuevoId);
        if (nuevo is null) return Fallo<CitaDto?>("CITA_ESTADO", "El estado de cita no es válido.");
        if (!TransicionPermitida(cita.EstadoCita.Descripcion, nuevo.Descripcion))
            return Fallo<CitaDto?>("CITA_TRANSICION", $"No se permite cambiar de {cita.EstadoCita.Descripcion} a {nuevo.Descripcion}.");

        cita.EstadoCitaId = nuevo.EstadoCitaId;
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return Result.Success<CitaDto?>(await ProyectarCita(citaId));
    }

    public async Task<bool> PerteneceACliente(int clienteId, int citaId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Citas.AnyAsync(c => c.CitaId == citaId && c.ClienteId == clienteId);
    }

    public async Task<bool> PerteneceAAgendaBarbero(int usuarioId, int citaId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Citas.AnyAsync(c => c.CitaId == citaId && c.Barbero.UsuarioId == usuarioId && c.Barbero.Activo);
    }

    private string? ValidarVentana(DateTime inicio)
    {
        var ahora = HoraLocal.Ahora;
        if (inicio < ahora.AddHours(Reglas.AnticipacionMinimaHoras))
            return $"Reserva con al menos {Reglas.AnticipacionMinimaHoras} horas de anticipación.";
        if (inicio > ahora.AddDays(Reglas.DiasMaximosAnticipacion))
            return $"Las reservas se permiten hasta {Reglas.DiasMaximosAnticipacion} días en adelante.";
        if (inicio.Second != 0 || inicio.Millisecond != 0 || inicio.Minute % DisponibilidadService.IntervaloMin != 0)
            return "Elige una hora disponible en intervalos de 30 minutos.";
        return null;
    }

    private static async Task<List<(TimeOnly Inicio, TimeOnly Fin)>> ObtenerHorarios(
        BookingHubContext db, int barberoId, DateOnly fecha)
    {
        var horarios = await db.HorariosBarberos.Where(h => h.BarberoId == barberoId && h.Activo)
            .Select(h => new { h.DiaSemana, h.HoraInicio, h.HoraFin }).ToListAsync();
        if (horarios.Count == 0)
            return fecha.DayOfWeek == DayOfWeek.Sunday ? [] : [(AperturaPorDefecto, CierrePorDefecto)];
        var dia = (byte)fecha.DayOfWeek;
        return horarios.Where(h => h.DiaSemana == dia && h.HoraFin > h.HoraInicio)
            .Select(h => (h.HoraInicio, h.HoraFin)).ToList();
    }

    private static bool EntraEnHorario(DateTime inicio, DateTime fin, List<(TimeOnly Inicio, TimeOnly Fin)> horarios)
    {
        if (DateOnly.FromDateTime(inicio) != DateOnly.FromDateTime(fin)) return false;
        var horaInicio = TimeOnly.FromDateTime(inicio);
        var horaFin = TimeOnly.FromDateTime(fin);
        return horarios.Any(h => horaInicio >= h.Inicio && horaFin <= h.Fin);
    }

    private async Task<bool> EstaBloqueado(BookingHubContext db, int barberoId, DateTime inicio, DateTime fin, int? citaExcluir) =>
        await db.IndisponibilidadBarberos.AnyAsync(i => (i.BarberoId == barberoId || i.BarberoId == null) &&
            i.FechaInicio < fin && i.FechaFin > inicio) ||
        await db.Citas.AnyAsync(c => c.BarberoId == barberoId && c.CitaId != (citaExcluir ?? 0) &&
            !c.EstadoCita.LiberaHorario && c.FechaInicio < fin && c.FechaFin > inicio);

    private async Task<int?> ObtenerEstado(BookingHubContext db, string nombre) =>
        await db.EstadosCita.Where(e => e.Descripcion.ToLower() == nombre.ToLower())
            .Select(e => (int?)e.EstadoCitaId).FirstOrDefaultAsync();

    private static bool TransicionPermitida(string actual, string nuevo)
    {
        var origen = NormalizarEstado(actual);
        var destino = NormalizarEstado(nuevo);
        return (origen, destino) switch
        {
            ("pendiente", "confirmada") => true,
            ("pendiente", "cancelada") => true,
            ("pendiente", "noshow") => true,
            ("confirmada", "enproceso") => true,
            ("confirmada", "cancelada") => true,
            ("confirmada", "noshow") => true,
            ("enproceso", "completada") => true,
            _ => false
        };
    }

    private static string NormalizarEstado(string estado) =>
        new(estado.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private async Task<CitaDto?> ProyectarCita(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var cita = await db.Citas.AsNoTracking().Include(c => c.EstadoCita)
            .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
            .Include(c => c.Cliente)
            .Include(c => c.CitaDetalles).ThenInclude(d => d.CatalogoItem)
            .Include(c => c.Resena)
            .FirstOrDefaultAsync(c => c.CitaId == id);
        if (cita is null) return null;
        return new CitaDto
        {
            CitaId = cita.CitaId,
            ClienteId = cita.ClienteId,
            ClienteNombre = cita.Cliente.Nombre,
            ClienteTelefono = cita.Cliente.Telefono,
            BarberoId = cita.BarberoId,
            BarberoNombre = cita.Barbero.Usuario.Nombre,
            EstadoCitaId = cita.EstadoCitaId,
            Estado = cita.EstadoCita.Descripcion,
            Resenada = cita.Resena is not null,
            FechaInicio = cita.FechaInicio,
            FechaFin = cita.FechaFin,
            Total = cita.Total,
            Notas = cita.Notas,
            Detalles = cita.CitaDetalles.Select(d => new CitaDetalleDto
            {
                CitaDetalleId = d.CitaDetalleId,
                CatalogoItemId = d.CatalogoItemId,
                Nombre = d.CatalogoItem.Nombre,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Descuento = d.Descuento,
                Subtotal = d.Cantidad * d.PrecioUnitario - d.Descuento
            }).ToList()
        };
    }

    private static string? Limitar(string? texto, int largo) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim()[..Math.Min(texto.Trim().Length, largo)];

    private static Result<T> Fallo<T>(string codigo, string mensaje) => Result.Failure<T>(new Error(codigo, mensaje));
}
