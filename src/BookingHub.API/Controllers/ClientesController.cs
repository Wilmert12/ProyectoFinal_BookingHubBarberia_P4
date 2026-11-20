using BookingHub.API.Seguridad;
using BookingHub.Application.DTOs;
using BookingHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController(
    ClienteService clienteService,
    ServicioAutenticacionCliente autenticacionCliente,
    ExperienciaClienteService experienciaClienteService,
    FotoPerfilAzureService fotoPerfilAzureService) : ControllerBase
{
    [Authorize(Roles = RolesSistema.Personal)]
    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> GetClientes(bool incluirInactivos = false)
    {
        var result = await clienteService.GetList(c => incluirInactivos || c.Activo);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Personal)]
    [HttpGet("{id}")]
    public async Task<ActionResult<ClienteDto>> GetCliente(int id)
    {
        var result = await clienteService.Buscar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value == null)
            return NotFound();

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> PostCliente(ClienteDto cliente)
    {
        cliente.ClienteId = 0;

        var result = await clienteService.Guardar(cliente);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetCliente), new { id = cliente.ClienteId }, cliente);
    }

    [Authorize(Roles = RolesSistema.Personal)]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCliente(int id, ClienteDto cliente)
    {
        if (id != cliente.ClienteId)
            return BadRequest("El id de la URL no coincide con el del registro.");

        var existe = await clienteService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await clienteService.Guardar(cliente);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    [Authorize(Roles = RolesSistema.Personal)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCliente(int id)
    {
        var existe = await clienteService.Buscar(id);
        if (existe.Value == null)
            return NotFound();

        var result = await clienteService.Eliminar(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpGet("me")]
    public async Task<ActionResult<ClienteDto>> ObtenerMiPerfil()
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        var result = await experienciaClienteService.Perfil(clienteId);
        return result.IsFailure ? BadRequest(result.Error) : result.Value is null ? NotFound() : Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpPut("me")]
    public async Task<ActionResult<ClienteDto>> ActualizarMiPerfil(ActualizarPerfilClienteRequest request)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        var result = await experienciaClienteService.ActualizarPerfil(clienteId, request);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpPost("me/foto")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(1_100_000)]
    public async Task<IActionResult> GuardarFotoPerfil(IFormFile foto, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        if (foto is null || foto.Length is < 4 or > 1_000_000) return BadRequest("La imagen debe pesar hasta 1 MB.");
        await using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria, cancellationToken);
        var contenido = memoria.ToArray();
        if (!FotoPerfilAzureService.EsImagenPermitida(contenido)) return BadRequest("La imagen debe estar en formato JPEG o PNG.");
        if (fotoPerfilAzureService.Configurado())
        {
            try
            {
                await fotoPerfilAzureService.Guardar(clienteId, contenido, cancellationToken);
                return NoContent();
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, "No se pudo guardar la foto en Azure Blob Storage.");
            }
        }

        var result = await experienciaClienteService.GuardarFoto(clienteId, contenido);
        return result.IsFailure ? BadRequest(result.Error) : NoContent();
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpGet("me/foto")]
    public async Task<IActionResult> ObtenerFotoPerfil(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        if (fotoPerfilAzureService.Configurado())
        {
            try
            {
                var azureFoto = await fotoPerfilAzureService.Obtener(clienteId, cancellationToken);
                if (azureFoto is not null) return File(azureFoto, FotoPerfilAzureService.TipoContenido(azureFoto));
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, "No se pudo leer la foto desde Azure Blob Storage.");
            }
        }
        var foto = await experienciaClienteService.ObtenerFoto(clienteId);
        return foto is null ? NotFound() : File(foto, FotoPerfilAzureService.TipoContenido(foto));
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpGet("me/favoritos")]
    public async Task<ActionResult<List<BarberoDto>>> ObtenerFavoritos()
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        var result = await experienciaClienteService.Favoritos(clienteId);
        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpPost("me/favoritos/{barberoId:int}")]
    public async Task<IActionResult> AgregarFavorito(int barberoId)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        var result = await experienciaClienteService.AgregarFavorito(clienteId, barberoId);
        return result.IsFailure ? BadRequest(result.Error) : NoContent();
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpDelete("me/favoritos/{barberoId:int}")]
    public async Task<IActionResult> QuitarFavorito(int barberoId)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        var result = await experienciaClienteService.QuitarFavorito(clienteId, barberoId);
        return result.IsFailure ? BadRequest(result.Error) : NoContent();
    }

    [Authorize(Roles = RolesSistema.Cliente)]
    [HttpDelete("me")]
    public async Task<IActionResult> EliminarMiCuenta(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var clienteId)) return Unauthorized();
        if (fotoPerfilAzureService.Configurado())
        {
            try
            {
                await fotoPerfilAzureService.Eliminar(clienteId, cancellationToken);
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, "No se pudo eliminar la foto de Azure Blob Storage.");
            }
        }
        var eliminada = await autenticacionCliente.EliminarCuenta(clienteId, cancellationToken);
        return eliminada ? NoContent() : NotFound("No se encontró una cuenta activa para eliminar.");
    }
}
