namespace uri;

/// <summary>
/// Declares optional scheme-specific URI normalization behavior.
/// </summary>
public class UriSchemeProfile
{
    /// <summary>The scheme to which this profile applies, compared without ASCII case.</summary>
    public string Scheme;

    /// <summary>Whether an authority with an empty path is normalized to <c>/</c>.</summary>
    public bool NormalizeEmptyAuthorityPath;

    /// <summary>Whether a present empty port delimiter is removed.</summary>
    public bool RemoveEmptyPort;

    /// <summary>Whether the profile declares a default port to remove.</summary>
    public bool HasDefaultPort;

    /// <summary>The declared default port when <see cref="HasDefaultPort"/> is true.</summary>
    public int DefaultPort;

    /// <summary>Whether a present empty query delimiter is removed.</summary>
    public bool RemoveEmptyQuery;

    /// <summary>Whether a present empty fragment delimiter is removed.</summary>
    public bool RemoveEmptyFragment;

    /// <summary>Initializes a profile with no scheme-specific transformations enabled.</summary>
    public UriSchemeProfile()
    {
        Scheme = "";
        NormalizeEmptyAuthorityPath = false;
        RemoveEmptyPort = false;
        HasDefaultPort = false;
        DefaultPort = 0;
        RemoveEmptyQuery = false;
        RemoveEmptyFragment = false;
    }
}
