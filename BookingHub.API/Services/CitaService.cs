using Aplicada1.Core;
using BookingHub.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.API.Services;

public class CitaService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<Cita, int>
{

    public async Task<Result> Guardar(Cita cita)
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

    private async Task<bool> Insertar(Cita cita)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        contexto.Citas.Add(cita);
        return await contexto.SaveChangesAsync() > 0;
    }

    private async Task<bool> Modificar(Cita cita)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        contexto.Update(cita);
        return await contexto
            .SaveChangesAsync() > 0;
    }

    public async Task<Result<Cita?>> Buscar(int citaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Barbero)
            .Include(c => c.CitaDetalles)
            .FirstOrDefaultAsync(c => c.CitaId == citaId);
    }

    public async Task<Result<List<Cita>>> GetList(Expression<Func<Cita, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Barbero)
            .Where(criterio)
            .AsNoTracking()
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
