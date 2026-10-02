namespace SteelDeckFire.Core.Fire;

public static class FireTables
{
    /// <summary>Curva-padrão ISO 834 / NBR 14432: θg = 20 + 345·log10(8t + 1), t em minutos.</summary>
    public static double IsoGas(double tMin) => 20.0 + 345.0 * Math.Log10(8.0 * tMin + 1.0);

    // Distribuição de temperatura em laje (h_eff ≤ 150 mm) sob incêndio-padrão,
    // conforme Tabela 3-1 do guia FRACOF (estabelecida a partir da EN 1992-1-2).
    // x = distância à face exposta (mm).
    private static readonly double[] SlabX = { 2.5, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150 };
    private static readonly double[] SlabT = { 30, 60, 90, 120, 180 };
    private static readonly double[,] SlabTheta =
    {
        // 30    60    90   120   180 min
        { 681,  837,  918,  973, 1048 }, // 2,5
        { 509,  682,  778,  844,  933 }, // 10
        { 345,  519,  621,  694,  796 }, // 20
        { 233,  395,  497,  571,  677 }, // 30
        { 156,  300,  398,  470,  577 }, // 40
        { 106,  228,  318,  388,  492 }, // 50
        {  76,  172,  254,  320,  420 }, // 60
        {  56,  130,  203,  263,  359 }, // 70
        {  42,  101,  161,  217,  307 }, // 80
        {  33,   80,  129,  178,  262 }, // 90
        {  27,   64,  104,  146,  224 }, // 100
        {  24,   51,   86,  121,  191 }, // 110
        {  22,   42,   71,  101,  163 }, // 120
        {  21,   35,   60,   86,  140 }, // 130
        {  21,   30,   50,   74,  122 }, // 140
        {  20,   27,   43,   64,  107 }, // 150
    };

    public const double SlabTableMinTime = 30, SlabTableMaxTime = 180;

    /// <summary>Temperatura na laje à distância x (mm) da face exposta, no tempo t (min). Interpolação bilinear.</summary>
    public static double SlabTemperature(double xMm, double tMin)
    {
        double x = Math.Clamp(xMm, SlabX[0], SlabX[^1]);
        double t = Math.Clamp(tMin, SlabT[0], SlabT[^1]);
        int i = FindInterval(SlabX, x);
        int j = FindInterval(SlabT, t);
        double fx = (x - SlabX[i]) / (SlabX[i + 1] - SlabX[i]);
        double ft = (t - SlabT[j]) / (SlabT[j + 1] - SlabT[j]);
        double a = Lerp(SlabTheta[i, j], SlabTheta[i + 1, j], fx);
        double b = Lerp(SlabTheta[i, j + 1], SlabTheta[i + 1, j + 1], fx);
        return Lerp(a, b, ft);
    }

    // Fatores de redução do aço estrutural (EN 1993-1-2 Tab. 3.1 / NBR 14323): k_y,θ
    private static readonly double[] StT = { 20, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200 };
    private static readonly double[] Ky = { 1, 1, 1, 1, 1, 0.78, 0.47, 0.23, 0.11, 0.06, 0.04, 0.02, 0 };
    // k_u,θ – resistência última (EN 1994-1-2 Tab. 3.2), usado para os conectores
    private static readonly double[] Ku = { 1.25, 1.25, 1.25, 1.25, 1.0, 0.78, 0.47, 0.23, 0.11, 0.06, 0.04, 0.02, 0 };
    // Armadura trefilada a frio (EN 1992-1-2 Tab. 3.2a, classe N, "cold worked")
    private static readonly double[] KsCold = { 1, 1, 1, 1, 0.94, 0.67, 0.40, 0.12, 0.11, 0.08, 0.05, 0.03, 0 };
    // Armadura laminada a quente (EN 1992-1-2 Tab. 3.2a, classe N, "hot rolled")
    private static readonly double[] KsHot = { 1, 1, 1, 1, 1, 0.78, 0.47, 0.23, 0.11, 0.06, 0.04, 0.02, 0 };

    public static double KyTheta(double theta) => Interp(StT, Ky, theta);
    public static double KuTheta(double theta) => Interp(StT, Ku, theta);
    public static double KsTheta(double theta, bool coldWorked) => Interp(StT, coldWorked ? KsCold : KsHot, theta);

    /// <summary>Calor específico do aço (EN 1993-1-2, 3.4.1.2), J/(kg·K).</summary>
    public static double SteelSpecificHeat(double theta)
    {
        if (theta < 600) return 425 + 0.773 * theta - 1.69e-3 * theta * theta + 2.22e-6 * theta * theta * theta;
        if (theta < 735) return 666 + 13002.0 / (738 - theta);
        if (theta < 900) return 545 + 17820.0 / (theta - 731);
        return 650;
    }

    /// <summary>Espessura efetiva mínima para o critério de isolamento térmico I (EN 1994-1-2, Tab. D.6).</summary>
    public static double MinHeffForInsulation(double tMin) => tMin switch
    {
        <= 30 => 60,
        <= 60 => 80,
        <= 90 => 100,
        <= 120 => 120,
        <= 180 => 150,
        _ => 175
    };

    private static double Interp(double[] xs, double[] ys, double x)
    {
        if (x <= xs[0]) return ys[0];
        if (x >= xs[^1]) return ys[^1];
        int i = FindInterval(xs, x);
        return Lerp(ys[i], ys[i + 1], (x - xs[i]) / (xs[i + 1] - xs[i]));
    }

    private static int FindInterval(double[] xs, double x)
    {
        for (int i = 0; i < xs.Length - 1; i++)
            if (x <= xs[i + 1]) return i;
        return xs.Length - 2;
    }

    private static double Lerp(double a, double b, double f) => a + (b - a) * f;
}
