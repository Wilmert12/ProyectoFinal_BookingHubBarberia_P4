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
        var logo = Ui.Logo(56);
        logo.Margin = new Padding(3, 0, 3, 18);
        contenido.Controls.Add(logo);
        contenido.Controls.Add(new Label { Text = Ui.Marca, Font = Ui.Titulo, ForeColor = Ui.Tinta, AutoSize = true });
        contenido.Controls.Add(new Label { Text = "Recepción y caja", ForeColor = Ui.TextoSuave, AutoSize = true, Margin = new Padding(3, 2, 3, 24) });
        contenido.Controls.Add(Ui.Etiqueta("Correo"));
        contenido.Controls.Add(_email);
        contenido.Controls.Add(Ui.Etiqueta("Contraseña"));
        contenido.Controls.Add(_password);
        contenido.Controls.Add(_error);
        _entrar.Margin = new Padding(3, 20, 3, 3);
        _entrar.AutoSize = false;
        _entrar.Size = new Size(_email.Width, 38);
        contenido.Controls.Add(_entrar);

        // Tarjeta de vidrio centrada en la ventana (como d-flex + justify-content-center + align-items-center).
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
