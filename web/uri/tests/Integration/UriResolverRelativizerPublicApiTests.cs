namespace uri.Tests;

public class UriResolverRelativizerPublicApiTests
{
    [Test]
    public void ResolverExposesThrowingAndResultBasedSuccessPaths()
    {
        IUriResolver resolver = new UriResolver();
        UriResolutionOptions options = new UriResolutionOptions();

        string resolved = resolver.ResolveReference(
            "http://a/b/c/d;p?q",
            "../g?new#fragment",
            options);
        UriTextResult attempted = resolver.TryResolveReference(
            "http://a/b/c/d;p?q",
            "../g?new#fragment",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.EqualTo("http://a/b/g?new#fragment"));
            Assert.That(attempted.Success, Is.True);
            Assert.That(attempted.Text, Is.EqualTo(resolved));
            Assert.That(attempted.Length, Is.EqualTo(resolved.Length));
            Assert.That(attempted.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void ResolverPublicOptionsSelectStrictOrCompatibleBehavior()
    {
        IUriResolver resolver = new UriResolver();
        UriResolutionOptions strictOptions = new UriResolutionOptions();
        UriResolutionOptions compatibleOptions = new UriResolutionOptions();
        compatibleOptions.Mode = UriResolutionMode.Compatible;

        string strict = resolver.ResolveReference(
            "http://a/b/c/d",
            "http:g",
            strictOptions);
        string compatible = resolver.ResolveReference(
            "http://a/b/c/d",
            "http:g",
            compatibleOptions);

        Assert.Multiple(() =>
        {
            Assert.That(strict, Is.EqualTo("http:g"));
            Assert.That(compatible, Is.EqualTo("http://a/b/c/g"));
        });
    }

    [Test]
    public void ResolverFailureIsAvailableAsAResultOrResolutionException()
    {
        IUriResolver resolver = new UriResolver();
        UriResolutionOptions options = new UriResolutionOptions();

        UriTextResult result = resolver.TryResolveReference(
            "relative/base",
            "target",
            options);
        UriResolutionException? exception = Assert.Throws<UriResolutionException>(() =>
            resolver.ResolveReference("relative/base", "target", options));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.UnresolvableReference));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Scheme));
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.Code, Is.EqualTo(result.Diagnostic.Code));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(result.Diagnostic.Component));
            Assert.That(exception.Message, Is.EqualTo(result.Diagnostic.Message));
        });
    }

    [Test]
    public void RelativizerExposesThrowingAndResultBasedSuccessPaths()
    {
        IUriRelativizer relativizer = new UriRelativizer();
        UriRelativizationOptions options = new UriRelativizationOptions();

        string relative = relativizer.RelativizeReference(
            "http://a/b/c/d",
            "http://a/b/g?new#fragment",
            options);
        UriTextResult attempted = relativizer.TryRelativizeReference(
            "http://a/b/c/d",
            "http://a/b/g?new#fragment",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(relative, Is.EqualTo("../g?new#fragment"));
            Assert.That(attempted.Success, Is.True);
            Assert.That(attempted.Text, Is.EqualTo(relative));
            Assert.That(attempted.Length, Is.EqualTo(relative.Length));
            Assert.That(attempted.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void PublicRelativizationRoundTripsThroughThePublicResolver()
    {
        IUriRelativizer relativizer = new UriRelativizer();
        IUriResolver resolver = new UriResolver();
        UriRelativizationOptions options = new UriRelativizationOptions();
        options.Mode = UriRelativizationMode.Shortest;
        string baseUri = "http://example.com/a/very/deep/base";
        string targetUri = "http://example.com/x?query#fragment";

        string relative = relativizer.RelativizeReference(
            baseUri,
            targetUri,
            options);
        string resolved = resolver.ResolveReference(
            baseUri,
            relative,
            new UriResolutionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(relative, Is.EqualTo("/x?query#fragment"));
            Assert.That(resolved, Is.EqualTo(targetUri));
        });
    }

    [Test]
    public void RelativizerUsesTheSelectedComparisonPolicy()
    {
        IUriRelativizer relativizer = new UriRelativizer();
        UriRelativizationOptions options = new UriRelativizationOptions();
        options.ComparisonLevel = UriComparisonLevel.Syntax;

        string relative = relativizer.RelativizeReference(
            "HTTP://EXAMPLE.com/a/b",
            "http://example.com/a/./c",
            options);

        Assert.That(relative, Is.EqualTo("c"));
    }

    [Test]
    public void RelativizerFailureIsAvailableAsAResultOrResolutionException()
    {
        IUriRelativizer relativizer = new UriRelativizer();
        UriRelativizationOptions options = new UriRelativizationOptions();
        string baseUri = "http://a/a/b";
        string targetUri = "http://a/a/./c";

        UriTextResult result = relativizer.TryRelativizeReference(
            baseUri,
            targetUri,
            options);
        UriResolutionException? exception = Assert.Throws<UriResolutionException>(() =>
            relativizer.RelativizeReference(baseUri, targetUri, options));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.UnresolvableReference));
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Diagnostic.Code, Is.EqualTo(result.Diagnostic.Code));
            Assert.That(exception.Message, Is.EqualTo(result.Diagnostic.Message));
        });
    }
}
