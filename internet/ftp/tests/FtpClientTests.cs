using System.Security.Authentication;
using System.Threading;

namespace ftp.Tests;

public class FtpClientTests
{
    [Test]
    public void Connect_retains_a_deep_validated_option_snapshot()
    {
        FtpClientOptions options = CreateSecureOptions("ftp.example");
        options.Endpoint.Port = 2121;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv6;
        options.Credentials.UserName = "user";
        options.Credentials.Password = "secret";
        options.Credentials.Account = "billing";
        options.Timeouts.ConnectTimeoutMilliseconds = 4321;
        options.Timeouts.CancellationToken = new CancellationToken(true);
        options.ControlSocket.NoDelay = true;
        options.ControlSocket.ReceiveBufferBytes = 4096;
        options.DataConnection.Mode = FtpDataConnectionMode.Active;
        options.DataConnection.Socket.KeepAlive = true;
        options.DataConnection.ActiveBindEndpoint = new FtpEndpoint();
        options.DataConnection.ActiveBindEndpoint.Host = "::1";
        options.DataConnection.ActiveBindEndpoint.Port = 0;
        options.DataConnection.ActiveBindEndpoint.AddressFamily = FtpAddressFamily.IPv6;
        options.DataConnection.ActiveAdvertisedAddress = "2001:db8::1";
        options.Tls.Protocols = SslProtocols.Tls13;
        options.Tls.TargetHost = "certificate.example";
        options.VirtualHost = "tenant.example";
        options.RetryCount = 3;

        FtpClient client = new FtpClient(new StrictFtpCertificateValidator());

        Assert.Throws<OperationCanceledException>(() => client.Connect(options));
        FtpClientOptions snapshot = client.State.Options!;

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Is.Not.SameAs(options));
            Assert.That(snapshot.Endpoint, Is.Not.SameAs(options.Endpoint));
            Assert.That(snapshot.Credentials, Is.Not.SameAs(options.Credentials));
            Assert.That(snapshot.Timeouts, Is.Not.SameAs(options.Timeouts));
            Assert.That(snapshot.ControlSocket, Is.Not.SameAs(options.ControlSocket));
            Assert.That(snapshot.DataConnection, Is.Not.SameAs(options.DataConnection));
            Assert.That(snapshot.DataConnection.Socket, Is.Not.SameAs(options.DataConnection.Socket));
            Assert.That(snapshot.DataConnection.ActiveBindEndpoint, Is.Not.SameAs(options.DataConnection.ActiveBindEndpoint));
            Assert.That(snapshot.Tls, Is.Not.SameAs(options.Tls));
            Assert.That(snapshot.Endpoint.Host, Is.EqualTo("ftp.example"));
            Assert.That(snapshot.Endpoint.Port, Is.EqualTo(2121));
            Assert.That(snapshot.Endpoint.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv6));
            Assert.That(snapshot.Credentials.UserName, Is.EqualTo("user"));
            Assert.That(snapshot.Credentials.Password, Is.EqualTo("secret"));
            Assert.That(snapshot.Credentials.Account, Is.EqualTo("billing"));
            Assert.That(snapshot.Timeouts.ConnectTimeoutMilliseconds, Is.EqualTo(4321));
            Assert.That(snapshot.Timeouts.CancellationToken.IsCancellationRequested, Is.True);
            Assert.That(snapshot.ControlSocket.NoDelay, Is.True);
            Assert.That(snapshot.ControlSocket.ReceiveBufferBytes, Is.EqualTo(4096));
            Assert.That(snapshot.DataConnection.Mode, Is.EqualTo(FtpDataConnectionMode.Active));
            Assert.That(snapshot.DataConnection.Socket.KeepAlive, Is.True);
            Assert.That(snapshot.DataConnection.ActiveBindEndpoint!.Host, Is.EqualTo("::1"));
            Assert.That(snapshot.DataConnection.ActiveAdvertisedAddress, Is.EqualTo("2001:db8::1"));
            Assert.That(snapshot.Tls.Protocols, Is.EqualTo(SslProtocols.Tls13));
            Assert.That(snapshot.Tls.TargetHost, Is.EqualTo("certificate.example"));
            Assert.That(snapshot.VirtualHost, Is.EqualTo("tenant.example"));
            Assert.That(snapshot.RetryCount, Is.EqualTo(3));
            Assert.That(client.State.ConnectRequested, Is.True);
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Disconnected));
        });

        options.Endpoint.Host = "changed.example";
        options.Credentials.Password = "changed";
        options.Timeouts.ConnectTimeoutMilliseconds = 9999;
        options.ControlSocket.NoDelay = false;
        options.DataConnection.Socket.KeepAlive = false;
        options.DataConnection.ActiveBindEndpoint.Host = "127.0.0.1";
        options.Tls.TargetHost = "changed-certificate.example";
        options.VirtualHost = "changed-tenant.example";

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Endpoint.Host, Is.EqualTo("ftp.example"));
            Assert.That(snapshot.Credentials.Password, Is.EqualTo("secret"));
            Assert.That(snapshot.Timeouts.ConnectTimeoutMilliseconds, Is.EqualTo(4321));
            Assert.That(snapshot.ControlSocket.NoDelay, Is.True);
            Assert.That(snapshot.DataConnection.Socket.KeepAlive, Is.True);
            Assert.That(snapshot.DataConnection.ActiveBindEndpoint!.Host, Is.EqualTo("::1"));
            Assert.That(snapshot.Tls.TargetHost, Is.EqualTo("certificate.example"));
            Assert.That(snapshot.VirtualHost, Is.EqualTo("tenant.example"));
        });
    }

    [Test]
    public void Connect_rejects_invalid_or_unbound_secure_configuration_atomically()
    {
        FtpClient client = new FtpClient(new StrictFtpCertificateValidator());
        FtpClientOptions valid = CreateSecureOptions("first.example");
        valid.Timeouts.CancellationToken = new CancellationToken(true);
        Assert.Throws<OperationCanceledException>(() => client.Connect(valid));
        FtpClientOptions retained = client.State.Options!;

        FtpClientOptions invalid = CreateSecureOptions("");
        Assert.Throws<FtpConfigurationException>(() => client.Connect(invalid));

        FtpClient unboundSecureClient = new FtpClient(null);
        FtpClientOptions secure = CreateSecureOptions("secure.example");
        Assert.Throws<FtpConfigurationException>(() => unboundSecureClient.Connect(secure));

        Assert.Multiple(() =>
        {
            Assert.That(client.State.Options, Is.SameAs(retained));
            Assert.That(retained.Endpoint.Host, Is.EqualTo("first.example"));
            Assert.That(unboundSecureClient.State.Options, Is.Null);
            Assert.That(unboundSecureClient.State.ConnectRequested, Is.False);
        });
    }

    [Test]
    public void Plain_configuration_can_snapshot_without_a_certificate_validator()
    {
        FtpClientOptions options = CreateSecureOptions("plain.example");
        options.SecurityMode = FtpSecurityMode.Plain;
        FtpClientOptions snapshot = FtpClientOptionsSnapshot.CreateValidated(options, null);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Is.Not.SameAs(options));
            Assert.That(snapshot.SecurityMode, Is.EqualTo(FtpSecurityMode.Plain));
            Assert.That(snapshot.Endpoint.Host, Is.EqualTo("plain.example"));
        });
    }

    [Test]
    public void Probe_validates_an_isolated_configuration_without_replacing_reconnect_state()
    {
        FtpClient client = new FtpClient(new StrictFtpCertificateValidator());
        FtpClientOptions connected = CreateSecureOptions("retained.example");
        FtpClientOptions retained = FtpClientOptionsSnapshot.CreateValidated(
            connected, new StrictFtpCertificateValidator());
        client.State.Options = retained;
        FtpClientOptions probeOptions = CreateSecureOptions("probe.example");
        probeOptions.Timeouts.CancellationToken = new CancellationToken(true);

        FtpConnectionProbeResult result = client.ProbeConnectionProfiles(probeOptions);
        probeOptions.Endpoint.Host = "changed.example";

        Assert.Multiple(() =>
        {
            Assert.That(result.Complete, Is.True);
            Assert.That(result.Profiles, Has.Length.EqualTo(3));
            Assert.That(result.Profiles, Has.All.Matches<FtpConnectionProfile>(profile => !profile.Viable));
            Assert.That(result.FailureMessage, Does.Contain("No credential-free"));
            Assert.That(client.State.Options, Is.SameAs(retained));
            Assert.That(retained.Endpoint.Host, Is.EqualTo("retained.example"));
            Assert.That(client.State.LastOperation, Is.EqualTo("ProbeConnectionProfiles"));
        });
    }

    [Test]
    public void Public_operations_record_dispatch_and_enforce_connection_state()
    {
        FtpClient client = new FtpClient(new StrictFtpCertificateValidator());

        FtpProtocolException closedFailure = Assert.Throws<FtpProtocolException>(
            () => client.RequestSystemType())!;
        Assert.That(closedFailure.Message, Does.Contain("requires an open session"));

        client.State.Session.OpenConnection();
        FtpConnectionException commandFailure = Assert.Throws<FtpConnectionException>(
            () => client.ExecuteCommand(new FtpCommand()))!;
        FtpConnectionException listingFailure = Assert.Throws<FtpConnectionException>(
            () => client.ListDirectory("/"))!;
        FtpProtocolException transferFailure = Assert.Throws<FtpProtocolException>(
            () => client.Transfer(new FtpTransferRequest()))!;

        Assert.Multiple(() =>
        {
            Assert.That(commandFailure.Message, Does.Contain("open configured"));
            Assert.That(listingFailure.Message, Does.Contain("open configured"));
            Assert.That(transferFailure.Message, Does.Contain("Transfer"));
            Assert.That(client.State.LastOperation, Is.EqualTo("Transfer"));
            Assert.That(client.State.DispatchCount, Is.EqualTo(4));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Connected));
        });

        client.Disconnect();
        Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
    }

    [Test]
    public void Disconnect_abort_marker_and_reconnect_use_central_state()
    {
        FtpClient client = new FtpClient(new StrictFtpCertificateValidator());
        Assert.DoesNotThrow(() => client.Disconnect());
        Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Disconnected));

        Assert.Throws<FtpConnectionException>(() => client.Reconnect());
        FtpClientOptions options = CreateSecureOptions("reconnect.example");
        options.Timeouts.CancellationToken = new CancellationToken(true);
        Assert.Throws<OperationCanceledException>(() => client.Connect(options));
        options.Endpoint.Host = "changed.example";
        Assert.Throws<OperationCanceledException>(() => client.Reconnect());

        client.State.LatestRestartMarker = "offset=8192";
        client.State.Session.OpenConnection();
        client.AbortConnection();

        Assert.Multiple(() =>
        {
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(client.GetLatestRestartMarker(), Is.EqualTo("offset=8192"));
            Assert.That(client.State.Options!.Endpoint.Host, Is.EqualTo("reconnect.example"));
        });
    }

    private static FtpClientOptions CreateSecureOptions(string host)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = host;
        return options;
    }
}
