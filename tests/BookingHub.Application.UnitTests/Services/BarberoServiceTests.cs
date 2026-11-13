using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using BookingHub.Application.UnitTests.Infraestructura;

namespace BookingHub.Application.UnitTests.Services;

public class BarberoServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _baseDeDatos = new();
    private readonly BarberoService _service;

    public BarberoServiceTests()
    {
        _service = new BarberoService(_baseDeDatos.Factory);
    }

    [Fact]
    public async Task Buscar_DevuelveNombreDelUsuarioSinCedula()
    {
        var result = await _service.Buscar(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Carlos Pérez", result.Value!.Nombre);
        Assert.Equal("El Fade", result.Value.Apodo);
        Assert.Null(result.Value.Cedula);
    }

    [Fact]
    public async Task Guardar_ModificarSinCedula_ConservaLaCedula()
    {
        var barbero = new BarberoDto { BarberoId = 1, UsuarioId = 1, Apodo = "Nuevo Apodo", ColorHex = "#000000", Activo = true };

        var result = await _service.Guardar(barbero);

        Assert.True(result.IsSuccess);
        using var contexto = _baseDeDatos.CrearContexto();
        var enBaseDeDatos = await contexto.Barberos.FindAsync(1);
        Assert.Equal("Nuevo Apodo", enBaseDeDatos!.Apodo);
        Assert.Equal("00100000001", enBaseDeDatos.Cedula);
    }

    [Fact]
    public async Task Guardar_ModificarConCedula_LaActualizaYNoLaDevuelve()
    {
        var barbero = new BarberoDto { BarberoId = 2, UsuarioId = 2, Apodo = "Barbas", Cedula = "00199999999", ColorHex = "#43A047", Activo = true };

        var result = await _service.Guardar(barbero);

        Assert.True(result.IsSuccess);
        Assert.Null(barbero.Cedula);
        using var contexto = _baseDeDatos.CrearContexto();
        var enBaseDeDatos = await contexto.Barberos.FindAsync(2);
        Assert.Equal("00199999999", enBaseDeDatos!.Cedula);
    }

    [Fact]
    public async Task GetList_OrdenaPorNombre()
    {
        var result = await _service.GetList(b => true);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Carlos Pérez", "Luis Gómez"], result.Value!.Select(b => b.Nombre));
    }

    public void Dispose() => _baseDeDatos.Dispose();
}
