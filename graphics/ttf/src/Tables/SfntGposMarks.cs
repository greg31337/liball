namespace ttf;

internal class SfntGposAnchorMatrix
{
    int recordCount;
    int markClassCount;
    SfntLayoutAnchor?[] anchors;

    internal SfntGposAnchorMatrix(
        int recordCount,
        int markClassCount,
        SfntLayoutAnchor?[] anchors)
    {
        this.recordCount = recordCount;
        this.markClassCount = markClassCount;
        this.anchors = anchors;
    }

    internal int GetRecordCount()
    {
        return recordCount;
    }

    internal int GetMarkClassCount()
    {
        return markClassCount;
    }

    internal SfntLayoutAnchor? GetAnchor(
        int recordIndex,
        int markClass)
    {
        if (recordIndex < 0 || recordIndex >= recordCount)
        {
            throw new System.ArgumentOutOfRangeException("recordIndex");
        }

        if (markClass < 0 || markClass >= markClassCount)
        {
            throw new System.ArgumentOutOfRangeException("markClass");
        }

        return anchors[recordIndex * markClassCount + markClass];
    }
}

internal class SfntGposMarkAttachment
{
    SfntCoverageTable markCoverage;
    SfntCoverageTable baseCoverage;
    int markClassCount;
    SfntMarkArray markArray;
    SfntGposAnchorMatrix baseAnchors;

    internal SfntGposMarkAttachment(
        SfntCoverageTable markCoverage,
        SfntCoverageTable baseCoverage,
        int markClassCount,
        SfntMarkArray markArray,
        SfntGposAnchorMatrix baseAnchors)
    {
        this.markCoverage = markCoverage;
        this.baseCoverage = baseCoverage;
        this.markClassCount = markClassCount;
        this.markArray = markArray;
        this.baseAnchors = baseAnchors;
    }

    internal SfntCoverageTable GetMarkCoverage()
    {
        return markCoverage;
    }

    internal SfntCoverageTable GetBaseCoverage()
    {
        return baseCoverage;
    }

    internal int GetMarkClassCount()
    {
        return markClassCount;
    }

    internal SfntMarkArray GetMarkArray()
    {
        return markArray;
    }

    internal SfntGposAnchorMatrix GetBaseAnchors()
    {
        return baseAnchors;
    }

    internal SfntMarkRecord? FindMarkRecord(int glyphId)
    {
        int coverageIndex = markCoverage.GetCoverageIndex(glyphId);

        if (coverageIndex < 0)
        {
            return null;
        }

        return markArray.GetMark(coverageIndex);
    }

    internal SfntLayoutAnchor? FindBaseAnchor(
        int glyphId,
        int markClass)
    {
        int coverageIndex = baseCoverage.GetCoverageIndex(glyphId);

        if (coverageIndex < 0)
        {
            return null;
        }

        return baseAnchors.GetAnchor(coverageIndex, markClass);
    }
}

internal class SfntGposLigatureAttach
{
    int componentCount;
    int markClassCount;
    SfntLayoutAnchor?[] anchors;

    internal SfntGposLigatureAttach(
        int componentCount,
        int markClassCount,
        SfntLayoutAnchor?[] anchors)
    {
        this.componentCount = componentCount;
        this.markClassCount = markClassCount;
        this.anchors = anchors;
    }

    internal int GetComponentCount()
    {
        return componentCount;
    }

    internal SfntLayoutAnchor? GetAnchor(
        int componentIndex,
        int markClass)
    {
        if (componentIndex < 0 || componentIndex >= componentCount)
        {
            throw new System.ArgumentOutOfRangeException("componentIndex");
        }

        if (markClass < 0 || markClass >= markClassCount)
        {
            throw new System.ArgumentOutOfRangeException("markClass");
        }

        return anchors[componentIndex * markClassCount + markClass];
    }
}

internal class SfntGposMarkLigatureAdjustment
{
    SfntCoverageTable markCoverage;
    SfntCoverageTable ligatureCoverage;
    int markClassCount;
    SfntMarkArray markArray;
    SfntGposLigatureAttach[] ligatureAttachments;

    internal SfntGposMarkLigatureAdjustment(
        SfntCoverageTable markCoverage,
        SfntCoverageTable ligatureCoverage,
        int markClassCount,
        SfntMarkArray markArray,
        SfntGposLigatureAttach[] ligatureAttachments)
    {
        this.markCoverage = markCoverage;
        this.ligatureCoverage = ligatureCoverage;
        this.markClassCount = markClassCount;
        this.markArray = markArray;
        this.ligatureAttachments = ligatureAttachments;
    }

    internal SfntCoverageTable GetMarkCoverage()
    {
        return markCoverage;
    }

    internal SfntCoverageTable GetLigatureCoverage()
    {
        return ligatureCoverage;
    }

    internal int GetMarkClassCount()
    {
        return markClassCount;
    }

    internal SfntMarkArray GetMarkArray()
    {
        return markArray;
    }

    internal int GetLigatureCount()
    {
        return ligatureAttachments.Length;
    }

    internal SfntMarkRecord? FindMarkRecord(int glyphId)
    {
        int coverageIndex = markCoverage.GetCoverageIndex(glyphId);

        if (coverageIndex < 0)
        {
            return null;
        }

        return markArray.GetMark(coverageIndex);
    }

    internal SfntGposLigatureAttach? FindLigatureAttach(int glyphId)
    {
        int coverageIndex = ligatureCoverage.GetCoverageIndex(glyphId);

        if (coverageIndex < 0)
        {
            return null;
        }

        return ligatureAttachments[coverageIndex];
    }
}
