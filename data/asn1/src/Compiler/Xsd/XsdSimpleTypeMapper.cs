namespace asn1;

internal class XsdSimpleTypeMapper
{
    internal XsdSchemaGraphResult Graph = new XsdSchemaGraphResult();
    internal AsnResourceLimits Limits = new AsnResourceLimits();
    internal System.Collections.Generic.List<XsdSimpleTypeDefinition> Types =
        new System.Collections.Generic.List<XsdSimpleTypeDefinition>();
    internal System.Collections.Generic.List<AsnDiagnostic> Diagnostics =
        new System.Collections.Generic.List<AsnDiagnostic>();
    internal System.Collections.Generic.List<XsdValueConstraintDefinition>
        ValueConstraints =
            new System.Collections.Generic.List<XsdValueConstraintDefinition>();
    internal System.Collections.Generic.List<string> ActiveTypes =
        new System.Collections.Generic.List<string>();

    internal XsdSimpleTypeMappingResult MapSimpleTypes(
        XsdSchemaGraphResult graph,
        AsnXsdImportOptions options)
    {
        if (graph == null)
        {
            throw new System.ArgumentNullException("graph");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        this.Graph = graph;
        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        this.Types =
            new System.Collections.Generic.List<XsdSimpleTypeDefinition>();
        this.Diagnostics = new System.Collections.Generic.List<AsnDiagnostic>();
        this.ValueConstraints =
            new System.Collections.Generic.List<XsdValueConstraintDefinition>();
        this.ActiveTypes = new System.Collections.Generic.List<string>();
        if (!graph.Success)
        {
            return this.CreateResult();
        }
        int componentIndex = 0;
        while (componentIndex < graph.Components.Length)
        {
            XsdComponentDefinition component = graph.Components[componentIndex];
            if (component.Kind == "simpleType"
                && this.HasSimpleTypeVariety(component))
            {
                if (this.Types.Count >= this.Limits.MaximumCollectionElements)
                {
                    this.AddDiagnostic(
                        XsdDiagnosticCodes.GraphLimit,
                        "The mapped XML Schema simple-type count exceeds the configured collection limit.",
                        component.Span);
                    break;
                }
                XsdLoadedDocument? document = this.FindDocument(component);
                this.Types.Add(new XsdSimpleTypeDefinition
                {
                    StableId = "x694:simple-type:" + component.StableId,
                    Name = component.Name,
                    NamespaceUri = component.NamespaceUri,
                    Component = component,
                    Document = document,
                    Token = component.Token,
                    Span = component.Span
                });
            }
            componentIndex = componentIndex + 1;
        }
        int typeIndex = 0;
        while (typeIndex < this.Types.Count)
        {
            this.MapDefinition(this.Types[typeIndex]);
            typeIndex = typeIndex + 1;
        }
        this.MapValueConstraints();
        return this.CreateResult();
    }

    internal bool HasSimpleTypeVariety(XsdComponentDefinition component)
    {
        XsdLoadedDocument? document = this.FindDocument(component);
        if (document == null || component.Token == null)
        {
            return false;
        }
        return this.FindDirectChild(document, component.Token, "restriction")
                != null
            || this.FindDirectChild(document, component.Token, "list") != null
            || this.FindDirectChild(document, component.Token, "union") != null;
    }

    internal bool HasRestriction(XsdComponentDefinition component)
    {
        XsdLoadedDocument? document = this.FindDocument(component);
        if (document == null || component.Token == null)
        {
            return false;
        }
        return this.FindDirectChild(
            document,
            component.Token,
            "restriction") != null;
    }

    internal bool MapDefinition(XsdSimpleTypeDefinition definition)
    {
        if (definition.AsnTypeDefinition.Length > 0)
        {
            return true;
        }
        if (this.ContainsText(this.ActiveTypes, definition.StableId))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidSimpleType,
                "The XML Schema simple-type restriction chain contains a cycle.",
                definition.Span);
            return false;
        }
        XsdLoadedDocument? document = definition.Document;
        XerXmlToken? token = definition.Token;
        if (document == null || token == null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidSimpleType,
                "An XML Schema simple type has no source definition.",
                definition.Span);
            return false;
        }
        XerXmlToken? restriction = this.FindDirectChild(
            document,
            token,
            "restriction");
        XerXmlToken? list = this.FindDirectChild(document, token, "list");
        XerXmlToken? union = this.FindDirectChild(document, token, "union");
        this.ActiveTypes.Add(definition.StableId);
        if (list != null)
        {
            bool listSuccess = this.MapListDefinition(
                definition,
                document,
                list);
            this.ActiveTypes.RemoveAt(this.ActiveTypes.Count - 1);
            return listSuccess;
        }
        if (union != null)
        {
            bool unionSuccess = this.MapUnionDefinition(
                definition,
                document,
                union);
            this.ActiveTypes.RemoveAt(this.ActiveTypes.Count - 1);
            return unionSuccess;
        }
        if (restriction == null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidSimpleType,
                "An XML Schema simple type requires restriction, list, or union content.",
                definition.Span);
            this.ActiveTypes.RemoveAt(this.ActiveTypes.Count - 1);
            return false;
        }
        definition.Variety = XsdSimpleTypeVariety.Atomic;
        string baseExpression = this.ResolveBaseType(
            definition,
            restriction);
        if (baseExpression.Length == 0)
        {
            this.ActiveTypes.RemoveAt(this.ActiveTypes.Count - 1);
            return false;
        }
        definition.Facets = this.CollectFacets(document, restriction);
        definition.EffectiveFacets = this.MergeFacetSets(
            definition.EffectiveFacets,
            definition.Facets);
        string whitespace = this.EffectiveWhitespace(
            definition.BuiltInBaseName,
            definition.EffectiveFacets);
        definition.EnumerationItems = this.MapEnumeration(
            definition,
            whitespace);
        definition.Enumerated = definition.EnumerationItems.Length > 0
            && (this.IsStringDerived(definition.BuiltInBaseName)
                || this.IsIntegerDerived(definition.BuiltInBaseName));
        definition.Constraints = this.MapConstraints(
            definition,
            whitespace);
        definition.XerInstructions = this.MapXerInstructions(
            definition,
            whitespace);
        definition.AsnTypeDefinition = definition.Enumerated
            ? this.RenderEnumeration(definition)
            : this.RenderConstrainedType(baseExpression, definition.Constraints);
        this.ActiveTypes.RemoveAt(this.ActiveTypes.Count - 1);
        return true;
    }

    internal string ResolveBaseType(
        XsdSimpleTypeDefinition definition,
        XerXmlToken restriction)
    {
        XerXmlAttribute? baseAttribute = new XsdDocumentLoader().FindAttribute(
            restriction,
            "",
            "base");
        if (baseAttribute == null || baseAttribute.Value.Length == 0)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidSimpleType,
                "A restricted XML Schema simple type requires a base QName.",
                definition.Span);
            return "";
        }
        string[] qualifiedName = this.ResolveQualifiedName(
            restriction,
            baseAttribute.Value);
        if (qualifiedName.Length == 0)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.UnknownSimpleTypeBase,
                "The XML Schema simple-type base QName cannot be resolved.",
                definition.Span);
            return "";
        }
        definition.BaseTypeNamespace = qualifiedName[0];
        definition.BaseTypeName = qualifiedName[1];
        XsdBuiltInTypeDefinition? builtIn =
            XsdBuiltInTypeCatalog.FindType(qualifiedName[0], qualifiedName[1]);
        if (builtIn != null && qualifiedName[1] != "anyType")
        {
            definition.BuiltInBaseName = builtIn.XsdName;
            definition.Variety = XsdSimpleTypeVariety.Atomic;
            return builtIn.AsnTypeDefinition;
        }
        XsdSimpleTypeDefinition? mappedBase = this.FindMappedType(
            qualifiedName[0],
            qualifiedName[1]);
        if (mappedBase == null || !this.MapDefinition(mappedBase))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.UnknownSimpleTypeBase,
                "The XML Schema simple-type base {"
                    + qualifiedName[0] + "}" + qualifiedName[1]
                    + " is not a mapped restriction type.",
                definition.Span);
            return "";
        }
        definition.BuiltInBaseName = mappedBase.BuiltInBaseName;
        definition.Variety = mappedBase.Variety;
        definition.ItemType = mappedBase.ItemType;
        definition.UnionAlternatives = mappedBase.UnionAlternatives;
        definition.BaseDefinition = mappedBase;
        definition.EffectiveFacets = mappedBase.EffectiveFacets;
        return this.ToTypeReference(mappedBase.Name);
    }

    internal bool MapListDefinition(
        XsdSimpleTypeDefinition definition,
        XsdLoadedDocument document,
        XerXmlToken list)
    {
        XerXmlAttribute? itemType = new XsdDocumentLoader().FindAttribute(
            list,
            "",
            "itemType");
        XerXmlToken? anonymousToken = this.FindDirectChild(
            document,
            list,
            "simpleType");
        if ((itemType == null || itemType.Value.Length == 0)
            == (anonymousToken == null))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidListType,
                "An XML Schema list requires exactly one itemType QName or anonymous simple type.",
                definition.Span);
            return false;
        }
        XsdSimpleTypeUse? use = itemType != null
            ? this.ResolveNamedTypeUse(list, itemType.Value)
            : anonymousToken == null ? null
                : this.CreateAnonymousTypeUse(
                    definition,
                    document,
                    anonymousToken,
                    "item");
        if (use == null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidListType,
                "The XML Schema list item type cannot be resolved.",
                definition.Span);
            return false;
        }
        if ((use.Definition != null
                && use.Definition.Variety == XsdSimpleTypeVariety.List)
            || this.IsBuiltInListType(use.BuiltInTypeName))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidListType,
                "An XML Schema list item type cannot itself be a list type.",
                definition.Span);
            return false;
        }
        string itemExpression = use.AsnTypeDefinition;
        if (this.IsStringDerived(use.BuiltInTypeName)
            && (use.Definition == null || !use.Definition.Enumerated))
        {
            itemExpression = itemExpression
                + " (FROM ({0,0,0,33}..{0,16,255,253}))";
        }
        definition.Variety = XsdSimpleTypeVariety.List;
        definition.ItemType = use;
        definition.AsnTypeDefinition = "SEQUENCE OF " + itemExpression;
        definition.XerInstructions =
        [
            new AsnEncodingInstruction
            {
                Name = "LIST",
                Kind = AsnInstructionKind.List
            }
        ];
        return true;
    }

    internal bool MapUnionDefinition(
        XsdSimpleTypeDefinition definition,
        XsdLoadedDocument document,
        XerXmlToken union)
    {
        System.Collections.Generic.List<XsdSimpleTypeUse> uses =
            new System.Collections.Generic.List<XsdSimpleTypeUse>();
        XerXmlAttribute? memberTypes = new XsdDocumentLoader().FindAttribute(
            union,
            "",
            "memberTypes");
        if (memberTypes != null)
        {
            string[] names = this.SplitXmlWhitespace(memberTypes.Value);
            int nameIndex = 0;
            while (nameIndex < names.Length)
            {
                XsdSimpleTypeUse? use = this.ResolveNamedTypeUse(
                    union,
                    names[nameIndex]);
                if (use == null)
                {
                    this.AddDiagnostic(
                        XsdDiagnosticCodes.InvalidUnionType,
                        "The XML Schema union member type '"
                            + names[nameIndex] + "' cannot be resolved.",
                        definition.Span);
                }
                else
                {
                    uses.Add(use);
                }
                nameIndex = nameIndex + 1;
            }
        }
        XerXmlToken[] children = this.GetDirectChildren(document, union);
        int childIndex = 0;
        int anonymousIndex = 0;
        while (childIndex < children.Length)
        {
            XerXmlToken child = children[childIndex];
            if (child.NamespaceUri == XsdDocumentLoader.SchemaNamespace
                && child.LocalName == "simpleType")
            {
                XsdSimpleTypeUse? anonymous = this.CreateAnonymousTypeUse(
                    definition,
                    document,
                    child,
                    "member-" + anonymousIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
                if (anonymous != null)
                {
                    uses.Add(anonymous);
                }
                anonymousIndex = anonymousIndex + 1;
            }
            childIndex = childIndex + 1;
        }
        if (uses.Count == 0)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidUnionType,
                "An XML Schema union requires at least one member type.",
                definition.Span);
            return false;
        }
        System.Collections.Generic.List<XsdUnionAlternative> alternatives =
            new System.Collections.Generic.List<XsdUnionAlternative>();
        int index = 0;
        while (index < uses.Count)
        {
            XsdSimpleTypeUse use = uses[index];
            string candidate = use.Anonymous
                ? "alt"
                : this.ToIdentifier(use.Name);
            string identifier = this.UniqueAlternativeIdentifier(
                alternatives,
                candidate);
            AsnEncodingInstruction[] instructions = use.Anonymous
                ?
                [
                    new AsnEncodingInstruction
                    {
                        Name = "NAME",
                        Kind = AsnInstructionKind.Name,
                        Arguments = ["AS", ""]
                    }
                ]
                : [];
            alternatives.Add(new XsdUnionAlternative
            {
                StableId = definition.StableId + ":alternative:"
                    + index.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                Identifier = identifier,
                TypeUse = use,
                XerInstructions = instructions
            });
            index = index + 1;
        }
        definition.Variety = XsdSimpleTypeVariety.Union;
        definition.UnionAlternatives = alternatives.ToArray();
        definition.AsnTypeDefinition = this.RenderUnion(alternatives);
        definition.XerInstructions =
        [
            new AsnEncodingInstruction
            {
                Name = "USE-UNION",
                Kind = AsnInstructionKind.UseUnion
            }
        ];
        return true;
    }

    internal XsdSimpleTypeUse? ResolveNamedTypeUse(
        XerXmlToken context,
        string qualifiedNameText)
    {
        string[] qualifiedName = this.ResolveQualifiedName(
            context,
            qualifiedNameText);
        if (qualifiedName.Length == 0)
        {
            return null;
        }
        XsdBuiltInTypeDefinition? builtIn =
            XsdBuiltInTypeCatalog.FindType(qualifiedName[0], qualifiedName[1]);
        if (builtIn != null && builtIn.XsdName != "anyType")
        {
            return new XsdSimpleTypeUse
            {
                StableId = builtIn.StableId,
                Name = builtIn.XsdName,
                NamespaceUri = qualifiedName[0],
                BuiltInTypeName = builtIn.XsdName,
                AsnTypeDefinition = builtIn.AsnTypeDefinition,
                XerInstructions = this.MapBuiltInUseInstructions(builtIn)
            };
        }
        XsdSimpleTypeDefinition? mapped = this.FindMappedType(
            qualifiedName[0],
            qualifiedName[1]);
        if (mapped == null || !this.MapDefinition(mapped))
        {
            return null;
        }
        return new XsdSimpleTypeUse
        {
            StableId = mapped.StableId,
            Name = mapped.Name,
            NamespaceUri = mapped.NamespaceUri,
            BuiltInTypeName = mapped.BuiltInBaseName,
            AsnTypeDefinition = this.ToTypeReference(mapped.Name),
            Definition = mapped,
            XerInstructions = mapped.XerInstructions
        };
    }

    internal XsdSimpleTypeUse? CreateAnonymousTypeUse(
        XsdSimpleTypeDefinition owner,
        XsdLoadedDocument document,
        XerXmlToken token,
        string suffix)
    {
        AsnSourceSpan? span = document.Input == null
            ? null
            : new XsdDocumentLoader().CreateTokenSpan(document.Input, token);
        XsdSimpleTypeDefinition anonymous = new XsdSimpleTypeDefinition
        {
            StableId = owner.StableId + ":anonymous:" + suffix,
            Name = "",
            NamespaceUri = owner.NamespaceUri,
            Anonymous = true,
            Document = document,
            Token = token,
            Span = span
        };
        if (this.Types.Count >= this.Limits.MaximumCollectionElements)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.GraphLimit,
                "The mapped XML Schema simple-type count exceeds the configured collection limit.",
                span);
            return null;
        }
        this.Types.Add(anonymous);
        if (!this.MapDefinition(anonymous))
        {
            return null;
        }
        return new XsdSimpleTypeUse
        {
            StableId = anonymous.StableId,
            Name = "alt",
            NamespaceUri = owner.NamespaceUri,
            BuiltInTypeName = anonymous.BuiltInBaseName,
            AsnTypeDefinition = anonymous.AsnTypeDefinition,
            Anonymous = true,
            Definition = anonymous,
            XerInstructions = anonymous.XerInstructions
        };
    }

    internal AsnEncodingInstruction[] MapBuiltInUseInstructions(
        XsdBuiltInTypeDefinition builtIn)
    {
        System.Collections.Generic.List<AsnEncodingInstruction> result =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        int index = 0;
        while (index < builtIn.XerInstructions.Length)
        {
            string text = builtIn.XerInstructions[index];
            AsnInstructionKind kind = AsnInstructionKind.Unknown;
            string name = text;
            string[] arguments = [];
            if (text == "BASE64")
            {
                kind = AsnInstructionKind.Base64;
            }
            else if (text == "DECIMAL")
            {
                kind = AsnInstructionKind.Decimal;
            }
            else if (text == "LIST")
            {
                kind = AsnInstructionKind.List;
            }
            else if (text == "USE-QNAME")
            {
                kind = AsnInstructionKind.UseQName;
            }
            else if (text == "USE-UNION")
            {
                kind = AsnInstructionKind.UseUnion;
            }
            else if (text.IndexOf(
                "WHITESPACE ",
                System.StringComparison.Ordinal) == 0)
            {
                kind = AsnInstructionKind.Whitespace;
                name = "WHITESPACE";
                arguments = [text.Substring("WHITESPACE ".Length)];
            }
            if (kind != AsnInstructionKind.Unknown)
            {
                result.Add(new AsnEncodingInstruction
                {
                    Name = name,
                    Kind = kind,
                    Arguments = arguments
                });
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal void MapValueConstraints()
    {
        int index = 0;
        while (index < this.Graph.Components.Length)
        {
            XsdComponentDefinition component = this.Graph.Components[index];
            if ((component.Kind == "element" || component.Kind == "attribute")
                && component.Token != null)
            {
                XerXmlAttribute? defaultValue =
                    new XsdDocumentLoader().FindAttribute(
                        component.Token,
                        "",
                        "default");
                XerXmlAttribute? fixedValue =
                    new XsdDocumentLoader().FindAttribute(
                        component.Token,
                        "",
                        "fixed");
                if (defaultValue != null && fixedValue != null)
                {
                    this.AddDiagnostic(
                        XsdDiagnosticCodes.InvalidValueConstraint,
                        "An XML Schema declaration cannot specify both default and fixed values.",
                        component.Span);
                }
                else if (defaultValue != null || fixedValue != null)
                {
                    XerXmlAttribute value = defaultValue == null
                        ? fixedValue ?? new XerXmlAttribute()
                        : defaultValue;
                    XsdSimpleTypeUse? use = this.ResolveValueConstraintType(
                        component);
                    if (use == null)
                    {
                        this.AddDiagnostic(
                            XsdDiagnosticCodes.InvalidValueConstraint,
                            "The value constraint type cannot be resolved as an XML Schema simple type.",
                            component.Span);
                    }
                    else if (this.ValueConstraints.Count
                        >= this.Limits.MaximumCollectionElements)
                    {
                        this.AddDiagnostic(
                            XsdDiagnosticCodes.GraphLimit,
                            "The mapped XML Schema value-constraint count exceeds the configured collection limit.",
                            component.Span);
                        break;
                    }
                    else
                    {
                        this.MapValueConstraint(
                            component,
                            use,
                            defaultValue == null ? "fixed" : "default",
                            value.Value);
                    }
                }
            }
            index = index + 1;
        }
    }

    internal XsdSimpleTypeUse? ResolveValueConstraintType(
        XsdComponentDefinition component)
    {
        if (component.Token == null)
        {
            return null;
        }
        XerXmlAttribute? type = new XsdDocumentLoader().FindAttribute(
            component.Token,
            "",
            "type");
        XsdLoadedDocument? document = this.FindDocument(component);
        XerXmlToken? anonymous = document == null
            ? null
            : this.FindDirectChild(document, component.Token, "simpleType");
        if (type != null && type.Value.Length > 0 && anonymous != null)
        {
            return null;
        }
        if (type != null && type.Value.Length > 0)
        {
            return this.ResolveNamedTypeUse(component.Token, type.Value);
        }
        if (document == null || anonymous == null)
        {
            return null;
        }
        XsdSimpleTypeDefinition owner = new XsdSimpleTypeDefinition
        {
            StableId = "x694:value-owner:" + component.StableId,
            Name = component.Name,
            NamespaceUri = component.NamespaceUri,
            Document = document,
            Token = component.Token,
            Span = component.Span
        };
        return this.CreateAnonymousTypeUse(
            owner,
            document,
            anonymous,
            "type");
    }

    internal void MapValueConstraint(
        XsdComponentDefinition component,
        XsdSimpleTypeUse use,
        string kind,
        string lexicalValue)
    {
        XsdValueConstraintDefinition mapped =
            new XsdValueConstraintDefinition
            {
                StableId = "x694:value-constraint:" + component.StableId
                    + ":" + kind,
                ComponentId = component.StableId,
                ComponentKind = component.Kind,
                ComponentName = component.Name,
                Kind = kind,
                LexicalValue = lexicalValue,
                TypeName = use.Name,
                TypeNamespace = use.NamespaceUri,
                BuiltInTypeName = use.BuiltInTypeName,
                TypeInstructions = use.XerInstructions,
                IgnoredForQName = component.Kind == "element"
                    && this.IsQNameTypeUse(use),
                Span = component.Span
            };
        if (mapped.IgnoredForQName)
        {
            this.ValueConstraints.Add(mapped);
            return;
        }
        XsdValueParseResult parsed = this.ParseTypeUseValue(
            use,
            lexicalValue,
            component.Token);
        if (!parsed.Success || parsed.Value == null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidValueConstraint,
                "The " + kind + " value is outside the declared simple type value space.",
                component.Span);
            return;
        }
        mapped.CanonicalValue = parsed.CanonicalValue;
        mapped.Value = parsed.Value;
        mapped.DefaultForEmptyInstruction = new AsnEncodingInstruction
        {
            Name = "DEFAULT-FOR-EMPTY",
            Kind = AsnInstructionKind.DefaultForEmpty,
            Arguments = [parsed.CanonicalValue],
            Span = component.Span
        };
        if (kind == "fixed")
        {
            mapped.FixedConstraint = new AsnConstraint
            {
                Kind = AsnConstraintKind.SingleValue,
                Elements =
                [
                    new AsnConstraintElement
                    {
                        Kind = AsnConstraintKind.SingleValue,
                        Value = parsed.Value,
                        Text = "(" + parsed.CanonicalValue + ")"
                    }
                ],
                Span = component.Span
            };
        }
        this.ValueConstraints.Add(mapped);
    }

    internal bool IsQNameTypeUse(XsdSimpleTypeUse use)
    {
        return use.BuiltInTypeName == "QName"
            || use.BuiltInTypeName == "NOTATION";
    }

    internal string RenderUnion(
        System.Collections.Generic.List<XsdUnionAlternative> alternatives)
    {
        string result = "CHOICE {";
        int index = 0;
        while (index < alternatives.Count)
        {
            if (index > 0)
            {
                result = result + ", ";
            }
            XsdUnionAlternative alternative = alternatives[index];
            XsdSimpleTypeUse? use = alternative.TypeUse;
            result = result + alternative.Identifier + " ";
            if (alternative.XerInstructions.Length > 0)
            {
                result = result + "[NAME AS \"\"] ";
            }
            result = result + (use == null ? "NULL" : use.AsnTypeDefinition);
            index = index + 1;
        }
        return result + "}";
    }

    internal string UniqueAlternativeIdentifier(
        System.Collections.Generic.List<XsdUnionAlternative> alternatives,
        string candidate)
    {
        string result = candidate;
        int suffix = 1;
        int index = 0;
        while (index < alternatives.Count)
        {
            if (alternatives[index].Identifier == result)
            {
                result = candidate + "-" + suffix.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
                suffix = suffix + 1;
                index = 0;
            }
            else
            {
                index = index + 1;
            }
        }
        return result;
    }

    internal string[] SplitXmlWhitespace(string text)
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && this.IsXmlWhitespace(text[index]))
            {
                index = index + 1;
            }
            int start = index;
            while (index < text.Length && !this.IsXmlWhitespace(text[index]))
            {
                index = index + 1;
            }
            if (index > start)
            {
                result.Add(text.Substring(start, index - start));
            }
        }
        return result.ToArray();
    }

    internal bool IsXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\n'
            || value == '\r';
    }

    internal XsdFacetValue[] CollectFacets(
        XsdLoadedDocument document,
        XerXmlToken restriction)
    {
        System.Collections.Generic.List<XsdFacetValue> result =
            new System.Collections.Generic.List<XsdFacetValue>();
        XerXmlToken[] children = this.GetDirectChildren(document, restriction);
        int index = 0;
        while (index < children.Length)
        {
            XerXmlToken token = children[index];
            if (this.IsFacetName(token.LocalName)
                && token.NamespaceUri == XsdDocumentLoader.SchemaNamespace)
            {
                XerXmlAttribute? value = new XsdDocumentLoader().FindAttribute(
                    token,
                    "",
                    "value");
                AsnSourceSpan? span = document.Input == null
                    ? null
                    : new XsdDocumentLoader().CreateTokenSpan(
                        document.Input,
                        token);
                if (value == null)
                {
                    this.AddDiagnostic(
                        XsdDiagnosticCodes.InvalidFacet,
                        "An XML Schema facet requires a value attribute.",
                        span);
                }
                else if (result.Count >= this.Limits.MaximumCollectionElements)
                {
                    this.AddDiagnostic(
                        XsdDiagnosticCodes.GraphLimit,
                        "The XML Schema facet count exceeds the configured collection limit.",
                        span);
                    break;
                }
                else
                {
                    result.Add(new XsdFacetValue
                    {
                        Name = token.LocalName,
                        LexicalValue = value.Value,
                        NormalizedValue = value.Value,
                        Token = token,
                        Span = span
                    });
                }
            }
            index = index + 1;
        }
        this.ValidateSingletonFacets(result);
        this.ValidateFacetRelationships(result);
        return result.ToArray();
    }

    internal bool IsFacetName(string name)
    {
        switch (name)
        {
            case "length":
            case "minLength":
            case "maxLength":
            case "pattern":
            case "whiteSpace":
            case "enumeration":
            case "totalDigits":
            case "fractionDigits":
            case "maxInclusive":
            case "maxExclusive":
            case "minInclusive":
            case "minExclusive":
                return true;
            default:
                return false;
        }
    }

    internal void ValidateSingletonFacets(
        System.Collections.Generic.List<XsdFacetValue> facets)
    {
        int index = 0;
        while (index < facets.Count)
        {
            string name = facets[index].Name;
            if (name != "enumeration" && name != "pattern")
            {
                int earlier = 0;
                while (earlier < index)
                {
                    if (facets[earlier].Name == name)
                    {
                        this.AddDiagnostic(
                            XsdDiagnosticCodes.ConflictingFacets,
                            "The XML Schema facet '" + name
                                + "' occurs more than once in one restriction.",
                            facets[index].Span);
                        break;
                    }
                    earlier = earlier + 1;
                }
            }
            index = index + 1;
        }
    }

    internal void ValidateFacetRelationships(
        System.Collections.Generic.List<XsdFacetValue> facets)
    {
        XsdFacetValue[] values = facets.ToArray();
        XsdFacetValue? length = this.FindFacet(values, "length");
        XsdFacetValue? minimumLength = this.FindFacet(values, "minLength");
        XsdFacetValue? maximumLength = this.FindFacet(values, "maxLength");
        XsdFacetValue? minimumInclusive = this.FindFacet(
            values,
            "minInclusive");
        XsdFacetValue? minimumExclusive = this.FindFacet(
            values,
            "minExclusive");
        XsdFacetValue? maximumInclusive = this.FindFacet(
            values,
            "maxInclusive");
        XsdFacetValue? maximumExclusive = this.FindFacet(
            values,
            "maxExclusive");
        if (length != null
            && (minimumLength != null || maximumLength != null))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ConflictingFacets,
                "The length facet cannot be combined with minLength or maxLength in one restriction.",
                length.Span);
        }
        if (minimumInclusive != null && minimumExclusive != null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ConflictingFacets,
                "A restriction cannot contain both minInclusive and minExclusive.",
                minimumExclusive.Span);
        }
        if (maximumInclusive != null && maximumExclusive != null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ConflictingFacets,
                "A restriction cannot contain both maxInclusive and maxExclusive.",
                maximumExclusive.Span);
        }
        XsdFacetValue? total = this.FindFacet(values, "totalDigits");
        XsdFacetValue? fraction = this.FindFacet(values, "fractionDigits");
        if (total != null && fraction != null
            && this.IsPositiveInteger(total.LexicalValue)
            && this.IsUnsignedInteger(fraction.LexicalValue)
            && this.ParseBigInteger(fraction.LexicalValue)
                > this.ParseBigInteger(total.LexicalValue))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ConflictingFacets,
                "fractionDigits cannot exceed totalDigits.",
                fraction.Span);
        }
    }

    internal string EffectiveWhitespace(
        string builtInName,
        XsdFacetValue[] facets)
    {
        XsdFacetValue? facet = this.FindFacet(facets, "whiteSpace");
        if (facet != null)
        {
            if (facet.LexicalValue == "preserve"
                || facet.LexicalValue == "replace"
                || facet.LexicalValue == "collapse")
            {
                facet.NormalizedValue = facet.LexicalValue;
                return facet.LexicalValue;
            }
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidFacet,
                "The whiteSpace facet requires preserve, replace, or collapse.",
                facet.Span);
        }
        if (builtInName == "string")
        {
            return "preserve";
        }
        if (builtInName == "normalizedString")
        {
            return "replace";
        }
        return "collapse";
    }

    internal XsdEnumerationItem[] MapEnumeration(
        XsdSimpleTypeDefinition definition,
        string whitespace)
    {
        System.Collections.Generic.List<XsdEnumerationItem> result =
            new System.Collections.Generic.List<XsdEnumerationItem>();
        int index = 0;
        while (index < definition.Facets.Length)
        {
            XsdFacetValue facet = definition.Facets[index];
            if (facet.Name == "enumeration")
            {
                string normalized = this.NormalizeWhitespace(
                    facet.LexicalValue,
                    whitespace);
                facet.NormalizedValue = normalized;
                XsdValueParseResult parsed = this.ParseDefinitionValue(
                    definition,
                    normalized,
                    facet.Token);
                if (!parsed.Success || parsed.Value == null)
                {
                    this.AddDiagnostic(
                        XsdDiagnosticCodes.InvalidFacet,
                        "The enumeration member is not in the value space of xsd:"
                            + definition.BuiltInBaseName + ".",
                        facet.Span);
                }
                else if (this.SatisfiesEnumerationWhitespace(
                    facet.LexicalValue,
                    whitespace,
                    definition.BuiltInBaseName)
                    && this.SatisfiesFilteringFacets(
                    parsed,
                    definition,
                    whitespace)
                    && !this.ContainsEnumerationValue(
                        result,
                        parsed.CanonicalValue))
                {
                    result.Add(new XsdEnumerationItem
                    {
                        LexicalValue = facet.LexicalValue,
                        CanonicalValue = parsed.CanonicalValue,
                        Value = parsed.Value,
                        Span = facet.Span
                    });
                }
            }
            index = index + 1;
        }
        this.SortEnumeration(result, definition.BuiltInBaseName);
        index = 0;
        while (index < result.Count)
        {
            result[index].Identifier = this.IsIntegerDerived(
                definition.BuiltInBaseName)
                ? "int" + result[index].CanonicalValue
                : this.UniqueEnumerationIdentifier(
                    result,
                    index,
                    this.ToIdentifier(result[index].CanonicalValue));
            index = index + 1;
        }
        return result.ToArray();
    }

    internal bool SatisfiesEnumerationWhitespace(
        string lexicalValue,
        string whitespace,
        string builtInName)
    {
        if (!this.IsStringDerived(builtInName)
            || whitespace == "preserve")
        {
            return true;
        }
        if (this.ContainsXmlWhitespace(lexicalValue))
        {
            return false;
        }
        if (whitespace == "replace")
        {
            return true;
        }
        return lexicalValue.Length == 0
            || lexicalValue[0] != ' '
                && lexicalValue[lexicalValue.Length - 1] != ' '
                && lexicalValue.IndexOf("  ", System.StringComparison.Ordinal)
                    < 0;
    }

    internal string UniqueEnumerationIdentifier(
        System.Collections.Generic.List<XsdEnumerationItem> items,
        int currentIndex,
        string candidate)
    {
        string result = candidate;
        int suffix = 1;
        int index = 0;
        while (index < currentIndex)
        {
            if (items[index].Identifier == result)
            {
                result = candidate + "-" + suffix.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
                suffix = suffix + 1;
                index = 0;
            }
            else
            {
                index = index + 1;
            }
        }
        return result;
    }

    internal bool SatisfiesFilteringFacets(
        XsdValueParseResult value,
        XsdSimpleTypeDefinition definition,
        string whitespace)
    {
        if (!this.SatisfiesLengthFacets(value, definition)
            || !this.SatisfiesDigitFacets(value, definition)
            || !this.SatisfiesBoundFacets(value, definition)
            || !this.SatisfiesPatternFacets(value, definition)
            || !this.SatisfiesBaseEnumeration(value, definition.BaseDefinition))
        {
            return false;
        }
        if (whitespace == "replace"
            && this.ContainsXmlWhitespace(value.CanonicalValue))
        {
            return false;
        }
        if (whitespace == "collapse"
            && value.CanonicalValue != this.NormalizeWhitespace(
                value.CanonicalValue,
                "collapse"))
        {
            return false;
        }
        return true;
    }

    internal bool SatisfiesBaseEnumeration(
        XsdValueParseResult value,
        XsdSimpleTypeDefinition? baseDefinition)
    {
        if (baseDefinition == null)
        {
            return true;
        }
        bool hasEnumeration = this.FindFacet(
            baseDefinition.Facets,
            "enumeration") != null;
        if (hasEnumeration)
        {
            bool found = false;
            int index = 0;
            while (index < baseDefinition.EnumerationItems.Length)
            {
                if (baseDefinition.EnumerationItems[index].CanonicalValue
                    == value.CanonicalValue)
                {
                    found = true;
                    break;
                }
                index = index + 1;
            }
            if (!found)
            {
                return false;
            }
        }
        return this.SatisfiesBaseEnumeration(
            value,
            baseDefinition.BaseDefinition);
    }

    internal bool SatisfiesPatternFacets(
        XsdValueParseResult value,
        XsdSimpleTypeDefinition definition)
    {
        int index = 0;
        while (index < definition.EffectiveFacets.Length)
        {
            XsdFacetValue facet = definition.EffectiveFacets[index];
            if (facet.Name == "pattern")
            {
                PatternParseResult parsed = new PatternParser(
                    facet.LexicalValue).ParsePattern();
                if (parsed.Root != null)
                {
                    PatternProgram program = new PatternProgram
                    {
                        Root = parsed.Root
                    };
                    if (!program.Matches(value.CanonicalValue))
                    {
                        return false;
                    }
                }
            }
            index = index + 1;
        }
        return true;
    }

    internal bool SatisfiesLengthFacets(
        XsdValueParseResult value,
        XsdSimpleTypeDefinition definition)
    {
        if (definition.BuiltInBaseName == "QName"
            || definition.BuiltInBaseName == "NOTATION")
        {
            return true;
        }
        int length = value.Value == null
            ? 0
            : value.Value.Kind == AsnValueKind.OctetString
                ? value.Value.Octets.Length
                : value.Value.Kind == AsnValueKind.SequenceOf
                    ? value.Value.Items.Length
                    : this.CountScalars(value.CanonicalValue);
        XsdFacetValue? exact = this.FindFacet(definition.EffectiveFacets, "length");
        XsdFacetValue? minimum = this.FindFacet(
            definition.EffectiveFacets,
            "minLength");
        XsdFacetValue? maximum = this.FindFacet(
            definition.EffectiveFacets,
            "maxLength");
        return (exact == null || !this.IsUnsignedInteger(exact.LexicalValue)
                || length == this.ParseBoundedInteger(exact.LexicalValue))
            && (minimum == null
                || !this.IsUnsignedInteger(minimum.LexicalValue)
                || length >= this.ParseBoundedInteger(minimum.LexicalValue))
            && (maximum == null
                || !this.IsUnsignedInteger(maximum.LexicalValue)
                || length <= this.ParseBoundedInteger(maximum.LexicalValue));
    }

    internal bool SatisfiesDigitFacets(
        XsdValueParseResult value,
        XsdSimpleTypeDefinition definition)
    {
        XsdFacetValue? total = this.FindFacet(
            definition.EffectiveFacets,
            "totalDigits");
        XsdFacetValue? fraction = this.FindFacet(
            definition.EffectiveFacets,
            "fractionDigits");
        if (total == null && fraction == null)
        {
            return true;
        }
        int totalDigits = this.TotalDigits(value.Value);
        int fractionDigits = this.FractionDigits(value.Value);
        return (total == null || !this.IsUnsignedInteger(total.LexicalValue)
                || totalDigits <= this.ParseBoundedInteger(total.LexicalValue))
            && (fraction == null
                || !this.IsUnsignedInteger(fraction.LexicalValue)
                || fractionDigits <= this.ParseBoundedInteger(
                    fraction.LexicalValue));
    }

    internal bool SatisfiesBoundFacets(
        XsdValueParseResult value,
        XsdSimpleTypeDefinition definition)
    {
        string[] names =
        [
            "minInclusive",
            "minExclusive",
            "maxInclusive",
            "maxExclusive"
        ];
        int index = 0;
        while (index < names.Length)
        {
            XsdFacetValue? facet = this.FindFacet(
                definition.EffectiveFacets,
                names[index]);
            if (facet != null)
            {
                string normalized = this.NormalizeWhitespace(
                    facet.LexicalValue,
                    "collapse");
                XsdValueParseResult bound = this.ParseValue(
                    definition.BuiltInBaseName,
                    normalized,
                    facet.Token);
                if (bound.Success && bound.Value != null && value.Value != null)
                {
                    int comparison = this.CompareValues(
                        value.Value,
                        bound.Value);
                    if (names[index] == "minInclusive" && comparison < 0
                        || names[index] == "minExclusive" && comparison <= 0
                        || names[index] == "maxInclusive" && comparison > 0
                        || names[index] == "maxExclusive" && comparison >= 0)
                    {
                        return false;
                    }
                }
            }
            index = index + 1;
        }
        return true;
    }

    internal AsnConstraint[] MapConstraints(
        XsdSimpleTypeDefinition definition,
        string whitespace)
    {
        System.Collections.Generic.List<AsnConstraint> result =
            new System.Collections.Generic.List<AsnConstraint>();
        if (!definition.Enumerated)
        {
            AsnConstraint? size = this.MapSizeConstraint(definition);
            if (size != null)
            {
                result.Add(size);
            }
            AsnConstraint? range = this.MapRangeConstraint(definition);
            if (range != null)
            {
                result.Add(range);
            }
            this.AddDigitConstraints(result, definition);
            this.AddPatternConstraints(result, definition);
            this.AddEnumerationConstraint(result, definition);
            this.AddWhitespaceConstraints(result, definition, whitespace);
        }
        return result.ToArray();
    }

    internal AsnConstraint? MapSizeConstraint(
        XsdSimpleTypeDefinition definition)
    {
        if (definition.BuiltInBaseName == "QName"
            || definition.BuiltInBaseName == "NOTATION")
        {
            return null;
        }
        XsdFacetValue? exact = this.FindFacet(definition.Facets, "length");
        XsdFacetValue? minimum = this.FindFacet(
            definition.Facets,
            "minLength");
        XsdFacetValue? maximum = this.FindFacet(
            definition.Facets,
            "maxLength");
        if (exact == null && minimum == null && maximum == null)
        {
            return null;
        }
        if (exact != null && !this.IsUnsignedInteger(exact.LexicalValue)
            || minimum != null
                && !this.IsUnsignedInteger(minimum.LexicalValue)
            || maximum != null
                && !this.IsUnsignedInteger(maximum.LexicalValue))
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidFacet,
                "Length facets require non-negative integer values.",
                exact != null ? exact.Span
                    : minimum != null ? minimum.Span : maximum == null
                        ? null : maximum.Span);
            return null;
        }
        System.Numerics.BigInteger lower = System.Numerics.BigInteger.Zero;
        System.Numerics.BigInteger upper = System.Numerics.BigInteger.Zero;
        bool hasUpper = false;
        if (exact != null)
        {
            lower = this.ParseBigInteger(exact.LexicalValue);
            upper = lower;
            hasUpper = true;
        }
        else
        {
            if (minimum != null)
            {
                lower = this.ParseBigInteger(minimum.LexicalValue);
            }
            if (maximum != null)
            {
                upper = this.ParseBigInteger(maximum.LexicalValue);
                hasUpper = true;
            }
        }
        if (hasUpper && lower > upper)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ConflictingFacets,
                "The effective minimum length exceeds the maximum length.",
                maximum == null ? exact == null ? null : exact.Span
                    : maximum.Span);
        }
        string text = exact != null
            ? "(SIZE(" + lower.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "))"
            : "(SIZE(" + lower.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ".."
                + (hasUpper ? upper.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                    : "MAX") + "))";
        return new AsnConstraint
        {
            Kind = AsnConstraintKind.Size,
            Elements =
            [
                new AsnConstraintElement
                {
                    Kind = AsnConstraintKind.ValueRange,
                    HasLowerBound = true,
                    LowerBound = lower,
                    HasUpperBound = hasUpper,
                    UpperBound = upper,
                    UpperEndpointUnbounded = !hasUpper,
                    Text = text
                }
            ],
            Span = exact != null ? exact.Span
                : minimum != null ? minimum.Span : maximum == null
                    ? null : maximum.Span
        };
    }

    internal AsnConstraint? MapRangeConstraint(
        XsdSimpleTypeDefinition definition)
    {
        XsdFacetValue? lowerInclusive = this.FindFacet(
            definition.Facets,
            "minInclusive");
        XsdFacetValue? lowerExclusive = this.FindFacet(
            definition.Facets,
            "minExclusive");
        XsdFacetValue? upperInclusive = this.FindFacet(
            definition.Facets,
            "maxInclusive");
        XsdFacetValue? upperExclusive = this.FindFacet(
            definition.Facets,
            "maxExclusive");
        if (lowerInclusive == null && lowerExclusive == null
            && upperInclusive == null && upperExclusive == null)
        {
            return null;
        }
        if (this.IsDateTimeDerived(definition.BuiltInBaseName))
        {
            return this.CreateUserConstraint(
                "date/time bounds: " + this.JoinFacetValues(
                    definition.Facets,
                    ["minInclusive", "minExclusive", "maxInclusive", "maxExclusive"]),
                lowerInclusive != null ? lowerInclusive.Span
                    : lowerExclusive != null ? lowerExclusive.Span
                    : upperInclusive != null ? upperInclusive.Span
                    : upperExclusive == null ? null : upperExclusive.Span);
        }
        XsdFacetValue? lowerFacet = lowerInclusive == null
            ? lowerExclusive
            : lowerInclusive;
        XsdFacetValue? upperFacet = upperInclusive == null
            ? upperExclusive
            : upperInclusive;
        XsdValueParseResult lower = lowerFacet == null
            ? new XsdValueParseResult()
            : this.ParseValue(
                definition.BuiltInBaseName,
                this.NormalizeWhitespace(lowerFacet.LexicalValue, "collapse"),
                lowerFacet.Token);
        XsdValueParseResult upper = upperFacet == null
            ? new XsdValueParseResult()
            : this.ParseValue(
                definition.BuiltInBaseName,
                this.NormalizeWhitespace(upperFacet.LexicalValue, "collapse"),
                upperFacet.Token);
        if (lowerFacet != null && !lower.Success
            || upperFacet != null && !upper.Success)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidFacet,
                "A range facet value is outside the base type value space.",
                lowerFacet != null && !lower.Success ? lowerFacet.Span
                    : upperFacet == null ? null : upperFacet.Span);
            return null;
        }
        bool lowerIncluded = lowerInclusive != null;
        bool upperIncluded = upperInclusive != null;
        if (lower.Value != null && upper.Value != null
            && this.CompareValues(lower.Value, upper.Value) > 0)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.ConflictingFacets,
                "The effective lower bound exceeds the upper bound.",
                upperFacet == null ? null : upperFacet.Span);
        }
        string lowerText = lowerFacet == null
            ? "MIN"
            : lower.CanonicalValue;
        string upperText = upperFacet == null
            ? "MAX"
            : upper.CanonicalValue;
        string text = lowerFacet != null && upperFacet != null
            && lowerIncluded && upperIncluded
            && lower.CanonicalValue == upper.CanonicalValue
            ? "(" + lower.CanonicalValue + ")"
            : "(" + lowerText + (lowerIncluded ? ".." : "<..")
                + (upperIncluded ? "" : "<") + upperText + ")";
        AsnConstraintElement element = new AsnConstraintElement
        {
            Kind = lowerFacet != null && upperFacet != null
                && lowerIncluded && upperIncluded
                && lower.CanonicalValue == upper.CanonicalValue
                ? AsnConstraintKind.SingleValue
                : AsnConstraintKind.ValueRange,
            LowerValue = lower.Value,
            UpperValue = upper.Value,
            LowerEndpointUnbounded = lowerFacet == null,
            UpperEndpointUnbounded = upperFacet == null,
            LowerEndpointIncluded = lowerIncluded,
            UpperEndpointIncluded = upperIncluded,
            Text = text
        };
        if (lower.Value != null && lower.Value.HasIntegerValue)
        {
            element.HasLowerBound = true;
            element.LowerBound = lower.Value.IntegerValue;
        }
        if (upper.Value != null && upper.Value.HasIntegerValue)
        {
            element.HasUpperBound = true;
            element.UpperBound = upper.Value.IntegerValue;
        }
        return new AsnConstraint
        {
            Kind = element.Kind,
            Elements = [element],
            Span = lowerFacet == null
                ? upperFacet == null ? null : upperFacet.Span
                : lowerFacet.Span
        };
    }

    internal void AddDigitConstraints(
        System.Collections.Generic.List<AsnConstraint> result,
        XsdSimpleTypeDefinition definition)
    {
        XsdFacetValue? total = this.FindFacet(
            definition.Facets,
            "totalDigits");
        XsdFacetValue? fraction = this.FindFacet(
            definition.Facets,
            "fractionDigits");
        if (total != null)
        {
            if (!this.IsPositiveInteger(total.LexicalValue))
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidFacet,
                    "The totalDigits facet requires a positive integer.",
                    total.Span);
            }
            else
            {
                result.Add(this.CreateUserConstraint(
                    "totalDigits=" + total.LexicalValue,
                    total.Span));
            }
        }
        if (fraction != null)
        {
            if (!this.IsUnsignedInteger(fraction.LexicalValue))
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidFacet,
                    "The fractionDigits facet requires a non-negative integer.",
                    fraction.Span);
            }
            else
            {
                result.Add(this.CreateUserConstraint(
                    "fractionDigits=" + fraction.LexicalValue,
                    fraction.Span));
            }
        }
    }

    internal void AddPatternConstraints(
        System.Collections.Generic.List<AsnConstraint> result,
        XsdSimpleTypeDefinition definition)
    {
        int index = 0;
        while (index < definition.Facets.Length)
        {
            XsdFacetValue facet = definition.Facets[index];
            if (facet.Name == "pattern")
            {
                string escaped = facet.LexicalValue.Replace(
                    "*/",
                    "*&#x2F;");
                result.Add(this.CreateUserConstraint(
                    "XML representation of the XSD pattern \""
                        + escaped + "\"",
                    facet.Span));
            }
            index = index + 1;
        }
    }

    internal void AddEnumerationConstraint(
        System.Collections.Generic.List<AsnConstraint> result,
        XsdSimpleTypeDefinition definition)
    {
        if (definition.EnumerationItems.Length == 0)
        {
            return;
        }
        System.Collections.Generic.List<AsnConstraintElement> elements =
            new System.Collections.Generic.List<AsnConstraintElement>();
        int index = 0;
        while (index < definition.EnumerationItems.Length)
        {
            XsdEnumerationItem item = definition.EnumerationItems[index];
            elements.Add(new AsnConstraintElement
            {
                Kind = AsnConstraintKind.SingleValue,
                Value = item.Value,
                Text = item.CanonicalValue
            });
            index = index + 1;
        }
        result.Add(new AsnConstraint
        {
            Kind = elements.Count == 1
                ? AsnConstraintKind.SingleValue
                : AsnConstraintKind.Union,
            Elements = elements.ToArray(),
            Span = definition.EnumerationItems[0].Span
        });
    }

    internal void AddWhitespaceConstraints(
        System.Collections.Generic.List<AsnConstraint> result,
        XsdSimpleTypeDefinition definition,
        string whitespace)
    {
        XsdFacetValue? facet = this.FindFacet(
            definition.Facets,
            "whiteSpace");
        if (facet == null || whitespace == "preserve"
            || !this.IsRestrictedString(definition.BuiltInBaseName))
        {
            return;
        }
        result.Add(new AsnConstraint
        {
            Kind = AsnConstraintKind.PermittedAlphabet,
            Elements =
            [
                new AsnConstraintElement
                {
                    Kind = AsnConstraintKind.PermittedAlphabet,
                    Text = "(FROM ({0,0,0,32}..{0,16,255,255}))"
                }
            ],
            Span = facet.Span
        });
        if (whitespace == "collapse")
        {
            result.Add(new AsnConstraint
            {
                Kind = AsnConstraintKind.Pattern,
                Elements =
                [
                    new AsnConstraintElement
                    {
                        Kind = AsnConstraintKind.Pattern,
                        Text = "(PATTERN \"([^ ]([^ ]| [^ ])*)?\")"
                    }
                ],
                Span = facet.Span
            });
        }
    }

    internal AsnConstraint CreateUserConstraint(
        string body,
        AsnSourceSpan? span)
    {
        return new AsnConstraint
        {
            Kind = AsnConstraintKind.UserDefined,
            UserDefinedBody = body,
            RequiresApplicationValidation = true,
            Span = span
        };
    }

    internal AsnEncodingInstruction[] MapXerInstructions(
        XsdSimpleTypeDefinition definition,
        string whitespace)
    {
        System.Collections.Generic.List<AsnEncodingInstruction> result =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        if (definition.Variety == XsdSimpleTypeVariety.List)
        {
            result.Add(new AsnEncodingInstruction
            {
                Name = "LIST",
                Kind = AsnInstructionKind.List
            });
        }
        if (definition.Variety == XsdSimpleTypeVariety.Union)
        {
            result.Add(new AsnEncodingInstruction
            {
                Name = "USE-UNION",
                Kind = AsnInstructionKind.UseUnion
            });
        }
        if (definition.BuiltInBaseName == "QName"
            || definition.BuiltInBaseName == "NOTATION")
        {
            result.Add(new AsnEncodingInstruction
            {
                Name = "USE-QNAME",
                Kind = AsnInstructionKind.UseQName
            });
        }
        if (definition.Enumerated
            && this.IsIntegerDerived(definition.BuiltInBaseName))
        {
            result.Add(new AsnEncodingInstruction
            {
                Name = "USE-NUMBER",
                Kind = AsnInstructionKind.UseNumber
            });
        }
        if (definition.Enumerated
            && this.IsStringDerived(definition.BuiltInBaseName)
            && (whitespace == "preserve" || whitespace == "replace"))
        {
            string[] targets = new string[definition.EnumerationItems.Length];
            int index = 0;
            while (index < targets.Length)
            {
                targets[index] = definition.EnumerationItems[index].Identifier;
                index = index + 1;
            }
            result.Add(new AsnEncodingInstruction
            {
                Name = "TEXT",
                Kind = AsnInstructionKind.Text,
                Targets = targets
            });
        }
        XsdFacetValue? facet = this.FindFacet(
            definition.Facets,
            "whiteSpace");
        bool textAssigned = result.Count > 0
            && result[result.Count - 1].Kind == AsnInstructionKind.Text;
        if (facet != null && whitespace != "preserve"
            && (!definition.Enumerated || textAssigned))
        {
            result.Add(new AsnEncodingInstruction
            {
                Name = "WHITESPACE",
                Kind = AsnInstructionKind.Whitespace,
                Arguments = [whitespace == "replace" ? "REPLACE" : "COLLAPSE"],
                Span = facet.Span
            });
        }
        return result.ToArray();
    }

    internal string RenderEnumeration(XsdSimpleTypeDefinition definition)
    {
        string result = "ENUMERATED {";
        int index = 0;
        while (index < definition.EnumerationItems.Length)
        {
            if (index > 0)
            {
                result = result + ", ";
            }
            XsdEnumerationItem item = definition.EnumerationItems[index];
            result = result + item.Identifier;
            if (this.IsIntegerDerived(definition.BuiltInBaseName))
            {
                result = result + "(" + item.CanonicalValue + ")";
            }
            index = index + 1;
        }
        return result + "}";
    }

    internal string RenderConstrainedType(
        string baseExpression,
        AsnConstraint[] constraints)
    {
        string result = baseExpression;
        int index = 0;
        while (index < constraints.Length)
        {
            AsnConstraint constraint = constraints[index];
            if (constraint.Kind == AsnConstraintKind.UserDefined)
            {
                result = result + " (CONSTRAINED BY { /* "
                    + constraint.UserDefinedBody + " */ })";
            }
            else if (constraint.Elements.Length > 0)
            {
                if (constraint.Kind == AsnConstraintKind.Union)
                {
                    result = result + " (";
                    int elementIndex = 0;
                    while (elementIndex < constraint.Elements.Length)
                    {
                        if (elementIndex > 0)
                        {
                            result = result + " | ";
                        }
                        result = result + constraint.Elements[elementIndex].Text;
                        elementIndex = elementIndex + 1;
                    }
                    result = result + ")";
                }
                else
                {
                    result = result + " " + constraint.Elements[0].Text;
                }
            }
            index = index + 1;
        }
        return result;
    }

    internal XsdValueParseResult ParseDefinitionValue(
        XsdSimpleTypeDefinition definition,
        string text,
        XerXmlToken? token)
    {
        if (definition.Variety == XsdSimpleTypeVariety.List)
        {
            return this.ParseListValue(definition, text, token);
        }
        if (definition.Variety == XsdSimpleTypeVariety.Union)
        {
            return this.ParseUnionValue(definition, text, token);
        }
        return this.ParseValue(definition.BuiltInBaseName, text, token);
    }

    internal XsdValueParseResult ParseListValue(
        XsdSimpleTypeDefinition definition,
        string text,
        XerXmlToken? token)
    {
        if (definition.ItemType == null)
        {
            return new XsdValueParseResult();
        }
        string[] lexicalItems = this.SplitXmlWhitespace(text);
        AsnValue[] values = new AsnValue[lexicalItems.Length];
        string canonical = "";
        int index = 0;
        while (index < lexicalItems.Length)
        {
            XsdValueParseResult item = this.ParseTypeUseValue(
                definition.ItemType,
                lexicalItems[index],
                token);
            if (!item.Success || item.Value == null)
            {
                return new XsdValueParseResult();
            }
            values[index] = item.Value;
            if (index > 0)
            {
                canonical = canonical + " ";
            }
            canonical = canonical + item.CanonicalValue;
            index = index + 1;
        }
        return new XsdValueParseResult
        {
            Success = true,
            CanonicalValue = canonical,
            Value = new AsnValue
            {
                Kind = AsnValueKind.SequenceOf,
                Items = values,
                TextValue = canonical
            }
        };
    }

    internal XsdValueParseResult ParseUnionValue(
        XsdSimpleTypeDefinition definition,
        string text,
        XerXmlToken? token)
    {
        int index = 0;
        while (index < definition.UnionAlternatives.Length)
        {
            XsdUnionAlternative alternative =
                definition.UnionAlternatives[index];
            XsdSimpleTypeUse? use = alternative.TypeUse;
            if (use != null)
            {
                XsdValueParseResult member = this.ParseTypeUseValue(
                    use,
                    text,
                    token);
                if (member.Success && member.Value != null)
                {
                    return new XsdValueParseResult
                    {
                        Success = true,
                        CanonicalValue = member.CanonicalValue,
                        Value = new AsnValue
                        {
                            Kind = AsnValueKind.Choice,
                            NamedValues =
                            [
                                new AsnNamedValue
                                {
                                    Name = alternative.Identifier,
                                    Value = member.Value
                                }
                            ],
                            TextValue = member.CanonicalValue
                        }
                    };
                }
            }
            index = index + 1;
        }
        return new XsdValueParseResult();
    }

    internal XsdValueParseResult ParseTypeUseValue(
        XsdSimpleTypeUse use,
        string text,
        XerXmlToken? token)
    {
        if (use.Definition != null)
        {
            string normalized = text;
            if (use.Definition.Variety == XsdSimpleTypeVariety.List)
            {
                normalized = this.NormalizeWhitespace(text, "collapse");
            }
            else if (use.Definition.Variety == XsdSimpleTypeVariety.Atomic)
            {
                normalized = this.NormalizeWhitespace(
                    text,
                    this.EffectiveWhitespace(
                        use.Definition.BuiltInBaseName,
                        use.Definition.EffectiveFacets));
            }
            XsdValueParseResult parsed = this.ParseDefinitionValue(
                use.Definition,
                normalized,
                token);
            if (!parsed.Success
                || !this.SatisfiesBuiltInValueSpace(
                    use.BuiltInTypeName,
                    parsed)
                || !this.SatisfiesFilteringFacets(
                    parsed,
                    use.Definition,
                    use.Definition.Variety == XsdSimpleTypeVariety.Atomic
                        ? this.EffectiveWhitespace(
                            use.Definition.BuiltInBaseName,
                            use.Definition.EffectiveFacets)
                        : "collapse"))
            {
                return new XsdValueParseResult();
            }
            return parsed;
        }
        XsdValueParseResult builtIn = this.ParseValue(
            use.BuiltInTypeName,
            this.NormalizeWhitespace(
                text,
                this.EffectiveWhitespace(use.BuiltInTypeName, [])),
            token);
        return this.SatisfiesBuiltInValueSpace(use.BuiltInTypeName, builtIn)
            ? builtIn
            : new XsdValueParseResult();
    }

    internal bool SatisfiesBuiltInValueSpace(
        string builtInName,
        XsdValueParseResult parsed)
    {
        if (!parsed.Success || parsed.Value == null
            || !parsed.Value.HasIntegerValue)
        {
            return parsed.Success;
        }
        System.Numerics.BigInteger value = parsed.Value.IntegerValue;
        switch (builtInName)
        {
            case "byte":
                return value >= new System.Numerics.BigInteger(-128)
                    && value <= new System.Numerics.BigInteger(127);
            case "short":
                return value >= new System.Numerics.BigInteger(-32768)
                    && value <= new System.Numerics.BigInteger(32767);
            case "int":
                return value >= new System.Numerics.BigInteger(-2147483648L)
                    && value <= new System.Numerics.BigInteger(2147483647);
            case "long":
                return value >= this.ParseBigInteger("-9223372036854775808")
                    && value <= this.ParseBigInteger("9223372036854775807");
            case "negativeInteger":
                return value < System.Numerics.BigInteger.Zero;
            case "nonNegativeInteger":
                return value >= System.Numerics.BigInteger.Zero;
            case "nonPositiveInteger":
                return value <= System.Numerics.BigInteger.Zero;
            case "positiveInteger":
                return value > System.Numerics.BigInteger.Zero;
            case "unsignedByte":
                return value >= System.Numerics.BigInteger.Zero
                    && value <= new System.Numerics.BigInteger(255);
            case "unsignedShort":
                return value >= System.Numerics.BigInteger.Zero
                    && value <= new System.Numerics.BigInteger(65535);
            case "unsignedInt":
                return value >= System.Numerics.BigInteger.Zero
                    && value <= this.ParseBigInteger("4294967295");
            case "unsignedLong":
                return value >= System.Numerics.BigInteger.Zero
                    && value <= this.ParseBigInteger("18446744073709551615");
            default:
                return true;
        }
    }

    internal XsdValueParseResult ParseValue(
        string builtInName,
        string text,
        XerXmlToken? token)
    {
        if (this.IsIntegerDerived(builtInName))
        {
            return this.ParseIntegerValue(text);
        }
        if (builtInName == "decimal")
        {
            return this.ParseDecimalValue(text, false);
        }
        if (builtInName == "float" || builtInName == "double")
        {
            return this.ParseDecimalValue(text, true);
        }
        if (builtInName == "boolean")
        {
            if (text == "true" || text == "1")
            {
                return this.CreateTextValue(
                    AsnValueKind.Boolean,
                    "true");
            }
            if (text == "false" || text == "0")
            {
                return this.CreateTextValue(
                    AsnValueKind.Boolean,
                    "false");
            }
            return new XsdValueParseResult();
        }
        if (builtInName == "hexBinary")
        {
            return this.ParseHexBinaryValue(text);
        }
        if (builtInName == "base64Binary")
        {
            return this.ParseBase64BinaryValue(text);
        }
        if (builtInName == "QName" || builtInName == "NOTATION")
        {
            return this.ParseQNameValue(text, token);
        }
        if (this.IsDateTimeDerived(builtInName))
        {
            return this.ParseTimeValue(builtInName, text);
        }
        return this.CreateTextValue(AsnValueKind.CharacterString, text);
    }

    internal XsdValueParseResult ParseIntegerValue(string text)
    {
        if (!this.IsSignedInteger(text))
        {
            return new XsdValueParseResult();
        }
        System.Numerics.BigInteger value = this.ParseBigInteger(text);
        string canonical = value.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        return new XsdValueParseResult
        {
            Success = true,
            CanonicalValue = canonical,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = value,
                TextValue = canonical
            }
        };
    }

    internal XsdValueParseResult ParseDecimalValue(
        string text,
        bool allowExponent)
    {
        if (text == "INF" || text == "-INF" || text == "NaN")
        {
            if (!allowExponent)
            {
                return new XsdValueParseResult();
            }
            AsnRealKind kind = text == "INF"
                ? AsnRealKind.PositiveInfinity
                : text == "-INF"
                    ? AsnRealKind.NegativeInfinity
                    : AsnRealKind.NotANumber;
            return new XsdValueParseResult
            {
                Success = true,
                CanonicalValue = text,
                Value = new AsnValue
                {
                    Kind = AsnValueKind.Real,
                    RealValue = new AsnRealValue
                    {
                        Kind = kind,
                        Normalized = true
                    },
                    TextValue = text
                }
            };
        }
        if (!this.IsDecimalLexical(text, allowExponent))
        {
            return new XsdValueParseResult();
        }
        RealNormalizationResult normalized = new RealValueNormalizer()
            .NormalizeRealValue(new AsnRealValue
            {
                Kind = AsnRealKind.FiniteDecimal,
                HasDecimalValue = true,
                DecimalText = text
            });
        if (!normalized.Success || normalized.Value == null)
        {
            return new XsdValueParseResult();
        }
        string canonical = normalized.Value.Kind == AsnRealKind.Zero
            || normalized.Value.Kind == AsnRealKind.MinusZero
            ? "0"
            : normalized.Value.DecimalText;
        return new XsdValueParseResult
        {
            Success = true,
            CanonicalValue = canonical,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Real,
                RealValue = normalized.Value,
                TextValue = canonical
            }
        };
    }

    internal XsdValueParseResult ParseHexBinaryValue(string text)
    {
        string compact = this.RemoveXmlWhitespace(text);
        if (compact.Length % 2 != 0)
        {
            return new XsdValueParseResult();
        }
        byte[] octets = new byte[compact.Length / 2];
        int index = 0;
        while (index < octets.Length)
        {
            int high = this.HexValue(compact[index * 2]);
            int low = this.HexValue(compact[index * 2 + 1]);
            if (high < 0 || low < 0)
            {
                return new XsdValueParseResult();
            }
            octets[index] = (byte)(high * 16 + low);
            index = index + 1;
        }
        return new XsdValueParseResult
        {
            Success = true,
            CanonicalValue = this.ToUpperHex(octets),
            Value = new AsnValue
            {
                Kind = AsnValueKind.OctetString,
                Octets = octets,
                TextValue = this.ToUpperHex(octets)
            }
        };
    }

    internal XsdValueParseResult ParseBase64BinaryValue(string text)
    {
        string compact = this.RemoveXmlWhitespace(text);
        if (!this.IsBase64Lexical(compact))
        {
            return new XsdValueParseResult();
        }
        byte[] octets = System.Convert.FromBase64String(compact);
        string canonical = System.Convert.ToBase64String(octets);
        return new XsdValueParseResult
        {
            Success = true,
            CanonicalValue = canonical,
            Value = new AsnValue
            {
                Kind = AsnValueKind.OctetString,
                Octets = octets,
                TextValue = canonical
            }
        };
    }

    internal XsdValueParseResult ParseQNameValue(
        string text,
        XerXmlToken? token)
    {
        int colon = text.IndexOf(':');
        if (colon == 0 || colon == text.Length - 1
            || colon >= 0 && text.IndexOf(':', colon + 1) >= 0)
        {
            return new XsdValueParseResult();
        }
        string prefix = colon < 0 ? "" : text.Substring(0, colon);
        string localName = colon < 0 ? text : text.Substring(colon + 1);
        if (!this.IsXmlName(localName)
            || prefix.Length > 0 && !this.IsXmlName(prefix))
        {
            return new XsdValueParseResult();
        }
        string namespaceUri = this.ResolveNamespace(token, prefix);
        if (prefix.Length > 0 && namespaceUri.Length == 0)
        {
            return new XsdValueParseResult();
        }
        System.Collections.Generic.List<AsnNamedValue> components =
            new System.Collections.Generic.List<AsnNamedValue>();
        if (namespaceUri.Length > 0)
        {
            components.Add(new AsnNamedValue
            {
                Name = "uri",
                Value = new AsnValue
                {
                    Kind = AsnValueKind.CharacterString,
                    TextValue = namespaceUri
                }
            });
        }
        components.Add(new AsnNamedValue
        {
            Name = "name",
            Value = new AsnValue
            {
                Kind = AsnValueKind.CharacterString,
                TextValue = localName
            }
        });
        return new XsdValueParseResult
        {
            Success = true,
            CanonicalValue = "{" + namespaceUri + "}" + localName,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Sequence,
                NamedValues = components.ToArray(),
                TextValue = text
            }
        };
    }

    internal XsdValueParseResult ParseTimeValue(
        string builtInName,
        string text)
    {
        if (text.Length == 0)
        {
            return new XsdValueParseResult();
        }
        AsnTimeKind kind = builtInName == "duration"
            ? AsnTimeKind.Duration
            : builtInName == "date"
                ? AsnTimeKind.Date
                : builtInName == "time"
                    ? AsnTimeKind.TimeOfDay
                    : builtInName == "dateTime"
                        ? AsnTimeKind.DateTime
                        : AsnTimeKind.Time;
        return new XsdValueParseResult
        {
            Success = true,
            CanonicalValue = text,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Time,
                TimeValue = new AsnTimeValue
                {
                    Kind = kind,
                    Text = text
                },
                TextValue = text
            }
        };
    }

    internal XsdValueParseResult CreateTextValue(
        AsnValueKind kind,
        string text)
    {
        return new XsdValueParseResult
        {
            Success = true,
            CanonicalValue = text,
            Value = new AsnValue
            {
                Kind = kind,
                TextValue = text
            }
        };
    }

    internal int CompareValues(AsnValue first, AsnValue second)
    {
        if (first.HasIntegerValue && second.HasIntegerValue)
        {
            return first.IntegerValue.CompareTo(second.IntegerValue);
        }
        if (first.RealValue != null && second.RealValue != null)
        {
            return this.CompareRealValues(first.RealValue, second.RealValue);
        }
        return string.CompareOrdinal(first.TextValue, second.TextValue);
    }

    internal int CompareRealValues(AsnRealValue first, AsnRealValue second)
    {
        if (first.Kind == AsnRealKind.NotANumber
            || second.Kind == AsnRealKind.NotANumber)
        {
            return 0;
        }
        if (first.Kind == AsnRealKind.PositiveInfinity
            || second.Kind == AsnRealKind.NegativeInfinity)
        {
            return first.Kind == second.Kind ? 0 : 1;
        }
        if (first.Kind == AsnRealKind.NegativeInfinity
            || second.Kind == AsnRealKind.PositiveInfinity)
        {
            return first.Kind == second.Kind ? 0 : -1;
        }
        System.Numerics.BigInteger firstCoefficient =
            first.Kind == AsnRealKind.Zero || first.Kind == AsnRealKind.MinusZero
                ? System.Numerics.BigInteger.Zero
                : first.DecimalCoefficient;
        System.Numerics.BigInteger secondCoefficient =
            second.Kind == AsnRealKind.Zero || second.Kind == AsnRealKind.MinusZero
                ? System.Numerics.BigInteger.Zero
                : second.DecimalCoefficient;
        if (firstCoefficient.Sign != secondCoefficient.Sign)
        {
            return firstCoefficient.Sign.CompareTo(secondCoefficient.Sign);
        }
        if (firstCoefficient.IsZero && secondCoefficient.IsZero)
        {
            return 0;
        }
        int sign = firstCoefficient.Sign;
        string firstDigits = System.Numerics.BigInteger.Abs(firstCoefficient)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        string secondDigits = System.Numerics.BigInteger.Abs(secondCoefficient)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        System.Numerics.BigInteger firstMagnitude = first.DecimalExponent
            + new System.Numerics.BigInteger(firstDigits.Length);
        System.Numerics.BigInteger secondMagnitude = second.DecimalExponent
            + new System.Numerics.BigInteger(secondDigits.Length);
        int magnitude = firstMagnitude.CompareTo(secondMagnitude);
        if (magnitude != 0)
        {
            return sign * magnitude;
        }
        int length = firstDigits.Length > secondDigits.Length
            ? firstDigits.Length
            : secondDigits.Length;
        firstDigits = firstDigits.PadRight(length, '0');
        secondDigits = secondDigits.PadRight(length, '0');
        return sign * string.CompareOrdinal(firstDigits, secondDigits);
    }

    internal int TotalDigits(AsnValue? value)
    {
        if (value == null)
        {
            return 0;
        }
        if (value.HasIntegerValue)
        {
            return System.Numerics.BigInteger.Abs(value.IntegerValue)
                .ToString(System.Globalization.CultureInfo.InvariantCulture)
                .Length;
        }
        if (value.RealValue != null
            && value.RealValue.Kind == AsnRealKind.FiniteDecimal)
        {
            return System.Numerics.BigInteger.Abs(
                value.RealValue.DecimalCoefficient)
                .ToString(System.Globalization.CultureInfo.InvariantCulture)
                .Length;
        }
        return 0;
    }

    internal int FractionDigits(AsnValue? value)
    {
        if (value == null || value.RealValue == null
            || value.RealValue.Kind != AsnRealKind.FiniteDecimal
            || value.RealValue.DecimalExponent >= System.Numerics.BigInteger.Zero)
        {
            return 0;
        }
        System.Numerics.BigInteger digits = -value.RealValue.DecimalExponent;
        return digits > new System.Numerics.BigInteger(int.MaxValue)
            ? int.MaxValue
            : (int)digits;
    }

    internal void SortEnumeration(
        System.Collections.Generic.List<XsdEnumerationItem> items,
        string builtInName)
    {
        int index = 1;
        while (index < items.Count)
        {
            XsdEnumerationItem current = items[index];
            int position = index - 1;
            while (position >= 0
                && this.CompareEnumerationItems(
                    items[position],
                    current,
                    builtInName) > 0)
            {
                items[position + 1] = items[position];
                position = position - 1;
            }
            items[position + 1] = current;
            index = index + 1;
        }
    }

    internal int CompareEnumerationItems(
        XsdEnumerationItem first,
        XsdEnumerationItem second,
        string builtInName)
    {
        if (this.IsIntegerDerived(builtInName)
            && first.Value != null && second.Value != null)
        {
            return this.CompareValues(first.Value, second.Value);
        }
        return string.CompareOrdinal(first.CanonicalValue, second.CanonicalValue);
    }

    internal bool ContainsEnumerationValue(
        System.Collections.Generic.List<XsdEnumerationItem> items,
        string canonicalValue)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].CanonicalValue == canonicalValue)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal string NormalizeWhitespace(string text, string action)
    {
        if (action == "preserve")
        {
            return text;
        }
        System.Text.StringBuilder replaced = new System.Text.StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            replaced.Append(value == '\t' || value == '\n' || value == '\r'
                ? ' '
                : value);
            index = index + 1;
        }
        if (action == "replace")
        {
            return replaced.ToString();
        }
        System.Text.StringBuilder collapsed = new System.Text.StringBuilder();
        bool pendingSpace = false;
        index = 0;
        while (index < replaced.Length)
        {
            char value = replaced[index];
            if (value == ' ')
            {
                pendingSpace = collapsed.Length > 0;
            }
            else
            {
                if (pendingSpace)
                {
                    collapsed.Append(' ');
                }
                collapsed.Append(value);
                pendingSpace = false;
            }
            index = index + 1;
        }
        return collapsed.ToString();
    }

    internal bool ContainsXmlWhitespace(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\t' || text[index] == '\n'
                || text[index] == '\r')
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal string RemoveXmlWhitespace(string text)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (value != ' ' && value != '\t' && value != '\n' && value != '\r')
            {
                result.Append(value);
            }
            index = index + 1;
        }
        return result.ToString();
    }

    internal bool IsSignedInteger(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }
        int index = text[0] == '+' || text[0] == '-' ? 1 : 0;
        if (index == text.Length)
        {
            return false;
        }
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsUnsignedInteger(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsPositiveInteger(string text)
    {
        return this.IsUnsignedInteger(text)
            && this.ParseBigInteger(text) > System.Numerics.BigInteger.Zero;
    }

    internal System.Numerics.BigInteger ParseBigInteger(string text)
    {
        return System.Numerics.BigInteger.Parse(
            text,
            System.Globalization.CultureInfo.InvariantCulture);
    }

    internal int ParseBoundedInteger(string text)
    {
        System.Numerics.BigInteger value = this.ParseBigInteger(text);
        return value > new System.Numerics.BigInteger(int.MaxValue)
            ? int.MaxValue
            : (int)value;
    }

    internal bool IsDecimalLexical(string text, bool allowExponent)
    {
        if (text.Length == 0)
        {
            return false;
        }
        int index = text[0] == '+' || text[0] == '-' ? 1 : 0;
        bool digitSeen = false;
        bool pointSeen = false;
        bool exponentSeen = false;
        bool exponentDigit = false;
        while (index < text.Length)
        {
            char value = text[index];
            if (value >= '0' && value <= '9')
            {
                digitSeen = true;
                if (exponentSeen)
                {
                    exponentDigit = true;
                }
            }
            else if (value == '.' && !pointSeen && !exponentSeen)
            {
                pointSeen = true;
            }
            else if ((value == 'E' || value == 'e') && allowExponent
                && digitSeen && !exponentSeen)
            {
                exponentSeen = true;
                if (index + 1 < text.Length
                    && (text[index + 1] == '+' || text[index + 1] == '-'))
                {
                    index = index + 1;
                }
            }
            else
            {
                return false;
            }
            index = index + 1;
        }
        return digitSeen && (!exponentSeen || exponentDigit);
    }

    internal bool IsBase64Lexical(string text)
    {
        if (text.Length == 0)
        {
            return true;
        }
        if (text.Length % 4 != 0)
        {
            return false;
        }
        int padding = text.EndsWith("==", System.StringComparison.Ordinal)
            ? 2
            : text.EndsWith("=", System.StringComparison.Ordinal) ? 1 : 0;
        int index = 0;
        while (index < text.Length - padding)
        {
            char value = text[index];
            if (!(value >= 'A' && value <= 'Z')
                && !(value >= 'a' && value <= 'z')
                && !(value >= '0' && value <= '9')
                && value != '+' && value != '/')
            {
                return false;
            }
            index = index + 1;
        }
        while (index < text.Length)
        {
            if (text[index] != '=')
            {
                return false;
            }
            index = index + 1;
        }
        return padding == 0
            || padding == 1 && this.Base64Value(text[text.Length - 2]) % 4 == 0
            || padding == 2 && this.Base64Value(text[text.Length - 3]) % 16 == 0;
    }

    internal int Base64Value(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value - 'A';
        }
        if (value >= 'a' && value <= 'z')
        {
            return value - 'a' + 26;
        }
        if (value >= '0' && value <= '9')
        {
            return value - '0' + 52;
        }
        return value == '+' ? 62 : value == '/' ? 63 : -1;
    }

    internal int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        return -1;
    }

    internal string ToUpperHex(byte[] octets)
    {
        const string Digits = "0123456789ABCDEF";
        char[] result = new char[octets.Length * 2];
        int index = 0;
        while (index < octets.Length)
        {
            result[index * 2] = Digits[octets[index] >> 4];
            result[index * 2 + 1] = Digits[octets[index] & 15];
            index = index + 1;
        }
        return new string(result);
    }

    internal bool IsStringDerived(string builtInName)
    {
        switch (builtInName)
        {
            case "string":
            case "normalizedString":
            case "token":
            case "language":
            case "Name":
            case "NCName":
            case "NMTOKEN":
            case "NMTOKENS":
            case "ID":
            case "IDREF":
            case "IDREFS":
            case "ENTITY":
            case "ENTITIES":
                return true;
            default:
                return false;
        }
    }

    internal bool IsBuiltInListType(string builtInName)
    {
        return builtInName == "NMTOKENS"
            || builtInName == "IDREFS"
            || builtInName == "ENTITIES";
    }

    internal bool IsRestrictedString(string builtInName)
    {
        return this.IsStringDerived(builtInName)
            || builtInName == "anyURI";
    }

    internal bool IsIntegerDerived(string builtInName)
    {
        switch (builtInName)
        {
            case "integer":
            case "byte":
            case "short":
            case "int":
            case "long":
            case "negativeInteger":
            case "nonNegativeInteger":
            case "nonPositiveInteger":
            case "positiveInteger":
            case "unsignedByte":
            case "unsignedShort":
            case "unsignedInt":
            case "unsignedLong":
                return true;
            default:
                return false;
        }
    }

    internal bool IsDateTimeDerived(string builtInName)
    {
        switch (builtInName)
        {
            case "date":
            case "dateTime":
            case "duration":
            case "gDay":
            case "gMonth":
            case "gMonthDay":
            case "gYear":
            case "gYearMonth":
            case "time":
                return true;
            default:
                return false;
        }
    }

    internal string ToTypeReference(string name)
    {
        string identifier = this.ToIdentifier(name);
        if (identifier.Length == 0)
        {
            return "XsdType";
        }
        char first = identifier[0];
        return char.ToUpperInvariant(first) + identifier.Substring(1);
    }

    internal string ToIdentifier(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool permitted = current >= 'A' && current <= 'Z'
                || current >= 'a' && current <= 'z'
                || current >= '0' && current <= '9';
            if (permitted)
            {
                result.Append(current);
            }
            else if (result.Length > 0 && result[result.Length - 1] != '-')
            {
                result.Append('-');
            }
            index = index + 1;
        }
        while (result.Length > 0 && result[result.Length - 1] == '-')
        {
            result.Remove(result.Length - 1, 1);
        }
        if (result.Length == 0
            || !(result[0] >= 'A' && result[0] <= 'Z')
                && !(result[0] >= 'a' && result[0] <= 'z'))
        {
            result.Insert(0, "value-");
        }
        return result.ToString();
    }

    internal int CountScalars(string text)
    {
        int count = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (char.IsHighSurrogate(text[index])
                && index + 1 < text.Length
                && char.IsLowSurrogate(text[index + 1]))
            {
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }
            count = count + 1;
        }
        return count;
    }

    internal bool IsXmlName(string text)
    {
        if (text.Length == 0
            || !this.IsXmlNameStart(text[0]))
        {
            return false;
        }
        int index = 1;
        while (index < text.Length)
        {
            char value = text[index];
            if (!this.IsXmlNameStart(value)
                && !(value >= '0' && value <= '9')
                && value != '-' && value != '.')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsXmlNameStart(char value)
    {
        return value == '_' || value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z'
            || value >= '\u0080';
    }

    internal string ResolveNamespace(XerXmlToken? token, string prefix)
    {
        if (token == null)
        {
            return "";
        }
        int index = 0;
        while (index < token.NamespaceBindings.Length)
        {
            XerXmlNamespaceBinding binding = token.NamespaceBindings[index];
            if (binding.Prefix == prefix)
            {
                return binding.NamespaceUri;
            }
            index = index + 1;
        }
        return "";
    }

    internal string[] ResolveQualifiedName(XerXmlToken token, string text)
    {
        int colon = text.IndexOf(':');
        if (colon == 0 || colon == text.Length - 1
            || colon >= 0 && text.IndexOf(':', colon + 1) >= 0)
        {
            return [];
        }
        string prefix = colon < 0 ? "" : text.Substring(0, colon);
        string localName = colon < 0 ? text : text.Substring(colon + 1);
        if (!this.IsXmlName(localName)
            || prefix.Length > 0 && !this.IsXmlName(prefix))
        {
            return [];
        }
        string namespaceUri = this.ResolveNamespace(token, prefix);
        if (prefix.Length > 0 && namespaceUri.Length == 0)
        {
            return [];
        }
        return [namespaceUri, localName];
    }

    internal string JoinFacetValues(
        XsdFacetValue[] facets,
        string[] names)
    {
        string result = "";
        int index = 0;
        while (index < facets.Length)
        {
            if (this.ContainsTextArray(names, facets[index].Name))
            {
                if (result.Length > 0)
                {
                    result = result + ", ";
                }
                result = result + facets[index].Name + "="
                    + facets[index].LexicalValue;
            }
            index = index + 1;
        }
        return result;
    }

    internal bool ContainsTextArray(string[] values, string candidate)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == candidate)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal XsdFacetValue? FindFacet(
        XsdFacetValue[] facets,
        string name)
    {
        int index = 0;
        while (index < facets.Length)
        {
            if (facets[index].Name == name)
            {
                return facets[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdFacetValue[] MergeFacetSets(
        XsdFacetValue[] inherited,
        XsdFacetValue[] declared)
    {
        System.Collections.Generic.List<XsdFacetValue> result =
            new System.Collections.Generic.List<XsdFacetValue>();
        int index = 0;
        while (index < inherited.Length)
        {
            XsdFacetValue facet = inherited[index];
            if ((facet.Name == "pattern" || facet.Name == "enumeration")
                || this.FindFacet(declared, facet.Name) == null)
            {
                result.Add(facet);
            }
            index = index + 1;
        }
        index = 0;
        while (index < declared.Length)
        {
            result.Add(declared[index]);
            index = index + 1;
        }
        return result.ToArray();
    }

    internal XsdSimpleTypeDefinition? FindMappedType(
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

    internal XsdLoadedDocument? FindDocument(
        XsdComponentDefinition component)
    {
        int index = 0;
        while (index < this.Graph.DocumentInstances.Length)
        {
            XsdDocumentInstance instance =
                this.Graph.DocumentInstances[index];
            if (instance.StableId == component.SourceInstanceId)
            {
                return instance.Document;
            }
            index = index + 1;
        }
        return null;
    }

    internal XerXmlToken? FindDirectChild(
        XsdLoadedDocument document,
        XerXmlToken parent,
        string localName)
    {
        XerXmlToken[] children = this.GetDirectChildren(document, parent);
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].NamespaceUri
                    == XsdDocumentLoader.SchemaNamespace
                && children[index].LocalName == localName)
            {
                return children[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XerXmlToken[] GetDirectChildren(
        XsdLoadedDocument document,
        XerXmlToken parent)
    {
        System.Collections.Generic.List<XerXmlToken> result =
            new System.Collections.Generic.List<XerXmlToken>();
        int parentIndex = this.FindTokenIndex(document.Tokens, parent);
        if (parentIndex < 0 || parent.Kind == XerXmlTokenKind.EmptyElement)
        {
            return [];
        }
        int depth = 0;
        int index = parentIndex + 1;
        while (index < document.Tokens.Length)
        {
            XerXmlToken token = document.Tokens[index];
            if (token.Kind == XerXmlTokenKind.EndElement)
            {
                if (depth == 0)
                {
                    break;
                }
                depth = depth - 1;
            }
            else if (token.Kind == XerXmlTokenKind.StartElement)
            {
                if (depth == 0)
                {
                    result.Add(token);
                }
                depth = depth + 1;
            }
            else if (token.Kind == XerXmlTokenKind.EmptyElement
                && depth == 0)
            {
                result.Add(token);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal int FindTokenIndex(XerXmlToken[] tokens, XerXmlToken target)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal bool ContainsText(
        System.Collections.Generic.List<string> values,
        string candidate)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == candidate)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void AddDiagnostic(
        int code,
        string message,
        AsnSourceSpan? span)
    {
        if (this.Diagnostics.Count >= this.Limits.MaximumDiagnostics)
        {
            return;
        }
        this.Diagnostics.Add(new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = AsnDiagnosticCategory.Xsd,
            Code = DiagnosticBag.CreateCode(AsnDiagnosticCategory.Xsd, code),
            Message = message,
            Span = span
        });
    }

    internal XsdSimpleTypeMappingResult CreateResult()
    {
        return new XsdSimpleTypeMappingResult
        {
            Success = this.Diagnostics.Count == 0,
            Types = this.Types.ToArray(),
            ValueConstraints = this.ValueConstraints.ToArray(),
            Diagnostics = this.Diagnostics.ToArray()
        };
    }
}
