namespace Frete.Core.Calendario;

/// <summary>
/// Os feriados nacionais de um ano, fixos e móveis.
/// </summary>
/// <remarks>
/// Os móveis dependem todos da Páscoa, que é o primeiro domingo depois da
/// primeira lua cheia após o equinócio de março. A conta que resolve isso é o
/// algoritmo de Meeus/Butcher, e ela parece um amontoado de restos de divisão
/// porque é exatamente isso: aritmética modular embutindo o ciclo de 19 anos
/// da lua e a correção gregoriana dos séculos.
/// </remarks>
public static class Feriados
{
    /// <summary>Data da Páscoa no ano informado.</summary>
    public static DateOnly Pascoa(int ano)
    {
        if (ano is < 1583 or > 4099)
        {
            throw new ArgumentOutOfRangeException(nameof(ano), "A fórmula vale para o calendário gregoriano.");
        }

        var a = ano % 19;
        var b = ano / 100;
        var c = ano % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var mes = (h + l - (7 * m) + 114) / 31;
        var dia = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateOnly(ano, mes, dia);
    }

    /// <summary>Terça-feira de carnaval: 47 dias antes da Páscoa.</summary>
    public static DateOnly Carnaval(int ano) => Pascoa(ano).AddDays(-47);

    /// <summary>Sexta-feira santa: dois dias antes da Páscoa.</summary>
    public static DateOnly SextaFeiraSanta(int ano) => Pascoa(ano).AddDays(-2);

    /// <summary>Corpus Christi: 60 dias depois da Páscoa.</summary>
    public static DateOnly CorpusChristi(int ano) => Pascoa(ano).AddDays(60);

    /// <summary>
    /// Todos os feriados nacionais do ano, com o nome de cada um.
    /// </summary>
    public static IReadOnlyDictionary<DateOnly, string> Do(int ano)
    {
        var feriados = new Dictionary<DateOnly, string>
        {
            [new DateOnly(ano, 1, 1)] = "Confraternização Universal",
            [new DateOnly(ano, 4, 21)] = "Tiradentes",
            [new DateOnly(ano, 5, 1)] = "Dia do Trabalho",
            [new DateOnly(ano, 9, 7)] = "Independência",
            [new DateOnly(ano, 10, 12)] = "Nossa Senhora Aparecida",
            [new DateOnly(ano, 11, 2)] = "Finados",
            [new DateOnly(ano, 11, 15)] = "Proclamação da República",
            [new DateOnly(ano, 12, 25)] = "Natal",
        };

        // Feriado nacional desde a Lei 14.759/2023.
        if (ano >= 2024)
        {
            feriados[new DateOnly(ano, 11, 20)] = "Consciência Negra";
        }

        feriados[Carnaval(ano)] = "Carnaval";
        feriados[SextaFeiraSanta(ano)] = "Sexta-feira Santa";
        feriados[CorpusChristi(ano)] = "Corpus Christi";

        return feriados;
    }

    /// <summary>Indica se a data é feriado nacional.</summary>
    public static bool EhFeriado(DateOnly data) => Do(data.Year).ContainsKey(data);

    /// <summary>O nome do feriado, ou nulo se não for feriado.</summary>
    public static string? Nome(DateOnly data) => Do(data.Year).GetValueOrDefault(data);
}
