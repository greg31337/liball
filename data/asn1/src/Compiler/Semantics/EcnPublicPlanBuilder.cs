namespace asn1;

internal class EcnPublicPlanBuilder
{
    internal AsnEncodingPlan[] BuildPlans(EcnSymbolTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }
        AsnEcnClass[] classes = this.BuildClasses(table);
        AsnEcnObject[] objects = this.BuildObjects(table);
        System.Collections.Generic.List<AsnEncodingPlan> result =
            new System.Collections.Generic.List<AsnEncodingPlan>();
        int index = 0;
        while (index < table.EncodingObjects.Length)
        {
            EcnSymbol symbol = table.EncodingObjects[index];
            if (!symbol.Imported && symbol.ObjectDefinition != null
                && symbol.ObjectDefinition.Plan != null
                && symbol.ObjectDefinition.Plan.Valid)
            {
                result.Add(this.BuildPlan(
                    symbol.ObjectDefinition.Plan,
                    classes,
                    objects));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal AsnEncodingPlan BuildPlan(
        EcnExecutablePlan source,
        AsnEcnClass[] classes,
        AsnEcnObject[] objects)
    {
        AsnEncodingInstruction[] instructions =
            this.BuildPlanInstructions(source);
        return new AsnEncodingPlan
        {
            StableId = source.StableId,
            EncodingRules = AsnEncodingRules.Ecn,
            Steps = source.Root == null
                ? []
                : [this.BuildStep(source.Root)],
            Instructions = instructions,
            EcnClasses = classes,
            EcnObjects = objects
        };
    }

    internal AsnEncodingPlanStep BuildStep(EcnExecutablePlanStep source)
    {
        AsnEncodingPlanStep[] children =
            new AsnEncodingPlanStep[source.Children.Length];
        int index = 0;
        while (index < children.Length)
        {
            children[index] = this.BuildStep(source.Children[index]);
            index = index + 1;
        }
        return new AsnEncodingPlanStep
        {
            Kind = this.MapStepKind(source.Kind),
            Instruction = source.Trace.SemanticKind == "object"
                ? null
                : this.BuildStepInstruction(source),
            Path = source.Path,
            Children = children
        };
    }

    internal AsnEncodingInstruction BuildStepInstruction(
        EcnExecutablePlanStep source)
    {
        return new AsnEncodingInstruction
        {
            Name = source.Operation.Length == 0
                ? source.Name
                : source.Operation,
            Kind = AsnInstructionKind.Ecn,
            Arguments = source.Value.Length == 0 ? [] : [source.Value],
            Targets = source.Path.Length == 0 ? [] : [source.Path],
            Span = this.CloneSpan(source.Trace.Span)
        };
    }

    internal AsnEncodingInstruction[] BuildPlanInstructions(
        EcnExecutablePlan source)
    {
        System.Collections.Generic.List<AsnEncodingInstruction> result =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        int index = 0;
        while (index < source.Steps.Length)
        {
            if (source.Steps[index].Trace.SemanticKind != "object")
            {
                result.Add(this.BuildStepInstruction(source.Steps[index]));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal AsnEcnClass[] BuildClasses(EcnSymbolTable table)
    {
        System.Collections.Generic.List<AsnEcnClass> result =
            new System.Collections.Generic.List<AsnEcnClass>();
        int index = 0;
        while (index < table.EncodingClasses.Length)
        {
            EcnSymbol symbol = table.EncodingClasses[index];
            if (!symbol.Imported)
            {
                AsnEncodingPlanStep[] steps =
                    this.FindClassPlanSteps(table, symbol.Name);
                result.Add(new AsnEcnClass
                {
                    Name = symbol.Name,
                    Category = symbol.ClassCategory.ToString(),
                    Steps = steps
                });
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal AsnEncodingPlanStep[] FindClassPlanSteps(
        EcnSymbolTable table,
        string className)
    {
        int index = 0;
        while (index < table.EncodingObjects.Length)
        {
            EcnEncodingObjectDefinition? definition =
                table.EncodingObjects[index].ObjectDefinition;
            if (definition != null && definition.GovernorName == className
                && definition.Plan != null && definition.Plan.Root != null)
            {
                return [this.BuildStep(definition.Plan.Root)];
            }
            index = index + 1;
        }
        return [];
    }

    internal AsnEcnObject[] BuildObjects(EcnSymbolTable table)
    {
        System.Collections.Generic.List<AsnEcnObject> result =
            new System.Collections.Generic.List<AsnEcnObject>();
        int index = 0;
        while (index < table.EncodingObjects.Length)
        {
            EcnEncodingObjectDefinition? definition =
                table.EncodingObjects[index].ObjectDefinition;
            if (!table.EncodingObjects[index].Imported && definition != null)
            {
                result.Add(new AsnEcnObject
                {
                    Name = definition.Name,
                    ClassName = definition.GovernorName,
                    Instructions = definition.Plan == null
                        ? []
                        : this.BuildPlanInstructions(definition.Plan)
                });
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal AsnPlanStepKind MapStepKind(EcnPlanStepKind kind)
    {
        switch (kind)
        {
            case EcnPlanStepKind.Primitive:
                return AsnPlanStepKind.Primitive;
            case EcnPlanStepKind.Sequence:
                return AsnPlanStepKind.Sequence;
            case EcnPlanStepKind.Choice:
                return AsnPlanStepKind.Choice;
            case EcnPlanStepKind.Repetition:
                return AsnPlanStepKind.Repetition;
            case EcnPlanStepKind.Optional:
                return AsnPlanStepKind.Optional;
            case EcnPlanStepKind.Tag:
                return AsnPlanStepKind.Tag;
            case EcnPlanStepKind.Length:
                return AsnPlanStepKind.Length;
            case EcnPlanStepKind.Alignment:
                return AsnPlanStepKind.Alignment;
            case EcnPlanStepKind.Padding:
                return AsnPlanStepKind.Padding;
            case EcnPlanStepKind.Condition:
                return AsnPlanStepKind.Condition;
            case EcnPlanStepKind.DeterminantRead:
            case EcnPlanStepKind.DeterminantWrite:
                return AsnPlanStepKind.Determinant;
            case EcnPlanStepKind.Mapping:
                return AsnPlanStepKind.Mapping;
            case EcnPlanStepKind.Replacement:
                return AsnPlanStepKind.Replacement;
            case EcnPlanStepKind.Transformation:
                return AsnPlanStepKind.Transformation;
            case EcnPlanStepKind.OpenType:
                return AsnPlanStepKind.OpenType;
            case EcnPlanStepKind.StandardEncoding:
                return AsnPlanStepKind.StandardEncoding;
            default:
                return AsnPlanStepKind.Unknown;
        }
    }

    internal AsnSourceSpan? CloneSpan(AsnSourceSpan? source)
    {
        if (source == null)
        {
            return null;
        }
        return new AsnSourceSpan
        {
            Start = new AsnSourcePosition
            {
                SourceName = source.Start.SourceName,
                Offset = source.Start.Offset,
                Line = source.Start.Line,
                Column = source.Start.Column
            },
            End = new AsnSourcePosition
            {
                SourceName = source.End.SourceName,
                Offset = source.End.Offset,
                Line = source.End.Line,
                Column = source.End.Column
            }
        };
    }
}
