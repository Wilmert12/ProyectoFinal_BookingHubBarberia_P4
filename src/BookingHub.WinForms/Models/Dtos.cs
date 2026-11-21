namespace BookingHub.WinForms.Models;

public record CitaDto
{
    public int CitaId { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public int BarberoId { get; set; }
    public string? BarberoNombre { get; set; }
    public int EstadoCitaId { get; set; }
    public string? Estado { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal Total { get; set; }
    public string? Notas { get; set; }
    public List<CitaDetalleDto>? Detalles { get; set; }
}

public record CitaDetalleDto
{
    public int CitaDetalleId { get; set; }
    public int CatalogoItemId { get; set; }
    public string? Nombre { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }
}

public record ClienteDto
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
}

public record BarberoDto
{
    public int BarberoId { get; set; }
    public int UsuarioId { get; set; }
    public string? Nombre { get; set; }
    public string? Apodo { get; set; }
    public string ColorHex { get; set; } = "#000000";
    public bool Activo { get; set; } = true;
    public string? Cedula { get; set; }
}

public record CatalogoItemDto
{
    public int CatalogoItemId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsServicio { get; set; }
    public decimal Precio { get; set; }
    public int? DuracionMin { get; set; }
    public bool Activo { get; set; } = true;
}

public record PagoDto
{
    public int PagoId { get; set; }
    public int CitaId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? BarberoNombre { get; set; }
    public int MetodoPagoId { get; set; }
    public string? MetodoPago { get; set; }
    public decimal Monto { get; set; }
    public decimal Propina { get; set; }
    public string? Referencia { get; set; }
    public DateTime FechaPago { get; set; }
}

public record EstadoCitaDto(int EstadoCitaId, string Descripcion, bool LiberaHorario);

public record MetodoPagoDto(int MetodoPagoId, string Descripcion);

public record RolDto(int RolId, string Descripcion);

public record UsuarioDto
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int RolId { get; set; }
    public string? Rol { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime? UltimoLogin { get; set; }
    public string? Password { get; set; }
}

public record LoginDto(string Email, string Password);

public record SesionDto(string Token, DateTime Expira, int UsuarioId, string Nombre, string Email, string Rol);

public record ReporteIngresosDto
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public decimal TotalIngresos { get; set; }
    public decimal TotalPropinas { get; set; }
    public int CantidadPagos { get; set; }
    public int CitasCompletadas { get; set; }
    public List<IngresoPorMetodoDto> PorMetodo { get; set; } = [];
    public List<IngresoPorBarberoDto> PorBarbero { get; set; } = [];
    public List<IngresoPorServicioDto> PorServicio { get; set; } = [];
}

public record IngresoPorMetodoDto(string MetodoPago, int Cantidad, decimal Total);

public record IngresoPorBarberoDto(int BarberoId, string Nombre, int Citas, decimal Ingresos);

public record IngresoPorServicioDto(int CatalogoItemId, string Nombre, bool EsServicio, int Cantidad, decimal Ingresos);

public record DisponibilidadDto(int BarberoId, DateOnly Fecha, int DuracionMin, List<HorarioLibreDto> Horarios);

public record HorarioLibreDto(DateTime Inicio, DateTime Fin);

public record HorarioDto(byte DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);

public record TipoIndisponibilidadDto(int TipoIndisponibilidadId, string Descripcion);

public record IndisponibilidadDto
{
    public int IndisponibilidadId { get; set; }
    public int? BarberoId { get; set; }
    public string? BarberoNombre { get; set; }
    public int TipoIndisponibilidadId { get; set; }
    public string? Tipo { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public record ErrorApi(string? Code, string? Description);
