using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailExternalBodyMetadata"/>.
/// </summary>
public interface IEmailExternalBodyMetadataContract
{
    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <returns>The current parameters.</returns>
    EmailParameterList GetParameters();
    /// <summary>
    /// Determines whether the access type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the access type is available; otherwise, <c>false</c>.</returns>
    bool HasAccessType();
    /// <summary>
    /// Gets the access type.
    /// </summary>
    /// <returns>The current access type.</returns>
    string? GetAccessType();
    /// <summary>
    /// Gets the normalized access type.
    /// </summary>
    /// <returns>The current normalized access type.</returns>
    string? GetNormalizedAccessType();
    /// <summary>
    /// Determines whether the valid access type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the valid access type is available; otherwise, <c>false</c>.</returns>
    bool HasValidAccessType();
    /// <summary>
    /// Determines whether the expiration is present or available.
    /// </summary>
    /// <returns><c>true</c> when the expiration is available; otherwise, <c>false</c>.</returns>
    bool HasExpiration();
    /// <summary>
    /// Gets the expiration.
    /// </summary>
    /// <returns>The current expiration.</returns>
    EmailDateTime? GetExpiration();
    /// <summary>
    /// Determines whether the size is present or available.
    /// </summary>
    /// <returns><c>true</c> when the size is available; otherwise, <c>false</c>.</returns>
    bool HasSize();
    /// <summary>
    /// Gets the size.
    /// </summary>
    /// <returns>The current size.</returns>
    long GetSize();
    /// <summary>
    /// Determines whether the permission is present or available.
    /// </summary>
    /// <returns><c>true</c> when the permission is available; otherwise, <c>false</c>.</returns>
    bool HasPermission();
    /// <summary>
    /// Gets the permission.
    /// </summary>
    /// <returns>The current permission.</returns>
    string? GetPermission();
    /// <summary>
    /// Gets the effective permission.
    /// </summary>
    /// <returns>The current effective permission.</returns>
    string GetEffectivePermission();
    /// <summary>
    /// Determines whether the known permission is present or available.
    /// </summary>
    /// <returns><c>true</c> when the known permission is available; otherwise, <c>false</c>.</returns>
    bool HasKnownPermission();
    /// <summary>
    /// Determines whether the name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the name is available; otherwise, <c>false</c>.</returns>
    bool HasName();
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string? GetName();
    /// <summary>
    /// Determines whether the site is present or available.
    /// </summary>
    /// <returns><c>true</c> when the site is available; otherwise, <c>false</c>.</returns>
    bool HasSite();
    /// <summary>
    /// Gets the site.
    /// </summary>
    /// <returns>The current site.</returns>
    string? GetSite();
    /// <summary>
    /// Determines whether the directory is present or available.
    /// </summary>
    /// <returns><c>true</c> when the directory is available; otherwise, <c>false</c>.</returns>
    bool HasDirectory();
    /// <summary>
    /// Gets the directory.
    /// </summary>
    /// <returns>The current directory.</returns>
    string? GetDirectory();
    /// <summary>
    /// Determines whether the mode is present or available.
    /// </summary>
    /// <returns><c>true</c> when the mode is available; otherwise, <c>false</c>.</returns>
    bool HasMode();
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    string? GetMode();
    /// <summary>
    /// Determines whether the server is present or available.
    /// </summary>
    /// <returns><c>true</c> when the server is available; otherwise, <c>false</c>.</returns>
    bool HasServer();
    /// <summary>
    /// Gets the server.
    /// </summary>
    /// <returns>The current server.</returns>
    string? GetServer();
    /// <summary>
    /// Determines whether the subject is present or available.
    /// </summary>
    /// <returns><c>true</c> when the subject is available; otherwise, <c>false</c>.</returns>
    bool HasSubject();
    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <returns>The current subject.</returns>
    string? GetSubject();
    /// <summary>
    /// Determines whether the url is present or available.
    /// </summary>
    /// <returns><c>true</c> when the url is available; otherwise, <c>false</c>.</returns>
    bool HasUrl();
    /// <summary>
    /// Gets the url.
    /// </summary>
    /// <returns>The current url.</returns>
    string? GetUrl();
    /// <summary>
    /// Gets the referenced headers.
    /// </summary>
    /// <returns>The current referenced headers.</returns>
    EmailHeaders GetReferencedHeaders();
    /// <summary>
    /// Determines whether the compared values are referenced headers complete.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool AreReferencedHeadersComplete();
    /// <summary>
    /// Gets the referenced header range.
    /// </summary>
    /// <returns>The current referenced header range.</returns>
    EmailRange GetReferencedHeaderRange();
    /// <summary>
    /// Determines whether the phantom body range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the phantom body range is available; otherwise, <c>false</c>.</returns>
    bool HasPhantomBodyRange();
    /// <summary>
    /// Gets the phantom body range.
    /// </summary>
    /// <returns>The current phantom body range.</returns>
    EmailRange? GetPhantomBodyRange();
}
