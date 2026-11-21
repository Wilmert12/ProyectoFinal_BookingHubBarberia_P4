using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;

namespace BookingHub.WinForms.Modulos;

/// <summary>
/// Ficha de clientes: buscar, corregir datos e historial de citas.
/// El teléfono es lo que el cliente usa para ver y reservar citas en la web, por eso se puede corregir aquí.
/// </summary>
public class ClientesControl : UserControl
{
    private readonly Apis _apis;
    private readonly DataGridView _grilla = Ui.Grilla();
    private readonly DataGridView _historial = Ui.Grilla();
    private readonly TextBox _buscar = new() { Width = 260, PlaceholderText = "Buscar por nombre, teléfono o número" };
    private readonly CheckBox _inactivos = new() { Text = "Mostrar inactivos", AutoSize = true };
    private readonly TextBox _nombre = new() { MaxLength = 200 };
    private readonly TextBox _telefono = new() { MaxLength = 20 };
    private readonly TextBox _email = new() { MaxLength = 255 };
    private readonly TextBox _notas = new() { Multiline = true, Height = 54, MaxLength = 500 };
    private readonly CheckBox _activo = new() { Text = "Activo", AutoSize = true, Checked = true };
    private readonly Label _modo = new() { AutoSize = true, Font = Ui.Destacado, Padding = new Padding(0, 0, 0, 6) };
    private readonly Label _resumen = new() { AutoSize = true, ForeColor = Ui.TextoSuave, MaximumSize = new Size(440, 0), Padding = new Padding(0, 0, 0, 6) };
    private List<ClienteDto> _clientes = [];
    private int _clienteId;
    private int _consultaHistorial;

    private sealed record FilaCliente(int Id, int Numero, string Nombre, string Telefono, string Correo, bool Activo);

    private sealed record FilaHistorial(string Fecha, string Servicios, string Estado, decimal Total);

    public ClientesControl(Apis apis)
    {
        _apis = apis;

        var nuevo = Ui.Boton("Nuevo");
        var guardar = Ui.Boton("Guardar", primario: true);
        var formulario = Ui.Formulario(
            ("Nombre", _nombre),
            ("Teléfono", _telefono),
            ("Correo", _email),
            ("Notas", _notas),
            ("", _activo));

        var columnaFormulario = Ui.Columna(_modo, formulario, Ui.Barra(nuevo, guardar));
        var tarjetaFormulario = Ui.EnVidrio(columnaFormulario, llenar: false);
        tarjetaFormulario.Dock = DockStyle.Top;
        tarjetaFormulario.AutoScroll = false;
        // Alto justo del formulario: así el historial de abajo aprovecha el resto.
        tarjetaFormulario.Layout += (_, _) =>
        {
            var alto = columnaFormulario.Height + tarjetaFormulario.Padding.Vertical;
            if (tarjetaFormulario.Height != alto)
                tarjetaFormulario.Height = alto;
        };

        var tituloHistorial = new Label { Text = "Historial de citas", Font = Ui.Destacado, AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 14, 0, 4) };
        _resumen.Dock = DockStyle.Top;
        var tarjetaHistorial = Ui.EnVidrio(_historial);

        var panelDerecho = new Panel { Dock = DockStyle.Right, Width = 470, Padding = new Padding(16, 0, 0, 0) };
        panelDerecho.Controls.Add(tarjetaHistorial);
        panelDerecho.Controls.Add(_resumen);
        panelDerecho.Controls.Add(tituloHistorial);
        panelDerecho.Controls.Add(tarjetaFormulario);

        Controls.Add(Ui.EnVidrio(_grilla));
        Controls.Add(panelDerecho);
        Controls.Add(Ui.Barra(_buscar, _inactivos));
        Controls.Add(Ui.TituloModulo("Clientes"));

        Load += async (_, _) => await Cargar();
        VisibleChanged += async (_, _) =>
        {
            if (Visible && IsHandleCreated)
                await Cargar(_clienteId == 0 ? null : _clienteId);
        };
        _buscar.TextChanged += (_, _) => Mostrar();
        _inactivos.CheckedChanged += async (_, _) => await Cargar();
        _grilla.SelectionChanged += async (_, _) => await Editar();
        nuevo.Click += (_, _) => Limpiar();
        guardar.Click += async (_, _) => await Guardar();
    }

    private async Task Cargar(int? seleccionar = null)
    {
        try
        {
            _clientes = await _apis.Clientes.GetClientes(_inactivos.Checked);
            Mostrar(seleccionar);
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private void Mostrar(int? seleccionar = null)
    {
        var filtro = _buscar.Text.Trim();
        var digitos = new string(filtro.Where(char.IsDigit).ToArray());
        var lista = _clientes
            .Where(c => filtro.Length == 0
                || c.Nombre.Contains(filtro, StringComparison.CurrentCultureIgnoreCase)
                || (digitos.Length > 0 && (new string(c.Telefono.Where(char.IsDigit).ToArray()).Contains(digitos) || c.ClienteId.ToString() == digitos)))
            .OrderBy(c => c.Nombre)
            .Select(c => new FilaCliente(c.ClienteId, c.ClienteId, c.Nombre, c.Telefono, c.Email ?? "", c.Activo))
            .ToList();

        Ui.Mostrar(_grilla, lista);
        _grilla.Columns[nameof(FilaCliente.Numero)]!.HeaderText = "Número";
        _grilla.Columns[nameof(FilaCliente.Numero)]!.FillWeight = 60;

        if (seleccionar is { } id)
        {
            foreach (DataGridViewRow fila in _grilla.Rows)
                if (fila.DataBoundItem is FilaCliente f && f.Id == id)
                    _grilla.CurrentCell = fila.Cells[nameof(FilaCliente.Nombre)];
        }
        else if (_clienteId == 0)
        {
            _grilla.ClearSelection();
        }
    }

    private async Task Editar()
    {
        // SelectedRows primero: al seleccionar desde el código, CurrentRow puede seguir en la fila anterior.
        var actual = _grilla.SelectedRows.Count > 0 ? _grilla.SelectedRows[0] : _grilla.CurrentRow;
        if (actual?.DataBoundItem is not FilaCliente fila || _clientes.FirstOrDefault(c => c.ClienteId == fila.Id) is not { } cliente)
            return;
        if (cliente.ClienteId == _clienteId && _nombre.Text == cliente.Nombre)
            return;

        _clienteId = cliente.ClienteId;
        _modo.Text = $"Cliente #{cliente.ClienteId}";
        _nombre.Text = cliente.Nombre;
        _telefono.Text = cliente.Telefono;
        _email.Text = cliente.Email;
        _notas.Text = cliente.Notas;
        _activo.Checked = cliente.Activo;
        await CargarHistorial(cliente.ClienteId);
    }

    private async Task CargarHistorial(int clienteId)
    {
        var consulta = ++_consultaHistorial;
        try
        {
            var citas = (await _apis.Citas.GetCitasCliente(clienteId)).OrderByDescending(c => c.FechaInicio).ToList();
            // Si mientras tanto se eligió otro cliente, este historial ya no sirve.
            if (consulta != _consultaHistorial)
                return;
            Ui.Mostrar(_historial, citas.Select(c => new FilaHistorial(
                c.FechaInicio.ToString("dd/MM/yy"),
                c.Detalles is { Count: > 0 } d ? string.Join(", ", d.Select(x => x.Nombre)) : c.Notas ?? "",
                c.Estado ?? "",
                c.Total)), nameof(FilaHistorial.Total));

            var completadas = citas.Where(c => c.Estado == "Completada").ToList();
            var noVino = citas.Count(c => c.Estado == "NoShow");
            var ultima = completadas.FirstOrDefault();
            var visita = ultima is null ? "" : $"\nÚltima visita: {ultima.FechaInicio:dd/MM/yyyy} con {ultima.BarberoNombre}";
            _resumen.Text = citas.Count == 0
                ? "Todavía no tiene citas."
                : $"{citas.Count} citas · {completadas.Count} completadas · {noVino} no asistió · Pagado {Mensajes.Precio(citas.Sum(c => c.Pagado))}{visita}";
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private void Limpiar()
    {
        _clienteId = 0;
        _modo.Text = "Nuevo cliente";
        _nombre.Text = _telefono.Text = _email.Text = _notas.Text = string.Empty;
        _activo.Checked = true;
        _resumen.Text = string.Empty;
        _historial.DataSource = null;
        _grilla.ClearSelection();
        _nombre.Focus();
    }

    private async Task Guardar()
    {
        var digitos = _telefono.Text.Count(char.IsDigit);
        if (string.IsNullOrWhiteSpace(_nombre.Text) || digitos < 7)
        {
            Mensajes.Informacion(this, "Escribe el nombre y un teléfono válido (al menos 7 dígitos).");
            return;
        }

        var original = _clientes.FirstOrDefault(c => c.ClienteId == _clienteId);
        if (original is not null && original.Telefono != _telefono.Text.Trim()
            && !Mensajes.Confirmar(this, "El cliente usa su teléfono para ver y reservar citas en la web. ¿Cambiarlo?"))
            return;

        var cliente = new ClienteDto
        {
            ClienteId = _clienteId,
            Nombre = _nombre.Text.Trim(),
            Telefono = _telefono.Text.Trim(),
            Email = string.IsNullOrWhiteSpace(_email.Text) ? null : _email.Text.Trim(),
            Notas = string.IsNullOrWhiteSpace(_notas.Text) ? null : _notas.Text.Trim(),
            Activo = _activo.Checked
        };

        try
        {
            if (_clienteId == 0)
                cliente = await _apis.Clientes.CrearCliente(cliente);
            else
                await _apis.Clientes.ModificarCliente(_clienteId, cliente);

            _clienteId = cliente.ClienteId;
            await Cargar(cliente.ClienteId);
            Mensajes.Informacion(this, $"Cliente #{cliente.ClienteId} guardado.");
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }
}
