using iso20022;

namespace iso20022.Tests;

public class IdentityNamespaceServiceTests
{
    [TestCase("pacs.008.001.10", "pacs", "008", "001", "10")]
    [TestCase("head.001.001.04", "head", "001", "001", "04")]
    [TestCase("nvlp.002.001.02", "nvlp", "002", "001", "02")]
    public void IdentifierParserReturnsExactComponents(string text, string area, string functionality, string variant, string version)
    {
        IMessageIdentifierParser parser = new MessageIdentifierParser();

        MessageIdentifierParseResult result = parser.ParseIdentifier(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Identifier, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Identifier?.BusinessArea, Is.EqualTo(area));
            Assert.That(result.Identifier?.MessageFunctionality, Is.EqualTo(functionality));
            Assert.That(result.Identifier?.Variant, Is.EqualTo(variant));
            Assert.That(result.Identifier?.Version, Is.EqualTo(version));
            Assert.That(result.Identifier?.Value, Is.EqualTo(text));
            Assert.That(result.ErrorCode, Is.Empty);
            Assert.That(result.ErrorMessage, Is.Empty);
        });
    }

    [TestCase(null, "identifier.null")]
    [TestCase("", "identifier.invalid")]
    [TestCase("PACS.008.001.10", "identifier.invalid")]
    [TestCase("pacs.8.001.10", "identifier.invalid")]
    [TestCase("pacs.008.001.1", "identifier.invalid")]
    public void IdentifierParserReportsExpectedInvalidInputWithoutThrowing(string? text, string errorCode)
    {
        IMessageIdentifierParser parser = new MessageIdentifierParser();

        MessageIdentifierParseResult result = parser.ParseIdentifier(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Identifier, Is.Null);
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        });
    }

    [TestCase("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10", NamespaceKind.Message)]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:head.001.001.04", NamespaceKind.Header)]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:nvlp.001.001.02", NamespaceKind.Envelope)]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:supl.001.001.01", NamespaceKind.Supplementary)]
    public void StrictNamespaceParsingClassifiesIdentifierFamilies(string namespaceUri, NamespaceKind expectedKind)
    {
        INamespaceService service = new NamespaceService();

        NamespaceServiceResult result = service.ParseNamespace(namespaceUri, NamespaceMode.Strict);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Descriptor, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Descriptor?.Uri, Is.EqualTo(namespaceUri));
            Assert.That(result.Descriptor?.Kind, Is.EqualTo(expectedKind));
            Assert.That(result.Descriptor?.Mode, Is.EqualTo(NamespaceMode.Strict));
            Assert.That(result.Descriptor?.MessageIdentifier?.Value, Is.EqualTo(namespaceUri.Substring(Iso20022Namespaces.SchemaUrnPrefix.Length)));
            Assert.That(result.Repairs, Is.Empty);
        });
    }

    [TestCase("urn:iso:std:iso:20022:2013:ecore")]
    [TestCase("urn:iso:std:iso:20022:2013:ecore:extension")]
    public void StrictNamespaceParsingRecognizesRepositoryNamespaces(string namespaceUri)
    {
        INamespaceService service = new NamespaceService();

        NamespaceServiceResult result = service.ParseNamespace(namespaceUri, NamespaceMode.Strict);

        Assert.That(result.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Descriptor?.Kind, Is.EqualTo(NamespaceKind.Repository));
            Assert.That(result.Descriptor?.MessageIdentifier, Is.Null);
            Assert.That(result.Repairs, Is.Empty);
        });
    }

    [Test]
    public void UnknownValidNamespaceIsPreservedWithoutReclassification()
    {
        INamespaceService service = new NamespaceService();

        NamespaceServiceResult result = service.ParseNamespace("relative/custom/namespace", NamespaceMode.Strict);

        Assert.That(result.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Descriptor?.Uri, Is.EqualTo("relative/custom/namespace"));
            Assert.That(result.Descriptor?.Kind, Is.EqualTo(NamespaceKind.Unknown));
            Assert.That(result.Descriptor?.MessageIdentifier, Is.Null);
            Assert.That(result.Repairs, Is.Empty);
        });
    }

    [Test]
    public void LenientNamespaceParsingRecordsEveryRepairInOrder()
    {
        INamespaceService service = new NamespaceService();
        string input = " \tURN:ISO:STD:ISO:20022:TECH:XSD:PACS.008.001.10/\r\n";

        NamespaceServiceResult result = service.ParseNamespace(input, NamespaceMode.Lenient);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Repairs, Has.Count.EqualTo(4));
        Assert.Multiple(() =>
        {
            Assert.That(result.Descriptor?.Uri, Is.EqualTo("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10"));
            Assert.That(result.Descriptor?.Kind, Is.EqualTo(NamespaceKind.Message));
            Assert.That(result.Descriptor?.Mode, Is.EqualTo(NamespaceMode.Lenient));
            Assert.That(result.Repairs[0].Kind, Is.EqualTo(NamespaceRepairKind.TrimXmlWhitespace));
            Assert.That(result.Repairs[1].Kind, Is.EqualTo(NamespaceRepairKind.RemoveTrailingSlash));
            Assert.That(result.Repairs[2].Kind, Is.EqualTo(NamespaceRepairKind.NormalizeTechnicalPrefixCase));
            Assert.That(result.Repairs[3].Kind, Is.EqualTo(NamespaceRepairKind.NormalizeBusinessAreaCase));
            Assert.That(result.Repairs[0].OriginalValue, Is.EqualTo(input));
            Assert.That(result.Repairs[3].NormalizedValue, Is.EqualTo(result.Descriptor?.Uri));
        });
    }

    [TestCase("URN:ISO:STD:ISO:20022:TECH:XSD:pacs.008.001.10", "namespace.noncanonical")]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:PACS.008.001.10", "namespace.message_identifier")]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10/", "namespace.message_identifier")]
    [TestCase("namespace with spaces", "namespace.invalid_uri")]
    public void StrictNamespaceParsingRejectsRepairableAndMalformedInput(string namespaceUri, string errorCode)
    {
        INamespaceService service = new NamespaceService();

        NamespaceServiceResult result = service.ParseNamespace(namespaceUri, NamespaceMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Descriptor, Is.Null);
            Assert.That(result.Repairs, Is.Empty);
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        });
    }

    [Test]
    public void NamespaceParsingReportsNullAndUnsupportedModesWithoutThrowing()
    {
        INamespaceService service = new NamespaceService();

        NamespaceServiceResult nullResult = service.ParseNamespace(null, NamespaceMode.Strict);
        NamespaceServiceResult modeResult = service.ParseNamespace("urn:example:value", (NamespaceMode)99);

        Assert.Multiple(() =>
        {
            Assert.That(nullResult.Success, Is.False);
            Assert.That(nullResult.ErrorCode, Is.EqualTo("namespace.null"));
            Assert.That(modeResult.Success, Is.False);
            Assert.That(modeResult.ErrorCode, Is.EqualTo("namespace.mode"));
        });
    }

    [TestCase("pacs", NamespaceKind.Message)]
    [TestCase("head", NamespaceKind.Header)]
    [TestCase("nvlp", NamespaceKind.Envelope)]
    [TestCase("supl", NamespaceKind.Supplementary)]
    public void NamespaceBuilderCreatesCanonicalNamespaceAndClassification(string area, NamespaceKind expectedKind)
    {
        INamespaceService service = new NamespaceService();
        MessageIdentifier identifier = new MessageIdentifier
        {
            BusinessArea = area,
            MessageFunctionality = "001",
            Variant = "001",
            Version = "01",
            Value = area + ".001.001.01"
        };

        NamespaceServiceResult result = service.BuildNamespaceFromIdentifier(identifier);

        Assert.That(result.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Descriptor?.Uri, Is.EqualTo(Iso20022Namespaces.SchemaUrnPrefix + area + ".001.001.01"));
            Assert.That(result.Descriptor?.Kind, Is.EqualTo(expectedKind));
            Assert.That(result.Descriptor?.Mode, Is.EqualTo(NamespaceMode.Strict));
            Assert.That(result.Descriptor?.MessageIdentifier, Is.Not.SameAs(identifier));
            Assert.That(result.Descriptor?.MessageIdentifier?.Value, Is.EqualTo(area + ".001.001.01"));
            Assert.That(result.Repairs, Is.Empty);
        });
    }

    [Test]
    public void NamespaceBuilderReportsNullAndInvalidIdentifiersWithoutThrowing()
    {
        INamespaceService service = new NamespaceService();
        MessageIdentifier invalid = new MessageIdentifier
        {
            BusinessArea = "PACS",
            MessageFunctionality = "008",
            Variant = "001",
            Version = "10"
        };

        NamespaceServiceResult nullResult = service.BuildNamespaceFromIdentifier(null);
        NamespaceServiceResult invalidResult = service.BuildNamespaceFromIdentifier(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(nullResult.Success, Is.False);
            Assert.That(nullResult.ErrorCode, Is.EqualTo("namespace.identifier_null"));
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.ErrorCode, Is.EqualTo("namespace.identifier_invalid"));
        });
    }

    [Test]
    public void NamespaceBuilderRejectsConflictingCompleteIdentifier()
    {
        INamespaceService service = new NamespaceService();
        MessageIdentifier identifier = new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = "10",
            Value = "pacs.008.001.09"
        };

        NamespaceServiceResult result = service.BuildNamespaceFromIdentifier(identifier);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Descriptor, Is.Null);
            Assert.That(result.ErrorCode, Is.EqualTo("namespace.identifier_conflict"));
        });
    }

    [Test]
    public void NamespaceBuilderCanonicalizesAnIdentifierWithoutCachedText()
    {
        MessageIdentifier identifier = new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = "10"
        };

        NamespaceServiceResult result = new NamespaceService()
            .BuildNamespaceFromIdentifier(identifier);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Descriptor?.MessageIdentifier?.Value,
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(identifier.Value, Is.Empty);
        });
    }

    [Test]
    public void NamespaceWhitespaceAndAreaHelpersCoverShortCircuitBoundaries()
    {
        string prefix = Iso20022Namespaces.SchemaUrnPrefix;

        Assert.Multiple(() =>
        {
            Assert.That(NamespaceService.TrimXmlWhitespace(string.Empty), Is.Empty);
            Assert.That(NamespaceService.TrimXmlWhitespace(" \t\r\n"), Is.Empty);
            Assert.That(NamespaceService.TrimXmlWhitespace("value "), Is.EqualTo("value"));
            Assert.That(NamespaceService.NormalizeBusinessArea(
                prefix + "[acs.008.001.10"), Is.EqualTo(prefix + "[acs.008.001.10"));
            Assert.That(NamespaceService.NormalizeBusinessArea(
                prefix + "1acs.008.001.10"), Is.EqualTo(prefix + "1acs.008.001.10"));
            Assert.That(NamespaceService.NormalizeBusinessArea(
                prefix + "pacs.008.001.10"), Is.EqualTo(prefix + "pacs.008.001.10"));
        });
    }
}
