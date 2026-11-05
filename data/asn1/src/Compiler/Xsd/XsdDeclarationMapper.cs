namespace asn1;

internal class XsdDeclarationMapper
{
    internal XsdSchemaGraphResult Graph = new XsdSchemaGraphResult();
    internal XsdSimpleTypeMappingResult SimpleTypes =
        new XsdSimpleTypeMappingResult();
    internal AsnResourceLimits Limits = new AsnResourceLimits();
    internal XsdSimpleTypeMapper SimpleMapper = new XsdSimpleTypeMapper();
    internal System.Collections.Generic.List<XsdDeclarationDefinition>
        Declarations =
            new System.Collections.Generic.List<XsdDeclarationDefinition>();
    internal System.Collections.Generic.List<AsnDiagnostic> Diagnostics =
        new System.Collections.Generic.List<AsnDiagnostic>();

    internal XsdDeclarationMappingResult MapDeclarations(
        XsdSchemaGraphResult graph,
        XsdSimpleTypeMappingResult simpleTypes,
        AsnXsdImportOptions options)
    {
        if (graph == null)
        {
            throw new System.ArgumentNullException("graph");
        }
        if (simpleTypes == null)
        {
            throw new System.ArgumentNullException("simpleTypes");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        this.Graph = graph;
        this.SimpleTypes = simpleTypes;
        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        this.Declarations =
            new System.Collections.Generic.List<XsdDeclarationDefinition>();
        this.Diagnostics = new System.Collections.Generic.List<AsnDiagnostic>();
        this.SimpleMapper = new XsdSimpleTypeMapper
        {
            Graph = graph,
            Limits = this.Limits,
            Types = new System.Collections.Generic.List<XsdSimpleTypeDefinition>(
                simpleTypes.Types),
            Diagnostics = new System.Collections.Generic.List<AsnDiagnostic>(),
            ActiveTypes = new System.Collections.Generic.List<string>()
        };
        if (!graph.Success || !simpleTypes.Success)
        {
            return this.CreateResult();
        }
        int index = 0;
        while (index < graph.DocumentInstances.Length)
        {
            this.MapDocument(graph.DocumentInstances[index]);
            index = index + 1;
        }
        simpleTypes.Types = this.SimpleMapper.Types.ToArray();
        index = 0;
        while (index < this.SimpleMapper.Diagnostics.Count)
        {
            this.Diagnostics.Add(this.SimpleMapper.Diagnostics[index]);
            index = index + 1;
        }
        return this.CreateResult();
    }

    internal void MapDocument(XsdDocumentInstance instance)
    {
        XsdLoadedDocument? document = instance.Document;
        if (document == null)
        {
            return;
        }
        System.Collections.Generic.List<XerXmlToken> parents =
            new System.Collections.Generic.List<XerXmlToken>();
        int index = 0;
        while (index < document.Tokens.Length)
        {
            XerXmlToken token = document.Tokens[index];
            if (token.Kind == XerXmlTokenKind.EndElement)
            {
                if (parents.Count > 0)
                {
                    parents.RemoveAt(parents.Count - 1);
                }
            }
            else if (token.Kind == XerXmlTokenKind.StartElement
                || token.Kind == XerXmlTokenKind.EmptyElement)
            {
                XerXmlToken? parent = parents.Count == 0
                    ? null
                    : parents[parents.Count - 1];
                if (token.NamespaceUri == XsdDocumentLoader.SchemaNamespace
                    && (token.LocalName == "element"
                        || token.LocalName == "attribute"))
                {
                    this.MapDeclaration(instance, document, token, parent, index);
                }
                if (token.Kind == XerXmlTokenKind.StartElement)
                {
                    parents.Add(token);
                }
            }
            index = index + 1;
        }
    }

    internal void MapDeclaration(
        XsdDocumentInstance instance,
        XsdLoadedDocument document,
        XerXmlToken token,
        XerXmlToken? parent,
        int tokenIndex)
    {
        if (this.Declarations.Count >= this.Limits.MaximumCollectionElements)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.GraphLimit,
                "The mapped XML Schema declaration count exceeds the configured collection limit.",
                this.TokenSpan(document, token));
            return;
        }
        string kind = token.LocalName;
        XsdComponentDefinition? component = this.FindGlobalComponent(
            instance.StableId,
            token,
            kind);
        bool global = component != null;
        XerXmlAttribute? reference = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "ref");
        string[] referenceName = reference == null
            ? []
            : this.SimpleMapper.ResolveQualifiedName(token, reference.Value);
        XsdComponentDefinition? referenced = referenceName.Length == 0
            ? null
            : this.FindComponent(kind, referenceName[0], referenceName[1]);
        XerXmlToken effectiveToken = referenced == null
            || referenced.Token == null
            ? token
            : referenced.Token;
        XsdDocumentInstance effectiveInstance = referenced == null
            ? instance
            : this.FindDocumentInstance(referenced.SourceInstanceId)
                ?? instance;
        XsdLoadedDocument effectiveDocument = effectiveInstance.Document
            ?? document;
        XerXmlAttribute? nameAttribute = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "name");
        string name = nameAttribute == null ? "" : nameAttribute.Value;
        if (name.Length == 0 && referenceName.Length > 0)
        {
            name = referenceName[1];
        }
        AsnSourceSpan? span = this.TokenSpan(document, token);
        if (name.Length == 0)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidDeclaration,
                "An XML Schema declaration requires a name or a resolvable reference.",
                span);
            return;
        }
        string stableId = component == null
            ? "x694:local-declaration:" + instance.StableId + ":"
                + tokenIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
            : "x694:declaration:" + component.StableId;
        bool qualified = this.IsQualified(
            instance,
            document,
            token,
            kind,
            global,
            referenceName);
        string namespaceUri = qualified
            ? referenceName.Length > 0
                ? referenceName[0]
                : instance.EffectiveTargetNamespace
            : "";
        XsdDeclarationDefinition declaration =
            new XsdDeclarationDefinition
            {
                StableId = stableId,
                SourceComponentId = component == null ? "" : component.StableId,
                Kind = kind,
                Name = name,
                Identifier = this.ToIdentifier(name),
                TypeReferenceName = global ? this.ToTypeReference(name) : "",
                NamespaceUri = namespaceUri,
                SchemaNamespaceUri = instance.EffectiveTargetNamespace,
                Qualified = qualified,
                Global = global,
                Reference = reference != null,
                ReferenceName = referenceName.Length == 0
                    ? ""
                    : referenceName[1],
                ReferenceNamespace = referenceName.Length == 0
                    ? ""
                    : referenceName[0],
                DocumentInstance = instance,
                Document = document,
                Token = token,
                Span = span
            };
        this.MapOccurrence(declaration, token);
        this.MapFlags(
            declaration,
            effectiveDocument,
            token,
            effectiveToken);
        declaration.TypeUse = this.ResolveTypeUse(
            declaration,
            effectiveInstance,
            effectiveDocument,
            effectiveToken);
        if (declaration.TypeUse == null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.UnknownDeclarationType,
                "The type of XML Schema " + kind + " declaration '"
                    + name + "' cannot be resolved.",
                span);
        }
        else
        {
            this.MapValueConstraint(declaration, token, effectiveToken);
            this.RenderDeclaration(declaration);
        }
        declaration.Ignored = kind == "element"
            && global
            && declaration.Abstract;
        this.Declarations.Add(declaration);
    }

    internal bool IsQualified(
        XsdDocumentInstance instance,
        XsdLoadedDocument document,
        XerXmlToken token,
        string kind,
        bool global,
        string[] referenceName)
    {
        if (global || referenceName.Length > 0)
        {
            return (referenceName.Length > 0
                    ? referenceName[0]
                    : instance.EffectiveTargetNamespace).Length > 0;
        }
        XerXmlAttribute? form = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "form");
        string value = form == null ? "" : form.Value;
        if (value.Length == 0 && document.SchemaToken != null)
        {
            XerXmlAttribute? defaultForm =
                new XsdDocumentLoader().FindAttribute(
                    document.SchemaToken,
                    "",
                    kind == "element"
                        ? "elementFormDefault"
                        : "attributeFormDefault");
            value = defaultForm == null ? "unqualified" : defaultForm.Value;
        }
        if (value != "qualified" && value != "unqualified")
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidDeclarationFlag,
                "The form of an XML Schema declaration must be qualified or unqualified.",
                this.TokenSpan(document, token));
            return false;
        }
        return value == "qualified"
            && instance.EffectiveTargetNamespace.Length > 0;
    }

    internal void MapOccurrence(
        XsdDeclarationDefinition declaration,
        XerXmlToken token)
    {
        if (declaration.Kind == "attribute")
        {
            if (declaration.Global)
            {
                declaration.MinimumOccurs = System.Numerics.BigInteger.One;
                declaration.MaximumOccurs = System.Numerics.BigInteger.One;
                return;
            }
            XerXmlAttribute? use = new XsdDocumentLoader().FindAttribute(
                token,
                "",
                "use");
            declaration.AttributeUse = use == null ? "optional" : use.Value;
            if (declaration.AttributeUse != "optional"
                && declaration.AttributeUse != "required"
                && declaration.AttributeUse != "prohibited")
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidDeclarationFlag,
                    "An XML Schema attribute use must be optional, required, or prohibited.",
                    declaration.Span);
                declaration.AttributeUse = "optional";
            }
            declaration.Required = declaration.AttributeUse == "required";
            declaration.Prohibited = declaration.AttributeUse == "prohibited";
            declaration.MinimumOccurs = declaration.Required
                ? System.Numerics.BigInteger.One
                : System.Numerics.BigInteger.Zero;
            declaration.MaximumOccurs = declaration.Prohibited
                ? System.Numerics.BigInteger.Zero
                : System.Numerics.BigInteger.One;
            declaration.Optional = declaration.AttributeUse == "optional";
            return;
        }
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
                XsdDiagnosticCodes.InvalidOccurrence,
                "XML Schema occurrence bounds require non-negative integers or unbounded.",
                declaration.Span);
            minimumText = "1";
            maximumText = "1";
        }
        declaration.MinimumOccurs = this.SimpleMapper.ParseBigInteger(
            minimumText);
        declaration.MaximumOccursUnbounded = maximumText == "unbounded";
        declaration.MaximumOccurs = declaration.MaximumOccursUnbounded
            ? System.Numerics.BigInteger.Zero
            : this.SimpleMapper.ParseBigInteger(maximumText);
        if (!declaration.MaximumOccursUnbounded
            && declaration.MinimumOccurs > declaration.MaximumOccurs)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidOccurrence,
                "XML Schema minOccurs cannot exceed maxOccurs.",
                declaration.Span);
        }
        declaration.Optional = declaration.MinimumOccurs.IsZero
            && !declaration.MaximumOccursUnbounded
            && declaration.MaximumOccurs == System.Numerics.BigInteger.One;
        declaration.Repeated = declaration.MaximumOccursUnbounded
            || declaration.MaximumOccurs > System.Numerics.BigInteger.One;
    }

    internal void MapFlags(
        XsdDeclarationDefinition declaration,
        XsdLoadedDocument document,
        XerXmlToken token,
        XerXmlToken effectiveToken)
    {
        if (declaration.Kind == "element")
        {
            declaration.Nillable = this.BooleanAttribute(
                effectiveToken,
                "nillable",
                false,
                declaration.Span);
            declaration.Abstract = this.BooleanAttribute(
                effectiveToken,
                "abstract",
                false,
                declaration.Span);
            declaration.BlockValues = this.EffectiveFlagValues(
                document,
                effectiveToken,
                "block",
                "blockDefault",
                ["extension", "restriction", "substitution"],
                declaration.Span);
            declaration.FinalValues = this.EffectiveFlagValues(
                document,
                effectiveToken,
                "final",
                "finalDefault",
                ["extension", "restriction"],
                declaration.Span);
        }
        XerXmlAttribute? defaultValue = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "default");
        XerXmlAttribute? fixedValue = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "fixed");
        if (defaultValue == null && fixedValue == null && token != effectiveToken)
        {
            defaultValue = new XsdDocumentLoader().FindAttribute(
                effectiveToken,
                "",
                "default");
            fixedValue = new XsdDocumentLoader().FindAttribute(
                effectiveToken,
                "",
                "fixed");
        }
        declaration.HasDefault = defaultValue != null;
        declaration.DefaultValue = defaultValue == null ? "" : defaultValue.Value;
        declaration.HasFixed = fixedValue != null;
        declaration.FixedValue = fixedValue == null ? "" : fixedValue.Value;
        if (declaration.HasDefault && declaration.HasFixed)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidValueConstraint,
                "An XML Schema declaration cannot specify both default and fixed values.",
                declaration.Span);
        }
    }

    internal bool BooleanAttribute(
        XerXmlToken token,
        string name,
        bool fallback,
        AsnSourceSpan? span)
    {
        XerXmlAttribute? attribute = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            name);
        if (attribute == null)
        {
            return fallback;
        }
        if (attribute.Value == "true" || attribute.Value == "1")
        {
            return true;
        }
        if (attribute.Value == "false" || attribute.Value == "0")
        {
            return false;
        }
        this.AddDiagnostic(
            XsdDiagnosticCodes.InvalidDeclarationFlag,
            "The XML Schema " + name + " flag requires a boolean value.",
            span);
        return fallback;
    }

    internal string[] EffectiveFlagValues(
        XsdLoadedDocument document,
        XerXmlToken token,
        string name,
        string defaultName,
        string[] allowed,
        AsnSourceSpan? span)
    {
        XerXmlAttribute? attribute = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            name);
        if (attribute == null && document.SchemaToken != null)
        {
            attribute = new XsdDocumentLoader().FindAttribute(
                document.SchemaToken,
                "",
                defaultName);
        }
        if (attribute == null || attribute.Value.Length == 0)
        {
            return [];
        }
        string[] values = this.SimpleMapper.SplitXmlWhitespace(attribute.Value);
        if (values.Length == 1 && values[0] == "#all")
        {
            return allowed;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!this.SimpleMapper.ContainsTextArray(allowed, values[index]))
            {
                this.AddDiagnostic(
                    XsdDiagnosticCodes.InvalidDeclarationFlag,
                    "The XML Schema " + name + " flag contains an invalid derivation value.",
                    span);
                return [];
            }
            index = index + 1;
        }
        return values;
    }

    internal XsdDeclarationTypeUse? ResolveTypeUse(
        XsdDeclarationDefinition declaration,
        XsdDocumentInstance instance,
        XsdLoadedDocument document,
        XerXmlToken token)
    {
        XerXmlAttribute? type = new XsdDocumentLoader().FindAttribute(
            token,
            "",
            "type");
        if (type != null && type.Value.Length > 0)
        {
            XsdSimpleTypeUse? simple = this.SimpleMapper.ResolveNamedTypeUse(
                token,
                type.Value);
            if (simple != null)
            {
                return this.FromSimpleTypeUse(simple, token, declaration.Span);
            }
            string[] name = this.SimpleMapper.ResolveQualifiedName(
                token,
                type.Value);
            if (name.Length > 0 && name[0] == XsdDocumentLoader.SchemaNamespace
                && name[1] == "anyType")
            {
                XsdBuiltInTypeDefinition? anyType =
                    XsdBuiltInTypeCatalog.FindType(name[0], name[1]);
                return anyType == null ? null : new XsdDeclarationTypeUse
                {
                    StableId = anyType.StableId,
                    Name = anyType.XsdName,
                    NamespaceUri = name[0],
                    Kind = "complex",
                    AsnTypeDefinition = anyType.AsnTypeDefinition,
                    BuiltIn = true,
                    Token = token,
                    Span = declaration.Span
                };
            }
            XsdComponentDefinition? complex = name.Length == 0
                ? null
                : this.FindComponent("complexType", name[0], name[1]);
            return complex == null ? null : new XsdDeclarationTypeUse
            {
                StableId = complex.StableId,
                Name = complex.Name,
                NamespaceUri = complex.NamespaceUri,
                Kind = "complex",
                AsnTypeDefinition = this.ToTypeReference(complex.Name),
                ComplexType = complex,
                Token = complex.Token,
                Span = complex.Span,
                PendingComplexMapping = true
            };
        }
        XerXmlToken? simpleToken = this.SimpleMapper.FindDirectChild(
            document,
            token,
            "simpleType");
        if (simpleToken != null)
        {
            XsdSimpleTypeDefinition? existing = this.FindSimpleTypeByToken(
                simpleToken);
            if (existing != null)
            {
                return this.FromSimpleTypeUse(
                    new XsdSimpleTypeUse
                    {
                        StableId = existing.StableId,
                        Name = existing.Name,
                        NamespaceUri = existing.NamespaceUri,
                        BuiltInTypeName = existing.BuiltInBaseName,
                        AsnTypeDefinition = existing.AsnTypeDefinition,
                        Anonymous = true,
                        Definition = existing,
                        XerInstructions = existing.XerInstructions
                    },
                    simpleToken,
                    declaration.Span);
            }
            XsdSimpleTypeDefinition owner = new XsdSimpleTypeDefinition
            {
                StableId = declaration.StableId + ":type-owner",
                Name = declaration.Name,
                NamespaceUri = instance.EffectiveTargetNamespace,
                Document = document,
                Token = token,
                Span = declaration.Span
            };
            XsdSimpleTypeUse? anonymous = this.SimpleMapper
                .CreateAnonymousTypeUse(owner, document, simpleToken, "type");
            return anonymous == null
                ? null
                : this.FromSimpleTypeUse(
                    anonymous,
                    simpleToken,
                    declaration.Span);
        }
        XerXmlToken? complexToken = this.SimpleMapper.FindDirectChild(
            document,
            token,
            "complexType");
        if (complexToken != null)
        {
            return new XsdDeclarationTypeUse
            {
                StableId = declaration.StableId + ":anonymous:complex",
                Name = "",
                NamespaceUri = instance.EffectiveTargetNamespace,
                Kind = "complex",
                AsnTypeDefinition = "SEQUENCE {}",
                Anonymous = true,
                PendingComplexMapping = true,
                Token = complexToken,
                Span = declaration.Span
            };
        }
        XsdBuiltInTypeDefinition? fallback = XsdBuiltInTypeCatalog.FindType(
            XsdDocumentLoader.SchemaNamespace,
            "anyType");
        return fallback == null ? null : new XsdDeclarationTypeUse
        {
            StableId = fallback.StableId,
            Name = fallback.XsdName,
            NamespaceUri = XsdDocumentLoader.SchemaNamespace,
            Kind = "complex",
            AsnTypeDefinition = fallback.AsnTypeDefinition,
            BuiltIn = true,
            Token = token,
            Span = declaration.Span
        };
    }

    internal XsdDeclarationTypeUse FromSimpleTypeUse(
        XsdSimpleTypeUse simple,
        XerXmlToken token,
        AsnSourceSpan? span)
    {
        return new XsdDeclarationTypeUse
        {
            StableId = simple.StableId,
            Name = simple.Name,
            NamespaceUri = simple.NamespaceUri,
            Kind = "simple",
            AsnTypeDefinition = simple.AsnTypeDefinition,
            Anonymous = simple.Anonymous,
            BuiltIn = simple.Definition == null,
            SimpleType = simple.Definition,
            Token = token,
            Span = span,
            XerInstructions = simple.XerInstructions
        };
    }

    internal void MapValueConstraint(
        XsdDeclarationDefinition declaration,
        XerXmlToken token,
        XerXmlToken effectiveToken)
    {
        if (!declaration.HasDefault && !declaration.HasFixed
            || declaration.TypeUse == null)
        {
            return;
        }
        XsdValueConstraintDefinition? existing =
            this.FindExistingValueConstraint(declaration);
        if (existing != null)
        {
            declaration.ValueConstraint = existing;
            return;
        }
        if (declaration.TypeUse.Kind != "simple")
        {
            return;
        }
        XsdSimpleTypeUse simple = this.ToSimpleTypeUse(declaration.TypeUse);
        string lexical = declaration.HasFixed
            ? declaration.FixedValue
            : declaration.DefaultValue;
        XsdValueConstraintDefinition mapped =
            new XsdValueConstraintDefinition
            {
                StableId = "x694:value-constraint:" + declaration.StableId,
                ComponentId = declaration.StableId,
                ComponentKind = declaration.Kind,
                ComponentName = declaration.Name,
                Kind = declaration.HasFixed ? "fixed" : "default",
                LexicalValue = lexical,
                TypeName = simple.Name,
                TypeNamespace = simple.NamespaceUri,
                BuiltInTypeName = simple.BuiltInTypeName,
                TypeInstructions = simple.XerInstructions,
                IgnoredForQName = declaration.Kind == "element"
                    && this.SimpleMapper.IsQNameTypeUse(simple),
                Span = declaration.Span
            };
        if (mapped.IgnoredForQName)
        {
            declaration.ValueConstraint = mapped;
            return;
        }
        XsdValueParseResult parsed = this.SimpleMapper.ParseTypeUseValue(
            simple,
            lexical,
            effectiveToken);
        if (!parsed.Success || parsed.Value == null)
        {
            this.AddDiagnostic(
                XsdDiagnosticCodes.InvalidValueConstraint,
                "The declaration value is outside its simple type value space.",
                declaration.Span);
            return;
        }
        mapped.CanonicalValue = parsed.CanonicalValue;
        mapped.Value = parsed.Value;
        mapped.DefaultForEmptyInstruction = new AsnEncodingInstruction
        {
            Name = "DEFAULT-FOR-EMPTY",
            Kind = AsnInstructionKind.DefaultForEmpty,
            Arguments = [parsed.CanonicalValue],
            Span = declaration.Span
        };
        if (declaration.HasFixed)
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
                Span = declaration.Span
            };
        }
        declaration.ValueConstraint = mapped;
    }

    internal XsdSimpleTypeUse ToSimpleTypeUse(XsdDeclarationTypeUse type)
    {
        return new XsdSimpleTypeUse
        {
            StableId = type.StableId,
            Name = type.Name,
            NamespaceUri = type.NamespaceUri,
            BuiltInTypeName = type.SimpleType == null
                ? type.Name
                : type.SimpleType.BuiltInBaseName,
            AsnTypeDefinition = type.AsnTypeDefinition,
            Anonymous = type.Anonymous,
            Definition = type.SimpleType,
            XerInstructions = type.XerInstructions
        };
    }

    internal XsdValueConstraintDefinition? FindExistingValueConstraint(
        XsdDeclarationDefinition declaration)
    {
        int index = 0;
        while (index < this.SimpleTypes.ValueConstraints.Length)
        {
            XsdValueConstraintDefinition candidate =
                this.SimpleTypes.ValueConstraints[index];
            if (candidate.ComponentName == declaration.Name
                && candidate.ComponentKind == declaration.Kind
                && candidate.ComponentId == declaration.SourceComponentId)
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    internal void RenderDeclaration(XsdDeclarationDefinition declaration)
    {
        XsdDeclarationTypeUse type = declaration.TypeUse
            ?? new XsdDeclarationTypeUse();
        string core = type.AsnTypeDefinition;
        System.Collections.Generic.List<AsnEncodingInstruction> instructions =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        this.AddInstructions(instructions, type.XerInstructions);
        if (declaration.Nillable)
        {
            XsdBuiltInTypeDefinition? builtIn = type.BuiltIn
                ? XsdBuiltInTypeCatalog.FindType(
                    type.NamespaceUri,
                    type.Name)
                : null;
            core = builtIn != null
                && builtIn.NillableAsnTypeDefinition.Length > 0
                ? builtIn.NillableAsnTypeDefinition
                : "SEQUENCE {content " + core + " OPTIONAL}";
            instructions.Add(new AsnEncodingInstruction
            {
                Name = "USE-NIL",
                Kind = AsnInstructionKind.UseNil,
                Span = declaration.Span
            });
        }
        AsnEncodingInstruction? nameInstruction = this.NameInstruction(
            declaration);
        if (nameInstruction != null)
        {
            instructions.Add(nameInstruction);
        }
        if (declaration.NamespaceUri.Length > 0)
        {
            instructions.Add(new AsnEncodingInstruction
            {
                Name = "NAMESPACE",
                Kind = AsnInstructionKind.Namespace,
                Arguments = [declaration.NamespaceUri],
                Span = declaration.Span
            });
        }
        if (declaration.Kind == "attribute")
        {
            instructions.Add(new AsnEncodingInstruction
            {
                Name = "ATTRIBUTE",
                Kind = AsnInstructionKind.Attribute,
                Span = declaration.Span
            });
        }
        if (declaration.ValueConstraint != null
            && declaration.ValueConstraint.DefaultForEmptyInstruction != null)
        {
            instructions.Add(
                declaration.ValueConstraint.DefaultForEmptyInstruction);
        }
        declaration.AsnTypeDefinition = core;
        declaration.XerInstructions = instructions.ToArray();
        declaration.OccurrenceTypeDefinition = this.RenderOccurrence(
            declaration,
            core);
    }

    internal string RenderOccurrence(
        XsdDeclarationDefinition declaration,
        string core)
    {
        if (declaration.Prohibited)
        {
            return "ABSENT";
        }
        if (!declaration.MaximumOccursUnbounded
            && declaration.MaximumOccurs.IsZero)
        {
            return "ABSENT";
        }
        if (!declaration.Repeated)
        {
            if (declaration.Kind == "attribute"
                && declaration.Optional
                && declaration.ValueConstraint != null
                && declaration.ValueConstraint.Value != null)
            {
                return core + " DEFAULT "
                    + declaration.ValueConstraint.CanonicalValue;
            }
            return core + (declaration.Optional ? " OPTIONAL" : "");
        }
        string size = "";
        if (!(declaration.MinimumOccurs.IsZero
            && declaration.MaximumOccursUnbounded))
        {
            string minimum = declaration.MinimumOccurs.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            string maximum = declaration.MaximumOccursUnbounded
                ? "MAX"
                : declaration.MaximumOccurs.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            size = minimum == maximum
                ? " (SIZE(" + minimum + "))"
                : " (SIZE(" + minimum + ".." + maximum + "))";
        }
        return "SEQUENCE" + size + " OF " + declaration.Identifier
            + " " + core;
    }

    internal AsnEncodingInstruction? NameInstruction(
        XsdDeclarationDefinition declaration)
    {
        string mappedName = declaration.Global
            ? declaration.TypeReferenceName
            : declaration.Identifier;
        if (mappedName == declaration.Name)
        {
            return null;
        }
        string[] arguments;
        if (mappedName.Length == declaration.Name.Length
            && mappedName.Substring(1) == declaration.Name.Substring(1)
            && char.ToLowerInvariant(mappedName[0])
                == char.ToLowerInvariant(declaration.Name[0]))
        {
            arguments =
            [
                "AS",
                declaration.Global ? "UNCAPITALIZED" : "CAPITALIZED"
            ];
        }
        else
        {
            arguments = ["AS", declaration.Name];
        }
        return new AsnEncodingInstruction
        {
            Name = "NAME",
            Kind = AsnInstructionKind.Name,
            Arguments = arguments,
            Span = declaration.Span
        };
    }

    internal void AddInstructions(
        System.Collections.Generic.List<AsnEncodingInstruction> target,
        AsnEncodingInstruction[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    internal string ToTypeReference(string value)
    {
        string cleaned = this.CleanName(value, true);
        if (cleaned.Length == 0)
        {
            return "X";
        }
        if (cleaned[0] >= 'a' && cleaned[0] <= 'z')
        {
            return char.ToUpperInvariant(cleaned[0]) + cleaned.Substring(1);
        }
        return cleaned[0] >= '0' && cleaned[0] <= '9'
            ? "X" + cleaned
            : cleaned;
    }

    internal string ToIdentifier(string value)
    {
        string cleaned = this.CleanName(value, false);
        if (cleaned.Length == 0)
        {
            return "x";
        }
        if (cleaned[0] >= 'A' && cleaned[0] <= 'Z')
        {
            return char.ToLowerInvariant(cleaned[0]) + cleaned.Substring(1);
        }
        return cleaned[0] >= '0' && cleaned[0] <= '9'
            ? "x" + cleaned
            : cleaned;
    }

    internal string CleanName(string value, bool typeReference)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == ' ' || current == '.' || current == '_')
            {
                current = '-';
            }
            bool permitted = current >= 'A' && current <= 'Z'
                || current >= 'a' && current <= 'z'
                || current >= '0' && current <= '9'
                || current == '-';
            if (permitted
                && (current != '-' || result.Length == 0
                    || result[result.Length - 1] != '-'))
            {
                result.Append(current);
            }
            index = index + 1;
        }
        while (result.Length > 0 && result[0] == '-')
        {
            result.Remove(0, 1);
        }
        while (result.Length > 0 && result[result.Length - 1] == '-')
        {
            result.Remove(result.Length - 1, 1);
        }
        return result.ToString();
    }

    internal XsdComponentDefinition? FindGlobalComponent(
        string instanceId,
        XerXmlToken token,
        string kind)
    {
        int index = 0;
        while (index < this.Graph.Components.Length)
        {
            XsdComponentDefinition component = this.Graph.Components[index];
            if (component.SourceInstanceId == instanceId
                && component.Kind == kind
                && component.Token == token)
            {
                return component;
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdComponentDefinition? FindComponent(
        string kind,
        string namespaceUri,
        string name)
    {
        int index = 0;
        while (index < this.Graph.Components.Length)
        {
            XsdComponentDefinition component = this.Graph.Components[index];
            if (component.Kind == kind
                && component.NamespaceUri == namespaceUri
                && component.Name == name)
            {
                return component;
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdSimpleTypeDefinition? FindSimpleTypeByToken(
        XerXmlToken token)
    {
        int index = 0;
        while (index < this.SimpleMapper.Types.Count)
        {
            if (this.SimpleMapper.Types[index].Token == token)
            {
                return this.SimpleMapper.Types[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal XsdDocumentInstance? FindDocumentInstance(string stableId)
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

    internal XsdDeclarationMappingResult CreateResult()
    {
        System.Collections.Generic.List<XsdDeclarationDefinition> elements =
            new System.Collections.Generic.List<XsdDeclarationDefinition>();
        System.Collections.Generic.List<XsdDeclarationDefinition> attributes =
            new System.Collections.Generic.List<XsdDeclarationDefinition>();
        int index = 0;
        while (index < this.Declarations.Count)
        {
            if (this.Declarations[index].Kind == "element")
            {
                elements.Add(this.Declarations[index]);
            }
            else
            {
                attributes.Add(this.Declarations[index]);
            }
            index = index + 1;
        }
        return new XsdDeclarationMappingResult
        {
            Success = this.Diagnostics.Count == 0,
            Declarations = this.Declarations.ToArray(),
            Elements = elements.ToArray(),
            Attributes = attributes.ToArray(),
            Diagnostics = this.Diagnostics.ToArray()
        };
    }
}
