using BookingHub.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookingHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CitaService>();
        services.AddScoped<BarberoService>();
        services.AddScoped<ClienteService>();
        services.AddScoped<CatalogoItemService>();

        return services;
    }
}
