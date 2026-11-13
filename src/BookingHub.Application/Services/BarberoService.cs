using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.Application.Services;

public class BarberoService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<BarberoDto, int>
{
    private static readonly Expression<Func<Barbero, BarberoDto>> ABarberoDto = b => new BarberoDto
    {
        BarberoId = b.BarberoId,
        UsuarioId = b.UsuarioId,
        Nombre = b.Usuario.Nombre,
        Apodo = b.Apodo,
        ColorHex = b.ColorHex,
        Activo = b.Activo
    };

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
        var barbero = new Barbero
        {
            UsuarioId = barberoDto.UsuarioId,
            Apodo = barberoDto.Apodo,
            Cedula = barberoDto.Cedula ?? string.Empty,
            ColorHex = barberoDto.ColorHex,
            Activo = barberoDto.Activo
        };
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
            .Select(ABarberoDto)
            .FirstOrDefaultAsync();
    }

    public async Task<Result<List<BarberoDto>>> GetList(Expression<Func<BarberoDto, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Barberos
            .AsNoTracking()
            .Select(ABarberoDto)
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
