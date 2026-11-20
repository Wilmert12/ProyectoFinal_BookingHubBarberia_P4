using BookingHub.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/InformacionBarberia")]
public sealed class InformacionBarberiaController(IOptions<InformacionBarberiaOptions> configuracion) : ControllerBase
{
    [HttpGet]
    public ActionResult<InformacionBarberiaDto> Get()
    {
        var info = configuracion.Value;
        return Ok(new InformacionBarberiaDto(info.Nombre, info.Direccion, info.Telefono,
            info.WhatsApp, info.Horario, info.UrlMapa));
    }
}
