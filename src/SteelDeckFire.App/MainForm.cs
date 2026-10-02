using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
    private string _lastHtml = "";
    private bool _reportDirty = true;

    private readonly PropertyGrid _grid = new();
    private readonly ResultStrip _strip = new();
    private readonly PlanView _plan = new();
    private readonly SectionView _section = new();
    private readonly TimeChartView _chart = new();
    private readonly ListView _checks = new();
    private readonly WebBrowser _browser = new();
    private readonly TabControl _tabs = new();
    private readonly TabPage _tabReport = new("Memorial de cálculo");
    private readonly ToolStripStatusLabel _status = new();
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 250 };

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public MainForm()
    {
        Text = "SteelDeck Fire · laje mista em incêndio com ação de membrana";
        Font = Theme.UiFont(9f);
        Size = new Size(1440, 900);
        MinimumSize = new Size(1100, 700);
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
        tool.Items.Add(Btn("Exportar memorial…", (_, _) => ExportReport()));
        tool.Items.Add(Btn("Abrir memorial no navegador", (_, _) => OpenReportInBrowser()));

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
            Height = 34,
            Padding = new Padding(10, 8, 0, 0),
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

        _browser.Dock = DockStyle.Fill;
        _browser.ScriptErrorsSuppressed = true;
        _browser.AllowWebBrowserDrop = false;

        _tabs.TabPages.Add(Page("Planta e linhas de ruptura", _plan));
        _tabs.TabPages.Add(Page("Seção da laje", _section));
        _tabs.TabPages.Add(Page("Resistência × tempo", _chart));
        _tabs.TabPages.Add(Page("Verificações", _checks));
        _tabReport.Controls.Add(_browser);
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

        Shown += (_, _) => split.SplitterDistance = 400;
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
    private string BuildReportHtml()
    {
        if (_result is null) return "";
        string Png(Bitmap bmp)
        {
            using (bmp)
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        var imgs = new ReportImages(
            Png(_plan.Render(1300, 900)),
            Png(_section.Render(1300, 520)),
            Png(_chart.Render(1300, 620)));
        return ReportBuilder.Build(_input, _result, _sweep, imgs);
    }

    private void UpdateReport()
    {
        if (_result is null) return;
        Cursor = Cursors.WaitCursor;
        try
        {
            _lastHtml = BuildReportHtml();
            _browser.DocumentText = _lastHtml;
            _reportDirty = false;
        }
        finally { Cursor = Cursors.Default; }
    }

    private void ExportReport()
    {
        if (_result is null) return;
        if (_reportDirty || string.IsNullOrEmpty(_lastHtml)) { _lastHtml = BuildReportHtml(); _reportDirty = false; }
        using var dlg = new SaveFileDialog
        {
            Filter = "Página HTML (*.html)|*.html",
            FileName = $"Memorial - {Sanitize(_input.PanelName)}.html",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dlg.FileName, _lastHtml, new UTF8Encoding(true));
        _status.Text = $"Memorial exportado: {dlg.FileName}. Para PDF, abra no navegador e imprima.";
    }

    private void OpenReportInBrowser()
    {
        if (_result is null) return;
        if (_reportDirty || string.IsNullOrEmpty(_lastHtml)) { _lastHtml = BuildReportHtml(); _reportDirty = false; }
        var path = Path.Combine(Path.GetTempPath(), $"SteelDeckFire_{Sanitize(_input.PanelName)}.html");
        File.WriteAllText(path, _lastHtml, new UTF8Encoding(true));
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
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
