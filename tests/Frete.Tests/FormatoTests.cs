using System.Globalization;
using Frete.Core;
using Frete.Core.Cotacao;
using Frete.Core.Tabela;

namespace Frete.Tests;

/// <summary>
/// Estes testes trocam a cultura da thread de propósito.
/// </summary>
/// <remarks>
/// Sem eles a suíte passa no Windows em português e mente: no servidor a
/// cotação sairia com "1,234.56" em vez de "1.234,56". Rodar a mesma asserção
/// sob duas culturas é o que transforma isso em erro na máquina de quem
/// escreveu.
/// </remarks>
public class FormatoTests : IDisposable
{
    private static readonly DateOnly Segunda = new(2026, 9, 21);

    private readonly CultureInfo original = CultureInfo.CurrentCulture;

    private static void Sob(string cultura, Action verificar)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultura);
        verificar();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = original;
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("")]
    public void A_moeda_sai_em_formato_brasileiro_em_qualquer_cultura(string cultura)
    {
        Sob(cultura, () => Formato.Moeda(1_234.56m).Should().Be("1.234,56"));
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public void O_peso_nao_arrasta_zero_a_toa(string cultura)
    {
        Sob(cultura, () =>
        {
            Formato.Peso(144m).Should().Be("144");
            Formato.Peso(18.5m).Should().Be("18,5");
        });
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public void A_razao_vira_percentual(string cultura)
    {
        Sob(cultura, () => Formato.Razao(0.005m).Should().Be("0,50%"));
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public void A_faixa_se_descreve_igual_em_qualquer_cultura(string cultura)
    {
        Sob(cultura, () =>
        {
            new FaixaDePeso { AteKg = 1_000, Preco = 1_234.5m }.ToString()
                .Should().Be("até 1000kg: 1.234,50");

            new FaixaDePeso { Preco = 214m, PorKgExcedente = 1.92m }.ToString()
                .Should().Be("acima: 214,00 + 1,92/kg");
        });
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public void O_detalhamento_da_cotacao_sai_igual_em_qualquer_cultura(string cultura)
    {
        Sob(cultura, () =>
        {
            var pedido = PedidoDeCotacao.Simples("01310100", "69450000", 3, 100, 80, 60, 2_500m, Segunda);

            var detalhe = new Precificador().Cotar(pedido, Exemplos.Economica).Detalhar();

            detalhe.Should().Contain("peso real 3 kg, cubado 144 kg");
            detalhe.Should().Contain("0,40% de 2.500,00");
            detalhe.Should().Contain("TOTAL: 417,59");
            detalhe.Should().Contain("entrega em 13/10/2026");
            detalhe.Should().NotContain("2,500.00");
        });
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public void A_data_da_entrega_usa_barra_em_qualquer_cultura(string cultura)
    {
        // Em algumas culturas a barra do formato vira ponto ou traço; aqui ela
        // precisa continuar barra.
        Sob(cultura, () =>
        {
            var pedido = PedidoDeCotacao.Simples("01310100", "04567000", 5, 30, 20, 20, 0, Segunda);

            new Precificador().Cotar(pedido, Exemplos.Regional).Detalhar().Should().Contain("/2026");
        });
    }
}
