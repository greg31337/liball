using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Stores the title and ordered sections used to generate a simple report presentation.
/// </summary>
public class PptxReportData
{
    /// <summary>
    /// Stores the report title shown on the first slide.
    /// </summary>
    public string Title;
    /// <summary>
    /// Stores the optional report subtitle shown on the first slide.
    /// </summary>
    public string Subtitle;
    /// <summary>
    /// Contains report sections in generated slide order.
    /// </summary>
    public List<PptxReportSection> Sections;

    /// <summary>
    /// Initializes empty report data.
    /// </summary>
    public PptxReportData()
    {
        Title = "";
        Subtitle = "";
        Sections = new List<PptxReportSection>();
    }
}
