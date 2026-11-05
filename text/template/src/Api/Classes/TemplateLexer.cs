using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Scans template source into tokens according to a syntax profile while collecting recoverable diagnostics.
/// </summary>
public class TemplateLexer : ITemplateLexer
{
    /// <summary>
    /// Stores the template source associated with this value.
    /// </summary>
    internal TemplateSource Source;
    /// <summary>
    /// Specifies the profile for this lexer.
    /// </summary>
    internal TemplateProfile Profile;
    internal string ActiveOutputStartDelimiter;
    internal string ActiveOutputEndDelimiter;
    internal string ActiveStatementStartDelimiter;
    internal string ActiveStatementEndDelimiter;
    internal string ActiveCommentStartDelimiter;
    internal string ActiveCommentEndDelimiter;
    internal ITemplateDiagnosticSink? DiagnosticSink;

    /// <summary>
    /// Initializes a new template lexer and validates any supplied values.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateLexer(TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        source.Validate();
        Source = source;
        Profile = NativeProfile.Create();
        ActiveOutputStartDelimiter = "";
        ActiveOutputEndDelimiter = "";
        ActiveStatementStartDelimiter = "";
        ActiveStatementEndDelimiter = "";
        ActiveCommentStartDelimiter = "";
        ActiveCommentEndDelimiter = "";
        DiagnosticSink = null;
    }

    /// <summary>
    /// Sets the sink that receives lexer diagnostics as they are created.
    /// </summary>
    /// <param name="diagnosticSink">The sink that receives diagnostics.</param>
    public void SetDiagnosticSink(ITemplateDiagnosticSink diagnosticSink)
    {
        if (diagnosticSink == null)
        {
            throw new ArgumentNullException("diagnosticSink");
        }

        DiagnosticSink = diagnosticSink;
    }

    /// <summary>
    /// Configures the syntax profile used by the lexer.
    /// </summary>
    /// <param name="profile">The syntax profile used to recognize template tags.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>profile</c> is <c>null</c>.</exception>
    public void Configure(TemplateProfile profile)
    {
        if (profile == null)
        {
            throw new ArgumentNullException("profile");
        }

        profile.Validate();
        Profile = profile;
    }

    /// <summary>
    /// Creates a lexer that uses the supplied syntax profile.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <param name="profile">The syntax profile used to recognize template tags.</param>
    /// <returns>The configured lexer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateLexer Create(TemplateSource source, TemplateProfile profile)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (profile == null)
        {
            throw new ArgumentNullException("profile");
        }

        profile.Validate();
        TemplateLexer lexer = new TemplateLexer(source);
        lexer.Profile = profile;
        return lexer;
    }

    /// <summary>
    /// Tokenizes native template text and returns tokens with recoverable diagnostics.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The result of the operation.</returns>
    internal static TemplateLexerResult LexNative(string text)
    {
        TemplateSource source = TemplateSource.FromString(text);
        TemplateLexer lexer = new TemplateLexer(source);
        return lexer.Lex();
    }

    /// <summary>
    /// Tokenizes the configured source using its profile and returns recoverable diagnostics with the tokens.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public TemplateLexerResult Lex()
    {
        TemplateLexerResult result = new TemplateLexerResult();
        ResetActiveDelimiters();
        int index = 0;
        bool inTag = false;
        string currentEndDelimiter = "";
        TemplateTokenKind currentEndKind = TemplateTokenKind.Unknown;
        TemplateTokenKind currentStartKind = TemplateTokenKind.Unknown;
        int currentTagTokenStartIndex = -1;
        int currentTagStartOffset = -1;
        TemplateTrimKind currentLeftTrimKind = TemplateTrimKind.None;
        TemplateTrimKind currentRightTrimKind = TemplateTrimKind.None;

        while (index < Source.Length)
        {
            if (!inTag)
            {
                TemplateDelimiterMatch match = FindNextTagStart(index);
                TemplateDelimiterMatch escapedMatch = FindNextEscapedTagStart(index);
                if (escapedMatch.Index >= 0 && (match.Index < 0 || escapedMatch.Index <= match.Index))
                {
                    if (escapedMatch.Index > index)
                    {
                        AddToken(result, TemplateTokenKind.Text, index, escapedMatch.Index - index);
                    }

                    AddEscapedTextToken(result, escapedMatch.Index, escapedMatch.Length + 1, escapedMatch.StartText);
                    index = escapedMatch.Index + escapedMatch.Length + 1;
                }
                else if (match.Index < 0)
                {
                    AddToken(result, TemplateTokenKind.Text, index, Source.Length - index);
                    index = Source.Length;
                }
                else
                {
                    TemplateDelimiterChangeMatch delimiterChange = MatchDelimiterChange(match.Index);
                    if (delimiterChange.Matched)
                    {
                        if (match.Index > index)
                        {
                            AddToken(result, TemplateTokenKind.Text, index, match.Index - index);
                        }

                        TemplateToken changeToken = AddToken(result, TemplateTokenKind.DelimiterChange, delimiterChange.Start, delimiterChange.End - delimiterChange.Start);
                        if (Profile.StandaloneStatementTrimming && IsStandaloneLine(delimiterChange.Start, delimiterChange.End))
                        {
                            changeToken.StandaloneLine = true;
                        }

                        if (delimiterChange.Valid)
                        {
                            ApplyDelimiterChange(delimiterChange);
                        }
                        else
                        {
                            AddInvalidDelimiterChangeDiagnostic(result, delimiterChange.Start, delimiterChange.End);
                        }

                        index = delimiterChange.End;
                        continue;
                    }

                    TemplateBlockTagMatch rawOpening = MatchStatementKeywordTag(match.Index, Profile.RawBlockStartKeyword);
                    if (match.StartKind == TemplateTokenKind.StatementStartDelimiter && rawOpening.Matched)
                    {
                        if (match.Index > index)
                        {
                            AddToken(result, TemplateTokenKind.Text, index, match.Index - index);
                        }

                        index = LexLiteralBlock(result, rawOpening, Profile.RawBlockEndKeyword, "raw");
                        continue;
                    }

                    TemplateBlockTagMatch verbatimOpening = MatchStatementKeywordTag(match.Index, Profile.VerbatimBlockStartKeyword);
                    if (match.StartKind == TemplateTokenKind.StatementStartDelimiter && verbatimOpening.Matched)
                    {
                        if (match.Index > index)
                        {
                            AddToken(result, TemplateTokenKind.Text, index, match.Index - index);
                        }

                        index = LexLiteralBlock(result, verbatimOpening, Profile.VerbatimBlockEndKeyword, "verbatim");
                        continue;
                    }

                    if (match.Index > index)
                    {
                        AddToken(result, TemplateTokenKind.Text, index, match.Index - index);
                    }

                    TemplateToken startToken = AddToken(result, match.StartKind, match.Index, match.Length);
                    index = match.Index + match.Length;
                    inTag = true;
                    currentEndDelimiter = match.EndText;
                    currentEndKind = match.EndKind;
                    currentStartKind = match.StartKind;
                    currentTagTokenStartIndex = result.Tokens.Count - 1;
                    currentTagStartOffset = match.Index;
                    currentLeftTrimKind = TemplateTrimKind.None;
                    currentRightTrimKind = TemplateTrimKind.None;

                    TemplateTrimKind leftTrimKind = GetLeftTrimKindAt(index, currentEndDelimiter);
                    if (leftTrimKind != TemplateTrimKind.None)
                    {
                        TemplateToken trimToken = AddToken(result, TemplateTokenKind.LeftTrimMarker, index, 1);
                        trimToken.TrimLeft = true;
                        trimToken.LeftTrimKind = leftTrimKind;
                        startToken.TrimLeft = true;
                        startToken.LeftTrimKind = leftTrimKind;
                        currentLeftTrimKind = leftTrimKind;
                        index = index + 1;
                    }
                }
            }
            else
            {
                if (currentStartKind == TemplateTokenKind.CommentStartDelimiter)
                {
                    index = LexCommentToEnd(result, index, currentEndDelimiter, currentEndKind, currentTagTokenStartIndex, currentStartKind, currentTagStartOffset, currentLeftTrimKind);
                    inTag = false;
                    currentStartKind = TemplateTokenKind.Unknown;
                    currentTagTokenStartIndex = -1;
                    currentTagStartOffset = -1;
                }
                else if (StartsWithAt(index, currentEndDelimiter))
                {
                    TemplateToken endToken = AddToken(result, currentEndKind, index, currentEndDelimiter.Length);
                    if (currentRightTrimKind != TemplateTrimKind.None)
                    {
                        endToken.TrimRight = true;
                        endToken.RightTrimKind = currentRightTrimKind;
                    }

                    MarkCurrentTagMetadata(result, currentTagTokenStartIndex, result.Tokens.Count - 1, currentStartKind, currentTagStartOffset, endToken.Span.End, currentLeftTrimKind, currentRightTrimKind);
                    index = index + currentEndDelimiter.Length;
                    inTag = false;
                    currentStartKind = TemplateTokenKind.Unknown;
                    currentTagTokenStartIndex = -1;
                    currentTagStartOffset = -1;
                }
                else if (GetRightTrimKindAt(index, currentEndDelimiter) != TemplateTrimKind.None)
                {
                    TemplateTrimKind rightTrimKind = GetRightTrimKindAt(index, currentEndDelimiter);
                    TemplateToken trimToken = AddToken(result, TemplateTokenKind.RightTrimMarker, index, 1);
                    trimToken.TrimRight = true;
                    trimToken.RightTrimKind = rightTrimKind;
                    currentRightTrimKind = rightTrimKind;
                    index = index + 1;
                }
                else if (IsRecoveryTagStartAt(index))
                {
                    AddUnterminatedTagDiagnostic(result, currentTagStartOffset, index, currentEndDelimiter);
                    inTag = false;
                    currentStartKind = TemplateTokenKind.Unknown;
                    currentTagTokenStartIndex = -1;
                    currentTagStartOffset = -1;
                    currentLeftTrimKind = TemplateTrimKind.None;
                    currentRightTrimKind = TemplateTrimKind.None;
                }
                else if (char.IsWhiteSpace(Source.Text[index]))
                {
                    index = index + 1;
                }
                else
                {
                    index = LexTagContentToken(result, index);
                }
            }
        }

        if (inTag)
        {
            AddUnterminatedTagDiagnostic(result, currentTagStartOffset, Source.Length, currentEndDelimiter);
        }

        AddToken(result, TemplateTokenKind.EndOfFile, Source.Length, 0);
        return result;
    }

    internal int LexCommentToEnd(TemplateLexerResult result, int index, string currentEndDelimiter, TemplateTokenKind currentEndKind, int currentTagTokenStartIndex, TemplateTokenKind currentStartKind, int currentTagStartOffset, TemplateTrimKind leftTrimKind)
    {
        int endIndex = IndexOf(currentEndDelimiter, index);
        if (endIndex < 0)
        {
            if (index < Source.Length)
            {
                AddToken(result, TemplateTokenKind.CommentText, index, Source.Length - index);
            }

            AddUnterminatedTagDiagnostic(result, currentTagStartOffset, Source.Length, currentEndDelimiter);
            return Source.Length;
        }

        TemplateTrimKind rightTrimKind = TemplateTrimKind.None;
        int contentEnd = endIndex;
        if (endIndex > index)
        {
            rightTrimKind = GetRightTrimKindAt(endIndex - 1, currentEndDelimiter);
            if (rightTrimKind != TemplateTrimKind.None)
            {
                contentEnd = endIndex - 1;
            }
        }

        if (contentEnd > index)
        {
            AddToken(result, TemplateTokenKind.CommentText, index, contentEnd - index);
        }

        if (rightTrimKind != TemplateTrimKind.None)
        {
            TemplateToken trimToken = AddToken(result, TemplateTokenKind.RightTrimMarker, contentEnd, 1);
            trimToken.TrimRight = true;
            trimToken.RightTrimKind = rightTrimKind;
        }

        TemplateToken endToken = AddToken(result, currentEndKind, endIndex, currentEndDelimiter.Length);
        if (rightTrimKind != TemplateTrimKind.None)
        {
            endToken.TrimRight = true;
            endToken.RightTrimKind = rightTrimKind;
        }

        MarkCurrentTagMetadata(result, currentTagTokenStartIndex, result.Tokens.Count - 1, currentStartKind, currentTagStartOffset, endToken.Span.End, leftTrimKind, rightTrimKind);
        return endIndex + currentEndDelimiter.Length;
    }

    internal int LexLiteralBlock(TemplateLexerResult result, TemplateBlockTagMatch opening, string endKeyword, string blockName)
    {
        TemplateToken startToken = AddToken(result, TemplateTokenKind.RawBlockStartDelimiter, opening.Start, opening.End - opening.Start);
        SetTrimMetadata(startToken, opening.LeftTrimKind, opening.RightTrimKind);
        startToken.StandaloneLine = Profile.StandaloneStatementTrimming && IsStandaloneLine(opening.Start, opening.End);

        TemplateBlockTagMatch closing = FindLiteralBlockEnd(opening.End, endKeyword);
        if (!closing.Matched)
        {
            if (opening.End < Source.Length)
            {
                AddToken(result, TemplateTokenKind.RawText, opening.End, Source.Length - opening.End);
            }

            AddUnterminatedLiteralBlockDiagnostic(result, opening.Start, endKeyword, blockName);
            return Source.Length;
        }

        if (closing.Start > opening.End)
        {
            AddToken(result, TemplateTokenKind.RawText, opening.End, closing.Start - opening.End);
        }

        TemplateToken endToken = AddToken(result, TemplateTokenKind.RawBlockEndDelimiter, closing.Start, closing.End - closing.Start);
        SetTrimMetadata(endToken, closing.LeftTrimKind, closing.RightTrimKind);
        endToken.StandaloneLine = Profile.StandaloneStatementTrimming && IsStandaloneLine(closing.Start, closing.End);
        return closing.End;
    }

    internal bool HasLeftTrimMarkerAt(int index, string currentEndDelimiter)
    {
        return GetLeftTrimKindAt(index, currentEndDelimiter) != TemplateTrimKind.None;
    }

    internal TemplateTrimKind GetLeftTrimKindAt(int index, string currentEndDelimiter)
    {
        if (!Profile.SupportsWhitespaceTrim)
        {
            return TemplateTrimKind.None;
        }

        if (index >= Source.Length)
        {
            return TemplateTrimKind.None;
        }

        if (StartsWithAt(index, currentEndDelimiter))
        {
            return TemplateTrimKind.None;
        }

        if (Source.Text[index] == Profile.LeftTrimMarker)
        {
            return TemplateTrimKind.Greedy;
        }

        if (Source.Text[index] == Profile.NonGreedyLeftTrimMarker)
        {
            return TemplateTrimKind.NonGreedy;
        }

        return TemplateTrimKind.None;
    }

    internal bool HasRightTrimMarkerAt(int index, string currentEndDelimiter)
    {
        return GetRightTrimKindAt(index, currentEndDelimiter) != TemplateTrimKind.None;
    }

    internal TemplateTrimKind GetRightTrimKindAt(int index, string currentEndDelimiter)
    {
        if (!Profile.SupportsWhitespaceTrim)
        {
            return TemplateTrimKind.None;
        }

        if (index >= Source.Length)
        {
            return TemplateTrimKind.None;
        }

        if (!StartsWithAt(index + 1, currentEndDelimiter))
        {
            return TemplateTrimKind.None;
        }

        if (Source.Text[index] == Profile.RightTrimMarker)
        {
            return TemplateTrimKind.Greedy;
        }

        if (Source.Text[index] == Profile.NonGreedyRightTrimMarker)
        {
            return TemplateTrimKind.NonGreedy;
        }

        return TemplateTrimKind.None;
    }

    internal int LexTagContentToken(TemplateLexerResult result, int index)
    {
        char current = Source.Text[index];

        if (IsIdentifierStartAt(index))
        {
            return LexIdentifier(result, index);
        }

        if (IsDigitAt(index))
        {
            return LexNumber(result, index);
        }

        if (current == '\'' || current == '"')
        {
            return LexString(result, index);
        }

        if (current == '=' && index + 1 < Source.Length && Source.Text[index + 1] == '=')
        {
            return LexOperator(result, index);
        }

        TemplateTokenKind punctuationKind = GetPunctuationKind(current);
        if (punctuationKind != TemplateTokenKind.Unknown)
        {
            AddToken(result, punctuationKind, index, 1);
            return index + 1;
        }

        if (IsOperatorCharacter(current))
        {
            return LexOperator(result, index);
        }

        int scalarLength = GetScalarLength(index);
        if (IsInvalidSurrogateAt(index))
        {
            AddInvalidUnicodeDiagnostic(result, index);
        }

        AddToken(result, TemplateTokenKind.Unknown, index, scalarLength);
        return index + scalarLength;
    }

    internal int LexIdentifier(TemplateLexerResult result, int start)
    {
        int index = start + GetScalarLength(start);
        while (index < Source.Length && IsIdentifierPartAt(index))
        {
            index = index + GetScalarLength(index);
        }

        string text = Source.Text.Substring(start, index - start);
        TemplateTokenKind kind = TemplateTokenKind.Identifier;
        if (text == "true" || text == "false")
        {
            kind = TemplateTokenKind.BooleanLiteral;
        }
        else if (text == "null")
        {
            kind = TemplateTokenKind.NullLiteral;
        }
        else if (text == "empty")
        {
            kind = TemplateTokenKind.EmptyLiteral;
        }

        AddToken(result, kind, start, index - start);
        return index;
    }

    internal int LexNumber(TemplateLexerResult result, int start)
    {
        int index = start;
        bool hasDecimalPoint = false;

        while (index < Source.Length)
        {
            if (IsDigitAt(index))
            {
                index = index + GetScalarLength(index);
            }
            else if (!hasDecimalPoint && Source.Text[index] == '.' && index + 1 < Source.Length && char.IsDigit(Source.Text[index + 1]))
            {
                hasDecimalPoint = true;
                index = index + 1;
            }
            else
            {
                break;
            }
        }

        AddToken(result, TemplateTokenKind.NumberLiteral, start, index - start);
        return index;
    }

    internal int LexString(TemplateLexerResult result, int start)
    {
        char quote = Source.Text[start];
        int index = start + 1;
        while (index < Source.Length)
        {
            if (Source.Text[index] == '\\')
            {
                index = index + 1;
                if (index < Source.Length)
                {
                    index = index + 1;
                }
            }
            else if (Source.Text[index] == quote)
            {
                index = index + 1;
                break;
            }
            else
            {
                index = index + 1;
            }
        }

        AddToken(result, TemplateTokenKind.StringLiteral, start, index - start);
        return index;
    }

    internal int LexOperator(TemplateLexerResult result, int start)
    {
        int index = start + 1;
        while (index < Source.Length && IsOperatorCharacter(Source.Text[index]))
        {
            if (StartsWithAt(index, ActiveOutputEndDelimiter)
                || StartsWithAt(index, ActiveStatementEndDelimiter)
                || StartsWithAt(index, ActiveCommentEndDelimiter))
            {
                break;
            }

            index = index + 1;
        }

        AddToken(result, TemplateTokenKind.Operator, start, index - start);
        return index;
    }

    internal TemplateDelimiterMatch FindNextTagStart(int startIndex)
    {
        TemplateDelimiterMatch result = new TemplateDelimiterMatch();
        ConsiderDelimiter(result, startIndex, ActiveOutputStartDelimiter, ActiveOutputEndDelimiter, TemplateTokenKind.OutputStartDelimiter, TemplateTokenKind.OutputEndDelimiter);
        ConsiderDelimiter(result, startIndex, ActiveStatementStartDelimiter, ActiveStatementEndDelimiter, TemplateTokenKind.StatementStartDelimiter, TemplateTokenKind.StatementEndDelimiter);
        ConsiderDelimiter(result, startIndex, ActiveCommentStartDelimiter, ActiveCommentEndDelimiter, TemplateTokenKind.CommentStartDelimiter, TemplateTokenKind.CommentEndDelimiter);
        return result;
    }

    internal TemplateDelimiterMatch FindNextEscapedTagStart(int startIndex)
    {
        TemplateDelimiterMatch result = new TemplateDelimiterMatch();
        ConsiderEscapedDelimiter(result, startIndex, ActiveOutputStartDelimiter, TemplateTokenKind.OutputStartDelimiter);
        ConsiderEscapedDelimiter(result, startIndex, ActiveStatementStartDelimiter, TemplateTokenKind.StatementStartDelimiter);
        ConsiderEscapedDelimiter(result, startIndex, ActiveCommentStartDelimiter, TemplateTokenKind.CommentStartDelimiter);
        return result;
    }

    internal void ConsiderEscapedDelimiter(TemplateDelimiterMatch result, int startIndex, string startText, TemplateTokenKind startKind)
    {
        int delimiterIndex = IndexOf(startText, startIndex);
        while (delimiterIndex >= 0)
        {
            if (IsEscapedDelimiterAt(delimiterIndex))
            {
                int escapeIndex = delimiterIndex - 1;
                if (result.Index < 0 || escapeIndex < result.Index)
                {
                    result.Index = escapeIndex;
                    result.Length = startText.Length;
                    result.StartKind = startKind;
                    result.StartText = startText;
                }

                return;
            }

            delimiterIndex = IndexOf(startText, delimiterIndex + startText.Length);
        }
    }

    internal void ConsiderDelimiter(TemplateDelimiterMatch result, int startIndex, string startText, string endText, TemplateTokenKind startKind, TemplateTokenKind endKind)
    {
        int index = IndexOf(startText, startIndex);
        if (index < 0)
        {
            return;
        }

        if (result.Index >= 0 && (index > result.Index || (index == result.Index && startText.Length <= result.Length)))
        {
            return;
        }

        result.Index = index;
        result.Length = startText.Length;
        result.StartKind = startKind;
        result.EndKind = endKind;
        result.StartText = startText;
        result.EndText = endText;
    }

    internal TemplateBlockTagMatch FindLiteralBlockEnd(int startIndex, string endKeyword)
    {
        int index = startIndex;
        while (index < Source.Length)
        {
            int statementIndex = IndexOf(ActiveStatementStartDelimiter, index);
            if (statementIndex < 0)
            {
                return new TemplateBlockTagMatch();
            }

            TemplateBlockTagMatch match = MatchStatementKeywordTag(statementIndex, endKeyword);
            if (match.Matched)
            {
                return match;
            }

            index = statementIndex + ActiveStatementStartDelimiter.Length;
        }

        return new TemplateBlockTagMatch();
    }

    internal TemplateBlockTagMatch MatchStatementKeywordTag(int startIndex, string keyword)
    {
        TemplateBlockTagMatch result = new TemplateBlockTagMatch();
        if (!StartsWithAt(startIndex, ActiveStatementStartDelimiter))
        {
            return result;
        }

        int index = startIndex + ActiveStatementStartDelimiter.Length;
        TemplateTrimKind leftTrimKind = GetLeftTrimKindAt(index, ActiveStatementEndDelimiter);
        if (leftTrimKind != TemplateTrimKind.None)
        {
            index = index + 1;
        }

        index = SkipWhitespace(index);
        int keywordStart = index;
        while (index < Source.Length && IsIdentifierPart(Source.Text[index]))
        {
            index = index + 1;
        }

        if (keywordStart == index)
        {
            return result;
        }

        string text = Source.Text.Substring(keywordStart, index - keywordStart);
        if (text != keyword)
        {
            return result;
        }

        index = SkipWhitespace(index);
        TemplateTrimKind rightTrimKind = GetRightTrimKindAt(index, ActiveStatementEndDelimiter);
        if (rightTrimKind != TemplateTrimKind.None)
        {
            index = index + 1;
        }

        if (!StartsWithAt(index, ActiveStatementEndDelimiter))
        {
            return result;
        }

        result.Matched = true;
        result.Start = startIndex;
        result.End = index + ActiveStatementEndDelimiter.Length;
        result.LeftTrimKind = leftTrimKind;
        result.RightTrimKind = rightTrimKind;
        return result;
    }

    internal int IndexOf(string value, int startIndex)
    {
        if (startIndex < 0 || startIndex > Source.Length)
        {
            return -1;
        }

        ReadOnlySpan<char> remaining = Source.Text.AsSpan(startIndex);
        int relativeIndex = remaining.IndexOf(value.AsSpan());
        if (relativeIndex < 0)
        {
            return -1;
        }

        return startIndex + relativeIndex;
    }

    internal bool StartsWithAt(int index, string value)
    {
        if (value == null)
        {
            return false;
        }

        if (index < 0)
        {
            return false;
        }

        if (index > Source.Length || value.Length > Source.Length - index)
        {
            return false;
        }

        ReadOnlySpan<char> sourceSpan = Source.Text.AsSpan(index, value.Length);
        return sourceSpan.SequenceEqual(value.AsSpan());
    }

    internal TemplateTokenKind GetPunctuationKind(char value)
    {
        switch (value)
        {
            case '.':
                return TemplateTokenKind.Dot;
            case ',':
                return TemplateTokenKind.Comma;
            case ':':
                return TemplateTokenKind.Colon;
            case ';':
                return TemplateTokenKind.Semicolon;
            case '|':
                return TemplateTokenKind.Pipe;
            case '?':
                return TemplateTokenKind.Question;
            case '(':
                return TemplateTokenKind.OpenParen;
            case ')':
                return TemplateTokenKind.CloseParen;
            case '[':
                return TemplateTokenKind.OpenBracket;
            case ']':
                return TemplateTokenKind.CloseBracket;
            case '{':
                return TemplateTokenKind.OpenBrace;
            case '}':
                return TemplateTokenKind.CloseBrace;
            case '=':
                return TemplateTokenKind.Equals;
            default:
                return TemplateTokenKind.Unknown;
        }
    }

    internal bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    internal bool IsIdentifierStartAt(int index)
    {
        if (IsInvalidSurrogateAt(index))
        {
            return false;
        }

        Rune value = Rune.GetRuneAt(Source.Text, index);
        return Rune.IsLetter(value) || value.Value == '_';
    }

    internal bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    internal bool IsIdentifierPartAt(int index)
    {
        if (IsInvalidSurrogateAt(index))
        {
            return false;
        }

        Rune value = Rune.GetRuneAt(Source.Text, index);
        return Rune.IsLetterOrDigit(value) || value.Value == '_';
    }

    internal bool IsDigitAt(int index)
    {
        if (IsInvalidSurrogateAt(index))
        {
            return false;
        }

        Rune value = Rune.GetRuneAt(Source.Text, index);
        return Rune.IsDigit(value);
    }

    internal int GetScalarLength(int index)
    {
        if (index < 0 || index >= Source.Length)
        {
            return 0;
        }

        char value = Source.Text[index];
        if (char.IsHighSurrogate(value) && index + 1 < Source.Length && char.IsLowSurrogate(Source.Text[index + 1]))
        {
            return 2;
        }

        return 1;
    }

    internal bool IsInvalidSurrogateAt(int index)
    {
        char value = Source.Text[index];
        if (char.IsHighSurrogate(value))
        {
            return index + 1 >= Source.Length || !char.IsLowSurrogate(Source.Text[index + 1]);
        }

        return char.IsLowSurrogate(value);
    }

    internal int SkipWhitespace(int index)
    {
        while (index < Source.Length && char.IsWhiteSpace(Source.Text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal bool IsOperatorCharacter(char value)
    {
        return value == '+'
            || value == '-'
            || value == '*'
            || value == '/'
            || value == '%'
            || value == '$'
            || value == '@'
            || value == '!'
            || value == '<'
            || value == '>'
            || value == '='
            || value == '&';
    }

    internal void MarkCurrentTagMetadata(TemplateLexerResult result, int startTokenIndex, int endTokenIndex, TemplateTokenKind startKind, int tagStartOffset, int tagEndOffset, TemplateTrimKind leftTrimKind, TemplateTrimKind rightTrimKind)
    {
        if (startTokenIndex < 0)
        {
            return;
        }

        if (leftTrimKind != TemplateTrimKind.None)
        {
            result.Tokens[startTokenIndex].TrimLeft = true;
            result.Tokens[startTokenIndex].LeftTrimKind = leftTrimKind;
        }

        if (rightTrimKind != TemplateTrimKind.None)
        {
            result.Tokens[endTokenIndex].TrimRight = true;
            result.Tokens[endTokenIndex].RightTrimKind = rightTrimKind;
        }

        if (startKind == TemplateTokenKind.OutputStartDelimiter)
        {
            return;
        }

        if (!Profile.StandaloneStatementTrimming || !IsStandaloneLine(tagStartOffset, tagEndOffset))
        {
            return;
        }

        int index = startTokenIndex;
        while (index <= endTokenIndex)
        {
            result.Tokens[index].StandaloneLine = true;
            index = index + 1;
        }
    }

    internal bool IsStandaloneLine(int tagStartOffset, int tagEndOffset)
    {
        int before = tagStartOffset - 1;
        while (before >= 0 && Source.Text[before] != '\r' && Source.Text[before] != '\n')
        {
            if (Source.Text[before] != ' ' && Source.Text[before] != '\t')
            {
                return false;
            }

            before = before - 1;
        }

        int after = tagEndOffset;
        while (after < Source.Length && Source.Text[after] != '\r' && Source.Text[after] != '\n')
        {
            if (Source.Text[after] != ' ' && Source.Text[after] != '\t')
            {
                return false;
            }

            after = after + 1;
        }

        return true;
    }

    internal TemplateToken AddToken(TemplateLexerResult result, TemplateTokenKind kind, int start, int length)
    {
        TemplateSourceSpan span = Source.CreateSpan(start, length);
        string text = Source.Text.Substring(start, length);
        TemplateToken token = new TemplateToken(kind, span, text);
        result.Tokens.Add(token);
        return token;
    }

    internal TemplateToken AddEscapedTextToken(TemplateLexerResult result, int start, int length, string text)
    {
        TemplateSourceSpan span = Source.CreateSpan(start, length);
        TemplateToken token = new TemplateToken(TemplateTokenKind.Text, span, text);
        token.Escaped = true;
        result.Tokens.Add(token);
        return token;
    }

    internal void SetTrimMetadata(TemplateToken token, TemplateTrimKind leftTrimKind, TemplateTrimKind rightTrimKind)
    {
        token.LeftTrimKind = leftTrimKind;
        token.RightTrimKind = rightTrimKind;
        token.TrimLeft = leftTrimKind != TemplateTrimKind.None;
        token.TrimRight = rightTrimKind != TemplateTrimKind.None;
    }

    internal void ResetActiveDelimiters()
    {
        ActiveOutputStartDelimiter = Profile.OutputStartDelimiter;
        ActiveOutputEndDelimiter = Profile.OutputEndDelimiter;
        ActiveStatementStartDelimiter = Profile.StatementStartDelimiter;
        ActiveStatementEndDelimiter = Profile.StatementEndDelimiter;
        ActiveCommentStartDelimiter = Profile.CommentStartDelimiter;
        ActiveCommentEndDelimiter = Profile.CommentEndDelimiter;
    }

    internal bool IsEscapedDelimiterAt(int delimiterIndex)
    {
        int slashCount = 0;
        int index = delimiterIndex - 1;
        while (index >= 0 && Source.Text[index] == '\\')
        {
            slashCount = slashCount + 1;
            index = index - 1;
        }

        return slashCount % 2 == 1;
    }

    internal bool IsRecoveryTagStartAt(int index)
    {
        TemplateDelimiterMatch match = FindNextTagStart(index);
        return match.Index == index;
    }

    internal TemplateDelimiterChangeMatch MatchDelimiterChange(int start)
    {
        TemplateDelimiterChangeMatch result = new TemplateDelimiterChangeMatch();
        if (Profile.Name != "mustache" || !StartsWithAt(start, ActiveOutputStartDelimiter))
        {
            return result;
        }

        int contentStart = start + ActiveOutputStartDelimiter.Length;
        int index = SkipWhitespace(contentStart);
        if (index >= Source.Length || Source.Text[index] != '=')
        {
            return result;
        }

        result.Matched = true;
        result.Start = start;
        int endIndex = IndexOf(ActiveOutputEndDelimiter, index + 1);
        if (endIndex < 0)
        {
            result.End = Source.Length;
            return result;
        }

        result.End = endIndex + ActiveOutputEndDelimiter.Length;
        int equalsEnd = endIndex - 1;
        while (equalsEnd > index && char.IsWhiteSpace(Source.Text[equalsEnd]))
        {
            equalsEnd = equalsEnd - 1;
        }

        if (Source.Text[equalsEnd] != '=')
        {
            return result;
        }

        string delimiterText = Source.Text.Substring(index + 1, equalsEnd - index - 1).Trim();
        int separator = FindWhitespace(delimiterText, 0);
        if (separator <= 0)
        {
            return result;
        }

        int secondStart = SkipTextWhitespace(delimiterText, separator);
        if (secondStart >= delimiterText.Length)
        {
            return result;
        }

        int secondEnd = FindWhitespace(delimiterText, secondStart);
        if (secondEnd < 0)
        {
            secondEnd = delimiterText.Length;
        }

        int trailing = SkipTextWhitespace(delimiterText, secondEnd);
        if (trailing != delimiterText.Length)
        {
            return result;
        }

        string outputStart = delimiterText.Substring(0, separator);
        string outputEnd = delimiterText.Substring(secondStart, secondEnd - secondStart);
        if (!IsValidChangedDelimiter(outputStart) || !IsValidChangedDelimiter(outputEnd))
        {
            return result;
        }

        result.Valid = true;
        result.OutputStartDelimiter = outputStart;
        result.OutputEndDelimiter = outputEnd;
        return result;
    }

    internal void ApplyDelimiterChange(TemplateDelimiterChangeMatch match)
    {
        ActiveOutputStartDelimiter = match.OutputStartDelimiter;
        ActiveOutputEndDelimiter = match.OutputEndDelimiter;
        ActiveStatementStartDelimiter = match.OutputStartDelimiter;
        ActiveStatementEndDelimiter = match.OutputEndDelimiter;
        ActiveCommentStartDelimiter = match.OutputStartDelimiter + "!";
        ActiveCommentEndDelimiter = match.OutputEndDelimiter;
    }

    internal bool IsValidChangedDelimiter(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (char.IsWhiteSpace(value[index]) || value[index] == '=')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal int FindWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int SkipTextWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal void AddUnterminatedTagDiagnostic(TemplateLexerResult result, int start, int end, string expectedDelimiter)
    {
        if (start < 0)
        {
            start = Source.Length;
        }

        if (end < start || end > Source.Length)
        {
            end = Source.Length;
        }

        TemplateSourceSpan span = Source.CreateSpan(start, end - start);
        string message = "Unterminated tag. Expected '" + expectedDelimiter + "'.";
        AddDiagnostic(result, new TemplateDiagnostic("LEX001", TemplateDiagnosticSeverity.Error, message, span, "Add the missing closing delimiter."));
    }

    internal void AddUnterminatedLiteralBlockDiagnostic(TemplateLexerResult result, int start, string endKeyword, string blockName)
    {
        TemplateSourceSpan span = Source.CreateSpan(start, Source.Length - start);
        string message = "Unterminated " + blockName + " block. Expected '" + endKeyword + "'.";
        AddDiagnostic(result, new TemplateDiagnostic("LEX002", TemplateDiagnosticSeverity.Error, message, span, "Add the matching literal block closing tag."));
    }

    internal void AddInvalidDelimiterChangeDiagnostic(TemplateLexerResult result, int start, int end)
    {
        TemplateSourceSpan span = Source.CreateSpan(start, end - start);
        AddDiagnostic(result, new TemplateDiagnostic("LEX003", TemplateDiagnosticSeverity.Error, "Invalid Mustache delimiter change.", span, "Use two non-empty delimiters separated by whitespace and surrounded by equals signs."));
    }

    internal void AddInvalidUnicodeDiagnostic(TemplateLexerResult result, int index)
    {
        TemplateSourceSpan span = Source.CreateSpan(index, 1);
        AddDiagnostic(result, new TemplateDiagnostic("LEX004", TemplateDiagnosticSeverity.Error, "Invalid Unicode surrogate code unit.", span, "Replace the unpaired surrogate with a valid Unicode scalar value."));
    }

    internal void AddDiagnostic(TemplateLexerResult result, TemplateDiagnostic diagnostic)
    {
        result.Diagnostics.Add(diagnostic);
        if (DiagnosticSink != null)
        {
            DiagnosticSink.Report(diagnostic);
        }
    }
}
