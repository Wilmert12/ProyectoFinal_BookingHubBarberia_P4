using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Services;

public class CatalogosService(
    IDbContextFactory<BookingHubContext> contextFactory
)
{
    public async Task<Result<List<EstadoCitaDto>>> GetEstadosCita()
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.EstadosCita
            .AsNoTracking()
            .OrderBy(e => e.EstadoCitaId)
            .Select(e => new EstadoCitaDto(e.EstadoCitaId, e.Descripcion, e.LiberaHorario))
            .ToListAsync();
    }

    public async Task<Result<List<MetodoPagoDto>>> GetMetodosPago()
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.MetodosPagos
            .AsNoTracking()
            .Where(m => m.Activo)
            .OrderBy(m => m.MetodoPagoId)
            .Select(m => new MetodoPagoDto(m.MetodoPagoId, m.Descripcion))
            .ToListAsync();
    }
}
