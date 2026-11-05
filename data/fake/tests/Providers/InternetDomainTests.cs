using fake;

namespace fake.Tests;

[TestFixture]
public class InternetDomainTests
{
    [Test]
    public void DomainWordsAreDeterministicAndRespectDnsLabelRules()
    {
        InternetProvider first = Fake.Create(7901).Internet();
        InternetProvider second = Fake.Create(7901).Internet();

        for (int length = 1; length <= 63; length++)
        {
            string actual = first.DomainWordFromLength(length);
            Assert.That(actual, Is.EqualTo(second.DomainWordFromLength(length)));
            Assert.That(actual, Has.Length.EqualTo(length));
            Assert.That(actual, Does.Match("^[a-z](?:[a-z0-9-]*[a-z0-9])?$"));
        }
    }

    [Test]
    public void DefaultDomainWordsUseTheDocumentedLengthRange()
    {
        InternetProvider internet = Fake.Create(7902).Internet();

        int index = 0;
        while (index < 256)
        {
            string value = internet.DomainWord();
            Assert.That(value.Length, Is.InRange(5, 12));
            Assert.That(value, Does.Match("^[a-z](?:[a-z0-9-]*[a-z0-9])?$"));
            index++;
        }
    }

    [TestCase(0)]
    [TestCase(64)]
    [TestCase(-1)]
    public void DomainWordRejectsLengthsOutsideDnsLimits(int length)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Fake.Create(7903).Internet().DomainWordFromLength(length));

        Assert.That(exception.ParamName, Is.EqualTo("length"));
    }

    [Test]
    public void BundledSuffixesExposeTheCurrentReviewedIanaSnapshot()
    {
        InternetProvider internet = Fake.Create(7904).Internet();

        string[] first = internet.DomainSuffixes();
        string[] second = internet.DomainSuffixes();

        Assert.Multiple(() =>
        {
            Assert.That(internet.DomainSuffixVersion(), Is.EqualTo("serial 2026081400"));
            Assert.That(first, Has.Length.EqualTo(1438));
            Assert.That(first[0], Is.EqualTo("aaa"));
            Assert.That(first[first.Length - 1], Is.EqualTo("zw"));
            Assert.That(first, Does.Contain("com"));
            Assert.That(first, Does.Contain("xn--vermgensberater-ctb"));
            Assert.That(first, Is.EqualTo(first.OrderBy(value => value, StringComparer.Ordinal).ToArray()));
            Assert.That(new HashSet<string>(first).Count, Is.EqualTo(first.Length));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(second, Is.Not.SameAs(first));
        });

        first[0] = "changed";
        Assert.That(internet.DomainSuffixes()[0], Is.EqualTo("aaa"));
    }

    [Test]
    public void DomainSuffixSelectionIsDeterministicAndRegistryBacked()
    {
        InternetProvider first = Fake.Create(7905).Internet();
        InternetProvider second = Fake.Create(7905).Internet();
        HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);

        int index = 0;
        while (index < 4096)
        {
            string value = first.DomainSuffix();
            Assert.That(value, Is.EqualTo(second.DomainSuffix()));
            Assert.That(first.IsDomainSuffix(value), Is.True);
            Assert.That(value, Does.Match("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$"));
            selected.Add(value);
            index++;
        }

        Assert.That(selected.Count, Is.GreaterThan(1000));
        Assert.That(first.IsDomainSuffix("COM"), Is.True);
        Assert.That(first.IsDomainSuffix("not-a-current-root-zone-tld"), Is.False);
    }

    [Test]
    public void ReservedModeKeepsDomainsAndHostsUnderExampleDomains()
    {
        InternetProvider internet = Fake.Create(7906).Internet();
        string[] examples = new string[] { "example.com", "example.net", "example.org" };

        int index = 0;
        while (index < 256)
        {
            string example = internet.ExampleDomain();
            string domain = internet.DomainName();
            string host = internet.HostName();

            Assert.That(examples, Does.Contain(example));
            Assert.That(examples.Any(value => domain.EndsWith("." + value, StringComparison.Ordinal)), Is.True);
            Assert.That(examples.Any(value => host.EndsWith("." + value, StringComparison.Ordinal)), Is.True);
            AssertValidAsciiDomain(domain);
            AssertValidAsciiDomain(host);
            Assert.That(host.Count(character => character == '.'), Is.GreaterThan(domain.Count(character => character == '.')));
            index++;
        }
    }

    [Test]
    public void PlausibleModeUsesARegisteredSuffixWithoutMakingItReserved()
    {
        FakeOptions options = FakeOptions.Create(7907);
        options.SafetyMode = SafetyMode.SyntheticPlausible;
        InternetProvider internet = Fake.Create(options).Internet();

        int index = 0;
        while (index < 256)
        {
            string domain = internet.DomainName();
            string suffix = domain.Substring(domain.LastIndexOf('.') + 1);
            Assert.That(internet.IsDomainSuffix(suffix), Is.True);
            AssertValidAsciiDomain(domain);
            index++;
        }
    }

    [TestCase("bücher.example", "xn--bcher-kva.example")]
    [TestCase("例え.example", "xn--r8jz45g.example")]
    [TestCase("BÜCHER。EXAMPLE.", "xn--bcher-kva.example.")]
    public void IdnConversionMatchesCanonicalPunycodeVectors(string unicode, string ascii)
    {
        InternetProvider internet = Fake.Create(7908).Internet();

        Assert.That(internet.ToAsciiDomain(unicode), Is.EqualTo(ascii));
        Assert.That(internet.ToAsciiDomain(internet.ToUnicodeDomain(ascii)), Is.EqualTo(ascii));
    }

    [Test]
    public void GeneratedIdnAndPunycodeFormsAreDeterministicReservedAndRoundTrip()
    {
        InternetProvider unicodeProvider = Fake.Create(7909).Internet();
        InternetProvider asciiProvider = Fake.Create(7909).Internet();

        int index = 0;
        while (index < 128)
        {
            string unicode = unicodeProvider.IdnDomainName();
            string ascii = asciiProvider.PunycodeDomainName();

            Assert.That(unicode.Any(character => character > 127), Is.True);
            Assert.That(ascii, Does.StartWith("xn--"));
            Assert.That(ascii.All(character => character <= 127), Is.True);
            Assert.That(unicodeProvider.ToAsciiDomain(unicode), Is.EqualTo(ascii));
            Assert.That(unicodeProvider.ToAsciiDomain(unicodeProvider.ToUnicodeDomain(ascii)), Is.EqualTo(ascii));
            Assert.That(
                new string[] { "example.com", "example.net", "example.org" }
                    .Any(value => ascii.EndsWith("." + value, StringComparison.Ordinal)),
                Is.True);
            index++;
        }
    }

    [Test]
    public void DomainLengthLimitCountsAsciiPunycodeAndAllowsA253CharacterName()
    {
        InternetProvider internet = Fake.Create(7910).Internet();
        string maximum = string.Join(
            ".",
            new string('a', 63),
            new string('b', 63),
            new string('c', 63),
            new string('d', 61));
        string overlong = string.Join(
            ".",
            new string('a', 63),
            new string('b', 63),
            new string('c', 63),
            new string('d', 62));

        Assert.That(maximum, Has.Length.EqualTo(253));
        Assert.That(internet.ToAsciiDomain(maximum), Is.EqualTo(maximum));
        Assert.That(internet.ToAsciiDomain(maximum + "."), Is.EqualTo(maximum + "."));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => internet.ToAsciiDomain(overlong)).ParamName,
            Is.EqualTo("domainName"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(".example")]
    [TestCase("example..com")]
    [TestCase("-bad.example")]
    [TestCase("bad-.example")]
    [TestCase("bad_label.example")]
    public void IdnConversionRejectsMalformedDomains(string value)
    {
        Assert.Throws<ArgumentException>(() => Fake.Create(7911).Internet().ToAsciiDomain(value));
    }

    [Test]
    public void IdnConversionRejectsNullOverlongAndInvalidUnicodeLabels()
    {
        InternetProvider internet = Fake.Create(7912).Internet();

        Assert.That(
            Assert.Throws<ArgumentNullException>(() => internet.ToAsciiDomain(null)).ParamName,
            Is.EqualTo("domainName"));
        Assert.Throws<ArgumentException>(() => internet.ToAsciiDomain("bad\ud800.example"));
        Assert.Throws<ArgumentException>(() => internet.ToAsciiDomain(new string('a', 64) + ".example"));
        Assert.Throws<ArgumentException>(() => internet.ToUnicodeDomain("xn--.example"));
    }

    [Test]
    public void ConfiguredCollectionAndOutputLimitsApplyToDomainApis()
    {
        FakeOptions countOptions = FakeOptions.Create(7913);
        countOptions.MaximumCount = 1000;
        FakeOptions outputOptions = FakeOptions.Create(7913);
        outputOptions.MaximumOutputSize = 5;

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Fake.Create(countOptions).Internet().DomainSuffixes()).ParamName,
            Is.EqualTo("domainSuffixes"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Fake.Create(outputOptions).Internet().DomainWordFromLength(6)).ParamName,
            Is.EqualTo("length"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Fake.Create(outputOptions).Internet().ToAsciiDomain("example.com")).ParamName,
            Is.EqualTo("domainName"));
    }

    static void AssertValidAsciiDomain(string value)
    {
        Assert.That(value.Length, Is.InRange(1, 253));
        Assert.That(value, Does.Match("^[a-z0-9](?:[a-z0-9.-]*[a-z0-9])?$"));
        foreach (string label in value.Split('.'))
        {
            Assert.That(label.Length, Is.InRange(1, 63));
            Assert.That(label, Does.Match("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$"));
        }
    }
}
