namespace SteelDeckFire.Core.Calc;

public enum CheckStatus { Ok, Warning, Fail }

public sealed record Check(string Title, CheckStatus Status, string Detail);

public sealed class LoadResult
{
    public double ConcreteEqThickness;   // mm (volume de concreto por m²)
    public double SlabSelfWeight;        // kN/m²
    public bool SlabWeightComputed;
    public double DeckWeight;            // kN/m²
    public double PermanentTotal;        // kN/m² (característico)
    public double QfiSd;                 // kN/m²
}

public sealed class ThermalResult
{
    public double H1;          // mm – concreto acima da fôrma
    public double H2;          // mm
    public bool HeffFormulaA;  // true: h1 + 0,5h2(l1+l2)/(l1+l3)
    public double Heff;        // mm
    public double HeffUsed;    // mm (limitada a 150 para a tabela)
    public double HeffMin;     // mm (isolamento)
    public double Theta1, Theta2, ThetaS; // °C
    public double XMesh;       // mm – posição da tela a partir da face exposta
    public double Ks;          // fator de redução da tela
    public double FsyTheta;    // MPa
}

/// <summary>Valores intermediários do método de Bailey (FRACOF).</summary>
public sealed class MembraneResult
{
    public double Lmm, lmm;          // vão maior e menor (mm)
    public bool LongIsL2;            // true se L (maior) = L2
    public double As;                // mm²/mm – barras paralelas a L
    public double K;                 // relação As(paralela a l)/As(paralela a L)
    public double T0;                // N/mm – As·fsy,θ
    public double G01, G02;
    public double M0;                // N·mm/mm – momento das barras paralelas a L
    public double MuM0;              // N·mm/mm – momento das barras paralelas a l
    public double Mu;
    public double A;                 // L/l
    public double N;
    public double Pfi;               // kN/m²
    public double WThermal, WMechRaw, WMech, WLimitTotal, W; // mm
    public double K_;                // parâmetro k
    public double Ld2;               // mm²
    public double CoefA, CoefB, CoefC, CoefD;
    public double BTension, BCompression, B;
    public bool CompressionGoverns;
    public double Alpha1, Beta1, Alpha2, Beta2;
    public double E1b, E1m, E2b, E2m, E1, E2, E;
    public double QSlab;             // kN/m²
    /// <summary>Posição (fração do comprimento da diagonal, a partir do canto) onde a força muda de compressão para tração: k/(1+k).</summary>
    public double DiagonalZeroFraction => K_ / (1 + K_);
}

public sealed class BeamResult
{
    public int Count;
    public double Spacing;           // mm
    public double KshAvFlange, KshAvWeb, Ksh, AvFlange, AvWeb;
    public double Theta;             // °C
    public bool ThetaImposed;
    public double Ky, ThetaStud, Ku;
    public double Fa;                // N
    public double Beff;              // mm
    public double Hu;                // mm
    public double NcTheta;
    public bool FullConnection;
    public double MFull, MplA, MfiRd; // N·mm
    public double QBeams;            // kN/m²
    public bool HuWithinSlab;
}

public sealed class PerimeterBeamResult
{
    public string Name = "";
    public double Span;     // m
    public double Leff;     // m
    public double MSd;      // kN·m
    public double VSd;      // kN
}

public sealed class DesignResult
{
    public LoadResult Load = new();
    public ThermalResult Thermal = new();
    public MembraneResult Membrane = new();
    public BeamResult Beams = new();
    public List<PerimeterBeamResult> Perimeter = new();
    public double QfiRd;       // kN/m²
    public double Utilization; // QfiSd/QfiRd
    public List<Check> Checks = new();
    public bool Passed => Checks.All(c => c.Status != CheckStatus.Fail);
}

public sealed record TimePoint(double Time, double QSlab, double QBeams, double QTotal, double ThetaBeam, double ThetaMesh);
