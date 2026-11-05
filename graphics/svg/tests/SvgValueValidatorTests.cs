namespace svg.Tests;

public class SvgValueValidatorTests
{
    [Test]
    public void ValidTypedListsPathsRangesAndTimingPassTogether()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 10'><defs><filter><feConvolveMatrix order='2' kernelMatrix='1 0 0 1' kernelUnitLength='1 2'/><feTurbulence baseFrequency='.1 .2' numOctaves='2'/></filter></defs><path d='M0 0 L10 10' pathLength='10'/><polyline points='0,0 5,5 10,0'/><rect width='10' height='5' opacity='.5'><animate attributeName='x' begin='0s;click+1s' dur='1s' repeatCount='2' restart='always' keyTimes='0;1' values='0;10'/></rect></svg>";

        SvgValidationResult result = Load(source).Validate();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Valid, Is.True, Messages(result));
            Assert.That(result.FindIssues(SvgValidationRule.TypedValue), Is.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.ValueRange), Is.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.ListValue), Is.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.PathData), Is.Empty);
            Assert.That(result.FindIssues(SvgValidationRule.TimingValue), Is.Empty);
        });
    }

    [Test]
    public void TypedListAndPathSyntaxFailuresUseParserSourceOffsets()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 nope 10'><path id='path' d='M0 0 L nope'/><polyline id='points' points='0,0 1'/></svg>";

        SvgValidationResult result = Load(source).Validate();
        SvgValidationIssue path = result.FindIssues(SvgValidationRule.PathData).Single();
        List<SvgValidationIssue> lists = result.FindIssues(SvgValidationRule.ListValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(path.Attribute.LocalName, Is.EqualTo("d"));
            Assert.That(path.Diagnostic.Source.StartOffset, Is.EqualTo(source.LastIndexOf("nope", StringComparison.Ordinal)));
            Assert.That(path.RecoveryDescription, Does.Contain("segments"));
            Assert.That(lists.Select(item => item.Attribute.LocalName), Is.EquivalentTo(new[] { "viewBox", "points" }));
            Assert.That(lists.All(item => item.Diagnostic.Source != null), Is.True);
            Assert.That(lists.All(item => item.RecoveryDescription.Length != 0), Is.True);
        });
    }

    [Test]
    public void ScalarAndListRangesReportTheOffendingAttributes()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient><stop id='stop' offset='120%'/></linearGradient><filter><feConvolveMatrix id='kernel' order='2' kernelMatrix='1 2 3' kernelUnitLength='1 0' targetX='2'/><feConvolveMatrix id='badOrder' order='2 0' kernelMatrix='1 2'/><feTurbulence id='noise' baseFrequency='-.2' numOctaves='0'/><feSpecularLighting id='light' specularExponent='129'><feDistantLight/></feSpecularLighting></filter></defs><path d='M0 0' pathLength='0'/><rect id='rect' width='-1' height='1' opacity='2' stroke-miterlimit='0'/></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> ranges = result.FindIssues(SvgValidationRule.ValueRange);
        List<SvgValidationIssue> lists = result.FindIssues(SvgValidationRule.ListValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("offset"));
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("order"));
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("kernelUnitLength"));
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("targetX"));
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("baseFrequency"));
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("numOctaves"));
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("specularExponent"));
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("pathLength"));
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("width"));
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("opacity"));
            Assert.That(ranges.Select(item => item.Attribute.LocalName), Does.Contain("stroke-miterlimit"));
            Assert.That(lists.Single().Attribute.LocalName, Is.EqualTo("kernelMatrix"));
            Assert.That(ranges.All(item => item.RecoveryDescription.Length != 0), Is.True);
        });
    }

    [Test]
    public void ListCardinalitiesAreCheckedAfterSuccessfulTypedParsing()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><filter><feConvolveMatrix order='2 2 2' kernelMatrix='1 2 3'/><feTurbulence baseFrequency='.1 .2 .3'/><feGaussianBlur stdDeviation='1 2 3'/></filter></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> issues = result.FindIssues(SvgValidationRule.ListValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(issues.Select(item => item.Attribute.LocalName), Is.EquivalentTo(new[] { "order", "baseFrequency", "stdDeviation" }));
            Assert.That(issues.All(item => item.Diagnostic.Code == SvgDiagnosticCode.InvalidValue), Is.True);
            Assert.That(issues.All(item => item.RecoveryDescription.Length != 0), Is.True);
        });
    }

    [Test]
    public void TimingSyntaxRangesKeywordsAndKeyListsAreLocatedAndRecoverable()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='x' begin='0s;' dur='-1s' repeatCount='0' restart='later' keyTimes='0;.8;.2'/><animateMotion path='M0 0 L10 0' keyPoints='0;2'/></rect></svg>";

        SvgValidationResult result = Load(source).Validate();
        List<SvgValidationIssue> issues = result.FindIssues(SvgValidationRule.TimingValue);
        SvgValidationIssue keyTimes = issues.Single(item => item.Attribute.LocalName == "keyTimes");
        SvgValidationIssue duration = issues.Single(item => item.Attribute.LocalName == "dur");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Valid, Is.False);
            Assert.That(issues.Select(item => item.Attribute.LocalName), Is.EquivalentTo(new[] { "begin", "dur", "repeatCount", "restart", "keyTimes", "keyPoints" }));
            Assert.That(keyTimes.Diagnostic.Source.StartOffset, Is.EqualTo(source.IndexOf(".2'", StringComparison.Ordinal)));
            Assert.That(duration.RecoveryDescription, Does.Contain("indefinite"));
            Assert.That(issues.All(item => item.RecoveryDescription.Length != 0), Is.True);
        });
    }

    [Test]
    public void TimingAndTypedParserBoundsAreValidatedAndReported()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='x' begin='0s;1s'/></rect></svg>");
        SvgValidationOptions bounded = new SvgValidationOptions();
        bounded.MaximumTimingItems = 1;
        SvgValidationOptions invalid = new SvgValidationOptions();
        invalid.MaximumTimingItems = 0;

        SvgValidationResult limited = document.ValidateWithOptions(bounded);
        SvgValidationResult rejected = document.ValidateWithOptions(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.True);
            Assert.That(limited.FindIssues(SvgValidationRule.TimingValue), Has.Count.EqualTo(1));
            Assert.That(limited.FindIssues(SvgValidationRule.TimingValue).Single().RecoveryDescription, Is.Not.Empty);
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Error, Is.EqualTo(SvgValidationError.InvalidArgument));
            Assert.That(rejected.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
        });
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
