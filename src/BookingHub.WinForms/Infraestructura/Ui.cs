using System.Globalization;

namespace BookingHub.WinForms.Infraestructura;

public static class Ui
{
    public const string Marca = "CerquilloApp";

    public static readonly Color Tinta = Color.FromArgb(23, 21, 15);
    public static readonly Color Acento = Color.FromArgb(201, 161, 91);
    public static readonly Color Fondo = Color.FromArgb(250, 248, 243);
    public static readonly Color Superficie = Color.White;
    public static readonly Color Borde = Color.FromArgb(230, 227, 220);
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
        var boton = new Button
        {
            Text = texto,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 34),
            Padding = new Padding(12, 2, 12, 2),
            Margin = new Padding(0, 4, 8, 4),
            FlatStyle = FlatStyle.Flat,
            BackColor = primario ? Tinta : Superficie,
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
            BackgroundColor = Superficie,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            GridColor = Borde,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 38,
            RowTemplate = { Height = 36 }
        };
        grilla.ColumnHeadersDefaultCellStyle.BackColor = Superficie;
        grilla.ColumnHeadersDefaultCellStyle.ForeColor = TextoSuave;
        grilla.ColumnHeadersDefaultCellStyle.Font = Pequena;
        grilla.ColumnHeadersDefaultCellStyle.SelectionBackColor = Superficie;
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

    public static T? Seleccionado<T>(DataGridView grilla) where T : class =>
        grilla.CurrentRow?.DataBoundItem as T;

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
}
