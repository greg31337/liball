using System.Text;
using iso20022;

namespace iso20022.Tests;

public class ForwardCompatibilityTests
{
    [Test]
    public void FutureRepositoryFieldsRemainAttachedToTheirKnownOwner()
    {
        string path = GetFixturePath("FutureRepositoryFields.iso20022");
        IRepositoryImporter importer = new RepositoryImporter();
        RepositoryImportResult result;
        using (Stream input = File.OpenRead(path))
        {
            result = importer.ImportRawStream(
                input,
                new RepositoryImportOptions
                {
                    SourceName = Path.GetFileName(path),
                    Coordinate = new ArtifactCoordinate
                    {
                        Kind = ArtifactKind.Repository,
                        Edition = "future-fixture",
                        Identifier = "future-repository",
                        Version = "1"
                    }
                });
        }

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Snapshot, Is.Not.Null);
        RepositorySnapshot snapshot = result.Snapshot!;
        DataTypeDefinition known = snapshot.DataTypes.Single(
            item => item.Identifier == "_knownText");
        RepositoryUnknownData facet = known.UnknownContent.Single(
            item => item.Name.LocalName == "newFacet");
        RepositoryUnknownData extension = known.UnknownContent.Single(
            item => item.Name.LocalName == "extension");
        RepositoryUnknownData detail = extension.Children.Single(
            item => item.Name.LocalName == "detail");

        Assert.Multiple(() =>
        {
            Assert.That(known.Name, Is.EqualTo("KnownText"));
            Assert.That(known.Facets.Single(
                    item => item.Name == "minLength").IntegerValue,
                Is.EqualTo(1));
            Assert.That(facet.IsAttribute, Is.True);
            Assert.That(facet.Name.NamespaceUri,
                Is.EqualTo("urn:example:iso20022:future:repository"));
            Assert.That(facet.Value, Is.EqualTo("future-value"));
            Assert.That(extension.Identifier,
                Is.EqualTo("_futureExtension"));
            Assert.That(extension.TypeName,
                Is.EqualTo("future:DataTypeExtension"));
            Assert.That(extension.Value, Is.EqualTo("PrefixSuffix"));
            Assert.That(extension.RawContent?.RootName?.LocalName,
                Is.EqualTo("extension"));
            Assert.That(detail.Value, Is.EqualTo("Payload"));
            Assert.That(detail.Children.Single(
                    item => item.Name.LocalName == "level").Value,
                Is.EqualTo("2"));
            Assert.That(snapshot.UnknownContent,
                Has.Some.SameAs(facet).And.Some.SameAs(extension));
        });
    }

    [TestCase("FutureNamespaceMessage.xml", "futr.001.001.01")]
    [TestCase("FutureMessageVersion.xml", "pacs.008.001.99")]
    public void FutureNamespacesAndVersionsUseLosslessGenericProcessing(
        string fileName,
        string identifierValue)
    {
        string xml = File.ReadAllText(GetFixturePath(fileName));
        MessageIdentifier identifier = ParseIdentifier(identifierValue);
        IXmlMessageReader reader = new XmlMessageReader();
        MessageReadOptions readOptions = new MessageReadOptions
        {
            UnknownContentMode = UnknownContentMode.Preserve
        };

        MessageReadResult read = reader.ReadAutoText(xml, readOptions);
        MessageReadResult exact = reader.ReadExactText(
            xml,
            identifier,
            readOptions);
        Assert.That(read.Success, Is.True, read.ErrorMessage);
        Assert.That(read.Document, Is.Not.Null);
        MessageDocument document = read.Document!;
        ChecksumResult before = SemanticChecksum(document);
        MessageWriteResult written = new XmlMessageWriter().WriteGenericString(
            document,
            new MessageWriteOptions
            {
                UnknownContentMode = UnknownContentMode.Preserve
            });
        Assert.That(written.Success, Is.True, written.ErrorMessage);
        MessageReadResult reread = reader.ReadAutoText(
            written.Text,
            readOptions);
        Assert.That(reread.Success, Is.True, reread.ErrorMessage);
        Assert.That(reread.Document, Is.Not.Null);
        ChecksumResult after = SemanticChecksum(reread.Document!);

        Assert.Multiple(() =>
        {
            Assert.That(document.Metadata.MessageIdentifier?.Value,
                Is.EqualTo(identifierValue));
            Assert.That(document.Payloads, Has.Count.EqualTo(1));
            Assert.That(document.Payloads[0].Identifier?.Value,
                Is.EqualTo(identifierValue));
            Assert.That(document.Payloads[0].TypedContent, Is.Null);
            Assert.That(document.Payloads[0].GenericContent, Is.Not.Null);
            Assert.That(exact.Success, Is.True, exact.ErrorMessage);
            Assert.That(before.Success, Is.True, before.ErrorMessage);
            Assert.That(after.Success, Is.True, after.ErrorMessage);
            Assert.That(after.Checksum.Value,
                Is.EqualTo(before.Checksum.Value));
            Assert.That(written.Text,
                Does.Contain("urn:example:iso20022:future:"));
            Assert.That(written.Text, Does.Contain("future:mode=\"preserve\""));
        });
    }

    [Test]
    public void UnknownExternalCodeIsAStableNotFoundResultForPinnedEdition()
    {
        string path = GetFixturePath("FutureExternalCodes.json");
        ExternalCodeImportRequest importRequest =
            new ExternalCodeImportRequest
            {
                SourceName = Path.GetFileName(path),
                Edition = "future-fixture",
                Artifact = new ArtifactManifest
                {
                    Coordinate = new ArtifactCoordinate
                    {
                        Kind = ArtifactKind.ExternalCodeSet,
                        Edition = "future-fixture",
                        Identifier = "future-external-codes",
                        Version = "1"
                    },
                    OriginUri = new Uri(path).AbsoluteUri,
                    IsApproved = true
                }
            };
        ExternalCodeImportResult imported;
        using (Stream input = File.OpenRead(path))
        {
            imported = new ExternalCodeSetImporter().ImportJsonStream(
                input,
                importRequest);
        }
        Assert.That(imported.Success, Is.True, imported.ErrorMessage);
        Assert.That(imported.Catalog, Is.Not.Null);
        ICodeSetProvider provider = new CodeSetProvider(
            new List<RepositorySnapshot>(),
            new List<ExternalCodeCatalog> { imported.Catalog! });

        CodeLookupResult known = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalForwardStatus1Code",
            Value = "KNWN",
            Edition = "future-fixture"
        });
        CodeLookupResult unknown = provider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalForwardStatus1Code",
            Value = "FUTR",
            Edition = "future-fixture"
        });

        Assert.Multiple(() =>
        {
            Assert.That(known.Success, Is.True, known.ErrorMessage);
            Assert.That(known.Found, Is.True);
            Assert.That(known.ExternalCodeValue?.Value, Is.EqualTo("KNWN"));
            Assert.That(unknown.Success, Is.True, unknown.ErrorMessage);
            Assert.That(unknown.Found, Is.False);
            Assert.That(unknown.ErrorCode, Is.Empty);
            Assert.That(unknown.Edition, Is.EqualTo("future-fixture"));
            Assert.That(unknown.Artifact.Coordinate.Identifier,
                Is.EqualTo("future-external-codes"));
        });
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

    internal static string GetFixturePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../tests/Fixtures/ForwardCompatibility/" + fileName));
    }
}
