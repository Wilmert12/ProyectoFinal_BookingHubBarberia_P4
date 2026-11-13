using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.UnitTests.Infraestructura;

public sealed class BaseDeDatosDePrueba : IDisposable
{
    private readonly SqliteConnection _conexion;

    public IDbContextFactory<BookingHubContext> Factory { get; }

    public BaseDeDatosDePrueba()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        var opciones = new DbContextOptionsBuilder<BookingHubContext>()
            .UseSqlite(_conexion)
            .Options;

        Factory = new ContextoDePruebaFactory(opciones);

        using var contexto = Factory.CreateDbContext();
        contexto.Database.EnsureCreated();
        Sembrar(contexto);
    }

    public BookingHubContext CrearContexto() => Factory.CreateDbContext();

    private static void Sembrar(BookingHubContext contexto)
    {
        var rol = new Role { Descripcion = "Barbero" };
        contexto.Roles.Add(rol);

        var usuarioCarlos = new Usuario { Nombre = "Carlos Pérez", Email = "carlos@test.com", PasswordHash = "hash", Rol = rol };
        var usuarioLuis = new Usuario { Nombre = "Luis Gómez", Email = "luis@test.com", PasswordHash = "hash", Rol = rol };
        contexto.Usuarios.AddRange(usuarioCarlos, usuarioLuis);

        contexto.Barberos.AddRange(
            new Barbero { Usuario = usuarioCarlos, Apodo = "El Fade", Cedula = "00100000001", ColorHex = "#1E88E5", Activo = true },
            new Barbero { Usuario = usuarioLuis, Apodo = "Barbas", Cedula = "00100000002", ColorHex = "#43A047", Activo = true });

        contexto.Clientes.AddRange(
            new Cliente { Nombre = "Cliente Demo", Telefono = "8091234567", Email = "cliente@test.com", Notas = "Prefiere la mañana", Activo = true },
            new Cliente { Nombre = "Cliente Inactivo", Telefono = "8097654321", Activo = false });

        contexto.EstadosCita.AddRange(
            new EstadosCitum { Descripcion = "Pendiente" },
            new EstadosCitum { Descripcion = "Cancelada", LiberaHorario = true });

        contexto.SaveChanges();
    }

    public void Dispose() => _conexion.Dispose();

    private sealed class ContextoDePruebaFactory(DbContextOptions<BookingHubContext> opciones)
        : IDbContextFactory<BookingHubContext>
    {
        public BookingHubContext CreateDbContext() => new ContextoSqlite(opciones);
    }

    private sealed class ContextoSqlite(DbContextOptions<BookingHubContext> opciones) : BookingHubContext(opciones)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entidad in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var restriccion in entidad.GetCheckConstraints().ToList())
                    entidad.RemoveCheckConstraint(restriccion.ModelName);

                foreach (var propiedad in entidad.GetProperties())
                    if (propiedad.GetDefaultValueSql() is not null)
                        propiedad.SetDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }
    }
}
