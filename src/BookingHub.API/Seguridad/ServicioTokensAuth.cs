using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using BookingHub.Infrastructure.Data;
using BookingHub.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BookingHub.API.Seguridad;

public sealed class ServicioTokensAuth(IDbContextFactory<BookingHubContext> contextFactory)
{
    private const string TipoRefresh = "Refresh";
    private static readonly TimeSpan DuracionRefresh = TimeSpan.FromDays(30);

    public async Task<string> CrearParaCliente(int clienteId, CancellationToken cancellationToken = default) =>
        await Crear(clienteId, null, cancellationToken);

    public async Task<string> CrearParaUsuario(int usuarioId, CancellationToken cancellationToken = default) =>
        await Crear(null, usuarioId, cancellationToken);

    private async Task<string> Crear(int? clienteId, int? usuarioId, CancellationToken cancellationToken)
    {
        var raw = CrearTokenAleatorio();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.AuthTokens.Add(new AuthToken
        {
            ClienteId = clienteId,
            UsuarioId = usuarioId,
            Tipo = TipoRefresh,
            TokenHash = HashTokenAuth.Calcular(raw),
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.Add(DuracionRefresh)
        });
        await db.SaveChangesAsync(cancellationToken);
        return raw;
    }

    public async Task<RefreshRotado?> Rotar(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var hashActual = HashTokenAuth.Calcular(token);
        var nuevoToken = CrearTokenAleatorio();
        var nuevoHash = HashTokenAuth.Calcular(nuevoToken);
        var ahora = DateTime.UtcNow;

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var registro = await db.AuthTokens.AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == hashActual && t.Tipo == TipoRefresh, cancellationToken);
        if (registro is null || registro.FechaExpiracion <= ahora) return null;
        if (registro.FechaRevocacion is not null)
        {
            if (registro.ReemplazadoPorHash is not null)
            {
                await db.AuthTokens.Where(t => t.Tipo == TipoRefresh && t.FechaRevocacion == null &&
                        (registro.ClienteId != null ? t.ClienteId == registro.ClienteId : t.UsuarioId == registro.UsuarioId))
                    .ExecuteUpdateAsync(set => set.SetProperty(t => t.FechaRevocacion, ahora), cancellationToken);
            }
            return null;
        }

        IdentidadAutenticada identidad;
        if (registro.ClienteId is int clienteId)
        {
            var cliente = await db.Clientes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.Activo && c.PasswordHash != null, cancellationToken);
            if (cliente is null || cliente.Email is null) return null;
            identidad = new IdentidadAutenticada(null, cliente.ClienteId, cliente.Nombre, cliente.Email, "Cliente");
        }
        else if (registro.UsuarioId is int usuarioId)
        {
            var usuario = await db.Usuarios.AsNoTracking().Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId && u.Activo, cancellationToken);
            if (usuario is null) return null;
            identidad = new IdentidadAutenticada(usuario.UsuarioId, null, usuario.Nombre, usuario.Email, usuario.Rol.Descripcion);
        }
        else return null;

        // La actualización condicional garantiza que dos peticiones concurrentes no puedan reutilizar el refresh.
        var reclamado = await db.AuthTokens
            .Where(t => t.AuthTokenId == registro.AuthTokenId && t.FechaRevocacion == null && t.FechaExpiracion > ahora)
            .ExecuteUpdateAsync(set => set
                .SetProperty(t => t.FechaRevocacion, ahora)
                .SetProperty(t => t.ReemplazadoPorHash, nuevoHash), cancellationToken);
        if (reclamado != 1) return null;

        db.AuthTokens.Add(new AuthToken
        {
            ClienteId = registro.ClienteId,
            UsuarioId = registro.UsuarioId,
            Tipo = TipoRefresh,
            TokenHash = nuevoHash,
            FechaCreacion = ahora,
            FechaExpiracion = ahora.Add(DuracionRefresh)
        });
        await db.SaveChangesAsync(cancellationToken);
        return new RefreshRotado(identidad, nuevoToken);
    }

    private static string CrearTokenAleatorio() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
