namespace SteelDeckFire.Core.Calc;

/// <summary>
/// Propriedades de perfil I laminado duplamente simétrico com concordâncias alma–mesa de raio r.
/// Dimensões em mm; resultados em mm² e mm³.
/// </summary>
public static class SectionProperties
{
    /// <summary>Área de uma concordância: quadrado r × r menos um quarto de círculo.</summary>
    public static double FilletArea(double r) => (1 - Math.PI / 4) * r * r;

    /// <summary>Distância do centroide da concordância à face interna da mesa.</summary>
    public static double FilletCentroid(double r) => r * (10 - 3 * Math.PI) / (12 - 3 * Math.PI);

    /// <summary>A = 2·bf·tf + (d − 2tf)·tw + 4·(1 − π/4)·r²</summary>
    public static double Area(double d, double bf, double tw, double tf, double r) =>
        2 * bf * tf + (d - 2 * tf) * tw + 4 * FilletArea(r);

    /// <summary>Zx = bf·tf·(d − tf) + tw·(d − 2tf)²/4 + 4·A_conc·(d/2 − tf − ȳ_conc)</summary>
    public static double Zx(double d, double bf, double tw, double tf, double r) =>
        bf * tf * (d - tf) + tw * (d - 2 * tf) * (d - 2 * tf) / 4 + FilletsZx(d, tf, r);

    /// <summary>Parcela das quatro concordâncias no módulo plástico.</summary>
    public static double FilletsZx(double d, double tf, double r) =>
        4 * FilletArea(r) * (d / 2 - tf - FilletCentroid(r));
}
