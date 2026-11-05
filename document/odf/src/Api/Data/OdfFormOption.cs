namespace odf;

/// <summary>
/// Describes one list-box option or combo-box item.
/// </summary>
public class OdfFormOption
{
    /// <summary>Whether the source element is a list-box option.</summary>
    public bool IsOption;
    /// <summary>The displayed label.</summary>
    public string Label = "";
    /// <summary>The submitted option value.</summary>
    public string Value = "";
    /// <summary>The element text.</summary>
    public string Text = "";
    /// <summary>The preserved default-selected lexical value.</summary>
    public string SelectedValue = "";
    /// <summary>Whether the option is selected by default.</summary>
    public bool IsSelected;
    /// <summary>The preserved current-selected lexical value.</summary>
    public string CurrentSelectedValue = "";
    /// <summary>Whether the option is currently selected.</summary>
    public bool IsCurrentSelected;
    /// <summary>The underlying item or option element.</summary>
    public OdfNode? Node;
    internal string OriginalLabel = "";
    internal string OriginalValue = "";
    internal string OriginalText = "";
    internal string OriginalSelectedValue = "";
    internal bool OriginalIsSelected;
    internal string OriginalCurrentSelectedValue = "";
    internal bool OriginalIsCurrentSelected;
}
