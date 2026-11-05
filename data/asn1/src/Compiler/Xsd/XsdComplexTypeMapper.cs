namespace asn1;

internal class XsdComplexTypeMapper
{
    internal XsdSchemaGraphResult Graph = new XsdSchemaGraphResult();
    internal XsdSimpleTypeMappingResult SimpleTypes =
        new XsdSimpleTypeMappingResult();
    internal XsdDeclarationMappingResult Declarations =
        new XsdDeclarationMappingResult();
    internal AsnResourceLimits Limits = new AsnResourceLimits();
    internal XsdSimpleTypeMapper SimpleMapper = new XsdSimpleTypeMapper();
    internal System.Collections.Generic.List<XsdComplexTypeDefinition> Types =
        new System.Collections.Generic.List<XsdComplexTypeDefinition>();
    internal System.Collections.Generic.List<XsdModelGroupDefinition> Groups =
        new System.Collections.Generic.List<XsdModelGroupDefinition>();
    internal System.Collections.Generic.List<AsnDiagnostic> Diagnostics =
        new System.Collections.Generic.List<AsnDiagnostic>();
    internal System.Collections.Generic.List<string> Active =
        new System.Collections.Generic.List<string>();

    internal XsdComplexTypeMappingResult MapComplexTypes(
        XsdSchemaGraphResult graph,
        XsdSimpleTypeMappingResult simpleTypes,
        XsdDeclarationMappingResult declarations,
        AsnXsdImportOptions options)
    {
        if (graph == null || simpleTypes == null || declarations == null
            || options == null)
        {
            throw new System.ArgumentNullException("mapping input");
        }
        this.Graph = graph;
        this.SimpleTypes = simpleTypes;
        this.Declarations = declarations;
        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        this.Types = new System.Collections.Generic.List<XsdComplexTypeDefinition>();
        this.Groups = new System.Collections.Generic.List<XsdModelGroupDefinition>();
        this.Diagnostics = new System.Collections.Generic.List<AsnDiagnostic>();
        this.Active = new System.Collections.Generic.List<string>();
        this.SimpleMapper = new XsdSimpleTypeMapper
        {
            Graph = graph,
            Limits = this.Limits,
            Types = new System.Collections.Generic.List<XsdSimpleTypeDefinition>(
                simpleTypes.Types),
            Diagnostics = new System.Collections.Generic.List<AsnDiagnostic>(),
            ActiveTypes = new System.Collections.Generic.List<string>()
        };
        if (!graph.Success || !simpleTypes.Success || !declarations.Success)
        {
            return this.CreateResult();
        }
        this.CreateGroupSkeletons();
        this.CreateTypeSkeletons();
        int index = 0;
        while (index < this.Groups.Count)
        {
            this.MapGroup(this.Groups[index]);
            index = index + 1;
        }
        index = 0;
        while (index < this.Types.Count)
        {
            this.MapType(this.Types[index]);
            index = index + 1;
        }
        simpleTypes.Types = this.SimpleMapper.Types.ToArray();
        this.RefreshDeclarations();
        return this.CreateResult();
    }

    internal void CreateGroupSkeletons()
    {
        int index = 0;
        while (index < this.Graph.Components.Length)
        {
            XsdComponentDefinition component = this.Graph.Components[index];
            if (component.Kind == "group" || component.Kind == "attributeGroup")
            {
                XsdDocumentInstance? instance = this.FindInstance(
                    component.SourceInstanceId);
                this.Groups.Add(new XsdModelGroupDefinition
                {
                    StableId = "x694:" + component.Kind + ":"
                        + component.StableId,
                    Name = component.Name,
                    NamespaceUri = component.NamespaceUri,
                    TypeReferenceName = new XsdDeclarationMapper()
                        .ToTypeReference(component.Name),
                    AttributeGroup = component.Kind == "attributeGroup",
                    Component = component,
                    DocumentInstance = instance,
                    Document = instance == null ? null : instance.Document,
                    Token = component.Token,
                    Span = component.Span
                });
            }
            index = index + 1;
        }
    }

    internal void CreateTypeSkeletons()
    {
        int index = 0;
        while (index < this.Graph.Components.Length)
        {
            XsdComponentDefinition component = this.Graph.Components[index];
            if (component.Kind == "complexType")
            {
                XsdDocumentInstance? instance = this.FindInstance(
                    component.SourceInstanceId);
                this.Types.Add(new XsdComplexTypeDefinition
                {
                    StableId = "x694:complex-type:" + component.StableId,
                    Name = component.Name,
                    NamespaceUri = component.NamespaceUri,
                    TypeReferenceName = new XsdDeclarationMapper()
                        .ToTypeReference(component.Name),
                    Component = component,
                    DocumentInstance = instance,
                    Document = instance == null ? null : instance.Document,
                    Token = component.Token,
                    Span = component.Span
                });
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.Declarations.Declarations.Length)
        {
            XsdDeclarationDefinition declaration =
                this.Declarations.Declarations[index];
            XsdDeclarationTypeUse? use = declaration.TypeUse;
            if (use != null && use.Kind == "complex" && use.Anonymous
                && use.Token != null && this.FindTypeByToken(use.Token) == null)
            {
                this.Types.Add(new XsdComplexTypeDefinition
                {
                    StableId = use.StableId,
                    Name = "",
                    NamespaceUri = declaration.SchemaNamespaceUri,
                    Anonymous = true,
                    DocumentInstance = declaration.DocumentInstance,
                    Document = declaration.Document,
                    Token = use.Token,
                    Span = use.Span
                });
            }
            index = index + 1;
        }
    }

    internal bool MapGroup(XsdModelGroupDefinition group)
    {
        if (group.Mapped)
        {
            return true;
        }
        if (group.Document == null || group.Token == null)
        {
            return false;
        }
        if (this.ContainsActive(group.StableId))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.UnknownGroupReference,
                "An XML Schema group reference cycle cannot be expanded.",
                group.Span);
            return false;
        }
        this.Active.Add(group.StableId);
        if (group.AttributeGroup)
        {
            group.AttributeUses = this.CollectAttributeUses(
                group.Document,
                group.Token,
                []);
            group.ReferencedAttributeGroups = this.CollectAttributeGroupNames(
                group.Document,
                group.Token);
            group.AttributeUses = this.ExpandAttributeGroups(
                group.AttributeUses,
                group.ReferencedAttributeGroups,
                group.Span);
            group.Mapped = true;
            this.Active.RemoveAt(this.Active.Count - 1);
            return true;
        }
        XerXmlToken? content = this.FindModelGroupChild(
            group.Document,
            group.Token);
        if (content == null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidParticle,
                "An XML Schema model group requires sequence, choice, or all content.",
                group.Span);
            this.Active.RemoveAt(this.Active.Count - 1);
            return false;
        }
        group.Kind = content.LocalName;
        group.Particle = this.MapParticle(
            group.DocumentInstance,
            group.Document,
            content,
            false,
            group.StableId + ":particle");
        group.AsnTypeDefinition = group.Kind == "all"
            ? ""
            : group.Particle == null ? "" : group.Particle.AsnTypeDefinition;
        group.XerInstructions = group.Particle == null
            ? []
            : group.Particle.XerInstructions;
        group.Mapped = true;
        this.Active.RemoveAt(this.Active.Count - 1);
        return group.Particle != null;
    }

    internal bool MapType(XsdComplexTypeDefinition type)
    {
        if (type.Mapped)
        {
            return true;
        }
        if (type.Document == null || type.Token == null)
        {
            return false;
        }
        if (this.ContainsActive(type.StableId))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.UnknownComplexTypeBase,
                "An XML Schema complex-type derivation cycle cannot be expanded.",
                type.Span);
            return false;
        }
        this.Active.Add(type.StableId);
        type.Abstract = this.BooleanAttribute(type.Token, "abstract");
        type.Mixed = this.BooleanAttribute(type.Token, "mixed");
        XerXmlToken source = type.Token;
        XerXmlToken? simpleContent = this.SimpleMapper.FindDirectChild(
            type.Document,
            type.Token,
            "simpleContent");
        XerXmlToken? complexContent = this.SimpleMapper.FindDirectChild(
            type.Document,
            type.Token,
            "complexContent");
        if (complexContent != null
            && new XsdDocumentLoader().FindAttribute(
                complexContent,
                "",
                "mixed") != null)
        {
            type.Mixed = this.BooleanAttribute(complexContent, "mixed");
        }
        if (simpleContent != null || complexContent != null)
        {
            XerXmlToken container = simpleContent == null
                ? complexContent ?? type.Token
                : simpleContent;
            XerXmlToken? derivation = this.SimpleMapper.FindDirectChild(
                type.Document,
                container,
                "extension");
            if (derivation == null)
            {
                derivation = this.SimpleMapper.FindDirectChild(
                    type.Document,
                    container,
                    "restriction");
            }
            if (derivation == null)
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidComplexType,
                    "XML Schema simpleContent or complexContent requires extension or restriction.",
                    type.Span);
                this.Active.RemoveAt(this.Active.Count - 1);
                return false;
            }
            source = derivation;
            type.Derivation = derivation.LocalName;
            this.ResolveBase(type, derivation, simpleContent != null);
            if (simpleContent != null)
            {
                type.ContentKind = "simple";
                if (type.Derivation == "restriction"
                    && type.SimpleContentType != null)
                {
                    type.SimpleContentType = this.RestrictSimpleContent(
                        type,
                        derivation,
                        type.SimpleContentType);
                }
            }
        }
        XsdParticleDefinition? declaredParticle = type.ContentKind == "simple"
            ? null
            : this.MapContentParticle(type, source);
        XsdDeclarationDefinition[] declaredAttributes =
            this.CollectAttributeUses(type.Document, source, []);
        string[] attributeGroups = this.CollectAttributeGroupNames(
            type.Document,
            source);
        declaredAttributes = this.ExpandAttributeGroups(
            declaredAttributes,
            attributeGroups,
            type.Span);
        type.AttributeGroupReferences = attributeGroups;
        if (type.BaseComplexType != null)
        {
            this.MapType(type.BaseComplexType);
            if (type.Derivation == "extension" && type.BaseComplexType.Mixed)
            {
                type.Mixed = true;
            }
            type.AttributeUses = this.MergeAttributeUses(
                type.BaseComplexType.AttributeUses,
                declaredAttributes);
            if (type.ContentKind != "simple")
            {
                type.Particle = type.Derivation == "extension"
                    ? this.CombineParticles(
                        type.BaseComplexType.Particle,
                        declaredParticle,
                        type)
                    : declaredParticle;
            }
            if (type.ContentKind == "simple"
                && type.SimpleContentType == null)
            {
                type.SimpleContentType =
                    type.BaseComplexType.SimpleContentType;
            }
        }
        else
        {
            type.AttributeUses = declaredAttributes;
            type.Particle = declaredParticle;
        }
        type.AttributeUses = this.SortAttributeUses(type.AttributeUses);
        if (type.ContentKind.Length == 0)
        {
            type.ContentKind = type.Particle == null ? "empty" : "particle";
        }
        if (type.Particle != null && type.Particle.Kind == "all")
        {
            type.XerInstructions = type.Particle.XerInstructions;
        }
        type.AsnTypeDefinition = this.RenderComplexType(type);
        type.Mapped = true;
        this.Active.RemoveAt(this.Active.Count - 1);
        return true;
    }

    internal void ResolveBase(
        XsdComplexTypeDefinition type,
        XerXmlToken derivation,
        bool simpleContent)
    {
        XerXmlAttribute? baseAttribute = new XsdDocumentLoader().FindAttribute(
            derivation,
            "",
            "base");
        if (baseAttribute == null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.UnknownComplexTypeBase,
                "An XML Schema derivation requires a base type.",
                type.Span);
            return;
        }
        string[] name = this.SimpleMapper.ResolveQualifiedName(
            derivation,
            baseAttribute.Value);
        if (name.Length == 0)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.UnknownComplexTypeBase,
                "The XML Schema derivation base QName cannot be resolved.",
                type.Span);
            return;
        }
        type.BaseTypeNamespace = name[0];
        type.BaseTypeName = name[1];
        if (name[0] == XsdDocumentLoader.SchemaNamespace
            && name[1] == "anyType" && !simpleContent)
        {
            return;
        }
        XsdComplexTypeDefinition? complex = this.FindDerivationBaseType(
            type,
            name[0],
            name[1]);
        if (complex != null)
        {
            type.BaseComplexType = complex;
            if (simpleContent)
            {
                this.MapType(complex);
                type.SimpleContentType = complex.SimpleContentType;
            }
            return;
        }
        XsdSimpleTypeUse? simple = this.SimpleMapper.ResolveNamedTypeUse(
            derivation,
            baseAttribute.Value);
        if (simpleContent && simple != null)
        {
            type.SimpleContentType = simple;
            return;
        }
        this.AddDiagnostic(
            XsdDiagnosticCodes.UnknownComplexTypeBase,
            "The XML Schema complex-type base cannot be resolved.",
            type.Span);
    }

    internal XsdComplexTypeDefinition? FindDerivationBaseType(
        XsdComplexTypeDefinition owner,
        string namespaceUri,
        string name)
    {
        XsdComponentDefinition? component = owner.Component;
        if (component != null
            && component.Redefinition
            && owner.NamespaceUri == namespaceUri
            && owner.Name == name)
        {
            int edgeIndex = 0;
            while (edgeIndex < this.Graph.Edges.Length)
            {
                XsdSchemaGraphEdge edge = this.Graph.Edges[edgeIndex];
                if (edge.Kind == "redefine"
                    && edge.SourceInstanceId == component.SourceInstanceId)
                {
                    int typeIndex = 0;
                    while (typeIndex < this.Types.Count)
                    {
                        XsdComplexTypeDefinition candidate =
                            this.Types[typeIndex];
                        XsdComponentDefinition? candidateComponent =
                            candidate.Component;
                        if (candidateComponent != null
                            && !candidateComponent.Redefinition
                            && candidateComponent.SourceInstanceId
                                == edge.TargetInstanceId
                            && candidate.NamespaceUri == namespaceUri
                            && candidate.Name == name)
                        {
                            return candidate;
                        }
                        typeIndex = typeIndex + 1;
                    }
                }
                edgeIndex = edgeIndex + 1;
            }
        }
        return this.FindType(namespaceUri, name);
    }

    internal XsdSimpleTypeUse RestrictSimpleContent(
        XsdComplexTypeDefinition owner,
        XerXmlToken restriction,
        XsdSimpleTypeUse baseUse)
    {
        XsdSimpleTypeDefinition definition = new XsdSimpleTypeDefinition
        {
            StableId = owner.StableId + ":simple-content",
            Name = "",
            NamespaceUri = owner.NamespaceUri,
            BuiltInBaseName = baseUse.BuiltInTypeName,
            Variety = baseUse.Definition == null
                ? XsdSimpleTypeVariety.Atomic
                : baseUse.Definition.Variety,
            ItemType = baseUse.Definition == null
                ? null
                : baseUse.Definition.ItemType,
            UnionAlternatives = baseUse.Definition == null
                ? []
                : baseUse.Definition.UnionAlternatives,
            BaseDefinition = baseUse.Definition,
            EffectiveFacets = baseUse.Definition == null
                ? []
                : baseUse.Definition.EffectiveFacets,
            Document = owner.Document,
            Token = restriction,
            Anonymous = true,
            Span = owner.Span
        };
        if (owner.Document == null)
        {
            return baseUse;
        }
        definition.Facets = this.SimpleMapper.CollectFacets(
            owner.Document,
            restriction);
        definition.EffectiveFacets = this.SimpleMapper.MergeFacetSets(
            definition.EffectiveFacets,
            definition.Facets);
        string whitespace = this.SimpleMapper.EffectiveWhitespace(
            definition.BuiltInBaseName,
            definition.EffectiveFacets);
        definition.EnumerationItems = this.SimpleMapper.MapEnumeration(
            definition,
            whitespace);
        definition.Enumerated = definition.EnumerationItems.Length > 0
            && (this.SimpleMapper.IsStringDerived(definition.BuiltInBaseName)
                || this.SimpleMapper.IsIntegerDerived(
                    definition.BuiltInBaseName));
        definition.Constraints = this.SimpleMapper.MapConstraints(
            definition,
            whitespace);
        definition.XerInstructions = this.SimpleMapper.MapXerInstructions(
            definition,
            whitespace);
        definition.AsnTypeDefinition = definition.Enumerated
            ? this.SimpleMapper.RenderEnumeration(definition)
            : this.SimpleMapper.RenderConstrainedType(
                baseUse.AsnTypeDefinition,
                definition.Constraints);
        this.SimpleMapper.Types.Add(definition);
        return new XsdSimpleTypeUse
        {
            StableId = definition.StableId,
            Name = "",
            NamespaceUri = owner.NamespaceUri,
            BuiltInTypeName = definition.BuiltInBaseName,
            AsnTypeDefinition = definition.AsnTypeDefinition,
            Anonymous = true,
            Definition = definition,
            XerInstructions = definition.XerInstructions
        };
    }

    internal XsdParticleDefinition? MapContentParticle(
        XsdComplexTypeDefinition type,
        XerXmlToken source)
    {
        if (type.Document == null)
        {
            return null;
        }
        XerXmlToken? content = this.FindParticleChild(type.Document, source);
        XsdParticleDefinition? particle = content == null
            ? null
            : this.MapParticle(
                type.DocumentInstance,
                type.Document,
                content,
                true,
                type.StableId + ":content");
        if (particle != null && particle.Kind == "all"
            && particle.MinimumOccurs.IsZero)
        {
            this.ApplyOptionalAllContent(particle);
        }
        return particle;
    }

    internal void ApplyOptionalAllContent(XsdParticleDefinition particle)
    {
        int index = 0;
        while (index < particle.Children.Length)
        {
            XsdParticleDefinition child = particle.Children[index];
            if (child.MinimumOccurs == System.Numerics.BigInteger.One)
            {
                child.MinimumOccurs = System.Numerics.BigInteger.Zero;
                child.Optional = true;
                string core = child.Declaration == null
                    ? child.AsnTypeDefinition
                    : this.ResolveDeclarationCore(child.Declaration);
                child.NamedTypeDefinition = this.RenderParticleUse(
                    child,
                    core,
                    true,
                    child.Identifier);
            }
            index = index + 1;
        }
        particle.AsnTypeDefinition = this.RenderModelGroup(particle);
    }

    internal XsdParticleDefinition? MapParticle(
        XsdDocumentInstance? instance,
        XsdLoadedDocument document,
        XerXmlToken token,
        bool sequenceComponent,
        string stableId)
    {
        XsdParticleDefinition particle = new XsdParticleDefinition
        {
            StableId = stableId,
            Kind = token.LocalName,
            Token = token,
            Span = this.TokenSpan(document, token)
        };
        this.MapParticleOccurrence(particle, token);
        if (particle.Repeated)
        {
            particle.XerInstructions =
            [
                new AsnEncodingInstruction
                {
                    Name = "UNTAGGED",
                    Kind = AsnInstructionKind.Untagged,
                    Span = particle.Span
                }
            ];
        }
        if (token.LocalName == "element")
        {
            particle.Declaration = this.FindDeclaration(token);
            if (particle.Declaration == null)
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidParticle,
                    "An element particle has no mapped declaration.",
                    particle.Span);
                return null;
            }
            particle.Name = particle.Declaration.Name;
            particle.Identifier = particle.Declaration.Identifier;
            string core = this.ResolveDeclarationCore(particle.Declaration);
            particle.AsnTypeDefinition = core;
            particle.NamedTypeDefinition = this.RenderParticleUse(
                particle,
                core,
                sequenceComponent,
                particle.Identifier);
            return particle;
        }
        if (token.LocalName == "group")
        {
            XerXmlAttribute? reference = new XsdDocumentLoader().FindAttribute(
                token,
                "",
                "ref");
            string[] name = reference == null
                ? []
                : this.SimpleMapper.ResolveQualifiedName(token, reference.Value);
            XsdModelGroupDefinition? group = name.Length == 0
                ? null
                : this.FindGroup(name[0], name[1], false);
            if (group == null || !this.MapGroup(group))
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.UnknownGroupReference,
                    "An XML Schema model-group reference cannot be resolved.",
                    particle.Span);
                return null;
            }
            particle.ReferencedGroup = group;
            particle.Name = group.Name;
            particle.Identifier = new XsdDeclarationMapper().ToIdentifier(
                group.Name);
            particle.AsnTypeDefinition = group.Kind == "all"
                && group.Particle != null
                ? group.Particle.AsnTypeDefinition
                : group.TypeReferenceName;
            particle.NamedTypeDefinition = this.RenderParticleUse(
                particle,
                particle.AsnTypeDefinition,
                sequenceComponent,
                particle.Identifier);
            return particle;
        }
        if (token.LocalName == "any")
        {
            particle.Name = "elem";
            particle.Identifier = "elem";
            particle.PendingWildcardMapping = true;
            particle.AsnTypeDefinition = "XSDv2.String";
            particle.NamedTypeDefinition = this.RenderParticleUse(
                particle,
                particle.AsnTypeDefinition,
                sequenceComponent,
                particle.Identifier);
            return particle;
        }
        if (token.LocalName != "sequence" && token.LocalName != "choice"
            && token.LocalName != "all")
        {
            return null;
        }
        XerXmlToken[] children = this.SimpleMapper.GetDirectChildren(
            document,
            token);
        System.Collections.Generic.List<XsdParticleDefinition> mapped =
            new System.Collections.Generic.List<XsdParticleDefinition>();
        int index = 0;
        while (index < children.Length)
        {
            if (this.IsParticleToken(children[index]))
            {
                XsdParticleDefinition? child = this.MapParticle(
                    instance,
                    document,
                    children[index],
                    token.LocalName == "sequence" || token.LocalName == "all",
                    stableId + ":" + index.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
                if (child != null)
                {
                    mapped.Add(child);
                }
            }
            index = index + 1;
        }
        particle.Children = mapped.ToArray();
        particle.Name = token.LocalName;
        particle.Identifier = token.LocalName;
        particle.PointlessSequence = token.LocalName == "sequence"
            && particle.MinimumOccurs == System.Numerics.BigInteger.One
            && !particle.MaximumOccursUnbounded
            && particle.MaximumOccurs == System.Numerics.BigInteger.One
            && sequenceComponent;
        particle.AsnTypeDefinition = this.RenderModelGroup(particle);
        particle.XerInstructions =
        [
            new AsnEncodingInstruction
            {
                Name = token.LocalName == "all" ? "USE-ORDER" : "UNTAGGED",
                Kind = token.LocalName == "all"
                    ? AsnInstructionKind.UseOrder
                    : AsnInstructionKind.Untagged,
                Span = particle.Span
            }
        ];
        particle.NamedTypeDefinition = particle.PointlessSequence
            ? this.RenderComponents(particle.Children)
            : this.RenderParticleUse(
                particle,
                particle.AsnTypeDefinition,
                sequenceComponent,
                particle.Identifier);
        return particle;
    }

    internal void MapParticleOccurrence(
        XsdParticleDefinition particle,
        XerXmlToken token)
    {
        XerXmlAttribute? minimum = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "minOccurs");
        XerXmlAttribute? maximum = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "maxOccurs");
        string minimumText = minimum == null ? "1" : minimum.Value;
        string maximumText = maximum == null ? "1" : maximum.Value;
        if (!this.SimpleMapper.IsUnsignedInteger(minimumText)
            || maximumText != "unbounded"
                && !this.SimpleMapper.IsUnsignedInteger(maximumText))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidParticle,
                "A particle occurrence range is invalid.",
                particle.Span);
            minimumText = "1";
            maximumText = "1";
        }
        particle.MinimumOccurs = this.SimpleMapper.ParseBigInteger(minimumText);
        particle.MaximumOccursUnbounded = maximumText == "unbounded";
        particle.MaximumOccurs = particle.MaximumOccursUnbounded
            ? System.Numerics.BigInteger.Zero
            : this.SimpleMapper.ParseBigInteger(maximumText);
        if (!particle.MaximumOccursUnbounded
            && particle.MinimumOccurs > particle.MaximumOccurs)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidParticle,
                "A particle minOccurs cannot exceed maxOccurs.",
                particle.Span);
        }
        particle.Optional = particle.MinimumOccurs.IsZero
            && !particle.MaximumOccursUnbounded
            && particle.MaximumOccurs == System.Numerics.BigInteger.One;
        particle.Repeated = particle.MaximumOccursUnbounded
            || particle.MaximumOccurs > System.Numerics.BigInteger.One;
        AsnConstraint? size = this.CreateOccurrenceConstraint(particle);
        particle.Constraints = size == null ? [] : [size];
    }

    internal AsnConstraint? CreateOccurrenceConstraint(
        XsdParticleDefinition particle)
    {
        if (!particle.Repeated
            || particle.MinimumOccurs.IsZero
                && particle.MaximumOccursUnbounded)
        {
            return null;
        }
        string lower = particle.MinimumOccurs.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        string upper = particle.MaximumOccursUnbounded
            ? "MAX"
            : particle.MaximumOccurs.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        string text = lower == upper
            ? "(SIZE(" + lower + "))"
            : "(SIZE(" + lower + ".." + upper + "))";
        return new AsnConstraint
        {
            Kind = AsnConstraintKind.Size,
            Elements =
            [
                new AsnConstraintElement
                {
                    Kind = AsnConstraintKind.ValueRange,
                    HasLowerBound = true,
                    LowerBound = particle.MinimumOccurs,
                    HasUpperBound = !particle.MaximumOccursUnbounded,
                    UpperBound = particle.MaximumOccurs,
                    UpperEndpointUnbounded = particle.MaximumOccursUnbounded,
                    Text = text
                }
            ],
            Span = particle.Span
        };
    }

    internal string RenderParticleUse(
        XsdParticleDefinition particle,
        string core,
        bool sequenceComponent,
        string identifier)
    {
        if (!particle.MaximumOccursUnbounded
            && particle.MaximumOccurs.IsZero)
        {
            return "";
        }
        if (!particle.Repeated && !(particle.Optional && !sequenceComponent))
        {
            return identifier + " " + core
                + (particle.Optional ? " OPTIONAL" : "");
        }
        string size = particle.Constraints.Length == 0
            ? ""
            : " " + particle.Constraints[0].Elements[0].Text;
        if (particle.Optional && !sequenceComponent)
        {
            size = " (SIZE(0..1))";
        }
        return identifier + "-list SEQUENCE" + size + " OF "
            + identifier + " " + core;
    }

    internal string RenderModelGroup(XsdParticleDefinition particle)
    {
        if (particle.Kind == "choice" && particle.Children.Length == 0)
        {
            return "NULL";
        }
        if (particle.Kind == "all")
        {
            string order = "order SEQUENCE OF ENUMERATED {";
            int orderIndex = 0;
            while (orderIndex < particle.Children.Length)
            {
                if (orderIndex > 0)
                {
                    order = order + ", ";
                }
                order = order + particle.Children[orderIndex].Identifier;
                orderIndex = orderIndex + 1;
            }
            order = order + "}";
            string components = this.RenderComponents(particle.Children);
            return "SEQUENCE {" + order
                + (components.Length == 0 ? "" : ", " + components) + "}";
        }
        return (particle.Kind == "choice" ? "CHOICE" : "SEQUENCE")
            + " {" + this.RenderComponents(particle.Children) + "}";
    }

    internal string RenderComponents(XsdParticleDefinition[] children)
    {
        string result = "";
        int index = 0;
        while (index < children.Length)
        {
            string component = children[index].NamedTypeDefinition;
            if (children[index].PointlessSequence)
            {
                component = this.RenderComponents(children[index].Children);
            }
            if (component.Length > 0)
            {
                if (result.Length > 0)
                {
                    result = result + ", ";
                }
                result = result + component;
            }
            index = index + 1;
        }
        return result;
    }

    internal string RenderComplexType(XsdComplexTypeDefinition type)
    {
        string components = type.Mixed
            ? "embed-values SEQUENCE OF XSDv2.String"
            : "";
        components = this.AppendComponent(
            components,
            this.RenderAttributes(type.AttributeUses));
        if (type.AttributeWildcard != null)
        {
            components = this.AppendComponent(
                components,
                "attr " + type.AttributeWildcard.AsnTypeDefinition);
        }
        if (type.ContentKind == "simple" && type.SimpleContentType != null)
        {
            components = this.AppendComponent(
                components,
                "base " + type.SimpleContentType.AsnTypeDefinition);
        }
        else if (type.Particle != null)
        {
            string particleComponents = type.Particle.Kind == "all"
                ? this.RenderAllComponents(type.Particle)
                : type.Particle.Kind == "sequence"
                && !type.Particle.Repeated && !type.Particle.Optional
                    ? this.RenderComponents(type.Particle.Children)
                    : type.Particle.NamedTypeDefinition;
            components = this.AppendComponent(components, particleComponents);
        }
        return "SEQUENCE {" + components + "}";
    }

    internal string RenderAllComponents(XsdParticleDefinition particle)
    {
        string order = "order SEQUENCE OF ENUMERATED {";
        int index = 0;
        while (index < particle.Children.Length)
        {
            if (index > 0)
            {
                order = order + ", ";
            }
            order = order + particle.Children[index].Identifier;
            index = index + 1;
        }
        return order + "}, " + this.RenderComponents(particle.Children);
    }

    internal string RenderAttributes(XsdDeclarationDefinition[] attributes)
    {
        string result = "";
        int index = 0;
        while (index < attributes.Length)
        {
            XsdDeclarationDefinition attribute = attributes[index];
            if (!attribute.Prohibited)
            {
                result = this.AppendComponent(
                    result,
                    attribute.Identifier + " "
                        + attribute.OccurrenceTypeDefinition);
            }
            index = index + 1;
        }
        return result;
    }

    internal string AppendComponent(string existing, string addition)
    {
        if (addition.Length == 0)
        {
            return existing;
        }
        return existing.Length == 0 ? addition : existing + ", " + addition;
    }

    internal XsdParticleDefinition? CombineParticles(
        XsdParticleDefinition? baseParticle,
        XsdParticleDefinition? declaredParticle,
        XsdComplexTypeDefinition owner)
    {
        if (baseParticle == null)
        {
            return declaredParticle;
        }
        if (declaredParticle == null)
        {
            return baseParticle;
        }
        XsdParticleDefinition combined = new XsdParticleDefinition
        {
            StableId = owner.StableId + ":extended-content",
            Kind = "sequence",
            Name = "sequence",
            Identifier = "sequence",
            MinimumOccurs = System.Numerics.BigInteger.One,
            MaximumOccurs = System.Numerics.BigInteger.One,
            Children = [baseParticle, declaredParticle],
            Span = owner.Span
        };
        combined.AsnTypeDefinition = this.RenderModelGroup(combined);
        combined.NamedTypeDefinition = this.RenderComponents(combined.Children);
        return combined;
    }

    internal XsdDeclarationDefinition[] CollectAttributeUses(
        XsdLoadedDocument document,
        XerXmlToken owner,
        XsdDeclarationDefinition[] inherited)
    {
        System.Collections.Generic.List<XsdDeclarationDefinition> result =
            new System.Collections.Generic.List<XsdDeclarationDefinition>();
        int inheritedIndex = 0;
        while (inheritedIndex < inherited.Length)
        {
            result.Add(inherited[inheritedIndex]);
            inheritedIndex = inheritedIndex + 1;
        }
        XerXmlToken[] children = this.SimpleMapper.GetDirectChildren(
            document,
            owner);
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].NamespaceUri
                    == XsdDocumentLoader.SchemaNamespace
                && children[index].LocalName == "attribute")
            {
                XsdDeclarationDefinition? declaration =
                    this.FindDeclaration(children[index]);
                if (declaration != null)
                {
                    result.Add(declaration);
                }
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal string[] CollectAttributeGroupNames(
        XsdLoadedDocument document,
        XerXmlToken owner)
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        XerXmlToken[] children = this.SimpleMapper.GetDirectChildren(
            document,
            owner);
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].NamespaceUri
                    == XsdDocumentLoader.SchemaNamespace
                && children[index].LocalName == "attributeGroup")
            {
                XerXmlAttribute? reference =
                    new XsdDocumentLoader().FindAttribute(
                        children[index],
                        "",
                        "ref");
                if (reference != null)
                {
                    string[] name = this.SimpleMapper.ResolveQualifiedName(
                        children[index],
                        reference.Value);
                    if (name.Length > 0)
                    {
                        result.Add(name[0] + "\n" + name[1]);
                    }
                }
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal XsdDeclarationDefinition[] ExpandAttributeGroups(
        XsdDeclarationDefinition[] direct,
        string[] references,
        AsnSourceSpan? span)
    {
        System.Collections.Generic.List<XsdDeclarationDefinition> result =
            new System.Collections.Generic.List<XsdDeclarationDefinition>();
        int index = 0;
        while (index < references.Length)
        {
            string[] name = this.DecodeGroupName(references[index]);
            XsdModelGroupDefinition? group = name.Length == 0
                ? null
                : this.FindGroup(name[0], name[1], true);
            if (group == null || !this.MapGroup(group))
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.UnknownGroupReference,
                    "An XML Schema attribute-group reference cannot be resolved.",
                    span);
            }
            else
            {
                int item = 0;
                while (item < group.AttributeUses.Length)
                {
                    result.Add(group.AttributeUses[item]);
                    item = item + 1;
                }
            }
            index = index + 1;
        }
        index = 0;
        while (index < direct.Length)
        {
            result.Add(direct[index]);
            index = index + 1;
        }
        return result.ToArray();
    }

    internal string[] DecodeGroupName(string value)
    {
        int separator = value.IndexOf('\n');
        return separator < 0
            ? []
            : [value.Substring(0, separator), value.Substring(separator + 1)];
    }

    internal string[] ResolveGroupName(string text)
    {
        int index = 0;
        while (index < this.Graph.DocumentInstances.Length)
        {
            XsdLoadedDocument? document =
                this.Graph.DocumentInstances[index].Document;
            if (document != null && document.SchemaToken != null)
            {
                string[] name = this.SimpleMapper.ResolveQualifiedName(
                    document.SchemaToken,
                    text);
                if (name.Length > 0)
                {
                    return name;
                }
            }
            index = index + 1;
        }
        return [];
    }

    internal XsdDeclarationDefinition[] MergeAttributeUses(
        XsdDeclarationDefinition[] inherited,
        XsdDeclarationDefinition[] declared)
    {
        System.Collections.Generic.List<XsdDeclarationDefinition> result =
            new System.Collections.Generic.List<XsdDeclarationDefinition>();
        int index = 0;
        while (index < inherited.Length)
        {
            XsdDeclarationDefinition item = inherited[index];
            XsdDeclarationDefinition? replacement = this.FindAttributeByName(
                declared,
                item.NamespaceUri,
                item.Name);
            if (replacement == null)
            {
                result.Add(item);
            }
            else if (!replacement.Prohibited)
            {
                result.Add(replacement);
            }
            index = index + 1;
        }
        index = 0;
        while (index < declared.Length)
        {
            if (this.FindAttributeByName(
                inherited,
                declared[index].NamespaceUri,
                declared[index].Name) == null
                && !declared[index].Prohibited)
            {
                result.Add(declared[index]);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal XsdDeclarationDefinition[] SortAttributeUses(
        XsdDeclarationDefinition[] values)
    {
        XsdDeclarationDefinition[] result =
            new XsdDeclarationDefinition[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            int position = index;
            while (position > 0
                && this.CompareAttributeUses(
                    result[position - 1],
                    result[position]) > 0)
            {
                XsdDeclarationDefinition earlier = result[position - 1];
                result[position - 1] = result[position];
                result[position] = earlier;
                position = position - 1;
            }
            index = index + 1;
        }
        return result;
    }

    internal int CompareAttributeUses(
        XsdDeclarationDefinition first,
        XsdDeclarationDefinition second)
    {
        int namespaceComparison = string.CompareOrdinal(
            first.NamespaceUri,
            second.NamespaceUri);
        return namespaceComparison == 0
            ? string.CompareOrdinal(first.Name, second.Name)
            : namespaceComparison;
    }

    internal XsdDeclarationDefinition? FindAttributeByName(
        XsdDeclarationDefinition[] values,
        string namespaceUri,
        string name)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].NamespaceUri == namespaceUri
                && values[index].Name == name)
            {
                return values[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XerXmlToken? FindModelGroupChild(
        XsdLoadedDocument document,
        XerXmlToken owner)
    {
        XerXmlToken[] children = this.SimpleMapper.GetDirectChildren(
            document,
            owner);
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].NamespaceUri
                    == XsdDocumentLoader.SchemaNamespace
                && (children[index].LocalName == "sequence"
                    || children[index].LocalName == "choice"
                    || children[index].LocalName == "all"))
            {
                return children[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XerXmlToken? FindParticleChild(
        XsdLoadedDocument document,
        XerXmlToken owner)
    {
        XerXmlToken[] children = this.SimpleMapper.GetDirectChildren(
            document,
            owner);
        int index = 0;
        while (index < children.Length)
        {
            if (this.IsParticleToken(children[index]))
            {
                return children[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal bool IsParticleToken(XerXmlToken token)
    {
        return token.NamespaceUri == XsdDocumentLoader.SchemaNamespace
            && (token.LocalName == "element" || token.LocalName == "group"
                || token.LocalName == "sequence" || token.LocalName == "choice"
                || token.LocalName == "all" || token.LocalName == "any");
    }

    internal string ResolveDeclarationCore(
        XsdDeclarationDefinition declaration)
    {
        XsdDeclarationTypeUse? use = declaration.TypeUse;
        if (use != null && use.Kind == "complex")
        {
            XsdComplexTypeDefinition? complex = use.Anonymous
                && use.Token != null
                ? this.FindTypeByToken(use.Token)
                : this.FindType(use.NamespaceUri, use.Name);
            if (complex != null)
            {
                this.MapType(complex);
                use.AsnTypeDefinition = complex.AsnTypeDefinition;
                use.MappedComplexType = complex;
                use.PendingComplexMapping = false;
                return complex.AsnTypeDefinition;
            }
        }
        return declaration.AsnTypeDefinition;
    }

    internal void RefreshDeclarations()
    {
        XsdDeclarationMapper renderer = new XsdDeclarationMapper();
        int index = 0;
        while (index < this.Declarations.Declarations.Length)
        {
            XsdDeclarationDefinition declaration =
                this.Declarations.Declarations[index];
            XsdDeclarationTypeUse? use = declaration.TypeUse;
            if (use != null && use.Kind == "complex")
            {
                XsdComplexTypeDefinition? complex = use.Anonymous
                    && use.Token != null
                    ? this.FindTypeByToken(use.Token)
                    : this.FindType(use.NamespaceUri, use.Name);
                if (complex != null && this.MapType(complex))
                {
                    use.AsnTypeDefinition = complex.AsnTypeDefinition;
                    use.MappedComplexType = complex;
                    use.PendingComplexMapping = false;
                    renderer.RenderDeclaration(declaration);
                }
            }
            index = index + 1;
        }
    }

    internal XsdDeclarationDefinition? FindDeclaration(XerXmlToken token)
    {
        int index = 0;
        while (index < this.Declarations.Declarations.Length)
        {
            if (this.Declarations.Declarations[index].Token == token)
            {
                return this.Declarations.Declarations[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdComplexTypeDefinition? FindType(
        string namespaceUri,
        string name)
    {
        int index = 0;
        while (index < this.Types.Count)
        {
            if (this.Types[index].NamespaceUri == namespaceUri
                && this.Types[index].Name == name)
            {
                return this.Types[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdComplexTypeDefinition? FindTypeByToken(XerXmlToken token)
    {
        int index = 0;
        while (index < this.Types.Count)
        {
            if (this.Types[index].Token == token)
            {
                return this.Types[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdModelGroupDefinition? FindGroup(
        string namespaceUri,
        string name,
        bool attributeGroup)
    {
        int index = 0;
        while (index < this.Groups.Count)
        {
            if (this.Groups[index].NamespaceUri == namespaceUri
                && this.Groups[index].Name == name
                && this.Groups[index].AttributeGroup == attributeGroup)
            {
                return this.Groups[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdDocumentInstance? FindInstance(string stableId)
    {
        int index = 0;
        while (index < this.Graph.DocumentInstances.Length)
        {
            if (this.Graph.DocumentInstances[index].StableId == stableId)
            {
                return this.Graph.DocumentInstances[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal bool BooleanAttribute(XerXmlToken token, string name)
    {
        XerXmlAttribute? value = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            name);
        return value != null && (value.Value == "true" || value.Value == "1");
    }

    internal bool ContainsActive(string stableId)
    {
        int index = 0;
        while (index < this.Active.Count)
        {
            if (this.Active[index] == stableId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal AsnSourceSpan? TokenSpan(
        XsdLoadedDocument document,
        XerXmlToken token)
    {
        return document.Input == null
            ? null
            : new XsdDocumentLoader().CreateTokenSpan(document.Input, token);
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

    internal XsdComplexTypeMappingResult CreateResult()
    {
        System.Collections.Generic.List<XsdModelGroupDefinition> modelGroups =
            new System.Collections.Generic.List<XsdModelGroupDefinition>();
        System.Collections.Generic.List<XsdModelGroupDefinition> attributeGroups =
            new System.Collections.Generic.List<XsdModelGroupDefinition>();
        int index = 0;
        while (index < this.Groups.Count)
        {
            if (this.Groups[index].AttributeGroup)
            {
                attributeGroups.Add(this.Groups[index]);
            }
            else
            {
                modelGroups.Add(this.Groups[index]);
            }
            index = index + 1;
        }
        return new XsdComplexTypeMappingResult
        {
            Success = this.Diagnostics.Count == 0,
            Types = this.Types.ToArray(),
            ModelGroups = modelGroups.ToArray(),
            AttributeGroups = attributeGroups.ToArray(),
            Diagnostics = this.Diagnostics.ToArray()
        };
    }
}
