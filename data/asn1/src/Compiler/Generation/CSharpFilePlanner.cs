namespace asn1;

internal class CSharpFilePlanner
{
    internal CSharpNameMapper NameMapper = new CSharpNameMapper();

    internal CSharpGenerationPlan PlanCompilation(
        LoweredCompilationIr compilation,
        string rootNamespace)
    {
        if (compilation == null)
        {
            throw new System.ArgumentNullException("compilation");
        }

        if (rootNamespace == null)
        {
            throw new System.ArgumentNullException("rootNamespace");
        }

        if (rootNamespace.Length == 0)
        {
            throw new System.ArgumentException(
                "C# generation requires a root namespace.",
                "rootNamespace");
        }

        CSharpNameScope namespaceScope = new CSharpNameScope(false);
        CSharpNameMapping mappedRoot = this.NameMapper.MapNamespaceName(
            rootNamespace,
            compilation.StableId + "/generated-root-namespace",
            namespaceScope);
        LoweredModuleIr[] orderedModules = this.OrderModules(
            compilation.Modules);
        CSharpGenerationPlan plan = new CSharpGenerationPlan
        {
            RootNamespace = mappedRoot,
            Modules = new CSharpModulePlan[orderedModules.Length]
        };
        int moduleIndex = 0;
        while (moduleIndex < orderedModules.Length)
        {
            LoweredModuleIr module = this.CreateOrderedModuleView(
                orderedModules[moduleIndex]);
            CSharpNameMapping namespaceName = this.NameMapper.MapNamespaceName(
                mappedRoot.GeneratedName + "." + module.Name,
                module.StableId + "/generated-namespace",
                namespaceScope);
            plan.Modules[moduleIndex] = new CSharpModulePlan
            {
                Module = module,
                NamespaceName = namespaceName
            };
            moduleIndex = moduleIndex + 1;
        }

        this.PlanTypes(plan);
        this.PlanImports(plan);
        this.PlanSupportTypeNames(plan);
        this.PlanObjectTables(plan);
        this.PlanFilePartitions(plan);
        return plan;
    }

    internal LoweredModuleIr CreateOrderedModuleView(LoweredModuleIr source)
    {
        return new LoweredModuleIr
        {
            StableId = source.StableId,
            SourceSpan = source.SourceSpan,
            Derivations = source.Derivations,
            EmissionOrder = source.EmissionOrder,
            Name = source.Name,
            ObjectIdentifier = source.ObjectIdentifier,
            Iri = source.Iri,
            VersionModule = source.VersionModule,
            Version = source.Version,
            TagDefault = source.TagDefault,
            ExtensibilityImplied = source.ExtensibilityImplied,
            Imports = this.OrderImports(source.Imports),
            Assignments = this.OrderAssignments(source.Assignments),
            Instructions = source.Instructions,
            PerInstructionRegistrations = source.PerInstructionRegistrations
        };
    }

    internal LoweredModuleIr[] OrderModules(LoweredModuleIr[] source)
    {
        LoweredModuleIr[] result = new LoweredModuleIr[source.Length];
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            result[sourceIndex] = source[sourceIndex];
            sourceIndex = sourceIndex + 1;
        }

        int index = 1;
        while (index < result.Length)
        {
            LoweredModuleIr value = result[index];
            int target = index;
            while (target > 0
                && this.CompareModules(value, result[target - 1]) < 0)
            {
                result[target] = result[target - 1];
                target = target - 1;
            }

            result[target] = value;
            index = index + 1;
        }

        return result;
    }

    internal int CompareModules(LoweredModuleIr left, LoweredModuleIr right)
    {
        int comparison = left.EmissionOrder.CompareTo(right.EmissionOrder);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = System.String.CompareOrdinal(left.StableId, right.StableId);
        return comparison != 0
            ? comparison
            : System.String.CompareOrdinal(left.Name, right.Name);
    }

    internal LoweredAssignmentIr[] OrderAssignments(
        LoweredAssignmentIr[] source)
    {
        LoweredAssignmentIr[] result = new LoweredAssignmentIr[source.Length];
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            result[sourceIndex] = source[sourceIndex];
            sourceIndex = sourceIndex + 1;
        }

        int index = 1;
        while (index < result.Length)
        {
            LoweredAssignmentIr value = result[index];
            int target = index;
            while (target > 0
                && this.CompareAssignments(value, result[target - 1]) < 0)
            {
                result[target] = result[target - 1];
                target = target - 1;
            }

            result[target] = value;
            index = index + 1;
        }

        return result;
    }

    internal int CompareAssignments(
        LoweredAssignmentIr left,
        LoweredAssignmentIr right)
    {
        int comparison = left.EmissionOrder.CompareTo(right.EmissionOrder);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = System.String.CompareOrdinal(left.StableId, right.StableId);
        return comparison != 0
            ? comparison
            : System.String.CompareOrdinal(left.QualifiedName, right.QualifiedName);
    }

    internal void PlanTypes(CSharpGenerationPlan plan)
    {
        System.Collections.Generic.List<CSharpTypePlan> types =
            new System.Collections.Generic.List<CSharpTypePlan>();
        int moduleIndex = 0;
        while (moduleIndex < plan.Modules.Length)
        {
            CSharpModulePlan module = plan.Modules[moduleIndex];
            LoweredAssignmentIr[] assignments = this.OrderAssignments(
                module.Module.Assignments);
            int assignmentIndex = 0;
            while (assignmentIndex < assignments.Length)
            {
                LoweredAssignmentIr assignment = assignments[assignmentIndex];
                if (assignment.Kind == AsnAssignmentKind.Type
                    && assignment.Type != null
                    && this.FindTypeInList(types, assignment.Type) == null)
                {
                    CSharpNameMapping typeName = this.NameMapper.MapTypeName(
                        assignment.Name,
                        assignment.StableId,
                        module.TypeScope);
                    CSharpNameMapping? choiceName = null;
                    if (assignment.Type.Kind == AsnTypeKind.Choice)
                    {
                        choiceName = this.NameMapper.MapTypeName(
                            assignment.Name + "-selection",
                            assignment.StableId + "/choice-selection",
                            module.TypeScope);
                    }

                    types.Add(new CSharpTypePlan
                    {
                        Type = assignment.Type,
                        Assignment = assignment,
                        Module = module,
                        TypeName = typeName,
                        ChoiceName = choiceName
                    });
                }

                assignmentIndex = assignmentIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        plan.Types = types.ToArray();
    }

    internal CSharpTypePlan? FindTypeInList(
        System.Collections.Generic.List<CSharpTypePlan> types,
        LoweredTypeIr type)
    {
        int index = 0;
        while (index < types.Count)
        {
            if (types[index].Type == type
                || (type.StableId.Length > 0
                    && types[index].Type.StableId == type.StableId))
            {
                return types[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void PlanImports(CSharpGenerationPlan plan)
    {
        int moduleIndex = 0;
        while (moduleIndex < plan.Modules.Length)
        {
            CSharpModulePlan module = plan.Modules[moduleIndex];
            LoweredImportIr[] imports = this.OrderImports(module.Module.Imports);
            CSharpImportPlan[] planned = new CSharpImportPlan[imports.Length];
            System.Collections.Generic.List<string> namespaces =
                new System.Collections.Generic.List<string>();
            int importIndex = 0;
            while (importIndex < imports.Length)
            {
                CSharpModulePlan? target = this.FindModuleByName(
                    plan,
                    imports[importIndex].ModuleName);
                if (target == null)
                {
                    throw new System.InvalidOperationException(
                        "A generated import references unknown module '"
                            + imports[importIndex].ModuleName + "'.");
                }

                planned[importIndex] = new CSharpImportPlan
                {
                    Import = imports[importIndex],
                    TargetModule = target,
                    NamespaceName = target.NamespaceName.GeneratedName
                };
                if (target != module
                    && !namespaces.Contains(target.NamespaceName.GeneratedName))
                {
                    namespaces.Add(target.NamespaceName.GeneratedName);
                }

                importIndex = importIndex + 1;
            }

            module.Imports = planned;
            module.ImportedNamespaces = this.OrderStrings(namespaces.ToArray());
            moduleIndex = moduleIndex + 1;
        }
    }

    internal LoweredImportIr[] OrderImports(LoweredImportIr[] source)
    {
        LoweredImportIr[] result = new LoweredImportIr[source.Length];
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            result[sourceIndex] = source[sourceIndex];
            sourceIndex = sourceIndex + 1;
        }

        int index = 1;
        while (index < result.Length)
        {
            LoweredImportIr value = result[index];
            int target = index;
            while (target > 0
                && this.CompareImports(value, result[target - 1]) < 0)
            {
                result[target] = result[target - 1];
                target = target - 1;
            }

            result[target] = value;
            index = index + 1;
        }

        return result;
    }

    internal int CompareImports(LoweredImportIr left, LoweredImportIr right)
    {
        int comparison = System.String.CompareOrdinal(
            left.ModuleName,
            right.ModuleName);
        return comparison != 0
            ? comparison
            : System.String.CompareOrdinal(left.StableId, right.StableId);
    }

    internal CSharpModulePlan? FindModuleByName(
        CSharpGenerationPlan plan,
        string moduleName)
    {
        int index = 0;
        while (index < plan.Modules.Length)
        {
            if (plan.Modules[index].Module.Name == moduleName)
            {
                return plan.Modules[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal string[] OrderStrings(string[] source)
    {
        string[] result = new string[source.Length];
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            result[sourceIndex] = source[sourceIndex];
            sourceIndex = sourceIndex + 1;
        }

        int index = 1;
        while (index < result.Length)
        {
            string value = result[index];
            int target = index;
            while (target > 0
                && System.String.CompareOrdinal(
                    value,
                    result[target - 1]) < 0)
            {
                result[target] = result[target - 1];
                target = target - 1;
            }

            result[target] = value;
            index = index + 1;
        }

        return result;
    }

    internal void PlanSupportTypeNames(CSharpGenerationPlan plan)
    {
        int moduleIndex = 0;
        while (moduleIndex < plan.Modules.Length)
        {
            CSharpModulePlan module = plan.Modules[moduleIndex];
            System.Collections.Generic.List<CSharpNameMapping> constantNames =
                new System.Collections.Generic.List<CSharpNameMapping>();
            bool hasModuleValues = false;
            int assignmentIndex = 0;
            while (assignmentIndex < module.Module.Assignments.Length)
            {
                LoweredAssignmentIr assignment =
                    module.Module.Assignments[assignmentIndex];
                if (assignment.Kind == AsnAssignmentKind.Value
                    && assignment.Type != null
                    && assignment.Value != null)
                {
                    hasModuleValues = true;
                }

                assignmentIndex = assignmentIndex + 1;
            }

            if (hasModuleValues)
            {
                constantNames.Add(this.NameMapper.MapTypeName(
                    module.Module.Name + "-values",
                    module.Module.StableId + "/generated-values",
                    module.TypeScope));
            }

            LoweredAssignmentIr[] assignments = this.OrderAssignments(
                module.Module.Assignments);
            assignmentIndex = 0;
            while (assignmentIndex < assignments.Length)
            {
                LoweredAssignmentIr assignment = assignments[assignmentIndex];
                if (assignment.Kind == AsnAssignmentKind.Type
                    && assignment.Type != null
                    && assignment.Type.NamedValues.Length > 0)
                {
                    constantNames.Add(this.NameMapper.MapTypeName(
                        assignment.Name + "-named-values",
                        assignment.StableId + "/generated-named-values",
                        module.TypeScope));
                }

                assignmentIndex = assignmentIndex + 1;
            }

            module.ConstantTypeNames = constantNames.ToArray();
            module.SchemaName = this.NameMapper.MapTypeName(
                module.Module.Name + "-schema",
                module.Module.StableId + "/generated-schema",
                module.TypeScope);
            module.ValidatorName = this.NameMapper.MapTypeName(
                module.Module.Name + "-validator",
                module.Module.StableId + "/generated-validator",
                module.TypeScope);
            module.CodecName = this.NameMapper.MapTypeName(
                module.Module.Name + "-codec",
                module.Module.StableId + "/generated-codec",
                module.TypeScope);
            moduleIndex = moduleIndex + 1;
        }
    }

    internal void PlanObjectTables(CSharpGenerationPlan plan)
    {
        System.Collections.Generic.List<LoweredObjectTableIr> tables =
            new System.Collections.Generic.List<LoweredObjectTableIr>();
        System.Collections.Generic.List<LoweredTypeIr> visited =
            new System.Collections.Generic.List<LoweredTypeIr>();
        int moduleIndex = 0;
        while (moduleIndex < plan.Modules.Length)
        {
            LoweredAssignmentIr[] assignments =
                plan.Modules[moduleIndex].Module.Assignments;
            int assignmentIndex = 0;
            while (assignmentIndex < assignments.Length)
            {
                if (assignments[assignmentIndex].Type != null)
                {
                    this.CollectTypeTables(
                        assignments[assignmentIndex].Type
                            ?? throw new System.InvalidOperationException(),
                        tables,
                        visited);
                }

                if (assignments[assignmentIndex].ObjectSet != null
                    && assignments[assignmentIndex].ObjectSet?.Table != null)
                {
                    this.AddTable(
                        assignments[assignmentIndex].ObjectSet?.Table
                            ?? throw new System.InvalidOperationException(),
                        tables);
                }

                assignmentIndex = assignmentIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        CSharpObjectTablePlan[] planned =
            new CSharpObjectTablePlan[tables.Count];
        int tableIndex = 0;
        while (tableIndex < tables.Count)
        {
            LoweredObjectTableIr table = tables[tableIndex];
            CSharpObjectTableEntryPlan[] entries =
                new CSharpObjectTableEntryPlan[table.Entries.Length];
            int entryIndex = 0;
            while (entryIndex < entries.Length)
            {
                entries[entryIndex] = new CSharpObjectTableEntryPlan
                {
                    Entry = table.Entries[entryIndex],
                    Type = table.Entries[entryIndex].Type == null
                        ? null
                        : plan.FindTypePlan(
                            table.Entries[entryIndex].Type
                                ?? throw new System.InvalidOperationException())
                };
                entryIndex = entryIndex + 1;
            }

            planned[tableIndex] = new CSharpObjectTablePlan
            {
                Table = table,
                Entries = entries
            };
            tableIndex = tableIndex + 1;
        }

        plan.ObjectTables = planned;
    }

    internal void CollectTypeTables(
        LoweredTypeIr type,
        System.Collections.Generic.List<LoweredObjectTableIr> tables,
        System.Collections.Generic.List<LoweredTypeIr> visited)
    {
        if (visited.Contains(type))
        {
            return;
        }

        visited.Add(type);
        int tableIndex = 0;
        while (tableIndex < type.ObjectTables.Length)
        {
            this.AddTable(type.ObjectTables[tableIndex], tables);
            tableIndex = tableIndex + 1;
        }

        if (type.ElementType != null)
        {
            this.CollectTypeTables(type.ElementType, tables, visited);
        }

        int fieldIndex = 0;
        while (fieldIndex < type.Fields.Length)
        {
            if (type.Fields[fieldIndex].Type != null)
            {
                this.CollectTypeTables(
                    type.Fields[fieldIndex].Type
                        ?? throw new System.InvalidOperationException(),
                    tables,
                    visited);
            }

            fieldIndex = fieldIndex + 1;
        }
    }

    internal void AddTable(
        LoweredObjectTableIr table,
        System.Collections.Generic.List<LoweredObjectTableIr> tables)
    {
        if (!tables.Contains(table))
        {
            tables.Add(table);
        }
    }

    internal void PlanFilePartitions(CSharpGenerationPlan plan)
    {
        System.Collections.Generic.List<CSharpFilePartition> all =
            new System.Collections.Generic.List<CSharpFilePartition>();
        int moduleIndex = 0;
        while (moduleIndex < plan.Modules.Length)
        {
            CSharpModulePlan module = plan.Modules[moduleIndex];
            System.Collections.Generic.List<CSharpFilePartition> moduleFiles =
                new System.Collections.Generic.List<CSharpFilePartition>();
            int typeIndex = 0;
            while (typeIndex < plan.Types.Length)
            {
                CSharpTypePlan type = plan.Types[typeIndex];
                if (type.Module == module)
                {
                    CSharpFilePartition partition = this.CreatePartition(
                        module,
                        type.TypeName,
                        CSharpFilePartitionKind.Model,
                        type.Assignment.StableId + "/generated-model-file");
                    partition.Type = type;
                    type.Partition = partition;
                    moduleFiles.Add(partition);
                    all.Add(partition);
                }

                typeIndex = typeIndex + 1;
            }

            if (module.ConstantTypeNames.Length > 0)
            {
                CSharpFilePartition constants = this.CreatePartition(
                    module,
                    module.ConstantTypeNames[0],
                    CSharpFilePartitionKind.Constants,
                    module.Module.StableId + "/generated-constants-file");
                moduleFiles.Add(constants);
                all.Add(constants);
            }

            CSharpFilePartition descriptors = this.CreatePartition(
                module,
                module.SchemaName,
                CSharpFilePartitionKind.Descriptors,
                module.Module.StableId + "/generated-descriptors-file");
            CSharpFilePartition validator = this.CreatePartition(
                module,
                module.ValidatorName,
                CSharpFilePartitionKind.Validator,
                module.Module.StableId + "/generated-validator-file");
            CSharpFilePartition codec = this.CreatePartition(
                module,
                module.CodecName,
                CSharpFilePartitionKind.Codec,
                module.Module.StableId + "/generated-codec-file");
            moduleFiles.Add(descriptors);
            moduleFiles.Add(validator);
            moduleFiles.Add(codec);
            all.Add(descriptors);
            all.Add(validator);
            all.Add(codec);
            module.Partitions = moduleFiles.ToArray();
            moduleIndex = moduleIndex + 1;
        }

        plan.Partitions = all.ToArray();
    }

    internal CSharpFilePartition CreatePartition(
        CSharpModulePlan module,
        CSharpNameMapping declarationName,
        CSharpFilePartitionKind kind,
        string stableId)
    {
        CSharpNameMapping fileName = this.NameMapper.MapFileName(
            declarationName,
            module.FileScope);
        return new CSharpFilePartition
        {
            StableId = stableId,
            RelativePath = module.NamespaceName.GeneratedName.Replace('.', '/')
                + "/" + fileName.GeneratedName,
            Kind = kind,
            Module = module,
            FileName = fileName
        };
    }
}
