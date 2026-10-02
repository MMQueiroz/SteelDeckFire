using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Models;

namespace SteelDeckFire.App.Views;

/// <summary>Seção transversal da laje (duas ondas), com gradiente térmico, tela, h_eff e temperaturas de cálculo.</summary>
internal sealed class SectionView : Control
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
        Draw(e.Graphics, ClientRectangle);
    }

    public Bitmap Render(int width, int height)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Theme.Paper);
        Draw(g, new Rectangle(0, 0, width, height));
        return bmp;
    }

    private void Draw(Graphics g, Rectangle area)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        using var fSmall = Theme.UiFont(8.5f);
        using var fNorm = Theme.UiFont(9.5f);
        using var fBold = Theme.UiFont(9.5f, FontStyle.Bold);
        using var ink = new SolidBrush(Theme.Ink);
        using var muted = new SolidBrush(Theme.Muted);
        if (_inp is null || _res is null) return;
        var inp = _inp; var th = _res.Thermal;

        double pitch = inp.DeckL1 + inp.DeckL3;
        double ht = inp.SlabThickness, h2 = inp.DeckH2;
        if (pitch <= 0 || ht <= h2 || h2 <= 0) { g.DrawString("Geometria da fôrma inválida.", fNorm, muted, 20, 20); return; }
        double totalW = 2 * pitch + inp.DeckL3; // termina sobre uma mesa superior
        const int mL = 70, mR = 230, mT = 50, mB = 70;
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
        double yMesh = ht - inp.MeshDepth;
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

        // Cotas à direita e temperaturas
        float rx = P(totalW, 0).X + 16;
        void Tag(double y, string text, Brush b, Font f)
        {
            var p = P(totalW, y);
            using var lp = new Pen(Theme.Rule, 1f);
            g.DrawLine(lp, p.X + 2, p.Y, rx, p.Y);
            g.DrawString(text, f, b, rx + 4, p.Y - 8);
        }
        using var heatB = new SolidBrush(Theme.Heat);
        using var guardB = new SolidBrush(Theme.Guard);
        using var goodB = new SolidBrush(Theme.Good);
        Tag(ht, $"face não exposta  θ1 ≈ {Theme.F(th.Theta1, 0)} °C", ink, fNorm);
        Tag(yMesh, $"tela, d = {Theme.F(inp.MeshDepth, 0)} mm  θs = {Theme.F(th.ThetaS, 0)} °C  (ks = {Theme.F(th.Ks, 2)})", goodB, fNorm);
        Tag(Math.Min(th.Heff, ht) , $"h_eff = {Theme.F(th.Heff, 1)} mm", guardB, fNorm);
        Tag(0, $"face exposta  θ2 = {Theme.F(th.Theta2, 0)} °C", heatB, fBold);

        // Cotas verticais à esquerda
        using var dim = new Pen(Theme.Muted, 1f) { StartCap = LineCap.ArrowAnchor, EndCap = LineCap.ArrowAnchor };
        float lx = ox - 26;
        g.DrawLine(dim, lx, P(0, 0).Y, lx, P(0, ht).Y);
        g.DrawLine(dim, lx - 18, P(0, 0).Y, lx - 18, P(0, h2).Y);
        var st = g.Save();
        g.TranslateTransform(lx - 4, (P(0, 0).Y + P(0, ht).Y) / 2); g.RotateTransform(-90);
        g.DrawString($"ht = {Theme.F(ht, 0)}", fSmall, ink, -24, -2);
        g.Restore(st);
        st = g.Save();
        g.TranslateTransform(lx - 22, (P(0, 0).Y + P(0, h2).Y) / 2); g.RotateTransform(-90);
        g.DrawString($"h2 = {Theme.F(h2, 0)}", fSmall, ink, -22, -14);
        g.Restore(st);

        // Título e rótulo do fogo
        g.DrawString($"{inp.DeckName} · t = {Theme.F(inp.DeckThickness)} mm · {inp.MeshName} · TRRF {Theme.F(inp.FireTime, 0)} min",
            fBold, ink, area.X + mL, area.Y + 14);
        g.DrawString("exposição ao fogo (ISO 834) pela face inferior", fSmall, heatB, area.X + mL, P(0, 0).Y + 16);
    }
}
