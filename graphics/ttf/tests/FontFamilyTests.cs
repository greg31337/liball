using ttf;

namespace ttf.Tests;

internal class FontFamilyTests
{
    [Test]
    public void GroupsTypographicAliasesAndUsesRegularStyleForCanonicalNames()
    {
        FontFace bold = CreateFace(
            new int[] { 16, 16, 17, 17, 1, 2 },
            new int[] { 0x0409, 0x040C, 0x0409, 0x040C, 0x0409, 0x0409 },
            new string[]
            {
                "  acme   sans ", "Acme French Bold", "Bold", "Gras",
                "Acme Sans Bold", "Bold"
            },
            700,
            5,
            false,
            true);
        FontFace regular = CreateFace(
            new int[] { 16, 16, 17, 17, 1, 2 },
            new int[] { 0x0409, 0x040C, 0x0409, 0x040C, 0x0409, 0x0409 },
            new string[]
            {
                "Acme Sans", "Famille Acme", "Regular", "Normal",
                "Acme Sans", "Regular"
            },
            400,
            5,
            false,
            false);
        FontFace other = CreateFace(
            new int[] { 16, 17 },
            new int[] { 0x0409, 0x0409 },
            new string[] { "Other Family", "Regular" },
            400,
            5,
            false,
            false);
        FontSet set = new FontSet();

        Assert.That(set.Add(bold), Is.True);
        FontFamily originalSnapshot = set.GetFamily(0);
        Assert.That(set.Add(regular), Is.True);
        Assert.That(set.Add(other), Is.True);
        FontFamily family = set.GetFamily(0);

        Assert.Multiple(() =>
        {
            Assert.That(set.GetFamilyCount(), Is.EqualTo(2));
            Assert.That(originalSnapshot.GetFaceCount(), Is.EqualTo(1));
            Assert.That(family.GetFaceCount(), Is.EqualTo(2));
            Assert.That(family.GetFace(0), Is.SameAs(bold));
            Assert.That(family.GetFace(1), Is.SameAs(regular));
            Assert.That(family.GetFamilyName()!.GetValue(), Is.EqualTo("Acme Sans"));
            Assert.That(family.GetLocalizedFamilyName("fr-FR")!.GetValue(), Is.EqualTo("Famille Acme"));
            Assert.That(family.GetLocalizedFamilyName("fr-FR")!.GetMatchKind(),
                Is.EqualTo(FontNameMatchKind.ExactLanguage));
            Assert.That(family.GetLocalizedFaceStyleName(0, "fr-FR")!.GetValue(), Is.EqualTo("Gras"));
            Assert.That(family.GetFaceStyleName(1)!.GetValue(), Is.EqualTo("Regular"));
            Assert.That(set.GetFamily(1).GetFamilyName()!.GetValue(), Is.EqualTo("Other Family"));
        });
    }

    [Test]
    public void MergesFamiliesThroughLocalizedAliasBridges()
    {
        FontFace first = CreateFace(
            new int[] { 16, 16, 17 },
            new int[] { 0x0409, 0x040C, 0x0409 },
            new string[] { "Alpha", "Pont", "Regular" },
            400,
            5,
            false,
            false);
        FontFace second = CreateFace(
            new int[] { 16, 16, 17 },
            new int[] { 0x040C, 0x0411, 0x0409 },
            new string[] { "Pont", "ベータ", "Regular" },
            400,
            5,
            false,
            false);
        FontFace third = CreateFace(
            new int[] { 16, 17 },
            new int[] { 0x0411, 0x0409 },
            new string[] { "ベータ", "Regular" },
            400,
            5,
            false,
            false);
        FontSet set = new FontSet();
        set.Add(first);
        set.Add(second);
        set.Add(third);

        FontFamily family = set.GetFamily(0);

        Assert.Multiple(() =>
        {
            Assert.That(set.GetFamilyCount(), Is.EqualTo(1));
            Assert.That(family.GetFaceCount(), Is.EqualTo(3));
            Assert.That(family.GetLocalizedFamilyName("en-US")!.GetValue(), Is.EqualTo("Alpha"));
            Assert.That(family.GetLocalizedFamilyName("fr-FR")!.GetValue(), Is.EqualTo("Pont"));
            Assert.That(family.GetLocalizedFamilyName("ja-JP")!.GetValue(), Is.EqualTo("ベータ"));
        });
    }

    [Test]
    public void FallsBackToLegacyFamilyAndSubfamilyNames()
    {
        FontFace regular = CreateFace(
            new int[] { 1, 2 },
            new int[] { 0x0409, 0x0409 },
            new string[] { "Legacy Family", "Book" },
            400,
            5,
            false,
            false);
        FontFace italic = CreateFace(
            new int[] { 1, 2 },
            new int[] { 0x0409, 0x0409 },
            new string[] { "legacy family", "Italic" },
            400,
            5,
            true,
            false);
        FontSet set = new FontSet();
        set.Add(regular);
        set.Add(italic);

        FontFamily family = set.GetFamily(0);

        Assert.Multiple(() =>
        {
            Assert.That(set.GetFamilyCount(), Is.EqualTo(1));
            Assert.That(family.GetFamilyName()!.GetValue(), Is.EqualTo("Legacy Family"));
            Assert.That(family.GetFaceStyleName(0)!.GetValue(), Is.EqualTo("Book"));
            Assert.That(family.GetFaceStyleName(1)!.GetValue(), Is.EqualTo("Italic"));
        });
    }

    [Test]
    public void KeepsUnnamedFacesInSeparateFamilies()
    {
        FontFace first = CreateFace(
            new int[] { 2 },
            new int[] { 0x0409 },
            new string[] { "Regular" },
            400,
            5,
            false,
            false);
        FontFace second = CreateFace(
            new int[] { 2 },
            new int[] { 0x0409 },
            new string[] { "Bold" },
            700,
            5,
            false,
            true);
        FontSet set = new FontSet();
        set.Add(first);
        set.Add(second);

        Assert.Multiple(() =>
        {
            Assert.That(set.GetFamilyCount(), Is.EqualTo(2));
            Assert.That(set.GetFamily(0).GetFamilyName(), Is.Null);
            Assert.That(set.GetFamily(1).GetLocalizedFamilyName("en-US"), Is.Null);
            Assert.That(set.GetFamily(0).GetFaceStyleName(0)!.GetValue(), Is.EqualTo("Regular"));
        });
    }

    [Test]
    public void RejectsInvalidFamilyAndFaceQueries()
    {
        FontSet empty = new FontSet();
        FontSet populated = new FontSet();
        populated.Add(CreateFace(
            new int[] { 16, 17 },
            new int[] { 0x0409, 0x0409 },
            new string[] { "Family", "Regular" },
            400,
            5,
            false,
            false));
        FontFamily family = populated.GetFamily(0);

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetFamilyCount(), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.GetFamily(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => populated.GetFamily(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => populated.GetFamily(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => family.GetFace(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => family.GetFaceStyleName(1));
            Assert.Throws<ArgumentNullException>(() => family.GetLocalizedFamilyName(null!));
            Assert.Throws<ArgumentException>(() => family.GetLocalizedFamilyName("en_US"));
            Assert.Throws<ArgumentNullException>(() => family.GetLocalizedFaceStyleName(0, null!));
            Assert.Throws<ArgumentException>(() => family.GetLocalizedFaceStyleName(0, "fr_XX"));
        });
    }

    static FontFace CreateFace(
        int[] nameIds,
        int[] languageIds,
        string[] values,
        int weightClass,
        int widthClass,
        bool italic,
        bool bold)
    {
        int macStyle = (bold ? 1 : 0) | (italic ? 2 : 0);
        int selectionFlags = (italic ? 1 : 0) |
            (bold ? 1 << 5 : 0) |
            (!italic && !bold ? 1 << 6 : 0);
        SfntTableData[] tables = new SfntTableData[]
        {
            CreateTable(FontTags.Head, BuildHead(macStyle)),
            CreateTable(FontTags.Name, BuildName(nameIds, languageIds, values)),
            CreateTable(FontTags.Os2, BuildOs2(weightClass, widthClass, selectionFlags))
        };
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x4F54544FU,
            new SfntValidationResult(FontOutlineKind.Cff1, Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntTableData CreateTable(int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static byte[] BuildName(int[] nameIds, int[] languageIds, string[] values)
    {
        if (nameIds.Length != languageIds.Length || nameIds.Length != values.Length)
        {
            throw new ArgumentException("Name test data lengths must match.");
        }

        byte[][] encoded = new byte[values.Length][];
        int[] order = new int[values.Length];
        int storageOffset = 6 + values.Length * 12;
        int stringOffset = 0;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)values.Length)
            .WriteUInt16((ushort)storageOffset);

        for (int index = 0; index < values.Length; index += 1)
        {
            encoded[index] = System.Text.Encoding.BigEndianUnicode.GetBytes(values[index]);
            order[index] = index;
        }

        for (int index = 1; index < order.Length; index += 1)
        {
            int selected = order[index];
            int insertionIndex = index;

            while (insertionIndex > 0 && ComesBefore(
                selected,
                order[insertionIndex - 1],
                nameIds,
                languageIds))
            {
                order[insertionIndex] = order[insertionIndex - 1];
                insertionIndex -= 1;
            }

            order[insertionIndex] = selected;
        }

        for (int index = 0; index < order.Length; index += 1)
        {
            int sourceIndex = order[index];
            builder.WriteUInt16(3)
                .WriteUInt16(1)
                .WriteUInt16((ushort)languageIds[sourceIndex])
                .WriteUInt16((ushort)nameIds[sourceIndex])
                .WriteUInt16((ushort)encoded[sourceIndex].Length)
                .WriteUInt16((ushort)stringOffset);
            stringOffset += encoded[sourceIndex].Length;
        }

        for (int index = 0; index < order.Length; index += 1)
        {
            builder.WriteBytes(encoded[order[index]]);
        }

        return builder.CopyBytes();
    }

    static bool ComesBefore(
        int first,
        int second,
        int[] nameIds,
        int[] languageIds)
    {
        if (languageIds[first] != languageIds[second])
        {
            return languageIds[first] < languageIds[second];
        }

        return nameIds[first] < nameIds[second];
    }

    static byte[] BuildHead(int macStyle)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt32(0x00010000U).WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U).WriteUInt16(0).WriteUInt16(1000)
            .WriteInt64(0).WriteInt64(0)
            .WriteInt16(-20).WriteInt16(-30).WriteInt16(1000).WriteInt16(900)
            .WriteUInt16((ushort)macStyle).WriteUInt16(9)
            .WriteInt16(2).WriteInt16(0).WriteInt16(0)
            .CopyBytes();
    }

    static byte[] BuildOs2(int weightClass, int widthClass, int selectionFlags)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0).WriteInt16(500)
            .WriteUInt16((ushort)weightClass).WriteUInt16((ushort)widthClass)
            .WriteUInt16(0)
            .WriteInt16(600).WriteInt16(600).WriteInt16(0).WriteInt16(100)
            .WriteInt16(600).WriteInt16(600).WriteInt16(0).WriteInt16(400)
            .WriteInt16(50).WriteInt16(250).WriteInt16(0);

        for (int index = 0; index < 10; index += 1)
        {
            builder.WriteUInt8(0);
        }

        return builder
            .WriteUInt32(1).WriteUInt32(0).WriteUInt32(0).WriteUInt32(0)
            .WriteUInt32(0x54455354U).WriteUInt16((ushort)selectionFlags)
            .WriteUInt16(0x20).WriteUInt16(0x7E)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(20)
            .WriteUInt16(900).WriteUInt16(250)
            .CopyBytes();
    }
}
