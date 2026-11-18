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
        services.AddScoped<PagoService>();
        services.AddScoped<CatalogosService>();
        services.AddScoped<ReporteService>();
        services.AddScoped<UsuarioService>();

        return services;
    }
}
