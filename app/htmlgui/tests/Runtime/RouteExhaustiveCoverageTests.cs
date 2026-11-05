using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class RouteExhaustiveCoverageTests
{
    [TestCase("string", "hello", UiValueKind.String)]
    [TestCase("bool", "FALSE", UiValueKind.Boolean)]
    [TestCase("int", "-2147483648", UiValueKind.Integer)]
    [TestCase("long", "2147483648", UiValueKind.Integer)]
    [TestCase("decimal", "1234.50", UiValueKind.Decimal)]
    [TestCase("double", "1.25e2", UiValueKind.FloatingPoint)]
    [TestCase("float", "3.5", UiValueKind.FloatingPoint)]
    [TestCase("guid", "A0B1C2D3-E4F5-4678-9012-ABCDEF123456", UiValueKind.String)]
    [TestCase("datetime", "2024-02-29T23:59:58Z", UiValueKind.DateTime)]
    public void MatchesEverySupportedRouteConstraint(string constraint, string supplied, UiValueKind expectedKind)
    {
        RouteTemplateCore templates = new RouteTemplateCore();
        UiRoute route = Route("value", "/value/{item:" + constraint + "}");
        RouteUrlResult url = new RouteCore().ParseRouteUrl("/value/" + supplied, "/");

        RouteMatchResult result = templates.MatchRouteTemplate(route, url);
        UiValue value = result.RouteData!.Parameters.Single().Value;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Matched, Is.True);
            Assert.That(value.Kind, Is.EqualTo(expectedKind));
            Assert.That(ConstraintValueText(value), Is.EqualTo(ExpectedConstraintValue(constraint, supplied)));
        });
    }

    [TestCase("bool", "yes")]
    [TestCase("int", "2147483648")]
    [TestCase("long", "9223372036854775808")]
    [TestCase("decimal", "1e999")]
    [TestCase("double", "NaN")]
    [TestCase("float", "3.5.1")]
    [TestCase("guid", "not-a-guid")]
    [TestCase("datetime", "2023-02-29")]
    public void RejectsInvalidValuesForEveryRestrictiveConstraint(string constraint, string supplied)
    {
        UiRoute route = Route("value", "/value/{item:" + constraint + "}");
        RouteMatchResult result = new RouteTemplateCore().MatchRouteTemplate(route, new RouteCore().ParseRouteUrl("/value/" + supplied, "/"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Matched, Is.False);
            Assert.That(result.RouteData, Is.Null);
        });
    }

    [TestCase("/value/{item=hello%20world}", UiValueKind.String, "hello world")]
    [TestCase("/value/{item:bool=true}", UiValueKind.Boolean, "true")]
    [TestCase("/value/{item:int=-12}", UiValueKind.Integer, "-12")]
    [TestCase("/value/{item:long=2147483648}", UiValueKind.Integer, "2147483648")]
    [TestCase("/value/{item:decimal=12.50}", UiValueKind.Decimal, "12.5")]
    [TestCase("/value/{item:double=1.25e2}", UiValueKind.FloatingPoint, "125")]
    [TestCase("/value/{item:float=3.5}", UiValueKind.FloatingPoint, "3.5")]
    [TestCase("/value/{item:guid=A0B1C2D3-E4F5-4678-9012-ABCDEF123456}", UiValueKind.String, "a0b1c2d3-e4f5-4678-9012-abcdef123456")]
    [TestCase("/value/{item:datetime=2024-02-29}", UiValueKind.DateTime, "2024-02-29")]
    public void AppliesAndConvertsEverySupportedDefaultValue(string template, UiValueKind expectedKind, string expectedValue)
    {
        RouteTemplateCore templates = new RouteTemplateCore();
        RouteTemplateResult parsed = templates.ParseRouteTemplate(template);
        RouteMatchResult matched = templates.MatchRouteTemplate(Route("default", template), new RouteCore().ParseRouteUrl("/value", "/"));
        UiValue value = matched.RouteData!.Parameters.Single().Value;

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Segments.Last().IsOptional, Is.True);
            Assert.That(matched.Matched, Is.True);
            Assert.That(value.Kind, Is.EqualTo(expectedKind));
            Assert.That(ConstraintValueText(value), Is.EqualTo(expectedValue));
        });
    }

    [Test]
    public void DistinguishesRequiredOptionalDefaultAndCatchAllSegmentMatching()
    {
        RouteCore urls = new RouteCore();
        RouteTemplateCore templates = new RouteTemplateCore();
        RouteMatchResult literal = templates.MatchRouteTemplate(Route("literal", "/fixed"), urls.ParseRouteUrl("/fixed", "/"));
        RouteMatchResult requiredMissing = templates.MatchRouteTemplate(Route("required", "/{value}"), urls.ParseRouteUrl("/", "/"));
        RouteMatchResult optionalMissing = templates.MatchRouteTemplate(Route("optional", "/{value?}"), urls.ParseRouteUrl("/", "/"));
        RouteMatchResult defaultMissing = templates.MatchRouteTemplate(Route("default", "/{value=fallback}"), urls.ParseRouteUrl("/", "/"));
        RouteMatchResult catchAllMany = templates.MatchRouteTemplate(Route("catch", "/files/{*path}"), urls.ParseRouteUrl("/files/one/two/three", "/"));
        RouteMatchResult literalCaseMismatch = templates.MatchRouteTemplate(Route("case", "/Fixed"), urls.ParseRouteUrl("/fixed", "/"));

        Assert.Multiple(() =>
        {
            Assert.That(literal.Matched, Is.True);
            Assert.That(requiredMissing.Matched, Is.False);
            Assert.That(optionalMissing.RouteData!.Parameters.Single().Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(defaultMissing.RouteData!.Parameters.Single().Value.StringValue, Is.EqualTo("fallback"));
            Assert.That(catchAllMany.RouteData!.Parameters.Single().Value.StringValue, Is.EqualTo("one/two/three"));
            Assert.That(literalCaseMismatch.Matched, Is.False);
        });
    }

    [Test]
    public void DecodesAndCanonicalizesEncodedLiteralsParametersDefaultsQueriesAndFragments()
    {
        RouteTemplateCore templates = new RouteTemplateCore();
        RouteTemplateResult parsed = templates.ParseRouteTemplate("/caf%C3%A9/%7Bguide%7D/{name=hello%20world}");
        UiRoute route = Route("encoded", "/caf%C3%A9/%7Bguide%7D/{name=hello%20world}");
        RouteUrlResult suppliedUrl = new RouteCore().ParseRouteUrl("/caf%C3%A9/%7Bguide%7D/%F0%9F%98%80?q=%E2%9C%93#part%20one", "/");
        RouteMatchResult supplied = templates.MatchRouteTemplate(route, suppliedUrl);
        RouteMatchResult defaulted = templates.MatchRouteTemplate(route, new RouteCore().ParseRouteUrl("/caf%C3%A9/%7Bguide%7D", "/"));

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.CanonicalTemplate, Is.EqualTo("/caf%C3%A9/%7Bguide%7D/{name=hello%20world}"));
            Assert.That(parsed.Segments[0].Value, Is.EqualTo("café"));
            Assert.That(parsed.Segments[1].Value, Is.EqualTo("{guide}"));
            Assert.That(supplied.Matched, Is.True);
            Assert.That(supplied.RouteData!.Parameters.Single().Value.StringValue, Is.EqualTo("😀"));
            Assert.That(supplied.RouteData.Query.Single().Value.StringValue, Is.EqualTo("✓"));
            Assert.That(supplied.RouteData.Fragment, Is.EqualTo("part one"));
            Assert.That(defaulted.RouteData!.Parameters.Single().Value.StringValue, Is.EqualTo("hello world"));
        });
    }

    [Test]
    public void AppliesCanonicalBasePathsAtRootExactRelativeAndOutsideBoundaries()
    {
        RouteCore core = new RouteCore();
        RouteUrlResult encodedBase = core.CanonicalizeRouteBasePath("/%63atalog//app");
        RouteUrlResult root = core.ParseRouteUrl("/", "/");
        RouteUrlResult exact = core.ParseRouteUrl("/catalog/app", "/catalog/app/");
        RouteUrlResult relative = core.ParseRouteUrl("items/../products?view=grid", "/catalog/app/");
        RouteUrlResult queryOnly = core.ParseRouteUrl("?view=list#top", "/catalog/app/");
        RouteUrlResult sibling = core.ParseRouteUrl("/catalog/application", "/catalog/app/");

        Assert.Multiple(() =>
        {
            Assert.That(encodedBase.Success, Is.True);
            Assert.That(encodedBase.BasePath, Is.EqualTo("/catalog/app/"));
            Assert.That(root.CanonicalUrl, Is.EqualTo("/"));
            Assert.That(exact.Path, Is.EqualTo("/"));
            Assert.That(exact.CanonicalUrl, Is.EqualTo("/catalog/app/"));
            Assert.That(relative.Path, Is.EqualTo("/products"));
            Assert.That(relative.CanonicalUrl, Is.EqualTo("/catalog/app/products?view=grid"));
            Assert.That(queryOnly.Path, Is.EqualTo("/"));
            Assert.That(queryOnly.CanonicalUrl, Is.EqualTo("/catalog/app/?view=list#top"));
            Assert.That(sibling.Code, Is.EqualTo("route.outside-base-path"));
        });
    }

    [Test]
    public void ResolvesLiteralConstrainedParameterAndCatchAllPrecedenceAtRuntime()
    {
        RouteResolverCore resolver = new RouteResolverCore();
        UiRoute[] routes = new UiRoute[]
        {
            Route("catch", "/items/{*value}"),
            Route("parameter", "/items/{value}"),
            Route("integer", "/items/{value:int}"),
            Route("literal", "/items/new")
        };

        RouteResolutionResult literal = resolver.ResolveRouteTree(routes, null, null, "/items/new", "/");
        RouteResolutionResult integer = resolver.ResolveRouteTree(routes, null, null, "/items/42", "/");
        RouteResolutionResult parameter = resolver.ResolveRouteTree(routes, null, null, "/items/word", "/");
        RouteResolutionResult catchAll = resolver.ResolveRouteTree(routes, null, null, "/items/one/two", "/");

        Assert.Multiple(() =>
        {
            Assert.That(literal.RouteData!.RouteId, Is.EqualTo("literal"));
            Assert.That(integer.RouteData!.RouteId, Is.EqualTo("integer"));
            Assert.That(parameter.RouteData!.RouteId, Is.EqualTo("parameter"));
            Assert.That(catchAll.RouteData!.RouteId, Is.EqualTo("catch"));
            Assert.That(catchAll.RouteData.Parameters.Single().Value.StringValue, Is.EqualTo("one/two"));
        });
    }

    [Test]
    public void DetectsAmbiguityForEveryOverlappingSegmentShapeAndNestedEffectivePath()
    {
        RouteTemplateCore templates = new RouteTemplateCore();
        RouteCollectionResult required = templates.ValidateRouteCollection(new UiRoute[] { Route("a", "/{first}"), Route("b", "/{second}") });
        RouteCollectionResult optional = templates.ValidateRouteCollection(new UiRoute[] { Route("a", "/{first?}"), Route("b", "/{second?}") });
        RouteCollectionResult defaults = templates.ValidateRouteCollection(new UiRoute[] { Route("a", "/{first=one}"), Route("b", "/{second=two}") });
        RouteCollectionResult catches = templates.ValidateRouteCollection(new UiRoute[] { Route("a", "/{*first}"), Route("b", "/{*second}") });
        RouteCollectionResult stringBoolean = templates.ValidateRouteCollection(new UiRoute[] { Route("a", "/{value:string}"), Route("b", "/{value:bool}") });
        RouteCollectionResult disjoint = templates.ValidateRouteCollection(new UiRoute[] { Route("bool", "/{value:bool}"), Route("guid", "/{value:guid}"), Route("date", "/{value:datetime}") });
        UiRoute parent = Route("parent", "/root");
        UiRoute firstChild = Route("first-child", "/{id}");
        firstChild.ParentId = parent.Id;
        UiRoute secondChild = Route("second-child", "/{name}");
        secondChild.ParentId = parent.Id;
        RouteDefinitionResult nested = new RouteResolverCore().PrepareRouteDefinitions(new UiRoute[] { parent, firstChild, secondChild });

        Assert.Multiple(() =>
        {
            Assert.That(required.Code, Is.EqualTo("route.ambiguous"));
            Assert.That(optional.Code, Is.EqualTo("route.ambiguous"));
            Assert.That(defaults.Code, Is.EqualTo("route.ambiguous"));
            Assert.That(catches.Code, Is.EqualTo("route.ambiguous"));
            Assert.That(stringBoolean.Code, Is.EqualTo("route.ambiguous"));
            Assert.That(disjoint.Success, Is.True);
            Assert.That(nested.Code, Is.EqualTo("route.ambiguous"));
            Assert.That(nested.Success, Is.False);
        });
    }

    [Test]
    public void RejectsNullInvalidAndResourceBoundParserAndMatcherInputs()
    {
        RouteCore urls = new RouteCore();
        RouteTemplateCore templates = new RouteTemplateCore();
        string tooLong = "/" + new string('a', RouteCore.MaximumRouteUrlLength);
        string[] manySegments = Enumerable.Repeat("x", RouteCore.MaximumRouteSegmentCount + 1).ToArray();
        string tooMany = "/" + string.Join("/", manySegments);
        RouteUrlResult validUrl = urls.ParseRouteUrl("/value", "/");
        RouteUrlResult externalUrl = urls.ParseRouteUrl("https://example.test/value", "/");

        Assert.Multiple(() =>
        {
            Assert.That(urls.ParseRouteUrl(null, "/").Code, Is.EqualTo("route.invalid-url"));
            Assert.That(urls.ParseRouteUrl(string.Empty, "/").Code, Is.EqualTo("route.invalid-url"));
            Assert.That(urls.ParseRouteUrl(tooLong, "/").Code, Is.EqualTo("route.invalid-url"));
            Assert.That(urls.ParseRouteUrl(tooMany, "/").Code, Is.EqualTo("route.too-many-segments"));
            Assert.That(templates.ParseRouteTemplate(null).Code, Is.EqualTo("route.invalid-template"));
            Assert.That(templates.ParseRouteTemplate(tooLong).Code, Is.EqualTo("route.invalid-template"));
            Assert.That(templates.ParseRouteTemplate(tooMany).Code, Is.EqualTo("route.too-many-template-segments"));
            Assert.That(templates.MatchRouteTemplate(null, validUrl).Code, Is.EqualTo("route.null-route"));
            Assert.That(templates.MatchRouteTemplate(Route("route", "/value"), null).Code, Is.EqualTo("route.invalid-match-url"));
            Assert.That(templates.MatchRouteTemplate(Route("route", "/value"), externalUrl).Code, Is.EqualTo("route.invalid-match-url"));
            Assert.That(templates.MatchRouteTemplate(Route(string.Empty, "/value"), validUrl).Code, Is.EqualTo("route.invalid-id"));
            Assert.That(templates.ValidateRouteCollection(null).Code, Is.EqualTo("route.null-routes"));
            Assert.That(templates.ValidateRouteCollection(new UiRoute[] { null! }).Code, Is.EqualTo("route.null-route"));
        });
    }

    [Test]
    public void FillsADeeplyNestedNamedOutletWithoutMutatingTheRegisteredLayout()
    {
        UiNode outlet = new UiNode { Kind = UiNodeKind.Slot, Name = "detail" };
        UiNode layoutNode = new UiNode { Kind = UiNodeKind.Layout, Layout = new UiLayout { Children = new UiNode[] { outlet } } };
        UiNode conditionalNode = new UiNode
        {
            Kind = UiNodeKind.Conditional,
            Conditional = new UiConditional { WhenTrue = new UiNode[] { layoutNode } }
        };
        UiRoute parent = Route("parent", "/workspace");
        parent.View = new UiView { Key = "shell", ContentRoot = new UiNode { Kind = UiNodeKind.Fragment, Children = new UiNode[] { conditionalNode } } };
        UiRoute child = Route("child", "/report");
        child.ParentId = parent.Id;
        child.OutletName = "detail";
        child.View = new UiView { Key = "report", ContentRoot = new UiNode { Kind = UiNodeKind.Text, Text = "Report" } };

        RouteResolutionResult result = new RouteResolverCore().ResolveRouteTree(new UiRoute[] { child, parent }, null, null, "/workspace/report", "/");
        UiNode filled = result.View!.ContentRoot!.Children[0].Conditional!.WhenTrue[0].Layout!.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(filled.Kind, Is.EqualTo(UiNodeKind.Text));
            Assert.That(filled.Text, Is.EqualTo("Report"));
            Assert.That(outlet.Kind, Is.EqualTo(UiNodeKind.Slot));
            Assert.That(outlet.Name, Is.EqualTo("detail"));
        });
    }

    [Test]
    public void SelectsNestedNotFoundOnlyWhenItsConstrainedParentPathMatches()
    {
        UiRoute teams = Route("teams", "/teams/{teamId:int}");
        teams.View = LayoutView("team-shell", "content");
        UiRoute teamMissing = Route("team-missing", "/");
        teamMissing.ParentId = teams.Id;
        teamMissing.OutletName = "content";
        teamMissing.IsNotFound = true;
        teamMissing.View = TextView("Team page missing");
        UiRoute globalMissing = Route("global-missing", "/");
        globalMissing.IsNotFound = true;
        globalMissing.View = TextView("Page missing");
        RouteResolverCore resolver = new RouteResolverCore();

        RouteResolutionResult nested = resolver.ResolveRouteTree(new UiRoute[] { globalMissing, teamMissing, teams }, null, null, "/teams/12/missing/%E2%9C%93?from=link#details", "/");
        RouteResolutionResult global = resolver.ResolveRouteTree(new UiRoute[] { teams, teamMissing, globalMissing }, null, null, "/teams/not-a-number/missing", "/");

        Assert.Multiple(() =>
        {
            Assert.That(nested.Status, Is.EqualTo(UiNavigationStatus.NotFound));
            Assert.That(nested.RouteData!.RouteId, Is.EqualTo("team-missing"));
            Assert.That(nested.RouteData.Parameters.Single().Value.IntegerValue, Is.EqualTo(12));
            Assert.That(nested.RouteData.Query.Single().Value.StringValue, Is.EqualTo("link"));
            Assert.That(nested.RouteData.Fragment, Is.EqualTo("details"));
            Assert.That(nested.View!.ContentRoot!.Children[0].Text, Is.EqualTo("Team page missing"));
            Assert.That(global.RouteData!.RouteId, Is.EqualTo("global-missing"));
            Assert.That(global.View!.ContentRoot!.Text, Is.EqualTo("Page missing"));
        });
    }

    private static UiRoute Route(string id, string template)
    {
        return new UiRoute { Id = id, Template = template };
    }

    private static string ConstraintValueText(UiValue value)
    {
        switch (value.Kind)
        {
            case UiValueKind.Boolean:
                return value.BooleanValue ? "true" : "false";
            case UiValueKind.Integer:
                return value.IntegerValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case UiValueKind.Decimal:
                return value.DecimalValue.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);
            case UiValueKind.FloatingPoint:
                return value.FloatingPointValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            case UiValueKind.DateTime:
                return value.DateTimeValue ?? string.Empty;
            default:
                return value.StringValue ?? string.Empty;
        }
    }

    private static string ExpectedConstraintValue(string constraint, string supplied)
    {
        if (constraint == "bool")
        {
            return supplied.ToLowerInvariant();
        }

        if (constraint == "decimal")
        {
            return "1234.5";
        }

        if (constraint == "double")
        {
            return "125";
        }

        if (constraint == "guid")
        {
            return supplied.ToLowerInvariant();
        }

        return supplied;
    }

    private static UiView LayoutView(string key, string outlet)
    {
        return new UiView
        {
            Key = key,
            ContentRoot = new UiNode
            {
                Kind = UiNodeKind.Fragment,
                Children = new UiNode[] { new UiNode { Kind = UiNodeKind.Slot, Name = outlet } }
            }
        };
    }

    private static UiView TextView(string text)
    {
        return new UiView { Key = text, ContentRoot = new UiNode { Kind = UiNodeKind.Text, Text = text } };
    }
}
