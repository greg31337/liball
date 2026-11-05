namespace ttf;

/// <summary>Contains baseline coordinates for one script.</summary>
public class FontBaselineScript : IFontBaselineScript
{
    FontBaselineAxis axis;
    SfntBaseScript script;

    internal FontBaselineScript(FontBaselineAxis axis, SfntBaseScript script)
    {
        this.axis = axis;
        this.script = script;
    }

    /// <summary>Gets the script tag.</summary>
    /// <returns>The big-endian script tag.</returns>
    public int GetScriptTag() { return script.GetScriptTag(); }

    /// <summary>Reports whether the script supplies baseline coordinates.</summary>
    /// <returns>True when coordinate values are present.</returns>
    public bool HasBaselineValues() { return script.GetValues() != null; }

    /// <summary>Gets the default baseline tag.</summary>
    /// <returns>The default baseline tag.</returns>
    /// <exception cref="System.InvalidOperationException">Baseline values are absent.</exception>
    public int GetDefaultBaselineTag()
    {
        SfntBaseValues? values = script.GetValues();

        if (values == null)
        {
            throw new System.InvalidOperationException(
                "The BASE script has no baseline values.");
        }

        return axis.GetBaselineTagValue(values.GetDefaultBaselineIndex());
    }

    /// <summary>Gets the number of baseline coordinates.</summary>
    /// <returns>The coordinate count.</returns>
    public int GetCoordinateCount()
    {
        SfntBaseValues? values = script.GetValues();
        return values == null ? 0 : values.GetCoordinateCount();
    }

    /// <summary>Gets one baseline coordinate in axis tag order.</summary>
    /// <param name="index">The zero-based baseline tag index.</param>
    /// <returns>The coordinate.</returns>
    public FontBaselineCoordinate GetCoordinate(int index)
    {
        SfntBaseValues? values = script.GetValues();

        if (values == null || index < 0 || index >= values.GetCoordinateCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return new FontBaselineCoordinate(
            axis.GetBaselineTagValue(index),
            values.GetCoordinate(index));
    }

    /// <summary>Gets the script's default minimum and maximum extents.</summary>
    /// <returns>The extents, or null.</returns>
    public FontBaselineMinMax? GetDefaultMinMax()
    {
        SfntBaseMinMax? extents = script.GetDefaultMinMax();
        return extents == null ? null : new FontBaselineMinMax(extents);
    }

    /// <summary>Gets the language-system extent count.</summary>
    /// <returns>The count.</returns>
    public int GetLanguageSystemCount()
    {
        return script.GetLanguageSystemCount();
    }

    /// <summary>Gets a language-system extent record.</summary>
    /// <param name="index">The index.</param>
    /// <returns>The language-system record.</returns>
    public FontBaselineLanguageSystem GetLanguageSystem(int index)
    {
        if (index < 0 || index >= script.GetLanguageSystemCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontBaselineLanguageSystem(script.GetLanguageSystem(index));
    }

    /// <summary>Finds a language-system extent record.</summary>
    /// <param name="languageTag">The language tag.</param>
    /// <returns>The record, or null.</returns>
    public FontBaselineLanguageSystem? FindLanguageSystem(int languageTag)
    {
        SfntBaseLanguageSystem? language =
            script.FindLanguageSystem(languageTag);
        return language == null ? null :
            new FontBaselineLanguageSystem(language);
    }
}
