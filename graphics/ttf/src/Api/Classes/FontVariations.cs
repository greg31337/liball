namespace ttf;

/// <summary>Provides localized public variable-font and style metadata.</summary>
public class FontVariations : IFontVariations
{
    string? requestedLanguage;
    SfntFvarTable? fvar;
    FontNames names;
    FontAvar? avar;
    FontStat? stat;

    internal FontVariations(
        FontFace face,
        FontNames names,
        string? requestedLanguage)
    {
        this.names = names;
        this.requestedLanguage = requestedLanguage;
        fvar = face.GetFvarTable();
        SfntAvarTable? avarTable = face.GetAvarTable();
        SfntStatTable? statTable = face.GetStatTable();

        if (fvar == null && face.HasTable(FontTags.Fvar))
        {
            throw FontFormatException.Create("Variation inspection requires a supported fvar table.");
        }
        if (avarTable == null && face.HasTable(FontTags.Avar))
        {
            throw FontFormatException.Create("Variation inspection requires a supported avar table.");
        }
        if (statTable == null && face.HasTable(FontTags.Stat))
        {
            throw FontFormatException.Create("Variation inspection requires a supported STAT table.");
        }

        avar = avarTable == null ? null : new FontAvar(avarTable);
        stat = statTable == null ? null : new FontStat(statTable, names);
    }

    /// <summary>Gets the requested language used for localized names.</summary>
    /// <returns>The language tag, or null for default selection.</returns>
    public string? GetRequestedLanguage() { return requestedLanguage; }

    /// <summary>Gets the number of <c>fvar</c> axes.</summary>
    /// <returns>The axis count, or zero for a static face.</returns>
    public int GetAxisCount() { return fvar == null ? 0 : fvar.GetAxisCount(); }

    /// <summary>Gets one variation axis.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The axis model.</returns>
    public FontVariationAxis GetAxis(int index)
    {
        if (fvar == null)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontVariationAxis(fvar.GetAxis(index), names);
    }

    /// <summary>Finds a variation axis by tag.</summary>
    /// <param name="axisTag">The big-endian axis tag.</param>
    /// <returns>The axis model, or null.</returns>
    public FontVariationAxis? FindAxis(int axisTag)
    {
        if (fvar == null) { return null; }
        SfntFvarAxis? axis = fvar.FindAxis(axisTag);
        return axis == null ? null : new FontVariationAxis(axis, names);
    }

    /// <summary>Gets the number of named instances.</summary>
    /// <returns>The named-instance count.</returns>
    public int GetNamedInstanceCount()
    {
        return fvar == null ? 0 : fvar.GetInstanceCount();
    }

    /// <summary>Gets one named instance.</summary>
    /// <param name="index">The zero-based instance index.</param>
    /// <returns>The named-instance model.</returns>
    public FontNamedInstance GetNamedInstance(int index)
    {
        if (fvar == null)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontNamedInstance(index, fvar, fvar.GetInstance(index), names);
    }

    /// <summary>Gets the <c>avar</c> axis-mapping model.</summary>
    /// <returns>The model, or null when <c>avar</c> is absent.</returns>
    public FontAvar? GetAvar() { return avar; }

    /// <summary>Gets the <c>STAT</c> style-attributes model.</summary>
    /// <returns>The model, or null when <c>STAT</c> is absent.</returns>
    public FontStat? GetStat() { return stat; }
}
