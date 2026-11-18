using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Modulos;

namespace BookingHub.WinForms.Formularios;

public class FrmPrincipal : Form
{
    public bool CerroSesion { get; private set; }

    public FrmPrincipal(Apis apis, Sesion sesion)
    {
        Text = "BookingHub - Recepción y caja";
        Font = Ui.Fuente;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1180, 760);
        MinimumSize = new Size(900, 600);
        BackColor = Ui.Fondo;

        var encabezado = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Ui.Marino, Padding = new Padding(16, 0, 16, 0) };
        var marca = new Label
        {
            Text = "BookingHub  Barbería",
            Font = Ui.Destacado,
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 15)
        };
        var usuario = new Label
        {
            Text = $"{sesion.Actual!.Nombre} · {sesion.Actual.Rol}",
            ForeColor = Color.Gainsboro,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        var cerrarSesion = Ui.Boton("Cerrar sesión");
        cerrarSesion.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        cerrarSesion.Click += (_, _) =>
        {
            CerroSesion = true;
            Close();
        };
        encabezado.Controls.AddRange([marca, usuario, cerrarSesion]);
        encabezado.Resize += (_, _) =>
        {
            cerrarSesion.Location = new Point(encabezado.Width - cerrarSesion.Width - 16, 10);
            usuario.Location = new Point(cerrarSesion.Left - usuario.Width - 14, 17);
        };

        var pestanas = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 6) };
        AgregarPestana(pestanas, "Agenda del día", new AgendaControl(apis));
        AgregarPestana(pestanas, "Caja", new CajaControl(apis));
        if (sesion.EsAdmin)
        {
            AgregarPestana(pestanas, "Catálogo", new CatalogoControl(apis));
            AgregarPestana(pestanas, "Personal", new PersonalControl(apis));
            AgregarPestana(pestanas, "Reportes", new ReportesControl(apis));
        }

        Controls.Add(pestanas);
        Controls.Add(encabezado);
    }

    private static void AgregarPestana(TabControl pestanas, string titulo, UserControl modulo)
    {
        var pestana = new TabPage(titulo) { BackColor = Ui.Fondo, Padding = new Padding(16) };
        modulo.Dock = DockStyle.Fill;
        pestana.Controls.Add(modulo);
        pestanas.TabPages.Add(pestana);
    }
}
