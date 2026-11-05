using System;

namespace pdf;

/// <summary>
/// Associates a short paper affiliation identifier with its display name.
/// </summary>
public class PdfPaperAffiliation
{
    /// <summary>
    /// Stores the short identifier referenced by author lines.
    /// </summary>
    public string Identifier = string.Empty;

    /// <summary>
    /// Stores the affiliation display name.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Initializes a paper affiliation.
    /// </summary>
    /// <param name="identifier">The short identifier referenced by author lines.</param>
    /// <param name="name">The affiliation display name.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfPaperAffiliation(string identifier, string name)
    {
        if (identifier == null)
        {
            throw new ArgumentNullException("identifier");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (identifier.Length == 0)
        {
            throw new ArgumentException("Affiliation identifier cannot be empty.", "identifier");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Affiliation name cannot be empty.", "name");
        }

        Identifier = identifier;
        Name = name;
    }
}
