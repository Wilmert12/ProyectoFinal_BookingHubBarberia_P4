using Aplicada1.Core;
using BookingHub.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.API.Services;

public class BarberoService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<Barbero, int>
{

    public async Task<Result> Guardar(Barbero barbero)
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

    private async Task<bool> Insertar(Barbero barbero)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        contexto.Barberos.Add(barbero);
        return await contexto.SaveChangesAsync() > 0;
    }

    private async Task<bool> Modificar(Barbero barbero)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        contexto.Update(barbero);
        return await contexto
            .SaveChangesAsync() > 0;
    }

    public async Task<Result<Barbero?>> Buscar(int barberoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Barberos
            .Include(b => b.Usuario)
            .FirstOrDefaultAsync(b => b.BarberoId == barberoId);
    }

    public async Task<Result<List<Barbero>>> GetList(Expression<Func<Barbero, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Barberos
            .Include(b => b.Usuario)
            .Where(criterio)
            .OrderBy(b => b.Usuario.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    // Baja lógica: no se borra el registro porque puede tener citas en su historial
    public async Task<Result> Eliminar(int barberoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var eliminado = await contexto.Barberos
            .Where(b => b.BarberoId == barberoId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Activo, false)) > 0;
        return Result.Success(eliminado);
    }
}
