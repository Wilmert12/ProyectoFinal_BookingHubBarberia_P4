namespace BookingHub.Application.Services;

public interface ICorreoTransaccional
{
    bool EstaConfigurado { get; }
    Task Enviar(string destinatario, string asunto, string contenido, CancellationToken cancellationToken);
}
