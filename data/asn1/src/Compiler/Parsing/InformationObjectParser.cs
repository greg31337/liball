namespace asn1;

internal class InformationObjectParser
{
    internal AsnToken[] Tokens;
    internal DiagnosticBag Diagnostics;
    internal bool ReportDiagnostics;
    internal int Position;

    internal InformationObjectParser(
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

    internal InformationObjectParseResult ParseObjectSyntax(
        ParsedObjectClassSyntax? governingClass)
    {
        int start = this.Position;
        if (this.IsInformationFromObjectsStart(AsnAssignmentKind.Object))
        {
            InformationFromObjectsParseResult selected = this.ParseSelectedInformation(
                AsnAssignmentKind.Object);
            if (!selected.Success || selected.Syntax == null)
            {
                return this.CreateObjectResult(false, null, start);
            }

            ParsedInformationObjectSyntax selectedSyntax = new ParsedInformationObjectSyntax
            {
                Object = new AsnInformationObject
                {
                    Name = this.JoinTokenText(selected.Syntax.SourceTokens),
                    ObjectClass = governingClass == null ? null : governingClass.ObjectClass
                },
                InformationFromObjects = selected.Syntax,
                SourceTokens = this.CopyTokenRange(start, this.Position),
                Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
            };
            return this.CreateObjectResult(true, selectedSyntax, start);
        }

        InformationObjectParseResult parameterized = this.ParseParameterizedObject(
            start,
            governingClass);
        if (parameterized.Success)
        {
            return parameterized;
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            return this.ParseDefinedObjectReference(start, governingClass);
        }

        this.Position = this.Position + 1;
        ParsedInformationObjectSyntax syntax = new ParsedInformationObjectSyntax
        {
            Object = new AsnInformationObject
            {
                ObjectClass = governingClass == null ? null : governingClass.ObjectClass
            }
        };
        bool success;
        if (governingClass != null && governingClass.SyntaxTemplate.Length > 0)
        {
            syntax.DefinedSyntax = true;
            success = this.ParseDefinedObjectBody(syntax, governingClass);
        }
        else
        {
            syntax.DefaultSyntax = true;
            success = this.ParseDefaultObjectBody(syntax, governingClass);
        }

        if (!success)
        {
            syntax.SourceTokens = this.CopyTokenRange(start, this.Position);
            syntax.Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1));
            return this.CreateObjectResult(false, syntax, start);
        }

        syntax.SourceTokens = this.CopyTokenRange(start, this.Position);
        syntax.Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1));
        return this.CreateObjectResult(true, syntax, start);
    }

    internal InformationObjectParseResult ParseParameterizedObject(
        int start,
        ParsedObjectClassSyntax? governingClass)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ParameterizedReferenceParseResult parsed = new ParameterizedParser(
            remaining,
            this.Diagnostics,
            false).ParseParameterizedReferenceSyntax(AsnParameterKind.Object, null);
        if (!parsed.Success || parsed.Syntax == null)
        {
            return this.CreateObjectResult(false, null, start);
        }

        this.Position = this.Position + parsed.ConsumedTokenCount;
        string name = parsed.Syntax.ReferenceModule.Length == 0
            ? parsed.Syntax.ReferenceName
            : parsed.Syntax.ReferenceModule + "." + parsed.Syntax.ReferenceName;
        ParsedInformationObjectSyntax syntax = new ParsedInformationObjectSyntax
        {
            Object = new AsnInformationObject
            {
                Name = name,
                ObjectClass = governingClass == null ? null : governingClass.ObjectClass
            },
            ParameterizedReference = parsed.Syntax,
            ReferenceModule = parsed.Syntax.ReferenceModule,
            ReferenceName = parsed.Syntax.ReferenceName,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = parsed.Syntax.Span
        };
        return this.CreateObjectResult(true, syntax, start);
    }

    internal InformationObjectParseResult ParseDefinedObjectReference(
        int start,
        ParsedObjectClassSyntax? governingClass)
    {
        AsnToken? first = this.PeekToken();
        if (!this.IsLowerReference(first))
        {
            this.AddDiagnostic(177, "Expected an information object definition or reference.", first);
            return this.CreateObjectResult(false, null, start);
        }

        this.Position = this.Position + 1;
        string module = "";
        AsnToken reference = first == null ? new AsnToken() : first;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.Dot)
            && this.IsLowerReference(this.PeekTokenAt(1)))
        {
            module = reference.Text;
            reference = this.PeekTokenAt(1) ?? reference;
            this.Position = this.Position + 2;
        }

        string name = module.Length == 0 ? reference.Text : module + "." + reference.Text;
        ParsedInformationObjectSyntax syntax = new ParsedInformationObjectSyntax
        {
            Object = new AsnInformationObject
            {
                Name = name,
                ObjectClass = governingClass == null ? null : governingClass.ObjectClass
            },
            ReferenceModule = module,
            ReferenceName = reference.Text,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), reference)
        };
        return this.CreateObjectResult(true, syntax, start);
    }

    internal bool ParseDefaultObjectBody(
        ParsedInformationObjectSyntax syntax,
        ParsedObjectClassSyntax? governingClass)
    {
        if ((governingClass == null
                || governingClass.ObjectClass.Fields.Length == 0)
            && this.PeekToken() != null
            && !this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace)
            && !this.IsFieldReference(this.PeekToken()))
        {
            int bodyStart = this.Position;
            if (!this.ReadOpaqueDefinedObjectBody())
            {
                this.AddDiagnostic(181, "The information object definition is missing its closing brace.", this.PeekTokenAt(-1));
                return false;
            }

            syntax.DefaultSyntax = false;
            syntax.DefinedSyntax = true;
            syntax.DefinedSyntaxTokens = this.CopyTokenRange(bodyStart, this.Position - 1);
            return true;
        }

        System.Collections.Generic.List<ParsedObjectFieldSettingSyntax> settings =
            new System.Collections.Generic.List<ParsedObjectFieldSettingSyntax>();
        System.Collections.Generic.List<ParsedRecoverySyntax> recoveries =
            new System.Collections.Generic.List<ParsedRecoverySyntax>();
        bool success = true;
        while (this.PeekToken() != null
            && !this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            int settingStart = this.Position;
            AsnToken? fieldName = this.PeekToken();
            if (!this.IsFieldReference(fieldName))
            {
                this.AddDiagnostic(178, "Expected a primitive field name in the object definition.", fieldName);
                success = false;
                this.RecoverObjectSyntax(settingStart, recoveries);
            }
            else
            {
                this.Position = this.Position + 1;
                AsnObjectClassField? field = this.FindClassField(
                    governingClass,
                    fieldName == null ? "" : fieldName.Text);
                ParsedObjectFieldSettingSyntax? setting = this.ParseFieldSetting(
                    fieldName == null ? "" : fieldName.Text,
                    field);
                if (setting == null)
                {
                    success = false;
                    this.RecoverObjectSyntax(settingStart, recoveries);
                }
                else
                {
                    settings.Add(setting);
                }
            }

            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                AsnToken comma = this.ReadRequiredToken();
                if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
                {
                    this.AddDiagnostic(179, "An object definition cannot have a trailing comma.", comma);
                    success = false;
                    break;
                }

                continue;
            }

            if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
            {
                this.AddDiagnostic(180, "Expected ',' or '}' after the field setting.", this.PeekToken());
                success = false;
                this.RecoverObjectSyntax(this.Position, recoveries);
                if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
                {
                    this.Position = this.Position + 1;
                    continue;
                }

                if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
                {
                    break;
                }
            }
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(181, "The information object definition is missing its closing brace.", this.PeekTokenAt(-1));
            syntax.FieldSettingSyntax = settings.ToArray();
            syntax.Object.FieldSettings = this.CopyPublicSettings(settings);
            syntax.Recoveries = recoveries.ToArray();
            return false;
        }

        this.Position = this.Position + 1;
        syntax.FieldSettingSyntax = settings.ToArray();
        syntax.Object.FieldSettings = this.CopyPublicSettings(settings);
        syntax.Recoveries = recoveries.ToArray();
        return success;
    }

    internal bool ReadOpaqueDefinedObjectBody()
    {
        int nestedBraces = 0;
        while (this.PeekToken() != null)
        {
            AsnToken current = this.ReadRequiredToken();
            if (current.Kind == AsnTokenKind.LeftBrace)
            {
                nestedBraces = nestedBraces + 1;
            }
            else if (current.Kind == AsnTokenKind.RightBrace)
            {
                if (nestedBraces == 0)
                {
                    return true;
                }

                nestedBraces = nestedBraces - 1;
            }
        }

        return false;
    }

    internal bool ParseDefinedObjectBody(
        ParsedInformationObjectSyntax syntax,
        ParsedObjectClassSyntax governingClass)
    {
        int definedStart = this.Position;
        System.Collections.Generic.List<ParsedObjectFieldSettingSyntax> settings =
            new System.Collections.Generic.List<ParsedObjectFieldSettingSyntax>();
        bool matched = governingClass.SyntaxGrammar == null
            ? this.ParseDefinedTemplateItems(
                governingClass.SyntaxTemplate,
                governingClass,
                settings)
            : this.ParseCompiledSyntaxItems(
                governingClass.SyntaxGrammar.Items,
                settings);
        if (!matched)
        {
            this.RecoverDefinedObjectSyntax(definedStart, syntax, settings);
            return false;
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(182, "The defined object syntax does not match its WITH SYNTAX template.", this.PeekToken());
            this.RecoverDefinedObjectSyntax(definedStart, syntax, settings);
            return false;
        }

        syntax.DefinedSyntaxTokens = this.CopyTokenRange(definedStart, this.Position);
        this.Position = this.Position + 1;
        syntax.FieldSettingSyntax = settings.ToArray();
        syntax.Object.FieldSettings = this.CopyPublicSettings(settings);
        return true;
    }

    internal bool ParseCompiledSyntaxItems(
        CompiledSyntaxItem[] items,
        System.Collections.Generic.List<ParsedObjectFieldSettingSyntax> settings)
    {
        int index = 0;
        while (index < items.Length)
        {
            CompiledSyntaxItem item = items[index];
            if (item.OptionalGroup)
            {
                if (this.ShouldParseOptionalSyntaxGroup(item))
                {
                    int savedCount = settings.Count;
                    if (!this.ParseCompiledSyntaxItems(item.Children, settings))
                    {
                        return false;
                    }

                    if (settings.Count == savedCount
                        && this.ContainsCompiledSyntaxLiteral(item.Children))
                    {
                        this.AddDiagnostic(
                            195,
                            "A present optional defined-syntax group must include a field setting.",
                            this.PeekTokenAt(-1));
                        return false;
                    }
                }

                index = index + 1;
                continue;
            }

            if (item.Literal.Length > 0)
            {
                AsnToken? literal = this.PeekToken();
                if (literal == null || literal.Text != item.Literal)
                {
                    this.AddDiagnostic(
                        183,
                        "Expected literal '" + item.Literal
                            + "' in defined object syntax.",
                        literal);
                    return false;
                }

                this.Position = this.Position + 1;
                index = index + 1;
                continue;
            }

            if (item.Field == null)
            {
                return false;
            }

            ParsedObjectFieldSettingSyntax? setting = this.ParseFieldSetting(
                item.Field.Name,
                item.Field);
            if (setting == null)
            {
                return false;
            }

            settings.Add(setting);
            index = index + 1;
        }

        return true;
    }

    internal bool ShouldParseOptionalSyntaxGroup(CompiledSyntaxItem group)
    {
        int index = 0;
        while (index < group.FirstItems.Length)
        {
            if (this.CanStartCompiledSyntaxItem(group.FirstItems[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsCompiledSyntaxLiteral(CompiledSyntaxItem[] items)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index].Literal.Length > 0
                || (items[index].OptionalGroup
                    && this.ContainsCompiledSyntaxLiteral(items[index].Children)))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool CanStartCompiledSyntaxItem(CompiledSyntaxItem item)
    {
        AsnToken? token = this.PeekToken();
        if (item.Literal.Length > 0)
        {
            return token != null && token.Text == item.Literal;
        }

        if (item.Field == null || token == null)
        {
            return false;
        }

        int savedPosition = this.Position;
        bool savedReporting = this.ReportDiagnostics;
        this.ReportDiagnostics = false;
        ParsedObjectFieldSettingSyntax? setting = this.ParseFieldSetting(
            item.Field.Name,
            item.Field);
        bool matched = setting != null && this.Position > savedPosition;
        this.Position = savedPosition;
        this.ReportDiagnostics = savedReporting;
        return matched;
    }

    internal void RecoverObjectSyntax(
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
                    || current.Kind == AsnTokenKind.RightBracket))
            {
                break;
            }

            if (current.Kind == AsnTokenKind.LeftBrace
                || current.Kind == AsnTokenKind.LeftParenthesis
                || current.Kind == AsnTokenKind.LeftBracket)
            {
                depth = depth + 1;
            }
            else if ((current.Kind == AsnTokenKind.RightBrace
                || current.Kind == AsnTokenKind.RightParenthesis
                || current.Kind == AsnTokenKind.RightBracket) && depth > 0)
            {
                depth = depth - 1;
            }

            this.Position = this.Position + 1;
        }

        AsnToken[] skipped = this.CopyTokenRange(recoveryStart, this.Position);
        recoveries.Add(new ParsedRecoverySyntax
        {
            Boundary = "object syntax",
            SkippedTokens = skipped,
            Span = skipped.Length == 0
                ? null
                : this.CreateSpan(skipped[0], skipped[skipped.Length - 1])
        });
    }

    internal void RecoverDefinedObjectSyntax(
        int recoveryStart,
        ParsedInformationObjectSyntax syntax,
        System.Collections.Generic.List<ParsedObjectFieldSettingSyntax> settings)
    {
        System.Collections.Generic.List<ParsedRecoverySyntax> recoveries =
            new System.Collections.Generic.List<ParsedRecoverySyntax>();
        this.RecoverObjectSyntax(recoveryStart, recoveries);
        if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            syntax.DefinedSyntaxTokens = this.CopyTokenRange(recoveryStart, this.Position);
            this.Position = this.Position + 1;
        }

        syntax.FieldSettingSyntax = settings.ToArray();
        syntax.Object.FieldSettings = this.CopyPublicSettings(settings);
        syntax.Recoveries = recoveries.ToArray();
    }

    internal bool ParseDefinedTemplateItems(
        ParsedSyntaxTemplateItem[] items,
        ParsedObjectClassSyntax governingClass,
        System.Collections.Generic.List<ParsedObjectFieldSettingSyntax> settings)
    {
        int index = 0;
        while (index < items.Length)
        {
            ParsedSyntaxTemplateItem item = items[index];
            if (item.OptionalGroup)
            {
                int savedPosition = this.Position;
                int savedCount = settings.Count;
                bool savedReporting = this.ReportDiagnostics;
                this.ReportDiagnostics = false;
                bool present = this.ParseDefinedTemplateItems(
                    item.Children,
                    governingClass,
                    settings);
                this.ReportDiagnostics = savedReporting;
                if (!present)
                {
                    this.Position = savedPosition;
                    while (settings.Count > savedCount)
                    {
                        settings.RemoveAt(settings.Count - 1);
                    }
                }

                index = index + 1;
                continue;
            }

            if (!item.FieldReference)
            {
                AsnToken? literal = this.PeekToken();
                if (literal == null || literal.Text != item.Text)
                {
                    if (this.ReportDiagnostics)
                    {
                        this.AddDiagnostic(183, "Expected literal '" + item.Text + "' in defined object syntax.", literal);
                    }

                    return false;
                }

                this.Position = this.Position + 1;
                index = index + 1;
                continue;
            }

            AsnObjectClassField? field = this.FindClassField(governingClass, item.Text);
            if (field == null)
            {
                this.AddDiagnostic(184, "The WITH SYNTAX field is not declared by its object class.", this.PeekToken());
                return false;
            }

            ParsedObjectFieldSettingSyntax? setting = this.ParseFieldSetting(item.Text, field);
            if (setting == null)
            {
                return false;
            }

            settings.Add(setting);
            index = index + 1;
        }

        return true;
    }

    internal ParsedObjectFieldSettingSyntax? ParseFieldSetting(
        string fieldName,
        AsnObjectClassField? field)
    {
        int start = this.Position;
        if (field == null)
        {
            this.ReadUnknownSetting();
            if (this.Position == start)
            {
                this.AddDiagnostic(185, "Expected a field setting.", this.PeekToken());
                return null;
            }

            return new ParsedObjectFieldSettingSyntax
            {
                Setting = new AsnObjectFieldSetting { FieldName = fieldName },
                SourceTokens = this.CopyTokenRange(start, this.Position),
                Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
            };
        }

        ParsedObjectFieldSettingSyntax syntax = new ParsedObjectFieldSettingSyntax
        {
            Kind = field.Kind,
            Setting = new AsnObjectFieldSetting { FieldName = fieldName }
        };
        bool success;
        if (field.Kind == AsnObjectFieldKind.Type)
        {
            success = this.ParseTypeSetting(syntax);
        }
        else if (field.Kind == AsnObjectFieldKind.FixedTypeValue
            || field.Kind == AsnObjectFieldKind.VariableTypeValue)
        {
            success = this.ParseValueSetting(syntax, field.Type);
        }
        else if (field.Kind == AsnObjectFieldKind.FixedTypeValueSet
            || field.Kind == AsnObjectFieldKind.VariableTypeValueSet)
        {
            success = this.ParseValueSetSetting(syntax);
        }
        else if (field.Kind == AsnObjectFieldKind.Object)
        {
            success = this.ParseObjectSetting(syntax);
        }
        else if (field.Kind == AsnObjectFieldKind.ObjectSet)
        {
            success = this.ParseObjectSetSetting(syntax);
        }
        else
        {
            success = false;
        }

        if (!success)
        {
            return null;
        }

        syntax.SourceTokens = this.CopyTokenRange(start, this.Position);
        syntax.Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1));
        return syntax;
    }

    internal bool ParseTypeSetting(ParsedObjectFieldSettingSyntax syntax)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        TypeParseResult parsed = new TypeParser(
            remaining,
            this.Diagnostics,
            this.ReportDiagnostics).ParseTypeSyntax();
        if (!parsed.Success || parsed.Syntax == null || parsed.ConsumedTokenCount == 0)
        {
            this.AddDiagnostic(186, "Expected a type field setting.", this.PeekToken());
            return false;
        }

        this.Position = this.Position + parsed.ConsumedTokenCount;
        syntax.TypeSyntax = parsed.Syntax;
        syntax.InformationFromObjects = parsed.Syntax.InformationFromObjects;
        syntax.Setting.Type = parsed.Syntax.Type;
        return true;
    }

    internal bool ParseValueSetting(ParsedObjectFieldSettingSyntax syntax, AsnType? expectedType)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ValueParseResult parsed = new ValueParser(
            remaining,
            this.Diagnostics,
            this.ReportDiagnostics).ParseValueSyntax(expectedType);
        if (!parsed.Success || parsed.Syntax == null || parsed.ConsumedTokenCount == 0)
        {
            this.AddDiagnostic(187, "Expected a value field setting.", this.PeekToken());
            return false;
        }

        this.Position = this.Position + parsed.ConsumedTokenCount;
        syntax.ValueSyntax = parsed.Syntax;
        syntax.InformationFromObjects = parsed.Syntax.InformationFromObjects;
        syntax.Setting.Value = parsed.Syntax.Value;
        return true;
    }

    internal bool ParseValueSetSetting(ParsedObjectFieldSettingSyntax syntax)
    {
        AsnToken[] parameterizedTokens = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ParameterizedReferenceParseResult parameterized = new ParameterizedParser(
            parameterizedTokens,
            this.Diagnostics,
            false).ParseParameterizedReferenceSyntax(AsnParameterKind.ValueSet, null);
        if (parameterized.Success && parameterized.Syntax != null)
        {
            this.Position = this.Position + parameterized.ConsumedTokenCount;
            syntax.ParameterizedReference = parameterized.Syntax;
            return true;
        }

        if (this.IsInformationFromObjectsStart(AsnAssignmentKind.ValueSet))
        {
            InformationFromObjectsParseResult selected = this.ParseSelectedInformation(
                AsnAssignmentKind.ValueSet);
            if (!selected.Success || selected.Syntax == null)
            {
                return false;
            }

            syntax.InformationFromObjects = selected.Syntax;
            return true;
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            this.AddDiagnostic(188, "Expected a brace-delimited value-set setting.", this.PeekToken());
            return false;
        }

        int start = this.Position;
        if (!this.ReadBalancedBraceGroup())
        {
            this.AddDiagnostic(189, "The value-set setting is missing its closing brace.", this.PeekTokenAt(-1));
            return false;
        }

        AsnToken[] raw = this.CopyTokenRange(start, this.Position);
        if (raw.Length > 2)
        {
            AsnToken[] constraintTokens = this.CreateConstraintTokens(raw);
            ConstraintParseResult parsed = new ConstraintParser(
                constraintTokens,
                this.Diagnostics,
                false).ParseConstraintSyntax(new AsnType { Kind = AsnTypeKind.Defined });
            if (parsed.Success && parsed.ConsumedTokenCount == constraintTokens.Length)
            {
                syntax.ValueSetSyntax = parsed.Syntax;
            }
        }

        return true;
    }

    internal bool ParseObjectSetting(ParsedObjectFieldSettingSyntax syntax)
    {
        InformationObjectParseResult parsed = this.ParseObjectSyntax(null);
        if (!parsed.Success || parsed.Syntax == null)
        {
            return false;
        }

        syntax.ObjectSyntax = parsed.Syntax;
        syntax.InformationFromObjects = parsed.Syntax.InformationFromObjects;
        syntax.Setting.Object = parsed.Syntax.Object;
        return true;
    }

    internal bool ParseObjectSetSetting(ParsedObjectFieldSettingSyntax syntax)
    {
        if (this.IsInformationFromObjectsStart(AsnAssignmentKind.ObjectSet))
        {
            InformationFromObjectsParseResult selected = this.ParseSelectedInformation(
                AsnAssignmentKind.ObjectSet);
            if (!selected.Success || selected.Syntax == null)
            {
                return false;
            }

            syntax.InformationFromObjects = selected.Syntax;
            syntax.Setting.ObjectSet = new AsnInformationObjectSet
            {
                Name = this.JoinTokenText(selected.Syntax.SourceTokens)
            };
            return true;
        }

        InformationObjectSetParseResult parsed = this.ParseObjectSetSyntax(null);
        if (!parsed.Success || parsed.Syntax == null)
        {
            return false;
        }

        syntax.ObjectSetSyntax = parsed.Syntax;
        syntax.Setting.ObjectSet = parsed.Syntax.ObjectSet;
        return true;
    }

    internal InformationObjectSetParseResult ParseObjectSetSyntax(
        ParsedObjectClassSyntax? governingClass)
    {
        int start = this.Position;
        if (this.IsInformationFromObjectsStart(AsnAssignmentKind.ObjectSet))
        {
            InformationFromObjectsParseResult selected = this.ParseSelectedInformation(
                AsnAssignmentKind.ObjectSet);
            if (!selected.Success || selected.Syntax == null)
            {
                return this.CreateObjectSetResult(false, null, start);
            }

            ParsedInformationObjectSetSyntax selectedSyntax =
                new ParsedInformationObjectSetSyntax
                {
                    ObjectSet = new AsnInformationObjectSet
                    {
                        Name = this.JoinTokenText(selected.Syntax.SourceTokens),
                        ObjectClass = governingClass == null
                            ? null
                            : governingClass.ObjectClass
                    },
                    InformationFromObjects = selected.Syntax,
                    SourceTokens = this.CopyTokenRange(start, this.Position),
                    Span = selected.Syntax.Span
                };
            return this.CreateObjectSetResult(true, selectedSyntax, start);
        }

        AsnToken[] parameterizedTokens = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ParameterizedReferenceParseResult parameterized = new ParameterizedParser(
            parameterizedTokens,
            this.Diagnostics,
            false).ParseParameterizedReferenceSyntax(AsnParameterKind.ObjectSet, null);
        if (parameterized.Success && parameterized.Syntax != null)
        {
            this.Position = this.Position + parameterized.ConsumedTokenCount;
            string name = parameterized.Syntax.ReferenceModule.Length == 0
                ? parameterized.Syntax.ReferenceName
                : parameterized.Syntax.ReferenceModule + "." + parameterized.Syntax.ReferenceName;
            ParsedInformationObjectSetSyntax parameterizedSyntax =
                new ParsedInformationObjectSetSyntax
                {
                    ObjectSet = new AsnInformationObjectSet
                    {
                        Name = name,
                        ObjectClass = governingClass == null ? null : governingClass.ObjectClass
                    },
                    ParameterizedReference = parameterized.Syntax,
                    SourceTokens = this.CopyTokenRange(start, this.Position),
                    Span = parameterized.Syntax.Span
                };
            return this.CreateObjectSetResult(true, parameterizedSyntax, start);
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            this.AddDiagnostic(190, "Expected '{' to begin an information object set.", this.PeekToken());
            return this.CreateObjectSetResult(false, null, start);
        }

        this.Position = this.Position + 1;
        ParsedInformationObjectSetSyntax syntax = new ParsedInformationObjectSetSyntax
        {
            ObjectSet = new AsnInformationObjectSet
            {
                ObjectClass = governingClass == null ? null : governingClass.ObjectClass
            }
        };
        if (this.IsKind(this.PeekToken(), AsnTokenKind.Ellipsis))
        {
            syntax.ObjectSet.Extensible = true;
            this.Position = this.Position + 1;
            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                this.Position = this.Position + 1;
                syntax.AdditionalElement = this.ParseObjectSetUnion(governingClass);
                if (syntax.AdditionalElement == null)
                {
                    this.AddDiagnostic(191, "Expected an additional object-set element after the extension marker.", this.PeekToken());
                    return this.CreateObjectSetResult(false, syntax, start);
                }
            }
        }
        else
        {
            syntax.RootElement = this.ParseObjectSetUnion(governingClass);
            if (syntax.RootElement == null)
            {
                this.AddDiagnostic(192, "Expected an object-set element expression.", this.PeekToken());
                return this.CreateObjectSetResult(false, syntax, start);
            }

            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                this.Position = this.Position + 1;
                if (!this.IsKind(this.PeekToken(), AsnTokenKind.Ellipsis))
                {
                    this.AddDiagnostic(193, "Expected an extension marker after ','.", this.PeekToken());
                    return this.CreateObjectSetResult(false, syntax, start);
                }

                syntax.ObjectSet.Extensible = true;
                this.Position = this.Position + 1;
                if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
                {
                    this.Position = this.Position + 1;
                    syntax.AdditionalElement = this.ParseObjectSetUnion(governingClass);
                    if (syntax.AdditionalElement == null)
                    {
                        this.AddDiagnostic(191, "Expected an additional object-set element after the extension marker.", this.PeekToken());
                        return this.CreateObjectSetResult(false, syntax, start);
                    }
                }
            }
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(194, "The information object set is missing its closing brace.", this.PeekToken());
            return this.CreateObjectSetResult(false, syntax, start);
        }

        this.Position = this.Position + 1;
        System.Collections.Generic.List<AsnInformationObject> rootObjects =
            new System.Collections.Generic.List<AsnInformationObject>();
        System.Collections.Generic.List<AsnInformationObject> additionalObjects =
            new System.Collections.Generic.List<AsnInformationObject>();
        this.CollectObjects(syntax.RootElement, rootObjects);
        this.CollectObjects(syntax.AdditionalElement, additionalObjects);
        syntax.ObjectSet.RootObjects = rootObjects.ToArray();
        syntax.ObjectSet.AdditionalObjects = additionalObjects.ToArray();
        rootObjects.AddRange(additionalObjects);
        syntax.ObjectSet.Objects = rootObjects.ToArray();
        syntax.SourceTokens = this.CopyTokenRange(start, this.Position);
        syntax.Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1));
        return this.CreateObjectSetResult(true, syntax, start);
    }

    internal ParsedObjectSetElementSyntax? ParseObjectSetUnion(
        ParsedObjectClassSyntax? governingClass)
    {
        int start = this.Position;
        ParsedObjectSetElementSyntax? left = this.ParseObjectSetIntersection(governingClass);
        if (left == null)
        {
            return null;
        }

        while (this.IsUnionOperator(this.PeekToken()))
        {
            this.Position = this.Position + 1;
            ParsedObjectSetElementSyntax? right = this.ParseObjectSetIntersection(governingClass);
            if (right == null)
            {
                return null;
            }

            left = this.CreateSetOperation("Union", left, right, start);
        }

        return left;
    }

    internal ParsedObjectSetElementSyntax? ParseObjectSetIntersection(
        ParsedObjectClassSyntax? governingClass)
    {
        int start = this.Position;
        ParsedObjectSetElementSyntax? left = this.ParseObjectSetExclusion(governingClass);
        if (left == null)
        {
            return null;
        }

        while (this.IsIntersectionOperator(this.PeekToken()))
        {
            this.Position = this.Position + 1;
            ParsedObjectSetElementSyntax? right = this.ParseObjectSetExclusion(governingClass);
            if (right == null)
            {
                return null;
            }

            left = this.CreateSetOperation("Intersection", left, right, start);
        }

        return left;
    }

    internal ParsedObjectSetElementSyntax? ParseObjectSetExclusion(
        ParsedObjectClassSyntax? governingClass)
    {
        int start = this.Position;
        ParsedObjectSetElementSyntax? left = this.ParseObjectSetOperand(governingClass);
        if (left == null)
        {
            return null;
        }

        if (this.PeekToken() != null && this.PeekToken()?.Text == "EXCEPT")
        {
            this.Position = this.Position + 1;
            ParsedObjectSetElementSyntax? right = this.ParseObjectSetOperand(governingClass);
            if (right == null)
            {
                return null;
            }

            return this.CreateSetOperation("Except", left, right, start);
        }

        return left;
    }

    internal ParsedObjectSetElementSyntax? ParseObjectSetOperand(
        ParsedObjectClassSyntax? governingClass)
    {
        int start = this.Position;
        AsnToken? current = this.PeekToken();
        if (current == null)
        {
            return null;
        }

        AsnToken[] parameterizedTokens = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ParameterizedReferenceParseResult parameterized = new ParameterizedParser(
            parameterizedTokens,
            this.Diagnostics,
            false).ParseParameterizedReferenceSyntax(AsnParameterKind.ObjectSet, null);
        if (parameterized.Success && parameterized.Syntax != null)
        {
            this.Position = this.Position + parameterized.ConsumedTokenCount;
            return new ParsedObjectSetElementSyntax
            {
                Kind = "ParameterizedObjectSet",
                ReferenceModule = parameterized.Syntax.ReferenceModule,
                ReferenceName = parameterized.Syntax.ReferenceName,
                ParameterizedReference = parameterized.Syntax,
                SourceTokens = this.CopyTokenRange(start, this.Position),
                Span = parameterized.Syntax.Span
            };
        }

        if (current.Text == "ALL" && this.PeekTokenAt(1)?.Text == "EXCEPT")
        {
            this.Position = this.Position + 2;
            ParsedObjectSetElementSyntax? operand = this.ParseObjectSetOperand(governingClass);
            if (operand == null)
            {
                return null;
            }

            return new ParsedObjectSetElementSyntax
            {
                Kind = "AllExcept",
                Children = [operand],
                SourceTokens = this.CopyTokenRange(start, this.Position),
                Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
            };
        }

        if (current.Kind == AsnTokenKind.LeftParenthesis)
        {
            this.Position = this.Position + 1;
            ParsedObjectSetElementSyntax? grouped = this.ParseObjectSetUnion(governingClass);
            if (grouped == null || !this.IsKind(this.PeekToken(), AsnTokenKind.RightParenthesis))
            {
                return null;
            }

            this.Position = this.Position + 1;
            grouped.SourceTokens = this.CopyTokenRange(start, this.Position);
            grouped.Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1));
            return grouped;
        }

        if (this.IsInformationFromObjectsStart(AsnAssignmentKind.ObjectSet))
        {
            InformationFromObjectsParseResult selected = this.ParseSelectedInformation(
                AsnAssignmentKind.ObjectSet);
            if (!selected.Success || selected.Syntax == null)
            {
                return null;
            }

            return new ParsedObjectSetElementSyntax
            {
                Kind = "InformationFromObjects",
                InformationFromObjects = selected.Syntax,
                SourceTokens = this.CopyTokenRange(start, this.Position),
                Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
            };
        }

        if (current.Kind == AsnTokenKind.LeftBrace || this.IsLowerReference(current))
        {
            InformationObjectParseResult objectResult = this.ParseObjectSyntax(governingClass);
            if (!objectResult.Success || objectResult.Syntax == null)
            {
                return null;
            }

            return new ParsedObjectSetElementSyntax
            {
                Kind = "Object",
                ObjectSyntax = objectResult.Syntax,
                SourceTokens = this.CopyTokenRange(start, this.Position),
                Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
            };
        }

        if (!this.IsUpperReference(current))
        {
            return null;
        }

        this.Position = this.Position + 1;
        string module = "";
        AsnToken reference = current;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.Dot)
            && this.IsUpperReference(this.PeekTokenAt(1)))
        {
            module = reference.Text;
            reference = this.PeekTokenAt(1) ?? reference;
            this.Position = this.Position + 2;
        }

        return new ParsedObjectSetElementSyntax
        {
            Kind = "ObjectSetReference",
            ReferenceModule = module,
            ReferenceName = reference.Text,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
    }

    internal ParsedObjectSetElementSyntax CreateSetOperation(
        string kind,
        ParsedObjectSetElementSyntax left,
        ParsedObjectSetElementSyntax right,
        int start)
    {
        return new ParsedObjectSetElementSyntax
        {
            Kind = kind,
            Children = [left, right],
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
    }

    internal InformationObjectAssignmentParseResult ParseObjectAssignmentSyntax(
        ParsedObjectClassSyntax? resolvedGovernor)
    {
        int start = this.Position;
        AsnToken? name = this.PeekToken();
        if (!this.IsLowerReference(name))
        {
            this.AddDiagnostic(195, "Expected an object reference to begin the assignment.", name);
            return this.CreateAssignmentResult(false, null, start);
        }

        this.Position = this.Position + 1;
        ParsedObjectClassSyntax? governor = this.ParseAssignmentGovernor();
        if (governor == null)
        {
            return this.CreateAssignmentResult(false, null, start);
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.Assignment))
        {
            this.AddDiagnostic(196, "Expected '::=' after the object assignment governor.", this.PeekToken());
            return this.CreateAssignmentResult(false, null, start);
        }

        this.Position = this.Position + 1;
        ParsedObjectClassSyntax effective = resolvedGovernor == null ? governor : resolvedGovernor;
        InformationObjectParseResult value = this.ParseObjectSyntax(effective);
        if (!value.Success || value.Syntax == null)
        {
            return this.CreateAssignmentResult(false, null, start);
        }

        AsnAssignment assignment = new AsnAssignment
        {
            Name = name == null ? "" : name.Text,
            Kind = AsnAssignmentKind.Object,
            ObjectClass = effective.ObjectClass,
            Object = value.Syntax.Object,
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        ParsedInformationObjectAssignmentSyntax syntax = new ParsedInformationObjectAssignmentSyntax
        {
            Assignment = assignment,
            GovernorSyntax = governor,
            ObjectSyntax = value.Syntax,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = assignment.Span
        };
        return this.CreateAssignmentResult(true, syntax, start);
    }

    internal InformationObjectAssignmentParseResult ParseObjectSetAssignmentSyntax(
        ParsedObjectClassSyntax? resolvedGovernor)
    {
        int start = this.Position;
        AsnToken? name = this.PeekToken();
        if (!this.IsUpperReference(name))
        {
            this.AddDiagnostic(197, "Expected an object-set reference to begin the assignment.", name);
            return this.CreateAssignmentResult(false, null, start);
        }

        this.Position = this.Position + 1;
        ParsedObjectClassSyntax? governor = this.ParseAssignmentGovernor();
        if (governor == null)
        {
            return this.CreateAssignmentResult(false, null, start);
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.Assignment))
        {
            this.AddDiagnostic(198, "Expected '::=' after the object-set assignment governor.", this.PeekToken());
            return this.CreateAssignmentResult(false, null, start);
        }

        this.Position = this.Position + 1;
        ParsedObjectClassSyntax effective = resolvedGovernor == null ? governor : resolvedGovernor;
        InformationObjectSetParseResult value = this.ParseObjectSetSyntax(effective);
        if (!value.Success || value.Syntax == null)
        {
            return this.CreateAssignmentResult(false, null, start);
        }

        AsnAssignment assignment = new AsnAssignment
        {
            Name = name == null ? "" : name.Text,
            Kind = AsnAssignmentKind.ObjectSet,
            ObjectClass = effective.ObjectClass,
            ObjectSet = value.Syntax.ObjectSet,
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        ParsedInformationObjectAssignmentSyntax syntax = new ParsedInformationObjectAssignmentSyntax
        {
            Assignment = assignment,
            GovernorSyntax = governor,
            ObjectSetSyntax = value.Syntax,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = assignment.Span
        };
        return this.CreateAssignmentResult(true, syntax, start);
    }

    internal ParsedObjectClassSyntax? ParseAssignmentGovernor()
    {
        int start = this.Position;
        int end = start;
        while (end < this.Tokens.Length
            && this.Tokens[end].Kind != AsnTokenKind.Assignment)
        {
            end = end + 1;
        }

        if (end == start)
        {
            this.AddDiagnostic(199, "Expected a defined object-class governor.", this.PeekToken());
            return null;
        }

        AsnToken[] governorTokens = this.CopyTokenRange(start, end);
        ObjectClassParseResult parsed = new ObjectClassParser(
            governorTokens,
            this.Diagnostics,
            false).ParseObjectClassSyntax();
        if (!parsed.Success || parsed.Syntax == null
            || parsed.ConsumedTokenCount != governorTokens.Length)
        {
            this.AddDiagnostic(199, "Expected a defined object-class governor.", this.PeekToken());
            return null;
        }

        this.Position = end;
        return parsed.Syntax;
    }

    internal InformationFromObjectsParseResult ParseSelectedInformation(
        AsnAssignmentKind expectedKind)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        InformationFromObjectsParseResult parsed = new InformationFromObjectsParser(
            remaining,
            this.Diagnostics,
            this.ReportDiagnostics).ParseInformationFromObjectsSyntax(expectedKind);
        this.Position = this.Position + parsed.ConsumedTokenCount;
        return parsed;
    }

    internal bool IsInformationFromObjectsStart(AsnAssignmentKind expectedKind)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        InformationFromObjectsParseResult parsed = new InformationFromObjectsParser(
            remaining,
            this.Diagnostics,
            false).ParseInformationFromObjectsSyntax(expectedKind);
        return parsed.Success;
    }

    internal AsnObjectClassField? FindClassField(
        ParsedObjectClassSyntax? governingClass,
        string fieldName)
    {
        if (governingClass == null)
        {
            return null;
        }

        int index = 0;
        while (index < governingClass.ObjectClass.Fields.Length)
        {
            if (governingClass.ObjectClass.Fields[index].Name == fieldName)
            {
                return governingClass.ObjectClass.Fields[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnObjectFieldSetting[] CopyPublicSettings(
        System.Collections.Generic.List<ParsedObjectFieldSettingSyntax> settings)
    {
        AsnObjectFieldSetting[] result = new AsnObjectFieldSetting[settings.Count];
        int index = 0;
        while (index < settings.Count)
        {
            result[index] = settings[index].Setting;
            index = index + 1;
        }

        return result;
    }

    internal void CollectObjects(
        ParsedObjectSetElementSyntax? element,
        System.Collections.Generic.List<AsnInformationObject> objects)
    {
        if (element == null)
        {
            return;
        }

        if (element.Kind == "Object" && element.ObjectSyntax != null)
        {
            objects.Add(element.ObjectSyntax.Object);
        }

        int index = 0;
        while (index < element.Children.Length)
        {
            this.CollectObjects(element.Children[index], objects);
            index = index + 1;
        }
    }

    internal void ReadUnknownSetting()
    {
        int braces = 0;
        int parentheses = 0;
        while (this.PeekToken() != null)
        {
            AsnToken current = this.PeekToken() ?? new AsnToken();
            if (braces == 0 && parentheses == 0
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

            this.Position = this.Position + 1;
        }
    }

    internal bool ReadBalancedBraceGroup()
    {
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            return false;
        }

        int depth = 0;
        while (this.PeekToken() != null)
        {
            AsnToken current = this.ReadRequiredToken();
            if (current.Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (current.Kind == AsnTokenKind.RightBrace)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal AsnToken[] CreateConstraintTokens(AsnToken[] valueSetTokens)
    {
        AsnToken[] result = new AsnToken[valueSetTokens.Length];
        int index = 0;
        while (index < valueSetTokens.Length)
        {
            AsnToken source = valueSetTokens[index];
            if (index == 0 || index == valueSetTokens.Length - 1)
            {
                result[index] = new AsnToken
                {
                    Kind = index == 0
                        ? AsnTokenKind.LeftParenthesis
                        : AsnTokenKind.RightParenthesis,
                    Text = index == 0 ? "(" : ")",
                    Span = source.Span
                };
            }
            else
            {
                result[index] = source;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool IsUnionOperator(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.VerticalBar || token.Text == "UNION");
    }

    internal bool IsIntersectionOperator(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.Caret || token.Text == "INTERSECTION");
    }

    internal bool IsLowerReference(AsnToken? token)
    {
        return token != null
            && token.Text.Length > 0
            && token.Text[0] >= 'a' && token.Text[0] <= 'z';
    }

    internal bool IsUpperReference(AsnToken? token)
    {
        return token != null
            && token.Text.Length > 0
            && token.Text[0] >= 'A' && token.Text[0] <= 'Z'
            && token.Kind != AsnTokenKind.ReservedWord;
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

    internal InformationObjectParseResult CreateObjectResult(
        bool success,
        ParsedInformationObjectSyntax? syntax,
        int start)
    {
        return new InformationObjectParseResult
        {
            Success = success,
            Syntax = syntax,
            ConsumedTokenCount = this.Position - start
        };
    }

    internal InformationObjectSetParseResult CreateObjectSetResult(
        bool success,
        ParsedInformationObjectSetSyntax? syntax,
        int start)
    {
        if (!success && syntax != null
            && this.IsKind(this.TokenAt(start), AsnTokenKind.LeftBrace))
        {
            int recoveryStart = this.Position;
            int depth = 0;
            while (this.PeekToken() != null)
            {
                AsnToken current = this.ReadRequiredToken();
                if (current.Kind == AsnTokenKind.LeftBrace)
                {
                    depth = depth + 1;
                }
                else if (current.Kind == AsnTokenKind.RightBrace)
                {
                    if (depth == 0)
                    {
                        break;
                    }

                    depth = depth - 1;
                }
            }

            AsnToken[] skipped = this.CopyTokenRange(recoveryStart, this.Position);
            syntax.Recoveries = [new ParsedRecoverySyntax
            {
                Boundary = "object syntax",
                SkippedTokens = skipped,
                Span = skipped.Length == 0
                    ? null
                    : this.CreateSpan(skipped[0], skipped[skipped.Length - 1])
            }];
            System.Collections.Generic.List<AsnInformationObject> rootObjects =
                new System.Collections.Generic.List<AsnInformationObject>();
            System.Collections.Generic.List<AsnInformationObject> additionalObjects =
                new System.Collections.Generic.List<AsnInformationObject>();
            this.CollectObjects(syntax.RootElement, rootObjects);
            this.CollectObjects(syntax.AdditionalElement, additionalObjects);
            syntax.ObjectSet.RootObjects = rootObjects.ToArray();
            syntax.ObjectSet.AdditionalObjects = additionalObjects.ToArray();
            rootObjects.AddRange(additionalObjects);
            syntax.ObjectSet.Objects = rootObjects.ToArray();
            syntax.SourceTokens = this.CopyTokenRange(start, this.Position);
            syntax.Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1));
        }

        return new InformationObjectSetParseResult
        {
            Success = success,
            Syntax = syntax,
            ConsumedTokenCount = this.Position - start
        };
    }

    internal InformationObjectAssignmentParseResult CreateAssignmentResult(
        bool success,
        ParsedInformationObjectAssignmentSyntax? syntax,
        int start)
    {
        return new InformationObjectAssignmentParseResult
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

    internal string JoinTokenText(AsnToken[] tokens)
    {
        string result = "";
        int index = 0;
        while (index < tokens.Length)
        {
            result = index == 0 ? tokens[index].Text : result + " " + tokens[index].Text;
            index = index + 1;
        }

        return result;
    }

    internal AsnSourceSpan? CreateSpan(AsnToken? first, AsnToken? last)
    {
        if (first == null || first.Span == null || last == null || last.Span == null)
        {
            return null;
        }

        return new AsnSourceSpan { Start = first.Span.Start, End = last.Span.End };
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
