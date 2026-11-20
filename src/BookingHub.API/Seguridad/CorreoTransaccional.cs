using System.Net;
using System.Net.Mail;
using BookingHub.Application.Services;

namespace BookingHub.API.Seguridad;

public sealed class CorreoTransaccional(IConfiguration configuration) : ICorreoTransaccional
{
    private string? Host => configuration["Email:SmtpHost"];
    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(Host) &&
                                   !string.IsNullOrWhiteSpace(configuration["Email:From"]);

    public async Task Enviar(string destinatario, string asunto, string contenido, CancellationToken cancellationToken)
    {
        if (!EstaConfigurado) throw new InvalidOperationException("El correo transaccional no está configurado.");
        var from = configuration["Email:From"]!;
        using var message = new MailMessage(from, destinatario, asunto, contenido) { IsBodyHtml = false };
        using var client = new SmtpClient(Host!, int.TryParse(configuration["Email:Port"], out var port) ? port : 587)
        {
            EnableSsl = !bool.TryParse(configuration["Email:EnableSsl"], out var ssl) || ssl,
            Timeout = 15000
        };

        var user = configuration["Email:User"];
        var password = configuration["Email:Password"];
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new NetworkCredential(user, password);
        await client.SendMailAsync(message, cancellationToken);
    }
}
