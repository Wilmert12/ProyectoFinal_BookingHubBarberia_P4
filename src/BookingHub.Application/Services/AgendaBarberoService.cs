using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BookingHub.Application.Services;

/// <summary>Permite a cada barbero administrar únicamente sus propios bloqueos de agenda.</summary>
public sealed class AgendaBarberoService(IDbContextFactory<BookingHubContext> contextFactory)
{
    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Descanso", "Vacaciones", "Bloqueo"
    };

    public async Task<Result<List<BloqueoAgendaDto>>> Listar(int usuarioId, DateOnly? desde, DateOnly? hasta)
    {
        if (hasta < desde)
            return Fallo<List<BloqueoAgendaDto>>("BLOQUEO_FECHAS", "El rango de fechas no es válido.");

        await using var db = await contextFactory.CreateDbContextAsync();
        var barberoId = await ObtenerBarberoId(db, usuarioId);
        if (barberoId is null)
            return Fallo<List<BloqueoAgendaDto>>("BARBERO_NO_EXISTE", "Tu usuario no tiene un perfil activo de barbero.");

        var inicio = desde?.ToDateTime(TimeOnly.MinValue);
        var fin = hasta?.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var bloqueos = await db.IndisponibilidadBarberos.AsNoTracking()
            .Include(i => i.TipoIndisponibilidad)
            .Where(i => i.BarberoId == barberoId &&
                (inicio == null || i.FechaFin > inicio) && (fin == null || i.FechaInicio < fin))
            .OrderBy(i => i.FechaInicio)
            .Select(i => new BloqueoAgendaDto(i.IndisponibilidadId, i.FechaInicio, i.FechaFin,
                i.TipoIndisponibilidad.Descripcion, i.Descripcion))
            .ToListAsync();
        return Result.Success(bloqueos);
    }

    public async Task<Result<BloqueoAgendaDto?>> Crear(int usuarioId, CrearBloqueoAgendaRequest request)
    {
        var tipoTexto = request.Tipo?.Trim();
        if (tipoTexto is null || !TiposPermitidos.Contains(tipoTexto))
            return Fallo<BloqueoAgendaDto?>("BLOQUEO_TIPO", "Usa uno de estos tipos: Descanso, Vacaciones o Bloqueo.");
        if (request.FechaFin <= request.FechaInicio || request.FechaFin - request.FechaInicio > TimeSpan.FromDays(90))
            return Fallo<BloqueoAgendaDto?>("BLOQUEO_FECHAS", "El bloqueo debe durar más de cero y hasta 90 días.");
        if (request.FechaInicio < HoraLocal.Ahora)
            return Fallo<BloqueoAgendaDto?>("BLOQUEO_FECHAS", "No se pueden crear bloqueos en el pasado.");

        await using var db = await contextFactory.CreateDbContextAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var barberoId = await ObtenerBarberoId(db, usuarioId);
        if (barberoId is null)
            return Fallo<BloqueoAgendaDto?>("BARBERO_NO_EXISTE", "Tu usuario no tiene un perfil activo de barbero.");

        var tieneCitas = await db.Citas.AnyAsync(c => c.BarberoId == barberoId && !c.EstadoCita.LiberaHorario &&
            c.FechaInicio < request.FechaFin && c.FechaFin > request.FechaInicio);
        if (tieneCitas)
            return Fallo<BloqueoAgendaDto?>("BLOQUEO_CITAS", "El rango incluye citas activas. Reprograma o cancela esas citas primero.");

        var tipo = await db.TiposIndisponibilidads.FirstOrDefaultAsync(t => t.Descripcion.ToLower() == tipoTexto.ToLower());
        if (tipo is null)
            return Fallo<BloqueoAgendaDto?>("BLOQUEO_CONFIGURACION", "Falta ejecutar la migración que configura tipos de bloqueo.");

        var bloqueo = new IndisponibilidadBarbero
        {
            BarberoId = barberoId.Value,
            TipoIndisponibilidadId = tipo.TipoIndisponibilidadId,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim()[..Math.Min(request.Descripcion.Trim().Length, 500)]
        };
        db.IndisponibilidadBarberos.Add(bloqueo);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return Result.Success<BloqueoAgendaDto?>(new BloqueoAgendaDto(bloqueo.IndisponibilidadId,
            bloqueo.FechaInicio, bloqueo.FechaFin, tipo.Descripcion, bloqueo.Descripcion));
    }

    public async Task<Result<bool>> Eliminar(int usuarioId, int bloqueoId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var barberoId = await ObtenerBarberoId(db, usuarioId);
        if (barberoId is null)
            return Fallo<bool>("BARBERO_NO_EXISTE", "Tu usuario no tiene un perfil activo de barbero.");

        var bloqueo = await db.IndisponibilidadBarberos.FirstOrDefaultAsync(i =>
            i.IndisponibilidadId == bloqueoId && i.BarberoId == barberoId);
        if (bloqueo is null)
            return Fallo<bool>("BLOQUEO_NO_EXISTE", "No se encontró ese bloqueo en tu agenda.");

        db.IndisponibilidadBarberos.Remove(bloqueo);
        await db.SaveChangesAsync();
        return Result.Success(true);
    }

    private static Task<int?> ObtenerBarberoId(BookingHubContext db, int usuarioId) => db.Barberos
        .Where(b => b.UsuarioId == usuarioId && b.Activo)
        .Select(b => (int?)b.BarberoId)
        .FirstOrDefaultAsync();

    private static Result<T> Fallo<T>(string codigo, string mensaje) => Result.Failure<T>(new Error(codigo, mensaje));
}
