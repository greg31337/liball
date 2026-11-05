using System;

namespace svg;

/// <summary>Parses bounded SVG paint values and URL fallback chains.</summary>
public class SvgPaintParser : ISvgPaintParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPaintParser()
    {
    }

    /// <summary>Parses a complete SVG paint value with optional invalid-fallback recovery.</summary>
    /// <param name="value">The SVG paint text.</param>
    /// <param name="maxFallbacks">The maximum encountered fallback count.</param>
    /// <param name="maxTokenCharacters">The maximum URL, keyword, or color token length.</param>
    /// <param name="recoverInvalidFallbacks">Whether malformed fallbacks are skipped.</param>
    /// <returns>A complete, recovered, or failed paint parse result.</returns>
    public SvgPaintParseResult Parse(string? value, int maxFallbacks, int maxTokenCharacters, bool recoverInvalidFallbacks)
    {
        if (value == null || maxFallbacks < 0 || maxTokenCharacters < 1)
        {
            return new SvgPaintParser().Failure(SvgPaintParseError.InvalidArgument, 0, null);
        }
        int position = new SvgPaintParser().SkipWhitespace(value, 0);
        if (position == value.Length)
        {
            return new SvgPaintParser().Failure(SvgPaintParseError.ExpectedPaint, position, null);
        }
        int firstStart = position;
        int firstEnd = new SvgPaintParser().ScanAtomEnd(value, firstStart);
        SvgPaintAtomResult first = new SvgPaintParser().ParseAtom(value, firstStart, firstEnd, maxTokenCharacters);
        if (!first.Success)
        {
            return new SvgPaintParser().Failure(first.Error, first.ErrorOffset, null);
        }
        (first.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Source = value.Substring(firstStart, firstEnd - firstStart);
        position = firstEnd;
        int afterWhitespace = new SvgPaintParser().SkipWhitespace(value, position);
        if (afterWhitespace < value.Length && afterWhitespace == position)
        {
            SvgPaintParseError error = first.Paint.Kind == SvgPaintKind.Url ? SvgPaintParseError.InvalidFallback : SvgPaintParseError.TrailingCharacters;
            return new SvgPaintParser().Failure(error, position, first.Paint);
        }
        position = afterWhitespace;
        if (first.Paint.Kind != SvgPaintKind.Url)
        {
            if (position != value.Length)
            {
                return new SvgPaintParser().Failure(SvgPaintParseError.TrailingCharacters, position, first.Paint);
            }
            first.Paint.Source = value;
            return new SvgPaintParseResult(true, false, SvgPaintParseError.None, first.Paint, -1);
        }

        System.Collections.Generic.List<SvgPaint> fallbacks = new System.Collections.Generic.List<SvgPaint>();
        bool recovered = false;
        SvgPaintParseError firstError = SvgPaintParseError.None;
        int firstErrorOffset = -1;
        bool terminalSeen = false;
        int encountered = 0;
        while (position < value.Length)
        {
            if (encountered >= maxFallbacks)
            {
                SvgPaint prefix = new SvgPaintParser().BuildUrl(first.Paint.Url, fallbacks, value);
                return new SvgPaintParser().Failure(SvgPaintParseError.FallbackLimitExceeded, position, prefix);
            }
            encountered = encountered + 1;
            int start = position;
            int end = new SvgPaintParser().ScanAtomEnd(value, start);
            SvgPaintAtomResult atom = new SvgPaintParser().ParseAtom(value, start, end, maxTokenCharacters);
            bool validPosition = atom.Success && !terminalSeen && (atom.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind != SvgPaintKind.ContextFill && atom.Paint.Kind != SvgPaintKind.ContextStroke;
            SvgPaintParseError error = atom.Success ? SvgPaintParseError.InvalidFallback : atom.Error;
            int errorOffset = atom.Success ? start : atom.ErrorOffset;
            if (!validPosition)
            {
                if (!recoverInvalidFallbacks)
                {
                    SvgPaint prefix = new SvgPaintParser().BuildUrl(first.Paint.Url, fallbacks, value);
                    return new SvgPaintParser().Failure(error, errorOffset, prefix);
                }
                if (!recovered)
                {
                    firstError = error;
                    firstErrorOffset = errorOffset;
                }
                recovered = true;
            }
            else
            {
                (atom.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Source = value.Substring(start, end - start);
                fallbacks.Add(atom.Paint);
                terminalSeen = atom.Paint.Kind != SvgPaintKind.Url;
            }
            position = end;
            afterWhitespace = new SvgPaintParser().SkipWhitespace(value, position);
            if (afterWhitespace < value.Length && afterWhitespace == position)
            {
                SvgPaint prefix = new SvgPaintParser().BuildUrl(first.Paint.Url, fallbacks, value);
                if (!recoverInvalidFallbacks)
                {
                    return new SvgPaintParser().Failure(SvgPaintParseError.InvalidFallback, position, prefix);
                }
                if (!recovered)
                {
                    firstError = SvgPaintParseError.InvalidFallback;
                    firstErrorOffset = position;
                }
                recovered = true;
                position = new SvgPaintParser().RecoverToWhitespace(value, position);
                position = new SvgPaintParser().SkipWhitespace(value, position);
            }
            else
            {
                position = afterWhitespace;
            }
        }
        SvgPaint paint = new SvgPaintParser().BuildUrl(first.Paint.Url, fallbacks, value);
        return new SvgPaintParseResult(true, recovered, firstError, paint, firstErrorOffset);
    }
SvgPaintAtomResult ParseAtom(string value, int start, int end, int maximum)
    {
        if (end <= start)
        {
            return new SvgPaintParser().AtomFailure(SvgPaintParseError.ExpectedPaint, start);
        }
        int open = new SvgPaintParser().FindFunctionOpen(value, start, end);
        if (open >= 0)
        {
            SvgPaintNameResult name = new SvgPaintParser().DecodeName(value, start, open, maximum);
            if (!name.Success) return new SvgPaintParser().AtomFailure(name.Error, name.ErrorOffset);
            if (new SvgPaintParser().Equal(name.Name, "url")) return new SvgPaintParser().ParseUrl(value, start, open, end, maximum);
        }
        else
        {
            SvgPaintNameResult name = new SvgPaintParser().DecodeName(value, start, end, maximum);
            if (name.Success)
            {
                if (new SvgPaintParser().Equal(name.Name, "none")) return new SvgPaintParser().AtomSuccess(SvgPaint.NoPaint());
                if (new SvgPaintParser().Equal(name.Name, "context-fill")) return new SvgPaintParser().AtomSuccess(SvgPaint.Context(SvgPaintKind.ContextFill));
                if (new SvgPaintParser().Equal(name.Name, "context-stroke")) return new SvgPaintParser().AtomSuccess(SvgPaint.Context(SvgPaintKind.ContextStroke));
            }
            else if (name.Error == SvgPaintParseError.TokenTooLong)
            {
                return new SvgPaintParser().AtomFailure(name.Error, name.ErrorOffset);
            }
        }
        string text = value.Substring(start, end - start);
        SvgColorParseResult color = new SvgColorParser().Parse(text, maximum);
        if (color.Success)
        {
            return new SvgPaintParser().AtomSuccess(SvgPaint.Solid((color.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))));
        }
        SvgPaintParseError error = color.Error == SvgColorParseError.TokenTooLong ? SvgPaintParseError.TokenTooLong : color.Error == SvgColorParseError.UnknownColor || color.Error == SvgColorParseError.UnknownFunction || color.Error == SvgColorParseError.ExpectedColor ? SvgPaintParseError.UnknownPaint : SvgPaintParseError.InvalidColor;
        return new SvgPaintParser().AtomFailure(error, start + color.ErrorOffset);
    }
SvgPaintAtomResult ParseUrl(string value, int start, int open, int end, int maximum)
    {
        if (end <= open + 1 || value[end - 1] != ')')
        {
            return new SvgPaintParser().AtomFailure(SvgPaintParseError.InvalidUrl, end);
        }
        int contentStart = new SvgPaintParser().SkipWhitespace(value, open + 1);
        int close = end - 1;
        if (contentStart >= close)
        {
            return new SvgPaintParser().AtomFailure(SvgPaintParseError.InvalidUrl, contentStart);
        }
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        char quote = value[contentStart];
        if (quote == '\'' || quote == '"')
        {
            int position = contentStart + 1;
            bool closed = false;
            while (position < close)
            {
                char current = value[position];
                if (current == quote)
                {
                    closed = true;
                    position = position + 1;
                    break;
                }
                if (current == '\n' || current == '\r' || current == '\f')
                {
                    return new SvgPaintParser().AtomFailure(SvgPaintParseError.InvalidUrl, position);
                }
                if (current == '\\')
                {
                    SvgPaintEscapeResult escape = new SvgPaintParser().ReadEscape(value, position, maximum);
                    if (!escape.Success) return new SvgPaintParser().AtomFailure(escape.Error, escape.ErrorOffset);
                    if (escape.End > close) return new SvgPaintParser().AtomFailure(SvgPaintParseError.InvalidUrl, position);
                    builder.Append(escape.Value);
                    position = escape.End;
                }
                else
                {
                    builder.Append(current);
                    position = position + 1;
                }
                if (builder.Length > maximum) return new SvgPaintParser().AtomFailure(SvgPaintParseError.TokenTooLong, position - 1);
            }
            if (!closed) return new SvgPaintParser().AtomFailure(SvgPaintParseError.InvalidUrl, close);
            position = new SvgPaintParser().SkipWhitespace(value, position);
            if (position != close) return new SvgPaintParser().AtomFailure(SvgPaintParseError.InvalidUrl, position);
        }
        else
        {
            int position = contentStart;
            bool trailingWhitespace = false;
            while (position < close)
            {
                char current = value[position];
                if (SvgSpanCharacters.IsCssWhitespace(current))
                {
                    trailingWhitespace = true;
                    position = position + 1;
                    continue;
                }
                if (trailingWhitespace || current == '\'' || current == '"' || current == '(' || current < 32 || current == 127)
                {
                    return new SvgPaintParser().AtomFailure(SvgPaintParseError.InvalidUrl, position);
                }
                if (current == '\\')
                {
                    SvgPaintEscapeResult escape = new SvgPaintParser().ReadEscape(value, position, maximum);
                    if (!escape.Success) return new SvgPaintParser().AtomFailure(escape.Error, escape.ErrorOffset);
                    if (escape.End > close) return new SvgPaintParser().AtomFailure(SvgPaintParseError.InvalidUrl, position);
                    builder.Append(escape.Value);
                    position = escape.End;
                }
                else
                {
                    builder.Append(current);
                    position = position + 1;
                }
                if (builder.Length > maximum) return new SvgPaintParser().AtomFailure(SvgPaintParseError.TokenTooLong, position - 1);
            }
        }
        if (builder.Length == 0)
        {
            return new SvgPaintParser().AtomFailure(SvgPaintParseError.InvalidUrl, contentStart);
        }
        SvgPaint paint = SvgPaint.UrlReference(builder.ToString(), new System.Collections.Generic.List<SvgPaint>());
        return new SvgPaintParser().AtomSuccess(paint);
    }
SvgPaintEscapeResult ReadEscape(string value, int position, int maximum)
    {
        SvgSpanReader reader = new SvgSpanReader(value, maximum);
        reader.SetPosition(position);
        SvgSpanEscapeResult escape = reader.ReadEscape();
        if (!escape.Success)
        {
            return new SvgPaintEscapeResult(false, SvgPaintParseError.InvalidUrl, escape.ErrorOffset, escape.End, "");
        }
        return new SvgPaintEscapeResult(true, SvgPaintParseError.None, -1, escape.End, escape.Value);
    }
SvgPaintNameResult DecodeName(string value, int start, int end, int maximum)
    {
        if (end <= start)
        {
            return new SvgPaintNameResult(false, SvgPaintParseError.UnknownPaint, start, "");
        }
        SvgSpanReader reader = SvgSpanReader.FromMemory(value.AsMemory(start, end - start), maximum);
        SvgSpanTokenResult token = reader.ReadToken(IsNameCharacter, true);
        if (!token.Success)
        {
            SvgPaintParseError error = token.Error == SvgSpanReadError.TokenTooLong ? SvgPaintParseError.TokenTooLong : SvgPaintParseError.UnknownPaint;
            return new SvgPaintNameResult(false, error, start + token.ErrorOffset, "");
        }
        if (!reader.IsEnd())
        {
            return new SvgPaintNameResult(false, SvgPaintParseError.UnknownPaint, start + reader.GetPosition(), "");
        }
        return new SvgPaintNameResult(true, SvgPaintParseError.None, -1, token.GetValue());
    }
int ScanAtomEnd(string value, int start)
    {
        int position = start;
        int depth = 0;
        char quote = '\0';
        while (position < value.Length)
        {
            char current = value[position];
            if (current == '\\')
            {
                position = new SvgPaintParser().SkipEscape(value, position);
                continue;
            }
            if (quote != '\0')
            {
                if (current == quote) quote = '\0';
                position = position + 1;
                continue;
            }
            if (current == '\'' || current == '"')
            {
                quote = current;
                position = position + 1;
                continue;
            }
            if (current == '(')
            {
                depth = depth + 1;
                position = position + 1;
                continue;
            }
            if (current == ')' && depth > 0)
            {
                depth = depth - 1;
                position = position + 1;
                if (depth == 0) return position;
                continue;
            }
            if (depth == 0 && SvgSpanCharacters.IsCssWhitespace(current)) return position;
            position = position + 1;
        }
        return position;
    }
int FindFunctionOpen(string value, int start, int end)
    {
        int position = start;
        while (position < end)
        {
            char current = value[position];
            if (current == '\\')
            {
                position = new SvgPaintParser().SkipEscape(value, position);
                continue;
            }
            if (current == '(') return position;
            position = position + 1;
        }
        return -1;
    }
int SkipEscape(string value, int position)
    {
        int next = position + 1;
        if (next >= value.Length) return next;
        if (SvgSpanCharacters.HexValue(value[next]) >= 0)
        {
            int count = 0;
            while (next < value.Length && count < 6 && SvgSpanCharacters.HexValue(value[next]) >= 0)
            {
                next = next + 1;
                count = count + 1;
            }
            if (next < value.Length && SvgSpanCharacters.IsCssWhitespace(value[next]))
            {
                char whitespace = value[next];
                next = next + 1;
                if (whitespace == '\r' && next < value.Length && value[next] == '\n') next = next + 1;
            }
            return next;
        }
        return next + 1;
    }
int SkipWhitespace(string value, int position)
    {
        while (position < value.Length && SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
        return position;
    }
int RecoverToWhitespace(string value, int position)
    {
        while (position < value.Length && !SvgSpanCharacters.IsCssWhitespace(value[position])) position = position + 1;
        return position;
    }
SvgPaint BuildUrl(string url, System.Collections.Generic.List<SvgPaint> fallbacks, string source)
    {
        SvgPaint paint = SvgPaint.UrlReference(url, fallbacks);
        paint.Source = source;
        return paint;
    }
bool Equal(string first, string second)
    {
        return string.Equals(first, second, System.StringComparison.OrdinalIgnoreCase);
    }
bool IsNameCharacter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value == '-';
    }
SvgPaintAtomResult AtomSuccess(SvgPaint paint)
    {
        return new SvgPaintAtomResult(true, SvgPaintParseError.None, -1, paint);
    }
SvgPaintAtomResult AtomFailure(SvgPaintParseError error, int offset)
    {
        return new SvgPaintAtomResult(false, error, offset, null);
    }
SvgPaintParseResult Failure(SvgPaintParseError error, int offset, SvgPaint? paint)
    {
        return new SvgPaintParseResult(false, false, error, paint, offset);
    }
}
