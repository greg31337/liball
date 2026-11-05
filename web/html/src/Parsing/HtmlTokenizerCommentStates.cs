using System;

namespace html;

internal class HtmlCommentTokenBuilder
{
    internal CharacterBuffer data;

    internal HtmlCommentTokenBuilder(int maximumCharacters)
    {
        if (maximumCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumCharacters", maximumCharacters,
                "A maximum comment length must be positive.");
        }

        int initialCapacity = maximumCharacters < 16 ? maximumCharacters : 16;
        this.data = CharacterBuffer.Create(initialCapacity, maximumCharacters);
    }

    internal bool Append(char value)
    {
        return this.data.TryAppend(value);
    }

    internal bool AppendString(string value)
    {
        return this.data.TryAppendString(value);
    }

    internal string GetData()
    {
        return this.data.GetString();
    }
}

internal static class HtmlTokenizerCommentStates
{
    internal static bool Run(HtmlTokenizerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (context.GetAvailableInputSpan().Length == 0)
        {
            if (!context.IsInputComplete())
            {
                return false;
            }

            return HandleEndOfInput(context);
        }

        switch (context.GetState())
        {
            case HtmlTokenizerState.MarkupDeclarationOpen:
                return RunMarkupDeclarationOpen(context);
            case HtmlTokenizerState.BogusComment:
                return RunBogusComment(context);
            case HtmlTokenizerState.CommentStart:
                return RunCommentStart(context);
            case HtmlTokenizerState.CommentStartDash:
                return RunCommentStartDash(context);
            case HtmlTokenizerState.Comment:
                return RunComment(context);
            case HtmlTokenizerState.CommentLessThanSign:
                return RunCommentLessThanSign(context);
            case HtmlTokenizerState.CommentLessThanSignBang:
                return RunCommentLessThanSignBang(context);
            case HtmlTokenizerState.CommentLessThanSignBangDash:
                return RunCommentLessThanSignBangDash(context);
            case HtmlTokenizerState.CommentLessThanSignBangDashDash:
                return RunCommentLessThanSignBangDashDash(context);
            case HtmlTokenizerState.CommentEndDash:
                return RunCommentEndDash(context);
            case HtmlTokenizerState.CommentEnd:
                return RunCommentEnd(context);
            case HtmlTokenizerState.CommentEndBang:
                return RunCommentEndBang(context);
            case HtmlTokenizerState.CdataSection:
                return RunCdataSection(context);
            case HtmlTokenizerState.CdataSectionBracket:
                return RunCdataSectionBracket(context);
            case HtmlTokenizerState.CdataSectionEnd:
                return RunCdataSectionEnd(context);
            default:
                throw new InvalidOperationException(
                    "The current tokenizer state is not a comment state.");
        }
    }

    internal static bool RunMarkupDeclarationOpen(HtmlTokenizerContext context)
    {
        ReadOnlySpan<char> available = context.GetAvailableInputSpan();
        if (StartsWith(available, "--", false))
        {
            Consume(context, 2);
            context.BeginComment();
            context.SetState(HtmlTokenizerState.CommentStart);
            return true;
        }

        if (StartsWith(available, "DOCTYPE", true))
        {
            Consume(context, 7);
            context.BeginDoctype();
            context.SetState(HtmlTokenizerState.Doctype);
            return true;
        }

        if (StartsWith(available, "[CDATA[", false))
        {
            Consume(context, 7);
            if (context.GetCdataAllowed())
            {
                context.BeginCdataSection();
                context.SetState(HtmlTokenizerState.CdataSection);
                return true;
            }

            context.BeginComment();
            context.SetState(HtmlTokenizerState.BogusComment);
            context.AddTokenizerDiagnosticWithCode(HtmlDiagnosticCode.TokenizerMalformedComment,
                "A CDATA section in HTML content was treated as a bogus comment.",
                context.GetMarkupStart(), context.GetInputPosition());
            return Append(context, "[CDATA[");
        }

        if (!context.IsInputComplete()
            && (IsPossiblePrefix(available, "--", false)
                || IsPossiblePrefix(available, "DOCTYPE", true)
                || IsPossiblePrefix(available, "[CDATA[", false)))
        {
            return false;
        }

        int position = context.GetInputPosition();
        context.BeginComment();
        context.AddTokenizerDiagnosticWithCode(HtmlDiagnosticCode.TokenizerMalformedComment,
            "A markup declaration was treated as a bogus comment.",
            context.GetMarkupStart(), position + 1);
        context.SetState(HtmlTokenizerState.BogusComment);
        return true;
    }

    internal static bool RunCdataSection(HtmlTokenizerContext context)
    {
        ReadOnlySpan<char> available = context.GetAvailableInputSpan();
        int length = 0;
        int maximum = context.GetMaximumTokenCharacters();
        while (length < available.Length && length < maximum
            && available[length] != ']')
        {
            length = length + 1;
        }

        if (length > 0)
        {
            int start = context.GetInputPosition();
            Consume(context, length);
            string data = new string(available.Slice(0, length));
            return context.EmitCdataCharacters(data, start, start + length);
        }

        int bracketStart = context.GetInputPosition();
        context.ConsumeInput();
        context.SetCdataPendingStart(bracketStart);
        context.SetState(HtmlTokenizerState.CdataSectionBracket);
        return true;
    }

    internal static bool RunCdataSectionBracket(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == ']')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CdataSectionEnd);
            return true;
        }

        int start = context.GetCdataPendingStart();
        context.SetState(HtmlTokenizerState.CdataSection);
        return context.EmitCdataCharacters("]", start, start + 1);
    }

    internal static bool RunCdataSectionEnd(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == ']')
        {
            int start = context.GetCdataPendingStart();
            context.ConsumeInput();
            context.SetCdataPendingStart(start + 1);
            return context.EmitCdataCharacters("]", start, start + 1);
        }

        if (current == '>')
        {
            context.ConsumeInput();
            context.EndCdataSection();
            return true;
        }

        int pendingStart = context.GetCdataPendingStart();
        context.SetCdataPendingStart(pendingStart + 1);
        context.SetState(HtmlTokenizerState.CdataSectionBracket);
        return context.EmitCdataCharacters("]", pendingStart, pendingStart + 1);
    }

    internal static bool RunBogusComment(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        context.ConsumeInput();
        if (current == '>')
        {
            return context.EmitCurrentComment();
        }

        return Append(context, current);
    }

    internal static bool RunCommentStart(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CommentStartDash);
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            AddMalformedCommentDiagnostic(context,
                "An empty comment was abruptly closed.");
            return context.EmitCurrentComment();
        }

        context.SetState(HtmlTokenizerState.Comment);
        return true;
    }

    internal static bool RunCommentStartDash(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CommentEnd);
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            AddMalformedCommentDiagnostic(context,
                "An empty comment was abruptly closed.");
            return context.EmitCurrentComment();
        }

        context.SetState(HtmlTokenizerState.Comment);
        return Append(context, '-');
    }

    internal static bool RunComment(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '<')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CommentLessThanSign);
            return Append(context, current);
        }

        if (current == '-')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CommentEndDash);
            return true;
        }

        context.ConsumeInput();
        return Append(context, current);
    }

    internal static bool RunCommentLessThanSign(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '!')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CommentLessThanSignBang);
            return Append(context, current);
        }

        if (current == '<')
        {
            context.ConsumeInput();
            return Append(context, current);
        }

        context.SetState(HtmlTokenizerState.Comment);
        return true;
    }

    internal static bool RunCommentLessThanSignBang(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CommentLessThanSignBangDash);
            return true;
        }

        context.SetState(HtmlTokenizerState.Comment);
        return true;
    }

    internal static bool RunCommentLessThanSignBangDash(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CommentLessThanSignBangDashDash);
            return true;
        }

        context.SetState(HtmlTokenizerState.CommentEndDash);
        return true;
    }

    internal static bool RunCommentLessThanSignBangDashDash(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current != '>')
        {
            AddMalformedCommentDiagnostic(context,
                "A nested comment opener appeared inside a comment.");
        }

        context.SetState(HtmlTokenizerState.CommentEnd);
        return true;
    }

    internal static bool RunCommentEndDash(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CommentEnd);
            return true;
        }

        context.SetState(HtmlTokenizerState.Comment);
        return Append(context, '-');
    }

    internal static bool RunCommentEnd(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentComment();
        }

        if (current == '!')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CommentEndBang);
            return true;
        }

        if (current == '-')
        {
            context.ConsumeInput();
            return Append(context, '-');
        }

        context.SetState(HtmlTokenizerState.Comment);
        return Append(context, "--");
    }

    internal static bool RunCommentEndBang(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.CommentEndDash);
            return Append(context, "--!");
        }

        if (current == '>')
        {
            context.ConsumeInput();
            AddMalformedCommentDiagnostic(context,
                "A comment was incorrectly closed with --!>.");
            return context.EmitCurrentComment();
        }

        context.SetState(HtmlTokenizerState.Comment);
        return Append(context, "--!");
    }

    internal static bool HandleEndOfInput(HtmlTokenizerContext context)
    {
        HtmlTokenizerState state = context.GetState();
        if (state == HtmlTokenizerState.MarkupDeclarationOpen)
        {
            context.BeginComment();
            AddMalformedCommentDiagnostic(context,
                "The input ended inside a markup declaration.");
            return context.EmitCurrentComment();
        }

        if (state == HtmlTokenizerState.BogusComment)
        {
            return context.EmitCurrentComment();
        }

        if (state == HtmlTokenizerState.CdataSectionBracket)
        {
            int start = context.GetCdataPendingStart();
            context.SetState(HtmlTokenizerState.CdataSection);
            return context.EmitCdataCharacters("]", start, start + 1);
        }

        if (state == HtmlTokenizerState.CdataSectionEnd)
        {
            int start = context.GetCdataPendingStart();
            context.SetCdataPendingStart(start + 1);
            context.SetState(HtmlTokenizerState.CdataSectionBracket);
            return context.EmitCdataCharacters("]", start, start + 1);
        }

        if (state == HtmlTokenizerState.CdataSection)
        {
            context.AddTokenizerDiagnostic("The input ended inside a CDATA section.",
                context.GetMarkupStart(), context.GetInputPosition());
            context.EndCdataSection();
            return true;
        }

        AddMalformedCommentDiagnostic(context,
            "The input ended before the comment was closed.");
        return context.EmitCurrentComment();
    }

    internal static bool Append(HtmlTokenizerContext context, char value)
    {
        if (!context.GetCurrentComment().Append(value))
        {
            context.MarkTokenCharacterLimitExceeded();
            return false;
        }

        return true;
    }

    internal static bool Append(HtmlTokenizerContext context, string value)
    {
        if (!context.GetCurrentComment().AppendString(value))
        {
            context.MarkTokenCharacterLimitExceeded();
            return false;
        }

        return true;
    }

    internal static void AddMalformedCommentDiagnostic(HtmlTokenizerContext context,
        string message)
    {
        context.AddTokenizerDiagnosticWithCode(HtmlDiagnosticCode.TokenizerMalformedComment,
            message, context.GetMarkupStart(), context.GetInputPosition());
    }

    internal static bool StartsWith(ReadOnlySpan<char> available,
        string expected, bool ignoreCase)
    {
        if (available.Length < expected.Length)
        {
            return false;
        }

        ReadOnlySpan<char> candidate = available.Slice(0, expected.Length);
        if (ignoreCase)
        {
            return Ascii.EqualsIgnoreCase(candidate, expected.AsSpan());
        }

        return candidate.SequenceEqual(expected.AsSpan());
    }

    internal static bool IsPossiblePrefix(ReadOnlySpan<char> available,
        string expected, bool ignoreCase)
    {
        if (available.Length >= expected.Length)
        {
            return false;
        }

        ReadOnlySpan<char> prefix = expected.AsSpan(0, available.Length);
        if (ignoreCase)
        {
            return Ascii.EqualsIgnoreCase(available, prefix);
        }

        return available.SequenceEqual(prefix);
    }

    internal static void Consume(HtmlTokenizerContext context, int count)
    {
        int index = 0;
        while (index < count)
        {
            context.ConsumeInput();
            index = index + 1;
        }
    }
}
