using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using BookingHub.Application.UnitTests.Infraestructura;

namespace BookingHub.Application.UnitTests.Services;

public class CitaDetalleTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _baseDeDatos = new();
    private readonly CitaService _citaService;
    private readonly PagoService _pagoService;
    private readonly int _corteId;
    private readonly int _ceraId;

    public CitaDetalleTests()
    {
        _citaService = new CitaService(_baseDeDatos.Factory);
        _pagoService = new PagoService(_baseDeDatos.Factory);
        _corteId = _baseDeDatos.CrearItem("Corte", 8m);
        _ceraId = _baseDeDatos.CrearItem("Cera Mate", 10m, esServicio: false);
    }

    private CitaDto NuevaCita(params CitaDetalleDto[] detalles) => new()
    {
        ClienteId = 1,
        BarberoId = 1,
        EstadoCitaId = 1,
        FechaInicio = new DateTime(2026, 11, 20, 10, 0, 0),
        FechaFin = new DateTime(2026, 11, 20, 10, 30, 0),
        Total = 0,
        Detalles = detalles.ToList()
    };

    [Fact]
    public async Task Guardar_CitaConDetalles_CalculaElTotalYDevuelveLosDetallesSinCitaId()
    {
        var cita = NuevaCita(
            new CitaDetalleDto { CatalogoItemId = _corteId, Cantidad = 1, PrecioUnitario = 8m },
            new CitaDetalleDto { CatalogoItemId = _ceraId, Cantidad = 2, PrecioUnitario = 10m, Descuento = 5m });

        var result = await _citaService.Guardar(cita);

        Assert.True(result.IsSuccess);
        var guardada = (await _citaService.Buscar(cita.CitaId)).Value!;
        Assert.Equal(23m, guardada.Total);
        Assert.Equal(2, guardada.Detalles!.Count);
        var cera = guardada.Detalles.Single(d => d.CatalogoItemId == _ceraId);
        Assert.Equal("Cera Mate", cera.Nombre);
        Assert.Equal(15m, cera.Subtotal);
    }

    [Fact]
    public async Task Guardar_ModificarConDetalles_ReemplazaLosDetallesAnteriores()
    {
        var cita = NuevaCita(new CitaDetalleDto { CatalogoItemId = _corteId, Cantidad = 1, PrecioUnitario = 8m });
        await _citaService.Guardar(cita);

        cita.Detalles = [new CitaDetalleDto { CatalogoItemId = _ceraId, Cantidad = 1, PrecioUnitario = 10m }];
        var result = await _citaService.Guardar(cita);

        Assert.True(result.IsSuccess);
        var guardada = (await _citaService.Buscar(cita.CitaId)).Value!;
        Assert.Single(guardada.Detalles!);
        Assert.Equal(_ceraId, guardada.Detalles![0].CatalogoItemId);
        Assert.Equal(10m, guardada.Total);
    }

    [Fact]
    public async Task Guardar_ModificarSinDetalles_ConservaLosDetalles()
    {
        var cita = NuevaCita(new CitaDetalleDto { CatalogoItemId = _corteId, Cantidad = 1, PrecioUnitario = 8m });
        await _citaService.Guardar(cita);

        cita.Detalles = null;
        cita.EstadoCitaId = 3;
        var result = await _citaService.Guardar(cita);

        Assert.True(result.IsSuccess);
        var guardada = (await _citaService.Buscar(cita.CitaId)).Value!;
        Assert.Equal("Completada", guardada.Estado);
        Assert.Single(guardada.Detalles!);
    }

    [Fact]
    public async Task Guardar_DescuentoMayorQueElSubtotal_Falla()
    {
        var cita = NuevaCita(new CitaDetalleDto { CatalogoItemId = _corteId, Cantidad = 1, PrecioUnitario = 8m, Descuento = 9m });

        var result = await _citaService.Guardar(cita);

        Assert.True(result.IsFailure);
        Assert.Equal("DETALLE_DESCUENTO", result.Error.Code);
    }

    [Fact]
    public async Task Eliminar_CitaConDetalles_BorraLaCitaYSusDetalles()
    {
        var cita = NuevaCita(new CitaDetalleDto { CatalogoItemId = _corteId, Cantidad = 1, PrecioUnitario = 8m });
        await _citaService.Guardar(cita);

        var result = await _citaService.Eliminar(cita.CitaId);

        Assert.True(result.IsSuccess);
        Assert.Null((await _citaService.Buscar(cita.CitaId)).Value);
    }

    [Fact]
    public async Task Eliminar_CitaConPagos_FallaYNoLaBorra()
    {
        var cita = NuevaCita(new CitaDetalleDto { CatalogoItemId = _corteId, Cantidad = 1, PrecioUnitario = 8m });
        await _citaService.Guardar(cita);
        await _pagoService.Guardar(new PagoDto { CitaId = cita.CitaId, MetodoPagoId = 1, Monto = 8m });

        var result = await _citaService.Eliminar(cita.CitaId);

        Assert.True(result.IsFailure);
        Assert.Equal("CITA_CON_PAGOS", result.Error.Code);
        Assert.NotNull((await _citaService.Buscar(cita.CitaId)).Value);
    }

    public void Dispose() => _baseDeDatos.Dispose();
}
