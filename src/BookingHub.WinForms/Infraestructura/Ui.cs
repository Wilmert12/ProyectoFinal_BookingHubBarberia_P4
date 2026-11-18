using System.Globalization;

namespace BookingHub.WinForms.Infraestructura;

public static class Ui
{
    public static readonly Color Marino = Color.FromArgb(33, 37, 41);
    public static readonly Color Primario = Color.FromArgb(13, 110, 253);
    public static readonly Color Fondo = Color.FromArgb(248, 249, 250);
    public static readonly Font Fuente = new("Segoe UI", 10f);
    public static readonly Font Titulo = new("Segoe UI Semibold", 15f);
    public static readonly Font Destacado = new("Segoe UI Semibold", 11f);

    private static readonly CultureInfo Moneda = CultureInfo.GetCultureInfo("en-US");

    public static Label TituloModulo(string texto) => new()
    {
        Text = texto,
        Font = Titulo,
        AutoSize = true,
        Dock = DockStyle.Top,
        Padding = new Padding(0, 0, 0, 10)
    };

    public static Label Etiqueta(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 8, 10, 4)
    };

    public static Button Boton(string texto, bool primario = false)
    {
        var boton = new Button
        {
            Text = texto,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(0, 4, 8, 4),
            FlatStyle = FlatStyle.Flat,
            BackColor = primario ? Primario : Color.White,
            ForeColor = primario ? Color.White : Color.Black,
            Cursor = Cursors.Hand
        };
        boton.FlatAppearance.BorderColor = primario ? Primario : Color.FromArgb(206, 212, 218);
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
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 34,
            RowTemplate = { Height = 30 }
        };
        grilla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(233, 236, 239);
        grilla.ColumnHeadersDefaultCellStyle.Font = new Font(Fuente, FontStyle.Bold);
        grilla.DefaultCellStyle.SelectionBackColor = Color.FromArgb(207, 226, 255);
        grilla.DefaultCellStyle.SelectionForeColor = Color.Black;
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
