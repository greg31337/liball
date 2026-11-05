namespace ttf;

/// <summary>Defines public variable-font metadata and style attributes.</summary>
public interface IFontVariations
{
    /// <summary>Gets the requested language used for localized names.</summary>
    /// <returns>The language tag, or null for default selection.</returns>
    string? GetRequestedLanguage();

    /// <summary>Gets the number of <c>fvar</c> axes.</summary>
    /// <returns>The axis count, or zero for a static face.</returns>
    int GetAxisCount();

    /// <summary>Gets one variation axis.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The axis model.</returns>
    FontVariationAxis GetAxis(int index);

    /// <summary>Finds a variation axis by tag.</summary>
    /// <param name="axisTag">The big-endian axis tag.</param>
    /// <returns>The axis model, or null.</returns>
    FontVariationAxis? FindAxis(int axisTag);

    /// <summary>Gets the number of named instances.</summary>
    /// <returns>The named-instance count.</returns>
    int GetNamedInstanceCount();

    /// <summary>Gets one named instance.</summary>
    /// <param name="index">The zero-based instance index.</param>
    /// <returns>The named-instance model.</returns>
    FontNamedInstance GetNamedInstance(int index);

    /// <summary>Gets the <c>avar</c> axis-mapping model.</summary>
    /// <returns>The model, or null when <c>avar</c> is absent.</returns>
    FontAvar? GetAvar();

    /// <summary>Gets the <c>STAT</c> style-attributes model.</summary>
    /// <returns>The model, or null when <c>STAT</c> is absent.</returns>
    FontStat? GetStat();
}
