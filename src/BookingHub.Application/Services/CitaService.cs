using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Application.Mapeos;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.Application.Services;

public class CitaService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<CitaDto, int>
{
    public async Task<Result> Guardar(CitaDto cita)
    {
        if (Validar(cita) is { } error)
            return Result.Failure(error);

        if (await ChocaConOtraCita(cita))
            return Result.Failure(new Error("CITA_CHOQUE", "El barbero ya tiene otra cita en ese horario."));

        if (cita.Detalles is { Count: > 0 })
            cita.Total = cita.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario - d.Descuento);

        if (!await Existe(cita.CitaId))
        {
            var inserto = await Insertar(cita);
            return Result.Success(inserto);
        }
        else
        {
            var modifico = await Modificar(cita);
            return Result.Success(modifico);
        }
    }

    private static Error? Validar(CitaDto cita)
    {
        if (cita.FechaFin <= cita.FechaInicio)
            return new Error("CITA_FECHAS", "La hora de fin debe ser posterior a la de inicio.");

        foreach (var detalle in cita.Detalles ?? [])
        {
            if (detalle.Cantidad <= 0)
                return new Error("DETALLE_CANTIDAD", "La cantidad de cada detalle debe ser mayor que 0.");
            if (detalle.PrecioUnitario < 0)
                return new Error("DETALLE_PRECIO", "El precio de un detalle no puede ser negativo.");
            if (detalle.Descuento < 0 || detalle.Descuento > detalle.Cantidad * detalle.PrecioUnitario)
                return new Error("DETALLE_DESCUENTO", "El descuento no puede ser negativo ni mayor que el subtotal.");
        }

        return null;
    }

    private async Task<bool> ChocaConOtraCita(CitaDto cita)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var liberaHorario = await contexto.EstadosCita
            .Where(e => e.EstadoCitaId == cita.EstadoCitaId)
            .Select(e => e.LiberaHorario)
            .FirstOrDefaultAsync();
        if (liberaHorario)
            return false;

        // Al editar sin mover la cita (por ejemplo, solo cambiar el estado) no se vuelve a validar.
        var actual = await contexto.Citas
            .AsNoTracking()
            .Where(c => c.CitaId == cita.CitaId)
            .Select(c => new { c.BarberoId, c.FechaInicio, c.FechaFin, c.EstadoCita.LiberaHorario })
            .FirstOrDefaultAsync();
        if (actual is not null && !actual.LiberaHorario
            && actual.BarberoId == cita.BarberoId
            && actual.FechaInicio == cita.FechaInicio
            && actual.FechaFin == cita.FechaFin)
            return false;

        return await contexto.Citas.AnyAsync(c =>
            c.CitaId != cita.CitaId
            && c.BarberoId == cita.BarberoId
            && !c.EstadoCita.LiberaHorario
            && c.FechaInicio < cita.FechaFin
            && c.FechaFin > cita.FechaInicio);
    }

    private async Task<bool> Existe(int citaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Citas
            .AnyAsync(c => c.CitaId == citaId);
    }

    private async Task<bool> Insertar(CitaDto citaDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var cita = citaDto.Adapt<Cita>(MapeoConfig.Config);
        cita.CitaDetalles = CrearDetalles(citaDto.Detalles);
        contexto.Citas.Add(cita);
        var inserto = await contexto.SaveChangesAsync() > 0;
        citaDto.CitaId = cita.CitaId;
        return inserto;
    }

    private async Task<bool> Modificar(CitaDto citaDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        await using var transaccion = await contexto.Database.BeginTransactionAsync();

        var modifico = await contexto.Citas
            .Where(c => c.CitaId == citaDto.CitaId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.ClienteId, citaDto.ClienteId)
                .SetProperty(c => c.BarberoId, citaDto.BarberoId)
                .SetProperty(c => c.EstadoCitaId, citaDto.EstadoCitaId)
                .SetProperty(c => c.FechaInicio, citaDto.FechaInicio)
                .SetProperty(c => c.FechaFin, citaDto.FechaFin)
                .SetProperty(c => c.Total, citaDto.Total)
                .SetProperty(c => c.Notas, citaDto.Notas)) > 0;

        if (citaDto.Detalles is not null)
        {
            await contexto.CitaDetalles
                .Where(d => d.CitaId == citaDto.CitaId)
                .ExecuteDeleteAsync();

            foreach (var detalle in CrearDetalles(citaDto.Detalles))
            {
                detalle.CitaId = citaDto.CitaId;
                contexto.CitaDetalles.Add(detalle);
            }
            await contexto.SaveChangesAsync();
        }

        await transaccion.CommitAsync();
        return modifico;
    }

    private static List<CitaDetalle> CrearDetalles(List<CitaDetalleDto>? detalles) =>
        (detalles ?? [])
            .Select(d => d.Adapt<CitaDetalle>(MapeoConfig.Config))
            .ToList();

    public async Task<Result<CitaDto?>> Buscar(int citaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Citas
            .AsNoTracking()
            .Where(c => c.CitaId == citaId)
            .ProjectToType<CitaDto>(MapeoConfig.Config)
            .FirstOrDefaultAsync();
    }

    public async Task<Result<List<CitaDto>>> GetList(Expression<Func<CitaDto, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Citas
            .AsNoTracking()
            .ProjectToType<CitaDto>(MapeoConfig.Config)
            .Where(criterio)
            .OrderBy(c => c.FechaInicio)
            .ToListAsync();
    }

    /// <summary>Estados que un barbero puede poner en sus propias citas.</summary>
    public static readonly string[] EstadosDelBarbero = ["Confirmada", "EnProceso", "Completada", "NoShow"];

    /// <summary>
    /// El barbero marca una de sus citas. No puede tocar citas canceladas ni de otro barbero.
    /// Devuelve null si la cita no existe o no es suya.
    /// </summary>
    public async Task<Result<bool?>> CambiarEstadoPorBarbero(int citaId, int barberoId, int estadoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var cita = await contexto.Citas
            .AsNoTracking()
            .Where(c => c.CitaId == citaId && c.BarberoId == barberoId)
            .Select(c => new { c.EstadoCita.Descripcion })
            .FirstOrDefaultAsync();
        if (cita is null)
            return Result.Success<bool?>(null);

        if (cita.Descripcion == "Cancelada")
            return Result.Failure<bool?>(new Error("CITA_CANCELADA", "La cita está cancelada."));

        var estado = await contexto.EstadosCita
            .Where(e => e.EstadoCitaId == estadoId)
            .Select(e => e.Descripcion)
            .FirstOrDefaultAsync();
        if (estado is null || !EstadosDelBarbero.Contains(estado))
            return Result.Failure<bool?>(new Error("ESTADO_NO_PERMITIDO", "Ese estado no se puede poner desde la agenda del barbero."));

        await contexto.Citas
            .Where(c => c.CitaId == citaId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.EstadoCitaId, estadoId));
        return Result.Success<bool?>(true);
    }

    public async Task<Result> Eliminar(int citaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        if (await contexto.Pagos.AnyAsync(p => p.CitaId == citaId))
            return Result.Failure(new Error("CITA_CON_PAGOS", "La cita tiene pagos registrados; cámbiala a Cancelada en lugar de eliminarla."));

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        await contexto.CitaDetalles
            .Where(d => d.CitaId == citaId)
            .ExecuteDeleteAsync();
        var eliminado = await contexto.Citas
            .Where(c => c.CitaId == citaId)
            .ExecuteDeleteAsync() > 0;
        await transaccion.CommitAsync();

        return Result.Success(eliminado);
    }
}
