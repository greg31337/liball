namespace ttf;

/// <summary>Aggregates immutable public inspection results for one font face.</summary>
public class FontDescription : IFontDescription
{
    int faceIndex;
    uint scalerType;
    FontOutlineKind outlineKind;
    FontNames names;
    FontClassification classification;
    FontCoverage coverage;
    FontMetrics metrics;
    FontEmbeddingRights? embeddingRights;

    internal FontDescription(
        int faceIndex,
        uint scalerType,
        FontOutlineKind outlineKind,
        FontNames names,
        FontClassification classification,
        FontCoverage coverage,
        FontMetrics metrics,
        FontEmbeddingRights? embeddingRights)
    {
        this.faceIndex = faceIndex;
        this.scalerType = scalerType;
        this.outlineKind = outlineKind;
        this.names = names;
        this.classification = classification;
        this.coverage = coverage;
        this.metrics = metrics;
        this.embeddingRights = embeddingRights;
    }

    /// <summary>Gets the zero-based package face index.</summary>
    /// <returns>The face index.</returns>
    public int GetFaceIndex() { return faceIndex; }

    /// <summary>Gets the SFNT scaler type.</summary>
    /// <returns>The big-endian scaler type.</returns>
    public uint GetScalerType() { return scalerType; }

    /// <summary>Gets the validated outline kind.</summary>
    /// <returns>The outline kind.</returns>
    public FontOutlineKind GetOutlineKind() { return outlineKind; }

    /// <summary>Gets localized naming metadata.</summary>
    /// <returns>The names result.</returns>
    public FontNames GetNames() { return names; }

    /// <summary>Gets style and family classification metadata.</summary>
    /// <returns>The classification result.</returns>
    public FontClassification GetClassification() { return classification; }

    /// <summary>Gets merged Unicode coverage.</summary>
    /// <returns>The coverage result.</returns>
    public FontCoverage GetCoverage() { return coverage; }

    /// <summary>Gets unscaled metric-source metadata.</summary>
    /// <returns>The metrics result.</returns>
    public FontMetrics GetMetrics() { return metrics; }

    /// <summary>Gets interpreted embedding rights.</summary>
    /// <returns>The rights result, or null when OS/2 is unavailable.</returns>
    public FontEmbeddingRights? GetEmbeddingRights() { return embeddingRights; }
}
