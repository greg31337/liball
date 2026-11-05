using System.Xml.Schema;
using iso20022;

namespace iso20022.Tests;

public class SchemaApiDataTests
{
    [Test]
    public void SchemaArtifactCarriesExactRegistryIdentityContentAndNormalizedMetadata()
    {
        MessageIdentifier identifier = new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = "10",
            Value = "pacs.008.001.10"
        };
        SchemaArtifact schema = CreateSchema(identifier);
        schema.IncludeLocations.Add("common-types.xsd");
        schema.ImportNamespaceUris.Add("urn:iso:std:iso:20022:tech:xsd:head.001.001.04");
        schema.ImportLocations.Add("head.001.001.04.xsd");
        schema.GlobalElements.Add(new QualifiedNameData
        {
            NamespaceUri = schema.NamespaceUri,
            LocalName = "Document"
        });
        schema.GlobalTypes.Add(new QualifiedNameData
        {
            NamespaceUri = schema.NamespaceUri,
            LocalName = "FIToFICustomerCreditTransferV10"
        });
        DataTypeDefinition dataType = new DataTypeDefinition
        {
            Identifier = "Max35Text",
            Name = "Max35Text",
            SchemaTypeName = "xs:string"
        };
        dataType.Facets.Add(new DataTypeFacet
        {
            Name = "maxLength",
            Value = "35",
            HasIntegerValue = true,
            IntegerValue = 35
        });
        schema.DataTypes.Add(dataType);
        schema.MessageComponents.Add(new MessageComponentDefinition
        {
            Identifier = "CreditTransferTransaction",
            Name = "CreditTransferTransaction"
        });
        schema.MessageElements.Add(new MessageElementDefinition
        {
            Identifier = "Document",
            Name = "Document",
            XmlTag = "Document"
        });
        schema.MessageDefinitions.Add(new MessageDefinitionMetadata
        {
            Identifier = "pacs.008.001.10",
            MessageIdentifier = identifier,
            NamespaceUri = schema.NamespaceUri,
            RootElement = "Document"
        });
        schema.RepositoryFallbackDefinitions.Add(new ExternalSchemaDefinition
        {
            Identifier = "repository-fallback",
            Name = "SupplementaryDataEnvelope"
        });

        Assert.Multiple(() =>
        {
            Assert.That(schema.Artifact.Coordinate.Kind, Is.EqualTo(ArtifactKind.Schema));
            Assert.That(schema.Artifact.Coordinate.Edition, Is.EqualTo("2026"));
            Assert.That(schema.SourceName, Is.EqualTo("pacs.008.001.10.xsd"));
            Assert.That(schema.BaseUri, Is.EqualTo("file:///schemas/pacs.008.001.10.xsd"));
            Assert.That(schema.NamespaceUri,
                Is.EqualTo("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10"));
            Assert.That(schema.MessageIdentifier, Is.SameAs(identifier));
            Assert.That(schema.Version, Is.EqualTo("10"));
            Assert.That(schema.Profile, Is.EqualTo("cbpr-plus"));
            Assert.That(schema.RootElement, Is.EqualTo("Document"));
            Assert.That(schema.Content, Is.EqualTo("<xs:schema/>"u8.ToArray()));
            Assert.That(schema.Precedence, Is.EqualTo(20));
            Assert.That(schema.IsEntryPoint, Is.True);
            Assert.That(schema.IncludeLocations, Is.EqualTo(new[] { "common-types.xsd" }));
            Assert.That(schema.ImportNamespaceUris.Single(), Does.EndWith("head.001.001.04"));
            Assert.That(schema.ImportLocations, Is.EqualTo(new[] { "head.001.001.04.xsd" }));
            Assert.That(schema.GlobalElements.Single().LocalName, Is.EqualTo("Document"));
            Assert.That(schema.GlobalTypes.Single().LocalName,
                Is.EqualTo("FIToFICustomerCreditTransferV10"));
            Assert.That(schema.DataTypes.Single().Facets.Single().IntegerValue, Is.EqualTo(35));
            Assert.That(schema.MessageComponents.Single().Name,
                Is.EqualTo("CreditTransferTransaction"));
            Assert.That(schema.MessageElements.Single().XmlTag, Is.EqualTo("Document"));
            Assert.That(schema.MessageDefinitions.Single().MessageIdentifier, Is.SameAs(identifier));
            Assert.That(schema.RepositoryFallbackDefinitions.Single().Identifier,
                Is.EqualTo("repository-fallback"));
        });
    }

    [Test]
    public void RegistryOptionsAndResultCollectionsHaveIndependentBoundedDefaults()
    {
        SchemaRegistryOptions firstOptions = new SchemaRegistryOptions();
        SchemaRegistryOptions secondOptions = new SchemaRegistryOptions();
        SchemaArtifact firstSchema = new SchemaArtifact();
        SchemaArtifact secondSchema = new SchemaArtifact();
        SchemaCompilationResult firstResult = new SchemaCompilationResult();
        SchemaCompilationResult secondResult = new SchemaCompilationResult();

        firstOptions.Limits.MaxSchemaCount = 3;
        firstOptions.LocalSchemas.Add(firstSchema);
        firstSchema.IncludeLocations.Add("first.xsd");
        firstSchema.Content = new byte[] { 1 };
        firstResult.DiagnosticCodes.Add("schema.test");
        firstResult.Schemas.Add(firstSchema);

        Assert.Multiple(() =>
        {
            Assert.That(firstOptions.ResolveIncludes, Is.True);
            Assert.That(firstOptions.ResolveImports, Is.True);
            Assert.That(firstOptions.AllowRepositoryFallback, Is.True);
            Assert.That(firstOptions.CacheCompiledSchemas, Is.True);
            Assert.That(firstOptions.AllowReplacement, Is.False);
            Assert.That(firstOptions.RequireApprovedArtifacts, Is.False);
            Assert.That(firstOptions.CompileOnRegistration, Is.False);
            Assert.That(secondOptions.Limits.MaxSchemaCount,
                Is.EqualTo(Iso20022Limits.MaxSchemaCount));
            Assert.That(secondOptions.Limits, Is.Not.SameAs(firstOptions.Limits));
            Assert.That(secondOptions.LocalSchemas, Is.Empty);
            Assert.That(secondOptions.LocalSchemas, Is.Not.SameAs(firstOptions.LocalSchemas));
            Assert.That(secondSchema.IncludeLocations, Is.Empty);
            Assert.That(secondSchema.Content, Is.Empty);
            Assert.That(secondResult.DiagnosticCodes, Is.Empty);
            Assert.That(secondResult.DiagnosticPaths, Is.Empty);
            Assert.That(secondResult.DiagnosticSchemaUris, Is.Empty);
            Assert.That(secondResult.Schemas, Is.Empty);
            Assert.That(firstResult.DiagnosticMessages, Is.Not.SameAs(secondResult.DiagnosticMessages));
            Assert.That(firstResult.DiagnosticLocations, Is.Not.SameAs(secondResult.DiagnosticLocations));
            Assert.That(firstResult.DiagnosticPaths, Is.Not.SameAs(secondResult.DiagnosticPaths));
            Assert.That(firstResult.DiagnosticSchemaUris,
                Is.Not.SameAs(secondResult.DiagnosticSchemaUris));
        });
    }

    [Test]
    public void ImporterAndRegistryContractsRetainSelectionCompilationAndProvenance()
    {
        MessageIdentifier identifier = new MessageIdentifier { Value = "pacs.008.001.10" };
        SchemaArtifact schema = CreateSchema(identifier);
        SchemaRegistryOptions options = new SchemaRegistryOptions
        {
            LocalRootPath = "/schemas",
            AllowReplacement = true,
            RequireApprovedArtifacts = true,
            CompileOnRegistration = true,
            CacheCompiledSchemas = true
        };
        CapturingSchemaImporter importerImplementation = new CapturingSchemaImporter();
        ISchemaImporter importer = importerImplementation;
        SchemaCompilationResult imported = importer.ImportSchemaStream(
            new MemoryStream(schema.Content),
            schema,
            options);
        SchemaCompilationResult importedFile = importer.ImportSchemaFile(
            "/schemas/pacs.008.001.10.xsd",
            schema,
            options);

        CapturingSchemaRegistry registryImplementation = new CapturingSchemaRegistry();
        ISchemaRegistry registry = registryImplementation;
        SchemaCompilationResult registered = registry.RegisterSchema(schema, options);
        SchemaCompilationResult selected = registry.LookupSchema(schema, options);
        SchemaCompilationResult compiled = registry.CompileSchema(schema, options);

        Assert.Multiple(() =>
        {
            Assert.That(imported.Success, Is.True, imported.ErrorMessage);
            Assert.That(imported.Found, Is.True);
            Assert.That(imported.RootSchema, Is.SameAs(schema));
            Assert.That(imported.Schemas.Single(), Is.SameAs(schema));
            Assert.That(importerImplementation.LastSchema, Is.SameAs(schema));
            Assert.That(importerImplementation.LastOptions, Is.SameAs(options));
            Assert.That(importedFile.Success, Is.True);
            Assert.That(importerImplementation.LastPath,
                Is.EqualTo("/schemas/pacs.008.001.10.xsd"));
            Assert.That(registered.Registered, Is.True);
            Assert.That(registered.ReplacedExistingSchema, Is.True);
            Assert.That(registered.InvalidatedCachedCompilation, Is.True);
            Assert.That(selected.Success, Is.True);
            Assert.That(selected.Found, Is.True);
            Assert.That(selected.RootSchema?.Artifact, Is.SameAs(schema.Artifact));
            Assert.That(compiled.Success, Is.True);
            Assert.That(compiled.CompiledSchemas, Is.Not.Null);
            Assert.That(compiled.ReusedCachedCompilation, Is.True);
            Assert.That(registryImplementation.LastSchema, Is.SameAs(schema));
            Assert.That(registryImplementation.LastOptions, Is.SameAs(options));
        });
    }

    private static SchemaArtifact CreateSchema(MessageIdentifier identifier)
    {
        return new SchemaArtifact
        {
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Schema,
                    Edition = "2026",
                    Identifier = "pacs.008.001.10",
                    Version = "10"
                },
                OriginUri = "https://example.test/pacs.008.001.10.xsd",
                MediaType = Iso20022MediaTypes.Xml,
                IsApproved = true
            },
            SourceName = "pacs.008.001.10.xsd",
            BaseUri = "file:///schemas/pacs.008.001.10.xsd",
            NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10",
            MessageIdentifier = identifier,
            Version = "10",
            Profile = "cbpr-plus",
            RootElement = "Document",
            Content = "<xs:schema/>"u8.ToArray(),
            Precedence = 20,
            IsEntryPoint = true
        };
    }

    private sealed class CapturingSchemaImporter : ISchemaImporter
    {
        internal SchemaArtifact? LastSchema;
        internal SchemaRegistryOptions? LastOptions;
        internal string LastPath = string.Empty;

        public SchemaCompilationResult ImportSchemaStream(
            Stream? input,
            SchemaArtifact? schema,
            SchemaRegistryOptions? options)
        {
            LastSchema = schema;
            LastOptions = options;
            return CreateSuccess(schema);
        }

        public SchemaCompilationResult ImportSchemaFile(
            string? path,
            SchemaArtifact? schema,
            SchemaRegistryOptions? options)
        {
            LastPath = path ?? string.Empty;
            LastSchema = schema;
            LastOptions = options;
            return CreateSuccess(schema);
        }
    }

    private sealed class CapturingSchemaRegistry : ISchemaRegistry
    {
        internal SchemaArtifact? LastSchema;
        internal SchemaRegistryOptions? LastOptions;

        public SchemaCompilationResult ImportRegisterSchemaStream(
            Stream? input,
            SchemaArtifact? schema,
            SchemaRegistryOptions? options)
        {
            Capture(schema, options);
            SchemaCompilationResult result = CreateSuccess(schema);
            result.Registered = input != null && schema != null;
            return result;
        }

        public SchemaCompilationResult ImportRegisterSchemaFile(
            string? path,
            SchemaArtifact? schema,
            SchemaRegistryOptions? options)
        {
            Capture(schema, options);
            SchemaCompilationResult result = CreateSuccess(schema);
            result.Registered = path != null && schema != null;
            return result;
        }

        public SchemaCompilationResult ImportRegisterSchemaArchive(
            Stream? input,
            SchemaArtifact? schema,
            SchemaRegistryOptions? options)
        {
            Capture(schema, options);
            SchemaCompilationResult result = CreateSuccess(schema);
            result.Registered = input != null && schema != null;
            return result;
        }

        public SchemaCompilationResult RegisterSchema(
            SchemaArtifact? schema,
            SchemaRegistryOptions? options)
        {
            Capture(schema, options);
            SchemaCompilationResult result = CreateSuccess(schema);
            result.Registered = true;
            result.ReplacedExistingSchema = options?.AllowReplacement == true;
            result.InvalidatedCachedCompilation = true;
            return result;
        }

        public SchemaCompilationResult LookupSchema(
            SchemaArtifact? selector,
            SchemaRegistryOptions? options)
        {
            Capture(selector, options);
            return CreateSuccess(selector);
        }

        public SchemaCompilationResult CompileSchema(
            SchemaArtifact? selector,
            SchemaRegistryOptions? options)
        {
            Capture(selector, options);
            SchemaCompilationResult result = CreateSuccess(selector);
            result.CompiledSchemas = new XmlSchemaSet();
            result.ReusedCachedCompilation = options?.CacheCompiledSchemas == true;
            return result;
        }

        private void Capture(SchemaArtifact? schema, SchemaRegistryOptions? options)
        {
            LastSchema = schema;
            LastOptions = options;
        }
    }

    private static SchemaCompilationResult CreateSuccess(SchemaArtifact? schema)
    {
        SchemaCompilationResult result = new SchemaCompilationResult
        {
            Success = schema != null,
            Found = schema != null,
            RootSchema = schema
        };
        if (schema != null)
        {
            result.Schemas.Add(schema);
        }

        return result;
    }
}
