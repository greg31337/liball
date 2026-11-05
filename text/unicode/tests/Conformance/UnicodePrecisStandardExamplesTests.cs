using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePrecisStandardExamplesTests
{
    [Test]
    public void Rfc8265_UsernameExamplesMatchIdentifierProfiles()
    {
        UnicodePrecis precis = new UnicodePrecis();
        UnicodePrecisProfile mapped = new UnicodeProfiles().CreateUsernameCaseMapped();
        string[] legal = [
            "juliet@example.com",
            "fussball",
            "fu\u00DFball",
            "\u03C0",
            "\u03A3",
            "\u03C3",
            "\u03C2",
        ];

        int index = 0;
        while (index < legal.Length)
        {
            string prepared = precis.Prepare(legal[index], mapped);
            Assert.That(precis.Validate(prepared, mapped).IsValid, Is.True, legal[index]);
            index = index + 1;
        }

        Assert.That(precis.EqualsPrepared("fussball", "fu\u00DFball", mapped), Is.False);
        Assert.That(precis.EqualsPrepared("\u03A3", "\u03C3", mapped), Is.True);
        Assert.That(precis.EqualsPrepared("\u03C3", "\u03C2", mapped), Is.False);

        Assert.That(precis.Validate("foo bar", mapped).IsValid, Is.False);
        Assert.That(precis.Validate("", mapped).Status, Is.EqualTo(UnicodePrecisStatus.Empty));
        Assert.That(precis.Validate("henry\u2163", mapped).Status, Is.EqualTo(UnicodePrecisStatus.DisallowedCodePoint));
        Assert.That(precis.Validate("\u221E", mapped).Status, Is.EqualTo(UnicodePrecisStatus.DisallowedCodePoint));
    }

    [Test]
    public void Rfc8265_OpaqueStringExamplesPreservePasswordDistinctions()
    {
        UnicodePrecis precis = new UnicodePrecis();
        UnicodePrecisProfile opaque = new UnicodeProfiles().CreateOpaqueString();
        string[] legal = [
            "correct horse battery staple",
            "Correct Horse Battery Staple",
            "\u03C0\u00DF\u00E5",
            "Jack of \u2666s",
        ];

        int index = 0;
        while (index < legal.Length)
        {
            Assert.That(precis.Validate(legal[index], opaque).IsValid, Is.True, legal[index]);
            index = index + 1;
        }

        Assert.That(precis.PrepareOpaqueString("foo\u1680bar"), Is.EqualTo("foo bar"));
        Assert.That(precis.EqualsPrepared(legal[0], legal[1], opaque), Is.False);
        Assert.That(precis.Validate("", opaque).Status, Is.EqualTo(UnicodePrecisStatus.Empty));
        Assert.That(precis.Validate("my cat is a \tby", opaque).Status, Is.EqualTo(UnicodePrecisStatus.DisallowedControl));
    }

    [Test]
    public void Rfc8266_NicknameComparisonExamplesMatchPublishedKeys()
    {
        UnicodePrecis precis = new UnicodePrecis();
        UnicodePrecisProfile nickname = new UnicodeProfiles().CreateNickname();
        string[] inputs = ["Foo", "foo", "Foo Bar", "foo bar", "\u03A3", "\u03C3", "\u03C2", "\u03D4", "\u221E", "Richard \u2163"];
        string[] expected = ["foo", "foo", "foo bar", "foo bar", "\u03C3", "\u03C3", "\u03C2", "\u03CB", "\u221E", "richard iv"];

        int index = 0;
        while (index < inputs.Length)
        {
            Assert.That(precis.GetCompareKey(inputs[index], nickname), Is.EqualTo(expected[index]), inputs[index]);
            index = index + 1;
        }

        Assert.That(precis.PrepareNickname("  Foo\u00A0\u2003Bar  "), Is.EqualTo("Foo Bar"));
        Assert.That(precis.PrepareNickname("\u03D4"), Is.EqualTo("\u03AB"));
    }

    [Test]
    public void Rfc8264_ContextualAndBidiRulesApplyAfterMapping()
    {
        UnicodePrecis precis = new UnicodePrecis();
        UnicodePrecisProfile username = new UnicodeProfiles().CreateUsernameCaseMapped();
        UnicodePrecisProfile opaque = new UnicodeProfiles().CreateOpaqueString();

        Assert.That(precis.Validate("\u0915\u094D\u200D\u0937", username).IsValid, Is.True);
        Assert.That(precis.Validate("\u0628\u200C\u0628", username).IsValid, Is.True);
        Assert.That(precis.Validate("\u0915\u094D\u200D\u0937", opaque).IsValid, Is.True);
        Assert.That(precis.Validate("a\u200Da", username).Status, Is.EqualTo(UnicodePrecisStatus.ContextualRule));
        Assert.That(precis.Validate("\u05D0\u05D11", username).IsValid, Is.True);
        Assert.That(precis.Validate("\u05D0a\u05D1", username).Status, Is.EqualTo(UnicodePrecisStatus.BidiRule));
    }

    [Test]
    public void Uts46_NontransitionalExamplesMapAndValidateAtUnicodeLayer()
    {
        UnicodeIdna idna = new UnicodeIdna();

        Assert.That(idna.PrepareLabel("blo\u00DF"), Is.EqualTo("blo\u00DF"));
        Assert.That(idna.PrepareLabel("BLO\u1E9E"), Is.EqualTo("blo\u00DF"));
        Assert.That(idna.PrepareLabel("u\u0308"), Is.EqualTo("\u00FC"));
        Assert.That(idna.PrepareLabel("\u65E5\u672C\u8A9E\u3002\uFF2A\uFF30"), Is.EqualTo("\u65E5\u672C\u8A9E.jp"));
        Assert.That(idna.IsLabelValid("\u2615"), Is.True);

        UnicodeIdnaValidationResult dottedMapping = idna.ValidateLabel("a\u2488com");
        Assert.That(dottedMapping.PreparedLabel, Is.EqualTo("a1.com"));
        Assert.That(dottedMapping.Status, Is.EqualTo(UnicodeIdnaStatus.LabelSeparator));
    }

    [Test]
    public void Uts46_DeviationsJoinersAndBidiRulesRemainDistinct()
    {
        UnicodeIdna idna = new UnicodeIdna();

        Assert.That(idna.PrepareLabel("\u00DF\u03C2\u200C\u200D"), Is.EqualTo("\u00DF\u03C2\u200C\u200D"));
        Assert.That(idna.IsLabelValid("\u0915\u094D\u200D\u0937"), Is.True);
        Assert.That(idna.IsLabelValid("\u0628\u200C\u0628"), Is.True);
        Assert.That(idna.ValidateLabel("a\u200Ca").Status, Is.EqualTo(UnicodeIdnaStatus.ContextualRule));

        Assert.That(idna.IsLabelValid("\u05D0\u05D11"), Is.True);
        Assert.That(idna.ValidateLabel("\u05D0a\u05D1").Status, Is.EqualTo(UnicodeIdnaStatus.BidiRule));
        Assert.That(idna.ValidateLabel("\u0627\u0661" + "1").Status, Is.EqualTo(UnicodeIdnaStatus.BidiRule));
        Assert.That(idna.ValidateLabel("\u0627\u2615").Status, Is.EqualTo(UnicodeIdnaStatus.BidiRule));
        Assert.That(idna.IsLabelValid("1abc"), Is.True);
    }

    [Test]
    public void PreparationIsIdempotentAndMalformedScalarsStayInvalid()
    {
        UnicodePrecis precis = new UnicodePrecis();
        UnicodePrecisProfile[] profiles = [
            new UnicodeProfiles().CreateUsernameCaseMapped(),
            new UnicodeProfiles().CreateUsernameCasePreserved(),
            new UnicodeProfiles().CreateNickname(),
            new UnicodeProfiles().CreateOpaqueString(),
        ];
        string[] samples = ["\uFF21\u030A", "  \u03D4\u00A0\u2003X  ", "A\u00DF", "\u0915\u094D\u200D\u0937"];

        int profileIndex = 0;
        while (profileIndex < profiles.Length)
        {
            int sampleIndex = 0;
            while (sampleIndex < samples.Length)
            {
                string once = precis.Prepare(samples[sampleIndex], profiles[profileIndex]);
                string twice = precis.Prepare(once, profiles[profileIndex]);
                Assert.That(twice, Is.EqualTo(once));
                sampleIndex = sampleIndex + 1;
            }

            Assert.That(precis.Validate("\uD800", profiles[profileIndex]).Status, Is.EqualTo(UnicodePrecisStatus.InvalidScalar));
            profileIndex = profileIndex + 1;
        }

        UnicodeIdna idna = new UnicodeIdna();
        string[] labels = ["BLO\u1E9E", "u\u0308", "\uFF21\uFF22\uFF23", "a\u00ADb", "\u2615"];
        int labelIndex = 0;
        while (labelIndex < labels.Length)
        {
            string once = idna.PrepareLabel(labels[labelIndex]);
            Assert.That(idna.PrepareLabel(once), Is.EqualTo(once));
            labelIndex = labelIndex + 1;
        }

        Assert.That(idna.ValidateLabel("").Status, Is.EqualTo(UnicodeIdnaStatus.Empty));
        Assert.That(idna.ValidateLabel("\uD800").Status, Is.EqualTo(UnicodeIdnaStatus.InvalidScalar));
    }
}
