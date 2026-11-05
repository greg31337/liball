namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgResourceResolver"/>.</summary>
public interface ISvgResourceResolver
{
    /// <summary>Acquires one resource without implicit network or filesystem access.</summary>
    /// <param name="request">The caller-visible resource request.</param>
    /// <param name="options">The callback, cancellation, ownership, and byte limits.</param>
    /// <returns>A bounded byte snapshot with declared and actual media metadata.</returns>
    SvgResourceAcquisitionResult Resolve(SvgResourceRequest request, SvgResourceAcquisitionOptions options);
}
