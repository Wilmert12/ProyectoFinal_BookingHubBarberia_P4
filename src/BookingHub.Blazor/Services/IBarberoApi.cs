using BookingHub.Blazor.Models;
using Refit;

namespace BookingHub.Blazor.Services;

public interface IBarberoApi
{
    [Get("/api/Barberos")]
    Task<List<BarberoDto>> GetBarberos();
}
