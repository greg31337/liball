namespace ftp.Tests;

public class PublicTransferDataTests
{
    [Test]
    public void Transfer_options_use_binary_non_destructive_defaults()
    {
        FtpTransferOptions first = new FtpTransferOptions();
        FtpTransferOptions second = new FtpTransferOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.Type, Is.EqualTo(FtpTransferType.Image));
            Assert.That(first.TextFormat, Is.EqualTo(FtpTextFormat.NonPrint));
            Assert.That(first.Structure, Is.EqualTo(FtpFileStructure.File));
            Assert.That(first.Mode, Is.EqualTo(FtpTransferMode.Stream));
            Assert.That(first.HasLocalByteSize, Is.False);
            Assert.That(first.LocalByteSize, Is.Zero);
            Assert.That(first.CollisionPolicy, Is.EqualTo(FtpCollisionPolicy.Fail));
            Assert.That(first.ResumePolicy, Is.EqualTo(FtpResumePolicy.Disabled));
            Assert.That(first.VerificationMode, Is.EqualTo(FtpVerificationMode.Size));
            Assert.That(first.Timeouts, Is.Not.SameAs(second.Timeouts));
            Assert.That(first.CreateParentDirectories, Is.False);
            Assert.That(first.AtomicUpload, Is.False);
            Assert.That(first.CleanupPartialOnFailure, Is.True);
            Assert.That(first.PreserveModifiedTime, Is.False);
            Assert.That(first.EnableKeepAlive, Is.False);
            Assert.That(first.KeepAliveIntervalMilliseconds,
                Is.EqualTo(FtpDefaults.TransferKeepAliveIntervalMilliseconds));
            Assert.That(first.BufferBytes, Is.EqualTo(FtpDefaults.TransferBufferBytes));
            Assert.That(first.ExpectedChecksumAlgorithm, Is.Null);
            Assert.That(first.ExpectedChecksum, Is.Null);
        });
    }

    [Test]
    public void Transfer_request_is_an_unbound_download_with_explicit_availability()
    {
        FtpTransferRequest first = new FtpTransferRequest();
        FtpTransferRequest second = new FtpTransferRequest();

        Assert.Multiple(() =>
        {
            Assert.That(first.Direction, Is.EqualTo(FtpTransferDirection.Download));
            Assert.That(first.RemotePath, Is.Empty);
            Assert.That(first.LocalPath, Is.Null);
            Assert.That(first.Content, Is.Null);
            Assert.That(first.LeaveContentOpen, Is.True);
            Assert.That(first.HasRestartOffset, Is.False);
            Assert.That(first.RestartOffset, Is.Zero);
            Assert.That(first.HasExpectedSize, Is.False);
            Assert.That(first.ExpectedSize, Is.Zero);
            Assert.That(first.Options, Is.Not.SameAs(second.Options));
            Assert.That(first.Observer, Is.Null);
        });
    }

    [Test]
    public void Transfer_progress_starts_at_zero_without_an_expected_size()
    {
        FtpTransferProgress progress = new FtpTransferProgress();

        Assert.Multiple(() =>
        {
            Assert.That(progress.Direction, Is.EqualTo(FtpTransferDirection.Download));
            Assert.That(progress.Path, Is.Null);
            Assert.That(progress.HasExpectedBytes, Is.False);
            Assert.That(progress.ExpectedBytes, Is.Zero);
            Assert.That(progress.LogicalBytes, Is.Zero);
            Assert.That(progress.WireBytes, Is.Zero);
            Assert.That(progress.Elapsed, Is.EqualTo(TimeSpan.Zero));
            Assert.That(progress.Completed, Is.False);
        });
    }

    [Test]
    public void Transfer_result_does_not_imply_completion_or_verification()
    {
        FtpTransferResult first = new FtpTransferResult();
        FtpTransferResult second = new FtpTransferResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Request, Is.Not.SameAs(second.Request));
            Assert.That(first.Progress, Is.Not.SameAs(second.Progress));
            Assert.That(first.Commands, Is.Empty);
            Assert.That(first.Commands, Is.Not.SameAs(second.Commands));
            Assert.That(first.Succeeded, Is.False);
            Assert.That(first.VerificationAttempted, Is.False);
            Assert.That(first.Verified, Is.False);
            Assert.That(first.VerificationMode, Is.EqualTo(FtpVerificationMode.None));
            Assert.That(first.HasActualSize, Is.False);
            Assert.That(first.ActualSize, Is.Zero);
            Assert.That(first.ChecksumAlgorithm, Is.Null);
            Assert.That(first.ExpectedChecksum, Is.Null);
            Assert.That(first.ActualChecksum, Is.Null);
            Assert.That(first.RestartMarker, Is.Null);
            Assert.That(first.UniqueRemoteName, Is.Null);
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Directory_options_disable_destructive_and_link_behavior()
    {
        FtpDirectoryTransferOptions first = new FtpDirectoryTransferOptions();
        FtpDirectoryTransferOptions second = new FtpDirectoryTransferOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.Transfer, Is.Not.SameAs(second.Transfer));
            Assert.That(first.SyncMode, Is.EqualTo(FtpDirectorySyncMode.Copy));
            Assert.That(first.SymlinkPolicy, Is.EqualTo(FtpSymlinkPolicy.Skip));
            Assert.That(first.FailurePolicy, Is.EqualTo(FtpFailurePolicy.Stop));
            Assert.That(first.Filter, Is.Null);
            Assert.That(first.DryRun, Is.False);
            Assert.That(first.DeleteExtraneous, Is.False);
            Assert.That(first.AllowRecursiveDeletion, Is.False);
            Assert.That(first.PreserveEmptyDirectories, Is.True);
            Assert.That(first.MaximumDepth, Is.EqualTo(128));
            Assert.That(first.MaximumEntryCount, Is.EqualTo(FtpDefaults.MaximumListingEntryCount));
        });
    }

    [Test]
    public void Directory_request_is_an_unbound_download()
    {
        FtpDirectoryTransferRequest first = new FtpDirectoryTransferRequest();
        FtpDirectoryTransferRequest second = new FtpDirectoryTransferRequest();

        Assert.Multiple(() =>
        {
            Assert.That(first.Direction, Is.EqualTo(FtpTransferDirection.Download));
            Assert.That(first.LocalPath, Is.Empty);
            Assert.That(first.RemotePath, Is.Empty);
            Assert.That(first.Options, Is.Not.SameAs(second.Options));
        });
    }

    [Test]
    public void Directory_item_starts_unplanned_and_unexecuted()
    {
        FtpDirectoryItemResult item = new FtpDirectoryItemResult();

        Assert.Multiple(() =>
        {
            Assert.That(item.LocalPath, Is.Empty);
            Assert.That(item.RemotePath, Is.Empty);
            Assert.That(item.Direction, Is.EqualTo(FtpTransferDirection.Download));
            Assert.That(item.EntryType, Is.EqualTo(FtpEntryType.Unknown));
            Assert.That(item.Planned, Is.False);
            Assert.That(item.Executed, Is.False);
            Assert.That(item.Succeeded, Is.False);
            Assert.That(item.Skipped, Is.False);
            Assert.That(item.Deleted, Is.False);
            Assert.That(item.TransferResult, Is.Null);
            Assert.That(item.CommandResult, Is.Null);
            Assert.That(item.Message, Is.Null);
        });
    }

    [Test]
    public void Directory_plan_owns_an_empty_deterministic_item_collection()
    {
        FtpDirectoryPlan first = new FtpDirectoryPlan();
        FtpDirectoryPlan second = new FtpDirectoryPlan();

        Assert.Multiple(() =>
        {
            Assert.That(first.Request, Is.Not.SameAs(second.Request));
            Assert.That(first.Items, Is.Empty);
            Assert.That(first.Items, Is.Not.SameAs(second.Items));
            Assert.That(first.Complete, Is.False);
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Directory_result_starts_empty_and_non_successful()
    {
        FtpDirectoryTransferResult first = new FtpDirectoryTransferResult();
        FtpDirectoryTransferResult second = new FtpDirectoryTransferResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Request, Is.Not.SameAs(second.Request));
            Assert.That(first.Plan, Is.Not.SameAs(second.Plan));
            Assert.That(first.Items, Is.Empty);
            Assert.That(first.Items, Is.Not.SameAs(second.Items));
            Assert.That(first.Succeeded, Is.False);
            Assert.That(first.TotalItems, Is.Zero);
            Assert.That(first.SucceededItems, Is.Zero);
            Assert.That(first.FailedItems, Is.Zero);
            Assert.That(first.SkippedItems, Is.Zero);
            Assert.That(first.DeletedItems, Is.Zero);
            Assert.That(first.LogicalBytes, Is.Zero);
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Server_transfer_request_requires_explicit_sessions_and_paths()
    {
        FtpServerTransferRequest first = new FtpServerTransferRequest();
        FtpServerTransferRequest second = new FtpServerTransferRequest();

        Assert.Multiple(() =>
        {
            Assert.That(first.SourceClient, Is.Null);
            Assert.That(first.TargetClient, Is.Null);
            Assert.That(first.SourcePath, Is.Empty);
            Assert.That(first.TargetPath, Is.Empty);
            Assert.That(first.Append, Is.False);
            Assert.That(first.Unique, Is.False);
            Assert.That(first.Options, Is.Not.SameAs(second.Options));
        });
    }

    [Test]
    public void Server_transfer_result_requires_both_sessions_to_complete()
    {
        FtpServerTransferResult first = new FtpServerTransferResult();
        FtpServerTransferResult second = new FtpServerTransferResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Request, Is.Not.SameAs(second.Request));
            Assert.That(first.SourceCommands, Is.Empty);
            Assert.That(first.SourceCommands, Is.Not.SameAs(second.SourceCommands));
            Assert.That(first.TargetCommands, Is.Empty);
            Assert.That(first.TargetCommands, Is.Not.SameAs(second.TargetCommands));
            Assert.That(first.Succeeded, Is.False);
            Assert.That(first.HasLogicalBytes, Is.False);
            Assert.That(first.LogicalBytes, Is.Zero);
            Assert.That(first.UniqueTargetName, Is.Null);
            Assert.That(first.FailureMessage, Is.Null);
        });
    }
}
