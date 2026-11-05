using fake;

namespace fake.Tests;

[TestFixture]
public class LoremGeneratorTests
{
    [Test]
    public void ExposesWeightedWordsAndStablePartOfSpeechCatalog()
    {
        TextProvider text = Fake.Create(4101).Text();

        Assert.Multiple(() =>
        {
            Assert.That(
                text.PartsOfSpeech(),
                Is.EqualTo(new string[] { "adjective", "adverb", "noun", "verb" }));
            Assert.That(text.WordFromPartOfSpeech("noun"), Is.AnyOf("anchor", "brook", "canvas", "garden", "harbor", "lantern", "meadow", "pattern", "signal", "window"));
            Assert.That(text.PartOfSpeech("adjective"), Is.AnyOf("bright", "calm", "clear", "gentle", "open", "quiet", "steady", "subtle", "warm", "wide"));
            Assert.That(text.PartOfSpeechFromNoArguments(), Is.AnyOf("adjective", "adverb", "noun", "verb"));
            Assert.That(text.Word(), Is.Not.Empty);
        });
    }

    [Test]
    public void RuntimeLocaleWordsOverrideFallbackAndHonorWeights()
    {
        Fake fake = Fake.Create(4102);
        Dictionary<string, object> words = new Dictionary<string, object>
        {
            ["values"] = new object[]
            {
                new Dictionary<string, object> { ["text"] = "never", ["partOfSpeech"] = "noun" },
                new Dictionary<string, object> { ["text"] = "local", ["partOfSpeech"] = "noun" },
                new Dictionary<string, object> { ["text"] = "move", ["partOfSpeech"] = "verb" }
            },
            ["weights"] = new double[] { 0.0, 5.0, 1.0 }
        };
        fake.Locale().RegisterFromLocaleAndFields("en-US", new Dictionary<string, object> { ["text.words"] = words });
        TextProvider text = fake.Text();

        Assert.That(Enumerable.Range(0, 50).Select(_ => text.WordFromPartOfSpeech("noun")), Has.All.EqualTo("local"));
        Assert.That(text.PartsOfSpeech(), Is.EqualTo(new string[] { "noun", "verb" }));
    }

    [Test]
    public void GeneratesWordCollectionsSentencesParagraphsLinesAndSlugs()
    {
        TextProvider text = Fake.Create(4103).Text();

        string words = text.Words(4);
        string sentence = text.SentenceFromWordCount(5);
        string sentences = text.Sentences(3);
        string paragraph = text.ParagraphFromSentenceCount(2);
        string paragraphs = text.Paragraphs(2);
        string lines = text.Lines(3);
        string slug = text.SlugFromWordCount(4);

        Assert.Multiple(() =>
        {
            Assert.That(words.Split(' '), Has.Length.EqualTo(4));
            Assert.That(sentence, Does.Match(@"^[A-Z][a-z]+(?: [a-z]+){4}\.$"));
            Assert.That(sentences.Count(character => character == '.'), Is.EqualTo(3));
            Assert.That(paragraph.Count(character => character == '.'), Is.EqualTo(2));
            Assert.That(paragraphs.Split("\n\n"), Has.Length.EqualTo(2));
            Assert.That(lines.Split('\n'), Has.Length.EqualTo(3));
            Assert.That(slug.Split('-'), Has.Length.EqualTo(4));
            Assert.That(slug, Does.Match("^[a-z]+(?:-[a-z]+){3}$"));
        });
    }

    [Test]
    public void ExactAndBoundedTextHonorUtf16Lengths()
    {
        TextProvider text = Fake.Create(4104).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.Text(0), Is.Empty);
            Assert.That(text.Text(1), Has.Length.EqualTo(1));
            Assert.That(text.Text(17), Has.Length.EqualTo(17));
            Assert.That(text.Text(80), Has.Length.EqualTo(80));
        });
        for (int index = 0; index < 100; index++)
        {
            Assert.That(text.TextFromMinimumLengthAndMaximumLength(12, 24).Length, Is.InRange(12, 24));
        }
    }

    [Test]
    public void LoremGenerationIsDeterministicForEqualSeeds()
    {
        TextProvider first = Fake.Create(4105).Text();
        TextProvider second = Fake.Create(4105).Text();

        Assert.Multiple(() =>
        {
            Assert.That(first.Words(8), Is.EqualTo(second.Words(8)));
            Assert.That(first.Sentence(), Is.EqualTo(second.Sentence()));
            Assert.That(first.Paragraph(), Is.EqualTo(second.Paragraph()));
            Assert.That(first.Text(50), Is.EqualTo(second.Text(50)));
        });
    }

    [Test]
    public void RejectsInvalidCountsRangesPartsAndOversizedOutput()
    {
        TextProvider text = Fake.Create(4106).Text();
        FakeOptions options = new FakeOptions();
        options.MaximumOutputSize = 10;
        TextProvider limited = Fake.Create(options).Text();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Words(-1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.SentenceFromWordCount(0)).ParamName, Is.EqualTo("wordCount"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.SentenceFromMinimumWordsAndMaximumWords(5, 4)).ParamName, Is.EqualTo("maximumWords"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.WordFromPartOfSpeech("article")).ParamName, Is.EqualTo("partOfSpeech"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Text(-1)).ParamName, Is.EqualTo("length"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.TextFromMinimumLengthAndMaximumLength(8, 4)).ParamName, Is.EqualTo("maximumLength"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => limited.Text(11)).ParamName, Is.EqualTo("length"));
        });
    }
}
