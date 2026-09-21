namespace Frete.Core.Calendario;

/// <summary>
/// Contagem de dias úteis, que é a unidade em que prazo de entrega é vendido.
/// </summary>
/// <remarks>
/// Prometer "cinco dias" e entregar cinco dias corridos é a origem de metade
/// das reclamações de prazo: cinco dias úteis a partir de uma quinta-feira
/// caem na quinta seguinte, não no domingo.
/// </remarks>
public sealed class DiasUteis
{
    private readonly HashSet<DateOnly> feriadosExtras;

    /// <summary>Cria a contagem, opcionalmente com feriados locais.</summary>
    public DiasUteis(IEnumerable<DateOnly>? feriadosLocais = null)
    {
        feriadosExtras = feriadosLocais?.ToHashSet() ?? [];
    }

    /// <summary>Instância só com os feriados nacionais.</summary>
    public static DiasUteis Nacional { get; } = new();

    /// <summary>Indica se a data é dia útil.</summary>
    public bool EhUtil(DateOnly data)
    {
        if (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        return !Feriados.EhFeriado(data) && !feriadosExtras.Contains(data);
    }

    /// <summary>O próximo dia útil, ou a própria data se ela já for útil.</summary>
    public DateOnly ProximoUtil(DateOnly data)
    {
        var atual = data;

        while (!EhUtil(atual))
        {
            atual = atual.AddDays(1);
        }

        return atual;
    }

    /// <summary>
    /// Soma dias úteis a uma data. Somar zero devolve o próximo dia útil, que
    /// é o comportamento certo para uma coleta feita no sábado.
    /// </summary>
    public DateOnly Somar(DateOnly inicio, int dias)
    {
        if (dias < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dias), "O prazo não pode ser negativo.");
        }

        var atual = ProximoUtil(inicio);

        for (var contados = 0; contados < dias; contados++)
        {
            atual = ProximoUtil(atual.AddDays(1));
        }

        return atual;
    }

    /// <summary>Quantos dias úteis existem entre duas datas, sem contar a inicial.</summary>
    public int Entre(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio)
        {
            throw new ArgumentOutOfRangeException(nameof(fim), "A data final não pode ser anterior à inicial.");
        }

        var contados = 0;

        for (var atual = inicio.AddDays(1); atual <= fim; atual = atual.AddDays(1))
        {
            if (EhUtil(atual))
            {
                contados++;
            }
        }

        return contados;
    }

    /// <summary>Os feriados locais configurados.</summary>
    public IReadOnlyCollection<DateOnly> FeriadosLocais => feriadosExtras;
}
