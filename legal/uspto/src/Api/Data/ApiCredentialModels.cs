namespace uspto;

/// <summary>
/// Describes a request for a service-specific API credential.
/// </summary>
public class ApiCredentialRequest
{
    /// <summary>The service for which a credential is requested.</summary>
    public ApiService Service = ApiService.Unknown;

    /// <summary>Whether the caller permits an anonymous result when no key is available.</summary>
    public bool AllowAnonymous = false;
}

/// <summary>
/// Describes the credential selected for a request without requiring a particular credential source.
/// </summary>
public class ApiCredentialResult
{
    /// <summary>The credential request that produced this result.</summary>
    public ApiCredentialRequest Request = new ApiCredentialRequest();

    /// <summary>The API key, or <c>null</c> when anonymous access was selected.</summary>
    public string? ApiKey = null;

    /// <summary>A non-secret description of the selected credential source.</summary>
    public string? Source = null;

    /// <summary>Whether a nonblank API key is available.</summary>
    public bool IsAvailable = false;

    /// <summary>The optional time after which the credential should no longer be used.</summary>
    public global::System.DateTimeOffset ExpiresAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ExpiresAt"/> contains a value.</summary>
    public bool HasExpiresAt = false;
}
