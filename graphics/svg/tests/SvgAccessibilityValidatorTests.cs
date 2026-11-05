namespace svg.Tests;

public class SvgAccessibilityValidatorTests
{
    [Test]
    public void RelevantImagesLinksAndInteractiveRolesRequireNamesAndComplexDocumentsRequireDescriptions()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' role='graphics-document' aria-label='Chart'><image id='image' href='data:image/png;base64,AA=='/><a id='link' href='#target'/><rect id='button' role='button' tabindex='0'/><rect id='target'/></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> names = result.FindIssues(SvgValidationRule.MissingAccessibleName);
        SvgValidationIssue description = result.FindIssues(SvgValidationRule.MissingAccessibleDescription).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Valid, Is.True, Messages(result));
            Assert.That(names.Select(item => Id(item.Node)), Is.EqualTo(new[] { "image", "link", "button" }));
            Assert.That(names.All(item => item.Diagnostic.Severity == SvgDiagnosticSeverity.Warning), Is.True);
            Assert.That(description.Node, Is.SameAs(result.Document.DocumentElement));
            Assert.That(AccessibilityIssues(result).All(item => item.Diagnostic.Source != null), Is.True);
            Assert.That(AccessibilityIssues(result).All(item => item.RecoveryDescription.Length != 0), Is.True);
        });
    }

    [Test]
    public void AriaRelationshipsLabelsTitlesDescriptionsAndLinkTextSatisfyRequirements()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' role='graphics-document' aria-labelledby='heading' aria-describedby='details'><text id='heading'>Chart</text><text id='details'>Quarterly totals</text><image aria-label='Logo' href='data:image/png;base64,AA=='/><a href='#heading'><text>Open chart</text></a><rect role='button' tabindex='0'><title>Refresh</title></rect></svg>";

        SvgValidationResult result = Load(source).Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.FindIssues(SvgValidationRule.MissingAccessibleName), Is.Empty, Messages(result));
            Assert.That(result.FindIssues(SvgValidationRule.MissingAccessibleDescription), Is.Empty, Messages(result));
        });
    }

    [Test]
    public void ContradictoryPresentationHiddenRoleAndRangeStatesAreReported()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='presentation' role='presentation' aria-expanded='true'/><rect id='hidden' tabindex='0' aria-hidden='true' aria-label='Hidden'/><rect id='button' role='button' aria-label='Toggle' aria-checked='true'/><rect id='slider' role='slider' aria-label='Level' aria-valuemin='10' aria-valuemax='1' aria-valuenow='20'/></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> issues = result.FindIssues(SvgValidationRule.AriaContradiction);

        Assert.Multiple(() =>
        {
            Assert.That(issues.Select(item => Id(item.Node)), Is.EqualTo(new[] { "presentation", "hidden", "button", "slider" }), Messages(result));
            Assert.That(issues.All(item => item.Diagnostic.Code == SvgDiagnosticCode.InvalidValue), Is.True);
            Assert.That(issues.All(item => item.Attribute != null), Is.True);
        });
    }

    [Test]
    public void LinksExcludedFromSequentialFocusAreReported()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><a id='native' href='#target' tabindex='-1' aria-label='Native'/><rect id='pseudo' role='link' aria-label='Pseudo' onclick='open()'/><a id='good' href='#target' aria-label='Good'/><rect id='target'/></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> issues = result.FindIssues(SvgValidationRule.KeyboardAccessibility);

        Assert.Multiple(() =>
        {
            Assert.That(issues.Select(item => Id(item.Node)), Is.EqualTo(new[] { "native", "pseudo" }));
            Assert.That(issues.All(item => item.Diagnostic.Severity == SvgDiagnosticSeverity.Warning), Is.True);
            Assert.That(issues.All(item => item.RecoveryDescription.Contains("keyboard")), Is.True);
        });
    }

    [Test]
    public void UsedTitlesAndValidViewBoxesCarryPreservationFindings()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' role='graphics-document' viewBox='0 0 20 10'><title>Sales chart</title><desc>Sales by quarter</desc><g aria-label='Override'><title>Unused fallback</title></g></svg>";

        SvgValidationResult result = Load(source).Validate();
        SvgValidationIssue title = result.FindIssues(SvgValidationRule.TitlePreservationRisk).Single();
        SvgValidationIssue viewBox = result.FindIssues(SvgValidationRule.ViewBoxPreservationRisk).Single();

        Assert.Multiple(() =>
        {
            Assert.That(title.Node, Is.SameAs(result.Document.DocumentElement));
            Assert.That(title.Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Info));
            Assert.That(viewBox.Attribute.LocalName, Is.EqualTo(SvgAttributeNames.ViewBox));
            Assert.That(viewBox.Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Info));
            Assert.That(result.Valid, Is.True, Messages(result));
        });
    }

    [Test]
    public void InvalidAccessibilityBoundsFailBeforeValidation()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgValidationOptions missing = new SvgValidationOptions();
        missing.Accessibility = null;
        SvgValidationOptions bounded = new SvgValidationOptions();
        bounded.Accessibility.MaximumReferences = 0;

        SvgValidationResult first = document.ValidateWithOptions(missing);
        SvgValidationResult second = document.ValidateWithOptions(bounded);

        Assert.Multiple(() =>
        {
            Assert.That(first.Error, Is.EqualTo(SvgValidationError.InvalidArgument));
            Assert.That(second.Error, Is.EqualTo(SvgValidationError.InvalidArgument));
        });
    }

    static List<SvgValidationIssue> AccessibilityIssues(SvgValidationResult result)
    {
        return result.Issues.Where(item => item.Rule >= SvgValidationRule.MissingAccessibleName && item.Rule <= SvgValidationRule.ViewBoxPreservationRisk).ToList();
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
        return node.Attributes.FirstOrDefault(item => item.LocalName == SvgAttributeNames.Id)?.Value ?? "";
    }

    static string Messages(SvgValidationResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
