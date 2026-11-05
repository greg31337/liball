using iso20022;

namespace iso20022.Tests;

public class GenerationModelNormalizerTests
{
    [Test]
    public void RepositoryNormalizationPreservesEveryGenerationConceptInSourceOrder()
    {
        RepositorySnapshot snapshot = CreateRepositorySnapshot();
        GenerationModelNormalizer normalizer = new GenerationModelNormalizer();

        GenerationModel model = normalizer.NormalizeRepositorySnapshot(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(model.Sources, Has.Count.EqualTo(1));
            Assert.That(model.Sources[0].Kind, Is.EqualTo(GenerationSourceKind.Repository));
            Assert.That(model.Sources[0].Ordinal, Is.EqualTo(0));
            Assert.That(model.Sources[0].Identifier, Is.EqualTo("repository-1"));
            Assert.That(model.Sources[0].Edition, Is.EqualTo("2026"));
            Assert.That(model.Sources[0].Artifact, Is.SameAs(snapshot.Provenance.Artifact));
            Assert.That(model.Sources[0].Repository, Is.SameAs(snapshot));
            Assert.That(model.Sources[0].Schema, Is.Null);
            Assert.That(model.Concepts.Select(value => value.Kind), Is.EqualTo(new[]
            {
                GenerationConceptKind.MessageDefinition,
                GenerationConceptKind.MessageBuildingBlock,
                GenerationConceptKind.MessageComponent,
                GenerationConceptKind.MessageElement,
                GenerationConceptKind.Choice,
                GenerationConceptKind.DataType,
                GenerationConceptKind.CodeSet,
                GenerationConceptKind.CodeValue,
                GenerationConceptKind.CodeValue,
                GenerationConceptKind.ExternalSchema
            }));
            Assert.That(model.Concepts.Select(value => value.ConceptOrdinal),
                Is.EqualTo(Enumerable.Range(0, 10)));
            Assert.That(model.Concepts.All(value =>
                value.SourceKind == GenerationSourceKind.Repository), Is.True);
            Assert.That(model.Concepts[0].MessageDefinition,
                Is.SameAs(snapshot.MessageDefinitions[0]));
            Assert.That(model.Concepts[1].MessageBuildingBlock,
                Is.SameAs(snapshot.MessageBuildingBlocks[0]));
            Assert.That(model.Concepts[2].MessageComponent,
                Is.SameAs(snapshot.MessageComponents[0]));
            Assert.That(model.Concepts[3].MessageElement,
                Is.SameAs(snapshot.MessageElements[0]));
            Assert.That(model.Concepts[4].Choice, Is.SameAs(snapshot.Choices[0]));
            Assert.That(model.Concepts[5].DataType, Is.SameAs(snapshot.DataTypes[0]));
            Assert.That(model.Concepts[6].CodeSet, Is.SameAs(snapshot.CodeSets[0]));
            Assert.That(model.Concepts[7].CodeValue, Is.SameAs(snapshot.CodeSets[0].Codes[0]));
            Assert.That(model.Concepts[8].CodeValue, Is.SameAs(snapshot.CodeSets[0].Codes[1]));
            Assert.That(model.Concepts[7].OwnerIdentifier, Is.EqualTo("code-set-1"));
            Assert.That(model.Concepts[7].DeclarationOrdinal, Is.EqualTo(0));
            Assert.That(model.Concepts[8].DeclarationOrdinal, Is.EqualTo(1));
            Assert.That(model.Concepts[9].ExternalSchema,
                Is.SameAs(snapshot.ExternalSchemas[0]));
            Assert.That(model.Concepts[9].IsRepositoryFallback, Is.False);
        });
    }

    [Test]
    public void RepositoryNormalizationRetainsFacetOwnersAndExactFacetObjects()
    {
        RepositorySnapshot snapshot = CreateRepositorySnapshot();
        GenerationModelNormalizer normalizer = new GenerationModelNormalizer();

        GenerationModel model = normalizer.NormalizeRepositorySnapshot(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(model.Facets, Has.Count.EqualTo(3));
            Assert.That(model.Facets[0].OwnerKind, Is.EqualTo(GenerationConceptKind.DataType));
            Assert.That(model.Facets[0].OwnerIdentifier, Is.EqualTo("data-type-1"));
            Assert.That(model.Facets[0].DeclarationOrdinal, Is.EqualTo(0));
            Assert.That(model.Facets[0].FacetOrdinal, Is.EqualTo(0));
            Assert.That(model.Facets[0].Facet, Is.SameAs(snapshot.DataTypes[0].Facets[0]));
            Assert.That(model.Facets[1].Facet, Is.SameAs(snapshot.DataTypes[0].Facets[1]));
            Assert.That(model.Facets[1].Facet.EnumerationValues,
                Is.EqualTo(new[] { "A", "B" }));
            Assert.That(model.Facets[2].OwnerKind, Is.EqualTo(GenerationConceptKind.CodeSet));
            Assert.That(model.Facets[2].OwnerIdentifier, Is.EqualTo("code-set-1"));
            Assert.That(model.Facets[2].Facet, Is.SameAs(snapshot.CodeSets[0].Facets[0]));
        });
    }

    [Test]
    public void SchemaNormalizationPreservesSchemaDeclarationsAndFallbackMetadata()
    {
        SchemaArtifact schema = CreateSchemaArtifact();
        GenerationModelNormalizer normalizer = new GenerationModelNormalizer();

        GenerationModel model = normalizer.NormalizeSchemaArtifact(schema);

        Assert.Multiple(() =>
        {
            Assert.That(model.Sources, Has.Count.EqualTo(1));
            Assert.That(model.Sources[0].Kind, Is.EqualTo(GenerationSourceKind.Xsd));
            Assert.That(model.Sources[0].Identifier, Is.EqualTo("payments.xsd"));
            Assert.That(model.Sources[0].NamespaceUri, Is.EqualTo("urn:example:payments"));
            Assert.That(model.Sources[0].Version, Is.EqualTo("13"));
            Assert.That(model.Sources[0].Profile, Is.EqualTo("market"));
            Assert.That(model.Sources[0].Artifact, Is.SameAs(schema.Artifact));
            Assert.That(model.Sources[0].Schema, Is.SameAs(schema));
            Assert.That(model.Sources[0].Repository, Is.Null);
            Assert.That(model.Sources[0].Schema?.IncludeLocations,
                Is.EqualTo(new[] { "common.xsd" }));
            Assert.That(model.Sources[0].Schema?.GlobalElements[0],
                Is.SameAs(schema.GlobalElements[0]));
            Assert.That(model.Concepts.Select(value => value.Kind), Is.EqualTo(new[]
            {
                GenerationConceptKind.MessageDefinition,
                GenerationConceptKind.MessageComponent,
                GenerationConceptKind.MessageElement,
                GenerationConceptKind.DataType,
                GenerationConceptKind.ExternalSchema
            }));
            Assert.That(model.Concepts[0].MessageDefinition,
                Is.SameAs(schema.MessageDefinitions[0]));
            Assert.That(model.Concepts[1].MessageComponent,
                Is.SameAs(schema.MessageComponents[0]));
            Assert.That(model.Concepts[2].MessageElement,
                Is.SameAs(schema.MessageElements[0]));
            Assert.That(model.Concepts[3].DataType, Is.SameAs(schema.DataTypes[0]));
            Assert.That(model.Concepts[4].ExternalSchema,
                Is.SameAs(schema.RepositoryFallbackDefinitions[0]));
            Assert.That(model.Concepts[4].IsRepositoryFallback, Is.True);
            Assert.That(model.Facets[0].SourceKind, Is.EqualTo(GenerationSourceKind.Xsd));
            Assert.That(model.Facets[0].Facet, Is.SameAs(schema.DataTypes[0].Facets[0]));
        });
    }

    [Test]
    public void AppendingSourcesKeepsDuplicateIdentifiersAndDistinctOrigins()
    {
        RepositorySnapshot snapshot = CreateRepositorySnapshot();
        SchemaArtifact schema = CreateSchemaArtifact();
        schema.MessageDefinitions[0].Identifier = snapshot.MessageDefinitions[0].Identifier;
        GenerationModelNormalizer normalizer = new GenerationModelNormalizer();
        GenerationModel model = normalizer.NormalizeRepositorySnapshot(snapshot);

        normalizer.AppendSchemaArtifact(model, schema);

        GenerationConcept[] duplicates = model.Concepts.Where(value =>
            value.Kind == GenerationConceptKind.MessageDefinition &&
            value.Identifier == "message-1").ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(model.Sources, Has.Count.EqualTo(2));
            Assert.That(model.Sources.Select(value => value.Ordinal), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(duplicates, Has.Length.EqualTo(2));
            Assert.That(duplicates[0].SourceKind, Is.EqualTo(GenerationSourceKind.Repository));
            Assert.That(duplicates[0].SourceOrdinal, Is.EqualTo(0));
            Assert.That(duplicates[1].SourceKind, Is.EqualTo(GenerationSourceKind.Xsd));
            Assert.That(duplicates[1].SourceOrdinal, Is.EqualTo(1));
            Assert.That(model.Concepts.Select(value => value.ConceptOrdinal),
                Is.EqualTo(Enumerable.Range(0, model.Concepts.Count)));
        });
    }

    [Test]
    public void NullInputsAreRejectedBeforeTheModelIsChanged()
    {
        GenerationModelNormalizer normalizer = new GenerationModelNormalizer();
        GenerationModel model = new GenerationModel();

        Assert.Multiple(() =>
        {
            Assert.That(() => normalizer.AppendRepositorySnapshot(model, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("snapshot"));
            Assert.That(() => normalizer.AppendSchemaArtifact(model, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("schema"));
            Assert.That(() => normalizer.AppendRepositorySnapshot(null!, new RepositorySnapshot()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("model"));
            Assert.That(model.Sources, Is.Empty);
            Assert.That(model.Concepts, Is.Empty);
            Assert.That(model.Facets, Is.Empty);
        });
    }

    private static RepositorySnapshot CreateRepositorySnapshot()
    {
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = "repository-1",
            Edition = "2026"
        };
        snapshot.Provenance.Artifact.OriginUri = "https://example.test/repository.ecore";
        snapshot.MessageDefinitions.Add(new MessageDefinitionMetadata
        {
            Identifier = "message-1",
            Name = "PaymentMessage",
            Definition = "A payment message."
        });
        snapshot.MessageBuildingBlocks.Add(new MessageBuildingBlockDefinition
        {
            Identifier = "block-1",
            Name = "PaymentInformation",
            Definition = "The message body."
        });
        snapshot.MessageComponents.Add(new MessageComponentDefinition
        {
            Identifier = "component-1",
            Name = "PaymentComponent",
            Definition = "A reusable component."
        });
        snapshot.MessageComponents[0].UnknownContent.Add(new RepositoryUnknownData
        {
            TypeName = "future:ComponentMetadata",
            Value = "preserved"
        });
        snapshot.MessageElements.Add(new MessageElementDefinition
        {
            Identifier = "element-1",
            Name = "Amount",
            Definition = "The instructed amount.",
            XmlTag = "Amt"
        });
        snapshot.Choices.Add(new ChoiceDefinition
        {
            Identifier = "choice-1",
            Name = "AccountChoice",
            Definition = "One account identifier."
        });
        DataTypeDefinition dataType = new DataTypeDefinition
        {
            Identifier = "data-type-1",
            Name = "RestrictedText",
            Definition = "A restricted string.",
            Kind = DataTypeKind.String
        };
        dataType.Facets.Add(new DataTypeFacet
        {
            Name = "minLength",
            Value = "1",
            HasIntegerValue = true,
            IntegerValue = 1
        });
        DataTypeFacet enumeration = new DataTypeFacet
        {
            Name = "enumeration"
        };
        enumeration.EnumerationValues.Add("A");
        enumeration.EnumerationValues.Add("B");
        dataType.Facets.Add(enumeration);
        snapshot.DataTypes.Add(dataType);
        CodeSetDefinition codeSet = new CodeSetDefinition
        {
            Identifier = "code-set-1",
            Name = "PaymentStatusCode",
            Definition = "Payment statuses.",
            Kind = CodeSetKind.Internal
        };
        codeSet.Facets.Add(new DataTypeFacet
        {
            Name = "length",
            Value = "4",
            HasIntegerValue = true,
            IntegerValue = 4
        });
        codeSet.Codes.Add(new CodeValueDefinition
        {
            Identifier = "code-1",
            Value = "ACCP",
            Name = "Accepted",
            Definition = "Accepted for processing."
        });
        codeSet.Codes.Add(new CodeValueDefinition
        {
            Identifier = "code-2",
            Value = "RJCT",
            Name = "Rejected",
            Definition = "Rejected for processing."
        });
        snapshot.CodeSets.Add(codeSet);
        snapshot.ExternalSchemas.Add(new ExternalSchemaDefinition
        {
            Identifier = "external-1",
            Name = "SupplementaryDataEnvelope",
            Definition = "An extension container.",
            NamespaceMode = "##any"
        });
        return snapshot;
    }

    private static SchemaArtifact CreateSchemaArtifact()
    {
        SchemaArtifact schema = new SchemaArtifact
        {
            SourceName = "payments.xsd",
            NamespaceUri = "urn:example:payments",
            Version = "13",
            Profile = "market"
        };
        schema.Artifact.OriginUri = "file:///schemas/payments.xsd";
        schema.IncludeLocations.Add("common.xsd");
        schema.GlobalElements.Add(new QualifiedNameData
        {
            NamespaceUri = "urn:example:payments",
            LocalName = "Document"
        });
        schema.MessageDefinitions.Add(new MessageDefinitionMetadata
        {
            Identifier = "xsd-message-1",
            Name = "SchemaPaymentMessage",
            NamespaceUri = "urn:example:payments"
        });
        schema.MessageComponents.Add(new MessageComponentDefinition
        {
            Identifier = "xsd-component-1",
            Name = "DocumentType"
        });
        schema.MessageElements.Add(new MessageElementDefinition
        {
            Identifier = "xsd-element-1",
            Name = "Document",
            XmlTag = "Document"
        });
        DataTypeDefinition dataType = new DataTypeDefinition
        {
            Identifier = "xsd-data-type-1",
            Name = "Max35Text",
            Kind = DataTypeKind.String
        };
        dataType.Facets.Add(new DataTypeFacet
        {
            Name = "maxLength",
            Value = "35",
            HasIntegerValue = true,
            IntegerValue = 35
        });
        schema.DataTypes.Add(dataType);
        schema.RepositoryFallbackDefinitions.Add(new ExternalSchemaDefinition
        {
            Identifier = "fallback-1",
            Name = "RepositoryExtension",
            Definition = "Fallback semantic metadata."
        });
        return schema;
    }
}
