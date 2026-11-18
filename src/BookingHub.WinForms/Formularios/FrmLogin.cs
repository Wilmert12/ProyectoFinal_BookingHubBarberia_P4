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

    public FrmLogin(Apis apis, Sesion sesion)
    {
        _apis = apis;
        _sesion = sesion;

        Text = "BookingHub - Iniciar sesión";
        Font = Ui.Fuente;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.White;
        AcceptButton = _entrar;

        var contenido = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(28, 24, 28, 24)
        };
        contenido.Controls.Add(new Label { Text = "BookingHub", Font = Ui.Titulo, AutoSize = true });
        contenido.Controls.Add(new Label { Text = "Recepción y caja de la barbería", ForeColor = Color.DimGray, AutoSize = true, Margin = new Padding(3, 0, 3, 16) });
        contenido.Controls.Add(Ui.Etiqueta("Correo"));
        contenido.Controls.Add(_email);
        contenido.Controls.Add(Ui.Etiqueta("Contraseña"));
        contenido.Controls.Add(_password);
        contenido.Controls.Add(_error);
        _entrar.Margin = new Padding(3, 14, 3, 3);
        contenido.Controls.Add(_entrar);
        Controls.Add(contenido);

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
}
