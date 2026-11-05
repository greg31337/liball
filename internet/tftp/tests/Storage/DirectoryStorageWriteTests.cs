using System.Threading;
using tftp;

namespace tftp.Tests;

public class DirectoryStorageWriteTests
{
    [Test]
    public void CreatesFileAtomicallyFromSameDirectoryTemporaryContent()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            false);
        TftpServerRequest request = WriteRequest(
            "created.bin",
            TftpTransferMode.Octet,
            true,
            3);

        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            request,
            CancellationToken.None);
        string destinationPath = Path.Combine(
            directory.RootPath,
            "created.bin");
        string temporaryPath = PendingPath(storage, opened.CompletionToken!);

        Assert.Multiple(() =>
        {
            Assert.That(opened.Success, Is.True);
            Assert.That(opened.Content, Is.Not.Null);
            Assert.That(opened.Content!.CanWrite, Is.True);
            Assert.That(opened.OwnsContent, Is.True);
            Assert.That(opened.HasKnownSize, Is.True);
            Assert.That(opened.KnownSize, Is.EqualTo(3));
            Assert.That(opened.CompletionToken, Is.Not.Null);
            Assert.That(File.Exists(destinationPath), Is.False);
            Assert.That(File.Exists(temporaryPath), Is.True);
            Assert.That(Path.GetDirectoryName(temporaryPath),
                Is.EqualTo(directory.RootPath));
            Assert.That(temporaryPath, Does.EndWith(".tftp.tmp"));
        });

        opened.Content.Write(new byte[] { 1, 2, 3 });
        opened.Content.Dispose();
        TftpStorageCommitResult committed = storage.CommitWrite(
            opened.CompletionToken,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(committed.Success, Is.True);
            Assert.That(File.ReadAllBytes(destinationPath),
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(File.Exists(temporaryPath), Is.False);
            Assert.That(storage.PendingWrites, Is.Empty);
        });
    }

    [Test]
    public void OverwritesExistingFileOnlyAtCommit()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string destinationPath = Path.Combine(
            directory.RootPath,
            "existing.bin");
        byte[] original = { 9, 9 };
        File.WriteAllBytes(destinationPath, original);
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            false,
            true);

        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            WriteRequest(
                "existing.bin",
                TftpTransferMode.Octet,
                false,
                0),
            CancellationToken.None);
        opened.Content!.Write(new byte[] { 4, 5, 6 });

        Assert.That(File.ReadAllBytes(destinationPath), Is.EqualTo(original));

        TftpStorageCommitResult committed = storage.CommitWrite(
            opened.CompletionToken!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(committed.Success, Is.True);
            Assert.That(File.ReadAllBytes(destinationPath),
                Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(opened.Content.CanWrite, Is.False);
            Assert.That(TemporaryFiles(directory.RootPath), Is.Empty);
        });
    }

    [Test]
    public void CommitsMatchingNetasciiAnnouncedSize()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            false);
        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            WriteRequest(
                "text.txt",
                TftpTransferMode.Netascii,
                true,
                5),
            CancellationToken.None);
        byte[] decodedContent = { 10, 13, 65 };
        opened.Content!.Write(decodedContent);

        TftpStorageCommitResult committed = storage.CommitWrite(
            opened.CompletionToken!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(committed.Success, Is.True);
            Assert.That(File.ReadAllBytes(
                Path.Combine(directory.RootPath, "text.txt")),
                Is.EqualTo(decodedContent));
        });
    }

    [TestCase(TftpTransferMode.Octet, 3)]
    [TestCase(TftpTransferMode.Netascii, 1)]
    public void RejectsContentThatDoesNotMatchAnnouncedSize(
        TftpTransferMode mode,
        long announcedSize)
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            false);
        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            WriteRequest("mismatch.bin", mode, true, announcedSize),
            CancellationToken.None);
        opened.Content!.Write(new byte[] { 10, 65 });

        TftpStorageCommitResult committed = storage.CommitWrite(
            opened.CompletionToken!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(committed.Success, Is.False);
            Assert.That(committed.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(committed.ErrorMessage, Does.Contain("announced"));
            Assert.That(File.Exists(Path.Combine(
                directory.RootPath,
                "mismatch.bin")), Is.False);
            Assert.That(TemporaryFiles(directory.RootPath), Is.Empty);
            Assert.That(storage.PendingWrites, Is.Empty);
        });
    }

    [Test]
    public void AbortsPendingContentWithoutPublishingIt()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            false);
        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            WriteRequest(
                "aborted.bin",
                TftpTransferMode.Octet,
                false,
                0),
            CancellationToken.None);
        opened.Content!.Write(new byte[] { 1, 2 });
        string temporaryPath = PendingPath(storage, opened.CompletionToken!);

        TftpStorageCommitResult aborted = storage.AbortWrite(
            opened.CompletionToken!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(aborted.Success, Is.True);
            Assert.That(opened.Content.CanWrite, Is.False);
            Assert.That(File.Exists(temporaryPath), Is.False);
            Assert.That(File.Exists(Path.Combine(
                directory.RootPath,
                "aborted.bin")), Is.False);
            Assert.That(storage.PendingWrites, Is.Empty);
        });
        AssertCompletionFailure(
            storage.AbortWrite(opened.CompletionToken!, CancellationToken.None),
            TftpErrorCode.AccessViolation,
            "stale");
    }

    [Test]
    public void EnforcesWriteCreateAndOverwritePolicies()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string existingPath = Path.Combine(directory.RootPath, "existing.bin");
        string directoryPath = Path.Combine(directory.RootPath, "folder");
        File.WriteAllBytes(existingPath, new byte[] { 1 });
        Directory.CreateDirectory(directoryPath);

        TftpStorageOpenResult writeDenied = CreateStorage(
            directory.RootPath,
            false,
            true,
            true).OpenPendingWrite(
                WriteRequest("new.bin", TftpTransferMode.Octet, false, 0),
                CancellationToken.None);
        TftpStorageOpenResult createDenied = CreateStorage(
            directory.RootPath,
            true,
            false,
            true).OpenPendingWrite(
                WriteRequest("new.bin", TftpTransferMode.Octet, false, 0),
                CancellationToken.None);
        TftpStorageOpenResult overwriteDenied = CreateStorage(
            directory.RootPath,
            true,
            true,
            false).OpenPendingWrite(
                WriteRequest("existing.bin", TftpTransferMode.Octet, false, 0),
                CancellationToken.None);
        TftpStorageOpenResult directoryDenied = CreateStorage(
            directory.RootPath,
            true,
            true,
            true).OpenPendingWrite(
                WriteRequest("folder", TftpTransferMode.Octet, false, 0),
                CancellationToken.None);

        AssertOpenFailure(
            writeDenied,
            TftpErrorCode.AccessViolation,
            "not permitted");
        AssertOpenFailure(
            createDenied,
            TftpErrorCode.AccessViolation,
            "Creating");
        AssertOpenFailure(
            overwriteDenied,
            TftpErrorCode.FileAlreadyExists,
            "already exists");
        AssertOpenFailure(
            directoryDenied,
            TftpErrorCode.FileAlreadyExists,
            "directory");
        Assert.That(TemporaryFiles(directory.RootPath), Is.Empty);
    }

    [Test]
    public void RejectsUnsafeAndUnavailableDestinationDirectories()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true);

        TftpStorageOpenResult traversal = storage.OpenPendingWrite(
            WriteRequest(
                "../outside.bin",
                TftpTransferMode.Octet,
                false,
                0),
            CancellationToken.None);
        TftpStorageOpenResult missingParent = storage.OpenPendingWrite(
            WriteRequest(
                "missing/file.bin",
                TftpTransferMode.Octet,
                false,
                0),
            CancellationToken.None);

        AssertOpenFailure(
            traversal,
            TftpErrorCode.AccessViolation,
            "traversal");
        AssertOpenFailure(
            missingParent,
            TftpErrorCode.AccessViolation,
            "does not exist");
    }

    [Test]
    public void RejectsInvalidRequestsAndNegativeAnnouncedSize()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true);
        TftpServerRequest wrongDirection = WriteRequest(
            "file.bin",
            TftpTransferMode.Octet,
            false,
            0);
        wrongDirection.Direction = TftpTransferDirection.Read;
        TftpServerRequest negativeSize = WriteRequest(
            "file.bin",
            TftpTransferMode.Octet,
            true,
            -1);
        TftpServerRequest wrongMode = WriteRequest(
            "file.bin",
            (TftpTransferMode)99,
            false,
            0);

        AssertOpenFailure(
            storage.OpenPendingWrite(null!, CancellationToken.None),
            TftpErrorCode.AccessViolation,
            "cannot be null");
        AssertOpenFailure(
            storage.OpenPendingWrite(wrongDirection, CancellationToken.None),
            TftpErrorCode.AccessViolation,
            "not a write");
        AssertOpenFailure(
            storage.OpenPendingWrite(negativeSize, CancellationToken.None),
            TftpErrorCode.IllegalOperation,
            "negative");
        AssertOpenFailure(
            storage.OpenPendingWrite(wrongMode, CancellationToken.None),
            TftpErrorCode.IllegalOperation,
            "mode");
    }

    [Test]
    public void TwoPendingCreatesUseUniqueFilesAndResolveCommitRace()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            false);
        TftpServerRequest request = WriteRequest(
            "raced.bin",
            TftpTransferMode.Octet,
            false,
            0);
        TftpStorageOpenResult first = storage.OpenPendingWrite(
            request,
            CancellationToken.None);
        TftpStorageOpenResult second = storage.OpenPendingWrite(
            request,
            CancellationToken.None);
        string firstTemporary = PendingPath(storage, first.CompletionToken!);
        string secondTemporary = PendingPath(storage, second.CompletionToken!);
        first.Content!.Write(new byte[] { 1 });
        second.Content!.Write(new byte[] { 2 });

        TftpStorageCommitResult firstCommit = storage.CommitWrite(
            first.CompletionToken!,
            CancellationToken.None);
        TftpStorageCommitResult secondCommit = storage.CommitWrite(
            second.CompletionToken!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(firstTemporary, Is.Not.EqualTo(secondTemporary));
            Assert.That(firstCommit.Success, Is.True);
            Assert.That(secondCommit.Success, Is.False);
            Assert.That(secondCommit.ErrorCode,
                Is.EqualTo(TftpErrorCode.FileAlreadyExists));
            Assert.That(File.ReadAllBytes(Path.Combine(
                directory.RootPath,
                "raced.bin")), Is.EqualTo(new byte[] { 1 }));
            Assert.That(TemporaryFiles(directory.RootPath), Is.Empty);
            Assert.That(storage.PendingWrites, Is.Empty);
        });
    }

    [Test]
    public void RechecksCreatePolicyWhenExistingDestinationDisappears()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string destinationPath = Path.Combine(
            directory.RootPath,
            "existing.bin");
        File.WriteAllBytes(destinationPath, new byte[] { 8 });
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            false,
            true);
        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            WriteRequest(
                "existing.bin",
                TftpTransferMode.Octet,
                false,
                0),
            CancellationToken.None);
        opened.Content!.Write(new byte[] { 3 });
        File.Delete(destinationPath);

        TftpStorageCommitResult committed = storage.CommitWrite(
            opened.CompletionToken!,
            CancellationToken.None);

        AssertCompletionFailure(
            committed,
            TftpErrorCode.AccessViolation,
            "removed");
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(destinationPath), Is.False);
            Assert.That(TemporaryFiles(directory.RootPath), Is.Empty);
        });
    }

    [Test]
    public void RechecksTargetTypeBeforeCommit()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string destinationPath = Path.Combine(directory.RootPath, "target");
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true);
        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            WriteRequest(
                "target",
                TftpTransferMode.Octet,
                false,
                0),
            CancellationToken.None);
        opened.Content!.WriteByte(1);
        Directory.CreateDirectory(destinationPath);

        TftpStorageCommitResult committed = storage.CommitWrite(
            opened.CompletionToken!,
            CancellationToken.None);

        AssertCompletionFailure(
            committed,
            TftpErrorCode.FileAlreadyExists,
            "directory");
        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(destinationPath), Is.True);
            Assert.That(TemporaryFiles(directory.RootPath), Is.Empty);
        });
    }

    [Test]
    public void CancellationLeavesPendingWriteAbortable()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            false);
        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            WriteRequest(
                "cancelled.bin",
                TftpTransferMode.Octet,
                false,
                0),
            CancellationToken.None);
        opened.Content!.WriteByte(1);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            storage.CommitWrite(opened.CompletionToken!, cancellation.Token));
        Assert.Multiple(() =>
        {
            Assert.That(storage.PendingWrites, Has.Count.EqualTo(1));
            Assert.That(TemporaryFiles(directory.RootPath), Has.Length.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(
                directory.RootPath,
                "cancelled.bin")), Is.False);
        });

        TftpStorageCommitResult aborted = storage.AbortWrite(
            opened.CompletionToken!,
            CancellationToken.None);
        Assert.That(aborted.Success, Is.True);
        Assert.That(TemporaryFiles(directory.RootPath), Is.Empty);
    }

    [Test]
    public void CancellationBeforeOpenCreatesNoTemporaryContent()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            false);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            storage.OpenPendingWrite(
                WriteRequest(
                    "cancelled.bin",
                    TftpTransferMode.Octet,
                    false,
                    0),
                cancellation.Token));
        Assert.Multiple(() =>
        {
            Assert.That(TemporaryFiles(directory.RootPath), Is.Empty);
            Assert.That(storage.PendingWrites, Is.Empty);
        });
    }

    [Test]
    public void InvalidAndStaleCompletionTokensReturnFailures()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true);

        AssertCompletionFailure(
            storage.CommitWrite(null!, CancellationToken.None),
            TftpErrorCode.AccessViolation,
            "invalid");
        AssertCompletionFailure(
            storage.CommitWrite(new object(), CancellationToken.None),
            TftpErrorCode.AccessViolation,
            "stale");
        AssertCompletionFailure(
            storage.AbortWrite(null!, CancellationToken.None),
            TftpErrorCode.AccessViolation,
            "invalid");
        AssertCompletionFailure(
            storage.AbortWrite(new object(), CancellationToken.None),
            TftpErrorCode.AccessViolation,
            "stale");
    }

    [Test]
    public void ConstructorSnapshotsWritePolicies()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpDirectoryStorageOptions options = new TftpDirectoryStorageOptions();
        options.RootPath = directory.RootPath;
        options.AllowWrite = true;
        options.AllowCreate = true;
        options.AllowOverwrite = false;
        TftpDirectoryStorage storage = new TftpDirectoryStorage(options);
        options.AllowWrite = false;
        options.AllowCreate = false;
        options.AllowOverwrite = true;

        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            WriteRequest(
                "created.bin",
                TftpTransferMode.Octet,
                false,
                0),
            CancellationToken.None);

        Assert.That(opened.Success, Is.True);
        storage.AbortWrite(opened.CompletionToken!, CancellationToken.None);
    }

    private static TftpDirectoryStorage CreateStorage(
        string rootPath,
        bool allowWrite,
        bool allowCreate,
        bool allowOverwrite)
    {
        TftpDirectoryStorageOptions options = new TftpDirectoryStorageOptions();
        options.RootPath = rootPath;
        options.AllowWrite = allowWrite;
        options.AllowCreate = allowCreate;
        options.AllowOverwrite = allowOverwrite;
        return new TftpDirectoryStorage(options);
    }

    private static TftpServerRequest WriteRequest(
        string filename,
        TftpTransferMode mode,
        bool hasAnnouncedSize,
        long announcedSize)
    {
        TftpServerRequest request = new TftpServerRequest();
        request.Filename = filename;
        request.Direction = TftpTransferDirection.Write;
        request.Mode = mode;
        request.HasAnnouncedSize = hasAnnouncedSize;
        request.AnnouncedSize = announcedSize;
        return request;
    }

    private static string PendingPath(
        TftpDirectoryStorage storage,
        object completionToken)
    {
        return storage.PendingWrites[completionToken].TemporaryPath;
    }

    private static string[] TemporaryFiles(string rootPath)
    {
        return Directory.GetFiles(rootPath, "*.tftp.tmp");
    }

    private static void AssertOpenFailure(
        TftpStorageOpenResult result,
        TftpErrorCode errorCode,
        string messageFragment)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.ErrorMessage,
                Does.Contain(messageFragment).IgnoreCase);
            Assert.That(result.Content, Is.Null);
            Assert.That(result.CompletionToken, Is.Null);
        });
    }

    private static void AssertCompletionFailure(
        TftpStorageCommitResult result,
        TftpErrorCode errorCode,
        string messageFragment)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.ErrorMessage,
                Does.Contain(messageFragment).IgnoreCase);
        });
    }
}

internal class DirectoryStorageWriteTestDirectory : IDisposable
{
    internal string BasePath;
    internal string RootPath;

    internal DirectoryStorageWriteTestDirectory()
    {
        BasePath = Path.Combine(
            Path.GetTempPath(),
            "tftp-directory-write-tests-" + Guid.NewGuid().ToString("N"));
        RootPath = Path.Combine(BasePath, "root");
        Directory.CreateDirectory(RootPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(BasePath))
        {
            Directory.Delete(BasePath, true);
        }
    }
}
