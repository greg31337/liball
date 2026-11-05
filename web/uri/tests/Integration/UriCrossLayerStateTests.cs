namespace uri.Tests;

public class UriCrossLayerStateTests
{
    [Test]
    public void SameReferenceFlowsThroughEveryUriServiceWithoutStateLoss()
    {
        const string source =
            "HTTP://Example.COM:/a/./b/../c?#";
        const string normalized =
            "http://example.com:/a/c?#";
        const string target =
            "http://example.com:/a/d?#";
        IUriParser parser = new UriParser();
        IUriComposer composer = new UriComposer();
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer comparer = new UriComparer();
        IUriEditor editor = new UriEditor();
        IUriRelativizer relativizer = new UriRelativizer();
        IUriResolver resolver = new UriResolver();

        UriReference parsed = parser.ParseReference(
            source,
            new UriParseOptions());
        string recomposed = composer.ComposeReference(
            parsed,
            new UriValidationOptions());
        string normalizedText = normalizer.NormalizeSyntax(
            recomposed,
            new UriNormalizationOptions());
        UriComparisonResult syntaxComparison = comparer.CompareSyntax(
            source,
            normalizedText,
            new UriComparisonOptions());
        UriComparisonResult exactComparison = comparer.CompareExact(
            source,
            normalizedText,
            new UriComparisonOptions());

        UriReferenceChanges targetChanges = new UriReferenceChanges();
        targetChanges.ChangePath = true;
        targetChanges.Path = "/a/d";
        string editedTarget = editor.CopyWithChanges(
            normalizedText,
            targetChanges,
            new UriValidationOptions());

        UriReferenceChanges baseChanges = new UriReferenceChanges();
        baseChanges.ChangePath = true;
        baseChanges.Path = "/a/base";
        baseChanges.ChangeFragment = true;
        baseChanges.HasFragment = false;
        string baseUri = editor.CopyWithChanges(
            normalizedText,
            baseChanges,
            new UriValidationOptions());

        UriRelativizationOptions relativizationOptions =
            new UriRelativizationOptions();
        relativizationOptions.Mode = UriRelativizationMode.Shortest;
        string relative = relativizer.RelativizeReference(
            baseUri,
            editedTarget,
            relativizationOptions);
        string resolved = resolver.ResolveReference(
            baseUri,
            relative,
            new UriResolutionOptions());
        UriComparisonResult roundTripComparison = comparer.CompareExact(
            editedTarget,
            resolved,
            new UriComparisonOptions());
        UriReference finalReference = parser.ParseReference(
            resolved,
            new UriParseOptions());
        string finalComposition = composer.ComposeReference(
            finalReference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(recomposed, Is.EqualTo(source));
            Assert.That(normalizedText, Is.EqualTo(normalized));
            Assert.That(syntaxComparison.Success, Is.True);
            Assert.That(syntaxComparison.Equal, Is.True);
            Assert.That(syntaxComparison.Level,
                Is.EqualTo(UriComparisonLevel.Syntax));
            Assert.That(exactComparison.Success, Is.True);
            Assert.That(exactComparison.Equal, Is.False);
            Assert.That(editedTarget, Is.EqualTo(target));
            Assert.That(baseUri,
                Is.EqualTo("http://example.com:/a/base?"));
            Assert.That(relative, Is.EqualTo("d?#"));
            Assert.That(resolved, Is.EqualTo(target));
            Assert.That(roundTripComparison.Success, Is.True);
            Assert.That(roundTripComparison.Equal, Is.True);
            Assert.That(finalComposition, Is.EqualTo(target));
        });

        AssertStates(parsed, true, true, true, true);
        AssertStates(
            parser.ParseReference(normalizedText, new UriParseOptions()),
            true,
            true,
            true,
            true);
        AssertStates(
            parser.ParseReference(baseUri, new UriParseOptions()),
            true,
            true,
            true,
            false);
        AssertStates(finalReference, true, true, true, true);
        AssertStates(
            parser.ParseReference(relative, new UriParseOptions()),
            false,
            false,
            true,
            true);
    }

    [TestCase("x:/path", false, false, false, false)]
    [TestCase("x:///path", true, false, false, false)]
    [TestCase("x://host/path", true, false, false, false)]
    [TestCase("x://host:/path", true, true, false, false)]
    [TestCase("x:/path?", false, false, true, false)]
    [TestCase("x:/path#", false, false, false, true)]
    [TestCase("x:/path?#", false, false, true, true)]
    public void AbsentAndEmptyStatesSurviveCrossLayerNoOpWorkflow(
        string text,
        bool hasAuthority,
        bool hasPort,
        bool hasQuery,
        bool hasFragment)
    {
        IUriParser parser = new UriParser();
        IUriComposer composer = new UriComposer();
        IUriEditor editor = new UriEditor();
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer comparer = new UriComparer();

        UriReference parsed = parser.ParseReference(
            text,
            new UriParseOptions());
        string composed = composer.ComposeReference(
            parsed,
            new UriValidationOptions());
        string edited = editor.CopyWithChanges(
            composed,
            new UriReferenceChanges(),
            new UriValidationOptions());
        string normalized = normalizer.NormalizeSyntax(
            edited,
            new UriNormalizationOptions());
        UriComparisonResult exact = comparer.CompareExact(
            text,
            normalized,
            new UriComparisonOptions());
        UriComparisonResult syntax = comparer.CompareSyntax(
            text,
            normalized,
            new UriComparisonOptions());
        UriReference finalReference = parser.ParseReference(
            normalized,
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(composed, Is.EqualTo(text));
            Assert.That(edited, Is.EqualTo(text));
            Assert.That(normalized, Is.EqualTo(text));
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Equal, Is.True);
            Assert.That(exact.DifferentComponent,
                Is.EqualTo(UriComponent.None));
            Assert.That(syntax.Success, Is.True);
            Assert.That(syntax.Equal, Is.True);
        });
        AssertStates(
            finalReference,
            hasAuthority,
            hasPort,
            hasQuery,
            hasFragment);
    }

    [Test]
    public void EditorCanClearAndRestorePresentEmptyDelimiters()
    {
        IUriEditor editor = new UriEditor();
        IUriParser parser = new UriParser();
        const string source = "x://host:/path?#";

        UriReferenceChanges clear = new UriReferenceChanges();
        clear.ChangeAuthority = true;
        clear.HasAuthority = false;
        clear.ChangeQuery = true;
        clear.HasQuery = false;
        clear.ChangeFragment = true;
        clear.HasFragment = false;
        string cleared = editor.CopyWithChanges(
            source,
            clear,
            new UriValidationOptions());

        UriReferenceChanges restore = new UriReferenceChanges();
        restore.ChangeAuthority = true;
        restore.HasAuthority = true;
        restore.Authority = parser.ParseReference(
            "//host:",
            new UriParseOptions()).Authority;
        restore.ChangeQuery = true;
        restore.HasQuery = true;
        restore.Query = "";
        restore.ChangeFragment = true;
        restore.HasFragment = true;
        restore.Fragment = "";
        string restored = editor.CopyWithChanges(
            cleared,
            restore,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(cleared, Is.EqualTo("x:/path"));
            Assert.That(restored, Is.EqualTo(source));
        });
        AssertStates(
            parser.ParseReference(cleared, new UriParseOptions()),
            false,
            false,
            false,
            false);
        AssertStates(
            parser.ParseReference(restored, new UriParseOptions()),
            true,
            true,
            true,
            true);
    }

    private static void AssertStates(
        UriReference reference,
        bool hasAuthority,
        bool hasPort,
        bool hasQuery,
        bool hasFragment)
    {
        Assert.Multiple(() =>
        {
            Assert.That(reference.HasAuthority, Is.EqualTo(hasAuthority));
            Assert.That(reference.Authority.HasPort, Is.EqualTo(hasPort));
            Assert.That(reference.Authority.Port, Is.Empty);
            Assert.That(reference.HasQuery, Is.EqualTo(hasQuery));
            Assert.That(reference.Query, Is.Empty);
            Assert.That(reference.HasFragment, Is.EqualTo(hasFragment));
            Assert.That(reference.Fragment, Is.Empty);
        });
    }
}
