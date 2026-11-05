namespace asn1;

internal class InformationFromObjectsResolver
{
    internal ValueResolver ValueResolver;
    internal InformationObjectBinder ObjectBinder;
    internal ObjectSetEvaluator ObjectSetEvaluator;
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];
    internal System.Collections.Generic.HashSet<ResolvedTypeSyntax> VisitedTypes =
        new System.Collections.Generic.HashSet<ResolvedTypeSyntax>();
    internal bool Success = true;

    internal InformationFromObjectsResolver(
        ValueResolver valueResolver,
        InformationObjectBinder objectBinder,
        ObjectSetEvaluator objectSetEvaluator)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
        this.ObjectBinder = objectBinder == null
            ? throw new System.ArgumentNullException("objectBinder")
            : objectBinder;
        this.ObjectSetEvaluator = objectSetEvaluator == null
            ? throw new System.ArgumentNullException("objectSetEvaluator")
            : objectSetEvaluator;
        this.Diagnostics = valueResolver.Diagnostics;
    }

    internal bool ResolveInformation(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int typeIndex = 0;
            while (typeIndex < tables[tableIndex].TypeSymbols.Length)
            {
                this.ResolveTypeAssignmentInformation(
                    tables[tableIndex].TypeSymbols[typeIndex],
                    tables[tableIndex]);
                typeIndex = typeIndex + 1;
            }

            int objectIndex = 0;
            while (objectIndex < tables[tableIndex].ObjectSymbols.Length)
            {
                this.ResolveObjectAssignmentInformation(
                    tables[tableIndex].ObjectSymbols[objectIndex],
                    tables[tableIndex]);
                objectIndex = objectIndex + 1;
            }

            int setIndex = 0;
            while (setIndex < tables[tableIndex].ObjectSetSymbols.Length)
            {
                if (!tables[tableIndex].ObjectSetSymbols[setIndex].Parameterized)
                {
                    this.ResolveObjectSetAssignmentInformation(
                        tables[tableIndex].ObjectSetSymbols[setIndex],
                        tables[tableIndex]);
                }

                setIndex = setIndex + 1;
            }

            int valueSetIndex = 0;
            while (valueSetIndex < tables[tableIndex].ValueSetSymbols.Length)
            {
                this.ResolveValueSetAssignmentInformation(
                    tables[tableIndex].ValueSetSymbols[valueSetIndex],
                    tables[tableIndex]);
                valueSetIndex = valueSetIndex + 1;
            }

            int valueIndex = 0;
            while (valueIndex < tables[tableIndex].ValueSymbols.Length)
            {
                this.ResolveValueAssignmentInformation(
                    tables[tableIndex].ValueSymbols[valueIndex],
                    tables[tableIndex]);
                valueIndex = valueIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal void ResolveTypeAssignmentInformation(
        SymbolTableEntry symbol,
        SymbolTable module)
    {
        ResolvedTypeSyntax? resolution = symbol.ResolvedType;
        if (resolution == null)
        {
            return;
        }

        this.VisitedTypes.Clear();
        this.ResolveTypeInformation(resolution, module, symbol);
    }

    internal void ResolveTypeInformation(
        ResolvedTypeSyntax resolution,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        if (!this.VisitedTypes.Add(resolution))
        {
            return;
        }

        ParsedTypeSyntax? syntax = resolution.SourceSyntax;
        if (syntax != null && syntax.InformationFromObjects != null)
        {
            if (this.IsFormalObjectClassParameter(
                declaration,
                syntax.InformationFromObjects.ReferenceName))
            {
                resolution.ParameterReference = true;
            }
            else
            {
                InformationSelectionResult selected = this.ResolveSelection(
                    syntax.InformationFromObjects,
                    module,
                    declaration,
                    AsnAssignmentKind.Type);
                if (!selected.Success || selected.Type == null)
                {
                    resolution.Success = false;
                }
                else
                {
                    resolution.AliasTarget = selected.Type;
                    resolution.EffectiveType = selected.Type.EffectiveType;
                    resolution.EffectiveKind = selected.Type.EffectiveKind;
                    resolution.EffectiveDeclaration = selected.Type.EffectiveDeclaration;
                    resolution.Components = selected.Type.Components;
                    resolution.ObjectClassField = selected.Type.ObjectClassField;
                    resolution.ParameterReference = false;
                    resolution.Success = selected.Type.Success;
                }
            }
        }

        if (resolution.InnerType != null)
        {
            this.ResolveTypeInformation(resolution.InnerType, module, declaration);
        }

        int index = 0;
        while (index < resolution.Components.Length)
        {
            ResolvedTypeSyntax? component = resolution.Components[index].TypeResolution;
            if (component != null)
            {
                this.ResolveTypeInformation(component, module, declaration);
            }

            index = index + 1;
        }
    }

    internal bool IsFormalObjectClassParameter(
        SymbolTableEntry declaration,
        string name)
    {
        int index = 0;
        while (index < declaration.FormalParameters.Length)
        {
            if (declaration.FormalParameters[index].Name == name
                && declaration.FormalParameters[index].Kind
                    == AsnParameterKind.ObjectClass)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ResolveValueAssignmentInformation(
        SymbolTableEntry symbol,
        SymbolTable module)
    {
        ParsedValueSyntax? syntax = symbol.DeclaredValueSyntax;
        if (syntax == null || syntax.InformationFromObjects == null)
        {
            return;
        }

        InformationSelectionResult selected = this.ResolveSelection(
            syntax.InformationFromObjects,
            module,
            symbol,
            AsnAssignmentKind.Value);
        if (!selected.Success || selected.Value == null)
        {
            this.MarkValueInformationFailure(symbol, syntax);
            return;
        }

        if (!this.AreTypeGovernorsCompatible(
            symbol.GoverningType,
            selected.Value.GoverningType))
        {
            this.AddDiagnostic(
                InformationFromObjectsDiagnosticCodes.GovernorMismatch,
                "The selected value is not governed by the value assignment's type.",
                syntax.Span);
            this.MarkValueInformationFailure(symbol, syntax);
            return;
        }

        ResolvedValueSyntax result = new ResolvedValueSyntax
        {
            SourceSyntax = syntax,
            SourceValue = syntax.Value,
            EffectiveValue = selected.Value.EffectiveValue,
            GoverningType = symbol.GoverningType,
            SourceDeclaration = symbol,
            EffectiveDeclaration = selected.Value.EffectiveDeclaration,
            ReferenceTarget = selected.Value,
            Items = selected.Value.Items,
            ComponentValues = selected.Value.ComponentValues,
            ItemNames = selected.Value.ItemNames,
            NamedCollection = selected.Value.NamedCollection,
            BitStringValue = selected.Value.BitStringValue,
            ObjectIdentifierValue = selected.Value.ObjectIdentifierValue,
            Octets = selected.Value.Octets,
            Success = true
        };
        syntax.Resolution = result;
        symbol.ResolvedValue = result;
        symbol.ValueResolutionState = 2;
    }

    internal void ResolveValueSetAssignmentInformation(
        SymbolTableEntry symbol,
        SymbolTable module)
    {
        if (symbol.ValueSetResolutionState == 2)
        {
            return;
        }

        symbol.ValueSetResolutionState = 1;
        AsnToken[] rightSide = this.ObjectBinder.GetRightSideTokens(symbol);
        InformationFromObjectsParseResult parsed = new InformationFromObjectsParser(
            rightSide,
            this.Diagnostics,
            false).ParseInformationFromObjectsSyntax(AsnAssignmentKind.ValueSet);
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != rightSide.Length)
        {
            symbol.ValueSetResolutionState = 2;
            return;
        }

        InformationSelectionResult selected = this.ResolveSelection(
            parsed.Syntax,
            module,
            symbol,
            AsnAssignmentKind.ValueSet);
        ResolvedTypeSyntax? governor = this.ResolveValueSetGovernor(symbol, module);
        if (!selected.Success || selected.ValueSetType == null)
        {
            symbol.ValueSetResolutionState = 2;
            return;
        }

        if (governor == null
            || !this.ArePublicTypesCompatible(
                governor.EffectiveType,
                selected.ValueSetType))
        {
            this.AddDiagnostic(
                InformationFromObjectsDiagnosticCodes.GovernorMismatch,
                "The selected value set is not governed by the assignment's type.",
                parsed.Syntax.Span);
            symbol.ValueSetResolutionState = 2;
            return;
        }

        symbol.ResolvedValueSetType = selected.ValueSetType;
        symbol.ValueSetResolutionState = 2;
    }

    internal ResolvedTypeSyntax? ResolveValueSetGovernor(
        SymbolTableEntry symbol,
        SymbolTable module)
    {
        AsnToken[] governorTokens = this.ObjectBinder.GetObjectGovernorTokens(symbol);
        TypeParseResult parsed = new TypeParser(
            governorTokens,
            this.Diagnostics,
            false).ParseTypeSyntax();
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != governorTokens.Length)
        {
            return null;
        }

        return this.ValueResolver.TypeResolver.ResolveNestedType(
            parsed.Syntax,
            module,
            symbol,
            true);
    }

    internal void ResolveObjectAssignmentInformation(
        SymbolTableEntry symbol,
        SymbolTable module)
    {
        ParsedInformationObjectSyntax? syntax = symbol.DeclaredObjectSyntax;
        if (syntax == null)
        {
            return;
        }

        if (syntax.InformationFromObjects != null)
        {
            InformationSelectionResult selected = this.ResolveSelection(
                syntax.InformationFromObjects,
                module,
                symbol,
                AsnAssignmentKind.Object);
            if (!selected.Success || selected.ObjectSyntax == null)
            {
                return;
            }

            if (selected.ObjectSyntax.Object.ObjectClass
                != syntax.Object.ObjectClass)
            {
                this.AddDiagnostic(
                    InformationFromObjectsDiagnosticCodes.GovernorMismatch,
                    "The selected object is governed by a different object class.",
                    syntax.Span);
                return;
            }

            string name = syntax.Object.Name;
            string stableId = syntax.Object.StableId;
            syntax.Object = new AsnInformationObject
            {
                Name = name,
                StableId = stableId,
                ObjectClass = selected.ObjectSyntax.Object.ObjectClass,
                FieldSettings = selected.ObjectSyntax.Object.FieldSettings
            };
            syntax.FieldSettingSyntax = selected.ObjectSyntax.FieldSettingSyntax;
        }

        this.ResolveObjectFieldSelections(syntax, module, symbol);
    }

    internal void ResolveObjectSetAssignmentInformation(
        SymbolTableEntry symbol,
        SymbolTable module)
    {
        ParsedInformationObjectSetSyntax? syntax = symbol.DeclaredObjectSetSyntax;
        if (syntax == null)
        {
            return;
        }

        if (syntax.InformationFromObjects != null)
        {
            InformationSelectionResult selected = this.ResolveSelection(
                syntax.InformationFromObjects,
                module,
                symbol,
                AsnAssignmentKind.ObjectSet);
            if (!selected.Success || selected.ObjectSetSyntax == null)
            {
                return;
            }

            if (selected.ObjectSetSyntax.ObjectSet.ObjectClass
                != syntax.ObjectSet.ObjectClass)
            {
                this.AddDiagnostic(
                    InformationFromObjectsDiagnosticCodes.GovernorMismatch,
                    "The selected object set is governed by a different object class.",
                    syntax.Span);
                return;
            }

            syntax.InformationFromObjects.ResolvedObjectSetSyntax =
                selected.ObjectSetSyntax;
        }

        this.ResolveObjectSetElementSelections(syntax.RootElement, module, symbol);
        this.ResolveObjectSetElementSelections(syntax.AdditionalElement, module, symbol);
        ParsedObjectClassSyntax? governor = syntax.ObjectSet.ObjectClass == null
            ? null
            : this.ObjectBinder.FindResolvedClass(syntax.ObjectSet.ObjectClass);
        if (governor != null)
        {
            this.ObjectSetEvaluator.EvaluateSetSyntax(
                syntax,
                module,
                symbol,
                governor,
                new ObjectSetEvaluationContext
                {
                    StablePrefix = symbol.QualifiedName
                });
        }
    }

    internal void ResolveObjectFieldSelections(
        ParsedInformationObjectSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        ParsedObjectClassSyntax? ownerClass = syntax.Object.ObjectClass == null
            ? null
            : this.ObjectBinder.FindResolvedClass(syntax.Object.ObjectClass);
        int index = 0;
        while (ownerClass != null && index < syntax.FieldSettingSyntax.Length)
        {
            ParsedObjectFieldSettingSyntax setting = syntax.FieldSettingSyntax[index];
            ParsedObjectClassFieldSyntax? field = this.ObjectBinder.FindClassFieldSyntax(
                ownerClass,
                setting.Setting.FieldName);
            if (field != null && setting.InformationFromObjects != null)
            {
                this.ResolveFieldSettingSelection(
                    setting,
                    field,
                    syntax,
                    ownerClass,
                    module,
                    declaration);
            }

            if (setting.ObjectSyntax != null)
            {
                this.ResolveObjectFieldSelections(
                    setting.ObjectSyntax,
                    module,
                    declaration);
            }

            if (setting.ObjectSetSyntax != null)
            {
                this.ResolveNestedObjectSetSelections(
                    setting.ObjectSetSyntax,
                    module,
                    declaration,
                    syntax.Object.StableId + ":" + setting.Setting.FieldName);
            }

            index = index + 1;
        }
    }

    internal void ResolveFieldSettingSelection(
        ParsedObjectFieldSettingSyntax setting,
        ParsedObjectClassFieldSyntax field,
        ParsedInformationObjectSyntax owner,
        ParsedObjectClassSyntax ownerClass,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        AsnAssignmentKind expected = this.GetFieldResultKind(field.Field.Kind);
        InformationSelectionResult selected = this.ResolveSelection(
            setting.InformationFromObjects ?? new ParsedInformationFromObjectsSyntax(),
            module,
            declaration,
            expected);
        if (!selected.Success)
        {
            return;
        }

        if (expected == AsnAssignmentKind.Type && selected.Type != null)
        {
            setting.TypeResolution = selected.Type;
            setting.Setting.Type = selected.Type.EffectiveType;
            return;
        }

        if (expected == AsnAssignmentKind.Value && selected.Value != null)
        {
            ResolvedTypeSyntax? governor = this.ObjectBinder.ResolveSettingGovernor(
                field,
                owner,
                ownerClass);
            if (!this.AreTypeGovernorsCompatible(governor, selected.Value.GoverningType))
            {
                this.AddDiagnostic(
                    InformationFromObjectsDiagnosticCodes.GovernorMismatch,
                    "The selected value is not governed by field '"
                        + field.Field.Name + "'.",
                    setting.Span);
                return;
            }

            setting.Setting.Value = selected.Value.EffectiveValue;
            if (setting.ValueSyntax != null)
            {
                setting.ValueSyntax.Resolution = selected.Value;
            }

            return;
        }

        if (expected == AsnAssignmentKind.ValueSet
            && selected.ValueSetType != null)
        {
            ResolvedTypeSyntax? governor = this.ObjectBinder.ResolveSettingGovernor(
                field,
                owner,
                ownerClass);
            if (governor == null
                || !this.ArePublicTypesCompatible(
                    governor.EffectiveType,
                    selected.ValueSetType))
            {
                this.AddDiagnostic(
                    InformationFromObjectsDiagnosticCodes.GovernorMismatch,
                    "The selected value set is not governed by field '"
                        + field.Field.Name + "'.",
                    setting.Span);
                return;
            }

            setting.Setting.ValueSetType = selected.ValueSetType;
            return;
        }

        if (expected == AsnAssignmentKind.Object && selected.ObjectSyntax != null)
        {
            if (selected.ObjectSyntax.Object.ObjectClass != field.Field.ObjectClass)
            {
                this.AddFieldGovernorDiagnostic(field, setting.Span);
                return;
            }

            setting.ObjectSyntax = selected.ObjectSyntax;
            setting.Setting.Object = selected.ObjectSyntax.Object;
            return;
        }

        if (expected == AsnAssignmentKind.ObjectSet
            && selected.ObjectSetSyntax != null)
        {
            if (selected.ObjectSetSyntax.ObjectSet.ObjectClass != field.Field.ObjectClass)
            {
                this.AddFieldGovernorDiagnostic(field, setting.Span);
                return;
            }

            setting.ObjectSetSyntax = selected.ObjectSetSyntax;
            setting.Setting.ObjectSet = selected.ObjectSetSyntax.ObjectSet;
        }
    }

    internal void ResolveNestedObjectSetSelections(
        ParsedInformationObjectSetSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        string stablePrefix)
    {
        if (syntax.InformationFromObjects != null)
        {
            InformationSelectionResult selected = this.ResolveSelection(
                syntax.InformationFromObjects,
                module,
                declaration,
                AsnAssignmentKind.ObjectSet);
            syntax.InformationFromObjects.ResolvedObjectSetSyntax =
                selected.ObjectSetSyntax;
        }

        this.ResolveObjectSetElementSelections(syntax.RootElement, module, declaration);
        this.ResolveObjectSetElementSelections(
            syntax.AdditionalElement,
            module,
            declaration);
        ParsedObjectClassSyntax? governor = syntax.ObjectSet.ObjectClass == null
            ? null
            : this.ObjectBinder.FindResolvedClass(syntax.ObjectSet.ObjectClass);
        if (governor != null)
        {
            this.ObjectSetEvaluator.EvaluateSetSyntax(
                syntax,
                module,
                declaration,
                governor,
                new ObjectSetEvaluationContext { StablePrefix = stablePrefix });
        }
    }

    internal void ResolveObjectSetElementSelections(
        ParsedObjectSetElementSyntax? element,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        if (element == null)
        {
            return;
        }

        if (element.InformationFromObjects != null)
        {
            InformationSelectionResult selected = this.ResolveSelection(
                element.InformationFromObjects,
                module,
                declaration,
                AsnAssignmentKind.ObjectSet);
            element.InformationFromObjects.ResolvedObjectSetSyntax =
                selected.ObjectSetSyntax;
        }

        int index = 0;
        while (index < element.Children.Length)
        {
            this.ResolveObjectSetElementSelections(
                element.Children[index],
                module,
                declaration);
            index = index + 1;
        }
    }

    internal InformationSelectionResult ResolveSelection(
        ParsedInformationFromObjectsSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        AsnAssignmentKind expected)
    {
        if (syntax.ResolutionState == 2)
        {
            return this.CopyStoredSelection(syntax);
        }

        if (syntax.ResolutionState == 1)
        {
            this.AddDiagnostic(
                InformationFromObjectsDiagnosticCodes.ReferenceCycle,
                "The information-from-objects selection forms a cycle.",
                syntax.Span);
            return new InformationSelectionResult();
        }

        syntax.ResolutionState = 1;
        InformationSelectionResult result;
        if (expected == AsnAssignmentKind.Type && !syntax.ReferencedObjectSet)
        {
            SymbolTableEntry? classSymbol = this.ObjectBinder.FindSymbol(
                module,
                syntax.ReferenceModule,
                syntax.ReferenceName,
                AsnAssignmentKind.ObjectClass);
            ParsedObjectClassSyntax? useful = classSymbol == null
                ? this.ValueResolver.UsefulObjectClasses.ResolveUsefulClass(
                    syntax.ReferenceModule,
                    syntax.ReferenceName)
                : null;
            result = classSymbol == null && useful == null
                ? this.ResolveObjectBasedSelection(
                    syntax,
                    module,
                    declaration,
                    expected)
                : this.ResolveClassFieldSelection(syntax, classSymbol, useful);
        }
        else
        {
            result = this.ResolveObjectBasedSelection(
                syntax,
                module,
                declaration,
                expected);
        }

        syntax.ResolutionSuccess = result.Success;
        syntax.ResolvedType = result.Type;
        syntax.ResolvedValueSetType = result.ValueSetType;
        syntax.ResolvedValue = result.Value;
        syntax.ResolvedObjectSyntax = result.ObjectSyntax;
        syntax.ResolvedObjectSetSyntax = result.ObjectSetSyntax;
        syntax.ResolutionState = 2;
        return result;
    }

    internal InformationSelectionResult ResolveClassFieldSelection(
        ParsedInformationFromObjectsSyntax syntax,
        SymbolTableEntry? classSymbol,
        ParsedObjectClassSyntax? usefulClass)
    {
        ParsedObjectClassSyntax? objectClass = classSymbol == null
            ? usefulClass
            : classSymbol.ResolvedObjectClassSyntax;
        SymbolTableEntry? selectedClassSymbol = classSymbol;
        AsnObjectClassField? field = null;
        ParsedObjectClassFieldSyntax? selectedFieldSyntax = null;
        int pathIndex = 0;
        while (objectClass != null && pathIndex < syntax.FieldPath.Length)
        {
            ParsedObjectClassFieldSyntax? fieldSyntax =
                this.ObjectBinder.FindClassFieldSyntax(
                    objectClass,
                    syntax.FieldPath[pathIndex]);
            field = fieldSyntax == null ? null : fieldSyntax.Field;
            selectedFieldSyntax = fieldSyntax;
            if (field == null)
            {
                this.AddUnknownFieldDiagnostic(syntax.FieldPath[pathIndex], syntax.Span);
                return new InformationSelectionResult();
            }

            if (pathIndex + 1 < syntax.FieldPath.Length)
            {
                if ((field.Kind != AsnObjectFieldKind.Object
                        && field.Kind != AsnObjectFieldKind.ObjectSet)
                    || field.ObjectClass == null)
                {
                    this.AddResultKindDiagnostic(syntax.Span);
                    return new InformationSelectionResult();
                }

                objectClass = this.ObjectBinder.FindResolvedClass(field.ObjectClass);
                selectedClassSymbol = objectClass == null
                    ? null
                    : this.FindClassSymbolBySyntax(objectClass);
            }

            pathIndex = pathIndex + 1;
        }

        if (objectClass == null || field == null)
        {
            this.AddUnknownReferenceDiagnostic(syntax);
            return new InformationSelectionResult();
        }

        ResolvedTypeSyntax? fieldGovernor = selectedFieldSyntax == null
            || selectedFieldSyntax.GovernorTypeSyntax == null
            ? null
            : selectedFieldSyntax.GovernorTypeSyntax.Resolution;
        AsnType effective = fieldGovernor == null
            || fieldGovernor.EffectiveType == null
            ? field.Type ?? new AsnType
            {
                Kind = field.Kind == AsnObjectFieldKind.Type
                    ? AsnTypeKind.OpenType
                    : AsnTypeKind.Defined,
                Name = (classSymbol == null
                    ? objectClass.ObjectClass.Name
                    : classSymbol.QualifiedName) + "." + field.Name
            }
            : fieldGovernor.EffectiveType;
        ResolvedTypeSyntax type = new ResolvedTypeSyntax
        {
            SourceType = effective,
            EffectiveType = effective,
            EffectiveKind = effective.Kind,
            EffectiveDeclaration = fieldGovernor == null
                ? selectedClassSymbol
                : fieldGovernor.EffectiveDeclaration,
            AliasTarget = fieldGovernor,
            Components = fieldGovernor == null ? [] : fieldGovernor.Components,
            ObjectClassField = new ResolvedObjectClassFieldSyntax
            {
                ObjectClassSymbol = selectedClassSymbol,
                ObjectClassSyntax = objectClass,
                Field = field
            },
            Success = field.Kind == AsnObjectFieldKind.Type
                || field.Kind == AsnObjectFieldKind.FixedTypeValue
                || field.Kind == AsnObjectFieldKind.VariableTypeValue
                || field.Kind == AsnObjectFieldKind.FixedTypeValueSet
                || field.Kind == AsnObjectFieldKind.VariableTypeValueSet
        };
        if (!type.Success)
        {
            this.AddResultKindDiagnostic(syntax.Span);
        }

        return new InformationSelectionResult
        {
            Type = type,
            ObjectClassSyntax = objectClass,
            Field = field,
            Success = type.Success
        };
    }

    internal InformationSelectionResult ResolveObjectBasedSelection(
        ParsedInformationFromObjectsSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        AsnAssignmentKind expected)
    {
        System.Collections.Generic.List<ParsedInformationObjectSyntax> objects =
            new System.Collections.Generic.List<ParsedInformationObjectSyntax>();
        ParsedObjectClassSyntax? objectClass;
        if (syntax.ReferencedObjectSet)
        {
            SymbolTableEntry? setSymbol = this.ObjectBinder.FindSymbol(
                module,
                syntax.ReferenceModule,
                syntax.ReferenceName,
                AsnAssignmentKind.ObjectSet);
            ParsedInformationObjectSetSyntax? set = setSymbol == null
                ? null
                : this.ObjectSetEvaluator.ResolveDeclaredSet(setSymbol);
            if (set == null)
            {
                this.AddUnknownReferenceDiagnostic(syntax);
                return new InformationSelectionResult();
            }

            this.ResolveObjectSetAssignmentInformation(
                setSymbol ?? declaration,
                this.ValueResolver.FindDeclaringModule(setSymbol ?? declaration) ?? module);
            this.AddSetMemberObjects(set, objects);
            objectClass = set.ObjectSet.ObjectClass == null
                ? null
                : this.ObjectBinder.FindResolvedClass(set.ObjectSet.ObjectClass);
        }
        else
        {
            SymbolTableEntry? objectSymbol = this.ObjectBinder.FindSymbol(
                module,
                syntax.ReferenceModule,
                syntax.ReferenceName,
                AsnAssignmentKind.Object);
            ParsedInformationObjectSyntax? selected = objectSymbol == null
                ? null
                : this.ObjectBinder.ResolveDeclaredObject(objectSymbol);
            if (selected == null)
            {
                this.AddUnknownReferenceDiagnostic(syntax);
                return new InformationSelectionResult();
            }

            this.ResolveObjectAssignmentInformation(
                objectSymbol ?? declaration,
                this.ValueResolver.FindDeclaringModule(objectSymbol ?? declaration) ?? module);
            objects.Add(selected);
            objectClass = selected.Object.ObjectClass == null
                ? null
                : this.ObjectBinder.FindResolvedClass(selected.Object.ObjectClass);
        }

        if (objects.Count == 0 || objectClass == null)
        {
            this.AddDiagnostic(
                InformationFromObjectsDiagnosticCodes.EmptySelection,
                "The information-from-objects selection has no source objects.",
                syntax.Span);
            return new InformationSelectionResult();
        }

        return this.SelectFieldPath(
            syntax,
            objects,
            objectClass,
            expected,
            declaration,
            module);
    }

    internal InformationSelectionResult SelectFieldPath(
        ParsedInformationFromObjectsSyntax syntax,
        System.Collections.Generic.List<ParsedInformationObjectSyntax> objects,
        ParsedObjectClassSyntax objectClass,
        AsnAssignmentKind expected,
        SymbolTableEntry declaration,
        SymbolTable module)
    {
        int pathIndex = 0;
        while (pathIndex < syntax.FieldPath.Length)
        {
            ParsedObjectClassFieldSyntax? field = this.ObjectBinder.FindClassFieldSyntax(
                objectClass,
                syntax.FieldPath[pathIndex]);
            if (field == null)
            {
                this.AddUnknownFieldDiagnostic(syntax.FieldPath[pathIndex], syntax.Span);
                return new InformationSelectionResult();
            }

            if (pathIndex + 1 == syntax.FieldPath.Length)
            {
                return this.SelectFinalField(
                    objects,
                    objectClass,
                    field,
                    expected,
                    declaration,
                    module,
                    syntax.Span);
            }

            System.Collections.Generic.List<ParsedInformationObjectSyntax> next =
                new System.Collections.Generic.List<ParsedInformationObjectSyntax>();
            int objectIndex = 0;
            while (objectIndex < objects.Count)
            {
                ParsedObjectFieldSettingSyntax? setting = this.FindObjectSettingSyntax(
                    objects[objectIndex],
                    field.Field.Name);
                if (setting != null && setting.InformationFromObjects != null)
                {
                    this.ResolveFieldSettingSelection(
                        setting,
                        field,
                        objects[objectIndex],
                        objectClass,
                        module,
                        declaration);
                }

                if (field.Field.Kind == AsnObjectFieldKind.Object
                    && setting != null
                    && setting.ObjectSyntax != null)
                {
                    next.Add(setting.ObjectSyntax);
                }
                else if (field.Field.Kind == AsnObjectFieldKind.ObjectSet
                    && setting != null
                    && setting.ObjectSetSyntax != null)
                {
                    this.ResolveNestedObjectSetSelections(
                        setting.ObjectSetSyntax,
                        module,
                        declaration,
                        objects[objectIndex].Object.StableId + ":"
                            + field.Field.Name);
                    this.AddSetMemberObjects(setting.ObjectSetSyntax, next);
                }
                else
                {
                    this.AddResultKindDiagnostic(syntax.Span);
                    return new InformationSelectionResult();
                }

                objectIndex = objectIndex + 1;
            }

            objectClass = field.Field.ObjectClass == null
                ? new ParsedObjectClassSyntax()
                : this.ObjectBinder.FindResolvedClass(field.Field.ObjectClass)
                    ?? new ParsedObjectClassSyntax();
            objects = next;
            pathIndex = pathIndex + 1;
        }

        return new InformationSelectionResult();
    }

    internal InformationSelectionResult SelectFinalField(
        System.Collections.Generic.List<ParsedInformationObjectSyntax> objects,
        ParsedObjectClassSyntax objectClass,
        ParsedObjectClassFieldSyntax field,
        AsnAssignmentKind expected,
        SymbolTableEntry declaration,
        SymbolTable module,
        AsnSourceSpan? span)
    {
        if (expected == AsnAssignmentKind.Type)
        {
            if (objects.Count > 1)
            {
                this.AddAmbiguousSelectionDiagnostic(span);
                return new InformationSelectionResult();
            }

            if (objects.Count != 1 || field.Field.Kind != AsnObjectFieldKind.Type)
            {
                this.AddResultKindDiagnostic(span);
                return new InformationSelectionResult();
            }

            ParsedObjectFieldSettingSyntax? setting = this.FindObjectSettingSyntax(
                objects[0],
                field.Field.Name);
            return new InformationSelectionResult
            {
                Type = setting == null ? null : setting.TypeResolution,
                ObjectClassSyntax = objectClass,
                Field = field.Field,
                Success = setting != null && setting.TypeResolution != null
            };
        }

        if (expected == AsnAssignmentKind.Value)
        {
            if (objects.Count > 1)
            {
                this.AddAmbiguousSelectionDiagnostic(span);
                return new InformationSelectionResult();
            }

            if (objects.Count != 1
                || (field.Field.Kind != AsnObjectFieldKind.FixedTypeValue
                    && field.Field.Kind != AsnObjectFieldKind.VariableTypeValue))
            {
                this.AddResultKindDiagnostic(span);
                return new InformationSelectionResult();
            }

            ParsedObjectFieldSettingSyntax? setting = this.FindObjectSettingSyntax(
                objects[0],
                field.Field.Name);
            if (setting != null && setting.InformationFromObjects != null)
            {
                this.ResolveFieldSettingSelection(
                    setting,
                    field,
                    objects[0],
                    objectClass,
                    module,
                    declaration);
            }

            return new InformationSelectionResult
            {
                Value = setting == null || setting.ValueSyntax == null
                    ? null
                    : setting.ValueSyntax.Resolution,
                ObjectClassSyntax = objectClass,
                Field = field.Field,
                Success = setting != null
                    && setting.ValueSyntax != null
                    && setting.ValueSyntax.Resolution != null
            };
        }

        if (expected == AsnAssignmentKind.ValueSet)
        {
            return this.SelectValueSetField(
                objects,
                objectClass,
                field,
                declaration,
                module,
                span);
        }

        if (expected == AsnAssignmentKind.Object)
        {
            if (objects.Count > 1)
            {
                this.AddAmbiguousSelectionDiagnostic(span);
                return new InformationSelectionResult();
            }

            if (objects.Count != 1 || field.Field.Kind != AsnObjectFieldKind.Object)
            {
                this.AddResultKindDiagnostic(span);
                return new InformationSelectionResult();
            }

            ParsedObjectFieldSettingSyntax? setting = this.FindObjectSettingSyntax(
                objects[0],
                field.Field.Name);
            return new InformationSelectionResult
            {
                ObjectSyntax = setting == null ? null : setting.ObjectSyntax,
                ObjectClassSyntax = objectClass,
                Field = field.Field,
                Success = setting != null && setting.ObjectSyntax != null
            };
        }

        if (expected == AsnAssignmentKind.ObjectSet)
        {
            return this.SelectObjectSetField(objects, objectClass, field, span);
        }

        this.AddResultKindDiagnostic(span);
        return new InformationSelectionResult();
    }

    internal InformationSelectionResult SelectValueSetField(
        System.Collections.Generic.List<ParsedInformationObjectSyntax> objects,
        ParsedObjectClassSyntax objectClass,
        ParsedObjectClassFieldSyntax field,
        SymbolTableEntry declaration,
        SymbolTable module,
        AsnSourceSpan? span)
    {
        System.Collections.Generic.List<AsnValue> values =
            new System.Collections.Generic.List<AsnValue>();
        System.Collections.Generic.List<AsnType> sets =
            new System.Collections.Generic.List<AsnType>();
        AsnType? governor = null;
        int index = 0;
        while (index < objects.Count)
        {
            ParsedObjectFieldSettingSyntax? setting = this.FindObjectSettingSyntax(
                objects[index],
                field.Field.Name);
            if (setting != null && setting.InformationFromObjects != null)
            {
                this.ResolveFieldSettingSelection(
                    setting,
                    field,
                    objects[index],
                    objectClass,
                    module,
                    declaration);
            }

            if ((field.Field.Kind == AsnObjectFieldKind.FixedTypeValue
                    || field.Field.Kind == AsnObjectFieldKind.VariableTypeValue)
                && setting != null
                && setting.Setting.Value != null)
            {
                AsnType? itemType = setting.ValueSyntax == null
                    || setting.ValueSyntax.Resolution == null
                    || setting.ValueSyntax.Resolution.GoverningType == null
                    ? field.Field.Type
                    : setting.ValueSyntax.Resolution.GoverningType.EffectiveType;
                if (governor == null)
                {
                    governor = itemType;
                }

                if (!this.ArePublicTypesCompatible(governor, itemType))
                {
                    this.AddFieldGovernorDiagnostic(field, span);
                    return new InformationSelectionResult();
                }

                values.Add(setting.Setting.Value);
            }
            else if ((field.Field.Kind == AsnObjectFieldKind.FixedTypeValueSet
                    || field.Field.Kind == AsnObjectFieldKind.VariableTypeValueSet)
                && setting != null
                && setting.Setting.ValueSetType != null)
            {
                if (governor == null)
                {
                    governor = setting.Setting.ValueSetType;
                }

                if (!this.ArePublicTypesCompatible(
                    governor,
                    setting.Setting.ValueSetType))
                {
                    this.AddFieldGovernorDiagnostic(field, span);
                    return new InformationSelectionResult();
                }

                sets.Add(setting.Setting.ValueSetType);
            }
            else
            {
                this.AddResultKindDiagnostic(span);
                return new InformationSelectionResult();
            }

            index = index + 1;
        }

        AsnType? valueSet = governor == null
            ? null
            : this.CreateSelectedValueSet(governor, values, sets);
        return new InformationSelectionResult
        {
            ValueSetType = valueSet,
            ObjectClassSyntax = objectClass,
            Field = field.Field,
            Success = valueSet != null
        };
    }

    internal InformationSelectionResult SelectObjectSetField(
        System.Collections.Generic.List<ParsedInformationObjectSyntax> objects,
        ParsedObjectClassSyntax objectClass,
        ParsedObjectClassFieldSyntax field,
        AsnSourceSpan? span)
    {
        if (field.Field.Kind != AsnObjectFieldKind.Object
            && field.Field.Kind != AsnObjectFieldKind.ObjectSet)
        {
            this.AddResultKindDiagnostic(span);
            return new InformationSelectionResult();
        }

        System.Collections.Generic.List<EvaluatedObjectSetMember> members =
            new System.Collections.Generic.List<EvaluatedObjectSetMember>();
        int index = 0;
        while (index < objects.Count)
        {
            ParsedObjectFieldSettingSyntax? setting = this.FindObjectSettingSyntax(
                objects[index],
                field.Field.Name);
            if (field.Field.Kind == AsnObjectFieldKind.Object
                && setting != null
                && setting.ObjectSyntax != null)
            {
                members.Add(new EvaluatedObjectSetMember
                {
                    Object = setting.ObjectSyntax.Object,
                    ObjectSyntax = setting.ObjectSyntax,
                    StableId = setting.ObjectSyntax.Object.StableId,
                    Span = setting.Span
                });
            }
            else if (field.Field.Kind == AsnObjectFieldKind.ObjectSet
                && setting != null
                && setting.ObjectSetSyntax != null)
            {
                this.AddSetMembers(setting.ObjectSetSyntax, members);
            }
            else
            {
                this.AddResultKindDiagnostic(span);
                return new InformationSelectionResult();
            }

            index = index + 1;
        }

        ParsedInformationObjectSetSyntax selected = this.CreateSelectedObjectSet(
            members,
            field.Field.ObjectClass,
            span);
        return new InformationSelectionResult
        {
            ObjectSetSyntax = selected,
            ObjectClassSyntax = objectClass,
            Field = field.Field,
            Success = true
        };
    }

    internal AsnType CreateSelectedValueSet(
        AsnType governor,
        System.Collections.Generic.List<AsnValue> values,
        System.Collections.Generic.List<AsnType> sets)
    {
        AsnType result = this.ValueResolver.TypeResolver.CopyType(
            governor,
            governor.ElementType,
            governor.Components);
        System.Collections.Generic.List<AsnConstraintElement> elements =
            new System.Collections.Generic.List<AsnConstraintElement>();
        int index = 0;
        while (index < values.Count)
        {
            elements.Add(new AsnConstraintElement
            {
                Kind = AsnConstraintKind.SingleValue,
                Value = values[index]
            });
            index = index + 1;
        }

        index = 0;
        while (index < sets.Count)
        {
            elements.Add(new AsnConstraintElement
            {
                Kind = AsnConstraintKind.ContainedSubtype,
                Type = sets[index]
            });
            index = index + 1;
        }

        result.Constraints = [new AsnConstraint
        {
            Kind = elements.Count == 1
                ? elements[0].Kind
                : AsnConstraintKind.Union,
            Elements = elements.ToArray()
        }];
        return result;
    }

    internal ParsedInformationObjectSetSyntax CreateSelectedObjectSet(
        System.Collections.Generic.List<EvaluatedObjectSetMember> members,
        AsnObjectClass? objectClass,
        AsnSourceSpan? span)
    {
        EvaluatedObjectSetMember[] array = members.ToArray();
        return new ParsedInformationObjectSetSyntax
        {
            ObjectSet = new AsnInformationObjectSet
            {
                ObjectClass = objectClass,
                Objects = this.CopySelectedObjects(array),
                RootObjects = this.CopySelectedObjects(array),
                StableId = this.CreateSelectionStableId(span)
            },
            RootMembers = array,
            Span = span
        };
    }

    internal ParsedObjectFieldSettingSyntax? FindObjectSettingSyntax(
        ParsedInformationObjectSyntax syntax,
        string fieldName)
    {
        int index = 0;
        while (index < syntax.FieldSettingSyntax.Length)
        {
            if (syntax.FieldSettingSyntax[index].Setting.FieldName == fieldName)
            {
                return syntax.FieldSettingSyntax[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal SymbolTableEntry? FindClassSymbolBySyntax(
        ParsedObjectClassSyntax syntax)
    {
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < this.Tables[tableIndex].ObjectClassSymbols.Length)
            {
                SymbolTableEntry symbol = this.Tables[tableIndex]
                    .ObjectClassSymbols[symbolIndex];
                if (symbol.ResolvedObjectClassSyntax == syntax)
                {
                    return symbol;
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return null;
    }

    internal void AddSetMemberObjects(
        ParsedInformationObjectSetSyntax syntax,
        System.Collections.Generic.List<ParsedInformationObjectSyntax> objects)
    {
        int index = 0;
        while (index < syntax.RootMembers.Length)
        {
            if (syntax.RootMembers[index].ObjectSyntax != null)
            {
                objects.Add(syntax.RootMembers[index].ObjectSyntax
                    ?? new ParsedInformationObjectSyntax());
            }

            index = index + 1;
        }

        index = 0;
        while (index < syntax.AdditionalMembers.Length)
        {
            if (syntax.AdditionalMembers[index].ObjectSyntax != null)
            {
                objects.Add(syntax.AdditionalMembers[index].ObjectSyntax
                    ?? new ParsedInformationObjectSyntax());
            }

            index = index + 1;
        }
    }

    internal void AddSetMembers(
        ParsedInformationObjectSetSyntax syntax,
        System.Collections.Generic.List<EvaluatedObjectSetMember> members)
    {
        members.AddRange(syntax.RootMembers);
        members.AddRange(syntax.AdditionalMembers);
    }

    internal AsnInformationObject[] CopySelectedObjects(
        EvaluatedObjectSetMember[] members)
    {
        AsnInformationObject[] objects = new AsnInformationObject[members.Length];
        int index = 0;
        while (index < members.Length)
        {
            objects[index] = members[index].Object;
            index = index + 1;
        }

        return objects;
    }

    internal InformationSelectionResult CopyStoredSelection(
        ParsedInformationFromObjectsSyntax syntax)
    {
        return new InformationSelectionResult
        {
            Type = syntax.ResolvedType,
            ValueSetType = syntax.ResolvedValueSetType,
            Value = syntax.ResolvedValue,
            ObjectSyntax = syntax.ResolvedObjectSyntax,
            ObjectSetSyntax = syntax.ResolvedObjectSetSyntax,
            Success = syntax.ResolutionSuccess
        };
    }

    internal void MarkValueInformationFailure(
        SymbolTableEntry symbol,
        ParsedValueSyntax syntax)
    {
        ResolvedValueSyntax failed = new ResolvedValueSyntax
        {
            SourceSyntax = syntax,
            SourceValue = syntax.Value,
            GoverningType = symbol.GoverningType,
            SourceDeclaration = symbol
        };
        syntax.Resolution = failed;
        symbol.ResolvedValue = failed;
        symbol.ValueResolutionState = 2;
    }

    internal AsnAssignmentKind GetFieldResultKind(AsnObjectFieldKind kind)
    {
        if (kind == AsnObjectFieldKind.Type)
        {
            return AsnAssignmentKind.Type;
        }

        if (kind == AsnObjectFieldKind.FixedTypeValue
            || kind == AsnObjectFieldKind.VariableTypeValue)
        {
            return AsnAssignmentKind.Value;
        }

        if (kind == AsnObjectFieldKind.FixedTypeValueSet
            || kind == AsnObjectFieldKind.VariableTypeValueSet)
        {
            return AsnAssignmentKind.ValueSet;
        }

        return kind == AsnObjectFieldKind.Object
            ? AsnAssignmentKind.Object
            : AsnAssignmentKind.ObjectSet;
    }

    internal bool AreTypeGovernorsCompatible(
        ResolvedTypeSyntax? left,
        ResolvedTypeSyntax? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.EffectiveType == right.EffectiveType
            || left.EffectiveKind == right.EffectiveKind;
    }

    internal bool ArePublicTypesCompatible(AsnType? left, AsnType? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left == right || left.Kind == right.Kind;
    }

    internal string CreateSelectionStableId(AsnSourceSpan? span)
    {
        return span == null
            ? "information-selection"
            : "information-selection:" + span.Start.SourceName + ":"
                + span.Start.Offset.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
    }

    internal void AddUnknownReferenceDiagnostic(
        ParsedInformationFromObjectsSyntax syntax)
    {
        this.AddDiagnostic(
            InformationFromObjectsDiagnosticCodes.UnknownReference,
            "The information source '" + this.FormatReference(
                syntax.ReferenceModule,
                syntax.ReferenceName) + "' could not be resolved.",
            syntax.Span);
    }

    internal void AddUnknownFieldDiagnostic(
        string fieldName,
        AsnSourceSpan? span)
    {
        this.AddDiagnostic(
            InformationFromObjectsDiagnosticCodes.UnknownField,
            "The selected field '" + fieldName
                + "' is not declared by the current object class.",
            span);
    }

    internal void AddResultKindDiagnostic(AsnSourceSpan? span)
    {
        this.AddDiagnostic(
            InformationFromObjectsDiagnosticCodes.ResultKindMismatch,
            "The selected field path does not produce the required information category.",
            span);
    }

    internal void AddAmbiguousSelectionDiagnostic(AsnSourceSpan? span)
    {
        this.AddDiagnostic(
            InformationFromObjectsDiagnosticCodes.AmbiguousSelection,
            "A scalar information selection cannot produce settings from more than one object.",
            span);
    }

    internal void AddFieldGovernorDiagnostic(
        ParsedObjectClassFieldSyntax field,
        AsnSourceSpan? span)
    {
        this.AddDiagnostic(
            InformationFromObjectsDiagnosticCodes.GovernorMismatch,
            "The selected information is not governed by field '"
                + field.Field.Name + "'.",
            span);
    }

    internal string FormatReference(string moduleName, string referenceName)
    {
        return moduleName.Length == 0
            ? referenceName
            : moduleName + "." + referenceName;
    }

    internal void AddDiagnostic(int code, string message, AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            code,
            message,
            span,
            null);
        this.Success = false;
    }
}
