using System.Text;
using iso20022;

namespace iso20022.Tests;

public class OfficialMessageExampleTests
{
    private const string EnvelopeIdentifier = "nvlp.001.001.02";
    private const string HeaderIdentifier = "head.001.001.02";
    private const string PayloadIdentifier = "pacs.008.001.08";
    private const string ReferenceNamespace = "urn:refdata:xsd:BMEv1_Format";

    [Test]
    public void EveryBundledOfficialMessageExampleCompletesTheMessageLifecycle()
    {
        string xml = File.ReadAllText(GetExamplePath(
            "BusinessMessageEnvelopeV02.xml"));
        MessageReadOptions readOptions = new MessageReadOptions();
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult read = reader.ReadAutoText(xml, readOptions);
        Assert.That(read.Success, Is.True, read.ErrorMessage);
        Assert.That(read.Document, Is.Not.Null);
        MessageDocument document = read.Document!;

        MessageIdentifier payloadIdentifier = ParseIdentifier(PayloadIdentifier);
        MessageReadResult exact = reader.ReadExactText(
            xml,
            payloadIdentifier,
            readOptions);
        SchemaCompilationResult validation = ValidateEnvelope(xml);
        ChecksumResult before = SemanticChecksum(document);
        MessageWriteResult written = new XmlMessageWriter().WriteGenericString(
            document,
            new MessageWriteOptions());
        Assert.That(written.Success, Is.True, written.ErrorMessage);
        MessageReadResult reread = reader.ReadAutoText(
            written.Text,
            readOptions);
        Assert.That(reread.Success, Is.True, reread.ErrorMessage);
        Assert.That(reread.Document, Is.Not.Null);
        MessageDocument roundTripped = reread.Document!;
        ChecksumResult after = SemanticChecksum(roundTripped);
        DynamicXmlNode reference = FindRequiredElement(
            roundTripped.Envelope?.GenericContent,
            ReferenceNamespace,
            "Reference");

        Assert.Multiple(() =>
        {
            Assert.That(document.Layout,
                Is.EqualTo(EnvelopeKind.BusinessMessageEnvelope));
            Assert.That(document.Envelope?.Namespace?.MessageIdentifier?.Value,
                Is.EqualTo(EnvelopeIdentifier));
            Assert.That(document.Header?.Identifier?.Value,
                Is.EqualTo(HeaderIdentifier));
            Assert.That(document.Payloads, Has.Count.EqualTo(1));
            Assert.That(document.Payloads[0].Identifier?.Value,
                Is.EqualTo(PayloadIdentifier));
            Assert.That(document.Metadata.MessageIdentifier?.Value,
                Is.EqualTo(PayloadIdentifier));
            Assert.That(document.Metadata.BusinessMessageIdentifier,
                Is.EqualTo("1234567890"));
            Assert.That(document.Metadata.BusinessService,
                Is.EqualTo("swift.cbprplus.02"));
            Assert.That(exact.Success, Is.True, exact.ErrorMessage);
            Assert.That(validation.Success, Is.True,
                validation.ErrorMessage + " " +
                string.Join(" | ", validation.DiagnosticMessages));
            Assert.That(validation.Found, Is.True);
            Assert.That(validation.DiagnosticCodes,
                Has.None.EqualTo("schema.validation.error"));
            Assert.That(before.Success, Is.True, before.ErrorMessage);
            Assert.That(after.Success, Is.True, after.ErrorMessage);
            Assert.That(after.Checksum.Value,
                Is.EqualTo(before.Checksum.Value));
            Assert.That(CollectText(reference, "UUID"),
                Is.EqualTo("3606b3e6-d267-4c5c-a1c0-0f68c7f1bb45"));
            Assert.That(written.Text,
                Does.Contain("urn:refdata:xsd:BMEv1_Format"));
        });
    }

    [TestCase(
        "SupplementaryDataGlobal.xml",
        "seev.031.001.03",
        "This extends the second occurrence",
        1)]
    [TestCase(
        "SupplementaryDataLegacyExtension.xml",
        "seev.039.001.01",
        "part 2 of the extension",
        2)]
    [TestCase(
        "SupplementaryDataStructured.xml",
        "seev.031.001.03",
        "<ext:MyNewElement>abc</ext:MyNewElement>",
        1)]
    public void EveryOfficialSupplementaryExampleCompletesTheMessageLifecycle(
        string fileName,
        string identifierValue,
        string preservedValue,
        int extensionCount)
    {
        string xml = File.ReadAllText(GetExamplePath(fileName));
        MessageIdentifier identifier = ParseIdentifier(identifierValue);
        IXmlMessageReader reader = new XmlMessageReader();
        MessageReadResult read = reader.ReadAutoText(
            xml,
            new MessageReadOptions());
        MessageReadResult exact = reader.ReadExactText(
            xml,
            identifier,
            new MessageReadOptions());
        Assert.That(read.Success, Is.True, read.ErrorMessage);
        Assert.That(read.Document, Is.Not.Null);
        MessageDocument document = read.Document!;
        SupplementaryValidationResult validation =
            ValidateSupplementary(xml);
        ChecksumResult before = SemanticChecksum(document);
        MessageWriteResult written = new XmlMessageWriter().WriteGenericString(
            document,
            new MessageWriteOptions());
        Assert.That(written.Success, Is.True, written.ErrorMessage);
        MessageReadResult reread = reader.ReadAutoText(
            written.Text,
            new MessageReadOptions());
        Assert.That(reread.Success, Is.True, reread.ErrorMessage);
        Assert.That(reread.Document, Is.Not.Null);
        ChecksumResult after = SemanticChecksum(reread.Document!);
        SupplementaryValidationResult roundTripValidation =
            ValidateSupplementary(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(document.Layout,
                Is.EqualTo(EnvelopeKind.StandaloneDocument));
            Assert.That(document.Metadata.MessageIdentifier?.Value,
                Is.EqualTo(identifierValue));
            Assert.That(document.Payloads.Single().Identifier?.Value,
                Is.EqualTo(identifierValue));
            Assert.That(exact.Success, Is.True, exact.ErrorMessage);
            Assert.That(validation.Success, Is.True, validation.ErrorMessage);
            Assert.That(validation.Valid, Is.True);
            Assert.That(validation.Items, Has.Count.EqualTo(extensionCount));
            Assert.That(roundTripValidation.Success,
                Is.True,
                roundTripValidation.ErrorMessage);
            Assert.That(roundTripValidation.Valid, Is.True);
            Assert.That(roundTripValidation.Items,
                Has.Count.EqualTo(extensionCount));
            Assert.That(roundTripValidation.Items.Select(
                    item => item.Payload?.Xml ?? string.Empty),
                Has.Some.Contains(preservedValue));
            Assert.That(before.Success, Is.True, before.ErrorMessage);
            Assert.That(after.Success, Is.True, after.ErrorMessage);
            Assert.That(after.Checksum.Value,
                Is.EqualTo(before.Checksum.Value));
        });
    }

    private static SchemaCompilationResult ValidateEnvelope(string xml)
    {
        string specificationRoot = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022"));
        string archivePath = Path.Combine(
            specificationRoot,
            "ISO20022_Business_Message_Envelope_V02.zip");
        string namespaceUri = Iso20022Namespaces.SchemaUrnPrefix +
            EnvelopeIdentifier;
        MessageIdentifier identifier = ParseIdentifier(EnvelopeIdentifier);
        SchemaArtifact artifact = new SchemaArtifact
        {
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Schema,
                    Edition = "2025",
                    Identifier = EnvelopeIdentifier,
                    Version = identifier.Version
                },
                OriginUri = new Uri(archivePath).AbsoluteUri,
                IsApproved = true
            },
            SourceName = EnvelopeIdentifier + ".xsd",
            BaseUri = new Uri(Path.Combine(
                specificationRoot,
                EnvelopeIdentifier + ".xsd")).AbsoluteUri,
            NamespaceUri = namespaceUri,
            MessageIdentifier = identifier,
            Version = identifier.Version,
            Profile = "iso",
            Precedence = 100,
            IsEntryPoint = true
        };
        SchemaRegistryOptions options = new SchemaRegistryOptions
        {
            LocalRootPath = specificationRoot
        };
        ISchemaRegistry registry = new SchemaRegistry();
        using (Stream input = File.OpenRead(archivePath))
        {
            SchemaCompilationResult registered =
                registry.ImportRegisterSchemaArchive(input, artifact, options);
            Assert.That(registered.Success, Is.True, registered.ErrorMessage);
            Assert.That(registered.Registered, Is.True);
        }
        SchemaArtifact selector = new SchemaArtifact
        {
            NamespaceUri = namespaceUri,
            MessageIdentifier = identifier,
            Version = identifier.Version,
            Profile = "iso"
        };
        return new XsdValidator(registry).ValidateXmlStream(
            new MemoryStream(Encoding.UTF8.GetBytes(xml)),
            selector,
            options);
    }

    private static ChecksumResult SemanticChecksum(MessageDocument document)
    {
        return new MessageChecksum().CalculateChecksum(new ChecksumRequest
        {
            Document = document,
            EqualityMode = MessageEqualityMode.SemanticModel,
            Scope = ChecksumScope.WholeMessage
        });
    }

    private static MessageIdentifier ParseIdentifier(string value)
    {
        MessageIdentifierParseResult parsed =
            new MessageIdentifierParser().ParseIdentifier(value);
        return parsed.Identifier ??
            throw new AssertionException(parsed.ErrorMessage);
    }

    private static SupplementaryValidationResult ValidateSupplementary(
        string xml)
    {
        return new SupplementaryDataValidator().ValidateSupplementaryData(
            new SupplementaryValidationRequest
            {
                Xml = Encoding.UTF8.GetBytes(xml)
            });
    }

    private static string GetExamplePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../tests/Fixtures/OfficialExamples/" + fileName));
    }

    private static DynamicXmlNode FindRequiredElement(
        DynamicXmlNode? node,
        string namespaceUri,
        string localName)
    {
        if (node != null &&
            node.Kind == XmlNodeKind.Element &&
            node.Name.NamespaceUri == namespaceUri &&
            node.Name.LocalName == localName)
        {
            return node;
        }
        if (node != null)
        {
            foreach (DynamicXmlNode child in node.Children)
            {
                try
                {
                    return FindRequiredElement(child, namespaceUri, localName);
                }
                catch (AssertionException)
                {
                }
            }
        }
        throw new AssertionException(
            "Expected element {" + namespaceUri + "}" + localName +
            " was not preserved.");
    }

    private static string CollectText(DynamicXmlNode node, string localName)
    {
        DynamicXmlNode target = FindRequiredElement(
            node,
            ReferenceNamespace,
            localName);
        StringBuilder text = new StringBuilder();
        foreach (DynamicXmlNode child in target.Children)
        {
            if (child.Kind == XmlNodeKind.Text ||
                child.Kind == XmlNodeKind.CData)
            {
                text.Append(child.Value);
            }
        }
        return text.ToString();
    }
}
