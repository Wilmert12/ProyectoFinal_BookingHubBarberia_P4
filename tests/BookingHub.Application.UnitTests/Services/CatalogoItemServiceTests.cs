using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using BookingHub.Application.UnitTests.Infraestructura;

namespace BookingHub.Application.UnitTests.Services;

public class CatalogoItemServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _baseDeDatos = new();
    private readonly CatalogoItemService _service;

    public CatalogoItemServiceTests()
    {
        _service = new CatalogoItemService(_baseDeDatos.Factory);
    }

    [Fact]
    public async Task Guardar_ServicioNuevo_LoInsertaConSuDuracion()
    {
        var servicio = new CatalogoItemDto { Nombre = "Corte + Barba", EsServicio = true, Precio = 12m, DuracionMin = 40 };

        var result = await _service.Guardar(servicio);

        Assert.True(result.IsSuccess);
        var guardado = await _service.Buscar(servicio.CatalogoItemId);
        Assert.Equal(40, guardado.Value!.DuracionMin);
        Assert.True(guardado.Value.EsServicio);
    }

    [Fact]
    public async Task Guardar_ProductoExistente_ActualizaElPrecio()
    {
        var producto = new CatalogoItemDto { Nombre = "Cera Mate", EsServicio = false, Precio = 10m };
        await _service.Guardar(producto);

        producto.Precio = 15m;
        var result = await _service.Guardar(producto);

        Assert.True(result.IsSuccess);
        var actualizado = await _service.Buscar(producto.CatalogoItemId);
        Assert.Equal(15m, actualizado.Value!.Precio);
    }

    [Fact]
    public async Task GetList_SoloServicios_ExcluyeProductos()
    {
        await _service.Guardar(new CatalogoItemDto { Nombre = "Corte", EsServicio = true, Precio = 8m, DuracionMin = 30 });
        await _service.Guardar(new CatalogoItemDto { Nombre = "Cera Mate", EsServicio = false, Precio = 10m });

        var result = await _service.GetList(i => i.EsServicio);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Corte", result.Value![0].Nombre);
    }

    [Fact]
    public async Task Eliminar_IdInexistente_NoFalla()
    {
        var result = await _service.Eliminar(999);

        Assert.True(result.IsSuccess);
    }

    public void Dispose() => _baseDeDatos.Dispose();
}
