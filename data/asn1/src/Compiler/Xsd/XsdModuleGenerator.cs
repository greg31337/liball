namespace asn1;

internal class XsdModuleGenerator
{
    internal XsdSchemaGraphResult Graph = new XsdSchemaGraphResult();
    internal XsdSimpleTypeMappingResult SimpleTypes =
        new XsdSimpleTypeMappingResult();
    internal XsdDeclarationMappingResult Declarations =
        new XsdDeclarationMappingResult();
    internal XsdComplexTypeMappingResult ComplexTypes =
        new XsdComplexTypeMappingResult();
    internal XsdAdvancedMappingResult Advanced =
        new XsdAdvancedMappingResult();
    internal XsdV2SupportModule Support = new XsdV2SupportModule();
    internal AsnResourceLimits Limits = new AsnResourceLimits();
    internal XsdNameAllocator Names = new XsdNameAllocator();
    internal System.Collections.Generic.List<XsdGeneratedAssignment> Assignments =
        new System.Collections.Generic.List<XsdGeneratedAssignment>();
    internal System.Collections.Generic.List<AsnDiagnostic> Diagnostics =
        new System.Collections.Generic.List<AsnDiagnostic>();
    internal System.Collections.Generic.List<XsdSavedDeclarationState>
        SavedDeclarations =
            new System.Collections.Generic.List<XsdSavedDeclarationState>();
    internal System.Collections.Generic.List<XsdSavedSimpleTypeState>
        SavedSimpleTypes =
            new System.Collections.Generic.List<XsdSavedSimpleTypeState>();
    internal System.Collections.Generic.List<XsdSavedSimpleUseState>
        SavedSimpleUses =
            new System.Collections.Generic.List<XsdSavedSimpleUseState>();
    internal System.Collections.Generic.List<XsdSavedComplexTypeState>
        SavedComplexTypes =
            new System.Collections.Generic.List<XsdSavedComplexTypeState>();
    internal System.Collections.Generic.List<XsdSavedGroupState> SavedGroups =
        new System.Collections.Generic.List<XsdSavedGroupState>();
    internal System.Collections.Generic.List<XsdSavedParticleState>
        SavedParticles =
            new System.Collections.Generic.List<XsdSavedParticleState>();
    internal System.Collections.Generic.List<XsdSavedTypeSubstitutionState>
        SavedTypeSubstitutions =
            new System.Collections.Generic.List<XsdSavedTypeSubstitutionState>();
    internal System.Collections.Generic.List<XsdSavedSubstitutionGroupState>
        SavedSubstitutionGroups =
            new System.Collections.Generic.List<XsdSavedSubstitutionGroupState>();

    internal XsdModuleGenerationResult GenerateModules(
        XsdSchemaGraphResult graph,
        XsdSimpleTypeMappingResult simpleTypes,
        XsdDeclarationMappingResult declarations,
        XsdComplexTypeMappingResult complexTypes,
        XsdAdvancedMappingResult advanced,
        XsdV2SupportModule support,
        AsnXsdImportOptions options)
    {
        if (graph == null || simpleTypes == null || declarations == null
            || complexTypes == null || advanced == null || support == null
            || options == null)
        {
            throw new System.ArgumentNullException("module generation input");
        }
        this.Graph = graph;
        this.SimpleTypes = simpleTypes;
        this.Declarations = declarations;
        this.ComplexTypes = complexTypes;
        this.Advanced = advanced;
        this.Support = support;
        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        this.Names = new XsdNameAllocator();
        this.Names.ReserveSupportNames(support);
        this.Assignments =
            new System.Collections.Generic.List<XsdGeneratedAssignment>();
        this.Diagnostics = new System.Collections.Generic.List<AsnDiagnostic>();
        if (!advanced.Success)
        {
            return this.CreateResult([]);
        }
        string moduleName = this.CreateModuleName(options);
        if (moduleName.Length == 0)
        {
            return this.CreateResult([]);
        }
        this.CreateDirectAssignments();
        this.SortDirectAssignments();
        this.AllocateDirectNames();
        this.CreateSpecialAssignments();
        this.SortSpecialAssignments();
        this.AllocateSpecialNames();
        this.SaveMappedState();
        this.RewriteMappedNames();
        this.NormalizeComponentIdentifiers();
        this.RefreshMappedDefinitions();
        this.FillAssignmentDefinitions();
        XsdGeneratedModule module = this.CreateModule(moduleName);
        module.SourceUnit = this.RenderModule(module);
        this.RestoreMappedState();
        return this.CreateResult([module]);
    }

    internal string CreateModuleName(AsnXsdImportOptions options)
    {
        string source = options.HasModuleName
            ? options.ModuleName
            : this.DefaultModuleName();
        if (source.Length == 0)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidGeneratedModule,
                "The generated ASN.1 module name cannot be empty.",
                null);
            return "";
        }
        XsdNameAllocator allocator = new XsdNameAllocator();
        return allocator.AllocateTypeName(source);
    }

    internal string DefaultModuleName()
    {
        string namespaceUri = "";
        int index = 0;
        while (index < this.Graph.DocumentInstances.Length)
        {
            string candidate = this.Graph.DocumentInstances[index]
                .EffectiveTargetNamespace;
            if (candidate.Length > 0
                && (namespaceUri.Length == 0
                    || string.CompareOrdinal(candidate, namespaceUri) < 0))
            {
                namespaceUri = candidate;
            }
            index = index + 1;
        }
        return namespaceUri.Length == 0
            ? "XSD-Mapped"
            : "XSD-" + namespaceUri;
    }

    internal void CreateDirectAssignments()
    {
        int index = 0;
        while (index < this.Declarations.Elements.Length)
        {
            XsdDeclarationDefinition value = this.Declarations.Elements[index];
            if (value.Global && !value.Ignored)
            {
                this.Assignments.Add(new XsdGeneratedAssignment
                {
                    StableId = "x694:assignment:" + value.StableId,
                    OriginalName = value.Name,
                    NamespaceUri = value.NamespaceUri,
                    Kind = "element",
                    Category = 0,
                    Declaration = value,
                    Span = value.Span
                });
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.Declarations.Attributes.Length)
        {
            XsdDeclarationDefinition value =
                this.Declarations.Attributes[index];
            if (value.Global)
            {
                this.Assignments.Add(new XsdGeneratedAssignment
                {
                    StableId = "x694:assignment:" + value.StableId,
                    OriginalName = value.Name,
                    NamespaceUri = value.NamespaceUri,
                    Kind = "attribute",
                    Category = 1,
                    Declaration = value,
                    Span = value.Span
                });
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.SimpleTypes.Types.Length)
        {
            XsdSimpleTypeDefinition value = this.SimpleTypes.Types[index];
            if (!value.Anonymous && value.Name.Length > 0
                && value.Component != null)
            {
                this.Assignments.Add(new XsdGeneratedAssignment
                {
                    StableId = "x694:assignment:" + value.StableId,
                    OriginalName = value.Name,
                    NamespaceUri = value.NamespaceUri,
                    Kind = "simpleType",
                    Category = 2,
                    SimpleType = value,
                    Span = value.Span
                });
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.ComplexTypes.Types.Length)
        {
            XsdComplexTypeDefinition value = this.ComplexTypes.Types[index];
            if (!value.Anonymous && value.Name.Length > 0
                && value.Component != null)
            {
                this.Assignments.Add(new XsdGeneratedAssignment
                {
                    StableId = "x694:assignment:" + value.StableId,
                    OriginalName = value.Name,
                    NamespaceUri = value.NamespaceUri,
                    Kind = "complexType",
                    Category = 2,
                    ComplexType = value,
                    Span = value.Span
                });
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.ComplexTypes.ModelGroups.Length)
        {
            XsdModelGroupDefinition value =
                this.ComplexTypes.ModelGroups[index];
            if (value.Kind == "sequence" || value.Kind == "choice")
            {
                this.Assignments.Add(new XsdGeneratedAssignment
                {
                    StableId = "x694:assignment:" + value.StableId,
                    OriginalName = value.Name,
                    NamespaceUri = value.NamespaceUri,
                    Kind = "group",
                    Category = 3,
                    ModelGroup = value,
                    Span = value.Span
                });
            }
            index = index + 1;
        }
    }

    internal void SortDirectAssignments()
    {
        int index = 1;
        while (index < this.Assignments.Count)
        {
            XsdGeneratedAssignment current = this.Assignments[index];
            int position = index;
            while (position > 0
                && this.CompareDirectAssignments(
                    this.Assignments[position - 1],
                    current) > 0)
            {
                this.Assignments[position] = this.Assignments[position - 1];
                position = position - 1;
            }
            this.Assignments[position] = current;
            index = index + 1;
        }
        index = 0;
        while (index < this.Assignments.Count)
        {
            this.Assignments[index].Order = index;
            index = index + 1;
        }
    }

    internal int CompareDirectAssignments(
        XsdGeneratedAssignment first,
        XsdGeneratedAssignment second)
    {
        int comparison = string.CompareOrdinal(
            first.NamespaceUri,
            second.NamespaceUri);
        if (comparison == 0)
        {
            comparison = first.Category.CompareTo(second.Category);
        }
        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(
                first.OriginalName,
                second.OriginalName);
        }
        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(first.Kind, second.Kind);
        }
        return comparison;
    }

    internal void AllocateDirectNames()
    {
        int index = 0;
        while (index < this.Assignments.Count)
        {
            XsdGeneratedAssignment assignment = this.Assignments[index];
            assignment.Name = this.Names.AllocateTypeName(
                assignment.OriginalName);
            index = index + 1;
        }
    }

    internal void CreateSpecialAssignments()
    {
        int index = 0;
        while (index < this.Advanced.TypeSubstitutions.Length)
        {
            XsdTypeSubstitutionDefinition value =
                this.Advanced.TypeSubstitutions[index];
            this.Assignments.Add(new XsdGeneratedAssignment
            {
                StableId = "x694:assignment:" + value.StableId,
                OriginalName = value.TypeReferenceName,
                NamespaceUri = value.BaseTypeNamespace,
                Kind = "type-substitution",
                Category = 4,
                Order = this.AssociatedTypeOrder(value),
                Special = true,
                TypeSubstitution = value,
                Span = value.Span
            });
            index = index + 1;
        }
        index = 0;
        while (index < this.Advanced.SubstitutionGroups.Length)
        {
            XsdSubstitutionGroupDefinition value =
                this.Advanced.SubstitutionGroups[index];
            this.Assignments.Add(new XsdGeneratedAssignment
            {
                StableId = "x694:assignment:" + value.StableId,
                OriginalName = value.TypeReferenceName,
                NamespaceUri = value.HeadNamespace,
                Kind = "substitution-group",
                Category = 5,
                Order = this.AssociatedGroupOrder(value),
                Special = true,
                SubstitutionGroup = value,
                Span = value.Span
            });
            index = index + 1;
        }
    }

    internal int AssociatedTypeOrder(XsdTypeSubstitutionDefinition value)
    {
        int index = 0;
        while (index < this.Assignments.Count)
        {
            XsdGeneratedAssignment assignment = this.Assignments[index];
            if (!assignment.Special
                && assignment.OriginalName == value.BaseTypeName
                && assignment.NamespaceUri == value.BaseTypeNamespace)
            {
                return assignment.Order;
            }
            index = index + 1;
        }
        return this.Assignments.Count;
    }

    internal int AssociatedGroupOrder(XsdSubstitutionGroupDefinition value)
    {
        int index = 0;
        while (index < this.Assignments.Count)
        {
            XsdGeneratedAssignment assignment = this.Assignments[index];
            if (!assignment.Special && assignment.Declaration == value.Head)
            {
                return assignment.Order;
            }
            index = index + 1;
        }
        return this.Assignments.Count;
    }

    internal void SortSpecialAssignments()
    {
        int first = 0;
        while (first < this.Assignments.Count
            && !this.Assignments[first].Special)
        {
            first = first + 1;
        }
        int index = first + 1;
        while (index < this.Assignments.Count)
        {
            XsdGeneratedAssignment current = this.Assignments[index];
            int position = index;
            while (position > first
                && this.CompareSpecialAssignments(
                    this.Assignments[position - 1],
                    current) > 0)
            {
                this.Assignments[position] = this.Assignments[position - 1];
                position = position - 1;
            }
            this.Assignments[position] = current;
            index = index + 1;
        }
    }

    internal int CompareSpecialAssignments(
        XsdGeneratedAssignment first,
        XsdGeneratedAssignment second)
    {
        int comparison = first.Order.CompareTo(second.Order);
        if (comparison == 0)
        {
            comparison = first.Category.CompareTo(second.Category);
        }
        return comparison == 0
            ? string.CompareOrdinal(first.OriginalName, second.OriginalName)
            : comparison;
    }

    internal void AllocateSpecialNames()
    {
        int index = 0;
        while (index < this.Assignments.Count)
        {
            if (this.Assignments[index].Special)
            {
                this.Assignments[index].Name = this.Names.AllocateTypeName(
                    this.Assignments[index].OriginalName);
            }
            index = index + 1;
        }
    }

    internal void SaveMappedState()
    {
        this.SavedDeclarations =
            new System.Collections.Generic.List<XsdSavedDeclarationState>();
        this.SavedSimpleTypes =
            new System.Collections.Generic.List<XsdSavedSimpleTypeState>();
        this.SavedSimpleUses =
            new System.Collections.Generic.List<XsdSavedSimpleUseState>();
        this.SavedComplexTypes =
            new System.Collections.Generic.List<XsdSavedComplexTypeState>();
        this.SavedGroups =
            new System.Collections.Generic.List<XsdSavedGroupState>();
        this.SavedParticles =
            new System.Collections.Generic.List<XsdSavedParticleState>();
        this.SavedTypeSubstitutions =
            new System.Collections.Generic.List<XsdSavedTypeSubstitutionState>();
        this.SavedSubstitutionGroups =
            new System.Collections.Generic.List<XsdSavedSubstitutionGroupState>();
        int index = 0;
        while (index < this.Declarations.Declarations.Length)
        {
            XsdDeclarationDefinition value =
                this.Declarations.Declarations[index];
            this.SavedDeclarations.Add(new XsdSavedDeclarationState
            {
                Value = value,
                Identifier = value.Identifier,
                TypeReferenceName = value.TypeReferenceName,
                AsnTypeDefinition = value.AsnTypeDefinition,
                OccurrenceTypeDefinition = value.OccurrenceTypeDefinition
            });
            index = index + 1;
        }
        index = 0;
        while (index < this.SimpleTypes.Types.Length)
        {
            XsdSimpleTypeDefinition value = this.SimpleTypes.Types[index];
            string[] identifiers =
                new string[value.EnumerationItems.Length];
            int itemIndex = 0;
            while (itemIndex < value.EnumerationItems.Length)
            {
                identifiers[itemIndex] =
                    value.EnumerationItems[itemIndex].Identifier;
                itemIndex = itemIndex + 1;
            }
            this.SavedSimpleTypes.Add(new XsdSavedSimpleTypeState
            {
                Value = value,
                AsnTypeDefinition = value.AsnTypeDefinition,
                EnumerationIdentifiers = identifiers
            });
            if (value.ItemType != null)
            {
                this.SaveSimpleUse(value.ItemType);
            }
            itemIndex = 0;
            while (itemIndex < value.UnionAlternatives.Length)
            {
                XsdSimpleTypeUse? use = value.UnionAlternatives[itemIndex]
                    .TypeUse;
                if (use != null)
                {
                    this.SaveSimpleUse(use);
                }
                itemIndex = itemIndex + 1;
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.ComplexTypes.Types.Length)
        {
            XsdComplexTypeDefinition value = this.ComplexTypes.Types[index];
            this.SavedComplexTypes.Add(new XsdSavedComplexTypeState
            {
                Value = value,
                TypeReferenceName = value.TypeReferenceName,
                AsnTypeDefinition = value.AsnTypeDefinition
            });
            this.SaveParticleState(value.Particle);
            index = index + 1;
        }
        index = 0;
        while (index < this.ComplexTypes.ModelGroups.Length)
        {
            XsdModelGroupDefinition value =
                this.ComplexTypes.ModelGroups[index];
            this.SavedGroups.Add(new XsdSavedGroupState
            {
                Value = value,
                TypeReferenceName = value.TypeReferenceName,
                AsnTypeDefinition = value.AsnTypeDefinition
            });
            this.SaveParticleState(value.Particle);
            index = index + 1;
        }
        index = 0;
        while (index < this.Advanced.TypeSubstitutions.Length)
        {
            XsdTypeSubstitutionDefinition value =
                this.Advanced.TypeSubstitutions[index];
            this.SavedTypeSubstitutions.Add(
                new XsdSavedTypeSubstitutionState
                {
                    Value = value,
                    TypeReferenceName = value.TypeReferenceName
                });
            index = index + 1;
        }
        index = 0;
        while (index < this.Advanced.SubstitutionGroups.Length)
        {
            XsdSubstitutionGroupDefinition value =
                this.Advanced.SubstitutionGroups[index];
            this.SavedSubstitutionGroups.Add(
                new XsdSavedSubstitutionGroupState
                {
                    Value = value,
                    TypeReferenceName = value.TypeReferenceName
                });
            index = index + 1;
        }
    }

    internal void SaveSimpleUse(XsdSimpleTypeUse use)
    {
        this.SavedSimpleUses.Add(new XsdSavedSimpleUseState
        {
            Value = use,
            AsnTypeDefinition = use.AsnTypeDefinition
        });
    }

    internal void SaveParticleState(XsdParticleDefinition? particle)
    {
        if (particle == null)
        {
            return;
        }
        this.SavedParticles.Add(new XsdSavedParticleState
        {
            Value = particle,
            Identifier = particle.Identifier,
            AsnTypeDefinition = particle.AsnTypeDefinition,
            NamedTypeDefinition = particle.NamedTypeDefinition
        });
        int index = 0;
        while (index < particle.Children.Length)
        {
            this.SaveParticleState(particle.Children[index]);
            index = index + 1;
        }
    }

    internal void RestoreMappedState()
    {
        int index = 0;
        while (index < this.SavedDeclarations.Count)
        {
            XsdSavedDeclarationState state = this.SavedDeclarations[index];
            if (state.Value != null)
            {
                state.Value.Identifier = state.Identifier;
                state.Value.TypeReferenceName = state.TypeReferenceName;
                state.Value.AsnTypeDefinition = state.AsnTypeDefinition;
                state.Value.OccurrenceTypeDefinition =
                    state.OccurrenceTypeDefinition;
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.SavedSimpleTypes.Count)
        {
            XsdSavedSimpleTypeState state = this.SavedSimpleTypes[index];
            if (state.Value != null)
            {
                state.Value.AsnTypeDefinition = state.AsnTypeDefinition;
                int itemIndex = 0;
                while (itemIndex < state.EnumerationIdentifiers.Length)
                {
                    state.Value.EnumerationItems[itemIndex].Identifier =
                        state.EnumerationIdentifiers[itemIndex];
                    itemIndex = itemIndex + 1;
                }
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.SavedSimpleUses.Count)
        {
            XsdSavedSimpleUseState state = this.SavedSimpleUses[index];
            if (state.Value != null)
            {
                state.Value.AsnTypeDefinition = state.AsnTypeDefinition;
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.SavedComplexTypes.Count)
        {
            XsdSavedComplexTypeState state = this.SavedComplexTypes[index];
            if (state.Value != null)
            {
                state.Value.TypeReferenceName = state.TypeReferenceName;
                state.Value.AsnTypeDefinition = state.AsnTypeDefinition;
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.SavedGroups.Count)
        {
            XsdSavedGroupState state = this.SavedGroups[index];
            if (state.Value != null)
            {
                state.Value.TypeReferenceName = state.TypeReferenceName;
                state.Value.AsnTypeDefinition = state.AsnTypeDefinition;
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.SavedParticles.Count)
        {
            XsdSavedParticleState state = this.SavedParticles[index];
            if (state.Value != null)
            {
                state.Value.Identifier = state.Identifier;
                state.Value.AsnTypeDefinition = state.AsnTypeDefinition;
                state.Value.NamedTypeDefinition = state.NamedTypeDefinition;
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.SavedTypeSubstitutions.Count)
        {
            XsdSavedTypeSubstitutionState state =
                this.SavedTypeSubstitutions[index];
            if (state.Value != null)
            {
                state.Value.TypeReferenceName = state.TypeReferenceName;
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.SavedSubstitutionGroups.Count)
        {
            XsdSavedSubstitutionGroupState state =
                this.SavedSubstitutionGroups[index];
            if (state.Value != null)
            {
                state.Value.TypeReferenceName = state.TypeReferenceName;
            }
            index = index + 1;
        }
    }

    internal void RewriteMappedNames()
    {
        int index = 0;
        while (index < this.Assignments.Count)
        {
            XsdGeneratedAssignment assignment = this.Assignments[index];
            if (!assignment.Special)
            {
                if (assignment.Declaration != null)
                {
                    assignment.Declaration.TypeReferenceName = assignment.Name;
                }
                if (assignment.ComplexType != null)
                {
                    assignment.ComplexType.TypeReferenceName = assignment.Name;
                }
                if (assignment.ModelGroup != null)
                {
                    assignment.ModelGroup.TypeReferenceName = assignment.Name;
                }
            }
            else if (assignment.TypeSubstitution != null)
            {
                assignment.TypeSubstitution.TypeReferenceName = assignment.Name;
            }
            else if (assignment.SubstitutionGroup != null)
            {
                assignment.SubstitutionGroup.TypeReferenceName = assignment.Name;
            }
            index = index + 1;
        }
        this.RewriteSimpleTypeUses();
        this.RewriteDeclarations();
    }

    internal void RewriteSimpleTypeUses()
    {
        int index = 0;
        while (index < this.SimpleTypes.Types.Length)
        {
            XsdSimpleTypeDefinition type = this.SimpleTypes.Types[index];
            if (type.ItemType != null)
            {
                this.RewriteSimpleUse(type.ItemType);
            }
            int alternativeIndex = 0;
            while (alternativeIndex < type.UnionAlternatives.Length)
            {
                XsdSimpleTypeUse? use =
                    type.UnionAlternatives[alternativeIndex].TypeUse;
                if (use != null)
                {
                    this.RewriteSimpleUse(use);
                }
                alternativeIndex = alternativeIndex + 1;
            }
            index = index + 1;
        }
    }

    internal void RewriteSimpleUse(XsdSimpleTypeUse use)
    {
        if (use.Definition == null || use.Anonymous)
        {
            return;
        }
        XsdGeneratedAssignment? assignment = this.FindSimpleAssignment(
            use.Definition);
        if (assignment != null)
        {
            use.AsnTypeDefinition = assignment.Name;
        }
    }

    internal void RewriteDeclarations()
    {
        XsdDeclarationMapper renderer = new XsdDeclarationMapper();
        int index = 0;
        while (index < this.Declarations.Declarations.Length)
        {
            XsdDeclarationDefinition declaration =
                this.Declarations.Declarations[index];
            string core = this.RewrittenDeclarationCore(declaration);
            declaration.AsnTypeDefinition = core;
            declaration.OccurrenceTypeDefinition = renderer.RenderOccurrence(
                declaration,
                core);
            index = index + 1;
        }
    }

    internal string RewrittenDeclarationCore(
        XsdDeclarationDefinition declaration)
    {
        if (declaration.TypeSubstitution != null)
        {
            XsdGeneratedAssignment? special = this.FindTypeSubstitutionAssignment(
                declaration.TypeSubstitution);
            if (special != null)
            {
                return special.Name;
            }
        }
        XsdDeclarationTypeUse? use = declaration.TypeUse;
        if (use == null || use.Anonymous || use.BuiltIn)
        {
            return declaration.AsnTypeDefinition;
        }
        string reference = "";
        if (use.Kind == "simple" && use.SimpleType != null)
        {
            XsdGeneratedAssignment? assignment = this.FindSimpleAssignment(
                use.SimpleType);
            reference = assignment == null ? "" : assignment.Name;
        }
        else if (use.Kind == "complex")
        {
            XsdGeneratedAssignment? assignment = this.FindComplexAssignment(
                use.NamespaceUri,
                use.Name);
            reference = assignment == null ? "" : assignment.Name;
        }
        if (reference.Length == 0)
        {
            return declaration.AsnTypeDefinition;
        }
        return declaration.Nillable
            ? "SEQUENCE {content " + reference + " OPTIONAL}"
            : reference;
    }

    internal void NormalizeComponentIdentifiers()
    {
        int index = 0;
        while (index < this.ComplexTypes.Types.Length)
        {
            XsdComplexTypeDefinition type = this.ComplexTypes.Types[index];
            System.Collections.Generic.List<string> used =
                new System.Collections.Generic.List<string>();
            if (type.Mixed)
            {
                used.Add("embed-values");
            }
            int attributeIndex = 0;
            while (attributeIndex < type.AttributeUses.Length)
            {
                XsdDeclarationDefinition attribute =
                    type.AttributeUses[attributeIndex];
                attribute.Identifier = this.Names.AllocateIdentifier(
                    attribute.Name,
                    used);
                attribute.OccurrenceTypeDefinition =
                    new XsdDeclarationMapper().RenderOccurrence(
                        attribute,
                        attribute.AsnTypeDefinition);
                attributeIndex = attributeIndex + 1;
            }
            if (type.AttributeWildcard != null)
            {
                used.Add("attr");
            }
            if (type.ContentKind == "simple")
            {
                used.Add("base");
            }
            if (type.Particle != null)
            {
                if (type.Particle.Kind == "sequence"
                    && !type.Particle.Repeated && !type.Particle.Optional
                    || type.Particle.Kind == "all")
                {
                    if (type.Particle.Kind == "all")
                    {
                        used.Add("order");
                    }
                    this.RenameParticleChildren(type.Particle, used);
                }
                else
                {
                    type.Particle.Identifier = this.Names.AllocateIdentifier(
                        type.Particle.Name,
                        used);
                    this.RenameNestedParticle(type.Particle);
                }
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.ComplexTypes.ModelGroups.Length)
        {
            XsdModelGroupDefinition group =
                this.ComplexTypes.ModelGroups[index];
            if (group.Particle != null)
            {
                this.RenameParticleChildren(
                    group.Particle,
                    new System.Collections.Generic.List<string>());
            }
            index = index + 1;
        }
        this.NormalizeEnumerationIdentifiers();
    }

    internal void RenameParticleChildren(
        XsdParticleDefinition owner,
        System.Collections.Generic.List<string> used)
    {
        int index = 0;
        while (index < owner.Children.Length)
        {
            XsdParticleDefinition child = owner.Children[index];
            if (child.PointlessSequence)
            {
                this.RenameParticleChildren(child, used);
            }
            else
            {
                child.Identifier = this.Names.AllocateIdentifier(
                    child.Name,
                    used);
                if (child.Declaration != null)
                {
                    child.Declaration.Identifier = child.Identifier;
                }
                this.RenameNestedParticle(child);
            }
            index = index + 1;
        }
    }

    internal void RenameNestedParticle(XsdParticleDefinition particle)
    {
        if (particle.Children.Length > 0)
        {
            this.RenameParticleChildren(
                particle,
                new System.Collections.Generic.List<string>());
        }
    }

    internal void NormalizeEnumerationIdentifiers()
    {
        int typeIndex = 0;
        while (typeIndex < this.SimpleTypes.Types.Length)
        {
            XsdSimpleTypeDefinition type = this.SimpleTypes.Types[typeIndex];
            if (type.Enumerated
                && !new XsdSimpleTypeMapper().IsIntegerDerived(
                    type.BuiltInBaseName))
            {
                System.Collections.Generic.List<string> used =
                    new System.Collections.Generic.List<string>();
                int itemIndex = 0;
                while (itemIndex < type.EnumerationItems.Length)
                {
                    type.EnumerationItems[itemIndex].Identifier =
                        this.Names.AllocateIdentifier(
                            type.EnumerationItems[itemIndex].CanonicalValue,
                            used);
                    itemIndex = itemIndex + 1;
                }
            }
            typeIndex = typeIndex + 1;
        }
    }

    internal void RefreshMappedDefinitions()
    {
        int index = 0;
        while (index < this.SimpleTypes.Types.Length)
        {
            this.RefreshSimpleDefinition(this.SimpleTypes.Types[index]);
            index = index + 1;
        }
        index = 0;
        while (index < this.ComplexTypes.ModelGroups.Length)
        {
            XsdModelGroupDefinition group =
                this.ComplexTypes.ModelGroups[index];
            if (group.Particle != null)
            {
                this.RefreshParticleDefinition(group.Particle, false);
                group.AsnTypeDefinition = group.Particle.AsnTypeDefinition;
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.ComplexTypes.Types.Length)
        {
            XsdComplexTypeDefinition type = this.ComplexTypes.Types[index];
            if (type.Particle != null)
            {
                this.RefreshParticleDefinition(type.Particle, true);
            }
            type.AsnTypeDefinition =
                new XsdComplexTypeMapper().RenderComplexType(type);
            index = index + 1;
        }
    }

    internal void RefreshSimpleDefinition(XsdSimpleTypeDefinition type)
    {
        XsdSimpleTypeMapper renderer = new XsdSimpleTypeMapper();
        if (type.Enumerated)
        {
            type.AsnTypeDefinition = renderer.RenderEnumeration(type);
            return;
        }
        if (type.Variety == XsdSimpleTypeVariety.Union)
        {
            System.Collections.Generic.List<XsdUnionAlternative> alternatives =
                new System.Collections.Generic.List<XsdUnionAlternative>(
                    type.UnionAlternatives);
            type.AsnTypeDefinition = renderer.RenderUnion(alternatives);
            return;
        }
        if (type.Variety == XsdSimpleTypeVariety.List
            && type.ItemType != null)
        {
            string previous = type.ItemType.Anonymous
                ? type.ItemType.Definition == null
                    ? type.ItemType.AsnTypeDefinition
                    : type.ItemType.Definition.AsnTypeDefinition
                : new XsdDeclarationMapper().ToTypeReference(
                    type.ItemType.Name);
            type.AsnTypeDefinition = this.ReplaceFirst(
                type.AsnTypeDefinition,
                previous,
                type.ItemType.AsnTypeDefinition);
            return;
        }
        if (type.BaseDefinition != null)
        {
            XsdGeneratedAssignment? assignment = this.FindSimpleAssignment(
                type.BaseDefinition);
            if (assignment != null)
            {
                type.AsnTypeDefinition = this.ReplaceFirst(
                    type.AsnTypeDefinition,
                    new XsdDeclarationMapper().ToTypeReference(
                        type.BaseDefinition.Name),
                    assignment.Name);
            }
        }
    }

    internal void RefreshParticleDefinition(
        XsdParticleDefinition particle,
        bool sequenceComponent)
    {
        int index = 0;
        while (index < particle.Children.Length)
        {
            this.RefreshParticleDefinition(
                particle.Children[index],
                particle.Kind == "sequence" || particle.Kind == "all");
            index = index + 1;
        }
        XsdComplexTypeMapper renderer = new XsdComplexTypeMapper();
        if (particle.Kind == "sequence" || particle.Kind == "choice"
            || particle.Kind == "all")
        {
            particle.AsnTypeDefinition = renderer.RenderModelGroup(particle);
            particle.NamedTypeDefinition = particle.PointlessSequence
                ? renderer.RenderComponents(particle.Children)
                : renderer.RenderParticleUse(
                    particle,
                    particle.AsnTypeDefinition,
                    sequenceComponent,
                    particle.Identifier);
            return;
        }
        string core = particle.AsnTypeDefinition;
        if (particle.Declaration != null)
        {
            core = particle.Declaration.AsnTypeDefinition;
        }
        else if (particle.ReferencedGroup != null)
        {
            core = particle.ReferencedGroup.TypeReferenceName;
        }
        else if (particle.Wildcard != null)
        {
            core = this.RenderWildcardDefinition(particle.Wildcard);
        }
        particle.AsnTypeDefinition = core;
        particle.NamedTypeDefinition = renderer.RenderParticleUse(
            particle,
            core,
            sequenceComponent,
            particle.Identifier);
    }

    internal string RenderWildcardDefinition(XsdWildcardDefinition wildcard)
    {
        return new XsdSimpleTypeMapper().RenderConstrainedType(
            wildcard.AsnTypeDefinition,
            wildcard.Constraints);
    }

    internal string ReplaceFirst(
        string source,
        string oldValue,
        string newValue)
    {
        if (oldValue.Length == 0 || oldValue == newValue)
        {
            return source;
        }
        int index = source.IndexOf(
            oldValue,
            System.StringComparison.Ordinal);
        return index < 0
            ? source
            : source.Substring(0, index) + newValue
                + source.Substring(index + oldValue.Length);
    }

    internal void FillAssignmentDefinitions()
    {
        int index = 0;
        while (index < this.Assignments.Count)
        {
            XsdGeneratedAssignment assignment = this.Assignments[index];
            if (assignment.Declaration != null)
            {
                assignment.Definition = assignment.Declaration.AsnTypeDefinition;
            }
            else if (assignment.SimpleType != null)
            {
                assignment.Definition = assignment.SimpleType.AsnTypeDefinition;
            }
            else if (assignment.ComplexType != null)
            {
                assignment.Definition = assignment.ComplexType.AsnTypeDefinition;
                if (assignment.ComplexType.AttributeWildcard != null)
                {
                    XsdWildcardDefinition wildcard =
                        assignment.ComplexType.AttributeWildcard;
                    assignment.Definition = this.ReplaceFirst(
                        assignment.Definition,
                        "attr " + wildcard.AsnTypeDefinition,
                        "attr " + this.RenderWildcardDefinition(wildcard));
                }
            }
            else if (assignment.ModelGroup != null)
            {
                assignment.Definition = assignment.ModelGroup.AsnTypeDefinition;
            }
            else if (assignment.TypeSubstitution != null)
            {
                assignment.Definition = this.RenderTypeSubstitution(
                    assignment.TypeSubstitution);
            }
            else if (assignment.SubstitutionGroup != null)
            {
                assignment.Definition = this.RenderSubstitutionGroup(
                    assignment.SubstitutionGroup);
            }
            index = index + 1;
        }
    }

    internal string RenderTypeSubstitution(
        XsdTypeSubstitutionDefinition substitution)
    {
        string result = "CHOICE {";
        System.Collections.Generic.List<string> identifiers =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < substitution.Alternatives.Length)
        {
            XsdTypeAlternativeDefinition alternative =
                substitution.Alternatives[index];
            if (index > 0)
            {
                result = result + ", ";
            }
            string identifier = this.Names.AllocateIdentifier(
                alternative.Name,
                identifiers);
            string definition = alternative.AsnTypeDefinition;
            if (!alternative.Nillable && alternative.SimpleType != null)
            {
                XsdGeneratedAssignment? assignment = this.FindSimpleAssignment(
                    alternative.SimpleType);
                definition = assignment == null
                    ? definition
                    : assignment.Name;
            }
            else if (!alternative.Nillable && alternative.ComplexType != null)
            {
                XsdGeneratedAssignment? assignment = this.FindComplexAssignment(
                    alternative.NamespaceUri,
                    alternative.Name);
                definition = assignment == null
                    ? definition
                    : assignment.Name;
            }
            result = result + identifier + " " + definition;
            index = index + 1;
        }
        return result + "}";
    }

    internal string RenderSubstitutionGroup(
        XsdSubstitutionGroupDefinition group)
    {
        string result = "CHOICE {";
        System.Collections.Generic.List<string> identifiers =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < group.Members.Length)
        {
            XsdDeclarationDefinition member = group.Members[index];
            if (index > 0)
            {
                result = result + ", ";
            }
            XsdGeneratedAssignment? assignment =
                this.FindDeclarationAssignment(member);
            result = result + this.Names.AllocateIdentifier(
                member.Name,
                identifiers) + " " + (assignment == null
                    ? member.TypeReferenceName
                    : assignment.Name);
            index = index + 1;
        }
        return result + "}";
    }

    internal XsdGeneratedModule CreateModule(string moduleName)
    {
        XsdGeneratedModule module = new XsdGeneratedModule
        {
            StableId = "x694:generated-module:"
                + new XsdIdentityBuilder().EscapeIdentity(moduleName),
            ModuleName = moduleName,
            ModuleIdentifier = moduleName,
            AutomaticTags = true,
            TargetNamespaces = this.CollectNamespaces(),
            Assignments = this.Assignments.ToArray()
        };
        module.Imports = this.CreateImports(module.Assignments);
        module.XerInstructions = this.CreateInstructions(module.Assignments);
        return module;
    }

    internal string[] CollectNamespaces()
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < this.Assignments.Count)
        {
            string value = this.Assignments[index].NamespaceUri;
            if (!this.ContainsString(result, value))
            {
                result.Add(value);
            }
            index = index + 1;
        }
        this.SortStrings(result);
        return result.ToArray();
    }

    internal XsdGeneratedImport[] CreateImports(
        XsdGeneratedAssignment[] assignments)
    {
        System.Collections.Generic.List<string> symbols =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < assignments.Length)
        {
            this.CollectSupportReferences(assignments[index].Definition, symbols);
            index = index + 1;
        }
        this.SortStrings(symbols);
        if (symbols.Count == 0)
        {
            return [];
        }
        return
        [
            new XsdGeneratedImport
            {
                ModuleName = this.Support.ModuleName,
                ModuleIdentifier =
                    "{ joint-iso-itu-t(2) asn1(1) specification(0) modules(0) "
                        + "xsd-module(2) version2(2) }",
                Symbols = symbols.ToArray()
            }
        ];
    }

    internal void CollectSupportReferences(
        string definition,
        System.Collections.Generic.List<string> symbols)
    {
        string prefix = this.Support.ModuleName + ".";
        int index = 0;
        while (index < definition.Length)
        {
            int found = definition.IndexOf(
                prefix,
                index,
                System.StringComparison.Ordinal);
            if (found < 0)
            {
                return;
            }
            int start = found + prefix.Length;
            int end = start;
            while (end < definition.Length
                && this.IsAsnNameCharacter(definition[end]))
            {
                end = end + 1;
            }
            if (end > start)
            {
                string symbol = definition.Substring(start, end - start);
                if (!this.ContainsString(symbols, symbol))
                {
                    symbols.Add(symbol);
                }
            }
            index = end > start ? end : start;
        }
    }

    internal bool IsAsnNameCharacter(char value)
    {
        return value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z'
            || value >= '0' && value <= '9'
            || value == '-';
    }

    internal XsdGeneratedInstruction[] CreateInstructions(
        XsdGeneratedAssignment[] assignments)
    {
        System.Collections.Generic.List<XsdGeneratedInstruction> result =
            new System.Collections.Generic.List<XsdGeneratedInstruction>();
        int index = 0;
        while (index < assignments.Length)
        {
            XsdGeneratedAssignment assignment = assignments[index];
            this.AddTopAssignmentInstructions(assignment, result);
            XsdDeclarationTypeUse? declarationType =
                assignment.Declaration == null
                    ? null
                    : assignment.Declaration.TypeUse;
            if (declarationType != null
                && declarationType.Anonymous
                && declarationType.MappedComplexType != null)
            {
                this.AddComplexInstructions(
                    assignment.Name,
                    declarationType.MappedComplexType,
                    result);
            }
            else if (assignment.ComplexType != null)
            {
                this.AddComplexInstructions(
                    assignment.Name,
                    assignment.ComplexType,
                    result);
            }
            else if (assignment.ModelGroup != null
                && assignment.ModelGroup.Particle != null)
            {
                this.AddParticleInstructions(
                    assignment.Name,
                    assignment.ModelGroup.Particle,
                    result,
                    true);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal void AddTopAssignmentInstructions(
        XsdGeneratedAssignment assignment,
        System.Collections.Generic.List<XsdGeneratedInstruction> result)
    {
        AsnEncodingInstruction[] source = [];
        if (assignment.Declaration != null)
        {
            source = assignment.Declaration.XerInstructions;
        }
        else if (assignment.SimpleType != null)
        {
            source = assignment.SimpleType.XerInstructions;
        }
        else if (assignment.ComplexType != null)
        {
            source = assignment.ComplexType.XerInstructions;
        }
        else if (assignment.TypeSubstitution != null)
        {
            source = assignment.TypeSubstitution.XerInstructions;
        }
        else if (assignment.SubstitutionGroup != null)
        {
            source = assignment.SubstitutionGroup.XerInstructions;
        }
        int index = 0;
        while (index < source.Length)
        {
            if (source[index].Kind != AsnInstructionKind.Name
                && source[index].Kind != AsnInstructionKind.Namespace)
            {
                this.AddGeneratedInstruction(
                    result,
                    assignment.Name,
                    source[index]);
            }
            index = index + 1;
        }
        AsnEncodingInstruction? name = this.CreateNameInstruction(
            assignment.OriginalName,
            assignment.Name,
            true);
        if (name != null && !assignment.Special)
        {
            name.Span = assignment.Span;
            this.AddGeneratedInstruction(result, assignment.Name, name);
        }
        if (assignment.NamespaceUri.Length > 0 && !assignment.Special)
        {
            this.AddGeneratedInstruction(
                result,
                assignment.Name,
                new AsnEncodingInstruction
                {
                    Name = "NAMESPACE",
                    Kind = AsnInstructionKind.Namespace,
                    Arguments = [assignment.NamespaceUri],
                    Span = assignment.Span
                });
        }
    }

    internal void AddComplexInstructions(
        string path,
        XsdComplexTypeDefinition type,
        System.Collections.Generic.List<XsdGeneratedInstruction> result)
    {
        int index = 0;
        while (index < type.AttributeUses.Length)
        {
            this.AddDeclarationInstructions(
                path + "." + type.AttributeUses[index].Identifier,
                type.AttributeUses[index],
                result);
            index = index + 1;
        }
        if (type.AttributeWildcard != null)
        {
            this.AddInstructionArray(
                result,
                path + ".attr",
                type.AttributeWildcard.XerInstructions);
        }
        if (type.Particle != null)
        {
            this.AddParticleInstructions(
                path,
                type.Particle,
                result,
                true);
        }
    }

    internal void AddParticleInstructions(
        string path,
        XsdParticleDefinition particle,
        System.Collections.Generic.List<XsdGeneratedInstruction> result,
        bool root)
    {
        string target = root && (particle.Kind == "sequence"
                && !particle.Repeated && !particle.Optional
                || particle.Kind == "all")
            ? path
            : path + "." + particle.Identifier
                + (particle.Repeated ? "-list" : "");
        if (!(root && particle.PointlessSequence))
        {
            this.AddInstructionArray(result, target, particle.XerInstructions);
        }
        if (particle.Declaration != null)
        {
            string declarationTarget = particle.Repeated
                ? target + ".*"
                : target;
            this.AddDeclarationInstructions(
                declarationTarget,
                particle.Declaration,
                result);
            XsdDeclarationTypeUse? type = particle.Declaration.TypeUse;
            if (type != null
                && type.Anonymous
                && type.MappedComplexType != null)
            {
                this.AddComplexInstructions(
                    declarationTarget,
                    type.MappedComplexType,
                    result);
            }
        }
        int index = 0;
        while (index < particle.Children.Length)
        {
            this.AddParticleInstructions(
                particle.PointlessSequence ? path : target,
                particle.Children[index],
                result,
                false);
            index = index + 1;
        }
    }

    internal void AddDeclarationInstructions(
        string target,
        XsdDeclarationDefinition declaration,
        System.Collections.Generic.List<XsdGeneratedInstruction> result)
    {
        int index = 0;
        while (index < declaration.XerInstructions.Length)
        {
            AsnEncodingInstruction instruction =
                declaration.XerInstructions[index];
            if (instruction.Kind != AsnInstructionKind.Name
                && instruction.Kind != AsnInstructionKind.Namespace)
            {
                this.AddGeneratedInstruction(result, target, instruction);
            }
            index = index + 1;
        }
        AsnEncodingInstruction? name = this.CreateNameInstruction(
            declaration.Name,
            declaration.Identifier,
            false);
        if (name != null)
        {
            name.Span = declaration.Span;
            this.AddGeneratedInstruction(result, target, name);
        }
        if (declaration.NamespaceUri.Length > 0)
        {
            this.AddGeneratedInstruction(
                result,
                target,
                new AsnEncodingInstruction
                {
                    Name = "NAMESPACE",
                    Kind = AsnInstructionKind.Namespace,
                    Arguments = [declaration.NamespaceUri],
                    Span = declaration.Span
                });
        }
    }

    internal AsnEncodingInstruction? CreateNameInstruction(
        string original,
        string mapped,
        bool typeReference)
    {
        if (original == mapped)
        {
            return null;
        }
        string[] arguments;
        if (original.Length == mapped.Length && original.Length > 0
            && original.Substring(1) == mapped.Substring(1)
            && char.ToLowerInvariant(original[0])
                == char.ToLowerInvariant(mapped[0]))
        {
            arguments =
            [
                "AS",
                typeReference ? "UNCAPITALIZED" : "CAPITALIZED"
            ];
        }
        else
        {
            arguments = ["AS", original];
        }
        return new AsnEncodingInstruction
        {
            Name = "NAME",
            Kind = AsnInstructionKind.Name,
            Arguments = arguments
        };
    }

    internal void AddInstructionArray(
        System.Collections.Generic.List<XsdGeneratedInstruction> result,
        string target,
        AsnEncodingInstruction[] instructions)
    {
        int index = 0;
        while (index < instructions.Length)
        {
            this.AddGeneratedInstruction(result, target, instructions[index]);
            index = index + 1;
        }
    }

    internal void AddGeneratedInstruction(
        System.Collections.Generic.List<XsdGeneratedInstruction> result,
        string target,
        AsnEncodingInstruction instruction)
    {
        int index = 0;
        while (index < result.Count)
        {
            XsdGeneratedInstruction existing = result[index];
            if (existing.Target == target && existing.Instruction != null
                && existing.Instruction.Kind == instruction.Kind
                && this.StringArraysEqual(
                    existing.Instruction.Arguments,
                    instruction.Arguments))
            {
                return;
            }
            index = index + 1;
        }
        result.Add(new XsdGeneratedInstruction
        {
            Target = target,
            Instruction = instruction,
            Span = instruction.Span
        });
    }

    internal AsnSourceUnit RenderModule(XsdGeneratedModule module)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        System.Collections.Generic.List<XsdGeneratedSourceMapEntry> sourceMap =
            new System.Collections.Generic.List<XsdGeneratedSourceMapEntry>();
        text.Append(module.ModuleName);
        text.Append(" DEFINITIONS XER INSTRUCTIONS AUTOMATIC TAGS ::=\n");
        text.Append("BEGIN\n");
        if (module.Imports.Length > 0)
        {
            text.Append("IMPORTS\n");
            int importIndex = 0;
            while (importIndex < module.Imports.Length)
            {
                XsdGeneratedImport import = module.Imports[importIndex];
                int symbolIndex = 0;
                while (symbolIndex < import.Symbols.Length)
                {
                    if (symbolIndex > 0)
                    {
                        text.Append(", ");
                    }
                    text.Append(import.Symbols[symbolIndex]);
                    symbolIndex = symbolIndex + 1;
                }
                text.Append(" FROM ");
                text.Append(import.ModuleName);
                if (import.ModuleIdentifier.Length > 0)
                {
                    text.Append(' ');
                    text.Append(import.ModuleIdentifier);
                }
                if (importIndex + 1 < module.Imports.Length)
                {
                    text.Append('\n');
                }
                importIndex = importIndex + 1;
            }
            text.Append(";\n");
        }
        int assignmentIndex = 0;
        while (assignmentIndex < module.Assignments.Length)
        {
            XsdGeneratedAssignment assignment =
                module.Assignments[assignmentIndex];
            text.Append('\n');
            int assignmentStart = text.Length;
            text.Append(assignment.Name);
            text.Append(" ::= ");
            text.Append(assignment.Definition);
            int assignmentEnd = text.Length;
            XsdGeneratedSourceMapEntry assignmentMapping =
                this.CreateAssignmentSourceMapping(
                    assignment,
                    assignmentStart,
                    assignmentEnd);
            if (assignmentMapping.XsdSpan != null)
            {
                sourceMap.Add(assignmentMapping);
            }
            text.Append(';');
            text.Append('\n');
            assignmentIndex = assignmentIndex + 1;
        }
        text.Append("\nENCODING-CONTROL XER\n");
        text.Append("GLOBAL-DEFAULTS MODIFIED-ENCODINGS\n");
        text.Append("GLOBAL-DEFAULTS CONTROL-NAMESPACE ");
        text.Append(this.Quote("http://www.w3.org/2001/XMLSchema-instance"));
        text.Append(" PREFIX ");
        text.Append(this.Quote("xsi"));
        text.Append('\n');
        int instructionIndex = 0;
        while (instructionIndex < module.XerInstructions.Length)
        {
            XsdGeneratedInstruction generatedInstruction =
                module.XerInstructions[instructionIndex];
            string line = this.RenderInstruction(generatedInstruction);
            if (line.Length > 0)
            {
                int instructionStart = text.Length;
                text.Append(line);
                int instructionEnd = text.Length;
                XsdGeneratedSourceMapEntry instructionMapping =
                    this.CreateInstructionSourceMapping(
                        module,
                        generatedInstruction,
                        instructionStart,
                        instructionEnd);
                if (instructionMapping.XsdSpan != null)
                {
                    sourceMap.Add(instructionMapping);
                }
                text.Append('\n');
            }
            instructionIndex = instructionIndex + 1;
        }
        text.Append("END\n");
        AsnSourceUnit sourceUnit = new AsnSourceUnit
        {
            Name = module.ModuleName + ".asn1",
            Text = text.ToString()
        };
        int mappingIndex = 0;
        while (mappingIndex < sourceMap.Count)
        {
            XsdGeneratedSourceMapEntry mapping = sourceMap[mappingIndex];
            mapping.GeneratedSpan = sourceUnit.GetSpan(
                mapping.GeneratedStartOffset,
                mapping.GeneratedEndOffset);
            mappingIndex = mappingIndex + 1;
        }
        module.SourceMap = sourceMap.ToArray();
        module.XsdSourceMappings = this.CreateIrSourceMappings(
            module.SourceMap);
        return sourceUnit;
    }

    internal XsdSourceMappingSyntax[] CreateIrSourceMappings(
        XsdGeneratedSourceMapEntry[] sourceMap)
    {
        XsdSourceMappingSyntax[] result =
            new XsdSourceMappingSyntax[sourceMap.Length];
        int index = 0;
        while (index < sourceMap.Length)
        {
            XsdGeneratedSourceMapEntry mapping = sourceMap[index];
            result[index] = new XsdSourceMappingSyntax
            {
                ComponentName = mapping.XsdComponentId.Length > 0
                    ? mapping.XsdComponentId
                    : mapping.XsdComponentName,
                GeneratedSpan = mapping.GeneratedSpan,
                SourceSpan = mapping.XsdSpan
            };
            index = index + 1;
        }
        return result;
    }

    internal XsdGeneratedSourceMapEntry CreateAssignmentSourceMapping(
        XsdGeneratedAssignment assignment,
        int startOffset,
        int endOffset)
    {
        XsdGeneratedSourceMapEntry result = new XsdGeneratedSourceMapEntry
        {
            StableId = "x694:source-map:" + assignment.StableId
                + ":assignment",
            Kind = "assignment",
            GeneratedSymbol = assignment.Name,
            GeneratedStartOffset = startOffset,
            GeneratedEndOffset = endOffset,
            XsdComponentKind = assignment.Kind,
            XsdComponentName = assignment.OriginalName,
            XsdSpan = assignment.Span
        };
        this.FillAssignmentSourceIdentity(result, assignment);
        return result;
    }

    internal XsdGeneratedSourceMapEntry CreateInstructionSourceMapping(
        XsdGeneratedModule module,
        XsdGeneratedInstruction generated,
        int startOffset,
        int endOffset)
    {
        XsdGeneratedAssignment? assignment =
            this.FindOwningAssignment(module, generated.Target);
        AsnSourceSpan? xsdSpan = generated.Span;
        if (xsdSpan == null && assignment != null)
        {
            xsdSpan = assignment.Span;
        }
        XsdGeneratedSourceMapEntry result = new XsdGeneratedSourceMapEntry
        {
            StableId = "x694:source-map:" + module.StableId
                + ":instruction:" + startOffset.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
            Kind = "instruction",
            GeneratedSymbol = generated.Target,
            GeneratedStartOffset = startOffset,
            GeneratedEndOffset = endOffset,
            XsdSpan = xsdSpan
        };
        if (assignment != null)
        {
            result.XsdComponentKind = assignment.Kind;
            result.XsdComponentName = assignment.OriginalName;
            this.FillAssignmentSourceIdentity(result, assignment);
        }
        if (result.XsdDocumentId.Length == 0 && xsdSpan != null)
        {
            result.XsdDocumentId = this.FindSourceDocumentId(xsdSpan);
        }
        return result;
    }

    internal XsdGeneratedAssignment? FindOwningAssignment(
        XsdGeneratedModule module,
        string target)
    {
        int index = 0;
        while (index < module.Assignments.Length)
        {
            XsdGeneratedAssignment assignment = module.Assignments[index];
            if (target == assignment.Name
                || target.StartsWith(
                    assignment.Name + ".",
                    System.StringComparison.Ordinal)
                || target.StartsWith(
                    assignment.Name + ":",
                    System.StringComparison.Ordinal))
            {
                return assignment;
            }
            index = index + 1;
        }
        return null;
    }

    internal void FillAssignmentSourceIdentity(
        XsdGeneratedSourceMapEntry result,
        XsdGeneratedAssignment assignment)
    {
        if (assignment.Declaration != null)
        {
            result.XsdComponentId = assignment.Declaration.SourceComponentId;
            if (assignment.Declaration.Document != null)
            {
                result.XsdDocumentId =
                    assignment.Declaration.Document.StableId;
            }
        }
        else if (assignment.SimpleType != null
            && assignment.SimpleType.Component != null)
        {
            result.XsdComponentId = assignment.SimpleType.Component.StableId;
            result.XsdDocumentId =
                assignment.SimpleType.Component.SourceDocumentId;
            result.XsdComponentKind = assignment.SimpleType.Component.Kind;
            result.XsdComponentName = assignment.SimpleType.Component.Name;
        }
        else if (assignment.ComplexType != null
            && assignment.ComplexType.Component != null)
        {
            result.XsdComponentId = assignment.ComplexType.Component.StableId;
            result.XsdDocumentId =
                assignment.ComplexType.Component.SourceDocumentId;
            result.XsdComponentKind = assignment.ComplexType.Component.Kind;
            result.XsdComponentName = assignment.ComplexType.Component.Name;
        }
        else if (assignment.ModelGroup != null
            && assignment.ModelGroup.Component != null)
        {
            result.XsdComponentId = assignment.ModelGroup.Component.StableId;
            result.XsdDocumentId =
                assignment.ModelGroup.Component.SourceDocumentId;
            result.XsdComponentKind = assignment.ModelGroup.Component.Kind;
            result.XsdComponentName = assignment.ModelGroup.Component.Name;
        }
        else if (assignment.SubstitutionGroup != null
            && assignment.SubstitutionGroup.Head != null)
        {
            result.XsdComponentId =
                assignment.SubstitutionGroup.Head.SourceComponentId;
            if (assignment.SubstitutionGroup.Head.Document != null)
            {
                result.XsdDocumentId =
                    assignment.SubstitutionGroup.Head.Document.StableId;
            }
            result.XsdComponentKind = "element";
            result.XsdComponentName =
                assignment.SubstitutionGroup.Head.Name;
        }
        else if (assignment.TypeSubstitution != null)
        {
            result.XsdComponentId = assignment.TypeSubstitution.StableId;
            result.XsdComponentKind = "type-substitution";
            result.XsdComponentName =
                assignment.TypeSubstitution.BaseTypeName;
        }
        if (result.XsdDocumentId.Length == 0 && result.XsdSpan != null)
        {
            result.XsdDocumentId = this.FindSourceDocumentId(result.XsdSpan);
        }
    }

    internal string FindSourceDocumentId(AsnSourceSpan span)
    {
        int index = 0;
        while (index < this.Graph.DocumentInstances.Length)
        {
            XsdLoadedDocument? document =
                this.Graph.DocumentInstances[index].Document;
            if (document != null && document.Input != null
                && document.Input.Name == span.Start.SourceName)
            {
                return document.StableId;
            }
            index = index + 1;
        }
        return "";
    }

    internal string RenderInstruction(XsdGeneratedInstruction generated)
    {
        AsnEncodingInstruction? instruction = generated.Instruction;
        if (instruction == null || instruction.Kind == AsnInstructionKind.Unknown
            || instruction.Kind == AsnInstructionKind.GlobalDefaults)
        {
            return "";
        }
        string name = instruction.Negated
            ? "NOT-" + instruction.Name
            : instruction.Name;
        string result = name + " " + generated.Target;
        if (instruction.Kind == AsnInstructionKind.Name)
        {
            if (instruction.Arguments.Length > 1)
            {
                string value = instruction.Arguments[1];
                result = result + " AS "
                    + (value == "CAPITALIZED" || value == "UNCAPITALIZED"
                        ? value
                        : this.Quote(value));
            }
            return result;
        }
        if (instruction.Kind == AsnInstructionKind.Namespace)
        {
            return instruction.Arguments.Length == 0
                ? result
                : result + " AS " + this.Quote(instruction.Arguments[0]);
        }
        if (instruction.Kind == AsnInstructionKind.DefaultForEmpty)
        {
            return instruction.Arguments.Length == 0
                ? result
                : result + " AS " + this.Quote(instruction.Arguments[0]);
        }
        if (instruction.Kind == AsnInstructionKind.Text
            && instruction.Targets.Length > 0)
        {
            result = name + " ";
            int targetIndex = 0;
            while (targetIndex < instruction.Targets.Length)
            {
                if (targetIndex > 0)
                {
                    result = result + ", ";
                }
                result = result + generated.Target + ":"
                    + instruction.Targets[targetIndex];
                targetIndex = targetIndex + 1;
            }
            return result;
        }
        if (instruction.Kind == AsnInstructionKind.Whitespace
            && instruction.Arguments.Length > 0)
        {
            return result + " " + instruction.Arguments[0];
        }
        return result;
    }

    internal string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    internal XsdGeneratedAssignment? FindSimpleAssignment(
        XsdSimpleTypeDefinition type)
    {
        int index = 0;
        while (index < this.Assignments.Count)
        {
            if (this.Assignments[index].SimpleType == type)
            {
                return this.Assignments[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdGeneratedAssignment? FindComplexAssignment(
        string namespaceUri,
        string name)
    {
        int index = 0;
        while (index < this.Assignments.Count)
        {
            XsdGeneratedAssignment assignment = this.Assignments[index];
            if (assignment.ComplexType != null
                && assignment.NamespaceUri == namespaceUri
                && assignment.OriginalName == name)
            {
                return assignment;
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdGeneratedAssignment? FindDeclarationAssignment(
        XsdDeclarationDefinition declaration)
    {
        int index = 0;
        while (index < this.Assignments.Count)
        {
            if (this.Assignments[index].Declaration == declaration)
            {
                return this.Assignments[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdGeneratedAssignment? FindTypeSubstitutionAssignment(
        XsdTypeSubstitutionDefinition substitution)
    {
        int index = 0;
        while (index < this.Assignments.Count)
        {
            if (this.Assignments[index].TypeSubstitution == substitution)
            {
                return this.Assignments[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal bool ContainsString(
        System.Collections.Generic.List<string> values,
        string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void SortStrings(System.Collections.Generic.List<string> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            string current = values[index];
            int position = index;
            while (position > 0
                && string.CompareOrdinal(values[position - 1], current) > 0)
            {
                values[position] = values[position - 1];
                position = position - 1;
            }
            values[position] = current;
            index = index + 1;
        }
    }

    internal bool StringArraysEqual(string[] first, string[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal void AddDiagnostic(
        int code,
        string message,
        AsnSourceSpan? span)
    {
        if (this.Diagnostics.Count < this.Limits.MaximumDiagnostics)
        {
            this.Diagnostics.Add(new AsnDiagnostic
            {
                Severity = AsnDiagnosticSeverity.Error,
                Category = AsnDiagnosticCategory.Xsd,
                Code = DiagnosticBag.CreateCode(
                    AsnDiagnosticCategory.Xsd,
                    code),
                Message = message,
                Span = span
            });
        }
    }

    internal XsdModuleGenerationResult CreateResult(
        XsdGeneratedModule[] modules)
    {
        AsnSourceUnit[] sourceUnits = new AsnSourceUnit[modules.Length];
        System.Collections.Generic.List<XsdGeneratedSourceMapEntry> sourceMap =
            new System.Collections.Generic.List<XsdGeneratedSourceMapEntry>();
        int index = 0;
        while (index < modules.Length)
        {
            sourceUnits[index] = modules[index].SourceUnit;
            int mappingIndex = 0;
            while (mappingIndex < modules[index].SourceMap.Length)
            {
                sourceMap.Add(modules[index].SourceMap[mappingIndex]);
                mappingIndex = mappingIndex + 1;
            }
            index = index + 1;
        }
        return new XsdModuleGenerationResult
        {
            Success = this.Diagnostics.Count == 0,
            Modules = modules,
            SourceUnits = sourceUnits,
            SourceMap = sourceMap.ToArray(),
            Diagnostics = this.Diagnostics.ToArray()
        };
    }
}
