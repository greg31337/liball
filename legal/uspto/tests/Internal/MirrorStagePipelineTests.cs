using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;
using uspto;

namespace uspto.Tests;

public class MirrorStagePipelineTests
{
    [Test]
    public void PipelinePersistsIndependentDownloadChecksumExtractionAndParseStages()
    {
        string directory = CreateDirectory();
        try
        {
            SaveManifest(directory, Array.Empty<DatasetManifestStage>());
            RecordingMirrorStageExecutor executor = new RecordingMirrorStageExecutor();
            MirrorStagePipeline pipeline = new MirrorStagePipeline(executor);

            MirrorStagePipelineResult result = pipeline.Run(Request(directory));
            DatasetManifestFile file = DatasetManifestStore.Load(directory).Products[0].Files[0];

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.ExecutedCount, Is.EqualTo(4));
                Assert.That(result.ReusedCount, Is.Zero);
                Assert.That(executor.Calls, Is.EqualTo(new[]
                {
                    MirrorPipelineStage.Download,
                    MirrorPipelineStage.Checksum,
                    MirrorPipelineStage.Extraction,
                    MirrorPipelineStage.Parse
                }));
                Assert.That(file.Stages.Select(stage => stage.Name), Is.EqualTo(new[] { "download", "checksum", "extract", "parse" }));
                Assert.That(file.Stages.Select(stage => stage.State), Has.All.EqualTo(MirrorStageState.Complete));
                Assert.That(file.Size, Is.EqualTo(executor.Content.Length));
                Assert.That(file.DigestAlgorithm, Is.EqualTo(ChecksumAlgorithm.Sha256));
                Assert.That(file.DigestVerified, Is.True);
                Assert.That(file.ExtractedPaths, Is.EqualTo(new[] { "extracted/data.txt" }));
                Assert.That(File.ReadAllText(Path.Combine(directory, "extracted/data.txt")), Is.EqualTo("parsed input"));
                Assert.That(File.Exists(Path.Combine(directory, MirrorStateCoordinator.LockFileName)), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ParseRetryReusesEveryCompletedUpstreamArtifact()
    {
        string directory = CreateDirectory();
        try
        {
            SaveManifest(directory, Array.Empty<DatasetManifestStage>());
            RecordingMirrorStageExecutor executor = new RecordingMirrorStageExecutor();
            executor.ConfigureFailure(MirrorPipelineStage.Parse, 1, "parser saw secret-value");
            MirrorStagePipeline pipeline = new MirrorStagePipeline(executor);
            MirrorStagePipelineRequest request = Request(directory);
            request.Secrets = new[] { "secret-value" };

            MirrorStagePipelineResult failed = pipeline.Run(request);
            DatasetManifestFile failedFile = DatasetManifestStore.Load(directory).Products[0].Files[0];
            executor.Calls.Clear();
            MirrorStagePipelineResult retried = pipeline.Run(request);

            Assert.Multiple(() =>
            {
                Assert.That(failed.IsComplete, Is.False);
                Assert.That(failed.Stages.Last().FailureCode, Is.EqualTo("PARSE_FAILED"));
                Assert.That(failed.Stages.Last().FailureMessage, Is.EqualTo("parser saw [REDACTED]"));
                Assert.That(Stage(failedFile, "parse").FailureMessage, Is.EqualTo("parser saw [REDACTED]"));
                Assert.That(retried.IsComplete, Is.True);
                Assert.That(retried.ReusedCount, Is.EqualTo(3));
                Assert.That(retried.ExecutedCount, Is.EqualTo(1));
                Assert.That(executor.Calls, Is.EqualTo(new[] { MirrorPipelineStage.Parse }));
                Assert.That(File.ReadAllText(Path.Combine(directory, "files/data.zip")), Is.EqualTo(Encoding.UTF8.GetString(executor.Content)));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ChecksumRetryReusesDownloadAndRerunsAllDownstreamStages()
    {
        string directory = CreateDirectory();
        try
        {
            SaveManifest(directory, Array.Empty<DatasetManifestStage>());
            RecordingMirrorStageExecutor executor = new RecordingMirrorStageExecutor();
            executor.ConfigureFailure(MirrorPipelineStage.Checksum, 1, "checksum failed");
            MirrorStagePipeline pipeline = new MirrorStagePipeline(executor);

            MirrorStagePipelineResult failed = pipeline.Run(Request(directory));
            executor.Calls.Clear();
            MirrorStagePipelineResult retried = pipeline.Run(Request(directory));

            Assert.Multiple(() =>
            {
                Assert.That(failed.Stages.Select(stage => stage.Stage), Is.EqualTo(new[] { MirrorPipelineStage.Download, MirrorPipelineStage.Checksum }));
                Assert.That(retried.ReusedCount, Is.EqualTo(1));
                Assert.That(retried.ExecutedCount, Is.EqualTo(3));
                Assert.That(executor.Calls, Is.EqualTo(new[]
                {
                    MirrorPipelineStage.Checksum,
                    MirrorPipelineStage.Extraction,
                    MirrorPipelineStage.Parse
                }));
                Assert.That(DatasetManifestStore.Load(directory).Products[0].Files[0].Stages.Select(stage => stage.State), Has.All.EqualTo(MirrorStageState.Complete));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void RerunningAnUpstreamStageInvalidatesPreviouslyCompletedDownstreamState()
    {
        string directory = CreateDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(directory, "files"));
            File.WriteAllBytes(Path.Combine(directory, "files/data.zip"), Encoding.UTF8.GetBytes("archive bytes"));
            SaveManifest(directory, new[]
            {
                ManifestStage("download", MirrorStageState.Complete),
                ManifestStage("checksum", MirrorStageState.Failed),
                ManifestStage("extract", MirrorStageState.Complete),
                ManifestStage("parse", MirrorStageState.Complete)
            });
            RecordingMirrorStageExecutor executor = new RecordingMirrorStageExecutor();
            MirrorStagePipeline pipeline = new MirrorStagePipeline(executor);

            MirrorStagePipelineResult result = pipeline.Run(Request(directory));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.ReusedCount, Is.EqualTo(1));
                Assert.That(executor.Calls, Is.EqualTo(new[]
                {
                    MirrorPipelineStage.Checksum,
                    MirrorPipelineStage.Extraction,
                    MirrorPipelineStage.Parse
                }));
                Assert.That(result.Stages.Skip(1).Select(stage => stage.WasExecuted), Has.All.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void LastStageStopsThePipelineWithoutCompletingLaterWork()
    {
        string directory = CreateDirectory();
        try
        {
            SaveManifest(directory, Array.Empty<DatasetManifestStage>());
            RecordingMirrorStageExecutor executor = new RecordingMirrorStageExecutor();
            MirrorStagePipelineRequest request = Request(directory);
            request.LastStage = MirrorPipelineStage.Checksum;

            MirrorStagePipelineResult result = new MirrorStagePipeline(executor).Run(request);
            DatasetManifestFile file = DatasetManifestStore.Load(directory).Products[0].Files[0];

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(executor.Calls, Is.EqualTo(new[] { MirrorPipelineStage.Download, MirrorPipelineStage.Checksum }));
                Assert.That(Stage(file, "download").State, Is.EqualTo(MirrorStageState.Complete));
                Assert.That(Stage(file, "checksum").State, Is.EqualTo(MirrorStageState.Complete));
                Assert.That(Stage(file, "extract").State, Is.EqualTo(MirrorStageState.NotStarted));
                Assert.That(Stage(file, "parse").State, Is.EqualTo(MirrorStageState.NotStarted));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CancellationBeforePipelineStartDoesNotMutateManifestOrCreateLock()
    {
        string directory = CreateDirectory();
        try
        {
            SaveManifest(directory, Array.Empty<DatasetManifestStage>());
            string before = File.ReadAllText(DatasetManifestStore.GetPath(directory));
            RecordingMirrorStageExecutor executor = new RecordingMirrorStageExecutor();
            MirrorStagePipelineRequest request = Request(directory);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            request.Context.CancellationToken = cancellation.Token;

            Assert.Throws<OperationCanceledException>(() => new MirrorStagePipeline(executor).Run(request));

            Assert.Multiple(() =>
            {
                Assert.That(executor.Calls, Is.Empty);
                Assert.That(File.ReadAllText(DatasetManifestStore.GetPath(directory)), Is.EqualTo(before));
                Assert.That(File.Exists(Path.Combine(directory, MirrorStateCoordinator.LockFileName)), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CancellationBetweenMirrorStagesPreservesCompletedWorkAndReleasesTheLock()
    {
        string directory = CreateDirectory();
        try
        {
            SaveManifest(directory, Array.Empty<DatasetManifestStage>());
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            RecordingMirrorStageExecutor executor = new RecordingMirrorStageExecutor
            {
                Cancellation = cancellation,
                CancelAfterStage = MirrorPipelineStage.Download
            };
            MirrorStagePipelineRequest request = Request(directory);
            request.Context.CancellationToken = cancellation.Token;

            OperationCanceledException failure = Assert.Throws<OperationCanceledException>(() => new MirrorStagePipeline(executor).Run(request));
            DatasetManifestFile file = DatasetManifestStore.Load(directory).Products[0].Files[0];

            Assert.Multiple(() =>
            {
                Assert.That(failure.CancellationToken, Is.EqualTo(cancellation.Token));
                Assert.That(executor.Calls, Is.EqualTo(new[] { MirrorPipelineStage.Download }));
                Assert.That(Stage(file, "download").State, Is.EqualTo(MirrorStageState.Complete));
                Assert.That(Stage(file, "checksum").State, Is.EqualTo(MirrorStageState.NotStarted));
                Assert.That(File.Exists(Path.Combine(directory, MirrorStateCoordinator.LockFileName)), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ConfiguredMirrorExecutorPropagatesCancelledExtractionState()
    {
        string directory = CreateDirectory();
        string archivePath = Path.Combine(directory, "data.zip");
        using HttpClient client = new HttpClient();
        ConfiguredClientComponents components = new ConfiguredClientComponents(new UsptoClientOptions { HttpClient = client });
        try
        {
            using (FileStream output = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, false))
            {
                for (int index = 0; index < 1000; index++)
                {
                    ZipArchiveEntry entry = archive.CreateEntry("data-" + index.ToString("D4") + ".txt", CompressionLevel.NoCompression);
                    using Stream content = entry.Open();
                    content.Write(new byte[1024]);
                }
            }

            using CancellationTokenSource cancellation = new CancellationTokenSource();
            MirrorPipelineStageRequest request = new MirrorPipelineStageRequest
            {
                Stage = MirrorPipelineStage.Extraction,
                Family = DatasetFamily.OfficeAction,
                MirrorDirectory = directory,
                LocalPath = archivePath,
                ManifestFile = new DatasetManifestFile { ProductIdentifier = "PRODUCT", FileName = "data.zip" },
                Context = new RequestContext { CancellationToken = cancellation.Token }
            };

            ConfiguredMirrorPipelineStageExecutor executor = new ConfiguredMirrorPipelineStageExecutor(components);
            Task<MirrorPipelineStageExecutionResult> execution = Task.Run(() => executor.ExecuteMirrorPipelineStage(request));
            string extractionDestination = Path.Combine(directory, "extracted", "PRODUCT", "data.zip");
            Assert.That(SpinWait.SpinUntil(() => Directory.Exists(extractionDestination), 5000), Is.True);
            Assert.That(execution.IsCompleted, Is.False);
            cancellation.Cancel();
            MirrorPipelineStageExecutionResult result = execution.GetAwaiter().GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.IsCancelled, Is.True);
                Assert.That(result.FailureCode, Is.EqualTo("CANCELLED"));
            });
        }
        finally
        {
            components.Lifetime.Dispose();
            Directory.Delete(directory, true);
        }
    }

    private static MirrorStagePipelineRequest Request(string directory)
    {
        return new MirrorStagePipelineRequest
        {
            MirrorDirectory = directory,
            Family = DatasetFamily.OfficeAction,
            ProductIdentifier = "PRODUCT",
            FileName = "data.zip",
            LastStage = MirrorPipelineStage.Parse,
            StaleLockSeconds = 3600,
            Now = DateTimeOffset.UtcNow,
            Context = new RequestContext(),
            Secrets = Array.Empty<string>()
        };
    }

    private static void SaveManifest(string directory, DatasetManifestStage[] stages)
    {
        PatentDataProvenance provenance = new PatentDataProvenance { Source = DataProvenance.LocalMirror, IsAuthoritative = false };
        DatasetManifest manifest = new DatasetManifest
        {
            ManifestIdentifier = "pipeline",
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
                            FileName = "data.zip",
                            LocalPath = "files/data.zip",
                            PartialPath = "files/data.zip.partial",
                            Stages = stages,
                            Provenance = provenance
                        }
                    }
                }
            }
        };
        DatasetManifestStore.Save(directory, manifest, Array.Empty<string>());
    }

    private static DatasetManifestStage ManifestStage(string name, MirrorStageState state)
    {
        return new DatasetManifestStage { Name = name, State = state };
    }

    private static DatasetManifestStage Stage(DatasetManifestFile file, string name)
    {
        return file.Stages.Single(stage => stage.Name == name);
    }

    private static string CreateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-mirror-pipeline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

internal class RecordingMirrorStageExecutor : IMirrorPipelineStageExecutor
{
    internal List<MirrorPipelineStage> Calls = new List<MirrorPipelineStage>();
    internal byte[] Content = Encoding.UTF8.GetBytes("archive bytes");
    internal MirrorPipelineStage FailureStage = MirrorPipelineStage.Download;
    internal bool HasFailureStage = false;
    internal int FailuresRemaining = 0;
    internal string FailureMessage = "stage failed";
    internal CancellationTokenSource? Cancellation = null;
    internal MirrorPipelineStage CancelAfterStage = MirrorPipelineStage.Parse;

    internal void ConfigureFailure(MirrorPipelineStage stage, int count, string message)
    {
        FailureStage = stage;
        HasFailureStage = true;
        FailuresRemaining = count;
        FailureMessage = message;
    }

    public MirrorPipelineStageExecutionResult ExecuteMirrorPipelineStage(MirrorPipelineStageRequest request)
    {
        Calls.Add(request.Stage);
        if (HasFailureStage && request.Stage == FailureStage && FailuresRemaining > 0)
        {
            FailuresRemaining -= 1;
            return new MirrorPipelineStageExecutionResult
            {
                FailureCode = request.Stage == MirrorPipelineStage.Checksum ? "CHECKSUM_FAILED" : "PARSE_FAILED",
                FailureMessage = FailureMessage
            };
        }

        MirrorPipelineStageExecutionResult result = new MirrorPipelineStageExecutionResult { IsComplete = true };
        if (request.Stage == MirrorPipelineStage.Download)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(request.LocalPath)!);
            File.WriteAllBytes(request.LocalPath, Content);
            result.ActualSize = Content.Length;
            result.HasActualSize = true;
        }
        else if (request.Stage == MirrorPipelineStage.Checksum)
        {
            result.DigestAlgorithm = ChecksumAlgorithm.Sha256;
            result.Digest = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(request.LocalPath))).ToLowerInvariant();
            result.HasDigest = true;
        }
        else if (request.Stage == MirrorPipelineStage.Extraction)
        {
            string relativePath = "extracted/data.txt";
            string path = Path.Combine(request.MirrorDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "parsed input");
            result.ExtractedPaths = new[] { relativePath };
            result.HasExtractedPaths = true;
        }

        if (Cancellation != null && request.Stage == CancelAfterStage)
        {
            Cancellation.Cancel();
        }

        return result;
    }
}
