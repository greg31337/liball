namespace uri;

/// <summary>
/// Stores the lossless components and classification of an IRI reference.
/// </summary>
public class IriReference
{
    /// <summary>The parsed reference classification.</summary>
    public UriReferenceKind Kind;

    /// <summary>Whether a scheme and its delimiter are present.</summary>
    public bool HasScheme;

    /// <summary>The scheme text without its trailing delimiter.</summary>
    public string Scheme;

    /// <summary>Whether an authority and its leading delimiter are present.</summary>
    public bool HasAuthority;

    /// <summary>The authority subcomponents, including an internationalized registered name.</summary>
    public UriAuthority Authority;

    /// <summary>The internationalized path text, which may be empty.</summary>
    public string Path;

    /// <summary>Whether a query delimiter is present.</summary>
    public bool HasQuery;

    /// <summary>The internationalized query text without its leading delimiter.</summary>
    public string Query;

    /// <summary>Whether a fragment delimiter is present.</summary>
    public bool HasFragment;

    /// <summary>The internationalized fragment text without its leading delimiter.</summary>
    public string Fragment;

    /// <summary>The complete original IRI-reference text.</summary>
    public string OriginalText;

    /// <summary>Initializes an empty IRI reference with every optional component absent.</summary>
    public IriReference()
    {
        Kind = UriReferenceKind.EmptyReference;
        HasScheme = false;
        Scheme = "";
        HasAuthority = false;
        Authority = new UriAuthority();
        Path = "";
        HasQuery = false;
        Query = "";
        HasFragment = false;
        Fragment = "";
        OriginalText = "";
    }
}
