using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using BookingHub.Application.UnitTests.Infraestructura;
using BookingHub.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookingHub.Application.UnitTests.Services;

public sealed class ReservaCitaServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _baseDeDatos = new();
    private readonly ReservaCitaService _servicio;

    public ReservaCitaServiceTests()
    {
        _servicio = new ReservaCitaService(
            _baseDeDatos.Factory,
            Options.Create(new ReglasReservaOptions
            {
                AnticipacionMinimaHoras = 0,
                DiasMaximosAnticipacion = 90,
                CitasActivasMaximas = 10,
                HorasMinimasParaCancelar = 0
            }));
    }

    [Fact]
    public async Task Crear_RecalculaPrecioDuracionYFinDesdeElCatalogo()
    {
        var itemId = _baseDeDatos.CrearItem("Corte", 450m);
        var solicitud = NuevaSolicitud(itemId, precioCliente: 1m, minutosAdicionalesCliente: 90);

        var resultado = await _servicio.Crear(clienteId: 1, solicitud);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(450m, resultado.Value!.Total);
        Assert.Equal(solicitud.FechaInicio.AddMinutes(30), resultado.Value.FechaFin);
        Assert.Equal(450m, Assert.Single(resultado.Value.Detalles!).PrecioUnitario);
    }

    [Fact]
    public async Task Crear_RechazaServicioInactivo()
    {
        var itemId = _baseDeDatos.CrearItem("Servicio temporal", 200m);
        using (var db = _baseDeDatos.CrearContexto())
        {
            var item = await db.CatalogoItems.SingleAsync(i => i.CatalogoItemId == itemId);
            item.Activo = false;
            await db.SaveChangesAsync();
        }

        var resultado = await _servicio.Crear(1, NuevaSolicitud(itemId));

        Assert.True(resultado.IsFailure);
        Assert.Equal("RESERVA_SERVICIOS", resultado.Error.Code);
    }

    [Fact]
    public async Task Crear_RechazaUnHorarioQueYaTieneUnaCitaActiva()
    {
        ActivarClienteSecundario();
        var itemId = _baseDeDatos.CrearItem("Corte", 350m);
        var primera = await _servicio.Crear(1, NuevaSolicitud(itemId));
        Assert.True(primera.IsSuccess);

        var segunda = await _servicio.Crear(2, NuevaSolicitud(itemId));

        Assert.True(segunda.IsFailure);
        Assert.Equal("CITA_CHOQUE", segunda.Error.Code);
    }

    [Fact]
    public async Task Cancelar_NoPermiteQueOtroClienteCambieLaCita()
    {
        ActivarClienteSecundario();
        var itemId = _baseDeDatos.CrearItem("Corte", 350m);
        var creada = await _servicio.Crear(1, NuevaSolicitud(itemId));
        Assert.True(creada.IsSuccess);

        var resultado = await _servicio.Cancelar(2, creada.Value!.CitaId, "No es mía");

        Assert.True(resultado.IsFailure);
        Assert.Equal("CITA_NO_EXISTE", resultado.Error.Code);
        Assert.True(await _servicio.PerteneceACliente(1, creada.Value.CitaId));
        Assert.False(await _servicio.PerteneceACliente(2, creada.Value.CitaId));
    }

    [Fact]
    public async Task CambiarEstadoBarbero_ValidaPropiedadYTransiciones()
    {
        var itemId = _baseDeDatos.CrearItem("Corte", 350m);
        using (var db = _baseDeDatos.CrearContexto())
        {
            db.EstadosCita.Add(new EstadosCitum { Descripcion = "Confirmada" });
            await db.SaveChangesAsync();
        }
        var creada = await _servicio.Crear(1, NuevaSolicitud(itemId));
        Assert.True(creada.IsSuccess);
        var confirmadaId = await EstadoId("Confirmada");
        var completadaId = await EstadoId("Completada");

        var otroBarbero = await _servicio.CambiarEstadoBarbero(2, creada.Value!.CitaId, confirmadaId);
        var transicionInvalida = await _servicio.CambiarEstadoBarbero(1, creada.Value.CitaId, completadaId);
        var confirmacion = await _servicio.CambiarEstadoBarbero(1, creada.Value.CitaId, confirmadaId);

        Assert.True(otroBarbero.IsFailure);
        Assert.Equal("CITA_NO_EXISTE", otroBarbero.Error.Code);
        Assert.True(transicionInvalida.IsFailure);
        Assert.Equal("CITA_TRANSICION", transicionInvalida.Error.Code);
        Assert.True(confirmacion.IsSuccess);
        Assert.Equal("Confirmada", confirmacion.Value!.Estado);
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private void ActivarClienteSecundario()
    {
        using var db = _baseDeDatos.CrearContexto();
        var cliente = db.Clientes.Single(c => c.ClienteId == 2);
        cliente.Activo = true;
        db.SaveChanges();
    }

    private async Task<int> EstadoId(string nombre)
    {
        using var db = _baseDeDatos.CrearContexto();
        return await db.EstadosCita.Where(e => e.Descripcion == nombre)
            .Select(e => e.EstadoCitaId)
            .SingleAsync();
    }

    private static CitaDto NuevaSolicitud(int itemId, decimal precioCliente = 1m, int minutosAdicionalesCliente = 0)
    {
        var fecha = DateTime.Today.AddDays(2);
        while (fecha.DayOfWeek == DayOfWeek.Sunday)
            fecha = fecha.AddDays(1);

        var inicio = fecha.AddHours(10);
        return new CitaDto
        {
            ClienteId = 999,
            BarberoId = 1,
            EstadoCitaId = 3,
            FechaInicio = inicio,
            FechaFin = inicio.AddMinutes(30 + minutosAdicionalesCliente),
            Total = precioCliente,
            Notas = "  nota de prueba  ",
            Detalles = [new CitaDetalleDto
            {
                CatalogoItemId = itemId,
                Nombre = "Nombre controlado por el cliente",
                Cantidad = 1,
                PrecioUnitario = precioCliente
            }]
        };
    }
}
