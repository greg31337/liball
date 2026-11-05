using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Configures indentation, newline text, and whitespace normalization for template formatting.
/// </summary>
public class TemplateFormatterOptions
{
    /// <summary>
    /// Specifies the indent text for this formatter options.
    /// </summary>
    public string IndentText;
    /// <summary>
    /// Specifies the new line for this formatter options.
    /// </summary>
    public string NewLine;
    /// <summary>
    /// Specifies the whitespace mode for this formatter options.
    /// </summary>
    public TemplateWhitespaceMode WhitespaceMode;

    /// <summary>
    /// Initializes a new template formatter options and validates any supplied values.
    /// </summary>
    public TemplateFormatterOptions()
    {
        IndentText = "  ";
        NewLine = "\n";
        WhitespaceMode = TemplateWhitespaceMode.NormalizeLineFeeds;
    }

    /// <summary>
    /// Creates an independent copy so later field changes do not affect the original instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    internal TemplateFormatterOptions Clone()
    {
        TemplateFormatterOptions clone = new TemplateFormatterOptions();
        clone.IndentText = IndentText;
        clone.NewLine = NewLine;
        clone.WhitespaceMode = WhitespaceMode;
        return clone;
    }

    /// <summary>
    /// Checks the current settings for missing or contradictory values and throws when they cannot be used safely.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void Validate()
    {
        if (IndentText == null)
        {
            throw new ArgumentNullException("IndentText");
        }

        if (NewLine == null)
        {
            throw new ArgumentNullException("NewLine");
        }

        if (NewLine == "")
        {
            throw new ArgumentException("NewLine is required.", "NewLine");
        }
    }
}

