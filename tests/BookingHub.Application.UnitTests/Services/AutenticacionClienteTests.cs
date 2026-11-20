using BookingHub.Application.DTOs;
using BookingHub.Application.UnitTests.Infraestructura;
using BookingHub.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookingHub.Application.UnitTests.Services;

public class AutenticacionClienteTests : IDisposable
{
    private const string Password = "ClaveSegura1";
    private readonly BaseDeDatosDePrueba _baseDeDatos = new();
    private readonly CorreoFalso _correo = new();
    private readonly ServicioAutenticacionCliente _service;

    public AutenticacionClienteTests()
    {
        _service = new ServicioAutenticacionCliente(
            _baseDeDatos.Factory,
            _correo,
            NullLogger<ServicioAutenticacionCliente>.Instance);
    }

    [Fact]
    public async Task Registrar_GuardaHashYPermiteIniciarSesion()
    {
        var (cliente, error) = await _service.Registrar(
            new RegistroClienteRequest("María Cliente", "8095551234", "MARIA@test.com", Password),
            CancellationToken.None);

        Assert.Null(error);
        Assert.NotNull(cliente);
        Assert.Equal("maria@test.com", cliente.EmailNormalizado);
        Assert.NotEqual(Password, cliente.PasswordHash);
        Assert.StartsWith("AQAAAA", cliente.PasswordHash);

        var sesion = await _service.IniciarSesion("Maria@Test.com", Password, CancellationToken.None);
        Assert.NotNull(sesion);
        Assert.Equal(cliente.ClienteId, sesion.ClienteId);
    }

    [Fact]
    public async Task RecuperarContrasena_ValidaCorreoYTelefonoYPermiteReclamarCuenta()
    {
        await _service.Registrar(
            new RegistroClienteRequest("Cliente Legado", "8095550001", "legado@test.com", Password),
            CancellationToken.None);

        await _service.SolicitarRestablecimiento("LEGADO@test.com", "809-555-0001", CancellationToken.None);
        Assert.Single(_correo.Enviados);
        var codigo = _correo.Enviados[0].Contenido.Split("\n\n", StringSplitOptions.None)[1].Split('\n')[0];

        var resultado = await _service.RestablecerContrasena(
            new RestablecerContrasenaRequest(codigo, "NuevaClave2"), CancellationToken.None);

        Assert.True(resultado.Hecho);
        Assert.NotNull(await _service.IniciarSesion("legado@test.com", "NuevaClave2", CancellationToken.None));
        Assert.Null(await _service.IniciarSesion("legado@test.com", Password, CancellationToken.None));
    }

    [Fact]
    public async Task RestablecerContrasena_CodigoDeUnSoloUso()
    {
        await _service.Registrar(
            new RegistroClienteRequest("Cliente Prueba", "8295551234", "unico@test.com", Password),
            CancellationToken.None);
        await _service.SolicitarRestablecimiento("unico@test.com", "8295551234", CancellationToken.None);
        var codigo = _correo.Enviados[0].Contenido.Split("\n\n", StringSplitOptions.None)[1].Split('\n')[0];

        var primera = await _service.RestablecerContrasena(
            new RestablecerContrasenaRequest(codigo, "NuevaClave2"), CancellationToken.None);
        var segunda = await _service.RestablecerContrasena(
            new RestablecerContrasenaRequest(codigo, "OtraClave3"), CancellationToken.None);

        Assert.True(primera.Hecho);
        Assert.False(segunda.Hecho);
    }

    public void Dispose() => _baseDeDatos.Dispose();

    private sealed class CorreoFalso : ICorreoTransaccional
    {
        public bool EstaConfigurado => true;
        public List<(string Destinatario, string Asunto, string Contenido)> Enviados { get; } = [];

        public Task Enviar(string destinatario, string asunto, string contenido, CancellationToken cancellationToken)
        {
            Enviados.Add((destinatario, asunto, contenido));
            return Task.CompletedTask;
        }
    }
}
