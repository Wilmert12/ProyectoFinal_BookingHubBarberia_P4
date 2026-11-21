using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Services;

/// <summary>Horarios semanales y días libres de los barberos.</summary>
public class AgendaBarberoService(IDbContextFactory<BookingHubContext> contextFactory)
{
    private static readonly string[] TiposPorDefecto = ["Día libre", "Vacaciones", "Enfermedad", "Feriado", "Otro"];

    public async Task<Result<List<HorarioDto>?>> GetHorarios(int barberoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        if (!await contexto.Barberos.AnyAsync(b => b.BarberoId == barberoId))
            return Result.Success<List<HorarioDto>?>(null);

        var horarios = await contexto.HorariosBarberos
            .AsNoTracking()
            .Where(h => h.BarberoId == barberoId && h.Activo)
            .OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio)
            .Select(h => new HorarioDto(h.DiaSemana, h.HoraInicio, h.HoraFin))
            .ToListAsync();
        return Result.Success<List<HorarioDto>?>(horarios);
    }

    /// <summary>
    /// Reemplaza la semana completa del barbero. Una lista vacía vuelve al horario por defecto
    /// (lunes a sábado, 9:00 a 18:00).
    /// </summary>
    public async Task<Result> GuardarHorarios(int barberoId, List<HorarioDto> horarios)
    {
        foreach (var h in horarios)
        {
            if (h.DiaSemana > 6)
                return Result.Failure(new Error("HORARIO_DIA", "El día de la semana debe estar entre 0 (domingo) y 6 (sábado)."));
            if (h.HoraFin <= h.HoraInicio)
                return Result.Failure(new Error("HORARIO_HORAS", "La hora de salida debe ser posterior a la de entrada."));
        }

        var choca = horarios
            .GroupBy(h => h.DiaSemana)
            .Any(dia => dia.OrderBy(h => h.HoraInicio).Zip(dia.OrderBy(h => h.HoraInicio).Skip(1)).Any(par => par.Second.HoraInicio < par.First.HoraFin));
        if (choca)
            return Result.Failure(new Error("HORARIO_CHOQUE", "Hay bloques que se cruzan en el mismo día."));

        await using var contexto = await contextFactory.CreateDbContextAsync();
        if (!await contexto.Barberos.AnyAsync(b => b.BarberoId == barberoId))
            return Result.Failure(new Error("BARBERO_NO_EXISTE", "El barbero no existe."));

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        await contexto.HorariosBarberos.Where(h => h.BarberoId == barberoId).ExecuteDeleteAsync();
        contexto.HorariosBarberos.AddRange(horarios.Select(h => new HorariosBarbero
        {
            BarberoId = barberoId,
            DiaSemana = h.DiaSemana,
            HoraInicio = h.HoraInicio,
            HoraFin = h.HoraFin,
            Activo = true
        }));
        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();
        return Result.Success(true);
    }

    public async Task<Result<List<TipoIndisponibilidadDto>>> GetTipos()
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        // La tabla de tipos no viene en los datos iniciales; se llena la primera vez.
        if (!await contexto.TiposIndisponibilidads.AnyAsync())
        {
            contexto.TiposIndisponibilidads.AddRange(TiposPorDefecto.Select(t => new TiposIndisponibilidad { Descripcion = t }));
            await contexto.SaveChangesAsync();
        }

        return await contexto.TiposIndisponibilidads
            .AsNoTracking()
            .OrderBy(t => t.TipoIndisponibilidadId)
            .Select(t => new TipoIndisponibilidadDto(t.TipoIndisponibilidadId, t.Descripcion))
            .ToListAsync();
    }

    /// <summary>Días libres que todavía no terminan (o desde la fecha indicada).</summary>
    public async Task<Result<List<IndisponibilidadDto>>> GetIndisponibilidades(DateTime desde, int? barberoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.IndisponibilidadBarberos
            .AsNoTracking()
            .Where(i => i.FechaFin > desde && (barberoId == null || i.BarberoId == barberoId || i.BarberoId == null))
            .OrderBy(i => i.FechaInicio)
            .Select(i => new IndisponibilidadDto
            {
                IndisponibilidadId = i.IndisponibilidadId,
                BarberoId = i.BarberoId,
                BarberoNombre = i.Barbero != null ? i.Barbero.Usuario.Nombre : null,
                TipoIndisponibilidadId = i.TipoIndisponibilidadId,
                Tipo = i.TipoIndisponibilidad.Descripcion,
                Descripcion = i.Descripcion,
                FechaInicio = i.FechaInicio,
                FechaFin = i.FechaFin
            })
            .ToListAsync();
    }

    public async Task<Result> CrearIndisponibilidad(IndisponibilidadDto dto)
    {
        if (dto.FechaFin <= dto.FechaInicio)
            return Result.Failure(new Error("INDISPONIBILIDAD_FECHAS", "La fecha de fin debe ser posterior a la de inicio."));

        await using var contexto = await contextFactory.CreateDbContextAsync();
        if (dto.BarberoId is { } barberoId && !await contexto.Barberos.AnyAsync(b => b.BarberoId == barberoId))
            return Result.Failure(new Error("BARBERO_NO_EXISTE", "El barbero no existe."));
        if (!await contexto.TiposIndisponibilidads.AnyAsync(t => t.TipoIndisponibilidadId == dto.TipoIndisponibilidadId))
            return Result.Failure(new Error("INDISPONIBILIDAD_TIPO", "Elige el tipo de día libre."));

        var descripcion = dto.Descripcion?.Trim();
        var entidad = new IndisponibilidadBarbero
        {
            BarberoId = dto.BarberoId,
            TipoIndisponibilidadId = dto.TipoIndisponibilidadId,
            Descripcion = string.IsNullOrEmpty(descripcion) ? null : descripcion[..Math.Min(descripcion.Length, 500)],
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin
        };
        contexto.IndisponibilidadBarberos.Add(entidad);
        await contexto.SaveChangesAsync();
        dto.IndisponibilidadId = entidad.IndisponibilidadId;
        return Result.Success(true);
    }

    public async Task<Result> EliminarIndisponibilidad(int id)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var eliminado = await contexto.IndisponibilidadBarberos
            .Where(i => i.IndisponibilidadId == id)
            .ExecuteDeleteAsync() > 0;
        return Result.Success(eliminado);
    }
}
