using System;

namespace html;

internal class HtmlDoctypeTokenBuilder
{
    internal CharacterBuffer name;
    internal CharacterBuffer publicIdentifier;
    internal CharacterBuffer systemIdentifier;
    internal int maximumTokenCharacters;
    internal int maximumNameCharacters;
    internal int tokenCharacterCount;
    internal HtmlLimitKind exceededLimit;
    internal int exceededLimitValue;
    internal bool hasName;
    internal bool hasPublicIdentifier;
    internal bool hasSystemIdentifier;
    internal bool forceQuirks;

    internal HtmlDoctypeTokenBuilder(int maximumTokenCharacters,
        int maximumNameCharacters)
    {
        if (maximumTokenCharacters <= 0 || maximumNameCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumTokenCharacters",
                maximumTokenCharacters, "All DOCTYPE limits must be positive.");
        }

        int nameMaximum = maximumNameCharacters < maximumTokenCharacters
            ? maximumNameCharacters : maximumTokenCharacters;
        int nameCapacity = nameMaximum < 16 ? nameMaximum : 16;
        int identifierCapacity = maximumTokenCharacters < 16
            ? maximumTokenCharacters : 16;
        this.name = CharacterBuffer.Create(nameCapacity, nameMaximum);
        this.publicIdentifier = CharacterBuffer.Create(
            identifierCapacity, maximumTokenCharacters);
        this.systemIdentifier = CharacterBuffer.Create(
            identifierCapacity, maximumTokenCharacters);
        this.maximumTokenCharacters = maximumTokenCharacters;
        this.maximumNameCharacters = maximumNameCharacters;
        this.tokenCharacterCount = 0;
        this.exceededLimit = HtmlLimitKind.None;
        this.exceededLimitValue = 0;
        this.hasName = false;
        this.hasPublicIdentifier = false;
        this.hasSystemIdentifier = false;
        this.forceQuirks = false;
    }

    internal bool AppendName(char value)
    {
        if (this.name.GetLength() >= this.maximumNameCharacters)
        {
            return this.ExceedLimit(HtmlLimitKind.NameCharacters,
                this.name.GetLength() + 1);
        }

        if (!this.CanAppendTokenCharacter())
        {
            return false;
        }

        this.name.TryAppend(Ascii.ToLower(value));
        this.tokenCharacterCount = this.tokenCharacterCount + 1;
        this.hasName = true;
        return true;
    }

    internal void SetPublicIdentifier()
    {
        this.hasPublicIdentifier = true;
    }

    internal bool AppendPublicIdentifier(char value)
    {
        if (!this.CanAppendTokenCharacter())
        {
            return false;
        }

        this.publicIdentifier.TryAppend(value);
        this.tokenCharacterCount = this.tokenCharacterCount + 1;
        return true;
    }

    internal void SetSystemIdentifier()
    {
        this.hasSystemIdentifier = true;
    }

    internal bool AppendSystemIdentifier(char value)
    {
        if (!this.CanAppendTokenCharacter())
        {
            return false;
        }

        this.systemIdentifier.TryAppend(value);
        this.tokenCharacterCount = this.tokenCharacterCount + 1;
        return true;
    }

    internal void SetForceQuirks()
    {
        this.forceQuirks = true;
    }

    internal HtmlLimitKind GetExceededLimit()
    {
        return this.exceededLimit;
    }

    internal int GetExceededLimitValue()
    {
        return this.exceededLimitValue;
    }

    internal HtmlToken CreateToken(HtmlSourceSpan sourceSpan)
    {
        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        return HtmlToken.CreateDoctype(this.hasName, this.name.GetString(),
            this.hasPublicIdentifier, this.publicIdentifier.GetString(),
            this.hasSystemIdentifier, this.systemIdentifier.GetString(),
            this.forceQuirks, sourceSpan);
    }

    internal bool CanAppendTokenCharacter()
    {
        if (this.tokenCharacterCount >= this.maximumTokenCharacters)
        {
            return this.ExceedLimit(HtmlLimitKind.TokenCharacters,
                this.tokenCharacterCount + 1);
        }

        return true;
    }

    internal bool ExceedLimit(HtmlLimitKind kind, int value)
    {
        this.exceededLimit = kind;
        this.exceededLimitValue = value;
        return false;
    }
}

internal static class HtmlTokenizerDoctypeStates
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
            case HtmlTokenizerState.Doctype:
                return RunDoctype(context);
            case HtmlTokenizerState.BeforeDoctypeName:
                return RunBeforeDoctypeName(context);
            case HtmlTokenizerState.DoctypeName:
                return RunDoctypeName(context);
            case HtmlTokenizerState.AfterDoctypeName:
                return RunAfterDoctypeName(context);
            case HtmlTokenizerState.AfterDoctypePublicKeyword:
                return RunAfterDoctypePublicKeyword(context);
            case HtmlTokenizerState.BeforeDoctypePublicIdentifier:
                return RunBeforeDoctypePublicIdentifier(context);
            case HtmlTokenizerState.DoctypePublicIdentifierDoubleQuoted:
                return RunDoctypePublicIdentifier(context, '"');
            case HtmlTokenizerState.DoctypePublicIdentifierSingleQuoted:
                return RunDoctypePublicIdentifier(context, '\'');
            case HtmlTokenizerState.AfterDoctypePublicIdentifier:
                return RunAfterDoctypePublicIdentifier(context);
            case HtmlTokenizerState.BetweenDoctypePublicAndSystemIdentifiers:
                return RunBetweenDoctypePublicAndSystemIdentifiers(context);
            case HtmlTokenizerState.AfterDoctypeSystemKeyword:
                return RunAfterDoctypeSystemKeyword(context);
            case HtmlTokenizerState.BeforeDoctypeSystemIdentifier:
                return RunBeforeDoctypeSystemIdentifier(context);
            case HtmlTokenizerState.DoctypeSystemIdentifierDoubleQuoted:
                return RunDoctypeSystemIdentifier(context, '"');
            case HtmlTokenizerState.DoctypeSystemIdentifierSingleQuoted:
                return RunDoctypeSystemIdentifier(context, '\'');
            case HtmlTokenizerState.AfterDoctypeSystemIdentifier:
                return RunAfterDoctypeSystemIdentifier(context);
            case HtmlTokenizerState.BogusDoctype:
                return RunBogusDoctype(context);
            default:
                throw new InvalidOperationException(
                    "The current tokenizer state is not a DOCTYPE state.");
        }
    }

    internal static bool RunDoctype(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.BeforeDoctypeName);
            return true;
        }

        if (current != '>')
        {
            AddMalformedDoctypeDiagnostic(context,
                "Whitespace was missing before the DOCTYPE name.");
        }

        context.SetState(HtmlTokenizerState.BeforeDoctypeName);
        return true;
    }

    internal static bool RunBeforeDoctypeName(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            return true;
        }

        if (current == '>')
        {
            AddMalformedDoctypeDiagnostic(context, "The DOCTYPE name was missing.");
            context.ConsumeInput();
            context.GetCurrentDoctype().SetForceQuirks();
            return context.EmitCurrentDoctype();
        }

        context.ConsumeInput();
        context.SetState(HtmlTokenizerState.DoctypeName);
        return AppendName(context, current);
    }

    internal static bool RunDoctypeName(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.AfterDoctypeName);
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentDoctype();
        }

        context.ConsumeInput();
        return AppendName(context, current);
    }

    internal static bool RunAfterDoctypeName(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentDoctype();
        }

        ReadOnlySpan<char> available = context.GetAvailableInputSpan();
        if (StartsWithIgnoreCase(available, "PUBLIC"))
        {
            Consume(context, 6);
            context.SetState(HtmlTokenizerState.AfterDoctypePublicKeyword);
            return true;
        }

        if (StartsWithIgnoreCase(available, "SYSTEM"))
        {
            Consume(context, 6);
            context.SetState(HtmlTokenizerState.AfterDoctypeSystemKeyword);
            return true;
        }

        if (!context.IsInputComplete()
            && (IsPossiblePrefixIgnoreCase(available, "PUBLIC")
                || IsPossiblePrefixIgnoreCase(available, "SYSTEM")))
        {
            return false;
        }

        AddMalformedDoctypeDiagnostic(context,
            "An invalid character sequence followed the DOCTYPE name.");
        context.GetCurrentDoctype().SetForceQuirks();
        context.SetState(HtmlTokenizerState.BogusDoctype);
        return true;
    }

    internal static bool RunAfterDoctypePublicKeyword(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.BeforeDoctypePublicIdentifier);
            return true;
        }

        if (current == '"' || current == '\'')
        {
            AddMalformedDoctypeDiagnostic(context,
                "Whitespace was missing after the DOCTYPE PUBLIC keyword.");
            return StartPublicIdentifier(context, current);
        }

        if (current == '>')
        {
            AddMalformedDoctypeDiagnostic(context,
                "The DOCTYPE public identifier was missing.");
            context.ConsumeInput();
            context.GetCurrentDoctype().SetForceQuirks();
            return context.EmitCurrentDoctype();
        }

        AddMalformedDoctypeDiagnostic(context,
            "A quote was missing before the DOCTYPE public identifier.");
        context.GetCurrentDoctype().SetForceQuirks();
        context.SetState(HtmlTokenizerState.BogusDoctype);
        return true;
    }

    internal static bool RunBeforeDoctypePublicIdentifier(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            return true;
        }

        if (current == '"' || current == '\'')
        {
            return StartPublicIdentifier(context, current);
        }

        if (current == '>')
        {
            AddMalformedDoctypeDiagnostic(context,
                "The DOCTYPE public identifier was missing.");
            context.ConsumeInput();
            context.GetCurrentDoctype().SetForceQuirks();
            return context.EmitCurrentDoctype();
        }

        AddMalformedDoctypeDiagnostic(context,
            "A quote was missing before the DOCTYPE public identifier.");
        context.GetCurrentDoctype().SetForceQuirks();
        context.SetState(HtmlTokenizerState.BogusDoctype);
        return true;
    }

    internal static bool RunDoctypePublicIdentifier(HtmlTokenizerContext context,
        char quote)
    {
        char current = (char)context.PeekInput();
        if (current == quote)
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.AfterDoctypePublicIdentifier);
            return true;
        }

        if (current == '>')
        {
            AddMalformedDoctypeDiagnostic(context,
                "The DOCTYPE public identifier was abruptly closed.");
            context.ConsumeInput();
            context.GetCurrentDoctype().SetForceQuirks();
            return context.EmitCurrentDoctype();
        }

        context.ConsumeInput();
        return AppendPublicIdentifier(context, current);
    }

    internal static bool RunAfterDoctypePublicIdentifier(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.BetweenDoctypePublicAndSystemIdentifiers);
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentDoctype();
        }

        if (current == '"' || current == '\'')
        {
            AddMalformedDoctypeDiagnostic(context,
                "Whitespace was missing between the DOCTYPE public and system identifiers.");
            return StartSystemIdentifier(context, current);
        }

        AddMalformedDoctypeDiagnostic(context,
            "A quote was missing before the DOCTYPE system identifier.");
        context.GetCurrentDoctype().SetForceQuirks();
        context.SetState(HtmlTokenizerState.BogusDoctype);
        return true;
    }

    internal static bool RunBetweenDoctypePublicAndSystemIdentifiers(
        HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentDoctype();
        }

        if (current == '"' || current == '\'')
        {
            return StartSystemIdentifier(context, current);
        }

        AddMalformedDoctypeDiagnostic(context,
            "A quote was missing before the DOCTYPE system identifier.");
        context.GetCurrentDoctype().SetForceQuirks();
        context.SetState(HtmlTokenizerState.BogusDoctype);
        return true;
    }

    internal static bool RunAfterDoctypeSystemKeyword(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.BeforeDoctypeSystemIdentifier);
            return true;
        }

        if (current == '"' || current == '\'')
        {
            AddMalformedDoctypeDiagnostic(context,
                "Whitespace was missing after the DOCTYPE SYSTEM keyword.");
            return StartSystemIdentifier(context, current);
        }

        if (current == '>')
        {
            AddMalformedDoctypeDiagnostic(context,
                "The DOCTYPE system identifier was missing.");
            context.ConsumeInput();
            context.GetCurrentDoctype().SetForceQuirks();
            return context.EmitCurrentDoctype();
        }

        AddMalformedDoctypeDiagnostic(context,
            "A quote was missing before the DOCTYPE system identifier.");
        context.GetCurrentDoctype().SetForceQuirks();
        context.SetState(HtmlTokenizerState.BogusDoctype);
        return true;
    }

    internal static bool RunBeforeDoctypeSystemIdentifier(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            return true;
        }

        if (current == '"' || current == '\'')
        {
            return StartSystemIdentifier(context, current);
        }

        if (current == '>')
        {
            AddMalformedDoctypeDiagnostic(context,
                "The DOCTYPE system identifier was missing.");
            context.ConsumeInput();
            context.GetCurrentDoctype().SetForceQuirks();
            return context.EmitCurrentDoctype();
        }

        AddMalformedDoctypeDiagnostic(context,
            "A quote was missing before the DOCTYPE system identifier.");
        context.GetCurrentDoctype().SetForceQuirks();
        context.SetState(HtmlTokenizerState.BogusDoctype);
        return true;
    }

    internal static bool RunDoctypeSystemIdentifier(HtmlTokenizerContext context,
        char quote)
    {
        char current = (char)context.PeekInput();
        if (current == quote)
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.AfterDoctypeSystemIdentifier);
            return true;
        }

        if (current == '>')
        {
            AddMalformedDoctypeDiagnostic(context,
                "The DOCTYPE system identifier was abruptly closed.");
            context.ConsumeInput();
            context.GetCurrentDoctype().SetForceQuirks();
            return context.EmitCurrentDoctype();
        }

        context.ConsumeInput();
        return AppendSystemIdentifier(context, current);
    }

    internal static bool RunAfterDoctypeSystemIdentifier(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentDoctype();
        }

        AddMalformedDoctypeDiagnostic(context,
            "An unexpected character followed the DOCTYPE system identifier.");
        context.SetState(HtmlTokenizerState.BogusDoctype);
        return true;
    }

    internal static bool RunBogusDoctype(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        context.ConsumeInput();
        if (current == '>')
        {
            return context.EmitCurrentDoctype();
        }

        return true;
    }

    internal static bool HandleEndOfInput(HtmlTokenizerContext context)
    {
        if (context.GetState() != HtmlTokenizerState.BogusDoctype)
        {
            AddMalformedDoctypeDiagnostic(context,
                "The input ended before the DOCTYPE was closed.");
            context.GetCurrentDoctype().SetForceQuirks();
        }

        return context.EmitCurrentDoctype();
    }

    internal static bool StartPublicIdentifier(HtmlTokenizerContext context, char quote)
    {
        context.ConsumeInput();
        context.GetCurrentDoctype().SetPublicIdentifier();
        if (quote == '"')
        {
            context.SetState(HtmlTokenizerState.DoctypePublicIdentifierDoubleQuoted);
        }
        else
        {
            context.SetState(HtmlTokenizerState.DoctypePublicIdentifierSingleQuoted);
        }

        return true;
    }

    internal static bool StartSystemIdentifier(HtmlTokenizerContext context, char quote)
    {
        context.ConsumeInput();
        context.GetCurrentDoctype().SetSystemIdentifier();
        if (quote == '"')
        {
            context.SetState(HtmlTokenizerState.DoctypeSystemIdentifierDoubleQuoted);
        }
        else
        {
            context.SetState(HtmlTokenizerState.DoctypeSystemIdentifierSingleQuoted);
        }

        return true;
    }

    internal static bool AppendName(HtmlTokenizerContext context, char value)
    {
        if (!context.GetCurrentDoctype().AppendName(value))
        {
            context.MarkCurrentDoctypeLimitExceeded();
            return false;
        }

        return true;
    }

    internal static bool AppendPublicIdentifier(HtmlTokenizerContext context, char value)
    {
        if (!context.GetCurrentDoctype().AppendPublicIdentifier(value))
        {
            context.MarkCurrentDoctypeLimitExceeded();
            return false;
        }

        return true;
    }

    internal static bool AppendSystemIdentifier(HtmlTokenizerContext context, char value)
    {
        if (!context.GetCurrentDoctype().AppendSystemIdentifier(value))
        {
            context.MarkCurrentDoctypeLimitExceeded();
            return false;
        }

        return true;
    }

    internal static void AddMalformedDoctypeDiagnostic(HtmlTokenizerContext context,
        string message)
    {
        int end = context.GetInputPosition();
        if (context.GetAvailableInputSpan().Length > 0)
        {
            end = end + 1;
        }

        context.AddTokenizerDiagnosticWithCode(HtmlDiagnosticCode.TokenizerMalformedDoctype,
            message, context.GetMarkupStart(), end);
    }

    internal static bool StartsWithIgnoreCase(ReadOnlySpan<char> available, string expected)
    {
        return available.Length >= expected.Length
            && Ascii.EqualsIgnoreCase(available.Slice(0, expected.Length),
                expected.AsSpan());
    }

    internal static bool IsPossiblePrefixIgnoreCase(ReadOnlySpan<char> available,
        string expected)
    {
        return available.Length < expected.Length
            && Ascii.EqualsIgnoreCase(available,
                expected.AsSpan(0, available.Length));
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
