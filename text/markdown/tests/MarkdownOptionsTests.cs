using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownOptionsTests
{
    [Test]
    public void FeatureSet_ZeroPresetDisablesAllFeatures()
    {
        MarkdownFeatureSet features = MarkdownFeatureSet.ZeroPreset();

        Assert.That(features.CommonMark, Is.False);
        Assert.That(features.GfmTables, Is.False);
        Assert.That(features.Footnotes, Is.False);
        Assert.That(features.CustomInlineDelimiters, Is.False);
        Assert.That(features.Details, Is.False);
    }

    [Test]
    public void FeatureSet_CommonMarkPresetEnablesOnlyCommonMark()
    {
        MarkdownFeatureSet features = MarkdownFeatureSet.CommonMarkPreset();

        Assert.That(features.CommonMark, Is.True);
        Assert.That(features.GfmTables, Is.False);
        Assert.That(features.Footnotes, Is.False);
    }

    [Test]
    public void FeatureSet_GfmPresetEnablesGfmGroup()
    {
        MarkdownFeatureSet features = MarkdownFeatureSet.GfmPreset();

        Assert.That(features.CommonMark, Is.True);
        Assert.That(features.GfmTables, Is.True);
        Assert.That(features.GfmTaskLists, Is.True);
        Assert.That(features.GfmStrikethrough, Is.True);
        Assert.That(features.GfmBareAutolinks, Is.True);
        Assert.That(features.GfmDisallowedRawHtml, Is.True);
        Assert.That(features.Footnotes, Is.False);
    }

    [Test]
    public void FeatureSet_AdvancedPresetEnablesAdvancedExtensions()
    {
        MarkdownFeatureSet features = MarkdownFeatureSet.AdvancedPreset();

        Assert.That(features.CommonMark, Is.True);
        Assert.That(features.GfmTables, Is.True);
        Assert.That(features.Footnotes, Is.True);
        Assert.That(features.DefinitionLists, Is.True);
        Assert.That(features.HeadingIds, Is.True);
        Assert.That(features.Attributes, Is.True);
        Assert.That(features.FrontMatter, Is.True);
        Assert.That(features.Abbreviations, Is.True);
        Assert.That(features.Superscript, Is.True);
        Assert.That(features.Subscript, Is.True);
        Assert.That(features.InsertedText, Is.True);
        Assert.That(features.MarkedText, Is.True);
        Assert.That(features.Typography, Is.True);
        Assert.That(features.Math, Is.True);
        Assert.That(features.Diagrams, Is.True);
        Assert.That(features.Emoji, Is.True);
        Assert.That(features.CjkSoftBreaks, Is.True);
        Assert.That(features.Admonitions, Is.True);
        Assert.That(features.CustomContainers, Is.True);
        Assert.That(features.CustomFencedBlocks, Is.True);
        Assert.That(features.CustomInlineDelimiters, Is.True);
        Assert.That(features.Details, Is.True);
    }

    [Test]
    public void FeatureSet_CopyCreatesIndependentFlags()
    {
        MarkdownFeatureSet features = MarkdownFeatureSet.GfmPreset();
        MarkdownFeatureSet copy = features.Copy();

        features.DisableAll();

        Assert.That(copy.CommonMark, Is.True);
        Assert.That(copy.GfmTables, Is.True);
    }

    [Test]
    public void Options_DefaultsToSafeCommonMarkConfiguration()
    {
        MarkdownOptions options = new MarkdownOptions();

        Assert.That(options.Features.CommonMark, Is.True);
        Assert.That(options.Features.GfmTables, Is.False);
        Assert.That(options.MaximumNestingDepth, Is.EqualTo(100));
        Assert.That(options.MaximumInputSize, Is.EqualTo(10485760));
        Assert.That(options.MaximumReferenceDefinitions, Is.EqualTo(10000));
        Assert.That(options.MaximumOutputSize, Is.EqualTo(52428800));
        Assert.That(options.MaximumLineWidth, Is.EqualTo(0));
        Assert.That(options.TrackTrivia, Is.False);
        Assert.That(options.AllowRawHtml, Is.False);
        Assert.That(options.AllowUnsafeHtml, Is.False);
        Assert.That(options.AllowUnsafeLinks, Is.False);
        Assert.That(options.DeterministicOutput, Is.True);
        Assert.That(options.SoftBreakMode, Is.EqualTo(MarkdownSoftBreakMode.Preserve));
        Assert.That(options.LineEnding, Is.EqualTo("\n"));
    }

    [Test]
    public void Options_PresetsAssignFeatureSets()
    {
        Assert.That(MarkdownOptions.CommonMarkPreset().Features.CommonMark, Is.True);
        Assert.That(MarkdownOptions.GfmPreset().Features.GfmTables, Is.True);
        Assert.That(MarkdownOptions.AdvancedPreset().Features.Footnotes, Is.True);
        Assert.That(MarkdownOptions.ZeroPreset().Features.CommonMark, Is.False);
    }

    [Test]
    public void Options_CopyCreatesIndependentFeatureSet()
    {
        MarkdownOptions options = MarkdownOptions.GfmPreset();
        MarkdownOptions copy = options.Copy();

        options.Features.DisableAll();
        options.SetMaximumInputSize(12);
        options.SetLineEnding("\r\n");

        Assert.That(copy.Features.GfmTables, Is.True);
        Assert.That(copy.MaximumInputSize, Is.EqualTo(10485760));
        Assert.That(copy.LineEnding, Is.EqualTo("\n"));
    }

    [Test]
    public void Options_SettersValidateAndAssignValues()
    {
        MarkdownOptions options = new MarkdownOptions();
        MarkdownFeatureSet features = MarkdownFeatureSet.AdvancedPreset();

        options.SetFeatureSet(features);
        features.DisableAll();
        options.SetMaximumNestingDepth(3);
        options.SetMaximumInputSize(4);
        options.SetMaximumReferenceDefinitions(5);
        options.SetMaximumOutputSize(6);
        options.SetLineEnding("\r\n");

        Assert.That(options.Features.Footnotes, Is.True);
        Assert.That(options.MaximumNestingDepth, Is.EqualTo(3));
        Assert.That(options.MaximumInputSize, Is.EqualTo(4));
        Assert.That(options.MaximumReferenceDefinitions, Is.EqualTo(5));
        Assert.That(options.MaximumOutputSize, Is.EqualTo(6));
        Assert.That(options.LineEnding, Is.EqualTo("\r\n"));
    }

    [Test]
    public void Options_RejectInvalidValues()
    {
        MarkdownOptions options = new MarkdownOptions();

        Assert.That(() => options.SetFeatureSet(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => options.SetMaximumNestingDepth(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.SetMaximumInputSize(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.SetMaximumReferenceDefinitions(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.SetMaximumOutputSize(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.SetLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => options.SetLineEnding("\n\n"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownOptions.ValidatePositiveLimit(1, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownOptions.ValidatePositiveLimit(0, "limit"), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
