using System.Collections.Generic;

namespace http;

internal static class RangeSemantics
{
    internal static bool AppliesToRequestMethod(string method)
    {
        return method == HttpMethods.Get;
    }

    internal static HttpRangeDecision NormalizeByteRanges(
        HttpRangeRequest request,
        long representationLength)
    {
        if (!HasValidContext(request, representationLength))
        {
            return CreateDecision(
                HttpRangeOutcome.Malformed,
                request,
                "The byte-range normalization context is malformed.");
        }

        if (!IsValidRangeUnit(request.Unit))
        {
            return CreateDecision(
                HttpRangeOutcome.Malformed,
                request,
                "The range unit is not a valid token.");
        }

        if (!Ascii.EqualsStringsIgnoreCase(request.Unit, HttpRangeUnits.Bytes))
        {
            return CreateDecision(
                HttpRangeOutcome.UnsupportedUnit,
                request,
                "Only the bytes range unit can be normalized.");
        }

        if (request.OtherRanges.Count != 0 || request.Ranges.Count == 0)
        {
            return CreateDecision(
                HttpRangeOutcome.Malformed,
                request,
                "A bytes range request must contain only byte ranges.");
        }

        if (request.Ranges.Count > request.Limits.MaximumRangeCount)
        {
            return CreateDecision(
                HttpRangeOutcome.Ignored,
                request,
                "The range count exceeds the configured anti-abuse limit.");
        }

        HttpRangeDecision result = CreateDecision(
            HttpRangeOutcome.Satisfiable,
            request,
            "At least one requested byte range is satisfiable.");
        int index = 0;
        while (index < request.Ranges.Count)
        {
            HttpByteRange source = request.Ranges[index];
            if (!IsValidByteRange(source))
            {
                return CreateDecision(
                    HttpRangeOutcome.Malformed,
                    request,
                    "A requested byte range is malformed.");
            }

            HttpByteRange? normalized = NormalizeByteRange(
                source,
                representationLength);
            if (normalized != null)
            {
                AddNormalizedRange(
                    result.Ranges,
                    normalized,
                    request.CoalesceOverlaps,
                    request.CoalesceAdjacent);
            }

            index = index + 1;
        }

        if (result.Ranges.Count == 0)
        {
            result.Outcome = HttpRangeOutcome.Unsatisfiable;
            result.Reason = "None of the requested byte ranges is satisfiable.";
            result.ContentRanges.Add(BuildUnsatisfiedContentRange(representationLength));
            return result;
        }

        index = 0;
        while (index < result.Ranges.Count)
        {
            result.ContentRanges.Add(BuildContentRange(
                result.Ranges[index],
                representationLength));
            index = index + 1;
        }

        return result;
    }

    internal static HttpContentRange BuildContentRange(
        HttpByteRange range,
        long representationLength)
    {
        if (range == null || !range.IsNormalized || range.IsSuffix ||
            !range.HasStart || !range.HasEnd || range.Start < 0 ||
            range.End < range.Start || representationLength < 0 ||
            range.End >= representationLength)
        {
            throw new HttpProtocolException(
                "A Content-Range value requires a valid normalized byte range.",
                "invalid-normalized-byte-range");
        }

        HttpContentRange result = new HttpContentRange();
        result.Unit = HttpRangeUnits.Bytes;
        result.HasStart = true;
        result.Start = range.Start;
        result.HasEnd = true;
        result.End = range.End;
        result.HasCompleteLength = true;
        result.CompleteLength = representationLength;
        return result;
    }

    internal static HttpContentRange BuildUnsatisfiedContentRange(
        long representationLength)
    {
        if (representationLength < 0)
        {
            throw new HttpProtocolException(
                "An unsatisfied Content-Range value requires a known representation length.",
                "invalid-representation-length");
        }

        HttpContentRange result = new HttpContentRange();
        result.Unit = HttpRangeUnits.Bytes;
        result.HasCompleteLength = true;
        result.CompleteLength = representationLength;
        result.IsUnsatisfied = true;
        return result;
    }

    static bool HasValidContext(HttpRangeRequest request, long representationLength)
    {
        return request != null && request.Unit != null && request.Ranges != null &&
            request.OtherRanges != null && request.Limits != null &&
            request.Limits.MaximumRangeCount >= 0 && representationLength >= 0;
    }

    static bool IsValidRangeUnit(string unit)
    {
        if (unit.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < unit.Length)
        {
            char character = unit[index];
            if (character > 0x7f || !HttpGrammar.IsTokenCharacter((byte)character))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsValidByteRange(HttpByteRange range)
    {
        if (range == null)
        {
            return false;
        }

        if (range.IsSuffix)
        {
            return !range.HasStart && !range.HasEnd && range.SuffixLength >= 0;
        }

        return range.HasStart && range.Start >= 0 &&
            (!range.HasEnd || range.End >= range.Start);
    }

    static HttpByteRange? NormalizeByteRange(
        HttpByteRange source,
        long representationLength)
    {
        if (representationLength == 0)
        {
            return null;
        }

        long start;
        long end = representationLength - 1;
        if (source.IsSuffix)
        {
            if (source.SuffixLength == 0)
            {
                return null;
            }

            start = source.SuffixLength >= representationLength ?
                0 : representationLength - source.SuffixLength;
        }
        else
        {
            if (source.Start >= representationLength)
            {
                return null;
            }

            start = source.Start;
            if (source.HasEnd && source.End < end)
            {
                end = source.End;
            }
        }

        HttpByteRange result = new HttpByteRange();
        result.HasStart = true;
        result.Start = start;
        result.HasEnd = true;
        result.End = end;
        result.IsNormalized = true;
        return result;
    }

    static void AddNormalizedRange(
        List<HttpByteRange> ranges,
        HttpByteRange candidate,
        bool coalesceOverlaps,
        bool coalesceAdjacent)
    {
        int mergeIndex = -1;
        int index = 0;
        while (index < ranges.Count && mergeIndex < 0)
        {
            if (ShouldCoalesce(
                ranges[index],
                candidate,
                coalesceOverlaps,
                coalesceAdjacent))
            {
                mergeIndex = index;
            }

            index = index + 1;
        }

        if (mergeIndex < 0)
        {
            ranges.Add(candidate);
            return;
        }

        HttpByteRange target = ranges[mergeIndex];
        MergeBounds(target, candidate);
        index = 0;
        while (index < ranges.Count)
        {
            if (index != mergeIndex && ShouldCoalesce(
                target,
                ranges[index],
                coalesceOverlaps,
                coalesceAdjacent))
            {
                MergeBounds(target, ranges[index]);
                ranges.RemoveAt(index);
                if (index < mergeIndex)
                {
                    mergeIndex = mergeIndex - 1;
                }

                index = 0;
            }
            else
            {
                index = index + 1;
            }
        }
    }

    static bool ShouldCoalesce(
        HttpByteRange left,
        HttpByteRange right,
        bool coalesceOverlaps,
        bool coalesceAdjacent)
    {
        bool overlaps = left.Start <= right.End && right.Start <= left.End;
        if (coalesceOverlaps && overlaps)
        {
            return true;
        }

        if (!coalesceAdjacent)
        {
            return false;
        }

        bool leftBeforeRight = left.End < long.MaxValue &&
            left.End + 1 == right.Start;
        bool rightBeforeLeft = right.End < long.MaxValue &&
            right.End + 1 == left.Start;
        return leftBeforeRight || rightBeforeLeft;
    }

    static void MergeBounds(HttpByteRange target, HttpByteRange source)
    {
        if (source.Start < target.Start)
        {
            target.Start = source.Start;
        }

        if (source.End > target.End)
        {
            target.End = source.End;
        }
    }

    static HttpRangeDecision CreateDecision(
        HttpRangeOutcome outcome,
        HttpRangeRequest? request,
        string reason)
    {
        HttpRangeDecision result = new HttpRangeDecision();
        result.Outcome = outcome;
        result.Request = request;
        result.Reason = reason;
        return result;
    }
}
