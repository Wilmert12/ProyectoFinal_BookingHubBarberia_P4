using System;
using System.Collections.Generic;

namespace BookingHub.Infrastructure.Models;

public partial class Barbero
{
    public int UsuarioId { get; set; }

    public string? Apodo { get; set; }

    public string Cedula { get; set; } = null!;

    public string ColorHex { get; set; } = null!;

    public bool Activo { get; set; }

    /// <summary>Porcentaje de lo cobrado (sin ITBIS) que le toca al barbero. Columna nueva: ver Database/Migraciones.</summary>
    public decimal ComisionPorcentaje { get; set; } = 50;

    public DateTime FechaRegistro { get; set; }

    public int BarberoId { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<HorariosBarbero> HorariosBarberos { get; set; } = new List<HorariosBarbero>();

    public virtual ICollection<IndisponibilidadBarbero> IndisponibilidadBarberos { get; set; } = new List<IndisponibilidadBarbero>();

    public virtual Usuario Usuario { get; set; } = null!;
}
