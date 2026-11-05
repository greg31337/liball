using System;
using System.Collections.Generic;
using System.Text;

namespace sql;

internal class SqlWriter
{
    internal List<string> Lines = new List<string>();
    internal StringBuilder CurrentLine = new StringBuilder();
    internal string IndentText = "    ";
    internal int IndentLevel;
    internal SqlFormatOptions Options = SqlFormatOptions.CreateDefault();

    internal static SqlWriter CreateDefault()
    {
        return new SqlWriter(SqlFormatOptions.CreateDefault());
    }

    internal static SqlWriter Create(string indentText)
    {
        if (indentText == null)
        {
            throw new ArgumentNullException("indentText");
        }

        SqlFormatOptions options = SqlFormatOptions.CreateDefault();
        options.IndentText = indentText;
        return new SqlWriter(options);
    }

    internal SqlWriter(SqlFormatOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        Initialize(options);
    }

    internal static SqlWriter Create(SqlFormatOptions options)
    {
        return new SqlWriter(options);
    }

    internal void Clear()
    {
        Lines.Clear();
        CurrentLine.Clear();
        IndentLevel = 0;
    }

    internal void Indent()
    {
        IndentLevel = IndentLevel + 1;
    }

    internal void Dedent()
    {
        if (IndentLevel == 0)
        {
            throw new InvalidOperationException("Cannot dedent below zero.");
        }

        IndentLevel = IndentLevel - 1;
    }

    internal void SetIndentLevel(int indentLevel)
    {
        if (indentLevel < 0)
        {
            throw new ArgumentOutOfRangeException("indentLevel");
        }

        IndentLevel = indentLevel;
    }

    internal int GetIndentLevel()
    {
        return IndentLevel;
    }

    internal void Write(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length == 0)
        {
            return;
        }

        ApplyIndentation();
        CurrentLine.Append(ApplyKeywordCase(text));
    }

    internal void WriteRaw(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length == 0)
        {
            return;
        }

        ApplyIndentation();
        CurrentLine.Append(text);
    }

    internal void WriteSpace()
    {
        if (CurrentLine.Length == 0)
        {
            return;
        }

        char last = CurrentLine[CurrentLine.Length - 1];
        if (last == ' ' || last == '\t')
        {
            return;
        }

        CurrentLine.Append(' ');
    }

    internal void Space()
    {
        WriteSpace();
    }

    internal void WriteLine()
    {
        Lines.Add(CurrentLine.ToString());
        CurrentLine.Clear();
    }

    internal void WriteTextLine(string text)
    {
        Write(text);
        WriteLine();
    }

    internal void BlankLine()
    {
        if (CurrentLine.Length > 0)
        {
            WriteLine();
        }

        Lines.Add("");
    }

    internal bool HasCurrentLine()
    {
        return CurrentLine.Length > 0;
    }

    internal int GetCurrentLineLength()
    {
        return CurrentLine.Length;
    }

    internal string GetCurrentLineText()
    {
        return CurrentLine.ToString();
    }

    internal string ToSql()
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < Lines.Count)
        {
            if (index > 0)
            {
                builder.Append('\n');
            }

            builder.Append(Lines[index]);
            index = index + 1;
        }

        if (CurrentLine.Length > 0)
        {
            if (Lines.Count > 0)
            {
                builder.Append('\n');
            }

            builder.Append(CurrentLine.ToString());
        }

        string sql = builder.ToString();
        if (Options.Mode == SqlFormatMode.RoundTrip)
        {
            return sql;
        }

        if (Options.Mode == SqlFormatMode.Compact)
        {
            sql = NormalizeOutputWhitespace(sql, false);
        }
        else if (Options.Mode == SqlFormatMode.Minimal)
        {
            sql = NormalizeOutputWhitespace(sql, true);
        }

        if (Options.TargetLineWidth > 0)
        {
            sql = WrapOutputToTargetWidth(sql);
        }

        return sql;
    }

    internal string NormalizeOutputWhitespace(string text, bool minimal)
    {
        StringBuilder builder = new StringBuilder();
        bool pendingSpace = false;
        char quote = '\0';
        bool bracket = false;
        bool lineComment = false;
        bool blockComment = false;
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            char next = '\0';
            if (index + 1 < text.Length)
            {
                next = text[index + 1];
            }

            if (lineComment)
            {
                builder.Append(current);
                if (current == '\n' || current == '\r')
                {
                    lineComment = false;
                }

                index = index + 1;
                continue;
            }

            if (blockComment)
            {
                builder.Append(current);
                if (current == '*' && next == '/')
                {
                    builder.Append(next);
                    index = index + 2;
                    blockComment = false;
                }
                else
                {
                    index = index + 1;
                }

                continue;
            }

            if (quote != '\0')
            {
                builder.Append(current);
                if (current == quote)
                {
                    if (next == quote)
                    {
                        builder.Append(next);
                        index = index + 2;
                        continue;
                    }

                    quote = '\0';
                }

                index = index + 1;
                continue;
            }

            if (bracket)
            {
                builder.Append(current);
                if (current == ']')
                {
                    if (next == ']')
                    {
                        builder.Append(next);
                        index = index + 2;
                        continue;
                    }

                    bracket = false;
                }

                index = index + 1;
                continue;
            }

            if (char.IsWhiteSpace(current))
            {
                pendingSpace = builder.Length > 0;
                index = index + 1;
                continue;
            }

            if (pendingSpace)
            {
                if (current == '-' && next == '-')
                {
                    builder.Append('\n');
                }
                else
                {
                    AppendNormalizedSpace(builder, current, minimal);
                }

                pendingSpace = false;
            }

            if (current == '-' && next == '-')
            {
                builder.Append(current);
                builder.Append(next);
                lineComment = true;
                index = index + 2;
                continue;
            }

            if (current == '/' && next == '*')
            {
                builder.Append(current);
                builder.Append(next);
                blockComment = true;
                index = index + 2;
                continue;
            }

            builder.Append(current);
            if (current == '\'' || current == '"' || current == '`')
            {
                quote = current;
            }
            else if (current == '[')
            {
                bracket = true;
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal void AppendNormalizedSpace(StringBuilder builder, char next, bool minimal)
    {
        if (builder.Length == 0)
        {
            return;
        }

        char previous = builder[builder.Length - 1];
        if ((previous == '-' && next == '-') || (previous == '/' && next == '*') || (previous == '*' && next == '/'))
        {
            builder.Append(' ');
            return;
        }

        if (minimal && (IsMinimalWhitespaceBoundary(previous) || IsMinimalWhitespaceBoundary(next)))
        {
            return;
        }

        builder.Append(' ');
    }

    internal bool IsMinimalWhitespaceBoundary(char value)
    {
        return value == '(' || value == ')' || value == ',' || value == '.' || value == ';' || value == '=' || value == '<' || value == '>' || value == '+' || value == '-' || value == '*' || value == '/' || value == '%' || value == '|' || value == '&' || value == '^';
    }

    internal string WrapOutputToTargetWidth(string text)
    {
        StringBuilder builder = new StringBuilder(text);
        int lineStart = 0;
        int lastBreak = -1;
        char quote = '\0';
        bool bracket = false;
        int index = 0;
        while (index < builder.Length)
        {
            char current = builder[index];
            char next = '\0';
            if (index + 1 < builder.Length)
            {
                next = builder[index + 1];
            }

            if (quote != '\0')
            {
                if (current == quote)
                {
                    if (next == quote)
                    {
                        index = index + 2;
                        continue;
                    }

                    quote = '\0';
                }
            }
            else if (bracket)
            {
                if (current == ']')
                {
                    bracket = false;
                }
            }
            else if (current == '\'' || current == '"' || current == '`')
            {
                quote = current;
            }
            else if (current == '[')
            {
                bracket = true;
            }
            else if (current == ' ' || current == '\t')
            {
                lastBreak = index;
            }
            else if (current == '\n' || current == '\r')
            {
                lineStart = index + 1;
                lastBreak = -1;
            }

            if (index - lineStart + 1 > Options.TargetLineWidth && lastBreak >= lineStart)
            {
                builder[lastBreak] = '\n';
                lineStart = lastBreak + 1;
                lastBreak = -1;
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal void ApplyIndentation()
    {
        if (CurrentLine.Length > 0)
        {
            return;
        }

        int index = 0;
        while (index < IndentLevel)
        {
            CurrentLine.Append(IndentText);
            index = index + 1;
        }
    }

    internal void Initialize(SqlFormatOptions options)
    {
        options.Validate();
        Options = options.Copy();
        Lines = new List<string>();
        CurrentLine = new StringBuilder();
        IndentText = Options.IndentText;
        IndentLevel = 0;
    }

    internal string ApplyKeywordCase(string text)
    {
        if (Options.KeywordCase == SqlKeywordCase.Preserve)
        {
            return text;
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (IsIdentifierStart(current))
            {
                int start = index;
                index = index + 1;
                while (index < text.Length && IsIdentifierPart(text[index]))
                {
                    index = index + 1;
                }

                string word = text.Substring(start, index - start);
                builder.Append(ApplyKeywordCaseToWord(word));
            }
            else
            {
                if (current == '\'' || current == '"' || current == '`')
                {
                    index = AppendQuotedText(builder, text, index, current);
                }
                else
                {
                    if (current == '[')
                    {
                        index = AppendBracketQuotedText(builder, text, index);
                    }
                    else
                    {
                        builder.Append(current);
                        index = index + 1;
                    }
                }
            }
        }

        return builder.ToString();
    }

    internal string ApplyKeywordCaseToWord(string word)
    {
        string? canonical = Options.KeywordDialect.GetCanonicalKeyword(word);
        if (canonical == null)
        {
            return word;
        }

        if (Options.KeywordCase == SqlKeywordCase.Lower)
        {
            return canonical.ToLowerInvariant();
        }

        return canonical;
    }

    internal int AppendQuotedText(StringBuilder builder, string text, int start, char quote)
    {
        int index = start;
        builder.Append(text[index]);
        index = index + 1;
        while (index < text.Length)
        {
            char current = text[index];
            builder.Append(current);
            index = index + 1;
            if (current == quote)
            {
                if (index < text.Length && text[index] == quote)
                {
                    builder.Append(text[index]);
                    index = index + 1;
                }
                else
                {
                    break;
                }
            }
        }

        return index;
    }

    internal int AppendBracketQuotedText(StringBuilder builder, string text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            char current = text[index];
            builder.Append(current);
            index = index + 1;
            if (current == ']')
            {
                break;
            }
        }

        return index;
    }

    internal bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    internal bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }
}
