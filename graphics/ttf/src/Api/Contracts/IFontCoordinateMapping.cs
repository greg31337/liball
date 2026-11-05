namespace ttf;

/// <summary>Defines one reported user-to-normalized variation-axis mapping.</summary>
public interface IFontCoordinateMapping
{
    /// <summary>Gets the big-endian variation-axis tag.</summary>
    /// <returns>The axis tag.</returns>
    int GetAxisTag();

    /// <summary>Reports whether the caller explicitly supplied this coordinate.</summary>
    /// <returns>True for an explicit coordinate; otherwise false.</returns>
    bool WasExplicitlySpecified();

    /// <summary>Gets the requested value, or the axis default when omitted.</summary>
    /// <returns>The finite user-space value before clamping.</returns>
    double GetRequestedValue();

    /// <summary>Gets the axis minimum value.</summary>
    /// <returns>The minimum user-space coordinate.</returns>
    double GetMinimumValue();

    /// <summary>Gets the axis default value.</summary>
    /// <returns>The default user-space coordinate.</returns>
    double GetDefaultValue();

    /// <summary>Gets the axis maximum value.</summary>
    /// <returns>The maximum user-space coordinate.</returns>
    double GetMaximumValue();

    /// <summary>Reports whether the requested value was clamped.</summary>
    /// <returns>True when clamping changed the requested value; otherwise false.</returns>
    bool WasClamped();

    /// <summary>Gets the canonical clamped user-space value.</summary>
    /// <returns>The resolved 16.16 coordinate.</returns>
    double GetUserValue();

    /// <summary>Gets the normalized coordinate before <c>avar</c> mapping.</summary>
    /// <returns>The pre-<c>avar</c> F2Dot14 coordinate.</returns>
    double GetUnmappedNormalizedValue();

    /// <summary>Gets the final normalized coordinate after <c>avar</c> mapping.</summary>
    /// <returns>The final F2Dot14 coordinate.</returns>
    double GetNormalizedValue();

    /// <summary>Reports whether <c>avar</c> changed the normalized coordinate.</summary>
    /// <returns>True when the mapped value differs; otherwise false.</returns>
    bool WasAvarMapped();
}
