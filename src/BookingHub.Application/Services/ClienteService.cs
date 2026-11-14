using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Application.Mapeos;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.Application.Services;

public class ClienteService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<ClienteDto, int>
{
    public async Task<Result> Guardar(ClienteDto cliente)
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

    private async Task<bool> Insertar(ClienteDto clienteDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var cliente = clienteDto.Adapt<Cliente>(MapeoConfig.Config);
        contexto.Clientes.Add(cliente);
        var inserto = await contexto.SaveChangesAsync() > 0;
        clienteDto.ClienteId = cliente.ClienteId;
        return inserto;
    }

    private async Task<bool> Modificar(ClienteDto clienteDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Clientes
            .Where(c => c.ClienteId == clienteDto.ClienteId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Nombre, clienteDto.Nombre)
                .SetProperty(c => c.Telefono, clienteDto.Telefono)
                .SetProperty(c => c.Email, clienteDto.Email)
                .SetProperty(c => c.Activo, clienteDto.Activo)) > 0;
    }

    public async Task<Result<ClienteDto?>> Buscar(int clienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Clientes
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .ProjectToType<ClienteDto>(MapeoConfig.Config)
            .FirstOrDefaultAsync();
    }

    public async Task<Result<List<ClienteDto>>> GetList(Expression<Func<ClienteDto, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Clientes
            .AsNoTracking()
            .ProjectToType<ClienteDto>(MapeoConfig.Config)
            .Where(criterio)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Result> Eliminar(int clienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var eliminado = await contexto.Clientes
            .Where(c => c.ClienteId == clienteId)
            .ExecuteDeleteAsync() > 0;
        return Result.Success(eliminado);
    }
}
