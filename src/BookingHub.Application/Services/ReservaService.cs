using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Services;

/// <summary>
/// Reglas para los clientes que usan la web o el celular sin iniciar sesión.
/// El cliente se identifica con su número de cliente y su teléfono.
/// </summary>
public class ReservaService(
    IDbContextFactory<BookingHubContext> contextFactory,
    CitaService citaService,
    DisponibilidadService disponibilidadService)
{
    public const string EncabezadoTelefono = "X-Cliente-Telefono";
    public const string ErrorNoVerificado = "CLIENTE_NO_VERIFICADO";

    private const string EstadoInicial = "Pendiente";
    private const int CantidadMaxima = 10;
    private const int DigitosMinimos = 7;

    public async Task<bool> TelefonoCoincide(int clienteId, string? telefono)
    {
        var digitos = SoloDigitos(telefono);
        if (digitos.Length < DigitosMinimos)
            return false;

        await using var contexto = await contextFactory.CreateDbContextAsync();
        var registrado = await contexto.Clientes
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId && c.Activo)
            .Select(c => c.Telefono)
            .FirstOrDefaultAsync();

        return registrado is not null && SoloDigitos(registrado) == digitos;
    }

    /// <summary>
    /// Crea la cita de un cliente. Los precios, la duración y el estado los pone el servidor,
    /// y la hora tiene que estar libre en la agenda del barbero.
    /// </summary>
    public async Task<Result> Reservar(CitaDto cita, string? telefono)
    {
        if (!await TelefonoCoincide(cita.ClienteId, telefono))
            return Result.Failure(new Error(ErrorNoVerificado, "El número de cliente y el teléfono no coinciden."));

        var pedidos = (cita.Detalles ?? [])
            .GroupBy(d => d.CatalogoItemId)
            .Select(g => new { CatalogoItemId = g.Key, Cantidad = g.Sum(d => d.Cantidad) })
            .ToList();
        if (pedidos.Count == 0)
            return Result.Failure(new Error("RESERVA_SIN_SERVICIO", "Elige al menos un servicio."));
        if (pedidos.Any(p => p.Cantidad is < 1 or > CantidadMaxima))
            return Result.Failure(new Error("DETALLE_CANTIDAD", $"La cantidad de cada artículo debe estar entre 1 y {CantidadMaxima}."));

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var ids = pedidos.Select(p => p.CatalogoItemId).ToList();
        var items = await contexto.CatalogoItems
            .AsNoTracking()
            .Where(i => ids.Contains(i.CatalogoItemId) && i.Activo)
            .Select(i => new { i.CatalogoItemId, i.Nombre, i.EsServicio, i.Precio, i.DuracionMin })
            .ToListAsync();
        if (items.Count != ids.Count)
            return Result.Failure(new Error("RESERVA_ITEM", "Uno de los servicios elegidos ya no está disponible."));
        if (!items.Any(i => i.EsServicio))
            return Result.Failure(new Error("RESERVA_SIN_SERVICIO", "Elige al menos un servicio."));

        var estadoId = await contexto.EstadosCita
            .Where(e => e.Descripcion == EstadoInicial)
            .Select(e => (int?)e.EstadoCitaId)
            .FirstOrDefaultAsync();
        if (estadoId is null)
            return Result.Failure(new Error("RESERVA_ESTADO", $"No existe el estado {EstadoInicial}."));

        var duracion = pedidos.Sum(p =>
        {
            var item = items.First(i => i.CatalogoItemId == p.CatalogoItemId);
            return item.EsServicio ? (item.DuracionMin ?? 30) * p.Cantidad : 0;
        });

        // La hora tiene que salir en la disponibilidad: horario del barbero, sin choques y en el futuro.
        var inicio = cita.FechaInicio.AddSeconds(-cita.FechaInicio.Second).AddMilliseconds(-cita.FechaInicio.Millisecond);
        var disponibilidad = await disponibilidadService.Calcular(cita.BarberoId, DateOnly.FromDateTime(inicio), duracion);
        if (disponibilidad.IsFailure)
            return Result.Failure(disponibilidad.Error);
        if (disponibilidad.Value is not { } agenda)
            return Result.Failure(new Error("RESERVA_BARBERO", "El barbero elegido no existe."));
        if (!agenda.Horarios.Any(h => h.Inicio == inicio))
            return Result.Failure(new Error("CITA_CHOQUE", "Esa hora ya no está disponible. Elige otra."));

        var notas = cita.Notas?.Trim();
        cita.CitaId = 0;
        cita.EstadoCitaId = estadoId.Value;
        cita.FechaInicio = inicio;
        cita.FechaFin = inicio.AddMinutes(duracion);
        cita.Notas = string.IsNullOrEmpty(notas) ? null : notas[..Math.Min(notas.Length, 500)];
        cita.Detalles = pedidos.Select(p => new CitaDetalleDto
        {
            CatalogoItemId = p.CatalogoItemId,
            Cantidad = p.Cantidad,
            PrecioUnitario = items.First(i => i.CatalogoItemId == p.CatalogoItemId).Precio,
            Descuento = 0
        }).ToList();

        return await citaService.Guardar(cita);
    }

    /// <summary>
    /// El cliente cancela una cita suya que todavía no empieza.
    /// Devuelve null si la cita no existe.
    /// </summary>
    public async Task<Result<bool?>> Cancelar(int citaId, string? telefono)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var cita = await contexto.Citas
            .AsNoTracking()
            .Where(c => c.CitaId == citaId)
            .Select(c => new { c.ClienteId, c.FechaInicio, c.EstadoCita.Descripcion, c.EstadoCita.LiberaHorario })
            .FirstOrDefaultAsync();
        if (cita is null)
            return Result.Success<bool?>(null);

        if (!await TelefonoCoincide(cita.ClienteId, telefono))
            return Result.Failure<bool?>(new Error(ErrorNoVerificado, "El número de cliente y el teléfono no coinciden."));
        if (cita.LiberaHorario)
            return Result.Failure<bool?>(new Error("CITA_CANCELADA", "La cita ya estaba cancelada."));
        if (cita.Descripcion is not ("Pendiente" or "Confirmada") || cita.FechaInicio <= HoraLocal.Ahora)
            return Result.Failure<bool?>(new Error("CITA_NO_CANCELABLE", "Solo se pueden cancelar citas que todavía no empiezan."));

        var canceladaId = await contexto.EstadosCita
            .Where(e => e.Descripcion == "Cancelada")
            .Select(e => (int?)e.EstadoCitaId)
            .FirstOrDefaultAsync();
        if (canceladaId is null)
            return Result.Failure<bool?>(new Error("RESERVA_ESTADO", "No existe el estado Cancelada."));

        await contexto.Citas
            .Where(c => c.CitaId == citaId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.EstadoCitaId, canceladaId.Value));
        return Result.Success<bool?>(true);
    }

    /// <summary>
    /// Devuelve el número del cliente activo cuyo teléfono y nombre coinciden
    /// (sin importar mayúsculas, tildes ni espacios de más). Null si no hay uno.
    /// </summary>
    public async Task<NumeroClienteDto?> RecuperarNumero(string? nombre, string? telefono)
    {
        var digitos = SoloDigitos(telefono);
        var buscado = Normalizar(nombre);
        if (digitos.Length < DigitosMinimos || buscado.Length == 0)
            return null;

        await using var contexto = await contextFactory.CreateDbContextAsync();
        var candidatos = await contexto.Clientes
            .AsNoTracking()
            .Where(c => c.Activo)
            .Select(c => new { c.ClienteId, c.Nombre, c.Telefono })
            .ToListAsync();

        var cliente = candidatos.FirstOrDefault(c => SoloDigitos(c.Telefono) == digitos && Normalizar(c.Nombre) == buscado);
        return cliente is null ? null : new NumeroClienteDto(cliente.ClienteId, cliente.Nombre);
    }

    private static string Normalizar(string? texto)
    {
        var sinTildes = new string((texto ?? string.Empty)
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());
        return string.Join(' ', sinTildes.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string SoloDigitos(string? texto) =>
        new((texto ?? string.Empty).Where(char.IsDigit).ToArray());
}
