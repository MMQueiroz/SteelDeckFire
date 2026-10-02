using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Models;

namespace SteelDeckFire.App.Views;

/// <summary>Faixa superior com o veredito e os números que decidem o painel.</summary>
[ToolboxItem(true), DesignerCategory("Code")]
public class ResultStrip : Control
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
        Theme.PaintScaled(this, e.Graphics, Draw);
    }

    private void Draw(Graphics g, Rectangle area)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        using var rule = new Pen(Theme.Rule, 1f);
        g.DrawLine(rule, 0, area.Height - 1, area.Width, area.Height - 1);
        if (_inp is null || _res is null) return;

        bool ok = _res.QfiRd >= _res.Load.QfiSd;
        int warns = _res.Checks.Count(c => c.Status == CheckStatus.Warning);
        int fails = _res.Checks.Count(c => c.Status == CheckStatus.Fail);
        Color c0 = fails > 0 ? Theme.Heat : warns > 0 ? Theme.Amber : Theme.Good;

        // Barra lateral de status
        using (var b = new SolidBrush(c0)) g.FillRectangle(b, 0, 0, 8, area.Height);

        using var fVerdict = Theme.DrawFont(13f, FontStyle.Bold);
        using var fSub = Theme.DrawFont(9f);
        using var fNum = Theme.DrawFont(20f, FontStyle.Bold);
        using var fLbl = Theme.DrawFont(8.5f);
        using var ink = new SolidBrush(Theme.Ink);
        using var muted = new SolidBrush(Theme.Muted);
        using var cb = new SolidBrush(c0);

        // Indicadores à direita, medidos; os menos importantes saem se faltar espaço
        const float gap = 30f, right = 20f, left = 24f, minText = 260f;
        var kpis = new List<(string Value, string Label, Brush Brush)>
        {
            (Theme.F(_res.QfiRd), "q_fi,Rd  kN/m²", ink),
            (Theme.F(_res.Load.QfiSd), "q_fi,Sd  kN/m²", ink),
            ($"{Theme.F(_res.Utilization * 100, 0)}%", "utilização", cb),
            (Theme.F(_res.Membrane.E, 2), "fator de membrana e", ink),
            (Theme.F(_res.Membrane.W, 0), "deslocamento w  mm", ink),
        };
        float KpiWidth((string Value, string Label, Brush) k) =>
            Math.Max(g.MeasureString(k.Value, fNum).Width, g.MeasureString(k.Label, fLbl).Width);
        float total;
        while (true)
        {
            total = kpis.Sum(KpiWidth) + gap * (kpis.Count - 1);
            if (kpis.Count <= 3 || area.Width - right - total - gap >= left + minText) break;
            kpis.RemoveAt(kpis.Count - 1);
        }
        float hNum = g.MeasureString("0", fNum).Height, hLbl = g.MeasureString("0", fLbl).Height;
        float ky = (area.Height - hNum - hLbl) / 2;
        float x = Math.Max(left + minText, area.Width - right - total);
        foreach (var k in kpis)
        {
            g.DrawString(k.Value, fNum, k.Brush, x, ky);
            g.DrawString(k.Label, fLbl, muted, x + 2, ky + hNum);
            x += KpiWidth(k) + gap;
        }

        // Veredito à esquerda, cortado com reticências se não couber
        float textW = Math.Max(minText, area.Width - right - total - gap - left);
        using var fmt = new StringFormat(StringFormatFlags.NoWrap) { Trimming = StringTrimming.EllipsisCharacter };
        string verdict = ok ? "Atende à capacidade portante" : "Não atende à capacidade portante";
        string sub = $"{_inp.PanelName} · TRRF {Theme.F(_inp.FireTime, 0)} min"
                   + (fails > 0 ? $" · {fails} verificação(ões) não atendida(s)" : "")
                   + (warns > 0 ? $" · {warns} ponto(s) de atenção" : "");
        // O subtítulo pode quebrar em até duas linhas
        using var subFmt = new StringFormat(StringFormatFlags.LineLimit) { Trimming = StringTrimming.EllipsisWord };
        float hV = g.MeasureString(verdict, fVerdict).Height;
        float hLine = g.MeasureString("0", fSub).Height;
        float hS = Math.Min(2 * hLine, g.MeasureString(sub, fSub, (int)textW, subFmt).Height);
        float ty = Math.Max(4, (area.Height - hV - hS - 2) / 2);
        g.DrawString(verdict, fVerdict, cb, new RectangleF(left, ty, textW, hV), fmt);
        g.DrawString(sub, fSub, muted, new RectangleF(left + 2, ty + hV + 2, textW, hS + 1), subFmt);
    }
}
