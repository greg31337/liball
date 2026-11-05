using System.Collections.Generic;

namespace image;

/// <summary>Contains the outcome of comparing two metadata documents.</summary>
public class ImageMetadataDiffResult
{
    /// <summary>Indicates whether the compared metadata was equivalent.</summary>
    public bool AreEqual;
    /// <summary>The human-readable differences in comparison order.</summary>
    public List<string> Differences;

    /// <summary>Creates an equal result with an empty differences list.</summary>
    public ImageMetadataDiffResult()
    {
        AreEqual = true;
        Differences = new List<string>();
    }
}
