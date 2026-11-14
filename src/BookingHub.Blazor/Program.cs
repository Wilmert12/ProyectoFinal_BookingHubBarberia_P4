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

await builder.Build().RunAsync();
