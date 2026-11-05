using ttf;

namespace ttf.Tests;

internal class FontInstanceTests
{
    [Test]
    public void CreatesStableDefaultInstanceAndExposesGlobalDesignMetrics()
    {
        FontFace face = CreateFace();
        FontInstance instance = face.CreateDefaultInstance();
        FontDesignMetrics metrics = instance.GetGlobalMetrics();

        Assert.Multiple(() =>
        {
            Assert.That(face.CreateDefaultInstance(), Is.SameAs(instance));
            Assert.That(instance.GetFace(), Is.SameAs(face));
            Assert.That(instance.GetGlyphCount(), Is.EqualTo(4));
            Assert.That(instance.GetGlobalMetrics(), Is.SameAs(metrics));
            Assert.That(metrics.GetUnitsPerEm(), Is.EqualTo(1000));
            Assert.That(metrics.GetAscender(), Is.EqualTo(750));
            Assert.That(metrics.GetDescender(), Is.EqualTo(-250));
            Assert.That(metrics.GetLineGap(), Is.EqualTo(50));
        });
    }

    [Test]
    public void ExposesNormalizedSimpleOutlineExactBoundsAndCopiedInstructions()
    {
        FontInstance instance = CreateFace().CreateDefaultInstance();
        GlyphOutline outline = instance.GetGlyphOutline(0);
        GlyphPathCommand move = outline.GetCommand(0);
        GlyphPathCommand curve = outline.GetCommand(1);
        GlyphPathCommand close = outline.GetCommand(2);
        GlyphBounds? bounds = outline.GetBounds();
        byte[] instructions = outline.CopyInstructions();
        GlyphPathCommand?[] commands = outline.GetCommands();
        instructions[0] = 0;
        commands[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(instance.GetGlyphOutline(0), Is.SameAs(outline));
            Assert.That(outline.GetGlyphId(), Is.Zero);
            Assert.That(outline.IsEmpty(), Is.False);
            Assert.That(outline.IsComposite(), Is.False);
            Assert.That(outline.GetCommandCount(), Is.EqualTo(3));
            Assert.That(move.GetKind(), Is.EqualTo(GlyphPathCommandKind.Move));
            Assert.That(move.GetEndX(), Is.Zero);
            Assert.That(move.GetEndY(), Is.Zero);
            Assert.That(curve.GetKind(), Is.EqualTo(GlyphPathCommandKind.Quadratic));
            Assert.That(curve.GetControlX(), Is.EqualTo(10));
            Assert.That(curve.GetControlY(), Is.EqualTo(20));
            Assert.That(curve.GetEndX(), Is.EqualTo(20));
            Assert.That(curve.GetEndY(), Is.Zero);
            Assert.That(close.GetKind(), Is.EqualTo(GlyphPathCommandKind.Close));
            Assert.That(bounds!.IsEmpty(), Is.False);
            Assert.That(bounds.GetXMinimum(), Is.Zero);
            Assert.That(bounds.GetYMinimum(), Is.Zero);
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(20));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(10));
            Assert.That(bounds.GetWidth(), Is.EqualTo(20));
            Assert.That(bounds.GetHeight(), Is.EqualTo(10));
            Assert.That(outline.GetInstructionLength(), Is.EqualTo(2));
            Assert.That(outline.CopyInstructions(), Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(outline.GetCommand(0), Is.SameAs(move));
            Assert.That(outline.GetComponentCount(), Is.Zero);
            Assert.That(outline.GetComponentClosureCount(), Is.Zero);
            Assert.That(outline.GetComponentDepth(), Is.Zero);
            Assert.That(outline.GetMetricsGlyphId(), Is.Zero);
            Assert.That(outline.UsesInheritedMetrics(), Is.False);
        });
    }

    [Test]
    public void ExposesCompositePlacementTransformClosureAndInstructions()
    {
        GlyphOutline outline = CreateFace()
            .CreateDefaultInstance()
            .GetGlyphOutline(2);
        GlyphComponent transformed = outline.GetComponent(0);
        GlyphComponent attached = outline.GetComponent(1);
        GlyphComponent?[] components = outline.GetComponents();
        int[] closure = outline.GetComponentClosure();
        components[0] = null;
        closure[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(outline.IsComposite(), Is.True);
            Assert.That(outline.GetComponentCount(), Is.EqualTo(2));
            Assert.That(outline.GetComponentClosureCount(), Is.EqualTo(2));
            Assert.That(outline.GetComponentClosureGlyphId(0), Is.Zero);
            Assert.That(outline.GetComponentClosureGlyphId(1), Is.EqualTo(1));
            Assert.That(outline.GetComponentDepth(), Is.EqualTo(1));
            Assert.That(outline.GetMetricsGlyphId(), Is.EqualTo(1));
            Assert.That(outline.UsesInheritedMetrics(), Is.True);
            Assert.That(outline.HasOverlappingComponents(), Is.True);
            Assert.That(outline.GetInstructionLength(), Is.EqualTo(1));
            Assert.That(outline.CopyInstructions(), Is.EqualTo(new byte[] { 0xCC }));
            Assert.That(outline.GetBounds()!.GetXMinimum(), Is.EqualTo(10));
            Assert.That(outline.GetBounds()!.GetYMinimum(), Is.EqualTo(4));
            Assert.That(outline.GetBounds()!.GetXMaximum(), Is.EqualTo(20));
            Assert.That(outline.GetBounds()!.GetYMaximum(), Is.EqualTo(9));
            Assert.That(outline.GetCommand(0).GetEndX(), Is.EqualTo(10));
            Assert.That(outline.GetCommand(1).GetControlX(), Is.EqualTo(15));
            Assert.That(outline.GetCommand(1).GetControlY(), Is.EqualTo(14));
            Assert.That(outline.GetComponent(0), Is.SameAs(transformed));
        });

        Assert.Multiple(() =>
        {
            Assert.That(transformed.GetComponentIndex(), Is.Zero);
            Assert.That(transformed.GetGlyphId(), Is.Zero);
            Assert.That(transformed.GetPlacement(), Is.EqualTo(GlyphComponentPlacement.Offset));
            Assert.That(transformed.GetXOffset(), Is.EqualTo(10));
            Assert.That(transformed.GetYOffset(), Is.EqualTo(4));
            Assert.That(transformed.GetEffectiveXOffset(), Is.EqualTo(10));
            Assert.That(transformed.GetEffectiveYOffset(), Is.EqualTo(4));
            Assert.That(transformed.GetOffsetScaling(),
                Is.EqualTo(GlyphComponentOffsetScaling.Default));
            Assert.That(transformed.GetTransformKind(),
                Is.EqualTo(GlyphComponentTransformKind.UniformScale));
            Assert.That(transformed.GetXScale(), Is.EqualTo(0.5));
            Assert.That(transformed.GetYScale(), Is.EqualTo(0.5));
            Assert.That(transformed.GetScale01(), Is.Zero);
            Assert.That(transformed.GetScale10(), Is.Zero);
            Assert.That(transformed.HasMoreComponents(), Is.True);
            Assert.That(transformed.HasOverlappingComponents(), Is.True);
            Assert.That(transformed.UsesMetrics(), Is.False);
            Assert.That(attached.GetComponentIndex(), Is.EqualTo(1));
            Assert.That(attached.GetGlyphId(), Is.EqualTo(1));
            Assert.That(attached.GetPlacement(),
                Is.EqualTo(GlyphComponentPlacement.PointAttachment));
            Assert.That(attached.GetParentPointIndex(), Is.Zero);
            Assert.That(attached.GetChildPointIndex(), Is.Zero);
            Assert.That(attached.GetTransformKind(),
                Is.EqualTo(GlyphComponentTransformKind.Identity));
            Assert.That(attached.UsesMetrics(), Is.True);
            Assert.That(attached.HasCompositeInstructions(), Is.True);
            Assert.That(attached.HasMoreComponents(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                transformed.GetParentPointIndex());
            Assert.Throws<InvalidOperationException>(() => attached.GetXOffset());
        });
    }

    [Test]
    public void ResolvesInheritedComponentMetricsForRequestedGlyph()
    {
        FontInstance instance = CreateFace().CreateDefaultInstance();
        GlyphMetrics composite = instance.GetGlyphMetrics(2);
        GlyphMetrics ownSource = instance.GetGlyphMetrics(1);

        Assert.Multiple(() =>
        {
            Assert.That(instance.GetGlyphMetrics(2), Is.SameAs(composite));
            Assert.That(composite.GetGlyphId(), Is.EqualTo(2));
            Assert.That(composite.GetAdvanceWidth(), Is.EqualTo(800));
            Assert.That(composite.GetLeftSideBearing(), Is.EqualTo(20));
            Assert.That(composite.GetAdvanceHeight(), Is.EqualTo(1000));
            Assert.That(composite.GetVerticalOriginY(), Is.EqualTo(1000));
            Assert.That(composite.GetTopSideBearing(), Is.EqualTo(991));
            Assert.That(composite.UsesSynthesizedVerticalAdvance(), Is.True);
            Assert.That(ownSource.GetGlyphId(), Is.EqualTo(1));
            Assert.That(ownSource.GetAdvanceWidth(), Is.EqualTo(800));
            Assert.That(ownSource.GetLeftSideBearing(), Is.EqualTo(20));
        });
    }

    [Test]
    public void ExposesEmptyGlyphAndRejectsInvalidAccessors()
    {
        FontInstance instance = CreateFace().CreateDefaultInstance();
        GlyphOutline empty = instance.GetGlyphOutline(3);
        GlyphOutline simple = instance.GetGlyphOutline(0);
        GlyphPathCommand move = simple.GetCommand(0);
        GlyphPathCommand curve = simple.GetCommand(1);
        GlyphPathCommand close = simple.GetCommand(2);

        Assert.Multiple(() =>
        {
            Assert.That(empty.IsEmpty(), Is.True);
            Assert.That(empty.GetBounds()!.IsEmpty(), Is.True);
            Assert.That(empty.GetCommandCount(), Is.Zero);
            Assert.That(empty.GetInstructionLength(), Is.Zero);
            Assert.Throws<InvalidOperationException>(() =>
                empty.GetBounds()!.GetXMinimum());
            Assert.Throws<InvalidOperationException>(() => move.GetControlX());
            Assert.Throws<InvalidOperationException>(() =>
                curve.GetSecondControlX());
            Assert.Throws<InvalidOperationException>(() => close.GetEndX());
            Assert.Throws<ArgumentOutOfRangeException>(() => simple.GetCommand(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => simple.GetCommand(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => simple.GetComponent(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                simple.GetComponentClosureGlyphId(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.GetGlyphOutline(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.GetGlyphOutline(4));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.GetGlyphMetrics(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.GetGlyphMetrics(4));
        });
    }

    [Test]
    public void ExposesTrueTypeBoundsAndSyntheticGlyphNamesDirectly()
    {
        FontInstance instance = CreateFace().CreateDefaultInstance();
        string cachedName = instance.GetGlyphName(2);

        Assert.Multiple(() =>
        {
            Assert.That(instance.GetGlyphBounds(0),
                Is.SameAs(instance.GetGlyphOutline(0).GetBounds()));
            Assert.That(instance.GetGlyphName(0), Is.EqualTo("gid0000"));
            Assert.That(instance.GetGlyphName(2), Is.EqualTo("gid0002"));
            Assert.That(instance.GetGlyphName(2), Is.SameAs(cachedName));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.GetGlyphBounds(4));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.GetGlyphName(4));
        });
    }

    [Test]
    public void PreservesTrueTypeHintDataAsDefensiveCopies()
    {
        FontInstance instance = CreateFace().CreateDefaultInstance();
        TrueTypeHintingData? hints = instance.GetTrueTypeHintingData();
        byte[] fontProgram = hints!.CopyFontProgram();
        byte[] controlValueProgram = hints.CopyControlValueProgram();
        byte[] controlValueData = hints.CopyControlValueData();
        fontProgram[0] = 0;
        controlValueProgram[0] = 0;
        controlValueData[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(instance.GetTrueTypeHintingData(), Is.SameAs(hints));
            Assert.That(hints.HasFontProgram(), Is.True);
            Assert.That(hints.GetFontProgramLength(), Is.EqualTo(3));
            Assert.That(hints.CopyFontProgram(),
                Is.EqualTo(new byte[] { 0xB0, 0x01, 0x2C }));
            Assert.That(hints.HasControlValueProgram(), Is.True);
            Assert.That(hints.GetControlValueProgramLength(), Is.EqualTo(2));
            Assert.That(hints.CopyControlValueProgram(),
                Is.EqualTo(new byte[] { 0xB0, 0x02 }));
            Assert.That(hints.HasControlValueData(), Is.True);
            Assert.That(hints.GetControlValueDataLength(), Is.EqualTo(4));
            Assert.That(hints.CopyControlValueData(),
                Is.EqualTo(new byte[] { 0x00, 0x10, 0xFF, 0xF0 }));
            Assert.That(instance.GetGlyphOutline(0).CopyInstructions(),
                Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(instance.GetGlyphOutline(0).GetCffStemHintCount(), Is.Zero);
            Assert.That(instance.GetGlyphOutline(0).GetCffHintMaskCount(), Is.Zero);
        });
    }

    [Test]
    public void AppliesGvarToSimpleCompositeAndPhantomPoints()
    {
        FontFace face = CreateVariableFace();
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        FontInstance defaultInstance = face.CreateDefaultInstance();
        FontInstance variedInstance = face.CreateInstance(selectedCoordinates);
        GlyphOutline simple = variedInstance.GetGlyphOutline(0);
        GlyphOutline composite = variedInstance.GetGlyphOutline(2);
        GlyphOutline empty = variedInstance.GetGlyphOutline(3);
        GlyphMetrics simpleMetrics = variedInstance.GetGlyphMetrics(0);
        GlyphMetrics inheritedMetrics = variedInstance.GetGlyphMetrics(2);
        GlyphMetrics emptyMetrics = variedInstance.GetGlyphMetrics(3);

        Assert.Multiple(() =>
        {
            Assert.That(defaultInstance.GetGlyphOutline(0).GetCommand(0).GetEndX(),
                Is.Zero);
            Assert.That(simple.GetCommand(0).GetEndX(), Is.EqualTo(5));
            Assert.That(simple.GetCommand(0).GetEndY(), Is.Zero);
            Assert.That(simple.GetCommand(1).GetControlX(), Is.EqualTo(20));
            Assert.That(simple.GetCommand(1).GetControlY(), Is.EqualTo(25));
            Assert.That(simple.GetCommand(1).GetEndX(), Is.EqualTo(35));
            Assert.That(simple.GetBounds()!.GetXMinimum(), Is.EqualTo(5));
            Assert.That(simple.GetBounds()!.GetXMaximum(), Is.EqualTo(35));
            Assert.That(simple.GetBounds()!.GetYMaximum(), Is.EqualTo(12.5));
            Assert.That(simpleMetrics.GetAdvanceWidth(), Is.EqualTo(605));
            Assert.That(simpleMetrics.GetLeftSideBearing(), Is.EqualTo(13));
            Assert.That(simpleMetrics.GetAdvanceHeight(), Is.EqualTo(1015));
            Assert.That(simpleMetrics.GetTopSideBearing(), Is.EqualTo(998));
            Assert.That(simpleMetrics.GetVerticalOriginY(), Is.EqualTo(1010));
            Assert.That(composite.IsComposite(), Is.True);
            Assert.That(composite.GetComponent(0).GetXOffset(), Is.EqualTo(10));
            Assert.That(composite.GetComponent(0).GetYOffset(), Is.EqualTo(4));
            Assert.That(composite.GetComponent(0).GetEffectiveXOffset(),
                Is.EqualTo(20));
            Assert.That(composite.GetComponent(0).GetEffectiveYOffset(),
                Is.EqualTo(9));
            Assert.That(composite.GetBounds()!.GetXMinimum(), Is.EqualTo(22.5));
            Assert.That(composite.GetBounds()!.GetXMaximum(), Is.EqualTo(37.5));
            Assert.That(composite.GetBounds()!.GetYMinimum(), Is.EqualTo(9));
            Assert.That(inheritedMetrics.GetAdvanceWidth(), Is.EqualTo(810));
            Assert.That(inheritedMetrics.GetLeftSideBearing(), Is.EqualTo(10));
            Assert.That(inheritedMetrics.GetAdvanceHeight(), Is.EqualTo(1000));
            Assert.That(inheritedMetrics.GetVerticalOriginY(), Is.EqualTo(1000));
            Assert.That(empty.IsEmpty(), Is.True);
            Assert.That(emptyMetrics.GetAdvanceWidth(), Is.EqualTo(1005));
            Assert.That(emptyMetrics.GetLeftSideBearing(), Is.EqualTo(35));
            Assert.That(emptyMetrics.GetAdvanceHeight(), Is.EqualTo(1020));
            Assert.That(emptyMetrics.GetTopSideBearing(), Is.EqualTo(1015));
            Assert.That(emptyMetrics.GetVerticalOriginY(), Is.EqualTo(1015));
        });
    }

    [Test]
    public void AppliesCvarToRetainedControlValuesForNondefaultInstances()
    {
        FontFace face = CreateVariableFace();
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        TrueTypeHintingData? defaultHints =
            face.CreateDefaultInstance().GetTrueTypeHintingData();
        TrueTypeHintingData? variedHints = face
            .CreateInstance(selectedCoordinates)
            .GetTrueTypeHintingData();

        Assert.Multiple(() =>
        {
            Assert.That(defaultHints!.CopyControlValueData(), Is.EqualTo(
                new byte[] { 0, 100, 0, 200, 0xFF, 0x9C, 0, 0 }));
            Assert.That(variedHints!.CopyControlValueData(), Is.EqualTo(
                new byte[] { 0, 105, 0, 210, 0xFF, 0x92, 0, 20 }));
            Assert.That(variedHints.CopyFontProgram(), Is.EqualTo(
                new byte[] { 0xB0, 0x01, 0x2C }));
            Assert.That(variedHints.CopyControlValueProgram(), Is.EqualTo(
                new byte[] { 0xB0, 0x02 }));
        });
    }

    [Test]
    public void UsesHvarAndVvarInsteadOfGvarPhantomsForVariedMetrics()
    {
        FontFace face = CreateVariableFace(true);
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        FontInstance instance = face.CreateInstance(selectedCoordinates);
        GlyphMetrics simple = instance.GetGlyphMetrics(0);
        GlyphMetrics inherited = instance.GetGlyphMetrics(2);

        Assert.Multiple(() =>
        {
            Assert.That(simple.GetAdvanceWidth(), Is.EqualTo(650));
            Assert.That(simple.GetLeftSideBearing(), Is.EqualTo(15));
            Assert.That(simple.GetAdvanceHeight(), Is.EqualTo(950));
            Assert.That(simple.GetTopSideBearing(), Is.EqualTo(105));
            Assert.That(simple.GetVerticalOriginY(), Is.EqualTo(118));
            Assert.That(simple.UsesSynthesizedVerticalAdvance(), Is.False);
            Assert.That(simple.UsesVorgOrigin(), Is.False);
            Assert.That(inherited.GetAdvanceWidth(), Is.EqualTo(870));
            Assert.That(inherited.GetLeftSideBearing(), Is.EqualTo(35));
            Assert.That(inherited.GetAdvanceHeight(), Is.EqualTo(1070));
            Assert.That(inherited.GetTopSideBearing(), Is.EqualTo(215));
        });
    }

    [Test]
    public void InterpolatesCompositeAndMetricVariationsAtPartialScalar()
    {
        FontFace face = CreateVariableFace(true);
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set(
            (int)TestSfntEncoding.EncodeTag("wght"),
            0.25);
        FontInstance instance = face.CreateInstance(selectedCoordinates);
        GlyphOutline composite = instance.GetGlyphOutline(2);
        GlyphMetrics metrics = instance.GetGlyphMetrics(0);

        Assert.Multiple(() =>
        {
            Assert.That(composite.GetComponent(0).GetEffectiveXOffset(),
                Is.EqualTo(15));
            Assert.That(composite.GetComponent(0).GetEffectiveYOffset(),
                Is.EqualTo(6.5));
            Assert.That(metrics.GetAdvanceWidth(), Is.EqualTo(625));
            Assert.That(metrics.GetLeftSideBearing(), Is.EqualTo(13));
            Assert.That(metrics.GetAdvanceHeight(), Is.EqualTo(925));
            Assert.That(metrics.GetTopSideBearing(), Is.EqualTo(103));
        });
    }

    [Test]
    public void MaterializesAndReopensAStaticTrueTypeInstance()
    {
        FontFace face = CreateReopenableVariableFace();
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        FontInstance varied = face.CreateInstance(selectedCoordinates);
        byte[] bytes = varied.CopyStaticSfntBytes();
        byte[] repeatedBytes = varied.CopyStaticSfntBytes();
        byte[] explicitBytes = varied.CopyStaticSfntBytesWithOptions(
            new FontOperationOptions());
        FontFace staticFace = FontLoader
            .Load(bytes, FontLoadOptions.CreateCompatible())
            .GetFace(0);
        FontInstance materialized = staticFace.CreateDefaultInstance();
        GlyphOutline simple = materialized.GetGlyphOutline(0);
        GlyphOutline composite = materialized.GetGlyphOutline(2);
        GlyphMetrics simpleMetrics = materialized.GetGlyphMetrics(0);
        GlyphMetrics compositeMetrics = materialized.GetGlyphMetrics(2);
        int privateTag = (int)TestSfntEncoding.EncodeTag("ZZZZ");
        byte[] resolvedGsub = staticFace.GetTableStore()
            .FindTable(FontTags.Gsub)!.GetDataSlice().CopyBytes();
        byte[] resolvedGpos = staticFace.GetTableStore()
            .FindTable(FontTags.Gpos)!.GetDataSlice().CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.That(repeatedBytes, Is.EqualTo(bytes));
            Assert.That(repeatedBytes, Is.Not.SameAs(bytes));
            Assert.That(explicitBytes, Is.EqualTo(bytes));
            Assert.That(FontChecksum.VerifyFont(bytes), Is.True);
            Assert.That(staticFace.GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.TrueType));
            Assert.That(staticFace.GetLocaTable()!.GetFormat(),
                Is.EqualTo(SfntLocaFormat.Long));
            Assert.That(staticFace.HasTable(FontTags.Fvar), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Gvar), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Hvar), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Vvar), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Cvar), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Dsig), Is.False);
            Assert.That(staticFace.HasTable(FontTags.Stat), Is.False);
            Assert.That(staticFace.HasTable(privateTag), Is.True);
            Assert.That(staticFace.GetNames().GetSubfamilyName()!.GetValue(),
                Is.EqualTo("Medium"));
            Assert.That(staticFace.GetNames().GetTypographicSubfamilyName()!
                .GetValue(), Is.EqualTo("Medium"));
            Assert.That(staticFace.GetNames().GetFullName()!.GetValue(),
                Is.EqualTo("Demo Medium"));
            Assert.That(staticFace.GetNames().GetPostScriptName()!.GetValue(),
                Is.EqualTo("Demo-Medium"));
            Assert.That(staticFace.GetHeadTable()!.GetXMin(), Is.EqualTo(0));
            Assert.That(staticFace.GetHeadTable()!.GetYMin(), Is.EqualTo(0));
            Assert.That(staticFace.GetHeadTable()!.GetXMax(), Is.EqualTo(38));
            Assert.That(staticFace.GetHeadTable()!.GetYMax(), Is.EqualTo(25));
            Assert.That(staticFace.GetMaxpTable()!.GetMaxPoints(), Is.EqualTo(3));
            Assert.That(staticFace.GetMaxpTable()!.GetMaxContours(), Is.EqualTo(1));
            Assert.That(staticFace.GetMaxpTable()!.GetMaxCompositePoints(),
                Is.EqualTo(5));
            Assert.That(staticFace.GetMaxpTable()!.GetMaxCompositeContours(),
                Is.EqualTo(2));
            Assert.That(staticFace.GetMaxpTable()!.GetMaxInstructionSize(),
                Is.EqualTo(2));
            Assert.That(staticFace.GetOs2Table()!.GetWeightClass(), Is.EqualTo(1));
            Assert.That(staticFace.GetOs2Table()!.GetWidthClass(), Is.EqualTo(5));
            Assert.That(staticFace.GetOs2Table()!.HasSelectionFlag(6), Is.True);
            Assert.That(ReadUInt32(resolvedGsub, 0), Is.EqualTo(0x00010000U));
            Assert.That(ReadLayoutFeatureLookup(resolvedGsub), Is.EqualTo(1));
            Assert.That(ReadUInt32(resolvedGpos, 0), Is.EqualTo(0x00010000U));
            Assert.That(ReadLayoutFeatureLookup(resolvedGpos), Is.EqualTo(1));
            Assert.That(simple.GetCommand(0).GetEndX(), Is.EqualTo(5));
            Assert.That(simple.GetCommand(1).GetControlX(), Is.EqualTo(20));
            Assert.That(simple.GetCommand(1).GetControlY(), Is.EqualTo(25));
            Assert.That(simple.GetCommand(1).GetEndX(), Is.EqualTo(35));
            Assert.That(simple.CopyInstructions(),
                Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(composite.GetBounds()!.GetXMinimum(), Is.EqualTo(22.5));
            Assert.That(composite.GetBounds()!.GetXMaximum(), Is.EqualTo(37.5));
            Assert.That(composite.GetBounds()!.GetYMinimum(), Is.EqualTo(9));
            Assert.That(composite.CopyInstructions(),
                Is.EqualTo(new byte[] { 0xCC }));
            Assert.That(composite.UsesInheritedMetrics(), Is.False);
            Assert.That(simpleMetrics.GetAdvanceWidth(), Is.EqualTo(650));
            Assert.That(simpleMetrics.GetLeftSideBearing(), Is.EqualTo(15));
            Assert.That(simpleMetrics.GetAdvanceHeight(), Is.EqualTo(950));
            Assert.That(simpleMetrics.GetTopSideBearing(), Is.EqualTo(105));
            Assert.That(compositeMetrics.GetAdvanceWidth(), Is.EqualTo(870));
            Assert.That(compositeMetrics.GetLeftSideBearing(), Is.EqualTo(35));
            Assert.That(compositeMetrics.GetAdvanceHeight(), Is.EqualTo(1070));
            Assert.That(compositeMetrics.GetTopSideBearing(), Is.EqualTo(215));
            Assert.That(materialized.GetTrueTypeHintingData()!
                .CopyControlValueData(), Is.EqualTo(
                    new byte[] { 0, 105, 0, 210, 0xFF, 0x92, 0, 20 }));
        });

        repeatedBytes[0] = 0;
        Assert.That(varied.CopyStaticSfntBytes(), Is.EqualTo(bytes));
    }

    [Test]
    public void ReopenedTrueTypeInstanceHasNoLiveVariationDependencies()
    {
        FontFace source = CreateReopenableVariableFace();
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set(
            (int)TestSfntEncoding.EncodeTag("wght"),
            0.5);

        Assert.Multiple(() =>
        {
            Assert.That(source.HasTable(FontTags.Avar), Is.True);
            Assert.That(source.HasTable(FontTags.Cvar), Is.True);
            Assert.That(source.HasTable(FontTags.Fvar), Is.True);
            Assert.That(source.HasTable(FontTags.Gvar), Is.True);
            Assert.That(source.HasTable(FontTags.Hvar), Is.True);
            Assert.That(source.HasTable(FontTags.Mvar), Is.True);
            Assert.That(source.HasTable(FontTags.Stat), Is.True);
            Assert.That(source.HasTable(FontTags.Vvar), Is.True);
        });

        byte[] firstBytes = source
            .CreateInstance(selectedCoordinates)
            .CopyStaticSfntBytes();
        FontFace firstFace = FontLoader.Load(
            firstBytes,
            FontLoadOptions.CreateStrict()).GetFace(0);
        FontInstance firstInstance = firstFace.CreateDefaultInstance();
        byte[] secondBytes = firstInstance.CopyStaticSfntBytes();
        FontFace secondFace = FontLoader.Load(
            secondBytes,
            FontLoadOptions.CreateStrict()).GetFace(0);
        FontInstance secondInstance = secondFace.CreateDefaultInstance();

        AssertStaticVariationTablesAbsent(firstFace);
        AssertStaticVariationTablesAbsent(secondFace);
        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyFont(firstBytes), Is.True);
            Assert.That(FontChecksum.VerifyFont(secondBytes), Is.True);
            Assert.That(firstFace.GetNamedInstanceCount(), Is.Zero);
            Assert.That(secondFace.GetNamedInstanceCount(), Is.Zero);
            Assert.That(firstInstance.GetCoordinates().GetAxisCount(), Is.Zero);
            Assert.That(secondInstance.GetCoordinates().GetAxisCount(), Is.Zero);
            Assert.That(secondInstance.GetGlyphOutline(0)
                .GetCommand(0).GetEndX(), Is.EqualTo(5));
            Assert.That(secondInstance.GetGlyphMetrics(0).GetAdvanceWidth(),
                Is.EqualTo(650));
            Assert.That(secondInstance.GetGlyphMetrics(2).GetAdvanceHeight(),
                Is.EqualTo(1070));
        });
    }

    [Test]
    public void MaterializesGvarPhantomMetricsWithoutSourceVerticalTables()
    {
        FontFace face = CreateReopenableVariableFace(false);
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        FontFace staticFace = FontLoader.Load(
            face.CreateInstance(selectedCoordinates).CopyStaticSfntBytes(),
            FontLoadOptions.CreateCompatible()).GetFace(0);
        GlyphMetrics metrics = staticFace
            .CreateDefaultInstance()
            .GetGlyphMetrics(0);

        Assert.Multiple(() =>
        {
            Assert.That(staticFace.HasTable(FontTags.Vhea), Is.True);
            Assert.That(staticFace.HasTable(FontTags.Vmtx), Is.True);
            Assert.That(metrics.GetAdvanceWidth(), Is.EqualTo(605));
            Assert.That(metrics.GetLeftSideBearing(), Is.EqualTo(13));
            Assert.That(metrics.GetAdvanceHeight(), Is.EqualTo(1015));
            Assert.That(metrics.GetTopSideBearing(), Is.EqualTo(998));
        });
    }

    [Test]
    public void MaterializesMetricOnlyTrueTypeVariationsWithoutGvar()
    {
        FontFace face = CreateReopenableVariableFace(true, false);
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        FontFace staticFace = FontLoader.Load(
            face.CreateInstance(selectedCoordinates).CopyStaticSfntBytes(),
            FontLoadOptions.CreateCompatible()).GetFace(0);
        FontInstance materialized = staticFace.CreateDefaultInstance();
        GlyphMetrics simple = materialized.GetGlyphMetrics(0);
        GlyphMetrics composite = materialized.GetGlyphMetrics(2);
        GlyphOutline compositeOutline = materialized.GetGlyphOutline(2);

        Assert.Multiple(() =>
        {
            Assert.That(staticFace.HasTable(FontTags.Gvar), Is.False);
            Assert.That(materialized.GetGlyphOutline(0)
                .GetCommand(0).GetEndX(), Is.Zero);
            Assert.That(simple.GetAdvanceWidth(), Is.EqualTo(650));
            Assert.That(simple.GetLeftSideBearing(), Is.EqualTo(15));
            Assert.That(composite.GetAdvanceWidth(), Is.EqualTo(870));
            Assert.That(composite.GetLeftSideBearing(), Is.EqualTo(35));
            Assert.That(compositeOutline.GetBounds()!.GetXMinimum(),
                Is.EqualTo(10));
            Assert.That(compositeOutline.GetBounds()!.GetYMinimum(),
                Is.EqualTo(4));
            Assert.That(compositeOutline.GetBounds()!.GetXMaximum(),
                Is.EqualTo(20));
            Assert.That(compositeOutline.GetBounds()!.GetYMaximum(),
                Is.EqualTo(9));
            Assert.That(compositeOutline.UsesInheritedMetrics(), Is.False);
            Assert.That(materialized.GetTrueTypeHintingData()!
                .CopyControlValueData(), Is.EqualTo(
                    new byte[] { 0, 105, 0, 210, 0xFF, 0x92, 0, 20 }));
        });
    }

    [Test]
    public void RetainsGvarBearingsWhenMetricVariationMapsAreAbsent()
    {
        FontFace face = CreateVariableFace(true, false);
        FontCoordinates selectedCoordinates = new FontCoordinates();
        selectedCoordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        GlyphMetrics metrics = face
            .CreateInstance(selectedCoordinates)
            .GetGlyphMetrics(0);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.GetAdvanceWidth(), Is.EqualTo(650));
            Assert.That(metrics.GetLeftSideBearing(), Is.EqualTo(13));
            Assert.That(metrics.GetAdvanceHeight(), Is.EqualTo(950));
            Assert.That(metrics.GetTopSideBearing(), Is.EqualTo(118));
            Assert.That(metrics.GetVerticalOriginY(), Is.EqualTo(130));
        });
    }

    internal static FontFace CreateFace()
    {
        byte[] simpleCurve = BuildSimpleGlyph(
            new int[] { 0, 10, 20 },
            new int[] { 0, 20, 0 },
            new bool[] { true, false, true },
            new byte[] { 0xAA, 0xBB });
        byte[] simpleLine = BuildSimpleGlyph(
            new int[] { 0, 5 },
            new int[] { 0, 0 },
            new bool[] { true, true },
            Array.Empty<byte>());
        byte[] composite = BuildComposite();
        byte[][] glyphs = new byte[][]
        {
            simpleCurve,
            simpleLine,
            composite,
            Array.Empty<byte>()
        };
        byte[] glyf = Join(glyphs);
        byte[] loca = BuildLoca(glyphs);
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Head, BuildHead());
        AddTable(tables, FontTags.Maxp, BuildMaxp());
        AddTable(tables, FontTags.Hhea, BuildHhea());
        AddTable(tables, FontTags.Hmtx, BuildHmtx());
        AddTable(tables, FontTags.Loca, loca);
        AddTable(tables, FontTags.Glyf, glyf);
        AddTable(tables, FontTags.Fpgm, new byte[] { 0xB0, 0x01, 0x2C });
        AddTable(tables, FontTags.Prep, new byte[] { 0xB0, 0x02 });
        AddTable(tables, FontTags.Cvt, new byte[] { 0x00, 0x10, 0xFF, 0xF0 });
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static FontFace CreateVariableFace()
    {
        return CreateVariableFace(false);
    }

    internal static FontFace CreateVariableFace(bool includeMetricVariations)
    {
        return CreateVariableFace(includeMetricVariations, true);
    }

    static FontFace CreateVariableFace(
        bool includeMetricVariations,
        bool includeBearingMaps)
    {
        byte[] simpleCurve = BuildSimpleGlyph(
            new int[] { 0, 10, 20 },
            new int[] { 0, 20, 0 },
            new bool[] { true, false, true },
            new byte[] { 0xAA, 0xBB });
        byte[] simpleLine = BuildSimpleGlyph(
            new int[] { 0, 5 },
            new int[] { 0, 0 },
            new bool[] { true, true },
            Array.Empty<byte>());
        byte[][] glyphs = new byte[][]
        {
            simpleCurve,
            simpleLine,
            BuildComposite(),
            Array.Empty<byte>()
        };
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Head, BuildHead());
        AddTable(tables, FontTags.Maxp, BuildMaxp());
        AddTable(tables, FontTags.Hhea, BuildHhea());
        AddTable(tables, FontTags.Hmtx, BuildHmtx());
        AddTable(tables, FontTags.Loca, BuildLoca(glyphs));
        AddTable(tables, FontTags.Glyf, Join(glyphs));
        AddTable(tables, FontTags.Name, BuildVariationName());
        AddTable(tables, FontTags.Fvar, BuildVariationFvar());
        AddTable(tables, FontTags.Avar, BuildIdentityAvar());
        AddTable(tables, FontTags.Gvar, BuildVariationGvar());
        AddTable(tables, FontTags.Mvar, BuildEmptyMvar());
        AddTable(tables, FontTags.Fpgm, new byte[] { 0xB0, 0x01, 0x2C });
        AddTable(tables, FontTags.Prep, new byte[] { 0xB0, 0x02 });
        AddTable(tables, FontTags.Cvt,
            new byte[] { 0, 100, 0, 200, 0xFF, 0x9C, 0, 0 });
        AddTable(tables, FontTags.Cvar, BuildVariationCvar());

        if (includeMetricVariations)
        {
            AddTable(tables, FontTags.Vhea, BuildVhea());
            AddTable(tables, FontTags.Vmtx, BuildVmtx());
            AddTable(tables, FontTags.Hvar,
                BuildMetricVariations(false, includeBearingMaps));
            AddTable(tables, FontTags.Vvar,
                BuildMetricVariations(true, includeBearingMaps));
        }

        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static FontFace CreateReopenableVariableFace()
    {
        return CreateReopenableVariableFace(true, true);
    }

    static FontFace CreateReopenableVariableFace(bool includeMetricVariations)
    {
        return CreateReopenableVariableFace(includeMetricVariations, true);
    }

    static FontFace CreateReopenableVariableFace(
        bool includeMetricVariations,
        bool includeGvar)
    {
        FontFace source = CreateVariableFace(includeMetricVariations);
        SfntTableStore sourceTables = source.GetTableStore();
        List<SfntTableData> tables = new List<SfntTableData>();
        int index = 0;

        while (index < sourceTables.GetTableCount())
        {
            SfntTableData table = sourceTables.GetTable(index);

            if (includeGvar || table.GetTag() != FontTags.Gvar)
            {
                tables.Add(table);
            }

            index += 1;
        }

        AddTable(tables, FontTags.Cmap, new byte[4]);
        AddTable(tables, FontTags.Os2, BuildStaticOs2());
        AddTable(tables, FontTags.Post, new byte[4]);
        AddTable(tables, FontTags.Dsig, new byte[8]);
        AddTable(tables, FontTags.Stat, new byte[8]);
        AddTable(tables, FontTags.Gsub, BuildStaticFeatureVariationsLayout());
        AddTable(tables, FontTags.Gpos, BuildStaticFeatureVariationsLayout());
        AddTable(tables, (int)TestSfntEncoding.EncodeTag("ZZZZ"),
            new byte[] { 1, 2, 3 });
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            source.GetScalerType(),
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static byte[] BuildVariationName()
    {
        int[] nameIds = new int[] { 1, 2, 4, 6, 17, 256, 257, 258 };
        string[] values = new string[]
        {
            "Demo", "Regular", "Demo Regular", "Demo-Regular", "Regular",
            "Weight", "Medium", "Demo-Medium"
        };
        byte[][] encoded = values.Select(
            System.Text.Encoding.BigEndianUnicode.GetBytes).ToArray();
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)values.Length)
            .WriteUInt16((ushort)(6 + values.Length * 12));
        int offset = 0;

        for (int index = 0; index < values.Length; index += 1)
        {
            builder.WriteUInt16(3).WriteUInt16(1).WriteUInt16(0x0409)
                .WriteUInt16((ushort)nameIds[index])
                .WriteUInt16((ushort)encoded[index].Length)
                .WriteUInt16((ushort)offset);
            offset += encoded[index].Length;
        }

        foreach (byte[] value in encoded) { builder.WriteBytes(value); }
        return builder.CopyBytes();
    }

    static byte[] BuildVariationFvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(16)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(20)
            .WriteUInt16(1)
            .WriteUInt16(10)
            .WriteTag("wght")
            .WriteInt32(0)
            .WriteInt32(0)
            .WriteInt32(0x00010000)
            .WriteUInt16(0)
            .WriteUInt16(256)
            .WriteUInt16(257)
            .WriteUInt16(0)
            .WriteInt32(0x00008000)
            .WriteUInt16(258)
            .CopyBytes();
    }

    static byte[] BuildIdentityAvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(1)
            .WriteUInt16(3)
            .WriteInt16(-0x4000).WriteInt16(-0x4000)
            .WriteInt16(0).WriteInt16(0)
            .WriteInt16(0x4000).WriteInt16(0x4000)
            .CopyBytes();
    }

    static byte[] BuildEmptyMvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(8)
            .WriteUInt16(0).WriteUInt16(0)
            .CopyBytes();
    }

    static void AssertStaticVariationTablesAbsent(FontFace face)
    {
        Assert.Multiple(() =>
        {
            Assert.That(face.HasTable(FontTags.Avar), Is.False);
            Assert.That(face.HasTable(FontTags.Cvar), Is.False);
            Assert.That(face.HasTable(FontTags.Fvar), Is.False);
            Assert.That(face.HasTable(FontTags.Gvar), Is.False);
            Assert.That(face.HasTable(FontTags.Hvar), Is.False);
            Assert.That(face.HasTable(FontTags.Mvar), Is.False);
            Assert.That(face.HasTable(FontTags.Stat), Is.False);
            Assert.That(face.HasTable(FontTags.Vvar), Is.False);
        });
    }

    static byte[] BuildVariationGvar()
    {
        byte[][] glyphData = new byte[][]
        {
            BuildGlyphVariation(
                new int[] { 10, 20, 30, 5, 15, 0, 0 },
                new int[] { 0, 10, 0, 0, 0, 20, -10 }),
            BuildGlyphVariation(
                new int[] { 0, 10, 20, 40, 0, 0 },
                new int[] { 0, 0, 0, 0, 0, 0 }),
            BuildGlyphVariation(
                new int[] { 20, 99, 80, 100, 0, 0 },
                new int[] { 10, 99, 0, 0, 0, 0 }),
            BuildGlyphVariation(
                new int[] { 10, 20, 0, 0 },
                new int[] { 0, 0, 30, -10 })
        };
        int sharedTupleOffset = 40;
        int glyphDataOffset = 42;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt32((uint)sharedTupleOffset)
            .WriteUInt16(4)
            .WriteUInt16(1)
            .WriteUInt32((uint)glyphDataOffset);
        int offset = 0;
        builder.WriteUInt32(0);

        for (int index = 0; index < glyphData.Length; index += 1)
        {
            offset += glyphData[index].Length;
            builder.WriteUInt32((uint)offset);
        }

        builder.WriteInt16(0x4000);

        for (int index = 0; index < glyphData.Length; index += 1)
        {
            builder.WriteBytes(glyphData[index]);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildVariationCvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(14)
            .WriteUInt16(5)
            .WriteUInt16(0x8000)
            .WriteInt16(0x4000)
            .WriteUInt8(3)
            .WriteInt8(10)
            .WriteInt8(20)
            .WriteInt8(-20)
            .WriteInt8(40)
            .CopyBytes();
    }

    static byte[] BuildMetricVariations(
        bool vertical,
        bool includeBearingMaps)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0);

        if (vertical)
        {
            builder.WriteUInt32(0);
        }

        int advanceOffset = builder.GetLength();
        WriteMetricMap(builder, new byte[] { 0, 1, 2, 3 });
        int leadingOffset = 0;
        int trailingOffset = 0;

        if (includeBearingMaps)
        {
            leadingOffset = builder.GetLength();
            WriteMetricMap(builder, new byte[] { 4, 5, 6, 7 });
            trailingOffset = builder.GetLength();
            WriteMetricMap(builder, new byte[] { 8, 9, 10, 11 });
        }

        int originOffset = 0;

        if (vertical)
        {
            originOffset = builder.GetLength();
            WriteMetricMap(builder, new byte[] { 8, 9, 10, 11 });
        }

        int storeOffset = builder.GetLength();
        WriteMetricVariationStore(builder);
        byte[] result = builder.CopyBytes();
        PatchUInt32(result, 4, (uint)storeOffset);
        PatchUInt32(result, 8, (uint)advanceOffset);
        PatchUInt32(result, 12, (uint)leadingOffset);
        PatchUInt32(result, 16, (uint)trailingOffset);

        if (vertical)
        {
            PatchUInt32(result, 20, (uint)originOffset);
        }

        return result;
    }

    static void WriteMetricMap(
        TestBinaryBuilder builder,
        byte[] innerIndices)
    {
        builder
            .WriteUInt8(0)
            .WriteUInt8(3)
            .WriteUInt16((ushort)innerIndices.Length)
            .WriteBytes(innerIndices);
    }

    static void WriteMetricVariationStore(TestBinaryBuilder builder)
    {
        int[] deltas = new int[]
        {
            100, 120, 140, 160,
            10, 20, 30, 40,
            200, 220, 240, 260
        };
        builder
            .WriteUInt16(1)
            .WriteUInt32(12)
            .WriteUInt16(1)
            .WriteUInt32(22)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteInt16(0)
            .WriteInt16(0x4000)
            .WriteInt16(0x4000)
            .WriteUInt16((ushort)deltas.Length)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(0);

        int index = 0;

        while (index < deltas.Length)
        {
            builder.WriteInt16((short)deltas[index]);
            index += 1;
        }
    }

    static byte[] BuildGlyphVariation(int[] xDeltas, int[] yDeltas)
    {
        TestBinaryBuilder serialized = new TestBinaryBuilder();
        WritePackedByteDeltas(serialized, xDeltas);
        WritePackedByteDeltas(serialized, yDeltas);
        byte[] data = serialized.CopyBytes();
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(8)
            .WriteUInt16((ushort)data.Length)
            .WriteUInt16(0)
            .WriteBytes(data)
            .CopyBytes();
    }

    static void WritePackedByteDeltas(TestBinaryBuilder builder, int[] deltas)
    {
        builder.WriteUInt8((byte)(deltas.Length - 1));

        for (int index = 0; index < deltas.Length; index += 1)
        {
            builder.WriteInt8((sbyte)deltas[index]);
        }
    }

    static byte[] BuildSimpleGlyph(
        int[] xs,
        int[] ys,
        bool[] onCurve,
        byte[] instructions)
    {
        int xMinimum = xs[0];
        int yMinimum = ys[0];
        int xMaximum = xs[0];
        int yMaximum = ys[0];

        for (int index = 1; index < xs.Length; index += 1)
        {
            xMinimum = Math.Min(xMinimum, xs[index]);
            yMinimum = Math.Min(yMinimum, ys[index]);
            xMaximum = Math.Max(xMaximum, xs[index]);
            yMaximum = Math.Max(yMaximum, ys[index]);
        }

        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteInt16(1)
            .WriteInt16((short)xMinimum)
            .WriteInt16((short)yMinimum)
            .WriteInt16((short)xMaximum)
            .WriteInt16((short)yMaximum)
            .WriteUInt16((ushort)(xs.Length - 1))
            .WriteUInt16((ushort)instructions.Length)
            .WriteBytes(instructions);

        for (int index = 0; index < xs.Length; index += 1)
        {
            builder.WriteUInt8(onCurve[index] ? (byte)1 : (byte)0);
        }

        int previous = 0;

        for (int index = 0; index < xs.Length; index += 1)
        {
            builder.WriteInt16((short)(xs[index] - previous));
            previous = xs[index];
        }

        previous = 0;

        for (int index = 0; index < ys.Length; index += 1)
        {
            builder.WriteInt16((short)(ys[index] - previous));
            previous = ys[index];
        }

        if ((builder.GetLength() & 1) != 0)
        {
            builder.WriteUInt8(0);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildComposite()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteInt16(-1)
            .WriteInt16(10)
            .WriteInt16(4)
            .WriteInt16(20)
            .WriteInt16(9)
            .WriteUInt16(0x042B)
            .WriteUInt16(0)
            .WriteInt16(10)
            .WriteInt16(4)
            .WriteInt16(0x2000)
            .WriteUInt16(0x0301)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt8(0xCC);

        if ((builder.GetLength() & 1) != 0)
        {
            builder.WriteUInt8(0);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildHead()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt32(0x00010000U).WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U).WriteUInt16(0).WriteUInt16(1000)
            .WriteInt64(0).WriteInt64(0)
            .WriteInt16(0).WriteInt16(0).WriteInt16(1000).WriteInt16(1000)
            .WriteUInt16(0).WriteUInt16(8).WriteInt16(2).WriteInt16(0).WriteInt16(0)
            .CopyBytes();
    }

    static byte[] BuildStaticOs2()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(5).WriteInt16(500).WriteUInt16(400).WriteUInt16(5)
            .WriteUInt16(0)
            .WriteInt16(650).WriteInt16(600).WriteInt16(10).WriteInt16(140)
            .WriteInt16(700).WriteInt16(650).WriteInt16(20).WriteInt16(480)
            .WriteInt16(50).WriteInt16(300).WriteInt16(0);
        builder.WriteZeros(10);
        return builder
            .WriteUInt32(0).WriteUInt32(0).WriteUInt32(0).WriteUInt32(0)
            .WriteUInt32(0x54455354).WriteUInt16(0x0040)
            .WriteUInt16(0).WriteUInt16(0xFFFF)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(20)
            .WriteUInt16(900).WriteUInt16(250)
            .WriteUInt32(0).WriteUInt32(0)
            .WriteInt16(450).WriteInt16(700)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(160).WriteUInt16(720)
            .CopyBytes();
    }

    static byte[] BuildStaticFeatureVariationsLayout()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00010001U)
            .WriteUInt16(14).WriteUInt16(16).WriteUInt16(30).WriteUInt32(48)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteTag("kern").WriteUInt16(8)
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(2).WriteUInt16(6).WriteUInt16(12)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt32(1)
            .WriteUInt32(16).WriteUInt32(30)
            .WriteUInt16(1).WriteUInt32(6)
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(0x1000).WriteInt16(0x3000)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(1)
            .WriteUInt16(0).WriteUInt32(12)
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(1)
            .CopyBytes();
    }

    static int ReadLayoutFeatureLookup(byte[] data)
    {
        int featureList = ReadUInt16(data, 6);
        int feature = featureList + ReadUInt16(data, featureList + 6);
        return ReadUInt16(data, feature + 4);
    }

    static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] << 8 | data[offset + 1];
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)data[offset] << 24 |
            (uint)data[offset + 1] << 16 |
            (uint)data[offset + 2] << 8 |
            data[offset + 3];
    }

    static byte[] BuildMaxp()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16(4)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt16(5)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .CopyBytes();
    }

    static byte[] BuildHhea()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(750).WriteInt16(-250).WriteInt16(50)
            .WriteUInt16(1000).WriteInt16(0).WriteInt16(0).WriteInt16(1000)
            .WriteInt16(1).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16(3)
            .CopyBytes();
    }

    static byte[] BuildHmtx()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(600).WriteInt16(10)
            .WriteUInt16(800).WriteInt16(20)
            .WriteUInt16(1000).WriteInt16(30)
            .WriteInt16(40)
            .CopyBytes();
    }

    static byte[] BuildVhea()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(0)
            .WriteUInt16(1200).WriteInt16(0).WriteInt16(0).WriteInt16(1000)
            .WriteInt16(1).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteUInt16(4)
            .CopyBytes();
    }

    static byte[] BuildVmtx()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(900).WriteInt16(100)
            .WriteUInt16(1000).WriteInt16(200)
            .WriteUInt16(1100).WriteInt16(300)
            .WriteUInt16(1200).WriteInt16(400)
            .CopyBytes();
    }

    static byte[] BuildLoca(byte[][] glyphs)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();
        int offset = 0;
        builder.WriteUInt16(0);

        foreach (byte[] glyph in glyphs)
        {
            offset += glyph.Length;
            builder.WriteUInt16((ushort)(offset / 2));
        }

        return builder.CopyBytes();
    }

    static byte[] Join(byte[][] glyphs)
    {
        int length = 0;

        foreach (byte[] glyph in glyphs)
        {
            length += glyph.Length;
        }

        byte[] result = new byte[length];
        int offset = 0;

        foreach (byte[] glyph in glyphs)
        {
            glyph.CopyTo(result, offset);
            offset += glyph.Length;
        }

        return result;
    }

    static void AddTable(
        List<SfntTableData> tables,
        int tag,
        byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static void PatchUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
