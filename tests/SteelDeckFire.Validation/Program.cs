using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Models;

// Validação contra o exemplo resolvido do FRACOF Design Guide (zona B, 9 × 12 m, R60, tela ST 25C)
var inp = ProjectInput.FracofExample();
var r = FireDesign.Run(inp);
var m = r.Membrane;
int fails = 0;

void Check(string name, double value, double expected, double tolRel = 0.01)
{
    bool ok = Math.Abs(value - expected) <= Math.Abs(expected) * tolRel + 1e-9;
    if (!ok) fails++;
    Console.WriteLine($"{(ok ? "OK  " : "FALHA")} {name,-22} calc = {value,14:F4}   guia = {expected,14:F4}");
}

Check("q_fi,Sd (kN/m²)", r.Load.QfiSd, 6.35);
Check("h_eff (mm)", r.Thermal.Heff, 95, 0.01);
Check("θ2 (°C)", r.Thermal.Theta2, 837);
// O guia lê 77 °C; a interpolação linear da própria tabela em x = h_eff dá ≈ 72 °C (efeito < 0,5 % em q_fi,Rd).
Check("θ1 (°C)", r.Thermal.Theta1, 77, 0.08);
Check("θs (°C)", r.Thermal.ThetaS, 151, 0.03);
Check("g01", m.G01, 0.597);
Check("M0 (N·mm/mm)", m.M0, 3466.5);
Check("μ", m.Mu, 1.0);
Check("n", m.N, 0.427);
Check("p_fi (kN/m²)", m.Pfi, 0.794);
Check("w (mm)", m.W, 658.5);
Check("k", m.K_, 1.194);
Check("A", m.CoefA, 1978359);
Check("B", m.CoefB, 7242376);
Check("C", m.CoefC, 2305602);
Check("D", m.CoefD, 388465);
Check("b", m.B, 0.909);
Check("e1b", m.E1b, 0.935);
Check("e1m", m.E1m, 5.802);
Check("e2b", m.E2b, 0.991);
Check("e2m", m.E2m, 2.980);
Check("e", m.E, 6.130);
Check("q_laje (kN/m²)", m.QSlab, 4.87);
Check("θ viga (°C)", r.Beams.Theta, 938.6, 0.01);
Check("M_fi,Rd viga (kN·m)", r.Beams.MfiRd / 1e6, 51.5, 0.02);
Check("q_vigas (kN/m²)", r.Beams.QBeams, 1.70, 0.02);
Check("q_fi,Rd (kN/m²)", r.QfiRd, 6.57, 0.01);
Check("M viga perím. L2 (kN·m)", r.Perimeter[0].MSd, 668.5, 0.02);
Check("M viga perím. L1 (kN·m)", r.Perimeter[1].MSd, 404.4, 0.02);

Console.WriteLine();
Console.WriteLine("Zona A (9 × 9 m):");
var inpA = ProjectInput.FracofExample(); inpA.L2 = 9; inpA.UnprotectedBeams = 2;
var rA = FireDesign.Run(inpA);
Check("b (compressão)", rA.Membrane.B, 1.232);
Check("e", rA.Membrane.E, 5.475);
Check("q_laje (kN/m²)", rA.Membrane.QSlab, 5.62);

Console.WriteLine();
Console.WriteLine(fails == 0 ? "VALIDAÇÃO CONCLUÍDA SEM DIVERGÊNCIAS" : $"{fails} DIVERGÊNCIA(S)");

var sweep = FireDesign.TimeSweep(inp, 30);
foreach (var p in sweep) Console.WriteLine($"t={p.Time,4} min  q_laje={p.QSlab,6:F2}  q_vigas={p.QBeams,6:F2}  total={p.QTotal,6:F2}  θviga={p.ThetaBeam,6:F0}");
var memorial = SteelDeckFire.Core.Report.ReportBuilder.Build(inp, r, FireDesign.TimeSweep(inp));
Console.WriteLine($"Memorial gerado: {memorial.Blocks.Count} blocos");
var d = FireDesign.Run(new ProjectInput());
Console.WriteLine($"Padrão MF-75/Q196: heff={d.Thermal.Heff:F1} θs={d.Thermal.ThetaS:F0} n={d.Membrane.N:F3} b={d.Membrane.B:F3} e={d.Membrane.E:F2} qlaje={d.Membrane.QSlab:F2} qvig={d.Beams.QBeams:F2} θv={d.Beams.Theta:F0} qRd={d.QfiRd:F2} qSd={d.Load.QfiSd:F2}");
foreach (var c in d.Checks) Console.WriteLine($"  [{c.Status}] {c.Title}: {c.Detail}");
var sq = new ProjectInput { L1 = 12, L2 = 8, UnprotectedBeams = 2 };
var ds = FireDesign.Run(sq);
Console.WriteLine($"L1>L2: long={ds.Membrane.LongIsL2} a={ds.Membrane.A:F2} e={ds.Membrane.E:F2} qRd={ds.QfiRd:F2}");
return fails == 0 ? 0 : 1;
