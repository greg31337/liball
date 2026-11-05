namespace asn1;

internal class ObjectClassParser
{
    internal AsnToken[] Tokens;
    internal DiagnosticBag Diagnostics;
    internal bool ReportDiagnostics;
    internal int Position;
    internal AsnToken[] TemplateTokens = [];
    internal int TemplatePosition;

    internal ObjectClassParser(
        AsnToken[] tokens,
        DiagnosticBag diagnostics,
        bool reportDiagnostics)
    {
        this.Tokens = tokens == null ? throw new System.ArgumentNullException("tokens") : tokens;
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
        this.ReportDiagnostics = reportDiagnostics;
    }

    internal ObjectClassParseResult ParseObjectClassSyntax()
    {
        int start = this.Position;
        AsnToken? first = this.PeekToken();
        if (first == null)
        {
            this.AddDiagnostic(154, "Expected an object-class definition or reference.", null);
            return this.CreateResult(false, null, start);
        }

        ObjectClassParseResult parameterized = this.ParseParameterizedObjectClass(start);
        if (parameterized.Success)
        {
            return parameterized;
        }

        if (first.Text != "CLASS")
        {
            return this.ParseDefinedObjectClass(start);
        }

        this.Position = this.Position + 1;
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            this.AddDiagnostic(155, "Expected '{' after CLASS.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        System.Collections.Generic.List<ParsedObjectClassFieldSyntax> fields =
            new System.Collections.Generic.List<ParsedObjectClassFieldSyntax>();
        if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(156, "An object class requires at least one field specification.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        while (this.PeekToken() != null
            && !this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            ParsedObjectClassFieldSyntax? field = this.ParseFieldSpec();
            if (field == null)
            {
                return this.CreateResult(false, this.CreatePartialSyntax(start, fields), start);
            }

            fields.Add(field);
            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                AsnToken comma = this.ReadRequiredToken();
                if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
                {
                    this.AddDiagnostic(157, "An object-class field list cannot have a trailing comma.", comma);
                    return this.CreateResult(false, this.CreatePartialSyntax(start, fields), start);
                }

                continue;
            }

            if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
            {
                this.AddDiagnostic(158, "Expected ',' or '}' after the object-class field.", this.PeekToken());
                return this.CreateResult(false, this.CreatePartialSyntax(start, fields), start);
            }
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(159, "The object-class definition is missing its closing brace.", this.PeekTokenAt(-1));
            return this.CreateResult(false, this.CreatePartialSyntax(start, fields), start);
        }

        this.Position = this.Position + 1;
        ParsedObjectClassSyntax syntax = this.CreatePartialSyntax(start, fields);
        if (this.PeekToken() != null && this.PeekToken()?.Text == "WITH")
        {
            if (!this.ParseWithSyntax(syntax))
            {
                syntax.SourceTokens = this.CopyTokenRange(start, this.Position);
                syntax.Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1));
                return this.CreateResult(false, syntax, start);
            }
        }

        syntax.SourceTokens = this.CopyTokenRange(start, this.Position);
        syntax.Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1));
        return this.CreateResult(true, syntax, start);
    }

    internal ObjectClassParseResult ParseParameterizedObjectClass(int start)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ParameterizedReferenceParseResult parsed = new ParameterizedParser(
            remaining,
            this.Diagnostics,
            false).ParseParameterizedReferenceSyntax(AsnParameterKind.ObjectClass, null);
        if (!parsed.Success || parsed.Syntax == null)
        {
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + parsed.ConsumedTokenCount;
        string name = parsed.Syntax.ReferenceModule.Length == 0
            ? parsed.Syntax.ReferenceName
            : parsed.Syntax.ReferenceModule + "." + parsed.Syntax.ReferenceName;
        ParsedObjectClassSyntax syntax = new ParsedObjectClassSyntax
        {
            ObjectClass = new AsnObjectClass { Name = name },
            ParameterizedReference = parsed.Syntax,
            ReferenceModule = parsed.Syntax.ReferenceModule,
            ReferenceName = parsed.Syntax.ReferenceName,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = parsed.Syntax.Span
        };
        return this.CreateResult(true, syntax, start);
    }

    internal ObjectClassParseResult ParseDefinedObjectClass(int start)
    {
        AsnToken? first = this.PeekToken();
        if (!this.IsObjectClassReference(first))
        {
            this.AddDiagnostic(154, "Expected an object-class definition or reference.", first);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        string module = "";
        AsnToken last = first == null ? new AsnToken() : first;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.Dot))
        {
            AsnToken? reference = this.PeekTokenAt(1);
            if (!this.IsObjectClassReference(reference))
            {
                this.AddDiagnostic(160, "Expected an object-class reference after '.'.", reference);
                return this.CreateResult(false, null, start);
            }

            module = last.Text;
            this.Position = this.Position + 2;
            last = reference == null ? last : reference;
        }

        string name = module.Length == 0 ? last.Text : module + "." + last.Text;
        ParsedObjectClassSyntax syntax = new ParsedObjectClassSyntax
        {
            ObjectClass = new AsnObjectClass { Name = name },
            ReferenceModule = module,
            ReferenceName = last.Text,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), last)
        };
        return this.CreateResult(true, syntax, start);
    }

    internal ParsedObjectClassSyntax CreatePartialSyntax(
        int start,
        System.Collections.Generic.List<ParsedObjectClassFieldSyntax> fields)
    {
        AsnObjectClassField[] publicFields = new AsnObjectClassField[fields.Count];
        int index = 0;
        while (index < fields.Count)
        {
            publicFields[index] = fields[index].Field;
            index = index + 1;
        }

        return new ParsedObjectClassSyntax
        {
            ObjectClass = new AsnObjectClass { Fields = publicFields },
            FieldSyntax = fields.ToArray(),
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
    }

    internal ParsedObjectClassFieldSyntax? ParseFieldSpec()
    {
        int start = this.Position;
        AsnToken? name = this.PeekToken();
        if (!this.IsFieldReference(name))
        {
            this.AddDiagnostic(161, "Expected an object-class field reference.", name);
            return null;
        }

        this.Position = this.Position + 1;
        ParsedObjectClassFieldSyntax syntax = new ParsedObjectClassFieldSyntax
        {
            Field = new AsnObjectClassField { Name = name == null ? "" : name.Text }
        };
        bool upper = name != null && name.Text.Length > 1
            && name.Text[1] >= 'A' && name.Text[1] <= 'Z';
        if (upper && this.IsTypeFieldTail(this.PeekToken()))
        {
            syntax.Field.Kind = AsnObjectFieldKind.Type;
            if (!this.ParseTypeOptionality(syntax))
            {
                return null;
            }

            return this.CompleteFieldSyntax(syntax, start);
        }

        if (this.IsFieldReference(this.PeekToken()))
        {
            syntax.GovernorFieldPath = this.ParseFieldName();
            syntax.Field.Kind = upper
                ? AsnObjectFieldKind.VariableTypeValueSet
                : AsnObjectFieldKind.VariableTypeValue;
        }
        else if (this.IsObjectClassGovernorStart())
        {
            if (!this.ParseObjectClassGovernor(syntax))
            {
                return null;
            }

            syntax.Field.Kind = upper ? AsnObjectFieldKind.ObjectSet : AsnObjectFieldKind.Object;
        }
        else
        {
            AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
            TypeParseResult governor = new TypeParser(remaining, this.Diagnostics, false).ParseTypeSyntax();
            if (!governor.Success || governor.Syntax == null || governor.ConsumedTokenCount == 0)
            {
                this.AddDiagnostic(162, "Expected a type, field name, or object-class governor.", this.PeekToken());
                return null;
            }

            this.Position = this.Position + governor.ConsumedTokenCount;
            syntax.GovernorTypeSyntax = governor.Syntax;
            syntax.Field.Type = governor.Syntax.Type;
            syntax.Field.Kind = upper
                ? AsnObjectFieldKind.FixedTypeValueSet
                : AsnObjectFieldKind.FixedTypeValue;
        }

        if (syntax.Field.Kind == AsnObjectFieldKind.FixedTypeValue
            && this.PeekToken() != null && this.PeekToken()?.Text == "UNIQUE")
        {
            syntax.Field.Unique = true;
            this.Position = this.Position + 1;
        }

        if (!this.ParseSettingOptionality(syntax))
        {
            return null;
        }

        return this.CompleteFieldSyntax(syntax, start);
    }

    internal bool ParseTypeOptionality(ParsedObjectClassFieldSyntax syntax)
    {
        AsnToken? token = this.PeekToken();
        if (token != null && token.Text == "OPTIONAL")
        {
            syntax.Field.Optional = true;
            this.Position = this.Position + 1;
            return true;
        }

        if (token == null || token.Text != "DEFAULT")
        {
            return true;
        }

        this.Position = this.Position + 1;
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        TypeParseResult value = new TypeParser(remaining, this.Diagnostics, false).ParseTypeSyntax();
        if (!value.Success || value.Syntax == null || value.ConsumedTokenCount == 0)
        {
            this.AddDiagnostic(163, "Expected a type after DEFAULT.", this.PeekToken());
            return false;
        }

        this.Position = this.Position + value.ConsumedTokenCount;
        syntax.DefaultTypeSyntax = value.Syntax;
        syntax.Field.HasDefaultSetting = true;
        syntax.Field.DefaultSetting = new AsnObjectFieldSetting
        {
            FieldName = syntax.Field.Name,
            Type = value.Syntax.Type
        };
        return true;
    }

    internal bool ParseSettingOptionality(ParsedObjectClassFieldSyntax syntax)
    {
        AsnToken? token = this.PeekToken();
        if (token != null && token.Text == "OPTIONAL")
        {
            syntax.Field.Optional = true;
            this.Position = this.Position + 1;
            return true;
        }

        if (token == null || token.Text != "DEFAULT")
        {
            return true;
        }

        this.Position = this.Position + 1;
        syntax.Field.HasDefaultSetting = true;
        if (syntax.Field.Kind == AsnObjectFieldKind.FixedTypeValue
            || syntax.Field.Kind == AsnObjectFieldKind.VariableTypeValue)
        {
            AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
            AsnType? expected = syntax.Field.Type;
            ValueParseResult value = new ValueParser(
                remaining,
                this.Diagnostics,
                false).ParseValueSyntax(expected);
            if (!value.Success || value.Syntax == null || value.ConsumedTokenCount == 0)
            {
                this.AddDiagnostic(164, "Expected a value after DEFAULT.", this.PeekToken());
                return false;
            }

            this.Position = this.Position + value.ConsumedTokenCount;
            syntax.DefaultValueSyntax = value.Syntax;
            syntax.DefaultTokens = this.CopyTokenRange(
                this.Position - value.ConsumedTokenCount,
                this.Position);
            syntax.Field.DefaultSetting = new AsnObjectFieldSetting
            {
                FieldName = syntax.Field.Name,
                Value = value.Syntax.Value
            };
            return true;
        }

        int defaultStart = this.Position;
        this.ReadRawDefault();
        if (this.Position == defaultStart)
        {
            this.AddDiagnostic(165, "Expected a field setting after DEFAULT.", this.PeekToken());
            return false;
        }

        syntax.DefaultTokens = this.CopyTokenRange(defaultStart, this.Position);
        string defaultName = this.JoinTokenText(syntax.DefaultTokens);
        AsnObjectFieldSetting setting = new AsnObjectFieldSetting
        {
            FieldName = syntax.Field.Name
        };
        if (syntax.Field.Kind == AsnObjectFieldKind.Object)
        {
            setting.Object = new AsnInformationObject { Name = defaultName };
        }
        else if (syntax.Field.Kind == AsnObjectFieldKind.ObjectSet)
        {
            setting.ObjectSet = new AsnInformationObjectSet { Name = defaultName };
        }

        syntax.Field.DefaultSetting = setting;
        return true;
    }

    internal void ReadRawDefault()
    {
        int braces = 0;
        int parentheses = 0;
        int brackets = 0;
        while (this.PeekToken() != null)
        {
            AsnToken current = this.PeekToken() ?? new AsnToken();
            if (braces == 0 && parentheses == 0 && brackets == 0
                && (current.Kind == AsnTokenKind.Comma
                    || current.Kind == AsnTokenKind.RightBrace))
            {
                return;
            }

            if (current.Kind == AsnTokenKind.LeftBrace)
            {
                braces = braces + 1;
            }
            else if (current.Kind == AsnTokenKind.RightBrace && braces > 0)
            {
                braces = braces - 1;
            }
            else if (current.Kind == AsnTokenKind.LeftParenthesis)
            {
                parentheses = parentheses + 1;
            }
            else if (current.Kind == AsnTokenKind.RightParenthesis && parentheses > 0)
            {
                parentheses = parentheses - 1;
            }
            else if (current.Kind == AsnTokenKind.LeftBracket
                || current.Kind == AsnTokenKind.LeftVersionBracket)
            {
                brackets = brackets + (current.Kind == AsnTokenKind.LeftVersionBracket ? 2 : 1);
            }
            else if ((current.Kind == AsnTokenKind.RightBracket
                || current.Kind == AsnTokenKind.RightVersionBracket) && brackets > 0)
            {
                brackets = brackets - (current.Kind == AsnTokenKind.RightVersionBracket ? 2 : 1);
            }

            this.Position = this.Position + 1;
        }
    }

    internal string[] ParseFieldName()
    {
        System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
        while (this.IsFieldReference(this.PeekToken()))
        {
            AsnToken field = this.ReadRequiredToken();
            parts.Add(field.Text);
            if (!this.IsKind(this.PeekToken(), AsnTokenKind.Dot))
            {
                break;
            }

            if (!this.IsFieldReference(this.PeekTokenAt(1)))
            {
                break;
            }

            this.Position = this.Position + 1;
        }

        return parts.ToArray();
    }

    internal bool ParseObjectClassGovernor(ParsedObjectClassFieldSyntax syntax)
    {
        AsnToken? first = this.PeekToken();
        if (!this.IsObjectClassReference(first))
        {
            return false;
        }

        this.Position = this.Position + 1;
        string module = "";
        AsnToken last = first == null ? new AsnToken() : first;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.Dot))
        {
            AsnToken? reference = this.PeekTokenAt(1);
            if (!this.IsObjectClassReference(reference))
            {
                this.AddDiagnostic(160, "Expected an object-class reference after '.'.", reference);
                return false;
            }

            module = last.Text;
            this.Position = this.Position + 2;
            last = reference == null ? last : reference;
        }

        syntax.GovernorObjectClassModule = module;
        syntax.GovernorObjectClassName = last.Text;
        return true;
    }

    internal bool ParseWithSyntax(ParsedObjectClassSyntax syntax)
    {
        this.Position = this.Position + 1;
        AsnToken? syntaxWord = this.PeekToken();
        if (syntaxWord == null || syntaxWord.Text != "SYNTAX")
        {
            this.AddDiagnostic(166, "Expected SYNTAX after WITH.", syntaxWord);
            return false;
        }

        this.Position = this.Position + 1;
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            this.AddDiagnostic(167, "Expected '{' after WITH SYNTAX.", this.PeekToken());
            return false;
        }

        this.Position = this.Position + 1;
        int templateStart = this.Position;
        while (this.PeekToken() != null
            && !this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.Position = this.Position + 1;
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(168, "The WITH SYNTAX template is missing its closing brace.", this.PeekTokenAt(-1));
            return false;
        }

        AsnToken[] raw = this.CopyTokenRange(templateStart, this.Position);
        this.TemplateTokens = this.NormalizeTemplateTokens(raw);
        this.TemplatePosition = 0;
        System.Collections.Generic.List<ParsedSyntaxTemplateItem> items =
            new System.Collections.Generic.List<ParsedSyntaxTemplateItem>();
        if (!this.ParseTemplateItems(items, false))
        {
            return false;
        }

        if (items.Count == 0)
        {
            this.AddDiagnostic(169, "A WITH SYNTAX template requires at least one token or optional group.", this.PeekToken());
            return false;
        }

        this.Position = this.Position + 1;
        syntax.SyntaxTemplate = items.ToArray();
        string[] publicTokens = new string[this.TemplateTokens.Length];
        int index = 0;
        while (index < this.TemplateTokens.Length)
        {
            publicTokens[index] = this.TemplateTokens[index].Text;
            index = index + 1;
        }

        syntax.ObjectClass.SyntaxTokens = publicTokens;
        return true;
    }

    internal bool ParseTemplateItems(
        System.Collections.Generic.List<ParsedSyntaxTemplateItem> items,
        bool stopAtRightBracket)
    {
        while (this.TemplatePosition < this.TemplateTokens.Length)
        {
            AsnToken current = this.TemplateTokens[this.TemplatePosition];
            if (current.Kind == AsnTokenKind.RightBracket)
            {
                if (!stopAtRightBracket)
                {
                    this.AddDiagnostic(170, "A WITH SYNTAX template contains an unmatched ']'.", current);
                    return false;
                }

                return true;
            }

            if (current.Kind == AsnTokenKind.LeftBracket)
            {
                int groupStart = this.TemplatePosition;
                this.TemplatePosition = this.TemplatePosition + 1;
                System.Collections.Generic.List<ParsedSyntaxTemplateItem> children =
                    new System.Collections.Generic.List<ParsedSyntaxTemplateItem>();
                if (!this.ParseTemplateItems(children, true))
                {
                    return false;
                }

                if (this.TemplatePosition >= this.TemplateTokens.Length
                    || this.TemplateTokens[this.TemplatePosition].Kind != AsnTokenKind.RightBracket)
                {
                    this.AddDiagnostic(171, "An optional WITH SYNTAX group is missing its closing ']'.", current);
                    return false;
                }

                if (children.Count == 0)
                {
                    this.AddDiagnostic(172, "An optional WITH SYNTAX group cannot be empty.", current);
                    return false;
                }

                this.TemplatePosition = this.TemplatePosition + 1;
                items.Add(new ParsedSyntaxTemplateItem
                {
                    OptionalGroup = true,
                    Children = children.ToArray(),
                    SourceTokens = this.CopyTemplateTokenRange(groupStart, this.TemplatePosition),
                    Span = this.CreateSpan(current, this.TemplateTokens[this.TemplatePosition - 1])
                });
                continue;
            }

            if (!this.IsRequiredSyntaxToken(current))
            {
                this.AddDiagnostic(173, "Expected a literal word, comma, field reference, or optional group in WITH SYNTAX.", current);
                return false;
            }

            this.TemplatePosition = this.TemplatePosition + 1;
            items.Add(new ParsedSyntaxTemplateItem
            {
                Text = current.Text,
                FieldReference = this.IsFieldReference(current),
                SourceTokens = [current],
                Span = current.Span
            });
        }

        if (stopAtRightBracket)
        {
            this.AddDiagnostic(
                171,
                "An optional WITH SYNTAX group is missing its closing ']'.",
                this.TemplatePosition == 0
                    ? null
                    : this.TemplateTokens[this.TemplatePosition - 1]);
            return false;
        }

        return true;
    }

    internal AsnToken[] NormalizeTemplateTokens(AsnToken[] tokens)
    {
        System.Collections.Generic.List<AsnToken> normalized =
            new System.Collections.Generic.List<AsnToken>();
        int index = 0;
        while (index < tokens.Length)
        {
            AsnToken token = tokens[index];
            if (token.Kind == AsnTokenKind.LeftVersionBracket)
            {
                normalized.Add(this.CreateBracketToken(token, true));
                normalized.Add(this.CreateBracketToken(token, true));
            }
            else if (token.Kind == AsnTokenKind.RightVersionBracket)
            {
                normalized.Add(this.CreateBracketToken(token, false));
                normalized.Add(this.CreateBracketToken(token, false));
            }
            else
            {
                normalized.Add(token);
            }

            index = index + 1;
        }

        return normalized.ToArray();
    }

    internal AsnToken CreateBracketToken(AsnToken source, bool left)
    {
        return new AsnToken
        {
            Kind = left ? AsnTokenKind.LeftBracket : AsnTokenKind.RightBracket,
            Text = left ? "[" : "]",
            Span = source.Span
        };
    }

    internal bool IsRequiredSyntaxToken(AsnToken token)
    {
        if (token.Kind == AsnTokenKind.Comma || this.IsFieldReference(token))
        {
            return true;
        }

        return token.Kind == AsnTokenKind.ReservedWord
            || token.Kind == AsnTokenKind.Identifier
            || token.Kind == AsnTokenKind.ValueReference
            || token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ModuleReference
            || token.Kind == AsnTokenKind.ObjectReference
            || token.Kind == AsnTokenKind.ObjectSetReference
            || token.Kind == AsnTokenKind.ObjectClassReference
            || token.Kind == AsnTokenKind.EncodingReference;
    }

    internal ParsedObjectClassFieldSyntax CompleteFieldSyntax(
        ParsedObjectClassFieldSyntax syntax,
        int start)
    {
        syntax.SourceTokens = this.CopyTokenRange(start, this.Position);
        syntax.Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1));
        return syntax;
    }

    internal bool IsTypeFieldTail(AsnToken? token)
    {
        return token == null
            || token.Kind == AsnTokenKind.Comma
            || token.Kind == AsnTokenKind.RightBrace
            || token.Text == "OPTIONAL"
            || token.Text == "DEFAULT";
    }

    internal bool IsObjectClassGovernorStart()
    {
        AsnToken? first = this.PeekToken();
        if (first == null)
        {
            return false;
        }

        if (first.Text == "TYPE-IDENTIFIER" || first.Text == "ABSTRACT-SYNTAX")
        {
            return true;
        }

        if (first.Kind == AsnTokenKind.ObjectClassReference)
        {
            return true;
        }

        return this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Dot)
            && this.IsObjectClassReference(this.PeekTokenAt(2));
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

    internal bool IsFieldReference(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.TypeFieldReference
                || token.Kind == AsnTokenKind.ValueFieldReference
                || token.Kind == AsnTokenKind.ValueSetFieldReference
                || token.Kind == AsnTokenKind.ObjectFieldReference
                || token.Kind == AsnTokenKind.ObjectSetFieldReference);
    }

    internal string JoinTokenText(AsnToken[] tokens)
    {
        string result = "";
        int index = 0;
        while (index < tokens.Length)
        {
            if (index > 0)
            {
                result = result + " ";
            }

            result = result + tokens[index].Text;
            index = index + 1;
        }

        return result;
    }

    internal ObjectClassParseResult CreateResult(
        bool success,
        ParsedObjectClassSyntax? syntax,
        int start)
    {
        return new ObjectClassParseResult
        {
            Success = success,
            Syntax = syntax,
            ConsumedTokenCount = this.Position - start
        };
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

    internal AsnToken[] CopyTemplateTokenRange(int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = start;
        while (index < end)
        {
            result[index - start] = this.TemplateTokens[index];
            index = index + 1;
        }

        return result;
    }

    internal AsnToken ReadRequiredToken()
    {
        AsnToken token = this.Tokens[this.Position];
        this.Position = this.Position + 1;
        return token;
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

    internal AsnToken? TokenAt(int index)
    {
        return index < 0 || index >= this.Tokens.Length ? null : this.Tokens[index];
    }

    internal bool IsKind(AsnToken? token, AsnTokenKind kind)
    {
        return token != null && token.Kind == kind;
    }

    internal AsnSourceSpan? CreateSpan(AsnToken? first, AsnToken? last)
    {
        if (first == null || first.Span == null || last == null || last.Span == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = first.Span.Start,
            End = last.Span.End
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
