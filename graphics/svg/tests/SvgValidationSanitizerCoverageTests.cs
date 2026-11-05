namespace svg.Tests;

public class SvgValidationSanitizerCoverageTests
{
    [Test]
    public void EveryCurrentElementFamilyIsRecognizedAndSanitizable()
    {
        SvgElementCatalog catalog = new SvgCatalogFactory().CreateElementCatalog();
        List<SvgElementDefinition> definitions = catalog.GetEntries();
        HashSet<SvgElementCategory> categories = new HashSet<SvgElementCategory>();
        List<string> unrecognized = new List<string>();
        List<string> failedSanitization = new List<string>();

        foreach (SvgElementDefinition definition in definitions)
        {
            foreach (SvgElementCategory category in definition.Categories) categories.Add(category);
            string content = definition.Kind == SvgElementKind.Svg ? "<svg xmlns='http://www.w3.org/2000/svg' id='subject'/>" : "<svg xmlns='http://www.w3.org/2000/svg'><" + definition.LocalName + " id='subject'/></svg>";
            SvgDocument document = Load(content);
            SvgNode subject = Find(document, "subject");
            SvgValidationResult validation = document.Validate();
            SvgSanitizationResult sanitization = document.Sanitize();
            if (validation.FindIssues(SvgValidationRule.UnknownContent).Any(item => item.Node == subject)) unrecognized.Add(definition.LocalName);
            if (!sanitization.Success) failedSanitization.Add(definition.LocalName);
        }

        Assert.Multiple(() =>
        {
            Assert.That(categories, Is.EquivalentTo(Enum.GetValues<SvgElementCategory>()));
            Assert.That(unrecognized, Is.Empty, string.Join(", ", unrecognized));
            Assert.That(failedSanitization, Is.Empty, string.Join(", ", failedSanitization));
        });
    }

    [Test]
    public void EveryLegacyElementFamilyIsExplicitlyValidatedAndPreservedBySanitization()
    {
        SvgLegacyCatalog catalog = new SvgCatalogFactory().CreateLegacyCatalog();
        List<SvgLegacyFeatureDefinition> definitions = catalog.GetElementEntries();
        List<string> unreported = new List<string>();
        List<string> incompatible = new List<string>();
        List<string> removed = new List<string>();

        foreach (SvgLegacyFeatureDefinition definition in definitions)
        {
            string content = "<svg xmlns='http://www.w3.org/2000/svg'><" + definition.LocalName + " id='subject'/></svg>";
            SvgDocument document = Load(content);
            SvgNode subject = Find(document, "subject");
            SvgValidationResult current = document.Validate();
            SvgValidationOptions options = new SvgValidationOptions();
            options.Profile = SvgValidationProfile.Svg11Compatibility;
            SvgValidationResult compatibility = document.ValidateWithOptions(options);
            SvgSanitizationResult sanitization = document.Sanitize();
            if (!current.Issues.Any(item => item.Node == subject && (item.Rule == SvgValidationRule.LegacyContent || item.Rule == SvgValidationRule.DeprecatedContent))) unreported.Add(definition.LocalName);
            if (compatibility.FindIssues(SvgValidationRule.UnknownContent).Any(item => item.Node == subject)) incompatible.Add(definition.LocalName);
            if (!sanitization.Success || Find(sanitization.Document, "subject") == null) removed.Add(definition.LocalName);
        }

        Assert.Multiple(() =>
        {
            Assert.That(definitions.Select(item => item.Handling).Distinct(), Is.EquivalentTo(Enum.GetValues<SvgLegacyHandling>()));
            Assert.That(unreported, Is.Empty, string.Join(", ", unreported));
            Assert.That(incompatible, Is.Empty, string.Join(", ", incompatible));
            Assert.That(removed, Is.Empty, string.Join(", ", removed));
        });
    }

    [Test]
    public void InvalidValuesReferencesAndCssAreReportedTogetherWithoutStoppingValidation()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 nope 10'><defs>" +
            "<style>rect &gt; {broken;made-up:1;fill:not-a-paint;display:grid}@vendor demo;</style>" +
            "<linearGradient id='cycleA' href='#cycleB'/><linearGradient id='cycleB' href='#cycleA'/>" +
            "<rect id='wrongTarget'/></defs><path d='M0 0 L nope'/><polyline points='0,0 1'/>" +
            "<rect id='duplicate' width='-1' style='--a:var(--b);--b:var(--a);color:var(--a)'><animate attributeName='x' begin='0s;' keyTimes='0;.8;.2'/></rect>" +
            "<circle id='duplicate'/><use href='#missing'/><text><textPath href='#wrongTarget'>x</textPath></text></svg>";

        SvgValidationResult result = Load(source).Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Valid, Is.False);
            Assert.That(result.FindIssues(SvgValidationRule.ListValue), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.PathData), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.ValueRange), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.TimingValue), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.CssSelector), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.CssAtRule), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.CssDeclaration), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.CssProperty), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.CssUnsupportedValue), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.CssVariable), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.DuplicateId), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.MissingReference), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.WrongReferenceType), Is.Not.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.ReferenceCycle), Is.Not.Empty);
            Assert.That(result.Issues.All(item => item.Diagnostic.Source != null), Is.True);
        });
    }

    [Test]
    public void CommonAttackFormsAreRemovedAndOutputIsSecurityCleanAndIdempotent()
    {
        string source = "<?xml-stylesheet href='https://evil.test/theme.css'?><svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'>" +
            "<script id='script'>run()</script><foreignObject id='foreign'><div xmlns='http://www.w3.org/1999/xhtml'>x</div></foreignObject>" +
            "<rect id='handler' oNlOaD='run()'/><a id='mixed' href='jAvAsCrIpT:run()'/><a id='entity' href='java&#115;cript:run()'/>" +
            "<a id='control' href='java&#x0A;script:run()'/><use id='vbscript' xlink:href='VBScript:run()'/>" +
            "<image id='external' href='https://evil.test/image.png'/><image id='activeData' href='data:image/svg+xml,%3Csvg/%3E'/>" +
            "<animate id='animatedHandler' attributeName='onclick' values='run()'/><style>@import url('https://evil.test/a.css');.x{fill:url('javascript:run()')}</style></svg>";
        SvgDocument document = Load(source, "https://safe.test/root.svg");

        SvgValidationResult unsafeValidation = document.Validate();
        SvgSanitizationResult first = document.Sanitize();
        SvgSanitizationResult second = first.Document.Sanitize();
        SvgValidationResult safeValidation = first.Document.Validate();
        SvgNodeApi nodes = new SvgNodeApi();

        Assert.Multiple(() =>
        {
            Assert.That(unsafeValidation.FindIssues(SvgValidationRule.ScriptUrl).Any(item => Id(item.Node) == "control"), Is.True, Messages(unsafeValidation));
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(first.Document.DocumentNode.Children.Any(item => item.Kind == SvgNodeKind.ProcessingInstruction), Is.False);
            Assert.That(Find(first.Document, "script"), Is.Null);
            Assert.That(Find(first.Document, "foreign"), Is.Null);
            Assert.That(Attribute(Find(first.Document, "handler"), "onload"), Is.Null);
            Assert.That(Attribute(Find(first.Document, "mixed"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(first.Document, "entity"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(first.Document, "control"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(first.Document, "vbscript"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(first.Document, "external"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Attribute(Find(first.Document, "activeData"), SvgAttributeNames.Href), Is.Null);
            Assert.That(Find(first.Document, "animatedHandler"), Is.Null);
            Assert.That(SecurityIssues(safeValidation), Is.Empty, Messages(safeValidation));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changed, Is.False);
            Assert.That(second.Changes, Is.Empty);
            Assert.That(nodes.DeepEquals(first.Document.DocumentNode, second.Document.DocumentNode), Is.True);
        });
    }

    [TestCase(false, false, false, false, true)]
    [TestCase(true, true, false, false, true)]
    [TestCase(true, true, true, true, false)]
    [TestCase(true, true, true, false, false)]
    public void LinkResourceOriginAndDataPoliciesComposeIndependently(bool allowLinks, bool allowHttps, bool allowCrossOrigin, bool restrictOrigin, bool allowData)
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><path id='localTarget'/><use id='local' href='#localTarget'/>" +
            "<a id='sameLink' href='https://same.test/page'/><a id='crossLink' href='https://other.test/page'/>" +
            "<image id='sameImage' href='https://same.test/image.png'/><image id='crossImage' href='https://other.test/image.png'/>" +
            "<image id='dataImage' href='data:image/png;base64,AA=='/></svg>";
        SvgSanitizationOptions options = new SvgSanitizationOptions();
        options.AllowExternalLinks = allowLinks;
        options.ResourcePolicy.AllowHttpsUris = allowHttps;
        options.ResourcePolicy.AllowCrossOriginUris = allowCrossOrigin;
        options.AllowList.DataUrls = allowData;
        if (restrictOrigin) options.AllowList.Origins.Add("https://same.test");

        SvgSanitizationResult result = Load(source, "https://same.test/root.svg").SanitizeWithOptions(options);
        bool sameAllowed = allowHttps;
        bool crossAllowed = allowHttps && allowCrossOrigin && !restrictOrigin;
        bool sameLinkAllowed = allowLinks;
        bool crossLinkAllowed = allowLinks && !restrictOrigin;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(Attribute(Find(result.Document, "local"), SvgAttributeNames.Href), Is.Not.Null);
            Assert.That(Attribute(Find(result.Document, "sameImage"), SvgAttributeNames.Href) != null, Is.EqualTo(sameAllowed));
            Assert.That(Attribute(Find(result.Document, "crossImage"), SvgAttributeNames.Href) != null, Is.EqualTo(crossAllowed));
            Assert.That(Attribute(Find(result.Document, "sameLink"), SvgAttributeNames.Href) != null, Is.EqualTo(sameLinkAllowed));
            Assert.That(Attribute(Find(result.Document, "crossLink"), SvgAttributeNames.Href) != null, Is.EqualTo(crossLinkAllowed));
            Assert.That(Attribute(Find(result.Document, "dataImage"), SvgAttributeNames.Href) != null, Is.EqualTo(allowData));
        });
    }

    [Test]
    public void SanitizationPreservesAccessibleNamesDescriptionsAndScalableViewBoxSemantics()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' role='graphics-document' viewBox='0 0 20 10'><title>Sales chart</title><desc>Sales by quarter</desc>" +
            "<path id='target'/><image href='data:image/png;base64,AA==' aria-label='Logo'/><a href='#target'><text>Open chart</text></a>" +
            "<rect role='slider' aria-label='Level' aria-valuemin='0' aria-valuemax='10' aria-valuenow='5' tabindex='0' onclick='change()'/></svg>";

        SvgSanitizationResult sanitization = Load(source).Sanitize();
        SvgValidationResult validation = sanitization.Document.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(sanitization.Success, Is.True, Messages(sanitization));
            Assert.That(sanitization.Document.DocumentElement.Children.Any(item => item.ElementKind == SvgElementKind.Title), Is.True);
            Assert.That(sanitization.Document.DocumentElement.Children.Any(item => item.ElementKind == SvgElementKind.Desc), Is.True);
            Assert.That(Attribute(sanitization.Document.DocumentElement, SvgAttributeNames.ViewBox), Is.Not.Null);
            Assert.That(validation.FindIssues(SvgValidationRule.MissingAccessibleName), Is.Empty, Messages(validation));
            Assert.That(validation.FindIssues(SvgValidationRule.MissingAccessibleDescription), Is.Empty, Messages(validation));
            Assert.That(validation.FindIssues(SvgValidationRule.AriaContradiction), Is.Empty, Messages(validation));
            Assert.That(validation.FindIssues(SvgValidationRule.KeyboardAccessibility), Is.Empty, Messages(validation));
            Assert.That(validation.FindIssues(SvgValidationRule.TitlePreservationRisk), Has.Count.EqualTo(1));
            Assert.That(validation.FindIssues(SvgValidationRule.ViewBoxPreservationRisk), Has.Count.EqualTo(1));
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
        if (document == null) return null;
        return document.Index.GetById(id);
    }

    static SvgAttribute Attribute(SvgNode node, string name)
    {
        if (node == null) return null;
        return node.Attributes.FirstOrDefault(item => item.LocalName == name);
    }

    static string Id(SvgNode node)
    {
        SvgAttribute attribute = Attribute(node, SvgAttributeNames.Id);
        return attribute == null ? "" : attribute.Value;
    }

    static string Messages(SvgValidationResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }

    static string Messages(SvgSanitizationResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
