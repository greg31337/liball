using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Describes a host-neutral streaming upload to a JMAP account.
/// </summary>
public class BlobUploadRequest
{
    /// <summary>The account to which the binary data is uploaded.</summary>
    public string? AccountId;

    /// <summary>The media type sent in the upload Content-Type header.</summary>
    public string? MediaType;

    /// <summary>The stream containing the raw binary data.</summary>
    public Stream? Content;

    /// <summary>The number of octets in the content when known.</summary>
    public long ContentLength;

    /// <summary>Whether the content length is known.</summary>
    public bool HasContentLength;
}

/// <summary>
/// Represents the JSON response returned after a successful binary upload.
/// </summary>
public class BlobUploadResponse
{
    /// <summary>The account to which the data was uploaded.</summary>
    public string? AccountId;

    /// <summary>The immutable identifier assigned to the uploaded bytes.</summary>
    public string? BlobId;

    /// <summary>The media type supplied with the upload.</summary>
    public string? Type;

    /// <summary>The size of the uploaded data in octets.</summary>
    public long Size;

    /// <summary>Stores unrecognized upload response properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Describes a host-neutral request to download immutable binary data.
/// </summary>
public class BlobDownloadRequest
{
    /// <summary>The account containing an object that references the blob.</summary>
    public string? AccountId;

    /// <summary>The identifier of the binary data to download.</summary>
    public string? BlobId;

    /// <summary>The requested response media type.</summary>
    public string? Type;

    /// <summary>The requested safe response filename.</summary>
    public string? Name;
}

/// <summary>
/// Represents a host-neutral streaming binary download response.
/// </summary>
public class BlobDownloadResponse
{
    /// <summary>The stream containing the raw downloaded bytes.</summary>
    public Stream? Content;

    /// <summary>The media type returned with the content.</summary>
    public string? MediaType;

    /// <summary>The filename returned with the content, or null when none was supplied.</summary>
    public string? FileName;

    /// <summary>The number of octets in the content when known.</summary>
    public long ContentLength;

    /// <summary>Whether the content length is known.</summary>
    public bool HasContentLength;

    /// <summary>The response Cache-Control value, or null when none was supplied.</summary>
    public string? CacheControl;
}

/// <summary>
/// Contains the arguments for copying blobs between JMAP accounts.
/// </summary>
public class BlobCopyRequest
{
    /// <summary>The account from which blobs are copied.</summary>
    public string? FromAccountId;

    /// <summary>The account to which blobs are copied.</summary>
    public string? AccountId;

    /// <summary>The identifiers of blobs to copy.</summary>
    public List<string> BlobIds = new List<string>();

    /// <summary>Stores unrecognized Blob/copy arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for copying blobs between JMAP accounts.
/// </summary>
public class BlobCopyResponse
{
    /// <summary>The account from which blobs were copied.</summary>
    public string? FromAccountId;

    /// <summary>The account to which blobs were copied.</summary>
    public string? AccountId;

    /// <summary>Maps source blob identifiers to destination blob identifiers, or null when none succeeded.</summary>
    public Dictionary<string, string>? Copied;

    /// <summary>Maps source blob identifiers to errors, or null when none failed.</summary>
    public Dictionary<string, JmapSetError>? NotCopied;

    /// <summary>Stores unrecognized Blob/copy result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
