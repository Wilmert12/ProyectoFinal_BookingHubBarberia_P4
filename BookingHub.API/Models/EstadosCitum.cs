using System;
using System.Collections.Generic;

namespace BookingHub.API.Models;

public partial class EstadosCitum
{
    public string Descripcion { get; set; } = null!;

    public bool LiberaHorario { get; set; }

    public int EstadoCitaId { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();
}
