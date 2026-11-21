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
    private readonly CheckedListBox _servicio = new() { CheckOnClick = true, IntegralHeight = false, Height = 110, MinimumSize = new Size(0, 110), BorderStyle = BorderStyle.FixedSingle };
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

    private record OpcionServicio(CatalogoItemDto Item)
    {
        public override string ToString() => $"{Item.Nombre} · {Mensajes.Precio(Item.Precio)} · {Item.DuracionMin ?? 30} min";
    }

    public FrmNuevaCita(Apis apis, DateTime fecha)
    {
        _apis = apis;
        Text = "Nueva cita";
        Font = Ui.Fuente;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 700);
        BackColor = Ui.Superficie;
        Icon = Ui.Icono();
        Ui.EfectoVentana(this);
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
            ("Servicios", _servicio),
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
        // ItemCheck llega antes de que cambie la marca; se espera a que se aplique.
        _servicio.ItemCheck += (_, _) => BeginInvoke(new Action(async () => await CargarHoras()));
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

            _servicio.Items.Clear();
            _servicio.Items.AddRange(_servicios.Select(s => (object)new OpcionServicio(s)).ToArray());
            if (_servicio.Items.Count > 0)
                _servicio.SetItemChecked(0, true);

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

    private List<CatalogoItemDto> ServiciosElegidos =>
        _servicio.CheckedItems.Cast<OpcionServicio>().Select(o => o.Item).ToList();

    /// <summary>La cita dura lo que suman los servicios elegidos.</summary>
    private int DuracionMin => Math.Max(ServiciosElegidos.Sum(s => s.DuracionMin ?? 30), 5);

    private decimal Total => ServiciosElegidos.Sum(s => s.Precio);

    /// <summary>Pide a la API las horas libres del barbero para el servicio y la fecha elegidos.</summary>
    private async Task CargarHoras()
    {
        if (!_cargado)
            return;

        var consulta = ++_consultaHoras;
        _horaLibre.DataSource = null;

        if (ServiciosElegidos.Count == 0 || _barbero.SelectedValue is not int barberoId)
        {
            _infoHoras.Text = "Elige al menos un servicio y el barbero para ver las horas libres.";
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
        if (ServiciosElegidos.Count == 0)
        {
            _resumen.Text = !_cargado ? string.Empty
                : _servicios.Count == 0 ? "No hay servicios activos en el catálogo."
                : "Elige al menos un servicio.";
            return;
        }
        _resumen.Text = InicioElegido is { } inicio
            ? $"Total {Mensajes.Precio(Total)} · {DuracionMin} min · De {inicio:HH:mm} a {inicio.AddMinutes(DuracionMin):HH:mm}"
            : $"Total {Mensajes.Precio(Total)} · {DuracionMin} min · Elige una hora";
    }

    private async Task Guardar()
    {
        var servicios = ServiciosElegidos;
        if (servicios.Count == 0 || _barbero.SelectedValue is not int barberoId || _estado.SelectedValue is not int estadoId)
        {
            Mensajes.Informacion(this, "Elige al menos un servicio, el barbero y el estado.");
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
            var nombres = string.Join(" + ", servicios.Select(s => s.Nombre));

            await _apis.Citas.CrearCita(new CitaDto
            {
                ClienteId = clienteId,
                BarberoId = barberoId,
                EstadoCitaId = estadoId,
                FechaInicio = inicio,
                FechaFin = inicio.AddMinutes(DuracionMin),
                Total = Total,
                Notas = string.IsNullOrEmpty(notas) ? nombres : $"{nombres} - {notas}",
                Detalles = servicios
                    .Select(s => new CitaDetalleDto { CatalogoItemId = s.CatalogoItemId, Cantidad = 1, PrecioUnitario = s.Precio })
                    .ToList()
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
