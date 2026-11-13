using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using BookingHub.Application.UnitTests.Infraestructura;

namespace BookingHub.Application.UnitTests.Services;

public class ClienteServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _baseDeDatos = new();
    private readonly ClienteService _service;

    public ClienteServiceTests()
    {
        _service = new ClienteService(_baseDeDatos.Factory);
    }

    [Fact]
    public async Task Guardar_ClienteNuevo_LoInsertaYAsignaId()
    {
        var cliente = new ClienteDto { Nombre = "Nuevo", Telefono = "8090000000" };

        var result = await _service.Guardar(cliente);

        Assert.True(result.IsSuccess);
        Assert.True(cliente.ClienteId > 0);
        var guardado = await _service.Buscar(cliente.ClienteId);
        Assert.Equal("Nuevo", guardado.Value!.Nombre);
    }

    [Fact]
    public async Task Guardar_ClienteExistente_ModificaSinBorrarCamposFueraDelDto()
    {
        var cliente = new ClienteDto { ClienteId = 1, Nombre = "Cliente Editado", Telefono = "8091111111", Activo = true };

        var result = await _service.Guardar(cliente);

        Assert.True(result.IsSuccess);
        using var contexto = _baseDeDatos.CrearContexto();
        var enBaseDeDatos = await contexto.Clientes.FindAsync(1);
        Assert.Equal("Cliente Editado", enBaseDeDatos!.Nombre);
        Assert.Equal("Prefiere la mañana", enBaseDeDatos.Notas);
    }

    [Fact]
    public async Task Buscar_IdInexistente_DevuelveNull()
    {
        var result = await _service.Buscar(999);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetList_ConCriterioActivo_ExcluyeInactivos()
    {
        var result = await _service.GetList(c => c.Activo);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Cliente Demo", result.Value![0].Nombre);
    }

    [Fact]
    public async Task Eliminar_ClienteExistente_LoBorra()
    {
        var result = await _service.Eliminar(2);

        Assert.True(result.IsSuccess);
        var buscado = await _service.Buscar(2);
        Assert.Null(buscado.Value);
    }

    public void Dispose() => _baseDeDatos.Dispose();
}
