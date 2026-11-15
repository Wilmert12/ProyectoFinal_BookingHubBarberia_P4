using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Application.Mapeos;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.Application.Services;

public class CatalogoItemService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<CatalogoItemDto, int>
{
    public async Task<Result> Guardar(CatalogoItemDto catalogoItem)
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

    private async Task<bool> Insertar(CatalogoItemDto catalogoItemDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var catalogoItem = catalogoItemDto.Adapt<CatalogoItem>(MapeoConfig.Config);
        contexto.CatalogoItems.Add(catalogoItem);
        var inserto = await contexto.SaveChangesAsync() > 0;
        catalogoItemDto.CatalogoItemId = catalogoItem.CatalogoItemId;
        return inserto;
    }

    private async Task<bool> Modificar(CatalogoItemDto catalogoItemDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.CatalogoItems
            .Where(i => i.CatalogoItemId == catalogoItemDto.CatalogoItemId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.Nombre, catalogoItemDto.Nombre)
                .SetProperty(i => i.Descripcion, catalogoItemDto.Descripcion)
                .SetProperty(i => i.EsServicio, catalogoItemDto.EsServicio)
                .SetProperty(i => i.Precio, catalogoItemDto.Precio)
                .SetProperty(i => i.DuracionMin, catalogoItemDto.DuracionMin)
                .SetProperty(i => i.Activo, catalogoItemDto.Activo)) > 0;
    }

    public async Task<Result<CatalogoItemDto?>> Buscar(int catalogoItemId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.CatalogoItems
            .AsNoTracking()
            .Where(i => i.CatalogoItemId == catalogoItemId)
            .ProjectToType<CatalogoItemDto>(MapeoConfig.Config)
            .FirstOrDefaultAsync();
    }

    public async Task<Result<List<CatalogoItemDto>>> GetList(Expression<Func<CatalogoItemDto, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.CatalogoItems
            .AsNoTracking()
            .ProjectToType<CatalogoItemDto>(MapeoConfig.Config)
            .Where(criterio)
            .OrderBy(i => i.Nombre)
            .ToListAsync();
    }

    public async Task<Result> Eliminar(int catalogoItemId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var eliminado = await contexto.CatalogoItems
            .Where(i => i.CatalogoItemId == catalogoItemId)
            .ExecuteDeleteAsync() > 0;
        return Result.Success(eliminado);
    }
}
