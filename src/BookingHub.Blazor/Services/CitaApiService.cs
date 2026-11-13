using System.Net.Http.Json;
using BookingHub.Blazor.Models;

namespace BookingHub.Blazor.Services;

public class CitaApiService(HttpClient httpClient)
{
    public async Task<List<CitaDto>> GetCitasDelCliente(int clienteId)
    {
        var citas = await httpClient.GetFromJsonAsync<List<CitaDto>>($"api/Citas?clienteId={clienteId}");
        return citas ?? [];
    }
}
