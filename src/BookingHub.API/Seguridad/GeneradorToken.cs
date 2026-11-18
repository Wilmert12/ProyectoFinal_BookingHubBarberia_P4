using BookingHub.Application.DTOs;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace BookingHub.API.Seguridad;

public class GeneradorToken(SymmetricSecurityKey clave)
{
    public const string Emisor = "BookingHub";
    public static readonly TimeSpan Duracion = TimeSpan.FromHours(8);

    public SesionDto Crear(UsuarioSesionDto usuario)
    {
        var expira = DateTime.UtcNow.Add(Duracion);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Emisor,
            Audience = Emisor,
            Expires = expira,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", usuario.UsuarioId.ToString()),
                new Claim("name", usuario.Nombre),
                new Claim("email", usuario.Email),
                new Claim("role", usuario.Rol)
            ]),
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new SesionDto(token, expira, usuario.UsuarioId, usuario.Nombre, usuario.Email, usuario.Rol);
    }
}
