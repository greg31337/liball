using System.Reflection;
using System.Security.Authentication;

namespace ftp.Tests;

public class FtpConfigurationValidatorTests
{
    [Test]
    public void Secure_default_derived_configurations_are_valid_when_bound()
    {
        FtpClientOptions client = CreateValidClient();
        FtpServerOptions server = CreateValidServer();
        FtpTransferRequest transfer = CreateValidTransferRequest();
        FtpDirectoryTransferRequest directory = new FtpDirectoryTransferRequest();
        directory.LocalPath = "/local";
        directory.RemotePath = "/remote";

        IFtpCertificateValidator certificateValidator = CreateProxy<IFtpCertificateValidator>();
        IFtpFileSystem fileSystem = CreateProxy<IFtpFileSystem>();
        IFtpAuthenticator authenticator = CreateProxy<IFtpAuthenticator>();
        IFtpAuthorizer authorizer = CreateProxy<IFtpAuthorizer>();
        IFtpHostResolver hostResolver = CreateProxy<IFtpHostResolver>();
        IFtpCertificateProvider certificateProvider = CreateProxy<IFtpCertificateProvider>();
        IFtpEventSink eventSink = CreateProxy<IFtpEventSink>();

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => FtpConfigurationValidator.ValidateClientOptions(client));
            Assert.DoesNotThrow(() => FtpConfigurationValidator.ValidateClientProviderPresence(client, certificateValidator));
            Assert.DoesNotThrow(() => FtpConfigurationValidator.ValidateServerOptions(server));
            Assert.DoesNotThrow(() => FtpConfigurationValidator.ValidateServerProviderPresence(
                fileSystem, authenticator, authorizer, hostResolver, certificateProvider, eventSink));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferRequest(transfer));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateDirectoryTransferRequest(directory));
        });
    }

    [Test]
    public void Endpoint_and_port_range_bounds_are_enforced()
    {
        FtpEndpoint missingHost = new FtpEndpoint();
        FtpEndpoint invalidPort = new FtpEndpoint();
        invalidPort.Host = "ftp.example";
        invalidPort.Port = 65536;
        FtpDataConnectionOptions partialActiveRange = new FtpDataConnectionOptions();
        partialActiveRange.ActivePortStart = 40000;
        FtpDataConnectionOptions reversedPassiveRange = new FtpDataConnectionOptions();
        reversedPassiveRange.PassivePortStart = 50000;
        reversedPassiveRange.PassivePortEnd = 49999;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateRemoteEndpoint(missingHost));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateRemoteEndpoint(invalidPort));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateDataConnectionOptions(partialActiveRange));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateDataConnectionOptions(reversedPassiveRange));
        });
    }

    [Test]
    public void Data_connection_security_combinations_require_explicit_consistent_policy()
    {
        FtpDataConnectionOptions epsvFallback = new FtpDataConnectionOptions();
        epsvFallback.SendExtendedPassiveAll = true;
        FtpDataConnectionOptions foreignPeerMatch = new FtpDataConnectionOptions();
        foreignPeerMatch.AllowForeignDataConnections = true;
        FtpDataConnectionOptions safeFxp = new FtpDataConnectionOptions();
        safeFxp.AllowForeignDataConnections = true;
        safeFxp.RequireDataPeerMatch = false;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateDataConnectionOptions(epsvFallback));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateDataConnectionOptions(foreignPeerMatch));
            Assert.DoesNotThrow(() => FtpConfigurationValidator.ValidateDataConnectionOptions(safeFxp));
        });
    }

    [Test]
    public void Tls_and_plaintext_security_requirements_are_enforced()
    {
        FtpTlsOptions invalidProtocols = new FtpTlsOptions();
        invalidProtocols.Protocols = (SslProtocols)int.MaxValue;
        FtpTlsOptions clearReuse = new FtpTlsOptions();
        clearReuse.DataProtectionLevel = FtpDataProtectionLevel.Clear;
        clearReuse.RequireDataSessionReuse = true;
        FtpClientOptions opportunistic = CreateValidClient();
        opportunistic.SecurityMode = FtpSecurityMode.ExplicitTlsOpportunistic;
        FtpClientOptions plaintextCredentials = CreateValidClient();
        plaintextCredentials.SecurityMode = FtpSecurityMode.Plain;
        plaintextCredentials.Credentials.UserName = "user";
        plaintextCredentials.Credentials.Password = "secret";

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateTlsOptions(invalidProtocols));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateTlsOptions(clearReuse));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateClientOptions(opportunistic));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateClientOptions(plaintextCredentials));
            plaintextCredentials.AllowPlaintextCredentials = true;
            Assert.DoesNotThrow(() => FtpConfigurationValidator.ValidateClientOptions(plaintextCredentials));
        });
    }

    [Test]
    public void Timeout_server_and_virtual_host_resource_bounds_are_enforced()
    {
        FtpTimeoutOptions timeout = new FtpTimeoutOptions();
        timeout.ControlReadTimeoutMilliseconds = 0;
        FtpServerOptions noListeners = new FtpServerOptions();
        FtpServerOptions inconsistentSessions = CreateValidServer();
        inconsistentSessions.MaximumSessionsPerAddress = inconsistentSessions.MaximumSessions + 1;
        FtpVirtualHostOptions anonymousWrites = new FtpVirtualHostOptions();
        anonymousWrites.HostName = "example";
        anonymousWrites.RootPath = "/root";
        anonymousWrites.AllowAnonymousWrites = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateTimeoutOptions(timeout));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateServerOptions(noListeners));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateServerOptions(inconsistentSessions));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateVirtualHostOptions(anonymousWrites));
        });
    }

    [Test]
    public void Resource_limit_flags_require_consistent_non_negative_values()
    {
        FtpResourceLimits hiddenTransfer = new FtpResourceLimits();
        hiddenTransfer.TransferByteCeiling = 1;
        FtpResourceLimits zeroBandwidth = new FtpResourceLimits();
        zeroBandwidth.HasReadBytesPerSecond = true;
        FtpResourceLimits valid = new FtpResourceLimits();
        valid.HasStorageQuotaBytes = true;
        valid.StorageQuotaBytes = 0;
        valid.HasListingByteCap = true;
        valid.ListingByteCap = 0;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                FtpConfigurationValidator.ValidateResourceLimits(
                    hiddenTransfer));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpConfigurationValidator.ValidateResourceLimits(
                    zeroBandwidth));
            Assert.DoesNotThrow(() =>
                FtpConfigurationValidator.ValidateResourceLimits(valid));
        });
    }

    [Test]
    public void Transfer_representation_checksum_and_atomic_combinations_are_enforced()
    {
        FtpTransferOptions localByte = new FtpTransferOptions();
        localByte.Type = FtpTransferType.LocalByte;
        FtpTransferOptions checksum = new FtpTransferOptions();
        checksum.VerificationMode = FtpVerificationMode.Checksum;
        checksum.ExpectedChecksumAlgorithm = "SHA-256";
        FtpTransferOptions atomicAppend = new FtpTransferOptions();
        atomicAppend.AtomicUpload = true;
        atomicAppend.CollisionPolicy = FtpCollisionPolicy.Append;
        FtpTransferOptions validLocalByte = new FtpTransferOptions();
        validLocalByte.Type = FtpTransferType.LocalByte;
        validLocalByte.HasLocalByteSize = true;
        validLocalByte.LocalByteSize = 8;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateTransferOptions(localByte));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateTransferOptions(checksum));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateTransferOptions(atomicAppend));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateTransferOptions(validLocalByte));
        });
    }

    [Test]
    public void Transfer_requests_require_paths_stream_binding_and_consistent_optional_values()
    {
        FtpTransferRequest unbound = new FtpTransferRequest();
        unbound.RemotePath = "/remote";
        FtpTransferRequest ambiguous = CreateValidTransferRequest();
        ambiguous.Content = new MemoryStream();
        FtpTransferRequest hiddenRestart = CreateValidTransferRequest();
        hiddenRestart.RestartOffset = 5;
        FtpTransferRequest atomicDownload = CreateValidTransferRequest();
        atomicDownload.Options.AtomicUpload = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateTransferRequest(unbound));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateTransferRequest(ambiguous));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateTransferRequest(hiddenRestart));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateTransferRequest(atomicDownload));
        });

        ambiguous.Content.Dispose();
    }

    [Test]
    public void Destructive_directory_options_require_mirror_mode_and_explicit_opt_in()
    {
        FtpDirectoryTransferOptions copyDeletion = new FtpDirectoryTransferOptions();
        copyDeletion.DeleteExtraneous = true;
        FtpDirectoryTransferOptions mirrorWithoutOptIn = new FtpDirectoryTransferOptions();
        mirrorWithoutOptIn.SyncMode = FtpDirectorySyncMode.Mirror;
        mirrorWithoutOptIn.DeleteExtraneous = true;
        FtpDirectoryTransferOptions mirror = new FtpDirectoryTransferOptions();
        mirror.SyncMode = FtpDirectorySyncMode.Mirror;
        mirror.DeleteExtraneous = true;
        mirror.AllowRecursiveDeletion = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateDirectoryTransferOptions(copyDeletion));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateDirectoryTransferOptions(mirrorWithoutOptIn));
            Assert.DoesNotThrow(() => FtpTransferValidator.ValidateDirectoryTransferOptions(mirror));
        });
    }

    [Test]
    public void Provider_presence_and_server_to_server_sessions_are_required()
    {
        FtpClientOptions tlsClient = CreateValidClient();
        FtpClientOptions plainClient = CreateValidClient();
        plainClient.SecurityMode = FtpSecurityMode.Plain;
        IFtpFileSystem fileSystem = CreateProxy<IFtpFileSystem>();
        IFtpAuthenticator authenticator = CreateProxy<IFtpAuthenticator>();
        IFtpAuthorizer authorizer = CreateProxy<IFtpAuthorizer>();
        IFtpHostResolver hostResolver = CreateProxy<IFtpHostResolver>();
        IFtpCertificateProvider certificateProvider = CreateProxy<IFtpCertificateProvider>();
        IFtpEventSink eventSink = CreateProxy<IFtpEventSink>();

        FtpServerTransferRequest missingSessions = new FtpServerTransferRequest();
        missingSessions.SourcePath = "/source";
        missingSessions.TargetPath = "/target";
        FtpServerTransferRequest sameSession = new FtpServerTransferRequest();
        IFtpClient client = CreateProxy<IFtpClient>();
        sameSession.SourceClient = client;
        sameSession.TargetClient = client;
        sameSession.SourcePath = "/source";
        sameSession.TargetPath = "/target";

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateClientProviderPresence(tlsClient, null));
            Assert.DoesNotThrow(() => FtpConfigurationValidator.ValidateClientProviderPresence(plainClient, null));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateServerProviderPresence(
                null, authenticator, authorizer, hostResolver, certificateProvider, eventSink));
            Assert.Throws<FtpConfigurationException>(() => FtpConfigurationValidator.ValidateServerProviderPresence(
                fileSystem, authenticator, authorizer, hostResolver, certificateProvider, null));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateServerTransferRequest(missingSessions));
            Assert.Throws<FtpConfigurationException>(() => FtpTransferValidator.ValidateServerTransferRequest(sameSession));
        });
    }

    private static FtpClientOptions CreateValidClient()
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = "ftp.example";
        return options;
    }

    private static FtpServerOptions CreateValidServer()
    {
        FtpServerOptions options = new FtpServerOptions();
        options.Listeners = new[] { new FtpListenerOptions() };
        return options;
    }

    private static FtpTransferRequest CreateValidTransferRequest()
    {
        FtpTransferRequest request = new FtpTransferRequest();
        request.RemotePath = "/remote";
        request.LocalPath = "/local";
        return request;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, ValidationProviderProxy>();
    }
}

public class ValidationProviderProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? arguments)
    {
        throw new NotSupportedException("Validation tests do not invoke provider operations.");
    }
}
