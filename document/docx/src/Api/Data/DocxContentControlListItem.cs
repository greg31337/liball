namespace docx;

/// <summary>
/// Describes one combo-box or drop-down-list item.
/// </summary>
public class DocxContentControlListItem
{
    /// <summary>The displayed item text.</summary>
    public string DisplayText;
    /// <summary>The stored item value.</summary>
    public string Value;

    /// <summary>Creates a content-control list item.</summary>
    /// <param name="displayText">The displayed text.</param>
    /// <param name="value">The stored value.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxContentControlListItem(string displayText, string value)
    {
        if (displayText == null)
        {
            throw new ArgumentNullException("displayText");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.DisplayText = displayText;
        this.Value = value;
    }
}
