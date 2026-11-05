using uspto;

namespace uspto.Tests;

public class MirrorStateCoordinatorTests
{
    [Test]
    public void ActiveLockIsExclusiveAndOnlyItsOwnerCanReleaseIt()
    {
        string directory = CreateDirectory();
        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            MirrorLockHandle owner = MirrorStateCoordinator.Acquire(CreateLockRequest(directory, now));
            MirrorLockHandle contender = MirrorStateCoordinator.Acquire(CreateLockRequest(directory, now.AddSeconds(1)));

            Assert.Multiple(() =>
            {
                Assert.That(owner.IsAcquired, Is.True);
                Assert.That(contender.IsAcquired, Is.False);
                Assert.That(MirrorStateCoordinator.Release(contender), Is.False);
                Assert.That(File.Exists(owner.LockPath), Is.True);
                Assert.That(MirrorStateCoordinator.Release(owner), Is.True);
                Assert.That(File.Exists(owner.LockPath), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ExpiredLockIsRecoveredWithoutAllowingTheOldOwnerToDeleteTheNewLock()
    {
        string directory = CreateDirectory();
        try
        {
            DateTimeOffset oldTime = DateTimeOffset.UtcNow.AddHours(-2);
            MirrorLockHandle oldOwner = MirrorStateCoordinator.Acquire(CreateLockRequest(directory, oldTime));
            MirrorLockHandle recovered = MirrorStateCoordinator.Acquire(CreateLockRequest(directory, oldTime.AddHours(2)));

            Assert.Multiple(() =>
            {
                Assert.That(recovered.IsAcquired, Is.True);
                Assert.That(recovered.RecoveredStaleLock, Is.True);
                Assert.That(recovered.OwnerToken, Is.Not.EqualTo(oldOwner.OwnerToken));
                Assert.That(MirrorStateCoordinator.Release(oldOwner), Is.False);
                Assert.That(File.Exists(recovered.LockPath), Is.True);
                Assert.That(MirrorStateCoordinator.Release(recovered), Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void WorkDirectoriesAreUniqueContainedAndOwnershipProtected()
    {
        string directory = CreateDirectory();
        try
        {
            MirrorLockHandle owner = MirrorStateCoordinator.Acquire(CreateLockRequest(directory, DateTimeOffset.UtcNow));
            string first = MirrorStateCoordinator.CreateWorkDirectory(owner, "download/archive");
            string second = MirrorStateCoordinator.CreateWorkDirectory(owner, "download/archive");
            ValidationException outside = Assert.Throws<ValidationException>(() => MirrorStateCoordinator.DeleteWorkDirectory(owner, directory))!;

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.Not.EqualTo(second));
                Assert.That(first, Does.StartWith(Path.Combine(directory, MirrorStateCoordinator.WorkDirectoryName) + Path.DirectorySeparatorChar));
                Assert.That(Directory.Exists(first), Is.True);
                Assert.That(Directory.Exists(second), Is.True);
                Assert.That(outside.ErrorCode, Is.EqualTo("WORK_PATH_INVALID"));
                Assert.That(MirrorStateCoordinator.DeleteWorkDirectory(owner, first), Is.True);
                Assert.That(Directory.Exists(first), Is.False);
            });

            MirrorStateCoordinator.Release(owner);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void StageTransitionsAreValidatedRedactedAndAtomicallyPersisted()
    {
        string directory = CreateDirectory();
        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DatasetManifestStore.Save(directory, CreateManifest(), Array.Empty<string>());
            MirrorLockHandle owner = MirrorStateCoordinator.Acquire(CreateLockRequest(directory, now));

            MirrorStateCoordinator.TransitionStage(CreateTransition(owner, MirrorStageState.InProgress, now, null, null));
            MirrorStateCoordinator.TransitionStage(CreateTransition(owner, MirrorStageState.Failed, now.AddMinutes(1), "NETWORK", "token was secret-value"));
            DatasetManifest failed = DatasetManifestStore.Load(directory);
            DatasetManifestStage failedStage = failed.Products[0].Files[0].Stages[0];
            MirrorStateCoordinator.TransitionStage(CreateTransition(owner, MirrorStageState.InProgress, now.AddMinutes(2), null, null));
            DatasetManifest completed = MirrorStateCoordinator.TransitionStage(CreateTransition(owner, MirrorStageState.Complete, now.AddMinutes(3), null, null));

            Assert.Multiple(() =>
            {
                Assert.That(failedStage.State, Is.EqualTo(MirrorStageState.Failed));
                Assert.That(failedStage.FailureCode, Is.EqualTo("NETWORK"));
                Assert.That(failedStage.FailureMessage, Is.EqualTo("token was [REDACTED]"));
                Assert.That(completed.Products[0].Files[0].Stages[0].State, Is.EqualTo(MirrorStageState.Complete));
                Assert.That(completed.Products[0].Files[0].Stages[0].FailureCode, Is.Null);
                Assert.That(completed.UpdatedAt, Is.EqualTo(now.AddMinutes(3)));
                Assert.That(File.Exists(DatasetManifestStore.GetPath(directory) + ".tmp"), Is.False);
            });

            MirrorStateCoordinator.Release(owner);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CancellationStopsLockAcquisitionBeforeFilesystemMutation()
    {
        string directory = CreateDirectory();
        try
        {
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            MirrorLockRequest request = CreateLockRequest(directory, DateTimeOffset.UtcNow);
            request.CancellationToken = cancellation.Token;

            Assert.Throws<OperationCanceledException>(() => MirrorStateCoordinator.Acquire(request));
            Assert.That(File.Exists(Path.Combine(directory, MirrorStateCoordinator.LockFileName)), Is.False);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static MirrorLockRequest CreateLockRequest(string directory, DateTimeOffset now)
    {
        return new MirrorLockRequest { MirrorDirectory = directory, Now = now, StaleAfterSeconds = 3600 };
    }

    private static MirrorStageTransitionRequest CreateTransition(MirrorLockHandle owner, MirrorStageState state, DateTimeOffset now, string? code, string? message)
    {
        return new MirrorStageTransitionRequest
        {
            Lock = owner,
            ProductIdentifier = "PV-GRANT",
            FileName = "g_patent.tsv.gz",
            StageName = "download",
            State = state,
            UpdatedAt = now,
            FailureCode = code,
            FailureMessage = message,
            Secrets = new[] { "secret-value" }
        };
    }

    private static DatasetManifest CreateManifest()
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
                    ProductIdentifier = "PV-GRANT",
                    Family = DatasetFamily.PatentsViewGrant,
                    Provenance = provenance,
                    Files = new[]
                    {
                        new DatasetManifestFile
                        {
                            ProductIdentifier = "PV-GRANT",
                            FileName = "g_patent.tsv.gz",
                            LocalPath = "downloads/g_patent.tsv.gz",
                            Provenance = provenance,
                            Stages = new[] { new DatasetManifestStage { Name = "download" } }
                        }
                    }
                }
            }
        };
    }

    private static string CreateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-mirror-state-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
