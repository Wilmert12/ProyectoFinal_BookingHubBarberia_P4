using BookingHub.WinForms.Models;
using Refit;

namespace BookingHub.WinForms.Api;

public interface IAuthApi
{
    [Post("/api/Auth/login")]
    Task<SesionDto> Login([Body] LoginDto login);
}

public interface ICitaApi
{
    [Get("/api/Citas")]
    Task<List<CitaDto>> GetCitas(int? barberoId = null, string? fecha = null);

    [Get("/api/Citas/{id}")]
    Task<CitaDto> GetCita(int id);

    [Post("/api/Citas")]
    Task<CitaDto> CrearCita([Body] CitaDto cita);

    [Put("/api/Citas/{id}")]
    Task ModificarCita(int id, [Body] CitaDto cita);

    [Delete("/api/Citas/{id}")]
    Task EliminarCita(int id);
}

public interface IClienteApi
{
    [Get("/api/Clientes")]
    Task<List<ClienteDto>> GetClientes();

    [Post("/api/Clientes")]
    Task<ClienteDto> CrearCliente([Body] ClienteDto cliente);
}

public interface IBarberoApi
{
    [Get("/api/Barberos")]
    Task<List<BarberoDto>> GetBarberos(bool incluirInactivos = false);

    [Post("/api/Barberos")]
    Task<BarberoDto> CrearBarbero([Body] BarberoDto barbero);

    [Put("/api/Barberos/{id}")]
    Task ModificarBarbero(int id, [Body] BarberoDto barbero);
}

public interface ICatalogoItemApi
{
    [Get("/api/CatalogoItems")]
    Task<List<CatalogoItemDto>> GetCatalogoItems(bool incluirInactivos = false);

    [Post("/api/CatalogoItems")]
    Task<CatalogoItemDto> CrearItem([Body] CatalogoItemDto item);

    [Put("/api/CatalogoItems/{id}")]
    Task ModificarItem(int id, [Body] CatalogoItemDto item);

    [Delete("/api/CatalogoItems/{id}")]
    Task EliminarItem(int id);
}

public interface IPagoApi
{
    [Get("/api/Pagos")]
    Task<List<PagoDto>> GetPagos(string? fecha = null, int? citaId = null);

    [Post("/api/Pagos")]
    Task<PagoDto> CrearPago([Body] PagoDto pago);
}

public interface ICatalogosApi
{
    [Get("/api/EstadosCita")]
    Task<List<EstadoCitaDto>> GetEstadosCita();

    [Get("/api/MetodosPago")]
    Task<List<MetodoPagoDto>> GetMetodosPago();

    [Get("/api/Roles")]
    Task<List<RolDto>> GetRoles();
}

public interface IReporteApi
{
    [Get("/api/Reportes/ingresos")]
    Task<ReporteIngresosDto> GetIngresos(string desde, string hasta);
}

public interface IUsuarioApi
{
    [Get("/api/Usuarios")]
    Task<List<UsuarioDto>> GetUsuarios(bool incluirInactivos = false);

    [Post("/api/Usuarios")]
    Task<UsuarioDto> CrearUsuario([Body] UsuarioDto usuario);

    [Put("/api/Usuarios/{id}")]
    Task ModificarUsuario(int id, [Body] UsuarioDto usuario);
}
