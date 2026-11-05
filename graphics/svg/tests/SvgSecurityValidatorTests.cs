namespace svg.Tests;

public class SvgSecurityValidatorTests
{
    [Test]
    public void ScriptsHandlersAndScriptUrlsAreReportedWithLocatedRecovery()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><script>run()</script><rect id='handler' onclick='run()'/><a id='link' href='javascript:run()'><text>x</text></a></svg>";

        SvgValidationResult result = Load(source, "https://example.test/root.svg").Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.FindIssues(SvgValidationRule.ScriptContent), Has.Count.EqualTo(1));
            Assert.That(result.FindIssues(SvgValidationRule.EventHandler), Has.Count.EqualTo(1));
            Assert.That(result.FindIssues(SvgValidationRule.ScriptUrl), Has.Count.EqualTo(1));
            Assert.That(result.FindIssues(SvgValidationRule.ScriptUrl).Single().Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Error));
            Assert.That(SecurityIssues(result).All(item => item.Diagnostic.Source != null), Is.True);
            Assert.That(SecurityIssues(result).All(item => item.RecoveryDescription.Length != 0), Is.True);
        });
    }

    [Test]
    public void ForeignSubtreesAndExternalLinksRequireHostManagementOncePerRoot()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:h='http://www.w3.org/1999/xhtml' xmlns:v='urn:vendor'><v:item/><foreignObject><h:div><h:span>x</h:span></h:div></foreignObject><a href='https://example.test/page'><text>go</text></a></svg>";

        SvgValidationResult result = Load(source, "https://example.test/root.svg").Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.FindIssues(SvgValidationRule.ForeignContent), Has.Count.EqualTo(2));
            Assert.That(result.FindIssues(SvgValidationRule.ExternalLink), Has.Count.EqualTo(1));
            Assert.That(result.FindIssues(SvgValidationRule.ForeignContent).All(item => item.Diagnostic.Code == SvgDiagnosticCode.CallbackRequired), Is.True);
            Assert.That(result.FindIssues(SvgValidationRule.ExternalLink).Single().Diagnostic.Code, Is.EqualTo(SvgDiagnosticCode.CallbackRequired));
        });
    }

    [Test]
    public void AllowedExternalImportsImagesAndFontsRemainCallerMediated()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><style>@import url('https://example.test/theme.css');@font-face{font-family:x;src:url('https://example.test/font.woff2')}rect{fill:url('https://example.test/paint.png')}</style><image href='https://example.test/image.png'/></svg>";

        SvgValidationResult result = Load(source, "https://example.test/root.svg").Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Valid, Is.True, Messages(result));
            Assert.That(result.FindIssues(SvgValidationRule.ExternalStyleSheet), Has.Count.EqualTo(1));
            Assert.That(result.FindIssues(SvgValidationRule.ExternalFont), Has.Count.EqualTo(1));
            Assert.That(result.FindIssues(SvgValidationRule.ExternalImage), Has.Count.EqualTo(2));
            Assert.That(SecurityIssues(result).All(item => item.Diagnostic.Severity == SvgDiagnosticSeverity.Warning), Is.True);
            Assert.That(SecurityIssues(result).All(item => item.Diagnostic.Code == SvgDiagnosticCode.CallbackRequired), Is.True);
        });
    }

    [Test]
    public void ResourcePolicyDenialsAreValidationErrors()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><style>@import url('https://cdn.test/theme.css');@font-face{font-family:x;src:url('https://cdn.test/font.woff2')}</style><image href='https://cdn.test/image.png'/></svg>";
        SvgValidationOptions options = new SvgValidationOptions();

        SvgValidationResult result = Load(source, "https://example.test/root.svg").ValidateWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Valid, Is.False);
            Assert.That(result.FindIssues(SvgValidationRule.ExternalStyleSheet).Single().Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Error));
            Assert.That(result.FindIssues(SvgValidationRule.ExternalFont).Single().Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Error));
            Assert.That(result.FindIssues(SvgValidationRule.ExternalImage).Single().Diagnostic.Severity, Is.EqualTo(SvgDiagnosticSeverity.Error));
            Assert.That(SecurityIssues(result).All(item => item.Diagnostic.Code == SvgDiagnosticCode.ResourceDenied), Is.True);
        });
    }

    [Test]
    public void ActiveDataTypesAreRejectedWhilePassiveRasterDataIsAllowed()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><image href='data:image/svg+xml,%3Csvg/%3E'/><image href='data:image/png;base64,AA=='/><rect style=\"fill:url('data:text/html,x')\"/></svg>";

        SvgValidationResult result = Load(source, "https://example.test/root.svg").Validate();
        List<SvgValidationIssue> issues = result.FindIssues(SvgValidationRule.UnsafeDataUrl);

        Assert.Multiple(() =>
        {
            Assert.That(issues, Has.Count.EqualTo(2), Messages(result));
            Assert.That(issues.All(item => item.Diagnostic.Severity == SvgDiagnosticSeverity.Error), Is.True);
            Assert.That(issues.All(item => item.Diagnostic.Code == SvgDiagnosticCode.ResourceDenied), Is.True);
            Assert.That(result.FindIssues(SvgValidationRule.ExternalImage), Is.Empty);
        });
    }

    [Test]
    public void AuthoredDeclarationBoundCountsInlineCssAndPresentationAttributes()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect opacity='1' style='fill:red;stroke:none'/></svg>", "https://example.test/root.svg");
        SvgValidationOptions options = new SvgValidationOptions();
        options.MaximumAuthoredDeclarations = 2;

        SvgValidationResult result = document.ValidateWithOptions(options);
        SvgValidationIssue issue = result.FindIssues(SvgValidationRule.ExcessiveDeclarations).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Valid, Is.False);
            Assert.That(issue.Diagnostic.Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(issue.Attribute, Is.Not.Null);
            Assert.That(issue.Attribute.LocalName, Is.EqualTo(SvgAttributeNames.Style));
            Assert.That(issue.RecoveryDescription, Does.Contain("configured security bound"));
        });
    }

    [Test]
    public void InvalidSecurityOptionsFailBeforeValidation()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'/>", "https://example.test/root.svg");
        SvgValidationOptions missingPolicy = new SvgValidationOptions();
        missingPolicy.ResourcePolicy = null;
        SvgValidationOptions invalidBound = new SvgValidationOptions();
        invalidBound.MaximumAuthoredDeclarations = 0;

        SvgValidationResult first = document.ValidateWithOptions(missingPolicy);
        SvgValidationResult second = document.ValidateWithOptions(invalidBound);

        Assert.Multiple(() =>
        {
            Assert.That(first.Error, Is.EqualTo(SvgValidationError.InvalidArgument));
            Assert.That(second.Error, Is.EqualTo(SvgValidationError.InvalidArgument));
        });
    }

    static List<SvgValidationIssue> SecurityIssues(SvgValidationResult result)
    {
        return result.Issues.Where(item => item.Rule >= SvgValidationRule.ScriptContent && item.Rule <= SvgValidationRule.ExcessiveDeclarations).ToList();
    }

    static SvgDocument Load(string source, string baseUri)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = baseUri;
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Messages(SvgValidationResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
