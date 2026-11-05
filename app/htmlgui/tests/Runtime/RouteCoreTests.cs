using System.Globalization;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class RouteCoreTests
{
    [Test]
    public void CanonicalizesBaseAndApplicationRelativePaths()
    {
        RouteCore core = new RouteCore();
        RouteUrlResult normalizedBase = core.CanonicalizeRouteBasePath("store//catalog/../app");
        RouteUrlResult relative = core.ParseRouteUrl("catalog/../caf%C3%A9//item%20one/?z=last&a=first#d%C3%A9tails", "/store/./app/");
        RouteUrlResult absolute = core.ParseRouteUrl("/store/app/users/../profile/", "/store/app");
        RouteUrlResult baseRoot = core.ParseRouteUrl("/store/app", "/store/app/");

        Assert.Multiple(() =>
        {
            Assert.That(normalizedBase.Success, Is.True);
            Assert.That(normalizedBase.BasePath, Is.EqualTo("/store/app/"));
            Assert.That(normalizedBase.PathSegments, Is.EqualTo(new string[] { "store", "app" }));
            Assert.That(relative.Success, Is.True);
            Assert.That(relative.BasePath, Is.EqualTo("/store/app/"));
            Assert.That(relative.Path, Is.EqualTo("/café/item one"));
            Assert.That(relative.PathSegments, Is.EqualTo(new string[] { "café", "item one" }));
            Assert.That(relative.CanonicalUrl, Is.EqualTo("/store/app/caf%C3%A9/item%20one?a=first&z=last#d%C3%A9tails"));
            Assert.That(relative.Fragment, Is.EqualTo("détails"));
            Assert.That(absolute.Path, Is.EqualTo("/profile"));
            Assert.That(absolute.CanonicalUrl, Is.EqualTo("/store/app/profile"));
            Assert.That(baseRoot.Path, Is.EqualTo("/"));
            Assert.That(baseRoot.CanonicalUrl, Is.EqualTo("/store/app/"));
        });
    }

    [Test]
    public void DecodesUtf8QueryAndFragmentWithoutLosingRepeatedValues()
    {
        RouteCore core = new RouteCore();
        RouteUrlResult parsed = core.ParseRouteUrl("/%F0%9F%98%80/%7euser?empty=&name=Ada+Lovelace&symbol=%2B&tag=one&tag=two#part%202", "/");
        UiValueEntry tag = parsed.Query.Single(item => item.Key == "tag");

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.PathSegments, Is.EqualTo(new string[] { "😀", "~user" }));
            Assert.That(parsed.Query.Select(item => item.Key), Is.EqualTo(new string[] { "empty", "name", "symbol", "tag" }));
            Assert.That(parsed.Query.Single(item => item.Key == "empty").Value.StringValue, Is.EqualTo(string.Empty));
            Assert.That(parsed.Query.Single(item => item.Key == "name").Value.StringValue, Is.EqualTo("Ada Lovelace"));
            Assert.That(parsed.Query.Single(item => item.Key == "symbol").Value.StringValue, Is.EqualTo("+"));
            Assert.That(tag.Value.Kind, Is.EqualTo(UiValueKind.Array));
            Assert.That(tag.Value.ArrayValue!.Select(item => item.StringValue), Is.EqualTo(new string[] { "one", "two" }));
            Assert.That(parsed.Fragment, Is.EqualTo("part 2"));
            Assert.That(parsed.CanonicalUrl, Is.EqualTo("/%F0%9F%98%80/~user?empty=&name=Ada%20Lovelace&symbol=%2B&tag=one&tag=two#part%202"));
        });
    }

    [TestCase("/%", "route.invalid-percent-encoding")]
    [TestCase("/%GG", "route.invalid-percent-encoding")]
    [TestCase("/%C0%AF", "route.invalid-utf8")]
    [TestCase("/%ED%A0%80", "route.invalid-utf8")]
    [TestCase("/%F4%90%80%80", "route.invalid-utf8")]
    [TestCase("/%2F", "route.invalid-decoded-character")]
    [TestCase("/?=value", "route.invalid-query-key")]
    [TestCase("/line%0Abreak", "route.invalid-decoded-character")]
    public void RejectsMalformedOrUnsafeEncodedComponents(string url, string code)
    {
        RouteUrlResult result = new RouteCore().ParseRouteUrl(url, "/");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(code));
        });
    }

    [Test]
    public void PreventsTraversalOutsideTheCanonicalBasePath()
    {
        RouteCore core = new RouteCore();
        RouteUrlResult relativeTraversal = core.ParseRouteUrl("../secret", "/app/");
        RouteUrlResult absoluteTraversal = core.ParseRouteUrl("/app/%2e%2e/secret", "/app/");
        RouteUrlResult siblingPrefix = core.ParseRouteUrl("/application/page", "/app/");
        RouteUrlResult invalidBaseTraversal = core.CanonicalizeRouteBasePath("../app");
        RouteUrlResult invalidBaseQuery = core.CanonicalizeRouteBasePath("/app?mode=test");
        RouteUrlResult collapsed = core.ParseRouteUrl("./users//details/../profile", "/app//");

        Assert.Multiple(() =>
        {
            Assert.That(relativeTraversal.Code, Is.EqualTo("route.path-escapes-base"));
            Assert.That(absoluteTraversal.Code, Is.EqualTo("route.outside-base-path"));
            Assert.That(siblingPrefix.Code, Is.EqualTo("route.outside-base-path"));
            Assert.That(invalidBaseTraversal.Code, Is.EqualTo("route.invalid-base-path"));
            Assert.That(invalidBaseQuery.Code, Is.EqualTo("route.invalid-base-path"));
            Assert.That(collapsed.Success, Is.True);
            Assert.That(collapsed.CanonicalUrl, Is.EqualTo("/app/users/profile"));
        });
    }

    [Test]
    public void AllowsOnlyWellFormedSafeExternalSchemes()
    {
        RouteCore core = new RouteCore();
        RouteUrlResult secure = core.ParseRouteUrl("HTTPS://EXAMPLE.TEST:443/a/../b?q=x#part", "/app/");
        RouteUrlResult mail = core.ParseRouteUrl("mailto:person@example.test", "/app/");
        RouteUrlResult telephone = core.ParseRouteUrl("tel:+15551212", "/app/");
        RouteUrlResult schemeRelative = core.ParseRouteUrl("//EXAMPLE.TEST:443/a", "/app/");
        RouteUrlResult script = core.ParseRouteUrl("javascript:alert(1)", "/app/");
        RouteUrlResult data = core.ParseRouteUrl("data:text/html,test", "/app/");
        RouteUrlResult file = core.ParseRouteUrl("file:///tmp/item", "/app/");
        RouteUrlResult malformed = core.ParseRouteUrl("https:///missing-host", "/app/");
        RouteUrlResult invalidScheme = core.ParseRouteUrl("1http:value", "/app/");

        Assert.Multiple(() =>
        {
            Assert.That(secure.Success, Is.True);
            Assert.That(secure.IsExternal, Is.True);
            Assert.That(secure.Scheme, Is.EqualTo("https"));
            Assert.That(secure.CanonicalUrl, Is.EqualTo("https://example.test/b?q=x#part"));
            Assert.That(mail.CanonicalUrl, Is.EqualTo("mailto:person@example.test"));
            Assert.That(telephone.CanonicalUrl, Is.EqualTo("tel:+15551212"));
            Assert.That(schemeRelative.CanonicalUrl, Is.EqualTo("//example.test/a"));
            Assert.That(script.Code, Is.EqualTo("route.unsafe-scheme"));
            Assert.That(data.Code, Is.EqualTo("route.unsafe-scheme"));
            Assert.That(file.Code, Is.EqualTo("route.unsafe-scheme"));
            Assert.That(malformed.Code, Is.EqualTo("route.invalid-external-url"));
            Assert.That(invalidScheme.Code, Is.EqualTo("route.invalid-scheme"));
        });
    }

    [Test]
    public void FormatsPortableRouteValuesIndependentlyOfCurrentCulture()
    {
        RouteCore core = new RouteCore();
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        RouteValueResult boolean;
        RouteValueResult integer;
        RouteValueResult floatingPoint;
        RouteValueResult decimalValue;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            boolean = core.FormatInvariantRouteValue(new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true });
            integer = core.FormatInvariantRouteValue(new UiValue { Kind = UiValueKind.Integer, IntegerValue = -1234567 });
            floatingPoint = core.FormatInvariantRouteValue(new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = 1234.5 });
            decimalValue = core.FormatInvariantRouteValue(new UiValue { Kind = UiValueKind.Decimal, DecimalValue = 9876.50m });
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        RouteValueResult nullValue = core.FormatInvariantRouteValue(null);
        RouteValueResult nonFinite = core.FormatInvariantRouteValue(new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = double.PositiveInfinity });
        RouteValueResult collection = core.FormatInvariantRouteValue(new UiValue { Kind = UiValueKind.Array, ArrayValue = System.Array.Empty<UiValue>() });

        Assert.Multiple(() =>
        {
            Assert.That(boolean.Text, Is.EqualTo("true"));
            Assert.That(integer.Text, Is.EqualTo("-1234567"));
            Assert.That(floatingPoint.Text, Is.EqualTo("1234.5"));
            Assert.That(decimalValue.Text, Is.EqualTo("9876.5"));
            Assert.That(nullValue.Code, Is.EqualTo("route.null-value"));
            Assert.That(nonFinite.Code, Is.EqualTo("route.non-finite-value"));
            Assert.That(collection.Code, Is.EqualTo("route.unsupported-value-kind"));
        });
    }
}
