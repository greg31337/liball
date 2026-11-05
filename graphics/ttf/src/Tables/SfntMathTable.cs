namespace ttf;

internal enum SfntMathConstant
{
    MathLeading = 0,
    AxisHeight = 1,
    AccentBaseHeight = 2,
    FlattenedAccentBaseHeight = 3,
    SubscriptShiftDown = 4,
    SubscriptTopMax = 5,
    SubscriptBaselineDropMin = 6,
    SuperscriptShiftUp = 7,
    SuperscriptShiftUpCramped = 8,
    SuperscriptBottomMin = 9,
    SuperscriptBaselineDropMax = 10,
    SubSuperscriptGapMin = 11,
    SuperscriptBottomMaxWithSubscript = 12,
    SpaceAfterScript = 13,
    UpperLimitGapMin = 14,
    UpperLimitBaselineRiseMin = 15,
    LowerLimitGapMin = 16,
    LowerLimitBaselineDropMin = 17,
    StackTopShiftUp = 18,
    StackTopDisplayStyleShiftUp = 19,
    StackBottomShiftDown = 20,
    StackBottomDisplayStyleShiftDown = 21,
    StackGapMin = 22,
    StackDisplayStyleGapMin = 23,
    StretchStackTopShiftUp = 24,
    StretchStackBottomShiftDown = 25,
    StretchStackGapAboveMin = 26,
    StretchStackGapBelowMin = 27,
    FractionNumeratorShiftUp = 28,
    FractionNumeratorDisplayStyleShiftUp = 29,
    FractionDenominatorShiftDown = 30,
    FractionDenominatorDisplayStyleShiftDown = 31,
    FractionNumeratorGapMin = 32,
    FractionNumeratorDisplayStyleGapMin = 33,
    FractionRuleThickness = 34,
    FractionDenominatorGapMin = 35,
    FractionDenominatorDisplayStyleGapMin = 36,
    SkewedFractionHorizontalGap = 37,
    SkewedFractionVerticalGap = 38,
    OverbarVerticalGap = 39,
    OverbarRuleThickness = 40,
    OverbarExtraAscender = 41,
    UnderbarVerticalGap = 42,
    UnderbarRuleThickness = 43,
    UnderbarExtraDescender = 44,
    RadicalVerticalGap = 45,
    RadicalDisplayStyleVerticalGap = 46,
    RadicalRuleThickness = 47,
    RadicalExtraAscender = 48,
    RadicalKernBeforeDegree = 49,
    RadicalKernAfterDegree = 50
}

internal class SfntMathValue
{
    int value;
    int deviceOffset;
    int deviceTableOffset;
    SfntDeviceOrVariationIndex? adjustment;

    internal SfntMathValue(
        int value,
        int deviceOffset,
        int deviceTableOffset,
        SfntDeviceOrVariationIndex? adjustment)
    {
        this.value = value;
        this.deviceOffset = deviceOffset;
        this.deviceTableOffset = deviceTableOffset;
        this.adjustment = adjustment;
    }

    internal int GetValue()
    {
        return value;
    }

    internal bool HasAdjustment()
    {
        return adjustment != null;
    }

    internal int GetDeviceOffset()
    {
        return deviceOffset;
    }

    internal int GetDeviceTableOffset()
    {
        return deviceTableOffset;
    }

    internal SfntDeviceOrVariationIndex? GetAdjustment()
    {
        return adjustment;
    }
}

internal class SfntMathConstants
{
    int scriptPercentScaleDown;
    int scriptScriptPercentScaleDown;
    int delimitedSubFormulaMinimumHeight;
    int displayOperatorMinimumHeight;
    SfntMathValue[] values;
    int radicalDegreeBottomRaisePercent;

    internal SfntMathConstants(
        int scriptPercentScaleDown,
        int scriptScriptPercentScaleDown,
        int delimitedSubFormulaMinimumHeight,
        int displayOperatorMinimumHeight,
        SfntMathValue[] values,
        int radicalDegreeBottomRaisePercent)
    {
        this.scriptPercentScaleDown = scriptPercentScaleDown;
        this.scriptScriptPercentScaleDown =
            scriptScriptPercentScaleDown;
        this.delimitedSubFormulaMinimumHeight =
            delimitedSubFormulaMinimumHeight;
        this.displayOperatorMinimumHeight =
            displayOperatorMinimumHeight;
        this.values = values;
        this.radicalDegreeBottomRaisePercent =
            radicalDegreeBottomRaisePercent;
    }

    internal int GetScriptPercentScaleDown()
    {
        return scriptPercentScaleDown;
    }

    internal int GetScriptScriptPercentScaleDown()
    {
        return scriptScriptPercentScaleDown;
    }

    internal int GetDelimitedSubFormulaMinimumHeight()
    {
        return delimitedSubFormulaMinimumHeight;
    }

    internal int GetDisplayOperatorMinimumHeight()
    {
        return displayOperatorMinimumHeight;
    }

    internal int GetValueCount()
    {
        return values.Length;
    }

    internal SfntMathValue GetValue(SfntMathConstant constant)
    {
        int index = (int)constant;

        if (index < 0 || index >= values.Length)
        {
            throw new System.ArgumentOutOfRangeException("constant");
        }

        return values[index];
    }

    internal int GetRadicalDegreeBottomRaisePercent()
    {
        return radicalDegreeBottomRaisePercent;
    }
}

internal class SfntMathGlyphValueTable
{
    SfntCoverageTable coverage;
    SfntMathValue[] values;

    internal SfntMathGlyphValueTable(
        SfntCoverageTable coverage,
        SfntMathValue[] values)
    {
        this.coverage = coverage;
        this.values = values;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetValueCount()
    {
        return values.Length;
    }

    internal SfntMathValue GetValue(int index)
    {
        if (index < 0 || index >= values.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return values[index];
    }

    internal SfntMathValue? FindValue(int glyphId)
    {
        int index = coverage.GetCoverageIndex(glyphId);
        return index < 0 ? null : values[index];
    }
}

internal class SfntMathKern
{
    SfntMathValue[] correctionHeights;
    SfntMathValue[] kernValues;

    internal SfntMathKern(
        SfntMathValue[] correctionHeights,
        SfntMathValue[] kernValues)
    {
        this.correctionHeights = correctionHeights;
        this.kernValues = kernValues;
    }

    internal int GetCorrectionHeightCount()
    {
        return correctionHeights.Length;
    }

    internal SfntMathValue GetCorrectionHeight(int index)
    {
        if (index < 0 || index >= correctionHeights.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return correctionHeights[index];
    }

    internal int GetKernValueCount()
    {
        return kernValues.Length;
    }

    internal SfntMathValue GetKernValue(int index)
    {
        if (index < 0 || index >= kernValues.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return kernValues[index];
    }
}

internal class SfntMathKernRecord
{
    SfntMathKern? topRight;
    SfntMathKern? topLeft;
    SfntMathKern? bottomRight;
    SfntMathKern? bottomLeft;

    internal SfntMathKernRecord(
        SfntMathKern? topRight,
        SfntMathKern? topLeft,
        SfntMathKern? bottomRight,
        SfntMathKern? bottomLeft)
    {
        this.topRight = topRight;
        this.topLeft = topLeft;
        this.bottomRight = bottomRight;
        this.bottomLeft = bottomLeft;
    }

    internal SfntMathKern? GetTopRight()
    {
        return topRight;
    }

    internal SfntMathKern? GetTopLeft()
    {
        return topLeft;
    }

    internal SfntMathKern? GetBottomRight()
    {
        return bottomRight;
    }

    internal SfntMathKern? GetBottomLeft()
    {
        return bottomLeft;
    }
}

internal class SfntMathKernInfo
{
    SfntCoverageTable coverage;
    SfntMathKernRecord[] records;

    internal SfntMathKernInfo(
        SfntCoverageTable coverage,
        SfntMathKernRecord[] records)
    {
        this.coverage = coverage;
        this.records = records;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetRecordCount()
    {
        return records.Length;
    }

    internal SfntMathKernRecord GetRecord(int index)
    {
        if (index < 0 || index >= records.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return records[index];
    }

    internal SfntMathKernRecord? FindRecord(int glyphId)
    {
        int index = coverage.GetCoverageIndex(glyphId);
        return index < 0 ? null : records[index];
    }
}

internal class SfntMathGlyphInfo
{
    SfntMathGlyphValueTable? italicsCorrections;
    SfntMathGlyphValueTable? topAccentAttachments;
    SfntCoverageTable? extendedShapeCoverage;
    SfntMathKernInfo? kernInfo;

    internal SfntMathGlyphInfo(
        SfntMathGlyphValueTable? italicsCorrections,
        SfntMathGlyphValueTable? topAccentAttachments,
        SfntCoverageTable? extendedShapeCoverage,
        SfntMathKernInfo? kernInfo)
    {
        this.italicsCorrections = italicsCorrections;
        this.topAccentAttachments = topAccentAttachments;
        this.extendedShapeCoverage = extendedShapeCoverage;
        this.kernInfo = kernInfo;
    }

    internal SfntMathGlyphValueTable? GetItalicsCorrections()
    {
        return italicsCorrections;
    }

    internal SfntMathGlyphValueTable? GetTopAccentAttachments()
    {
        return topAccentAttachments;
    }

    internal SfntCoverageTable? GetExtendedShapeCoverage()
    {
        return extendedShapeCoverage;
    }

    internal bool IsExtendedShape(int glyphId)
    {
        return extendedShapeCoverage != null &&
            extendedShapeCoverage.Covers(glyphId);
    }

    internal SfntMathKernInfo? GetKernInfo()
    {
        return kernInfo;
    }
}

internal class SfntMathGlyphVariant
{
    int glyphId;
    int advanceMeasurement;

    internal SfntMathGlyphVariant(
        int glyphId,
        int advanceMeasurement)
    {
        this.glyphId = glyphId;
        this.advanceMeasurement = advanceMeasurement;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetAdvanceMeasurement()
    {
        return advanceMeasurement;
    }
}

internal class SfntMathGlyphPart
{
    int glyphId;
    int startConnectorLength;
    int endConnectorLength;
    int fullAdvance;
    int flags;

    internal SfntMathGlyphPart(
        int glyphId,
        int startConnectorLength,
        int endConnectorLength,
        int fullAdvance,
        int flags)
    {
        this.glyphId = glyphId;
        this.startConnectorLength = startConnectorLength;
        this.endConnectorLength = endConnectorLength;
        this.fullAdvance = fullAdvance;
        this.flags = flags;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetStartConnectorLength()
    {
        return startConnectorLength;
    }

    internal int GetEndConnectorLength()
    {
        return endConnectorLength;
    }

    internal int GetFullAdvance()
    {
        return fullAdvance;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal bool IsExtender()
    {
        return (flags & 1) != 0;
    }
}

internal class SfntMathGlyphAssembly
{
    SfntMathValue italicsCorrection;
    SfntMathGlyphPart[] parts;

    internal SfntMathGlyphAssembly(
        SfntMathValue italicsCorrection,
        SfntMathGlyphPart[] parts)
    {
        this.italicsCorrection = italicsCorrection;
        this.parts = parts;
    }

    internal SfntMathValue GetItalicsCorrection()
    {
        return italicsCorrection;
    }

    internal int GetPartCount()
    {
        return parts.Length;
    }

    internal SfntMathGlyphPart GetPart(int index)
    {
        if (index < 0 || index >= parts.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return parts[index];
    }
}

internal class SfntMathGlyphConstruction
{
    SfntMathGlyphAssembly? assembly;
    SfntMathGlyphVariant[] variants;

    internal SfntMathGlyphConstruction(
        SfntMathGlyphAssembly? assembly,
        SfntMathGlyphVariant[] variants)
    {
        this.assembly = assembly;
        this.variants = variants;
    }

    internal SfntMathGlyphAssembly? GetAssembly()
    {
        return assembly;
    }

    internal int GetVariantCount()
    {
        return variants.Length;
    }

    internal SfntMathGlyphVariant GetVariant(int index)
    {
        if (index < 0 || index >= variants.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return variants[index];
    }
}

internal class SfntMathVariants
{
    int minimumConnectorOverlap;
    SfntCoverageTable verticalCoverage;
    SfntCoverageTable horizontalCoverage;
    SfntMathGlyphConstruction[] verticalConstructions;
    SfntMathGlyphConstruction[] horizontalConstructions;

    internal SfntMathVariants(
        int minimumConnectorOverlap,
        SfntCoverageTable verticalCoverage,
        SfntCoverageTable horizontalCoverage,
        SfntMathGlyphConstruction[] verticalConstructions,
        SfntMathGlyphConstruction[] horizontalConstructions)
    {
        this.minimumConnectorOverlap = minimumConnectorOverlap;
        this.verticalCoverage = verticalCoverage;
        this.horizontalCoverage = horizontalCoverage;
        this.verticalConstructions = verticalConstructions;
        this.horizontalConstructions = horizontalConstructions;
    }

    internal int GetMinimumConnectorOverlap()
    {
        return minimumConnectorOverlap;
    }

    internal SfntCoverageTable GetVerticalCoverage()
    {
        return verticalCoverage;
    }

    internal SfntCoverageTable GetHorizontalCoverage()
    {
        return horizontalCoverage;
    }

    internal int GetVerticalConstructionCount()
    {
        return verticalConstructions.Length;
    }

    internal SfntMathGlyphConstruction GetVerticalConstruction(int index)
    {
        if (index < 0 || index >= verticalConstructions.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return verticalConstructions[index];
    }

    internal SfntMathGlyphConstruction? FindVerticalConstruction(
        int glyphId)
    {
        int index = verticalCoverage.GetCoverageIndex(glyphId);
        return index < 0 ? null : verticalConstructions[index];
    }

    internal int GetHorizontalConstructionCount()
    {
        return horizontalConstructions.Length;
    }

    internal SfntMathGlyphConstruction GetHorizontalConstruction(int index)
    {
        if (index < 0 || index >= horizontalConstructions.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return horizontalConstructions[index];
    }

    internal SfntMathGlyphConstruction? FindHorizontalConstruction(
        int glyphId)
    {
        int index = horizontalCoverage.GetCoverageIndex(glyphId);
        return index < 0 ? null : horizontalConstructions[index];
    }
}

internal class SfntMathTable
{
    int majorVersion;
    int minorVersion;
    SfntMathConstants constants;
    SfntMathGlyphInfo glyphInfo;
    SfntMathVariants variants;

    internal SfntMathTable(
        int majorVersion,
        int minorVersion,
        SfntMathConstants constants,
        SfntMathGlyphInfo glyphInfo,
        SfntMathVariants variants)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.constants = constants;
        this.glyphInfo = glyphInfo;
        this.variants = variants;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal SfntMathConstants GetConstants()
    {
        return constants;
    }

    internal SfntMathGlyphInfo GetGlyphInfo()
    {
        return glyphInfo;
    }

    internal SfntMathVariants GetVariants()
    {
        return variants;
    }
}
