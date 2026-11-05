using System.Text;
using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetJwtTests
{
    [Test]
    public void JwtIsDeterministicUnsecuredAndBase64UrlEncoded()
    {
        InternetProvider first = Fake.Create(8701).Internet();
        InternetProvider second = Fake.Create(8701).Internet();

        string token = first.Jwt();
        string[] segments = token.Split('.');

        Assert.Multiple(() =>
        {
            Assert.That(token, Is.EqualTo(second.Jwt()));
            Assert.That(segments, Has.Length.EqualTo(3));
            Assert.That(segments[0], Does.Match("^[A-Za-z0-9_-]+$"));
            Assert.That(segments[1], Does.Match("^[A-Za-z0-9_-]+$"));
            Assert.That(segments[2], Is.Empty);
            Assert.That(token, Does.EndWith("."));
            Assert.That(token, Does.Not.Contain("="));
            Assert.That(token, Does.Not.Contain("+"));
            Assert.That(token, Does.Not.Contain("/"));
        });
    }

    [Test]
    public void JwtHeaderExplicitlyDeclaresTheUnsecuredAlgorithm()
    {
        InternetProvider internet = Fake.Create(8702).Internet();
        string[] segments = internet.Jwt().Split('.');

        using JsonDocument header = JsonDocument.Parse(Decode(segments[0]));

        Assert.Multiple(() =>
        {
            Assert.That(header.RootElement.GetProperty("alg").GetString(), Is.EqualTo("none"));
            Assert.That(header.RootElement.GetProperty("typ").GetString(), Is.EqualTo("JWT"));
            Assert.That(internet.JwtAlgorithm(), Is.EqualTo("none"));
        });
    }

    [Test]
    public void JwtClaimsCorrelateWithTheCapturedReferenceClockAndDefaultLifetime()
    {
        FakeOptions options = FakeOptions.Create(8703);
        options.ReferenceTime = new DateTimeOffset(2031, 4, 5, 6, 7, 8, TimeSpan.FromHours(5.5));
        InternetProvider internet = Fake.Create(options).Internet();
        string[] segments = internet.Jwt().Split('.');
        long issuedAt = options.ReferenceTime.ToUnixTimeSeconds();

        using JsonDocument payload = JsonDocument.Parse(Decode(segments[1]));

        Assert.Multiple(() =>
        {
            Assert.That(payload.RootElement.GetProperty("iss").GetString(), Is.EqualTo("https://example.com/issuer"));
            Assert.That(payload.RootElement.GetProperty("aud").GetString(), Is.EqualTo("https://example.com/audience"));
            Assert.That(payload.RootElement.GetProperty("sub").GetString(), Does.Match("^[a-z]+\\.[a-z]+\\.[0-9]{4}$"));
            Assert.That(payload.RootElement.GetProperty("iat").GetInt64(), Is.EqualTo(issuedAt));
            Assert.That(payload.RootElement.GetProperty("nbf").GetInt64(), Is.EqualTo(issuedAt));
            Assert.That(payload.RootElement.GetProperty("exp").GetInt64(), Is.EqualTo(issuedAt + 3600L));
            Assert.That(payload.RootElement.GetProperty("jti").GetString(), Does.Match("^fixture-[0-9a-f]{32}$"));
            Assert.That(payload.RootElement.GetProperty("synthetic").GetBoolean(), Is.True);
        });
    }

    [Test]
    public void JwtRecordMatchesEveryEncodedClaimAndSecurityMarker()
    {
        InternetProvider textInternet = Fake.Create(8704).Internet();
        InternetProvider recordInternet = Fake.Create(8704).Internet();
        JwtRecord record = recordInternet.JwtRecordFromLifetime(TimeSpan.FromMinutes(90.0));
        string token = textInternet.JwtFromLifetime(TimeSpan.FromMinutes(90.0));
        string[] segments = record.Token.Split('.');

        using JsonDocument header = JsonDocument.Parse(Decode(segments[0]));
        using JsonDocument payload = JsonDocument.Parse(Decode(segments[1]));

        Assert.Multiple(() =>
        {
            Assert.That(record.Token, Is.EqualTo(token));
            Assert.That(record.HeaderJson, Is.EqualTo(Decode(segments[0])));
            Assert.That(record.PayloadJson, Is.EqualTo(Decode(segments[1])));
            Assert.That(record.Algorithm, Is.EqualTo(header.RootElement.GetProperty("alg").GetString()));
            Assert.That(record.Type, Is.EqualTo(header.RootElement.GetProperty("typ").GetString()));
            Assert.That(record.Issuer, Is.EqualTo(payload.RootElement.GetProperty("iss").GetString()));
            Assert.That(record.Subject, Is.EqualTo(payload.RootElement.GetProperty("sub").GetString()));
            Assert.That(record.Audience, Is.EqualTo(payload.RootElement.GetProperty("aud").GetString()));
            Assert.That(record.IssuedAtUnixSeconds, Is.EqualTo(payload.RootElement.GetProperty("iat").GetInt64()));
            Assert.That(record.NotBeforeUnixSeconds, Is.EqualTo(payload.RootElement.GetProperty("nbf").GetInt64()));
            Assert.That(record.ExpiresAtUnixSeconds, Is.EqualTo(payload.RootElement.GetProperty("exp").GetInt64()));
            Assert.That(record.ExpiresAtUnixSeconds - record.IssuedAtUnixSeconds, Is.EqualTo(5400L));
            Assert.That(record.JwtId, Is.EqualTo(payload.RootElement.GetProperty("jti").GetString()));
            Assert.That(record.IsSigned, Is.False);
            Assert.That(record.IsAuthenticated, Is.False);
            Assert.That(record.IsSynthetic, Is.True);
        });
    }

    [Test]
    public void JwtLifetimeUsesWholeSecondsAndRejectsValuesBelowOneSecond()
    {
        JwtRecord record = Fake.Create(8705).Internet().JwtRecordFromLifetime(TimeSpan.FromMilliseconds(1500.0));
        InternetProvider internet = Fake.Create(8706).Internet();

        Assert.That(record.ExpiresAtUnixSeconds - record.IssuedAtUnixSeconds, Is.EqualTo(1L));
        Assert.That(
            () => internet.JwtFromLifetime(TimeSpan.Zero),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("lifetime"));
        Assert.That(
            () => internet.JwtFromLifetime(TimeSpan.FromMilliseconds(999.0)),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("lifetime"));
        Assert.That(
            () => internet.JwtRecordFromLifetime(TimeSpan.FromSeconds(-1.0)),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("lifetime"));
    }

    [Test]
    public void JwtHonorsTheConfiguredMaximumOutputSize()
    {
        string baseline = Fake.Create(8707).Internet().Jwt();
        FakeOptions exactOptions = FakeOptions.Create(8707);
        exactOptions.MaximumOutputSize = baseline.Length;
        FakeOptions shortOptions = FakeOptions.Create(8707);
        shortOptions.MaximumOutputSize = baseline.Length - 1;
        FakeOptions algorithmOptions = FakeOptions.Create(8707);
        algorithmOptions.MaximumOutputSize = 3;

        Assert.That(Fake.Create(exactOptions).Internet().Jwt(), Is.EqualTo(baseline));
        Assert.That(() => Fake.Create(shortOptions).Internet().Jwt(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(algorithmOptions).Internet().JwtAlgorithm(), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static string Decode(string segment)
    {
        string padded = segment.Replace('-', '+').Replace('_', '/');
        int remainder = padded.Length % 4;
        if (remainder == 2)
        {
            padded = padded + "==";
        }
        else if (remainder == 3)
        {
            padded = padded + "=";
        }
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
