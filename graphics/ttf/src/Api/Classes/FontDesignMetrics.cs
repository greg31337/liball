namespace ttf;

/// <summary>Contains selected global font metrics in design units.</summary>
public class FontDesignMetrics : IFontDesignMetrics
{
    FontMetricSelectionPolicy policy;
    FontLineMetricSource lineSource;
    int unitsPerEm;
    int ascender;
    int descender;
    int lineGap;
    FontBounds bounds;
    bool hasCapAndXHeight;
    int capHeight;
    int xHeight;
    bool hasUnderline;
    int underlinePosition;
    int underlineThickness;
    bool hasStrikeout;
    int strikeoutPosition;
    int strikeoutThickness;
    bool hasScriptMetrics;
    int subscriptXSize;
    int subscriptYSize;
    int subscriptXOffset;
    int subscriptYOffset;
    int superscriptXSize;
    int superscriptYSize;
    int superscriptXOffset;
    int superscriptYOffset;
    bool hasCaret;
    int caretSlopeRise;
    int caretSlopeRun;
    int caretOffset;
    bool hasVertical;
    int verticalAscender;
    int verticalDescender;
    int verticalLineGap;
    int verticalCaretSlopeRise;
    int verticalCaretSlopeRun;
    int verticalCaretOffset;
    int[] gaspRangeMaximumPpems;
    int[] gaspRangeBehaviors;

    internal FontDesignMetrics(
        FontMetricSelectionPolicy policy,
        FontLineMetricSource lineSource,
        int unitsPerEm,
        int ascender,
        int descender,
        int lineGap,
        FontBounds bounds,
        bool hasCapAndXHeight,
        int capHeight,
        int xHeight,
        bool hasUnderline,
        int underlinePosition,
        int underlineThickness,
        bool hasStrikeout,
        int strikeoutPosition,
        int strikeoutThickness,
        bool hasScriptMetrics,
        int subscriptXSize,
        int subscriptYSize,
        int subscriptXOffset,
        int subscriptYOffset,
        int superscriptXSize,
        int superscriptYSize,
        int superscriptXOffset,
        int superscriptYOffset,
        bool hasCaret,
        int caretSlopeRise,
        int caretSlopeRun,
        int caretOffset,
        bool hasVertical,
        int verticalAscender,
        int verticalDescender,
        int verticalLineGap,
        int verticalCaretSlopeRise,
        int verticalCaretSlopeRun,
        int verticalCaretOffset,
        int[] gaspRangeMaximumPpems,
        int[] gaspRangeBehaviors)
    {
        this.policy = policy;
        this.lineSource = lineSource;
        this.unitsPerEm = unitsPerEm;
        this.ascender = ascender;
        this.descender = descender;
        this.lineGap = lineGap;
        this.bounds = bounds;
        this.hasCapAndXHeight = hasCapAndXHeight;
        this.capHeight = capHeight;
        this.xHeight = xHeight;
        this.hasUnderline = hasUnderline;
        this.underlinePosition = underlinePosition;
        this.underlineThickness = underlineThickness;
        this.hasStrikeout = hasStrikeout;
        this.strikeoutPosition = strikeoutPosition;
        this.strikeoutThickness = strikeoutThickness;
        this.hasScriptMetrics = hasScriptMetrics;
        this.subscriptXSize = subscriptXSize;
        this.subscriptYSize = subscriptYSize;
        this.subscriptXOffset = subscriptXOffset;
        this.subscriptYOffset = subscriptYOffset;
        this.superscriptXSize = superscriptXSize;
        this.superscriptYSize = superscriptYSize;
        this.superscriptXOffset = superscriptXOffset;
        this.superscriptYOffset = superscriptYOffset;
        this.hasCaret = hasCaret;
        this.caretSlopeRise = caretSlopeRise;
        this.caretSlopeRun = caretSlopeRun;
        this.caretOffset = caretOffset;
        this.hasVertical = hasVertical;
        this.verticalAscender = verticalAscender;
        this.verticalDescender = verticalDescender;
        this.verticalLineGap = verticalLineGap;
        this.verticalCaretSlopeRise = verticalCaretSlopeRise;
        this.verticalCaretSlopeRun = verticalCaretSlopeRun;
        this.verticalCaretOffset = verticalCaretOffset;
        this.gaspRangeMaximumPpems = gaspRangeMaximumPpems;
        this.gaspRangeBehaviors = gaspRangeBehaviors;
    }

    /// <summary>Gets the requested selection policy.</summary>
    /// <returns>The selection policy.</returns>
    public FontMetricSelectionPolicy GetSelectionPolicy() { return policy; }

    /// <summary>Gets the selected line-metric source.</summary>
    /// <returns>The selected source.</returns>
    public FontLineMetricSource GetLineMetricSource() { return lineSource; }

    /// <summary>Gets the design-unit scale.</summary>
    /// <returns>Units per em.</returns>
    public int GetUnitsPerEm() { return unitsPerEm; }

    /// <summary>Gets the selected ascender.</summary>
    /// <returns>The ascender in design units.</returns>
    public int GetAscender() { return ascender; }

    /// <summary>Gets the selected descender.</summary>
    /// <returns>The descender in design units.</returns>
    public int GetDescender() { return descender; }

    /// <summary>Gets the selected line gap.</summary>
    /// <returns>The line gap in design units.</returns>
    public int GetLineGap() { return lineGap; }

    /// <summary>Gets the selected line height.</summary>
    /// <returns>Ascender minus descender plus line gap.</returns>
    public int GetLineHeight() { return ascender - descender + lineGap; }

    /// <summary>Gets the global head bounds.</summary>
    /// <returns>The global bounds.</returns>
    public FontBounds GetGlobalBounds() { return bounds; }

    /// <summary>Reports whether cap height and x-height are available.</summary>
    /// <returns>True when OS/2 version 2 fields are present.</returns>
    public bool HasCapHeightAndXHeight() { return hasCapAndXHeight; }

    /// <summary>Gets the cap height.</summary>
    /// <returns>The cap height in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Cap height is unavailable.</exception>
    public int GetCapHeight() { EnsureCapAndXHeight(); return capHeight; }

    /// <summary>Gets the x-height.</summary>
    /// <returns>The x-height in design units.</returns>
    /// <exception cref="System.InvalidOperationException">X-height is unavailable.</exception>
    public int GetXHeight() { EnsureCapAndXHeight(); return xHeight; }

    /// <summary>Reports whether underline metrics are available.</summary>
    /// <returns>True when post was parsed.</returns>
    public bool HasUnderlineMetrics() { return hasUnderline; }

    /// <summary>Gets the underline position.</summary>
    /// <returns>The position in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    public int GetUnderlinePosition() { EnsureUnderline(); return underlinePosition; }

    /// <summary>Gets the underline thickness.</summary>
    /// <returns>The thickness in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Underline metrics are unavailable.</exception>
    public int GetUnderlineThickness() { EnsureUnderline(); return underlineThickness; }

    /// <summary>Reports whether strikeout metrics are available.</summary>
    /// <returns>True when OS/2 was parsed.</returns>
    public bool HasStrikeoutMetrics() { return hasStrikeout; }

    /// <summary>Gets the strikeout position.</summary>
    /// <returns>The position in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    public int GetStrikeoutPosition() { EnsureStrikeout(); return strikeoutPosition; }

    /// <summary>Gets the strikeout thickness.</summary>
    /// <returns>The thickness in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Strikeout metrics are unavailable.</exception>
    public int GetStrikeoutThickness() { EnsureStrikeout(); return strikeoutThickness; }

    /// <summary>Reports whether subscript and superscript metrics are available.</summary>
    /// <returns>True when OS/2 script metrics are available.</returns>
    public bool HasScriptMetrics() { return hasScriptMetrics; }

    /// <summary>Gets the subscript horizontal size.</summary>
    /// <returns>The size in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public int GetSubscriptXSize() { EnsureScriptMetrics(); return subscriptXSize; }

    /// <summary>Gets the subscript vertical size.</summary>
    /// <returns>The size in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public int GetSubscriptYSize() { EnsureScriptMetrics(); return subscriptYSize; }

    /// <summary>Gets the subscript horizontal offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public int GetSubscriptXOffset() { EnsureScriptMetrics(); return subscriptXOffset; }

    /// <summary>Gets the subscript vertical offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public int GetSubscriptYOffset() { EnsureScriptMetrics(); return subscriptYOffset; }

    /// <summary>Gets the superscript horizontal size.</summary>
    /// <returns>The size in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public int GetSuperscriptXSize() { EnsureScriptMetrics(); return superscriptXSize; }

    /// <summary>Gets the superscript vertical size.</summary>
    /// <returns>The size in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public int GetSuperscriptYSize() { EnsureScriptMetrics(); return superscriptYSize; }

    /// <summary>Gets the superscript horizontal offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public int GetSuperscriptXOffset() { EnsureScriptMetrics(); return superscriptXOffset; }

    /// <summary>Gets the superscript vertical offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Script metrics are unavailable.</exception>
    public int GetSuperscriptYOffset() { EnsureScriptMetrics(); return superscriptYOffset; }

    /// <summary>Reports whether horizontal caret metrics are available.</summary>
    /// <returns>True when hhea was parsed.</returns>
    public bool HasCaretMetrics() { return hasCaret; }

    /// <summary>Gets the caret slope rise.</summary>
    /// <returns>The unscaled slope rise.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    public int GetCaretSlopeRise() { EnsureCaret(); return caretSlopeRise; }

    /// <summary>Gets the caret slope run.</summary>
    /// <returns>The unscaled slope run.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    public int GetCaretSlopeRun() { EnsureCaret(); return caretSlopeRun; }

    /// <summary>Gets the caret offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Caret metrics are unavailable.</exception>
    public int GetCaretOffset() { EnsureCaret(); return caretOffset; }

    /// <summary>Reports whether vertical global metrics are available.</summary>
    /// <returns>True when vhea was parsed.</returns>
    public bool HasVerticalMetrics() { return hasVertical; }

    /// <summary>Gets the vertical ascender.</summary>
    /// <returns>The ascender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalAscender() { EnsureVertical(); return verticalAscender; }

    /// <summary>Gets the vertical descender.</summary>
    /// <returns>The descender in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalDescender() { EnsureVertical(); return verticalDescender; }

    /// <summary>Gets the vertical line gap.</summary>
    /// <returns>The line gap in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalLineGap() { EnsureVertical(); return verticalLineGap; }

    /// <summary>Gets the vertical line height.</summary>
    /// <returns>Ascender minus descender plus line gap.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalLineHeight()
    {
        EnsureVertical();
        return verticalAscender - verticalDescender + verticalLineGap;
    }

    /// <summary>Gets the vertical caret slope rise.</summary>
    /// <returns>The unscaled slope rise.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalCaretSlopeRise() { EnsureVertical(); return verticalCaretSlopeRise; }

    /// <summary>Gets the vertical caret slope run.</summary>
    /// <returns>The unscaled slope run.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalCaretSlopeRun() { EnsureVertical(); return verticalCaretSlopeRun; }

    /// <summary>Gets the vertical caret offset.</summary>
    /// <returns>The offset in design units.</returns>
    /// <exception cref="System.InvalidOperationException">Vertical metrics are unavailable.</exception>
    public int GetVerticalCaretOffset() { EnsureVertical(); return verticalCaretOffset; }

    /// <summary>Gets the number of resolved gasp ranges.</summary>
    /// <returns>The range count, or zero when gasp is absent.</returns>
    public int GetGaspRangeCount() { return gaspRangeMaximumPpems.Length; }

    /// <summary>Gets a resolved gasp range maximum.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The maximum pixels per em.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the ranges.</exception>
    public int GetGaspRangeMaximumPpem(int index)
    {
        ValidateGaspRangeIndex(index);
        return gaspRangeMaximumPpems[index];
    }

    /// <summary>Gets a resolved gasp range behavior mask.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The behavior mask.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the ranges.</exception>
    public int GetGaspRangeBehavior(int index)
    {
        ValidateGaspRangeIndex(index);
        return gaspRangeBehaviors[index];
    }

    internal FontDesignMetrics ApplyVariations(SfntMvarLocation location)
    {
        if (location == null)
        {
            throw new System.ArgumentNullException("location");
        }

        int variedAscender = ascender;
        int variedDescender = descender;
        int variedLineGap = lineGap;

        if (lineSource == FontLineMetricSource.Typographic)
        {
            variedAscender = AddDelta(
                ascender,
                location.CalculateDelta(SfntMvarValueTags.HorizontalAscender));
            variedDescender = AddDelta(
                descender,
                location.CalculateDelta(SfntMvarValueTags.HorizontalDescender));
            variedLineGap = AddDelta(
                lineGap,
                location.CalculateDelta(SfntMvarValueTags.HorizontalLineGap));
        }
        else if (lineSource == FontLineMetricSource.Windows)
        {
            variedAscender = AddDelta(
                ascender,
                location.CalculateDelta(
                    SfntMvarValueTags.HorizontalClippingAscent));
            variedDescender = AddDelta(
                descender,
                -location.CalculateDelta(
                    SfntMvarValueTags.HorizontalClippingDescent));
        }

        int[] variedGaspMaximums =
            new int[gaspRangeMaximumPpems.Length];
        int gaspIndex = 0;

        while (gaspIndex < variedGaspMaximums.Length)
        {
            int maximum = gaspRangeMaximumPpems[gaspIndex];

            if (gaspIndex < variedGaspMaximums.Length - 1)
            {
                maximum = AddDelta(
                    maximum,
                    location.CalculateDelta(
                        SfntMvarValueTags.GaspRange0 + gaspIndex));
            }

            if (maximum < 0 || maximum > 0xFFFF ||
                (gaspIndex > 0 &&
                maximum <= variedGaspMaximums[gaspIndex - 1]))
            {
                throw FontFormatException.Create(
                    "Varied gasp range maxima are invalid.");
            }

            variedGaspMaximums[gaspIndex] = maximum;
            gaspIndex += 1;
        }

        return new FontDesignMetrics(
            policy,
            lineSource,
            unitsPerEm,
            variedAscender,
            variedDescender,
            variedLineGap,
            bounds,
            hasCapAndXHeight,
            AddOptionalDelta(capHeight, hasCapAndXHeight, location,
                SfntMvarValueTags.CapHeight),
            AddOptionalDelta(xHeight, hasCapAndXHeight, location,
                SfntMvarValueTags.XHeight),
            hasUnderline,
            AddOptionalDelta(underlinePosition, hasUnderline, location,
                SfntMvarValueTags.UnderlinePosition),
            AddOptionalDelta(underlineThickness, hasUnderline, location,
                SfntMvarValueTags.UnderlineSize),
            hasStrikeout,
            AddOptionalDelta(strikeoutPosition, hasStrikeout, location,
                SfntMvarValueTags.StrikeoutPosition),
            AddOptionalDelta(strikeoutThickness, hasStrikeout, location,
                SfntMvarValueTags.StrikeoutSize),
            hasScriptMetrics,
            AddOptionalDelta(subscriptXSize, hasScriptMetrics, location,
                SfntMvarValueTags.SubscriptXSize),
            AddOptionalDelta(subscriptYSize, hasScriptMetrics, location,
                SfntMvarValueTags.SubscriptYSize),
            AddOptionalDelta(subscriptXOffset, hasScriptMetrics, location,
                SfntMvarValueTags.SubscriptXOffset),
            AddOptionalDelta(subscriptYOffset, hasScriptMetrics, location,
                SfntMvarValueTags.SubscriptYOffset),
            AddOptionalDelta(superscriptXSize, hasScriptMetrics, location,
                SfntMvarValueTags.SuperscriptXSize),
            AddOptionalDelta(superscriptYSize, hasScriptMetrics, location,
                SfntMvarValueTags.SuperscriptYSize),
            AddOptionalDelta(superscriptXOffset, hasScriptMetrics, location,
                SfntMvarValueTags.SuperscriptXOffset),
            AddOptionalDelta(superscriptYOffset, hasScriptMetrics, location,
                SfntMvarValueTags.SuperscriptYOffset),
            hasCaret,
            AddOptionalDelta(caretSlopeRise, hasCaret, location,
                SfntMvarValueTags.HorizontalCaretRise),
            AddOptionalDelta(caretSlopeRun, hasCaret, location,
                SfntMvarValueTags.HorizontalCaretRun),
            AddOptionalDelta(caretOffset, hasCaret, location,
                SfntMvarValueTags.HorizontalCaretOffset),
            hasVertical,
            AddOptionalDelta(verticalAscender, hasVertical, location,
                SfntMvarValueTags.VerticalAscender),
            AddOptionalDelta(verticalDescender, hasVertical, location,
                SfntMvarValueTags.VerticalDescender),
            AddOptionalDelta(verticalLineGap, hasVertical, location,
                SfntMvarValueTags.VerticalLineGap),
            AddOptionalDelta(verticalCaretSlopeRise, hasVertical, location,
                SfntMvarValueTags.VerticalCaretRise),
            AddOptionalDelta(verticalCaretSlopeRun, hasVertical, location,
                SfntMvarValueTags.VerticalCaretRun),
            AddOptionalDelta(verticalCaretOffset, hasVertical, location,
                SfntMvarValueTags.VerticalCaretOffset),
            variedGaspMaximums,
            gaspRangeBehaviors);
    }

    /// <summary>Scales all coordinate metrics to a point size.</summary>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>A scaled immutable result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>pointSize</c> is negative or non-finite.</exception>
    public FontScaledMetrics Scale(double pointSize)
    {
        return new FontScaledMetrics(this, pointSize);
    }

    void EnsureCapAndXHeight()
    {
        if (!hasCapAndXHeight) { throw new System.InvalidOperationException("Cap height and x-height are unavailable."); }
    }

    void EnsureUnderline()
    {
        if (!hasUnderline) { throw new System.InvalidOperationException("Underline metrics are unavailable."); }
    }

    void EnsureStrikeout()
    {
        if (!hasStrikeout) { throw new System.InvalidOperationException("Strikeout metrics are unavailable."); }
    }

    void EnsureScriptMetrics()
    {
        if (!hasScriptMetrics) { throw new System.InvalidOperationException("Script metrics are unavailable."); }
    }

    void EnsureCaret()
    {
        if (!hasCaret) { throw new System.InvalidOperationException("Caret metrics are unavailable."); }
    }

    void EnsureVertical()
    {
        if (!hasVertical) { throw new System.InvalidOperationException("Vertical metrics are unavailable."); }
    }

    void ValidateGaspRangeIndex(int index)
    {
        if (index < 0 || index >= gaspRangeMaximumPpems.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    int AddOptionalDelta(
        int value,
        bool available,
        SfntMvarLocation location,
        int tag)
    {
        return available ? AddDelta(value, location.CalculateDelta(tag)) : value;
    }

    int AddDelta(int value, double delta)
    {
        double varied = value + delta;

        if (!double.IsFinite(varied) ||
            varied < int.MinValue || varied > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A varied global metric is outside the supported range.");
        }

        return (int)System.Math.Round(
            varied,
            System.MidpointRounding.AwayFromZero);
    }
}
