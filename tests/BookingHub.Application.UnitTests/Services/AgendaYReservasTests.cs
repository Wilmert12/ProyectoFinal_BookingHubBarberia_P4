using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using BookingHub.Application.UnitTests.Infraestructura;

namespace BookingHub.Application.UnitTests.Services;

/// <summary>
/// Reglas de agenda: horas libres, reservas de clientes (verificadas con el teléfono),
/// cancelaciones, horarios y días libres, y lo que el barbero puede marcar.
/// </summary>
public class AgendaYReservasTests : IDisposable
{
    private const int Carlos = 1;
    private const int Luis = 2;
    private const int ClienteDemo = 1;
    private const int ClienteInactivo = 2;
    private const int Pendiente = 1;
    private const int Cancelada = 2;
    private const int Completada = 3;
    private const string TelefonoDemo = "809-123-4567";

    private readonly BaseDeDatosDePrueba _baseDeDatos = new();
    private readonly CitaService _citas;
    private readonly DisponibilidadService _disponibilidad;
    private readonly ReservaService _reservas;
    private readonly AgendaBarberoService _agenda;
    private readonly int _corte;
    private readonly DateOnly _lunes = ProximoLunes();

    public AgendaYReservasTests()
    {
        _citas = new CitaService(_baseDeDatos.Factory);
        _disponibilidad = new DisponibilidadService(_baseDeDatos.Factory);
        _reservas = new ReservaService(_baseDeDatos.Factory, _citas, _disponibilidad);
        _agenda = new AgendaBarberoService(_baseDeDatos.Factory);
        _corte = _baseDeDatos.CrearItem("Corte clásico", 400);
    }

    public void Dispose() => _baseDeDatos.Dispose();

    // Un lunes a más de una semana: siempre en el futuro y dentro del horario por defecto.
    private static DateOnly ProximoLunes()
    {
        var dia = DateOnly.FromDateTime(DateTime.Today).AddDays(7);
        while (dia.DayOfWeek != DayOfWeek.Monday)
            dia = dia.AddDays(1);
        return dia;
    }

    private DateTime A(int hora, int minuto = 0) => _lunes.ToDateTime(new TimeOnly(hora, minuto));

    private async Task<int> CrearCita(int barberoId, DateTime inicio, int estadoId = Pendiente)
    {
        var cita = new CitaDto
        {
            ClienteId = ClienteDemo,
            BarberoId = barberoId,
            EstadoCitaId = estadoId,
            FechaInicio = inicio,
            FechaFin = inicio.AddMinutes(30),
            Detalles = [new CitaDetalleDto { CatalogoItemId = _corte, Cantidad = 1, PrecioUnitario = 400 }]
        };
        var resultado = await _citas.Guardar(cita);
        Assert.True(resultado.IsSuccess);
        return cita.CitaId;
    }

    private async Task<List<DateTime>> HorasLibres(int barberoId, DateOnly fecha, int duracion = 30)
    {
        var resultado = await _disponibilidad.Calcular(barberoId, fecha, duracion);
        Assert.True(resultado.IsSuccess);
        return resultado.Value!.Horarios.Select(h => h.Inicio).ToList();
    }

    // ---------- Disponibilidad ----------

    [Fact]
    public async Task Disponibilidad_SinHorarioPropio_UsaLunesASabadoDe9A18()
    {
        var libres = await HorasLibres(Carlos, _lunes);

        Assert.Equal(18, libres.Count);
        Assert.Equal(A(9), libres.First());
        Assert.Equal(A(17, 30), libres.Last());
        Assert.Empty(await HorasLibres(Carlos, _lunes.AddDays(6)));
    }

    [Fact]
    public async Task Disponibilidad_CitaPendienteOcupaYCanceladaNo()
    {
        await CrearCita(Carlos, A(10));
        await CrearCita(Carlos, A(11), Cancelada);

        var libres = await HorasLibres(Carlos, _lunes);

        Assert.DoesNotContain(A(10), libres);
        Assert.Contains(A(9, 30), libres);
        Assert.Contains(A(11), libres);
        Assert.Contains(A(10), await HorasLibres(Luis, _lunes));
    }

    [Fact]
    public async Task Disponibilidad_DuracionLargaNoEmpiezaSiChocaConLaSiguienteCita()
    {
        await CrearCita(Carlos, A(10));

        var libres = await HorasLibres(Carlos, _lunes, duracion: 60);

        Assert.DoesNotContain(A(9, 30), libres);
        Assert.Contains(A(9), libres);
    }

    [Fact]
    public async Task Disponibilidad_DuracionInvalidaYBarberoInexistente()
    {
        var invalida = await _disponibilidad.Calcular(Carlos, _lunes, 1);
        var inexistente = await _disponibilidad.Calcular(999, _lunes, 30);

        Assert.True(invalida.IsFailure);
        Assert.Equal("DURACION_INVALIDA", invalida.Error.Code);
        Assert.True(inexistente.IsSuccess);
        Assert.Null(inexistente.Value);
    }

    // ---------- Horarios y días libres ----------

    [Fact]
    public async Task Horarios_PropiosReemplazanAlHorarioPorDefecto()
    {
        var guardado = await _agenda.GuardarHorarios(Carlos, [new HorarioDto(1, new TimeOnly(10, 0), new TimeOnly(12, 0))]);

        Assert.True(guardado.IsSuccess);
        Assert.Equal([A(10), A(10, 30), A(11), A(11, 30)], await HorasLibres(Carlos, _lunes));
        Assert.Empty(await HorasLibres(Carlos, _lunes.AddDays(1)));
    }

    [Fact]
    public async Task Horarios_BloquesQueSeCruzanSonError()
    {
        var resultado = await _agenda.GuardarHorarios(Carlos,
        [
            new HorarioDto(1, new TimeOnly(9, 0), new TimeOnly(13, 0)),
            new HorarioDto(1, new TimeOnly(12, 0), new TimeOnly(18, 0))
        ]);

        Assert.True(resultado.IsFailure);
        Assert.Equal("HORARIO_CHOQUE", resultado.Error.Code);
    }

    [Fact]
    public async Task DiaLibre_DeTodaLaBarberiaBloqueaATodos()
    {
        var tipos = await _agenda.GetTipos();
        Assert.Equal(5, tipos.Value!.Count);

        var cierre = new IndisponibilidadDto
        {
            BarberoId = null,
            TipoIndisponibilidadId = tipos.Value[0].TipoIndisponibilidadId,
            FechaInicio = _lunes.ToDateTime(TimeOnly.MinValue),
            FechaFin = _lunes.AddDays(1).ToDateTime(TimeOnly.MinValue)
        };
        Assert.True((await _agenda.CrearIndisponibilidad(cierre)).IsSuccess);

        Assert.Empty(await HorasLibres(Carlos, _lunes));
        Assert.Empty(await HorasLibres(Luis, _lunes));
        Assert.NotEmpty(await HorasLibres(Carlos, _lunes.AddDays(1)));
    }

    // ---------- Reservas de clientes ----------

    private CitaDto Reserva(DateTime inicio) => new()
    {
        ClienteId = ClienteDemo,
        BarberoId = Carlos,
        EstadoCitaId = Completada, // el cliente no puede elegir el estado
        FechaInicio = inicio,
        FechaFin = inicio,
        Total = 1,
        Detalles = [new CitaDetalleDto { CatalogoItemId = _corte, Cantidad = 1, PrecioUnitario = 1 }]
    };

    [Fact]
    public async Task Reservar_UsaPrecioDelCatalogoDuracionYEstadoPendiente()
    {
        var cita = Reserva(A(10));

        var resultado = await _reservas.Reservar(cita, TelefonoDemo);

        Assert.True(resultado.IsSuccess);
        var guardada = (await _citas.Buscar(cita.CitaId)).Value!;
        Assert.Equal(400, guardada.Total);
        Assert.Equal("Pendiente", guardada.Estado);
        Assert.Equal(A(10, 30), guardada.FechaFin);
    }

    [Theory]
    [InlineData("8090000000")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Reservar_TelefonoQueNoCoincideEsRechazado(string? telefono)
    {
        var resultado = await _reservas.Reservar(Reserva(A(10)), telefono);

        Assert.True(resultado.IsFailure);
        Assert.Equal(ReservaService.ErrorNoVerificado, resultado.Error.Code);
    }

    [Fact]
    public async Task Reservar_ClienteInactivoEsRechazado()
    {
        var cita = Reserva(A(10));
        cita.ClienteId = ClienteInactivo;

        var resultado = await _reservas.Reservar(cita, "8097654321");

        Assert.Equal(ReservaService.ErrorNoVerificado, resultado.Error.Code);
    }

    [Fact]
    public async Task Reservar_HoraOcupadaOFueraDeHorarioEsRechazada()
    {
        await CrearCita(Carlos, A(10));

        var ocupada = await _reservas.Reservar(Reserva(A(10)), TelefonoDemo);
        var deNoche = await _reservas.Reservar(Reserva(A(21)), TelefonoDemo);

        Assert.Equal("CITA_CHOQUE", ocupada.Error.Code);
        Assert.Equal("CITA_CHOQUE", deNoche.Error.Code);
    }

    // ---------- Recuperar el número ----------

    [Theory]
    [InlineData("Cliente Demo", "8091234567")]
    [InlineData("  cliente   DEMO ", "(809) 123-4567")]
    public async Task RecuperarNumero_ConNombreYTelefonoDelRegistro(string nombre, string telefono)
    {
        var numero = await _reservas.RecuperarNumero(nombre, telefono);

        Assert.Equal(ClienteDemo, numero?.ClienteId);
    }

    [Theory]
    [InlineData("Cliente Demo", "8090000000")]
    [InlineData("Otro Nombre", "8091234567")]
    [InlineData("Cliente Inactivo", "8097654321")]
    [InlineData("", "8091234567")]
    public async Task RecuperarNumero_SiNoCoincideNoDevuelveNada(string nombre, string telefono)
    {
        Assert.Null(await _reservas.RecuperarNumero(nombre, telefono));
    }

    // ---------- Cancelación por el cliente ----------

    [Fact]
    public async Task Cancelar_ConSuTelefonoLaCancelaYLiberaLaHora()
    {
        var id = await CrearCita(Carlos, A(10));

        var resultado = await _reservas.Cancelar(id, TelefonoDemo);

        Assert.True(resultado.IsSuccess);
        Assert.Equal("Cancelada", (await _citas.Buscar(id)).Value!.Estado);
        Assert.Contains(A(10), await HorasLibres(Carlos, _lunes));
    }

    [Fact]
    public async Task Cancelar_TelefonoAjenoYaCanceladaOPasadaSonRechazadas()
    {
        var futura = await CrearCita(Carlos, A(10));
        var cancelada = await CrearCita(Carlos, A(11), Cancelada);
        var pasada = await CrearCita(Carlos, DateTime.Today.AddDays(-3).AddHours(10));

        Assert.Equal(ReservaService.ErrorNoVerificado, (await _reservas.Cancelar(futura, "8090000000")).Error.Code);
        Assert.Equal("CITA_CANCELADA", (await _reservas.Cancelar(cancelada, TelefonoDemo)).Error.Code);
        Assert.Equal("CITA_NO_CANCELABLE", (await _reservas.Cancelar(pasada, TelefonoDemo)).Error.Code);
        Assert.Null((await _reservas.Cancelar(999, TelefonoDemo)).Value);
    }

    // ---------- Estados que marca el barbero ----------

    [Fact]
    public async Task Barbero_PuedeCompletarSuCitaPeroNoLaDeOtro()
    {
        var id = await CrearCita(Carlos, A(10));

        var deOtro = await _citas.CambiarEstadoPorBarbero(id, Luis, Completada);
        var suya = await _citas.CambiarEstadoPorBarbero(id, Carlos, Completada);

        Assert.Null(deOtro.Value);
        Assert.True(suya.IsSuccess);
        Assert.Equal("Completada", (await _citas.Buscar(id)).Value!.Estado);
    }

    [Fact]
    public async Task Barbero_NoPuedeVolverAPendienteNiTocarCanceladas()
    {
        var id = await CrearCita(Carlos, A(10));
        var cancelada = await CrearCita(Carlos, A(11), Cancelada);

        Assert.Equal("ESTADO_NO_PERMITIDO", (await _citas.CambiarEstadoPorBarbero(id, Carlos, Pendiente)).Error.Code);
        Assert.Equal("CITA_CANCELADA", (await _citas.CambiarEstadoPorBarbero(cancelada, Carlos, Completada)).Error.Code);
    }
}
