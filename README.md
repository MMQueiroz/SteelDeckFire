# SteelDeck Fire

Verificação de painéis de laje mista (steel deck) em situação de incêndio considerando a **ação de membrana tracionada**, pelo método simplificado de Bailey na forma implementada no FRACOF / MACS+.

## Estrutura

```
SteelDeckFire.sln
src/
  SteelDeckFire.Core/          biblioteca net8.0, sem dependências externas
    Models/ProjectInput.cs     todos os dados de entrada (com atributos para o PropertyGrid)
    Models/CatalogItems.cs     fôrma, tela, perfil
    Data/Catalogs.cs           Metform MF-50/MF-75, telas Gerdau série Q, perfis Gerdau W e HP e IPE 400
    Fire/FireTables.cs         ISO 834, temperaturas na laje, k_y, k_u, k_s, c_a, h_eff mínimo
    Fire/SteelHeating.cs       aquecimento de perfil sem proteção (método incremental)
    Calc/SectionProperties.cs  área e módulo plástico do perfil I pela geometria (com concordâncias)
    Calc/FireDesign.cs         método de Bailey, vigas internas, vigas de perímetro, verificações
    Calc/Results.cs            todos os valores intermediários
    Report/ReportDocument.cs   memorial como blocos independentes de formato (títulos, equações, tabelas, figuras)
    Report/ReportBuilder.cs    memorial passo a passo (fórmula → substituição → resultado)
  SteelDeckFire.App/           WinForms net8.0-windows
    MainForm.cs                lógica da janela: cálculo, arquivos, memorial, impressão
    MainForm.Designer.cs       layout da janela (editável no designer do Visual Studio)
    Views/PlanView.cs          planta com linhas de ruptura e forças de membrana
    Views/SectionView.cs       seção da laje com gradiente térmico e temperaturas
    Views/TimeChartView.cs     resistência × tempo
    Views/ResultStrip.cs       veredito e números principais
    Views/ReportLayout.cs      paginação A4 e desenho do memorial (GDI+), igual na tela e na impressão
    Views/ReportView.cs        visualização paginada do memorial
    Views/RichText.cs          texto com subscrito/itálico e quebra de linha
tests/
  SteelDeckFire.Validation/    reproduz o exemplo resolvido do FRACOF Design Guide
```

## Compilar e executar (Windows)

```
dotnet build SteelDeckFire.sln -c Release
dotnet run --project src/SteelDeckFire.App
dotnet run --project tests/SteelDeckFire.Validation
```

Requer .NET 8 SDK. Não há pacotes NuGet.

## Uso

1. Preencha os dados no painel à esquerda. Os cálculos são refeitos automaticamente.
2. Fôrma, tela e perfil podem ser escolhidos do catálogo ou definidos como "Personalizada".
3. Abas:
   - **Planta e linhas de ruptura**: charneiras com espessura proporcional à força de membrana (azul = compressão, vermelho = tração), zona tracionada, anel comprimido e fissura central.
   - **Seção da laje**: posição da tela, h_eff e temperaturas θ1, θs, θ2.
   - **Resistência × tempo**: q_fi,Rd de 30 a 180 min contra q_fi,Sd.
   - **Verificações**: capacidade, isolamento, ductilidade, escopo, modo de ruptura.
   - **Memorial de cálculo**: passo a passo com figuras, paginado em A4 (Ctrl + roda do mouse para zoom, Ctrl + 0 para ajustar à largura).
     Use "Exportar memorial em PDF…" (via Microsoft Print to PDF), "Visualizar impressão" ou "Imprimir…".
4. "Carregar exemplo FRACOF" preenche o exemplo do guia (zona B, 9 × 12 m, R60) para conferência.

## Convenções

- **L1** = vão das vigas internas sem proteção; **L2** = dimensão perpendicular.
- As vigas internas são paralelas a L1 e igualmente espaçadas em L2/(n+1).
- **L** e **l** do método = maior e menor entre L1 e L2 (feito internamente).
- **d** = distância do eixo da tela à face superior da laje.

## Validação

O projeto `SteelDeckFire.Validation` compara 30 grandezas com o exemplo resolvido do FRACOF Design Guide:

- Os coeficientes A, B, C e D coincidem com o guia.
- b, e, q_laje, temperatura da viga, M_fi,Rd e q_fi,Rd ficam dentro de 0,5 %.

A única diferença é θ1: o guia lê 77 °C e a interpolação da própria tabela em x = h_eff dá ≈ 72 °C, com efeito desprezível no resultado.

## Pontos a conferir antes do uso em projeto

- **Geometria das nervuras Metform (l1, l2, l3)**: os valores são aproximados. Altura e largura útil são as nominais; confira as demais com o catálogo ou manual técnico vigente, ou use "Personalizada".
- **Perfis W e HP**: o catálogo traz as bitolas Gerdau (d, bf, tw, tf, r); A e Zx são calculados pela geometria, incluindo as concordâncias alma–mesa. Confira as dimensões com a tabela vigente do fabricante. Ao editar qualquer dimensão, o perfil passa a "Personalizada".
- **Ductilidade da tela**: o método foi calibrado com telas classe B ou C (EN 10080). Telas CA-60 trefiladas usuais têm alongamento baixo, e o programa emite alerta.
- **Combinação de ações**: γg e ψ são entradas. O padrão é NBR 14323 (0,7·ψ2); confira para o caso.
- **Vigas internas com interação parcial**: o momento é interpolado linearmente entre o perfil isolado e a interação completa, simplificação a favor da segurança.
- **Vigas de perímetro**: expressões do FRACOF com a largura efetiva descontada dos dois lados. Os esforços servem para definir a temperatura crítica e a proteção.
- **Aplicação no Brasil**: o método não é normatizado pela NBR 14323 e requer justificativa como método avançado perante o Corpo de Bombeiros.

## Referências

- Bailey, C. G.; Moore, D. B. *The structural behaviour of steel frames with composite floor slabs subject to fire*, Parts 1 e 2. The Structural Engineer, 2000.
- Bailey, C. G. *Membrane action of slab/beam composite floor systems in fire*. Engineering Structures 26, 2004.
- Vassart, O.; Zhao, B. *FRACOF – Design Guide* e *Engineering Background*, 2011.
- EN 1991-1-2, EN 1992-1-2, EN 1993-1-2, EN 1994-1-2; ABNT NBR 14323, NBR 14432, NBR 8681.
