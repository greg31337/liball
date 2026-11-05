namespace pdf;

/// <summary>
/// Defines a mutually exclusive AcroForm radio-button group.
/// </summary>
public interface IPdfRadioButtonGroup
{
    /// <summary>Returns the registered group name.</summary>
    /// <returns>The group name.</returns>
    string GetGroupName();

    /// <summary>Adds a radio option widget.</summary>
    /// <param name="exportValue">The unique value exported when selected.</param>
    /// <param name="page">The page receiving the option widget.</param>
    /// <param name="x">The horizontal widget coordinate.</param>
    /// <param name="y">The vertical widget coordinate.</param>
    /// <param name="width">The positive widget width.</param>
    /// <param name="height">The positive widget height.</param>
    /// <returns>The option widget field.</returns>
    PdfAcroFormField AddOption(string exportValue, PdfPage page, double x, double y, double width, double height);

    /// <summary>Adds a radio option widget from a rectangle.</summary>
    /// <param name="exportValue">The unique value exported when selected.</param>
    /// <param name="page">The page receiving the option widget.</param>
    /// <param name="rectangle">The positive widget rectangle.</param>
    /// <returns>The option widget field.</returns>
    PdfAcroFormField AddOptionInRectangle(string exportValue, PdfPage page, PdfRectangle rectangle);

    /// <summary>Selects one registered option and clears every other option.</summary>
    /// <param name="exportValue">The option value to select.</param>
    void SelectOption(string exportValue);

    /// <summary>Clears the current selection.</summary>
    void ClearSelection();

    /// <summary>Returns the selected export value.</summary>
    /// <returns>The selected value, or an empty value when no option is selected.</returns>
    string GetSelectedValue();

    /// <summary>Returns the number of option widgets.</summary>
    /// <returns>The option count.</returns>
    int GetOptionCount();

    /// <summary>Returns a registered option widget.</summary>
    /// <param name="index">The zero-based option position.</param>
    /// <returns>The requested option widget.</returns>
    PdfAcroFormField GetOption(int index);

    /// <summary>Sets the accessible name written on the parent field.</summary>
    /// <param name="name">The non-empty accessible group name.</param>
    void SetAccessibilityName(string name);

    /// <summary>Returns the accessible group name.</summary>
    /// <returns>The configured accessible name.</returns>
    string GetAccessibilityName();
}
