namespace svg.Tests;

public class SvgDeterministicWriterTests
{
    [Test]
    public void CanonicalModeFormatsEveryTypedValueFamilyAndCssDeterministically()
    {
        SvgDocument document = Load(DocumentSource());
        SvgWriteOptions options = new SvgWriteOptions();
        options.Mode = SvgWriteMode.Canonical;

        SvgWriteResult first = new SvgWriter().WriteWithDocumentAndOptions(document, options);
        SvgWriteResult second = new SvgWriter().WriteWithDocumentAndOptions(document, options);
        SvgDocument reloaded = Load(first.Text);
        SvgWriteResult third = new SvgWriter().WriteWithDocumentAndOptions(reloaded, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.Message);
            Assert.That(first.Text, Is.EqualTo(second.Text));
            Assert.That(first.Text, Is.EqualTo(third.Text));
            Assert.That(first.Text, Does.Contain("viewBox=\"0 0 10.5 20\""));
            Assert.That(first.Text, Does.Contain("d=\"M 0.5 1 L 2 3.5 z\""));
            Assert.That(first.Text, Does.Contain("fill=\"color(srgb 1 0 0)\""));
            Assert.That(first.Text, Does.Contain("opacity=\"0.5\""));
            Assert.That(first.Text, Does.Contain("transform=\"translate(1 0) scale(2 2)\""));
            Assert.That(first.Text, Does.Contain("<style>.a{fill:color(srgb 1 0 0);opacity:0.5;--vendor:foo(1.23);}</style>"));
            Assert.That(first.Text, Does.Contain("style=\"stroke:color(srgb 1 0 0);opacity:0.25;\""));
            Assert.That(first.Text, Does.Contain("<v:thing data-weird=\" Keep  this \" q=\"01\"/>"));
        });
    }

    [Test]
    public void CompactModeUsesShortFormsWithoutChangingUnknownContent()
    {
        SvgDocument document = Load(DocumentSource());
        SvgWriteOptions options = new SvgWriteOptions();
        options.Mode = SvgWriteMode.Compact;

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Does.Contain("viewBox=\"0,0,10.5,20\""));
            Assert.That(result.Text, Does.Contain("d=\"M.5 1 2 3.5z\""));
            Assert.That(result.Text, Does.Contain("fill=\"#f00\""));
            Assert.That(result.Text, Does.Contain("opacity=\".5\""));
            Assert.That(result.Text, Does.Contain("transform=\"translate(1),scale(2)\""));
            Assert.That(result.Text, Does.Contain("<style>.a{fill:#f00;opacity:.5;--vendor:foo(1.23);}</style>"));
            Assert.That(result.Text, Does.Contain("style=\"stroke:#f00;opacity:.25;\""));
            Assert.That(result.Text, Does.Contain("data-weird=\" Keep  this \" q=\"01\""));
        });
    }

    [Test]
    public void PrettyModeIndentsXmlAndEmbeddedCssWithConfiguredWhitespace()
    {
        SvgDocument document = Load(DocumentSource());
        SvgWriteOptions options = new SvgWriteOptions();
        options.Mode = SvgWriteMode.Pretty;
        options.IndentText = "\t";
        options.NewLine = "\r\n";

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Does.Contain("\r\n\t<style>.a {\r\n\tfill:color(srgb 1 0 0);\r\n\topacity:0.5;\r\n\t--vendor:foo(1.23);\r\n}</style>"));
            Assert.That(result.Text, Does.Contain("\r\n\t<path "));
            Assert.That(result.Text, Does.EndWith("\r\n</svg>"));
        });
    }

    [Test]
    public void ConfiguredPrecisionIsBoundedAndDoesNotMutateTheDocument()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><path id='p' opacity='0.544' d='M1.234 5.678 L2.345 6.789' transform='translate(.544,0)'/></svg>");
        SvgNode path = document.Index.GetById("p");
        string originalPath = path.Attributes.Single(attribute => attribute.LocalName == "d").Value;
        SvgWriteOptions options = new SvgWriteOptions();
        options.Mode = SvgWriteMode.Compact;
        options.Values.Numbers = SvgValueFormatOptions.Create(SvgValueFormatMode.Canonical, true, 1, 0.05);
        options.Values.Paths = SvgValueFormatOptions.Create(SvgValueFormatMode.Canonical, true, 2, 0.005);
        options.Values.Transforms = SvgValueFormatOptions.Create(SvgValueFormatMode.Canonical, true, 1, 0.05);

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Does.Contain("opacity=\".5\""));
            Assert.That(result.Text, Does.Contain("d=\"M1.23 5.68 2.35 6.79\""));
            Assert.That(result.Text, Does.Contain("transform=\"translate(.5)\""));
            Assert.That(path.Attributes.Single(attribute => attribute.LocalName == "d").Value, Is.EqualTo(originalPath));
            Assert.That(document.IsDirty, Is.False);
        });
    }

    [Test]
    public void InvalidValuePrecisionFailsAtomically()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgWriteOptions options = new SvgWriteOptions();
        options.Values.Paths.FractionalDigits = SvgNumberFormatter.MaximumFixedPrecision + 1;

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgWriteError.InvalidArgument));
            Assert.That(result.Text, Is.Empty);
        });
    }

    [Test]
    public void RecoveredCssAndUnknownXmlContentRemainRepresentable()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:v='urn:vendor'><style>.a { vendor: ok; broken ???; color: red }</style><v:widget data-x=' A  B '>raw &amp; text</v:widget></svg>");
        SvgWriteOptions options = new SvgWriteOptions();
        options.Mode = SvgWriteMode.Canonical;

        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, options);
        SvgDocument reloaded = Load(result.Text);
        SvgNode widget = reloaded.DocumentElement.Children.Single(node => node.NamespaceUri == "urn:vendor");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Text, Does.Contain("broken ???"));
            Assert.That(result.Text, Does.Contain("vendor:ok"));
            Assert.That(widget.Attributes.Single(attribute => attribute.LocalName == "data-x").Value, Is.EqualTo(" A  B "));
            Assert.That(new SvgNodeApi().GetTextContent(widget), Is.EqualTo("raw & text"));
        });
    }

    static string DocumentSource()
    {
        return "<svg xmlns='http://www.w3.org/2000/svg' xmlns:v='urn:vendor' viewBox='0.00,0,10.500,20'><style>/* comment */ .a { fill: RGB(255, 0, 0); opacity: .5000 ; --vendor: foo( 1.2300 ); }</style><path id='p' d='M0.5000,1.000 L2.000 3.500z' transform='translate(1.00,0.0) scale(2.0,2.0)' fill='RGB(255, 0, 0)' opacity='.5000' style='stroke:RGB(255,0,0); opacity:.2500'/><v:thing data-weird=' Keep  this ' q='01'/></svg>";
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Document;
    }
}
