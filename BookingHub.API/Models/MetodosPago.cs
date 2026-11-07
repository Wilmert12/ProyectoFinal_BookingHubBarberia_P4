using System;
using System.Collections.Generic;

namespace BookingHub.API.Models;

public partial class MetodosPago
{
    public string Descripcion { get; set; } = null!;

    public bool Activo { get; set; }

    public int MetodoPagoId { get; set; }

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
