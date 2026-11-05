namespace svg.Tests;

public class SvgResourcePolicyTests
{
    [Test]
    public void DefaultsAreSecureCallerMediatedAndIndependentlyMutable()
    {
        SvgResourcePolicy first = new SvgResourcePolicy();
        SvgResourcePolicy second = new SvgResourcePolicy();
        first.AllowedSchemes.Add("memory");
        first.AllowedMediaTypes.Add("application/x-fixture");

        Assert.Multiple(() =>
        {
            Assert.That(first.AllowCallerResolvedRootUri, Is.True);
            Assert.That(first.AllowImportedStyleSheets, Is.True);
            Assert.That(first.AllowSameDocumentUris, Is.True);
            Assert.That(first.AllowRelativeUris, Is.True);
            Assert.That(first.AllowDataUris, Is.True);
            Assert.That(first.AllowFileUris, Is.False);
            Assert.That(first.AllowRemoteFileUris, Is.False);
            Assert.That(first.AllowHttpUris, Is.True);
            Assert.That(first.AllowHttpsUris, Is.True);
            Assert.That(first.AllowCrossOriginUris, Is.False);
            Assert.That(first.AllowCrossOriginRedirects, Is.False);
            Assert.That(first.AllowCustomSchemes, Is.False);
            Assert.That(first.AllowUnknownMediaTypes, Is.False);
            Assert.That(second.AllowedSchemes, Is.Empty);
            Assert.That(second.AllowedMediaTypes, Is.Empty);
        });
    }

    [Test]
    public void SameOriginRelativeImageRequestIsNormalizedAndAllowed()
    {
        SvgResourcePolicy policy = new SvgResourcePolicy();
        SvgResourcePolicyRequest request = Request("../images/icon.png", "https://EXAMPLE.test/docs/root.svg", "../images/icon.png", SvgResourceKind.Image, SvgMimeTypes.Png);

        SvgResourcePolicyDecision decision = policy.Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Allowed, Is.True);
            Assert.That(decision.Reason, Is.EqualTo(SvgResourceDenialReason.None));
            Assert.That(decision.Diagnostic, Is.Null);
            Assert.That(decision.NormalizedUri, Is.EqualTo("https://example.test/images/icon.png"));
            Assert.That(decision.Scheme, Is.EqualTo("https"));
            Assert.That(decision.Origin, Is.EqualTo("https://example.test"));
            Assert.That(decision.IsSameOrigin, Is.True);
            Assert.That(decision.IsSameDocument, Is.False);
        });
    }

    [Test]
    public void SameDocumentReferencesHaveAnExplicitPolicyDecision()
    {
        SvgResourcePolicy policy = new SvgResourcePolicy();
        SvgResourcePolicyRequest request = Request("#paint", "https://example.test/root.svg", "https://example.test/root.svg#paint", SvgResourceKind.Document, "");

        SvgResourcePolicyDecision allowed = policy.Evaluate(request);
        policy.AllowSameDocumentUris = false;
        SvgResourcePolicyDecision denied = policy.Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Allowed, Is.True);
            Assert.That(allowed.IsSameDocument, Is.True);
            Assert.That(denied.Allowed, Is.False);
            Assert.That(denied.Reason, Is.EqualTo(SvgResourceDenialReason.SameDocumentDenied));
            Assert.That(denied.Diagnostic.Code, Is.EqualTo(SvgDiagnosticCode.ResourceDenied));
        });
    }

    [Test]
    public void SchemeAndImportSpecificSwitchesProduceDistinctDenials()
    {
        SvgResourcePolicy policy = new SvgResourcePolicy();
        SvgResourcePolicyDecision dataImage = policy.Evaluate(Request("data:image/png;base64,", "", "data:image/png;base64,", SvgResourceKind.Image, SvgMimeTypes.Png));
        SvgResourcePolicyDecision dataCss = policy.Evaluate(Request("data:text/css,a%7B%7D", "https://example.test/root.svg", "data:text/css,a%7B%7D", SvgResourceKind.StyleSheet, SvgMimeTypes.Css));
        SvgResourcePolicyDecision fileImage = policy.Evaluate(Request("file:///tmp/a.png", "", "file:///tmp/a.png", SvgResourceKind.Image, SvgMimeTypes.Png));
        SvgResourcePolicyDecision custom = policy.Evaluate(Request("memory:image", "", "memory:image", SvgResourceKind.Image, SvgMimeTypes.Png));
        policy.AllowedSchemes.Add("memory");
        SvgResourcePolicyDecision customAllowed = policy.Evaluate(Request("memory:image", "", "memory:image", SvgResourceKind.Image, SvgMimeTypes.Png));

        Assert.Multiple(() =>
        {
            Assert.That(dataImage.Allowed, Is.True);
            Assert.That(dataCss.Reason, Is.EqualTo(SvgResourceDenialReason.DataUriDenied));
            Assert.That(fileImage.Reason, Is.EqualTo(SvgResourceDenialReason.FileUriDenied));
            Assert.That(custom.Reason, Is.EqualTo(SvgResourceDenialReason.SchemeDenied));
            Assert.That(customAllowed.Allowed, Is.True);
        });
    }

    [Test]
    public void CrossOriginRequestsRequireTheKindSpecificOptIn()
    {
        SvgResourcePolicy policy = new SvgResourcePolicy();
        SvgResourcePolicyRequest image = Request("https://cdn.test/a.png", "https://example.test/root.svg", "https://cdn.test/a.png", SvgResourceKind.Image, SvgMimeTypes.Png);
        SvgResourcePolicyRequest style = Request("https://cdn.test/a.css", "https://example.test/root.svg", "https://cdn.test/a.css", SvgResourceKind.StyleSheet, SvgMimeTypes.Css);

        SvgResourcePolicyDecision imageDenied = policy.Evaluate(image);
        SvgResourcePolicyDecision styleDenied = policy.Evaluate(style);
        policy.AllowCrossOriginUris = true;
        policy.AllowCrossOriginImports = true;
        SvgResourcePolicyDecision imageAllowed = policy.Evaluate(image);
        SvgResourcePolicyDecision styleAllowed = policy.Evaluate(style);

        Assert.Multiple(() =>
        {
            Assert.That(imageDenied.Reason, Is.EqualTo(SvgResourceDenialReason.CrossOriginDenied));
            Assert.That(styleDenied.Reason, Is.EqualTo(SvgResourceDenialReason.CrossOriginDenied));
            Assert.That(imageDenied.IsSameOrigin, Is.False);
            Assert.That(imageAllowed.Allowed, Is.True);
            Assert.That(styleAllowed.Allowed, Is.True);
        });
    }

    [Test]
    public void FileRootsBlockTraversalSiblingAndRemoteAuthorities()
    {
        SvgResourcePolicy policy = new SvgResourcePolicy();
        policy.AllowFileUris = true;
        policy.FileRootUri = "/safe/root";

        SvgResourcePolicyDecision child = policy.Evaluate(Request("file:///safe/root/icons/a.svg", "", "file:///safe/root/icons/a.svg", SvgResourceKind.Image, SvgMimeTypes.Svg));
        SvgResourcePolicyDecision traversal = policy.Evaluate(Request("file:///safe/root/%2e%2e/secret.svg", "", "file:///safe/root/%2e%2e/secret.svg", SvgResourceKind.Image, SvgMimeTypes.Svg));
        SvgResourcePolicyDecision sibling = policy.Evaluate(Request("file:///safe/root-other/a.svg", "", "file:///safe/root-other/a.svg", SvgResourceKind.Image, SvgMimeTypes.Svg));
        SvgResourcePolicyDecision remote = policy.Evaluate(Request("file://server/safe/root/a.svg", "", "file://server/safe/root/a.svg", SvgResourceKind.Image, SvgMimeTypes.Svg));

        Assert.Multiple(() =>
        {
            Assert.That(child.Allowed, Is.True);
            Assert.That(child.IsWithinFileRoot, Is.True);
            Assert.That(traversal.Reason, Is.EqualTo(SvgResourceDenialReason.FileRootDenied));
            Assert.That(sibling.Reason, Is.EqualTo(SvgResourceDenialReason.FileRootDenied));
            Assert.That(remote.Reason, Is.EqualTo(SvgResourceDenialReason.RemoteFileDenied));
        });
    }

    [Test]
    public void MediaTypesAreCheckedByKindWithExplicitOverrides()
    {
        SvgResourcePolicy policy = new SvgResourcePolicy();
        SvgResourcePolicyDecision png = policy.Evaluate(Request("https://example.test/a.png", "https://example.test/root.svg", "https://example.test/a.png", SvgResourceKind.Image, "IMAGE/PNG; profile=screen"));
        SvgResourcePolicyDecision cssImage = policy.Evaluate(Request("https://example.test/a.css", "https://example.test/root.svg", "https://example.test/a.css", SvgResourceKind.Image, SvgMimeTypes.Css));
        SvgResourcePolicyDecision custom = policy.Evaluate(Request("https://example.test/a.bin", "https://example.test/root.svg", "https://example.test/a.bin", SvgResourceKind.Other, "application/x-fixture"));
        policy.AllowedMediaTypes.Add("application/x-fixture");
        SvgResourcePolicyDecision listed = policy.Evaluate(Request("https://example.test/a.bin", "https://example.test/root.svg", "https://example.test/a.bin", SvgResourceKind.Other, "application/x-fixture"));
        policy.AllowUnknownMediaTypes = true;
        SvgResourcePolicyDecision unknown = policy.Evaluate(Request("https://example.test/b.bin", "https://example.test/root.svg", "https://example.test/b.bin", SvgResourceKind.Other, SvgMimeTypes.Binary));
        SvgResourcePolicyDecision unknownImage = policy.Evaluate(Request("https://example.test/a.bmp", "https://example.test/root.svg", "https://example.test/a.bmp", SvgResourceKind.Image, "image/bmp"));
        SvgResourcePolicyDecision knownWrongKind = policy.Evaluate(Request("https://example.test/c.css", "https://example.test/root.svg", "https://example.test/c.css", SvgResourceKind.Image, SvgMimeTypes.Css));

        Assert.Multiple(() =>
        {
            Assert.That(png.Allowed, Is.True);
            Assert.That(cssImage.Reason, Is.EqualTo(SvgResourceDenialReason.MediaTypeDenied));
            Assert.That(custom.Reason, Is.EqualTo(SvgResourceDenialReason.MediaTypeDenied));
            Assert.That(listed.Allowed, Is.True);
            Assert.That(unknown.Allowed, Is.True);
            Assert.That(unknownImage.Allowed, Is.True);
            Assert.That(knownWrongKind.Reason, Is.EqualTo(SvgResourceDenialReason.MediaTypeDenied));
        });
    }

    [Test]
    public void RedirectsEnforceEnablementCountAndOriginIndependently()
    {
        SvgResourcePolicy policy = new SvgResourcePolicy();
        SvgResourcePolicyRequest sameOrigin = Request("https://example.test/b.png", "https://example.test/a.png", "https://example.test/b.png", SvgResourceKind.Image, SvgMimeTypes.Png);
        sameOrigin.IsRedirect = true;
        sameOrigin.RedirectCount = 2;
        sameOrigin.MaxRedirects = 2;
        SvgResourcePolicyDecision allowed = policy.Evaluate(sameOrigin);
        sameOrigin.RedirectCount = 3;
        SvgResourcePolicyDecision overLimit = policy.Evaluate(sameOrigin);
        sameOrigin.RedirectCount = 1;
        policy.AllowRedirects = false;
        SvgResourcePolicyDecision disabled = policy.Evaluate(sameOrigin);

        SvgResourcePolicy crossPolicy = new SvgResourcePolicy();
        crossPolicy.AllowCrossOriginUris = true;
        SvgResourcePolicyRequest cross = Request("https://cdn.test/b.png", "https://example.test/a.png", "https://cdn.test/b.png", SvgResourceKind.Image, SvgMimeTypes.Png);
        cross.IsRedirect = true;
        cross.RedirectCount = 1;
        SvgResourcePolicyDecision crossDenied = crossPolicy.Evaluate(cross);
        crossPolicy.AllowCrossOriginRedirects = true;
        SvgResourcePolicyDecision crossAllowed = crossPolicy.Evaluate(cross);
        SvgResourcePolicyRequest missingCount = Request("https://example.test/b.png", "https://example.test/a.png", "https://example.test/b.png", SvgResourceKind.Image, SvgMimeTypes.Png);
        missingCount.IsRedirect = true;
        SvgResourcePolicyDecision invalidCount = crossPolicy.Evaluate(missingCount);

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Allowed, Is.True);
            Assert.That(allowed.RedirectCount, Is.EqualTo(2));
            Assert.That(overLimit.Reason, Is.EqualTo(SvgResourceDenialReason.RedirectLimitExceeded));
            Assert.That(disabled.Reason, Is.EqualTo(SvgResourceDenialReason.RedirectDenied));
            Assert.That(crossDenied.Reason, Is.EqualTo(SvgResourceDenialReason.CrossOriginRedirectDenied));
            Assert.That(crossAllowed.Allowed, Is.True);
            Assert.That(invalidCount.Reason, Is.EqualTo(SvgResourceDenialReason.InvalidRequest));
        });
    }

    [Test]
    public void DenialsRetainSourceAndUseReasonSpecificDiagnostics()
    {
        SvgSourceSpan source = new SvgSourceSpan(new SvgSourceUri("root.svg"), 4, 1, 5, 20, 1, 21);
        SvgResourcePolicy policy = new SvgResourcePolicy();
        SvgResourcePolicyRequest request = Request("file:///tmp/a.svg", "", "file:///tmp/a.svg", SvgResourceKind.Document, SvgMimeTypes.Svg);
        request.Source = source;

        SvgResourcePolicyDecision denied = policy.Evaluate(request);
        SvgResourcePolicyDecision invalid = policy.Evaluate(null);

        Assert.Multiple(() =>
        {
            Assert.That(denied.Reason, Is.EqualTo(SvgResourceDenialReason.FileUriDenied));
            Assert.That(denied.Diagnostic.Code, Is.EqualTo(SvgDiagnosticCode.ResourceDenied));
            Assert.That(denied.Diagnostic.Source, Is.SameAs(source));
            Assert.That(denied.Diagnostic.Message, Does.Contain("file URL"));
            Assert.That(invalid.Reason, Is.EqualTo(SvgResourceDenialReason.InvalidRequest));
            Assert.That(invalid.Diagnostic.Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
        });
    }

    [Test]
    public void EveryPolicyOutcomeIsReachableWithItsStableDiagnosticContract()
    {
        List<SvgResourcePolicyDecision> decisions = new List<SvgResourcePolicyDecision>();

        SvgResourcePolicy allowedPolicy = new SvgResourcePolicy();
        decisions.Add(allowedPolicy.Evaluate(Request("image.png", "https://example.test/root.svg", "image.png", SvgResourceKind.Image, SvgMimeTypes.Png)));
        decisions.Add(new SvgResourcePolicy().Evaluate(null));

        SvgResourcePolicy rootPolicy = new SvgResourcePolicy();
        rootPolicy.AllowCallerResolvedRootUri = false;
        decisions.Add(rootPolicy.Evaluate(Request("memory:root", "", "memory:root", SvgResourceKind.RootDocument, SvgMimeTypes.Svg)));

        SvgResourcePolicy sameDocumentPolicy = new SvgResourcePolicy();
        sameDocumentPolicy.AllowSameDocumentUris = false;
        decisions.Add(sameDocumentPolicy.Evaluate(Request("#paint", "https://example.test/root.svg", "https://example.test/root.svg#paint", SvgResourceKind.Document, "")));

        SvgResourcePolicy stylesheetPolicy = new SvgResourcePolicy();
        stylesheetPolicy.AllowImportedStyleSheets = false;
        decisions.Add(stylesheetPolicy.Evaluate(Request("theme.css", "https://example.test/root.svg", "theme.css", SvgResourceKind.StyleSheet, SvgMimeTypes.Css)));

        SvgResourcePolicy relativePolicy = new SvgResourcePolicy();
        relativePolicy.AllowRelativeUris = false;
        decisions.Add(relativePolicy.Evaluate(Request("image.png", "https://example.test/root.svg", "image.png", SvgResourceKind.Image, SvgMimeTypes.Png)));

        decisions.Add(new SvgResourcePolicy().Evaluate(Request("memory:image", "", "memory:image", SvgResourceKind.Image, SvgMimeTypes.Png)));

        SvgResourcePolicy dataPolicy = new SvgResourcePolicy();
        dataPolicy.AllowDataUris = false;
        decisions.Add(dataPolicy.Evaluate(Request("data:image/png;base64,", "", "data:image/png;base64,", SvgResourceKind.Image, SvgMimeTypes.Png)));

        decisions.Add(new SvgResourcePolicy().Evaluate(Request("file:///safe/image.png", "", "file:///safe/image.png", SvgResourceKind.Image, SvgMimeTypes.Png)));

        SvgResourcePolicy remoteFilePolicy = new SvgResourcePolicy();
        remoteFilePolicy.AllowFileUris = true;
        decisions.Add(remoteFilePolicy.Evaluate(Request("file://server/safe/image.png", "", "file://server/safe/image.png", SvgResourceKind.Image, SvgMimeTypes.Png)));

        SvgResourcePolicy fileRootPolicy = new SvgResourcePolicy();
        fileRootPolicy.AllowFileUris = true;
        fileRootPolicy.FileRootUri = "/safe/root";
        decisions.Add(fileRootPolicy.Evaluate(Request("file:///outside/image.png", "", "file:///outside/image.png", SvgResourceKind.Image, SvgMimeTypes.Png)));

        decisions.Add(new SvgResourcePolicy().Evaluate(Request("https://cdn.test/image.png", "https://example.test/root.svg", "https://cdn.test/image.png", SvgResourceKind.Image, SvgMimeTypes.Png)));
        decisions.Add(new SvgResourcePolicy().Evaluate(Request("style.css", "https://example.test/root.svg", "style.css", SvgResourceKind.Image, SvgMimeTypes.Css)));

        SvgResourcePolicy redirectPolicy = new SvgResourcePolicy();
        redirectPolicy.AllowRedirects = false;
        SvgResourcePolicyRequest redirectRequest = Request("image.png", "https://example.test/first.png", "https://example.test/image.png", SvgResourceKind.Image, SvgMimeTypes.Png);
        redirectRequest.IsRedirect = true;
        redirectRequest.RedirectCount = 1;
        decisions.Add(redirectPolicy.Evaluate(redirectRequest));

        SvgResourcePolicy redirectLimitPolicy = new SvgResourcePolicy();
        SvgResourcePolicyRequest redirectLimitRequest = Request("image.png", "https://example.test/first.png", "https://example.test/image.png", SvgResourceKind.Image, SvgMimeTypes.Png);
        redirectLimitRequest.IsRedirect = true;
        redirectLimitRequest.RedirectCount = 2;
        redirectLimitRequest.MaxRedirects = 1;
        decisions.Add(redirectLimitPolicy.Evaluate(redirectLimitRequest));

        SvgResourcePolicy crossRedirectPolicy = new SvgResourcePolicy();
        SvgResourcePolicyRequest crossRedirectRequest = Request("image.png", "https://example.test/first.png", "https://cdn.test/image.png", SvgResourceKind.Image, SvgMimeTypes.Png);
        crossRedirectRequest.IsRedirect = true;
        crossRedirectRequest.RedirectCount = 1;
        decisions.Add(crossRedirectPolicy.Evaluate(crossRedirectRequest));

        Array outcomes = Enum.GetValues(typeof(SvgResourceDenialReason));
        Assert.That(decisions, Has.Count.EqualTo(outcomes.Length));
        int index = 0;
        while (index < decisions.Count)
        {
            SvgResourceDenialReason expected = (SvgResourceDenialReason)outcomes.GetValue(index);
            SvgResourcePolicyDecision decision = decisions[index];
            Assert.That(decision.Reason, Is.EqualTo(expected), "Outcome index " + index);
            Assert.That(decision.Allowed, Is.EqualTo(expected == SvgResourceDenialReason.None), "Allowed index " + index);
            if (expected == SvgResourceDenialReason.None) Assert.That(decision.Diagnostic, Is.Null);
            else Assert.That(decision.Diagnostic, Is.Not.Null);
            index = index + 1;
        }
    }

    static SvgResourcePolicyRequest Request(string authored, string baseUri, string resolved, SvgResourceKind kind, string mediaType)
    {
        SvgResourcePolicyRequest request = new SvgResourcePolicyRequest(authored, baseUri, resolved, kind);
        request.MediaType = mediaType;
        return request;
    }
}
