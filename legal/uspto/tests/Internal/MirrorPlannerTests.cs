using System.Security.Cryptography;
using System.Text;
using uspto;

namespace uspto.Tests;

public class MirrorPlannerTests
{
    [Test]
    public void PlanClassifiesEveryCatalogAndLocalStateWithoutDeletingArtifacts()
    {
        string directory = CreateDirectory();
        try
        {
            WriteFile(directory, "files/unchanged.bin", "steady");
            WriteFile(directory, "files/corrupt.bin", "broken");
            WriteFile(directory, "partial/resumable.bin.partial", "part");
            WriteFile(directory, "files/orphaned.bin", "preserve-me");

            DatasetManifest manifest = CreateManifest(new[]
            {
                ManifestFile("unchanged.bin", "files/unchanged.bin", null, 6L, Digest("steady"), true),
                ManifestFile("changed.bin", "files/changed.bin", null, 8L, null, false),
                ManifestFile("missing.bin", "files/missing.bin", null, 9L, null, false),
                ManifestFile("corrupt.bin", "files/corrupt.bin", null, 6L, Digest("target"), true),
                ManifestFile("resumable.bin", "files/resumable.bin", "partial/resumable.bin.partial", 12L, null, false),
                ManifestFile("orphaned.bin", "files/orphaned.bin", null, 11L, null, false)
            });
            manifest.Products[0].Files[4].ETag = "\"resume-validator\"";
            BulkProduct catalog = CreateCatalogProduct(new[]
            {
                CatalogFile("unchanged.bin", 6L, Digest("steady")),
                CatalogFile("new.bin", 7L, null),
                CatalogFile("changed.bin", 10L, null),
                CatalogFile("missing.bin", 9L, null),
                CatalogFile("corrupt.bin", 6L, Digest("target")),
                CatalogFile("resumable.bin", 12L, null)
            });

            MirrorPlanResult result = MirrorPlanner.Plan(CreateRequest(directory, catalog, manifest));

            Assert.Multiple(() =>
            {
                Assert.That(result.Entries.Select(entry => entry.Action), Is.EqualTo(new[]
                {
                    MirrorAction.Unchanged,
                    MirrorAction.New,
                    MirrorAction.Changed,
                    MirrorAction.Missing,
                    MirrorAction.Corrupt,
                    MirrorAction.Resumable,
                    MirrorAction.Orphaned
                }));
                Assert.That(result.UnchangedCount, Is.EqualTo(1));
                Assert.That(result.NewCount, Is.EqualTo(1));
                Assert.That(result.ChangedCount, Is.EqualTo(1));
                Assert.That(result.MissingCount, Is.EqualTo(1));
                Assert.That(result.CorruptCount, Is.EqualTo(1));
                Assert.That(result.ResumableCount, Is.EqualTo(1));
                Assert.That(result.OrphanedCount, Is.EqualTo(1));
                Assert.That(result.Entries[4].Reason, Is.EqualTo("LOCAL_DIGEST_MISMATCH"));
                Assert.That(result.Entries[5].LocalSize, Is.EqualTo(4L));
                Assert.That(result.Entries[6].CatalogFile, Is.Null);
                Assert.That(File.ReadAllText(Path.Combine(directory, "files/orphaned.bin")), Is.EqualTo("preserve-me"));
                Assert.That(File.ReadAllText(Path.Combine(directory, "partial/resumable.bin.partial")), Is.EqualTo("part"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CatalogValidatorChangesTakePrecedenceOverLocalArtifactState()
    {
        string directory = CreateDirectory();
        try
        {
            WriteFile(directory, "files/data.bin", "current");
            DatasetManifestFile manifestFile = ManifestFile("data.bin", "files/data.bin", null, 7L, Digest("current"), true);
            DatasetManifest manifest = CreateManifest(new[] { manifestFile });
            BulkProduct catalog = CreateCatalogProduct(new[] { CatalogFile("data.bin", 7L, Digest("replacement")) });

            MirrorPlanResult result = MirrorPlanner.Plan(CreateRequest(directory, catalog, manifest));

            Assert.Multiple(() =>
            {
                Assert.That(result.Entries.Single().Action, Is.EqualTo(MirrorAction.Changed));
                Assert.That(result.Entries.Single().Reason, Is.EqualTo("CATALOG_METADATA_CHANGED"));
                Assert.That(File.ReadAllText(Path.Combine(directory, "files/data.bin")), Is.EqualTo("current"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void OversizedOrUnvalidatedPartialsAreNotConsideredResumable()
    {
        string directory = CreateDirectory();
        try
        {
            WriteFile(directory, "partial/oversized.partial", "too-large");
            WriteFile(directory, "partial/unvalidated.partial", "part");
            DatasetManifestFile oversized = ManifestFile("oversized.bin", "files/oversized.bin", "partial/oversized.partial", 4L, null, false);
            oversized.ETag = "\"etag\"";
            DatasetManifestFile unvalidated = ManifestFile("unvalidated.bin", "files/unvalidated.bin", "partial/unvalidated.partial", 10L, null, false);
            DatasetManifest manifest = CreateManifest(new[] { oversized, unvalidated });
            BulkProduct catalog = CreateCatalogProduct(new[]
            {
                CatalogFile("oversized.bin", 4L, null),
                CatalogFile("unvalidated.bin", 10L, null)
            });

            MirrorPlanResult result = MirrorPlanner.Plan(CreateRequest(directory, catalog, manifest));

            Assert.That(result.Entries.Select(entry => entry.Action), Is.EqualTo(new[] { MirrorAction.Corrupt, MirrorAction.Missing }));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CancellationStopsPlanningWithoutCreatingTheMirrorDirectory()
    {
        string parent = CreateDirectory();
        string directory = Path.Combine(parent, "not-created");
        try
        {
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            MirrorPlanRequest request = CreateRequest(directory, CreateCatalogProduct(Array.Empty<BulkProductFile>()), CreateManifest(Array.Empty<DatasetManifestFile>()));
            request.CancellationToken = cancellation.Token;

            Assert.Throws<OperationCanceledException>(() => MirrorPlanner.Plan(request));
            Assert.That(Directory.Exists(directory), Is.False);
        }
        finally
        {
            Directory.Delete(parent, true);
        }
    }

    private static MirrorPlanRequest CreateRequest(string directory, BulkProduct catalog, DatasetManifest manifest)
    {
        return new MirrorPlanRequest
        {
            MirrorDirectory = directory,
            CatalogProducts = new[] { catalog },
            Manifest = manifest,
            VerifyDigests = true,
            ChecksumBufferSize = 4
        };
    }

    private static BulkProduct CreateCatalogProduct(BulkProductFile[] files)
    {
        return new BulkProduct { ProductIdentifier = "PRODUCT", Files = files };
    }

    private static BulkProductFile CatalogFile(string name, long size, string? digest)
    {
        BulkProductFile result = new BulkProductFile
        {
            ProductIdentifier = "PRODUCT",
            FileName = name,
            Size = size,
            HasSize = true
        };
        if (digest != null)
        {
            result.Checksums = new[] { new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256, Value = digest } };
        }

        return result;
    }

    private static DatasetManifest CreateManifest(DatasetManifestFile[] files)
    {
        PatentDataProvenance provenance = new PatentDataProvenance { Source = DataProvenance.LocalMirror, IsAuthoritative = false };
        return new DatasetManifest
        {
            Provenance = provenance,
            Products = new[]
            {
                new DatasetManifestProduct
                {
                    ProductIdentifier = "PRODUCT",
                    Family = DatasetFamily.PatentsViewGrant,
                    Files = files,
                    Provenance = provenance
                }
            }
        };
    }

    private static DatasetManifestFile ManifestFile(string name, string localPath, string? partialPath, long size, string? digest, bool verified)
    {
        return new DatasetManifestFile
        {
            ProductIdentifier = "PRODUCT",
            FileName = name,
            LocalPath = localPath,
            PartialPath = partialPath,
            Size = size,
            HasSize = true,
            DigestAlgorithm = digest == null ? ChecksumAlgorithm.None : ChecksumAlgorithm.Sha256,
            Digest = digest,
            DigestVerified = verified,
            Provenance = new PatentDataProvenance { Source = DataProvenance.LocalMirror, IsAuthoritative = false }
        };
    }

    private static string Digest(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static void WriteFile(string root, string relativePath, string content)
    {
        string path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static string CreateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-mirror-plan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
