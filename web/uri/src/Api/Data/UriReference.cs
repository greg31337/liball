namespace uri;

/// <summary>
/// Stores the lossless components and classification of a URI reference.
/// </summary>
public class UriReference
{
    /// <summary>The parsed reference classification.</summary>
    public UriReferenceKind Kind;

    /// <summary>Whether a scheme and its delimiter are present.</summary>
    public bool HasScheme;

    /// <summary>The scheme text without its trailing delimiter.</summary>
    public string Scheme;

    /// <summary>Whether an authority and its leading delimiter are present.</summary>
    public bool HasAuthority;

    /// <summary>The authority subcomponents.</summary>
    public UriAuthority Authority;

    /// <summary>The path text, which may be empty.</summary>
    public string Path;

    /// <summary>Whether a query delimiter is present.</summary>
    public bool HasQuery;

    /// <summary>The query text without its leading delimiter.</summary>
    public string Query;

    /// <summary>Whether a fragment delimiter is present.</summary>
    public bool HasFragment;

    /// <summary>The fragment text without its leading delimiter.</summary>
    public string Fragment;

    /// <summary>The complete original reference text.</summary>
    public string OriginalText;

    /// <summary>Initializes an empty reference with every optional component absent.</summary>
    public UriReference()
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
