using System;
using System.Collections.Generic;

namespace BookingHub.API.Models;

public partial class CitaDetalle
{
    public int CitaId { get; set; }

    public int CatalogoItemId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int CitaDetalleId { get; set; }

    public virtual CatalogoItem CatalogoItem { get; set; } = null!;

    public virtual Cita Cita { get; set; } = null!;
}
