namespace asn1;

internal class ReflectionServiceImplementation : IAsnReflectionService
{
    public AsnReflectionCatalog BuildCatalog(AsnValidationResult validation)
    {
        if (validation == null)
        {
            throw new System.ArgumentNullException("validation");
        }

        LoweredCompilationIr ir = new ValidationReflectionIrAdapter()
            .LowerValidation(validation);
        return new ReflectionCatalogBuilder().BuildCatalog(ir);
    }

    public AsnModuleDescriptor? ResolveModule(
        AsnReflectionCatalog catalog,
        string moduleName)
    {
        if (catalog == null)
        {
            throw new System.ArgumentNullException("catalog");
        }

        if (moduleName == null)
        {
            throw new System.ArgumentNullException("moduleName");
        }

        AsnModuleDescriptor? result = null;
        int index = 0;
        while (index < catalog.Modules.Length)
        {
            AsnModuleDescriptor candidate = catalog.Modules[index];
            if (this.ModuleMatches(candidate, moduleName))
            {
                if (result != null && result != candidate)
                {
                    return null;
                }

                result = candidate;
            }

            index = index + 1;
        }

        return result;
    }

    public AsnTypeDescriptor? ResolveType(
        AsnReflectionCatalog catalog,
        string moduleName,
        string typeName)
    {
        if (catalog == null)
        {
            throw new System.ArgumentNullException("catalog");
        }

        if (moduleName == null)
        {
            throw new System.ArgumentNullException("moduleName");
        }

        if (typeName == null)
        {
            throw new System.ArgumentNullException("typeName");
        }

        ReflectionQualifiedName qualified = this.ParseQualifiedName(
            moduleName,
            typeName);
        AsnModuleDescriptor? module = this.ResolveModule(
            catalog,
            qualified.ModuleName);
        if (module == null)
        {
            return null;
        }

        AsnTypeDescriptor? result = null;
        int index = 0;
        while (index < module.Types.Length)
        {
            AsnTypeDescriptor candidate = module.Types[index];
            if (this.TypeMatches(candidate, qualified.LocalName))
            {
                if (result != null && result != candidate)
                {
                    return null;
                }

                result = candidate;
            }

            index = index + 1;
        }

        index = 0;
        while (index < module.Assignments.Length)
        {
            AsnAssignmentDescriptor assignment = module.Assignments[index];
            if (assignment.Type != null
                && (assignment.QualifiedName == typeName
                    || assignment.Name == qualified.LocalName
                    || assignment.GeneratedName == qualified.LocalName
                    || assignment.StableId == qualified.LocalName))
            {
                if (result != null && result != assignment.Type)
                {
                    return null;
                }

                result = assignment.Type;
            }

            index = index + 1;
        }

        return result;
    }

    public AsnValueDescriptor? ResolveValue(
        AsnReflectionCatalog catalog,
        string moduleName,
        string valueName)
    {
        if (catalog == null)
        {
            throw new System.ArgumentNullException("catalog");
        }

        if (moduleName == null)
        {
            throw new System.ArgumentNullException("moduleName");
        }

        if (valueName == null)
        {
            throw new System.ArgumentNullException("valueName");
        }

        ReflectionQualifiedName qualified = this.ParseQualifiedName(
            moduleName,
            valueName);
        AsnModuleDescriptor? module = this.ResolveModule(
            catalog,
            qualified.ModuleName);
        if (module == null)
        {
            return null;
        }

        AsnValueDescriptor? result = null;
        int index = 0;
        while (index < module.Values.Length)
        {
            AsnValueDescriptor candidate = module.Values[index];
            if (this.ValueMatches(candidate, qualified.LocalName))
            {
                if (result != null && result != candidate)
                {
                    return null;
                }

                result = candidate;
            }

            index = index + 1;
        }

        index = 0;
        while (index < module.Assignments.Length)
        {
            AsnAssignmentDescriptor assignment = module.Assignments[index];
            if (assignment.Value != null
                && (assignment.QualifiedName == valueName
                    || assignment.Name == qualified.LocalName
                    || assignment.GeneratedName == qualified.LocalName
                    || assignment.StableId == qualified.LocalName))
            {
                if (result != null && result != assignment.Value)
                {
                    return null;
                }

                result = assignment.Value;
            }

            index = index + 1;
        }

        return result;
    }

    public AsnTypeDescriptor[] GetTypes(AsnReflectionCatalog catalog)
    {
        if (catalog == null)
        {
            throw new System.ArgumentNullException("catalog");
        }

        System.Collections.Generic.List<AsnTypeDescriptor> result =
            new System.Collections.Generic.List<AsnTypeDescriptor>();
        int moduleIndex = 0;
        while (moduleIndex < catalog.Modules.Length)
        {
            AsnModuleDescriptor module = catalog.Modules[moduleIndex];
            int typeIndex = 0;
            while (typeIndex < module.Types.Length)
            {
                this.AddTypeTraversal(module.Types[typeIndex], result);
                typeIndex = typeIndex + 1;
            }

            int assignmentIndex = 0;
            while (assignmentIndex < module.Assignments.Length)
            {
                AsnAssignmentDescriptor assignment =
                    module.Assignments[assignmentIndex];
                if (assignment.Type != null)
                {
                    this.AddTypeTraversal(assignment.Type, result);
                }

                int tableIndex = 0;
                while (tableIndex < assignment.ObjectTables.Length)
                {
                    this.AddObjectTableTraversal(
                        assignment.ObjectTables[tableIndex],
                        result);
                    tableIndex = tableIndex + 1;
                }

                assignmentIndex = assignmentIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        return result.ToArray();
    }

    internal bool ModuleMatches(
        AsnModuleDescriptor module,
        string moduleName)
    {
        return module.Name == moduleName
            || module.GeneratedName == moduleName
            || module.StableId == moduleName
            || module.ObjectIdentifier.Length > 0
                && module.Name + "{" + module.ObjectIdentifier + "}" == moduleName
            || module.Iri.Length > 0
                && module.Name + "\"" + module.Iri + "\"" == moduleName;
    }

    internal bool TypeMatches(AsnTypeDescriptor type, string typeName)
    {
        return type.Name == typeName
            || type.GeneratedName == typeName
            || type.StableId == typeName;
    }

    internal bool ValueMatches(AsnValueDescriptor value, string valueName)
    {
        return value.Name == valueName
            || value.GeneratedName == valueName
            || value.StableId == valueName;
    }

    internal ReflectionQualifiedName ParseQualifiedName(
        string moduleName,
        string localName)
    {
        int separator = localName.LastIndexOf('.');
        if (separator <= 0 || separator == localName.Length - 1)
        {
            return new ReflectionQualifiedName
            {
                ModuleName = moduleName,
                LocalName = localName
            };
        }

        string qualifier = localName.Substring(0, separator);
        string parsedLocal = localName.Substring(separator + 1);
        if (moduleName.Length > 0 && moduleName != qualifier)
        {
            return new ReflectionQualifiedName
            {
                ModuleName = "",
                LocalName = ""
            };
        }

        return new ReflectionQualifiedName
        {
            ModuleName = qualifier,
            LocalName = parsedLocal
        };
    }

    internal void AddTypeTraversal(
        AsnTypeDescriptor type,
        System.Collections.Generic.List<AsnTypeDescriptor> result)
    {
        if (result.Contains(type))
        {
            return;
        }

        result.Add(type);
        int fieldIndex = 0;
        while (fieldIndex < type.Fields.Length)
        {
            if (type.Fields[fieldIndex].Type != null)
            {
                this.AddTypeTraversal(
                    type.Fields[fieldIndex].Type
                        ?? throw new System.InvalidOperationException(),
                    result);
            }

            fieldIndex = fieldIndex + 1;
        }

        if (type.ElementType != null)
        {
            this.AddTypeTraversal(type.ElementType, result);
        }

        int tableIndex = 0;
        while (tableIndex < type.ObjectTables.Length)
        {
            this.AddObjectTableTraversal(type.ObjectTables[tableIndex], result);
            tableIndex = tableIndex + 1;
        }
    }

    internal void AddObjectTableTraversal(
        AsnObjectTableDescriptor table,
        System.Collections.Generic.List<AsnTypeDescriptor> result)
    {
        int index = 0;
        while (index < table.Entries.Length)
        {
            if (table.Entries[index].Type != null)
            {
                this.AddTypeTraversal(
                    table.Entries[index].Type
                        ?? throw new System.InvalidOperationException(),
                    result);
            }

            index = index + 1;
        }
    }
}
