using Frete.Core.Geografia;

namespace Frete.Core.Cotacao;

/// <summary>Uma parcela do preço, com o nome do que ela é.</summary>
/// <param name="Item">O que está sendo cobrado.</param>
/// <param name="Valor">Quanto.</param>
/// <param name="Detalhe">Como o valor foi obtido.</param>
public sealed record Parcela(string Item, decimal Valor, string Detalhe = "")
{
    /// <inheritdoc />
    public override string ToString()
        => string.IsNullOrEmpty(Detalhe) ? $"{Item}: {Valor:0.00}" : $"{Item}: {Valor:0.00} ({Detalhe})";
}

/// <summary>
/// A cotação de uma transportadora, com o preço aberto parcela a parcela.
/// </summary>
/// <remarks>
/// Entregar só o total é o que faz o cliente desconfiar do frete. Mostrar de
/// onde vem cada real — peso, ad valorem, GRIS, pedágio e imposto — resolve a
/// maior parte das dúvidas antes de elas virarem chamado.
/// </remarks>
public sealed record Cotacao
{
    /// <summary>Nome da transportadora.</summary>
    public required string Transportadora { get; init; }

    /// <summary>Zona tarifária usada.</summary>
    public required Zona Zona { get; init; }

    /// <summary>Peso que entrou na tabela.</summary>
    public required decimal PesoTaxavel { get; init; }

    /// <summary>Peso real da carga.</summary>
    public required decimal PesoReal { get; init; }

    /// <summary>Peso cubado da carga.</summary>
    public required decimal PesoCubado { get; init; }

    /// <summary>As parcelas que compõem o preço, sem o imposto.</summary>
    public required IReadOnlyList<Parcela> Parcelas { get; init; }

    /// <summary>Soma das parcelas, antes do ICMS.</summary>
    public decimal Subtotal => Arredondar(Parcelas.Sum(parcela => parcela.Valor));

    /// <summary>Valor do ICMS embutido.</summary>
    public required decimal Icms { get; init; }

    /// <summary>Valor total a pagar.</summary>
    public required decimal Total { get; init; }

    /// <summary>Prazo em dias úteis.</summary>
    public required int PrazoEmDiasUteis { get; init; }

    /// <summary>Data prevista de entrega.</summary>
    public required DateOnly Entrega { get; init; }

    /// <summary>Indica se o volume, e não o peso, definiu o preço.</summary>
    public bool CubagemMandou => PesoCubado > PesoReal;

    /// <summary>Observações, como área de difícil acesso ou valor mínimo aplicado.</summary>
    public IReadOnlyList<string> Observacoes { get; init; } = [];

    /// <summary>O preço aberto, pronto para mostrar.</summary>
    public string Detalhar()
    {
        var linhas = new List<string>
        {
            $"{Transportadora} — {Zonas.Descrever(Zona)}",
            $"  peso real {PesoReal:0.###} kg, cubado {PesoCubado:0.###} kg, taxável {PesoTaxavel:0.###} kg",
        };

        foreach (var parcela in Parcelas)
        {
            linhas.Add($"  {parcela}");
        }

        linhas.Add($"  ICMS: {Icms:0.00}");
        linhas.Add($"  TOTAL: {Total:0.00} — entrega em {Entrega:dd/MM/yyyy} ({PrazoEmDiasUteis} dias úteis)");

        foreach (var observacao in Observacoes)
        {
            linhas.Add($"  * {observacao}");
        }

        return string.Join(Environment.NewLine, linhas);
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
