using Frete.Core.Calendario;
using Frete.Core.Cotacao;
using Frete.Core.Cubagem;
using Frete.Core.Geografia;
using Frete.Core.Tabela;
using Microsoft.AspNetCore.Mvc;

var construtor = WebApplication.CreateBuilder(args);

construtor.Services.AddEndpointsApiExplorer();
construtor.Services.AddSwaggerGen();
construtor.Services.AddSingleton(_ => new Precificador());
construtor.Services.AddSingleton<IReadOnlyList<TabelaDeFrete>>(_ => Exemplos.Todas);

var aplicacao = construtor.Build();

if (aplicacao.Environment.IsDevelopment())
{
    aplicacao.UseSwagger();
    aplicacao.UseSwaggerUI();
}

aplicacao.MapGet("/saude", () => Results.Ok(new { estado = "ok" }))
    .WithName("Saude")
    .WithTags("Serviço");

aplicacao.MapGet("/ceps/{cep}", (string cep) =>
        Cep.TentarAnalisar(cep, out var lido)
            ? Results.Ok(new
            {
                cep = lido.Formatado(),
                uf = lido.Uf,
                regiao = lido.Regiao.ToString(),
                prefixo = lido.Prefixo,
            })
            : Problema("CEP inválido", "O CEP precisa ter 8 dígitos e cair em uma faixa conhecida."))
    .WithName("AbrirCep")
    .WithTags("Geografia");

aplicacao.MapGet("/rotas", (string origem, string destino) =>
    {
        if (!Cep.TentarAnalisar(origem, out var de) || !Cep.TentarAnalisar(destino, out var para))
        {
            return Problema("CEP inválido", "Confira a origem e o destino.");
        }

        var zona = Zonas.Entre(de, para);

        return Results.Ok(new
        {
            origem = new { cep = de.Formatado(), uf = de.Uf, regiao = de.Regiao.ToString() },
            destino = new { cep = para.Formatado(), uf = para.Uf, regiao = para.Regiao.ToString() },
            zona = zona.ToString(),
            descricao = Zonas.Descrever(zona),
        });
    })
    .WithName("Rota")
    .WithTags("Geografia");

aplicacao.MapGet("/transportadoras", (IReadOnlyList<TabelaDeFrete> tabelas) =>
        Results.Ok(tabelas.Select(tabela => new
        {
            tabela.Transportadora,
            zonas = tabela.Zonas.Keys.Select(zona => zona.ToString()),
            fatorDeCubagem = tabela.FatorDeCubagem,
            minimo = tabela.Minimo,
        })))
    .WithName("Transportadoras")
    .WithTags("Tabelas");

aplicacao.MapGet("/feriados/{ano:int}", (int ano) =>
    {
        if (ano is < 1583 or > 4099)
        {
            return Problema("Ano fora da faixa", "A fórmula da Páscoa vale para o calendário gregoriano.");
        }

        return Results.Ok(Feriados.Do(ano)
            .OrderBy(feriado => feriado.Key)
            .Select(feriado => new { data = feriado.Key.ToString("yyyy-MM-dd"), nome = feriado.Value }));
    })
    .WithName("Feriados")
    .WithTags("Calendário");

aplicacao.MapPost("/cotacoes", ([FromBody] PedidoEmJson pedido, Precificador precificador, IReadOnlyList<TabelaDeFrete> tabelas) =>
    {
        try
        {
            var cotacoes = precificador.Comparar(pedido.ParaNucleo(), tabelas);

            if (cotacoes.Count == 0)
            {
                return Problema("Sem cobertura", "Nenhuma transportadora atende essa rota.");
            }

            return Results.Ok(cotacoes.Select(Resposta.De));
        }
        catch (Exception erro) when (erro is FormatException or ArgumentException or InvalidOperationException)
        {
            return Problema("Pedido inválido", erro.Message);
        }
    })
    .WithName("Cotar")
    .WithTags("Cotação");

aplicacao.Run();

static IResult Problema(string titulo, string detalhe) => Results.BadRequest(new ProblemDetails
{
    Title = titulo,
    Detail = detalhe,
    Status = StatusCodes.Status400BadRequest,
});

/// <summary>O pedido como ele chega pela API.</summary>
/// <param name="Origem">CEP de origem.</param>
/// <param name="Destino">CEP de destino.</param>
/// <param name="Volumes">Os volumes da carga.</param>
/// <param name="ValorDaNota">Valor da mercadoria.</param>
/// <param name="Coleta">Data da coleta; hoje quando não informada.</param>
public sealed record PedidoEmJson(
    string Origem,
    string Destino,
    IReadOnlyList<VolumeEmJson> Volumes,
    decimal ValorDaNota = 0,
    DateOnly? Coleta = null)
{
    /// <summary>Converte para o modelo do núcleo.</summary>
    public PedidoDeCotacao ParaNucleo() => new()
    {
        Origem = Cep.Analisar(Origem),
        Destino = Cep.Analisar(Destino),
        ValorDaNota = ValorDaNota,
        Coleta = Coleta ?? DateOnly.FromDateTime(DateTime.Today),
        Volumes = (Volumes ?? []).Select(volume => volume.ParaNucleo()).ToList(),
    };
}

/// <summary>Um volume como ele chega pela API.</summary>
/// <param name="PesoEmKg">Peso em quilos.</param>
/// <param name="Comprimento">Comprimento em cm.</param>
/// <param name="Largura">Largura em cm.</param>
/// <param name="Altura">Altura em cm.</param>
/// <param name="Quantidade">Quantas peças iguais.</param>
public sealed record VolumeEmJson(
    decimal PesoEmKg,
    decimal Comprimento,
    decimal Largura,
    decimal Altura,
    int Quantidade = 1)
{
    /// <summary>Converte para o modelo do núcleo.</summary>
    public Volume ParaNucleo() => new()
    {
        PesoEmKg = PesoEmKg,
        Dimensoes = new Dimensoes(Comprimento, Largura, Altura),
        Quantidade = Quantidade,
    };
}

/// <summary>A cotação como a API devolve.</summary>
public sealed record Resposta(
    string Transportadora,
    string Zona,
    decimal PesoReal,
    decimal PesoCubado,
    decimal PesoTaxavel,
    bool CubagemMandou,
    IReadOnlyList<object> Parcelas,
    decimal Subtotal,
    decimal Icms,
    decimal Total,
    int PrazoEmDiasUteis,
    string Entrega,
    IReadOnlyList<string> Observacoes)
{
    /// <summary>Monta a resposta a partir da cotação.</summary>
    public static Resposta De(Cotacao cotacao) => new(
        cotacao.Transportadora,
        Zonas.Descrever(cotacao.Zona),
        cotacao.PesoReal,
        cotacao.PesoCubado,
        cotacao.PesoTaxavel,
        cotacao.CubagemMandou,
        cotacao.Parcelas.Select(parcela => (object)new { parcela.Item, parcela.Valor, parcela.Detalhe }).ToList(),
        cotacao.Subtotal,
        cotacao.Icms,
        cotacao.Total,
        cotacao.PrazoEmDiasUteis,
        cotacao.Entrega.ToString("yyyy-MM-dd"),
        cotacao.Observacoes);
}

/// <summary>Exposta para que os testes de integração possam subir a API.</summary>
public partial class Program;
