using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Services;

/// <summary>Funciones de perfil, barberos favoritos y reseñas vinculadas a citas reales.</summary>
public sealed class ExperienciaClienteService(IDbContextFactory<BookingHubContext> contextFactory)
{
    public async Task<Result<ClienteDto?>> Perfil(int clienteId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var perfil = await db.Clientes.AsNoTracking().Where(c => c.ClienteId == clienteId && c.Activo)
            .Select(c => new ClienteDto
            {
                ClienteId = c.ClienteId,
                Nombre = c.Nombre,
                Telefono = c.Telefono,
                Email = c.Email,
                Activo = c.Activo
            }).FirstOrDefaultAsync();
        return Result.Success<ClienteDto?>(perfil);
    }

    public async Task<Result<ClienteDto>> ActualizarPerfil(int clienteId, ActualizarPerfilClienteRequest request)
    {
        var nombre = request.Nombre?.Trim();
        var telefono = request.Telefono?.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 100)
            return Fallo<ClienteDto>("PERFIL_NOMBRE", "El nombre es obligatorio y admite hasta 100 caracteres.");
        if (string.IsNullOrWhiteSpace(telefono) || telefono.Length > 20)
            return Fallo<ClienteDto>("PERFIL_TELEFONO", "El teléfono es obligatorio y admite hasta 20 caracteres.");
        if (email is not null && (email.Length > 255 || !email.Contains('@')))
            return Fallo<ClienteDto>("PERFIL_EMAIL", "El correo electrónico no es válido.");

        await using var db = await contextFactory.CreateDbContextAsync();
        var emailNormalizado = email?.ToLowerInvariant();
        var duplicado = emailNormalizado is not null && await db.Clientes.AnyAsync(c => c.ClienteId != clienteId &&
            c.Activo && c.EmailNormalizado == emailNormalizado);
        if (duplicado) return Fallo<ClienteDto>("CLIENTE_EMAIL", "Ese correo electrónico ya pertenece a otra cuenta.");

        var actualizado = await db.Clientes.Where(c => c.ClienteId == clienteId && c.Activo)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Nombre, nombre)
                .SetProperty(c => c.Telefono, telefono)
                .SetProperty(c => c.Email, email)
                .SetProperty(c => c.EmailNormalizado, emailNormalizado));
        if (actualizado == 0) return Fallo<ClienteDto>("CLIENTE_NO_EXISTE", "No se encontró una cuenta activa.");
        return Result.Success(new ClienteDto
        {
            ClienteId = clienteId,
            Nombre = nombre,
            Telefono = telefono,
            Email = email,
            Activo = true
        });
    }

    public async Task<Result<bool>> GuardarFoto(int clienteId, byte[] contenido)
    {
        if (contenido.Length is < 4 or > 1_000_000)
            return Fallo<bool>("PERFIL_FOTO", "La foto debe pesar hasta 1 MB.");
        var jpeg = contenido.Length >= 3 && contenido[0] == 0xFF && contenido[1] == 0xD8 && contenido[2] == 0xFF;
        var png = contenido.Length >= 8 && contenido.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        if (!jpeg && !png) return Fallo<bool>("PERFIL_FOTO", "La imagen debe estar en formato JPEG o PNG.");

        await using var db = await contextFactory.CreateDbContextAsync();
        var guardada = await db.Clientes.Where(c => c.ClienteId == clienteId && c.Activo)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.FotoPerfil, contenido));
        return guardada == 0
            ? Fallo<bool>("CLIENTE_NO_EXISTE", "No se encontró una cuenta activa.")
            : Result.Success(true);
    }

    public async Task<byte[]?> ObtenerFoto(int clienteId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Clientes.AsNoTracking().Where(c => c.ClienteId == clienteId && c.Activo)
            .Select(c => c.FotoPerfil).FirstOrDefaultAsync();
    }

    public async Task<Result<List<BarberoDto>>> Favoritos(int clienteId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var favoritos = await db.FavoritosBarberos.AsNoTracking()
            .Where(f => f.ClienteId == clienteId && f.Barbero.Activo)
            .Select(f => new BarberoDto
            {
                BarberoId = f.Barbero.BarberoId,
                UsuarioId = f.Barbero.UsuarioId,
                Nombre = f.Barbero.Usuario.Nombre,
                Apodo = f.Barbero.Apodo,
                ColorHex = f.Barbero.ColorHex,
                Activo = f.Barbero.Activo,
                CalificacionPromedio = f.Barbero.Resenas.Average(r => (decimal?)r.Puntaje) ?? 0,
                TotalResenas = f.Barbero.Resenas.Count,
                Especialidades = f.Barbero.Especialidades
            }).ToListAsync();
        return Result.Success(favoritos);
    }

    public async Task<Result<bool>> AgregarFavorito(int clienteId, int barberoId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        if (!await db.Clientes.AnyAsync(c => c.ClienteId == clienteId && c.Activo))
            return Fallo<bool>("CLIENTE_NO_EXISTE", "No se encontró una cuenta activa.");
        if (!await db.Barberos.AnyAsync(b => b.BarberoId == barberoId && b.Activo))
            return Fallo<bool>("BARBERO_NO_DISPONIBLE", "No se encontró un barbero activo.");
        if (await db.FavoritosBarberos.AnyAsync(f => f.ClienteId == clienteId && f.BarberoId == barberoId))
            return Result.Success(true);
        db.FavoritosBarberos.Add(new FavoritoBarbero { ClienteId = clienteId, BarberoId = barberoId });
        await db.SaveChangesAsync();
        return Result.Success(true);
    }

    public async Task<Result<bool>> QuitarFavorito(int clienteId, int barberoId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        await db.FavoritosBarberos.Where(f => f.ClienteId == clienteId && f.BarberoId == barberoId).ExecuteDeleteAsync();
        return Result.Success(true);
    }

    public async Task<Result<ResenaDto>> CrearResena(int clienteId, CrearResenaRequest request)
    {
        if (request.Puntaje is < 1 or > 5)
            return Fallo<ResenaDto>("RESENA_PUNTAJE", "La calificación debe estar entre 1 y 5 estrellas.");
        var comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim();
        if (comentario?.Length > 500)
            return Fallo<ResenaDto>("RESENA_COMENTARIO", "El comentario admite hasta 500 caracteres.");

        await using var db = await contextFactory.CreateDbContextAsync();
        var cita = await db.Citas.Include(c => c.EstadoCita).Include(c => c.Barbero).ThenInclude(b => b.Usuario)
            .FirstOrDefaultAsync(c => c.CitaId == request.CitaId && c.ClienteId == clienteId);
        if (cita is null) return Fallo<ResenaDto>("CITA_NO_EXISTE", "No se encontró una cita de tu cuenta.");
        if (!cita.EstadoCita.Descripcion.Equals("Completada", StringComparison.OrdinalIgnoreCase))
            return Fallo<ResenaDto>("RESENA_CITA_PENDIENTE", "Solo puedes reseñar una cita completada.");
        if (await db.Resenas.AnyAsync(r => r.CitaId == request.CitaId))
            return Fallo<ResenaDto>("RESENA_DUPLICADA", "Esta cita ya tiene una reseña.");

        var resena = new Resena
        {
            CitaId = cita.CitaId,
            ClienteId = clienteId,
            BarberoId = cita.BarberoId,
            Puntaje = (byte)request.Puntaje,
            Comentario = comentario
        };
        db.Resenas.Add(resena);
        await db.SaveChangesAsync();
        return Result.Success(new ResenaDto(resena.ResenaId, resena.CitaId, resena.BarberoId,
            cita.Barbero.Usuario.Nombre, resena.Puntaje, resena.Comentario, resena.FechaRegistro));
    }

    public async Task<Result<List<ResenaDto>>> ResenasDeBarbero(int barberoId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var existe = await db.Barberos.AnyAsync(b => b.BarberoId == barberoId && b.Activo);
        if (!existe) return Fallo<List<ResenaDto>>("BARBERO_NO_EXISTE", "No se encontró un barbero activo.");
        var resenas = await db.Resenas.AsNoTracking().Where(r => r.BarberoId == barberoId)
            .OrderByDescending(r => r.FechaRegistro)
            .Select(r => new ResenaDto(r.ResenaId, r.CitaId, r.BarberoId, r.Barbero.Usuario.Nombre,
                r.Puntaje, r.Comentario, r.FechaRegistro)).ToListAsync();
        return Result.Success(resenas);
    }

    private static Result<T> Fallo<T>(string codigo, string mensaje) => Result.Failure<T>(new Error(codigo, mensaje));
}
