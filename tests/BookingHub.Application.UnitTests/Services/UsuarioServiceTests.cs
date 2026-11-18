using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using BookingHub.Application.UnitTests.Infraestructura;
using BookingHub.Infrastructure.Models;

namespace BookingHub.Application.UnitTests.Services;

public class UsuarioServiceTests : IDisposable
{
    private const string Password = "Secreta-123";

    private readonly BaseDeDatosDePrueba _baseDeDatos = new();
    private readonly UsuarioService _service;
    private readonly int _rolAdminId;

    public UsuarioServiceTests()
    {
        _service = new UsuarioService(_baseDeDatos.Factory);
        using var contexto = _baseDeDatos.CrearContexto();
        var rol = new Role { Descripcion = "Admin" };
        contexto.Roles.Add(rol);
        contexto.SaveChanges();
        _rolAdminId = rol.RolId;
    }

    private UsuarioDto NuevoUsuario(string email = "admin@test.com") => new()
    {
        Nombre = "Admin Prueba",
        Email = email,
        RolId = _rolAdminId,
        Password = Password
    };

    [Fact]
    public async Task Guardar_UsuarioNuevo_GuardaLaContrasenaCifradaYNoLaDevuelve()
    {
        var usuario = NuevoUsuario();

        var result = await _service.Guardar(usuario);

        Assert.True(result.IsSuccess);
        Assert.Null(usuario.Password);
        using var contexto = _baseDeDatos.CrearContexto();
        var enBaseDeDatos = await contexto.Usuarios.FindAsync(usuario.UsuarioId);
        Assert.NotEqual(Password, enBaseDeDatos!.PasswordHash);
        Assert.StartsWith("AQAAAA", enBaseDeDatos.PasswordHash);
    }

    [Fact]
    public async Task Guardar_UsuarioNuevoSinContrasena_Falla()
    {
        var usuario = NuevoUsuario();
        usuario.Password = null;

        var result = await _service.Guardar(usuario);

        Assert.True(result.IsFailure);
        Assert.Equal("USUARIO_PASSWORD", result.Error.Code);
    }

    [Fact]
    public async Task Guardar_CorreoRepetido_Falla()
    {
        await _service.Guardar(NuevoUsuario());

        var result = await _service.Guardar(NuevoUsuario("ADMIN@test.com"));

        Assert.True(result.IsFailure);
        Assert.Equal("USUARIO_EMAIL", result.Error.Code);
    }

    [Fact]
    public async Task IniciarSesion_ContrasenaCorrecta_DevuelveElRol()
    {
        await _service.Guardar(NuevoUsuario());

        var result = await _service.IniciarSesion(new LoginDto("Admin@Test.com", Password));

        Assert.True(result.IsSuccess);
        Assert.Equal("Admin", result.Value!.Rol);
        Assert.Equal("Admin Prueba", result.Value.Nombre);
    }

    [Fact]
    public async Task IniciarSesion_ContrasenaIncorrecta_Falla()
    {
        await _service.Guardar(NuevoUsuario());

        var result = await _service.IniciarSesion(new LoginDto("admin@test.com", "otra-clave"));

        Assert.True(result.IsFailure);
        Assert.Equal("LOGIN_INVALIDO", result.Error.Code);
    }

    [Fact]
    public async Task IniciarSesion_UsuarioInactivo_Falla()
    {
        var usuario = NuevoUsuario();
        usuario.Activo = false;
        await _service.Guardar(usuario);

        var result = await _service.IniciarSesion(new LoginDto("admin@test.com", Password));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task IniciarSesion_ContrasenaProvisionalSinCifrar_FallaSinLanzarError()
    {
        var result = await _service.IniciarSesion(new LoginDto("carlos@test.com", "hash"));

        Assert.True(result.IsFailure);
        Assert.Equal("LOGIN_INVALIDO", result.Error.Code);
    }

    [Fact]
    public async Task Guardar_ModificarSinContrasena_ConservaLaAnterior()
    {
        var usuario = NuevoUsuario();
        await _service.Guardar(usuario);

        usuario.Nombre = "Nuevo Nombre";
        usuario.Password = null;
        var result = await _service.Guardar(usuario);

        Assert.True(result.IsSuccess);
        var login = await _service.IniciarSesion(new LoginDto("admin@test.com", Password));
        Assert.True(login.IsSuccess);
        Assert.Equal("Nuevo Nombre", login.Value!.Nombre);
    }

    public void Dispose() => _baseDeDatos.Dispose();
}
