namespace ttf.Tests;

internal class SfntNameSelectorTests
{
    [Test]
    public void ExactLanguageUsesStablePlatformAndEncodingPrecedence()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                RecordWithTag(0, 4, 0, 1, "Unicode"),
                RecordWithTag(3, 1, 0, 1, "Windows BMP"),
                RecordWithTag(3, 10, 0, 1, "Windows Full"),
                RecordWithTag(1, 0, 0, 1, "Macintosh")
            },
            new string[] { "fr-CA" });

        SfntLocalizedName? selected = table.SelectName(1, "FR-ca");

        Assert.Multiple(() =>
        {
            Assert.That(selected, Is.Not.Null);
            Assert.That(selected!.GetRecordIndex(), Is.EqualTo(2));
            Assert.That(selected.GetRecord(), Is.SameAs(table.GetRecord(2)));
            Assert.That(selected.GetDecodedName(), Is.SameAs(table.GetDecodedName(2)));
            Assert.That(selected.GetValue(), Is.EqualTo("Windows Full"));
            Assert.That(selected.GetLanguageTag(), Is.EqualTo("fr-CA"));
            Assert.That(selected.GetMatchKind(),
                Is.EqualTo(SfntLocalizedNameMatchKind.ExactLanguage));
        });
    }

    [Test]
    public void WalksParentsAndRemovesBcp47ExtensionSingletons()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                RecordWithTag(0, 4, 0, 1, "Generic Chinese"),
                RecordWithTag(3, 1, 1, 1, "Traditional Chinese")
            },
            new string[] { "zh", "zh-Hant" });

        SfntLocalizedName? selected = table.SelectName(
            1,
            "zh-Hant-HK-u-nu-hanidec");

        Assert.Multiple(() =>
        {
            Assert.That(selected!.GetValue(), Is.EqualTo("Traditional Chinese"));
            Assert.That(selected.GetLanguageTag(), Is.EqualTo("zh-Hant"));
            Assert.That(selected.GetMatchKind(),
                Is.EqualTo(SfntLocalizedNameMatchKind.ParentLanguage));
        });
    }

    [Test]
    public void BaseLanguageCanSelectARegionalRecordByPlatformPriority()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                RecordWithTag(0, 4, 0, 1, "Canadian French"),
                RecordWithTag(3, 1, 1, 1, "French French")
            },
            new string[] { "fr-CA", "fr-FR" });

        SfntLocalizedName? selected = table.SelectName(1, "fr");

        Assert.Multiple(() =>
        {
            Assert.That(selected!.GetValue(), Is.EqualTo("French French"));
            Assert.That(selected.GetLanguageTag(), Is.EqualTo("fr-FR"));
            Assert.That(selected.GetMatchKind(),
                Is.EqualTo(SfntLocalizedNameMatchKind.ParentLanguage));
        });
    }

    [Test]
    public void ResolvesWindowsAndMacintoshLanguageIdentifiers()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                Record(1, 0, 1, 1, "Mac French", true, true),
                Record(3, 1, 0x040C, 1, "Windows French", true, true)
            },
            Array.Empty<string>());

        SfntLocalizedName? exact = table.SelectName(1, "fr-FR");
        SfntLocalizedName? parent = table.SelectName(1, "fr-CA");

        Assert.Multiple(() =>
        {
            Assert.That(exact!.GetValue(), Is.EqualTo("Windows French"));
            Assert.That(exact.GetLanguageTag(), Is.EqualTo("fr-FR"));
            Assert.That(table.GetResolvedLanguageTagForRecord(0), Is.EqualTo("fr"));
            Assert.That(table.GetResolvedLanguageTagForRecord(1), Is.EqualTo("fr-FR"));
            Assert.That(exact.GetMatchKind(),
                Is.EqualTo(SfntLocalizedNameMatchKind.ExactLanguage));
            Assert.That(parent!.GetValue(), Is.EqualTo("Windows French"));
            Assert.That(parent.GetMatchKind(),
                Is.EqualTo(SfntLocalizedNameMatchKind.ParentLanguage));
        });
    }

    [Test]
    public void SkipsMalformedRequestedNamesAndFallsBackToEnglish()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                Record(3, 1, 0x0407, 1, "Broken", true, false),
                Record(1, 0, 11, 1, "Japanese", true, true),
                Record(1, 0, 0, 1, "Mac English", true, true),
                Record(3, 1, 0x0409, 1, "Windows English", true, true)
            },
            Array.Empty<string>());

        SfntLocalizedName? selected = table.SelectName(1, "de-DE");

        Assert.Multiple(() =>
        {
            Assert.That(selected!.GetValue(), Is.EqualTo("Windows English"));
            Assert.That(selected.GetLanguageTag(), Is.EqualTo("en-US"));
            Assert.That(selected.GetMatchKind(),
                Is.EqualTo(SfntLocalizedNameMatchKind.EnglishFallback));
        });
    }

    [Test]
    public void SelectionWithoutALanguagePrefersEnglish()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                Record(1, 0, 11, 1, "Japanese", true, true),
                Record(3, 1, 0x0409, 1, "English", true, true)
            },
            Array.Empty<string>());

        SfntLocalizedName? selected = table.SelectDefaultName(1);

        Assert.Multiple(() =>
        {
            Assert.That(selected!.GetValue(), Is.EqualTo("English"));
            Assert.That(selected.GetMatchKind(),
                Is.EqualTo(SfntLocalizedNameMatchKind.EnglishFallback));
        });
    }

    [Test]
    public void LastResortUsesFirstValidRecordInSourceOrder()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                Record(3, 1, 0x0411, 1, "Broken", true, false),
                Record(1, 0, 11, 1, "First valid", true, true),
                Record(3, 1, 0x040C, 1, "Preferred platform", true, true)
            },
            Array.Empty<string>());

        SfntLocalizedName? selected = table.SelectName(1, "ar-EG");

        Assert.Multiple(() =>
        {
            Assert.That(selected!.GetRecordIndex(), Is.EqualTo(1));
            Assert.That(selected.GetValue(), Is.EqualTo("First valid"));
            Assert.That(selected.GetMatchKind(),
                Is.EqualTo(SfntLocalizedNameMatchKind.FirstValidRecord));
        });
    }

    [Test]
    public void IgnoresOtherNameIdsAndInvalidLanguages()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                Record(3, 1, 0x0409, 2, "Wrong name ID", true, true),
                Record(3, 1, 0x0409, 1, "Invalid language", false, true)
            },
            Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.That(table.SelectName(1, "en-US"), Is.Null);
            Assert.That(table.SelectDefaultName(3), Is.Null);
        });
    }

    [Test]
    public void StableTiesUseTheFirstRecord()
    {
        SfntNameTable table = CreateTable(
            new SfntNameRecord[]
            {
                Record(3, 1, 0x0409, 1, "First", true, true),
                Record(3, 1, 0x0409, 1, "Second", true, true)
            },
            Array.Empty<string>());

        SfntLocalizedName? selected = table.SelectName(1, "en-US");

        Assert.That(selected!.GetValue(), Is.EqualTo("First"));
    }

    [TestCase("")]
    [TestCase("en-")]
    [TestCase("en--US")]
    [TestCase("en_US")]
    [TestCase("abcdefghi")]
    public void RejectsMalformedRequestedLanguageTags(string languageTag)
    {
        SfntNameTable table = CreateTable(
            Array.Empty<SfntNameRecord>(),
            Array.Empty<string>());

        Assert.Throws<ArgumentException>(() => table.SelectName(1, languageTag));
    }

    [Test]
    public void RejectsInvalidArguments()
    {
        SfntNameTable table = CreateTable(
            Array.Empty<SfntNameRecord>(),
            Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(
                () => SfntNameSelector.Find(null!, 1, "en"));
            Assert.Throws<ArgumentNullException>(() => table.SelectName(1, null!));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => table.SelectDefaultName(-1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => table.SelectDefaultName(65536));
        });
    }

    static SfntNameTable CreateTable(
        SfntNameRecord[] records,
        string[] languageTags)
    {
        SfntLanguageTagRecord[] tags = new SfntLanguageTagRecord[languageTags.Length];

        for (int index = 0; index < languageTags.Length; index += 1)
        {
            byte[] bytes = Utf16(languageTags[index]);
            FontData data = FontData.Create(bytes);
            tags[index] = new SfntLanguageTagRecord(
                0x8000 + index,
                bytes.Length,
                0,
                languageTags[index],
                true,
                data.GetSlice(0, bytes.Length));
        }

        return new SfntNameTable(
            1,
            0,
            0,
            records,
            tags,
            0,
            0,
            0);
    }

    static SfntNameRecord RecordWithTag(
        int platformId,
        int encodingId,
        int languageTagIndex,
        int nameId,
        string value)
    {
        return Record(
            platformId,
            encodingId,
            0x8000 + languageTagIndex,
            nameId,
            value,
            true,
            true);
    }

    static SfntNameRecord Record(
        int platformId,
        int encodingId,
        int languageId,
        int nameId,
        string value,
        bool validLanguage,
        bool validString)
    {
        byte[] bytes = platformId == 1 ? Ascii(value) : Utf16(value);

        if (!validString)
        {
            bytes = new byte[] { 0xD8, 0 };
        }

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
            languageId >= 0x8000,
            validLanguage,
            validString,
            false,
            data.GetSlice(0, bytes.Length));
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
