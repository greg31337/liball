namespace uri;

/// <summary>
/// Describes explicit keep, set, or clear operations for URI reference components.
/// </summary>
public class UriReferenceChanges
{
    /// <summary>Whether to change the scheme; false keeps the source scheme.</summary>
    public bool ChangeScheme;

    /// <summary>Whether the changed scheme delimiter is present; false clears it.</summary>
    public bool HasScheme;

    /// <summary>The scheme text used when changing to a present scheme.</summary>
    public string Scheme;

    /// <summary>Whether to change the authority; false keeps the source authority.</summary>
    public bool ChangeAuthority;

    /// <summary>Whether the changed authority delimiter is present; false clears it.</summary>
    public bool HasAuthority;

    /// <summary>The authority used when changing to a present authority.</summary>
    public UriAuthority Authority;

    /// <summary>Whether to replace the path; false keeps the source path.</summary>
    public bool ChangePath;

    /// <summary>The replacement path, including any leading slash.</summary>
    public string Path;

    /// <summary>Whether to change the query; false keeps the source query.</summary>
    public bool ChangeQuery;

    /// <summary>Whether the changed query delimiter is present; false clears it.</summary>
    public bool HasQuery;

    /// <summary>The query text used when changing to a present query.</summary>
    public string Query;

    /// <summary>Whether to change the fragment; false keeps the source fragment.</summary>
    public bool ChangeFragment;

    /// <summary>Whether the changed fragment delimiter is present; false clears it.</summary>
    public bool HasFragment;

    /// <summary>The fragment text used when changing to a present fragment.</summary>
    public string Fragment;

    /// <summary>Initializes a change set that keeps every source component.</summary>
    public UriReferenceChanges()
    {
        ChangeScheme = false;
        HasScheme = false;
        Scheme = "";
        ChangeAuthority = false;
        HasAuthority = false;
        Authority = new UriAuthority();
        ChangePath = false;
        Path = "";
        ChangeQuery = false;
        HasQuery = false;
        Query = "";
        ChangeFragment = false;
        HasFragment = false;
        Fragment = "";
    }
}
