using System;

namespace html;

internal static class HtmlTokenizerScriptStates
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
            case HtmlTokenizerState.ScriptData:
                return RunScriptData(context);
            case HtmlTokenizerState.ScriptDataLessThanSign:
                return RunScriptDataLessThanSign(context);
            case HtmlTokenizerState.ScriptDataEndTagOpen:
                return RunEndTagOpen(context, HtmlTokenizerState.ScriptDataEndTagName,
                    HtmlTokenizerState.ScriptData);
            case HtmlTokenizerState.ScriptDataEndTagName:
                return RunEndTagName(context, HtmlTokenizerState.ScriptData);
            case HtmlTokenizerState.ScriptDataEscapeStart:
                return RunEscapeStart(context);
            case HtmlTokenizerState.ScriptDataEscapeStartDash:
                return RunEscapeStartDash(context);
            case HtmlTokenizerState.ScriptDataEscaped:
                return RunEscaped(context);
            case HtmlTokenizerState.ScriptDataEscapedDash:
                return RunEscapedDash(context);
            case HtmlTokenizerState.ScriptDataEscapedDashDash:
                return RunEscapedDashDash(context);
            case HtmlTokenizerState.ScriptDataEscapedLessThanSign:
                return RunEscapedLessThanSign(context);
            case HtmlTokenizerState.ScriptDataEscapedEndTagOpen:
                return RunEndTagOpen(context,
                    HtmlTokenizerState.ScriptDataEscapedEndTagName,
                    HtmlTokenizerState.ScriptDataEscaped);
            case HtmlTokenizerState.ScriptDataEscapedEndTagName:
                return RunEndTagName(context, HtmlTokenizerState.ScriptDataEscaped);
            case HtmlTokenizerState.ScriptDataDoubleEscapeStart:
                return RunDoubleEscapeStart(context);
            case HtmlTokenizerState.ScriptDataDoubleEscaped:
                return RunDoubleEscaped(context);
            case HtmlTokenizerState.ScriptDataDoubleEscapedDash:
                return RunDoubleEscapedDash(context);
            case HtmlTokenizerState.ScriptDataDoubleEscapedDashDash:
                return RunDoubleEscapedDashDash(context);
            case HtmlTokenizerState.ScriptDataDoubleEscapedLessThanSign:
                return RunDoubleEscapedLessThanSign(context);
            case HtmlTokenizerState.ScriptDataDoubleEscapeEnd:
                return RunDoubleEscapeEnd(context);
            default:
                throw new InvalidOperationException(
                    "The current tokenizer state is not a script-data state.");
        }
    }

    internal static bool RunScriptData(HtmlTokenizerContext context)
    {
        int runLength = GetRunLength(context, false, true);
        if (runLength < 0)
        {
            return false;
        }

        if (runLength > 0)
        {
            return EmitRun(context, runLength);
        }

        int start = context.GetInputPosition();
        context.ConsumeInput();
        context.BeginMarkup(start);
        context.ClearScriptTemporaryBuffer();
        context.SetState(HtmlTokenizerState.ScriptDataLessThanSign);
        return true;
    }

    internal static bool RunScriptDataLessThanSign(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '/')
        {
            context.ConsumeInput();
            context.ClearScriptTemporaryBuffer();
            context.SetState(HtmlTokenizerState.ScriptDataEndTagOpen);
            return true;
        }

        if (current == '!')
        {
            context.ConsumeInput();
            return context.EmitMarkupCharactersAndSwitchState("<!",
                HtmlTokenizerState.ScriptDataEscapeStart);
        }

        return context.EmitMarkupCharactersAndSwitchState("<", HtmlTokenizerState.ScriptData);
    }

    internal static bool RunEndTagOpen(HtmlTokenizerContext context,
        HtmlTokenizerState nameState, HtmlTokenizerState fallbackState)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsAlpha(current))
        {
            context.BeginTag(HtmlTokenKind.EndTag);
            context.SetState(nameState);
            return true;
        }

        return EmitEndTagFallback(context, fallbackState);
    }

    internal static bool RunEndTagName(HtmlTokenizerContext context,
        HtmlTokenizerState fallbackState)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsAlpha(current))
        {
            context.ConsumeInput();
            if (!context.GetCurrentTag().AppendName(current))
            {
                context.MarkCurrentTagLimitExceeded();
                return false;
            }

            if (!context.AppendScriptTemporaryCharacter(current))
            {
                context.MarkTokenCharacterLimitExceeded();
                return false;
            }

            return true;
        }

        bool appropriate = context.IsAppropriateEndTagName(
            context.GetScriptTemporaryBuffer().AsSpan());
        if (appropriate && Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.BeforeAttributeName);
            return true;
        }

        if (appropriate && current == '/')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.SelfClosingStartTag);
            return true;
        }

        if (appropriate && current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentTag();
        }

        return EmitEndTagFallback(context, fallbackState);
    }

    internal static bool RunEscapeStart(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            int start = context.GetInputPosition();
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.ScriptDataEscapeStartDash);
            return EmitCharacters(context, "-", start, start + 1);
        }

        context.SetState(HtmlTokenizerState.ScriptData);
        return true;
    }

    internal static bool RunEscapeStartDash(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            int start = context.GetInputPosition();
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.ScriptDataEscapedDashDash);
            return EmitCharacters(context, "-", start, start + 1);
        }

        context.SetState(HtmlTokenizerState.ScriptData);
        return true;
    }

    internal static bool RunEscaped(HtmlTokenizerContext context)
    {
        int runLength = GetRunLength(context, true, true);
        if (runLength < 0)
        {
            return false;
        }

        if (runLength > 0)
        {
            return EmitRun(context, runLength);
        }

        char current = (char)context.PeekInput();
        if (current == '-')
        {
            int start = context.GetInputPosition();
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.ScriptDataEscapedDash);
            return EmitCharacters(context, "-", start, start + 1);
        }

        int markupStart = context.GetInputPosition();
        context.ConsumeInput();
        context.BeginMarkup(markupStart);
        context.SetState(HtmlTokenizerState.ScriptDataEscapedLessThanSign);
        return true;
    }

    internal static bool RunEscapedDash(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            return ConsumeAndEmit(context,
                HtmlTokenizerState.ScriptDataEscapedDashDash);
        }

        if (current == '<')
        {
            int start = context.GetInputPosition();
            context.ConsumeInput();
            context.BeginMarkup(start);
            context.SetState(HtmlTokenizerState.ScriptDataEscapedLessThanSign);
            return true;
        }

        return ConsumeAndEmit(context, HtmlTokenizerState.ScriptDataEscaped);
    }

    internal static bool RunEscapedDashDash(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            return ConsumeAndEmit(context,
                HtmlTokenizerState.ScriptDataEscapedDashDash);
        }

        if (current == '<')
        {
            int start = context.GetInputPosition();
            context.ConsumeInput();
            context.BeginMarkup(start);
            context.SetState(HtmlTokenizerState.ScriptDataEscapedLessThanSign);
            return true;
        }

        if (current == '>')
        {
            return ConsumeAndEmit(context, HtmlTokenizerState.ScriptData);
        }

        return ConsumeAndEmit(context, HtmlTokenizerState.ScriptDataEscaped);
    }

    internal static bool RunEscapedLessThanSign(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '/')
        {
            context.ConsumeInput();
            context.ClearScriptTemporaryBuffer();
            context.SetState(HtmlTokenizerState.ScriptDataEscapedEndTagOpen);
            return true;
        }

        if (Ascii.IsAlpha(current))
        {
            context.ClearScriptTemporaryBuffer();
            return context.EmitMarkupCharactersAndSwitchState("<",
                HtmlTokenizerState.ScriptDataDoubleEscapeStart);
        }

        return context.EmitMarkupCharactersAndSwitchState("<",
            HtmlTokenizerState.ScriptDataEscaped);
    }

    internal static bool RunDoubleEscapeStart(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsAlpha(current))
        {
            if (!context.AppendScriptTemporaryCharacter(current))
            {
                context.MarkTokenCharacterLimitExceeded();
                return false;
            }

            return ConsumeAndEmit(context,
                HtmlTokenizerState.ScriptDataDoubleEscapeStart);
        }

        if (IsDoubleEscapeDelimiter(current))
        {
            HtmlTokenizerState next = context.IsScriptTemporaryBuffer("script")
                ? HtmlTokenizerState.ScriptDataDoubleEscaped
                : HtmlTokenizerState.ScriptDataEscaped;
            return ConsumeAndEmit(context, next);
        }

        context.SetState(HtmlTokenizerState.ScriptDataEscaped);
        return true;
    }

    internal static bool RunDoubleEscaped(HtmlTokenizerContext context)
    {
        int runLength = GetRunLength(context, true, true);
        if (runLength < 0)
        {
            return false;
        }

        if (runLength > 0)
        {
            return EmitRun(context, runLength);
        }

        char current = (char)context.PeekInput();
        if (current == '-')
        {
            return ConsumeAndEmit(context,
                HtmlTokenizerState.ScriptDataDoubleEscapedDash);
        }

        return ConsumeAndEmit(context,
            HtmlTokenizerState.ScriptDataDoubleEscapedLessThanSign);
    }

    internal static bool RunDoubleEscapedDash(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            return ConsumeAndEmit(context,
                HtmlTokenizerState.ScriptDataDoubleEscapedDashDash);
        }

        if (current == '<')
        {
            return ConsumeAndEmit(context,
                HtmlTokenizerState.ScriptDataDoubleEscapedLessThanSign);
        }

        return ConsumeAndEmit(context, HtmlTokenizerState.ScriptDataDoubleEscaped);
    }

    internal static bool RunDoubleEscapedDashDash(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '-')
        {
            return ConsumeAndEmit(context,
                HtmlTokenizerState.ScriptDataDoubleEscapedDashDash);
        }

        if (current == '<')
        {
            return ConsumeAndEmit(context,
                HtmlTokenizerState.ScriptDataDoubleEscapedLessThanSign);
        }

        if (current == '>')
        {
            return ConsumeAndEmit(context, HtmlTokenizerState.ScriptData);
        }

        return ConsumeAndEmit(context, HtmlTokenizerState.ScriptDataDoubleEscaped);
    }

    internal static bool RunDoubleEscapedLessThanSign(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '/')
        {
            context.ClearScriptTemporaryBuffer();
            return ConsumeAndEmit(context,
                HtmlTokenizerState.ScriptDataDoubleEscapeEnd);
        }

        context.SetState(HtmlTokenizerState.ScriptDataDoubleEscaped);
        return true;
    }

    internal static bool RunDoubleEscapeEnd(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsAlpha(current))
        {
            if (!context.AppendScriptTemporaryCharacter(current))
            {
                context.MarkTokenCharacterLimitExceeded();
                return false;
            }

            return ConsumeAndEmit(context,
                HtmlTokenizerState.ScriptDataDoubleEscapeEnd);
        }

        if (IsDoubleEscapeDelimiter(current))
        {
            HtmlTokenizerState next = context.IsScriptTemporaryBuffer("script")
                ? HtmlTokenizerState.ScriptDataEscaped
                : HtmlTokenizerState.ScriptDataDoubleEscaped;
            return ConsumeAndEmit(context, next);
        }

        context.SetState(HtmlTokenizerState.ScriptDataDoubleEscaped);
        return true;
    }

    internal static bool HandleEndOfInput(HtmlTokenizerContext context)
    {
        HtmlTokenizerState state = context.GetState();
        if (state == HtmlTokenizerState.ScriptData)
        {
            context.SetState(HtmlTokenizerState.Data);
            return true;
        }

        if (state == HtmlTokenizerState.ScriptDataLessThanSign)
        {
            return context.EmitMarkupCharactersAndSwitchState("<", HtmlTokenizerState.ScriptData);
        }

        if (state == HtmlTokenizerState.ScriptDataEndTagOpen)
        {
            return EmitEndTagFallback(context, HtmlTokenizerState.ScriptData);
        }

        if (state == HtmlTokenizerState.ScriptDataEndTagName)
        {
            return EmitEndTagFallback(context, HtmlTokenizerState.ScriptData);
        }

        if (state == HtmlTokenizerState.ScriptDataEscapeStart
            || state == HtmlTokenizerState.ScriptDataEscapeStartDash)
        {
            context.SetState(HtmlTokenizerState.ScriptData);
            return true;
        }

        if (state == HtmlTokenizerState.ScriptDataEscapedLessThanSign)
        {
            return context.EmitMarkupCharactersAndSwitchState("<",
                HtmlTokenizerState.ScriptDataEscaped);
        }

        if (state == HtmlTokenizerState.ScriptDataEscapedEndTagOpen
            || state == HtmlTokenizerState.ScriptDataEscapedEndTagName)
        {
            return EmitEndTagFallback(context, HtmlTokenizerState.ScriptDataEscaped);
        }

        if (state == HtmlTokenizerState.ScriptDataDoubleEscapeStart)
        {
            context.SetState(HtmlTokenizerState.ScriptDataEscaped);
            return true;
        }

        if (state == HtmlTokenizerState.ScriptDataDoubleEscapedLessThanSign
            || state == HtmlTokenizerState.ScriptDataDoubleEscapeEnd)
        {
            context.SetState(HtmlTokenizerState.ScriptDataDoubleEscaped);
            return true;
        }

        int position = context.GetInputPosition();
        context.AddTokenizerDiagnostic(
            "The input ended inside HTML-comment-like script text.",
            position, position);
        context.SetState(HtmlTokenizerState.Data);
        return true;
    }

    internal static bool EmitEndTagFallback(HtmlTokenizerContext context,
        HtmlTokenizerState fallbackState)
    {
        string data = "</" + context.GetScriptTemporaryBuffer();
        return context.EmitMarkupCharactersAndSwitchState(data, fallbackState);
    }

    internal static bool ConsumeAndEmit(HtmlTokenizerContext context,
        HtmlTokenizerState nextState)
    {
        int start = context.GetInputPosition();
        char current = (char)context.ConsumeInput();
        context.SetState(nextState);
        return EmitCharacters(context, new string(current, 1), start, start + 1);
    }

    internal static int GetRunLength(HtmlTokenizerContext context,
        bool stopAtHyphen, bool stopAtLessThan)
    {
        ReadOnlySpan<char> available = context.GetAvailableInputSpan();
        int maximum = context.GetMaximumTokenCharacters();
        int length = 0;
        while (length < available.Length)
        {
            char current = available[length];
            if (stopAtHyphen && current == '-'
                || stopAtLessThan && current == '<')
            {
                break;
            }

            int characterLength = UnicodeTools.IsHighSurrogate(current) ? 2 : 1;
            if (characterLength > maximum - length)
            {
                if (length == 0)
                {
                    context.MarkTokenCharacterLimitExceeded();
                    return -1;
                }

                break;
            }

            length = length + characterLength;
            if (!context.GetCoalesceCharacterTokens())
            {
                break;
            }
        }

        return length;
    }

    internal static bool EmitRun(HtmlTokenizerContext context, int length)
    {
        int start = context.GetInputPosition();
        string data = new string(context.GetAvailableInputSpan().Slice(0, length));
        Consume(context, length);
        return EmitCharacters(context, data, start, start + length);
    }

    internal static bool EmitCharacters(HtmlTokenizerContext context,
        string data, int start, int end)
    {
        HtmlToken token = HtmlToken.CreateCharacter(data,
            context.CreateSourceSpan(start, end));
        bool emitted = context.TryEmitToken(token);
        context.CompactInput();
        return emitted;
    }

    internal static bool IsDoubleEscapeDelimiter(char value)
    {
        return Ascii.IsHtmlWhitespace(value) || value == '/' || value == '>';
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
