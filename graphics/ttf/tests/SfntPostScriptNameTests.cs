namespace ttf.Tests;

internal class SfntPostScriptNameTests
{
    [Test]
    public void LeavesValidPostScriptNamesUnchanged()
    {
        string original = "Font!#$&'*+,-.0123456789:=?@A\\^_`|~";
        SfntPostScriptName name = SfntPostScriptNameNormalizer.Normalize(original);

        Assert.Multiple(() =>
        {
            Assert.That(name.GetOriginalName(), Is.SameAs(original));
            Assert.That(name.GetNormalizedName(), Is.EqualTo(original));
            Assert.That(name.IsOriginalValid(), Is.True);
            Assert.That(name.WasNormalized(), Is.False);
            Assert.That(name.WasTruncated(), Is.False);
            Assert.That(name.GetInvalidCharacterCount(), Is.Zero);
            Assert.That(name.GetSelectedName(), Is.Null);
            Assert.That(SfntPostScriptNameNormalizer.IsValid(original), Is.True);
        });
    }

    [Test]
    public void ReplacesInvalidCharacterRunsWithoutChangingTheOriginal()
    {
        string original = " Bad/[Font](é)% Name ";
        SfntPostScriptName name = SfntPostScriptNameNormalizer.Normalize(original);

        Assert.Multiple(() =>
        {
            Assert.That(name.GetOriginalName(), Is.EqualTo(original));
            Assert.That(name.GetNormalizedName(), Is.EqualTo("-Bad-Font-Name-"));
            Assert.That(name.IsOriginalValid(), Is.False);
            Assert.That(name.WasNormalized(), Is.True);
            Assert.That(name.WasTruncated(), Is.False);
            Assert.That(name.GetInvalidCharacterCount(), Is.EqualTo(10));
            Assert.That(
                SfntPostScriptNameNormalizer.IsValid(name.GetNormalizedName()),
                Is.True);
        });
    }

    [TestCase("A B")]
    [TestCase("A/B")]
    [TestCase("A%B")]
    [TestCase("A[B]")]
    [TestCase("A(B)")]
    [TestCase("A{B}")]
    [TestCase("A<B>")]
    [TestCase("Café")]
    [TestCase("\u007F")]
    public void RejectsNamesOutsideTheOpenTypePostScriptCharacterSet(string value)
    {
        Assert.That(SfntPostScriptNameNormalizer.IsValid(value), Is.False);
    }

    [Test]
    public void TruncatesNormalizedNamesAtSixtyThreeAsciiCharacters()
    {
        string original = new string('A', 80);
        SfntPostScriptName name = SfntPostScriptNameNormalizer.Normalize(original);

        Assert.Multiple(() =>
        {
            Assert.That(name.GetOriginalName(), Has.Length.EqualTo(80));
            Assert.That(name.GetNormalizedName(), Is.EqualTo(new string('A', 63)));
            Assert.That(name.IsOriginalValid(), Is.False);
            Assert.That(name.WasNormalized(), Is.True);
            Assert.That(name.WasTruncated(), Is.True);
            Assert.That(name.GetInvalidCharacterCount(), Is.Zero);
            Assert.That(
                SfntPostScriptNameNormalizer.IsValid(name.GetNormalizedName()),
                Is.True);
        });
    }

    [Test]
    public void UsesAStableFallbackForAnEmptyName()
    {
        SfntPostScriptName name = SfntPostScriptNameNormalizer.Normalize("");

        Assert.Multiple(() =>
        {
            Assert.That(name.GetOriginalName(), Is.Empty);
            Assert.That(name.GetNormalizedName(), Is.EqualTo("UnnamedFont"));
            Assert.That(name.IsOriginalValid(), Is.False);
            Assert.That(name.WasNormalized(), Is.True);
            Assert.That(SfntPostScriptNameNormalizer.IsValid(null!), Is.False);
            Assert.That(SfntPostScriptNameNormalizer.IsValid(""), Is.False);
            Assert.Throws<ArgumentNullException>(
                () => SfntPostScriptNameNormalizer.Normalize(null!));
        });
    }

    [Test]
    public void SelectsNameIdSixAndPreservesItsMetadataView()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                Record(1, 0, 0, 6, "MacPSName"),
                Record(3, 1, 0x0409, 6, "Windows / Name"),
                Record(3, 1, 0x0409, 1, "Family Name")
            });

        SfntPostScriptName? name = table.GetPostScriptName();

        Assert.Multiple(() =>
        {
            Assert.That(name!.GetSelectedName()!.GetRecordIndex(), Is.EqualTo(1));
            Assert.That(name.GetOriginalName(), Is.EqualTo("Windows / Name"));
            Assert.That(name.GetNormalizedName(), Is.EqualTo("Windows-Name"));
            Assert.That(table.GetDecodedName(1).GetValue(),
                Is.EqualTo("Windows / Name"));
            Assert.That(table.GetRecord(1).GetNameId(), Is.EqualTo(6));
        });
    }

    [Test]
    public void ReturnsNoResultWhenNameIdSixIsAbsent()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                Record(3, 1, 0x0409, 1, "Family Name")
            });

        Assert.Multiple(() =>
        {
            Assert.That(table.GetPostScriptName(), Is.Null);
            Assert.Throws<ArgumentNullException>(
                () => SfntPostScriptNameNormalizer.Find(null!));
        });
    }

    [Test]
    public void FontFaceExposesTheOriginalAndNormalizedNames()
    {
        FontFace face = CreateFace("Face / Name");
        SfntPostScriptName? name = face.GetPostScriptName();

        Assert.Multiple(() =>
        {
            Assert.That(name!.GetOriginalName(), Is.EqualTo("Face / Name"));
            Assert.That(name.GetNormalizedName(), Is.EqualTo("Face-Name"));
            Assert.That(face.GetNameTable()!.SelectDefaultName(6)!.GetValue(),
                Is.EqualTo("Face / Name"));
        });
    }

    static SfntNameTable CreateTable(SfntNameRecord[] records)
    {
        return new SfntNameTable(
            0,
            0,
            0,
            records,
            new SfntLanguageTagRecord[0],
            0,
            0,
            0);
    }

    static SfntNameRecord Record(
        int platformId,
        int encodingId,
        int languageId,
        int nameId,
        string value)
    {
        byte[] bytes = platformId == 1 ? Ascii(value) : Utf16(value);
        FontData data = FontData.Create(bytes);
        SfntNameEncodingKind kind = platformId == 1 ?
            SfntNameEncodingKind.MacintoshRoman :
            SfntNameEncodingKind.Utf16BigEndian;
        return new SfntNameRecord(
            platformId,
            encodingId,
            languageId,
            nameId,
            bytes.Length,
            0,
            kind,
            false,
            true,
            true,
            false,
            data.GetSlice(0, bytes.Length));
    }

    static FontFace CreateFace(string postScriptName)
    {
        byte[] nameBytes = BuildNameTable(postScriptName);
        FontData data = FontData.Create(nameBytes);
        SfntTableData table = new SfntTableData(
            FontTags.Name,
            FontChecksum.CalculateTable(nameBytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                new FontDiagnostic[0]),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntValidationResult validation = new SfntValidationResult(
            FontOutlineKind.TrueType,
            new FontDiagnostic[0]);
        return new FontFace(0, 0x00010000U, validation, cache);
    }

    static byte[] BuildNameTable(string value)
    {
        byte[] encoded = Utf16(value);
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(18)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt16(0x0409)
            .WriteUInt16(6)
            .WriteUInt16((ushort)encoded.Length)
            .WriteUInt16(0);

        for (int index = 0; index < encoded.Length; index += 1)
        {
            builder.WriteUInt8(encoded[index]);
        }

        return builder.CopyBytes();
    }

    static byte[] Utf16(string value)
    {
        byte[] bytes = new byte[value.Length * 2];

        for (int index = 0; index < value.Length; index += 1)
        {
            bytes[index * 2] = (byte)(value[index] >> 8);
            bytes[index * 2 + 1] = (byte)value[index];
        }

        return bytes;
    }

    static byte[] Ascii(string value)
    {
        byte[] bytes = new byte[value.Length];

        for (int index = 0; index < value.Length; index += 1)
        {
            bytes[index] = (byte)value[index];
        }

        return bytes;
    }
}
