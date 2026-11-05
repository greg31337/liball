using System;

namespace html;

internal class HtmlTagAttributeBuilder
{
    internal CharacterBuffer name;
    internal CharacterBuffer value;
    internal int sourceStart;
    internal int sourceEnd;
    internal bool nameCompleted;
    internal bool included;

    internal HtmlTagAttributeBuilder(int maximumNameCharacters,
        int maximumValueCharacters, int sourceStart)
    {
        int nameCapacity = maximumNameCharacters < 16 ? maximumNameCharacters : 16;
        int valueCapacity = maximumValueCharacters < 16 ? maximumValueCharacters : 16;
        this.name = CharacterBuffer.Create(nameCapacity, maximumNameCharacters);
        this.value = CharacterBuffer.Create(valueCapacity, maximumValueCharacters);
        this.sourceStart = sourceStart;
        this.sourceEnd = sourceStart;
        this.nameCompleted = false;
        this.included = true;
    }

    internal bool AppendName(char value)
    {
        return this.name.TryAppend(Ascii.ToLower(value));
    }

    internal bool AppendValue(char value)
    {
        return this.value.TryAppend(value);
    }

    internal bool AppendValueString(string value)
    {
        return this.value.TryAppendString(value);
    }

    internal string GetName()
    {
        return this.name.GetString();
    }

    internal string GetValue()
    {
        return this.value.GetString();
    }

    internal int GetNameLength()
    {
        return this.name.GetLength();
    }

    internal int GetValueLength()
    {
        return this.value.GetLength();
    }

    internal int GetSourceStart()
    {
        return this.sourceStart;
    }

    internal int GetSourceEnd()
    {
        return this.sourceEnd;
    }

    internal void SetSourceEnd(int value)
    {
        if (value < this.sourceStart)
        {
            throw new ArgumentOutOfRangeException("value", value,
                "An attribute source end cannot precede its start.");
        }

        this.sourceEnd = value;
    }

    internal bool IsNameCompleted()
    {
        return this.nameCompleted;
    }

    internal void CompleteName()
    {
        this.nameCompleted = true;
    }

    internal bool IsIncluded()
    {
        return this.included;
    }

    internal void Exclude()
    {
        this.included = false;
    }
}

internal class HtmlTagTokenBuilder
{
    internal HtmlTokenKind kind;
    internal CharacterBuffer name;
    internal HtmlTagAttributeBuilder[] attributes;
    internal int attributeCount;
    internal HtmlTagAttributeBuilder? currentAttribute;
    internal int maximumTokenCharacters;
    internal int maximumAttributes;
    internal int maximumNameCharacters;
    internal int maximumValueCharacters;
    internal int tokenCharacterCount;
    internal HtmlLimitKind exceededLimit;
    internal int exceededLimitValue;
    internal bool selfClosing;

    internal HtmlTagTokenBuilder(HtmlTokenKind kind, int maximumTokenCharacters,
        int maximumAttributes, int maximumNameCharacters, int maximumValueCharacters)
    {
        if (kind != HtmlTokenKind.StartTag && kind != HtmlTokenKind.EndTag)
        {
            throw new ArgumentOutOfRangeException("kind", (int)kind,
                "A tag builder requires a start-tag or end-tag kind.");
        }

        if (maximumTokenCharacters <= 0 || maximumAttributes <= 0
            || maximumNameCharacters <= 0 || maximumValueCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumTokenCharacters",
                maximumTokenCharacters, "All tag limits must be positive.");
        }

        int nameMaximum = maximumNameCharacters < maximumTokenCharacters
            ? maximumNameCharacters : maximumTokenCharacters;
        int initialCapacity = nameMaximum < 16 ? nameMaximum : 16;
        this.kind = kind;
        this.name = CharacterBuffer.Create(initialCapacity, nameMaximum);
        this.attributes = new HtmlTagAttributeBuilder[4];
        this.attributeCount = 0;
        this.currentAttribute = null;
        this.maximumTokenCharacters = maximumTokenCharacters;
        this.maximumAttributes = maximumAttributes;
        this.maximumNameCharacters = maximumNameCharacters;
        this.maximumValueCharacters = maximumValueCharacters;
        this.tokenCharacterCount = 0;
        this.exceededLimit = HtmlLimitKind.None;
        this.exceededLimitValue = 0;
        this.selfClosing = false;
    }

    internal HtmlTokenKind GetKind()
    {
        return this.kind;
    }

    internal int GetAttributeCount()
    {
        return this.attributeCount;
    }

    internal bool IsSelfClosing()
    {
        return this.selfClosing;
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
        return true;
    }

    internal bool StartAttribute(int sourceStart)
    {
        if (this.attributeCount >= this.maximumAttributes)
        {
            return this.ExceedLimit(HtmlLimitKind.AttributesPerTag,
                this.attributeCount + 1);
        }

        this.EnsureAttributeCapacity(this.attributeCount + 1);
        int nameMaximum = this.maximumNameCharacters < this.maximumTokenCharacters
            ? this.maximumNameCharacters : this.maximumTokenCharacters;
        int valueMaximum = this.maximumValueCharacters < this.maximumTokenCharacters
            ? this.maximumValueCharacters : this.maximumTokenCharacters;
        HtmlTagAttributeBuilder attribute = new HtmlTagAttributeBuilder(
            nameMaximum, valueMaximum, sourceStart);
        this.attributes[this.attributeCount] = attribute;
        this.attributeCount = this.attributeCount + 1;
        this.currentAttribute = attribute;
        return true;
    }

    internal bool AppendAttributeName(char value)
    {
        HtmlTagAttributeBuilder attribute = this.GetCurrentAttribute();
        if (attribute.GetNameLength() >= this.maximumNameCharacters)
        {
            return this.ExceedLimit(HtmlLimitKind.NameCharacters,
                attribute.GetNameLength() + 1);
        }

        if (!this.CanAppendTokenCharacter())
        {
            return false;
        }

        attribute.AppendName(value);
        this.tokenCharacterCount = this.tokenCharacterCount + 1;
        return true;
    }

    internal bool AppendAttributeValue(char value)
    {
        HtmlTagAttributeBuilder attribute = this.GetCurrentAttribute();
        if (attribute.GetValueLength() >= this.maximumValueCharacters)
        {
            return this.ExceedLimit(HtmlLimitKind.AttributeValueCharacters,
                attribute.GetValueLength() + 1);
        }

        if (!this.CanAppendTokenCharacter())
        {
            return false;
        }

        attribute.AppendValue(value);
        this.tokenCharacterCount = this.tokenCharacterCount + 1;
        return true;
    }

    internal bool AppendAttributeValueString(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        HtmlTagAttributeBuilder attribute = this.GetCurrentAttribute();
        int amount = value.Length;
        if (amount > this.maximumValueCharacters - attribute.GetValueLength())
        {
            return this.ExceedLimit(HtmlLimitKind.AttributeValueCharacters,
                attribute.GetValueLength() + amount);
        }

        if (amount > this.maximumTokenCharacters - this.tokenCharacterCount)
        {
            return this.ExceedLimit(HtmlLimitKind.TokenCharacters,
                this.tokenCharacterCount + amount);
        }

        attribute.AppendValueString(value);
        this.tokenCharacterCount = this.tokenCharacterCount + amount;
        return true;
    }

    internal bool CompleteCurrentAttributeName()
    {
        HtmlTagAttributeBuilder current = this.GetCurrentAttribute();
        if (current.IsNameCompleted())
        {
            return false;
        }

        current.CompleteName();
        string nameValue = current.GetName();
        int index = 0;
        while (index < this.attributeCount - 1)
        {
            HtmlTagAttributeBuilder other = this.attributes[index];
            if (other.IsIncluded() && other.GetName() == nameValue)
            {
                current.Exclude();
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int GetCurrentAttributeSourceStart()
    {
        return this.GetCurrentAttribute().GetSourceStart();
    }

    internal void SetCurrentAttributeSourceEnd(int value)
    {
        this.GetCurrentAttribute().SetSourceEnd(value);
    }

    internal HtmlLimitKind GetExceededLimit()
    {
        return this.exceededLimit;
    }

    internal int GetExceededLimitValue()
    {
        return this.exceededLimitValue;
    }

    internal void SetSelfClosing()
    {
        this.selfClosing = true;
    }

    internal HtmlToken CreateToken(HtmlTokenizerContext context, HtmlSourceSpan sourceSpan)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        int includedCount = 0;
        int index = 0;
        while (index < this.attributeCount)
        {
            if (this.attributes[index].IsIncluded())
            {
                includedCount = includedCount + 1;
            }

            index = index + 1;
        }

        HtmlAttribute[] completedAttributes = new HtmlAttribute[includedCount];
        index = 0;
        int outputIndex = 0;
        while (index < this.attributeCount)
        {
            HtmlTagAttributeBuilder attribute = this.attributes[index];
            if (attribute.IsIncluded())
            {
                HtmlSourceSpan attributeSpan = context.CreateSourceSpan(
                    attribute.GetSourceStart(), attribute.GetSourceEnd());
                completedAttributes[outputIndex] = HtmlAttribute.Create(
                    attribute.GetName(), attribute.GetValue(), attributeSpan);
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        string completedName = this.name.GetString();
        if (this.kind == HtmlTokenKind.StartTag)
        {
            return HtmlToken.CreateStartTag(completedName, completedAttributes,
                this.selfClosing, sourceSpan);
        }

        return HtmlToken.CreateEndTag(completedName, completedAttributes,
            this.selfClosing, sourceSpan);
    }

    internal HtmlTagAttributeBuilder GetCurrentAttribute()
    {
        if (this.currentAttribute == null)
        {
            throw new InvalidOperationException("No tag attribute is being built.");
        }

        return this.currentAttribute;
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

    internal void EnsureAttributeCapacity(int required)
    {
        if (required <= this.attributes.Length)
        {
            return;
        }

        if (this.attributes.Length > int.MaxValue / 2)
        {
            throw new InsufficientMemoryException("The tag cannot represent another attribute.");
        }

        HtmlTagAttributeBuilder[] expanded =
            new HtmlTagAttributeBuilder[this.attributes.Length * 2];
        Array.Copy(this.attributes, expanded, this.attributeCount);
        this.attributes = expanded;
    }
}

internal static class HtmlTokenizerTagStates
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
            case HtmlTokenizerState.TagOpen:
                return RunTagOpen(context);
            case HtmlTokenizerState.EndTagOpen:
                return RunEndTagOpen(context);
            case HtmlTokenizerState.TagName:
                return RunTagName(context);
            case HtmlTokenizerState.BeforeAttributeName:
                return RunBeforeAttributeName(context);
            case HtmlTokenizerState.AttributeName:
                return RunAttributeName(context);
            case HtmlTokenizerState.AfterAttributeName:
                return RunAfterAttributeName(context);
            case HtmlTokenizerState.BeforeAttributeValue:
                return RunBeforeAttributeValue(context);
            case HtmlTokenizerState.AttributeValueDoubleQuoted:
                return RunQuotedAttributeValue(context, '"');
            case HtmlTokenizerState.AttributeValueSingleQuoted:
                return RunQuotedAttributeValue(context, '\'');
            case HtmlTokenizerState.AttributeValueUnquoted:
                return RunUnquotedAttributeValue(context);
            case HtmlTokenizerState.AfterAttributeValueQuoted:
                return RunAfterAttributeValueQuoted(context);
            case HtmlTokenizerState.SelfClosingStartTag:
                return RunSelfClosingStartTag(context);
            default:
                throw new InvalidOperationException("The current tokenizer state is not a tag state.");
        }
    }

    internal static bool RunTagOpen(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '/')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.EndTagOpen);
            return true;
        }

        if (current == '!')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.MarkupDeclarationOpen);
            return true;
        }

        if (current == '?')
        {
            int questionPosition = context.GetInputPosition();
            context.BeginComment();
            context.AddTokenizerDiagnosticWithCode(HtmlDiagnosticCode.TokenizerMalformedComment,
                "A question mark was used where a tag name was expected.",
                questionPosition, questionPosition + 1);
            context.SetState(HtmlTokenizerState.BogusComment);
            return true;
        }

        if (Ascii.IsAlpha(current))
        {
            context.BeginTag(HtmlTokenKind.StartTag);
            context.SetState(HtmlTokenizerState.TagName);
            return true;
        }

        int position = context.GetInputPosition();
        context.AddTokenizerDiagnostic("An invalid first character followed a less-than sign.",
            position, position + 1);
        return context.EmitMarkupCharacters("<");
    }

    internal static bool RunEndTagOpen(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsAlpha(current))
        {
            context.BeginTag(HtmlTokenKind.EndTag);
            context.SetState(HtmlTokenizerState.TagName);
            return true;
        }

        if (current == '>')
        {
            int start = context.GetMarkupStart();
            context.ConsumeInput();
            context.AddTokenizerDiagnostic("An end tag was missing its name.",
                start, context.GetInputPosition());
            context.AbandonCurrentTag();
            return true;
        }

        int position = context.GetInputPosition();
        context.BeginComment();
        context.AddTokenizerDiagnosticWithCode(HtmlDiagnosticCode.TokenizerMalformedComment,
            "An invalid first character followed an end-tag opener.",
            position, position + 1);
        context.SetState(HtmlTokenizerState.BogusComment);
        return true;
    }

    internal static bool RunTagName(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.BeforeAttributeName);
            return true;
        }

        if (current == '/')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.SelfClosingStartTag);
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentTag();
        }

        context.ConsumeInput();
        if (!context.GetCurrentTag().AppendName(current))
        {
            context.MarkCurrentTagLimitExceeded();
        }

        return true;
    }

    internal static bool RunBeforeAttributeName(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            return true;
        }

        if (current == '/')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.SelfClosingStartTag);
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentTag();
        }

        int attributeStart = context.GetInputPosition();
        if (!context.GetCurrentTag().StartAttribute(attributeStart))
        {
            context.MarkCurrentTagLimitExceeded();
            return true;
        }

        context.SetState(HtmlTokenizerState.AttributeName);
        if (current == '=')
        {
            int position = context.GetInputPosition();
            context.ConsumeInput();
            if (!context.GetCurrentTag().AppendAttributeName(current))
            {
                context.MarkCurrentTagLimitExceeded();
            }

            context.GetCurrentTag().SetCurrentAttributeSourceEnd(
                context.GetInputPosition());

            context.AddTokenizerDiagnostic("An equals sign appeared before an attribute name.",
                position, context.GetInputPosition());
        }

        return true;
    }

    internal static bool RunAttributeName(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current) || current == '/' || current == '>')
        {
            CompleteAttributeName(context);
            context.SetState(HtmlTokenizerState.AfterAttributeName);
            return true;
        }

        if (current == '=')
        {
            CompleteAttributeName(context);
            context.ConsumeInput();
            context.GetCurrentTag().SetCurrentAttributeSourceEnd(
                context.GetInputPosition());
            context.SetState(HtmlTokenizerState.BeforeAttributeValue);
            return true;
        }

        context.ConsumeInput();
        if (!context.GetCurrentTag().AppendAttributeName(current))
        {
            context.MarkCurrentTagLimitExceeded();
        }

        context.GetCurrentTag().SetCurrentAttributeSourceEnd(
            context.GetInputPosition());

        if (current == '"' || current == '\'' || current == '<')
        {
            int end = context.GetInputPosition();
            context.AddTokenizerDiagnostic("An unexpected character appeared in an attribute name.",
                end - 1, end);
        }

        return true;
    }

    internal static bool RunAfterAttributeName(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            return true;
        }

        if (current == '/')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.SelfClosingStartTag);
            return true;
        }

        if (current == '=')
        {
            context.ConsumeInput();
            context.GetCurrentTag().SetCurrentAttributeSourceEnd(
                context.GetInputPosition());
            context.SetState(HtmlTokenizerState.BeforeAttributeValue);
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentTag();
        }

        int attributeStart = context.GetInputPosition();
        if (!context.GetCurrentTag().StartAttribute(attributeStart))
        {
            context.MarkCurrentTagLimitExceeded();
            return true;
        }

        context.SetState(HtmlTokenizerState.AttributeName);
        return true;
    }

    internal static bool RunBeforeAttributeValue(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            context.GetCurrentTag().SetCurrentAttributeSourceEnd(
                context.GetInputPosition());
            return true;
        }

        if (current == '"')
        {
            context.ConsumeInput();
            context.GetCurrentTag().SetCurrentAttributeSourceEnd(
                context.GetInputPosition());
            context.SetState(HtmlTokenizerState.AttributeValueDoubleQuoted);
            return true;
        }

        if (current == '\'')
        {
            context.ConsumeInput();
            context.GetCurrentTag().SetCurrentAttributeSourceEnd(
                context.GetInputPosition());
            context.SetState(HtmlTokenizerState.AttributeValueSingleQuoted);
            return true;
        }

        if (current == '>')
        {
            int position = context.GetInputPosition();
            context.GetCurrentTag().SetCurrentAttributeSourceEnd(position);
            context.ConsumeInput();
            context.AddTokenizerDiagnostic("An attribute was missing its value.",
                position, context.GetInputPosition());
            return context.EmitCurrentTag();
        }

        context.SetState(HtmlTokenizerState.AttributeValueUnquoted);
        return true;
    }

    internal static bool RunQuotedAttributeValue(HtmlTokenizerContext context, char quote)
    {
        char current = (char)context.PeekInput();
        if (current == '&')
        {
            return RunAttributeCharacterReference(context, quote);
        }

        context.ConsumeInput();
        context.GetCurrentTag().SetCurrentAttributeSourceEnd(
            context.GetInputPosition());
        if (current == quote)
        {
            context.SetState(HtmlTokenizerState.AfterAttributeValueQuoted);
            return true;
        }

        if (!context.GetCurrentTag().AppendAttributeValue(current))
        {
            context.MarkCurrentTagLimitExceeded();
        }

        return true;
    }

    internal static bool RunUnquotedAttributeValue(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.BeforeAttributeName);
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentTag();
        }

        if (current == '&')
        {
            return RunAttributeCharacterReference(context, '>');
        }

        context.ConsumeInput();
        if (!context.GetCurrentTag().AppendAttributeValue(current))
        {
            context.MarkCurrentTagLimitExceeded();
        }

        context.GetCurrentTag().SetCurrentAttributeSourceEnd(
            context.GetInputPosition());

        if (current == '"' || current == '\'' || current == '<'
            || current == '=' || current == '`')
        {
            int end = context.GetInputPosition();
            context.AddTokenizerDiagnostic(
                "An unexpected character appeared in an unquoted attribute value.",
                end - 1, end);
        }

        return true;
    }

    internal static bool RunAttributeCharacterReference(HtmlTokenizerContext context,
        char additionalAllowedCharacter)
    {
        ReadOnlySpan<char> available = context.GetAvailableInputSpan();
        HtmlCharacterReferenceResult reference = HtmlCharacterReferenceDecoder.Decode(
            available.Slice(1), context.IsInputComplete(),
            HtmlCharacterReferenceContext.Attribute, additionalAllowedCharacter);
        if (reference.GetStatus() == HtmlCharacterReferenceStatus.NeedMoreInput)
        {
            return false;
        }

        int ampersandOffset = context.GetInputPosition();
        context.AddCharacterReferenceDiagnostics(reference, ampersandOffset);
        string replacement = reference.IsMatch()
            ? reference.GetReplacementString() : "&";
        int consumed = reference.IsMatch()
            ? 1 + reference.GetConsumedCharacters() : 1;
        Consume(context, consumed);
        context.GetCurrentTag().SetCurrentAttributeSourceEnd(
            context.GetInputPosition());
        if (!context.GetCurrentTag().AppendAttributeValueString(replacement))
        {
            context.MarkCurrentTagLimitExceeded();
            return false;
        }

        return true;
    }

    internal static bool RunAfterAttributeValueQuoted(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (Ascii.IsHtmlWhitespace(current))
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.BeforeAttributeName);
            return true;
        }

        if (current == '/')
        {
            context.ConsumeInput();
            context.SetState(HtmlTokenizerState.SelfClosingStartTag);
            return true;
        }

        if (current == '>')
        {
            context.ConsumeInput();
            return context.EmitCurrentTag();
        }

        int position = context.GetInputPosition();
        context.AddTokenizerDiagnostic("Whitespace was missing between attributes.",
            position, position + 1);
        context.SetState(HtmlTokenizerState.BeforeAttributeName);
        return true;
    }

    internal static bool RunSelfClosingStartTag(HtmlTokenizerContext context)
    {
        char current = (char)context.PeekInput();
        if (current == '>')
        {
            context.ConsumeInput();
            context.GetCurrentTag().SetSelfClosing();
            return context.EmitCurrentTag();
        }

        int position = context.GetInputPosition();
        context.AddTokenizerDiagnostic("An unexpected solidus appeared in a tag.",
            position, position + 1);
        context.SetState(HtmlTokenizerState.BeforeAttributeName);
        return true;
    }

    internal static bool HandleEndOfInput(HtmlTokenizerContext context)
    {
        HtmlTokenizerState state = context.GetState();
        if (state == HtmlTokenizerState.AttributeName)
        {
            CompleteAttributeName(context);
        }

        int start = context.GetMarkupStart();
        int end = context.GetInputPosition();
        context.AddTokenizerDiagnostic("The input ended before the current tag was complete.",
            start, end);
        if (state == HtmlTokenizerState.TagOpen)
        {
            return context.EmitMarkupCharacters("<");
        }

        if (state == HtmlTokenizerState.EndTagOpen)
        {
            return context.EmitMarkupCharacters("</");
        }

        context.AbandonCurrentTag();
        return true;
    }

    internal static void CompleteAttributeName(HtmlTokenizerContext context)
    {
        HtmlTagTokenBuilder builder = context.GetCurrentTag();
        if (builder.CompleteCurrentAttributeName())
        {
            context.AddTokenizerDiagnosticWithCode(HtmlDiagnosticCode.TokenizerDuplicateAttribute,
                "A duplicate attribute was discarded.",
                builder.GetCurrentAttributeSourceStart(), context.GetInputPosition());
        }
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
