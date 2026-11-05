namespace ftp.Tests;

public class PublicServerContractTests
{
    [Test]
    public void Server_and_provider_contracts_expose_the_planned_operations()
    {
        AssertMethods<IFtpServer>(
            nameof(IFtpServer.StartServer),
            nameof(IFtpServer.StopServer),
            nameof(IFtpServer.GetServerState));
        AssertMethods<IFtpFileSystem>(
            nameof(IFtpFileSystem.ResolveEntry),
            nameof(IFtpFileSystem.EnumerateDirectory),
            nameof(IFtpFileSystem.OpenFileRead),
            nameof(IFtpFileSystem.OpenFileWrite),
            nameof(IFtpFileSystem.CreateDirectoryEntry),
            nameof(IFtpFileSystem.DeleteFileEntry),
            nameof(IFtpFileSystem.DeleteDirectoryEntry),
            nameof(IFtpFileSystem.RenameEntry),
            nameof(IFtpFileSystem.CreateUniqueEntry),
            nameof(IFtpFileSystem.AllocateEntry),
            nameof(IFtpFileSystem.SetModificationTime),
            nameof(IFtpFileSystem.SetPermissions),
            nameof(IFtpFileSystem.QuerySpace),
            nameof(IFtpFileSystem.MountStructure));
        AssertMethods<IFtpAuthenticator>(nameof(IFtpAuthenticator.AuthenticateIdentity));
        AssertMethods<IFtpAuthorizer>(nameof(IFtpAuthorizer.AuthorizeOperation));
        AssertMethods<IFtpHostResolver>(nameof(IFtpHostResolver.ResolveVirtualHost));
        AssertMethods<IFtpCertificateProvider>(nameof(IFtpCertificateProvider.SelectServerCertificate));
        AssertMethods<IFtpCertificateValidator>(nameof(IFtpCertificateValidator.ValidateRemoteCertificate));
        AssertMethods<IFtpExtensionHandler>(nameof(IFtpExtensionHandler.HandleExtensionCommand));
        AssertMethods<IFtpTransferObserver>(
            nameof(IFtpTransferObserver.ObserveTransferStarted),
            nameof(IFtpTransferObserver.ObserveTransferProgress),
            nameof(IFtpTransferObserver.ObserveTransferCompleted));
        AssertMethods<IFtpTransferFilter>(nameof(IFtpTransferFilter.IncludeTransferEntry));
        AssertMethods<IFtpEventSink>(nameof(IFtpEventSink.RecordEvent));
    }

    [Test]
    public void Transfer_observer_receives_the_supplied_snapshots_and_result()
    {
        TransferObserverStub observer = new TransferObserverStub();
        FtpTransferProgress started = new FtpTransferProgress();
        FtpTransferProgress progress = new FtpTransferProgress();
        FtpTransferResult completed = new FtpTransferResult();

        observer.ObserveTransferStarted(started);
        observer.ObserveTransferProgress(progress);
        observer.ObserveTransferCompleted(completed);

        Assert.Multiple(() =>
        {
            Assert.That(observer.Started, Is.SameAs(started));
            Assert.That(observer.Progress, Is.SameAs(progress));
            Assert.That(observer.Completed, Is.SameAs(completed));
        });
    }

    private static void AssertMethods<TContract>(params string[] expected)
    {
        string[] actual = typeof(TContract)
            .GetMethods()
            .Select(method => method.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected.OrderBy(name => name).ToArray()));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
        });
    }

    private class TransferObserverStub : IFtpTransferObserver
    {
        public FtpTransferProgress? Started;
        public FtpTransferProgress? Progress;
        public FtpTransferResult? Completed;

        public void ObserveTransferStarted(FtpTransferProgress progress)
        {
            Started = progress;
        }

        public void ObserveTransferProgress(FtpTransferProgress progress)
        {
            Progress = progress;
        }

        public void ObserveTransferCompleted(FtpTransferResult result)
        {
            Completed = result;
        }
    }
}
