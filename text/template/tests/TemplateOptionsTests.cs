using System.Globalization;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateOptionsTests
{
    [Test]
    public void Constructor_UsesConservativeDefaults()
    {
        TemplateOptions options = new TemplateOptions();

        Assert.That(options.SyntaxProfile, Is.EqualTo("native"));
        Assert.That(options.StrictMode, Is.False);
        Assert.That(options.StrictMissingValues, Is.False);
        Assert.That(options.StrictTypeConversions, Is.False);
        Assert.That(options.StrictUnknownFunctions, Is.False);
        Assert.That(options.StrictUnsafeMembers, Is.False);
        Assert.That(options.WhitespaceMode, Is.EqualTo(TemplateWhitespaceMode.Preserve));
        Assert.That(options.TrimFinalNewline, Is.False);
        Assert.That(options.AutoIndentMultilineValues, Is.False);
        Assert.That(options.EscapingMode, Is.EqualTo(TemplateEscapingMode.None));
        Assert.That(options.AutoEscape, Is.False);
        Assert.That(options.TruthinessMode, Is.EqualTo(TemplateTruthinessMode.ProfileDefault));
        Assert.That(options.Culture, Is.SameAs(CultureInfo.InvariantCulture));
        Assert.That(options.MaximumTemplateLength, Is.EqualTo(1048576));
        Assert.That(options.MaximumOutputLength, Is.EqualTo(16777216));
        Assert.That(options.MaximumIncludeDepth, Is.EqualTo(32));
        Assert.That(options.MaximumLoopIterations, Is.EqualTo(100000));
        Assert.That(options.MaximumExpressionDepth, Is.EqualTo(64));
        Assert.That(options.MaximumTemplateCount, Is.EqualTo(1024));
        Assert.That(options.MaximumRenderMilliseconds, Is.EqualTo(0));
    }

    [Test]
    public void CreateDefault_ReturnsValidOptions()
    {
        TemplateOptions options = TemplateOptions.CreateDefault();

        options.Validate();
        Assert.That(options.SyntaxProfile, Is.EqualTo("native"));
    }

    [Test]
    public void SetStrictMode_UpdatesAllStrictnessFields()
    {
        TemplateOptions options = new TemplateOptions();

        options.SetStrictMode(true);

        Assert.That(options.StrictMode, Is.True);
        Assert.That(options.StrictMissingValues, Is.True);
        Assert.That(options.StrictTypeConversions, Is.True);
        Assert.That(options.StrictUnknownFunctions, Is.True);
        Assert.That(options.StrictUnsafeMembers, Is.True);

        options.SetStrictMode(false);

        Assert.That(options.StrictMode, Is.False);
        Assert.That(options.StrictMissingValues, Is.False);
        Assert.That(options.StrictTypeConversions, Is.False);
        Assert.That(options.StrictUnknownFunctions, Is.False);
        Assert.That(options.StrictUnsafeMembers, Is.False);
    }

    [Test]
    public void Clone_CopiesAllFieldsAndCanBeMutatedIndependently()
    {
        TemplateOptions options = new TemplateOptions();
        options.SyntaxProfile = "liquid";
        options.SetStrictMode(true);
        options.WhitespaceMode = TemplateWhitespaceMode.NormalizeLineFeeds;
        options.TrimFinalNewline = true;
        options.AutoIndentMultilineValues = true;
        options.EscapingMode = TemplateEscapingMode.Html;
        options.AutoEscape = true;
        options.TruthinessMode = TemplateTruthinessMode.Liquid;
        options.Culture = CultureInfo.GetCultureInfo("fr-FR");
        options.MaximumTemplateLength = 10;
        options.MaximumOutputLength = 20;
        options.MaximumIncludeDepth = 3;
        options.MaximumLoopIterations = 4;
        options.MaximumExpressionDepth = 5;
        options.MaximumTemplateCount = 6;
        options.MaximumRenderMilliseconds = 7;

        TemplateOptions clone = options.Clone();
        clone.SyntaxProfile = "native";
        clone.MaximumOutputLength = 99;

        Assert.That(options.SyntaxProfile, Is.EqualTo("liquid"));
        Assert.That(options.MaximumOutputLength, Is.EqualTo(20));
        Assert.That(clone.StrictMode, Is.True);
        Assert.That(clone.StrictMissingValues, Is.True);
        Assert.That(clone.StrictTypeConversions, Is.True);
        Assert.That(clone.StrictUnknownFunctions, Is.True);
        Assert.That(clone.StrictUnsafeMembers, Is.True);
        Assert.That(clone.WhitespaceMode, Is.EqualTo(TemplateWhitespaceMode.NormalizeLineFeeds));
        Assert.That(clone.TrimFinalNewline, Is.True);
        Assert.That(clone.AutoIndentMultilineValues, Is.True);
        Assert.That(clone.EscapingMode, Is.EqualTo(TemplateEscapingMode.Html));
        Assert.That(clone.AutoEscape, Is.True);
        Assert.That(clone.TruthinessMode, Is.EqualTo(TemplateTruthinessMode.Liquid));
        Assert.That(clone.Culture.Name, Is.EqualTo("fr-FR"));
        Assert.That(clone.MaximumTemplateLength, Is.EqualTo(10));
        Assert.That(clone.MaximumOutputLength, Is.EqualTo(99));
        Assert.That(clone.MaximumIncludeDepth, Is.EqualTo(3));
        Assert.That(clone.MaximumLoopIterations, Is.EqualTo(4));
        Assert.That(clone.MaximumExpressionDepth, Is.EqualTo(5));
        Assert.That(clone.MaximumTemplateCount, Is.EqualTo(6));
        Assert.That(clone.MaximumRenderMilliseconds, Is.EqualTo(7));
    }

    [Test]
    public void Validate_RejectsMissingProfileCultureAndInvalidLimits()
    {
        TemplateOptions missingProfile = new TemplateOptions();
        missingProfile.SyntaxProfile = "";

        TemplateOptions missingCulture = new TemplateOptions();
        missingCulture.Culture = null!;

        TemplateOptions badTemplateLength = new TemplateOptions();
        badTemplateLength.MaximumTemplateLength = 0;

        TemplateOptions badOutputLength = new TemplateOptions();
        badOutputLength.MaximumOutputLength = 0;

        TemplateOptions badIncludeDepth = new TemplateOptions();
        badIncludeDepth.MaximumIncludeDepth = -1;

        TemplateOptions badLoopIterations = new TemplateOptions();
        badLoopIterations.MaximumLoopIterations = -1;

        TemplateOptions badExpressionDepth = new TemplateOptions();
        badExpressionDepth.MaximumExpressionDepth = 0;

        TemplateOptions badTemplateCount = new TemplateOptions();
        badTemplateCount.MaximumTemplateCount = 0;

        TemplateOptions badRenderTime = new TemplateOptions();
        badRenderTime.MaximumRenderMilliseconds = -1;

        Assert.Throws<ArgumentException>(() => missingProfile.Validate());
        Assert.Throws<ArgumentNullException>(() => missingCulture.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => badTemplateLength.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => badOutputLength.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => badIncludeDepth.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => badLoopIterations.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => badExpressionDepth.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => badTemplateCount.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => badRenderTime.Validate());
    }
}
