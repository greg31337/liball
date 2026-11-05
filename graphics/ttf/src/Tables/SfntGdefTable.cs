namespace ttf;

internal class SfntGdefAttachmentPoint
{
    int[] pointIndices;

    internal SfntGdefAttachmentPoint(int[] pointIndices)
    {
        this.pointIndices = pointIndices;
    }

    internal int GetPointCount()
    {
        return pointIndices.Length;
    }

    internal int GetPointIndex(int index)
    {
        if (index < 0 || index >= pointIndices.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return pointIndices[index];
    }
}

internal class SfntGdefAttachmentList
{
    SfntCoverageTable coverage;
    SfntGdefAttachmentPoint[] attachmentPoints;

    internal SfntGdefAttachmentList(
        SfntCoverageTable coverage,
        SfntGdefAttachmentPoint[] attachmentPoints)
    {
        this.coverage = coverage;
        this.attachmentPoints = attachmentPoints;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetGlyphCount()
    {
        return attachmentPoints.Length;
    }

    internal SfntGdefAttachmentPoint GetAttachmentPoint(int index)
    {
        if (index < 0 || index >= attachmentPoints.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return attachmentPoints[index];
    }

    internal SfntGdefAttachmentPoint? FindAttachmentPoint(int glyphId)
    {
        int index = coverage.GetCoverageIndex(glyphId);
        return index < 0 ? null : attachmentPoints[index];
    }
}

internal class SfntGdefCaretValue
{
    int format;
    int coordinate;
    int contourPointIndex;
    int adjustmentOffset;
    int adjustmentTableOffset;
    SfntDeviceOrVariationIndex? adjustment;

    internal SfntGdefCaretValue(
        int format,
        int coordinate,
        int contourPointIndex,
        int adjustmentOffset,
        int adjustmentTableOffset,
        SfntDeviceOrVariationIndex? adjustment)
    {
        this.format = format;
        this.coordinate = coordinate;
        this.contourPointIndex = contourPointIndex;
        this.adjustmentOffset = adjustmentOffset;
        this.adjustmentTableOffset = adjustmentTableOffset;
        this.adjustment = adjustment;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetCoordinate()
    {
        return coordinate;
    }

    internal bool HasContourPointIndex()
    {
        return format == 2;
    }

    internal int GetContourPointIndex()
    {
        return contourPointIndex;
    }

    internal int GetAdjustmentOffset()
    {
        return adjustmentOffset;
    }

    internal int GetAdjustmentTableOffset()
    {
        return adjustmentTableOffset;
    }

    internal SfntDeviceOrVariationIndex? GetAdjustment()
    {
        return adjustment;
    }
}

internal class SfntGdefLigatureGlyph
{
    SfntGdefCaretValue[] caretValues;

    internal SfntGdefLigatureGlyph(SfntGdefCaretValue[] caretValues)
    {
        this.caretValues = caretValues;
    }

    internal int GetCaretCount()
    {
        return caretValues.Length;
    }

    internal SfntGdefCaretValue GetCaretValue(int index)
    {
        if (index < 0 || index >= caretValues.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return caretValues[index];
    }
}

internal class SfntGdefLigatureCaretList
{
    SfntCoverageTable coverage;
    SfntGdefLigatureGlyph[] ligatureGlyphs;

    internal SfntGdefLigatureCaretList(
        SfntCoverageTable coverage,
        SfntGdefLigatureGlyph[] ligatureGlyphs)
    {
        this.coverage = coverage;
        this.ligatureGlyphs = ligatureGlyphs;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetGlyphCount()
    {
        return ligatureGlyphs.Length;
    }

    internal SfntGdefLigatureGlyph GetLigatureGlyph(int index)
    {
        if (index < 0 || index >= ligatureGlyphs.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return ligatureGlyphs[index];
    }

    internal SfntGdefLigatureGlyph? FindLigatureGlyph(int glyphId)
    {
        int index = coverage.GetCoverageIndex(glyphId);
        return index < 0 ? null : ligatureGlyphs[index];
    }
}

internal class SfntGdefMarkGlyphSets
{
    int format;
    SfntCoverageTable[] coverages;

    internal SfntGdefMarkGlyphSets(
        int format,
        SfntCoverageTable[] coverages)
    {
        this.format = format;
        this.coverages = coverages;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetSetCount()
    {
        return coverages.Length;
    }

    internal SfntCoverageTable GetCoverage(int index)
    {
        if (index < 0 || index >= coverages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return coverages[index];
    }

    internal bool Covers(int setIndex, int glyphId)
    {
        return GetCoverage(setIndex).Covers(glyphId);
    }
}

internal class SfntGdefTable
{
    int majorVersion;
    int minorVersion;
    SfntClassDefinitionTable? glyphClasses;
    SfntGdefAttachmentList? attachmentList;
    SfntGdefLigatureCaretList? ligatureCaretList;
    SfntClassDefinitionTable? markAttachmentClasses;
    SfntGdefMarkGlyphSets? markGlyphSets;
    SfntItemVariationStore? itemVariationStore;

    internal SfntGdefTable(
        int majorVersion,
        int minorVersion,
        SfntClassDefinitionTable? glyphClasses,
        SfntGdefAttachmentList? attachmentList,
        SfntGdefLigatureCaretList? ligatureCaretList,
        SfntClassDefinitionTable? markAttachmentClasses,
        SfntGdefMarkGlyphSets? markGlyphSets,
        SfntItemVariationStore? itemVariationStore)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.glyphClasses = glyphClasses;
        this.attachmentList = attachmentList;
        this.ligatureCaretList = ligatureCaretList;
        this.markAttachmentClasses = markAttachmentClasses;
        this.markGlyphSets = markGlyphSets;
        this.itemVariationStore = itemVariationStore;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal SfntClassDefinitionTable? GetGlyphClasses()
    {
        return glyphClasses;
    }

    internal int GetGlyphClass(int glyphId)
    {
        return glyphClasses == null ? 0 :
            glyphClasses.GetClassForGlyph(glyphId);
    }

    internal SfntGdefAttachmentList? GetAttachmentList()
    {
        return attachmentList;
    }

    internal SfntGdefLigatureCaretList? GetLigatureCaretList()
    {
        return ligatureCaretList;
    }

    internal SfntClassDefinitionTable? GetMarkAttachmentClasses()
    {
        return markAttachmentClasses;
    }

    internal int GetMarkAttachmentClass(int glyphId)
    {
        return markAttachmentClasses == null ? 0 :
            markAttachmentClasses.GetClassForGlyph(glyphId);
    }

    internal SfntGdefMarkGlyphSets? GetMarkGlyphSets()
    {
        return markGlyphSets;
    }

    internal SfntItemVariationStore? GetItemVariationStore()
    {
        return itemVariationStore;
    }
}
