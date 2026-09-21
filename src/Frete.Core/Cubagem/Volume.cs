namespace Frete.Core.Cubagem;

/// <summary>Dimensões de um volume, em centímetros.</summary>
/// <param name="Comprimento">Comprimento em cm.</param>
/// <param name="Largura">Largura em cm.</param>
/// <param name="Altura">Altura em cm.</param>
public readonly record struct Dimensoes(decimal Comprimento, decimal Largura, decimal Altura)
{
    /// <summary>Volume em metros cúbicos.</summary>
    public decimal MetrosCubicos => Comprimento * Largura * Altura / 1_000_000m;

    /// <summary>Confere se as três medidas são positivas.</summary>
    public Dimensoes Validar()
    {
        if (Comprimento <= 0 || Largura <= 0 || Altura <= 0)
            throw new ArgumentOutOfRangeException(nameof(Comprimento), "As três medidas precisam ser positivas.");

        return this;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Comprimento}x{Largura}x{Altura} cm";
}

/// <summary>
/// Um volume da carga: o que ele pesa e o espaço que ocupa.
/// </summary>
/// <remarks>
/// A transportadora cobra pelo que a carga <em>custa</em> transportar, e uma
/// caixa de travesseiros custa o mesmo que uma de parafusos porque ocupa o
/// mesmo espaço no caminhão. Daí o peso cubado: o volume convertido em quilos
/// por um fator, e o que vale é o maior entre ele e o peso real.
/// </remarks>
public sealed record Volume
{
    /// <summary>Fator usado no transporte rodoviário: 300 kg por metro cúbico.</summary>
    public const decimal FatorRodoviario = 300m;

    /// <summary>Fator usado no transporte aéreo: 167 kg por metro cúbico.</summary>
    public const decimal FatorAereo = 167m;

    /// <summary>Peso real em quilos.</summary>
    public required decimal PesoEmKg { get; init; }

    /// <summary>Dimensões da embalagem.</summary>
    public required Dimensoes Dimensoes { get; init; }

    /// <summary>Quantas peças iguais a esta.</summary>
    public int Quantidade { get; init; } = 1;

    /// <summary>Peso real somando todas as peças.</summary>
    public decimal PesoTotal => PesoEmKg * Quantidade;

    /// <summary>Volume total em metros cúbicos.</summary>
    public decimal MetrosCubicos => Dimensoes.MetrosCubicos * Quantidade;

    /// <summary>Peso cubado, pelo fator informado.</summary>
    public decimal PesoCubado(decimal fator) => Arredondar(MetrosCubicos * fator);

    /// <summary>O peso que a transportadora cobra: o maior entre real e cubado.</summary>
    public decimal PesoTaxavel(decimal fator) => Math.Max(Arredondar(PesoTotal), PesoCubado(fator));

    /// <summary>Indica se é o volume, e não o peso, que manda no preço.</summary>
    public bool Manda(decimal fator) => PesoCubado(fator) > Arredondar(PesoTotal);

    /// <summary>Confere os valores e devolve o próprio volume.</summary>
    public Volume Validar()
    {
        if (PesoEmKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(PesoEmKg), "O peso precisa ser positivo.");

        if (Quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(Quantidade), "A quantidade precisa ser positiva.");

        Dimensoes.Validar();
        return this;
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 3, MidpointRounding.AwayFromZero);
}

/// <summary>Contas sobre um conjunto de volumes.</summary>
public static class Carga
{
    /// <summary>Peso real somado.</summary>
    public static decimal PesoReal(IEnumerable<Volume> volumes) => volumes.Sum(volume => volume.PesoTotal);

    /// <summary>Metros cúbicos somados.</summary>
    public static decimal MetrosCubicos(IEnumerable<Volume> volumes) => volumes.Sum(volume => volume.MetrosCubicos);

    /// <summary>Peso cubado somado.</summary>
    public static decimal PesoCubado(IEnumerable<Volume> volumes, decimal fator)
        => Math.Round(MetrosCubicos(volumes) * fator, 3, MidpointRounding.AwayFromZero);

    /// <summary>
    /// O peso que entra na tabela. A comparação é feita no total da carga, e
    /// não volume a volume: é assim que a transportadora fecha o conhecimento.
    /// </summary>
    public static decimal PesoTaxavel(IEnumerable<Volume> volumes, decimal fator)
    {
        var lista = volumes as IReadOnlyCollection<Volume> ?? volumes.ToList();
        return Math.Max(Math.Round(PesoReal(lista), 3, MidpointRounding.AwayFromZero), PesoCubado(lista, fator));
    }

    /// <summary>Quantas peças a carga tem no total.</summary>
    public static int Pecas(IEnumerable<Volume> volumes) => volumes.Sum(volume => volume.Quantidade);
}
