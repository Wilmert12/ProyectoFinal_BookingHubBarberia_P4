using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Services;

public class DisponibilidadService(IDbContextFactory<BookingHubContext> contextFactory)
{
    public const int IntervaloMin = 30;
    public const int DuracionMinima = 5;
    public const int DuracionMaxima = 480;

    // Horario por defecto cuando un barbero no tiene horarios cargados: lunes a sábado, 9:00 a 18:00.
    private static readonly TimeOnly AperturaPorDefecto = new(9, 0);
    private static readonly TimeOnly CierrePorDefecto = new(18, 0);

    public async Task<Result<DisponibilidadDto?>> Calcular(int barberoId, DateOnly fecha, int duracionMin)
    {
        if (duracionMin is < DuracionMinima or > DuracionMaxima)
            return Result.Failure<DisponibilidadDto?>(new Error("DURACION_INVALIDA",
                $"La duración debe estar entre {DuracionMinima} y {DuracionMaxima} minutos."));

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var barbero = await contexto.Barberos
            .AsNoTracking()
            .Where(b => b.BarberoId == barberoId)
            .Select(b => new { b.Activo })
            .FirstOrDefaultAsync();

        if (barbero is null)
            return Result.Success<DisponibilidadDto?>(null);

        var vacio = new DisponibilidadDto(barberoId, fecha, duracionMin, []);
        if (!barbero.Activo)
            return Result.Success<DisponibilidadDto?>(vacio);

        var bloques = await BloquesDeTrabajo(contexto, barberoId, fecha);

        var inicioDia = fecha.ToDateTime(TimeOnly.MinValue);
        var finDia = inicioDia.AddDays(1);

        var ocupados = await contexto.Citas
            .AsNoTracking()
            .Where(c => c.BarberoId == barberoId
                && !c.EstadoCita.LiberaHorario
                && c.FechaInicio < finDia
                && c.FechaFin > inicioDia)
            .Select(c => new { c.FechaInicio, c.FechaFin })
            .ToListAsync();

        // Una indisponibilidad sin barbero aplica a toda la barbería (feriado, cierre).
        var bloqueos = await contexto.IndisponibilidadBarberos
            .AsNoTracking()
            .Where(i => (i.BarberoId == barberoId || i.BarberoId == null)
                && i.FechaInicio < finDia
                && i.FechaFin > inicioDia)
            .Select(i => new { i.FechaInicio, i.FechaFin })
            .ToListAsync();

        var choques = ocupados.Select(o => (o.FechaInicio, o.FechaFin))
            .Concat(bloqueos.Select(b => (b.FechaInicio, b.FechaFin)))
            .ToList();

        var ahora = HoraLocal.Ahora;
        var libres = new List<HorarioLibreDto>();

        foreach (var (apertura, cierre) in bloques)
        {
            var finBloque = fecha.ToDateTime(cierre);
            for (var inicio = fecha.ToDateTime(apertura); inicio.AddMinutes(duracionMin) <= finBloque; inicio = inicio.AddMinutes(IntervaloMin))
            {
                var fin = inicio.AddMinutes(duracionMin);
                if (inicio <= ahora)
                    continue;
                if (choques.Any(c => c.FechaInicio < fin && c.FechaFin > inicio))
                    continue;
                libres.Add(new HorarioLibreDto(inicio, fin));
            }
        }

        return Result.Success<DisponibilidadDto?>(vacio with { Horarios = libres });
    }

    private static async Task<List<(TimeOnly Apertura, TimeOnly Cierre)>> BloquesDeTrabajo(
        BookingHubContext contexto, int barberoId, DateOnly fecha)
    {
        // diaSemana sigue a DayOfWeek de .NET: 0 = domingo ... 6 = sábado.
        var horarios = await contexto.HorariosBarberos
            .AsNoTracking()
            .Where(h => h.BarberoId == barberoId && h.Activo)
            .Select(h => new { h.DiaSemana, h.HoraInicio, h.HoraFin })
            .ToListAsync();

        if (horarios.Count == 0)
            return fecha.DayOfWeek == DayOfWeek.Sunday ? [] : [(AperturaPorDefecto, CierrePorDefecto)];

        var dia = (byte)fecha.DayOfWeek;
        return horarios
            .Where(h => h.DiaSemana == dia && h.HoraFin > h.HoraInicio)
            .OrderBy(h => h.HoraInicio)
            .Select(h => (h.HoraInicio, h.HoraFin))
            .ToList();
    }
}
