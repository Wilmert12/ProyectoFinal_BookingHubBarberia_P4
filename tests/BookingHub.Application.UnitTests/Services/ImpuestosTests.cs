using BookingHub.Application.Services;

namespace BookingHub.Application.UnitTests.Services;

public class ImpuestosTests
{
    [Theory]
    [InlineData(600, 91.53, 508.47)]
    [InlineData(118, 18, 100)]
    [InlineData(400, 61.02, 338.98)]
    [InlineData(0, 0, 0)]
    public void ItbisIncluido_SeparaElImpuestoDelTotal(decimal total, decimal itbis, decimal sinItbis)
    {
        Assert.Equal(itbis, Impuestos.ItbisIncluido(total));
        Assert.Equal(sinItbis, Impuestos.SinItbis(total));
        Assert.Equal(total, Impuestos.ItbisIncluido(total) + Impuestos.SinItbis(total));
    }
}
