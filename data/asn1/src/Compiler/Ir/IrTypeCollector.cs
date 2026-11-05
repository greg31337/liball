namespace asn1;

internal class IrTypeCollector
{
    internal System.Collections.Generic.List<LoweredTypeIr> Types =
        new System.Collections.Generic.List<LoweredTypeIr>();
    internal ResourceCounter Resources;

    internal IrTypeCollector(AsnResourceLimits limits)
    {
        this.Resources = new ResourceCounter(limits);
    }

    internal LoweredTypeIr[] CollectCompilationTypes(LoweredCompilationIr compilation)
    {
        int moduleIndex = 0;
        while (moduleIndex < compilation.Modules.Length)
        {
            LoweredModuleIr module = compilation.Modules[moduleIndex];
            int assignmentIndex = 0;
            while (assignmentIndex < module.Assignments.Length)
            {
                this.CollectAssignmentTypes(module.Assignments[assignmentIndex]);
                assignmentIndex = assignmentIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        return this.Types.ToArray();
    }

    internal void CollectAssignmentTypes(LoweredAssignmentIr assignment)
    {
        this.CollectTypeGraph(assignment.Type);
        if (assignment.ObjectClass != null)
        {
            int fieldIndex = 0;
            while (fieldIndex < assignment.ObjectClass.Fields.Length)
            {
                LoweredObjectClassFieldIr field =
                    assignment.ObjectClass.Fields[fieldIndex];
                this.CollectTypeGraph(field.Type);
                if (field.DefaultSetting != null)
                {
                    this.CollectSettingTypes(field.DefaultSetting);
                }

                fieldIndex = fieldIndex + 1;
            }
        }

        if (assignment.Object != null)
        {
            this.CollectObjectTypes(assignment.Object);
        }

        if (assignment.ObjectSet != null)
        {
            this.CollectObjectSetTypes(assignment.ObjectSet);
        }
    }

    internal void CollectTypeGraph(LoweredTypeIr? type)
    {
        if (type == null || this.Types.Contains(type))
        {
            return;
        }

        if (!this.Resources.TryEnterNesting())
        {
            this.ThrowResourceLimit("IR type nesting exceeds the configured depth limit.");
        }

        this.Types.Add(type);
        if (!this.Resources.AllowsCollectionElements(this.Types.Count))
        {
            this.ThrowResourceLimit("IR type count exceeds the configured element limit.");
        }

        int fieldIndex = 0;
        while (fieldIndex < type.Fields.Length)
        {
            this.CollectTypeGraph(type.Fields[fieldIndex].Type);
            fieldIndex = fieldIndex + 1;
        }

        this.CollectTypeGraph(type.ElementType);
        int tableIndex = 0;
        while (tableIndex < type.ObjectTables.Length)
        {
            this.CollectTableTypes(type.ObjectTables[tableIndex]);
            tableIndex = tableIndex + 1;
        }

        this.Resources.LeaveNesting();
    }

    internal void CollectObjectTypes(LoweredObjectIr value)
    {
        int index = 0;
        while (index < value.FieldSettings.Length)
        {
            this.CollectSettingTypes(value.FieldSettings[index]);
            index = index + 1;
        }
    }

    internal void CollectSettingTypes(LoweredObjectFieldSettingIr setting)
    {
        this.CollectTypeGraph(setting.Type);
        this.CollectTypeGraph(setting.ValueSetType);
    }

    internal void CollectObjectSetTypes(LoweredObjectSetIr set)
    {
        int index = 0;
        while (index < set.RootObjects.Length)
        {
            this.CollectObjectTypes(set.RootObjects[index]);
            index = index + 1;
        }

        index = 0;
        while (index < set.AdditionalObjects.Length)
        {
            this.CollectObjectTypes(set.AdditionalObjects[index]);
            index = index + 1;
        }

        if (set.Table != null)
        {
            this.CollectTableTypes(set.Table);
        }
    }

    internal void CollectTableTypes(LoweredObjectTableIr table)
    {
        int index = 0;
        while (index < table.Entries.Length)
        {
            this.CollectTypeGraph(table.Entries[index].Type);
            index = index + 1;
        }
    }

    internal int FindTypeIndex(LoweredTypeIr? type)
    {
        if (type == null)
        {
            return -1;
        }

        int index = 0;
        while (index < this.Types.Count)
        {
            if (this.Types[index] == type)
            {
                return index;
            }

            index = index + 1;
        }

        throw new System.InvalidOperationException(
            "The IR type graph changed during serialization.");
    }

    internal void ThrowResourceLimit(string message)
    {
        throw new AsnResourceLimitException(message, "ASNRES0001", []);
    }
}
