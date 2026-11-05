namespace pptx;

/// <summary>
/// Generates simple report presentations from ordered data using built-in presentation operations.
/// </summary>
public class PptxReportGenerator : IPptxReportGenerator
{
    /// <summary>
    /// Creates a report generator instance.
    /// </summary>
    public PptxReportGenerator()
    {
    }

    /// <summary>
    /// Generates a title slide followed by one slide for each report section.
    /// </summary>
    /// <param name="data">The report data to generate.</param>
    /// <returns>A valid editable presentation document.</returns>
    public PptxDocument Generate(PptxReportData data)
    {
        return PptxReportGeneratorCore.Generate(data);
    }
}
