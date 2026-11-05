namespace svg.Tests;

public class SvgSanitizerTests
{
    [Test]
    public void SecureDefaultsRemoveScriptsHandlersAndForeignSubtreesWithoutMutatingSource()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:h='http://www.w3.org/1999/xhtml' xmlns:v='urn:vendor'><?xml-stylesheet href='https://example.test/theme.css'?><script id='script'>run()</script><rect id='handler' onclick='run()'/><foreignObject id='foreign'><h:div>unsafe</h:div></foreignObject><v:item id='vendor'/><circle id='safe'/></svg>";
        SvgDocument document = Load(source);

        SvgSanitizationResult result = document.Sanitize();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Changed, Is.True);
            Assert.That(result.Document, Is.Not.SameAs(document));
            Assert.That(result.RemovedNodes, Is.EqualTo(4));
            Assert.That(result.RemovedAttributes, Is.EqualTo(3));
            Assert.That(Find(result.Document, "script"), Is.Null);
            Assert.That(Find(result.Document, "foreign"), Is.Null);
            Assert.That(Find(result.Document, "vendor"), Is.Null);
            Assert.That(Attribute(Find(result.Document, "handler"), "onclick"), Is.Null);
            Assert.That(Find(result.Document, "safe"), Is.Not.Null);
            Assert.That(Find(document, "script"), Is.Not.Null);
            Assert.That(Attribute(Find(document, "handler"), "onclick"), Is.Not.Null);
            Assert.That(result.Diagnostics.All(item => item.Code == SvgDiagnosticCode.LossyChange), Is.True);
            Assert.That(result.Diagnostics.All(item => item.Source != null), Is.True);
        });
    }

    [Test]
    public void ScriptUnsafeDataExternalResourcesAndNavigationAreRemovedButLocalReferencesRemain()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><path id='target'/></defs>" +
            "<a id='scriptLink' href='javascript:run()'/><a id='externalLink' href='https://example.test/page'/><a id='localLink' href='#target'/>" +
            "<animate id='animatedLink' href='#localLink' attributeName='href' values='#target;javascript:run()'/>" +
            "<use id='localUse' href='#target'/><use id='absoluteUse' href='https://example.test/root.svg#target'/>" +
            "<image id='remote' href='https://example.test/image.png'/><image id='activeData' href='data:image/svg+xml,%3Csvg/%3E'/><image id='passiveData' href='data:image/png;base64,AA=='/></svg>";
        SvgDocument document = Load(source, "https://example.test/root.svg");

        SvgSanitizationResult result = document.Sanitize();

        Assert.Multiple(() =>
        {
            Assert.That(Attribute(Find(result.Document, "scriptLink"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(result.Document, "externalLink"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(result.Document, "localLink"), SvgAttributeNames.Href).Value, Is.EqualTo("#target"));
            Assert.That(Find(result.Document, "animatedLink"), Is.Null);
            Assert.That(Attribute(Find(result.Document, "localUse"), SvgAttributeNames.Href).Value, Is.EqualTo("#target"));
            Assert.That(Attribute(Find(result.Document, "absoluteUse"), SvgAttributeNames.Href).Value, Is.EqualTo("https://example.test/root.svg#target"));
            Assert.That(Attribute(Find(result.Document, "remote"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(result.Document, "activeData"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(result.Document, "passiveData"), SvgAttributeNames.Href).Value, Does.StartWith("data:image/png"));
            Assert.That(result.RemovedAttributes, Is.EqualTo(4));
        });
    }

    [Test]
    public void EmbeddedInlineAndPresentationCssRemoveDeniedUrlsAndRetainSafeContent()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><clipPath id='clip'><rect/></clipPath></defs>" +
            "<style>@import url('https://example.test/theme.css');@font-face{font-family:x;src:url('https://example.test/font.woff2')}.script{fill:url('javascript:run()')}.external{fill:url('https://example.test/paint.png')}.safe{fill:red}</style>" +
            "<rect id='inline' style=\"fill:red;stroke:url('https://example.test/stroke.png');opacity:.5\"/>" +
            "<rect id='presentation' fill='url(https://example.test/fill.png)' clip-path='url(#clip)'/></svg>";
        SvgDocument document = Load(source, "https://example.test/root.svg");

        SvgSanitizationResult result = document.Sanitize();
        SvgNode style = result.Document.DocumentElement.Children.Single(item => item.ElementKind == SvgElementKind.Style);
        string stylesheet = new SvgNodeApi().GetTextContent(style);
        string inline = Attribute(Find(result.Document, "inline"), SvgAttributeNames.Style).Value;

        Assert.Multiple(() =>
        {
            Assert.That(stylesheet, Does.Contain(".safe"));
            Assert.That(stylesheet, Does.Not.Contain("@import"));
            Assert.That(stylesheet, Does.Not.Contain("@font-face"));
            Assert.That(stylesheet, Does.Not.Contain("javascript"));
            Assert.That(stylesheet, Does.Not.Contain("paint.png"));
            Assert.That(inline, Does.Contain("fill:red"));
            Assert.That(inline, Does.Contain("opacity:.5"));
            Assert.That(inline, Does.Not.Contain("stroke"));
            Assert.That(Attribute(Find(result.Document, "presentation"), "fill"), Is.Null);
            Assert.That(Attribute(Find(result.Document, "presentation"), "clip-path").Value, Is.EqualTo("url(#clip)"));
            Assert.That(result.RemovedCssRules, Is.EqualTo(4));
            Assert.That(result.RemovedCssDeclarations, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExplicitPoliciesCanRetainApprovedResourcesAndExternalLinks()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><a id='link' href='https://example.test/page'/><image id='image' href='https://example.test/image.png'/></svg>", "https://example.test/root.svg");
        SvgSanitizationOptions options = new SvgSanitizationOptions();
        options.AllowExternalLinks = true;
        options.ResourcePolicy.AllowHttpsUris = true;
        options.ResourcePolicy.AllowCrossOriginUris = false;

        SvgSanitizationResult result = document.SanitizeWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Changed, Is.False);
            Assert.That(Attribute(Find(result.Document, "link"), SvgAttributeNames.Href), Is.Not.Null);
            Assert.That(Attribute(Find(result.Document, "image"), SvgAttributeNames.Href), Is.Not.Null);
        });
    }

    [Test]
    public void ElementAttributeAndNamespaceAllowListsNarrowRetainedMarkup()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:x='urn:vendor'><rect id='kept' fill='red' stroke='blue' x:note='remove'/><circle id='removed'/></svg>";
        SvgSanitizationOptions options = new SvgSanitizationOptions();
        options.AllowList.Elements.Add(SvgElementNames.Svg);
        options.AllowList.Elements.Add(SvgElementNames.Rect);
        options.AllowList.Attributes.Add("xmlns");
        options.AllowList.Attributes.Add(SvgAttributeNames.Id);
        options.AllowList.Attributes.Add("fill");
        options.AllowList.Namespaces.Add("");
        options.AllowList.Namespaces.Add(SvgNamespaces.Svg);
        options.AllowList.Namespaces.Add(SvgNamespaces.Xmlns);

        SvgSanitizationResult result = Load(source).SanitizeWithOptions(options);
        SvgNode kept = Find(result.Document, "kept");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(kept, Is.Not.Null);
            Assert.That(Attribute(kept, "fill").Value, Is.EqualTo("red"));
            Assert.That(Attribute(kept, "stroke"), Is.Null);
            Assert.That(Attribute(kept, "note"), Is.Null);
            Assert.That(Find(result.Document, "removed"), Is.Null);
        });
    }

    [Test]
    public void CssPropertyAndFunctionAllowListsApplyToStyleSheetsInlineStylesAndPresentationAttributes()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'>" +
            "<style>.safe{fill:rgb(1 2 3)}.function{stroke:calc(1px + 2px)}.property{opacity:.5}</style>" +
            "<rect id='inline' style='fill:rgb(1 2 3);stroke:calc(1px + 2px);opacity:.5'/>" +
            "<rect id='presentation' fill='rgb(1 2 3)' stroke='calc(1px + 2px)' opacity='.5'/></svg>";
        SvgSanitizationOptions options = new SvgSanitizationOptions();
        options.AllowList.CssProperties.Add("fill");
        options.AllowList.CssProperties.Add("stroke");
        options.AllowList.CssFunctions.Add("rgb");

        SvgSanitizationResult result = Load(source).SanitizeWithOptions(options);
        SvgNode style = result.Document.DocumentElement.Children.Single(item => item.ElementKind == SvgElementKind.Style);
        string stylesheet = new SvgNodeApi().GetTextContent(style);
        string inline = Attribute(Find(result.Document, "inline"), SvgAttributeNames.Style).Value;
        SvgNode presentation = Find(result.Document, "presentation");

        Assert.Multiple(() =>
        {
            Assert.That(stylesheet, Does.Contain(".safe"));
            Assert.That(stylesheet, Does.Not.Contain(".function"));
            Assert.That(stylesheet, Does.Not.Contain(".property"));
            Assert.That(inline, Does.Contain("fill:rgb"));
            Assert.That(inline, Does.Not.Contain("stroke"));
            Assert.That(inline, Does.Not.Contain("opacity"));
            Assert.That(Attribute(presentation, "fill"), Is.Not.Null);
            Assert.That(Attribute(presentation, "stroke"), Is.Null);
            Assert.That(Attribute(presentation, "opacity"), Is.Null);
        });
    }

    [Test]
    public void UriSchemeOriginAndMediaTypeAllowListsRestrictResourcesWithoutRestrictingLinkMediaTypes()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'>" +
            "<image id='good' href='https://good.test/image.png'/><image id='origin' href='https://other.test/image.png'/>" +
            "<image id='scheme' href='http://good.test/image.png'/><image id='media' href='https://good.test/image.jpg'/>" +
            "<a id='link' href='https://good.test/page'/><a id='otherLink' href='https://other.test/page'/></svg>";
        SvgSanitizationOptions options = new SvgSanitizationOptions();
        options.AllowExternalLinks = true;
        options.ResourcePolicy.AllowHttpUris = true;
        options.ResourcePolicy.AllowHttpsUris = true;
        options.ResourcePolicy.AllowCrossOriginUris = true;
        options.AllowList.UriSchemes.Add("https");
        options.AllowList.Origins.Add("https://good.test");
        options.AllowList.MediaTypes.Add(SvgMimeTypes.Png);

        SvgSanitizationResult result = Load(source, "https://good.test/root.svg").SanitizeWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(Attribute(Find(result.Document, "good"), SvgAttributeNames.Href), Is.Not.Null);
            Assert.That(Attribute(Find(result.Document, "origin"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(result.Document, "scheme"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(result.Document, "media"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(result.Document, "link"), SvgAttributeNames.Href), Is.Not.Null);
            Assert.That(Attribute(Find(result.Document, "otherLink"), SvgAttributeNames.Href), Is.Null);
        });
    }

    [Test]
    public void LinkMetadataAndDataUrlSwitchesRemoveTheirContentIndependently()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><metadata id='metadata'>author</metadata><path id='target'/>" +
            "<a id='link' href='#target'/><image id='data' href='data:image/png;base64,AA=='/></svg>";
        SvgSanitizationOptions options = new SvgSanitizationOptions();
        options.AllowList.Links = false;
        options.AllowList.Metadata = false;
        options.AllowList.DataUrls = false;

        SvgSanitizationResult result = Load(source).SanitizeWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(Find(result.Document, "metadata"), Is.Null);
            Assert.That(Attribute(Find(result.Document, "link"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(result.Document, "data"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Find(result.Document, "target"), Is.Not.Null);
        });
    }

    [Test]
    public void NullAllowListCollectionsAreRejected()
    {
        SvgSanitizationOptions options = new SvgSanitizationOptions();
        options.AllowList.CssFunctions = null;

        SvgSanitizationResult result = Load("<svg xmlns='http://www.w3.org/2000/svg'/>").SanitizeWithOptions(options);

        Assert.That(result.Error, Is.EqualTo(SvgSanitizationError.InvalidArgument));
    }

    [Test]
    public void CleanupRepairsBrokenReferencesAndReportsEverySourceLocatedChange()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><clipPath id='gone'><script/></clipPath></defs>" +
            "<g id='empty'><script/></g><title id='label'>Label</title><use id='use' href='#gone'/>" +
            "<rect id='shape' aria-labelledby='label missing' clip-path='url(#gone)' style='fill:url(#gone);stroke:red'/>" +
            "<style>.broken{fill:url(#gone)}.safe{fill:red}</style></svg>";

        SvgSanitizationResult result = Load(source).Sanitize();
        SvgNode shape = Find(result.Document, "shape");
        SvgNode style = result.Document.DocumentElement.Children.Single(item => item.ElementKind == SvgElementKind.Style);
        string stylesheet = new SvgNodeApi().GetTextContent(style);
        SvgSanitizationResult repeated = result.Document.Sanitize();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(Find(result.Document, "gone"), Is.Null);
            Assert.That(Find(result.Document, "empty"), Is.Null);
            Assert.That(Attribute(Find(result.Document, "use"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(shape, "clip-path"), Is.Null);
            Assert.That(Attribute(shape, "aria-labelledby").Value, Is.EqualTo("label"));
            Assert.That(Attribute(shape, SvgAttributeNames.Style).Value, Is.EqualTo("stroke:red"));
            Assert.That(stylesheet, Does.Contain(".safe"));
            Assert.That(stylesheet, Does.Not.Contain(".broken"));
            Assert.That(result.Changes.Any(item => item.Kind == SvgSanitizationChangeKind.ReferenceRepaired), Is.True);
            Assert.That(result.Changes.Any(item => item.Kind == SvgSanitizationChangeKind.CssDeclarationRemoved), Is.True);
            Assert.That(result.Changes.Any(item => item.Kind == SvgSanitizationChangeKind.CssRuleRemoved), Is.True);
            Assert.That(result.Changes.All(item => item.Source != null), Is.True, string.Join(" | ", result.Changes.Where(item => item.Source == null).Select(item => item.Kind + ":" + item.NodeName + ":" + item.AttributeName)));
            Assert.That(repeated.Success, Is.True, Messages(repeated));
            Assert.That(repeated.Changed, Is.False);
            Assert.That(repeated.Changes, Is.Empty);
        });
    }

    [Test]
    public void NamespaceRepairRestoresRequiredBindingsAndRemovesUnusedDeclarations()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' xmlns:v='urn:unused'><path id='target'/><use id='use' xlink:href='#target'/></svg>");
        SvgNode root = document.DocumentElement;
        SvgAttribute defaultNamespace = root.Attributes.Single(item => item.QualifiedName == "xmlns");
        SvgAttribute xlinkNamespace = root.Attributes.Single(item => item.QualifiedName == "xmlns:xlink");
        Assert.That(new SvgNodeApi().RemoveAttribute(root, defaultNamespace).Success, Is.True);
        Assert.That(new SvgNodeApi().RemoveAttribute(root, xlinkNamespace).Success, Is.True);

        SvgSanitizationResult result = document.Sanitize();
        SvgNode sanitizedRoot = result.Document.DocumentElement;
        SvgNode use = Find(result.Document, "use");
        SvgSanitizationResult repeated = result.Document.Sanitize();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(sanitizedRoot.Attributes.Single(item => item.QualifiedName == "xmlns").Value, Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(use.Attributes.Single(item => item.QualifiedName == "xmlns:xlink").Value, Is.EqualTo(SvgNamespaces.XLink));
            Assert.That(sanitizedRoot.Attributes.Any(item => item.QualifiedName == "xmlns:v"), Is.False);
            Assert.That(result.Changes.Count(item => item.Kind == SvgSanitizationChangeKind.NamespaceAdded), Is.EqualTo(2));
            Assert.That(result.Changes.Count(item => item.Kind == SvgSanitizationChangeKind.NamespaceRemoved), Is.EqualTo(1));
            Assert.That(result.Changes.Where(item => item.Kind == SvgSanitizationChangeKind.NamespaceAdded).All(item => item.Source != null), Is.True, string.Join(" | ", result.Changes.Where(item => item.Kind == SvgSanitizationChangeKind.NamespaceAdded && item.Source == null).Select(item => item.NodeName + ":" + item.AttributeName)));
            Assert.That(repeated.Success, Is.True, Messages(repeated));
            Assert.That(repeated.Changed, Is.False);
            Assert.That(repeated.Changes, Is.Empty);
        });
    }

    [Test]
    public void SanitizedOutputHasNoSecurityValidationFindingsAndIsIdempotent()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><script>run()</script><rect onclick='run()'/><image href='https://example.test/image.png'/></svg>", "https://example.test/root.svg");

        SvgSanitizationResult first = document.Sanitize();
        SvgSanitizationResult second = first.Document.Sanitize();
        SvgValidationResult validation = first.Document.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changed, Is.False);
            Assert.That(second.RemovedNodes, Is.Zero);
            Assert.That(second.RemovedAttributes, Is.Zero);
            Assert.That(SecurityIssues(validation), Is.Empty, string.Join(" | ", validation.Diagnostics.Select(item => item.Message)));
        });
    }

    [Test]
    public void InvalidAndExceededBoundsReturnStableFailures()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect/></g></svg>");
        SvgSanitizationOptions invalid = new SvgSanitizationOptions();
        invalid.ResourcePolicy = null;
        SvgSanitizationOptions limited = new SvgSanitizationOptions();
        limited.MaximumNodes = 1;

        SvgSanitizationResult first = document.SanitizeWithOptions(invalid);
        SvgSanitizationResult second = document.SanitizeWithOptions(limited);
        SvgSanitizationResult third = new SvgSanitizer().SanitizeWithDocument(null);

        Assert.Multiple(() =>
        {
            Assert.That(first.Error, Is.EqualTo(SvgSanitizationError.InvalidArgument));
            Assert.That(second.Error, Is.EqualTo(SvgSanitizationError.LimitExceeded));
            Assert.That(second.Diagnostics.Last().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(third.Error, Is.EqualTo(SvgSanitizationError.InvalidArgument));
            Assert.That(document.IsDirty, Is.False);
        });
    }

    static List<SvgValidationIssue> SecurityIssues(SvgValidationResult result)
    {
        return result.Issues.Where(item => item.Rule >= SvgValidationRule.ScriptContent && item.Rule <= SvgValidationRule.ExcessiveDeclarations).ToList();
    }

    static SvgDocument Load(string source)
    {
        return Load(source, "");
    }

    static SvgDocument Load(string source, string baseUri)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = baseUri;
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static SvgNode Find(SvgDocument document, string id)
    {
        return document.Index.GetById(id);
    }

    static SvgAttribute Attribute(SvgNode node, string name)
    {
        if (node == null) return null;
        return node.Attributes.FirstOrDefault(item => item.LocalName == name);
    }

    static string Messages(SvgSanitizationResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
