using iso20022;

namespace iso20022.Tests;

public class GenerationSymbolTableTests
{
    [Test]
    public void IdentifierNamingHandlesKeywordsUnicodeInvalidCharactersAndLeadingDigits()
    {
        GenerationIdentifierNamer namer = new GenerationIdentifierNamer();

        Assert.Multiple(() =>
        {
            Assert.That(namer.EscapeIdentifier("class"), Is.EqualTo("@class"));
            Assert.That(namer.EscapeIdentifier("record"), Is.EqualTo("@record"));
            Assert.That(namer.EscapeIdentifier("payment"), Is.EqualTo("payment"));
            Assert.That(namer.CreateTypeName("crédit-transfer"),
                Is.EqualTo("CréditTransfer"));
            Assert.That(namer.CreateTypeName("cre\u0301dit transfer"),
                Is.EqualTo("CréditTransfer"));
            Assert.That(namer.CreateTypeName("支付 instruction"),
                Is.EqualTo("支付Instruction"));
            Assert.That(namer.CreateMemberName("123 amount"), Is.EqualTo("_123Amount"));
            Assert.That(namer.CreateTypeName("💶"), Is.EqualTo("_"));
            Assert.That(namer.CreateNamespaceRoot("contoso.generated-models"),
                Is.EqualTo("Contoso.GeneratedModels"));
            Assert.That(namer.CreateNamespaceRoot("  "),
                Is.EqualTo("Iso20022.Generated"));
        });
    }

    [Test]
    public void LongIdentifiersAreBoundedAndUseStableContentHashes()
    {
        GenerationIdentifierNamer namer = new GenerationIdentifierNamer();
        string common = new string('A', 180);

        string first = namer.CreateTypeName(common + "first");
        string repeated = namer.CreateTypeName(common + "first");
        string second = namer.CreateTypeName(common + "second");
        string disambiguated = namer.AppendDisambiguator(common, new string('b', 64));

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(GenerationIdentifierNamer.MaximumIdentifierLength));
            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(disambiguated,
                Has.Length.EqualTo(GenerationIdentifierNamer.MaximumIdentifierLength));
            Assert.That(disambiguated, Does.EndWith("_" + new string('b', 64)));
        });
    }

    [Test]
    public void ShapeIdentifiersIgnoreDisplayMetadataButTrackWireStructureAndOrder()
    {
        GenerationShapeIdentifierBuilder builder = new GenerationShapeIdentifierBuilder();
        GenerationConcept first = CreateComponentConcept(
            "component-a", "Payment", "First definition", "member-a", "member-b");
        GenerationConcept renamed = CreateComponentConcept(
            "component-b", "Transfer", "Other definition", "member-a", "member-b");
        GenerationConcept changed = CreateComponentConcept(
            "component-c", "Payment", "First definition", "member-a", "member-c");
        GenerationConcept reordered = CreateComponentConcept(
            "component-d", "Payment", "First definition", "member-b", "member-a");

        string firstShape = builder.CreateShapeIdentifier(first);
        string renamedShape = builder.CreateShapeIdentifier(renamed);
        string changedShape = builder.CreateShapeIdentifier(changed);
        string reorderedShape = builder.CreateShapeIdentifier(reordered);

        Assert.Multiple(() =>
        {
            Assert.That(firstShape, Does.StartWith("shape_"));
            Assert.That(firstShape, Has.Length.EqualTo(70));
            Assert.That(renamedShape, Is.EqualTo(firstShape));
            Assert.That(changedShape, Is.Not.EqualTo(firstShape));
            Assert.That(reorderedShape, Is.Not.EqualTo(firstShape));
            Assert.That(() => builder.CreateShapeIdentifier(null!), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void SymbolTablePartitionsNamespacesAndResolvesSemanticNameCollisions()
    {
        RepositorySnapshot snapshot = CreatePartitionedSnapshot(false);
        GenerationModel model = new GenerationModelNormalizer().NormalizeRepositorySnapshot(snapshot);

        GenerationSymbolTable table = new GenerationSymbolTableBuilder().BuildSymbolTable(
            model,
            "contoso.generated-models");

        GenerationSymbolEntry message = FindSymbol(table, "message-1");
        GenerationSymbolEntry block = FindSymbol(table, "block-1");
        GenerationSymbolEntry componentA = FindSymbol(table, "component-a");
        GenerationSymbolEntry componentB = FindSymbol(table, "component-b");
        GenerationSymbolEntry componentDuplicate = FindSymbol(table, "component-a-copy");
        GenerationSymbolEntry sanitizedA = FindSymbol(table, "component-c");
        GenerationSymbolEntry sanitizedB = FindSymbol(table, "component-d");
        GenerationSymbolEntry dataType = FindSymbol(table, "type-1");
        GenerationSymbolEntry codeSet = FindSymbol(table, "codes-1");
        GenerationSymbolEntry codeValue = FindSymbol(table, "code-1");
        GenerationSymbolEntry external = FindSymbol(table, "external-1");

        Assert.Multiple(() =>
        {
            Assert.That(table.RootNamespace, Is.EqualTo("Contoso.GeneratedModels"));
            Assert.That(message.NamespaceName, Is.EqualTo("Contoso.GeneratedModels.Messages.Pacs"));
            Assert.That(block.NamespaceName, Is.EqualTo(message.NamespaceName));
            Assert.That(componentA.NamespaceName,
                Is.EqualTo("Contoso.GeneratedModels.Components"));
            Assert.That(dataType.NamespaceName, Is.EqualTo("Contoso.GeneratedModels.Types"));
            Assert.That(codeSet.NamespaceName, Is.EqualTo("Contoso.GeneratedModels.Codes"));
            Assert.That(codeValue.NamespaceName, Is.EqualTo(codeSet.NamespaceName));
            Assert.That(external.NamespaceName, Is.EqualTo("Contoso.GeneratedModels.External"));
            Assert.That(componentA.BaseName, Is.EqualTo("PaymentDetails"));
            Assert.That(componentA.HasCollision, Is.True);
            Assert.That(componentB.HasCollision, Is.True);
            Assert.That(componentA.SymbolName, Is.Not.EqualTo(componentB.SymbolName));
            Assert.That(componentDuplicate.SymbolName, Is.EqualTo(componentA.SymbolName));
            Assert.That(componentDuplicate.ShapeIdentifier,
                Is.EqualTo(componentA.ShapeIdentifier));
            Assert.That(sanitizedA.BaseName, Is.EqualTo("PaymentDetail"));
            Assert.That(sanitizedB.BaseName, Is.EqualTo(sanitizedA.BaseName));
            Assert.That(sanitizedA.ShapeIdentifier, Is.EqualTo(sanitizedB.ShapeIdentifier));
            Assert.That(sanitizedA.SymbolName, Is.Not.EqualTo(sanitizedB.SymbolName));
            Assert.That(table.Symbols.Select(value => value.StableOrdinal),
                Is.EqualTo(Enumerable.Range(0, table.Symbols.Count)));
        });
    }

    [Test]
    public void SchemaPartitionsUseExactMessageIdentityOrNamespaceHashFallback()
    {
        GenerationModelNormalizer normalizer = new GenerationModelNormalizer();
        GenerationNamespacePartitioner partitioner = new GenerationNamespacePartitioner();
        SchemaArtifact messageSchema = new SchemaArtifact
        {
            SourceName = "pacs.008.001.10.xsd",
            NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10",
            MessageIdentifier = new MessageIdentifier
            {
                BusinessArea = "pacs",
                MessageFunctionality = "008",
                Variant = "001",
                Version = "10"
            }
        };
        messageSchema.DataTypes.Add(new DataTypeDefinition
        {
            Identifier = "schema-type",
            Name = "Max35Text"
        });
        SchemaArtifact extensionSchema = new SchemaArtifact
        {
            SourceName = "extension.xsd",
            NamespaceUri = "urn:example:extension"
        };
        extensionSchema.DataTypes.Add(new DataTypeDefinition
        {
            Identifier = "extension-type",
            Name = "ExtensionText"
        });
        GenerationModel messageModel = normalizer.NormalizeSchemaArtifact(messageSchema);
        GenerationModel extensionModel = normalizer.NormalizeSchemaArtifact(extensionSchema);

        string messageNamespace = partitioner.CreateNamespace(
            messageModel,
            messageModel.Concepts[0],
            "iso.models");
        string extensionNamespace = partitioner.CreateNamespace(
            extensionModel,
            extensionModel.Concepts[0],
            "iso.models");
        string repeatedNamespace = partitioner.CreateNamespace(
            extensionModel,
            extensionModel.Concepts[0],
            "iso.models");

        Assert.Multiple(() =>
        {
            Assert.That(messageNamespace,
                Is.EqualTo("Iso.Models.Messages.Pacs.Pacs00800110"));
            Assert.That(extensionNamespace, Does.StartWith("Iso.Models.Schemas.N"));
            Assert.That(extensionNamespace, Has.Length.EqualTo("Iso.Models.Schemas.N".Length + 16));
            Assert.That(repeatedNamespace, Is.EqualTo(extensionNamespace));
        });
    }

    [Test]
    public void StableOrderingDoesNotDependOnRepositoryDeclarationOrder()
    {
        RepositorySnapshot firstSnapshot = CreatePartitionedSnapshot(false);
        RepositorySnapshot shuffledSnapshot = CreatePartitionedSnapshot(true);
        GenerationModelNormalizer normalizer = new GenerationModelNormalizer();
        GenerationSymbolTableBuilder builder = new GenerationSymbolTableBuilder();

        GenerationSymbolTable first = builder.BuildSymbolTable(
            normalizer.NormalizeRepositorySnapshot(firstSnapshot),
            "iso20022.generated");
        GenerationSymbolTable shuffled = builder.BuildSymbolTable(
            normalizer.NormalizeRepositorySnapshot(shuffledSnapshot),
            "iso20022.generated");

        string[] firstSymbols = first.Symbols.Select(CreateSymbolSnapshot).ToArray();
        string[] shuffledSymbols = shuffled.Symbols.Select(CreateSymbolSnapshot).ToArray();
        Assert.That(shuffledSymbols, Is.EqualTo(firstSymbols));
    }

    private static GenerationConcept CreateComponentConcept(
        string identifier,
        string name,
        string definition,
        string firstMember,
        string secondMember)
    {
        MessageComponentDefinition component = new MessageComponentDefinition
        {
            Identifier = identifier,
            Name = name,
            Definition = definition
        };
        component.MessageElements.Add(CreateReference("messageElements", 0, firstMember));
        component.MessageElements.Add(CreateReference("messageElements", 1, secondMember));
        return new GenerationConcept
        {
            SourceKind = GenerationSourceKind.Repository,
            Kind = GenerationConceptKind.MessageComponent,
            Identifier = identifier,
            Name = name,
            Definition = definition,
            MessageComponent = component
        };
    }

    private static RepositorySnapshot CreatePartitionedSnapshot(bool shuffled)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = "repository-2026",
            Edition = "2026"
        };
        MessageDefinitionMetadata message = new MessageDefinitionMetadata
        {
            Identifier = "message-1",
            Name = "CustomerCreditTransfer",
            MessageIdentifier = new MessageIdentifier
            {
                BusinessArea = "pacs",
                MessageFunctionality = "008",
                Variant = "001",
                Version = "10"
            }
        };
        snapshot.MessageDefinitions.Add(message);
        snapshot.MessageBuildingBlocks.Add(new MessageBuildingBlockDefinition
        {
            Identifier = "block-1",
            Name = "CreditTransferInformation",
            MessageDefinition = CreateReference("messageDefinition", 0, "message-1")
        });
        MessageComponentDefinition componentA = CreateComponent(
            "component-a", "PaymentDetails", "member-a");
        MessageComponentDefinition componentB = CreateComponent(
            "component-b", "PaymentDetails", "member-b");
        MessageComponentDefinition componentCopy = CreateComponent(
            "component-a-copy", "PaymentDetails", "member-a");
        MessageComponentDefinition sanitizedA = CreateComponent(
            "component-c", "payment-detail", "member-c");
        MessageComponentDefinition sanitizedB = CreateComponent(
            "component-d", "payment detail", "member-c");
        if (shuffled)
        {
            snapshot.MessageComponents.Add(sanitizedB);
            snapshot.MessageComponents.Add(componentB);
            snapshot.MessageComponents.Add(componentCopy);
            snapshot.MessageComponents.Add(sanitizedA);
            snapshot.MessageComponents.Add(componentA);
        }
        else
        {
            snapshot.MessageComponents.Add(componentA);
            snapshot.MessageComponents.Add(componentB);
            snapshot.MessageComponents.Add(componentCopy);
            snapshot.MessageComponents.Add(sanitizedA);
            snapshot.MessageComponents.Add(sanitizedB);
        }
        snapshot.DataTypes.Add(new DataTypeDefinition
        {
            Identifier = "type-1",
            Name = "Max35Text",
            Kind = DataTypeKind.String
        });
        CodeSetDefinition codeSet = new CodeSetDefinition
        {
            Identifier = "codes-1",
            Name = "PaymentStatusCode",
            Kind = CodeSetKind.Internal
        };
        codeSet.Codes.Add(new CodeValueDefinition
        {
            Identifier = "code-1",
            Name = "Accepted",
            Value = "ACCP"
        });
        snapshot.CodeSets.Add(codeSet);
        snapshot.ExternalSchemas.Add(new ExternalSchemaDefinition
        {
            Identifier = "external-1",
            Name = "SupplementaryDataEnvelope"
        });
        return snapshot;
    }

    private static MessageComponentDefinition CreateComponent(
        string identifier,
        string name,
        string memberIdentifier)
    {
        MessageComponentDefinition component = new MessageComponentDefinition
        {
            Identifier = identifier,
            Name = name
        };
        component.MessageElements.Add(CreateReference("messageElements", 0, memberIdentifier));
        return component;
    }

    private static RepositoryReference CreateReference(
        string propertyName,
        int position,
        string targetIdentifier)
    {
        return new RepositoryReference
        {
            PropertyName = propertyName,
            Position = position,
            TargetIdentifier = targetIdentifier,
            TargetKind = RepositoryEntityKind.MessageElement,
            IsResolved = true
        };
    }

    private static GenerationSymbolEntry FindSymbol(
        GenerationSymbolTable table,
        string identifier)
    {
        return table.Symbols.Single(value => value.Concept.Identifier == identifier);
    }

    private static string CreateSymbolSnapshot(GenerationSymbolEntry symbol)
    {
        return symbol.NamespaceName + "|" + symbol.SymbolName + "|" +
            symbol.ShapeIdentifier + "|" + symbol.Concept.Identifier;
    }
}
