using System.Security.Authentication;

namespace ftp.Tests;

public class PublicOptionDataTests
{
    [Test]
    public void Endpoint_uses_control_port_and_unspecified_address_family()
    {
        FtpEndpoint endpoint = new FtpEndpoint();

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Host, Is.Empty);
            Assert.That(endpoint.Port, Is.EqualTo(FtpPorts.Control));
            Assert.That(endpoint.AddressFamily, Is.EqualTo(FtpAddressFamily.Unspecified));
        });
    }

    [Test]
    public void Credentials_do_not_enable_anonymous_or_supply_secrets()
    {
        FtpCredentials credentials = new FtpCredentials();

        Assert.Multiple(() =>
        {
            Assert.That(credentials.UserName, Is.Empty);
            Assert.That(credentials.Password, Is.Null);
            Assert.That(credentials.Account, Is.Null);
            Assert.That(credentials.Anonymous, Is.False);
        });
    }

    [Test]
    public void Timeout_options_use_bounded_defaults_and_no_cancellation()
    {
        FtpTimeoutOptions options = new FtpTimeoutOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.ConnectTimeoutMilliseconds, Is.EqualTo(FtpDefaults.ConnectTimeoutMilliseconds));
            Assert.That(options.ControlReadTimeoutMilliseconds, Is.EqualTo(FtpDefaults.ControlReadTimeoutMilliseconds));
            Assert.That(options.ControlWriteTimeoutMilliseconds, Is.EqualTo(FtpDefaults.ControlWriteTimeoutMilliseconds));
            Assert.That(options.DataConnectionTimeoutMilliseconds, Is.EqualTo(FtpDefaults.DataConnectionTimeoutMilliseconds));
            Assert.That(options.TlsHandshakeTimeoutMilliseconds, Is.EqualTo(FtpDefaults.TlsHandshakeTimeoutMilliseconds));
            Assert.That(options.IdleTimeoutMilliseconds, Is.EqualTo(FtpDefaults.IdleTimeoutMilliseconds));
            Assert.That(options.GracefulStopTimeoutMilliseconds, Is.EqualTo(FtpDefaults.GracefulStopTimeoutMilliseconds));
            Assert.That(options.CancellationToken.CanBeCanceled, Is.False);
        });
    }

    [Test]
    public void Client_quirks_disable_historic_alias_fallback_by_default()
    {
        FtpClientQuirkOptions first = new FtpClientQuirkOptions();
        FtpClientQuirkOptions second = new FtpClientQuirkOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.LenientReplies, Is.False);
            Assert.That(first.HonorPassiveAdvertisedAddress, Is.False);
            Assert.That(first.IgnoreUtf8FeatureClaim, Is.False);
            Assert.That(first.IgnoreMlstFeatureClaim, Is.False);
            Assert.That(first.IgnoreSizeFeatureClaim, Is.False);
            Assert.That(first.IgnoreMdtmFeatureClaim, Is.False);
            Assert.That(first.AllowMlsdFallback, Is.True);
            Assert.That(first.AllowSizeFallback, Is.True);
            Assert.That(first.AllowMdtmFallback, Is.True);
            Assert.That(first.AllowHistoricAliasFallback, Is.False);
            Assert.That(first.CommandsBeforeAuthorization, Is.Empty);
            Assert.That(first.CommandsBeforeAuthorization,
                Is.Not.SameAs(second.CommandsBeforeAuthorization));
            Assert.That(first.CommandsAfterAuthorization, Is.Empty);
            Assert.That(first.CommandsAfterAuthorization,
                Is.Not.SameAs(second.CommandsAfterAuthorization));
        });
    }

    [Test]
    public void Socket_options_retain_operating_system_defaults()
    {
        FtpSocketOptions options = new FtpSocketOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.NoDelay, Is.False);
            Assert.That(options.KeepAlive, Is.False);
            Assert.That(options.ReceiveBufferBytes, Is.Zero);
            Assert.That(options.SendBufferBytes, Is.Zero);
            Assert.That(options.ReceiveTimeoutMilliseconds, Is.Zero);
            Assert.That(options.SendTimeoutMilliseconds, Is.Zero);
            Assert.That(options.LingerEnabled, Is.False);
            Assert.That(options.LingerSeconds, Is.Zero);
        });
    }

    [Test]
    public void Data_connection_options_are_passive_first_and_restrict_peers()
    {
        FtpDataConnectionOptions options = new FtpDataConnectionOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(FtpDataConnectionMode.Passive));
            Assert.That(options.Socket, Is.Not.Null);
            Assert.That(options.UseExtendedPassive, Is.True);
            Assert.That(options.SendExtendedPassiveAll, Is.False);
            Assert.That(options.AllowPassiveFallback, Is.True);
            Assert.That(options.UseExtendedActive, Is.True);
            Assert.That(options.AllowActiveFallback, Is.True);
            Assert.That(options.IgnorePassiveAdvertisedAddress, Is.True);
            Assert.That(options.RejectPassiveAddressMismatch, Is.True);
            Assert.That(options.RequireDataPeerMatch, Is.True);
            Assert.That(options.AllowForeignDataConnections, Is.False);
            Assert.That(options.RejectPrivilegedActivePorts, Is.True);
            Assert.That(options.ActiveBindEndpoint, Is.Null);
            Assert.That(options.ActiveAdvertisedAddress, Is.Null);
            Assert.That(options.ActivePortStart, Is.Zero);
            Assert.That(options.ActivePortEnd, Is.Zero);
            Assert.That(options.PassivePortStart, Is.EqualTo(FtpDefaults.PassivePortStart));
            Assert.That(options.PassivePortEnd, Is.EqualTo(FtpDefaults.PassivePortEnd));
        });
    }

    [Test]
    public void Tls_options_require_normal_certificate_validation()
    {
        FtpTlsOptions options = new FtpTlsOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Protocols, Is.EqualTo(SslProtocols.None));
            Assert.That(options.CheckCertificateRevocation, Is.True);
            Assert.That(options.RequireCertificateNameMatch, Is.True);
            Assert.That(options.RequireCertificateChainTrust, Is.True);
            Assert.That(options.RequireClientCertificate, Is.False);
            Assert.That(options.RequireDataSessionReuse, Is.False);
            Assert.That(options.AllowClearCommandChannel, Is.False);
            Assert.That(options.DataProtectionLevel, Is.EqualTo(FtpDataProtectionLevel.Private));
            Assert.That(options.TargetHost, Is.Null);
            Assert.That(options.ClientCertificate, Is.Null);
            Assert.That(options.ServerCertificate, Is.Null);
        });
    }

    [Test]
    public void Listener_options_require_explicit_tls_and_own_nested_options()
    {
        FtpListenerOptions first = new FtpListenerOptions();
        FtpListenerOptions second = new FtpListenerOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.Endpoint, Is.Not.Null);
            Assert.That(first.Endpoint, Is.Not.SameAs(second.Endpoint));
            Assert.That(first.SecurityMode, Is.EqualTo(FtpSecurityMode.ExplicitTlsRequired));
            Assert.That(first.Tls, Is.Not.Null);
            Assert.That(first.Tls, Is.Not.SameAs(second.Tls));
            Assert.That(first.ControlSocket,
                Is.Not.SameAs(second.ControlSocket));
            Assert.That(first.Backlog, Is.EqualTo(128));
            Assert.That(first.DualStack, Is.True);
            Assert.That(first.AllowPlaintextCredentials, Is.False);
            Assert.That(first.PassiveAdvertisedAddress, Is.Null);
        });
    }

    [Test]
    public void Client_options_are_tls_required_passive_and_independently_owned()
    {
        FtpClientOptions first = new FtpClientOptions();
        FtpClientOptions second = new FtpClientOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.Endpoint, Is.Not.SameAs(second.Endpoint));
            Assert.That(first.LocalBindEndpoint, Is.Null);
            Assert.That(first.Credentials, Is.Not.SameAs(second.Credentials));
            Assert.That(first.Timeouts, Is.Not.SameAs(second.Timeouts));
            Assert.That(first.ControlSocket,
                Is.Not.SameAs(second.ControlSocket));
            Assert.That(first.DataConnection, Is.Not.SameAs(second.DataConnection));
            Assert.That(first.Tls, Is.Not.SameAs(second.Tls));
            Assert.That(first.Quirks, Is.Not.SameAs(second.Quirks));
            Assert.That(first.SecurityMode, Is.EqualTo(FtpSecurityMode.ExplicitTlsRequired));
            Assert.That(first.VirtualHost, Is.Null);
            Assert.That(first.NegotiateFeatures, Is.True);
            Assert.That(first.EnableUtf8, Is.True);
            Assert.That(first.EnablePret, Is.False);
            Assert.That(first.ClientName, Is.Null);
            Assert.That(first.AllowPlaintextCredentials, Is.False);
            Assert.That(first.StrictReplyParsing, Is.True);
            Assert.That(first.RedactSecrets, Is.True);
            Assert.That(first.RetryCount, Is.Zero);
        });
    }

    [Test]
    public void Virtual_host_options_disable_anonymous_access_by_default()
    {
        FtpVirtualHostOptions first = new FtpVirtualHostOptions();
        FtpVirtualHostOptions second = new FtpVirtualHostOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.HostName, Is.Empty);
            Assert.That(first.Greeting, Is.EqualTo("FTP service ready"));
            Assert.That(first.RootPath, Is.Empty);
            Assert.That(first.Tls, Is.Not.SameAs(second.Tls));
            Assert.That(first.FileSystem, Is.Null);
            Assert.That(first.Authenticator, Is.Null);
            Assert.That(first.Authorizer, Is.Null);
            Assert.That(first.CertificateProvider, Is.Null);
            Assert.That(first.Features, Is.Not.SameAs(second.Features));
            Assert.That(first.ExtensionHandlers, Is.Null);
            Assert.That(first.AllowAnonymous, Is.False);
            Assert.That(first.AllowAnonymousWrites, Is.False);
            Assert.That(first.MaximumSessions, Is.EqualTo(256));
            Assert.That(first.MaximumSessionsPerUser, Is.EqualTo(8));
        });
    }

    [Test]
    public void Server_options_start_empty_with_bounded_secure_limits()
    {
        FtpServerOptions first = new FtpServerOptions();
        FtpServerOptions second = new FtpServerOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.Listeners, Is.Empty);
            Assert.That(first.Listeners, Is.Not.SameAs(second.Listeners));
            Assert.That(first.VirtualHosts, Is.Empty);
            Assert.That(first.VirtualHosts, Is.Not.SameAs(second.VirtualHosts));
            Assert.That(first.Timeouts, Is.Not.SameAs(second.Timeouts));
            Assert.That(first.DataConnection, Is.Not.SameAs(second.DataConnection));
            Assert.That(first.Tls, Is.Not.SameAs(second.Tls));
            Assert.That(first.ExtensionHandlers, Is.Empty);
            Assert.That(first.ExtensionHandlers,
                Is.Not.SameAs(second.ExtensionHandlers));
            Assert.That(first.FixedUsers, Is.Null);
            Assert.That(first.DefaultHostName, Is.Null);
            Assert.That(first.MaximumPendingConnections, Is.EqualTo(128));
            Assert.That(first.MaximumSessions, Is.EqualTo(256));
            Assert.That(first.MaximumSessionsPerAddress, Is.EqualTo(16));
            Assert.That(first.MaximumSessionsPerUser, Is.EqualTo(8));
            Assert.That(first.MaximumSimultaneousTransfers, Is.EqualTo(64));
            Assert.That(first.MaximumCommandsPerMinute, Is.EqualTo(600));
            Assert.That(first.MaximumMalformedLines, Is.EqualTo(5));
            Assert.That(first.MaximumAuthenticationAttempts, Is.EqualTo(FtpDefaults.MaximumAuthenticationAttempts));
            Assert.That(first.MaximumListingEntryCount, Is.EqualTo(FtpDefaults.MaximumListingEntryCount));
            Assert.That(first.AllowForeignDataConnections, Is.False);
            Assert.That(first.EnableHistoricAliases, Is.False);
            Assert.That(first.EnableCompatibilityExtensions, Is.False);
            Assert.That(first.EnablePret, Is.False);
            Assert.That(first.EnableClnt, Is.False);
            Assert.That(first.EnableMfmt, Is.False);
            Assert.That(first.EnableModeZ, Is.False);
            Assert.That(first.EnableHash, Is.False);
            Assert.That(first.EnableMd5, Is.False);
            Assert.That(first.EnableXmd5, Is.False);
            Assert.That(first.EnableXsha1, Is.False);
            Assert.That(first.EnableXsha256, Is.False);
            Assert.That(first.EnableXsha512, Is.False);
            Assert.That(first.EnableXcrc, Is.False);
            Assert.That(first.RedactSecrets, Is.True);
        });
    }
}
