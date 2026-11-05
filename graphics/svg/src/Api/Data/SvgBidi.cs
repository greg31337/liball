namespace svg;

/// <summary>Configures bounded Unicode bidirectional analysis.</summary>
public class SvgBidiOptions
{
    /// <summary>The paragraph direction selection mode.</summary>
    public SvgBidiDirection Direction;
    /// <summary>The maximum accepted scalar count.</summary>
    public int MaxScalars;
    /// <summary>The maximum explicit embedding level, from zero through 125.</summary>
    public int MaxExplicitLevel;
    /// <summary>Whether mirrored display scalars are substituted on odd levels.</summary>
    public bool ApplyMirroring;
    /// <summary>The cancellation token observed during bounded processing.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates bounded automatic-direction defaults.</summary>
    public SvgBidiOptions()
    {
        Direction = SvgBidiDirection.Auto;
        MaxScalars = 1000000;
        MaxExplicitLevel = 125;
        ApplyMirroring = true;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains resolved bidirectional data for one logical Unicode scalar.</summary>
public class SvgBidiScalar
{
    /// <summary>The logical scalar index.</summary>
    public int LogicalIndex;
    /// <summary>The normalized Unicode scalar value.</summary>
    public int Value;
    /// <summary>The original Unicode bidirectional class.</summary>
    public SvgBidiClass OriginalClass;
    /// <summary>The class after weak, neutral, and bracket resolution.</summary>
    public SvgBidiClass ResolvedClass;
    /// <summary>The final embedding level.</summary>
    public int Level;
    /// <summary>Whether the scalar is a non-rendering bidi formatting control.</summary>
    public bool RemovedControl;
    /// <summary>Whether the displayed scalar was mirrored.</summary>
    public bool Mirrored;
    /// <summary>The scalar value used in visual text.</summary>
    public int DisplayValue;
    /// <summary>The matching isolate control index, or minus one.</summary>
    public int MatchingControlIndex;
    /// <summary>The paired bracket index resolved by rule N0, or minus one.</summary>
    public int PairedBracketIndex;
    /// <summary>The visual scalar index, or minus one for non-rendering controls.</summary>
    public int VisualIndex;

    internal SvgBidiScalar()
    {
        LogicalIndex = -1;
        Value = 0;
        OriginalClass = SvgBidiClass.OtherNeutral;
        ResolvedClass = SvgBidiClass.OtherNeutral;
        Level = 0;
        RemovedControl = false;
        Mirrored = false;
        DisplayValue = 0;
        MatchingControlIndex = -1;
        PairedBracketIndex = -1;
        VisualIndex = -1;
    }
}

/// <summary>Contains one independently resolved bidirectional paragraph.</summary>
public class SvgBidiParagraph
{
    /// <summary>The first logical scalar index.</summary>
    public int ScalarStart;
    /// <summary>The logical scalar count including a trailing paragraph separator.</summary>
    public int ScalarCount;
    /// <summary>The resolved paragraph embedding level, zero or one.</summary>
    public int EmbeddingLevel;
    /// <summary>The first visual scalar index.</summary>
    public int VisualStart;
    /// <summary>The number of rendering scalars in the paragraph.</summary>
    public int VisualCount;

    internal SvgBidiParagraph()
    {
        ScalarStart = 0;
        ScalarCount = 0;
        EmbeddingLevel = 0;
        VisualStart = 0;
        VisualCount = 0;
    }
}

/// <summary>Contains bidirectional levels, visual text, and logical-to-visual maps.</summary>
public class SvgBidiResult
{
    /// <summary>Whether analysis completed successfully.</summary>
    public bool Success;
    /// <summary>The stable analysis outcome.</summary>
    public SvgBidiError Error;
    /// <summary>A deterministic failure explanation, or empty string.</summary>
    public string Message;
    /// <summary>The normalized logical text.</summary>
    public string LogicalText;
    /// <summary>The reordered text with non-rendering bidi controls omitted.</summary>
    public string VisualText;
    /// <summary>The per-logical-scalar resolved data.</summary>
    public System.Collections.Generic.List<SvgBidiScalar> Scalars;
    /// <summary>The independently resolved paragraphs.</summary>
    public System.Collections.Generic.List<SvgBidiParagraph> Paragraphs;
    /// <summary>Maps each logical scalar to a visual scalar, using minus one for controls.</summary>
    public System.Collections.Generic.List<int> LogicalToVisual;
    /// <summary>Maps each rendering visual scalar to its logical scalar.</summary>
    public System.Collections.Generic.List<int> VisualToLogical;

    internal SvgBidiResult()
    {
        Success = false;
        Error = SvgBidiError.None;
        Message = "";
        LogicalText = "";
        VisualText = "";
        Scalars = new System.Collections.Generic.List<SvgBidiScalar>();
        Paragraphs = new System.Collections.Generic.List<SvgBidiParagraph>();
        LogicalToVisual = new System.Collections.Generic.List<int>();
        VisualToLogical = new System.Collections.Generic.List<int>();
    }
}
