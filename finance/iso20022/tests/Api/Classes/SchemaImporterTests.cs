using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Reflection;
using iso20022;

namespace iso20022.Tests;

public class SchemaImporterTests
{
    [Test]
    public void StreamImportNormalizesNamespaceTypesElementsFacetsAndProvenance()
    {
        byte[] xsd = Encoding.UTF8.GetBytes(CreateSchemaFixture());
        SchemaArtifact request = CreateSchemaRequest(
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        ISchemaImporter importer = new SchemaImporter();

        SchemaRegistryOptions options = new SchemaRegistryOptions
        {
            ResolveIncludes = false,
            ResolveImports = false
        };
        SchemaCompilationResult result = importer.ImportSchemaStream(
            new MemoryStream(xsd),
            request,
            options);
        SchemaArtifact schema = result.RootSchema ??
            throw new AssertionException(result.ErrorMessage);
        DataTypeDefinition textType = schema.DataTypes.Single(item => item.Name == "Max35Text");
        DataTypeDefinition statusType = schema.DataTypes.Single(item => item.Name == "PaymentStatusCode");
        MessageComponentDefinition payment = schema.MessageComponents.Single(
            item => item.Name == "PaymentInstruction10");
        MessageElementDefinition amount = schema.MessageElements.Single(item => item.Name == "Amount");
        DataTypeFacet enumeration = statusType.Facets.Single(item => item.Name == "enumeration");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Found, Is.True);
            Assert.That(result.Schemas.Single(), Is.SameAs(schema));
            Assert.That(schema.NamespaceUri,
                Is.EqualTo("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10"));
            Assert.That(schema.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(schema.Version, Is.EqualTo("fixture-v1"));
            Assert.That(schema.RootElement, Is.EqualTo("Document"));
            Assert.That(schema.Content, Is.EqualTo(xsd));
            Assert.That(schema.Artifact, Is.Not.SameAs(request.Artifact));
            Assert.That(schema.Artifact.Coordinate.Identifier,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(schema.Artifact.HashAlgorithm, Is.EqualTo("SHA-256"));
            Assert.That(schema.Artifact.Hash.Value, Is.EqualTo(SHA256.HashData(xsd)));
            Assert.That(schema.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.XmlSchema));
            Assert.That(schema.IncludeLocations, Is.EqualTo(new[] { "common-types.xsd" }));
            Assert.That(schema.ImportNamespaceUris,
                Is.EqualTo(new[] { "urn:iso:std:iso:20022:tech:xsd:head.001.001.04" }));
            Assert.That(schema.ImportLocations, Is.EqualTo(new[] { "head.001.001.04.xsd" }));
            Assert.That(schema.GlobalTypes.Select(item => item.LocalName),
                Is.EqualTo(new[] { "Max35Text", "PaymentStatusCode", "PaymentInstruction10" }));
            Assert.That(schema.GlobalElements.Single().LocalName, Is.EqualTo("Document"));
            Assert.That(textType.SchemaTypeName, Is.EqualTo("xs:string"));
            Assert.That(textType.Definition, Is.EqualTo("Text limited to thirty-five characters."));
            Assert.That(textType.Facets.Single(item => item.Name == "minLength").IntegerValue,
                Is.EqualTo(1));
            Assert.That(textType.Facets.Single(item => item.Name == "maxLength").IntegerValue,
                Is.EqualTo(35));
            Assert.That(textType.Facets.Single(item => item.Name == "pattern").Value,
                Is.EqualTo("[A-Z0-9 ]+"));
            Assert.That(enumeration.EnumerationValues, Is.EqualTo(new[] { "ACTV", "INAC" }));
            Assert.That(payment.Definition, Is.EqualTo("A payment instruction."));
            Assert.That(payment.MessageElements.Single().TargetIdentifier,
                Is.EqualTo(amount.Identifier));
            Assert.That(amount.Definition, Is.EqualTo("The instructed amount."));
            Assert.That(amount.MemberType.TargetIdentifier, Is.EqualTo("xs:decimal"));
            Assert.That(amount.Multiplicity.MinimumOccurs, Is.Zero);
            Assert.That(amount.Multiplicity.HasMaximumOccurs, Is.False);
            Assert.That(schema.MessageDefinitions.Single().RootElement, Is.EqualTo("Document"));
            Assert.That(schema.MessageDefinitions.Single().XmlName,
                Is.EqualTo("PaymentInstruction10"));
            Assert.That(schema.MessageDefinitions.Single().MessageIdentifier.Value,
                Is.EqualTo("pacs.008.001.10"));
        });
    }

    [Test]
    public void RepositoryFallbackSuppliesMissingNamespaceTypesAndSemanticDefinitions()
    {
        const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:simpleType name="RepositoryText">
                <xs:restriction base="xs:string"/>
              </xs:simpleType>
            </xs:schema>
            """;
        SchemaArtifact request = CreateSchemaRequest(string.Empty);
        ExternalSchemaDefinition matching = new ExternalSchemaDefinition
        {
            Identifier = "repository-text",
            Name = "RepositoryText",
            Definition = "Definition supplied by repository metadata."
        };
        matching.NamespaceUris.Add("urn:example:fallback");
        ExternalSchemaDefinition additional = new ExternalSchemaDefinition
        {
            Identifier = "repository-envelope",
            SourceTypeName = "ExternalSchema",
            Name = "SupplementaryEnvelope",
            Definition = "Repository-only fallback type."
        };
        request.RepositoryFallbackDefinitions.Add(matching);
        request.RepositoryFallbackDefinitions.Add(additional);
        ISchemaImporter importer = new SchemaImporter();

        SchemaCompilationResult result = importer.ImportSchemaStream(
            new MemoryStream(Encoding.UTF8.GetBytes(xsd)),
            request,
            new SchemaRegistryOptions());
        SchemaArtifact schema = result.RootSchema ??
            throw new AssertionException(result.ErrorMessage);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(schema.NamespaceUri, Is.EqualTo("urn:example:fallback"));
            Assert.That(schema.DataTypes.Single(item => item.Name == "RepositoryText").Definition,
                Is.EqualTo("Definition supplied by repository metadata."));
            Assert.That(schema.DataTypes.Single(item => item.Name == "SupplementaryEnvelope").Kind,
                Is.EqualTo(DataTypeKind.ExternalSchema));
            Assert.That(schema.GlobalTypes.Select(item => item.LocalName),
                Is.EqualTo(new[] { "RepositoryText", "SupplementaryEnvelope" }));
            Assert.That(schema.GlobalTypes.Select(item => item.NamespaceUri),
                Is.EqualTo(new[] { "urn:example:fallback", "urn:example:fallback" }));
            Assert.That(schema.RepositoryFallbackDefinitions,
                Is.EqualTo(new[] { matching, additional }));
            Assert.That(result.DiagnosticCodes,
                Is.EqualTo(new[] { "schema.repository_fallback_applied" }));
            Assert.That(result.DiagnosticMessages, Has.Count.EqualTo(1));
            Assert.That(result.DiagnosticLocations, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ImportReportsPolicyFailuresAndUsesSecureBoundedFileParsing()
    {
        byte[] xsd = Encoding.UTF8.GetBytes(CreateSchemaFixture());
        ISchemaImporter importer = new SchemaImporter();
        SchemaRegistryOptions options = new SchemaRegistryOptions
        {
            ResolveIncludes = false,
            ResolveImports = false
        };
        SchemaCompilationResult inputMissing = importer.ImportSchemaStream(
            null,
            CreateSchemaRequest(string.Empty),
            options);
        SchemaCompilationResult requestMissing = importer.ImportSchemaStream(
            new MemoryStream(xsd),
            null,
            options);
        SchemaCompilationResult optionsMissing = importer.ImportSchemaStream(
            new MemoryStream(xsd),
            CreateSchemaRequest(string.Empty),
            null);

        SchemaArtifact mismatchRequest = CreateSchemaRequest("urn:example:wrong");
        SchemaCompilationResult mismatch = importer.ImportSchemaStream(
            new MemoryStream(xsd),
            mismatchRequest,
            options);
        SchemaArtifact hashRequest = CreateSchemaRequest(string.Empty);
        hashRequest.Artifact.HashAlgorithm = "SHA-256";
        hashRequest.Artifact.Hash.Value = new byte[32];
        SchemaCompilationResult hash = importer.ImportSchemaStream(
            new MemoryStream(xsd),
            hashRequest,
            options);
        SchemaRegistryOptions limitedOptions = new SchemaRegistryOptions();
        limitedOptions.Limits.MaxTotalBytes = xsd.Length - 1;
        SchemaCompilationResult limited = importer.ImportSchemaStream(
            new MemoryStream(xsd),
            CreateSchemaRequest(string.Empty),
            limitedOptions);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        SchemaRegistryOptions cancelledOptions = new SchemaRegistryOptions
        {
            CancellationToken = cancellation.Token
        };

        const string dtd = """
            <!DOCTYPE xs:schema [<!ENTITY external SYSTEM "file:///etc/passwd">]>
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:annotation><xs:documentation>&external;</xs:documentation></xs:annotation>
            </xs:schema>
            """;
        string path = Path.Combine(
            Path.GetTempPath(),
            "iso20022-schema-import-" + Guid.NewGuid().ToString("N") + ".xsd");
        File.WriteAllBytes(path, xsd);
        try
        {
            SchemaArtifact fileRequest = CreateSchemaRequest(string.Empty);
            fileRequest.SourceName = string.Empty;
            fileRequest.BaseUri = string.Empty;
            SchemaCompilationResult file = importer.ImportSchemaFile(path, fileRequest, options);

            Assert.Multiple(() =>
            {
                Assert.That(inputMissing.ErrorCode, Is.EqualTo("schema.input_missing"));
                Assert.That(requestMissing.ErrorCode, Is.EqualTo("schema.request_missing"));
                Assert.That(optionsMissing.ErrorCode, Is.EqualTo("schema.options_missing"));
                Assert.That(mismatch.ErrorCode, Is.EqualTo("schema.namespace_mismatch"));
                Assert.That(hash.ErrorCode, Is.EqualTo("schema.artifact_hash_mismatch"));
                Assert.That(limited.ErrorCode, Is.EqualTo("schema.input_limit"));
                Assert.That(
                    () => importer.ImportSchemaStream(
                        new MemoryStream(xsd),
                        CreateSchemaRequest(string.Empty),
                        cancelledOptions),
                    Throws.InstanceOf<OperationCanceledException>());
                Assert.That(file.Success, Is.True, file.ErrorMessage);
                Assert.That(file.RootSchema?.SourceName, Is.EqualTo(Path.GetFileName(path)));
                Assert.That(file.RootSchema?.BaseUri, Does.StartWith("file:"));
                Assert.That(
                    () => importer.ImportSchemaStream(
                        new MemoryStream(Encoding.UTF8.GetBytes(dtd)),
                        CreateSchemaRequest(string.Empty),
                        options),
                    Throws.InstanceOf<XmlException>());
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void RequestValidationRejectsEveryMissingReferenceAndInvalidLimit()
    {
        string[] schemaFields =
        {
            "SourceName", "BaseUri", "NamespaceUri", "Version", "Profile",
            "RootElement", "Content", "IncludeLocations", "ImportNamespaceUris",
            "ImportLocations", "GlobalElements", "GlobalTypes", "DataTypes",
            "MessageComponents", "MessageElements", "Choices", "ExternalSchemas",
            "MessageDefinitions", "RepositoryFallbackDefinitions"
        };
        List<SchemaCompilationResult?> missing = new List<SchemaCompilationResult?>();
        SchemaArtifact missingArtifact = CreateSchemaRequest(string.Empty);
        missingArtifact.Artifact = null!;
        missing.Add(SchemaImporter.ValidateSchemaRequest(
            missingArtifact, new SchemaRegistryOptions()));
        SchemaArtifact missingCoordinate = CreateSchemaRequest(string.Empty);
        missingCoordinate.Artifact.Coordinate = null!;
        missing.Add(SchemaImporter.ValidateSchemaRequest(
            missingCoordinate, new SchemaRegistryOptions()));
        SchemaArtifact missingHash = CreateSchemaRequest(string.Empty);
        missingHash.Artifact.Hash = null!;
        missing.Add(SchemaImporter.ValidateSchemaRequest(
            missingHash, new SchemaRegistryOptions()));
        SchemaArtifact missingHashValue = CreateSchemaRequest(string.Empty);
        missingHashValue.Artifact.Hash.Value = null!;
        missing.Add(SchemaImporter.ValidateSchemaRequest(
            missingHashValue, new SchemaRegistryOptions()));
        foreach (string fieldName in schemaFields)
        {
            SchemaArtifact request = CreateSchemaRequest(string.Empty);
            FieldInfo field = typeof(SchemaArtifact).GetField(fieldName) ??
                throw new AssertionException("Missing schema field " + fieldName);
            field.SetValue(request, null);
            missing.Add(SchemaImporter.ValidateSchemaRequest(
                request, new SchemaRegistryOptions()));
        }

        SchemaRegistryOptions missingRoot = new SchemaRegistryOptions();
        missingRoot.LocalRootPath = null!;
        missing.Add(SchemaImporter.ValidateSchemaRequest(
            CreateSchemaRequest(string.Empty), missingRoot));
        SchemaRegistryOptions missingLimits = new SchemaRegistryOptions();
        missingLimits.Limits = null!;
        missing.Add(SchemaImporter.ValidateSchemaRequest(
            CreateSchemaRequest(string.Empty), missingLimits));
        SchemaRegistryOptions missingLocals = new SchemaRegistryOptions();
        missingLocals.LocalSchemas = null!;
        missing.Add(SchemaImporter.ValidateSchemaRequest(
            CreateSchemaRequest(string.Empty), missingLocals));

        string[] limitFields =
        {
            "MaxDepth", "MaxAttributesPerElement", "MaxTextLength", "MaxElementCount",
            "MaxCollectionCount", "MaxTotalBytes", "MaxSchemaCount", "MaxDiagnosticCount"
        };
        List<SchemaCompilationResult?> invalidLimits = new List<SchemaCompilationResult?>();
        foreach (string fieldName in limitFields)
        {
            SchemaRegistryOptions options = new SchemaRegistryOptions();
            FieldInfo field = typeof(ResourceLimits).GetField(fieldName) ??
                throw new AssertionException("Missing limit field " + fieldName);
            field.SetValue(options.Limits, 0);
            invalidLimits.Add(SchemaImporter.ValidateSchemaRequest(
                CreateSchemaRequest(string.Empty), options));
        }

        Assert.Multiple(() =>
        {
            Assert.That(missing.Select(result => result?.ErrorCode),
                Is.All.EqualTo("schema.request_invalid"));
            Assert.That(invalidLimits.Select(result => result?.ErrorCode),
                Is.All.EqualTo("schema.limits_invalid"));
        });
    }

    [Test]
    public void RequestValidationDistinguishesKindHashAndIdentifierFailures()
    {
        SchemaArtifact wrongKind = CreateSchemaRequest(string.Empty);
        wrongKind.Artifact.Coordinate.Kind = ArtifactKind.Repository;
        SchemaArtifact shortHash = CreateSchemaRequest(string.Empty);
        shortHash.Artifact.Hash.Value = new byte[31];
        SchemaArtifact badHashAlgorithm = CreateSchemaRequest(string.Empty);
        badHashAlgorithm.Artifact.Hash.Value = new byte[32];
        badHashAlgorithm.Artifact.HashAlgorithm = "SHA-1";
        SchemaArtifact badIdentifier = CreateSchemaRequest(string.Empty);
        badIdentifier.MessageIdentifier = new MessageIdentifier
        {
            Value = "bad"
        };
        SchemaArtifact absentIdentifier = CreateSchemaRequest(string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(SchemaImporter.ValidateSchemaRequest(
                wrongKind, new SchemaRegistryOptions())?.ErrorCode,
                Is.EqualTo("schema.artifact_kind_invalid"));
            Assert.That(SchemaImporter.ValidateSchemaRequest(
                shortHash, new SchemaRegistryOptions())?.ErrorCode,
                Is.EqualTo("schema.artifact_hash_invalid"));
            Assert.That(SchemaImporter.ValidateSchemaRequest(
                badHashAlgorithm, new SchemaRegistryOptions())?.ErrorCode,
                Is.EqualTo("schema.artifact_hash_invalid"));
            Assert.That(SchemaImporter.ValidateSchemaRequest(
                badIdentifier, new SchemaRegistryOptions())?.ErrorCode,
                Is.EqualTo("schema.message_identifier_invalid"));
            Assert.That(SchemaImporter.ValidateSchemaRequest(
                absentIdentifier, new SchemaRegistryOptions()), Is.Null);
        });
    }

    [Test]
    public void FileEntryPointAndMalformedSchemaReturnStableFailures()
    {
        ISchemaImporter importer = new SchemaImporter();
        SchemaRegistryOptions options = new SchemaRegistryOptions();
        SchemaArtifact invalid = CreateSchemaRequest(string.Empty);
        invalid.Artifact.Coordinate.Kind = ArtifactKind.Repository;

        SchemaCompilationResult nullPath = importer.ImportSchemaFile(
            null, CreateSchemaRequest(string.Empty), options);
        SchemaCompilationResult emptyPath = importer.ImportSchemaFile(
            string.Empty, CreateSchemaRequest(string.Empty), options);
        SchemaCompilationResult missingRequest = importer.ImportSchemaFile(
            "unused.xsd", null, options);
        SchemaCompilationResult missingOptions = importer.ImportSchemaFile(
            "unused.xsd", CreateSchemaRequest(string.Empty), null);
        SchemaCompilationResult invalidRequest = importer.ImportSchemaFile(
            "unused.xsd", invalid, options);
        SchemaCompilationResult invalidStreamRequest = importer.ImportSchemaStream(
            new MemoryStream("<root />"u8.ToArray()), invalid, options);
        SchemaCompilationResult malformed = importer.ImportSchemaStream(
            new MemoryStream("<root />"u8.ToArray()),
            CreateSchemaRequest(string.Empty),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(nullPath.ErrorCode, Is.EqualTo("schema.path_missing"));
            Assert.That(emptyPath.ErrorCode, Is.EqualTo("schema.path_missing"));
            Assert.That(missingRequest.ErrorCode, Is.EqualTo("schema.request_missing"));
            Assert.That(missingOptions.ErrorCode, Is.EqualTo("schema.options_missing"));
            Assert.That(invalidRequest.ErrorCode, Is.EqualTo("schema.artifact_kind_invalid"));
            Assert.That(invalidStreamRequest.ErrorCode,
                Is.EqualTo("schema.artifact_kind_invalid"));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.RootSchema, Is.Null);
        });
    }

    [Test]
    public void CopyObservesCancellationAfterARead()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        SchemaRegistryOptions options = new SchemaRegistryOptions
        {
            CancellationToken = cancellation.Token
        };
        using Stream input = new CancellingReadStream(
            new byte[] { 1, 2, 3 }, cancellation);

        Assert.That(() => SchemaImporter.CopySchemaInput(input, 10, options),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void RepositoryFallbackAndDiagnosticsCoverNoopAndBoundedCases()
    {
        SchemaArtifact schema = CreateSchemaRequest(string.Empty);
        SchemaRegistryOptions disabled = new SchemaRegistryOptions
        {
            AllowRepositoryFallback = false
        };
        SchemaCompilationResult result = new SchemaCompilationResult();
        SchemaImporter.ApplyRepositoryFallback(schema, disabled, result);

        ExternalSchemaDefinition nameless = new ExternalSchemaDefinition
        {
            Name = string.Empty
        };
        nameless.NamespaceUris.Add("urn:fallback");
        bool namespaceOnly = SchemaImporter.ApplyOneRepositoryFallback(schema, nameless);
        ExternalSchemaDefinition nullName = new ExternalSchemaDefinition();
        nullName.Name = null!;
        bool nullNameResult = SchemaImporter.ApplyOneRepositoryFallback(schema, nullName);
        DataTypeDefinition existing = new DataTypeDefinition
        {
            Name = "Existing",
            Definition = "already present"
        };
        schema.DataTypes.Add(existing);
        ExternalSchemaDefinition unchanged = new ExternalSchemaDefinition
        {
            Name = "Existing",
            Definition = string.Empty
        };
        bool existingResult = SchemaImporter.ApplyOneRepositoryFallback(schema, unchanged);
        SchemaRegistryOptions bounded = new SchemaRegistryOptions();
        bounded.Limits.MaxDiagnosticCount = 0;
        SchemaImporter.AddSchemaDiagnostic(
            result, bounded, "ignored", "ignored", new SourceLocation());

        Assert.Multiple(() =>
        {
            Assert.That(namespaceOnly, Is.True);
            Assert.That(schema.NamespaceUri, Is.EqualTo("urn:fallback"));
            Assert.That(nullNameResult, Is.False);
            Assert.That(existingResult, Is.False);
            Assert.That(SchemaImporter.FindSchemaDataType(schema, "Existing"),
                Is.SameAs(existing));
            Assert.That(SchemaImporter.FindSchemaDataType(schema, "Missing"), Is.Null);
            Assert.That(result.DiagnosticCodes, Is.Empty);
        });
    }

    [Test]
    public void IdentityFinalizationHandlesAbsentKnownAndUnknownNamespaces()
    {
        SchemaXsdReadState state = new SchemaXsdReadState
        {
            ParsedVersion = "1"
        };
        SchemaArtifact source = CreateSchemaRequest(string.Empty);
        SchemaArtifact absent = CreateSchemaRequest(string.Empty);
        absent.MessageIdentifier = null;
        SchemaCompilationResult? absentResult = SchemaImporter.FinalizeSchemaIdentity(
            absent, source, state);

        SchemaArtifact known = CreateSchemaRequest(
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10");
        known.MessageIdentifier = new MessageIdentifier { Value = "pacs.008.001.10" };
        SchemaCompilationResult? knownResult = SchemaImporter.FinalizeSchemaIdentity(
            known, source, state);

        SchemaArtifact unknown = CreateSchemaRequest("not a namespace");
        unknown.MessageIdentifier = null;
        SchemaCompilationResult? unknownResult = SchemaImporter.FinalizeSchemaIdentity(
            unknown, source, state);

        Assert.Multiple(() =>
        {
            Assert.That(absentResult, Is.Null);
            Assert.That(absent.Version, Is.EqualTo("1"));
            Assert.That(absent.MessageIdentifier, Is.Null);
            Assert.That(knownResult, Is.Null);
            Assert.That(known.MessageIdentifier?.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(unknownResult, Is.Null);
            Assert.That(unknown.MessageIdentifier, Is.Null);
        });
    }

    private static SchemaArtifact CreateSchemaRequest(string namespaceUri)
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
                IsApproved = true
            },
            SourceName = "pacs.008.001.10.xsd",
            BaseUri = "file:///schemas/pacs.008.001.10.xsd",
            NamespaceUri = namespaceUri,
            Profile = "base",
            IsEntryPoint = true
        };
    }

    private static string CreateSchemaFixture()
    {
        return """
            <?xml version="1.0" encoding="UTF-8"?>
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              targetNamespace="urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10"
              version="fixture-v1">
              <xs:include schemaLocation="common-types.xsd"/>
              <xs:import namespace="urn:iso:std:iso:20022:tech:xsd:head.001.001.04"
                schemaLocation="head.001.001.04.xsd"/>
              <xs:simpleType name="Max35Text">
                <xs:annotation>
                  <xs:documentation>Text limited to thirty-five characters.</xs:documentation>
                </xs:annotation>
                <xs:restriction base="xs:string">
                  <xs:minLength value="1"/>
                  <xs:maxLength value="35"/>
                  <xs:pattern value="[A-Z0-9 ]+"/>
                </xs:restriction>
              </xs:simpleType>
              <xs:simpleType name="PaymentStatusCode">
                <xs:restriction base="xs:string">
                  <xs:enumeration value="ACTV"/>
                  <xs:enumeration value="INAC"/>
                </xs:restriction>
              </xs:simpleType>
              <xs:complexType name="PaymentInstruction10">
                <xs:annotation><xs:documentation>A payment instruction.</xs:documentation></xs:annotation>
                <xs:sequence>
                  <xs:element name="Amount" type="xs:decimal" minOccurs="0" maxOccurs="unbounded">
                    <xs:annotation><xs:documentation>The instructed amount.</xs:documentation></xs:annotation>
                  </xs:element>
                </xs:sequence>
              </xs:complexType>
              <xs:element name="Document" type="PaymentInstruction10"/>
            </xs:schema>
            """;
    }

    private sealed class CancellingReadStream : MemoryStream
    {
        private readonly CancellationTokenSource cancellation;

        internal CancellingReadStream(
            byte[] content,
            CancellationTokenSource cancellation) : base(content)
        {
            this.cancellation = cancellation;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = base.Read(buffer, offset, count);
            if (read > 0)
            {
                cancellation.Cancel();
            }
            return read;
        }
    }
}
