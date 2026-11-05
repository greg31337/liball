
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlForeignNamesTests
{
    [Test]
    public void AdjustsMathMlAndSvgNamesAndPreservesUnknownNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlForeignNames.AdjustMathMlAttributeName("definitionurl"), Is.EqualTo("definitionURL"));
            Assert.That(HtmlForeignNames.AdjustMathMlAttributeName("display"), Is.EqualTo("display"));
            Assert.That(HtmlForeignNames.AdjustSvgTagName("FOREIGNOBJECT"), Is.EqualTo("foreignObject"));
            Assert.That(HtmlForeignNames.AdjustSvgTagName("lineargradient"), Is.EqualTo("linearGradient"));
            Assert.That(HtmlForeignNames.AdjustSvgTagName("circle"), Is.EqualTo("circle"));
            Assert.That(HtmlForeignNames.AdjustSvgAttributeName("viewbox"), Is.EqualTo("viewBox"));
            Assert.That(HtmlForeignNames.AdjustSvgAttributeName("preserveaspectratio"), Is.EqualTo("preserveAspectRatio"));
            Assert.That(HtmlForeignNames.AdjustSvgAttributeName("fill"), Is.EqualTo("fill"));
        });
    }

    [Test]
    public void ResolvesXLinkAndXmlForeignAttributes()
    {
        HtmlForeignAttributeAdjustment xlink = HtmlForeignNames.GetForeignAttributeAdjustment("xlink:href");
        HtmlForeignAttributeAdjustment xml = HtmlForeignNames.GetForeignAttributeAdjustment("XML:LANG");

        Assert.Multiple(() =>
        {
            Assert.That(xlink.GetFound(), Is.True);
            Assert.That(xlink.GetPrefix(), Is.EqualTo("xlink"));
            Assert.That(xlink.GetLocalName(), Is.EqualTo("href"));
            Assert.That(xlink.GetNamespaceUri(), Is.EqualTo("http://www.w3.org/1999/xlink"));
            Assert.That(xml.GetFound(), Is.True);
            Assert.That(xml.GetPrefix(), Is.EqualTo("xml"));
            Assert.That(xml.GetLocalName(), Is.EqualTo("lang"));
            Assert.That(xml.GetNamespaceUri(), Is.EqualTo("http://www.w3.org/XML/1998/namespace"));
        });
    }

    [Test]
    public void ResolvesXmlnsFormsAndReportsUnknownAttributes()
    {
        HtmlForeignAttributeAdjustment defaultXmlns = HtmlForeignNames.GetForeignAttributeAdjustment("xmlns");
        HtmlForeignAttributeAdjustment prefixedXmlns = HtmlForeignNames.GetForeignAttributeAdjustment("xmlns:xlink");
        HtmlForeignAttributeAdjustment unknown = HtmlForeignNames.GetForeignAttributeAdjustment("aria-label");

        Assert.Multiple(() =>
        {
            Assert.That(defaultXmlns.GetFound(), Is.True);
            Assert.That(defaultXmlns.GetPrefix(), Is.Empty);
            Assert.That(defaultXmlns.GetLocalName(), Is.EqualTo("xmlns"));
            Assert.That(defaultXmlns.GetNamespaceUri(), Is.EqualTo("http://www.w3.org/2000/xmlns/"));
            Assert.That(prefixedXmlns.GetFound(), Is.True);
            Assert.That(prefixedXmlns.GetPrefix(), Is.EqualTo("xmlns"));
            Assert.That(prefixedXmlns.GetLocalName(), Is.EqualTo("xlink"));
            Assert.That(unknown.GetFound(), Is.False);
            Assert.That(unknown.GetPrefix(), Is.Empty);
            Assert.That(unknown.GetNamespaceUri(), Is.Empty);
        });
    }
}
