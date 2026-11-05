
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerMarkupDeclarationTests
{
    [Test]
    public void DoctypeKeywordIsRecognizedCaseInsensitivelyAtEveryChunkSplit()
    {
        string input = "<!dOcTyPe";
        for (int split = 0; split <= input.Length; split++)
        {
            HtmlTokenizerContext context = new HtmlTokenizerContext(
                HtmlTokenizerOptions.CreateDefault(), null, false);
            context.Feed(input.AsSpan(0, split), false);
            Assert.That(context.Read().GetStatus(), Is.EqualTo(HtmlTokenReadStatus.NeedMoreInput));

            context.Feed(input.AsSpan(split), false);
            Assert.That(context.Read().GetStatus(), Is.EqualTo(HtmlTokenReadStatus.NeedMoreInput));
            Assert.That(context.GetState(), Is.EqualTo(HtmlTokenizerState.Doctype),
                $"split {split}");
        }
    }

    [Test]
    public void DeclarationRecognitionWaitsOnlyWhileAKeywordCanStillMatch()
    {
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
        tokenizer.Feed("<!DOC".AsSpan(), false);

        Assert.That(tokenizer.Read().GetStatus(), Is.EqualTo(HtmlTokenReadStatus.NeedMoreInput));

        tokenizer.Feed("X>".AsSpan(), true);
        HtmlTokenReadResult result = tokenizer.Read();
        Assert.That(result.GetToken().GetKind(), Is.EqualTo(HtmlTokenKind.Comment));
        Assert.That(result.GetToken().GetData(), Is.EqualTo("DOCX"));
        Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.TokenizerMalformedComment }));
    }

    [Test]
    public void CdataKeywordIsCaseSensitiveInHtmlContent()
    {
        HtmlTokenReadResult exact = Tokenize("<![CDATA[x]]>", false).Read();
        HtmlTokenReadResult wrongCase = Tokenize("<![cdata[x]]>", false).Read();

        Assert.Multiple(() =>
        {
            Assert.That(exact.GetToken().GetKind(), Is.EqualTo(HtmlTokenKind.Comment));
            Assert.That(exact.GetToken().GetData(), Is.EqualTo("[CDATA[x]]"));
            Assert.That(wrongCase.GetToken().GetKind(), Is.EqualTo(HtmlTokenKind.Comment));
            Assert.That(wrongCase.GetToken().GetData(), Is.EqualTo("[cdata[x]]"));
            Assert.That(exact.GetDiagnostics().Select(value => value.GetCode()),
                Is.EqualTo(new[] { HtmlDiagnosticCode.TokenizerMalformedComment }));
            Assert.That(wrongCase.GetDiagnostics().Select(value => value.GetCode()),
                Is.EqualTo(new[] { HtmlDiagnosticCode.TokenizerMalformedComment }));
        });
    }

    [Test]
    public void ForeignContentCdataIsStableAtEveryChunkSplit()
    {
        string input = "<![CDATA[a]b]]c]]>";
        for (int split = 0; split <= input.Length; split++)
        {
            HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
            tokenizer.SetCdataAllowed(true);
            tokenizer.Feed(input.AsSpan(0, split), false);
            List<HtmlDiagnosticCode> diagnostics = new List<HtmlDiagnosticCode>();
            string actual = ReadCharactersUntilPause(tokenizer, diagnostics);

            tokenizer.Feed(input.AsSpan(split), true);
            actual = actual + ReadCharactersUntilPause(tokenizer, diagnostics);

            Assert.That(actual, Is.EqualTo("a]b]]c"), $"split {split}");
            Assert.That(diagnostics, Is.Empty, $"split {split}");
        }
    }

    [Test]
    public void CdataCharacterSpansExcludeDeclarationDelimiters()
    {
        HtmlTokenizer tokenizer = Tokenize("<![CDATA[abc]]>", true);

        HtmlTokenReadResult text = tokenizer.Read();

        Assert.Multiple(() =>
        {
            Assert.That(text.GetToken().GetKind(), Is.EqualTo(HtmlTokenKind.Character));
            Assert.That(text.GetToken().GetData(), Is.EqualTo("abc"));
            Assert.That(text.GetToken().GetSourceSpan().GetStart()
                .GetNormalizedCharacterOffset(), Is.EqualTo(9));
            Assert.That(text.GetToken().GetSourceSpan().GetNormalizedCharacterLength(),
                Is.EqualTo(3));
        });
    }

    [TestCase("<![CDATA[x", "x")]
    [TestCase("<![CDATA[x]", "x]")]
    [TestCase("<![CDATA[x]]", "x]]")]
    public void EofInCdataFlushesPendingBracketsBeforeEofDiagnostic(
        string input, string expected)
    {
        HtmlTokenizer tokenizer = Tokenize(input, true);
        List<HtmlDiagnosticCode> diagnostics = new List<HtmlDiagnosticCode>();

        string actual = ReadCharactersUntilPause(tokenizer, diagnostics);

        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(diagnostics, Is.EqualTo(new[]
        {
            HtmlDiagnosticCode.TokenizerUnexpectedCharacter
        }));
    }

    [Test]
    public void CdataCharacterRunsHonorTheMaximumTokenLength()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokenCharacters(2);
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);
        tokenizer.SetCdataAllowed(true);
        tokenizer.Feed("<![CDATA[abcde]]>".AsSpan(), true);

        Assert.That(tokenizer.Read().GetToken().GetData(), Is.EqualTo("ab"));
        Assert.That(tokenizer.Read().GetToken().GetData(), Is.EqualTo("cd"));
        Assert.That(tokenizer.Read().GetToken().GetData(), Is.EqualTo("e"));
        Assert.That(tokenizer.Read().GetToken().GetKind(), Is.EqualTo(HtmlTokenKind.EndOfFile));
    }

    static HtmlTokenizer Tokenize(string input, bool cdataAllowed)
    {
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
        tokenizer.SetCdataAllowed(cdataAllowed);
        tokenizer.Feed(input.AsSpan(), true);
        return tokenizer;
    }

    static string ReadCharactersUntilPause(HtmlTokenizer tokenizer,
        List<HtmlDiagnosticCode> diagnostics)
    {
        string result = "";
        bool reading = true;
        while (reading)
        {
            HtmlTokenReadResult next = tokenizer.Read();
            diagnostics.AddRange(next.GetDiagnostics().Select(value => value.GetCode()));
            if (next.GetStatus() != HtmlTokenReadStatus.TokenAvailable)
            {
                reading = false;
            }
            else if (next.GetToken().GetKind() == HtmlTokenKind.Character)
            {
                result = result + next.GetToken().GetData();
            }
            else if (next.GetToken().GetKind() == HtmlTokenKind.EndOfFile)
            {
                reading = false;
            }
            else
            {
                Assert.Fail("Only character and EOF tokens were expected.");
            }
        }

        return result;
    }
}
