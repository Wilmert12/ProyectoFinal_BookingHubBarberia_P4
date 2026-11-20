using System.Security.Cryptography;
using System.Text;

namespace BookingHub.Application.Services;

public static class HashTokenAuth
{
    public static string Calcular(string valor) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));
}
