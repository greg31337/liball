namespace asn1;

internal class EncodingNeutralIrLowerer
{
    internal AsnEncodingRules PerEncodingRules = AsnEncodingRules.Uper;
    internal System.Collections.Generic.List<ResolvedTypeSyntax> ResolvedTypeSources =
        new System.Collections.Generic.List<ResolvedTypeSyntax>();
    internal System.Collections.Generic.List<LoweredTypeIr> ResolvedTypeResults =
        new System.Collections.Generic.List<LoweredTypeIr>();
    internal System.Collections.Generic.List<AsnType> PublicTypeSources =
        new System.Collections.Generic.List<AsnType>();
    internal System.Collections.Generic.List<LoweredTypeIr> PublicTypeResults =
        new System.Collections.Generic.List<LoweredTypeIr>();
    internal CompilationGraph? Graph;
    internal XsdSourceMappingSyntax[] CurrentXsdMappings = [];

    internal LoweredCompilationIr LowerCompilation(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        DiagnosticBag graphDiagnostics = new DiagnosticBag(
            false,
            new AsnResourceLimits());
        this.Graph = new CompilationGraphBuilder(graphDiagnostics).BuildGraph(tables);
        SymbolTable[] orderedTables = new DeterministicIrOrderer().OrderModules(
            tables,
            this.Graph);
        LoweredModuleIr[] modules = new LoweredModuleIr[orderedTables.Length];
        int index = 0;
        while (index < orderedTables.Length)
        {
            modules[index] = this.LowerModule(orderedTables[index]);
            index = index + 1;
        }

        LoweredCompilationIr result = new LoweredCompilationIr { Modules = modules };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(result);
        return result;
    }

    internal LoweredModuleIr LowerModule(SymbolTable table)
    {
        ModuleSyntax? syntax = table.Module;
        this.CurrentXsdMappings = syntax == null ? [] : syntax.XsdSourceMappings;
        SymbolTableEntry[] symbols = this.Graph == null
            ? table.Symbols
            : new DeterministicIrOrderer().OrderAssignments(table, this.Graph);
        LoweredAssignmentIr[] assignments =
            new LoweredAssignmentIr[symbols.Length];
        int index = 0;
        while (index < symbols.Length)
        {
            assignments[index] = this.LowerAssignment(symbols[index]);
            index = index + 1;
        }

        LoweredImportIr[] imports = new LoweredImportIr[table.ImportResolutions.Length];
        index = 0;
        while (index < table.ImportResolutions.Length)
        {
            imports[index] = this.LowerImport(table.ImportResolutions[index]);
            index = index + 1;
        }

        return new LoweredModuleIr
        {
            SourceSpan = this.CloneSpan(syntax == null ? null : syntax.Span),
            Derivations = this.CreateXsdDerivations(
                syntax == null ? null : syntax.Span),
            Name = table.ModuleName,
            ObjectIdentifier = table.ObjectIdentifier,
            Iri = table.Iri,
            VersionModule = table.VersionModule,
            Version = table.Version,
            TagDefault = syntax == null
                ? AsnTaggingMode.Unspecified
                : syntax.TagDefault,
            ExtensibilityImplied = syntax != null && syntax.ExtensibilityImplied,
            Imports = imports,
            Assignments = assignments,
            Instructions = this.LowerModuleInstructions(syntax)
        };
    }

    internal LoweredImportIr LowerImport(ImportResolution resolution)
    {
        string[] symbols = new string[resolution.Symbols.Length];
        int index = 0;
        while (index < symbols.Length)
        {
            ImportedSymbolBinding binding = resolution.Symbols[index];
            symbols[index] = binding.Target == null
                ? binding.QualifiedName
                : binding.Target.Name;
            index = index + 1;
        }

        return new LoweredImportIr
        {
            SourceSpan = this.CloneSpan(
                resolution.Import == null ? null : resolution.Import.Span),
            Derivations = this.CreateXsdDerivations(
                resolution.Import == null ? null : resolution.Import.Span),
            ModuleName = resolution.TargetModule == null
                ? resolution.Import == null ? "" : resolution.Import.ModuleName
                : resolution.TargetModule.ModuleName,
            Symbols = symbols
        };
    }

    internal LoweredAssignmentIr LowerAssignment(SymbolTableEntry symbol)
    {
        LoweredAssignmentIr result = new LoweredAssignmentIr
        {
            SourceSpan = this.CloneSpan(symbol.Span),
            Derivations = this.CreateAssignmentDerivations(symbol),
            Name = symbol.Name,
            QualifiedName = symbol.QualifiedName,
            Kind = symbol.Kind,
            Exported = symbol.Exported,
            Parameterized = symbol.Parameterized,
            Parameters = this.LowerParameters(
                symbol.FormalParameters,
                symbol.FormalParameterSyntax)
        };
        if (symbol.Kind == AsnAssignmentKind.Type)
        {
            result.Type = symbol.ResolvedType == null
                ? null
                : this.LowerResolvedType(symbol.ResolvedType);
        }
        else if (symbol.Kind == AsnAssignmentKind.Value)
        {
            result.Type = symbol.GoverningType == null
                ? null
                : this.LowerResolvedType(symbol.GoverningType);
            result.Value = symbol.ResolvedValue == null
                ? null
                : this.LowerResolvedValue(symbol.ResolvedValue);
        }
        else if (symbol.Kind == AsnAssignmentKind.ValueSet)
        {
            result.Type = symbol.ResolvedValueSetType == null
                ? null
                : this.LowerPublicType(symbol.ResolvedValueSetType);
        }
        else if (symbol.Kind == AsnAssignmentKind.ObjectClass)
        {
            result.ObjectClass = symbol.ResolvedObjectClassSyntax == null
                ? null
                : this.LowerObjectClass(symbol.ResolvedObjectClassSyntax);
        }
        else if (symbol.Kind == AsnAssignmentKind.Object)
        {
            result.Object = symbol.DeclaredObjectSyntax == null
                ? null
                : this.LowerObjectSyntax(symbol.DeclaredObjectSyntax);
        }
        else if (symbol.Kind == AsnAssignmentKind.ObjectSet)
        {
            result.ObjectSet = symbol.DeclaredObjectSetSyntax == null
                ? null
                : this.LowerObjectSet(symbol.DeclaredObjectSetSyntax);
        }

        if (result.Type != null)
        {
            this.ApplyParameterDerivations(
                result.Type,
                result.Derivations,
                new System.Collections.Generic.List<LoweredTypeIr>());
        }

        if (result.Value != null)
        {
            this.ApplyParameterDerivationsToValue(
                result.Value,
                result.Derivations);
        }

        return result;
    }

    internal LoweredParameterIr[] LowerParameters(
        AsnParameter[] parameters,
        ParsedParameterSyntax[] syntax)
    {
        LoweredParameterIr[] result = new LoweredParameterIr[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            AsnParameter source = parameters[index];
            result[index] = new LoweredParameterIr
            {
                SourceSpan = this.CloneSpan(
                    index < syntax.Length ? syntax[index].Span : null),
                Derivations = this.CreateXsdDerivations(
                    index < syntax.Length ? syntax[index].Span : null),
                Name = source.Name,
                Kind = source.Kind,
                GovernorTypeKind = source.GovernorType == null
                    ? AsnTypeKind.Unknown
                    : source.GovernorType.Kind,
                GovernorObjectClassName = source.GovernorObjectClass == null
                    ? ""
                    : source.GovernorObjectClass.Name
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredTypeIr LowerResolvedType(ResolvedTypeSyntax source)
    {
        int existing = this.FindResolvedTypeIndex(source);
        if (existing >= 0)
        {
            return this.ResolvedTypeResults[existing];
        }

        AsnType? effective = source.EffectiveType;
        LoweredTypeIr result = new LoweredTypeIr
        {
            SourceSpan = this.CloneSpan(this.GetResolvedTypeSpan(source)),
            Derivations = this.CreateTypeDerivations(source),
            Name = effective == null ? "" : effective.Name,
            Kind = source.EffectiveKind,
            Extensible = effective != null && effective.Extensible,
            ParameterReference = source.ParameterReference
        };
        this.ResolvedTypeSources.Add(source);
        this.ResolvedTypeResults.Add(result);
        result.Tags = this.LowerTags(source.TagChain);
        result.Fields = this.LowerFields(source.Components);
        result.ElementType = source.InnerType == null
            ? null
            : this.LowerResolvedType(source.InnerType);
        if (result.ElementType != null
            && source.SourceSyntax != null
            && source.SourceSyntax.ElementName.Length > 0)
        {
            result.ElementType.Name = source.SourceSyntax.ElementName;
        }
        result.NamedValues = this.LowerPublicNamedValues(
            effective == null ? [] : effective.NamedValues);
        result.Constraints = source.EffectiveConstraint == null
            ? []
            : [this.LowerConstraint(source.EffectiveConstraint)];
        result.Extensions = this.LowerExtensionPartition(source, result.Fields);
        result.ObjectTables = this.LowerObjectTables(source.TableConstraints);
        result.Instructions = source.XerInstructionResolutionState == 2
                || source.PerInstructionResolutionState == 2
            ? this.LowerResolvedTypeInstructions(source)
            : this.LowerTypeInstructions(source.SourceSyntax);
        result.PerInstructionPlanSteps = this.ClonePlanSteps(
            source.PerInstructionPlanSteps);
        return result;
    }

    internal int FindResolvedTypeIndex(ResolvedTypeSyntax source)
    {
        int index = 0;
        while (index < this.ResolvedTypeSources.Count)
        {
            if (this.ResolvedTypeSources[index] == source)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal LoweredFieldIr[] LowerFields(ResolvedComponentSyntax[] components)
    {
        LoweredFieldIr[] result = new LoweredFieldIr[components.Length];
        int index = 0;
        while (index < components.Length)
        {
            ResolvedComponentSyntax source = components[index];
            AsnComponent? component = source.Component;
            bool occurrenceInstructions = source.TypeResolution != null
                && source.FinalPerEncodingInstructions
                    != source.TypeResolution.FinalPerEncodingInstructions;
            result[index] = new LoweredFieldIr
            {
                SourceSpan = this.CloneSpan(this.GetComponentOriginSpan(source)),
                Derivations = this.CreateComponentDerivations(source),
                Name = component == null ? "" : component.Name,
                Type = source.TypeResolution == null
                    ? component == null || component.Type == null
                        ? null
                        : this.LowerPublicType(component.Type)
                    : this.LowerResolvedType(source.TypeResolution),
                Optional = component != null && component.Optional,
                HasDefaultValue = component != null && component.HasDefaultValue,
                DefaultValue = source.DefaultValue == null
                    ? component == null || component.DefaultValue == null
                        ? null
                        : this.LowerPublicValue(component.DefaultValue)
                    : this.LowerResolvedValue(source.DefaultValue),
                Extension = component != null && component.Extension,
                ExtensionGroupIndex = this.GetExtensionGroupIndex(source),
                Tags = this.LowerTags(source.TagChain),
                Instructions = occurrenceInstructions
                    ? this.LowerPerInstructions(
                        source.FinalPerEncodingInstructions)
                    : [],
                PerInstructionPlanSteps = occurrenceInstructions
                    ? this.ClonePlanSteps(source.PerInstructionPlanSteps)
                    : []
            };
            index = index + 1;
        }

        return result;
    }

    internal int GetExtensionGroupIndex(ResolvedComponentSyntax source)
    {
        int index = source.Provenance.Length - 1;
        while (index >= 0)
        {
            if (source.Provenance[index].ExtensionGroupIndex >= 0)
            {
                return source.Provenance[index].ExtensionGroupIndex;
            }

            index = index - 1;
        }

        return -1;
    }

    internal LoweredTagIr[] LowerTags(ResolvedTagSyntax[] tags)
    {
        LoweredTagIr[] result = new LoweredTagIr[tags.Length];
        int index = 0;
        while (index < tags.Length)
        {
            ResolvedTagSyntax source = tags[index];
            result[index] = new LoweredTagIr
            {
                SourceSpan = this.CloneSpan(this.GetTagSpan(source)),
                Derivations = this.CreateTagDerivations(source),
                Class = source.Tag.Class,
                Mode = source.Tag.Mode,
                Number = source.Tag.Number,
                Universal = source.Universal,
                AutomaticallyGenerated = source.AutomaticallyGenerated
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredExtensionPartitionIr LowerExtensionPartition(
        ResolvedTypeSyntax source,
        LoweredFieldIr[] fields)
    {
        int rootCount = 0;
        int index = 0;
        while (index < fields.Length)
        {
            if (!fields[index].Extension)
            {
                rootCount = rootCount + 1;
            }

            index = index + 1;
        }

        LoweredFieldIr[] roots = new LoweredFieldIr[rootCount];
        LoweredFieldIr[] additions = new LoweredFieldIr[fields.Length - rootCount];
        int rootIndex = 0;
        int additionIndex = 0;
        index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Extension)
            {
                additions[additionIndex] = fields[index];
                additionIndex = additionIndex + 1;
            }
            else
            {
                roots[rootIndex] = fields[index];
                rootIndex = rootIndex + 1;
            }

            index = index + 1;
        }

        ParsedTypeSyntax? syntax = source.SourceSyntax;
        return new LoweredExtensionPartitionIr
        {
            SourceSpan = this.CloneSpan(syntax == null ? null : syntax.Span),
            Derivations = this.CreateXsdDerivations(
                syntax == null ? null : syntax.Span),
            Extensible = source.EffectiveType != null && source.EffectiveType.Extensible,
            RootFields = roots,
            AdditionalFields = additions,
            RootNamedValues = syntax == null
                ? []
                : this.LowerPublicNamedValues(syntax.RootNamedValues),
            AdditionalNamedValues = syntax == null
                ? []
                : this.LowerPublicNamedValues(syntax.ExtensionNamedValues)
        };
    }

    internal LoweredConstraintIr LowerConstraint(NormalizedConstraintGraph source)
    {
        LoweredConstraintIr[] applications =
            new LoweredConstraintIr[source.Applications.Length];
        int index = 0;
        while (index < applications.Length)
        {
            applications[index] = this.LowerConstraint(source.Applications[index]);
            index = index + 1;
        }

        return new LoweredConstraintIr
        {
            SourceSpan = this.CloneSpan(
                source.SourceSyntax == null ? null : source.SourceSyntax.Span),
            Derivations = this.CreateXsdDerivations(
                source.SourceSyntax == null ? null : source.SourceSyntax.Span),
            DomainKind = source.Domain == null
                ? AsnTypeKind.Unknown
                : source.Domain.EffectiveKind,
            Root = source.RootPartition == null
                || source.RootPartition.Expression == null
                ? null
                : this.LowerConstraintNode(source.RootPartition.Expression),
            Extension = source.ExtensionPartition == null
                || source.ExtensionPartition.Expression == null
                ? null
                : this.LowerConstraintNode(source.ExtensionPartition.Expression),
            Applications = applications,
            RootIntervals = source.RootPartition == null
                ? []
                : this.LowerIntervals(
                    source.RootPartition.IntegerSet,
                    source.SourceSyntax == null ? null : source.SourceSyntax.Span),
            ExtensionIntervals = source.ExtensionPartition == null
                ? []
                : this.LowerIntervals(
                    source.ExtensionPartition.IntegerSet,
                    source.SourceSyntax == null ? null : source.SourceSyntax.Span),
            Extensible = source.ExtensionPartition != null,
            OpenExtension = source.ExtensionPartition != null
                && source.ExtensionPartition.Open
        };
    }

    internal LoweredConstraintNodeIr LowerConstraintNode(
        NormalizedConstraintNode source)
    {
        LoweredConstraintNodeIr[] children =
            new LoweredConstraintNodeIr[source.Children.Length];
        int index = 0;
        while (index < children.Length)
        {
            children[index] = this.LowerConstraintNode(source.Children[index]);
            index = index + 1;
        }

        LoweredConstraintIr[] nested =
            new LoweredConstraintIr[source.NestedConstraints.Length];
        index = 0;
        while (index < nested.Length)
        {
            nested[index] = this.LowerConstraint(source.NestedConstraints[index]);
            index = index + 1;
        }

        return new LoweredConstraintNodeIr
        {
            SourceSpan = this.CloneSpan(this.GetConstraintNodeSpan(source)),
            Derivations = this.CreateXsdDerivations(
                this.GetConstraintNodeSpan(source)),
            Kind = source.Kind,
            Name = source.Name,
            Text = source.Text,
            Value = source.Value == null ? null : this.LowerPublicValue(source.Value),
            LowerValue = source.LowerValue == null
                ? null
                : this.LowerPublicValue(source.LowerValue),
            UpperValue = source.UpperValue == null
                ? null
                : this.LowerPublicValue(source.UpperValue),
            LowerBound = source.LowerBound,
            UpperBound = source.UpperBound,
            HasLowerBound = source.HasLowerBound,
            HasUpperBound = source.HasUpperBound,
            LowerEndpointIncluded = source.LowerEndpointIncluded,
            UpperEndpointIncluded = source.UpperEndpointIncluded,
            Children = children,
            NestedConstraints = nested
        };
    }

    internal LoweredIntervalIr[] LowerIntervals(
        IntegerConstraintSet? set,
        AsnSourceSpan? span)
    {
        if (set == null)
        {
            return [];
        }

        LoweredIntervalIr[] result = new LoweredIntervalIr[set.Intervals.Length];
        int index = 0;
        while (index < result.Length)
        {
            IntegerInterval source = set.Intervals[index];
            result[index] = new LoweredIntervalIr
            {
                SourceSpan = this.CloneSpan(span),
                Derivations = this.CreateXsdDerivations(span),
                LowerBound = source.LowerBound,
                UpperBound = source.UpperBound,
                HasLowerBound = source.HasLowerBound,
                HasUpperBound = source.HasUpperBound
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredValueIr LowerResolvedValue(ResolvedValueSyntax source)
    {
        AsnValue value = source.EffectiveValue ?? source.SourceValue ?? new AsnValue();
        LoweredValueIr[] items = new LoweredValueIr[source.Items.Length];
        int index = 0;
        while (index < items.Length)
        {
            items[index] = this.LowerResolvedValue(source.Items[index]);
            index = index + 1;
        }

        LoweredNamedValueIr[] named =
            new LoweredNamedValueIr[source.ComponentValues.Length];
        index = 0;
        while (index < named.Length)
        {
            ResolvedComponentValueSyntax component = source.ComponentValues[index];
            named[index] = new LoweredNamedValueIr
            {
                Name = component.Name,
                Value = component.Value == null
                    ? null
                    : this.LowerResolvedValue(component.Value)
            };
            index = index + 1;
        }

        IrValuePayloadCloner payloads = new IrValuePayloadCloner();
        return new LoweredValueIr
        {
            SourceSpan = this.CloneSpan(this.GetResolvedValueSpan(source)),
            Derivations = this.CreateValueDerivations(source),
            Kind = value.Kind,
            HasIntegerValue = value.HasIntegerValue,
            IntegerValue = value.IntegerValue,
            TextValue = value.TextValue,
            Octets = this.CopyBytes(source.Octets),
            BitStringValue = payloads.CloneBitString(source.BitStringValue),
            ObjectIdentifierValue = payloads.CloneObjectIdentifier(
                source.ObjectIdentifierValue),
            RealValue = payloads.CloneReal(value.RealValue),
            TimeValue = payloads.CloneTime(value.TimeValue),
            Items = items,
            NamedValues = named.Length == 0
                ? this.LowerPublicNamedValues(value.NamedValues)
                : named
        };
    }

    internal LoweredValueIr LowerPublicValue(AsnValue source)
    {
        IrValuePayloadCloner payloads = new IrValuePayloadCloner();
        return new LoweredValueIr
        {
            Kind = source.Kind,
            HasIntegerValue = source.HasIntegerValue,
            IntegerValue = source.IntegerValue,
            TextValue = source.TextValue,
            Octets = this.CopyBytes(source.Octets),
            BitStringValue = payloads.CloneBitString(source.BitStringValue),
            ObjectIdentifierValue = payloads.CloneObjectIdentifier(
                source.ObjectIdentifierValue),
            RealValue = payloads.CloneReal(source.RealValue),
            TimeValue = payloads.CloneTime(source.TimeValue),
            Items = this.LowerPublicValueItems(source.Items),
            NamedValues = this.LowerPublicNamedValues(source.NamedValues)
        };
    }

    internal LoweredValueIr[] LowerPublicValueItems(AsnValue[] values)
    {
        LoweredValueIr[] result = new LoweredValueIr[values.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.LowerPublicValue(values[index]);
            index = index + 1;
        }

        return result;
    }

    internal LoweredNamedValueIr[] LowerPublicNamedValues(AsnNamedValue[] values)
    {
        LoweredNamedValueIr[] result = new LoweredNamedValueIr[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = new LoweredNamedValueIr
            {
                Name = values[index].Name,
                Value = values[index].Value == null
                    ? null
                    : this.LowerPublicValue(values[index].Value
                        ?? throw new System.InvalidOperationException())
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredTypeIr LowerPublicType(AsnType source)
    {
        int existing = this.FindPublicTypeIndex(source);
        if (existing >= 0)
        {
            return this.PublicTypeResults[existing];
        }

        LoweredTypeIr result = new LoweredTypeIr
        {
            Name = source.Name,
            Kind = source.Kind,
            Extensible = source.Extensible,
            NamedValues = this.LowerPublicNamedValues(source.NamedValues)
        };
        this.PublicTypeSources.Add(source);
        this.PublicTypeResults.Add(result);
        result.ElementType = source.ElementType == null
            ? null
            : this.LowerPublicType(source.ElementType);
        result.Fields = this.LowerPublicFields(source.Components);
        if (source.Tag != null)
        {
            result.Tags = [new LoweredTagIr
            {
                Class = source.Tag.Class,
                Mode = source.Tag.Mode,
                Number = source.Tag.Number
            }];
        }

        return result;
    }

    internal int FindPublicTypeIndex(AsnType source)
    {
        int index = 0;
        while (index < this.PublicTypeSources.Count)
        {
            if (this.PublicTypeSources[index] == source)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal LoweredFieldIr[] LowerPublicFields(AsnComponent[] components)
    {
        LoweredFieldIr[] result = new LoweredFieldIr[components.Length];
        int index = 0;
        while (index < components.Length)
        {
            AsnComponent source = components[index];
            result[index] = new LoweredFieldIr
            {
                Name = source.Name,
                Type = source.Type == null ? null : this.LowerPublicType(source.Type),
                Optional = source.Optional,
                HasDefaultValue = source.HasDefaultValue,
                DefaultValue = source.DefaultValue == null
                    ? null
                    : this.LowerPublicValue(source.DefaultValue),
                Extension = source.Extension
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredObjectClassIr LowerObjectClass(ParsedObjectClassSyntax source)
    {
        LoweredObjectClassFieldIr[] fields =
            new LoweredObjectClassFieldIr[source.FieldSyntax.Length];
        int index = 0;
        while (index < fields.Length)
        {
            fields[index] = this.LowerObjectClassField(source.FieldSyntax[index]);
            index = index + 1;
        }

        return new LoweredObjectClassIr
        {
            SourceSpan = this.CloneSpan(source.Span),
            Derivations = this.CreateXsdDerivations(source.Span),
            Name = source.ObjectClass.Name,
            Fields = fields,
            SyntaxTokens = this.CopyStrings(source.ObjectClass.SyntaxTokens)
        };
    }

    internal LoweredObjectClassFieldIr LowerObjectClassField(
        ParsedObjectClassFieldSyntax source)
    {
        AsnObjectClassField field = source.Field;
        return new LoweredObjectClassFieldIr
        {
            SourceSpan = this.CloneSpan(source.Span),
            Derivations = this.CreateXsdDerivations(source.Span),
            Name = field.Name,
            Kind = field.Kind,
            Type = source.GovernorTypeSyntax == null
                || source.GovernorTypeSyntax.Resolution == null
                ? field.Type == null ? null : this.LowerPublicType(field.Type)
                : this.LowerResolvedType(source.GovernorTypeSyntax.Resolution),
            GovernorObjectClassName = field.ObjectClass == null
                ? ""
                : field.ObjectClass.Name,
            GovernorFieldPath = this.CopyStrings(field.GovernorFieldPath),
            Optional = field.Optional,
            Unique = field.Unique,
            HasDefaultSetting = field.HasDefaultSetting,
            DefaultSetting = field.DefaultSetting == null
                ? null
                : this.LowerObjectSetting(field.DefaultSetting)
        };
    }

    internal LoweredObjectIr LowerObjectPublic(AsnInformationObject source)
    {
        LoweredObjectFieldSettingIr[] settings =
            new LoweredObjectFieldSettingIr[source.FieldSettings.Length];
        int index = 0;
        while (index < settings.Length)
        {
            settings[index] = this.LowerObjectSetting(source.FieldSettings[index]);
            index = index + 1;
        }

        return new LoweredObjectIr
        {
            StableId = source.StableId,
            Name = source.Name,
            ObjectClassName = source.ObjectClass == null
                ? ""
                : source.ObjectClass.Name,
            FieldSettings = settings
        };
    }

    internal LoweredObjectIr LowerObjectSyntax(ParsedInformationObjectSyntax source)
    {
        LoweredObjectFieldSettingIr[] settings =
            new LoweredObjectFieldSettingIr[source.FieldSettingSyntax.Length];
        int index = 0;
        while (index < settings.Length)
        {
            settings[index] = this.LowerObjectSettingSyntax(
                source.FieldSettingSyntax[index]);
            index = index + 1;
        }

        AsnInformationObject value = source.Object;
        return new LoweredObjectIr
        {
            StableId = value.StableId,
            SourceSpan = this.CloneSpan(source.Span),
            Derivations = this.CreateXsdDerivations(source.Span),
            Name = value.Name,
            ObjectClassName = value.ObjectClass == null
                ? ""
                : value.ObjectClass.Name,
            FieldSettings = settings.Length == 0
                ? this.LowerObjectSettingsPublic(value.FieldSettings)
                : settings
        };
    }

    internal LoweredObjectFieldSettingIr[] LowerObjectSettingsPublic(
        AsnObjectFieldSetting[] source)
    {
        LoweredObjectFieldSettingIr[] result =
            new LoweredObjectFieldSettingIr[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.LowerObjectSetting(source[index]);
            index = index + 1;
        }

        return result;
    }

    internal LoweredObjectFieldSettingIr LowerObjectSettingSyntax(
        ParsedObjectFieldSettingSyntax source)
    {
        AsnObjectFieldSetting setting = source.Setting;
        return new LoweredObjectFieldSettingIr
        {
            SourceSpan = this.CloneSpan(source.Span),
            Derivations = this.CreateXsdDerivations(source.Span),
            FieldName = setting.FieldName,
            Type = source.TypeResolution == null
                ? setting.Type == null ? null : this.LowerPublicType(setting.Type)
                : this.LowerResolvedType(source.TypeResolution),
            ValueSetType = setting.ValueSetType == null
                ? null
                : this.LowerPublicType(setting.ValueSetType),
            Value = setting.Value == null
                ? null
                : this.LowerPublicValue(setting.Value),
            ObjectStableId = setting.Object == null ? "" : setting.Object.StableId,
            ObjectSetStableId = setting.ObjectSet == null
                ? ""
                : setting.ObjectSet.StableId
        };
    }

    internal LoweredObjectFieldSettingIr LowerObjectSetting(
        AsnObjectFieldSetting source)
    {
        return new LoweredObjectFieldSettingIr
        {
            FieldName = source.FieldName,
            Type = source.Type == null ? null : this.LowerPublicType(source.Type),
            ValueSetType = source.ValueSetType == null
                ? null
                : this.LowerPublicType(source.ValueSetType),
            Value = source.Value == null ? null : this.LowerPublicValue(source.Value),
            ObjectStableId = source.Object == null ? "" : source.Object.StableId,
            ObjectSetStableId = source.ObjectSet == null
                ? ""
                : source.ObjectSet.StableId
        };
    }

    internal LoweredObjectSetIr LowerObjectSet(ParsedInformationObjectSetSyntax source)
    {
        LoweredObjectIr[] roots = new LoweredObjectIr[source.RootMembers.Length];
        LoweredObjectIr[] additions =
            new LoweredObjectIr[source.AdditionalMembers.Length];
        int index = 0;
        while (index < roots.Length)
        {
            roots[index] = this.LowerEvaluatedObject(source.RootMembers[index]);
            index = index + 1;
        }

        index = 0;
        while (index < additions.Length)
        {
            additions[index] = this.LowerEvaluatedObject(
                source.AdditionalMembers[index]);
            index = index + 1;
        }

        AsnInformationObjectSet set = source.ObjectSet;
        return new LoweredObjectSetIr
        {
            StableId = set.StableId,
            SourceSpan = this.CloneSpan(source.Span),
            Derivations = this.CreateXsdDerivations(source.Span),
            Name = set.Name,
            ObjectClassName = set.ObjectClass == null ? "" : set.ObjectClass.Name,
            Extensible = set.Extensible,
            RootObjects = roots,
            AdditionalObjects = additions,
            Table = set.DispatchEntries.Length == 0
                ? null
                : this.LowerDispatchTable(
                    set.StableId,
                    AsnConstraintKind.SimpleTable,
                    set.ObjectClass == null ? "" : set.ObjectClass.Name,
                    set.DispatchEntries,
                    source.Span)
        };
    }

    internal LoweredObjectIr LowerEvaluatedObject(EvaluatedObjectSetMember source)
    {
        LoweredObjectIr result = source.ObjectSyntax == null
            ? this.LowerObjectPublic(source.Object)
            : this.LowerObjectSyntax(source.ObjectSyntax);
        if (result.SourceSpan == null)
        {
            result.SourceSpan = this.CloneSpan(source.Span);
            result.Derivations = this.CreateXsdDerivations(source.Span);
        }

        return result;
    }

    internal LoweredObjectTableIr[] LowerObjectTables(
        TableConstraintRelationship[] relationships)
    {
        LoweredObjectTableIr[] result =
            new LoweredObjectTableIr[relationships.Length];
        int index = 0;
        while (index < result.Length)
        {
            TableConstraintRelationship source = relationships[index];
            LoweredObjectTableIr table = this.LowerDispatchTable(
                source.StableId,
                source.Kind,
                source.ObjectSet == null || source.ObjectSet.ObjectClass == null
                    ? ""
                    : source.ObjectSet.ObjectClass.Name,
                source.DispatchEntries,
                source.SourceNode == null
                    || source.SourceNode.SourceGeneralConstraint == null
                    ? null
                    : source.SourceNode.SourceGeneralConstraint.Span);
            table.UniqueFieldName = source.ConstrainedField == null
                || source.ConstrainedField.Field == null
                ? ""
                : source.ConstrainedField.Field.Name;
            result[index] = table;
            index = index + 1;
        }

        return result;
    }

    internal LoweredObjectTableIr LowerDispatchTable(
        string stableId,
        AsnConstraintKind kind,
        string objectClassName,
        AsnOpenTypeDispatchEntry[] entries,
        AsnSourceSpan? sourceSpan)
    {
        LoweredObjectTableEntryIr[] lowered =
            new LoweredObjectTableEntryIr[entries.Length];
        int index = 0;
        while (index < lowered.Length)
        {
            AsnOpenTypeDispatchEntry source = entries[index];
            lowered[index] = new LoweredObjectTableEntryIr
            {
                StableId = source.StableId,
                Key = source.Key == null ? null : this.LowerPublicValue(source.Key),
                Type = source.Type == null ? null : this.LowerPublicType(source.Type)
            };
            index = index + 1;
        }

        return new LoweredObjectTableIr
        {
            StableId = stableId,
            SourceSpan = this.CloneSpan(sourceSpan),
            Derivations = this.CreateXsdDerivations(sourceSpan),
            Kind = kind,
            ObjectClassName = objectClassName,
            Entries = lowered
        };
    }

    internal LoweredInstructionIr[] LowerTypeInstructions(ParsedTypeSyntax? syntax)
    {
        if (syntax == null)
        {
            return [];
        }

        LoweredInstructionIr[] result =
            new LoweredInstructionIr[syntax.EncodingInstructions.Length];
        int index = 0;
        while (index < result.Length)
        {
            AsnEncodingInstruction source = syntax.EncodingInstructions[index];
            result[index] = new LoweredInstructionIr
            {
                SourceSpan = this.CloneSpan(source.Span),
                Derivations = this.CreateEncodingControlDerivations(
                    syntax.EncodingReference,
                    source.Span),
                EncodingReference = syntax.EncodingReference,
                Name = source.Name,
                Kind = source.Kind,
                Negated = source.Negated,
                Arguments = this.CopyStrings(source.Arguments),
                Targets = source.Targets.Length == 0
                    ? this.CopyInstructionTargets(syntax.EncodingInstructionTokens)
                    : this.CopyStrings(source.Targets)
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredInstructionIr[] LowerResolvedTypeInstructions(ResolvedTypeSyntax source)
    {
        int xerCount = source.XerInstructionResolutionState == 2
            ? source.FinalEncodingInstructions.Length
            : 0;
        int perCount = source.PerInstructionResolutionState == 2
            ? source.FinalPerEncodingInstructions.Length
            : 0;
        LoweredInstructionIr[] result = new LoweredInstructionIr[
            xerCount + perCount];
        int index = 0;
        while (index < xerCount)
        {
            AsnEncodingInstruction instruction = source.FinalEncodingInstructions[index];
            string[] targets = instruction.Targets;
            if (targets.Length == 0
                && xerCount == 1
                && source.SourceSyntax != null
                && source.SourceSyntax.EncodingInstructionTokens.Length > 1)
            {
                targets = this.CopyInstructionTargets(
                    source.SourceSyntax.EncodingInstructionTokens);
            }

            result[index] = new LoweredInstructionIr
            {
                SourceSpan = this.CloneSpan(instruction.Span),
                Derivations = this.CreateEncodingControlDerivations(
                    "XER",
                    instruction.Span),
                EncodingReference = "XER",
                Name = instruction.Name,
                Kind = instruction.Kind,
                Arguments = this.CopyStrings(instruction.Arguments),
                Targets = this.CopyStrings(targets)
            };
            index = index + 1;
        }

        int perIndex = 0;
        while (perIndex < perCount)
        {
            AsnEncodingInstruction instruction =
                source.FinalPerEncodingInstructions[perIndex];
            result[xerCount + perIndex] = this.LowerPerInstruction(instruction);
            perIndex = perIndex + 1;
        }

        return result;
    }

    internal LoweredInstructionIr[] LowerPerInstructions(
        AsnEncodingInstruction[] instructions)
    {
        LoweredInstructionIr[] result =
            new LoweredInstructionIr[instructions.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.LowerPerInstruction(instructions[index]);
            index = index + 1;
        }

        return result;
    }

    internal LoweredInstructionIr LowerPerInstruction(
        AsnEncodingInstruction instruction)
    {
        string encodingReference = "UPER";
        if (this.PerEncodingRules == AsnEncodingRules.Aper)
        {
            encodingReference = "PER";
        }
        else if (this.PerEncodingRules == AsnEncodingRules.CanonicalAlignedPer)
        {
            encodingReference = "CPER";
        }
        else if (this.PerEncodingRules == AsnEncodingRules.CanonicalUnalignedPer)
        {
            encodingReference = "CUPER";
        }

        return new LoweredInstructionIr
        {
            SourceSpan = this.CloneSpan(instruction.Span),
            Derivations = this.CreateEncodingControlDerivations(
                "PER",
                instruction.Span),
            EncodingReference = encodingReference,
            Name = instruction.Name,
            Kind = instruction.Kind,
            Negated = instruction.Negated,
            Arguments = this.CopyStrings(instruction.Arguments),
            Targets = this.CopyStrings(instruction.Targets)
        };
    }

    internal AsnEncodingPlanStep[] ClonePlanSteps(
        AsnEncodingPlanStep[] source)
    {
        AsnEncodingPlanStep[] result = new AsnEncodingPlanStep[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            AsnEncodingPlanStep step = source[index];
            result[index] = new AsnEncodingPlanStep
            {
                Kind = step.Kind,
                Instruction = step.Instruction == null
                    ? null
                    : this.ClonePublicInstruction(step.Instruction),
                Path = step.Path,
                Children = this.ClonePlanSteps(step.Children)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnEncodingInstruction ClonePublicInstruction(
        AsnEncodingInstruction source)
    {
        return new AsnEncodingInstruction
        {
            Name = source.Name,
            Kind = source.Kind,
            Negated = source.Negated,
            Arguments = this.CopyStrings(source.Arguments),
            Targets = this.CopyStrings(source.Targets),
            Span = this.CloneSpan(source.Span)
        };
    }

    internal LoweredInstructionIr[] LowerModuleInstructions(ModuleSyntax? syntax)
    {
        if (syntax == null)
        {
            return [];
        }

        int count = 0;
        int sectionIndex = 0;
        while (sectionIndex < syntax.EncodingControlSections.Length)
        {
            EncodingControlSectionSyntax section = syntax.EncodingControlSections[sectionIndex];
            count = count + (section.Instructions.Length == 0 ? 1 : section.Instructions.Length);
            sectionIndex = sectionIndex + 1;
        }

        LoweredInstructionIr[] result = new LoweredInstructionIr[count];
        int resultIndex = 0;
        sectionIndex = 0;
        while (sectionIndex < syntax.EncodingControlSections.Length)
        {
            EncodingControlSectionSyntax source =
                syntax.EncodingControlSections[sectionIndex];
            if (source.Instructions.Length == 0)
            {
                result[resultIndex] = new LoweredInstructionIr
                {
                    SourceSpan = this.CloneSpan(source.Span),
                    Derivations = this.CreateEncodingControlDerivations(
                        source.EncodingReference,
                        source.Span),
                    EncodingReference = source.EncodingReference,
                    Name = this.FindInstructionName(source.InstructionTokens),
                    Targets = this.CopyInstructionTargets(source.InstructionTokens)
                };
                resultIndex = resultIndex + 1;
            }
            else
            {
                int instructionIndex = 0;
                while (instructionIndex < source.Instructions.Length)
                {
                    AsnEncodingInstruction instruction =
                        source.Instructions[instructionIndex].Instruction;
                    result[resultIndex] = new LoweredInstructionIr
                    {
                        SourceSpan = this.CloneSpan(instruction.Span),
                        Derivations = this.CreateEncodingControlDerivations(
                            source.EncodingReference,
                            instruction.Span),
                        EncodingReference = source.EncodingReference,
                        Name = instruction.Name,
                        Kind = instruction.Kind,
                        Negated = instruction.Negated,
                        Arguments = this.CopyStrings(instruction.Arguments),
                        Targets = this.CopyStrings(instruction.Targets)
                    };
                    resultIndex = resultIndex + 1;
                    instructionIndex = instructionIndex + 1;
                }
            }

            sectionIndex = sectionIndex + 1;
        }

        return result;
    }

    internal string[] CopyInstructionTargets(AsnToken[] tokens)
    {
        int count = 0;
        int significant = 0;
        int tokenIndex = 0;
        while (tokenIndex < tokens.Length)
        {
            if (tokens[tokenIndex].Kind != AsnTokenKind.Whitespace
                && tokens[tokenIndex].Kind != AsnTokenKind.Comment)
            {
                if (significant > 0)
                {
                    count = count + 1;
                }

                significant = significant + 1;
            }

            tokenIndex = tokenIndex + 1;
        }

        string[] result = new string[count];
        int index = 0;
        significant = 0;
        tokenIndex = 0;
        while (tokenIndex < tokens.Length)
        {
            if (tokens[tokenIndex].Kind != AsnTokenKind.Whitespace
                && tokens[tokenIndex].Kind != AsnTokenKind.Comment)
            {
                if (significant > 0)
                {
                    result[index] = tokens[tokenIndex].Text;
                    index = index + 1;
                }

                significant = significant + 1;
            }

            tokenIndex = tokenIndex + 1;
        }

        return result;
    }

    internal string FindInstructionName(AsnToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind != AsnTokenKind.Whitespace
                && tokens[index].Kind != AsnTokenKind.Comment)
            {
                return tokens[index].Text;
            }

            index = index + 1;
        }

        return "";
    }

    internal string[] CopyStrings(string[] source)
    {
        string[] result = new string[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    internal LoweredDerivationIr[] CreateAssignmentDerivations(
        SymbolTableEntry symbol)
    {
        System.Collections.Generic.List<LoweredDerivationIr> result =
            new System.Collections.Generic.List<LoweredDerivationIr>();
        int index = 0;
        while (index < symbol.ParameterizedReferences.Length)
        {
            ParsedParameterizedReferenceSyntax reference =
                symbol.ParameterizedReferences[index];
            ParameterInstantiationTrace? trace = reference.Instantiation == null
                ? null
                : reference.Instantiation.Trace;
            if (trace != null)
            {
                string name = reference.Instantiation == null
                    ? ""
                    : reference.Instantiation.StableId;
                if (name.Length == 0)
                {
                    name = reference.ReferenceModule.Length == 0
                        ? reference.ReferenceName
                        : reference.ReferenceModule + "." + reference.ReferenceName;
                }

                result.Add(new LoweredDerivationIr
                {
                    Kind = LoweredDerivationKind.ParameterInstantiation,
                    Name = name,
                    Span = this.CloneSpan(trace.Span)
                });
            }

            index = index + 1;
        }

        this.AddXsdDerivation(result, symbol.Span);
        return result.ToArray();
    }

    internal LoweredDerivationIr[] CreateTypeDerivations(
        ResolvedTypeSyntax source)
    {
        System.Collections.Generic.List<LoweredDerivationIr> result =
            new System.Collections.Generic.List<LoweredDerivationIr>();
        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        ResolvedTypeSyntax? current = source;
        while (current != null && current.AliasTarget != null
            && !visited.Contains(current))
        {
            visited.Add(current);
            ResolvedTypeSyntax target = current.AliasTarget;
            string name = target.SourceDeclaration == null
                ? target.SourceType == null ? "" : target.SourceType.Name
                : target.SourceDeclaration.QualifiedName;
            result.Add(new LoweredDerivationIr
            {
                Kind = LoweredDerivationKind.Alias,
                Name = name,
                Span = this.CloneSpan(this.GetResolvedTypeSpan(target))
            });
            current = target;
        }

        this.AddXsdDerivation(result, this.GetResolvedTypeSpan(source));
        return result.ToArray();
    }

    internal LoweredDerivationIr[] CreateComponentDerivations(
        ResolvedComponentSyntax source)
    {
        System.Collections.Generic.List<LoweredDerivationIr> result =
            new System.Collections.Generic.List<LoweredDerivationIr>();
        int index = source.Provenance.Length - 1;
        while (index >= 0)
        {
            ComponentProvenanceSyntax provenance = source.Provenance[index];
            if (provenance.ComponentsOf)
            {
                string name = provenance.Declaration == null
                    ? ""
                    : provenance.Declaration.QualifiedName;
                result.Add(new LoweredDerivationIr
                {
                    Kind = LoweredDerivationKind.ComponentsOf,
                    Name = name,
                    Span = this.CloneSpan(provenance.ComponentSyntax == null
                        ? null
                        : provenance.ComponentSyntax.Span)
                });
            }

            index = index - 1;
        }

        this.AddXsdDerivation(result, this.GetComponentOriginSpan(source));
        return result.ToArray();
    }

    internal LoweredDerivationIr[] CreateTagDerivations(ResolvedTagSyntax source)
    {
        System.Collections.Generic.List<LoweredDerivationIr> result =
            new System.Collections.Generic.List<LoweredDerivationIr>();
        AsnSourceSpan? span = this.GetTagSpan(source);
        if (source.AutomaticallyGenerated)
        {
            string name = source.OriginDeclaration == null
                ? ""
                : source.OriginDeclaration.QualifiedName;
            if (source.OriginComponentSyntax != null
                && source.OriginComponentSyntax.Component != null)
            {
                name = name.Length == 0
                    ? source.OriginComponentSyntax.Component.Name
                    : name + "." + source.OriginComponentSyntax.Component.Name;
            }

            result.Add(new LoweredDerivationIr
            {
                Kind = LoweredDerivationKind.AutomaticTag,
                Name = name,
                Span = this.CloneSpan(span)
            });
        }

        this.AddXsdDerivation(result, span);
        return result.ToArray();
    }

    internal LoweredDerivationIr[] CreateValueDerivations(
        ResolvedValueSyntax source)
    {
        System.Collections.Generic.List<LoweredDerivationIr> result =
            new System.Collections.Generic.List<LoweredDerivationIr>();
        System.Collections.Generic.List<ResolvedValueSyntax> visited =
            new System.Collections.Generic.List<ResolvedValueSyntax>();
        ResolvedValueSyntax? current = source;
        while (current != null && current.ReferenceTarget != null
            && !visited.Contains(current))
        {
            visited.Add(current);
            ResolvedValueSyntax target = current.ReferenceTarget;
            string name = target.SourceDeclaration == null
                ? ""
                : target.SourceDeclaration.QualifiedName;
            result.Add(new LoweredDerivationIr
            {
                Kind = LoweredDerivationKind.Alias,
                Name = name,
                Span = this.CloneSpan(this.GetResolvedValueSpan(target))
            });
            current = target;
        }

        this.AddXsdDerivation(result, this.GetResolvedValueSpan(source));
        return result.ToArray();
    }

    internal LoweredDerivationIr[] CreateEncodingControlDerivations(
        string encodingReference,
        AsnSourceSpan? span)
    {
        System.Collections.Generic.List<LoweredDerivationIr> result =
            new System.Collections.Generic.List<LoweredDerivationIr>();
        result.Add(new LoweredDerivationIr
        {
            Kind = LoweredDerivationKind.EncodingControl,
            Name = encodingReference,
            Span = this.CloneSpan(span)
        });
        this.AddXsdDerivation(result, span);
        return result.ToArray();
    }

    internal LoweredDerivationIr[] CreateXsdDerivations(AsnSourceSpan? span)
    {
        System.Collections.Generic.List<LoweredDerivationIr> result =
            new System.Collections.Generic.List<LoweredDerivationIr>();
        this.AddXsdDerivation(result, span);
        return result.ToArray();
    }

    internal void ApplyParameterDerivations(
        LoweredTypeIr type,
        LoweredDerivationIr[] assignmentDerivations,
        System.Collections.Generic.List<LoweredTypeIr> visited)
    {
        if (visited.Contains(type))
        {
            return;
        }

        visited.Add(type);
        type.Derivations = this.PrependParameterDerivations(
            type.Derivations,
            assignmentDerivations);
        int tagIndex = 0;
        while (tagIndex < type.Tags.Length)
        {
            type.Tags[tagIndex].Derivations = this.PrependParameterDerivations(
                type.Tags[tagIndex].Derivations,
                assignmentDerivations);
            tagIndex = tagIndex + 1;
        }

        int fieldIndex = 0;
        while (fieldIndex < type.Fields.Length)
        {
            LoweredFieldIr field = type.Fields[fieldIndex];
            field.Derivations = this.PrependParameterDerivations(
                field.Derivations,
                assignmentDerivations);
            int fieldTagIndex = 0;
            while (fieldTagIndex < field.Tags.Length)
            {
                field.Tags[fieldTagIndex].Derivations =
                    this.PrependParameterDerivations(
                        field.Tags[fieldTagIndex].Derivations,
                        assignmentDerivations);
                fieldTagIndex = fieldTagIndex + 1;
            }

            if (field.Type != null)
            {
                this.ApplyParameterDerivations(
                    field.Type,
                    assignmentDerivations,
                    visited);
            }

            fieldIndex = fieldIndex + 1;
        }

        if (type.ElementType != null)
        {
            this.ApplyParameterDerivations(
                type.ElementType,
                assignmentDerivations,
                visited);
        }
    }

    internal void ApplyParameterDerivationsToValue(
        LoweredValueIr value,
        LoweredDerivationIr[] assignmentDerivations)
    {
        value.Derivations = this.PrependParameterDerivations(
            value.Derivations,
            assignmentDerivations);
        int itemIndex = 0;
        while (itemIndex < value.Items.Length)
        {
            this.ApplyParameterDerivationsToValue(
                value.Items[itemIndex],
                assignmentDerivations);
            itemIndex = itemIndex + 1;
        }

        int namedIndex = 0;
        while (namedIndex < value.NamedValues.Length)
        {
            LoweredNamedValueIr named = value.NamedValues[namedIndex];
            named.Derivations = this.PrependParameterDerivations(
                named.Derivations,
                assignmentDerivations);
            if (named.Value != null)
            {
                this.ApplyParameterDerivationsToValue(
                    named.Value,
                    assignmentDerivations);
            }

            namedIndex = namedIndex + 1;
        }
    }

    internal LoweredDerivationIr[] PrependParameterDerivations(
        LoweredDerivationIr[] existing,
        LoweredDerivationIr[] assignmentDerivations)
    {
        int count = 0;
        int index = 0;
        while (index < assignmentDerivations.Length)
        {
            if (assignmentDerivations[index].Kind
                == LoweredDerivationKind.ParameterInstantiation)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        if (count == 0)
        {
            return existing;
        }

        LoweredDerivationIr[] result =
            new LoweredDerivationIr[count + existing.Length];
        int resultIndex = 0;
        index = 0;
        while (index < assignmentDerivations.Length)
        {
            LoweredDerivationIr source = assignmentDerivations[index];
            if (source.Kind == LoweredDerivationKind.ParameterInstantiation)
            {
                result[resultIndex] = new LoweredDerivationIr
                {
                    Kind = source.Kind,
                    Name = source.Name,
                    Span = this.CloneSpan(source.Span)
                };
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < existing.Length)
        {
            result[resultIndex] = existing[index];
            resultIndex = resultIndex + 1;
            index = index + 1;
        }

        return result;
    }

    internal void AddXsdDerivation(
        System.Collections.Generic.List<LoweredDerivationIr> result,
        AsnSourceSpan? span)
    {
        if (span == null)
        {
            return;
        }

        int index = 0;
        while (index < this.CurrentXsdMappings.Length)
        {
            XsdSourceMappingSyntax mapping = this.CurrentXsdMappings[index];
            if (mapping.GeneratedSpan != null
                && this.SpanContains(mapping.GeneratedSpan, span))
            {
                result.Add(new LoweredDerivationIr
                {
                    Kind = LoweredDerivationKind.XsdMapping,
                    Name = mapping.ComponentName,
                    Span = this.CloneSpan(mapping.SourceSpan)
                });
            }

            index = index + 1;
        }
    }

    internal bool SpanContains(AsnSourceSpan container, AsnSourceSpan contained)
    {
        return container.Start.SourceName == contained.Start.SourceName
            && container.End.SourceName == contained.End.SourceName
            && container.Start.Offset <= contained.Start.Offset
            && container.End.Offset >= contained.End.Offset;
    }

    internal AsnSourceSpan? GetResolvedTypeSpan(ResolvedTypeSyntax source)
    {
        if (source.SourceSyntax != null && source.SourceSyntax.Span != null)
        {
            return source.SourceSyntax.Span;
        }

        return source.SourceDeclaration == null
            ? null
            : source.SourceDeclaration.Span;
    }

    internal AsnSourceSpan? GetResolvedValueSpan(ResolvedValueSyntax source)
    {
        if (source.SourceSyntax != null && source.SourceSyntax.Span != null)
        {
            return source.SourceSyntax.Span;
        }

        return source.SourceDeclaration == null
            ? null
            : source.SourceDeclaration.Span;
    }

    internal AsnSourceSpan? GetComponentOriginSpan(ResolvedComponentSyntax source)
    {
        int index = 0;
        while (index < source.Provenance.Length)
        {
            ComponentProvenanceSyntax provenance = source.Provenance[index];
            if (!provenance.ComponentsOf && provenance.ComponentSyntax != null
                && provenance.ComponentSyntax.Span != null)
            {
                return provenance.ComponentSyntax.Span;
            }

            index = index + 1;
        }

        if (source.Provenance.Length == 0)
        {
            return null;
        }

        ParsedComponentSyntax? component = source.Provenance[0].ComponentSyntax;
        return component == null ? null : component.Span;
    }

    internal AsnSourceSpan? GetConstraintNodeSpan(NormalizedConstraintNode source)
    {
        if (source.SourceSyntax != null)
        {
            return source.SourceSyntax.Span;
        }

        if (source.SourceNamedConstraint != null)
        {
            return source.SourceNamedConstraint.Span;
        }

        return source.SourceGeneralConstraint == null
            ? null
            : source.SourceGeneralConstraint.Span;
    }

    internal AsnSourceSpan? GetTagSpan(ResolvedTagSyntax source)
    {
        if (source.OriginSyntax != null && source.OriginSyntax.Span != null)
        {
            return source.OriginSyntax.Span;
        }

        if (source.OriginComponentSyntax != null
            && source.OriginComponentSyntax.Span != null)
        {
            return source.OriginComponentSyntax.Span;
        }

        return source.OriginDeclaration == null
            ? null
            : source.OriginDeclaration.Span;
    }

    internal AsnSourceSpan? CloneSpan(AsnSourceSpan? source)
    {
        if (source == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = this.ClonePosition(source.Start),
            End = this.ClonePosition(source.End)
        };
    }

    internal AsnSourcePosition ClonePosition(AsnSourcePosition source)
    {
        return new AsnSourcePosition
        {
            SourceName = source.SourceName,
            Offset = source.Offset,
            Line = source.Line,
            Column = source.Column
        };
    }
}
