namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="GlyphOutline"/>.</summary>
public interface IGlyphOutline
{
    /// <summary>Gets the glyph identifier.</summary>
    /// <returns>The zero-based glyph identifier.</returns>
    int GetGlyphId();

    /// <summary>Reports whether the glyph has no normalized path commands.</summary>
    /// <returns>True when the outline is empty.</returns>
    bool IsEmpty();

    /// <summary>Reports whether this glyph is composite.</summary>
    /// <returns>True for a composite TrueType glyph.</returns>
    bool IsComposite();

    /// <summary>Gets the exact normalized path bounds.</summary>
    /// <returns>The immutable exact bounds.</returns>
    GlyphBounds? GetBounds();

    /// <summary>Gets the normalized command count.</summary>
    /// <returns>The command count.</returns>
    int GetCommandCount();

    /// <summary>Gets one normalized command.</summary>
    /// <param name="index">The zero-based command index.</param>
    /// <returns>The immutable command.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the command list.</exception>
    GlyphPathCommand GetCommand(int index);

    /// <summary>Copies all normalized commands in contour order.</summary>
    /// <returns>A new command array.</returns>
    GlyphPathCommand[] GetCommands();

    /// <summary>Gets the direct component count.</summary>
    /// <returns>The direct component count.</returns>
    int GetComponentCount();

    /// <summary>Gets one direct component.</summary>
    /// <param name="index">The zero-based direct component index.</param>
    /// <returns>The immutable component metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the component list.</exception>
    GlyphComponent GetComponent(int index);

    /// <summary>Copies all direct component metadata.</summary>
    /// <returns>A new component array.</returns>
    GlyphComponent[] GetComponents();

    /// <summary>Gets the stable transitive component-closure count.</summary>
    /// <returns>The closure count.</returns>
    int GetComponentClosureCount();

    /// <summary>Gets one glyph identifier from the stable component closure.</summary>
    /// <param name="index">The zero-based closure index.</param>
    /// <returns>The component glyph identifier.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the closure.</exception>
    int GetComponentClosureGlyphId(int index);

    /// <summary>Copies the stable transitive component closure.</summary>
    /// <returns>A new glyph-identifier array.</returns>
    int[] GetComponentClosure();

    /// <summary>Gets the maximum nested composite depth.</summary>
    /// <returns>Zero for non-composites; otherwise the maximum component depth.</returns>
    int GetComponentDepth();

    /// <summary>Gets the glyph identifier supplying design-unit metrics.</summary>
    /// <returns>The resolved metrics glyph identifier.</returns>
    int GetMetricsGlyphId();

    /// <summary>Reports whether component metrics replace this glyph's own metrics.</summary>
    /// <returns>True when metrics are inherited.</returns>
    bool UsesInheritedMetrics();

    /// <summary>Reports whether overlapping components were declared.</summary>
    /// <returns>True when the overlap flag is present.</returns>
    bool HasOverlappingComponents();

    /// <summary>Gets the retained, unexecuted TrueType instruction length.</summary>
    /// <remarks>The instructions are not interpreted or rasterized.</remarks>
    /// <returns>The instruction byte count.</returns>
    int GetInstructionLength();

    /// <summary>Copies the retained, unexecuted TrueType instructions.</summary>
    /// <remarks>The instructions are not interpreted or rasterized.</remarks>
    /// <returns>A new instruction-byte array.</returns>
    byte[] CopyInstructions();

    /// <summary>Gets the preserved CFF stem-hint count.</summary>
    /// <returns>Zero for TrueType outlines; otherwise the Type 2 stem count.</returns>
    int GetCffStemHintCount();

    /// <summary>Gets one preserved CFF stem hint.</summary>
    /// <param name="index">The zero-based stem-hint index.</param>
    /// <returns>The immutable stem-hint data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the hint list.</exception>
    CffStemHint GetCffStemHint(int index);

    /// <summary>Copies the preserved CFF stem-hint view array.</summary>
    /// <returns>A new array containing immutable hint views.</returns>
    CffStemHint[] GetCffStemHints();

    /// <summary>Gets the preserved CFF hint-mask count.</summary>
    /// <returns>Zero for TrueType outlines; otherwise the Type 2 mask count.</returns>
    int GetCffHintMaskCount();

    /// <summary>Gets one preserved CFF hint mask.</summary>
    /// <param name="index">The zero-based hint-mask index.</param>
    /// <returns>The immutable hint-mask data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the mask list.</exception>
    CffHintMask GetCffHintMask(int index);

    /// <summary>Copies the preserved CFF hint-mask view array.</summary>
    /// <returns>A new array containing immutable mask views.</returns>
    CffHintMask[] GetCffHintMasks();
}
