namespace asn1;

internal class ObjectClassBinder
{
    internal ValueResolver ValueResolver;
    internal DiagnosticBag Diagnostics;
    internal TypeResolver TypeResolver;
    internal SymbolTable[] Tables = [];
    internal System.Collections.Generic.List<AsnObjectClass> ReportedCycles =
        new System.Collections.Generic.List<AsnObjectClass>();
    internal bool Success = true;

    internal ObjectClassBinder(ValueResolver valueResolver)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
        this.Diagnostics = valueResolver.Diagnostics;
        this.TypeResolver = valueResolver.TypeResolver;
    }

    internal bool BindClasses(SymbolTable[] tables)
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
            while (symbolIndex < tables[tableIndex].ObjectClassSymbols.Length)
            {
                SymbolTableEntry symbol = tables[tableIndex].ObjectClassSymbols[symbolIndex];
                symbol.DeclaredObjectClassSyntax = null;
                symbol.ResolvedObjectClassSyntax = null;
                symbol.ObjectClassResolutionState = 0;
                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].ObjectClassSymbols.Length)
            {
                this.ResolveClassSymbol(tables[tableIndex].ObjectClassSymbols[symbolIndex]);
                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        this.ValidateMandatoryClassCycles();

        return this.Success;
    }

    internal ParsedObjectClassSyntax? ResolveClassSymbol(SymbolTableEntry symbol)
    {
        if (symbol.ObjectClassResolutionState == 2)
        {
            return symbol.ResolvedObjectClassSyntax;
        }

        if (symbol.ObjectClassResolutionState == 1)
        {
            if (symbol.DeclaredObjectClassSyntax != null
                && symbol.DeclaredObjectClassSyntax.ReferenceName.Length == 0)
            {
                return symbol.DeclaredObjectClassSyntax;
            }

            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.ReferenceCycle,
                "The object-class reference cycle containing '" + symbol.QualifiedName
                    + "' cannot be resolved.",
                symbol.Span);
            return null;
        }

        symbol.ObjectClassResolutionState = 1;
        SymbolTable? module = this.ValueResolver.FindDeclaringModule(symbol);
        AsnToken[] rightSide = this.GetRightSideTokens(symbol);
        ObjectClassParseResult parsed = new ObjectClassParser(
            rightSide,
            this.Diagnostics,
            true).ParseObjectClassSyntax();
        symbol.DeclaredObjectClassSyntax = parsed.Syntax;
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != rightSide.Length
            || module == null)
        {
            symbol.ObjectClassResolutionState = 2;
            this.Success = false;
            return null;
        }

        ParsedObjectClassSyntax syntax = parsed.Syntax;
        if (syntax.ReferenceName.Length > 0)
        {
            ParsedObjectClassSyntax? useful = this.ValueResolver.UsefulObjectClasses
                .ResolveUsefulClass(syntax.ReferenceModule, syntax.ReferenceName);
            if (useful != null)
            {
                ParsedObjectClassSyntax derived = this.ValueResolver.UsefulObjectClasses
                    .DeriveUsefulClass(symbol.Name, useful);
                symbol.ResolvedObjectClassSyntax = derived;
                symbol.ObjectClassResolutionState = 2;
                return derived;
            }

            SymbolTableEntry? target = this.FindClassSymbol(
                module,
                syntax.ReferenceModule,
                syntax.ReferenceName);
            ParsedObjectClassSyntax? resolved = target == null
                ? null
                : this.ResolveClassSymbol(target);
            if (resolved == null)
            {
                this.AddDiagnostic(
                    ObjectClassDiagnosticCodes.UnknownGovernor,
                    "The object class '" + this.FormatReference(
                        syntax.ReferenceModule,
                        syntax.ReferenceName) + "' could not be resolved.",
                    syntax.Span);
                symbol.ObjectClassResolutionState = 2;
                return null;
            }

            symbol.ResolvedObjectClassSyntax = resolved;
            symbol.ObjectClassResolutionState = 2;
            return resolved;
        }

        syntax.ObjectClass.Name = symbol.Name;
        symbol.ResolvedObjectClassSyntax = syntax;
        this.BindClassDefinition(symbol, module, syntax);
        symbol.ObjectClassResolutionState = 2;
        return syntax;
    }

    internal void BindClassDefinition(
        SymbolTableEntry symbol,
        SymbolTable module,
        ParsedObjectClassSyntax syntax)
    {
        this.ValidateFieldNames(syntax);
        int index = 0;
        while (index < syntax.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax fieldSyntax = syntax.FieldSyntax[index];
            this.BindDirectGovernor(symbol, module, syntax, fieldSyntax);
            index = index + 1;
        }

        index = 0;
        while (index < syntax.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax fieldSyntax = syntax.FieldSyntax[index];
            if (fieldSyntax.Field.Kind == AsnObjectFieldKind.VariableTypeValue
                || fieldSyntax.Field.Kind == AsnObjectFieldKind.VariableTypeValueSet)
            {
                this.BindFieldGovernor(syntax, fieldSyntax);
            }

            index = index + 1;
        }

        this.ValidateUniqueFields(syntax);
        index = 0;
        while (index < syntax.FieldSyntax.Length)
        {
            if (syntax.FieldSyntax[index].Field.Kind == AsnObjectFieldKind.Type)
            {
                this.BindDefaultSetting(symbol, module, syntax, syntax.FieldSyntax[index]);
            }

            index = index + 1;
        }

        index = 0;
        while (index < syntax.FieldSyntax.Length)
        {
            if (syntax.FieldSyntax[index].Field.Kind != AsnObjectFieldKind.Type)
            {
                this.BindDefaultSetting(symbol, module, syntax, syntax.FieldSyntax[index]);
            }

            index = index + 1;
        }

        if (!new SyntaxGrammarCompiler(this.Diagnostics).CompileSyntax(syntax))
        {
            this.Success = false;
        }
    }

    internal void ValidateFieldNames(ParsedObjectClassSyntax syntax)
    {
        int index = 0;
        while (index < syntax.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax current = syntax.FieldSyntax[index];
            int earlier = 0;
            while (earlier < index)
            {
                if (syntax.FieldSyntax[earlier].Field.Name == current.Field.Name)
                {
                    this.AddDiagnostic(
                        ObjectClassDiagnosticCodes.DuplicateField,
                        "The object-class field '" + current.Field.Name
                            + "' is declared more than once.",
                        current.Span);
                    break;
                }

                earlier = earlier + 1;
            }

            if (!this.HasValidFieldCategory(current.Field))
            {
                this.AddDiagnostic(
                    ObjectClassDiagnosticCodes.InvalidFieldCategory,
                    "The field reference '" + current.Field.Name
                        + "' does not use the lexical category required by its field kind.",
                    current.Span);
            }

            index = index + 1;
        }
    }

    internal bool HasValidFieldCategory(AsnObjectClassField field)
    {
        bool upper = field.Name.Length > 1
            && field.Name[1] >= 'A'
            && field.Name[1] <= 'Z';
        if (field.Kind == AsnObjectFieldKind.Type
            || field.Kind == AsnObjectFieldKind.FixedTypeValueSet
            || field.Kind == AsnObjectFieldKind.VariableTypeValueSet
            || field.Kind == AsnObjectFieldKind.ObjectSet)
        {
            return upper;
        }

        return !upper
            && (field.Kind == AsnObjectFieldKind.FixedTypeValue
                || field.Kind == AsnObjectFieldKind.VariableTypeValue
                || field.Kind == AsnObjectFieldKind.Object);
    }

    internal void BindDirectGovernor(
        SymbolTableEntry symbol,
        SymbolTable module,
        ParsedObjectClassSyntax owner,
        ParsedObjectClassFieldSyntax syntax)
    {
        AsnObjectClassField field = syntax.Field;
        if (field.Kind == AsnObjectFieldKind.FixedTypeValue
            || field.Kind == AsnObjectFieldKind.FixedTypeValueSet)
        {
            if (syntax.GovernorTypeSyntax == null)
            {
                this.AddDiagnostic(
                    ObjectClassDiagnosticCodes.InvalidTypeGovernor,
                    "The field '" + field.Name + "' requires a fixed type governor.",
                    syntax.Span);
                return;
            }

            ResolvedTypeSyntax resolved = this.TypeResolver.ResolveNestedType(
                syntax.GovernorTypeSyntax,
                module,
                symbol,
                true);
            if (!resolved.Success || resolved.EffectiveType == null)
            {
                this.Success = false;
                return;
            }

            field.Type = resolved.EffectiveType;
            return;
        }

        if (field.Kind != AsnObjectFieldKind.Object
            && field.Kind != AsnObjectFieldKind.ObjectSet)
        {
            return;
        }

        ParsedObjectClassSyntax? governor = this.ResolveClassReference(
            module,
            syntax.GovernorObjectClassModule,
            syntax.GovernorObjectClassName);
        if (governor == null)
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.UnknownGovernor,
                "The governing object class '" + this.FormatReference(
                    syntax.GovernorObjectClassModule,
                    syntax.GovernorObjectClassName) + "' for field '" + field.Name
                    + "' could not be resolved.",
                syntax.Span);
            return;
        }

        field.ObjectClass = governor.ObjectClass;
    }

    internal void BindFieldGovernor(
        ParsedObjectClassSyntax owner,
        ParsedObjectClassFieldSyntax syntax)
    {
        AsnObjectClassField field = syntax.Field;
        field.GovernorFieldPath = this.CopyNames(syntax.GovernorFieldPath);
        if (syntax.GovernorFieldPath.Length == 0)
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.InvalidFieldGovernor,
                "The variable-type field '" + field.Name
                    + "' requires a field-name governor.",
                syntax.Span);
            return;
        }

        ParsedObjectClassSyntax currentClass = owner;
        AsnObjectClassField? target = null;
        int index = 0;
        while (index < syntax.GovernorFieldPath.Length)
        {
            target = this.FindField(currentClass, syntax.GovernorFieldPath[index]);
            if (target == null)
            {
                this.AddDiagnostic(
                    ObjectClassDiagnosticCodes.InvalidFieldGovernor,
                    "The governor path for field '" + field.Name
                        + "' contains the unknown field '"
                        + syntax.GovernorFieldPath[index] + "'.",
                    syntax.Span);
                return;
            }

            if (index + 1 < syntax.GovernorFieldPath.Length)
            {
                if (target.Kind != AsnObjectFieldKind.Object
                    || target.ObjectClass == null)
                {
                    this.AddDiagnostic(
                        ObjectClassDiagnosticCodes.InvalidFieldGovernor,
                        "Every intermediate governor field for '" + field.Name
                            + "' must be an object field with a resolved class.",
                        syntax.Span);
                    return;
                }

                ParsedObjectClassSyntax? linked = this.FindResolvedClass(target.ObjectClass);
                if (linked == null)
                {
                    this.AddDiagnostic(
                        ObjectClassDiagnosticCodes.InvalidFieldGovernor,
                        "The linked object class in the governor for field '"
                            + field.Name + "' could not be located.",
                        syntax.Span);
                    return;
                }

                currentClass = linked;
            }

            index = index + 1;
        }

        if (target == null || target.Kind != AsnObjectFieldKind.Type)
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.InvalidFieldGovernor,
                "The governor path for field '" + field.Name
                    + "' must end at a type field.",
                syntax.Span);
            return;
        }

        this.ValidateVariableOptionality(field, target, syntax.Span);
    }

    internal void ValidateVariableOptionality(
        AsnObjectClassField field,
        AsnObjectClassField target,
        AsnSourceSpan? span)
    {
        if (target.Optional && !field.Optional)
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.InvalidOptionality,
                "The field '" + field.Name
                    + "' must be OPTIONAL because its governing type field is OPTIONAL.",
                span);
        }

        if (field.HasDefaultSetting
            && (!target.HasDefaultSetting
                || target.DefaultSetting == null
                || target.DefaultSetting.Type == null))
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.InvalidDefaultSetting,
                "The field '" + field.Name
                    + "' can have a DEFAULT only when its governing type field has a DEFAULT type.",
                span);
        }
    }

    internal void ValidateUniqueFields(ParsedObjectClassSyntax syntax)
    {
        int uniqueCount = 0;
        int index = 0;
        while (index < syntax.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax fieldSyntax = syntax.FieldSyntax[index];
            AsnObjectClassField field = fieldSyntax.Field;
            if (field.Unique)
            {
                uniqueCount = uniqueCount + 1;
                if (field.Kind != AsnObjectFieldKind.FixedTypeValue
                    || field.HasDefaultSetting)
                {
                    this.AddDiagnostic(
                        ObjectClassDiagnosticCodes.InvalidUniqueField,
                        "A UNIQUE field must be a fixed-type value field and cannot have a DEFAULT setting.",
                        fieldSyntax.Span);
                }

                if (uniqueCount > 1)
                {
                    this.AddDiagnostic(
                        ObjectClassDiagnosticCodes.InvalidUniqueField,
                        "An object class can define at most one UNIQUE identifier field.",
                        fieldSyntax.Span);
                }
            }

            index = index + 1;
        }
    }

    internal void BindDefaultSetting(
        SymbolTableEntry symbol,
        SymbolTable module,
        ParsedObjectClassSyntax owner,
        ParsedObjectClassFieldSyntax syntax)
    {
        AsnObjectClassField field = syntax.Field;
        if (!field.HasDefaultSetting || field.DefaultSetting == null)
        {
            return;
        }

        if (field.Optional)
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.InvalidOptionality,
                "The field '" + field.Name + "' cannot be both OPTIONAL and DEFAULT.",
                syntax.Span);
            return;
        }

        if (field.Kind == AsnObjectFieldKind.Type)
        {
            this.BindDefaultType(symbol, module, syntax);
            return;
        }

        if (field.Kind == AsnObjectFieldKind.FixedTypeValue)
        {
            this.BindDefaultValue(symbol, module, syntax, syntax.GovernorTypeSyntax);
            return;
        }

        if (field.Kind == AsnObjectFieldKind.VariableTypeValue)
        {
            ParsedObjectClassFieldSyntax? targetSyntax =
                this.ResolveFinalGovernorSyntax(owner, syntax);
            this.BindDefaultValue(
                symbol,
                module,
                syntax,
                targetSyntax == null ? null : targetSyntax.DefaultTypeSyntax);
            return;
        }

        if (field.Kind == AsnObjectFieldKind.FixedTypeValueSet
            || field.Kind == AsnObjectFieldKind.VariableTypeValueSet)
        {
            this.BindDefaultValueSet(owner, syntax);
        }
    }

    internal void BindDefaultType(
        SymbolTableEntry symbol,
        SymbolTable module,
        ParsedObjectClassFieldSyntax syntax)
    {
        if (syntax.DefaultTypeSyntax == null || syntax.Field.DefaultSetting == null)
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.InvalidDefaultSetting,
                "The type field '" + syntax.Field.Name + "' has no default type.",
                syntax.Span);
            return;
        }

        ResolvedTypeSyntax resolved = this.TypeResolver.ResolveNestedType(
            syntax.DefaultTypeSyntax,
            module,
            symbol,
            true);
        if (!resolved.Success || resolved.EffectiveType == null)
        {
            this.Success = false;
            return;
        }

        syntax.Field.DefaultSetting.Type = resolved.EffectiveType;
    }

    internal void BindDefaultValue(
        SymbolTableEntry symbol,
        SymbolTable module,
        ParsedObjectClassFieldSyntax syntax,
        ParsedTypeSyntax? governorSyntax)
    {
        if (syntax.Field.DefaultSetting == null || governorSyntax == null)
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.InvalidDefaultSetting,
                "The value field '" + syntax.Field.Name
                    + "' has no resolvable default value governor.",
                syntax.Span);
            return;
        }

        ResolvedTypeSyntax governor = this.TypeResolver.ResolveNestedType(
            governorSyntax,
            module,
            symbol,
            true);
        ValueParseResult parsed = new ValueParser(
            syntax.DefaultTokens,
            this.Diagnostics,
            true).ParseValueSyntax(governor.EffectiveType);
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != syntax.DefaultTokens.Length)
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.InvalidDefaultSetting,
                "The DEFAULT setting for field '" + syntax.Field.Name
                    + "' is not a value of its governing type.",
                syntax.Span);
            return;
        }

        syntax.DefaultValueSyntax = parsed.Syntax;
        ResolvedValueSyntax resolved = this.ValueResolver.ResolveNestedValueSyntax(
            parsed.Syntax,
            governor,
            module,
            symbol);
        if (!governor.Success || !resolved.Success || resolved.EffectiveValue == null)
        {
            this.Success = false;
            return;
        }

        syntax.Field.DefaultSetting.Value = resolved.EffectiveValue;
    }

    internal void BindDefaultValueSet(
        ParsedObjectClassSyntax owner,
        ParsedObjectClassFieldSyntax syntax)
    {
        if (syntax.DefaultTokens.Length < 2)
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.InvalidDefaultSetting,
                "The value-set field '" + syntax.Field.Name
                    + "' has no default value-set setting.",
                syntax.Span);
            return;
        }

        AsnType? governor = syntax.Field.Type;
        if (syntax.Field.Kind == AsnObjectFieldKind.VariableTypeValueSet)
        {
            ParsedObjectClassFieldSyntax? target =
                this.ResolveFinalGovernorSyntax(owner, syntax);
            governor = target == null || target.Field.DefaultSetting == null
                ? null
                : target.Field.DefaultSetting.Type;
        }

        if (governor == null)
        {
            return;
        }

        AsnToken[] constraintTokens = this.CreateConstraintTokens(syntax.DefaultTokens);
        ConstraintParseResult parsed = new ConstraintParser(
            constraintTokens,
            this.Diagnostics,
            true).ParseConstraintSyntax(governor);
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != constraintTokens.Length)
        {
            this.AddDiagnostic(
                ObjectClassDiagnosticCodes.InvalidDefaultSetting,
                "The DEFAULT setting for field '" + syntax.Field.Name
                    + "' is not a value set of its governing type.",
                syntax.Span);
        }
    }

    internal ParsedObjectClassFieldSyntax? ResolveFinalGovernorSyntax(
        ParsedObjectClassSyntax owner,
        ParsedObjectClassFieldSyntax syntax)
    {
        ParsedObjectClassSyntax current = owner;
        ParsedObjectClassFieldSyntax? target = null;
        int index = 0;
        while (index < syntax.GovernorFieldPath.Length)
        {
            AsnObjectClassField? field = this.FindField(
                current,
                syntax.GovernorFieldPath[index]);
            target = field == null ? null : this.FindFieldSyntax(current, field);
            if (target == null)
            {
                return null;
            }

            if (index + 1 < syntax.GovernorFieldPath.Length)
            {
                if (target.Field.ObjectClass == null)
                {
                    return null;
                }

                ParsedObjectClassSyntax? linked = this.FindResolvedClass(
                    target.Field.ObjectClass);
                if (linked == null)
                {
                    return null;
                }

                current = linked;
            }

            index = index + 1;
        }

        return target;
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

    internal ParsedObjectClassFieldSyntax? FindFieldSyntax(
        ParsedObjectClassSyntax owner,
        AsnObjectClassField field)
    {
        int index = 0;
        while (index < owner.FieldSyntax.Length)
        {
            if (owner.FieldSyntax[index].Field == field)
            {
                return owner.FieldSyntax[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnObjectClassField? FindField(
        ParsedObjectClassSyntax syntax,
        string name)
    {
        int index = 0;
        while (index < syntax.ObjectClass.Fields.Length)
        {
            if (syntax.ObjectClass.Fields[index].Name == name)
            {
                return syntax.ObjectClass.Fields[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal ParsedObjectClassSyntax? ResolveClassReference(
        SymbolTable module,
        string referenceModule,
        string referenceName)
    {
        ParsedObjectClassSyntax? useful = this.ValueResolver.UsefulObjectClasses
            .ResolveUsefulClass(referenceModule, referenceName);
        if (useful != null)
        {
            return useful;
        }

        SymbolTableEntry? symbol = this.FindClassSymbol(
            module,
            referenceModule,
            referenceName);
        return symbol == null ? null : this.ResolveClassSymbol(symbol);
    }

    internal SymbolTableEntry? FindClassSymbol(
        SymbolTable module,
        string referenceModule,
        string referenceName)
    {
        System.Collections.Generic.List<SymbolTableEntry> matches =
            new System.Collections.Generic.List<SymbolTableEntry>();
        if (referenceModule.Length == 0 || referenceModule == module.ModuleName)
        {
            SymbolTableEntry[] local = module.FindSymbols(
                referenceName,
                AsnAssignmentKind.ObjectClass);
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
                        && binding.Target.Name == referenceName
                        && binding.Kind == AsnAssignmentKind.ObjectClass)
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

    internal void ValidateMandatoryClassCycles()
    {
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < this.Tables[tableIndex].ObjectClassSymbols.Length)
            {
                ParsedObjectClassSyntax? syntax = this.Tables[tableIndex]
                    .ObjectClassSymbols[symbolIndex].ResolvedObjectClassSyntax;
                if (syntax != null && !this.ReportedCycles.Contains(syntax.ObjectClass))
                {
                    this.InspectMandatoryClassCycle(
                        syntax,
                        new System.Collections.Generic.List<AsnObjectClass>());
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }
    }

    internal void InspectMandatoryClassCycle(
        ParsedObjectClassSyntax syntax,
        System.Collections.Generic.List<AsnObjectClass> path)
    {
        if (path.Contains(syntax.ObjectClass))
        {
            return;
        }

        path.Add(syntax.ObjectClass);
        int fieldIndex = 0;
        while (fieldIndex < syntax.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax fieldSyntax = syntax.FieldSyntax[fieldIndex];
            AsnObjectClassField field = fieldSyntax.Field;
            if ((field.Kind == AsnObjectFieldKind.Object
                    || field.Kind == AsnObjectFieldKind.ObjectSet)
                && !field.Optional
                && !field.HasDefaultSetting
                && field.ObjectClass != null)
            {
                if (path.Contains(field.ObjectClass))
                {
                    if (!this.ReportedCycles.Contains(field.ObjectClass))
                    {
                        this.AddDiagnostic(
                            ObjectClassDiagnosticCodes.InvalidOptionality,
                            "A recursive chain of object-class link fields must contain an OPTIONAL or DEFAULT field.",
                            fieldSyntax.Span);
                        this.RememberCycleClasses(path);
                    }
                }
                else
                {
                    ParsedObjectClassSyntax? linked = this.FindResolvedClass(field.ObjectClass);
                    if (linked != null)
                    {
                        this.InspectMandatoryClassCycle(linked, path);
                    }
                }
            }

            fieldIndex = fieldIndex + 1;
        }

        path.RemoveAt(path.Count - 1);
    }

    internal void RememberCycleClasses(
        System.Collections.Generic.List<AsnObjectClass> path)
    {
        int index = 0;
        while (index < path.Count)
        {
            if (!this.ReportedCycles.Contains(path[index]))
            {
                this.ReportedCycles.Add(path[index]);
            }

            index = index + 1;
        }
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

    internal string[] CopyNames(string[] names)
    {
        string[] result = new string[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            result[index] = names[index];
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
