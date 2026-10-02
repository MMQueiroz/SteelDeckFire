using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Models;

namespace SteelDeckFire.App.Views;

/// <summary>Seção transversal da laje (duas ondas), com gradiente térmico, tela, h_eff e temperaturas de cálculo.</summary>
[ToolboxItem(true)]
public class SectionView : Control
{
    private ProjectInput? _inp;
    private DesignResult? _res;

    public SectionView()
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
        using var ink = new SolidBrush(Theme.Ink);
        using var muted = new SolidBrush(Theme.Muted);
        if (_inp is null || _res is null) return;
        var inp = _inp; var th = _res.Thermal;

        double pitch = inp.DeckL1 + inp.DeckL3;
        double ht = inp.SlabThickness, h2 = inp.DeckH2;
        if (pitch <= 0 || ht <= h2 || h2 <= 0) { g.DrawString("Geometria da fôrma inválida.", fNorm, muted, 20, 20); return; }
        double totalW = 2 * pitch + inp.DeckL3; // termina sobre uma mesa superior
        using var heatB = new SolidBrush(Theme.Heat);
        using var guardB = new SolidBrush(Theme.Guard);
        using var goodB = new SolidBrush(Theme.Good);
        double yMesh = ht - inp.MeshDepth;
        var tags = new List<(double Y, string Text, Brush Brush, Font Font)>
        {
            (ht, $"face não exposta  θ1 ≈ {Theme.F(th.Theta1, 0)} °C", ink, fNorm),
            (yMesh, $"tela, d = {Theme.F(inp.MeshDepth, 0)} mm  θs = {Theme.F(th.ThetaS, 0)} °C  (ks = {Theme.F(th.Ks, 2)})", goodB, fNorm),
            (Math.Min(th.Heff, ht), $"h_eff = {Theme.F(th.Heff, 1)} mm", guardB, fNorm),
            (0, $"face exposta  θ2 = {Theme.F(th.Theta2, 0)} °C", heatB, fBold),
        };
        // Margem direita do tamanho dos rótulos
        int mL = 80, mT = 50, mB = 70;
        int mR = (int)Math.Ceiling(tags.Max(t => g.MeasureString(t.Text, t.Font).Width)) + 50;
        float avW = area.Width - mL - mR, avH = area.Height - mT - mB;
        if (avW < 80 || avH < 60) return;
        float s = (float)Math.Min(avW / totalW, avH / ht);
        float ox = area.X + mL, oy = area.Y + mT + (float)ht * s; // origem na face inferior da fôrma (y para cima)
        PointF P(double x, double y) => new(ox + (float)(x * s), oy - (float)(y * s));

        // Contorno inferior (fôrma): mesa superior l3, alma, fundo l2, alma
        var steel = new List<PointF>();
        double x0 = 0;
        double web = (inp.DeckL1 - inp.DeckL2) / 2.0;
        steel.Add(P(0, h2));
        while (x0 < totalW - 1e-6)
        {
            steel.Add(P(Math.Min(x0 + inp.DeckL3, totalW), h2));
            if (x0 + inp.DeckL3 >= totalW) break;
            steel.Add(P(x0 + inp.DeckL3 + web, 0));
            steel.Add(P(x0 + inp.DeckL3 + web + inp.DeckL2, 0));
            steel.Add(P(x0 + pitch, h2));
            x0 += pitch;
        }
        var outline = new List<PointF>(steel) { P(totalW, h2), P(totalW, ht), P(0, ht) };

        using (var path = new GraphicsPath())
        {
            path.AddPolygon(outline.ToArray());
            var top = P(0, ht); var bottom = P(0, 0);
            using var grad = new LinearGradientBrush(new PointF(top.X, top.Y - 1), new PointF(bottom.X, bottom.Y + 1),
                Color.FromArgb(214, 218, 220), Color.FromArgb(236, 150, 110));
            grad.InterpolationColors = new ColorBlend
            {
                Colors = new[] { Color.FromArgb(214, 218, 220), Color.FromArgb(222, 206, 196), Color.FromArgb(236, 150, 110), Color.FromArgb(214, 90, 60) },
                Positions = new[] { 0f, 0.45f, 0.8f, 1f }
            };
            g.FillPath(grad, path);
            using var edge = new Pen(Theme.Muted, 1f);
            g.DrawPath(edge, path);
        }
        using (var steelPen = new Pen(Theme.Ink, 2.4f)) g.DrawLines(steelPen, steel.ToArray());

        // Tela
        using (var meshPen = new Pen(Theme.Good, 1.6f)) g.DrawLine(meshPen, P(0, yMesh), P(totalW, yMesh));
        using (var meshBrush = new SolidBrush(Theme.Good))
            for (double x = 40; x < totalW; x += 100)
            {
                var c = P(x, yMesh);
                g.FillEllipse(meshBrush, c.X - 3, c.Y - 3, 6, 6);
            }

        // h_eff (medida a partir da face inferior da fôrma)
        using (var effPen = new Pen(Theme.Guard, 1.2f) { DashStyle = DashStyle.Dash })
        {
            double yh = Math.Min(th.Heff, ht);
            g.DrawLine(effPen, P(-10, yh), P(totalW + 10, yh));
        }

        // Cotas à direita e temperaturas: rótulos afastados verticalmente para não se sobreporem
        float rx = P(totalW, 0).X + 30;
        var placed = tags.Select(t => (t, P(totalW, t.Y).Y, g.MeasureString(t.Text, t.Font).Height))
                         .OrderBy(p => p.Item2).ToList();
        var labelY = placed.Select(p => p.Item2 - p.Item3 / 2).ToArray();
        for (int i = 1; i < labelY.Length; i++)
            labelY[i] = Math.Max(labelY[i], labelY[i - 1] + placed[i - 1].Item3);
        float overflow = labelY[^1] + placed[^1].Item3 - (area.Bottom - 4);
        if (overflow > 0)
        {
            labelY[^1] -= overflow;
            for (int i = labelY.Length - 2; i >= 0; i--)
                labelY[i] = Math.Min(labelY[i], labelY[i + 1] - placed[i].Item3);
        }
        using (var lp = new Pen(Theme.Muted, 1f))
            for (int i = 0; i < placed.Count; i++)
            {
                var (t, py, h) = placed[i];
                float ly = labelY[i] + h / 2;
                g.DrawLine(lp, P(totalW, 0).X + 2, py, rx - 10, py);
                g.DrawLine(lp, rx - 10, py, rx - 2, ly);
                g.DrawString(t.Text, t.Font, t.Brush, rx, labelY[i]);
            }

        // Cotas verticais à esquerda
        using var dim = new Pen(Theme.Muted, 1f) { StartCap = LineCap.ArrowAnchor, EndCap = LineCap.ArrowAnchor };
        // h2 junto à seção, ht por fora; cada rótulo fica à esquerda da sua linha
        float th2 = g.MeasureString("h2", fSmall).Height;
        float xh2 = ox - 14, xht = xh2 - th2 - 14;
        g.DrawLine(dim, xh2, P(0, 0).Y, xh2, P(0, h2).Y);
        g.DrawLine(dim, xht, P(0, 0).Y, xht, P(0, ht).Y);
        void VLabel(string text, float x, float yMid)
        {
            var st = g.Save();
            g.TranslateTransform(x - 3, yMid); g.RotateTransform(-90);
            DrawCentered(g, text, fSmall, ink, 0, -th2);
            g.Restore(st);
        }
        VLabel($"h2 = {Theme.F(h2, 0)}", xh2, (P(0, 0).Y + P(0, h2).Y) / 2);
        VLabel($"ht = {Theme.F(ht, 0)}", xht, (P(0, 0).Y + P(0, ht).Y) / 2);

        // Título e rótulo do fogo
        g.DrawString($"{inp.DeckName} · t = {Theme.F(inp.DeckThickness)} mm · {inp.MeshName} · TRRF {Theme.F(inp.FireTime, 0)} min",
            fBold, ink, area.X + mL, area.Y + 14);
        g.DrawString("exposição ao fogo (ISO 834) pela face inferior", fSmall, heatB, area.X + mL, P(0, 0).Y + 16);
    }

    private static void DrawCentered(Graphics g, string text, Font f, Brush b, float cx, float y)
    {
        var sz = g.MeasureString(text, f);
        g.DrawString(text, f, b, cx - sz.Width / 2, y);
    }
}
