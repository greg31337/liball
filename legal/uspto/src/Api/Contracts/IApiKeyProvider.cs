namespace uspto;

/// <summary>
/// Supplies service-specific API credentials at request time.
/// </summary>
public interface IApiKeyProvider
{
    /// <summary>Gets the credential currently available for a service.</summary>
    /// <param name="request">The credential request.</param>
    /// <returns>The selected credential and its non-secret source metadata.</returns>
    ApiCredentialResult GetApiKey(ApiCredentialRequest request);
}
