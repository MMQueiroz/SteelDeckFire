using System.Drawing;
using System.Text;

namespace SteelDeckFire.App.Views;

/// <summary>Trecho de texto com estilo uniforme.</summary>
internal readonly record struct TextRun(string Text, bool Italic = false, bool Bold = false, int Shift = 0); // Shift: -1 subscrito, +1 sobrescrito

/// <summary>
/// Texto com subscrito, sobrescrito, itálico e negrito, com quebra de linha.
/// Marcação: <c>_{…}</c>, <c>^{…}</c>, <c>*…*</c>; <c>\</c> torna o caractere seguinte literal.
/// </summary>
internal sealed class RichText
{
    private readonly record struct Piece(string Text, Font Font, float Width, float Offset, Color Color);
    private readonly record struct Line(List<(Piece P, float X)> Pieces);

    private readonly List<Line> _lines = new();
    private readonly float _lineHeight, _halfLeading, _ascent;

    public float Height => _lines.Count * _lineHeight;
    public float Width { get; }

    public static List<TextRun> Parse(string s, bool italic = false, bool bold = false)
    {
        var runs = new List<TextRun>();
        var sb = new StringBuilder();
        int shift = 0;
        void Flush() { if (sb.Length > 0) { runs.Add(new TextRun(sb.ToString(), italic, bold, shift)); sb.Clear(); } }

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '\\' && i + 1 < s.Length) { sb.Append(s[++i]); continue; }
            if ((c == '_' || c == '^') && shift == 0 && i + 1 < s.Length && s[i + 1] == '{')
            {
                Flush(); shift = c == '_' ? -1 : 1; i++; continue;
            }
            if (c == '}' && shift != 0) { Flush(); shift = 0; continue; }
            if (c == '*') { Flush(); italic = !italic; continue; }
            sb.Append(c);
        }
        Flush();
        return runs;
    }

    public static List<TextRun> Plain(string s, bool bold = false) => new() { new TextRun(s, Bold: bold) };

    /// <param name="fonts">Fornece a fonte para (itálico, negrito, reduzida).</param>
    public RichText(Graphics g, IEnumerable<TextRun> runs, ReportFonts.Family fonts, float size, Color color, float maxWidth, float lineSpacing = 1.35f)
    {
        float small = size * 0.72f;
        var baseFont = fonts.Get(size, false, false);
        float natural = baseFont.Size * baseFont.FontFamily.GetLineSpacing(baseFont.Style) / baseFont.FontFamily.GetEmHeight(baseFont.Style);
        _lineHeight = natural * lineSpacing;
        _halfLeading = (_lineHeight - natural) / 2;
        _ascent = Ascent(baseFont);

        // Divide em palavras mantendo o espaço final em cada uma
        var pieces = new List<Piece>();
        var glue = new List<bool>(); // true quando a peça não pode ser separada da anterior
        foreach (var r in runs)
        {
            var f = fonts.Get(r.Shift == 0 ? size : small, r.Italic, r.Bold);
            float off = r.Shift switch { -1 => _ascent - Ascent(f) + size * 0.22f, 1 => _ascent - Ascent(f) - size * 0.38f, _ => _ascent - Ascent(f) };
            int start = 0;
            bool first = true;
            for (int i = 0; i <= r.Text.Length; i++)
            {
                bool end = i == r.Text.Length;
                if (!end && !(r.Text[i] == ' ' && (i + 1 == r.Text.Length || r.Text[i + 1] != ' '))) continue;
                int stop = end ? i : i + 1;
                if (stop > start)
                {
                    var t = r.Text[start..stop];
                    pieces.Add(new Piece(t, f, ReportFonts.Measure(g, t, f), off, color));
                    glue.Add(first && pieces.Count > 1 && !EndsWithSpace(pieces[^2].Text));
                    first = false;
                }
                start = stop;
            }
        }

        // Quebra de linha gulosa; grupos colados (ex.: "q" + "fi,Rd") se movem juntos
        var line = new List<(Piece, float)>();
        float x = 0, widest = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            int j = i + 1;
            float groupW = pieces[i].Width;
            while (j < pieces.Count && glue[j]) { groupW += pieces[j].Width; j++; }
            float trimmed = groupW - TrailingSpace(g, pieces[j - 1]);
            if (line.Count > 0 && x + trimmed > maxWidth)
            {
                _lines.Add(new Line(line));
                widest = Math.Max(widest, x);
                line = new List<(Piece, float)>();
                x = 0;
            }
            for (int k = i; k < j; k++) { line.Add((pieces[k], x)); x += pieces[k].Width; }
            i = j - 1;
        }
        if (line.Count > 0) { _lines.Add(new Line(line)); widest = Math.Max(widest, x); }
        if (_lines.Count == 0) _lines.Add(new Line(new()));
        Width = Math.Min(widest, maxWidth);
    }

    public void Draw(Graphics g, float x, float y, Color? color = null)
    {
        float top = y + _halfLeading;
        foreach (var l in _lines)
        {
            foreach (var (p, px) in l.Pieces)
            {
                using var b = new SolidBrush(color ?? p.Color);
                g.DrawString(p.Text, p.Font, b, x + px, top + p.Offset, ReportFonts.Format);
            }
            top += _lineHeight;
        }
    }

    private static bool EndsWithSpace(string s) => s.Length > 0 && s[^1] == ' ';
    private static float TrailingSpace(Graphics g, Piece p) => EndsWithSpace(p.Text) ? p.Width - ReportFonts.Measure(g, p.Text.TrimEnd(), p.Font) : 0;

    private static float Ascent(Font f) =>
        f.Size * f.FontFamily.GetCellAscent(f.Style) / f.FontFamily.GetEmHeight(f.Style);
}
