using ttf;

namespace ttf.Tests;

internal class EmbeddingRightsTests
{
    [TestCase(0, SfntEmbeddingPermission.Installable)]
    [TestCase(2, SfntEmbeddingPermission.Restricted)]
    [TestCase(4, SfntEmbeddingPermission.PreviewAndPrint)]
    [TestCase(8, SfntEmbeddingPermission.Editable)]
    public void InterpretsEveryEmbeddingPermission(
        int flags,
        SfntEmbeddingPermission expected)
    {
        SfntEmbeddingRightsReport report = Interpret(5, flags);

        Assert.Multiple(() =>
        {
            Assert.That(report.GetTableVersion(), Is.EqualTo(5));
            Assert.That(report.GetRawFlags(), Is.EqualTo(flags));
            Assert.That(report.GetPermissionFlags(), Is.EqualTo(flags));
            Assert.That(report.GetPermission(), Is.EqualTo(expected));
            Assert.That(report.GetDeclaredPermissionCount(),
                Is.EqualTo(flags == 0 ? 0 : 1));
            Assert.That(report.UsesLegacyLeastRestrictivePrecedence(), Is.False);
            Assert.That(report.GetIgnoredFlags(), Is.Zero);
        });
    }

    [Test]
    public void ReportsInstallableRightsWithoutRestrictions()
    {
        SfntEmbeddingRightsReport report = Interpret(5, 0);

        Assert.Multiple(() =>
        {
            Assert.That(report.AllowsEmbedding(), Is.True);
            Assert.That(report.AllowsPermanentInstallation(), Is.True);
            Assert.That(report.AllowsDocumentEditing(), Is.True);
            Assert.That(report.RequiresReadOnlyDocuments(), Is.False);
            Assert.That(report.RequiresTemporaryLoading(), Is.False);
            Assert.That(report.AllowsSubsetting(), Is.True);
            Assert.That(report.RequiresFullEmbedding(), Is.False);
            Assert.That(report.AllowsOutlineEmbedding(), Is.True);
            Assert.That(report.AllowsBitmapEmbedding(), Is.True);
            Assert.That(report.RequiresBitmapOnlyEmbedding(), Is.False);
            Assert.That(report.GetRecommendedAction(),
                Is.EqualTo(SfntEmbeddingRecommendedAction.EmbedSubsetOrFullFont));
            Assert.That(report.GetRestrictionDiagnosticCount(), Is.Zero);
        });
    }

    [Test]
    public void ReportsRestrictedRightsWithoutGrantingEmbeddingPermission()
    {
        SfntEmbeddingRightsReport report = Interpret(5, 2);

        Assert.Multiple(() =>
        {
            Assert.That(report.DeclaresRestrictedPermission(), Is.True);
            Assert.That(report.DeclaresPreviewAndPrintPermission(), Is.False);
            Assert.That(report.DeclaresEditablePermission(), Is.False);
            Assert.That(report.AllowsEmbedding(), Is.False);
            Assert.That(report.AllowsPermanentInstallation(), Is.False);
            Assert.That(report.AllowsDocumentEditing(), Is.False);
            Assert.That(report.RequiresReadOnlyDocuments(), Is.False);
            Assert.That(report.RequiresTemporaryLoading(), Is.False);
            Assert.That(report.AllowsSubsetting(), Is.False);
            Assert.That(report.AllowsOutlineEmbedding(), Is.False);
            Assert.That(report.AllowsBitmapEmbedding(), Is.False);
            Assert.That(report.GetRecommendedAction(), Is.EqualTo(
                SfntEmbeddingRecommendedAction.DoNotEmbedWithoutExplicitPermission));
            Assert.That(report.GetRestrictionDiagnosticCount(), Is.EqualTo(1));
            Assert.That(report.GetRestrictionDiagnosticCode(0),
                Is.EqualTo(FontDiagnosticCode.EmbeddingRestricted));
        });
    }

    [Test]
    public void ReportsPreviewAndPrintAsTemporaryReadOnlyEmbedding()
    {
        SfntEmbeddingRightsReport report = Interpret(5, 4);

        Assert.Multiple(() =>
        {
            Assert.That(report.DeclaresPreviewAndPrintPermission(), Is.True);
            Assert.That(report.AllowsEmbedding(), Is.True);
            Assert.That(report.AllowsPermanentInstallation(), Is.False);
            Assert.That(report.AllowsDocumentEditing(), Is.False);
            Assert.That(report.RequiresReadOnlyDocuments(), Is.True);
            Assert.That(report.RequiresTemporaryLoading(), Is.True);
        });
    }

    [Test]
    public void ReportsEditableAsTemporaryEditableEmbedding()
    {
        SfntEmbeddingRightsReport report = Interpret(5, 8);

        Assert.Multiple(() =>
        {
            Assert.That(report.DeclaresEditablePermission(), Is.True);
            Assert.That(report.AllowsEmbedding(), Is.True);
            Assert.That(report.AllowsPermanentInstallation(), Is.False);
            Assert.That(report.AllowsDocumentEditing(), Is.True);
            Assert.That(report.RequiresReadOnlyDocuments(), Is.False);
            Assert.That(report.RequiresTemporaryLoading(), Is.True);
        });
    }

    [TestCase(2 | 4, SfntEmbeddingPermission.PreviewAndPrint)]
    [TestCase(2 | 8, SfntEmbeddingPermission.Editable)]
    [TestCase(4 | 8, SfntEmbeddingPermission.Editable)]
    [TestCase(2 | 4 | 8, SfntEmbeddingPermission.Editable)]
    public void AppliesLeastRestrictivePrecedenceToLegacyCombinedPermissions(
        int flags,
        SfntEmbeddingPermission expected)
    {
        SfntEmbeddingRightsReport report = Interpret(2, flags);

        Assert.Multiple(() =>
        {
            Assert.That(report.GetPermission(), Is.EqualTo(expected));
            Assert.That(report.GetDeclaredPermissionCount(),
                Is.EqualTo(CountPermissionBits(flags)));
            Assert.That(report.UsesLegacyLeastRestrictivePrecedence(), Is.True);
        });
    }

    [Test]
    public void IgnoresLaterRestrictionBitsInVersionsZeroAndOne()
    {
        SfntEmbeddingRightsReport version0 = Interpret(0, 0x0300);
        SfntEmbeddingRightsReport version1 = Interpret(1, 0x0300);

        Assert.Multiple(() =>
        {
            Assert.That(version0.GetIgnoredFlags(), Is.EqualTo(0x0300));
            Assert.That(version1.GetIgnoredFlags(), Is.EqualTo(0x0300));
            Assert.That(version0.RequiresFullEmbedding(), Is.False);
            Assert.That(version0.RequiresBitmapOnlyEmbedding(), Is.False);
            Assert.That(version0.GetRecommendedAction(),
                Is.EqualTo(SfntEmbeddingRecommendedAction.EmbedSubsetOrFullFont));
            Assert.That(version1.GetRestrictionDiagnosticCount(), Is.Zero);
        });
    }

    [TestCase(0x0100, SfntEmbeddingRecommendedAction.EmbedFullFont)]
    [TestCase(0x0200, SfntEmbeddingRecommendedAction.EmbedBitmapDataOnly)]
    [TestCase(0x0300, SfntEmbeddingRecommendedAction.EmbedCompleteBitmapDataOnly)]
    public void ReportsIndependentSubsettingAndBitmapConstraints(
        int flags,
        SfntEmbeddingRecommendedAction expectedAction)
    {
        SfntEmbeddingRightsReport report = Interpret(2, flags);
        bool noSubsetting = (flags & 0x0100) != 0;
        bool bitmapOnly = (flags & 0x0200) != 0;

        Assert.Multiple(() =>
        {
            Assert.That(report.AllowsSubsetting(), Is.EqualTo(!noSubsetting));
            Assert.That(report.RequiresFullEmbedding(), Is.EqualTo(noSubsetting));
            Assert.That(report.AllowsOutlineEmbedding(), Is.EqualTo(!bitmapOnly));
            Assert.That(report.AllowsBitmapEmbedding(), Is.True);
            Assert.That(report.RequiresBitmapOnlyEmbedding(), Is.EqualTo(bitmapOnly));
            Assert.That(report.GetRecommendedAction(), Is.EqualTo(expectedAction));
            Assert.That(report.GetRestrictionDiagnosticCount(),
                Is.EqualTo((noSubsetting ? 1 : 0) + (bitmapOnly ? 1 : 0)));
        });
    }

    [Test]
    public void RestrictedPermissionTakesPriorityOverOtherConstraints()
    {
        SfntEmbeddingRightsReport report = Interpret(5, 2 | 0x0100 | 0x0200);

        Assert.Multiple(() =>
        {
            Assert.That(report.GetRecommendedAction(), Is.EqualTo(
                SfntEmbeddingRecommendedAction.DoNotEmbedWithoutExplicitPermission));
            Assert.That(report.GetRestrictionDiagnosticCount(), Is.EqualTo(3));
            Assert.That(report.GetRestrictionDiagnosticCode(0),
                Is.EqualTo(FontDiagnosticCode.EmbeddingRestricted));
            Assert.That(report.GetRestrictionDiagnosticCode(1),
                Is.EqualTo(FontDiagnosticCode.SubsettingRestricted));
            Assert.That(report.GetRestrictionDiagnosticCode(2),
                Is.EqualTo(FontDiagnosticCode.BitmapEmbeddingOnly));
        });
    }

    [Test]
    public void RejectsInvalidInterpreterArgumentsAndModernCombinedPermissions()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Interpret(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Interpret(5, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Interpret(5, 65536));
            Assert.Throws<FontFormatException>(() => Interpret(5, 1));
            Assert.Throws<FontFormatException>(() => Interpret(3, 2 | 4));
        });
    }

    [Test]
    public void DiagnosticAccessRejectsIndexesOutsideTheReport()
    {
        SfntEmbeddingRightsReport unrestricted = Interpret(5, 0);
        SfntEmbeddingRightsReport restricted = Interpret(5, 2);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => unrestricted.GetRestrictionDiagnosticCode(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => restricted.GetRestrictionDiagnosticCode(-1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => restricted.GetRestrictionDiagnosticCode(1));
        });
    }

    [Test]
    public void ParsedTableAndFaceCacheTheSameRightsReport()
    {
        byte[] os2 = BuildOs2(4 | 0x0100);
        SfntTableCache cache = CreateCache(os2, BuildHead());
        SfntOs2Table? table = SfntOs2TableParser.FindTable(cache);
        SfntValidationResult validation = new SfntValidationResult(
            FontOutlineKind.Cff1,
            Array.Empty<FontDiagnostic>());
        FontFace face = new FontFace(0, 0x4F54544FU, validation, cache);
        SfntEmbeddingRightsReport report = face.GetSfntEmbeddingRights();

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetEmbeddingRights(), Is.SameAs(report));
            Assert.That(face.GetSfntEmbeddingRights(), Is.SameAs(report));
            Assert.That(report.GetPermission(),
                Is.EqualTo(SfntEmbeddingPermission.PreviewAndPrint));
            Assert.That(report.GetRecommendedAction(),
                Is.EqualTo(SfntEmbeddingRecommendedAction.EmbedFullFont));
        });
    }

    [Test]
    public void FaceDoesNotAssumeRightsWhenOs2IsMissing()
    {
        SfntTableCache cache = CreateCache(null, BuildHead());
        SfntValidationResult validation = new SfntValidationResult(
            FontOutlineKind.Cff1,
            Array.Empty<FontDiagnostic>());
        FontFace face = new FontFace(0, 0x4F54544FU, validation, cache);

        Assert.Throws<FontFormatException>(() => face.GetSfntEmbeddingRights());
    }

    static SfntEmbeddingRightsReport Interpret(int version, int flags)
    {
        return SfntEmbeddingRightsInterpreter.Interpret(version, flags);
    }

    static int CountPermissionBits(int flags)
    {
        int count = 0;

        if ((flags & 2) != 0)
        {
            count += 1;
        }

        if ((flags & 4) != 0)
        {
            count += 1;
        }

        if ((flags & 8) != 0)
        {
            count += 1;
        }

        return count;
    }

    static SfntTableCache CreateCache(byte[]? os2, byte[] head)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Os2, os2);
        AddTable(tables, FontTags.Head, head);
        SfntTableStore store = new SfntTableStore(
            tables.ToArray(),
            Array.Empty<FontDiagnostic>());
        return SfntTableCache.Create(store, FontLoadMode.Compatible);
    }

    static void AddTable(List<SfntTableData> tables, int tag, byte[]? bytes)
    {
        if (bytes == null)
        {
            return;
        }

        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static byte[] BuildOs2(int embeddingFlags)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(5)
            .WriteInt16(500)
            .WriteUInt16(400)
            .WriteUInt16(5)
            .WriteUInt16((ushort)embeddingFlags)
            .WriteInt16(650)
            .WriteInt16(650)
            .WriteInt16(0)
            .WriteInt16(140)
            .WriteInt16(650)
            .WriteInt16(650)
            .WriteInt16(0)
            .WriteInt16(480)
            .WriteInt16(50)
            .WriteInt16(300)
            .WriteInt16(0);

        for (int index = 0; index < 10; index += 1)
        {
            builder.WriteUInt8(0);
        }

        return builder
            .WriteUInt32(1)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0x54455354U)
            .WriteUInt16(0)
            .WriteUInt16(0x20)
            .WriteUInt16(0x7E)
            .WriteInt16(800)
            .WriteInt16(-200)
            .WriteInt16(0)
            .WriteUInt16(900)
            .WriteUInt16(250)
            .WriteUInt32(1)
            .WriteUInt32(0)
            .WriteInt16(450)
            .WriteInt16(700)
            .WriteUInt16(0)
            .WriteUInt16(0x20)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(0xFFFF)
            .CopyBytes();
    }

    static byte[] BuildHead()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U)
            .WriteUInt16(0)
            .WriteUInt16(1000)
            .WriteInt64(0)
            .WriteInt64(0)
            .WriteInt16(-20)
            .WriteInt16(-30)
            .WriteInt16(1000)
            .WriteInt16(900)
            .WriteUInt16(0)
            .WriteUInt16(9)
            .WriteInt16(2)
            .WriteInt16(0)
            .WriteInt16(0)
            .CopyBytes();
    }
}
