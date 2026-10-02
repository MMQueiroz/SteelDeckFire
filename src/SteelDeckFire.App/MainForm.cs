using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Text.Json;
using SteelDeckFire.App.Views;
using SteelDeckFire.Core.Calc;
using SteelDeckFire.Core.Models;
using SteelDeckFire.Core.Report;

namespace SteelDeckFire.App;

internal sealed class MainForm : Form
{
    private ProjectInput _input = new();
    private DesignResult? _result;
    private List<TimePoint> _sweep = new();
    private string? _currentFile;
    private ReportLayout? _report;
    private bool _reportDirty = true;

    private readonly PropertyGrid _grid = new();
    private readonly ResultStrip _strip = new();
    private readonly PlanView _plan = new();
    private readonly SectionView _section = new();
    private readonly TimeChartView _chart = new();
    private readonly ListView _checks = new();
    private readonly ReportView _reportView = new();
    private readonly TabControl _tabs = new();
    private readonly TabPage _tabReport = new("Memorial de cálculo");
    private readonly ToolStripStatusLabel _status = new();
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 250 };

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public MainForm()
    {
        Text = "SteelDeck Fire · laje mista em incêndio com ação de membrana";
        Font = Theme.UiFont(9f);
        var work = Screen.PrimaryScreen?.WorkingArea.Size ?? new Size(1440, 900);
        var size = LogicalToDeviceUnits(new Size(1440, 900));
        Size = new Size(Math.Min(size.Width, work.Width), Math.Min(size.Height, work.Height));
        MinimumSize = new Size(Math.Min(LogicalToDeviceUnits(1000), work.Width), Math.Min(LogicalToDeviceUnits(640), work.Height));
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        BuildLayout();

        _grid.PropertyValueChanged += (_, _) => { _debounce.Stop(); _debounce.Start(); };
        _debounce.Tick += (_, _) => { _debounce.Stop(); Recalculate(); };
        _tabs.SelectedIndexChanged += (_, _) => { if (_tabs.SelectedTab == _tabReport && _reportDirty) UpdateReport(); };

        LoadInput(new ProjectInput());
    }

    // ================================================================ layout
    private void BuildLayout()
    {
        // Ferramentas
        var tool = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(8, 4, 8, 4), BackColor = Color.White, RenderMode = ToolStripRenderMode.System };
        tool.Items.Add(Btn("Novo", (_, _) => { _currentFile = null; LoadInput(new ProjectInput()); }));
        tool.Items.Add(Btn("Abrir…", (_, _) => OpenProject()));
        tool.Items.Add(Btn("Salvar", (_, _) => SaveProject(false)));
        tool.Items.Add(Btn("Salvar como…", (_, _) => SaveProject(true)));
        tool.Items.Add(new ToolStripSeparator());
        tool.Items.Add(Btn("Carregar exemplo FRACOF", (_, _) => { _currentFile = null; LoadInput(ProjectInput.FracofExample()); }));
        tool.Items.Add(new ToolStripSeparator());
        tool.Items.Add(Btn("Exportar memorial em PDF…", (_, _) => ExportPdf()));
        tool.Items.Add(Btn("Visualizar impressão", (_, _) => PrintPreview()));
        tool.Items.Add(Btn("Imprimir…", (_, _) => Print()));

        var statusStrip = new StatusStrip { SizingGrip = false, BackColor = Color.White };
        _status.Spring = true;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        statusStrip.Items.Add(_status);

        // Entrada
        _grid.Dock = DockStyle.Fill;
        _grid.PropertySort = PropertySort.Categorized;
        _grid.ToolbarVisible = false;
        _grid.HelpVisible = true;
        _grid.LineColor = Theme.Rule;
        _grid.CategoryForeColor = Theme.Ink;
        _grid.ViewBackColor = Color.White;

        var inputHeader = new Label
        {
            Text = "Dados do painel",
            Dock = DockStyle.Top,
            Height = LogicalToDeviceUnits(34),
            Padding = new Padding(LogicalToDeviceUnits(10), LogicalToDeviceUnits(8), 0, 0),
            Font = Theme.UiFont(10.5f, FontStyle.Bold),
            ForeColor = Theme.Ink,
        };

        // Abas
        _tabs.Dock = DockStyle.Fill;
        _tabs.Padding = new Point(14, 6);
        _plan.Dock = DockStyle.Fill;
        _section.Dock = DockStyle.Fill;
        _chart.Dock = DockStyle.Fill;

        _checks.Dock = DockStyle.Fill;
        _checks.View = View.Details;
        _checks.FullRowSelect = true;
        _checks.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _checks.BorderStyle = BorderStyle.None;
        _checks.Font = Theme.UiFont(9.5f);
        _checks.Columns.Add("Situação", 110);
        _checks.Columns.Add("Verificação", 300);
        _checks.Columns.Add("Detalhe", 800);

        _reportView.Dock = DockStyle.Fill;

        _tabs.TabPages.Add(Page("Planta e linhas de ruptura", _plan));
        _tabs.TabPages.Add(Page("Seção da laje", _section));
        _tabs.TabPages.Add(Page("Resistência × tempo", _chart));
        _tabs.TabPages.Add(Page("Verificações", _checks));
        _tabReport.Controls.Add(_reportView);
        _tabs.TabPages.Add(_tabReport);

        _strip.Dock = DockStyle.Top;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 6,
            BackColor = Theme.Rule,
        };
        split.Panel1.BackColor = Color.White;
        split.Panel2.BackColor = Color.White;
        // Ordem de inclusão: Fill primeiro, depois Top/Bottom (o docking processa do último para o primeiro)
        split.Panel1.Controls.Add(_grid);
        split.Panel1.Controls.Add(inputHeader);
        split.Panel2.Controls.Add(_tabs);
        split.Panel2.Controls.Add(_strip);

        Controls.Add(split);
        Controls.Add(tool);
        Controls.Add(statusStrip);

        Shown += (_, _) => split.SplitterDistance = LogicalToDeviceUnits(430);
    }

    private static ToolStripButton Btn(string text, EventHandler onClick)
    {
        var b = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text, Margin = new Padding(0, 0, 6, 0) };
        b.Click += onClick;
        return b;
    }

    private static TabPage Page(string title, Control content)
    {
        var p = new TabPage(title) { BackColor = Theme.Paper };
        p.Controls.Add(content);
        return p;
    }

    // ================================================================ cálculo
    private void LoadInput(ProjectInput inp)
    {
        _input = inp;
        _grid.SelectedObject = _input;
        _grid.ExpandAllGridItems();
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
            _status.Text = "Dados inválidos: " + err;
            _status.ForeColor = Theme.Heat;
            return;
        }
        try
        {
            _result = FireDesign.Run(_input);
            _sweep = FireDesign.TimeSweep(_input);
        }
        catch (Exception ex)
        {
            _status.Text = "Erro no cálculo: " + ex.Message;
            _status.ForeColor = Theme.Heat;
            return;
        }

        _strip.SetData(_input, _result);
        _plan.SetData(_input, _result);
        _section.SetData(_input, _result);
        _chart.SetData(_sweep, _result.Load.QfiSd, _input.FireTime);
        FillChecks(_result);

        _status.ForeColor = Theme.Ink;
        _status.Text = $"Calculado · q_fi,Rd = {Theme.F(_result.QfiRd)} kN/m² (laje {Theme.F(_result.Membrane.QSlab)} + vigas {Theme.F(_result.Beams.QBeams)}) · q_fi,Sd = {Theme.F(_result.Load.QfiSd)} kN/m²"
                     + (_currentFile is null ? "" : $" · {Path.GetFileName(_currentFile)}");

        _reportDirty = true;
        if (_tabs.SelectedTab == _tabReport) UpdateReport();
    }

    private void FillChecks(DesignResult r)
    {
        _checks.BeginUpdate();
        _checks.Items.Clear();
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
            _checks.Items.Add(it);
        }
        _checks.EndUpdate();
    }

    // ================================================================ memorial
    private ReportLayout? BuildReport()
    {
        if (_result is null) return null;
        var doc = ReportBuilder.Build(_input, _result, _sweep);
        return new ReportLayout(doc, fig => fig switch
        {
            ReportFigure.Plan => _plan.Draw,
            ReportFigure.Section => _section.Draw,
            ReportFigure.TimeChart => _chart.Draw,
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
                _reportView.SetLayout(_report);
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
            _status.ForeColor = Theme.Ink;
            _status.Text = done;
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
        _status.Text = $"Projeto salvo: {_currentFile}";
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
