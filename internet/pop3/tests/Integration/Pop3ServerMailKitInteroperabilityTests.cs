using System.Net;
using System.Net.Sockets;
using System.Text;
using MailKit.Security;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using MailKitPop3Capabilities = MailKit.Net.Pop3.Pop3Capabilities;
using MailKitPop3Client = MailKit.Net.Pop3.Pop3Client;

namespace pop3.Tests;

[NonParallelizable]
public class Pop3ServerMailKitInteroperabilityTests
{
    [Test]
    public void MailKitRunsTheCompleteMailboxWorkflowAgainstThePublicServer()
    {
        FakeAuthenticationProvider authentication = new FakeAuthenticationProvider();
        FakeMailboxSession mailbox = CreateMailbox();
        Pop3ServerOptions options = CreateServerOptions(authentication, mailbox);
        options.AllowUnprotectedCredentials = true;
        using ServerConnection server = new ServerConnection(options);
        using MailKitPop3Client client = new MailKitPop3Client();
        client.Timeout = 5000;

        client.Connect(
            "localhost",
            server.Port,
            SecureSocketOptions.None,
            CancellationToken.None);

        MailKitPop3Capabilities capabilities = client.Capabilities;
        client.Authenticate("alice", "secret", CancellationToken.None);
        int count = client.GetMessageCount(CancellationToken.None);
        int firstSize = client.GetMessageSize(0, CancellationToken.None);
        IList<int> sizes = client.GetMessageSizes(CancellationToken.None);
        string firstUniqueId = client.GetMessageUid(0, CancellationToken.None);
        IList<string> uniqueIds = client.GetMessageUids(CancellationToken.None);
        MimeKit.HeaderList headers = client.GetMessageHeaders(
            0,
            CancellationToken.None);
        string retrieved;
        using (Stream stream = client.GetStream(
            0,
            false,
            CancellationToken.None,
            null))
        using (StreamReader reader = new StreamReader(
            stream,
            Encoding.UTF8,
            false,
            1024,
            false))
        {
            retrieved = reader.ReadToEnd();
        }
        client.DeleteMessage(0, CancellationToken.None);
        client.Reset(CancellationToken.None);
        client.NoOp(CancellationToken.None);
        client.DeleteMessage(1, CancellationToken.None);
        client.Disconnect(true, CancellationToken.None);
        server.AwaitCompletion();

        Assert.That(capabilities.HasFlag(MailKitPop3Capabilities.User), Is.True);
        Assert.That(capabilities.HasFlag(MailKitPop3Capabilities.Sasl), Is.True);
        Assert.That(capabilities.HasFlag(MailKitPop3Capabilities.Pipelining), Is.True);
        Assert.That(capabilities.HasFlag(MailKitPop3Capabilities.Top), Is.True);
        Assert.That(capabilities.HasFlag(MailKitPop3Capabilities.UIDL), Is.True);
        Assert.That(authentication.SaslPlainCallCount, Is.EqualTo(1));
        Assert.That(authentication.LastSaslPlainCredentials!.Username,
            Is.EqualTo("alice"));
        Assert.That(authentication.LastSaslPlainCredentials.Password,
            Is.EqualTo("secret"));
        Assert.That(count, Is.EqualTo(2));
        Assert.That(firstSize, Is.EqualTo(mailbox.Messages[0].OctetCount));
        Assert.That(sizes, Is.EqualTo(mailbox.Messages.Select(
            message => (int)message.OctetCount)));
        Assert.That(firstUniqueId, Is.EqualTo("mailkit-one"));
        Assert.That(uniqueIds, Is.EqualTo(new[] { "mailkit-one", "mailkit-two" }));
        Assert.That(headers[MimeKit.HeaderId.Subject], Is.EqualTo("dot test"));
        Assert.That(retrieved, Does.Contain("\r\n.leading-dot\r\n"));
        Assert.That(retrieved, Does.Not.Contain("\r\n..leading-dot\r\n"));
        Assert.That(mailbox.WriteTopCallCount, Is.EqualTo(1));
        Assert.That(mailbox.WriteMessageCallCount, Is.EqualTo(1));
        Assert.That(mailbox.ResetCallCount, Is.EqualTo(1));
        Assert.That(mailbox.MarkedMessageNumbers, Is.EqualTo(new[] { 2 }));
        Assert.That(mailbox.CommitCallCount, Is.EqualTo(1));
        Assert.That(mailbox.RollbackCallCount, Is.Zero);
        Assert.That(mailbox.ReleaseCallCount, Is.EqualTo(1));
    }

    [Test]
    public void MailKitNegotiatesStartTlsAndRefreshesCapabilitiesBeforeAuthentication()
    {
        FakeAuthenticationProvider authentication = new FakeAuthenticationProvider();
        FakeMailboxSession mailbox = CreateMailbox();
        Pop3ServerOptions options = CreateServerOptions(authentication, mailbox);
        options.SecurityMode = Pop3SecurityMode.StlsRequired;
        options.TlsProvider = new ManagedTlsProvider();
        using ServerConnection server = new ServerConnection(options);
        using MailKitPop3Client client = new MailKitPop3Client();
        client.Timeout = 5000;
        client.ServerCertificateValidationCallback =
            (sender, peerCertificate, chain, errors) => true;

        client.Connect(
            "localhost",
            server.Port,
            SecureSocketOptions.StartTls,
            CancellationToken.None);
        MailKitPop3Capabilities capabilities = client.Capabilities;
        bool secured = client.IsSecure;
        client.Authenticate("alice", "secret", CancellationToken.None);
        int count = client.GetMessageCount(CancellationToken.None);
        client.Disconnect(true, CancellationToken.None);
        server.AwaitCompletion();

        Assert.That(secured, Is.True);
        Assert.That(capabilities.HasFlag(MailKitPop3Capabilities.StartTLS), Is.True);
        Assert.That(capabilities.HasFlag(MailKitPop3Capabilities.User), Is.True);
        Assert.That(capabilities.HasFlag(MailKitPop3Capabilities.Sasl), Is.True);
        Assert.That(authentication.SaslPlainCallCount, Is.EqualTo(1));
        Assert.That(count, Is.EqualTo(2));
        Assert.That(mailbox.CommitCallCount, Is.EqualTo(1));
        Assert.That(mailbox.ReleaseCallCount, Is.EqualTo(1));
    }

    static Pop3ServerOptions CreateServerOptions(
        FakeAuthenticationProvider authentication,
        FakeMailboxSession mailbox)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "MailKit interoperability server";
        options.ImplementationText = "pop3-tests";
        options.AuthenticationProvider = authentication;
        options.MailboxProvider = new FakeMailboxProvider(mailbox);
        options.EnablePipelining = true;
        options.EnableTop = true;
        options.EnableUidl = true;
        options.ReadTimeoutMilliseconds = 5000;
        options.WriteTimeoutMilliseconds = 5000;
        options.AuthenticationTimeoutMilliseconds = 5000;
        return options;
    }

    static FakeMailboxSession CreateMailbox()
    {
        byte[] first = Encoding.ASCII.GetBytes(
            "From: sender@example.com\r\n" +
            "To: receiver@example.com\r\n" +
            "Subject: dot test\r\n" +
            "\r\n" +
            ".leading-dot\r\n" +
            "body\r\n");
        byte[] second = Encoding.ASCII.GetBytes(
            "From: other@example.com\r\n" +
            "To: receiver@example.com\r\n" +
            "Subject: second\r\n" +
            "\r\n" +
            "second body\r\n");
        FakeMailboxSession mailbox = new FakeMailboxSession();
        mailbox.Status = Status(2, first.Length + second.Length);
        mailbox.ResetStatus = Status(2, first.Length + second.Length);
        mailbox.Messages = new[]
        {
            Message(1, first.Length),
            Message(2, second.Length)
        };
        mailbox.UniqueIds = new[]
        {
            UniqueId(1, "mailkit-one"),
            UniqueId(2, "mailkit-two")
        };
        mailbox.MessageData.Add(1, first);
        mailbox.MessageData.Add(2, second);
        mailbox.TopData.Add(1, Encoding.ASCII.GetBytes(
            "From: sender@example.com\r\n" +
            "To: receiver@example.com\r\n" +
            "Subject: dot test\r\n" +
            "\r\n"));
        mailbox.TopData.Add(2, Encoding.ASCII.GetBytes(
            "From: other@example.com\r\n" +
            "To: receiver@example.com\r\n" +
            "Subject: second\r\n" +
            "\r\n"));
        return mailbox;
    }

    static Pop3MailboxStatus Status(int count, long octets)
    {
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        status.MessageCount = count;
        status.TotalOctets = octets;
        return status;
    }

    static Pop3MessageInfo Message(int number, long octets)
    {
        Pop3MessageInfo message = new Pop3MessageInfo();
        message.MessageNumber = number;
        message.OctetCount = octets;
        return message;
    }

    static Pop3UniqueId UniqueId(int number, string value)
    {
        Pop3UniqueId uniqueId = new Pop3UniqueId();
        uniqueId.MessageNumber = number;
        uniqueId.Value = value;
        return uniqueId;
    }

    static byte[] CertificateData()
    {
        string encoded =
            "MIIJ9wIBAzCCCaUGCSqGSIb3DQEHAaCCCZYEggmSMIIJjjCCA/oGCSqGSIb3DQEHBqCCA+swggPnAgEAMIID4AYJKoZIhvcNAQcB" +
            "MF8GCSqGSIb3DQEFDTBSMDEGCSqGSIb3DQEFDDAkBBD8BrngVSwiea61yiMNvBurAgIIADAMBggqhkiG9w0CCQUAMB0GCWCGSAFl" +
            "AwQBKgQQffsTReUJk4WgXAnAqfAxr4CCA3AN6wnNxGgQgQTEtRo4Fvh0hO396CkMSl26+wPEhCkQFjtGW3qWVYgg3+BH6dGHHNVO" +
            "8Kt3idGJVJ4Ct4/dUWnGFe4VoUIyFwvLuGwAnyJxQ+YPh4hf2uadgtUiVTuo2P5WYoT9ZnpFxQhTAgF7KJyXls7RcPWQ0MxdnxuZ" +
            "btLOpMyEKWWakHDJimFVPdn+GWaGCouGOD+N4+Zj8NSsx8bu/4NCVMYSi2QOWx6DPOyqOZnlNoOzWFiHt+7IBHFzGHICdZMBVcsJ" +
            "F/PDrBfB/rJqw4sQmZtkv36wKJJtn4N0Ch95+pohl/YXhgsbbtU0B8A0fN+4+8KAP/whlRpNVA9y0Lp7JAVkTAiLabSjBdKY9qd5" +
            "NVehd+Z42J3ticSLs7+st1VQZKAeu3TcMP0+msO7uaV4JsyC+DJnT38vJBgPazyZ821SqzdjQhL3sgfzltYwgt9UulYj4br+wNYl" +
            "GVIDTb18YgYzSwynNNRuKC0r4FZIHmZJZQTVCXmP8ez7JXQJnuYgNBR34wxJmDoISaCAg+2lBvdPl/mCk7wPkzOt5pA9kJ22nXcb" +
            "KDGrd7gIRP6sqKDjk0pgFVk/zZC6XSQ/k/XQDtnNmsJQB2uLr75f3sTv/TJ4OCHw0BkiMKSGlbPsETlsko1eYPLinZbsvPwpoeBq" +
            "lk0bDV/wIj7X5/nB4vBSEQrALfIgrWX2re94QElTpoSg4QEL8yNh9xHPxdYkf+dt1lqp/FJbX5jv+YsDpofirHDPWhHTF4QgouT1" +
            "+ctK8q39Tik3Wo95jXucLPpvxE/+vpaaKOdYTZIIOXzpfiSwy0kDg2oTzlzJvbnTKXSB6SAyt2tTK2aL/Ll+YbpA9Z4fs7WrLi26" +
            "HSGcS0dx747BDh5c4FNSTmqtBZyurEQ6mvBe14mqMhQNpbpJ/uOZ/zNwVfCeL/I8ZtmlO38OGY+Wi2Y84uH15RVFzT15etBh2h9n" +
            "dQ8omUMTree0grkXbvwZL2Kcy5o3XpBJ6ezRIBntDrKPRNkfArIJji+5p64jHOhEVeUh1eSBGLz8MIHwl0G6Uthg+AaQxFqDchZ1" +
            "n2sjdmChOsSuwRT9CSZgMJRlAw0gA5U/iI+/E3MGUiS9iYzaewx5jvgvSVO9F98InSpAo6MVhBLadjzdTChYns+uXgYkLwKCnIIK" +
            "s4UDs7jjMIIFjAYJKoZIhvcNAQcBoIIFfQSCBXkwggV1MIIFcQYLKoZIhvcNAQwKAQKgggU5MIIFNTBfBgkqhkiG9w0BBQ0wUjAx" +
            "BgkqhkiG9w0BBQwwJAQQKxpqKojLnVVM9VSNDm+rDgICCAAwDAYIKoZIhvcNAgkFADAdBglghkgBZQMEASoEEGvnwSRDa9+mCNge" +
            "Qdb4GEYEggTQSITvLB9EzulJtIhhpvT7yORrwKhikd4l7WNkzzxwuVZVQG4NkdLEbjBbBq7v5W/RBVASnZQGw1O7VgDCmVOD05yF" +
            "6bYGr8yscBLV/+7N57yUwLBm8TCFYAb9ssqY2C6FbMBabxAuQ5oGldjoRKb1NWXRkNA5j1mnXC7zrwzcA5pbkU3pmxJXYw22dO4C" +
            "E8SUOIYUH2VznqW25LSSE+FUy1wrv/AoJDvSZa+iUrY90Zr00H1gpEGqoPDbtsMMaJwt117JWzLoe9zih1rS9ciT2EXKqSbbQn/T" +
            "w8huWn1xzkhdCF59VSjr5DtYUQn015SS3jYD9ob6C8A0dP/+NV1DpTjK5GIeB4//nneygABIkUNiG1mYBGjaUVHSkHLJLeH2D7wI" +
            "moWMAa0FLsYwk4rbgsnM9jtBtT7kpkykzAn3y870V/Qn3miaeKdvX0bgk/yQ2UnWR8FJ9p0UKqBOpYh2Y343Xzs21wKTPciQ4WHj" +
            "pABP8hhw9GaBxHvVRZi9HG/lxZWuZtFMOwH5k9bWzN1bC2quWZCm6BuI3+q3+dFPC0pnEBKThyn/rElL+MLX8jpdUYFz61p+uIl4" +
            "siBPmxokY/7aDWa8npWhEF6WEEO5F9pM1k4k1wPW+18FPLOT2Lqq4ic0hB9owoiXSnhMDn7G7flIW/zYL76eprnxi9U0H5UP9NMl" +
            "bOyCwbQKM5mGKje55ANi1ByikVxpo4Brr0r7lecNykV5YSl2RjxEFBROJ1mMwFZbzhwSly14ePaTHzWbE3OSTJ9DeW1f7WIEn6XL" +
            "O1kacPybkpIbscvr/f3p+yr+00ithJdBISZYGj91R4GplKbXC+16/0UCDwFlGP0y3DK5wK+b1mT9RfRWBLyISWqxf/cZD2dmy3Uk" +
            "9OGAMbjSye5h1Wn8hgVUpFt/LVZeWPWtawoHHQ5im2qiOXEDU0p5TU7FHOetBEv1nXTNwbHbuXVBlbCZEZGYeqm8t084y/ho3PAK" +
            "//tnGfX+74un0izHYJRY4pqhFSZTmqkz8/0sBL/6VLvPF9V4dmON0fhnFfcluXHYVYMwTOjtuAHbjl8sWYsq8amlt90aRaq6WpMu" +
            "7aqqnEH3byM6FaSYqwBvCcbC1BEl6kM9qO15nBIaLhZ/vk7YxOWye3yubvP6IfQfNocOaxVgACgC8TWomyFJ1l2yLDjl3Xm3gvuB" +
            "gC4dq84OG/xn74rWMgdxPb9o/E+GbZeb72Rrnr+Wlzf9vMRUGwFGPnXZzw8BsSbh7o0u051iW857s2WKcim2OTczcwrWcEiLS23A" +
            "5kPTL0IMZ8CnjkXTgwlFsEXPTAbDTHgLYL76ctA+I3YT1XrMFnHT+shumKikp1sQoSGCns4KJL46T8vDPCkEhXm35+idwvZGTnxe" +
            "atsG3KSs25zc3cEV/xpfxpDIpsOSzEK20FySN+6DG9DBiBRw3EHf/XnvhCKUxDqjOwEu6StLjEjeVqMhTq/SKoRTlC4ci+Hwo9lz" +
            "n1knGTsH+uwpGrl7qwInxvFmJrwT5uY0DvGuVfzD/BDtOitzOKCUJ/qAJDH879FDTQgQkko3EK6y/xOcSRPmV5NrnDMZBkY5cVvu" +
            "blSypLB7rSPyZpoOTBDRrD9ql7qJm2a27UQktwKRLxpgVXXmkKf8slExJTAjBgkqhkiG9w0BCRUxFgQUPY0O7v4D1okVIBNkRLFu" +
            "RR63eTEwSTAxMA0GCWCGSAFlAwQCAQUABCCnxpmTNCdjXZLNtkpcZHVN4ICjDAOjHCvTKjCGDJMmJwQQ75Bh3F4B6VYtqeOIpDDj" +
            "vgICCAA=";
        return Convert.FromBase64String(encoded);
    }

    sealed class ManagedTlsProvider : IPop3ServerTlsProvider
    {
        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TlsServerProtocol protocol = new TlsServerProtocol(stream);
            protocol.Accept(new ManagedTlsServer());
            return protocol.Stream;
        }
    }

    sealed class ManagedTlsServer : DefaultTlsServer
    {
        readonly Org.BouncyCastle.Crypto.AsymmetricKeyParameter PrivateKey;
        readonly Org.BouncyCastle.Tls.Certificate CertificateChain;

        internal ManagedTlsServer()
            : base(new BcTlsCrypto())
        {
            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            using (MemoryStream input = new MemoryStream(CertificateData()))
            {
                store.Load(input, "pop3-tests".ToCharArray());
            }
            string alias = store.Aliases.Single(store.IsKeyEntry);
            PrivateKey = store.GetKey(alias).Key;
            X509CertificateEntry[] entries = store.GetCertificateChain(alias);
            TlsCertificate[] certificates = entries.Select(entry =>
                Crypto.CreateCertificate(entry.Certificate.GetEncoded())).ToArray();
            CertificateChain = new Org.BouncyCastle.Tls.Certificate(certificates);
        }

        protected override TlsCredentialedSigner GetRsaSignerCredentials()
        {
            TlsCryptoParameters parameters = new TlsCryptoParameters(m_context);
            SignatureAndHashAlgorithm algorithm = new SignatureAndHashAlgorithm(
                Org.BouncyCastle.Tls.HashAlgorithm.sha256,
                SignatureAlgorithm.rsa);
            return new BcDefaultTlsCredentialedSigner(
                parameters,
                (BcTlsCrypto)Crypto,
                PrivateKey,
                CertificateChain,
                algorithm);
        }

        protected override ProtocolVersion[] GetSupportedVersions()
        {
            return new[] { ProtocolVersion.TLSv12 };
        }
    }

    sealed class ServerConnection : IDisposable
    {
        readonly TcpListener Listener;
        readonly Task ServerTask;

        internal int Port;

        internal ServerConnection(Pop3ServerOptions options)
        {
            Listener = new TcpListener(IPAddress.Loopback, 0);
            Listener.Start();
            Port = ((IPEndPoint)Listener.LocalEndpoint).Port;
            ServerTask = Task.Run(() => Serve(options));
        }

        public void Dispose()
        {
            Listener.Stop();
            if (!ServerTask.IsCompleted)
            {
                Task.WhenAny(ServerTask, Task.Delay(5000)).GetAwaiter().GetResult();
            }
        }

        internal void AwaitCompletion()
        {
            Task completed = Task.WhenAny(
                ServerTask,
                Task.Delay(5000)).GetAwaiter().GetResult();
            Assert.That(completed, Is.SameAs(ServerTask));
            ServerTask.GetAwaiter().GetResult();
        }

        void Serve(Pop3ServerOptions options)
        {
            using TcpClient accepted = Listener.AcceptTcpClient();
            Pop3ServerConnection connection = new Pop3ServerConnection();
            connection.Stream = accepted.GetStream();
            connection.LocalEndPoint = accepted.Client.LocalEndPoint?.ToString();
            connection.RemoteEndPoint = accepted.Client.RemoteEndPoint?.ToString();
            connection.LeaveOpen = true;
            new Pop3Server().ServeConnection(
                connection,
                options,
                CancellationToken.None);
        }
    }
}
