namespace odf.Tests;

public class OdfVersionRulesTests
{
    [TestCase("1.0", OdfVersion.Version10, 0)]
    [TestCase("1.1", OdfVersion.Version11, 1)]
    [TestCase("1.2", OdfVersion.Version12, 2)]
    [TestCase("1.3", OdfVersion.Version13, 3)]
    [TestCase("1.4", OdfVersion.Version14, 4)]
    public void ParseVersionRecognizesSupportedVersions(string lexicalValue, OdfVersion version, int minor)
    {
        OdfVersionInfo result = OdfVersionRules.ParseVersion(lexicalValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.LexicalValue, Is.EqualTo(lexicalValue));
            Assert.That(result.Version, Is.EqualTo(version));
            Assert.That(result.Major, Is.EqualTo(1));
            Assert.That(result.Minor, Is.EqualTo(minor));
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsFuture, Is.False);
            Assert.That(OdfVersionRules.FormatVersion(result), Is.EqualTo(lexicalValue));
        });
    }

    [TestCase("1.5", 1, 5)]
    [TestCase("2.0", 2, 0)]
    [TestCase("12.345", 12, 345)]
    [TestCase("2147483647.2147483647", 2147483647, 2147483647)]
    public void ParseVersionPreservesSyntacticallyValidFutureVersions(string lexicalValue, int major, int minor)
    {
        OdfVersionInfo result = OdfVersionRules.ParseVersion(lexicalValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Version, Is.EqualTo(OdfVersion.Future));
            Assert.That(result.Major, Is.EqualTo(major));
            Assert.That(result.Minor, Is.EqualTo(minor));
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsFuture, Is.True);
            Assert.That(OdfVersionRules.FormatVersion(result), Is.EqualTo(lexicalValue));
            Assert.That(OdfVersionRules.IsAtLeastVersion(result, OdfVersion.Version14), Is.True);
        });
    }

    [Test]
    public void ParseVersionPreservesValidOlderUnknownVersion()
    {
        OdfVersionInfo result = OdfVersionRules.ParseVersion("0.9");

        Assert.Multiple(() =>
        {
            Assert.That(result.Version, Is.EqualTo(OdfVersion.Unknown));
            Assert.That(result.Major, Is.Zero);
            Assert.That(result.Minor, Is.EqualTo(9));
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsFuture, Is.False);
            Assert.That(OdfVersionRules.FormatVersion(result), Is.EqualTo("0.9"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("1")]
    [TestCase("1.")]
    [TestCase(".1")]
    [TestCase("1.2.3")]
    [TestCase("01.2")]
    [TestCase("1.02")]
    [TestCase(" 1.4")]
    [TestCase("1.4 ")]
    [TestCase("v1.4")]
    [TestCase("1.-1")]
    [TestCase("2147483648.0")]
    [TestCase("1.2147483648")]
    public void ParseVersionRejectsInvalidLexicalValues(string? lexicalValue)
    {
        OdfVersionInfo result = OdfVersionRules.ParseVersion(lexicalValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Version, Is.EqualTo(OdfVersion.Unknown));
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsFuture, Is.False);
            Assert.That(result.LexicalValue, Is.EqualTo(lexicalValue ?? ""));
        });
    }

    [Test]
    public void FormatVersionUsesKnownCanonicalValuesAndPreservesUnknownLexicalValues()
    {
        OdfVersionInfo known = new OdfVersionInfo
        {
            Version = OdfVersion.Version13,
            LexicalValue = "preserved-but-not-used"
        };
        OdfVersionInfo synthesized = new OdfVersionInfo
        {
            Version = OdfVersion.Future,
            Major = 3,
            Minor = 7,
            IsValid = true
        };
        OdfVersionInfo invalid = new OdfVersionInfo
        {
            LexicalValue = "invalid"
        };

        Assert.Multiple(() =>
        {
            Assert.That(OdfVersionRules.FormatVersion(known), Is.EqualTo("1.3"));
            Assert.That(OdfVersionRules.FormatVersion(synthesized), Is.EqualTo("3.7"));
            Assert.That(OdfVersionRules.FormatVersion(invalid), Is.EqualTo("invalid"));
            Assert.That(OdfVersionRules.FormatVersion(null), Is.Empty);
        });
    }

    [TestCase("strict", OdfConformanceClass.Strict, "strict")]
    [TestCase("conforming", OdfConformanceClass.Conforming, "conforming")]
    [TestCase("extended", OdfConformanceClass.Extended, "extended")]
    [TestCase("extended-conforming", OdfConformanceClass.Extended, "extended")]
    public void ConformanceClassParsesAndFormatsCanonicalValues(
        string lexicalValue,
        OdfConformanceClass expected,
        string canonical)
    {
        OdfVersionInfo result = new OdfVersionInfo();

        bool success = OdfVersionRules.ApplyConformanceClass(result, lexicalValue);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result.ConformanceLexicalValue, Is.EqualTo(lexicalValue));
            Assert.That(result.ConformanceClass, Is.EqualTo(expected));
            Assert.That(OdfVersionRules.FormatConformanceClass(expected), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("STRICT")]
    [TestCase("extended ")]
    [TestCase("foreign")]
    public void ConformanceClassRejectsUnknownLexicalValues(string? lexicalValue)
    {
        OdfVersionInfo result = new OdfVersionInfo
        {
            ConformanceClass = OdfConformanceClass.Strict
        };

        bool success = OdfVersionRules.ApplyConformanceClass(result, lexicalValue);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result.ConformanceClass, Is.EqualTo(OdfConformanceClass.Unknown));
            Assert.That(result.ConformanceLexicalValue, Is.EqualTo(lexicalValue ?? ""));
        });
    }

    [TestCase("LibreOffice/25.2", "libreoffice-extended")]
    [TestCase("Collabora Office 24.04", "libreoffice-extended")]
    [TestCase("APACHE OPENOFFICE/4.1", "openoffice-extended")]
    [TestCase("StarOffice 9", "openoffice-extended")]
    [TestCase("MicrosoftOffice/16.0", "microsoft-office-extended")]
    [TestCase("Calligra/3.2", "calligra-extended")]
    [TestCase("KOffice/2.3", "calligra-extended")]
    [TestCase("Example Producer", "extended")]
    [TestCase("", "extended")]
    public void ExtendedDocumentsReceiveProducerCompatibilityProfiles(string producer, string expectedProfile)
    {
        OdfVersionInfo result = new OdfVersionInfo
        {
            ConformanceClass = OdfConformanceClass.Extended
        };

        OdfVersionRules.ApplyProducerProfile(result, producer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Producer, Is.EqualTo(producer));
            Assert.That(result.ProducerProfile, Is.EqualTo(expectedProfile));
        });
    }

    [Test]
    public void NonExtendedDocumentsUseStandardProfile()
    {
        OdfVersionInfo result = new OdfVersionInfo
        {
            ConformanceClass = OdfConformanceClass.Strict
        };

        OdfVersionRules.ApplyProducerProfile(result, "LibreOffice/25.2");

        Assert.That(result.ProducerProfile, Is.EqualTo("standard"));
    }

    [Test]
    public void VersionComparisonAndMinimumChecksUseParsedNumbers()
    {
        OdfVersionInfo version12 = OdfVersionRules.ParseVersion("1.2");
        OdfVersionInfo version14 = OdfVersionRules.ParseVersion("1.4");
        OdfVersionInfo version20 = OdfVersionRules.ParseVersion("2.0");

        Assert.Multiple(() =>
        {
            Assert.That(OdfVersionRules.CompareVersions(version12, version14), Is.EqualTo(-1));
            Assert.That(OdfVersionRules.CompareVersions(version14, version12), Is.EqualTo(1));
            Assert.That(OdfVersionRules.CompareVersions(version14, OdfVersionRules.ParseVersion("1.4")), Is.Zero);
            Assert.That(OdfVersionRules.CompareVersions(version20, version14), Is.EqualTo(1));
            Assert.That(OdfVersionRules.CompareVersions(null, version12), Is.EqualTo(-1));
            Assert.That(OdfVersionRules.CompareVersions(version12, null), Is.EqualTo(1));
            Assert.That(OdfVersionRules.CompareVersions(null, null), Is.Zero);
            Assert.That(OdfVersionRules.IsAtLeastVersion(version12, OdfVersion.Version12), Is.True);
            Assert.That(OdfVersionRules.IsAtLeastVersion(version12, OdfVersion.Version13), Is.False);
            Assert.That(OdfVersionRules.IsAtLeastVersion(version20, OdfVersion.Version14), Is.True);
            Assert.That(OdfVersionRules.IsAtLeastVersion(version14, OdfVersion.Future), Is.False);
            Assert.That(OdfVersionRules.IsAtLeastVersion(null, OdfVersion.Version10), Is.False);
        });
    }

    [Test]
    public void VersionInfoDefaultsPreserveNoAccidentalVersionState()
    {
        OdfVersionInfo result = new OdfVersionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(result.LexicalValue, Is.Empty);
            Assert.That(result.Version, Is.EqualTo(OdfVersion.Unknown));
            Assert.That(result.Major, Is.Zero);
            Assert.That(result.Minor, Is.Zero);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsFuture, Is.False);
            Assert.That(result.ConformanceLexicalValue, Is.Empty);
            Assert.That(result.ConformanceClass, Is.EqualTo(OdfConformanceClass.Unknown));
            Assert.That(result.Producer, Is.Empty);
            Assert.That(result.ProducerProfile, Is.Empty);
        });
    }
}
