namespace asn1;

internal class PerInstructionCompiler
{
    internal DiagnosticBag Diagnostics;
    internal PerInstructionRegistrySnapshot Registry;
    internal AsnEncodingRules Rules;

    internal PerInstructionCompiler(
        DiagnosticBag diagnostics,
        PerInstructionRegistrySnapshot registry,
        AsnEncodingRules rules)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
        this.Registry = registry == null
            ? throw new System.ArgumentNullException("registry")
            : registry;
        this.Rules = rules;
    }

    internal bool CompileTypes(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (this.Rules != AsnEncodingRules.Uper
            && this.Rules != AsnEncodingRules.CanonicalUnalignedPer)
        {
            return true;
        }

        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int typeIndex = 0;
            while (typeIndex < tables[tableIndex].TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = tables[tableIndex].TypeSymbols[typeIndex]
                    .ResolvedType;
                if (type != null)
                {
                    this.CompileType(type, visited);
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return !this.Diagnostics.ContainsErrors();
    }

    internal void CompileType(
        ResolvedTypeSyntax type,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (visited.Contains(type))
        {
            return;
        }

        visited.Add(type);
        string identity = type.SourceDeclaration == null
            ? type.EffectiveDeclaration == null
                ? type.EffectiveKind.ToString()
                : type.EffectiveDeclaration.QualifiedName
            : type.SourceDeclaration.QualifiedName;
        type.PerInstructionPlanSteps = this.CompileInstructionSet(
            type.FinalPerEncodingInstructions,
            this.CreatePublicType(type),
            identity);

        if (type.AliasTarget != null)
        {
            this.CompileType(type.AliasTarget, visited);
        }

        if (type.InnerType != null)
        {
            this.CompileType(type.InnerType, visited);
        }

        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedComponentSyntax component = type.Components[index];
            ResolvedTypeSyntax? componentType = component.TypeResolution;
            if (componentType != null)
            {
                this.CompileType(componentType, visited);
                if (component.FinalPerEncodingInstructions
                    != componentType.FinalPerEncodingInstructions)
                {
                    string componentName = component.Component == null
                        ? index.ToString(
                            System.Globalization.CultureInfo.InvariantCulture)
                        : component.Component.Name;
                    component.PerInstructionPlanSteps = this.CompileInstructionSet(
                        component.FinalPerEncodingInstructions,
                        this.CreatePublicType(componentType),
                        identity + "." + componentName);
                }
            }

            index = index + 1;
        }

        index = 0;
        while (index < type.ConstraintTypes.Length)
        {
            this.CompileType(type.ConstraintTypes[index], visited);
            index = index + 1;
        }
    }

    internal AsnEncodingPlanStep[] CompileInstructionSet(
        AsnEncodingInstruction[] instructions,
        AsnType type,
        string identity)
    {
        System.Collections.Generic.List<AsnEncodingPlanStep> steps =
            new System.Collections.Generic.List<AsnEncodingPlanStep>();
        int index = 0;
        while (index < instructions.Length)
        {
            AsnEncodingInstruction instruction = instructions[index];
            AsnPerInstructionRegistration? registration =
                this.Registry.ResolveRegistration(instruction.Name);
            IPerInstructionHandler? handler =
                this.Registry.ResolveHandler(instruction.Name);
            bool valid = true;
            if (registration == null || handler == null)
            {
                this.AddError(
                    188,
                    "Unaligned PER instruction is not supported by the compilation registry: "
                        + instruction.Name,
                    instruction.Span);
                valid = false;
            }
            else
            {
                if (!registration.SupportsUnaligned)
                {
                    this.AddError(
                        189,
                        "PER instruction handler is not applicable to unaligned PER: "
                            + instruction.Name,
                        instruction.Span);
                    valid = false;
                }

                if (!registration.Reversible)
                {
                    this.AddError(
                        191,
                        "PER instruction handler is not reversible for decoding: "
                            + instruction.Name,
                        instruction.Span);
                    valid = false;
                }

                if (this.HasConflict(registration, instructions, index))
                {
                    this.AddError(
                        190,
                        "Conflicting PER instructions are applied to " + identity
                            + ": " + instruction.Name,
                        instruction.Span);
                    valid = false;
                }

                AsnDiagnostic[]? handlerDiagnostics = handler.ValidateInstruction(
                    this.CloneInstruction(instruction),
                    type);
                if (handlerDiagnostics == null)
                {
                    this.AddError(
                        192,
                        "PER instruction handler returned no validation result: "
                            + instruction.Name,
                        instruction.Span);
                    valid = false;
                }
                else
                {
                    int diagnosticIndex = 0;
                    while (diagnosticIndex < handlerDiagnostics.Length)
                    {
                        AsnDiagnostic diagnostic = handlerDiagnostics[diagnosticIndex];
                        if (diagnostic.Span == null)
                        {
                            diagnostic = this.CloneDiagnosticWithSpan(
                                diagnostic,
                                instruction.Span);
                        }

                        this.Diagnostics.AddDiagnostic(diagnostic);
                        if (diagnostic.Severity == AsnDiagnosticSeverity.Error)
                        {
                            valid = false;
                        }

                        diagnosticIndex = diagnosticIndex + 1;
                    }
                }

                if (valid)
                {
                    AsnEncodingPlan plan = new AsnEncodingPlan
                    {
                        StableId = identity + "/uper",
                        EncodingRules = this.Rules,
                        Steps = this.CloneSteps(steps.ToArray()),
                        Instructions = this.CloneInstructions(instructions),
                        PerInstructionRegistrations =
                            this.Registry.GetRegistrations()
                    };
                    AsnEncodingPlanStep[]? contributed = handler.ApplyInstruction(
                        this.CloneInstruction(instruction),
                        type,
                        plan);
                    if (contributed == null)
                    {
                        this.AddError(
                            192,
                            "PER instruction handler returned no lowering steps: "
                                + instruction.Name,
                            instruction.Span);
                    }
                    else
                    {
                        steps.AddRange(this.CloneSteps(contributed));
                    }
                }
            }

            index = index + 1;
        }

        return steps.ToArray();
    }

    internal bool HasConflict(
        AsnPerInstructionRegistration registration,
        AsnEncodingInstruction[] instructions,
        int instructionIndex)
    {
        int otherIndex = instructionIndex + 1;
        while (otherIndex < instructions.Length)
        {
            AsnPerInstructionRegistration? other =
                this.Registry.ResolveRegistration(instructions[otherIndex].Name);
            if (this.ContainsKeyword(
                    registration.ConflictingKeywords,
                    instructions[otherIndex].Name)
                || (other != null
                    && this.ContainsKeyword(
                        other.ConflictingKeywords,
                        registration.Keyword)))
            {
                return true;
            }

            otherIndex = otherIndex + 1;
        }

        return false;
    }

    internal bool ContainsKeyword(string[] keywords, string keyword)
    {
        int index = 0;
        while (index < keywords.Length)
        {
            if (keywords[index] == keyword)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal AsnType CreatePublicType(ResolvedTypeSyntax type)
    {
        AsnType? source = type.EffectiveType == null
            ? type.SourceType
            : type.EffectiveType;
        return new AsnType
        {
            Name = source == null ? "" : source.Name,
            Kind = type.EffectiveKind,
            Constraints = source == null ? [] : source.Constraints,
            Components = source == null ? [] : source.Components,
            ElementType = source == null ? null : source.ElementType,
            NamedValues = source == null ? [] : source.NamedValues,
            Extensible = source != null && source.Extensible
        };
    }

    internal void AddError(int number, string message, AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Encoding,
            number,
            message,
            span,
            []);
    }

    internal AsnDiagnostic CloneDiagnosticWithSpan(
        AsnDiagnostic source,
        AsnSourceSpan? span)
    {
        AsnDiagnostic clone = this.Diagnostics.CloneDiagnostic(source);
        clone.Span = span == null ? null : this.Diagnostics.CloneSpan(span);
        return clone;
    }

    internal AsnEncodingInstruction[] CloneInstructions(
        AsnEncodingInstruction[] source)
    {
        AsnEncodingInstruction[] result =
            new AsnEncodingInstruction[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.CloneInstruction(source[index]);
            index = index + 1;
        }

        return result;
    }

    internal AsnEncodingInstruction CloneInstruction(
        AsnEncodingInstruction source)
    {
        return new AsnEncodingInstruction
        {
            Name = source.Name,
            Kind = source.Kind,
            Negated = source.Negated,
            Arguments = this.CopyStrings(source.Arguments),
            Targets = this.CopyStrings(source.Targets),
            Span = source.Span == null
                ? null
                : this.Diagnostics.CloneSpan(source.Span)
        };
    }

    internal AsnEncodingPlanStep[] CloneSteps(AsnEncodingPlanStep[] source)
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
                    : this.CloneInstruction(step.Instruction),
                Path = step.Path,
                Children = this.CloneSteps(step.Children)
            };
            index = index + 1;
        }

        return result;
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
}
