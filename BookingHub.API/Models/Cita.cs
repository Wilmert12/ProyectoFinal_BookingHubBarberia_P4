using System;
using System.Collections.Generic;

namespace BookingHub.API.Models;

public partial class Cita
{
    public int ClienteId { get; set; }

    public int BarberoId { get; set; }

    public int EstadoCitaId { get; set; }

    public decimal Total { get; set; }

    public string? Notas { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int CitaId { get; set; }

    public virtual Barbero Barbero { get; set; } = null!;

    public virtual ICollection<CitaDetalle> CitaDetalles { get; set; } = new List<CitaDetalle>();

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual EstadosCitum EstadoCita { get; set; } = null!;

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
