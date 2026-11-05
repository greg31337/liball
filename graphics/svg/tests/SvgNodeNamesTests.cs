namespace svg.Tests;

public class SvgNodeNamesTests
{
    [TestCase("http://www.w3.org/2000/svg", "svg", SvgElementKind.Svg, true, false)]
    [TestCase("", "rect", SvgElementKind.Rect, true, false)]
    [TestCase("http://www.w3.org/2000/svg", "feDropShadow", SvgElementKind.FeDropShadow, true, false)]
    [TestCase("http://www.w3.org/2000/svg", "audio", SvgElementKind.Audio, true, false)]
    [TestCase("http://www.w3.org/2000/svg", "font-face", SvgElementKind.FontFace, false, true)]
    [TestCase("", "missing-glyph", SvgElementKind.MissingGlyph, false, true)]
    [TestCase("http://www.w3.org/2000/svg", "flowRoot", SvgElementKind.Unknown, false, false)]
    [TestCase("http://www.w3.org/2000/svg", "Rect", SvgElementKind.Unknown, false, false)]
    [TestCase("urn:vendor", "rect", SvgElementKind.Unknown, false, false)]
    [TestCase("http://www.w3.org/1999/xhtml", "svg", SvgElementKind.Unknown, false, false)]
    public void ElementRecognitionIsNamespaceAwareCaseSensitiveAndProfileTagged(string namespaceUri, string localName, SvgElementKind expected, bool svg2, bool legacy)
    {
        SvgElementKind actual = new SvgNodeNames().GetElementKind(namespaceUri, localName);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(new SvgNodeNames().IsSvg2Element(actual), Is.EqualTo(svg2));
            Assert.That(new SvgNodeNames().IsSvg11LegacyElement(actual), Is.EqualTo(legacy));
        });
    }

    [TestCase("", "viewBox", SvgAttributeKind.Svg2)]
    [TestCase("", "fill", SvgAttributeKind.Svg2)]
    [TestCase("", "d", SvgAttributeKind.Svg2)]
    [TestCase("", "onclick", SvgAttributeKind.Svg2)]
    [TestCase("", "limitingConeAngle", SvgAttributeKind.Svg2)]
    [TestCase("", "font-feature-settings", SvgAttributeKind.Svg2)]
    [TestCase("", "requiredFeatures", SvgAttributeKind.Svg11Legacy)]
    [TestCase("", "attributeType", SvgAttributeKind.Svg11Legacy)]
    [TestCase("", "horiz-adv-x", SvgAttributeKind.Svg11Legacy)]
    [TestCase("", "enable-background", SvgAttributeKind.Svg11Legacy)]
    [TestCase("http://www.w3.org/1999/xlink", "href", SvgAttributeKind.XLink)]
    [TestCase("http://www.w3.org/1999/xlink", "arcrole", SvgAttributeKind.XLink)]
    [TestCase("http://www.w3.org/1999/xlink", "bogus", SvgAttributeKind.Unknown)]
    [TestCase("http://www.w3.org/XML/1998/namespace", "base", SvgAttributeKind.Xml)]
    [TestCase("http://www.w3.org/XML/1998/namespace", "lang", SvgAttributeKind.Xml)]
    [TestCase("http://www.w3.org/XML/1998/namespace", "space", SvgAttributeKind.Xml)]
    [TestCase("http://www.w3.org/XML/1998/namespace", "href", SvgAttributeKind.Unknown)]
    [TestCase("http://www.w3.org/2000/xmlns/", "vendor", SvgAttributeKind.NamespaceDeclaration)]
    [TestCase("", "aria-label", SvgAttributeKind.Aria)]
    [TestCase("", "aria-valuenow", SvgAttributeKind.Aria)]
    [TestCase("", "aria-madeup", SvgAttributeKind.Unknown)]
    [TestCase("", "role", SvgAttributeKind.Aria)]
    [TestCase("", "data-user-id", SvgAttributeKind.Data)]
    [TestCase("", "data-", SvgAttributeKind.Unknown)]
    [TestCase("", "futureAttribute", SvgAttributeKind.Unknown)]
    [TestCase("urn:vendor", "viewBox", SvgAttributeKind.Unknown)]
    [TestCase("http://www.w3.org/2000/svg", "viewBox", SvgAttributeKind.Unknown)]
    public void AttributeRecognitionUsesExpandedNamesAndStableCategories(string namespaceUri, string localName, SvgAttributeKind expected)
    {
        Assert.That(new SvgNodeNames().GetAttributeKind(namespaceUri, localName), Is.EqualTo(expected));
    }

    [Test]
    public void LoadedNodesAndAttributesReceiveRecognitionKindsWithoutDroppingUnknownContent()
    {
        string input = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' xmlns:v='urn:vendor' " +
            "id='root' role='img' aria-label='chart' data-key='one' future='kept' xml:lang='en' xlink:href='#old' v:href='#vendor'>" +
            "<rect fill='red' onclick='ignored'/><font requiredFeatures='legacy'/><flowRoot/><v:rect/></svg>";

        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        SvgNode root = result.Document.DocumentElement;
        SvgNode rect = root.Children[0];
        SvgNode font = root.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(root.ElementKind, Is.EqualTo(SvgElementKind.Svg));
            Assert.That(rect.ElementKind, Is.EqualTo(SvgElementKind.Rect));
            Assert.That(font.ElementKind, Is.EqualTo(SvgElementKind.Font));
            Assert.That(new SvgNodeNames().IsSvg11LegacyElement(font.ElementKind), Is.True);
            Assert.That(root.Children[2].ElementKind, Is.EqualTo(SvgElementKind.Unknown));
            Assert.That(root.Children[3].ElementKind, Is.EqualTo(SvgElementKind.Unknown));
            Assert.That(root.Attributes.Select(attribute => attribute.AttributeKind), Is.EqualTo(new[]
            {
                SvgAttributeKind.NamespaceDeclaration,
                SvgAttributeKind.NamespaceDeclaration,
                SvgAttributeKind.NamespaceDeclaration,
                SvgAttributeKind.Svg2,
                SvgAttributeKind.Aria,
                SvgAttributeKind.Aria,
                SvgAttributeKind.Data,
                SvgAttributeKind.Unknown,
                SvgAttributeKind.Xml,
                SvgAttributeKind.XLink,
                SvgAttributeKind.Unknown
            }));
            Assert.That(rect.Attributes.Select(attribute => attribute.AttributeKind), Is.EqualTo(new[] { SvgAttributeKind.Svg2, SvgAttributeKind.Svg2 }));
            Assert.That(font.Attributes[0].AttributeKind, Is.EqualTo(SvgAttributeKind.Svg11Legacy));
            Assert.That(root.Children[2].QualifiedName, Is.EqualTo("flowRoot"));
            Assert.That(root.Children[3].QualifiedName, Is.EqualTo("v:rect"));
            Assert.That(root.Attributes[7].Value, Is.EqualTo("kept"));
        });
    }

    [Test]
    public void RecognitionRejectsNullArgumentsAndInvalidKindsRemainUnrecognized()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => new SvgNodeNames().GetElementKind(null, "svg"));
            Assert.Throws<ArgumentNullException>(() => new SvgNodeNames().GetElementKind("", null));
            Assert.Throws<ArgumentNullException>(() => new SvgNodeNames().GetAttributeKind(null, "id"));
            Assert.Throws<ArgumentNullException>(() => new SvgNodeNames().GetAttributeKind("", null));
            Assert.That(new SvgNodeNames().IsSvg2Element((SvgElementKind)999), Is.False);
            Assert.That(new SvgNodeNames().IsSvg11LegacyElement((SvgElementKind)999), Is.False);
        });
    }
}
