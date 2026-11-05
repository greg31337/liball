using System.Reflection;
using System.Text;
using System.Text.Json;
using iso20022;

namespace iso20022.Tests;

public class ExternalCodeSetImporterBoundaryTests
{
    [Test]
    public void EntryPointsRejectMissingInputsRequestsAndExpandedSize()
    {
        ExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        ExternalCodeImportRequest jsonSize = CreateRequest();
        jsonSize.Limits.MaxTotalBytes = 1;
        ExternalCodeImportRequest xsdSize = CreateRequest();
        xsdSize.Limits.MaxTotalBytes = 1;

        Assert.Multiple(() =>
        {
            Assert.That(importer.ImportJsonStream(null, CreateRequest()).ErrorCode,
                Is.EqualTo("external-code.input_missing"));
            Assert.That(importer.ImportJsonStream(new MemoryStream(), null).ErrorCode,
                Is.EqualTo("external-code.request_missing"));
            Assert.That(importer.ImportXsdStream(null, CreateRequest()).ErrorCode,
                Is.EqualTo("external-code.input_missing"));
            Assert.That(importer.ImportXsdStream(new MemoryStream(), null).ErrorCode,
                Is.EqualTo("external-code.request_missing"));
            Assert.That(importer.ImportXlsxStream(null, CreateRequest()).ErrorCode,
                Is.EqualTo("external-code.input_missing"));
            Assert.That(importer.ImportXlsxStream(new MemoryStream(), null).ErrorCode,
                Is.EqualTo("external-code.request_missing"));
            Assert.That(importer.ImportJsonStream(Bytes("{}"), jsonSize).ErrorCode,
                Is.EqualTo("external-code.json.size"));
            Assert.That(importer.ImportXsdStream(Bytes("<x/>"), xsdSize).ErrorCode,
                Is.EqualTo("external-code.xsd.size"));
            Assert.That(importer.CompareCatalogs(null).ErrorCode,
                Is.EqualTo("external-code.compare.request_missing"));
        });
    }

    [Test]
    public void EveryRequestReferenceAndLimitIsValidatedForEveryFormat()
    {
        string[] requestReferences =
        {
            "SourceName", "ArchiveEntryName", "Edition", "Artifact", "Limits"
        };
        string[] manifestReferences = { "Coordinate", "Hash" };
        string[] requestLimits =
        {
            "MaxArtifactBytes", "MaxArchiveEntryBytes", "MaxArchiveEntryCount",
            "MaxArchiveExpandedBytes", "MaxArchiveCompressionRatio"
        };
        string[] commonLimits =
        {
            "MaxDepth", "MaxTextLength", "MaxElementCount", "MaxCollectionCount",
            "MaxTotalBytes", "MaxSchemaCount", "MaxDiagnosticCount"
        };

        foreach (string name in requestReferences)
        {
            AssertInvalidReference(name, false);
        }

        foreach (string name in manifestReferences)
        {
            AssertInvalidManifestReference(name);
        }

        ExternalCodeImportRequest missingHashValue = CreateRequest();
        SetField(missingHashValue.Artifact.Hash, "Value", null);
        AssertEveryFormat(missingHashValue, "external-code.request_invalid");

        foreach (string name in requestLimits)
        {
            ExternalCodeImportRequest request = CreateRequest();
            SetField(request, name, 0);
            AssertEveryFormat(request, "external-code.limits_invalid");
        }

        foreach (string name in commonLimits)
        {
            ExternalCodeImportRequest request = CreateRequest();
            SetField(request.Limits, name, 0);
            AssertEveryFormat(request, "external-code.limits_invalid");
        }

        ExternalCodeImportRequest attributes = CreateRequest();
        attributes.Limits.MaxAttributesPerElement = 0;
        Assert.That(Import(attributes, "JSON").ErrorCode,
            Is.Not.EqualTo("external-code.limits_invalid"));
        Assert.That(Import(attributes, "XSD").ErrorCode,
            Is.EqualTo("external-code.limits_invalid"));
        Assert.That(Import(attributes, "XLSX").ErrorCode,
            Is.EqualTo("external-code.limits_invalid"));

        ExternalCodeImportRequest inconsistent = CreateRequest();
        inconsistent.MaxArchiveEntryBytes = 2;
        inconsistent.MaxArchiveExpandedBytes = 1;
        AssertEveryFormat(inconsistent, "external-code.limits_invalid");
    }

    [Test]
    public void EveryFormatValidatesArtifactHashLengthAndAlgorithm()
    {
        ExternalCodeImportRequest shortHash = CreateRequest();
        shortHash.Artifact.Hash.Value = new byte[1];
        ExternalCodeImportRequest wrongAlgorithm = CreateRequest();
        wrongAlgorithm.Artifact.Hash.Value = new byte[32];
        wrongAlgorithm.Artifact.HashAlgorithm = "MD5";
        ExternalCodeImportRequest accepted = CreateRequest();
        accepted.Artifact.Hash.Value = new byte[32];
        accepted.Artifact.HashAlgorithm = "sha256";

        AssertEveryFormat(shortHash, "external-code.artifact.hash_invalid");
        AssertEveryFormat(wrongAlgorithm, "external-code.artifact.hash_invalid");
        Assert.Multiple(() =>
        {
            Assert.That(ExternalCodeSetImporter.ArtifactHashAlgorithmIsSha256(null), Is.False);
            Assert.That(ExternalCodeSetImporter.ArtifactHashAlgorithmIsSha256("sha-256"), Is.True);
            Assert.That(ExternalCodeSetImporter.ArtifactHashAlgorithmIsSha256("sha256"), Is.True);
            Assert.That(ExternalCodeSetImporter.ValidateJsonRequest(accepted), Is.Null);
            Assert.That(ExternalCodeSetImporter.ValidateXsdRequest(accepted), Is.Null);
            Assert.That(ExternalCodeSetImporter.ValidateXlsxRequest(accepted), Is.Null);
        });
    }

    [TestCase("[]", "external-code.json.root")]
    [TestCase("{\"a\":1,\"a\":2}", "external-code.json.root")]
    [TestCase("{\"type\":\"object\",\"definitions\":{}}", "external-code.json.schema")]
    [TestCase("{\"$schema\":null,\"type\":\"object\",\"definitions\":{}}", "external-code.json.schema")]
    [TestCase("{\"$schema\":\"\",\"type\":\"object\",\"definitions\":{}}", "external-code.json.schema")]
    [TestCase("{\"$schema\":\"s\",\"type\":\"object\"}", "external-code.json.definitions")]
    [TestCase("{\"$schema\":\"s\",\"type\":\"object\",\"definitions\":[]}", "external-code.json.definitions")]
    [TestCase("{\"$schema\":\"s\",\"type\":\"object\",\"definitions\":{\"A\":{},\"A\":{}}}", "external-code.json.definitions")]
    public void JsonRejectsInvalidRootMetadata(string json, string expected)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        ExternalCodeImportResult result = new ExternalCodeImportResult();
        ExternalCodeSetImporter.ParseJsonCatalog(
            document.RootElement,
            CreateRequest(),
            new ExternalCodeArtifactOpenResult(),
            result);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
    }

    [TestCase("[]", "external-code.json.definition_shape")]
    [TestCase("{\"a\":1,\"a\":2}", "external-code.json.definition_shape")]
    [TestCase("{}", "external-code.json.definition_type")]
    [TestCase("{\"type\":\"number\"}", "external-code.json.definition_type")]
    [TestCase("{\"type\":\"string\"}", "external-code.json.description")]
    [TestCase("{\"type\":\"string\",\"description\":null}", "external-code.json.description")]
    [TestCase("{\"type\":\"string\",\"description\":\"d\",\"minLength\":\"1\"}", "external-code.json.length_facet")]
    [TestCase("{\"type\":\"string\",\"description\":\"d\",\"minLength\":-1}", "external-code.json.length_facet")]
    [TestCase("{\"type\":\"string\",\"description\":\"d\",\"minLength\":2147483648}", "external-code.json.length_facet")]
    [TestCase("{\"type\":\"string\",\"description\":\"d\",\"minLength\":2,\"maxLength\":1}", "external-code.json.length_range")]
    [TestCase("{\"type\":\"string\",\"description\":\"d\",\"pattern\":1}", "external-code.json.pattern")]
    [TestCase("{\"type\":\"string\",\"description\":\"d\",\"enum\":{}}", "external-code.json.values")]
    [TestCase("{\"type\":\"string\",\"description\":\"d\",\"enum\":[1]}", "external-code.json.code")]
    [TestCase("{\"type\":\"string\",\"description\":\"d\",\"enum\":[\"\"]}", "external-code.json.code")]
    public void JsonRejectsInvalidDefinitionShapes(string definition, string expected)
    {
        string json = "{\"$schema\":\"s\",\"type\":\"object\",\"definitions\":{\"A\":" +
            definition + "}}";
        Assert.That(ImportJson(json, CreateRequest()).ErrorCode, Is.EqualTo(expected));
    }

    [Test]
    public void JsonHonorsEveryDefinitionAndTextLimit()
    {
        ExternalCodeImportRequest collection = CreateRequest();
        collection.Limits.MaxCollectionCount = 1;
        ExternalCodeImportRequest elements = CreateRequest();
        elements.Limits.MaxElementCount = 1;
        ExternalCodeImportRequest identifier = CreateRequest();
        identifier.Limits.MaxTextLength = 1;
        ExternalCodeImportRequest schemaIdentifier = CreateRequest();
        schemaIdentifier.Limits.MaxTextLength = 2;
        ExternalCodeImportRequest description = CreateRequest();
        description.Limits.MaxTextLength = 1;
        ExternalCodeImportRequest pattern = CreateRequest();
        pattern.Limits.MaxTextLength = 1;
        ExternalCodeImportRequest values = CreateRequest();
        values.Limits.MaxCollectionCount = 1;
        ExternalCodeImportRequest code = CreateRequest();
        code.Limits.MaxTextLength = 1;
        string twoDefinitions = "{\"$schema\":\"s\",\"type\":\"object\",\"definitions\":{\"A\":{},\"B\":{}}}";
        string basic = "{\"$schema\":\"s\",\"type\":\"object\",\"definitions\":{\"AA\":{\"type\":\"string\",\"description\":\"dd\",\"pattern\":\"pp\",\"enum\":[\"AA\",\"BB\"]}}}";
        string longSchemaId = "{\"$schema\":\"s\",\"type\":\"object\",\"properties\":{\"$id\":{\"default\":\"long\"}},\"definitions\":{}}";

        Assert.Multiple(() =>
        {
            Assert.That(ImportJson(twoDefinitions, collection).ErrorCode,
                Is.EqualTo("external-code.json.definition_limit"));
            Assert.That(ImportJson(twoDefinitions, elements).ErrorCode,
                Is.EqualTo("external-code.json.definition_limit"));
            Assert.That(ImportJson(basic, identifier).ErrorCode,
                Is.EqualTo("external-code.json.definition_identifier"));
            Assert.That(ImportJson(longSchemaId, schemaIdentifier).ErrorCode,
                Is.EqualTo("external-code.json.schema_identifier"));
            Assert.That(ImportJson(basic, description).ErrorCode,
                Is.EqualTo("external-code.json.definition_identifier"));
            Assert.That(ImportJson(basic, pattern).ErrorCode,
                Is.EqualTo("external-code.json.definition_identifier"));
            Assert.That(ImportJson(basic, values).ErrorCode,
                Is.EqualTo("external-code.json.values"));
            Assert.That(ImportJson(basic, code).ErrorCode,
                Is.EqualTo("external-code.json.definition_identifier"));
        });
    }

    [Test]
    public void JsonReportsMissingEditionTotalCodeLimitAndMissingCodeDefinition()
    {
        const string oneCode = "{\"$schema\":\"s\",\"type\":\"object\",\"definitions\":{\"A\":{\"type\":\"string\",\"description\":\"d\",\"enum\":[\"A\"]}}}";
        ExternalCodeImportRequest missingEdition = CreateRequest();
        missingEdition.Edition = string.Empty;
        missingEdition.Artifact.Coordinate.Edition = string.Empty;
        ExternalCodeImportRequest elementLimit = CreateRequest();
        elementLimit.Limits.MaxElementCount = 1;
        ExternalCodeImportResult missingDefinition = ImportJson(oneCode, CreateRequest());

        Assert.Multiple(() =>
        {
            Assert.That(ImportJson(
                "{\"$schema\":\"s\",\"type\":\"object\",\"definitions\":{}}",
                missingEdition).ErrorCode, Is.EqualTo("external-code.json.edition_missing"));
            Assert.That(ImportJson(oneCode, elementLimit).ErrorCode,
                Is.EqualTo("external-code.json.code_limit"));
            Assert.That(missingDefinition.Success, Is.True);
            Assert.That(missingDefinition.DiagnosticCodes,
                Does.Contain("external-code.json.code_definition_unavailable"));
        });
    }

    [Test]
    public void JsonHelpersCoverFallbacksPreambleAndTextExtraction()
    {
        using JsonDocument nonObject = JsonDocument.Parse("[]");
        using JsonDocument objectDocument = JsonDocument.Parse(
            "{\"properties\":{\"$id\":{\"default\":\"id\"}},\"a\":1,\"b\":2}");
        ExternalCodeImportRequest request = CreateRequest();
        request.Edition = string.Empty;
        request.Artifact.Coordinate.Edition = "coordinate";
        ExternalCodeImportRequest inferred = CreateRequest();
        inferred.Edition = string.Empty;
        inferred.Artifact.Coordinate.Edition = string.Empty;

        Assert.Multiple(() =>
        {
            Assert.That(ExternalCodeSetImporter.FindJsonMember(nonObject.RootElement, "x").ValueKind,
                Is.EqualTo(JsonValueKind.Undefined));
            Assert.That(ExternalCodeSetImporter.CountObjectMembers(objectDocument.RootElement),
                Is.EqualTo(3));
            Assert.That(ExternalCodeSetImporter.ReadSchemaIdentifier(
                objectDocument.RootElement, "fallback"), Is.EqualTo("id"));
            Assert.That(ExternalCodeSetImporter.ReadSchemaIdentifier(
                nonObject.RootElement, "fallback"), Is.EqualTo("fallback"));
            Assert.That(ExternalCodeSetImporter.ReadSchemaVersion("other"), Is.EqualTo("other"));
            Assert.That(ExternalCodeSetImporter.ExtractSetDefinition("*`A`-Value"), Is.Empty);
            Assert.That(ExternalCodeSetImporter.ExtractSetDefinition("Definition\n*`A`-Value"),
                Is.EqualTo("Definition"));
            Assert.That(ExternalCodeSetImporter.ExtractCodeDefinition("none", "A"), Is.Empty);
            Assert.That(ExternalCodeSetImporter.ExtractCodeDefinition(
                "*`A`-First\n*`B`-Second", "A"), Is.EqualTo("First"));
            Assert.That(ExternalCodeSetImporter.ResolveEdition(request, "x"),
                Is.EqualTo("coordinate"));
            Assert.That(ExternalCodeSetImporter.ResolveEdition(inferred, "2q2027_file.json"),
                Is.EqualTo("2027-Q2"));
            Assert.That(ExternalCodeSetImporter.ResolveEdition(inferred, "bad"), Is.Empty);
            Assert.That(ExternalCodeSetImporter.ResolveEdition(inferred, "5Q2027_file.json"), Is.Empty);
            Assert.That(ExternalCodeSetImporter.ResolveEdition(inferred, "1X2027_file.json"), Is.Empty);
            Assert.That(ExternalCodeSetImporter.ResolveEdition(inferred, "1QABCD_file.json"), Is.Empty);
            Assert.That(ExternalCodeSetImporter.RemoveUtf8Preamble(new byte[] { 1, 2 }),
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(ExternalCodeSetImporter.RemoveUtf8Preamble(
                new byte[] { 239, 187, 191, 1 }), Is.EqualTo(new byte[] { 1 }));
        });
    }

    private static void AssertInvalidReference(string name, bool unused)
    {
        ExternalCodeImportRequest request = CreateRequest();
        SetField(request, name, null);
        AssertEveryFormat(request, "external-code.request_invalid");
    }

    private static void AssertInvalidManifestReference(string name)
    {
        ExternalCodeImportRequest request = CreateRequest();
        SetField(request.Artifact, name, null);
        AssertEveryFormat(request, "external-code.request_invalid");
    }

    private static void AssertEveryFormat(ExternalCodeImportRequest request, string error)
    {
        Assert.That(Import(request, "JSON").ErrorCode, Is.EqualTo(error));
        Assert.That(Import(request, "XSD").ErrorCode, Is.EqualTo(error));
        Assert.That(Import(request, "XLSX").ErrorCode, Is.EqualTo(error));
    }

    private static ExternalCodeImportResult Import(ExternalCodeImportRequest request, string format)
    {
        ExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        if (format == "JSON")
        {
            return importer.ImportJsonStream(Bytes("{}"), request);
        }

        if (format == "XSD")
        {
            return importer.ImportXsdStream(Bytes("<x/>"), request);
        }

        return importer.ImportXlsxStream(new MemoryStream(), request);
    }

    private static ExternalCodeImportResult ImportJson(
        string json,
        ExternalCodeImportRequest request)
    {
        return new ExternalCodeSetImporter().ImportJsonStream(Bytes(json), request);
    }

    private static MemoryStream Bytes(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value));
    }

    private static ExternalCodeImportRequest CreateRequest()
    {
        return new ExternalCodeImportRequest
        {
            SourceName = "fixture.json",
            Edition = "2026-Q1",
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.ExternalCodeSet,
                    Edition = "2026-Q1",
                    Identifier = "external-codes",
                    Version = "1"
                },
                OriginUri = "https://example.test/external-codes"
            }
        };
    }

    private static void SetField(object target, string name, object? value)
    {
        FieldInfo field = target.GetType().GetField(name) ??
            throw new AssertionException("Missing field " + name);
        field.SetValue(target, value);
    }
}
