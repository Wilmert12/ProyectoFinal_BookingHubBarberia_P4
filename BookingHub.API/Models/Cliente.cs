using System;
using System.Collections.Generic;

namespace BookingHub.API.Models;

public partial class Cliente
{
    public string Nombre { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string? Email { get; set; }

    public string? Notas { get; set; }

    public int TotalNoShows { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int ClienteId { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();
}
