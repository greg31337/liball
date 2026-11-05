using xml;

namespace xml.Tests;

[TestFixture]
public class XmlElementTagParserTests
{
    [Test]
    public void ParsesOrderedRawAttributesAndPreservesQuotesAndRanges()
    {
        string text = "<root b='one&amp;' a = \"two&#65;\">";
        XmlElementTagParser parser = CreateParser(text, true, null);

        XmlElementTag tag = parser.ParseNextXmlElementTag();

        Assert.That(tag.Name, Is.EqualTo("root"));
        Assert.That(tag.IsEndElement, Is.False);
        Assert.That(tag.IsEmptyElement, Is.False);
        Assert.That(tag.Depth, Is.Zero);
        Assert.That(tag.StartUtf16Offset, Is.Zero);
        Assert.That(tag.Utf16Length, Is.EqualTo(text.Length));
        Assert.That(tag.Attributes.Select(attribute => attribute.Name), Is.EqualTo(new string[] { "b", "a" }));
        Assert.That(tag.Attributes.Select(attribute => attribute.RawValue), Is.EqualTo(new string[] { "one&amp;", "two&#65;" }));
        Assert.That(tag.Attributes.Select(attribute => attribute.Quote), Is.EqualTo(new char[] { '\'', '"' }));
        Assert.That(tag.Attributes[0].StartUtf16Offset, Is.EqualTo(6));
        Assert.That(tag.Attributes[0].Utf16Length, Is.EqualTo(12));
        Assert.That(tag.Attributes[1].StartUtf16Offset, Is.EqualTo(19));
        Assert.That(tag.Attributes[1].Utf16Length, Is.EqualTo(14));
        Assert.That(parser.CurrentXmlElementDepth(), Is.EqualTo(1));
    }

    [Test]
    public void CanDiscardLexicalQuoteChoiceWhileRetainingRawValues()
    {
        XmlElementTagParser parser = CreateParser("<r a='one' b=\"two\"/>", false, null);

        XmlElementTag tag = parser.ParseNextXmlElementTag();

        Assert.That(tag.Attributes.Select(attribute => attribute.RawValue), Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(tag.Attributes.Select(attribute => attribute.Quote), Is.EqualTo(new char[] { '\0', '\0' }));
        Assert.That(tag.IsEmptyElement, Is.True);
        Assert.That(parser.CurrentXmlElementDepth(), Is.Zero);
    }

    [Test]
    public void TracksNestedStartEmptyAndEndElementDepths()
    {
        XmlElementTagParser parser = CreateParser("<outer><middle><leaf/></middle></outer>", true, null);

        XmlElementTag outerStart = parser.ParseNextXmlElementTag();
        XmlElementTag middleStart = parser.ParseNextXmlElementTag();
        XmlElementTag leaf = parser.ParseNextXmlElementTag();
        XmlElementTag middleEnd = parser.ParseNextXmlElementTag();
        XmlElementTag outerEnd = parser.ParseNextXmlElementTag();

        Assert.That(new int[] { outerStart.Depth, middleStart.Depth, leaf.Depth, middleEnd.Depth, outerEnd.Depth }, Is.EqualTo(new int[] { 0, 1, 2, 1, 0 }));
        Assert.That(new bool[] { outerStart.IsEmptyElement, middleStart.IsEmptyElement, leaf.IsEmptyElement }, Is.EqualTo(new bool[] { false, false, true }));
        Assert.That(middleEnd.IsEndElement, Is.True);
        Assert.That(outerEnd.IsEndElement, Is.True);
        Assert.That(middleEnd.Attributes, Is.Empty);
        Assert.That(parser.CurrentXmlElementDepth(), Is.Zero);
    }

    [Test]
    public void ParsesAnEndTagAfterTheCallerConsumesContent()
    {
        XmlScanner scanner = CreateScanner("<root>text</root>");
        XmlElementTagParser parser = new XmlElementTagParser(scanner, true, null);

        XmlElementTag start = parser.ParseNextXmlElementTag();
        XmlToken text = scanner.ScanNextXmlToken();
        XmlElementTag end = parser.ParseNextXmlElementTag();

        Assert.That(start.Name, Is.EqualTo("root"));
        Assert.That(text.Kind, Is.EqualTo(XmlTokenKind.Text));
        Assert.That(text.Value, Is.EqualTo("text"));
        Assert.That(end.Name, Is.EqualTo("root"));
        Assert.That(end.Depth, Is.Zero);
        Assert.That(end.Utf16Length, Is.EqualTo(7));
    }

    [TestCase("<root a='one' a=\"two\">")]
    [TestCase("<root p:a='one' p:a='two'>")]
    public void RejectsLexicallyDuplicateAttributes(string text)
    {
        XmlElementTagParser parser = CreateParser(text, true, null);

        Assert.Throws<FormatException>(() => parser.ParseNextXmlElementTag());
        Assert.That(parser.CurrentXmlElementDepth(), Is.Zero);
    }

    [Test]
    public void CallsDuplicateHookWithPrecedingAttributesInSourceOrder()
    {
        CaseInsensitiveDuplicateDetector detector = new CaseInsensitiveDuplicateDetector();
        XmlElementTagParser parser = CreateParser("<root first='1' second='2' FIRST='3'>", true, detector);

        Assert.Throws<FormatException>(() => parser.ParseNextXmlElementTag());

        Assert.That(detector.CallCount, Is.EqualTo(3));
        Assert.That(detector.LastCandidateName, Is.EqualTo("FIRST"));
        Assert.That(detector.LastPrecedingNames, Is.EqualTo(new string[] { "first", "second" }));
        Assert.That(parser.CurrentXmlElementDepth(), Is.Zero);
    }

    [TestCase("<root a='one'b='two'>")]
    [TestCase("<root a>")]
    [TestCase("<root a=>")]
    [TestCase("<root a=value>")]
    [TestCase("< root>")]
    [TestCase("<root a='x<y'>")]
    public void RejectsMalformedStartTagsAndAttributeValues(string text)
    {
        XmlElementTagParser parser = CreateParser(text, true, null);

        Assert.Throws<FormatException>(() => parser.ParseNextXmlElementTag());
        Assert.That(parser.CurrentXmlElementDepth(), Is.Zero);
    }

    [TestCase("<root a='&missing'>")]
    [TestCase("<root a='&;'>")]
    [TestCase("<root a='&9name;'>")]
    [TestCase("<root a='&name space;'>")]
    [TestCase("<root a='&#;'>")]
    [TestCase("<root a='&#x;'>")]
    [TestCase("<root a='&#12x;'>")]
    [TestCase("<root a='&#xG;'>")]
    [TestCase("<root a='&#0;'>")]
    [TestCase("<root a='&#x110000;'>")]
    public void RejectsMalformedReferencesInsideAttributeValues(string text)
    {
        XmlElementTagParser parser = CreateParser(text, true, null);

        Assert.Throws<FormatException>(() => parser.ParseNextXmlElementTag());
        Assert.That(parser.CurrentXmlElementDepth(), Is.Zero);
    }

    [Test]
    public void AcceptsEveryAttributeReferenceFormAndPermittedCDataClosingText()
    {
        string text = "<root a='&amp;&entity;&#65;&#x1F600;]]>'>";
        XmlElementTag tag = CreateParser(text, true, null).ParseNextXmlElementTag();

        Assert.That(tag.Attributes[0].RawValue, Is.EqualTo("&amp;&entity;&#65;&#x1F600;]]>"));
    }

    [TestCase("</root x>")]
    [TestCase("</root/>")]
    [TestCase("</ root>")]
    public void RejectsMalformedEndTags(string endTag)
    {
        XmlElementTagParser parser = CreateParser("<root>" + endTag, true, null);
        parser.ParseNextXmlElementTag();

        Assert.Throws<FormatException>(() => parser.ParseNextXmlElementTag());
        Assert.That(parser.CurrentXmlElementDepth(), Is.EqualTo(1));
    }

    [Test]
    public void RejectsMismatchedAndUnmatchedEndTagsWithoutChangingDepth()
    {
        XmlElementTagParser mismatch = CreateParser("<outer><inner></outer>", true, null);
        mismatch.ParseNextXmlElementTag();
        mismatch.ParseNextXmlElementTag();

        Assert.Throws<FormatException>(() => mismatch.ParseNextXmlElementTag());
        Assert.That(mismatch.CurrentXmlElementDepth(), Is.EqualTo(2));

        XmlElementTagParser unmatched = CreateParser("</orphan>", true, null);
        Assert.Throws<FormatException>(() => unmatched.ParseNextXmlElementTag());
        Assert.That(unmatched.CurrentXmlElementDepth(), Is.Zero);
    }

    [Test]
    public void RequiresTheNextScannerTokenToOpenAnElementTag()
    {
        XmlElementTagParser parser = CreateParser("text", true, null);

        Assert.Throws<FormatException>(() => parser.ParseNextXmlElementTag());
        Assert.That(parser.CurrentXmlElementDepth(), Is.Zero);
    }

    private static XmlElementTagParser CreateParser(string text, bool preserveAttributeQuotes, IXmlAttributeDuplicateDetector? duplicateDetector)
    {
        return new XmlElementTagParser(CreateScanner(text), preserveAttributeQuotes, duplicateDetector);
    }

    private static XmlScanner CreateScanner(string text)
    {
        return new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
    }

    private class CaseInsensitiveDuplicateDetector : IXmlAttributeDuplicateDetector
    {
        public int CallCount;
        public string? LastCandidateName;
        public string[]? LastPrecedingNames;

        public bool IsDuplicateXmlAttribute(XmlRawAttribute[] precedingAttributes, XmlRawAttribute candidate)
        {
            CallCount = CallCount + 1;
            LastCandidateName = candidate.Name;
            LastPrecedingNames = precedingAttributes.Select(attribute => attribute.Name).ToArray();
            return precedingAttributes.Any(attribute => string.Equals(attribute.Name, candidate.Name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
