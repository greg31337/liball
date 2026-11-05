namespace svg.Tests;

public class SvgNestedDocumentLoaderTests
{
    [Test]
    public void ReferenceResolverLoadsExternalFragmentWithIndependentBaseAndNestedProvenance()
    {
        SvgDocument root = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='assets/icons.svg#mark'/></svg>", "https://example.test/docs/root.svg");
        SvgResourceRequest observed = null;
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.ResourceProvider = new SvgResourceProvider(request =>
        {
            observed = request;
            return Response("<svg xmlns='http://www.w3.org/2000/svg'><symbol id='mark'/></svg>", "https://example.test/cdn/icons.svg", 1);
        });
        SvgReferenceResolveOptions options = ReferenceOptions(loadOptions);

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNodeAndOptions(root.DocumentElement.Children[0], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(SvgReferenceStatus.Resolved));
            Assert.That(result.Target.LocalName, Is.EqualTo("symbol"));
            Assert.That(result.TargetDocument.BaseUri, Is.EqualTo("https://example.test/cdn/icons.svg"));
            Assert.That(root.BaseUri, Is.EqualTo("https://example.test/docs/root.svg"));
            Assert.That(observed.SourceUri, Is.EqualTo("assets/icons.svg#mark"));
            Assert.That(observed.NormalizedUri, Is.EqualTo("https://example.test/docs/assets/icons.svg"));
            Assert.That(observed.ResourceKind, Is.EqualTo(SvgResourceKind.Document));
            Assert.That(observed.ReferenceDepth, Is.EqualTo(1));
            Assert.That(result.TargetDocument.Source.Parent, Is.SameAs(root.Source));
            Assert.That(result.TargetDocument.Source.ReferenceSpan, Is.SameAs(root.DocumentElement.Children[0].Attributes[0].SourceSpan));
            Assert.That(result.Target.SourceSpan.Source, Is.SameAs(result.TargetDocument.Source));
            Assert.That(result.TargetProvenance, Has.Count.EqualTo(2));
            Assert.That(options.NestedDocumentOptions.Context.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(options.NestedDocumentOptions.Context.Resources.LoadedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void NestedRelativeReferencesResolveAgainstEachDocumentsFinalBaseUri()
    {
        SvgDocument root = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='icons.svg#outer'/></svg>", "https://example.test/docs/root.svg");
        List<string> requested = new List<string>();
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.ResourceProvider = new SvgResourceProvider(request =>
        {
            requested.Add(request.NormalizedUri);
            if (request.NormalizedUri.EndsWith("/icons.svg", StringComparison.Ordinal))
            {
                return Response("<svg xmlns='http://www.w3.org/2000/svg'><symbol id='outer'><use href='child.svg#inner'/></symbol></svg>", "https://example.test/cdn/icons.svg", 1);
            }
            return Response("<svg xmlns='http://www.w3.org/2000/svg'><g id='inner'/></svg>", "https://example.test/cdn/child.svg", 0);
        });
        SvgReferenceResolveOptions options = ReferenceOptions(loadOptions);

        SvgReferenceResult outer = new SvgReferenceResolver().ResolveWithNodeAndOptions(root.DocumentElement.Children[0], options);
        SvgReferenceResult inner = new SvgReferenceResolver().ResolveWithNodeAndOptions(outer.Target.Children[0], options);

        Assert.Multiple(() =>
        {
            Assert.That(outer.Success, Is.True);
            Assert.That(inner.Success, Is.True);
            Assert.That(requested, Is.EqualTo(new[]
            {
                "https://example.test/docs/icons.svg",
                "https://example.test/cdn/child.svg"
            }));
            Assert.That(inner.BaseUri, Is.EqualTo("https://example.test/cdn/icons.svg"));
            Assert.That(inner.TargetDocument.BaseUri, Is.EqualTo("https://example.test/cdn/child.svg"));
            Assert.That(inner.TargetDocument.Source.Parent, Is.SameAs(outer.TargetDocument.Source));
            Assert.That(inner.TargetProvenance, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void CrossOriginPolicyDenialOccursBeforeTheCallerCallback()
    {
        SvgDocument root = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='https://other.test/icons.svg#mark'/></svg>", "https://example.test/root.svg");
        bool invoked = false;
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.ResourceProvider = new SvgResourceProvider(request =>
        {
            invoked = true;
            return Response("<svg xmlns='http://www.w3.org/2000/svg'><g id='mark'/></svg>", request.NormalizedUri, 0);
        });

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNodeAndOptions(root.DocumentElement.Children[0], ReferenceOptions(loadOptions));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(SvgReferenceStatus.Invalid));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.ResourceDenied));
            Assert.That(result.Diagnostics[0].Source, Is.SameAs(root.DocumentElement.Children[0].Attributes[0].SourceSpan));
            Assert.That(invoked, Is.False);
        });
    }

    [Test]
    public void NestedParsingInheritsTreeLimitsAndLocatesFailuresInTheNestedResource()
    {
        SvgDocument root = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='large.svg'/></svg>", "https://example.test/root.svg");
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.Limits.Tree.MaxNodes = 2;
        loadOptions.ResourceProvider = new SvgResourceProvider(request => Response("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/></g></svg>", request.NormalizedUri, 0));

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNodeAndOptions(root.DocumentElement.Children[0], ReferenceOptions(loadOptions));
        SvgDiagnostic diagnostic = result.Diagnostics.Single(value => value.Code == SvgDiagnosticCode.InputLimitExceeded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.NestedDocument.LoadResult.Success, Is.False);
            Assert.That(diagnostic.Source.Source.Uri, Is.EqualTo("https://example.test/large.svg"));
            Assert.That(diagnostic.Source.Source.Parent, Is.SameAs(root.Source));
            Assert.That(diagnostic.ResourcePath, Does.StartWith("0/"));
        });
    }

    [Test]
    public void MissingNestedFragmentRetainsTheParsedDocumentAndBothSourceLocations()
    {
        SvgDocument root = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='icons.svg#missing'/></svg>", "https://example.test/root.svg");
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.ResourceProvider = new SvgResourceProvider(request => Response("<svg xmlns='http://www.w3.org/2000/svg'><g id='present'/></svg>", request.NormalizedUri, 0));

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNodeAndOptions(root.DocumentElement.Children[0], ReferenceOptions(loadOptions));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(SvgReferenceStatus.TargetNotFound));
            Assert.That(result.NestedDocument.Document, Is.Not.Null);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidReference));
            Assert.That(result.Diagnostics[0].Source, Is.SameAs(root.DocumentElement.Children[0].Attributes[0].SourceSpan));
            Assert.That(result.Diagnostics[0].RelatedSpans.Single().Source, Is.SameAs(result.NestedDocument.Document.Source));
        });
    }

    [Test]
    public void ResolvedUseExpansionStopsRecursiveExternalDocumentCycles()
    {
        SvgDocument root = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='host'/><use href='a.svg#outer'/></svg>", "https://example.test/root.svg");
        int callbackCount = 0;
        SvgResolveOptions options = new SvgResolveOptions();
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            callbackCount = callbackCount + 1;
            return Response("<svg xmlns='http://www.w3.org/2000/svg'><symbol id='outer'><use href='root.svg#host'/></symbol></svg>", request.NormalizedUri, 0);
        });

        SvgResolvedDocument result = new SvgDocumentResolver().ResolveWithOptions(root, options);
        SvgDiagnostic cycle = result.Diagnostics.Single(value => value.Code == SvgDiagnosticCode.ReferenceCycle);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(result.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(result.Resources.LoadedCount, Is.EqualTo(1));
            Assert.That(cycle.Source.Source.Uri, Is.EqualTo("https://example.test/a.svg"));
            Assert.That(cycle.Source.Source.Parent, Is.SameAs(root.Source));
            Assert.That(cycle.RelatedSpans, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RepeatedFragmentsShareOneNestedAcquisitionAndSelectIndependentTargets()
    {
        SvgDocument root = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='icons.svg#first'/><use href='./icons.svg#second'/></svg>", "https://example.test/root.svg");
        int callbackCount = 0;
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.ResourceProvider = new SvgResourceProvider(request =>
        {
            callbackCount = callbackCount + 1;
            return Response("<svg xmlns='http://www.w3.org/2000/svg'><g id='first'/><g id='second'/></svg>", request.NormalizedUri, 0);
        });
        SvgReferenceResolveOptions options = ReferenceOptions(loadOptions);

        SvgReferenceResult first = new SvgReferenceResolver().ResolveWithNodeAndOptions(root.DocumentElement.Children[0], options);
        SvgReferenceResult second = new SvgReferenceResolver().ResolveWithNodeAndOptions(root.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(first.Target.Attributes.Single(attribute => attribute.LocalName == SvgAttributeNames.Id).Value, Is.EqualTo("first"));
            Assert.That(second.Target.Attributes.Single(attribute => attribute.LocalName == SvgAttributeNames.Id).Value, Is.EqualTo("second"));
            Assert.That(first.TargetDocument, Is.Not.SameAs(second.TargetDocument));
            Assert.That(first.TargetDocument.BaseUri, Is.EqualTo(second.TargetDocument.BaseUri));
            Assert.That(first.NestedDocument.Acquisition.CacheHit, Is.False);
            Assert.That(second.NestedDocument.Acquisition.CacheHit, Is.True);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(options.NestedDocumentOptions.Context.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(options.NestedDocumentOptions.Context.Resources.LoadedCount, Is.EqualTo(1));
            Assert.That(first.TargetDocument.Source.ReferenceSpan, Is.Not.SameAs(second.TargetDocument.Source.ReferenceSpan));
        });
    }

    [Test]
    public void NestedRedirectIsRevalidatedAgainstFinalOriginBeforeParsing()
    {
        SvgDocument root = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='icons.svg#mark'/></svg>", "https://example.test/root.svg");
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.ResourceProvider = new SvgResourceProvider(request => Response("<svg xmlns='http://www.w3.org/2000/svg'><g id='mark'/></svg>", "https://cdn.test/icons.svg", 1));

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNodeAndOptions(root.DocumentElement.Children[0], ReferenceOptions(loadOptions));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.NestedDocument.InitialPolicyDecision.Allowed, Is.True);
            Assert.That(result.NestedDocument.Acquisition.Success, Is.True);
            Assert.That(result.NestedDocument.FinalPolicyDecision.Allowed, Is.False);
            Assert.That(result.NestedDocument.FinalPolicyDecision.Reason, Is.EqualTo(SvgResourceDenialReason.CrossOriginRedirectDenied));
            Assert.That(result.NestedDocument.LoadResult, Is.Null);
            Assert.That(result.NestedDocument.Document, Is.Null);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.ResourceDenied));
        });
    }

    static SvgReferenceResolveOptions ReferenceOptions(SvgLoadOptions loadOptions)
    {
        SvgNestedDocumentOptions nested = new SvgNestedDocumentOptions();
        nested.LoadOptions = loadOptions;
        nested.Context = new SvgNestedDocumentContext(loadOptions.Limits.Resources);
        SvgReferenceResolveOptions result = new SvgReferenceResolveOptions();
        result.NestedDocumentOptions = nested;
        result.MaxUriCharacters = loadOptions.Limits.Resources.MaxUriCharacters;
        result.MaxBaseDepth = loadOptions.Limits.Tree.MaxDepth;
        return result;
    }

    static SvgResourceResponse Response(string xml, string finalUri, int redirects)
    {
        SvgResourceResponse response = new SvgResourceResponse();
        response.Bytes = System.Text.Encoding.UTF8.GetBytes(xml);
        response.ActualMediaType = SvgMimeTypes.Svg;
        response.FinalUri = finalUri;
        response.RedirectCount = redirects;
        response.Cacheable = true;
        return response;
    }

    static SvgDocument Load(string source, string baseUri)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = baseUri;
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
