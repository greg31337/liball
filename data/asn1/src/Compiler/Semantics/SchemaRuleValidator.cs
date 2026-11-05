namespace asn1;

internal class SchemaRuleValidator
{
    internal DiagnosticBag Diagnostics;
    internal AsnSourceSpan? FirstVersionNumberSpan;

    internal SchemaRuleValidator(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal bool ValidateTables(SymbolTable[] tables)
    {
        bool success = true;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            System.Collections.Generic.List<ResolvedTypeSyntax> types =
                new System.Collections.Generic.List<ResolvedTypeSyntax>();
            int typeIndex = 0;
            while (typeIndex < tables[tableIndex].TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = tables[tableIndex]
                    .TypeSymbols[typeIndex]
                    .ResolvedType;
                if (type != null)
                {
                    this.CollectTypeResolutions(type, types);
                }

                typeIndex = typeIndex + 1;
            }

            int valueIndex = 0;
            while (valueIndex < tables[tableIndex].ValueSymbols.Length)
            {
                ResolvedTypeSyntax? governingType = tables[tableIndex]
                    .ValueSymbols[valueIndex]
                    .GoverningType;
                if (governingType != null)
                {
                    this.CollectTypeResolutions(governingType, types);
                }

                valueIndex = valueIndex + 1;
            }

            bool moduleUsesVersionNumbers = this.ModuleUsesVersionNumbers(types);
            typeIndex = 0;
            while (typeIndex < types.Count)
            {
                if (!this.ValidateTypeResolution(
                    types[typeIndex],
                    moduleUsesVersionNumbers))
                {
                    success = false;
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return success;
    }

    internal void CollectTypeResolutions(
        ResolvedTypeSyntax type,
        System.Collections.Generic.List<ResolvedTypeSyntax> types)
    {
        if (types.Contains(type))
        {
            return;
        }

        types.Add(type);
        if (type.InnerType != null)
        {
            this.CollectTypeResolutions(type.InnerType, types);
        }

        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedTypeSyntax? componentType = type.Components[index].TypeResolution;
            if (componentType != null)
            {
                this.CollectTypeResolutions(componentType, types);
            }

            index = index + 1;
        }

        int constraintIndex = 0;
        while (constraintIndex < type.ConstraintTypes.Length)
        {
            this.CollectTypeResolutions(type.ConstraintTypes[constraintIndex], types);
            constraintIndex = constraintIndex + 1;
        }
    }

    internal bool ModuleUsesVersionNumbers(
        System.Collections.Generic.List<ResolvedTypeSyntax> types)
    {
        this.FirstVersionNumberSpan = null;
        int typeIndex = 0;
        while (typeIndex < types.Count)
        {
            ParsedTypeSyntax? syntax = types[typeIndex].SourceSyntax;
            if (syntax != null)
            {
                int componentIndex = 0;
                while (componentIndex < syntax.ComponentSyntax.Length)
                {
                    if (syntax.ComponentSyntax[componentIndex].HasVersionNumber)
                    {
                        this.FirstVersionNumberSpan =
                            syntax.ComponentSyntax[componentIndex].Span;
                        return true;
                    }

                    componentIndex = componentIndex + 1;
                }
            }

            typeIndex = typeIndex + 1;
        }

        return false;
    }

    internal bool ValidateTypeResolution(
        ResolvedTypeSyntax type,
        bool moduleUsesVersionNumbers)
    {
        ParsedTypeSyntax? syntax = type.SourceSyntax;
        if (syntax == null)
        {
            return true;
        }

        bool success = true;
        if (syntax.Type.Kind == AsnTypeKind.Enumerated
            && !this.ValidateEnumeratedType(syntax))
        {
            success = false;
        }

        if ((syntax.Type.Kind == AsnTypeKind.Sequence
                || syntax.Type.Kind == AsnTypeKind.Set
                || syntax.Type.Kind == AsnTypeKind.Choice)
            && !this.ValidateStructuredType(
                type,
                syntax,
                moduleUsesVersionNumbers))
        {
            success = false;
        }

        return success;
    }

    internal bool ValidateEnumeratedType(ParsedTypeSyntax syntax)
    {
        bool success = true;
        if (syntax.Type.Extensible && syntax.RootNamedValues.Length == 0)
        {
            this.AddSchemaDiagnostic(
                SchemaDiagnosticCodes.ExtensionRootInvalid,
                "An ENUMERATED extension marker requires a non-empty root enumeration.",
                syntax.Span);
            success = false;
        }

        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<AsnSourceSpan?> nameSpans =
            new System.Collections.Generic.List<AsnSourceSpan?>();
        System.Collections.Generic.List<System.Numerics.BigInteger> usedValues =
            new System.Collections.Generic.List<System.Numerics.BigInteger>();
        System.Collections.Generic.List<AsnSourceSpan?> usedValueSpans =
            new System.Collections.Generic.List<AsnSourceSpan?>();
        int index = 0;
        while (index < syntax.RootNamedValues.Length)
        {
            AsnNamedValue item = syntax.RootNamedValues[index];
            AsnSourceSpan? itemSpan = this.GetNamedValueSpan(syntax, false, index);
            if (!this.AddEnumerationName(item.Name, names, nameSpans, itemSpan))
            {
                success = false;
            }

            if (item.Value != null && item.Value.HasIntegerValue)
            {
                int duplicateIndex = usedValues.IndexOf(item.Value.IntegerValue);
                if (duplicateIndex >= 0)
                {
                    this.AddSchemaDiagnosticWithRelated(
                        SchemaDiagnosticCodes.EnumeratedItemConflict,
                        "ENUMERATED root number '" + item.Value.IntegerValue.ToString()
                            + "' is assigned more than once.",
                        itemSpan,
                        "The first enumeration with this number is here.",
                        usedValueSpans[duplicateIndex]);
                    success = false;
                }
                else
                {
                    usedValues.Add(item.Value.IntegerValue);
                    usedValueSpans.Add(itemSpan);
                }
            }
            else if (item.Value != null)
            {
                this.AddSchemaDiagnostic(
                    SchemaDiagnosticCodes.EnumeratedItemConflict,
                    "ENUMERATED number for '" + item.Name + "' cannot be resolved.",
                    itemSpan);
                success = false;
            }

            index = index + 1;
        }

        System.Numerics.BigInteger nextRoot = System.Numerics.BigInteger.Zero;
        index = 0;
        while (index < syntax.RootNamedValues.Length)
        {
            AsnNamedValue item = syntax.RootNamedValues[index];
            if (item.Value == null)
            {
                while (usedValues.Contains(nextRoot))
                {
                    nextRoot = nextRoot + System.Numerics.BigInteger.One;
                }

                item.Value = this.CreateEnumeratedNumber(nextRoot);
                usedValues.Add(nextRoot);
                usedValueSpans.Add(this.GetNamedValueSpan(syntax, false, index));
                nextRoot = nextRoot + System.Numerics.BigInteger.One;
            }

            index = index + 1;
        }

        bool hasPreviousExtension = false;
        System.Numerics.BigInteger previousExtension = System.Numerics.BigInteger.Zero;
        AsnSourceSpan? previousExtensionSpan = null;
        index = 0;
        while (index < syntax.ExtensionNamedValues.Length)
        {
            AsnNamedValue item = syntax.ExtensionNamedValues[index];
            AsnSourceSpan? itemSpan = this.GetNamedValueSpan(syntax, true, index);
            if (!this.AddEnumerationName(item.Name, names, nameSpans, itemSpan))
            {
                success = false;
            }

            System.Numerics.BigInteger number = System.Numerics.BigInteger.Zero;
            bool hasNumber = false;
            if (item.Value == null)
            {
                number = hasPreviousExtension
                    ? previousExtension + System.Numerics.BigInteger.One
                    : System.Numerics.BigInteger.Zero;
                while (usedValues.Contains(number))
                {
                    number = number + System.Numerics.BigInteger.One;
                }

                item.Value = this.CreateEnumeratedNumber(number);
                hasNumber = true;
            }
            else if (item.Value.HasIntegerValue)
            {
                number = item.Value.IntegerValue;
                hasNumber = true;
            }
            else
            {
                this.AddSchemaDiagnostic(
                    SchemaDiagnosticCodes.EnumeratedItemConflict,
                    "ENUMERATED number for '" + item.Name + "' cannot be resolved.",
                    itemSpan);
                success = false;
            }

            if (hasNumber)
            {
                int duplicateIndex = usedValues.IndexOf(number);
                if (duplicateIndex >= 0)
                {
                    this.AddSchemaDiagnosticWithRelated(
                        SchemaDiagnosticCodes.EnumeratedItemConflict,
                        "ENUMERATED number '" + number.ToString()
                            + "' is assigned more than once.",
                        itemSpan,
                        "The first enumeration with this number is here.",
                        usedValueSpans[duplicateIndex]);
                    success = false;
                }

                if (hasPreviousExtension && number <= previousExtension)
                {
                    this.AddSchemaDiagnosticWithRelated(
                        SchemaDiagnosticCodes.EnumeratedExtensionOrder,
                        "Each ENUMERATED extension number must be greater than "
                            + "all preceding extension numbers.",
                        itemSpan,
                        "The preceding extension enumeration is here.",
                        previousExtensionSpan);
                    success = false;
                }

                if (!usedValues.Contains(number))
                {
                    usedValues.Add(number);
                    usedValueSpans.Add(itemSpan);
                }

                previousExtension = number;
                previousExtensionSpan = itemSpan;
                hasPreviousExtension = true;
            }

            index = index + 1;
        }

        return success;
    }

    internal bool AddEnumerationName(
        string name,
        System.Collections.Generic.List<string> names,
        System.Collections.Generic.List<AsnSourceSpan?> spans,
        AsnSourceSpan? span)
    {
        int duplicateIndex = names.IndexOf(name);
        if (duplicateIndex >= 0)
        {
            this.AddSchemaDiagnosticWithRelated(
                SchemaDiagnosticCodes.EnumeratedItemConflict,
                "ENUMERATED identifier '" + name + "' is declared more than once.",
                span,
                "The first enumeration with this identifier is here.",
                spans[duplicateIndex]);
            return false;
        }

        names.Add(name);
        spans.Add(span);
        return true;
    }

    internal AsnSourceSpan? GetNamedValueSpan(
        ParsedTypeSyntax syntax,
        bool extension,
        int index)
    {
        AsnSourceSpan?[] spans = extension
            ? syntax.ExtensionNamedValueSpans
            : syntax.RootNamedValueSpans;
        return index < spans.Length ? spans[index] : syntax.Span;
    }

    internal AsnValue CreateEnumeratedNumber(System.Numerics.BigInteger number)
    {
        return new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = number
        };
    }

    internal bool ValidateStructuredType(
        ResolvedTypeSyntax resolution,
        ParsedTypeSyntax syntax,
        bool moduleUsesVersionNumbers)
    {
        bool success = true;
        if (syntax.Type.Kind == AsnTypeKind.Choice
            && syntax.HasExtensionMarker
            && !this.HasRootComponent(syntax.ComponentSyntax))
        {
            this.AddSchemaDiagnostic(
                SchemaDiagnosticCodes.ExtensionRootInvalid,
                "A CHOICE extension marker requires a non-empty root alternative list.",
                syntax.Span);
            success = false;
        }

        if (syntax.HasExtensionEndMarker && !syntax.HasExtensionMarker)
        {
            this.AddSchemaDiagnostic(
                SchemaDiagnosticCodes.ExtensionMarkerInvalid,
                "An extension end marker requires an initial extension marker.",
                syntax.Span);
            success = false;
        }

        if (!this.ValidateComponentDeclarations(
            syntax,
            moduleUsesVersionNumbers))
        {
            success = false;
        }

        if (!this.ValidateDistinctComponentNames(resolution, syntax.Span))
        {
            success = false;
        }

        return success;
    }

    internal bool HasRootComponent(ParsedComponentSyntax[] components)
    {
        int index = 0;
        while (index < components.Length)
        {
            if (!components[index].Extension && !components[index].AfterExtensionEnd)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ValidateComponentDeclarations(
        ParsedTypeSyntax syntax,
        bool moduleUsesVersionNumbers)
    {
        bool success = true;
        int previousGroupIndex = -1;
        bool currentGroupHasVersion = false;
        System.Numerics.BigInteger currentGroupVersion = System.Numerics.BigInteger.Zero;
        bool hasPreviousVersion = false;
        System.Numerics.BigInteger previousVersion = System.Numerics.BigInteger.Zero;
        AsnSourceSpan? previousVersionSpan = null;
        AsnSourceSpan? currentGroupSpan = null;
        int index = 0;
        while (index < syntax.ComponentSyntax.Length)
        {
            ParsedComponentSyntax component = syntax.ComponentSyntax[index];
            if ((component.Extension || component.ExtensionGroupIndex >= 0)
                && !syntax.HasExtensionMarker)
            {
                this.AddSchemaDiagnostic(
                    SchemaDiagnosticCodes.ExtensionMarkerInvalid,
                    "An extension addition requires an extension marker.",
                    component.Span);
                success = false;
            }

            if (component.AfterExtensionEnd && !syntax.HasExtensionEndMarker)
            {
                this.AddSchemaDiagnostic(
                    SchemaDiagnosticCodes.ExtensionMarkerInvalid,
                    "A trailing root component requires an extension end marker.",
                    component.Span);
                success = false;
            }

            if (component.Extension && component.AfterExtensionEnd)
            {
                this.AddSchemaDiagnostic(
                    SchemaDiagnosticCodes.ExtensionMarkerInvalid,
                    "A component cannot be both an extension addition and a trailing root component.",
                    component.Span);
                success = false;
            }

            if (component.ExtensionGroupIndex >= 0 && !component.Extension)
            {
                this.AddSchemaDiagnostic(
                    SchemaDiagnosticCodes.ExtensionMarkerInvalid,
                    "Version brackets can contain only extension additions.",
                    component.Span);
                success = false;
            }

            if (!this.ValidateComponentOptionality(syntax.Type.Kind, component))
            {
                success = false;
            }

            if (moduleUsesVersionNumbers
                && component.Extension
                && (component.ExtensionGroupIndex < 0 || !component.HasVersionNumber))
            {
                this.AddSchemaDiagnosticWithRelated(
                    SchemaDiagnosticCodes.VersionBracketConsistency,
                    "Every extension addition in a module that uses version numbers "
                        + "must be in a numbered extension group.",
                    component.Span,
                    "The module's first numbered extension group is here.",
                    this.FirstVersionNumberSpan);
                success = false;
            }

            if (component.ExtensionGroupIndex >= 0
                && component.ExtensionGroupIndex != previousGroupIndex)
            {
                if (component.HasVersionNumber)
                {
                    if (component.VersionNumber < new System.Numerics.BigInteger(2))
                    {
                        this.AddSchemaDiagnostic(
                            SchemaDiagnosticCodes.ExtensionGroupVersion,
                            "An extension group version number must be at least 2.",
                            component.Span);
                        success = false;
                    }

                    if (hasPreviousVersion
                        && component.VersionNumber <= previousVersion)
                    {
                        this.AddSchemaDiagnosticWithRelated(
                            SchemaDiagnosticCodes.ExtensionGroupVersion,
                            "Extension group version numbers must strictly increase "
                                + "within an insertion point.",
                            component.Span,
                            "The preceding numbered extension group is here.",
                            previousVersionSpan);
                        success = false;
                    }

                    previousVersion = component.VersionNumber;
                    previousVersionSpan = component.Span;
                    hasPreviousVersion = true;
                }

                previousGroupIndex = component.ExtensionGroupIndex;
                currentGroupHasVersion = component.HasVersionNumber;
                currentGroupVersion = component.VersionNumber;
                currentGroupSpan = component.Span;
            }
            else if (component.ExtensionGroupIndex >= 0
                && (component.HasVersionNumber != currentGroupHasVersion
                    || (component.HasVersionNumber
                        && component.VersionNumber != currentGroupVersion)))
            {
                this.AddSchemaDiagnosticWithRelated(
                    SchemaDiagnosticCodes.VersionBracketConsistency,
                    "All components in an extension group must use the same version number.",
                    component.Span,
                    "The first component in this extension group is here.",
                    currentGroupSpan);
                success = false;
            }

            index = index + 1;
        }

        return success;
    }

    internal bool ValidateComponentOptionality(
        AsnTypeKind containingKind,
        ParsedComponentSyntax syntax)
    {
        AsnComponent? component = syntax.Component;
        if (component == null)
        {
            return true;
        }

        bool success = true;
        if (component.Optional && component.HasDefaultValue)
        {
            this.AddSchemaDiagnostic(
                SchemaDiagnosticCodes.ComponentOptionalityDefault,
                "A component cannot be both OPTIONAL and DEFAULT.",
                syntax.Span);
            success = false;
        }

        bool hasDefaultSyntax = syntax.DefaultValueSyntax != null
            || syntax.DefaultValueTokens.Length > 0
            || component.DefaultValue != null;
        if (component.HasDefaultValue != hasDefaultSyntax)
        {
            this.AddSchemaDiagnostic(
                SchemaDiagnosticCodes.ComponentOptionalityDefault,
                "A component DEFAULT marker must have exactly one default value.",
                syntax.Span);
            success = false;
        }

        if (containingKind == AsnTypeKind.Choice
            && (component.Optional || component.HasDefaultValue))
        {
            this.AddSchemaDiagnostic(
                SchemaDiagnosticCodes.ComponentOptionalityDefault,
                "CHOICE alternatives cannot be OPTIONAL or DEFAULT.",
                syntax.Span);
            success = false;
        }

        return success;
    }

    internal bool ValidateDistinctComponentNames(
        ResolvedTypeSyntax resolution,
        AsnSourceSpan? span)
    {
        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<ResolvedComponentSyntax> declarations =
            new System.Collections.Generic.List<ResolvedComponentSyntax>();
        bool success = true;
        int index = 0;
        while (index < resolution.Components.Length)
        {
            AsnComponent? component = resolution.Components[index].Component;
            if (component != null)
            {
                int duplicateIndex = names.IndexOf(component.Name);
                if (duplicateIndex >= 0)
                {
                    this.AddSchemaDiagnosticWithRelated(
                        SchemaDiagnosticCodes.ComponentNameDuplicate,
                        "Component or alternative identifier '" + component.Name
                            + "' is declared more than once.",
                        this.GetComponentDeclarationSpan(resolution.Components[index], span),
                        "The first component or alternative with this identifier is here.",
                        this.GetComponentDeclarationSpan(
                            declarations[duplicateIndex],
                            span));
                    success = false;
                }
                else
                {
                    names.Add(component.Name);
                    declarations.Add(resolution.Components[index]);
                }
            }

            index = index + 1;
        }

        return success;
    }

    internal AsnSourceSpan? GetComponentDeclarationSpan(
        ResolvedComponentSyntax component,
        AsnSourceSpan? fallback)
    {
        int index = component.Provenance.Length - 1;
        while (index >= 0)
        {
            ParsedComponentSyntax? syntax = component.Provenance[index].ComponentSyntax;
            if (syntax != null && syntax.Span != null)
            {
                return syntax.Span;
            }

            index = index - 1;
        }

        return fallback;
    }

    internal void AddSchemaDiagnostic(
        int number,
        string message,
        AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            number,
            message,
            span,
            null);
    }

    internal void AddSchemaDiagnosticWithRelated(
        int number,
        string message,
        AsnSourceSpan? span,
        string relatedMessage,
        AsnSourceSpan? relatedSpan)
    {
        AsnDiagnosticRelatedLocation[] related = relatedSpan == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = relatedMessage,
                Span = relatedSpan
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            number,
            message,
            span,
            related);
    }
}
