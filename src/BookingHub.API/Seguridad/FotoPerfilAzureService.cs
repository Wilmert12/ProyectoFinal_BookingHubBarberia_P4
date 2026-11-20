using System.Net;
using System.Net.Http.Headers;

namespace BookingHub.API.Seguridad;

/// <summary>Almacena fotos en un contenedor Azure Blob privado usando un SAS de contenedor.</summary>
public sealed class FotoPerfilAzureService(IHttpClientFactory clients, IConfiguration configuration)
{
    private Uri? CrearUri(int clienteId, bool png)
    {
        var sas = configuration["AzureBlob:FotoPerfilSasUrl"];
        if (!Uri.TryCreate(sas, UriKind.Absolute, out var contenedor) ||
            contenedor.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(contenedor.Query)) return null;

        var builder = new UriBuilder(contenedor)
        {
            Path = $"{contenedor.AbsolutePath.TrimEnd('/')}/clientes/{clienteId}.{(png ? "png" : "jpg")}"
        };
        return builder.Uri;
    }

    public bool Configurado() => CrearUri(1, png: false) is not null;

    public static bool EsImagenPermitida(byte[] bytes) => EsJpeg(bytes) || EsPng(bytes);

    public static string TipoContenido(byte[] bytes) => EsPng(bytes) ? "image/png" : "image/jpeg";

    public async Task<bool> Guardar(int clienteId, byte[] contenido, CancellationToken cancellationToken)
    {
        var png = EsPng(contenido);
        var uri = CrearUri(clienteId, png);
        if (uri is null) return false;
        using var solicitud = new HttpRequestMessage(HttpMethod.Put, uri);
        solicitud.Headers.TryAddWithoutValidation("x-ms-blob-type", "BlockBlob");
        solicitud.Headers.TryAddWithoutValidation("x-ms-version", "2023-11-03");
        solicitud.Content = new ByteArrayContent(contenido);
        solicitud.Content.Headers.ContentType = new MediaTypeHeaderValue(png ? "image/png" : "image/jpeg");
        using var response = await clients.CreateClient().SendAsync(solicitud, cancellationToken);
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<byte[]?> Obtener(int clienteId, CancellationToken cancellationToken)
    {
        var uri = CrearUri(clienteId, png: false);
        if (uri is null) return null;
        using var response = await clients.CreateClient().GetAsync(uri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            uri = CrearUri(clienteId, png: true)!;
            using var pngResponse = await clients.CreateClient().GetAsync(uri, cancellationToken);
            if (pngResponse.StatusCode == HttpStatusCode.NotFound) return null;
            pngResponse.EnsureSuccessStatusCode();
            return await pngResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task Eliminar(int clienteId, CancellationToken cancellationToken)
    {
        foreach (var png in new[] { false, true })
        {
            var uri = CrearUri(clienteId, png);
            if (uri is null) return;
            using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
            request.Headers.TryAddWithoutValidation("x-ms-version", "2023-11-03");
            using var response = await clients.CreateClient().SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.NotFound) response.EnsureSuccessStatusCode();
        }
    }

    private static bool EsJpeg(byte[] bytes) => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;

    private static bool EsPng(byte[] bytes) => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
}
