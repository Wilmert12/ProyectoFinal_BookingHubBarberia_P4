using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using BookingHub.Application.UnitTests.Infraestructura;

namespace BookingHub.Application.UnitTests.Services;

public class CitaServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _baseDeDatos = new();
    private readonly CitaService _service;

    public CitaServiceTests()
    {
        _service = new CitaService(_baseDeDatos.Factory);
    }

    private static CitaDto NuevaCita(int barberoId = 1, int hora = 10) => new()
    {
        ClienteId = 1,
        BarberoId = barberoId,
        EstadoCitaId = 1,
        FechaInicio = new DateTime(2026, 11, 20, hora, 0, 0),
        FechaFin = new DateTime(2026, 11, 20, hora, 40, 0),
        Total = 12m,
        Notas = "Corte + Barba"
    };

    [Fact]
    public async Task Guardar_CitaNueva_LaInsertaYAsignaId()
    {
        var cita = NuevaCita();

        var result = await _service.Guardar(cita);

        Assert.True(result.IsSuccess);
        Assert.True(cita.CitaId > 0);
    }

    [Fact]
    public async Task Buscar_DevuelveNombresDeClienteBarberoYEstado()
    {
        var cita = NuevaCita();
        await _service.Guardar(cita);

        var result = await _service.Buscar(cita.CitaId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cliente Demo", result.Value!.ClienteNombre);
        Assert.Equal("Carlos Pérez", result.Value.BarberoNombre);
        Assert.Equal("Pendiente", result.Value.Estado);
    }

    [Fact]
    public async Task Guardar_CitaExistente_CambiaElEstado()
    {
        var cita = NuevaCita();
        await _service.Guardar(cita);

        cita.EstadoCitaId = 2;
        var result = await _service.Guardar(cita);

        Assert.True(result.IsSuccess);
        var actualizada = await _service.Buscar(cita.CitaId);
        Assert.Equal("Cancelada", actualizada.Value!.Estado);
    }

    [Fact]
    public async Task GetList_FiltraPorBarberoYOrdenaPorFecha()
    {
        await _service.Guardar(NuevaCita(barberoId: 1, hora: 15));
        await _service.Guardar(NuevaCita(barberoId: 2, hora: 9));
        await _service.Guardar(NuevaCita(barberoId: 1, hora: 9));

        var result = await _service.GetList(c => c.BarberoId == 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(9, result.Value[0].FechaInicio.Hour);
        Assert.Equal(15, result.Value[1].FechaInicio.Hour);
    }

    [Fact]
    public async Task Eliminar_CitaExistente_LaBorra()
    {
        var cita = NuevaCita();
        await _service.Guardar(cita);

        var result = await _service.Eliminar(cita.CitaId);

        Assert.True(result.IsSuccess);
        var buscada = await _service.Buscar(cita.CitaId);
        Assert.Null(buscada.Value);
    }

    public void Dispose() => _baseDeDatos.Dispose();
}
