using System.Threading;
using tftp;

namespace tftp.Tests;

public class DirectoryStorageContractTests
{
    [TestCase(false, false, false, false)]
    [TestCase(false, false, true, false)]
    [TestCase(false, true, false, true)]
    [TestCase(false, true, true, true)]
    [TestCase(true, false, false, false)]
    [TestCase(true, true, false, false)]
    [TestCase(true, false, true, true)]
    [TestCase(true, true, true, true)]
    public void HonorsEveryCreateAndOverwritePolicyCombination(
        bool destinationExists,
        bool allowCreate,
        bool allowOverwrite,
        bool expectedSuccess)
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string destinationPath = Path.Combine(directory.RootPath, "file.bin");
        if (destinationExists)
        {
            File.WriteAllBytes(destinationPath, new byte[] { 9 });
        }

        TftpDirectoryStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            allowCreate,
            allowOverwrite,
            true,
            true);

        TftpStorageOpenResult result = storage.OpenPendingWrite(
            WriteRequest("file.bin", false, 0),
            CancellationToken.None);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
        if (expectedSuccess)
        {
            Assert.That(result.CompletionToken, Is.Not.Null);
            TftpStorageCommitResult aborted = storage.AbortWrite(
                result.CompletionToken!,
                CancellationToken.None);
            Assert.That(aborted.Success, Is.True);
        }
        else
        {
            Assert.That(result.ErrorCode, Is.EqualTo(
                destinationExists
                    ? TftpErrorCode.FileAlreadyExists
                    : TftpErrorCode.AccessViolation));
        }

        Assert.That(Directory.GetFiles(
            directory.RootPath,
            "*.tftp.tmp"), Is.Empty);
    }

    [Test]
    public void AppliesDisabledSubdirectoryPolicyToReadsAndWrites()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string nestedPath = Path.Combine(directory.RootPath, "nested");
        Directory.CreateDirectory(nestedPath);
        File.WriteAllBytes(
            Path.Combine(nestedPath, "content.bin"),
            new byte[] { 1 });
        ITftpServerStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true,
            true,
            false,
            true);

        TftpStorageOpenResult readResult = storage.OpenRead(
            ReadRequest("nested/content.bin"),
            CancellationToken.None);
        TftpStorageOpenResult writeResult = storage.OpenPendingWrite(
            WriteRequest("nested/new.bin", false, 0),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(readResult.Success, Is.False);
            Assert.That(readResult.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(writeResult.Success, Is.False);
            Assert.That(writeResult.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
        });
    }

    [Test]
    public void RejectsSiblingRootPrefixForBothContractDirections()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string siblingRoot = directory.RootPath + "-sibling";
        Directory.CreateDirectory(siblingRoot);
        File.WriteAllBytes(
            Path.Combine(siblingRoot, "secret.bin"),
            new byte[] { 7 });
        ITftpServerStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true,
            true,
            true,
            true);

        TftpStorageOpenResult readResult = storage.OpenRead(
            ReadRequest("../root-sibling/secret.bin"),
            CancellationToken.None);
        TftpStorageOpenResult writeResult = storage.OpenPendingWrite(
            WriteRequest("../root-sibling/new.bin", false, 0),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(readResult.Success, Is.False);
            Assert.That(readResult.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(writeResult.Success, Is.False);
            Assert.That(writeResult.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(File.ReadAllBytes(Path.Combine(
                siblingRoot,
                "secret.bin")), Is.EqualTo(new byte[] { 7 }));
            Assert.That(File.Exists(Path.Combine(
                siblingRoot,
                "new.bin")), Is.False);
        });
    }

    [Test]
    public void RejectsInternalLinksWhenLinkPolicyIsEnabled()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string targetPath = Path.Combine(directory.RootPath, "target");
        string linkPath = Path.Combine(directory.RootPath, "link");
        Directory.CreateDirectory(targetPath);
        File.WriteAllBytes(
            Path.Combine(targetPath, "content.bin"),
            new byte[] { 2 });
        RequireDirectoryLink(linkPath, targetPath);
        ITftpServerStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true,
            true,
            true,
            true);

        TftpStorageOpenResult readResult = storage.OpenRead(
            ReadRequest("link/content.bin"),
            CancellationToken.None);
        TftpStorageOpenResult writeResult = storage.OpenPendingWrite(
            WriteRequest("link/new.bin", false, 0),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(readResult.Success, Is.False);
            Assert.That(readResult.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(writeResult.Success, Is.False);
            Assert.That(writeResult.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
        });
    }

    [Test]
    public void AllowsInternalLinksForBothDirectionsWhenPolicyIsDisabled()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string targetPath = Path.Combine(directory.RootPath, "target");
        string linkPath = Path.Combine(directory.RootPath, "link");
        Directory.CreateDirectory(targetPath);
        File.WriteAllBytes(
            Path.Combine(targetPath, "content.bin"),
            new byte[] { 3 });
        RequireDirectoryLink(linkPath, targetPath);
        ITftpServerStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true,
            true,
            true,
            false);

        TftpStorageOpenResult readResult = storage.OpenRead(
            ReadRequest("link/content.bin"),
            CancellationToken.None);
        using (Stream content = readResult.Content!)
        {
            Assert.That(content.ReadByte(), Is.EqualTo(3));
        }

        TftpStorageOpenResult writeResult = storage.OpenPendingWrite(
            WriteRequest("link/new.bin", false, 0),
            CancellationToken.None);
        writeResult.Content!.Write(new byte[] { 4, 5 });
        TftpStorageCommitResult commitResult = storage.CommitWrite(
            writeResult.CompletionToken!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(readResult.Success, Is.True);
            Assert.That(writeResult.Success, Is.True);
            Assert.That(commitResult.Success, Is.True);
            Assert.That(File.ReadAllBytes(Path.Combine(
                targetPath,
                "new.bin")), Is.EqualTo(new byte[] { 4, 5 }));
        });
    }

    [Test]
    public void RejectsEscapingLinksEvenWhenLinkPolicyIsDisabled()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string outsidePath = Path.Combine(directory.BasePath, "outside");
        string linkPath = Path.Combine(directory.RootPath, "link");
        Directory.CreateDirectory(outsidePath);
        File.WriteAllBytes(
            Path.Combine(outsidePath, "secret.bin"),
            new byte[] { 6 });
        RequireDirectoryLink(linkPath, outsidePath);
        ITftpServerStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true,
            true,
            true,
            false);

        TftpStorageOpenResult readResult = storage.OpenRead(
            ReadRequest("link/secret.bin"),
            CancellationToken.None);
        TftpStorageOpenResult writeResult = storage.OpenPendingWrite(
            WriteRequest("link/new.bin", false, 0),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(readResult.Success, Is.False);
            Assert.That(readResult.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(writeResult.Success, Is.False);
            Assert.That(writeResult.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(File.Exists(Path.Combine(
                outsidePath,
                "new.bin")), Is.False);
        });
    }

    [Test]
    public void RejectsDestinationLinkIntroducedBeforeCommit()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string outsidePath = Path.Combine(directory.BasePath, "outside");
        string outsideFile = Path.Combine(outsidePath, "outside.bin");
        string destinationPath = Path.Combine(directory.RootPath, "file.bin");
        Directory.CreateDirectory(outsidePath);
        File.WriteAllBytes(outsideFile, new byte[] { 9 });
        ITftpServerStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true,
            true,
            true,
            true);
        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            WriteRequest("file.bin", false, 0),
            CancellationToken.None);
        opened.Content!.Write(new byte[] { 1 });
        RequireFileLink(destinationPath, outsideFile);

        TftpStorageCommitResult committed = storage.CommitWrite(
            opened.CompletionToken!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(committed.Success, Is.False);
            Assert.That(committed.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(File.ReadAllBytes(outsideFile),
                Is.EqualTo(new byte[] { 9 }));
            Assert.That(Directory.GetFiles(
                directory.RootPath,
                "*.tftp.tmp"), Is.Empty);
        });
    }

    [Test]
    public void AbortPreservesExistingDestinationByteForByte()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        string destinationPath = Path.Combine(directory.RootPath, "file.bin");
        byte[] original = { 8, 7, 6 };
        File.WriteAllBytes(destinationPath, original);
        ITftpServerStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            false,
            true,
            true,
            true);

        TftpStorageOpenResult opened = storage.OpenPendingWrite(
            WriteRequest("file.bin", false, 0),
            CancellationToken.None);
        opened.Content!.Write(new byte[] { 1, 2, 3 });
        Assert.That(File.ReadAllBytes(destinationPath), Is.EqualTo(original));

        TftpStorageCommitResult aborted = storage.AbortWrite(
            opened.CompletionToken!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(aborted.Success, Is.True);
            Assert.That(File.ReadAllBytes(destinationPath), Is.EqualTo(original));
            Assert.That(Directory.GetFiles(
                directory.RootPath,
                "*.tftp.tmp"), Is.Empty);
        });
    }

    [TestCase(false, 0)]
    [TestCase(true, 0)]
    [TestCase(true, 27)]
    public void DistinguishesAbsentZeroAndPositiveWriteSizeMetadata(
        bool hasAnnouncedSize,
        long announcedSize)
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        ITftpServerStorage storage = CreateStorage(
            directory.RootPath,
            true,
            true,
            true,
            false,
            true,
            true);

        TftpStorageOpenResult result = storage.OpenPendingWrite(
            WriteRequest("file.bin", hasAnnouncedSize, announcedSize),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasKnownSize, Is.EqualTo(hasAnnouncedSize));
            Assert.That(result.KnownSize,
                Is.EqualTo(hasAnnouncedSize ? announcedSize : 0));
        });
        TftpStorageCommitResult aborted = storage.AbortWrite(
            result.CompletionToken!,
            CancellationToken.None);
        Assert.That(aborted.Success, Is.True);
    }

    private static TftpDirectoryStorage CreateStorage(
        string rootPath,
        bool allowRead,
        bool allowWrite,
        bool allowCreate,
        bool allowOverwrite,
        bool allowSubdirectories,
        bool rejectSymbolicLinks)
    {
        TftpDirectoryStorageOptions options = new TftpDirectoryStorageOptions();
        options.RootPath = rootPath;
        options.AllowRead = allowRead;
        options.AllowWrite = allowWrite;
        options.AllowCreate = allowCreate;
        options.AllowOverwrite = allowOverwrite;
        options.AllowSubdirectories = allowSubdirectories;
        options.RejectSymbolicLinks = rejectSymbolicLinks;
        return new TftpDirectoryStorage(options);
    }

    private static TftpServerRequest ReadRequest(string filename)
    {
        TftpServerRequest request = new TftpServerRequest();
        request.Filename = filename;
        request.Direction = TftpTransferDirection.Read;
        request.Mode = TftpTransferMode.Octet;
        return request;
    }

    private static TftpServerRequest WriteRequest(
        string filename,
        bool hasAnnouncedSize,
        long announcedSize)
    {
        TftpServerRequest request = new TftpServerRequest();
        request.Filename = filename;
        request.Direction = TftpTransferDirection.Write;
        request.Mode = TftpTransferMode.Octet;
        request.HasAnnouncedSize = hasAnnouncedSize;
        request.AnnouncedSize = announcedSize;
        return request;
    }

    private static void RequireDirectoryLink(
        string linkPath,
        string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (PlatformNotSupportedException)
        {
            Assert.Ignore("Symbolic links are not supported on this platform.");
        }
        catch (UnauthorizedAccessException)
        {
            Assert.Ignore("Symbolic-link creation is not permitted on this platform.");
        }
        catch (IOException)
        {
            Assert.Ignore("Symbolic-link creation is unavailable in this environment.");
        }
    }

    private static void RequireFileLink(
        string linkPath,
        string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (PlatformNotSupportedException)
        {
            Assert.Ignore("Symbolic links are not supported on this platform.");
        }
        catch (UnauthorizedAccessException)
        {
            Assert.Ignore("Symbolic-link creation is not permitted on this platform.");
        }
        catch (IOException)
        {
            Assert.Ignore("Symbolic-link creation is unavailable in this environment.");
        }
    }
}
