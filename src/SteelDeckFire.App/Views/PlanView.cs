using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Models;

namespace SteelDeckFire.App.Views;

/// <summary>
/// Planta do painel: vigas de perímetro protegidas, vigas internas sem proteção, padrão de charneiras
/// (espessura proporcional à força de membrana; azul = compressão, vermelho = tração),
/// zona tracionada central, anel comprimido e fissura central através do vão menor.
/// Eixo horizontal = L2; eixo vertical = L1 (vigas internas verticais).
/// </summary>
[ToolboxItem(true)]
public class PlanView : Control
{
    private ProjectInput? _inp;
    private DesignResult? _res;

    public PlanView()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Paper;
    }

    public void SetData(ProjectInput inp, DesignResult res) { _inp = inp; _res = res; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Theme.PaintScaled(this, e.Graphics, Draw);
    }

    /// <summary>Desenha a vista em <paramref name="area"/>; usado também pelo memorial.</summary>
    internal void Draw(Graphics g, Rectangle area)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        using var fSmall = Theme.DrawFont(8.5f);
        using var fNorm = Theme.DrawFont(9.5f);
        using var fBold = Theme.DrawFont(9.5f, FontStyle.Bold);
        using var inkBrush = new SolidBrush(Theme.Ink);
        using var mutedBrush = new SolidBrush(Theme.Muted);

        if (_inp is null || _res is null || _inp.L1 <= 0 || _inp.L2 <= 0)
        {
            g.DrawString("Defina a geometria do painel para visualizar.", fNorm, mutedBrush, area.X + 20, area.Y + 20);
            return;
        }

        var inp = _inp; var m = _res.Membrane;
        double W = inp.L2, H = inp.L1;
        // Margens a partir do tamanho real dos textos
        string nLText = $"n·L = {Theme.F(m.N * m.Lmm / 1000.0)} m";
        string info = $"a = {Theme.F(m.A, 3)}   n = {Theme.F(m.N, 3)}   k = {Theme.F(m.K_, 3)}   b = {Theme.F(m.B, 3)}   e = {Theme.F(m.E, 2)}   w = {Theme.F(m.W, 0)} mm";
        float hInfo = g.MeasureString(info, fNorm).Height, hSmall = g.MeasureString(nLText, fSmall).Height;
        float hBold = g.MeasureString("L2", fBold).Height;
        var legend = new List<(Color C, float W, DashStyle? Ds, string Text)>
        {
            (Theme.Guard, 5f, DashStyle.Solid, "Viga protegida (perímetro)"),
            (Theme.Heat, 2.5f, DashStyle.Dash, "Viga sem proteção"),
            (Theme.Heat, 5f, DashStyle.Solid, "Charneira em tração"),
            (Theme.Guard, 5f, DashStyle.Solid, "Charneira em compressão"),
            (Theme.TensionFill, 0, null, "Zona de membrana tracionada"),
            (Theme.CompressionFill, 0, null, "Anel comprimido"),
            (Theme.Ink, 1.6f, DashStyle.DashDot, m.CompressionGoverns ? "Fissura central · governa esmagamento nos cantos (⊗)" : "Fissura central · governa fratura da tela"),
        };
        var legendRows = LegendRows(g, legend.Select(l => l.Text), fSmall, area.Width - 40);
        float rowH = Math.Max(20, hSmall + 6);
        int mL = (int)(hBold + 50), mR = 40;
        int mT = (int)(12 + hInfo + 10 + hSmall + 2 + 16 + 6);
        int mB = (int)(26 + 4 + hBold + 14 + legendRows.Count * rowH + 8);
        if (!m.LongIsL2) mR = (int)(16 + 4 + g.MeasureString(nLText, fSmall).Width + 10);
        float avW = area.Width - mL - mR, avH = area.Height - mT - mB;
        if (avW < 60 || avH < 60) return;
        float s = (float)Math.Min(avW / W, avH / H);
        float ox = area.X + mL + (avW - (float)W * s) / 2f;
        float oy = area.Y + mT + (avH - (float)H * s) / 2f;
        PointF P(double x, double y) => new(ox + (float)(x * s), oy + (float)(y * s));
        var panel = new RectangleF(ox, oy, (float)W * s, (float)H * s);

        // Anel comprimido (todo o painel) e zona tracionada central
        using (var b = new SolidBrush(Theme.CompressionFill)) g.FillRectangle(b, panel);

        double nL = m.N * m.Lmm / 1000.0;
        double f0 = m.DiagonalZeroFraction;
        RectangleF tz;
        if (m.LongIsL2)
        {
            var p0 = P(f0 * nL, f0 * H / 2); var p1 = P(W - f0 * nL, H - f0 * H / 2);
            tz = RectangleF.FromLTRB(p0.X, p0.Y, p1.X, p1.Y);
        }
        else
        {
            var p0 = P(f0 * W / 2, f0 * nL); var p1 = P(W - f0 * W / 2, H - f0 * nL);
            tz = RectangleF.FromLTRB(p0.X, p0.Y, p1.X, p1.Y);
        }
        using (var path = RoundedRect(tz, Math.Min(tz.Width, tz.Height) * 0.35f))
        using (var b = new SolidBrush(Theme.TensionFill))
            g.FillPath(b, path);

        // Nervuras da fôrma (paralelas a L2, perpendiculares às vigas internas)
        using (var ribPen = new Pen(Theme.Rib, 1f))
        {
            int nr = Math.Max(8, (int)(H / 0.35));
            for (int i = 1; i < nr; i++)
            {
                double y = H * i / nr;
                g.DrawLine(ribPen, P(0, y), P(W, y));
            }
        }

        // Vigas internas sem proteção
        int nb = Math.Max(0, inp.UnprotectedBeams);
        using (var beamPen = new Pen(Theme.Heat, 2.5f) { DashStyle = DashStyle.Dash })
        {
            for (int i = 1; i <= nb; i++)
            {
                double x = W * i / (nb + 1);
                g.DrawLine(beamPen, P(x, 0), P(x, H));
            }
        }

        // Charneiras com espessura proporcional à força de membrana
        double k = m.K_;
        double fmax = Math.Max(k, 1.0);
        (PointF a, PointF b) ridge;
        var diags = new List<(PointF c, PointF e)>();
        if (m.LongIsL2)
        {
            var A = P(nL, H / 2); var B = P(W - nL, H / 2);
            ridge = (A, B);
            diags.Add((P(0, 0), A)); diags.Add((P(0, H), A));
            diags.Add((P(W, 0), B)); diags.Add((P(W, H), B));
        }
        else
        {
            var A = P(W / 2, nL); var B = P(W / 2, H - nL);
            ridge = (A, B);
            diags.Add((P(0, 0), A)); diags.Add((P(W, 0), A));
            diags.Add((P(0, H), B)); diags.Add((P(W, H), B));
        }
        const int seg = 28;
        foreach (var (c, e) in diags)
        {
            for (int i = 0; i < seg; i++)
            {
                double s0 = (double)i / seg, s1 = (double)(i + 1) / seg, sm = (s0 + s1) / 2;
                double f = -k + (1 + k) * sm; // em unidades de b·K·T0
                using var pen = new Pen(f < 0 ? Theme.Guard : Theme.Heat, (float)(1.5 + 6 * Math.Abs(f) / fmax))
                { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(pen, Lerp(c, e, s0), Lerp(c, e, s1));
            }
        }
        using (var pen = new Pen(Theme.Heat, (float)(1.5 + 6 / fmax)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLine(pen, ridge.a, ridge.b);

        // Fissura central através do vão menor (ou esmagamento nos cantos)
        using (var crackPen = new Pen(Theme.Ink, 1.6f) { DashStyle = DashStyle.DashDot })
        {
            if (m.LongIsL2) g.DrawLine(crackPen, P(W / 2, 0), P(W / 2, H));
            else g.DrawLine(crackPen, P(0, H / 2), P(W, H / 2));
        }
        if (m.CompressionGoverns)
        {
            using var cp = new Pen(Theme.Guard, 2f);
            foreach (var (c, _) in diags)
            {
                var q = Lerp(c, new PointF(panel.X + panel.Width / 2, panel.Y + panel.Height / 2), 0.06f);
                g.DrawEllipse(cp, q.X - 9, q.Y - 9, 18, 18);
                g.DrawLine(cp, q.X - 6, q.Y - 6, q.X + 6, q.Y + 6);
                g.DrawLine(cp, q.X - 6, q.Y + 6, q.X + 6, q.Y - 6);
            }
        }

        // Vigas de perímetro protegidas e pilares
        using (var guard = new Pen(Theme.Guard, 5f)) g.DrawRectangle(guard, panel.X, panel.Y, panel.Width, panel.Height);
        foreach (var c in new[] { P(0, 0), P(W, 0), P(0, H), P(W, H) })
            g.FillRectangle(inkBrush, c.X - 7, c.Y - 7, 14, 14);

        // Cotas
        using var dimPen = new Pen(Theme.Muted, 1f) { StartCap = LineCap.ArrowAnchor, EndCap = LineCap.ArrowAnchor };
        float yDim = panel.Bottom + 26;
        g.DrawLine(dimPen, panel.Left, yDim, panel.Right, yDim);
        DrawCentered(g, $"L2 = {Theme.F(W)} m", fBold, inkBrush, (panel.Left + panel.Right) / 2, yDim + 4);
        float xDim = panel.Left - 30;
        g.DrawLine(dimPen, xDim, panel.Top, xDim, panel.Bottom);
        var st = g.Save();
        g.TranslateTransform(xDim - 6, (panel.Top + panel.Bottom) / 2);
        g.RotateTransform(-90);
        DrawCentered(g, $"L1 = {Theme.F(H)} m", fBold, inkBrush, 0, -hBold - 2);
        g.Restore(st);
        // n·L
        if (m.LongIsL2)
        {
            float y = panel.Top - 16;
            g.DrawLine(dimPen, panel.Left, y, P(nL, 0).X, y);
            DrawCentered(g, nLText, fSmall, mutedBrush, (panel.Left + P(nL, 0).X) / 2, y - hSmall - 2);
        }
        else
        {
            float x = panel.Right + 16;
            g.DrawLine(dimPen, x, panel.Top, x, P(0, nL).Y);
            g.DrawString(nLText, fSmall, mutedBrush, x + 4, (panel.Top + P(0, nL).Y) / 2 - hSmall / 2);
        }

        // Parâmetros
        g.DrawString(info, fNorm, inkBrush, area.X + mL, area.Y + 12);

        // Legenda (quebra em linhas conforme a largura)
        float ly = area.Bottom - legendRows.Count * rowH - 8;
        int idx = 0;
        foreach (int count in legendRows)
        {
            float lx = area.X + 20;
            for (int i = 0; i < count; i++, idx++)
            {
                var (c, w, ds, text) = legend[idx];
                float cy = ly + hSmall / 2;
                if (ds is DashStyle d)
                {
                    using var p = new Pen(c, w) { DashStyle = d };
                    g.DrawLine(p, lx, cy, lx + 28, cy);
                }
                else
                {
                    using var br = new SolidBrush(Color.FromArgb(Math.Min(255, c.A * 2), c));
                    g.FillRectangle(br, lx, cy - 7, 28, 14);
                }
                g.DrawString(text, fSmall, inkBrush, lx + 34, ly);
                lx += LegendItemWidth(g, text, fSmall);
            }
            ly += rowH;
        }
    }

    private static float LegendItemWidth(Graphics g, string text, Font f) => 34 + g.MeasureString(text, f).Width + 22;

    /// <summary>Quantos itens da legenda cabem em cada linha.</summary>
    private static List<int> LegendRows(Graphics g, IEnumerable<string> texts, Font f, float width)
    {
        var rows = new List<int>();
        float x = 0; int n = 0;
        foreach (var t in texts)
        {
            float w = LegendItemWidth(g, t, f);
            if (n > 0 && x + w > width) { rows.Add(n); x = 0; n = 0; }
            x += w; n++;
        }
        if (n > 0) rows.Add(n);
        return rows;
    }

    private static PointF Lerp(PointF a, PointF b, double t) =>
        new((float)(a.X + (b.X - a.X) * t), (float)(a.Y + (b.Y - a.Y) * t));

    private static void DrawCentered(Graphics g, string text, Font f, Brush b, float cx, float y)
    {
        var sz = g.MeasureString(text, f);
        g.DrawString(text, f, b, cx - sz.Width / 2, y);
    }

    private static GraphicsPath RoundedRect(RectangleF r, float rad)
    {
        var path = new GraphicsPath();
        float d = Math.Max(1f, Math.Min(rad * 2, Math.Min(r.Width, r.Height)));
        if (r.Width < 2 || r.Height < 2) { path.AddRectangle(r); return path; }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
