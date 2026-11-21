using BookingHub.Application.DTOs;
using BookingHub.Infrastructure.Models;
using Mapster;

namespace BookingHub.Application.Mapeos;

public static class MapeoConfig
{
    public static TypeAdapterConfig Config { get; } = Crear();

    private static TypeAdapterConfig Crear()
    {
        var config = new TypeAdapterConfig();

        config.NewConfig<Cita, CitaDto>()
            .Map(dto => dto.ClienteNombre, cita => cita.Cliente.Nombre)
            .Map(dto => dto.BarberoNombre, cita => cita.Barbero.Usuario.Nombre)
            .Map(dto => dto.Estado, cita => cita.EstadoCita.Descripcion)
            .Map(dto => dto.Detalles, cita => cita.CitaDetalles)
            .Map(dto => dto.Pagado, cita => cita.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m);

        config.NewConfig<CitaDetalle, CitaDetalleDto>()
            .Map(dto => dto.Nombre, detalle => detalle.CatalogoItem.Nombre)
            .Map(dto => dto.Subtotal, detalle => detalle.Cantidad * detalle.PrecioUnitario - detalle.Descuento);

        config.NewConfig<CitaDetalleDto, CitaDetalle>()
            .Ignore(detalle => detalle.CitaDetalleId)
            .Ignore(detalle => detalle.CatalogoItem)
            .Ignore(detalle => detalle.Cita);

        config.NewConfig<Pago, PagoDto>()
            .Map(dto => dto.ClienteNombre, pago => pago.Cita.Cliente.Nombre)
            .Map(dto => dto.BarberoNombre, pago => pago.Cita.Barbero.Usuario.Nombre)
            .Map(dto => dto.MetodoPago, pago => pago.MetodoPago.Descripcion);

        config.NewConfig<PagoDto, Pago>()
            .Ignore(pago => pago.MetodoPago)
            .Ignore(pago => pago.Cita);

        config.NewConfig<Barbero, BarberoDto>()
            .Map(dto => dto.Nombre, barbero => barbero.Usuario.Nombre)
            .Ignore(dto => dto.Cedula);

        config.NewConfig<BarberoDto, Barbero>()
            .Map(barbero => barbero.Cedula, dto => dto.Cedula ?? string.Empty);

        config.Compile();
        return config;
    }
}
