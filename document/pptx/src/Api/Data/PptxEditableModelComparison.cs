using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Describes differences between two editable presentation-model summaries.
/// </summary>
public class PptxEditableModelComparison
{
    /// <summary>Stores the left editable-model summary.</summary>
    public PptxEditableModelSummary Left;
    /// <summary>Stores the right editable-model summary.</summary>
    public PptxEditableModelSummary Right;
    /// <summary>Indicates whether every summarized model field is equal.</summary>
    public bool Equal;
    /// <summary>Contains changed fields in stable summary-field order.</summary>
    public List<PptxEditableModelDifference> Differences;

    /// <summary>
    /// Initializes an empty editable-model comparison.
    /// </summary>
    public PptxEditableModelComparison()
    {
        Left = new PptxEditableModelSummary();
        Right = new PptxEditableModelSummary();
        Equal = true;
        Differences = new List<PptxEditableModelDifference>();
    }
}
