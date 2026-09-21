using Frete.Core.Calendario;
using Frete.Core.Cotacao;
using Frete.Core.Cubagem;
using Frete.Core.Geografia;
using Frete.Core.Tabela;

namespace Frete.Tests;

public class PrecificadorTests
{
    private static readonly DateOnly Segunda = new(2026, 9, 21);

    private readonly Precificador precificador = new();

    private static PedidoDeCotacao Pedido(
        string origem = "01310100",
        string destino = "04567000",
        decimal peso = 5,
        decimal valorDaNota = 0) =>
        PedidoDeCotacao.Simples(origem, destino, peso, 30, 20, 20, valorDaNota, Segunda);

    private static TabelaDeFrete Simples(decimal icms = 0m, decimal minimo = 0m) => new TabelaDeFrete
    {
        Transportadora = "Teste",
        AdValorem = 0,
        Gris = 0,
        Icms = icms,
        Minimo = minimo,
        Zonas = new Dictionary<Zona, PrecoDaZona>
        {
            [Zona.MesmoEstado] = new()
            {
                PrazoEmDiasUteis = 2,
                Faixas =
                [
                    new FaixaDePeso { AteKg = 10, Preco = 100m },
                    new FaixaDePeso { Preco = 100m, PorKgExcedente = 5m },
                ],
            },
        },
    };

    [Fact]
    public void A_faixa_de_peso_define_o_preco_base()
    {
        var cotacao = precificador.Cotar(Pedido(peso: 5), Simples());

        cotacao.Total.Should().Be(100m);
        cotacao.Parcelas.Should().ContainSingle(parcela => parcela.Item == "frete peso");
    }

    [Fact]
    public void Acima_da_ultima_faixa_cobra_por_quilo_excedente()
    {
        var cotacao = precificador.Cotar(Pedido(peso: 15), Simples());

        // 100 da faixa aberta, mais cinco quilos acima dos dez.
        cotacao.Total.Should().Be(125m);
    }

    [Fact]
    public void O_excedente_e_cobrado_por_quilo_inteiro()
    {
        var cotacao = precificador.Cotar(Pedido(peso: 10.2m), Simples());

        cotacao.Total.Should().Be(105m);
    }

    [Fact]
    public void O_icms_e_calculado_por_dentro()
    {
        // Com base 100 e alíquota de 12%, o total não é 112: é 100/0,88.
        var cotacao = precificador.Cotar(Pedido(peso: 5), Simples(icms: 0.12m));

        cotacao.Subtotal.Should().Be(100m);
        cotacao.Total.Should().Be(113.64m);
        cotacao.Icms.Should().Be(13.64m);
    }

    [Fact]
    public void O_icms_por_dentro_fecha_com_a_aliquota()
    {
        var cotacao = precificador.Cotar(Pedido(peso: 5), Simples(icms: 0.12m));

        // O imposto é 12% do total, e não do subtotal — é isso que "por
        // dentro" significa.
        (cotacao.Icms / cotacao.Total).Should().BeApproximately(0.12m, 0.001m);
    }

    [Fact]
    public void Sem_icms_o_total_e_o_subtotal()
    {
        var cotacao = precificador.Cotar(Pedido(peso: 5), Simples());

        cotacao.Icms.Should().Be(0m);
        cotacao.Total.Should().Be(cotacao.Subtotal);
    }

    [Fact]
    public void O_minimo_completa_o_frete_barato()
    {
        var cotacao = precificador.Cotar(Pedido(peso: 5), Simples(minimo: 150m));

        cotacao.Subtotal.Should().Be(150m);
        cotacao.Parcelas.Should().Contain(parcela => parcela.Item == "complemento de mínimo");
        cotacao.Observacoes.Should().Contain(texto => texto.Contains("mínimo"));
    }

    [Fact]
    public void O_minimo_nao_mexe_no_frete_caro()
    {
        var cotacao = precificador.Cotar(Pedido(peso: 5), Simples(minimo: 50m));

        cotacao.Subtotal.Should().Be(100m);
    }

    [Fact]
    public void Ad_valorem_e_gris_saem_do_valor_da_nota()
    {
        var cotacao = precificador.Cotar(Pedido(valorDaNota: 10_000m), Exemplos.Economica);

        cotacao.Parcelas.Should().Contain(parcela => parcela.Item == "ad valorem" && parcela.Valor == 40m);
        cotacao.Parcelas.Should().Contain(parcela => parcela.Item == "GRIS" && parcela.Valor == 8m);
    }

    [Fact]
    public void Sem_valor_de_nota_nao_ha_ad_valorem()
    {
        var cotacao = precificador.Cotar(Pedido(valorDaNota: 0), Exemplos.Economica);

        cotacao.Parcelas.Should().NotContain(parcela => parcela.Item == "ad valorem");
    }

    [Fact]
    public void O_pedagio_e_cobrado_por_fracao_de_cem_quilos()
    {
        var cotacao = precificador.Cotar(Pedido(peso: 101), Exemplos.Economica);

        // 101 kg pagam duas frações.
        cotacao.Parcelas.Should().Contain(parcela => parcela.Item == "pedágio" && parcela.Valor == 9m);
    }

    [Fact]
    public void A_cubagem_pode_mandar_no_preco()
    {
        var pedido = new PedidoDeCotacao
        {
            Origem = Cep.Analisar("01310100"),
            Destino = Cep.Analisar("04567000"),
            Coleta = Segunda,
            Volumes = [new Volume { PesoEmKg = 3, Dimensoes = new Dimensoes(100, 80, 60) }],
        };

        var cotacao = precificador.Cotar(pedido, Exemplos.Economica);

        cotacao.CubagemMandou.Should().BeTrue();
        cotacao.PesoTaxavel.Should().Be(144m);
        cotacao.Observacoes.Should().Contain(texto => texto.Contains("cubagem"));
    }

    [Fact]
    public void A_zona_muda_o_preco_e_o_prazo()
    {
        var dentro = precificador.Cotar(Pedido(destino: "04567000"), Exemplos.Economica);
        var longe = precificador.Cotar(Pedido(destino: "69005040"), Exemplos.Economica);

        longe.Total.Should().BeGreaterThan(dentro.Total);
        longe.PrazoEmDiasUteis.Should().BeGreaterThan(dentro.PrazoEmDiasUteis);
    }

    [Fact]
    public void Area_de_dificil_acesso_soma_taxa_e_prazo()
    {
        var normal = precificador.Cotar(Pedido(destino: "69005040"), Exemplos.Economica);
        var dificil = precificador.Cotar(Pedido(destino: "69450000"), Exemplos.Economica);

        dificil.PrazoEmDiasUteis.Should().Be(normal.PrazoEmDiasUteis + 6);
        dificil.Parcelas.Should().Contain(parcela => parcela.Item == "taxa de área");
        dificil.Observacoes.Should().Contain(texto => texto.Contains("fluvial"));
    }

    [Fact]
    public void A_entrega_e_contada_em_dias_uteis()
    {
        // Coleta na segunda 21/09 com prazo de 2 dias úteis: quarta 23/09.
        var cotacao = precificador.Cotar(Pedido(), Simples());

        cotacao.Entrega.Should().Be(new DateOnly(2026, 9, 23));
    }

    [Fact]
    public void A_entrega_pula_feriado_e_fim_de_semana()
    {
        var pedido = PedidoDeCotacao.Simples("01310100", "04567000", 5, 30, 20, 20, 0, new DateOnly(2026, 12, 24));

        var cotacao = precificador.Cotar(pedido, Simples());

        // 25/12 é sexta e feriado; sobram segunda 28 e terça 29.
        cotacao.Entrega.Should().Be(new DateOnly(2026, 12, 29));
    }

    [Fact]
    public void Feriado_local_atrasa_a_entrega()
    {
        var comFeriado = new Precificador(new DiasUteis([new DateOnly(2026, 9, 22)]));

        var cotacao = comFeriado.Cotar(Pedido(), Simples());

        cotacao.Entrega.Should().Be(new DateOnly(2026, 9, 24));
    }

    [Fact]
    public void Transportadora_que_nao_atende_a_zona_reclama()
    {
        var acao = () => precificador.Cotar(Pedido(destino: "69005040"), Exemplos.Regional);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*não atende*");
    }

    [Fact]
    public void Comparar_ordena_da_mais_barata_para_a_mais_cara()
    {
        var cotacoes = precificador.Comparar(Pedido(peso: 8, valorDaNota: 500), Exemplos.Todas);

        cotacoes.Should().HaveCount(3);
        cotacoes.Should().BeInAscendingOrder(cotacao => cotacao.Total);
    }

    [Fact]
    public void Comparar_deixa_de_fora_quem_nao_atende()
    {
        var cotacoes = precificador.Comparar(Pedido(destino: "69005040"), Exemplos.Todas);

        cotacoes.Should().NotContain(cotacao => cotacao.Transportadora == "Local Entregas");
    }

    [Fact]
    public void A_mais_barata_nem_sempre_e_a_mais_rapida()
    {
        var pedido = Pedido(peso: 8, valorDaNota: 500);

        var barata = precificador.MaisBarata(pedido, Exemplos.Todas);
        var rapida = precificador.MaisRapida(pedido, Exemplos.Todas);

        barata!.Total.Should().BeLessThan(rapida!.Total);
        rapida.PrazoEmDiasUteis.Should().BeLessThan(barata.PrazoEmDiasUteis);
    }

    [Fact]
    public void Sem_nenhuma_tabela_nao_ha_cotacao()
    {
        precificador.MaisBarata(Pedido(), []).Should().BeNull();
    }

    [Fact]
    public void O_detalhamento_mostra_as_parcelas()
    {
        var detalhe = precificador.Cotar(Pedido(valorDaNota: 1000), Exemplos.Economica).Detalhar();

        detalhe.Should().Contain("frete peso").And.Contain("ICMS").And.Contain("TOTAL");
    }

    [Fact]
    public void Pedido_sem_volume_reclama()
    {
        var pedido = new PedidoDeCotacao
        {
            Origem = Cep.Analisar("01310100"),
            Destino = Cep.Analisar("04567000"),
            Volumes = [],
        };

        var acao = () => precificador.Cotar(pedido, Simples());

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Valor_de_nota_negativo_reclama()
    {
        var acao = () => precificador.Cotar(Pedido(valorDaNota: -1), Simples());

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }
}

public class TabelaTests
{
    [Fact]
    public void A_faixa_fechada_cobra_preco_fixo()
    {
        var faixa = new FaixaDePeso { AteKg = 10, Preco = 50m, PorKgExcedente = 5m };

        faixa.Cobrar(8, 0).Should().Be(50m);
    }

    [Fact]
    public void A_faixa_aberta_cobra_o_excedente()
    {
        var faixa = new FaixaDePeso { Preco = 50m, PorKgExcedente = 5m };

        faixa.Cobrar(13, 10).Should().Be(65m);
    }

    [Fact]
    public void A_faixa_aberta_sem_excedente_cobra_so_o_preco()
    {
        new FaixaDePeso { Preco = 50m }.Cobrar(500, 10).Should().Be(50m);
    }

    [Fact]
    public void A_faixa_sabe_se_o_peso_cabe()
    {
        new FaixaDePeso { AteKg = 10, Preco = 1 }.Cabe(10).Should().BeTrue();
        new FaixaDePeso { AteKg = 10, Preco = 1 }.Cabe(11).Should().BeFalse();
        new FaixaDePeso { Preco = 1 }.Cabe(9999).Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 10, 1)]
    [InlineData(10, -1, 1)]
    [InlineData(10, 10, -1)]
    public void Faixa_com_valor_invalido_reclama(decimal ate, decimal preco, decimal excedente)
    {
        var faixa = new FaixaDePeso { AteKg = ate == 0 ? 0 : ate, Preco = preco, PorKgExcedente = excedente };

        var acao = () => faixa.Validar();

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Tabela_sem_faixa_aberta_reclama()
    {
        var tabela = new TabelaDeFrete
        {
            Transportadora = "X",
            Zonas = new Dictionary<Zona, PrecoDaZona>
            {
                [Zona.MesmoEstado] = new()
                {
                    PrazoEmDiasUteis = 1,
                    Faixas = [new FaixaDePeso { AteKg = 10, Preco = 1 }],
                },
            },
        };

        var acao = () => tabela.Validar();

        acao.Should().Throw<ArgumentException>().WithMessage("*faixa aberta*");
    }

    [Fact]
    public void Tabela_sem_zona_reclama()
    {
        var tabela = new TabelaDeFrete
        {
            Transportadora = "X",
            Zonas = new Dictionary<Zona, PrecoDaZona>(),
        };

        var acao = () => tabela.Validar();

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Tabela_sem_nome_reclama()
    {
        var tabela = Exemplos.Economica with { Transportadora = "  " };

        var acao = () => tabela.Validar();

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Aliquota_invalida_reclama()
    {
        var acao = () => (Exemplos.Economica with { Icms = 1.5m }).Validar();

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Peso_acima_de_toda_faixa_fechada_reclama()
    {
        var preco = new PrecoDaZona
        {
            PrazoEmDiasUteis = 1,
            Faixas = [new FaixaDePeso { AteKg = 10, Preco = 1 }],
        };

        var acao = () => preco.Cobrar(50);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*faixa aberta*");
    }

    [Fact]
    public void As_tabelas_de_exemplo_sao_validas()
    {
        foreach (var tabela in Exemplos.Todas)
        {
            tabela.Validar().Should().NotBeNull();
        }
    }
}
