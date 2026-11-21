using BookingHub.WinForms.Api;

namespace BookingHub.WinForms.Infraestructura;

public class Apis(
    IAuthApi auth,
    ICitaApi citas,
    IClienteApi clientes,
    IBarberoApi barberos,
    ICatalogoItemApi catalogo,
    IPagoApi pagos,
    ICatalogosApi catalogos,
    IReporteApi reportes,
    IUsuarioApi usuarios,
    IIndisponibilidadApi indisponibilidades)
{
    public IAuthApi Auth => auth;
    public ICitaApi Citas => citas;
    public IClienteApi Clientes => clientes;
    public IBarberoApi Barberos => barberos;
    public ICatalogoItemApi Catalogo => catalogo;
    public IPagoApi Pagos => pagos;
    public ICatalogosApi Catalogos => catalogos;
    public IReporteApi Reportes => reportes;
    public IUsuarioApi Usuarios => usuarios;
    public IIndisponibilidadApi Indisponibilidades => indisponibilidades;
}
