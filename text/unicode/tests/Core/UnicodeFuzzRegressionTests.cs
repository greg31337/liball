using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeFuzzRegressionTests
{
    [Test]
    public void RandomScalarStrings_RoundTripThroughByteEncodings()
    {
        Random random = new Random(0x170089);
        int iteration = 0;
        while (iteration < 64)
        {
            string text = GenerateScalarString(random, random.Next(0, 25));

            Assert.That(new UnicodeScalarReader().ReplaceInvalid(text), Is.EqualTo(text), "ReplaceInvalid " + iteration.ToString());
            Assert.That(new UnicodeByteEncoding().DecodeUtf8(new UnicodeByteEncoding().EncodeUtf8(text)), Is.EqualTo(text), "UTF-8 " + iteration.ToString());
            Assert.That(new UnicodeByteEncoding().DecodeUtf16Le(new UnicodeByteEncoding().EncodeUtf16Le(text)), Is.EqualTo(text), "UTF-16LE " + iteration.ToString());
            Assert.That(new UnicodeByteEncoding().DecodeUtf16Be(new UnicodeByteEncoding().EncodeUtf16Be(text)), Is.EqualTo(text), "UTF-16BE " + iteration.ToString());
            Assert.That(new UnicodeByteEncoding().DecodeUtf32Le(new UnicodeByteEncoding().EncodeUtf32Le(text)), Is.EqualTo(text), "UTF-32LE " + iteration.ToString());
            Assert.That(new UnicodeByteEncoding().DecodeUtf32Be(new UnicodeByteEncoding().EncodeUtf32Be(text)), Is.EqualTo(text), "UTF-32BE " + iteration.ToString());
            iteration = iteration + 1;
        }
    }

    [Test]
    public void InvalidUtf16Fuzz_ReplacesInvalidCodeUnitsConsistently()
    {
        Random random = new Random(0xBAD17);
        int iteration = 0;
        while (iteration < 48)
        {
            string text = GeneratePossiblyInvalidUtf16String(random, random.Next(1, 30));
            string safeText = new UnicodeScalarReader().ReplaceInvalid(text);
            UnicodeValidationResult validation = new UnicodeScalarReader().Validate(text);

            Assert.That(new UnicodePdfText().ContainsOnlyValidActualTextScalars(safeText), Is.True, "safe scalars " + iteration.ToString());
            Assert.That(CountReplacementRecords(validation.Scalars), Is.EqualTo(validation.ReplacementCount), "replacement count " + iteration.ToString());
            Assert.That(new UnicodeByteEncoding().DecodeUtf8(new UnicodeByteEncoding().EncodeUtf8(text)), Is.EqualTo(safeText), "UTF-8 replacement " + iteration.ToString());
            Assert.That(new UnicodeByteEncoding().DecodeUtf16Be(new UnicodeByteEncoding().EncodeUtf16Be(text)), Is.EqualTo(safeText), "UTF-16BE replacement " + iteration.ToString());
            Assert.That(new UnicodePdfEncoding().DecodeTextString(new UnicodePdfEncoding().EncodeTextString(text)), Is.EqualTo(safeText), "PDF replacement " + iteration.ToString());
            iteration = iteration + 1;
        }
    }

    [Test]
    public void NormalizationFuzz_IsIdempotentForAllForms()
    {
        Random random = new Random(0x4EFC);
        UnicodeNormalizationForm[] forms = new UnicodeNormalizationForm[]
        {
            UnicodeNormalizationForm.Nfc,
            UnicodeNormalizationForm.Nfd,
            UnicodeNormalizationForm.Nfkc,
            UnicodeNormalizationForm.Nfkd,
        };

        int iteration = 0;
        while (iteration < 64)
        {
            string text = GenerateNormalizationStressString(random, random.Next(0, 18));
            int formIndex = 0;
            while (formIndex < forms.Length)
            {
                string normalized = new UnicodeNormalization().Normalize(text, forms[formIndex]);

                Assert.That(new UnicodeNormalization().Normalize(normalized, forms[formIndex]), Is.EqualTo(normalized), "idempotent " + iteration.ToString() + " " + forms[formIndex].ToString());
                Assert.That(new UnicodeNormalization().IsNormalized(normalized, forms[formIndex]), Is.True, "is normalized " + iteration.ToString() + " " + forms[formIndex].ToString());
                formIndex = formIndex + 1;
            }

            iteration = iteration + 1;
        }
    }

    [Test]
    public void SegmentationFuzz_OffsetsAreMonotonicAndCoverText()
    {
        Random random = new Random(0x5E617);
        int iteration = 0;
        while (iteration < 64)
        {
            string text = GenerateSegmentationStressString(random, random.Next(0, 30));

            AssertSegmentsCoverText(new UnicodeSegments().GetGraphemeSegments(text), text.Length, "grapheme " + iteration.ToString());
            AssertSegmentsCoverText(new UnicodeSegments().GetWordSegments(text), text.Length, "word " + iteration.ToString());
            AssertSegmentsCoverText(new UnicodeSegments().GetSentenceSegments(text), text.Length, "sentence " + iteration.ToString());
            AssertSegmentsCoverText(new UnicodeSegments().GetLineSegments(text), text.Length, "line " + iteration.ToString());
            iteration = iteration + 1;
        }
    }

    [Test]
    public void PdfFuzz_RoundTripsTextStringsActualTextAndEscapedBytes()
    {
        Random random = new Random(0x0A11);
        int iteration = 0;
        while (iteration < 64)
        {
            string text = GeneratePossiblyInvalidUtf16String(random, random.Next(0, 24));
            string safeText = new UnicodeScalarReader().ReplaceInvalid(text);
            string actualText = new UnicodePdfText().PrepareActualText(text);
            byte[] textBytes = new UnicodePdfEncoding().EncodeTextString(text);
            byte[] actualTextBytes = new UnicodePdfEncoding().EncodeTextString(actualText);

            Assert.That(new UnicodePdfEncoding().DecodeTextString(textBytes), Is.EqualTo(safeText), "PDF text " + iteration.ToString());
            Assert.That(new UnicodePdfText().IsActualTextPrepared(actualText), Is.True, "ActualText prepared " + iteration.ToString());
            Assert.That(new UnicodePdfEncoding().DecodeTextString(actualTextBytes), Is.EqualTo(actualText), "ActualText round trip " + iteration.ToString());
            Assert.That(new UnicodePdfText().ToHexString(textBytes).Length, Is.EqualTo(textBytes.Length * 2), "hex length " + iteration.ToString());
            Assert.That(new UnicodePdfText().EscapeLiteralString(textBytes), Is.Not.Null, "literal escape " + iteration.ToString());
            iteration = iteration + 1;
        }
    }

    static string GenerateScalarString(Random random, int scalarCount)
    {
        int[] pool = new int[]
        {
            0x0009,
            0x000A,
            0x0020,
            0x0021,
            0x0030,
            0x0041,
            0x005A,
            0x0061,
            0x007A,
            0x00C0,
            0x00B2,
            0x0300,
            0x0301,
            0x0327,
            0x0340,
            0x0370,
            0x0600,
            0x2126,
            0x3400,
            0xAC00,
            0x1F600,
        };

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < scalarCount)
        {
            builder.Append(new UnicodeScalar().ToUtf16String(pool[random.Next(0, pool.Length)]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string GenerateNormalizationStressString(Random random, int scalarCount)
    {
        int[] pool = new int[]
        {
            0x0041,
            0x0055,
            0x00C0,
            0x00B2,
            0x0300,
            0x0301,
            0x0315,
            0x0327,
            0x0340,
            0x0341,
            0x01DB,
            0x2126,
            0xAC00,
        };

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < scalarCount)
        {
            builder.Append(new UnicodeScalar().ToUtf16String(pool[random.Next(0, pool.Length)]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string GenerateSegmentationStressString(Random random, int scalarCount)
    {
        int[] pool = new int[]
        {
            0x000A,
            0x000D,
            0x0020,
            0x0027,
            0x002E,
            0x0030,
            0x0041,
            0x0061,
            0x0300,
            0x0301,
            0x0340,
            0x0370,
            0x0600,
            0x200D,
            0x3400,
            0x1F1E6,
            0x1F1E7,
            0x1F3FB,
            0x1F600,
        };

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < scalarCount)
        {
            builder.Append(new UnicodeScalar().ToUtf16String(pool[random.Next(0, pool.Length)]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string GeneratePossiblyInvalidUtf16String(Random random, int unitCount)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < unitCount)
        {
            int choice = random.Next(0, 10);
            if (choice == 0)
            {
                builder.Append('\uD800');
            }
            else
            {
                if (choice == 1)
                {
                    builder.Append('\uDC00');
                }
                else
                {
                    if (choice == 2)
                    {
                        builder.Append("\U0001F600");
                    }
                    else
                    {
                        builder.Append(GenerateScalarString(random, 1));
                    }
                }
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    static int CountReplacementRecords(UnicodeScalarRecord[] records)
    {
        int count = 0;
        int index = 0;
        while (index < records.Length)
        {
            if (records[index].IsReplacement)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static void AssertSegmentsCoverText(UnicodeSegmentRecord[] segments, int textLength, string context)
    {
        if (textLength == 0)
        {
            Assert.That(segments, Is.Empty, context);
            return;
        }

        Assert.That(segments, Is.Not.Empty, context);
        Assert.That(segments[0].StartUtf16Index, Is.EqualTo(0), context);

        int index = 0;
        int previousEnd = 0;
        while (index < segments.Length)
        {
            Assert.That(segments[index].StartUtf16Index, Is.EqualTo(previousEnd), context);
            Assert.That(segments[index].EndUtf16Index, Is.GreaterThanOrEqualTo(segments[index].StartUtf16Index), context);
            Assert.That(segments[index].EndUtf16Index, Is.LessThanOrEqualTo(textLength), context);
            previousEnd = segments[index].EndUtf16Index;
            index = index + 1;
        }

        Assert.That(previousEnd, Is.EqualTo(textLength), context);
    }
}
