namespace svg.Tests;

public class SvgSecurityLimitsTests
{
    [Test]
    public void SecureDefaultsAreExactAndValid()
    {
        SvgSecurityLimits limits = new SvgSecurityLimits();
        SvgSecurityLimitValidation validation = limits.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(limits.Input.MaxInputBytes, Is.EqualTo(33554432));
            Assert.That(limits.Input.MaxInputCharacters, Is.EqualTo(33554432));
            Assert.That(limits.Input.MaxGzipOutputBytes, Is.EqualTo(67108864));
            Assert.That(limits.Input.MaxGzipExpansionRatio, Is.EqualTo(100));
            Assert.That(limits.Input.MaxXmlTokenCharacters, Is.EqualTo(16777216));

            Assert.That(limits.Tree.MaxDepth, Is.EqualTo(128));
            Assert.That(limits.Tree.MaxNodes, Is.EqualTo(250000));
            Assert.That(limits.Tree.MaxAttributesPerElement, Is.EqualTo(256));
            Assert.That(limits.Tree.MaxTotalAttributes, Is.EqualTo(500000));
            Assert.That(limits.Tree.MaxTextCharacters, Is.EqualTo(16777216));
            Assert.That(limits.Tree.MaxNameCharacters, Is.EqualTo(1024));

            Assert.That(limits.Css.MaxCharacters, Is.EqualTo(8388608));
            Assert.That(limits.Css.MaxTokens, Is.EqualTo(1000000));
            Assert.That(limits.Css.MaxRules, Is.EqualTo(50000));
            Assert.That(limits.Css.MaxDeclarations, Is.EqualTo(250000));
            Assert.That(limits.Css.MaxSelectorTokens, Is.EqualTo(4096));
            Assert.That(limits.Css.MaxBlockDepth, Is.EqualTo(64));
            Assert.That(limits.Css.MaxImportDepth, Is.EqualTo(16));
            Assert.That(limits.Css.MaxVariableExpansionTokens, Is.EqualTo(100000));

            Assert.That(limits.References.MaxDepth, Is.EqualTo(64));
            Assert.That(limits.References.MaxCount, Is.EqualTo(100000));
            Assert.That(limits.References.MaxUseInstances, Is.EqualTo(100000));
            Assert.That(limits.References.MaxPaintServerDepth, Is.EqualTo(32));
            Assert.That(limits.References.MaxEffectDepth, Is.EqualTo(32));
            Assert.That(limits.References.MaxConditionalChecks, Is.EqualTo(1000000));

            Assert.That(limits.Resources.MaxCount, Is.EqualTo(256));
            Assert.That(limits.Resources.MaxBytesPerResource, Is.EqualTo(33554432));
            Assert.That(limits.Resources.MaxTotalBytes, Is.EqualTo(134217728));
            Assert.That(limits.Resources.MaxDataUrlBytes, Is.EqualTo(16777216));
            Assert.That(limits.Resources.MaxRedirects, Is.EqualTo(5));
            Assert.That(limits.Resources.MaxUriCharacters, Is.EqualTo(8192));
            Assert.That(limits.Resources.MaxCallbackMilliseconds, Is.EqualTo(30000));

            Assert.That(limits.Geometry.MaxPathSegments, Is.EqualTo(1000000));
            Assert.That(limits.Geometry.MaxPoints, Is.EqualTo(1000000));
            Assert.That(limits.Geometry.MaxFlattenedSegments, Is.EqualTo(2000000));
            Assert.That(limits.Geometry.MaxSubdivisionDepth, Is.EqualTo(32));
            Assert.That(limits.Geometry.MaxIntersections, Is.EqualTo(1000000));
            Assert.That(limits.Geometry.MaxBooleanEdges, Is.EqualTo(2000000));
            Assert.That(limits.Geometry.MaxHitTestNodes, Is.EqualTo(1000000));

            Assert.That(limits.Fonts.MaxFonts, Is.EqualTo(128));
            Assert.That(limits.Fonts.MaxBytesPerFont, Is.EqualTo(33554432));
            Assert.That(limits.Fonts.MaxTotalBytes, Is.EqualTo(134217728));
            Assert.That(limits.Fonts.MaxTablesPerFont, Is.EqualTo(4096));
            Assert.That(limits.Fonts.MaxGlyphsPerFont, Is.EqualTo(65535));
            Assert.That(limits.Fonts.MaxPointsPerGlyph, Is.EqualTo(1000000));
            Assert.That(limits.Fonts.MaxShapingGlyphs, Is.EqualTo(100000));
            Assert.That(limits.Fonts.MaxVariationAxes, Is.EqualTo(64));

            Assert.That(limits.Filters.MaxPrimitives, Is.EqualTo(256));
            Assert.That(limits.Filters.MaxAreaPixels, Is.EqualTo(16777216));
            Assert.That(limits.Filters.MaxKernelElements, Is.EqualTo(65536));
            Assert.That(limits.Filters.MaxBlurStandardDeviation, Is.EqualTo(4096));
            Assert.That(limits.Filters.MaxTurbulenceOctaves, Is.EqualTo(10));
            Assert.That(limits.Filters.MaxOffscreenSurfaces, Is.EqualTo(64));

            Assert.That(limits.Raster.MaxWidth, Is.EqualTo(32768));
            Assert.That(limits.Raster.MaxHeight, Is.EqualTo(32768));
            Assert.That(limits.Raster.MaxSurfacePixels, Is.EqualTo(100000000));
            Assert.That(limits.Raster.MaxPixelBytes, Is.EqualTo(400000000));
            Assert.That(limits.Raster.MaxPngChunks, Is.EqualTo(100000));
            Assert.That(limits.Raster.MaxPngDecodedBytes, Is.EqualTo(400000000));

            Assert.That(limits.Timeline.MaxAnimations, Is.EqualTo(100000));
            Assert.That(limits.Timeline.MaxInstances, Is.EqualTo(1000000));
            Assert.That(limits.Timeline.MaxKeyframes, Is.EqualTo(100000));
            Assert.That(limits.Timeline.MaxEventTimes, Is.EqualTo(100000));
            Assert.That(limits.Timeline.MaxRepeatCount, Is.EqualTo(1000000));
            Assert.That(limits.Timeline.MaxSampleWork, Is.EqualTo(10000000));

            Assert.That(validation.Success, Is.True);
            Assert.That(validation.GroupName, Is.Empty);
            Assert.That(validation.FieldName, Is.Empty);
            Assert.That(validation.Value, Is.Zero);
        });
    }

    [Test]
    public void AggregateInstancesDoNotShareMutableLimitGroups()
    {
        SvgSecurityLimits first = new SvgSecurityLimits();
        SvgSecurityLimits second = new SvgSecurityLimits();

        first.Input.MaxInputBytes = 1;
        first.Resources.MaxCount = 1;
        first.Raster.MaxWidth = 1;

        Assert.Multiple(() =>
        {
            Assert.That(second.Input.MaxInputBytes, Is.EqualTo(33554432));
            Assert.That(second.Resources.MaxCount, Is.EqualTo(256));
            Assert.That(second.Raster.MaxWidth, Is.EqualTo(32768));
        });
    }

    [Test]
    public void ValidationReportsFirstInvalidFieldInEachGroup()
    {
        SvgSecurityLimits input = new SvgSecurityLimits();
        input.Input.MaxInputBytes = 0;
        AssertInvalid(input, "Input", "MaxInputBytes", 0);

        SvgSecurityLimits tree = new SvgSecurityLimits();
        tree.Tree.MaxDepth = -2;
        AssertInvalid(tree, "Tree", "MaxDepth", -2);

        SvgSecurityLimits css = new SvgSecurityLimits();
        css.Css.MaxCharacters = 0;
        css.Css.MaxTokens = 0;
        AssertInvalid(css, "Css", "MaxCharacters", 0);

        SvgSecurityLimits references = new SvgSecurityLimits();
        references.References.MaxUseInstances = 0;
        AssertInvalid(references, "References", "MaxUseInstances", 0);

        SvgSecurityLimits resources = new SvgSecurityLimits();
        resources.Resources.MaxDataUrlBytes = -1;
        AssertInvalid(resources, "Resources", "MaxDataUrlBytes", -1);

        SvgSecurityLimits geometry = new SvgSecurityLimits();
        geometry.Geometry.MaxSubdivisionDepth = 0;
        AssertInvalid(geometry, "Geometry", "MaxSubdivisionDepth", 0);

        SvgSecurityLimits fonts = new SvgSecurityLimits();
        fonts.Fonts.MaxGlyphsPerFont = 0;
        AssertInvalid(fonts, "Fonts", "MaxGlyphsPerFont", 0);

        SvgSecurityLimits filters = new SvgSecurityLimits();
        filters.Filters.MaxAreaPixels = 0;
        AssertInvalid(filters, "Filters", "MaxAreaPixels", 0);

        SvgSecurityLimits raster = new SvgSecurityLimits();
        raster.Raster.MaxSurfacePixels = -10;
        AssertInvalid(raster, "Raster", "MaxSurfacePixels", -10);

        SvgSecurityLimits timeline = new SvgSecurityLimits();
        timeline.Timeline.MaxInstances = 0;
        AssertInvalid(timeline, "Timeline", "MaxInstances", 0);
    }

    [Test]
    public void ValidationReportsMissingGroupInDeclarationOrder()
    {
        SvgSecurityLimits limits = new SvgSecurityLimits();
        limits.Css = null;
        limits.Filters = null;

        SvgSecurityLimitValidation validation = limits.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.False);
            Assert.That(validation.GroupName, Is.EqualTo("Css"));
            Assert.That(validation.FieldName, Is.EqualTo("Css"));
            Assert.That(validation.Value, Is.EqualTo(-1));
            Assert.That(validation.Message, Is.EqualTo("The Css limit group is required."));
        });
    }

    static void AssertInvalid(SvgSecurityLimits limits, string groupName, string fieldName, long value)
    {
        SvgSecurityLimitValidation validation = limits.Validate();

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.False);
            Assert.That(validation.GroupName, Is.EqualTo(groupName));
            Assert.That(validation.FieldName, Is.EqualTo(fieldName));
            Assert.That(validation.Value, Is.EqualTo(value));
            Assert.That(validation.Message, Is.EqualTo("Security limits must be greater than zero."));
        });
    }
}
