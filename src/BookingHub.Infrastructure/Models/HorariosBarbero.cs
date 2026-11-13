using System;
using System.Collections.Generic;

namespace BookingHub.Infrastructure.Models;

public partial class HorariosBarbero
{
    public int HorarioId { get; set; }

    public int BarberoId { get; set; }

    public byte DiaSemana { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Barbero Barbero { get; set; } = null!;
}
