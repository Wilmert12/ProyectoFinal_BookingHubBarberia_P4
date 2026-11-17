using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BookingHub.Blazor;
using BookingHub.Blazor.Services;
using Refit;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddRefitGeneratedClient<ICitaApi>()
    .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddRefitGeneratedClient<IBarberoApi>()
    .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddRefitGeneratedClient<IClienteApi>()
    .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddRefitGeneratedClient<ICatalogoItemApi>()
    .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddScoped<SesionCliente>();

await builder.Build().RunAsync();
