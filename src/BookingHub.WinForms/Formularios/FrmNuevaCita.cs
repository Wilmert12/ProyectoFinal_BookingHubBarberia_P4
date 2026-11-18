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
    private readonly DateTimePicker _hora = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true };
    private readonly TextBox _notas = new();
    private readonly Label _resumen = new() { AutoSize = true, ForeColor = Ui.TextoSuave };
    private readonly Button _guardar = Ui.Boton("Guardar cita", primario: true);
    private List<CatalogoItemDto> _servicios = [];

    private record OpcionCliente(int ClienteId, string Texto);

    public FrmNuevaCita(Apis apis, DateTime fecha)
    {
        _apis = apis;
        Text = "Nueva cita";
        Font = Ui.Fuente;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 520);
        BackColor = Ui.Superficie;
        Icon = Ui.Icono();
        AcceptButton = _guardar;

        _fecha.Value = fecha;
        var ahora = DateTime.Now;
        _hora.Value = DateTime.Today.AddHours(ahora.Hour).AddMinutes(ahora.Minute < 30 ? 30 : 60);

        var formulario = Ui.Formulario(
            ("Cliente", _cliente),
            ("", _clienteNuevo),
            ("Nombre", _nombre),
            ("Teléfono", _telefono),
            ("Servicio", _servicio),
            ("Barbero", _barbero),
            ("Fecha", _fecha),
            ("Hora", _hora),
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
        _servicio.SelectedIndexChanged += (_, _) => ActualizarResumen();
        _hora.ValueChanged += (_, _) => ActualizarResumen();
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

            ActualizarResumen();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
            Close();
        }
    }

    private CatalogoItemDto? ServicioSeleccionado =>
        _servicios.FirstOrDefault(s => s.CatalogoItemId == (_servicio.SelectedValue as int? ?? 0));

    private void ActualizarResumen()
    {
        if (ServicioSeleccionado is not { } servicio)
        {
            _resumen.Text = "No hay servicios activos en el catálogo.";
            return;
        }
        var fin = _hora.Value.AddMinutes(servicio.DuracionMin ?? 30);
        _resumen.Text = $"Total {Mensajes.Precio(servicio.Precio)} · Termina a las {fin:HH:mm}";
    }

    private async Task Guardar()
    {
        if (ServicioSeleccionado is not { } servicio || _barbero.SelectedValue is not int barberoId || _estado.SelectedValue is not int estadoId)
        {
            Mensajes.Informacion(this, "Elige el servicio, el barbero y el estado.");
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
            var clienteId = _clienteNuevo.Checked
                ? (await _apis.Clientes.CrearCliente(new ClienteDto { Nombre = _nombre.Text.Trim(), Telefono = _telefono.Text.Trim() })).ClienteId
                : (int)_cliente.SelectedValue!;

            var inicio = _fecha.Value.Date.Add(_hora.Value.TimeOfDay);
            inicio = inicio.AddSeconds(-inicio.Second);
            var notas = _notas.Text.Trim();

            await _apis.Citas.CrearCita(new CitaDto
            {
                ClienteId = clienteId,
                BarberoId = barberoId,
                EstadoCitaId = estadoId,
                FechaInicio = inicio,
                FechaFin = inicio.AddMinutes(servicio.DuracionMin ?? 30),
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
        }
        finally
        {
            _guardar.Enabled = true;
        }
    }
}
