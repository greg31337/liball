namespace uri;

/// <summary>
/// Stores one variable specification from a URI Template expression.
/// </summary>
public class UriTemplateVariable
{
    /// <summary>The case-sensitive variable name with percent triplets preserved.</summary>
    public string Name;

    /// <summary>Whether the variable has a prefix modifier.</summary>
    public bool HasPrefix;

    /// <summary>The prefix length in Unicode scalar values when present.</summary>
    public int PrefixLength;

    /// <summary>Whether the variable has an explode modifier.</summary>
    public bool Explode;

    /// <summary>The range occupied by the variable specification in the original template.</summary>
    public TextRange Range;

    /// <summary>Initializes an unmodified empty variable specification.</summary>
    public UriTemplateVariable()
    {
        Name = "";
        HasPrefix = false;
        PrefixLength = 0;
        Explode = false;
        Range = new TextRange(0, 0);
    }
}
