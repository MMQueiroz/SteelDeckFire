using SteelDeckFire.Core.Calc;

namespace SteelDeckFire.Core.Report;

/// <summary>
/// Memorial de cálculo como sequência de blocos, independente de formato.
/// A interface desenha os blocos (tela, impressão e PDF).
/// </summary>
/// <remarks>
/// Marcação dos textos: <c>_{…}</c> subscrito, <c>^{…}</c> sobrescrito, <c>*…*</c> itálico e
/// <c>\</c> para usar um desses caracteres literalmente (ver <see cref="ReportBuilder.Escape"/>).
/// </remarks>
public sealed class ReportDocument
{
    public string Title { get; init; } = "";
    public List<ReportBlock> Blocks { get; } = new();
}

public abstract record ReportBlock;

/// <summary>Cabeçalho do memorial.</summary>
public sealed record TitleBlock(string Kind, string Title, string Meta) : ReportBlock;

/// <summary>Quadro de resumo com números principais e veredito.</summary>
public sealed record SummaryBlock(bool Ok, IReadOnlyList<SummaryFigure> Figures, string Text) : ReportBlock;

public sealed record SummaryFigure(string Value, string Label, bool Highlight = false);

/// <summary>Título de seção; com <see cref="Step"/> é numerado como passo de cálculo.</summary>
public sealed record HeadingBlock(string Title, int? Step = null) : ReportBlock;

public sealed record ParagraphBlock(string Text) : ReportBlock;

/// <summary>Equação em forma literal, substituída e resultado.</summary>
public sealed record EquationBlock(string Symbolic, string Substituted, string Result, string Unit, string? Note) : ReportBlock;

public sealed record VerdictBlock(bool Ok, string Text) : ReportBlock;

public enum ReportFigure { Plan, Section, TimeChart }

/// <summary>Figura desenhada pela interface; <see cref="AspectRatio"/> = altura / largura.</summary>
public sealed record FigureBlock(ReportFigure Figure, double AspectRatio, string Caption) : ReportBlock;

public sealed record TableBlock(string[] Header, string[][] Rows) : ReportBlock;

/// <summary>Tabela de verificações com a situação de cada uma.</summary>
public sealed record ChecksBlock(IReadOnlyList<Check> Checks) : ReportBlock;
