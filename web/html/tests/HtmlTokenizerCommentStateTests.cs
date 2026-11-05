
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerCommentStateTests
{
    [Test]
    public void CommentStateRecoveryIsStableAtEveryChunkSplit()
    {
        string input = "<!--a<<!x<!-y--!>tail";
        for (int split = 0; split < input.Length - 4; split++)
        {
            HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
            tokenizer.Feed(input.AsSpan(0, split), false);
            HtmlTokenReadResult suspension = tokenizer.Read();
            Assert.That(suspension.GetStatus(), Is.EqualTo(HtmlTokenReadStatus.NeedMoreInput));
            tokenizer.Feed(input.AsSpan(split), true);

            HtmlTokenReadResult comment = tokenizer.Read();
            HtmlDiagnosticCode[] codes = suspension.GetDiagnostics()
                .Concat(comment.GetDiagnostics()).Select(value => value.GetCode()).ToArray();

            Assert.Multiple(() =>
            {
                AssertComment(comment, "a<<!x<!-y");
                Assert.That(comment.GetToken().GetSourceSpan().GetNormalizedCharacterLength(),
                    Is.EqualTo(input.Length - 4), $"split {split}");
                Assert.That(codes, Is.EqualTo(new[]
                {
                    HtmlDiagnosticCode.TokenizerMalformedComment
                }), $"split {split}");
            });

            AssertCharacter(tokenizer.Read(), "tail");
        }
    }

    [TestCase("<!-->")]
    [TestCase("<!--->")]
    public void AbruptEmptyCommentClosesEmitEmptyCommentWithDiagnostic(string input)
    {
        HtmlTokenReadResult result = Tokenize(input).Read();

        AssertComment(result, "");
        AssertMalformedDiagnostics(result, 1);
    }

    [Test]
    public void NestedCommentOpenersAreDiagnosedAndRemainInCommentData()
    {
        HtmlTokenizer tokenizer = Tokenize("<!--a<!--b-->tail");

        HtmlTokenReadResult result = tokenizer.Read();

        AssertComment(result, "a<!--b");
        AssertMalformedDiagnostics(result, 1);
        AssertCharacter(tokenizer.Read(), "tail");
    }

    [TestCase("<!--a--x-->", "a--x")]
    [TestCase("<!--a--!x-->", "a--!x")]
    [TestCase("<!--a--!-x-->", "a--!-x")]
    public void FalseCommentClosersAreRetained(string input, string expected)
    {
        HtmlTokenReadResult result = Tokenize(input).Read();

        AssertComment(result, expected);
        Assert.That(result.GetDiagnostics(), Is.Empty);
    }

    [TestCase("<?target?>", "?target?")]
    [TestCase("<!bogus>", "bogus")]
    [TestCase("</42>", "42")]
    public void BogusCommentEntryPathsConsumeThroughGreaterThan(string input, string expected)
    {
        HtmlTokenReadResult result = Tokenize(input).Read();

        AssertComment(result, expected);
        AssertMalformedDiagnostics(result, 1);
        Assert.That(result.GetToken().GetSourceSpan().GetNormalizedCharacterLength(),
            Is.EqualTo(input.Length));
    }

    [TestCase("<?tail", "?tail")]
    [TestCase("<!", "")]
    [TestCase("<!bogus", "bogus")]
    [TestCase("</42", "42")]
    public void BogusCommentAtEofEmitsWithoutAnAdditionalEofDiagnostic(
        string input, string expected)
    {
        HtmlTokenReadResult result = Tokenize(input).Read();

        AssertComment(result, expected);
        AssertMalformedDiagnostics(result, 1);
    }

    [Test]
    public void MarkupDeclarationSuspendsWhileACommentOpenerRemainsPossible()
    {
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
        tokenizer.Feed("<!-".AsSpan(), false);

        Assert.That(tokenizer.Read().GetStatus(), Is.EqualTo(HtmlTokenReadStatus.NeedMoreInput));

        tokenizer.Feed("x>".AsSpan(), true);
        HtmlTokenReadResult result = tokenizer.Read();
        AssertComment(result, "-x");
        AssertMalformedDiagnostics(result, 1);
    }

    [TestCase("<!--", "")]
    [TestCase("<!---", "")]
    [TestCase("<!--x", "x")]
    [TestCase("<!--x-", "x")]
    [TestCase("<!--x--", "x")]
    [TestCase("<!--x--!", "x")]
    public void EofInCommentDiscardsPendingDelimiterCharacters(string input, string expected)
    {
        HtmlTokenizer tokenizer = Tokenize(input);

        HtmlTokenReadResult result = tokenizer.Read();

        AssertComment(result, expected);
        AssertMalformedDiagnostics(result, 1);
        Assert.That(tokenizer.Read().GetToken().GetKind(), Is.EqualTo(HtmlTokenKind.EndOfFile));
    }

    [Test]
    public void NullCharactersAreReplacedInsideComments()
    {
        HtmlTokenReadResult result = Tokenize("<!--a\0b-->").Read();

        AssertComment(result, "a\uFFFDb");
        Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.InputNullCharacter }));
    }

    [Test]
    public void DisabledSourceTrackingReturnsUnknownCommentSpan()
    {
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetTrackSourceLocations(false);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);
        tokenizer.Feed("<!--note-->".AsSpan(), true);

        Assert.That(tokenizer.Read().GetToken().GetSourceSpan().IsKnown(), Is.False);
    }

    [Test]
    public void CommentDataHonorsMaximumTokenCharacters()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokenCharacters(3);
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);
        tokenizer.Feed("<!--abcd-->".AsSpan(), true);

        Assert.That(tokenizer.Read().GetStatus(), Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
        Assert.That(tokenizer.Read().GetStatus(), Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
    }

    static HtmlTokenizer Tokenize(string input)
    {
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
        tokenizer.Feed(input.AsSpan(), true);
        return tokenizer;
    }

    static void AssertComment(HtmlTokenReadResult result, string expected)
    {
        Assert.That(result.GetStatus(), Is.EqualTo(HtmlTokenReadStatus.TokenAvailable));
        Assert.That(result.GetToken().GetKind(), Is.EqualTo(HtmlTokenKind.Comment));
        Assert.That(result.GetToken().GetData(), Is.EqualTo(expected));
    }

    static void AssertCharacter(HtmlTokenReadResult result, string expected)
    {
        Assert.That(result.GetToken().GetKind(), Is.EqualTo(HtmlTokenKind.Character));
        Assert.That(result.GetToken().GetData(), Is.EqualTo(expected));
    }

    static void AssertMalformedDiagnostics(HtmlTokenReadResult result, int count)
    {
        Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(count));
        Assert.That(result.GetDiagnostics().All(value =>
            value.GetCode() == HtmlDiagnosticCode.TokenizerMalformedComment), Is.True);
    }
}
