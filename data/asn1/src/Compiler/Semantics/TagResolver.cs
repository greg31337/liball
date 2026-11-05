namespace asn1;

internal class TagResolver
{
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];

    internal TagResolver(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal TagResolutionResult ResolveTags(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        System.Collections.Generic.List<SymbolTableEntry> types =
            new System.Collections.Generic.List<SymbolTableEntry>();
        bool success = true;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int typeIndex = 0;
            while (typeIndex < tables[tableIndex].TypeSymbols.Length)
            {
                SymbolTableEntry entry = tables[tableIndex].TypeSymbols[typeIndex];
                types.Add(entry);
                if (entry.ResolvedType == null
                    || !this.ResolveTypeTags(entry.ResolvedType, tables[tableIndex]))
                {
                    success = false;
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return new TagResolutionResult
        {
            Types = types.ToArray(),
            Success = success
        };
    }

    internal bool ResolveTypeTags(ResolvedTypeSyntax resolution, SymbolTable module)
    {
        if (resolution.TagResolutionState == 2)
        {
            return resolution.TagResolutionSuccess;
        }

        if (resolution.TagResolutionState == 1)
        {
            return true;
        }

        resolution.TagResolutionState = 1;
        bool success = this.CalculateOwnTagChain(resolution, module);
        resolution.TagResolutionSuccess = success;
        resolution.TagResolutionState = 2;
        if (!this.ResolveChildTags(resolution, module))
        {
            success = false;
        }

        resolution.TagResolutionSuccess = success;
        return success;
    }

    internal bool CalculateOwnTagChain(ResolvedTypeSyntax resolution, SymbolTable module)
    {
        ParsedTypeSyntax? syntax = resolution.SourceSyntax;
        if (syntax == null)
        {
            AsnTypeKind effectiveKind = resolution.EffectiveKind;
            int effectiveNumber = this.GetUniversalTagNumber(effectiveKind);
            resolution.TagChain = effectiveNumber < 0
                ? []
                : [this.CreateUniversalTag(
                    null,
                    resolution.SourceDeclaration,
                    module,
                    effectiveKind,
                    effectiveNumber)];
            return effectiveKind != AsnTypeKind.Unknown;
        }

        AsnTypeKind kind = syntax.Type.Kind;
        if (kind == AsnTypeKind.Defined)
        {
            return this.CopyAliasTagChain(resolution, module);
        }

        if (kind == AsnTypeKind.Selection)
        {
            return this.CopySelectionTagChain(resolution, module);
        }

        if (kind == AsnTypeKind.Tagged)
        {
            return this.CalculateTaggedTypeChain(resolution, module);
        }

        if (kind == AsnTypeKind.Prefixed)
        {
            return this.CopyPrefixedTagChain(resolution, module);
        }

        int universalNumber = this.GetUniversalTagNumber(kind);
        if (universalNumber >= 0)
        {
            resolution.TagChain = [this.CreateUniversalTag(
                syntax,
                resolution.SourceDeclaration,
                module,
                kind,
                universalNumber)];
        }
        else
        {
            resolution.TagChain = [];
        }

        return kind != AsnTypeKind.Unknown;
    }

    internal bool CopyAliasTagChain(ResolvedTypeSyntax resolution, SymbolTable module)
    {
        ResolvedTypeSyntax? target = resolution.AliasTarget;
        if (target == null)
        {
            resolution.TagChain = [];
            return resolution.ParameterReference;
        }

        SymbolTable targetModule = this.FindResolutionModule(target, module);
        bool success = this.ResolveTypeTags(target, targetModule);
        resolution.TagChain = this.CopyTagChain(target.TagChain);
        return success;
    }

    internal bool CopySelectionTagChain(ResolvedTypeSyntax resolution, SymbolTable module)
    {
        ResolvedComponentSyntax? selected = resolution.SelectedComponent;
        ResolvedTypeSyntax? selectedType = selected == null ? null : selected.TypeResolution;
        if (selectedType == null)
        {
            resolution.TagChain = [];
            return false;
        }

        SymbolTable selectedModule = this.FindResolutionModule(selectedType, module);
        bool success = this.ResolveTypeTags(selectedType, selectedModule);
        resolution.TagChain = this.CopyTagChain(selectedType.TagChain);
        return success;
    }

    internal bool CalculateTaggedTypeChain(
        ResolvedTypeSyntax resolution,
        SymbolTable module)
    {
        ParsedTypeSyntax? syntax = resolution.SourceSyntax;
        ResolvedTypeSyntax? inner = resolution.InnerType;
        AsnTag? sourceTag = syntax == null ? null : syntax.Type.Tag;
        if (syntax == null || inner == null || sourceTag == null)
        {
            resolution.TagChain = [];
            return false;
        }

        SymbolTable innerModule = this.FindResolutionModule(inner, module);
        bool success = this.ResolveTypeTags(inner, innerModule);
        bool untagged = inner.TagChain.Length == 0;
        AsnTaggingMode declaredMode = sourceTag.Mode;
        bool defaulted = declaredMode == AsnTaggingMode.Unspecified;
        AsnTaggingMode effectiveMode = declaredMode;
        if (defaulted)
        {
            AsnTaggingMode moduleDefault = module.Module == null
                ? AsnTaggingMode.Explicit
                : module.Module.TagDefault;
            effectiveMode = (moduleDefault == AsnTaggingMode.Implicit
                || moduleDefault == AsnTaggingMode.Automatic) && !untagged
                ? AsnTaggingMode.Implicit
                : AsnTaggingMode.Explicit;
        }

        if (effectiveMode == AsnTaggingMode.Implicit && untagged)
        {
            this.AddIllegalImplicitDiagnostic(syntax, inner);
            resolution.TagChain = [];
            return false;
        }

        ResolvedTagSyntax outer = this.CreateTextualTag(
            syntax,
            resolution.SourceDeclaration,
            module,
            sourceTag,
            effectiveMode,
            declaredMode,
            defaulted);
        resolution.TagChain = effectiveMode == AsnTaggingMode.Explicit
            ? this.PrependTag(outer, inner.TagChain)
            : this.ReplaceOuterTag(outer, inner.TagChain);
        return success;
    }

    internal bool CopyPrefixedTagChain(ResolvedTypeSyntax resolution, SymbolTable module)
    {
        ResolvedTypeSyntax? inner = resolution.InnerType;
        if (inner == null)
        {
            resolution.TagChain = [];
            return false;
        }

        SymbolTable innerModule = this.FindResolutionModule(inner, module);
        bool success = this.ResolveTypeTags(inner, innerModule);
        resolution.TagChain = this.CopyTagChain(inner.TagChain);
        return success;
    }

    internal bool ResolveChildTags(ResolvedTypeSyntax resolution, SymbolTable module)
    {
        ParsedTypeSyntax? syntax = resolution.SourceSyntax;
        if (syntax == null)
        {
            return true;
        }

        bool success = true;
        if (syntax.Type.Kind == AsnTypeKind.Sequence
            || syntax.Type.Kind == AsnTypeKind.Set
            || syntax.Type.Kind == AsnTypeKind.Choice)
        {
            int index = 0;
            while (index < resolution.Components.Length)
            {
                ResolvedTypeSyntax? component = resolution.Components[index].TypeResolution;
                if (component != null)
                {
                    SymbolTable componentModule = this.FindResolutionModule(component, module);
                    if (!this.ResolveTypeTags(component, componentModule))
                    {
                        success = false;
                    }
                }

                index = index + 1;
            }
        }
        else if ((syntax.Type.Kind == AsnTypeKind.SequenceOf
                || syntax.Type.Kind == AsnTypeKind.SetOf)
            && resolution.InnerType != null)
        {
            SymbolTable elementModule = this.FindResolutionModule(resolution.InnerType, module);
            success = this.ResolveTypeTags(resolution.InnerType, elementModule);
        }

        return success;
    }

    internal ResolvedTagSyntax CreateUniversalTag(
        ParsedTypeSyntax? syntax,
        SymbolTableEntry? declaration,
        SymbolTable module,
        AsnTypeKind kind,
        int number)
    {
        return new ResolvedTagSyntax
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Mode = AsnTaggingMode.Unspecified,
                Number = new System.Numerics.BigInteger(number)
            },
            OriginSyntax = syntax,
            OriginDeclaration = declaration,
            OriginModule = module.Module,
            UniversalTypeKind = kind,
            DeclaredMode = AsnTaggingMode.Unspecified,
            Universal = true,
            HasNumericNumber = true
        };
    }

    internal ResolvedTagSyntax CreateTextualTag(
        ParsedTypeSyntax syntax,
        SymbolTableEntry? declaration,
        SymbolTable module,
        AsnTag source,
        AsnTaggingMode effectiveMode,
        AsnTaggingMode declaredMode,
        bool defaulted)
    {
        return new ResolvedTagSyntax
        {
            Tag = new AsnTag
            {
                Class = source.Class,
                Mode = effectiveMode,
                Number = source.Number
            },
            OriginSyntax = syntax,
            OriginDeclaration = declaration,
            OriginModule = module.Module,
            DeclaredMode = declaredMode,
            NumberReference = syntax.TagNumberReference,
            DefaultModeApplied = defaulted,
            HasNumericNumber = syntax.HasNumericTagNumber
        };
    }

    internal ResolvedTagSyntax[] PrependTag(
        ResolvedTagSyntax outer,
        ResolvedTagSyntax[] inner)
    {
        ResolvedTagSyntax[] result = new ResolvedTagSyntax[inner.Length + 1];
        result[0] = outer;
        int index = 0;
        while (index < inner.Length)
        {
            result[index + 1] = inner[index];
            index = index + 1;
        }

        return result;
    }

    internal ResolvedTagSyntax[] ReplaceOuterTag(
        ResolvedTagSyntax outer,
        ResolvedTagSyntax[] inner)
    {
        ResolvedTagSyntax[] result = new ResolvedTagSyntax[inner.Length];
        result[0] = outer;
        int index = 1;
        while (index < inner.Length)
        {
            result[index] = inner[index];
            index = index + 1;
        }

        return result;
    }

    internal ResolvedTagSyntax[] CopyTagChain(ResolvedTagSyntax[] source)
    {
        ResolvedTagSyntax[] result = new ResolvedTagSyntax[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    internal SymbolTable FindResolutionModule(
        ResolvedTypeSyntax resolution,
        SymbolTable fallback)
    {
        SymbolTableEntry? declaration = resolution.SourceDeclaration;
        if (declaration == null)
        {
            return fallback;
        }

        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < this.Tables[tableIndex].Symbols.Length)
            {
                if (this.Tables[tableIndex].Symbols[symbolIndex] == declaration)
                {
                    return this.Tables[tableIndex];
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return fallback;
    }

    internal int GetUniversalTagNumber(AsnTypeKind kind)
    {
        switch (kind)
        {
            case AsnTypeKind.Boolean:
                return AsnUniversalTags.Boolean;
            case AsnTypeKind.Integer:
                return AsnUniversalTags.Integer;
            case AsnTypeKind.Enumerated:
                return AsnUniversalTags.Enumerated;
            case AsnTypeKind.Real:
                return AsnUniversalTags.Real;
            case AsnTypeKind.BitString:
                return AsnUniversalTags.BitString;
            case AsnTypeKind.OctetString:
                return AsnUniversalTags.OctetString;
            case AsnTypeKind.Null:
                return AsnUniversalTags.Null;
            case AsnTypeKind.ObjectIdentifier:
                return AsnUniversalTags.ObjectIdentifier;
            case AsnTypeKind.RelativeObjectIdentifier:
                return AsnUniversalTags.RelativeObjectIdentifier;
            case AsnTypeKind.ObjectIdentifierIri:
                return AsnUniversalTags.ObjectIdentifierIri;
            case AsnTypeKind.RelativeObjectIdentifierIri:
                return AsnUniversalTags.RelativeObjectIdentifierIri;
            case AsnTypeKind.Sequence:
                return AsnUniversalTags.Sequence;
            case AsnTypeKind.Set:
                return AsnUniversalTags.Set;
            case AsnTypeKind.SequenceOf:
                return AsnUniversalTags.SequenceOf;
            case AsnTypeKind.SetOf:
                return AsnUniversalTags.SetOf;
            case AsnTypeKind.InstanceOf:
                return AsnUniversalTags.InstanceOf;
            case AsnTypeKind.External:
                return AsnUniversalTags.External;
            case AsnTypeKind.EmbeddedPdv:
                return AsnUniversalTags.EmbeddedPdv;
            case AsnTypeKind.CharacterString:
                return AsnUniversalTags.CharacterString;
            case AsnTypeKind.ObjectDescriptor:
                return AsnUniversalTags.ObjectDescriptor;
            case AsnTypeKind.Utf8String:
                return AsnUniversalTags.Utf8String;
            case AsnTypeKind.NumericString:
                return AsnUniversalTags.NumericString;
            case AsnTypeKind.PrintableString:
                return AsnUniversalTags.PrintableString;
            case AsnTypeKind.TeletexString:
                return AsnUniversalTags.TeletexString;
            case AsnTypeKind.VideotexString:
                return AsnUniversalTags.VideotexString;
            case AsnTypeKind.Ia5String:
                return AsnUniversalTags.Ia5String;
            case AsnTypeKind.GraphicString:
                return AsnUniversalTags.GraphicString;
            case AsnTypeKind.VisibleString:
                return AsnUniversalTags.VisibleString;
            case AsnTypeKind.GeneralString:
                return AsnUniversalTags.GeneralString;
            case AsnTypeKind.UniversalString:
                return AsnUniversalTags.UniversalString;
            case AsnTypeKind.BmpString:
                return AsnUniversalTags.BmpString;
            case AsnTypeKind.UtcTime:
                return AsnUniversalTags.UtcTime;
            case AsnTypeKind.GeneralizedTime:
                return AsnUniversalTags.GeneralizedTime;
            case AsnTypeKind.Time:
                return AsnUniversalTags.Time;
            case AsnTypeKind.Date:
                return AsnUniversalTags.Date;
            case AsnTypeKind.TimeOfDay:
                return AsnUniversalTags.TimeOfDay;
            case AsnTypeKind.DateTime:
                return AsnUniversalTags.DateTime;
            case AsnTypeKind.Duration:
                return AsnUniversalTags.Duration;
            default:
                return -1;
        }
    }

    internal void AddIllegalImplicitDiagnostic(
        ParsedTypeSyntax syntax,
        ResolvedTypeSyntax inner)
    {
        AsnDiagnosticRelatedLocation[] related = inner.SourceSyntax == null
            || inner.SourceSyntax?.Span == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The untagged underlying type is here.",
                Span = inner.SourceSyntax.Span
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            SchemaDiagnosticCodes.IllegalImplicitTag,
            "IMPLICIT tagging is not permitted for an untagged CHOICE, open type, or dummy reference.",
            syntax.Span,
            related);
    }
}
