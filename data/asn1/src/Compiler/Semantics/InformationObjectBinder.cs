namespace asn1;

internal class InformationObjectBinder
{
    internal ValueResolver ValueResolver;
    internal DiagnosticBag Diagnostics;
    internal TypeResolver TypeResolver;
    internal SymbolTable[] Tables = [];
    internal System.Collections.Generic.List<AsnObjectClassField> LinkedDefaultStack =
        new System.Collections.Generic.List<AsnObjectClassField>();
    internal bool Success = true;

    internal InformationObjectBinder(ValueResolver valueResolver)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
        this.Diagnostics = valueResolver.Diagnostics;
        this.TypeResolver = valueResolver.TypeResolver;
    }

    internal bool BindObjects(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].ObjectSymbols.Length)
            {
                SymbolTableEntry symbol = tables[tableIndex].ObjectSymbols[symbolIndex];
                symbol.DeclaredObjectSyntax = null;
                symbol.ObjectResolutionState = 0;
                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        if (!this.ValidateLinkedDefaultSettings())
        {
            this.Success = false;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].ObjectSymbols.Length)
            {
                this.ResolveDeclaredObject(tables[tableIndex].ObjectSymbols[symbolIndex]);
                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal bool ValidateLinkedDefaultSettings()
    {
        bool success = true;
        System.Collections.Generic.List<AsnObjectClass> validated =
            new System.Collections.Generic.List<AsnObjectClass>();
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < this.Tables[tableIndex].ObjectClassSymbols.Length)
            {
                SymbolTableEntry symbol = this.Tables[tableIndex]
                    .ObjectClassSymbols[symbolIndex];
                ParsedObjectClassSyntax? objectClass = symbol.ResolvedObjectClassSyntax;
                if (objectClass != null
                    && symbol.DeclaredObjectClassSyntax == objectClass
                    && !validated.Contains(objectClass.ObjectClass))
                {
                    validated.Add(objectClass.ObjectClass);
                    SymbolTable? module = this.ValueResolver.FindDeclaringModule(symbol);
                    if (module != null
                        && !this.ValidateClassLinkedDefaults(
                            objectClass,
                            module,
                            symbol))
                    {
                        success = false;
                    }
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return success;
    }

    internal bool ValidateClassLinkedDefaults(
        ParsedObjectClassSyntax objectClass,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        bool success = true;
        int fieldIndex = 0;
        while (fieldIndex < objectClass.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax field = objectClass.FieldSyntax[fieldIndex];
            if (field.Field.HasDefaultSetting
                && (field.Field.Kind == AsnObjectFieldKind.Object
                    || field.Field.Kind == AsnObjectFieldKind.ObjectSet))
            {
                ParsedObjectFieldSettingSyntax setting =
                    this.CreateDefaultSettingSyntax(field, module);
                if (!this.BindLinkedSetting(
                    setting,
                    field,
                    module,
                    declaration))
                {
                    success = false;
                }
                else if (field.Field.DefaultSetting != null)
                {
                    field.Field.DefaultSetting.Object = setting.Setting.Object;
                    field.Field.DefaultSetting.ObjectSet = setting.Setting.ObjectSet;
                }
            }

            fieldIndex = fieldIndex + 1;
        }

        return success;
    }

    internal ParsedInformationObjectSyntax? ResolveDeclaredObject(
        SymbolTableEntry symbol)
    {
        if (symbol.ObjectResolutionState == 2)
        {
            return symbol.DeclaredObjectSyntax;
        }

        if (symbol.ObjectResolutionState == 1)
        {
            this.AddDiagnostic(
                InformationObjectDiagnosticCodes.ReferenceCycle,
                "The information-object reference cycle containing '"
                    + symbol.QualifiedName + "' cannot be resolved.",
                symbol.Span);
            return null;
        }

        symbol.ObjectResolutionState = 1;
        SymbolTable? module = this.ValueResolver.FindDeclaringModule(symbol);
        ParsedObjectClassSyntax? governor = this.ResolveGovernorClass(symbol);
        if (module == null || governor == null)
        {
            this.AddDiagnostic(
                InformationObjectDiagnosticCodes.UnknownGovernor,
                "The governing object class for information object '"
                    + symbol.QualifiedName + "' could not be resolved.",
                symbol.Span);
            symbol.ObjectResolutionState = 2;
            return null;
        }

        AsnToken[] rightSide = this.GetRightSideTokens(symbol);
        InformationObjectParseResult parsed = new InformationObjectParser(
            rightSide,
            this.Diagnostics,
            true).ParseObjectSyntax(governor);
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != rightSide.Length)
        {
            symbol.ObjectResolutionState = 2;
            this.Success = false;
            return null;
        }

        ParsedInformationObjectSyntax syntax = parsed.Syntax;
        if (!this.BindObjectValue(syntax, module, symbol, governor))
        {
            this.Success = false;
        }

        syntax.Object.Name = symbol.Name;
        syntax.Object.StableId = symbol.QualifiedName;
        syntax.Object.ObjectClass = governor.ObjectClass;
        symbol.DeclaredObjectSyntax = syntax;
        symbol.ObjectResolutionState = 2;
        return syntax;
    }

    internal ParsedObjectClassSyntax? ResolveGovernorClass(SymbolTableEntry symbol)
    {
        SymbolTable? module = this.ValueResolver.FindDeclaringModule(symbol);
        AsnToken[] governorTokens = this.GetObjectGovernorTokens(symbol);
        if (governorTokens.Length == 0 || module == null)
        {
            return null;
        }

        ObjectClassParseResult parsed = new ObjectClassParser(
            governorTokens,
            this.Diagnostics,
            false).ParseObjectClassSyntax();
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != governorTokens.Length
            || parsed.Syntax.ReferenceName.Length == 0)
        {
            return null;
        }

        ParsedObjectClassSyntax? useful = this.ValueResolver.UsefulObjectClasses
            .ResolveUsefulClass(
                parsed.Syntax.ReferenceModule,
                parsed.Syntax.ReferenceName);
        if (useful != null)
        {
            return useful;
        }

        SymbolTableEntry? classSymbol = this.FindSymbol(
            module,
            parsed.Syntax.ReferenceModule,
            parsed.Syntax.ReferenceName,
            AsnAssignmentKind.ObjectClass);
        return classSymbol == null ? null : classSymbol.ResolvedObjectClassSyntax;
    }

    internal AsnToken[] GetObjectGovernorTokens(SymbolTableEntry symbol)
    {
        AsnToken[] tokens = symbol.GetSemanticDeclarationTokens();
        if (tokens.Length < 2)
        {
            return [];
        }

        int start = 1;
        if (tokens[start].Kind == AsnTokenKind.LeftBrace)
        {
            int depth = 0;
            while (start < tokens.Length)
            {
                if (tokens[start].Kind == AsnTokenKind.LeftBrace)
                {
                    depth = depth + 1;
                }
                else if (tokens[start].Kind == AsnTokenKind.RightBrace)
                {
                    depth = depth - 1;
                    if (depth == 0)
                    {
                        start = start + 1;
                        break;
                    }
                }

                start = start + 1;
            }
        }

        int end = start;
        while (end < tokens.Length && tokens[end].Kind != AsnTokenKind.Assignment)
        {
            end = end + 1;
        }

        if (end <= start)
        {
            return [];
        }

        AsnToken[] result = new AsnToken[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = tokens[start + index];
            index = index + 1;
        }

        return result;
    }

    internal bool BindObjectValue(
        ParsedInformationObjectSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governor)
    {
        syntax.Object.ObjectClass = governor.ObjectClass;
        if (syntax.ReferenceName.Length > 0)
        {
            SymbolTableEntry? target = this.FindSymbol(
                module,
                syntax.ReferenceModule,
                syntax.ReferenceName,
                AsnAssignmentKind.Object);
            ParsedInformationObjectSyntax? resolved = target == null
                ? null
                : this.ResolveDeclaredObject(target);
            if (resolved == null)
            {
                this.AddDiagnostic(
                    InformationObjectDiagnosticCodes.InvalidObjectSetting,
                    "The information object '" + this.FormatReference(
                        syntax.ReferenceModule,
                        syntax.ReferenceName) + "' could not be resolved.",
                    syntax.Span);
                return false;
            }

            if (resolved.Object.ObjectClass != governor.ObjectClass)
            {
                this.AddDiagnostic(
                    InformationObjectDiagnosticCodes.ClassMismatch,
                    "The referenced information object is governed by a different object class.",
                    syntax.Span);
                return false;
            }

            syntax.Object.FieldSettings = resolved.Object.FieldSettings;
            syntax.Object.StableId = resolved.Object.StableId;
            syntax.FieldSettingSyntax = resolved.FieldSettingSyntax;
            return true;
        }

        if (syntax.InformationFromObjects != null
            || syntax.ParameterizedReference != null)
        {
            return true;
        }

        return this.BuildFieldSettings(syntax, module, declaration, governor);
    }

    internal bool BuildFieldSettings(
        ParsedInformationObjectSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governor)
    {
        ParsedObjectFieldSettingSyntax[] supplied = syntax.FieldSettingSyntax;
        System.Collections.Generic.List<ParsedObjectFieldSettingSyntax> ordered =
            new System.Collections.Generic.List<ParsedObjectFieldSettingSyntax>();
        bool success = true;
        int suppliedIndex = 0;
        while (suppliedIndex < supplied.Length)
        {
            ParsedObjectFieldSettingSyntax current = supplied[suppliedIndex];
            ParsedObjectClassFieldSyntax? field = this.FindClassFieldSyntax(
                governor,
                current.Setting.FieldName);
            if (field == null || current.Kind == AsnObjectFieldKind.Unknown)
            {
                this.AddDiagnostic(
                    InformationObjectDiagnosticCodes.UnknownSetting,
                    "The field setting '" + current.Setting.FieldName
                        + "' is not declared by the governing object class.",
                    current.Span);
                success = false;
            }

            int earlier = 0;
            while (earlier < suppliedIndex)
            {
                if (supplied[earlier].Setting.FieldName == current.Setting.FieldName)
                {
                    this.AddDiagnostic(
                        InformationObjectDiagnosticCodes.DuplicateSetting,
                        "The field setting '" + current.Setting.FieldName
                            + "' occurs more than once in the information object.",
                        current.Span);
                    success = false;
                    break;
                }

                earlier = earlier + 1;
            }

            suppliedIndex = suppliedIndex + 1;
        }

        SymbolTable? defaultModule = this.FindClassModule(governor);
        int fieldIndex = 0;
        while (fieldIndex < governor.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax field = governor.FieldSyntax[fieldIndex];
            ParsedObjectFieldSettingSyntax? setting = this.FindSettingSyntax(
                supplied,
                field.Field.Name);
            if (setting == null && field.Field.HasDefaultSetting)
            {
                setting = this.CreateDefaultSettingSyntax(
                    field,
                    defaultModule == null ? module : defaultModule);
            }

            if (setting == null)
            {
                if (!field.Field.Optional)
                {
                    this.AddDiagnostic(
                        InformationObjectDiagnosticCodes.MissingRequiredSetting,
                        "The required field setting '" + field.Field.Name
                            + "' is missing from the information object.",
                        syntax.Span);
                    success = false;
                }
            }
            else
            {
                setting.Kind = field.Field.Kind;
                ordered.Add(setting);
            }

            fieldIndex = fieldIndex + 1;
        }

        syntax.FieldSettingSyntax = ordered.ToArray();
        syntax.Object.FieldSettings = this.CopyPublicSettings(syntax.FieldSettingSyntax);
        fieldIndex = 0;
        while (fieldIndex < governor.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax field = governor.FieldSyntax[fieldIndex];
            ParsedObjectFieldSettingSyntax? setting = this.FindSettingSyntax(
                syntax.FieldSettingSyntax,
                field.Field.Name);
            if (setting != null && field.Field.Kind == AsnObjectFieldKind.Type
                && !this.BindTypeSetting(setting, field, module, declaration))
            {
                success = false;
            }

            fieldIndex = fieldIndex + 1;
        }

        fieldIndex = 0;
        while (fieldIndex < governor.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax field = governor.FieldSyntax[fieldIndex];
            ParsedObjectFieldSettingSyntax? setting = this.FindSettingSyntax(
                syntax.FieldSettingSyntax,
                field.Field.Name);
            if (setting != null
                && (field.Field.Kind == AsnObjectFieldKind.Object
                    || field.Field.Kind == AsnObjectFieldKind.ObjectSet)
                && !this.BindLinkedSetting(
                    setting,
                    field,
                    module,
                    declaration))
            {
                success = false;
            }

            fieldIndex = fieldIndex + 1;
        }

        fieldIndex = 0;
        while (fieldIndex < governor.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax field = governor.FieldSyntax[fieldIndex];
            ParsedObjectFieldSettingSyntax? setting = this.FindSettingSyntax(
                syntax.FieldSettingSyntax,
                field.Field.Name);
            if (setting != null
                && field.Field.Kind != AsnObjectFieldKind.Type
                && field.Field.Kind != AsnObjectFieldKind.Object
                && field.Field.Kind != AsnObjectFieldKind.ObjectSet
                && !this.BindGovernedSetting(
                    setting,
                    field,
                    syntax,
                    module,
                    declaration,
                    governor))
            {
                success = false;
            }

            fieldIndex = fieldIndex + 1;
        }

        syntax.Object.FieldSettings = this.CopyPublicSettings(syntax.FieldSettingSyntax);
        return success;
    }

    internal ParsedObjectFieldSettingSyntax CreateDefaultSettingSyntax(
        ParsedObjectClassFieldSyntax field,
        SymbolTable module)
    {
        AsnObjectFieldSetting source = field.Field.DefaultSetting
            ?? new AsnObjectFieldSetting();
        return new ParsedObjectFieldSettingSyntax
        {
            Kind = field.Field.Kind,
            Setting = new AsnObjectFieldSetting
            {
                FieldName = field.Field.Name,
                Type = source.Type,
                ValueSetType = source.ValueSetType,
                Value = source.Value,
                Object = source.Object,
                ObjectSet = source.ObjectSet
            },
            TypeSyntax = field.DefaultTypeSyntax,
            TypeResolution = field.DefaultTypeSyntax == null
                ? null
                : field.DefaultTypeSyntax.Resolution,
            ValueSyntax = field.DefaultValueSyntax,
            SourceTokens = field.DefaultTokens,
            ResolutionModule = module,
            Span = field.Span
        };
    }

    internal bool BindTypeSetting(
        ParsedObjectFieldSettingSyntax setting,
        ParsedObjectClassFieldSyntax field,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        if (setting.TypeSyntax == null)
        {
            this.AddDiagnostic(
                InformationObjectDiagnosticCodes.InvalidTypeSetting,
                "The field '" + field.Field.Name + "' requires a type setting.",
                setting.Span);
            return false;
        }

        SymbolTable resolutionModule = setting.ResolutionModule == null
            ? module
            : setting.ResolutionModule;
        ResolvedTypeSyntax resolved = this.TypeResolver.ResolveNestedType(
            setting.TypeSyntax,
            resolutionModule,
            declaration,
            true);
        if (this.ValueResolver.InstanceOfMaterializer != null)
        {
            this.ValueResolver.InstanceOfMaterializer.MaterializeType(
                resolved,
                resolutionModule);
        }

        setting.TypeResolution = resolved;
        setting.Setting.Type = resolved.EffectiveType;
        if (!resolved.Success || resolved.EffectiveType == null)
        {
            this.AddDiagnostic(
                InformationObjectDiagnosticCodes.InvalidTypeSetting,
                "The type setting for field '" + field.Field.Name
                    + "' could not be resolved.",
                setting.Span);
            return false;
        }

        return true;
    }

    internal bool BindLinkedSetting(
        ParsedObjectFieldSettingSyntax setting,
        ParsedObjectClassFieldSyntax field,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        bool defaultSetting = setting.SourceTokens == field.DefaultTokens;
        if (defaultSetting && this.LinkedDefaultStack.Contains(field.Field))
        {
            this.AddDiagnostic(
                field.Field.Kind == AsnObjectFieldKind.Object
                    ? InformationObjectDiagnosticCodes.InvalidObjectSetting
                    : InformationObjectDiagnosticCodes.InvalidObjectSetSetting,
                "The DEFAULT setting for field '" + field.Field.Name
                    + "' recursively requires itself.",
                setting.Span);
            return false;
        }

        if (defaultSetting)
        {
            this.LinkedDefaultStack.Add(field.Field);
        }

        bool success = this.BindLinkedSettingCore(
            setting,
            field,
            module,
            declaration);
        if (defaultSetting)
        {
            this.LinkedDefaultStack.RemoveAt(this.LinkedDefaultStack.Count - 1);
        }

        return success;
    }

    internal bool BindLinkedSettingCore(
        ParsedObjectFieldSettingSyntax setting,
        ParsedObjectClassFieldSyntax field,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        AsnObjectClass? objectClass = field.Field.ObjectClass;
        ParsedObjectClassSyntax? governor = objectClass == null
            ? null
            : this.FindResolvedClass(objectClass);
        if (governor == null)
        {
            this.AddDiagnostic(
                field.Field.Kind == AsnObjectFieldKind.Object
                    ? InformationObjectDiagnosticCodes.InvalidObjectSetting
                    : InformationObjectDiagnosticCodes.InvalidObjectSetSetting,
                "The governing object class for field '" + field.Field.Name
                    + "' could not be resolved.",
                setting.Span);
            return false;
        }

        SymbolTable resolutionModule = setting.ResolutionModule == null
            ? module
            : setting.ResolutionModule;
        if (field.Field.Kind == AsnObjectFieldKind.Object)
        {
            InformationObjectParseResult parsed = new InformationObjectParser(
                setting.SourceTokens,
                this.Diagnostics,
                false).ParseObjectSyntax(governor);
            if (!parsed.Success
                || parsed.Syntax == null
                || parsed.ConsumedTokenCount != setting.SourceTokens.Length
                || !this.BindObjectValue(
                    parsed.Syntax,
                    resolutionModule,
                    declaration,
                    governor))
            {
                this.AddDiagnostic(
                    InformationObjectDiagnosticCodes.InvalidObjectSetting,
                    "The setting for field '" + field.Field.Name
                        + "' is not an object of its governing class.",
                    setting.Span);
                return false;
            }

            setting.ObjectSyntax = parsed.Syntax;
            setting.Setting.Object = parsed.Syntax.Object;
            return true;
        }

        if (setting.InformationFromObjects != null)
        {
            return true;
        }

        InformationObjectSetParseResult set = new InformationObjectParser(
            setting.SourceTokens,
            this.Diagnostics,
            false).ParseObjectSetSyntax(governor);
        if (!set.Success
            || set.Syntax == null
            || set.ConsumedTokenCount != setting.SourceTokens.Length
            || !this.BindObjectSetValue(
                set.Syntax,
                resolutionModule,
                declaration,
                governor))
        {
            this.AddDiagnostic(
                InformationObjectDiagnosticCodes.InvalidObjectSetSetting,
                "The setting for field '" + field.Field.Name
                    + "' is not an object set of its governing class.",
                setting.Span);
            return false;
        }

        setting.ObjectSetSyntax = set.Syntax;
        setting.Setting.ObjectSet = set.Syntax.ObjectSet;
        return true;
    }

    internal bool BindGovernedSetting(
        ParsedObjectFieldSettingSyntax setting,
        ParsedObjectClassFieldSyntax field,
        ParsedInformationObjectSyntax owner,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governorClass)
    {
        ResolvedTypeSyntax? governor = this.ResolveSettingGovernor(
            field,
            owner,
            governorClass);
        bool value = field.Field.Kind == AsnObjectFieldKind.FixedTypeValue
            || field.Field.Kind == AsnObjectFieldKind.VariableTypeValue;
        if (governor == null || governor.EffectiveType == null)
        {
            this.AddDiagnostic(
                value
                    ? InformationObjectDiagnosticCodes.InvalidValueSetting
                    : InformationObjectDiagnosticCodes.InvalidValueSetSetting,
                "The type governor for field '" + field.Field.Name
                    + "' is not available in this information object.",
                setting.Span);
            return false;
        }

        if (value)
        {
            return this.BindValueSetting(
                setting,
                field,
                governor,
                module,
                declaration);
        }

        return this.BindValueSetSetting(setting, field, governor);
    }

    internal ResolvedTypeSyntax? ResolveSettingGovernor(
        ParsedObjectClassFieldSyntax field,
        ParsedInformationObjectSyntax owner,
        ParsedObjectClassSyntax governorClass)
    {
        if (field.Field.Kind == AsnObjectFieldKind.FixedTypeValue
            || field.Field.Kind == AsnObjectFieldKind.FixedTypeValueSet)
        {
            return field.GovernorTypeSyntax == null
                ? null
                : field.GovernorTypeSyntax.Resolution;
        }

        ParsedInformationObjectSyntax currentObject = owner;
        ParsedObjectClassSyntax currentClass = governorClass;
        int index = 0;
        while (index < field.GovernorFieldPath.Length)
        {
            string name = field.GovernorFieldPath[index];
            ParsedObjectClassFieldSyntax? governorField = this.FindClassFieldSyntax(
                currentClass,
                name);
            ParsedObjectFieldSettingSyntax? setting = this.FindSettingSyntax(
                currentObject.FieldSettingSyntax,
                name);
            if (governorField == null || setting == null)
            {
                return null;
            }

            if (index + 1 == field.GovernorFieldPath.Length)
            {
                return setting.TypeResolution;
            }

            if (setting.ObjectSyntax == null
                || governorField.Field.ObjectClass == null)
            {
                return null;
            }

            ParsedObjectClassSyntax? linked = this.FindResolvedClass(
                governorField.Field.ObjectClass);
            if (linked == null)
            {
                return null;
            }

            currentObject = setting.ObjectSyntax;
            currentClass = linked;
            index = index + 1;
        }

        return null;
    }

    internal bool BindValueSetting(
        ParsedObjectFieldSettingSyntax setting,
        ParsedObjectClassFieldSyntax field,
        ResolvedTypeSyntax governor,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        SymbolTable resolutionModule = setting.ResolutionModule == null
            ? module
            : setting.ResolutionModule;
        ValueParseResult parsed = new ValueParser(
            setting.SourceTokens,
            this.Diagnostics,
            false).ParseValueSyntax(governor.EffectiveType);
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != setting.SourceTokens.Length)
        {
            this.AddDiagnostic(
                InformationObjectDiagnosticCodes.InvalidValueSetting,
                "The setting for field '" + field.Field.Name
                    + "' is not a value of its governing type.",
                setting.Span);
            return false;
        }

        if (parsed.Syntax.InformationFromObjects != null)
        {
            setting.ValueSyntax = parsed.Syntax;
            setting.InformationFromObjects = parsed.Syntax.InformationFromObjects;
            return true;
        }

        ResolvedValueSyntax resolved = this.ValueResolver.ResolveNestedValueSyntax(
            parsed.Syntax,
            governor,
            resolutionModule,
            declaration);
        if (!resolved.Success
            || resolved.EffectiveValue == null
            || !this.HasCompatibleReferenceType(governor, resolved))
        {
            this.AddDiagnostic(
                InformationObjectDiagnosticCodes.InvalidValueSetting,
                "The setting for field '" + field.Field.Name
                    + "' is not a value of its governing type.",
                setting.Span);
            return false;
        }

        setting.ValueSyntax = parsed.Syntax;
        setting.Setting.Value = resolved.EffectiveValue;
        return true;
    }

    internal bool BindValueSetSetting(
        ParsedObjectFieldSettingSyntax setting,
        ParsedObjectClassFieldSyntax field,
        ResolvedTypeSyntax governor)
    {
        if (setting.InformationFromObjects != null)
        {
            return true;
        }

        AsnType? effectiveType = governor.EffectiveType;
        if (setting.SourceTokens.Length < 2 || effectiveType == null)
        {
            this.AddDiagnostic(
                InformationObjectDiagnosticCodes.InvalidValueSetSetting,
                "The field '" + field.Field.Name + "' requires a value-set setting.",
                setting.Span);
            return false;
        }

        AsnToken[] constraintTokens = this.CreateConstraintTokens(setting.SourceTokens);
        ConstraintParseResult parsed = new ConstraintParser(
            constraintTokens,
            this.Diagnostics,
            false).ParseConstraintSyntax(effectiveType);
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != constraintTokens.Length)
        {
            this.AddDiagnostic(
                InformationObjectDiagnosticCodes.InvalidValueSetSetting,
                "The setting for field '" + field.Field.Name
                    + "' is not a value set of its governing type.",
                setting.Span);
            return false;
        }

        AsnType valueSetType = this.TypeResolver.CopyType(
            effectiveType,
            effectiveType.ElementType,
            effectiveType.Components);
        valueSetType.Constraints = [parsed.Syntax.Constraint];
        setting.ValueSetSyntax = parsed.Syntax;
        setting.Setting.ValueSetType = valueSetType;
        return true;
    }

    internal bool BindObjectSetValue(
        ParsedInformationObjectSetSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governor)
    {
        syntax.ObjectSet.ObjectClass = governor.ObjectClass;
        bool success = this.BindObjectSetElement(
            syntax.RootElement,
            module,
            declaration,
            governor);
        if (!this.BindObjectSetElement(
            syntax.AdditionalElement,
            module,
            declaration,
            governor))
        {
            success = false;
        }

        System.Collections.Generic.List<AsnInformationObject> rootObjects =
            new System.Collections.Generic.List<AsnInformationObject>();
        System.Collections.Generic.List<AsnInformationObject> additionalObjects =
            new System.Collections.Generic.List<AsnInformationObject>();
        this.CollectBoundObjects(syntax.RootElement, rootObjects);
        this.CollectBoundObjects(syntax.AdditionalElement, additionalObjects);
        syntax.ObjectSet.RootObjects = rootObjects.ToArray();
        syntax.ObjectSet.AdditionalObjects = additionalObjects.ToArray();
        rootObjects.AddRange(additionalObjects);
        syntax.ObjectSet.Objects = rootObjects.ToArray();
        return success;
    }

    internal bool BindObjectSetElement(
        ParsedObjectSetElementSyntax? element,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governor)
    {
        if (element == null)
        {
            return true;
        }

        bool success = true;
        if (element.Kind == "Object" && element.ObjectSyntax != null)
        {
            if (!this.BindObjectValue(element.ObjectSyntax, module, declaration, governor))
            {
                success = false;
            }
        }
        else if (element.Kind == "ObjectSetReference")
        {
            SymbolTableEntry? target = this.FindSymbol(
                module,
                element.ReferenceModule,
                element.ReferenceName,
                AsnAssignmentKind.ObjectSet);
            ParsedObjectClassSyntax? targetClass = target == null
                ? null
                : this.ResolveGovernorClass(target);
            if (targetClass == null || targetClass.ObjectClass != governor.ObjectClass)
            {
                this.AddDiagnostic(
                    InformationObjectDiagnosticCodes.InvalidObjectSetSetting,
                    "The referenced object set is not governed by the required object class.",
                    element.Span);
                success = false;
            }
        }

        int index = 0;
        while (index < element.Children.Length)
        {
            if (!this.BindObjectSetElement(
                element.Children[index],
                module,
                declaration,
                governor))
            {
                success = false;
            }

            index = index + 1;
        }

        return success;
    }

    internal void CollectBoundObjects(
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
            this.CollectBoundObjects(element.Children[index], objects);
            index = index + 1;
        }
    }

    internal bool HasCompatibleReferenceType(
        ResolvedTypeSyntax expected,
        ResolvedValueSyntax value)
    {
        ResolvedTypeSyntax? actual = value.ReferenceTarget == null
            ? null
            : value.ReferenceTarget.GoverningType;
        if (actual == null)
        {
            return true;
        }

        return expected.EffectiveType == actual.EffectiveType
            || expected.EffectiveKind == actual.EffectiveKind;
    }

    internal ParsedObjectClassFieldSyntax? FindClassFieldSyntax(
        ParsedObjectClassSyntax syntax,
        string name)
    {
        int index = 0;
        while (index < syntax.FieldSyntax.Length)
        {
            if (syntax.FieldSyntax[index].Field.Name == name)
            {
                return syntax.FieldSyntax[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal ParsedObjectFieldSettingSyntax? FindSettingSyntax(
        ParsedObjectFieldSettingSyntax[] settings,
        string name)
    {
        int index = 0;
        while (index < settings.Length)
        {
            if (settings[index].Setting.FieldName == name)
            {
                return settings[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal ParsedObjectClassSyntax? FindResolvedClass(AsnObjectClass objectClass)
    {
        ParsedObjectClassSyntax? useful = this.ValueResolver.UsefulObjectClasses
            .FindUsefulClass(objectClass);
        if (useful != null)
        {
            return useful;
        }

        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < this.Tables[tableIndex].ObjectClassSymbols.Length)
            {
                ParsedObjectClassSyntax? syntax = this.Tables[tableIndex]
                    .ObjectClassSymbols[symbolIndex].ResolvedObjectClassSyntax;
                if (syntax != null && syntax.ObjectClass == objectClass)
                {
                    return syntax;
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return null;
    }

    internal SymbolTable? FindClassModule(ParsedObjectClassSyntax objectClass)
    {
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < this.Tables[tableIndex].ObjectClassSymbols.Length)
            {
                if (this.Tables[tableIndex].ObjectClassSymbols[symbolIndex]
                    .ResolvedObjectClassSyntax == objectClass)
                {
                    return this.Tables[tableIndex];
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return null;
    }

    internal SymbolTableEntry? FindSymbol(
        SymbolTable module,
        string referenceModule,
        string name,
        AsnAssignmentKind kind)
    {
        System.Collections.Generic.List<SymbolTableEntry> matches =
            new System.Collections.Generic.List<SymbolTableEntry>();
        if (referenceModule.Length == 0 || referenceModule == module.ModuleName)
        {
            SymbolTableEntry[] local = module.FindSymbols(name, kind);
            int localIndex = 0;
            while (localIndex < local.Length)
            {
                matches.Add(local[localIndex]);
                localIndex = localIndex + 1;
            }
        }

        if (referenceModule.Length == 0 && matches.Count > 0)
        {
            return matches.Count == 1 ? matches[0] : null;
        }

        int importIndex = 0;
        while (importIndex < module.ImportResolutions.Length)
        {
            ImportResolution import = module.ImportResolutions[importIndex];
            if (referenceModule.Length == 0
                || (import.TargetModule != null
                    && import.TargetModule.ModuleName == referenceModule))
            {
                int bindingIndex = 0;
                while (bindingIndex < import.Symbols.Length)
                {
                    ImportedSymbolBinding binding = import.Symbols[bindingIndex];
                    if (binding.Target != null
                        && binding.Target.Name == name
                        && binding.Kind == kind)
                    {
                        matches.Add(binding.Target);
                    }

                    bindingIndex = bindingIndex + 1;
                }
            }

            importIndex = importIndex + 1;
        }

        return matches.Count == 1 ? matches[0] : null;
    }

    internal AsnObjectFieldSetting[] CopyPublicSettings(
        ParsedObjectFieldSettingSyntax[] settings)
    {
        AsnObjectFieldSetting[] result = new AsnObjectFieldSetting[settings.Length];
        int index = 0;
        while (index < settings.Length)
        {
            result[index] = settings[index].Setting;
            index = index + 1;
        }

        return result;
    }

    internal AsnToken[] GetRightSideTokens(SymbolTableEntry symbol)
    {
        AsnToken[] declarationTokens = symbol.GetSemanticDeclarationTokens();
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

    internal AsnToken[] CreateConstraintTokens(AsnToken[] valueSetTokens)
    {
        AsnToken[] result = new AsnToken[valueSetTokens.Length];
        int index = 0;
        while (index < result.Length)
        {
            AsnToken source = valueSetTokens[index];
            if (index == 0 || index == result.Length - 1)
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
