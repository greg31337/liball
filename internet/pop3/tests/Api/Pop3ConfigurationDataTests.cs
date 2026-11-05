using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace pop3.Tests;

public class Pop3ConfigurationDataTests
{
    [Test]
    public void ClientOptionsUseProtocolDefaultsAndIndependentNestedData()
    {
        Pop3ClientOptions first = new Pop3ClientOptions();
        Pop3ClientOptions second = new Pop3ClientOptions();

        Assert.That(first.Host, Is.Empty);
        Assert.That(first.Port, Is.EqualTo(Pop3Defaults.DefaultPort));
        Assert.That(first.SecurityMode, Is.EqualTo(Pop3SecurityMode.Plaintext));
        Assert.That(first.ConnectTimeoutMilliseconds, Is.EqualTo(Pop3Defaults.DefaultConnectTimeoutMilliseconds));
        Assert.That(first.ReadTimeoutMilliseconds, Is.EqualTo(Pop3Defaults.DefaultReadTimeoutMilliseconds));
        Assert.That(first.WriteTimeoutMilliseconds, Is.EqualTo(Pop3Defaults.DefaultWriteTimeoutMilliseconds));
        Assert.That(first.AuthenticationTimeoutMilliseconds, Is.EqualTo(Pop3Defaults.DefaultAuthenticationTimeoutMilliseconds));
        Assert.That(first.BufferSize, Is.EqualTo(Pop3Defaults.DefaultBufferSize));
        Assert.That(first.MaximumMessageLineOctets, Is.EqualTo(Pop3Defaults.DefaultMaximumMessageLineOctets));
        Assert.That(first.MaximumSaslLineOctets, Is.EqualTo(Pop3Defaults.DefaultMaximumSaslLineOctets));
        Assert.That(first.MessageMemoryCeilingBytes, Is.EqualTo(Pop3Defaults.DefaultMessageMemoryCeilingBytes));
        Assert.That(first.EnablePipelining, Is.False);
        Assert.That(first.ForcePipelining, Is.False);
        Assert.That(first.ProbeUserWhenCapaUnavailable, Is.False);
        Assert.That(first.ProbeTopWhenCapaUnavailable, Is.False);
        Assert.That(first.ProbeUidlWhenCapaUnavailable, Is.False);
        Assert.That(first.Authentication, Is.Not.SameAs(second.Authentication));
        Assert.That(first.Tls, Is.Not.SameAs(second.Tls));
        Assert.That(first.TlsProvider, Is.Null);
        Assert.That(first.Logger, Is.Null);
        Assert.That(first.TransferObserver, Is.Null);
        Assert.That(first.LeaveStreamOpen, Is.False);
    }

    [Test]
    public void ClientOptionsAndSnapshotFieldsRoundTrip()
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        Pop3ClientSnapshot snapshot = new Pop3ClientSnapshot();
        Pop3Response response = new Pop3Response();
        Pop3AuthenticationRequest authentication = new Pop3AuthenticationRequest();
        Pop3TlsClientOptions tls = new Pop3TlsClientOptions();
        FakeTlsProvider tlsProvider = new FakeTlsProvider();
        FakeProtocolLogger logger = new FakeProtocolLogger();
        FakeTransferObserver observer = new FakeTransferObserver();

        options.Host = "pop.example.test";
        options.Port = 995;
        options.SecurityMode = Pop3SecurityMode.ImplicitTls;
        options.ConnectTimeoutMilliseconds = 101;
        options.ReadTimeoutMilliseconds = 102;
        options.WriteTimeoutMilliseconds = 103;
        options.AuthenticationTimeoutMilliseconds = 104;
        options.BufferSize = 105;
        options.MaximumMessageLineOctets = 106;
        options.MaximumSaslLineOctets = 107;
        options.MessageMemoryCeilingBytes = 108;
        options.EnablePipelining = true;
        options.ForcePipelining = true;
        options.ProbeUserWhenCapaUnavailable = true;
        options.ProbeTopWhenCapaUnavailable = true;
        options.ProbeUidlWhenCapaUnavailable = true;
        options.Authentication = authentication;
        options.Tls = tls;
        options.TlsProvider = tlsProvider;
        options.Logger = logger;
        options.TransferObserver = observer;
        options.LeaveStreamOpen = true;
        snapshot.State = Pop3SessionState.Transaction;
        snapshot.IsConnected = true;
        snapshot.IsSecure = true;
        snapshot.IsAuthenticated = true;
        snapshot.AuthenticatedIdentity = "alice";
        snapshot.Greeting = "ready";
        snapshot.LastResponse = response;
        snapshot.IsUtf8Enabled = true;
        snapshot.IsUtf8UserEnabled = true;
        snapshot.SelectedLanguage = "en";

        Assert.That(options.Host, Is.EqualTo("pop.example.test"));
        Assert.That(options.Port, Is.EqualTo(995));
        Assert.That(options.SecurityMode, Is.EqualTo(Pop3SecurityMode.ImplicitTls));
        Assert.That(options.ConnectTimeoutMilliseconds, Is.EqualTo(101));
        Assert.That(options.ReadTimeoutMilliseconds, Is.EqualTo(102));
        Assert.That(options.WriteTimeoutMilliseconds, Is.EqualTo(103));
        Assert.That(options.AuthenticationTimeoutMilliseconds, Is.EqualTo(104));
        Assert.That(options.BufferSize, Is.EqualTo(105));
        Assert.That(options.MaximumMessageLineOctets, Is.EqualTo(106));
        Assert.That(options.MaximumSaslLineOctets, Is.EqualTo(107));
        Assert.That(options.MessageMemoryCeilingBytes, Is.EqualTo(108));
        Assert.That(options.EnablePipelining, Is.True);
        Assert.That(options.ForcePipelining, Is.True);
        Assert.That(options.ProbeUserWhenCapaUnavailable, Is.True);
        Assert.That(options.ProbeTopWhenCapaUnavailable, Is.True);
        Assert.That(options.ProbeUidlWhenCapaUnavailable, Is.True);
        Assert.That(options.Authentication, Is.SameAs(authentication));
        Assert.That(options.Tls, Is.SameAs(tls));
        Assert.That(options.TlsProvider, Is.SameAs(tlsProvider));
        Assert.That(options.Logger, Is.SameAs(logger));
        Assert.That(options.TransferObserver, Is.SameAs(observer));
        Assert.That(options.LeaveStreamOpen, Is.True);
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(snapshot.IsConnected, Is.True);
        Assert.That(snapshot.IsSecure, Is.True);
        Assert.That(snapshot.IsAuthenticated, Is.True);
        Assert.That(snapshot.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(snapshot.Greeting, Is.EqualTo("ready"));
        Assert.That(snapshot.LastResponse, Is.SameAs(response));
        Assert.That(snapshot.IsUtf8Enabled, Is.True);
        Assert.That(snapshot.IsUtf8UserEnabled, Is.True);
        Assert.That(snapshot.SelectedLanguage, Is.EqualTo("en"));
    }

    [Test]
    public void TlsOptionsDefaultToModernProtocolsAndRevocationChecks()
    {
        Pop3TlsClientOptions firstClient = new Pop3TlsClientOptions();
        Pop3TlsClientOptions secondClient = new Pop3TlsClientOptions();
        Pop3TlsServerOptions server = new Pop3TlsServerOptions();
        SslProtocols expectedProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

        Assert.That(firstClient.TargetHost, Is.Empty);
        Assert.That(firstClient.EnabledProtocols, Is.EqualTo(expectedProtocols));
        Assert.That(firstClient.CheckCertificateRevocation, Is.True);
        Assert.That(firstClient.ClientCertificates, Is.Empty);
        Assert.That(firstClient.ClientCertificates, Is.Not.SameAs(secondClient.ClientCertificates));
        Assert.That(firstClient.LeaveInnerStreamOpen, Is.False);
        Assert.That(server.ServerCertificate, Is.Null);
        Assert.That(server.EnabledProtocols, Is.EqualTo(expectedProtocols));
        Assert.That(server.RequireClientCertificate, Is.False);
        Assert.That(server.CheckCertificateRevocation, Is.True);
        Assert.That(server.LeaveInnerStreamOpen, Is.False);
    }

    [Test]
    public void TlsOptionFieldsRoundTrip()
    {
        using (RSA key = RSA.Create(2048))
        {
            CertificateRequest request = new CertificateRequest(
                "CN=api.test",
                key,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            using (X509Certificate2 certificate = MacOsKeychainTestGuard.Create(() =>
                request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddMinutes(-1),
                    DateTimeOffset.UtcNow.AddMinutes(1))))
            {
                Pop3TlsClientOptions client = new Pop3TlsClientOptions();
                Pop3TlsServerOptions server = new Pop3TlsServerOptions();
                X509Certificate2Collection certificates =
                    new X509Certificate2Collection(certificate);

                client.TargetHost = "pop.example.test";
                client.EnabledProtocols = SslProtocols.Tls12;
                client.CheckCertificateRevocation = false;
                client.ClientCertificates = certificates;
                client.LeaveInnerStreamOpen = true;
                server.ServerCertificate = certificate;
                server.EnabledProtocols = SslProtocols.Tls13;
                server.RequireClientCertificate = true;
                server.CheckCertificateRevocation = false;
                server.LeaveInnerStreamOpen = true;

                Assert.That(client.TargetHost, Is.EqualTo("pop.example.test"));
                Assert.That(client.EnabledProtocols, Is.EqualTo(SslProtocols.Tls12));
                Assert.That(client.CheckCertificateRevocation, Is.False);
                Assert.That(client.ClientCertificates, Is.SameAs(certificates));
                Assert.That(client.LeaveInnerStreamOpen, Is.True);
                Assert.That(server.ServerCertificate, Is.SameAs(certificate));
                Assert.That(server.EnabledProtocols, Is.EqualTo(SslProtocols.Tls13));
                Assert.That(server.RequireClientCertificate, Is.True);
                Assert.That(server.CheckCertificateRevocation, Is.False);
                Assert.That(server.LeaveInnerStreamOpen, Is.True);
            }
        }
    }

    [Test]
    public void ServerOptionsUseConservativeProviderAndResourceDefaults()
    {
        Pop3ServerOptions first = new Pop3ServerOptions();
        Pop3ServerOptions second = new Pop3ServerOptions();

        Assert.That(first.GreetingText, Is.Empty);
        Assert.That(first.ImplementationText, Is.Empty);
        Assert.That(first.SecurityMode, Is.EqualTo(Pop3SecurityMode.Plaintext));
        Assert.That(first.Tls, Is.Not.SameAs(second.Tls));
        Assert.That(first.TlsProvider, Is.Null);
        Assert.That(first.AuthenticationProvider, Is.Null);
        Assert.That(first.MailboxProvider, Is.Null);
        Assert.That(first.SaslFactories, Is.Empty);
        Assert.That(first.SaslFactories, Is.Not.SameAs(second.SaslFactories));
        Assert.That(first.InactivityTimeoutMilliseconds, Is.EqualTo(Pop3Defaults.DefaultServerInactivityTimeoutMilliseconds));
        Assert.That(first.ReadTimeoutMilliseconds, Is.EqualTo(Pop3Defaults.DefaultReadTimeoutMilliseconds));
        Assert.That(first.WriteTimeoutMilliseconds, Is.EqualTo(Pop3Defaults.DefaultWriteTimeoutMilliseconds));
        Assert.That(first.AuthenticationTimeoutMilliseconds, Is.EqualTo(Pop3Defaults.DefaultAuthenticationTimeoutMilliseconds));
        Assert.That(first.BufferSize, Is.EqualTo(Pop3Defaults.DefaultBufferSize));
        Assert.That(first.MaximumMessageLineOctets, Is.EqualTo(Pop3Defaults.DefaultMaximumMessageLineOctets));
        Assert.That(first.MaximumSaslLineOctets, Is.EqualTo(Pop3Defaults.DefaultMaximumSaslLineOctets));
        Assert.That(first.EnableTop, Is.True);
        Assert.That(first.EnableUserPassword, Is.True);
        Assert.That(first.EnableApop, Is.False);
        Assert.That(first.AllowUnprotectedCredentials, Is.False);
        Assert.That(first.EnablePipelining, Is.True);
        Assert.That(first.EnableUidl, Is.True);
        Assert.That(first.EnableResponseCodes, Is.True);
        Assert.That(first.EnableUtf8, Is.False);
        Assert.That(first.EnableUtf8User, Is.False);
        Assert.That(first.LanguageProvider, Is.Null);
        Assert.That(first.DefaultLanguage, Is.Null);
        Assert.That(first.Extensions, Is.Empty);
        Assert.That(first.Extensions, Is.Not.SameAs(second.Extensions));
        Assert.That(first.Logger, Is.Null);
        Assert.That(first.TransferObserver, Is.Null);
    }

    [Test]
    public void ServerOptionFieldsRoundTrip()
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        Pop3TlsServerOptions tls = new Pop3TlsServerOptions();
        FakeTlsProvider tlsProvider = new FakeTlsProvider();
        FakeAuthenticationProvider authentication = new FakeAuthenticationProvider();
        FakeMailboxProvider mailbox = new FakeMailboxProvider();
        IPop3SaslServerFactory[] saslFactories =
            new IPop3SaslServerFactory[] { new FakeSaslServerFactory() };
        FakeLanguageProvider language = new FakeLanguageProvider();
        IPop3ServerExtension[] extensions =
            new IPop3ServerExtension[] { new FakeServerExtension() };
        FakeProtocolLogger logger = new FakeProtocolLogger();
        FakeTransferObserver observer = new FakeTransferObserver();

        options.GreetingText = "ready";
        options.ImplementationText = "test-server";
        options.SecurityMode = Pop3SecurityMode.StlsRequired;
        options.Tls = tls;
        options.TlsProvider = tlsProvider;
        options.AuthenticationProvider = authentication;
        options.MailboxProvider = mailbox;
        options.SaslFactories = saslFactories;
        options.InactivityTimeoutMilliseconds = 201;
        options.ReadTimeoutMilliseconds = 202;
        options.WriteTimeoutMilliseconds = 203;
        options.AuthenticationTimeoutMilliseconds = 204;
        options.BufferSize = 205;
        options.MaximumMessageLineOctets = 206;
        options.MaximumSaslLineOctets = 207;
        options.EnableTop = false;
        options.EnableUserPassword = false;
        options.EnableApop = true;
        options.AllowUnprotectedCredentials = true;
        options.EnablePipelining = false;
        options.EnableUidl = false;
        options.EnableResponseCodes = false;
        options.EnableUtf8 = true;
        options.EnableUtf8User = true;
        options.LanguageProvider = language;
        options.DefaultLanguage = "fr";
        options.Extensions = extensions;
        options.Logger = logger;
        options.TransferObserver = observer;

        Assert.That(options.GreetingText, Is.EqualTo("ready"));
        Assert.That(options.ImplementationText, Is.EqualTo("test-server"));
        Assert.That(options.SecurityMode, Is.EqualTo(Pop3SecurityMode.StlsRequired));
        Assert.That(options.Tls, Is.SameAs(tls));
        Assert.That(options.TlsProvider, Is.SameAs(tlsProvider));
        Assert.That(options.AuthenticationProvider, Is.SameAs(authentication));
        Assert.That(options.MailboxProvider, Is.SameAs(mailbox));
        Assert.That(options.SaslFactories, Is.SameAs(saslFactories));
        Assert.That(options.InactivityTimeoutMilliseconds, Is.EqualTo(201));
        Assert.That(options.ReadTimeoutMilliseconds, Is.EqualTo(202));
        Assert.That(options.WriteTimeoutMilliseconds, Is.EqualTo(203));
        Assert.That(options.AuthenticationTimeoutMilliseconds, Is.EqualTo(204));
        Assert.That(options.BufferSize, Is.EqualTo(205));
        Assert.That(options.MaximumMessageLineOctets, Is.EqualTo(206));
        Assert.That(options.MaximumSaslLineOctets, Is.EqualTo(207));
        Assert.That(options.EnableTop, Is.False);
        Assert.That(options.EnableUserPassword, Is.False);
        Assert.That(options.EnableApop, Is.True);
        Assert.That(options.AllowUnprotectedCredentials, Is.True);
        Assert.That(options.EnablePipelining, Is.False);
        Assert.That(options.EnableUidl, Is.False);
        Assert.That(options.EnableResponseCodes, Is.False);
        Assert.That(options.EnableUtf8, Is.True);
        Assert.That(options.EnableUtf8User, Is.True);
        Assert.That(options.LanguageProvider, Is.SameAs(language));
        Assert.That(options.DefaultLanguage, Is.EqualTo("fr"));
        Assert.That(options.Extensions, Is.SameAs(extensions));
        Assert.That(options.Logger, Is.SameAs(logger));
        Assert.That(options.TransferObserver, Is.SameAs(observer));
    }
}
