namespace svg;

/// <summary>Parses keyTimes, keySplines, and keyPoints lists.</summary>
public class SvgAnimationValueParser : ISvgAnimationValueParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgAnimationValueParser()
    {
    }

    /// <summary>Parses a bounded monotonic keyTimes list in the unit interval.</summary>
    /// <param name="value">The keyTimes text.</param>
    /// <param name="maxItems">The maximum value count.</param>
    /// <param name="maxTokenCharacters">The maximum number token length.</param>
    /// <returns>A normalized value list or valid prefix and failure.</returns>
    public SvgKeyValueListParseResult ParseKeyTimes(string value, int maxItems, int maxTokenCharacters)
    {
        return new SvgAnimationValueParser().ParseKeyValues(value, maxItems, maxTokenCharacters, true);
    }

    /// <summary>Parses a bounded keyPoints list in the unit interval.</summary>
    /// <param name="value">The keyPoints text.</param>
    /// <param name="maxItems">The maximum value count.</param>
    /// <param name="maxTokenCharacters">The maximum number token length.</param>
    /// <returns>A normalized value list or valid prefix and failure.</returns>
    public SvgKeyValueListParseResult ParseKeyPoints(string value, int maxItems, int maxTokenCharacters)
    {
        return new SvgAnimationValueParser().ParseKeyValues(value, maxItems, maxTokenCharacters, false);
    }

    /// <summary>Parses semicolon-separated cubic Bézier keySplines.</summary>
    /// <param name="value">The keySplines text.</param>
    /// <param name="maxItems">The maximum spline count.</param>
    /// <param name="maxTokenCharacters">The maximum number token length.</param>
    /// <returns>A spline list or valid prefix and failure.</returns>
    public SvgKeySplineListParseResult ParseKeySplines(string? value, int maxItems, int maxTokenCharacters)
    {
        System.Collections.Generic.List<SvgKeySpline> splines = new System.Collections.Generic.List<SvgKeySpline>();
        if (value == null || maxItems < 1 || maxTokenCharacters < 1) return new SvgAnimationValueParser().SplineResult(false, SvgTimingParseError.InvalidArgument, splines, 0);
        int position = new SvgAnimationValueParser().SkipWhitespace(value, 0, value.Length);
        if (position == value.Length) return new SvgAnimationValueParser().SplineResult(false, SvgTimingParseError.ExpectedValue, splines, position);
        while (position < value.Length)
        {
            if (splines.Count >= maxItems) return new SvgAnimationValueParser().SplineResult(false, SvgTimingParseError.ItemLimitExceeded, splines, position);
            int end = new SvgAnimationValueParser().Find(value, ';', position);
            int trimmedEnd = new SvgAnimationValueParser().TrimEnd(value, position, end);
            if (trimmedEnd == position) return new SvgAnimationValueParser().SplineResult(false, SvgTimingParseError.ExpectedValue, splines, position);
            string item = value.Substring(position, trimmedEnd - position);
            SvgNumberListResult parsed = SvgListParser.ParseNumberList(item, SvgListSeparatorMode.CommaOrWhitespace, 5, maxTokenCharacters, false);
            if (!parsed.Success || parsed.Values.Length != 4)
            {
                int offset = parsed.ErrorOffset < 0 ? position : position + parsed.ErrorOffset;
                return new SvgAnimationValueParser().SplineResult(false, SvgTimingParseError.InvalidKeyValue, splines, offset);
            }
            int index = 0;
            while (index < 4)
            {
                if (parsed.Values[index] < 0 || parsed.Values[index] > 1) return new SvgAnimationValueParser().SplineResult(false, SvgTimingParseError.InvalidKeyValue, splines, position);
                index = index + 1;
            }
            splines.Add(new SvgKeySpline(parsed.Values[0], parsed.Values[1], parsed.Values[2], parsed.Values[3], position, trimmedEnd));
            if (end == value.Length) return new SvgAnimationValueParser().SplineResult(true, SvgTimingParseError.None, splines, -1);
            position = new SvgAnimationValueParser().SkipWhitespace(value, end + 1, value.Length);
            if (position == value.Length) return new SvgAnimationValueParser().SplineResult(false, SvgTimingParseError.InvalidSeparator, splines, position);
        }
        return new SvgAnimationValueParser().SplineResult(true, SvgTimingParseError.None, splines, -1);
    }
SvgKeyValueListParseResult ParseKeyValues(string? value, int maximum, int tokenMaximum, bool requireMonotonic)
    {
        System.Collections.Generic.List<double> values = new System.Collections.Generic.List<double>();
        if (value == null || maximum < 1 || tokenMaximum < 1) return new SvgAnimationValueParser().KeyResult(false, SvgTimingParseError.InvalidArgument, values, 0);
        int position = new SvgAnimationValueParser().SkipWhitespace(value, 0, value.Length);
        if (position == value.Length) return new SvgAnimationValueParser().KeyResult(false, SvgTimingParseError.ExpectedValue, values, position);
        double previous = 0;
        while (position < value.Length)
        {
            if (values.Count >= maximum) return new SvgAnimationValueParser().KeyResult(false, SvgTimingParseError.ItemLimitExceeded, values, position);
            int end = new SvgAnimationValueParser().Find(value, ';', position);
            int trimmedEnd = new SvgAnimationValueParser().TrimEnd(value, position, end);
            if (trimmedEnd == position) return new SvgAnimationValueParser().KeyResult(false, SvgTimingParseError.ExpectedValue, values, position);
            SvgNumberParseResult number = SvgNumberParser.ParseNumber(value.Substring(position, trimmedEnd - position), tokenMaximum);
            if (!number.Success) return new SvgAnimationValueParser().KeyResult(false, number.Error == SvgNumberParseError.TokenTooLong ? SvgTimingParseError.TokenTooLong : SvgTimingParseError.InvalidKeyValue, values, position + number.ErrorOffset);
            if (number.Value < 0 || number.Value > 1) return new SvgAnimationValueParser().KeyResult(false, SvgTimingParseError.InvalidKeyValue, values, position);
            if (requireMonotonic && values.Count > 0 && number.Value < previous) return new SvgAnimationValueParser().KeyResult(false, SvgTimingParseError.NonMonotonic, values, position);
            values.Add(number.Value);
            previous = number.Value;
            if (end == value.Length) return new SvgAnimationValueParser().KeyResult(true, SvgTimingParseError.None, values, -1);
            position = new SvgAnimationValueParser().SkipWhitespace(value, end + 1, value.Length);
            if (position == value.Length) return new SvgAnimationValueParser().KeyResult(false, SvgTimingParseError.InvalidSeparator, values, position);
        }
        return new SvgAnimationValueParser().KeyResult(true, SvgTimingParseError.None, values, -1);
    }
SvgKeyValueListParseResult KeyResult(bool success, SvgTimingParseError error, System.Collections.Generic.List<double> values, int offset)
    {
        double[] snapshot = new double[values.Count];
        int index = 0;
        while (index < values.Count)
        {
            snapshot[index] = values[index];
            index = index + 1;
        }
        return new SvgKeyValueListParseResult(success, error, snapshot, offset);
    }
SvgKeySplineListParseResult SplineResult(bool success, SvgTimingParseError error, System.Collections.Generic.List<SvgKeySpline> splines, int offset)
    {
        return new SvgKeySplineListParseResult(success, error, splines, offset);
    }
int Find(string value, char character, int start)
    {
        int position = start;
        while (position < value.Length)
        {
            if (value[position] == character) return position;
            position = position + 1;
        }
        return value.Length;
    }
int SkipWhitespace(string value, int position, int end)
    {
        while (position < end && SvgSpanCharacters.IsSvgWhitespace(value[position])) position = position + 1;
        return position;
    }
int TrimEnd(string value, int start, int end)
    {
        while (end > start && SvgSpanCharacters.IsSvgWhitespace(value[end - 1])) end = end - 1;
        return end;
    }
}
