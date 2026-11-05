using System;

namespace xlsx;

/// <summary>
/// Describes one deterministic feature update made by a structural edit.
/// </summary>
public class XlsxStructuralReferenceUpdate
{
    /// <summary>
    /// Stores the feature category.
    /// </summary>
    public XlsxStructuralReferenceCategory Category;
    /// <summary>
    /// Stores a stable target identifier within the edited workbook.
    /// </summary>
    public string Target;
    /// <summary>
    /// Stores the previous reference or state.
    /// </summary>
    public string PreviousReference;
    /// <summary>
    /// Stores the updated reference or state.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Indicates whether the target reference or object was removed.
    /// </summary>
    public bool Removed;

    /// <summary>
    /// Initializes an empty structural reference update.
    /// </summary>
    public XlsxStructuralReferenceUpdate()
    {
        Category = XlsxStructuralReferenceCategory.Formula;
        Target = "";
        PreviousReference = "";
        Reference = "";
        Removed = false;
    }
}
