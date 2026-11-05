namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifCapabilityReport"/>.</summary>
public interface IAvifCapabilityReport
{
    /// <summary>Reports whether the source declares an AVIF brand.</summary>
    /// <returns><c>true</c> for an AVIF still, sequence, intra-only, or profile brand.</returns>
    bool IsAvif();

    /// <summary>Reports whether still-image item capability is declared.</summary>
    /// <returns><c>true</c> when the <c>avif</c> brand is present.</returns>
    bool HasImageItems();

    /// <summary>Reports whether timed image-sequence capability is declared.</summary>
    /// <returns><c>true</c> when the <c>avis</c> brand is present.</returns>
    bool HasImageSequence();

    /// <summary>Reports whether the intra-only AVIF constraint is declared.</summary>
    /// <returns><c>true</c> when the <c>avio</c> brand is present.</returns>
    bool IsIntraOnly();

    /// <summary>Reports whether the AVIF baseline profile is declared.</summary>
    /// <returns><c>true</c> when the <c>MA1B</c> brand is present.</returns>
    bool HasBaselineProfile();

    /// <summary>Reports whether the AVIF advanced profile is declared.</summary>
    /// <returns><c>true</c> when the <c>MA1A</c> brand is present.</returns>
    bool HasAdvancedProfile();

    /// <summary>Returns the MIME type inferred from declared brands.</summary>
    /// <returns>The inferred MIME type, or an empty string when unknown.</returns>
    string GetMimeType();

    /// <summary>Returns the number of detected high-level features.</summary>
    /// <returns>The feature count.</returns>
    int GetFeatureCount();

    /// <summary>Returns one detected feature and its supported operations.</summary>
    /// <param name="index">The zero-based detected-feature index.</param>
    /// <returns>An independent feature description, or null for an invalid index.</returns>
    AvifFeatureCapability? GetFeature(int index);

    /// <summary>Reports whether one high-level feature was detected.</summary>
    /// <param name="feature">The feature to locate.</param>
    /// <returns><c>true</c> when the source declares or contains the feature.</returns>
    bool HasFeature(AvifFeature feature);

    /// <summary>Finds the supported operations for one detected feature.</summary>
    /// <param name="feature">The feature to locate.</param>
    /// <returns>An independent feature description, or null when undetected.</returns>
    AvifFeatureCapability? GetFeatureCapability(AvifFeature feature);
}
