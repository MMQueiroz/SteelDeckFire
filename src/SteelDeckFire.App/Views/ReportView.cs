using System.Drawing;
using System.Drawing.Drawing2D;

namespace SteelDeckFire.App.Views;

/// <summary>Exibe o memorial paginado, como na impressão. Ctrl + roda do mouse altera o zoom.</summary>
internal sealed class ReportView : ScrollableControl
{
    private const float Gap = 24f; // em pixels de tela
    private ReportLayout? _layout;
    private float? _zoom; // null = ajustar à largura

    public ReportView()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        AutoScroll = true;
        BackColor = Color.FromArgb(225, 229, 232);
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
    }

    public void SetLayout(ReportLayout? layout)
    {
        _layout = layout;
        UpdateScrollSize();
        Invalidate();
    }

    /// <summary>Pixels de tela por unidade do memorial (1/100 pol.).</summary>
    private float PixelScale
    {
        get
        {
            float px = DeviceDpi / 100f;
            if (_zoom is float z) return z * px;
            float fit = (ClientSize.Width - 2 * Gap) / ReportLayout.PageWidth;
            return Math.Clamp(fit, 0.4f * px, 1.25f * px);
        }
    }

    private void UpdateScrollSize()
    {
        if (_layout is null) { AutoScrollMinSize = Size.Empty; return; }
        float s = PixelScale;
        AutoScrollMinSize = new Size(
            (int)(ReportLayout.PageWidth * s + 2 * Gap),
            (int)(_layout.PageCount * (ReportLayout.PageHeight * s + Gap) + Gap));
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_zoom is null) UpdateScrollSize();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        base.OnMouseDown(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) == 0) { base.OnMouseWheel(e); return; }
        float px = DeviceDpi / 100f;
        float z = PixelScale / px * (e.Delta > 0 ? 1.1f : 1 / 1.1f);
        _zoom = Math.Clamp(z, 0.4f, 3f);
        UpdateScrollSize();
        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        int y = -AutoScrollPosition.Y;
        int page = (int)(ReportLayout.PageHeight * PixelScale + Gap);
        int? ny = e.KeyCode switch
        {
            Keys.Down => y + 40,
            Keys.Up => y - 40,
            Keys.PageDown => y + page,
            Keys.PageUp => y - page,
            Keys.Home => 0,
            Keys.End => int.MaxValue / 2,
            Keys.D0 or Keys.NumPad0 when e.Control => ResetZoom(),
            _ => null,
        };
        if (ny is int v) AutoScrollPosition = new Point(-AutoScrollPosition.X, Math.Max(0, v));
    }

    private int ResetZoom()
    {
        _zoom = null;
        UpdateScrollSize();
        Invalidate();
        return -AutoScrollPosition.Y;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_layout is null) return;
        var g = e.Graphics;
        float s = PixelScale;
        float pw = ReportLayout.PageWidth * s, ph = ReportLayout.PageHeight * s;
        float x0 = Math.Max(Gap, (ClientSize.Width - pw) / 2) + AutoScrollPosition.X;
        float y = Gap + AutoScrollPosition.Y;
        using var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        for (int i = 0; i < _layout.PageCount; i++, y += ph + Gap)
        {
            if (y > e.ClipRectangle.Bottom || y + ph < e.ClipRectangle.Top) continue;
            g.FillRectangle(shadow, x0 + 3, y + 3, pw, ph);
            g.FillRectangle(Brushes.White, x0, y, pw, ph);
            var state = g.Save();
            g.SetClip(new RectangleF(x0, y, pw, ph), CombineMode.Intersect);
            g.TranslateTransform(x0, y);
            g.ScaleTransform(s, s);
            _layout.DrawPage(g, i);
            g.Restore(state);
        }
    }
}
