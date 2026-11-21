using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;

namespace BookingHub.WinForms.Modulos;

public class PersonalControl : UserControl
{
    private const string RolBarbero = "Barbero";

    private readonly Apis _apis;
    private readonly DataGridView _grilla = Ui.Grilla();
    private readonly TextBox _nombre = new();
    private readonly TextBox _email = new();
    private readonly TextBox _telefono = new();
    private readonly ComboBox _rol = Ui.Lista();
    private readonly TextBox _password = new() { UseSystemPasswordChar = true, PlaceholderText = "Vacío = no cambiar" };
    private readonly CheckBox _activo = new() { Text = "Activo", AutoSize = true, Checked = true };
    private readonly TextBox _apodo = new();
    private readonly TextBox _cedula = new();
    private readonly NumericUpDown _comision = Ui.Numero(100, 0);
    private readonly Button _color = Ui.Boton("Elegir color");
    private readonly GroupBox _grupoBarbero = new() { Text = "Datos de barbero", AutoSize = true, Padding = new Padding(10) };
    private readonly Label _modo = new() { AutoSize = true, Font = Ui.Destacado, Padding = new Padding(0, 0, 0, 6) };
    private List<UsuarioDto> _usuarios = [];
    private List<BarberoDto> _barberos = [];
    private int _usuarioId;
    private string _colorHex = "#1E88E5";

    private sealed record FilaUsuario(int Id, string Nombre, string Email, string Rol, string Barbero, bool Activo, string UltimoLogin);

    public PersonalControl(Apis apis)
    {
        _apis = apis;

        _comision.Value = 50;
        _grupoBarbero.Controls.Add(Ui.Formulario(("Apodo", _apodo), ("Cédula", _cedula), ("Comisión (%)", _comision), ("Color", _color)));
        var nuevo = Ui.Boton("Nuevo");
        var guardar = Ui.Boton("Guardar", primario: true);

        var formulario = Ui.Formulario(
            ("Nombre", _nombre),
            ("Correo", _email),
            ("Teléfono", _telefono),
            ("Rol", _rol),
            ("Contraseña", _password),
            ("", _activo));

        var panelFormulario = new Panel { Dock = DockStyle.Right, Width = 400, Padding = new Padding(16, 0, 0, 0) };
        panelFormulario.Controls.Add(Ui.EnVidrio(Ui.Columna(_modo, formulario, _grupoBarbero, Ui.Barra(nuevo, guardar)), llenar: false));

        Controls.Add(Ui.EnVidrio(_grilla));
        Controls.Add(panelFormulario);
        Controls.Add(Ui.TituloModulo("Personal y usuarios del sistema"));

        Load += async (_, _) => await Inicializar();
        _grilla.SelectionChanged += (_, _) => Editar();
        _rol.SelectedIndexChanged += (_, _) => _grupoBarbero.Visible = EsRolBarbero();
        _color.Click += (_, _) => ElegirColor();
        nuevo.Click += (_, _) => Limpiar();
        guardar.Click += async (_, _) => await Guardar();
    }

    private async Task Inicializar()
    {
        try
        {
            var roles = await _apis.Catalogos.GetRoles();
            _rol.DisplayMember = nameof(RolDto.Descripcion);
            _rol.ValueMember = nameof(RolDto.RolId);
            _rol.DataSource = roles.Where(r => r.Descripcion != "Cliente").ToList();
            Limpiar();
            await Cargar();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private async Task Cargar(int? seleccionar = null)
    {
        try
        {
            _usuarios = await _apis.Usuarios.GetUsuarios(incluirInactivos: true);
            _barberos = await _apis.Barberos.GetBarberos(incluirInactivos: true);

            Ui.Mostrar(_grilla, _usuarios.Select(u => new FilaUsuario(
                u.UsuarioId,
                u.Nombre,
                u.Email,
                u.Rol ?? "",
                _barberos.FirstOrDefault(b => b.UsuarioId == u.UsuarioId)?.Apodo ?? "",
                u.Activo,
                u.UltimoLogin?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca")));

            if (seleccionar is { } id)
                foreach (DataGridViewRow fila in _grilla.Rows)
                    if (fila.DataBoundItem is FilaUsuario usuario && usuario.Id == id)
                        _grilla.CurrentCell = fila.Cells[nameof(FilaUsuario.Nombre)];
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private bool EsRolBarbero() => (_rol.SelectedItem as RolDto)?.Descripcion == RolBarbero;

    private BarberoDto? BarberoDe(int usuarioId) => _barberos.FirstOrDefault(b => b.UsuarioId == usuarioId);

    private void Editar()
    {
        if (Ui.Seleccionado<FilaUsuario>(_grilla) is not { } fila || _usuarios.FirstOrDefault(u => u.UsuarioId == fila.Id) is not { } usuario)
            return;

        _usuarioId = usuario.UsuarioId;
        _modo.Text = $"Editando: {usuario.Nombre}";
        _nombre.Text = usuario.Nombre;
        _email.Text = usuario.Email;
        _telefono.Text = usuario.Telefono;
        _rol.SelectedValue = usuario.RolId;
        _password.Text = string.Empty;
        _activo.Checked = usuario.Activo;

        var barbero = BarberoDe(usuario.UsuarioId);
        _apodo.Text = barbero?.Apodo;
        _comision.Value = Math.Clamp(barbero?.ComisionPorcentaje ?? 50, 0, 100);
        _cedula.Text = string.Empty;
        _cedula.PlaceholderText = barbero is null ? "Obligatoria" : "Vacío = no cambiar";
        PonerColor(barbero?.ColorHex ?? "#1E88E5");
    }

    private void Limpiar()
    {
        _usuarioId = 0;
        _modo.Text = "Nuevo usuario";
        _nombre.Text = _email.Text = _telefono.Text = _password.Text = _apodo.Text = _cedula.Text = string.Empty;
        _comision.Value = 50;
        _password.PlaceholderText = "Mínimo 8 caracteres";
        _cedula.PlaceholderText = "Obligatoria";
        _activo.Checked = true;
        if (_rol.Items.Count > 0)
            _rol.SelectedIndex = 0;
        PonerColor("#1E88E5");
        _grupoBarbero.Visible = EsRolBarbero();
        _grilla.ClearSelection();
    }

    private void ElegirColor()
    {
        using var dialogo = new ColorDialog { Color = ColorTranslator.FromHtml(_colorHex), FullOpen = true };
        if (dialogo.ShowDialog(this) == DialogResult.OK)
            PonerColor($"#{dialogo.Color.R:X2}{dialogo.Color.G:X2}{dialogo.Color.B:X2}");
    }

    private void PonerColor(string hex)
    {
        _colorHex = hex;
        var color = ColorTranslator.FromHtml(hex);
        _color.BackColor = color;
        _color.ForeColor = color.GetBrightness() < 0.55f ? Color.White : Color.Black;
        _color.Text = hex;
    }

    private async Task Guardar()
    {
        if (_rol.SelectedItem is not RolDto rol)
            return;
        if (string.IsNullOrWhiteSpace(_nombre.Text) || string.IsNullOrWhiteSpace(_email.Text))
        {
            Mensajes.Informacion(this, "El nombre y el correo son obligatorios.");
            return;
        }
        if (_usuarioId == 0 && _password.Text.Length < 8)
        {
            Mensajes.Informacion(this, "La contraseña debe tener al menos 8 caracteres.");
            return;
        }

        var barbero = BarberoDe(_usuarioId);
        var esBarbero = rol.Descripcion == RolBarbero;
        if (esBarbero && barbero is null && string.IsNullOrWhiteSpace(_cedula.Text))
        {
            Mensajes.Informacion(this, "Para un barbero nuevo la cédula es obligatoria.");
            return;
        }

        var usuario = new UsuarioDto
        {
            UsuarioId = _usuarioId,
            Nombre = _nombre.Text.Trim(),
            Email = _email.Text.Trim(),
            Telefono = string.IsNullOrWhiteSpace(_telefono.Text) ? null : _telefono.Text.Trim(),
            RolId = rol.RolId,
            Activo = _activo.Checked,
            Password = string.IsNullOrEmpty(_password.Text) ? null : _password.Text
        };

        try
        {
            if (_usuarioId == 0)
                usuario = await _apis.Usuarios.CrearUsuario(usuario);
            else
                await _apis.Usuarios.ModificarUsuario(_usuarioId, usuario);

            if (esBarbero || barbero is not null)
                await GuardarBarbero(usuario.UsuarioId, barbero, activo: esBarbero && _activo.Checked);

            _password.Text = string.Empty;
            await Cargar(usuario.UsuarioId);
            Mensajes.Informacion(this, "Usuario guardado.");
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private async Task GuardarBarbero(int usuarioId, BarberoDto? existente, bool activo)
    {
        var datos = new BarberoDto
        {
            BarberoId = existente?.BarberoId ?? 0,
            UsuarioId = usuarioId,
            Apodo = string.IsNullOrWhiteSpace(_apodo.Text) ? null : _apodo.Text.Trim(),
            Cedula = string.IsNullOrWhiteSpace(_cedula.Text) ? null : _cedula.Text.Trim(),
            ComisionPorcentaje = _comision.Value,
            ColorHex = _colorHex,
            Activo = activo
        };

        if (existente is null)
            await _apis.Barberos.CrearBarbero(datos);
        else
            await _apis.Barberos.ModificarBarbero(existente.BarberoId, datos);
    }
}
