using System;
using System.Collections.Generic;

namespace BookingHub.Infrastructure.Models;

public partial class Barbero
{
    public int UsuarioId { get; set; }

    public string? Apodo { get; set; }

    public string? Especialidades { get; set; }

    public string Cedula { get; set; } = null!;

    public string ColorHex { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int BarberoId { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<HorariosBarbero> HorariosBarberos { get; set; } = new List<HorariosBarbero>();

    public virtual ICollection<IndisponibilidadBarbero> IndisponibilidadBarberos { get; set; } = new List<IndisponibilidadBarbero>();

    public virtual ICollection<FavoritoBarbero> ClientesFavoritos { get; set; } = new List<FavoritoBarbero>();

    public virtual ICollection<Resena> Resenas { get; set; } = new List<Resena>();

    public virtual Usuario Usuario { get; set; } = null!;
}
