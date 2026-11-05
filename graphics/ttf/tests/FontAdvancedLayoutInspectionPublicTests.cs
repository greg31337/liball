using ttf;

namespace ttf.Tests;

internal class FontAdvancedLayoutInspectionPublicTests
{
    [Test]
    public void ExposesGdefClassesAttachmentsCaretsAndMarkSets()
    {
        FontFace face = CreateFace();
        FontAdvancedLayoutInspection inspection =
            face.GetAdvancedLayoutInspection();
        FontGlyphDefinition gdef = inspection.GetGlyphDefinition()!;
        FontGlyphCaret coordinate = gdef.GetLigatureCaret(4, 0);
        FontGlyphCaret point = gdef.GetLigatureCaret(4, 1);
        FontGlyphCaret adjusted = gdef.GetLigatureCaret(4, 2);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetAdvancedLayoutInspection(),
                Is.SameAs(inspection));
            Assert.That(gdef.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(gdef.GetMinorVersion(), Is.EqualTo(3));
            Assert.That(gdef.GetGlyphClass(1), Is.EqualTo(1));
            Assert.That(gdef.GetGlyphClass(4), Is.EqualTo(2));
            Assert.That(gdef.GetGlyphClass(6), Is.EqualTo(3));
            Assert.That(gdef.GetGlyphClass(8), Is.EqualTo(4));
            Assert.That(gdef.GetMarkAttachmentClass(7), Is.EqualTo(2));
            Assert.That(gdef.GetAttachmentGlyphCount(), Is.EqualTo(2));
            Assert.That(gdef.GetAttachmentGlyphId(0), Is.EqualTo(1));
            Assert.That(gdef.GetAttachmentPointCount(1), Is.EqualTo(2));
            Assert.That(gdef.GetAttachmentPoint(1, 1), Is.EqualTo(5));
            Assert.That(gdef.GetAttachmentPointCount(2), Is.Zero);
            Assert.That(gdef.GetLigatureGlyphCount(), Is.EqualTo(1));
            Assert.That(gdef.GetLigatureGlyphId(0), Is.EqualTo(4));
            Assert.That(gdef.GetLigatureCaretCount(4), Is.EqualTo(3));
            Assert.That(coordinate.GetFormat(), Is.EqualTo(1));
            Assert.That(coordinate.GetCoordinate(), Is.EqualTo(-40));
            Assert.That(point.HasContourPointIndex(), Is.True);
            Assert.That(point.GetContourPointIndex(), Is.EqualTo(9));
            Assert.That(adjusted.GetCoordinate(), Is.EqualTo(120));
            Assert.That(adjusted.HasAdjustment(), Is.True);
            Assert.That(adjusted.IsVariationAdjustment(), Is.True);
            Assert.That(gdef.GetMarkGlyphSetCount(), Is.EqualTo(2));
            Assert.That(gdef.MarkGlyphSetContains(0, 7), Is.True);
            Assert.That(gdef.MarkGlyphSetContains(1, 9), Is.True);
            Assert.That(gdef.MarkGlyphSetContains(1, 7), Is.False);
            Assert.That(gdef.HasItemVariationStore(), Is.True);
            Assert.Throws<ArgumentException>(() =>
                gdef.GetLigatureCaret(3, 0));
        });
    }

    [Test]
    public void ExtendsBaseInspectionWithScriptLanguageAndFeatureExtents()
    {
        FontBaselines baselines = CreateFace()
            .GetAdvancedLayoutInspection()
            .GetBaselines()!;
        FontBaselineScript latin = baselines.GetHorizontalAxis()!
            .FindScript(Tag("latn"))!;
        FontBaselineMinMax defaults = latin.GetDefaultMinMax()!;
        FontBaselineFeatureMinMax subs = defaults.FindFeature(Tag("subs"))!;
        FontBaselineLanguageSystem english =
            latin.FindLanguageSystem(Tag("ENG "))!;

        Assert.Multiple(() =>
        {
            Assert.That(latin.GetDefaultBaselineTag(), Is.EqualTo(Tag("romn")));
            Assert.That(defaults.GetMinimum()!.GetCoordinate(), Is.EqualTo(-250));
            Assert.That(defaults.GetMaximum()!.GetCoordinate(), Is.EqualTo(800));
            Assert.That(defaults.GetMaximum()!.HasAdjustment(), Is.True);
            Assert.That(defaults.GetMaximum()!.IsDeviceAdjustment(), Is.True);
            Assert.That(defaults.GetFeatureCount(), Is.EqualTo(1));
            Assert.That(defaults.GetFeature(0).GetFeatureTag(),
                Is.EqualTo(Tag("subs")));
            Assert.That(subs.GetMinimum()!.GetCoordinate(), Is.EqualTo(-300));
            Assert.That(subs.GetMaximum(), Is.Null);
            Assert.That(latin.GetLanguageSystemCount(), Is.EqualTo(1));
            Assert.That(english.GetLanguageTag(), Is.EqualTo(Tag("ENG ")));
            Assert.That(english.GetMinMax().GetMinimum()!.GetCoordinate(),
                Is.EqualTo(-260));
            Assert.That(english.GetMinMax().GetMaximum()!.GetCoordinate(),
                Is.EqualTo(810));
            Assert.That(defaults.FindFeature(Tag("liga")), Is.Null);
        });
    }

    [Test]
    public void ExposesJstfScriptsPrioritiesAndLookupActions()
    {
        FontJustification jstf = CreateFace()
            .GetAdvancedLayoutInspection()
            .GetJustification()!;
        FontJustificationScript arab = jstf.FindScript(Tag("arab"))!;
        FontJustificationLanguageSystem defaults =
            arab.GetDefaultLanguageSystem()!;
        FontJustificationPriority first = defaults.GetPriority(0);
        FontJustificationOpportunity shrinkage = first.GetShrinkage();
        FontJustificationOpportunity extension = first.GetExtension();

        Assert.Multiple(() =>
        {
            Assert.That(jstf.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(jstf.GetScriptCount(), Is.EqualTo(1));
            Assert.That(jstf.GetScript(0).GetTag(), Is.EqualTo(Tag("arab")));
            Assert.That(arab.GetExtenderGlyphCount(), Is.EqualTo(2));
            Assert.That(arab.GetExtenderGlyphId(1), Is.EqualTo(5));
            Assert.That(defaults.GetTag(), Is.Zero);
            Assert.That(defaults.GetPriorityCount(), Is.EqualTo(2));
            Assert.That(shrinkage.IsShrinkage(), Is.True);
            Assert.That(shrinkage.HasActions(), Is.True);
            Assert.That(shrinkage.GetSubstitutionEnableCount(), Is.EqualTo(1));
            Assert.That(shrinkage.GetSubstitutionEnableLookup(0), Is.Zero);
            Assert.That(extension.IsExtension(), Is.True);
            Assert.That(extension.GetSubstitutionDisableLookup(0), Is.Zero);
            Assert.That(extension.GetPositioningEnableLookup(0), Is.Zero);
            Assert.That(extension.GetMaximumLookupCount(), Is.EqualTo(1));
            Assert.That(extension.GetMaximumLookup(0).GetEffectiveType(),
                Is.EqualTo(1));
            Assert.That(arab.GetLanguageSystemCount(), Is.EqualTo(1));
            Assert.That(arab.FindLanguageSystem(Tag("FARS"))!
                .GetPriorityCount(), Is.EqualTo(1));
            Assert.That(jstf.FindScript(Tag("latn")), Is.Null);
        });
    }

    [Test]
    public void ExposesMathConstantsVariantsConstructionsAndAssemblies()
    {
        FontMath math = CreateFace()
            .GetAdvancedLayoutInspection()
            .GetMath()!;
        FontMathConstruction vertical = math.FindVerticalConstruction(6)!;
        FontMathConstruction horizontal = math.FindHorizontalConstruction(7)!;
        FontMathAssembly assembly = vertical.GetAssembly()!;
        FontMathPart extender = assembly.GetPart(1);

        Assert.Multiple(() =>
        {
            Assert.That(math.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(math.GetScriptPercentScaleDown(), Is.EqualTo(80));
            Assert.That(math.GetScriptScriptPercentScaleDown(), Is.EqualTo(60));
            Assert.That(math.GetDelimitedSubFormulaMinimumHeight(),
                Is.EqualTo(1500));
            Assert.That(math.GetDisplayOperatorMinimumHeight(), Is.EqualTo(1300));
            Assert.That(math.GetConstantCount(), Is.EqualTo(51));
            Assert.That(math.GetConstant(0).GetValue(), Is.EqualTo(-25));
            Assert.That(math.GetConstant(1).GetValue(), Is.EqualTo(-24));
            Assert.That(math.GetConstant(1).HasAdjustment(), Is.True);
            Assert.That(math.GetConstant(1).IsDeviceAdjustment(), Is.True);
            Assert.That(math.GetRadicalDegreeBottomRaisePercent(), Is.EqualTo(60));
            Assert.That(math.FindItalicsCorrection(2)!.GetValue(), Is.EqualTo(30));
            Assert.That(math.FindTopAccentAttachment(3)!.GetValue(), Is.EqualTo(200));
            Assert.That(math.IsExtendedShape(5), Is.True);
            Assert.That(math.GetMinimumConnectorOverlap(), Is.EqualTo(20));
            Assert.That(math.GetVerticalConstructionCount(), Is.EqualTo(1));
            Assert.That(math.GetVerticalConstructionGlyphId(0), Is.EqualTo(6));
            Assert.That(vertical.GetVariantCount(), Is.EqualTo(2));
            Assert.That(vertical.GetVariant(0).GetGlyphId(), Is.EqualTo(8));
            Assert.That(vertical.GetVariant(1).GetAdvanceMeasurement(),
                Is.EqualTo(700));
            Assert.That(horizontal.GetAssembly(), Is.Null);
            Assert.That(math.GetHorizontalConstructionGlyphId(0), Is.EqualTo(7));
            Assert.That(assembly.GetItalicsCorrection().GetValue(), Is.EqualTo(15));
            Assert.That(assembly.GetPartCount(), Is.EqualTo(2));
            Assert.That(extender.GetGlyphId(), Is.EqualTo(7));
            Assert.That(extender.GetStartConnectorLength(), Is.EqualTo(20));
            Assert.That(extender.GetEndConnectorLength(), Is.Zero);
            Assert.That(extender.GetFullAdvance(), Is.EqualTo(150));
            Assert.That(extender.IsExtender(), Is.True);
            Assert.That(math.FindVerticalConstruction(7), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => math.GetConstant(51));
        });
    }

    static FontFace CreateFace()
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U).WriteUInt16(12).CopyBytes());
        AddTable(tables, FontTags.Name, SfntGdefTableTests.BuildName());
        AddTable(tables, FontTags.Fvar, SfntGdefTableTests.BuildFvar());
        AddTable(tables, FontTags.Gdef, SfntGdefTableTests.BuildVersion13());
        AddTable(tables, FontTags.Base, SfntBaseTableTests.BuildVersion10());
        AddTable(tables, FontTags.Gsub, SfntJstfTableTests.BuildLayout(false));
        AddTable(tables, FontTags.Gpos, SfntJstfTableTests.BuildLayout(true));
        AddTable(tables, FontTags.Jstf, SfntJstfTableTests.BuildJstf());
        AddTable(tables, FontTags.Math, SfntMathTableTests.BuildMath());
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static void AddTable(List<SfntTableData> tables, int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
