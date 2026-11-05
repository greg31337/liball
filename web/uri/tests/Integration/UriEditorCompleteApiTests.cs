namespace uri.Tests;

public class UriEditorCompleteApiTests
{
    [Test]
    public void PublicConstructionDerivesKindForThrowingAndResultPaths()
    {
        IUriEditor editor = new UriEditor();
        UriReference components = Parse("x://user@host:80/path?#");
        components.Kind = UriReferenceKind.EmptyReference;
        components.OriginalText = "ignored";

        string built = editor.BuildFromComponents(
            components,
            new UriValidationOptions());
        UriTextResult attempted = editor.TryBuildFromComponents(
            components,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(built, Is.EqualTo("x://user@host:80/path?#"));
            Assert.That(attempted.Success, Is.True);
            Assert.That(attempted.Text, Is.EqualTo(built));
            Assert.That(attempted.Length, Is.EqualTo(built.Length));
            Assert.That(attempted.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void PublicCopyCanKeepSetAndClearSchemeAuthorityAndPath()
    {
        IUriEditor editor = new UriEditor();
        UriReferenceChanges keep = new UriReferenceChanges();
        UriReferenceChanges set = new UriReferenceChanges();
        set.ChangeScheme = true;
        set.HasScheme = true;
        set.Scheme = "https";
        set.ChangeAuthority = true;
        set.HasAuthority = true;
        set.Authority = Parse("//other:443/").Authority;
        set.ChangePath = true;
        set.Path = "/new";
        UriReferenceChanges clear = new UriReferenceChanges();
        clear.ChangeScheme = true;
        clear.HasScheme = false;
        clear.ChangeAuthority = true;
        clear.HasAuthority = false;
        clear.ChangePath = true;
        clear.Path = "relative/path";

        string kept = editor.CopyWithChanges(
            "http://host/old?query#fragment",
            keep,
            new UriValidationOptions());
        string changed = editor.CopyWithChanges(
            "http://host/old?query#fragment",
            set,
            new UriValidationOptions());
        string cleared = editor.CopyWithChanges(
            "http://host/old?query#fragment",
            clear,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(kept, Is.EqualTo("http://host/old?query#fragment"));
            Assert.That(changed, Is.EqualTo("https://other:443/new?query#fragment"));
            Assert.That(cleared, Is.EqualTo("relative/path?query#fragment"));
            Assert.That(Parse(cleared).Authority.HostKind, Is.EqualTo(UriHostKind.Absent));
        });
    }

    [TestCase(false, false, "ignored", "x:/path", false, "")]
    [TestCase(true, false, "ignored", "x:/path", false, "")]
    [TestCase(true, true, "", "x:/path?", true, "")]
    [TestCase(true, true, "new", "x:/path?new", true, "new")]
    public void QueryEditingPreservesAbsentEmptyAndPresentStates(
        bool change,
        bool hasQuery,
        string query,
        string expected,
        bool expectedHasQuery,
        string expectedQuery)
    {
        IUriEditor editor = new UriEditor();
        UriReferenceChanges changes = new UriReferenceChanges();
        changes.ChangeQuery = change;
        changes.HasQuery = hasQuery;
        changes.Query = query;

        string edited = editor.CopyWithChanges(
            "x:/path",
            changes,
            new UriValidationOptions());
        UriReference parsed = Parse(edited);

        Assert.Multiple(() =>
        {
            Assert.That(edited, Is.EqualTo(expected));
            Assert.That(parsed.HasQuery, Is.EqualTo(expectedHasQuery));
            Assert.That(parsed.Query, Is.EqualTo(expectedQuery));
        });
    }

    [TestCase(false, false, "ignored", "x:/path", false, "")]
    [TestCase(true, false, "ignored", "x:/path", false, "")]
    [TestCase(true, true, "", "x:/path#", true, "")]
    [TestCase(true, true, "new", "x:/path#new", true, "new")]
    public void FragmentEditingPreservesAbsentEmptyAndPresentStates(
        bool change,
        bool hasFragment,
        string fragment,
        string expected,
        bool expectedHasFragment,
        string expectedFragment)
    {
        IUriEditor editor = new UriEditor();
        UriReferenceChanges changes = new UriReferenceChanges();
        changes.ChangeFragment = change;
        changes.HasFragment = hasFragment;
        changes.Fragment = fragment;

        string edited = editor.CopyWithChanges(
            "x:/path",
            changes,
            new UriValidationOptions());
        UriReference parsed = Parse(edited);

        Assert.Multiple(() =>
        {
            Assert.That(edited, Is.EqualTo(expected));
            Assert.That(parsed.HasFragment, Is.EqualTo(expectedHasFragment));
            Assert.That(parsed.Fragment, Is.EqualTo(expectedFragment));
        });
    }

    [TestCase("//", "x:///path", UriHostKind.RegisteredName)]
    [TestCase("//example.com", "x://example.com/path", UriHostKind.RegisteredName)]
    [TestCase("//192.0.2.1", "x://192.0.2.1/path", UriHostKind.Ipv4)]
    [TestCase("//[2001:db8::1]", "x://[2001:db8::1]/path", UriHostKind.Ipv6)]
    [TestCase("//[v1.a:b]", "x://[v1.a:b]/path", UriHostKind.IpvFuture)]
    public void AuthorityReplacementSupportsEveryHostKind(
        string authorityText,
        string expected,
        UriHostKind expectedKind)
    {
        IUriEditor editor = new UriEditor();
        UriReferenceChanges changes = new UriReferenceChanges();
        changes.ChangeAuthority = true;
        changes.HasAuthority = true;
        changes.Authority = Parse(authorityText).Authority;

        string edited = editor.CopyWithChanges(
            "x://source/path",
            changes,
            new UriValidationOptions());
        UriReference parsed = Parse(edited);

        Assert.Multiple(() =>
        {
            Assert.That(edited, Is.EqualTo(expected));
            Assert.That(parsed.Authority.HostKind, Is.EqualTo(expectedKind));
        });
    }

    [Test]
    public void PublicChangeSetDefaultsKeepEveryComponentIndependently()
    {
        UriReferenceChanges first = new UriReferenceChanges();
        UriReferenceChanges second = new UriReferenceChanges();
        first.Authority.Host = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first.ChangeScheme, Is.False);
            Assert.That(first.ChangeAuthority, Is.False);
            Assert.That(first.ChangePath, Is.False);
            Assert.That(first.ChangeQuery, Is.False);
            Assert.That(first.ChangeFragment, Is.False);
            Assert.That(first.Scheme, Is.Empty);
            Assert.That(first.Path, Is.Empty);
            Assert.That(first.Query, Is.Empty);
            Assert.That(first.Fragment, Is.Empty);
            Assert.That(second.Authority.Host, Is.Empty);
            Assert.That(first.Authority, Is.Not.SameAs(second.Authority));
        });
    }

    [Test]
    public void InvalidPostEditCombinationsThrowComponentDiagnostics()
    {
        IUriEditor editor = new UriEditor();
        UriReferenceChanges authority = new UriReferenceChanges();
        authority.ChangeAuthority = true;
        authority.HasAuthority = true;
        authority.Authority = Parse("//host/").Authority;
        authority.ChangePath = true;
        authority.Path = "rootless";
        UriReferenceChanges relativeColon = new UriReferenceChanges();
        relativeColon.ChangeScheme = true;
        relativeColon.HasScheme = false;
        relativeColon.ChangePath = true;
        relativeColon.Path = "name:part";
        UriReferenceChanges query = new UriReferenceChanges();
        query.ChangeQuery = true;
        query.HasQuery = true;
        query.Query = "%GG";

        UriValidationException authorityFailure = Assert.Throws<UriValidationException>(() =>
            editor.CopyWithChanges("x:/path", authority, new UriValidationOptions()))!;
        UriValidationException pathFailure = Assert.Throws<UriValidationException>(() =>
            editor.CopyWithChanges("x:/path", relativeColon, new UriValidationOptions()))!;
        UriValidationException queryFailure = Assert.Throws<UriValidationException>(() =>
            editor.CopyWithChanges("x:/path", query, new UriValidationOptions()))!;

        Assert.Multiple(() =>
        {
            AssertException(authorityFailure, UriDiagnosticCode.InvalidPath, UriComponent.Path);
            AssertException(pathFailure, UriDiagnosticCode.InvalidPath, UriComponent.Path);
            AssertException(queryFailure, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Query);
        });
    }

    [Test]
    public void BuildResultAndExceptionPathsReportMatchingFailures()
    {
        IUriEditor editor = new UriEditor();
        UriReference invalid = Parse("x:/path");
        invalid.Path = "%GG";

        UriTextResult result = editor.TryBuildFromComponents(
            invalid,
            new UriValidationOptions());
        UriValidationException exception = Assert.Throws<UriValidationException>(() =>
            editor.BuildFromComponents(invalid, new UriValidationOptions()))!;

        AssertFailure(result, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path);
        AssertException(exception, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path);
        Assert.That(exception.Message, Is.EqualTo(result.Diagnostic!.Message));
    }

    [Test]
    public void MissingArgumentsAndMalformedSourcesFailPredictably()
    {
        IUriEditor editor = new UriEditor();
        UriValidationOptions missingLimits = new UriValidationOptions();
        missingLimits.Limits = null!;
        UriReference components = Parse("x:/path");

        UriTextResult missingComponents = editor.TryBuildFromComponents(
            null!,
            new UriValidationOptions());
        UriTextResult missingOptions = editor.TryBuildFromComponents(
            components,
            null!);
        UriTextResult missingBuildLimits = editor.TryBuildFromComponents(
            components,
            missingLimits);
        UriValidationException sourceFailure = Assert.Throws<UriValidationException>(() =>
            editor.CopyWithChanges("x:%GG", new UriReferenceChanges(), new UriValidationOptions()))!;
        UriValidationException missingSource = Assert.Throws<UriValidationException>(() =>
            editor.CopyWithChanges(null!, new UriReferenceChanges(), new UriValidationOptions()))!;
        UriValidationException missingChanges = Assert.Throws<UriValidationException>(() =>
            editor.CopyWithChanges("x:/path", null!, new UriValidationOptions()))!;
        UriValidationException missingCopyOptions = Assert.Throws<UriValidationException>(() =>
            editor.CopyWithChanges("x:/path", new UriReferenceChanges(), null!))!;

        Assert.Multiple(() =>
        {
            AssertFailure(missingComponents, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertFailure(missingOptions, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertFailure(missingBuildLimits, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertException(sourceFailure, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path);
            AssertException(missingSource, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertException(missingChanges, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertException(missingCopyOptions, UriDiagnosticCode.InvalidInput, UriComponent.Input);
        });
    }

    [Test]
    public void PublicBuildAndCopyApplyComponentSegmentAndOutputLimits()
    {
        IUriEditor editor = new UriEditor();
        UriValidationOptions component = new UriValidationOptions();
        component.Limits.MaxComponentCharacters = 2;
        UriValidationOptions segments = new UriValidationOptions();
        segments.Limits.MaxPathSegments = 1;
        UriValidationOptions output = new UriValidationOptions();
        output.Limits.MaxOutputCharacters = 3;
        UriReferenceChanges outputChanges = new UriReferenceChanges();
        outputChanges.ChangePath = true;
        outputChanges.Path = "long";

        UriTextResult componentResult = editor.TryBuildFromComponents(
            Parse("x:long"),
            component);
        UriTextResult segmentResult = editor.TryBuildFromComponents(
            Parse("x:/a/b"),
            segments);
        UriValidationException outputFailure = Assert.Throws<UriValidationException>(() =>
            editor.CopyWithChanges("x:a", outputChanges, output))!;

        Assert.Multiple(() =>
        {
            AssertFailure(componentResult, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
            AssertFailure(segmentResult, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
            AssertException(outputFailure, UriDiagnosticCode.LimitExceeded, UriComponent.Output);
        });
    }

    private static UriReference Parse(string text)
    {
        IUriParser parser = new UriParser();
        return parser.ParseReference(text, new UriParseOptions());
    }

    private static void AssertFailure(
        UriTextResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Length, Is.Zero);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }

    private static void AssertException(
        UriValidationException exception,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(exception.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(exception.Message, Is.EqualTo(exception.Diagnostic.Message));
    }
}
