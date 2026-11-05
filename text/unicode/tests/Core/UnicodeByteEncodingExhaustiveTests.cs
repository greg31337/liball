using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeByteEncodingExhaustiveTests
{
    static readonly UnicodeByteEncodingKind[] AllEncodings = new UnicodeByteEncodingKind[]
    {
        UnicodeByteEncodingKind.Utf8,
        UnicodeByteEncodingKind.Utf16Le,
        UnicodeByteEncodingKind.Utf16Be,
        UnicodeByteEncodingKind.Utf32Le,
        UnicodeByteEncodingKind.Utf32Be
    };

    [Test]
    public void EveryUnicodeScalar_RoundTripsThroughEveryEncoding()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();
        string allScalars = BuildAllScalarString();

        foreach (UnicodeByteEncodingKind encoding in AllEncodings)
        {
            byte[] bytes = encoder.EncodeWithPolicy(
                allScalars,
                encoding,
                UnicodeMalformedInputPolicy.Strict);
            string decoded = encoder.DecodeWithPolicy(
                bytes,
                encoding,
                UnicodeMalformedInputPolicy.Strict);

            Assert.That(decoded, Is.EqualTo(allScalars), encoding.ToString());
        }
    }

    [Test]
    public void BoundaryScalars_EncodeToExactBytesInEveryEncodingAndByteOrder()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();
        int[] boundaries = new int[]
        {
            0x0000,
            0x007F,
            0x0080,
            0x07FF,
            0x0800,
            0xD7FF,
            0xE000,
            0xFFFF,
            0x10000,
            0x10FFFF
        };

        foreach (UnicodeByteEncodingKind encoding in AllEncodings)
        {
            for (int index = 0; index < boundaries.Length; index++)
            {
                int codePoint = boundaries[index];
                string text = char.ConvertFromUtf32(codePoint);
                byte[] expected = EncodeScalar(codePoint, encoding);

                Assert.That(
                    encoder.EncodeWithPolicy(text, encoding, UnicodeMalformedInputPolicy.Strict),
                    Is.EqualTo(expected),
                    encoding.ToString() + " U+" + codePoint.ToString("X"));
                Assert.That(
                    encoder.DecodeWithPolicy(expected, encoding, UnicodeMalformedInputPolicy.Strict),
                    Is.EqualTo(text),
                    encoding.ToString() + " U+" + codePoint.ToString("X"));
            }
        }
    }

    [Test]
    public void EverySurrogateCodeUnit_HasStrictAndReplacementBehaviorInEveryEncoding()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();

        foreach (UnicodeByteEncodingKind encoding in AllEncodings)
        {
            byte[] encodedReplacement = EncodeScalar(UnicodeScalarData.ReplacementCharacter, encoding);
            for (int codeUnit = UnicodeScalarData.HighSurrogateStart; codeUnit <= UnicodeScalarData.LowSurrogateEnd; codeUnit++)
            {
                string malformedText = new string((char)codeUnit, 1);
                string label = encoding.ToString() + " U+" + codeUnit.ToString("X4");

                Assert.Throws<System.Text.EncoderFallbackException>(() => encoder.EncodeWithPolicy(
                    malformedText,
                    encoding,
                    UnicodeMalformedInputPolicy.Strict), label);
                Assert.That(
                    encoder.EncodeWithPolicy(
                        malformedText,
                        encoding,
                        UnicodeMalformedInputPolicy.Replace),
                    Is.EqualTo(encodedReplacement),
                    label);

                byte[] encodedSurrogate = EncodeScalar(codeUnit, encoding);
                string decodedReplacement = encoding == UnicodeByteEncodingKind.Utf8
                    ? "\uFFFD\uFFFD\uFFFD"
                    : "\uFFFD";
                Assert.Throws<System.Text.DecoderFallbackException>(() => encoder.DecodeWithPolicy(
                    encodedSurrogate,
                    encoding,
                    UnicodeMalformedInputPolicy.Strict), label);
                Assert.That(
                    encoder.DecodeWithPolicy(
                        encodedSurrogate,
                        encoding,
                        UnicodeMalformedInputPolicy.Replace),
                    Is.EqualTo(decodedReplacement),
                    label);
            }
        }
    }

    [Test]
    public void AdjacentMalformedSurrogateShapes_EncodeConsistentlyInEveryEncoding()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();
        string[] malformed = new string[]
        {
            "\uD800",
            "\uDC00",
            "\uD800\uD801",
            "\uDC00\uDC01",
            "\uDC00\uD800",
            "\uD800A",
            "A\uDC00",
            "\uD800\uD801\uDC00",
            "\uDC00\uD800\uDC00",
            "\uD800A\uDC00"
        };
        string[] replacements = new string[]
        {
            "\uFFFD",
            "\uFFFD",
            "\uFFFD\uFFFD",
            "\uFFFD\uFFFD",
            "\uFFFD\uFFFD",
            "\uFFFDA",
            "A\uFFFD",
            "\uFFFD\U00010400",
            "\uFFFD\U00010000",
            "\uFFFDA\uFFFD"
        };

        foreach (UnicodeByteEncodingKind encoding in AllEncodings)
        {
            for (int index = 0; index < malformed.Length; index++)
            {
                string text = malformed[index];
                string label = encoding.ToString() + " case " + index.ToString();
                byte[] expected = encoder.EncodeWithPolicy(
                    replacements[index],
                    encoding,
                    UnicodeMalformedInputPolicy.Strict);

                Assert.Throws<System.Text.EncoderFallbackException>(() => encoder.EncodeWithPolicy(
                    text,
                    encoding,
                    UnicodeMalformedInputPolicy.Strict), label);
                Assert.That(
                    encoder.EncodeWithPolicy(text, encoding, UnicodeMalformedInputPolicy.Replace),
                    Is.EqualTo(expected),
                    label);
            }
        }
    }

    [Test]
    public void Utf8MalformedSequenceCategories_HaveStrictAndReplacementBehavior()
    {
        byte[][] malformed = new byte[][]
        {
            new byte[] { 0x80 },
            new byte[] { 0xC0 },
            new byte[] { 0xC1 },
            new byte[] { 0xF5 },
            new byte[] { 0xFF },
            new byte[] { 0xC2 },
            new byte[] { 0xE0, 0xA0 },
            new byte[] { 0xF0, 0x90, 0x80 },
            new byte[] { 0xC2, 0x20 },
            new byte[] { 0xE1, 0x20, 0x80 },
            new byte[] { 0xF1, 0x80, 0x20, 0x80 },
            new byte[] { 0xC0, 0xAF },
            new byte[] { 0xE0, 0x80, 0x80 },
            new byte[] { 0xF0, 0x80, 0x80, 0x80 },
            new byte[] { 0xED, 0xA0, 0x80 },
            new byte[] { 0xF4, 0x90, 0x80, 0x80 }
        };
        string[] replacements = new string[]
        {
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFD ",
            "\uFFFD \uFFFD",
            "\uFFFD \uFFFD",
            "\uFFFD\uFFFD",
            "\uFFFD\uFFFD\uFFFD",
            "\uFFFD\uFFFD\uFFFD\uFFFD",
            "\uFFFD\uFFFD\uFFFD",
            "\uFFFD\uFFFD\uFFFD\uFFFD"
        };

        AssertMalformedCases(UnicodeByteEncodingKind.Utf8, malformed, replacements);
    }

    [Test]
    public void Utf16MalformedSequenceCategories_HaveStrictAndReplacementBehaviorInBothByteOrders()
    {
        byte[][] littleEndian = new byte[][]
        {
            new byte[] { 0x41 },
            new byte[] { 0x00, 0xD8 },
            new byte[] { 0x00, 0xD8, 0x41, 0x00 },
            new byte[] { 0x00, 0xDC },
            new byte[] { 0x00, 0xDC, 0x00, 0xD8 },
            new byte[] { 0x41, 0x00, 0x42 }
        };
        byte[][] bigEndian = new byte[][]
        {
            new byte[] { 0x41 },
            new byte[] { 0xD8, 0x00 },
            new byte[] { 0xD8, 0x00, 0x00, 0x41 },
            new byte[] { 0xDC, 0x00 },
            new byte[] { 0xDC, 0x00, 0xD8, 0x00 },
            new byte[] { 0x00, 0x41, 0x42 }
        };
        string[] replacements = new string[]
        {
            "\uFFFD",
            "\uFFFD",
            "\uFFFDA",
            "\uFFFD",
            "\uFFFD\uFFFD",
            "A\uFFFD"
        };

        AssertMalformedCases(UnicodeByteEncodingKind.Utf16Le, littleEndian, replacements);
        AssertMalformedCases(UnicodeByteEncodingKind.Utf16Be, bigEndian, replacements);
    }

    [Test]
    public void Utf32MalformedSequenceCategories_HaveStrictAndReplacementBehaviorInBothByteOrders()
    {
        byte[][] littleEndian = new byte[][]
        {
            new byte[] { 0x00 },
            new byte[] { 0x00, 0x00 },
            new byte[] { 0x00, 0x00, 0x00 },
            new byte[] { 0x00, 0xD8, 0x00, 0x00 },
            new byte[] { 0xFF, 0xDF, 0x00, 0x00 },
            new byte[] { 0x00, 0x00, 0x11, 0x00 },
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            new byte[] { 0x41, 0x00, 0x00, 0x00, 0x42 }
        };
        byte[][] bigEndian = new byte[][]
        {
            new byte[] { 0x00 },
            new byte[] { 0x00, 0x00 },
            new byte[] { 0x00, 0x00, 0x00 },
            new byte[] { 0x00, 0x00, 0xD8, 0x00 },
            new byte[] { 0x00, 0x00, 0xDF, 0xFF },
            new byte[] { 0x00, 0x11, 0x00, 0x00 },
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            new byte[] { 0x00, 0x00, 0x00, 0x41, 0x42 }
        };
        string[] replacements = new string[]
        {
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "\uFFFD",
            "A\uFFFD"
        };

        AssertMalformedCases(UnicodeByteEncodingKind.Utf32Le, littleEndian, replacements);
        AssertMalformedCases(UnicodeByteEncodingKind.Utf32Be, bigEndian, replacements);
    }

    [Test]
    public void BomBytes_AreNeverAddedAndAreAlwaysPreservedAsCharacters()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();

        foreach (UnicodeByteEncodingKind encoding in AllEncodings)
        {
            byte[] bom = EncodeScalar(0xFEFF, encoding);
            byte[] bomAndA = Combine(bom, EncodeScalar(0x0041, encoding));

            Assert.That(
                encoder.EncodeWithPolicy("", encoding, UnicodeMalformedInputPolicy.Strict),
                Is.Empty,
                encoding.ToString());
            Assert.That(
                encoder.EncodeWithPolicy("\uFEFFA", encoding, UnicodeMalformedInputPolicy.Strict),
                Is.EqualTo(bomAndA),
                encoding.ToString());
            Assert.That(
                encoder.DecodeWithPolicy(bomAndA, encoding, UnicodeMalformedInputPolicy.Strict),
                Is.EqualTo("\uFEFFA"),
                encoding.ToString());
            Assert.That(
                encoder.DecodeWithPolicy(EncodeScalar(0xFFFE, encoding), encoding, UnicodeMalformedInputPolicy.Strict),
                Is.EqualTo("\uFFFE"),
                encoding.ToString());
            Assert.That(
                encoder.DecodeWithPolicy(
                    Combine(EncodeScalar(0x0041, encoding), bom, EncodeScalar(0x0042, encoding)),
                    encoding,
                    UnicodeMalformedInputPolicy.Strict),
                Is.EqualTo("A\uFEFFB"),
                encoding.ToString());
        }
    }

    [Test]
    public void BufferMethods_RejectEveryUndersizedDestinationWithoutPartialWrites()
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();
        string text = BuildString(new int[] { 0x0000, 0x07FF, 0x0800, 0x10000, 0x10FFFF });

        foreach (UnicodeByteEncodingKind encoding in AllEncodings)
        {
            byte[] expectedBytes = encoder.EncodeWithPolicy(
                text,
                encoding,
                UnicodeMalformedInputPolicy.Strict);
            for (int length = 0; length < expectedBytes.Length; length++)
            {
                byte[] destination = new byte[length];
                Array.Fill(destination, (byte)0xCC);
                UnicodeBufferResult result = encoder.EncodeBuffer(
                    text,
                    destination,
                    encoding,
                    UnicodeMalformedInputPolicy.Strict);

                Assert.That(result.Success, Is.False, encoding.ToString() + " encode length " + length.ToString());
                Assert.That(result.RequiredLength, Is.EqualTo(expectedBytes.Length));
                Assert.That(result.WrittenLength, Is.EqualTo(0));
                Assert.That(destination, Is.All.EqualTo((byte)0xCC));
            }

            byte[] exactBytes = new byte[expectedBytes.Length];
            UnicodeBufferResult completeEncode = encoder.EncodeBuffer(
                text,
                exactBytes,
                encoding,
                UnicodeMalformedInputPolicy.Strict);
            Assert.That(completeEncode.Success, Is.True, encoding.ToString());
            Assert.That(exactBytes, Is.EqualTo(expectedBytes), encoding.ToString());

            for (int length = 0; length < text.Length; length++)
            {
                char[] destination = new char[length];
                Array.Fill(destination, 'X');
                UnicodeBufferResult result = encoder.DecodeBuffer(
                    expectedBytes,
                    destination,
                    encoding,
                    UnicodeMalformedInputPolicy.Strict);

                Assert.That(result.Success, Is.False, encoding.ToString() + " decode length " + length.ToString());
                Assert.That(result.RequiredLength, Is.EqualTo(text.Length));
                Assert.That(result.WrittenLength, Is.EqualTo(0));
                Assert.That(destination, Is.All.EqualTo('X'));
            }

            char[] exactCharacters = new char[text.Length];
            UnicodeBufferResult completeDecode = encoder.DecodeBuffer(
                expectedBytes,
                exactCharacters,
                encoding,
                UnicodeMalformedInputPolicy.Strict);
            Assert.That(completeDecode.Success, Is.True, encoding.ToString());
            Assert.That(new string(exactCharacters), Is.EqualTo(text), encoding.ToString());

            UnicodeBufferResult emptyEncode = encoder.EncodeBuffer(
                System.ReadOnlySpan<char>.Empty,
                System.Span<byte>.Empty,
                encoding,
                UnicodeMalformedInputPolicy.Strict);
            UnicodeBufferResult emptyDecode = encoder.DecodeBuffer(
                System.ReadOnlySpan<byte>.Empty,
                System.Span<char>.Empty,
                encoding,
                UnicodeMalformedInputPolicy.Strict);
            Assert.That(emptyEncode.Success, Is.True, encoding.ToString());
            Assert.That(emptyEncode.RequiredLength, Is.Zero);
            Assert.That(emptyEncode.WrittenLength, Is.Zero);
            Assert.That(emptyDecode.Success, Is.True, encoding.ToString());
            Assert.That(emptyDecode.RequiredLength, Is.Zero);
            Assert.That(emptyDecode.WrittenLength, Is.Zero);
        }
    }

    static void AssertMalformedCases(
        UnicodeByteEncodingKind encoding,
        byte[][] malformed,
        string[] replacements)
    {
        UnicodeByteEncoding encoder = new UnicodeByteEncoding();
        Assert.That(malformed, Has.Length.EqualTo(replacements.Length));

        for (int index = 0; index < malformed.Length; index++)
        {
            byte[] bytes = malformed[index];
            string label = encoding.ToString() + " case " + index.ToString();

            Assert.Throws<System.Text.DecoderFallbackException>(() => encoder.DecodeWithPolicy(
                bytes,
                encoding,
                UnicodeMalformedInputPolicy.Strict), label);
            Assert.That(
                encoder.DecodeWithPolicy(bytes, encoding, UnicodeMalformedInputPolicy.Replace),
                Is.EqualTo(replacements[index]),
                label);
        }
    }

    static byte[] EncodeScalar(int codePoint, UnicodeByteEncodingKind encoding)
    {
        if (encoding == UnicodeByteEncodingKind.Utf8)
        {
            if (codePoint <= 0x7F)
            {
                return new byte[] { (byte)codePoint };
            }

            if (codePoint <= 0x7FF)
            {
                return new byte[]
                {
                    (byte)(0xC0 | (codePoint >> 6)),
                    (byte)(0x80 | (codePoint & 0x3F))
                };
            }

            if (codePoint <= 0xFFFF)
            {
                return new byte[]
                {
                    (byte)(0xE0 | (codePoint >> 12)),
                    (byte)(0x80 | ((codePoint >> 6) & 0x3F)),
                    (byte)(0x80 | (codePoint & 0x3F))
                };
            }

            return new byte[]
            {
                (byte)(0xF0 | (codePoint >> 18)),
                (byte)(0x80 | ((codePoint >> 12) & 0x3F)),
                (byte)(0x80 | ((codePoint >> 6) & 0x3F)),
                (byte)(0x80 | (codePoint & 0x3F))
            };
        }

        if (encoding == UnicodeByteEncodingKind.Utf16Le || encoding == UnicodeByteEncodingKind.Utf16Be)
        {
            int first = codePoint;
            int second = -1;
            if (codePoint > 0xFFFF)
            {
                int supplementary = codePoint - 0x10000;
                first = UnicodeScalarData.HighSurrogateStart + (supplementary / 0x400);
                second = UnicodeScalarData.LowSurrogateStart + (supplementary % 0x400);
            }

            byte[] bytes = new byte[second < 0 ? 2 : 4];
            WriteUInt16(bytes, 0, first, encoding == UnicodeByteEncodingKind.Utf16Be);
            if (second >= 0)
            {
                WriteUInt16(bytes, 2, second, encoding == UnicodeByteEncodingKind.Utf16Be);
            }

            return bytes;
        }

        bool bigEndian = encoding == UnicodeByteEncodingKind.Utf32Be;
        if (bigEndian)
        {
            return new byte[]
            {
                (byte)((codePoint >> 24) & 0xFF),
                (byte)((codePoint >> 16) & 0xFF),
                (byte)((codePoint >> 8) & 0xFF),
                (byte)(codePoint & 0xFF)
            };
        }

        return new byte[]
        {
            (byte)(codePoint & 0xFF),
            (byte)((codePoint >> 8) & 0xFF),
            (byte)((codePoint >> 16) & 0xFF),
            (byte)((codePoint >> 24) & 0xFF)
        };
    }

    static void WriteUInt16(byte[] bytes, int index, int value, bool bigEndian)
    {
        if (bigEndian)
        {
            bytes[index] = (byte)((value >> 8) & 0xFF);
            bytes[index + 1] = (byte)(value & 0xFF);
        }
        else
        {
            bytes[index] = (byte)(value & 0xFF);
            bytes[index + 1] = (byte)((value >> 8) & 0xFF);
        }
    }

    static byte[] Combine(params byte[][] parts)
    {
        int length = 0;
        for (int index = 0; index < parts.Length; index++)
        {
            length = length + parts[index].Length;
        }

        byte[] result = new byte[length];
        int destinationIndex = 0;
        for (int index = 0; index < parts.Length; index++)
        {
            Array.Copy(parts[index], 0, result, destinationIndex, parts[index].Length);
            destinationIndex = destinationIndex + parts[index].Length;
        }

        return result;
    }

    static string BuildString(int[] codePoints)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int index = 0; index < codePoints.Length; index++)
        {
            builder.Append(char.ConvertFromUtf32(codePoints[index]));
        }

        return builder.ToString();
    }

    static string BuildAllScalarString()
    {
        int scalarCount = 0x110000 - 0x800;
        int supplementaryCount = 0x100000;
        char[] utf16 = new char[scalarCount + supplementaryCount];
        int utf16Index = 0;

        for (int codePoint = 0; codePoint <= UnicodeScalarData.MaxCodePoint; codePoint++)
        {
            if (codePoint >= UnicodeScalarData.HighSurrogateStart && codePoint <= UnicodeScalarData.LowSurrogateEnd)
            {
                continue;
            }

            if (codePoint <= 0xFFFF)
            {
                utf16[utf16Index] = (char)codePoint;
                utf16Index = utf16Index + 1;
            }
            else
            {
                int supplementary = codePoint - 0x10000;
                utf16[utf16Index] = (char)(UnicodeScalarData.HighSurrogateStart + (supplementary / 0x400));
                utf16[utf16Index + 1] = (char)(UnicodeScalarData.LowSurrogateStart + (supplementary % 0x400));
                utf16Index = utf16Index + 2;
            }
        }

        Assert.That(utf16Index, Is.EqualTo(utf16.Length));
        return new string(utf16);
    }
}
