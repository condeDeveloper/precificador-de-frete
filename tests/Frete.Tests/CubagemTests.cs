using Frete.Core.Cubagem;

namespace Frete.Tests;

public class CubagemTests
{
    private static Volume Caixa(decimal peso, decimal c, decimal l, decimal a, int quantidade = 1) => new()
    {
        PesoEmKg = peso,
        Dimensoes = new Dimensoes(c, l, a),
        Quantidade = quantidade,
    };

    [Fact]
    public void Calcula_o_volume_em_metros_cubicos()
    {
        new Dimensoes(100, 100, 100).MetrosCubicos.Should().Be(1m);
        new Dimensoes(50, 40, 30).MetrosCubicos.Should().Be(0.06m);
    }

    [Fact]
    public void O_peso_cubado_e_o_volume_vezes_o_fator()
    {
        // Um metro cúbico no rodoviário vale 300 kg.
        Caixa(1, 100, 100, 100).PesoCubado(Volume.FatorRodoviario).Should().Be(300m);
    }

    [Fact]
    public void O_fator_aereo_e_menor_que_o_rodoviario()
    {
        var caixa = Caixa(1, 100, 100, 100);

        caixa.PesoCubado(Volume.FatorAereo).Should().BeLessThan(caixa.PesoCubado(Volume.FatorRodoviario));
    }

    [Fact]
    public void Carga_pesada_e_pequena_cobra_pelo_peso_real()
    {
        // Uma caixa de parafusos: 40 kg em 30 litros.
        var caixa = Caixa(40, 30, 20, 50);

        caixa.PesoTaxavel(Volume.FatorRodoviario).Should().Be(40m);
        caixa.Manda(Volume.FatorRodoviario).Should().BeFalse();
    }

    [Fact]
    public void Carga_leve_e_grande_cobra_pela_cubagem()
    {
        // Uma caixa de travesseiros: 3 kg ocupando meio metro cúbico.
        var caixa = Caixa(3, 100, 80, 60);

        caixa.PesoTaxavel(Volume.FatorRodoviario).Should().Be(144m);
        caixa.Manda(Volume.FatorRodoviario).Should().BeTrue();
    }

    [Fact]
    public void A_quantidade_multiplica_peso_e_volume()
    {
        var tres = Caixa(2, 50, 40, 30, quantidade: 3);

        tres.PesoTotal.Should().Be(6m);
        tres.MetrosCubicos.Should().Be(0.18m);
    }

    [Fact]
    public void A_carga_soma_os_volumes()
    {
        var volumes = new[] { Caixa(10, 50, 40, 30), Caixa(5, 20, 20, 20) };

        Carga.PesoReal(volumes).Should().Be(15m);
        Carga.MetrosCubicos(volumes).Should().Be(0.068m);
        Carga.Pecas(volumes).Should().Be(2);
    }

    [Fact]
    public void A_comparacao_e_feita_no_total_da_carga()
    {
        // Sozinha, nenhuma das duas caixas seria cubada; juntas, sim.
        var volumes = new[] { Caixa(20, 90, 90, 90), Caixa(20, 90, 90, 90) };

        Carga.PesoReal(volumes).Should().Be(40m);
        Carga.PesoTaxavel(volumes, Volume.FatorRodoviario).Should().BeGreaterThan(400m);
    }

    [Theory]
    [InlineData(0, 10, 10, 10)]
    [InlineData(-1, 10, 10, 10)]
    [InlineData(1, 0, 10, 10)]
    [InlineData(1, 10, -5, 10)]
    public void Medida_invalida_reclama(decimal peso, decimal c, decimal l, decimal a)
    {
        var acao = () => Caixa(peso, c, l, a).Validar();

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Quantidade_invalida_reclama()
    {
        var acao = () => Caixa(1, 10, 10, 10, quantidade: 0).Validar();

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void As_dimensoes_se_descrevem_de_volta()
    {
        new Dimensoes(50, 40, 30).ToString().Should().Be("50x40x30 cm");
    }
}
