namespace ttf;

/// <summary>Reports how one user coordinate was clamped and normalized.</summary>
public class FontCoordinateMapping : IFontCoordinateMapping
{
    int axisTag;
    bool explicitlySpecified;
    double requestedValue;
    double minimumValue;
    double defaultValue;
    double maximumValue;
    double userValue;
    double unmappedNormalizedValue;
    double normalizedValue;

    internal FontCoordinateMapping(
        int axisTag,
        bool explicitlySpecified,
        double requestedValue,
        double minimumValue,
        double defaultValue,
        double maximumValue,
        double userValue,
        double unmappedNormalizedValue,
        double normalizedValue)
    {
        this.axisTag = axisTag;
        this.explicitlySpecified = explicitlySpecified;
        this.requestedValue = requestedValue;
        this.minimumValue = minimumValue;
        this.defaultValue = defaultValue;
        this.maximumValue = maximumValue;
        this.userValue = userValue;
        this.unmappedNormalizedValue = unmappedNormalizedValue;
        this.normalizedValue = normalizedValue;
    }

    /// <summary>Gets the big-endian variation-axis tag.</summary>
    /// <returns>The axis tag.</returns>
    public int GetAxisTag() { return axisTag; }

    /// <summary>Reports whether the caller explicitly supplied this coordinate.</summary>
    /// <returns>True for an explicit coordinate; otherwise false.</returns>
    public bool WasExplicitlySpecified() { return explicitlySpecified; }

    /// <summary>Gets the requested value, or the axis default when omitted.</summary>
    /// <returns>The finite user-space value before clamping.</returns>
    public double GetRequestedValue() { return requestedValue; }

    /// <summary>Gets the axis minimum value.</summary>
    /// <returns>The minimum user-space coordinate.</returns>
    public double GetMinimumValue() { return minimumValue; }

    /// <summary>Gets the axis default value.</summary>
    /// <returns>The default user-space coordinate.</returns>
    public double GetDefaultValue() { return defaultValue; }

    /// <summary>Gets the axis maximum value.</summary>
    /// <returns>The maximum user-space coordinate.</returns>
    public double GetMaximumValue() { return maximumValue; }

    /// <summary>Reports whether the requested value was clamped.</summary>
    /// <returns>True when clamping changed the requested value; otherwise false.</returns>
    public bool WasClamped()
    {
        return requestedValue < minimumValue || requestedValue > maximumValue;
    }

    /// <summary>Gets the canonical clamped user-space value.</summary>
    /// <returns>The resolved 16.16 coordinate.</returns>
    public double GetUserValue() { return userValue; }

    /// <summary>Gets the normalized coordinate before <c>avar</c> mapping.</summary>
    /// <returns>The pre-<c>avar</c> F2Dot14 coordinate.</returns>
    public double GetUnmappedNormalizedValue() { return unmappedNormalizedValue; }

    /// <summary>Gets the final normalized coordinate after <c>avar</c> mapping.</summary>
    /// <returns>The final F2Dot14 coordinate.</returns>
    public double GetNormalizedValue() { return normalizedValue; }

    /// <summary>Reports whether <c>avar</c> changed the normalized coordinate.</summary>
    /// <returns>True when the mapped value differs; otherwise false.</returns>
    public bool WasAvarMapped()
    {
        return unmappedNormalizedValue != normalizedValue;
    }
}
