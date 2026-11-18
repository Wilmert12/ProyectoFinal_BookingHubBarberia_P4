using BookingHub.WinForms.Api;
using BookingHub.WinForms.Formularios;
using BookingHub.WinForms.Infraestructura;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace BookingHub.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var configuracion = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        var apiBaseUrl = new Uri(configuracion["ApiBaseUrl"] ?? "https://localhost:7109/");

        var servicios = new ServiceCollection();
        servicios.AddSingleton<Sesion>();
        servicios.AddTransient<AutenticacionHandler>();
        AgregarApi<IAuthApi>(servicios, apiBaseUrl);
        AgregarApi<ICitaApi>(servicios, apiBaseUrl);
        AgregarApi<IClienteApi>(servicios, apiBaseUrl);
        AgregarApi<IBarberoApi>(servicios, apiBaseUrl);
        AgregarApi<ICatalogoItemApi>(servicios, apiBaseUrl);
        AgregarApi<IPagoApi>(servicios, apiBaseUrl);
        AgregarApi<ICatalogosApi>(servicios, apiBaseUrl);
        AgregarApi<IReporteApi>(servicios, apiBaseUrl);
        AgregarApi<IUsuarioApi>(servicios, apiBaseUrl);
        servicios.AddSingleton<Apis>();

        using var proveedor = servicios.BuildServiceProvider();
        var apis = proveedor.GetRequiredService<Apis>();
        var sesion = proveedor.GetRequiredService<Sesion>();

        while (true)
        {
            using var login = new FrmLogin(apis, sesion);
            if (login.ShowDialog() != DialogResult.OK)
                break;

            using var principal = new FrmPrincipal(apis, sesion);
            Application.Run(principal);
            if (!principal.CerroSesion)
                break;

            sesion.Cerrar();
        }
    }

    private static void AgregarApi<T>(IServiceCollection servicios, Uri apiBaseUrl) where T : class
    {
        servicios.AddRefitGeneratedClient<T>()
            .ConfigureHttpClient(cliente =>
            {
                cliente.BaseAddress = apiBaseUrl;
                cliente.Timeout = TimeSpan.FromSeconds(60);
            })
            .AddHttpMessageHandler<AutenticacionHandler>();
    }
}
