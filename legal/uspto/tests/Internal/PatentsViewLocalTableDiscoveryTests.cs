using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentsViewLocalTableDiscoveryTests
{
    [Test]
    public void ManifestExtractionAndHeaderEvidenceMergeIntoOneInspectedTable()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string tableDirectory = Path.Combine(directory, "tables");
            Directory.CreateDirectory(tableDirectory);
            string path = Path.Combine(tableDirectory, "g_patent.tsv");
            File.WriteAllText(path, "patent_id\tpatent_title\tfuture_metric\n0012345\tWidget\tnew-value\n", new UTF8Encoding(false));

            PatentsViewLocalManifestTable manifest = new PatentsViewLocalManifestTable
            {
                Family = DatasetFamily.PatentsViewGrant,
                TableIdentifier = "g_patent",
                RelativePath = "tables/g_patent.tsv",
                Delimiter = '\t',
                Headers = new[] { "patent_id", "patent_title", "future_metric" }
            };
            ExtractionEntryResult extracted = new ExtractionEntryResult
            {
                Entry = new ArchiveEntry { Name = "tables/g_patent.tsv" },
                DestinationPath = path,
                IsExtracted = true
            };
            PatentsViewLocalHeaderEvidence header = new PatentsViewLocalHeaderEvidence
            {
                Family = DatasetFamily.PatentsViewGrant,
                FilePath = path,
                Headers = new[] { "patent_id", "patent_title", "future_metric" }
            };
            PatentsViewLocalTableDiscoveryRequest request = CreateRequest(directory);
            request.ManifestTables = new[] { manifest };
            request.ExtractionResults = new[] { new ExtractionResult { Entries = new[] { extracted }, IsComplete = true } };
            request.HeaderEvidence = new[] { header };

            PatentsViewLocalTableDiscoveryResult result = PatentsViewLocalTableDiscovery.Discover(request);
            PatentsViewLocalTableInfo table = result.Tables.Single();

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.ManifestCandidateCount, Is.EqualTo(1));
                Assert.That(result.ExtractionCandidateCount, Is.EqualTo(1));
                Assert.That(result.HeaderCandidateCount, Is.EqualTo(1));
                Assert.That(table.TableIdentifier, Is.EqualTo("g_patent"));
                Assert.That(table.Family, Is.EqualTo(DatasetFamily.PatentsViewGrant));
                Assert.That(table.RelativePath, Is.EqualTo("tables/g_patent.tsv"));
                Assert.That(table.Delimiter, Is.EqualTo('\t'));
                Assert.That(table.FromManifest, Is.True);
                Assert.That(table.FromExtraction, Is.True);
                Assert.That(table.FromHeaderEvidence, Is.True);
                Assert.That(table.FileExists, Is.True);
                Assert.That(table.FileInspected, Is.True);
                Assert.That(table.HasFileSize, Is.True);
                Assert.That(table.FileSize, Is.GreaterThan(0));
                Assert.That(table.HasDeclaredSchema, Is.True);
                Assert.That(table.HasObservedSchema, Is.True);
                Assert.That(table.HasSchemaComparison, Is.True);
                Assert.That(table.SchemaMatchesManifest, Is.True);
                Assert.That(table.Schema.Headers, Is.EqualTo(new[] { "patent_id", "patent_title", "future_metric" }));
                Assert.That(table.Provenance.Source, Is.EqualTo(DataProvenance.LocalMirror));
                Assert.That(table.Provenance.DatasetIdentifier, Is.EqualTo("g_patent"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FileInspectionReportsManifestSchemaDriftAndExposesNewColumns()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string path = Path.Combine(directory, "annualized_inventor.tsv");
            File.WriteAllText(path, "inventor_id\tyear\tpatent_count\tnew_collaboration_score\ninv-1\t2025\t4\t0.75\n", new UTF8Encoding(false));
            PatentsViewLocalTableDiscoveryRequest request = CreateRequest(directory);
            request.DefaultFamily = DatasetFamily.PatentsViewAnnualized;
            request.ManifestTables = new[]
            {
                new PatentsViewLocalManifestTable
                {
                    RelativePath = "annualized_inventor.tsv",
                    Headers = new[] { "inventor_id", "year", "patent_count" }
                }
            };

            PatentsViewLocalTableInfo table = PatentsViewLocalTableDiscovery.Discover(request).Tables.Single();

            Assert.Multiple(() =>
            {
                Assert.That(table.Family, Is.EqualTo(DatasetFamily.PatentsViewAnnualized));
                Assert.That(table.HasSchemaComparison, Is.True);
                Assert.That(table.SchemaMatchesManifest, Is.False);
                Assert.That(table.DeclaredSchema.ColumnCount, Is.EqualTo(3));
                Assert.That(table.ObservedSchema.ColumnCount, Is.EqualTo(4));
                Assert.That(table.Schema.Headers, Does.Contain("new_collaboration_score"));
                Assert.That(table.Schema.Fingerprint, Does.EndWith("newcollaborationscore"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ExtractedFilesInferFamilyTableAndDelimiterFromTheirPaths()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string publicationPath = Path.Combine(directory, "pg_publication.csv");
            string sortedPath = Path.Combine(directory, "portfolio_sorted.tsv");
            File.WriteAllText(publicationPath, "document_number,publication_title,publication_year\nUS20240001234A1,Controller,2024\n", new UTF8Encoding(false));
            File.WriteAllText(sortedPath, "experimental_key\tscore\tfuture_flag\nA\t1.5\tyes\n", new UTF8Encoding(false));

            PatentsViewLocalTableDiscoveryRequest publicationRequest = CreateRequest(directory);
            publicationRequest.ExtractedFiles = new[] { publicationPath };
            PatentsViewLocalTableInfo publication = PatentsViewLocalTableDiscovery.Discover(publicationRequest).Tables.Single();

            PatentsViewLocalTableDiscoveryRequest sortedRequest = CreateRequest(directory);
            sortedRequest.ExtractedFiles = new[] { sortedPath };
            PatentsViewLocalTableInfo sorted = PatentsViewLocalTableDiscovery.Discover(sortedRequest).Tables.Single();

            Assert.Multiple(() =>
            {
                Assert.That(publication.Family, Is.EqualTo(DatasetFamily.PatentsViewPreGrant));
                Assert.That(publication.TableIdentifier, Is.EqualTo("pg_publication"));
                Assert.That(publication.Delimiter, Is.EqualTo(','));
                Assert.That(publication.Schema.Headers, Is.EqualTo(new[] { "document_number", "publication_title", "publication_year" }));
                Assert.That(sorted.Family, Is.EqualTo(DatasetFamily.PatentsViewSorted));
                Assert.That(sorted.TableIdentifier, Is.EqualTo("portfolio_sorted"));
                Assert.That(sorted.Delimiter, Is.EqualTo('\t'));
                Assert.That(sorted.Schema.Headers, Does.Contain("future_flag"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void HeaderOnlyEvidenceDiscoversTablesWithoutLocalFiles()
    {
        PatentsViewLocalTableDiscoveryRequest request = new PatentsViewLocalTableDiscoveryRequest
        {
            HeaderEvidence = new[]
            {
                new PatentsViewLocalHeaderEvidence
                {
                    ProductIdentifier = "PV-ANNUAL",
                    TableIdentifier = "PV-ANNUAL",
                    Headers = new[] { "inventor_id", "year", "patent_count", "future_measure" }
                },
                new PatentsViewLocalHeaderEvidence
                {
                    ProductIdentifier = "PV-SORTED",
                    Headers = new[] { "experimental_key", "future_value" }
                }
            }
        };

        PatentsViewLocalTableDiscoveryResult result = PatentsViewLocalTableDiscovery.Discover(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Tables, Has.Length.EqualTo(2));
            Assert.That(result.HeaderCandidateCount, Is.EqualTo(2));
            Assert.That(result.Tables[0].Family, Is.EqualTo(DatasetFamily.PatentsViewAnnualized));
            Assert.That(result.Tables[0].TableIdentifier, Is.EqualTo("inventor"));
            Assert.That(result.Tables[0].HasObservedSchema, Is.True);
            Assert.That(result.Tables[0].FilePath, Is.Null);
            Assert.That(result.Tables[0].Schema.Headers, Does.Contain("future_measure"));
            Assert.That(result.Tables[1].Family, Is.EqualTo(DatasetFamily.PatentsViewSorted));
            Assert.That(result.Tables[1].TableIdentifier, Is.EqualTo("table"));
            Assert.That(result.Tables[1].Schema.Headers, Does.Contain("future_value"));
        });
    }

    [Test]
    public void MissingAndUnsupportedManifestFilesRemainDiscoverableWithOutcomes()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string unsupportedPath = Path.Combine(directory, "metadata.json");
            File.WriteAllText(unsupportedPath, "{}", new UTF8Encoding(false));
            PatentsViewLocalTableDiscoveryRequest request = CreateRequest(directory);
            request.DefaultFamily = DatasetFamily.PatentsViewSorted;
            request.ManifestTables = new[]
            {
                new PatentsViewLocalManifestTable { RelativePath = "missing.tsv", TableIdentifier = "missing_table" },
                new PatentsViewLocalManifestTable { RelativePath = "metadata.json", TableIdentifier = "metadata", Headers = new[] { "key", "value" } }
            };

            PatentsViewLocalTableDiscoveryResult result = PatentsViewLocalTableDiscovery.Discover(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Tables, Has.Length.EqualTo(2));
                Assert.That(result.MissingFileCount, Is.EqualTo(1));
                Assert.That(result.UnsupportedFileCount, Is.EqualTo(1));
                Assert.That(result.Tables[0].TableIdentifier, Is.EqualTo("missing_table"));
                Assert.That(result.Tables[0].FileExists, Is.False);
                Assert.That(result.Tables[0].InspectionFailureCode, Is.EqualTo("FILE_NOT_FOUND"));
                Assert.That(result.Tables[1].HasDeclaredSchema, Is.True);
                Assert.That(result.Tables[1].InspectionFailureCode, Is.EqualTo("UNSUPPORTED_FILE_TYPE"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void EmptyExtractedTableIsReportedWithoutInventingASchema()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string path = Path.Combine(directory, "annualized_empty.tsv");
            File.WriteAllText(path, string.Empty, new UTF8Encoding(false));
            PatentsViewLocalTableDiscoveryRequest request = CreateRequest(directory);
            request.DefaultFamily = DatasetFamily.PatentsViewAnnualized;
            request.ExtractedFiles = new[] { path };

            PatentsViewLocalTableDiscoveryResult result = PatentsViewLocalTableDiscovery.Discover(request);
            PatentsViewLocalTableInfo table = result.Tables.Single();

            Assert.Multiple(() =>
            {
                Assert.That(table.FileExists, Is.True);
                Assert.That(table.HasFileSize, Is.True);
                Assert.That(table.FileSize, Is.Zero);
                Assert.That(table.FileInspected, Is.False);
                Assert.That(table.HasSchema, Is.False);
                Assert.That(table.InspectionFailureCode, Is.EqualTo("EMPTY_OR_HEADERLESS_FILE"));
                Assert.That(result.IsComplete, Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DiscoveryEnforcesMirrorContainmentSchemaValidityAndTableLimits()
    {
        string directory = CreateTemporaryDirectory();
        string outside = Path.Combine(Path.GetTempPath(), "outside-" + Guid.NewGuid().ToString("N") + ".tsv");
        try
        {
            PatentsViewLocalTableDiscoveryRequest unsafeRequest = CreateRequest(directory);
            unsafeRequest.ExtractedFiles = new[] { outside };
            ValidationException pathFailure = Assert.Throws<ValidationException>(() => PatentsViewLocalTableDiscovery.Discover(unsafeRequest))!;

            PatentsViewLocalTableDiscoveryRequest duplicateRequest = CreateRequest(directory);
            duplicateRequest.HeaderEvidence = new[]
            {
                new PatentsViewLocalHeaderEvidence
                {
                    Family = DatasetFamily.PatentsViewGrant,
                    Headers = new[] { "patent_id", "Patent Id" }
                }
            };
            ParseException schemaFailure = Assert.Throws<ParseException>(() => PatentsViewLocalTableDiscovery.Discover(duplicateRequest))!;

            PatentsViewLocalTableDiscoveryRequest limitedRequest = CreateRequest(directory);
            limitedRequest.MaximumTables = 1;
            limitedRequest.HeaderEvidence = new[]
            {
                new PatentsViewLocalHeaderEvidence { Family = DatasetFamily.PatentsViewGrant, TableIdentifier = "first", Headers = new[] { "patent_id" } },
                new PatentsViewLocalHeaderEvidence { Family = DatasetFamily.PatentsViewGrant, TableIdentifier = "second", Headers = new[] { "patent_id" } }
            };
            ValidationException limitFailure = Assert.Throws<ValidationException>(() => PatentsViewLocalTableDiscovery.Discover(limitedRequest))!;

            Assert.Multiple(() =>
            {
                Assert.That(pathFailure.ErrorCode, Is.EqualTo("PATH_OUTSIDE_MIRROR"));
                Assert.That(schemaFailure.DataPath, Is.EqualTo("Patent Id"));
                Assert.That(limitFailure.ErrorCode, Is.EqualTo("TABLE_LIMIT_EXCEEDED"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PreCancelledDiscoveryStopsBeforeInspectingCandidates()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        PatentsViewLocalTableDiscoveryRequest request = new PatentsViewLocalTableDiscoveryRequest
        {
            CancellationToken = cancellation.Token,
            HeaderEvidence = new[]
            {
                new PatentsViewLocalHeaderEvidence
                {
                    Family = DatasetFamily.PatentsViewGrant,
                    Headers = new[] { "patent_id" }
                }
            }
        };

        Assert.Throws<OperationCanceledException>(() => PatentsViewLocalTableDiscovery.Discover(request));
    }

    private static PatentsViewLocalTableDiscoveryRequest CreateRequest(string directory)
    {
        return new PatentsViewLocalTableDiscoveryRequest
        {
            MirrorDirectory = directory,
            Limits = new UsptoLimits { MaximumResults = 100 }
        };
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-local-table-discovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
