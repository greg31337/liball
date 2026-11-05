namespace asn1;

internal class XsdAdvancedMapper
{
    internal const string WildcardNamespace = "urn:oid:2.1.5.2.0.1";
    internal XsdSchemaGraphResult Graph = new XsdSchemaGraphResult();
    internal XsdSimpleTypeMappingResult SimpleTypes =
        new XsdSimpleTypeMappingResult();
    internal XsdDeclarationMappingResult Declarations =
        new XsdDeclarationMappingResult();
    internal XsdComplexTypeMappingResult ComplexTypes =
        new XsdComplexTypeMappingResult();
    internal AsnResourceLimits Limits = new AsnResourceLimits();
    internal XsdSimpleTypeMapper SimpleMapper = new XsdSimpleTypeMapper();
    internal XsdComplexTypeMapper ComplexMapper = new XsdComplexTypeMapper();
    internal System.Collections.Generic.List<XsdWildcardDefinition> Wildcards =
        new System.Collections.Generic.List<XsdWildcardDefinition>();
    internal System.Collections.Generic.List<XsdSubstitutionGroupDefinition>
        SubstitutionGroups =
            new System.Collections.Generic.List<XsdSubstitutionGroupDefinition>();
    internal System.Collections.Generic.List<XsdTypeSubstitutionDefinition>
        TypeSubstitutions =
            new System.Collections.Generic.List<XsdTypeSubstitutionDefinition>();
    internal System.Collections.Generic.List<AsnDiagnostic> Diagnostics =
        new System.Collections.Generic.List<AsnDiagnostic>();

    internal XsdAdvancedMappingResult MapAdvancedFeatures(
        XsdSchemaGraphResult graph,
        XsdSimpleTypeMappingResult simpleTypes,
        XsdDeclarationMappingResult declarations,
        XsdComplexTypeMappingResult complexTypes,
        AsnXsdImportOptions options)
    {
        if (graph == null || simpleTypes == null || declarations == null
            || complexTypes == null || options == null)
        {
            throw new System.ArgumentNullException("mapping input");
        }
        this.Graph = graph;
        this.SimpleTypes = simpleTypes;
        this.Declarations = declarations;
        this.ComplexTypes = complexTypes;
        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        this.SimpleMapper = new XsdSimpleTypeMapper
        {
            Graph = graph,
            Limits = this.Limits,
            Types = new System.Collections.Generic.List<XsdSimpleTypeDefinition>(
                simpleTypes.Types),
            Diagnostics = new System.Collections.Generic.List<AsnDiagnostic>()
        };
        this.ComplexMapper = new XsdComplexTypeMapper();
        this.Wildcards = new System.Collections.Generic.List<XsdWildcardDefinition>();
        this.SubstitutionGroups =
            new System.Collections.Generic.List<XsdSubstitutionGroupDefinition>();
        this.TypeSubstitutions =
            new System.Collections.Generic.List<XsdTypeSubstitutionDefinition>();
        this.Diagnostics = new System.Collections.Generic.List<AsnDiagnostic>();
        if (!complexTypes.Success)
        {
            return this.CreateResult();
        }
        this.MapMixedContentAndWildcards();
        this.MapSubstitutionGroups();
        this.MapTypeSubstitutions();
        this.ApplySubstitutionGroups();
        this.RefreshComplexTypes();
        this.RefreshDeclarations();
        this.ApplySubstitutionGroups();
        this.RefreshComplexTypes();
        this.RefreshDeclarations();
        return this.CreateResult();
    }

    internal void MapMixedContentAndWildcards()
    {
        int groupIndex = 0;
        while (groupIndex < this.ComplexTypes.AttributeGroups.Length)
        {
            XsdModelGroupDefinition group =
                this.ComplexTypes.AttributeGroups[groupIndex];
            if (group.Document != null && group.Token != null)
            {
                XerXmlToken? token = this.SimpleMapper.FindDirectChild(
                    group.Document,
                    group.Token,
                    "anyAttribute");
                if (token != null)
                {
                    group.AttributeWildcard = this.MapWildcard(
                        token,
                        group.NamespaceUri,
                        true,
                        group.Document);
                }
            }
            groupIndex = groupIndex + 1;
        }
        groupIndex = 0;
        while (groupIndex < this.ComplexTypes.ModelGroups.Length)
        {
            XsdModelGroupDefinition group =
                this.ComplexTypes.ModelGroups[groupIndex];
            System.Collections.Generic.List<XsdWildcardDefinition> mapped =
                new System.Collections.Generic.List<XsdWildcardDefinition>();
            this.MapParticleWildcards(
                group.Particle,
                group.Document,
                group.NamespaceUri,
                mapped);
            groupIndex = groupIndex + 1;
        }
        int index = 0;
        while (index < this.ComplexTypes.Types.Length)
        {
            XsdComplexTypeDefinition type = this.ComplexTypes.Types[index];
            if (type.Mixed)
            {
                this.AddInstruction(
                    type,
                    new AsnEncodingInstruction
                    {
                        Name = "EMBED-VALUES",
                        Kind = AsnInstructionKind.EmbedValues,
                        Span = type.Span
                    });
            }
            if (type.Document != null && type.Token != null)
            {
                XerXmlToken? anyAttribute = this.FindAnyAttribute(type);
                if (anyAttribute != null)
                {
                    type.AttributeWildcard = this.MapWildcard(
                        anyAttribute,
                        type.NamespaceUri,
                        true,
                        type.Document);
                }
                else
                {
                    type.AttributeWildcard = this.FindGroupAttributeWildcard(type);
                }
            }
            System.Collections.Generic.List<XsdWildcardDefinition> mapped =
                new System.Collections.Generic.List<XsdWildcardDefinition>();
            this.MapParticleWildcards(
                type.Particle,
                type.Document,
                type.NamespaceUri,
                mapped);
            type.Wildcards = mapped.ToArray();
            index = index + 1;
        }
        index = 0;
        while (index < this.ComplexTypes.Types.Length)
        {
            XsdComplexTypeDefinition type = this.ComplexTypes.Types[index];
            if (type.AttributeWildcard == null)
            {
                type.AttributeWildcard = this.FindGroupAttributeWildcard(type);
            }
            index = index + 1;
        }
    }

    internal void MapParticleWildcards(
        XsdParticleDefinition? particle,
        XsdLoadedDocument? document,
        string targetNamespace,
        System.Collections.Generic.List<XsdWildcardDefinition> mapped)
    {
        if (particle == null)
        {
            return;
        }
        if (particle.Kind == "any" && particle.Token != null
            && document != null)
        {
            XsdWildcardDefinition wildcard = this.MapWildcard(
                particle.Token,
                targetNamespace,
                false,
                document);
            particle.Wildcard = wildcard;
            particle.PendingWildcardMapping = false;
            particle.AsnTypeDefinition = wildcard.AsnTypeDefinition;
            particle.XerInstructions = wildcard.XerInstructions;
            particle.NamedTypeDefinition = this.ComplexMapper.RenderParticleUse(
                particle,
                wildcard.AsnTypeDefinition,
                true,
                particle.Identifier);
            mapped.Add(wildcard);
        }
        int index = 0;
        while (index < particle.Children.Length)
        {
            this.MapParticleWildcards(
                particle.Children[index],
                document,
                targetNamespace,
                mapped);
            index = index + 1;
        }
    }

    internal XsdWildcardDefinition MapWildcard(
        XerXmlToken token,
        string targetNamespace,
        bool attributeWildcard,
        XsdLoadedDocument document)
    {
        XerXmlAttribute? process = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "processContents");
        XerXmlAttribute? namespaceValue = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "namespace");
        string processContents = process == null ? "strict" : process.Value;
        if (processContents != "strict" && processContents != "lax"
            && processContents != "skip")
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidWildcard,
                "A wildcard processContents value must be strict, lax, or skip.",
                this.TokenSpan(document, token));
            processContents = "strict";
        }
        string namespaceConstraint = namespaceValue == null
            ? "##any"
            : namespaceValue.Value;
        string[] namespaceTokens = this.SimpleMapper.SplitXmlWhitespace(
            namespaceConstraint);
        if (namespaceTokens.Length == 0)
        {
            namespaceTokens = ["##any"];
        }
        string mode = attributeWildcard
            ? "ATTRIBUTES"
            : this.WildcardMappingMode(token, document, processContents);
        XsdWildcardDefinition result = new XsdWildcardDefinition
        {
            StableId = "x694:wildcard:" + token.ByteOffset.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            Kind = attributeWildcard ? "attribute" : "element",
            ProcessContents = processContents,
            MappingMode = mode,
            NamespaceConstraint = namespaceConstraint,
            NamespaceTokens = namespaceTokens,
            Token = token,
            Span = this.TokenSpan(document, token)
        };
        if (attributeWildcard)
        {
            result.AsnTypeDefinition = "SEQUENCE OF XSDv2.String";
            result.Constraints =
            [
                this.UserConstraint(
                    "Each item shall conform to AnyAttributeFormat.",
                    result.Span)
            ];
            result.XerInstructions =
            [
                this.WildcardInstruction(
                    AsnInstructionKind.AnyAttributes,
                    "ANY-ATTRIBUTES",
                    namespaceTokens,
                    result.Span)
            ];
        }
        else
        {
            this.MapElementWildcard(result, targetNamespace);
        }
        this.Wildcards.Add(result);
        return result;
    }

    internal string WildcardMappingMode(
        XerXmlToken token,
        XsdLoadedDocument document,
        string processContents)
    {
        XerXmlToken? annotation = this.SimpleMapper.FindDirectChild(
            document,
            token,
            "annotation");
        string mode = "";
        if (annotation != null)
        {
            int index = 0;
            while (index < annotation.Attributes.Length)
            {
                XerXmlAttribute attribute = annotation.Attributes[index];
                if (attribute.NamespaceUri == WildcardNamespace
                    && attribute.LocalName == "wildcard-mapping")
                {
                    mode = attribute.Value;
                    break;
                }
                index = index + 1;
            }
        }
        if (mode.Length == 0)
        {
            return processContents == "skip" ? "FI" : "CHOICE-FI";
        }
        if (mode != "CHOICE-FI" && mode != "CHOICE-UTF-8"
            && mode != "FI" && mode != "UTF-8")
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidWildcard,
                "The wildcard-mapping attribute has an unsupported value.",
                this.TokenSpan(document, token));
            return processContents == "skip" ? "FI" : "CHOICE-FI";
        }
        if (processContents == "skip"
            && (mode == "CHOICE-FI" || mode == "CHOICE-UTF-8"))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidWildcard,
                "A skip wildcard cannot use a CHOICE wildcard mapping.",
                this.TokenSpan(document, token));
        }
        return mode;
    }

    internal void MapElementWildcard(
        XsdWildcardDefinition wildcard,
        string targetNamespace)
    {
        AsnEncodingInstruction anyElement = this.WildcardInstruction(
            AsnInstructionKind.AnyElement,
            "ANY-ELEMENT",
            wildcard.NamespaceTokens,
            wildcard.Span);
        if (wildcard.MappingMode == "UTF-8")
        {
            wildcard.AsnTypeDefinition = "UTF8String";
            wildcard.Constraints =
            [
                this.UserConstraint(
                    "The value shall be a well-formed XML document encoded in UTF-8.",
                    wildcard.Span)
            ];
            wildcard.XerInstructions = [anyElement];
            return;
        }
        if (wildcard.MappingMode == "FI")
        {
            wildcard.AsnTypeDefinition = "OCTET STRING";
            wildcard.Constraints =
            [
                this.UserConstraint(
                    "The value shall be a well-formed fast infoset document.",
                    wildcard.Span)
            ];
            wildcard.XerInstructions = [anyElement];
            return;
        }
        XsdDeclarationDefinition[] allowed = this.AllowedGlobalElements(
            wildcard.NamespaceTokens,
            targetNamespace);
        wildcard.AllowedElements = allowed;
        wildcard.IncludesFallback = true;
        string choice = "CHOICE {";
        int index = 0;
        while (index < allowed.Length)
        {
            if (index > 0)
            {
                choice = choice + ", ";
            }
            choice = choice + allowed[index].Identifier + " "
                + allowed[index].TypeReferenceName;
            index = index + 1;
        }
        if (index > 0)
        {
            choice = choice + ", ";
        }
        choice = choice + "elem "
            + (wildcard.MappingMode == "CHOICE-UTF-8"
                ? "UTF8String"
                : "OCTET STRING") + "}";
        wildcard.AsnTypeDefinition = choice;
        wildcard.Constraints =
        [
            this.UserConstraint(
                wildcard.ProcessContents == "strict"
                    ? "The fallback alternative shall be used if and only if xsi:type is present."
                    : "The fallback alternative is selected using lax xsi:type rules.",
                wildcard.Span)
        ];
        wildcard.XerInstructions =
        [
            anyElement,
            new AsnEncodingInstruction
            {
                Name = "UNTAGGED",
                Kind = AsnInstructionKind.Untagged,
                Span = wildcard.Span
            }
        ];
    }

    internal XsdDeclarationDefinition[] AllowedGlobalElements(
        string[] namespaceTokens,
        string targetNamespace)
    {
        System.Collections.Generic.List<XsdDeclarationDefinition> result =
            new System.Collections.Generic.List<XsdDeclarationDefinition>();
        int index = 0;
        while (index < this.Declarations.Elements.Length)
        {
            XsdDeclarationDefinition candidate = this.Declarations.Elements[index];
            if (candidate.Global && !candidate.Abstract
                && this.NamespaceAllowed(
                    candidate.NamespaceUri,
                    namespaceTokens,
                    targetNamespace))
            {
                result.Add(candidate);
            }
            index = index + 1;
        }
        this.SortDeclarations(result);
        return result.ToArray();
    }

    internal bool NamespaceAllowed(
        string candidate,
        string[] tokens,
        string targetNamespace)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            string token = tokens[index];
            if (token == "##any"
                || token == "##targetNamespace" && candidate == targetNamespace
                || token == "##local" && candidate.Length == 0
                || token == "##other" && candidate != targetNamespace
                    && candidate.Length > 0
                || token == candidate)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void MapSubstitutionGroups()
    {
        int index = 0;
        while (index < this.Declarations.Elements.Length)
        {
            XsdDeclarationDefinition member = this.Declarations.Elements[index];
            if (member.Global && member.Token != null)
            {
                XerXmlAttribute? affiliation =
                    new XsdDocumentLoader().FindAttribute(
                        member.Token,
                        "",
                        "substitutionGroup");
                if (affiliation != null)
                {
                    string[] name = this.SimpleMapper.ResolveQualifiedName(
                        member.Token,
                        affiliation.Value);
                    XsdDeclarationDefinition? head = name.Length == 0
                        ? null
                        : this.FindGlobalElement(name[0], name[1]);
                    if (head == null)
                    {
                        this.AddDiagnostic(
                            XsdDiagnosticCodes.InvalidSubstitutionGroup,
                            "An element substitution-group head cannot be resolved.",
                            member.Span);
                    }
                    else if (!this.ContainsText(
                            head.BlockValues,
                            "substitution")
                        && this.FindSubstitutionGroup(head) == null)
                    {
                        this.CreateSubstitutionGroup(head);
                    }
                }
            }
            index = index + 1;
        }
    }

    internal void CreateSubstitutionGroup(XsdDeclarationDefinition head)
    {
        System.Collections.Generic.List<XsdDeclarationDefinition> members =
            new System.Collections.Generic.List<XsdDeclarationDefinition>();
        int index = 0;
        while (index < this.Declarations.Elements.Length)
        {
            XsdDeclarationDefinition candidate = this.Declarations.Elements[index];
            if (candidate.Global && !candidate.Abstract
                && !this.ContainsText(head.BlockValues, "substitution")
                && (candidate == head && !head.Abstract
                    || this.IsSubstitutionMember(candidate, head)))
            {
                members.Add(candidate);
            }
            index = index + 1;
        }
        this.SortDeclarations(members);
        string asn = "CHOICE {";
        index = 0;
        while (index < members.Count)
        {
            if (index > 0)
            {
                asn = asn + ", ";
            }
            asn = asn + members[index].Identifier + " "
                + members[index].TypeReferenceName;
            index = index + 1;
        }
        XsdSubstitutionGroupDefinition group =
            new XsdSubstitutionGroupDefinition
            {
                StableId = "x694:substitution-group:" + head.StableId,
                HeadName = head.Name,
                HeadNamespace = head.NamespaceUri,
                Head = head,
                Members = members.ToArray(),
                TypeReferenceName = head.TypeReferenceName + "-group",
                AsnTypeDefinition = asn + "}",
                XerInstructions =
                [
                    new AsnEncodingInstruction
                    {
                        Name = "UNTAGGED",
                        Kind = AsnInstructionKind.Untagged,
                        Span = head.Span
                    }
                ],
                Span = head.Span
            };
        head.SubstitutionGroup = group;
        this.SubstitutionGroups.Add(group);
    }

    internal bool IsSubstitutionMember(
        XsdDeclarationDefinition candidate,
        XsdDeclarationDefinition head)
    {
        XsdDeclarationDefinition? current = candidate;
        int depth = 0;
        while (current != null && current.Token != null
            && depth < this.Declarations.Elements.Length)
        {
            XerXmlAttribute? affiliation =
                new XsdDocumentLoader().FindAttribute(
                    current.Token,
                    "",
                    "substitutionGroup");
            if (affiliation == null)
            {
                return false;
            }
            string[] name = this.SimpleMapper.ResolveQualifiedName(
                current.Token,
                affiliation.Value);
            current = name.Length == 0
                ? null
                : this.FindGlobalElement(name[0], name[1]);
            if (current == head)
            {
                return true;
            }
            depth = depth + 1;
        }
        return false;
    }

    internal void MapTypeSubstitutions()
    {
        int index = 0;
        while (index < this.Declarations.Elements.Length)
        {
            XsdDeclarationDefinition declaration =
                this.Declarations.Elements[index];
            XsdDeclarationTypeUse? use = declaration.TypeUse;
            if (declaration.Kind == "element" && use != null
                && !use.Anonymous && !use.BuiltIn)
            {
                System.Collections.Generic.List<XsdTypeAlternativeDefinition>
                    alternatives = this.TypeAlternatives(
                        use,
                        declaration.Nillable,
                        declaration);
                bool abstractBase = this.IsAbstractComplexTypeUse(use);
                if (alternatives.Count > 1
                    || abstractBase && alternatives.Count > 0)
                {
                    XsdTypeSubstitutionDefinition mapped =
                        this.CreateTypeSubstitution(
                            declaration,
                            use,
                            alternatives);
                    declaration.TypeSubstitution = mapped;
                    this.TypeSubstitutions.Add(mapped);
                }
                else if (abstractBase)
                {
                    this.AddDiagnostic(
                        XsdDiagnosticCodes.InvalidTypeSubstitution,
                        "An abstract XML Schema type has no concrete substitution alternative.",
                        declaration.Span);
                }
            }
            index = index + 1;
        }
    }

    internal System.Collections.Generic.List<XsdTypeAlternativeDefinition>
        TypeAlternatives(
            XsdDeclarationTypeUse use,
            bool nillable,
            XsdDeclarationDefinition declaration)
    {
        System.Collections.Generic.List<XsdTypeAlternativeDefinition> result =
            new System.Collections.Generic.List<XsdTypeAlternativeDefinition>();
        if (use.Kind == "simple" && use.SimpleType != null)
        {
            result.Add(this.SimpleAlternative(use.SimpleType, nillable));
            int index = 0;
            while (index < this.SimpleTypes.Types.Length)
            {
                XsdSimpleTypeDefinition candidate = this.SimpleTypes.Types[index];
                if (candidate.Name.Length > 0
                    && candidate != use.SimpleType
                    && this.DerivesFromSimple(candidate, use.SimpleType)
                    && !this.ContainsText(
                        declaration.BlockValues,
                        "restriction"))
                {
                    result.Add(this.SimpleAlternative(candidate, nillable));
                }
                index = index + 1;
            }
        }
        else if (use.Kind == "complex" && use.ComplexType != null)
        {
            XsdComplexTypeDefinition? baseType = this.FindComplexType(
                use.NamespaceUri,
                use.Name);
            if (baseType != null)
            {
                if (!baseType.Abstract)
                {
                    result.Add(this.ComplexAlternative(baseType, nillable));
                }
                int index = 0;
                while (index < this.ComplexTypes.Types.Length)
                {
                    XsdComplexTypeDefinition candidate =
                        this.ComplexTypes.Types[index];
                    if (candidate.Name.Length > 0 && candidate != baseType
                        && !candidate.Abstract
                        && this.AllowsComplexAlternative(
                            candidate,
                            baseType,
                            declaration))
                    {
                        result.Add(this.ComplexAlternative(candidate, nillable));
                    }
                    index = index + 1;
                }
            }
        }
        int sortStart = use.Kind == "simple"
            ? 1
            : result.Count > 0
                && result[0].ComplexType == this.FindComplexType(
                    use.NamespaceUri,
                    use.Name)
                    ? 1
                    : 0;
        if (result.Count > sortStart + 1)
        {
            this.SortTypeAlternatives(result, sortStart);
        }
        return result;
    }

    internal bool IsAbstractComplexTypeUse(XsdDeclarationTypeUse use)
    {
        if (use.Kind != "complex")
        {
            return false;
        }
        XsdComplexTypeDefinition? type = this.FindComplexType(
            use.NamespaceUri,
            use.Name);
        return type != null && type.Abstract;
    }

    internal bool AllowsComplexAlternative(
        XsdComplexTypeDefinition candidate,
        XsdComplexTypeDefinition expected,
        XsdDeclarationDefinition declaration)
    {
        XsdComplexTypeDefinition? current = candidate;
        while (current != null && current != expected)
        {
            if (this.ContainsText(
                    declaration.BlockValues,
                    current.Derivation))
            {
                return false;
            }
            current = current.BaseComplexType;
        }
        return current == expected;
    }

    internal bool ContainsText(string[] values, string expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == expected)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal XsdTypeAlternativeDefinition SimpleAlternative(
        XsdSimpleTypeDefinition type,
        bool nillable)
    {
        string core = type.AsnTypeDefinition;
        return new XsdTypeAlternativeDefinition
        {
            StableId = "x694:type-alternative:" + type.StableId,
            Name = type.Name,
            NamespaceUri = type.NamespaceUri,
            Kind = "simple",
            AsnTypeDefinition = nillable
                ? "SEQUENCE {content " + core + " OPTIONAL}"
                : new XsdDeclarationMapper().ToTypeReference(type.Name),
            Nillable = nillable,
            SimpleType = type,
            XerInstructions = nillable
                ?
                [
                    new AsnEncodingInstruction
                    {
                        Name = "USE-NIL",
                        Kind = AsnInstructionKind.UseNil
                    }
                ]
                : []
        };
    }

    internal XsdTypeAlternativeDefinition ComplexAlternative(
        XsdComplexTypeDefinition type,
        bool nillable)
    {
        return new XsdTypeAlternativeDefinition
        {
            StableId = "x694:type-alternative:" + type.StableId,
            Name = type.Name,
            NamespaceUri = type.NamespaceUri,
            Kind = "complex",
            AsnTypeDefinition = nillable
                ? this.RenderNillableComplex(type)
                : type.TypeReferenceName,
            Nillable = nillable,
            ComplexType = type,
            XerInstructions = nillable
                ?
                [
                    new AsnEncodingInstruction
                    {
                        Name = "USE-NIL",
                        Kind = AsnInstructionKind.UseNil
                    }
                ]
                : []
        };
    }

    internal XsdTypeSubstitutionDefinition CreateTypeSubstitution(
        XsdDeclarationDefinition declaration,
        XsdDeclarationTypeUse use,
        System.Collections.Generic.List<XsdTypeAlternativeDefinition>
            alternatives)
    {
        string asn = "CHOICE {";
        int index = 0;
        while (index < alternatives.Count)
        {
            if (index > 0)
            {
                asn = asn + ", ";
            }
            asn = asn + new XsdDeclarationMapper().ToIdentifier(
                alternatives[index].Name) + " "
                + alternatives[index].AsnTypeDefinition;
            index = index + 1;
        }
        string suffix = declaration.Nillable
            ? "-deriv-nillable"
            : "-derivations";
        return new XsdTypeSubstitutionDefinition
        {
            StableId = "x694:type-substitution:" + declaration.StableId,
            BaseTypeName = use.Name,
            BaseTypeNamespace = use.NamespaceUri,
            Nillable = declaration.Nillable,
            Alternatives = alternatives.ToArray(),
            TypeReferenceName = new XsdDeclarationMapper().ToTypeReference(
                use.Name + suffix),
            AsnTypeDefinition = asn + "}",
            XerInstructions =
            [
                new AsnEncodingInstruction
                {
                    Name = "USE-TYPE",
                    Kind = AsnInstructionKind.UseType,
                    Span = declaration.Span
                }
            ],
            Span = declaration.Span
        };
    }

    internal bool DerivesFromSimple(
        XsdSimpleTypeDefinition candidate,
        XsdSimpleTypeDefinition expected)
    {
        XsdSimpleTypeDefinition? current = candidate.BaseDefinition;
        while (current != null)
        {
            if (current == expected)
            {
                return true;
            }
            current = current.BaseDefinition;
        }
        return false;
    }

    internal bool DerivesFromComplex(
        XsdComplexTypeDefinition candidate,
        XsdComplexTypeDefinition expected)
    {
        XsdComplexTypeDefinition? current = candidate.BaseComplexType;
        while (current != null)
        {
            if (current == expected)
            {
                return true;
            }
            current = current.BaseComplexType;
        }
        return false;
    }

    internal void ApplySubstitutionGroups()
    {
        int index = 0;
        while (index < this.ComplexTypes.ModelGroups.Length)
        {
            XsdModelGroupDefinition group =
                this.ComplexTypes.ModelGroups[index];
            this.ApplyParticleSubstitutions(group.Particle, false);
            if (group.Particle != null)
            {
                group.AsnTypeDefinition = group.Particle.AsnTypeDefinition;
                group.XerInstructions = group.Particle.XerInstructions;
            }
            index = index + 1;
        }
        index = 0;
        while (index < this.ComplexTypes.Types.Length)
        {
            XsdComplexTypeDefinition type = this.ComplexTypes.Types[index];
            this.ApplyParticleSubstitutions(type.Particle, true);
            index = index + 1;
        }
    }

    internal void ApplyParticleSubstitutions(
        XsdParticleDefinition? particle,
        bool sequenceComponent)
    {
        if (particle == null)
        {
            return;
        }
        bool groupSubstitution = false;
        if (particle.Declaration != null && particle.Declaration.Reference)
        {
            XsdDeclarationDefinition? head = this.FindGlobalElement(
                particle.Declaration.ReferenceNamespace,
                particle.Declaration.ReferenceName);
            XsdSubstitutionGroupDefinition? group = head == null
                ? null
                : this.FindSubstitutionGroup(head);
            if (group != null)
            {
                groupSubstitution = true;
                particle.Declaration.SubstitutionGroup = group;
                particle.AsnTypeDefinition = group.AsnTypeDefinition;
                particle.NamedTypeDefinition = this.ComplexMapper
                    .RenderParticleUse(
                        particle,
                        group.AsnTypeDefinition,
                        sequenceComponent,
                        particle.Identifier);
            }
        }
        if (particle.Declaration != null && !groupSubstitution)
        {
            string core = particle.Declaration.TypeSubstitution == null
                ? particle.Declaration.AsnTypeDefinition
                : particle.Declaration.TypeSubstitution.AsnTypeDefinition;
            particle.AsnTypeDefinition = core;
            particle.NamedTypeDefinition = this.ComplexMapper.RenderParticleUse(
                particle,
                core,
                sequenceComponent,
                particle.Identifier);
        }
        int index = 0;
        while (index < particle.Children.Length)
        {
            this.ApplyParticleSubstitutions(
                particle.Children[index],
                particle.Kind == "sequence" || particle.Kind == "all");
            index = index + 1;
        }
        if (particle.Kind == "sequence" || particle.Kind == "choice"
            || particle.Kind == "all")
        {
            particle.AsnTypeDefinition = this.ComplexMapper.RenderModelGroup(
                particle);
            particle.NamedTypeDefinition = particle.PointlessSequence
                ? this.ComplexMapper.RenderComponents(particle.Children)
                : this.ComplexMapper.RenderParticleUse(
                    particle,
                    particle.AsnTypeDefinition,
                    sequenceComponent,
                    particle.Identifier);
        }
    }

    internal void RefreshComplexTypes()
    {
        int index = 0;
        while (index < this.ComplexTypes.Types.Length)
        {
            XsdComplexTypeDefinition type = this.ComplexTypes.Types[index];
            type.AsnTypeDefinition = this.ComplexMapper.RenderComplexType(type);
            index = index + 1;
        }
    }

    internal void RefreshDeclarations()
    {
        XsdDeclarationMapper renderer = new XsdDeclarationMapper();
        int index = 0;
        while (index < this.Declarations.Declarations.Length)
        {
            XsdDeclarationDefinition declaration =
                this.Declarations.Declarations[index];
            if (declaration.TypeSubstitution != null)
            {
                declaration.AsnTypeDefinition =
                    declaration.TypeSubstitution.AsnTypeDefinition;
                this.MergeInstruction(
                    declaration,
                    declaration.TypeSubstitution.XerInstructions[0]);
                declaration.OccurrenceTypeDefinition = renderer.RenderOccurrence(
                    declaration,
                    declaration.AsnTypeDefinition);
            }
            else if (declaration.TypeUse != null
                && declaration.TypeUse.Kind == "complex")
            {
                XsdComplexTypeDefinition? type = declaration.TypeUse.Anonymous
                    && declaration.TypeUse.Token != null
                    ? this.FindComplexTypeByToken(declaration.TypeUse.Token)
                    : this.FindComplexType(
                        declaration.TypeUse.NamespaceUri,
                        declaration.TypeUse.Name);
                if (type != null)
                {
                    declaration.TypeUse.AsnTypeDefinition =
                        type.AsnTypeDefinition;
                    if (declaration.Nillable)
                    {
                        declaration.AsnTypeDefinition =
                            this.RenderNillableComplex(type);
                        declaration.OccurrenceTypeDefinition =
                            renderer.RenderOccurrence(
                                declaration,
                                declaration.AsnTypeDefinition);
                    }
                    else
                    {
                        renderer.RenderDeclaration(declaration);
                    }
                }
            }
            index = index + 1;
        }
    }

    internal string RenderNillableComplex(XsdComplexTypeDefinition type)
    {
        string components = type.Mixed
            ? "embed-values SEQUENCE OF XSDv2.String"
            : "";
        components = this.ComplexMapper.AppendComponent(
            components,
            this.ComplexMapper.RenderAttributes(type.AttributeUses));
        if (type.AttributeWildcard != null)
        {
            components = this.ComplexMapper.AppendComponent(
                components,
                "attr " + type.AttributeWildcard.AsnTypeDefinition);
        }
        string content;
        if (type.ContentKind == "simple" && type.SimpleContentType != null)
        {
            content = type.SimpleContentType.AsnTypeDefinition;
        }
        else if (type.Particle != null)
        {
            string inner = type.Particle.Kind == "sequence"
                ? this.ComplexMapper.RenderComponents(type.Particle.Children)
                : type.Particle.Kind == "all"
                    ? this.ComplexMapper.RenderAllComponents(type.Particle)
                    : type.Particle.NamedTypeDefinition;
            content = "SEQUENCE {" + inner + "}";
        }
        else
        {
            content = "NULL";
        }
        components = this.ComplexMapper.AppendComponent(
            components,
            "content " + content + " OPTIONAL");
        return "SEQUENCE {" + components + "}";
    }

    internal XsdWildcardDefinition? FindGroupAttributeWildcard(
        XsdComplexTypeDefinition type)
    {
        int referenceIndex = 0;
        while (referenceIndex < type.AttributeGroupReferences.Length)
        {
            string[] name = this.ComplexMapper.DecodeGroupName(
                type.AttributeGroupReferences[referenceIndex]);
            int groupIndex = 0;
            while (groupIndex < this.ComplexTypes.AttributeGroups.Length)
            {
                XsdModelGroupDefinition group =
                    this.ComplexTypes.AttributeGroups[groupIndex];
                if (name.Length > 0 && group.NamespaceUri == name[0]
                    && group.Name == name[1])
                {
                    XsdWildcardDefinition? mapped =
                        this.FindAttributeWildcardInGroup(group, 0);
                    if (mapped != null)
                    {
                        return mapped;
                    }
                }
                groupIndex = groupIndex + 1;
            }
            referenceIndex = referenceIndex + 1;
        }
        return type.BaseComplexType == null
            ? null
            : type.BaseComplexType.AttributeWildcard
                ?? this.FindGroupAttributeWildcard(type.BaseComplexType);
    }

    internal XsdWildcardDefinition? FindAttributeWildcardInGroup(
        XsdModelGroupDefinition group,
        int depth)
    {
        if (group.AttributeWildcard != null)
        {
            return group.AttributeWildcard;
        }
        if (depth >= this.ComplexTypes.AttributeGroups.Length)
        {
            return null;
        }
        int referenceIndex = 0;
        while (referenceIndex < group.ReferencedAttributeGroups.Length)
        {
            string[] name = this.ComplexMapper.DecodeGroupName(
                group.ReferencedAttributeGroups[referenceIndex]);
            int groupIndex = 0;
            while (groupIndex < this.ComplexTypes.AttributeGroups.Length)
            {
                XsdModelGroupDefinition candidate =
                    this.ComplexTypes.AttributeGroups[groupIndex];
                if (name.Length > 0 && candidate.NamespaceUri == name[0]
                    && candidate.Name == name[1])
                {
                    XsdWildcardDefinition? mapped =
                        this.FindAttributeWildcardInGroup(
                            candidate,
                            depth + 1);
                    if (mapped != null)
                    {
                        return mapped;
                    }
                }
                groupIndex = groupIndex + 1;
            }
            referenceIndex = referenceIndex + 1;
        }
        return null;
    }

    internal XerXmlToken? FindAnyAttribute(XsdComplexTypeDefinition type)
    {
        if (type.Document == null || type.Token == null)
        {
            return null;
        }
        XerXmlToken owner = type.Token;
        XerXmlToken? simpleContent = this.SimpleMapper.FindDirectChild(
            type.Document,
            type.Token,
            "simpleContent");
        XerXmlToken? complexContent = this.SimpleMapper.FindDirectChild(
            type.Document,
            type.Token,
            "complexContent");
        XerXmlToken? content = simpleContent == null
            ? complexContent
            : simpleContent;
        if (content != null)
        {
            XerXmlToken? derivation = this.SimpleMapper.FindDirectChild(
                type.Document,
                content,
                "extension");
            if (derivation == null)
            {
                derivation = this.SimpleMapper.FindDirectChild(
                    type.Document,
                    content,
                    "restriction");
            }
            if (derivation != null)
            {
                owner = derivation;
            }
        }
        return this.SimpleMapper.FindDirectChild(
            type.Document,
            owner,
            "anyAttribute");
    }

    internal XsdDeclarationDefinition? FindGlobalElement(
        string namespaceUri,
        string name)
    {
        int index = 0;
        while (index < this.Declarations.Elements.Length)
        {
            XsdDeclarationDefinition candidate = this.Declarations.Elements[index];
            if (candidate.Global && candidate.NamespaceUri == namespaceUri
                && candidate.Name == name)
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdSubstitutionGroupDefinition? FindSubstitutionGroup(
        XsdDeclarationDefinition head)
    {
        int index = 0;
        while (index < this.SubstitutionGroups.Count)
        {
            if (this.SubstitutionGroups[index].Head == head)
            {
                return this.SubstitutionGroups[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdComplexTypeDefinition? FindComplexType(
        string namespaceUri,
        string name)
    {
        int index = 0;
        while (index < this.ComplexTypes.Types.Length)
        {
            XsdComplexTypeDefinition candidate = this.ComplexTypes.Types[index];
            if (candidate.NamespaceUri == namespaceUri && candidate.Name == name)
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdComplexTypeDefinition? FindComplexTypeByToken(XerXmlToken token)
    {
        int index = 0;
        while (index < this.ComplexTypes.Types.Length)
        {
            if (this.ComplexTypes.Types[index].Token == token)
            {
                return this.ComplexTypes.Types[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal void SortDeclarations(
        System.Collections.Generic.List<XsdDeclarationDefinition> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            XsdDeclarationDefinition current = values[index];
            int position = index;
            while (position > 0
                && this.CompareDeclarations(values[position - 1], current) > 0)
            {
                values[position] = values[position - 1];
                position = position - 1;
            }
            values[position] = current;
            index = index + 1;
        }
    }

    internal int CompareDeclarations(
        XsdDeclarationDefinition first,
        XsdDeclarationDefinition second)
    {
        int comparison = string.CompareOrdinal(
            first.NamespaceUri,
            second.NamespaceUri);
        return comparison == 0
            ? string.CompareOrdinal(first.Name, second.Name)
            : comparison;
    }

    internal void SortTypeAlternatives(
        System.Collections.Generic.List<XsdTypeAlternativeDefinition> values,
        int start)
    {
        int index = start + 1;
        while (index < values.Count)
        {
            XsdTypeAlternativeDefinition current = values[index];
            int position = index;
            while (position > start
                && this.CompareTypeAlternatives(
                    values[position - 1],
                    current) > 0)
            {
                values[position] = values[position - 1];
                position = position - 1;
            }
            values[position] = current;
            index = index + 1;
        }
    }

    internal int CompareTypeAlternatives(
        XsdTypeAlternativeDefinition first,
        XsdTypeAlternativeDefinition second)
    {
        int comparison = string.CompareOrdinal(
            first.NamespaceUri,
            second.NamespaceUri);
        return comparison == 0
            ? string.CompareOrdinal(first.Name, second.Name)
            : comparison;
    }

    internal void AddInstruction(
        XsdComplexTypeDefinition type,
        AsnEncodingInstruction instruction)
    {
        AsnEncodingInstruction[] result =
            new AsnEncodingInstruction[type.XerInstructions.Length + 1];
        int index = 0;
        while (index < type.XerInstructions.Length)
        {
            result[index] = type.XerInstructions[index];
            index = index + 1;
        }
        result[index] = instruction;
        type.XerInstructions = result;
    }

    internal void MergeInstruction(
        XsdDeclarationDefinition declaration,
        AsnEncodingInstruction instruction)
    {
        int index = 0;
        while (index < declaration.XerInstructions.Length)
        {
            if (declaration.XerInstructions[index].Kind == instruction.Kind)
            {
                return;
            }
            index = index + 1;
        }
        AsnEncodingInstruction[] result =
            new AsnEncodingInstruction[declaration.XerInstructions.Length + 1];
        index = 0;
        while (index < declaration.XerInstructions.Length)
        {
            result[index] = declaration.XerInstructions[index];
            index = index + 1;
        }
        result[index] = instruction;
        declaration.XerInstructions = result;
    }

    internal AsnConstraint UserConstraint(
        string text,
        AsnSourceSpan? span)
    {
        return new AsnConstraint
        {
            Kind = AsnConstraintKind.UserDefined,
            UserDefinedBody = text,
            RequiresApplicationValidation = true,
            Span = span
        };
    }

    internal AsnEncodingInstruction WildcardInstruction(
        AsnInstructionKind kind,
        string name,
        string[] namespaceTokens,
        AsnSourceSpan? span)
    {
        return new AsnEncodingInstruction
        {
            Name = name,
            Kind = kind,
            Arguments = namespaceTokens,
            Span = span
        };
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

    internal XsdAdvancedMappingResult CreateResult()
    {
        return new XsdAdvancedMappingResult
        {
            Success = this.Diagnostics.Count == 0,
            Wildcards = this.Wildcards.ToArray(),
            SubstitutionGroups = this.SubstitutionGroups.ToArray(),
            TypeSubstitutions = this.TypeSubstitutions.ToArray(),
            Diagnostics = this.Diagnostics.ToArray()
        };
    }
}
