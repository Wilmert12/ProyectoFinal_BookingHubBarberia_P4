using System.Diagnostics;
using ClosedXML.Excel;

namespace BookingHub.WinForms.Infraestructura;

/// <summary>
/// Exporta tablas a un libro de Excel (.xlsx): una hoja por sección, título arriba,
/// encabezados en negrita con filtro, montos con formato de pesos y columnas ajustadas.
/// </summary>
public static class Exportar
{
    /// <param name="Hoja">Nombre corto de la pestaña (máximo 31 caracteres).</param>
    /// <param name="Titulo">Primera línea de la hoja.</param>
    public record Seccion(string Hoja, string Titulo, string[] Encabezados, IEnumerable<object?[]> Filas);

    private const string FormatoPesos = "\"RD$\"#,##0.00";
    private const string FormatoPorcentaje = "0.##\" %\"";

    public static void Excel(IWin32Window ventana, string nombreSugerido, params Seccion[] secciones)
    {
        using var dialogo = new SaveFileDialog
        {
            Title = "Exportar a Excel",
            Filter = "Libro de Excel|*.xlsx",
            FileName = nombreSugerido,
            DefaultExt = "xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialogo.ShowDialog(ventana) != DialogResult.OK)
            return;

        try
        {
            using var libro = Crear(secciones);
            libro.SaveAs(dialogo.FileName);
        }
        catch (IOException ex)
        {
            Mensajes.Informacion(ventana, $"No se pudo guardar el archivo. Si está abierto en Excel, ciérralo e intenta de nuevo.\n\n{ex.Message}");
            return;
        }

        if (Mensajes.Confirmar(ventana, "Archivo guardado. ¿Abrirlo ahora?"))
            Process.Start(new ProcessStartInfo(dialogo.FileName) { UseShellExecute = true });
    }

    /// <summary>Arma el libro en memoria (separado del diálogo para poder revisarlo sin abrir ventanas).</summary>
    public static XLWorkbook Crear(params Seccion[] secciones)
    {
        var libro = new XLWorkbook();
        foreach (var seccion in secciones)
        {
            var hoja = libro.Worksheets.Add(NombreHoja(libro, seccion.Hoja));
            hoja.Cell(1, 1).Value = seccion.Titulo;
            hoja.Cell(1, 1).Style.Font.Bold = true;
            hoja.Cell(1, 1).Style.Font.FontSize = 13;

            for (var c = 0; c < seccion.Encabezados.Length; c++)
                hoja.Cell(3, c + 1).Value = seccion.Encabezados[c];

            var fila = 4;
            foreach (var valores in seccion.Filas)
            {
                for (var c = 0; c < valores.Length; c++)
                    Escribir(hoja.Cell(fila, c + 1), valores[c], seccion.Encabezados.ElementAtOrDefault(c) ?? "");
                fila++;
            }

            var encabezado = hoja.Range(3, 1, 3, Math.Max(1, seccion.Encabezados.Length));
            encabezado.Style.Font.Bold = true;
            encabezado.Style.Fill.BackgroundColor = XLColor.FromArgb(243, 237, 225);
            encabezado.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            if (fila > 4)
                hoja.Range(3, 1, fila - 1, seccion.Encabezados.Length).SetAutoFilter();

            hoja.SheetView.FreezeRows(3);
            hoja.Columns(1, Math.Max(1, seccion.Encabezados.Length)).AdjustToContents(3, fila);
            foreach (var columna in hoja.ColumnsUsed())
                columna.Width = Math.Min(Math.Max(columna.Width + 2, 10), 45);
        }
        return libro;
    }

    private static void Escribir(IXLCell celda, object? valor, string encabezado)
    {
        switch (valor)
        {
            case null:
                break;
            case decimal d:
                celda.Value = d;
                celda.Style.NumberFormat.Format = encabezado.Contains('%') ? FormatoPorcentaje : FormatoPesos;
                break;
            case int n:
                celda.Value = n;
                break;
            case DateTime f:
                celda.Value = f;
                celda.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                break;
            default:
                celda.Value = Convert.ToString(valor) ?? string.Empty;
                break;
        }
    }

    // Excel no admite nombres repetidos ni ciertos caracteres en las pestañas.
    private static string NombreHoja(XLWorkbook libro, string nombre)
    {
        var limpio = new string(nombre.Where(c => !"[]*?/\\:".Contains(c)).ToArray()).Trim();
        limpio = limpio.Length == 0 ? "Hoja" : limpio[..Math.Min(limpio.Length, 31)];
        var final = limpio;
        for (var i = 2; libro.Worksheets.Contains(final); i++)
            final = $"{limpio[..Math.Min(limpio.Length, 28)]} {i}";
        return final;
    }
}
