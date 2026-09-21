namespace Frete.Core.Tabela;

/// <summary>
/// Uma linha da tabela de preços: até tantos quilos, custa tanto.
/// </summary>
/// <remarks>
/// A última faixa costuma ser aberta, com um preço por quilo excedente — é o
/// que evita ter que listar faixa por faixa até uma tonelada.
/// </remarks>
public sealed record FaixaDePeso
{
    /// <summary>Peso máximo da faixa, em quilos. Nulo significa sem teto.</summary>
    public decimal? AteKg { get; init; }

    /// <summary>Preço fixo da faixa.</summary>
    public required decimal Preco { get; init; }

    /// <summary>Preço de cada quilo acima do teto da faixa anterior.</summary>
    public decimal PorKgExcedente { get; init; }

    /// <summary>Indica se a faixa não tem teto.</summary>
    public bool Aberta => AteKg is null;

    /// <summary>Indica se o peso cabe nesta faixa.</summary>
    public bool Cabe(decimal pesoEmKg) => Aberta || pesoEmKg <= AteKg;

    /// <summary>
    /// Preço para um peso, a partir de onde a faixa começa. O excedente só
    /// aparece na faixa aberta; nas fechadas o preço é fixo.
    /// </summary>
    public decimal Cobrar(decimal pesoEmKg, decimal inicioDaFaixa)
    {
        if (!Aberta || PorKgExcedente <= 0)
        {
            return Preco;
        }

        var excedente = Math.Max(0, pesoEmKg - inicioDaFaixa);
        return Preco + (Math.Ceiling(excedente) * PorKgExcedente);
    }

    /// <summary>Confere os valores da faixa.</summary>
    public FaixaDePeso Validar()
    {
        if (AteKg is <= 0)
            throw new ArgumentOutOfRangeException(nameof(AteKg), "O teto da faixa precisa ser positivo.");

        if (Preco < 0)
            throw new ArgumentOutOfRangeException(nameof(Preco), "O preço não pode ser negativo.");

        if (PorKgExcedente < 0)
            throw new ArgumentOutOfRangeException(nameof(PorKgExcedente), "O excedente não pode ser negativo.");

        return this;
    }

    /// <inheritdoc />
    public override string ToString()
        => Aberta
            ? $"acima: {Formato.Moeda(Preco)} + {Formato.Moeda(PorKgExcedente)}/kg"
            : $"até {Formato.Peso(AteKg ?? 0)}kg: {Formato.Moeda(Preco)}";
}
