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

        // Dos tarjetas de vidrio: cobros del día (65 %) y resumen por método de pago (35 %).
        var division = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        division.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
        division.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
        division.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        var tarjetaPagos = Ui.EnVidrio(_pagos);
        tarjetaPagos.Margin = new Padding(0, 0, 8, 0);
        var tarjetaMetodos = Ui.EnVidrio(_porMetodo);
        tarjetaMetodos.Margin = new Padding(8, 0, 0, 0);
        division.Controls.Add(tarjetaPagos, 0, 0);
        division.Controls.Add(tarjetaMetodos, 1, 0);

        Controls.Add(division);
        Controls.Add(_totales);
        var anular = Ui.Boton("Anular cobro");
        anular.Click += async (_, _) => await Anular();
        var exportar = Ui.Boton("Exportar a Excel");
        exportar.Click += (_, _) => ExportarExcel();
        Controls.Add(Ui.Barra(Ui.Etiqueta("Fecha"), _fecha, cargar, imprimir, anular, exportar));
        Controls.Add(Ui.TituloModulo("Caja y cierre del día"));

        Load += async (_, _) => await Cargar();
        var reloj = new System.Windows.Forms.Timer { Interval = 30_000 };
        reloj.Tick += async (_, _) =>
        {
            if (Visible && !Ui.HayDialogoAbierto)
                await Cargar(silencioso: true);
        };
        VisibleChanged += async (_, _) =>
        {
            if (Visible && IsHandleCreated)
                await Cargar(silencioso: true);
        };
        reloj.Start();
        Disposed += (_, _) => reloj.Dispose();
        _fecha.ValueChanged += async (_, _) => await Cargar();
        cargar.Click += async (_, _) => await Cargar();
        imprimir.Click += (_, _) => ImprimirCierre();
    }

    private async Task Cargar(bool silencioso = false)
    {
        try
        {
            UseWaitCursor = !silencioso;
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
            _totales.Text = $"{_lista.Count} cobros   ·   Ventas {Mensajes.Precio(ventas)} (ITBIS {Mensajes.Precio(Impuestos.ItbisIncluido(ventas))})   ·   Propinas {Mensajes.Precio(propinas)}   ·   Total en caja {Mensajes.Precio(ventas + propinas)}";
        }
        catch (Exception ex)
        {
            if (silencioso)
                _totales.Text = $"No se pudo actualizar la caja ({DateTime.Now:HH:mm}). Se reintentará sola.";
            else
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

    /// <summary>
    /// Borra un cobro hecho por error (monto o método equivocado). La cita vuelve a quedar
    /// "por cobrar" y se puede cobrar de nuevo desde la Agenda.
    /// </summary>
    private async Task Anular()
    {
        if (Ui.Seleccionado<FilaPago>(_pagos) is not { } fila || _lista.FirstOrDefault(p => p.PagoId == fila.Id) is not { } pago)
        {
            Mensajes.Informacion(this, "Selecciona en la lista el cobro que quieres anular.");
            return;
        }
        if (!Mensajes.Confirmar(this,
                $"¿Anular el cobro de {pago.ClienteNombre} por {Mensajes.Precio(pago.Monto)} ({pago.MetodoPago}, {pago.FechaPago:HH:mm})?\n\n" +
                "Se borra de la caja y de los reportes, y la cita queda por cobrar. Esto no se puede deshacer."))
            return;

        try
        {
            await _apis.Pagos.EliminarPago(pago.PagoId);
            await Cargar();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private void ExportarExcel()
    {
        if (_lista.Count == 0)
        {
            Mensajes.Informacion(this, "No hay cobros en este día para exportar.");
            return;
        }

        var ventas = _lista.Sum(p => p.Monto);
        var propinas = _lista.Sum(p => p.Propina);
        Exportar.Csv(this, $"Caja {_fecha.Value:yyyy-MM-dd}.csv",
            new Exportar.Seccion($"Cobros del {_fecha.Value:dd/MM/yyyy}",
                ["Hora", "Recibo", "Cliente", "Barbero", "Método", "Monto", "ITBIS incluido", "Propina", "Referencia"],
                _lista.OrderBy(p => p.FechaPago).Select(p => new object?[]
                {
                    p.FechaPago.ToString("HH:mm"), p.PagoId, p.ClienteNombre, p.BarberoNombre, p.MetodoPago,
                    p.Monto, Impuestos.ItbisIncluido(p.Monto), p.Propina, p.Referencia
                })),
            new Exportar.Seccion("Por método de pago", ["Método", "Cobros", "Total"],
                _lista.GroupBy(p => p.MetodoPago ?? "Sin método").Select(g => new object?[] { g.Key, g.Count(), g.Sum(p => p.Monto) })),
            new Exportar.Seccion("Totales", ["Concepto", "Valor"],
            [
                ["Ventas (ITBIS incluido)", ventas],
                ["ITBIS (18 %)", Impuestos.ItbisIncluido(ventas)],
                ["Propinas", propinas],
                ["Total en caja", ventas + propinas]
            ]));
    }
}
