namespace BookingHub.Application.DTOs;

public sealed record InformacionBarberiaDto(
    string Nombre,
    string Direccion,
    string Telefono,
    string WhatsApp,
    string Horario,
    string UrlMapa);
