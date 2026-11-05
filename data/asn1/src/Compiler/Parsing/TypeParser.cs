namespace asn1;

internal class TypeParser
{
    internal AsnToken[] Tokens;
    internal DiagnosticBag Diagnostics;
    internal bool ReportDiagnostics;
    internal int Position;

    internal TypeParser(AsnToken[] tokens, DiagnosticBag diagnostics, bool reportDiagnostics)
    {
        this.Tokens = tokens == null ? throw new System.ArgumentNullException("tokens") : tokens;
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
        this.ReportDiagnostics = reportDiagnostics;
    }

    internal TypeParseResult ParseTypeSyntax()
    {
        int start = this.Position;
        TypeParseResult result = this.ParseUnconstrainedTypeSyntax();
        if (!result.Success || result.Syntax == null)
        {
            return result;
        }

        while (this.IsKind(this.PeekToken(), AsnTokenKind.LeftParenthesis))
        {
            AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
            ConstraintParseResult constraint = new ConstraintParser(
                remaining,
                this.Diagnostics,
                this.ReportDiagnostics).ParseConstraintSyntax(result.Syntax.Type);
            this.Position = this.Position + constraint.ConsumedTokenCount;
            if (!constraint.Success || constraint.Syntax == null)
            {
                return this.CreateResult(false, result.Syntax, start);
            }

            this.AppendConstraintSyntax(result.Syntax, constraint.Syntax);
        }

        if (this.Position > start)
        {
            result.Syntax.Span = this.CreateSpan(this.Tokens[start], this.PeekTokenAt(-1));
        }

        return this.CreateResult(true, result.Syntax, start);
    }

    internal TypeParseResult ParseUnconstrainedTypeSyntax()
    {
        int start = this.Position;
        AsnToken? first = this.PeekToken();
        if (first == null)
        {
            this.AddDiagnostic(14, "Expected an ASN.1 type.", null);
            return this.CreateResult(false, null, start);
        }

        TypeParseResult informationType = this.ParseTypeFromObject(start);
        if (informationType.Success)
        {
            return informationType;
        }

        TypeParseResult parameterizedType = this.ParseParameterizedTypeReference(start);
        if (parameterizedType.Success)
        {
            return parameterizedType;
        }

        if (first.Kind == AsnTokenKind.LeftBracket)
        {
            return this.ParsePrefixedType(start);
        }

        if (this.IsComponentName(first)
            && this.IsKind(this.PeekTokenAt(1), AsnTokenKind.LessThan))
        {
            return this.ParseSelectionType(start);
        }

        if (first.Text == "INTEGER")
        {
            return this.ParseIntegerType(start);
        }

        if (first.Text == "ENUMERATED")
        {
            return this.ParseEnumeratedType(start);
        }

        if (first.Text == "BIT")
        {
            return this.ParseBitStringType(start);
        }

        if (first.Text == "OCTET")
        {
            return this.ParseTwoWordType(start, "STRING", AsnTypeKind.OctetString);
        }

        if (first.Text == "OBJECT")
        {
            return this.ParseTwoWordType(start, "IDENTIFIER", AsnTypeKind.ObjectIdentifier);
        }

        if (first.Text == "CHARACTER")
        {
            return this.ParseTwoWordType(start, "STRING", AsnTypeKind.CharacterString);
        }

        if (first.Text == "INSTANCE")
        {
            return this.ParseInstanceOfType(start);
        }

        if (first.Text == "ANY")
        {
            return this.ParseLegacyAnyType(start);
        }

        if (first.Text == "MACRO")
        {
            this.Position = this.Position + 1;
            this.AddDiagnostic(
                221,
                "X.208 MACRO syntax is retired and is not accepted as an ASN.1 type.",
                first);
            return this.CreateResult(false, null, start);
        }

        if (first.Text == "EXTERNAL")
        {
            return this.ParseSingleTokenType(start, AsnTypeKind.External);
        }

        if (first.Text == "EMBEDDED")
        {
            return this.ParseTwoWordType(start, "PDV", AsnTypeKind.EmbeddedPdv);
        }

        if (first.Text == "CHOICE")
        {
            return this.ParseChoiceType(start);
        }

        if (first.Text == "SEQUENCE")
        {
            return this.ParseSequenceOrSetType(start, true);
        }

        if (first.Text == "SET")
        {
            return this.ParseSequenceOrSetType(start, false);
        }

        AsnTypeKind simpleKind = this.GetSimpleTypeKind(first.Text);
        if (simpleKind != AsnTypeKind.Unknown)
        {
            this.Position = this.Position + 1;
            ParsedTypeSyntax syntax = new ParsedTypeSyntax
            {
                Type = new AsnType
                {
                    Kind = simpleKind
                },
                SourceTypeName = first.Text,
                Span = first.Span
            };
            return this.CreateResult(true, syntax, start);
        }

        return this.ParseDefinedType(start);
    }

    internal TypeParseResult ParseLegacyAnyType(int start)
    {
        AsnToken first = this.ReadRequiredToken();
        bool hasDefinedBy = false;
        string definedByReference = "";
        AsnToken last = first;
        AsnToken? defined = this.PeekToken();
        if (defined != null && defined.Text == "DEFINED")
        {
            hasDefinedBy = true;
            this.Position = this.Position + 1;
            last = defined;
            AsnToken? by = this.PeekToken();
            if (by == null || by.Text != "BY")
            {
                this.AddDiagnostic(223, "Expected BY after ANY DEFINED.", by);
                return this.CreateResult(false, null, start);
            }

            this.Position = this.Position + 1;
            last = by;
            AsnToken? reference = this.PeekToken();
            if (!this.IsComponentName(reference))
            {
                this.AddDiagnostic(224, "ANY DEFINED BY requires a component identifier.", reference);
                return this.CreateResult(false, null, start);
            }

            this.Position = this.Position + 1;
            last = reference == null ? last : reference;
            definedByReference = last.Text;
        }

        if (!this.Diagnostics.EnableLegacyAny)
        {
            this.AddDiagnostic(
                222,
                "Retired ANY syntax requires the legacy ANY compatibility option.",
                first);
            return this.CreateResult(false, null, start);
        }

        return this.CreateResult(
            true,
            new ParsedTypeSyntax
            {
                Type = new AsnType
                {
                    Kind = AsnTypeKind.OpenType
                },
                SourceTypeName = hasDefinedBy
                    ? "ANY DEFINED BY " + definedByReference
                    : "ANY",
                DefinedByReference = definedByReference,
                LegacyAnySyntax = true,
                HasDefinedByReference = hasDefinedBy,
                Span = this.CreateSpan(first, last)
            },
            start);
    }

    internal TypeParseResult ParseTypeFromObject(int start)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        InformationFromObjectsParseResult parsed = new InformationFromObjectsParser(
            remaining,
            this.Diagnostics,
            false).ParseInformationFromObjectsSyntax(AsnAssignmentKind.Type);
        if (!parsed.Success || parsed.Syntax == null)
        {
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + parsed.ConsumedTokenCount;
        string name = this.JoinInformationTokens(parsed.Syntax.SourceTokens);
        return this.CreateResult(
            true,
            new ParsedTypeSyntax
            {
                Type = new AsnType { Kind = AsnTypeKind.Defined, Name = name },
                InformationFromObjects = parsed.Syntax,
                SourceTypeName = name,
                Span = parsed.Syntax.Span
            },
            start);
    }

    internal string JoinInformationTokens(AsnToken[] tokens)
    {
        string result = "";
        int index = 0;
        while (index < tokens.Length)
        {
            result = result + tokens[index].Text;
            index = index + 1;
        }

        return result;
    }

    internal TypeParseResult ParseParameterizedTypeReference(int start)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ParameterizedReferenceParseResult parsed = new ParameterizedParser(
            remaining,
            this.Diagnostics,
            false).ParseParameterizedReferenceSyntax(AsnParameterKind.Type, null);
        if (!parsed.Success || parsed.Syntax == null)
        {
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + parsed.ConsumedTokenCount;
        string name = parsed.Syntax.ReferenceModule.Length == 0
            ? parsed.Syntax.ReferenceName
            : parsed.Syntax.ReferenceModule + "." + parsed.Syntax.ReferenceName;
        return this.CreateResult(
            true,
            new ParsedTypeSyntax
            {
                Type = new AsnType { Kind = AsnTypeKind.Defined, Name = name },
                ParameterizedReference = parsed.Syntax,
                ReferenceModule = parsed.Syntax.ReferenceModule,
                ReferenceName = parsed.Syntax.ReferenceName,
                SourceTypeName = this.JoinInformationTokens(parsed.Syntax.SourceTokens),
                Span = parsed.Syntax.Span
            },
            start);
    }

    internal TypeParseResult ParseIntegerType(int start)
    {
        AsnToken first = this.ReadRequiredToken();
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = new AsnType
            {
                Kind = AsnTypeKind.Integer
            },
            SourceTypeName = first.Text,
            Span = first.Span
        };
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            return this.CreateResult(true, syntax, start);
        }

        NamedListParseResult list = this.ParseNamedList(false, false, true);
        if (!list.Success)
        {
            return this.CreateResult(false, syntax, start);
        }

        syntax.RootNamedValues = list.RootValues;
        syntax.RootNamedValueSpans = list.RootValueSpans;
        syntax.Type.NamedValues = list.RootValues;
        syntax.Span = this.CreateSpan(first, list.LastToken);
        return this.CreateResult(true, syntax, start);
    }

    internal TypeParseResult ParseEnumeratedType(int start)
    {
        AsnToken first = this.ReadRequiredToken();
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = new AsnType
            {
                Kind = AsnTypeKind.Enumerated
            },
            SourceTypeName = first.Text,
            Span = first.Span
        };
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            this.AddDiagnostic(15, "ENUMERATED requires an enumeration list.", this.PeekToken());
            return this.CreateResult(false, syntax, start);
        }

        NamedListParseResult list = this.ParseNamedList(true, true, true);
        if (!list.Success)
        {
            return this.CreateResult(false, syntax, start);
        }

        syntax.RootNamedValues = list.RootValues;
        syntax.ExtensionNamedValues = list.ExtensionValues;
        syntax.RootNamedValueSpans = list.RootValueSpans;
        syntax.ExtensionNamedValueSpans = list.ExtensionValueSpans;
        syntax.ExtensionExceptionTokens = list.ExtensionExceptionTokens;
        syntax.Type.NamedValues = this.CombineNamedValues(list.RootValues, list.ExtensionValues);
        syntax.Type.Extensible = list.ExtensionValues.Length > 0
            || list.ExtensionExceptionTokens.Length > 0
            || this.ContainsExtensionMarker(start, this.Position);
        syntax.Span = this.CreateSpan(first, list.LastToken);
        return this.CreateResult(true, syntax, start);
    }

    internal TypeParseResult ParseBitStringType(int start)
    {
        AsnToken first = this.ReadRequiredToken();
        AsnToken? second = this.PeekToken();
        if (second == null || second.Text != "STRING")
        {
            this.AddDiagnostic(16, "Expected STRING after BIT.", second);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = new AsnType
            {
                Kind = AsnTypeKind.BitString
            },
            SourceTypeName = first.Text + " " + second.Text,
            Span = this.CreateSpan(first, second)
        };
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            return this.CreateResult(true, syntax, start);
        }

        NamedListParseResult list = this.ParseNamedList(false, false, false);
        if (!list.Success)
        {
            return this.CreateResult(false, syntax, start);
        }

        syntax.RootNamedValues = list.RootValues;
        syntax.RootNamedValueSpans = list.RootValueSpans;
        syntax.Type.NamedValues = list.RootValues;
        syntax.Span = this.CreateSpan(first, list.LastToken);
        return this.CreateResult(true, syntax, start);
    }

    internal TypeParseResult ParseTwoWordType(int start, string secondText, AsnTypeKind kind)
    {
        AsnToken first = this.ReadRequiredToken();
        AsnToken? second = this.PeekToken();
        if (second == null || second.Text != secondText)
        {
            this.AddDiagnostic(17, "Expected " + secondText + " after " + first.Text + ".", second);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        return this.CreateResult(
            true,
            new ParsedTypeSyntax
            {
                Type = new AsnType
                {
                    Kind = kind
                },
                SourceTypeName = first.Text + " " + second.Text,
                Span = this.CreateSpan(first, second)
            },
            start);
    }

    internal TypeParseResult ParseSingleTokenType(int start, AsnTypeKind kind)
    {
        AsnToken token = this.ReadRequiredToken();
        return this.CreateResult(
            true,
            new ParsedTypeSyntax
            {
                Type = new AsnType
                {
                    Kind = kind
                },
                SourceTypeName = token.Text,
                Span = token.Span
            },
            start);
    }

    internal TypeParseResult ParsePrefixedType(int start)
    {
        if (this.HasExplicitTagEncodingReference() || this.MatchesTagPrefixShape())
        {
            return this.ParseTaggedType(start);
        }

        return this.ParseEncodingPrefixedType(start);
    }

    internal bool HasExplicitTagEncodingReference()
    {
        AsnToken? reference = this.PeekTokenAt(1);
        return reference != null
            && reference.Text == "TAG"
            && this.IsKind(this.PeekTokenAt(2), AsnTokenKind.Colon);
    }

    internal bool MatchesTagPrefixShape()
    {
        int offset = 1;
        AsnToken? reference = this.PeekTokenAt(offset);
        if (reference != null && this.IsKind(this.PeekTokenAt(offset + 1), AsnTokenKind.Colon))
        {
            if (reference.Text != "TAG")
            {
                return false;
            }

            offset = offset + 2;
        }

        AsnToken? current = this.PeekTokenAt(offset);
        if (this.IsTagClassToken(current))
        {
            return true;
        }

        if (current == null)
        {
            return false;
        }

        if (current.Kind == AsnTokenKind.IntegerLiteral
            || current.Kind == AsnTokenKind.Identifier
            || current.Kind == AsnTokenKind.ValueReference)
        {
            return true;
        }

        if (this.IsTypeReference(current)
            && this.IsKind(this.PeekTokenAt(offset + 1), AsnTokenKind.Dot))
        {
            AsnToken? value = this.PeekTokenAt(offset + 2);
            return this.IsComponentName(value);
        }

        return false;
    }

    internal TypeParseResult ParseTaggedType(int start)
    {
        AsnToken left = this.ReadRequiredToken();
        string encodingReference = "TAG";
        AsnToken? current = this.PeekToken();
        if (current != null && this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Colon))
        {
            encodingReference = current.Text;
            this.Position = this.Position + 2;
            current = this.PeekToken();
        }

        AsnTagClass tagClass = AsnTagClass.ContextSpecific;
        if (this.IsTagClassToken(current))
        {
            tagClass = this.GetTagClass(current == null ? "" : current.Text);
            this.Position = this.Position + 1;
            current = this.PeekToken();
        }

        bool hasNumber = false;
        System.Numerics.BigInteger number = default(System.Numerics.BigInteger);
        string numberReference = "";
        if (current != null && current.Kind == AsnTokenKind.IntegerLiteral)
        {
            hasNumber = true;
            number = System.Numerics.BigInteger.Parse(
                current.Text,
                System.Globalization.CultureInfo.InvariantCulture);
            this.Position = this.Position + 1;
        }
        else
        {
            numberReference = this.ParseDefinedValueText();
            if (numberReference.Length == 0)
            {
                this.AddDiagnostic(51, "A tag requires a non-negative number or defined value.", current);
                return this.CreateResult(false, null, start);
            }
        }

        AsnToken? right = this.PeekToken();
        if (!this.IsKind(right, AsnTokenKind.RightBracket))
        {
            this.AddDiagnostic(52, "A tag is missing its closing bracket.", right);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        AsnTaggingMode mode = AsnTaggingMode.Unspecified;
        current = this.PeekToken();
        if (current != null && current.Text == "IMPLICIT")
        {
            mode = AsnTaggingMode.Implicit;
            this.Position = this.Position + 1;
        }
        else if (current != null && current.Text == "EXPLICIT")
        {
            mode = AsnTaggingMode.Explicit;
            this.Position = this.Position + 1;
        }

        TypeParseResult inner = this.ParseTypeSyntax();
        if (!inner.Success || inner.Syntax == null)
        {
            this.AddDiagnostic(53, "A tagged type requires an inner type.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = new AsnType
            {
                Kind = AsnTypeKind.Tagged,
                Tag = new AsnTag
                {
                    Class = tagClass,
                    Mode = mode,
                    Number = number
                },
                ElementType = inner.Syntax.Type
            },
            InnerTypeSyntax = inner.Syntax,
            SourceTypeName = "TAGGED",
            EncodingReference = encodingReference,
            HasNumericTagNumber = hasNumber,
            TagNumberReference = numberReference,
            Span = this.CreateSpan(left, this.PeekTokenAt(-1))
        };
        return this.CreateResult(true, syntax, start);
    }

    internal TypeParseResult ParseEncodingPrefixedType(int start)
    {
        AsnToken left = this.ReadRequiredToken();
        string encodingReference = "";
        AsnToken? current = this.PeekToken();
        if (current != null && this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Colon))
        {
            encodingReference = current.Text;
            this.Position = this.Position + 2;
            current = this.PeekToken();
        }

        System.Collections.Generic.List<AsnToken> instructionTokens =
            new System.Collections.Generic.List<AsnToken>();
        while (current != null && current.Kind != AsnTokenKind.RightBracket)
        {
            if (current.Kind == AsnTokenKind.LeftBracket)
            {
                this.AddDiagnostic(54, "An encoding instruction cannot contain a bracket.", current);
                return this.CreateResult(false, null, start);
            }

            instructionTokens.Add(current);
            this.Position = this.Position + 1;
            current = this.PeekToken();
        }

        if (current == null)
        {
            this.AddDiagnostic(55, "An encoding prefix is missing its closing bracket.", left);
            return this.CreateResult(false, null, start);
        }

        if (instructionTokens.Count == 0)
        {
            this.AddDiagnostic(56, "An encoding prefix requires an instruction.", current);
            return this.CreateResult(false, null, start);
        }

        AsnToken right = this.ReadRequiredToken();
        TypeParseResult inner = this.ParseTypeSyntax();
        if (!inner.Success || inner.Syntax == null)
        {
            this.AddDiagnostic(57, "An encoding-prefixed type requires an inner type.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        AsnToken[] tokens = instructionTokens.ToArray();
        AsnEncodingInstruction instruction;
        ParsedEncodingInstructionSyntax[] encodingInstructionSyntax = [];
        if (encodingReference == "XER" || encodingReference.Length == 0)
        {
            XerEncodingInstructionParseResult parsed =
                new XerEncodingInstructionParser(tokens).ParsePrefix();
            if (parsed.Success && parsed.Instructions.Length > 0)
            {
                encodingInstructionSyntax = parsed.Instructions;
                instruction = parsed.Instructions[0].Instruction;
            }
            else if (encodingReference == "XER")
            {
                this.AddDiagnostic(58, parsed.ErrorMessage, parsed.ErrorToken);
                return this.CreateResult(false, null, start);
            }
            else
            {
                instruction = new AsnEncodingInstruction
                {
                    Name = tokens[0].Text,
                    Kind = this.GetEncodingInstructionKind(encodingReference, tokens[0].Text),
                    Span = this.CreateSpan(tokens[0], tokens[tokens.Length - 1])
                };
            }
        }
        else if (encodingReference == "PER")
        {
            PerEncodingInstructionParseResult parsed =
                new PerEncodingInstructionParser(tokens).ParsePrefix();
            if (!parsed.Success || parsed.Instructions.Length == 0)
            {
                this.AddDiagnostic(59, parsed.ErrorMessage, parsed.ErrorToken);
                return this.CreateResult(false, null, start);
            }

            encodingInstructionSyntax = parsed.Instructions;
            instruction = parsed.Instructions[0].Instruction;
        }
        else
        {
            instruction = new AsnEncodingInstruction
            {
                Name = tokens[0].Text,
                Kind = this.GetEncodingInstructionKind(encodingReference, tokens[0].Text),
                Span = this.CreateSpan(tokens[0], tokens[tokens.Length - 1])
            };
        }

        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = new AsnType
            {
                Kind = AsnTypeKind.Prefixed,
                ElementType = inner.Syntax.Type
            },
            InnerTypeSyntax = inner.Syntax,
            EncodingInstructions = [instruction],
            EncodingInstructionSyntax = encodingInstructionSyntax,
            EncodingInstructionTokens = tokens,
            EncodingReference = encodingReference,
            SourceTypeName = "PREFIXED",
            Span = this.CreateSpan(left, this.PeekTokenAt(-1))
        };
        return this.CreateResult(true, syntax, start);
    }

    internal TypeParseResult ParseSelectionType(int start)
    {
        AsnToken identifier = this.ReadRequiredToken();
        this.Position = this.Position + 1;
        TypeParseResult selected = this.ParseTypeSyntax();
        if (!selected.Success || selected.Syntax == null)
        {
            this.AddDiagnostic(58, "A selection type requires a type after '<'.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        return this.CreateResult(
            true,
            new ParsedTypeSyntax
            {
                Type = new AsnType
                {
                    Kind = AsnTypeKind.Selection,
                    Name = identifier.Text,
                    ElementType = selected.Syntax.Type
                },
                InnerTypeSyntax = selected.Syntax,
                SelectionIdentifier = identifier.Text,
                SourceTypeName = identifier.Text + " <",
                Span = this.CreateSpan(identifier, this.PeekTokenAt(-1))
            },
            start);
    }

    internal TypeParseResult ParseInstanceOfType(int start)
    {
        AsnToken first = this.ReadRequiredToken();
        AsnToken? ofToken = this.PeekToken();
        if (ofToken == null || ofToken.Text != "OF")
        {
            this.AddDiagnostic(59, "Expected OF after INSTANCE.", ofToken);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        AsnToken? reference = this.PeekToken();
        if (!this.IsObjectClassReference(reference))
        {
            this.AddDiagnostic(60, "INSTANCE OF requires a defined object class.", reference);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        string moduleName = "";
        AsnToken last = reference == null ? first : reference;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.Dot))
        {
            AsnToken? classReference = this.PeekTokenAt(1);
            if (!this.IsObjectClassReference(classReference))
            {
                this.AddDiagnostic(61, "Expected an object class reference after '.'.", classReference);
                return this.CreateResult(false, null, start);
            }

            moduleName = last.Text;
            this.Position = this.Position + 2;
            last = classReference == null ? last : classReference;
        }

        string name = moduleName.Length == 0 ? last.Text : moduleName + "." + last.Text;
        return this.CreateResult(
            true,
            new ParsedTypeSyntax
            {
                Type = new AsnType
                {
                    Kind = AsnTypeKind.InstanceOf,
                    Name = name
                },
                ReferenceModule = moduleName,
                ReferenceName = last.Text,
                SourceTypeName = "INSTANCE OF",
                Span = this.CreateSpan(first, last)
            },
            start);
    }

    internal bool IsObjectClassReference(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.ObjectClassReference
                || token.Kind == AsnTokenKind.TypeReference
                || token.Kind == AsnTokenKind.ModuleReference
                || token.Text == "TYPE-IDENTIFIER"
                || token.Text == "ABSTRACT-SYNTAX");
    }

    internal bool IsTagClassToken(AsnToken? token)
    {
        return token != null
            && (token.Text == "UNIVERSAL"
                || token.Text == "APPLICATION"
                || token.Text == "PRIVATE");
    }

    internal AsnTagClass GetTagClass(string text)
    {
        switch (text)
        {
            case "UNIVERSAL":
                return AsnTagClass.Universal;
            case "APPLICATION":
                return AsnTagClass.Application;
            case "PRIVATE":
                return AsnTagClass.Private;
            default:
                return AsnTagClass.ContextSpecific;
        }
    }

    internal AsnInstructionKind GetEncodingInstructionKind(string encodingReference, string name)
    {
        if (encodingReference == "PER")
        {
            return AsnInstructionKind.PerApplication;
        }

        if (encodingReference == "ECN")
        {
            return AsnInstructionKind.Ecn;
        }

        switch (name)
        {
            case "ANY-ATTRIBUTES":
                return AsnInstructionKind.AnyAttributes;
            case "ANY-ELEMENT":
                return AsnInstructionKind.AnyElement;
            case "ATTRIBUTE":
                return AsnInstructionKind.Attribute;
            case "BASE64":
                return AsnInstructionKind.Base64;
            case "DECIMAL":
                return AsnInstructionKind.Decimal;
            case "DEFAULT-FOR-EMPTY":
                return AsnInstructionKind.DefaultForEmpty;
            case "ELEMENT":
                return AsnInstructionKind.Element;
            case "EMBED-VALUES":
                return AsnInstructionKind.EmbedValues;
            case "GLOBAL-DEFAULTS":
                return AsnInstructionKind.GlobalDefaults;
            case "LIST":
                return AsnInstructionKind.List;
            case "NAME":
                return AsnInstructionKind.Name;
            case "NAMESPACE":
                return AsnInstructionKind.Namespace;
            case "PI-OR-COMMENT":
                return AsnInstructionKind.PiOrComment;
            case "TEXT":
                return AsnInstructionKind.Text;
            case "UNTAGGED":
                return AsnInstructionKind.Untagged;
            case "USE-NIL":
                return AsnInstructionKind.UseNil;
            case "USE-NUMBER":
                return AsnInstructionKind.UseNumber;
            case "USE-ORDER":
                return AsnInstructionKind.UseOrder;
            case "USE-QNAME":
                return AsnInstructionKind.UseQName;
            case "USE-TYPE":
                return AsnInstructionKind.UseType;
            case "USE-UNION":
                return AsnInstructionKind.UseUnion;
            case "WHITESPACE":
                return AsnInstructionKind.Whitespace;
            default:
                return AsnInstructionKind.Unknown;
        }
    }

    internal TypeParseResult ParseChoiceType(int start)
    {
        AsnToken first = this.ReadRequiredToken();
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            this.AddDiagnostic(30, "CHOICE requires an alternative type list.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        return this.ParseStructuredTypeBody(start, first, AsnTypeKind.Choice, true);
    }

    internal TypeParseResult ParseSequenceOrSetType(int start, bool sequence)
    {
        AsnToken first = this.ReadRequiredToken();
        AsnTypeKind structuredKind = sequence ? AsnTypeKind.Sequence : AsnTypeKind.Set;
        AsnTypeKind collectionKind = sequence ? AsnTypeKind.SequenceOf : AsnTypeKind.SetOf;
        AsnToken? current = this.PeekToken();
        if (current != null && current.Text == "OF")
        {
            this.Position = this.Position + 1;
            return this.ParseCollectionOfType(start, first, collectionKind);
        }

        if (this.IsKind(current, AsnTokenKind.LeftParenthesis)
            || this.IsText(current, "SIZE"))
        {
            AsnType collectionType = new AsnType
            {
                Kind = collectionKind
            };
            AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
            ConstraintParser constraintParser = new ConstraintParser(
                remaining,
                this.Diagnostics,
                this.ReportDiagnostics);
            ConstraintParseResult constraint = this.IsText(current, "SIZE")
                ? constraintParser.ParseSizeConstraintSyntax(collectionType)
                : constraintParser.ParseConstraintSyntax(collectionType);
            this.Position = this.Position + constraint.ConsumedTokenCount;
            if (!constraint.Success || constraint.Syntax == null)
            {
                return this.CreateResult(false, null, start);
            }

            if (!this.IsText(this.PeekToken(), "OF"))
            {
                this.AddDiagnostic(124, "Expected OF after the collection constraint.", this.PeekToken());
                return this.CreateResult(false, null, start);
            }

            this.Position = this.Position + 1;
            TypeParseResult constrainedCollection = this.ParseCollectionOfType(
                start,
                first,
                collectionKind);
            if (constrainedCollection.Success && constrainedCollection.Syntax != null)
            {
                this.AppendConstraintSyntax(constrainedCollection.Syntax, constraint.Syntax);
            }

            return constrainedCollection;
        }

        if (!this.IsKind(current, AsnTokenKind.LeftBrace))
        {
            this.AddDiagnostic(
                31,
                "Expected '{' or OF after " + first.Text + ".",
                current);
            return this.CreateResult(false, null, start);
        }

        return this.ParseStructuredTypeBody(start, first, structuredKind, false);
    }

    internal TypeParseResult ParseCollectionOfType(int start, AsnToken first, AsnTypeKind kind)
    {
        string elementName = "";
        AsnToken? current = this.PeekToken();
        if (this.IsComponentName(current))
        {
            elementName = current == null ? "" : current.Text;
            this.Position = this.Position + 1;
        }

        int elementStart = this.Position;
        TypeParseResult element = this.ParseTypeSyntax();
        if (!element.Success || element.Syntax == null || this.Position == elementStart)
        {
            this.AddDiagnostic(32, first.Text + " OF requires an element type.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        AsnToken? last = this.PeekTokenAt(-1);
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = new AsnType
            {
                Kind = kind,
                ElementType = element.Syntax.Type
            },
            InnerTypeSyntax = element.Syntax,
            SourceTypeName = first.Text + " OF",
            ElementName = elementName,
            Span = this.CreateSpan(first, last)
        };
        return this.CreateResult(true, syntax, start);
    }

    internal TypeParseResult ParseStructuredTypeBody(
        int start,
        AsnToken first,
        AsnTypeKind kind,
        bool choice)
    {
        this.Position = this.Position + 1;
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = new AsnType
            {
                Kind = kind
            },
            SourceTypeName = first.Text
        };
        if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            AsnToken right = this.ReadRequiredToken();
            syntax.Span = this.CreateSpan(first, right);
            if (choice)
            {
                this.AddDiagnostic(33, "A CHOICE alternative list cannot be empty.", right);
                return this.CreateResult(false, syntax, start);
            }

            return this.CreateResult(true, syntax, start);
        }

        if (!this.ParseComponentTypeList(syntax, choice))
        {
            syntax.Type.Components = this.GetNamedComponents(syntax.ComponentSyntax);
            syntax.Span = this.CreateSpan(first, this.PeekTokenAt(-1));
            return this.CreateResult(false, syntax, start);
        }

        syntax.Type.Components = this.GetNamedComponents(syntax.ComponentSyntax);
        syntax.Span = this.CreateSpan(first, this.PeekTokenAt(-1));
        return this.CreateResult(true, syntax, start);
    }

    internal bool ParseComponentTypeList(ParsedTypeSyntax syntax, bool choice)
    {
        System.Collections.Generic.List<ParsedComponentSyntax> components =
            new System.Collections.Generic.List<ParsedComponentSyntax>();
        System.Collections.Generic.List<ParsedRecoverySyntax> recoveries =
            new System.Collections.Generic.List<ParsedRecoverySyntax>();
        bool success = true;
        bool extensionStarted = false;
        bool extensionEnded = false;
        int extensionGroupIndex = 0;
        AsnToken? current = this.PeekToken();
        while (current != null && current.Kind != AsnTokenKind.RightBrace)
        {
            int itemStart = this.Position;
            bool itemSuccess = true;
            if (current.Kind == AsnTokenKind.Ellipsis)
            {
                if (!extensionStarted)
                {
                    extensionStarted = true;
                    syntax.HasExtensionMarker = true;
                    syntax.Type.Extensible = true;
                    this.Position = this.Position + 1;
                    System.Collections.Generic.List<AsnToken> exception =
                        new System.Collections.Generic.List<AsnToken>();
                    this.CollectExtensionException(exception);
                    syntax.ExtensionExceptionTokens = exception.ToArray();
                }
                else if (!extensionEnded)
                {
                    extensionEnded = true;
                    syntax.HasExtensionEndMarker = true;
                    this.Position = this.Position + 1;
                }
                else
                {
                    this.AddDiagnostic(34, "Only two extension markers are valid in a component list.", current);
                    this.Position = this.Position + 1;
                    itemSuccess = false;
                }
            }
            else if (current.Kind == AsnTokenKind.LeftVersionBracket)
            {
                if (!extensionStarted || extensionEnded)
                {
                    this.AddDiagnostic(35, "An extension group must occur among extension additions.", current);
                    itemSuccess = false;
                }
                else if (!this.ParseExtensionAdditionGroup(components, choice, extensionGroupIndex))
                {
                    itemSuccess = false;
                }
                else
                {
                    extensionGroupIndex = extensionGroupIndex + 1;
                }
            }
            else
            {
                if (choice && extensionEnded)
                {
                    this.AddDiagnostic(36, "A CHOICE cannot contain alternatives after its final extension marker.", current);
                    itemSuccess = false;
                }
                else
                {
                    ParsedComponentSyntax? component = this.ParseComponentTypeSyntax(
                        choice,
                        extensionStarted && !extensionEnded,
                        extensionEnded);
                    if (component == null)
                    {
                        itemSuccess = false;
                    }
                    else
                    {
                        components.Add(component);
                    }

                }
            }

            if (!itemSuccess)
            {
                success = false;
                this.RecoverComponentSyntax(itemStart, recoveries);
            }

            current = this.PeekToken();
            if (this.IsKind(current, AsnTokenKind.Comma))
            {
                this.Position = this.Position + 1;
                current = this.PeekToken();
                if (this.IsKind(current, AsnTokenKind.RightBrace))
                {
                    this.AddDiagnostic(37, "A component list cannot have a trailing comma.", current);
                    success = false;
                    break;
                }

                continue;
            }

            if (current == null)
            {
                this.AddDiagnostic(39, "The component list is missing its closing brace.", null);
                success = false;
                break;
            }

            if (!this.IsKind(current, AsnTokenKind.RightBrace))
            {
                this.AddDiagnostic(38, "Expected ',' or '}' in a component list.", current);
                success = false;
                this.RecoverComponentSyntax(this.Position, recoveries);
                current = this.PeekToken();
                if (this.IsKind(current, AsnTokenKind.Comma))
                {
                    this.Position = this.Position + 1;
                    current = this.PeekToken();
                    continue;
                }

                if (!this.IsKind(current, AsnTokenKind.RightBrace))
                {
                    break;
                }
            }
        }

        if (current == null)
        {
            this.AddDiagnostic(39, "The component list is missing its closing brace.", null);
            success = false;
        }
        else
        {
            this.Position = this.Position + 1;
        }

        syntax.ComponentSyntax = components.ToArray();
        syntax.Recoveries = recoveries.ToArray();
        return success;
    }

    internal void RecoverComponentSyntax(
        int recoveryStart,
        System.Collections.Generic.List<ParsedRecoverySyntax> recoveries)
    {
        int depth = 0;
        while (this.PeekToken() != null)
        {
            AsnToken current = this.PeekToken() ?? new AsnToken();
            if (depth == 0
                && (current.Kind == AsnTokenKind.Comma
                    || current.Kind == AsnTokenKind.RightBrace
                    || current.Kind == AsnTokenKind.Ellipsis))
            {
                break;
            }

            if (this.IsOpeningDelimiter(current.Kind))
            {
                depth = depth + 1;
            }
            else if (this.IsClosingDelimiter(current.Kind) && depth > 0)
            {
                depth = depth - 1;
            }

            this.Position = this.Position + 1;
        }

        AsnToken[] skipped = this.CopyTokenRange(recoveryStart, this.Position);
        recoveries.Add(new ParsedRecoverySyntax
        {
            Boundary = "component",
            SkippedTokens = skipped,
            Span = skipped.Length == 0
                ? null
                : this.CreateSpan(skipped[0], skipped[skipped.Length - 1])
        });
    }

    internal bool ParseExtensionAdditionGroup(
        System.Collections.Generic.List<ParsedComponentSyntax> components,
        bool choice,
        int groupIndex)
    {
        AsnToken left = this.ReadRequiredToken();
        bool hasVersion = false;
        System.Numerics.BigInteger version = default(System.Numerics.BigInteger);
        AsnToken? current = this.PeekToken();
        if (current != null
            && current.Kind == AsnTokenKind.IntegerLiteral
            && this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Colon))
        {
            hasVersion = true;
            version = System.Numerics.BigInteger.Parse(
                current.Text,
                System.Globalization.CultureInfo.InvariantCulture);
            this.Position = this.Position + 2;
            current = this.PeekToken();
        }

        if (this.IsKind(current, AsnTokenKind.RightVersionBracket))
        {
            this.AddDiagnostic(40, "An extension addition group cannot be empty.", current);
            return false;
        }

        while (current != null && current.Kind != AsnTokenKind.RightVersionBracket)
        {
            ParsedComponentSyntax? component = this.ParseComponentTypeSyntax(choice, true, false);
            if (component == null)
            {
                return false;
            }

            component.ExtensionGroupIndex = groupIndex;
            component.HasVersionNumber = hasVersion;
            component.VersionNumber = version;
            components.Add(component);
            current = this.PeekToken();
            if (this.IsKind(current, AsnTokenKind.Comma))
            {
                this.Position = this.Position + 1;
                current = this.PeekToken();
                if (this.IsKind(current, AsnTokenKind.RightVersionBracket))
                {
                    this.AddDiagnostic(41, "An extension addition group cannot have a trailing comma.", current);
                    return false;
                }

                continue;
            }

            if (!this.IsKind(current, AsnTokenKind.RightVersionBracket))
            {
                this.AddDiagnostic(42, "Expected ',' or ']]' in an extension addition group.", current);
                return false;
            }
        }

        if (current == null)
        {
            this.AddDiagnostic(43, "The extension addition group is missing its closing brackets.", left);
            return false;
        }

        this.Position = this.Position + 1;
        return true;
    }

    internal ParsedComponentSyntax? ParseComponentTypeSyntax(
        bool choice,
        bool extension,
        bool afterExtensionEnd)
    {
        AsnToken? first = this.PeekToken();
        if (first != null && first.Text == "COMPONENTS")
        {
            if (choice)
            {
                this.AddDiagnostic(44, "COMPONENTS OF is not valid in a CHOICE.", first);
                return null;
            }

            this.Position = this.Position + 1;
            AsnToken? ofToken = this.PeekToken();
            if (ofToken == null || ofToken.Text != "OF")
            {
                this.AddDiagnostic(45, "Expected OF after COMPONENTS.", ofToken);
                return null;
            }

            this.Position = this.Position + 1;
            TypeParseResult included = this.ParseTypeSyntax();
            if (!included.Success || included.Syntax == null)
            {
                return null;
            }

            return new ParsedComponentSyntax
            {
                TypeSyntax = included.Syntax,
                ComponentsOf = true,
                Extension = extension,
                AfterExtensionEnd = afterExtensionEnd,
                Span = this.CreateSpan(first, this.PeekTokenAt(-1))
            };
        }

        if (!this.IsComponentName(first))
        {
            this.AddDiagnostic(46, "Expected a named component type.", first);
            return null;
        }

        AsnToken name = this.ReadRequiredToken();
        TypeParseResult parsedType = this.ParseTypeSyntax();
        if (!parsedType.Success || parsedType.Syntax == null)
        {
            return null;
        }

        AsnComponent component = new AsnComponent
        {
            Name = name.Text,
            Type = parsedType.Syntax.Type,
            Extension = extension
        };
        AsnToken[] defaultTokens = [];
        ParsedValueSyntax? defaultValueSyntax = null;
        AsnToken? current = this.PeekToken();
        if (current != null && current.Text == "OPTIONAL")
        {
            if (choice)
            {
                this.AddDiagnostic(47, "CHOICE alternatives cannot be OPTIONAL.", current);
                return null;
            }

            component.Optional = true;
            this.Position = this.Position + 1;
        }
        else if (current != null && current.Text == "DEFAULT")
        {
            if (choice)
            {
                this.AddDiagnostic(48, "CHOICE alternatives cannot have DEFAULT values.", current);
                return null;
            }

            this.Position = this.Position + 1;
            defaultTokens = this.ParseDefaultValueTokens();
            if (defaultTokens.Length == 0)
            {
                return null;
            }

            ValueParseResult parsedDefault = new ValueParser(
                defaultTokens,
                this.Diagnostics,
                this.ReportDiagnostics).ParseValueSyntax(component.Type);
            if (!parsedDefault.Success
                || parsedDefault.Syntax == null
                || parsedDefault.ConsumedTokenCount != defaultTokens.Length)
            {
                this.AddDiagnostic(62, "The DEFAULT value is not valid for its component type.", current);
                return null;
            }

            component.HasDefaultValue = true;
            component.DefaultValue = parsedDefault.Syntax.Value;
            defaultValueSyntax = parsedDefault.Syntax;
            if (component.DefaultValue.TextValue.Length == 0)
            {
                component.DefaultValue.TextValue = this.JoinTokenText(defaultTokens);
            }
        }

        return new ParsedComponentSyntax
        {
            Component = component,
            TypeSyntax = parsedType.Syntax,
            DefaultValueSyntax = defaultValueSyntax,
            Extension = extension,
            AfterExtensionEnd = afterExtensionEnd,
            DefaultValueTokens = defaultTokens,
            Span = this.CreateSpan(name, this.PeekTokenAt(-1))
        };
    }

    internal AsnToken[] ParseDefaultValueTokens()
    {
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        int depth = 0;
        AsnToken? current = this.PeekToken();
        while (current != null)
        {
            bool outerDelimiter = depth == 0
                && (current.Kind == AsnTokenKind.Comma
                    || current.Kind == AsnTokenKind.RightBrace
                    || current.Kind == AsnTokenKind.RightVersionBracket);
            if (outerDelimiter)
            {
                break;
            }

            if (this.IsOpeningDelimiter(current.Kind))
            {
                depth = depth + 1;
            }
            else if (this.IsClosingDelimiter(current.Kind))
            {
                if (depth == 0)
                {
                    break;
                }

                depth = depth - 1;
            }

            tokens.Add(current);
            this.Position = this.Position + 1;
            current = this.PeekToken();
        }

        if (tokens.Count == 0)
        {
            this.AddDiagnostic(49, "DEFAULT requires a value.", current);
            return [];
        }

        if (depth != 0)
        {
            this.AddDiagnostic(50, "The DEFAULT value contains an unclosed delimiter.", current);
            return [];
        }

        return tokens.ToArray();
    }

    internal string JoinTokenText(AsnToken[] tokens)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < tokens.Length)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append(tokens[index].Text);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal AsnComponent[] GetNamedComponents(ParsedComponentSyntax[] syntax)
    {
        System.Collections.Generic.List<AsnComponent> components =
            new System.Collections.Generic.List<AsnComponent>();
        int index = 0;
        while (index < syntax.Length)
        {
            AsnComponent? component = syntax[index].Component;
            if (component != null)
            {
                components.Add(component);
            }

            index = index + 1;
        }

        return components.ToArray();
    }

    internal bool IsComponentName(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.Identifier || token.Kind == AsnTokenKind.ValueReference);
    }

    internal bool IsOpeningDelimiter(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.LeftBrace
            || kind == AsnTokenKind.LeftParenthesis
            || kind == AsnTokenKind.LeftBracket
            || kind == AsnTokenKind.LeftVersionBracket;
    }

    internal bool IsClosingDelimiter(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.RightBrace
            || kind == AsnTokenKind.RightParenthesis
            || kind == AsnTokenKind.RightBracket
            || kind == AsnTokenKind.RightVersionBracket;
    }

    internal TypeParseResult ParseDefinedType(int start)
    {
        AsnToken? first = this.PeekToken();
        if (first == null || !this.IsTypeReference(first))
        {
            this.AddDiagnostic(14, "Expected an ASN.1 type.", first);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        string moduleName = "";
        AsnToken last = first;
        AsnToken? dot = this.PeekToken();
        if (this.IsKind(dot, AsnTokenKind.Dot))
        {
            AsnToken? reference = this.PeekTokenAt(1);
            if (reference == null || !this.IsTypeReference(reference))
            {
                this.AddDiagnostic(18, "Expected a type reference after '.'.", reference);
                return this.CreateResult(false, null, start);
            }

            moduleName = first.Text;
            this.Position = this.Position + 2;
            last = reference;
        }

        string referenceName = last.Text;
        return this.CreateResult(
            true,
            new ParsedTypeSyntax
            {
                Type = new AsnType
                {
                    Kind = AsnTypeKind.Defined,
                    Name = moduleName.Length == 0
                        ? referenceName
                        : moduleName + "." + referenceName
                },
                ReferenceModule = moduleName,
                ReferenceName = referenceName,
                SourceTypeName = moduleName.Length == 0
                    ? referenceName
                    : moduleName + "." + referenceName,
                Span = this.CreateSpan(first, last)
            },
            start);
    }

    internal NamedListParseResult ParseNamedList(
        bool allowBareNames,
        bool allowExtension,
        bool allowNegativeNumbers)
    {
        AsnToken? left = this.PeekToken();
        if (!this.IsKind(left, AsnTokenKind.LeftBrace))
        {
            return new NamedListParseResult();
        }

        this.Position = this.Position + 1;
        System.Collections.Generic.List<AsnNamedValue> root =
            new System.Collections.Generic.List<AsnNamedValue>();
        System.Collections.Generic.List<AsnNamedValue> extension =
            new System.Collections.Generic.List<AsnNamedValue>();
        System.Collections.Generic.List<AsnSourceSpan?> rootSpans =
            new System.Collections.Generic.List<AsnSourceSpan?>();
        System.Collections.Generic.List<AsnSourceSpan?> extensionSpans =
            new System.Collections.Generic.List<AsnSourceSpan?>();
        System.Collections.Generic.List<AsnToken> exception =
            new System.Collections.Generic.List<AsnToken>();
        bool inExtension = false;
        bool expectingItem = true;
        AsnToken? current = this.PeekToken();
        while (current != null && current.Kind != AsnTokenKind.RightBrace)
        {
            if (current.Kind == AsnTokenKind.Ellipsis)
            {
                if (!allowExtension || inExtension)
                {
                    this.AddDiagnostic(19, "An extension marker is not valid in this named list.", current);
                    return this.CreateNamedListFailure(root, extension, exception);
                }

                inExtension = true;
                expectingItem = false;
                this.Position = this.Position + 1;
                this.CollectExtensionException(exception);
                current = this.PeekToken();
            }
            else
            {
                int itemStart = this.Position;
                AsnNamedValue? item = this.ParseNamedItem(allowBareNames, allowNegativeNumbers);
                if (item == null)
                {
                    return this.CreateNamedListFailure(root, extension, exception);
                }

                AsnSourceSpan? itemSpan = itemStart < this.Tokens.Length
                    ? this.CreateSpan(this.Tokens[itemStart], this.PeekTokenAt(-1))
                    : null;

                if (inExtension)
                {
                    extension.Add(item);
                    extensionSpans.Add(itemSpan);
                }
                else
                {
                    root.Add(item);
                    rootSpans.Add(itemSpan);
                }

                expectingItem = false;
                current = this.PeekToken();
            }

            if (current != null && current.Kind == AsnTokenKind.Comma)
            {
                this.Position = this.Position + 1;
                expectingItem = true;
                current = this.PeekToken();
                continue;
            }

            if (current != null && current.Kind != AsnTokenKind.RightBrace)
            {
                this.AddDiagnostic(20, "Expected ',' or '}' in a named list.", current);
                return this.CreateNamedListFailure(root, extension, exception);
            }
        }

        if (current == null)
        {
            this.AddDiagnostic(21, "The named list is missing its closing brace.", null);
            return this.CreateNamedListFailure(root, extension, exception);
        }

        if (expectingItem && (root.Count > 0 || extension.Count > 0 || inExtension))
        {
            this.AddDiagnostic(22, "A trailing comma is not valid in a named list.", current);
            return this.CreateNamedListFailure(root, extension, exception);
        }

        if (!allowExtension && root.Count == 0)
        {
            this.AddDiagnostic(29, "A named-number or named-bit list cannot be empty.", current);
            return this.CreateNamedListFailure(root, extension, exception);
        }

        if (allowExtension && root.Count == 0 && !inExtension)
        {
            this.AddDiagnostic(29, "An enumeration list cannot be empty.", current);
            return this.CreateNamedListFailure(root, extension, exception);
        }

        this.Position = this.Position + 1;
        return new NamedListParseResult
        {
            Success = true,
            RootValues = root.ToArray(),
            ExtensionValues = extension.ToArray(),
            RootValueSpans = rootSpans.ToArray(),
            ExtensionValueSpans = extensionSpans.ToArray(),
            ExtensionExceptionTokens = exception.ToArray(),
            LastToken = current
        };
    }

    internal AsnNamedValue? ParseNamedItem(bool allowBareName, bool allowNegativeNumber)
    {
        AsnToken? name = this.PeekToken();
        if (name == null
            || (name.Kind != AsnTokenKind.Identifier && name.Kind != AsnTokenKind.ValueReference))
        {
            this.AddDiagnostic(23, "Expected an identifier in a named list.", name);
            return null;
        }

        this.Position = this.Position + 1;
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftParenthesis))
        {
            if (!allowBareName)
            {
                this.AddDiagnostic(24, "A named number or bit requires a parenthesized value.", name);
                return null;
            }

            return new AsnNamedValue
            {
                Name = name.Text
            };
        }

        this.Position = this.Position + 1;
        AsnValue? value = this.ParseNamedNumberValue(allowNegativeNumber);
        if (value == null)
        {
            return null;
        }

        AsnToken? right = this.PeekToken();
        if (!this.IsKind(right, AsnTokenKind.RightParenthesis))
        {
            this.AddDiagnostic(25, "A named value is missing its closing parenthesis.", right);
            return null;
        }

        this.Position = this.Position + 1;
        return new AsnNamedValue
        {
            Name = name.Text,
            Value = value
        };
    }

    internal AsnValue? ParseNamedNumberValue(bool allowNegative)
    {
        bool negative = false;
        AsnToken? current = this.PeekToken();
        if (this.IsKind(current, AsnTokenKind.Minus))
        {
            if (!allowNegative)
            {
                this.AddDiagnostic(26, "Named bit numbers cannot be negative.", current);
                return null;
            }

            negative = true;
            this.Position = this.Position + 1;
            current = this.PeekToken();
        }

        if (current != null && current.Kind == AsnTokenKind.IntegerLiteral)
        {
            System.Numerics.BigInteger number = System.Numerics.BigInteger.Parse(
                current.Text,
                System.Globalization.CultureInfo.InvariantCulture);
            this.Position = this.Position + 1;
            return new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = negative ? -number : number
            };
        }

        if (negative)
        {
            this.AddDiagnostic(27, "Expected a number after '-'.", current);
            return null;
        }

        string definedValue = this.ParseDefinedValueText();
        if (definedValue.Length == 0)
        {
            this.AddDiagnostic(28, "Expected a number or defined value.", current);
            return null;
        }

        return new AsnValue
        {
            Kind = AsnValueKind.Defined,
            TextValue = definedValue
        };
    }

    internal string ParseDefinedValueText()
    {
        AsnToken? first = this.PeekToken();
        if (first == null)
        {
            return "";
        }

        if (first.Kind == AsnTokenKind.Identifier || first.Kind == AsnTokenKind.ValueReference)
        {
            this.Position = this.Position + 1;
            return first.Text;
        }

        if (!this.IsTypeReference(first)
            || !this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Dot))
        {
            return "";
        }

        AsnToken? value = this.PeekTokenAt(2);
        if (value == null
            || (value.Kind != AsnTokenKind.Identifier && value.Kind != AsnTokenKind.ValueReference))
        {
            return "";
        }

        this.Position = this.Position + 3;
        return first.Text + "." + value.Text;
    }

    internal void CollectExtensionException(System.Collections.Generic.List<AsnToken> tokens)
    {
        AsnToken? current = this.PeekToken();
        if (!this.IsKind(current, AsnTokenKind.ExclamationMark))
        {
            return;
        }

        while (current != null
            && current.Kind != AsnTokenKind.Comma
            && current.Kind != AsnTokenKind.RightBrace)
        {
            tokens.Add(current);
            this.Position = this.Position + 1;
            current = this.PeekToken();
        }
    }

    internal bool ContainsExtensionMarker(int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (this.Tokens[index].Kind == AsnTokenKind.Ellipsis)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal AsnNamedValue[] CombineNamedValues(AsnNamedValue[] first, AsnNamedValue[] second)
    {
        AsnNamedValue[] result = new AsnNamedValue[first.Length + second.Length];
        int index = 0;
        while (index < first.Length)
        {
            result[index] = first[index];
            index = index + 1;
        }

        int secondIndex = 0;
        while (secondIndex < second.Length)
        {
            result[index] = second[secondIndex];
            index = index + 1;
            secondIndex = secondIndex + 1;
        }

        return result;
    }

    internal NamedListParseResult CreateNamedListFailure(
        System.Collections.Generic.List<AsnNamedValue> root,
        System.Collections.Generic.List<AsnNamedValue> extension,
        System.Collections.Generic.List<AsnToken> exception)
    {
        return new NamedListParseResult
        {
            RootValues = root.ToArray(),
            ExtensionValues = extension.ToArray(),
            ExtensionExceptionTokens = exception.ToArray()
        };
    }

    internal AsnTypeKind GetSimpleTypeKind(string text)
    {
        switch (text)
        {
            case "BOOLEAN":
                return AsnTypeKind.Boolean;
            case "REAL":
                return AsnTypeKind.Real;
            case "NULL":
                return AsnTypeKind.Null;
            case "RELATIVE-OID":
                return AsnTypeKind.RelativeObjectIdentifier;
            case "OID-IRI":
                return AsnTypeKind.ObjectIdentifierIri;
            case "RELATIVE-OID-IRI":
                return AsnTypeKind.RelativeObjectIdentifierIri;
            case "ObjectDescriptor":
                return AsnTypeKind.ObjectDescriptor;
            case "UTF8String":
                return AsnTypeKind.Utf8String;
            case "NumericString":
                return AsnTypeKind.NumericString;
            case "PrintableString":
                return AsnTypeKind.PrintableString;
            case "TeletexString":
            case "T61String":
                return AsnTypeKind.TeletexString;
            case "VideotexString":
                return AsnTypeKind.VideotexString;
            case "IA5String":
                return AsnTypeKind.Ia5String;
            case "GraphicString":
                return AsnTypeKind.GraphicString;
            case "VisibleString":
            case "ISO646String":
                return AsnTypeKind.VisibleString;
            case "GeneralString":
                return AsnTypeKind.GeneralString;
            case "UniversalString":
                return AsnTypeKind.UniversalString;
            case "BMPString":
                return AsnTypeKind.BmpString;
            case "UTCTime":
                return AsnTypeKind.UtcTime;
            case "GeneralizedTime":
                return AsnTypeKind.GeneralizedTime;
            case "TIME":
                return AsnTypeKind.Time;
            case "DATE":
                return AsnTypeKind.Date;
            case "TIME-OF-DAY":
                return AsnTypeKind.TimeOfDay;
            case "DATE-TIME":
                return AsnTypeKind.DateTime;
            case "DURATION":
                return AsnTypeKind.Duration;
            default:
                return AsnTypeKind.Unknown;
        }
    }

    internal bool IsTypeReference(AsnToken token)
    {
        return token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ModuleReference
            || token.Kind == AsnTokenKind.ObjectClassReference;
    }

    internal bool IsKind(AsnToken? token, AsnTokenKind kind)
    {
        return token != null && token.Kind == kind;
    }

    internal AsnToken? PeekToken()
    {
        return this.PeekTokenAt(0);
    }

    internal AsnToken? PeekTokenAt(int distance)
    {
        int index = this.Position + distance;
        return index < 0 || index >= this.Tokens.Length ? null : this.Tokens[index];
    }

    internal AsnToken ReadRequiredToken()
    {
        AsnToken token = this.Tokens[this.Position];
        this.Position = this.Position + 1;
        return token;
    }

    internal AsnToken[] CopyTokenRange(int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = start;
        while (index < end)
        {
            result[index - start] = this.Tokens[index];
            index = index + 1;
        }

        return result;
    }

    internal void AppendConstraintSyntax(
        ParsedTypeSyntax typeSyntax,
        ParsedConstraintSyntax constraintSyntax)
    {
        System.Collections.Generic.List<ParsedConstraintSyntax> syntax =
            new System.Collections.Generic.List<ParsedConstraintSyntax>();
        syntax.AddRange(typeSyntax.ConstraintSyntax);
        syntax.Add(constraintSyntax);
        typeSyntax.ConstraintSyntax = syntax.ToArray();

        System.Collections.Generic.List<AsnConstraint> constraints =
            new System.Collections.Generic.List<AsnConstraint>();
        constraints.AddRange(typeSyntax.Type.Constraints);
        constraints.Add(constraintSyntax.Constraint);
        typeSyntax.Type.Constraints = constraints.ToArray();
    }

    internal bool IsText(AsnToken? token, string text)
    {
        return token != null && token.Text == text;
    }

    internal AsnSourceSpan? CreateSpan(AsnToken first, AsnToken? last)
    {
        if (first.Span == null || last == null || last.Span == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = first.Span.Start,
            End = last.Span.End
        };
    }

    internal TypeParseResult CreateResult(bool success, ParsedTypeSyntax? syntax, int start)
    {
        return new TypeParseResult
        {
            Success = success,
            Syntax = syntax,
            ConsumedTokenCount = this.Position - start
        };
    }

    internal void AddDiagnostic(int number, string message, AsnToken? token)
    {
        if (!this.ReportDiagnostics)
        {
            return;
        }

        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Syntax,
            number,
            message,
            token == null ? null : token.Span,
            null);
    }
}
