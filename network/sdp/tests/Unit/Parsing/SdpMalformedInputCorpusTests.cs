using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpMalformedInputCorpusTests
{
    [TestCase("v=\r\n")]
    [TestCase("v=0\r\no=- 1")]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=x\r\nt=0")]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=x\r\nt=0 0\r\nm=audio")]
    public void TruncatedDocuments_AreRejectedByTextAndUtf8EntryPoints(string input)
    {
        ISdpParser parser = new SdpParser();

        SdpParseResult text = parser.ParseText(input, new SdpParseOptions());
        SdpParseResult utf8 = parser.ParseUtf8Bytes(Encoding.UTF8.GetBytes(input), new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(text.Success, Is.False);
            Assert.That(text.Diagnostics, Is.Not.Empty);
            Assert.That(utf8.Success, Is.False);
            Assert.That(utf8.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(text.Diagnostics.Select(diagnostic => diagnostic.Code)));
            Assert.That(utf8.BytesConsumed, Is.EqualTo(Encoding.UTF8.GetByteCount(input)));
        });
    }

    [Test]
    public void InvalidUtf8Corpus_ReportsTheFirstInvalidByteWithoutThrowing()
    {
        byte[][] corpus =
        {
            new byte[] { 255 },
            new byte[] { 195 },
            new byte[] { 226, 40, 161 },
            new byte[] { 118, 61, 48, 13, 10, 240, 159, 146 }
        };
        int[] invalidOffsets = { 0, 0, 0, 5 };
        ISdpParser parser = new SdpFragmentParser();

        for (int index = 0; index < corpus.Length; index++)
        {
            SdpParseResult result = parser.ParseUtf8Bytes(corpus[index], new SdpParseOptions());

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidUtf8));
                Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
                Assert.That(result.Diagnostics[0].Location.ByteOffset, Is.EqualTo(invalidOffsets[index]));
                Assert.That(result.BytesConsumed, Is.EqualTo(invalidOffsets[index]));
            });
        }
    }

    [Test]
    public void ControlCharacterCorpus_IsRejectedAtTheExactCharacter()
    {
        char[] corpus = { '\0', '\t', '\u0001', '\u001f', '\u007f' };
        ISdpParser parser = new SdpFragmentParser();

        foreach (char control in corpus)
        {
            string input = "v=0\r\na=x" + control + "tail";
            SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(control == '\0' ? SdpDiagnosticCodes.ParseNulCharacterNotAllowed : SdpDiagnosticCodes.ParseControlCharacterNotAllowed));
                Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(8));
                Assert.That(result.CharactersConsumed, Is.EqualTo(8));
            });
        }
    }

    [Test]
    public void ExtremeDecimalText_ProducesBoundedOverflowDiagnostics()
    {
        string decimalText = new string('9', 4096);
        string input =
            "v=" + decimalText + "\r\n" +
            "o=- " + decimalText + " " + decimalText + " IN IP4 host\r\n" +
            "s=x\r\n" +
            "t=" + decimalText + " " + decimalText;
        SdpParseOptions options = new SdpParseOptions
        {
            MaximumLineLength = 16384,
            MaximumTokenLength = 4096,
            MaximumDiagnosticCount = 16
        };
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(result.Diagnostics, Has.Count.LessThanOrEqualTo(options.MaximumDiagnosticCount));
            Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == SdpDiagnosticCodes.ParseNumberOverflow), Is.True);
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Detail.Length < 512), Is.True);
            Assert.That(result.Diagnostics.All(diagnostic => !diagnostic.Detail.Contains(decimalText, StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void DuplicateStorm_ExhaustsOnlyTheConfiguredDiagnosticCapacity()
    {
        StringBuilder input = new StringBuilder("v=0\r\n");
        for (int index = 0; index < 256; index++)
        {
            input.Append("v=0\r\n");
        }
        input.Append("o=- 1 1 IN IP4 host\r\ns=first\r\nt=0 0\r\n");
        SdpParseOptions options = new SdpParseOptions { MaximumDiagnosticCount = 8 };

        SdpParseResult result = new SdpParser().ParseText(input.ToString(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.DiagnosticLimitReached, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(options.MaximumDiagnosticCount));
            Assert.That(result.Diagnostics.Take(options.MaximumDiagnosticCount - 1).All(diagnostic => diagnostic.Code == SdpDiagnosticCodes.ParseDuplicateField), Is.True);
            Assert.That(result.Diagnostics[^1].Code, Is.EqualTo(SdpDiagnosticCodes.DiagnosticLimitReached));
            Assert.That(result.Diagnostics[^1].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
        });
    }

    [Test]
    public void DeepAttributeList_AcceptsTheBoundaryAndRejectsLimitExhaustion()
    {
        const int attributeCount = 1024;
        StringBuilder input = new StringBuilder();
        for (int index = 0; index < attributeCount; index++)
        {
            input.Append("a=x-");
            input.Append(index);
            input.Append(":value\r\n");
        }
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult accepted = parser.ParseText(input.ToString(), new SdpParseOptions { MaximumAttributesPerScope = attributeCount });
        SdpParseResult exhausted = parser.ParseText(input.ToString(), new SdpParseOptions { MaximumAttributesPerScope = 32 });

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.Diagnostics, Is.Empty);
            Assert.That(accepted.Description?.Attributes, Has.Count.EqualTo(attributeCount));
            Assert.That(accepted.Description?.Attributes[^1].Name, Is.EqualTo("x-1023"));
            Assert.That(exhausted.Success, Is.False);
            Assert.That(exhausted.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(exhausted.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseAttributeCountLimitExceeded));
            Assert.That(exhausted.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(exhausted.Description?.Attributes, Has.Count.EqualTo(32));
        });
    }

    [Test]
    public void RidReferenceCycle_IsRejectedAfterSuccessfulParsing()
    {
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=cycle\r\n" +
            "t=0 0\r\n" +
            "m=video 9 RTP/AVP 96\r\n" +
            "a=rtpmap:96 VP8/90000\r\n" +
            "a=rid:a send depend=b\r\n" +
            "a=rid:b send depend=c\r\n" +
            "a=rid:c send depend=a";
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());

        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Description, Is.Not.Null);
        SdpValidationResult validation = new SdpValidator().ValidateDescription(parsed.Description!, new SdpValidationOptions { Profile = SdpValidationProfile.Rtp });

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.False);
            Assert.That(validation.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationRid && issue.Detail.Contains("cycle", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void SecretBearingMalformedInput_NeverLeaksValuesIntoDiagnostics()
    {
        string password = "password-value-that-must-not-leak";
        string fingerprint = "PRIVATE-FINGERPRINT-VALUE";
        string keyMaterial = "private-inline-key-material";
        string legacyKey = "legacy-encryption-secret";
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=redaction\r\n" +
            "t=0 0\r\n" +
            "k=clear:" + legacyKey + "\r\n" +
            "m=audio 9 RTP/AVP 96\r\n" +
            "a=rtpmap:96 opus/48000/2\r\n" +
            "a=ice-pwd:" + password + "\r\n" +
            "a=fingerprint:sha-256 " + fingerprint + "\r\n" +
            "a=crypto:invalid inline:" + keyMaterial;
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions());

        Assert.That(parsed.Description, Is.Not.Null);
        SdpValidationResult validation = new SdpValidator().ValidateDescription(parsed.Description!, new SdpValidationOptions { Profile = SdpValidationProfile.CombinedStrict });
        IEnumerable<string> details = parsed.Diagnostics.Select(diagnostic => diagnostic.Detail).Concat(validation.Issues.Select(issue => issue.Detail));

        Assert.Multiple(() =>
        {
            Assert.That(validation.Issues, Is.Not.Empty);
            Assert.That(details.All(detail => !detail.Contains(password, StringComparison.Ordinal)), Is.True);
            Assert.That(details.All(detail => !detail.Contains(fingerprint, StringComparison.Ordinal)), Is.True);
            Assert.That(details.All(detail => !detail.Contains(keyMaterial, StringComparison.Ordinal)), Is.True);
            Assert.That(details.All(detail => !detail.Contains(legacyKey, StringComparison.Ordinal)), Is.True);
        });
    }
}
