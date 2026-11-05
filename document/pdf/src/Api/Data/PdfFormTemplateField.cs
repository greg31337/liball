using System;

namespace pdf;

/// <summary>Describes one labeled interactive form field.</summary>
public class PdfFormTemplateField
{
    /// <summary>Stores the unique PDF field name.</summary>
    public string Name = string.Empty;
    /// <summary>Stores the displayed field label.</summary>
    public string Label = string.Empty;
    /// <summary>Stores the field kind.</summary>
    public PdfFormTemplateFieldKind Kind;

    /// <summary>Initializes a form-template field.</summary>
    /// <param name="name">The unique PDF field name.</param>
    /// <param name="label">The displayed field label.</param>
    /// <param name="kind">The field kind.</param>
    public PdfFormTemplateField(string name, string label, PdfFormTemplateFieldKind kind)
    {
        if (name == null) { throw new ArgumentNullException("name"); }
        if (label == null) { throw new ArgumentNullException("label"); }
        if (name.Length == 0) { throw new ArgumentException("Form field name cannot be empty.", "name"); }
        Name = name;
        Label = label;
        Kind = kind;
    }
}
