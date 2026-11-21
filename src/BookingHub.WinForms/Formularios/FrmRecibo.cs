using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;
using System.Drawing.Printing;
using System.Text;

namespace BookingHub.WinForms.Formularios;

public class FrmRecibo : Form
{
    private readonly string _contenido;
    private readonly Font _fuenteImpresion = new("Consolas", 10f);
    private int _lineaActual;

    public FrmRecibo(string titulo, string contenido)
    {
        _contenido = contenido;
        Text = titulo;
        Font = Ui.Fuente;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(480, 620);
        BackColor = Ui.Superficie;
        Icon = Ui.Icono();
        Ui.EfectoVentana(this);

        var texto = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = _fuenteImpresion,
            Text = contenido.Replace("\n", Environment.NewLine),
            BackColor = Ui.Superficie,
            BorderStyle = BorderStyle.None
        };

        var imprimir = Ui.Boton("Imprimir", primario: true);
        var cerrar = Ui.Boton("Cerrar");
        cerrar.DialogResult = DialogResult.OK;
        CancelButton = cerrar;
        var barra = Ui.Barra(imprimir, cerrar);
        barra.Dock = DockStyle.Bottom;

        var contenedor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        contenedor.Controls.Add(texto);
        contenedor.Controls.Add(barra);
        Controls.Add(contenedor);

        imprimir.Click += (_, _) => Imprimir();
        Shown += (_, _) =>
        {
            texto.SelectionStart = 0;
            texto.SelectionLength = 0;
            imprimir.Focus();
        };
    }

    private void Imprimir()
    {
        using var documento = new PrintDocument { DocumentName = Text };
        using var dialogo = new PrintDialog { Document = documento, UseEXDialog = true };
        if (dialogo.ShowDialog(this) != DialogResult.OK)
            return;

        var lineas = _contenido.Split('\n');
        _lineaActual = 0;
        documento.PrintPage += (_, e) =>
        {
            var y = (float)e.MarginBounds.Top;
            var alto = _fuenteImpresion.GetHeight(e.Graphics!);
            while (_lineaActual < lineas.Length && y + alto <= e.MarginBounds.Bottom)
            {
                e.Graphics!.DrawString(lineas[_lineaActual], _fuenteImpresion, Brushes.Black, e.MarginBounds.Left, y);
                y += alto;
                _lineaActual++;
            }
            e.HasMorePages = _lineaActual < lineas.Length;
        };

        try
        {
            documento.Print();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }
}

public static class Recibos
{
    private const int Ancho = 42;

    public static string DeCobro(CitaDto cita, List<CitaDetalleDto> detalles, PagoDto pago)
    {
        var texto = new StringBuilder();
        Centrado(texto, "CERQUILLOAPP · BARBERÍA");
        Centrado(texto, "Recibo de pago");
        texto.AppendLine(new string('=', Ancho));
        texto.AppendLine($"Recibo No.: {pago.PagoId}");
        texto.AppendLine($"Fecha:      {pago.FechaPago:dd/MM/yyyy HH:mm}");
        texto.AppendLine($"Cliente:    {cita.ClienteNombre}");
        texto.AppendLine($"Barbero:    {cita.BarberoNombre}");
        texto.AppendLine(new string('-', Ancho));
        foreach (var detalle in detalles)
        {
            var subtotal = detalle.Cantidad * detalle.PrecioUnitario - detalle.Descuento;
            Columnas(texto, $"{detalle.Cantidad} x {detalle.Nombre}", Mensajes.Precio(subtotal));
            if (detalle.Descuento > 0)
                Columnas(texto, "   descuento", "-" + Mensajes.Precio(detalle.Descuento));
        }
        texto.AppendLine(new string('-', Ancho));
        Columnas(texto, "TOTAL", Mensajes.Precio(pago.Monto));
        if (pago.Propina > 0)
            Columnas(texto, "Propina", Mensajes.Precio(pago.Propina));
        Columnas(texto, "Método de pago", pago.MetodoPago ?? "");
        if (!string.IsNullOrWhiteSpace(pago.Referencia))
            Columnas(texto, "Referencia", pago.Referencia);
        texto.AppendLine(new string('=', Ancho));
        Centrado(texto, "¡Gracias por su visita!");
        return texto.ToString();
    }

    public static string DeCierre(DateTime fecha, List<PagoDto> pagos)
    {
        var texto = new StringBuilder();
        Centrado(texto, "CERQUILLOAPP · BARBERÍA");
        Centrado(texto, "Cierre de caja");
        Centrado(texto, fecha.ToString("dddd dd/MM/yyyy"));
        texto.AppendLine(new string('=', Ancho));
        foreach (var grupo in pagos.GroupBy(p => p.MetodoPago ?? "Sin método").OrderBy(g => g.Key))
            Columnas(texto, $"{grupo.Key} ({grupo.Count()})", Mensajes.Precio(grupo.Sum(p => p.Monto)));
        texto.AppendLine(new string('-', Ancho));
        Columnas(texto, "Cobros", pagos.Count.ToString());
        Columnas(texto, "Total ventas", Mensajes.Precio(pagos.Sum(p => p.Monto)));
        Columnas(texto, "Total propinas", Mensajes.Precio(pagos.Sum(p => p.Propina)));
        Columnas(texto, "TOTAL EN CAJA", Mensajes.Precio(pagos.Sum(p => p.Monto + p.Propina)));
        texto.AppendLine(new string('=', Ancho));
        texto.AppendLine($"Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}");
        return texto.ToString();
    }

    private static void Centrado(StringBuilder texto, string linea) =>
        texto.AppendLine(linea.PadLeft((Ancho + linea.Length) / 2));

    private static void Columnas(StringBuilder texto, string izquierda, string derecha)
    {
        if (izquierda.Length + derecha.Length >= Ancho)
            izquierda = izquierda[..Math.Max(0, Ancho - derecha.Length - 2)] + "…";
        var espacios = Math.Max(1, Ancho - izquierda.Length - derecha.Length);
        texto.AppendLine(izquierda + new string(' ', espacios) + derecha);
    }
}
