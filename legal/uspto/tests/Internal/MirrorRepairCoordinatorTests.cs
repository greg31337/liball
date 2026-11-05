using System.Security.Cryptography;
using System.Text;
using uspto;

namespace uspto.Tests;

public class MirrorRepairCoordinatorTests
{
    [Test]
    public void MissingFileIsDownloadedThenChecksummedExtractedAndParsed()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifest manifest = Manifest("data.bin", Array.Empty<DatasetManifestStage>(), Array.Empty<string>());
            Save(directory, manifest);
            byte[] replacement = Encoding.UTF8.GetBytes("replacement");
            RecordingMirrorTransfer transfer = Transfer("data.bin", replacement);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();
            MirrorRepairCoordinator repair = Coordinator(transfer, stages);

            MirrorRepairResult result = repair.Repair(Request(directory, Catalog("data.bin", replacement.LongLength)));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.RepairedCount, Is.EqualTo(1));
                Assert.That(result.Synchronization.TransferredCount, Is.EqualTo(1));
                Assert.That(transfer.CallCount, Is.EqualTo(1));
                Assert.That(stages.Calls, Is.EqualTo(new[] { MirrorPipelineStage.Checksum, MirrorPipelineStage.Extraction, MirrorPipelineStage.Parse }));
                Assert.That(File.ReadAllBytes(Path.Combine(directory, "files/data.bin")), Is.EqualTo(replacement));
                Assert.That(result.FinalVerification.Files[0].IsValid, Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void IncompleteChecksumRestartsAtChecksumWithoutDownloadingAgain()
    {
        string directory = CreateDirectory();
        try
        {
            byte[] content = Encoding.UTF8.GetBytes("local content");
            Write(directory, "files/data.bin", content);
            DatasetManifest manifest = Manifest("data.bin", new[]
            {
                Stage("download", MirrorStageState.Complete),
                Stage("checksum", MirrorStageState.Failed),
                Stage("extract", MirrorStageState.NotStarted),
                Stage("parse", MirrorStageState.NotStarted)
            }, Array.Empty<string>());
            SetSize(manifest, content.LongLength);
            Save(directory, manifest);
            RecordingMirrorTransfer transfer = Transfer("data.bin", content);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();

            MirrorRepairResult result = Coordinator(transfer, stages).Repair(Request(directory, Catalog("data.bin", content.LongLength)));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.RepairedCount, Is.EqualTo(1));
                Assert.That(transfer.CallCount, Is.Zero);
                Assert.That(stages.Calls, Is.EqualTo(new[] { MirrorPipelineStage.Checksum, MirrorPipelineStage.Extraction, MirrorPipelineStage.Parse }));
                Assert.That(result.Files[0].Synchronization, Is.Null);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MissingExtractionAndFailedParseReuseVerifiedDownloadAndChecksum()
    {
        string directory = CreateDirectory();
        try
        {
            byte[] content = Encoding.UTF8.GetBytes("local content");
            Write(directory, "files/data.bin", content);
            Write(directory, "extracted/keep.txt", Encoding.UTF8.GetBytes("keep"));
            DatasetManifest manifest = Manifest("data.bin", new[]
            {
                Stage("download", MirrorStageState.Complete),
                Stage("checksum", MirrorStageState.Complete),
                Stage("extract", MirrorStageState.Complete),
                Stage("parse", MirrorStageState.Failed)
            }, new[] { "extracted/keep.txt", "extracted/missing.txt" });
            SetEvidence(manifest, content.LongLength, Digest(content));
            Save(directory, manifest);
            RecordingMirrorTransfer transfer = Transfer("data.bin", content);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();

            MirrorRepairResult result = Coordinator(transfer, stages).Repair(Request(directory, Catalog("data.bin", content.LongLength)));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(transfer.CallCount, Is.Zero);
                Assert.That(stages.Calls, Is.EqualTo(new[] { MirrorPipelineStage.Extraction, MirrorPipelineStage.Parse }));
                Assert.That(File.ReadAllText(Path.Combine(directory, "extracted/keep.txt")), Is.EqualTo("keep"));
                Assert.That(DatasetManifestStore.Load(directory).Products[0].Files[0].ExtractedPaths, Is.EqualTo(new[] { "extracted/data.txt" }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CorruptFileIsReplacedWhileItsOriginalBytesArePreserved()
    {
        string directory = CreateDirectory();
        try
        {
            byte[] corrupt = Encoding.UTF8.GetBytes("bad");
            byte[] replacement = Encoding.UTF8.GetBytes("new");
            Write(directory, "files/data.bin", corrupt);
            DatasetManifest manifest = Manifest("data.bin", CompleteStages(), Array.Empty<string>());
            SetEvidence(manifest, replacement.LongLength, Digest(replacement));
            Save(directory, manifest);
            RecordingMirrorTransfer transfer = Transfer("data.bin", replacement);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();

            MirrorRepairResult result = Coordinator(transfer, stages).Repair(Request(directory, Catalog("data.bin", replacement.LongLength)));
            string backupDirectory = Path.Combine(directory, ".superseded", "PRODUCT");

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(transfer.CallCount, Is.EqualTo(1));
                Assert.That(File.ReadAllBytes(Path.Combine(directory, "files/data.bin")), Is.EqualTo(replacement));
                Assert.That(Directory.GetFiles(backupDirectory), Has.Length.EqualTo(1));
                Assert.That(File.ReadAllBytes(Directory.GetFiles(backupDirectory).Single()), Is.EqualTo(corrupt));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void RepeatingACompletedRepairIsIdempotent()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifest manifest = Manifest("data.bin", Array.Empty<DatasetManifestStage>(), Array.Empty<string>());
            Save(directory, manifest);
            byte[] replacement = Encoding.UTF8.GetBytes("replacement");
            RecordingMirrorTransfer transfer = Transfer("data.bin", replacement);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();
            MirrorRepairCoordinator repair = Coordinator(transfer, stages);
            MirrorRepairRequest request = Request(directory, Catalog("data.bin", replacement.LongLength));

            MirrorRepairResult first = repair.Repair(request);
            int stageCallsAfterFirst = stages.Calls.Count;
            MirrorRepairResult second = repair.Repair(request);

            Assert.Multiple(() =>
            {
                Assert.That(first.RepairedCount, Is.EqualTo(1));
                Assert.That(second.IsComplete, Is.True);
                Assert.That(second.RepairedCount, Is.Zero);
                Assert.That(second.SkippedCount, Is.EqualTo(1));
                Assert.That(second.Files[0].WasSkipped, Is.True);
                Assert.That(second.Synchronization.Entries, Is.Empty);
                Assert.That(transfer.CallCount, Is.EqualTo(1));
                Assert.That(stages.Calls, Has.Count.EqualTo(stageCallsAfterFirst));
                Assert.That(File.ReadAllBytes(Path.Combine(directory, "files/data.bin")), Is.EqualTo(replacement));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void InconsistentManifestBlocksAutomaticMutation()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifest manifest = Manifest("data.bin", Array.Empty<DatasetManifestStage>(), Array.Empty<string>());
            manifest.Products[0].Files[0].ProductIdentifier = "OTHER";
            Save(directory, manifest);
            string before = File.ReadAllText(DatasetManifestStore.GetPath(directory));
            RecordingMirrorTransfer transfer = Transfer("data.bin", Encoding.UTF8.GetBytes("replacement"));
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();

            MirrorRepairResult result = Coordinator(transfer, stages).Repair(Request(directory, Catalog("data.bin", 11L)));

            Assert.Multiple(() =>
            {
                Assert.That(result.ManifestBlocked, Is.True);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.Files[0].FailureCode, Is.EqualTo("MIRROR_MANIFEST_INCONSISTENT"));
                Assert.That(transfer.CallCount, Is.Zero);
                Assert.That(stages.Calls, Is.Empty);
                Assert.That(File.ReadAllText(DatasetManifestStore.GetPath(directory)), Is.EqualTo(before));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FailedRedownloadKeepsPartialStateAndDoesNotRunLaterStages()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifest manifest = Manifest("data.bin", Array.Empty<DatasetManifestStage>(), Array.Empty<string>());
            Save(directory, manifest);
            byte[] replacement = Encoding.UTF8.GetBytes("replacement");
            RecordingMirrorTransfer transfer = Transfer("data.bin", replacement);
            transfer.FailOnceFile = "data.bin";
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();

            MirrorRepairResult result = Coordinator(transfer, stages).Repair(Request(directory, Catalog("data.bin", replacement.LongLength)));
            DatasetManifestFile file = DatasetManifestStore.Load(directory).Products[0].Files[0];

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailedCount, Is.EqualTo(1));
                Assert.That(result.Files[0].FailureCode, Is.EqualTo("INTERRUPTED"));
                Assert.That(stages.Calls, Is.Empty);
                Assert.That(File.Exists(Path.Combine(directory, file.PartialPath!)), Is.True);
                Assert.That(File.ReadAllText(Path.Combine(directory, file.PartialPath!)), Is.EqualTo("par"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static MirrorRepairCoordinator Coordinator(RecordingMirrorTransfer transfer, RecordingMirrorStageExecutor stages)
    {
        return new MirrorRepairCoordinator(new MirrorSynchronizer(transfer), new MirrorStagePipeline(stages));
    }

    private static MirrorRepairRequest Request(string directory, BulkProduct catalog)
    {
        return new MirrorRepairRequest
        {
            MirrorDirectory = directory,
            Family = DatasetFamily.OfficeAction,
            CatalogProducts = new[] { catalog },
            MaximumParallelTransfers = 2,
            StaleLockSeconds = 3600,
            Now = DateTimeOffset.UtcNow,
            ChecksumBufferSize = 4,
            ArchiveLimits = new ExtractionLimits(),
            Context = new RequestContext(),
            Secrets = Array.Empty<string>()
        };
    }

    private static BulkProduct Catalog(string fileName, long size)
    {
        return new BulkProduct
        {
            ProductIdentifier = "PRODUCT",
            Files = new[]
            {
                new BulkProductFile
                {
                    ProductIdentifier = "PRODUCT",
                    FileName = fileName,
                    DownloadUri = new Uri("https://api.uspto.gov/files/" + fileName),
                    Size = size,
                    HasSize = true
                }
            }
        };
    }

    private static RecordingMirrorTransfer Transfer(string fileName, byte[] body)
    {
        RecordingMirrorTransfer result = new RecordingMirrorTransfer(0);
        result.Bodies[fileName] = body;
        return result;
    }

    private static DatasetManifest Manifest(string fileName, DatasetManifestStage[] stages, string[] extractedPaths)
    {
        PatentDataProvenance provenance = new PatentDataProvenance { Source = DataProvenance.LocalMirror, IsAuthoritative = false };
        return new DatasetManifest
        {
            ManifestIdentifier = "repair",
            Provenance = provenance,
            Products = new[]
            {
                new DatasetManifestProduct
                {
                    ProductIdentifier = "PRODUCT",
                    Family = DatasetFamily.OfficeAction,
                    Provenance = provenance,
                    Files = new[]
                    {
                        new DatasetManifestFile
                        {
                            ProductIdentifier = "PRODUCT",
                            FileName = fileName,
                            LocalPath = "files/" + fileName,
                            PartialPath = "files/" + fileName + ".partial",
                            ExtractedPaths = extractedPaths,
                            Stages = stages,
                            Provenance = provenance
                        }
                    }
                }
            }
        };
    }

    private static DatasetManifestStage[] CompleteStages()
    {
        return new[]
        {
            Stage("download", MirrorStageState.Complete),
            Stage("checksum", MirrorStageState.Complete),
            Stage("extract", MirrorStageState.Complete),
            Stage("parse", MirrorStageState.Complete)
        };
    }

    private static DatasetManifestStage Stage(string name, MirrorStageState state)
    {
        return new DatasetManifestStage { Name = name, State = state };
    }

    private static void SetSize(DatasetManifest manifest, long size)
    {
        manifest.Products[0].Files[0].Size = size;
        manifest.Products[0].Files[0].HasSize = true;
    }

    private static void SetEvidence(DatasetManifest manifest, long size, string digest)
    {
        SetSize(manifest, size);
        manifest.Products[0].Files[0].DigestAlgorithm = ChecksumAlgorithm.Sha256;
        manifest.Products[0].Files[0].Digest = digest;
        manifest.Products[0].Files[0].DigestVerified = true;
    }

    private static string Digest(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    private static void Write(string directory, string relativePath, byte[] content)
    {
        string path = Path.Combine(directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
    }

    private static void Save(string directory, DatasetManifest manifest)
    {
        DatasetManifestStore.Save(directory, manifest, Array.Empty<string>());
    }

    private static string CreateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-mirror-repair-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
