using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Application.Mapeos;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.Application.Services;

public class BarberoService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<BarberoDto, int>
{
    public async Task<Result> Guardar(BarberoDto barbero)
    {
        if (!await Existe(barbero.BarberoId))
        {
            var inserto = await Insertar(barbero);
            return Result.Success(inserto);
        }
        else
        {
            var modifico = await Modificar(barbero);
            return Result.Success(modifico);
        }
    }

    private async Task<bool> Existe(int barberoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Barberos
            .AnyAsync(b => b.BarberoId == barberoId);
    }

    private async Task<bool> Insertar(BarberoDto barberoDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var barbero = barberoDto.Adapt<Barbero>(MapeoConfig.Config);
        contexto.Barberos.Add(barbero);
        var inserto = await contexto.SaveChangesAsync() > 0;
        barberoDto.BarberoId = barbero.BarberoId;
        barberoDto.Cedula = null;
        return inserto;
    }

    private async Task<bool> Modificar(BarberoDto barberoDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var modifico = await contexto.Barberos
            .Where(b => b.BarberoId == barberoDto.BarberoId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.UsuarioId, barberoDto.UsuarioId)
                .SetProperty(b => b.Apodo, barberoDto.Apodo)
                .SetProperty(b => b.Cedula, b => barberoDto.Cedula ?? b.Cedula)
                .SetProperty(b => b.ColorHex, barberoDto.ColorHex)
                .SetProperty(b => b.Activo, barberoDto.Activo)) > 0;
        barberoDto.Cedula = null;
        return modifico;
    }

    public async Task<Result<BarberoDto?>> Buscar(int barberoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Barberos
            .AsNoTracking()
            .Where(b => b.BarberoId == barberoId)
            .ProjectToType<BarberoDto>(MapeoConfig.Config)
            .FirstOrDefaultAsync();
    }

    public async Task<Result<List<BarberoDto>>> GetList(Expression<Func<BarberoDto, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Barberos
            .AsNoTracking()
            .ProjectToType<BarberoDto>(MapeoConfig.Config)
            .Where(criterio)
            .OrderBy(b => b.Nombre)
            .ToListAsync();
    }

    public async Task<Result> Eliminar(int barberoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var eliminado = await contexto.Barberos
            .Where(b => b.BarberoId == barberoId)
            .ExecuteDeleteAsync() > 0;
        return Result.Success(eliminado);
    }
}
