using Frete.Core.Calendario;
using Frete.Core.Cubagem;
using Frete.Core.Tabela;

namespace Frete.Core.Cotacao;

/// <summary>
/// Monta a cotação de um pedido contra uma ou mais tabelas.
/// </summary>
/// <remarks>
/// A ordem das contas importa e é a que o conhecimento de transporte usa:
/// primeiro o peso taxável decide a faixa, depois entram os acessórios sobre o
/// valor da nota, e só no fim o ICMS — que é calculado "por dentro", ou seja,
/// a alíquota incide sobre o total já com o próprio imposto.
/// </remarks>
public sealed class Precificador
{
    private readonly DiasUteis calendario;

    /// <summary>Cria o precificador com o calendário informado.</summary>
    public Precificador(DiasUteis? calendario = null)
    {
        this.calendario = calendario ?? DiasUteis.Nacional;
    }

    /// <summary>Cota o pedido em uma tabela.</summary>
    public Cotacao Cotar(PedidoDeCotacao pedido, TabelaDeFrete tabela)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        ArgumentNullException.ThrowIfNull(tabela);

        pedido.Validar();
        tabela.Validar();

        var zona = pedido.Zona;

        if (!tabela.Zonas.TryGetValue(zona, out var preco))
        {
            throw new InvalidOperationException(
                $"A tabela de {tabela.Transportadora} não atende {Geografia.Zonas.Descrever(zona)}.");
        }

        var pesoReal = Carga.PesoReal(pedido.Volumes);
        var pesoCubado = Carga.PesoCubado(pedido.Volumes, tabela.FatorDeCubagem);
        var pesoTaxavel = Math.Max(pesoReal, pesoCubado);

        var parcelas = new List<Parcela>();
        var observacoes = new List<string>();

        var (faixa, valorDoPeso) = preco.Cobrar(pesoTaxavel);
        parcelas.Add(new Parcela("frete peso", valorDoPeso, faixa.ToString()));

        if (pedido.ValorDaNota > 0)
        {
            if (tabela.AdValorem > 0)
            {
                parcelas.Add(new Parcela(
                    "ad valorem",
                    Arredondar(pedido.ValorDaNota * tabela.AdValorem),
                    $"{Formato.Razao(tabela.AdValorem)} de {Formato.Moeda(pedido.ValorDaNota)}"));
            }

            if (tabela.Gris > 0)
            {
                parcelas.Add(new Parcela(
                    "GRIS",
                    Arredondar(pedido.ValorDaNota * tabela.Gris),
                    $"{Formato.Razao(tabela.Gris)} de {Formato.Moeda(pedido.ValorDaNota)}"));
            }
        }

        if (tabela.PedagioPor100Kg > 0)
        {
            // O pedágio é por fração: 101 kg pagam duas vezes.
            var fracoes = Math.Ceiling(pesoTaxavel / 100m);
            parcelas.Add(new Parcela(
                "pedágio",
                Arredondar(fracoes * tabela.PedagioPor100Kg),
                $"{Formato.Peso(fracoes)} × {Formato.Moeda(tabela.PedagioPor100Kg)}"));
        }

        var prazo = preco.PrazoEmDiasUteis;
        var area = tabela.AreaDe(pedido.Destino);

        if (area is not null)
        {
            if (area.Taxa > 0)
            {
                parcelas.Add(new Parcela("taxa de área", area.Taxa, area.Motivo));
            }

            prazo += area.DiasAMais;
            observacoes.Add($"{pedido.Destino.Formatado()} é {area.Motivo}: +{area.DiasAMais} dia(s) útil(eis).");
        }

        var subtotal = Arredondar(parcelas.Sum(parcela => parcela.Valor));

        if (tabela.Minimo > 0 && subtotal < tabela.Minimo)
        {
            parcelas.Add(new Parcela("complemento de mínimo", Arredondar(tabela.Minimo - subtotal), $"mínimo {Formato.Moeda(tabela.Minimo)}"));
            observacoes.Add($"O frete calculado ficou abaixo do mínimo de {Formato.Moeda(tabela.Minimo)}.");
            subtotal = tabela.Minimo;
        }

        // ICMS por dentro: a base inclui o próprio imposto, então não basta
        // multiplicar pela alíquota — o valor com imposto é o subtotal
        // dividido por (1 - alíquota).
        var total = tabela.Icms > 0 ? Arredondar(subtotal / (1 - tabela.Icms)) : subtotal;
        var icms = Arredondar(total - subtotal);

        if (pesoCubado > pesoReal)
        {
            observacoes.Add($"A cubagem mandou no preço: {Formato.Peso(pesoCubado)} kg contra {Formato.Peso(pesoReal)} kg reais.");
        }

        return new Cotacao
        {
            Transportadora = tabela.Transportadora,
            Zona = zona,
            PesoReal = pesoReal,
            PesoCubado = pesoCubado,
            PesoTaxavel = pesoTaxavel,
            Parcelas = parcelas,
            Icms = icms,
            Total = total,
            PrazoEmDiasUteis = prazo,
            Entrega = calendario.Somar(pedido.Coleta, prazo),
            Observacoes = observacoes,
        };
    }

    /// <summary>
    /// Cota em várias tabelas, da mais barata para a mais cara. Quem não
    /// atende a rota simplesmente fica de fora, em vez de derrubar a cotação
    /// inteira.
    /// </summary>
    public IReadOnlyList<Cotacao> Comparar(PedidoDeCotacao pedido, IEnumerable<TabelaDeFrete> tabelas)
    {
        ArgumentNullException.ThrowIfNull(tabelas);

        var cotacoes = new List<Cotacao>();

        foreach (var tabela in tabelas)
        {
            if (!tabela.Atende(pedido.Zona))
            {
                continue;
            }

            cotacoes.Add(Cotar(pedido, tabela));
        }

        return cotacoes
            .OrderBy(cotacao => cotacao.Total)
            .ThenBy(cotacao => cotacao.PrazoEmDiasUteis)
            .ThenBy(cotacao => cotacao.Transportadora, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>A mais barata entre as tabelas, ou nulo se nenhuma atende.</summary>
    public Cotacao? MaisBarata(PedidoDeCotacao pedido, IEnumerable<TabelaDeFrete> tabelas)
        => Comparar(pedido, tabelas).FirstOrDefault();

    /// <summary>A mais rápida entre as tabelas, ou nulo se nenhuma atende.</summary>
    public Cotacao? MaisRapida(PedidoDeCotacao pedido, IEnumerable<TabelaDeFrete> tabelas)
        => Comparar(pedido, tabelas)
            .OrderBy(cotacao => cotacao.PrazoEmDiasUteis)
            .ThenBy(cotacao => cotacao.Total)
            .FirstOrDefault();

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
