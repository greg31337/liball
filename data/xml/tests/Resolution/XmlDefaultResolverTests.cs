using System.Net;
using System.Net.Sockets;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlDefaultResolverTests
{
    [Test]
    public void ResolvesAllowedFilesRelativeToABaseAndReturnsOwnedBytes()
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "document.xml");
        File.WriteAllText(path, "<root/>", Encoding.UTF8);
        try
        {
            XmlResolverPolicy policy = AllowedPolicy("file");
            policy.AllowedRoots = [directory];
            string baseUri = new Uri(directory + Path.DirectorySeparatorChar).AbsoluteUri;

            XmlResolvedResource resource = new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, "public-id", "document.xml", baseUri, policy);

            Assert.That(resource.Access, Is.EqualTo(XmlResourceAccess.Allowed));
            Assert.That(resource.Kind, Is.EqualTo(XmlResourceKind.Document));
            Assert.That(resource.PublicIdentifier, Is.EqualTo("public-id"));
            Assert.That(resource.SystemIdentifier, Is.EqualTo("document.xml"));
            Assert.That(resource.RequestedUri, Is.EqualTo(new Uri(new Uri(baseUri), "document.xml").AbsoluteUri));
            Assert.That(resource.AbsoluteUri, Is.EqualTo(new Uri(path).AbsoluteUri));
            Assert.That(resource.MediaType, Is.EqualTo("application/xml"));
            Assert.That(resource.Input!.Kind, Is.EqualTo(XmlInputKind.ByteMemory));
            Assert.That(Encoding.UTF8.GetString(resource.Input.Bytes.Span), Does.EndWith("<root/>"));
            Assert.That(resource.Input.BaseUri, Is.EqualTo(resource.AbsoluteUri));
            Assert.That(resource.Input.OwnsResource, Is.False);
            Assert.That(resource.OwnsResource, Is.False);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase("document.xml", "application/xml")]
    [TestCase("schema.XSD", "application/xml")]
    [TestCase("style.xsl", "application/xml")]
    [TestCase("style.XSLT", "application/xml")]
    [TestCase("document.dtd", "text/plain")]
    [TestCase("entity.ent", "text/plain")]
    [TestCase("notes.txt", "text/plain")]
    [TestCase("data.json", "application/json")]
    [TestCase("data.bin", null)]
    public void InfersFileMediaTypesCaseInsensitively(string fileName, string? expectedMediaType)
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, fileName);
        File.WriteAllText(path, "data", Encoding.ASCII);
        try
        {
            XmlResolverPolicy policy = AllowedPolicy("file");
            policy.AllowedRoots = [directory];

            XmlResolvedResource resource = new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, new Uri(path).AbsoluteUri, null, policy);

            Assert.That(resource.MediaType, Is.EqualTo(expectedMediaType));
            Assert.That(resource.Input!.MediaType, Is.EqualTo(expectedMediaType));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ClosesFileHandlesBeforeReturningBufferedContent()
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "data.xml");
        File.WriteAllText(path, "<root/>", Encoding.ASCII);
        try
        {
            XmlResolverPolicy policy = AllowedPolicy("file");
            policy.AllowedRoots = [directory];

            new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, new Uri(path).AbsoluteUri, null, policy);

            using FileStream exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.That(exclusive.Length, Is.GreaterThan(0));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MapsMissingAndOversizedFilesToStableResourceErrors()
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "large.txt");
        File.WriteAllText(path, "12345", Encoding.ASCII);
        try
        {
            XmlResolverPolicy policy = AllowedPolicy("file");
            policy.AllowedRoots = [directory];
            XmlResourceException missing = Assert.Throws<XmlResourceException>(() => new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, new Uri(Path.Combine(directory, "missing.xml")).AbsoluteUri, null, policy))!;
            Assert.That(missing.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceNotFound));

            policy.MaximumResourceBytes = 4;
            XmlResourceException limit = Assert.Throws<XmlResourceException>(() => new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.UnparsedText, null, new Uri(path).AbsoluteUri, null, policy))!;
            Assert.That(limit.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(limit.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Limit));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DeniesDefaultAndUnsupportedSchemeAccess()
    {
        XmlResolverPolicy deniedPolicy = new XmlResolverPolicy();
        XmlResourceException denied = Assert.Throws<XmlResourceException>(() => new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, "file:///tmp/data.xml", null, deniedPolicy))!;
        Assert.That(denied.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceDenied));

        XmlResolverPolicy unsupportedPolicy = AllowedPolicy("ftp");
        XmlResourceException unsupported = Assert.Throws<XmlResourceException>(() => new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, "ftp://example.test/data.xml", null, unsupportedPolicy))!;
        Assert.That(unsupported.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceDenied));
    }

    [Test]
    public void ResolvesHttpBytesAndTransportMetadata()
    {
        byte[] body = Encoding.UTF8.GetBytes("<root/>");
        using LoopbackHttpServer server = new LoopbackHttpServer(
        [
            HttpResponse("200 OK", "Content-Type: application/xml; charset=utf-8\r\n", body)
        ]);
        XmlResolverPolicy policy = AllowedPolicy("http");

        XmlResolvedResource resource = new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, server.BaseUri + "document.xml", null, policy);

        Assert.That(Encoding.UTF8.GetString(resource.Input!.Bytes.Span), Is.EqualTo("<root/>"));
        Assert.That(resource.MediaType, Is.EqualTo("application/xml"));
        Assert.That(resource.EncodingName, Is.EqualTo("utf-8"));
        Assert.That(resource.Input.MediaType, Is.EqualTo("application/xml"));
        Assert.That(resource.Input.EncodingName, Is.EqualTo("utf-8"));
    }

    [Test]
    public void SendsConfiguredAcceptMetadataWithHttpRequests()
    {
        LoopbackHttpServer server = new LoopbackHttpServer(
        [
            HttpResponse("200 OK", "Content-Type: text/plain\r\n", Encoding.UTF8.GetBytes("content"))
        ]);
        XmlResolverPolicy policy = AllowedPolicy("http");
        policy.Accept = "text/plain";
        policy.AcceptLanguage = "en-US, fr";

        new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.XIncludeText, null, server.BaseUri + "content.txt", null, policy);
        server.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(server.Requests, Has.Count.EqualTo(1));
            Assert.That(server.Requests[0], Does.Contain("Accept: text/plain\r\n"));
            Assert.That(server.Requests[0], Does.Contain("Accept-Language: en-US, fr\r\n"));
        });
    }

    [Test]
    public void FollowsAllowedRelativeHttpRedirectsAndRetainsRequestIdentity()
    {
        byte[] body = Encoding.ASCII.GetBytes("done");
        using LoopbackHttpServer server = new LoopbackHttpServer(
        [
            HttpResponse("302 Found", "Location: /final.txt\r\n", []),
            HttpResponse("200 OK", "Content-Type: text/plain\r\n", body)
        ]);
        XmlResolverPolicy policy = AllowedPolicy("http");
        policy.AllowRedirects = true;
        policy.MaximumRedirects = 2;
        string requested = server.BaseUri + "start";

        XmlResolvedResource resource = new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.UnparsedText, null, requested, null, policy);

        Assert.That(resource.RequestedUri, Is.EqualTo(requested));
        Assert.That(resource.AbsoluteUri, Is.EqualTo(server.BaseUri + "final.txt"));
        Assert.That(Encoding.ASCII.GetString(resource.Input!.Bytes.Span), Is.EqualTo("done"));
    }

    [Test]
    public void DetectsHttpRedirectCyclesBeforeRepeatingARequest()
    {
        using LoopbackHttpServer server = new LoopbackHttpServer(
        [
            HttpResponse("302 Found", "Location: /middle\r\n", []),
            HttpResponse("302 Found", "Location: /start\r\n", [])
        ]);
        XmlResolverPolicy policy = AllowedPolicy("http");
        policy.AllowRedirects = true;
        policy.MaximumRedirects = 4;
        string requested = server.BaseUri + "start";

        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, requested, null, policy))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceCycle));
        Assert.That(failure.Location!.SourceUri, Is.EqualTo(requested));
    }

    [Test]
    public void EnforcesHttpRedirectLimitAtTheRedirectingResource()
    {
        using LoopbackHttpServer server = new LoopbackHttpServer(
        [
            HttpResponse("302 Found", "Location: /one\r\n", []),
            HttpResponse("302 Found", "Location: /two\r\n", [])
        ]);
        XmlResolverPolicy policy = AllowedPolicy("http");
        policy.AllowRedirects = true;
        policy.MaximumRedirects = 1;

        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, server.BaseUri + "start", null, policy))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        Assert.That(failure.Location!.SourceUri, Is.EqualTo(server.BaseUri + "one"));
    }

    [Test]
    public void EnforcesHttpBodyLimitAndReportsTheResourceLocation()
    {
        using LoopbackHttpServer server = new LoopbackHttpServer(
        [
            HttpResponse("200 OK", "Content-Type: application/octet-stream\r\n", Encoding.ASCII.GetBytes("12345"))
        ]);
        XmlResolverPolicy policy = AllowedPolicy("http");
        policy.MaximumResourceBytes = 4;
        string requested = server.BaseUri + "large.bin";

        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.UnparsedText, null, requested, null, policy))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Limit));
        Assert.That(failure.Location!.SourceUri, Is.EqualTo(requested));
    }

    [Test]
    public void TrimsQuotedHttpCharsetAndLeavesAbsentMetadataUnset()
    {
        using LoopbackHttpServer server = new LoopbackHttpServer(
        [
            HttpResponse("200 OK", "Content-Type: application/xml; charset=\"utf-16\"\r\n", Encoding.Unicode.GetBytes("<root/>")),
            HttpResponse("200 OK", "", Encoding.ASCII.GetBytes("plain"))
        ]);
        XmlResolverPolicy policy = AllowedPolicy("http");

        XmlResolvedResource typed = new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, server.BaseUri + "typed", null, policy);
        XmlResolvedResource untyped = new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.UnparsedText, null, server.BaseUri + "untyped", null, policy);

        Assert.That(typed.MediaType, Is.EqualTo("application/xml"));
        Assert.That(typed.EncodingName, Is.EqualTo("utf-16"));
        Assert.That(typed.Input!.EncodingName, Is.EqualTo("utf-16"));
        Assert.That(untyped.MediaType, Is.Null);
        Assert.That(untyped.EncodingName, Is.Null);
    }

    [Test]
    public void RejectsDeniedRedirectsAndHttpFailureStatuses()
    {
        using (LoopbackHttpServer redirectServer = new LoopbackHttpServer(
        [
            HttpResponse("302 Found", "Location: /final\r\n", [])
        ]))
        {
            XmlResolverPolicy redirectPolicy = AllowedPolicy("http");
            XmlResourceException redirect = Assert.Throws<XmlResourceException>(() => new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, redirectServer.BaseUri + "start", null, redirectPolicy))!;
            Assert.That(redirect.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceDenied));
        }

        using (LoopbackHttpServer missingServer = new LoopbackHttpServer(
        [
            HttpResponse("404 Not Found", "Content-Type: text/plain\r\n", Encoding.ASCII.GetBytes("missing"))
        ]))
        {
            XmlResolverPolicy missingPolicy = AllowedPolicy("http");
            XmlResourceException missing = Assert.Throws<XmlResourceException>(() => new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, missingServer.BaseUri + "missing", null, missingPolicy))!;
            Assert.That(missing.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceNotFound));
            Assert.That(missing.Message, Does.Contain("404"));
        }
    }

    [Test]
    public void MapsHttpTransportFailuresToResourceExceptions()
    {
        TcpListener reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        int port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        XmlResolverPolicy policy = AllowedPolicy("http");

        XmlResourceException failure = Assert.Throws<XmlResourceException>(() => new XmlDefaultResolver().ResolveXmlResource(XmlResourceKind.Document, null, "http://127.0.0.1:" + port + "/missing", null, policy))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.ResourceNotFound));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Resource));
    }

    private static XmlResolverPolicy AllowedPolicy(string scheme)
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        policy.AllowedSchemes = [scheme];
        policy.MaximumRedirects = 0;
        policy.MaximumDepth = 4;
        policy.MaximumResourceBytes = 1048576;
        policy.MaximumWorkUnits = 2097152;
        return policy;
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "xml-default-resolver-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static byte[] HttpResponse(string status, string headers, byte[] body)
    {
        string head = "HTTP/1.1 " + status + "\r\n" + headers + "Content-Length: " + body.Length + "\r\nConnection: close\r\n\r\n";
        byte[] prefix = Encoding.ASCII.GetBytes(head);
        byte[] response = new byte[prefix.Length + body.Length];
        Buffer.BlockCopy(prefix, 0, response, 0, prefix.Length);
        Buffer.BlockCopy(body, 0, response, prefix.Length, body.Length);
        return response;
    }

    private sealed class LoopbackHttpServer : IDisposable
    {
        private readonly TcpListener listener;
        private readonly Task worker;
        public readonly string BaseUri;
        public readonly List<string> Requests;

        public LoopbackHttpServer(byte[][] responses)
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            BaseUri = "http://127.0.0.1:" + port + "/";
            Requests = [];
            worker = Task.Run(() => Serve(responses));
        }

        public void Dispose()
        {
            Assert.That(worker.Wait(TimeSpan.FromSeconds(5)), Is.True, "The loopback HTTP server did not finish.");
            listener.Stop();
            worker.Dispose();
        }

        private void Serve(byte[][] responses)
        {
            foreach (byte[] response in responses)
            {
                using TcpClient client = listener.AcceptTcpClient();
                using NetworkStream stream = client.GetStream();
                Requests.Add(ReadRequest(stream));
                stream.Write(response, 0, response.Length);
            }
        }

        private static string ReadRequest(NetworkStream stream)
        {
            StringBuilder request = new StringBuilder();
            int matched = 0;
            while (matched < 4)
            {
                int value = stream.ReadByte();
                if (value < 0)
                {
                    return request.ToString();
                }

                request.Append((char)value);

                if (matched == 0 && value == '\r' || matched == 2 && value == '\r')
                {
                    matched++;
                }
                else if (matched == 1 && value == '\n' || matched == 3 && value == '\n')
                {
                    matched++;
                }
                else
                {
                    matched = value == '\r' ? 1 : 0;
                }
            }

            return request.ToString();
        }
    }
}
