namespace asn1;

internal class XerEncodingInstructionParser
{
    internal AsnToken[] Tokens;

    internal XerEncodingInstructionParser(AsnToken[] tokens)
    {
        this.Tokens = tokens == null
            ? throw new System.ArgumentNullException("tokens")
            : tokens;
    }

    internal XerEncodingInstructionParseResult ParseSection()
    {
        AsnToken[] significant = this.CopySignificantTokens(this.Tokens);
        if (significant.Length == 0)
        {
            return new XerEncodingInstructionParseResult();
        }

        if (significant[0].Kind == AsnTokenKind.LeftBracket)
        {
            return this.ParseBracketedList(significant);
        }

        return this.ParseHistoricalList();
    }

    internal XerEncodingInstructionParseResult ParsePrefix()
    {
        AsnToken[] significant = this.CopySignificantTokens(this.Tokens);
        XerEncodingInstructionParseResult result = new XerEncodingInstructionParseResult();
        ParsedEncodingInstructionSyntax? instruction = this.ParseInstructionParts(
            significant,
            [],
            result);
        if (instruction != null)
        {
            result.Instructions = [instruction];
        }

        return result;
    }

    internal XerEncodingInstructionParseResult ParseBracketedList(AsnToken[] tokens)
    {
        XerEncodingInstructionParseResult result = new XerEncodingInstructionParseResult();
        System.Collections.Generic.List<ParsedEncodingInstructionSyntax> instructions =
            new System.Collections.Generic.List<ParsedEncodingInstructionSyntax>();
        int position = 0;
        while (position < tokens.Length && result.Success)
        {
            AsnToken left = tokens[position];
            if (left.Kind != AsnTokenKind.LeftBracket)
            {
                this.Fail(result, "Expected '[' before an XER encoding instruction.", left);
                continue;
            }

            position = position + 1;
            int bodyStart = position;
            while (position < tokens.Length && tokens[position].Kind != AsnTokenKind.RightBracket)
            {
                position = position + 1;
            }

            if (position == tokens.Length)
            {
                this.Fail(result, "An XER encoding instruction is missing its closing bracket.", left);
                continue;
            }

            AsnToken[] body = this.CopyRange(tokens, bodyStart, position);
            position = position + 1;
            int targetStart = position;
            while (position < tokens.Length && tokens[position].Kind != AsnTokenKind.LeftBracket)
            {
                position = position + 1;
            }

            AsnToken[] targets = this.CopyRange(tokens, targetStart, position);
            ParsedEncodingInstructionSyntax? instruction = this.ParseInstructionParts(
                body,
                targets,
                result);
            if (instruction != null)
            {
                instructions.Add(instruction);
            }
        }

        result.Instructions = instructions.ToArray();
        return result;
    }

    internal XerEncodingInstructionParseResult ParseHistoricalList()
    {
        XerEncodingInstructionParseResult result = new XerEncodingInstructionParseResult();
        System.Collections.Generic.List<ParsedEncodingInstructionSyntax> instructions =
            new System.Collections.Generic.List<ParsedEncodingInstructionSyntax>();
        AsnToken[] tokens = this.CopySignificantTokens(this.Tokens);
        if (tokens.Length == 0 || !this.IsInstructionStart(tokens, 0))
        {
            return result;
        }

        int position = 0;
        while (position < tokens.Length && result.Success)
        {
            int next = tokens[position].Text == "NOT" ? position + 2 : position + 1;
            while (next < tokens.Length)
            {
                bool qualifiedKeyword = next > 0
                    && tokens[next - 1].Kind == AsnTokenKind.Dot;
                if (!qualifiedKeyword && this.IsInstructionStart(tokens, next))
                {
                    break;
                }

                next = next + 1;
            }

            ParsedEncodingInstructionSyntax? instruction = this.ParseHistoricalInstruction(
                this.CopyRange(tokens, position, next),
                result);
            if (instruction != null)
            {
                instructions.Add(instruction);
            }

            position = next;
        }

        result.Instructions = instructions.ToArray();
        return result;
    }

    internal bool IsInstructionStart(AsnToken[] tokens, int index)
    {
        if (index >= tokens.Length)
        {
            return false;
        }

        if (tokens[index].Text == "NOT")
        {
            return index + 1 < tokens.Length
                && this.GetInstructionKind(tokens[index + 1].Text) != AsnInstructionKind.Unknown;
        }

        return this.GetInstructionKind(tokens[index].Text) != AsnInstructionKind.Unknown;
    }

    internal ParsedEncodingInstructionSyntax? ParseHistoricalInstruction(
        AsnToken[] tokens,
        XerEncodingInstructionParseResult result)
    {
        int keywordIndex = this.FindInstructionKeyword(tokens);
        if (keywordIndex < 0)
        {
            return null;
        }

        if (keywordIndex > 0 && tokens[0].Text != "NOT")
        {
            return null;
        }

        int nameIndex = tokens[0].Text == "NOT" ? 1 : 0;
        AsnInstructionKind kind = this.GetInstructionKind(tokens[nameIndex].Text);
        int argumentStart = this.FindHistoricalArgumentStart(tokens, nameIndex, kind);
        AsnToken[] targetTokens = this.CopyRange(tokens, nameIndex + 1, argumentStart);
        AsnToken[] bodyTokens = this.CombineInstructionAndArguments(
            tokens,
            nameIndex,
            argumentStart);
        return this.ParseInstructionParts(bodyTokens, targetTokens, result);
    }

    internal ParsedEncodingInstructionSyntax? ParseInstructionParts(
        AsnToken[] bodyTokens,
        AsnToken[] targetTokens,
        XerEncodingInstructionParseResult result)
    {
        if (bodyTokens.Length == 0)
        {
            this.Fail(result, "An XER encoding instruction requires a keyword.", null);
            return null;
        }

        int nameIndex = bodyTokens[0].Text == "NOT" ? 1 : 0;
        if (nameIndex == bodyTokens.Length)
        {
            this.Fail(result, "NOT must be followed by an XER instruction keyword.", bodyTokens[0]);
            return null;
        }

        AsnToken name = bodyTokens[nameIndex];
        AsnInstructionKind kind = this.GetInstructionKind(name.Text);
        if (kind == AsnInstructionKind.Unknown)
        {
            this.Fail(result, "Unknown XER encoding instruction '" + name.Text + "'.", name);
            return null;
        }

        bool negated = nameIndex == 1 || name.Text == "ELEMENT";
        AsnToken[] arguments = this.CopyRange(bodyTokens, nameIndex + 1, bodyTokens.Length);
        ParsedEncodingInstructionSyntax syntax = new ParsedEncodingInstructionSyntax
        {
            Instruction = new AsnEncodingInstruction
            {
                Name = name.Text,
                Kind = kind,
                Negated = negated,
                Arguments = this.CopyArgumentTexts(arguments),
                Targets = this.ParseTargets(targetTokens),
                Span = this.CreateInstructionSpan(bodyTokens, targetTokens)
            },
            ArgumentTokens = arguments
        };
        this.ParseNamespaceSettings(syntax);
        return syntax;
    }

    internal int FindHistoricalArgumentStart(
        AsnToken[] tokens,
        int nameIndex,
        AsnInstructionKind kind)
    {
        if (kind == AsnInstructionKind.GlobalDefaults)
        {
            return nameIndex + 1;
        }

        string marker = "";
        if (kind == AsnInstructionKind.Name
            || kind == AsnInstructionKind.DefaultForEmpty
            || kind == AsnInstructionKind.Namespace
            || kind == AsnInstructionKind.PiOrComment
            || kind == AsnInstructionKind.Text)
        {
            marker = "AS";
        }

        int index = nameIndex + 1;
        while (index < tokens.Length)
        {
            if ((kind == AsnInstructionKind.AnyAttributes
                    || kind == AsnInstructionKind.AnyElement)
                && (tokens[index].Text == "FROM"
                    || tokens[index].Text == "EXCEPT"))
            {
                return index;
            }

            if (marker.Length > 0 && tokens[index].Text == marker)
            {
                return index;
            }

            if (kind == AsnInstructionKind.Whitespace
                && (tokens[index].Text == "REPLACE" || tokens[index].Text == "COLLAPSE"))
            {
                return index;
            }

            index = index + 1;
        }

        return tokens.Length;
    }

    internal string[] CopyArgumentTexts(AsnToken[] tokens)
    {
        string[] result = new string[tokens.Length];
        int index = 0;
        while (index < tokens.Length)
        {
            result[index] = tokens[index].Text;
            index = index + 1;
        }

        return result;
    }

    internal AsnToken[] CombineInstructionAndArguments(
        AsnToken[] tokens,
        int nameIndex,
        int argumentStart)
    {
        int prefixCount = nameIndex == 1 ? 2 : 1;
        int argumentCount = tokens.Length - argumentStart;
        AsnToken[] result = new AsnToken[prefixCount + argumentCount];
        int index = 0;
        while (index < prefixCount)
        {
            result[index] = tokens[index];
            index = index + 1;
        }

        int argumentIndex = 0;
        while (argumentIndex < argumentCount)
        {
            result[prefixCount + argumentIndex] = tokens[argumentStart + argumentIndex];
            argumentIndex = argumentIndex + 1;
        }

        return result;
    }

    internal string[] ParseTargets(AsnToken[] tokens)
    {
        System.Collections.Generic.List<string> targets = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<AsnToken> current = new System.Collections.Generic.List<AsnToken>();
        int index = 0;
        while (index <= tokens.Length)
        {
            bool boundary = index == tokens.Length
                || tokens[index].Kind == AsnTokenKind.Comma;
            if (index < tokens.Length
                && tokens[index].Kind == AsnTokenKind.Comma
                && this.CanExtendContextIdentifiers(current)
                && this.HasContextMarkerAhead(tokens, index + 1))
            {
                boundary = false;
            }

            if (boundary)
            {
                if (current.Count > 0)
                {
                    targets.Add(this.JoinTargetTokens(current.ToArray()));
                    current.Clear();
                }
            }
            else if (tokens[index].Kind != AsnTokenKind.Whitespace
                && tokens[index].Kind != AsnTokenKind.Comment)
            {
                current.Add(tokens[index]);
            }

            index = index + 1;
        }

        return targets.ToArray();
    }

    internal bool CanExtendContextIdentifiers(
        System.Collections.Generic.List<AsnToken> tokens)
    {
        int index = 0;
        while (index < tokens.Count)
        {
            if (tokens[index].Kind != AsnTokenKind.Identifier
                && tokens[index].Kind != AsnTokenKind.Comma)
            {
                return false;
            }

            index = index + 1;
        }

        return tokens.Count > 0;
    }

    internal bool HasContextMarkerAhead(AsnToken[] tokens, int start)
    {
        int index = start;
        while (index < tokens.Length)
        {
            if (tokens[index].Text == "IN")
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string JoinTargetTokens(AsnToken[] tokens)
    {
        string result = "";
        int index = 0;
        while (index < tokens.Length)
        {
            bool punctuation = tokens[index].Kind == AsnTokenKind.Dot
                || tokens[index].Kind == AsnTokenKind.Colon
                || tokens[index].Kind == AsnTokenKind.Comma;
            bool previousPunctuation = index > 0
                && (tokens[index - 1].Kind == AsnTokenKind.Dot
                    || tokens[index - 1].Kind == AsnTokenKind.Colon);
            if (index > 0 && !punctuation && !previousPunctuation)
            {
                result = result + " ";
            }

            result = result + tokens[index].Text;
            index = index + 1;
        }

        return result;
    }

    internal void ParseNamespaceSettings(ParsedEncodingInstructionSyntax syntax)
    {
        AsnToken[] arguments = syntax.ArgumentTokens;
        int index = 0;
        if (syntax.Instruction.Kind == AsnInstructionKind.GlobalDefaults)
        {
            if (index < arguments.Length && arguments[index].Text == "MODIFIED-ENCODINGS")
            {
                syntax.ModifiedEncodings = true;
                return;
            }

            if (index < arguments.Length && arguments[index].Text == "CONTROL-NAMESPACE")
            {
                syntax.ControlNamespace = true;
                index = index + 1;
            }
        }
        else if (syntax.Instruction.Kind == AsnInstructionKind.Namespace
            && index < arguments.Length
            && arguments[index].Text == "AS")
        {
            index = index + 1;
        }

        if ((syntax.Instruction.Kind == AsnInstructionKind.Namespace || syntax.ControlNamespace)
            && index < arguments.Length
            && arguments[index].Kind == AsnTokenKind.CharacterStringLiteral)
        {
            syntax.NamespaceUri = this.DecodeQuotedText(arguments[index].Text);
            index = index + 1;
            if (index + 1 < arguments.Length
                && arguments[index].Text == "PREFIX"
                && arguments[index + 1].Kind == AsnTokenKind.CharacterStringLiteral)
            {
                syntax.NamespacePrefix = this.DecodeQuotedText(arguments[index + 1].Text);
            }
        }
    }

    internal int FindInstructionKeyword(AsnToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Text == "NOT"
                && index + 1 < tokens.Length
                && this.GetInstructionKind(tokens[index + 1].Text) != AsnInstructionKind.Unknown)
            {
                return index;
            }

            if (this.GetInstructionKind(tokens[index].Text) != AsnInstructionKind.Unknown)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal AsnInstructionKind GetInstructionKind(string name)
    {
        switch (name)
        {
            case "ANY-ATTRIBUTES": return AsnInstructionKind.AnyAttributes;
            case "ANY-ELEMENT": return AsnInstructionKind.AnyElement;
            case "ATTRIBUTE": return AsnInstructionKind.Attribute;
            case "BASE64": return AsnInstructionKind.Base64;
            case "DECIMAL": return AsnInstructionKind.Decimal;
            case "DEFAULT-FOR-EMPTY": return AsnInstructionKind.DefaultForEmpty;
            case "ELEMENT": return AsnInstructionKind.Element;
            case "EMBED-VALUES": return AsnInstructionKind.EmbedValues;
            case "GLOBAL-DEFAULTS": return AsnInstructionKind.GlobalDefaults;
            case "LIST": return AsnInstructionKind.List;
            case "NAME": return AsnInstructionKind.Name;
            case "NAMESPACE": return AsnInstructionKind.Namespace;
            case "PI-OR-COMMENT": return AsnInstructionKind.PiOrComment;
            case "TEXT": return AsnInstructionKind.Text;
            case "UNTAGGED": return AsnInstructionKind.Untagged;
            case "USE-NIL": return AsnInstructionKind.UseNil;
            case "USE-NUMBER": return AsnInstructionKind.UseNumber;
            case "USE-ORDER": return AsnInstructionKind.UseOrder;
            case "USE-QNAME": return AsnInstructionKind.UseQName;
            case "USE-TYPE": return AsnInstructionKind.UseType;
            case "USE-UNION": return AsnInstructionKind.UseUnion;
            case "WHITESPACE": return AsnInstructionKind.Whitespace;
        }

        return AsnInstructionKind.Unknown;
    }

    internal AsnToken[] CopySignificantTokens(AsnToken[] tokens)
    {
        System.Collections.Generic.List<AsnToken> result = new System.Collections.Generic.List<AsnToken>();
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind != AsnTokenKind.Whitespace
                && tokens[index].Kind != AsnTokenKind.Comment
                && tokens[index].Kind != AsnTokenKind.Semicolon)
            {
                result.Add(tokens[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal AsnToken[] CopyRange(AsnToken[] tokens, int start, int end)
    {
        int length = end - start;
        AsnToken[] result = new AsnToken[length];
        int index = 0;
        while (index < length)
        {
            result[index] = tokens[start + index];
            index = index + 1;
        }

        return result;
    }

    internal AsnSourceSpan? CreateInstructionSpan(AsnToken[] body, AsnToken[] targets)
    {
        AsnToken first = body[0];
        AsnToken last = body[body.Length - 1];
        if (targets.Length > 0
            && targets[targets.Length - 1].Span != null
            && last.Span != null
            && targets[targets.Length - 1].Span?.End.Offset > last.Span.End.Offset)
        {
            last = targets[targets.Length - 1];
        }
        if (first.Span == null || last.Span == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = first.Span.Start,
            End = last.Span.End
        };
    }

    internal string DecodeQuotedText(string text)
    {
        if (text.Length < 2)
        {
            return "";
        }

        return text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
    }

    internal void Fail(
        XerEncodingInstructionParseResult result,
        string message,
        AsnToken? token)
    {
        result.Success = false;
        result.ErrorMessage = message;
        result.ErrorToken = token;
    }
}
