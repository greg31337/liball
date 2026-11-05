namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontDescription"/>.</summary>
public interface IFontDescription
{
    /// <summary>Gets the zero-based package face index.</summary>
    /// <returns>The face index.</returns>
    int GetFaceIndex();

    /// <summary>Gets the SFNT scaler type.</summary>
    /// <returns>The big-endian scaler type.</returns>
    uint GetScalerType();

    /// <summary>Gets the validated outline kind.</summary>
    /// <returns>The outline kind.</returns>
    FontOutlineKind GetOutlineKind();

    /// <summary>Gets localized naming metadata.</summary>
    /// <returns>The names result.</returns>
    FontNames GetNames();

    /// <summary>Gets style and family classification metadata.</summary>
    /// <returns>The classification result.</returns>
    FontClassification GetClassification();

    /// <summary>Gets merged Unicode coverage.</summary>
    /// <returns>The coverage result.</returns>
    FontCoverage GetCoverage();

    /// <summary>Gets unscaled metric-source metadata.</summary>
    /// <returns>The metrics result.</returns>
    FontMetrics GetMetrics();

    /// <summary>Gets interpreted embedding rights.</summary>
    /// <returns>The rights result, or null when OS/2 is unavailable.</returns>
    FontEmbeddingRights? GetEmbeddingRights();
}
