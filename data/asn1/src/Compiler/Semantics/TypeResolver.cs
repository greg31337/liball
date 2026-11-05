namespace asn1;

internal class TypeResolver
{
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];

    internal TypeResolver(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal TypeResolutionResult ResolveTypes(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        System.Collections.Generic.List<SymbolTableEntry> types =
            new System.Collections.Generic.List<SymbolTableEntry>();
        bool success = this.ParseTypeAssignments(types);
        int index = 0;
        while (index < types.Count)
        {
            SymbolTableEntry entry = types[index];
            SymbolTable? module = this.FindDeclaringModule(entry);
            if (module == null || !this.ResolveTypeEntry(entry, module, false).Success)
            {
                success = false;
            }

            index = index + 1;
        }

        this.RefreshResolvedReferences(types.ToArray());
        new ConstraintNormalizer().NormalizeTypes(types.ToArray());
        return new TypeResolutionResult
        {
            Types = types.ToArray(),
            Success = success
        };
    }

    internal bool ParseTypeAssignments(
        System.Collections.Generic.List<SymbolTableEntry> types)
    {
        bool success = true;
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            SymbolTable table = this.Tables[tableIndex];
            int symbolIndex = 0;
            while (symbolIndex < table.TypeSymbols.Length)
            {
                SymbolTableEntry entry = table.TypeSymbols[symbolIndex];
                if (entry.Predeclared)
                {
                    if (entry.ResolvedType == null
                        || !entry.ResolvedType.Success)
                    {
                        success = false;
                    }
                }
                else
                {
                    entry.TypeResolutionState = 0;
                    entry.DeclaredTypeSyntax = null;
                    entry.ResolvedType = null;
                    AsnToken[] tokens = this.GetRightSideTokens(entry);
                    TypeParseResult parsed = new TypeParser(
                        tokens,
                        this.Diagnostics,
                        true).ParseTypeSyntax();
                    if (!parsed.Success || parsed.Syntax == null)
                    {
                        success = false;
                    }
                    else
                    {
                        entry.DeclaredTypeSyntax = parsed.Syntax;
                    }
                }

                types.Add(entry);
                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return success;
    }

    internal AsnToken[] GetRightSideTokens(SymbolTableEntry entry)
    {
        AsnToken[] declarationTokens = entry.GetSemanticDeclarationTokens();
        int start = 0;
        while (start < declarationTokens.Length
            && declarationTokens[start].Kind != AsnTokenKind.Assignment)
        {
            start = start + 1;
        }

        start = start + 1;
        int end = declarationTokens.Length;
        while (end > start
            && declarationTokens[end - 1].Kind == AsnTokenKind.Semicolon)
        {
            end = end - 1;
        }

        if (start < 0 || start >= end)
        {
            return [];
        }

        AsnToken[] result = new AsnToken[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = declarationTokens[start + index];
            index = index + 1;
        }

        return result;
    }

    internal ResolvedTypeSyntax ResolveTypeEntry(
        SymbolTableEntry entry,
        SymbolTable module,
        bool structuralBoundary)
    {
        if (entry.ResolvedType != null && entry.TypeResolutionState == 2)
        {
            return entry.ResolvedType;
        }

        if (entry.ResolvedType != null && entry.TypeResolutionState == 1)
        {
            entry.ResolvedType.RecursiveReference = true;
            return entry.ResolvedType;
        }

        ParsedTypeSyntax? declared = entry.DeclaredTypeSyntax;
        if (declared == null)
        {
            ResolvedTypeSyntax failed = new ResolvedTypeSyntax
            {
                SourceDeclaration = entry
            };
            entry.ResolvedType = failed;
            entry.TypeResolutionState = 2;
            return failed;
        }

        ResolvedTypeSyntax resolution = this.CreateResolution(declared, entry);
        entry.ResolvedType = resolution;
        declared.Resolution = resolution;
        entry.TypeResolutionState = 1;
        this.ResolveTypeSyntax(declared, module, entry, structuralBoundary, resolution);
        entry.TypeResolutionState = 2;
        return resolution;
    }

    internal void ResolveTypeSyntax(
        ParsedTypeSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        bool structuralBoundary,
        ResolvedTypeSyntax resolution)
    {
        AsnTypeKind kind = syntax.Type.Kind;
        if (kind == AsnTypeKind.Defined)
        {
            this.ResolveDefinedType(
                syntax,
                module,
                declaration,
                structuralBoundary,
                resolution);
            return;
        }

        if (kind == AsnTypeKind.Selection)
        {
            this.ResolveSelectionType(syntax, module, declaration, structuralBoundary, resolution);
            return;
        }

        if (kind == AsnTypeKind.Sequence
            || kind == AsnTypeKind.Set
            || kind == AsnTypeKind.Choice)
        {
            this.ResolveStructuredType(syntax, module, declaration, resolution);
            return;
        }

        if (kind == AsnTypeKind.SequenceOf || kind == AsnTypeKind.SetOf)
        {
            this.ResolveCollectionType(syntax, module, declaration, resolution);
            return;
        }

        if (kind == AsnTypeKind.Tagged || kind == AsnTypeKind.Prefixed)
        {
            this.ResolvePrefixedType(syntax, module, declaration, structuralBoundary, resolution);
            return;
        }

        resolution.EffectiveType = syntax.Type;
        resolution.EffectiveKind = syntax.Type.Kind;
        resolution.Success = true;
    }

    internal void ResolveDefinedType(
        ParsedTypeSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        bool structuralBoundary,
        ResolvedTypeSyntax resolution)
    {
        if (syntax.InformationFromObjects != null || syntax.ParameterizedReference != null)
        {
            resolution.EffectiveType = syntax.Type;
            resolution.EffectiveKind = AsnTypeKind.Defined;
            resolution.ParameterReference = true;
            resolution.Success = true;
            return;
        }

        if (syntax.ReferenceModule.Length == 0
            && this.IsFormalTypeParameter(declaration, syntax.ReferenceName))
        {
            resolution.EffectiveType = syntax.Type;
            resolution.EffectiveKind = AsnTypeKind.Defined;
            resolution.ParameterReference = true;
            resolution.Success = true;
            return;
        }

        TypeReferenceBinding binding = this.FindTypeBinding(
            module,
            syntax.ReferenceModule,
            syntax.ReferenceName);
        SymbolTableEntry? target = binding.Target;
        if (target == null || binding.Module == null)
        {
            this.AddTypeReferenceDiagnostic(syntax, binding.Ambiguous);
            return;
        }

        bool recursive = target.TypeResolutionState == 1;
        ResolvedTypeSyntax targetResolution = this.ResolveTypeEntry(
            target,
            binding.Module,
            structuralBoundary);
        resolution.AliasTarget = targetResolution;
        resolution.EffectiveType = targetResolution.EffectiveType;
        resolution.EffectiveKind = targetResolution.EffectiveKind;
        resolution.EffectiveDeclaration = targetResolution.EffectiveDeclaration;
        resolution.Components = targetResolution.Components;
        if (recursive)
        {
            resolution.RecursiveReference = true;
            if (!structuralBoundary)
            {
                this.AddAliasCycleDiagnostic(syntax, target);
                return;
            }
        }

        resolution.Success = targetResolution.Success || recursive;
    }

    internal void ResolveSelectionType(
        ParsedTypeSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        bool structuralBoundary,
        ResolvedTypeSyntax resolution)
    {
        ParsedTypeSyntax? selectedSyntax = syntax.InnerTypeSyntax;
        if (selectedSyntax == null)
        {
            this.AddSelectionBaseDiagnostic(syntax, null);
            return;
        }

        ResolvedTypeSyntax selectedType = this.ResolveNestedType(
            selectedSyntax,
            module,
            declaration,
            structuralBoundary);
        resolution.InnerType = selectedType;
        if (!selectedType.Success || selectedType.EffectiveKind != AsnTypeKind.Choice)
        {
            this.AddSelectionBaseDiagnostic(syntax, selectedType);
            return;
        }

        int index = 0;
        while (index < selectedType.Components.Length)
        {
            ResolvedComponentSyntax component = selectedType.Components[index];
            if (component.Component != null
                && component.Component?.Name == syntax.SelectionIdentifier)
            {
                resolution.SelectedComponent = component;
                ResolvedTypeSyntax? componentType = component.TypeResolution;
                if (componentType == null)
                {
                    resolution.EffectiveType = component.Component.Type;
                    resolution.EffectiveKind = component.Component.Type == null
                        ? AsnTypeKind.Unknown
                        : component.Component.Type.Kind;
                    resolution.EffectiveDeclaration = declaration;
                }
                else
                {
                    resolution.EffectiveType = componentType.EffectiveType;
                    resolution.EffectiveKind = componentType.EffectiveKind;
                    resolution.EffectiveDeclaration = componentType.EffectiveDeclaration;
                    resolution.Components = componentType.Components;
                }

                resolution.Success = resolution.EffectiveType != null;
                return;
            }

            index = index + 1;
        }

        this.AddSelectionMemberDiagnostic(syntax, selectedType);
    }

    internal void ResolveStructuredType(
        ParsedTypeSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedTypeSyntax resolution)
    {
        System.Collections.Generic.List<ResolvedComponentSyntax> components =
            new System.Collections.Generic.List<ResolvedComponentSyntax>();
        bool success = true;
        int index = 0;
        while (index < syntax.ComponentSyntax.Length)
        {
            ParsedComponentSyntax componentSyntax = syntax.ComponentSyntax[index];
            if (componentSyntax.ComponentsOf)
            {
                if (!this.ExpandComponentsOf(
                    syntax.Type.Kind,
                    componentSyntax,
                    module,
                    declaration,
                    components))
                {
                    success = false;
                }
            }
            else if (!this.ResolveNamedComponent(
                componentSyntax,
                module,
                declaration,
                components))
            {
                success = false;
            }

            index = index + 1;
        }

        resolution.Components = components.ToArray();
        AsnComponent[] effectiveComponents = this.CopyEffectiveComponents(resolution.Components);
        resolution.EffectiveType = this.CopyType(
            syntax.Type,
            syntax.Type.ElementType,
            effectiveComponents);
        resolution.EffectiveKind = syntax.Type.Kind;
        resolution.Success = success;
    }

    internal bool ResolveNamedComponent(
        ParsedComponentSyntax componentSyntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        System.Collections.Generic.List<ResolvedComponentSyntax> components)
    {
        if (componentSyntax.Component == null || componentSyntax.TypeSyntax == null)
        {
            return false;
        }

        ResolvedTypeSyntax type = this.ResolveNestedType(
            componentSyntax.TypeSyntax,
            module,
            declaration,
            true);
        ComponentProvenanceSyntax provenance = this.CreateComponentProvenance(
            declaration,
            componentSyntax);
        AsnComponent component = this.CopyComponent(
            componentSyntax.Component,
            type.EffectiveType,
            componentSyntax.Extension);
        components.Add(new ResolvedComponentSyntax
        {
            Component = component,
            TypeResolution = type,
            Provenance = [provenance]
        });
        return type.Success;
    }

    internal bool ExpandComponentsOf(
        AsnTypeKind containingKind,
        ParsedComponentSyntax inclusion,
        SymbolTable module,
        SymbolTableEntry declaration,
        System.Collections.Generic.List<ResolvedComponentSyntax> components)
    {
        if (inclusion.TypeSyntax == null)
        {
            return false;
        }

        ResolvedTypeSyntax included = this.ResolveNestedType(
            inclusion.TypeSyntax,
            module,
            declaration,
            true);
        if (included.ParameterReference)
        {
            return true;
        }

        if (!included.Success
            || included.EffectiveKind != containingKind
            || (containingKind != AsnTypeKind.Sequence && containingKind != AsnTypeKind.Set))
        {
            this.AddComponentsKindDiagnostic(
                inclusion,
                containingKind,
                included);
            return false;
        }

        int added = 0;
        int index = 0;
        while (index < included.Components.Length)
        {
            ResolvedComponentSyntax source = included.Components[index];
            if (source.Component != null && !source.Component.Extension)
            {
                AsnComponent component = this.CopyComponent(
                    source.Component,
                    source.TypeResolution == null
                        ? source.Component.Type
                        : source.TypeResolution.EffectiveType,
                    inclusion.Extension);
                components.Add(new ResolvedComponentSyntax
                {
                    Component = component,
                    TypeResolution = source.TypeResolution,
                    Provenance = this.AppendComponentProvenance(
                        source.Provenance,
                        this.CreateComponentProvenance(declaration, inclusion))
                });
                added = added + 1;
            }

            index = index + 1;
        }

        if (added == 0)
        {
            this.AddComponentsEmptyDiagnostic(inclusion, included);
            return false;
        }

        return true;
    }

    internal void ResolveCollectionType(
        ParsedTypeSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedTypeSyntax resolution)
    {
        ParsedTypeSyntax? inner = syntax.InnerTypeSyntax;
        if (inner == null)
        {
            resolution.EffectiveType = syntax.Type;
            resolution.EffectiveKind = syntax.Type.Kind;
            return;
        }

        ResolvedTypeSyntax element = this.ResolveNestedType(inner, module, declaration, true);
        resolution.InnerType = element;
        resolution.EffectiveType = this.CopyType(syntax.Type, element.EffectiveType, []);
        resolution.EffectiveKind = syntax.Type.Kind;
        resolution.Success = element.Success;
    }

    internal void ResolvePrefixedType(
        ParsedTypeSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        bool structuralBoundary,
        ResolvedTypeSyntax resolution)
    {
        ParsedTypeSyntax? inner = syntax.InnerTypeSyntax;
        if (inner == null)
        {
            resolution.EffectiveType = syntax.Type;
            resolution.EffectiveKind = syntax.Type.Kind;
            return;
        }

        ResolvedTypeSyntax baseType = this.ResolveNestedType(
            inner,
            module,
            declaration,
            structuralBoundary);
        resolution.InnerType = baseType;
        resolution.EffectiveType = this.CopyType(syntax.Type, baseType.EffectiveType, []);
        resolution.EffectiveKind = baseType.EffectiveKind;
        resolution.Components = baseType.Components;
        resolution.Success = baseType.Success;
    }

    internal ResolvedTypeSyntax ResolveNestedType(
        ParsedTypeSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        bool structuralBoundary)
    {
        if (syntax.Resolution != null && syntax.Resolution.Success)
        {
            return syntax.Resolution;
        }

        ResolvedTypeSyntax resolution = syntax.Resolution == null
            ? this.CreateResolution(syntax, declaration)
            : syntax.Resolution;
        syntax.Resolution = resolution;
        this.ResolveTypeSyntax(syntax, module, declaration, structuralBoundary, resolution);
        return resolution;
    }

    internal ResolvedTypeSyntax CreateResolution(
        ParsedTypeSyntax syntax,
        SymbolTableEntry declaration)
    {
        return new ResolvedTypeSyntax
        {
            SourceSyntax = syntax,
            SourceType = syntax.Type,
            EffectiveType = syntax.Type,
            EffectiveKind = syntax.Type.Kind,
            SourceDeclaration = declaration,
            EffectiveDeclaration = declaration
        };
    }

    internal TypeReferenceBinding FindTypeBinding(
        SymbolTable module,
        string moduleName,
        string referenceName)
    {
        TypeReferenceBinding result = new TypeReferenceBinding();
        int count = 0;
        if (moduleName.Length == 0 || moduleName == module.ModuleName)
        {
            SymbolTableEntry[] local = module.FindSymbols(referenceName, AsnAssignmentKind.Type);
            if (local.Length == 1)
            {
                result.Module = module;
                result.Target = local[0];
                count = 1;
            }
            else if (local.Length > 1)
            {
                count = local.Length;
            }
        }

        if (moduleName.Length == 0 && count > 0)
        {
            result.Ambiguous = count > 1;
            if (result.Ambiguous)
            {
                result.Module = null;
                result.Target = null;
            }

            return result;
        }

        int resolutionIndex = 0;
        while (resolutionIndex < module.ImportResolutions.Length)
        {
            ImportResolution import = module.ImportResolutions[resolutionIndex];
            if (moduleName.Length == 0
                || (import.TargetModule != null
                    && import.TargetModule?.ModuleName == moduleName))
            {
                int bindingIndex = 0;
                while (bindingIndex < import.Symbols.Length)
                {
                    ImportedSymbolBinding binding = import.Symbols[bindingIndex];
                    if (binding.Import != null
                        && binding.Import?.Name == referenceName
                        && binding.Kind == AsnAssignmentKind.Type
                        && binding.Target != null)
                    {
                        count = count + 1;
                        result.Module = import.TargetModule;
                        result.Target = binding.Target;
                    }

                    bindingIndex = bindingIndex + 1;
                }
            }

            resolutionIndex = resolutionIndex + 1;
        }

        result.Ambiguous = count > 1;
        if (result.Ambiguous)
        {
            result.Module = null;
            result.Target = null;
        }

        return result;
    }

    internal SymbolTable? FindDeclaringModule(SymbolTableEntry entry)
    {
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < this.Tables[tableIndex].Symbols.Length)
            {
                if (this.Tables[tableIndex].Symbols[symbolIndex] == entry)
                {
                    return this.Tables[tableIndex];
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return null;
    }

    internal bool IsFormalTypeParameter(SymbolTableEntry entry, string name)
    {
        int index = 0;
        while (index < entry.FormalParameterSyntax.Length)
        {
            if (entry.FormalParameterSyntax[index].Parameter.Name == name
                && entry.FormalParameterSyntax[index].Parameter.Kind
                    == AsnParameterKind.Type)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal ComponentProvenanceSyntax CreateComponentProvenance(
        SymbolTableEntry declaration,
        ParsedComponentSyntax syntax)
    {
        return new ComponentProvenanceSyntax
        {
            Declaration = declaration,
            ComponentSyntax = syntax,
            ComponentsOf = syntax.ComponentsOf,
            Extension = syntax.Extension,
            AfterExtensionEnd = syntax.AfterExtensionEnd,
            ExtensionGroupIndex = syntax.ExtensionGroupIndex,
            HasVersionNumber = syntax.HasVersionNumber,
            VersionNumber = syntax.VersionNumber
        };
    }

    internal ComponentProvenanceSyntax[] AppendComponentProvenance(
        ComponentProvenanceSyntax[] source,
        ComponentProvenanceSyntax addition)
    {
        ComponentProvenanceSyntax[] result =
            new ComponentProvenanceSyntax[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        result[result.Length - 1] = addition;
        return result;
    }

    internal AsnComponent[] CopyEffectiveComponents(ResolvedComponentSyntax[] components)
    {
        AsnComponent[] result = new AsnComponent[components.Length];
        int index = 0;
        while (index < components.Length)
        {
            result[index] = components[index].Component == null
                ? new AsnComponent()
                : components[index].Component ?? new AsnComponent();
            index = index + 1;
        }

        return result;
    }

    internal AsnComponent CopyComponent(
        AsnComponent source,
        AsnType? type,
        bool extension)
    {
        return new AsnComponent
        {
            Name = source.Name,
            Type = type,
            Optional = source.Optional,
            HasDefaultValue = source.HasDefaultValue,
            DefaultValue = source.DefaultValue,
            Extension = extension
        };
    }

    internal AsnType CopyType(
        AsnType source,
        AsnType? elementType,
        AsnComponent[] components)
    {
        return new AsnType
        {
            Name = source.Name,
            Kind = source.Kind,
            Constraints = source.Constraints,
            Tag = source.Tag,
            Components = components,
            ElementType = elementType,
            NamedValues = source.NamedValues,
            Extensible = source.Extensible
        };
    }

    internal void RefreshResolvedReferences(SymbolTableEntry[] types)
    {
        int pass = 0;
        while (pass <= types.Length)
        {
            int typeIndex = 0;
            while (typeIndex < types.Length)
            {
                ResolvedTypeSyntax? resolution = types[typeIndex].ResolvedType;
                if (resolution != null)
                {
                    if (resolution.AliasTarget != null)
                    {
                        resolution.EffectiveType = resolution.AliasTarget.EffectiveType;
                        resolution.EffectiveKind = resolution.AliasTarget.EffectiveKind;
                        resolution.EffectiveDeclaration = resolution.AliasTarget.EffectiveDeclaration;
                        resolution.Components = resolution.AliasTarget.Components;
                    }

                    if (resolution.SelectedComponent != null)
                    {
                        ResolvedTypeSyntax? selected = resolution.SelectedComponent.TypeResolution;
                        if (selected != null)
                        {
                            resolution.EffectiveType = selected.EffectiveType;
                            resolution.EffectiveKind = selected.EffectiveKind;
                            resolution.EffectiveDeclaration = selected.EffectiveDeclaration;
                            resolution.Components = selected.Components;
                        }
                    }

                    int componentIndex = 0;
                    while (componentIndex < resolution.Components.Length)
                    {
                        ResolvedComponentSyntax component = resolution.Components[componentIndex];
                        if (component.Component != null && component.TypeResolution != null)
                        {
                            component.Component.Type = component.TypeResolution.EffectiveType;
                        }

                        componentIndex = componentIndex + 1;
                    }
                }

                typeIndex = typeIndex + 1;
            }

            pass = pass + 1;
        }
    }

    internal void AddTypeReferenceDiagnostic(ParsedTypeSyntax syntax, bool ambiguous)
    {
        string name = syntax.ReferenceModule.Length == 0
            ? syntax.ReferenceName
            : syntax.ReferenceModule + "." + syntax.ReferenceName;
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            12,
            ambiguous
                ? "Type reference '" + name + "' is ambiguous."
                : "Type reference '" + name + "' cannot be resolved.",
            syntax.Span,
            null);
    }

    internal void AddAliasCycleDiagnostic(
        ParsedTypeSyntax syntax,
        SymbolTableEntry target)
    {
        AsnDiagnosticRelatedLocation[] related = target.Span == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The cyclic type declaration is here.",
                Span = target.Span
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            13,
            "A type alias cycle must pass through a structured component boundary.",
            syntax.Span,
            related);
    }

    internal void AddSelectionBaseDiagnostic(
        ParsedTypeSyntax syntax,
        ResolvedTypeSyntax? selectedType)
    {
        AsnSourceSpan? relatedSpan = selectedType == null
            || selectedType.SourceDeclaration == null
            ? null
            : selectedType.SourceDeclaration.Span;
        AsnDiagnosticRelatedLocation[] related = relatedSpan == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The selected type is declared here.",
                Span = relatedSpan
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            SchemaDiagnosticCodes.SelectionRequiresChoice,
            "Selection '" + syntax.SelectionIdentifier + "' requires a CHOICE type.",
            syntax.Span,
            related);
    }

    internal void AddSelectionMemberDiagnostic(
        ParsedTypeSyntax syntax,
        ResolvedTypeSyntax selectedType)
    {
        AsnSourceSpan? declarationSpan = selectedType.SourceDeclaration == null
            ? null
            : selectedType.SourceDeclaration.Span;
        AsnDiagnosticRelatedLocation[] related = declarationSpan == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The CHOICE type is declared here.",
                Span = declarationSpan
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            SchemaDiagnosticCodes.SelectionAlternativeMissing,
            "CHOICE does not contain alternative '" + syntax.SelectionIdentifier + "'.",
            syntax.Span,
            related);
    }

    internal void AddComponentsKindDiagnostic(
        ParsedComponentSyntax inclusion,
        AsnTypeKind containingKind,
        ResolvedTypeSyntax included)
    {
        AsnSourceSpan? relatedSpan = included.SourceDeclaration == null
            ? null
            : included.SourceDeclaration.Span;
        AsnDiagnosticRelatedLocation[] related = relatedSpan == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The included type is declared here.",
                Span = relatedSpan
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            SchemaDiagnosticCodes.ComponentsOfKindMismatch,
            "COMPONENTS OF in " + containingKind.ToString()
                + " requires a " + containingKind.ToString()
                + " type, but the effective type is " + included.EffectiveKind.ToString() + ".",
            inclusion.Span,
            related);
    }

    internal void AddComponentsEmptyDiagnostic(
        ParsedComponentSyntax inclusion,
        ResolvedTypeSyntax included)
    {
        AsnSourceSpan? relatedSpan = included.SourceDeclaration == null
            ? null
            : included.SourceDeclaration.Span;
        AsnDiagnosticRelatedLocation[] related = relatedSpan == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The included type with no root components is declared here.",
                Span = relatedSpan
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            SchemaDiagnosticCodes.ComponentsOfEmpty,
            "COMPONENTS OF requires at least one root component.",
            inclusion.Span,
            related);
    }
}
