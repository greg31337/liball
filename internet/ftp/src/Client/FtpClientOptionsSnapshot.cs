namespace ftp;

internal static class FtpClientOptionsSnapshot
{
    internal static FtpClientOptions CreateValidated(
        FtpClientOptions? options,
        IFtpCertificateValidator? certificateValidator)
    {
        FtpConfigurationValidator.ValidateClientOptions(options);
        if (options == null)
        {
            throw new FtpConfigurationException("Client options are required.", null);
        }

        FtpConfigurationValidator.ValidateClientProviderPresence(
            options, certificateValidator);

        FtpClientOptions snapshot = new FtpClientOptions();
        snapshot.Endpoint = CopyEndpoint(options.Endpoint);
        snapshot.LocalBindEndpoint = CopyOptionalEndpoint(options.LocalBindEndpoint);
        snapshot.Credentials = CopyCredentials(options.Credentials);
        snapshot.Timeouts = CopyTimeouts(options.Timeouts);
        snapshot.ControlSocket = FtpSocketSettings.CreateSnapshot(options.ControlSocket);
        snapshot.DataConnection = CopyDataConnection(options.DataConnection);
        snapshot.Tls = CopyTls(options.Tls);
        snapshot.Quirks = CopyQuirks(options.Quirks);
        snapshot.SecurityMode = options.SecurityMode;
        snapshot.VirtualHost = options.VirtualHost;
        snapshot.NegotiateFeatures = options.NegotiateFeatures;
        snapshot.EnableUtf8 = options.EnableUtf8;
        snapshot.EnablePret = options.EnablePret;
        snapshot.ClientName = options.ClientName;
        snapshot.AllowPlaintextCredentials = options.AllowPlaintextCredentials;
        snapshot.StrictReplyParsing = options.StrictReplyParsing &&
            !options.Quirks.LenientReplies;
        snapshot.RedactSecrets = options.RedactSecrets;
        snapshot.RetryCount = options.RetryCount;
        if (snapshot.Quirks.HonorPassiveAdvertisedAddress)
        {
            snapshot.DataConnection.IgnorePassiveAdvertisedAddress = false;
        }
        return snapshot;
    }

    internal static FtpEndpoint CopyEndpoint(FtpEndpoint endpoint)
    {
        FtpEndpoint snapshot = new FtpEndpoint();
        snapshot.Host = endpoint.Host;
        snapshot.Port = endpoint.Port;
        snapshot.AddressFamily = endpoint.AddressFamily;
        return snapshot;
    }

    internal static FtpEndpoint? CopyOptionalEndpoint(FtpEndpoint? endpoint)
    {
        if (endpoint == null)
        {
            return null;
        }

        return CopyEndpoint(endpoint);
    }

    internal static FtpCredentials CopyCredentials(FtpCredentials credentials)
    {
        FtpCredentials snapshot = new FtpCredentials();
        snapshot.UserName = credentials.UserName;
        snapshot.Password = credentials.Password;
        snapshot.Account = credentials.Account;
        snapshot.Anonymous = credentials.Anonymous;
        return snapshot;
    }

    internal static FtpTimeoutOptions CopyTimeouts(FtpTimeoutOptions options)
    {
        FtpTimeoutOptions snapshot = new FtpTimeoutOptions();
        snapshot.ConnectTimeoutMilliseconds = options.ConnectTimeoutMilliseconds;
        snapshot.ControlReadTimeoutMilliseconds = options.ControlReadTimeoutMilliseconds;
        snapshot.ControlWriteTimeoutMilliseconds = options.ControlWriteTimeoutMilliseconds;
        snapshot.DataConnectionTimeoutMilliseconds = options.DataConnectionTimeoutMilliseconds;
        snapshot.TlsHandshakeTimeoutMilliseconds = options.TlsHandshakeTimeoutMilliseconds;
        snapshot.IdleTimeoutMilliseconds = options.IdleTimeoutMilliseconds;
        snapshot.GracefulStopTimeoutMilliseconds = options.GracefulStopTimeoutMilliseconds;
        snapshot.CancellationToken = options.CancellationToken;
        return snapshot;
    }

    internal static FtpDataConnectionOptions CopyDataConnection(
        FtpDataConnectionOptions options)
    {
        FtpDataConnectionOptions snapshot = new FtpDataConnectionOptions();
        snapshot.Mode = options.Mode;
        snapshot.Socket = FtpSocketSettings.CreateSnapshot(options.Socket);
        snapshot.UseExtendedPassive = options.UseExtendedPassive;
        snapshot.SendExtendedPassiveAll = options.SendExtendedPassiveAll;
        snapshot.AllowPassiveFallback = options.AllowPassiveFallback;
        snapshot.UseExtendedActive = options.UseExtendedActive;
        snapshot.AllowActiveFallback = options.AllowActiveFallback;
        snapshot.IgnorePassiveAdvertisedAddress = options.IgnorePassiveAdvertisedAddress;
        snapshot.RejectPassiveAddressMismatch = options.RejectPassiveAddressMismatch;
        snapshot.RequireDataPeerMatch = options.RequireDataPeerMatch;
        snapshot.AllowForeignDataConnections = options.AllowForeignDataConnections;
        snapshot.RejectPrivilegedActivePorts = options.RejectPrivilegedActivePorts;
        snapshot.ActiveBindEndpoint = CopyOptionalEndpoint(options.ActiveBindEndpoint);
        snapshot.ActiveAdvertisedAddress = options.ActiveAdvertisedAddress;
        snapshot.ActivePortStart = options.ActivePortStart;
        snapshot.ActivePortEnd = options.ActivePortEnd;
        snapshot.PassivePortStart = options.PassivePortStart;
        snapshot.PassivePortEnd = options.PassivePortEnd;
        return snapshot;
    }

    internal static FtpTlsOptions CopyTls(FtpTlsOptions options)
    {
        FtpTlsOptions snapshot = new FtpTlsOptions();
        snapshot.Protocols = options.Protocols;
        snapshot.CheckCertificateRevocation = options.CheckCertificateRevocation;
        snapshot.RequireCertificateNameMatch = options.RequireCertificateNameMatch;
        snapshot.RequireCertificateChainTrust = options.RequireCertificateChainTrust;
        snapshot.RequireClientCertificate = options.RequireClientCertificate;
        snapshot.RequireDataSessionReuse = options.RequireDataSessionReuse;
        snapshot.AllowClearCommandChannel = options.AllowClearCommandChannel;
        snapshot.DataProtectionLevel = options.DataProtectionLevel;
        snapshot.TargetHost = options.TargetHost;
        snapshot.ClientCertificate = options.ClientCertificate;
        snapshot.ServerCertificate = options.ServerCertificate;
        return snapshot;
    }

    internal static FtpClientQuirkOptions CopyQuirks(
        FtpClientQuirkOptions options)
    {
        FtpClientQuirkOptions snapshot = new FtpClientQuirkOptions();
        snapshot.LenientReplies = options.LenientReplies;
        snapshot.HonorPassiveAdvertisedAddress =
            options.HonorPassiveAdvertisedAddress;
        snapshot.IgnoreUtf8FeatureClaim = options.IgnoreUtf8FeatureClaim;
        snapshot.IgnoreMlstFeatureClaim = options.IgnoreMlstFeatureClaim;
        snapshot.IgnoreSizeFeatureClaim = options.IgnoreSizeFeatureClaim;
        snapshot.IgnoreMdtmFeatureClaim = options.IgnoreMdtmFeatureClaim;
        snapshot.AllowMlsdFallback = options.AllowMlsdFallback;
        snapshot.AllowSizeFallback = options.AllowSizeFallback;
        snapshot.AllowMdtmFallback = options.AllowMdtmFallback;
        snapshot.AllowHistoricAliasFallback =
            options.AllowHistoricAliasFallback;
        snapshot.CommandsBeforeAuthorization = CopyQuirkCommands(
            options.CommandsBeforeAuthorization);
        snapshot.CommandsAfterAuthorization = CopyQuirkCommands(
            options.CommandsAfterAuthorization);
        return snapshot;
    }

    internal static FtpCommand[] CopyQuirkCommands(FtpCommand[] commands)
    {
        FtpCommand[] snapshot = new FtpCommand[commands.Length];
        int index = 0;
        while (index < commands.Length)
        {
            string commandLine = FtpCommandWriter.BuildCommandLine(commands[index]);
            snapshot[index] = FtpCommandParser.ParseDecodedCommand(commandLine);
            index++;
        }
        return snapshot;
    }
}
