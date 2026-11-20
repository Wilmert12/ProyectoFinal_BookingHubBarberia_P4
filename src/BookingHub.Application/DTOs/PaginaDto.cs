namespace BookingHub.Application.DTOs;

public sealed record PaginaDto<T>(
    IReadOnlyList<T> Items,
    int Pagina,
    int Tamano,
    bool TieneMas);
