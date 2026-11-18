using BookingHub.WinForms.Infraestructura;
using BookingHub.WinForms.Models;

namespace BookingHub.WinForms.Formularios;

public class FrmCobro : Form
{
    private readonly Apis _apis;
    private readonly CitaDto _cita;
    private readonly List<CitaDetalleDto> _detalles;
    private readonly DataGridView _grilla = Ui.Grilla();
    private readonly ComboBox _item = Ui.Lista();
    private readonly NumericUpDown _cantidad = Ui.Numero(99, 0, 1);
    private readonly NumericUpDown _descuento = Ui.Numero(100000);
    private readonly ComboBox _metodo = Ui.Lista();
    private readonly NumericUpDown _propina = Ui.Numero(100000);
    private readonly TextBox _referencia = new() { Width = 200 };
    private readonly Label _total = new() { AutoSize = true, Font = Ui.Titulo };
    private readonly Button _cobrar = Ui.Boton("Cobrar y emitir recibo", primario: true);
    private List<CatalogoItemDto> _catalogo = [];
    private int _estadoCompletadaId;

    private record FilaDetalle(int Indice, string Item, int Cantidad, decimal Precio, decimal Descuento, decimal Subtotal);

    public FrmCobro(Apis apis, CitaDto cita)
    {
        _apis = apis;
        _cita = cita;
        _detalles = (cita.Detalles ?? []).Select(d => d with { }).ToList();

        Text = "Cobrar cita";
        Font = Ui.Fuente;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(820, 640);
        MinimumSize = new Size(700, 560);
        BackColor = Color.White;

        var encabezado = new Label
        {
            Text = $"{cita.ClienteNombre}  ·  {cita.BarberoNombre}  ·  {cita.FechaInicio:dd/MM/yyyy HH:mm}",
            Font = Ui.Destacado,
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 10)
        };

        _item.Width = 260;
        _cantidad.Width = 60;
        _cantidad.Value = 1;
        _descuento.Width = 90;
        var agregar = Ui.Boton("Agregar");
        var quitar = Ui.Boton("Quitar seleccionado");
        var barraItems = Ui.Barra(Ui.Etiqueta("Servicio o producto"), _item, Ui.Etiqueta("Cant."), _cantidad, Ui.Etiqueta("Descuento"), _descuento, agregar, quitar);

        _metodo.Width = 160;
        _propina.Width = 90;
        var cancelar = Ui.Boton("Cancelar");
        cancelar.DialogResult = DialogResult.Cancel;
        CancelButton = cancelar;
        var barraPago = Ui.Barra(Ui.Etiqueta("Método de pago"), _metodo, Ui.Etiqueta("Propina"), _propina, Ui.Etiqueta("Referencia"), _referencia);
        barraPago.Dock = DockStyle.Bottom;
        var barraTotal = Ui.Barra(_total, _cobrar, cancelar);
        barraTotal.Dock = DockStyle.Bottom;

        var contenedor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        contenedor.Controls.Add(_grilla);
        contenedor.Controls.Add(barraItems);
        contenedor.Controls.Add(encabezado);
        contenedor.Controls.Add(barraPago);
        contenedor.Controls.Add(barraTotal);
        Controls.Add(contenedor);

        Load += async (_, _) => await Cargar();
        agregar.Click += (_, _) => Agregar();
        quitar.Click += (_, _) => Quitar();
        _cobrar.Click += async (_, _) => await Cobrar();
    }

    private async Task Cargar()
    {
        try
        {
            _catalogo = await _apis.Catalogo.GetCatalogoItems();
            var metodos = await _apis.Catalogos.GetMetodosPago();
            var estados = await _apis.Catalogos.GetEstadosCita();
            _estadoCompletadaId = estados.FirstOrDefault(e => e.Descripcion == "Completada")?.EstadoCitaId ?? 0;

            _item.DisplayMember = nameof(CatalogoItemDto.Nombre);
            _item.ValueMember = nameof(CatalogoItemDto.CatalogoItemId);
            _item.DataSource = _catalogo;
            _item.Format += (_, e) =>
            {
                if (e.ListItem is CatalogoItemDto item)
                    e.Value = $"{item.Nombre} · {Mensajes.Precio(item.Precio)}";
            };

            _metodo.DisplayMember = nameof(MetodoPagoDto.Descripcion);
            _metodo.ValueMember = nameof(MetodoPagoDto.MetodoPagoId);
            _metodo.DataSource = metodos;

            Refrescar();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
            Close();
        }
    }

    private decimal Total => _detalles.Sum(d => d.Cantidad * d.PrecioUnitario - d.Descuento);

    private void Refrescar()
    {
        Ui.Mostrar(_grilla, _detalles.Select((d, i) => new FilaDetalle(
            i,
            d.Nombre ?? _catalogo.FirstOrDefault(c => c.CatalogoItemId == d.CatalogoItemId)?.Nombre ?? "",
            d.Cantidad,
            d.PrecioUnitario,
            d.Descuento,
            d.Cantidad * d.PrecioUnitario - d.Descuento)),
            nameof(FilaDetalle.Precio), nameof(FilaDetalle.Descuento), nameof(FilaDetalle.Subtotal));
        _grilla.Columns[nameof(FilaDetalle.Indice)]!.Visible = false;
        _total.Text = $"Total: {Mensajes.Precio(Total)}";
    }

    private void Agregar()
    {
        if (_item.SelectedItem is not CatalogoItemDto item)
            return;

        var cantidad = (int)_cantidad.Value;
        if (_descuento.Value > item.Precio * cantidad)
        {
            Mensajes.Informacion(this, "El descuento no puede ser mayor que el subtotal del ítem.");
            return;
        }

        _detalles.Add(new CitaDetalleDto
        {
            CatalogoItemId = item.CatalogoItemId,
            Nombre = item.Nombre,
            Cantidad = cantidad,
            PrecioUnitario = item.Precio,
            Descuento = _descuento.Value
        });
        _cantidad.Value = 1;
        _descuento.Value = 0;
        Refrescar();
    }

    private void Quitar()
    {
        if (Ui.Seleccionado<FilaDetalle>(_grilla) is not { } fila)
            return;

        _detalles.RemoveAt(fila.Indice);
        Refrescar();
    }

    private async Task Cobrar()
    {
        if (_detalles.Count == 0 || Total <= 0)
        {
            Mensajes.Informacion(this, "Agrega al menos un servicio o producto con un total mayor que cero.");
            return;
        }
        if (_metodo.SelectedItem is not MetodoPagoDto metodo)
        {
            Mensajes.Informacion(this, "Elige el método de pago.");
            return;
        }
        if (_estadoCompletadaId == 0)
        {
            Mensajes.Informacion(this, "No existe el estado Completada en la base de datos.");
            return;
        }

        _cobrar.Enabled = false;
        try
        {
            var cita = _cita with { EstadoCitaId = _estadoCompletadaId, Detalles = _detalles };
            await _apis.Citas.ModificarCita(cita.CitaId, cita);

            var pago = await _apis.Pagos.CrearPago(new PagoDto
            {
                CitaId = cita.CitaId,
                MetodoPagoId = metodo.MetodoPagoId,
                Monto = Total,
                Propina = _propina.Value,
                Referencia = string.IsNullOrWhiteSpace(_referencia.Text) ? null : _referencia.Text.Trim()
            });

            using var recibo = new FrmRecibo("Recibo", Recibos.DeCobro(cita, _detalles, pago));
            recibo.ShowDialog(this);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Mensajes.Error(this, ex);
        }
        finally
        {
            _cobrar.Enabled = true;
        }
    }
}
