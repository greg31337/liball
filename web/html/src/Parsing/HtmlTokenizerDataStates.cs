using System;

namespace html;

internal static class HtmlTokenizerDataStates
{
    internal static bool Run(HtmlTokenizerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        HtmlTokenizerState state = context.GetState();
        switch (state)
        {
            case HtmlTokenizerState.Data:
                return RunText(context, true, false, true);
            case HtmlTokenizerState.Rcdata:
                return RunText(context, true, true, false);
            case HtmlTokenizerState.RawText:
                return RunText(context, false, true, false);
            case HtmlTokenizerState.PlainText:
                return RunText(context, false, false, false);
            default:
                throw new InvalidOperationException("The current tokenizer state is not a text state.");
        }
    }

    internal static bool RunText(HtmlTokenizerContext context, bool decodeCharacterReferences,
        bool recognizeAppropriateEndTag, bool recognizeTags)
    {
        int start = context.GetInputPosition();
        int maximum = context.GetMaximumTokenCharacters();
        int initialCapacity = maximum < 16 ? maximum : 16;
        CharacterBuffer data = CharacterBuffer.Create(initialCapacity, maximum);
        bool coalesce = context.GetCoalesceCharacterTokens();
        bool suspended = false;

        while (context.GetAvailableInputSpan().Length > 0)
        {
            ReadOnlySpan<char> available = context.GetAvailableInputSpan();
            char current = available[0];
            if (recognizeTags && current == '<')
            {
                if (data.GetLength() > 0)
                {
                    break;
                }

                int markupStart = context.GetInputPosition();
                context.ConsumeInput();
                context.BeginMarkup(markupStart);
                context.SetState(HtmlTokenizerState.TagOpen);
                return true;
            }

            if (recognizeAppropriateEndTag && current == '<')
            {
                int endTagLength = GetAppropriateEndTagLength(context);
                if (endTagLength < 0)
                {
                    suspended = true;
                    break;
                }

                if (endTagLength > 0)
                {
                    if (data.GetLength() > 0)
                    {
                        break;
                    }

                    return EmitAppropriateEndTag(context, endTagLength);
                }
            }

            if (decodeCharacterReferences && current == '&')
            {
                HtmlCharacterReferenceResult reference = HtmlCharacterReferenceDecoder.Decode(
                    available.Slice(1), context.IsInputComplete(),
                    HtmlCharacterReferenceContext.Data);
                if (reference.GetStatus() == HtmlCharacterReferenceStatus.NeedMoreInput)
                {
                    suspended = true;
                    break;
                }

                string replacement = reference.IsMatch()
                    ? reference.GetReplacementString() : "&";
                if (reference.IsMatch()
                    && (!coalesce || replacement.Length > maximum))
                {
                    if (data.GetLength() > 0)
                    {
                        break;
                    }

                    return EmitCharacterReferenceReplacement(context,
                        reference, replacement, maximum);
                }

                if (!CanAppend(data, replacement.Length, maximum))
                {
                    if (data.GetLength() == 0)
                    {
                        context.MarkTokenCharacterLimitExceeded();
                        return true;
                    }

                    break;
                }

                int ampersandOffset = context.GetInputPosition();
                context.AddCharacterReferenceDiagnostics(reference, ampersandOffset);
                int consumed = reference.IsMatch()
                    ? 1 + reference.GetConsumedCharacters() : 1;
                Consume(context, consumed);
                data.TryAppendString(replacement);
                if (!coalesce)
                {
                    break;
                }

                continue;
            }

            int characterLength = UnicodeTools.IsHighSurrogate(current) ? 2 : 1;
            if (!CanAppend(data, characterLength, maximum))
            {
                if (data.GetLength() == 0)
                {
                    context.MarkTokenCharacterLimitExceeded();
                    return true;
                }

                break;
            }

            int index = 0;
            while (index < characterLength)
            {
                data.TryAppend((char)context.ConsumeInput());
                index = index + 1;
            }

            if (!coalesce)
            {
                break;
            }
        }

        if (data.GetLength() > 0)
        {
            EmitCharacterData(context, data.GetString(), start);
            return true;
        }

        return !suspended && context.GetAvailableInputSpan().Length == 0;
    }

    internal static int GetAppropriateEndTagLength(HtmlTokenizerContext context)
    {
        ReadOnlySpan<char> available = context.GetAvailableInputSpan();
        if (available.Length == 0 || available[0] != '<')
        {
            return 0;
        }

        if (available.Length == 1)
        {
            return context.IsInputComplete() ? 0 : -1;
        }

        if (available[1] != '/')
        {
            return 0;
        }

        if (available.Length == 2)
        {
            return context.IsInputComplete() ? 0 : -1;
        }

        if (!Ascii.IsAlpha(available[2]))
        {
            return 0;
        }

        int nameEnd = 2;
        while (nameEnd < available.Length && Ascii.IsAlpha(available[nameEnd]))
        {
            nameEnd = nameEnd + 1;
        }

        if (nameEnd == available.Length)
        {
            return context.IsInputComplete() ? 0 : -1;
        }

        if (!context.IsAppropriateEndTagName(available.Slice(2, nameEnd - 2)))
        {
            return 0;
        }

        if (available[nameEnd] == '>')
        {
            return nameEnd + 1;
        }

        if (available[nameEnd] == '/')
        {
            return GetSolidusEndTagLength(available, nameEnd, context.IsInputComplete());
        }

        if (!Ascii.IsHtmlWhitespace(available[nameEnd]))
        {
            return 0;
        }

        int position = nameEnd + 1;
        while (position < available.Length && Ascii.IsHtmlWhitespace(available[position]))
        {
            position = position + 1;
        }

        if (position == available.Length)
        {
            return context.IsInputComplete() ? 0 : -1;
        }

        if (available[position] == '>')
        {
            return position + 1;
        }

        if (available[position] == '/')
        {
            return GetSolidusEndTagLength(available, position, context.IsInputComplete());
        }

        return 0;
    }

    internal static bool EmitAppropriateEndTag(HtmlTokenizerContext context, int length)
    {
        int start = context.GetInputPosition();
        ReadOnlySpan<char> available = context.GetAvailableInputSpan();
        int nameLength = length - 3;
        char[] foldedName = new char[nameLength];
        int index = 0;
        while (index < nameLength)
        {
            foldedName[index] = Ascii.ToLower(available[index + 2]);
            index = index + 1;
        }

        Consume(context, length);
        int end = context.GetInputPosition();
        context.SetState(HtmlTokenizerState.Data);
        bool selfClosing = available[length - 2] == '/';
        HtmlToken token = HtmlToken.CreateEndTag(new string(foldedName),
            new HtmlAttribute[0], selfClosing, context.CreateSourceSpan(start, end));
        context.TryEmitToken(token);
        context.CompactInput();
        return true;
    }

    internal static void EmitCharacterData(HtmlTokenizerContext context, string data, int start)
    {
        int end = context.GetInputPosition();
        HtmlToken token = HtmlToken.CreateCharacter(data,
            context.CreateSourceSpan(start, end));
        context.TryEmitToken(token);
        context.CompactInput();
    }

    internal static bool EmitCharacterReferenceReplacement(HtmlTokenizerContext context,
        HtmlCharacterReferenceResult reference, string replacement, int maximum)
    {
        int replacementOffset = 0;
        while (replacementOffset < replacement.Length)
        {
            int scalarLength = UnicodeTools.IsHighSurrogate(
                replacement[replacementOffset]) ? 2 : 1;
            if (scalarLength > maximum)
            {
                context.MarkTokenCharacterLimitExceeded();
                return false;
            }

            replacementOffset = replacementOffset + scalarLength;
        }

        int ampersandOffset = context.GetInputPosition();
        context.AddCharacterReferenceDiagnostics(reference, ampersandOffset);
        int consumed = 1 + reference.GetConsumedCharacters();
        Consume(context, consumed);
        int sourceEnd = context.GetInputPosition();
        replacementOffset = 0;
        bool emitted = false;
        while (replacementOffset < replacement.Length)
        {
            int length = 0;
            while (replacementOffset + length < replacement.Length)
            {
                int scalarLength = UnicodeTools.IsHighSurrogate(
                    replacement[replacementOffset + length]) ? 2 : 1;
                if ((!context.GetCoalesceCharacterTokens() && length > 0)
                    || scalarLength > maximum - length)
                {
                    break;
                }

                length = length + scalarLength;
            }

            string part = replacement.Substring(replacementOffset, length);
            HtmlToken token = HtmlToken.CreateCharacter(part,
                context.CreateSourceSpan(ampersandOffset, sourceEnd));
            if (context.TryEmitToken(token))
            {
                emitted = true;
            }

            replacementOffset = replacementOffset + length;
        }

        context.CompactInput();
        return emitted;
    }

    internal static bool CanAppend(CharacterBuffer buffer, int amount, int maximum)
    {
        return amount <= maximum - buffer.GetLength();
    }

    internal static int GetSolidusEndTagLength(ReadOnlySpan<char> available, int solidus,
        bool inputComplete)
    {
        if (solidus + 1 == available.Length)
        {
            return inputComplete ? 0 : -1;
        }

        return available[solidus + 1] == '>' ? solidus + 2 : 0;
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
