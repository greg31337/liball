namespace ttf;

/// <summary>Contains one immutable normalized glyph outline and its source metadata.</summary>
public class GlyphOutline : IGlyphOutline
{
    int glyphId;
    GlyphPathCommand[]? commands;
    GlyphComponent[]? components;
    int[]? componentClosure;
    byte[]? instructions;
    CffStemHint[]? cffStemHints;
    CffHintMask[]? cffHintMasks;
    GlyphBounds? bounds;
    bool composite;
    int componentDepth;
    int metricsGlyphId;
    bool overlappingComponents;

    internal GlyphOutline(
        SfntResolvedGlyph? resolved,
        SfntVariedGlyph? varied,
        SfntSimpleGlyph? simple,
        SfntCompositeGlyph? compositeGlyph,
        CffGlyph? cffGlyph)
    {
        if (cffGlyph != null)
        {
            InitializeCff(cffGlyph);
            return;
        }

        if (varied != null)
        {
            InitializeTrueType(
                varied.GetResolvedGlyph(),
                simple,
                compositeGlyph,
                varied);
            return;
        }

        if (resolved == null)
        {
            throw new System.ArgumentNullException("resolved");
        }

        InitializeTrueType(resolved, simple, compositeGlyph, null);
    }

    internal static GlyphOutline Create(
        SfntResolvedGlyph resolved,
        SfntSimpleGlyph? simple,
        SfntCompositeGlyph? compositeGlyph)
    {
        return new GlyphOutline(
            resolved,
            null,
            simple,
            compositeGlyph,
            null);
    }

    internal static GlyphOutline Create(
        SfntVariedGlyph varied,
        SfntSimpleGlyph? simple,
        SfntCompositeGlyph? compositeGlyph)
    {
        if (varied == null)
        {
            throw new System.ArgumentNullException("varied");
        }

        return new GlyphOutline(
            null,
            varied,
            simple,
            compositeGlyph,
            null);
    }

    void InitializeTrueType(
        SfntResolvedGlyph resolved,
        SfntSimpleGlyph? simple,
        SfntCompositeGlyph? compositeGlyph,
        SfntVariedGlyph? varied)
    {
        glyphId = resolved.GetGlyphId();
        SfntGlyphOutline source = resolved.GetOutline();
        commands = new GlyphPathCommand[source.GetCommandCount()];
        int commandIndex = 0;

        while (commandIndex < commands.Length)
        {
            commands[commandIndex] = new GlyphPathCommand(
                source.GetCommand(commandIndex));
            commandIndex += 1;
        }

        int componentCount = compositeGlyph == null
            ? 0
            : compositeGlyph.GetComponentCount();
        components = new GlyphComponent[componentCount];
        int componentIndex = 0;

        while (componentIndex < componentCount)
        {
            System.ArgumentNullException.ThrowIfNull(compositeGlyph);
            SfntCompositeComponent component =
                compositeGlyph.GetComponent(componentIndex);
            components[componentIndex] = varied == null ?
                GlyphComponent.Create(component) :
                GlyphComponent.Create(
                    component,
                    varied.GetEffectiveComponentXOffset(componentIndex),
                    varied.GetEffectiveComponentYOffset(componentIndex));
            componentIndex += 1;
        }

        componentClosure = new int[resolved.GetComponentClosureCount()];
        int closureIndex = 0;

        while (closureIndex < componentClosure.Length)
        {
            componentClosure[closureIndex] =
                resolved.GetComponentClosureGlyphId(closureIndex);
            closureIndex += 1;
        }

        if (simple != null)
        {
            instructions = simple.CopyInstructions();
        }
        else if (compositeGlyph != null)
        {
            instructions = compositeGlyph.CopyInstructions();
        }
        else
        {
            instructions = new byte[0];
        }

        bounds = new GlyphBounds(source.GetBounds());
        composite = resolved.IsComposite();
        componentDepth = resolved.GetComponentDepth();
        metricsGlyphId = resolved.GetMetricsGlyphId();
        overlappingComponents = compositeGlyph != null &&
            compositeGlyph.HasOverlappingComponents();
        cffStemHints = new CffStemHint[0];
        cffHintMasks = new CffHintMask[0];
    }

    void InitializeCff(CffGlyph glyph)
    {
        if (glyph == null)
        {
            throw new System.ArgumentNullException("glyph");
        }

        glyphId = glyph.GetGlyphId();
        SfntGlyphOutline source = glyph.GetOutline();
        commands = new GlyphPathCommand[source.GetCommandCount()];
        int commandIndex = 0;

        while (commandIndex < commands.Length)
        {
            commands[commandIndex] = new GlyphPathCommand(
                source.GetCommand(commandIndex));
            commandIndex += 1;
        }

        components = new GlyphComponent[0];
        componentClosure = new int[0];
        instructions = new byte[0];
        Type2CharStringResult result = glyph.GetExecutionResult();
        cffStemHints = new CffStemHint[result.GetStemCount()];
        int stemIndex = 0;

        while (stemIndex < cffStemHints.Length)
        {
            cffStemHints[stemIndex] = new CffStemHint(
                result.GetStem(stemIndex));
            stemIndex += 1;
        }

        cffHintMasks = new CffHintMask[result.GetHintMaskCount()];
        int maskIndex = 0;

        while (maskIndex < cffHintMasks.Length)
        {
            cffHintMasks[maskIndex] = new CffHintMask(
                result.GetHintMask(maskIndex));
            maskIndex += 1;
        }

        bounds = new GlyphBounds(source.GetBounds());
        metricsGlyphId = glyphId;
    }

    internal static GlyphOutline Create(CffGlyph glyph)
    {
        if (glyph == null)
        {
            throw new System.ArgumentNullException("glyph");
        }

        return new GlyphOutline(null, null, null, null, glyph);
    }

    /// <summary>Gets the glyph identifier.</summary>
    /// <returns>The zero-based glyph identifier.</returns>
    public int GetGlyphId() { return glyphId; }

    /// <summary>Reports whether the glyph has no normalized path commands.</summary>
    /// <returns>True when the outline is empty.</returns>
    public bool IsEmpty() { System.ArgumentNullException.ThrowIfNull(commands); return commands.Length == 0; }

    /// <summary>Reports whether this glyph is composite.</summary>
    /// <returns>True for a composite TrueType glyph.</returns>
    public bool IsComposite() { return composite; }

    /// <summary>Gets the exact normalized path bounds.</summary>
    /// <returns>The immutable exact bounds.</returns>
    public GlyphBounds? GetBounds() { return bounds; }

    /// <summary>Gets the normalized command count.</summary>
    /// <returns>The command count.</returns>
    public int GetCommandCount() { System.ArgumentNullException.ThrowIfNull(commands); return commands.Length; }

    /// <summary>Gets one normalized command.</summary>
    /// <param name="index">The zero-based command index.</param>
    /// <returns>The immutable command.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the command list.</exception>
    public GlyphPathCommand GetCommand(int index)
    {
        System.ArgumentNullException.ThrowIfNull(commands);
        if (index < 0 || index >= commands.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return commands[index];
    }

    /// <summary>Copies all normalized commands in contour order.</summary>
    /// <returns>A new command array.</returns>
    public GlyphPathCommand[] GetCommands() { System.ArgumentNullException.ThrowIfNull(commands); return commands.AsSpan().ToArray(); }

    /// <summary>Gets the direct component count.</summary>
    /// <returns>The direct component count.</returns>
    public int GetComponentCount() { System.ArgumentNullException.ThrowIfNull(components); return components.Length; }

    /// <summary>Gets one direct component.</summary>
    /// <param name="index">The zero-based direct component index.</param>
    /// <returns>The immutable component metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the component list.</exception>
    public GlyphComponent GetComponent(int index)
    {
        System.ArgumentNullException.ThrowIfNull(components);
        if (index < 0 || index >= components.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return components[index];
    }

    /// <summary>Copies all direct component metadata.</summary>
    /// <returns>A new component array.</returns>
    public GlyphComponent[] GetComponents() { System.ArgumentNullException.ThrowIfNull(components); return components.AsSpan().ToArray(); }

    /// <summary>Gets the stable transitive component-closure count.</summary>
    /// <returns>The closure count.</returns>
    public int GetComponentClosureCount() { System.ArgumentNullException.ThrowIfNull(componentClosure); return componentClosure.Length; }

    /// <summary>Gets one glyph identifier from the stable component closure.</summary>
    /// <param name="index">The zero-based closure index.</param>
    /// <returns>The component glyph identifier.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the closure.</exception>
    public int GetComponentClosureGlyphId(int index)
    {
        System.ArgumentNullException.ThrowIfNull(componentClosure);
        if (index < 0 || index >= componentClosure.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return componentClosure[index];
    }

    /// <summary>Copies the stable transitive component closure.</summary>
    /// <returns>A new glyph-identifier array.</returns>
    public int[] GetComponentClosure() { System.ArgumentNullException.ThrowIfNull(componentClosure); return componentClosure.AsSpan().ToArray(); }

    /// <summary>Gets the maximum nested composite depth.</summary>
    /// <returns>Zero for non-composites; otherwise the maximum component depth.</returns>
    public int GetComponentDepth() { return componentDepth; }

    /// <summary>Gets the glyph identifier supplying design-unit metrics.</summary>
    /// <returns>The resolved metrics glyph identifier.</returns>
    public int GetMetricsGlyphId() { return metricsGlyphId; }

    /// <summary>Reports whether component metrics replace this glyph's own metrics.</summary>
    /// <returns>True when metrics are inherited.</returns>
    public bool UsesInheritedMetrics() { return metricsGlyphId != glyphId; }

    /// <summary>Reports whether overlapping components were declared.</summary>
    /// <returns>True when the overlap flag is present.</returns>
    public bool HasOverlappingComponents() { return overlappingComponents; }

    /// <summary>Gets the retained, unexecuted TrueType instruction length.</summary>
    /// <remarks>The instructions are not interpreted or rasterized.</remarks>
    /// <returns>The instruction byte count.</returns>
    public int GetInstructionLength() { System.ArgumentNullException.ThrowIfNull(instructions); return instructions.Length; }

    /// <summary>Copies the retained, unexecuted TrueType instructions.</summary>
    /// <remarks>The instructions are not interpreted or rasterized.</remarks>
    /// <returns>A new instruction-byte array.</returns>
    public byte[] CopyInstructions() { System.ArgumentNullException.ThrowIfNull(instructions); return instructions.AsSpan().ToArray(); }

    /// <summary>Gets the preserved CFF stem-hint count.</summary>
    /// <returns>Zero for TrueType outlines; otherwise the Type 2 stem count.</returns>
    public int GetCffStemHintCount() { System.ArgumentNullException.ThrowIfNull(cffStemHints); return cffStemHints.Length; }

    /// <summary>Gets one preserved CFF stem hint.</summary>
    /// <param name="index">The zero-based stem-hint index.</param>
    /// <returns>The immutable stem-hint data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the hint list.</exception>
    public CffStemHint GetCffStemHint(int index)
    {
        System.ArgumentNullException.ThrowIfNull(cffStemHints);
        if (index < 0 || index >= cffStemHints.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return cffStemHints[index];
    }

    /// <summary>Copies the preserved CFF stem-hint view array.</summary>
    /// <returns>A new array containing immutable hint views.</returns>
    public CffStemHint[] GetCffStemHints()
    {
        System.ArgumentNullException.ThrowIfNull(cffStemHints);
        return cffStemHints.AsSpan().ToArray();
    }

    /// <summary>Gets the preserved CFF hint-mask count.</summary>
    /// <returns>Zero for TrueType outlines; otherwise the Type 2 mask count.</returns>
    public int GetCffHintMaskCount() { System.ArgumentNullException.ThrowIfNull(cffHintMasks); return cffHintMasks.Length; }

    /// <summary>Gets one preserved CFF hint mask.</summary>
    /// <param name="index">The zero-based hint-mask index.</param>
    /// <returns>The immutable hint-mask data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the mask list.</exception>
    public CffHintMask GetCffHintMask(int index)
    {
        System.ArgumentNullException.ThrowIfNull(cffHintMasks);
        if (index < 0 || index >= cffHintMasks.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return cffHintMasks[index];
    }

    /// <summary>Copies the preserved CFF hint-mask view array.</summary>
    /// <returns>A new array containing immutable mask views.</returns>
    public CffHintMask[] GetCffHintMasks()
    {
        System.ArgumentNullException.ThrowIfNull(cffHintMasks);
        return cffHintMasks.AsSpan().ToArray();
    }
}
