using System;
using System.Collections.Generic;

namespace BookingHub.API.Models;

public partial class Pago
{
    public int CitaId { get; set; }

    public int MetodoPagoId { get; set; }

    public decimal Monto { get; set; }

    public decimal Propina { get; set; }

    public string? Referencia { get; set; }

    public DateTime FechaPago { get; set; }

    public int PagoId { get; set; }

    public virtual Cita Cita { get; set; } = null!;

    public virtual MetodosPago MetodoPago { get; set; } = null!;
}
