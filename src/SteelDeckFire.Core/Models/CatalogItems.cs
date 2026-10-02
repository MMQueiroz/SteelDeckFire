namespace SteelDeckFire.Core.Models;

/// <summary>
/// Geometria da telha-fôrma segundo a nomenclatura da EN 1994-1-2, Anexo D (Fig. D.1):
/// l1 = largura da nervura de concreto no topo da fôrma, l2 = largura da nervura de concreto no fundo,
/// l3 = largura da mesa superior da fôrma (entre nervuras de concreto), h2 = altura da fôrma.
/// Passo da onda = l1 + l3.
/// </summary>
public sealed record DeckProfile(
    string Name,
    double H2,          // mm
    double L1,          // mm
    double L2,          // mm
    double L3,          // mm
    double[] Thicknesses, // mm
    double CoverWidth,  // largura útil, mm
    string Note)
{
    public double Pitch => L1 + L3;
}

/// <summary>Tela soldada: áreas por metro nas duas direções (mm²/m) e escoamento (MPa).</summary>
public sealed record MeshType(
    string Name,
    double AsLongitudinal, // mm²/m
    double AsTransversal,  // mm²/m
    double Fy,             // MPa
    bool ColdWorked,       // fio trefilado (CA-60) → curva de redução "cold worked"
    string Note);

/// <summary>
/// Perfil I laminado: d, bf, tw, tf e r (raio de concordância) em mm; massa nominal em kg/m.
/// A e Zx são calculados pela geometria.
/// </summary>
public sealed record SteelSection(
    string Name,
    double H, double B, double Tw, double Tf, double R,
    double MassKgM)
{
    public double AreaCm2 => Calc.SectionProperties.Area(H, B, Tw, Tf, R) / 100.0;
    public double ZxCm3 => Calc.SectionProperties.Zx(H, B, Tw, Tf, R) / 1000.0;

    /// <summary>Massa pela área calculada (aço 7850 kg/m³).</summary>
    public double ComputedMassKgM => AreaCm2 * 0.785;
}
