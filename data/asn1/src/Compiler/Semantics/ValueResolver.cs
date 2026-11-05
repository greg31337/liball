namespace asn1;

internal class ValueResolver
{
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];
    internal TypeResolver TypeResolver;
    internal UsefulObjectClassCatalog UsefulObjectClasses;
    internal InstanceOfMaterializer? InstanceOfMaterializer;
    internal bool ParameterIntegrationRerun;
    internal System.Collections.Generic.List<AsnNamedValue> ResolvingNamedValues =
        new System.Collections.Generic.List<AsnNamedValue>();

    internal ValueResolver(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
        this.TypeResolver = new TypeResolver(this.Diagnostics);
        this.UsefulObjectClasses = new UsefulObjectClassCatalog(this.Diagnostics);
    }

    internal ValueResolutionResult ResolveValues(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        this.TypeResolver.Tables = tables;
        System.Collections.Generic.List<SymbolTableEntry> values =
            new System.Collections.Generic.List<SymbolTableEntry>();
        bool success = this.PrepareValueAssignments(values);
        if (!this.ResolveAllNamedValues())
        {
            success = false;
        }

        ObjectClassBinder classBinder = new ObjectClassBinder(this);
        if (!classBinder.BindClasses(tables))
        {
            success = false;
        }

        this.InstanceOfMaterializer = new InstanceOfMaterializer(this, classBinder);
        if (!this.InstanceOfMaterializer.MaterializeTypes(tables))
        {
            success = false;
        }

        if (!this.ReparseValueAssignments(values))
        {
            success = false;
        }

        InformationObjectBinder objectBinder = new InformationObjectBinder(this);
        if (!objectBinder.BindObjects(tables))
        {
            success = false;
        }

        ObjectSetEvaluator objectSetEvaluator = new ObjectSetEvaluator(
            this,
            objectBinder);
        if (!objectSetEvaluator.BindObjectSets(tables))
        {
            success = false;
        }

        if (!new InformationFromObjectsResolver(
            this,
            objectBinder,
            objectSetEvaluator).ResolveInformation(tables))
        {
            success = false;
        }

        if (!new ComponentConstraintCompiler(this.Diagnostics).CompileTypes(tables))
        {
            success = false;
        }

        if (!new TypeContentsConstraintCompiler(this).CompileTypes(tables))
        {
            success = false;
        }

        if (!new TableConstraintCompiler(this).CompileTypes(tables))
        {
            success = false;
        }

        if (!new OpaqueConstraintCompiler(this).CompileTypes(tables))
        {
            success = false;
        }

        if (!new SchemaRuleValidator(this.Diagnostics).ValidateTables(this.Tables))
        {
            success = false;
        }

        if (!this.ResolveAllDefaultValues())
        {
            success = false;
        }

        int index = 0;
        while (index < values.Count)
        {
            SymbolTableEntry entry = values[index];
            SymbolTable? module = this.FindDeclaringModule(entry);
            if (module == null || !this.ResolveValueEntry(entry, module).Success)
            {
                success = false;
            }

            index = index + 1;
        }

        if (!new ConstraintSolver(this).SolveTypes(tables))
        {
            success = false;
        }

        if (!new PatternConstraintCompiler(this.Diagnostics).CompileTypes(tables))
        {
            success = false;
        }

        if (!new TimeSettingsCompiler(this.Diagnostics).CompileTypes(tables))
        {
            success = false;
        }

        new EncodingConstraintCompiler(this).CompileTypes(tables);

        if (!new ConstraintValidator(this).ValidateTypes(tables))
        {
            success = false;
        }

        if (!new ActualParameterValidator(this).ValidateReferences(tables))
        {
            success = false;
        }

        if (!this.ParameterIntegrationRerun
            && new ParameterInstantiationIntegrator().Integrate(tables))
        {
            this.ParameterIntegrationRerun = true;
            TypeResolutionResult integratedTypes = this.TypeResolver.ResolveTypes(tables);
            ValueResolutionResult integratedValues = this.ResolveValues(tables);
            if (!integratedTypes.Success)
            {
                integratedValues.Success = false;
            }

            return integratedValues;
        }

        if (this.ParameterIntegrationRerun
            && !new ParameterInstantiationSchemaValidator(this.Diagnostics)
                .ValidateTables(tables))
        {
            success = false;
        }

        return new ValueResolutionResult
        {
            Values = values.ToArray(),
            Success = success
        };
    }

    internal bool PrepareValueAssignments(
        System.Collections.Generic.List<SymbolTableEntry> values)
    {
        bool success = true;
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            SymbolTable table = this.Tables[tableIndex];
            int symbolIndex = 0;
            while (symbolIndex < table.ValueSymbols.Length)
            {
                SymbolTableEntry entry = table.ValueSymbols[symbolIndex];
                entry.ValueResolutionState = 0;
                entry.GoverningType = null;
                entry.DeclaredValueSyntax = null;
                entry.ResolvedValue = null;
                if (!this.PrepareValueAssignment(entry, table))
                {
                    success = false;
                }

                values.Add(entry);
                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return success;
    }

    internal bool PrepareValueAssignment(SymbolTableEntry entry, SymbolTable module)
    {
        AssignmentHeadAlternativeSyntax? declared = entry.DeclaredReference;
        ParsedTypeSyntax? governor = declared == null ? null : declared.GovernorType;
        if (governor == null && declared != null && declared.GovernorTokens.Length > 0)
        {
            TypeParseResult parsedGovernor = new TypeParser(
                declared.GovernorTokens,
                this.Diagnostics,
                true).ParseTypeSyntax();
            if (parsedGovernor.Success
                && parsedGovernor.Syntax != null
                && parsedGovernor.ConsumedTokenCount == declared.GovernorTokens.Length)
            {
                governor = parsedGovernor.Syntax;
            }
        }

        if (governor == null)
        {
            this.AddValueDiagnostic(
                SchemaDiagnosticCodes.MissingValueGovernor,
                "Value assignment '" + entry.QualifiedName + "' has no resolvable governing type.",
                entry.Span,
                null);
            return false;
        }

        ResolvedTypeSyntax governingType = this.TypeResolver.ResolveNestedType(
            governor,
            module,
            entry,
            true);
        entry.GoverningType = governingType;
        if (!governingType.Success || governingType.EffectiveType == null)
        {
            return false;
        }

        this.ParsePreparedValueAssignment(entry, governingType, false);
        return true;
    }

    internal bool ReparseValueAssignments(
        System.Collections.Generic.List<SymbolTableEntry> values)
    {
        bool success = true;
        int index = 0;
        while (index < values.Count)
        {
            SymbolTableEntry entry = values[index];
            ResolvedTypeSyntax? governingType = entry.GoverningType;
            entry.DeclaredValueSyntax = null;
            entry.ResolvedValue = null;
            entry.ValueResolutionState = 0;
            if (governingType == null
                || !this.ParsePreparedValueAssignment(entry, governingType, true))
            {
                success = false;
            }

            index = index + 1;
        }

        return success;
    }

    internal bool ParsePreparedValueAssignment(
        SymbolTableEntry entry,
        ResolvedTypeSyntax governingType,
        bool reportDiagnostics)
    {
        AsnToken[] tokens = this.GetValueRightSideTokens(entry);
        ValueParseResult parsedValue = new ValueParser(
            tokens,
            this.Diagnostics,
            reportDiagnostics).ParseValueSyntax(governingType.EffectiveType);
        if (!parsedValue.Success
            || parsedValue.Syntax == null
            || parsedValue.ConsumedTokenCount != tokens.Length)
        {
            if (reportDiagnostics
                && parsedValue.Success
                && parsedValue.ConsumedTokenCount != tokens.Length)
            {
                AsnSourceSpan? span = parsedValue.ConsumedTokenCount < tokens.Length
                    ? tokens[parsedValue.ConsumedTokenCount].Span
                    : entry.Span;
                this.AddValueDiagnostic(
                    SchemaDiagnosticCodes.UnsupportedValueNotation,
                    "The value assignment contains trailing or unsupported value notation.",
                    span,
                    null);
            }

            return false;
        }

        entry.DeclaredValueSyntax = parsedValue.Syntax;
        return true;
    }

    internal AsnToken[] GetValueRightSideTokens(SymbolTableEntry entry)
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

    internal ResolvedValueSyntax ResolveValueEntry(
        SymbolTableEntry entry,
        SymbolTable module)
    {
        if (entry.ResolvedValue != null && entry.ValueResolutionState == 2)
        {
            return entry.ResolvedValue;
        }

        if (entry.ResolvedValue != null && entry.ValueResolutionState == 1)
        {
            this.AddValueCycleDiagnostic(entry);
            return entry.ResolvedValue;
        }

        ParsedValueSyntax? syntax = entry.DeclaredValueSyntax;
        if (syntax == null)
        {
            ResolvedValueSyntax failed = new ResolvedValueSyntax
            {
                SourceDeclaration = entry,
                GoverningType = entry.GoverningType
            };
            entry.ResolvedValue = failed;
            entry.ValueResolutionState = 2;
            return failed;
        }

        ResolvedValueSyntax resolution = this.CreateValueResolution(
            syntax,
            entry.GoverningType,
            entry);
        entry.ResolvedValue = resolution;
        syntax.Resolution = resolution;
        entry.ValueResolutionState = 1;
        this.EvaluateAndValidateValueSyntax(
            syntax,
            entry.GoverningType,
            module,
            entry,
            resolution);
        entry.ValueResolutionState = 2;
        return resolution;
    }

    internal ResolvedValueSyntax CreateValueResolution(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTableEntry declaration)
    {
        return new ResolvedValueSyntax
        {
            SourceSyntax = syntax,
            SourceValue = syntax.Value,
            GoverningType = governingType,
            SourceDeclaration = declaration,
            EffectiveDeclaration = declaration
        };
    }

    internal void EvaluateValueSyntax(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        if (syntax.IntegerExpression)
        {
            this.EvaluateIntegerExpression(
                syntax,
                governingType,
                module,
                declaration,
                resolution);
            return;
        }

        if (syntax.Value.Kind == AsnValueKind.Defined)
        {
            this.EvaluateDefinedValue(
                syntax,
                governingType,
                module,
                declaration,
                resolution);
            return;
        }

        switch (syntax.Value.Kind)
        {
            case AsnValueKind.Integer:
                this.EvaluateIntegerLiteral(syntax, resolution);
                return;
            case AsnValueKind.Real:
                this.EvaluateRealValue(syntax, resolution);
                return;
            case AsnValueKind.Enumerated:
                this.EvaluateEnumeratedValue(
                    syntax,
                    governingType,
                    module,
                    declaration,
                    resolution);
                return;
            case AsnValueKind.BitString:
                this.EvaluateBitStringValue(
                    syntax,
                    governingType,
                    module,
                    declaration,
                    resolution);
                return;
            case AsnValueKind.ObjectIdentifier:
            case AsnValueKind.RelativeObjectIdentifier:
                this.EvaluateObjectIdentifierValue(
                    syntax,
                    governingType,
                    module,
                    declaration,
                    resolution);
                return;
            case AsnValueKind.Sequence:
            case AsnValueKind.Set:
            case AsnValueKind.External:
            case AsnValueKind.EmbeddedPdv:
            case AsnValueKind.InstanceOf:
                this.EvaluateStructuredValue(
                    syntax,
                    governingType,
                    module,
                    declaration,
                    resolution);
                return;
            case AsnValueKind.SequenceOf:
            case AsnValueKind.SetOf:
                this.EvaluateCollectionValue(
                    syntax,
                    governingType,
                    module,
                    declaration,
                    resolution);
                return;
            case AsnValueKind.Choice:
                this.EvaluateChoiceValue(
                    syntax,
                    governingType,
                    module,
                    declaration,
                    resolution);
                return;
            case AsnValueKind.OpenType:
                this.EvaluateOpenTypeValue(
                    syntax,
                    governingType,
                    module,
                    declaration,
                    resolution);
                return;
            default:
                resolution.EffectiveValue = this.CopyValue(syntax.Value);
                resolution.Octets = this.CopyBytes(syntax.Octets);
                resolution.Success = true;
                return;
        }
    }

    internal void EvaluateAndValidateValueSyntax(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        this.EvaluateValueSyntax(
            syntax,
            governingType,
            module,
            declaration,
            resolution);
        if (resolution.Success
            && !new SemanticValueValidator(this.Diagnostics)
                .ValidateResolvedValue(resolution))
        {
            resolution.Success = false;
        }
    }

    internal void EvaluateIntegerExpression(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        IntegerEvaluationResult evaluated = new IntegerExpressionEvaluator(
            this,
            syntax.SourceTokens,
            module,
            declaration,
            governingType).EvaluateExpression();
        if (!evaluated.Success)
        {
            return;
        }

        resolution.EffectiveValue = new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = evaluated.Value,
            TextValue = syntax.Value.TextValue
        };
        resolution.Success = true;
    }

    internal void EvaluateIntegerLiteral(
        ParsedValueSyntax syntax,
        ResolvedValueSyntax resolution)
    {
        if (!syntax.Value.HasIntegerValue)
        {
            this.AddValueDiagnostic(
                SchemaDiagnosticCodes.IntegerValueMissing,
                "An INTEGER value did not produce an integer.",
                syntax.Span,
                null);
            return;
        }

        resolution.EffectiveValue = this.CopyValue(syntax.Value);
        resolution.Success = true;
    }

    internal void EvaluateRealValue(
        ParsedValueSyntax syntax,
        ResolvedValueSyntax resolution)
    {
        RealNormalizationResult normalized = new RealValueNormalizer()
            .NormalizeRealValue(syntax.Value.RealValue);
        if (!normalized.Success || normalized.Value == null)
        {
            this.AddValueDiagnostic(
                SchemaDiagnosticCodes.RealValueInvalid,
                normalized.ErrorMessage,
                syntax.Span,
                null);
            return;
        }

        AsnValue value = this.CopyValue(syntax.Value);
        value.RealValue = normalized.Value;
        resolution.EffectiveValue = value;
        resolution.Success = true;
    }

    internal void EvaluateDefinedValue(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        if (governingType != null
            && (governingType.EffectiveKind == AsnTypeKind.Integer
                || governingType.EffectiveKind == AsnTypeKind.Enumerated))
        {
            IntegerEvaluationResult named = this.FindNamedIntegerValue(
                governingType,
                syntax.ReferenceName,
                module,
                declaration,
                syntax.Span);
            if (named.Success)
            {
                resolution.EffectiveValue = new AsnValue
                {
                    Kind = governingType.EffectiveKind == AsnTypeKind.Enumerated
                        ? AsnValueKind.Enumerated
                        : AsnValueKind.Integer,
                    HasIntegerValue = true,
                    IntegerValue = named.Value,
                    TextValue = syntax.Value.TextValue
                };
                resolution.Success = true;
                return;
            }
        }

        ValueReferenceBinding binding = this.FindValueBinding(
            module,
            syntax.ReferenceModule,
            syntax.ReferenceName);
        if (binding.Target == null || binding.Module == null)
        {
            this.AddValueReferenceDiagnostic(syntax, binding.Ambiguous);
            return;
        }

        ResolvedValueSyntax target = this.ResolveValueEntry(binding.Target, binding.Module);
        resolution.ReferenceTarget = target;
        resolution.EffectiveDeclaration = target.EffectiveDeclaration;
        if (!target.Success || target.EffectiveValue == null)
        {
            return;
        }

        resolution.EffectiveValue = this.CopyValue(target.EffectiveValue);
        resolution.Items = this.CopyResolvedItems(target.Items);
        resolution.ComponentValues = this.CopyComponentValues(target.ComponentValues);
        resolution.ItemNames = this.CopyStrings(target.ItemNames);
        resolution.NamedCollection = target.NamedCollection;
        resolution.BitStringValue = this.CopyBitString(target.BitStringValue);
        resolution.ObjectIdentifierValue = this.CopyObjectIdentifier(
            target.ObjectIdentifierValue);
        resolution.Octets = this.CopyBytes(target.Octets);
        resolution.Success = true;
    }

    internal void EvaluateEnumeratedValue(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        IntegerEvaluationResult named = this.FindNamedIntegerValue(
            governingType,
            syntax.Value.TextValue,
            module,
            declaration,
            syntax.Span);
        AsnValue value = this.CopyValue(syntax.Value);
        if (named.Success)
        {
            value.HasIntegerValue = true;
            value.IntegerValue = named.Value;
        }

        resolution.EffectiveValue = value;
        resolution.Success = true;
    }

    internal IntegerEvaluationResult ResolveIntegerReference(
        string moduleName,
        string referenceName,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedTypeSyntax? governingType,
        AsnToken? token)
    {
        if (moduleName.Length == 0)
        {
            IntegerEvaluationResult named = this.FindNamedIntegerValue(
                governingType,
                referenceName,
                module,
                declaration,
                token == null ? null : token.Span);
            if (named.Success)
            {
                return named;
            }
        }

        ValueReferenceBinding binding = this.FindValueBinding(
            module,
            moduleName,
            referenceName);
        if (binding.Target == null || binding.Module == null)
        {
            this.AddIntegerReferenceDiagnostic(
                moduleName,
                referenceName,
                binding.Ambiguous,
                token == null ? null : token.Span);
            return new IntegerEvaluationResult();
        }

        ResolvedValueSyntax target = this.ResolveValueEntry(binding.Target, binding.Module);
        AsnValue? value = target.EffectiveValue;
        if (!target.Success || value == null || !value.HasIntegerValue)
        {
            this.AddValueDiagnostic(
                SchemaDiagnosticCodes.NonIntegerValueReference,
                "Value reference '" + this.QualifyReference(moduleName, referenceName)
                    + "' does not resolve to an integer.",
                token == null ? null : token.Span,
                null);
            return new IntegerEvaluationResult();
        }

        return new IntegerEvaluationResult
        {
            Success = true,
            Value = value.IntegerValue
        };
    }

    internal IntegerEvaluationResult FindNamedIntegerValue(
        ResolvedTypeSyntax? governingType,
        string name,
        SymbolTable module,
        SymbolTableEntry declaration,
        AsnSourceSpan? span)
    {
        if (governingType == null || name.Length == 0)
        {
            return new IntegerEvaluationResult();
        }

        ResolvedTypeSyntax? current = governingType;
        int depth = 0;
        while (current != null && depth <= this.Tables.Length + 64)
        {
            ParsedTypeSyntax? source = current.SourceSyntax;
            if (source != null)
            {
                AsnNamedValue? item = this.FindNamedValueInArray(
                    source.RootNamedValues,
                    name);
                if (item == null)
                {
                    item = this.FindNamedValueInArray(source.ExtensionNamedValues, name);
                }

                if (item != null)
                {
                    SymbolTableEntry namedDeclaration = current.SourceDeclaration == null
                        ? declaration
                        : current.SourceDeclaration;
                    SymbolTable? namedModule = this.FindDeclaringModule(namedDeclaration);
                    return this.ResolveNamedIntegerItem(
                        item,
                        namedModule == null ? module : namedModule,
                        namedDeclaration,
                        current,
                        span);
                }
            }

            current = current.AliasTarget;
            depth = depth + 1;
        }

        return new IntegerEvaluationResult();
    }

    internal AsnNamedValue? FindNamedValueInArray(
        AsnNamedValue[] values,
        string name)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Name == name)
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal IntegerEvaluationResult ResolveNamedIntegerItem(
        AsnNamedValue item,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedTypeSyntax governingType,
        AsnSourceSpan? span)
    {
        AsnValue? value = item.Value;
        if (value == null)
        {
            return new IntegerEvaluationResult();
        }

        if (value.HasIntegerValue)
        {
            return new IntegerEvaluationResult
            {
                Success = true,
                Value = value.IntegerValue
            };
        }

        if (value.Kind != AsnValueKind.Defined)
        {
            return new IntegerEvaluationResult();
        }

        if (this.ResolvingNamedValues.Contains(item))
        {
            this.AddValueDiagnostic(
                SchemaDiagnosticCodes.NamedNumberCycle,
                "A named number or named bit participates in a cycle.",
                span,
                null);
            return new IntegerEvaluationResult();
        }

        this.ResolvingNamedValues.Add(item);
        string moduleName = "";
        string referenceName = value.TextValue;
        int dot = referenceName.IndexOf('.');
        if (dot >= 0)
        {
            moduleName = referenceName.Substring(0, dot);
            referenceName = referenceName.Substring(dot + 1);
        }

        IntegerEvaluationResult result = this.ResolveIntegerReference(
            moduleName,
            referenceName,
            module,
            declaration,
            governingType,
            null);
        this.ResolvingNamedValues.Remove(item);
        if (result.Success)
        {
            item.Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = result.Value,
                TextValue = value.TextValue
            };
        }

        return result;
    }

    internal ValueReferenceBinding FindValueBinding(
        SymbolTable module,
        string moduleName,
        string referenceName)
    {
        ValueReferenceBinding result = new ValueReferenceBinding();
        int count = 0;
        if (moduleName.Length == 0 || moduleName == module.ModuleName)
        {
            SymbolTableEntry[] local = module.FindSymbols(
                referenceName,
                AsnAssignmentKind.Value);
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
                        && binding.Kind == AsnAssignmentKind.Value
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

    internal void EvaluateBitStringValue(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        if (syntax.BitStringValue != null)
        {
            resolution.EffectiveValue = this.CopyValue(syntax.Value);
            resolution.BitStringValue = this.CopyBitString(syntax.BitStringValue);
            resolution.Success = true;
            return;
        }

        int maximum = -1;
        int index = 0;
        int[] positions = new int[syntax.Value.NamedValues.Length];
        while (index < syntax.Value.NamedValues.Length)
        {
            AsnNamedValue selected = syntax.Value.NamedValues[index];
            IntegerEvaluationResult position = this.FindNamedIntegerValue(
                governingType,
                selected.Name,
                module,
                declaration,
                syntax.Span);
            if (!position.Success)
            {
                this.AddValueDiagnostic(
                    SchemaDiagnosticCodes.NamedBitUnresolved,
                    "Named bit '" + selected.Name + "' cannot be resolved.",
                    syntax.Span,
                    null);
                return;
            }

            if (position.Value < System.Numerics.BigInteger.Zero
                || position.Value > new System.Numerics.BigInteger(int.MaxValue - 1))
            {
                this.AddValueDiagnostic(
                    SchemaDiagnosticCodes.NamedBitPositionUnsupported,
                    "Named bit '" + selected.Name + "' has an unsupported bit position.",
                    syntax.Span,
                    null);
                return;
            }

            positions[index] = (int)position.Value;
            if (positions[index] > maximum)
            {
                maximum = positions[index];
            }

            index = index + 1;
        }

        int bitLength = maximum + 1;
        byte[] bytes = new byte[(bitLength + 7) / 8];
        index = 0;
        while (index < positions.Length)
        {
            int byteIndex = positions[index] / 8;
            int shift = 7 - positions[index] % 8;
            bytes[byteIndex] = (byte)(bytes[byteIndex] | (byte)(1 << shift));
            index = index + 1;
        }

        resolution.EffectiveValue = this.CopyValue(syntax.Value);
        resolution.BitStringValue = new AsnBitStringValue
        {
            Bytes = bytes,
            BitLength = bitLength
        };
        resolution.Success = true;
    }

    internal void EvaluateObjectIdentifierValue(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        bool relative = syntax.Value.Kind == AsnValueKind.RelativeObjectIdentifier;
        if (syntax.ObjectIdentifierComponents.Length == 0
            && syntax.ObjectIdentifierValue != null)
        {
            resolution.EffectiveValue = this.CopyValue(syntax.Value);
            resolution.ObjectIdentifierValue = this.CopyObjectIdentifier(
                syntax.ObjectIdentifierValue);
            resolution.Success = true;
            return;
        }

        System.Collections.Generic.List<System.Numerics.BigInteger> arcs =
            new System.Collections.Generic.List<System.Numerics.BigInteger>();
        int index = 0;
        while (index < syntax.ObjectIdentifierComponents.Length)
        {
            AssignedOidComponentSyntax component = syntax.ObjectIdentifierComponents[index];
            if (!this.AppendObjectIdentifierComponent(
                component,
                arcs,
                relative,
                module,
                declaration,
                governingType))
            {
                return;
            }

            index = index + 1;
        }

        AsnObjectIdentifierValue value = new AsnObjectIdentifierValue
        {
            Relative = relative,
            Arcs = arcs.ToArray()
        };
        AsnValue effective = this.CopyValue(syntax.Value);
        effective.TextValue = this.JoinObjectIdentifierArcs(value.Arcs);
        resolution.EffectiveValue = effective;
        resolution.ObjectIdentifierValue = value;
        resolution.Success = true;
    }

    internal bool AppendObjectIdentifierComponent(
        AssignedOidComponentSyntax component,
        System.Collections.Generic.List<System.Numerics.BigInteger> arcs,
        bool relative,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedTypeSyntax? governingType)
    {
        if (component.HasNumber)
        {
            arcs.Add(component.Number);
            return true;
        }

        if (component.HasNumberDefinedValue)
        {
            string numberModule = "";
            string numberName = component.NumberDefinedValue;
            int dot = numberName.IndexOf('.');
            if (dot >= 0)
            {
                numberModule = numberName.Substring(0, dot);
                numberName = numberName.Substring(dot + 1);
            }

            IntegerEvaluationResult number = this.ResolveIntegerReference(
                numberModule,
                numberName,
                module,
                declaration,
                governingType,
                null);
            if (!number.Success)
            {
                return false;
            }

            arcs.Add(number.Value);
            return true;
        }

        if (!component.HasDefinedValue)
        {
            return false;
        }

        if (!relative && arcs.Count == 0)
        {
            IntegerEvaluationResult root = this.ResolveStandardRootArc(component.DefinedValue);
            if (root.Success)
            {
                arcs.Add(root.Value);
                return true;
            }
        }

        ValueReferenceBinding binding = this.FindValueBinding(
            module,
            component.DefinedValueModule,
            component.DefinedValue);
        if (binding.Target == null || binding.Module == null)
        {
            this.AddValueDiagnostic(
                SchemaDiagnosticCodes.ObjectIdentifierComponentUnresolved,
                "Object identifier component '"
                    + this.QualifyReference(
                        component.DefinedValueModule,
                        component.DefinedValue)
                    + "' cannot be resolved.",
                component.Span,
                null);
            return false;
        }

        ResolvedValueSyntax target = this.ResolveValueEntry(binding.Target, binding.Module);
        if (!target.Success || target.EffectiveValue == null)
        {
            return false;
        }

        if (target.ObjectIdentifierValue != null)
        {
            int arcIndex = 0;
            while (arcIndex < target.ObjectIdentifierValue.Arcs.Length)
            {
                arcs.Add(target.ObjectIdentifierValue.Arcs[arcIndex]);
                arcIndex = arcIndex + 1;
            }

            return true;
        }

        if (target.EffectiveValue.HasIntegerValue)
        {
            arcs.Add(target.EffectiveValue.IntegerValue);
            return true;
        }

        this.AddValueDiagnostic(
            SchemaDiagnosticCodes.ObjectIdentifierComponentKind,
            "An object identifier component must resolve to an integer or object identifier.",
            component.Span,
            null);
        return false;
    }

    internal IntegerEvaluationResult ResolveStandardRootArc(string name)
    {
        if (name == "itu-t" || name == "ccitt")
        {
            return new IntegerEvaluationResult
            {
                Success = true,
                Value = System.Numerics.BigInteger.Zero
            };
        }

        if (name == "iso")
        {
            return new IntegerEvaluationResult
            {
                Success = true,
                Value = System.Numerics.BigInteger.One
            };
        }

        if (name == "joint-iso-itu-t" || name == "joint-iso-ccitt")
        {
            return new IntegerEvaluationResult
            {
                Success = true,
                Value = new System.Numerics.BigInteger(2)
            };
        }

        return new IntegerEvaluationResult();
    }

    internal string JoinObjectIdentifierArcs(
        System.Numerics.BigInteger[] arcs)
    {
        string text = "";
        int index = 0;
        while (index < arcs.Length)
        {
            string arc = arcs[index].ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            text = text.Length == 0 ? arc : text + "." + arc;
            index = index + 1;
        }

        return text;
    }

    internal void EvaluateStructuredValue(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        if (governingType == null
            || (governingType.EffectiveKind != AsnTypeKind.Sequence
                && governingType.EffectiveKind != AsnTypeKind.Set
                && governingType.EffectiveKind != AsnTypeKind.InstanceOf))
        {
            this.EvaluateLooseStructuredValue(
                syntax,
                governingType,
                module,
                declaration,
                resolution);
            return;
        }

        int componentCount = governingType.Components.Length;
        ResolvedComponentValueSyntax[] known =
            new ResolvedComponentValueSyntax[componentCount];
        bool[] seen = new bool[componentCount];
        System.Collections.Generic.List<ResolvedComponentValueSyntax> unknown =
            new System.Collections.Generic.List<ResolvedComponentValueSyntax>();
        System.Collections.Generic.List<ResolvedValueSyntax> sourceItems =
            new System.Collections.Generic.List<ResolvedValueSyntax>();
        System.Collections.Generic.List<string> sourceNames =
            new System.Collections.Generic.List<string>();
        bool success = true;
        int lastKnownIndex = -1;
        ParsedValueSyntax? lastKnownSourceSyntax = null;
        int index = 0;
        while (index < syntax.Items.Length)
        {
            string name = index < syntax.Value.NamedValues.Length
                ? syntax.Value.NamedValues[index].Name
                : "";
            int duplicateSourceIndex = sourceNames.IndexOf(name);
            if (duplicateSourceIndex >= 0)
            {
                this.AddStructuredValueDiagnosticWithRelated(
                    SchemaDiagnosticCodes.StructuredValueDuplicate,
                    "Structured value component '" + name + "' is specified more than once.",
                    syntax.Items[index],
                    "The first value for this component is here.",
                    syntax.Items[duplicateSourceIndex].Span);
                success = false;
                index = index + 1;
                continue;
            }

            sourceNames.Add(name);
            int componentIndex = this.FindComponentResolutionIndex(governingType, name);
            if (componentIndex < 0)
            {
                if (!this.IsExtensibleValueType(governingType))
                {
                    this.AddStructuredValueDiagnosticWithRelated(
                        SchemaDiagnosticCodes.StructuredValueUnknownComponent,
                        "Structured value component '" + name
                            + "' is not declared by the governing type.",
                        syntax.Items[index],
                        "The governing structured type is declared here.",
                        governingType.SourceSyntax == null
                            ? null
                            : governingType.SourceSyntax.Span);
                    success = false;
                    index = index + 1;
                    continue;
                }

                ResolvedValueSyntax extensionValue = this.ResolveNestedValueSyntax(
                    syntax.Items[index],
                    null,
                    module,
                    declaration);
                unknown.Add(new ResolvedComponentValueSyntax
                {
                    Name = name,
                    Value = extensionValue,
                    SourceSyntax = syntax.Items[index],
                    SourceOrder = index,
                    Present = true,
                    Extension = true,
                    UnknownExtension = true
                });
                sourceItems.Add(extensionValue);
                if (!extensionValue.Success)
                {
                    success = false;
                }

                index = index + 1;
                continue;
            }

            if (seen[componentIndex])
            {
                this.AddStructuredValueDiagnostic(
                    SchemaDiagnosticCodes.StructuredValueDuplicate,
                    "Structured value component '" + name + "' is specified more than once.",
                    syntax.Items[index]);
                success = false;
                index = index + 1;
                continue;
            }

            if ((governingType.EffectiveKind == AsnTypeKind.Sequence
                    || governingType.EffectiveKind == AsnTypeKind.InstanceOf)
                && componentIndex < lastKnownIndex)
            {
                this.AddStructuredValueDiagnosticWithRelated(
                    SchemaDiagnosticCodes.SequenceValueOrder,
                    "SEQUENCE value components are not in declaration order.",
                    syntax.Items[index],
                    "The preceding component value is here.",
                    lastKnownSourceSyntax == null ? null : lastKnownSourceSyntax.Span);
                success = false;
            }

            lastKnownIndex = componentIndex;
            lastKnownSourceSyntax = syntax.Items[index];
            seen[componentIndex] = true;
            ResolvedComponentSyntax component = governingType.Components[componentIndex];
            ResolvedValueSyntax explicitValue = this.ResolveNestedValueSyntax(
                syntax.Items[index],
                component.TypeResolution,
                module,
                declaration);
            sourceItems.Add(explicitValue);
            known[componentIndex] = new ResolvedComponentValueSyntax
            {
                Name = name,
                Component = component,
                Value = explicitValue,
                SourceSyntax = syntax.Items[index],
                SourceOrder = index,
                Present = true,
                PresentWithDefaultValue = component.DefaultValue != null
                    && this.AreResolvedValuesEqual(explicitValue, component.DefaultValue),
                Extension = component.Component != null && component.Component.Extension
            };
            if (!explicitValue.Success)
            {
                success = false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < componentCount)
        {
            if (!seen[index])
            {
                ResolvedComponentSyntax component = governingType.Components[index];
                AsnComponent? definition = component.Component;
                string name = definition == null ? "" : definition.Name;
                if (definition != null
                    && definition.HasDefaultValue
                    && component.DefaultValue != null
                    && component.DefaultValue.Success)
                {
                    known[index] = new ResolvedComponentValueSyntax
                    {
                        Name = name,
                        Component = component,
                        Value = component.DefaultValue,
                        Defaulted = true,
                        Extension = definition.Extension
                    };
                }
                else
                {
                    known[index] = new ResolvedComponentValueSyntax
                    {
                        Name = name,
                        Component = component,
                        Extension = definition != null && definition.Extension
                    };
                    bool permittedAbsent = definition == null
                        || definition.Optional
                        || definition.Extension;
                    if (!permittedAbsent)
                    {
                        this.AddStructuredValueDiagnosticWithRelated(
                            SchemaDiagnosticCodes.StructuredValueMissingComponent,
                            "Required structured value component '" + name + "' is absent.",
                            syntax,
                            "The required component is declared here.",
                            this.GetResolvedComponentSpan(component));
                        success = false;
                    }
                }
            }

            index = index + 1;
        }

        ResolvedComponentValueSyntax[] componentValues =
            new ResolvedComponentValueSyntax[componentCount + unknown.Count];
        index = 0;
        while (index < componentCount)
        {
            componentValues[index] = known[index];
            index = index + 1;
        }

        int unknownIndex = 0;
        while (unknownIndex < unknown.Count)
        {
            componentValues[componentCount + unknownIndex] = unknown[unknownIndex];
            unknownIndex = unknownIndex + 1;
        }

        int effectiveCount = this.CountEffectiveComponentValues(componentValues);
        AsnNamedValue[] named = new AsnNamedValue[effectiveCount];
        int effectiveIndex = 0;
        index = 0;
        while (index < componentValues.Length)
        {
            ResolvedComponentValueSyntax componentValue = componentValues[index];
            if ((componentValue.Present || componentValue.Defaulted)
                && componentValue.Value != null)
            {
                named[effectiveIndex] = new AsnNamedValue
                {
                    Name = componentValue.Name,
                    Value = componentValue.Value.EffectiveValue
                };
                effectiveIndex = effectiveIndex + 1;
            }

            index = index + 1;
        }

        AsnValue value = this.CopyValue(syntax.Value);
        value.NamedValues = named;
        resolution.EffectiveValue = value;
        resolution.Items = sourceItems.ToArray();
        resolution.ComponentValues = componentValues;
        resolution.Success = success;
    }

    internal void EvaluateLooseStructuredValue(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        ResolvedValueSyntax[] items = new ResolvedValueSyntax[syntax.Items.Length];
        AsnNamedValue[] named = new AsnNamedValue[syntax.Items.Length];
        bool success = true;
        int index = 0;
        while (index < syntax.Items.Length)
        {
            string name = index < syntax.Value.NamedValues.Length
                ? syntax.Value.NamedValues[index].Name
                : "";
            ResolvedTypeSyntax? componentType = this.FindComponentTypeResolution(
                governingType,
                name);
            items[index] = this.ResolveNestedValueSyntax(
                syntax.Items[index],
                componentType,
                module,
                declaration);
            named[index] = new AsnNamedValue
            {
                Name = name,
                Value = items[index].EffectiveValue
            };
            if (!items[index].Success)
            {
                success = false;
            }

            index = index + 1;
        }

        AsnValue value = this.CopyValue(syntax.Value);
        value.NamedValues = named;
        resolution.EffectiveValue = value;
        resolution.Items = items;
        resolution.Success = success;
    }

    internal void EvaluateCollectionValue(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        ResolvedTypeSyntax? elementType = governingType == null
            ? null
            : governingType.InnerType;
        ResolvedValueSyntax[] items = new ResolvedValueSyntax[syntax.Items.Length];
        bool success = true;
        int index = 0;
        while (index < syntax.Items.Length)
        {
            items[index] = this.ResolveNestedValueSyntax(
                syntax.Items[index],
                elementType,
                module,
                declaration);
            if (!items[index].Success)
            {
                success = false;
            }

            index = index + 1;
        }

        AsnValue value = this.CopyValue(syntax.Value);
        int nameCount = syntax.Value.NamedValues.Length;
        if (nameCount > 0 && nameCount != items.Length)
        {
            this.AddStructuredValueDiagnostic(
                SchemaDiagnosticCodes.CollectionValueNames,
                "A collection value cannot mix named and unnamed elements.",
                syntax);
            success = false;
        }

        if (nameCount == items.Length && nameCount > 0)
        {
            AsnNamedValue[] named = new AsnNamedValue[items.Length];
            string[] names = new string[items.Length];
            System.Collections.Generic.List<string> seenNames =
                new System.Collections.Generic.List<string>();
            index = 0;
            while (index < items.Length)
            {
                string name = syntax.Value.NamedValues[index].Name;
                int duplicateNameIndex = seenNames.IndexOf(name);
                if (duplicateNameIndex >= 0)
                {
                    this.AddStructuredValueDiagnosticWithRelated(
                        SchemaDiagnosticCodes.CollectionValueNames,
                        "Collection element name '" + name + "' is specified more than once.",
                        syntax.Items[index],
                        "The first collection element with this name is here.",
                        syntax.Items[duplicateNameIndex].Span);
                    success = false;
                }

                seenNames.Add(name);
                names[index] = name;
                named[index] = new AsnNamedValue
                {
                    Name = name,
                    Value = items[index].EffectiveValue
                };
                index = index + 1;
            }

            value.NamedValues = named;
            resolution.ItemNames = names;
            resolution.NamedCollection = true;
        }

        resolution.EffectiveValue = value;
        resolution.Items = items;
        resolution.Success = success;
    }

    internal void EvaluateChoiceValue(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        if (syntax.InnerValue == null || syntax.Value.NamedValues.Length == 0)
        {
            return;
        }

        string name = syntax.Value.NamedValues[0].Name;
        int componentIndex = governingType == null
            ? -1
            : this.FindComponentResolutionIndex(governingType, name);
        bool unknownExtension = componentIndex < 0;
        if (governingType != null
            && unknownExtension
            && !this.IsExtensibleValueType(governingType))
        {
            this.AddStructuredValueDiagnosticWithRelated(
                SchemaDiagnosticCodes.ChoiceValueUnknownAlternative,
                "CHOICE alternative '" + name
                    + "' is not declared by the governing type.",
                syntax.InnerValue,
                "The governing CHOICE type is declared here.",
                governingType.SourceSyntax == null
                    ? null
                    : governingType.SourceSyntax.Span);
            return;
        }

        ResolvedComponentSyntax? component = componentIndex < 0
            ? null
            : governingType?.Components[componentIndex];
        ResolvedTypeSyntax? selectedType = component == null
            ? null
            : component.TypeResolution;
        ResolvedValueSyntax selected = this.ResolveNestedValueSyntax(
            syntax.InnerValue,
            selectedType,
            module,
            declaration);
        AsnValue value = this.CopyValue(syntax.Value);
        value.NamedValues = [new AsnNamedValue
        {
            Name = name,
            Value = selected.EffectiveValue
        }];
        resolution.EffectiveValue = value;
        resolution.Items = [selected];
        resolution.ComponentValues = [new ResolvedComponentValueSyntax
        {
            Name = name,
            Component = component,
            Value = selected,
            SourceSyntax = syntax.InnerValue,
            SourceOrder = 0,
            Present = true,
            Extension = unknownExtension
                || (component?.Component != null && component.Component.Extension),
            UnknownExtension = unknownExtension && governingType != null
        }];
        resolution.Success = selected.Success;
    }

    internal void EvaluateOpenTypeValue(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedValueSyntax resolution)
    {
        if (syntax.InnerValue == null || syntax.GoverningTypeSyntax == null)
        {
            return;
        }

        ResolvedTypeSyntax openType = this.TypeResolver.ResolveNestedType(
            syntax.GoverningTypeSyntax,
            module,
            declaration,
            true);
        ResolvedValueSyntax inner = this.ResolveNestedValueSyntax(
            syntax.InnerValue,
            openType,
            module,
            declaration);
        resolution.EffectiveValue = this.CopyValue(syntax.Value);
        resolution.Items = [inner];
        resolution.Success = openType.Success && inner.Success;
    }

    internal ResolvedValueSyntax ResolveNestedValueSyntax(
        ParsedValueSyntax syntax,
        ResolvedTypeSyntax? governingType,
        SymbolTable module,
        SymbolTableEntry declaration)
    {
        if (syntax.Resolution != null && syntax.Resolution.Success)
        {
            return syntax.Resolution;
        }

        ResolvedValueSyntax resolution = this.CreateValueResolution(
            syntax,
            governingType,
            declaration);
        syntax.Resolution = resolution;
        this.EvaluateAndValidateValueSyntax(
            syntax,
            governingType,
            module,
            declaration,
            resolution);
        return resolution;
    }

    internal ResolvedTypeSyntax? FindComponentTypeResolution(
        ResolvedTypeSyntax? governingType,
        string name)
    {
        if (governingType == null)
        {
            return null;
        }

        int index = 0;
        while (index < governingType.Components.Length)
        {
            ResolvedComponentSyntax component = governingType.Components[index];
            if (component.Component != null
                && component.Component?.Name == name)
            {
                return component.TypeResolution;
            }

            index = index + 1;
        }

        return null;
    }

    internal int FindComponentResolutionIndex(
        ResolvedTypeSyntax governingType,
        string name)
    {
        int index = 0;
        while (index < governingType.Components.Length)
        {
            ResolvedComponentSyntax component = governingType.Components[index];
            if (component.Component != null && component.Component.Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal AsnSourceSpan? GetResolvedComponentSpan(ResolvedComponentSyntax component)
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

        return component.TypeResolution == null
            || component.TypeResolution.SourceSyntax == null
            ? null
            : component.TypeResolution.SourceSyntax.Span;
    }

    internal bool IsExtensibleValueType(ResolvedTypeSyntax governingType)
    {
        return governingType.EffectiveType != null
            && governingType.EffectiveType.Extensible;
    }

    internal int CountEffectiveComponentValues(
        ResolvedComponentValueSyntax[] componentValues)
    {
        int count = 0;
        int index = 0;
        while (index < componentValues.Length)
        {
            if ((componentValues[index].Present || componentValues[index].Defaulted)
                && componentValues[index].Value != null)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool AreResolvedValuesEqual(
        ResolvedValueSyntax left,
        ResolvedValueSyntax right)
    {
        if (!left.Success
            || !right.Success
            || left.EffectiveValue == null
            || right.EffectiveValue == null)
        {
            return false;
        }

        AsnValue leftValue = left.EffectiveValue;
        AsnValue rightValue = right.EffectiveValue;
        if (leftValue.Kind != rightValue.Kind
            || leftValue.HasIntegerValue != rightValue.HasIntegerValue
            || (leftValue.HasIntegerValue
                && leftValue.IntegerValue != rightValue.IntegerValue)
            || leftValue.TextValue != rightValue.TextValue)
        {
            return false;
        }

        if (!this.AreRealValuesEqual(leftValue.RealValue, rightValue.RealValue)
            || !this.AreTimeValuesEqual(leftValue.TimeValue, rightValue.TimeValue)
            || !this.AreBitStringValuesEqual(left.BitStringValue, right.BitStringValue)
            || !this.AreObjectIdentifierValuesEqual(
                left.ObjectIdentifierValue,
                right.ObjectIdentifierValue)
            || !this.AreByteArraysEqual(left.Octets, right.Octets))
        {
            return false;
        }

        if (left.ItemNames.Length != right.ItemNames.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.ItemNames.Length)
        {
            if (left.ItemNames[index] != right.ItemNames[index])
            {
                return false;
            }

            index = index + 1;
        }

        return this.AreResolvedItemArraysEqual(left, right);
    }

    internal bool AreResolvedItemArraysEqual(
        ResolvedValueSyntax left,
        ResolvedValueSyntax right)
    {
        if (left.ComponentValues.Length > 0 || right.ComponentValues.Length > 0)
        {
            return this.AreComponentValueArraysEqual(
                left.ComponentValues,
                right.ComponentValues);
        }

        if (left.Items.Length != right.Items.Length)
        {
            return false;
        }

        bool unordered = left.GoverningType != null
            && left.GoverningType.EffectiveKind == AsnTypeKind.SetOf;
        if (!unordered)
        {
            int index = 0;
            while (index < left.Items.Length)
            {
                if (!this.AreResolvedValuesEqual(left.Items[index], right.Items[index]))
                {
                    return false;
                }

                index = index + 1;
            }

            return true;
        }

        bool[] matched = new bool[right.Items.Length];
        int leftIndex = 0;
        while (leftIndex < left.Items.Length)
        {
            bool found = false;
            int rightIndex = 0;
            while (rightIndex < right.Items.Length && !found)
            {
                if (!matched[rightIndex]
                    && this.AreResolvedValuesEqual(
                        left.Items[leftIndex],
                        right.Items[rightIndex]))
                {
                    matched[rightIndex] = true;
                    found = true;
                }

                rightIndex = rightIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            leftIndex = leftIndex + 1;
        }

        return true;
    }

    internal bool AreComponentValueArraysEqual(
        ResolvedComponentValueSyntax[] left,
        ResolvedComponentValueSyntax[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            ResolvedComponentValueSyntax leftItem = left[index];
            ResolvedComponentValueSyntax rightItem = right[index];
            if (leftItem.Name != rightItem.Name)
            {
                return false;
            }

            bool leftEffective = (leftItem.Present || leftItem.Defaulted)
                && leftItem.Value != null;
            bool rightEffective = (rightItem.Present || rightItem.Defaulted)
                && rightItem.Value != null;
            if (leftEffective != rightEffective)
            {
                return false;
            }

            if (leftEffective
                && leftItem.Value != null
                && rightItem.Value != null
                && !this.AreResolvedValuesEqual(leftItem.Value, rightItem.Value))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool AreRealValuesEqual(AsnRealValue? left, AsnRealValue? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        return left.Normalized == right.Normalized
            && left.Kind == right.Kind
            && left.HasDecimalValue == right.HasDecimalValue
            && left.DecimalText == right.DecimalText
            && left.DecimalCoefficient == right.DecimalCoefficient
            && left.DecimalExponent == right.DecimalExponent
            && left.HasBinaryValue == right.HasBinaryValue
            && left.Mantissa == right.Mantissa
            && left.Radix == right.Radix
            && left.Exponent == right.Exponent;
    }

    internal bool AreTimeValuesEqual(AsnTimeValue? left, AsnTimeValue? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        return left.Kind == right.Kind
            && left.Text == right.Text
            && left.HasUtcOffsetMinutes == right.HasUtcOffsetMinutes
            && left.UtcOffsetMinutes == right.UtcOffsetMinutes
            && left.HasFractionalDigits == right.HasFractionalDigits
            && left.FractionalDigits == right.FractionalDigits;
    }

    internal bool AreBitStringValuesEqual(
        AsnBitStringValue? left,
        AsnBitStringValue? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        return left.BitLength == right.BitLength
            && this.AreByteArraysEqual(left.Bytes, right.Bytes);
    }

    internal bool AreObjectIdentifierValuesEqual(
        AsnObjectIdentifierValue? left,
        AsnObjectIdentifierValue? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        if (left.Relative != right.Relative || left.Arcs.Length != right.Arcs.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Arcs.Length)
        {
            if (left.Arcs[index] != right.Arcs[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool AreByteArraysEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool ResolveAllNamedValues()
    {
        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        bool success = true;
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            SymbolTable table = this.Tables[tableIndex];
            int typeIndex = 0;
            while (typeIndex < table.TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = table.TypeSymbols[typeIndex].ResolvedType;
                if (type != null
                    && !this.ResolveNamedValuesInType(type, visited))
                {
                    success = false;
                }

                typeIndex = typeIndex + 1;
            }

            int valueIndex = 0;
            while (valueIndex < table.ValueSymbols.Length)
            {
                ResolvedTypeSyntax? type = table.ValueSymbols[valueIndex].GoverningType;
                if (type != null
                    && !this.ResolveNamedValuesInType(type, visited))
                {
                    success = false;
                }

                valueIndex = valueIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return success;
    }

    internal bool ResolveNamedValuesInType(
        ResolvedTypeSyntax type,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (visited.Contains(type))
        {
            return true;
        }

        visited.Add(type);
        bool success = true;
        SymbolTableEntry? declaration = type.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.FindDeclaringModule(declaration);
        ParsedTypeSyntax? syntax = type.SourceSyntax;
        if (syntax != null && declaration != null && module != null)
        {
            if (!this.ResolveNamedValueArray(
                syntax.RootNamedValues,
                module,
                declaration,
                type))
            {
                success = false;
            }

            if (!this.ResolveNamedValueArray(
                syntax.ExtensionNamedValues,
                module,
                declaration,
                type))
            {
                success = false;
            }
        }

        if (type.AliasTarget != null
            && !this.ResolveNamedValuesInType(type.AliasTarget, visited))
        {
            success = false;
        }

        if (type.InnerType != null
            && !this.ResolveNamedValuesInType(type.InnerType, visited))
        {
            success = false;
        }

        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedTypeSyntax? componentType = type.Components[index].TypeResolution;
            if (componentType != null
                && !this.ResolveNamedValuesInType(componentType, visited))
            {
                success = false;
            }

            index = index + 1;
        }

        return success;
    }

    internal bool ResolveNamedValueArray(
        AsnNamedValue[] values,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedTypeSyntax type)
    {
        bool success = true;
        int index = 0;
        while (index < values.Length)
        {
            AsnValue? value = values[index].Value;
            if (value != null
                && value.Kind == AsnValueKind.Defined
                && !this.ResolveNamedIntegerItem(
                    values[index],
                    module,
                    declaration,
                    type,
                    type.SourceSyntax == null ? null : type.SourceSyntax.Span).Success)
            {
                success = false;
            }

            index = index + 1;
        }

        return success;
    }

    internal bool ResolveAllDefaultValues()
    {
        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        bool success = true;
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            SymbolTable table = this.Tables[tableIndex];
            int typeIndex = 0;
            while (typeIndex < table.TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = table.TypeSymbols[typeIndex].ResolvedType;
                if (type != null && !this.ResolveDefaultsInType(type, visited))
                {
                    success = false;
                }

                typeIndex = typeIndex + 1;
            }

            int valueIndex = 0;
            while (valueIndex < table.ValueSymbols.Length)
            {
                ResolvedTypeSyntax? type = table.ValueSymbols[valueIndex].GoverningType;
                if (type != null && !this.ResolveDefaultsInType(type, visited))
                {
                    success = false;
                }

                valueIndex = valueIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return success;
    }

    internal bool ResolveDefaultsInType(
        ResolvedTypeSyntax type,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (visited.Contains(type))
        {
            return true;
        }

        visited.Add(type);
        bool success = true;
        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedComponentSyntax component = type.Components[index];
            if (component.TypeResolution != null
                && !this.ResolveDefaultsInType(component.TypeResolution, visited))
            {
                success = false;
            }

            ComponentProvenanceSyntax? origin = this.FindDefaultOrigin(component);
            if (origin != null
                && origin.ComponentSyntax != null
                && origin.ComponentSyntax?.DefaultValueSyntax != null
                && origin.Declaration != null)
            {
                SymbolTable? module = this.FindDeclaringModule(origin.Declaration);
                if (module == null)
                {
                    success = false;
                }
                else
                {
                    ParsedValueSyntax defaultSyntax =
                        origin.ComponentSyntax.DefaultValueSyntax;
                    AsnType? effectiveType = component.TypeResolution == null
                        ? null
                        : component.TypeResolution.EffectiveType;
                    if (effectiveType != null
                        && origin.ComponentSyntax.DefaultValueTokens.Length > 0)
                    {
                        ValueParseResult reparsed = new ValueParser(
                            origin.ComponentSyntax.DefaultValueTokens,
                            this.Diagnostics,
                            true).ParseValueSyntax(effectiveType);
                        if (!reparsed.Success
                            || reparsed.Syntax == null
                            || reparsed.ConsumedTokenCount
                                != origin.ComponentSyntax.DefaultValueTokens.Length)
                        {
                            success = false;
                            index = index + 1;
                            continue;
                        }

                        defaultSyntax = reparsed.Syntax;
                        origin.ComponentSyntax.DefaultValueSyntax = defaultSyntax;
                    }

                    ResolvedValueSyntax resolvedDefault = this.ResolveNestedValueSyntax(
                        defaultSyntax,
                        component.TypeResolution,
                        module,
                        origin.Declaration);
                    component.DefaultValue = resolvedDefault;
                    if (component.Component != null)
                    {
                        component.Component.DefaultValue = resolvedDefault.EffectiveValue;
                    }

                    if (!resolvedDefault.Success)
                    {
                        success = false;
                    }
                }
            }

            index = index + 1;
        }

        if (type.InnerType != null
            && !this.ResolveDefaultsInType(type.InnerType, visited))
        {
            success = false;
        }

        if (type.AliasTarget != null
            && !this.ResolveDefaultsInType(type.AliasTarget, visited))
        {
            success = false;
        }

        int constraintIndex = 0;
        while (constraintIndex < type.ConstraintTypes.Length)
        {
            if (!this.ResolveDefaultsInType(type.ConstraintTypes[constraintIndex], visited))
            {
                success = false;
            }

            constraintIndex = constraintIndex + 1;
        }

        return success;
    }

    internal ComponentProvenanceSyntax? FindDefaultOrigin(
        ResolvedComponentSyntax component)
    {
        int index = 0;
        while (index < component.Provenance.Length)
        {
            ParsedComponentSyntax? syntax = component.Provenance[index].ComponentSyntax;
            if (syntax != null && syntax.DefaultValueSyntax != null)
            {
                return component.Provenance[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnValue CopyValue(AsnValue source)
    {
        AsnNamedValue[] named = new AsnNamedValue[source.NamedValues.Length];
        int index = 0;
        while (index < named.Length)
        {
            named[index] = new AsnNamedValue
            {
                Name = source.NamedValues[index].Name,
                Value = source.NamedValues[index].Value == null
                    ? null
                    : this.CopyValue(source.NamedValues[index].Value
                        ?? new AsnValue())
            };
            index = index + 1;
        }

        return new AsnValue
        {
            Kind = source.Kind,
            RealValue = this.CopyRealValue(source.RealValue),
            TimeValue = source.TimeValue,
            HasIntegerValue = source.HasIntegerValue,
            IntegerValue = source.IntegerValue,
            TextValue = source.TextValue,
            NamedValues = named
        };
    }

    internal AsnRealValue? CopyRealValue(AsnRealValue? source)
    {
        return source == null
            ? null
            : new AsnRealValue
            {
                Normalized = source.Normalized,
                Kind = source.Kind,
                HasDecimalValue = source.HasDecimalValue,
                DecimalText = source.DecimalText,
                DecimalCoefficient = source.DecimalCoefficient,
                DecimalExponent = source.DecimalExponent,
                HasBinaryValue = source.HasBinaryValue,
                Mantissa = source.Mantissa,
                Radix = source.Radix,
                Exponent = source.Exponent
            };
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, 0, result, 0, source.Length);
        return result;
    }

    internal AsnBitStringValue? CopyBitString(AsnBitStringValue? source)
    {
        return source == null
            ? null
            : new AsnBitStringValue
            {
                Bytes = this.CopyBytes(source.Bytes),
                BitLength = source.BitLength
            };
    }

    internal AsnObjectIdentifierValue? CopyObjectIdentifier(
        AsnObjectIdentifierValue? source)
    {
        if (source == null)
        {
            return null;
        }

        System.Numerics.BigInteger[] arcs =
            new System.Numerics.BigInteger[source.Arcs.Length];
        System.Array.Copy(source.Arcs, 0, arcs, 0, source.Arcs.Length);
        return new AsnObjectIdentifierValue
        {
            Relative = source.Relative,
            Arcs = arcs
        };
    }

    internal ResolvedValueSyntax[] CopyResolvedItems(
        ResolvedValueSyntax[] source)
    {
        ResolvedValueSyntax[] result = new ResolvedValueSyntax[source.Length];
        System.Array.Copy(source, 0, result, 0, source.Length);
        return result;
    }

    internal ResolvedComponentValueSyntax[] CopyComponentValues(
        ResolvedComponentValueSyntax[] source)
    {
        ResolvedComponentValueSyntax[] result =
            new ResolvedComponentValueSyntax[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            ResolvedComponentValueSyntax item = source[index];
            result[index] = new ResolvedComponentValueSyntax
            {
                Name = item.Name,
                Component = item.Component,
                Value = item.Value,
                SourceSyntax = item.SourceSyntax,
                SourceOrder = item.SourceOrder,
                Present = item.Present,
                Defaulted = item.Defaulted,
                PresentWithDefaultValue = item.PresentWithDefaultValue,
                Extension = item.Extension,
                UnknownExtension = item.UnknownExtension
            };
            index = index + 1;
        }

        return result;
    }

    internal string[] CopyStrings(string[] source)
    {
        string[] result = new string[source.Length];
        System.Array.Copy(source, 0, result, 0, source.Length);
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

    internal string QualifyReference(string moduleName, string referenceName)
    {
        return moduleName.Length == 0
            ? referenceName
            : moduleName + "." + referenceName;
    }

    internal void AddValueReferenceDiagnostic(
        ParsedValueSyntax syntax,
        bool ambiguous)
    {
        string name = this.QualifyReference(
            syntax.ReferenceModule,
            syntax.ReferenceName);
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            14,
            ambiguous
                ? "Value reference '" + name + "' is ambiguous."
                : "Value reference '" + name + "' cannot be resolved.",
            syntax.Span,
            null);
    }

    internal void AddIntegerReferenceDiagnostic(
        string moduleName,
        string referenceName,
        bool ambiguous,
        AsnSourceSpan? span)
    {
        string name = this.QualifyReference(moduleName, referenceName);
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            15,
            ambiguous
                ? "Integer value reference '" + name + "' is ambiguous."
                : "Integer value reference '" + name + "' cannot be resolved.",
            span,
            null);
    }

    internal void AddValueCycleDiagnostic(SymbolTableEntry entry)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            16,
            "Value assignment '" + entry.QualifiedName + "' participates in a cycle.",
            entry.Span,
            null);
    }

    internal void AddIntegerExpressionDiagnostic(string message, AsnToken? token)
    {
        this.AddValueDiagnostic(
            SchemaDiagnosticCodes.IntegerExpressionInvalid,
            message,
            token == null ? null : token.Span,
            null);
    }

    internal void AddStructuredValueDiagnostic(
        int number,
        string message,
        ParsedValueSyntax syntax)
    {
        this.AddValueDiagnostic(number, message, syntax.Span, null);
    }

    internal void AddStructuredValueDiagnosticWithRelated(
        int number,
        string message,
        ParsedValueSyntax syntax,
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
        this.AddValueDiagnostic(number, message, syntax.Span, related);
    }

    internal void AddValueDiagnostic(
        int number,
        string message,
        AsnSourceSpan? span,
        AsnDiagnosticRelatedLocation[]? related)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            number,
            message,
            span,
            related);
    }
}
