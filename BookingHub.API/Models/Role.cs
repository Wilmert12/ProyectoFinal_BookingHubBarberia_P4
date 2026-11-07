using System;
using System.Collections.Generic;

namespace BookingHub.API.Models;

public partial class Role
{
    public string Descripcion { get; set; } = null!;

    public int RolId { get; set; }

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
