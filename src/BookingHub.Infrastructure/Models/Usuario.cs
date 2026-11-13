using System;
using System.Collections.Generic;

namespace BookingHub.Infrastructure.Models;

public partial class Usuario
{
    public string Nombre { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Telefono { get; set; }

    public int RolId { get; set; }

    public bool Activo { get; set; }

    public byte[]? FotoPerfil { get; set; }

    public DateTime? UltimoLogin { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int UsuarioId { get; set; }

    public virtual Barbero? Barbero { get; set; }

    public virtual Role Rol { get; set; } = null!;
}
