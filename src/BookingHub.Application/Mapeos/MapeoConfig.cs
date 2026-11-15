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
            .Map(dto => dto.Estado, cita => cita.EstadoCita.Descripcion);

        config.NewConfig<Barbero, BarberoDto>()
            .Map(dto => dto.Nombre, barbero => barbero.Usuario.Nombre)
            .Ignore(dto => dto.Cedula);

        config.NewConfig<BarberoDto, Barbero>()
            .Map(barbero => barbero.Cedula, dto => dto.Cedula ?? string.Empty);

        config.Compile();
        return config;
    }
}
