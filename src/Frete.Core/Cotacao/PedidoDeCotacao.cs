using Frete.Core.Cubagem;
using Frete.Core.Geografia;

namespace Frete.Core.Cotacao;

/// <summary>O que se quer transportar, de onde para onde.</summary>
public sealed record PedidoDeCotacao
{
    /// <summary>CEP de origem.</summary>
    public required Cep Origem { get; init; }

    /// <summary>CEP de destino.</summary>
    public required Cep Destino { get; init; }

    /// <summary>Os volumes da carga.</summary>
    public required IReadOnlyList<Volume> Volumes { get; init; }

    /// <summary>Valor da mercadoria, base do ad valorem e do GRIS.</summary>
    public decimal ValorDaNota { get; init; }

    /// <summary>Data da coleta, base para o prazo.</summary>
    public DateOnly Coleta { get; init; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>A zona tarifária da rota.</summary>
    public Zona Zona => Zonas.Entre(Origem, Destino);

    /// <summary>Monta um pedido a partir de CEPs em texto e um volume só.</summary>
    public static PedidoDeCotacao Simples(
        string origem,
        string destino,
        decimal pesoEmKg,
        decimal comprimento,
        decimal largura,
        decimal altura,
        decimal valorDaNota = 0,
        DateOnly? coleta = null) => new()
        {
            Origem = Cep.Analisar(origem),
            Destino = Cep.Analisar(destino),
            Volumes = [new Volume
            {
                PesoEmKg = pesoEmKg,
                Dimensoes = new Dimensoes(comprimento, largura, altura),
            }],
            ValorDaNota = valorDaNota,
            Coleta = coleta ?? DateOnly.FromDateTime(DateTime.Today),
        };

    /// <summary>Confere o pedido e devolve ele mesmo.</summary>
    public PedidoDeCotacao Validar()
    {
        if (Volumes.Count == 0)
            throw new ArgumentException("O pedido precisa de ao menos um volume.", nameof(Volumes));

        if (ValorDaNota < 0)
            throw new ArgumentOutOfRangeException(nameof(ValorDaNota), "O valor da nota não pode ser negativo.");

        foreach (var volume in Volumes)
        {
            volume.Validar();
        }

        return this;
    }
}
