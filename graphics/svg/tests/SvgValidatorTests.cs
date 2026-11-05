namespace svg.Tests;

public class SvgValidatorTests
{
    [Test]
    public void ValidSvg2DocumentPassesCatalogDrivenStructuralValidation()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='50'><defs><linearGradient id='paint'><stop offset='0'/><stop offset='1'/></linearGradient><filter id='effect'><feComponentTransfer><feFuncR type='identity'/><feFuncG type='identity'/><feFuncB type='identity'/><feFuncA type='identity'/></feComponentTransfer></filter></defs><rect id='box' width='10' height='10' fill='url(#paint)' aria-label='Box' data-key='one'/><text id='label'>Hello<tspan> world</tspan></text></svg>";
        SvgDocument document = Load(source);

        SvgValidationResult result = document.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Valid, Is.True, Messages(result));
            Assert.That(result.Error, Is.EqualTo(SvgValidationError.None));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.VisitedNodeCount, Is.GreaterThan(10));
            Assert.That(result.IsCurrent(), Is.True);
        });
    }

    [Test]
    public void NamespacePolicyIsStrictForSvgProfilesAndRecoverableForPreservation()
    {
        SvgDocument document = Load("<svg><g><rect/></g></svg>");
        SvgValidationOptions preservationOptions = Options(SvgValidationProfile.LegacyPreservation);

        SvgValidationResult strict = document.Validate();
        SvgValidationResult preservation = document.ValidateWithOptions(preservationOptions);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.True);
            Assert.That(strict.Valid, Is.False);
            Assert.That(strict.FindIssues(SvgValidationRule.Namespace), Has.Count.EqualTo(3));
            Assert.That(strict.FindIssues(SvgValidationRule.Namespace).All(item => item.Diagnostic.Code == SvgDiagnosticCode.XmlNamespace), Is.True);
            Assert.That(preservation.Success, Is.True);
            Assert.That(preservation.Valid, Is.True, Messages(preservation));
            Assert.That(preservation.WarningCount, Is.EqualTo(3));
            Assert.That(preservation.ErrorCount, Is.Zero);
        });
    }

    [Test]
    public void ContentModelsReportTextIllegalChildrenAndCardinality()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><circle id='circle'>bad<rect/></circle><filter><feMerge id='merge'/><feDiffuseLighting id='light'/></filter></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> issues = result.FindIssues(SvgValidationRule.ContentModel);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(issues.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(issues.Any(item => item.Diagnostic.Message.Contains("Text content")), Is.True);
            Assert.That(issues.Any(item => Id(item.Node) == "" && item.Diagnostic.Message.Contains("rect")), Is.True);
            Assert.That(issues.Any(item => Id(item.Node) == "merge" && item.Diagnostic.Message.Contains("fewer")), Is.True);
            Assert.That(issues.Any(item => Id(item.Node) == "light" && item.Diagnostic.Message.Contains("fewer")), Is.True);
            Assert.That(issues.All(item => item.Diagnostic.Source != null), Is.True);
        });
    }

    [Test]
    public void AttributeApplicabilityAndRequiredAttributesUseElementMetadata()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><image id='image' kernelMatrix='1'/><g><animateTransform id='transform'/></g><filter><feConvolveMatrix id='kernel'/></filter></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> required = result.FindIssues(SvgValidationRule.RequiredAttribute);
        List<SvgValidationIssue> applicable = result.FindIssues(SvgValidationRule.AttributeApplicability);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(required.Select(item => Id(item.Node)), Is.EquivalentTo(new[] { "image", "transform", "kernel" }));
            Assert.That(required.All(item => item.Attribute == null), Is.True);
            Assert.That(applicable, Has.Count.EqualTo(1));
            Assert.That(Id(applicable[0].Node), Is.EqualTo("image"));
            Assert.That(applicable[0].Attribute.LocalName, Is.EqualTo("kernelMatrix"));
            Assert.That(applicable[0].Diagnostic.Source, Is.SameAs(applicable[0].Attribute.SourceSpan));
        });
    }

    [Test]
    public void LegacyFeaturesAreProfileAwareAndRemainExplicitlyReported()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' version='1.1'><animateColor id='color'/><font id='font'/></svg>";
        SvgDocument document = Load(source);

        SvgValidationResult svg2 = document.ValidateWithOptions(Options(SvgValidationProfile.Svg2));
        SvgValidationResult svg11 = document.ValidateWithOptions(Options(SvgValidationProfile.Svg11Compatibility));
        SvgValidationResult preservation = document.ValidateWithOptions(Options(SvgValidationProfile.LegacyPreservation));

        Assert.Multiple(() =>
        {
            Assert.That(svg2.Valid, Is.True, Messages(svg2));
            Assert.That(svg2.FindIssues(SvgValidationRule.DeprecatedContent), Has.Count.EqualTo(2));
            Assert.That(svg2.FindIssues(SvgValidationRule.LegacyContent), Has.Count.EqualTo(1));
            Assert.That(svg2.WarningCount, Is.EqualTo(2));
            Assert.That(svg2.InformationCount, Is.EqualTo(1));
            Assert.That(svg11.Valid, Is.True, Messages(svg11));
            Assert.That(svg11.Issues, Is.Empty);
            Assert.That(preservation.Valid, Is.True, Messages(preservation));
            Assert.That(preservation.InformationCount, Is.EqualTo(3));
            Assert.That(preservation.WarningCount, Is.Zero);
        });
    }

    [Test]
    public void Svg11ProfileRejectsKnownSvg2OnlyContentAndModernHref()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><discard id='discard'/><image id='modern' href='image.png'/><image id='legacy' xlink:href='image.png'/></svg>";

        SvgValidationResult result = Load(source).ValidateWithOptions(Options(SvgValidationProfile.Svg11Compatibility));
        List<SvgValidationIssue> issues = result.FindIssues(SvgValidationRule.ProfileContent);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(issues, Has.Count.EqualTo(2));
            Assert.That(issues.Any(item => Id(item.Node) == "discard"), Is.True);
            Assert.That(issues.Any(item => Id(item.Node) == "modern" && item.Attribute != null && item.Attribute.LocalName == "href"), Is.True);
            Assert.That(issues.Any(item => Id(item.Node) == "legacy"), Is.False);
        });
    }

    [Test]
    public void UnknownAndForeignContentRespectParentModelsAndPreservationProfile()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:v='urn:vendor'><v:outside id='outside'/><mystery id='unknown'/><foreignObject><v:inside id='inside'/></foreignObject></svg>";
        SvgDocument document = Load(source);

        SvgValidationResult strict = document.Validate();
        SvgValidationResult preservation = document.ValidateWithOptions(Options(SvgValidationProfile.LegacyPreservation));

        Assert.Multiple(() =>
        {
            Assert.That(strict.Valid, Is.False);
            Assert.That(strict.FindIssues(SvgValidationRule.Namespace).Select(item => Id(item.Node)), Is.EqualTo(new[] { "outside" }));
            Assert.That(strict.FindIssues(SvgValidationRule.UnknownContent).Select(item => Id(item.Node)), Is.EqualTo(new[] { "unknown" }));
            Assert.That(strict.Issues.Any(item => Id(item.Node) == "inside"), Is.False);
            Assert.That(preservation.Success, Is.True);
            Assert.That(preservation.Valid, Is.True, Messages(preservation));
            Assert.That(preservation.WarningCount, Is.EqualTo(4));
            Assert.That(preservation.FindIssues(SvgValidationRule.ForeignContent), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void FragmentRootsLimitsAndSnapshotVersionsAreDeterministic()
    {
        SvgDocument fragment = Load("<g xmlns='http://www.w3.org/2000/svg' id='root'><rect width='1' height='1'/></g>");
        SvgValidationResult validFragment = fragment.Validate();
        fragment.IsFragment = false;
        SvgValidationResult invalidDocument = fragment.Validate();
        fragment.IsFragment = true;
        SvgValidationOptions limitedOptions = Options(SvgValidationProfile.Svg2);
        limitedOptions.MaximumNodes = 1;
        SvgValidationResult limited = fragment.ValidateWithOptions(limitedOptions);
        SvgValidationResult snapshot = fragment.Validate();
        SvgMutationResult mutation = new SvgNodeApi().AppendAttribute(fragment.DocumentElement, new SvgAttribute("data-change", "", "data-change", "", "yes"));

        Assert.Multiple(() =>
        {
            Assert.That(validFragment.Valid, Is.True, Messages(validFragment));
            Assert.That(invalidDocument.Valid, Is.False);
            Assert.That(invalidDocument.FindIssues(SvgValidationRule.DocumentStructure), Has.Count.EqualTo(1));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Error, Is.EqualTo(SvgValidationError.LimitExceeded));
            Assert.That(limited.Diagnostics.Last().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(snapshot.Success, Is.True);
            Assert.That(mutation.Success, Is.True);
            Assert.That(snapshot.IsCurrent(), Is.False);
        });
    }

    [Test]
    public void IssueLimitStopsValidationWithAStableOperationalFailure()
    {
        SvgDocument document = Load("<svg><g><rect/></g></svg>");
        SvgValidationOptions options = Options(SvgValidationProfile.Svg2);
        options.MaximumIssues = 1;

        SvgValidationResult result = document.ValidateWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgValidationError.LimitExceeded));
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics.Last().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    static SvgValidationOptions Options(SvgValidationProfile profile)
    {
        SvgValidationOptions result = new SvgValidationOptions();
        result.Profile = profile;
        return result;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Id(SvgNode node)
    {
        if (node == null) return "";
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == "id") return attribute.Value;
            index = index + 1;
        }
        return "";
    }

    static string Messages(SvgValidationResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
