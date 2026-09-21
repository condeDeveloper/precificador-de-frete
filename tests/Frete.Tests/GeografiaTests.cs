using Frete.Core.Geografia;

namespace Frete.Tests;

public class CepTests
{
    [Theory]
    [InlineData("01310100", "SP")]
    [InlineData("20040-020", "RJ")]
    [InlineData("30130-010", "MG")]
    [InlineData("40020-000", "BA")]
    [InlineData("70040-010", "DF")]
    [InlineData("80010-000", "PR")]
    [InlineData("90010-000", "RS")]
    [InlineData("69005-040", "AM")]
    public void Deduz_a_uf_pela_faixa(string entrada, string uf)
    {
        Cep.Analisar(entrada).Uf.Should().Be(uf);
    }

    [Theory]
    [InlineData(72850, "GO")]
    [InlineData(73100, "DF")]
    [InlineData(73800, "GO")]
    public void Goias_e_o_distrito_federal_se_intercalam(int prefixo, string uf)
    {
        // As faixas do DF e de Goiás não são contíguas; é o caso que quebra
        // qualquer implementação que use só o primeiro dígito.
        Cep.UfDe(prefixo).Should().Be(uf);
    }

    [Theory]
    [InlineData(69350, "RR")]
    [InlineData(69500, "AM")]
    [InlineData(78950, "RO")]
    public void Estados_com_mais_de_uma_faixa(int prefixo, string uf)
    {
        Cep.UfDe(prefixo).Should().Be(uf);
    }

    [Fact]
    public void Aceita_mascara_e_espacos()
    {
        Cep.Analisar(" 01310-100 ").Digitos.Should().Be("01310100");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("abcdefgh")]
    [InlineData("00100000")]
    public void Recusa_cep_invalido(string? entrada)
    {
        Cep.TentarAnalisar(entrada, out _).Should().BeFalse();
    }

    [Fact]
    public void Analisar_lanca_quando_o_cep_e_invalido()
    {
        var acao = () => Cep.Analisar("00000000");

        acao.Should().Throw<FormatException>();
    }

    [Fact]
    public void Separa_prefixo_e_sufixo()
    {
        var cep = Cep.Analisar("01310100");

        cep.Prefixo.Should().Be(1310);
        cep.Sufixo.Should().Be(100);
    }

    [Fact]
    public void Formata_com_o_traco()
    {
        Cep.Analisar("01310100").Formatado().Should().Be("01310-100");
    }

    [Theory]
    [InlineData("01310100", Regiao.Sudeste)]
    [InlineData("40020000", Regiao.Nordeste)]
    [InlineData("80010000", Regiao.Sul)]
    [InlineData("70040010", Regiao.CentroOeste)]
    [InlineData("69005040", Regiao.Norte)]
    public void Sabe_a_regiao(string entrada, Regiao regiao)
    {
        Cep.Analisar(entrada).Regiao.Should().Be(regiao);
    }

    [Fact]
    public void Sabe_se_esta_dentro_de_uma_faixa()
    {
        var cep = Cep.Analisar("69450000");

        cep.Entre(69400, 69899).Should().BeTrue();
        cep.Entre(1000, 19999).Should().BeFalse();
    }
}

public class ZonaTests
{
    private static Cep De(string cep) => Cep.Analisar(cep);

    [Fact]
    public void Mesma_uf_e_mesmo_estado()
    {
        Zonas.Entre(De("01310100"), De("04567000")).Should().Be(Zona.MesmoEstado);
    }

    [Fact]
    public void Ufs_diferentes_na_mesma_regiao()
    {
        Zonas.Entre(De("01310100"), De("20040020")).Should().Be(Zona.MesmaRegiao);
    }

    [Fact]
    public void Regioes_diferentes()
    {
        Zonas.Entre(De("01310100"), De("69005040")).Should().Be(Zona.OutraRegiao);
    }

    [Fact]
    public void A_zona_e_a_mesma_nos_dois_sentidos()
    {
        var sp = De("01310100");
        var am = De("69005040");

        Zonas.Entre(sp, am).Should().Be(Zonas.Entre(am, sp));
    }

    [Theory]
    [InlineData(Zona.MesmoEstado, "mesmo estado")]
    [InlineData(Zona.MesmaRegiao, "mesma região")]
    [InlineData(Zona.OutraRegiao, "outra região")]
    public void Descreve_a_zona(Zona zona, string esperado)
    {
        Zonas.Descrever(zona).Should().Be(esperado);
    }
}
