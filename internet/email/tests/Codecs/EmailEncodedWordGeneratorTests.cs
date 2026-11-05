using System.Text;
using email;

namespace email.Tests;

public class EmailEncodedWordGeneratorTests
{
    [Test]
    public void QGenerationProducesCanonicalKnownVectorAndRoundTrips()
    {
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("iso-8859-1");
        options.SetEncodingChoice(EmailEncodedWordEncodingChoice.QuotedPrintable);

        EmailEncodedWordEncodeResult encoded = new EmailEncodedWord().Encode(
            new byte[] { 65, 110, 100, 114, 233 },
            options);
        EmailEncodedWordParseResult decoded = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            encoded.GetBytes(),
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(Encoding.ASCII.GetString(encoded.GetBytes()), Is.EqualTo("=?iso-8859-1?Q?Andr=E9?="));
        Assert.That(encoded.GetEncoding(), Is.EqualTo(EmailEncodedWordEncoding.QuotedPrintable));
        Assert.That(encoded.GetEncodedWordCount(), Is.EqualTo(1));
        Assert.That(encoded.GetFoldCount(), Is.Zero);
        Assert.That(decoded.GetCombinedBytes(), Is.EqualTo(new byte[] { 65, 110, 100, 114, 233 }));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void Base64GenerationProducesCanonicalKnownVector()
    {
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("utf-8");
        options.SetEncodingChoice(EmailEncodedWordEncodingChoice.Base64);

        EmailEncodedWordEncodeResult result = new EmailEncodedWord().Encode(
            Encoding.UTF8.GetBytes("Hello"),
            options);

        Assert.That(Encoding.ASCII.GetString(result.GetBytes()), Is.EqualTo("=?utf-8?B?SGVsbG8=?="));
        Assert.That(result.GetEncoding(), Is.EqualTo(EmailEncodedWordEncoding.Base64));
        Assert.That(result.GetFinalLineColumn(), Is.EqualTo(20));
    }

    [Test]
    public void AutomaticChoiceUsesQForMostlyReadableTextAndBForDenseBinaryData()
    {
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("x-test");

        EmailEncodedWordEncodeResult readable = new EmailEncodedWord().Encode(
            Encoding.ASCII.GetBytes("mostly readable words"),
            options);
        EmailEncodedWordEncodeResult binary = new EmailEncodedWord().Encode(
            new byte[] { 0, 1, 2, 3, 128, 129, 130, 250, 251, 252, 253, 254 },
            options);

        Assert.That(readable.GetEncoding(), Is.EqualTo(EmailEncodedWordEncoding.QuotedPrintable));
        Assert.That(binary.GetEncoding(), Is.EqualTo(EmailEncodedWordEncoding.Base64));
    }

    [Test]
    public void QGenerationEscapesBytesAccordingToPhraseAndCommentContexts()
    {
        EmailEncodedWordGenerationOptions phraseOptions = new EmailEncodedWordGenerationOptions("us-ascii");
        phraseOptions.SetEncodingChoice(EmailEncodedWordEncodingChoice.QuotedPrintable);
        phraseOptions.SetContext(EmailEncodedWordContext.Phrase);
        EmailEncodedWordGenerationOptions commentOptions = new EmailEncodedWordGenerationOptions("us-ascii");
        commentOptions.SetEncodingChoice(EmailEncodedWordEncodingChoice.QuotedPrintable);
        commentOptions.SetContext(EmailEncodedWordContext.Comment);

        EmailEncodedWordEncodeResult phrase = new EmailEncodedWord().Encode(Encoding.ASCII.GetBytes("a.b c"), phraseOptions);
        EmailEncodedWordEncodeResult comment = new EmailEncodedWord().Encode(Encoding.ASCII.GetBytes("(a)\\b"), commentOptions);
        EmailEncodedWordParseResult parsedPhrase = new EmailEncodedWord().ParseWithInputAndModeAndContext(phrase.GetBytes(), EmailEncodedWordParseMode.Strict, EmailEncodedWordContext.Phrase);
        EmailEncodedWordParseResult parsedComment = new EmailEncodedWord().ParseWithInputAndModeAndContext(comment.GetBytes(), EmailEncodedWordParseMode.Strict, EmailEncodedWordContext.Comment);

        Assert.That(Encoding.ASCII.GetString(phrase.GetBytes()), Is.EqualTo("=?us-ascii?Q?a=2Eb_c?="));
        Assert.That(Encoding.ASCII.GetString(comment.GetBytes()), Is.EqualTo("=?us-ascii?Q?=28a=29=5Cb?="));
        Assert.That(parsedPhrase.GetIssues().GetCount(), Is.Zero);
        Assert.That(parsedComment.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void LongValuesAreSegmentedIntoWordsNoLongerThanSeventyFiveBytes()
    {
        byte[] input = Encoding.ASCII.GetBytes(new string('a', 200));
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("utf-8");
        options.SetEncodingChoice(EmailEncodedWordEncodingChoice.QuotedPrintable);

        EmailEncodedWordEncodeResult encoded = new EmailEncodedWord().Encode(input, options);
        EmailEncodedWordParseResult decoded = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            encoded.GetBytes(),
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(encoded.GetEncodedWordCount(), Is.GreaterThan(1));
        Assert.That(decoded.GetPartCount(), Is.EqualTo(encoded.GetEncodedWordCount()));
        int index = 0;
        while (index < decoded.GetPartCount())
        {
            Assert.That(decoded.GetPart(index).GetSourceByteCount(), Is.LessThanOrEqualTo(EmailEncodedWordConstants.MaximumWordBytes));
            index++;
        }

        Assert.That(decoded.GetCombinedBytes(), Is.EqualTo(input));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void FoldingUsesCrlfSpaceAndKeepsEveryEncodedWordLineWithinTheLimit()
    {
        byte[] input = Encoding.ASCII.GetBytes(new string('x', 180));
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("utf-8");
        options.SetEncodingChoice(EmailEncodedWordEncodingChoice.QuotedPrintable);
        options.SetFirstLineColumn(9);

        EmailEncodedWordEncodeResult encoded = new EmailEncodedWord().Encode(input, options);
        string value = Encoding.ASCII.GetString(encoded.GetBytes());
        string[] lines = value.Split("\r\n");

        Assert.That(value, Does.StartWith("\r\n "));
        Assert.That(encoded.GetFoldCount(), Is.EqualTo(encoded.GetEncodedWordCount()));
        int index = 1;
        while (index < lines.Length)
        {
            Assert.That(lines[index], Does.StartWith(" "));
            Assert.That(lines[index].Length, Is.LessThanOrEqualTo(76));
            index++;
        }
    }

    [Test]
    public void TextGenerationNeverSplitsUtf8SupplementaryCharacters()
    {
        string input = "😀😀😀😀😀😀";
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("utf-8");
        options.SetEncodingChoice(EmailEncodedWordEncodingChoice.Base64);
        options.SetMaximumLineLength(24);

        EmailEncodedWordEncodeResult encoded = new EmailEncodedWord().EncodeText(input, new UTF8Encoding(false, true), options);
        EmailEncodedWordParseResult decoded = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            encoded.GetBytes(),
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);
        UTF8Encoding strictUtf8 = new UTF8Encoding(false, true);

        Assert.That(encoded.GetEncodedWordCount(), Is.EqualTo(6));
        int index = 0;
        while (index < decoded.GetPartCount())
        {
            Assert.That(strictUtf8.GetString(decoded.GetPart(index).GetBytes()), Is.EqualTo("😀"));
            index++;
        }

        Assert.That(strictUtf8.GetString(decoded.GetCombinedBytes()), Is.EqualTo(input));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void RawUtf8GenerationAlsoKeepsValidSequencesInOneWord()
    {
        byte[] input = Encoding.UTF8.GetBytes("ab😀cd😀ef😀gh");
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("UTF-8");
        options.SetEncodingChoice(EmailEncodedWordEncodingChoice.Base64);
        options.SetMaximumLineLength(24);

        EmailEncodedWordEncodeResult encoded = new EmailEncodedWord().Encode(input, options);
        EmailEncodedWordParseResult decoded = new EmailEncodedWord().ParseWithInputAndModeAndContext(encoded.GetBytes(), EmailEncodedWordParseMode.Strict, EmailEncodedWordContext.UnstructuredText);
        UTF8Encoding strictUtf8 = new UTF8Encoding(false, true);

        int index = 0;
        while (index < decoded.GetPartCount())
        {
            strictUtf8.GetString(decoded.GetPart(index).GetBytes());
            index++;
        }

        Assert.That(decoded.GetCombinedBytes(), Is.EqualTo(input));
    }

    [Test]
    public void LanguageMetadataIsGeneratedAndRecovered()
    {
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("utf-8");
        options.SetLanguage("fr-CA");
        options.SetEncodingChoice(EmailEncodedWordEncodingChoice.QuotedPrintable);

        EmailEncodedWordEncodeResult encoded = new EmailEncodedWord().Encode(Encoding.ASCII.GetBytes("bonjour"), options);
        EmailEncodedWordParseResult decoded = new EmailEncodedWord().ParseWithInputAndModeAndContext(encoded.GetBytes(), EmailEncodedWordParseMode.Strict, EmailEncodedWordContext.UnstructuredText);

        Assert.That(Encoding.ASCII.GetString(encoded.GetBytes()), Is.EqualTo("=?utf-8*fr-CA?Q?bonjour?="));
        Assert.That(decoded.GetPart(0).GetCharset(), Is.EqualTo("utf-8"));
        Assert.That(decoded.GetPart(0).HasLanguage(), Is.True);
        Assert.That(decoded.GetPart(0).GetLanguage(), Is.EqualTo("fr-CA"));
    }

    [Test]
    public void EmptyInputProducesNoWordsOrFolds()
    {
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("utf-8");
        options.SetFirstLineColumn(12);

        EmailEncodedWordEncodeResult result = new EmailEncodedWord().Encode(Array.Empty<byte>(), options);

        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetEncodedWordCount(), Is.Zero);
        Assert.That(result.GetFoldCount(), Is.Zero);
        Assert.That(result.GetFinalLineColumn(), Is.EqualTo(12));
    }

    [Test]
    public void EncodedWordLimitIsEnforcedBeforeReturningPartialOutput()
    {
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("utf-8");
        options.SetEncodingChoice(EmailEncodedWordEncodingChoice.QuotedPrintable);
        options.SetMaximumEncodedWords(1);

        EmailLimitException exception = Assert.Throws<EmailLimitException>(() => new EmailEncodedWord().Encode(Encoding.ASCII.GetBytes(new string('a', 100)), options));

        Assert.That(exception.GetLimitName(), Is.EqualTo("encoded words"));
        Assert.That(exception.GetLimit(), Is.EqualTo(1));
        Assert.That(exception.GetObserved(), Is.EqualTo(2));
    }

    [Test]
    public void InvalidOptionsAndArgumentsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailEncodedWordGenerationOptions(null!));
        Assert.Throws<ArgumentException>(() => new EmailEncodedWordGenerationOptions("bad.name"));
        Assert.Throws<ArgumentException>(() => new EmailEncodedWordGenerationOptions("bad name"));

        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("utf-8");
        Assert.Throws<ArgumentNullException>(() => options.SetLanguage(null));
        Assert.Throws<ArgumentException>(() => options.SetLanguage("en_US"));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetEncodingChoice((EmailEncodedWordEncodingChoice)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetContext((EmailEncodedWordContext)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetFirstLineColumn(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumLineLength(77));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEncodedWords(-1));
        Assert.Throws<ArgumentNullException>(() => new EmailEncodedWord().Encode(Array.Empty<byte>(), null!));
        Assert.Throws<ArgumentNullException>(() => new EmailEncodedWord().EncodeText(null!, Encoding.UTF8, options));
        Assert.Throws<ArgumentNullException>(() => new EmailEncodedWord().EncodeText("x", null!, options));

        options.SetContext(EmailEncodedWordContext.Forbidden);
        Assert.Throws<ArgumentException>(() => new EmailEncodedWord().Encode(Encoding.ASCII.GetBytes("x"), options));
    }

    [Test]
    public void TooSmallLineLimitAndOversizedCharacterAreRejected()
    {
        EmailEncodedWordGenerationOptions tooSmall = new EmailEncodedWordGenerationOptions("utf-8");
        tooSmall.SetMaximumLineLength(10);
        Assert.Throws<ArgumentException>(() => new EmailEncodedWord().Encode(Encoding.ASCII.GetBytes("x"), tooSmall));

        EmailEncodedWordGenerationOptions oneCharacterTooLarge = new EmailEncodedWordGenerationOptions("utf-8");
        oneCharacterTooLarge.SetEncodingChoice(EmailEncodedWordEncodingChoice.QuotedPrintable);
        oneCharacterTooLarge.SetMaximumLineLength(20);
        Assert.Throws<ArgumentException>(() => new EmailEncodedWord().EncodeText("😀", new UTF8Encoding(false, true), oneCharacterTooLarge));
    }

    [Test]
    public void OptionsAndResultCopiesAreIndependent()
    {
        EmailEncodedWordGenerationOptions options = new EmailEncodedWordGenerationOptions("utf-8");
        options.SetLanguage("");
        options.SetEncodingChoice(EmailEncodedWordEncodingChoice.Base64);
        options.SetFirstLineColumn(4);
        options.SetMaximumEncodedWords(3);
        EmailEncodedWordGenerationOptions copy = options.Clone();
        options.ClearLanguage();
        options.SetCharset("us-ascii");

        EmailEncodedWordEncodeResult result = new EmailEncodedWord().Encode(Encoding.ASCII.GetBytes("A"), copy);
        byte[] returned = result.GetBytes();
        returned[0] = 0;

        Assert.That(copy.GetCharset(), Is.EqualTo("utf-8"));
        Assert.That(copy.HasLanguage(), Is.True);
        Assert.That(copy.GetLanguage(), Is.Empty);
        Assert.That(copy.GetMaximumEncodedWords(), Is.EqualTo(3));
        Assert.That(Encoding.ASCII.GetString(result.GetBytes()), Is.EqualTo("=?utf-8*?B?QQ==?="));
    }
}
