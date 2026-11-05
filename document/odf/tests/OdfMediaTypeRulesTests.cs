namespace odf.Tests;

public class OdfMediaTypeRulesTests
{
    [TestCase("application/vnd.oasis.opendocument.text", "application", "vnd.oasis.opendocument.text")]
    [TestCase("IMAGE/PNG", "image", "png")]
    [TestCase("application/ld+json", "application", "ld+json")]
    [TestCase("text/plain ", "text", "plain")]
    public void MediaTypeParserRecognizesTypeAndSubtype(string lexical, string type, string subtype)
    {
        OdfMediaTypeValue result = OdfMediaTypeRules.ParseMediaType(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.LexicalValue, Is.EqualTo(lexical));
            Assert.That(result.Type, Is.EqualTo(type));
            Assert.That(result.Subtype, Is.EqualTo(subtype));
            Assert.That(result.ParameterNames, Is.Empty);
            Assert.That(result.ParameterValues, Is.Empty);
            Assert.That(OdfMediaTypeRules.FormatMediaType(result), Is.EqualTo(type + "/" + subtype));
        });
    }

    [Test]
    public void MediaTypeParserHandlesTokenAndQuotedParameters()
    {
        OdfMediaTypeValue result = OdfMediaTypeRules.ParseMediaType(
            "Text/Plain ; Charset=UTF-8; title=\"An \\\"ODF\\\" document\"; empty=\"\"");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Type, Is.EqualTo("text"));
            Assert.That(result.Subtype, Is.EqualTo("plain"));
            Assert.That(result.ParameterNames, Is.EqualTo(new[] { "charset", "title", "empty" }));
            Assert.That(result.ParameterValues, Is.EqualTo(new[] { "UTF-8", "An \"ODF\" document", "" }));
            Assert.That(
                OdfMediaTypeRules.FormatMediaType(result),
                Is.EqualTo("text/plain; charset=UTF-8; title=\"An \\\"ODF\\\" document\"; empty=\"\""));
        });
    }

    [Test]
    public void MediaTypeParserAcceptsExtendedParameterNames()
    {
        OdfMediaTypeValue result = OdfMediaTypeRules.ParseMediaType(
            "application/example; filename*=UTF-8''document.odt");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ParameterNames, Is.EqualTo(new[] { "filename*" }));
            Assert.That(result.ParameterValues, Is.EqualTo(new[] { "UTF-8''document.odt" }));
            Assert.That(
                OdfMediaTypeRules.FormatMediaType(result),
                Is.EqualTo("application/example; filename*=UTF-8''document.odt"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("text")]
    [TestCase("/plain")]
    [TestCase("text/")]
    [TestCase(" text/plain")]
    [TestCase("text/pläin")]
    [TestCase("text/plain;")]
    [TestCase("text/plain; charset")]
    [TestCase("text/plain; charset=")]
    [TestCase("text/plain; charset=\"unterminated")]
    [TestCase("text/plain; charset=UTF-8; CHARSET=ascii")]
    [TestCase("text/plain; bad name=value")]
    public void MediaTypeParserRejectsMalformedValues(string? lexical)
    {
        OdfMediaTypeValue result = OdfMediaTypeRules.ParseMediaType(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfMediaTypeRules.FormatMediaType(result), Is.Empty);
        });
    }

    [Test]
    public void EveryDeclaredOdfDocumentMediaTypeParses()
    {
        string[] mediaTypes =
        [
            OdfMediaTypes.Base,
            OdfMediaTypes.Text,
            OdfMediaTypes.TextTemplate,
            OdfMediaTypes.TextMaster,
            OdfMediaTypes.TextMasterTemplate,
            OdfMediaTypes.Spreadsheet,
            OdfMediaTypes.SpreadsheetTemplate,
            OdfMediaTypes.Presentation,
            OdfMediaTypes.PresentationTemplate,
            OdfMediaTypes.Graphics,
            OdfMediaTypes.GraphicsTemplate,
            OdfMediaTypes.Chart,
            OdfMediaTypes.ChartTemplate,
            OdfMediaTypes.Formula,
            OdfMediaTypes.FormulaTemplate,
            OdfMediaTypes.Image,
            OdfMediaTypes.ImageTemplate
        ];

        Assert.That(mediaTypes.Select(OdfMediaTypeRules.ParseMediaType).All(value => value.IsValid), Is.True);
    }
}
