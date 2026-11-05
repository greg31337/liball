namespace docx;

/// <summary>
/// Describes a named placeholder discovered in a document template.
/// </summary>
public class DocxTemplatePlaceholder
{
    /// <summary>The placeholder key without delimiters.</summary>
    public string Key;
    /// <summary>The complete placeholder token.</summary>
    public string Token;
    /// <summary>The number of occurrences across supported document stories.</summary>
    public int Count;

    /// <summary>Creates a template placeholder descriptor.</summary>
    /// <param name="key">The placeholder key.</param>
    /// <param name="token">The complete placeholder token.</param>
    /// <param name="count">The occurrence count.</param>
    public DocxTemplatePlaceholder(string key, string token, int count)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        this.Key = key;
        this.Token = token;
        this.Count = count;
    }
}
