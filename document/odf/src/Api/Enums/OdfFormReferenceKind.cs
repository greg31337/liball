namespace odf;

/// <summary>
/// Identifies a relationship involving a form control.
/// </summary>
public enum OdfFormReferenceKind
{
    /// <summary>The relationship is not recognized.</summary>
    Unknown = 0,
    /// <summary>A label or grouping control names another control.</summary>
    LabelTarget = 1,
    /// <summary>A drawing control shape displays the form control.</summary>
    DrawingControl = 2
}
