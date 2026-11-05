using iso20022;

namespace iso20022.Tests;

public class RepositoryTypeSystemDataTests
{
    [Test]
    public void EveryDataTypeFamilyCanBeRepresentedWithoutLosingSourceTypeIdentity()
    {
        (DataTypeKind Kind, string SourceName)[] families =
        {
            (DataTypeKind.Text, "Text"),
            (DataTypeKind.String, "String"),
            (DataTypeKind.Identifier, "IdentifierSet"),
            (DataTypeKind.Boolean, "Boolean"),
            (DataTypeKind.Indicator, "Indicator"),
            (DataTypeKind.Decimal, "Decimal"),
            (DataTypeKind.Amount, "Amount"),
            (DataTypeKind.Quantity, "Quantity"),
            (DataTypeKind.Rate, "Rate"),
            (DataTypeKind.Binary, "Binary"),
            (DataTypeKind.Date, "Date"),
            (DataTypeKind.DateTime, "DateTime"),
            (DataTypeKind.Day, "Day"),
            (DataTypeKind.Duration, "Duration"),
            (DataTypeKind.Month, "Month"),
            (DataTypeKind.MonthDay, "MonthDay"),
            (DataTypeKind.Time, "Time"),
            (DataTypeKind.Year, "Year"),
            (DataTypeKind.YearMonth, "YearMonth"),
            (DataTypeKind.CodeSet, "CodeSet"),
            (DataTypeKind.Schema, "SchemaType"),
            (DataTypeKind.ExternalSchema, "ExternalSchema"),
            (DataTypeKind.UserDefined, "UserDefined")
        };
        RepositorySnapshot snapshot = new RepositorySnapshot();

        foreach ((DataTypeKind kind, string sourceName) in families)
        {
            snapshot.DataTypes.Add(new DataTypeDefinition
            {
                Identifier = "_" + sourceName,
                Kind = kind,
                SourceTypeName = sourceName,
                Name = sourceName + "Definition"
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.DataTypes, Has.Count.EqualTo(23));
            Assert.That(snapshot.DataTypes.Select(item => item.Kind), Is.EqualTo(families.Select(item => item.Kind)));
            Assert.That(snapshot.DataTypes.Select(item => item.SourceTypeName),
                Is.EqualTo(families.Select(item => item.SourceName)));
            Assert.That(snapshot.DataTypes.Select(item => item.Identifier).Distinct().ToList(), Has.Count.EqualTo(23));
        });
    }

    [Test]
    public void DataTypeFacetsAndFamilySpecificMetadataRetainParsedAndLexicalValues()
    {
        DataTypeFacet minimumLength = new DataTypeFacet
        {
            Name = "minLength",
            Value = "1",
            HasIntegerValue = true,
            IntegerValue = 1
        };
        DataTypeFacet decimalMinimum = new DataTypeFacet
        {
            Name = "minInclusive",
            Value = "0.00",
            DecimalValue = new IsoDecimalValue
            {
                Value = decimal.Zero,
                Scale = 2,
                LexicalValue = "0.00"
            }
        };
        DataTypeFacet pattern = new DataTypeFacet
        {
            Name = "pattern",
            Value = "[A-Z]{3}"
        };
        DataTypeFacet enumeration = new DataTypeFacet
        {
            Name = "enumeration"
        };
        enumeration.EnumerationValues.AddRange(new[] { "ACTV", "INAC" });

        DataTypeDefinition identifier = new DataTypeDefinition
        {
            Identifier = "_identifier",
            Kind = DataTypeKind.Identifier,
            IdentificationScheme = "urn:example:identification-scheme"
        };
        identifier.Facets.Add(minimumLength);
        identifier.Facets.Add(pattern);
        identifier.Facets.Add(enumeration);

        DataTypeDefinition indicator = new DataTypeDefinition
        {
            Identifier = "_indicator",
            Kind = DataTypeKind.Indicator,
            MeaningWhenTrue = "The instruction is final.",
            MeaningWhenFalse = "The instruction may change."
        };
        DataTypeDefinition rate = new DataTypeDefinition
        {
            Identifier = "_rate",
            Kind = DataTypeKind.Rate,
            BaseValue = new IsoDecimalValue { Value = 100m, Scale = 0, LexicalValue = "100" },
            UnitCode = "PERCENT"
        };
        rate.Facets.Add(decimalMinimum);
        DataTypeDefinition quantity = new DataTypeDefinition
        {
            Identifier = "_quantity",
            Kind = DataTypeKind.Quantity,
            UnitCode = "KILO"
        };
        DataTypeDefinition amount = new DataTypeDefinition
        {
            Identifier = "_amount",
            Kind = DataTypeKind.Amount,
            CurrencyIdentifierSet = Reference("_amount", RepositoryEntityKind.DataType,
                "currencyIdentifierSet", "_currencyCode", RepositoryEntityKind.CodeSet, 0)
        };
        DataTypeDefinition binary = new DataTypeDefinition
        {
            Identifier = "_binary",
            Kind = DataTypeKind.Binary,
            BinaryEncoding = BinaryEncodingKind.Base64
        };
        DataTypeDefinition schema = new DataTypeDefinition
        {
            Identifier = "_schema",
            Kind = DataTypeKind.Schema,
            SchemaTypeName = "anyURI"
        };
        DataTypeDefinition date = new DataTypeDefinition
        {
            Identifier = "_date",
            Kind = DataTypeKind.Date
        };
        date.Facets.Add(new DataTypeFacet { Name = "maxInclusive", Value = "2026-12-31" });
        identifier.BusinessElements.Add(Reference("_identifier", RepositoryEntityKind.DataType,
            "businessElement", "_businessElement", RepositoryEntityKind.BusinessAttribute, 0));
        identifier.MessageMembers.Add(Reference("_identifier", RepositoryEntityKind.DataType,
            "messageMember", "_messageElement", RepositoryEntityKind.MessageAttribute, 0));

        Assert.Multiple(() =>
        {
            Assert.That(identifier.IdentificationScheme, Is.EqualTo("urn:example:identification-scheme"));
            Assert.That(identifier.Facets[0].HasIntegerValue, Is.True);
            Assert.That(identifier.Facets[0].IntegerValue, Is.EqualTo(1));
            Assert.That(identifier.Facets[1].Value, Is.EqualTo("[A-Z]{3}"));
            Assert.That(identifier.Facets[2].EnumerationValues, Is.EqualTo(new[] { "ACTV", "INAC" }));
            Assert.That(rate.BaseValue?.Value, Is.EqualTo(100m));
            Assert.That(rate.Facets.Single().DecimalValue?.Scale, Is.EqualTo(2));
            Assert.That(quantity.UnitCode, Is.EqualTo("KILO"));
            Assert.That(amount.CurrencyIdentifierSet?.TargetIdentifier, Is.EqualTo("_currencyCode"));
            Assert.That(indicator.MeaningWhenTrue, Is.EqualTo("The instruction is final."));
            Assert.That(indicator.MeaningWhenFalse, Is.EqualTo("The instruction may change."));
            Assert.That(binary.BinaryEncoding, Is.EqualTo(BinaryEncodingKind.Base64));
            Assert.That(schema.SchemaTypeName, Is.EqualTo("anyURI"));
            Assert.That(date.Facets.Single().Value, Is.EqualTo("2026-12-31"));
            Assert.That(identifier.BusinessElements.Single().TargetIdentifier, Is.EqualTo("_businessElement"));
            Assert.That(identifier.MessageMembers.Single().TargetIdentifier, Is.EqualTo("_messageElement"));
        });
    }

    [Test]
    public void CodeSetsRetainOwnershipOrderingFacetsAndDerivationGraph()
    {
        CodeSetDefinition baseSet = new CodeSetDefinition
        {
            Identifier = "_baseCodeSet",
            Kind = CodeSetKind.Internal,
            Name = "TransactionStatusCode",
            IdentificationScheme = "urn:example:transaction-status",
            Registration = new RegistrationMetadata { Status = RegistrationStatus.Registered }
        };
        baseSet.Facets.Add(new DataTypeFacet
        {
            Name = "length",
            Value = "4",
            HasIntegerValue = true,
            IntegerValue = 4
        });
        baseSet.Codes.Add(new CodeValueDefinition
        {
            Identifier = "_active",
            Value = "ACTV",
            Name = "Active",
            Definition = "The transaction is active.",
            Owner = Reference("_active", RepositoryEntityKind.CodeValue,
                "owner", "_baseCodeSet", RepositoryEntityKind.CodeSet, 0)
        });
        baseSet.Codes.Add(new CodeValueDefinition
        {
            Identifier = "_inactive",
            Value = "INAC",
            Name = "Inactive",
            Definition = "The transaction is inactive.",
            Owner = Reference("_inactive", RepositoryEntityKind.CodeValue,
                "owner", "_baseCodeSet", RepositoryEntityKind.CodeSet, 0)
        });
        CodeSetDefinition derivedSet = new CodeSetDefinition
        {
            Identifier = "_derivedCodeSet",
            Kind = CodeSetKind.Derived,
            Name = "CurrentTransactionStatusCode",
            BaseCodeSet = Reference("_derivedCodeSet", RepositoryEntityKind.CodeSet,
                "trace", "_baseCodeSet", RepositoryEntityKind.CodeSet, 0)
        };
        baseSet.DerivedCodeSets.Add(Reference("_baseCodeSet", RepositoryEntityKind.CodeSet,
            "derivation", "_derivedCodeSet", RepositoryEntityKind.CodeSet, 0));
        RepositorySnapshot snapshot = new RepositorySnapshot();
        snapshot.CodeSets.Add(baseSet);
        snapshot.CodeSets.Add(derivedSet);

        Assert.Multiple(() =>
        {
            Assert.That(baseSet.Kind, Is.EqualTo(CodeSetKind.Internal));
            Assert.That(baseSet.Facets.Single().IntegerValue, Is.EqualTo(4));
            Assert.That(baseSet.Codes.Select(code => code.Value), Is.EqualTo(new[] { "ACTV", "INAC" }));
            Assert.That(baseSet.Codes.Select(code => code.Name), Is.EqualTo(new[] { "Active", "Inactive" }));
            Assert.That(baseSet.Codes.All(code => code.Owner.TargetIdentifier == "_baseCodeSet"), Is.True);
            Assert.That(derivedSet.BaseCodeSet?.TargetIdentifier, Is.EqualTo("_baseCodeSet"));
            Assert.That(baseSet.DerivedCodeSets.Single().TargetIdentifier, Is.EqualTo("_derivedCodeSet"));
            Assert.That(snapshot.CodeSets.Select(set => set.Identifier),
                Is.EqualTo(new[] { "_baseCodeSet", "_derivedCodeSet" }));
        });
    }

    [Test]
    public void SyntaxEncodingExternalSchemasAndSchemesRetainProcessingGraph()
    {
        SyntaxDefinition syntax = new SyntaxDefinition
        {
            Identifier = "_syntax",
            Name = "XML"
        };
        syntax.PossibleEncodings.Add(Reference("_syntax", RepositoryEntityKind.Syntax,
            "possibleEncodings", "_encoding", RepositoryEntityKind.Encoding, 0));
        syntax.GeneratedForMessageSets.Add(Reference("_syntax", RepositoryEntityKind.Syntax,
            "generatedFor", "_messageSet", RepositoryEntityKind.MessageSet, 0));

        EncodingDefinition encoding = new EncodingDefinition
        {
            Identifier = "_encoding",
            Name = "UTF-8 XML",
            Syntax = Reference("_encoding", RepositoryEntityKind.Encoding,
                "syntax", "_syntax", RepositoryEntityKind.Syntax, 0)
        };
        encoding.MessageSets.Add(Reference("_encoding", RepositoryEntityKind.Encoding,
            "messageSet", "_messageSet", RepositoryEntityKind.MessageSet, 0));

        ExternalSchemaDefinition external = new ExternalSchemaDefinition
        {
            Identifier = "_external",
            Kind = DataTypeKind.ExternalSchema,
            SourceTypeName = "ExternalSchema",
            Name = "SupplementaryPayload",
            NamespaceMode = "list",
            ProcessContents = "LAX",
            IsTechnical = true
        };
        external.NamespaceUris.Add("urn:example:supplementary:one");
        external.NamespaceUris.Add("urn:example:supplementary:two");
        external.MessageBuildingBlocks.Add(Reference("_external", RepositoryEntityKind.ExternalSchema,
            "messageBuildingBlock", "_supplementaryData", RepositoryEntityKind.MessageBuildingBlock, 0));

        ExternalSchemaDefinition userDefined = new ExternalSchemaDefinition
        {
            Identifier = "_userDefined",
            Kind = DataTypeKind.UserDefined,
            SourceTypeName = "UserDefined",
            NamespaceMode = "##other",
            ProcessContents = "STRICT"
        };

        SyntaxMessageSchemeDefinition scheme = new SyntaxMessageSchemeDefinition
        {
            Identifier = "_scheme",
            Name = "pacs.008 XML Scheme",
            MessageDefinitionTrace = Reference("_scheme", RepositoryEntityKind.SyntaxMessageScheme,
                "messageDefinitionTrace", "_pacs008", RepositoryEntityKind.MessageDefinition, 0),
            Syntax = Reference("_scheme", RepositoryEntityKind.SyntaxMessageScheme,
                "syntax", "_syntax", RepositoryEntityKind.Syntax, 0),
            Encoding = Reference("_scheme", RepositoryEntityKind.SyntaxMessageScheme,
                "encoding", "_encoding", RepositoryEntityKind.Encoding, 0)
        };
        RepositorySnapshot snapshot = new RepositorySnapshot();
        snapshot.Syntaxes.Add(syntax);
        snapshot.Encodings.Add(encoding);
        snapshot.ExternalSchemas.Add(external);
        snapshot.ExternalSchemas.Add(userDefined);
        snapshot.SyntaxMessageSchemes.Add(scheme);

        Assert.Multiple(() =>
        {
            Assert.That(syntax.PossibleEncodings.Single().TargetIdentifier, Is.EqualTo("_encoding"));
            Assert.That(syntax.GeneratedForMessageSets.Single().TargetIdentifier, Is.EqualTo("_messageSet"));
            Assert.That(encoding.Syntax.TargetIdentifier, Is.EqualTo("_syntax"));
            Assert.That(encoding.MessageSets.Single().TargetIdentifier, Is.EqualTo("_messageSet"));
            Assert.That(external.NamespaceUris,
                Is.EqualTo(new[] { "urn:example:supplementary:one", "urn:example:supplementary:two" }));
            Assert.That(external.ProcessContents, Is.EqualTo("LAX"));
            Assert.That(external.MessageBuildingBlocks.Single().TargetIdentifier, Is.EqualTo("_supplementaryData"));
            Assert.That(userDefined.Kind, Is.EqualTo(DataTypeKind.UserDefined));
            Assert.That(userDefined.NamespaceMode, Is.EqualTo("##other"));
            Assert.That(scheme.MessageDefinitionTrace.TargetIdentifier, Is.EqualTo("_pacs008"));
            Assert.That(scheme.Syntax?.TargetIdentifier, Is.EqualTo("_syntax"));
            Assert.That(scheme.Encoding?.TargetIdentifier, Is.EqualTo("_encoding"));
        });
    }

    [Test]
    public void ConvergenceRetainsIndustryMappingsAndStructuredSemanticMarkup()
    {
        ConvergenceDefinition convergence = new ConvergenceDefinition
        {
            Identifier = "_convergence",
            Name = "ISO 15022 to ISO 20022 Securities Mapping",
            Definition = "Maps earlier securities messages to ISO 20022 concepts.",
            BusinessProcessCatalogue = Reference("_convergence", RepositoryEntityKind.Convergence,
                "businessProcessCatalogue", "_catalogue", RepositoryEntityKind.BusinessProcessCatalogue, 0)
        };
        convergence.IndustryMessageSets.Add(Reference("_convergence", RepositoryEntityKind.Convergence,
            "industryMessageSet", "_iso15022Set", RepositoryEntityKind.MessageSet, 0));
        convergence.SourceItems.Add(Reference("_convergence", RepositoryEntityKind.Convergence,
            "sourceItem", "_mtField", RepositoryEntityKind.Unknown, 0));
        convergence.TargetItems.Add(Reference("_convergence", RepositoryEntityKind.Convergence,
            "targetItem", "_isoElement", RepositoryEntityKind.MessageElement, 0));
        SemanticMarkupData mapping = new SemanticMarkupData { Type = "mapping" };
        mapping.Elements.Add(new SemanticMarkupData { Name = "confidence", Value = "exact" });
        mapping.Elements.Add(new SemanticMarkupData { Name = "direction", Value = "source-to-target" });
        convergence.SemanticMarkup.Add(mapping);
        RepositorySnapshot snapshot = new RepositorySnapshot();
        snapshot.ConvergenceDefinitions.Add(convergence);

        Assert.Multiple(() =>
        {
            Assert.That(convergence.IndustryMessageSets.Single().TargetIdentifier, Is.EqualTo("_iso15022Set"));
            Assert.That(convergence.SourceItems.Single().TargetIdentifier, Is.EqualTo("_mtField"));
            Assert.That(convergence.TargetItems.Single().TargetIdentifier, Is.EqualTo("_isoElement"));
            Assert.That(convergence.SemanticMarkup.Single().Elements.Select(item => item.Value),
                Is.EqualTo(new[] { "exact", "source-to-target" }));
            Assert.That(snapshot.ConvergenceDefinitions.Single(), Is.SameAs(convergence));
        });
    }

    [Test]
    public void MutableTypeSystemDefaultsAreIndependentBetweenInstances()
    {
        RepositorySnapshot firstSnapshot = new RepositorySnapshot();
        RepositorySnapshot secondSnapshot = new RepositorySnapshot();
        DataTypeDefinition firstType = new DataTypeDefinition();
        DataTypeDefinition secondType = new DataTypeDefinition();
        DataTypeFacet firstFacet = new DataTypeFacet();
        DataTypeFacet secondFacet = new DataTypeFacet();
        CodeSetDefinition firstCodeSet = new CodeSetDefinition();
        CodeSetDefinition secondCodeSet = new CodeSetDefinition();
        SyntaxDefinition firstSyntax = new SyntaxDefinition();
        SyntaxDefinition secondSyntax = new SyntaxDefinition();
        EncodingDefinition firstEncoding = new EncodingDefinition();
        EncodingDefinition secondEncoding = new EncodingDefinition();
        ExternalSchemaDefinition firstExternal = new ExternalSchemaDefinition();
        ExternalSchemaDefinition secondExternal = new ExternalSchemaDefinition();
        SyntaxMessageSchemeDefinition firstScheme = new SyntaxMessageSchemeDefinition();
        SyntaxMessageSchemeDefinition secondScheme = new SyntaxMessageSchemeDefinition();
        ConvergenceDefinition firstConvergence = new ConvergenceDefinition();
        ConvergenceDefinition secondConvergence = new ConvergenceDefinition();

        firstSnapshot.DataTypes.Add(firstType);
        firstType.Facets.Add(firstFacet);
        firstFacet.EnumerationValues.Add("value");
        firstCodeSet.Codes.Add(new CodeValueDefinition());
        firstSyntax.PossibleEncodings.Add(new RepositoryReference());
        firstEncoding.MessageSets.Add(new RepositoryReference());
        firstExternal.NamespaceUris.Add("urn:example");
        firstScheme.UnknownContent.Add(new RepositoryUnknownData());
        firstConvergence.SourceItems.Add(new RepositoryReference());

        Assert.Multiple(() =>
        {
            Assert.That(secondSnapshot.DataTypes, Is.Empty);
            Assert.That(secondType.Facets, Is.Empty);
            Assert.That(secondFacet.EnumerationValues, Is.Empty);
            Assert.That(secondCodeSet.Codes, Is.Empty);
            Assert.That(secondSyntax.PossibleEncodings, Is.Empty);
            Assert.That(secondEncoding.MessageSets, Is.Empty);
            Assert.That(secondExternal.NamespaceUris, Is.Empty);
            Assert.That(secondScheme.UnknownContent, Is.Empty);
            Assert.That(secondConvergence.SourceItems, Is.Empty);
            Assert.That(firstType.Registration, Is.Not.SameAs(secondType.Registration));
            Assert.That(firstCodeSet.DataDictionary, Is.Not.SameAs(secondCodeSet.DataDictionary));
            Assert.That(firstEncoding.Syntax, Is.Not.SameAs(secondEncoding.Syntax));
            Assert.That(firstConvergence.SemanticMarkup, Is.Not.SameAs(secondConvergence.SemanticMarkup));
        });
    }

    private static RepositoryReference Reference(
        string sourceIdentifier,
        RepositoryEntityKind sourceKind,
        string propertyName,
        string targetIdentifier,
        RepositoryEntityKind targetKind,
        int position)
    {
        return new RepositoryReference
        {
            SourceIdentifier = sourceIdentifier,
            SourceKind = sourceKind,
            PropertyName = propertyName,
            TargetIdentifier = targetIdentifier,
            TargetKind = targetKind,
            Position = position,
            IsResolved = true
        };
    }
}
