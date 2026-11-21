using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Modulos;

namespace BookingHub.WinForms.Formularios;

public class FrmPrincipal : Form
{
    private readonly Panel _contenido = new() { Dock = DockStyle.Fill, Padding = new Padding(32, 24, 32, 24), BackColor = Color.Transparent };
    private readonly FondoVidrio _fondo = new();
    private readonly FlowLayoutPanel _menu = new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = false,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };
    private readonly List<(Button Boton, UserControl Modulo)> _secciones = [];

    public bool CerroSesion { get; private set; }

    public FrmPrincipal(Apis apis, Sesion sesion)
    {
        Text = $"{Ui.Marca} · Recepción y caja";
        Icon = Ui.Icono();
        Font = Ui.Fuente;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1200, 780);
        MinimumSize = new Size(960, 620);
        BackColor = Ui.Fondo;
        DoubleBuffered = true;
        Ui.EfectoVentana(this);

        // El vidrio del encabezado lo pinta la ventana (OnPaintBackground); aquí todo es transparente
        // para que el blanco translúcido no se aplique dos veces sobre el menú y el usuario.
        var encabezado = new Panel { Dock = DockStyle.Top, Height = AltoEncabezado, BackColor = Color.Transparent };
        var linea = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.Transparent };

        var logo = Ui.Logo(30);
        logo.Location = new Point(28, 17);
        var marca = new Label
        {
            Text = Ui.Marca,
            Font = Ui.Destacado,
            ForeColor = Ui.Tinta,
            AutoSize = true,
            Location = new Point(66, 20)
        };

        var usuario = new Label
        {
            Text = $"{sesion.Actual!.Nombre} · {sesion.Actual.Rol}",
            ForeColor = Ui.TextoSuave,
            AutoSize = true
        };
        var cerrarSesion = Ui.Enlace("Cerrar sesión");
        cerrarSesion.Click += (_, _) =>
        {
            CerroSesion = true;
            Close();
        };

        AgregarSeccion("Agenda", new AgendaControl(apis));
        AgregarSeccion("Caja", new CajaControl(apis));
        if (sesion.EsAdmin)
        {
            AgregarSeccion("Catálogo", new CatalogoControl(apis));
            AgregarSeccion("Personal", new PersonalControl(apis));
            AgregarSeccion("Horarios", new HorariosControl(apis));
            AgregarSeccion("Reportes", new ReportesControl(apis));
        }

        encabezado.Controls.AddRange([logo, marca, _menu, usuario, cerrarSesion]);
        // Si heredan el blanco semitransparente del encabezado, se ve un recuadro doble.
        foreach (Control hijo in encabezado.Controls)
            hijo.BackColor = Color.Transparent;
        encabezado.Resize += (_, _) =>
        {
            _menu.Location = new Point(Math.Max(marca.Right + 40, 200), 0);
            cerrarSesion.Location = new Point(encabezado.Width - cerrarSesion.Width - 24, (encabezado.Height - cerrarSesion.Height) / 2);
            usuario.Location = new Point(cerrarSesion.Left - usuario.Width - 8, (encabezado.Height - usuario.Height) / 2);
        };

        Controls.Add(_contenido);
        Controls.Add(linea);
        Controls.Add(encabezado);

        Mostrar(0);
        VigilarSesion(sesion);
    }

    /// <summary>
    /// Vuelve al login cuando el token vence: por la hora de vencimiento o porque la API respondió 401.
    /// </summary>
    private void VigilarSesion(Sesion sesion)
    {
        Mensajes.SesionVencida = false;

        void AlExpirar()
        {
            Mensajes.SesionVencida = true;
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(CerrarPorVencimiento);
        }

        var reloj = new System.Windows.Forms.Timer { Interval = 30_000 };
        reloj.Tick += (_, _) =>
        {
            if (sesion.YaVencio)
                sesion.MarcarExpirada();
        };

        sesion.Expiro += AlExpirar;
        reloj.Start();
        FormClosed += (_, _) =>
        {
            sesion.Expiro -= AlExpirar;
            reloj.Dispose();
        };
    }

    private void CerrarPorVencimiento()
    {
        if (CerroSesion)
            return;
        CerroSesion = true;

        // Cierra primero los diálogos abiertos (nueva cita, cobro...) para no dejarlos huérfanos.
        foreach (var ventana in Application.OpenForms.Cast<Form>().Where(f => f != this).ToList())
            ventana.Close();

        Mensajes.Informacion(this, "Tu sesión expiró. Vuelve a iniciar sesión para continuar.");
        Close();
    }

    private void AgregarSeccion(string titulo, UserControl modulo)
    {
        var indice = _secciones.Count;
        var boton = new BotonMenu
        {
            Text = titulo,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Height = 64,
            MinimumSize = new Size(0, 64),
            Padding = new Padding(14, 0, 14, 0),
            Margin = Padding.Empty,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Ui.TextoSuave,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        boton.FlatAppearance.BorderSize = 0;
        boton.FlatAppearance.MouseOverBackColor = Ui.VidrioHover;
        boton.FlatAppearance.MouseDownBackColor = Ui.VidrioHover;
        boton.Paint += (_, e) =>
        {
            if (_secciones.Count > indice && _secciones[indice].Modulo.Visible)
            {
                using var pincel = new SolidBrush(Ui.Acento);
                e.Graphics.FillRectangle(pincel, 14, boton.Height - 3, boton.Width - 28, 3);
            }
        };
        boton.Click += (_, _) => Mostrar(indice);

        modulo.Dock = DockStyle.Fill;
        modulo.BackColor = Color.Transparent;
        modulo.Visible = false;
        _contenido.Controls.Add(modulo);
        _menu.Controls.Add(boton);
        _secciones.Add((boton, modulo));
    }

    private void Mostrar(int indice)
    {
        for (var i = 0; i < _secciones.Count; i++)
        {
            var (boton, modulo) = _secciones[i];
            var activo = i == indice;
            modulo.Visible = activo;
            boton.ForeColor = activo ? Ui.Tinta : Ui.TextoSuave;
            boton.Invalidate();
        }
    }

    private const int AltoEncabezado = 64;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        _fondo.Pintar(e.Graphics, ClientRectangle);

        using var vidrio = new SolidBrush(Color.FromArgb(140, 255, 255, 255));
        e.Graphics.FillRectangle(vidrio, 0, 0, ClientSize.Width, AltoEncabezado);
        using var linea = new Pen(Ui.VidrioBorde);
        e.Graphics.DrawLine(linea, 0, AltoEncabezado, ClientSize.Width, AltoEncabezado);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    // WS_EX_COMPOSITED: la ventana se compone completa antes de mostrarse y evita parpadeo con tanto control transparente.
    protected override CreateParams CreateParams
    {
        get
        {
            var parametros = base.CreateParams;
            parametros.ExStyle |= 0x02000000;
            return parametros;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _fondo.Dispose();
        base.Dispose(disposing);
    }

    private sealed class BotonMenu : Button
    {
        public BotonMenu() => SetStyle(ControlStyles.Selectable, false);

        protected override bool ShowFocusCues => false;

        public override void NotifyDefault(bool value) => base.NotifyDefault(false);
    }
}
