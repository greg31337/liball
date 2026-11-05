using iso20022;

namespace iso20022.Tests;

public class IdentityNamespaceExhaustiveTests
{
    [TestCase("pacs.008.001.10", NamespaceKind.Message)]
    [TestCase("admi.004.001.02", NamespaceKind.Message)]
    [TestCase("head.001.001.04", NamespaceKind.Header)]
    [TestCase("head.002.001.01", NamespaceKind.Header)]
    [TestCase("nvlp.001.001.02", NamespaceKind.Envelope)]
    [TestCase("nvlp.002.001.02", NamespaceKind.Envelope)]
    [TestCase("supl.001.001.01", NamespaceKind.Supplementary)]
    public void EveryIdentifierFamilyRoundTripsThroughThePublicApis(string identifierText, NamespaceKind expectedKind)
    {
        IMessageIdentifierParser parser = new MessageIdentifierParser();
        INamespaceService service = new NamespaceService();

        MessageIdentifierParseResult identifierResult = parser.ParseIdentifier(identifierText);

        Assert.That(identifierResult.Success, Is.True);
        Assert.That(identifierResult.Identifier, Is.Not.Null);
        NamespaceServiceResult buildResult = service.BuildNamespaceFromIdentifier(identifierResult.Identifier);
        Assert.That(buildResult.Success, Is.True);
        Assert.That(buildResult.Descriptor, Is.Not.Null);
        NamespaceServiceResult parseResult = service.ParseNamespace(buildResult.Descriptor?.Uri, NamespaceMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(buildResult.Descriptor?.Kind, Is.EqualTo(expectedKind));
            Assert.That(buildResult.Descriptor?.MessageIdentifier?.Value, Is.EqualTo(identifierText));
            Assert.That(parseResult.Success, Is.True);
            Assert.That(parseResult.Descriptor?.Kind, Is.EqualTo(expectedKind));
            Assert.That(parseResult.Descriptor?.MessageIdentifier?.Value, Is.EqualTo(identifierText));
            Assert.That(parseResult.Descriptor?.Uri, Is.EqualTo(Iso20022Namespaces.SchemaUrnPrefix + identifierText));
        });
    }

    [TestCase("pacs", NamespaceKind.Message)]
    [TestCase("head", NamespaceKind.Header)]
    [TestCase("nvlp", NamespaceKind.Envelope)]
    [TestCase("supl", NamespaceKind.Supplementary)]
    public void EveryTwoDigitVersionIncludingLeadingZeroVersionsIsPreserved(string area, NamespaceKind expectedKind)
    {
        IMessageIdentifierParser parser = new MessageIdentifierParser();
        INamespaceService service = new NamespaceService();
        int versionNumber = 0;
        while (versionNumber <= 99)
        {
            string version = versionNumber.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
            string identifierText = area + ".001.001." + version;

            MessageIdentifierParseResult identifierResult = parser.ParseIdentifier(identifierText);
            Assert.That(identifierResult.Success, Is.True, identifierText);
            Assert.That(identifierResult.Identifier?.Version, Is.EqualTo(version), identifierText);

            NamespaceServiceResult namespaceResult = service.BuildNamespaceFromIdentifier(identifierResult.Identifier);
            Assert.That(namespaceResult.Success, Is.True, identifierText);
            Assert.That(namespaceResult.Descriptor?.Kind, Is.EqualTo(expectedKind), identifierText);
            Assert.That(namespaceResult.Descriptor?.MessageIdentifier?.Version, Is.EqualTo(version), identifierText);
            Assert.That(namespaceResult.Descriptor?.Uri, Is.EqualTo(Iso20022Namespaces.SchemaUrnPrefix + identifierText), identifierText);
            versionNumber++;
        }
    }

    [TestCase("000", "000")]
    [TestCase("001", "001")]
    [TestCase("009", "010")]
    [TestCase("010", "099")]
    [TestCase("099", "100")]
    [TestCase("100", "999")]
    [TestCase("999", "000")]
    public void FunctionalityAndVariantLeadingZeroesArePreserved(string functionality, string variant)
    {
        IMessageIdentifierParser parser = new MessageIdentifierParser();
        INamespaceService service = new NamespaceService();
        string identifierText = "pacs." + functionality + "." + variant + ".01";

        MessageIdentifierParseResult identifierResult = parser.ParseIdentifier(identifierText);
        NamespaceServiceResult namespaceResult = service.BuildNamespaceFromIdentifier(identifierResult.Identifier);

        Assert.Multiple(() =>
        {
            Assert.That(identifierResult.Success, Is.True);
            Assert.That(identifierResult.Identifier?.MessageFunctionality, Is.EqualTo(functionality));
            Assert.That(identifierResult.Identifier?.Variant, Is.EqualTo(variant));
            Assert.That(namespaceResult.Success, Is.True);
            Assert.That(namespaceResult.Descriptor?.MessageIdentifier?.Value, Is.EqualTo(identifierText));
        });
    }

    [TestCase(" urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10", NamespaceRepairKind.TrimXmlWhitespace)]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10/", NamespaceRepairKind.RemoveTrailingSlash)]
    [TestCase("URN:ISO:STD:ISO:20022:TECH:XSD:pacs.008.001.10", NamespaceRepairKind.NormalizeTechnicalPrefixCase)]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:PACS.008.001.10", NamespaceRepairKind.NormalizeBusinessAreaCase)]
    public void EachSupportedNamespaceAliasRequiresLenientModeAndRecordsItsRepair(string alias, NamespaceRepairKind expectedRepair)
    {
        INamespaceService service = new NamespaceService();

        NamespaceServiceResult strictResult = service.ParseNamespace(alias, NamespaceMode.Strict);
        NamespaceServiceResult lenientResult = service.ParseNamespace(alias, NamespaceMode.Lenient);

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.Success, Is.False);
            Assert.That(strictResult.Repairs, Is.Empty);
            Assert.That(lenientResult.Success, Is.True);
            Assert.That(lenientResult.Descriptor?.Uri, Is.EqualTo("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10"));
            Assert.That(lenientResult.Descriptor?.Mode, Is.EqualTo(NamespaceMode.Lenient));
            Assert.That(lenientResult.Repairs, Has.Count.EqualTo(1));
            Assert.That(lenientResult.Repairs[0].Kind, Is.EqualTo(expectedRepair));
            Assert.That(lenientResult.Repairs[0].OriginalValue, Is.EqualTo(alias));
            Assert.That(lenientResult.Repairs[0].NormalizedValue, Is.EqualTo(lenientResult.Descriptor?.Uri));
        });
    }

    [TestCase("PACS.008.001.10", "pacs.008.001.10", NamespaceKind.Message)]
    [TestCase("HEAD.001.001.04", "head.001.001.04", NamespaceKind.Header)]
    [TestCase("NVLP.001.001.02", "nvlp.001.001.02", NamespaceKind.Envelope)]
    [TestCase("NVLP.002.001.02", "nvlp.002.001.02", NamespaceKind.Envelope)]
    [TestCase("SUPL.001.001.01", "supl.001.001.01", NamespaceKind.Supplementary)]
    public void BusinessAreaAliasesNormalizeAcrossEveryNamespaceFamily(string aliasIdentifier, string canonicalIdentifier, NamespaceKind expectedKind)
    {
        INamespaceService service = new NamespaceService();
        string alias = Iso20022Namespaces.SchemaUrnPrefix + aliasIdentifier;

        NamespaceServiceResult result = service.ParseNamespace(alias, NamespaceMode.Lenient);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Descriptor?.Uri, Is.EqualTo(Iso20022Namespaces.SchemaUrnPrefix + canonicalIdentifier));
            Assert.That(result.Descriptor?.Kind, Is.EqualTo(expectedKind));
            Assert.That(result.Descriptor?.MessageIdentifier?.Value, Is.EqualTo(canonicalIdentifier));
            Assert.That(result.Repairs, Has.Count.EqualTo(1));
            Assert.That(result.Repairs[0].Kind, Is.EqualTo(NamespaceRepairKind.NormalizeBusinessAreaCase));
        });
    }

    [Test]
    public void CombinedNamespaceAliasesRecordRepairsInTransformationOrder()
    {
        INamespaceService service = new NamespaceService();
        string alias = "\tURN:ISO:STD:ISO:20022:TECH:XSD:NVLP.002.001.02/\r\n";

        NamespaceServiceResult result = service.ParseNamespace(alias, NamespaceMode.Lenient);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Repairs, Has.Count.EqualTo(4));
        Assert.Multiple(() =>
        {
            Assert.That(result.Descriptor?.Uri, Is.EqualTo("urn:iso:std:iso:20022:tech:xsd:nvlp.002.001.02"));
            Assert.That(result.Descriptor?.Kind, Is.EqualTo(NamespaceKind.Envelope));
            Assert.That(result.Repairs[0].Kind, Is.EqualTo(NamespaceRepairKind.TrimXmlWhitespace));
            Assert.That(result.Repairs[1].Kind, Is.EqualTo(NamespaceRepairKind.RemoveTrailingSlash));
            Assert.That(result.Repairs[2].Kind, Is.EqualTo(NamespaceRepairKind.NormalizeTechnicalPrefixCase));
            Assert.That(result.Repairs[3].Kind, Is.EqualTo(NamespaceRepairKind.NormalizeBusinessAreaCase));
            Assert.That(result.Repairs[0].OriginalValue, Is.EqualTo(alias));
            Assert.That(result.Repairs[3].NormalizedValue, Is.EqualTo(result.Descriptor?.Uri));
        });
    }

    [TestCase("camt.008.001.10")]
    [TestCase("pacs.009.001.10")]
    [TestCase("pacs.008.002.10")]
    [TestCase("pacs.008.001.09")]
    [TestCase("head.008.001.10")]
    [TestCase("pacs.008.001.10/")]
    public void NamespaceBuilderRejectsEveryCompleteIdentifierConflict(string conflictingValue)
    {
        INamespaceService service = new NamespaceService();
        MessageIdentifier identifier = CreatePacsIdentifier();
        identifier.Value = conflictingValue;

        NamespaceServiceResult result = service.BuildNamespaceFromIdentifier(identifier);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Descriptor, Is.Null);
            Assert.That(result.Repairs, Is.Empty);
            Assert.That(result.ErrorCode, Is.EqualTo("namespace.identifier_conflict"));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        });
    }

    [TestCase("urn:iso:std:iso:20022:tech:xsd:pacs.8.001.10")]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:pacs.008.1.10")]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.1")]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:head.001.001.001")]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:nvlp.001.001.-1")]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:supl.001.001.0A")]
    public void NamespaceParsingRejectsMalformedComponentsInEveryFamily(string namespaceUri)
    {
        INamespaceService service = new NamespaceService();

        NamespaceServiceResult strictResult = service.ParseNamespace(namespaceUri, NamespaceMode.Strict);
        NamespaceServiceResult lenientResult = service.ParseNamespace(namespaceUri, NamespaceMode.Lenient);

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.Success, Is.False);
            Assert.That(strictResult.ErrorCode, Is.EqualTo("namespace.message_identifier"));
            Assert.That(lenientResult.Success, Is.False);
            Assert.That(lenientResult.ErrorCode, Is.EqualTo("namespace.message_identifier"));
        });
    }

    [Test]
    public void UnknownNamespacesAreNotTreatedAsAliases()
    {
        INamespaceService service = new NamespaceService();
        string namespaceUri = "HTTP://Example.COM/custom/";

        NamespaceServiceResult result = service.ParseNamespace(namespaceUri, NamespaceMode.Lenient);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Descriptor?.Uri, Is.EqualTo(namespaceUri));
            Assert.That(result.Descriptor?.Kind, Is.EqualTo(NamespaceKind.Unknown));
            Assert.That(result.Descriptor?.MessageIdentifier, Is.Null);
            Assert.That(result.Repairs, Is.Empty);
        });
    }

    internal static MessageIdentifier CreatePacsIdentifier()
    {
        MessageIdentifier identifier = new MessageIdentifier();
        identifier.BusinessArea = "pacs";
        identifier.MessageFunctionality = "008";
        identifier.Variant = "001";
        identifier.Version = "10";
        identifier.Value = "pacs.008.001.10";
        return identifier;
    }
}
