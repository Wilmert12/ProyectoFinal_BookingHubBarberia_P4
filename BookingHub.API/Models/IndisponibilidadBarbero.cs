using System;
using System.Collections.Generic;

namespace BookingHub.API.Models;

public partial class IndisponibilidadBarbero
{
    public int IndisponibilidadId { get; set; }

    public int? BarberoId { get; set; }

    public int TipoIndisponibilidadId { get; set; }

    public string? Descripcion { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Barbero? Barbero { get; set; }

    public virtual TiposIndisponibilidad TipoIndisponibilidad { get; set; } = null!;
}
