using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using BookingHub.Application.UnitTests.Infraestructura;

namespace BookingHub.Application.UnitTests.Services;

public class PagoYReporteTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _baseDeDatos = new();
    private readonly CitaService _citaService;
    private readonly PagoService _pagoService;
    private readonly ReporteService _reporteService;
    private readonly CatalogosService _catalogosService;
    private readonly int _corteId;
    private readonly int _ceraId;

    public PagoYReporteTests()
    {
        _citaService = new CitaService(_baseDeDatos.Factory);
        _pagoService = new PagoService(_baseDeDatos.Factory);
        _reporteService = new ReporteService(_baseDeDatos.Factory);
        _catalogosService = new CatalogosService(_baseDeDatos.Factory);
        _corteId = _baseDeDatos.CrearItem("Corte", 8m);
        _ceraId = _baseDeDatos.CrearItem("Cera Mate", 10m, esServicio: false);
    }

    private async Task<int> CitaCobrada(int barberoId, int metodoPagoId, DateTime fecha, decimal propina, params CitaDetalleDto[] detalles)
    {
        var cita = new CitaDto
        {
            ClienteId = 1,
            BarberoId = barberoId,
            EstadoCitaId = 3,
            FechaInicio = fecha,
            FechaFin = fecha.AddMinutes(30),
            Detalles = detalles.ToList()
        };
        await _citaService.Guardar(cita);
        await _pagoService.Guardar(new PagoDto
        {
            CitaId = cita.CitaId,
            MetodoPagoId = metodoPagoId,
            Monto = cita.Total,
            Propina = propina,
            FechaPago = fecha.AddMinutes(30)
        });
        return cita.CitaId;
    }

    [Fact]
    public async Task Guardar_PagoNuevo_AsignaIdYFechaYDevuelveMetodoYCliente()
    {
        var citaId = await CitaCobrada(1, 2, new DateTime(2026, 11, 20, 9, 0, 0), 0m,
            new CitaDetalleDto { CatalogoItemId = _corteId, Cantidad = 1, PrecioUnitario = 8m });
        var pago = new PagoDto { CitaId = citaId, MetodoPagoId = 1, Monto = 5m };

        var result = await _pagoService.Guardar(pago);

        Assert.True(result.IsSuccess);
        Assert.True(pago.PagoId > 0);
        Assert.NotEqual(default, pago.FechaPago);
        var pagos = (await _pagoService.GetList(p => p.CitaId == citaId)).Value!;
        Assert.Equal(2, pagos.Count);
        Assert.Contains(pagos, p => p.MetodoPago == "Tarjeta" && p.ClienteNombre == "Cliente Demo" && p.BarberoNombre == "Carlos Pérez");
    }

    [Fact]
    public async Task Guardar_PagoConMontoCero_Falla()
    {
        var result = await _pagoService.Guardar(new PagoDto { CitaId = 1, MetodoPagoId = 1, Monto = 0m });

        Assert.True(result.IsFailure);
        Assert.Equal("PAGO_MONTO", result.Error.Code);
    }

    [Fact]
    public async Task GetIngresos_SumaPorMetodoBarberoYServicioDentroDelRango()
    {
        var dia = new DateTime(2026, 11, 20, 9, 0, 0);
        await CitaCobrada(1, 1, dia, 2m,
            new CitaDetalleDto { CatalogoItemId = _corteId, Cantidad = 1, PrecioUnitario = 8m });
        await CitaCobrada(2, 2, dia.AddHours(2), 0m,
            new CitaDetalleDto { CatalogoItemId = _corteId, Cantidad = 1, PrecioUnitario = 8m },
            new CitaDetalleDto { CatalogoItemId = _ceraId, Cantidad = 1, PrecioUnitario = 10m });
        await CitaCobrada(1, 1, dia.AddDays(5), 0m,
            new CitaDetalleDto { CatalogoItemId = _corteId, Cantidad = 1, PrecioUnitario = 8m });

        var result = await _reporteService.GetIngresos(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 20));

        Assert.True(result.IsSuccess);
        var reporte = result.Value!;
        Assert.Equal(26m, reporte.TotalIngresos);
        Assert.Equal(2m, reporte.TotalPropinas);
        Assert.Equal(2, reporte.CantidadPagos);
        Assert.Equal(2, reporte.CitasCompletadas);
        Assert.Equal(8m, reporte.PorMetodo.Single(m => m.MetodoPago == "Efectivo").Total);
        Assert.Equal(18m, reporte.PorMetodo.Single(m => m.MetodoPago == "Tarjeta").Total);
        Assert.Equal(18m, reporte.PorBarbero.Single(b => b.Nombre == "Luis Gómez").Ingresos);
        var corte = reporte.PorServicio.Single(s => s.Nombre == "Corte");
        Assert.Equal(2, corte.Cantidad);
        Assert.Equal(16m, corte.Ingresos);
    }

    [Fact]
    public async Task GetIngresos_FechaFinalAnterior_Falla()
    {
        var result = await _reporteService.GetIngresos(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 19));

        Assert.True(result.IsFailure);
        Assert.Equal("REPORTE_FECHAS", result.Error.Code);
    }

    [Fact]
    public async Task GetMetodosPago_DevuelveSoloLosActivos()
    {
        var result = await _catalogosService.GetMetodosPago();

        Assert.True(result.IsSuccess);
        Assert.Equal(["Efectivo", "Tarjeta"], result.Value!.Select(m => m.Descripcion));
    }

    [Fact]
    public async Task GetEstadosCita_DevuelveLosEstadosEnOrden()
    {
        var result = await _catalogosService.GetEstadosCita();

        Assert.True(result.IsSuccess);
        Assert.Equal(["Pendiente", "Cancelada", "Completada"], result.Value!.Select(e => e.Descripcion));
    }

    public void Dispose() => _baseDeDatos.Dispose();
}
