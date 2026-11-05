using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace dns.Tests.Api;

public class DnsTlsTransportTests
{
    [Test]
    public void AppliesDotPortOnlyToTheOrdinaryDnsDefault()
    {
        DnsEndpoint standard = new DnsEndpoint();
        standard.Host = "resolver.example";
        DnsEndpoint custom = new DnsEndpoint();
        custom.Host = "resolver.example";
        custom.Port = 8853;

        DnsEndpoint standardResult =
            TlsTransportOperations.ApplyDefaultPort(standard);
        DnsEndpoint customResult =
            TlsTransportOperations.ApplyDefaultPort(custom);

        Assert.Multiple(() =>
        {
            Assert.That(standardResult.Port,
                Is.EqualTo(DnsPorts.DnsOverTls));
            Assert.That(customResult.Port, Is.EqualTo(8853));
            Assert.That(standard.Port, Is.EqualTo(DnsPorts.Dns));
        });
    }

    [Test]
    public void ChoosesConfiguredHostAndNumericAuthenticationNames()
    {
        DnsTransportRequest configured = new DnsTransportRequest();
        configured.Endpoint.Host = "endpoint.test";
        configured.Options.Tls.AuthenticationName = "configured.test";
        DnsTransportRequest host = new DnsTransportRequest();
        host.Endpoint.Host = "endpoint.test";
        DnsTransportRequest numeric = new DnsTransportRequest();
        IPEndPoint remote = new IPEndPoint(IPAddress.Loopback, 853);

        string configuredResult =
            TlsTransportOperations.ReadAuthenticationName(configured, remote);
        string hostResult =
            TlsTransportOperations.ReadAuthenticationName(host, remote);
        string numericResult =
            TlsTransportOperations.ReadAuthenticationName(numeric, remote);

        Assert.Multiple(() =>
        {
            Assert.That(configuredResult, Is.EqualTo("configured.test"));
            Assert.That(hostResult, Is.EqualTo("endpoint.test"));
            Assert.That(numericResult, Is.EqualTo("127.0.0.1"));
        });
    }

    [Test]
    public void ExchangesFragmentedFrameAndSendsConfiguredAuthenticationName()
    {
        RequireLocalCertificateSupport();
        using CertificateFixture certificate = new CertificateFixture();
        using Socket listener = BindListener();
        string? receivedName = null;
        DnsMessage query = Query(0x6101, "tls.example.");
        byte[] responseBytes = Encode(Response(query));
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            using SslStream stream = ServerStream(connection);
            AuthenticateServer(stream, certificate.Certificate, name =>
            {
                receivedName = name;
            });
            Assert.That(ReadFrame(stream), Is.EqualTo(Encode(query)));
            WriteFrameFragmented(stream, responseBytes);
        });
        DnsTransportRequest request = Request(listener, query);
        request.Options.Tls.AuthenticationName = "resolver.test";
        request.Options.ReuseConnections = false;

        DnsTransportResult result = new DnsTlsTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(result.Transport, Is.EqualTo(DnsTransportKind.Tls));
            Assert.That(result.ResponseBytes, Is.EqualTo(responseBytes));
            Assert.That(result.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x6101));
            Assert.That(receivedName, Is.EqualTo("resolver.test"));
        });
    }

    [Test]
    public void RejectsAnUntrustedCertificateWhenValidationIsEnabled()
    {
        RequireLocalCertificateSupport();
        using CertificateFixture certificate = new CertificateFixture();
        using Socket listener = BindListener();
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            using SslStream stream = ServerStream(connection);
            try
            {
                AuthenticateServer(stream, certificate.Certificate, null);
            }
            catch (AuthenticationException)
            {
            }
            catch (IOException)
            {
            }
        });
        DnsTransportRequest request = Request(
            listener, Query(0x6102, "validation.example."));
        request.Options.Tls.AuthenticationName = "resolver.test";
        request.Options.Tls.ValidateCertificate = true;

        DnsTransportResult result = new DnsTlsTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.AuthenticationFailed));
            Assert.That(result.ResponseMessage, Is.Null);
        });
    }

    [Test]
    public void ReusesOneAuthenticatedConnectionForSequentialQueries()
    {
        RequireLocalCertificateSupport();
        using CertificateFixture certificate = new CertificateFixture();
        using Socket listener = BindListener();
        int acceptedCount = 0;
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            acceptedCount++;
            using SslStream stream = ServerStream(connection);
            AuthenticateServer(stream, certificate.Certificate, null);
            byte[] firstQuery = ReadFrame(stream);
            WriteFrame(stream, ResponseBytes(firstQuery));
            byte[] secondQuery = ReadFrame(stream);
            WriteFrame(stream, ResponseBytes(secondQuery));
        });
        DnsTlsTransport transport = new DnsTlsTransport();

        DnsTransportResult first = transport.Exchange(Request(
            listener, Query(0x6103, "first.example.")));
        DnsTransportResult second = transport.Exchange(Request(
            listener, Query(0x6104, "second.example.")));
        serverTask.GetAwaiter().GetResult();
        transport.CloseTransport();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(second.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(acceptedCount, Is.EqualTo(1));
            Assert.That(second.ResponseMessage!.Header.Identifier,
                Is.EqualTo(0x6104));
        });
    }

    [Test]
    public void TlsReusePolicyCanRequireANewConnection()
    {
        RequireLocalCertificateSupport();
        using CertificateFixture certificate = new CertificateFixture();
        using Socket listener = BindListener();
        int acceptedCount = 0;
        Task serverTask = Task.Run(() =>
        {
            int index = 0;
            while (index < 2)
            {
                using Socket connection = listener.Accept();
                acceptedCount++;
                using SslStream stream = ServerStream(connection);
                AuthenticateServer(stream, certificate.Certificate, null);
                byte[] queryBytes = ReadFrame(stream);
                WriteFrame(stream, ResponseBytes(queryBytes));
                index++;
            }
        });
        DnsTlsTransport transport = new DnsTlsTransport();
        DnsTransportRequest firstRequest = Request(
            listener, Query(0x6105, "one.example."));
        firstRequest.Options.Tls.ReuseConnection = false;
        DnsTransportRequest secondRequest = Request(
            listener, Query(0x6106, "two.example."));
        secondRequest.Options.Tls.ReuseConnection = false;

        DnsTransportResult first = transport.Exchange(firstRequest);
        DnsTransportResult second = transport.Exchange(secondRequest);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(second.Status, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That(acceptedCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void TimesOutWhileWaitingForDeclaredTlsPayload()
    {
        RequireLocalCertificateSupport();
        using CertificateFixture certificate = new CertificateFixture();
        using Socket listener = BindListener();
        DnsMessage query = Query(0x6107, "timeout.example.");
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            using SslStream stream = ServerStream(connection);
            AuthenticateServer(stream, certificate.Certificate, null);
            byte[] ignored = ReadFrame(stream);
            stream.Write(new byte[] { 0, 12 });
            stream.Flush();
            Thread.Sleep(150);
        });
        DnsTransportRequest request = Request(listener, query);
        request.Options.AttemptTimeoutMilliseconds = 40;

        DnsTransportResult result = new DnsTlsTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.TimedOut));
            Assert.That(result.TimedOut, Is.True);
            Assert.That(result.Cancelled, Is.False);
            Assert.That(result.ElapsedMilliseconds, Is.LessThan(1000));
        });
    }

    [Test]
    public void RejectsAZeroLengthTlsFrame()
    {
        RequireLocalCertificateSupport();
        using CertificateFixture certificate = new CertificateFixture();
        using Socket listener = BindListener();
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            using SslStream stream = ServerStream(connection);
            AuthenticateServer(stream, certificate.Certificate, null);
            byte[] ignored = ReadFrame(stream);
            stream.Write(new byte[] { 0, 0 });
            stream.Flush();
        });
        DnsTransportRequest request = Request(
            listener, Query(0x6109, "zero-frame.example."));

        DnsTransportResult result = new DnsTlsTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ProtocolError));
            Assert.That(result.ResponseMessage, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTcpFrame));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("tlsFrame.messageLength"));
        });
    }

    [Test]
    public void RejectsADeclaredTlsFrameBeyondTheReceiveLimit()
    {
        RequireLocalCertificateSupport();
        using CertificateFixture certificate = new CertificateFixture();
        using Socket listener = BindListener();
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            using SslStream stream = ServerStream(connection);
            AuthenticateServer(stream, certificate.Certificate, null);
            byte[] ignored = ReadFrame(stream);
            stream.Write(new byte[] { 0, 65 });
            stream.Flush();
        });
        DnsTransportRequest request = Request(
            listener, Query(0x6110, "oversized-frame.example."));
        request.Options.MaximumResponseLength = 64;

        DnsTransportResult result = new DnsTlsTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DnsTransportStatus.ResponseTooLarge));
            Assert.That(result.ResponseBytes, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("responseBytes"));
        });
    }

    [Test]
    public void TimesOutDuringTlsAuthentication()
    {
        using Socket listener = BindListener();
        Task serverTask = Task.Run(() =>
        {
            using Socket connection = listener.Accept();
            Thread.Sleep(150);
        });
        DnsTransportRequest request = Request(
            listener, Query(0x6111, "authentication-timeout.example."));
        request.Options.AttemptTimeoutMilliseconds = 40;

        DnsTransportResult result = new DnsTlsTransport().Exchange(request);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.TimedOut));
            Assert.That(result.TimedOut, Is.True);
            Assert.That(result.Cancelled, Is.False);
            Assert.That(result.ResponseMessage, Is.Null);
            Assert.That(result.ElapsedMilliseconds, Is.LessThan(1000));
        });
    }

    [Test]
    public void QueryClientRoutesDirectTlsRequestsToTlsTransport()
    {
        CapturingTransport transport = new CapturingTransport();
        DnsQueryClient client = new DnsQueryClient();
        client.NextServerOffset = 0;
        client.TlsTransport = transport;
        DnsQueryRequest request = new DnsQueryRequest();
        request.Question.Name = Parse("client.example.");
        request.Question.RecordType = DnsRecordType.A;
        request.Question.RecordTypeValue = 1;
        request.Question.Class = DnsClass.Internet;
        request.Question.ClassValue = 1;
        request.Servers.Add(new DnsEndpoint
        {
            HasAddress = true,
            Address = IPAddress.Loopback.GetAddressBytes(),
            AddressFamily = 4
        });
        request.TransportOptions.Kind = DnsTransportKind.Tls;
        request.TransportOptions.MaximumAttempts = 1;

        DnsQueryResult result = client.Query(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Attempts, Has.Count.EqualTo(1));
            Assert.That(transport.Requests, Has.Count.EqualTo(1));
            Assert.That(transport.Requests[0].Options.Kind,
                Is.EqualTo(DnsTransportKind.Tls));
        });
    }

    [Test]
    public void ClosedTransportRejectsFurtherTlsExchanges()
    {
        DnsTlsTransport transport = new DnsTlsTransport();
        transport.CloseTransport();
        DnsTransportRequest request = new DnsTransportRequest();
        request.Endpoint.HasAddress = true;
        request.Endpoint.Address = IPAddress.Loopback.GetAddressBytes();
        request.Endpoint.AddressFamily = 4;
        request.Options.Kind = DnsTransportKind.Tls;
        DnsMessage query = Query(0x6108, "closed.example.");
        request.QueryMessage = query;
        request.QueryBytes = Encode(query);

        DnsTransportResult result = transport.Exchange(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsTransportStatus.Closed));
            Assert.That(result.Transport, Is.EqualTo(DnsTransportKind.Tls));
        });
    }

    static Socket BindListener()
    {
        Socket listener = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(8);
        return listener;
    }

    static void RequireLocalCertificateSupport()
    {
        if (OperatingSystem.IsMacOS())
        {
            Assert.Ignore(
                "The macOS test sandbox does not expose a temporary certificate keychain.");
        }
    }

    static SslStream ServerStream(Socket connection)
    {
        return new SslStream(new NetworkStream(connection, false), false);
    }

    static void AuthenticateServer(
        SslStream stream,
        X509Certificate2 certificate,
        Action<string?>? captureName)
    {
        SslServerAuthenticationOptions options =
            new SslServerAuthenticationOptions();
        if (captureName != null)
        {
            options.ServerCertificateSelectionCallback = (sender, name) =>
            {
                captureName(name);
                return certificate;
            };
        }
        else
        {
            options.ServerCertificate = certificate;
        }

        stream.AuthenticateAsServer(options);
    }

    static DnsTransportRequest Request(Socket listener, DnsMessage query)
    {
        IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;
        DnsTransportRequest request = new DnsTransportRequest();
        request.Endpoint.HasAddress = true;
        request.Endpoint.Address = endpoint.Address.GetAddressBytes();
        request.Endpoint.AddressFamily = 4;
        request.Endpoint.Port = endpoint.Port;
        request.Options.Kind = DnsTransportKind.Tls;
        request.Options.AttemptTimeoutMilliseconds = 1000;
        request.Options.MaximumResponseLength = 4096;
        request.Options.ReuseConnections = true;
        request.Options.Tls.ValidateCertificate = false;
        request.Options.Tls.CheckCertificateRevocation = false;
        request.QueryMessage = query;
        request.QueryBytes = Encode(query);
        return request;
    }

    static byte[] ReadFrame(SslStream stream)
    {
        byte[] prefix = ReadExact(stream, 2);
        int length = (prefix[0] << 8) | prefix[1];
        return ReadExact(stream, length);
    }

    static byte[] ReadExact(SslStream stream, int length)
    {
        byte[] bytes = new byte[length];
        int received = 0;
        while (received < length)
        {
            int count = stream.Read(bytes, received, length - received);
            if (count == 0)
            {
                throw new InvalidOperationException(
                    "The test TLS stream closed early.");
            }

            received += count;
        }

        return bytes;
    }

    static void WriteFrame(SslStream stream, byte[] message)
    {
        stream.Write(Frame(message));
        stream.Flush();
    }

    static void WriteFrameFragmented(SslStream stream, byte[] message)
    {
        byte[] frame = Frame(message);
        int index = 0;
        while (index < frame.Length)
        {
            stream.Write(frame, index, 1);
            stream.Flush();
            index++;
        }
    }

    static byte[] Frame(byte[] message)
    {
        byte[] frame = new byte[message.Length + 2];
        frame[0] = (byte)(message.Length >> 8);
        frame[1] = (byte)(message.Length & 255);
        Array.Copy(message, 0, frame, 2, message.Length);
        return frame;
    }

    static byte[] ResponseBytes(byte[] queryBytes)
    {
        DnsDecodeResult decoded = new DnsMessageCodec().TryDecodeMessage(
            queryBytes, new DnsDecodeOptions());
        Assert.That(decoded.Success, Is.True);
        return Encode(Response(decoded.Message!));
    }

    static DnsMessage Query(ushort identifier, string name)
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = identifier;
        message.Header.Flags = DnsHeaderFlag.RecursionDesired;
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(name);
        question.RecordType = DnsRecordType.A;
        question.RecordTypeValue = 1;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        message.Questions.Add(question);
        return message;
    }

    static DnsMessage Response(DnsMessage query)
    {
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = query.Header.Identifier;
        message.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionDesired |
            DnsHeaderFlag.RecursionAvailable;
        DnsQuestion question = new DnsQuestion();
        question.Name = MessageOperations.CloneMessage(query).Questions[0].Name;
        question.RecordType = query.Questions[0].RecordType;
        question.RecordTypeValue = query.Questions[0].RecordTypeValue;
        question.Class = query.Questions[0].Class;
        question.ClassValue = query.Questions[0].ClassValue;
        message.Questions.Add(question);
        return message;
    }

    static byte[] Encode(DnsMessage message)
    {
        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());
        Assert.That(encoded.Success, Is.True);
        return encoded.Bytes;
    }

    static DnsName Parse(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }

    sealed class CertificateFixture : IDisposable
    {
        internal X509Certificate2 Certificate;

        internal CertificateFixture()
        {
            byte[] pfx = Convert.FromBase64String(CertificateBase64);
            this.Certificate = X509CertificateLoader.LoadPkcs12(
                pfx,
                "test",
                X509KeyStorageFlags.EphemeralKeySet);
        }

        public void Dispose()
        {
            this.Certificate.Dispose();
        }

        const string CertificateBase64 =
            "MIIKFwIBAzCCCcUGCSqGSIb3DQEHAaCCCbYEggmyMIIJrjCCBBoGCSqGSIb3DQEH" +
            "BqCCBAswggQHAgEAMIIEAAYJKoZIhvcNAQcBMF8GCSqGSIb3DQEFDTBSMDEGCSqG" +
            "SIb3DQEFDDAkBBBkw/IDIqZ0iWLnT5+z23RxAgIIADAMBggqhkiG9w0CCQUAMB0G" +
            "CWCGSAFlAwQBKgQQFJr44rU0Z82rV4UB79g4C4CCA5ALJcTkC2K9Khcc1NvwOZXf" +
            "gNLczDAVqgzo3mJkCE2gb6Ubqq0yWHBX5GFO4WAdjMVRy5j02r46ple0L8Y+MoLL" +
            "wln38KYodVqondmzLhuxppKXXCT4zlXXcNxRKZBZz0slXOO6Y2/MEeOASxuZX9nY" +
            "ROTdL+LCLh4TsKq6WO/tGRMPkTSVRl1HlJgHIhiKyH7cuN8jQ5usikoTSnTwKqE5" +
            "tlCPrRlAzTlZh5k11o+Z9fforD0DR6VqqAWrm0aHqAy7eN2cOgUNW0GA7YGbmNb" +
            "v/YIl7CVX8q0c59oJ3XrhkFjd3vHq+LDmbfjUC2HwxSZK9ZKHcL9Swf59Gh9mWJ" +
            "lxZaM+N0HW7xMmPmqrGOTUyf5xY7hsxuIqGf9+C7SPNjwPXUvDJuSc5MSVnK68U" +
            "jxOgMMGr8VRHyxvQQKJsuIM7GNIc3LlLAqMDb4TzXzU27H70vDgmmRn1qPA+jRt" +
            "zILFqov4BgoOb6iJAig7iHBVg1RU+e70SGzy5vrCjMYxRnRGyySzNtK7lCBwz2Rv" +
            "Hw1jHE36BBVI9oVsoe3tMJLCkWHVxVlT5L+t8xXpKdCf+A5Lf34zRg/JHv6Z1Sx" +
            "XNXIyIscerb4dO7nlyU1uXQOESJp6D5hW9cipnk9Q8EQyZjagzrqkDFN4GW1UXJ" +
            "botjsYjuXUb37JhuVvnaZE+a1NlAAIkD2Y7iL6oCxAUbAk7CdiPXhwv6mYMbPkW" +
            "2kjSEPVXUrSJkodhOimYr/+UuN3DNLUbduDNISCc9Ddofb4bpOxcaYfJ9Xncq4R" +
            "04D7lq+Ut3z8tgRTd/lRnLOkVG06hYg4WiXdRJG9sfC7gxyjkR/JJTiypSwn96s" +
            "WIpZxXBP2cBh0fHPKwnP9yHDX2hxMtBP8InTVPRPztEupAyb+vl2GJcdcZrGLdtz" +
            "Pnz9B/OM5ndKpTdeigFegZ5/yNj4xV9HKZ0XK9ELM4S4m7ZHHTxjhom4DO+mDBAG" +
            "iwPZN7Hp98bTCaCRTL57Z0Q5PtMaXsoIdwMzZauFluv+21ZG/3IE4HtsJaghO6h6" +
            "DrUjABb2NJG1a74lg2uzfQnpsH/QwOxoJVtQ2fJDiFJTruKs2DqdHIxtF0UshMJ" +
            "oUvLXKqnRLTbzBn6mAc4Y0Tc0jcbab+U2/6wCtt8+KkcTJPKFhkMBi4akeWndZh" +
            "Ltwk0JEQv04saLmbRjopSoFd7YtQ87ZfegNTPAIyXQRB4QxAx2h/vIwggWMBgkq" +
            "hkiG9w0BBwGgggV9BIIFeTCCBXUwggVxBgsqhkiG9w0BDAoBAqCCBTkwggU1MF8G" +
            "CSqGSIb3DQEFDTBSMDEGCSqGSIb3DQEFDDAkBBBxJv+Z0UVCJpKm5ZUc54WdAgII" +
            "ADAMBggqhkiG9w0CCQUAMB0GCWCGSAFlAwQBKgQQkhKroOUt7/0BbmlFFNP0bASC" +
            "BNA0IOuofDp6IW+WSwhdbbQWCtg2RYe0ugJIuaMP/f6Op1fYJ1rD50fjMFiUxgRJ" +
            "9YpaEyJbBVGQWcOSf9XbP5aLcNIsXHyLZNpyykoz1M7P5beBmJrg9ug+IzaaIcVt" +
            "Z1GK41u6o9WS4c0IQNBHjVSYLcEaGKjkH95U0Bx7Av6V3bQo1TJgEeSa8/l+F3c" +
            "fvFZ8w/GZKu4DT/7XyhZ8oThW/Ee/JvNPiBa/BwraunFUWHaZG5VxGj6raHc70E" +
            "qDgfPtzQT1mz/78sN3SWzp4O6ViW+5pBlDpj6IWjyT6M5eqUslISxa7X1B3Jg/B" +
            "BrJ51rRDQWx2OVdRutgX1Tu2QjuZv6KJUFME9rhwD7czvrdocl9eOeukpBLbL07s" +
            "w01+o+WhmvLqOGjPPqZ5j2R2D+E4wi+7YcxzHD77EJtc+odoV+arsbd/Br14Nzol" +
            "GleCKP7nNjiFRet5XBpGCRE45dn8MUpicPvyynR3gnpqksCugq3D9DcM3vYVdhj/" +
            "EWffG/+ryeV0/l16iifgb9KeS7GysVnT4fLJumlh+LTApxJcRpgbRZzXhNbKrVs" +
            "K1AoVipHb4HJlY55jzu0siGCweFwZdA8qLusdj9KnSYizmjXANjlHgPf2oNQsn/L" +
            "uA8uf2ta0qYkauQjRcC6AV1T0/aSRN9WLMaGsL6ckpPy5/v8POppjeB1lttvJzdD" +
            "bwh8yX/gXSrixaEC/kFzXpsR7SD7YGQaaKB7cznX2OhkL7CgzyLMkjxcDjEB19Xb" +
            "oG0xHqIQslFajdjKaToKN754mycXJut28ZGVWtUJ+5mqMzr/xcj1YD0NcyUkQX4E" +
            "ozc/jlGYc3aAEH1dTbDNs0oNtBLLLzNHAoLdnwCKWLghZhdHmkJMULdUJ9i47+2m" +
            "eYNDYIB8RrlvhGLdQQYKnj5Axsbh6X6XdX7DAwCbgewXhIo/QBc5QjlGClB47e+5" +
            "nFxgEJPrJ7qcHpaisS1Gx3qsCHhx8Cwi3Qf3Xexn5eikQwleU647y8hjFgD6Ubcm" +
            "1sL0RBTqCJOXKvrd9taC/gykNDyhZEz0e4FcvN4aieDerCuCZHfNiEBdBx4KLPK8" +
            "FEJvL1bjnE8kT2XxmZvBJjI/gE8OLoPBrnrO2hQx6/PMpFcS341kQNofzZtsHj78" +
            "kP/jb80toJnFoPgGN+fypSJv5rDQ7FuB1NOSRthCxuQqqElnaOxpq0xZ8tM9Od3C" +
            "ZakOy63QVhbncy8vnzwn1xks1ms1E13hUFqhYplwVWJbL/Vxh7Meyoux9Xz0L1qn" +
            "CL6GhywyuyXHK4eSy71co5zsOZXKlyCMW6FB12JADbYCONvJuLDZfMQASb3e76Qc" +
            "M5PWuAe/d4zn/8CYLRtOB+fe6FQY+1vNXknxHP17+xbw9uMdL8RoCSyaoSERTYfl" +
            "mpFkJ2HJablr2iblHLJqz4t+2aNpct5FUVw4VcjGPjfLFIgFs6JmE+VwUjEF5sOZ" +
            "56cC9sS0xpi7hXHfa4c+QGdpqa0gqOkE6lU441M41svmsnbVCWPMG3kjFGwM0dsg" +
            "RJtTJUnBqrvrZSMydMtNy3RS/LwfeRBdrkEyx/RQjm5wwOzbE7spjQDRvtiJVdsZ" +
            "MjFSgLq8YjevcdXnsJRvWpGtkQdM1KaaZLt7/rHvNkEW8BAeMTElMCMGCSqGSIb3" +
            "DQEJFTEWBBQTJ5lXfqoitcYAbpIN0WEIBaazkzBJMDEwDQYJYIZIAWUDBAIBBQAE" +
            "IG+4NPVRETTCPssOaG+yyJVzEMinh62fJzTlW5Xpm6d/BBAXCxbIrFd/30/RTn7P" +
            "i+kgAgIIAA==";
    }

    sealed class CapturingTransport : IDnsTransport
    {
        internal List<DnsTransportRequest> Requests =
            new List<DnsTransportRequest>();

        public DnsTransportResult Exchange(DnsTransportRequest request)
        {
            this.Requests.Add(request);
            DnsTransportResult result = new DnsTransportResult();
            result.Status = DnsTransportStatus.Success;
            result.Transport = DnsTransportKind.Tls;
            result.Endpoint = request.Endpoint;
            result.ResponseMessage = Response(request.QueryMessage!);
            result.ResponseBytes = Encode(result.ResponseMessage);
            return result;
        }

        public void CloseTransport()
        {
        }
    }
}
