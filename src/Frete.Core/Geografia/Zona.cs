namespace Frete.Core.Geografia;

/// <summary>
/// A zona tarifária de uma rota. É o que a tabela da transportadora usa para
/// decidir o preço: quanto mais longe o destino, mais cara a faixa.
/// </summary>
public enum Zona
{
    /// <summary>Origem e destino no mesmo estado.</summary>
    MesmoEstado,

    /// <summary>Estados diferentes, mesma região do país.</summary>
    MesmaRegiao,

    /// <summary>Regiões diferentes.</summary>
    OutraRegiao,
}

/// <summary>Descobre a zona de uma rota.</summary>
public static class Zonas
{
    /// <summary>A zona entre dois CEPs.</summary>
    public static Zona Entre(Cep origem, Cep destino)
    {
        if (string.Equals(origem.Uf, destino.Uf, StringComparison.Ordinal))
        {
            return Zona.MesmoEstado;
        }

        return origem.Regiao == destino.Regiao ? Zona.MesmaRegiao : Zona.OutraRegiao;
    }

    /// <summary>Descrição legível da zona, para o detalhamento da cotação.</summary>
    public static string Descrever(Zona zona) => zona switch
    {
        Zona.MesmoEstado => "mesmo estado",
        Zona.MesmaRegiao => "mesma região",
        _ => "outra região",
    };
}
