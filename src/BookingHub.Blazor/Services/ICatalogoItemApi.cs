using BookingHub.Blazor.Models;
using Refit;

namespace BookingHub.Blazor.Services;

public interface ICatalogoItemApi
{
    [Get("/api/CatalogoItems")]
    Task<List<CatalogoItemDto>> GetCatalogoItems();
}
