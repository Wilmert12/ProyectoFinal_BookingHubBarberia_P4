using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BookingHub.Blazor;
using BookingHub.Blazor.Services;
using Refit;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

// Singleton: los handlers de HttpClient se crean en otro scope y deben ver la misma sesión.
builder.Services.AddSingleton<SesionUsuario>();
builder.Services.AddTransient<AutenticacionHandler>();

builder.Services.AddRefitGeneratedClient<IAuthApi>()
    .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddRefitGeneratedClient<ICitaApi>()
    .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AutenticacionHandler>();
builder.Services.AddRefitGeneratedClient<IBarberoApi>()
    .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddRefitGeneratedClient<IClienteApi>()
    .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddRefitGeneratedClient<ICatalogoItemApi>()
    .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddSingleton<SesionCliente>();

await builder.Build().RunAsync();
