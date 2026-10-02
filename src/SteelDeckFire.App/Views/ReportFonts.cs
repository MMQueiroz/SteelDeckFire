using System.Drawing;

namespace SteelDeckFire.App.Views;

/// <summary>
/// Fontes do memorial em unidades de mundo (1/100 pol.), para que o mesmo layout sirva à tela e à impressora.
/// </summary>
internal sealed class ReportFonts : IDisposable
{
    public sealed class Family : IDisposable
    {
        private readonly string _name;
        private readonly Dictionary<(float, bool, bool), Font> _cache = new();
        public Family(string name) => _name = name;

        public Font Get(float size, bool italic = false, bool bold = false)
        {
            if (!_cache.TryGetValue((size, italic, bold), out var f))
            {
                var style = (italic ? FontStyle.Italic : 0) | (bold ? FontStyle.Bold : 0);
                f = new Font(_name, size, style, GraphicsUnit.World);
                _cache[(size, italic, bold)] = f;
            }
            return f;
        }

        public void Dispose()
        {
            foreach (var f in _cache.Values) f.Dispose();
            _cache.Clear();
        }
    }

    public Family Serif { get; } = new("Cambria");
    public Family Sans { get; } = new("Segoe UI");

    public static readonly StringFormat Format = CreateFormat();

    private static StringFormat CreateFormat()
    {
        var f = (StringFormat)StringFormat.GenericTypographic.Clone();
        f.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoClip | StringFormatFlags.NoWrap;
        return f;
    }

    public static float Measure(Graphics g, string text, Font font) =>
        text.Length == 0 ? 0 : g.MeasureString(text, font, PointF.Empty, Format).Width;

    public void Dispose()
    {
        Serif.Dispose();
        Sans.Dispose();
    }
}
