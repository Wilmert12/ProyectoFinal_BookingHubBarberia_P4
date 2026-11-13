using System;
using System.Collections.Generic;

namespace BookingHub.Infrastructure.Models;

public partial class TiposIndisponibilidad
{
    public string Descripcion { get; set; } = null!;

    public int TipoIndisponibilidadId { get; set; }

    public virtual ICollection<IndisponibilidadBarbero> IndisponibilidadBarberos { get; set; } = new List<IndisponibilidadBarbero>();
}
