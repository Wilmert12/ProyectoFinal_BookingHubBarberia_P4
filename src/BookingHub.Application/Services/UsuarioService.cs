using Aplicada1.Core;
using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BookingHub.Application.Services;

public class UsuarioService(
    IDbContextFactory<BookingHubContext> contextFactory
) : Aplicada1.Core.IServiceResult<UsuarioDto, int>
{
    public const int LargoMinimoPassword = 8;

    private static readonly PasswordHasher<Usuario> Hasher = new();

    private static readonly Expression<Func<Usuario, UsuarioDto>> AUsuarioDto = u => new UsuarioDto
    {
        UsuarioId = u.UsuarioId,
        Nombre = u.Nombre,
        Email = u.Email,
        Telefono = u.Telefono,
        RolId = u.RolId,
        Rol = u.Rol.Descripcion,
        Activo = u.Activo,
        UltimoLogin = u.UltimoLogin
    };

    public async Task<Result> Guardar(UsuarioDto usuario)
    {
        var esNuevo = !await Existe(usuario.UsuarioId);

        if (string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Email))
            return Result.Failure(new Error("USUARIO_DATOS", "El nombre y el correo son obligatorios."));
        if (esNuevo && string.IsNullOrEmpty(usuario.Password))
            return Result.Failure(new Error("USUARIO_PASSWORD", "La contraseña es obligatoria para un usuario nuevo."));
        if (!string.IsNullOrEmpty(usuario.Password) && usuario.Password.Length < LargoMinimoPassword)
            return Result.Failure(new Error("USUARIO_PASSWORD", $"La contraseña debe tener al menos {LargoMinimoPassword} caracteres."));
        if (await EmailEnUso(usuario.Email, usuario.UsuarioId))
            return Result.Failure(new Error("USUARIO_EMAIL", "Ya existe un usuario con ese correo."));

        var guardo = esNuevo ? await Insertar(usuario) : await Modificar(usuario);
        usuario.Password = null;
        return Result.Success(guardo);
    }

    private async Task<bool> Existe(int usuarioId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Usuarios
            .AnyAsync(u => u.UsuarioId == usuarioId);
    }

    private async Task<bool> EmailEnUso(string email, int usuarioId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var correo = email.Trim().ToLower();
        return await contexto.Usuarios
            .AnyAsync(u => u.Email.ToLower() == correo && u.UsuarioId != usuarioId);
    }

    private async Task<bool> Insertar(UsuarioDto usuarioDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var usuario = new Usuario
        {
            Nombre = usuarioDto.Nombre.Trim(),
            Email = usuarioDto.Email.Trim(),
            Telefono = usuarioDto.Telefono,
            RolId = usuarioDto.RolId,
            Activo = usuarioDto.Activo
        };
        usuario.PasswordHash = Hasher.HashPassword(usuario, usuarioDto.Password!);
        contexto.Usuarios.Add(usuario);
        var inserto = await contexto.SaveChangesAsync() > 0;
        usuarioDto.UsuarioId = usuario.UsuarioId;
        return inserto;
    }

    private async Task<bool> Modificar(UsuarioDto usuarioDto)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        var usuario = await contexto.Usuarios.FindAsync(usuarioDto.UsuarioId);
        if (usuario is null)
            return false;

        usuario.Nombre = usuarioDto.Nombre.Trim();
        usuario.Email = usuarioDto.Email.Trim();
        usuario.Telefono = usuarioDto.Telefono;
        usuario.RolId = usuarioDto.RolId;
        usuario.Activo = usuarioDto.Activo;
        if (!string.IsNullOrEmpty(usuarioDto.Password))
            usuario.PasswordHash = Hasher.HashPassword(usuario, usuarioDto.Password);

        await contexto.SaveChangesAsync();
        return true;
    }

    public async Task<Result<UsuarioDto?>> Buscar(int usuarioId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.UsuarioId == usuarioId)
            .Select(AUsuarioDto)
            .FirstOrDefaultAsync();
    }

    public async Task<Result<List<UsuarioDto>>> GetList(Expression<Func<UsuarioDto, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();
        return await contexto.Usuarios
            .AsNoTracking()
            .Select(AUsuarioDto)
            .Where(criterio)
            .OrderBy(u => u.Nombre)
            .ToListAsync();
    }

    public async Task<Result> Eliminar(int usuarioId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        if (await contexto.Barberos.AnyAsync(b => b.UsuarioId == usuarioId))
            return Result.Failure(new Error("USUARIO_ES_BARBERO", "El usuario está ligado a un barbero; desactívalo en lugar de eliminarlo."));

        var eliminado = await contexto.Usuarios
            .Where(u => u.UsuarioId == usuarioId)
            .ExecuteDeleteAsync() > 0;
        return Result.Success(eliminado);
    }

    public async Task<Result<UsuarioSesionDto>> IniciarSesion(LoginDto login)
    {
        var fallo = Result.Failure<UsuarioSesionDto>(new Error("LOGIN_INVALIDO", "Correo o contraseña incorrectos."));
        if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrEmpty(login.Password))
            return fallo;

        await using var contexto = await contextFactory.CreateDbContextAsync();
        var correo = login.Email.Trim().ToLower();
        var usuario = await contexto.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == correo);

        if (usuario is null || !usuario.Activo)
            return fallo;

        PasswordVerificationResult verificacion;
        try
        {
            verificacion = Hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, login.Password);
        }
        catch (FormatException)
        {
            return fallo;
        }

        if (verificacion == PasswordVerificationResult.Failed)
            return fallo;

        if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
            usuario.PasswordHash = Hasher.HashPassword(usuario, login.Password);
        usuario.UltimoLogin = DateTime.Now;
        await contexto.SaveChangesAsync();

        return Result.Success(new UsuarioSesionDto(usuario.UsuarioId, usuario.Nombre, usuario.Email, usuario.Rol.Descripcion));
    }
}
