using System;

namespace xml;

internal class SourceTracker
{
    internal string? SourceUri;
    internal XmlLineNormalizationResult Source;
    internal int NormalizedUtf16Offset;

    internal SourceTracker(string? sourceUri, XmlLineNormalizationResult source)
    {
        Guard.NotNull(source, "source");
        SourceUri = sourceUri;
        Source = source;
        NormalizedUtf16Offset = 0;
    }

    internal SourceLocation CurrentTrackedLocation()
    {
        return Source.GetNormalizedSourceLocation(NormalizedUtf16Offset, SourceUri);
    }

    internal SourceLocation TrackedLocationAt(int normalizedUtf16Offset)
    {
        EnsureScalarBoundary(normalizedUtf16Offset, "normalizedUtf16Offset");
        return Source.GetNormalizedSourceLocation(normalizedUtf16Offset, SourceUri);
    }

    internal void SetTrackedOffset(int normalizedUtf16Offset)
    {
        EnsureScalarBoundary(normalizedUtf16Offset, "normalizedUtf16Offset");
        NormalizedUtf16Offset = normalizedUtf16Offset;
    }

    internal void AdvanceTrackedUtf16(int utf16CodeUnitCount)
    {
        Guard.NonNegativeInt(utf16CodeUnitCount, "utf16CodeUnitCount");
        if (utf16CodeUnitCount > Source.Text.Length - NormalizedUtf16Offset)
        {
            throw new ArgumentOutOfRangeException("utf16CodeUnitCount");
        }

        int destination = NormalizedUtf16Offset + utf16CodeUnitCount;
        EnsureScalarBoundary(destination, "utf16CodeUnitCount");
        NormalizedUtf16Offset = destination;
    }

    internal void AdvanceTrackedScalar()
    {
        if (NormalizedUtf16Offset >= Source.Text.Length)
        {
            throw new InvalidOperationException("The source tracker is at the end of input.");
        }

        int first = Source.Text[NormalizedUtf16Offset];
        if (first >= 0xD800 && first <= 0xDBFF)
        {
            if (NormalizedUtf16Offset + 1 >= Source.Text.Length)
            {
                throw new FormatException("The tracked source ends with an unpaired high surrogate.");
            }

            int second = Source.Text[NormalizedUtf16Offset + 1];
            if (second < 0xDC00 || second > 0xDFFF)
            {
                throw new FormatException("The tracked source contains an unpaired high surrogate.");
            }

            NormalizedUtf16Offset = NormalizedUtf16Offset + 2;
            return;
        }

        if (first >= 0xDC00 && first <= 0xDFFF)
        {
            throw new FormatException("The tracked source contains an unpaired low surrogate.");
        }

        NormalizedUtf16Offset = NormalizedUtf16Offset + 1;
    }

    internal int MarkTrackedOffset()
    {
        return NormalizedUtf16Offset;
    }

    internal SourceRange CreateTrackedRange(int startUtf16Offset, int endUtf16Offset)
    {
        EnsureScalarBoundary(startUtf16Offset, "startUtf16Offset");
        EnsureScalarBoundary(endUtf16Offset, "endUtf16Offset");
        if (endUtf16Offset < startUtf16Offset)
        {
            throw new ArgumentException("A source range end cannot precede its start.", "endUtf16Offset");
        }

        SourceRange range = new SourceRange();
        range.Start = Source.GetNormalizedSourceLocation(startUtf16Offset, SourceUri);
        range.End = Source.GetNormalizedSourceLocation(endUtf16Offset, SourceUri);
        return range;
    }

    internal SourceRange CreateTrackedRangeToCurrent(int startUtf16Offset)
    {
        return CreateTrackedRange(startUtf16Offset, NormalizedUtf16Offset);
    }

    internal bool IsTrackedSourceEnd()
    {
        return NormalizedUtf16Offset >= Source.Text.Length;
    }

    internal int RemainingTrackedUtf16()
    {
        return Source.Text.Length - NormalizedUtf16Offset;
    }

    internal void EnsureScalarBoundary(int normalizedUtf16Offset, string parameterName)
    {
        Guard.NonNegativeInt(normalizedUtf16Offset, parameterName);
        if (normalizedUtf16Offset > Source.Text.Length)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        if (normalizedUtf16Offset > 0 && normalizedUtf16Offset < Source.Text.Length)
        {
            int current = Source.Text[normalizedUtf16Offset];
            int previous = Source.Text[normalizedUtf16Offset - 1];
            if (current >= 0xDC00 && current <= 0xDFFF && previous >= 0xD800 && previous <= 0xDBFF)
            {
                throw new ArgumentException("The offset is inside a Unicode surrogate pair.", parameterName);
            }
        }
    }
}
