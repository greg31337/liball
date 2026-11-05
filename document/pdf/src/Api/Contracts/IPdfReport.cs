using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfReport"/>.
/// </summary>
public interface IPdfReport
{
    /// <summary>Returns the underlying document for resource and metadata configuration.</summary>
    /// <returns>The report document.</returns>
    PdfDocument GetDocument();

    /// <summary>Adds a dedicated first-page report cover.</summary>
    /// <param name="title">The cover title.</param>
    /// <param name="subtitle">The cover subtitle.</param>
    /// <param name="author">The author or organization line.</param>
    /// <param name="reportingPeriod">The reporting-period line.</param>
    void AddCoverPage(string title, string subtitle, string author, string reportingPeriod);

    /// <summary>
    /// Adds a title to this report.
    /// </summary>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="subtitle">The subtitle to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddTitle(string title, string subtitle);

    /// <summary>
    /// Adds a section to this report.
    /// </summary>
    /// <param name="heading">The heading to use for this operation.</param>
    /// <param name="text">The text to measure, draw, or store.</param>
    void AddSection(string heading, string text);

    /// <summary>
    /// Adds a section to this report.
    /// </summary>
    /// <param name="section">The section to add to the report.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddReportSection(PdfReportSection section);

    /// <summary>Adds a styled executive-summary panel.</summary>
    /// <param name="text">The executive-summary text.</param>
    void AddExecutiveSummary(string text);

    /// <summary>Adds a subsection heading and body.</summary>
    /// <param name="heading">The subsection heading.</param>
    /// <param name="text">The subsection body.</param>
    void AddSubsection(string heading, string text);

    /// <summary>
    /// Adds a paragraph to this report.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddParagraph(string text);

    /// <summary>
    /// Adds a KPI to this report.
    /// </summary>
    /// <param name="label">The label to use for this operation.</param>
    /// <param name="value">The value to use for this operation.</param>
    void AddKpi(string label, string value);

    /// <summary>
    /// Adds a KPI to this report.
    /// </summary>
    /// <param name="kpi">The KPI to add to the report.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddReportKpi(PdfKpi kpi);

    /// <summary>Adds KPI cards in a multi-column layout.</summary>
    /// <param name="kpis">The KPI cards.</param>
    /// <param name="columnCount">The positive column count.</param>
    void AddKpiLayout(PdfKpi[] kpis, int columnCount);

    /// <summary>
    /// Adds a table to this report.
    /// </summary>
    /// <param name="table">The table to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddTable(PdfReportTable table);

    /// <summary>
    /// Adds a figure to this report.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="caption">The caption to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddFigure(PdfImage image, string caption);

    /// <summary>Adds a chart placeholder with a semantic caption.</summary>
    /// <param name="label">The chart label.</param>
    /// <param name="caption">The chart caption.</param>
    /// <param name="height">The positive chart height.</param>
    void AddChartFigure(string label, string caption, double height);

    /// <summary>Starts and adds an appendix section.</summary>
    /// <param name="heading">The appendix heading.</param>
    /// <param name="text">The appendix body.</param>
    void AddAppendix(string heading, string text);

    /// <summary>Configures the report header and footer text.</summary>
    /// <param name="reportName">The running report name.</param>
    /// <param name="footerText">The footer prefix placed before page numbering.</param>
    void SetReportPageFurniture(string reportName, string footerText);

    /// <summary>
    /// Builds a report from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    PdfDocument Build();
}
