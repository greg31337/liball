namespace svg;

/// <summary>Provides synchronous caller-mediated external SVG resources.</summary>
/// <remarks>Implementations may retain credentials, caches, request logs, or other host state across loading and resolution stages. The library never calls a provider until resource policy permits the request.</remarks>
public interface ISvgResourceProvider
{
    /// <summary>Gets one resource response for an approved request.</summary>
    /// <param name="request">The normalized bounded resource request.</param>
    /// <returns>The resource response, or <c>null</c> when the provider declines the request.</returns>
    SvgResourceResponse? GetResource(SvgResourceRequest request);
}
