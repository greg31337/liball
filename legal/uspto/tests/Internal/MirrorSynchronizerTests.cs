using System.Collections.Concurrent;
using System.Text;
using uspto;

namespace uspto.Tests;

public class MirrorSynchronizerTests
{
    [Test]
    public void SynchronizeBoundsParallelTransfersAndAtomicallyCompletesManifestStages()
    {
        string directory = CreateDirectory();
        try
        {
            RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(30);
            MirrorPlanEntry[] entries = NewEntries(6, transfer);
            MirrorSynchronizer synchronizer = new MirrorSynchronizer(transfer);
            MirrorSynchronizationRequest request = Request(directory, entries, EmptyManifest(), 2);

            MirrorSynchronizationResult result = synchronizer.Synchronize(request);
            DatasetManifest persisted = DatasetManifestStore.Load(directory);

            Assert.Multiple(() =>
            {
                Assert.That(result.LockAcquired, Is.True);
                Assert.That(result.TransferredCount, Is.EqualTo(6));
                Assert.That(result.FailedCount, Is.Zero);
                Assert.That(transfer.CallCount, Is.EqualTo(6));
                Assert.That(transfer.MaximumConcurrent, Is.EqualTo(2));
                Assert.That(persisted.Products.Single().Files.Length, Is.EqualTo(6));
                Assert.That(persisted.Products[0].Files.Select(file => DownloadStage(file).State), Has.All.EqualTo(MirrorStageState.Complete));
                Assert.That(entries.Select(entry => File.Exists(ExpectedPath(directory, entry.FileName!))), Has.All.True);
                Assert.That(File.Exists(Path.Combine(directory, MirrorStateCoordinator.LockFileName)), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ReentryRecognizesCommittedFilesWithoutRepeatingTransfers()
    {
        string directory = CreateDirectory();
        try
        {
            RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(0);
            MirrorPlanEntry[] entries = NewEntries(3, transfer);
            MirrorSynchronizer synchronizer = new MirrorSynchronizer(transfer);
            MirrorSynchronizationRequest request = Request(directory, entries, EmptyManifest(), 3);

            MirrorSynchronizationResult first = synchronizer.Synchronize(request);
            MirrorSynchronizationResult second = synchronizer.Synchronize(request);

            Assert.Multiple(() =>
            {
                Assert.That(first.TransferredCount, Is.EqualTo(3));
                Assert.That(second.TransferredCount, Is.Zero);
                Assert.That(second.SkippedCount, Is.EqualTo(3));
                Assert.That(second.Entries.Select(entry => entry.SkipReason), Has.All.EqualTo("ALREADY_SYNCHRONIZED"));
                Assert.That(second.Entries.Select(entry => entry.Download.State), Has.All.EqualTo(DownloadState.Completed));
                Assert.That(transfer.CallCount, Is.EqualTo(3));
                Assert.That(DatasetManifestStore.Load(directory).Products[0].Files.Select(file => DownloadStage(file).State), Has.All.EqualTo(MirrorStageState.Complete));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FailedPartialTransferPersistsStateAndResumesOnReentry()
    {
        string directory = CreateDirectory();
        try
        {
            RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(0);
            transfer.FailOnceFile = "file-0.bin";
            MirrorPlanEntry[] entries = NewEntries(1, transfer);
            MirrorSynchronizer synchronizer = new MirrorSynchronizer(transfer);
            MirrorSynchronizationRequest request = Request(directory, entries, EmptyManifest(), 1);

            MirrorSynchronizationResult failed = synchronizer.Synchronize(request);
            DatasetManifestFile failedFile = DatasetManifestStore.Load(directory).Products[0].Files[0];
            MirrorSynchronizationResult resumed = synchronizer.Synchronize(request);
            DatasetManifestFile completedFile = DatasetManifestStore.Load(directory).Products[0].Files[0];

            Assert.Multiple(() =>
            {
                Assert.That(failed.FailedCount, Is.EqualTo(1));
                Assert.That(DownloadStage(failedFile).State, Is.EqualTo(MirrorStageState.Failed));
                Assert.That(DownloadStage(failedFile).FailureCode, Is.EqualTo("INTERRUPTED"));
                Assert.That(resumed.TransferredCount, Is.EqualTo(1));
                Assert.That(transfer.ResumeOffsets, Is.EqualTo(new long[] { 0L, 3L }));
                Assert.That(File.ReadAllBytes(ExpectedPath(directory, "file-0.bin")), Is.EqualTo(transfer.Bodies["file-0.bin"]));
                Assert.That(DownloadStage(completedFile).State, Is.EqualTo(MirrorStageState.Complete));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ChangedTransferPreservesTheSupersededArtifact()
    {
        string directory = CreateDirectory();
        try
        {
            string oldPath = ExpectedPath(directory, "changed.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(oldPath)!);
            File.WriteAllText(oldPath, "old");
            DatasetManifest manifest = ManifestWithFile("changed.bin", 3L, Path.GetRelativePath(directory, oldPath));
            DatasetManifestStore.Save(directory, manifest, Array.Empty<string>());
            RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(0);
            transfer.Bodies["changed.bin"] = Encoding.UTF8.GetBytes("replacement");
            MirrorPlanEntry entry = Entry("changed.bin", MirrorAction.Changed, transfer.Bodies["changed.bin"].Length);
            MirrorSynchronizer synchronizer = new MirrorSynchronizer(transfer);

            MirrorSynchronizationResult result = synchronizer.Synchronize(Request(directory, new[] { entry }, manifest, 1));
            string backupDirectory = Path.Combine(directory, ".superseded", "PRODUCT");

            Assert.Multiple(() =>
            {
                Assert.That(result.TransferredCount, Is.EqualTo(1));
                Assert.That(File.ReadAllText(oldPath), Is.EqualTo("replacement"));
                Assert.That(Directory.GetFiles(backupDirectory), Has.Length.EqualTo(1));
                Assert.That(File.ReadAllText(Directory.GetFiles(backupDirectory).Single()), Is.EqualTo("old"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ActiveMirrorLockPreventsTransfers()
    {
        string directory = CreateDirectory();
        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            MirrorLockHandle owner = MirrorStateCoordinator.Acquire(new MirrorLockRequest { MirrorDirectory = directory, Now = now, StaleAfterSeconds = 3600 });
            RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(0);
            MirrorPlanEntry[] entries = NewEntries(1, transfer);
            MirrorSynchronizer synchronizer = new MirrorSynchronizer(transfer);
            MirrorSynchronizationRequest request = Request(directory, entries, EmptyManifest(), 1);
            request.Now = now.AddSeconds(1);

            MirrorSynchronizationResult result = synchronizer.Synchronize(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.LockAcquired, Is.False);
                Assert.That(result.SkippedCount, Is.EqualTo(1));
                Assert.That(result.Entries[0].SkipReason, Is.EqualTo("MIRROR_LOCK_BUSY"));
                Assert.That(transfer.CallCount, Is.Zero);
                Assert.That(File.Exists(owner.LockPath), Is.True);
            });

            MirrorStateCoordinator.Release(owner);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ConcurrentSynchronizersPermitOnlyOneTransferOwner()
    {
        string directory = CreateDirectory();
        using ManualResetEventSlim transferStarted = new ManualResetEventSlim(false);
        using ManualResetEventSlim continueTransfer = new ManualResetEventSlim(false);
        Task<MirrorSynchronizationResult>? ownerTask = null;
        try
        {
            RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(0);
            transfer.TransferStarted = transferStarted;
            transfer.ContinueTransfer = continueTransfer;
            MirrorPlanEntry[] entries = NewEntries(1, transfer);
            MirrorSynchronizer owner = new MirrorSynchronizer(transfer);
            MirrorSynchronizer contender = new MirrorSynchronizer(transfer);
            ownerTask = Task.Run(() => owner.Synchronize(Request(directory, entries, EmptyManifest(), 1)));

            Assert.That(transferStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);
            MirrorSynchronizationResult blocked = contender.Synchronize(Request(directory, entries, EmptyManifest(), 1));
            continueTransfer.Set();
            MirrorSynchronizationResult completed = ownerTask.GetAwaiter().GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(completed.LockAcquired, Is.True);
                Assert.That(completed.TransferredCount, Is.EqualTo(1));
                Assert.That(blocked.LockAcquired, Is.False);
                Assert.That(blocked.SkippedCount, Is.EqualTo(1));
                Assert.That(blocked.Entries[0].SkipReason, Is.EqualTo("MIRROR_LOCK_BUSY"));
                Assert.That(transfer.CallCount, Is.EqualTo(1));
                Assert.That(File.Exists(ExpectedPath(directory, "file-0.bin")), Is.True);
                Assert.That(File.Exists(Path.Combine(directory, MirrorStateCoordinator.LockFileName)), Is.False);
            });
        }
        finally
        {
            continueTransfer.Set();
            if (ownerTask != null)
            {
                ownerTask.Wait(TimeSpan.FromSeconds(5));
            }

            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FaultedWorkerBecomesAFileFailureAndReleasesTheLock()
    {
        string directory = CreateDirectory();
        try
        {
            RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(0);
            transfer.ThrowFile = "file-0.bin";
            MirrorPlanEntry[] entries = NewEntries(1, transfer);
            MirrorSynchronizer synchronizer = new MirrorSynchronizer(transfer);

            MirrorSynchronizationResult result = synchronizer.Synchronize(Request(directory, entries, EmptyManifest(), 1));

            Assert.Multiple(() =>
            {
                Assert.That(result.FailedCount, Is.EqualTo(1));
                Assert.That(result.Entries[0].Download.FailureCode, Is.EqualTo("MIRROR_TRANSFER_EXCEPTION"));
                Assert.That(DownloadStage(DatasetManifestStore.Load(directory).Products[0].Files[0]).State, Is.EqualTo(MirrorStageState.Failed));
                Assert.That(File.Exists(Path.Combine(directory, MirrorStateCoordinator.LockFileName)), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static MirrorSynchronizationRequest Request(string directory, MirrorPlanEntry[] entries, DatasetManifest manifest, int maximumParallel)
    {
        return new MirrorSynchronizationRequest
        {
            MirrorDirectory = directory,
            Family = DatasetFamily.OfficeAction,
            Entries = entries,
            Manifest = manifest,
            MaximumParallelTransfers = maximumParallel,
            StaleLockSeconds = 3600,
            Now = DateTimeOffset.UtcNow,
            Context = new RequestContext(),
            Secrets = Array.Empty<string>()
        };
    }

    private static MirrorPlanEntry[] NewEntries(int count, RecordingMirrorTransfer transfer)
    {
        MirrorPlanEntry[] result = new MirrorPlanEntry[count];
        for (int index = 0; index < count; index += 1)
        {
            string name = "file-" + index + ".bin";
            byte[] body = Encoding.UTF8.GetBytes("body-" + index);
            transfer.Bodies[name] = body;
            result[index] = Entry(name, MirrorAction.New, body.Length);
        }

        return result;
    }

    private static MirrorPlanEntry Entry(string fileName, MirrorAction action, long size)
    {
        BulkProductFile catalog = new BulkProductFile
        {
            ProductIdentifier = "PRODUCT",
            FileName = fileName,
            DownloadUri = new Uri("https://api.uspto.gov/files/" + fileName),
            Size = size,
            HasSize = true
        };
        return new MirrorPlanEntry
        {
            ProductIdentifier = "PRODUCT",
            FileName = fileName,
            Action = action,
            CatalogFile = catalog
        };
    }

    private static DatasetManifest EmptyManifest()
    {
        return new DatasetManifest
        {
            ManifestIdentifier = "mirror",
            Provenance = new PatentDataProvenance { Source = DataProvenance.LocalMirror, IsAuthoritative = false }
        };
    }

    private static DatasetManifest ManifestWithFile(string fileName, long size, string localPath)
    {
        PatentDataProvenance provenance = new PatentDataProvenance { Source = DataProvenance.LocalMirror, IsAuthoritative = false };
        return new DatasetManifest
        {
            ManifestIdentifier = "mirror",
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
                            LocalPath = localPath,
                            PartialPath = localPath + ".partial",
                            Size = size,
                            HasSize = true,
                            Provenance = provenance
                        }
                    }
                }
            }
        };
    }

    private static DatasetManifestStage DownloadStage(DatasetManifestFile file)
    {
        return file.Stages.Single(stage => stage.Name == "download");
    }

    private static string ExpectedPath(string directory, string fileName)
    {
        return Path.Combine(directory, "files", "PRODUCT", fileName);
    }

    private static string CreateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-mirror-sync-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

internal class RecordingMirrorTransfer : IMirrorFileTransfer
{
    internal ConcurrentDictionary<string, byte[]> Bodies = new ConcurrentDictionary<string, byte[]>(StringComparer.Ordinal);
    internal ConcurrentQueue<long> ResumeOffsets = new ConcurrentQueue<long>();
    internal int DelayMilliseconds;
    internal int CallCount = 0;
    internal int ActiveCount = 0;
    internal int MaximumConcurrent = 0;
    internal string? FailOnceFile = null;
    internal string? ThrowFile = null;
    internal int FailureUsed = 0;
    internal ManualResetEventSlim? TransferStarted = null;
    internal ManualResetEventSlim? ContinueTransfer = null;

    internal RecordingMirrorTransfer(int delayMilliseconds)
    {
        DelayMilliseconds = delayMilliseconds;
    }

    public DownloadResult TransferMirrorFile(DownloadRequest request, DownloadResumeState resumeState)
    {
        string fileName = request.CatalogFileName ?? string.Empty;
        Interlocked.Increment(ref CallCount);
        int active = Interlocked.Increment(ref ActiveCount);
        UpdateMaximum(active);
        try
        {
            if (TransferStarted != null)
            {
                TransferStarted.Set();
            }

            if (ContinueTransfer != null)
            {
                ContinueTransfer.Wait();
            }

            if (DelayMilliseconds > 0)
            {
                Thread.Sleep(DelayMilliseconds);
            }

            ResumeOffsets.Enqueue(resumeState.CompletedBytes);
            if (fileName == ThrowFile)
            {
                throw new IOException("simulated transfer exception");
            }

            if (fileName == FailOnceFile && Interlocked.CompareExchange(ref FailureUsed, 1, 0) == 0)
            {
                File.WriteAllBytes(resumeState.PartialPath!, Encoding.UTF8.GetBytes("par"));
                return new DownloadResult
                {
                    Request = request,
                    State = DownloadState.Failed,
                    FailureCode = "INTERRUPTED",
                    FailureMessage = "The response ended early.",
                    HasResumeState = true,
                    ResumeState = new DownloadResumeState
                    {
                        SourceUri = request.SourceUri,
                        PartialPath = resumeState.PartialPath,
                        CompletedBytes = 3L,
                        ExpectedTotalBytes = request.ExpectedSize,
                        HasExpectedTotalBytes = request.HasExpectedSize,
                        ETag = "\"resume\""
                    }
                };
            }

            byte[] body = Bodies[fileName];
            File.WriteAllBytes(request.DestinationPath!, body);
            if (File.Exists(resumeState.PartialPath))
            {
                File.Delete(resumeState.PartialPath!);
            }

            return new DownloadResult
            {
                Request = request,
                State = DownloadState.Completed,
                FinalPath = request.DestinationPath,
                Content = new DownloadContentMetadata { FileName = fileName, ETag = "\"complete\"" },
                Progress = new DownloadProgress { State = DownloadState.Completed, BytesTransferred = body.Length }
            };
        }
        finally
        {
            Interlocked.Decrement(ref ActiveCount);
        }
    }

    private void UpdateMaximum(int active)
    {
        lock (this)
        {
            if (active > MaximumConcurrent)
            {
                MaximumConcurrent = active;
            }
        }
    }
}
