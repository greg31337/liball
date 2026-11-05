
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorWptConformanceTests
{
    const string SvgNamespace = "http://www.w3.org/2000/svg";

    [TestCaseSource(nameof(GetValidCases))]
    public void PinnedWptSelectorsReturnExpectedNodes(SelectorCase testCase)
    {
        SelectorFixture fixture = CreateFixture(testCase.Fixture);
        HtmlNode context = fixture.Contexts[testCase.Context];
        HtmlSelectorCompileResult compilation = HtmlSelectors.Compile(
            testCase.Selector);

        Assert.That(compilation.IsSuccess(), Is.True,
            testCase.Selector);
        HtmlSelector selector = compilation.GetSelector();
        string[] actual = selector.FindAll(context)
            .Select(node => node.GetAttribute("id"))
            .ToArray();
        HtmlNode first = selector.FindFirst(context);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(testCase.ExpectedIds),
                testCase.Selector);
            Assert.That(first?.GetAttribute("id"), Is.EqualTo(
                testCase.ExpectedIds.FirstOrDefault()), testCase.Selector);
            Assert.That(context.QuerySelectorAll(testCase.Selector)
                .Select(node => node.GetAttribute("id")),
                Is.EqualTo(testCase.ExpectedIds), testCase.Selector);
        });
    }

    [TestCaseSource(nameof(GetInvalidCases))]
    public void PinnedWptMalformedSelectorsFailAsWholeLists(
        SelectorCase testCase)
    {
        SelectorFixture fixture = CreateFixture(testCase.Fixture);
        HtmlNode context = fixture.Contexts[testCase.Context];
        HtmlSelectorCompileResult compilation = HtmlSelectors.Compile(
            testCase.Selector);

        Assert.Multiple(() =>
        {
            Assert.That(compilation.IsSuccess(), Is.False,
                testCase.Selector);
            Assert.That(compilation.GetSelector(), Is.Null,
                testCase.Selector);
            Assert.That(compilation.GetDiagnostics(), Is.Not.Empty,
                testCase.Selector);
            Assert.Throws<ArgumentException>(() =>
                context.QuerySelectorAll(testCase.Selector),
                testCase.Selector);
        });
    }

    [Test]
    public void FragmentScopeRepresentsTheFragmentForCombinators()
    {
        HtmlDocument document = Html.Parse("<main></main>");
        HtmlNode fragment = document.CreateFragment();
        HtmlNode direct = CreateElement(document, "section", "direct");
        HtmlNode wrapper = CreateElement(document, "div", "wrapper");
        HtmlNode nested = CreateElement(document, "section", "nested");
        direct.SetAttribute("class", "hit");
        nested.SetAttribute("class", "hit");
        wrapper.Append(nested);
        fragment.Append(direct);
        fragment.Append(wrapper);

        Assert.Multiple(() =>
        {
            Assert.That(fragment.QuerySelectorAll(":scope > .hit"),
                Is.EqualTo(new[] { direct }));
            Assert.That(fragment.QuerySelector(":scope > div > .hit"),
                Is.SameAs(nested));
            Assert.That(fragment.QuerySelector(":scope"), Is.Null);
        });
    }

    [Test]
    public void ComplexityLimitAcceptsItsBoundaryAndRejectsNextUnit()
    {
        HtmlLimits exact = HtmlLimits.CreateDefault();
        exact.SetMaximumSelectorComplexity(2);
        HtmlLimits exceeded = HtmlLimits.CreateDefault();
        exceeded.SetMaximumSelectorComplexity(1);

        HtmlSelectorCompileResult accepted = HtmlSelectors.Compile(
            "div.hit", exact);
        HtmlSelectorCompileResult rejected = HtmlSelectors.Compile(
            "div.hit", exceeded);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.IsSuccess(), Is.True);
            Assert.That(rejected.IsSuccess(), Is.False);
            Assert.That(rejected.GetErrors().Single().GetKind(), Is.EqualTo(
                HtmlSelectorCompileErrorKind.ComplexityExceeded));
        });
    }

    [Test]
    public void IndexedQueriesRefreshAfterRemovalInsertionAndAttributeChanges()
    {
        HtmlDocument document = Html.Parse(
            "<main><a id=old class=selected></a></main>");
        HtmlNode main = document.QuerySelector("main");
        HtmlNode old = document.FindById("old");
        HtmlSelector selector = Compile("#target, .selected, article");
        HtmlNode[] snapshot = selector.FindAll(document.GetRoot());
        HtmlNode replacement = CreateElement(document, "article", "target");

        old.Remove();
        main.Append(replacement);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Is.EqualTo(new[] { old }));
            Assert.That(selector.FindAll(document.GetRoot()),
                Is.EqualTo(new[] { replacement }));
        });

        replacement.SetAttribute("id", "changed");
        replacement.SetAttribute("class", "selected");

        Assert.That(selector.FindAll(document.GetRoot()),
            Is.EqualTo(new[] { replacement }));

        replacement.SetAttribute("class", "other");
        replacement.ReplaceWith(CreateElement(document, "div", "target"));

        Assert.That(selector.FindAll(document.GetRoot())
            .Select(node => node.GetAttribute("id")),
            Is.EqualTo(new[] { "target" }));
    }

    static IEnumerable<TestCaseData> GetValidCases()
    {
        return GetCases("valid");
    }

    static IEnumerable<TestCaseData> GetInvalidCases()
    {
        return GetCases("invalid");
    }

    static IEnumerable<TestCaseData> GetCases(string mode)
    {
        int ordinal = 0;
        foreach (string line in File.ReadLines(GetSubsetPath()))
        {
            if (line.StartsWith('#'))
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields[0] != mode)
            {
                continue;
            }

            string[] expected = fields[4] == "-"
                ? Array.Empty<string>()
                : fields[4].Split(',');
            SelectorCase testCase = new SelectorCase(fields[1], fields[2],
                fields[3], expected);
            yield return new TestCaseData(testCase).SetName(
                $"WPT {mode} selector {++ordinal:D2}");
        }
    }

    static string GetSubsetPath()
    {
        DirectoryInfo directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string path = Path.Combine(directory.FullName, "tests", "data",
                "selectors", "wpt-selector-subset.tsv");
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the pinned selector subset.");
    }

    static SelectorFixture CreateFixture(string name)
    {
        return name switch
        {
            "scope" => CreateScopeFixture(),
            "namespace" => CreateNamespaceFixture(),
            "case" => CreateCaseFixture(),
            "escapes" => CreateEscapeFixture(),
            "general" => CreateGeneralFixture(),
            _ => throw new InvalidOperationException("Unknown fixture: "
                + name)
        };
    }

    static SelectorFixture CreateScopeFixture()
    {
        HtmlDocument document = Html.Parse(
            "<div id=scope><h1 id=test></h1><p id=p>"
            + "<span id=span>hello</span></p></div>");
        return CreateFixture(document,
            ("scope", document.FindById("scope")),
            ("p", document.FindById("p")));
    }

    static SelectorFixture CreateNamespaceFixture()
    {
        HtmlDocument document = Html.Parse(
            "<main id=root><svg id=thesvg xlink:href=foo>"
            + "<circle id=svg-circle></circle></svg></main>");
        HtmlNode root = document.FindById("root");
        root.Append(CreateElement(document, "", "circle",
            "no-namespace-circle"));
        return CreateFixture(document, ("root", root));
    }

    static SelectorFixture CreateCaseFixture()
    {
        HtmlDocument document = Html.Parse("<main id=case-root></main>");
        HtmlNode root = document.FindById("case-root");
        HtmlNode input = CreateElement(document, "input", "testInput");
        input.SetAttribute("name", "User");
        HtmlNode htmlCase = CreateElement(document, "div", "htmlCase");
        htmlCase.SetAttribute("viewBox", "html");
        HtmlNode svgExact = CreateElement(document, SvgNamespace, "rect",
            "svgExact");
        svgExact.SetAttribute("viewBox", "exact");
        HtmlNode svgLower = CreateElement(document, SvgNamespace, "rect",
            "svgLower");
        svgLower.SetAttribute("viewbox", "lower");
        root.Append(input);
        root.Append(htmlCase);
        root.Append(svgExact);
        root.Append(svgLower);
        return CreateFixture(document, ("case-root", root));
    }

    static SelectorFixture CreateEscapeFixture()
    {
        HtmlDocument document = Html.Parse("<main id=escape-root></main>");
        HtmlNode root = document.FindById("escape-root");
        root.Append(CreateElement(document, "div", "0nextIsWhiteSpace"));
        root.Append(CreateElement(document, "div", ".comma"));
        root.Append(CreateElement(document, "div", "🔑nonBMP"));
        root.Append(CreateElement(document, "div", "hello"));
        HtmlNode classColon = CreateElement(document, "div", "class-colon");
        classColon.SetAttribute("class", "foo:bar");
        root.Append(classColon);
        root.Append(CreateElement(document, "div", "test.foo[5]bar"));
        root.Append(CreateElement(document, "div", "null�"));
        return CreateFixture(document, ("escape-root", root));
    }

    static SelectorFixture CreateGeneralFixture()
    {
        HtmlDocument document = Html.Parse(
            "<main id=general-root>"
            + "<div id=id-div1></div><div id=id-div2></div>"
            + "<ul><li id=id-li-duplicate></li>"
            + "<li id=id-li-duplicate></li><li id=id-li-duplicate></li>"
            + "<li id=id-li-duplicate></li></ul>"
            + "<section id=universal><p id=universal-p1>"
            + "<code id=universal-code1></code></p>"
            + "<hr id=universal-hr1><pre id=universal-pre1>"
            + "<span id=universal-span1></span></pre></section>"
            + "<div id=attr-value><div id=attr-value-div1 "
            + "align=center></div></div></main>");
        return CreateFixture(document,
            ("general-root", document.FindById("general-root")));
    }

    static SelectorFixture CreateFixture(HtmlDocument document,
        params (string Name, HtmlNode Node)[] contexts)
    {
        Dictionary<string, HtmlNode> result = contexts.ToDictionary(
            value => value.Name, value => value.Node);
        result["document"] = document.GetRoot();
        return new SelectorFixture(result);
    }

    static HtmlNode CreateElement(HtmlDocument document, string name,
        string id)
    {
        HtmlNode result = document.CreateElement(name);
        result.SetAttribute("id", id);
        return result;
    }

    static HtmlNode CreateElement(HtmlDocument document, string namespaceUri,
        string name, string id)
    {
        HtmlNode result = document.CreateElementInNamespace(namespaceUri, name);
        result.SetAttribute("id", id);
        return result;
    }

    static HtmlSelector Compile(string source)
    {
        HtmlSelectorCompileResult result = HtmlSelectors.Compile(source);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetSelector();
    }

    public sealed class SelectorCase
    {
        public SelectorCase(string fixture, string context, string selector,
            string[] expectedIds)
        {
            this.Fixture = fixture;
            this.Context = context;
            this.Selector = selector;
            this.ExpectedIds = expectedIds;
        }

        public string Fixture { get; }
        public string Context { get; }
        public string Selector { get; }
        public string[] ExpectedIds { get; }
    }

    sealed class SelectorFixture
    {
        public SelectorFixture(Dictionary<string, HtmlNode> contexts)
        {
            this.Contexts = contexts;
        }

        public Dictionary<string, HtmlNode> Contexts { get; }
    }
}
