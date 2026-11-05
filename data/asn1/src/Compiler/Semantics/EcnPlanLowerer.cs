namespace asn1;

internal class EcnPlanLowerer
{
    internal EcnSymbolTable[] Tables = [];

    internal bool LowerTables(EcnSymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }
        this.Tables = tables;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int objectIndex = 0;
            while (objectIndex < tables[tableIndex].EncodingObjects.Length)
            {
                EcnSymbol symbol = tables[tableIndex].EncodingObjects[objectIndex];
                if (!symbol.Imported && symbol.ObjectDefinition != null)
                {
                    this.LowerObject(symbol.ObjectDefinition, tables[tableIndex]);
                }
                objectIndex = objectIndex + 1;
            }
            tableIndex = tableIndex + 1;
        }
        return true;
    }

    internal EcnExecutablePlan? LowerObject(
        EcnEncodingObjectDefinition definition,
        EcnSymbolTable table)
    {
        if (definition.PlanResolutionState == 2)
        {
            return definition.Plan;
        }
        definition.PlanResolutionState = 1;
        if (!definition.Valid)
        {
            definition.Plan = null;
            definition.PlanResolutionState = 2;
            return null;
        }

        string moduleName = definition.Symbol == null
            || definition.Symbol.SourceModuleName.Length == 0
            ? table.ModuleName
            : definition.Symbol.SourceModuleName;
        string planId = "ecn:plan:" + moduleName + ":" + definition.Name;
        EcnExecutablePlan plan = new EcnExecutablePlan
        {
            StableId = planId,
            ModuleName = moduleName,
            ObjectName = definition.Name,
            ClassName = definition.GovernorName,
            Reversible = true,
            Valid = true,
            Span = definition.Span
        };
        System.Collections.Generic.List<EcnExecutablePlanStep> all =
            new System.Collections.Generic.List<EcnExecutablePlanStep>();
        EcnExecutablePlanStep root = this.CreateStep(
            all,
            plan,
            this.GetRootKind(definition),
            definition.Name,
            "$value",
            "encode-value",
            definition.GovernorName,
            "object",
            definition.Name,
            0,
            definition.Span);
        System.Collections.Generic.List<EcnExecutablePlanStep> children =
            new System.Collections.Generic.List<EcnExecutablePlanStep>();
        this.AppendPropertySteps(definition, plan, all, children);
        this.AppendConditionSteps(definition, plan, all, children);
        this.AppendDeterminantSteps(definition, plan, all, children);
        this.AppendReplacementSteps(definition, plan, all, children);
        this.AppendTransformSteps(definition, plan, all, children);
        this.AppendMappingSteps(definition, plan, all, children);
        this.AppendFieldSteps(definition, plan, all, children);
        this.AppendLinkSteps(definition, plan, all, children);
        this.AppendOpenTypeStep(definition, plan, all, children);
        this.AddOrderingDependencies(children);
        root.Children = children.ToArray();
        this.AggregatePlanMetadata(plan, root, all);
        plan.Root = root;
        plan.Steps = all.ToArray();
        definition.Plan = plan;
        definition.PlanResolutionState = 2;
        return plan;
    }

    internal void AppendPropertySteps(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan,
        System.Collections.Generic.List<EcnExecutablePlanStep> all,
        System.Collections.Generic.List<EcnExecutablePlanStep> children)
    {
        int index = 0;
        while (index < definition.Properties.Length)
        {
            EcnEncodingProperty property = definition.Properties[index];
            EcnExecutablePlanStep step = this.CreateStep(
                all,
                plan,
                this.GetPropertyStepKind(property.Kind),
                property.Name,
                "$value",
                property.Kind.ToString(),
                property.Value,
                "property",
                property.Name,
                index,
                property.Span);
            this.ApplyPropertyMetadata(step, property);
            children.Add(step);
            index = index + 1;
        }
    }

    internal void ApplyPropertyMetadata(
        EcnExecutablePlanStep step,
        EcnEncodingProperty property)
    {
        if (property.Kind == EcnEncodingPropertyKind.Size
            && property.HasIntegerValue && property.IntegerValue >= 0)
        {
            step.Bounds.MinimumBits = property.IntegerValue;
            step.Bounds.MaximumBits = property.IntegerValue;
            step.Bounds.HasMinimumBits = true;
            step.Bounds.HasMaximumBits = true;
            step.Bounds.Exact = true;
            step.Buffer.MinimumBits = property.IntegerValue;
            if (property.IntegerValue % 8 == 0)
            {
                step.Bounds.MinimumBytes = property.IntegerValue / 8;
                step.Bounds.MaximumBytes = property.IntegerValue / 8;
                step.Bounds.HasMinimumBytes = true;
                step.Bounds.HasMaximumBytes = true;
                step.Buffer.MinimumBytes = property.IntegerValue / 8;
            }
            step.Dependencies =
            [
                this.CreateDependency(
                    EcnPlanDependencyKind.EncodingSpace,
                    EcnPlanUnit.Bits,
                    "",
                    step.Path,
                    "",
                    property.IntegerValue,
                    false,
                    property.Span)
            ];
        }
        else if (property.Kind == EcnEncodingPropertyKind.Range)
        {
            step.Bounds.LowerValue = property.LowerBound;
            step.Bounds.UpperValue = property.UpperBound;
            step.Bounds.HasLowerValue = property.LowerBound.Length > 0;
            step.Bounds.HasUpperValue = property.UpperBound.Length > 0;
        }
        else if (property.Kind == EcnEncodingPropertyKind.Alignment)
        {
            System.Collections.Generic.List<EcnPlanDependency> dependencies =
                new System.Collections.Generic.List<EcnPlanDependency>();
            dependencies.Add(this.CreateDependency(
                EcnPlanDependencyKind.BitPosition,
                EcnPlanUnit.Bits,
                "",
                step.Path,
                "",
                0,
                false,
                property.Span));
            if (this.PropertyUsesBytes(property))
            {
                dependencies.Add(this.CreateDependency(
                    EcnPlanDependencyKind.BytePosition,
                    EcnPlanUnit.Bytes,
                    "",
                    step.Path,
                    "",
                    0,
                    false,
                    property.Span));
            }
            step.Dependencies = dependencies.ToArray();
        }
        else if (property.Kind == EcnEncodingPropertyKind.ByteOrder)
        {
            step.Dependencies =
            [
                this.CreateDependency(
                    EcnPlanDependencyKind.BytePosition,
                    EcnPlanUnit.Bytes,
                    "",
                    step.Path,
                    "",
                    0,
                    false,
                    property.Span)
            ];
            step.Buffer.RequiresWholeValue = true;
            step.Buffer.RequiresRandomAccess = true;
            step.Buffer.Streamable = false;
        }
        else if (property.Kind == EcnEncodingPropertyKind.ValueReversal
            || property.Kind == EcnEncodingPropertyKind.BitReversal
            || property.Kind == EcnEncodingPropertyKind.WordOrder)
        {
            step.Buffer.RequiresWholeValue = true;
            step.Buffer.RequiresRandomAccess = true;
            step.Buffer.Streamable = false;
        }
    }

    internal void AppendConditionSteps(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan,
        System.Collections.Generic.List<EcnExecutablePlanStep> all,
        System.Collections.Generic.List<EcnExecutablePlanStep> children)
    {
        EcnConditionDefinition[] conditions = definition.Constructor == null
            ? []
            : definition.Constructor.Conditions;
        int index = 0;
        while (index < conditions.Length)
        {
            EcnConditionDefinition condition = conditions[index];
            EcnExecutablePlanStep step = this.CreateStep(
                all, plan, EcnPlanStepKind.Condition, condition.Name,
                condition.Reference, condition.Comparison, condition.Value,
                "condition", condition.Name, condition.Order, condition.Span);
            step.Dependencies =
            [
                this.CreateDependency(
                    EcnPlanDependencyKind.ConditionValue,
                    EcnPlanUnit.Values,
                    "",
                    condition.Reference,
                    "",
                    0,
                    true,
                    condition.Span)
            ];
            step.Buffer.RequiresLookahead = condition.All;
            children.Add(step);
            index = index + 1;
        }
    }

    internal void AppendDeterminantSteps(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan,
        System.Collections.Generic.List<EcnExecutablePlanStep> all,
        System.Collections.Generic.List<EcnExecutablePlanStep> children)
    {
        int index = 0;
        while (index < definition.Determinants.Length)
        {
            EcnDeterminantRelationship relationship =
                definition.Determinants[index];
            bool writes = relationship.Direction
                == EcnLinkDirection.ContentToDeterminant;
            string controlled = this.PathText(relationship.ControlledPath);
            string determinant = this.PathText(relationship.DeterminantPath);
            EcnExecutablePlanStep step = this.CreateStep(
                all,
                plan,
                writes
                    ? EcnPlanStepKind.DeterminantWrite
                    : EcnPlanStepKind.DeterminantRead,
                relationship.ControlKind.ToString(),
                controlled,
                relationship.Mode.ToString(),
                determinant,
                "determinant",
                relationship.ControlKind.ToString(),
                relationship.Order,
                relationship.Span);
            step.Dependencies =
            [
                this.CreateDependency(
                    EcnPlanDependencyKind.DeterminantValue,
                    EcnPlanUnit.Values,
                    "",
                    determinant,
                    "",
                    0,
                    !writes,
                    relationship.Span)
            ];
            if (writes)
            {
                step.Buffer.RequiresBackpatch = true;
                step.Buffer.RequiresRandomAccess = true;
                step.Buffer.Streamable = false;
            }
            children.Add(step);
            index = index + 1;
        }
    }

    internal void AppendReplacementSteps(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan,
        System.Collections.Generic.List<EcnExecutablePlanStep> all,
        System.Collections.Generic.List<EcnExecutablePlanStep> children)
    {
        EcnConversionDefinition? conversion = definition.Conversion;
        if (conversion == null)
        {
            return;
        }
        int index = 0;
        while (index < conversion.Replacements.Length)
        {
            EcnReplacementDefinition replacement = conversion.Replacements[index];
            string targetId = replacement.ReplacementClassSymbol == null
                ? "x692:class:" + replacement.ReplacementClassName
                : this.SymbolStableId(replacement.ReplacementClassSymbol);
            EcnExecutablePlanStep step = this.CreateStep(
                all, plan, EcnPlanStepKind.Replacement,
                replacement.ReplacementClassName, "$value",
                replacement.Scope.ToString(), replacement.EncodingObjectName,
                "replacement", replacement.ReplacementClassName, index,
                replacement.Span);
            step.Dependencies =
            [
                this.CreateDependency(
                    EcnPlanDependencyKind.ReplacementValue,
                    EcnPlanUnit.Values,
                    "",
                    "$value",
                    targetId,
                    0,
                    false,
                    replacement.Span)
            ];
            step.Buffer.RequiresWholeValue = true;
            step.Buffer.Streamable = false;
            step.Reversible = conversion.ContainedValueMapping == null
                || conversion.ContainedValueMapping.Reversible;
            children.Add(step);
            index = index + 1;
        }
    }

    internal void AppendTransformSteps(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan,
        System.Collections.Generic.List<EcnExecutablePlanStep> all,
        System.Collections.Generic.List<EcnExecutablePlanStep> children)
    {
        EcnConversionDefinition? conversion = definition.Conversion;
        if (conversion == null)
        {
            return;
        }
        int pipelineIndex = 0;
        while (pipelineIndex < conversion.TransformPipelines.Length)
        {
            EcnTransformPipeline pipeline =
                conversion.TransformPipelines[pipelineIndex];
            string previous = "";
            int stepIndex = 0;
            while (stepIndex < pipeline.Steps.Length)
            {
                EcnTransformStep transform = pipeline.Steps[stepIndex];
                EcnExecutablePlanStep step = this.CreateStep(
                    all, plan, EcnPlanStepKind.Transformation,
                    transform.Inline ? transform.Signature : transform.ReferenceName,
                    "$value", transform.Operation, transform.Argument,
                    "transform", pipeline.Direction.ToString(),
                    transform.Order, transform.Span);
                if (previous.Length > 0)
                {
                    step.Dependencies =
                    [
                        this.CreateDependency(
                            EcnPlanDependencyKind.TransformResult,
                            EcnPlanUnit.Values,
                            previous,
                            "$value",
                            "",
                            0,
                            true,
                            transform.Span)
                    ];
                }
                step.Reversible = transform.Reversible;
                step.RequiresRuntimeValidation =
                    transform.RequiresDomainValidation;
                step.Encodes = pipeline.Direction
                    != EcnTransformDirection.Decoder;
                step.Decodes = pipeline.Direction
                    != EcnTransformDirection.Encoder;
                children.Add(step);
                previous = step.StableId;
                stepIndex = stepIndex + 1;
            }
            pipelineIndex = pipelineIndex + 1;
        }
    }

    internal void AppendMappingSteps(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan,
        System.Collections.Generic.List<EcnExecutablePlanStep> all,
        System.Collections.Generic.List<EcnExecutablePlanStep> children)
    {
        int index = 0;
        while (index < definition.Mappings.Length)
        {
            EcnValueMapping mapping = definition.Mappings[index];
            EcnExecutablePlanStep step = this.CreateStep(
                all, plan, EcnPlanStepKind.Mapping,
                mapping.Kind.ToString(), "$value", mapping.Qualifier,
                mapping.Automatic ? "automatic" : "explicit",
                "mapping", mapping.Kind.ToString(), index, mapping.Span);
            step.Dependencies =
            [
                this.CreateDependency(
                    EcnPlanDependencyKind.FieldValue,
                    EcnPlanUnit.Values,
                    "",
                    "$value",
                    "",
                    0,
                    true,
                    mapping.Span)
            ];
            step.Reversible = mapping.Reversible;
            children.Add(step);
            index = index + 1;
        }
    }

    internal void AppendFieldSteps(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan,
        System.Collections.Generic.List<EcnExecutablePlanStep> all,
        System.Collections.Generic.List<EcnExecutablePlanStep> children)
    {
        EcnEncodingField[] fields = definition.Constructor == null
            ? []
            : definition.Constructor.Fields;
        int index = 0;
        while (index < fields.Length)
        {
            EcnEncodingField field = fields[index];
            EcnExecutablePlanStep step = this.CreateStep(
                all, plan, EcnPlanStepKind.Field, field.Name,
                field.Name, field.Optional ? "optional-field" : "field",
                field.OptionalClassReference, "field", field.Name, index,
                field.Span);
            step.Dependencies =
            [
                this.CreateDependency(
                    EcnPlanDependencyKind.FieldValue,
                    EcnPlanUnit.Values,
                    "",
                    field.Name,
                    field.OptionalClassSymbol == null
                        ? ""
                        : this.SymbolStableId(field.OptionalClassSymbol),
                    0,
                    true,
                    field.Span)
            ];
            children.Add(step);
            index = index + 1;
        }
    }

    internal void AppendLinkSteps(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan,
        System.Collections.Generic.List<EcnExecutablePlanStep> all,
        System.Collections.Generic.List<EcnExecutablePlanStep> children)
    {
        int index = 0;
        while (index < definition.Links.Length)
        {
            EcnEncodingLink link = definition.Links[index];
            string path = this.PathText(link.TargetPath);
            bool standard = link.PredeclaredObjectSet != null;
            string stableTarget = standard
                ? link.PredeclaredObjectSet == null
                    ? ""
                    : link.PredeclaredObjectSet.StableId
                : link.ReferenceSymbol == null
                    ? ""
                    : this.SymbolStableId(link.ReferenceSymbol);
            EcnExecutablePlanStep step = this.CreateStep(
                all,
                plan,
                standard
                    ? EcnPlanStepKind.StandardEncoding
                    : EcnPlanStepKind.Field,
                link.ReferenceName,
                path,
                link.Kind.ToString(),
                stableTarget,
                "link",
                link.ReferenceName,
                link.Order,
                link.Span);
            step.Dependencies =
            [
                this.CreateDependency(
                    standard
                        ? EcnPlanDependencyKind.EncodingObjectSet
                        : EcnPlanDependencyKind.EncodingObject,
                    EcnPlanUnit.Values,
                    "",
                    path,
                    stableTarget,
                    0,
                    true,
                    link.Span)
            ];
            children.Add(step);
            index = index + 1;
        }
    }

    internal void AppendOpenTypeStep(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan,
        System.Collections.Generic.List<EcnExecutablePlanStep> all,
        System.Collections.Generic.List<EcnExecutablePlanStep> children)
    {
        EcnOpenTypeMapping? mapping = definition.Conversion == null
            ? null
            : definition.Conversion.OpenTypeMapping;
        if (mapping == null)
        {
            return;
        }
        EcnExecutablePlanStep step = this.CreateStep(
            all, plan, EcnPlanStepKind.OpenType, "open-type", "$value",
            "select-encoding", mapping.KnownEncodingsName,
            "open-type", mapping.UnknownStructureName, 0, mapping.Span);
        System.Collections.Generic.List<EcnPlanDependency> dependencies =
            new System.Collections.Generic.List<EcnPlanDependency>();
        if (mapping.KnownPredeclaredEncodings != null)
        {
            dependencies.Add(this.CreateDependency(
                EcnPlanDependencyKind.EncodingObjectSet,
                EcnPlanUnit.Values,
                "",
                "$value",
                mapping.KnownPredeclaredEncodings.StableId,
                0,
                true,
                mapping.Span));
        }
        else if (mapping.KnownEncodingsSymbol != null)
        {
            dependencies.Add(this.CreateDependency(
                EcnPlanDependencyKind.EncodingObjectSet,
                EcnPlanUnit.Values,
                "",
                "$value",
                this.SymbolStableId(mapping.KnownEncodingsSymbol),
                0,
                true,
                mapping.Span));
        }
        if (mapping.UnknownPredeclaredEncodings != null)
        {
            dependencies.Add(this.CreateDependency(
                EcnPlanDependencyKind.EncodingObjectSet,
                EcnPlanUnit.Values,
                "",
                mapping.UnknownStructureName,
                mapping.UnknownPredeclaredEncodings.StableId,
                0,
                true,
                mapping.Span));
        }
        else if (mapping.UnknownEncodingsSymbol != null)
        {
            dependencies.Add(this.CreateDependency(
                EcnPlanDependencyKind.EncodingObjectSet,
                EcnPlanUnit.Values,
                "",
                mapping.UnknownStructureName,
                this.SymbolStableId(mapping.UnknownEncodingsSymbol),
                0,
                true,
                mapping.Span));
        }
        step.Dependencies = dependencies.ToArray();
        step.Buffer.RequiresLookahead = true;
        children.Add(step);
    }

    internal void AddOrderingDependencies(
        System.Collections.Generic.List<EcnExecutablePlanStep> steps)
    {
        int index = 1;
        while (index < steps.Count)
        {
            EcnPlanDependency ordering = this.CreateDependency(
                EcnPlanDependencyKind.Ordering,
                EcnPlanUnit.None,
                steps[index - 1].StableId,
                steps[index].Path,
                "",
                0,
                true,
                steps[index].Trace.Span);
            steps[index].Dependencies = this.AppendDependency(
                steps[index].Dependencies,
                ordering);
            index = index + 1;
        }
    }

    internal void AggregatePlanMetadata(
        EcnExecutablePlan plan,
        EcnExecutablePlanStep root,
        System.Collections.Generic.List<EcnExecutablePlanStep> steps)
    {
        int index = 1;
        while (index < steps.Count)
        {
            EcnExecutablePlanStep step = steps[index];
            if (step.Bounds.Exact && step.Kind == EcnPlanStepKind.Length)
            {
                plan.Bounds = this.CopyBounds(step.Bounds);
                root.Bounds = this.CopyBounds(step.Bounds);
            }
            if (step.Buffer.MinimumBits > plan.Buffer.MinimumBits)
            {
                plan.Buffer.MinimumBits = step.Buffer.MinimumBits;
            }
            if (step.Buffer.MinimumBytes > plan.Buffer.MinimumBytes)
            {
                plan.Buffer.MinimumBytes = step.Buffer.MinimumBytes;
            }
            plan.Buffer.RequiresWholeValue = plan.Buffer.RequiresWholeValue
                || step.Buffer.RequiresWholeValue;
            plan.Buffer.RequiresLookahead = plan.Buffer.RequiresLookahead
                || step.Buffer.RequiresLookahead;
            plan.Buffer.RequiresBackpatch = plan.Buffer.RequiresBackpatch
                || step.Buffer.RequiresBackpatch;
            plan.Buffer.RequiresRandomAccess = plan.Buffer.RequiresRandomAccess
                || step.Buffer.RequiresRandomAccess;
            plan.Buffer.Streamable = plan.Buffer.Streamable
                && step.Buffer.Streamable;
            plan.Reversible = plan.Reversible && step.Reversible;
            index = index + 1;
        }
        root.Buffer = this.CopyBuffer(plan.Buffer);
        root.Reversible = plan.Reversible;
    }

    internal EcnExecutablePlanStep CreateStep(
        System.Collections.Generic.List<EcnExecutablePlanStep> steps,
        EcnExecutablePlan plan,
        EcnPlanStepKind kind,
        string name,
        string path,
        string operation,
        string value,
        string semanticKind,
        string semanticName,
        int declarationOrder,
        AsnSourceSpan? span)
    {
        int order = steps.Count;
        EcnExecutablePlanStep step = new EcnExecutablePlanStep
        {
            StableId = plan.StableId + "/step/"
                + order.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
            Kind = kind,
            Name = name,
            Path = path,
            Operation = operation,
            Value = value,
            Order = order,
            Trace = new EcnPlanSourceTrace
            {
                ModuleName = plan.ModuleName,
                ObjectName = plan.ObjectName,
                SemanticKind = semanticKind,
                SemanticName = semanticName,
                Path = path,
                DeclarationOrder = declarationOrder,
                Span = span
            }
        };
        if (kind == EcnPlanStepKind.Choice
            || kind == EcnPlanStepKind.Optional
            || kind == EcnPlanStepKind.OpenType)
        {
            step.Buffer.RequiresLookahead = true;
        }
        steps.Add(step);
        return step;
    }

    internal EcnPlanDependency CreateDependency(
        EcnPlanDependencyKind kind,
        EcnPlanUnit unit,
        string sourceStepId,
        string path,
        string stableTargetId,
        long minimum,
        bool mustPrecede,
        AsnSourceSpan? span)
    {
        return new EcnPlanDependency
        {
            Kind = kind,
            Unit = unit,
            SourceStepId = sourceStepId,
            Path = path,
            StableTargetId = stableTargetId,
            Minimum = minimum,
            MustPrecede = mustPrecede,
            Span = span
        };
    }

    internal EcnPlanDependency[] AppendDependency(
        EcnPlanDependency[] source,
        EcnPlanDependency dependency)
    {
        EcnPlanDependency[] result =
            new EcnPlanDependency[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        result[source.Length] = dependency;
        return result;
    }

    internal EcnPlanBounds CopyBounds(EcnPlanBounds source)
    {
        return new EcnPlanBounds
        {
            MinimumBits = source.MinimumBits,
            MaximumBits = source.MaximumBits,
            MinimumBytes = source.MinimumBytes,
            MaximumBytes = source.MaximumBytes,
            LowerValue = source.LowerValue,
            UpperValue = source.UpperValue,
            HasMinimumBits = source.HasMinimumBits,
            HasMaximumBits = source.HasMaximumBits,
            HasMinimumBytes = source.HasMinimumBytes,
            HasMaximumBytes = source.HasMaximumBytes,
            HasLowerValue = source.HasLowerValue,
            HasUpperValue = source.HasUpperValue,
            Exact = source.Exact
        };
    }

    internal EcnPlanBufferRequirement CopyBuffer(
        EcnPlanBufferRequirement source)
    {
        return new EcnPlanBufferRequirement
        {
            MinimumBits = source.MinimumBits,
            MinimumBytes = source.MinimumBytes,
            RequiresWholeValue = source.RequiresWholeValue,
            RequiresLookahead = source.RequiresLookahead,
            RequiresBackpatch = source.RequiresBackpatch,
            RequiresRandomAccess = source.RequiresRandomAccess,
            Streamable = source.Streamable
        };
    }

    internal EcnPlanStepKind GetRootKind(
        EcnEncodingObjectDefinition definition)
    {
        EcnConstructorKind kind = definition.Constructor == null
            ? EcnConstructorKind.Unknown
            : definition.Constructor.Kind;
        switch (kind)
        {
            case EcnConstructorKind.Concatenation:
                return EcnPlanStepKind.Sequence;
            case EcnConstructorKind.Alternatives:
                return EcnPlanStepKind.Choice;
            case EcnConstructorKind.Repetition:
            case EcnConstructorKind.ConditionalRepetition:
                return EcnPlanStepKind.Repetition;
            case EcnConstructorKind.Optionality:
                return EcnPlanStepKind.Optional;
            case EcnConstructorKind.Tag:
                return EcnPlanStepKind.Tag;
            default:
                return EcnPlanStepKind.Primitive;
        }
    }

    internal EcnPlanStepKind GetPropertyStepKind(
        EcnEncodingPropertyKind kind)
    {
        if (kind == EcnEncodingPropertyKind.Size
            || kind == EcnEncodingPropertyKind.EncodingSpace
            || kind == EcnEncodingPropertyKind.RepetitionSpace)
        {
            return EcnPlanStepKind.Length;
        }
        if (kind == EcnEncodingPropertyKind.Alignment)
        {
            return EcnPlanStepKind.Alignment;
        }
        if (kind == EcnEncodingPropertyKind.Padding
            || kind == EcnEncodingPropertyKind.ValuePadding
            || kind == EcnEncodingPropertyKind.UnusedBits)
        {
            return EcnPlanStepKind.Padding;
        }
        return EcnPlanStepKind.Property;
    }

    internal bool PropertyUsesBytes(EcnEncodingProperty property)
    {
        return property.Unit == "octet"
            || property.Unit == "octets"
            || property.Value.IndexOf(
                "octet",
                System.StringComparison.Ordinal) >= 0;
    }

    internal string PathText(EcnFieldPath? path)
    {
        if (path == null)
        {
            return "";
        }
        return path.NormalizedText.Length > 0
            ? path.NormalizedText
            : path.Text;
    }

    internal string SymbolStableId(EcnSymbol symbol)
    {
        if (symbol.QualifiedName.Length > 0)
        {
            return "ecn:symbol:" + symbol.QualifiedName;
        }
        return "ecn:symbol:" + symbol.SourceModuleName + ":" + symbol.Name;
    }
}
