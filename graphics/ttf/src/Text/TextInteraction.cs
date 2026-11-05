namespace ttf;

internal class TextLineCaretPosition
{
    int clusterBoundary;
    int groupIndex;
    double x;
    double y;

    internal TextLineCaretPosition(
        int clusterBoundary,
        int groupIndex,
        double x,
        double y)
    {
        this.clusterBoundary = clusterBoundary;
        this.groupIndex = groupIndex;
        this.x = x;
        this.y = y;
    }

    internal int GetClusterBoundary() { return clusterBoundary; }
    internal int GetGroupIndex() { return groupIndex; }
    internal double GetX() { return x; }
    internal double GetY() { return y; }
}

internal class TextCaretStop
{
    int lineIndex;
    int clusterBoundary;
    int utf16Index;
    int groupIndex;
    double x;
    double y;

    internal TextCaretStop(
        int lineIndex,
        int clusterBoundary,
        int utf16Index,
        int groupIndex,
        double x,
        double y)
    {
        this.lineIndex = lineIndex;
        this.clusterBoundary = clusterBoundary;
        this.utf16Index = utf16Index;
        this.groupIndex = groupIndex;
        this.x = x;
        this.y = y;
    }

    internal int GetLineIndex() { return lineIndex; }
    internal int GetClusterBoundary() { return clusterBoundary; }
    internal int GetUtf16Index() { return utf16Index; }
    internal double GetX() { return x; }
    internal double GetY() { return y; }
    internal int GetGroupIndex() { return groupIndex; }
}

internal class TextHitTestData
{
    TextCaretStop caretStop;
    double distance;

    internal TextHitTestData(TextCaretStop caretStop, double distance)
    {
        this.caretStop = caretStop;
        this.distance = distance;
    }

    internal TextCaretStop GetCaretStop() { return caretStop; }
    internal int GetLineIndex() { return caretStop.GetLineIndex(); }
    internal int GetClusterBoundary() { return caretStop.GetClusterBoundary(); }
    internal int GetUtf16Index() { return caretStop.GetUtf16Index(); }
    internal double GetX() { return caretStop.GetX(); }
    internal double GetY() { return caretStop.GetY(); }
    internal double GetDistance() { return distance; }
}

internal class TextSelectionRectangle
{
    int lineIndex;
    int utf16Start;
    int utf16Length;
    double x;
    double y;
    double width;
    double height;

    internal TextSelectionRectangle(
        int lineIndex,
        int utf16Start,
        int utf16Length,
        double x,
        double y,
        double width,
        double height)
    {
        this.lineIndex = lineIndex;
        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    internal int GetLineIndex() { return lineIndex; }
    internal int GetUtf16Start() { return utf16Start; }
    internal int GetUtf16Length() { return utf16Length; }
    internal double GetX() { return x; }
    internal double GetY() { return y; }
    internal double GetWidth() { return width; }
    internal double GetHeight() { return height; }
}

internal static class TextInteraction
{
    internal static TextCaretStop[] BuildCaretStops(
        TextLine[] lines,
        TextBreakMap breaks)
    {
        return BuildCaretStopsCore(lines, breaks, true);
    }

    static TextCaretStop[] BuildCaretStopsCore(
        TextLine[] lines,
        TextBreakMap breaks,
        bool mergeCoincident)
    {
        System.Collections.Generic.List<TextCaretStop> stops =
            new System.Collections.Generic.List<TextCaretStop>();
        int lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            TextLine line = lines[lineIndex];
            int index = 0;
            while (index < line.GetCaretPositionCount())
            {
                TextLineCaretPosition position = line.GetCaretPosition(index);
                TextCaretStop stop = new TextCaretStop(
                    lineIndex,
                    position.GetClusterBoundary(),
                    GetUtf16Boundary(
                        breaks,
                        position.GetClusterBoundary()),
                    position.GetGroupIndex(),
                    position.GetX(),
                    position.GetY());
                if (!Contains(stops, stop, mergeCoincident))
                {
                    stops.Add(stop);
                }

                index++;
            }

            lineIndex++;
        }

        return stops.ToArray();
    }

    internal static TextHitTestData HitTest(
        TextCaretStop[] stops,
        TextLine[] lines,
        TextWritingMode writingMode,
        double x,
        double y)
    {
        if (!double.IsFinite(x))
        {
            throw new System.ArgumentOutOfRangeException("x");
        }

        if (!double.IsFinite(y))
        {
            throw new System.ArgumentOutOfRangeException("y");
        }

        if (stops.Length == 0)
        {
            throw new System.InvalidOperationException(
                "The text layout has no caret stops.");
        }

        int nearestLine = GetNearestLine(lines, writingMode, x, y);
        TextCaretStop nearest = GetFirstStopOnLine(stops, nearestLine);
        double nearestSquared = GetSquaredDistance(nearest, x, y);
        int index = 0;
        while (index < stops.Length)
        {
            TextCaretStop candidate = stops[index];
            if (candidate.GetLineIndex() == nearestLine)
            {
                double squared = GetSquaredDistance(candidate, x, y);
                if (squared < nearestSquared ||
                    (squared == nearestSquared &&
                        ComesBefore(candidate, nearest)))
                {
                    nearest = candidate;
                    nearestSquared = squared;
                }
            }

            index++;
        }

        return new TextHitTestData(nearest, System.Math.Sqrt(nearestSquared));
    }

    static int GetNearestLine(
        TextLine[] lines,
        TextWritingMode writingMode,
        double x,
        double y)
    {
        int nearest = 0;
        double nearestDistance = GetCrossDistance(
            lines[0],
            writingMode,
            x,
            y);
        int index = 1;
        while (index < lines.Length)
        {
            double distance = GetCrossDistance(
                lines[index],
                writingMode,
                x,
                y);
            if (distance < nearestDistance)
            {
                nearest = index;
                nearestDistance = distance;
            }

            index++;
        }

        return nearest;
    }

    static double GetCrossDistance(
        TextLine line,
        TextWritingMode writingMode,
        double x,
        double y)
    {
        return writingMode == TextWritingMode.Horizontal ?
            System.Math.Abs(line.GetBaseline() - y) :
            System.Math.Abs(line.GetBaseline() - x);
    }

    static TextCaretStop GetFirstStopOnLine(
        TextCaretStop[] stops,
        int lineIndex)
    {
        int index = 0;
        while (index < stops.Length)
        {
            if (stops[index].GetLineIndex() == lineIndex)
            {
                return stops[index];
            }

            index++;
        }

        throw new System.InvalidOperationException(
            "A laid out line has no caret stop.");
    }

    internal static TextSelectionRectangle[] GetSelection(
        TextLine[] lines,
        TextCaretStop[] stops,
        TextBreakMap breaks,
        int utf16Start,
        int utf16Length,
        int sourceLength)
    {
        if (utf16Start < 0 || utf16Start > sourceLength)
        {
            throw new System.ArgumentOutOfRangeException("utf16Start");
        }

        if (utf16Length < 0 || utf16Length > sourceLength - utf16Start)
        {
            throw new System.ArgumentOutOfRangeException("utf16Length");
        }

        int utf16End = utf16Start + utf16Length;
        if (!HasUtf16Boundary(stops, utf16Start) ||
            !HasUtf16Boundary(stops, utf16End))
        {
            throw new System.ArgumentException(
                "Selection endpoints must be safe visible caret boundaries.",
                "utf16Start");
        }

        if (utf16Length == 0)
        {
            return new TextSelectionRectangle[0];
        }

        System.Collections.Generic.List<TextSelectionRectangle> rectangles =
            new System.Collections.Generic.List<TextSelectionRectangle>();
        TextCaretStop[] selectionStops = BuildCaretStopsCore(
            lines,
            breaks,
            false);
        int lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            AddLineSelection(
                rectangles,
                lines[lineIndex],
                selectionStops,
                breaks,
                lineIndex,
                utf16Start,
                utf16End);
            lineIndex++;
        }

        SortRectangles(rectangles);
        return MergeRectangles(rectangles);
    }

    static void AddLineSelection(
        System.Collections.Generic.List<TextSelectionRectangle> rectangles,
        TextLine line,
        TextCaretStop[] stops,
        TextBreakMap breaks,
        int lineIndex,
        int selectionStart,
        int selectionEnd)
    {
        int index = 0;
        while (index < stops.Length)
        {
            TextCaretStop start = stops[index];
            if (start.GetLineIndex() == lineIndex)
            {
                TextCaretStop? end = FindNextInGroup(stops, start);
                if (end != null)
                {
                    int utf16Start = GetUtf16Boundary(
                        breaks,
                        start.GetClusterBoundary());
                    int utf16End = GetUtf16Boundary(
                        breaks,
                        end.GetClusterBoundary());
                    if (utf16Start >= selectionStart &&
                        utf16End <= selectionEnd)
                    {
                        rectangles.Add(CreateRectangle(
                            line,
                            lineIndex,
                            start,
                            end,
                            utf16Start,
                            utf16End));
                    }
                }
            }

            index++;
        }
    }

    static TextCaretStop? FindNextInGroup(
        TextCaretStop[] stops,
        TextCaretStop start)
    {
        TextCaretStop? next = null;
        int index = 0;
        while (index < stops.Length)
        {
            TextCaretStop candidate = stops[index];
            if (candidate.GetLineIndex() == start.GetLineIndex() &&
                candidate.GetGroupIndex() == start.GetGroupIndex() &&
                candidate.GetClusterBoundary() > start.GetClusterBoundary() &&
                (next == null || candidate.GetClusterBoundary() <
                    next.GetClusterBoundary()))
            {
                next = candidate;
            }

            index++;
        }

        return next;
    }

    static TextSelectionRectangle CreateRectangle(
        TextLine line,
        int lineIndex,
        TextCaretStop first,
        TextCaretStop second,
        int utf16Start,
        int utf16End)
    {
        if (line.GetWritingMode() == TextWritingMode.Horizontal)
        {
            double x = System.Math.Min(first.GetX(), second.GetX());
            double width = System.Math.Abs(first.GetX() - second.GetX());
            return new TextSelectionRectangle(
                lineIndex,
                utf16Start,
                utf16End - utf16Start,
                x,
                line.GetBaseline() - line.GetDescent(),
                width,
                line.GetAscent() + line.GetDescent());
        }

        double y = System.Math.Min(first.GetY(), second.GetY());
        double height = System.Math.Abs(first.GetY() - second.GetY());
        return new TextSelectionRectangle(
            lineIndex,
            utf16Start,
            utf16End - utf16Start,
            line.GetBaseline() - line.GetDescent(),
            y,
            line.GetAscent() + line.GetDescent(),
            height);
    }

    static void SortRectangles(
        System.Collections.Generic.List<TextSelectionRectangle> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            TextSelectionRectangle value = values[index];
            int target = index;
            while (target > 0 && ComesAfter(values[target - 1], value))
            {
                values[target] = values[target - 1];
                target--;
            }

            values[target] = value;
            index++;
        }
    }

    static bool ComesAfter(
        TextSelectionRectangle first,
        TextSelectionRectangle second)
    {
        if (first.GetLineIndex() != second.GetLineIndex())
        {
            return first.GetLineIndex() > second.GetLineIndex();
        }

        if (first.GetX() != second.GetX())
        {
            return first.GetX() > second.GetX();
        }

        return first.GetY() > second.GetY();
    }

    static TextSelectionRectangle[] MergeRectangles(
        System.Collections.Generic.List<TextSelectionRectangle> values)
    {
        System.Collections.Generic.List<TextSelectionRectangle> merged =
            new System.Collections.Generic.List<TextSelectionRectangle>();
        int index = 0;
        while (index < values.Count)
        {
            TextSelectionRectangle value = values[index];
            if (merged.Count != 0 && CanMerge(
                merged[merged.Count - 1],
                value))
            {
                merged[merged.Count - 1] = Merge(
                    merged[merged.Count - 1],
                    value);
            }
            else
            {
                merged.Add(value);
            }

            index++;
        }

        return merged.ToArray();
    }

    static bool CanMerge(
        TextSelectionRectangle first,
        TextSelectionRectangle second)
    {
        if (first.GetLineIndex() != second.GetLineIndex())
        {
            return false;
        }

        bool horizontal = first.GetY() == second.GetY() &&
            first.GetHeight() == second.GetHeight();
        if (horizontal)
        {
            return second.GetX() <= first.GetX() + first.GetWidth();
        }

        bool vertical = first.GetX() == second.GetX() &&
            first.GetWidth() == second.GetWidth();
        return vertical &&
            second.GetY() <= first.GetY() + first.GetHeight();
    }

    static TextSelectionRectangle Merge(
        TextSelectionRectangle first,
        TextSelectionRectangle second)
    {
        int utf16Start = System.Math.Min(
            first.GetUtf16Start(),
            second.GetUtf16Start());
        int utf16End = System.Math.Max(
            first.GetUtf16Start() + first.GetUtf16Length(),
            second.GetUtf16Start() + second.GetUtf16Length());
        double x = System.Math.Min(first.GetX(), second.GetX());
        double y = System.Math.Min(first.GetY(), second.GetY());
        double xMaximum = System.Math.Max(
            first.GetX() + first.GetWidth(),
            second.GetX() + second.GetWidth());
        double yMaximum = System.Math.Max(
            first.GetY() + first.GetHeight(),
            second.GetY() + second.GetHeight());
        return new TextSelectionRectangle(
            first.GetLineIndex(),
            utf16Start,
            utf16End - utf16Start,
            x,
            y,
            xMaximum - x,
            yMaximum - y);
    }

    static bool Contains(
        System.Collections.Generic.List<TextCaretStop> values,
        TextCaretStop candidate,
        bool mergeCoincident)
    {
        int index = 0;
        while (index < values.Count)
        {
            TextCaretStop value = values[index];
            if (value.GetLineIndex() == candidate.GetLineIndex() &&
                value.GetClusterBoundary() ==
                    candidate.GetClusterBoundary() &&
                (mergeCoincident || value.GetGroupIndex() ==
                    candidate.GetGroupIndex()) &&
                value.GetX() == candidate.GetX() &&
                value.GetY() == candidate.GetY())
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool HasUtf16Boundary(TextCaretStop[] stops, int utf16Index)
    {
        int index = 0;
        while (index < stops.Length)
        {
            if (stops[index].GetUtf16Index() == utf16Index)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static double GetSquaredDistance(
        TextCaretStop stop,
        double x,
        double y)
    {
        double deltaX = stop.GetX() - x;
        double deltaY = stop.GetY() - y;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }

    static bool ComesBefore(TextCaretStop first, TextCaretStop second)
    {
        if (first.GetLineIndex() != second.GetLineIndex())
        {
            return first.GetLineIndex() < second.GetLineIndex();
        }

        if (first.GetUtf16Index() != second.GetUtf16Index())
        {
            return first.GetUtf16Index() < second.GetUtf16Index();
        }

        return first.GetGroupIndex() < second.GetGroupIndex();
    }

    static int GetUtf16Boundary(
        TextBreakMap breaks,
        int clusterBoundary)
    {
        return clusterBoundary == breaks.GetClusterCount() ?
            breaks.GetSourceLength() :
            breaks.GetUtf16Start(clusterBoundary);
    }
}
