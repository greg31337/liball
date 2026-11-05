using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateProfileTests
{
    [Test]
    public void NativeProfile_Create_ReturnsNativeDelimiterAndRuntimeDefaults()
    {
        TemplateProfile profile = NativeProfile.Create();

        profile.Validate();
        Assert.That(profile.Name, Is.EqualTo("native"));
        Assert.That(profile.OutputStartDelimiter, Is.EqualTo("{{"));
        Assert.That(profile.OutputEndDelimiter, Is.EqualTo("}}"));
        Assert.That(profile.StatementStartDelimiter, Is.EqualTo("{%"));
        Assert.That(profile.StatementEndDelimiter, Is.EqualTo("%}"));
        Assert.That(profile.CommentStartDelimiter, Is.EqualTo("{#"));
        Assert.That(profile.CommentEndDelimiter, Is.EqualTo("#}"));
        Assert.That(profile.RawBlockStartKeyword, Is.EqualTo("raw"));
        Assert.That(profile.RawBlockEndKeyword, Is.EqualTo("endraw"));
        Assert.That(profile.SupportsWhitespaceTrim, Is.True);
        Assert.That(profile.LeftTrimMarker, Is.EqualTo('-'));
        Assert.That(profile.RightTrimMarker, Is.EqualTo('-'));
        Assert.That(profile.StandaloneStatementTrimming, Is.True);
        Assert.That(profile.CaseSensitiveNames, Is.True);
        Assert.That(profile.AllowsComplexExpressions, Is.True);
        Assert.That(profile.DefaultWhitespaceMode, Is.EqualTo(TemplateWhitespaceMode.Preserve));
        Assert.That(profile.DefaultEscapingMode, Is.EqualTo(TemplateEscapingMode.None));
        Assert.That(profile.AutoEscape, Is.False);
    }

    [Test]
    public void CreateOptions_AppliesProfileDefaultsToTemplateOptions()
    {
        TemplateOptions options = NativeProfile.CreateOptions();

        options.Validate();
        Assert.That(options.SyntaxProfile, Is.EqualTo("native"));
        Assert.That(options.WhitespaceMode, Is.EqualTo(TemplateWhitespaceMode.Preserve));
        Assert.That(options.EscapingMode, Is.EqualTo(TemplateEscapingMode.None));
        Assert.That(options.AutoEscape, Is.False);
        Assert.That(options.MaximumIncludeDepth, Is.EqualTo(32));
    }

    [Test]
    public void MustacheProfile_CreateOptions_UsesMustacheEscapingAndTruthinessProfile()
    {
        TemplateProfile profile = MustacheProfile.Create();
        TemplateOptions options = MustacheProfile.CreateOptions();

        profile.Validate();
        Assert.That(profile.Name, Is.EqualTo("mustache"));
        Assert.That(profile.DefaultEscapingMode, Is.EqualTo(TemplateEscapingMode.Html));
        Assert.That(profile.AutoEscape, Is.True);
        Assert.That(profile.AllowsComplexExpressions, Is.False);
        Assert.That(options.SyntaxProfile, Is.EqualTo("mustache"));
        Assert.That(options.EscapingMode, Is.EqualTo(TemplateEscapingMode.Html));
        Assert.That(options.AutoEscape, Is.True);
    }

    [Test]
    public void HandlebarsProfile_CreateOptions_UsesHandlebarsEscapingAndTrimDefaults()
    {
        TemplateProfile profile = HandlebarsProfile.Create();
        TemplateOptions options = HandlebarsProfile.CreateOptions();

        profile.Validate();
        Assert.That(profile.Name, Is.EqualTo("handlebars"));
        Assert.That(profile.LeftTrimMarker, Is.EqualTo('~'));
        Assert.That(profile.RightTrimMarker, Is.EqualTo('~'));
        Assert.That(profile.DefaultEscapingMode, Is.EqualTo(TemplateEscapingMode.Html));
        Assert.That(profile.AutoEscape, Is.True);
        Assert.That(options.SyntaxProfile, Is.EqualTo("handlebars"));
        Assert.That(options.EscapingMode, Is.EqualTo(TemplateEscapingMode.Html));
        Assert.That(options.AutoEscape, Is.True);
    }

    [Test]
    public void LiquidProfile_CreateOptions_UsesLiquidDelimitersAndRawOutputDefault()
    {
        TemplateProfile profile = LiquidProfile.Create();
        TemplateOptions options = LiquidProfile.CreateOptions();

        profile.Validate();
        Assert.That(profile.Name, Is.EqualTo("liquid"));
        Assert.That(profile.OutputStartDelimiter, Is.EqualTo("{{"));
        Assert.That(profile.StatementStartDelimiter, Is.EqualTo("{%"));
        Assert.That(profile.LeftTrimMarker, Is.EqualTo('-'));
        Assert.That(profile.DefaultEscapingMode, Is.EqualTo(TemplateEscapingMode.None));
        Assert.That(profile.AutoEscape, Is.False);
        Assert.That(options.SyntaxProfile, Is.EqualTo("liquid"));
        Assert.That(options.EscapingMode, Is.EqualTo(TemplateEscapingMode.None));
    }

    [Test]
    public void JinjaAndDjangoProfiles_CreateOptions_UseHtmlAutoescapeDefaults()
    {
        TemplateProfile jinja = JinjaProfile.Create();
        TemplateProfile django = DjangoProfile.Create();
        TemplateOptions jinjaOptions = JinjaProfile.CreateOptions();
        TemplateOptions djangoOptions = DjangoProfile.CreateOptions();

        jinja.Validate();
        django.Validate();
        Assert.That(jinja.Name, Is.EqualTo("jinja"));
        Assert.That(django.Name, Is.EqualTo("django"));
        Assert.That(jinja.DefaultEscapingMode, Is.EqualTo(TemplateEscapingMode.Html));
        Assert.That(django.DefaultEscapingMode, Is.EqualTo(TemplateEscapingMode.Html));
        Assert.That(jinjaOptions.SyntaxProfile, Is.EqualTo("jinja"));
        Assert.That(djangoOptions.SyntaxProfile, Is.EqualTo("django"));
        Assert.That(jinjaOptions.AutoEscape, Is.True);
        Assert.That(djangoOptions.AutoEscape, Is.True);
    }

    [Test]
    public void ApplyTo_UpdatesExistingOptionsWithoutChangingRuntimeLimits()
    {
        TemplateProfile profile = NativeProfile.Create();
        profile.DefaultEscapingMode = TemplateEscapingMode.Html;
        profile.AutoEscape = true;
        TemplateOptions options = new TemplateOptions();
        options.MaximumOutputLength = 42;

        profile.ApplyTo(options);

        Assert.That(options.SyntaxProfile, Is.EqualTo("native"));
        Assert.That(options.EscapingMode, Is.EqualTo(TemplateEscapingMode.Html));
        Assert.That(options.AutoEscape, Is.True);
        Assert.That(options.MaximumOutputLength, Is.EqualTo(42));
    }

    [Test]
    public void Clone_CopiesFieldsAndCanBeMutatedIndependently()
    {
        TemplateProfile profile = NativeProfile.Create();

        TemplateProfile clone = profile.Clone();
        clone.Name = "custom";
        clone.OutputStartDelimiter = "<<";
        clone.OutputEndDelimiter = ">>";
        clone.DefaultEscapingMode = TemplateEscapingMode.JsonString;

        Assert.That(profile.Name, Is.EqualTo("native"));
        Assert.That(profile.OutputStartDelimiter, Is.EqualTo("{{"));
        Assert.That(profile.OutputEndDelimiter, Is.EqualTo("}}"));
        Assert.That(profile.DefaultEscapingMode, Is.EqualTo(TemplateEscapingMode.None));
        Assert.That(clone.Name, Is.EqualTo("custom"));
        Assert.That(clone.OutputStartDelimiter, Is.EqualTo("<<"));
        Assert.That(clone.OutputEndDelimiter, Is.EqualTo(">>"));
        Assert.That(clone.DefaultEscapingMode, Is.EqualTo(TemplateEscapingMode.JsonString));
    }

    [Test]
    public void Validate_RejectsMissingRequiredProfileFields()
    {
        TemplateProfile missingName = NativeProfile.Create();
        missingName.Name = "";

        TemplateProfile missingOutputStart = NativeProfile.Create();
        missingOutputStart.OutputStartDelimiter = "";

        TemplateProfile missingOutputEnd = NativeProfile.Create();
        missingOutputEnd.OutputEndDelimiter = "";

        TemplateProfile missingStatementStart = NativeProfile.Create();
        missingStatementStart.StatementStartDelimiter = "";

        TemplateProfile missingStatementEnd = NativeProfile.Create();
        missingStatementEnd.StatementEndDelimiter = "";

        TemplateProfile missingCommentStart = NativeProfile.Create();
        missingCommentStart.CommentStartDelimiter = "";

        TemplateProfile missingCommentEnd = NativeProfile.Create();
        missingCommentEnd.CommentEndDelimiter = "";

        TemplateProfile missingRawStart = NativeProfile.Create();
        missingRawStart.RawBlockStartKeyword = "";

        TemplateProfile missingRawEnd = NativeProfile.Create();
        missingRawEnd.RawBlockEndKeyword = "";

        Assert.Throws<ArgumentException>(() => missingName.Validate());
        Assert.Throws<ArgumentException>(() => missingOutputStart.Validate());
        Assert.Throws<ArgumentException>(() => missingOutputEnd.Validate());
        Assert.Throws<ArgumentException>(() => missingStatementStart.Validate());
        Assert.Throws<ArgumentException>(() => missingStatementEnd.Validate());
        Assert.Throws<ArgumentException>(() => missingCommentStart.Validate());
        Assert.Throws<ArgumentException>(() => missingCommentEnd.Validate());
        Assert.Throws<ArgumentException>(() => missingRawStart.Validate());
        Assert.Throws<ArgumentException>(() => missingRawEnd.Validate());
    }
}
