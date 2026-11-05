using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPaletteBuilderTests
{
    [Test]
    public void CanReusePalette_MatchesRequiredColorsRegardlessOfOrder()
    {
        GifPalette candidate = new GifPalette(new GifColor[]
        {
            new GifColor(255, 0, 0),
            new GifColor(0, 255, 0),
            new GifColor(0, 0, 255)
        });
        GifPalette required = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 255),
            new GifColor(255, 0, 0)
        });

        GifPaletteReuseResult result = new GifPaletteBuilder().CanReusePalette(candidate, required);

        Assert.That(result.Success, Is.True);
        Assert.That(result.CanReuse, Is.True);
        Assert.That(result.MatchedColorCount, Is.EqualTo(2));
        Assert.That(result.MissingColorCount, Is.EqualTo(0));
        Assert.That(result.FirstMissingColorIndex, Is.EqualTo(-1));
    }

    [Test]
    public void CanReusePalette_ExcludesTransparentSlotsWhenMatchingOpaqueColors()
    {
        GifPalette candidate = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(0, 255, 0)
        });
        GifPalette transparentRequired = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(0, 255, 0)
        });
        GifPalette opaqueRequired = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(0, 255, 0)
        });

        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPaletteReuseResult transparent = builder.CanReusePaletteWithTransparency(candidate, transparentRequired, 0, 0);
        GifPaletteReuseResult opaque = builder.CanReusePaletteWithTransparency(candidate, opaqueRequired, 0, -1);

        Assert.That(transparent.Success, Is.True);
        Assert.That(transparent.CanReuse, Is.True);
        Assert.That(transparent.MatchedColorCount, Is.EqualTo(1));
        Assert.That(opaque.Success, Is.True);
        Assert.That(opaque.CanReuse, Is.False);
        Assert.That(opaque.MissingColorCount, Is.EqualTo(1));
        Assert.That(opaque.FirstMissingColorIndex, Is.EqualTo(0));
    }

    [Test]
    public void PlanGlobalPaletteReuse_ChoosesGlobalForReusableFramesAndLocalForMissingColors()
    {
        GifPalette global = new GifPalette(new GifColor[]
        {
            new GifColor(255, 0, 0),
            new GifColor(0, 255, 0)
        });
        GifLocalPaletteBuildResult local = CreateSuccessfulLocalPaletteResult(new GifPalette[]
        {
            new GifPalette(new GifColor[]
            {
                new GifColor(0, 255, 0),
                new GifColor(255, 0, 0)
            }),
            new GifPalette(new GifColor[]
            {
                new GifColor(0, 0, 255),
                new GifColor(255, 0, 0)
            })
        });

        GifPaletteReusePlanResult result = new GifPaletteBuilder().PlanGlobalPaletteReuse(global, local);

        Assert.That(result.Success, Is.True);
        Assert.That(result.GlobalPalette, Is.SameAs(global));
        Assert.That(result.ReuseGlobalPalette[0], Is.True);
        Assert.That(result.UseLocalPalette[0], Is.False);
        Assert.That(result.ReuseGlobalPalette[1], Is.False);
        Assert.That(result.UseLocalPalette[1], Is.True);
        Assert.That(result.ReusedGlobalPaletteCount, Is.EqualTo(1));
        Assert.That(result.LocalPaletteCount, Is.EqualTo(1));
    }

    [Test]
    public void BuildPaletteReusePlan_BuildsGlobalAndLocalPalettesAndReusesGlobal()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(new GifBitmap(2, 1, new byte[]
            {
                255, 0, 0, 255,
                0, 255, 0, 255
            })),
            CreateAnimationFrame(new GifBitmap(2, 1, new byte[]
            {
                0, 255, 0, 255,
                255, 0, 0, 255
            }))
        };

        GifPaletteReusePlanResult result = new GifPaletteBuilder().BuildPaletteReusePlan(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.GlobalPaletteResult.Success, Is.True);
        Assert.That(result.GlobalPaletteResult.UniqueColorCount, Is.EqualTo(2));
        Assert.That(result.LocalPaletteResult.Success, Is.True);
        Assert.That(result.LocalPaletteResult.FrameCount, Is.EqualTo(2));
        Assert.That(result.ReusedGlobalPaletteCount, Is.EqualTo(2));
        Assert.That(result.LocalPaletteCount, Is.EqualTo(0));
        Assert.That(result.ReuseGlobalPalette[0], Is.True);
        Assert.That(result.ReuseGlobalPalette[1], Is.True);
    }

    [Test]
    public void PaletteReuse_RejectsInvalidIndexesOrMissingLocalResults()
    {
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(1, 2, 3),
            new GifColor(4, 5, 6)
        });

        GifPaletteReuseResult badIndex = builder.CanReusePaletteWithTransparency(palette, palette, 2, -1);
        GifPaletteReusePlanResult missingLocal = builder.PlanGlobalPaletteReuse(palette, new GifLocalPaletteBuildResult());

        Assert.That(badIndex.Success, Is.False);
        Assert.That(badIndex.Error.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
        Assert.That(missingLocal.Success, Is.False);
        Assert.That(missingLocal.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
    }

    [Test]
    public void BuildLocalExactPalettes_BuildsIndependentLegalPalettesPerFrame()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(new GifBitmap(2, 1, new byte[]
            {
                255, 0, 0, 255,
                0, 255, 0, 255
            })),
            CreateAnimationFrame(new GifBitmap(3, 1, new byte[]
            {
                0, 0, 255, 255,
                255, 255, 0, 255,
                255, 0, 255, 255
            }))
        };

        GifLocalPaletteBuildResult result = new GifPaletteBuilder().BuildLocalExactPalettes(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(2));
        Assert.That(result.FrameResults.Length, Is.EqualTo(2));
        Assert.That(result.Palettes.Length, Is.EqualTo(2));
        Assert.That(result.FrameResults[0].UniqueColorCount, Is.EqualTo(2));
        Assert.That(result.FrameResults[1].UniqueColorCount, Is.EqualTo(3));
        Assert.That(result.Palettes[0].Count, Is.EqualTo(2));
        Assert.That(result.Palettes[1].Count, Is.EqualTo(4));
        Assert.That(result.Palettes[0], Is.SameAs(result.FrameResults[0].Palette));
        Assert.That(result.Palettes[1], Is.SameAs(result.FrameResults[1].Palette));
        AssertColor(result.Palettes[0].Colors[0], 255, 0, 0);
        AssertColor(result.Palettes[0].Colors[1], 0, 255, 0);
        AssertColor(result.Palettes[1].Colors[0], 0, 0, 255);
        AssertColor(result.Palettes[1].Colors[2], 255, 0, 255);
        AssertColor(result.Palettes[1].Colors[3], 255, 0, 255);
    }

    [Test]
    public void BuildLocalExactPalettes_ReservesTransparencyOnlyForFramesThatNeedIt()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(new GifBitmap(2, 1, new byte[]
            {
                10, 20, 30, 0,
                40, 50, 60, 255
            })),
            CreateAnimationFrame(new GifBitmap(2, 1, new byte[]
            {
                0, 0, 0, 255,
                255, 0, 0, 255
            }))
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.TransparentAlphaThreshold = 0;

        GifLocalPaletteBuildResult result = new GifPaletteBuilder().BuildLocalExactPalettesWithOptions(animation, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameResults[0].HasTransparency, Is.True);
        Assert.That(result.FrameResults[0].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(result.FrameResults[0].TransparentPixelCount, Is.EqualTo(1));
        Assert.That(result.FrameResults[0].UniqueColorCount, Is.EqualTo(2));
        AssertColor(result.Palettes[0].Colors[0], 0, 0, 0);
        AssertColor(result.Palettes[0].Colors[1], 40, 50, 60);
        Assert.That(result.FrameResults[1].HasTransparency, Is.False);
        Assert.That(result.FrameResults[1].TransparentColorIndex, Is.EqualTo(-1));
        Assert.That(result.FrameResults[1].TransparentPixelCount, Is.EqualTo(0));
        AssertColor(result.Palettes[1].Colors[0], 0, 0, 0);
        AssertColor(result.Palettes[1].Colors[1], 255, 0, 0);
    }

    [Test]
    public void BuildLocalExactPalettes_AllowsCombinedColorsAboveGlobalLimitWhenEachFrameFits()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(new GifBitmap(256, 1, CreateUniqueRgbaPixels(0, 256))),
            CreateAnimationFrame(new GifBitmap(256, 1, CreateUniqueRgbaPixels(256, 256)))
        };

        GifLocalPaletteBuildResult result = new GifPaletteBuilder().BuildLocalExactPalettes(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameResults[0].UniqueColorCount, Is.EqualTo(256));
        Assert.That(result.FrameResults[1].UniqueColorCount, Is.EqualTo(256));
        Assert.That(result.Palettes[0].Count, Is.EqualTo(256));
        Assert.That(result.Palettes[1].Count, Is.EqualTo(256));
        AssertColor(result.Palettes[0].Colors[255], 255, 0, 0);
        AssertColor(result.Palettes[1].Colors[0], 0, 1, 0);
    }

    [Test]
    public void BuildLocalExactPalettes_StopsOnInvalidFrameWithFrameIndex()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(new GifBitmap(1, 1, new byte[] { 1, 2, 3, 255 })),
            CreateAnimationFrame(new GifBitmap(1, 1, new byte[] { 4, 5, 6 }))
        };

        GifLocalPaletteBuildResult result = new GifPaletteBuilder().BuildLocalExactPalettes(animation);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FrameCount, Is.EqualTo(2));
        Assert.That(result.FrameResults[0].Success, Is.True);
        Assert.That(result.Palettes[0].Count, Is.EqualTo(2));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(1));
    }

    [Test]
    public void BuildLocalExactPalettes_RejectsTooManyUniqueColorsWithinFrame()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(new GifBitmap(2, 1, new byte[]
            {
                1, 0, 0, 255,
                2, 0, 0, 255
            })),
            CreateAnimationFrame(new GifBitmap(3, 1, new byte[]
            {
                3, 0, 0, 255,
                4, 0, 0, 255,
                5, 0, 0, 255
            }))
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;

        GifLocalPaletteBuildResult result = new GifPaletteBuilder().BuildLocalExactPalettesWithOptions(animation, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FrameResults[0].Success, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(1));
    }

    [Test]
    public void BuildGlobalExactPalette_CollectsUniqueColorsAcrossFramesAndCompacts()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(new GifBitmap(2, 1, new byte[]
            {
                255, 0, 0, 255,
                0, 255, 0, 255
            })),
            CreateAnimationFrame(new GifBitmap(2, 1, new byte[]
            {
                0, 0, 255, 255,
                255, 0, 0, 255
            }))
        };

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildGlobalExactPalette(animation);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.True);
        Assert.That(result.HasTransparency, Is.False);
        Assert.That(result.TransparentColorIndex, Is.EqualTo(-1));
        Assert.That(result.UniqueColorCount, Is.EqualTo(3));
        Assert.That(result.Palette.Count, Is.EqualTo(4));
        AssertColor(result.Palette.Colors[0], 255, 0, 0);
        AssertColor(result.Palette.Colors[1], 0, 255, 0);
        AssertColor(result.Palette.Colors[2], 0, 0, 255);
        AssertColor(result.Palette.Colors[3], 0, 0, 255);
    }

    [Test]
    public void BuildGlobalExactPalette_ReservesTransparencyAcrossFrames()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(new GifBitmap(2, 1, new byte[]
            {
                255, 0, 0, 0,
                0, 255, 0, 255
            })),
            CreateAnimationFrame(new GifBitmap(1, 1, new byte[]
            {
                0, 0, 0, 255
            }))
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.TransparentAlphaThreshold = 0;

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildGlobalExactPaletteWithOptions(animation, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTransparency, Is.True);
        Assert.That(result.TransparentColorIndex, Is.EqualTo(0));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(1));
        Assert.That(result.UniqueColorCount, Is.EqualTo(3));
        Assert.That(result.Palette.Count, Is.EqualTo(4));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 0, 255, 0);
        AssertColor(result.Palette.Colors[2], 0, 0, 0);
        AssertColor(result.Palette.Colors[3], 0, 0, 0);
    }

    [Test]
    public void BuildGlobalExactPalette_RejectsTooManyUniqueColorsAcrossFrames()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(new GifBitmap(200, 1, CreateUniqueRgbaPixels(0, 200))),
            CreateAnimationFrame(new GifBitmap(57, 1, CreateUniqueRgbaPixels(200, 57)))
        };

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildGlobalExactPalette(animation);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(1));
    }

    [Test]
    public void BuildGlobalExactPalette_RejectsMissingEmptyOrInvalidFrames()
    {
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifAnimation empty = new GifAnimation();
        GifAnimation invalid = new GifAnimation();
        invalid.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(new GifBitmap(1, 1, new byte[] { 1, 2, 3 }))
        };

        GifPaletteBuildResult missing = builder.BuildGlobalExactPalette(null!);
        GifPaletteBuildResult emptyResult = builder.BuildGlobalExactPalette(empty);
        GifPaletteBuildResult badLength = builder.BuildGlobalExactPalette(invalid);

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(emptyResult.Success, Is.False);
        Assert.That(emptyResult.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(badLength.Success, Is.False);
        Assert.That(badLength.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(badLength.Error.Position.FrameIndex, Is.EqualTo(0));
    }

    [Test]
    public void CompactToLegalColorTableSize_PadsToNextPowerOfTwo()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(1, 2, 3),
            new GifColor(4, 5, 6),
            new GifColor(7, 8, 9)
        });

        GifPaletteCompactionResult result = new GifPaletteBuilder().CompactToLegalColorTableSize(palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.OriginalColorCount, Is.EqualTo(3));
        Assert.That(result.LegalColorCount, Is.EqualTo(4));
        Assert.That(result.ColorTableSizeCode, Is.EqualTo(1));
        Assert.That(result.AddedPadding, Is.True);
        Assert.That(result.Palette.Count, Is.EqualTo(4));
        AssertColor(result.Palette.Colors[0], 1, 2, 3);
        AssertColor(result.Palette.Colors[2], 7, 8, 9);
        AssertColor(result.Palette.Colors[3], 7, 8, 9);
    }

    [Test]
    public void CompactToLegalColorTableSize_CopiesAlreadyLegalPaletteWithoutPadding()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(1, 2, 3),
            new GifColor(4, 5, 6),
            new GifColor(7, 8, 9),
            new GifColor(10, 11, 12)
        });

        GifPaletteCompactionResult result = new GifPaletteBuilder().CompactToLegalColorTableSize(palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LegalColorCount, Is.EqualTo(4));
        Assert.That(result.ColorTableSizeCode, Is.EqualTo(1));
        Assert.That(result.AddedPadding, Is.False);
        Assert.That(result.Palette.Colors, Is.Not.SameAs(palette.Colors));
        Assert.That(result.Palette.Colors[0], Is.Not.SameAs(palette.Colors[0]));
        AssertColor(result.Palette.Colors[3], 10, 11, 12);
    }

    [Test]
    public void CompactToLegalColorTableSize_ExpandsSingleColorToMinimumGifSize()
    {
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(9, 8, 7) });

        GifPaletteCompactionResult result = new GifPaletteBuilder().CompactToLegalColorTableSize(palette);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LegalColorCount, Is.EqualTo(2));
        Assert.That(result.ColorTableSizeCode, Is.EqualTo(0));
        Assert.That(result.AddedPadding, Is.True);
        AssertColor(result.Palette.Colors[0], 9, 8, 7);
        AssertColor(result.Palette.Colors[1], 9, 8, 7);
    }

    [Test]
    public void CompactToLegalColorTableSize_AcceptsMaximumGifPaletteSize()
    {
        GifColor[] colors = new GifColor[256];
        int index = 0;
        while (index < colors.Length)
        {
            colors[index] = new GifColor((byte)index, 0, 0);
            index = index + 1;
        }

        GifPaletteCompactionResult result = new GifPaletteBuilder().CompactToLegalColorTableSize(new GifPalette(colors));

        Assert.That(result.Success, Is.True);
        Assert.That(result.LegalColorCount, Is.EqualTo(256));
        Assert.That(result.ColorTableSizeCode, Is.EqualTo(7));
        Assert.That(result.AddedPadding, Is.False);
        AssertColor(result.Palette.Colors[255], 255, 0, 0);
    }

    [Test]
    public void CompactToLegalColorTableSize_RejectsMissingEmptyTooLargeOrNullColors()
    {
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPalette tooLarge = new GifPalette(new GifColor[257]);
        GifPalette missingColor = new GifPalette(new GifColor[] { new GifColor(1, 2, 3), null! });

        GifPaletteCompactionResult missing = builder.CompactToLegalColorTableSize(null!);
        GifPaletteCompactionResult empty = builder.CompactToLegalColorTableSize(new GifPalette(null));
        GifPaletteCompactionResult large = builder.CompactToLegalColorTableSize(tooLarge);
        GifPaletteCompactionResult badColor = builder.CompactToLegalColorTableSize(missingColor);

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
        Assert.That(empty.Success, Is.False);
        Assert.That(empty.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(large.Success, Is.False);
        Assert.That(large.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(badColor.Success, Is.False);
        Assert.That(badColor.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
    }

    [Test]
    public void FindNearestColor_ReturnsExactMatchWithZeroDistance()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(1, 2, 3),
            new GifColor(10, 20, 30)
        });

        GifPaletteLookupResult result = new GifPaletteBuilder().FindNearestRgbColor(palette, 10, 20, 30);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ColorIndex, Is.EqualTo(1));
        Assert.That(result.DistanceSquared, Is.EqualTo(0));
        Assert.That(result.Color, Is.SameAs(palette.Colors[1]));
    }

    [Test]
    public void FindNearestColor_UsesSquaredRgbDistance()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(100, 100, 100),
            new GifColor(12, 14, 16)
        });

        GifPaletteLookupResult result = new GifPaletteBuilder().FindNearestRgbColor(palette, 10, 10, 10);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ColorIndex, Is.EqualTo(2));
        Assert.That(result.DistanceSquared, Is.EqualTo(56));
    }

    [Test]
    public void FindNearestColor_PreservesPaletteOrderOnTies()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(20, 0, 0)
        });

        GifPaletteLookupResult result = new GifPaletteBuilder().FindNearestRgbColor(palette, 10, 0, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ColorIndex, Is.EqualTo(0));
        Assert.That(result.DistanceSquared, Is.EqualTo(100));
    }

    [Test]
    public void FindNearestColor_CanExcludeTransparentPaletteIndex()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 0, 0),
            new GifColor(0, 0, 0)
        });

        GifPaletteLookupResult result = new GifPaletteBuilder().FindNearestRgbColorExcludingIndex(palette, 0, 0, 0, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ColorIndex, Is.EqualTo(2));
        Assert.That(result.DistanceSquared, Is.EqualTo(0));
    }

    [Test]
    public void FindNearestColor_RejectsMissingEmptyOrUnusablePalette()
    {
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPalette empty = new GifPalette(null);
        GifPalette missingColor = new GifPalette(new GifColor[] { null! });
        GifPalette oneColor = new GifPalette(new GifColor[] { new GifColor(1, 2, 3) });

        GifPaletteLookupResult missing = builder.FindNearestRgbColor(null!, 1, 2, 3);
        GifPaletteLookupResult emptyResult = builder.FindNearestRgbColor(empty, 1, 2, 3);
        GifPaletteLookupResult badColor = builder.FindNearestRgbColor(missingColor, 1, 2, 3);
        GifPaletteLookupResult excludedOnly = builder.FindNearestRgbColorExcludingIndex(oneColor, 1, 2, 3, 0);

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
        Assert.That(emptyResult.Success, Is.False);
        Assert.That(emptyResult.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(badColor.Success, Is.False);
        Assert.That(badColor.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
        Assert.That(excludedOnly.Success, Is.False);
        Assert.That(excludedOnly.Error.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
    }

    [Test]
    public void BuildExactPaletteWithTransparency_ReservesTransparentIndexAndExcludesTransparentRgb()
    {
        byte[] rgbaPixels = new byte[]
        {
            10, 20, 30, 255,
            255, 0, 0, 0,
            0, 0, 0, 255,
            40, 50, 60, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.TransparentAlphaThreshold = 0;

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildExactRgbaPaletteWithTransparency(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTransparency, Is.True);
        Assert.That(result.TransparentColorIndex, Is.EqualTo(0));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(1));
        Assert.That(result.Palette.Count, Is.EqualTo(4));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[1], 10, 20, 30);
        AssertColor(result.Palette.Colors[2], 0, 0, 0);
        AssertColor(result.Palette.Colors[3], 40, 50, 60);
    }

    [Test]
    public void BuildExactPaletteWithTransparency_UsesAlphaThresholdInclusively()
    {
        byte[] rgbaPixels = new byte[]
        {
            1, 2, 3, 10,
            4, 5, 6, 11
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.TransparentAlphaThreshold = 10;

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildExactRgbaPaletteWithTransparency(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTransparency, Is.True);
        Assert.That(result.TransparentPixelCount, Is.EqualTo(1));
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        AssertColor(result.Palette.Colors[1], 4, 5, 6);
    }

    [Test]
    public void BuildExactPaletteWithTransparency_LeavesOpaqueImagesWithoutTransparentIndex()
    {
        byte[] rgbaPixels = new byte[]
        {
            1, 2, 3, 255,
            4, 5, 6, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.TransparentAlphaThreshold = 0;

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildExactRgbaPaletteWithTransparency(rgbaPixels, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTransparency, Is.False);
        Assert.That(result.TransparentColorIndex, Is.EqualTo(-1));
        Assert.That(result.TransparentPixelCount, Is.EqualTo(0));
        Assert.That(result.Palette.Count, Is.EqualTo(2));
        AssertColor(result.Palette.Colors[0], 1, 2, 3);
        AssertColor(result.Palette.Colors[1], 4, 5, 6);
    }

    [Test]
    public void BuildExactPaletteWithTransparency_RejectsWhenTransparentReserveExceedsMaxColors()
    {
        byte[] rgbaPixels = new byte[]
        {
            1, 2, 3, 0,
            4, 5, 6, 255,
            7, 8, 9, 255
        };
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        options.TransparentAlphaThreshold = 0;

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildExactRgbaPaletteWithTransparency(rgbaPixels, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
    }

    [Test]
    public void BuildExactPaletteWithTransparency_RejectsInvalidAlphaThreshold()
    {
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.TransparentAlphaThreshold = 256;

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildExactRgbaPaletteWithTransparency(new byte[] { 1, 2, 3, 255 }, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void BuildExactPalette_PreservesFirstSeenUniqueRgbColors()
    {
        byte[] rgbaPixels = new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255,
            255, 0, 0, 0,
            0, 0, 255, 255
        };

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildExactRgbaPalette(rgbaPixels);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Exact, Is.True);
        Assert.That(result.UniqueColorCount, Is.EqualTo(3));
        AssertColor(result.Palette.Colors[0], 255, 0, 0);
        AssertColor(result.Palette.Colors[1], 0, 255, 0);
        AssertColor(result.Palette.Colors[2], 0, 0, 255);
    }

    [Test]
    public void BuildExactPalette_AcceptsExactlyMaximumUniqueColors()
    {
        byte[] rgbaPixels = CreateUniqueRgbaPixels(256);

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildExactRgbaPalette(rgbaPixels);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Palette.Count, Is.EqualTo(256));
        Assert.That(result.UniqueColorCount, Is.EqualTo(256));
        AssertColor(result.Palette.Colors[0], 0, 0, 0);
        AssertColor(result.Palette.Colors[255], 255, 0, 0);
    }

    [Test]
    public void BuildExactPalette_RejectsMoreThanMaximumUniqueColors()
    {
        byte[] rgbaPixels = CreateUniqueRgbaPixels(257);

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildExactRgbaPalette(rgbaPixels);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
    }

    [Test]
    public void BuildExactPalette_UsesConfiguredMaximumColorCount()
    {
        GifQuantizeOptions options = new GifQuantizeOptions();
        options.MaxColors = 2;
        byte[] rgbaPixels = new byte[]
        {
            1, 0, 0, 255,
            2, 0, 0, 255,
            3, 0, 0, 255
        };

        GifPaletteBuildResult result = new GifPaletteBuilder().BuildExactRgbaPaletteWithOptions(rgbaPixels, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidColorTableSize));
    }

    [Test]
    public void BuildExactPalette_FromBitmapValidatesDimensionsAndRgbaLength()
    {
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifBitmap valid = new GifBitmap(2, 1, new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 });
        GifBitmap invalidLength = new GifBitmap(1, 1, new byte[] { 1, 2, 3 });
        GifBitmap invalidDimensions = new GifBitmap(0, 1, new byte[0]);

        GifPaletteBuildResult success = builder.BuildExactBitmapPalette(valid);
        GifPaletteBuildResult badLength = builder.BuildExactBitmapPalette(invalidLength);
        GifPaletteBuildResult badDimensions = builder.BuildExactBitmapPalette(invalidDimensions);

        Assert.That(success.Success, Is.True);
        Assert.That(success.Palette.Count, Is.EqualTo(2));
        Assert.That(badLength.Success, Is.False);
        Assert.That(badLength.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(badDimensions.Success, Is.False);
        Assert.That(badDimensions.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
    }

    [Test]
    public void BuildExactPalette_RejectsRgbaByteCountNotDivisibleByFour()
    {
        GifPaletteBuildResult result = new GifPaletteBuilder().BuildExactRgbaPalette(new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    static byte[] CreateUniqueRgbaPixels(int colorCount)
    {
        return CreateUniqueRgbaPixels(0, colorCount);
    }

    static byte[] CreateUniqueRgbaPixels(int startIndex, int colorCount)
    {
        byte[] pixels = new byte[colorCount * 4];
        int index = 0;
        while (index < colorCount)
        {
            int offset = index * 4;
            int colorIndex = startIndex + index;
            pixels[offset] = (byte)(colorIndex & 255);
            pixels[offset + 1] = (byte)((colorIndex >> 8) & 255);
            pixels[offset + 2] = 0;
            pixels[offset + 3] = 255;
            index = index + 1;
        }

        return pixels;
    }

    static GifAnimationFrame CreateAnimationFrame(GifBitmap bitmap)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        return frame;
    }

    static GifLocalPaletteBuildResult CreateSuccessfulLocalPaletteResult(GifPalette[] palettes)
    {
        GifLocalPaletteBuildResult result = new GifLocalPaletteBuildResult();
        result.Success = true;
        result.FrameCount = palettes.Length;
        result.Palettes = palettes;
        result.FrameResults = new GifPaletteBuildResult[palettes.Length];
        int index = 0;
        while (index < palettes.Length)
        {
            GifPaletteBuildResult frameResult = new GifPaletteBuildResult();
            frameResult.Success = true;
            frameResult.Exact = true;
            frameResult.Palette = palettes[index];
            frameResult.UniqueColorCount = palettes[index].Count;
            result.FrameResults[index] = frameResult;
            index = index + 1;
        }

        return result;
    }

    static void AssertColor(GifColor color, int red, int green, int blue)
    {
        Assert.That(color.Red, Is.EqualTo((byte)red));
        Assert.That(color.Green, Is.EqualTo((byte)green));
        Assert.That(color.Blue, Is.EqualTo((byte)blue));
    }
}
