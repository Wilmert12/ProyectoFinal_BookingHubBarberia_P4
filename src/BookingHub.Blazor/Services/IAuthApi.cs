using BookingHub.Blazor.Models;
using Refit;

namespace BookingHub.Blazor.Services;

public interface IAuthApi
{
    [Post("/api/Auth/login")]
    Task<SesionDto> Login([Body] LoginDto login);
}
