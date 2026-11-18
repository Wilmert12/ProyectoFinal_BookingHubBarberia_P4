using BookingHub.WinForms.Formularios;
using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;

namespace BookingHub.WinForms.Modulos;

public class AgendaControl : UserControl
{
    private readonly Apis _apis;
    private readonly DateTimePicker _fecha = new() { Format = DateTimePickerFormat.Short, Width = 130 };
    private readonly ComboBox _barbero = Ui.Lista();
    private readonly ComboBox _estado = Ui.Lista();
    private readonly DataGridView _grilla = Ui.Grilla();
    private readonly Label _resumen = new() { AutoSize = true, ForeColor = Ui.TextoSuave, Dock = DockStyle.Bottom, Padding = new Padding(0, 8, 0, 0) };
    private List<CitaDto> _citas = [];
    private bool _cargado;

    private record FilaCita(int Id, string Hora, string Cliente, string Barbero, string Servicios, string Estado, decimal Total);

    public AgendaControl(Apis apis)
    {
        _apis = apis;
        _barbero.Width = 200;
        _estado.Width = 160;

        var actualizar = Ui.Boton("Actualizar");
        var cambiarEstado = Ui.Boton("Cambiar estado");
        var nueva = Ui.Boton("Nueva cita / walk-in");
        var cobrar = Ui.Boton("Cobrar", primario: true);
        var eliminar = Ui.Boton("Eliminar");

        var filtros = Ui.Barra(Ui.Etiqueta("Fecha"), _fecha, Ui.Etiqueta("Barbero"), _barbero, actualizar);
        var acciones = Ui.Barra(nueva, cobrar, Ui.Etiqueta("Estado"), _estado, cambiarEstado, eliminar);
        acciones.Dock = DockStyle.Bottom;

        Controls.Add(_grilla);
        Controls.Add(acciones);
        Controls.Add(_resumen);
        Controls.Add(filtros);
        Controls.Add(Ui.TituloModulo("Agenda del día"));

        Load += async (_, _) => await Inicializar();
        _fecha.ValueChanged += async (_, _) => await Cargar();
        _barbero.SelectionChangeCommitted += async (_, _) => await Cargar();
        actualizar.Click += async (_, _) => await Cargar();
        cambiarEstado.Click += async (_, _) => await CambiarEstado();
        nueva.Click += async (_, _) => await NuevaCita();
        cobrar.Click += async (_, _) => await Cobrar();
        eliminar.Click += async (_, _) => await Eliminar();
        _grilla.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await Cobrar(); };
        _grilla.CellFormatting += ColorearEstado;
        _grilla.SelectionChanged += (_, _) => SincronizarEstado();
    }

    private async Task Inicializar()
    {
        try
        {
            var barberos = await _apis.Barberos.GetBarberos();
            var estados = await _apis.Catalogos.GetEstadosCita();

            _barbero.DisplayMember = nameof(BarberoDto.Nombre);
            _barbero.ValueMember = nameof(BarberoDto.BarberoId);
            _barbero.DataSource = new List<BarberoDto> { new() { BarberoId = 0, Nombre = "Todos" } }.Concat(barberos).ToList();

            _estado.DisplayMember = nameof(EstadoCitaDto.Descripcion);
            _estado.ValueMember = nameof(EstadoCitaDto.EstadoCitaId);
            _estado.DataSource = estados;

            _cargado = true;
            await Cargar();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private async Task Cargar()
    {
        if (!_cargado)
            return;

        try
        {
            UseWaitCursor = true;
            int? barberoId = _barbero.SelectedValue is int id && id > 0 ? id : null;
            _citas = await _apis.Citas.GetCitas(barberoId, Mensajes.Fecha(_fecha.Value));

            Ui.Mostrar(_grilla, _citas.Select(c => new FilaCita(
                c.CitaId,
                $"{c.FechaInicio:HH:mm} - {c.FechaFin:HH:mm}",
                c.ClienteNombre ?? "",
                c.BarberoNombre ?? "",
                Servicios(c),
                c.Estado ?? "",
                c.Total)), nameof(FilaCita.Total));

            var activas = _citas.Count(c => c.Estado is not ("Cancelada" or "NoShow"));
            _resumen.Text = $"{_citas.Count} citas · {activas} activas · {_citas.Count(c => c.Estado == "Completada")} completadas";
            SincronizarEstado();
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

    private static string Servicios(CitaDto cita) =>
        cita.Detalles is { Count: > 0 }
            ? string.Join(", ", cita.Detalles.Select(d => d.Cantidad > 1 ? $"{d.Nombre} x{d.Cantidad}" : d.Nombre))
            : cita.Notas ?? "";

    private CitaDto? CitaSeleccionada()
    {
        var fila = Ui.Seleccionado<FilaCita>(_grilla);
        return fila is null ? null : _citas.FirstOrDefault(c => c.CitaId == fila.Id);
    }

    private void SincronizarEstado()
    {
        if (CitaSeleccionada() is { } cita && _estado.DataSource is not null)
            _estado.SelectedValue = cita.EstadoCitaId;
    }

    private async Task CambiarEstado()
    {
        if (CitaSeleccionada() is not { } cita || _estado.SelectedValue is not int estadoId)
        {
            Mensajes.Informacion(this, "Selecciona una cita y el nuevo estado.");
            return;
        }

        try
        {
            await _apis.Citas.ModificarCita(cita.CitaId, cita with { EstadoCitaId = estadoId, Detalles = null });
            await Cargar();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private async Task NuevaCita()
    {
        using var dialogo = new FrmNuevaCita(_apis, _fecha.Value.Date);
        if (dialogo.ShowDialog(this) == DialogResult.OK)
            await Cargar();
    }

    private async Task Cobrar()
    {
        if (CitaSeleccionada() is not { } cita)
        {
            Mensajes.Informacion(this, "Selecciona la cita que vas a cobrar.");
            return;
        }
        if (cita.Estado is "Cancelada" or "NoShow")
        {
            Mensajes.Informacion(this, "No se puede cobrar una cita cancelada o en la que el cliente no se presentó.");
            return;
        }

        try
        {
            var pagos = await _apis.Pagos.GetPagos(citaId: cita.CitaId);
            if (pagos.Count > 0 && !Mensajes.Confirmar(this, "Esta cita ya tiene un pago registrado. ¿Quieres registrar otro cobro?"))
                return;

            using var dialogo = new FrmCobro(_apis, cita);
            if (dialogo.ShowDialog(this) == DialogResult.OK)
                await Cargar();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private async Task Eliminar()
    {
        if (CitaSeleccionada() is not { } cita)
            return;
        if (!Mensajes.Confirmar(this, $"¿Eliminar la cita de {cita.ClienteNombre} a las {cita.FechaInicio:HH:mm}? Si el cliente no vendrá, es mejor cambiarla a Cancelada."))
            return;

        try
        {
            await _apis.Citas.EliminarCita(cita.CitaId);
            await Cargar();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private void ColorearEstado(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || _grilla.Columns[e.ColumnIndex].Name != nameof(FilaCita.Estado) || e.CellStyle is null)
            return;

        var color = (e.Value as string) switch
        {
            "Pendiente" => Color.FromArgb(176, 122, 20),
            "Confirmada" => Color.FromArgb(37, 99, 160),
            "EnProceso" => Color.FromArgb(109, 76, 160),
            "Completada" => Color.FromArgb(46, 125, 80),
            "Cancelada" or "NoShow" => Color.FromArgb(180, 60, 60),
            _ => e.CellStyle.ForeColor
        };
        e.CellStyle.ForeColor = color;
        e.CellStyle.SelectionForeColor = color;
        e.CellStyle.Font = Ui.Destacado;
    }
}
