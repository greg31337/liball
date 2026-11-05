using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorMarkupPseudoTests
{
    [Test]
    public void ParsesMarkupObservablePseudoClassesAndCssEscapes()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            @":any-link,:link,:enabled,:disabled,:checked,"
            + @":indeterminate,:required,:optional,:read-only,"
            + @":read-write,:placeholder-shown,:def\61 ult").Parse();
        HtmlSelectorPseudoClassKind[] kinds = result.GetSelectorList()
            .GetSelectors().Select(GetOnlySimple)
            .Select(value => value.GetPseudoClassKind()).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(kinds, Is.EqualTo(new[]
            {
                HtmlSelectorPseudoClassKind.AnyLink,
                HtmlSelectorPseudoClassKind.Link,
                HtmlSelectorPseudoClassKind.Enabled,
                HtmlSelectorPseudoClassKind.Disabled,
                HtmlSelectorPseudoClassKind.Checked,
                HtmlSelectorPseudoClassKind.Indeterminate,
                HtmlSelectorPseudoClassKind.Required,
                HtmlSelectorPseudoClassKind.Optional,
                HtmlSelectorPseudoClassKind.ReadOnly,
                HtmlSelectorPseudoClassKind.ReadWrite,
                HtmlSelectorPseudoClassKind.PlaceholderShown,
                HtmlSelectorPseudoClassKind.Default
            }));
        });
    }

    [Test]
    public void ParsesLanguageRangesAndDirectionIdentifiers()
    {
        string source = @" :l\61 ng(en, ""*-Hant"", """", und)"
            + @":d\69r(RTL) ";
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(source)
            .Parse();
        HtmlSimpleSelectorAst[] simple = result.GetSelectorList()
            .GetSelectors()[0].GetCompounds()[0].GetSimpleSelectors();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(simple[0].GetPseudoClassKind(),
                Is.EqualTo(HtmlSelectorPseudoClassKind.Lang));
            Assert.That(simple[0].GetPseudoClassArguments(), Is.EqualTo(
                new[] { "en", "*-Hant", "", "und" }));
            Assert.That(simple[1].GetPseudoClassKind(),
                Is.EqualTo(HtmlSelectorPseudoClassKind.Dir));
            Assert.That(simple[1].GetPseudoClassArguments(),
                Is.EqualTo(new[] { "RTL" }));
            Assert.That(simple[0].GetSourceSpan().GetStart()
                .GetCharacterOffset(), Is.EqualTo(1));
            Assert.That(simple[1].GetSourceSpan().GetEnd()
                .GetCharacterOffset(), Is.EqualTo(source.LastIndexOf(')') + 1));
        });
    }

    [TestCase(":lang()")]
    [TestCase(":lang(en,)")]
    [TestCase(":lang(,en)")]
    [TestCase(":lang(*)")]
    [TestCase(":lang(en us)")]
    [TestCase(":dir()")]
    [TestCase(":dir(ltr,rtl)")]
    [TestCase(":dir(\"ltr\")")]
    [TestCase(":dir(ltr")]
    public void RejectsMalformedLanguageAndDirectionArguments(
        string selector)
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(selector)
            .Parse();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetSelectorList(), Is.Null);
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(result.GetDiagnostics()[0].GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.SelectorInvalidSyntax));
        });
    }

    [TestCase("::before", "Pseudo-elements are not supported.")]
    [TestCase(":before", "Pseudo-elements are not supported.")]
    [TestCase(":hover", "This pseudo-class depends on browser or user-agent state and is not supported.")]
    [TestCase(":focus-within", "This pseudo-class depends on browser or user-agent state and is not supported.")]
    [TestCase(":visited", "This pseudo-class depends on browser or user-agent state and is not supported.")]
    [TestCase(":target", "This pseudo-class depends on browser or user-agent state and is not supported.")]
    [TestCase(":valid", "This pseudo-class depends on browser or user-agent state and is not supported.")]
    [TestCase(":state(open)", "This pseudo-class depends on browser or user-agent state and is not supported.")]
    public void RejectsPseudoElementsAndBrowserStateWithStableDiagnostic(
        string selector, string message)
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(selector)
            .Parse();
        HtmlDiagnostic diagnostic = result.GetDiagnostics()[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetSelectorList(), Is.Null);
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(diagnostic.GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.SelectorUnsupportedFeature));
            Assert.That(diagnostic.GetIdentifier(), Is.EqualTo("UH05002"));
            Assert.That(diagnostic.GetSeverity(),
                Is.EqualTo(HtmlDiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Is.EqualTo(message));
        });
    }

    [Test]
    public void ArgumentCollectionsAreSnapshots()
    {
        HtmlSimpleSelectorAst lang = ParsePseudo(":lang(en,fr)");
        string[] arguments = lang.GetPseudoClassArguments();
        arguments[0] = "changed";

        Assert.That(lang.GetPseudoClassArguments(),
            Is.EqualTo(new[] { "en", "fr" }));
    }

    [Test]
    public void ArgumentAstRejectsInvalidStates()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                HtmlSimpleSelectorAst.CreatePseudoClass(
                    HtmlSelectorPseudoClassKind.Lang, null, span));
            Assert.Throws<ArgumentException>(() =>
                HtmlSimpleSelectorAst.CreateArgumentPseudoClass(
                    HtmlSelectorPseudoClassKind.Root,
                    new[] { "value" }, span));
            Assert.Throws<ArgumentException>(() =>
                HtmlSimpleSelectorAst.CreateArgumentPseudoClass(
                    HtmlSelectorPseudoClassKind.Lang,
                    new string[0], span));
            Assert.Throws<ArgumentException>(() =>
                HtmlSimpleSelectorAst.CreateArgumentPseudoClass(
                    HtmlSelectorPseudoClassKind.Dir,
                    new[] { "ltr", "rtl" }, span));
            Assert.Throws<ArgumentException>(() =>
                HtmlSimpleSelectorAst.CreateArgumentPseudoClass(
                    HtmlSelectorPseudoClassKind.Lang,
                    new string[] { null }, span));
        });
    }

    [Test]
    public void LanguageMatchesInheritedAndExtendedRanges()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode parent = document.CreateElement("section");
        HtmlNode inherited = document.CreateElement("p");
        HtmlNode untagged = document.CreateElement("p");
        HtmlNode undetermined = document.CreateElement("p");
        parent.SetAttribute("lang", "zh-Hant-TW");
        untagged.SetAttribute("lang", "");
        undetermined.SetAttribute("lang", "und");
        parent.InitializeChildrenStorage(new[]
        {
            inherited, untagged, undetermined
        });

        Assert.Multiple(() =>
        {
            Assert.That(Matches(inherited, ":lang(zh)"), Is.True);
            Assert.That(Matches(inherited, ":lang(\"*-hant\")"),
                Is.True);
            Assert.That(Matches(inherited, ":lang(en, ZH-tw)"), Is.True);
            Assert.That(Matches(inherited, ":lang(en)"), Is.False);
            Assert.That(Matches(untagged, ":lang(\"\")"), Is.True);
            Assert.That(Matches(untagged, ":lang(\"*\")"), Is.False);
            Assert.That(Matches(undetermined, ":lang(\"*\")"), Is.True);
        });
    }

    [Test]
    public void DirectionMatchesExplicitInheritedAutoAndTelephoneRules()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode rtl = document.CreateElement("section");
        HtmlNode inherited = document.CreateElement("span");
        HtmlNode ltr = document.CreateElement("span");
        HtmlNode autoRtl = document.CreateElement("div");
        HtmlNode autoLtr = document.CreateElement("div");
        HtmlNode telephone = document.CreateElement("input");
        rtl.SetAttribute("dir", "rtl");
        ltr.SetAttribute("dir", "LTR");
        autoRtl.SetAttribute("dir", "auto");
        autoLtr.SetAttribute("dir", "auto");
        telephone.SetAttribute("type", "tel");
        autoRtl.InitializeChildrenStorage(new[]
        {
            document.CreateText("123 שלום")
        });
        autoLtr.InitializeChildrenStorage(new[]
        {
            document.CreateText("123 hello שלום")
        });
        rtl.InitializeChildrenStorage(new[]
        {
            inherited, ltr, autoRtl, autoLtr, telephone
        });

        Assert.Multiple(() =>
        {
            Assert.That(Matches(inherited, ":dir(rtl)"), Is.True);
            Assert.That(Matches(ltr, ":dir(ltr)"), Is.True);
            Assert.That(Matches(autoRtl, ":dir(rtl)"), Is.True);
            Assert.That(Matches(autoLtr, ":dir(ltr)"), Is.True);
            Assert.That(Matches(telephone, ":dir(ltr)"), Is.True);
            Assert.That(Matches(inherited, ":dir(sideways)"), Is.False);
        });
    }

    [Test]
    public void AnyLinkAndLinkUseDeterministicUnvisitedSemantics()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode anchor = document.CreateElement("a");
        HtmlNode emptyAnchor = document.CreateElement("a");
        HtmlNode area = document.CreateElement("area");
        HtmlNode div = document.CreateElement("div");
        HtmlNode svgAnchor = document.CreateElementInNamespace(
            "http://www.w3.org/2000/svg", "a");
        anchor.SetAttribute("href", "/target");
        area.SetAttribute("href", "");
        div.SetAttribute("href", "/not-a-link");
        svgAnchor.SetAttribute("href", "graphic.svg");

        Assert.Multiple(() =>
        {
            Assert.That(Matches(anchor, ":any-link"), Is.True);
            Assert.That(Matches(anchor, ":link"), Is.True);
            Assert.That(Matches(area, ":link"), Is.True);
            Assert.That(Matches(svgAnchor, ":any-link"), Is.True);
            Assert.That(Matches(emptyAnchor, ":any-link"), Is.False);
            Assert.That(Matches(div, ":link"), Is.False);
        });
    }

    [Test]
    public void DisabledStateIncludesFieldsetLegendAndSelectInheritance()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fieldset = document.CreateElement("fieldset");
        HtmlNode legend = document.CreateElement("legend");
        HtmlNode exempt = document.CreateElement("input");
        HtmlNode disabled = document.CreateElement("input");
        HtmlNode ordinary = document.CreateElement("input");
        HtmlNode nestedFieldset = document.CreateElement("fieldset");
        HtmlNode select = document.CreateElement("select");
        HtmlNode option = document.CreateElement("option");
        HtmlNode optgroup = document.CreateElement("optgroup");
        HtmlNode groupedOption = document.CreateElement("option");
        fieldset.SetAttribute("disabled", "");
        select.SetAttribute("disabled", "");
        optgroup.SetAttribute("disabled", "");
        legend.InitializeChildrenStorage(new[] { exempt });
        optgroup.InitializeChildrenStorage(new[] { groupedOption });
        select.InitializeChildrenStorage(new[] { option, optgroup });
        fieldset.InitializeChildrenStorage(new[]
        {
            legend, disabled, nestedFieldset, select
        });

        Assert.Multiple(() =>
        {
            Assert.That(Matches(exempt, ":enabled"), Is.True);
            Assert.That(Matches(disabled, ":disabled"), Is.True);
            Assert.That(Matches(nestedFieldset, ":disabled"), Is.True);
            Assert.That(Matches(select, ":disabled"), Is.True);
            Assert.That(Matches(option, ":disabled"), Is.True);
            Assert.That(Matches(groupedOption, ":disabled"), Is.True);
            Assert.That(Matches(ordinary, ":enabled"), Is.True);
            Assert.That(Matches(document.CreateElement("div"), ":enabled"),
                Is.False);
        });
    }

    [Test]
    public void CheckedIndeterminateAndDefaultUseMarkupState()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode checkbox = Input(document, "checkbox");
        HtmlNode text = Input(document, "text");
        HtmlNode option = document.CreateElement("option");
        HtmlNode progress = document.CreateElement("progress");
        HtmlNode radios = document.CreateElement("div");
        HtmlNode radioOne = Input(document, "radio");
        HtmlNode radioTwo = Input(document, "radio");
        HtmlNode unnamedRadio = Input(document, "radio");
        checkbox.SetAttribute("checked", "");
        text.SetAttribute("checked", "");
        option.SetAttribute("selected", "");
        radioOne.SetAttribute("name", "group");
        radioTwo.SetAttribute("name", "group");
        radios.InitializeChildrenStorage(new[] { radioOne, radioTwo });

        Assert.Multiple(() =>
        {
            Assert.That(Matches(checkbox, ":checked"), Is.True);
            Assert.That(Matches(checkbox, ":default"), Is.True);
            Assert.That(Matches(text, ":checked"), Is.False);
            Assert.That(Matches(option, ":checked"), Is.True);
            Assert.That(Matches(option, ":default"), Is.True);
            Assert.That(Matches(progress, ":indeterminate"), Is.True);
            Assert.That(Matches(radioOne, ":indeterminate"), Is.True);
            Assert.That(Matches(unnamedRadio, ":indeterminate"), Is.False);
        });
    }

    [Test]
    public void CheckedRadioMakesItsGroupDeterminate()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode parent = document.CreateElement("div");
        HtmlNode uncheckedRadio = Input(document, "radio");
        HtmlNode checkedRadio = Input(document, "radio");
        uncheckedRadio.SetAttribute("name", "group");
        checkedRadio.SetAttribute("name", "group");
        checkedRadio.SetAttribute("checked", "");
        parent.InitializeChildrenStorage(new[]
        {
            uncheckedRadio, checkedRadio
        });

        Assert.That(Matches(uncheckedRadio, ":indeterminate"), Is.False);
    }

    [Test]
    public void RequiredAndOptionalOnlyApplyToSupportingControls()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode required = Input(document, "email");
        HtmlNode optional = Input(document, "text");
        HtmlNode submit = Input(document, "submit");
        HtmlNode select = document.CreateElement("select");
        required.SetAttribute("required", "");
        submit.SetAttribute("required", "");

        Assert.Multiple(() =>
        {
            Assert.That(Matches(required, ":required"), Is.True);
            Assert.That(Matches(required, ":optional"), Is.False);
            Assert.That(Matches(optional, ":optional"), Is.True);
            Assert.That(Matches(select, ":optional"), Is.True);
            Assert.That(Matches(submit, ":required"), Is.False);
            Assert.That(Matches(submit, ":optional"), Is.False);
        });
    }

    [Test]
    public void ReadStateIncludesMutableControlsAndEditableAncestors()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode text = Input(document, "text");
        HtmlNode unknown = Input(document, "future-text");
        HtmlNode readOnly = Input(document, "text");
        HtmlNode checkbox = Input(document, "checkbox");
        HtmlNode editor = document.CreateElement("div");
        HtmlNode editableChild = document.CreateElement("p");
        HtmlNode stopped = document.CreateElement("span");
        readOnly.SetAttribute("readonly", "");
        editor.SetAttribute("contenteditable", "true");
        stopped.SetAttribute("contenteditable", "false");
        editor.InitializeChildrenStorage(new[] { editableChild, stopped });

        Assert.Multiple(() =>
        {
            Assert.That(Matches(text, ":read-write"), Is.True);
            Assert.That(Matches(unknown, ":read-write"), Is.True);
            Assert.That(Matches(readOnly, ":read-only"), Is.True);
            Assert.That(Matches(checkbox, ":read-only"), Is.True);
            Assert.That(Matches(editor, ":read-write"), Is.True);
            Assert.That(Matches(editableChild, ":read-write"), Is.True);
            Assert.That(Matches(stopped, ":read-only"), Is.True);
        });
    }

    [Test]
    public void PlaceholderShownUsesInitialMarkupValue()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode empty = Input(document, "text");
        HtmlNode valued = Input(document, "text");
        HtmlNode checkbox = Input(document, "checkbox");
        HtmlNode emptyTextarea = document.CreateElement("textarea");
        HtmlNode valuedTextarea = document.CreateElement("textarea");
        empty.SetAttribute("placeholder", "hint");
        valued.SetAttribute("placeholder", "hint");
        valued.SetAttribute("value", "present");
        checkbox.SetAttribute("placeholder", "hint");
        emptyTextarea.SetAttribute("placeholder", "hint");
        valuedTextarea.SetAttribute("placeholder", "hint");
        valuedTextarea.InitializeChildrenStorage(new[]
        {
            document.CreateText("present")
        });

        Assert.Multiple(() =>
        {
            Assert.That(Matches(empty, ":placeholder-shown"), Is.True);
            Assert.That(Matches(valued, ":placeholder-shown"), Is.False);
            Assert.That(Matches(checkbox, ":placeholder-shown"), Is.False);
            Assert.That(Matches(emptyTextarea, ":placeholder-shown"),
                Is.True);
            Assert.That(Matches(valuedTextarea, ":placeholder-shown"),
                Is.False);
        });
    }

    [Test]
    public void FirstSubmitButtonIsTheDefaultForItsForm()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode form = document.CreateElement("form");
        HtmlNode command = document.CreateElement("button");
        HtmlNode first = document.CreateElement("button");
        HtmlNode second = Input(document, "submit");
        command.SetAttribute("command", "show-modal");
        form.InitializeChildrenStorage(new[] { command, first, second });

        Assert.Multiple(() =>
        {
            Assert.That(Matches(command, ":default"), Is.False);
            Assert.That(Matches(first, ":default"), Is.True);
            Assert.That(Matches(second, ":default"), Is.False);
        });
    }

    [Test]
    public void MarkupPseudoClassesWorkInsideHas()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode parent = document.CreateElement("div");
        HtmlNode input = Input(document, "text");
        input.SetAttribute("disabled", "");
        parent.InitializeChildrenStorage(new[] { input });
        HtmlSimpleSelectorAst has = ParsePseudo(":has(> input:disabled)");
        HtmlBudget budget = CreateBudget(100);

        bool result = HtmlSelectorMatcher.MatchesHas(parent, has, budget);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(budget.HasExceededLimit(), Is.False);
        });
    }

    [Test]
    public void DirectionTraversalStopsAtTheWorkLimit()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode node = document.CreateElement("div");
        node.SetAttribute("dir", "auto");
        node.InitializeChildrenStorage(new[]
        {
            document.CreateComment("one"),
            document.CreateComment("two")
        });
        HtmlBudget budget = CreateBudget(2);

        bool result = HtmlSelectorMarkupMatcher.Matches(node,
            ParsePseudo(":dir(ltr)"), budget);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(budget.HasExceededLimit(), Is.True);
            Assert.That(budget.GetFirstExceededLimit(),
                Is.EqualTo(HtmlLimitKind.HasWork));
            Assert.That(budget.GetUsed(HtmlLimitKind.HasWork), Is.EqualTo(2));
        });
    }

    static bool Matches(HtmlNode node, string selector)
    {
        HtmlBudget budget = CreateBudget(100000);
        bool result = HtmlSelectorMarkupMatcher.Matches(node,
            ParsePseudo(selector), budget);
        Assert.That(budget.HasExceededLimit(), Is.False);
        return result;
    }

    static HtmlSimpleSelectorAst ParsePseudo(string selector)
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(selector)
            .Parse();
        Assert.That(result.IsSuccess(), Is.True);
        return GetOnlySimple(result.GetSelectorList().GetSelectors()[0]);
    }

    static HtmlSimpleSelectorAst GetOnlySimple(
        HtmlComplexSelectorAst selector)
    {
        HtmlCompoundSelectorAst[] compounds = selector.GetCompounds();
        Assert.That(compounds, Has.Length.EqualTo(1));
        HtmlSimpleSelectorAst[] simple = compounds[0].GetSimpleSelectors();
        Assert.That(simple, Has.Length.EqualTo(1));
        return simple[0];
    }

    static HtmlNode Input(HtmlDocument document, string type)
    {
        HtmlNode result = document.CreateElement("input");
        result.SetAttribute("type", type);
        return result;
    }

    static HtmlBudget CreateBudget(int maximumHasWork)
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumHasWork(maximumHasWork);
        return new HtmlBudget(limits, CancellationToken.None);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("about:blank", "about:blank",
            HtmlDocumentMode.NoQuirks, new HtmlDiagnostic[0],
            HtmlSourceSpan.CreateUnknown());
    }
}
