using System.Collections.Generic;
using System.Text;

namespace dns;

internal enum DnsMasterFileTokenKind
{
    Atom = 0,
    QuotedString = 1
}

internal class DnsMasterFileToken
{
    internal DnsMasterFileTokenKind Kind;
    internal string Text = string.Empty;
    internal string RawText = string.Empty;
    internal string? SourcePath;
    internal int Line;
    internal int Column;
    internal int EndLine;
    internal int EndColumn;
}

internal class DnsMasterFileComment
{
    internal string Text = string.Empty;
    internal string? SourcePath;
    internal int Line;
    internal int Column;
}

internal class DnsMasterFileRecord
{
    internal bool OwnerOmitted;
    internal string? SourcePath;
    internal int Line;
    internal int Column;
    internal List<DnsMasterFileToken> Tokens = new List<DnsMasterFileToken>();
}

internal class DnsMasterFileTokenizeResult
{
    internal bool Success;
    internal int ResourceRecordCount;
    internal List<DnsMasterFileRecord> Records =
        new List<DnsMasterFileRecord>();
    internal List<DnsMasterFileComment> Comments =
        new List<DnsMasterFileComment>();
    internal List<DnsZoneDiagnostic> Diagnostics =
        new List<DnsZoneDiagnostic>();
}

internal class DnsMasterFileTokenReadResult
{
    internal bool Success;
    internal int NextIndex;
    internal int NextLine;
    internal int NextColumn;
    internal DnsMasterFileToken? Token;
    internal DnsZoneDiagnostic? Diagnostic;
}

internal static class DnsMasterFileTokenizer
{
    internal const int MaximumTokenLength = 65535;
    internal const int MaximumTokensPerRecord = 65535;

    internal static DnsMasterFileTokenizeResult Tokenize(
        string source,
        string? sourcePath,
        DnsZoneLoadOptions options)
    {
        if (source == null || options == null)
        {
            return Failure(
                sourcePath,
                DnsDiagnosticCode.ValueOutOfRange,
                "Master-file source and load options are required.",
                1,
                1);
        }

        if (options.MaximumRecords < 0)
        {
            return Failure(
                sourcePath,
                DnsDiagnosticCode.ValueOutOfRange,
                "The maximum record count cannot be negative.",
                1,
                1);
        }

        DnsMasterFileTokenizeResult result =
            new DnsMasterFileTokenizeResult();
        DnsMasterFileRecord? record = null;
        int index = 0;
        int line = 1;
        int column = 1;
        int parentheses = 0;
        bool leadingWhitespace = false;
        while (index < source.Length)
        {
            char character = source[index];
            if (character == ' ' || character == '\t' ||
                character == '\f')
            {
                if (record == null) leadingWhitespace = true;
                index = index + 1;
                column = column + 1;
                continue;
            }

            if (character == '\r' || character == '\n')
            {
                if (character == '\r' && index + 1 < source.Length &&
                    source[index + 1] == '\n')
                {
                    index = index + 1;
                }

                index = index + 1;
                line = line + 1;
                column = 1;
                if (parentheses == 0)
                {
                    DnsMasterFileTokenizeResult? limit = AddRecord(
                        result, record, options, sourcePath, line - 1);
                    if (limit != null) return limit;
                    record = null;
                    leadingWhitespace = false;
                }

                continue;
            }

            if (character == ';')
            {
                DnsMasterFileComment comment = ReadComment(
                    source, index, line, column, sourcePath);
                result.Comments.Add(comment);
                int commentLength = ReadCommentLength(source, index);
                index = index + commentLength;
                column = column + commentLength;
                continue;
            }

            if (character == '(')
            {
                if (parentheses != 0)
                {
                    return FailureWithPartial(
                        result,
                        sourcePath,
                        DnsDiagnosticCode.InvalidText,
                        "Nested master-file parentheses are not permitted.",
                        line,
                        column);
                }

                if (record == null)
                {
                    record = CreateRecord(
                        sourcePath, line, column, leadingWhitespace);
                }

                parentheses = 1;
                index = index + 1;
                column = column + 1;
                continue;
            }

            if (character == ')')
            {
                if (parentheses == 0)
                {
                    return FailureWithPartial(
                        result,
                        sourcePath,
                        DnsDiagnosticCode.InvalidText,
                        "A closing parenthesis has no matching opening parenthesis.",
                        line,
                        column);
                }

                parentheses = 0;
                index = index + 1;
                column = column + 1;
                continue;
            }

            if (record == null)
            {
                record = CreateRecord(
                    sourcePath, line, column, leadingWhitespace);
            }

            DnsMasterFileTokenReadResult token = ReadToken(
                source, index, line, column, sourcePath);
            if (!token.Success || token.Token == null)
            {
                if (token.Diagnostic != null)
                    result.Diagnostics.Add(token.Diagnostic);
                return result;
            }

            if (record.Tokens.Count >= MaximumTokensPerRecord)
            {
                return FailureWithPartial(
                    result,
                    sourcePath,
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "A logical record contains too many tokens.",
                    line,
                    column);
            }

            record.Tokens.Add(token.Token);
            index = token.NextIndex;
            line = token.NextLine;
            column = token.NextColumn;
        }

        if (parentheses != 0)
        {
            return FailureWithPartial(
                result,
                sourcePath,
                DnsDiagnosticCode.InvalidText,
                "The master file ends inside a parenthesized record.",
                line,
                column);
        }

        DnsMasterFileTokenizeResult? finalLimit = AddRecord(
            result, record, options, sourcePath, line);
        if (finalLimit != null) return finalLimit;
        result.Success = true;
        return result;
    }

    static DnsMasterFileTokenReadResult ReadToken(
        string source,
        int startIndex,
        int startLine,
        int startColumn,
        string? sourcePath)
    {
        bool quoted = source[startIndex] == '"';
        StringBuilder text = new StringBuilder();
        StringBuilder raw = new StringBuilder();
        int index = startIndex;
        int line = startLine;
        int column = startColumn;
        bool closed = !quoted;
        if (quoted)
        {
            raw.Append('"');
            index = index + 1;
            column = column + 1;
            closed = false;
        }

        while (index < source.Length)
        {
            char character = source[index];
            if (quoted && character == '"')
            {
                raw.Append(character);
                index = index + 1;
                column = column + 1;
                closed = true;
                break;
            }

            if (!quoted && IsTokenDelimiter(character))
            {
                break;
            }

            if (character == '\r' || character == '\n')
            {
                if (quoted)
                {
                    return TokenFailure(
                        sourcePath,
                        "A quoted string cannot contain an unescaped newline.",
                        line,
                        column);
                }

                break;
            }

            if (character == '\\')
            {
                DnsMasterFileTokenReadResult escape = ReadEscape(
                    source, index, line, column, sourcePath, text, raw);
                if (!escape.Success) return escape;
                index = escape.NextIndex;
                line = escape.NextLine;
                column = escape.NextColumn;
            }
            else
            {
                if (!quoted && character == '"')
                {
                    return TokenFailure(
                        sourcePath,
                        "A quote must begin a separate master-file token.",
                        line,
                        column);
                }

                text.Append(character);
                raw.Append(character);
                index = index + 1;
                column = column + 1;
            }

            if (raw.Length > MaximumTokenLength)
            {
                return TokenLimitFailure(sourcePath, line, column);
            }
        }

        if (quoted && !closed)
        {
            return TokenFailure(
                sourcePath,
                "The master file ends inside a quoted string.",
                startLine,
                startColumn);
        }

        if (quoted && index < source.Length &&
            !IsTokenDelimiter(source[index]) &&
            source[index] != '\r' && source[index] != '\n')
        {
            return TokenFailure(
                sourcePath,
                "A quoted token must be followed by a delimiter.",
                line,
                column);
        }

        DnsMasterFileToken token = new DnsMasterFileToken();
        token.Kind = DnsMasterFileTokenKind.Atom;
        if (quoted) token.Kind = DnsMasterFileTokenKind.QuotedString;
        token.Text = text.ToString();
        token.RawText = raw.ToString();
        token.SourcePath = sourcePath;
        token.Line = startLine;
        token.Column = startColumn;
        token.EndLine = line;
        token.EndColumn = column - 1;
        DnsMasterFileTokenReadResult result =
            new DnsMasterFileTokenReadResult();
        result.Success = true;
        result.NextIndex = index;
        result.NextLine = line;
        result.NextColumn = column;
        result.Token = token;
        return result;
    }

    static DnsMasterFileTokenReadResult ReadEscape(
        string source,
        int startIndex,
        int startLine,
        int startColumn,
        string? sourcePath,
        StringBuilder text,
        StringBuilder raw)
    {
        if (startIndex + 1 >= source.Length)
        {
            return TokenFailure(
                sourcePath,
                "The master file ends after an escape character.",
                startLine,
                startColumn);
        }

        raw.Append('\\');
        char escaped = source[startIndex + 1];
        if (escaped == '\r' || escaped == '\n')
        {
            int nextIndex = startIndex + 2;
            if (escaped == '\r' && nextIndex < source.Length &&
                source[nextIndex] == '\n')
            {
                raw.Append('\r');
                raw.Append('\n');
                nextIndex = nextIndex + 1;
            }
            else
            {
                raw.Append(escaped);
            }

            DnsMasterFileTokenReadResult continuation =
                new DnsMasterFileTokenReadResult();
            continuation.Success = true;
            continuation.NextIndex = nextIndex;
            continuation.NextLine = startLine + 1;
            continuation.NextColumn = 1;
            return continuation;
        }

        if (escaped >= '0' && escaped <= '9')
        {
            if (startIndex + 3 >= source.Length)
            {
                return TokenFailure(
                    sourcePath,
                    "A decimal escape requires exactly three digits.",
                    startLine,
                    startColumn);
            }

            char second = source[startIndex + 2];
            char third = source[startIndex + 3];
            if (second < '0' || second > '9' || third < '0' || third > '9')
            {
                return TokenFailure(
                    sourcePath,
                    "A decimal escape requires exactly three digits.",
                    startLine,
                    startColumn);
            }

            int value = ((escaped - '0') * 100) +
                ((second - '0') * 10) + (third - '0');
            if (value > 255)
            {
                return TokenFailure(
                    sourcePath,
                    "A decimal escape cannot exceed 255.",
                    startLine,
                    startColumn);
            }

            raw.Append(escaped);
            raw.Append(second);
            raw.Append(third);
            text.Append((char)value);
            DnsMasterFileTokenReadResult decimalResult =
                new DnsMasterFileTokenReadResult();
            decimalResult.Success = true;
            decimalResult.NextIndex = startIndex + 4;
            decimalResult.NextLine = startLine;
            decimalResult.NextColumn = startColumn + 4;
            return decimalResult;
        }

        raw.Append(escaped);
        text.Append(escaped);
        DnsMasterFileTokenReadResult result =
            new DnsMasterFileTokenReadResult();
        result.Success = true;
        result.NextIndex = startIndex + 2;
        result.NextLine = startLine;
        result.NextColumn = startColumn + 2;
        return result;
    }

    static bool IsTokenDelimiter(char value)
    {
        return value == ' ' || value == '\t' || value == '\f' ||
            value == ';' || value == '(' || value == ')';
    }

    static DnsMasterFileRecord CreateRecord(
        string? sourcePath,
        int line,
        int column,
        bool ownerOmitted)
    {
        DnsMasterFileRecord record = new DnsMasterFileRecord();
        record.SourcePath = sourcePath;
        record.Line = line;
        record.Column = column;
        record.OwnerOmitted = ownerOmitted;
        return record;
    }

    static DnsMasterFileTokenizeResult? AddRecord(
        DnsMasterFileTokenizeResult result,
        DnsMasterFileRecord? record,
        DnsZoneLoadOptions options,
        string? sourcePath,
        int line)
    {
        if (record == null || record.Tokens.Count == 0) return null;
        bool directive = record.Tokens[0].RawText.StartsWith(
            "$", System.StringComparison.Ordinal);
        if (!directive &&
            result.ResourceRecordCount >= options.MaximumRecords)
        {
            return FailureWithPartial(
                result,
                sourcePath,
                DnsDiagnosticCode.ResourceLimitExceeded,
                "The master file exceeds the configured record limit.",
                line,
                1);
        }

        result.Records.Add(record);
        if (!directive)
            result.ResourceRecordCount = result.ResourceRecordCount + 1;
        return null;
    }

    static DnsMasterFileComment ReadComment(
        string source,
        int start,
        int line,
        int column,
        string? sourcePath)
    {
        int length = ReadCommentLength(source, start);
        DnsMasterFileComment comment = new DnsMasterFileComment();
        comment.Text = source.Substring(start + 1, length - 1);
        comment.SourcePath = sourcePath;
        comment.Line = line;
        comment.Column = column;
        return comment;
    }

    static int ReadCommentLength(string source, int start)
    {
        int index = start;
        while (index < source.Length &&
            source[index] != '\r' && source[index] != '\n')
        {
            index = index + 1;
        }

        return index - start;
    }

    static DnsMasterFileTokenReadResult TokenFailure(
        string? sourcePath,
        string reason,
        int line,
        int column)
    {
        DnsMasterFileTokenReadResult result =
            new DnsMasterFileTokenReadResult();
        result.Diagnostic = CreateDiagnostic(
            sourcePath,
            DnsDiagnosticCode.InvalidText,
            reason,
            line,
            column);
        return result;
    }

    static DnsMasterFileTokenReadResult TokenLimitFailure(
        string? sourcePath,
        int line,
        int column)
    {
        DnsMasterFileTokenReadResult result =
            new DnsMasterFileTokenReadResult();
        result.Diagnostic = CreateDiagnostic(
            sourcePath,
            DnsDiagnosticCode.ResourceLimitExceeded,
            "A master-file token exceeds the supported length limit.",
            line,
            column);
        return result;
    }

    static DnsMasterFileTokenizeResult Failure(
        string? sourcePath,
        DnsDiagnosticCode code,
        string reason,
        int line,
        int column)
    {
        DnsMasterFileTokenizeResult result =
            new DnsMasterFileTokenizeResult();
        result.Diagnostics.Add(CreateDiagnostic(
            sourcePath, code, reason, line, column));
        return result;
    }

    static DnsMasterFileTokenizeResult FailureWithPartial(
        DnsMasterFileTokenizeResult result,
        string? sourcePath,
        DnsDiagnosticCode code,
        string reason,
        int line,
        int column)
    {
        result.Diagnostics.Add(CreateDiagnostic(
            sourcePath, code, reason, line, column));
        return result;
    }

    static DnsZoneDiagnostic CreateDiagnostic(
        string? sourcePath,
        DnsDiagnosticCode code,
        string reason,
        int line,
        int column)
    {
        DnsZoneDiagnostic diagnostic = new DnsZoneDiagnostic();
        diagnostic.Code = code;
        diagnostic.Reason = reason;
        diagnostic.SourcePath = sourcePath;
        diagnostic.HasLine = true;
        diagnostic.Line = line;
        diagnostic.HasColumn = true;
        diagnostic.Column = column;
        return diagnostic;
    }
}
