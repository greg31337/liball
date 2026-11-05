namespace svg.Tests;

public class SvgDocumentRecognitionTests
{
    [TestCase("<svg/>", false)]
    [TestCase("<svg xmlns='http://www.w3.org/2000/svg'/>", false)]
    [TestCase("<s:svg xmlns:s='http://www.w3.org/2000/svg'/>", false)]
    [TestCase("<g xmlns='http://www.w3.org/2000/svg'/>", true)]
    [TestCase("<s:symbol xmlns:s='http://www.w3.org/2000/svg'/>", true)]
    public void CompleteRootsAndNamespaceQualifiedFragmentsAreRecognized(string input, bool expectedFragment)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(result.Document.IsFragment, Is.EqualTo(expectedFragment));
            Assert.That(result.Document.ContentType, Is.EqualTo(SvgMimeTypes.Svg));
        });
    }

    [Test]
    public void LoadOptionsBecomeEffectiveDocumentMetadata()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = "https://example.test/base/";
        options.ContentType = "application/xml; charset=utf-8";
        options.Mode = SvgProcessingMode.Dynamic;

        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg/>", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Document.BaseUri, Is.EqualTo("https://example.test/base/"));
            Assert.That(result.Document.ContentType, Is.EqualTo("application/xml; charset=utf-8"));
            Assert.That(result.Document.ProcessingMode, Is.EqualTo(SvgProcessingMode.Dynamic));
        });
    }

    [Test]
    public void ResourceMetadataSuppliesBaseUriAndOverridesDetectedContentType()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.Mode = SvgProcessingMode.Static;
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            SvgResourceResponse response = new SvgResourceResponse();
            response.Bytes = System.Text.Encoding.UTF8.GetBytes("<svg/>");
            response.FinalUri = "https://cdn.example.test/assets/root.svg";
            response.DeclaredMediaType = "application/xml";
            response.ActualMediaType = "image/svg+xml; profile=full";
            return response;
        });

        SvgLoadResult result = new SvgLoader().LoadUri("https://example.test/root.svg", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Document.BaseUri, Is.EqualTo("https://cdn.example.test/assets/root.svg"));
            Assert.That(result.Document.ContentType, Is.EqualTo("image/svg+xml; profile=full"));
            Assert.That(result.Document.ProcessingMode, Is.EqualTo(SvgProcessingMode.Static));
        });
    }

    [TestCase("<html/>")]
    [TestCase("<svg xmlns='urn:not-svg'/>")]
    [TestCase("<html xmlns='http://www.w3.org/1999/xhtml'><svg xmlns='http://www.w3.org/2000/svg'/></html>")]
    public void NonSvgDocumentElementsFailWithAStableLocatedDiagnostic(string input)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = "memory:non-svg";

        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidSvgRoot));
            Assert.That(result.Diagnostics[0].Source.Source.Uri, Is.EqualTo("memory:non-svg"));
            Assert.That(result.Diagnostics[0].Source.StartOffset, Is.Zero);
            Assert.That(result.Diagnostics[0].Source.StartLine, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Source.StartColumn, Is.EqualTo(1));
        });
    }

    [Test]
    public void XmlReaderInputsUseTheSameRecognitionAndModeRules()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.Mode = SvgProcessingMode.Dynamic;
        System.Xml.XmlReaderSettings settings = new System.Xml.XmlReaderSettings();
        using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(new System.IO.StringReader("<g xmlns='http://www.w3.org/2000/svg'/>"), settings, "https://reader.example.test/fragment.svg"))
        {
            SvgLoadResult result = new SvgLoader().LoadWithXmlReaderAndSvgLoadOptions(reader, options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Document.IsFragment, Is.True);
                Assert.That(result.Document.ProcessingMode, Is.EqualTo(SvgProcessingMode.Dynamic));
                Assert.That(result.Document.ContentType, Is.EqualTo(SvgMimeTypes.Svg));
                Assert.That(result.Document.BaseUri, Is.EqualTo("https://reader.example.test/fragment.svg"));
            });
        }
    }
}
