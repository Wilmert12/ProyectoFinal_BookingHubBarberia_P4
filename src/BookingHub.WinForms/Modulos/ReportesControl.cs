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
    private readonly Label _itbis = Tarjeta();
    private readonly Label _cobros = Tarjeta();
    private readonly Label _completadas = Tarjeta();
    private readonly Label _porCobrar = Tarjeta();
    private readonly DataGridView _porBarbero = Ui.Grilla();
    private readonly DataGridView _porServicio = Ui.Grilla();
    private readonly DataGridView _porMetodo = Ui.Grilla();

    private record FilaBarbero(string Barbero, int Citas, decimal Ingresos, decimal Comision, decimal Propinas, decimal APagar);

    private ReporteIngresosDto? _reporte;

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
        tarjetas.Controls.AddRange([_ingresos, _itbis, _propinas, _cobros, _completadas, _porCobrar]);

        // Arriba, a todo el ancho, lo que se le paga a cada barbero; debajo, servicios y métodos de pago.
        var tablas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4 };
        tablas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
        tablas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
        tablas.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tablas.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
        tablas.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tablas.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));

        var tituloBarbero = Subtitulo("Por barbero: comisión (sobre lo cobrado sin ITBIS) + propinas = a pagar");
        tablas.Controls.Add(tituloBarbero, 0, 0);
        tablas.SetColumnSpan(tituloBarbero, 2);
        var tarjetaBarbero = Ui.EnVidrio(_porBarbero);
        tarjetaBarbero.Margin = new Padding(0, 0, 0, 10);
        tablas.Controls.Add(tarjetaBarbero, 0, 1);
        tablas.SetColumnSpan(tarjetaBarbero, 2);

        tablas.Controls.Add(Subtitulo("Servicios y productos más vendidos"), 0, 2);
        tablas.Controls.Add(Subtitulo("Por método de pago"), 1, 2);
        var tarjetaServicio = Ui.EnVidrio(_porServicio);
        tarjetaServicio.Margin = new Padding(0, 0, 12, 0);
        tablas.Controls.Add(tarjetaServicio, 0, 3);
        var tarjetaMetodo = Ui.EnVidrio(_porMetodo);
        tarjetaMetodo.Margin = Padding.Empty;
        tablas.Controls.Add(tarjetaMetodo, 1, 3);

        Controls.Add(tablas);
        Controls.Add(tarjetas);
        var exportar = Ui.Boton("Exportar a Excel");
        exportar.Click += (_, _) => ExportarExcel();
        Controls.Add(Ui.Barra(Ui.Etiqueta("Desde"), _desde, Ui.Etiqueta("Hasta"), _hasta, generar, hoyBoton, mes, exportar));
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
        Size = new Size(176, 72),
        BackColor = Ui.Vidrio,
        ForeColor = Ui.Tinta,
        Padding = new Padding(14, 12, 14, 12),
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
        _reporte = reporte;
        _ingresos.Text = $"Ingresos\n{Mensajes.Precio(reporte.TotalIngresos)}";
        _itbis.Text = $"ITBIS (18 %)\n{Mensajes.Precio(reporte.TotalItbis)}";
        _propinas.Text = $"Propinas\n{Mensajes.Precio(reporte.TotalPropinas)}";
        _cobros.Text = $"Cobros\n{reporte.CantidadPagos}";
        _completadas.Text = $"Completadas\n{reporte.CitasCompletadas}";
        // Completadas que recepción aún no cobró: todavía no son ingresos.
        _porCobrar.Text = $"Por cobrar ({reporte.CitasPorCobrar})\n{Mensajes.Precio(reporte.PorCobrar)}";
        _porCobrar.ForeColor = reporte.CitasPorCobrar > 0 ? Color.FromArgb(176, 122, 20) : Ui.Tinta;

        Ui.Mostrar(_porBarbero, reporte.PorBarbero.Select(b => new FilaBarbero(
                $"{b.Nombre} ({b.ComisionPorcentaje:0.##} %)", b.Citas, b.Ingresos, b.Comision, b.Propinas, b.APagar)),
            nameof(FilaBarbero.Ingresos), nameof(FilaBarbero.Comision), nameof(FilaBarbero.Propinas), nameof(FilaBarbero.APagar));
        _porBarbero.Columns[nameof(FilaBarbero.Comision)]!.HeaderText = "Comisión";
        _porBarbero.Columns[nameof(FilaBarbero.APagar)]!.HeaderText = "A pagar";
        _porBarbero.Columns[nameof(FilaBarbero.Citas)]!.FillWeight = 50;
        _porBarbero.Columns[nameof(FilaBarbero.Barbero)]!.FillWeight = 160;
        Ui.Mostrar(_porServicio, reporte.PorServicio.Select(s => new FilaServicio(s.Nombre, s.EsServicio ? "Servicio" : "Producto", s.Cantidad, s.Ingresos)), nameof(FilaServicio.Ingresos));
        Ui.Mostrar(_porMetodo, reporte.PorMetodo.Select(m => new FilaMetodo(m.MetodoPago, m.Cantidad, m.Total)), nameof(FilaMetodo.Total));
    }

    private void ExportarExcel()
    {
        if (_reporte is not { } r)
        {
            Mensajes.Informacion(this, "Genera el reporte primero.");
            return;
        }

        Exportar.Excel(this, $"Reporte de ingresos {r.Desde:yyyy-MM-dd} a {r.Hasta:yyyy-MM-dd}.xlsx",
            new Exportar.Seccion("Resumen", $"Reporte de ingresos del {r.Desde:dd/MM/yyyy} al {r.Hasta:dd/MM/yyyy}", ["Concepto", "Valor"],
            [
                ["Ingresos (ITBIS incluido)", r.TotalIngresos],
                ["ITBIS (18 %)", r.TotalItbis],
                ["Ingresos sin ITBIS", r.TotalIngresos - r.TotalItbis],
                ["Propinas", r.TotalPropinas],
                ["Cobros", r.CantidadPagos],
                ["Citas completadas", r.CitasCompletadas],
                ["Por cobrar", r.PorCobrar],
                ["Citas por cobrar", r.CitasPorCobrar]
            ]),
            new Exportar.Seccion("Por barbero", "Por barbero: comisión sobre lo cobrado sin ITBIS + propinas = a pagar", ["Barbero", "Citas", "Ingresos", "Comisión %", "Comisión", "Propinas", "A pagar"],
                r.PorBarbero.Select(b => new object?[] { b.Nombre, b.Citas, b.Ingresos, b.ComisionPorcentaje, b.Comision, b.Propinas, b.APagar })),
            new Exportar.Seccion("Servicios", "Servicios y productos más vendidos", ["Ítem", "Tipo", "Cantidad", "Ingresos"],
                r.PorServicio.Select(s => new object?[] { s.Nombre, s.EsServicio ? "Servicio" : "Producto", s.Cantidad, s.Ingresos })),
            new Exportar.Seccion("Métodos de pago", "Por método de pago", ["Método", "Cobros", "Total"],
                r.PorMetodo.Select(m => new object?[] { m.MetodoPago, m.Cantidad, m.Total })));
    }
}
