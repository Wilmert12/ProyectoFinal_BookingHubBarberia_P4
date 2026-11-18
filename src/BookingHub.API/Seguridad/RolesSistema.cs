namespace BookingHub.API.Seguridad;

public static class RolesSistema
{
    public const string Admin = "Admin";
    public const string Recepcion = "Recepcion";
    public const string Personal = Admin + "," + Recepcion;
}
