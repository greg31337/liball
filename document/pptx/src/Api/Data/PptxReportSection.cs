using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Stores one titled collection of lines used to generate a report slide.
/// </summary>
public class PptxReportSection
{
    /// <summary>
    /// Stores the section title.
    /// </summary>
    public string Title;
    /// <summary>
    /// Contains body lines in generated paragraph order.
    /// </summary>
    public List<string> Lines;

    /// <summary>
    /// Initializes an empty report section.
    /// </summary>
    public PptxReportSection()
    {
        Title = "";
        Lines = new List<string>();
    }
}
