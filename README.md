# precificador-de-frete

Motor de cotação de frete rodoviário em C# e .NET 8. Zona tarifária deduzida do
CEP, peso cubado, tabela por faixa de peso, acessórios, ICMS por dentro e prazo
em dias úteis com feriados móveis calculados.

```csharp
var precificador = new Precificador();
var pedido = PedidoDeCotacao.Simples("01310100", "69450000", pesoEmKg: 3,
                                     comprimento: 100, largura: 80, altura: 60,
                                     valorDaNota: 2_500);

foreach (var cotacao in precificador.Comparar(pedido, Exemplos.Todas))
{
    Console.WriteLine(cotacao.Detalhar());
}
```

```
Rodo Econômica — outra região
  peso real 3 kg, cubado 144 kg, taxável 144 kg
  frete peso: 298,48 (acima: 214,00 + 1,92/kg)
  ad valorem: 10,00 (0,40% de 2500,00)
  GRIS: 2,00 (0,08% de 2500,00)
  pedágio: 9,00 (2 × 4,50)
  taxa de área: 48,00 (acesso fluvial)
  ICMS: 50,11
  TOTAL: 417,59 — entrega em 13/10/2026 (15 dias úteis)
  * 69450-000 é acesso fluvial: +6 dia(s) útil(eis).
  * A cubagem mandou no preço: 144 kg contra 3 kg reais.
```

## Por que existe

Frete é uma daquelas contas que parecem uma multiplicação e são uma cascata de
regras. Quatro delas costumam ser implementadas errado:

**1. O peso que vale não é o peso.** Uma caixa de travesseiros custa o mesmo
que uma de parafusos porque ocupa o mesmo espaço no caminhão. Por isso existe o
peso cubado — o volume convertido em quilos por um fator, 300 kg/m³ no
rodoviário e 167 no aéreo — e o que entra na tabela é o **maior** entre ele e o
peso real. A comparação é feita no total da carga, não volume a volume.

**2. O ICMS do frete é "por dentro".** A alíquota incide sobre o total *já com
o imposto*, então com base de R$ 100 e 12% o valor final não é R$ 112, é
`100 / 0,88` = **R$ 113,64**. Errar isso é subfaturar todo conhecimento
emitido.

**3. Prazo é em dias úteis.** Cinco dias a partir de uma quinta caem na quinta
seguinte, não no domingo — e ainda precisam desviar dos feriados.

**4. Os feriados móveis dependem da Páscoa.** Carnaval é 47 dias antes,
Sexta-feira Santa 2 dias antes, Corpus Christi 60 dias depois. E a Páscoa sai
do algoritmo de Meeus/Butcher, um amontoado de restos de divisão que embute o
ciclo de 19 anos da lua e a correção gregoriana dos séculos.

## O que ele faz

| Etapa | Detalhe |
| --- | --- |
| **CEP → UF** | 31 faixas de prefixo dos Correios, incluindo as que se intercalam (DF e GO) e os estados com mais de uma faixa (AM, RO) |
| **Zona** | mesmo estado, mesma região ou outra região |
| **Peso taxável** | `max(peso real, volume × fator)` no total da carga |
| **Faixa de peso** | tabela por zona, terminando em faixa aberta com preço por quilo excedente |
| **Ad valorem e GRIS** | percentuais sobre o valor da nota |
| **Pedágio** | por fração de 100 kg — 101 kg pagam duas |
| **Área diferenciada** | faixas de CEP com taxa e prazo extras |
| **Mínimo** | complementa o frete barato, com a parcela aparecendo no detalhamento |
| **ICMS** | por dentro |
| **Prazo** | dias úteis, com feriados nacionais e locais |

O preço nunca sai só como total: cada parcela vem nomeada e com a conta que a
gerou. Entregar só o número é o que faz o cliente desconfiar do frete.

## Comparando transportadoras

```csharp
precificador.Comparar(pedido, Exemplos.Todas);   // ordenado por preço
precificador.MaisBarata(pedido, Exemplos.Todas);
precificador.MaisRapida(pedido, Exemplos.Todas);
```

Quem não atende a rota fica de fora da comparação em vez de derrubar a cotação
inteira. As três tabelas de exemplo são inventadas — não são de transportadora
nenhuma de verdade — e servem para a API subir pronta e para os testes terem um
cenário realista.

## Estrutura

```
Geografia/Cep.cs           faixas de prefixo → UF → região
Geografia/Zona.cs          a zona tarifária de uma rota
Cubagem/Volume.cs          dimensões, peso cubado e peso taxável da carga
Calendario/Feriados.cs     Meeus/Butcher e os feriados nacionais
Calendario/DiasUteis.cs    soma e contagem de dias úteis
Tabela/FaixaDePeso.cs      uma linha da tabela de preços
Tabela/TabelaDeFrete.cs    a tabela de uma transportadora
Tabela/Exemplos.cs         três tabelas fictícias
Cotacao/PedidoDeCotacao.cs o que se quer transportar
Cotacao/Cotacao.cs         o resultado, parcela a parcela
Cotacao/Precificador.cs    a cascata de regras
```

## API

```bash
dotnet run --project src/Frete.Api
```

| Método | Rota | O que faz |
| --- | --- | --- |
| `GET` | `/saude` | verificação de disponibilidade |
| `GET` | `/ceps/{cep}` | abre o CEP em UF, região e prefixo |
| `GET` | `/rotas?origem=&destino=` | a zona tarifária entre dois CEPs |
| `GET` | `/transportadoras` | as tabelas carregadas |
| `GET` | `/feriados/{ano}` | os feriados nacionais, com os móveis calculados |
| `POST` | `/cotacoes` | cota em todas as tabelas, da mais barata para a mais cara |

## Rodando

```bash
dotnet test
```

132 testes. O núcleo não depende de nada além da BCL.

## Limites conhecidos

- **Não distingue capital de interior por CEP.** Cada transportadora tem a sua
  lista de área diferenciada, e é assim que ela é configurada aqui — em vez de
  eu inventar uma tabela de faixas metropolitanas.
- Não consulta serviço de CEP: a UF sai das faixas, o resto não é verificado.
- Só feriados nacionais vêm prontos; estaduais e municipais entram como lista.
- Sem ICMS por UF de origem e destino (a alíquota é uma só por tabela), e sem
  substituição tributária.
- Sem restrição de dimensão máxima por veículo.

## Licença

MIT.
