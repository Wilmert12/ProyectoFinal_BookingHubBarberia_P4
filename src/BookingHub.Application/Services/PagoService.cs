using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Application.Mapeos;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.Application.Services;

public class PagoService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<PagoDto, int>
{
    public async Task<Result> Guardar(PagoDto pago)
    {
        if (pago.Monto <= 0)
            return Result.Failure(new Error("PAGO_MONTO", "El monto del pago debe ser mayor que 0."));
        if (pago.Propina < 0)
            return Result.Failure(new Error("PAGO_PROPINA", "La propina no puede ser negativa."));

        if (pago.FechaPago == default)
            pago.FechaPago = DateTime.Now;

        if (!await Existe(pago.PagoId))
        {
            var inserto = await Insertar(pago);
            return Result.Success(inserto);
        }
        else
        {
            var modifico = await Modificar(pago);
            return Result.Success(modifico);
        }
    }

    private async Task<bool> Existe(int pagoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Pagos
            .AnyAsync(p => p.PagoId == pagoId);
    }

    private async Task<bool> Insertar(PagoDto pagoDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var pago = pagoDto.Adapt<Pago>(MapeoConfig.Config);
        contexto.Pagos.Add(pago);
        var inserto = await contexto.SaveChangesAsync() > 0;
        pagoDto.PagoId = pago.PagoId;
        return inserto;
    }

    private async Task<bool> Modificar(PagoDto pagoDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Pagos
            .Where(p => p.PagoId == pagoDto.PagoId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.CitaId, pagoDto.CitaId)
                .SetProperty(p => p.MetodoPagoId, pagoDto.MetodoPagoId)
                .SetProperty(p => p.Monto, pagoDto.Monto)
                .SetProperty(p => p.Propina, pagoDto.Propina)
                .SetProperty(p => p.Referencia, pagoDto.Referencia)
                .SetProperty(p => p.FechaPago, pagoDto.FechaPago)) > 0;
    }

    public async Task<Result<PagoDto?>> Buscar(int pagoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Pagos
            .AsNoTracking()
            .Where(p => p.PagoId == pagoId)
            .ProjectToType<PagoDto>(MapeoConfig.Config)
            .FirstOrDefaultAsync();
    }

    public async Task<Result<List<PagoDto>>> GetList(Expression<Func<PagoDto, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Pagos
            .AsNoTracking()
            .ProjectToType<PagoDto>(MapeoConfig.Config)
            .Where(criterio)
            .OrderBy(p => p.FechaPago)
            .ToListAsync();
    }

    public async Task<Result> Eliminar(int pagoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var eliminado = await contexto.Pagos
            .Where(p => p.PagoId == pagoId)
            .ExecuteDeleteAsync() > 0;
        return Result.Success(eliminado);
    }
}
