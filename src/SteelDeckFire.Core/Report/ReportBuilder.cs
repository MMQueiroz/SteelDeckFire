using System.Globalization;
using System.Text;
using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Models;

namespace SteelDeckFire.Core.Report;

/// <summary>Monta o memorial de cálculo, com cada equação em forma literal, substituída e resultado.</summary>
public sealed class ReportBuilder
{
    private static readonly CultureInfo Br = new("pt-BR");
    private ReportDocument _doc = new();
    private int _step;

    private static string F(double v, int dec = 2) => v.ToString("N" + dec, Br);
    private static string H(string s) => Escape(s);

    /// <summary>Protege texto livre (nomes digitados pelo usuário) contra a marcação de subscrito/itálico.</summary>
    public static string Escape(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c is '\\' or '_' or '^' or '*' or '{' or '}') sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }

    public static ReportDocument Build(ProjectInput inp, DesignResult r, IReadOnlyList<TimePoint>? sweep)
        => new ReportBuilder().BuildInternal(inp, r, sweep);

    private ReportDocument BuildInternal(ProjectInput inp, DesignResult r, IReadOnlyList<TimePoint>? sweep)
    {
        _doc = new ReportDocument { Title = $"{inp.ProjectName} – memorial" };
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
            new[] { "Laje", "Geometria da nervura (h_{2} / l_{1} / l_{2} / l_{3})", $"{F(inp.DeckH2, 0)} / {F(inp.DeckL1, 0)} / {F(inp.DeckL2, 0)} / {F(inp.DeckL3, 0)} mm" },
            new[] { "Laje", "Altura total h_{t} / concreto acima da fôrma h_{1}", $"{F(inp.SlabThickness, 0)} / {F(th.H1, 0)} mm" },
            new[] { "Laje", "Concreto", $"f_{{ck}} = {F(inp.Fck, 0)} MPa" },
            new[] { "Tela", "Tipo", H(inp.MeshName) },
            new[] { "Tela", "A_{s} paralela a L2 / paralela a L1", $"{F(inp.AsAlongL2, 0)} / {F(inp.AsAlongL1, 0)} mm²/m" },
            new[] { "Tela", "f_{y} / d (eixo à face superior)", $"{F(inp.MeshFy, 0)} MPa / {F(inp.MeshDepth, 0)} mm" },
            new[] { "Vigas", "Perfil (d × b_{f} × t_{w} × t_{f}, r)", $"{F(inp.BeamH, 0)} × {F(inp.BeamB, 0)} × {F(inp.BeamTw, 1)} × {F(inp.BeamTf, 1)} mm, r = {F(inp.BeamR, 0)} mm" },
            new[] { "Vigas", "A / Z_{x} / f_{y} / grau de interação", $"{F(inp.BeamArea, 1)} cm² / {F(inp.BeamZx, 1)} cm³ / {F(inp.BeamFy, 0)} MPa / {F(inp.ShearConnection, 2)}" },
            new[] { "Incêndio", "TRRF – incêndio-padrão ISO 834", $"{F(t, 0)} min" },
        });
        Figure(ReportFigure.Plan, 0.69, "Planta do painel com o padrão de linhas de ruptura e a distribuição das forças de membrana nas charneiras.");
        Figure(ReportFigure.Section, 0.4, "Seção transversal da laje com a posição da tela e as temperaturas de cálculo.");

        // ------------------------------------------------------------ 1. Ações
        Step("Ações em situação de incêndio", "Combinação última excepcional (NBR 8681 / NBR 14323 ou EN 1990).");
        if (ld.SlabWeightComputed)
        {
            Eq("t_{eq} = h_{1} + h_{2}(l_{1} + l_{2}) / [2(l_{1} + l_{3})]",
               $"{F(th.H1, 0)} + {F(inp.DeckH2, 0)}·({F(inp.DeckL1, 0)} + {F(inp.DeckL2, 0)}) / [2·({F(inp.DeckL1, 0)} + {F(inp.DeckL3, 0)})]",
               F(ld.ConcreteEqThickness, 1), "mm", "espessura média de concreto");
            Eq("g_{laje} = γ_{c}·t_{eq} + g_{fôrma}",
               $"{F(inp.ConcreteWeight, 0)} × {F(ld.ConcreteEqThickness / 1000, 4)} + {F(ld.DeckWeight, 3)}",
               F(ld.SlabSelfWeight, 2), "kN/m²", "peso da fôrma estimado");
        }
        else Note($"Peso próprio da laje informado: g_{{laje}} = {F(ld.SlabSelfWeight)} kN/m².");
        Eq("G = g_{laje} + g_{adic} + g_{vigas}",
           $"{F(ld.SlabSelfWeight)} + {F(inp.AdditionalDead)} + {F(inp.BeamsSelfWeight)}", F(ld.PermanentTotal), "kN/m²");
        Eq("q_{fi,Sd} = γ_{g}·G + ψ·Q",
           $"{F(inp.GammaG)} × {F(ld.PermanentTotal)} + {F(inp.PsiFire)} × {F(inp.LiveLoad)}", F(ld.QfiSd), "kN/m²");

        // ------------------------------------------------------------ 2. Espessura efetiva
        Step("Espessura efetiva e isolamento térmico", "EN 1994-1-2, Anexo D (D.4) e Tabela D.6.");
        Note($"h_{{2}}/h_{{1}} = {F(th.H2 / th.H1)} {(th.HeffFormulaA ? "≤ 1,5 e h_{{1}} > 40 mm" : "> 1,5 ou h_{{1}} ≤ 40 mm")}.");
        if (th.HeffFormulaA)
            Eq("h_{eff} = h_{1} + 0,5·h_{2}·(l_{1} + l_{2})/(l_{1} + l_{3})",
               $"{F(th.H1, 0)} + 0,5 × {F(th.H2, 0)} × ({F(inp.DeckL1, 0)} + {F(inp.DeckL2, 0)})/({F(inp.DeckL1, 0)} + {F(inp.DeckL3, 0)})",
               F(th.Heff, 1), "mm");
        else
            Eq("h_{eff} = h_{1}·[1 + 0,75·(l_{1} + l_{2})/(l_{1} + l_{3})]",
               $"{F(th.H1, 0)} × [1 + 0,75 × ({F(inp.DeckL1, 0)} + {F(inp.DeckL2, 0)})/({F(inp.DeckL1, 0)} + {F(inp.DeckL3, 0)})]",
               F(th.Heff, 1), "mm");
        Verdict(th.Heff >= th.HeffMin, $"h_{{eff}} = {F(th.Heff, 1)} mm {(th.Heff >= th.HeffMin ? "≥" : "<")} h_{{eff,mín}} = {F(th.HeffMin, 0)} mm para {F(t, 0)} min (critério I).");

        // ------------------------------------------------------------ 3. Temperaturas
        Step("Temperaturas na laje", "Perfis de temperatura para incêndio-padrão (guia FRACOF, Tab. 3-1, a partir da EN 1992-1-2), interpolados em x e t.");
        Table(new[] { "Ponto", "Posição x (a partir da face exposta)", "Temperatura" }, new[]
        {
            new[] { "θ_{2} – face exposta", "2,5 mm", $"{F(th.Theta2, 0)} °C" },
            new[] { "θ_{1} – face não exposta", $"h_{{eff}} = {F(th.HeffUsed, 1)} mm", $"{F(th.Theta1, 0)} °C" },
            new[] { "θ_{s} – tela", $"h_{{eff}} − d = {F(th.XMesh, 1)} mm", $"{F(th.ThetaS, 0)} °C" },
        });
        Eq("f_{sy,θ} = k_{s,θ}·f_{sy}", $"{F(th.Ks, 3)} × {F(inp.MeshFy, 0)}", F(th.FsyTheta, 1), "MPa",
           inp.MeshColdWorked ? "fio trefilado – EN 1992-1-2 Tab. 3.2a" : "barra laminada – EN 1992-1-2 Tab. 3.2a");

        // ------------------------------------------------------------ 4. Momentos da tela
        string longName = m.LongIsL2 ? "L2" : "L1";
        string shortName = m.LongIsL2 ? "L1" : "L2";
        Step("Momentos resistentes da tela", $"L = {F(m.Lmm / 1000)} m ({longName}), l = {F(m.lmm / 1000)} m ({shortName}). A fôrma é desprezada.");
        Eq("A_{s} (barras paralelas a L)", "", F(m.As, 4), "mm²/mm");
        Eq("K = A_{s,l} / A_{s,L}", "", F(m.K, 3), "");
        Eq("g_{01} = 1 − 2·K·A_{s}·f_{sy,θ} / (0,85·f_{c}·d)",
           $"1 − 2 × {F(m.K, 3)} × {F(m.As, 4)} × {F(th.FsyTheta, 1)} / (0,85 × {F(inp.Fck, 0)} × {F(inp.MeshDepth, 0)})", F(m.G01, 3), "");
        Eq("g_{02} = 1 − 2·A_{s}·f_{sy,θ} / (0,85·f_{c}·d)",
           $"1 − 2 × {F(m.As, 4)} × {F(th.FsyTheta, 1)} / (0,85 × {F(inp.Fck, 0)} × {F(inp.MeshDepth, 0)})", F(m.G02, 3), "");
        Eq("M_{0} = A_{s}·f_{sy,θ}·d·(3 + g_{02})/4",
           $"{F(m.As, 4)} × {F(th.FsyTheta, 1)} × {F(inp.MeshDepth, 0)} × (3 + {F(m.G02, 3)})/4", F(m.M0, 1), "N·mm/mm");
        Eq("μ = K·(3 + g_{01})/(3 + g_{02})",
           $"{F(m.K, 3)} × (3 + {F(m.G01, 3)})/(3 + {F(m.G02, 3)})", F(m.Mu, 3), "");

        // ------------------------------------------------------------ 5. Charneiras
        Step("Carga de colapso por charneiras plásticas", "Padrão de charneiras de laje simplesmente apoiada nos quatro lados (sem membrana).");
        Eq("a = L / l", $"{F(m.Lmm, 0)} / {F(m.lmm, 0)}", F(m.A, 3), "");
        Eq("n = [√(3μa² + 1) − 1] / (2μa²)",
           $"[√(3 × {F(m.Mu, 3)} × {F(m.A, 3)}² + 1) − 1] / (2 × {F(m.Mu, 3)} × {F(m.A, 3)}²)", F(m.N, 4), "",
           $"cruzamento das charneiras a n·L = {F(m.N * m.Lmm / 1000, 3)} m dos lados menores");
        Eq("p_{fi} = 6·M_{0} / (n²·L²)",
           $"6 × {F(m.M0, 1)} / ({F(m.N, 4)}² × {F(m.Lmm, 0)}²) × 10³", F(m.Pfi, 3), "kN/m²");

        // ------------------------------------------------------------ 6. Deslocamento
        Step("Deslocamento de cálculo", "Curvatura térmica + alongamento da tela, calibrados com os ensaios de Cardington.");
        Eq("w_{θ} = α·(θ_{2} − θ_{1})·l² / (19,2·h_{eff})",
           $"{inp.ThermalAlpha.ToString("0.0E+0", Br)} × ({F(th.Theta2, 0)} − {F(th.Theta1, 0)}) × {F(m.lmm, 0)}² / (19,2 × {F(th.HeffUsed, 1)})",
           F(m.WThermal, 1), "mm");
        Eq("w_{mec} = √(0,5·f_{sy}/E_{s} · 3L²/8) ≤ l/30",
           $"min[√(0,5 × {F(inp.MeshFy, 0)}/{F(inp.MeshE, 0)} × 3 × {F(m.Lmm, 0)}²/8) = {F(m.WMechRaw, 1)} ; {F(m.lmm / 30, 1)}]",
           F(m.WMech, 1), "mm");
        Eq("w = min[w_{θ} + w_{mec} ; (L + l)/30]",
           $"min[{F(m.WThermal + m.WMech, 1)} ; {F(m.WLimitTotal, 1)}]", F(m.W, 1), "mm");

        // ------------------------------------------------------------ 7. Parâmetros de membrana
        Step("Parâmetros das forças de membrana",
             "k: relação compressão no canto / tração no fim da diagonal. A, B, C, D: momentos no plano, em torno da borda, das forças no meio elemento 1 (tração e compressão na diagonal, cisalhamento no plano, tração na charneira central).");
        Eq("k = 4na²(1 − 2n)/(4n²a² + 1) + 1",
           $"4 × {F(m.N, 4)} × {F(m.A, 3)}² × (1 − 2 × {F(m.N, 4)}) / (4 × {F(m.N, 4)}² × {F(m.A, 3)}² + 1) + 1", F(m.K_, 4), "");
        Eq("L_{d}² = (nL)² + l²/4", $"({F(m.N * m.Lmm, 1)})² + {F(m.lmm, 0)}²/4", F(m.Ld2, 0), "mm²");
        Eq("A = [L_{d}²(3k + 2)/(3(1 + k)) − nL²/2] / [2(1 + k)]", "", F(m.CoefA, 0), "mm²");
        Eq("B = k²/[2(1 + k)] · [nL²/2 − k·L_{d}²/(3(1 + k))]", "", F(m.CoefB, 0), "mm²");
        Eq("C = (k − 1)·l²/(16n)", $"({F(m.K_, 4)} − 1) × {F(m.lmm, 0)}²/(16 × {F(m.N, 4)})", F(m.CoefC, 0), "mm²");
        Eq("D = (L²/8)·(1 − 2n)²", $"({F(m.Lmm, 0)}²/8) × (1 − 2 × {F(m.N, 4)})²", F(m.CoefD, 0), "mm²");
        Eq("b_{fratura} = l² / [8K(A + B + C − D)]",
           $"{F(m.lmm, 0)}² / [8 × {F(m.K, 3)} × ({F(m.CoefA, 0)} + {F(m.CoefB, 0)} + {F(m.CoefC, 0)} − {F(m.CoefD, 0)})]",
           F(m.BTension, 4), "", "fratura da tela através do vão menor");
        Eq("b_{compr} = [0,85·f_{c}·0,45·d − (1 + K)·T_{0}/2] / (k·K·T_{0})",
           $"[0,85 × {F(inp.Fck, 0)} × 0,45 × {F(inp.MeshDepth, 0)} − (1 + {F(m.K, 3)}) × {F(m.T0, 2)}/2] / ({F(m.K_, 4)} × {F(m.K, 3)} × {F(m.T0, 2)})",
           F(m.BCompression, 4), "", "esmagamento do concreto nos cantos; T_{0} = A_{s}·f_{sy,θ} em N/mm");
        Eq("b = min(b_{fratura} ; b_{compr})", "", F(m.B, 4), "",
           m.CompressionGoverns ? "governa o esmagamento do concreto" : "governa a fratura da tela");

        // ------------------------------------------------------------ 8. Fatores de aumento
        Step("Fatores de aumento por ação de membrana", "Índice b: efeito das forças normais na resistência das charneiras; índice m: contribuição direta da membrana.");
        Eq("α_{1} = 2g_{01}/(3 + g_{01}) ; β_{1} = (1 − g_{01})/(3 + g_{01})", "", $"{F(m.Alpha1, 3)} ; {F(m.Beta1, 3)}", "");
        Eq("α_{2} = 2g_{02}/(3 + g_{02}) ; β_{2} = (1 − g_{02})/(3 + g_{02})", "", $"{F(m.Alpha2, 3)} ; {F(m.Beta2, 3)}", "");
        Eq("e_{1b} = 2n[1 + α_{1}b(k − 1)/2 − β_{1}b²(k² − k + 1)/3] + (1 − 2n)(1 − α_{1}b − β_{1}b²)", "", F(m.E1b, 3), "");
        Eq("e_{1m} = [4b/(3 + g_{01})]·(w/d)·[(1 − 2n) + n(3k + 2 − k³)/(3(1 + k)²)]",
           $"[4 × {F(m.B, 4)}/(3 + {F(m.G01, 3)})] × ({F(m.W, 1)}/{F(inp.MeshDepth, 0)}) × [...]", F(m.E1m, 3), "");
        Eq("e_{2b} = 1 + α_{2}bK(k − 1)/2 − β_{2}b²K²(k² − k + 1)/3", "", F(m.E2b, 3), "");
        Eq("e_{2m} = [4bK/(3 + g_{02})]·(w/d)·(2 + 3k − k³)/(6(1 + k)²)", "", F(m.E2m, 3), "");
        Eq("e_{1} = e_{1b} + e_{1m} ; e_{2} = e_{2b} + e_{2m}", "", $"{F(m.E1, 3)} ; {F(m.E2, 3)}", "");
        Eq("e = e_{1} − (e_{1} − e_{2})/(1 + 2μa²)",
           $"{F(m.E1, 3)} − ({F(m.E1, 3)} − {F(m.E2, 3)})/(1 + 2 × {F(m.Mu, 3)} × {F(m.A, 3)}²)", F(m.E, 3), "");

        // ------------------------------------------------------------ 9. Resistência da laje
        Step("Resistência da laje com ação de membrana", "");
        Eq("q_{fi,Rd,laje} = e·p_{fi}", $"{F(m.E, 3)} × {F(m.Pfi, 3)}", F(m.QSlab, 2), "kN/m²");

        // ------------------------------------------------------------ 10. Vigas internas
        Step("Contribuição das vigas internas sem proteção", "Temperatura pelo método incremental (EN 1994-1-2, 4.3.4.2.2); momento plástico da viga mista aquecida.");
        if (bm.Count == 0) Note("Não há vigas internas: q_{fi,Rd,vigas} = 0.");
        else
        {
            Eq("A = 2b_{f}·t_{f} + (d − 2t_{f})·t_{w} + (4 − π)·r²",
               $"2 × {F(inp.BeamB, 0)} × {F(inp.BeamTf, 1)} + ({F(inp.BeamH, 0)} − 2 × {F(inp.BeamTf, 1)}) × {F(inp.BeamTw, 1)} + (4 − π) × {F(inp.BeamR, 0)}²",
               F(inp.BeamArea, 2), "cm²", "área do perfil calculada pela geometria, com as concordâncias alma–mesa");
            Eq("Z_{x} = b_{f}·t_{f}·(d − t_{f}) + t_{w}·(d − 2t_{f})²/4 + 4A_{r}·(d/2 − t_{f} − ȳ_{r})",
               $"{F(inp.BeamB, 0)} × {F(inp.BeamTf, 1)} × ({F(inp.BeamH, 0)} − {F(inp.BeamTf, 1)}) + {F(inp.BeamTw, 1)} × ({F(inp.BeamH, 0)} − 2 × {F(inp.BeamTf, 1)})²/4 + {F(SectionProperties.FilletsZx(inp.BeamH, inp.BeamTf, inp.BeamR) / 1000, 1)} × 10³",
               F(inp.BeamZx, 1), "cm³", "A_{r} = (1 − π/4)·r², ȳ_{r} = r·(10 − 3π)/(12 − 3π): área e centroide de cada concordância");
            Eq("k_{sh} = 0,9·(d + 0,5b_{f})/(d + 1,5b_{f} − t_{w})",
               $"0,9 × ({F(inp.BeamH, 0)} + 0,5 × {F(inp.BeamB, 0)})/({F(inp.BeamH, 0)} + 1,5 × {F(inp.BeamB, 0)} − {F(inp.BeamTw, 1)})", F(bm.Ksh, 3), "");
            Eq("k_{sh}·(A/V)_{mesa} = k_{sh}·2(b_{f} + t_{f})/(b_{f}·t_{f})",
               $"{F(bm.Ksh, 3)} × {F(bm.AvFlange, 1)}", F(bm.KshAvFlange, 1), "m⁻¹");
            Eq("θ_{a} (mesa inferior e alma)", bm.ThetaImposed ? "valor imposto pelo usuário" : $"incremental, ISO 834, t = {F(t, 0)} min, Δt = 2 s", F(bm.Theta, 1), "°C");
            Eq("k_{y,θ}", "", F(bm.Ky, 4), "");
            Eq("θ_{conector} = 0,8·θ_{a} → k_{u,θ}", $"0,8 × {F(bm.Theta, 1)} = {F(bm.ThetaStud, 1)} °C", F(bm.Ku, 3), "");
            Eq("F_{a} = A·f_{y}·k_{y,θ}", $"{F(inp.BeamArea * 100, 0)} × {F(inp.BeamFy, 0)} × {F(bm.Ky, 4)}", F(bm.Fa / 1000, 1), "kN");
            Eq("b_{eff} = min(L1/4 ; s)", $"min({F(inp.L1 * 250, 0)} ; {F(bm.Spacing, 0)})", F(bm.Beff, 0), "mm");
            Eq("h_{u} = F_{a} / (b_{eff}·f_{c})", $"{F(bm.Fa, 0)} / ({F(bm.Beff, 0)} × {F(inp.Fck, 0)})", F(bm.Hu, 2), "mm");
            Eq("n_{c,θ} = n_{c,20}·k_{u,θ}·1,25 / k_{y,θ}",
               $"{F(inp.ShearConnection, 2)} × {F(bm.Ku, 3)} × 1,25 / {F(bm.Ky, 4)}", F(bm.NcTheta, 2), "",
               bm.FullConnection ? "≥ 1: interação completa" : "< 1: interação parcial (interpolação linear entre perfil isolado e interação completa)");
            Eq("M_{fi,Rd,completa} = F_{a}·(d/2 + h_{t} − h_{u}/2)",
               $"{F(bm.Fa, 0)} × ({F(inp.BeamH / 2, 1)} + {F(inp.SlabThickness, 0)} − {F(bm.Hu / 2, 2)})", F(bm.MFull / 1e6, 2), "kN·m");
            if (!bm.FullConnection)
                Eq("M_{fi,Rd} = M_{pl,a,θ} + n_{c,θ}(M_{completa} − M_{pl,a,θ})",
                   $"{F(bm.MplA / 1e6, 2)} + {F(bm.NcTheta, 2)} × ({F(bm.MFull / 1e6, 2)} − {F(bm.MplA / 1e6, 2)})", F(bm.MfiRd / 1e6, 2), "kN·m");
            Eq("q_{fi,Rd,vigas} = 8·M_{fi,Rd} / (L1²·s)",
               $"8 × {F(bm.MfiRd / 1e6, 2)} / ({F(inp.L1)}² × {F(bm.Spacing / 1000, 3)})", F(bm.QBeams, 2), "kN/m²");
        }

        // ------------------------------------------------------------ 11. Verificação
        Step("Verificação da capacidade portante", "");
        Eq("q_{fi,Rd} = q_{fi,Rd,laje} + q_{fi,Rd,vigas}", $"{F(m.QSlab)} + {F(bm.QBeams)}", F(r.QfiRd), "kN/m²");
        Verdict(r.QfiRd >= ld.QfiSd,
            $"q_{{fi,Rd}} = {F(r.QfiRd)} kN/m² {(r.QfiRd >= ld.QfiSd ? "≥" : "<")} q_{{fi,Sd}} = {F(ld.QfiSd)} kN/m² — utilização {F(r.Utilization * 100, 0)} %.");

        if (sweep is { Count: > 0 })
        {
            Section("Resistência ao longo do tempo de exposição");
            Figure(ReportFigure.TimeChart, 0.48, "Resistência do painel em função do tempo de incêndio-padrão.");
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
                new[] { "M_{fi,Sd}", $"{F(p.MSd, 1)} kN·m" },
                new[] { "V_{fi,Sd} = 4M/L", $"{F(p.VSd, 1)} kN" },
            });

        // ------------------------------------------------------------ Verificações
        Section("Resumo das verificações");
        _doc.Blocks.Add(new ChecksBlock(r.Checks));

        Section("Referências e limitações");
        Paragraph("Método simplificado de Bailey para ação de membrana em lajes mistas (Bailey & Moore, *The Structural Engineer*, 2000; Bailey, *Engineering Structures* 26, 2004), na forma do *FRACOF Design Guide* e *Engineering Background* (Vassart & Zhao, 2011). Aquecimento de perfis e fatores de redução: EN 1993-1-2, EN 1994-1-2, EN 1992-1-2, NBR 14323.");
        Paragraph("O método não é normatizado no Brasil; sua aplicação caracteriza método avançado/análise de engenharia de incêndio e requer justificativa perante o Corpo de Bombeiros. A contribuição da fôrma é desprezada. A validação experimental cobre telas dúcteis (classe B ou C), fôrmas até 80 mm, pórticos contraventados com ligações flexíveis e TRRF até 120 min. As dimensões das nervuras da fôrma devem ser conferidas com o catálogo do fabricante.");
        return _doc;
    }


    // ================================================================= blocos
    private void Head(ProjectInput inp)
    {
        string meta = H(inp.PanelName) + (string.IsNullOrWhiteSpace(inp.Engineer) ? "" : " · " + H(inp.Engineer))
                    + " · " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", Br);
        _doc.Blocks.Add(new TitleBlock("Memorial de cálculo · laje mista em situação de incêndio · ação de membrana", H(inp.ProjectName), meta));
    }

    private void Summary(ProjectInput inp, DesignResult r)
    {
        bool ok = r.QfiRd >= r.Load.QfiSd;
        int warns = r.Checks.Count(c => c.Status == CheckStatus.Warning);
        var figures = new[]
        {
            new SummaryFigure(F(r.QfiRd), "kN/m² resistência"),
            new SummaryFigure(F(r.Load.QfiSd), "kN/m² solicitação"),
            new SummaryFigure($"{F(r.Utilization * 100, 0)}%", "utilização", Highlight: true),
        };
        string text = $"{(ok ? "O painel atende" : "O painel não atende")} ao critério de capacidade portante para {F(inp.FireTime, 0)} min de incêndio-padrão"
                    + (warns > 0 ? $", com {warns} ponto(s) de atenção listados ao final." : ".");
        _doc.Blocks.Add(new SummaryBlock(ok, figures, text));
    }

    private void Section(string title) => _doc.Blocks.Add(new HeadingBlock(title));

    private void Step(string title, string intro)
    {
        _step++;
        _doc.Blocks.Add(new HeadingBlock(title, _step));
        if (!string.IsNullOrEmpty(intro)) Paragraph(intro);
    }

    private void Eq(string symbolic, string substituted, string result, string unit, string? note = null) =>
        _doc.Blocks.Add(new EquationBlock(symbolic, substituted, result, unit, string.IsNullOrEmpty(note) ? null : note));

    private void Paragraph(string text) => _doc.Blocks.Add(new ParagraphBlock(text));

    private void Note(string text) => Paragraph(text);

    private void Verdict(bool ok, string text) => _doc.Blocks.Add(new VerdictBlock(ok, text));

    private void Figure(ReportFigure figure, double aspect, string caption) => _doc.Blocks.Add(new FigureBlock(figure, aspect, caption));

    private void Table(string[] header, string[][] rows) => _doc.Blocks.Add(new TableBlock(header, rows));
}
