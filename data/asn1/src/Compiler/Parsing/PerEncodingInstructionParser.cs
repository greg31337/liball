namespace asn1;

internal class PerEncodingInstructionParser
{
    internal AsnToken[] Tokens;
    internal AsnToken[] TargetTokens = [];
    internal int TargetPosition;

    internal PerEncodingInstructionParser(AsnToken[] tokens)
    {
        this.Tokens = tokens == null
            ? throw new System.ArgumentNullException("tokens")
            : tokens;
    }

    internal PerEncodingInstructionParseResult ParsePrefix()
    {
        PerEncodingInstructionParseResult result = new PerEncodingInstructionParseResult();
        AsnToken[] significant = this.CopySignificantTokens(this.Tokens);
        ParsedEncodingInstructionSyntax? instruction = this.ParseInstruction(
            significant,
            [],
            result);
        if (instruction != null)
        {
            result.Instructions = [instruction];
        }

        return result;
    }

    internal PerEncodingInstructionParseResult ParseSection()
    {
        PerEncodingInstructionParseResult result = new PerEncodingInstructionParseResult();
        AsnToken[] significant = this.CopySignificantTokens(this.Tokens);
        if (significant.Length == 0
            || significant[0].Kind != AsnTokenKind.LeftBracket)
        {
            return result;
        }

        System.Collections.Generic.List<ParsedEncodingInstructionSyntax> instructions =
            new System.Collections.Generic.List<ParsedEncodingInstructionSyntax>();
        int position = 0;
        while (position < significant.Length && result.Success)
        {
            AsnToken left = significant[position];
            if (left.Kind != AsnTokenKind.LeftBracket)
            {
                this.Fail(
                    result,
                    "Expected '[' before a PER encoding instruction.",
                    left);
                continue;
            }

            position = position + 1;
            int bodyStart = position;
            while (position < significant.Length
                && significant[position].Kind != AsnTokenKind.RightBracket)
            {
                if (significant[position].Kind == AsnTokenKind.LeftBracket)
                {
                    this.Fail(
                        result,
                        "A PER encoding instruction cannot contain a bracket.",
                        significant[position]);
                }

                position = position + 1;
            }

            if (!result.Success)
            {
                continue;
            }

            if (position == significant.Length)
            {
                this.Fail(
                    result,
                    "A PER encoding instruction is missing its closing bracket.",
                    left);
                continue;
            }

            AsnToken[] body = this.CopyRange(significant, bodyStart, position);
            position = position + 1;
            int targetStart = position;
            while (position < significant.Length
                && significant[position].Kind != AsnTokenKind.LeftBracket)
            {
                if (significant[position].Kind == AsnTokenKind.RightBracket)
                {
                    this.Fail(
                        result,
                        "A PER target list contains an unmatched closing bracket.",
                        significant[position]);
                }

                position = position + 1;
            }

            if (!result.Success)
            {
                continue;
            }

            AsnToken[] targets = this.CopyRange(significant, targetStart, position);
            ParsedEncodingInstructionSyntax? instruction = this.ParseInstruction(
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

    internal ParsedEncodingInstructionSyntax? ParseInstruction(
        AsnToken[] bodyTokens,
        AsnToken[] targetTokens,
        PerEncodingInstructionParseResult result)
    {
        if (bodyTokens.Length == 0)
        {
            this.Fail(result, "A PER encoding instruction requires a keyword.", null);
            return null;
        }

        int nameIndex = bodyTokens[0].Text == "NOT" ? 1 : 0;
        if (nameIndex == bodyTokens.Length)
        {
            this.Fail(
                result,
                "NOT must be followed by a PER instruction keyword.",
                bodyTokens[0]);
            return null;
        }

        AsnToken name = bodyTokens[nameIndex];
        if (!this.IsIdentifyingKeyword(name))
        {
            this.Fail(result, "Expected a PER instruction keyword.", name);
            return null;
        }

        ParsedPerEncodingInstructionTargetSyntax[] targets =
            this.ParseTargetList(targetTokens, result);
        if (!result.Success)
        {
            return null;
        }

        AsnToken[] arguments = this.CopyRange(
            bodyTokens,
            nameIndex + 1,
            bodyTokens.Length);
        string[] targetTexts = new string[targets.Length];
        int targetIndex = 0;
        while (targetIndex < targets.Length)
        {
            targetTexts[targetIndex] = targets[targetIndex].Text;
            targetIndex = targetIndex + 1;
        }

        return new ParsedEncodingInstructionSyntax
        {
            Instruction = new AsnEncodingInstruction
            {
                Name = name.Text,
                Kind = AsnInstructionKind.PerApplication,
                Negated = nameIndex == 1,
                Arguments = this.CopyTexts(arguments),
                Targets = targetTexts,
                Span = this.CreateInstructionSpan(bodyTokens, targetTokens)
            },
            ArgumentTokens = arguments,
            PerTargets = targets
        };
    }

    internal ParsedPerEncodingInstructionTargetSyntax[] ParseTargetList(
        AsnToken[] tokens,
        PerEncodingInstructionParseResult result)
    {
        this.TargetTokens = tokens;
        this.TargetPosition = 0;
        System.Collections.Generic.List<ParsedPerEncodingInstructionTargetSyntax> targets =
            new System.Collections.Generic.List<ParsedPerEncodingInstructionTargetSyntax>();
        while (this.TargetPosition < this.TargetTokens.Length && result.Success)
        {
            ParsedPerEncodingInstructionTargetSyntax? target = this.ParseTarget(result);
            if (target != null)
            {
                targets.Add(target);
            }

            if (this.TargetPosition < this.TargetTokens.Length && result.Success)
            {
                AsnToken separator = this.TargetTokens[this.TargetPosition];
                if (separator.Kind != AsnTokenKind.Comma)
                {
                    this.Fail(result, "Expected ',' between PER instruction targets.", separator);
                }
                else
                {
                    this.TargetPosition = this.TargetPosition + 1;
                    if (this.TargetPosition == this.TargetTokens.Length)
                    {
                        this.Fail(
                            result,
                            "A PER target list cannot end with ','.",
                            separator);
                    }
                }
            }
        }

        return targets.ToArray();
    }

    internal ParsedPerEncodingInstructionTargetSyntax? ParseTarget(
        PerEncodingInstructionParseResult result)
    {
        AsnToken first = this.TargetTokens[this.TargetPosition];
        if (this.IsContextTargetStart(first))
        {
            int marker = this.FindContextMarker();
            if (marker >= 0)
            {
                return this.ParseContextTarget(marker, result);
            }
        }

        if (first.Text == "ALL")
        {
            this.TargetPosition = this.TargetPosition + 1;
            return new ParsedPerEncodingInstructionTargetSyntax
            {
                Kind = PerEncodingInstructionTargetKind.AllTypes,
                Text = "ALL",
                Span = first.Span
            };
        }

        string builtIn = this.ReadBuiltInTypeName();
        if (builtIn.Length > 0)
        {
            AsnToken lastBuiltIn = this.TargetTokens[this.TargetPosition - 1];
            return new ParsedPerEncodingInstructionTargetSyntax
            {
                Kind = PerEncodingInstructionTargetKind.BuiltInType,
                Text = builtIn,
                BuiltInType = builtIn,
                Span = this.CreateSpan(first, lastBuiltIn)
            };
        }

        return this.ParseTypeTarget(result);
    }

    internal ParsedPerEncodingInstructionTargetSyntax? ParseContextTarget(
        int marker,
        PerEncodingInstructionParseResult result)
    {
        AsnToken first = this.TargetTokens[this.TargetPosition];
        System.Collections.Generic.List<string> identifiers =
            new System.Collections.Generic.List<string>();
        bool all = false;
        bool components = false;
        if (first.Text == "ALL")
        {
            all = true;
            this.TargetPosition = this.TargetPosition + 1;
        }
        else if (first.Text == "COMPONENTS")
        {
            components = true;
            this.TargetPosition = this.TargetPosition + 1;
        }
        else
        {
            while (this.TargetPosition < marker)
            {
                AsnToken identifier = this.TargetTokens[this.TargetPosition];
                if (!this.IsIdentifier(identifier))
                {
                    this.Fail(
                        result,
                        "Expected a component identifier before IN.",
                        identifier);
                    return null;
                }

                identifiers.Add(identifier.Text);
                this.TargetPosition = this.TargetPosition + 1;
                if (this.TargetPosition < marker)
                {
                    AsnToken comma = this.TargetTokens[this.TargetPosition];
                    if (comma.Kind != AsnTokenKind.Comma)
                    {
                        this.Fail(
                            result,
                            "Expected ',' between identifiers before IN.",
                            comma);
                        return null;
                    }

                    this.TargetPosition = this.TargetPosition + 1;
                }
            }
        }

        if (this.TargetPosition != marker)
        {
            this.Fail(result, "Invalid identifier list before IN.", first);
            return null;
        }

        this.TargetPosition = this.TargetPosition + 1;
        ParsedPerEncodingInstructionTargetSyntax? context = this.ParseTypeTarget(result);
        if (context == null)
        {
            return null;
        }

        string identifierText = all
            ? "ALL"
            : components
                ? "COMPONENTS"
                : this.JoinIdentifiers(identifiers.ToArray());
        return new ParsedPerEncodingInstructionTargetSyntax
        {
            Kind = PerEncodingInstructionTargetKind.IdentifiersInContext,
            Text = identifierText + " IN " + context.Text,
            TypeReference = context.TypeReference,
            ComponentPath = context.ComponentPath,
            Identifiers = identifiers.ToArray(),
            AllIdentifiers = all,
            Components = components,
            Span = this.CreateSpan(first, this.TargetTokens[this.TargetPosition - 1])
        };
    }

    internal ParsedPerEncodingInstructionTargetSyntax? ParseTypeTarget(
        PerEncodingInstructionParseResult result)
    {
        if (this.TargetPosition >= this.TargetTokens.Length)
        {
            this.Fail(result, "Expected a type identification target.", null);
            return null;
        }

        AsnToken first = this.TargetTokens[this.TargetPosition];
        if (first.Text == "ALL")
        {
            this.TargetPosition = this.TargetPosition + 1;
            return new ParsedPerEncodingInstructionTargetSyntax
            {
                Kind = PerEncodingInstructionTargetKind.AllTypes,
                Text = "ALL",
                TypeReference = "ALL",
                Span = first.Span
            };
        }

        if (!this.IsTypeReference(first))
        {
            this.Fail(result, "Expected a PER type-reference target.", first);
            return null;
        }

        this.TargetPosition = this.TargetPosition + 1;
        System.Collections.Generic.List<string> path = new System.Collections.Generic.List<string>();
        AsnToken last = first;
        bool allComponent = false;
        while (this.TargetPosition < this.TargetTokens.Length
            && this.TargetTokens[this.TargetPosition].Kind == AsnTokenKind.Dot)
        {
            AsnToken dot = this.TargetTokens[this.TargetPosition];
            if (allComponent)
            {
                this.Fail(result, "ALL must be the last component in a PER target path.", dot);
                return null;
            }

            this.TargetPosition = this.TargetPosition + 1;
            if (this.TargetPosition == this.TargetTokens.Length)
            {
                this.Fail(result, "A PER target path cannot end with '.'.", dot);
                return null;
            }

            AsnToken component = this.TargetTokens[this.TargetPosition];
            if (!this.IsComponentId(component))
            {
                this.Fail(result, "Expected a component identifier after '.'.", component);
                return null;
            }

            path.Add(component.Text);
            allComponent = component.Text == "ALL";
            last = component;
            this.TargetPosition = this.TargetPosition + 1;
        }

        string[] components = path.ToArray();
        return new ParsedPerEncodingInstructionTargetSyntax
        {
            Kind = PerEncodingInstructionTargetKind.TypeReference,
            Text = this.JoinTypePath(first.Text, components),
            TypeReference = first.Text,
            ComponentPath = components,
            Span = this.CreateSpan(first, last)
        };
    }

    internal int FindContextMarker()
    {
        AsnToken first = this.TargetTokens[this.TargetPosition];
        if (first.Text == "ALL" || first.Text == "COMPONENTS")
        {
            int next = this.TargetPosition + 1;
            return next < this.TargetTokens.Length
                && this.TargetTokens[next].Text == "IN"
                ? next
                : -1;
        }

        int index = this.TargetPosition;
        while (index < this.TargetTokens.Length)
        {
            AsnToken token = this.TargetTokens[index];
            if (token.Text == "IN")
            {
                return index;
            }

            if (token.Kind != AsnTokenKind.Comma
                && !this.IsIdentifier(token)
                && token.Text != "ALL"
                && token.Text != "COMPONENTS")
            {
                return -1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal string ReadBuiltInTypeName()
    {
        string one = this.TargetTokens[this.TargetPosition].Text;
        if (this.TargetPosition + 1 < this.TargetTokens.Length)
        {
            string two = one + " " + this.TargetTokens[this.TargetPosition + 1].Text;
            if (this.IsBuiltInTypeName(two))
            {
                this.TargetPosition = this.TargetPosition + 2;
                return two;
            }
        }

        if (this.IsBuiltInTypeName(one))
        {
            this.TargetPosition = this.TargetPosition + 1;
            return one;
        }

        return "";
    }

    internal bool IsBuiltInTypeName(string text)
    {
        switch (text)
        {
            case "BIT STRING":
            case "BOOLEAN":
            case "CHARACTER STRING":
            case "CHOICE":
            case "DATE":
            case "DATE-TIME":
            case "DURATION":
            case "EMBEDDED PDV":
            case "ENUMERATED":
            case "EXTERNAL":
            case "GeneralizedTime":
            case "INSTANCE OF":
            case "INTEGER":
            case "NULL":
            case "ObjectDescriptor":
            case "OBJECT IDENTIFIER":
            case "OCTET STRING":
            case "OID-IRI":
            case "REAL":
            case "RELATIVE-OID":
            case "RELATIVE-OID-IRI":
            case "SEQUENCE":
            case "SEQUENCE OF":
            case "SET":
            case "SET OF":
            case "TIME":
            case "TIME-OF-DAY":
            case "UTCTime":
            case "BMPString":
            case "GeneralString":
            case "GraphicString":
            case "IA5String":
            case "ISO646String":
            case "NumericString":
            case "PrintableString":
            case "T61String":
            case "TeletexString":
            case "UniversalString":
            case "UTF8String":
            case "VideotexString":
            case "VisibleString":
                return true;
            default:
                return false;
        }
    }

    internal bool IsIdentifyingKeyword(AsnToken token)
    {
        return token.Kind == AsnTokenKind.EncodingReference
            || token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ModuleReference
            || token.Kind == AsnTokenKind.ObjectClassReference
            || token.Kind == AsnTokenKind.ObjectSetReference
            || token.Kind == AsnTokenKind.ReservedWord;
    }

    internal bool IsContextTargetStart(AsnToken token)
    {
        return this.IsIdentifier(token)
            || token.Text == "ALL"
            || token.Text == "COMPONENTS";
    }

    internal bool IsIdentifier(AsnToken token)
    {
        return token.Kind == AsnTokenKind.Identifier
            || token.Kind == AsnTokenKind.ValueReference
            || token.Kind == AsnTokenKind.ObjectReference;
    }

    internal bool IsTypeReference(AsnToken token)
    {
        return token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ModuleReference
            || token.Kind == AsnTokenKind.ObjectClassReference
            || token.Kind == AsnTokenKind.ObjectSetReference;
    }

    internal bool IsComponentId(AsnToken token)
    {
        return this.IsIdentifier(token)
            || token.Kind == AsnTokenKind.Asterisk
            || token.Text == "ALL";
    }

    internal string JoinIdentifiers(string[] identifiers)
    {
        string result = "";
        int index = 0;
        while (index < identifiers.Length)
        {
            if (index > 0)
            {
                result = result + ",";
            }

            result = result + identifiers[index];
            index = index + 1;
        }

        return result;
    }

    internal string JoinTypePath(string typeReference, string[] path)
    {
        string result = typeReference;
        int index = 0;
        while (index < path.Length)
        {
            result = result + "." + path[index];
            index = index + 1;
        }

        return result;
    }

    internal string[] CopyTexts(AsnToken[] tokens)
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
        if (targets.Length > 0)
        {
            last = targets[targets.Length - 1];
        }

        return this.CreateSpan(first, last);
    }

    internal AsnSourceSpan? CreateSpan(AsnToken first, AsnToken last)
    {
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

    internal void Fail(
        PerEncodingInstructionParseResult result,
        string message,
        AsnToken? token)
    {
        result.Success = false;
        result.ErrorMessage = message;
        result.ErrorToken = token;
    }
}
