using Aplicada1.Core;
using BookingHub.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.API.Services;

public class ClienteService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<Cliente, int>
{

    public async Task<Result> Guardar(Cliente cliente)
    {
        if (!await Existe(cliente.ClienteId))
        {
            var inserto = await Insertar(cliente);
            return Result.Success(inserto);
        }
        else
        {
            var modifico = await Modificar(cliente);
            return Result.Success(modifico);
        }
    }

    private async Task<bool> Existe(int clienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Clientes
            .AnyAsync(c => c.ClienteId == clienteId);
    }

    private async Task<bool> Insertar(Cliente cliente)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        contexto.Clientes.Add(cliente);
        return await contexto.SaveChangesAsync() > 0;
    }

    private async Task<bool> Modificar(Cliente cliente)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        contexto.Update(cliente);
        return await contexto
            .SaveChangesAsync() > 0;
    }

    public async Task<Result<Cliente?>> Buscar(int clienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Clientes
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId);
    }

    public async Task<Result<List<Cliente>>> GetList(Expression<Func<Cliente, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Clientes
            .Where(criterio)
            .OrderBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Result> Eliminar(int clienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var eliminado = await contexto.Clientes
            .Where(c => c.ClienteId == clienteId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Activo, false)) > 0;
        return Result.Success(eliminado);
    }
}
