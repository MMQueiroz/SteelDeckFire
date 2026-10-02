using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Text.Json;
using SteelDeckFire.App.Views;
using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Models;
using SteelDeckFire.Core.Report;

namespace SteelDeckFire.App;

internal sealed partial class MainForm : Form
{
    private ProjectInput _input = new();
    private DesignResult? _result;
    private List<TimePoint> _sweep = new();
    private string? _currentFile;
    private ReportLayout? _report;
    private bool _reportDirty = true;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public MainForm()
    {
        InitializeComponent();
        LoadInput(new ProjectInput());
    }

    // ================================================================ eventos
    private void btnNew_Click(object? sender, EventArgs e) { _currentFile = null; LoadInput(new ProjectInput()); }
    private void btnOpen_Click(object? sender, EventArgs e) => OpenProject();
    private void btnSave_Click(object? sender, EventArgs e) => SaveProject(false);
    private void btnSaveAs_Click(object? sender, EventArgs e) => SaveProject(true);
    private void btnExample_Click(object? sender, EventArgs e) { _currentFile = null; LoadInput(ProjectInput.FracofExample()); }
    private void btnExportPdf_Click(object? sender, EventArgs e) => ExportPdf();
    private void btnPrintPreview_Click(object? sender, EventArgs e) => PrintPreview();
    private void btnPrint_Click(object? sender, EventArgs e) => Print();

    private void grid_PropertyValueChanged(object? s, PropertyValueChangedEventArgs e) { debounce.Stop(); debounce.Start(); }
    private void debounce_Tick(object? sender, EventArgs e) { debounce.Stop(); Recalculate(); }
    private void tabs_SelectedIndexChanged(object? sender, EventArgs e) { if (tabs.SelectedTab == tabReport && _reportDirty) UpdateReport(); }

    // ================================================================ cálculo
    private void LoadInput(ProjectInput inp)
    {
        _input = inp;
        grid.SelectedObject = _input;
        grid.ExpandAllGridItems();
        Recalculate();
    }

    private string? ValidateInput()
    {
        var i = _input;
        if (i.L1 <= 0 || i.L2 <= 0) return "L1 e L2 devem ser positivos.";
        if (i.SlabThickness <= i.DeckH2) return "A altura total da laje deve ser maior que a altura da fôrma.";
        if (i.DeckL1 <= 0 || i.DeckL2 <= 0 || i.DeckL3 <= 0) return "Dimensões da nervura (l1, l2, l3) devem ser positivas.";
        if (i.AsAlongL1 <= 0 || i.AsAlongL2 <= 0) return "Informe as áreas da tela nas duas direções.";
        if (i.MeshDepth <= 0 || i.MeshDepth >= i.SlabThickness - i.DeckH2) return "O eixo da tela deve ficar no concreto acima da fôrma.";
        if (i.Fck <= 0 || i.MeshFy <= 0) return "Resistências dos materiais devem ser positivas.";
        if (i.FireTime < 30 || i.FireTime > 180) return "O TRRF deve estar entre 30 e 180 min (faixa da tabela de temperaturas).";
        if (i.UnprotectedBeams > 0 && (i.BeamArea <= 0 || i.BeamH <= 0 || i.BeamB <= 0 || i.BeamTw <= 0 || i.BeamTf <= 0))
            return "Dados do perfil das vigas internas incompletos.";
        return null;
    }

    private void Recalculate()
    {
        var err = ValidateInput();
        if (err is not null)
        {
            statusLabel.Text = "Dados inválidos: " + err;
            statusLabel.ForeColor = Theme.Heat;
            return;
        }
        try
        {
            _result = FireDesign.Run(_input);
            _sweep = FireDesign.TimeSweep(_input);
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Erro no cálculo: " + ex.Message;
            statusLabel.ForeColor = Theme.Heat;
            return;
        }

        resultStrip.SetData(_input, _result);
        planView.SetData(_input, _result);
        sectionView.SetData(_input, _result);
        chartView.SetData(_sweep, _result.Load.QfiSd, _input.FireTime);
        FillChecks(_result);

        statusLabel.ForeColor = Theme.Ink;
        statusLabel.Text = $"Calculado · q_fi,Rd = {Theme.F(_result.QfiRd)} kN/m² (laje {Theme.F(_result.Membrane.QSlab)} + vigas {Theme.F(_result.Beams.QBeams)}) · q_fi,Sd = {Theme.F(_result.Load.QfiSd)} kN/m²"
                     + (_currentFile is null ? "" : $" · {Path.GetFileName(_currentFile)}");

        _reportDirty = true;
        if (tabs.SelectedTab == tabReport) UpdateReport();
    }

    private void FillChecks(DesignResult r)
    {
        checksList.BeginUpdate();
        checksList.Items.Clear();
        foreach (var c in r.Checks)
        {
            var (lbl, col) = c.Status switch
            {
                CheckStatus.Ok => ("Atende", Theme.Good),
                CheckStatus.Warning => ("Atenção", Theme.Amber),
                _ => ("Não atende", Theme.Heat),
            };
            var it = new ListViewItem(new[] { lbl, c.Title, c.Detail }) { ForeColor = col, UseItemStyleForSubItems = false };
            it.SubItems[1].ForeColor = Theme.Ink;
            it.SubItems[2].ForeColor = Theme.Muted;
            checksList.Items.Add(it);
        }
        checksList.EndUpdate();
    }

    // ================================================================ memorial
    private ReportLayout? BuildReport()
    {
        if (_result is null) return null;
        var doc = ReportBuilder.Build(_input, _result, _sweep);
        return new ReportLayout(doc, fig => fig switch
        {
            ReportFigure.Plan => planView.Draw,
            ReportFigure.Section => sectionView.Draw,
            ReportFigure.TimeChart => chartView.Draw,
            _ => null,
        });
    }

    /// <summary>Memorial atualizado com os dados atuais, ou null se não há resultado.</summary>
    private ReportLayout? CurrentReport()
    {
        if (_reportDirty || _report is null)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                var old = _report;
                _report = BuildReport();
                reportView.SetLayout(_report);
                old?.Dispose();
                _reportDirty = false;
            }
            finally { Cursor = Cursors.Default; }
        }
        return _report;
    }

    private void UpdateReport() => CurrentReport();

    private PrintDocument? CreatePrintDocument()
    {
        var layout = CurrentReport();
        if (layout is null) return null;
        var doc = new PrintDocument { DocumentName = $"Memorial - {Sanitize(_input.PanelName)}" };
        var a4 = doc.PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p => p.Kind == PaperKind.A4);
        if (a4 is not null) doc.DefaultPageSettings.PaperSize = a4;
        doc.DefaultPageSettings.Landscape = false;

        int page = 0, last = 0;
        doc.BeginPrint += (_, _) =>
        {
            var ps = doc.PrinterSettings;
            bool some = ps.PrintRange == PrintRange.SomePages;
            page = some ? Math.Clamp(ps.FromPage, 1, layout.PageCount) - 1 : 0;
            last = some ? Math.Clamp(ps.ToPage, page + 1, layout.PageCount) - 1 : layout.PageCount - 1;
        };
        doc.PrintPage += (_, e) =>
        {
            var g = e.Graphics!;
            g.PageUnit = GraphicsUnit.Display; // 1/100 pol.
            // Na impressora a origem é a margem física; o layout usa a folha inteira.
            if (!doc.PrintController.IsPreview)
                g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);
            var bounds = e.PageBounds;
            float s = Math.Min(bounds.Width / ReportLayout.PageWidth, bounds.Height / ReportLayout.PageHeight);
            g.TranslateTransform((bounds.Width - ReportLayout.PageWidth * s) / 2, 0);
            g.ScaleTransform(s, s);
            layout.DrawPage(g, page);
            page++;
            e.HasMorePages = page <= last;
        };
        return doc;
    }

    private void PrintPreview()
    {
        using var doc = CreatePrintDocument();
        if (doc is null) return;
        using var dlg = new PrintPreviewDialog
        {
            Document = doc,
            UseAntiAlias = true,
            Width = Math.Min(1100, Screen.FromControl(this).WorkingArea.Width),
            Height = Math.Min(1000, Screen.FromControl(this).WorkingArea.Height),
            StartPosition = FormStartPosition.CenterParent,
            Text = "Visualizar impressão do memorial",
        };
        dlg.ShowDialog(this);
    }

    private void Print()
    {
        using var doc = CreatePrintDocument();
        if (doc is null) return;
        doc.PrinterSettings.MinimumPage = 1;
        doc.PrinterSettings.MaximumPage = _report!.PageCount;
        doc.PrinterSettings.FromPage = 1;
        doc.PrinterSettings.ToPage = _report.PageCount;
        using var dlg = new PrintDialog { Document = doc, AllowSomePages = true, UseEXDialog = true };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        TryPrint(doc, "Memorial enviado para impressão.");
    }

    private const string PdfPrinter = "Microsoft Print to PDF";

    private void ExportPdf()
    {
        if (_result is null) return;
        if (!PrinterSettings.InstalledPrinters.Cast<string>().Contains(PdfPrinter))
        {
            MessageBox.Show(this, $"A impressora \"{PdfPrinter}\" não está instalada.\n\nUse \"Imprimir…\" e escolha outra impressora PDF.",
                "Exportar PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new SaveFileDialog
        {
            Filter = "Documento PDF (*.pdf)|*.pdf",
            FileName = $"Memorial - {Sanitize(_input.PanelName)}.pdf",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        using var doc = CreatePrintDocument();
        if (doc is null) return;
        doc.PrinterSettings.PrinterName = PdfPrinter;
        doc.PrinterSettings.PrintToFile = true;
        doc.PrinterSettings.PrintFileName = dlg.FileName;
        var a4 = doc.PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p => p.Kind == PaperKind.A4);
        if (a4 is not null) doc.DefaultPageSettings.PaperSize = a4;
        doc.PrintController = new StandardPrintController(); // sem janela de progresso
        TryPrint(doc, $"Memorial exportado: {dlg.FileName}");
    }

    private void TryPrint(PrintDocument doc, string done)
    {
        Cursor = Cursors.WaitCursor;
        try
        {
            doc.Print();
            statusLabel.ForeColor = Theme.Ink;
            statusLabel.Text = done;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível imprimir o memorial.\n\n{ex.Message}", "Memorial",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { Cursor = Cursors.Default; }
    }

    private static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Trim();
    }

    // ================================================================ arquivos
    private void SaveProject(bool saveAs)
    {
        if (saveAs || _currentFile is null)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "Projeto SteelDeck Fire (*.sdfire.json)|*.sdfire.json",
                FileName = Sanitize(_input.PanelName) + ".sdfire.json",
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            _currentFile = dlg.FileName;
        }
        File.WriteAllText(_currentFile, JsonSerializer.Serialize(_input, JsonOpts), Encoding.UTF8);
        statusLabel.Text = $"Projeto salvo: {_currentFile}";
    }

    private void OpenProject()
    {
        using var dlg = new OpenFileDialog { Filter = "Projeto SteelDeck Fire (*.sdfire.json)|*.sdfire.json|JSON (*.json)|*.json" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var inp = JsonSerializer.Deserialize<ProjectInput>(File.ReadAllText(dlg.FileName));
            if (inp is null) throw new InvalidDataException("Arquivo vazio.");
            _currentFile = dlg.FileName;
            LoadInput(inp);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível abrir o projeto.\n\n{ex.Message}", "Abrir projeto",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
