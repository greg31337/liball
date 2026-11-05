namespace asn1;

internal class EcnEncodingStructureParser
{
    internal DiagnosticBag Diagnostics;
    internal AsnToken[] Tokens = [];
    internal int Position;
    internal EcnSymbolTable Table = new EcnSymbolTable();
    internal EcnSymbol Owner = new EcnSymbol();
    internal bool Success = true;

    internal EcnEncodingStructureParser(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal EcnEncodingClassDefinition ParseClassDefinition(
        EcnSymbol symbol,
        EcnSymbolTable table)
    {
        EcnAssignmentSyntax? assignment = symbol.Assignment;
        EcnEncodingClassDefinition definition =
            new EcnEncodingClassDefinition
            {
                Name = symbol.Name,
                Symbol = symbol,
                Span = symbol.Span
            };
        if (assignment == null)
        {
            definition.Valid = false;
            return definition;
        }

        this.Tokens = assignment.BodyTokens;
        this.Position = 0;
        this.Table = table;
        this.Owner = symbol;
        this.Success = true;
        if (symbol.Kind == EcnSymbolKind.EncodingClass)
        {
            definition.Kind = EcnEncodingClassDefinitionKind.Alias;
            EcnResolvedClassReference reference = this.ReadClassReference();
            definition.AliasClassReference = reference.Name;
            definition.AliasSymbol = reference.Symbol;
            definition.AliasBuiltIn = reference.BuiltIn;
            definition.AliasActualParameters = reference.ActualParameters;
            if (!reference.Success || this.Position != this.Tokens.Length)
            {
                this.AddInvalidDefinitionDiagnostic(
                    "An encoding class alias must contain exactly one class reference.",
                    this.CurrentSpan());
            }
            else if (reference.Category != EcnClassCategory.Unknown
                && !EcnClassCategoryCompatibility.CanDeriveAlias(
                    reference.Category))
            {
                this.AddCategoryDiagnostic(
                    reference.Name,
                    "a derivable encoding class category",
                    reference.Span);
            }
        }
        else
        {
            definition.Kind = EcnEncodingClassDefinitionKind.Structure;
            definition.Structure = this.ParseStructure();
            if (definition.Structure == null
                || this.Position != this.Tokens.Length)
            {
                this.AddInvalidDefinitionDiagnostic(
                    "The encoding structure definition contains unconsumed or invalid notation.",
                    this.CurrentSpan());
            }
        }

        definition.Valid = this.Success;
        return definition;
    }

    internal EcnEncodingStructure? ParseStructure()
    {
        if (this.Position >= this.Tokens.Length)
        {
            this.AddInvalidDefinitionDiagnostic(
                "An encoding structure is required.",
                this.CurrentSpan());
            return null;
        }

        int start = this.Position;
        if (this.Tokens[this.Position].Kind == AsnTokenKind.LeftBracket)
        {
            return this.ParseTaggedStructure(start);
        }

        EcnResolvedClassReference classReference = this.ReadClassReference();
        if (!classReference.Success)
        {
            return null;
        }

        if (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Kind == AsnTokenKind.LeftBrace)
        {
            if (classReference.Category == EcnClassCategory.Alternatives)
            {
                return this.ParseAlternativesStructure(
                    start,
                    classReference);
            }

            if (classReference.Category == EcnClassCategory.Repetition)
            {
                return this.ParseRepetitionStructure(
                    start,
                    classReference);
            }

            if (classReference.Category == EcnClassCategory.Concatenation)
            {
                return this.ParseConcatenationStructure(
                    start,
                    classReference);
            }

            this.AddCategoryDiagnostic(
                classReference.Name,
                "the alternatives, repetition, or concatenation category",
                classReference.Span);
            this.SkipBalancedGroup();
            return this.CreateStructure(
                EcnEncodingStructureKind.Concatenation,
                classReference,
                start);
        }

        EcnEncodingStructure field = this.CreateStructure(
            EcnEncodingStructureKind.Field,
            classReference,
            start);
        int constraintStart = this.Position;
        while (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Kind == AsnTokenKind.LeftParenthesis)
        {
            this.SkipBalancedGroup();
        }

        field.ConstraintTokens = this.CopyTokens(
            constraintStart,
            this.Position - constraintStart);
        this.CompleteStructure(field, start);
        if (classReference.Category != EcnClassCategory.Unknown
            && !EcnClassCategoryCompatibility.IsGroupCompatible(
                classReference.Category,
                EcnClassCategoryGroup.BitField))
        {
            this.AddCategoryDiagnostic(
                classReference.Name,
                "the bit-field group of categories",
                classReference.Span);
        }

        return field;
    }

    internal EcnEncodingStructure ParseTaggedStructure(int start)
    {
        this.Position = this.Position + 1;
        EcnResolvedClassReference tagClass = this.ReadClassReference();
        EcnEncodingStructure tagged = this.CreateStructure(
            EcnEncodingStructureKind.Tagged,
            tagClass,
            start);
        if (tagClass.Category != EcnClassCategory.Unknown
            && !EcnClassCategoryCompatibility.IsCategoryCompatible(
                tagClass.Category,
                EcnClassCategory.Tag))
        {
            this.Diagnostics.TryAddDiagnostic(
                AsnDiagnosticSeverity.Error,
                AsnDiagnosticCategory.Ecn,
                EcnDiagnosticCodes.InvalidTagClass,
                "The tag class '" + tagClass.Name
                    + "' is not in the tag category.",
                tagClass.Span,
                null);
            this.Success = false;
        }

        int tagStart = this.Position;
        if (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Kind == AsnTokenKind.LeftParenthesis)
        {
            this.SkipBalancedGroup();
        }

        tagged.ConstraintTokens = this.CopyTokens(
            tagStart,
            this.Position - tagStart);
        if (this.Position >= this.Tokens.Length
            || this.Tokens[this.Position].Kind != AsnTokenKind.RightBracket)
        {
            this.AddInvalidDefinitionDiagnostic(
                "A tagged encoding structure requires a closing ']'.",
                this.CurrentSpan());
            this.CompleteStructure(tagged, start);
            return tagged;
        }

        this.Position = this.Position + 1;
        tagged.InnerStructure = this.ParseStructure();
        if (tagged.InnerStructure != null
            && tagged.InnerStructure.Kind == EcnEncodingStructureKind.Tagged)
        {
            this.Diagnostics.TryAddDiagnostic(
                AsnDiagnosticSeverity.Error,
                AsnDiagnosticCategory.Ecn,
                EcnDiagnosticCodes.InvalidTagClass,
                "A tag class cannot immediately follow another tag class.",
                tagged.InnerStructure.Span,
                null);
            this.Success = false;
        }

        this.CompleteStructure(tagged, start);
        return tagged;
    }

    internal EcnEncodingStructure ParseAlternativesStructure(
        int start,
        EcnResolvedClassReference classReference)
    {
        EcnEncodingStructure structure = this.CreateStructure(
            EcnEncodingStructureKind.Alternatives,
            classReference,
            start);
        structure.Fields = this.ParseNamedFields(false);
        if (structure.Fields.Length < 2)
        {
            this.AddInvalidDefinitionDiagnostic(
                "An alternatives structure requires at least two named fields.",
                structure.Span);
        }

        this.CompleteStructure(structure, start);
        return structure;
    }

    internal EcnEncodingStructure ParseRepetitionStructure(
        int start,
        EcnResolvedClassReference classReference)
    {
        EcnEncodingStructure structure = this.CreateStructure(
            EcnEncodingStructureKind.Repetition,
            classReference,
            start);
        this.Position = this.Position + 1;
        if (this.Position < this.Tokens.Length
            && this.IsFieldName(this.Tokens[this.Position]))
        {
            structure.ElementName = this.Tokens[this.Position].Text;
            this.Position = this.Position + 1;
        }

        structure.Element = this.ParseStructure();
        if (this.Position >= this.Tokens.Length
            || this.Tokens[this.Position].Kind != AsnTokenKind.RightBrace)
        {
            this.AddInvalidDefinitionDiagnostic(
                "A repetition structure requires exactly one encoding structure.",
                this.CurrentSpan());
        }
        else
        {
            this.Position = this.Position + 1;
        }

        int constraintStart = this.Position;
        while (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Kind == AsnTokenKind.LeftParenthesis)
        {
            this.SkipBalancedGroup();
        }

        structure.ConstraintTokens = this.CopyTokens(
            constraintStart,
            this.Position - constraintStart);
        this.CompleteStructure(structure, start);
        return structure;
    }

    internal EcnEncodingStructure ParseConcatenationStructure(
        int start,
        EcnResolvedClassReference classReference)
    {
        EcnEncodingStructure structure = this.CreateStructure(
            EcnEncodingStructureKind.Concatenation,
            classReference,
            start);
        structure.Fields = this.ParseNamedFields(true);
        if (structure.Fields.Length == 0)
        {
            this.AddInvalidDefinitionDiagnostic(
                "A concatenation structure requires at least one named field.",
                structure.Span);
        }

        this.CompleteStructure(structure, start);
        return structure;
    }

    internal EcnEncodingField[] ParseNamedFields(bool allowOptional)
    {
        System.Collections.Generic.List<EcnEncodingField> fields =
            new System.Collections.Generic.List<EcnEncodingField>();
        this.Position = this.Position + 1;
        while (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Kind != AsnTokenKind.RightBrace)
        {
            int start = this.Position;
            if (!this.IsFieldName(this.Tokens[this.Position]))
            {
                this.AddInvalidDefinitionDiagnostic(
                    "A structure component requires a field name.",
                    this.CurrentSpan());
                this.SkipToFieldBoundary();
            }
            else
            {
                EcnEncodingField field = new EcnEncodingField
                {
                    Name = this.Tokens[this.Position].Text
                };
                this.Position = this.Position + 1;
                field.Structure = this.ParseStructure();
                if (allowOptional
                    && this.Position < this.Tokens.Length
                    && this.Tokens[this.Position].Text == "OPTIONAL-ENCODING")
                {
                    this.Position = this.Position + 1;
                    EcnResolvedClassReference optionalClass =
                        this.ReadClassReference();
                    field.Optional = true;
                    field.OptionalClassReference = optionalClass.Name;
                    field.OptionalClassSymbol = optionalClass.Symbol;
                    if (optionalClass.Category != EcnClassCategory.Unknown
                        && !EcnClassCategoryCompatibility.IsCategoryCompatible(
                            optionalClass.Category,
                            EcnClassCategory.Optionality))
                    {
                        this.Diagnostics.TryAddDiagnostic(
                            AsnDiagnosticSeverity.Error,
                            AsnDiagnosticCategory.Ecn,
                            EcnDiagnosticCodes.InvalidOptionalityClass,
                            "The optionality class '" + optionalClass.Name
                                + "' is not in the optionality category.",
                            optionalClass.Span,
                            null);
                        this.Success = false;
                    }
                }

                field.Span = this.CreateSpan(start, this.Position);
                this.DiagnoseDuplicateField(fields, field);
                fields.Add(field);
            }

            if (this.Position < this.Tokens.Length
                && this.Tokens[this.Position].Kind == AsnTokenKind.Comma)
            {
                this.Position = this.Position + 1;
            }
            else if (this.Position < this.Tokens.Length
                && this.Tokens[this.Position].Kind != AsnTokenKind.RightBrace)
            {
                this.AddInvalidDefinitionDiagnostic(
                    "Structure fields must be separated by commas.",
                    this.CurrentSpan());
                this.SkipToFieldBoundary();
                if (this.Position < this.Tokens.Length
                    && this.Tokens[this.Position].Kind == AsnTokenKind.Comma)
                {
                    this.Position = this.Position + 1;
                }
            }
        }

        if (this.Position >= this.Tokens.Length)
        {
            this.AddInvalidDefinitionDiagnostic(
                "The structure field list is missing a closing '}'.",
                this.CurrentSpan());
        }
        else
        {
            this.Position = this.Position + 1;
        }

        return fields.ToArray();
    }

    internal EcnResolvedClassReference ReadClassReference()
    {
        int start = this.Position;
        EcnResolvedClassReference result = new EcnResolvedClassReference();
        if (this.Position >= this.Tokens.Length)
        {
            this.AddUnknownClassDiagnostic("", this.CurrentSpan());
            return result;
        }

        if (this.Position + 2 < this.Tokens.Length
            && this.Tokens[this.Position + 1].Kind == AsnTokenKind.Dot
            && this.Tokens[this.Position + 2].Kind
                == AsnTokenKind.EncodingClassReference)
        {
            result.Name = this.Tokens[this.Position].Text + "."
                + this.Tokens[this.Position + 2].Text;
            this.Position = this.Position + 3;
        }
        else if (this.Tokens[this.Position].Kind
            == AsnTokenKind.EncodingClassReference)
        {
            result.Name = this.Tokens[this.Position].Text;
            this.Position = this.Position + 1;
        }
        else
        {
            this.AddUnknownClassDiagnostic(
                this.Tokens[this.Position].Text,
                this.Tokens[this.Position].Span);
            this.Position = this.Position + 1;
            return result;
        }

        result.Span = this.CreateSpan(start, this.Position);
        this.ResolveClassReference(result);
        if (this.Position + 1 < this.Tokens.Length
            && this.Tokens[this.Position].Kind == AsnTokenKind.LeftBrace
            && this.Tokens[this.Position + 1].Kind == AsnTokenKind.LessThan)
        {
            EcnActualParameterBinder binder =
                new EcnActualParameterBinder(this.Diagnostics);
            result.ActualParameters = binder.BindActualParameters(
                this.Tokens,
                this.Position,
                this.Table,
                this.Owner,
                result.Symbol);
            this.Position = binder.EndPosition;
            if (!binder.Success)
            {
                this.Success = false;
            }
        }
        else
        {
            EcnActualParameterBinder binder =
                new EcnActualParameterBinder(this.Diagnostics);
            if (!binder.ValidateParameterPresence(result.Symbol, result.Span))
            {
                this.Success = false;
            }
        }

        result.Span = this.CreateSpan(start, this.Position);
        return result;
    }

    internal void ResolveClassReference(EcnResolvedClassReference reference)
    {
        int external = reference.Name.IndexOf(
            ".#",
            System.StringComparison.Ordinal);
        string moduleName = external < 0
            ? ""
            : reference.Name.Substring(0, external);
        string simple = external < 0
            ? reference.Name
            : reference.Name.Substring(external + 1);
        if (moduleName.Length == 0)
        {
            EcnBuiltInClassDefinition? builtIn =
                EcnBuiltInClassCatalog.FindClass(simple);
            if (builtIn != null)
            {
                reference.BuiltIn = true;
                reference.Category = builtIn.Category;
                reference.PrimitiveClassName = builtIn.PrimitiveName;
                reference.Success = true;
                return;
            }

            EcnScope? scope = this.Owner.Scope;
            if (scope != null)
            {
                EcnSymbol? parameter = scope.FindLocalSymbol(simple);
                if (parameter != null
                    && parameter.ParameterKind == EcnParameterKind.EncodingClass)
                {
                    reference.Symbol = parameter;
                    reference.Success = true;
                    return;
                }
            }
        }

        EcnSymbol? found = null;
        int matches = 0;
        int index = 0;
        while (index < this.Table.Symbols.Length)
        {
            EcnSymbol candidate = this.Table.Symbols[index];
            bool classKind = candidate.Kind == EcnSymbolKind.EncodingClass
                || candidate.Kind == EcnSymbolKind.EncodingStructure;
            bool moduleMatches = moduleName.Length == 0
                || candidate.SourceModuleName == moduleName;
            if (classKind && moduleMatches && candidate.Name == simple)
            {
                found = candidate;
                matches = matches + 1;
            }

            index = index + 1;
        }

        if (matches == 1 && found != null)
        {
            reference.Symbol = found;
            reference.Category = found.ClassCategory;
            reference.PrimitiveClassName = found.PrimitiveClassName;
            reference.Success = true;
            return;
        }

        this.AddUnknownClassDiagnostic(reference.Name, reference.Span);
    }

    internal EcnEncodingStructure CreateStructure(
        EcnEncodingStructureKind kind,
        EcnResolvedClassReference classReference,
        int start)
    {
        return new EcnEncodingStructure
        {
            Kind = kind,
            ClassReference = classReference.Name,
            PrimitiveClassName = classReference.PrimitiveClassName,
            ClassCategory = classReference.Category,
            ClassSymbol = classReference.Symbol,
            ActualParameters = classReference.ActualParameters,
            Span = this.CreateSpan(start, this.Position)
        };
    }

    internal void CompleteStructure(
        EcnEncodingStructure structure,
        int start)
    {
        structure.SourceTokens = this.CopyTokens(
            start,
            this.Position - start);
        structure.Span = this.CreateSpan(start, this.Position);
    }

    internal void DiagnoseDuplicateField(
        System.Collections.Generic.List<EcnEncodingField> fields,
        EcnEncodingField candidate)
    {
        int index = 0;
        while (index < fields.Count)
        {
            if (fields[index].Name == candidate.Name)
            {
                AsnSourceSpan? originalSpan = fields[index].Span;
                AsnDiagnosticRelatedLocation[] related = originalSpan == null
                    ? []
                    : [new AsnDiagnosticRelatedLocation
                    {
                        Message = "The first field declaration is here.",
                        Span = originalSpan
                    }];
                this.Diagnostics.TryAddDiagnostic(
                    AsnDiagnosticSeverity.Error,
                    AsnDiagnosticCategory.Ecn,
                    EcnDiagnosticCodes.DuplicateStructureField,
                    "The encoding structure field '" + candidate.Name
                        + "' is declared more than once.",
                    candidate.Span,
                    related);
                this.Success = false;
                return;
            }

            index = index + 1;
        }
    }

    internal bool IsFieldName(AsnToken token)
    {
        return token.Kind == AsnTokenKind.Identifier
            || token.Kind == AsnTokenKind.ValueReference
            || token.Kind == AsnTokenKind.ObjectReference;
    }

    internal void SkipBalancedGroup()
    {
        if (this.Position >= this.Tokens.Length
            || !this.IsOpening(this.Tokens[this.Position].Kind))
        {
            return;
        }

        System.Collections.Generic.List<AsnTokenKind> closings =
            new System.Collections.Generic.List<AsnTokenKind>();
        while (this.Position < this.Tokens.Length)
        {
            AsnTokenKind kind = this.Tokens[this.Position].Kind;
            if (this.IsOpening(kind))
            {
                closings.Add(this.ClosingKind(kind));
            }
            else if (this.IsClosing(kind))
            {
                if (closings.Count == 0
                    || closings[closings.Count - 1] != kind)
                {
                    this.AddInvalidDefinitionDiagnostic(
                        "The encoding structure contains mismatched delimiters.",
                        this.Tokens[this.Position].Span);
                    this.Position = this.Position + 1;
                    return;
                }

                closings.RemoveAt(closings.Count - 1);
                if (closings.Count == 0)
                {
                    this.Position = this.Position + 1;
                    return;
                }
            }

            this.Position = this.Position + 1;
        }

        this.AddInvalidDefinitionDiagnostic(
            "The encoding structure contains an unterminated delimiter group.",
            this.CurrentSpan());
    }

    internal void SkipToFieldBoundary()
    {
        while (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Kind != AsnTokenKind.Comma
            && this.Tokens[this.Position].Kind != AsnTokenKind.RightBrace)
        {
            if (this.IsOpening(this.Tokens[this.Position].Kind))
            {
                this.SkipBalancedGroup();
            }
            else
            {
                this.Position = this.Position + 1;
            }
        }
    }

    internal bool IsOpening(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.LeftBrace
            || kind == AsnTokenKind.LeftParenthesis
            || kind == AsnTokenKind.LeftBracket
            || kind == AsnTokenKind.LeftVersionBracket
            || kind == AsnTokenKind.LessThan;
    }

    internal bool IsClosing(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.RightBrace
            || kind == AsnTokenKind.RightParenthesis
            || kind == AsnTokenKind.RightBracket
            || kind == AsnTokenKind.RightVersionBracket
            || kind == AsnTokenKind.GreaterThan;
    }

    internal AsnTokenKind ClosingKind(AsnTokenKind kind)
    {
        if (kind == AsnTokenKind.LeftBrace)
        {
            return AsnTokenKind.RightBrace;
        }

        if (kind == AsnTokenKind.LeftParenthesis)
        {
            return AsnTokenKind.RightParenthesis;
        }

        if (kind == AsnTokenKind.LeftBracket)
        {
            return AsnTokenKind.RightBracket;
        }

        if (kind == AsnTokenKind.LeftVersionBracket)
        {
            return AsnTokenKind.RightVersionBracket;
        }

        return AsnTokenKind.GreaterThan;
    }

    internal AsnToken[] CopyTokens(int offset, int count)
    {
        AsnToken[] result = new AsnToken[count];
        int index = 0;
        while (index < count)
        {
            result[index] = this.Tokens[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal AsnSourceSpan? CreateSpan(int start, int end)
    {
        if (start < 0 || start >= this.Tokens.Length || end <= start)
        {
            return this.CurrentSpan();
        }

        AsnSourceSpan? first = this.Tokens[start].Span;
        AsnSourceSpan? last = this.Tokens[end - 1].Span;
        if (first == null || last == null)
        {
            return first;
        }

        return new AsnSourceSpan
        {
            Start = first.Start,
            End = last.End
        };
    }

    internal AsnSourceSpan? CurrentSpan()
    {
        if (this.Tokens.Length == 0)
        {
            return this.Owner.Span;
        }

        int index = this.Position < this.Tokens.Length
            ? this.Position
            : this.Tokens.Length - 1;
        return this.Tokens[index].Span;
    }

    internal void AddInvalidDefinitionDiagnostic(
        string message,
        AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Ecn,
            EcnDiagnosticCodes.InvalidClassDefinition,
            message,
            span,
            null);
        this.Success = false;
    }

    internal void AddUnknownClassDiagnostic(
        string name,
        AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Ecn,
            EcnDiagnosticCodes.UnknownStructureClass,
            "The encoding structure class '" + name
                + "' could not be resolved.",
            span,
            null);
        this.Success = false;
    }

    internal void AddCategoryDiagnostic(
        string name,
        string required,
        AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Ecn,
            EcnDiagnosticCodes.IncompatibleClassCategory,
            "The encoding class '" + name + "' must belong to "
                + required + ".",
            span,
            null);
        this.Success = false;
    }
}
