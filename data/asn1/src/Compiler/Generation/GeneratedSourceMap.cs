namespace asn1;

internal class GeneratedSourceMap
{
    internal System.Collections.Generic.List<AsnSourceSpan> Spans =
        new System.Collections.Generic.List<AsnSourceSpan>();
    internal System.Collections.Generic.List<LoweredTypeIr> Types =
        new System.Collections.Generic.List<LoweredTypeIr>();
    internal System.Collections.Generic.List<LoweredValueIr> Values =
        new System.Collections.Generic.List<LoweredValueIr>();
    internal System.Collections.Generic.List<LoweredConstraintIr> Constraints =
        new System.Collections.Generic.List<LoweredConstraintIr>();
    internal System.Collections.Generic.List<LoweredObjectTableIr> Tables =
        new System.Collections.Generic.List<LoweredObjectTableIr>();
    internal System.Collections.Generic.List<LoweredObjectIr> Objects =
        new System.Collections.Generic.List<LoweredObjectIr>();

    internal AsnSourceSpan[] CollectPartitionSpans(
        CSharpFilePartition partition)
    {
        this.ResetCollections();
        if (partition.Kind == CSharpFilePartitionKind.Model)
        {
            if (partition.Type != null)
            {
                this.AddAssignmentSpans(partition.Type.Assignment);
            }
        }
        else if (partition.Kind == CSharpFilePartitionKind.Constants)
        {
            this.AddConstantSpans(partition.Module.Module);
        }
        else if (partition.Kind == CSharpFilePartitionKind.Descriptors)
        {
            this.AddModuleSpans(partition.Module.Module);
        }
        else
        {
            this.AddSpan(partition.Module.Module.SourceSpan);
            this.AddDerivationSpans(partition.Module.Module.Derivations);
        }

        return this.Spans.ToArray();
    }

    internal void ResetCollections()
    {
        this.Spans.Clear();
        this.Types.Clear();
        this.Values.Clear();
        this.Constraints.Clear();
        this.Tables.Clear();
        this.Objects.Clear();
    }

    internal void AddConstantSpans(LoweredModuleIr module)
    {
        this.AddSpan(module.SourceSpan);
        int index = 0;
        while (index < module.Assignments.Length)
        {
            LoweredAssignmentIr assignment = module.Assignments[index];
            if ((assignment.Kind == AsnAssignmentKind.Value
                    && assignment.Value != null)
                || (assignment.Kind == AsnAssignmentKind.Type
                    && assignment.Type != null
                    && assignment.Type.NamedValues.Length > 0))
            {
                this.AddAssignmentSpans(assignment);
            }

            index = index + 1;
        }
    }

    internal void AddModuleSpans(LoweredModuleIr module)
    {
        this.AddSpan(module.SourceSpan);
        this.AddDerivationSpans(module.Derivations);
        int importIndex = 0;
        while (importIndex < module.Imports.Length)
        {
            this.AddSpan(module.Imports[importIndex].SourceSpan);
            this.AddDerivationSpans(
                module.Imports[importIndex].Derivations);
            importIndex = importIndex + 1;
        }

        int assignmentIndex = 0;
        while (assignmentIndex < module.Assignments.Length)
        {
            this.AddAssignmentSpans(module.Assignments[assignmentIndex]);
            assignmentIndex = assignmentIndex + 1;
        }

        this.AddInstructionSpans(module.Instructions);
    }

    internal void AddAssignmentSpans(LoweredAssignmentIr assignment)
    {
        this.AddSpan(assignment.SourceSpan);
        this.AddDerivationSpans(assignment.Derivations);
        int parameterIndex = 0;
        while (parameterIndex < assignment.Parameters.Length)
        {
            this.AddSpan(assignment.Parameters[parameterIndex].SourceSpan);
            this.AddDerivationSpans(
                assignment.Parameters[parameterIndex].Derivations);
            parameterIndex = parameterIndex + 1;
        }

        if (assignment.Type != null)
        {
            this.AddTypeSpans(assignment.Type);
        }

        if (assignment.Value != null)
        {
            this.AddValueSpans(assignment.Value);
        }

        if (assignment.ObjectClass != null)
        {
            this.AddObjectClassSpans(assignment.ObjectClass);
        }

        if (assignment.Object != null)
        {
            this.AddObjectSpans(assignment.Object);
        }

        if (assignment.ObjectSet != null)
        {
            this.AddObjectSetSpans(assignment.ObjectSet);
        }
    }

    internal void AddTypeSpans(LoweredTypeIr type)
    {
        if (this.Types.Contains(type))
        {
            return;
        }

        this.Types.Add(type);
        this.AddSpan(type.SourceSpan);
        this.AddDerivationSpans(type.Derivations);
        int tagIndex = 0;
        while (tagIndex < type.Tags.Length)
        {
            this.AddSpan(type.Tags[tagIndex].SourceSpan);
            this.AddDerivationSpans(type.Tags[tagIndex].Derivations);
            tagIndex = tagIndex + 1;
        }

        int fieldIndex = 0;
        while (fieldIndex < type.Fields.Length)
        {
            this.AddFieldSpans(type.Fields[fieldIndex]);
            fieldIndex = fieldIndex + 1;
        }

        if (type.ElementType != null)
        {
            this.AddTypeSpans(type.ElementType);
        }

        int namedIndex = 0;
        while (namedIndex < type.NamedValues.Length)
        {
            this.AddSpan(type.NamedValues[namedIndex].SourceSpan);
            this.AddDerivationSpans(type.NamedValues[namedIndex].Derivations);
            if (type.NamedValues[namedIndex].Value != null)
            {
                this.AddValueSpans(
                    type.NamedValues[namedIndex].Value
                        ?? throw new System.InvalidOperationException());
            }

            namedIndex = namedIndex + 1;
        }

        int constraintIndex = 0;
        while (constraintIndex < type.Constraints.Length)
        {
            this.AddConstraintSpans(type.Constraints[constraintIndex]);
            constraintIndex = constraintIndex + 1;
        }

        this.AddSpan(type.Extensions.SourceSpan);
        this.AddDerivationSpans(type.Extensions.Derivations);
        int tableIndex = 0;
        while (tableIndex < type.ObjectTables.Length)
        {
            this.AddTableSpans(type.ObjectTables[tableIndex]);
            tableIndex = tableIndex + 1;
        }

        this.AddInstructionSpans(type.Instructions);
    }

    internal void AddFieldSpans(LoweredFieldIr field)
    {
        this.AddSpan(field.SourceSpan);
        this.AddDerivationSpans(field.Derivations);
        int tagIndex = 0;
        while (tagIndex < field.Tags.Length)
        {
            this.AddSpan(field.Tags[tagIndex].SourceSpan);
            this.AddDerivationSpans(field.Tags[tagIndex].Derivations);
            tagIndex = tagIndex + 1;
        }

        this.AddInstructionSpans(field.Instructions);

        if (field.Type != null)
        {
            this.AddTypeSpans(field.Type);
        }

        if (field.DefaultValue != null)
        {
            this.AddValueSpans(field.DefaultValue);
        }
    }

    internal void AddValueSpans(LoweredValueIr value)
    {
        if (this.Values.Contains(value))
        {
            return;
        }

        this.Values.Add(value);
        this.AddSpan(value.SourceSpan);
        this.AddDerivationSpans(value.Derivations);
        int itemIndex = 0;
        while (itemIndex < value.Items.Length)
        {
            this.AddValueSpans(value.Items[itemIndex]);
            itemIndex = itemIndex + 1;
        }

        int namedIndex = 0;
        while (namedIndex < value.NamedValues.Length)
        {
            this.AddSpan(value.NamedValues[namedIndex].SourceSpan);
            this.AddDerivationSpans(value.NamedValues[namedIndex].Derivations);
            if (value.NamedValues[namedIndex].Value != null)
            {
                this.AddValueSpans(
                    value.NamedValues[namedIndex].Value
                        ?? throw new System.InvalidOperationException());
            }

            namedIndex = namedIndex + 1;
        }
    }

    internal void AddConstraintSpans(LoweredConstraintIr constraint)
    {
        if (this.Constraints.Contains(constraint))
        {
            return;
        }

        this.Constraints.Add(constraint);
        this.AddSpan(constraint.SourceSpan);
        this.AddDerivationSpans(constraint.Derivations);
        if (constraint.Root != null)
        {
            this.AddConstraintNodeSpans(constraint.Root);
        }

        if (constraint.Extension != null)
        {
            this.AddConstraintNodeSpans(constraint.Extension);
        }

        int applicationIndex = 0;
        while (applicationIndex < constraint.Applications.Length)
        {
            this.AddConstraintSpans(
                constraint.Applications[applicationIndex]);
            applicationIndex = applicationIndex + 1;
        }
    }

    internal void AddConstraintNodeSpans(LoweredConstraintNodeIr node)
    {
        this.AddSpan(node.SourceSpan);
        this.AddDerivationSpans(node.Derivations);
        if (node.Value != null)
        {
            this.AddValueSpans(node.Value);
        }

        if (node.LowerValue != null)
        {
            this.AddValueSpans(node.LowerValue);
        }

        if (node.UpperValue != null)
        {
            this.AddValueSpans(node.UpperValue);
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            this.AddConstraintNodeSpans(node.Children[childIndex]);
            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            this.AddConstraintSpans(node.NestedConstraints[nestedIndex]);
            nestedIndex = nestedIndex + 1;
        }
    }

    internal void AddTableSpans(LoweredObjectTableIr table)
    {
        if (this.Tables.Contains(table))
        {
            return;
        }

        this.Tables.Add(table);
        this.AddSpan(table.SourceSpan);
        this.AddDerivationSpans(table.Derivations);
        int index = 0;
        while (index < table.Entries.Length)
        {
            LoweredObjectTableEntryIr entry = table.Entries[index];
            this.AddSpan(entry.SourceSpan);
            this.AddDerivationSpans(entry.Derivations);
            if (entry.Key != null)
            {
                this.AddValueSpans(entry.Key);
            }

            if (entry.Type != null)
            {
                this.AddTypeSpans(entry.Type);
            }

            index = index + 1;
        }
    }

    internal void AddObjectClassSpans(LoweredObjectClassIr objectClass)
    {
        this.AddSpan(objectClass.SourceSpan);
        this.AddDerivationSpans(objectClass.Derivations);
        int index = 0;
        while (index < objectClass.Fields.Length)
        {
            LoweredObjectClassFieldIr field = objectClass.Fields[index];
            this.AddSpan(field.SourceSpan);
            this.AddDerivationSpans(field.Derivations);
            if (field.Type != null)
            {
                this.AddTypeSpans(field.Type);
            }

            if (field.DefaultSetting != null)
            {
                this.AddObjectFieldSettingSpans(field.DefaultSetting);
            }

            index = index + 1;
        }
    }

    internal void AddObjectSpans(LoweredObjectIr value)
    {
        if (this.Objects.Contains(value))
        {
            return;
        }

        this.Objects.Add(value);
        this.AddSpan(value.SourceSpan);
        this.AddDerivationSpans(value.Derivations);
        int index = 0;
        while (index < value.FieldSettings.Length)
        {
            this.AddObjectFieldSettingSpans(value.FieldSettings[index]);
            index = index + 1;
        }
    }

    internal void AddObjectFieldSettingSpans(
        LoweredObjectFieldSettingIr setting)
    {
        this.AddSpan(setting.SourceSpan);
        this.AddDerivationSpans(setting.Derivations);
        if (setting.Type != null)
        {
            this.AddTypeSpans(setting.Type);
        }

        if (setting.ValueSetType != null)
        {
            this.AddTypeSpans(setting.ValueSetType);
        }

        if (setting.Value != null)
        {
            this.AddValueSpans(setting.Value);
        }
    }

    internal void AddObjectSetSpans(LoweredObjectSetIr set)
    {
        this.AddSpan(set.SourceSpan);
        this.AddDerivationSpans(set.Derivations);
        int index = 0;
        while (index < set.RootObjects.Length)
        {
            this.AddObjectSpans(set.RootObjects[index]);
            index = index + 1;
        }

        index = 0;
        while (index < set.AdditionalObjects.Length)
        {
            this.AddObjectSpans(set.AdditionalObjects[index]);
            index = index + 1;
        }

        if (set.Table != null)
        {
            this.AddTableSpans(set.Table);
        }
    }

    internal void AddInstructionSpans(LoweredInstructionIr[] instructions)
    {
        int index = 0;
        while (index < instructions.Length)
        {
            this.AddSpan(instructions[index].SourceSpan);
            this.AddDerivationSpans(instructions[index].Derivations);
            index = index + 1;
        }
    }

    internal void AddDerivationSpans(LoweredDerivationIr[] derivations)
    {
        int index = 0;
        while (index < derivations.Length)
        {
            this.AddSpan(derivations[index].Span);
            index = index + 1;
        }
    }

    internal void AddSpan(AsnSourceSpan? span)
    {
        if (span == null || this.ContainsSpan(span))
        {
            return;
        }

        this.Spans.Add(this.CloneSpan(span));
    }

    internal bool ContainsSpan(AsnSourceSpan span)
    {
        int index = 0;
        while (index < this.Spans.Count)
        {
            AsnSourceSpan candidate = this.Spans[index];
            if (candidate.Start.SourceName == span.Start.SourceName
                && candidate.Start.Offset == span.Start.Offset
                && candidate.Start.Line == span.Start.Line
                && candidate.Start.Column == span.Start.Column
                && candidate.End.Offset == span.End.Offset
                && candidate.End.Line == span.End.Line
                && candidate.End.Column == span.End.Column)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal AsnSourceSpan CloneSpan(AsnSourceSpan span)
    {
        return new AsnSourceSpan
        {
            Start = new AsnSourcePosition
            {
                SourceName = span.Start.SourceName,
                Offset = span.Start.Offset,
                Line = span.Start.Line,
                Column = span.Start.Column
            },
            End = new AsnSourcePosition
            {
                SourceName = span.End.SourceName,
                Offset = span.End.Offset,
                Line = span.End.Line,
                Column = span.End.Column
            }
        };
    }
}
