namespace pdf;

/// <summary>Selects the interactive field created by a form template.</summary>
public enum PdfFormTemplateFieldKind
{
    /// <summary>Creates a text-entry field.</summary>
    Text = 0,
    /// <summary>Creates a checkbox.</summary>
    Checkbox = 1,
    /// <summary>Creates a digital-signature placeholder.</summary>
    Signature = 2
}
