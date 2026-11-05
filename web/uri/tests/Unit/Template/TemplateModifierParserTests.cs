namespace uri.Tests;

public class TemplateModifierParserTests
{
    [TestCase(1)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(999)]
    [TestCase(9999)]
    public void ValidPrefixLengthsPopulateTheCompleteVariableSpecification(
        int prefixLength)
    {
        string text = "{Some%20Name:" + prefixLength + "}";

        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Template.Level, Is.EqualTo(UriTemplateLevel.Level4));
            UriTemplateVariable variable = result.Template.Parts[0].Variables[0];
            Assert.That(variable.Name, Is.EqualTo("Some%20Name"));
            Assert.That(variable.HasPrefix, Is.True);
            Assert.That(variable.PrefixLength, Is.EqualTo(prefixLength));
            Assert.That(variable.Explode, Is.False);
            Assert.That(variable.Range.Offset, Is.EqualTo(1));
            Assert.That(variable.Range.Length, Is.EqualTo(text.Length - 2));
        });
    }

    [Test]
    public void ExplodeModifierPopulatesTheCompleteVariableSpecification()
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            "{/items*}".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Template.Level, Is.EqualTo(UriTemplateLevel.Level4));
            UriTemplateVariable variable = result.Template.Parts[0].Variables[0];
            Assert.That(variable.Name, Is.EqualTo("items"));
            Assert.That(variable.HasPrefix, Is.False);
            Assert.That(variable.PrefixLength, Is.EqualTo(0));
            Assert.That(variable.Explode, Is.True);
            Assert.That(variable.Range.Offset, Is.EqualTo(2));
            Assert.That(variable.Range.Length, Is.EqualTo(6));
        });
    }

    [Test]
    public void MixedModifiedVariableListPreservesOrderAndExactRanges()
    {
        const string text = "pre{?one:3,two*,three}post";

        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Template.Level, Is.EqualTo(UriTemplateLevel.Level4));
            UriTemplatePart expression = result.Template.Parts[1];
            Assert.That(expression.Variables, Has.Length.EqualTo(3));
            AssertPrefix(expression.Variables[0], "one", 5, 5, 3);
            AssertExplode(expression.Variables[1], "two", 11, 4);
            AssertPlain(expression.Variables[2], "three", 16, 5);
        });
    }

    [TestCase("", UriTemplateLevel.Level1)]
    [TestCase("literal", UriTemplateLevel.Level1)]
    [TestCase("{var}", UriTemplateLevel.Level1)]
    [TestCase("{one}{two}", UriTemplateLevel.Level1)]
    [TestCase("{+var}", UriTemplateLevel.Level2)]
    [TestCase("{#var}", UriTemplateLevel.Level2)]
    [TestCase("{var,other}", UriTemplateLevel.Level3)]
    [TestCase("{+var,other}", UriTemplateLevel.Level3)]
    [TestCase("{.var}", UriTemplateLevel.Level3)]
    [TestCase("{/var}", UriTemplateLevel.Level3)]
    [TestCase("{;var}", UriTemplateLevel.Level3)]
    [TestCase("{?var}", UriTemplateLevel.Level3)]
    [TestCase("{&var}", UriTemplateLevel.Level3)]
    [TestCase("{var:1}", UriTemplateLevel.Level4)]
    [TestCase("{var*}", UriTemplateLevel.Level4)]
    [TestCase("{.one}{+two}{three*}", UriTemplateLevel.Level4)]
    public void TemplateLevelIsTheHighestFeatureLevelUsed(
        string text,
        UriTemplateLevel expected)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Template.Level, Is.EqualTo(expected));
        });
    }

    [TestCase("{var:}", 4)]
    [TestCase("{var:0}", 5)]
    [TestCase("{var:01}", 5)]
    [TestCase("{var:10000}", 9)]
    [TestCase("{var:12345}", 9)]
    [TestCase("{var:x}", 5)]
    [TestCase("{var:1x}", 6)]
    [TestCase("{var:1.}", 6)]
    [TestCase("{var:1%20}", 6)]
    [TestCase("{var*x}", 5)]
    public void InvalidModifierFormsReportTheExactOffendingCharacter(
        string text,
        int offset)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        AssertFailure(
            result,
            offset,
            TemplateParserCore.InvalidModifierMessage);
    }

    [TestCase("{var:1*}", 6)]
    [TestCase("{var*:1}", 5)]
    [TestCase("{var**}", 5)]
    [TestCase("{var:1:2}", 6)]
    public void MultipleModifiersReportTheExactSecondModifier(
        string text,
        int offset)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        AssertFailure(
            result,
            offset,
            TemplateParserCore.IncompatibleModifierMessage);
    }

    [TestCase("{*}", 1)]
    [TestCase("{:1}", 1)]
    [TestCase("{?a,*b}", 4)]
    public void AModifierWithoutAVariableNameIsANameError(
        string text,
        int offset)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.UriTemplate));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(
            UriDiagnosticCode.InvalidTemplateVariable));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(
            UriComponent.TemplateVariable));
        Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(
            TemplateParserCore.InvalidVariableMessage));
    }

    private static void AssertPrefix(
        UriTemplateVariable variable,
        string name,
        int offset,
        int length,
        int prefixLength)
    {
        Assert.That(variable.Name, Is.EqualTo(name));
        Assert.That(variable.HasPrefix, Is.True);
        Assert.That(variable.PrefixLength, Is.EqualTo(prefixLength));
        Assert.That(variable.Explode, Is.False);
        Assert.That(variable.Range.Offset, Is.EqualTo(offset));
        Assert.That(variable.Range.Length, Is.EqualTo(length));
    }

    private static void AssertExplode(
        UriTemplateVariable variable,
        string name,
        int offset,
        int length)
    {
        Assert.That(variable.Name, Is.EqualTo(name));
        Assert.That(variable.HasPrefix, Is.False);
        Assert.That(variable.PrefixLength, Is.EqualTo(0));
        Assert.That(variable.Explode, Is.True);
        Assert.That(variable.Range.Offset, Is.EqualTo(offset));
        Assert.That(variable.Range.Length, Is.EqualTo(length));
    }

    private static void AssertPlain(
        UriTemplateVariable variable,
        string name,
        int offset,
        int length)
    {
        Assert.That(variable.Name, Is.EqualTo(name));
        Assert.That(variable.HasPrefix, Is.False);
        Assert.That(variable.PrefixLength, Is.EqualTo(0));
        Assert.That(variable.Explode, Is.False);
        Assert.That(variable.Range.Offset, Is.EqualTo(offset));
        Assert.That(variable.Range.Length, Is.EqualTo(length));
    }

    private static void AssertFailure(
        UriTemplateParseResult result,
        int offset,
        string message)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Template.OriginalText, Is.Empty);
        Assert.That(result.Template.Parts, Is.Empty);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.UriTemplate));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(
            UriDiagnosticCode.InvalidTemplateModifier));
        Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(
            UriComponent.TemplateVariable));
        Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
    }
}
