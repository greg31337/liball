namespace fake;

/// <summary>Represents one reviewed filename-extension and registered media-type mapping.</summary>
/// <remarks>The mapping is conventional metadata, not content inspection or a security classification.</remarks>
public class FileFormatRecord
{
    /// <summary>The lowercase filename extension without a leading dot.</summary>
    public string Extension = string.Empty;

    /// <summary>The registered IANA media type associated with the extension.</summary>
    public string MimeType = string.Empty;

    /// <summary>The broad project-authored file category.</summary>
    public string Type = string.Empty;

    /// <summary>The source-version text of the bundled IANA media-type registry.</summary>
    public string RegistryVersion = string.Empty;

    /// <summary>Whether <c>MimeType</c> occurs in the bundled registry, always <c>true</c>.</summary>
    public bool MimeTypeIsRegistered;

    internal FileFormatRecord()
    {
    }
}
