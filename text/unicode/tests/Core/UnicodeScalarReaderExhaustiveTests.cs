using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeScalarReaderExhaustiveTests
{
    [Test]
    public void ReadAllSpan_VisitsEveryUnicodeScalarWithExactUtf16Metadata()
    {
        UnicodeScalarReader reader = new UnicodeScalarReader();
        int nextCodePoint = 0;

        while (nextCodePoint <= UnicodeScalarData.MaxCodePoint)
        {
            int[] expected = new int[4096];
            char[] utf16 = new char[8192];
            int expectedLength = 0;
            int utf16Length = 0;

            while (expectedLength < expected.Length && nextCodePoint <= UnicodeScalarData.MaxCodePoint)
            {
                int codePoint = nextCodePoint;
                nextCodePoint = nextCodePoint + 1;
                if (codePoint >= UnicodeScalarData.HighSurrogateStart && codePoint <= UnicodeScalarData.LowSurrogateEnd)
                {
                    continue;
                }

                expected[expectedLength] = codePoint;
                expectedLength = expectedLength + 1;
                if (codePoint <= 0xFFFF)
                {
                    utf16[utf16Length] = (char)codePoint;
                    utf16Length = utf16Length + 1;
                }
                else
                {
                    int supplementary = codePoint - 0x10000;
                    utf16[utf16Length] = (char)(UnicodeScalarData.HighSurrogateStart + (supplementary / 0x400));
                    utf16[utf16Length + 1] = (char)(UnicodeScalarData.LowSurrogateStart + (supplementary % 0x400));
                    utf16Length = utf16Length + 2;
                }
            }

            UnicodeScalarRecord[] records = reader.ReadAllSpan(
                utf16.AsSpan(0, utf16Length),
                UnicodeMalformedInputPolicy.Strict);

            if (records.Length != expectedLength)
            {
                Assert.Fail("Scalar count mismatch near U+" + expected[0].ToString("X"));
            }

            int utf16Index = 0;
            for (int index = 0; index < expectedLength; index++)
            {
                int codePoint = expected[index];
                int scalarLength = codePoint <= 0xFFFF ? 1 : 2;
                UnicodeScalarRecord record = records[index];
                if (record.CodePoint != codePoint
                    || record.Utf16Index != utf16Index
                    || record.Utf16Length != scalarLength
                    || record.IsReplacement)
                {
                    Assert.Fail("Incorrect scalar record for U+" + codePoint.ToString("X"));
                }

                utf16Index = utf16Index + scalarLength;
            }
        }
    }

    [Test]
    public void MalformedPolicies_CoverEveryIndividualSurrogateCodeUnit()
    {
        UnicodeScalarReader reader = new UnicodeScalarReader();

        for (int codeUnit = UnicodeScalarData.HighSurrogateStart; codeUnit <= UnicodeScalarData.LowSurrogateEnd; codeUnit++)
        {
            string text = new string((char)codeUnit, 1);
            UnicodeScalarRecord[] records = reader.ReadAllWithPolicy(
                text,
                UnicodeMalformedInputPolicy.Replace);

            Assert.That(records, Has.Length.EqualTo(1), "U+" + codeUnit.ToString("X4"));
            AssertRecord(records[0], UnicodeScalarData.ReplacementCharacter, 0, 1, true);
            Assert.Throws<System.Text.EncoderFallbackException>(() => reader.ReadAllWithPolicy(
                text,
                UnicodeMalformedInputPolicy.Strict), "U+" + codeUnit.ToString("X4"));
        }
    }

    [Test]
    public void MalformedPolicies_CoverEveryAdjacentSurrogateShape()
    {
        AssertReplacementShape("\uD800", new int[] { 0xFFFD });
        AssertReplacementShape("\uDC00", new int[] { 0xFFFD });
        AssertReplacementShape("\uD800\uD801", new int[] { 0xFFFD, 0xFFFD });
        AssertReplacementShape("\uDC00\uDC01", new int[] { 0xFFFD, 0xFFFD });
        AssertReplacementShape("\uDC00\uD800", new int[] { 0xFFFD, 0xFFFD });
        AssertReplacementShape("\uD800A", new int[] { 0xFFFD, 0x0041 });
        AssertReplacementShape("A\uDC00", new int[] { 0x0041, 0xFFFD });
        AssertReplacementShape("\uD800\uD801\uDC00", new int[] { 0xFFFD, 0x10400 });
        AssertReplacementShape("\uDC00\uD800\uDC00", new int[] { 0xFFFD, 0x10000 });
        AssertReplacementShape("\uD800A\uDC00", new int[] { 0xFFFD, 0x0041, 0xFFFD });
    }

    [Test]
    public void BoundaryScalars_HaveExpectedUtf16LengthsAndOffsets()
    {
        int[] codePoints = new int[]
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
        string text = BuildString(codePoints);

        UnicodeScalarRecord[] records = new UnicodeScalarReader().ReadAllWithPolicy(
            text,
            UnicodeMalformedInputPolicy.Strict);

        Assert.That(records, Has.Length.EqualTo(codePoints.Length));
        int utf16Index = 0;
        for (int index = 0; index < codePoints.Length; index++)
        {
            int utf16Length = codePoints[index] <= 0xFFFF ? 1 : 2;
            AssertRecord(records[index], codePoints[index], utf16Index, utf16Length, false);
            utf16Index = utf16Index + utf16Length;
        }
    }

    [Test]
    public void ReadIntoBuffer_RejectsEveryUndersizedDestinationWithoutWriting()
    {
        UnicodeScalarReader reader = new UnicodeScalarReader();
        string text = BuildString(new int[] { 0x0000, 0xD7FF, 0x10000, 0xE000, 0x10FFFF });

        for (int length = 0; length < 5; length++)
        {
            UnicodeScalarRecord[] destination = new UnicodeScalarRecord[length];
            UnicodeBufferResult result = reader.ReadIntoBuffer(
                text,
                destination,
                UnicodeMalformedInputPolicy.Strict);

            Assert.That(result.Success, Is.False, "destination length " + length.ToString());
            Assert.That(result.RequiredLength, Is.EqualTo(5));
            Assert.That(result.WrittenLength, Is.EqualTo(0));
            Assert.That(destination, Is.All.Null);
        }

        UnicodeScalarRecord[] exact = new UnicodeScalarRecord[5];
        UnicodeBufferResult complete = reader.ReadIntoBuffer(
            text,
            exact,
            UnicodeMalformedInputPolicy.Strict);

        Assert.That(complete.Success, Is.True);
        Assert.That(complete.RequiredLength, Is.EqualTo(5));
        Assert.That(complete.WrittenLength, Is.EqualTo(5));
        AssertRecord(exact[4], 0x10FFFF, 5, 2, false);
    }

    static void AssertReplacementShape(string text, int[] expectedCodePoints)
    {
        UnicodeScalarReader reader = new UnicodeScalarReader();
        UnicodeScalarRecord[] records = reader.ReadAllWithPolicy(
            text,
            UnicodeMalformedInputPolicy.Replace);

        Assert.That(records, Has.Length.EqualTo(expectedCodePoints.Length), Escape(text));
        int utf16Index = 0;
        for (int index = 0; index < records.Length; index++)
        {
            int codePoint = expectedCodePoints[index];
            int utf16Length = codePoint > 0xFFFF ? 2 : 1;
            bool replacement = codePoint == UnicodeScalarData.ReplacementCharacter;
            AssertRecord(records[index], codePoint, utf16Index, utf16Length, replacement);
            utf16Index = utf16Index + utf16Length;
        }

        Assert.Throws<System.Text.EncoderFallbackException>(() => reader.ReadAllWithPolicy(
            text,
            UnicodeMalformedInputPolicy.Strict), Escape(text));
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

    static string Escape(string text)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int index = 0; index < text.Length; index++)
        {
            builder.Append("U+");
            builder.Append(((int)text[index]).ToString("X4"));
            builder.Append(' ');
        }

        return builder.ToString();
    }

    static void AssertRecord(
        UnicodeScalarRecord record,
        int codePoint,
        int utf16Index,
        int utf16Length,
        bool isReplacement)
    {
        Assert.That(record.CodePoint, Is.EqualTo(codePoint));
        Assert.That(record.Utf16Index, Is.EqualTo(utf16Index));
        Assert.That(record.Utf16Length, Is.EqualTo(utf16Length));
        Assert.That(record.IsReplacement, Is.EqualTo(isReplacement));
    }
}
