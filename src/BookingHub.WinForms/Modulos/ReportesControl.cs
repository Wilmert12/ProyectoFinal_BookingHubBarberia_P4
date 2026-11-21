using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;

namespace BookingHub.WinForms.Modulos;

public class ReportesControl : UserControl
{
    private readonly Apis _apis;
    private readonly DateTimePicker _desde = new() { Format = DateTimePickerFormat.Short, Width = 130 };
    private readonly DateTimePicker _hasta = new() { Format = DateTimePickerFormat.Short, Width = 130 };
    private readonly Label _ingresos = Tarjeta();
    private readonly Label _propinas = Tarjeta();
    private readonly Label _cobros = Tarjeta();
    private readonly Label _completadas = Tarjeta();
    private readonly Label _porCobrar = Tarjeta();
    private readonly DataGridView _porBarbero = Ui.Grilla();
    private readonly DataGridView _porServicio = Ui.Grilla();
    private readonly DataGridView _porMetodo = Ui.Grilla();

    private record FilaBarbero(string Barbero, int Citas, decimal Ingresos);

    private record FilaServicio(string Item, string Tipo, int Cantidad, decimal Ingresos);

    private record FilaMetodo(string Metodo, int Cobros, decimal Total);

    public ReportesControl(Apis apis)
    {
        _apis = apis;
        var hoy = DateTime.Today;
        _desde.Value = new DateTime(hoy.Year, hoy.Month, 1);
        _hasta.Value = hoy;

        var generar = Ui.Boton("Generar reporte", primario: true);
        var mes = Ui.Boton("Este mes");
        var hoyBoton = Ui.Boton("Hoy");

        var tarjetas = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 4, 0, 10) };
        tarjetas.Controls.AddRange([_ingresos, _propinas, _cobros, _completadas, _porCobrar]);

        var tablas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
        tablas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
        tablas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
        tablas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
        tablas.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tablas.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        tablas.Controls.Add(Subtitulo("Ingresos por barbero"), 0, 0);
        tablas.Controls.Add(Subtitulo("Servicios y productos más vendidos"), 1, 0);
        tablas.Controls.Add(Subtitulo("Por método de pago"), 2, 0);
        var columna = 0;
        foreach (var grilla in new[] { _porBarbero, _porServicio, _porMetodo })
        {
            var tarjeta = Ui.EnVidrio(grilla);
            tarjeta.Margin = new Padding(0, 0, 12, 0);
            tablas.Controls.Add(tarjeta, columna++, 1);
        }

        Controls.Add(tablas);
        Controls.Add(tarjetas);
        Controls.Add(Ui.Barra(Ui.Etiqueta("Desde"), _desde, Ui.Etiqueta("Hasta"), _hasta, generar, hoyBoton, mes));
        Controls.Add(Ui.TituloModulo("Reporte de ingresos"));

        Load += async (_, _) => await Generar();
        VisibleChanged += async (_, _) =>
        {
            if (Visible && IsHandleCreated)
                await Generar();
        };
        generar.Click += async (_, _) => await Generar();
        hoyBoton.Click += async (_, _) =>
        {
            _desde.Value = _hasta.Value = DateTime.Today;
            await Generar();
        };
        mes.Click += async (_, _) =>
        {
            _desde.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _hasta.Value = DateTime.Today;
            await Generar();
        };
    }

    private static Label Tarjeta() => new()
    {
        AutoSize = false,
        Size = new Size(220, 72),
        BackColor = Ui.Vidrio,
        ForeColor = Ui.Tinta,
        Padding = new Padding(16, 12, 16, 12),
        Margin = new Padding(0, 0, 12, 0),
        Font = Ui.Destacado
    };

    private static Label Subtitulo(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        Font = Ui.Destacado,
        Padding = new Padding(0, 0, 0, 6)
    };

    private async Task Generar()
    {
        if (_hasta.Value.Date < _desde.Value.Date)
        {
            Mensajes.Informacion(this, "La fecha final no puede ser anterior a la inicial.");
            return;
        }

        try
        {
            UseWaitCursor = true;
            var reporte = await _apis.Reportes.GetIngresos(Mensajes.Fecha(_desde.Value), Mensajes.Fecha(_hasta.Value));
            Mostrar(reporte);
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

    private void Mostrar(ReporteIngresosDto reporte)
    {
        _ingresos.Text = $"Ingresos\n{Mensajes.Precio(reporte.TotalIngresos)}";
        _propinas.Text = $"Propinas\n{Mensajes.Precio(reporte.TotalPropinas)}";
        _cobros.Text = $"Cobros\n{reporte.CantidadPagos}";
        _completadas.Text = $"Citas completadas\n{reporte.CitasCompletadas}";
        // Completadas que recepción aún no cobró: todavía no son ingresos.
        _porCobrar.Text = $"Por cobrar ({reporte.CitasPorCobrar})\n{Mensajes.Precio(reporte.PorCobrar)}";
        _porCobrar.ForeColor = reporte.CitasPorCobrar > 0 ? Color.FromArgb(176, 122, 20) : Ui.Tinta;

        Ui.Mostrar(_porBarbero, reporte.PorBarbero.Select(b => new FilaBarbero(b.Nombre, b.Citas, b.Ingresos)), nameof(FilaBarbero.Ingresos));
        Ui.Mostrar(_porServicio, reporte.PorServicio.Select(s => new FilaServicio(s.Nombre, s.EsServicio ? "Servicio" : "Producto", s.Cantidad, s.Ingresos)), nameof(FilaServicio.Ingresos));
        Ui.Mostrar(_porMetodo, reporte.PorMetodo.Select(m => new FilaMetodo(m.MetodoPago, m.Cantidad, m.Total)), nameof(FilaMetodo.Total));
    }
}
