using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.API.Models;

public partial class BookingHubContext : DbContext
{
    public BookingHubContext()
    {
    }

    public BookingHubContext(DbContextOptions<BookingHubContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Barbero> Barberos { get; set; }

    public virtual DbSet<CatalogoItem> CatalogoItems { get; set; }

    public virtual DbSet<Cita> Citas { get; set; }

    public virtual DbSet<CitaDetalle> CitaDetalles { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<EstadosCitum> EstadosCita { get; set; }

    public virtual DbSet<HorariosBarbero> HorariosBarberos { get; set; }

    public virtual DbSet<IndisponibilidadBarbero> IndisponibilidadBarberos { get; set; }

    public virtual DbSet<MetodosPago> MetodosPagos { get; set; }

    public virtual DbSet<Pago> Pagos { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TiposIndisponibilidad> TiposIndisponibilidads { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Barbero>(entity =>
        {
            entity.HasKey(e => e.BarberoId).HasName("PK__Barberos__A416D4C3C62749FB");

            entity.HasIndex(e => e.Cedula, "UQ__Barberos__415B7BE5270817B2").IsUnique();

            entity.HasIndex(e => e.UsuarioId, "UQ__Barberos__A5B1AB8F0F5A7797").IsUnique();

            entity.Property(e => e.BarberoId).HasColumnName("barberoId");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Apodo)
                .HasMaxLength(50)
                .HasColumnName("apodo");
            entity.Property(e => e.Cedula)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasColumnName("cedula");
            entity.Property(e => e.ColorHex)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValue("#000000")
                .IsFixedLength()
                .HasColumnName("colorHex");
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fechaRegistro");
            entity.Property(e => e.UsuarioId).HasColumnName("usuarioId");

            entity.HasOne(d => d.Usuario).WithOne(p => p.Barbero)
                .HasForeignKey<Barbero>(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Barberos__usuari__4CA06362");
        });

        modelBuilder.Entity<CatalogoItem>(entity =>
        {
            entity.HasKey(e => e.CatalogoItemId).HasName("PK__Catalogo__82EA9348DAA509E1");

            entity.Property(e => e.CatalogoItemId).HasColumnName("catalogoItemId");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .HasColumnName("descripcion");
            entity.Property(e => e.DuracionMin).HasColumnName("duracionMin");
            entity.Property(e => e.EsServicio)
                .HasDefaultValue(true)
                .HasColumnName("esServicio");
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fechaRegistro");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Precio)
                .HasColumnType("money")
                .HasColumnName("precio");
        });

        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasKey(e => e.CitaId).HasName("PK__Citas__A9534E0CC61306E7");

            entity.HasIndex(e => new { e.BarberoId, e.FechaInicio, e.FechaFin }, "IX_Citas_Barbero_Fecha");

            entity.HasIndex(e => e.ClienteId, "IX_Citas_Cliente");

            entity.Property(e => e.CitaId).HasColumnName("citaId");
            entity.Property(e => e.BarberoId).HasColumnName("barberoId");
            entity.Property(e => e.ClienteId).HasColumnName("clienteId");
            entity.Property(e => e.EstadoCitaId).HasColumnName("estadoCitaId");
            entity.Property(e => e.FechaFin)
                .HasPrecision(0)
                .HasColumnName("fechaFin");
            entity.Property(e => e.FechaInicio)
                .HasPrecision(0)
                .HasColumnName("fechaInicio");
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fechaRegistro");
            entity.Property(e => e.Notas)
                .HasMaxLength(500)
                .HasColumnName("notas");
            entity.Property(e => e.Total)
                .HasColumnType("money")
                .HasColumnName("total");

            entity.HasOne(d => d.Barbero).WithMany(p => p.Cita)
                .HasForeignKey(d => d.BarberoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Citas__barberoId__6D0D32F4");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Cita)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Citas__clienteId__6C190EBB");

            entity.HasOne(d => d.EstadoCita).WithMany(p => p.Cita)
                .HasForeignKey(d => d.EstadoCitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Citas__estadoCit__6E01572D");
        });

        modelBuilder.Entity<CitaDetalle>(entity =>
        {
            entity.HasKey(e => e.CitaDetalleId).HasName("PK__CitaDeta__4EB3D73E5D51069B");

            entity.HasIndex(e => e.CitaId, "IX_CitaDetalles_Cita");

            entity.Property(e => e.CitaDetalleId).HasColumnName("citaDetalleId");
            entity.Property(e => e.Cantidad)
                .HasDefaultValue(1)
                .HasColumnName("cantidad");
            entity.Property(e => e.CatalogoItemId).HasColumnName("catalogoItemId");
            entity.Property(e => e.CitaId).HasColumnName("citaId");
            entity.Property(e => e.Descuento)
                .HasColumnType("money")
                .HasColumnName("descuento");
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fechaRegistro");
            entity.Property(e => e.PrecioUnitario)
                .HasColumnType("money")
                .HasColumnName("precioUnitario");

            entity.HasOne(d => d.CatalogoItem).WithMany(p => p.CitaDetalles)
                .HasForeignKey(d => d.CatalogoItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CitaDetal__catal__75A278F5");

            entity.HasOne(d => d.Cita).WithMany(p => p.CitaDetalles)
                .HasForeignKey(d => d.CitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CitaDetal__citaI__74AE54BC");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.ClienteId).HasName("PK__Clientes__C2FF245D2364C4BD");

            entity.HasIndex(e => e.Email, "IX_Clientes_Email")
                .IsUnique()
                .HasFilter("([email] IS NOT NULL)");

            entity.Property(e => e.ClienteId).HasColumnName("clienteId");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fechaRegistro");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Notas)
                .HasMaxLength(500)
                .HasColumnName("notas");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("telefono");
            entity.Property(e => e.TotalNoShows).HasColumnName("totalNoShows");
        });

        modelBuilder.Entity<EstadosCitum>(entity =>
        {
            entity.HasKey(e => e.EstadoCitaId).HasName("PK__EstadosC__31DF88F7F313F23C");

            entity.HasIndex(e => e.Descripcion, "UQ__EstadosC__298336B68A370D1C").IsUnique();

            entity.Property(e => e.EstadoCitaId).HasColumnName("estadoCitaId");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .HasColumnName("descripcion");
            entity.Property(e => e.LiberaHorario).HasColumnName("liberaHorario");
        });

        modelBuilder.Entity<HorariosBarbero>(entity =>
        {
            entity.HasKey(e => e.HorarioId).HasName("PK__Horarios__BFF858F3181405BA");

            entity.HasIndex(e => new { e.BarberoId, e.DiaSemana }, "IX_Horarios_Barbero_Dia");

            entity.Property(e => e.HorarioId).HasColumnName("horarioId");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.BarberoId).HasColumnName("barberoId");
            entity.Property(e => e.DiaSemana).HasColumnName("diaSemana");
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fechaRegistro");
            entity.Property(e => e.HoraFin)
                .HasPrecision(0)
                .HasColumnName("horaFin");
            entity.Property(e => e.HoraInicio)
                .HasPrecision(0)
                .HasColumnName("horaInicio");

            entity.HasOne(d => d.Barbero).WithMany(p => p.HorariosBarberos)
                .HasForeignKey(d => d.BarberoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HorariosB__barbe__5FB337D6");
        });

        modelBuilder.Entity<IndisponibilidadBarbero>(entity =>
        {
            entity.HasKey(e => e.IndisponibilidadId).HasName("PK__Indispon__265EEA1F810BFDCC");

            entity.HasIndex(e => new { e.BarberoId, e.FechaInicio, e.FechaFin }, "IX_Indisponibilidad_Barbero_Fecha");

            entity.Property(e => e.IndisponibilidadId).HasColumnName("indisponibilidadId");
            entity.Property(e => e.BarberoId).HasColumnName("barberoId");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .HasColumnName("descripcion");
            entity.Property(e => e.FechaFin)
                .HasPrecision(0)
                .HasColumnName("fechaFin");
            entity.Property(e => e.FechaInicio)
                .HasPrecision(0)
                .HasColumnName("fechaInicio");
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fechaRegistro");
            entity.Property(e => e.TipoIndisponibilidadId).HasColumnName("tipoIndisponibilidadId");

            entity.HasOne(d => d.Barbero).WithMany(p => p.IndisponibilidadBarberos)
                .HasForeignKey(d => d.BarberoId)
                .HasConstraintName("FK__Indisponi__barbe__66603565");

            entity.HasOne(d => d.TipoIndisponibilidad).WithMany(p => p.IndisponibilidadBarberos)
                .HasForeignKey(d => d.TipoIndisponibilidadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Indisponi__tipoI__6754599E");
        });

        modelBuilder.Entity<MetodosPago>(entity =>
        {
            entity.HasKey(e => e.MetodoPagoId).HasName("PK__MetodosP__FB2CE9535D64B37F");

            entity.ToTable("MetodosPago");

            entity.HasIndex(e => e.Descripcion, "UQ__MetodosP__298336B6600A352C").IsUnique();

            entity.Property(e => e.MetodoPagoId).HasColumnName("metodoPagoId");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.PagoId).HasName("PK__Pagos__5736FC0C87330DF0");

            entity.HasIndex(e => e.CitaId, "IX_Pagos_Cita");

            entity.Property(e => e.PagoId).HasColumnName("pagoId");
            entity.Property(e => e.CitaId).HasColumnName("citaId");
            entity.Property(e => e.FechaPago)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fechaPago");
            entity.Property(e => e.MetodoPagoId).HasColumnName("metodoPagoId");
            entity.Property(e => e.Monto)
                .HasColumnType("money")
                .HasColumnName("monto");
            entity.Property(e => e.Propina)
                .HasColumnType("money")
                .HasColumnName("propina");
            entity.Property(e => e.Referencia)
                .HasMaxLength(100)
                .HasColumnName("referencia");

            entity.HasOne(d => d.Cita).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.CitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Pagos__citaId__7E37BEF6");

            entity.HasOne(d => d.MetodoPago).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.MetodoPagoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Pagos__metodoPag__7F2BE32F");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("PK__Roles__54023634E1F692C9");

            entity.HasIndex(e => e.Descripcion, "UQ__Roles__298336B627D0CAB2").IsUnique();

            entity.Property(e => e.RolId).HasColumnName("rolId");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<TiposIndisponibilidad>(entity =>
        {
            entity.HasKey(e => e.TipoIndisponibilidadId).HasName("PK__TiposInd__BB957600C0D97137");

            entity.ToTable("TiposIndisponibilidad");

            entity.HasIndex(e => e.Descripcion, "UQ__TiposInd__298336B6C45F609E").IsUnique();

            entity.Property(e => e.TipoIndisponibilidadId).HasColumnName("tipoIndisponibilidadId");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("PK__Usuarios__A5B1AB8E3B3B8243");

            entity.HasIndex(e => e.Email, "UQ__Usuarios__AB6E6164B5D1948C").IsUnique();

            entity.Property(e => e.UsuarioId).HasColumnName("usuarioId");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fechaRegistro");
            entity.Property(e => e.FotoPerfil).HasColumnName("fotoPerfil");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("passwordHash");
            entity.Property(e => e.RolId).HasColumnName("rolId");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("telefono");
            entity.Property(e => e.UltimoLogin)
                .HasPrecision(0)
                .HasColumnName("ultimoLogin");

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuarios__rolId__45F365D3");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
