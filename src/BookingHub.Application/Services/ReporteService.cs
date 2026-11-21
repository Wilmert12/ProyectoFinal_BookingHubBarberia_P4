using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Services;

public class ReporteService(
    IDbContextFactory<BookingHubContext> contextFactory
)
{
    public async Task<Result<ReporteIngresosDto>> GetIngresos(DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde)
            return Result.Failure<ReporteIngresosDto>(new Error("REPORTE_FECHAS", "La fecha final no puede ser anterior a la inicial."));

        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pagos = await contexto.Pagos
            .AsNoTracking()
            .Where(p => p.FechaPago >= inicio && p.FechaPago < fin)
            .Select(p => new
            {
                p.CitaId,
                p.Monto,
                p.Propina,
                Metodo = p.MetodoPago.Descripcion,
                p.Cita.BarberoId,
                Barbero = p.Cita.Barbero.Usuario.Nombre
            })
            .ToListAsync();

        var citasPagadas = pagos.Select(p => p.CitaId).Distinct().ToList();

        var detalles = await contexto.CitaDetalles
            .AsNoTracking()
            .Where(d => citasPagadas.Contains(d.CitaId))
            .Select(d => new
            {
                d.CatalogoItemId,
                d.CatalogoItem.Nombre,
                d.CatalogoItem.EsServicio,
                d.Cantidad,
                d.PrecioUnitario,
                d.Descuento
            })
            .ToListAsync();

        var completadas = await contexto.Citas
            .AsNoTracking()
            .Where(c => c.EstadoCita.Descripcion == "Completada" && c.FechaInicio >= inicio && c.FechaInicio < fin)
            .Select(c => new { c.Total, Pagado = c.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m })
            .ToListAsync();
        var citasCompletadas = completadas.Count;

        // Completadas por el barbero pero que recepción todavía no cobró: no son ingresos aún.
        var porCobrar = completadas.Where(c => c.Pagado < c.Total).ToList();

        return new ReporteIngresosDto
        {
            Desde = desde,
            Hasta = hasta,
            TotalIngresos = pagos.Sum(p => p.Monto),
            TotalPropinas = pagos.Sum(p => p.Propina),
            TotalItbis = Impuestos.ItbisIncluido(pagos.Sum(p => p.Monto)),
            CantidadPagos = pagos.Count,
            CitasCompletadas = citasCompletadas,
            CitasPorCobrar = porCobrar.Count,
            PorCobrar = porCobrar.Sum(c => c.Total - c.Pagado),
            PorMetodo = pagos
                .GroupBy(p => p.Metodo)
                .Select(g => new IngresoPorMetodoDto(g.Key, g.Count(), g.Sum(p => p.Monto)))
                .OrderByDescending(m => m.Total)
                .ToList(),
            PorBarbero = pagos
                .GroupBy(p => new { p.BarberoId, p.Barbero })
                .Select(g => new IngresoPorBarberoDto(g.Key.BarberoId, g.Key.Barbero, g.Select(p => p.CitaId).Distinct().Count(), g.Sum(p => p.Monto)))
                .OrderByDescending(b => b.Ingresos)
                .ToList(),
            PorServicio = detalles
                .GroupBy(d => new { d.CatalogoItemId, d.Nombre, d.EsServicio })
                .Select(g => new IngresoPorServicioDto(
                    g.Key.CatalogoItemId,
                    g.Key.Nombre,
                    g.Key.EsServicio,
                    g.Sum(d => d.Cantidad),
                    g.Sum(d => d.Cantidad * d.PrecioUnitario - d.Descuento)))
                .OrderByDescending(s => s.Cantidad)
                .ToList()
        };
    }
}
