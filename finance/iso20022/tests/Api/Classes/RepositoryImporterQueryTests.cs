using System.IO.Compression;
using System.Text;
using System.Threading;
using iso20022;

namespace iso20022.Tests;

public class RepositoryImporterQueryTests
{
    [Test]
    public void ImporterReadsRawAndZipStreamsWithProvenanceAndIntegrityCounts()
    {
        byte[] raw = Encoding.UTF8.GetBytes(CreateRepositoryFixture());
        byte[] zip = CreateZip(raw);
        IRepositoryImporter importer = new RepositoryImporter();
        RepositoryImportOptions rawOptions = CreateOptions("stream-raw");
        RepositoryImportOptions zipOptions = CreateOptions("stream-zip");
        using MemoryStream rawStream = new MemoryStream(raw);
        using MemoryStream zipStream = new MemoryStream(zip);

        RepositoryImportResult rawResult = importer.ImportRawStream(rawStream, rawOptions);
        RepositoryImportResult zipResult = importer.ImportZipStream(zipStream, zipOptions);

        Assert.Multiple(() =>
        {
            Assert.That(rawResult.Success, Is.True);
            Assert.That(zipResult.Success, Is.True);
            Assert.That(rawResult.Snapshot?.Identifier, Is.EqualTo("_repository"));
            Assert.That(rawResult.Snapshot?.Edition, Is.EqualTo("stream-raw"));
            Assert.That(rawResult.Snapshot?.MessageDefinitions.Select(
                message => message.MessageIdentifier.Value), Is.EqualTo(new[]
            {
                "pacs.008.001.09",
                "pacs.008.001.10"
            }));
            Assert.That(rawResult.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.Xml));
            Assert.That(zipResult.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.Zip));
            Assert.That(zipResult.Provenance.SourceEntryName,
                Is.EqualTo("fixture.iso20022"));
            Assert.That(zipResult.Provenance.AdapterIdentifier,
                Is.EqualTo("iso20022.ecore.2013"));
            Assert.That(rawResult.SourceEntityCount, Is.GreaterThan(0));
            Assert.That(rawResult.SourceReferenceCount, Is.GreaterThan(0));
            Assert.That(rawResult.SourceDuplicateIdentifierCount, Is.EqualTo(0));
            Assert.That(rawResult.SourceUnresolvedReferenceCount, Is.EqualTo(0));
            Assert.That(rawResult.NormalizedReferenceCount,
                Is.EqualTo(rawResult.SourceReferenceCount));
            Assert.That(rawResult.IndexedEntityCount, Is.GreaterThan(6));
            Assert.That(rawResult.DiagnosticCodes, Is.Empty);
            Assert.That(zipResult.DiagnosticCodes, Is.Empty);
        });
    }

    [Test]
    public void ImporterReadsRawAndZipFilesAndRejectsMismatchedForms()
    {
        byte[] raw = Encoding.UTF8.GetBytes(CreateRepositoryFixture());
        byte[] zip = CreateZip(raw);
        string directory = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "repository-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string rawPath = Path.Combine(directory, "fixture.iso20022");
        string zipPath = Path.Combine(directory, "fixture.zip");
        File.WriteAllBytes(rawPath, raw);
        File.WriteAllBytes(zipPath, zip);
        IRepositoryImporter importer = new RepositoryImporter();

        try
        {
            RepositoryImportResult rawResult = importer.ImportRawFile(
                rawPath,
                CreateOptions("file-raw"));
            RepositoryImportResult zipResult = importer.ImportZipFile(
                zipPath,
                CreateOptions("file-zip"));
            RepositoryImportResult rawMismatch = importer.ImportRawFile(
                zipPath,
                CreateOptions("wrong-raw"));
            RepositoryImportResult zipMismatch = importer.ImportZipFile(
                rawPath,
                CreateOptions("wrong-zip"));

            Assert.Multiple(() =>
            {
                Assert.That(rawResult.Success, Is.True);
                Assert.That(zipResult.Success, Is.True);
                Assert.That(rawResult.Snapshot?.BusinessComponents.Single().Name,
                    Is.EqualTo("Account"));
                Assert.That(zipResult.Snapshot?.MessageSets.Single().Identifier,
                    Is.EqualTo("_set"));
                Assert.That(rawMismatch.Success, Is.False);
                Assert.That(rawMismatch.ErrorCode,
                    Is.EqualTo("repository.import.raw_required"));
                Assert.That(zipMismatch.Success, Is.False);
                Assert.That(zipMismatch.ErrorCode,
                    Is.EqualTo("repository.import.zip_required"));
                Assert.That(importer.ImportRawStream(null, CreateOptions("missing")).ErrorCode,
                    Is.EqualTo("repository.import.input_missing"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ImporterObservesCancellationBeforeReadingInput()
    {
        byte[] raw = Encoding.UTF8.GetBytes(CreateRepositoryFixture());
        using MemoryStream stream = new MemoryStream(raw);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RepositoryImportOptions options = CreateOptions("cancelled");
        options.CancellationToken = cancellation.Token;

        Assert.Throws<OperationCanceledException>(() =>
            new RepositoryImporter().ImportRawStream(stream, options));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void QueryCombinesEveryIndexedKeyAndReturnsDetachedResultMetadata()
    {
        RepositorySnapshot snapshot = ImportSnapshot();
        IRepositoryQuery query = new RepositoryQuery(snapshot);
        RepositoryQueryRequest request = new RepositoryQueryRequest
        {
            Identifier = "_message10",
            Name = "CurrentCreditTransfer",
            HasKind = true,
            Kind = RepositoryEntityKind.MessageDefinition,
            HasRegistrationStatus = true,
            RegistrationStatus = RegistrationStatus.Registered,
            BusinessAreaCode = "pacs",
            MessageSetIdentifier = "_set",
            MessageIdentifier = "pacs.008.001.10",
            NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10",
            MessageFunctionality = "008",
            Variant = "001",
            ArtifactEdition = "query-edition",
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.Exact,
                BaselineIdentifier = Identifier("10")
            }
        };

        RepositoryQueryResult result = query.QueryRepository(request);
        snapshot.MessageDefinitions[1].Name = "MutatedAfterIndexing";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalMatchCount, Is.EqualTo(1));
            Assert.That(result.Truncated, Is.False);
            Assert.That(result.Items.Single().Identifier, Is.EqualTo("_message10"));
            Assert.That(result.Items.Single().Name, Is.EqualTo("CurrentCreditTransfer"));
            Assert.That(result.Items.Single().Version, Is.EqualTo("10"));
            Assert.That(result.Items.Single().Value,
                Is.SameAs(snapshot.MessageDefinitions[1]));
            Assert.That(query.QueryRepository(new RepositoryQueryRequest
            {
                Name = "MutatedAfterIndexing"
            }).Items, Is.Empty);
            Assert.That(query.QueryRepository(new RepositoryQueryRequest
            {
                ArtifactEdition = "another-edition"
            }).Items, Is.Empty);
        });
    }

    [Test]
    public void QueryAppliesCurrentCompatibleRangeAndResultLimitDeterministically()
    {
        IRepositoryQuery query = new RepositoryQuery(ImportSnapshot());
        RepositoryQueryResult current = query.QueryRepository(new RepositoryQueryRequest
        {
            HasKind = true,
            Kind = RepositoryEntityKind.MessageDefinition,
            MessageFunctionality = "008",
            VersionSelector = new VersionSelector { Kind = VersionSelectionKind.Current }
        });
        RepositoryQueryResult compatible = query.QueryRepository(new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.LatestCompatible,
                BaselineIdentifier = Identifier("09")
            }
        });
        RepositoryQueryResult range = query.QueryRepository(new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.Range,
                MinimumVersion = "09",
                MaximumVersion = "10"
            }
        });
        RepositoryQueryResult set = query.QueryRepository(new RepositoryQueryRequest
        {
            HasKind = true,
            Kind = RepositoryEntityKind.MessageDefinition,
            MessageSetIdentifier = "_set"
        });
        RepositoryQueryResult limited = query.QueryRepository(new RepositoryQueryRequest
        {
            MaximumResultCount = 2
        });
        RepositoryQueryResult invalid = query.QueryRepository(new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.Range,
                MinimumVersion = "10",
                MaximumVersion = "09"
            }
        });
        RepositoryQueryResult unknownPolicy = query.QueryRepository(new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = (VersionSelectionKind)99
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(current.Items.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_message10" }));
            Assert.That(compatible.Items.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_message10" }));
            Assert.That(range.Items.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_message09", "_message10" }));
            Assert.That(set.Items.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_message09", "_message10" }));
            Assert.That(limited.TotalMatchCount, Is.GreaterThan(2));
            Assert.That(limited.Truncated, Is.True);
            Assert.That(limited.Items.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_repository", "_dictionary" }));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.ErrorCode,
                Is.EqualTo("repository.query.version_range_invalid"));
            Assert.That(unknownPolicy.ErrorCode,
                Is.EqualTo("repository.query.version_policy_invalid"));
            Assert.That(query.QueryRepository(null).ErrorCode,
                Is.EqualTo("repository.query.request_missing"));
        });
    }

    private static RepositorySnapshot ImportSnapshot()
    {
        byte[] raw = Encoding.UTF8.GetBytes(CreateRepositoryFixture());
        using MemoryStream stream = new MemoryStream(raw);
        RepositoryImportResult result = new RepositoryImporter().ImportRawStream(
            stream,
            CreateOptions("query-edition"));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        return result.Snapshot!;
    }

    private static RepositoryImportOptions CreateOptions(string edition)
    {
        return new RepositoryImportOptions
        {
            SourceName = "fixture.iso20022",
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.Repository,
                Edition = edition,
                Identifier = "fixture",
                Version = "1"
            },
            OriginUri = "https://example.test/repository",
            IsApproved = true
        };
    }

    private static MessageIdentifier Identifier(string version)
    {
        return new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = version,
            Value = "pacs.008.001." + version
        };
    }

    private static byte[] CreateZip(byte[] raw)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            ZipArchiveEntry entry = archive.CreateEntry("fixture.iso20022");
            using Stream destination = entry.Open();
            destination.Write(raw, 0, raw.Length);
        }

        return output.ToArray();
    }

    private static string CreateRepositoryFixture()
    {
        return """
            <?xml version='1.0'?>
            <iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore'
                xmlns:xmi='http://www.omg.org/XMI'
                xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                xmi:version='2.0' xmi:id='_repository'>
              <dataDictionary xmi:id='_dictionary'>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent'
                    xmi:id='_account' name='Account' registrationStatus='Registered'/>
              </dataDictionary>
              <businessProcessCatalogue xmi:id='_catalogue'>
                <topLevelCatalogueEntry xsi:type='iso:BusinessArea' xmi:id='_area'
                    name='Payments' code='pacs' registrationStatus='Registered'>
                  <messageDefinition xmi:id='_message09' name='PreviousCreditTransfer'
                      registrationStatus='Obsolete' messageSet='_set'>
                    <messageDefinitionIdentifier businessArea='pacs'
                        messageFunctionality='008' flavour='001' version='09'/>
                  </messageDefinition>
                  <messageDefinition xmi:id='_message10' name='CurrentCreditTransfer'
                      registrationStatus='Registered' messageSet='_set'>
                    <messageDefinitionIdentifier businessArea='pacs'
                        messageFunctionality='008' flavour='001' version='10'/>
                  </messageDefinition>
                </topLevelCatalogueEntry>
                <topLevelCatalogueEntry xsi:type='iso:MessageSet' xmi:id='_set'
                    name='PaymentsMessageSet' registrationStatus='Registered'
                    messageDefinition='_message09 _message10'/>
              </businessProcessCatalogue>
            </iso:Repository>
            """;
    }
}
