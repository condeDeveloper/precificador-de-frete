using Frete.Core.Cubagem;
using Frete.Core.Geografia;

namespace Frete.Core.Tabela;

/// <summary>Uma faixa de CEP com prazo e taxa adicionais.</summary>
/// <param name="Inicio">Primeiro prefixo de cinco dígitos da faixa.</param>
/// <param name="Fim">Último prefixo da faixa.</param>
/// <param name="Taxa">Valor cobrado a mais.</param>
/// <param name="DiasAMais">Dias úteis somados ao prazo.</param>
/// <param name="Motivo">Por que a área é diferenciada.</param>
public sealed record AreaDiferenciada(int Inicio, int Fim, decimal Taxa, int DiasAMais, string Motivo = "área de difícil acesso")
{
    /// <summary>Indica se o CEP cai nesta faixa.</summary>
    public bool Pega(Cep cep) => cep.Entre(Inicio, Fim);
}

/// <summary>O que uma zona custa e em quanto tempo é entregue.</summary>
public sealed record PrecoDaZona
{
    /// <summary>Faixas de peso, da menor para a maior.</summary>
    public required IReadOnlyList<FaixaDePeso> Faixas { get; init; }

    /// <summary>Prazo em dias úteis.</summary>
    public required int PrazoEmDiasUteis { get; init; }

    /// <summary>Encontra a faixa que atende o peso e o quanto ela cobra.</summary>
    public (FaixaDePeso Faixa, decimal Valor) Cobrar(decimal pesoEmKg)
    {
        decimal inicio = 0;

        foreach (var faixa in Faixas)
        {
            if (faixa.Cabe(pesoEmKg))
            {
                return (faixa, faixa.Cobrar(pesoEmKg, inicio));
            }

            inicio = faixa.AteKg ?? inicio;
        }

        throw new InvalidOperationException(
            $"A tabela não cobre {pesoEmKg} kg. Falta uma faixa aberta no fim.");
    }
}

/// <summary>
/// A tabela de uma transportadora: preço por zona e por peso, mais os
/// acessórios que todo conhecimento de transporte carrega.
/// </summary>
public sealed record TabelaDeFrete
{
    /// <summary>Nome da transportadora.</summary>
    public required string Transportadora { get; init; }

    /// <summary>Preços e prazos por zona.</summary>
    public required IReadOnlyDictionary<Zona, PrecoDaZona> Zonas { get; init; }

    /// <summary>Fator de cubagem, em quilos por metro cúbico.</summary>
    public decimal FatorDeCubagem { get; init; } = Volume.FatorRodoviario;

    /// <summary>Percentual do valor da nota cobrado como ad valorem.</summary>
    public decimal AdValorem { get; init; } = 0.005m;

    /// <summary>Percentual do valor da nota cobrado como GRIS.</summary>
    public decimal Gris { get; init; } = 0.001m;

    /// <summary>Pedágio cobrado a cada fração de 100 kg.</summary>
    public decimal PedagioPor100Kg { get; init; }

    /// <summary>Valor mínimo do frete.</summary>
    public decimal Minimo { get; init; }

    /// <summary>Alíquota de ICMS do frete.</summary>
    public decimal Icms { get; init; } = 0.12m;

    /// <summary>Áreas com taxa e prazo diferenciados.</summary>
    public IReadOnlyList<AreaDiferenciada> AreasDiferenciadas { get; init; } = [];

    /// <summary>Indica se a transportadora atende a zona.</summary>
    public bool Atende(Zona zona) => Zonas.ContainsKey(zona);

    /// <summary>A área diferenciada que pega o CEP, se houver.</summary>
    public AreaDiferenciada? AreaDe(Cep cep)
        => AreasDiferenciadas.FirstOrDefault(area => area.Pega(cep));

    /// <summary>Confere a tabela e devolve ela mesma.</summary>
    public TabelaDeFrete Validar()
    {
        if (string.IsNullOrWhiteSpace(Transportadora))
            throw new ArgumentException("A tabela precisa do nome da transportadora.", nameof(Transportadora));

        if (Zonas.Count == 0)
            throw new ArgumentException("A tabela precisa de ao menos uma zona.", nameof(Zonas));

        if (Icms is < 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(Icms), "A alíquota vai de 0 a menos de 1.");

        foreach (var (zona, preco) in Zonas)
        {
            if (preco.Faixas.Count == 0)
                throw new ArgumentException($"A zona {zona} está sem faixas.", nameof(Zonas));

            if (!preco.Faixas[^1].Aberta)
                throw new ArgumentException($"A zona {zona} precisa terminar com faixa aberta.", nameof(Zonas));

            foreach (var faixa in preco.Faixas)
            {
                faixa.Validar();
            }
        }

        return this;
    }
}
