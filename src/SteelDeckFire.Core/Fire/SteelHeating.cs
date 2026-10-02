namespace SteelDeckFire.Core.Fire;

/// <summary>
/// Temperatura de elemento de aço sem proteção sob incêndio-padrão, pelo método incremental
/// (EN 1994-1-2, 4.3.4.2.2 / NBR 14323):
///   Δθa = k_sh · (A/V) / (c_a·ρ_a) · h_net · Δt
///   h_net = α_c(θg − θa) + Φ·ε_m·ε_f·σ[(θg+273)⁴ − (θa+273)⁴]
/// </summary>
public static class SteelHeating
{
    private const double Rho = 7850;      // kg/m³
    private const double AlphaC = 25;     // W/(m²K)
    private const double EpsM = 0.7;      // emissividade do aço
    private const double EpsF = 1.0;      // emissividade do fogo
    private const double Sigma = 5.67e-8;
    private const double Dt = 2.0;        // s

    /// <summary>Temperatura no tempo tMin para um fator de massividade efetivo k_sh·(A/V) em 1/m.</summary>
    public static double Temperature(double kshAv, double tMin)
    {
        double theta = 20;
        double tEnd = tMin * 60.0;
        for (double t = 0; t < tEnd; t += Dt)
        {
            double tm = (t + Dt / 2) / 60.0;
            double tg = FireTables.IsoGas(tm);
            double hnet = AlphaC * (tg - theta)
                        + EpsM * EpsF * Sigma * (Math.Pow(tg + 273, 4) - Math.Pow(theta + 273, 4));
            theta += kshAv / (FireTables.SteelSpecificHeat(theta) * Rho) * hnet * Dt;
        }
        return theta;
    }

    /// <summary>
    /// Fator de sombra para perfil I simétrico aquecido em 3 lados (laje sobre a mesa superior):
    /// k_sh = 0,9·(H + 0,5B)/(H + 1,5B − t_w). Reproduz os valores do exemplo FRACOF (IPE 400 → 0,667).
    /// </summary>
    public static double ShadowFactor(double h, double b, double tw) => 0.9 * (h + 0.5 * b) / (h + 1.5 * b - tw);

    /// <summary>Fator de massividade da mesa inferior: 2(b + t_f)/(b·t_f), em 1/m (dimensões em mm).</summary>
    public static double FlangeSectionFactor(double b, double tf) => 2.0 * (b + tf) / (b * tf) * 1000.0;

    /// <summary>Fator de massividade da alma: 2/t_w, em 1/m.</summary>
    public static double WebSectionFactor(double tw) => 2.0 / tw * 1000.0;
}
