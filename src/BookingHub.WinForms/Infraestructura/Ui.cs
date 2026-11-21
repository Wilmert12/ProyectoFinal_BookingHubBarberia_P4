using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;

namespace BookingHub.WinForms.Infraestructura;

public static class Ui
{
    public const string Marca = "CerquilloApp";

    public static readonly Color Tinta = Color.FromArgb(23, 21, 15);
    public static readonly Color Acento = Color.FromArgb(201, 161, 91);
    public static readonly Color Fondo = Color.FromArgb(246, 243, 236);
    public static readonly Color Superficie = Color.White;
    public static readonly Color Borde = Color.FromArgb(230, 227, 220);

    // Vidrio: blanco semitransparente sobre el fondo con manchas de color.
    public static readonly Color Vidrio = Color.FromArgb(150, 255, 255, 255);
    public static readonly Color VidrioBorde = Color.FromArgb(220, 255, 255, 255);
    public static readonly Color VidrioBoton = Color.FromArgb(190, 255, 255, 255);
    public static readonly Color VidrioHover = Color.FromArgb(110, 255, 255, 255);
    // Tono de las grillas dentro de una tarjeta de vidrio (casi blanco, cálido).
    public static readonly Color GrillaFondo = Color.FromArgb(251, 250, 247);
    public static readonly Color TextoSuave = Color.FromArgb(120, 115, 105);
    public static readonly Color Seleccion = Color.FromArgb(243, 237, 225);
    public static readonly Font Fuente = new("Segoe UI", 10f);
    public static readonly Font Titulo = new("Segoe UI Semibold", 16f);
    public static readonly Font Destacado = new("Segoe UI Semibold", 11f);
    public static readonly Font Pequena = new("Segoe UI", 9f);

    public static readonly CultureInfo Moneda = CrearMoneda();

    // Pesos dominicanos: RD$1,250.00
    private static CultureInfo CrearMoneda()
    {
        var cultura = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        cultura.NumberFormat.CurrencySymbol = "RD$";
        cultura.NumberFormat.CurrencyPositivePattern = 0;
        cultura.NumberFormat.CurrencyNegativePattern = 1;
        return cultura;
    }

    public static Label TituloModulo(string texto) => new()
    {
        Text = texto,
        Font = Titulo,
        ForeColor = Tinta,
        AutoSize = true,
        Dock = DockStyle.Top,
        Padding = new Padding(0, 0, 0, 14)
    };

    public static Icon Icono() => new(Recurso("cerquilloapp.ico"));

    public static PictureBox Logo(int lado) => new()
    {
        Image = Image.FromStream(Recurso("cerquilloapp-logo-192.png")),
        SizeMode = PictureBoxSizeMode.Zoom,
        Size = new Size(lado, lado),
        Margin = Padding.Empty
    };

    private static Stream Recurso(string nombre) =>
        typeof(Ui).Assembly.GetManifestResourceStream($"BookingHub.WinForms.Recursos.{nombre}")
        ?? throw new InvalidOperationException($"No se encontró el recurso {nombre}.");

    public static Label Etiqueta(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        ForeColor = TextoSuave,
        Margin = new Padding(0, 8, 12, 4)
    };

    public static Button Boton(string texto, bool primario = false)
    {
        var boton = new BotonRedondo
        {
            Text = texto,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 34),
            Padding = new Padding(12, 2, 12, 2),
            Margin = new Padding(0, 4, 8, 4),
            FlatStyle = FlatStyle.Flat,
            BackColor = primario ? Tinta : VidrioBoton,
            ForeColor = primario ? Color.White : Tinta,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        boton.FlatAppearance.BorderColor = primario ? Tinta : Borde;
        boton.FlatAppearance.MouseOverBackColor = primario ? Color.FromArgb(55, 52, 45) : Seleccion;
        boton.FlatAppearance.MouseDownBackColor = primario ? Color.Black : Borde;
        return boton;
    }

    public static Button Enlace(string texto)
    {
        var boton = Boton(texto);
        boton.FlatAppearance.BorderSize = 0;
        boton.BackColor = Color.Transparent;
        boton.ForeColor = TextoSuave;
        return boton;
    }

    public static FlowLayoutPanel Barra(params Control[] controles)
    {
        var barra = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 8)
        };
        foreach (var control in controles)
        {
            control.Margin = new Padding(0, 4, 8, 4);
            if (control is Label etiqueta)
                etiqueta.Margin = new Padding(0, 9, 4, 4);
            barra.Controls.Add(control);
        }
        return barra;
    }

    public static TableLayoutPanel Columna(params Control[] controles)
    {
        var columna = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top
        };
        columna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        foreach (var control in controles)
        {
            control.Dock = DockStyle.Top;
            columna.Controls.Add(control);
        }
        return columna;
    }

    public static TableLayoutPanel Formulario(params (string Etiqueta, Control Control)[] campos)
    {
        var tabla = new TableLayoutPanel
        {
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 8)
        };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        foreach (var (texto, control) in campos)
        {
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 4, 0, 4);
            tabla.Controls.Add(Etiqueta(texto));
            tabla.Controls.Add(control);
        }
        return tabla;
    }

    public static DataGridView Grilla()
    {
        var grilla = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = GrillaFondo,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            GridColor = Borde,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 38,
            RowTemplate = { Height = 36 }
        };
        grilla.ColumnHeadersDefaultCellStyle.BackColor = GrillaFondo;
        grilla.ColumnHeadersDefaultCellStyle.ForeColor = TextoSuave;
        grilla.ColumnHeadersDefaultCellStyle.Font = Pequena;
        grilla.ColumnHeadersDefaultCellStyle.SelectionBackColor = GrillaFondo;
        grilla.DefaultCellStyle.BackColor = GrillaFondo;
        grilla.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grilla.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grilla.DefaultCellStyle.ForeColor = Tinta;
        grilla.DefaultCellStyle.SelectionBackColor = Seleccion;
        grilla.DefaultCellStyle.SelectionForeColor = Tinta;
        return grilla;
    }

    public static void Mostrar<T>(DataGridView grilla, IEnumerable<T> filas, params string[] columnasMoneda)
    {
        grilla.DataSource = filas.ToList();
        foreach (DataGridViewColumn columna in grilla.Columns)
        {
            columna.HeaderText = Encabezado(columna.Name);
            if (columna.Name == "Id")
                columna.Visible = false;
            if (columnasMoneda.Contains(columna.Name))
            {
                columna.DefaultCellStyle.Format = "C2";
                columna.DefaultCellStyle.FormatProvider = Moneda;
                columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }
    }

    /// <summary>
    /// Fila elegida. Primero SelectedRows: al seleccionar desde el código, el evento
    /// SelectionChanged llega antes de que CurrentRow cambie a la nueva fila.
    /// </summary>
    public static T? Seleccionado<T>(DataGridView grilla) where T : class =>
        (grilla.SelectedRows.Count > 0 ? grilla.SelectedRows[0] : grilla.CurrentRow)?.DataBoundItem as T;

    private static string Encabezado(string propiedad)
    {
        var texto = string.Concat(propiedad.Select((c, i) => i > 0 && char.IsUpper(c) && !char.IsUpper(propiedad[i - 1]) ? " " + char.ToLower(c) : c.ToString()));
        return texto
            .Replace("Telefono", "Teléfono")
            .Replace("Duracion", "Duración")
            .Replace("Metodo", "Método")
            .Replace("Descripcion", "Descripción")
            .Replace("Ultimo", "Último")
            .Replace("Cedula", "Cédula");
    }

    public static NumericUpDown Numero(decimal maximo, int decimales = 2, decimal minimo = 0) => new()
    {
        Maximum = maximo,
        Minimum = minimo,
        DecimalPlaces = decimales,
        ThousandsSeparator = true
    };

    public static ComboBox Lista() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    /// <summary>
    /// Envuelve un control en una tarjeta de vidrio que llena el espacio.
    /// Con <paramref name="llenar"/> en false el contenido queda arriba y la tarjeta se desplaza si no cabe (formularios).
    /// </summary>
    /// <summary>True si hay un diálogo modal abierto (nueva cita, cobro...): no conviene recargar debajo.</summary>
    public static bool HayDialogoAbierto => Application.OpenForms.Cast<Form>().Any(f => f.Modal);

    public static PanelVidrio EnVidrio(Control contenido, bool llenar = true)
    {
        var tarjeta = new PanelVidrio { Dock = DockStyle.Fill, AutoScroll = !llenar };
        contenido.Dock = llenar ? DockStyle.Fill : DockStyle.Top;
        tarjeta.Controls.Add(contenido);
        return tarjeta;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr ventana, int atributo, ref int valor, int tamano);

    /// <summary>
    /// Windows 11: barra de título con material Mica y esquinas redondeadas.
    /// En Windows 10 no hace nada.
    /// </summary>
    public static void EfectoVentana(Form formulario)
    {
        void Aplicar()
        {
            try
            {
                var mica = 2;      // DWMSBT_MAINWINDOW
                var redondo = 2;   // DWMWCP_ROUND
                DwmSetWindowAttribute(formulario.Handle, 38, ref mica, sizeof(int));     // DWMWA_SYSTEMBACKDROP_TYPE
                DwmSetWindowAttribute(formulario.Handle, 33, ref redondo, sizeof(int));  // DWMWA_WINDOW_CORNER_PREFERENCE
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
            }
        }

        if (formulario.IsHandleCreated)
            Aplicar();
        else
            formulario.HandleCreated += (_, _) => Aplicar();
    }
}

/// <summary>
/// Fondo cálido con tres manchas de color muy suaves, como en la web.
/// Se dibuja una vez en un mapa de bits y se reutiliza mientras no cambie el tamaño.
/// </summary>
public sealed class FondoVidrio : IDisposable
{
    private Bitmap? _cache;

    public void Pintar(Graphics g, Rectangle area)
    {
        if (area.Width <= 0 || area.Height <= 0)
            return;

        if (_cache is null || _cache.Size != area.Size)
        {
            _cache?.Dispose();
            _cache = Crear(area.Size);
        }
        g.DrawImageUnscaled(_cache, area.Location);
    }

    private static Bitmap Crear(Size tamano)
    {
        var mapa = new Bitmap(tamano.Width, tamano.Height);
        using var g = Graphics.FromImage(mapa);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Ui.Fondo);

        var lado = Math.Max(tamano.Width, tamano.Height);
        Mancha(g, new PointF(tamano.Width * 0.05f, -tamano.Height * 0.08f), lado * 0.55f, Color.FromArgb(105, 201, 161, 91));
        Mancha(g, new PointF(tamano.Width * 1.0f, tamano.Height * 0.35f), lado * 0.5f, Color.FromArgb(80, 140, 165, 155));
        Mancha(g, new PointF(tamano.Width * 0.4f, tamano.Height * 1.1f), lado * 0.45f, Color.FromArgb(50, 201, 161, 91));
        return mapa;
    }

    private static void Mancha(Graphics g, PointF centro, float radio, Color color)
    {
        using var camino = new GraphicsPath();
        camino.AddEllipse(centro.X - radio, centro.Y - radio, radio * 2, radio * 2);
        using var pincel = new PathGradientBrush(camino)
        {
            CenterPoint = centro,
            CenterColor = color,
            SurroundColors = [Color.FromArgb(0, color)]
        };
        g.FillPath(pincel, camino);
    }

    public void Dispose() => _cache?.Dispose();
}

/// <summary>Tarjeta de vidrio: blanco semitransparente, esquinas redondeadas y borde claro.</summary>
public class PanelVidrio : Panel
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Radio { get; set; } = 14;

    public PanelVidrio()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint, true);
        BackColor = Color.Transparent;
        Padding = new Padding(12);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var area = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var camino = Redondeado(area, Radio);
        using var relleno = new SolidBrush(Ui.Vidrio);
        using var borde = new Pen(Ui.VidrioBorde, 1f);
        e.Graphics.FillPath(relleno, camino);
        e.Graphics.DrawPath(borde, camino);
    }

    private static GraphicsPath Redondeado(RectangleF r, float radio)
    {
        var d = radio * 2;
        var camino = new GraphicsPath();
        camino.AddArc(r.X, r.Y, d, d, 180, 90);
        camino.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        camino.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        camino.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        camino.CloseFigure();
        return camino;
    }
}

/// <summary>
/// Botón con esquinas redondeadas y bordes suavizados. Usa los colores normales del botón:
/// BackColor de relleno, FlatAppearance.MouseOverBackColor / MouseDownBackColor al pasar o presionar
/// y FlatAppearance.BorderColor para el borde (BorderSize 0 = sin borde).
/// </summary>
public class BotonRedondo : Button
{
    private bool _encima;
    private bool _presionado;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Radio { get; set; } = 10;

    public BotonRedondo()
    {
        SetStyle(ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor, true);
        FlatStyle = FlatStyle.Flat;
    }

    protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _encima = _presionado = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _presionado = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _presionado = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        PintarFondoDelPadre(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var relleno = !Enabled ? Color.FromArgb(120, BackColor)
            : _presionado && !FlatAppearance.MouseDownBackColor.IsEmpty ? FlatAppearance.MouseDownBackColor
            : _encima && !FlatAppearance.MouseOverBackColor.IsEmpty ? FlatAppearance.MouseOverBackColor
            : BackColor;

        var area = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var camino = Redondeado(area, Math.Min(Radio, (int)(area.Height / 2)));
        if (relleno.A > 0)
        {
            using var pincel = new SolidBrush(relleno);
            g.FillPath(pincel, camino);
        }
        if (FlatAppearance.BorderSize > 0 && !FlatAppearance.BorderColor.IsEmpty)
        {
            using var lapiz = new Pen(FlatAppearance.BorderColor, 1f);
            g.DrawPath(lapiz, camino);
        }
        if (Focused && ShowFocusCues)
        {
            using var foco = new Pen(Color.FromArgb(160, Ui.Acento), 2f);
            g.DrawPath(foco, camino);
        }

        var texto = Enabled ? ForeColor : Color.FromArgb(150, ForeColor);
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, texto,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    // Las esquinas fuera del redondeo muestran lo que hay detrás (el vidrio o el fondo).
    private void PintarFondoDelPadre(PaintEventArgs e)
    {
        if (Parent is null)
            return;

        var estado = e.Graphics.Save();
        e.Graphics.TranslateTransform(-Left, -Top);
        using var pe = new PaintEventArgs(e.Graphics, new Rectangle(Left, Top, Width, Height));
        InvokePaintBackground(Parent, pe);
        InvokePaint(Parent, pe);
        e.Graphics.Restore(estado);
    }

    internal static GraphicsPath Redondeado(RectangleF r, float radio)
    {
        var camino = new GraphicsPath();
        if (radio <= 0)
        {
            camino.AddRectangle(r);
            return camino;
        }
        var d = radio * 2;
        camino.AddArc(r.X, r.Y, d, d, 180, 90);
        camino.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        camino.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        camino.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        camino.CloseFigure();
        return camino;
    }
}
