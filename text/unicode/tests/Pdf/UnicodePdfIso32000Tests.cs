using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfIso32000Tests
{
    [Test]
    public void PdfDocEncoding_AllByteMappingsMatchIso32000()
    {
        UnicodePdfEncoding encoding = new UnicodePdfEncoding();
        int[] expectedCodePoints = CreateExpectedPdfDocEncoding();

        int byteValue = 0;
        while (byteValue <= 0xFF)
        {
            string expected = char.ConvertFromUtf32(expectedCodePoints[byteValue]);
            string actual = encoding.DecodePdfDocEncoding(new byte[] { (byte)byteValue });

            Assert.That(actual, Is.EqualTo(expected), "byte " + byteValue.ToString("X2"));
            if (expectedCodePoints[byteValue] == UnicodeScalar.ReplacementCharacter)
            {
                Assert.That(
                    UnicodePdfEncoding.FindPdfDocEncodingByte(expectedCodePoints[byteValue]),
                    Is.EqualTo(-1),
                    "undefined byte " + byteValue.ToString("X2"));
            }
            else
            {
                Assert.That(
                    UnicodePdfEncoding.FindPdfDocEncodingByte(expectedCodePoints[byteValue]),
                    Is.EqualTo(byteValue),
                    "reverse byte " + byteValue.ToString("X2"));
            }

            byteValue = byteValue + 1;
        }
    }

    [Test]
    public void PdfDocEncoding_AllDefinedBytesRoundTripTogether()
    {
        UnicodePdfEncoding encoding = new UnicodePdfEncoding();
        int[] expectedCodePoints = CreateExpectedPdfDocEncoding();
        System.Text.StringBuilder textBuilder = new System.Text.StringBuilder(253);
        System.Collections.Generic.List<byte> byteBuilder = new System.Collections.Generic.List<byte>(253);

        int byteValue = 0;
        while (byteValue <= 0xFF)
        {
            if (expectedCodePoints[byteValue] != UnicodeScalar.ReplacementCharacter)
            {
                textBuilder.Append(char.ConvertFromUtf32(expectedCodePoints[byteValue]));
                byteBuilder.Add((byte)byteValue);
            }

            byteValue = byteValue + 1;
        }

        string text = textBuilder.ToString();
        byte[] bytes = byteBuilder.ToArray();

        Assert.That(encoding.CanEncodePdfDocEncoding(text), Is.True);
        Assert.That(encoding.EncodePdfDocEncoding(text), Is.EqualTo(bytes));
        Assert.That(encoding.DecodePdfDocEncoding(bytes), Is.EqualTo(text));
        Assert.That(encoding.GetTextStringEncoding(text), Is.EqualTo(UnicodePdfTextEncoding.PdfDocEncoding));
        Assert.That(encoding.EncodeTextString(text), Is.EqualTo(bytes));
        Assert.That(encoding.DecodeTextString(bytes), Is.EqualTo(text));
    }

    [Test]
    public void TextStringSelection_AvoidsReservedUtf16BeBomPrefix()
    {
        UnicodePdfEncoding encoding = new UnicodePdfEncoding();
        string text = "\u00FE\u00FFA";
        byte[] expected = new byte[] { 0xFE, 0xFF, 0x00, 0xFE, 0x00, 0xFF, 0x00, 0x41 };

        Assert.That(encoding.CanEncodePdfDocEncoding(text), Is.True);
        Assert.That(encoding.EncodePdfDocEncoding(text), Is.EqualTo(new byte[] { 0xFE, 0xFF, 0x41 }));
        Assert.That(encoding.GetTextStringEncoding(text), Is.EqualTo(UnicodePdfTextEncoding.Utf16BeWithBom));
        Assert.That(encoding.EncodeTextString(text), Is.EqualTo(expected));
        Assert.That(encoding.DecodeTextString(expected), Is.EqualTo(text));

        byte[] destination = new byte[expected.Length];
        UnicodeBufferResult result = encoding.EncodeTextStringBuffer(text.AsSpan(), destination);
        Assert.That(result.Success, Is.True);
        Assert.That(result.RequiredLength, Is.EqualTo(expected.Length));
        Assert.That(destination, Is.EqualTo(expected));

        Assert.That(encoding.GetTextStringEncoding("A\u00FE\u00FF"), Is.EqualTo(UnicodePdfTextEncoding.PdfDocEncoding));
        Assert.That(encoding.GetTextStringEncoding("\u00FF\u00FE"), Is.EqualTo(UnicodePdfTextEncoding.PdfDocEncoding));
    }

    [Test]
    public void PdfDocEncoding_UndefinedBytesAndScalarsUseExplicitPolicies()
    {
        UnicodePdfEncoding encoding = new UnicodePdfEncoding();
        byte[] undefinedBytes = new byte[] { 0x7F, 0x9F, 0xAD };
        string unmappableScalars = "\u007F\u009F\u00AD";

        Assert.That(encoding.DecodePdfDocEncoding(undefinedBytes), Is.EqualTo("\uFFFD\uFFFD\uFFFD"));
        Assert.That(encoding.DecodeTextString(undefinedBytes), Is.EqualTo("\uFFFD\uFFFD\uFFFD"));
        Assert.That(encoding.CanEncodePdfDocEncoding(unmappableScalars), Is.False);
        Assert.That(encoding.GetTextStringEncoding(unmappableScalars), Is.EqualTo(UnicodePdfTextEncoding.Utf16BeWithBom));
        Assert.That(encoding.DecodeTextString(encoding.EncodeTextString(unmappableScalars)), Is.EqualTo(unmappableScalars));
        Assert.Throws<ArgumentException>(() => encoding.EncodePdfDocEncoding(unmappableScalars));
    }

    [Test]
    public void TextStringDecoding_RecognizesOnlyLeadingUtf16BeBom()
    {
        UnicodePdfEncoding encoding = new UnicodePdfEncoding();

        Assert.That(encoding.DecodeTextString(new byte[] { 0xFE }), Is.EqualTo("\u00FE"));
        Assert.That(encoding.DecodeTextString(new byte[] { 0xFF, 0xFE }), Is.EqualTo("\u00FF\u00FE"));
        Assert.That(encoding.DecodeTextString(new byte[] { 0x41, 0xFE, 0xFF }), Is.EqualTo("A\u00FE\u00FF"));
        Assert.That(encoding.DecodeTextString(new byte[] { 0xFE, 0xFF }), Is.Empty);
        Assert.That(encoding.DecodeTextString(new byte[] { 0xFE, 0xFF, 0x00, 0x41 }), Is.EqualTo("A"));
        Assert.That(encoding.DecodeTextString(new byte[] { 0xFE, 0xFF, 0xFE, 0xFF }), Is.EqualTo("\uFEFF"));
    }

    [Test]
    public void Utf16BeDecoding_ReplacesEveryMalformedByteShape()
    {
        UnicodePdfEncoding encoding = new UnicodePdfEncoding();
        byte[][] inputs = new byte[][]
        {
            new byte[] { 0xFE, 0xFF, 0x00 },
            new byte[] { 0xFE, 0xFF, 0xD8, 0x00 },
            new byte[] { 0xFE, 0xFF, 0xDC, 0x00 },
            new byte[] { 0xFE, 0xFF, 0xD8, 0x00, 0x00, 0x41 },
            new byte[] { 0xFE, 0xFF, 0xDC, 0x00, 0x00, 0x41 },
            new byte[] { 0xFE, 0xFF, 0xD8, 0x00, 0xD8, 0x01 },
            new byte[] { 0xFE, 0xFF, 0xD8, 0x3D, 0xDE, 0x00, 0x00 },
        };
        string[] expected = new string[]
        {
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFDA",
            "\uFFFDA",
            "\uFFFD\uFFFD",
            "\U0001F600\uFFFD",
        };

        int index = 0;
        while (index < inputs.Length)
        {
            Assert.That(
                encoding.DecodeTextString(inputs[index]),
                Is.EqualTo(expected[index]),
                "case " + index.ToString());
            index = index + 1;
        }
    }

    [Test]
    public void Utf16BeEncoding_ReplacesEveryMalformedUtf16Shape()
    {
        UnicodePdfEncoding encoding = new UnicodePdfEncoding();
        string[] inputs = new string[]
        {
            "\uD800",
            "\uDC00",
            "\uD800\uD801",
            "\uDC00\uDC01",
            "\uDC00\uD800",
            "A\uD800B\uDC00C",
        };
        string[] expected = new string[]
        {
            "\uFFFD",
            "\uFFFD",
            "\uFFFD\uFFFD",
            "\uFFFD\uFFFD",
            "\uFFFD\uFFFD",
            "A\uFFFDB\uFFFDC",
        };

        int index = 0;
        while (index < inputs.Length)
        {
            byte[] bytes = encoding.EncodeTextString(inputs[index]);

            Assert.That(encoding.GetTextStringEncoding(inputs[index]), Is.EqualTo(UnicodePdfTextEncoding.Utf16BeWithBom));
            Assert.That(bytes[0], Is.EqualTo(0xFE));
            Assert.That(bytes[1], Is.EqualTo(0xFF));
            Assert.That(encoding.DecodeTextString(bytes), Is.EqualTo(expected[index]), "case " + index.ToString());
            index = index + 1;
        }
    }

    [Test]
    public void Utf16BeTextStrings_RoundTripSupplementaryBoundaryScalars()
    {
        UnicodePdfEncoding encoding = new UnicodePdfEncoding();
        string text = "\U00010000\U0001F600\U0010FFFF";
        byte[] expected = new byte[]
        {
            0xFE,
            0xFF,
            0xD8,
            0x00,
            0xDC,
            0x00,
            0xD8,
            0x3D,
            0xDE,
            0x00,
            0xDB,
            0xFF,
            0xDF,
            0xFF,
        };

        Assert.That(encoding.GetTextStringEncoding(text), Is.EqualTo(UnicodePdfTextEncoding.Utf16BeWithBom));
        Assert.That(encoding.EncodeTextString(text), Is.EqualTo(expected));
        Assert.That(encoding.DecodeTextString(expected), Is.EqualTo(text));
    }

    [Test]
    public void LiteralStringEscaping_CoversEveryByteValue()
    {
        UnicodePdfText pdfText = new UnicodePdfText();
        byte[] bytes = CreateAllBytes();
        System.Text.StringBuilder expectedBuilder = new System.Text.StringBuilder();

        int byteValue = 0;
        while (byteValue <= 0xFF)
        {
            expectedBuilder.Append(GetExpectedLiteralEscape((byte)byteValue));
            byteValue = byteValue + 1;
        }

        string expected = expectedBuilder.ToString();
        byte[] destination = new byte[expected.Length];
        UnicodeBufferResult result = pdfText.EscapeLiteralStringBuffer(bytes, destination);

        Assert.That(pdfText.EscapeLiteralString(bytes), Is.EqualTo(expected));
        Assert.That(result.Success, Is.True);
        Assert.That(result.RequiredLength, Is.EqualTo(expected.Length));
        Assert.That(result.WrittenLength, Is.EqualTo(expected.Length));
        Assert.That(System.Text.Encoding.ASCII.GetString(destination), Is.EqualTo(expected));
    }

    [Test]
    public void HexadecimalStringFormatting_CoversEveryByteValue()
    {
        UnicodePdfText pdfText = new UnicodePdfText();
        byte[] bytes = CreateAllBytes();
        string expected = Convert.ToHexString(bytes);
        byte[] destination = new byte[expected.Length];

        UnicodeBufferResult result = pdfText.ToHexStringBuffer(bytes, destination);

        Assert.That(pdfText.ToHexString(bytes), Is.EqualTo(expected));
        Assert.That(result.Success, Is.True);
        Assert.That(result.RequiredLength, Is.EqualTo(512));
        Assert.That(result.WrittenLength, Is.EqualTo(512));
        Assert.That(System.Text.Encoding.ASCII.GetString(destination), Is.EqualTo(expected));
    }

    static int[] CreateExpectedPdfDocEncoding()
    {
        int[] expected = new int[256];
        int index = 0;
        while (index < expected.Length)
        {
            expected[index] = index;
            index = index + 1;
        }

        int[] lowSpecials = new int[]
        {
            0x02D8,
            0x02C7,
            0x02C6,
            0x02D9,
            0x02DD,
            0x02DB,
            0x02DA,
            0x02DC,
        };
        index = 0;
        while (index < lowSpecials.Length)
        {
            expected[0x18 + index] = lowSpecials[index];
            index = index + 1;
        }

        int[] highSpecials = new int[]
        {
            0x2022,
            0x2020,
            0x2021,
            0x2026,
            0x2014,
            0x2013,
            0x0192,
            0x2044,
            0x2039,
            0x203A,
            0x2212,
            0x2030,
            0x201E,
            0x201C,
            0x201D,
            0x2018,
            0x2019,
            0x201A,
            0x2122,
            0xFB01,
            0xFB02,
            0x0141,
            0x0152,
            0x0160,
            0x0178,
            0x017D,
            0x0131,
            0x0142,
            0x0153,
            0x0161,
            0x017E,
        };
        index = 0;
        while (index < highSpecials.Length)
        {
            expected[0x80 + index] = highSpecials[index];
            index = index + 1;
        }

        expected[0x7F] = UnicodeScalar.ReplacementCharacter;
        expected[0x9F] = UnicodeScalar.ReplacementCharacter;
        expected[0xA0] = 0x20AC;
        expected[0xAD] = UnicodeScalar.ReplacementCharacter;
        return expected;
    }

    static byte[] CreateAllBytes()
    {
        byte[] bytes = new byte[256];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)index;
            index = index + 1;
        }

        return bytes;
    }

    static string GetExpectedLiteralEscape(byte value)
    {
        if (value == 0x08)
        {
            return "\\b";
        }

        if (value == 0x09)
        {
            return "\\t";
        }

        if (value == 0x0A)
        {
            return "\\n";
        }

        if (value == 0x0C)
        {
            return "\\f";
        }

        if (value == 0x0D)
        {
            return "\\r";
        }

        if (value == 0x28)
        {
            return "\\(";
        }

        if (value == 0x29)
        {
            return "\\)";
        }

        if (value == 0x5C)
        {
            return "\\\\";
        }

        if (value >= 0x20 && value <= 0x7E)
        {
            return ((char)value).ToString();
        }

        return "\\" + Convert.ToString(value, 8).PadLeft(3, '0');
    }
}
