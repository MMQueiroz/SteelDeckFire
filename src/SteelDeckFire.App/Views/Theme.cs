using System.Drawing;
using System.Globalization;

namespace SteelDeckFire.App.Views;

/// <summary>Paleta: grafite para estrutura, azul para o que é protegido/comprimido, vermelho-fogo para o que aquece/traciona.</summary>
internal static class Theme
{
    public static readonly Color Paper = Color.FromArgb(250, 251, 252);
    public static readonly Color Ink = Color.FromArgb(29, 40, 48);
    public static readonly Color Muted = Color.FromArgb(90, 107, 119);
    public static readonly Color Rule = Color.FromArgb(214, 221, 226);
    public static readonly Color Guard = Color.FromArgb(44, 106, 158);        // viga protegida / compressão
    public static readonly Color Heat = Color.FromArgb(184, 67, 47);          // viga sem proteção / tração
    public static readonly Color Good = Color.FromArgb(46, 125, 79);
    public static readonly Color Amber = Color.FromArgb(168, 107, 0);
    public static readonly Color TensionFill = Color.FromArgb(70, 232, 150, 120);
    public static readonly Color CompressionFill = Color.FromArgb(40, 44, 106, 158);
    public static readonly Color Concrete = Color.FromArgb(206, 210, 212);
    public static readonly Color Rib = Color.FromArgb(228, 232, 235);

    public static readonly CultureInfo Br = new("pt-BR");
    public static string F(double v, int dec = 2) => v.ToString("N" + dec, Br);

    public static Font UiFont(float size, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);
}
