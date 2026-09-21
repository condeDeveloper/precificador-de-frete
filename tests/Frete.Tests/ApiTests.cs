using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Frete.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> fabrica;

    public ApiTests(WebApplicationFactory<Program> fabrica)
    {
        this.fabrica = fabrica;
    }

    private static object Pedido(string destino = "04567000", decimal peso = 5, decimal valor = 0) => new
    {
        origem = "01310100",
        destino,
        valorDaNota = valor,
        coleta = "2026-09-21",
        volumes = new[] { new { pesoEmKg = peso, comprimento = 30, largura = 20, altura = 20, quantidade = 1 } },
    };

    [Fact]
    public async Task Responde_a_verificacao_de_saude()
    {
        (await fabrica.CreateClient().GetAsync("/saude")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Abre_o_cep()
    {
        var corpo = await fabrica.CreateClient().GetFromJsonAsync<JsonElement>("/ceps/01310100");

        corpo.GetProperty("uf").GetString().Should().Be("SP");
        corpo.GetProperty("regiao").GetString().Should().Be("Sudeste");
    }

    [Fact]
    public async Task Cep_invalido_devolve_quatrocentos()
    {
        (await fabrica.CreateClient().GetAsync("/ceps/00000000")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Descreve_a_rota()
    {
        var corpo = await fabrica.CreateClient()
            .GetFromJsonAsync<JsonElement>("/rotas?origem=01310100&destino=69005040");

        corpo.GetProperty("descricao").GetString().Should().Be("outra região");
    }

    [Fact]
    public async Task Rota_com_cep_invalido_reclama()
    {
        var resposta = await fabrica.CreateClient().GetAsync("/rotas?origem=abc&destino=01310100");

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Lista_as_transportadoras()
    {
        var corpo = await fabrica.CreateClient().GetFromJsonAsync<JsonElement>("/transportadoras");

        corpo.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Lista_os_feriados_do_ano()
    {
        var corpo = await fabrica.CreateClient().GetFromJsonAsync<JsonElement>("/feriados/2026");

        corpo.GetArrayLength().Should().Be(12);

        var datas = corpo.EnumerateArray().Select(feriado => feriado.GetProperty("data").GetString()).ToList();

        // 03/04 é a Sexta-feira Santa; o domingo de Páscoa em si não entra,
        // porque não é feriado nacional.
        datas.Should().Contain("2026-04-03").And.Contain("2026-12-25");
        datas.Should().NotContain("2026-04-05");
    }

    [Fact]
    public async Task Ano_fora_da_faixa_reclama()
    {
        (await fabrica.CreateClient().GetAsync("/feriados/1000")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cota_e_devolve_as_opcoes_ordenadas()
    {
        var resposta = await fabrica.CreateClient().PostAsJsonAsync("/cotacoes", Pedido(valor: 500));
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.GetArrayLength().Should().Be(3);

        var totais = corpo.EnumerateArray().Select(cotacao => cotacao.GetProperty("total").GetDecimal()).ToList();
        totais.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task A_cotacao_vem_com_as_parcelas_abertas()
    {
        var resposta = await fabrica.CreateClient().PostAsJsonAsync("/cotacoes", Pedido(valor: 10_000));
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        var primeira = corpo[0];
        primeira.GetProperty("parcelas").GetArrayLength().Should().BeGreaterThan(1);
        primeira.GetProperty("icms").GetDecimal().Should().BeGreaterThan(0);
        primeira.GetProperty("entrega").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Cep_invalido_no_pedido_reclama()
    {
        var resposta = await fabrica.CreateClient().PostAsJsonAsync("/cotacoes", Pedido(destino: "00000000"));

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Pedido_sem_volume_reclama()
    {
        var resposta = await fabrica.CreateClient().PostAsJsonAsync("/cotacoes", new
        {
            origem = "01310100",
            destino = "04567000",
            volumes = Array.Empty<object>(),
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
