using System.Text.Json;
using System.Text.Json.Nodes;
using uspto;

namespace uspto.Tests;

public class DatasetManifestStoreTests
{
    [Test]
    public void ManifestRoundTripPreservesMetadataValidatorsDigestStagesAndProvenance()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifest source = CreateManifest();

            DatasetManifestStore.Save(directory, source, Array.Empty<string>());
            DatasetManifest result = DatasetManifestStore.Load(directory);
            DatasetManifestFile file = result.Products[0].Files[0];

            Assert.Multiple(() =>
            {
                Assert.That(result.FormatVersion, Is.EqualTo(1));
                Assert.That(result.ManifestIdentifier, Is.EqualTo("mirror-main"));
                Assert.That(result.HasUpdatedAt, Is.True);
                Assert.That(result.Products[0].Family, Is.EqualTo(DatasetFamily.PatentsViewGrant));
                Assert.That(file.LocalPath, Is.EqualTo("downloads/g_patent.tsv.gz"));
                Assert.That(file.PartialPath, Is.EqualTo("work/g_patent.tsv.gz.partial"));
                Assert.That(file.ExtractedPaths, Is.EqualTo(new[] { "tables/g_patent.tsv" }));
                Assert.That(file.Size, Is.EqualTo(12345));
                Assert.That(file.ETag, Is.EqualTo("\"etag-1\""));
                Assert.That(file.DigestAlgorithm, Is.EqualTo(ChecksumAlgorithm.Sha256));
                Assert.That(file.DigestVerified, Is.True);
                Assert.That(file.Stages.Select(stage => stage.Name), Is.EqualTo(new[] { "download", "checksum", "extract", "parse" }));
                Assert.That(file.Stages[2].State, Is.EqualTo(MirrorStageState.Complete));
                Assert.That(file.Provenance.Source, Is.EqualTo(DataProvenance.LocalMirror));
                Assert.That(file.Provenance.DatasetVersion, Is.EqualTo("2026-09"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void EveryManifestFieldAndStageStateSurvivesRoundTrip()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifest source = CreateManifest();
            DateTimeOffset timestamp = source.UpdatedAt;
            DatasetManifestStage failed = Stage("failed", MirrorStageState.Failed, timestamp);
            failed.FailureCode = "PARSE_FAILED";
            failed.FailureMessage = "The local parser rejected the table.";
            source.Products[0].Files[0].Stages = new[]
            {
                Stage("pending", MirrorStageState.NotStarted, timestamp),
                Stage("running", MirrorStageState.InProgress, timestamp),
                Stage("complete", MirrorStageState.Complete, timestamp),
                failed
            };

            DatasetManifestStore.Save(directory, source, Array.Empty<string>());
            DatasetManifest result = DatasetManifestStore.Load(directory);
            DatasetManifestProduct product = result.Products.Single();
            DatasetManifestFile file = product.Files.Single();

            Assert.Multiple(() =>
            {
                Assert.That(result.FormatVersion, Is.EqualTo(DatasetManifestStore.FormatVersion));
                Assert.That(result.ManifestIdentifier, Is.EqualTo(source.ManifestIdentifier));
                Assert.That(result.CreatedAt, Is.EqualTo(source.CreatedAt));
                Assert.That(result.HasCreatedAt, Is.True);
                Assert.That(result.UpdatedAt, Is.EqualTo(source.UpdatedAt));
                Assert.That(result.HasUpdatedAt, Is.True);
                AssertProvenance(result.Provenance, source.Provenance);
                Assert.That(product.ProductIdentifier, Is.EqualTo("PTGRXML"));
                Assert.That(product.Title, Is.EqualTo("Patent grants"));
                Assert.That(product.Family, Is.EqualTo(DatasetFamily.PatentsViewGrant));
                Assert.That(product.CatalogUri, Is.EqualTo(new Uri("https://data.uspto.gov/product/PTGRXML")));
                Assert.That(product.LastModified, Is.EqualTo(timestamp));
                Assert.That(product.HasLastModified, Is.True);
                AssertProvenance(product.Provenance, source.Provenance);
                Assert.That(file.ProductIdentifier, Is.EqualTo("PTGRXML"));
                Assert.That(file.FileName, Is.EqualTo("g_patent.tsv.gz"));
                Assert.That(file.CatalogUri, Is.EqualTo(new Uri("https://data.uspto.gov/product/PTGRXML")));
                Assert.That(file.DownloadUri, Is.EqualTo(new Uri("https://example.test/g_patent.tsv.gz")));
                Assert.That(file.LocalPath, Is.EqualTo("downloads/g_patent.tsv.gz"));
                Assert.That(file.PartialPath, Is.EqualTo("work/g_patent.tsv.gz.partial"));
                Assert.That(file.ExtractedPaths, Is.EqualTo(new[] { "tables/g_patent.tsv" }));
                Assert.That(file.Size, Is.EqualTo(12345));
                Assert.That(file.HasSize, Is.True);
                Assert.That(file.ETag, Is.EqualTo("\"etag-1\""));
                Assert.That(file.LastModified, Is.EqualTo(timestamp));
                Assert.That(file.HasLastModified, Is.True);
                Assert.That(file.DigestAlgorithm, Is.EqualTo(ChecksumAlgorithm.Sha256));
                Assert.That(file.Digest, Is.EqualTo(new string('a', 64)));
                Assert.That(file.DigestVerified, Is.True);
                AssertProvenance(file.Provenance, source.Provenance);
                Assert.That(file.Stages.Select(stage => stage.State), Is.EqualTo(new[]
                {
                    MirrorStageState.NotStarted,
                    MirrorStageState.InProgress,
                    MirrorStageState.Complete,
                    MirrorStageState.Failed
                }));
                Assert.That(file.Stages.All(stage => stage.HasUpdatedAt && stage.UpdatedAt == timestamp), Is.True);
                Assert.That(file.Stages[3].FailureCode, Is.EqualTo("PARSE_FAILED"));
                Assert.That(file.Stages[3].FailureMessage, Is.EqualTo("The local parser rejected the table."));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MinimalVersionOneManifestLoadsWithDefaultsAndIgnoresUnknownFutureFields()
    {
        string directory = CreateDirectory();
        try
        {
            JsonObject root = new JsonObject
            {
                ["formatVersion"] = 1,
                ["manifestIdentifier"] = "legacy-v1",
                ["futureRootState"] = "preserved-by-a-newer-writer",
                ["products"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["productIdentifier"] = "LEGACY",
                        ["futureProductState"] = 7,
                        ["files"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["productIdentifier"] = "LEGACY",
                                ["fileName"] = "legacy.tsv",
                                ["futureFileState"] = true
                            }
                        }
                    }
                }
            };
            File.WriteAllText(DatasetManifestStore.GetPath(directory), root.ToJsonString());

            DatasetManifest result = DatasetManifestStore.Load(directory);
            DatasetManifestProduct product = result.Products.Single();
            DatasetManifestFile file = product.Files.Single();
            DatasetManifestStore.Save(directory, result, Array.Empty<string>());
            DatasetManifest migrated = DatasetManifestStore.Load(directory);
            string migratedJson = File.ReadAllText(DatasetManifestStore.GetPath(directory));

            Assert.Multiple(() =>
            {
                Assert.That(result.ManifestIdentifier, Is.EqualTo("legacy-v1"));
                Assert.That(result.HasCreatedAt, Is.False);
                Assert.That(result.HasUpdatedAt, Is.False);
                Assert.That(result.Provenance, Is.Not.Null);
                Assert.That(product.Family, Is.EqualTo(DatasetFamily.Unknown));
                Assert.That(product.Provenance, Is.Not.Null);
                Assert.That(file.ExtractedPaths, Is.Empty);
                Assert.That(file.Stages, Is.Empty);
                Assert.That(file.HasSize, Is.False);
                Assert.That(file.DigestAlgorithm, Is.EqualTo(ChecksumAlgorithm.None));
                Assert.That(file.Provenance, Is.Not.Null);
                Assert.That(migrated.ManifestIdentifier, Is.EqualTo("legacy-v1"));
                Assert.That(migrated.Products[0].Files[0].Stages, Is.Empty);
                Assert.That(migratedJson, Does.Not.Contain("futureRootState"));
                Assert.That(migratedJson, Does.Contain("\"hasSize\": false"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SerializationIsDeterministicAcrossRepeatedAndLoadedRoundTrips()
    {
        DatasetManifest source = CreateManifest();

        string first = DatasetManifestStore.Serialize(source);
        string repeated = DatasetManifestStore.Serialize(source);
        DatasetManifest deserialized = JsonSerializer.Deserialize<DatasetManifest>(first, DatasetManifestStore.CreateOptions())!;
        string roundTripped = DatasetManifestStore.Serialize(deserialized);

        Assert.Multiple(() =>
        {
            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(roundTripped, Is.EqualTo(first));
        });
    }

    [Test]
    public void SaveAtomicallyReplacesTheManifestAndLeavesNoTemporaryFile()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifest first = CreateManifest();
            DatasetManifestStore.Save(directory, first, Array.Empty<string>());
            DatasetManifest second = CreateManifest();
            second.ManifestIdentifier = "mirror-replaced";
            second.Products[0].Files[0].ETag = "\"etag-2\"";

            DatasetManifestStore.Save(directory, second, Array.Empty<string>());
            DatasetManifest loaded = DatasetManifestStore.Load(directory);
            string path = DatasetManifestStore.GetPath(directory);

            Assert.Multiple(() =>
            {
                Assert.That(loaded.ManifestIdentifier, Is.EqualTo("mirror-replaced"));
                Assert.That(loaded.Products[0].Files[0].ETag, Is.EqualTo("\"etag-2\""));
                Assert.That(File.Exists(path), Is.True);
                Assert.That(File.Exists(path + ".tmp"), Is.False);
                Assert.DoesNotThrow(() => JsonDocument.Parse(File.ReadAllText(path)));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FailedSaveLeavesThePreviousManifestUntouched()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifestStore.Save(directory, CreateManifest(), Array.Empty<string>());
            string path = DatasetManifestStore.GetPath(directory);
            string original = File.ReadAllText(path);
            DatasetManifest invalid = CreateManifest();
            invalid.Products[0].Files[0].LocalPath = "../outside.tsv";

            Assert.Throws<ValidationException>(() => DatasetManifestStore.Save(directory, invalid, Array.Empty<string>()));

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllText(path), Is.EqualTo(original));
                Assert.That(File.Exists(path + ".tmp"), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PersistedManifestExcludesUriCredentialsAndRedactsKnownSecrets()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifest source = CreateManifest();
            source.ManifestIdentifier = "mirror-top-secret";
            source.Provenance.SourceUri = new Uri("https://source-user:source-password@example.test/manifest?token=top-secret");
            source.Provenance.RequestIdentifier = "request-top-secret";
            source.Products[0].Title = "Patent grants top-secret";
            source.Products[0].CatalogUri = new Uri("https://catalog-user:catalog-password@example.test/product?api_key=top-secret");
            DatasetManifestFile file = source.Products[0].Files[0];
            file.DownloadUri = new Uri("https://user:password@example.test/file?api_key=top-secret&part=1");
            file.ETag = "top-secret";
            file.Stages[0].FailureMessage = "request used top-secret";

            DatasetManifest persisted = DatasetManifestStore.Save(directory, source, new[] { "top-secret" });
            string json = File.ReadAllText(DatasetManifestStore.GetPath(directory));

            Assert.Multiple(() =>
            {
                Assert.That(json, Does.Not.Contain("top-secret"));
                Assert.That(json, Does.Not.Contain("password"));
                Assert.That(json, Does.Not.Contain("user:"));
                Assert.That(persisted.ManifestIdentifier, Is.EqualTo("mirror-[REDACTED]"));
                Assert.That(persisted.Provenance.SourceUri!.UserInfo, Is.Empty);
                Assert.That(persisted.Provenance.SourceUri!.Query, Does.Contain("token=%5BREDACTED%5D"));
                Assert.That(persisted.Provenance.RequestIdentifier, Is.EqualTo("request-[REDACTED]"));
                Assert.That(persisted.Products[0].Title, Is.EqualTo("Patent grants [REDACTED]"));
                Assert.That(persisted.Products[0].CatalogUri!.UserInfo, Is.Empty);
                Assert.That(persisted.Products[0].Files[0].DownloadUri!.UserInfo, Is.Empty);
                Assert.That(persisted.Products[0].Files[0].DownloadUri!.Query, Does.Contain("api_key=%5BREDACTED%5D"));
                Assert.That(persisted.Products[0].Files[0].ETag, Is.EqualTo("[REDACTED]"));
                Assert.That(source.ManifestIdentifier, Is.EqualTo("mirror-top-secret"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("/absolute/table.tsv")]
    [TestCase("../outside.tsv")]
    [TestCase("./table.tsv")]
    [TestCase("tables//table.tsv")]
    [TestCase("tables/../table.tsv")]
    [TestCase("tables\\..\\table.tsv")]
    public void UnsafeLocalPathsAreRejected(string unsafePath)
    {
        DatasetManifest manifest = CreateManifest();
        manifest.Products[0].Files[0].LocalPath = unsafePath;

        ValidationException failure = Assert.Throws<ValidationException>(() => DatasetManifestStore.Serialize(manifest))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.FieldName, Is.EqualTo("LocalPath"));
            Assert.That(failure.ErrorCode, Is.EqualTo("MANIFEST_PATH_INVALID"));
        });
    }

    [Test]
    public void PartialAndExtractedPathsReceiveTheSameRelativePathValidation()
    {
        DatasetManifest partial = CreateManifest();
        partial.Products[0].Files[0].PartialPath = "/absolute/file.partial";
        DatasetManifest extracted = CreateManifest();
        extracted.Products[0].Files[0].ExtractedPaths = new[] { "tables/g_patent.tsv", "../outside.tsv" };

        ValidationException partialFailure = Assert.Throws<ValidationException>(() => DatasetManifestStore.Serialize(partial))!;
        ValidationException extractedFailure = Assert.Throws<ValidationException>(() => DatasetManifestStore.Serialize(extracted))!;

        Assert.Multiple(() =>
        {
            Assert.That(partialFailure.FieldName, Is.EqualTo("PartialPath"));
            Assert.That(partialFailure.ErrorCode, Is.EqualTo("MANIFEST_PATH_INVALID"));
            Assert.That(extractedFailure.FieldName, Is.EqualTo("ExtractedPaths"));
            Assert.That(extractedFailure.ErrorCode, Is.EqualTo("MANIFEST_PATH_INVALID"));
        });
    }

    [Test]
    public void MissingEmptyNullMalformedAndStructurallyInvalidManifestsFailPredictably()
    {
        string directory = CreateDirectory();
        try
        {
            string path = DatasetManifestStore.GetPath(directory);
            ValidationException missing = Assert.Throws<ValidationException>(() => DatasetManifestStore.Load(directory))!;

            File.WriteAllText(path, string.Empty);
            ValidationException empty = Assert.Throws<ValidationException>(() => DatasetManifestStore.Load(directory))!;

            File.WriteAllText(path, "null");
            ValidationException nullDocument = Assert.Throws<ValidationException>(() => DatasetManifestStore.Load(directory))!;

            File.WriteAllText(path, "{not-json");
            JsonException malformed = Assert.Throws<JsonException>(() => DatasetManifestStore.Load(directory))!;

            File.WriteAllText(path, "{\"formatVersion\":1,\"products\":[null]}");
            ValidationException invalidProduct = Assert.Throws<ValidationException>(() => DatasetManifestStore.Load(directory))!;

            Assert.Multiple(() =>
            {
                Assert.That(missing.ErrorCode, Is.EqualTo("MANIFEST_NOT_FOUND"));
                Assert.That(empty.ErrorCode, Is.EqualTo("MANIFEST_SIZE_INVALID"));
                Assert.That(nullDocument.ErrorCode, Is.EqualTo("MANIFEST_INVALID"));
                Assert.That(malformed.Path, Is.EqualTo("$"));
                Assert.That(invalidProduct.ErrorCode, Is.EqualTo("MANIFEST_PRODUCT_INVALID"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void InvalidVersionsPathsDigestsAndStagesAreRejected()
    {
        DatasetManifest version = CreateManifest();
        version.FormatVersion = 2;
        DatasetManifest path = CreateManifest();
        path.Products[0].Files[0].LocalPath = "../outside.tsv";
        DatasetManifest digest = CreateManifest();
        digest.Products[0].Files[0].Digest = "not-a-digest";
        DatasetManifest stage = CreateManifest();
        stage.Products[0].Files[0].Stages[0].Name = null;

        ValidationException versionFailure = Assert.Throws<ValidationException>(() => DatasetManifestStore.Serialize(version))!;
        ValidationException pathFailure = Assert.Throws<ValidationException>(() => DatasetManifestStore.Serialize(path))!;
        ValidationException digestFailure = Assert.Throws<ValidationException>(() => DatasetManifestStore.Serialize(digest))!;
        ValidationException stageFailure = Assert.Throws<ValidationException>(() => DatasetManifestStore.Serialize(stage))!;

        Assert.Multiple(() =>
        {
            Assert.That(versionFailure.ErrorCode, Is.EqualTo("MANIFEST_VERSION_UNSUPPORTED"));
            Assert.That(pathFailure.ErrorCode, Is.EqualTo("MANIFEST_PATH_INVALID"));
            Assert.That(digestFailure.ErrorCode, Is.EqualTo("MANIFEST_DIGEST_INVALID"));
            Assert.That(stageFailure.ErrorCode, Is.EqualTo("MANIFEST_STAGE_INVALID"));
        });
    }

    private static DatasetManifest CreateManifest()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);
        PatentDataProvenance provenance = new PatentDataProvenance
        {
            Source = DataProvenance.LocalMirror,
            SourceUri = new Uri("https://example.test/source"),
            DatasetIdentifier = "g_patent",
            DatasetVersion = "2026-09",
            RequestIdentifier = "manifest-request-1",
            RetrievedAt = timestamp,
            HasRetrievedAt = true,
            LastUpdatedAt = timestamp.AddHours(-1),
            HasLastUpdatedAt = true,
            FromCache = true,
            FieldSources = new[]
            {
                new FieldDataProvenance
                {
                    FieldPath = "Files",
                    Source = DataProvenance.PatentsView,
                    Delivery = DataProvenance.LocalMirror,
                    DatasetIdentifier = "g_patent",
                    DatasetVersion = "2026-09",
                    IsAuthoritative = false
                }
            },
            IsAuthoritative = false
        };
        DatasetManifestFile file = new DatasetManifestFile
        {
            ProductIdentifier = "PTGRXML",
            FileName = "g_patent.tsv.gz",
            CatalogUri = new Uri("https://data.uspto.gov/product/PTGRXML"),
            DownloadUri = new Uri("https://example.test/g_patent.tsv.gz"),
            LocalPath = "downloads/g_patent.tsv.gz",
            PartialPath = "work/g_patent.tsv.gz.partial",
            ExtractedPaths = new[] { "tables/g_patent.tsv" },
            Size = 12345,
            HasSize = true,
            ETag = "\"etag-1\"",
            LastModified = timestamp,
            HasLastModified = true,
            DigestAlgorithm = ChecksumAlgorithm.Sha256,
            Digest = new string('a', 64),
            DigestVerified = true,
            Provenance = provenance,
            Stages = new[]
            {
                Stage("download", MirrorStageState.Complete, timestamp),
                Stage("checksum", MirrorStageState.Complete, timestamp),
                Stage("extract", MirrorStageState.Complete, timestamp),
                Stage("parse", MirrorStageState.InProgress, timestamp)
            }
        };
        return new DatasetManifest
        {
            FormatVersion = 1,
            ManifestIdentifier = "mirror-main",
            CreatedAt = timestamp,
            HasCreatedAt = true,
            UpdatedAt = timestamp,
            HasUpdatedAt = true,
            Provenance = provenance,
            Products = new[]
            {
                new DatasetManifestProduct
                {
                    ProductIdentifier = "PTGRXML",
                    Title = "Patent grants",
                    Family = DatasetFamily.PatentsViewGrant,
                    CatalogUri = new Uri("https://data.uspto.gov/product/PTGRXML"),
                    LastModified = timestamp,
                    HasLastModified = true,
                    Files = new[] { file },
                    Provenance = provenance
                }
            }
        };
    }

    private static DatasetManifestStage Stage(string name, MirrorStageState state, DateTimeOffset timestamp)
    {
        return new DatasetManifestStage { Name = name, State = state, UpdatedAt = timestamp, HasUpdatedAt = true };
    }

    private static void AssertProvenance(PatentDataProvenance actual, PatentDataProvenance expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Source, Is.EqualTo(expected.Source));
            Assert.That(actual.SourceUri, Is.EqualTo(expected.SourceUri));
            Assert.That(actual.DatasetIdentifier, Is.EqualTo(expected.DatasetIdentifier));
            Assert.That(actual.DatasetVersion, Is.EqualTo(expected.DatasetVersion));
            Assert.That(actual.RequestIdentifier, Is.EqualTo(expected.RequestIdentifier));
            Assert.That(actual.RetrievedAt, Is.EqualTo(expected.RetrievedAt));
            Assert.That(actual.HasRetrievedAt, Is.EqualTo(expected.HasRetrievedAt));
            Assert.That(actual.LastUpdatedAt, Is.EqualTo(expected.LastUpdatedAt));
            Assert.That(actual.HasLastUpdatedAt, Is.EqualTo(expected.HasLastUpdatedAt));
            Assert.That(actual.FromCache, Is.EqualTo(expected.FromCache));
            Assert.That(actual.IsAuthoritative, Is.EqualTo(expected.IsAuthoritative));
            Assert.That(actual.FieldSources, Has.Length.EqualTo(1));
            Assert.That(actual.FieldSources[0].FieldPath, Is.EqualTo("Files"));
            Assert.That(actual.FieldSources[0].Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(actual.FieldSources[0].Delivery, Is.EqualTo(DataProvenance.LocalMirror));
            Assert.That(actual.FieldSources[0].DatasetIdentifier, Is.EqualTo("g_patent"));
            Assert.That(actual.FieldSources[0].DatasetVersion, Is.EqualTo("2026-09"));
            Assert.That(actual.FieldSources[0].IsAuthoritative, Is.False);
        });
    }

    private static string CreateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-manifest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
