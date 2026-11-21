using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;

namespace BookingHub.WinForms.Formularios;

public class FrmNuevaCita : Form
{
    private readonly Apis _apis;
    private readonly ComboBox _cliente = Ui.Lista();
    private readonly CheckBox _clienteNuevo = new() { Text = "Es un cliente nuevo", AutoSize = true };
    private readonly TextBox _nombre = new() { Enabled = false };
    private readonly TextBox _telefono = new() { Enabled = false };
    private readonly ComboBox _servicio = Ui.Lista();
    private readonly ComboBox _barbero = Ui.Lista();
    private readonly ComboBox _estado = Ui.Lista();
    private readonly DateTimePicker _fecha = new() { Format = DateTimePickerFormat.Short };
    private readonly ComboBox _horaLibre = Ui.Lista();
    private readonly Label _infoHoras = new() { AutoSize = true, ForeColor = Ui.TextoSuave };
    private readonly CheckBox _otraHora = new() { Text = "Usar otra hora (cliente sin cita previa)", AutoSize = true };
    private readonly DateTimePicker _horaManual = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Enabled = false };
    private readonly TextBox _notas = new();
    private readonly Label _resumen = new() { AutoSize = true, ForeColor = Ui.TextoSuave };
    private readonly Button _guardar = Ui.Boton("Guardar cita", primario: true);
    private List<CatalogoItemDto> _servicios = [];
    private bool _cargado;
    private int _consultaHoras;

    // Si el cliente nuevo se creó pero la cita falló, al reintentar se usa el mismo cliente.
    private int? _clienteCreadoId;

    private record OpcionCliente(int ClienteId, string Texto);
    private record OpcionHora(DateTime Inicio, string Texto);

    public FrmNuevaCita(Apis apis, DateTime fecha)
    {
        _apis = apis;
        Text = "Nueva cita";
        Font = Ui.Fuente;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(540, 600);
        BackColor = Ui.Superficie;
        Icon = Ui.Icono();
        AcceptButton = _guardar;

        _fecha.Value = fecha.Date < DateTime.Today ? DateTime.Today : fecha.Date;
        var ahora = DateTime.Now;
        _horaManual.Value = DateTime.Today.AddHours(ahora.Hour).AddMinutes(ahora.Minute < 30 ? 30 : 60);
        _horaLibre.DisplayMember = nameof(OpcionHora.Texto);

        var formulario = Ui.Formulario(
            ("Cliente", _cliente),
            ("", _clienteNuevo),
            ("Nombre", _nombre),
            ("Teléfono", _telefono),
            ("Servicio", _servicio),
            ("Barbero", _barbero),
            ("Fecha", _fecha),
            ("Hora libre", _horaLibre),
            ("", _infoHoras),
            ("", _otraHora),
            ("Otra hora", _horaManual),
            ("Estado", _estado),
            ("Notas", _notas),
            ("", _resumen));

        var cancelar = Ui.Boton("Cancelar");
        cancelar.DialogResult = DialogResult.Cancel;
        CancelButton = cancelar;
        var botones = Ui.Barra(_guardar, cancelar);
        botones.Dock = DockStyle.Bottom;

        var contenedor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };
        contenedor.Controls.Add(formulario);
        contenedor.Controls.Add(botones);
        Controls.Add(contenedor);

        Load += async (_, _) => await Cargar();
        _clienteNuevo.CheckedChanged += (_, _) =>
        {
            _cliente.Enabled = !_clienteNuevo.Checked;
            _nombre.Enabled = _telefono.Enabled = _clienteNuevo.Checked;
        };
        _nombre.TextChanged += (_, _) => _clienteCreadoId = null;
        _telefono.TextChanged += (_, _) => _clienteCreadoId = null;
        _servicio.SelectedIndexChanged += async (_, _) => await CargarHoras();
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
            var clientes = await _apis.Clientes.GetClientes();
            _servicios = (await _apis.Catalogo.GetCatalogoItems()).Where(i => i.EsServicio).ToList();
            var barberos = await _apis.Barberos.GetBarberos();
            var estados = await _apis.Catalogos.GetEstadosCita();

            _cliente.DisplayMember = nameof(OpcionCliente.Texto);
            _cliente.ValueMember = nameof(OpcionCliente.ClienteId);
            _cliente.DataSource = clientes.Select(c => new OpcionCliente(c.ClienteId, $"{c.Nombre} · {c.Telefono}")).ToList();

            _servicio.DisplayMember = nameof(CatalogoItemDto.Nombre);
            _servicio.ValueMember = nameof(CatalogoItemDto.CatalogoItemId);
            _servicio.DataSource = _servicios;

            _barbero.DisplayMember = nameof(BarberoDto.Nombre);
            _barbero.ValueMember = nameof(BarberoDto.BarberoId);
            _barbero.DataSource = barberos;

            _estado.DisplayMember = nameof(EstadoCitaDto.Descripcion);
            _estado.ValueMember = nameof(EstadoCitaDto.EstadoCitaId);
            _estado.DataSource = estados.Where(e => !e.LiberaHorario).ToList();

            _cargado = true;
            await CargarHoras();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
            Close();
        }
    }

    private CatalogoItemDto? ServicioSeleccionado =>
        _servicios.FirstOrDefault(s => s.CatalogoItemId == (_servicio.SelectedValue as int? ?? 0));

    private int DuracionMin => ServicioSeleccionado?.DuracionMin ?? 30;

    /// <summary>Pide a la API las horas libres del barbero para el servicio y la fecha elegidos.</summary>
    private async Task CargarHoras()
    {
        if (!_cargado)
            return;

        var consulta = ++_consultaHoras;
        _horaLibre.DataSource = null;

        if (ServicioSeleccionado is null || _barbero.SelectedValue is not int barberoId)
        {
            _infoHoras.Text = "Elige el servicio y el barbero para ver las horas libres.";
            ActualizarResumen();
            return;
        }

        _infoHoras.Text = "Buscando horas libres...";
        try
        {
            var disponibilidad = await _apis.Barberos.GetDisponibilidad(barberoId, DateOnly.FromDateTime(_fecha.Value), DuracionMin);
            if (consulta != _consultaHoras)
                return;

            var horas = disponibilidad.Horarios
                .Select(h => new OpcionHora(h.Inicio, $"{h.Inicio:HH:mm} – {h.Fin:HH:mm}"))
                .ToList();
            _horaLibre.DataSource = horas;
            _horaLibre.DisplayMember = nameof(OpcionHora.Texto);
            _infoHoras.Text = horas.Count == 0
                ? "Este barbero no tiene horas libres ese día."
                : $"{horas.Count} horas libres.";
        }
        catch (Exception ex)
        {
            if (consulta != _consultaHoras)
                return;
            _infoHoras.Text = "No se pudieron cargar las horas libres.";
            Mensajes.Error(this, ex);
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
        if (ServicioSeleccionado is not { } servicio)
        {
            _resumen.Text = _cargado ? "No hay servicios activos en el catálogo." : string.Empty;
            return;
        }
        _resumen.Text = InicioElegido is { } inicio
            ? $"Total {Mensajes.Precio(servicio.Precio)} · De {inicio:HH:mm} a {inicio.AddMinutes(DuracionMin):HH:mm}"
            : $"Total {Mensajes.Precio(servicio.Precio)} · Elige una hora";
    }

    private async Task Guardar()
    {
        if (ServicioSeleccionado is not { } servicio || _barbero.SelectedValue is not int barberoId || _estado.SelectedValue is not int estadoId)
        {
            Mensajes.Informacion(this, "Elige el servicio, el barbero y el estado.");
            return;
        }
        if (InicioElegido is not { } inicio)
        {
            Mensajes.Informacion(this, "Elige una hora libre o marca que vas a usar otra hora.");
            return;
        }
        if (_clienteNuevo.Checked && (string.IsNullOrWhiteSpace(_nombre.Text) || string.IsNullOrWhiteSpace(_telefono.Text)))
        {
            Mensajes.Informacion(this, "Escribe el nombre y el teléfono del cliente nuevo.");
            return;
        }
        if (!_clienteNuevo.Checked && _cliente.SelectedValue is not int)
        {
            Mensajes.Informacion(this, "Elige el cliente o marca que es un cliente nuevo.");
            return;
        }

        _guardar.Enabled = false;
        try
        {
            int clienteId;
            if (_clienteNuevo.Checked)
            {
                _clienteCreadoId ??= (await _apis.Clientes.CrearCliente(new ClienteDto { Nombre = _nombre.Text.Trim(), Telefono = _telefono.Text.Trim() })).ClienteId;
                clienteId = _clienteCreadoId.Value;
            }
            else
            {
                clienteId = (int)_cliente.SelectedValue!;
            }

            var notas = _notas.Text.Trim();

            await _apis.Citas.CrearCita(new CitaDto
            {
                ClienteId = clienteId,
                BarberoId = barberoId,
                EstadoCitaId = estadoId,
                FechaInicio = inicio,
                FechaFin = inicio.AddMinutes(DuracionMin),
                Total = servicio.Precio,
                Notas = string.IsNullOrEmpty(notas) ? servicio.Nombre : $"{servicio.Nombre} - {notas}",
                Detalles = [new CitaDetalleDto { CatalogoItemId = servicio.CatalogoItemId, Cantidad = 1, PrecioUnitario = servicio.Precio }]
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
