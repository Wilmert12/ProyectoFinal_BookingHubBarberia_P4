namespace BookingHub.API;

public sealed class InformacionBarberiaOptions
{
    public const string Seccion = "Barberia";
    public string Nombre { get; set; } = "CerquilloApp";
    public string Direccion { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string WhatsApp { get; set; } = "";
    public string Horario { get; set; } = "";
    public string UrlMapa { get; set; } = "";
}
