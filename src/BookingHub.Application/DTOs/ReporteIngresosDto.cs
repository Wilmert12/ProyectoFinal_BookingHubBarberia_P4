namespace BookingHub.Application.DTOs;

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
