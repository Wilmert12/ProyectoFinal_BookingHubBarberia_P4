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
        contexto.Citas.Add(cita);
        var inserto = await contexto.SaveChangesAsync() > 0;
        citaDto.CitaId = cita.CitaId;
        return inserto;
    }

    private async Task<bool> Modificar(CitaDto citaDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Citas
            .Where(c => c.CitaId == citaDto.CitaId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.ClienteId, citaDto.ClienteId)
                .SetProperty(c => c.BarberoId, citaDto.BarberoId)
                .SetProperty(c => c.EstadoCitaId, citaDto.EstadoCitaId)
                .SetProperty(c => c.FechaInicio, citaDto.FechaInicio)
                .SetProperty(c => c.FechaFin, citaDto.FechaFin)
                .SetProperty(c => c.Total, citaDto.Total)
                .SetProperty(c => c.Notas, citaDto.Notas)) > 0;
    }

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

    public async Task<Result> Eliminar(int citaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var eliminado = await contexto.Citas
            .Where(c => c.CitaId == citaId)
            .ExecuteDeleteAsync() > 0;
        return Result.Success(eliminado);
    }
}
