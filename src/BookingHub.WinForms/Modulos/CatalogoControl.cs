using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;
using Refit;
using System.Net;

namespace BookingHub.WinForms.Modulos;

public class CatalogoControl : UserControl
{
    private readonly Apis _apis;
    private readonly DataGridView _grilla = Ui.Grilla();
    private readonly TextBox _nombre = new();
    private readonly TextBox _descripcion = new() { Multiline = true, Height = 60 };
    private readonly ComboBox _tipo = Ui.Lista();
    private readonly NumericUpDown _precio = Ui.Numero(100000);
    private readonly NumericUpDown _duracion = Ui.Numero(600, 0);
    private readonly CheckBox _activo = new() { Text = "Activo", AutoSize = true, Checked = true };
    private readonly Label _modo = new() { AutoSize = true, Font = Ui.Destacado, Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 6) };
    private List<CatalogoItemDto> _items = [];
    private int _itemId;

    private record FilaItem(int Id, string Nombre, string Tipo, decimal Precio, string Duracion, bool Activo);

    public CatalogoControl(Apis apis)
    {
        _apis = apis;
        _tipo.Items.AddRange(["Servicio", "Producto"]);
        _tipo.SelectedIndex = 0;

        var nuevo = Ui.Boton("Nuevo");
        var guardar = Ui.Boton("Guardar", primario: true);
        var eliminar = Ui.Boton("Eliminar");

        var formulario = Ui.Formulario(
            ("Nombre", _nombre),
            ("Descripción", _descripcion),
            ("Tipo", _tipo),
            ("Precio", _precio),
            ("Duración (min)", _duracion),
            ("", _activo));

        var panelFormulario = new Panel { Dock = DockStyle.Right, Width = 380, Padding = new Padding(16, 0, 0, 0) };
        panelFormulario.Controls.Add(Ui.Columna(_modo, formulario, Ui.Barra(nuevo, guardar, eliminar)));

        Controls.Add(_grilla);
        Controls.Add(panelFormulario);
        Controls.Add(Ui.TituloModulo("Catálogo de servicios y productos"));

        Load += async (_, _) => await Cargar();
        _grilla.SelectionChanged += (_, _) => Editar();
        _tipo.SelectedIndexChanged += (_, _) => _duracion.Enabled = _tipo.SelectedIndex == 0;
        nuevo.Click += (_, _) => Limpiar();
        guardar.Click += async (_, _) => await Guardar();
        eliminar.Click += async (_, _) => await Eliminar();
        Limpiar();
    }

    private async Task Cargar(int? seleccionar = null)
    {
        try
        {
            _items = await _apis.Catalogo.GetCatalogoItems(incluirInactivos: true);
            Ui.Mostrar(_grilla, _items.Select(i => new FilaItem(
                i.CatalogoItemId,
                i.Nombre,
                i.EsServicio ? "Servicio" : "Producto",
                i.Precio,
                i.DuracionMin is { } minutos ? $"{minutos} min" : "-",
                i.Activo)),
                nameof(FilaItem.Precio));

            if (seleccionar is { } id)
                foreach (DataGridViewRow fila in _grilla.Rows)
                    if (fila.DataBoundItem is FilaItem item && item.Id == id)
                        _grilla.CurrentCell = fila.Cells[nameof(FilaItem.Nombre)];
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private void Editar()
    {
        if (Ui.Seleccionado<FilaItem>(_grilla) is not { } fila || _items.FirstOrDefault(i => i.CatalogoItemId == fila.Id) is not { } item)
            return;

        _itemId = item.CatalogoItemId;
        _modo.Text = $"Editando: {item.Nombre}";
        _nombre.Text = item.Nombre;
        _descripcion.Text = item.Descripcion;
        _tipo.SelectedIndex = item.EsServicio ? 0 : 1;
        _precio.Value = Math.Min(item.Precio, _precio.Maximum);
        _duracion.Value = item.DuracionMin ?? 0;
        _activo.Checked = item.Activo;
    }

    private void Limpiar()
    {
        _itemId = 0;
        _modo.Text = "Nuevo ítem";
        _nombre.Text = string.Empty;
        _descripcion.Text = string.Empty;
        _tipo.SelectedIndex = 0;
        _precio.Value = 0;
        _duracion.Value = 30;
        _activo.Checked = true;
        _grilla.ClearSelection();
        _nombre.Focus();
    }

    private async Task Guardar()
    {
        var esServicio = _tipo.SelectedIndex == 0;
        if (string.IsNullOrWhiteSpace(_nombre.Text))
        {
            Mensajes.Informacion(this, "El nombre es obligatorio.");
            return;
        }
        if (esServicio && _duracion.Value <= 0)
        {
            Mensajes.Informacion(this, "Un servicio necesita una duración mayor que cero.");
            return;
        }

        var item = new CatalogoItemDto
        {
            CatalogoItemId = _itemId,
            Nombre = _nombre.Text.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(_descripcion.Text) ? null : _descripcion.Text.Trim(),
            EsServicio = esServicio,
            Precio = _precio.Value,
            DuracionMin = esServicio ? (int)_duracion.Value : null,
            Activo = _activo.Checked
        };

        try
        {
            if (_itemId == 0)
                item = await _apis.Catalogo.CrearItem(item);
            else
                await _apis.Catalogo.ModificarItem(_itemId, item);

            await Cargar(item.CatalogoItemId);
            Mensajes.Informacion(this, "Ítem guardado.");
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }

    private async Task Eliminar()
    {
        if (_itemId == 0)
            return;
        if (!Mensajes.Confirmar(this, $"¿Eliminar \"{_nombre.Text}\"? Si ya se usó en alguna cita no se podrá: en ese caso desactívalo."))
            return;

        try
        {
            await _apis.Catalogo.EliminarItem(_itemId);
            Limpiar();
            await Cargar();
        }
        catch (ApiException api) when (api.StatusCode == HttpStatusCode.InternalServerError)
        {
            Mensajes.Informacion(this, "No se puede eliminar porque ya se usó en citas. Desmarca \"Activo\" y guarda para ocultarlo.");
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
    }
}
