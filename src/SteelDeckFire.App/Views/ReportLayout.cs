using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Report;

namespace SteelDeckFire.App.Views;

/// <summary>
/// Diagrama o memorial em páginas A4. Coordenadas em 1/100 pol. (unidade de exibição da impressora),
/// de modo que a mesma paginação é usada na tela, na visualização de impressão e no PDF.
/// </summary>
internal sealed class ReportLayout : IDisposable
{
    public const float PageWidth = 827f, PageHeight = 1169f; // A4
    private const float MarginX = 75f, MarginTop = 70f, MarginBottom = 85f;
    private const float ContentWidth = PageWidth - 2 * MarginX;
    private const float FigureVirtualWidth = 1000f;

    /// <summary>Desenha uma figura do memorial no retângulo dado (coordenadas de "pixel" da vista).</summary>
    public delegate void FigurePainter(Graphics g, Rectangle area);

    private abstract class Box
    {
        public float Height;
        public float SpaceBefore;
        public bool KeepWithNext;
        public abstract void Draw(Graphics g, float x, float y);
    }

    private sealed class DelegateBox : Box
    {
        private readonly Action<Graphics, float, float> _draw;
        public DelegateBox(float height, float spaceBefore, Action<Graphics, float, float> draw, bool keepWithNext = false)
        {
            Height = height; SpaceBefore = spaceBefore; KeepWithNext = keepWithNext; _draw = draw;
        }
        public override void Draw(Graphics g, float x, float y) => _draw(g, x, y);
    }

    private sealed class Page
    {
        public readonly List<(Box Box, float Y)> Items = new();
    }

    private static readonly Color IntroInk = Color.FromArgb(60, 74, 84);
    private static readonly Color HeaderFill = Color.FromArgb(242, 244, 246);
    private static readonly Color OkFill = Color.FromArgb(232, 243, 236);
    private static readonly Color FailFill = Color.FromArgb(248, 233, 230);

    private readonly ReportFonts _fonts = new();
    private readonly Func<ReportFigure, FigurePainter?> _figures;
    private readonly List<Page> _pages = new();
    private readonly string _title;

    public int PageCount => _pages.Count;

    public ReportLayout(ReportDocument doc, Func<ReportFigure, FigurePainter?> figures)
    {
        _figures = figures;
        _title = doc.Title;
        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);
        g.PageUnit = GraphicsUnit.Pixel;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;

        var boxes = new List<Box>();
        foreach (var b in doc.Blocks) boxes.AddRange(Make(g, b));
        Paginate(boxes);
    }

    // ================================================================ desenho
    /// <summary>Desenha a página <paramref name="index"/> com origem no canto superior esquerdo, em 1/100 pol.</summary>
    public void DrawPage(Graphics g, int index)
    {
        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        foreach (var (box, y) in _pages[index].Items) box.Draw(g, MarginX, y);

        // Rodapé
        float fy = PageHeight - MarginBottom + 30;
        using (var rule = new Pen(Theme.Rule, 1f)) g.DrawLine(rule, MarginX, fy, PageWidth - MarginX, fy);
        var f = _fonts.Sans.Get(11f);
        using var muted = new SolidBrush(Theme.Muted);
        string left = _title, right = $"Página {index + 1} de {_pages.Count}";
        float rw = ReportFonts.Measure(g, right, f);
        float maxLeft = ContentWidth - rw - 20;
        while (left.Length > 4 && ReportFonts.Measure(g, left, f) > maxLeft) left = left[..^5] + "…";
        g.DrawString(left, f, muted, MarginX, fy + 6, ReportFonts.Format);
        g.DrawString(right, f, muted, PageWidth - MarginX - rw, fy + 6, ReportFonts.Format);
        g.Restore(state);
    }

    // ================================================================ paginação
    private void Paginate(List<Box> boxes)
    {
        const float bottom = PageHeight - MarginBottom;
        var page = new Page();
        float y = MarginTop;
        for (int i = 0; i < boxes.Count; i++)
        {
            var box = boxes[i];
            float space = page.Items.Count == 0 ? 0 : box.SpaceBefore;
            float need = space + box.Height;
            for (int j = i; boxes[j].KeepWithNext && j + 1 < boxes.Count; j++)
                need += boxes[j + 1].SpaceBefore + boxes[j + 1].Height;
            if (page.Items.Count > 0 && y + need > bottom)
            {
                _pages.Add(page);
                page = new Page();
                y = MarginTop;
                space = 0;
            }
            page.Items.Add((box, y + space));
            y += space + box.Height;
        }
        if (page.Items.Count > 0 || _pages.Count == 0) _pages.Add(page);
    }

    // ================================================================ blocos
    private RichText Text(Graphics g, string markup, ReportFonts.Family fam, float size, Color color, float width, bool italic = false, bool bold = false) =>
        new(g, RichText.Parse(markup, italic, bold), fam, size, color, width);

    private IEnumerable<Box> Make(Graphics g, ReportBlock block)
    {
        switch (block)
        {
            case TitleBlock t: yield return Title(g, t); break;
            case SummaryBlock s: yield return Summary(g, s); break;
            case HeadingBlock h: yield return Heading(g, h); break;
            case ParagraphBlock p: yield return Paragraph(g, p.Text); break;
            case EquationBlock e: yield return Equation(g, e); break;
            case VerdictBlock v: yield return Verdict(g, v); break;
            case FigureBlock f: yield return Figure(g, f); break;
            case TableBlock t:
                foreach (var b in Table(g, t.Header, t.Rows.Select(r => r.Select(c => (c, (Color?)null)).ToArray()).ToList(), markup: true)) yield return b;
                break;
            case ChecksBlock c:
                var rows = c.Checks.Select(k =>
                {
                    var (lbl, col) = k.Status switch
                    {
                        CheckStatus.Ok => ("Atende", Theme.Good),
                        CheckStatus.Warning => ("Atenção", Theme.Amber),
                        _ => ("Não atende", Theme.Heat),
                    };
                    return new[] { (lbl, (Color?)col), (k.Title, null), (k.Detail, null) };
                }).ToList();
                foreach (var b in Table(g, new[] { "", "Verificação", "Detalhe" }, rows, markup: false)) yield return b;
                break;
        }
    }

    private Box Title(Graphics g, TitleBlock t)
    {
        var kind = Text(g, t.Kind, _fonts.Sans, 12.5f, Theme.Heat, ContentWidth, bold: true);
        var title = Text(g, t.Title, _fonts.Sans, 27f, Theme.Ink, ContentWidth, bold: true);
        var meta = Text(g, t.Meta, _fonts.Sans, 13.5f, Theme.Muted, ContentWidth);
        float h = kind.Height + 4 + title.Height + 2 + meta.Height + 14 + 3;
        return new DelegateBox(h, 0, (gr, x, y) =>
        {
            kind.Draw(gr, x, y); y += kind.Height + 4;
            title.Draw(gr, x, y); y += title.Height + 2;
            meta.Draw(gr, x, y); y += meta.Height + 14;
            using var b = new SolidBrush(Theme.Ink);
            gr.FillRectangle(b, x, y, ContentWidth, 3);
        });
    }

    private Box Summary(Graphics g, SummaryBlock s)
    {
        const float pad = 18f;
        Color accent = s.Ok ? Theme.Good : Theme.Heat;
        var figs = s.Figures.Select(f => (
            Num: Text(g, f.Value, _fonts.Sans, 31f, f.Highlight ? accent : Theme.Ink, ContentWidth, bold: true),
            Lbl: Text(g, f.Label, _fonts.Sans, 13f, Theme.Muted, ContentWidth))).ToList();
        float figH = figs.Count == 0 ? 0 : figs.Max(f => f.Num.Height + f.Lbl.Height);
        var text = Text(g, s.Text, _fonts.Serif, 15f, Theme.Ink, ContentWidth - 2 * pad);
        float h = pad + figH + 10 + text.Height + pad;
        return new DelegateBox(h, 10, (gr, x, y) =>
        {
            using (var pen = new Pen(accent, 2f)) gr.DrawRectangle(pen, x + 1, y + 1, ContentWidth - 2, h - 2);
            float fx = x + pad;
            foreach (var (num, lbl) in figs)
            {
                num.Draw(gr, fx, y + pad);
                lbl.Draw(gr, fx + 1, y + pad + num.Height);
                fx += Math.Max(num.Width, lbl.Width) + 36;
            }
            text.Draw(gr, x + pad, y + pad + figH + 10);
        });
    }

    private Box Heading(Graphics g, HeadingBlock hd)
    {
        const float badge = 26f, gap = 10f;
        float indent = hd.Step is null ? 0 : badge + gap;
        var title = Text(g, hd.Title, _fonts.Sans, 17.5f, Theme.Ink, ContentWidth - indent, bold: true);
        float top = 12f;
        float h = top + Math.Max(title.Height, hd.Step is null ? 0 : badge) + 6;
        return new DelegateBox(h, 26, (gr, x, y) =>
        {
            using (var rule = new Pen(Theme.Rule, 1f)) gr.DrawLine(rule, x, y, x + ContentWidth, y);
            float ty = y + top;
            if (hd.Step is int n)
            {
                float by = ty + Math.Max(0, (title.Height - badge) / 2);
                using (var b = new SolidBrush(Theme.Ink)) gr.FillRectangle(b, x, by, badge, badge);
                var f = _fonts.Sans.Get(13f, bold: true);
                string s = n.ToString();
                float w = ReportFonts.Measure(gr, s, f);
                gr.DrawString(s, f, Brushes.White, x + (badge - w) / 2, by + (badge - f.Size * 1.33f) / 2, ReportFonts.Format);
            }
            title.Draw(gr, x + indent, hd.Step is null ? ty : ty + Math.Max(0, (badge - title.Height) / 2));
        }, keepWithNext: true);
    }

    private Box Paragraph(Graphics g, string markup)
    {
        var t = Text(g, markup, _fonts.Serif, 15f, IntroInk, ContentWidth);
        return new DelegateBox(t.Height, 6, (gr, x, y) => t.Draw(gr, x, y));
    }

    private Box Equation(Graphics g, EquationBlock e)
    {
        const float bar = 3f, indent = 15f, padY = 6f;
        float w = ContentWidth - indent;
        var sym = Text(g, e.Symbolic, _fonts.Serif, 15f, Theme.Ink, w, italic: true);
        var sub = string.IsNullOrEmpty(e.Substituted) ? null : Text(g, "= " + e.Substituted, _fonts.Serif, 14f, Theme.Muted, w);
        var resRuns = new List<TextRun> { new("= ") };
        resRuns.AddRange(RichText.Parse(e.Result, bold: true));
        if (!string.IsNullOrEmpty(e.Unit)) resRuns.AddRange(RichText.Parse(" " + e.Unit));
        var res = new RichText(g, resRuns, _fonts.Serif, 15f, Theme.Ink, w);
        var note = e.Note is null ? null : Text(g, e.Note, _fonts.Sans, 12.5f, Theme.Muted, w);
        float h = padY + sym.Height + (sub?.Height ?? 0) + res.Height + (note?.Height ?? 0) + padY;
        return new DelegateBox(h, 9, (gr, x, y) =>
        {
            using (var b = new SolidBrush(Theme.Rule)) gr.FillRectangle(b, x, y, bar, h);
            float tx = x + indent, ty = y + padY;
            sym.Draw(gr, tx, ty); ty += sym.Height;
            if (sub is not null) { sub.Draw(gr, tx, ty); ty += sub.Height; }
            res.Draw(gr, tx, ty); ty += res.Height;
            note?.Draw(gr, tx, ty);
        });
    }

    private Box Verdict(Graphics g, VerdictBlock v)
    {
        const float bar = 4f, padX = 13f, padY = 8f;
        var runs = new List<TextRun> { new(v.Ok ? "Atende: " : "Não atende: ", Bold: true) };
        runs.AddRange(RichText.Parse(v.Text));
        var t = new RichText(g, runs, _fonts.Sans, 14f, Theme.Ink, ContentWidth - bar - 2 * padX);
        float h = padY + t.Height + padY;
        return new DelegateBox(h, 10, (gr, x, y) =>
        {
            using (var fill = new SolidBrush(v.Ok ? OkFill : FailFill)) gr.FillRectangle(fill, x, y, ContentWidth, h);
            using (var b = new SolidBrush(v.Ok ? Theme.Good : Theme.Heat)) gr.FillRectangle(b, x, y, bar, h);
            t.Draw(gr, x + bar + padX, y + padY);
        });
    }

    private Box Figure(Graphics g, FigureBlock f)
    {
        float imgH = (float)(ContentWidth * f.AspectRatio);
        var caption = Text(g, f.Caption, _fonts.Sans, 12.5f, Theme.Muted, ContentWidth);
        float h = imgH + 5 + caption.Height;
        var painter = _figures(f.Figure);
        return new DelegateBox(h, 16, (gr, x, y) =>
        {
            using (var b = new SolidBrush(Theme.Paper)) gr.FillRectangle(b, x, y, ContentWidth, imgH);
            if (painter is not null)
            {
                var state = gr.Save();
                gr.SetClip(new RectangleF(x, y, ContentWidth, imgH), CombineMode.Intersect);
                gr.TranslateTransform(x, y);
                float s = ContentWidth / FigureVirtualWidth;
                gr.ScaleTransform(s, s);
                painter(gr, new Rectangle(0, 0, (int)FigureVirtualWidth, (int)(imgH / s)));
                gr.Restore(state);
            }
            using (var pen = new Pen(Theme.Rule, 1f)) gr.DrawRectangle(pen, x, y, ContentWidth, imgH);
            caption.Draw(gr, x, y + imgH + 5);
        });
    }

    private IEnumerable<Box> Table(Graphics g, string[] header, List<(string Text, Color? Badge)[]> rows, bool markup)
    {
        const float padX = 8f, padY = 6f, size = 14f;
        int n = header.Length;
        List<TextRun> Runs(string s, bool bold = false) => markup ? RichText.Parse(s, bold: bold) : RichText.Plain(s, bold);

        // Larguras: naturais (sem quebra), ajustadas à largura disponível
        var natural = new float[n];
        void Fit(int c, List<TextRun> runs)
        {
            var t = new RichText(g, runs, _fonts.Sans, size, Theme.Ink, 1e6f);
            natural[c] = Math.Max(natural[c], t.Width + 2 * padX);
        }
        for (int c = 0; c < n; c++) Fit(c, Runs(header[c], true));
        foreach (var r in rows)
            for (int c = 0; c < n && c < r.Length; c++)
                Fit(c, r[c].Badge is null ? Runs(r[c].Text) : RichText.Plain(r[c].Text, true));
        var widths = ColumnWidths(natural, ContentWidth);
        // Colunas de selo não quebram
        for (int c = 0; c < n; c++)
            if (rows.Any(r => c < r.Length && r[c].Badge is not null) && widths[c] < natural[c])
            {
                float extra = natural[c] - widths[c];
                int widest = Array.IndexOf(widths, widths.Max());
                widths[widest] -= extra; widths[c] = natural[c];
            }

        Box Row((string Text, Color? Badge)[] cells, bool isHeader)
        {
            var texts = new RichText?[n];
            float rowH = 0;
            for (int c = 0; c < n && c < cells.Length; c++)
            {
                var (text, badge) = cells[c];
                var runs = isHeader || badge is not null ? (markup ? RichText.Parse(text, bold: true) : RichText.Plain(text, true)) : Runs(text);
                float fs = badge is null ? size : 12f;
                texts[c] = new RichText(g, runs, _fonts.Sans, fs, badge is null ? Theme.Ink : Color.White, widths[c] - 2 * padX, 1.3f);
                rowH = Math.Max(rowH, texts[c]!.Height);
            }
            float h = padY + rowH + padY;
            return new DelegateBox(h, 0, (gr, x, y) =>
            {
                if (isHeader)
                    using (var fill = new SolidBrush(HeaderFill)) gr.FillRectangle(fill, x, y, ContentWidth, h);
                float cx = x;
                for (int c = 0; c < n; c++)
                {
                    if (texts[c] is { } t)
                    {
                        if (cells[c].Badge is Color bc)
                        {
                            using var bb = new SolidBrush(bc);
                            gr.FillRectangle(bb, cx + padX, y + padY, t.Width + 12, t.Height);
                            t.Draw(gr, cx + padX + 6, y + padY);
                        }
                        else t.Draw(gr, cx + padX, y + padY);
                    }
                    cx += widths[c];
                }
                using var pen = new Pen(isHeader ? Theme.Ink : Theme.Rule, isHeader ? 2f : 1f);
                gr.DrawLine(pen, x, y + h, x + ContentWidth, y + h);
            }, keepWithNext: isHeader);
        }

        var head = Row(header.Select(s => (s, (Color?)null)).ToArray(), true);
        head.SpaceBefore = 10;
        yield return head;
        foreach (var r in rows) yield return Row(r, false);
        // espaço após a tabela
        yield return new DelegateBox(6, 0, (_, _, _) => { });
    }

    private static float[] ColumnWidths(float[] natural, float total)
    {
        int n = natural.Length;
        var w = new float[n];
        float sum = natural.Sum();
        if (sum <= total)
        {
            for (int i = 0; i < n; i++) w[i] = natural[i] + (total - sum) * natural[i] / sum;
            return w;
        }
        // Colunas estreitas recebem a largura natural; as demais dividem o restante
        var open = Enumerable.Range(0, n).ToList();
        float left = total;
        bool changed = true;
        while (changed && open.Count > 0)
        {
            changed = false;
            float fair = left / open.Count;
            foreach (int i in open.ToList())
                if (natural[i] <= fair) { w[i] = natural[i]; left -= natural[i]; open.Remove(i); changed = true; }
        }
        float openSum = open.Sum(i => natural[i]);
        foreach (int i in open) w[i] = left * natural[i] / openSum;
        return w;
    }

    public void Dispose() => _fonts.Dispose();
}
