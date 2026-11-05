namespace svg.Tests;

public class SvgValueCanonicalizerTests
{
    [Test]
    public void CanonicalJobFormatsEverySupportedValueFamilyAndConverges()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0.00,0,10.500,20'><path id='p' d='M0.5000,1.000 L2.000 3.500z' transform='translate(1.00,0.0) scale(2.0,2.0)' fill='RGB(255, 0, 0)' opacity='.5000'/><polyline id='q' points='0.0,1.00 2.000,3.5'/><text id='t' x='1.00,2.500' rotate='10.00,20.50'>x</text></svg>");

        SvgProcessResult first = Process(document, new SvgProcessJobs().CanonicalizeValuesWithNoArguments());
        SvgProcessResult second = Process(document, new SvgProcessJobs().CanonicalizeValuesWithNoArguments());
        SvgNode path = document.Index.GetById("p");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(first.Changes, Is.Not.Empty);
            Assert.That(Attribute(document.DocumentElement, "viewBox"), Is.EqualTo("0 0 10.5 20"));
            Assert.That(Attribute(path, "d"), Is.EqualTo("M 0.5 1 L 2 3.5 z"));
            Assert.That(Attribute(path, "transform"), Is.EqualTo("translate(1 0) scale(2 2)"));
            Assert.That(Attribute(path, "fill"), Is.EqualTo("color(srgb 1 0 0)"));
            Assert.That(Attribute(path, "opacity"), Is.EqualTo("0.5"));
            Assert.That(Attribute(document.Index.GetById("q"), "points"), Is.EqualTo("0 1 2 3.5"));
            Assert.That(Attribute(document.Index.GetById("t"), "x"), Is.EqualTo("1 2.5"));
            Assert.That(Attribute(document.Index.GetById("t"), "rotate"), Is.EqualTo("10 20.5"));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void CompactSettingsRoundIndependentlyWithinEachErrorBound()
    {
        SvgCanonicalizeOptions settings = CompactSettings();
        settings.Numbers = SvgValueFormatOptions.Create(SvgValueFormatMode.Compact, true, 1, 0.05);
        settings.Lists = SvgValueFormatOptions.Create(SvgValueFormatMode.Compact, true, 1, 0.01);
        settings.Paths = SvgValueFormatOptions.Create(SvgValueFormatMode.Compact, true, 2, 0.005);
        settings.Transforms = SvgValueFormatOptions.Create(SvgValueFormatMode.Compact, true, 1, 0.05);
        settings.Colors = SvgValueFormatOptions.Create(SvgValueFormatMode.Compact, false, 0, 0);
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><path id='p' opacity='0.54' fill='rgb(255 0 0)' d='M1.234 5.678 L2.345 6.789' transform='translate(.54,0) scale(2,2)'/><polyline id='q' points='1.24,2.34 3.44,4.54'/></svg>");

        SvgProcessResult result = Process(document, new SvgProcessJobs().CanonicalizeValuesWithOptions(settings));
        SvgNode path = document.Index.GetById("p");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(Attribute(path, "opacity"), Is.EqualTo(".5"));
            Assert.That(Attribute(path, "fill"), Is.EqualTo("#f00"));
            Assert.That(Attribute(path, "d"), Is.EqualTo("M1.23 5.68 2.35 6.79"));
            Assert.That(Attribute(path, "transform"), Is.EqualTo("translate(.5),scale(2)"));
            Assert.That(Attribute(document.Index.GetById("q"), "points"), Is.EqualTo("1.24,2.34 3.44,4.54"));
        });
    }

    [Test]
    public void JobCopiesSettingsAndInvalidSettingsFailWithoutMutation()
    {
        SvgCanonicalizeOptions copied = new SvgCanonicalizeOptions();
        SvgProcessJob job = new SvgProcessJobs().CanonicalizeValuesWithOptions(copied);
        copied.Numbers.Mode = SvgValueFormatMode.Compact;
        SvgDocument copiedDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='r' opacity='.50'/></svg>");
        SvgProcessResult copiedResult = Process(copiedDocument, job);

        SvgCanonicalizeOptions invalid = new SvgCanonicalizeOptions();
        invalid.Paths.FractionalDigits = 100;
        SvgDocument invalidDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='r' opacity='.50'/></svg>");
        SvgProcessResult invalidResult = Process(invalidDocument, new SvgProcessJobs().CanonicalizeValuesWithOptions(invalid));

        Assert.Multiple(() =>
        {
            Assert.That(copiedResult.Success, Is.True, Messages(copiedResult));
            Assert.That(Attribute(copiedDocument.Index.GetById("r"), "opacity"), Is.EqualTo("0.5"));
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.Error, Is.EqualTo(SvgProcessError.JobFailed));
            Assert.That(Attribute(invalidDocument.Index.GetById("r"), "opacity"), Is.EqualTo(".50"));
        });
    }

    [Test]
    public void CanonicalizePresetMaterializesAndRunsTheValueJob()
    {
        SvgProcessOptions preset = new SvgProcessPresets().Canonicalize();
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='r' opacity='.50'/></svg>");

        SvgProcessResult result = document.ProcessWithOptions(preset);

        Assert.Multiple(() =>
        {
            Assert.That(preset.Jobs.Select(item => item.Kind), Does.Contain(SvgProcessJobKind.CanonicalizeValues));
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(Attribute(document.Index.GetById("r"), "opacity"), Is.EqualTo("0.5"));
        });
    }

    static SvgCanonicalizeOptions CompactSettings()
    {
        SvgCanonicalizeOptions result = new SvgCanonicalizeOptions();
        result.Numbers.Mode = SvgValueFormatMode.Compact;
        result.Colors.Mode = SvgValueFormatMode.Compact;
        result.Lists.Mode = SvgValueFormatMode.Compact;
        result.Paths.Mode = SvgValueFormatMode.Compact;
        result.Transforms.Mode = SvgValueFormatMode.Compact;
        return result;
    }

    static SvgProcessResult Process(SvgDocument document, SvgProcessJob job)
    {
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(job);
        return document.ProcessWithOptions(options);
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Attribute(SvgNode node, string name)
    {
        return node.Attributes.Single(item => item.LocalName == name).Value;
    }

    static string Messages(SvgProcessResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
