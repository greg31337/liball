using fake;

namespace fake.Tests;

[TestFixture]
public class TextEdgeCaseTests
{
    [Test]
    public void EmptyLoremRequestsNeedNoLocaleDataAndConsumeNoEntropy()
    {
        FakeOptions options = FakeOptions.Create(4201L);
        options.StrictnessMode = StrictnessMode.Strict;
        Fake fake = Fake.Create(options);
        TextProvider text = fake.Text();
        FakeState before = fake.Snapshot();

        Assert.Multiple(() =>
        {
            Assert.That(text.Words(0), Is.Empty);
            Assert.That(text.Sentences(0), Is.Empty);
            Assert.That(text.ParagraphFromSentenceCount(0), Is.Empty);
            Assert.That(text.ParagraphFromMinimumSentencesAndMaximumSentences(0, 0), Is.Empty);
            Assert.That(text.Paragraphs(0), Is.Empty);
            Assert.That(text.Lines(0), Is.Empty);
            Assert.That(text.SlugFromWordCount(0), Is.Empty);
            Assert.That(text.Text(0), Is.Empty);
            Assert.That(text.TextFromMinimumLengthAndMaximumLength(0, 0), Is.Empty);
        });
        AssertState(fake.Snapshot(), before);
    }

    [Test]
    public void WhitespaceSeparatorsAndSentencePunctuationArePreservedPrecisely()
    {
        TextProvider text = Fake.Create(4202).Text();

        string words = text.WordsFromCountAndSeparator(3, "\t|\t");
        string sentence = text.SentenceFromWordCount(3);

        Assert.Multiple(() =>
        {
            Assert.That(words.Split("\t|\t"), Has.Length.EqualTo(3));
            Assert.That(words.Count(character => character == '|'), Is.EqualTo(2));
            Assert.That(sentence, Does.EndWith("."));
            Assert.That(sentence.Count(character => character == '.'), Is.EqualTo(1));
            Assert.That(sentence.TrimEnd('.').Split(' '), Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void LocaleWordsHonorLengthChangingNormalization()
    {
        Fake fake = LoremFake(
            4203L,
            "en-US",
            NormalizationMode.Nfkd,
            1000,
            new string[] { "ﬁ" },
            new string[] { "noun" },
            new double[] { 1.0 });
        TextProvider text = fake.Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.Word(), Is.EqualTo("fi"));
            Assert.That(text.WordsFromCountAndSeparator(2, "|"), Is.EqualTo("fi|fi"));
            Assert.That(text.SentenceFromWordCount(1), Is.EqualTo("Fi."));
            Assert.That(text.IsNormalized(text.Text(12)), Is.True);
        });
    }

    [Test]
    public void RtlLocaleWordsRemainRtlThroughSentenceAndSlugGeneration()
    {
        Fake fake = LoremFake(
            4204L,
            "he-IL",
            NormalizationMode.Nfc,
            1000,
            new string[] { "שלום" },
            new string[] { "noun" },
            new double[] { 1.0 });
        TextProvider text = fake.Text();

        string sentence = text.SentenceFromWordCount(2);

        Assert.Multiple(() =>
        {
            Assert.That(sentence, Is.EqualTo("שלום שלום."));
            Assert.That(text.Direction(sentence), Is.EqualTo(WritingDirection.RightToLeft));
            Assert.That(text.SlugFromWordCount(2), Is.EqualTo("shlvm-shlvm"));
        });
    }

    [Test]
    public void ExactTextNeverSplitsEmojiOrCombiningGraphemes()
    {
        const string family = "👨‍👩‍👧‍👦";
        Fake emojiFake = LoremFake(
            4205L,
            "en-US",
            NormalizationMode.None,
            1000,
            new string[] { family },
            new string[] { "noun" },
            new double[] { 1.0 });
        TextProvider emoji = emojiFake.Text();
        Fake combiningFake = LoremFake(
            4206L,
            "en-US",
            NormalizationMode.None,
            1000,
            new string[] { "e\u0301" },
            new string[] { "noun" },
            new double[] { 1.0 });
        TextProvider combining = combiningFake.Text();

        string shortenedEmoji = emoji.Text(family.Length - 1);

        Assert.Multiple(() =>
        {
            Assert.That(emoji.Text(family.Length), Is.EqualTo(family));
            Assert.That(shortenedEmoji, Has.Length.EqualTo(family.Length - 1));
            Assert.That(shortenedEmoji, Does.Match("^\\.+$"));
            Assert.That(emoji.SentenceFromWordCount(1), Is.EqualTo(family + "."));
            Assert.That(combining.Text(2), Is.EqualTo("e\u0301"));
            Assert.That(combining.Text(1), Is.EqualTo("."));
            Assert.That(combining.GraphemeCount(combining.Text(2)), Is.EqualTo(1));
        });
    }

    [Test]
    public void OverlongCompositionsFailBeforeConsumingEntropy()
    {
        Fake sentenceFake = LoremFake(
            4207L,
            "en-US",
            NormalizationMode.None,
            9,
            new string[] { "abcd" },
            new string[] { "noun" },
            new double[] { 1.0 });
        FakeState sentenceBefore = sentenceFake.Snapshot();

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => sentenceFake.Text().SentenceFromWordCount(2)).ParamName,
            Is.EqualTo("wordCount"));
        AssertState(sentenceFake.Snapshot(), sentenceBefore);

        Fake linesFake = LoremFake(
            4208L,
            "en-US",
            NormalizationMode.None,
            100,
            new string[] { "abcd" },
            new string[] { "noun" },
            new double[] { 1.0 });
        FakeState linesBefore = linesFake.Snapshot();

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => linesFake.Text().Lines(2)).ParamName,
            Is.EqualTo("count"));
        AssertState(linesFake.Snapshot(), linesBefore);
    }

    static Fake LoremFake(
        long seed,
        string locale,
        NormalizationMode normalizationMode,
        int maximumOutputSize,
        string[] values,
        string[] parts,
        double[] weights)
    {
        FakeOptions options = FakeOptions.Create(seed);
        options.Locale = locale;
        options.NormalizationMode = normalizationMode;
        options.MaximumOutputSize = maximumOutputSize;
        Fake fake = Fake.Create(options);
        object[] entries = new object[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            entries[index] = new Dictionary<string, object>
            {
                ["text"] = values[index],
                ["partOfSpeech"] = parts[index]
            };
            index = index + 1;
        }
        Dictionary<string, object> words = new Dictionary<string, object>
        {
            ["values"] = entries,
            ["weights"] = weights
        };
        fake.Locale().RegisterFromLocaleAndFields(
            locale,
            new Dictionary<string, object> { ["text.words"] = words });
        return fake;
    }

    static void AssertState(FakeState actual, FakeState expected)
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
