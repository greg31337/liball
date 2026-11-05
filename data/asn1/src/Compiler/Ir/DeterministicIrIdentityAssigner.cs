namespace asn1;

internal class DeterministicIrIdentityAssigner
{
    internal System.Collections.Generic.List<LoweredTypeIr> AssignedTypes =
        new System.Collections.Generic.List<LoweredTypeIr>();

    internal void AssignCompilationIdentities(LoweredCompilationIr compilation)
    {
        compilation.StableId = "ir:compilation";
        int moduleIndex = 0;
        while (moduleIndex < compilation.Modules.Length)
        {
            this.AssignModuleIdentity(compilation.Modules[moduleIndex], moduleIndex);
            moduleIndex = moduleIndex + 1;
        }
    }

    internal void AssignModuleIdentity(LoweredModuleIr module, int emissionOrder)
    {
        module.StableId = "ir:module/" + this.EscapeStablePart(module.Name)
            + ";oid=" + this.EscapeStablePart(module.ObjectIdentifier)
            + ";iri=" + this.EscapeStablePart(module.Iri)
            + ";version-module=" + this.EscapeStablePart(module.VersionModule)
            + ";version=" + this.EscapeStablePart(module.Version);
        module.EmissionOrder = emissionOrder;
        this.AssignDerivationIdentities(module.Derivations, module.StableId);
        int importIndex = 0;
        while (importIndex < module.Imports.Length)
        {
            module.Imports[importIndex].StableId = module.StableId
                + "/import/" + this.EscapeStablePart(
                    module.Imports[importIndex].ModuleName)
                + "/" + importIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            this.AssignDerivationIdentities(
                module.Imports[importIndex].Derivations,
                module.Imports[importIndex].StableId);
            importIndex = importIndex + 1;
        }

        int assignmentIndex = 0;
        while (assignmentIndex < module.Assignments.Length)
        {
            this.AssignAssignmentIdentity(
                module.Assignments[assignmentIndex],
                module.StableId,
                assignmentIndex);
            assignmentIndex = assignmentIndex + 1;
        }

        int instructionIndex = 0;
        while (instructionIndex < module.Instructions.Length)
        {
            this.AssignInstructionIdentity(
                module.Instructions[instructionIndex],
                module.StableId + "/instruction/"
                    + instructionIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
            instructionIndex = instructionIndex + 1;
        }
    }

    internal void AssignAssignmentIdentity(
        LoweredAssignmentIr assignment,
        string moduleId,
        int emissionOrder)
    {
        assignment.StableId = moduleId + "/assignment/"
            + ((int)assignment.Kind).ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + "/" + this.EscapeStablePart(assignment.Name);
        assignment.EmissionOrder = emissionOrder;
        this.AssignDerivationIdentities(
            assignment.Derivations,
            assignment.StableId);
        int parameterIndex = 0;
        while (parameterIndex < assignment.Parameters.Length)
        {
            assignment.Parameters[parameterIndex].StableId = assignment.StableId
                + "/parameter/" + this.EscapeStablePart(
                    assignment.Parameters[parameterIndex].Name)
                + "/" + parameterIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            this.AssignDerivationIdentities(
                assignment.Parameters[parameterIndex].Derivations,
                assignment.Parameters[parameterIndex].StableId);
            parameterIndex = parameterIndex + 1;
        }

        if (assignment.Type != null)
        {
            this.AssignTypeIdentity(assignment.Type, assignment.StableId + "/type");
        }

        if (assignment.Value != null)
        {
            this.AssignValueIdentity(assignment.Value, assignment.StableId + "/value");
        }

        if (assignment.ObjectClass != null)
        {
            this.AssignObjectClassIdentity(
                assignment.ObjectClass,
                assignment.StableId + "/object-class");
        }

        if (assignment.Object != null)
        {
            this.AssignObjectIdentity(assignment.Object, assignment.StableId + "/object");
        }

        if (assignment.ObjectSet != null)
        {
            this.AssignObjectSetIdentity(
                assignment.ObjectSet,
                assignment.StableId + "/object-set");
        }
    }

    internal void AssignTypeIdentity(LoweredTypeIr type, string stableId)
    {
        if (this.AssignedTypes.Contains(type))
        {
            return;
        }

        this.AssignedTypes.Add(type);
        type.StableId = stableId;
        this.AssignDerivationIdentities(type.Derivations, stableId);
        int tagIndex = 0;
        while (tagIndex < type.Tags.Length)
        {
            type.Tags[tagIndex].StableId = stableId + "/tag/"
                + tagIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            this.AssignDerivationIdentities(
                type.Tags[tagIndex].Derivations,
                type.Tags[tagIndex].StableId);
            tagIndex = tagIndex + 1;
        }

        int fieldIndex = 0;
        while (fieldIndex < type.Fields.Length)
        {
            this.AssignFieldIdentity(
                type.Fields[fieldIndex],
                stableId + "/field/" + this.EscapeStablePart(
                    type.Fields[fieldIndex].Name)
                    + "/" + fieldIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
            fieldIndex = fieldIndex + 1;
        }

        if (type.ElementType != null)
        {
            this.AssignTypeIdentity(type.ElementType, stableId + "/element");
        }

        int namedIndex = 0;
        while (namedIndex < type.NamedValues.Length)
        {
            this.AssignNamedValueIdentity(
                type.NamedValues[namedIndex],
                stableId + "/named/" + this.EscapeStablePart(
                    type.NamedValues[namedIndex].Name)
                    + "/" + namedIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
            namedIndex = namedIndex + 1;
        }

        int constraintIndex = 0;
        while (constraintIndex < type.Constraints.Length)
        {
            this.AssignConstraintIdentity(
                type.Constraints[constraintIndex],
                stableId + "/constraint/" + constraintIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            constraintIndex = constraintIndex + 1;
        }

        this.AssignExtensionIdentity(type.Extensions, stableId + "/extensions");
        int tableIndex = 0;
        while (tableIndex < type.ObjectTables.Length)
        {
            this.AssignObjectTableIdentity(
                type.ObjectTables[tableIndex],
                stableId + "/object-table/" + tableIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            tableIndex = tableIndex + 1;
        }

        int instructionIndex = 0;
        while (instructionIndex < type.Instructions.Length)
        {
            this.AssignInstructionIdentity(
                type.Instructions[instructionIndex],
                stableId + "/instruction/" + instructionIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            instructionIndex = instructionIndex + 1;
        }
    }

    internal void AssignFieldIdentity(LoweredFieldIr field, string stableId)
    {
        field.StableId = stableId;
        this.AssignDerivationIdentities(field.Derivations, stableId);
        int tagIndex = 0;
        while (tagIndex < field.Tags.Length)
        {
            field.Tags[tagIndex].StableId = stableId + "/tag/"
                + tagIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            this.AssignDerivationIdentities(
                field.Tags[tagIndex].Derivations,
                field.Tags[tagIndex].StableId);
            tagIndex = tagIndex + 1;
        }

        if (field.Type != null)
        {
            this.AssignTypeIdentity(field.Type, stableId + "/type");
        }

        if (field.DefaultValue != null)
        {
            this.AssignValueIdentity(field.DefaultValue, stableId + "/default");
        }
    }

    internal void AssignValueIdentity(LoweredValueIr value, string stableId)
    {
        value.StableId = stableId;
        this.AssignDerivationIdentities(value.Derivations, stableId);
        int itemIndex = 0;
        while (itemIndex < value.Items.Length)
        {
            this.AssignValueIdentity(
                value.Items[itemIndex],
                stableId + "/item/" + itemIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            itemIndex = itemIndex + 1;
        }

        int namedIndex = 0;
        while (namedIndex < value.NamedValues.Length)
        {
            this.AssignNamedValueIdentity(
                value.NamedValues[namedIndex],
                stableId + "/named/" + this.EscapeStablePart(
                    value.NamedValues[namedIndex].Name)
                    + "/" + namedIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
            namedIndex = namedIndex + 1;
        }
    }

    internal void AssignNamedValueIdentity(
        LoweredNamedValueIr named,
        string stableId)
    {
        named.StableId = stableId;
        this.AssignDerivationIdentities(named.Derivations, stableId);
        if (named.Value != null)
        {
            this.AssignValueIdentity(named.Value, stableId + "/value");
        }
    }

    internal void AssignConstraintIdentity(
        LoweredConstraintIr constraint,
        string stableId)
    {
        constraint.StableId = stableId;
        this.AssignDerivationIdentities(constraint.Derivations, stableId);
        if (constraint.Root != null)
        {
            this.AssignConstraintNodeIdentity(constraint.Root, stableId + "/root");
        }

        if (constraint.Extension != null)
        {
            this.AssignConstraintNodeIdentity(
                constraint.Extension,
                stableId + "/extension");
        }

        int applicationIndex = 0;
        while (applicationIndex < constraint.Applications.Length)
        {
            this.AssignConstraintIdentity(
                constraint.Applications[applicationIndex],
                stableId + "/application/" + applicationIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            applicationIndex = applicationIndex + 1;
        }

        int intervalIndex = 0;
        while (intervalIndex < constraint.RootIntervals.Length)
        {
            constraint.RootIntervals[intervalIndex].StableId = stableId
                + "/root-interval/" + intervalIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            this.AssignDerivationIdentities(
                constraint.RootIntervals[intervalIndex].Derivations,
                constraint.RootIntervals[intervalIndex].StableId);
            intervalIndex = intervalIndex + 1;
        }

        intervalIndex = 0;
        while (intervalIndex < constraint.ExtensionIntervals.Length)
        {
            constraint.ExtensionIntervals[intervalIndex].StableId = stableId
                + "/extension-interval/" + intervalIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            this.AssignDerivationIdentities(
                constraint.ExtensionIntervals[intervalIndex].Derivations,
                constraint.ExtensionIntervals[intervalIndex].StableId);
            intervalIndex = intervalIndex + 1;
        }
    }

    internal void AssignConstraintNodeIdentity(
        LoweredConstraintNodeIr node,
        string stableId)
    {
        node.StableId = stableId;
        this.AssignDerivationIdentities(node.Derivations, stableId);
        if (node.Value != null)
        {
            this.AssignValueIdentity(node.Value, stableId + "/value");
        }

        if (node.LowerValue != null)
        {
            this.AssignValueIdentity(node.LowerValue, stableId + "/lower");
        }

        if (node.UpperValue != null)
        {
            this.AssignValueIdentity(node.UpperValue, stableId + "/upper");
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            this.AssignConstraintNodeIdentity(
                node.Children[childIndex],
                stableId + "/child/" + childIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            this.AssignConstraintIdentity(
                node.NestedConstraints[nestedIndex],
                stableId + "/nested/" + nestedIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            nestedIndex = nestedIndex + 1;
        }
    }

    internal void AssignExtensionIdentity(
        LoweredExtensionPartitionIr extension,
        string stableId)
    {
        extension.StableId = stableId;
        this.AssignDerivationIdentities(extension.Derivations, stableId);
        int namedIndex = 0;
        while (namedIndex < extension.RootNamedValues.Length)
        {
            this.AssignNamedValueIdentity(
                extension.RootNamedValues[namedIndex],
                stableId + "/root-named/" + this.EscapeStablePart(
                    extension.RootNamedValues[namedIndex].Name)
                    + "/" + namedIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
            namedIndex = namedIndex + 1;
        }

        namedIndex = 0;
        while (namedIndex < extension.AdditionalNamedValues.Length)
        {
            this.AssignNamedValueIdentity(
                extension.AdditionalNamedValues[namedIndex],
                stableId + "/additional-named/" + this.EscapeStablePart(
                    extension.AdditionalNamedValues[namedIndex].Name)
                    + "/" + namedIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
            namedIndex = namedIndex + 1;
        }
    }

    internal void AssignObjectClassIdentity(
        LoweredObjectClassIr objectClass,
        string stableId)
    {
        objectClass.StableId = stableId;
        this.AssignDerivationIdentities(objectClass.Derivations, stableId);
        int fieldIndex = 0;
        while (fieldIndex < objectClass.Fields.Length)
        {
            LoweredObjectClassFieldIr field = objectClass.Fields[fieldIndex];
            field.StableId = stableId + "/field/"
                + this.EscapeStablePart(field.Name) + "/"
                + fieldIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            this.AssignDerivationIdentities(field.Derivations, field.StableId);
            if (field.Type != null)
            {
                this.AssignTypeIdentity(field.Type, field.StableId + "/type");
            }

            if (field.DefaultSetting != null)
            {
                this.AssignObjectSettingIdentity(
                    field.DefaultSetting,
                    field.StableId + "/default");
            }

            fieldIndex = fieldIndex + 1;
        }
    }

    internal void AssignObjectIdentity(LoweredObjectIr value, string fallbackId)
    {
        value.StableId = value.StableId.Length == 0
            ? fallbackId
            : "ir:object/" + this.EscapeStablePart(value.StableId);
        this.AssignDerivationIdentities(value.Derivations, value.StableId);
        int settingIndex = 0;
        while (settingIndex < value.FieldSettings.Length)
        {
            this.AssignObjectSettingIdentity(
                value.FieldSettings[settingIndex],
                value.StableId + "/setting/" + this.EscapeStablePart(
                    value.FieldSettings[settingIndex].FieldName)
                    + "/" + settingIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
            settingIndex = settingIndex + 1;
        }
    }

    internal void AssignObjectSettingIdentity(
        LoweredObjectFieldSettingIr setting,
        string stableId)
    {
        setting.StableId = stableId;
        this.AssignDerivationIdentities(setting.Derivations, stableId);
        if (setting.Type != null)
        {
            this.AssignTypeIdentity(setting.Type, stableId + "/type");
        }

        if (setting.ValueSetType != null)
        {
            this.AssignTypeIdentity(setting.ValueSetType, stableId + "/value-set");
        }

        if (setting.Value != null)
        {
            this.AssignValueIdentity(setting.Value, stableId + "/value");
        }

        if (setting.ObjectStableId.Length > 0)
        {
            setting.ObjectStableId = "ir:object/"
                + this.EscapeStablePart(setting.ObjectStableId);
        }

        if (setting.ObjectSetStableId.Length > 0)
        {
            setting.ObjectSetStableId = "ir:object-set/"
                + this.EscapeStablePart(setting.ObjectSetStableId);
        }
    }

    internal void AssignObjectSetIdentity(
        LoweredObjectSetIr set,
        string fallbackId)
    {
        set.StableId = set.StableId.Length == 0
            ? fallbackId
            : "ir:object-set/" + this.EscapeStablePart(set.StableId);
        this.AssignDerivationIdentities(set.Derivations, set.StableId);
        int rootIndex = 0;
        while (rootIndex < set.RootObjects.Length)
        {
            this.AssignObjectIdentity(
                set.RootObjects[rootIndex],
                set.StableId + "/root/" + rootIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            rootIndex = rootIndex + 1;
        }

        int additionIndex = 0;
        while (additionIndex < set.AdditionalObjects.Length)
        {
            this.AssignObjectIdentity(
                set.AdditionalObjects[additionIndex],
                set.StableId + "/addition/" + additionIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            additionIndex = additionIndex + 1;
        }

        if (set.Table != null)
        {
            this.AssignObjectTableIdentity(set.Table, set.StableId + "/table");
        }
    }

    internal void AssignObjectTableIdentity(
        LoweredObjectTableIr table,
        string stableId)
    {
        table.StableId = stableId;
        this.AssignDerivationIdentities(table.Derivations, stableId);
        int entryIndex = 0;
        while (entryIndex < table.Entries.Length)
        {
            LoweredObjectTableEntryIr entry = table.Entries[entryIndex];
            entry.StableId = stableId + "/entry/" + entryIndex.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            this.AssignDerivationIdentities(entry.Derivations, entry.StableId);
            if (entry.Key != null)
            {
                this.AssignValueIdentity(entry.Key, entry.StableId + "/key");
            }

            if (entry.Type != null)
            {
                this.AssignTypeIdentity(entry.Type, entry.StableId + "/type");
            }

            entryIndex = entryIndex + 1;
        }
    }

    internal void AssignInstructionIdentity(
        LoweredInstructionIr instruction,
        string stableId)
    {
        instruction.StableId = stableId;
        this.AssignDerivationIdentities(instruction.Derivations, stableId);
    }

    internal void AssignDerivationIdentities(
        LoweredDerivationIr[] derivations,
        string parentId)
    {
        int index = 0;
        while (index < derivations.Length)
        {
            derivations[index].StableId = parentId + "/derivation/"
                + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            index = index + 1;
        }
    }

    internal string EscapeStablePart(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool unescaped = current >= 'A' && current <= 'Z'
                || current >= 'a' && current <= 'z'
                || current >= '0' && current <= '9'
                || current == '-'
                || current == '_'
                || current == '.';
            if (unescaped)
            {
                result.Append(current);
            }
            else
            {
                result.Append('%');
                result.Append(((int)current).ToString(
                    "X4",
                    System.Globalization.CultureInfo.InvariantCulture));
            }

            index = index + 1;
        }

        return result.ToString();
    }
}
