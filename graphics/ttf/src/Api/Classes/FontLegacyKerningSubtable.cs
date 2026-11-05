namespace ttf;

/// <summary>Contains metadata and values for one legacy kerning subtable.</summary>
public class FontLegacyKerningSubtable : IFontLegacyKerningSubtable
{
    SfntKernSubtable subtable;
    int glyphCount;

    internal FontLegacyKerningSubtable(
        SfntKernSubtable subtable,
        int glyphCount)
    {
        this.subtable = subtable;
        this.glyphCount = glyphCount;
    }

    /// <summary>Gets the subtable format number.</summary>
    /// <returns>Format 0, 1, 2, or 3.</returns>
    public int GetFormat() { return subtable.GetFormat(); }

    /// <summary>Gets the encoded subtable length.</summary>
    /// <returns>The byte length.</returns>
    public int GetLength() { return subtable.GetLength(); }

    /// <summary>Gets the raw coverage mask.</summary>
    /// <returns>The coverage mask.</returns>
    public int GetCoverage() { return subtable.GetCoverage(); }

    /// <summary>Gets the Apple variation tuple index.</summary>
    /// <returns>The tuple index.</returns>
    public int GetTupleIndex() { return subtable.GetTupleIndex(); }

    /// <summary>Reports whether the subtable uses Apple metadata.</summary>
    /// <returns>True for Apple metadata.</returns>
    public bool IsAppleFormat() { return subtable.IsAppleFormat(); }

    /// <summary>Reports whether the subtable applies horizontally.</summary>
    /// <returns>True for horizontal kerning.</returns>
    public bool IsHorizontal() { return subtable.IsHorizontal(); }

    /// <summary>Reports whether the subtable applies vertically.</summary>
    /// <returns>True for vertical kerning.</returns>
    public bool IsVertical() { return subtable.IsVertical(); }

    /// <summary>Reports whether values represent minimum distances.</summary>
    /// <returns>True for minimum values.</returns>
    public bool ContainsMinimumValues() { return subtable.ContainsMinimumValues(); }

    /// <summary>Reports whether values apply across the text stream.</summary>
    /// <returns>True for cross-stream values.</returns>
    public bool IsCrossStream() { return subtable.IsCrossStream(); }

    /// <summary>Reports whether this subtable replaces earlier values.</summary>
    /// <returns>True for replacement behavior.</returns>
    public bool OverridesPreviousValues() { return subtable.OverridesPreviousValues(); }

    /// <summary>Reports whether this is Apple variation data.</summary>
    /// <returns>True for variation data.</returns>
    public bool IsVariationData() { return subtable.IsVariationData(); }

    /// <summary>Reports whether independent glyph-pair lookup is supported.</summary>
    /// <returns>True for formats 0, 2, and 3.</returns>
    public bool SupportsPairLookup() { return subtable.GetFormat() != 1; }

    /// <summary>Gets the kerning value for a glyph pair.</summary>
    /// <param name="leftGlyphId">The left glyph identifier.</param>
    /// <param name="rightGlyphId">The right glyph identifier.</param>
    /// <returns>The design-unit kerning value, or zero when absent.</returns>
    /// <exception cref="System.InvalidOperationException">The format is state-machine based.</exception>
    public int GetKerningValue(int leftGlyphId, int rightGlyphId)
    {
        ValidateGlyphId(leftGlyphId, "leftGlyphId");
        ValidateGlyphId(rightGlyphId, "rightGlyphId");

        if (subtable.GetFormat() == 0)
        {
            SfntKernFormat0Data? data = subtable.GetFormat0();
            System.ArgumentNullException.ThrowIfNull(data);
            return data.GetKerningValue(leftGlyphId, rightGlyphId);
        }

        if (subtable.GetFormat() == 2)
        {
            SfntKernFormat2Data? data = subtable.GetFormat2();
            System.ArgumentNullException.ThrowIfNull(data);
            return data.GetKerningValue(leftGlyphId, rightGlyphId);
        }

        if (subtable.GetFormat() == 3)
        {
            SfntKernFormat3Data? data = subtable.GetFormat3();
            System.ArgumentNullException.ThrowIfNull(data);
            return data.GetKerningValue(leftGlyphId, rightGlyphId);
        }

        throw new System.InvalidOperationException(
            "State-machine kerning does not support independent pair lookup.");
    }

    /// <summary>Gets the number of explicit format 0 pairs.</summary>
    /// <returns>The pair count.</returns>
    /// <exception cref="System.InvalidOperationException">The format is not 0.</exception>
    public int GetPairCount() { return GetFormat0().GetPairCount(); }

    /// <summary>Gets an explicit pair's left glyph.</summary>
    /// <param name="index">The zero-based pair index.</param>
    /// <returns>The left glyph identifier.</returns>
    public int GetPairLeftGlyphId(int index) { return GetPair(index).GetLeftGlyphId(); }

    /// <summary>Gets an explicit pair's right glyph.</summary>
    /// <param name="index">The zero-based pair index.</param>
    /// <returns>The right glyph identifier.</returns>
    public int GetPairRightGlyphId(int index) { return GetPair(index).GetRightGlyphId(); }

    /// <summary>Gets an explicit pair's value.</summary>
    /// <param name="index">The zero-based pair index.</param>
    /// <returns>The design-unit value.</returns>
    public int GetPairValue(int index) { return GetPair(index).GetValue(); }

    /// <summary>Gets a format 1 state-machine class count.</summary>
    /// <returns>The class count.</returns>
    /// <exception cref="System.InvalidOperationException">The format is not 1.</exception>
    public int GetStateClassCount() { return GetFormat1().GetClassCount(); }

    /// <summary>Gets a format 1 state count.</summary>
    /// <returns>The state count.</returns>
    public int GetStateCount() { return GetFormat1().GetStateCount(); }

    /// <summary>Gets the format 1 initial state index.</summary>
    /// <returns>The initial state index.</returns>
    public int GetInitialStateIndex() { return GetFormat1().GetInitialStateIndex(); }

    /// <summary>Gets the format 1 class for a glyph.</summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns>The state-machine class.</returns>
    public int GetStateClassForGlyph(int glyphId)
    {
        ValidateGlyphId(glyphId, "glyphId");
        return GetFormat1().GetClassTable().GetClassForGlyph(glyphId);
    }

    /// <summary>Gets the action index for a format 1 state and class.</summary>
    /// <param name="stateIndex">The zero-based state index.</param>
    /// <param name="classIndex">The zero-based class index.</param>
    /// <returns>The action index.</returns>
    public int GetStateActionIndex(int stateIndex, int classIndex)
    {
        SfntKernFormat1Data data = GetFormat1();

        if (stateIndex < 0 || stateIndex >= data.GetStateCount())
        {
            throw new System.ArgumentOutOfRangeException("stateIndex");
        }

        if (classIndex < 0 || classIndex >= data.GetClassCount())
        {
            throw new System.ArgumentOutOfRangeException("classIndex");
        }

        return data.GetStateEntryIndex(stateIndex, classIndex);
    }

    /// <summary>Gets the number of format 1 actions.</summary>
    /// <returns>The action count.</returns>
    public int GetStateActionCount() { return GetFormat1().GetEntryCount(); }

    /// <summary>Gets one format 1 action's raw flags.</summary>
    /// <param name="index">The zero-based action index.</param>
    /// <returns>The raw flags.</returns>
    public int GetStateActionFlags(int index) { return GetStateAction(index).GetFlags(); }

    /// <summary>Gets one format 1 action's new state index.</summary>
    /// <param name="index">The zero-based action index.</param>
    /// <returns>The new state index.</returns>
    public int GetStateActionNewStateIndex(int index)
    {
        return GetStateAction(index).GetNewStateIndex();
    }

    /// <summary>Reports whether a format 1 action pushes a glyph.</summary>
    /// <param name="index">The zero-based action index.</param>
    /// <returns>True when the glyph is pushed.</returns>
    public bool StateActionPushesGlyph(int index)
    {
        return GetStateAction(index).PushesGlyph();
    }

    /// <summary>Reports whether a format 1 action suppresses advancement.</summary>
    /// <param name="index">The zero-based action index.</param>
    /// <returns>True when advancement is suppressed.</returns>
    public bool StateActionDoesNotAdvance(int index)
    {
        return GetStateAction(index).DoesNotAdvance();
    }

    /// <summary>Gets the number of format 1 action values.</summary>
    /// <returns>The value count.</returns>
    public int GetStateValueCount() { return GetFormat1().GetValueCount(); }

    /// <summary>Gets one format 1 action value.</summary>
    /// <param name="index">The zero-based value index.</param>
    /// <returns>The signed value.</returns>
    public int GetStateValue(int index)
    {
        SfntKernFormat1Data data = GetFormat1();

        if (index < 0 || index >= data.GetValueCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return data.GetValue(index);
    }

    SfntKernFormat0Data GetFormat0()
    {
        SfntKernFormat0Data? data = subtable.GetFormat0();

        if (data == null)
        {
            throw new System.InvalidOperationException(
                "Explicit pairs are available only for kern format 0.");
        }

        return data;
    }

    SfntKernPair GetPair(int index)
    {
        SfntKernFormat0Data data = GetFormat0();

        if (index < 0 || index >= data.GetPairCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return data.GetPair(index);
    }

    SfntKernFormat1Data GetFormat1()
    {
        SfntKernFormat1Data? data = subtable.GetFormat1();

        if (data == null)
        {
            throw new System.InvalidOperationException(
                "State-machine data is available only for kern format 1.");
        }

        return data;
    }

    SfntKernStateEntry GetStateAction(int index)
    {
        SfntKernFormat1Data data = GetFormat1();

        if (index < 0 || index >= data.GetEntryCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return data.GetEntry(index);
    }

    void ValidateGlyphId(int glyphId, string parameterName)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
