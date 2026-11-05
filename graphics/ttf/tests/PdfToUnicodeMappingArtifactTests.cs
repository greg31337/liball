using ttf;

namespace ttf.Tests;

internal class PdfToUnicodeMappingArtifactTests
{
    [Test]
    public void MapsComplexOriginalSequencesIntoTwoByteCmap()
    {
        PdfFontUsage usage = CreateUsage(PdfFontOptions.Create());
        usage.AddGlyph(6, new int[] { 0x66, 0x69 });
        usage.AddGlyph(7, new int[] { 0x41, 0x030A });
        usage.AddGlyph(8, new int[] { 0x1F600 });
        usage.AddGlyph(9, new int[] { 0x2764, 0xFE0F });
        usage.AddGlyph(10, new int[] { 0x41, 0xE0100 });

        PdfToUnicodeMappingArtifact artifact =
            PdfToUnicodeMappingBuilder.Build(
                usage,
                PdfCidAllocator.Create(usage));
        string cmap = artifact.GetUnambiguousCMap().GetText();

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetCodeByteCount(), Is.EqualTo(2));
            Assert.That(artifact.GetMappingCount(), Is.EqualTo(5));
            Assert.That(artifact.GetAmbiguousMappingCount(), Is.Zero);
            Assert.That(artifact.GetSharedSequenceCount(), Is.Zero);
            Assert.That(artifact.IsComplete(), Is.True);
            Assert.That(artifact.GetActualTextRecommendationCount(),
                Is.Zero);
            Assert.That(artifact.GetCMap().GetText(),
                Is.EqualTo(artifact.GetUnambiguousCMap().GetText()));
            AssertMapping(artifact.GetMapping(0), 1, 6,
                new int[] { 0x66, 0x69 });
            AssertMapping(artifact.GetMapping(1), 2, 7,
                new int[] { 0x41, 0x030A });
            AssertMapping(artifact.GetMapping(2), 3, 8,
                new int[] { 0x1F600 });
            AssertMapping(artifact.GetMapping(3), 4, 9,
                new int[] { 0x2764, 0xFE0F });
            AssertMapping(artifact.GetMapping(4), 5, 10,
                new int[] { 0x41, 0xE0100 });
            Assert.That(cmap, Does.Contain("<00660069>"));
            Assert.That(cmap, Does.Contain("<0041030A>"));
            Assert.That(cmap, Does.Contain("<D83DDE00>"));
            Assert.That(cmap, Does.Contain("<2764FE0F>"));
            Assert.That(cmap, Does.Contain("<0041DB40DD00>"));
        });
    }

    [Test]
    public void PreservesLogicalClusterTextWhenGlyphsAreVisuallyReordered()
    {
        PdfFontUsage usage = CreateUsage(PdfFontOptions.Create());
        usage.AddShapedRun(CreateReorderedRun());

        PdfToUnicodeMappingArtifact artifact =
            PdfToUnicodeMappingBuilder.Build(
                usage,
                PdfCidAllocator.Create(usage));

        Assert.Multiple(() =>
        {
            AssertMapping(artifact.GetMapping(0), 1, 7,
                new int[] { 0x42 });
            AssertMapping(artifact.GetMapping(1), 2, 6,
                new int[] { 0x41, 0x0301 });
            Assert.That(artifact.GetUnambiguousCMap().GetText(),
                Does.Contain("[<0042> <00410301>]"));
        });
    }

    [Test]
    public void RetainsAmbiguousCandidatesWithoutGuessingAndMapsSharedText()
    {
        PdfFontUsage usage = CreateUsage(PdfFontOptions.Create());
        usage.AddGlyph(6, new int[] { 0x66, 0x69 });
        usage.AddGlyph(6, new int[] { 0xFB01 });
        usage.AddGlyph(7, new int[] { 0x41 });
        usage.AddGlyph(8, new int[] { 0x41 });

        PdfToUnicodeMappingArtifact artifact =
            PdfToUnicodeMappingBuilder.Build(
                usage,
                PdfCidAllocator.Create(usage));
        PdfToUnicodeCodeMapping ambiguous = artifact.GetMapping(0);
        string cmap = artifact.GetUnambiguousCMap().GetText();
        PdfActualTextRecommendation alternative =
            artifact.GetActualTextRecommendation(0);
        PdfActualTextRecommendation shared =
            artifact.GetActualTextRecommendation(1);
        PdfActualTextRecommendation?[] copiedRecommendations =
            artifact.GetActualTextRecommendations();
        copiedRecommendations[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(artifact.GetMappingCount(), Is.EqualTo(3));
            Assert.That(artifact.GetAmbiguousMappingCount(), Is.EqualTo(1));
            Assert.That(artifact.GetSharedSequenceCount(), Is.EqualTo(1));
            Assert.That(artifact.IsComplete(), Is.False);
            Assert.That(ambiguous.GetUnicodeSequenceCount(), Is.EqualTo(2));
            Assert.That(ambiguous.IsUnambiguous(), Is.False);
            Assert.That(ambiguous.HasCMapMapping(), Is.True);
            Assert.That(ambiguous.GetCMapUnicodeSequenceIndex(), Is.Zero);
            Assert.That(ambiguous.GetCMapUnicodeSequence(),
                Is.EqualTo(new int[] { 0x66, 0x69 }));
            Assert.That(ambiguous.GetUnicodeSequence(0),
                Is.EqualTo(new int[] { 0x66, 0x69 }));
            Assert.That(ambiguous.GetUnicodeSequence(1),
                Is.EqualTo(new int[] { 0xFB01 }));
            Assert.That(cmap, Does.Not.Contain("<0001>"));
            Assert.That(cmap,
                Does.Contain("<0002> <0003> [<0041> <0041>]"));
            Assert.That(artifact.FindMapping(2)!.GetSourceGlyphId(),
                Is.EqualTo(7));
            Assert.That(artifact.FindMapping(99), Is.Null);
            Assert.That(artifact.GetCMap().GetMappingCount(), Is.EqualTo(2));
            Assert.That(artifact.GetCMap().GetText(),
                Does.Contain("[<00660069> <0041>]"));
            Assert.That(artifact.FindMapping(3)!.HasCMapMapping(), Is.False);
            Assert.That(artifact.FindMapping(3)!
                .GetCMapUnicodeSequenceIndex(), Is.EqualTo(-1));
            Assert.Throws<InvalidOperationException>(() =>
                artifact.FindMapping(3)!.GetCMapUnicodeSequence());
            Assert.That(artifact.GetActualTextRecommendationCount(),
                Is.EqualTo(2));
            AssertRecommendation(
                alternative,
                1,
                6,
                new int[] { 0xFB01 },
                PdfActualTextReason.MultipleSequencesForCode);
            Assert.That(alternative.GetText(), Is.EqualTo("\uFB01"));
            AssertRecommendation(
                shared,
                3,
                8,
                new int[] { 0x41 },
                PdfActualTextReason.SequenceSharedByCodes);
            Assert.That(artifact.GetActualTextRecommendations()[0],
                Is.Not.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                artifact.GetActualTextRecommendation(2));
        });
    }

    [Test]
    public void UsesLexicalSequenceAndLowestCodePolicyDeterministically()
    {
        PdfFontUsage firstUsage = CreateUsage(PdfFontOptions.Create());
        firstUsage.AddGlyph(6, new int[] { 0x42 });
        firstUsage.AddGlyph(6, new int[] { 0x41 });
        firstUsage.AddGlyph(7, new int[] { 0x42 });
        PdfFontUsage reversedUsage = CreateUsage(PdfFontOptions.Create());
        reversedUsage.AddGlyph(6, new int[] { 0x41 });
        reversedUsage.AddGlyph(6, new int[] { 0x42 });
        reversedUsage.AddGlyph(7, new int[] { 0x42 });

        PdfToUnicodeMappingArtifact first =
            PdfToUnicodeMappingBuilder.Build(
                firstUsage,
                PdfCidAllocator.Create(firstUsage));
        PdfToUnicodeMappingArtifact reversed =
            PdfToUnicodeMappingBuilder.Build(
                reversedUsage,
                PdfCidAllocator.Create(reversedUsage));
        PdfActualTextRecommendation recommendation =
            first.GetActualTextRecommendation(0);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetCMap().GetText(),
                Is.EqualTo(reversed.GetCMap().GetText()));
            Assert.That(first.GetMapping(0).GetCMapUnicodeSequence(),
                Is.EqualTo(new int[] { 0x41 }));
            Assert.That(first.GetMapping(1).GetCMapUnicodeSequence(),
                Is.EqualTo(new int[] { 0x42 }));
            Assert.That(first.GetSharedSequenceCount(), Is.EqualTo(1));
            Assert.That(first.GetActualTextRecommendationCount(),
                Is.EqualTo(1));
            AssertRecommendation(
                recommendation,
                1,
                6,
                new int[] { 0x42 },
                PdfActualTextReason
                    .MultipleSequencesForCodeAndSequenceSharedByCodes);
        });
    }

    [Test]
    public void ExposesOneByteAndProgramArtifactMappingsDefensively()
    {
        PdfFontOptions simpleOptions = PdfFontOptions.Create();
        simpleOptions.SetFontKind(PdfFontKind.SimpleTrueType);
        PdfFontUsage simpleUsage = CreateUsage(simpleOptions);
        simpleUsage.AddGlyph(6, new int[] { 0x66, 0x69 });
        PdfSimpleFontEncodingArtifact simple =
            PdfFontEncodingBuilder.BuildSimple(
                simpleUsage,
                PdfCidAllocator.Create(simpleUsage));
        PdfToUnicodeCodeMapping simpleMapping =
            simple.GetToUnicodeMappings().GetMapping(0);
        int[] copy = simpleMapping.GetUnicodeSequence(0);
        copy[0] = 0;

        PdfFontUsage type0Usage = CreateUsage(PdfFontOptions.Create());
        type0Usage.AddGlyph(7, new int[] { 0x1F642 });
        PdfCidFontType2Artifact type2 = PdfCidFontType2Builder.Build(
            type0Usage,
            PdfCidAllocator.Create(type0Usage));

        Assert.Multiple(() =>
        {
            Assert.That(simple.GetToUnicodeMappings().GetCodeByteCount(),
                Is.EqualTo(1));
            Assert.That(simple.GetToUnicodeMappings()
                .GetUnambiguousCMap().GetText(),
                Does.Contain("<01> <00660069>"));
            Assert.That(simpleMapping.GetUnicodeScalar(0, 0),
                Is.EqualTo(0x66));
            Assert.That(type2.GetToUnicodeMappings().GetCodeByteCount(),
                Is.EqualTo(2));
            Assert.That(type2.GetToUnicodeMappings().GetMapping(0)
                .GetUnicodeSequence(0), Is.EqualTo(new int[] { 0x1F642 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                simple.GetToUnicodeMappings().GetMapping(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                simpleMapping.GetUnicodeSequence(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                simpleMapping.GetUnicodeScalar(0, 2));
        });
    }

    [Test]
    public void RejectsNullAndMismatchedBuildInputs()
    {
        PdfFontUsage usage = CreateUsage(PdfFontOptions.Create());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                PdfToUnicodeMappingBuilder.Build(
                    null!,
                    PdfCidAllocator.Create(14)));
            Assert.Throws<ArgumentNullException>(() =>
                PdfToUnicodeMappingBuilder.Build(usage, null!));
            Assert.Throws<ArgumentException>(() =>
                PdfToUnicodeMappingBuilder.Build(
                    usage,
                    PdfCidAllocator.Create(13)));
        });
    }

    static PdfFontUsage CreateUsage(PdfFontOptions options)
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        return new PdfFontUsageAccumulator().GetOrAdd(instance, options);
    }

    static ShapedRun CreateReorderedRun()
    {
        ShapedGlyph visualFirst = new ShapedGlyph(
            7,
            1,
            2,
            1,
            new int[] { 0x42 },
            1,
            0,
            1,
            0,
            ShapedGlyphFlags.None,
            500,
            0,
            0,
            0);
        ShapedGlyph visualSecond = new ShapedGlyph(
            6,
            0,
            0,
            2,
            new int[] { 0x41, 0x0301 },
            1,
            0,
            0,
            1,
            ShapedGlyphFlags.MarkAttachment,
            500,
            0,
            0,
            0);
        ShapedCluster logicalFirst = new ShapedCluster(
            0,
            2,
            new int[] { 0x41, 0x0301 },
            new int[] { 1 },
            0,
            1,
            false);
        ShapedCluster logicalSecond = new ShapedCluster(
            2,
            1,
            new int[] { 0x42 },
            new int[] { 0 },
            1,
            0,
            false);
        return new ShapedRun(
            new ShapedGlyph[] { visualFirst, visualSecond },
            new ShapedCluster[] { logicalFirst, logicalSecond },
            new int[] { 1, 0 },
            new int[] { 1, 0 },
            new int[] { 1, 0 },
            new int[] { 1, 0 },
            3,
            ShapingDirection.RightToLeft);
    }

    static void AssertMapping(
        PdfToUnicodeCodeMapping mapping,
        int code,
        int glyphId,
        int[] sequence)
    {
        Assert.Multiple(() =>
        {
            Assert.That(mapping.GetCharacterCode(), Is.EqualTo(code));
            Assert.That(mapping.GetSourceGlyphId(), Is.EqualTo(glyphId));
            Assert.That(mapping.GetUnicodeSequenceCount(), Is.EqualTo(1));
            Assert.That(mapping.IsUnambiguous(), Is.True);
            Assert.That(mapping.GetUnicodeScalarCount(0),
                Is.EqualTo(sequence.Length));
            Assert.That(mapping.GetUnicodeSequence(0), Is.EqualTo(sequence));
        });
    }

    static void AssertRecommendation(
        PdfActualTextRecommendation recommendation,
        int code,
        int glyphId,
        int[] sequence,
        PdfActualTextReason reason)
    {
        int[] copy = recommendation.GetUnicodeSequence();
        copy[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(recommendation.GetCharacterCode(), Is.EqualTo(code));
            Assert.That(recommendation.GetSourceGlyphId(),
                Is.EqualTo(glyphId));
            Assert.That(recommendation.GetReason(), Is.EqualTo(reason));
            Assert.That(recommendation.GetUnicodeScalarCount(),
                Is.EqualTo(sequence.Length));
            Assert.That(recommendation.GetUnicodeScalar(0),
                Is.EqualTo(sequence[0]));
            Assert.That(recommendation.GetUnicodeSequence(),
                Is.EqualTo(sequence));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                recommendation.GetUnicodeScalar(sequence.Length));
        });
    }
}
