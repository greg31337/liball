namespace sdp;

/// <summary>Compares SDP descriptions and returns path-addressed differences.</summary>
public interface ISdpComparer
{
    /// <summary>Compares two SDP descriptions using the requested comparison mode.</summary>
    /// <param name="originalDescription">The original description.</param>
    /// <param name="comparedDescription">The description compared with the original.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The deterministic comparison result.</returns>
    SdpComparisonResult CompareDescriptions(SdpSessionDescription originalDescription, SdpSessionDescription comparedDescription, SdpComparisonOptions options);
}
