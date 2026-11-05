using System;

namespace pdf;

/// <summary>
/// Represents a citation used by the PDF generation API.
/// </summary>
public class PdfCitation : IPdfCitation
{
    /// <summary>
    /// Stores the key used by this object.
    /// </summary>
    internal string Key = string.Empty;
    /// <summary>
    /// Stores the text used by this object.
    /// </summary>
    internal string Text = string.Empty;

    /// <summary>
    /// Initializes a new citation.
    /// </summary>
    /// <param name="key">The key to use for this operation.</param>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfCitation(string key, string text)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Key = key;
        Text = text;
    }

    /// <summary>
    /// Formats the result for use in generated content.
    /// </summary>
    /// <returns>The resulting text.</returns>
    public string Format()
    {
        return "[" + Key + "] " + Text;
    }
}
