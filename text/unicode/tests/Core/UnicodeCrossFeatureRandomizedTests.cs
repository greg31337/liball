using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeCrossFeatureRandomizedTests
{
    [Test]
    public void NormalizationAndCasing_RandomizedTransformsAreIdempotent()
    {
        Random random = new Random(0x30CA5E);
        UnicodeNormalization normalization = new UnicodeNormalization();
        UnicodeCase casing = new UnicodeCase();
        UnicodeNormalizationForm[] forms = new UnicodeNormalizationForm[]
        {
            UnicodeNormalizationForm.Nfc,
            UnicodeNormalizationForm.Nfd,
            UnicodeNormalizationForm.Nfkc,
            UnicodeNormalizationForm.Nfkd,
        };
        UnicodeCompareKeyMode[] keyModes = new UnicodeCompareKeyMode[]
        {
            UnicodeCompareKeyMode.OrdinalFold,
            UnicodeCompareKeyMode.NfcFold,
            UnicodeCompareKeyMode.NfkcFold,
        };

        int iteration = 0;
        while (iteration < 64)
        {
            string text = GenerateText(random, random.Next(0, 25), CaseAndNormalizationPool());
            int formIndex = 0;
            while (formIndex < forms.Length)
            {
                string normalized = normalization.Normalize(text, forms[formIndex]);
                Assert.That(
                    normalization.Normalize(normalized, forms[formIndex]),
                    Is.EqualTo(normalized),
                    "normalization iteration " + iteration.ToString() + " form " + forms[formIndex].ToString());
                formIndex = formIndex + 1;
            }

            Assert.That(casing.ToUpper(casing.ToUpper(text)), Is.EqualTo(casing.ToUpper(text)), "upper iteration " + iteration.ToString());
            Assert.That(casing.ToLower(casing.ToLower(text)), Is.EqualTo(casing.ToLower(text)), "lower iteration " + iteration.ToString());
            Assert.That(casing.Fold(casing.Fold(text)), Is.EqualTo(casing.Fold(text)), "fold iteration " + iteration.ToString());
            Assert.That(casing.FoldTurkic(casing.FoldTurkic(text)), Is.EqualTo(casing.FoldTurkic(text)), "Turkic fold iteration " + iteration.ToString());
            Assert.That(casing.ToNfkcCaseFold(casing.ToNfkcCaseFold(text)), Is.EqualTo(casing.ToNfkcCaseFold(text)), "NFKC fold iteration " + iteration.ToString());

            int modeIndex = 0;
            while (modeIndex < keyModes.Length)
            {
                string key = casing.CompareKey(text, keyModes[modeIndex]);
                Assert.That(
                    casing.CompareKey(key, keyModes[modeIndex]),
                    Is.EqualTo(key),
                    "key iteration " + iteration.ToString() + " mode " + keyModes[modeIndex].ToString());
                modeIndex = modeIndex + 1;
            }

            iteration = iteration + 1;
        }
    }

    [Test]
    public void Segmentation_RandomizedBreaksAreMonotonicAndMatchSegments()
    {
        Random random = new Random(0x30BEA7);
        UnicodeSegments segments = new UnicodeSegments();
        int iteration = 0;
        while (iteration < 64)
        {
            string text = GenerateText(random, random.Next(0, 35), SegmentationPool());
            int[] graphemeBreaks = segments.GetGraphemeBreaks(text);

            AssertBreaksMatchSegments(graphemeBreaks, segments.GetGraphemeSegments(text), text.Length, "grapheme " + iteration.ToString());
            AssertBreaksMatchSegments(segments.GetWordBreaks(text), segments.GetWordSegments(text), text.Length, "word " + iteration.ToString());
            AssertBreaksMatchSegments(segments.GetSentenceBreaks(text), segments.GetSentenceSegments(text), text.Length, "sentence " + iteration.ToString());
            AssertBreaksMatchSegments(segments.GetLineBreaks(text), segments.GetLineSegments(text), text.Length, "line " + iteration.ToString());
            Assert.That(segments.GetGraphemeClusterCount(text), Is.EqualTo(graphemeBreaks.Length - 1), "cluster count " + iteration.ToString());

            int clusterCount = graphemeBreaks.Length - 1;
            int keepCount = random.Next(0, clusterCount + 2);
            int keptEnd = keepCount >= clusterCount ? text.Length : graphemeBreaks[keepCount];
            Assert.That(
                segments.TruncateGraphemeClusters(text, keepCount),
                Is.EqualTo(text.Substring(0, keptEnd)),
                "truncate " + iteration.ToString());

            int start = random.Next(0, clusterCount + 1);
            int count = random.Next(0, clusterCount + 2);
            int selectedCount = System.Math.Min(count, clusterCount - start);
            int sliceStart = graphemeBreaks[start];
            int sliceEnd = graphemeBreaks[start + selectedCount];
            Assert.That(
                segments.SliceGraphemeClusters(text, start, count),
                Is.EqualTo(text.Substring(sliceStart, sliceEnd - sliceStart)),
                "slice " + iteration.ToString());

            UnicodeLineBreakRecord[] opportunities = segments.GetLineBreakOpportunities(text);
            Assert.That(opportunities, Is.Not.Empty, "line opportunities " + iteration.ToString());
            Assert.That(opportunities[opportunities.Length - 1].Utf16Index, Is.EqualTo(text.Length), "final line break " + iteration.ToString());
            Assert.That(opportunities[opportunities.Length - 1].Status, Is.EqualTo(UnicodeLineBreakStatus.Mandatory), "final line status " + iteration.ToString());
            AssertLineOpportunitiesAreMonotonic(opportunities, iteration);
            iteration = iteration + 1;
        }
    }

    [Test]
    public void IdentifierPreparation_RandomizedProfileOutputsAreStableAndValid()
    {
        Random random = new Random(0x301D3A);
        UnicodeProfiles profiles = new UnicodeProfiles();
        UnicodeIdentifiers identifiers = new UnicodeIdentifiers();
        UnicodeIdentifierProfile[] profileValues = new UnicodeIdentifierProfile[]
        {
            profiles.CreateDefaultIdentifier(),
            profiles.CreateImmutableIdentifier(),
            profiles.CreateNormalizedIdentifier(),
            profiles.CreateCaseInsensitiveIdentifier(),
        };

        int iteration = 0;
        while (iteration < 64)
        {
            string identifier = GenerateIdentifier(random, random.Next(1, 20));
            int profileIndex = 0;
            while (profileIndex < profileValues.Length)
            {
                UnicodeIdentifierProfile profile = profileValues[profileIndex];
                string prepared = identifiers.NormalizeIdentifier(identifier, profile);
                string key = identifiers.GetIdentifierCompareKey(identifier, profile);

                Assert.That(profile.Normalize(prepared), Is.EqualTo(prepared), "prepared iteration " + iteration.ToString() + " profile " + profileIndex.ToString());
                Assert.That(profile.GetCompareKey(key), Is.EqualTo(key), "key iteration " + iteration.ToString() + " profile " + profileIndex.ToString());
                Assert.That(identifiers.GetIdentifierCompareKey(prepared, profile), Is.EqualTo(key), "prepared key iteration " + iteration.ToString() + " profile " + profileIndex.ToString());
                Assert.That(identifiers.ValidateIdentifier(prepared, profile).IsValid, Is.True, "validation iteration " + iteration.ToString() + " profile " + profileIndex.ToString());
                Assert.That(identifiers.EqualsIdentifier(identifier, prepared, profile), Is.True, "equality iteration " + iteration.ToString() + " profile " + profileIndex.ToString());
                profileIndex = profileIndex + 1;
            }

            iteration = iteration + 1;
        }
    }

    [Test]
    public void SecuritySkeleton_RandomizedOutputsAreStableCanonicalAndSymmetric()
    {
        Random random = new Random(0x305EC0);
        UnicodeSecurity security = new UnicodeSecurity();
        UnicodeNormalization normalization = new UnicodeNormalization();
        int iteration = 0;
        while (iteration < 64)
        {
            string left = GenerateText(random, random.Next(0, 22), SecurityPool());
            string right = GenerateText(random, random.Next(0, 22), SecurityPool());
            string skeleton = security.GetSkeleton(left);

            Assert.That(security.GetSkeleton(skeleton), Is.EqualTo(skeleton), "stable skeleton " + iteration.ToString());
            Assert.That(normalization.IsNormalized(skeleton, UnicodeNormalizationForm.Nfd), Is.True, "NFD skeleton " + iteration.ToString());
            Assert.That(
                security.GetSkeleton(normalization.Normalize(left, UnicodeNormalizationForm.Nfd)),
                Is.EqualTo(skeleton),
                "canonical skeleton " + iteration.ToString());
            Assert.That(
                security.AreConfusable(left, right),
                Is.EqualTo(security.AreConfusable(right, left)),
                "confusable symmetry " + iteration.ToString());
            iteration = iteration + 1;
        }
    }

    [Test]
    public void Encoding_RandomizedCrossFeatureOutputsRoundTripStrictly()
    {
        Random random = new Random(0x30E0C0);
        UnicodeNormalization normalization = new UnicodeNormalization();
        UnicodeCase casing = new UnicodeCase();
        UnicodeSecurity security = new UnicodeSecurity();
        UnicodePdfText pdfText = new UnicodePdfText();
        UnicodeByteEncoding encoding = new UnicodeByteEncoding();
        UnicodePdfEncoding pdfEncoding = new UnicodePdfEncoding();
        UnicodeByteEncodingKind[] kinds = new UnicodeByteEncodingKind[]
        {
            UnicodeByteEncodingKind.Utf8,
            UnicodeByteEncodingKind.Utf16Le,
            UnicodeByteEncodingKind.Utf16Be,
            UnicodeByteEncodingKind.Utf32Le,
            UnicodeByteEncodingKind.Utf32Be,
        };

        int iteration = 0;
        while (iteration < 48)
        {
            string text = GenerateText(random, random.Next(0, 24), CrossFeaturePool());
            string[] outputs = new string[]
            {
                normalization.Normalize(text, UnicodeNormalizationForm.Nfc),
                casing.Fold(text),
                casing.ToNfkcCaseFold(text),
                security.GetSkeleton(text),
                pdfText.PrepareActualText(text),
            };
            int outputIndex = 0;
            while (outputIndex < outputs.Length)
            {
                int kindIndex = 0;
                while (kindIndex < kinds.Length)
                {
                    byte[] bytes = encoding.EncodeWithPolicy(outputs[outputIndex], kinds[kindIndex], UnicodeMalformedInputPolicy.Strict);
                    Assert.That(
                        encoding.DecodeWithPolicy(bytes, kinds[kindIndex], UnicodeMalformedInputPolicy.Strict),
                        Is.EqualTo(outputs[outputIndex]),
                        "iteration " + iteration.ToString() + " output " + outputIndex.ToString() + " encoding " + kinds[kindIndex].ToString());
                    kindIndex = kindIndex + 1;
                }

                Assert.That(
                    pdfEncoding.DecodeTextString(pdfEncoding.EncodeTextString(outputs[outputIndex])),
                    Is.EqualTo(outputs[outputIndex]),
                    "PDF iteration " + iteration.ToString() + " output " + outputIndex.ToString());
                outputIndex = outputIndex + 1;
            }

            iteration = iteration + 1;
        }
    }

    [Test]
    public void PdfExtractionMappings_RandomizedMapsAreValidDeterministicAndExact()
    {
        Random random = new Random(0x30C0DE);
        UnicodePdfCMap cmap = new UnicodePdfCMap();
        UnicodePdfDiagnostics diagnostics = new UnicodePdfDiagnostics();
        int iteration = 0;
        while (iteration < 40)
        {
            UnicodePdfGlyphMapping[] mappings = GeneratePdfMappings(random, random.Next(1, 14));
            UnicodePdfGlyphMapping[] shuffled = CopyAndShuffleMappings(mappings, random);
            UnicodePdfGlyphMap orderedMap = new UnicodePdfGlyphMap("Random Font", mappings);
            UnicodePdfGlyphMap shuffledMap = new UnicodePdfGlyphMap("Random Font", shuffled);

            Assert.That(cmap.ValidateToUnicodeMap(orderedMap).IsValid, Is.True, "CMap validation " + iteration.ToString());
            Assert.That(diagnostics.CheckFontUnicodeMapping(orderedMap).IsValid, Is.True, "font diagnostics " + iteration.ToString());

            string source = cmap.CreateToUnicodeMap(orderedMap);
            Assert.That(cmap.CreateToUnicodeMap(shuffledMap), Is.EqualTo(source), "deterministic CMap " + iteration.ToString());
            int width = UnicodePdfCMap.GetCharacterCodeHexWidth(mappings);
            int mappingIndex = 0;
            while (mappingIndex < mappings.Length)
            {
                string expectedEntry = UnicodePdfCMap.FormatCharacterCode(mappings[mappingIndex].CharacterCode, width)
                    + " "
                    + UnicodePdfCMap.FormatUnicodeString(mappings[mappingIndex].UnicodeCodePoints);
                Assert.That(source, Does.Contain(expectedEntry), "mapping iteration " + iteration.ToString() + " index " + mappingIndex.ToString());
                mappingIndex = mappingIndex + 1;
            }

            UnicodePdfGlyphMapping[] snapshot = orderedMap.GetMappings();
            snapshot[0].CharacterCode = 0;
            snapshot[0].UnicodeCodePoints[0] = 0x005A;
            Assert.That(cmap.CreateToUnicodeMap(orderedMap), Is.EqualTo(source), "mapping ownership " + iteration.ToString());
            iteration = iteration + 1;
        }
    }

    [TestCase("I\u0307\u00DF\u03A3")]
    [TestCase("A\u0300\u0327\u0340")]
    [TestCase("\u2126\u00B2\u00AD")]
    [TestCase("\U0001F469\u200D\U0001F4BB\U0001F1FA\U0001F1F8")]
    public void CrossFeatureRegressionCorpus_RemainsStable(string text)
    {
        UnicodeNormalization normalization = new UnicodeNormalization();
        UnicodeCase casing = new UnicodeCase();
        UnicodeSecurity security = new UnicodeSecurity();
        UnicodeSegments segments = new UnicodeSegments();
        string normalized = normalization.Normalize(text, UnicodeNormalizationForm.Nfc);
        string folded = casing.ToNfkcCaseFold(text);
        string skeleton = security.GetSkeleton(text);

        Assert.That(normalization.Normalize(normalized, UnicodeNormalizationForm.Nfc), Is.EqualTo(normalized));
        Assert.That(casing.ToNfkcCaseFold(folded), Is.EqualTo(folded));
        Assert.That(security.GetSkeleton(skeleton), Is.EqualTo(skeleton));
        AssertBreaksMatchSegments(segments.GetGraphemeBreaks(text), segments.GetGraphemeSegments(text), text.Length, "regression grapheme");
        Assert.That(new UnicodePdfEncoding().DecodeTextString(new UnicodePdfEncoding().EncodeTextString(normalized)), Is.EqualTo(normalized));
    }

    static void AssertBreaksMatchSegments(int[] breaks, UnicodeSegmentRecord[] segments, int textLength, string message)
    {
        Assert.That(breaks, Is.Not.Empty, message + " breaks");
        Assert.That(breaks[0], Is.EqualTo(0), message + " first");
        Assert.That(breaks[breaks.Length - 1], Is.EqualTo(textLength), message + " last");
        Assert.That(segments, Has.Length.EqualTo(breaks.Length - 1), message + " segment count");

        int index = 1;
        while (index < breaks.Length)
        {
            Assert.That(breaks[index], Is.GreaterThan(breaks[index - 1]), message + " monotonic " + index.ToString());
            Assert.That(segments[index - 1].StartUtf16Index, Is.EqualTo(breaks[index - 1]), message + " start " + index.ToString());
            Assert.That(segments[index - 1].EndUtf16Index, Is.EqualTo(breaks[index]), message + " end " + index.ToString());
            Assert.That(segments[index - 1].Utf16Length, Is.EqualTo(breaks[index] - breaks[index - 1]), message + " length " + index.ToString());
            index = index + 1;
        }
    }

    static void AssertLineOpportunitiesAreMonotonic(UnicodeLineBreakRecord[] opportunities, int iteration)
    {
        int index = 1;
        while (index < opportunities.Length)
        {
            Assert.That(
                opportunities[index].Utf16Index,
                Is.GreaterThan(opportunities[index - 1].Utf16Index),
                "line monotonic iteration " + iteration.ToString() + " index " + index.ToString());
            index = index + 1;
        }
    }

    static string GenerateIdentifier(Random random, int scalarCount)
    {
        int[] starts = new int[] { 0x0041, 0x005A, 0x0061, 0x007A, 0x00C0, 0x00DF, 0x03B1, 0x0430 };
        int[] continues = new int[] { 0x0041, 0x0061, 0x007A, 0x0030, 0x0039, 0x005F, 0x00C0, 0x00DF, 0x0300, 0x0301, 0x03B1, 0x0430, 0x2126 };
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(new UnicodeScalar().ToUtf16String(starts[random.Next(0, starts.Length)]));
        int index = 1;
        while (index < scalarCount)
        {
            builder.Append(new UnicodeScalar().ToUtf16String(continues[random.Next(0, continues.Length)]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static UnicodePdfGlyphMapping[] GeneratePdfMappings(Random random, int count)
    {
        int[] pool = new int[] { 0x0041, 0x0066, 0x0069, 0x00C0, 0x0301, 0x03B1, 0x0430, 0x20AC, 0x3400, 0x1F600, 0x1F642 };
        UnicodePdfGlyphMapping[] mappings = new UnicodePdfGlyphMapping[count];
        int index = 0;
        while (index < count)
        {
            int sequenceLength = random.Next(2, 4);
            int[] sequence = new int[sequenceLength];
            int sequenceIndex = 0;
            while (sequenceIndex < sequence.Length)
            {
                sequence[sequenceIndex] = pool[random.Next(0, pool.Length)];
                sequenceIndex = sequenceIndex + 1;
            }

            mappings[index] = new UnicodePdfGlyphMapping(0x0100 + (index * 2), sequence);
            index = index + 1;
        }

        return mappings;
    }

    static UnicodePdfGlyphMapping[] CopyAndShuffleMappings(UnicodePdfGlyphMapping[] mappings, Random random)
    {
        UnicodePdfGlyphMapping[] copy = new UnicodePdfGlyphMapping[mappings.Length];
        int index = 0;
        while (index < mappings.Length)
        {
            copy[index] = new UnicodePdfGlyphMapping(mappings[index].CharacterCode, mappings[index].UnicodeCodePoints);
            index = index + 1;
        }

        index = copy.Length - 1;
        while (index > 0)
        {
            int swapIndex = random.Next(0, index + 1);
            UnicodePdfGlyphMapping temporary = copy[index];
            copy[index] = copy[swapIndex];
            copy[swapIndex] = temporary;
            index = index - 1;
        }

        return copy;
    }

    static string GenerateText(Random random, int scalarCount, int[] pool)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < scalarCount)
        {
            builder.Append(new UnicodeScalar().ToUtf16String(pool[random.Next(0, pool.Length)]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static int[] CaseAndNormalizationPool()
    {
        return new int[]
        {
            0x0041, 0x0049, 0x004A, 0x0053, 0x0061, 0x0069, 0x006A, 0x00B2, 0x00C0,
            0x00CC, 0x00DF, 0x012E, 0x0130, 0x0300, 0x0301, 0x0307, 0x0315, 0x0327,
            0x0340, 0x0345, 0x03A3, 0x03C2, 0x03C3, 0x2126, 0xAC00, 0x1F600,
        };
    }

    static int[] SegmentationPool()
    {
        return new int[]
        {
            0x000A, 0x000D, 0x0020, 0x0027, 0x002E, 0x0030, 0x0041, 0x0061, 0x0300,
            0x0301, 0x0600, 0x094D, 0x200D, 0x2028, 0x1F1E6, 0x1F1E7, 0x1F3FB,
            0x1F469, 0x1F4BB, 0x1F600,
        };
    }

    static int[] SecurityPool()
    {
        return new int[]
        {
            0x0041, 0x004F, 0x0061, 0x006C, 0x006D, 0x006F, 0x0072, 0x006E, 0x00AD,
            0x00E9, 0x0300, 0x0301, 0x0391, 0x03B1, 0x03BF, 0x0430, 0x043E, 0x0440,
            0x200B, 0x3400, 0x1F600,
        };
    }

    static int[] CrossFeaturePool()
    {
        return new int[]
        {
            0x000A, 0x0020, 0x0030, 0x0041, 0x0049, 0x0061, 0x006D, 0x00B2, 0x00C0,
            0x00DF, 0x0130, 0x0300, 0x0301, 0x03A3, 0x0391, 0x0430, 0x200D, 0x2126,
            0x3400, 0xAC00, 0x1F469, 0x1F4BB, 0x1F600,
        };
    }
}
