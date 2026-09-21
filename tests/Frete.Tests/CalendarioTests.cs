using Frete.Core.Calendario;

namespace Frete.Tests;

public class FeriadosTests
{
    [Theory]
    [InlineData(2024, "2024-03-31")]
    [InlineData(2025, "2025-04-20")]
    [InlineData(2026, "2026-04-05")]
    [InlineData(2027, "2027-03-28")]
    public void Calcula_a_pascoa(int ano, string esperado)
    {
        Feriados.Pascoa(ano).Should().Be(DateOnly.Parse(esperado));
    }

    [Fact]
    public void A_pascoa_cai_sempre_num_domingo()
    {
        for (var ano = 2020; ano <= 2060; ano++)
        {
            Feriados.Pascoa(ano).DayOfWeek.Should().Be(DayOfWeek.Sunday);
        }
    }

    [Fact]
    public void Os_moveis_saem_da_pascoa()
    {
        Feriados.Carnaval(2026).Should().Be(new DateOnly(2026, 2, 17));
        Feriados.SextaFeiraSanta(2026).Should().Be(new DateOnly(2026, 4, 3));
        Feriados.CorpusChristi(2026).Should().Be(new DateOnly(2026, 6, 4));
    }

    [Fact]
    public void O_carnaval_cai_sempre_numa_terca()
    {
        for (var ano = 2020; ano <= 2040; ano++)
        {
            Feriados.Carnaval(ano).DayOfWeek.Should().Be(DayOfWeek.Tuesday);
        }
    }

    [Fact]
    public void Corpus_christi_cai_sempre_numa_quinta()
    {
        for (var ano = 2020; ano <= 2040; ano++)
        {
            Feriados.CorpusChristi(ano).DayOfWeek.Should().Be(DayOfWeek.Thursday);
        }
    }

    [Theory]
    [InlineData("2026-01-01")]
    [InlineData("2026-04-21")]
    [InlineData("2026-05-01")]
    [InlineData("2026-09-07")]
    [InlineData("2026-12-25")]
    public void Reconhece_os_feriados_fixos(string data)
    {
        Feriados.EhFeriado(DateOnly.Parse(data)).Should().BeTrue();
    }

    [Fact]
    public void A_consciencia_negra_e_nacional_a_partir_de_2024()
    {
        Feriados.EhFeriado(new DateOnly(2024, 11, 20)).Should().BeTrue();
        Feriados.EhFeriado(new DateOnly(2023, 11, 20)).Should().BeFalse();
    }

    [Fact]
    public void O_domingo_de_pascoa_nao_e_feriado_nacional()
    {
        // A Páscoa serve de âncora para o Carnaval, a Sexta-feira Santa e o
        // Corpus Christi, mas ela mesma não é feriado — e cai no domingo de
        // qualquer jeito.
        Feriados.EhFeriado(Feriados.Pascoa(2026)).Should().BeFalse();
        Feriados.EhFeriado(Feriados.SextaFeiraSanta(2026)).Should().BeTrue();
    }

    [Fact]
    public void Dia_comum_nao_e_feriado()
    {
        Feriados.EhFeriado(new DateOnly(2026, 3, 10)).Should().BeFalse();
        Feriados.Nome(new DateOnly(2026, 3, 10)).Should().BeNull();
    }

    [Fact]
    public void Devolve_o_nome_do_feriado()
    {
        Feriados.Nome(new DateOnly(2026, 12, 25)).Should().Be("Natal");
        Feriados.Nome(new DateOnly(2026, 2, 17)).Should().Be("Carnaval");
    }

    [Fact]
    public void Lista_os_feriados_do_ano()
    {
        Feriados.Do(2026).Should().HaveCount(12);
    }

    [Theory]
    [InlineData(1500)]
    [InlineData(5000)]
    public void Ano_fora_do_calendario_gregoriano_reclama(int ano)
    {
        var acao = () => Feriados.Pascoa(ano);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }
}

public class DiasUteisTests
{
    private readonly DiasUteis calendario = DiasUteis.Nacional;

    [Fact]
    public void Fim_de_semana_nao_e_util()
    {
        // 19/09/2026 é sábado e 20/09 é domingo.
        calendario.EhUtil(new DateOnly(2026, 9, 19)).Should().BeFalse();
        calendario.EhUtil(new DateOnly(2026, 9, 20)).Should().BeFalse();
        calendario.EhUtil(new DateOnly(2026, 9, 21)).Should().BeTrue();
    }

    [Fact]
    public void Feriado_nao_e_util()
    {
        calendario.EhUtil(new DateOnly(2026, 12, 25)).Should().BeFalse();
    }

    [Fact]
    public void O_proximo_util_pula_o_fim_de_semana()
    {
        calendario.ProximoUtil(new DateOnly(2026, 9, 19)).Should().Be(new DateOnly(2026, 9, 21));
    }

    [Fact]
    public void O_proximo_util_de_um_dia_util_e_ele_mesmo()
    {
        calendario.ProximoUtil(new DateOnly(2026, 9, 21)).Should().Be(new DateOnly(2026, 9, 21));
    }

    [Fact]
    public void Somar_zero_devolve_o_proximo_util()
    {
        calendario.Somar(new DateOnly(2026, 9, 19), 0).Should().Be(new DateOnly(2026, 9, 21));
    }

    [Fact]
    public void Cinco_dias_uteis_a_partir_de_quinta_caem_na_quinta_seguinte()
    {
        // 24/09/2026 é quinta; cinco dias úteis chegam em 01/10, não em 29/09.
        calendario.Somar(new DateOnly(2026, 9, 24), 5).Should().Be(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void O_prazo_atravessa_o_feriado()
    {
        // 25/12/2026 é sexta; dois dias úteis a partir de 24/12 caem em 29/12,
        // porque o Natal e o fim de semana ficam pelo caminho.
        calendario.Somar(new DateOnly(2026, 12, 24), 2).Should().Be(new DateOnly(2026, 12, 29));
    }

    [Fact]
    public void Feriado_local_tambem_conta()
    {
        var comAniversario = new DiasUteis([new DateOnly(2026, 1, 25)]);

        comAniversario.EhUtil(new DateOnly(2026, 1, 25)).Should().BeFalse();
        comAniversario.FeriadosLocais.Should().ContainSingle();
    }

    [Fact]
    public void Prazo_negativo_reclama()
    {
        var acao = () => calendario.Somar(new DateOnly(2026, 9, 21), -1);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Conta_os_dias_uteis_entre_duas_datas()
    {
        // De segunda 21/09 a sexta 25/09: quatro dias úteis depois da inicial.
        calendario.Entre(new DateOnly(2026, 9, 21), new DateOnly(2026, 9, 25)).Should().Be(4);
    }

    [Fact]
    public void Contar_de_tras_para_frente_reclama()
    {
        var acao = () => calendario.Entre(new DateOnly(2026, 9, 25), new DateOnly(2026, 9, 21));

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }
}
