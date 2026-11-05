using tftp;

namespace tftp.Tests;

public class StorageDataTests
{
    [Test]
    public void OpenResultDefaultsDescribeAnUnspecifiedFailure()
    {
        TftpStorageOpenResult result = new TftpStorageOpenResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(result.ErrorMessage, Is.Empty);
            Assert.That(result.Content, Is.Null);
            Assert.That(result.OwnsContent, Is.False);
            Assert.That(result.HasKnownSize, Is.False);
            Assert.That(result.KnownSize, Is.Zero);
            Assert.That(result.CompletionToken, Is.Null);
        });
    }

    [Test]
    public void OpenResultCanRepresentOwnedReadContentWithKnownZeroSize()
    {
        using MemoryStream content = new MemoryStream();
        TftpStorageOpenResult result = new TftpStorageOpenResult();
        result.Success = true;
        result.Content = content;
        result.OwnsContent = true;
        result.HasKnownSize = true;
        result.KnownSize = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Content, Is.SameAs(content));
            Assert.That(result.OwnsContent, Is.True);
            Assert.That(result.HasKnownSize, Is.True);
            Assert.That(result.KnownSize, Is.Zero);
            Assert.That(result.CompletionToken, Is.Null);
        });
    }

    [Test]
    public void OpenResultCanRepresentPendingWriteAndExpectedFailure()
    {
        using MemoryStream content = new MemoryStream();
        object token = new object();
        TftpStorageOpenResult result = new TftpStorageOpenResult();
        result.Success = true;
        result.Content = content;
        result.OwnsContent = false;
        result.CompletionToken = token;

        Assert.Multiple(() =>
        {
            Assert.That(result.Content, Is.SameAs(content));
            Assert.That(result.OwnsContent, Is.False);
            Assert.That(result.CompletionToken, Is.SameAs(token));
        });

        result.Success = false;
        result.Content = null;
        result.CompletionToken = null;
        result.ErrorCode = TftpErrorCode.FileAlreadyExists;
        result.ErrorMessage = "Destination exists";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Content, Is.Null);
            Assert.That(result.CompletionToken, Is.Null);
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.FileAlreadyExists));
            Assert.That(result.ErrorMessage, Is.EqualTo("Destination exists"));
        });
    }

    [Test]
    public void CommitResultRepresentsCommitAndAbortOutcomes()
    {
        TftpStorageCommitResult result = new TftpStorageCommitResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(result.ErrorMessage, Is.Empty);
        });

        result.Success = true;

        Assert.That(result.Success, Is.True);

        result.Success = false;
        result.ErrorCode = TftpErrorCode.StorageExhausted;
        result.ErrorMessage = "Atomic publication failed";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(result.ErrorMessage, Is.EqualTo("Atomic publication failed"));
        });
    }

    [Test]
    public void DirectoryStorageOptionsHaveConservativeDefaults()
    {
        TftpDirectoryStorageOptions options = new TftpDirectoryStorageOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.RootPath, Is.Empty);
            Assert.That(options.AllowRead, Is.True);
            Assert.That(options.AllowWrite, Is.False);
            Assert.That(options.AllowCreate, Is.False);
            Assert.That(options.AllowOverwrite, Is.False);
            Assert.That(options.AllowSubdirectories, Is.True);
            Assert.That(options.RejectSymbolicLinks, Is.True);
        });
    }

    [Test]
    public void DirectoryStorageOptionsCanRepresentAccessAndPathPolicy()
    {
        TftpDirectoryStorageOptions options = new TftpDirectoryStorageOptions();
        options.RootPath = "/srv/tftp";
        options.AllowRead = false;
        options.AllowWrite = true;
        options.AllowCreate = true;
        options.AllowOverwrite = true;
        options.AllowSubdirectories = false;
        options.RejectSymbolicLinks = false;

        Assert.Multiple(() =>
        {
            Assert.That(options.RootPath, Is.EqualTo("/srv/tftp"));
            Assert.That(options.AllowRead, Is.False);
            Assert.That(options.AllowWrite, Is.True);
            Assert.That(options.AllowCreate, Is.True);
            Assert.That(options.AllowOverwrite, Is.True);
            Assert.That(options.AllowSubdirectories, Is.False);
            Assert.That(options.RejectSymbolicLinks, Is.False);
        });
    }

    [Test]
    public void ServerOptionsCanCarryOptionalDirectoryStorageSettings()
    {
        TftpServerOptions first = new TftpServerOptions();
        TftpServerOptions second = new TftpServerOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.DirectoryStorage, Is.Null);
            Assert.That(second.DirectoryStorage, Is.Null);
        });

        first.DirectoryStorage = new TftpDirectoryStorageOptions();
        first.DirectoryStorage.RootPath = "/srv/tftp";

        Assert.Multiple(() =>
        {
            Assert.That(first.DirectoryStorage.RootPath, Is.EqualTo("/srv/tftp"));
            Assert.That(second.DirectoryStorage, Is.Null);
        });
    }
}
