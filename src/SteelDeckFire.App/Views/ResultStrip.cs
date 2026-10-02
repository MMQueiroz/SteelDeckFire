using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Models;

namespace SteelDeckFire.App.Views;

/// <summary>Faixa superior com o veredito e os números que decidem o painel.</summary>
internal sealed class ResultStrip : Control
{
    private ProjectInput? _inp;
    private DesignResult? _res;

    public ResultStrip()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
        Height = 92;
    }

    public void SetData(ProjectInput inp, DesignResult res) { _inp = inp; _res = res; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        using var rule = new Pen(Theme.Rule, 1f);
        g.DrawLine(rule, 0, Height - 1, Width, Height - 1);
        if (_inp is null || _res is null) return;

        bool ok = _res.QfiRd >= _res.Load.QfiSd;
        int warns = _res.Checks.Count(c => c.Status == CheckStatus.Warning);
        int fails = _res.Checks.Count(c => c.Status == CheckStatus.Fail);
        Color c0 = fails > 0 ? Theme.Heat : warns > 0 ? Theme.Amber : Theme.Good;

        // Barra lateral de status
        using (var b = new SolidBrush(c0)) g.FillRectangle(b, 0, 0, 8, Height);

        using var fVerdict = Theme.UiFont(13f, FontStyle.Bold);
        using var fSub = Theme.UiFont(9f);
        using var fNum = Theme.UiFont(20f, FontStyle.Bold);
        using var fLbl = Theme.UiFont(8.5f);
        using var ink = new SolidBrush(Theme.Ink);
        using var muted = new SolidBrush(Theme.Muted);
        using var cb = new SolidBrush(c0);

        string verdict = ok ? "Atende à capacidade portante" : "Não atende à capacidade portante";
        g.DrawString(verdict, fVerdict, cb, 24, 16);
        string sub = $"{_inp.PanelName} · TRRF {Theme.F(_inp.FireTime, 0)} min"
                   + (fails > 0 ? $" · {fails} verificação(ões) não atendida(s)" : "")
                   + (warns > 0 ? $" · {warns} ponto(s) de atenção" : "");
        g.DrawString(sub, fSub, muted, 26, 46);

        float x = Math.Max(420, Width * 0.38f);
        void Kpi(string value, string label, Brush b)
        {
            g.DrawString(value, fNum, b, x, 14);
            g.DrawString(label, fLbl, muted, x + 2, 52);
            x += Math.Max(g.MeasureString(value, fNum).Width, g.MeasureString(label, fLbl).Width) + 34;
        }
        Kpi(Theme.F(_res.QfiRd), "q_fi,Rd  kN/m²", ink);
        Kpi(Theme.F(_res.Load.QfiSd), "q_fi,Sd  kN/m²", ink);
        Kpi($"{Theme.F(_res.Utilization * 100, 0)}%", "utilização", cb);
        Kpi(Theme.F(_res.Membrane.E, 2), "fator de membrana e", ink);
        Kpi(Theme.F(_res.Membrane.W, 0), "deslocamento w  mm", ink);
    }
}
