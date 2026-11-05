namespace svg.Tests;

public class SvgReferenceValidatorTests
{
    [Test]
    public void DuplicateMissingAndWrongTypeFragmentReferencesAreDistinctAndLocated()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><path id='same' d='M0 0'/><rect id='same'/><rect id='wrongTarget'/></defs><use id='missing' href='#absent'/><text><textPath id='wrong' href='#wrongTarget'>x</textPath></text></svg>";

        SvgValidationResult result = Load(source).Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(result.FindIssues(SvgValidationRule.DuplicateId), Has.Count.EqualTo(1));
            Assert.That(result.FindIssues(SvgValidationRule.MissingReference), Has.Count.EqualTo(1));
            Assert.That(result.FindIssues(SvgValidationRule.WrongReferenceType), Has.Count.EqualTo(1));
            Assert.That(result.FindIssues(SvgValidationRule.DuplicateId).Single().Diagnostic.RelatedSpans, Has.Count.EqualTo(1));
            Assert.That(ReferenceIssues(result).All(item => item.Diagnostic.Source != null), Is.True);
            Assert.That(ReferenceIssues(result).All(item => item.RecoveryDescription.Length != 0), Is.True);
        });
    }

    [Test]
    public void UsePaintFilterAndNestedEffectCyclesAreReported()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs>" +
            "<style>#styledA{clip-path:url(#styledB)}#styledB{clip-path:url(#styledA)}</style>" +
            "<linearGradient id='a' href='#b'/><linearGradient id='b' href='#a'/>" +
            "<filter id='f1' href='#f2'/><filter id='f2' href='#f1'/>" +
            "<clipPath id='clip' clip-path='url(#clip)'><rect/></clipPath>" +
            "<clipPath id='styledA'><rect/></clipPath><clipPath id='styledB'><rect/></clipPath>" +
            "<use id='loop' href='#loop'/>" +
            "</defs></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> cycles = result.FindIssues(SvgValidationRule.ReferenceCycle);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(cycles.Count, Is.GreaterThanOrEqualTo(5), Messages(result));
            Assert.That(cycles.All(item => item.Diagnostic.Code == SvgDiagnosticCode.ReferenceCycle), Is.True);
            Assert.That(cycles.All(item => item.RecoveryDescription.Contains("Break")), Is.True);
        });
    }

    [Test]
    public void PaintEffectAndFilterInputReferencesUseTheirProcessingRecovery()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs>" +
            "<rect id='shape'/><linearGradient id='paint'><stop offset='0'/></linearGradient>" +
            "<filter id='fx'><feBlend in='missing-output' in2='SourceGraphic'/></filter>" +
            "</defs><rect fill='url(#shape)' clip-path='url(#paint)' filter='url(#absent)'/></svg>";

        SvgValidationResult result = Load(source).Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.FindIssues(SvgValidationRule.PaintReference), Has.Count.EqualTo(1), Messages(result));
            Assert.That(result.FindIssues(SvgValidationRule.EffectReference), Has.Count.EqualTo(1), Messages(result));
            Assert.That(result.FindIssues(SvgValidationRule.MissingReference), Has.Count.EqualTo(1), Messages(result));
            Assert.That(result.FindIssues(SvgValidationRule.FilterInput), Has.Count.EqualTo(1), Messages(result));
            Assert.That(result.FindIssues(SvgValidationRule.FilterInput).Single().Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Warning));
            Assert.That(result.FindIssues(SvgValidationRule.FilterInput).Single().RecoveryDescription, Does.Contain("transparent black"));
        });
    }

    [Test]
    public void AnimationHrefRejectsAnAnimationElementAsItsTarget()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><g><animate id='target' attributeName='opacity'/><animate id='source' href='#target' attributeName='opacity'/></g></svg>";

        SvgValidationResult result = Load(source).Validate();
        SvgValidationIssue issue = result.FindIssues(SvgValidationRule.AnimationTarget).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(issue.Node, Is.Not.Null);
            Assert.That(issue.Diagnostic.Message, Does.Contain("receive animation"));
            Assert.That(issue.RecoveryDescription, Is.Not.Empty);
        });
    }

    [Test]
    public void AcquiredDeclaredAndActualMediaTypeMismatchIsOptionalAndRecoverable()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='asset.bin'/></svg>");
        SvgNode image = document.DocumentElement.Children[0];
        SvgAttribute href = image.Attributes.Single(item => item.LocalName == SvgAttributeNames.Href);
        SvgResourceRequest request = new SvgResourceRequest("https://example.test/asset.bin", SvgMimeTypes.Png, CancellationToken.None);
        request.Source = href.SourceSpan;
        SvgResourceAcquisitionOptions acquisitionOptions = new SvgResourceAcquisitionOptions();
        acquisitionOptions.ResourceProvider = new SvgResourceProvider(value =>
        {
            SvgResourceResponse response = new SvgResourceResponse();
            response.Bytes = new byte[] { 1 };
            response.DeclaredMediaType = "application/octet-stream";
            response.ActualMediaType = "image/png";
            return response;
        });
        SvgResourceAcquisitionResult resource = new SvgResourceResolver().Resolve(request, acquisitionOptions);
        SvgValidationOptions options = new SvgValidationOptions();
        options.Resources.Add(resource);

        SvgValidationResult result = document.ValidateWithOptions(options);
        SvgValidationIssue issue = result.FindIssues(SvgValidationRule.ResourceMediaType).Single();

        Assert.Multiple(() =>
        {
            Assert.That(resource.Success, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.True, Messages(result));
            Assert.That(issue.Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Warning));
            Assert.That(issue.Node, Is.SameAs(image));
            Assert.That(issue.Attribute, Is.SameAs(href));
            Assert.That(issue.Diagnostic.Message, Does.Contain("application/octet-stream"));
            Assert.That(issue.Diagnostic.Message, Does.Contain("image/png"));
        });
    }

    [Test]
    public void CompatibleCrossReferencesPassTogether()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs>" +
            "<path id='path' d='M0 0L1 1'/><linearGradient id='base'><stop offset='0'/></linearGradient><linearGradient id='paint' href='#base'/>" +
            "<clipPath id='clip'><use href='#path'/></clipPath><filter id='filter'><feFlood result='f'/><feBlend in='f' in2='SourceGraphic'/></filter>" +
            "</defs><use href='#path'/><text><textPath href='#path'>x</textPath></text><rect fill='url(#paint)' clip-path='url(#clip)' filter='url(#filter)'><animate href='#path' attributeName='opacity'/></rect></svg>";

        SvgValidationResult result = Load(source).Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(ReferenceIssues(result), Is.Empty, Messages(result));
        });
    }

    static List<SvgValidationIssue> ReferenceIssues(SvgValidationResult result)
    {
        return result.Issues.Where(item => item.Rule >= SvgValidationRule.DuplicateId && item.Rule <= SvgValidationRule.ResourceMediaType).ToList();
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Messages(SvgValidationResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
