namespace sip.Tests;

public class TlsSipTransportTests
{
    [Test]
    public void BindsAsSecureTlsAndRejectsPlainTransportSend()
    {
        TlsSipTransport transport = new TlsSipTransport();
        SipEndpointAddress local = Bind(transport, null, null);
        SipTransportEvent bound = transport.PollTransportEvent();
        SipSendRequest request = new SipSendRequest();
        request.Bytes = RequestBytes();
        request.Transport = SipTransportKind.Tcp;
        request.CorrelationId = "plain-send";

        SipSendResult result = transport.SendTransport(request);
        SipTransportEvent failed = transport.PollTransportEvent();
        transport.CloseTransport();
        SipTransportEvent closed = transport.PollTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(local.Transport, Is.EqualTo(SipTransportKind.Tls));
            Assert.That(local.Secure, Is.True);
            Assert.That(bound.Kind, Is.EqualTo(SipTransportEventKind.Bound));
            Assert.That(bound.Transport, Is.EqualTo(SipTransportKind.Tls));
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("other than TLS"));
            Assert.That(failed.Kind, Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(failed.CorrelationId, Is.EqualTo("plain-send"));
            Assert.That(closed.Kind, Is.EqualTo(SipTransportEventKind.Closed));
        });
    }

    [Test]
    public void ValidatesCustomRootAndServerName()
    {
        using OpenSslTestServer server = OpenSslTestServer.Start(false);
        TlsSipTransport transport = new TlsSipTransport();
        Bind(transport, server.RootCertificate(), "sip.example.com");
        transport.PollTransportEvent();

        SipConnectionInfo connection = transport.ConnectTransport(server.Endpoint());
        SipTransportEvent connected = transport.PollTransportEvent();
        SipConnectionInfo reused = transport.ConnectTransport(server.Endpoint());
        SipSendRequest request = new SipSendRequest();
        request.Bytes = RequestBytes();
        request.ConnectionId = connection.Id;
        request.Transport = SipTransportKind.Tls;
        request.CorrelationId = "secure-send";
        SipSendResult sent = transport.SendTransport(request);
        SipTransportEvent sentEvent = PollUntil(transport,
            SipTransportEventKind.MessageSent);
        SipMessage crossDomainMessage = new SipMessage();
        crossDomainMessage.Kind = SipMessageKind.Request;
        crossDomainMessage.RequestUri = new SipUri();
        crossDomainMessage.RequestUri.Scheme = SipUriScheme.Sips;
        crossDomainMessage.RequestUri.Host = "other.example.com";
        SipSendRequest crossDomainRequest = new SipSendRequest();
        crossDomainRequest.Message = crossDomainMessage;
        crossDomainRequest.Bytes = RequestBytes();
        crossDomainRequest.ConnectionId = connection.Id;
        crossDomainRequest.Transport = SipTransportKind.Tls;
        SipSendResult crossDomain = transport.SendTransport(crossDomainRequest);

        Assert.Multiple(() =>
        {
            Assert.That(connection.State, Is.EqualTo(SipConnectionState.Connected));
            Assert.That(connection.Transport, Is.EqualTo(SipTransportKind.Tls));
            Assert.That(connection.Secure, Is.True);
            Assert.That(connection.PeerIdentity, Is.EqualTo("sip.example.com"));
            Assert.That(connection.CertificateIdentity, Is.Not.Null);
            Assert.That(connection.CertificateIdentity!.ChainValid, Is.True);
            Assert.That(connection.CertificateIdentity.NameValid, Is.True);
            Assert.That(connection.CertificateIdentity.Domains,
                Does.Contain("sip.example.com"));
            Assert.That(connection.CertificateIdentity.Domains,
                Does.Contain("valid.example.com"));
            Assert.That(connection.CertificateIdentity.CertificateBytes, Is.Not.Empty);
            Assert.That(connected.Kind, Is.EqualTo(SipTransportEventKind.Connected));
            Assert.That(connected.Connection!.Id, Is.EqualTo(connection.Id));
            Assert.That(reused.Id, Is.EqualTo(connection.Id));
            Assert.That(sent.Success, Is.True, sent.ErrorMessage);
            Assert.That(sent.Connection!.Secure, Is.True);
            Assert.That(sentEvent.CorrelationId, Is.EqualTo("secure-send"));
            Assert.That(crossDomain.Success, Is.False);
            Assert.That(crossDomain.ErrorMessage,
                Does.Contain("not authenticated for the destination domain"));
        });
        transport.CloseTransport();
    }

    [Test]
    public void RejectsWrongNameAndUntrustedChain()
    {
        using OpenSslTestServer wrongNameServer = OpenSslTestServer.Start(false);
        TlsSipTransport wrongNameTransport = new TlsSipTransport();
        Bind(wrongNameTransport, wrongNameServer.RootCertificate(),
            "wrong.example.com");
        wrongNameTransport.PollTransportEvent();

        SipTransportException? wrongName = Assert.Throws<SipTransportException>(
            () => wrongNameTransport.ConnectTransport(wrongNameServer.Endpoint()));
        wrongNameTransport.CloseTransport();

        using OpenSslTestServer untrustedServer = OpenSslTestServer.Start(false);
        TlsSipTransport untrustedTransport = new TlsSipTransport();
        Bind(untrustedTransport, null, "sip.example.com");
        untrustedTransport.PollTransportEvent();

        SipTransportException? untrusted = Assert.Throws<SipTransportException>(
            () => untrustedTransport.ConnectTransport(untrustedServer.Endpoint()));
        untrustedTransport.CloseTransport();

        Assert.Multiple(() =>
        {
            Assert.That(wrongName!.Message, Does.Contain("authentication failed"));
            Assert.That(untrusted!.Message, Does.Contain("authentication failed"));
        });
    }

    [Test]
    public void RejectsCertificateWithoutServerAuthenticationUsage()
    {
        using OpenSslTestServer server = OpenSslTestServer.Start(true);
        TlsSipTransport transport = new TlsSipTransport();
        Bind(transport, server.RootCertificate(), "client.example.com");
        transport.PollTransportEvent();

        SipTransportException? failure = Assert.Throws<SipTransportException>(
            () => transport.ConnectTransport(server.Endpoint()));
        transport.CloseTransport();

        Assert.That(failure!.Message, Does.Contain("authentication failed"));
    }

    [Test]
    public void PrefersSipUriIdentityAndNeverExpandsCertificateWildcard()
    {
        using OpenSslTestServer uriServer =
            OpenSslTestServer.StartWithSubjectAlternativeNames(
                "legacy.example.com",
                "URI:sip:service.example.com,DNS:ignored.example.com");
        TlsSipTransport uriTransport = new TlsSipTransport();
        Bind(uriTransport, uriServer.RootCertificate(), "service.example.com");
        uriTransport.PollTransportEvent();

        SipConnectionInfo accepted = uriTransport.ConnectTransport(uriServer.Endpoint());
        uriTransport.CloseTransport();

        using OpenSslTestServer wildcardServer =
            OpenSslTestServer.StartWithSubjectAlternativeNames(
                "legacy.example.com", "DNS:*.example.com");
        TlsSipTransport wildcardTransport = new TlsSipTransport();
        Bind(wildcardTransport, wildcardServer.RootCertificate(),
            "node.example.com");
        wildcardTransport.PollTransportEvent();
        SipTransportException? wildcard = Assert.Throws<SipTransportException>(
            () => wildcardTransport.ConnectTransport(wildcardServer.Endpoint()));
        wildcardTransport.CloseTransport();

        Assert.Multiple(() =>
        {
            Assert.That(accepted.PeerIdentity, Is.EqualTo("service.example.com"));
            Assert.That(accepted.CertificateIdentity!.Domains,
                Is.EqualTo(new string[] { "service.example.com" }));
            Assert.That(wildcard!.Message, Does.Contain("authentication failed"));
        });
    }

    [Test]
    public void AcceptsAValidIntermediateChainAndRejectsAnExpiredLeaf()
    {
        using OpenSslTestServer chainedServer = OpenSslTestServer.StartWithIntermediate();
        TlsSipTransport chainedTransport = new TlsSipTransport();
        Bind(chainedTransport, chainedServer.RootCertificate(), "sip.example.com");
        chainedTransport.PollTransportEvent();

        SipConnectionInfo chained = chainedTransport.ConnectTransport(
            chainedServer.Endpoint());
        chainedTransport.CloseTransport();

        using OpenSslTestServer expiredServer = OpenSslTestServer.StartExpired();
        TlsSipTransport expiredTransport = new TlsSipTransport();
        Bind(expiredTransport, expiredServer.RootCertificate(), "sip.example.com");
        expiredTransport.PollTransportEvent();
        SipTransportException? expired = Assert.Throws<SipTransportException>(
            () => expiredTransport.ConnectTransport(expiredServer.Endpoint()));
        expiredTransport.CloseTransport();

        Assert.Multiple(() =>
        {
            Assert.That(chained.State, Is.EqualTo(SipConnectionState.Connected));
            Assert.That(chained.CertificateIdentity!.ChainValid, Is.True);
            Assert.That(chained.CertificateIdentity.NameValid, Is.True);
            Assert.That(expired!.Message, Does.Contain("authentication failed"));
        });
    }

    [Test]
    public void CertificatePolicyCanPermitAnUntrustedPeerWithoutClaimingIdentity()
    {
        using OpenSslTestServer server = OpenSslTestServer.Start(false);
        TlsSipTransport transport = new TlsSipTransport();
        BindConfigured(transport, null, "wrong.example.com", null, null,
            false, false);
        transport.PollTransportEvent();

        SipConnectionInfo connection = transport.ConnectTransport(server.Endpoint());

        Assert.Multiple(() =>
        {
            Assert.That(connection.State, Is.EqualTo(SipConnectionState.Connected));
            Assert.That(connection.Secure, Is.True);
            Assert.That(connection.PeerIdentity, Is.Null);
            Assert.That(connection.CertificateIdentity, Is.Not.Null);
            Assert.That(connection.CertificateIdentity!.ChainValid, Is.False);
            Assert.That(connection.CertificateIdentity.NameValid, Is.False);
            Assert.That(connection.CertificateIdentity.MutualTls, Is.False);
            Assert.That(connection.CertificateIdentity.CertificateBytes, Is.Not.Empty);
        });
        transport.CloseTransport();
    }

    [Test]
    public void DistinguishesConfiguredClientIdentityFromNegotiatedMutualTls()
    {
        using OpenSslTestServer server = OpenSslTestServer.StartWithClientIdentity(false);
        TlsSipTransport transport = new TlsSipTransport();
        BindClientIdentityOrIgnore(transport, server, false);
        transport.PollTransportEvent();

        SipConnectionInfo connection = transport.ConnectTransport(server.Endpoint());

        Assert.That(connection.CertificateIdentity!.MutualTls, Is.False);
        transport.CloseTransport();
    }

    [Test]
    public void MutualTlsRejectsMissingClientIdentityAndAcceptsClientAuthenticationUsage()
    {
        using OpenSslTestServer server = OpenSslTestServer.StartWithClientIdentity(true);
        TlsSipTransport missingTransport = new TlsSipTransport();
        Bind(missingTransport, server.RootCertificate(), "sip.example.com");
        missingTransport.PollTransportEvent();
        SipTransportException? missing = Assert.Throws<SipTransportException>(
            () => missingTransport.ConnectTransport(server.Endpoint()));
        missingTransport.CloseTransport();

        TlsSipTransport authenticatedTransport = new TlsSipTransport();
        BindClientIdentityOrIgnore(authenticatedTransport, server, false);
        authenticatedTransport.PollTransportEvent();
        SipConnectionInfo authenticated = authenticatedTransport.ConnectTransport(
            server.Endpoint());

        Assert.Multiple(() =>
        {
            Assert.That(missing!.Message, Does.Contain("authentication failed"));
            Assert.That(authenticated.State, Is.EqualTo(SipConnectionState.Connected));
            Assert.That(authenticated.CertificateIdentity!.MutualTls, Is.True);
            Assert.That(authenticated.CertificateIdentity.ChainValid, Is.True);
            Assert.That(authenticated.PeerIdentity, Is.EqualTo("sip.example.com"));
        });
        authenticatedTransport.CloseTransport();
    }

    [Test]
    public void ReportsListenerHandshakeFailureAndOrderlyActiveShutdown()
    {
        TlsSipTransport listener = new TlsSipTransport();
        SipEndpointAddress listenerAddress = Bind(listener, null, null);
        listener.PollTransportEvent();
        using System.Net.Sockets.TcpClient rawClient = new System.Net.Sockets.TcpClient(
            System.Net.Sockets.AddressFamily.InterNetwork);
        rawClient.Connect(System.Net.IPAddress.Loopback, listenerAddress.Port);
        SipTransportEvent handshakeFailure = PollUntil(listener,
            SipTransportEventKind.Disconnected);
        listener.CloseTransport();
        SipTransportEvent listenerClosed = PollUntil(listener,
            SipTransportEventKind.Closed);

        using OpenSslTestServer server = OpenSslTestServer.Start(false);
        TlsSipTransport active = new TlsSipTransport();
        Bind(active, server.RootCertificate(), "sip.example.com");
        active.PollTransportEvent();
        SipConnectionInfo connection = active.ConnectTransport(server.Endpoint());
        active.PollTransportEvent();
        active.CloseTransport();
        active.CloseTransport();
        SipTransportEvent disconnected = PollUntil(active,
            SipTransportEventKind.Disconnected);
        SipTransportEvent closed = PollUntil(active,
            SipTransportEventKind.Closed);

        Assert.Multiple(() =>
        {
            Assert.That(handshakeFailure.ErrorMessage,
                Does.Contain("no server certificate"));
            Assert.That(listenerClosed.Kind, Is.EqualTo(SipTransportEventKind.Closed));
            Assert.That(disconnected.Connection!.Id, Is.EqualTo(connection.Id));
            Assert.That(disconnected.Connection.State,
                Is.EqualTo(SipConnectionState.Closed));
            Assert.That(disconnected.ErrorMessage, Does.Contain("transport was closed"));
            Assert.That(closed.Kind, Is.EqualTo(SipTransportEventKind.Closed));
            Assert.That(active.PollTransportEvent().Kind,
                Is.EqualTo(SipTransportEventKind.None));
        });
    }

    static SipEndpointAddress Bind(TlsSipTransport transport, byte[]? trustedRoot,
        string? serverName)
    {
        return BindConfigured(transport, trustedRoot, serverName, null, null,
            true, false);
    }

    static SipEndpointAddress BindConfigured(TlsSipTransport transport,
        byte[]? trustedRoot, string? serverName, byte[]? certificate,
        byte[]? privateKey, bool validatePeer, bool requireClientCertificate)
    {
        SipEndpointAddress local = new SipEndpointAddress();
        local.Host = "127.0.0.1";
        local.Port = 0;
        SipTransportOptions options = new SipTransportOptions();
        options.Kind = SipTransportKind.Tls;
        options.LocalAddress = local;
        options.MaxQueueCount = 8;
        options.IdleTimeoutMilliseconds = 60000;
        SipTlsOptions tls = new SipTlsOptions();
        tls.ServerName = serverName;
        tls.CertificateBytes = certificate == null ? new byte[0] : certificate;
        tls.PrivateKeyBytes = privateKey == null ? new byte[0] : privateKey;
        tls.TrustedRoots = trustedRoot == null
            ? new byte[0][] : new byte[][] { trustedRoot };
        tls.ValidatePeer = validatePeer;
        tls.RequireClientCertificate = requireClientCertificate;
        return transport.BindTransport(options, tls, new SipWebSocketOptions());
    }

    static void BindClientIdentityOrIgnore(TlsSipTransport transport,
        OpenSslTestServer server, bool requireClientCertificate)
    {
        try
        {
            BindConfigured(transport, server.RootCertificate(), "sip.example.com",
                server.ClientIdentity(), null, true, requireClientCertificate);
        }
        catch (System.Security.Cryptography.CryptographicException exception)
        {
            if (!System.OperatingSystem.IsMacOS())
            {
                throw;
            }
            Assert.Ignore("The macOS test sandbox cannot create the temporary keychain "
                + "required by SslStream client identities: " + exception.Message);
        }
    }

    static byte[] RequestBytes()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "OPTIONS sips:user@example.com SIP/2.0\r\nContent-Length: 0\r\n\r\n");
    }

    static SipTransportEvent PollUntil(TlsSipTransport transport,
        SipTransportEventKind kind)
    {
        long deadline = System.Environment.TickCount64 + 5000;
        SipTransportEvent result = transport.PollTransportEvent();
        while (result.Kind != kind && System.Environment.TickCount64 < deadline)
        {
            System.Threading.Thread.Sleep(2);
            result = transport.PollTransportEvent();
        }
        Assert.That(result.Kind, Is.EqualTo(kind), result.ErrorMessage);
        return result;
    }

    class OpenSslTestServer : System.IDisposable
    {
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        string directory = "";
        string rootPath = "";
        string clientIdentityPath = "";
        string clientKeyPath = "";
        int port;

        internal static OpenSslTestServer Start(bool clientUsageOnly)
        {
            OpenSslTestServer result = new OpenSslTestServer();
            result.StartServer(clientUsageOnly
                ? "client.example.com" : "sip.example.com",
                clientUsageOnly
                    ? "subjectAltName=DNS:client.example.com"
                    : "subjectAltName=DNS:sip.example.com,DNS:valid.example.com",
                clientUsageOnly, 30, false, false, false);
            return result;
        }

        internal static OpenSslTestServer StartWithSubjectAlternativeNames(
            string commonName, string names)
        {
            OpenSslTestServer result = new OpenSslTestServer();
            result.StartServer(commonName, "subjectAltName=" + names,
                false, 30, false, false, false);
            return result;
        }

        internal static OpenSslTestServer StartWithIntermediate()
        {
            OpenSslTestServer result = new OpenSslTestServer();
            result.StartServer("sip.example.com",
                "subjectAltName=DNS:sip.example.com",
                false, 30, true, false, false);
            return result;
        }

        internal static OpenSslTestServer StartExpired()
        {
            OpenSslTestServer result = new OpenSslTestServer();
            result.StartServer("sip.example.com",
                "subjectAltName=DNS:sip.example.com",
                false, 0, false, false, false);
            return result;
        }

        internal static OpenSslTestServer StartWithClientIdentity(bool requireClient)
        {
            OpenSslTestServer result = new OpenSslTestServer();
            result.StartServer("sip.example.com",
                "subjectAltName=DNS:sip.example.com",
                false, 30, false, true, requireClient);
            return result;
        }

        internal SipEndpointAddress Endpoint()
        {
            SipEndpointAddress result = new SipEndpointAddress();
            result.Host = "127.0.0.1";
            result.Port = port;
            result.Transport = SipTransportKind.Tls;
            result.Secure = true;
            return result;
        }

        internal byte[] RootCertificate()
        {
            return System.IO.File.ReadAllBytes(rootPath);
        }

        internal byte[] ClientIdentity()
        {
            return System.IO.File.ReadAllBytes(clientIdentityPath);
        }

        void StartServer(string commonName, string names,
            bool clientUsageOnly, int leafDays, bool useIntermediate,
            bool createClientIdentity, bool requireClientIdentity)
        {
            System.Net.Sockets.TcpListener reservation =
                new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            reservation.Start();
            port = ((System.Net.IPEndPoint)reservation.LocalEndpoint).Port;
            reservation.Stop();
            directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "sip-tls-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            rootPath = System.IO.Path.Combine(directory, "root.pem");
            string rootKeyPath = System.IO.Path.Combine(directory, "root.key");
            string identityPath = System.IO.Path.Combine(directory, "identity.pem");
            string identityKeyPath = System.IO.Path.Combine(directory, "identity.key");
            string requestPath = System.IO.Path.Combine(directory, "identity.csr");
            string issuerPath = rootPath;
            string issuerKeyPath = rootKeyPath;
            string chainPath = "";
            RunOpenSsl(new string[] { "req", "-x509", "-newkey", "rsa:2048",
                "-nodes", "-keyout", rootKeyPath, "-out", rootPath, "-days", "30",
                "-subj", "/CN=SIP Test Root",
                "-addext", "basicConstraints=critical,CA:TRUE",
                "-addext", "keyUsage=critical,keyCertSign,cRLSign" });
            if (useIntermediate)
            {
                chainPath = System.IO.Path.Combine(directory, "intermediate.pem");
                string intermediateKeyPath = System.IO.Path.Combine(
                    directory, "intermediate.key");
                string intermediateRequestPath = System.IO.Path.Combine(
                    directory, "intermediate.csr");
                RunOpenSsl(new string[] { "req", "-newkey", "rsa:2048", "-nodes",
                    "-keyout", intermediateKeyPath, "-out", intermediateRequestPath,
                    "-subj", "/CN=SIP Test Intermediate",
                    "-addext", "basicConstraints=critical,CA:TRUE,pathlen:0",
                    "-addext", "keyUsage=critical,keyCertSign,cRLSign" });
                RunOpenSsl(new string[] { "x509", "-req", "-in", intermediateRequestPath,
                    "-CA", rootPath, "-CAkey", rootKeyPath, "-CAcreateserial",
                    "-out", chainPath, "-days", "30", "-copy_extensions", "copy" });
                issuerPath = chainPath;
                issuerKeyPath = intermediateKeyPath;
            }
            string usage = clientUsageOnly ? "clientAuth" : "serverAuth";
            RunOpenSsl(new string[] { "req", "-newkey", "rsa:2048", "-nodes",
                "-keyout", identityKeyPath, "-out", requestPath,
                "-subj", "/CN=" + commonName,
                "-addext", "basicConstraints=critical,CA:FALSE",
                "-addext", "keyUsage=critical,digitalSignature,keyEncipherment",
                "-addext", "extendedKeyUsage=" + usage, "-addext", names });
            RunOpenSsl(new string[] { "x509", "-req", "-in", requestPath,
                "-CA", issuerPath, "-CAkey", issuerKeyPath, "-CAcreateserial",
                "-out", identityPath, "-days", leafDays.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                "-copy_extensions", "copy" });
            if (createClientIdentity)
            {
                string clientPath = System.IO.Path.Combine(directory, "client.pem");
                clientIdentityPath = System.IO.Path.Combine(directory, "client.p12");
                clientKeyPath = System.IO.Path.Combine(directory, "client.key");
                string clientRequestPath = System.IO.Path.Combine(directory, "client.csr");
                RunOpenSsl(new string[] { "req", "-newkey", "rsa:2048", "-nodes",
                    "-keyout", clientKeyPath, "-out", clientRequestPath,
                    "-subj", "/CN=client.example.com",
                    "-addext", "basicConstraints=critical,CA:FALSE",
                    "-addext", "keyUsage=critical,digitalSignature,keyEncipherment",
                    "-addext", "extendedKeyUsage=clientAuth",
                    "-addext", "subjectAltName=DNS:client.example.com" });
                RunOpenSsl(new string[] { "x509", "-req", "-in", clientRequestPath,
                    "-CA", rootPath, "-CAkey", rootKeyPath, "-CAserial",
                    System.IO.Path.Combine(directory, "root.srl"),
                    "-out", clientPath, "-days", "30", "-copy_extensions", "copy" });
                RunOpenSsl(new string[] { "pkcs12", "-export", "-out", clientIdentityPath,
                    "-inkey", clientKeyPath, "-in", clientPath, "-passout", "pass:" });
            }
            System.Diagnostics.ProcessStartInfo start =
                new System.Diagnostics.ProcessStartInfo();
            start.FileName = "openssl";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.ArgumentList.Add("s_server");
            start.ArgumentList.Add("-accept");
            start.ArgumentList.Add(port.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            start.ArgumentList.Add("-cert");
            start.ArgumentList.Add(identityPath);
            start.ArgumentList.Add("-key");
            start.ArgumentList.Add(identityKeyPath);
            if (useIntermediate)
            {
                start.ArgumentList.Add("-cert_chain");
                start.ArgumentList.Add(chainPath);
            }
            if (requireClientIdentity)
            {
                start.ArgumentList.Add("-Verify");
                start.ArgumentList.Add("1");
                start.ArgumentList.Add("-verify_return_error");
                start.ArgumentList.Add("-CAfile");
                start.ArgumentList.Add(rootPath);
            }
            start.ArgumentList.Add("-quiet");
            start.ArgumentList.Add("-www");
            process = System.Diagnostics.Process.Start(start)!;
            System.Threading.Thread.Sleep(100);
            if (process.HasExited)
            {
                throw new System.InvalidOperationException(
                    "OpenSSL test server failed: " + process.StandardError.ReadToEnd());
            }
        }

        static void RunOpenSsl(string[] arguments)
        {
            System.Diagnostics.ProcessStartInfo start =
                new System.Diagnostics.ProcessStartInfo();
            start.FileName = "openssl";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            int index = 0;
            while (index < arguments.Length)
            {
                start.ArgumentList.Add(arguments[index]);
                index = index + 1;
            }
            using System.Diagnostics.Process? command =
                System.Diagnostics.Process.Start(start);
            if (command == null || !command.WaitForExit(10000)
                || command.ExitCode != 0)
            {
                string error = command == null
                    ? "could not start" : command.StandardError.ReadToEnd();
                if (command != null && !command.HasExited)
                {
                    command.Kill(true);
                }
                throw new System.InvalidOperationException(
                    "OpenSSL certificate setup failed: " + error);
            }
        }

        public void Dispose()
        {
            if (!process.HasExited)
            {
                process.Kill(true);
                process.WaitForExit(2000);
            }
            process.Dispose();
            if (directory.Length > 0 && System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.Delete(directory, true);
            }
        }
    }
}
