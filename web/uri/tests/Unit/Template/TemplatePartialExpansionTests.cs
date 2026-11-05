namespace uri.Tests;

public class TemplatePartialExpansionTests
{
    [Test]
    public void MissingVariablesPreserveExpressionsForLaterExpansion()
    {
        UriTemplate template = Parse("café/{x}{?y}");

        string partial = TemplateExpanderCore.ExpandPartial(
            template,
            new UriTemplateBinding[0],
            new UriTemplateExpansionOptions());
        UriTemplateParseResult reparsed = TemplateParserCore.ParseTemplate(
            partial.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(partial, Is.EqualTo("caf%C3%A9/{x}{?y}"));
            Assert.That(reparsed.Success, Is.True);
        });
    }

    [Test]
    public void MixedExpressionsRemainAtomicAndRepeatExpansionIsStable()
    {
        UriTemplate template = Parse("{x,y}/{z}");
        UriTemplateBinding[] firstBindings = new UriTemplateBinding[]
        {
            ScalarBinding("x", "one"),
            ScalarBinding("z", "ok")
        };

        string partial = TemplateExpanderCore.ExpandPartial(
            template,
            firstBindings,
            new UriTemplateExpansionOptions());
        string repeated = TemplateExpanderCore.ExpandStrict(
            Parse(partial),
            new UriTemplateBinding[]
            {
                ScalarBinding("x", "one"),
                ScalarBinding("y", "two")
            },
            new UriTemplateExpansionOptions());
        string direct = TemplateExpanderCore.ExpandStrict(
            template,
            new UriTemplateBinding[]
            {
                ScalarBinding("x", "one"),
                ScalarBinding("y", "two"),
                ScalarBinding("z", "ok")
            },
            new UriTemplateExpansionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(partial, Is.EqualTo("{x,y}/ok"));
            Assert.That(repeated, Is.EqualTo("one,two/ok"));
            Assert.That(repeated, Is.EqualTo(direct));
        });
    }

    [Test]
    public void UnresolvedExpressionsPreserveEveryOperatorAndModifier()
    {
        string source =
            "{var}{+var}{#var}{.var}{/var}{;var}{?var}{&var}{var:2}{var*}";
        UriTemplate template = Parse(source);

        string partial = TemplateExpanderCore.ExpandPartial(
            template,
            new UriTemplateBinding[0],
            new UriTemplateExpansionOptions());

        Assert.That(partial, Is.EqualTo(source));
        Assert.That(
            TemplateParserCore.ParseTemplate(partial.AsSpan()).Success,
            Is.True);
    }

    [Test]
    public void UndefinedValuesRemainUnresolvedWhileDefinedEmptyValuesExpand()
    {
        UriTemplate template = Parse(
            "A{x}B{empty}C{list}D{map}E");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            new UriTemplateBinding("x", new UriTemplateValue()),
            ScalarBinding("empty", ""),
            ListBinding("list", new string[0]),
            MapBinding("map", new UriTemplateMapEntry[0])
        };

        string partial = TemplateExpanderCore.ExpandPartial(
            template,
            bindings,
            new UriTemplateExpansionOptions());

        Assert.That(partial, Is.EqualTo("A{x}BCDE"));
    }

    [Test]
    public void FullyResolvedExpressionsUseScalarListAndMapExpansion()
    {
        UriTemplate template = Parse("{x}{/list*}{?map*}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("x", "a b"),
            ListBinding("list", new string[] { "one", "two" }),
            MapBinding("map", new UriTemplateMapEntry[]
            {
                new UriTemplateMapEntry("first", true, "1"),
                new UriTemplateMapEntry("second", true, "2")
            })
        };

        string partial = TemplateExpanderCore.ExpandPartial(
            template,
            bindings,
            new UriTemplateExpansionOptions());
        string strict = TemplateExpanderCore.ExpandStrict(
            template,
            bindings,
            new UriTemplateExpansionOptions());

        Assert.That(partial, Is.EqualTo("a%20b/one/two?first=1&second=2"));
        Assert.That(partial, Is.EqualTo(strict));
    }

    [Test]
    public void DeferredExpressionsDoNotValidateOrConsumeBoundValues()
    {
        string malformed = "x" + new string('\ud800', 1);
        UriTemplate template = Parse("{bad,list,missing}");
        UriTemplateBinding[] deferredBindings = new UriTemplateBinding[]
        {
            ScalarBinding("bad", malformed),
            ListBinding("list", new string[] { "one", "two" })
        };
        UriTemplateExpansionOptions options = Options(100, 1);

        string partial = TemplateExpanderCore.ExpandPartial(
            template,
            deferredBindings,
            options);
        UriTemplateExpansionException exception = Assert.Throws<
            UriTemplateExpansionException>(() =>
                TemplateExpanderCore.ExpandPartial(
                    template,
                    new UriTemplateBinding[]
                    {
                        ScalarBinding("bad", malformed),
                        ListBinding(
                            "list",
                            new string[] { "one", "two" }),
                        ScalarBinding("missing", "value")
                    },
                    options));

        Assert.Multiple(() =>
        {
            Assert.That(partial, Is.EqualTo("{bad,list,missing}"));
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidUnicode));
        });
    }

    [Test]
    public void RetainedExpressionsAreIncludedInTheOutputLimit()
    {
        UriTemplate template = Parse("a{missing}");

        string exact = TemplateExpanderCore.ExpandPartial(
            template,
            new UriTemplateBinding[0],
            Options(10, 0));
        UriTemplateExpansionException exception = Assert.Throws<
            UriTemplateExpansionException>(() =>
                TemplateExpanderCore.ExpandPartial(
                    template,
                    new UriTemplateBinding[0],
                    Options(9, 0)));

        Assert.Multiple(() =>
        {
            Assert.That(exact, Is.EqualTo("a{missing}"));
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.LimitExceeded));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(
                UriComponent.Output));
            Assert.That(exception.Diagnostic.Range.Offset, Is.EqualTo(9));
            Assert.That(exception.Diagnostic.Range.Length, Is.EqualTo(1));
        });
    }

    private static UriTemplate Parse(string text)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());
        Assert.That(result.Success, Is.True);
        return result.Template;
    }

    private static UriTemplateExpansionOptions Options(
        int outputCharacters,
        int compositeMembers)
    {
        UriTemplateExpansionOptions options = new UriTemplateExpansionOptions();
        options.Limits.MaxOutputCharacters = outputCharacters;
        options.Limits.MaxTemplateCompositeMembers = compositeMembers;
        return options;
    }

    private static UriTemplateBinding ScalarBinding(string name, string value)
    {
        UriTemplateValue scalar = new UriTemplateValue();
        scalar.Kind = UriTemplateValueKind.Scalar;
        scalar.Scalar = value;
        return new UriTemplateBinding(name, scalar);
    }

    private static UriTemplateBinding ListBinding(
        string name,
        string[] members)
    {
        UriTemplateValue list = new UriTemplateValue();
        list.Kind = UriTemplateValueKind.List;
        list.List = members;
        return new UriTemplateBinding(name, list);
    }

    private static UriTemplateBinding MapBinding(
        string name,
        UriTemplateMapEntry[] members)
    {
        UriTemplateValue map = new UriTemplateValue();
        map.Kind = UriTemplateValueKind.Map;
        map.Map = members;
        return new UriTemplateBinding(name, map);
    }
}
