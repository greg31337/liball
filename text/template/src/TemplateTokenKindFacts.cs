using System;

namespace template;

internal static class TemplateTokenKindFacts
{
    internal static bool IsTagStart(TemplateTokenKind kind)
    {
        return kind == TemplateTokenKind.OutputStartDelimiter
            || kind == TemplateTokenKind.StatementStartDelimiter
            || kind == TemplateTokenKind.CommentStartDelimiter
            || kind == TemplateTokenKind.RawBlockStartDelimiter;
    }

    internal static bool IsTagEnd(TemplateTokenKind kind)
    {
        return kind == TemplateTokenKind.OutputEndDelimiter
            || kind == TemplateTokenKind.StatementEndDelimiter
            || kind == TemplateTokenKind.CommentEndDelimiter
            || kind == TemplateTokenKind.RawBlockEndDelimiter;
    }

    internal static bool IsDelimiter(TemplateTokenKind kind)
    {
        return IsTagStart(kind)
            || IsTagEnd(kind)
            || kind == TemplateTokenKind.Dot
            || kind == TemplateTokenKind.Comma
            || kind == TemplateTokenKind.Colon
            || kind == TemplateTokenKind.Semicolon
            || kind == TemplateTokenKind.Pipe
            || kind == TemplateTokenKind.Question
            || kind == TemplateTokenKind.OpenParen
            || kind == TemplateTokenKind.CloseParen
            || kind == TemplateTokenKind.OpenBracket
            || kind == TemplateTokenKind.CloseBracket
            || kind == TemplateTokenKind.OpenBrace
            || kind == TemplateTokenKind.CloseBrace
            || kind == TemplateTokenKind.Equals
            || kind == TemplateTokenKind.DelimiterChange;
    }

    internal static bool IsTrimMarker(TemplateTokenKind kind)
    {
        return kind == TemplateTokenKind.LeftTrimMarker
            || kind == TemplateTokenKind.RightTrimMarker;
    }

    internal static bool IsLiteral(TemplateTokenKind kind)
    {
        return kind == TemplateTokenKind.StringLiteral
            || kind == TemplateTokenKind.NumberLiteral
            || kind == TemplateTokenKind.BooleanLiteral
            || kind == TemplateTokenKind.NullLiteral
            || kind == TemplateTokenKind.EmptyLiteral;
    }

    internal static bool IsTagContent(TemplateTokenKind kind)
    {
        return kind == TemplateTokenKind.Identifier
            || IsLiteral(kind)
            || kind == TemplateTokenKind.Operator
            || IsDelimiter(kind)
            || IsTrimMarker(kind);
    }

    internal static string GetDisplayName(TemplateTokenKind kind)
    {
        switch (kind)
        {
            case TemplateTokenKind.Unknown:
                return "unknown";
            case TemplateTokenKind.Text:
                return "text";
            case TemplateTokenKind.OutputStartDelimiter:
                return "output start delimiter";
            case TemplateTokenKind.OutputEndDelimiter:
                return "output end delimiter";
            case TemplateTokenKind.StatementStartDelimiter:
                return "statement start delimiter";
            case TemplateTokenKind.StatementEndDelimiter:
                return "statement end delimiter";
            case TemplateTokenKind.CommentStartDelimiter:
                return "comment start delimiter";
            case TemplateTokenKind.CommentEndDelimiter:
                return "comment end delimiter";
            case TemplateTokenKind.RawBlockStartDelimiter:
                return "raw block start delimiter";
            case TemplateTokenKind.RawBlockEndDelimiter:
                return "raw block end delimiter";
            case TemplateTokenKind.LeftTrimMarker:
                return "left trim marker";
            case TemplateTokenKind.RightTrimMarker:
                return "right trim marker";
            case TemplateTokenKind.Identifier:
                return "identifier";
            case TemplateTokenKind.StringLiteral:
                return "string literal";
            case TemplateTokenKind.NumberLiteral:
                return "number literal";
            case TemplateTokenKind.BooleanLiteral:
                return "boolean literal";
            case TemplateTokenKind.NullLiteral:
                return "null literal";
            case TemplateTokenKind.EmptyLiteral:
                return "empty literal";
            case TemplateTokenKind.Operator:
                return "operator";
            case TemplateTokenKind.Dot:
                return "dot";
            case TemplateTokenKind.Comma:
                return "comma";
            case TemplateTokenKind.Colon:
                return "colon";
            case TemplateTokenKind.Semicolon:
                return "semicolon";
            case TemplateTokenKind.Pipe:
                return "pipe";
            case TemplateTokenKind.Question:
                return "question";
            case TemplateTokenKind.OpenParen:
                return "open parenthesis";
            case TemplateTokenKind.CloseParen:
                return "close parenthesis";
            case TemplateTokenKind.OpenBracket:
                return "open bracket";
            case TemplateTokenKind.CloseBracket:
                return "close bracket";
            case TemplateTokenKind.OpenBrace:
                return "open brace";
            case TemplateTokenKind.CloseBrace:
                return "close brace";
            case TemplateTokenKind.Equals:
                return "equals";
            case TemplateTokenKind.CommentText:
                return "comment text";
            case TemplateTokenKind.RawText:
                return "raw text";
            case TemplateTokenKind.EndOfFile:
                return "end of file";
            case TemplateTokenKind.DelimiterChange:
                return "delimiter change";
            default:
                return "unknown";
        }
    }
}
