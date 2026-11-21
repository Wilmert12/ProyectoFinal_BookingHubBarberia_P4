using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace BookingHub.WinForms.Infraestructura;

/// <summary>
/// Exporta tablas a un archivo CSV que Excel abre directamente.
/// Usa el separador de listas y el formato de números de Windows, así Excel pone
/// cada dato en su columna, y lo guarda en UTF-8 con BOM para que se vean las tildes.
/// </summary>
public static class Exportar
{
    public record Seccion(string Titulo, string[] Encabezados, IEnumerable<object?[]> Filas);

    public static void Csv(IWin32Window ventana, string nombreSugerido, params Seccion[] secciones)
    {
        using var dialogo = new SaveFileDialog
        {
            Title = "Exportar a Excel",
            Filter = "Excel (CSV)|*.csv",
            FileName = nombreSugerido,
            DefaultExt = "csv",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialogo.ShowDialog(ventana) != DialogResult.OK)
            return;

        try
        {
            File.WriteAllText(dialogo.FileName, Generar(secciones), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
        catch (IOException ex)
        {
            Mensajes.Informacion(ventana, $"No se pudo guardar el archivo. Si está abierto en Excel, ciérralo e intenta de nuevo.\n\n{ex.Message}");
            return;
        }

        if (Mensajes.Confirmar(ventana, "Archivo guardado. ¿Abrirlo ahora?"))
            Process.Start(new ProcessStartInfo(dialogo.FileName) { UseShellExecute = true });
    }

    /// <summary>Texto CSV de las secciones (una tabla debajo de otra, separadas por una línea en blanco).</summary>
    public static string Generar(params Seccion[] secciones)
    {
        var separador = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        var texto = new StringBuilder();
        foreach (var seccion in secciones)
        {
            if (texto.Length > 0)
                texto.AppendLine();
            texto.AppendLine(Celda(seccion.Titulo, separador));
            texto.AppendLine(string.Join(separador, seccion.Encabezados.Select(e => Celda(e, separador))));
            foreach (var fila in seccion.Filas)
                texto.AppendLine(string.Join(separador, fila.Select(v => Celda(v, separador))));
        }
        return texto.ToString();
    }

    private static string Celda(object? valor, string separador)
    {
        var texto = valor switch
        {
            null => string.Empty,
            decimal d => d.ToString("0.00", CultureInfo.CurrentCulture),
            DateTime f => f.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture),
            DateOnly f => f.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture),
            _ => Convert.ToString(valor, CultureInfo.CurrentCulture) ?? string.Empty
        };

        return texto.Contains(separador) || texto.Contains('"') || texto.Contains('\n')
            ? $"\"{texto.Replace("\"", "\"\"")}\""
            : texto;
    }
}
