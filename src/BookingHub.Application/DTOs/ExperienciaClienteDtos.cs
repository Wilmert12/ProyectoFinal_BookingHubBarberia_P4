namespace BookingHub.Application.DTOs;

public sealed record ActualizarPerfilClienteRequest(string Nombre, string Telefono, string? Email);

public sealed record CrearResenaRequest(int CitaId, int Puntaje, string? Comentario);

public sealed record ResenaDto(
    int ResenaId,
    int CitaId,
    int BarberoId,
    string BarberoNombre,
    int Puntaje,
    string? Comentario,
    DateTime FechaRegistro);
