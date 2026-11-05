using System.Text;
using email;

namespace email.Tests;

public class EmailRfc2231GeneratorTests
{
    [Test]
    public void ShortValueUsesOneExtendedParameterAndRoundTrips()
    {
        byte[] input = Encoding.UTF8.GetBytes("café.txt");
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("utf-8");
        options.SetLanguage("fr");

        EmailRfc2231EncodeResult encoded = new EmailRfc2231().Encode("filename", input, options);
        EmailRfc2231DecodeResult decoded = new EmailRfc2231().DecodeWithParametersAndMode(encoded.GetParameters(), EmailRfc2231DecodeMode.Strict);

        Assert.That(Encoding.ASCII.GetString(encoded.GetBytes()), Is.EqualTo("; filename*=utf-8'fr'caf%C3%A9.txt"));
        Assert.That(encoded.GetParameterCount(), Is.EqualTo(1));
        Assert.That(encoded.GetParameter(0).GetName(), Is.EqualTo("filename*"));
        Assert.That(encoded.GetParameter(0).WasQuoted(), Is.False);
        Assert.That(encoded.UsesContinuations(), Is.False);
        Assert.That(decoded.GetParameter(0).GetDecodedBytes(), Is.EqualTo(input));
        Assert.That(decoded.GetParameter(0).GetCharset(), Is.EqualTo("utf-8"));
        Assert.That(decoded.GetParameter(0).GetLanguage(), Is.EqualTo("fr"));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void EmptyCharsetLanguageAndValueStillProduceRequiredDelimiters()
    {
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("");

        EmailRfc2231EncodeResult encoded = new EmailRfc2231().Encode("name", Array.Empty<byte>(), options);
        EmailRfc2231DecodeResult decoded = new EmailRfc2231().DecodeWithParametersAndMode(encoded.GetParameters(), EmailRfc2231DecodeMode.Strict);

        Assert.That(Encoding.ASCII.GetString(encoded.GetBytes()), Is.EqualTo("; name*=''"));
        Assert.That(decoded.GetParameter(0).GetDecodedBytes(), Is.Empty);
        Assert.That(decoded.GetParameter(0).HasCharset(), Is.True);
        Assert.That(decoded.GetParameter(0).GetCharset(), Is.Empty);
        Assert.That(decoded.GetParameter(0).HasLanguage(), Is.True);
        Assert.That(decoded.GetParameter(0).GetLanguage(), Is.Empty);
    }

    [Test]
    public void LongValuesUseContiguousEncodedSectionsAndStrictlyRoundTrip()
    {
        byte[] input = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("résumé-", 20)));
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("utf-8");
        options.SetLanguage("en");
        options.SetFirstLineColumn(30);
        options.SetMaximumLineLength(40);

        EmailRfc2231EncodeResult encoded = new EmailRfc2231().Encode("filename", input, options);
        EmailRfc2231DecodeResult decoded = new EmailRfc2231().DecodeWithParametersAndMode(encoded.GetParameters(), EmailRfc2231DecodeMode.Strict);

        Assert.That(encoded.UsesContinuations(), Is.True);
        int index = 0;
        while (index < encoded.GetParameterCount())
        {
            Assert.That(encoded.GetParameter(index).GetName(), Is.EqualTo("filename*" + index + "*"));
            Assert.That(Encoding.ASCII.GetString(encoded.GetParameter(index).GetValueBytes()), Does.Not.EndWith("%"));
            Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
            index++;
        }

        Assert.That(decoded.GetParameter(0).GetDecodedBytes(), Is.EqualTo(input));
        Assert.That(decoded.GetParameter(0).GetSegmentCount(), Is.EqualTo(encoded.GetParameterCount()));
        AssertLines(encoded.GetBytes(), options.GetFirstLineColumn(), options.GetMaximumLineLength());
    }

    [Test]
    public void FoldingUsesOnlyCrlfSpaceAtParameterBoundaries()
    {
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("utf-8");
        options.SetFirstLineColumn(35);
        options.SetMaximumLineLength(40);

        EmailRfc2231EncodeResult result = new EmailRfc2231().Encode(
            "filename",
            Encoding.ASCII.GetBytes(new string('a', 80)),
            options);
        string value = Encoding.ASCII.GetString(result.GetBytes());

        Assert.That(value, Does.StartWith(";\r\n "));
        Assert.That(value, Does.Not.EndWith("\r\n"));
        Assert.That(result.GetFoldCount(), Is.GreaterThan(0));
        AssertLines(result.GetBytes(), options.GetFirstLineColumn(), options.GetMaximumLineLength());
    }

    [Test]
    public void FullFirstLineFoldsBeforeTheParameterDelimiter()
    {
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("utf-8");
        options.SetFirstLineColumn(40);
        options.SetMaximumLineLength(40);

        EmailRfc2231EncodeResult result = new EmailRfc2231().Encode("name", Encoding.ASCII.GetBytes("value"), options);

        Assert.That(Encoding.ASCII.GetString(result.GetBytes()), Does.StartWith("\r\n ;name*="));
        Assert.That(result.GetFoldCount(), Is.EqualTo(1));
        AssertLines(result.GetBytes(), options.GetFirstLineColumn(), options.GetMaximumLineLength());
    }

    [Test]
    public void RawUtf8ContinuationBoundariesNeverSplitAValidSequence()
    {
        string text = "😀😀😀😀😀😀";
        byte[] input = Encoding.UTF8.GetBytes(text);
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("UTF-8");
        options.SetMaximumLineLength(36);

        EmailRfc2231EncodeResult encoded = new EmailRfc2231().Encode("filename", input, options);
        EmailRfc2231DecodeResult decoded = new EmailRfc2231().DecodeWithParametersAndMode(encoded.GetParameters(), EmailRfc2231DecodeMode.Strict);
        UTF8Encoding strictUtf8 = new UTF8Encoding(false, true);

        Assert.That(encoded.GetParameterCount(), Is.EqualTo(6));
        int index = 0;
        while (index < encoded.GetParameterCount())
        {
            List<EmailRfc2231Parameter> one = new List<EmailRfc2231Parameter>();
            if (index == 0)
            {
                one.Add(encoded.GetParameter(index));
                EmailRfc2231DecodeResult first = new EmailRfc2231().DecodeWithParametersAndMode(one, EmailRfc2231DecodeMode.Strict);
                Assert.That(strictUtf8.GetString(first.GetParameter(0).GetDecodedBytes()), Is.EqualTo("😀"));
            }
            else
            {
                EmailPercentDecodeResult part = new EmailPercentEncoding().DecodeWithInputAndModeAndMaximumDecodedBytes(encoded.GetParameter(index).GetValueBytes(), EmailRfc2231DecodeMode.Strict, 100);
                Assert.That(strictUtf8.GetString(part.GetDecodedBytes()), Is.EqualTo("😀"));
            }

            index++;
        }

        Assert.That(strictUtf8.GetString(decoded.GetParameter(0).GetDecodedBytes()), Is.EqualTo(text));
    }

    [Test]
    public void TextGenerationKeepsSupplementaryCharactersIntegral()
    {
        string text = "😀😀😀😀";
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("utf-8");
        options.SetMaximumLineLength(36);

        EmailRfc2231EncodeResult encoded = new EmailRfc2231().EncodeText("filename", text, new UTF8Encoding(false, true), options);
        EmailRfc2231DecodeResult decoded = new EmailRfc2231().DecodeWithParametersAndMode(encoded.GetParameters(), EmailRfc2231DecodeMode.Strict);

        Assert.That(new UTF8Encoding(false, true).GetString(decoded.GetParameter(0).GetDecodedBytes()), Is.EqualTo(text));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void FirstContinuationMayContainOnlyMetadataToKeepACharacterIntegral()
    {
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("utf-8");
        options.SetMaximumLineLength(30);

        EmailRfc2231EncodeResult encoded = new EmailRfc2231().EncodeText("filename", "😀", new UTF8Encoding(false, true), options);
        EmailRfc2231DecodeResult decoded = new EmailRfc2231().DecodeWithParametersAndMode(encoded.GetParameters(), EmailRfc2231DecodeMode.Strict);

        Assert.That(encoded.GetParameterCount(), Is.EqualTo(2));
        Assert.That(Encoding.ASCII.GetString(encoded.GetParameter(0).GetValueBytes()), Is.EqualTo("utf-8''"));
        Assert.That(new UTF8Encoding(false, true).GetString(decoded.GetParameter(0).GetDecodedBytes()), Is.EqualTo("😀"));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void TextGenerationUsesTheCallerEncodingAndDeclaredCharset()
    {
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("iso-8859-1");

        EmailRfc2231EncodeResult encoded = new EmailRfc2231().EncodeText("title", "café", Encoding.Latin1, options);
        EmailRfc2231DecodeResult decoded = new EmailRfc2231().DecodeWithParametersAndMode(encoded.GetParameters(), EmailRfc2231DecodeMode.Strict);

        Assert.That(Encoding.ASCII.GetString(encoded.GetBytes()), Is.EqualTo("; title*=iso-8859-1''caf%E9"));
        Assert.That(decoded.GetParameter(0).GetDecodedBytes(), Is.EqualTo(Encoding.Latin1.GetBytes("café")));
    }

    [Test]
    public void SectionNumbersRemainDecimalWithoutLeadingZerosAcrossTen()
    {
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("");
        options.SetMaximumLineLength(20);

        EmailRfc2231EncodeResult encoded = new EmailRfc2231().Encode("x", Encoding.ASCII.GetBytes(new string('a', 150)), options);
        EmailRfc2231DecodeResult decoded = new EmailRfc2231().DecodeWithParametersAndMode(encoded.GetParameters(), EmailRfc2231DecodeMode.Strict);

        Assert.That(encoded.GetParameterCount(), Is.GreaterThan(10));
        Assert.That(encoded.GetParameter(0).GetName(), Is.EqualTo("x*0*"));
        Assert.That(encoded.GetParameter(10).GetName(), Is.EqualTo("x*10*"));
        Assert.That(decoded.GetParameter(0).GetDecodedBytes(), Is.EqualTo(Encoding.ASCII.GetBytes(new string('a', 150))));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void GenerationIsDeterministic()
    {
        byte[] input = Encoding.UTF8.GetBytes("quarterly résumé report " + new string('x', 80));
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("utf-8");
        options.SetLanguage("en-US");
        options.SetFirstLineColumn(22);
        options.SetMaximumLineLength(50);

        EmailRfc2231EncodeResult first = new EmailRfc2231().Encode("filename", input, options);
        EmailRfc2231EncodeResult second = new EmailRfc2231().Encode("filename", input, options);

        Assert.That(second.GetBytes(), Is.EqualTo(first.GetBytes()));
        Assert.That(second.GetParameterCount(), Is.EqualTo(first.GetParameterCount()));
        int index = 0;
        while (index < first.GetParameterCount())
        {
            Assert.That(second.GetParameter(index).GetName(), Is.EqualTo(first.GetParameter(index).GetName()));
            Assert.That(second.GetParameter(index).GetValueBytes(), Is.EqualTo(first.GetParameter(index).GetValueBytes()));
            index++;
        }
    }

    [Test]
    public void SegmentLimitStopsGenerationBeforeReturningPartialOutput()
    {
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("utf-8");
        options.SetMaximumLineLength(30);
        options.SetMaximumSegments(1);

        EmailLimitException exception = Assert.Throws<EmailLimitException>(() => new EmailRfc2231().Encode("filename", Encoding.ASCII.GetBytes(new string('x', 100)), options));

        Assert.That(exception.GetLimitName(), Is.EqualTo("RFC 2231 segments"));
        Assert.That(exception.GetLimit(), Is.EqualTo(1));
        Assert.That(exception.GetObserved(), Is.EqualTo(2));
    }

    [Test]
    public void TooSmallLineLimitRejectsMetadataOrOneCompleteCharacter()
    {
        EmailRfc2231GenerationOptions metadata = new EmailRfc2231GenerationOptions("utf-8");
        metadata.SetMaximumLineLength(10);
        Assert.Throws<ArgumentException>(() => new EmailRfc2231().Encode("filename", Array.Empty<byte>(), metadata));

        EmailRfc2231GenerationOptions character = new EmailRfc2231GenerationOptions("utf-8");
        character.SetMaximumLineLength(25);
        Assert.Throws<ArgumentException>(() => new EmailRfc2231().EncodeText("filename", "😀", new UTF8Encoding(false, true), character));
    }

    [Test]
    public void ArgumentsAndOptionsAreValidated()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailRfc2231GenerationOptions(null!));
        Assert.Throws<ArgumentException>(() => new EmailRfc2231GenerationOptions("bad charset"));
        Assert.Throws<ArgumentException>(() => new EmailRfc2231GenerationOptions("bad;charset"));

        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("utf-8");
        Assert.Throws<ArgumentNullException>(() => options.SetCharset(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetLanguage(null!));
        Assert.Throws<ArgumentException>(() => options.SetLanguage("en_US"));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetFirstLineColumn(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumLineLength(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumLineLength(999));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumSegments(0));
        Assert.Throws<ArgumentNullException>(() => new EmailRfc2231().Encode(null!, Array.Empty<byte>(), options));
        Assert.Throws<ArgumentException>(() => new EmailRfc2231().Encode("bad name", Array.Empty<byte>(), options));
        Assert.Throws<ArgumentNullException>(() => new EmailRfc2231().Encode("name", Array.Empty<byte>(), null!));
        Assert.Throws<ArgumentNullException>(() => new EmailRfc2231().EncodeText("name", null!, Encoding.UTF8, options));
        Assert.Throws<ArgumentNullException>(() => new EmailRfc2231().EncodeText("name", "value", null!, options));
    }

    [Test]
    public void OptionsAndResultSnapshotsAreIndependent()
    {
        EmailRfc2231GenerationOptions options = new EmailRfc2231GenerationOptions("utf-8");
        options.SetLanguage("en");
        options.SetFirstLineColumn(5);
        options.SetMaximumLineLength(60);
        options.SetMaximumSegments(3);
        EmailRfc2231GenerationOptions copy = options.Clone();
        options.SetCharset("us-ascii");
        options.SetLanguage("");

        EmailRfc2231EncodeResult result = new EmailRfc2231().Encode("name", Encoding.ASCII.GetBytes("value"), copy);
        byte[] returned = result.GetBytes();
        returned[0] = 0;
        result.GetParameters().Clear();

        Assert.That(copy.GetCharset(), Is.EqualTo("utf-8"));
        Assert.That(copy.GetLanguage(), Is.EqualTo("en"));
        Assert.That(copy.GetFirstLineColumn(), Is.EqualTo(5));
        Assert.That(copy.GetMaximumLineLength(), Is.EqualTo(60));
        Assert.That(copy.GetMaximumSegments(), Is.EqualTo(3));
        Assert.That(Encoding.ASCII.GetString(result.GetBytes()), Is.EqualTo("; name*=utf-8'en'value"));
        Assert.That(result.GetByteCount(), Is.EqualTo(result.GetBytes().Length));
        Assert.That(result.GetParameterCount(), Is.EqualTo(1));
        Assert.That(result.GetFinalLineColumn(), Is.EqualTo(27));
    }

    static void AssertLines(byte[] bytes, int firstLineColumn, int maximumLineLength)
    {
        string[] lines = Encoding.ASCII.GetString(bytes).Split("\r\n");
        Assert.That(firstLineColumn + lines[0].Length, Is.LessThanOrEqualTo(maximumLineLength));
        int index = 1;
        while (index < lines.Length)
        {
            Assert.That(lines[index], Does.StartWith(" "));
            Assert.That(lines[index].Length, Is.LessThanOrEqualTo(maximumLineLength));
            index++;
        }
    }
}
