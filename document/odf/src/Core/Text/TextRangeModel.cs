using System.Collections.Generic;

namespace odf;

internal class TextMarkerPosition
{
    internal OdfTextRun? Run;
    internal OdfTextBlock? Block;
    internal int Order;
}

internal static class TextRangeModel
{
    internal static void LoadTextRanges(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for text-range materialization.");
        }
        List<TextMarkerPosition> positions = new List<TextMarkerPosition>();
        int order = 0;
        int blockIndex = 0;
        while (blockIndex < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[blockIndex];
            order = CollectMarkerPositions(
                block.Runs,
                block,
                order,
                positions);
            blockIndex = blockIndex + 1;
        }
        List<OdfTextRange> ranges = new List<OdfTextRange>();
        AddPointRanges(positions, ranges);
        AddStartedRanges(positions, ranges);
        AddEndOnlyRanges(positions, ranges);
        document.TextRanges = ranges.ToArray();
    }

    static int CollectMarkerPositions(
        OdfTextRun[] runs,
        OdfTextBlock block,
        int order,
        List<TextMarkerPosition> positions)
    {
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (IsMarkerKind(run.Kind))
            {
                TextMarkerPosition position = new TextMarkerPosition();
                position.Run = run;
                position.Block = block;
                position.Order = order;
                positions.Add(position);
            }
            order = order + 1;
            order = CollectMarkerPositions(
                run.Runs,
                block,
                order,
                positions);
            index = index + 1;
        }
        return order;
    }

    static bool IsMarkerKind(OdfTextRunKind kind)
    {
        return kind >= OdfTextRunKind.Bookmark
            && kind <= OdfTextRunKind.BibliographyMark;
    }

    static void AddPointRanges(
        List<TextMarkerPosition> positions,
        List<OdfTextRange> ranges)
    {
        int index = 0;
        while (index < positions.Count)
        {
            TextMarkerPosition position = positions[index];
            if (position.Run != null && IsPointKind(position.Run.Kind))
            {
                OdfTextRange range = CreateRangeFromPosition(position);
                range.IsRange = false;
                range.IsResolved = true;
                range.EndNode = range.StartNode;
                range.EndRun = range.StartRun;
                range.EndBlock = range.StartBlock;
                ranges.Add(range);
            }
            index = index + 1;
        }
    }

    static bool IsPointKind(OdfTextRunKind kind)
    {
        return kind == OdfTextRunKind.Bookmark
            || kind == OdfTextRunKind.ReferenceMark
            || kind == OdfTextRunKind.TableOfContentMark
            || kind == OdfTextRunKind.UserIndexMark
            || kind == OdfTextRunKind.AlphabeticalIndexMark
            || kind == OdfTextRunKind.BibliographyMark;
    }

    static void AddStartedRanges(
        List<TextMarkerPosition> positions,
        List<OdfTextRange> ranges)
    {
        int index = 0;
        while (index < positions.Count)
        {
            TextMarkerPosition start = positions[index];
            if (start.Run != null && IsStartKind(start.Run.Kind))
            {
                OdfTextRange range = CreateRangeFromPosition(start);
                range.IsRange = true;
                int startCount = CountMatchingStarts(
                    positions,
                    range.Kind,
                    range.Name);
                int endCount = CountMatchingEnds(
                    positions,
                    range.Kind,
                    range.Name);
                range.HasDuplicateEndpoint = startCount > 1 || endCount > 1;
                TextMarkerPosition? end = FindMatchingEnd(
                    positions,
                    range.Kind,
                    range.Name);
                if (end != null)
                {
                    range.EndRun = end.Run;
                    range.EndBlock = end.Block;
                    if (end.Run != null)
                    {
                        range.EndNode = end.Run.Node;
                    }
                    range.IsReversed = end.Order < start.Order;
                    range.IsResolved = !range.HasDuplicateEndpoint
                        && !range.IsReversed
                        && start.Run.Node != null
                        && end.Run != null
                        && end.Run.Node != null
                        && start.Block != null
                        && end.Block != null
                        && start.Block.PartPath == end.Block.PartPath;
                }
                ranges.Add(range);
            }
            index = index + 1;
        }
    }

    static bool IsStartKind(OdfTextRunKind kind)
    {
        return kind == OdfTextRunKind.BookmarkStart
            || kind == OdfTextRunKind.ReferenceMarkStart
            || kind == OdfTextRunKind.TableOfContentMarkStart
            || kind == OdfTextRunKind.UserIndexMarkStart
            || kind == OdfTextRunKind.AlphabeticalIndexMarkStart;
    }

    static void AddEndOnlyRanges(
        List<TextMarkerPosition> positions,
        List<OdfTextRange> ranges)
    {
        int index = 0;
        while (index < positions.Count)
        {
            TextMarkerPosition end = positions[index];
            if (end.Run != null && IsEndKind(end.Run.Kind))
            {
                OdfTextRangeKind kind = RangeKindForRun(end.Run.Kind);
                if (CountMatchingStarts(
                    positions,
                    kind,
                    end.Run.MarkerName) == 0)
                {
                    OdfTextRange range = new OdfTextRange();
                    range.Kind = kind;
                    range.Name = end.Run.MarkerName;
                    range.IsRange = true;
                    range.EndRun = end.Run;
                    range.EndNode = end.Run.Node;
                    range.EndBlock = end.Block;
                    range.PartPath = end.Block == null
                        ? ""
                        : end.Block.PartPath;
                    range.HasDuplicateEndpoint = CountMatchingEnds(
                        positions,
                        kind,
                        range.Name) > 1;
                    ranges.Add(range);
                }
            }
            index = index + 1;
        }
    }

    static bool IsEndKind(OdfTextRunKind kind)
    {
        return kind == OdfTextRunKind.BookmarkEnd
            || kind == OdfTextRunKind.ReferenceMarkEnd
            || kind == OdfTextRunKind.TableOfContentMarkEnd
            || kind == OdfTextRunKind.UserIndexMarkEnd
            || kind == OdfTextRunKind.AlphabeticalIndexMarkEnd;
    }

    static OdfTextRange CreateRangeFromPosition(
        TextMarkerPosition position)
    {
        OdfTextRange result = new OdfTextRange();
        if (position.Run == null)
        {
            return result;
        }
        result.Kind = RangeKindForRun(position.Run.Kind);
        result.Name = position.Run.MarkerName.Length > 0
            ? position.Run.MarkerName
            : position.Run.StringValue;
        result.StartRun = position.Run;
        result.StartNode = position.Run.Node;
        result.StartBlock = position.Block;
        if (position.Block != null)
        {
            result.PartPath = position.Block.PartPath;
        }
        if (position.Run.Node != null)
        {
            result.XmlId = StyleDocumentModel.ReadAttribute(
                position.Run.Node,
                OdfNamespaces.Xml,
                "id");
            if (result.XmlId.Length == 0)
            {
                result.XmlId = position.Run.Node.XmlId;
            }
        }
        return result;
    }

    static OdfTextRangeKind RangeKindForRun(OdfTextRunKind kind)
    {
        if (kind == OdfTextRunKind.Bookmark
            || kind == OdfTextRunKind.BookmarkStart
            || kind == OdfTextRunKind.BookmarkEnd)
        {
            return OdfTextRangeKind.Bookmark;
        }
        if (kind == OdfTextRunKind.ReferenceMark
            || kind == OdfTextRunKind.ReferenceMarkStart
            || kind == OdfTextRunKind.ReferenceMarkEnd)
        {
            return OdfTextRangeKind.ReferenceMark;
        }
        if (kind == OdfTextRunKind.TableOfContentMark
            || kind == OdfTextRunKind.TableOfContentMarkStart
            || kind == OdfTextRunKind.TableOfContentMarkEnd)
        {
            return OdfTextRangeKind.TableOfContentMark;
        }
        if (kind == OdfTextRunKind.UserIndexMark
            || kind == OdfTextRunKind.UserIndexMarkStart
            || kind == OdfTextRunKind.UserIndexMarkEnd)
        {
            return OdfTextRangeKind.UserIndexMark;
        }
        if (kind == OdfTextRunKind.AlphabeticalIndexMark
            || kind == OdfTextRunKind.AlphabeticalIndexMarkStart
            || kind == OdfTextRunKind.AlphabeticalIndexMarkEnd)
        {
            return OdfTextRangeKind.AlphabeticalIndexMark;
        }
        if (kind == OdfTextRunKind.BibliographyMark)
        {
            return OdfTextRangeKind.BibliographyMark;
        }
        return OdfTextRangeKind.Unknown;
    }

    static int CountMatchingStarts(
        List<TextMarkerPosition> positions,
        OdfTextRangeKind kind,
        string name)
    {
        int result = 0;
        int index = 0;
        while (index < positions.Count)
        {
            OdfTextRun? run = positions[index].Run;
            if (run != null
                && IsStartKind(run.Kind)
                && RangeKindForRun(run.Kind) == kind
                && run.MarkerName == name)
            {
                result = result + 1;
            }
            index = index + 1;
        }
        return result;
    }

    static int CountMatchingEnds(
        List<TextMarkerPosition> positions,
        OdfTextRangeKind kind,
        string name)
    {
        int result = 0;
        int index = 0;
        while (index < positions.Count)
        {
            OdfTextRun? run = positions[index].Run;
            if (run != null
                && IsEndKind(run.Kind)
                && RangeKindForRun(run.Kind) == kind
                && run.MarkerName == name)
            {
                result = result + 1;
            }
            index = index + 1;
        }
        return result;
    }

    static TextMarkerPosition? FindMatchingEnd(
        List<TextMarkerPosition> positions,
        OdfTextRangeKind kind,
        string name)
    {
        int index = 0;
        while (index < positions.Count)
        {
            TextMarkerPosition position = positions[index];
            OdfTextRun? run = position.Run;
            if (run != null
                && IsEndKind(run.Kind)
                && RangeKindForRun(run.Kind) == kind
                && run.MarkerName == name)
            {
                return position;
            }
            index = index + 1;
        }
        return null;
    }
}
