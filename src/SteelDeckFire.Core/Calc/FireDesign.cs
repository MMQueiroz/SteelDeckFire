using SteelDeckFire.Core.Fire;
using SteelDeckFire.Core.Models;

namespace SteelDeckFire.Core.Calc;

/// <summary>
/// Verificação de painel de laje mista em incêndio considerando ação de membrana
/// (método simplificado de Bailey, conforme implementado no FRACOF / MACS+).
/// Unidades internas: N, mm, MPa. Resultados de carga em kN/m².
/// </summary>
public static class FireDesign
{
    public static DesignResult Run(ProjectInput inp) => Run(inp, inp.FireTime);

    public static DesignResult Run(ProjectInput inp, double tMin)
    {
        var r = new DesignResult();
        ComputeLoads(inp, r.Load);
        ComputeThermal(inp, tMin, r.Thermal);
        ComputeMembrane(inp, r.Thermal, r.Membrane);
        ComputeBeams(inp, tMin, r.Beams);

        r.QfiRd = r.Membrane.QSlab + r.Beams.QBeams;
        r.Utilization = r.QfiRd > 0 ? r.Load.QfiSd / r.QfiRd : double.PositiveInfinity;
        ComputePerimeter(inp, r);
        BuildChecks(inp, tMin, r);
        return r;
    }

    /// <summary>Resistência ao longo do tempo (30 a 180 min, passo de 5 min).</summary>
    public static List<TimePoint> TimeSweep(ProjectInput inp, double step = 5)
    {
        var list = new List<TimePoint>();
        for (double t = FireTables.SlabTableMinTime; t <= FireTables.SlabTableMaxTime + 1e-9; t += step)
        {
            var r = Run(inp, t);
            list.Add(new TimePoint(t, r.Membrane.QSlab, r.Beams.QBeams, r.QfiRd, r.Beams.Theta, r.Thermal.ThetaS));
        }
        return list;
    }

    // ---------------------------------------------------------------- Ações
    public static void ComputeLoads(ProjectInput inp, LoadResult o)
    {
        double h1 = inp.SlabThickness - inp.DeckH2;
        double pitch = inp.DeckL1 + inp.DeckL3;
        o.ConcreteEqThickness = h1 + inp.DeckH2 * (inp.DeckL1 + inp.DeckL2) / (2.0 * pitch);
        // peso aproximado da fôrma: t·7850 kg/m³ × fator de desenvolvimento 1,25
        o.DeckWeight = inp.DeckThickness / 1000.0 * 7850 * 1.25 * 9.81 / 1000.0;
        if (inp.SlabSelfWeightOverride > 0)
        {
            o.SlabSelfWeight = inp.SlabSelfWeightOverride;
            o.SlabWeightComputed = false;
        }
        else
        {
            o.SlabSelfWeight = inp.ConcreteWeight * o.ConcreteEqThickness / 1000.0 + o.DeckWeight;
            o.SlabWeightComputed = true;
        }
        o.PermanentTotal = o.SlabSelfWeight + inp.AdditionalDead + inp.BeamsSelfWeight;
        o.QfiSd = inp.GammaG * o.PermanentTotal + inp.PsiFire * inp.LiveLoad;
    }

    // ---------------------------------------------------------------- Térmica
    public static void ComputeThermal(ProjectInput inp, double tMin, ThermalResult o)
    {
        o.H2 = inp.DeckH2;
        o.H1 = inp.SlabThickness - inp.DeckH2;
        double ratio = (inp.DeckL1 + inp.DeckL2) / (inp.DeckL1 + inp.DeckL3);
        o.HeffFormulaA = o.H2 / o.H1 <= 1.5 && o.H1 > 40;
        o.Heff = o.HeffFormulaA ? o.H1 + 0.5 * o.H2 * ratio : o.H1 * (1 + 0.75 * ratio);
        o.HeffUsed = Math.Min(o.Heff, 150);
        o.HeffMin = FireTables.MinHeffForInsulation(tMin);
        o.Theta2 = FireTables.SlabTemperature(2.5, tMin);
        o.Theta1 = FireTables.SlabTemperature(o.HeffUsed, tMin);
        o.XMesh = o.HeffUsed - inp.MeshDepth;
        o.ThetaS = FireTables.SlabTemperature(o.XMesh, tMin);
        o.Ks = FireTables.KsTheta(o.ThetaS, inp.MeshColdWorked);
        o.FsyTheta = o.Ks * inp.MeshFy;
    }

    // ---------------------------------------------------------------- Membrana
    public static void ComputeMembrane(ProjectInput inp, ThermalResult th, MembraneResult m)
    {
        m.LongIsL2 = inp.L2 >= inp.L1;
        m.Lmm = Math.Max(inp.L1, inp.L2) * 1000.0;
        m.lmm = Math.Min(inp.L1, inp.L2) * 1000.0;
        double asL = (m.LongIsL2 ? inp.AsAlongL2 : inp.AsAlongL1) / 1000.0; // mm²/mm
        double asl = (m.LongIsL2 ? inp.AsAlongL1 : inp.AsAlongL2) / 1000.0;
        m.As = asL;
        m.K = asl / asL;
        double fsy = th.FsyTheta, fc = inp.Fck, d = inp.MeshDepth;
        double L = m.Lmm, l = m.lmm;

        m.T0 = m.As * fsy;
        m.G01 = 1 - 2 * m.K * m.As * fsy / (0.85 * fc * d);
        m.G02 = 1 - 2 * m.As * fsy / (0.85 * fc * d);
        m.M0 = m.As * fsy * d * (3 + m.G02) / 4.0;
        m.MuM0 = m.K * m.As * fsy * d * (3 + m.G01) / 4.0;
        m.Mu = m.K * (3 + m.G01) / (3 + m.G02);
        m.A = L / l;
        double a = m.A, mu = m.Mu;
        m.N = (Math.Sqrt(3 * mu * a * a + 1) - 1) / (2 * mu * a * a);
        double n = m.N;
        m.Pfi = 6 * m.M0 / (n * n * L * L) * 1000.0; // N/mm² → kN/m²

        // Deslocamento de cálculo
        m.WThermal = inp.ThermalAlpha * (th.Theta2 - th.Theta1) * l * l / (19.2 * th.HeffUsed);
        m.WMechRaw = Math.Sqrt(0.5 * inp.MeshFy / inp.MeshE * 3 * L * L / 8.0);
        m.WMech = Math.Min(m.WMechRaw, l / 30.0);
        m.WLimitTotal = (L + l) / 30.0;
        m.W = Math.Min(m.WThermal + m.WMech, m.WLimitTotal);

        // Parâmetros das forças de membrana
        double k = 4 * n * a * a * (1 - 2 * n) / (4 * n * n * a * a + 1) + 1;
        m.K_ = k;
        m.Ld2 = n * n * L * L + l * l / 4.0;
        m.CoefA = (m.Ld2 * (3 * k + 2) / (3 * (1 + k)) - n * L * L / 2.0) / (2 * (1 + k));
        m.CoefB = k * k / (2 * (1 + k)) * (n * L * L / 2.0 - k * m.Ld2 / (3 * (1 + k)));
        m.CoefC = (k - 1) * l * l / (16 * n);
        m.CoefD = L * L / 8.0 * (1 - 2 * n) * (1 - 2 * n);
        m.BTension = l * l / (8 * m.K * (m.CoefA + m.CoefB + m.CoefC - m.CoefD));
        m.BCompression = (0.85 * fc * 0.45 * d - (1 + m.K) * m.T0 / 2.0) / (k * m.K * m.T0);
        m.CompressionGoverns = m.BCompression < m.BTension;
        m.B = Math.Max(0, Math.Min(m.BTension, m.BCompression));
        double b = m.B, K = m.K;

        m.Alpha1 = 2 * m.G01 / (3 + m.G01);
        m.Beta1 = (1 - m.G01) / (3 + m.G01);
        m.Alpha2 = 2 * m.G02 / (3 + m.G02);
        m.Beta2 = (1 - m.G02) / (3 + m.G02);

        m.E1b = 2 * n * (1 + m.Alpha1 * b * (k - 1) / 2 - m.Beta1 * b * b * (k * k - k + 1) / 3)
              + (1 - 2 * n) * (1 - m.Alpha1 * b - m.Beta1 * b * b);
        m.E1m = 4 * b / (3 + m.G01) * (m.W / d) * ((1 - 2 * n) + n * (3 * k + 2 - k * k * k) / (3 * (1 + k) * (1 + k)));
        m.E2b = 1 + m.Alpha2 * b * K * (k - 1) / 2 - m.Beta2 * b * b * K * K * (k * k - k + 1) / 3;
        m.E2m = 4 * b * K / (3 + m.G02) * (m.W / d) * (2 + 3 * k - k * k * k) / (6 * (1 + k) * (1 + k));
        m.E1 = m.E1b + m.E1m;
        m.E2 = m.E2b + m.E2m;
        m.E = m.E1 - (m.E1 - m.E2) / (1 + 2 * mu * a * a);
        m.QSlab = m.E * m.Pfi;
    }

    // ---------------------------------------------------------------- Vigas internas
    public static void ComputeBeams(ProjectInput inp, double tMin, BeamResult o)
    {
        o.Count = Math.Max(0, inp.UnprotectedBeams);
        o.Spacing = inp.L2 * 1000.0 / (o.Count + 1);
        o.Ksh = SteelHeating.ShadowFactor(inp.BeamH, inp.BeamB, inp.BeamTw);
        o.AvFlange = SteelHeating.FlangeSectionFactor(inp.BeamB, inp.BeamTf);
        o.AvWeb = SteelHeating.WebSectionFactor(inp.BeamTw);
        o.KshAvFlange = o.Ksh * o.AvFlange;
        o.KshAvWeb = o.Ksh * o.AvWeb;
        if (o.Count == 0) return;

        o.ThetaImposed = inp.BeamTemperatureOverride > 0;
        o.Theta = o.ThetaImposed ? inp.BeamTemperatureOverride : SteelHeating.Temperature(o.KshAvFlange, tMin);
        o.Ky = FireTables.KyTheta(o.Theta);
        o.ThetaStud = 0.8 * o.Theta;
        o.Ku = FireTables.KuTheta(o.ThetaStud);

        double area = inp.BeamArea * 100.0; // mm²
        o.Fa = area * inp.BeamFy * o.Ky;
        o.Beff = Math.Min(inp.L1 * 1000.0 / 4.0, o.Spacing);
        o.Hu = o.Fa / (o.Beff * inp.Fck);
        o.HuWithinSlab = o.Hu <= inp.SlabThickness - inp.DeckH2;
        o.NcTheta = o.Ky > 0 ? inp.ShearConnection * o.Ku * 1.25 / o.Ky : 0;
        o.FullConnection = o.NcTheta >= 1.0;
        o.MFull = o.Fa * (inp.BeamH / 2.0 + inp.SlabThickness - o.Hu / 2.0);
        o.MplA = inp.BeamZx * 1000.0 * inp.BeamFy * o.Ky;
        o.MfiRd = o.FullConnection ? o.MFull : o.MplA + o.NcTheta * (o.MFull - o.MplA);
        double L1 = inp.L1 * 1000.0;
        o.QBeams = 8 * o.MfiRd / (L1 * L1 * o.Spacing) * 1000.0; // kN/m²
    }

    // ---------------------------------------------------------------- Vigas de perímetro
    private static void ComputePerimeter(ProjectInput inp, DesignResult r)
    {
        var m = r.Membrane;
        double q = r.Load.QfiSd;
        double L1 = inp.L1, L2 = inp.L2;
        // Momentos da laje por unidade de largura (kN·m/m) para barras paralelas a L1 e a L2
        double mBarsL2 = (m.LongIsL2 ? m.M0 : m.MuM0) / 1000.0;
        double mBarsL1 = (m.LongIsL2 ? m.MuM0 : m.M0) / 1000.0;
        double mub = r.Beams.MfiRd / 1e6; // kN·m
        int n = r.Beams.Count;

        // Vigas de perímetro perpendiculares às vigas internas (vão L2): charneira paralela a L1 em L2/2
        double l1eff = Math.Max(0, L1 - 2 * L2 / 8.0);
        double mb2 = Math.Max(0, (q * L1 * L2 * L2 - 8 * mBarsL2 * l1eff) / 12.0);
        r.Perimeter.Add(new PerimeterBeamResult
        { Name = "Vigas de perímetro perpendiculares às internas (vão L2)", Span = L2, Leff = l1eff, MSd = mb2, VSd = 4 * mb2 / L2 });

        // Vigas de perímetro paralelas às internas (vão L1): charneira paralela a L2 em L1/2
        double l2eff = Math.Max(0, L2 - n * r.Beams.Beff / 1000.0 - 2 * L1 / 8.0);
        double mb1 = Math.Max(0, (q * L2 * L1 * L1 - 8 * mBarsL1 * l2eff - 8 * n * mub) / 12.0);
        r.Perimeter.Add(new PerimeterBeamResult
        { Name = "Vigas de perímetro paralelas às internas (vão L1)", Span = L1, Leff = l2eff, MSd = mb1, VSd = 4 * mb1 / L1 });
    }

    // ---------------------------------------------------------------- Verificações
    private static void BuildChecks(ProjectInput inp, double tMin, DesignResult r)
    {
        var c = r.Checks;
        var th = r.Thermal; var m = r.Membrane;

        c.Add(new Check("Capacidade portante do painel (R)",
            r.QfiRd >= r.Load.QfiSd ? CheckStatus.Ok : CheckStatus.Fail,
            $"q_fi,Rd = {r.QfiRd:F2} kN/m² × q_fi,Sd = {r.Load.QfiSd:F2} kN/m² (utilização {r.Utilization:P0})."));

        c.Add(new Check("Isolamento térmico (I) – espessura efetiva",
            th.Heff >= th.HeffMin ? CheckStatus.Ok : CheckStatus.Fail,
            $"h_eff = {th.Heff:F1} mm; mínimo para {tMin:F0} min = {th.HeffMin:F0} mm (EN 1994-1-2, Tab. D.6)."));

        c.Add(new Check("Ductilidade da tela",
            inp.MeshDuctile ? CheckStatus.Ok : CheckStatus.Warning,
            inp.MeshDuctile
                ? "Tela declarada como classe B ou C."
                : "O método foi calibrado com telas dúcteis (classe B ou C). Telas CA-60 trefiladas usuais têm baixo alongamento: o resultado não está coberto pela validação experimental."));

        bool deckOk = inp.DeckH2 <= 80;
        bool h1Ok = th.H1 >= 60 && th.H1 <= 90;
        c.Add(new Check("Escopo geométrico do método",
            deckOk && h1Ok ? CheckStatus.Ok : CheckStatus.Warning,
            $"Fôrma h2 = {inp.DeckH2:F0} mm (limite 80); concreto acima da fôrma h1 = {th.H1:F0} mm (faixa validada 60–90)."));

        c.Add(new Check("Proporção do painel",
            m.A <= 2.0 ? CheckStatus.Ok : CheckStatus.Warning,
            $"a = L/l = {m.A:F2}. O ganho de membrana cai com a proporção; acima de 2 o painel tende a trabalhar em uma direção."));

        c.Add(new Check("TRRF dentro da faixa validada",
            tMin <= 120 ? CheckStatus.Ok : CheckStatus.Warning,
            $"TRRF = {tMin:F0} min (método validado até 120 min)."));

        if (m.Mu > 1.0)
            c.Add(new Check("Ortotropia da tela", CheckStatus.Warning,
                $"μ = {m.Mu:F2} > 1: a direção de menor capacidade deveria coincidir com o vão menor."));

        if (r.Beams.Count > 0 && !r.Beams.HuWithinSlab)
            c.Add(new Check("Bloco comprimido das vigas internas", CheckStatus.Warning,
                $"h_u = {r.Beams.Hu:F1} mm excede o concreto acima da fôrma ({th.H1:F0} mm)."));

        c.Add(new Check("Modo de ruptura governante",
            CheckStatus.Ok,
            m.CompressionGoverns
                ? "Esmagamento do concreto nos cantos (b limitado pela compressão)."
                : "Fratura da tela na fissura central, através do vão menor."));

        c.Add(new Check("Vigas de perímetro", CheckStatus.Warning,
            "Devem ser protegidas para o TRRF com temperatura crítica definida pelos esforços da seção 12 do memorial."));
    }
}
