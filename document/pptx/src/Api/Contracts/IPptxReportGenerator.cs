namespace pptx;

/// <summary>
/// Defines dependency-free generation of simple report presentations from ordered data.
/// </summary>
public interface IPptxReportGenerator
{
    /// <summary>
    /// Generates a title slide followed by one slide for each report section.
    /// </summary>
    /// <param name="data">The report data to generate.</param>
    /// <returns>A valid editable presentation document.</returns>
    PptxDocument Generate(PptxReportData data);
}
