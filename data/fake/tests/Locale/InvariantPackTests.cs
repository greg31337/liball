using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class InvariantPackTests
{
    [Test]
    public void ResolvesApprovedDocumentationAndFictionalConstants()
    {
        LocaleProvider locale = Fake.Create().Locale();

        JsonElement domains = (JsonElement)locale.Resolve("internet.domain.documentation");
        JsonElement ipv4 = (JsonElement)locale.Resolve("internet.ipv4.documentation-prefixes");
        JsonElement ipv6 = (JsonElement)locale.Resolve("internet.ipv6.documentation-prefix");
        JsonElement phone = (JsonElement)locale.Resolve("phone.nanpa.fictional-subscriber-range");

        Assert.Multiple(() =>
        {
            Assert.That(
                domains.EnumerateArray().Select(value => value.GetString()),
                Is.EqualTo(new string[] { "example.com", "example.net", "example.org" }));
            Assert.That(
                ipv4.EnumerateArray().Select(value => value.GetString()),
                Is.EqualTo(new string[] { "192.0.2.0/24", "198.51.100.0/24", "203.0.113.0/24" }));
            Assert.That(ipv6.GetString(), Is.EqualTo("2001:db8::/32"));
            Assert.That(phone.GetProperty("exchange").GetString(), Is.EqualTo("555"));
            Assert.That(phone.GetProperty("first").GetString(), Is.EqualTo("0100"));
            Assert.That(phone.GetProperty("last").GetString(), Is.EqualTo("0199"));
        });
    }

    [Test]
    public void TraceIdentifiesInvariantSourceVersionAndProvenance()
    {
        FallbackTrace trace = Fake.Create().Locale().FallbackTrace("internet.ipv6.documentation-prefix");

        Assert.Multiple(() =>
        {
            Assert.That(trace.RequestedLocale, Is.EqualTo("en-US"));
            Assert.That(trace.SourceLocale, Is.EqualTo("invariant"));
            Assert.That(trace.AttemptedLocales, Is.EqualTo(new string[] { "en-US", "en", "invariant" }));
            Assert.That(trace.UsedFallback, Is.True);
            Assert.That(trace.DataVersion, Is.EqualTo("1"));
            Assert.That(trace.Provenance, Does.Contain("IETF RFC 3849"));
            Assert.That(trace.Provenance, Does.Contain("license IETF-Trust"));
            Assert.That(trace.Provenance, Does.Contain("repository-data-review-2026-08-14"));
        });
    }

    [Test]
    public void StrictModeDoesNotUseInvariantFallback()
    {
        FakeOptions options = new FakeOptions();
        options.StrictnessMode = StrictnessMode.Strict;
        LocaleProvider locale = Fake.Create(options).Locale();

        FakeException exception = Assert.Throws<FakeException>(
            () => locale.Resolve("internet.domain.documentation"));

        Assert.That(
            exception.Message,
            Is.EqualTo("Locale field 'internet.domain.documentation' is unavailable for exact locale 'en-US' because strict fallback is enabled."));
    }

    [Test]
    public void ResolvingInvariantDataDoesNotConsumeRandomEntropy()
    {
        Fake fake = Fake.Create(90210);
        FakeState before = fake.Snapshot();

        fake.Locale().Resolve("internet.domain.documentation");
        fake.Locale().FallbackTrace("phone.nanpa.fictional-subscriber-range");
        fake.Locale().Coverage("en-US");

        AssertStateEqual(fake.Snapshot(), before);
    }

    static void AssertStateEqual(FakeState actual, FakeState expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Algorithm, Is.EqualTo(expected.Algorithm));
            Assert.That(actual.InitialSeed, Is.EqualTo(expected.InitialSeed));
            Assert.That(actual.State0, Is.EqualTo(expected.State0));
            Assert.That(actual.State1, Is.EqualTo(expected.State1));
            Assert.That(actual.State2, Is.EqualTo(expected.State2));
            Assert.That(actual.State3, Is.EqualTo(expected.State3));
        });
    }
}
