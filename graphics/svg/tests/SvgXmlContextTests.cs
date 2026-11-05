namespace svg.Tests;

public class SvgXmlContextTests
{
    [Test]
    public void XmlBaseLookupReturnsNearestValueAndResolvesTheAncestorChain()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load(
            "<svg xml:base='assets/'><g xml:base='../shared/'><rect/></g><circle/></svg>",
            "https://example.test/root/document.svg");
        SvgNode root = document.DocumentElement;
        SvgNode group = root.Children[0];
        SvgNode rectangle = group.Children[0];
        SvgNode circle = root.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(api.LookupXmlBase(rectangle), Is.EqualTo("../shared/"));
            Assert.That(api.ResolveXmlBase(rectangle), Is.EqualTo("https://example.test/root/shared/"));
            Assert.That(api.ResolveXmlBase(circle), Is.EqualTo("https://example.test/root/assets/"));
            Assert.That(api.GetInheritedXmlAttribute(rectangle, SvgAttributeNames.Base), Is.SameAs(group.Attributes[0]));
        });
    }

    [Test]
    public void XmlLanguageUsesNearestValueAndEmptyValuesResetInheritance()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg xml:lang='en'><g><text>one</text></g><g xml:lang=''><text>two</text></g><g xml:lang='fr'><text>trois</text></g></svg>", "");
        SvgNode root = document.DocumentElement;
        SvgNode inheritedText = root.Children[0].Children[0].Children[0];
        SvgNode resetText = root.Children[1].Children[0].Children[0];
        SvgNode frenchText = root.Children[2].Children[0].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(api.LookupXmlLanguage(inheritedText), Is.EqualTo("en"));
            Assert.That(api.LookupXmlLanguage(resetText), Is.Empty);
            Assert.That(api.LookupXmlLanguage(frenchText), Is.EqualTo("fr"));
            Assert.That(api.GetInheritedXmlAttribute(resetText, SvgAttributeNames.Lang), Is.SameAs(root.Children[1].Attributes[0]));
        });
    }

    [Test]
    public void XmlSpaceInheritsOverridesAndReportsInvalidNearestValues()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg xml:space='preserve'><g><text>a</text></g><g xml:space='default'><text>b</text></g><g xml:space='other'><text>c</text></g></svg>", "");
        SvgNode root = document.DocumentElement;
        SvgNode preserved = root.Children[0].Children[0].Children[0];
        SvgNode reset = root.Children[1].Children[0].Children[0];
        SvgNode invalid = root.Children[2].Children[0].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(api.LookupXmlSpace(preserved), Is.EqualTo(SvgXmlSpaceMode.Preserve));
            Assert.That(api.LookupXmlSpace(reset), Is.EqualTo(SvgXmlSpaceMode.Default));
            Assert.That(api.LookupXmlSpace(invalid), Is.EqualTo(SvgXmlSpaceMode.Invalid));
            Assert.That(api.LookupXmlSpace(new SvgNode(SvgNodeKind.Text, "", "", "", "", "x")), Is.EqualTo(SvgXmlSpaceMode.Default));
        });
    }

    [Test]
    public void WhitespacePoliciesPreserveSvg11LegacyRulesAndSvg2CssPriority()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg><text xml:space='preserve'> value </text><text> value </text></svg>", "");
        SvgNode preservedText = document.DocumentElement.Children[0].Children[0];
        SvgNode defaultText = document.DocumentElement.Children[1].Children[0];

        SvgTextWhitespacePolicy svg11Preserve = api.GetTextWhitespacePolicy(preservedText, SvgTextWhitespaceProfile.Svg11);
        SvgTextWhitespacePolicy svg11Default = api.GetTextWhitespacePolicy(defaultText, SvgTextWhitespaceProfile.Svg11);
        SvgTextWhitespacePolicy svg2Preserve = api.GetTextWhitespacePolicy(preservedText, SvgTextWhitespaceProfile.Svg2);

        Assert.Multiple(() =>
        {
            Assert.That(svg11Preserve.XmlSpace, Is.EqualTo(SvgXmlSpaceMode.Preserve));
            Assert.That(svg11Preserve.SourceAttribute, Is.SameAs(document.DocumentElement.Children[0].Attributes[0]));
            Assert.That(svg11Preserve.CssWhiteSpaceHasPriority, Is.False);
            Assert.That(svg11Preserve.RemoveNewLines, Is.False);
            Assert.That(svg11Preserve.ConvertNewLinesToSpaces, Is.True);
            Assert.That(svg11Preserve.ConvertTabsToSpaces, Is.True);
            Assert.That(svg11Preserve.TrimSpaces, Is.False);
            Assert.That(svg11Preserve.CollapseSpaces, Is.False);
            Assert.That(svg11Default.RemoveNewLines, Is.True);
            Assert.That(svg11Default.ConvertNewLinesToSpaces, Is.False);
            Assert.That(svg11Default.TrimSpaces, Is.True);
            Assert.That(svg11Default.CollapseSpaces, Is.True);
            Assert.That(svg2Preserve.CssWhiteSpaceHasPriority, Is.True);
            Assert.That(svg2Preserve.XmlSpace, Is.EqualTo(SvgXmlSpaceMode.Preserve));
        });
    }

    [Test]
    public void LegacyWhitespaceProcessingMatchesDefaultAndPreserveAlgorithms()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg><text>default</text><text xml:space='preserve'>preserve</text><text xml:space='invalid'>invalid</text></svg>", "");
        SvgNode defaultText = document.DocumentElement.Children[0].Children[0];
        SvgNode preserveText = document.DocumentElement.Children[1].Children[0];
        SvgNode invalidText = document.DocumentElement.Children[2].Children[0];
        string value = " \t alpha  \n beta\r\ngamma \t ";

        Assert.Multiple(() =>
        {
            Assert.That(api.ApplyLegacyTextWhitespace(defaultText, value), Is.EqualTo("alpha betagamma"));
            Assert.That(api.ApplyLegacyTextWhitespace(preserveText, value), Is.EqualTo("   alpha    beta gamma   "));
            Assert.That(api.ApplyLegacyTextWhitespace(invalidText, value), Is.EqualTo("alpha betagamma"));
        });
    }

    [Test]
    public void InheritedLookupReflectsTreeMovesWithoutCachedState()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgDocument document = Load("<svg><g xml:lang='en'><text>value</text></g><g xml:lang='fr'/></svg>", "");
        SvgNode englishGroup = document.DocumentElement.Children[0];
        SvgNode frenchGroup = document.DocumentElement.Children[1];
        SvgNode text = englishGroup.Children[0];

        Assert.That(api.LookupXmlLanguage(text), Is.EqualTo("en"));
        SvgMutationResult moved = api.MoveChild(text, frenchGroup, 0);

        Assert.Multiple(() =>
        {
            Assert.That(moved.Success, Is.True);
            Assert.That(api.LookupXmlLanguage(text), Is.EqualTo("fr"));
            Assert.That(api.LookupXmlLanguage(text.Children[0]), Is.EqualTo("fr"));
        });
    }

    [Test]
    public void XmlContextMethodsValidateRequiredArgumentsAndProfiles()
    {
        SvgNodeApi api = new SvgNodeApi();
        SvgNode node = new SvgNode(SvgNodeKind.Element, "g", "", "g", SvgNamespaces.Svg, "");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => api.GetInheritedXmlAttribute(null, SvgAttributeNames.Lang));
            Assert.Throws<ArgumentNullException>(() => api.GetInheritedXmlAttribute(node, null));
            Assert.Throws<ArgumentNullException>(() => api.LookupXmlBase(null));
            Assert.Throws<ArgumentNullException>(() => api.ResolveXmlBase(null));
            Assert.Throws<ArgumentNullException>(() => api.LookupXmlLanguage(null));
            Assert.Throws<ArgumentNullException>(() => api.LookupXmlSpace(null));
            Assert.Throws<ArgumentNullException>(() => api.ApplyLegacyTextWhitespace(node, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => api.GetTextWhitespacePolicy(node, (SvgTextWhitespaceProfile)99));
        });
    }

    static SvgDocument Load(string input, string baseUri)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = baseUri;
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);
        Assert.That(loaded.Success, Is.True);
        return loaded.Document;
    }
}
