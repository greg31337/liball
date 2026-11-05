using System;

namespace email;
/// <summary>
/// Represents an external body metadata.
/// </summary>
public class EmailExternalBodyMetadata : IEmailExternalBodyMetadataContract
{
    EmailParameterList parameters;
    string? accessType;
    bool validAccessType;
    EmailDateTime? expiration;
    long size;
    bool sizePresent;
    string? permission;
    string? name;
    string? site;
    string? directory;
    string? mode;
    string? server;
    string? subject;
    string? url;
    EmailHeaders referencedHeaders;
    bool referencedHeadersComplete;
    EmailRange referencedHeaderRange;
    EmailRange? phantomBodyRange;
    /// <summary>
    /// Creates a new external body metadata.
    /// </summary>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "accessType">The access type used by the operation.</param>
    /// <param name = "validAccessType">Whether to enable the valid access type behavior.</param>
    /// <param name = "expiration">The expiration used by the operation.</param>
    /// <param name = "size">The size used by the operation.</param>
    /// <param name = "sizePresent">Whether to enable the size present behavior.</param>
    /// <param name = "permission">The permission used by the operation.</param>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "site">The site used by the operation.</param>
    /// <param name = "directory">The directory used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "server">The server used by the operation.</param>
    /// <param name = "subject">The subject used by the operation.</param>
    /// <param name = "url">The url used by the operation.</param>
    /// <param name = "referencedHeaders">The referenced headers used by the operation.</param>
    /// <param name = "referencedHeadersComplete">Whether to enable the referenced headers complete behavior.</param>
    /// <param name = "referencedHeaderRange">The referenced header range used by the operation.</param>
    /// <param name = "phantomBodyRange">The phantom body range used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailExternalBodyMetadata(EmailParameterList parameters, string? accessType, bool validAccessType, EmailDateTime? expiration, long size, bool sizePresent, string? permission, string? name, string? site, string? directory, string? mode, string? server, string? subject, string? url, EmailHeaders referencedHeaders, bool referencedHeadersComplete, EmailRange referencedHeaderRange, EmailRange? phantomBodyRange)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        if (accessType != null && accessType.Length == 0)
        {
            throw new ArgumentException("An external-body access type cannot be empty.", "accessType");
        }

        if (sizePresent && size < 0)
        {
            throw new ArgumentOutOfRangeException("size");
        }

        if (referencedHeaders == null)
        {
            throw new ArgumentNullException("referencedHeaders");
        }

        if (referencedHeaderRange == null)
        {
            throw new ArgumentNullException("referencedHeaderRange");
        }

        if (!referencedHeadersComplete && phantomBodyRange != null)
        {
            throw new ArgumentException("An incomplete external-body header block cannot have a phantom-body range.", "phantomBodyRange");
        }

        this.parameters = parameters;
        this.accessType = accessType;
        this.validAccessType = validAccessType;
        this.expiration = expiration;
        this.size = size;
        this.sizePresent = sizePresent;
        this.permission = permission;
        this.name = name;
        this.site = site;
        this.directory = directory;
        this.mode = mode;
        this.server = server;
        this.subject = subject;
        this.url = url;
        this.referencedHeaders = referencedHeaders.Clone();
        this.referencedHeadersComplete = referencedHeadersComplete;
        this.referencedHeaderRange = referencedHeaderRange;
        this.phantomBodyRange = phantomBodyRange;
    }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <returns>The current parameters.</returns>
    public EmailParameterList GetParameters()
    {
        return parameters;
    }

    /// <summary>
    /// Determines whether the access type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the access type is available; otherwise, <c>false</c>.</returns>
    public bool HasAccessType()
    {
        return accessType != null;
    }

    /// <summary>
    /// Gets the access type.
    /// </summary>
    /// <returns>The current access type.</returns>
    public string? GetAccessType()
    {
        return accessType;
    }

    /// <summary>
    /// Gets the normalized access type.
    /// </summary>
    /// <returns>The current normalized access type.</returns>
    public string? GetNormalizedAccessType()
    {
        return accessType == null ? null : accessType.ToLowerInvariant();
    }

    /// <summary>
    /// Determines whether the valid access type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the valid access type is available; otherwise, <c>false</c>.</returns>
    public bool HasValidAccessType()
    {
        return validAccessType;
    }

    /// <summary>
    /// Determines whether the expiration is present or available.
    /// </summary>
    /// <returns><c>true</c> when the expiration is available; otherwise, <c>false</c>.</returns>
    public bool HasExpiration()
    {
        return expiration != null;
    }

    /// <summary>
    /// Gets the expiration.
    /// </summary>
    /// <returns>The current expiration.</returns>
    public EmailDateTime? GetExpiration()
    {
        return expiration;
    }

    /// <summary>
    /// Determines whether the size is present or available.
    /// </summary>
    /// <returns><c>true</c> when the size is available; otherwise, <c>false</c>.</returns>
    public bool HasSize()
    {
        return sizePresent;
    }

    /// <summary>
    /// Gets the size.
    /// </summary>
    /// <returns>The current size.</returns>
    public long GetSize()
    {
        return size;
    }

    /// <summary>
    /// Determines whether the permission is present or available.
    /// </summary>
    /// <returns><c>true</c> when the permission is available; otherwise, <c>false</c>.</returns>
    public bool HasPermission()
    {
        return permission != null;
    }

    /// <summary>
    /// Gets the permission.
    /// </summary>
    /// <returns>The current permission.</returns>
    public string? GetPermission()
    {
        return permission;
    }

    /// <summary>
    /// Gets the effective permission.
    /// </summary>
    /// <returns>The current effective permission.</returns>
    public string GetEffectivePermission()
    {
        return permission != null && string.Equals(permission, "read-write", StringComparison.OrdinalIgnoreCase) ? "read-write" : "read";
    }

    /// <summary>
    /// Determines whether the known permission is present or available.
    /// </summary>
    /// <returns><c>true</c> when the known permission is available; otherwise, <c>false</c>.</returns>
    public bool HasKnownPermission()
    {
        return permission == null || string.Equals(permission, "read", StringComparison.OrdinalIgnoreCase) || string.Equals(permission, "read-write", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the name is available; otherwise, <c>false</c>.</returns>
    public bool HasName()
    {
        return name != null;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string? GetName()
    {
        return name;
    }

    /// <summary>
    /// Determines whether the site is present or available.
    /// </summary>
    /// <returns><c>true</c> when the site is available; otherwise, <c>false</c>.</returns>
    public bool HasSite()
    {
        return site != null;
    }

    /// <summary>
    /// Gets the site.
    /// </summary>
    /// <returns>The current site.</returns>
    public string? GetSite()
    {
        return site;
    }

    /// <summary>
    /// Determines whether the directory is present or available.
    /// </summary>
    /// <returns><c>true</c> when the directory is available; otherwise, <c>false</c>.</returns>
    public bool HasDirectory()
    {
        return directory != null;
    }

    /// <summary>
    /// Gets the directory.
    /// </summary>
    /// <returns>The current directory.</returns>
    public string? GetDirectory()
    {
        return directory;
    }

    /// <summary>
    /// Determines whether the mode is present or available.
    /// </summary>
    /// <returns><c>true</c> when the mode is available; otherwise, <c>false</c>.</returns>
    public bool HasMode()
    {
        return mode != null;
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public string? GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Determines whether the server is present or available.
    /// </summary>
    /// <returns><c>true</c> when the server is available; otherwise, <c>false</c>.</returns>
    public bool HasServer()
    {
        return server != null;
    }

    /// <summary>
    /// Gets the server.
    /// </summary>
    /// <returns>The current server.</returns>
    public string? GetServer()
    {
        return server;
    }

    /// <summary>
    /// Determines whether the subject is present or available.
    /// </summary>
    /// <returns><c>true</c> when the subject is available; otherwise, <c>false</c>.</returns>
    public bool HasSubject()
    {
        return subject != null;
    }

    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <returns>The current subject.</returns>
    public string? GetSubject()
    {
        return subject;
    }

    /// <summary>
    /// Determines whether the url is present or available.
    /// </summary>
    /// <returns><c>true</c> when the url is available; otherwise, <c>false</c>.</returns>
    public bool HasUrl()
    {
        return url != null;
    }

    /// <summary>
    /// Gets the url.
    /// </summary>
    /// <returns>The current url.</returns>
    public string? GetUrl()
    {
        return url;
    }

    /// <summary>
    /// Gets the referenced headers.
    /// </summary>
    /// <returns>The current referenced headers.</returns>
    public EmailHeaders GetReferencedHeaders()
    {
        return referencedHeaders.Clone();
    }

    /// <summary>
    /// Determines whether the compared values are referenced headers complete.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool AreReferencedHeadersComplete()
    {
        return referencedHeadersComplete;
    }

    /// <summary>
    /// Gets the referenced header range.
    /// </summary>
    /// <returns>The current referenced header range.</returns>
    public EmailRange GetReferencedHeaderRange()
    {
        return referencedHeaderRange;
    }

    /// <summary>
    /// Determines whether the phantom body range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the phantom body range is available; otherwise, <c>false</c>.</returns>
    public bool HasPhantomBodyRange()
    {
        return phantomBodyRange != null;
    }

    /// <summary>
    /// Gets the phantom body range.
    /// </summary>
    /// <returns>The current phantom body range.</returns>
    public EmailRange? GetPhantomBodyRange()
    {
        return phantomBodyRange;
    }
}
