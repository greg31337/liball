namespace svg;

/// <summary>Contains a normalized IRI, its escaped URI form, fragment, file classification, and cache key.</summary>
public class SvgUriResult
{
    /// <summary>Whether processing completed successfully.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgUriError Error;
    /// <summary>The normalized Unicode IRI, including its fragment.</summary>
    public string NormalizedIri;
    /// <summary>The normalized ASCII URI with non-ASCII characters escaped as UTF-8.</summary>
    public string NormalizedUri;
    /// <summary>The normalized IRI without its fragment.</summary>
    public string ResourceIri;
    /// <summary>The fragment text without the number sign.</summary>
    public string Fragment;
    /// <summary>Whether a fragment delimiter was present, including an empty fragment.</summary>
    public bool HasFragment;
    /// <summary>Whether the normalized reference has a scheme.</summary>
    public bool IsAbsolute;
    /// <summary>Whether the normalized reference uses the file scheme.</summary>
    public bool IsFile;
    /// <summary>Whether the file URI has an empty or localhost authority.</summary>
    public bool IsLocalFile;
    /// <summary>The normalized ASCII resource URI without a fragment, suitable for cache identity.</summary>
    public string CacheKey;

    internal SvgUriResult()
    {
        Success = false;
        Error = SvgUriError.InvalidArgument;
        NormalizedIri = "";
        NormalizedUri = "";
        ResourceIri = "";
        Fragment = "";
        HasFragment = false;
        IsAbsolute = false;
        IsFile = false;
        IsLocalFile = false;
        CacheKey = "";
    }
}
