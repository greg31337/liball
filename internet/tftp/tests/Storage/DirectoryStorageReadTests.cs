using System.Threading;
using tftp;

namespace tftp.Tests;

public class DirectoryStorageReadTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(4097)]
    public void OpensOwnedOctetContentWithExactSize(int contentLength)
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        byte[] expected = CreateContent(contentLength);
        string filePath = Path.Combine(directory.RootPath, "content.bin");
        File.WriteAllBytes(filePath, expected);
        ITftpServerStorage storage = CreateStorage(directory.RootPath, true);

        TftpStorageOpenResult result = storage.OpenRead(
            ReadRequest("content.bin", TftpTransferMode.Octet, false),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(result.Content, Is.Not.Null);
            Assert.That(result.Content!.CanRead, Is.True);
            Assert.That(result.OwnsContent, Is.True);
            Assert.That(result.HasKnownSize, Is.True);
            Assert.That(result.KnownSize, Is.EqualTo(contentLength));
            Assert.That(result.CompletionToken, Is.Null);
        });

        Stream content = result.Content!;
        Assert.That(ReadAll(content), Is.EqualTo(expected));
        content.Dispose();
        Assert.That(content.CanRead, Is.False);
    }

    [TestCase("nested/content.bin")]
    [TestCase("nested\\content.bin")]
    public void OpensRootConfinedDescendantsWithEitherSeparator(
        string requestedPath)
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        string nestedPath = Path.Combine(directory.RootPath, "nested");
        Directory.CreateDirectory(nestedPath);
        File.WriteAllBytes(
            Path.Combine(nestedPath, "content.bin"),
            new byte[] { 4, 5 });
        ITftpServerStorage storage = CreateStorage(directory.RootPath, true);

        TftpStorageOpenResult result = storage.OpenRead(
            ReadRequest(requestedPath, TftpTransferMode.Octet, false),
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        using Stream content = result.Content!;
        Assert.That(ReadAll(content), Is.EqualTo(new byte[] { 4, 5 }));
    }

    [Test]
    public void ConstructorSnapshotsOptionsAndCanonicalRoot()
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "content.bin"),
            new byte[] { 8 });
        TftpDirectoryStorageOptions options = new TftpDirectoryStorageOptions();
        options.RootPath = Path.Combine(directory.RootPath, ".");
        options.AllowRead = true;
        TftpDirectoryStorage storage = new TftpDirectoryStorage(options);
        options.RootPath = directory.OutsidePath;
        options.AllowRead = false;

        TftpStorageOpenResult result = storage.OpenRead(
            ReadRequest("content.bin", TftpTransferMode.Octet, false),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(storage.Options.RootPath,
                Is.EqualTo(Path.GetFullPath(directory.RootPath)));
            Assert.That(storage.Options, Is.Not.SameAs(options));
        });
        result.Content!.Dispose();
    }

    [TestCase(false, 0)]
    [TestCase(true, 6)]
    public void ComputesNetasciiSizeOnlyWhenTransferSizeWasRequested(
        bool requestTransferSize,
        long expectedSize)
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        byte[] expected = { 0, 10, 13, 65 };
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "text.txt"),
            expected);
        ITftpServerStorage storage = CreateStorage(directory.RootPath, true);

        TftpStorageOpenResult result = storage.OpenRead(
            ReadRequest(
                "text.txt",
                TftpTransferMode.Netascii,
                requestTransferSize),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasKnownSize, Is.EqualTo(requestTransferSize));
            Assert.That(result.KnownSize, Is.EqualTo(expectedSize));
            Assert.That(result.Content!.Position, Is.Zero);
        });
        using Stream content = result.Content!;
        Assert.That(ReadAll(content), Is.EqualTo(expected));
    }

    [Test]
    public void TransferSizeOptionMatchingIsCaseInsensitive()
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "text.txt"),
            new byte[] { 10 });
        TftpServerRequest request = ReadRequest(
            "text.txt",
            TftpTransferMode.Netascii,
            false);
        TftpOption transferSize = new TftpOption();
        transferSize.Name = "TSIZE";
        transferSize.Value = "0";
        request.RequestedOptions.Add(transferSize);

        TftpStorageOpenResult result = CreateStorage(
            directory.RootPath,
            true).OpenRead(request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasKnownSize, Is.True);
            Assert.That(result.KnownSize, Is.EqualTo(2));
        });
        result.Content!.Dispose();
    }

    [Test]
    public void OmitsNetasciiSizeWhenRequestedOptionsAreUnavailable()
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "text.txt"),
            new byte[] { 10 });
        TftpServerRequest request = ReadRequest(
            "text.txt",
            TftpTransferMode.Netascii,
            false);
        request.RequestedOptions = null!;

        TftpStorageOpenResult result = CreateStorage(
            directory.RootPath,
            true).OpenRead(request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasKnownSize, Is.False);
            Assert.That(result.KnownSize, Is.Zero);
        });
        result.Content!.Dispose();
    }

    [Test]
    public void ReadPolicyDenialReturnsAccessViolationWithoutOpeningContent()
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "content.bin"),
            new byte[] { 1 });
        ITftpServerStorage storage = CreateStorage(directory.RootPath, false);

        TftpStorageOpenResult result = storage.OpenRead(
            ReadRequest("content.bin", TftpTransferMode.Octet, false),
            CancellationToken.None);

        AssertFailure(result, TftpErrorCode.AccessViolation, "not permitted");
    }

    [TestCase("missing.bin")]
    [TestCase("folder")]
    public void MissingFilesAndDirectoryTargetsReturnFileNotFound(
        string requestedPath)
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        Directory.CreateDirectory(Path.Combine(directory.RootPath, "folder"));
        ITftpServerStorage storage = CreateStorage(directory.RootPath, true);

        TftpStorageOpenResult result = storage.OpenRead(
            ReadRequest(requestedPath, TftpTransferMode.Octet, false),
            CancellationToken.None);

        AssertFailure(result, TftpErrorCode.FileNotFound, "not found");
    }

    [TestCase("../outside/secret.bin")]
    [TestCase("..\\outside\\secret.bin")]
    [TestCase("/absolute.bin")]
    [TestCase("C:\\absolute.bin")]
    public void UnsafePathsReturnAccessViolation(string requestedPath)
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        File.WriteAllBytes(
            Path.Combine(directory.OutsidePath, "secret.bin"),
            new byte[] { 9 });
        ITftpServerStorage storage = CreateStorage(directory.RootPath, true);

        TftpStorageOpenResult result = storage.OpenRead(
            ReadRequest(requestedPath, TftpTransferMode.Octet, false),
            CancellationToken.None);

        AssertFailure(result, TftpErrorCode.AccessViolation, "");
    }

    [Test]
    public void EscapingSymbolicLinkReturnsAccessViolation()
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        File.WriteAllBytes(
            Path.Combine(directory.OutsidePath, "secret.bin"),
            new byte[] { 9 });
        string linkPath = Path.Combine(directory.RootPath, "outside-link");
        RequireDirectoryLink(linkPath, directory.OutsidePath);
        ITftpServerStorage storage = CreateStorage(directory.RootPath, true);

        TftpStorageOpenResult result = storage.OpenRead(
            ReadRequest(
                "outside-link/secret.bin",
                TftpTransferMode.Octet,
                false),
            CancellationToken.None);

        AssertFailure(result, TftpErrorCode.AccessViolation, "Symbolic");
    }

    [Test]
    public void InvalidRequestsReturnSelectedProtocolErrors()
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "content.bin"),
            new byte[] { 1 });
        ITftpServerStorage storage = CreateStorage(directory.RootPath, true);
        TftpServerRequest wrongDirection = ReadRequest(
            "content.bin",
            TftpTransferMode.Octet,
            false);
        wrongDirection.Direction = TftpTransferDirection.Write;
        TftpServerRequest wrongMode = ReadRequest(
            "content.bin",
            (TftpTransferMode)99,
            false);

        TftpStorageOpenResult nullResult = storage.OpenRead(
            null!,
            CancellationToken.None);
        TftpStorageOpenResult directionResult = storage.OpenRead(
            wrongDirection,
            CancellationToken.None);
        TftpStorageOpenResult modeResult = storage.OpenRead(
            wrongMode,
            CancellationToken.None);

        AssertFailure(
            nullResult,
            TftpErrorCode.AccessViolation,
            "cannot be null");
        AssertFailure(
            directionResult,
            TftpErrorCode.AccessViolation,
            "not a read");
        AssertFailure(
            modeResult,
            TftpErrorCode.IllegalOperation,
            "mode");
    }

    [Test]
    public void CancellationStopsReadBeforeStorageAccess()
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "content.bin"),
            new byte[] { 1 });
        ITftpServerStorage storage = CreateStorage(directory.RootPath, true);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            storage.OpenRead(
                ReadRequest("content.bin", TftpTransferMode.Octet, false),
                cancellation.Token));

        using FileStream exclusive = new FileStream(
            Path.Combine(directory.RootPath, "content.bin"),
            FileMode.Open,
            FileAccess.Write,
            FileShare.None);
        Assert.That(exclusive.CanWrite, Is.True);
    }

    [Test]
    public void ConstructorRejectsUnavailableRoots()
    {
        using DirectoryStorageReadTestDirectory directory =
            new DirectoryStorageReadTestDirectory();
        string fileRoot = Path.Combine(directory.BasePath, "root-file");
        string missingRoot = Path.Combine(directory.BasePath, "missing");
        File.WriteAllBytes(fileRoot, new byte[] { 1 });
        TftpDirectoryStorageOptions fileOptions =
            new TftpDirectoryStorageOptions();
        fileOptions.RootPath = fileRoot;
        TftpDirectoryStorageOptions missingOptions =
            new TftpDirectoryStorageOptions();
        missingOptions.RootPath = missingRoot;

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<TftpStorageException>(() =>
                    new TftpDirectoryStorage(null!))!.Operation,
                Is.EqualTo("CreateDirectoryStorage"));
            Assert.That(
                Assert.Throws<TftpStorageException>(() =>
                    new TftpDirectoryStorage(fileOptions))!.Operation,
                Is.EqualTo("CreateDirectoryStorage"));
            Assert.That(
                Assert.Throws<TftpStorageException>(() =>
                    new TftpDirectoryStorage(missingOptions))!.Operation,
                Is.EqualTo("CreateDirectoryStorage"));
        });
    }

    private static ITftpServerStorage CreateStorage(
        string rootPath,
        bool allowRead)
    {
        TftpDirectoryStorageOptions options =
            new TftpDirectoryStorageOptions();
        options.RootPath = rootPath;
        options.AllowRead = allowRead;
        return new TftpDirectoryStorage(options);
    }

    private static TftpServerRequest ReadRequest(
        string filename,
        TftpTransferMode mode,
        bool requestTransferSize)
    {
        TftpServerRequest request = new TftpServerRequest();
        request.Filename = filename;
        request.Direction = TftpTransferDirection.Read;
        request.Mode = mode;
        if (requestTransferSize)
        {
            TftpOption option = new TftpOption();
            option.Name = TftpProtocolValues.TransferSizeOption;
            option.Value = "0";
            request.RequestedOptions.Add(option);
        }

        return request;
    }

    private static void AssertFailure(
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
            Assert.That(result.OwnsContent, Is.False);
            Assert.That(result.HasKnownSize, Is.False);
            Assert.That(result.KnownSize, Is.Zero);
            Assert.That(result.CompletionToken, Is.Null);
        });
    }

    private static byte[] ReadAll(Stream content)
    {
        using MemoryStream destination = new MemoryStream();
        content.CopyTo(destination);
        return destination.ToArray();
    }

    private static byte[] CreateContent(int length)
    {
        byte[] content = new byte[length];
        int index = 0;
        while (index < length)
        {
            content[index] = (byte)(index % 251);
            index = index + 1;
        }

        return content;
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
}

internal class DirectoryStorageReadTestDirectory : IDisposable
{
    internal string BasePath;
    internal string RootPath;
    internal string OutsidePath;

    internal DirectoryStorageReadTestDirectory()
    {
        BasePath = Path.Combine(
            Path.GetTempPath(),
            "tftp-directory-read-tests-" + Guid.NewGuid().ToString("N"));
        RootPath = Path.Combine(BasePath, "root");
        OutsidePath = Path.Combine(BasePath, "outside");
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(OutsidePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(BasePath))
        {
            Directory.Delete(BasePath, true);
        }
    }
}
