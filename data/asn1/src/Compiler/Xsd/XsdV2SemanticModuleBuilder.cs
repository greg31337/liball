namespace asn1;

internal class XsdV2SemanticModuleBuilder
{
    internal SymbolTable BuildTable()
    {
        XsdV2SupportModule support = XsdV2SupportModuleCatalog.GetModule();
        ModuleSyntax module = this.CreateModuleSyntax(support);
        SymbolTableEntry[] symbols =
            new SymbolTableEntry[support.Types.Length];
        int index = 0;
        while (index < support.Types.Length)
        {
            symbols[index] = this.CreateTypeEntry(
                support.Types[index],
                index);
            index = index + 1;
        }
        SymbolTable table = new SymbolTable
        {
            ModuleName = support.ModuleName,
            ObjectIdentifier = "2.1.0.0.2.2",
            PredeclaredXsdSupport = true,
            Module = module,
            Symbols = symbols,
            TypeSymbols = symbols
        };
        module.SymbolTable = table;
        return table;
    }

    internal ModuleSyntax CreateModuleSyntax(XsdV2SupportModule support)
    {
        DefinitiveOidComponentSyntax[] oid =
            new DefinitiveOidComponentSyntax[support.ObjectIdentifier.Length];
        int index = 0;
        while (index < oid.Length)
        {
            oid[index] = new DefinitiveOidComponentSyntax
            {
                Number = support.ObjectIdentifier[index],
                HasNumber = true
            };
            index = index + 1;
        }
        return new ModuleSyntax
        {
            Name = support.ModuleName,
            DefinitiveOidComponents = oid,
            HasDefinitiveOid = true,
            DefinitiveIri = support.InternationalizedResourceIdentifier,
            HasDefinitiveIri = true,
            EncodingReference = "XER",
            EncodingReferenceSpecified = true,
            TagDefault = AsnTaggingMode.Automatic,
            TagDefaultSpecified = true,
            ExportsAll = true,
            XerModifiedEncodings = true,
            XerControlNamespaceUri =
                "http://www.w3.org/2001/XMLSchema-instance",
            XerControlNamespacePrefix = "xsi"
        };
    }

    internal SymbolTableEntry CreateTypeEntry(
        XsdV2SupportTypeDefinition definition,
        int order)
    {
        SymbolTableEntry entry = new SymbolTableEntry
        {
            Name = definition.Name,
            QualifiedName = "XSDv2." + definition.Name,
            CandidateKinds = [AsnAssignmentKind.Type],
            Kind = AsnAssignmentKind.Type,
            DeclarationOrder = order,
            Exported = true,
            Predeclared = true,
            KindClassified = true,
            TypeResolutionState = 2
        };
        ResolvedTypeSyntax resolution = this.CreateTypeResolution(
            definition.Name);
        resolution.SourceDeclaration = entry;
        resolution.EffectiveDeclaration = entry;
        entry.DeclaredTypeSyntax = resolution.SourceSyntax;
        entry.ResolvedType = resolution;
        return entry;
    }

    internal ResolvedTypeSyntax CreateTypeResolution(string name)
    {
        if (name == "Decimal" || name == "Double" || name == "Float")
        {
            return this.CreatePrimitiveResolution(
                name,
                AsnTypeKind.Real,
                name == "Decimal"
                    ? [this.CreateInstruction(
                        "DECIMAL",
                        AsnInstructionKind.Decimal,
                        [])]
                    : []);
        }
        if (name == "Int" || name == "Long" || name == "Short"
            || name == "UnsignedInt" || name == "UnsignedLong"
            || name == "UnsignedShort")
        {
            return this.CreatePrimitiveResolution(
                name,
                AsnTypeKind.Integer,
                []);
        }
        if (name == "ENTITIES" || name == "IDREFS"
            || name == "NMTOKENS")
        {
            return this.CreateListResolution(name);
        }
        if (name == "QName" || name == "NOTATION")
        {
            return this.CreateQNameResolution(name);
        }
        if (name == "Date" || name == "Duration" || name == "GYear"
            || name == "GYearMonth"
            || name == "GenericTimeTypeChoice")
        {
            return this.CreateUnionResolution(name);
        }
        if (name == "AnyType" || name == "AnyType-nillable")
        {
            return this.CreateAnyTypeResolution(
                name,
                name == "AnyType-nillable");
        }
        string whitespace = this.FindWhitespaceAction(name);
        AsnEncodingInstruction[] instructions = whitespace.Length == 0
            ? []
            : [this.CreateInstruction(
                "WHITESPACE",
                AsnInstructionKind.Whitespace,
                [whitespace])];
        return this.CreatePrimitiveResolution(
            name,
            AsnTypeKind.Utf8String,
            instructions);
    }

    internal string FindWhitespaceAction(string name)
    {
        if (name == "AnyURI" || name == "Language" || name == "Token"
            || name == "DateTimeType")
        {
            return "COLLAPSE";
        }
        return name == "NormalizedString" ? "REPLACE" : "";
    }

    internal ResolvedTypeSyntax CreatePrimitiveResolution(
        string name,
        AsnTypeKind kind,
        AsnEncodingInstruction[] instructions)
    {
        AsnType type = new AsnType
        {
            Name = name,
            Kind = kind
        };
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = type,
            EncodingReference = "XER",
            EncodingInstructions = instructions
        };
        ResolvedTypeSyntax result = new ResolvedTypeSyntax
        {
            SourceSyntax = syntax,
            SourceType = type,
            EffectiveType = type,
            EffectiveKind = kind,
            Success = true
        };
        syntax.Resolution = result;
        return result;
    }

    internal ResolvedTypeSyntax CreateListResolution(string name)
    {
        ResolvedTypeSyntax item = this.CreatePrimitiveResolution(
            name + "-item",
            AsnTypeKind.Utf8String,
            []);
        AsnType type = new AsnType
        {
            Name = name,
            Kind = AsnTypeKind.SequenceOf,
            ElementType = item.EffectiveType
        };
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = type,
            EncodingReference = "XER",
            EncodingInstructions =
            [
                this.CreateInstruction(
                    "LIST",
                    AsnInstructionKind.List,
                    [])
            ]
        };
        ResolvedTypeSyntax result = new ResolvedTypeSyntax
        {
            SourceSyntax = syntax,
            SourceType = type,
            EffectiveType = type,
            EffectiveKind = AsnTypeKind.SequenceOf,
            InnerType = item,
            Success = true
        };
        syntax.Resolution = result;
        return result;
    }

    internal ResolvedTypeSyntax CreateQNameResolution(string name)
    {
        ResolvedTypeSyntax uri = this.CreatePrimitiveResolution(
            name + "-uri",
            AsnTypeKind.Utf8String,
            []);
        ResolvedTypeSyntax local = this.CreatePrimitiveResolution(
            name + "-name",
            AsnTypeKind.Utf8String,
            []);
        ResolvedComponentSyntax[] components =
        [
            this.CreateComponentResolution("uri", uri, true),
            this.CreateComponentResolution("name", local, false)
        ];
        AsnType type = this.CreateStructuredType(
            name,
            AsnTypeKind.Sequence,
            components);
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = type,
            EncodingReference = "XER",
            EncodingInstructions =
            [
                this.CreateInstruction(
                    "USE-QNAME",
                    AsnInstructionKind.UseQName,
                    [])
            ]
        };
        ResolvedTypeSyntax result = new ResolvedTypeSyntax
        {
            SourceSyntax = syntax,
            SourceType = type,
            EffectiveType = type,
            EffectiveKind = AsnTypeKind.Sequence,
            Components = components,
            Success = true
        };
        syntax.Resolution = result;
        return result;
    }

    internal ResolvedTypeSyntax CreateUnionResolution(string name)
    {
        ResolvedTypeSyntax supported = this.CreatePrimitiveResolution(
            name + "-supported",
            AsnTypeKind.VisibleString,
            []);
        ResolvedTypeSyntax lexical = this.CreatePrimitiveResolution(
            name + "-lexical",
            AsnTypeKind.Utf8String,
            []);
        ResolvedComponentSyntax[] components =
        [
            this.CreateComponentResolution(
                "asn1supportedvalue",
                supported,
                false),
            this.CreateComponentResolution("othervalues", lexical, false)
        ];
        AsnType type = this.CreateStructuredType(
            name,
            AsnTypeKind.Choice,
            components);
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = type,
            EncodingReference = "XER",
            EncodingInstructions =
            [
                this.CreateInstruction(
                    "USE-UNION",
                    AsnInstructionKind.UseUnion,
                    [])
            ]
        };
        ResolvedTypeSyntax result = new ResolvedTypeSyntax
        {
            SourceSyntax = syntax,
            SourceType = type,
            EffectiveType = type,
            EffectiveKind = AsnTypeKind.Choice,
            Components = components,
            Success = true
        };
        syntax.Resolution = result;
        return result;
    }

    internal ResolvedTypeSyntax CreateAnyTypeResolution(
        string name,
        bool nillable)
    {
        ResolvedTypeSyntax textItem = this.CreatePrimitiveResolution(
            name + "-text",
            AsnTypeKind.Utf8String,
            []);
        ResolvedTypeSyntax textList = this.CreateCollectionResolution(
            name + "-texts",
            textItem,
            []);
        ResolvedComponentSyntax[] components = nillable
            ?
            [
                this.CreateComponentResolution(
                    "embed-values",
                    textList,
                    false),
                this.CreateComponentResolution("attr", textList, false),
                this.CreateComponentResolution("content", textList, true)
            ]
            :
            [
                this.CreateComponentResolution(
                    "embed-values",
                    textList,
                    false),
                this.CreateComponentResolution("attr", textList, false),
                this.CreateComponentResolution("elem-list", textList, false)
            ];
        AsnType type = this.CreateStructuredType(
            name,
            AsnTypeKind.Sequence,
            components);
        System.Collections.Generic.List<AsnEncodingInstruction> instructions =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        instructions.Add(this.CreateInstruction(
            "EMBED-VALUES",
            AsnInstructionKind.EmbedValues,
            []));
        if (nillable)
        {
            instructions.Add(this.CreateInstruction(
                "USE-NIL",
                AsnInstructionKind.UseNil,
                []));
        }
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = type,
            EncodingReference = "XER",
            EncodingInstructions = instructions.ToArray()
        };
        ResolvedTypeSyntax result = new ResolvedTypeSyntax
        {
            SourceSyntax = syntax,
            SourceType = type,
            EffectiveType = type,
            EffectiveKind = AsnTypeKind.Sequence,
            Components = components,
            Success = true
        };
        syntax.Resolution = result;
        return result;
    }

    internal ResolvedTypeSyntax CreateCollectionResolution(
        string name,
        ResolvedTypeSyntax item,
        AsnEncodingInstruction[] instructions)
    {
        AsnType type = new AsnType
        {
            Name = name,
            Kind = AsnTypeKind.SequenceOf,
            ElementType = item.EffectiveType
        };
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = type,
            EncodingReference = "XER",
            EncodingInstructions = instructions
        };
        ResolvedTypeSyntax result = new ResolvedTypeSyntax
        {
            SourceSyntax = syntax,
            SourceType = type,
            EffectiveType = type,
            EffectiveKind = AsnTypeKind.SequenceOf,
            InnerType = item,
            Success = true
        };
        syntax.Resolution = result;
        return result;
    }

    internal ResolvedComponentSyntax CreateComponentResolution(
        string name,
        ResolvedTypeSyntax type,
        bool optional)
    {
        return new ResolvedComponentSyntax
        {
            Component = new AsnComponent
            {
                Name = name,
                Type = type.EffectiveType,
                Optional = optional
            },
            TypeResolution = type
        };
    }

    internal AsnType CreateStructuredType(
        string name,
        AsnTypeKind kind,
        ResolvedComponentSyntax[] components)
    {
        AsnComponent[] fields = new AsnComponent[components.Length];
        int index = 0;
        while (index < components.Length)
        {
            fields[index] = components[index].Component
                ?? new AsnComponent();
            index = index + 1;
        }
        return new AsnType
        {
            Name = name,
            Kind = kind,
            Components = fields
        };
    }

    internal AsnEncodingInstruction CreateInstruction(
        string name,
        AsnInstructionKind kind,
        string[] arguments)
    {
        return new AsnEncodingInstruction
        {
            Name = name,
            Kind = kind,
            Arguments = arguments
        };
    }
}
