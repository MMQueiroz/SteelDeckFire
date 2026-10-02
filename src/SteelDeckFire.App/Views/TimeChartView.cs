using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SteelDeckFire.Core.Calc;

namespace SteelDeckFire.App.Views;

/// <summary>Resistência do painel (laje, vigas e total) em função do tempo, com a solicitação e o TRRF.</summary>
internal sealed class TimeChartView : Control
{
    private IReadOnlyList<TimePoint> _pts = Array.Empty<TimePoint>();
    private double _qSd, _trrf;

    public TimeChartView()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Paper;
    }

    public void SetData(IReadOnlyList<TimePoint> pts, double qSd, double trrf)
    {
        _pts = pts; _qSd = qSd; _trrf = trrf; Invalidate();
    }

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
        if (_pts.Count < 2) return;

        const int mL = 64, mR = 30, mT = 44, mB = 80;
        var plot = new RectangleF(area.X + mL, area.Y + mT, area.Width - mL - mR, area.Height - mT - mB);
        if (plot.Width < 80 || plot.Height < 60) return;

        double t0 = _pts[0].Time, t1 = _pts[^1].Time;
        double yMax = Math.Max(_qSd, _pts.Max(p => p.QTotal)) * 1.15;
        yMax = NiceCeil(yMax);
        PointF P(double t, double q) => new(
            plot.X + (float)((t - t0) / (t1 - t0) * plot.Width),
            plot.Bottom - (float)(q / yMax * plot.Height));

        // Grade
        using var grid = new Pen(Theme.Rule, 1f);
        double yStep = NiceStep(yMax / 6);
        for (double y = 0; y <= yMax + 1e-9; y += yStep)
        {
            var p = P(t0, y);
            g.DrawLine(grid, plot.Left, p.Y, plot.Right, p.Y);
            var txt = Theme.F(y, yStep < 1 ? 1 : 0);
            var sz = g.MeasureString(txt, fSmall);
            g.DrawString(txt, fSmall, muted, plot.Left - sz.Width - 6, p.Y - sz.Height / 2);
        }
        for (double t = 30; t <= t1 + 1e-9; t += 30)
        {
            var p = P(t, 0);
            g.DrawLine(grid, p.X, plot.Top, p.X, plot.Bottom);
            var txt = Theme.F(t, 0);
            var sz = g.MeasureString(txt, fSmall);
            g.DrawString(txt, fSmall, muted, p.X - sz.Width / 2, plot.Bottom + 6);
        }
        g.DrawString("tempo de incêndio-padrão (min)", fSmall, muted, plot.Left + plot.Width / 2 - 80, plot.Bottom + 24);
        g.DrawString("kN/m²", fSmall, muted, area.X + 8, plot.Top - 22);

        // TRRF
        if (_trrf >= t0 && _trrf <= t1)
        {
            using var trrfPen = new Pen(Theme.Ink, 1.2f) { DashStyle = DashStyle.Dot };
            var pa = P(_trrf, 0); var pb = P(_trrf, yMax);
            g.DrawLine(trrfPen, pa, pb);
            g.DrawString($"TRRF {Theme.F(_trrf, 0)} min", fSmall, ink, pb.X + 4, plot.Top + 2);
        }

        // Solicitação
        using (var sdPen = new Pen(Theme.Heat, 1.6f) { DashStyle = DashStyle.Dash })
        {
            var a = P(t0, _qSd); var b = P(t1, _qSd);
            g.DrawLine(sdPen, a, b);
        }

        void Series(Func<TimePoint, double> sel, Color c, float w, DashStyle ds)
        {
            using var pen = new Pen(c, w) { DashStyle = ds, LineJoin = LineJoin.Round };
            g.DrawLines(pen, _pts.Select(p => P(p.Time, sel(p))).ToArray());
        }
        Series(p => p.QBeams, Theme.Muted, 1.6f, DashStyle.Dash);
        Series(p => p.QSlab, Theme.Guard, 2f, DashStyle.Solid);
        Series(p => p.QTotal, Theme.Ink, 3f, DashStyle.Solid);

        // Tempo de colapso estimado (primeiro cruzamento)
        double? tFail = null;
        for (int i = 1; i < _pts.Count; i++)
        {
            if (_pts[i - 1].QTotal >= _qSd && _pts[i].QTotal < _qSd)
            {
                double f = (_pts[i - 1].QTotal - _qSd) / (_pts[i - 1].QTotal - _pts[i].QTotal);
                tFail = _pts[i - 1].Time + f * (_pts[i].Time - _pts[i - 1].Time);
                break;
            }
        }
        string headline = _pts[0].QTotal < _qSd
            ? "Resistência abaixo da solicitação desde 30 min"
            : tFail is null ? $"Resistência acima da solicitação até {Theme.F(t1, 0)} min"
                            : $"Resistência cruza a solicitação em ≈ {Theme.F(tFail.Value, 0)} min";
        g.DrawString(headline, fBold, ink, plot.Left, area.Y + 12);
        if (tFail is double tf)
        {
            var p = P(tf, _qSd);
            using var mk = new SolidBrush(Theme.Heat);
            g.FillEllipse(mk, p.X - 5, p.Y - 5, 10, 10);
        }

        // Legenda
        float lx = plot.Left, ly = area.Bottom - 26;
        Legend(g, ref lx, ly, Theme.Ink, 3f, DashStyle.Solid, "Total (laje + vigas)", fSmall, ink);
        Legend(g, ref lx, ly, Theme.Guard, 2f, DashStyle.Solid, "Laje com membrana", fSmall, ink);
        Legend(g, ref lx, ly, Theme.Muted, 1.6f, DashStyle.Dash, "Vigas sem proteção", fSmall, ink);
        Legend(g, ref lx, ly, Theme.Heat, 1.6f, DashStyle.Dash, $"Solicitação q_fi,Sd = {Theme.F(_qSd)}", fSmall, ink);
    }

    private static void Legend(Graphics g, ref float x, float y, Color c, float w, DashStyle ds, string text, Font f, Brush b)
    {
        using var p = new Pen(c, w) { DashStyle = ds };
        g.DrawLine(p, x, y + 8, x + 26, y + 8);
        g.DrawString(text, f, b, x + 32, y);
        x += 32 + g.MeasureString(text, f).Width + 20;
    }

    private static double NiceStep(double raw)
    {
        if (raw <= 0) return 1;
        double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double r = raw / mag;
        return (r <= 1 ? 1 : r <= 2 ? 2 : r <= 5 ? 5 : 10) * mag;
    }

    private static double NiceCeil(double v) { double st = NiceStep(v / 6); return Math.Ceiling(v / st) * st; }
}
