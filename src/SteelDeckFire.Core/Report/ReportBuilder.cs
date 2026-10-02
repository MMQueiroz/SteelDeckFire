using System.Globalization;
using System.Net;
using System.Text;
using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Models;

namespace SteelDeckFire.Core.Report;

/// <summary>Imagens opcionais (PNG em base64) geradas pela interface para inserir no memorial.</summary>
public sealed record ReportImages(string? PlanPng, string? SectionPng, string? ChartPng);

/// <summary>Monta o memorial de cálculo em HTML, com cada equação em forma literal, substituída e resultado.</summary>
public sealed class ReportBuilder
{
    private static readonly CultureInfo Br = new("pt-BR");
    private readonly StringBuilder _sb = new();
    private int _step;

    private static string F(double v, int dec = 2) => v.ToString("N" + dec, Br);
    private static string H(string s) => WebUtility.HtmlEncode(s);

    public static string Build(ProjectInput inp, DesignResult r, IReadOnlyList<TimePoint>? sweep, ReportImages? img)
        => new ReportBuilder().BuildInternal(inp, r, sweep, img);

    private string BuildInternal(ProjectInput inp, DesignResult r, IReadOnlyList<TimePoint>? sweep, ReportImages? img)
    {
        var th = r.Thermal; var m = r.Membrane; var bm = r.Beams; var ld = r.Load;
        double t = inp.FireTime;

        Head(inp);
        Summary(inp, r);

        // ------------------------------------------------------------ Dados
        Section("Dados de entrada");
        Table(new[] { "Grupo", "Parâmetro", "Valor" }, new[]
        {
            new[] { "Painel", "L1 (vão das vigas internas sem proteção)", $"{F(inp.L1)} m" },
            new[] { "Painel", "L2 (dimensão perpendicular)", $"{F(inp.L2)} m" },
            new[] { "Painel", "Vigas internas sem proteção", $"{inp.UnprotectedBeams} × {H(inp.SectionName)}, espaçamento {F(bm.Spacing / 1000, 3)} m" },
            new[] { "Laje", "Telha-fôrma", $"{H(inp.DeckName)}, t = {F(inp.DeckThickness)} mm" },
            new[] { "Laje", "Geometria da nervura (h2 / l1 / l2 / l3)", $"{F(inp.DeckH2, 0)} / {F(inp.DeckL1, 0)} / {F(inp.DeckL2, 0)} / {F(inp.DeckL3, 0)} mm" },
            new[] { "Laje", "Altura total h_t / concreto acima da fôrma h1", $"{F(inp.SlabThickness, 0)} / {F(th.H1, 0)} mm" },
            new[] { "Laje", "Concreto", $"f_ck = {F(inp.Fck, 0)} MPa" },
            new[] { "Tela", "Tipo", H(inp.MeshName) },
            new[] { "Tela", "A_s paralela a L2 / paralela a L1", $"{F(inp.AsAlongL2, 0)} / {F(inp.AsAlongL1, 0)} mm²/m" },
            new[] { "Tela", "f_y / d (eixo à face superior)", $"{F(inp.MeshFy, 0)} MPa / {F(inp.MeshDepth, 0)} mm" },
            new[] { "Vigas", "Perfil (d × bf × tw × tf)", $"{F(inp.BeamH, 0)} × {F(inp.BeamB, 0)} × {F(inp.BeamTw, 1)} × {F(inp.BeamTf, 1)} mm" },
            new[] { "Vigas", "A / Z_x / f_y / grau de interação", $"{F(inp.BeamArea, 1)} cm² / {F(inp.BeamZx, 1)} cm³ / {F(inp.BeamFy, 0)} MPa / {F(inp.ShearConnection, 2)}" },
            new[] { "Incêndio", "TRRF – incêndio-padrão ISO 834", $"{F(t, 0)} min" },
        });
        if (img?.PlanPng is not null) Figure(img.PlanPng, "Planta do painel com o padrão de linhas de ruptura e a distribuição das forças de membrana nas charneiras.");
        if (img?.SectionPng is not null) Figure(img.SectionPng, "Seção transversal da laje com a posição da tela e as temperaturas de cálculo.");

        // ------------------------------------------------------------ 1. Ações
        Step("Ações em situação de incêndio", "Combinação última excepcional (NBR 8681 / NBR 14323 ou EN 1990).");
        if (ld.SlabWeightComputed)
        {
            Eq("t<sub>eq</sub> = h<sub>1</sub> + h<sub>2</sub>(l<sub>1</sub> + l<sub>2</sub>) / [2(l<sub>1</sub> + l<sub>3</sub>)]",
               $"{F(th.H1, 0)} + {F(inp.DeckH2, 0)}·({F(inp.DeckL1, 0)} + {F(inp.DeckL2, 0)}) / [2·({F(inp.DeckL1, 0)} + {F(inp.DeckL3, 0)})]",
               F(ld.ConcreteEqThickness, 1), "mm", "espessura média de concreto");
            Eq("g<sub>laje</sub> = γ<sub>c</sub>·t<sub>eq</sub> + g<sub>fôrma</sub>",
               $"{F(inp.ConcreteWeight, 0)} × {F(ld.ConcreteEqThickness / 1000, 4)} + {F(ld.DeckWeight, 3)}",
               F(ld.SlabSelfWeight, 2), "kN/m²", "peso da fôrma estimado");
        }
        else Note($"Peso próprio da laje informado: g<sub>laje</sub> = {F(ld.SlabSelfWeight)} kN/m².");
        Eq("G = g<sub>laje</sub> + g<sub>adic</sub> + g<sub>vigas</sub>",
           $"{F(ld.SlabSelfWeight)} + {F(inp.AdditionalDead)} + {F(inp.BeamsSelfWeight)}", F(ld.PermanentTotal), "kN/m²");
        Eq("q<sub>fi,Sd</sub> = γ<sub>g</sub>·G + ψ·Q",
           $"{F(inp.GammaG)} × {F(ld.PermanentTotal)} + {F(inp.PsiFire)} × {F(inp.LiveLoad)}", F(ld.QfiSd), "kN/m²");

        // ------------------------------------------------------------ 2. Espessura efetiva
        Step("Espessura efetiva e isolamento térmico", "EN 1994-1-2, Anexo D (D.4) e Tabela D.6.");
        Note($"h<sub>2</sub>/h<sub>1</sub> = {F(th.H2 / th.H1)} {(th.HeffFormulaA ? "≤ 1,5 e h<sub>1</sub> > 40 mm" : "> 1,5 ou h<sub>1</sub> ≤ 40 mm")}.");
        if (th.HeffFormulaA)
            Eq("h<sub>eff</sub> = h<sub>1</sub> + 0,5·h<sub>2</sub>·(l<sub>1</sub> + l<sub>2</sub>)/(l<sub>1</sub> + l<sub>3</sub>)",
               $"{F(th.H1, 0)} + 0,5 × {F(th.H2, 0)} × ({F(inp.DeckL1, 0)} + {F(inp.DeckL2, 0)})/({F(inp.DeckL1, 0)} + {F(inp.DeckL3, 0)})",
               F(th.Heff, 1), "mm");
        else
            Eq("h<sub>eff</sub> = h<sub>1</sub>·[1 + 0,75·(l<sub>1</sub> + l<sub>2</sub>)/(l<sub>1</sub> + l<sub>3</sub>)]",
               $"{F(th.H1, 0)} × [1 + 0,75 × ({F(inp.DeckL1, 0)} + {F(inp.DeckL2, 0)})/({F(inp.DeckL1, 0)} + {F(inp.DeckL3, 0)})]",
               F(th.Heff, 1), "mm");
        Verdict(th.Heff >= th.HeffMin, $"h<sub>eff</sub> = {F(th.Heff, 1)} mm {(th.Heff >= th.HeffMin ? "≥" : "<")} h<sub>eff,mín</sub> = {F(th.HeffMin, 0)} mm para {F(t, 0)} min (critério I).");

        // ------------------------------------------------------------ 3. Temperaturas
        Step("Temperaturas na laje", "Perfis de temperatura para incêndio-padrão (guia FRACOF, Tab. 3-1, a partir da EN 1992-1-2), interpolados em x e t.");
        Table(new[] { "Ponto", "Posição x (a partir da face exposta)", "Temperatura" }, new[]
        {
            new[] { "θ<sub>2</sub> – face exposta", "2,5 mm", $"{F(th.Theta2, 0)} °C" },
            new[] { "θ<sub>1</sub> – face não exposta", $"h<sub>eff</sub> = {F(th.HeffUsed, 1)} mm", $"{F(th.Theta1, 0)} °C" },
            new[] { "θ<sub>s</sub> – tela", $"h<sub>eff</sub> − d = {F(th.XMesh, 1)} mm", $"{F(th.ThetaS, 0)} °C" },
        });
        Eq("f<sub>sy,θ</sub> = k<sub>s,θ</sub>·f<sub>sy</sub>", $"{F(th.Ks, 3)} × {F(inp.MeshFy, 0)}", F(th.FsyTheta, 1), "MPa",
           inp.MeshColdWorked ? "fio trefilado – EN 1992-1-2 Tab. 3.2a" : "barra laminada – EN 1992-1-2 Tab. 3.2a");

        // ------------------------------------------------------------ 4. Momentos da tela
        string longName = m.LongIsL2 ? "L2" : "L1";
        string shortName = m.LongIsL2 ? "L1" : "L2";
        Step("Momentos resistentes da tela", $"L = {F(m.Lmm / 1000)} m ({longName}), l = {F(m.lmm / 1000)} m ({shortName}). A fôrma é desprezada.");
        Eq("A<sub>s</sub> (barras paralelas a L)", "", F(m.As, 4), "mm²/mm");
        Eq("K = A<sub>s,l</sub> / A<sub>s,L</sub>", "", F(m.K, 3), "");
        Eq("g<sub>01</sub> = 1 − 2·K·A<sub>s</sub>·f<sub>sy,θ</sub> / (0,85·f<sub>c</sub>·d)",
           $"1 − 2 × {F(m.K, 3)} × {F(m.As, 4)} × {F(th.FsyTheta, 1)} / (0,85 × {F(inp.Fck, 0)} × {F(inp.MeshDepth, 0)})", F(m.G01, 3), "");
        Eq("g<sub>02</sub> = 1 − 2·A<sub>s</sub>·f<sub>sy,θ</sub> / (0,85·f<sub>c</sub>·d)",
           $"1 − 2 × {F(m.As, 4)} × {F(th.FsyTheta, 1)} / (0,85 × {F(inp.Fck, 0)} × {F(inp.MeshDepth, 0)})", F(m.G02, 3), "");
        Eq("M<sub>0</sub> = A<sub>s</sub>·f<sub>sy,θ</sub>·d·(3 + g<sub>02</sub>)/4",
           $"{F(m.As, 4)} × {F(th.FsyTheta, 1)} × {F(inp.MeshDepth, 0)} × (3 + {F(m.G02, 3)})/4", F(m.M0, 1), "N·mm/mm");
        Eq("μ = K·(3 + g<sub>01</sub>)/(3 + g<sub>02</sub>)",
           $"{F(m.K, 3)} × (3 + {F(m.G01, 3)})/(3 + {F(m.G02, 3)})", F(m.Mu, 3), "");

        // ------------------------------------------------------------ 5. Charneiras
        Step("Carga de colapso por charneiras plásticas", "Padrão de charneiras de laje simplesmente apoiada nos quatro lados (sem membrana).");
        Eq("a = L / l", $"{F(m.Lmm, 0)} / {F(m.lmm, 0)}", F(m.A, 3), "");
        Eq("n = [√(3μa² + 1) − 1] / (2μa²)",
           $"[√(3 × {F(m.Mu, 3)} × {F(m.A, 3)}² + 1) − 1] / (2 × {F(m.Mu, 3)} × {F(m.A, 3)}²)", F(m.N, 4), "",
           $"cruzamento das charneiras a n·L = {F(m.N * m.Lmm / 1000, 3)} m dos lados menores");
        Eq("p<sub>fi</sub> = 6·M<sub>0</sub> / (n²·L²)",
           $"6 × {F(m.M0, 1)} / ({F(m.N, 4)}² × {F(m.Lmm, 0)}²) × 10³", F(m.Pfi, 3), "kN/m²");

        // ------------------------------------------------------------ 6. Deslocamento
        Step("Deslocamento de cálculo", "Curvatura térmica + alongamento da tela, calibrados com os ensaios de Cardington.");
        Eq("w<sub>θ</sub> = α·(θ<sub>2</sub> − θ<sub>1</sub>)·l² / (19,2·h<sub>eff</sub>)",
           $"{inp.ThermalAlpha.ToString("0.0E+0", Br)} × ({F(th.Theta2, 0)} − {F(th.Theta1, 0)}) × {F(m.lmm, 0)}² / (19,2 × {F(th.HeffUsed, 1)})",
           F(m.WThermal, 1), "mm");
        Eq("w<sub>mec</sub> = √(0,5·f<sub>sy</sub>/E<sub>s</sub> · 3L²/8) ≤ l/30",
           $"min[√(0,5 × {F(inp.MeshFy, 0)}/{F(inp.MeshE, 0)} × 3 × {F(m.Lmm, 0)}²/8) = {F(m.WMechRaw, 1)} ; {F(m.lmm / 30, 1)}]",
           F(m.WMech, 1), "mm");
        Eq("w = min[w<sub>θ</sub> + w<sub>mec</sub> ; (L + l)/30]",
           $"min[{F(m.WThermal + m.WMech, 1)} ; {F(m.WLimitTotal, 1)}]", F(m.W, 1), "mm");

        // ------------------------------------------------------------ 7. Parâmetros de membrana
        Step("Parâmetros das forças de membrana",
             "k: relação compressão no canto / tração no fim da diagonal. A, B, C, D: momentos no plano, em torno da borda, das forças no meio elemento 1 (tração e compressão na diagonal, cisalhamento no plano, tração na charneira central).");
        Eq("k = 4na²(1 − 2n)/(4n²a² + 1) + 1",
           $"4 × {F(m.N, 4)} × {F(m.A, 3)}² × (1 − 2 × {F(m.N, 4)}) / (4 × {F(m.N, 4)}² × {F(m.A, 3)}² + 1) + 1", F(m.K_, 4), "");
        Eq("L<sub>d</sub>² = (nL)² + l²/4", $"({F(m.N * m.Lmm, 1)})² + {F(m.lmm, 0)}²/4", F(m.Ld2, 0), "mm²");
        Eq("A = [L<sub>d</sub>²(3k + 2)/(3(1 + k)) − nL²/2] / [2(1 + k)]", "", F(m.CoefA, 0), "mm²");
        Eq("B = k²/[2(1 + k)] · [nL²/2 − k·L<sub>d</sub>²/(3(1 + k))]", "", F(m.CoefB, 0), "mm²");
        Eq("C = (k − 1)·l²/(16n)", $"({F(m.K_, 4)} − 1) × {F(m.lmm, 0)}²/(16 × {F(m.N, 4)})", F(m.CoefC, 0), "mm²");
        Eq("D = (L²/8)·(1 − 2n)²", $"({F(m.Lmm, 0)}²/8) × (1 − 2 × {F(m.N, 4)})²", F(m.CoefD, 0), "mm²");
        Eq("b<sub>fratura</sub> = l² / [8K(A + B + C − D)]",
           $"{F(m.lmm, 0)}² / [8 × {F(m.K, 3)} × ({F(m.CoefA, 0)} + {F(m.CoefB, 0)} + {F(m.CoefC, 0)} − {F(m.CoefD, 0)})]",
           F(m.BTension, 4), "", "fratura da tela através do vão menor");
        Eq("b<sub>compr</sub> = [0,85·f<sub>c</sub>·0,45·d − (1 + K)·T<sub>0</sub>/2] / (k·K·T<sub>0</sub>)",
           $"[0,85 × {F(inp.Fck, 0)} × 0,45 × {F(inp.MeshDepth, 0)} − (1 + {F(m.K, 3)}) × {F(m.T0, 2)}/2] / ({F(m.K_, 4)} × {F(m.K, 3)} × {F(m.T0, 2)})",
           F(m.BCompression, 4), "", "esmagamento do concreto nos cantos; T<sub>0</sub> = A<sub>s</sub>·f<sub>sy,θ</sub> em N/mm");
        Eq("b = min(b<sub>fratura</sub> ; b<sub>compr</sub>)", "", F(m.B, 4), "",
           m.CompressionGoverns ? "governa o esmagamento do concreto" : "governa a fratura da tela");

        // ------------------------------------------------------------ 8. Fatores de aumento
        Step("Fatores de aumento por ação de membrana", "Índice b: efeito das forças normais na resistência das charneiras; índice m: contribuição direta da membrana.");
        Eq("α<sub>1</sub> = 2g<sub>01</sub>/(3 + g<sub>01</sub>) ; β<sub>1</sub> = (1 − g<sub>01</sub>)/(3 + g<sub>01</sub>)", "", $"{F(m.Alpha1, 3)} ; {F(m.Beta1, 3)}", "");
        Eq("α<sub>2</sub> = 2g<sub>02</sub>/(3 + g<sub>02</sub>) ; β<sub>2</sub> = (1 − g<sub>02</sub>)/(3 + g<sub>02</sub>)", "", $"{F(m.Alpha2, 3)} ; {F(m.Beta2, 3)}", "");
        Eq("e<sub>1b</sub> = 2n[1 + α<sub>1</sub>b(k − 1)/2 − β<sub>1</sub>b²(k² − k + 1)/3] + (1 − 2n)(1 − α<sub>1</sub>b − β<sub>1</sub>b²)", "", F(m.E1b, 3), "");
        Eq("e<sub>1m</sub> = [4b/(3 + g<sub>01</sub>)]·(w/d)·[(1 − 2n) + n(3k + 2 − k³)/(3(1 + k)²)]",
           $"[4 × {F(m.B, 4)}/(3 + {F(m.G01, 3)})] × ({F(m.W, 1)}/{F(inp.MeshDepth, 0)}) × [...]", F(m.E1m, 3), "");
        Eq("e<sub>2b</sub> = 1 + α<sub>2</sub>bK(k − 1)/2 − β<sub>2</sub>b²K²(k² − k + 1)/3", "", F(m.E2b, 3), "");
        Eq("e<sub>2m</sub> = [4bK/(3 + g<sub>02</sub>)]·(w/d)·(2 + 3k − k³)/(6(1 + k)²)", "", F(m.E2m, 3), "");
        Eq("e<sub>1</sub> = e<sub>1b</sub> + e<sub>1m</sub> ; e<sub>2</sub> = e<sub>2b</sub> + e<sub>2m</sub>", "", $"{F(m.E1, 3)} ; {F(m.E2, 3)}", "");
        Eq("e = e<sub>1</sub> − (e<sub>1</sub> − e<sub>2</sub>)/(1 + 2μa²)",
           $"{F(m.E1, 3)} − ({F(m.E1, 3)} − {F(m.E2, 3)})/(1 + 2 × {F(m.Mu, 3)} × {F(m.A, 3)}²)", F(m.E, 3), "");

        // ------------------------------------------------------------ 9. Resistência da laje
        Step("Resistência da laje com ação de membrana", "");
        Eq("q<sub>fi,Rd,laje</sub> = e·p<sub>fi</sub>", $"{F(m.E, 3)} × {F(m.Pfi, 3)}", F(m.QSlab, 2), "kN/m²");

        // ------------------------------------------------------------ 10. Vigas internas
        Step("Contribuição das vigas internas sem proteção", "Temperatura pelo método incremental (EN 1994-1-2, 4.3.4.2.2); momento plástico da viga mista aquecida.");
        if (bm.Count == 0) Note("Não há vigas internas: q<sub>fi,Rd,vigas</sub> = 0.");
        else
        {
            Eq("k<sub>sh</sub> = 0,9·(d + 0,5b<sub>f</sub>)/(d + 1,5b<sub>f</sub> − t<sub>w</sub>)",
               $"0,9 × ({F(inp.BeamH, 0)} + 0,5 × {F(inp.BeamB, 0)})/({F(inp.BeamH, 0)} + 1,5 × {F(inp.BeamB, 0)} − {F(inp.BeamTw, 1)})", F(bm.Ksh, 3), "");
            Eq("k<sub>sh</sub>·(A/V)<sub>mesa</sub> = k<sub>sh</sub>·2(b<sub>f</sub> + t<sub>f</sub>)/(b<sub>f</sub>·t<sub>f</sub>)",
               $"{F(bm.Ksh, 3)} × {F(bm.AvFlange, 1)}", F(bm.KshAvFlange, 1), "m⁻¹");
            Eq("θ<sub>a</sub> (mesa inferior e alma)", bm.ThetaImposed ? "valor imposto pelo usuário" : $"incremental, ISO 834, t = {F(t, 0)} min, Δt = 2 s", F(bm.Theta, 1), "°C");
            Eq("k<sub>y,θ</sub>", "", F(bm.Ky, 4), "");
            Eq("θ<sub>conector</sub> = 0,8·θ<sub>a</sub> → k<sub>u,θ</sub>", $"0,8 × {F(bm.Theta, 1)} = {F(bm.ThetaStud, 1)} °C", F(bm.Ku, 3), "");
            Eq("F<sub>a</sub> = A·f<sub>y</sub>·k<sub>y,θ</sub>", $"{F(inp.BeamArea * 100, 0)} × {F(inp.BeamFy, 0)} × {F(bm.Ky, 4)}", F(bm.Fa / 1000, 1), "kN");
            Eq("b<sub>eff</sub> = min(L1/4 ; s)", $"min({F(inp.L1 * 250, 0)} ; {F(bm.Spacing, 0)})", F(bm.Beff, 0), "mm");
            Eq("h<sub>u</sub> = F<sub>a</sub> / (b<sub>eff</sub>·f<sub>c</sub>)", $"{F(bm.Fa, 0)} / ({F(bm.Beff, 0)} × {F(inp.Fck, 0)})", F(bm.Hu, 2), "mm");
            Eq("n<sub>c,θ</sub> = n<sub>c,20</sub>·k<sub>u,θ</sub>·1,25 / k<sub>y,θ</sub>",
               $"{F(inp.ShearConnection, 2)} × {F(bm.Ku, 3)} × 1,25 / {F(bm.Ky, 4)}", F(bm.NcTheta, 2), "",
               bm.FullConnection ? "≥ 1: interação completa" : "< 1: interação parcial (interpolação linear entre perfil isolado e interação completa)");
            Eq("M<sub>fi,Rd,completa</sub> = F<sub>a</sub>·(d/2 + h<sub>t</sub> − h<sub>u</sub>/2)",
               $"{F(bm.Fa, 0)} × ({F(inp.BeamH / 2, 1)} + {F(inp.SlabThickness, 0)} − {F(bm.Hu / 2, 2)})", F(bm.MFull / 1e6, 2), "kN·m");
            if (!bm.FullConnection)
                Eq("M<sub>fi,Rd</sub> = M<sub>pl,a,θ</sub> + n<sub>c,θ</sub>(M<sub>completa</sub> − M<sub>pl,a,θ</sub>)",
                   $"{F(bm.MplA / 1e6, 2)} + {F(bm.NcTheta, 2)} × ({F(bm.MFull / 1e6, 2)} − {F(bm.MplA / 1e6, 2)})", F(bm.MfiRd / 1e6, 2), "kN·m");
            Eq("q<sub>fi,Rd,vigas</sub> = 8·M<sub>fi,Rd</sub> / (L1²·s)",
               $"8 × {F(bm.MfiRd / 1e6, 2)} / ({F(inp.L1)}² × {F(bm.Spacing / 1000, 3)})", F(bm.QBeams, 2), "kN/m²");
        }

        // ------------------------------------------------------------ 11. Verificação
        Step("Verificação da capacidade portante", "");
        Eq("q<sub>fi,Rd</sub> = q<sub>fi,Rd,laje</sub> + q<sub>fi,Rd,vigas</sub>", $"{F(m.QSlab)} + {F(bm.QBeams)}", F(r.QfiRd), "kN/m²");
        Verdict(r.QfiRd >= ld.QfiSd,
            $"q<sub>fi,Rd</sub> = {F(r.QfiRd)} kN/m² {(r.QfiRd >= ld.QfiSd ? "≥" : "<")} q<sub>fi,Sd</sub> = {F(ld.QfiSd)} kN/m² — utilização {F(r.Utilization * 100, 0)} %.");

        if (sweep is { Count: > 0 })
        {
            Section("Resistência ao longo do tempo de exposição");
            if (img?.ChartPng is not null) Figure(img.ChartPng, "Resistência do painel em função do tempo de incêndio-padrão.");
            Table(new[] { "t (min)", "θ tela (°C)", "θ viga (°C)", "q laje (kN/m²)", "q vigas (kN/m²)", "q total (kN/m²)" },
                sweep.Where(p => p.Time % 15 == 0).Select(p => new[]
                { F(p.Time, 0), F(p.ThetaMesh, 0), F(p.ThetaBeam, 0), F(p.QSlab), F(p.QBeams), F(p.QTotal) }).ToArray());
        }

        // ------------------------------------------------------------ 12. Perímetro
        Step("Esforços nas vigas de perímetro",
             "Mecanismos alternativos com charneira única no meio do painel e rótulas nas vigas de perímetro (expressões do guia FRACOF, com a largura efetiva descontada dos dois lados, a favor da segurança). As vigas devem ser protegidas e dimensionadas para estes esforços no TRRF.");
        foreach (var p in r.Perimeter)
            Table(new[] { H(p.Name), "Valor" }, new[]
            {
                new[] { "Vão", $"{F(p.Span)} m" },
                new[] { "Comprimento efetivo da charneira na laje", $"{F(p.Leff)} m" },
                new[] { "M<sub>fi,Sd</sub>", $"{F(p.MSd, 1)} kN·m" },
                new[] { "V<sub>fi,Sd</sub> = 4M/L", $"{F(p.VSd, 1)} kN" },
            });

        // ------------------------------------------------------------ Verificações
        Section("Resumo das verificações");
        _sb.Append("<table class='checks'><tr><th></th><th>Verificação</th><th>Detalhe</th></tr>");
        foreach (var c in r.Checks)
        {
            string cls = c.Status switch { CheckStatus.Ok => "ok", CheckStatus.Warning => "warn", _ => "fail" };
            string lbl = c.Status switch { CheckStatus.Ok => "Atende", CheckStatus.Warning => "Atenção", _ => "Não atende" };
            _sb.Append($"<tr><td><span class='badge {cls}'>{lbl}</span></td><td>{H(c.Title)}</td><td>{H(c.Detail)}</td></tr>");
        }
        _sb.Append("</table>");

        Section("Referências e limitações");
        _sb.Append("<p>Método simplificado de Bailey para ação de membrana em lajes mistas (Bailey &amp; Moore, <i>The Structural Engineer</i>, 2000; Bailey, <i>Engineering Structures</i> 26, 2004), na forma do <i>FRACOF Design Guide</i> e <i>Engineering Background</i> (Vassart &amp; Zhao, 2011). Aquecimento de perfis e fatores de redução: EN 1993-1-2, EN 1994-1-2, EN 1992-1-2, NBR 14323.</p>");
        _sb.Append("<p>O método não é normatizado no Brasil; sua aplicação caracteriza método avançado/análise de engenharia de incêndio e requer justificativa perante o Corpo de Bombeiros. A contribuição da fôrma é desprezada. A validação experimental cobre telas dúcteis (classe B ou C), fôrmas até 80 mm, pórticos contraventados com ligações flexíveis e TRRF até 120 min. As dimensões das nervuras da fôrma devem ser conferidas com o catálogo do fabricante.</p>");
        _sb.Append("</main></body></html>");
        return _sb.ToString();
    }

    // ================================================================= helpers de HTML
    private void Head(ProjectInput inp)
    {
        _sb.Append("<!DOCTYPE html><html lang='pt-BR'><head><meta charset='utf-8'>");
        _sb.Append("<meta http-equiv='X-UA-Compatible' content='IE=edge'>");
        _sb.Append($"<title>{H(inp.ProjectName)} – memorial</title><style>{Css}</style></head><body><main>");
        _sb.Append($"<header><div class='kind'>Memorial de cálculo · laje mista em situação de incêndio · ação de membrana</div>");
        _sb.Append($"<h1>{H(inp.ProjectName)}</h1>");
        _sb.Append($"<div class='meta'>{H(inp.PanelName)}{(string.IsNullOrWhiteSpace(inp.Engineer) ? "" : " · " + H(inp.Engineer))} · {DateTime.Now.ToString("dd/MM/yyyy HH:mm", Br)}</div></header>");
    }

    private void Summary(ProjectInput inp, DesignResult r)
    {
        bool ok = r.QfiRd >= r.Load.QfiSd;
        int warns = r.Checks.Count(c => c.Status == CheckStatus.Warning);
        _sb.Append($"<section class='summary {(ok ? "ok" : "fail")}'>");
        _sb.Append($"<div class='big'><span class='num'>{F(r.QfiRd)}</span><span class='unit'>kN/m² resistência</span></div>");
        _sb.Append($"<div class='big'><span class='num'>{F(r.Load.QfiSd)}</span><span class='unit'>kN/m² solicitação</span></div>");
        _sb.Append($"<div class='big'><span class='num'>{F(r.Utilization * 100, 0)}%</span><span class='unit'>utilização</span></div>");
        _sb.Append($"<p class='verdict'>{(ok ? "O painel atende" : "O painel não atende")} ao critério de capacidade portante para {F(inp.FireTime, 0)} min de incêndio-padrão"
                 + (warns > 0 ? $", com {warns} ponto(s) de atenção listados ao final." : ".") + "</p></section>");
    }

    private void Section(string title) => _sb.Append($"<h2>{H(title)}</h2>");

    private void Step(string title, string intro)
    {
        _step++;
        _sb.Append($"<h2><span class='step'>{_step}</span>{H(title)}</h2>");
        if (!string.IsNullOrEmpty(intro)) _sb.Append($"<p class='intro'>{intro}</p>");
    }

    private void Eq(string symbolic, string substituted, string result, string unit, string? note = null)
    {
        _sb.Append("<div class='eq'><div class='sym'>").Append(symbolic).Append("</div>");
        if (!string.IsNullOrEmpty(substituted)) _sb.Append("<div class='sub'>= ").Append(substituted).Append("</div>");
        _sb.Append("<div class='res'>= <b>").Append(result).Append("</b> ").Append(unit).Append("</div>");
        if (!string.IsNullOrEmpty(note)) _sb.Append("<div class='note'>").Append(note).Append("</div>");
        _sb.Append("</div>");
    }

    private void Note(string html) => _sb.Append($"<p class='intro'>{html}</p>");

    private void Verdict(bool ok, string html) =>
        _sb.Append($"<p class='v {(ok ? "ok" : "fail")}'>{(ok ? "Atende" : "Não atende")}: {html}</p>");

    private void Figure(string pngBase64, string caption) =>
        _sb.Append($"<figure><img src='data:image/png;base64,{pngBase64}' alt='{H(caption)}'><figcaption>{H(caption)}</figcaption></figure>");

    private void Table(string[] header, string[][] rows)
    {
        _sb.Append("<table><tr>");
        foreach (var h in header) _sb.Append("<th>").Append(h).Append("</th>");
        _sb.Append("</tr>");
        foreach (var row in rows)
        {
            _sb.Append("<tr>");
            foreach (var c in row) _sb.Append("<td>").Append(c).Append("</td>");
            _sb.Append("</tr>");
        }
        _sb.Append("</table>");
    }

    private const string Css = @"
body{margin:0;background:#eef1f3;color:#1d2830;font:15px/1.6 Cambria,Georgia,'Times New Roman',serif}
main{max-width:860px;margin:0 auto;background:#fff;padding:40px 56px 56px}
header{border-bottom:3px solid #1d2830;padding-bottom:14px;margin-bottom:22px}
.kind{font:600 12px 'Segoe UI',Arial,sans-serif;color:#b8432f;letter-spacing:.02em}
h1{font:600 26px/1.25 'Segoe UI',Arial,sans-serif;margin:6px 0 4px}
.meta{font:13px 'Segoe UI',Arial,sans-serif;color:#5a6b77}
h2{font:600 17px 'Segoe UI',Arial,sans-serif;margin:34px 0 10px;padding-top:10px;border-top:1px solid #d6dde2}
.step{display:inline-block;min-width:26px;height:26px;line-height:26px;text-align:center;margin-right:10px;background:#1d2830;color:#fff;font-size:13px;border-radius:3px}
.intro{color:#3c4a54;margin:4px 0 12px}
.eq{border-left:3px solid #d6dde2;padding:6px 0 6px 14px;margin:10px 0}
.eq .sym{font-style:italic}
.eq .sub{color:#5a6b77;font-size:14px}
.eq .res b{color:#1d2830}
.eq .note{font:12.5px 'Segoe UI',Arial,sans-serif;color:#5a6b77}
table{border-collapse:collapse;width:100%;margin:10px 0 16px;font:13.5px 'Segoe UI',Arial,sans-serif}
th{text-align:left;background:#f2f4f6;border-bottom:2px solid #1d2830;padding:6px 8px}
td{border-bottom:1px solid #d6dde2;padding:6px 8px;vertical-align:top}
figure{margin:18px 0}figure img{max-width:100%;border:1px solid #d6dde2}
figcaption{font:12.5px 'Segoe UI',Arial,sans-serif;color:#5a6b77;margin-top:4px}
.summary{border:2px solid #2e7d4f;padding:16px 20px;margin:8px 0 10px}
.summary.fail{border-color:#b8432f}
.big{display:inline-block;margin-right:34px;vertical-align:top}
.big .num{display:block;font:600 30px 'Segoe UI',Arial,sans-serif}
.big .unit{font:12.5px 'Segoe UI',Arial,sans-serif;color:#5a6b77}
.summary.ok .big:last-of-type .num{color:#2e7d4f}.summary.fail .big:last-of-type .num{color:#b8432f}
.verdict{margin:12px 0 0}
.v{padding:8px 12px;margin:10px 0;font-family:'Segoe UI',Arial,sans-serif;font-size:14px}
.v.ok{background:#e8f3ec;border-left:4px solid #2e7d4f}.v.fail{background:#f8e9e6;border-left:4px solid #b8432f}
.badge{font:600 11.5px 'Segoe UI',Arial,sans-serif;padding:2px 8px;border-radius:2px;color:#fff;white-space:nowrap}
.badge.ok{background:#2e7d4f}.badge.warn{background:#a86b00}.badge.fail{background:#b8432f}
@media print{body{background:#fff}main{padding:0;max-width:none}h2{page-break-after:avoid}.eq,figure,table{page-break-inside:avoid}}
";
}
