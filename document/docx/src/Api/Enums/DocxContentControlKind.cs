namespace docx;

/// <summary>
/// Identifies a supported content-control kind.
/// </summary>
public enum DocxContentControlKind
{
    /// <summary>An untyped or unsupported content control.</summary>
    Unknown = 0,
    /// <summary>A plain-text content control.</summary>
    PlainText = 1,
    /// <summary>A rich-text content control.</summary>
    RichText = 2,
    /// <summary>A repeating-section content control.</summary>
    RepeatingSection = 3,
    /// <summary>A repeating-section item.</summary>
    RepeatingSectionItem = 4,
    /// <summary>A checkbox content control.</summary>
    Checkbox = 5,
    /// <summary>An editable combo-box content control.</summary>
    ComboBox = 6,
    /// <summary>A drop-down-list content control.</summary>
    DropDownList = 7,
    /// <summary>A date-picker content control.</summary>
    DatePicker = 8,
    /// <summary>A picture content control.</summary>
    Picture = 9
}
