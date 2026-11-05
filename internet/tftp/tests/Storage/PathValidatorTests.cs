using tftp;

namespace tftp.Tests;

public class PathValidatorTests
{
    [TestCase("file.bin", "file.bin")]
    [TestCase("folder/file.bin", "folder/file.bin")]
    [TestCase("folder\\nested/file.bin", "folder/nested/file.bin")]
    [TestCase("folder//file.bin", "folder/file.bin")]
    public void ResolvesPortableRelativePathsInsideCanonicalRoot(
        string requestedPath,
        string expectedRelativePath)
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            true,
            true);

        TftpPathValidationResult result = validator.ValidatePath(requestedPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FullPath, Is.EqualTo(Path.GetFullPath(
                expectedRelativePath.Replace('/', Path.DirectorySeparatorChar),
                directory.RootPath)));
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(result.ErrorMessage, Is.Empty);
        });
    }

    [Test]
    public void RejectsNullEmptyAndNulPaths()
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            true,
            true);

        AssertRejected(validator.ValidatePath(null!), "empty");
        AssertRejected(validator.ValidatePath(""), "empty");
        AssertRejected(validator.ValidatePath("name\0.bin"), "NUL");
    }

    [TestCase(".")]
    [TestCase("..")]
    [TestCase("...")]
    [TestCase("../outside.bin")]
    [TestCase("..\\outside.bin")]
    [TestCase("folder/../outside.bin")]
    [TestCase("folder\\..\\outside.bin")]
    [TestCase("folder/.. \\outside.bin")]
    public void RejectsTraversalWithEitherSeparator(string requestedPath)
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            true,
            true);

        TftpPathValidationResult result = validator.ValidatePath(requestedPath);

        AssertRejected(result, "traversal");
    }

    [TestCase("/etc/passwd")]
    [TestCase("\\server\\share\\file.bin")]
    [TestCase("//server/share/file.bin")]
    [TestCase("\\\\?\\C:\\file.bin")]
    [TestCase("//?/C:/file.bin")]
    [TestCase("C:\\file.bin")]
    [TestCase("c:relative.bin")]
    [TestCase("folder/file:stream")]
    public void RejectsAbsoluteDriveAndDevicePathForms(string requestedPath)
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            true,
            true);

        TftpPathValidationResult result = validator.ValidatePath(requestedPath);

        AssertRejected(result, "permitted");
    }

    [TestCase("CON")]
    [TestCase("con.txt")]
    [TestCase("PRN.")]
    [TestCase("AUX ")]
    [TestCase("nul.bin")]
    [TestCase("CONIN$")]
    [TestCase("conout$.txt")]
    [TestCase("CLOCK$")]
    [TestCase("COM1")]
    [TestCase("com9.log")]
    [TestCase("folder/LPT1.bin")]
    [TestCase("lpt9")]
    public void RejectsPortableDeviceNameComponents(string requestedPath)
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            true,
            true);

        TftpPathValidationResult result = validator.ValidatePath(requestedPath);

        AssertRejected(result, "Device");
    }

    [TestCase("console.bin")]
    [TestCase("null.bin")]
    [TestCase("COM0")]
    [TestCase("com10")]
    [TestCase("lpt0.txt")]
    [TestCase("auxiliary")]
    public void AcceptsNamesThatOnlyResembleDeviceNames(string requestedPath)
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            true,
            true);

        TftpPathValidationResult result = validator.ValidatePath(requestedPath);

        Assert.That(result.Success, Is.True);
    }

    [TestCase("folder/file.bin")]
    [TestCase("folder\\file.bin")]
    [TestCase("folder//file.bin")]
    public void RejectsDescendantsWhenSubdirectoriesAreDisabled(
        string requestedPath)
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            false,
            true);

        TftpPathValidationResult result = validator.ValidatePath(requestedPath);

        AssertRejected(result, "subdirectories");
    }

    [Test]
    public void UsesSeparatorBoundaryInsteadOfRootTextPrefix()
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            true,
            true);
        string siblingPath = Path.Combine(
            Path.GetDirectoryName(directory.RootPath)!,
            Path.GetFileName(directory.RootPath) + "-sibling",
            "file.bin");

        Assert.Multiple(() =>
        {
            Assert.That(validator.IsConfinedPath(directory.RootPath), Is.True);
            Assert.That(validator.IsConfinedPath(
                Path.Combine(directory.RootPath, "file.bin")), Is.True);
            Assert.That(validator.IsConfinedPath(siblingPath), Is.False);
        });
    }

    [Test]
    public void RejectsLinksEvenWhenTheirTargetsRemainInsideRoot()
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        string targetPath = Path.Combine(directory.RootPath, "target");
        string linkPath = Path.Combine(directory.RootPath, "link");
        Directory.CreateDirectory(targetPath);
        RequireDirectoryLink(linkPath, targetPath);
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            true,
            true);

        TftpPathValidationResult result = validator.ValidatePath(
            "link/file.bin");

        AssertRejected(result, "Symbolic");
    }

    [Test]
    public void AllowsInternalLinksButRejectsLinkEscapesWhenLinksAreEnabled()
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        string internalTarget = Path.Combine(directory.RootPath, "target");
        string internalLink = Path.Combine(directory.RootPath, "internal-link");
        string outsideLink = Path.Combine(directory.RootPath, "outside-link");
        Directory.CreateDirectory(internalTarget);
        RequireDirectoryLink(internalLink, internalTarget);
        RequireDirectoryLink(outsideLink, directory.OutsidePath);
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            true,
            false);

        TftpPathValidationResult internalResult = validator.ValidatePath(
            "internal-link/file.bin");
        TftpPathValidationResult outsideResult = validator.ValidatePath(
            "outside-link/file.bin");

        Assert.Multiple(() =>
        {
            Assert.That(internalResult.Success, Is.True);
            Assert.That(outsideResult.Success, Is.False);
            Assert.That(outsideResult.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(outsideResult.ErrorMessage, Does.Contain("escapes"));
        });
    }

    [Test]
    public void RejectsDanglingLinksThatPointOutsideRoot()
    {
        using PathValidatorTestDirectory directory =
            new PathValidatorTestDirectory();
        string linkPath = Path.Combine(directory.RootPath, "dangling-link");
        string missingOutsideTarget = Path.Combine(
            directory.OutsidePath,
            "missing");
        RequireDirectoryLink(linkPath, missingOutsideTarget);
        TftpPathValidator validator = CreateValidator(
            directory.RootPath,
            true,
            false);

        TftpPathValidationResult result = validator.ValidatePath(
            "dangling-link/file.bin");

        AssertRejected(result, "escapes");
    }

    [Test]
    public void RejectsInvalidConfiguredRoots()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<TftpStorageException>(() =>
                    new TftpPathValidator(null!))!.Operation,
                Is.EqualTo("ValidateRootPath"));

            TftpDirectoryStorageOptions empty =
                new TftpDirectoryStorageOptions();
            Assert.That(
                Assert.Throws<TftpStorageException>(() =>
                    new TftpPathValidator(empty))!.Operation,
                Is.EqualTo("ValidateRootPath"));

            TftpDirectoryStorageOptions nul =
                new TftpDirectoryStorageOptions();
            nul.RootPath = "root\0path";
            Assert.That(
                Assert.Throws<TftpStorageException>(() =>
                    new TftpPathValidator(nul))!.Operation,
                Is.EqualTo("ValidateRootPath"));
        });
    }

    private static TftpPathValidator CreateValidator(
        string rootPath,
        bool allowSubdirectories,
        bool rejectSymbolicLinks)
    {
        TftpDirectoryStorageOptions options =
            new TftpDirectoryStorageOptions();
        options.RootPath = rootPath;
        options.AllowSubdirectories = allowSubdirectories;
        options.RejectSymbolicLinks = rejectSymbolicLinks;
        return new TftpPathValidator(options);
    }

    private static void AssertRejected(
        TftpPathValidationResult result,
        string messageFragment)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FullPath, Is.Empty);
            Assert.That(result.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(result.ErrorMessage,
                Does.Contain(messageFragment).IgnoreCase);
        });
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

internal class PathValidatorTestDirectory : IDisposable
{
    internal string BasePath;
    internal string RootPath;
    internal string OutsidePath;

    internal PathValidatorTestDirectory()
    {
        BasePath = Path.Combine(
            Path.GetTempPath(),
            "tftp-path-tests-" + Guid.NewGuid().ToString("N"));
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
