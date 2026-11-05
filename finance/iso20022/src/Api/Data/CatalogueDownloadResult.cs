using System.Collections.Generic;
using System.IO;

namespace iso20022;

/// <summary>Contains the response, provenance, and exception-less outcome of a catalogue download.</summary>
public class CatalogueDownloadResult
{
    /// <summary>Whether the download completed and passed all configured checks.</summary>
    public bool Success;
    /// <summary>Whether the server reported that the conditionally requested artifact was unchanged.</summary>
    public bool NotModified;
    /// <summary>The downloaded content stream, owned by the caller when present.</summary>
    public Stream? Content;
    /// <summary>The manifest captured for downloaded content.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
    /// <summary>The final URI after redirects.</summary>
    public string FinalUri = string.Empty;
    /// <summary>The final HTTP status code.</summary>
    public int StatusCode;
    /// <summary>The response entity tag when present.</summary>
    public string EntityTag = string.Empty;
    /// <summary>The normalized response last-modified time when present.</summary>
    public IsoDateTimeValue? LastModified;
    /// <summary>The ordered redirect targets followed before the final response.</summary>
    public List<string> RedirectUris = new List<string>();
    /// <summary>The number of response-content bytes received.</summary>
    public long ByteCount;
    /// <summary>A stable error code when the download did not succeed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when the download did not succeed.</summary>
    public string ErrorMessage = string.Empty;
}
