namespace ttf;

/// <summary>Defines baseline coordinates for one script.</summary>
public interface IFontBaselineScript
{
    /// <summary>Gets the script tag.</summary>
    /// <returns>The big-endian script tag.</returns>
    int GetScriptTag();

    /// <summary>Reports whether the script supplies baseline coordinates.</summary>
    /// <returns>True when coordinate values are present.</returns>
    bool HasBaselineValues();

    /// <summary>Gets the default baseline tag.</summary>
    /// <returns>The default baseline tag.</returns>
    /// <exception cref="System.InvalidOperationException">Baseline values are absent.</exception>
    int GetDefaultBaselineTag();

    /// <summary>Gets the number of baseline coordinates.</summary>
    /// <returns>The coordinate count.</returns>
    int GetCoordinateCount();

    /// <summary>Gets one baseline coordinate in axis tag order.</summary>
    /// <param name="index">The zero-based baseline tag index.</param>
    /// <returns>The coordinate.</returns>
    FontBaselineCoordinate GetCoordinate(int index);

    /// <summary>Gets the script's default minimum and maximum extents.</summary>
    /// <returns>The extents, or null.</returns>
    FontBaselineMinMax? GetDefaultMinMax();

    /// <summary>Gets the language-system extent count.</summary>
    /// <returns>The count.</returns>
    int GetLanguageSystemCount();

    /// <summary>Gets a language-system extent record.</summary>
    /// <param name="index">The index.</param>
    /// <returns>The language-system record.</returns>
    FontBaselineLanguageSystem GetLanguageSystem(int index);

    /// <summary>Finds a language-system extent record.</summary>
    /// <param name="languageTag">The language tag.</param>
    /// <returns>The record, or null.</returns>
    FontBaselineLanguageSystem? FindLanguageSystem(int languageTag);
}
