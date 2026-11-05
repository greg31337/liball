namespace dns.Tests.Api;

public class DnsCookieServiceTests
{
    [Test]
    public void CreatesUnpredictableClientCookieForm()
    {
        IDnsCookieService service = new DnsCookieService();

        DnsEdnsCookie cookie = service.CreateClientCookie();

        Assert.Multiple(() =>
        {
            Assert.That(cookie.ClientCookie, Has.Length.EqualTo(8));
            Assert.That(cookie.ServerCookie, Is.Empty);
        });
    }

    [Test]
    public void CreatesRfc9018Ipv4TestVector()
    {
        IDnsCookieService service = new DnsCookieService();
        DnsEdnsCookie request = Request("2464c4abcf10c957");
        byte[] address = Hex("c6336464");
        byte[] secret = Hex("e5e973e5a6b2a43f48e7dc849e37bfcf");

        DnsEdnsCookie cookie = service.CreateServerCookie(
            request, address, secret, 1559731985);

        Assert.Multiple(() =>
        {
            Assert.That(cookie.ClientCookie,
                Is.EqualTo(Hex("2464c4abcf10c957")));
            Assert.That(cookie.ServerCookie,
                Is.EqualTo(Hex("010000005cf79f111f8130c3eee29480")));
            Assert.That(cookie.ClientCookie,
                Is.Not.SameAs(request.ClientCookie));
        });
    }

    [Test]
    public void CreatesRfc9018Ipv6TestVector()
    {
        IDnsCookieService service = new DnsCookieService();
        DnsEdnsCookie request = Request("22681ab97d52c298");
        byte[] address = Hex("20010db80220000159ded0f4876982b8");
        byte[] secret = Hex("445536bcd2513298075a5d379663c962");

        DnsEdnsCookie cookie = service.CreateServerCookie(
            request, address, secret, 1559741961);

        Assert.That(cookie.ServerCookie,
            Is.EqualTo(Hex("010000005cf7c609a6bb79d16625507a")));
    }

    [Test]
    public void VerifiesReservedBytesFromRfc9018TestVector()
    {
        IDnsCookieService service = new DnsCookieService();
        DnsEdnsCookie cookie = Request("fc93fc62807ddb86");
        cookie.ServerCookie = Hex(
            "01abcdef5cf78f71a314227b6679ebf5");

        bool valid = service.VerifyServerCookie(
            cookie,
            Hex("cb0071cb"),
            Hex("e5e973e5a6b2a43f48e7dc849e37bfcf"),
            1559727985,
            3600,
            300);

        Assert.That(valid, Is.True);
    }

    [Test]
    public void RejectsTamperingWrongBindingsAndExpiredTimestamps()
    {
        IDnsCookieService service = new DnsCookieService();
        byte[] address = Hex("c6336464");
        byte[] secret = Hex("e5e973e5a6b2a43f48e7dc849e37bfcf");
        DnsEdnsCookie cookie = service.CreateServerCookie(
            Request("2464c4abcf10c957"), address, secret, 1000);
        DnsEdnsCookie tampered = Copy(cookie);
        tampered.ServerCookie[15] ^= 1;
        DnsEdnsCookie wrongVersion = Copy(cookie);
        wrongVersion.ServerCookie[0] = 2;

        Assert.Multiple(() =>
        {
            Assert.That(Verify(service, cookie, address, secret, 4600),
                Is.True);
            Assert.That(Verify(service, cookie, address, secret, 4601),
                Is.False);
            Assert.That(Verify(service, cookie, address, secret, 700),
                Is.True);
            Assert.That(Verify(service, cookie, address, secret, 699),
                Is.False);
            Assert.That(Verify(service, tampered, address, secret, 1000),
                Is.False);
            Assert.That(Verify(
                service, cookie, Hex("c6336465"), secret, 1000),
                Is.False);
            Assert.That(Verify(
                service, cookie, address, new byte[16], 1000),
                Is.False);
            Assert.That(Verify(
                service, wrongVersion, address, secret, 1000),
                Is.False);
        });
    }

    [Test]
    public void TimestampVerificationUsesSerialNumberWraparound()
    {
        IDnsCookieService service = new DnsCookieService();
        byte[] address = Hex("c0000201");
        byte[] secret = Hex("000102030405060708090a0b0c0d0e0f");
        DnsEdnsCookie past = service.CreateServerCookie(
            Request("0001020304050607"),
            address,
            secret,
            uint.MaxValue - 49);
        DnsEdnsCookie future = service.CreateServerCookie(
            Request("08090a0b0c0d0e0f"), address, secret, 50);

        Assert.Multiple(() =>
        {
            Assert.That(service.VerifyServerCookie(
                past, address, secret, 50, 100, 0), Is.True);
            Assert.That(service.VerifyServerCookie(
                past, address, secret, 50, 99, 0), Is.False);
            Assert.That(service.VerifyServerCookie(
                future,
                address,
                secret,
                uint.MaxValue - 49,
                0,
                100),
                Is.True);
        });
    }

    [Test]
    public void RejectsInvalidGenerationAndVerificationConfiguration()
    {
        IDnsCookieService service = new DnsCookieService();
        DnsEdnsCookie request = Request("0001020304050607");
        byte[] address = Hex("c0000201");
        byte[] secret = new byte[16];

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                service.CreateServerCookie(
                    Request("0001"), address, secret, 0));
            Assert.Throws<ArgumentException>(() =>
                service.CreateServerCookie(
                    request, new byte[5], secret, 0));
            Assert.Throws<ArgumentException>(() =>
                service.CreateServerCookie(
                    request, address, new byte[15], 0));
            Assert.Throws<ArgumentException>(() =>
                service.VerifyServerCookie(
                    request, address, secret, 0, -1, 300));
        });
    }

    static bool Verify(
        IDnsCookieService service,
        DnsEdnsCookie cookie,
        byte[] address,
        byte[] secret,
        uint currentTimestamp)
    {
        return service.VerifyServerCookie(
            cookie, address, secret, currentTimestamp, 3600, 300);
    }

    static DnsEdnsCookie Request(string clientCookie)
    {
        DnsEdnsCookie cookie = new DnsEdnsCookie();
        cookie.ClientCookie = Hex(clientCookie);
        return cookie;
    }

    static DnsEdnsCookie Copy(DnsEdnsCookie source)
    {
        DnsEdnsCookie copy = new DnsEdnsCookie();
        copy.ClientCookie = source.ClientCookie.ToArray();
        copy.ServerCookie = source.ServerCookie.ToArray();
        return copy;
    }

    static byte[] Hex(string text)
    {
        return Convert.FromHexString(text);
    }
}
