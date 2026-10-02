#nullable disable

namespace SteelDeckFire.App
{
    partial class MainForm
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _report?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolStrip = new ToolStrip();
            btnNew = new ToolStripButton();
            btnOpen = new ToolStripButton();
            btnSave = new ToolStripButton();
            btnSaveAs = new ToolStripButton();
            sep1 = new ToolStripSeparator();
            btnExample = new ToolStripButton();
            sep2 = new ToolStripSeparator();
            btnExportPdf = new ToolStripButton();
            btnPrintPreview = new ToolStripButton();
            btnPrint = new ToolStripButton();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            split = new SplitContainer();
            grid = new PropertyGrid();
            lblInput = new Label();
            tabs = new TabControl();
            tabPlan = new TabPage();
            planView = new SteelDeckFire.App.Views.PlanView();
            tabSection = new TabPage();
            sectionView = new SteelDeckFire.App.Views.SectionView();
            tabChart = new TabPage();
            chartView = new SteelDeckFire.App.Views.TimeChartView();
            tabChecks = new TabPage();
            checksList = new ListView();
            colStatus = new ColumnHeader();
            colTitle = new ColumnHeader();
            colDetail = new ColumnHeader();
            tabReport = new TabPage();
            reportView = new SteelDeckFire.App.Views.ReportView();
            resultStrip = new SteelDeckFire.App.Views.ResultStrip();
            debounce = new System.Windows.Forms.Timer(components);
            toolStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)split).BeginInit();
            split.Panel1.SuspendLayout();
            split.Panel2.SuspendLayout();
            split.SuspendLayout();
            tabs.SuspendLayout();
            tabPlan.SuspendLayout();
            tabSection.SuspendLayout();
            tabChart.SuspendLayout();
            tabChecks.SuspendLayout();
            tabReport.SuspendLayout();
            SuspendLayout();
            //
            // toolStrip
            //
            toolStrip.BackColor = Color.White;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[] { btnNew, btnOpen, btnSave, btnSaveAs, sep1, btnExample, sep2, btnExportPdf, btnPrintPreview, btnPrint });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Padding = new Padding(8, 4, 8, 4);
            toolStrip.RenderMode = ToolStripRenderMode.System;
            toolStrip.Size = new Size(1424, 31);
            toolStrip.TabIndex = 1;
            //
            // btnNew
            //
            btnNew.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnNew.Margin = new Padding(0, 0, 6, 0);
            btnNew.Name = "btnNew";
            btnNew.Text = "Novo";
            btnNew.Click += btnNew_Click;
            //
            // btnOpen
            //
            btnOpen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnOpen.Margin = new Padding(0, 0, 6, 0);
            btnOpen.Name = "btnOpen";
            btnOpen.Text = "Abrir…";
            btnOpen.Click += btnOpen_Click;
            //
            // btnSave
            //
            btnSave.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnSave.Margin = new Padding(0, 0, 6, 0);
            btnSave.Name = "btnSave";
            btnSave.Text = "Salvar";
            btnSave.Click += btnSave_Click;
            //
            // btnSaveAs
            //
            btnSaveAs.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnSaveAs.Margin = new Padding(0, 0, 6, 0);
            btnSaveAs.Name = "btnSaveAs";
            btnSaveAs.Text = "Salvar como…";
            btnSaveAs.Click += btnSaveAs_Click;
            //
            // sep1
            //
            sep1.Name = "sep1";
            //
            // btnExample
            //
            btnExample.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnExample.Margin = new Padding(0, 0, 6, 0);
            btnExample.Name = "btnExample";
            btnExample.Text = "Carregar exemplo FRACOF";
            btnExample.Click += btnExample_Click;
            //
            // sep2
            //
            sep2.Name = "sep2";
            //
            // btnExportPdf
            //
            btnExportPdf.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnExportPdf.Margin = new Padding(0, 0, 6, 0);
            btnExportPdf.Name = "btnExportPdf";
            btnExportPdf.Text = "Exportar memorial em PDF…";
            btnExportPdf.Click += btnExportPdf_Click;
            //
            // btnPrintPreview
            //
            btnPrintPreview.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnPrintPreview.Margin = new Padding(0, 0, 6, 0);
            btnPrintPreview.Name = "btnPrintPreview";
            btnPrintPreview.Text = "Visualizar impressão";
            btnPrintPreview.Click += btnPrintPreview_Click;
            //
            // btnPrint
            //
            btnPrint.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnPrint.Margin = new Padding(0, 0, 6, 0);
            btnPrint.Name = "btnPrint";
            btnPrint.Text = "Imprimir…";
            btnPrint.Click += btnPrint_Click;
            //
            // statusStrip
            //
            statusStrip.BackColor = Color.White;
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
            statusStrip.Location = new Point(0, 839);
            statusStrip.Name = "statusStrip";
            statusStrip.SizingGrip = false;
            statusStrip.Size = new Size(1424, 22);
            statusStrip.TabIndex = 2;
            //
            // statusLabel
            //
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(1409, 17);
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // split
            //
            split.BackColor = Color.FromArgb(214, 221, 226);
            split.Dock = DockStyle.Fill;
            split.FixedPanel = FixedPanel.Panel1;
            split.Location = new Point(0, 31);
            split.Name = "split";
            //
            // split.Panel1
            //
            split.Panel1.BackColor = Color.White;
            split.Panel1.Controls.Add(grid);
            split.Panel1.Controls.Add(lblInput);
            //
            // split.Panel2
            //
            split.Panel2.BackColor = Color.White;
            split.Panel2.Controls.Add(tabs);
            split.Panel2.Controls.Add(resultStrip);
            split.Size = new Size(1424, 808);
            split.SplitterDistance = 430;
            split.SplitterWidth = 6;
            split.TabIndex = 0;
            //
            // grid
            //
            grid.CategoryForeColor = Color.FromArgb(29, 40, 48);
            grid.Dock = DockStyle.Fill;
            grid.LineColor = Color.FromArgb(214, 221, 226);
            grid.Location = new Point(0, 34);
            grid.Name = "grid";
            grid.PropertySort = PropertySort.Categorized;
            grid.Size = new Size(430, 774);
            grid.TabIndex = 1;
            grid.ToolbarVisible = false;
            grid.ViewBackColor = Color.White;
            grid.PropertyValueChanged += grid_PropertyValueChanged;
            //
            // lblInput
            //
            lblInput.Dock = DockStyle.Top;
            lblInput.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblInput.ForeColor = Color.FromArgb(29, 40, 48);
            lblInput.Location = new Point(0, 0);
            lblInput.Name = "lblInput";
            lblInput.Padding = new Padding(10, 8, 0, 0);
            lblInput.Size = new Size(430, 34);
            lblInput.TabIndex = 0;
            lblInput.Text = "Dados do painel";
            //
            // tabs
            //
            tabs.Controls.Add(tabPlan);
            tabs.Controls.Add(tabSection);
            tabs.Controls.Add(tabChart);
            tabs.Controls.Add(tabChecks);
            tabs.Controls.Add(tabReport);
            tabs.Dock = DockStyle.Fill;
            tabs.Location = new Point(0, 92);
            tabs.Name = "tabs";
            tabs.Padding = new Point(14, 6);
            tabs.SelectedIndex = 0;
            tabs.Size = new Size(988, 716);
            tabs.TabIndex = 1;
            tabs.SelectedIndexChanged += tabs_SelectedIndexChanged;
            //
            // tabPlan
            //
            tabPlan.BackColor = Color.FromArgb(250, 251, 252);
            tabPlan.Controls.Add(planView);
            tabPlan.Location = new Point(4, 32);
            tabPlan.Name = "tabPlan";
            tabPlan.Size = new Size(980, 680);
            tabPlan.TabIndex = 0;
            tabPlan.Text = "Planta e linhas de ruptura";
            //
            // planView
            //
            planView.Dock = DockStyle.Fill;
            planView.Location = new Point(0, 0);
            planView.Name = "planView";
            planView.Size = new Size(980, 680);
            planView.TabIndex = 0;
            //
            // tabSection
            //
            tabSection.BackColor = Color.FromArgb(250, 251, 252);
            tabSection.Controls.Add(sectionView);
            tabSection.Location = new Point(4, 32);
            tabSection.Name = "tabSection";
            tabSection.Size = new Size(980, 680);
            tabSection.TabIndex = 1;
            tabSection.Text = "Seção da laje";
            //
            // sectionView
            //
            sectionView.Dock = DockStyle.Fill;
            sectionView.Location = new Point(0, 0);
            sectionView.Name = "sectionView";
            sectionView.Size = new Size(980, 680);
            sectionView.TabIndex = 0;
            //
            // tabChart
            //
            tabChart.BackColor = Color.FromArgb(250, 251, 252);
            tabChart.Controls.Add(chartView);
            tabChart.Location = new Point(4, 32);
            tabChart.Name = "tabChart";
            tabChart.Size = new Size(980, 680);
            tabChart.TabIndex = 2;
            tabChart.Text = "Resistência × tempo";
            //
            // chartView
            //
            chartView.Dock = DockStyle.Fill;
            chartView.Location = new Point(0, 0);
            chartView.Name = "chartView";
            chartView.Size = new Size(980, 680);
            chartView.TabIndex = 0;
            //
            // tabChecks
            //
            tabChecks.BackColor = Color.FromArgb(250, 251, 252);
            tabChecks.Controls.Add(checksList);
            tabChecks.Location = new Point(4, 32);
            tabChecks.Name = "tabChecks";
            tabChecks.Size = new Size(980, 680);
            tabChecks.TabIndex = 3;
            tabChecks.Text = "Verificações";
            //
            // checksList
            //
            checksList.BorderStyle = BorderStyle.None;
            checksList.Columns.AddRange(new ColumnHeader[] { colStatus, colTitle, colDetail });
            checksList.Dock = DockStyle.Fill;
            checksList.Font = new Font("Segoe UI", 9.5F);
            checksList.FullRowSelect = true;
            checksList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            checksList.Location = new Point(0, 0);
            checksList.Name = "checksList";
            checksList.Size = new Size(980, 680);
            checksList.TabIndex = 0;
            checksList.UseCompatibleStateImageBehavior = false;
            checksList.View = View.Details;
            //
            // colStatus
            //
            colStatus.Text = "Situação";
            colStatus.Width = 110;
            //
            // colTitle
            //
            colTitle.Text = "Verificação";
            colTitle.Width = 300;
            //
            // colDetail
            //
            colDetail.Text = "Detalhe";
            colDetail.Width = 800;
            //
            // tabReport
            //
            tabReport.BackColor = Color.FromArgb(250, 251, 252);
            tabReport.Controls.Add(reportView);
            tabReport.Location = new Point(4, 32);
            tabReport.Name = "tabReport";
            tabReport.Size = new Size(980, 680);
            tabReport.TabIndex = 4;
            tabReport.Text = "Memorial de cálculo";
            //
            // reportView
            //
            reportView.Dock = DockStyle.Fill;
            reportView.Location = new Point(0, 0);
            reportView.Name = "reportView";
            reportView.Size = new Size(980, 680);
            reportView.TabIndex = 0;
            //
            // resultStrip
            //
            resultStrip.Dock = DockStyle.Top;
            resultStrip.Location = new Point(0, 0);
            resultStrip.Name = "resultStrip";
            resultStrip.Size = new Size(988, 92);
            resultStrip.TabIndex = 0;
            //
            // debounce
            //
            debounce.Interval = 250;
            debounce.Tick += debounce_Tick;
            //
            // MainForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1424, 861);
            Controls.Add(split);
            Controls.Add(toolStrip);
            Controls.Add(statusStrip);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(1000, 640);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SteelDeck Fire · laje mista em incêndio com ação de membrana";
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            split.Panel1.ResumeLayout(false);
            split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)split).EndInit();
            split.ResumeLayout(false);
            tabs.ResumeLayout(false);
            tabPlan.ResumeLayout(false);
            tabSection.ResumeLayout(false);
            tabChart.ResumeLayout(false);
            tabChecks.ResumeLayout(false);
            tabReport.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip;
        private ToolStripButton btnNew;
        private ToolStripButton btnOpen;
        private ToolStripButton btnSave;
        private ToolStripButton btnSaveAs;
        private ToolStripSeparator sep1;
        private ToolStripButton btnExample;
        private ToolStripSeparator sep2;
        private ToolStripButton btnExportPdf;
        private ToolStripButton btnPrintPreview;
        private ToolStripButton btnPrint;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private SplitContainer split;
        private PropertyGrid grid;
        private Label lblInput;
        private TabControl tabs;
        private TabPage tabPlan;
        private SteelDeckFire.App.Views.PlanView planView;
        private TabPage tabSection;
        private SteelDeckFire.App.Views.SectionView sectionView;
        private TabPage tabChart;
        private SteelDeckFire.App.Views.TimeChartView chartView;
        private TabPage tabChecks;
        private ListView checksList;
        private ColumnHeader colStatus;
        private ColumnHeader colTitle;
        private ColumnHeader colDetail;
        private TabPage tabReport;
        private SteelDeckFire.App.Views.ReportView reportView;
        private SteelDeckFire.App.Views.ResultStrip resultStrip;
        private System.Windows.Forms.Timer debounce;
    }
}
