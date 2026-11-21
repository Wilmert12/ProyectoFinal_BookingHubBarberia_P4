using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;
using System.Globalization;

namespace BookingHub.WinForms.Modulos;

/// <summary>Horario semanal de cada barbero y días libres (vacaciones, feriados, cierres).</summary>
public class HorariosControl : UserControl
{
    private static readonly string[] Dias = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
    private static readonly TimeOnly EntradaPorDefecto = new(9, 0);
    private static readonly TimeOnly SalidaPorDefecto = new(18, 0);

    private readonly Apis _apis;

    // Horario semanal
    private readonly ComboBox _barbero = Ui.Lista();
    private readonly DataGridView _semana = Ui.Grilla();
    private readonly Label _avisoSemana = new() { AutoSize = true, ForeColor = Ui.TextoSuave, MaximumSize = new Size(460, 0) };

    // Días libres
    private readonly DataGridView _libres = Ui.Grilla();
    private readonly ComboBox _libreBarbero = Ui.Lista();
    private readonly ComboBox _libreTipo = Ui.Lista();
    private readonly DateTimePicker _desde = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };
    private readonly DateTimePicker _hasta = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };
    private readonly CheckBox _diaCompleto = new() { Text = "Días completos", AutoSize = true, Checked = true };
    private readonly TextBox _descripcion = new() { MaxLength = 500 };

    private sealed record OpcionBarbero(int? BarberoId, string Nombre);
    private sealed record FilaLibre(int Id, string Barbero, string Tipo, string Desde, string Hasta, string Descripcion);

    public HorariosControl(Apis apis)
    {
        _apis = apis;

        ConfigurarSemana();

        var guardarSemana = Ui.Boton("Guardar horario", primario: true);
        var porDefecto = Ui.Boton("Usar horario por defecto");
        var panelSemana = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 16, 0) };
        panelSemana.Controls.Add(Ui.EnVidrio(_semana));
        panelSemana.Controls.Add(Ui.Columna(
            Ui.Barra(Ui.Etiqueta("Barbero"), _barbero),
            _avisoSemana));
        panelSemana.Controls.Add(Ui.Barra(guardarSemana, porDefecto));
        panelSemana.Controls[^1].Dock = DockStyle.Bottom;
        panelSemana.Controls.Add(Subtitulo("Horario semanal"));
        _barbero.Width = 260;

        var agregar = Ui.Boton("Agregar día libre", primario: true);
        var eliminar = Ui.Boton("Eliminar seleccionado");
        var formularioLibre = Ui.Formulario(
            ("Barbero", _libreBarbero),
            ("Tipo", _libreTipo),
            ("", _diaCompleto),
            ("Desde", _desde),
            ("Hasta", _hasta),
            ("Motivo", _descripcion));
        var panelLibres = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 0, 0) };
        panelLibres.Controls.Add(Ui.EnVidrio(_libres));
        panelLibres.Controls.Add(Ui.Columna(formularioLibre, Ui.Barra(agregar, eliminar)));
        panelLibres.Controls.Add(Subtitulo("Días libres y cierres"));

        var columnas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        columnas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        columnas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        columnas.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        columnas.Controls.Add(panelSemana, 0, 0);
        columnas.Controls.Add(panelLibres, 1, 0);

        Controls.Add(columnas);
        Controls.Add(Ui.TituloModulo("Horarios y días libres"));

        _desde.Value = DateTime.Today.AddDays(1);
        _hasta.Value = DateTime.Today.AddDays(1);
        AjustarFormatoFechas();

        Load += async (_, _) => await Inicializar();
        VisibleChanged += async (_, _) =>
        {
            if (Visible && _barbero.Items.Count > 0)
                await CargarLibres();
        };
        _barbero.SelectedIndexChanged += async (_, _) => await CargarSemana();
        guardarSemana.Click += async (_, _) => await GuardarSemana();
        porDefecto.Click += (_, _) => LlenarSemana([]);
        _diaCompleto.CheckedChanged += (_, _) => AjustarFormatoFechas();
        agregar.Click += async (_, _) => await AgregarLibre();
        eliminar.Click += async (_, _) => await EliminarLibre();
    }

    private static Label Subtitulo(string texto) => new()
    {
        Text = texto,
        Font = Ui.Destacado,
        ForeColor = Ui.Tinta,
        AutoSize = true,
        Dock = DockStyle.Top,
        Padding = new Padding(0, 0, 0, 8)
    };

    private void ConfigurarSemana()
    {
        _semana.ReadOnly = false;
        _semana.AutoGenerateColumns = false;
        _semana.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _semana.EditMode = DataGridViewEditMode.EditOnEnter;
        _semana.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dia", HeaderText = "Día", ReadOnly = true });
        _semana.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Trabaja", HeaderText = "Trabaja", FillWeight = 60 });
        _semana.Columns.Add(new DataGridViewTextBoxColumn { Name = "Entrada", HeaderText = "Entrada (HH:mm)" });
        _semana.Columns.Add(new DataGridViewTextBoxColumn { Name = "Salida", HeaderText = "Salida (HH:mm)" });

        // Lunes primero, domingo al final, como se lee una semana de trabajo.
        foreach (var dia in new[] { 1, 2, 3, 4, 5, 6, 0 })
        {
            var fila = _semana.Rows[_semana.Rows.Add(Dias[dia], false, "", "")];
            fila.Tag = (byte)dia;
        }

        _semana.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_semana.CurrentCell is DataGridViewCheckBoxCell)
                _semana.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _semana.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex < 0 || _semana.Columns[e.ColumnIndex].Name != "Trabaja")
                return;
            var fila = _semana.Rows[e.RowIndex];
            if (fila.Cells["Trabaja"].Value is true && string.IsNullOrWhiteSpace(fila.Cells["Entrada"].Value as string))
            {
                fila.Cells["Entrada"].Value = EntradaPorDefecto.ToString("HH:mm");
                fila.Cells["Salida"].Value = SalidaPorDefecto.ToString("HH:mm");
            }
        };
    }

    private async Task Inicializar()
    {
        try
        {
            var barberos = await _apis.Barberos.GetBarberos();
            var tipos = await _apis.Indisponibilidades.GetTipos();

            _barbero.DisplayMember = nameof(BarberoDto.Nombre);
            _barbero.ValueMember = nameof(BarberoDto.BarberoId);
            _barbero.DataSource = barberos;

            _libreBarbero.DisplayMember = nameof(OpcionBarbero.Nombre);
            _libreBarbero.DataSource = new List<OpcionBarbero> { new(null, "Toda la barbería") }
                .Concat(barberos.Select(b => new OpcionBarbero(b.BarberoId, b.Nombre ?? string.Empty)))
                .ToList();

            _libreTipo.DisplayMember = nameof(TipoIndisponibilidadDto.Descripcion);
            _libreTipo.ValueMember = nameof(TipoIndisponibilidadDto.TipoIndisponibilidadId);
            _libreTipo.DataSource = tipos;

            await CargarLibres();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private async Task CargarSemana()
    {
        if (_barbero.SelectedValue is not int barberoId)
            return;

        try
        {
            LlenarSemana(await _apis.Barberos.GetHorarios(barberoId));
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private void LlenarSemana(List<HorarioDto> horarios)
    {
        var porDefecto = horarios.Count == 0;
        foreach (DataGridViewRow fila in _semana.Rows)
        {
            var dia = (byte)fila.Tag!;
            var bloques = horarios.Where(h => h.DiaSemana == dia).ToList();
            var trabaja = porDefecto ? dia != 0 : bloques.Count > 0;
            var entrada = porDefecto ? EntradaPorDefecto : bloques.Select(b => b.HoraInicio).DefaultIfEmpty(EntradaPorDefecto).Min();
            var salida = porDefecto ? SalidaPorDefecto : bloques.Select(b => b.HoraFin).DefaultIfEmpty(SalidaPorDefecto).Max();

            fila.Cells["Trabaja"].Value = trabaja;
            fila.Cells["Entrada"].Value = trabaja ? entrada.ToString("HH:mm") : "";
            fila.Cells["Salida"].Value = trabaja ? salida.ToString("HH:mm") : "";
        }

        if (porDefecto)
            _avisoSemana.Text = "Sin horario propio: usa el de la barbería (lunes a sábado, 9:00 a 18:00). Cámbialo y pulsa Guardar.";
        else if (horarios.GroupBy(h => h.DiaSemana).Any(g => g.Count() > 1))
            _avisoSemana.Text = "Algunos días tenían varios bloques; aquí se muestran de la primera entrada a la última salida.";
        else
            _avisoSemana.Text = "Marca los días que trabaja y escribe las horas como 09:00.";
    }

    private async Task GuardarSemana()
    {
        if (_barbero.SelectedValue is not int barberoId)
            return;

        _semana.EndEdit();
        var horarios = new List<HorarioDto>();
        foreach (DataGridViewRow fila in _semana.Rows)
        {
            if (fila.Cells["Trabaja"].Value is not true)
                continue;

            var dia = (byte)fila.Tag!;
            if (!TimeOnly.TryParse(fila.Cells["Entrada"].Value as string, CultureInfo.InvariantCulture, out var entrada)
                || !TimeOnly.TryParse(fila.Cells["Salida"].Value as string, CultureInfo.InvariantCulture, out var salida))
            {
                Mensajes.Informacion(this, $"Revisa las horas del {Dias[dia].ToLower()}: escríbelas como 09:00.");
                return;
            }
            if (salida <= entrada)
            {
                Mensajes.Informacion(this, $"El {Dias[dia].ToLower()} la salida debe ser después de la entrada.");
                return;
            }
            horarios.Add(new HorarioDto(dia, entrada, salida));
        }

        if (horarios.Count == 0 && !Mensajes.Confirmar(this, "No marcaste ningún día. El barbero quedará con el horario por defecto de la barbería. ¿Continuar?"))
            return;

        try
        {
            await _apis.Barberos.GuardarHorarios(barberoId, horarios);
            await CargarSemana();
            Mensajes.Informacion(this, "Horario guardado.");
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private void AjustarFormatoFechas()
    {
        var formato = _diaCompleto.Checked ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm";
        _desde.CustomFormat = _hasta.CustomFormat = formato;
    }

    private async Task CargarLibres()
    {
        try
        {
            var indisponibilidades = await _apis.Indisponibilidades.GetIndisponibilidades();
            Ui.Mostrar(_libres, indisponibilidades.Select(i => new FilaLibre(
                i.IndisponibilidadId,
                i.BarberoNombre ?? "Toda la barbería",
                i.Tipo ?? "",
                i.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                i.FechaFin.ToString("dd/MM/yyyy HH:mm"),
                i.Descripcion ?? "")));
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private async Task AgregarLibre()
    {
        if (_libreBarbero.SelectedItem is not OpcionBarbero barbero || _libreTipo.SelectedValue is not int tipoId)
        {
            Mensajes.Informacion(this, "Elige el barbero (o toda la barbería) y el tipo.");
            return;
        }

        // Con días completos, "hasta" incluye ese día completo.
        var desde = _diaCompleto.Checked ? _desde.Value.Date : Redondear(_desde.Value);
        var hasta = _diaCompleto.Checked ? _hasta.Value.Date.AddDays(1) : Redondear(_hasta.Value);
        if (hasta <= desde)
        {
            Mensajes.Informacion(this, "La fecha final debe ser igual o posterior a la inicial.");
            return;
        }

        try
        {
            await _apis.Indisponibilidades.Crear(new IndisponibilidadDto
            {
                BarberoId = barbero.BarberoId,
                TipoIndisponibilidadId = tipoId,
                Descripcion = string.IsNullOrWhiteSpace(_descripcion.Text) ? null : _descripcion.Text.Trim(),
                FechaInicio = desde,
                FechaFin = hasta
            });
            _descripcion.Text = string.Empty;
            await CargarLibres();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private async Task EliminarLibre()
    {
        if (Ui.Seleccionado<FilaLibre>(_libres) is not { } fila)
        {
            Mensajes.Informacion(this, "Selecciona un día libre de la lista.");
            return;
        }
        if (!Mensajes.Confirmar(this, $"¿Eliminar el día libre de {fila.Barbero} ({fila.Desde} a {fila.Hasta})?"))
            return;

        try
        {
            await _apis.Indisponibilidades.Eliminar(fila.Id);
            await CargarLibres();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private static DateTime Redondear(DateTime fecha) => fecha.AddSeconds(-fecha.Second).AddMilliseconds(-fecha.Millisecond);
}
