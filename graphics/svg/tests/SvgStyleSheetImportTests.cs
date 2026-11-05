namespace svg.Tests;

public class SvgStyleSheetImportTests
{
    [Test]
    public void ParserRetainsImportUriAndMediaAndRejectsMalformedOrLateRules()
    {
        SvgCssParseResult valid = new SvgCssParser().ParseStyleSheetWithValueAndLimits("@import url(\"theme.css\") screen and (color); @namespace s 'urn:s'; s|item {}", new SvgCssLimits());
        SvgCssAtRule import = valid.StyleSheet.Rules[0].AtRule;
        SvgCssParseResult late = new SvgCssParser().ParseStyleSheetWithValueAndLimits("rect {} @import 'late.css';", new SvgCssLimits());
        string[] malformed =
        {
            "@import;",
            "@import '';",
            "@import 'a.css' {}",
            "@import 'a.css'"
        };

        Assert.Multiple(() =>
        {
            Assert.That(valid.StyleSheet.IsValid, Is.True);
            Assert.That(import.Kind, Is.EqualTo(SvgCssAtRuleKind.Import));
            Assert.That(import.ImportUri, Is.EqualTo("theme.css"));
            Assert.That(import.ImportMedia, Is.EqualTo("screen and (color)"));
            Assert.That(late.StyleSheet.IsValid, Is.False);
            Assert.That(late.StyleSheet.Rules[1].Diagnostics.Single().Message, Does.Contain("must precede"));
            foreach (string css in malformed)
            {
                SvgCssParseResult result = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits());
                Assert.That(result.StyleSheet.IsValid, Is.False, css);
                Assert.That(result.StyleSheet.Rules[0].Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidCss), css);
            }
        });
    }

    [Test]
    public void MatchingImportsExpandDepthFirstWithOrderAndSourceProvenance()
    {
        SvgDocument document = Load("<svg><style>@import 'a.css' screen; @import 'skip.css' print; rect { fill:red; }</style></svg>", "https://example.test/art/root.svg");
        List<SvgResourceRequest> requests = new List<SvgResourceRequest>();
        SvgLoadOptions options = ImportOptions(request =>
        {
            requests.Add(request);
            if (request.NormalizedUri.EndsWith("/a.css")) return CssResponse("@import 'nested/b.css'; circle { fill:blue; }", request.NormalizedUri);
            return CssResponse("path { stroke:black; }", request.NormalizedUri);
        });

        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(document, new List<SvgStyleSheet>(), options, media => media != "print");
        SvgCollectedStyleSheet nested = collection.StyleSheets[0];
        SvgCollectedStyleSheet first = collection.StyleSheets[1];
        SvgCollectedStyleSheet root = collection.StyleSheets[2];
        SvgCssAtRule rootImport = root.StyleSheet.Rules[0].AtRule;
        SvgCssAtRule nestedImport = first.StyleSheet.Rules[0].AtRule;

        Assert.Multiple(() =>
        {
            Assert.That(collection.IsValid, Is.True);
            Assert.That(collection.StyleSheets.Select(entry => entry.SourceKind), Is.EqualTo(new[]
            {
                SvgStyleSheetSourceKind.Imported,
                SvgStyleSheetSourceKind.Imported,
                SvgStyleSheetSourceKind.Embedded
            }));
            Assert.That(collection.StyleSheets.Select(entry => entry.CollectionOrder), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(collection.StyleSheets.Select(entry => entry.ImportDepth), Is.EqualTo(new[] { 2, 1, 0 }));
            Assert.That(requests.Select(request => request.NormalizedUri), Is.EqualTo(new[]
            {
                "https://example.test/art/a.css",
                "https://example.test/art/nested/b.css"
            }));
            Assert.That(requests.Select(request => request.ReferenceDepth), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(rootImport.ImportedStyleSheet, Is.SameAs(first.StyleSheet));
            Assert.That(rootImport.ImportResolvedUri, Is.EqualTo(first.ResolvedUri));
            Assert.That(nestedImport.ImportedStyleSheet, Is.SameAs(nested.StyleSheet));
            Assert.That(nested.Media, Is.Empty);
            Assert.That(first.Media, Is.EqualTo("screen"));
            Assert.That(nested.StyleSheet.Source.Source.Parent, Is.SameAs(first.StyleSheet.Source.Source));
            Assert.That(nested.StyleSheet.Source.Source.ReferenceSpan, Is.SameAs(nestedImport.Source));
            Assert.That(nested.ReferenceSource, Is.SameAs(nestedImport.Source));
            Assert.That(collection.Resources.RequestCount, Is.EqualTo(2));
            Assert.That(collection.Resources.LoadedCount, Is.EqualTo(2));
            Assert.That(collection.Resources.EncodedBytes, Is.GreaterThan(0));
        });
    }

    [Test]
    public void SecureImportPolicyDeniesRelativeCrossOriginDataAndFileUrisBeforeCallbacks()
    {
        SvgDocument document = Load("<svg><style>@import 'relative.css'; @import 'https://other.test/x.css'; @import 'data:text/css,a{}'; @import 'file:///tmp/a.css';</style></svg>", "https://example.test/root.svg");
        SvgLoadOptions options = ImportOptions(request => CssResponse("a {}", request.NormalizedUri));
        options.ResourcePolicy.AllowRelativeImportUris = false;

        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(document, new List<SvgStyleSheet>(), options, null);

        Assert.Multiple(() =>
        {
            Assert.That(collection.IsValid, Is.False);
            Assert.That(collection.StyleSheets, Has.Count.EqualTo(1));
            Assert.That(collection.Diagnostics, Has.Count.EqualTo(4));
            Assert.That(collection.Diagnostics.All(diagnostic => diagnostic.Code == SvgDiagnosticCode.ResourceDenied), Is.True);
            Assert.That(collection.Diagnostics.All(diagnostic => diagnostic.Source != null), Is.True);
            Assert.That(collection.Resources.RequestCount, Is.Zero);
        });
    }

    [Test]
    public void ImportCyclesAreLocatedAndDoNotIssueRepeatedRequests()
    {
        SvgDocument document = Load("<svg><style>@import 'a.css'; root {}</style></svg>", "https://example.test/root.svg");
        SvgLoadOptions options = ImportOptions(request => CssResponse("@import 'a.css'; imported {}", request.NormalizedUri));

        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(document, new List<SvgStyleSheet>(), options, null);
        SvgDiagnostic cycle = collection.Diagnostics.Single(diagnostic => diagnostic.Code == SvgDiagnosticCode.ReferenceCycle);

        Assert.Multiple(() =>
        {
            Assert.That(collection.IsValid, Is.False);
            Assert.That(collection.StyleSheets, Has.Count.EqualTo(2));
            Assert.That(collection.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(collection.Resources.LoadedCount, Is.EqualTo(1));
            Assert.That(cycle.Source.Source.Uri, Is.EqualTo("https://example.test/a.css"));
            Assert.That(cycle.ResourcePath, Does.StartWith("0/"));
        });
    }

    [Test]
    public void ImportDepthLimitStopsBeforeRequestingTheTooDeepResource()
    {
        SvgDocument document = Load("<svg><style>@import 'a.css';</style></svg>", "https://example.test/root.svg");
        List<string> requests = new List<string>();
        SvgLoadOptions options = ImportOptions(request =>
        {
            requests.Add(request.NormalizedUri);
            return CssResponse("@import 'b.css'; a {}", request.NormalizedUri);
        });
        options.Limits.Css.MaxImportDepth = 1;

        SvgStyleSheetCollection collection = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(document, new List<SvgStyleSheet>(), options, null);

        Assert.Multiple(() =>
        {
            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.That(collection.StyleSheets, Has.Count.EqualTo(2));
            Assert.That(collection.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(collection.Diagnostics[0].Message, Does.Contain("MaxImportDepth"));
        });
    }

    [Test]
    public void CallbackDiagnosticsMediaTypeAndMissingCallbackProduceLocatedFailures()
    {
        SvgDocument document = Load("<svg><style>@import 'bad.css';</style></svg>", "https://example.test/root.svg");
        SvgLoadOptions wrongType = ImportOptions(request =>
        {
            SvgResourceResponse response = CssResponse("not css", request.NormalizedUri);
            response.ActualMediaType = "text/plain";
            response.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CompatibilityRecovery, "callback warning", null));
            return response;
        });
        SvgStyleSheetCollection rejected = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(document, new List<SvgStyleSheet>(), wrongType, null);
        SvgLoadOptions missing = new SvgLoadOptions();
        SvgStyleSheetCollection noCallback = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(document, new List<SvgStyleSheet>(), missing, null);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new[]
            {
                SvgDiagnosticCode.CompatibilityRecovery,
                SvgDiagnosticCode.ResourceFailed
            }));
            Assert.That(rejected.Diagnostics.All(diagnostic => diagnostic.Source != null), Is.True);
            Assert.That(rejected.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(rejected.Resources.LoadedCount, Is.Zero);
            Assert.That(noCallback.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.CallbackRequired));
            Assert.That(noCallback.Resources.RequestCount, Is.Zero);
        });
    }

    [Test]
    public void CancellationStopsCollectionAndMediaBoundariesBeforeResourceCallbacks()
    {
        SvgDocument plainDocument = Load("<svg><style>rect { fill:red; }</style></svg>", "https://example.test/plain.svg");
        int preCancelledProviderCalls = 0;
        System.Threading.CancellationTokenSource preCancelledSource = new System.Threading.CancellationTokenSource();
        preCancelledSource.Cancel();
        SvgLoadOptions preCancelledOptions = ImportOptions(request =>
        {
            preCancelledProviderCalls = preCancelledProviderCalls + 1;
            return CssResponse("circle {}", request.NormalizedUri);
        });
        preCancelledOptions.CancellationToken = preCancelledSource.Token;

        SvgStyleSheetCollection preCancelled = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(plainDocument, new List<SvgStyleSheet>(), preCancelledOptions, null);

        SvgDocument importDocument = Load("<svg><style>@import 'theme.css' screen; rect {}</style></svg>", "https://example.test/import.svg");
        int mediaCalls = 0;
        int providerCalls = 0;
        System.Threading.CancellationTokenSource mediaCancellationSource = new System.Threading.CancellationTokenSource();
        SvgLoadOptions mediaCancellationOptions = ImportOptions(request =>
        {
            providerCalls = providerCalls + 1;
            return CssResponse("circle {}", request.NormalizedUri);
        });
        mediaCancellationOptions.CancellationToken = mediaCancellationSource.Token;
        SvgStyleSheetCollection cancelledByMedia = new SvgStyleSheetCollector().CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(importDocument, new List<SvgStyleSheet>(), mediaCancellationOptions, media =>
        {
            mediaCalls = mediaCalls + 1;
            mediaCancellationSource.Cancel();
            return true;
        });

        Assert.Multiple(() =>
        {
            Assert.That(preCancelled.IsValid, Is.False);
            Assert.That(preCancelled.StyleSheets, Has.Count.EqualTo(1));
            Assert.That(preCancelled.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
            Assert.That(preCancelled.Diagnostics[0].Source, Is.SameAs(plainDocument.DocumentElement.Children[0].SourceSpan));
            Assert.That(preCancelledProviderCalls, Is.Zero);
            Assert.That(preCancelled.Resources.RequestCount, Is.Zero);

            Assert.That(cancelledByMedia.IsValid, Is.False);
            Assert.That(cancelledByMedia.StyleSheets, Has.Count.EqualTo(1));
            Assert.That(cancelledByMedia.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
            Assert.That(cancelledByMedia.Diagnostics[0].Source, Is.SameAs(cancelledByMedia.StyleSheets[0].StyleSheet.Rules[0].Source));
            Assert.That(mediaCalls, Is.EqualTo(1));
            Assert.That(providerCalls, Is.Zero);
            Assert.That(cancelledByMedia.Resources.RequestCount, Is.Zero);
        });
    }

    static SvgDocument Load(string source, string baseUri)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = baseUri;
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static SvgLoadOptions ImportOptions(Func<SvgResourceRequest, SvgResourceResponse> callback)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.ResourceProvider = new SvgResourceProvider(callback);
        return options;
    }

    static SvgResourceResponse CssResponse(string css, string finalUri)
    {
        SvgResourceResponse response = new SvgResourceResponse();
        response.Bytes = System.Text.Encoding.UTF8.GetBytes(css);
        response.ActualMediaType = SvgMimeTypes.Css;
        response.FinalUri = finalUri;
        return response;
    }
}
