
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTextConverterGoldenTests
{
    static IEnumerable<TestCaseData> DefaultGoldenCases()
    {
        yield return new TestCaseData(
            " \tAlpha\r\n beta&nbsp;gamma \f delta ",
            "Alpha beta\u00A0gamma delta")
            .SetName("Whitespace_and_non_breaking_space");
        yield return new TestCaseData(
            "<p>Zażółć gęślą jaźń — 東京 😀</p>"
            + "<p><bdi dir=rtl>مرحبا</bdi> दुनिया</p>",
            "Zażółć gęślą jaźń — 東京 😀\n\n"
            + "\u2067مرحبا\u2069 दुनिया")
            .SetName("International_scripts_and_bidi_isolation");
        yield return new TestCaseData(
            "<p>one<div>two<p>three</div>four",
            "one\n\ntwo\n\nthree\n\nfour")
            .SetName("Malformed_paragraphs_use_the_corrected_tree");
        yield return new TestCaseData(
            "<ul><li>Alpha<ul><li>Nested</ul><li>Beta</ul>"
            + "<ol start=3><li>Third<li>Fourth</ol>",
            "- Alpha\n  * Nested\n- Beta\n\n3. Third\n4. Fourth")
            .SetName("Corrected_nested_and_ordered_lists");
        yield return new TestCaseData(
            "<p>inline <code>a   b</code><br>end</p>"
            + "<pre>  exact   text\r\n\tline\n</pre>",
            "inline a b\nend\n\n  exact   text\n\tline")
            .SetName("Preformatted_and_inline_code_whitespace");
    }

    [TestCaseSource(nameof(DefaultGoldenCases))]
    public void DefaultConversionMatchesGoldenText(string html,
        string expected)
    {
        HtmlTextResult result = HtmlTextConverter.Convert(html);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.GetText(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void LinksAndMediaMatchConfiguredGoldenText()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
        options.SetImageStyle(HtmlTextImageStyle.AltTextAndUrl);
        string html = "<p><a href='https://example.test/docs'>Docs</a> "
            + "<img alt='Map' src='https://example.test/map.png'> "
            + "<video title='Clip' src='https://example.test/movie.mp4'>"
            + "fallback</video></p>";

        HtmlTextResult result = HtmlTextConverter.Convert(html, options,
            CancellationToken.None);

        Assert.That(result.GetText(), Is.EqualTo(
            "Docs (https://example.test/docs) "
            + "Map (https://example.test/map.png) "
            + "Clip (https://example.test/movie.mp4)"));
    }

    [Test]
    public void CorrectedTableMatchesTsvGoldenText()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetTableStyle(HtmlTextTableStyle.Tsv);
        string html = "<table><caption>Stock</caption>"
            + "<tr><th>Item<th>Count<tbody>"
            + "<tr><td rowspan=2>Tea<td>2<tr><td>5</table>";

        HtmlTextResult result = HtmlTextConverter.Convert(html, options,
            CancellationToken.None);

        Assert.That(result.GetText(), Is.EqualTo(
            "Stock\nItem\tCount\nTea\t2\n\t5"));
    }

    [Test]
    public void SelectorRulesMatchGoldenPrecedenceAndScope()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.AddExcludedSelector(".skip, strong");
        options.AddReplacementSelector("article strong", "[important]");
        options.AddIncludedSelector("script.keep");
        string html = "<main><p class=skip>hidden</p><article>"
            + "Visible <strong>secret</strong>"
            + "<script class=keep>code()</script></article></main>";

        HtmlTextResult result = HtmlTextConverter.Convert(html, options,
            CancellationToken.None);

        Assert.That(result.GetText(),
            Is.EqualTo("Visible [important]code()"));
    }

    [Test]
    public void WrappingMatchesGoldenUnicodeAndHangingIndentText()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetWrapColumn(10);
        string html = "<p>猫猫 alpha beta</p>"
            + "<ul><li>first second extraordinary tail</li></ul>";

        HtmlTextResult result = HtmlTextConverter.Convert(html, options,
            CancellationToken.None);

        Assert.That(result.GetText(), Is.EqualTo(
            "猫猫 alpha\nbeta\n\n- first\n  second\n  extraordinary\n  tail"));
    }

    [Test]
    public void AsciiWhitespaceVariantsHaveOneNormalizedResult()
    {
        string[] separators =
            [" ", "  ", "\t", "\r", "\n", "\f", " \r\n\t "];
        string[] words = ["alpha", "beta", "gamma", "delta", "epsilon"];
        Random random = new Random(0x113113);
        int sample = 0;
        while (sample < 128)
        {
            string html = separators[random.Next(separators.Length)];
            int index = 0;
            while (index < words.Length)
            {
                html = html + words[index]
                    + separators[random.Next(separators.Length)];
                index = index + 1;
            }

            HtmlTextResult result = HtmlTextConverter.Convert(html);
            Assert.That(result.GetText(),
                Is.EqualTo("alpha beta gamma delta epsilon"),
                $"Whitespace sample {sample}");
            sample = sample + 1;
        }
    }

    [Test]
    public void SeededWrappingPreservesWordsAndOnlyOverflowsForLongTokens()
    {
        Random random = new Random(0x713713);
        int sample = 0;
        while (sample < 128)
        {
            int wrapColumn = random.Next(6, 25);
            int wordCount = random.Next(3, 18);
            string[] words = new string[wordCount];
            int index = 0;
            while (index < words.Length)
            {
                int length = random.Next(1, 32);
                words[index] = new string((char)('a' + index % 26), length);
                index = index + 1;
            }

            string source = string.Join(" ", words);
            HtmlTextOptions options = HtmlTextOptions.CreateDefault();
            options.SetWrapColumn(wrapColumn);
            string text = HtmlTextConverter.Convert("<p>" + source + "</p>",
                options, CancellationToken.None).GetText();

            Assert.That(text.Replace("\n", " "), Is.EqualTo(source),
                $"Wrapping sample {sample}");
            string[] lines = text.Split('\n');
            index = 0;
            while (index < lines.Length)
            {
                if (lines[index].Length > wrapColumn)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(lines[index], Does.Not.Contain(" "));
                        Assert.That(lines[index].Length,
                            Is.GreaterThan(wrapColumn));
                    });
                }

                index = index + 1;
            }

            sample = sample + 1;
        }
    }

    [Test]
    public void TightOutputBudgetsTerminateDeterministicallyWithinTheLimit()
    {
        string html = "<main>" + string.Concat(Enumerable.Repeat(
            "<p>alpha beta gamma delta epsilon</p>", 64)) + "</main>";
        int maximum = 1;
        while (maximum <= 32)
        {
            HtmlLimits limits = HtmlLimits.CreateDefault();
            limits.SetMaximumOutputCharacters(maximum);
            HtmlTextOptions options = HtmlTextOptions.CreateDefault();
            options.SetLimits(limits);
            options.SetTruncationMarker("~");

            HtmlTextResult first = HtmlTextConverter.Convert(html, options,
                CancellationToken.None);
            HtmlTextResult second = HtmlTextConverter.Convert(html, options,
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(first.WasTruncated(), Is.True,
                    $"Output budget {maximum}");
                Assert.That(first.GetText().Length,
                    Is.LessThanOrEqualTo(maximum),
                    $"Output budget {maximum}");
                Assert.That(first.GetText(), Does.EndWith("~"),
                    $"Output budget {maximum}");
                Assert.That(second.GetText(), Is.EqualTo(first.GetText()),
                    $"Output budget {maximum}");
            });

            maximum = maximum + 1;
        }
    }

    [Test]
    public void TightVisitBudgetsTerminateWithAStablePartialResult()
    {
        HtmlNode fragment = Html.ParseFragment(
            "<section><p>one <b>two</b></p><p>three <i>four</i></p>"
            + "<ul><li>five</li><li>six</li></ul></section>", "body")
            .GetFragment();
        int maximum = 1;
        while (maximum <= 12)
        {
            HtmlLimits limits = HtmlLimits.CreateDefault();
            limits.SetMaximumVisits(maximum);
            HtmlTextOptions options = HtmlTextOptions.CreateDefault();
            options.SetLimits(limits);
            options.SetTruncationMarker("~");

            HtmlTextResult first = HtmlTextConverter.Convert(fragment,
                options, CancellationToken.None);
            HtmlTextResult second = HtmlTextConverter.Convert(fragment,
                options, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(first.WasTruncated(), Is.True,
                    $"Visit budget {maximum}");
                Assert.That(first.IsComplete(), Is.False,
                    $"Visit budget {maximum}");
                Assert.That(first.GetText(), Does.EndWith("~"),
                    $"Visit budget {maximum}");
                Assert.That(second.GetText(), Is.EqualTo(first.GetText()),
                    $"Visit budget {maximum}");
            });

            maximum = maximum + 1;
        }
    }
}
