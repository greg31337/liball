namespace ttf;

/// <summary>Defines metadata and values for one legacy kerning subtable.</summary>
public interface IFontLegacyKerningSubtable
{
    /// <summary>Gets the subtable format number.</summary>
    /// <returns>Format 0, 1, 2, or 3.</returns>
    int GetFormat();

    /// <summary>Gets the encoded subtable length.</summary>
    /// <returns>The byte length.</returns>
    int GetLength();

    /// <summary>Gets the raw coverage mask.</summary>
    /// <returns>The coverage mask.</returns>
    int GetCoverage();

    /// <summary>Gets the Apple variation tuple index.</summary>
    /// <returns>The tuple index.</returns>
    int GetTupleIndex();

    /// <summary>Reports whether the subtable uses Apple metadata.</summary>
    /// <returns>True for Apple metadata.</returns>
    bool IsAppleFormat();

    /// <summary>Reports whether the subtable applies horizontally.</summary>
    /// <returns>True for horizontal kerning.</returns>
    bool IsHorizontal();

    /// <summary>Reports whether the subtable applies vertically.</summary>
    /// <returns>True for vertical kerning.</returns>
    bool IsVertical();

    /// <summary>Reports whether values represent minimum distances.</summary>
    /// <returns>True for minimum values.</returns>
    bool ContainsMinimumValues();

    /// <summary>Reports whether values apply across the text stream.</summary>
    /// <returns>True for cross-stream values.</returns>
    bool IsCrossStream();

    /// <summary>Reports whether this subtable replaces earlier values.</summary>
    /// <returns>True for replacement behavior.</returns>
    bool OverridesPreviousValues();

    /// <summary>Reports whether this is Apple variation data.</summary>
    /// <returns>True for variation data.</returns>
    bool IsVariationData();

    /// <summary>Reports whether independent glyph-pair lookup is supported.</summary>
    /// <returns>True for formats 0, 2, and 3.</returns>
    bool SupportsPairLookup();

    /// <summary>Gets the kerning value for a glyph pair.</summary>
    /// <param name="leftGlyphId">The left glyph identifier.</param>
    /// <param name="rightGlyphId">The right glyph identifier.</param>
    /// <returns>The design-unit kerning value, or zero when absent.</returns>
    /// <exception cref="System.InvalidOperationException">The format is state-machine based.</exception>
    int GetKerningValue(int leftGlyphId, int rightGlyphId);

    /// <summary>Gets the number of explicit format 0 pairs.</summary>
    /// <returns>The pair count.</returns>
    /// <exception cref="System.InvalidOperationException">The format is not 0.</exception>
    int GetPairCount();

    /// <summary>Gets an explicit pair's left glyph.</summary>
    /// <param name="index">The zero-based pair index.</param>
    /// <returns>The left glyph identifier.</returns>
    int GetPairLeftGlyphId(int index);

    /// <summary>Gets an explicit pair's right glyph.</summary>
    /// <param name="index">The zero-based pair index.</param>
    /// <returns>The right glyph identifier.</returns>
    int GetPairRightGlyphId(int index);

    /// <summary>Gets an explicit pair's value.</summary>
    /// <param name="index">The zero-based pair index.</param>
    /// <returns>The design-unit value.</returns>
    int GetPairValue(int index);

    /// <summary>Gets a format 1 state-machine class count.</summary>
    /// <returns>The class count.</returns>
    /// <exception cref="System.InvalidOperationException">The format is not 1.</exception>
    int GetStateClassCount();

    /// <summary>Gets a format 1 state count.</summary>
    /// <returns>The state count.</returns>
    int GetStateCount();

    /// <summary>Gets the format 1 initial state index.</summary>
    /// <returns>The initial state index.</returns>
    int GetInitialStateIndex();

    /// <summary>Gets the format 1 class for a glyph.</summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns>The state-machine class.</returns>
    int GetStateClassForGlyph(int glyphId);

    /// <summary>Gets the action index for a format 1 state and class.</summary>
    /// <param name="stateIndex">The zero-based state index.</param>
    /// <param name="classIndex">The zero-based class index.</param>
    /// <returns>The action index.</returns>
    int GetStateActionIndex(int stateIndex, int classIndex);

    /// <summary>Gets the number of format 1 actions.</summary>
    /// <returns>The action count.</returns>
    int GetStateActionCount();

    /// <summary>Gets one format 1 action's raw flags.</summary>
    /// <param name="index">The zero-based action index.</param>
    /// <returns>The raw flags.</returns>
    int GetStateActionFlags(int index);

    /// <summary>Gets one format 1 action's new state index.</summary>
    /// <param name="index">The zero-based action index.</param>
    /// <returns>The new state index.</returns>
    int GetStateActionNewStateIndex(int index);

    /// <summary>Reports whether a format 1 action pushes a glyph.</summary>
    /// <param name="index">The zero-based action index.</param>
    /// <returns>True when the glyph is pushed.</returns>
    bool StateActionPushesGlyph(int index);

    /// <summary>Reports whether a format 1 action suppresses advancement.</summary>
    /// <param name="index">The zero-based action index.</param>
    /// <returns>True when advancement is suppressed.</returns>
    bool StateActionDoesNotAdvance(int index);

    /// <summary>Gets the number of format 1 action values.</summary>
    /// <returns>The value count.</returns>
    int GetStateValueCount();

    /// <summary>Gets one format 1 action value.</summary>
    /// <param name="index">The zero-based value index.</param>
    /// <returns>The signed value.</returns>
    int GetStateValue(int index);
}
