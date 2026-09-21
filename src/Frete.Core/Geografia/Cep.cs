namespace Frete.Core.Geografia;

/// <summary>As cinco regiões do país.</summary>
public enum Regiao
{
    /// <summary>Norte.</summary>
    Norte,

    /// <summary>Nordeste.</summary>
    Nordeste,

    /// <summary>Centro-Oeste.</summary>
    CentroOeste,

    /// <summary>Sudeste.</summary>
    Sudeste,

    /// <summary>Sul.</summary>
    Sul,
}

/// <summary>
/// Um CEP válido, com a UF deduzida da faixa em que ele cai.
/// </summary>
/// <remarks>
/// O CEP não é um número qualquer: os Correios dividiram o país em faixas
/// contíguas, e é por isso que dá para saber o estado sem consultar serviço
/// nenhum. A faixa é por prefixo de cinco dígitos, e alguns estados têm mais
/// de uma — Goiás e o Distrito Federal, por exemplo, se intercalam.
/// </remarks>
public readonly record struct Cep
{
    private sealed record Faixa(int Inicio, int Fim, string Uf);

    private static readonly Faixa[] Faixas =
    [
        new(1000, 19999, "SP"),
        new(20000, 28999, "RJ"),
        new(29000, 29999, "ES"),
        new(30000, 39999, "MG"),
        new(40000, 48999, "BA"),
        new(49000, 49999, "SE"),
        new(50000, 56999, "PE"),
        new(57000, 57999, "AL"),
        new(58000, 58999, "PB"),
        new(59000, 59999, "RN"),
        new(60000, 63999, "CE"),
        new(64000, 64999, "PI"),
        new(65000, 65999, "MA"),
        new(66000, 68899, "PA"),
        new(68900, 68999, "AP"),
        new(69000, 69299, "AM"),
        new(69300, 69399, "RR"),
        new(69400, 69899, "AM"),
        new(69900, 69999, "AC"),
        new(70000, 72799, "DF"),
        new(72800, 72999, "GO"),
        new(73000, 73699, "DF"),
        new(73700, 76799, "GO"),
        new(76800, 76999, "RO"),
        new(77000, 77999, "TO"),
        new(78000, 78899, "MT"),
        new(78900, 78999, "RO"),
        new(79000, 79999, "MS"),
        new(80000, 87999, "PR"),
        new(88000, 89999, "SC"),
        new(90000, 99999, "RS"),
    ];

    private static readonly IReadOnlyDictionary<string, Regiao> Regioes = new Dictionary<string, Regiao>(StringComparer.Ordinal)
    {
        ["AC"] = Regiao.Norte, ["AM"] = Regiao.Norte, ["AP"] = Regiao.Norte, ["PA"] = Regiao.Norte,
        ["RO"] = Regiao.Norte, ["RR"] = Regiao.Norte, ["TO"] = Regiao.Norte,
        ["AL"] = Regiao.Nordeste, ["BA"] = Regiao.Nordeste, ["CE"] = Regiao.Nordeste,
        ["MA"] = Regiao.Nordeste, ["PB"] = Regiao.Nordeste, ["PE"] = Regiao.Nordeste,
        ["PI"] = Regiao.Nordeste, ["RN"] = Regiao.Nordeste, ["SE"] = Regiao.Nordeste,
        ["DF"] = Regiao.CentroOeste, ["GO"] = Regiao.CentroOeste,
        ["MS"] = Regiao.CentroOeste, ["MT"] = Regiao.CentroOeste,
        ["ES"] = Regiao.Sudeste, ["MG"] = Regiao.Sudeste, ["RJ"] = Regiao.Sudeste, ["SP"] = Regiao.Sudeste,
        ["PR"] = Regiao.Sul, ["RS"] = Regiao.Sul, ["SC"] = Regiao.Sul,
    };

    private Cep(string digitos, string uf)
    {
        Digitos = digitos;
        Uf = uf;
    }

    /// <summary>Os oito dígitos, sem máscara.</summary>
    public string Digitos { get; }

    /// <summary>A sigla da UF em que o CEP cai.</summary>
    public string Uf { get; }

    /// <summary>O prefixo de cinco dígitos, que é o que define a faixa.</summary>
    public int Prefixo => int.Parse(Digitos[..5]);

    /// <summary>O sufixo de três dígitos.</summary>
    public int Sufixo => int.Parse(Digitos[5..]);

    /// <summary>A região do país.</summary>
    public Regiao Regiao => Regioes[Uf];

    /// <summary>Tenta interpretar o CEP, aceitando máscara e espaços.</summary>
    public static bool TentarAnalisar(string? entrada, out Cep cep)
    {
        cep = default;

        var digitos = SomenteDigitos(entrada);
        if (digitos.Length != 8)
        {
            return false;
        }

        var prefixo = int.Parse(digitos[..5]);
        var uf = UfDe(prefixo);

        if (uf is null)
        {
            return false;
        }

        cep = new Cep(digitos, uf);
        return true;
    }

    /// <summary>Interpreta o CEP ou lança se ele for inválido.</summary>
    public static Cep Analisar(string? entrada)
        => TentarAnalisar(entrada, out var cep)
            ? cep
            : throw new FormatException($"CEP inválido: '{entrada}'.");

    /// <summary>A UF de um prefixo, ou nulo se ele cair fora de todas as faixas.</summary>
    public static string? UfDe(int prefixo)
    {
        foreach (var faixa in Faixas)
        {
            if (prefixo >= faixa.Inicio && prefixo <= faixa.Fim)
            {
                return faixa.Uf;
            }
        }

        return null;
    }

    /// <summary>A região de uma UF.</summary>
    public static Regiao RegiaoDe(string uf) => Regioes[uf];

    /// <summary>Indica se o CEP está dentro de uma faixa de prefixos.</summary>
    public bool Entre(int prefixoInicial, int prefixoFinal)
        => Prefixo >= prefixoInicial && Prefixo <= prefixoFinal;

    /// <summary>Devolve o CEP com a máscara usual.</summary>
    public string Formatado() => $"{Digitos[..5]}-{Digitos[5..]}";

    /// <inheritdoc />
    public override string ToString() => Digitos;

    private static string SomenteDigitos(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return string.Empty;
        }

        Span<char> destino = stackalloc char[valor.Length];
        var n = 0;

        foreach (var c in valor)
        {
            if (c is >= '0' and <= '9')
            {
                destino[n++] = c;
            }
        }

        return new string(destino[..n]);
    }
}
