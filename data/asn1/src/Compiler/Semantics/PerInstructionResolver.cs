namespace asn1;

internal class PerInstructionResolver
{
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];

    internal PerInstructionResolver(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal PerInstructionResolutionResult ResolveInstructions(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        int before = this.Diagnostics.GetCount();
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            this.ResetModuleTypes(tables[tableIndex]);
            tableIndex = tableIndex + 1;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            this.ExpandTargetedInstructions(tables[tableIndex]);
            tableIndex = tableIndex + 1;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            SymbolTable table = tables[tableIndex];
            int typeIndex = 0;
            while (typeIndex < table.TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = table.TypeSymbols[typeIndex].ResolvedType;
                if (type != null)
                {
                    this.ResolveTypeInstructions(type, table);
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return new PerInstructionResolutionResult
        {
            Success = this.Diagnostics.GetCount() == before
        };
    }

    internal void ResetModuleTypes(SymbolTable table)
    {
        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        int index = 0;
        while (index < table.TypeSymbols.Length)
        {
            ResolvedTypeSyntax? type = table.TypeSymbols[index].ResolvedType;
            if (type != null)
            {
                this.ResetTypeTree(type, visited);
            }

            index = index + 1;
        }
    }

    internal void ResetTypeTree(
        ResolvedTypeSyntax type,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (visited.Contains(type))
        {
            return;
        }

        visited.Add(type);
        type.PerTargetedEncodingInstructions = [];
        type.FinalPerEncodingInstructions = [];
        type.PerInstructionPlanSteps = [];
        type.PerInstructionResolutionState = 0;
        if (type.InnerType != null)
        {
            this.ResetTypeTree(type.InnerType, visited);
        }

        if (type.AliasTarget != null)
        {
            this.ResetTypeTree(type.AliasTarget, visited);
        }

        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedComponentSyntax componentSyntax = type.Components[index];
            componentSyntax.PerTargetedEncodingInstructions = [];
            componentSyntax.FinalPerEncodingInstructions = [];
            componentSyntax.PerInstructionPlanSteps = [];
            componentSyntax.PerInstructionResolutionState = 0;
            ResolvedTypeSyntax? component = componentSyntax.TypeResolution;
            if (component != null)
            {
                this.ResetTypeTree(component, visited);
            }

            index = index + 1;
        }

        index = 0;
        while (index < type.ConstraintTypes.Length)
        {
            this.ResetTypeTree(type.ConstraintTypes[index], visited);
            index = index + 1;
        }

        this.ResetContainedSubtypeInstructions(type.EffectiveConstraint, visited);
    }

    internal void ExpandTargetedInstructions(SymbolTable table)
    {
        ModuleSyntax? module = table.Module;
        if (module == null)
        {
            return;
        }

        int sectionIndex = 0;
        while (sectionIndex < module.EncodingControlSections.Length)
        {
            EncodingControlSectionSyntax section = module.EncodingControlSections[sectionIndex];
            if (section.EncodingReference == "PER")
            {
                int instructionIndex = 0;
                while (instructionIndex < section.Instructions.Length)
                {
                    ParsedEncodingInstructionSyntax instruction =
                        section.Instructions[instructionIndex];
                    int targetIndex = 0;
                    while (targetIndex < instruction.PerTargets.Length)
                    {
                        this.ExpandOneTarget(
                            table,
                            instruction.Instruction,
                            instruction.PerTargets[targetIndex]);
                        targetIndex = targetIndex + 1;
                    }

                    instructionIndex = instructionIndex + 1;
                }
            }

            sectionIndex = sectionIndex + 1;
        }
    }

    internal void ExpandOneTarget(
        SymbolTable table,
        AsnEncodingInstruction instruction,
        ParsedPerEncodingInstructionTargetSyntax target)
    {
        switch (target.Kind)
        {
            case PerEncodingInstructionTargetKind.AllTypes:
                this.ExpandAllTarget(table, instruction);
                return;
            case PerEncodingInstructionTargetKind.TypeReference:
                this.ExpandTypeTarget(table, instruction, target);
                return;
            case PerEncodingInstructionTargetKind.BuiltInType:
                this.ExpandBuiltInTarget(table, instruction, target.BuiltInType);
                return;
            case PerEncodingInstructionTargetKind.IdentifiersInContext:
                this.ExpandContextTarget(table, instruction, target);
                return;
        }
    }

    internal void ExpandAllTarget(SymbolTable table, AsnEncodingInstruction instruction)
    {
        int index = 0;
        while (index < table.TypeSymbols.Length)
        {
            ResolvedTypeSyntax? type = table.TypeSymbols[index].ResolvedType;
            if (type != null)
            {
                this.AppendAllOccurrences(type, instruction);
            }

            index = index + 1;
        }
    }

    internal void AppendAllOccurrences(
        ResolvedTypeSyntax type,
        AsnEncodingInstruction instruction)
    {
        this.AppendTargetedInstruction(type, instruction);
        this.AppendChildOccurrenceTargets(type, instruction, true, true);
    }

    internal void ExpandTypeTarget(
        SymbolTable table,
        AsnEncodingInstruction instruction,
        ParsedPerEncodingInstructionTargetSyntax target)
    {
        SymbolTableEntry? symbol = table.FindSymbol(
            target.TypeReference,
            AsnAssignmentKind.Type);
        ResolvedTypeSyntax? current = symbol == null ? null : symbol.ResolvedType;
        if (current == null)
        {
            return;
        }

        int pathIndex = 0;
        while (pathIndex < target.ComponentPath.Length)
        {
            string part = target.ComponentPath[pathIndex];
            bool last = pathIndex == target.ComponentPath.Length - 1;
            if (part == "ALL")
            {
                if (last)
                {
                    this.AppendChildOccurrenceTargets(current, instruction, true, false);
                }

                return;
            }

            if (part == "*")
            {
                current = this.FindCollectionElement(current);
            }
            else
            {
                ResolvedComponentSyntax? component = this.FindNamedComponent(current, part);
                if (component == null)
                {
                    return;
                }

                if (!last && this.IsComponentsOfComponent(component))
                {
                    return;
                }

                if (last)
                {
                    this.AppendComponentTargetedInstruction(component, instruction);
                    return;
                }

                current = component.TypeResolution;
            }

            if (current == null)
            {
                return;
            }

            pathIndex = pathIndex + 1;
        }

        this.AppendTargetedInstruction(current, instruction);
    }

    internal void ExpandBuiltInTarget(
        SymbolTable table,
        AsnEncodingInstruction instruction,
        string builtInType)
    {
        AsnTypeKind kind = this.GetBuiltInKind(builtInType);
        if (kind == AsnTypeKind.Unknown)
        {
            return;
        }

        int index = 0;
        while (index < table.TypeSymbols.Length)
        {
            ResolvedTypeSyntax? type = table.TypeSymbols[index].ResolvedType;
            if (type != null)
            {
                this.AppendBuiltInOccurrences(type, instruction, kind);
            }

            index = index + 1;
        }
    }

    internal void AppendBuiltInOccurrences(
        ResolvedTypeSyntax type,
        AsnEncodingInstruction instruction,
        AsnTypeKind kind)
    {
        ResolvedTypeSyntax baseType = this.UnwrapOccurrence(type);
        ParsedTypeSyntax? source = baseType.SourceSyntax;
        if (source != null && source.Type.Kind == kind)
        {
            this.AppendTargetedInstruction(type, instruction);
        }

        this.AppendBuiltInChildTargets(type, instruction, kind);
    }

    internal void AppendBuiltInChildTargets(
        ResolvedTypeSyntax type,
        AsnEncodingInstruction instruction,
        AsnTypeKind kind)
    {
        ResolvedTypeSyntax baseType = this.UnwrapOccurrence(type);
        ParsedTypeSyntax? source = baseType.SourceSyntax;
        if (source == null || source.Type.Kind == AsnTypeKind.Defined)
        {
            return;
        }

        if (source.Type.Kind == AsnTypeKind.SequenceOf
            || source.Type.Kind == AsnTypeKind.SetOf
            || source.Type.Kind == AsnTypeKind.Selection)
        {
            if (baseType.InnerType != null)
            {
                this.AppendBuiltInOccurrences(baseType.InnerType, instruction, kind);
            }

            return;
        }

        int index = 0;
        while (index < baseType.Components.Length)
        {
            ResolvedComponentSyntax componentSyntax = baseType.Components[index];
            ResolvedTypeSyntax? component = componentSyntax.TypeResolution;
            if (component != null && !this.IsComponentsOfComponent(componentSyntax))
            {
                this.AppendBuiltInOccurrences(component, instruction, kind);
            }

            index = index + 1;
        }
    }

    internal void ExpandContextTarget(
        SymbolTable table,
        AsnEncodingInstruction instruction,
        ParsedPerEncodingInstructionTargetSyntax target)
    {
        ResolvedTypeSyntax? context = this.FindSingleTypeTarget(table, target);
        if (context == null)
        {
            return;
        }

        ResolvedTypeSyntax baseType = this.UnwrapOccurrence(context);
        ParsedTypeSyntax? source = baseType.SourceSyntax;
        if (source == null
            || (source.Type.Kind != AsnTypeKind.Sequence
                && source.Type.Kind != AsnTypeKind.Set
                && source.Type.Kind != AsnTypeKind.Choice))
        {
            return;
        }

        if (target.AllIdentifiers)
        {
            this.AppendChildOccurrenceTargets(context, instruction, true, false);
            return;
        }

        if (target.Components)
        {
            this.AppendChildOccurrenceTargets(context, instruction, false, false);
            return;
        }

        int index = 0;
        while (index < target.Identifiers.Length)
        {
            ResolvedComponentSyntax? component = this.FindNamedComponent(
                context,
                target.Identifiers[index]);
            if (component != null)
            {
                this.AppendComponentTargetedInstruction(component, instruction);
            }

            index = index + 1;
        }
    }

    internal ResolvedTypeSyntax? FindSingleTypeTarget(
        SymbolTable table,
        ParsedPerEncodingInstructionTargetSyntax target)
    {
        if (target.TypeReference == "ALL")
        {
            return null;
        }

        SymbolTableEntry? symbol = table.FindSymbol(
            target.TypeReference,
            AsnAssignmentKind.Type);
        ResolvedTypeSyntax? current = symbol == null ? null : symbol.ResolvedType;
        int index = 0;
        while (current != null && index < target.ComponentPath.Length)
        {
            string part = target.ComponentPath[index];
            if (part == "ALL")
            {
                return null;
            }

            if (part == "*")
            {
                current = this.FindCollectionElement(current);
            }
            else
            {
                ResolvedComponentSyntax? component = this.FindNamedComponent(current, part);
                if (component == null
                    || (index + 1 < target.ComponentPath.Length
                        && this.IsComponentsOfComponent(component)))
                {
                    return null;
                }

                current = component.TypeResolution;
            }

            index = index + 1;
        }

        return current;
    }

    internal ResolvedComponentSyntax? FindNamedComponent(
        ResolvedTypeSyntax type,
        string name)
    {
        ResolvedTypeSyntax baseType = this.UnwrapOccurrence(type);
        ParsedTypeSyntax? source = baseType.SourceSyntax;
        if (source == null
            || (source.Type.Kind != AsnTypeKind.Sequence
                && source.Type.Kind != AsnTypeKind.Set
                && source.Type.Kind != AsnTypeKind.Choice))
        {
            return null;
        }

        int index = 0;
        while (index < baseType.Components.Length)
        {
            ResolvedComponentSyntax component = baseType.Components[index];
            if (component.Component != null && component.Component.Name == name)
            {
                return component;
            }

            index = index + 1;
        }

        return null;
    }

    internal ResolvedTypeSyntax? FindCollectionElement(ResolvedTypeSyntax type)
    {
        ResolvedTypeSyntax baseType = this.UnwrapOccurrence(type);
        ParsedTypeSyntax? source = baseType.SourceSyntax;
        if (source == null)
        {
            return null;
        }

        if (source.Type.Kind == AsnTypeKind.SequenceOf
            || source.Type.Kind == AsnTypeKind.SetOf)
        {
            return baseType.InnerType;
        }

        if ((source.Type.Kind == AsnTypeKind.BitString
                || source.Type.Kind == AsnTypeKind.OctetString)
            && baseType.ContentsConstraints.Length > 0)
        {
            return baseType.ContentsConstraints[0].ContainedType;
        }

        return null;
    }

    internal bool IsComponentsOfComponent(ResolvedComponentSyntax component)
    {
        int index = 0;
        while (index < component.Provenance.Length)
        {
            if (component.Provenance[index].ComponentsOf)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AppendChildOccurrenceTargets(
        ResolvedTypeSyntax type,
        AsnEncodingInstruction instruction,
        bool recursive,
        bool includeSelectionOperand)
    {
        ResolvedTypeSyntax baseType = this.UnwrapOccurrence(type);
        ParsedTypeSyntax? source = baseType.SourceSyntax;
        if (source == null || source.Type.Kind == AsnTypeKind.Defined)
        {
            return;
        }

        if (source.Type.Kind == AsnTypeKind.SequenceOf
            || source.Type.Kind == AsnTypeKind.SetOf
            || (includeSelectionOperand && source.Type.Kind == AsnTypeKind.Selection))
        {
            ResolvedTypeSyntax? inner = baseType.InnerType;
            if (inner != null)
            {
                this.AppendTargetedInstruction(inner, instruction);
                if (recursive)
                {
                    this.AppendChildOccurrenceTargets(inner, instruction, true, true);
                }
            }

            return;
        }

        int index = 0;
        while (index < baseType.Components.Length)
        {
            ResolvedComponentSyntax componentSyntax = baseType.Components[index];
            ResolvedTypeSyntax? component = componentSyntax.TypeResolution;
            if (component != null)
            {
                this.AppendComponentTargetedInstruction(componentSyntax, instruction);
                if (recursive && !this.IsComponentsOfComponent(componentSyntax))
                {
                    this.AppendChildOccurrenceTargets(component, instruction, true, true);
                }
            }

            index = index + 1;
        }
    }

    internal ResolvedTypeSyntax UnwrapOccurrence(ResolvedTypeSyntax type)
    {
        ResolvedTypeSyntax current = type;
        ParsedTypeSyntax? source = current.SourceSyntax;
        while (source != null
            && (source.Type.Kind == AsnTypeKind.Prefixed
                || source.Type.Kind == AsnTypeKind.Tagged)
            && current.InnerType != null)
        {
            current = current.InnerType;
            source = current.SourceSyntax;
        }

        return current;
    }

    internal void AppendTargetedInstruction(
        ResolvedTypeSyntax type,
        AsnEncodingInstruction instruction)
    {
        AsnEncodingInstruction[] current = type.PerTargetedEncodingInstructions;
        AsnEncodingInstruction[] result = new AsnEncodingInstruction[current.Length + 1];
        int index = 0;
        while (index < current.Length)
        {
            result[index] = current[index];
            index = index + 1;
        }

        result[current.Length] = instruction;
        type.PerTargetedEncodingInstructions = result;
    }

    internal void AppendComponentTargetedInstruction(
        ResolvedComponentSyntax component,
        AsnEncodingInstruction instruction)
    {
        if (!this.IsComponentsOfComponent(component) && component.TypeResolution != null)
        {
            this.AppendTargetedInstruction(component.TypeResolution, instruction);
            return;
        }

        AsnEncodingInstruction[] current = component.PerTargetedEncodingInstructions;
        AsnEncodingInstruction[] result = new AsnEncodingInstruction[current.Length + 1];
        int index = 0;
        while (index < current.Length)
        {
            result[index] = current[index];
            index = index + 1;
        }

        result[current.Length] = instruction;
        component.PerTargetedEncodingInstructions = result;
    }

    internal void ResolveTypeInstructions(ResolvedTypeSyntax type, SymbolTable module)
    {
        if (type.PerInstructionResolutionState == 2)
        {
            return;
        }

        if (type.PerInstructionResolutionState == 1)
        {
            return;
        }

        ParsedTypeSyntax? source = type.SourceSyntax;
        if (source != null
            && (source.Type.Kind == AsnTypeKind.Prefixed
                || source.Type.Kind == AsnTypeKind.Tagged))
        {
            this.ResolvePrefixedInstructions(type, module);
            return;
        }

        type.PerInstructionResolutionState = 1;
        System.Collections.Generic.List<AsnEncodingInstruction> final =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        this.AppendInitialInstructions(type, module, final);
        this.ApplyTargetedInstructions(type, final);
        type.FinalPerEncodingInstructions = final.ToArray();
        type.PerInstructionResolutionState = 2;
        this.ResolveChildInstructions(type, module);
        this.ResolveConstraintInstructions(type, module);
    }

    internal void ResolvePrefixedInstructions(ResolvedTypeSyntax type, SymbolTable module)
    {
        System.Collections.Generic.List<ResolvedTypeSyntax> wrappers =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        ResolvedTypeSyntax current = type;
        ParsedTypeSyntax? source = current.SourceSyntax;
        while (source != null
            && (source.Type.Kind == AsnTypeKind.Prefixed
                || source.Type.Kind == AsnTypeKind.Tagged)
            && current.InnerType != null)
        {
            current.PerInstructionResolutionState = 1;
            wrappers.Add(current);
            current = current.InnerType;
            source = current.SourceSyntax;
        }

        current.PerInstructionResolutionState = 1;
        System.Collections.Generic.List<AsnEncodingInstruction> final =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        this.AppendInitialInstructions(current, module, final);
        this.ApplyTargetedInstructions(type, final);
        current.FinalPerEncodingInstructions = final.ToArray();
        current.PerInstructionResolutionState = 2;

        int wrapperIndex = wrappers.Count - 1;
        while (wrapperIndex >= 0)
        {
            ResolvedTypeSyntax wrapper = wrappers[wrapperIndex];
            ParsedTypeSyntax? wrapperSource = wrapper.SourceSyntax;
            if (wrapperSource != null && wrapperSource.EncodingReference == "PER")
            {
                int instructionIndex = 0;
                while (instructionIndex < wrapperSource.EncodingInstructions.Length)
                {
                    this.ApplyInstruction(final, wrapperSource.EncodingInstructions[instructionIndex]);
                    instructionIndex = instructionIndex + 1;
                }
            }

            wrapper.FinalPerEncodingInstructions = final.ToArray();
            wrapper.PerInstructionResolutionState = 2;
            wrapperIndex = wrapperIndex - 1;
        }

        this.ResolveChildInstructions(current, module);
        this.ResolveConstraintInstructions(type, module);
    }

    internal void AppendInitialInstructions(
        ResolvedTypeSyntax type,
        SymbolTable module,
        System.Collections.Generic.List<AsnEncodingInstruction> final)
    {
        ParsedTypeSyntax? source = type.SourceSyntax;
        if (source != null
            && source.Type.Kind == AsnTypeKind.Defined
            && type.AliasTarget != null)
        {
            SymbolTable inheritedModule = this.FindDeclaringTable(type.AliasTarget) ?? module;
            this.ResolveTypeInstructions(type.AliasTarget, inheritedModule);
            this.AppendInheritedInstructions(type.AliasTarget, final);
        }
        else if (source != null
            && source.Type.Kind == AsnTypeKind.Selection
            && type.SelectedComponent != null
            && type.SelectedComponent.TypeResolution != null)
        {
            this.ResolveComponentInstructions(type.SelectedComponent, module);
            this.AppendInstructionArray(
                type.SelectedComponent.FinalPerEncodingInstructions,
                final);
        }
    }

    internal void ApplyTargetedInstructions(
        ResolvedTypeSyntax type,
        System.Collections.Generic.List<AsnEncodingInstruction> final)
    {
        int index = 0;
        while (index < type.PerTargetedEncodingInstructions.Length)
        {
            this.ApplyInstruction(final, type.PerTargetedEncodingInstructions[index]);
            index = index + 1;
        }
    }

    internal void AppendInheritedInstructions(
        ResolvedTypeSyntax source,
        System.Collections.Generic.List<AsnEncodingInstruction> destination)
    {
        this.AppendInstructionArray(source.FinalPerEncodingInstructions, destination);
    }

    internal void AppendInstructionArray(
        AsnEncodingInstruction[] source,
        System.Collections.Generic.List<AsnEncodingInstruction> destination)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal void ApplyInstruction(
        System.Collections.Generic.List<AsnEncodingInstruction> final,
        AsnEncodingInstruction instruction)
    {
        if (instruction.Negated)
        {
            final.Clear();
            return;
        }

        int index = final.Count - 1;
        while (index >= 0)
        {
            if (final[index].Name == instruction.Name)
            {
                final.RemoveAt(index);
            }

            index = index - 1;
        }

        final.Add(this.CloneInstruction(instruction));
    }

    internal AsnEncodingInstruction CloneInstruction(AsnEncodingInstruction source)
    {
        string[] targets = new string[source.Targets.Length];
        string[] arguments = new string[source.Arguments.Length];
        int index = 0;
        while (index < targets.Length)
        {
            targets[index] = source.Targets[index];
            index = index + 1;
        }

        index = 0;
        while (index < arguments.Length)
        {
            arguments[index] = source.Arguments[index];
            index = index + 1;
        }

        return new AsnEncodingInstruction
        {
            Name = source.Name,
            Kind = AsnInstructionKind.PerApplication,
            Negated = false,
            Arguments = arguments,
            Targets = targets,
            Span = source.Span
        };
    }

    internal void ResolveChildInstructions(ResolvedTypeSyntax type, SymbolTable module)
    {
        ParsedTypeSyntax? source = type.SourceSyntax;
        if (source != null
            && source.Type.Kind != AsnTypeKind.Defined
            && type.InnerType != null)
        {
            this.ResolveTypeInstructions(type.InnerType, module);
        }

        int index = 0;
        while (index < type.Components.Length)
        {
            this.ResolveComponentInstructions(type.Components[index], module);

            index = index + 1;
        }
    }

    internal void ResolveConstraintInstructions(
        ResolvedTypeSyntax type,
        SymbolTable module)
    {
        System.Collections.Generic.List<ResolvedTypeSyntax> retained =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        int index = 0;
        while (index < type.TypeConstraints.Length)
        {
            ResolvedTypeSyntax? constrained = type.TypeConstraints[index].ConstrainedType;
            if (constrained != null)
            {
                retained.Add(constrained);
                this.ResolveTypeInstructions(constrained, module);
            }

            index = index + 1;
        }

        index = 0;
        while (index < type.ContentsConstraints.Length)
        {
            ResolvedTypeSyntax? contained = type.ContentsConstraints[index].ContainedType;
            if (contained != null)
            {
                retained.Add(contained);
                this.ResolveTypeInstructions(contained, module);
            }

            index = index + 1;
        }

        index = 0;
        while (index < type.ConstraintTypes.Length)
        {
            ResolvedTypeSyntax constraintType = type.ConstraintTypes[index];
            if (!retained.Contains(constraintType))
            {
                this.DiscardTypeOccurrenceInstructions(constraintType);
            }

            index = index + 1;
        }

        this.DiscardContainedSubtypeInstructions(type.EffectiveConstraint, retained);
    }

    internal void ResetContainedSubtypeInstructions(
        NormalizedConstraintGraph? graph,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (graph == null)
        {
            return;
        }

        this.ResetContainedSubtypeNodeInstructions(
            graph.RootPartition == null ? null : graph.RootPartition.Expression,
            visited);
        this.ResetContainedSubtypeNodeInstructions(
            graph.ExtensionPartition == null
                ? null
                : graph.ExtensionPartition.Expression,
            visited);
    }

    internal void ResetContainedSubtypeNodeInstructions(
        NormalizedConstraintNode? node,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (node == null)
        {
            return;
        }

        if (node.ResolvedContainedSubtype != null
            && !this.IsDirectDefinedConstraintReference(node))
        {
            this.ResetTypeTree(node.ResolvedContainedSubtype, visited);
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            this.ResetContainedSubtypeNodeInstructions(node.Children[index], visited);
            index = index + 1;
        }

        index = 0;
        while (index < node.NestedConstraints.Length)
        {
            this.ResetContainedSubtypeInstructions(node.NestedConstraints[index], visited);
            index = index + 1;
        }
    }

    internal void DiscardContainedSubtypeInstructions(
        NormalizedConstraintGraph? graph,
        System.Collections.Generic.List<ResolvedTypeSyntax> retained)
    {
        if (graph == null)
        {
            return;
        }

        this.DiscardContainedSubtypeNodeInstructions(
            graph.RootPartition == null ? null : graph.RootPartition.Expression,
            retained);
        this.DiscardContainedSubtypeNodeInstructions(
            graph.ExtensionPartition == null
                ? null
                : graph.ExtensionPartition.Expression,
            retained);
    }

    internal void DiscardContainedSubtypeNodeInstructions(
        NormalizedConstraintNode? node,
        System.Collections.Generic.List<ResolvedTypeSyntax> retained)
    {
        if (node == null)
        {
            return;
        }

        ResolvedTypeSyntax? contained = node.ResolvedContainedSubtype;
        if (contained != null
            && !retained.Contains(contained)
            && !this.IsDirectDefinedConstraintReference(node))
        {
            this.DiscardTypeOccurrenceInstructions(contained);
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            this.DiscardContainedSubtypeNodeInstructions(node.Children[index], retained);
            index = index + 1;
        }

        index = 0;
        while (index < node.NestedConstraints.Length)
        {
            this.DiscardContainedSubtypeInstructions(node.NestedConstraints[index], retained);
            index = index + 1;
        }
    }

    internal bool IsDirectDefinedConstraintReference(NormalizedConstraintNode node)
    {
        ParsedTypeSyntax? syntax = node.SourceSyntax == null
            ? null
            : node.SourceSyntax.TypeSyntax;
        return syntax != null && syntax.Type.Kind == AsnTypeKind.Defined;
    }

    internal void DiscardTypeOccurrenceInstructions(ResolvedTypeSyntax type)
    {
        type.PerTargetedEncodingInstructions = [];
        type.FinalPerEncodingInstructions = [];
        type.PerInstructionPlanSteps = [];
        type.PerInstructionResolutionState = 2;
        if (type.InnerType != null
            && (type.SourceSyntax == null
                || type.SourceSyntax.Type.Kind != AsnTypeKind.Defined))
        {
            this.DiscardTypeOccurrenceInstructions(type.InnerType);
        }

        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedComponentSyntax component = type.Components[index];
            component.PerTargetedEncodingInstructions = [];
            component.FinalPerEncodingInstructions = [];
            component.PerInstructionPlanSteps = [];
            component.PerInstructionResolutionState = 2;
            if (component.TypeResolution != null)
            {
                this.DiscardTypeOccurrenceInstructions(component.TypeResolution);
            }

            index = index + 1;
        }
    }

    internal void ResolveComponentInstructions(
        ResolvedComponentSyntax component,
        SymbolTable module)
    {
        if (component.PerInstructionResolutionState == 2)
        {
            return;
        }

        if (component.PerInstructionResolutionState == 1)
        {
            return;
        }

        component.PerInstructionResolutionState = 1;
        ResolvedTypeSyntax? type = component.TypeResolution;
        if (type == null)
        {
            component.PerInstructionResolutionState = 2;
            return;
        }

        this.ResolveTypeInstructions(type, module);
        if (!this.IsComponentsOfComponent(component))
        {
            component.FinalPerEncodingInstructions = type.FinalPerEncodingInstructions;
            component.PerInstructionResolutionState = 2;
            return;
        }

        System.Collections.Generic.List<AsnEncodingInstruction> final =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        this.AppendInheritedInstructions(type, final);
        int index = 0;
        while (index < component.PerTargetedEncodingInstructions.Length)
        {
            this.ApplyInstruction(final, component.PerTargetedEncodingInstructions[index]);
            index = index + 1;
        }

        component.FinalPerEncodingInstructions = final.ToArray();
        component.PerInstructionResolutionState = 2;
    }

    internal AsnTypeKind GetBuiltInKind(string name)
    {
        switch (name)
        {
            case "BOOLEAN": return AsnTypeKind.Boolean;
            case "INTEGER": return AsnTypeKind.Integer;
            case "ENUMERATED": return AsnTypeKind.Enumerated;
            case "REAL": return AsnTypeKind.Real;
            case "BIT STRING": return AsnTypeKind.BitString;
            case "OCTET STRING": return AsnTypeKind.OctetString;
            case "NULL": return AsnTypeKind.Null;
            case "OBJECT IDENTIFIER": return AsnTypeKind.ObjectIdentifier;
            case "RELATIVE-OID": return AsnTypeKind.RelativeObjectIdentifier;
            case "OID-IRI": return AsnTypeKind.ObjectIdentifierIri;
            case "RELATIVE-OID-IRI": return AsnTypeKind.RelativeObjectIdentifierIri;
            case "CHOICE": return AsnTypeKind.Choice;
            case "SEQUENCE": return AsnTypeKind.Sequence;
            case "SET": return AsnTypeKind.Set;
            case "SEQUENCE OF": return AsnTypeKind.SequenceOf;
            case "SET OF": return AsnTypeKind.SetOf;
            case "INSTANCE OF": return AsnTypeKind.InstanceOf;
            case "EXTERNAL": return AsnTypeKind.External;
            case "EMBEDDED PDV": return AsnTypeKind.EmbeddedPdv;
            case "CHARACTER STRING": return AsnTypeKind.CharacterString;
            case "ObjectDescriptor": return AsnTypeKind.ObjectDescriptor;
            case "UTF8String": return AsnTypeKind.Utf8String;
            case "NumericString": return AsnTypeKind.NumericString;
            case "PrintableString": return AsnTypeKind.PrintableString;
            case "TeletexString": return AsnTypeKind.TeletexString;
            case "T61String": return AsnTypeKind.TeletexString;
            case "VideotexString": return AsnTypeKind.VideotexString;
            case "IA5String": return AsnTypeKind.Ia5String;
            case "GraphicString": return AsnTypeKind.GraphicString;
            case "VisibleString": return AsnTypeKind.VisibleString;
            case "ISO646String": return AsnTypeKind.VisibleString;
            case "GeneralString": return AsnTypeKind.GeneralString;
            case "UniversalString": return AsnTypeKind.UniversalString;
            case "BMPString": return AsnTypeKind.BmpString;
            case "UTCTime": return AsnTypeKind.UtcTime;
            case "GeneralizedTime": return AsnTypeKind.GeneralizedTime;
            case "TIME": return AsnTypeKind.Time;
            case "DATE": return AsnTypeKind.Date;
            case "TIME-OF-DAY": return AsnTypeKind.TimeOfDay;
            case "DATE-TIME": return AsnTypeKind.DateTime;
            case "DURATION": return AsnTypeKind.Duration;
        }

        return AsnTypeKind.Unknown;
    }

    internal SymbolTable? FindDeclaringTable(ResolvedTypeSyntax type)
    {
        SymbolTableEntry? declaration = type.SourceDeclaration;
        if (declaration == null)
        {
            return null;
        }

        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            SymbolTable table = this.Tables[tableIndex];
            int symbolIndex = 0;
            while (symbolIndex < table.Symbols.Length)
            {
                if (table.Symbols[symbolIndex] == declaration)
                {
                    return table;
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return null;
    }
}
