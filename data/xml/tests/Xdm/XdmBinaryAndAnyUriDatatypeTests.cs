using xml;

namespace xml.Tests;

[TestFixture]
public class XdmBinaryAndAnyUriDatatypeTests
{
    [TestCase("", "", new byte[] { })]
    [TestCase("  0fB7\t", "0FB7", new byte[] { 0x0F, 0xB7 })]
    [TestCase("deadBEEF", "DEADBEEF", new byte[] { 0xDE, 0xAD, 0xBE, 0xEF })]
    public void ParsesHexBinaryAndProducesUppercaseCanonicalForms(string lexical, string canonical, byte[] expected)
    {
        XdmAtomicValue value = new XdmBinaryDatatype().ParseXdmBinary(lexical, XdmAtomicType.HexBinary);

        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(XdmAtomicType.HexBinary));
            Assert.That(value.LexicalValue, Is.EqualTo(lexical));
            Assert.That(value.CanonicalValue, Is.EqualTo(canonical));
            Assert.That(value.StringValue, Is.EqualTo(canonical));
            Assert.That(value.BinaryValue, Is.EqualTo(expected));
            Assert.That(value.IsImmutable, Is.True);
        });
    }

    [TestCase("0")]
    [TestCase("0FG7")]
    [TestCase("00 11")]
    [TestCase("+-")]
    public void RejectsInvalidHexBinaryLexicalForms(string lexical)
    {
        XdmBinaryDatatype datatype = new XdmBinaryDatatype();
        XdmNumericParseResult result = datatype.TryParseXdmBinary(lexical, XdmAtomicType.HexBinary);
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmBinary(lexical, XdmAtomicType.HexBinary))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.ErrorCode, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
        });
    }

    [TestCase("", "", new byte[] { })]
    [TestCase(" Z G V z d A = = ", "ZGVzdA==", new byte[] { 100, 101, 115, 116 })]
    [TestCase("\tYWJj\r\nZA== ", "YWJjZA==", new byte[] { 97, 98, 99, 100 })]
    [TestCase("AP+A", "AP+A", new byte[] { 0, 255, 128 })]
    public void ParsesBase64BinaryWhitespaceAndProducesCanonicalForms(string lexical, string canonical, byte[] expected)
    {
        XdmAtomicValue value = new XdmBinaryDatatype().ParseXdmBinary(lexical, XdmAtomicType.Base64Binary);

        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(XdmAtomicType.Base64Binary));
            Assert.That(value.LexicalValue, Is.EqualTo(lexical));
            Assert.That(value.CanonicalValue, Is.EqualTo(canonical));
            Assert.That(value.StringValue, Is.EqualTo(canonical));
            Assert.That(value.BinaryValue, Is.EqualTo(expected));
        });
    }

    [Test]
    public void AcceptsBase64WithoutMimeLineLengthRestrictions()
    {
        byte[] bytes = new byte[90];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)index;
            index = index + 1;
        }

        string lexical = Convert.ToBase64String(bytes);
        XdmAtomicValue value = new XdmBinaryDatatype().ParseXdmBinary(lexical, XdmAtomicType.Base64Binary);

        Assert.Multiple(() =>
        {
            Assert.That(lexical.Length, Is.GreaterThan(76));
            Assert.That(value.BinaryValue, Is.EqualTo(bytes));
            Assert.That(value.CanonicalValue, Is.EqualTo(lexical));
        });
    }

    [TestCase("A")]
    [TestCase("AAA")]
    [TestCase("A===")]
    [TestCase("=AAA")]
    [TestCase("AA=A")]
    [TestCase("AAAA=")]
    [TestCase("AA==AA==")]
    [TestCase("Zh==")]
    [TestCase("Zm9=")]
    [TestCase("AAAA_")]
    [TestCase("AA\u00A0==")]
    public void RejectsMalformedBase64IncludingNonzeroPaddingBits(string lexical)
    {
        XdmBinaryDatatype datatype = new XdmBinaryDatatype();
        XdmNumericParseResult result = datatype.TryParseXdmBinary(lexical, XdmAtomicType.Base64Binary);
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmBinary(lexical, XdmAtomicType.Base64Binary))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
        });
    }

    [TestCase(XdmAtomicType.HexBinary)]
    [TestCase(XdmAtomicType.Base64Binary)]
    public void ComparesBinaryValuesByUnsignedOctetSequence(XdmAtomicType type)
    {
        XdmBinaryDatatype datatype = new XdmBinaryDatatype();
        XdmAtomicValue empty = datatype.CreateXdmBinaryValue([], type, null);
        XdmAtomicValue prefix = datatype.CreateXdmBinaryValue([0x01], type, null);
        XdmAtomicValue longer = datatype.CreateXdmBinaryValue([0x01, 0x00], type, null);
        XdmAtomicValue greater = datatype.CreateXdmBinaryValue([0x01, 0x80], type, null);
        XdmAtomicValue equal = datatype.CreateXdmBinaryValue([0x01, 0x80], type, null);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.CompareXdmBinary(empty, prefix), Is.EqualTo(-1));
            Assert.That(datatype.CompareXdmBinary(prefix, longer), Is.EqualTo(-1));
            Assert.That(datatype.CompareXdmBinary(longer, greater), Is.EqualTo(-1));
            Assert.That(datatype.EqualXdmBinary(greater, equal), Is.True);
            Assert.That(datatype.LessThanXdmBinary(longer, greater), Is.True);
            Assert.That(datatype.GreaterThanXdmBinary(greater, longer), Is.True);
        });
    }

    [Test]
    public void RequiresMatchingBinaryTypesForComparisonAndCopiesCreatedStorage()
    {
        XdmBinaryDatatype datatype = new XdmBinaryDatatype();
        byte[] source = [0x0F, 0xB7];
        XdmAtomicValue hexadecimal = datatype.CreateXdmBinaryValue(source, XdmAtomicType.HexBinary, null);
        XdmAtomicValue base64 = datatype.CreateXdmBinaryValue(source, XdmAtomicType.Base64Binary, null);
        source[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(hexadecimal.BinaryValue, Is.EqualTo(new byte[] { 0x0F, 0xB7 }));
            Assert.That(hexadecimal.CanonicalValue, Is.EqualTo("0FB7"));
            Assert.That(base64.CanonicalValue, Is.EqualTo("D7c="));
            Assert.Throws<ArgumentException>(() => datatype.CompareXdmBinary(hexadecimal, base64));
            Assert.Throws<ArgumentException>(() => datatype.ParseXdmBinary("00", XdmAtomicType.String));
            Assert.Throws<ArgumentNullException>(() => datatype.ParseXdmBinary(null!, XdmAtomicType.HexBinary));
        });
    }

    [TestCase("", "")]
    [TestCase(" \thttps://example.test/a  b\r\n", "https://example.test/a b")]
    [TestCase("relative path/%GG", "relative path/%GG")]
    [TestCase("https://例え.test/資料?q=é", "https://例え.test/資料?q=é")]
    [TestCase("a%2fb", "a%2fb")]
    public void ParsesAnyUriWithCollapseAndIdentityEscapeSemantics(string lexical, string expected)
    {
        XdmAtomicValue value = new XdmAnyUriDatatype().ParseXdmAnyUri(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(XdmAtomicType.AnyUri));
            Assert.That(value.LexicalValue, Is.EqualTo(lexical));
            Assert.That(value.StringValue, Is.EqualTo(expected));
            Assert.That(value.CanonicalValue, Is.EqualTo(expected));
            Assert.That(value.IsImmutable, Is.True);
        });
    }

    [Test]
    public void AnyUriEqualityDoesNotDecodeOrNormalizeEscapes()
    {
        XdmAnyUriDatatype datatype = new XdmAnyUriDatatype();
        XdmAtomicValue escaped = datatype.ParseXdmAnyUri("a%20b");
        XdmAtomicValue spaced = datatype.ParseXdmAnyUri("a b");
        XdmAtomicValue lowerEscape = datatype.ParseXdmAnyUri("a%2fb");
        XdmAtomicValue upperEscape = datatype.ParseXdmAnyUri("a%2Fb");

        Assert.Multiple(() =>
        {
            Assert.That(datatype.EqualXdmAnyUri(escaped, datatype.ParseXdmAnyUri("a%20b")), Is.True);
            Assert.That(datatype.EqualXdmAnyUri(escaped, spaced), Is.False);
            Assert.That(datatype.EqualXdmAnyUri(lowerEscape, upperEscape), Is.False);
            Assert.That(datatype.CanonicalizeXdmAnyUri("relative path/%GG"), Is.EqualTo("relative path/%GG"));
            Assert.Throws<XPathDynamicException>(() => datatype.CanonicalizeXdmAnyUri(" leading"));
        });
    }

    [Test]
    public void RejectsAnyUriCharactersOutsideTheXmlCharacterSet()
    {
        XdmAnyUriDatatype datatype = new XdmAnyUriDatatype();
        string[] invalid = ["\0", new string('\uD800', 1), new string('\uDC00', 1), "\uFFFF"];

        foreach (string lexical in invalid)
        {
            XdmStringParseResult result = datatype.TryParseXdmAnyUri(lexical);
            XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmAnyUri(lexical))!;
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
            });
        }
    }

    [Test]
    public void FactoryCreatesAndValidatesBinaryAndAnyUriRepresentations()
    {
        XdmFactory factory = new XdmFactory();
        XdmAtomicValue binary = factory.CreateXdmBinaryAtomicValue(" 0fb7 ", XdmAtomicType.HexBinary);
        XdmAtomicValue uri = factory.CreateXdmAnyUriAtomicValue("  a%20b  ");

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => factory.ValidateXdmAtomicValue(binary));
            Assert.DoesNotThrow(() => factory.ValidateXdmAtomicValue(uri));
            Assert.That(new SequenceNormalizer().CastXdmAtomicToSerializationString(binary), Is.EqualTo("0FB7"));
            Assert.That(new SequenceNormalizer().CastXdmAtomicToSerializationString(uri), Is.EqualTo("a%20b"));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.HexBinary, BinaryValue = [0], CanonicalValue = "01" }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.Base64Binary, BinaryValue = [0], LexicalValue = "AQ==" }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.AnyUri, StringValue = "a b", LexicalValue = "different" }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.AnyUri, StringValue = " a" }));
        });
    }
}
