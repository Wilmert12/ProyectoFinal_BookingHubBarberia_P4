using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace BookingHub.API.Seguridad;

public class GeneradorToken(SymmetricSecurityKey clave)
{
    public const string Emisor = "CerquilloApp";
    public static readonly TimeSpan Duracion = TimeSpan.FromHours(8);
    public static readonly TimeSpan DuracionCliente = TimeSpan.FromMinutes(15);

    public SesionDto Crear(UsuarioSesionDto usuario, string? refreshToken = null)
    {
        var expira = DateTime.UtcNow.Add(Duracion);
        var token = CrearToken(usuario.UsuarioId, usuario.Nombre, usuario.Email, usuario.Rol, expira);
        return new SesionDto(token, expira, usuario.UsuarioId, usuario.Nombre, usuario.Email, usuario.Rol, refreshToken);
    }

    public SesionClienteDto CrearCliente(Cliente cliente, string refreshToken)
    {
        var expira = DateTime.UtcNow.Add(DuracionCliente);
        var token = CrearToken(cliente.ClienteId, cliente.Nombre, cliente.Email!, "Cliente", expira);
        return new SesionClienteDto(token, expira, cliente.ClienteId, cliente.Nombre, cliente.Email!, "Cliente", refreshToken);
    }

    public string CrearToken(IdentidadAutenticada identidad, out DateTime expira)
    {
        expira = DateTime.UtcNow.Add(identidad.Rol.Equals("Cliente", StringComparison.OrdinalIgnoreCase)
            ? DuracionCliente
            : Duracion);
        return CrearToken(identidad.ClienteId ?? identidad.UsuarioId!.Value, identidad.Nombre,
            identidad.Email, identidad.Rol, expira);
    }

    private string CrearToken(int id, string nombre, string email, string rol, DateTime expira)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Emisor,
            Audience = Emisor,
            Expires = expira,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", id.ToString()),
                new Claim("name", nombre),
                new Claim("email", email),
                new Claim("role", rol)
            ]),
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
