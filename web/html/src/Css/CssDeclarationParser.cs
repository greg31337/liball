using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;

internal class CssDeclaration
{
    internal string name;
    internal string canonicalName;
    internal CssToken[] valueTokens;
    internal bool important;
    internal bool customProperty;
    internal bool duplicate;
    internal HtmlSourceSpan nameSpan;
    internal HtmlSourceSpan valueSpan;
    internal HtmlSourceSpan importantSpan;
    internal HtmlSourceSpan sourceSpan;

    internal CssDeclaration(string name, string canonicalName,
        CssToken[] valueTokens, bool important, bool customProperty,
        bool duplicate, HtmlSourceSpan nameSpan, HtmlSourceSpan valueSpan,
        HtmlSourceSpan importantSpan, HtmlSourceSpan sourceSpan)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (canonicalName == null)
        {
            throw new ArgumentNullException("canonicalName");
        }

        if (valueTokens == null)
        {
            throw new ArgumentNullException("valueTokens");
        }

        if (nameSpan == null)
        {
            throw new ArgumentNullException("nameSpan");
        }

        if (valueSpan == null)
        {
            throw new ArgumentNullException("valueSpan");
        }

        if (importantSpan == null)
        {
            throw new ArgumentNullException("importantSpan");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        this.name = name;
        this.canonicalName = canonicalName;
        this.valueTokens = CopyTokens(valueTokens);
        this.important = important;
        this.customProperty = customProperty;
        this.duplicate = duplicate;
        this.nameSpan = nameSpan;
        this.valueSpan = valueSpan;
        this.importantSpan = importantSpan;
        this.sourceSpan = sourceSpan;
    }

    internal string GetName()
    {
        return this.name;
    }

    internal string GetCanonicalName()
    {
        return this.canonicalName;
    }

    internal CssToken[] GetValueTokens()
    {
        return CopyTokens(this.valueTokens);
    }

    internal bool IsImportant()
    {
        return this.important;
    }

    internal bool IsCustomProperty()
    {
        return this.customProperty;
    }

    internal bool IsDuplicate()
    {
        return this.duplicate;
    }

    internal HtmlSourceSpan GetNameSpan()
    {
        return this.nameSpan;
    }

    internal HtmlSourceSpan GetValueSpan()
    {
        return this.valueSpan;
    }

    internal HtmlSourceSpan GetImportantSpan()
    {
        return this.importantSpan;
    }

    internal HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }

    internal static CssToken[] CopyTokens(CssToken[] values)
    {
        CssToken[] result = new CssToken[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A CSS declaration value cannot contain a null token.",
                    "valueTokens");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}

internal class CssDeclarationParseResult
{
    internal CssDeclaration[] declarations;
    internal bool success;
    internal bool truncated;
    internal int errorCount;
    internal int duplicateCount;

    internal CssDeclarationParseResult(CssDeclaration[] declarations,
        bool success, bool truncated, int errorCount, int duplicateCount)
    {
        if (declarations == null)
        {
            throw new ArgumentNullException("declarations");
        }

        if (errorCount < 0)
        {
            throw new ArgumentOutOfRangeException("errorCount");
        }

        if (duplicateCount < 0)
        {
            throw new ArgumentOutOfRangeException("duplicateCount");
        }

        this.declarations = CopyDeclarations(declarations);
        this.success = success;
        this.truncated = truncated;
        this.errorCount = errorCount;
        this.duplicateCount = duplicateCount;
    }

    internal CssDeclaration[] GetDeclarations()
    {
        return CopyDeclarations(this.declarations);
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal bool WasTruncated()
    {
        return this.truncated;
    }

    internal int GetErrorCount()
    {
        return this.errorCount;
    }

    internal int GetDuplicateCount()
    {
        return this.duplicateCount;
    }

    internal static CssDeclaration[] CopyDeclarations(CssDeclaration[] values)
    {
        CssDeclaration[] result = new CssDeclaration[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A CSS declaration collection cannot contain null.",
                    "declarations");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}

internal static class CssDeclarationParser
{
    internal static CssDeclarationParseResult Parse(string css)
    {
        return Parse(css, HtmlLimits.CreateDefault());
    }

    internal static CssDeclarationParseResult Parse(string css,
        HtmlLimits limits)
    {
        return Parse(css, limits, CancellationToken.None);
    }

    internal static CssDeclarationParseResult Parse(string css,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        if (css == null)
        {
            throw new ArgumentNullException("css");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        CssTokenizeResult tokenized = CssTokenizer.Tokenize(css, limits,
            cancellationToken);
        CssDeclarationParserState state = new CssDeclarationParserState(
            tokenized.GetTokens(), limits.GetMaximumCssDeclarations(),
            tokenized.GetErrorCount(), tokenized.WasTruncated(),
            cancellationToken);
        return state.Run();
    }
}

internal class CssDeclarationParserState
{
    internal CssToken[] tokens = Array.Empty<CssToken>();
    internal int maximumDeclarations;
    internal int position;
    internal List<CssDeclaration> declarations = new List<CssDeclaration>();
    internal Dictionary<string, bool> names = new Dictionary<string, bool>();
    internal int errorCount;
    internal int duplicateCount;
    internal bool truncated;
    internal CancellationToken cancellationToken;

    internal CssDeclarationParserState(CssToken[] tokens,
        int maximumDeclarations, int initialErrorCount, bool truncated,
        CancellationToken cancellationTokenValue)
    {
        this.Initialize(tokens, maximumDeclarations, initialErrorCount,
            truncated, cancellationTokenValue);
    }

    internal void Initialize(CssToken[] tokens, int maximumDeclarations,
        int initialErrorCount, bool truncated,
        CancellationToken cancellationTokenValue)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        this.tokens = CopyTokens(tokens);
        this.maximumDeclarations = maximumDeclarations;
        this.position = 0;
        this.declarations = new List<CssDeclaration>();
        this.names = new Dictionary<string, bool>(StringComparer.Ordinal);
        this.errorCount = initialErrorCount;
        this.duplicateCount = 0;
        this.truncated = truncated;
        this.cancellationToken = cancellationTokenValue;
    }

    internal CssDeclarationParseResult Run()
    {
        bool complete = false;
        while (!complete && this.position < this.tokens.Length)
        {
            this.CheckCancellation();
            this.SkipSeparators();
            if (this.position >= this.tokens.Length)
            {
                break;
            }

            if (this.tokens[this.position].GetKind() == CssTokenKind.End)
            {
                complete = true;
                continue;
            }

            int segmentStart = this.position;
            int segmentEnd = this.FindSegmentEnd(segmentStart);
            bool endedBySemicolon = segmentEnd < this.tokens.Length
                && this.tokens[segmentEnd].GetKind()
                    == CssTokenKind.Semicolon;
            bool endedByEnd = segmentEnd < this.tokens.Length
                && this.tokens[segmentEnd].GetKind() == CssTokenKind.End;

            if (!endedBySemicolon && !endedByEnd)
            {
                break;
            }

            if (!this.ParseSegment(segmentStart, segmentEnd,
                endedBySemicolon))
            {
                break;
            }

            this.position = endedBySemicolon
                ? segmentEnd + 1 : segmentEnd;
        }

        return new CssDeclarationParseResult(this.declarations.ToArray(),
            !this.truncated, this.truncated, this.errorCount,
            this.duplicateCount);
    }

    internal void SkipSeparators()
    {
        bool skipping = true;
        while (skipping && this.position < this.tokens.Length)
        {
            this.CheckCancellation();
            CssTokenKind kind = this.tokens[this.position].GetKind();
            if (kind == CssTokenKind.Whitespace
                || kind == CssTokenKind.Semicolon)
            {
                this.position = this.position + 1;
            }
            else
            {
                skipping = false;
            }
        }
    }

    internal int FindSegmentEnd(int start)
    {
        List<CssTokenKind> closers = new List<CssTokenKind>();
        int index = start;
        while (index < this.tokens.Length)
        {
            this.CheckCancellation();
            CssTokenKind kind = this.tokens[index].GetKind();
            if (kind == CssTokenKind.End)
            {
                return index;
            }

            if (kind == CssTokenKind.Semicolon && closers.Count == 0)
            {
                return index;
            }

            UpdateClosers(closers, kind);
            index = index + 1;
        }

        return index;
    }

    internal bool ParseSegment(int start, int end, bool endedBySemicolon)
    {
        this.CheckCancellation();
        int first = SkipWhitespaceForward(start, end);
        if (first >= end)
        {
            return true;
        }

        if (this.HasUnclosedComponents(start, end))
        {
            this.AddError();
        }

        CssToken nameToken = this.tokens[first];
        if (nameToken.GetKind() != CssTokenKind.Identifier)
        {
            this.AddError();
            return true;
        }

        int colon = SkipWhitespaceForward(first + 1, end);
        if (colon >= end
            || this.tokens[colon].GetKind() != CssTokenKind.Colon)
        {
            this.AddError();
            return true;
        }

        int valueStart = SkipWhitespaceForward(colon + 1, end);
        int valueEnd = SkipWhitespaceBackward(valueStart, end);
        bool important = false;
        int importantStart = -1;
        int importantEnd = -1;
        if (valueEnd > valueStart
            && this.tokens[valueEnd - 1].GetKind()
                == CssTokenKind.Identifier
            && IsAsciiEqual(this.tokens[valueEnd - 1].GetValue(),
                "important"))
        {
            int beforeImportant = SkipWhitespaceBackward(valueStart,
                valueEnd - 1);
            if (beforeImportant > valueStart
                && IsBang(this.tokens[beforeImportant - 1])
                && this.IsTopLevel(valueStart, beforeImportant - 1))
            {
                important = true;
                importantStart = beforeImportant - 1;
                importantEnd = valueEnd;
                valueEnd = SkipWhitespaceBackward(valueStart,
                    importantStart);
            }
        }

        string name = nameToken.GetValue();
        bool customProperty = IsCustomPropertyName(name);
        string canonicalName = customProperty ? name : AsciiLower(name);
        string key = customProperty
            ? "C" + name : "N" + canonicalName;
        bool duplicate = this.names.ContainsKey(key);

        if (this.declarations.Count >= this.maximumDeclarations)
        {
            this.truncated = true;
            return false;
        }

        if (duplicate)
        {
            this.IncrementDuplicateCount();
        }
        else
        {
            this.names.Add(key, true);
        }

        CssToken[] valueTokens = CopyRange(this.tokens, valueStart,
            valueEnd);
        HtmlSourcePosition emptyValuePosition = this.tokens[colon]
            .GetSourceSpan().GetEnd();
        HtmlSourceSpan valueSpan = valueStart < valueEnd
            ? CombineSpans(this.tokens[valueStart].GetSourceSpan(),
                this.tokens[valueEnd - 1].GetSourceSpan())
            : HtmlSourceSpan.Create(emptyValuePosition, emptyValuePosition);
        HtmlSourceSpan importantSpan = important
            ? CombineSpans(this.tokens[importantStart].GetSourceSpan(),
                this.tokens[importantEnd - 1].GetSourceSpan())
            : HtmlSourceSpan.CreateUnknown();
        HtmlSourceSpan sourceSpan = this.CreateDeclarationSpan(first, end,
            endedBySemicolon);

        this.declarations.Add(new CssDeclaration(name, canonicalName,
            valueTokens, important, customProperty, duplicate,
            nameToken.GetSourceSpan(), valueSpan, importantSpan, sourceSpan));
        return true;
    }

    internal HtmlSourceSpan CreateDeclarationSpan(int first, int end,
        bool endedBySemicolon)
    {
        HtmlSourceSpan firstSpan = this.tokens[first].GetSourceSpan();
        HtmlSourceSpan lastSpan;
        if (endedBySemicolon)
        {
            lastSpan = this.tokens[end].GetSourceSpan();
        }
        else
        {
            int last = end - 1;
            lastSpan = this.tokens[last].GetSourceSpan();
        }

        return CombineSpans(firstSpan, lastSpan);
    }

    internal bool IsTopLevel(int start, int target)
    {
        List<CssTokenKind> closers = new List<CssTokenKind>();
        int index = start;
        while (index < target)
        {
            UpdateClosers(closers, this.tokens[index].GetKind());
            index = index + 1;
        }

        return closers.Count == 0;
    }

    internal bool HasUnclosedComponents(int start, int end)
    {
        List<CssTokenKind> closers = new List<CssTokenKind>();
        int index = start;
        while (index < end)
        {
            UpdateClosers(closers, this.tokens[index].GetKind());
            index = index + 1;
        }

        return closers.Count > 0;
    }

    internal void AddError()
    {
        if (this.errorCount < int.MaxValue)
        {
            this.errorCount = this.errorCount + 1;
        }
    }

    internal void CheckCancellation()
    {
        this.cancellationToken.ThrowIfCancellationRequested();
    }

    internal void IncrementDuplicateCount()
    {
        if (this.duplicateCount < int.MaxValue)
        {
            this.duplicateCount = this.duplicateCount + 1;
        }
    }

    internal int SkipWhitespaceForward(int start, int end)
    {
        int index = start;
        while (index < end
            && this.tokens[index].GetKind() == CssTokenKind.Whitespace)
        {
            index = index + 1;
        }

        return index;
    }

    internal int SkipWhitespaceBackward(int start, int end)
    {
        int index = end;
        while (index > start
            && this.tokens[index - 1].GetKind() == CssTokenKind.Whitespace)
        {
            index = index - 1;
        }

        return index;
    }

    internal static void UpdateClosers(List<CssTokenKind> closers,
        CssTokenKind kind)
    {
        if (kind == CssTokenKind.Function
            || kind == CssTokenKind.OpenParenthesis)
        {
            closers.Add(CssTokenKind.CloseParenthesis);
            return;
        }

        if (kind == CssTokenKind.OpenSquare)
        {
            closers.Add(CssTokenKind.CloseSquare);
            return;
        }

        if (kind == CssTokenKind.OpenCurly)
        {
            closers.Add(CssTokenKind.CloseCurly);
            return;
        }

        if (closers.Count > 0 && closers[closers.Count - 1] == kind)
        {
            closers.RemoveAt(closers.Count - 1);
        }
    }

    internal static bool IsBang(CssToken token)
    {
        return token.GetKind() == CssTokenKind.Delimiter
            && token.GetValue() == "!";
    }

    internal static bool IsCustomPropertyName(string name)
    {
        return name.Length > 2 && name[0] == '-' && name[1] == '-';
    }

    internal static bool IsAsciiEqual(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            int leftValue = left[index];
            int rightValue = right[index];
            if (leftValue >= 'A' && leftValue <= 'Z')
            {
                leftValue = leftValue + 32;
            }

            if (rightValue >= 'A' && rightValue <= 'Z')
            {
                rightValue = rightValue + 32;
            }

            if (leftValue != rightValue)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string AsciiLower(string value)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current >= 'A' && current <= 'Z')
            {
                current = (char)(current + 32);
            }

            result.Append(current);
            index = index + 1;
        }

        return result.ToString();
    }

    internal static HtmlSourceSpan CombineSpans(HtmlSourceSpan first,
        HtmlSourceSpan last)
    {
        return HtmlSourceSpan.Create(first.GetStart(), last.GetEnd());
    }

    internal static CssToken[] CopyRange(CssToken[] values, int start, int end)
    {
        CssToken[] result = new CssToken[end - start];
        int index = start;
        while (index < end)
        {
            result[index - start] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static CssToken[] CopyTokens(CssToken[] values)
    {
        CssToken[] result = new CssToken[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A parser token collection cannot contain null.",
                    "tokens");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
