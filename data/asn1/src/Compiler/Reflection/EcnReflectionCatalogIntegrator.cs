namespace asn1;

internal class EcnReflectionCatalogIntegrator
{
    internal AsnReflectionCatalog BuildIntegratedCatalog(
        EcnSymbolTable table,
        AsnModule[] modules,
        AsnEncodingPlan[] plans)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }
        if (modules == null)
        {
            throw new System.ArgumentNullException("modules");
        }
        if (plans == null)
        {
            throw new System.ArgumentNullException("plans");
        }
        AsnValidationResult validation = new AsnValidationResult
        {
            Success = true,
            Modules = modules,
            Assignments = this.FlattenAssignments(modules)
        };
        LoweredCompilationIr lowered = new ValidationReflectionIrAdapter()
            .LowerValidation(validation);
        AsnReflectionCatalog catalog = new ReflectionCatalogBuilder()
            .BuildCatalog(lowered);
        AsnEncodingDescriptor[] descriptors = this.BuildDescriptors(
            table,
            plans);
        this.AttachTypeEncodings(catalog, table, descriptors);
        catalog.EcnCodecPlans = this.BuildCodecPlans(table, catalog);
        AsnModuleDescriptor ecnModule = new AsnModuleDescriptor
        {
            StableId = "ecn:module:" + table.ModuleName,
            Name = table.ModuleName,
            GeneratedName = table.ModuleName,
            Encodings = descriptors,
            SourceSpan = table.Module == null
                ? null
                : new EcnPublicPlanBuilder().CloneSpan(table.Module.Span),
            SourceModule = new AsnModule
            {
                Name = table.ModuleName,
                EncodingInstructions = this.CollectInstructions(plans),
                Span = table.Module == null
                    ? null
                    : new EcnPublicPlanBuilder().CloneSpan(table.Module.Span)
            }
        };
        catalog.Modules = this.AppendModule(catalog.Modules, ecnModule);
        catalog.StableId = catalog.StableId + "/ecn/" + table.ModuleName;
        return catalog;
    }

    internal AsnEncodingDescriptor[] BuildDescriptors(
        EcnSymbolTable table,
        AsnEncodingPlan[] plans)
    {
        AsnEncodingDescriptor[] result =
            new AsnEncodingDescriptor[plans.Length];
        int index = 0;
        while (index < plans.Length)
        {
            EcnExecutablePlan? internalPlan = this.FindInternalPlan(
                table,
                plans[index].StableId);
            result[index] = new AsnEncodingDescriptor
            {
                StableId = plans[index].StableId + "/reflection",
                EncodingRules = AsnEncodingRules.Ecn,
                EncodingReference = "ECN",
                Instructions = plans[index].Instructions,
                Plan = plans[index],
                SourceSpan = internalPlan == null
                    ? null
                    : new EcnPublicPlanBuilder().CloneSpan(internalPlan.Span)
            };
            index = index + 1;
        }
        return result;
    }

    internal EcnCodecPlanSelection[] BuildCodecPlans(
        EcnSymbolTable table,
        AsnReflectionCatalog catalog)
    {
        System.Collections.Generic.List<EcnCodecPlanSelection> result =
            new System.Collections.Generic.List<EcnCodecPlanSelection>();
        int index = 0;
        while (index < table.EncodingObjects.Length)
        {
            EcnSymbol symbol = table.EncodingObjects[index];
            EcnEncodingObjectDefinition? definition = symbol.ObjectDefinition;
            if (!symbol.Imported
                && definition != null
                && definition.Plan != null
                && definition.Plan.Valid)
            {
                int moduleIndex = 0;
                while (moduleIndex < catalog.Modules.Length)
                {
                    int typeIndex = 0;
                    while (typeIndex
                        < catalog.Modules[moduleIndex].Types.Length)
                    {
                        AsnTypeDescriptor type =
                            catalog.Modules[moduleIndex].Types[typeIndex];
                        if (string.Equals(
                            type.Name,
                            definition.Name,
                            System.StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(new EcnCodecPlanSelection
                            {
                                Name = definition.Name,
                                Plan = definition.Plan,
                                Category = symbol.ClassCategory,
                                Properties = definition.Properties,
                                Type = type
                            });
                        }
                        typeIndex = typeIndex + 1;
                    }
                    moduleIndex = moduleIndex + 1;
                }
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal EcnExecutablePlan? FindInternalPlan(
        EcnSymbolTable table,
        string stableId)
    {
        int index = 0;
        while (index < table.EncodingObjects.Length)
        {
            EcnEncodingObjectDefinition? definition =
                table.EncodingObjects[index].ObjectDefinition;
            if (definition != null && definition.Plan != null
                && definition.Plan.StableId == stableId)
            {
                return definition.Plan;
            }
            index = index + 1;
        }
        return null;
    }

    internal void AttachTypeEncodings(
        AsnReflectionCatalog catalog,
        EcnSymbolTable table,
        AsnEncodingDescriptor[] descriptors)
    {
        int descriptorIndex = 0;
        while (descriptorIndex < descriptors.Length)
        {
            string objectName = this.ObjectNameForPlan(
                table,
                descriptors[descriptorIndex].Plan == null
                    ? ""
                    : (descriptors[descriptorIndex].Plan
                        ?? new AsnEncodingPlan()).StableId);
            int moduleIndex = 0;
            while (moduleIndex < catalog.Modules.Length)
            {
                int typeIndex = 0;
                while (typeIndex < catalog.Modules[moduleIndex].Types.Length)
                {
                    AsnTypeDescriptor type =
                        catalog.Modules[moduleIndex].Types[typeIndex];
                    if (string.Equals(
                        type.Name,
                        objectName,
                        System.StringComparison.OrdinalIgnoreCase))
                    {
                        type.Encodings = this.AppendDescriptor(
                            type.Encodings,
                            descriptors[descriptorIndex]);
                    }
                    typeIndex = typeIndex + 1;
                }
                moduleIndex = moduleIndex + 1;
            }
            descriptorIndex = descriptorIndex + 1;
        }
    }

    internal string ObjectNameForPlan(
        EcnSymbolTable table,
        string stableId)
    {
        int index = 0;
        while (index < table.EncodingObjects.Length)
        {
            EcnEncodingObjectDefinition? definition =
                table.EncodingObjects[index].ObjectDefinition;
            if (definition != null && definition.Plan != null
                && definition.Plan.StableId == stableId)
            {
                return definition.Name;
            }
            index = index + 1;
        }
        return "";
    }

    internal AsnAssignment[] FlattenAssignments(AsnModule[] modules)
    {
        System.Collections.Generic.List<AsnAssignment> result =
            new System.Collections.Generic.List<AsnAssignment>();
        int moduleIndex = 0;
        while (moduleIndex < modules.Length)
        {
            int assignmentIndex = 0;
            while (assignmentIndex < modules[moduleIndex].Assignments.Length)
            {
                result.Add(modules[moduleIndex].Assignments[assignmentIndex]);
                assignmentIndex = assignmentIndex + 1;
            }
            moduleIndex = moduleIndex + 1;
        }
        return result.ToArray();
    }

    internal AsnEncodingInstruction[] CollectInstructions(
        AsnEncodingPlan[] plans)
    {
        System.Collections.Generic.List<AsnEncodingInstruction> result =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        int planIndex = 0;
        while (planIndex < plans.Length)
        {
            int instructionIndex = 0;
            while (instructionIndex < plans[planIndex].Instructions.Length)
            {
                result.Add(plans[planIndex].Instructions[instructionIndex]);
                instructionIndex = instructionIndex + 1;
            }
            planIndex = planIndex + 1;
        }
        return result.ToArray();
    }

    internal AsnModuleDescriptor[] AppendModule(
        AsnModuleDescriptor[] source,
        AsnModuleDescriptor value)
    {
        AsnModuleDescriptor[] result =
            new AsnModuleDescriptor[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        result[source.Length] = value;
        return result;
    }

    internal AsnEncodingDescriptor[] AppendDescriptor(
        AsnEncodingDescriptor[] source,
        AsnEncodingDescriptor value)
    {
        AsnEncodingDescriptor[] result =
            new AsnEncodingDescriptor[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        result[source.Length] = value;
        return result;
    }
}
