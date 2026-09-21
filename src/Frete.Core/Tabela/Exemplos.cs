using Frete.Core.Geografia;

namespace Frete.Core.Tabela;

/// <summary>
/// Tabelas de exemplo, para a API subir com algo pronto e para os testes terem
/// um cenário realista. Os valores são inventados: não são de transportadora
/// nenhuma de verdade.
/// </summary>
public static class Exemplos
{
    /// <summary>Transportadora barata e devagar.</summary>
    public static TabelaDeFrete Economica { get; } = new TabelaDeFrete
    {
        Transportadora = "Rodo Econômica",
        FatorDeCubagem = Cubagem.Volume.FatorRodoviario,
        AdValorem = 0.004m,
        Gris = 0.0008m,
        PedagioPor100Kg = 4.50m,
        Minimo = 28.00m,
        Icms = 0.12m,
        Zonas = new Dictionary<Zona, PrecoDaZona>
        {
            [Zona.MesmoEstado] = new()
            {
                PrazoEmDiasUteis = 3,
                Faixas =
                [
                    new FaixaDePeso { AteKg = 10, Preco = 26.00m },
                    new FaixaDePeso { AteKg = 30, Preco = 42.00m },
                    new FaixaDePeso { AteKg = 100, Preco = 88.00m },
                    new FaixaDePeso { Preco = 88.00m, PorKgExcedente = 0.82m },
                ],
            },
            [Zona.MesmaRegiao] = new()
            {
                PrazoEmDiasUteis = 5,
                Faixas =
                [
                    new FaixaDePeso { AteKg = 10, Preco = 38.00m },
                    new FaixaDePeso { AteKg = 30, Preco = 62.00m },
                    new FaixaDePeso { AteKg = 100, Preco = 128.00m },
                    new FaixaDePeso { Preco = 128.00m, PorKgExcedente = 1.15m },
                ],
            },
            [Zona.OutraRegiao] = new()
            {
                PrazoEmDiasUteis = 9,
                Faixas =
                [
                    new FaixaDePeso { AteKg = 10, Preco = 62.00m },
                    new FaixaDePeso { AteKg = 30, Preco = 104.00m },
                    new FaixaDePeso { AteKg = 100, Preco = 214.00m },
                    new FaixaDePeso { Preco = 214.00m, PorKgExcedente = 1.92m },
                ],
            },
        },
        AreasDiferenciadas =
        [
            // Interior do Amazonas e Roraima: entrega depende de balsa.
            new AreaDiferenciada(69400, 69899, 48.00m, 6, "acesso fluvial"),
            new AreaDiferenciada(69300, 69399, 55.00m, 8, "acesso restrito"),
        ],
    };

    /// <summary>Transportadora cara e rápida.</summary>
    public static TabelaDeFrete Expressa { get; } = new TabelaDeFrete
    {
        Transportadora = "Aero Expressa",
        FatorDeCubagem = Cubagem.Volume.FatorAereo,
        AdValorem = 0.008m,
        Gris = 0.0015m,
        PedagioPor100Kg = 0m,
        Minimo = 46.00m,
        Icms = 0.12m,
        Zonas = new Dictionary<Zona, PrecoDaZona>
        {
            [Zona.MesmoEstado] = new()
            {
                PrazoEmDiasUteis = 1,
                Faixas =
                [
                    new FaixaDePeso { AteKg = 10, Preco = 52.00m },
                    new FaixaDePeso { AteKg = 30, Preco = 96.00m },
                    new FaixaDePeso { Preco = 96.00m, PorKgExcedente = 2.60m },
                ],
            },
            [Zona.MesmaRegiao] = new()
            {
                PrazoEmDiasUteis = 2,
                Faixas =
                [
                    new FaixaDePeso { AteKg = 10, Preco = 74.00m },
                    new FaixaDePeso { AteKg = 30, Preco = 138.00m },
                    new FaixaDePeso { Preco = 138.00m, PorKgExcedente = 3.40m },
                ],
            },
            [Zona.OutraRegiao] = new()
            {
                PrazoEmDiasUteis = 3,
                Faixas =
                [
                    new FaixaDePeso { AteKg = 10, Preco = 118.00m },
                    new FaixaDePeso { AteKg = 30, Preco = 226.00m },
                    new FaixaDePeso { Preco = 226.00m, PorKgExcedente = 5.10m },
                ],
            },
        },
    };

    /// <summary>Transportadora que só roda dentro do estado.</summary>
    public static TabelaDeFrete Regional { get; } = new TabelaDeFrete
    {
        Transportadora = "Local Entregas",
        AdValorem = 0.003m,
        Gris = 0m,
        Minimo = 19.00m,
        Icms = 0.12m,
        Zonas = new Dictionary<Zona, PrecoDaZona>
        {
            [Zona.MesmoEstado] = new()
            {
                PrazoEmDiasUteis = 2,
                Faixas =
                [
                    new FaixaDePeso { AteKg = 5, Preco = 18.00m },
                    new FaixaDePeso { AteKg = 20, Preco = 31.00m },
                    new FaixaDePeso { Preco = 31.00m, PorKgExcedente = 1.40m },
                ],
            },
        },
    };

    /// <summary>As três tabelas de exemplo.</summary>
    public static IReadOnlyList<TabelaDeFrete> Todas { get; } = [Economica, Expressa, Regional];
}
