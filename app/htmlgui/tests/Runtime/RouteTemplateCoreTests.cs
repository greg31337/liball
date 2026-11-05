using System.Globalization;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class RouteTemplateCoreTests
{
    [Test]
    public void ParsesEveryTemplateSegmentFormIntoCanonicalMetadata()
    {
        RouteTemplateCore core = new RouteTemplateCore();
        RouteTemplateResult parsed = core.ParseRouteTemplate("/caf%C3%A9/{category}/{id:INT}/{tab=summary}/{*rest}/");

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.CanonicalTemplate, Is.EqualTo("/caf%C3%A9/{category}/{id:int}/{tab=summary}/{*rest}"));
            Assert.That(parsed.Segments.Select(item => item.Kind), Is.EqualTo(new string[]
            {
                RouteTemplateCore.LiteralSegment,
                RouteTemplateCore.ParameterSegment,
                RouteTemplateCore.ParameterSegment,
                RouteTemplateCore.ParameterSegment,
                RouteTemplateCore.CatchAllSegment
            }));
            Assert.That(parsed.Segments.Select(item => item.Value), Is.EqualTo(new string[] { "café", "category", "id", "tab", "rest" }));
            Assert.That(parsed.Segments[2].Constraint, Is.EqualTo("int"));
            Assert.That(parsed.Segments[2].IsOptional, Is.False);
            Assert.That(parsed.Segments[3].DefaultValue, Is.EqualTo("summary"));
            Assert.That(parsed.Segments[3].IsOptional, Is.True);
            Assert.That(parsed.Segments[4].IsOptional, Is.True);
            Assert.That(core.ParseRouteTemplate("/").Segments, Is.Empty);
        });
    }

    [TestCase("relative", "route.invalid-template")]
    [TestCase("/items?sort=name", "route.invalid-template")]
    [TestCase("/first//second", "route.empty-template-segment")]
    [TestCase("/item/{id}/{id}", "route.duplicate-parameter")]
    [TestCase("/item/{id?}/required", "route.required-after-optional")]
    [TestCase("/files/{*path}/tail", "route.catch-all-not-last")]
    [TestCase("/item/{id:unknown}", "route.unknown-constraint")]
    [TestCase("/item/{id:int=word}", "route.invalid-default")]
    [TestCase("/item/{*path?}", "route.invalid-catch-all-modifier")]
    [TestCase("/item/{id=value?}", "route.conflicting-parameter-modifiers")]
    [TestCase("/item/{bad name}", "route.invalid-parameter-name")]
    [TestCase("/item/{id", "route.invalid-template-brace")]
    [TestCase("/item/../tail", "route.invalid-literal")]
    public void RejectsMalformedOrAmbiguousTemplateGrammar(string template, string code)
    {
        RouteTemplateResult result = new RouteTemplateCore().ParseRouteTemplate(template);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(code));
        });
    }

    [Test]
    public void MatchesDefaultsOptionalsCatchAllAndCopiedUrlData()
    {
        RouteCore urlCore = new RouteCore();
        RouteTemplateCore templateCore = new RouteTemplateCore();
        UiRoute orderRoute = Route("order", "/orders/{id:int}/{tab=summary}");
        RouteUrlResult orderUrl = urlCore.ParseRouteUrl("/app/orders/42?tag=one&tag=two#details", "/app/");
        RouteMatchResult order = templateCore.MatchRouteTemplate(orderRoute, orderUrl);
        UiRoute optionalRoute = Route("optional", "/search/{term?}");
        RouteMatchResult optional = templateCore.MatchRouteTemplate(optionalRoute, urlCore.ParseRouteUrl("/search", "/"));
        UiRoute filesRoute = Route("files", "/files/{*path}");
        RouteMatchResult files = templateCore.MatchRouteTemplate(filesRoute, urlCore.ParseRouteUrl("/files/reports/2026/final", "/"));
        RouteMatchResult emptyCatchAll = templateCore.MatchRouteTemplate(filesRoute, urlCore.ParseRouteUrl("/files", "/"));
        RouteMatchResult constraintMismatch = templateCore.MatchRouteTemplate(orderRoute, urlCore.ParseRouteUrl("/app/orders/not-an-int", "/app/"));
        UiValueEntry repeatedQuery = order.RouteData!.Query.Single();
        orderUrl.Query[0].Value.ArrayValue![0].StringValue = "mutated source";

        Assert.Multiple(() =>
        {
            Assert.That(order.Success, Is.True);
            Assert.That(order.Matched, Is.True);
            Assert.That(order.RouteData!.RouteId, Is.EqualTo("order"));
            Assert.That(order.RouteData.Parameters.Select(item => item.Key), Is.EqualTo(new string[] { "id", "tab" }));
            Assert.That(order.RouteData.Parameters[0].Value.Kind, Is.EqualTo(UiValueKind.Integer));
            Assert.That(order.RouteData.Parameters[0].Value.IntegerValue, Is.EqualTo(42));
            Assert.That(order.RouteData.Parameters[1].Value.StringValue, Is.EqualTo("summary"));
            Assert.That(repeatedQuery.Value.ArrayValue!.Select(item => item.StringValue), Is.EqualTo(new string[] { "one", "two" }));
            Assert.That(order.RouteData.Fragment, Is.EqualTo("details"));
            Assert.That(optional.Matched, Is.True);
            Assert.That(optional.RouteData!.Parameters.Single().Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(files.RouteData!.Parameters.Single().Value.StringValue, Is.EqualTo("reports/2026/final"));
            Assert.That(emptyCatchAll.RouteData!.Parameters.Single().Value.StringValue, Is.EqualTo(string.Empty));
            Assert.That(constraintMismatch.Success, Is.True);
            Assert.That(constraintMismatch.Matched, Is.False);
            Assert.That(constraintMismatch.RouteData, Is.Null);
        });
    }

    [Test]
    public void ConvertsSupportedConstraintsUsingInvariantValuesAndBounds()
    {
        RouteTemplateCore core = new RouteTemplateCore();
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        RouteParameterResult decimalValue;
        RouteParameterResult floatingPoint;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            decimalValue = core.ConvertRouteParameter("1234.50", "decimal");
            floatingPoint = core.ConvertRouteParameter("1.25e2", "double");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        RouteParameterResult boolean = core.ConvertRouteParameter("TRUE", "bool");
        RouteParameterResult integer = core.ConvertRouteParameter("-2147483648", "int");
        RouteParameterResult integerOverflow = core.ConvertRouteParameter("2147483648", "int");
        RouteParameterResult longValue = core.ConvertRouteParameter("9223372036854775807", "long");
        RouteParameterResult longOverflow = core.ConvertRouteParameter("9223372036854775808", "long");
        RouteParameterResult guid = core.ConvertRouteParameter("A0B1C2D3-E4F5-4678-9012-ABCDEF123456", "guid");
        RouteParameterResult dateTime = core.ConvertRouteParameter("2024-02-29T23:59:58.123Z", "datetime");
        RouteParameterResult invalidDateTime = core.ConvertRouteParameter("2023-02-29", "datetime");
        RouteParameterResult nonFinite = core.ConvertRouteParameter("1e999", "double");

        Assert.Multiple(() =>
        {
            Assert.That(boolean.Value.BooleanValue, Is.True);
            Assert.That(integer.Value.IntegerValue, Is.EqualTo(int.MinValue));
            Assert.That(integerOverflow.Matched, Is.False);
            Assert.That(longValue.Value.IntegerValue, Is.EqualTo(long.MaxValue));
            Assert.That(longOverflow.Matched, Is.False);
            Assert.That(decimalValue.Matched, Is.True);
            Assert.That(decimalValue.Value.Kind, Is.EqualTo(UiValueKind.Decimal));
            Assert.That(decimalValue.Value.DecimalValue, Is.EqualTo(1234.50m));
            Assert.That(floatingPoint.Value.FloatingPointValue, Is.EqualTo(125.0));
            Assert.That(guid.Value.StringValue, Is.EqualTo("a0b1c2d3-e4f5-4678-9012-abcdef123456"));
            Assert.That(dateTime.Value.DateTimeValue, Is.EqualTo("2024-02-29T23:59:58.123Z"));
            Assert.That(invalidDateTime.Matched, Is.False);
            Assert.That(nonFinite.Matched, Is.False);
        });
    }

    [Test]
    public void SortsRoutesBySegmentPrecedenceIndependentlyOfRegistrationOrder()
    {
        RouteTemplateCore core = new RouteTemplateCore();
        UiRoute catchAll = Route("catch", "/{*path}");
        UiRoute parameter = Route("parameter", "/{value}");
        UiRoute optional = Route("optional", "/{value?}");
        UiRoute constrained = Route("constrained", "/{value:int}");
        UiRoute literal = Route("literal", "/fixed");
        UiRoute earlyLiteral = Route("early-literal", "/fixed/{value}");
        UiRoute lateLiteral = Route("late-literal", "/{value}/fixed");

        RouteCollectionResult forward = core.ValidateRouteCollection(new UiRoute[] { catchAll, parameter, optional, constrained, lateLiteral, literal, earlyLiteral });
        RouteCollectionResult reverse = core.ValidateRouteCollection(new UiRoute[] { earlyLiteral, literal, lateLiteral, constrained, optional, parameter, catchAll });

        Assert.Multiple(() =>
        {
            Assert.That(forward.Success, Is.True);
            Assert.That(reverse.Success, Is.True);
            Assert.That(forward.Routes.Select(item => item.Id), Is.EqualTo(new string[]
            {
                "literal",
                "early-literal",
                "constrained",
                "late-literal",
                "parameter",
                "optional",
                "catch"
            }));
            Assert.That(reverse.Routes.Select(item => item.Id), Is.EqualTo(forward.Routes.Select(item => item.Id)));
            Assert.That(forward.Routes.Single(item => item.Id == "constrained").Segments.Single().Constraint, Is.EqualTo("int"));
        });
    }

    [Test]
    public void RejectsEqualPrecedenceOverlapsButAllowsDisjointConstraints()
    {
        RouteTemplateCore core = new RouteTemplateCore();
        RouteCollectionResult renamedParameter = core.ValidateRouteCollection(new UiRoute[]
        {
            Route("first", "/items/{id}"),
            Route("second", "/items/{name}")
        });
        RouteCollectionResult overlappingNumbers = core.ValidateRouteCollection(new UiRoute[]
        {
            Route("integer", "/items/{id:int}"),
            Route("long", "/items/{id:long}")
        });
        RouteCollectionResult encodedLiteral = core.ValidateRouteCollection(new UiRoute[]
        {
            Route("encoded", "/caf%C3%A9"),
            Route("unicode", "/café")
        });
        RouteCollectionResult disjoint = core.ValidateRouteCollection(new UiRoute[]
        {
            Route("boolean", "/items/{value:bool}"),
            Route("guid", "/items/{value:guid}")
        });
        RouteCollectionResult duplicateId = core.ValidateRouteCollection(new UiRoute[] { Route("same", "/one"), Route("same", "/two") });

        Assert.Multiple(() =>
        {
            Assert.That(renamedParameter.Code, Is.EqualTo("route.ambiguous"));
            Assert.That(renamedParameter.FirstRouteId, Is.EqualTo("first"));
            Assert.That(renamedParameter.SecondRouteId, Is.EqualTo("second"));
            Assert.That(overlappingNumbers.Code, Is.EqualTo("route.ambiguous"));
            Assert.That(encodedLiteral.Code, Is.EqualTo("route.ambiguous"));
            Assert.That(disjoint.Success, Is.True);
            Assert.That(disjoint.Routes.Select(item => item.Id), Is.EqualTo(new string[] { "boolean", "guid" }));
            Assert.That(duplicateId.Code, Is.EqualTo("route.duplicate-id"));
        });
    }

    private static UiRoute Route(string id, string template)
    {
        return new UiRoute { Id = id, Template = template };
    }
}
