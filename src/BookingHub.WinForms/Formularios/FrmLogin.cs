using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;

namespace BookingHub.WinForms.Formularios;

public class FrmLogin : Form
{
    private readonly Apis _apis;
    private readonly Sesion _sesion;
    private readonly TextBox _email = new() { Width = 280 };
    private readonly TextBox _password = new() { Width = 280, UseSystemPasswordChar = true };
    private readonly Button _entrar = Ui.Boton("Entrar", primario: true);
    private readonly Label _error = new() { AutoSize = true, ForeColor = Color.Firebrick, MaximumSize = new Size(320, 0) };
    private readonly FondoVidrio _fondo = new();
    private readonly PanelVidrio _tarjeta = new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(36, 32, 36, 32), Radio = 18 };

    public FrmLogin(Apis apis, Sesion sesion)
    {
        _apis = apis;
        _sesion = sesion;

        Text = $"{Ui.Marca} · Iniciar sesión";
        Icon = Ui.Icono();
        Font = Ui.Fuente;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Ui.Fondo;
        DoubleBuffered = true;
        Ui.EfectoVentana(this);
        AcceptButton = _entrar;

        var contenido = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        // En una columna (FlowDirection.TopDown) Anchor = None centra el control en el ancho de la columna:
        // logo, nombre y subtítulo quedan centrados; los campos ocupan el ancho completo.
        var logo = Ui.Logo(56);
        logo.Anchor = AnchorStyles.None;
        logo.Margin = new Padding(0, 0, 0, 14);
        var nombre = new Label { Text = Ui.Marca, Font = Ui.Titulo, ForeColor = Ui.Tinta, AutoSize = true, Anchor = AnchorStyles.None, Margin = Padding.Empty };
        var subtitulo = new Label { Text = "Recepción y caja", ForeColor = Ui.TextoSuave, AutoSize = true, Anchor = AnchorStyles.None, Margin = new Padding(0, 2, 0, 22) };
        var correo = Ui.Etiqueta("Correo");
        var clave = Ui.Etiqueta("Contraseña");
        foreach (var etiqueta in new[] { correo, clave })
            etiqueta.Margin = new Padding(0, 6, 0, 4);
        _email.Margin = _password.Margin = new Padding(0, 0, 0, 6);
        _error.Margin = new Padding(0, 4, 0, 0);
        _entrar.Margin = new Padding(0, 16, 0, 0);
        _entrar.AutoSize = false;
        _entrar.Size = new Size(_email.Width, 40);

        contenido.Controls.AddRange(logo, nombre, subtitulo, correo, _email, clave, _password, _error, _entrar);

        // Tarjeta de vidrio centrada en la ventana (como d-flex + justify-content-center + align-items-center).
        contenido.Location = new Point(_tarjeta.Padding.Left, _tarjeta.Padding.Top);
        _tarjeta.Controls.Add(contenido);
        Controls.Add(_tarjeta);
        ClientSize = new Size(_tarjeta.PreferredSize.Width + 120, _tarjeta.PreferredSize.Height + 100);
        Resize += (_, _) => Centrar();
        Load += (_, _) => Centrar();

        _entrar.Click += async (_, _) => await Entrar();
    }

    private async Task Entrar()
    {
        _error.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(_email.Text) || string.IsNullOrEmpty(_password.Text))
        {
            _error.Text = "Escribe tu correo y tu contraseña.";
            return;
        }

        _entrar.Enabled = false;
        UseWaitCursor = true;
        try
        {
            var sesion = await _apis.Auth.Login(new LoginDto(_email.Text.Trim(), _password.Text));
            if (sesion.Rol is not ("Admin" or "Recepcion"))
            {
                _error.Text = "Este programa es solo para el personal de recepción y administración.";
                return;
            }

            _sesion.Iniciar(sesion);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _error.Text = Mensajes.Describir(ex);
        }
        finally
        {
            _entrar.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void Centrar()
    {
        _tarjeta.Location = new Point(
            Math.Max(0, (ClientSize.Width - _tarjeta.Width) / 2),
            Math.Max(0, (ClientSize.Height - _tarjeta.Height) / 2));
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e) => _fondo.Pintar(e.Graphics, ClientRectangle);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _fondo.Dispose();
        base.Dispose(disposing);
    }
}
