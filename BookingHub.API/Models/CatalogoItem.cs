using System;
using System.Collections.Generic;

namespace BookingHub.API.Models;

public partial class CatalogoItem
{
    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool EsServicio { get; set; }

    public decimal Precio { get; set; }

    public int? DuracionMin { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int CatalogoItemId { get; set; }

    public virtual ICollection<CitaDetalle> CitaDetalles { get; set; } = new List<CitaDetalle>();
}
