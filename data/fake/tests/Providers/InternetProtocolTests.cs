using fake;

namespace fake.Tests;

[TestFixture]
public class InternetProtocolTests
{
    [Test]
    public void DefaultAndNamedPortMethodsUseExactIanaRangesDeterministically()
    {
        InternetProvider first = Fake.Create(8501).Internet();
        InternetProvider second = Fake.Create(8501).Internet();

        int index = 0;
        while (index < 1024)
        {
            int dynamicPort = first.Port();
            int systemPort = first.SystemPort();
            int registeredPort = first.RegisteredPort();
            int namedDynamicPort = first.DynamicPort();

            Assert.That(dynamicPort, Is.EqualTo(second.Port()));
            Assert.That(systemPort, Is.EqualTo(second.SystemPort()));
            Assert.That(registeredPort, Is.EqualTo(second.RegisteredPort()));
            Assert.That(namedDynamicPort, Is.EqualTo(second.DynamicPort()));
            Assert.That(dynamicPort, Is.InRange(49152, 65535));
            Assert.That(systemPort, Is.InRange(0, 1023));
            Assert.That(registeredPort, Is.InRange(1024, 49151));
            Assert.That(namedDynamicPort, Is.InRange(49152, 65535));
            index++;
        }
    }

    [Test]
    public void ExplicitPortKindsUseTheirNonOverlappingRanges()
    {
        InternetProvider internet = Fake.Create(8502).Internet();

        int index = 0;
        while (index < 512)
        {
            Assert.That(internet.PortFromKind(PortKind.System), Is.InRange(0, 1023));
            Assert.That(internet.PortFromKind(PortKind.Registered), Is.InRange(1024, 49151));
            Assert.That(internet.PortFromKind(PortKind.Dynamic), Is.InRange(49152, 65535));
            index++;
        }

        Assert.That(
            () => internet.PortFromKind((PortKind)999),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("kind"));
    }

    [Test]
    public void TransportProtocolCatalogIsStableCopiedAndDrivesGeneration()
    {
        InternetProvider first = Fake.Create(8503).Internet();
        InternetProvider second = Fake.Create(8503).Internet();
        string[] protocols = first.Protocols();

        Assert.That(protocols, Is.EqualTo(new string[] { "dccp", "sctp", "tcp", "udp" }));
        protocols[0] = "changed";
        Assert.That(first.Protocols(), Is.EqualTo(new string[] { "dccp", "sctp", "tcp", "udp" }));

        int index = 0;
        while (index < 512)
        {
            string protocol = first.Protocol();
            Assert.That(protocol, Is.EqualTo(second.Protocol()));
            Assert.That(protocol, Is.AnyOf("dccp", "sctp", "tcp", "udp"));
            index++;
        }
    }

    [Test]
    public void KnownIanaServiceAssignmentsAreReturnedAsCorrelatedRecords()
    {
        InternetProvider internet = Fake.Create(8504).Internet();
        NetworkServiceRecord http = internet.ServiceRecordFromServiceAndTransport("HTTP", "TCP");
        NetworkServiceRecord https = internet.ServiceRecordFromServiceAndTransport("https", "udp");
        NetworkServiceRecord domain = internet.ServiceRecordFromServiceAndTransport("domain", "udp");

        Assert.Multiple(() =>
        {
            Assert.That(http.Service, Is.EqualTo("http"));
            Assert.That(http.Port, Is.EqualTo(80));
            Assert.That(http.Transport, Is.EqualTo("tcp"));
            Assert.That(http.PortKind, Is.EqualTo(PortKind.System));
            Assert.That(https.Port, Is.EqualTo(443));
            Assert.That(https.Transport, Is.EqualTo("udp"));
            Assert.That(domain.Port, Is.EqualTo(53));
            Assert.That(http.RegistryVersion, Is.Not.Empty);
            Assert.That(internet.ServiceVersion(), Is.EqualTo(http.RegistryVersion));
        });
    }

    [Test]
    public void GeneratedServicesComeFromAssignedSinglePortIanaRecords()
    {
        InternetProvider firstName = Fake.Create(8505).Internet();
        InternetProvider firstRecord = Fake.Create(8505).Internet();
        InternetProvider secondRecord = Fake.Create(8505).Internet();

        int index = 0;
        while (index < 1024)
        {
            string service = firstName.Service();
            NetworkServiceRecord record = firstRecord.ServiceRecord();
            NetworkServiceRecord duplicate = secondRecord.ServiceRecord();

            Assert.Multiple(() =>
            {
                Assert.That(service, Is.EqualTo(record.Service));
                Assert.That(record.Service, Is.EqualTo(duplicate.Service));
                Assert.That(record.Port, Is.EqualTo(duplicate.Port));
                Assert.That(record.Transport, Is.EqualTo(duplicate.Transport));
                Assert.That(record.Service, Is.Not.Empty);
                Assert.That(record.Service, Is.EqualTo(record.Service.ToLowerInvariant()));
                Assert.That(record.Service.Any(char.IsWhiteSpace), Is.False);
                Assert.That(record.Port, Is.InRange(0, 65535));
                Assert.That(record.Transport, Is.AnyOf("dccp", "sctp", "tcp", "udp"));
                Assert.That(record.PortKind, Is.EqualTo(Kind(record.Port)));
                Assert.That(record.RegistryVersion, Is.Not.Empty);
            });
            index++;
        }
    }

    [TestCase("dccp")]
    [TestCase("sctp")]
    [TestCase("tcp")]
    [TestCase("udp")]
    public void ServiceFilteringHonorsEverySupportedTransport(string transport)
    {
        InternetProvider names = Fake.Create(8506).Internet();
        InternetProvider records = Fake.Create(8506).Internet();

        int index = 0;
        while (index < 128)
        {
            string service = names.ServiceFromTransport(transport.ToUpperInvariant());
            NetworkServiceRecord record = records.ServiceRecordFromTransport(transport);

            Assert.That(service, Is.EqualTo(record.Service));
            Assert.That(record.Transport, Is.EqualTo(transport));
            index++;
        }
    }

    [Test]
    public void ServiceArgumentsAndReturnedRegistryRecordsAreIsolated()
    {
        InternetProvider internet = Fake.Create(8507).Internet();
        NetworkServiceRecord first = internet.ServiceRecordFromServiceAndTransport("http", "tcp");
        first.Service = "changed";
        first.Port = 9999;
        NetworkServiceRecord second = internet.ServiceRecordFromServiceAndTransport("http", "tcp");

        Assert.That(second.Service, Is.EqualTo("http"));
        Assert.That(second.Port, Is.EqualTo(80));
        Assert.That(() => internet.ServiceFromTransport(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => internet.ServiceFromTransport(" "), Throws.TypeOf<ArgumentException>());
        Assert.That(() => internet.ServiceFromTransport("quic"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => internet.ServiceRecordFromServiceAndTransport("missing-service", "tcp"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => internet.ServiceRecordFromServiceAndTransport(null, "tcp"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => internet.ServiceRecordFromServiceAndTransport("http", null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void HttpMethodsAndHeadersAreDeterministicRegisteredTokenSets()
    {
        InternetProvider first = Fake.Create(8508).Internet();
        InternetProvider second = Fake.Create(8508).Internet();
        string[] methods = first.HttpMethods();
        string[] headers = first.HttpHeaders();

        Assert.That(methods, Is.EqualTo(new string[]
        {
            "CONNECT", "DELETE", "GET", "HEAD", "OPTIONS",
            "PATCH", "POST", "PUT", "TRACE"
        }));
        Assert.That(headers, Does.Contain("Content-Type"));
        Assert.That(headers, Does.Contain("Host"));
        Assert.That(headers, Does.Contain("User-Agent"));
        Assert.That(headers, Does.Contain("WWW-Authenticate"));
        Assert.That(headers, Is.Unique);

        int index = 0;
        while (index < 512)
        {
            string method = first.HttpMethod();
            string header = first.HttpHeader();

            Assert.That(method, Is.EqualTo(second.HttpMethod()));
            Assert.That(header, Is.EqualTo(second.HttpHeader()));
            Assert.That(methods, Does.Contain(method));
            Assert.That(headers, Does.Contain(header));
            Assert.That(IsToken(method), Is.True);
            Assert.That(IsToken(header), Is.True);
            index++;
        }
    }

    [Test]
    public void HttpStatusCodesAndRecordsRemainCorrelated()
    {
        InternetProvider codes = Fake.Create(8509).Internet();
        InternetProvider records = Fake.Create(8509).Internet();

        int index = 0;
        while (index < 1024)
        {
            int code = codes.HttpStatus();
            HttpStatusRecord record = records.HttpStatusRecord();

            Assert.Multiple(() =>
            {
                Assert.That(record.Code, Is.EqualTo(code));
                Assert.That(record.StatusClass, Is.EqualTo(code / 100));
                Assert.That(record.IsError, Is.EqualTo(code >= 400));
                Assert.That(record.ReasonPhrase, Is.Not.Empty);
                Assert.That(record.Text, Is.EqualTo(code + " " + record.ReasonPhrase));
                Assert.That(records.HttpReasonPhrase(code), Is.EqualTo(record.ReasonPhrase));
            });
            index++;
        }
    }

    [Test]
    public void KnownHttpStatusesAndInvalidCodesHaveExplicitBehavior()
    {
        InternetProvider internet = Fake.Create(8510).Internet();

        Assert.That(internet.HttpStatusRecordFromStatusCode(103).Text, Is.EqualTo("103 Early Hints"));
        Assert.That(internet.HttpStatusRecordFromStatusCode(200).Text, Is.EqualTo("200 OK"));
        Assert.That(internet.HttpStatusRecordFromStatusCode(404).Text, Is.EqualTo("404 Not Found"));
        Assert.That(internet.HttpStatusRecordFromStatusCode(503).IsError, Is.True);
        Assert.That(internet.HttpReasonPhrase(308), Is.EqualTo("Permanent Redirect"));
        Assert.That(
            () => internet.HttpStatusRecordFromStatusCode(199),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("statusCode"));
        Assert.That(
            () => internet.HttpReasonPhrase(999),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("statusCode"));
    }

    [Test]
    public void MimeTypesComeFromTheBundledIanaRegistryAndAreValidTokens()
    {
        InternetProvider first = Fake.Create(8511).Internet();
        InternetProvider second = Fake.Create(8511).Internet();
        string[] all = first.MimeTypes();

        Assert.That(all.Length, Is.GreaterThan(2000));
        Assert.That(all, Does.Contain("application/json"));
        Assert.That(all, Does.Contain("application/octet-stream"));
        Assert.That(all, Does.Contain("image/png"));
        Assert.That(all, Does.Contain("text/plain"));
        Assert.That(first.IsMimeType("APPLICATION/JSON"), Is.True);
        Assert.That(first.IsMimeType("application/not-registered"), Is.False);
        Assert.That(first.MimeTypeVersion(), Is.Not.Empty);

        int index = 0;
        while (index < 1024)
        {
            string value = first.MimeType();
            string[] pieces = value.Split('/');

            Assert.That(value, Is.EqualTo(second.MimeType()));
            Assert.That(first.IsMimeType(value), Is.True);
            Assert.That(pieces.Length, Is.EqualTo(2));
            Assert.That(IsToken(pieces[0]), Is.True);
            Assert.That(IsToken(pieces[1]), Is.True);
            index++;
        }
    }

    [TestCase("application")]
    [TestCase("audio")]
    [TestCase("font")]
    [TestCase("haptics")]
    [TestCase("image")]
    [TestCase("message")]
    [TestCase("model")]
    [TestCase("multipart")]
    [TestCase("text")]
    [TestCase("video")]
    public void MimeTypeFilteringHonorsRegisteredTopLevelTypes(string topLevelType)
    {
        InternetProvider internet = Fake.Create(8512).Internet();

        int index = 0;
        while (index < 64)
        {
            string value = internet.MimeTypeFromTopLevelType(topLevelType.ToUpperInvariant());
            Assert.That(value, Does.StartWith(topLevelType + "/").IgnoreCase);
            Assert.That(internet.IsMimeType(value), Is.True);
            index++;
        }
    }

    [Test]
    public void MimeCatalogCopiesAndArgumentsAreIsolated()
    {
        InternetProvider internet = Fake.Create(8513).Internet();
        string[] first = internet.MimeTypes();
        string original = first[0];
        first[0] = "changed/changed";

        Assert.That(internet.MimeTypes()[0], Is.EqualTo(original));
        Assert.That(() => internet.MimeTypeFromTopLevelType(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => internet.MimeTypeFromTopLevelType(" "), Throws.TypeOf<ArgumentException>());
        Assert.That(() => internet.MimeTypeFromTopLevelType("bad/type"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => internet.MimeTypeFromTopLevelType("unknown"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => internet.IsMimeType(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => internet.IsMimeType(" "), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void ProtocolCollectionsAndTextOutputsHonorConfiguredLimits()
    {
        FakeOptions options = FakeOptions.Create(8514);
        options.MaximumCount = 3;
        options.MaximumOutputSize = 3;
        InternetProvider internet = Fake.Create(options).Internet();

        Assert.That(() => internet.Protocols(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.HttpMethods(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.HttpHeaders(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.MimeTypes(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.ServiceRecordFromServiceAndTransport("http", "tcp"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.HttpStatusRecordFromStatusCode(404), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => internet.MimeTypeFromTopLevelType("application"), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static PortKind Kind(int port)
    {
        if (port <= 1023)
        {
            return PortKind.System;
        }
        if (port <= 49151)
        {
            return PortKind.Registered;
        }
        return PortKind.Dynamic;
    }

    static bool IsToken(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        const string punctuation = "!#$%&'*+-.^_`|~";
        return value.All(character =>
            char.IsAsciiLetterOrDigit(character) || punctuation.Contains(character));
    }
}
