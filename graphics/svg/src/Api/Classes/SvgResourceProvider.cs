namespace svg;

/// <summary>Adapts a single synchronous callback to <see cref="ISvgResourceProvider"/>.</summary>
public class SvgResourceProvider : ISvgResourceProvider
{
    System.Func<SvgResourceRequest, SvgResourceResponse> callback;

    /// <summary>Creates a resource provider backed by one callback.</summary>
    /// <param name="callback">The non-null callback invoked for approved requests.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>callback</c> is <c>null</c>.</exception>
    public SvgResourceProvider(System.Func<SvgResourceRequest, SvgResourceResponse> callback)
    {
        if (callback == null) throw new System.ArgumentNullException("callback");
        this.callback = callback;
    }

    /// <summary>Gets one resource response from the configured callback.</summary>
    /// <param name="request">The normalized bounded resource request.</param>
    /// <returns>The resource response, or <c>null</c> when the callback declines the request.</returns>
    public SvgResourceResponse? GetResource(SvgResourceRequest request)
    {
        return callback(request);
    }
}
