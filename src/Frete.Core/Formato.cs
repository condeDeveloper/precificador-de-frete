using System.Globalization;

namespace Frete.Core;

/// <summary>
/// Formatação de número no padrão brasileiro, independente da máquina.
/// </summary>
/// <remarks>
/// Deixar o <c>ToString</c> usar a cultura corrente parece inofensivo até o
/// código sair do notebook: no Windows em português sai "1.234,56" e num
/// servidor Linux sai "1,234.56". Uma cotação de frete em reais impressa com
/// separador americano é defeito, mesmo que o total esteja certo. O formato é
/// montado à mão em vez de pedir a cultura pt-BR porque ela não existe quando
/// a aplicação roda com globalização invariante.
/// </remarks>
public static class Formato
{
    /// <summary>Separador de milhar com ponto e decimal com vírgula.</summary>
    public static NumberFormatInfo Brasileiro { get; } = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = [3],
        NumberNegativePattern = 1,
        PercentDecimalSeparator = ",",
        PercentGroupSeparator = ".",
        PercentPositivePattern = 1,
        PercentNegativePattern = 1,
    };

    /// <summary>Um valor em reais, com duas casas.</summary>
    public static string Moeda(decimal valor) => valor.ToString("N2", Brasileiro);

    /// <summary>Um número com a quantidade de casas informada.</summary>
    public static string Numero(decimal valor, int casas = 2) => valor.ToString($"N{casas}", Brasileiro);

    /// <summary>Um peso, com até três casas e sem zeros à toa no fim.</summary>
    public static string Peso(decimal valor)
        => valor.ToString("0.###", Brasileiro);

    /// <summary>Uma razão entre zero e um mostrada como percentual.</summary>
    public static string Razao(decimal razao, int casas = 2) => $"{Numero(razao * 100, casas)}%";
}
