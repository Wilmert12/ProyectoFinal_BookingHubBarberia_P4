using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;

namespace BookingHub.WinForms.Formularios;

/// <summary>Mueve una cita a otro día, hora o barbero. Mantiene los servicios, el total y el estado.</summary>
public class FrmReprogramar : Form
{
    private readonly Apis _apis;
    private readonly CitaDto _cita;
    private readonly int _duracionMin;
    private readonly ComboBox _barbero = Ui.Lista();
    private readonly DateTimePicker _fecha = new() { Format = DateTimePickerFormat.Short };
    private readonly ComboBox _horaLibre = Ui.Lista();
    private readonly Label _infoHoras = new() { AutoSize = true, ForeColor = Ui.TextoSuave };
    private readonly CheckBox _otraHora = new() { Text = "Usar otra hora", AutoSize = true };
    private readonly DateTimePicker _horaManual = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Enabled = false };
    private readonly Label _resumen = new() { AutoSize = true, ForeColor = Ui.TextoSuave };
    private readonly Button _guardar = Ui.Boton("Reprogramar", primario: true);
    private bool _cargado;
    private int _consulta;

    private record OpcionHora(DateTime Inicio, string Texto);

    public FrmReprogramar(Apis apis, CitaDto cita)
    {
        _apis = apis;
        _cita = cita;
        _duracionMin = Math.Max(5, (int)(cita.FechaFin - cita.FechaInicio).TotalMinutes);

        Text = "Reprogramar cita";
        Font = Ui.Fuente;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(500, 420);
        BackColor = Ui.Superficie;
        Icon = Ui.Icono();
        Ui.EfectoVentana(this);
        AcceptButton = _guardar;

        _fecha.Value = cita.FechaInicio.Date < DateTime.Today ? DateTime.Today : cita.FechaInicio.Date;
        _horaManual.Value = DateTime.Today.Add(cita.FechaInicio.TimeOfDay);

        var actual = new Label
        {
            Text = $"{cita.ClienteNombre}\nAhora: {cita.FechaInicio:dd/MM/yyyy HH:mm} - {cita.FechaFin:HH:mm} con {cita.BarberoNombre} ({_duracionMin} min)",
            AutoSize = true,
            Font = Ui.Destacado,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 12)
        };

        var formulario = Ui.Formulario(
            ("Barbero", _barbero),
            ("Fecha", _fecha),
            ("Hora libre", _horaLibre),
            ("", _infoHoras),
            ("", _otraHora),
            ("Otra hora", _horaManual),
            ("", _resumen));

        var cancelar = Ui.Boton("Cancelar");
        cancelar.DialogResult = DialogResult.Cancel;
        CancelButton = cancelar;
        var botones = Ui.Barra(_guardar, cancelar);
        botones.Dock = DockStyle.Bottom;

        var contenedor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        contenedor.Controls.Add(formulario);
        contenedor.Controls.Add(actual);
        contenedor.Controls.Add(botones);
        Controls.Add(contenedor);

        Load += async (_, _) => await Cargar();
        _barbero.SelectedIndexChanged += async (_, _) => await CargarHoras();
        _fecha.ValueChanged += async (_, _) => await CargarHoras();
        _horaLibre.SelectedIndexChanged += (_, _) => ActualizarResumen();
        _horaManual.ValueChanged += (_, _) => ActualizarResumen();
        _otraHora.CheckedChanged += (_, _) =>
        {
            _horaManual.Enabled = _otraHora.Checked;
            _horaLibre.Enabled = !_otraHora.Checked;
            ActualizarResumen();
        };
        _guardar.Click += async (_, _) => await Guardar();
    }

    private async Task Cargar()
    {
        try
        {
            var barberos = await _apis.Barberos.GetBarberos();
            _barbero.DisplayMember = nameof(BarberoDto.Nombre);
            _barbero.ValueMember = nameof(BarberoDto.BarberoId);
            _barbero.DataSource = barberos;
            _barbero.SelectedValue = _cita.BarberoId;
            _cargado = true;
            await CargarHoras();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
            Close();
        }
    }

    private async Task CargarHoras()
    {
        if (!_cargado || _barbero.SelectedValue is not int barberoId)
            return;

        var consulta = ++_consulta;
        _horaLibre.DataSource = null;
        _infoHoras.Text = "Buscando horas libres...";
        try
        {
            var disponibilidad = await _apis.Barberos.GetDisponibilidad(barberoId, DateOnly.FromDateTime(_fecha.Value), _duracionMin);
            if (consulta != _consulta)
                return;

            var horas = disponibilidad.Horarios
                .Select(h => new OpcionHora(h.Inicio, $"{h.Inicio:HH:mm} – {h.Fin:HH:mm}"))
                .ToList();
            _horaLibre.DataSource = horas;
            _horaLibre.DisplayMember = nameof(OpcionHora.Texto);
            _infoHoras.Text = horas.Count == 0
                ? "Sin horas libres ese día. Prueba otra fecha u otro barbero."
                : $"{horas.Count} horas libres.";
        }
        catch (Exception ex)
        {
            if (consulta == _consulta)
            {
                _infoHoras.Text = "No se pudieron cargar las horas libres.";
                Mensajes.Error(this, ex);
            }
        }
        ActualizarResumen();
    }

    private DateTime? InicioElegido
    {
        get
        {
            if (_otraHora.Checked)
            {
                var inicio = _fecha.Value.Date.Add(_horaManual.Value.TimeOfDay);
                return inicio.AddSeconds(-inicio.Second).AddMilliseconds(-inicio.Millisecond);
            }
            return (_horaLibre.SelectedItem as OpcionHora)?.Inicio;
        }
    }

    private void ActualizarResumen()
    {
        _resumen.Text = InicioElegido is { } inicio
            ? $"Nueva hora: {inicio:dd/MM/yyyy HH:mm} - {inicio.AddMinutes(_duracionMin):HH:mm}"
            : "Elige una hora.";
    }

    private async Task Guardar()
    {
        if (_barbero.SelectedValue is not int barberoId || InicioElegido is not { } inicio)
        {
            Mensajes.Informacion(this, "Elige el barbero y la nueva hora.");
            return;
        }

        _guardar.Enabled = false;
        try
        {
            // Detalles = null: la API conserva los servicios que ya tiene la cita.
            await _apis.Citas.ModificarCita(_cita.CitaId, _cita with
            {
                BarberoId = barberoId,
                FechaInicio = inicio,
                FechaFin = inicio.AddMinutes(_duracionMin),
                Detalles = null
            });
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
            if (ex is Refit.ApiException { Content: { } contenido } && contenido.Contains("CITA_CHOQUE"))
                await CargarHoras();
        }
        finally
        {
            _guardar.Enabled = true;
        }
    }
}
