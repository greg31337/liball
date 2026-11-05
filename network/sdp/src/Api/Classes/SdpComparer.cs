using System;

namespace sdp;

/// <summary>Provides structural and semantic SDP comparison.</summary>
public class SdpComparer : ISdpComparer
{
    /// <summary>Initializes an SDP comparer.</summary>
    public SdpComparer()
    {
    }

    /// <summary>Compares two SDP descriptions using the requested comparison mode.</summary>
    /// <param name="originalDescription">The original description.</param>
    /// <param name="comparedDescription">The description compared with the original.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The deterministic comparison result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when semantic comparison is requested before a semantic comparison policy is available.</exception>
    public SdpComparisonResult CompareDescriptions(SdpSessionDescription originalDescription, SdpSessionDescription comparedDescription, SdpComparisonOptions options)
    {
        if (originalDescription == null)
        {
            throw new ArgumentNullException("originalDescription");
        }
        if (comparedDescription == null)
        {
            throw new ArgumentNullException("comparedDescription");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        return SdpComparisonCore.CompareDescriptions(originalDescription, comparedDescription, options);
    }
}
