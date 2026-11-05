namespace svg.Tests;

public class SvgFeatureAnalysisTests
{
    [Test]
    public void DocumentAnalysisReportsEveryStatusAndUniqueDynamicFeatureUsage()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:h='http://www.w3.org/1999/xhtml'>" +
            "<rect fill='red'/><foreignObject><h:div/></foreignObject><script>run()</script>" +
            "<altGlyphDef/><altGlyph/><image href='https://example.test/image.png'/>" +
            "<image href='data:image/jpeg;base64,AA=='/><style>@keyframes pulse{from{opacity:0}to{opacity:1}}.a{animation:pulse 1s}</style></svg>";
        SvgDocument document = Load(source, "https://example.test/root.svg");
        SvgFeatureAnalysisOptions options = new SvgFeatureAnalysisOptions();
        options.ResourcePolicy.AllowHttpsUris = false;

        SvgFeatureAnalysisResult result = document.AnalyzeFeaturesWithOptions(options);
        List<SvgFeatureEntry> entries = result.FeatureReport.Snapshot();
        SvgCapabilities capabilities = new SvgCapabilities();
        string png = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.ImageFormat, "png");
        string resourceCallback = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.HostCallback, "external-resource");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.ImplementedCount, Is.GreaterThan(0));
            Assert.That(result.CallbackRequiredCount, Is.GreaterThan(0));
            Assert.That(result.PreserveOnlyCount, Is.GreaterThan(0));
            Assert.That(result.PolicyDisabledCount, Is.GreaterThan(0));
            Assert.That(result.UnsupportedCount, Is.GreaterThan(0));
            Assert.That(result.FeatureReport.FilterByStatus(SvgFeatureStatus.Implemented), Is.Not.Empty);
            Assert.That(result.FeatureReport.FilterByStatus(SvgFeatureStatus.CallbackRequired), Is.Not.Empty);
            Assert.That(result.FeatureReport.FilterByStatus(SvgFeatureStatus.PreserveOnly), Is.Not.Empty);
            Assert.That(result.FeatureReport.FilterByStatus(SvgFeatureStatus.PolicyDisabled), Is.Not.Empty);
            Assert.That(result.FeatureReport.FilterByStatus(SvgFeatureStatus.Unsupported), Is.Not.Empty);
            Assert.That(entries.All(item => item.Source != null), Is.True);
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.Shapes));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.PaintServers));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.CssCascade));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.ForeignContent));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.ScriptPreservation));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.ScriptExecution));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.ExternalResources));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.HostImageCodec));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(SvgFeatureIdentifiers.CssAnimation));
            Assert.That(entries.Any(item => item.Identifier == png && item.Status == SvgFeatureStatus.PolicyDisabled), Is.True);
            Assert.That(entries.Any(item => item.Identifier == resourceCallback && item.Status == SvgFeatureStatus.PolicyDisabled), Is.True);
            Assert.That(result.DynamicallyUsedFeatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(result.DynamicallyUsedFeatures.Count));
            Assert.That(result.FeatureReport.Diagnostics.Select(item => item.Code), Does.Contain(SvgDiagnosticCode.CallbackRequired));
            Assert.That(result.FeatureReport.Diagnostics.Select(item => item.Code), Does.Contain(SvgDiagnosticCode.PreservedFeature));
            Assert.That(result.FeatureReport.Diagnostics.Select(item => item.Code), Does.Contain(SvgDiagnosticCode.ResourceDenied));
            Assert.That(result.FeatureReport.Diagnostics.Select(item => item.Code), Does.Contain(SvgDiagnosticCode.UnsupportedFeature));
        });
    }

    [Test]
    public void HostCapabilitiesUpgradeCallbackFeaturesWhilePolicyStillTakesPrecedence()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><foreignObject/><image href='data:image/jpeg;base64,AA=='/><image href='https://example.test/image.png'/></svg>";
        SvgDocument document = Load(source, "https://example.test/root.svg");
        SvgFeatureAnalysisOptions options = new SvgFeatureAnalysisOptions();
        options.HostCapabilities.Add(SvgFeatureIdentifiers.ForeignContent);
        options.HostCapabilities.Add(SvgFeatureIdentifiers.HostImageCodec);
        options.HostCapabilities.Add(SvgFeatureIdentifiers.ExternalResources);
        options.ResourcePolicy.AllowHttpsUris = false;

        SvgFeatureAnalysisResult result = document.AnalyzeFeaturesWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FeatureReport.Entries.Any(item => item.Identifier == SvgFeatureIdentifiers.ForeignContent && item.Status == SvgFeatureStatus.Implemented), Is.True);
            Assert.That(result.FeatureReport.Entries.Any(item => item.Identifier == SvgFeatureIdentifiers.HostImageCodec && item.Status == SvgFeatureStatus.Implemented), Is.True);
            Assert.That(result.FeatureReport.Entries.Any(item => item.Identifier == SvgFeatureIdentifiers.ExternalResources && item.Status == SvgFeatureStatus.PolicyDisabled), Is.True);
            Assert.That(result.FeatureReport.Entries.Any(item => item.Identifier == SvgFeatureIdentifiers.ExternalResources && item.Status == SvgFeatureStatus.Implemented), Is.False);
        });
    }

    [Test]
    public void FeatureAnalysisIsVersionAwareAndReturnsStableBoundFailures()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/></g></svg>");
        SvgFeatureAnalysisResult current = document.AnalyzeFeatures();
        SvgFeatureAnalysisOptions limited = new SvgFeatureAnalysisOptions();
        limited.MaximumNodes = 1;
        SvgFeatureAnalysisOptions invalid = new SvgFeatureAnalysisOptions();
        invalid.HostCapabilities.Identifiers = null;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        SvgFeatureAnalysisOptions cancelledOptions = new SvgFeatureAnalysisOptions();
        cancelledOptions.CancellationToken = cancellation.Token;

        SvgFeatureAnalysisResult exceeded = document.AnalyzeFeaturesWithOptions(limited);
        SvgFeatureAnalysisResult malformed = document.AnalyzeFeaturesWithOptions(invalid);
        SvgFeatureAnalysisResult missing = new SvgCapabilities().AnalyzeDocumentWithDocument(null);
        SvgFeatureAnalysisResult explicitResult = new SvgCapabilities().AnalyzeDocumentWithDocumentAndOptions(document, new SvgFeatureAnalysisOptions());
        SvgFeatureAnalysisResult cancelled = new SvgCapabilities().AnalyzeDocumentWithDocumentAndOptions(document, cancelledOptions);
        document.MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(current.Success, Is.True, Messages(current));
            Assert.That(current.IsCurrent(), Is.False);
            Assert.That(exceeded.Error, Is.EqualTo(SvgFeatureAnalysisError.LimitExceeded));
            Assert.That(exceeded.FeatureReport.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(malformed.Error, Is.EqualTo(SvgFeatureAnalysisError.InvalidArgument));
            Assert.That(missing.Error, Is.EqualTo(SvgFeatureAnalysisError.InvalidArgument));
            Assert.That(explicitResult.Success, Is.True, Messages(explicitResult));
            Assert.That(cancelled.Error, Is.EqualTo(SvgFeatureAnalysisError.Cancelled));
            Assert.That(cancelled.InspectedNodes, Is.Zero);
            Assert.That(cancelled.FeatureReport.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
        });
    }

    [Test]
    public void ReportsGranularUsedFeaturesAndDerivesPolicyAndCallbackStatuses()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><style>" +
            "@font-face{font-family:x;src:url(data:font/woff2;base64,AA==)}" +
            "@media screen{@supports (display:inline){.a:hover>rect[fill]{opacity:.5}}}" +
            "@keyframes fade{from{opacity:0}to{opacity:1}}</style>" +
            "<filter><feGaussianBlur/></filter><rect class='a' style='fill:red;opacity:.5'>" +
            "<animate attributeName='opacity' from='0' to='1'/></rect>" +
            "<image href='data:image/jpeg;base64,AA=='/><foreignObject/><a href='https://example.test/'><rect/></a></svg>";
        SvgDocument document = Load(source);
        SvgCapabilities capabilities = new SvgCapabilities();
        SvgFeatureAnalysisResult result = document.AnalyzeFeatures();
        string jpeg = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.ImageFormat, "jpeg");
        string imageCallback = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.HostCallback, "image-codec");
        string foreignCallback = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.HostCallback, "foreign-content");
        string navigationCallback = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.HostCallback, "navigation");
        SvgFeatureAnalysisOptions enabledOptions = new SvgFeatureAnalysisOptions();
        enabledOptions.HostCapabilities.Add(imageCallback);
        enabledOptions.HostCapabilities.Add(foreignCallback);
        enabledOptions.HostCapabilities.Add(navigationCallback);
        enabledOptions.AllowExternalLinks = true;
        SvgFeatureAnalysisResult enabled = document.AnalyzeFeaturesWithOptions(enabledOptions);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.Element, "rect")));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.CssProperty, "opacity")));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.CssAtRule, "media")));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.CssSelector, "pseudo-hover")));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.CssSelector, "combinator-child")));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.CssSelector, "attribute-exists")));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.FilterPrimitive, "feGaussianBlur")));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.AnimationValue, "number")));
            Assert.That(result.DynamicallyUsedFeatures, Does.Contain(capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.FontFormat, "woff2")));
            Assert.That(result.FeatureReport.Entries.Any(entry => entry.Identifier == jpeg && entry.Status == SvgFeatureStatus.CallbackRequired), Is.True);
            Assert.That(result.FeatureReport.Entries.Any(entry => entry.Identifier == foreignCallback && entry.Status == SvgFeatureStatus.CallbackRequired), Is.True);
            Assert.That(result.FeatureReport.Entries.Any(entry => entry.Identifier == navigationCallback && entry.Status == SvgFeatureStatus.PolicyDisabled), Is.True);
            Assert.That(enabled.FeatureReport.Entries.Any(entry => entry.Identifier == jpeg && entry.Status == SvgFeatureStatus.Implemented), Is.True);
            Assert.That(enabled.FeatureReport.Entries.Any(entry => entry.Identifier == foreignCallback && entry.Status == SvgFeatureStatus.Implemented), Is.True);
            Assert.That(enabled.FeatureReport.Entries.Any(entry => entry.Identifier == navigationCallback && entry.Status == SvgFeatureStatus.Implemented), Is.True);
        });
    }

    static SvgDocument Load(string source, string baseUri = "")
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = baseUri;
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Messages(SvgFeatureAnalysisResult result)
    {
        return string.Join(" | ", result.FeatureReport.Diagnostics.Select(item => item.Message));
    }
}
