using Aplicada1.Core;
using BookingHub.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.API.Services;

public class CatalogoItemService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<CatalogoItem, int>
{

    public async Task<Result> Guardar(CatalogoItem catalogoItem)
    {
        if (!await Existe(catalogoItem.CatalogoItemId))
        {
            var inserto = await Insertar(catalogoItem);
            return Result.Success(inserto);
        }
        else
        {
            var modifico = await Modificar(catalogoItem);
            return Result.Success(modifico);
        }
    }

    private async Task<bool> Existe(int catalogoItemId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.CatalogoItems
            .AnyAsync(i => i.CatalogoItemId == catalogoItemId);
    }

    private async Task<bool> Insertar(CatalogoItem catalogoItem)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        contexto.CatalogoItems.Add(catalogoItem);
        return await contexto.SaveChangesAsync() > 0;
    }

    private async Task<bool> Modificar(CatalogoItem catalogoItem)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        contexto.Update(catalogoItem);
        return await contexto
            .SaveChangesAsync() > 0;
    }

    public async Task<Result<CatalogoItem?>> Buscar(int catalogoItemId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.CatalogoItems
            .FirstOrDefaultAsync(i => i.CatalogoItemId == catalogoItemId);
    }

    public async Task<Result<List<CatalogoItem>>> GetList(Expression<Func<CatalogoItem, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.CatalogoItems
            .Where(criterio)
            .OrderBy(i => i.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Result> Eliminar(int catalogoItemId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var eliminado = await contexto.CatalogoItems
            .Where(i => i.CatalogoItemId == catalogoItemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Activo, false)) > 0;
        return Result.Success(eliminado);
    }
}
