using BookingHub.WinForms.Formularios;
using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;

namespace BookingHub.WinForms.Modulos;

public class CajaControl : UserControl
{
    private readonly Apis _apis;
    private readonly DateTimePicker _fecha = new() { Format = DateTimePickerFormat.Short, Width = 130 };
    private readonly DataGridView _pagos = Ui.Grilla();
    private readonly DataGridView _porMetodo = Ui.Grilla();
    private readonly Label _totales = new() { AutoSize = true, Font = Ui.Destacado, Dock = DockStyle.Bottom, Padding = new Padding(0, 10, 0, 0) };
    private List<PagoDto> _lista = [];

    private record FilaPago(int Id, string Hora, string Cliente, string Barbero, string Metodo, decimal Monto, decimal Propina, string Referencia);

    private record FilaMetodo(string Metodo, int Cobros, decimal Total);

    public CajaControl(Apis apis)
    {
        _apis = apis;
        var cargar = Ui.Boton("Cargar");
        var imprimir = Ui.Boton("Imprimir cierre de caja", primario: true);

        var division = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 640 };
        division.Panel1.Controls.Add(_pagos);
        division.Panel2.Controls.Add(_porMetodo);
        division.Panel2.Padding = new Padding(10, 0, 0, 0);

        Controls.Add(division);
        Controls.Add(_totales);
        Controls.Add(Ui.Barra(Ui.Etiqueta("Fecha"), _fecha, cargar, imprimir));
        Controls.Add(Ui.TituloModulo("Caja y cierre del día"));

        Load += async (_, _) => await Cargar();
        _fecha.ValueChanged += async (_, _) => await Cargar();
        cargar.Click += async (_, _) => await Cargar();
        imprimir.Click += (_, _) => ImprimirCierre();
    }

    private async Task Cargar()
    {
        try
        {
            UseWaitCursor = true;
            _lista = await _apis.Pagos.GetPagos(fecha: Mensajes.Fecha(_fecha.Value));

            Ui.Mostrar(_pagos, _lista.Select(p => new FilaPago(
                p.PagoId,
                p.FechaPago.ToString("HH:mm"),
                p.ClienteNombre ?? "",
                p.BarberoNombre ?? "",
                p.MetodoPago ?? "",
                p.Monto,
                p.Propina,
                p.Referencia ?? "")),
                nameof(FilaPago.Monto), nameof(FilaPago.Propina));

            Ui.Mostrar(_porMetodo, _lista
                .GroupBy(p => p.MetodoPago ?? "Sin método")
                .Select(g => new FilaMetodo(g.Key, g.Count(), g.Sum(p => p.Monto)))
                .OrderByDescending(m => m.Total),
                nameof(FilaMetodo.Total));

            var ventas = _lista.Sum(p => p.Monto);
            var propinas = _lista.Sum(p => p.Propina);
            _totales.Text = $"{_lista.Count} cobros   ·   Ventas {Mensajes.Precio(ventas)}   ·   Propinas {Mensajes.Precio(propinas)}   ·   Total en caja {Mensajes.Precio(ventas + propinas)}";
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void ImprimirCierre()
    {
        if (_lista.Count == 0)
        {
            Mensajes.Informacion(this, "No hay cobros registrados en esta fecha.");
            return;
        }

        using var recibo = new FrmRecibo("Cierre de caja", Recibos.DeCierre(_fecha.Value.Date, _lista));
        recibo.ShowDialog(this);
    }
}
