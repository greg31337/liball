namespace pptx;

/// <summary>
/// Describes one changed field in an editable-model summary comparison.
/// </summary>
public class PptxEditableModelDifference
{
    /// <summary>Stores the public summary field name.</summary>
    public string FieldName;
    /// <summary>Stores the invariant text representation of the left value.</summary>
    public string LeftValue;
    /// <summary>Stores the invariant text representation of the right value.</summary>
    public string RightValue;

    /// <summary>
    /// Initializes an empty editable-model difference.
    /// </summary>
    public PptxEditableModelDifference()
    {
        FieldName = "";
        LeftValue = "";
        RightValue = "";
    }
}
