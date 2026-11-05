using System;

namespace pdf;

/// <summary>
/// Represents a bibliography entry used by the PDF generation API.
/// </summary>
public class PdfBibliographyEntry : IPdfBibliographyEntry
{
    /// <summary>
    /// Stores the key used by this object.
    /// </summary>
    internal string Key = string.Empty;
    /// <summary>
    /// Stores the authors used by this object.
    /// </summary>
    internal string Authors = string.Empty;
    /// <summary>
    /// Stores the title used by this object.
    /// </summary>
    internal string Title = string.Empty;
    /// <summary>
    /// Stores the source used by this object.
    /// </summary>
    internal string Source = string.Empty;
    /// <summary>
    /// Stores the year used by this object.
    /// </summary>
    internal string Year = string.Empty;

    /// <summary>
    /// Initializes a new bibliography entry.
    /// </summary>
    /// <param name="key">The key to use for this operation.</param>
    /// <param name="authors">The authors to use for this operation.</param>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="source">The source to use for this operation.</param>
    /// <param name="year">The year to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfBibliographyEntry(string key, string authors, string title, string source, string year)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (authors == null)
        {
            throw new ArgumentNullException("authors");
        }

        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (year == null)
        {
            throw new ArgumentNullException("year");
        }

        Key = key;
        Authors = authors;
        Title = title;
        Source = source;
        Year = year;
    }

    /// <summary>
    /// Formats the result for use in generated content.
    /// </summary>
    /// <returns>The resulting text.</returns>
    public string Format()
    {
        string authors = Authors;
        if (!authors.EndsWith("."))
        {
            authors += ".";
        }

        return "[" + Key + "] " + authors + " " + Title + ". " + Source + ", " + Year + ".";
    }
}
