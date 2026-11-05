using System.Text;
using ttf;

namespace ttf.Tests;

internal class FontNameRecordPublicTests
{
    [Test]
    public void EnumeratesCompleteRecordsWithDecodedValuesAndIdentifiers()
    {
        FontNames names = LoadFace().GetNames();
        FontNameRecord unique = names.GetRecord(0);
        FontNameRecord macintosh = names.GetRecord(1);
        FontNameRecord english = names.GetRecord(2);
        FontNameRecord traditional = names.GetRecord(4);
        FontNameRecord unsupported = names.GetRecord(6);

        Assert.Multiple(() =>
        {
            Assert.That(names.GetRecordCount(), Is.EqualTo(7));
            Assert.That(unique.GetRecordIndex(), Is.Zero);
            Assert.That(unique.GetPlatformId(), Is.Zero);
            Assert.That(unique.GetEncodingId(), Is.EqualTo(3));
            Assert.That(unique.GetLanguageId(), Is.Zero);
            Assert.That(unique.GetNameId(), Is.EqualTo(3));
            Assert.That(unique.GetEncodingKind(),
                Is.EqualTo(FontNameEncodingKind.Utf16BigEndian));
            Assert.That(unique.GetDecodedValue(), Is.EqualTo("Unique"));
            Assert.That(unique.GetLanguageTag(), Is.Null);
            Assert.That(unique.IsEncodingSupported(), Is.True);
            Assert.That(unique.IsValidForSelection(), Is.True);
            Assert.That(unique.GetDecodingDiagnosticCount(), Is.Zero);
            Assert.That(macintosh.GetEncodingKind(),
                Is.EqualTo(FontNameEncodingKind.MacintoshRoman));
            Assert.That(macintosh.GetLanguageTag(), Is.EqualTo("en"));
            Assert.That(macintosh.GetDecodedValue(), Is.EqualTo("Macintosh"));
            Assert.That(english.GetPlatformId(), Is.EqualTo(3));
            Assert.That(english.GetEncodingId(), Is.EqualTo(1));
            Assert.That(english.GetLanguageId(), Is.EqualTo(0x0409));
            Assert.That(english.GetNameId(), Is.EqualTo(1));
            Assert.That(english.GetLanguageTag(), Is.EqualTo("en-US"));
            Assert.That(english.GetEncodedLength(), Is.EqualTo(Utf16("English").Length));
            Assert.That(english.GetStringOffset(), Is.GreaterThan(0));
            Assert.That(english.HasValidLanguage(), Is.True);
            Assert.That(english.HasValidString(), Is.True);
            Assert.That(english.IsDuplicateKey(), Is.False);
            Assert.That(traditional.UsesLanguageTag(), Is.True);
            Assert.That(traditional.GetLanguageTagIndex(), Is.Zero);
            Assert.That(traditional.GetLanguageTag(), Is.EqualTo("zh-Hant-HK"));
            Assert.That(traditional.GetDecodedValue(), Is.EqualTo("Traditional"));
            Assert.That(unsupported.GetEncodingKind(),
                Is.EqualTo(FontNameEncodingKind.Unknown));
            Assert.That(unsupported.IsEncodingSupported(), Is.False);
            Assert.That(unsupported.GetDecodedValue(), Is.Empty);
            Assert.That(unsupported.IsValidForSelection(), Is.False);
            Assert.That(unsupported.GetDecodingDiagnosticCount(), Is.EqualTo(1));
            Assert.That(unsupported.GetDecodingDiagnosticCode(0),
                Is.EqualTo(FontDiagnosticCode.InvalidNameEncoding));
            Assert.Throws<InvalidOperationException>(() => english.GetLanguageTagIndex());
            Assert.Throws<ArgumentOutOfRangeException>(() => names.GetRecord(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => names.GetRecord(7));
        });

        byte[] first = english.CopyEncodedBytes();
        byte[] second = english.CopyEncodedBytes();
        first[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(Utf16("English")));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(english.CopyEncodedBytes(), Is.EqualTo(second));
        });
    }

    [Test]
    public void ExposesReplacementCountsAndDecodingDiagnostics()
    {
        FontNameRecord malformed = LoadFace().GetNames().GetRecord(5);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.GetPlatformId(), Is.EqualTo(3));
            Assert.That(malformed.GetEncodingKind(),
                Is.EqualTo(FontNameEncodingKind.Utf16BigEndian));
            Assert.That(malformed.HasValidLanguage(), Is.True);
            Assert.That(malformed.HasValidString(), Is.False);
            Assert.That(malformed.IsEncodingSupported(), Is.True);
            Assert.That(malformed.GetDecodedValue(), Is.EqualTo("\uFFFD"));
            Assert.That(malformed.HasReplacementCharacters(), Is.True);
            Assert.That(malformed.GetReplacementCount(), Is.EqualTo(1));
            Assert.That(malformed.IsValidForSelection(), Is.False);
            Assert.That(malformed.GetDecodingDiagnosticCount(), Is.EqualTo(2));
            Assert.That(malformed.GetDecodingDiagnosticCode(0),
                Is.EqualTo(FontDiagnosticCode.InvalidNameEncoding));
            Assert.That(malformed.GetDecodingDiagnosticCode(1),
                Is.EqualTo(FontDiagnosticCode.StringDecodingReplacement));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                malformed.GetDecodingDiagnosticCode(2));
        });
    }

    [Test]
    public void DecodesSupportedLegacyWindowsAndMacintoshEncodingsPublicly()
    {
        FontNames names = LoadFace(BuildLegacyNameTable()).GetNames();
        FontNameRecord cyrillic = names.GetRecord(0);
        FontNameRecord japanese = names.GetRecord(1);
        FontNameRecord simplifiedChinese = names.GetRecord(2);
        FontNameRecord traditionalChinese = names.GetRecord(3);
        FontNameRecord korean = names.GetRecord(4);

        Assert.Multiple(() =>
        {
            Assert.That(names.GetRecordCount(), Is.EqualTo(5));
            Assert.That(japanese.GetEncodingKind(),
                Is.EqualTo(FontNameEncodingKind.WindowsCodePage932));
            Assert.That(japanese.GetDecodedValue(), Is.EqualTo("\u3042"));
            Assert.That(japanese.GetLanguageTag(), Is.EqualTo("ja-JP"));
            Assert.That(simplifiedChinese.GetEncodingKind(),
                Is.EqualTo(FontNameEncodingKind.WindowsCodePage936));
            Assert.That(simplifiedChinese.GetDecodedValue(), Is.EqualTo("\u4E2D"));
            Assert.That(simplifiedChinese.GetLanguageTag(), Is.Null);
            Assert.That(traditionalChinese.GetEncodingKind(),
                Is.EqualTo(FontNameEncodingKind.WindowsCodePage950));
            Assert.That(traditionalChinese.GetDecodedValue(), Is.EqualTo("\u4E00"));
            Assert.That(traditionalChinese.GetLanguageTag(), Is.Null);
            Assert.That(korean.GetEncodingKind(),
                Is.EqualTo(FontNameEncodingKind.WindowsCodePage949));
            Assert.That(korean.GetDecodedValue(), Is.EqualTo("\uAC00"));
            Assert.That(korean.GetLanguageTag(), Is.EqualTo("ko-KR"));
            Assert.That(cyrillic.GetEncodingKind(),
                Is.EqualTo(FontNameEncodingKind.MacintoshLegacy));
            Assert.That(cyrillic.GetDecodedValue(), Is.EqualTo("\u0410"));
            Assert.That(cyrillic.GetLanguageTag(), Is.EqualTo("ru"));
            Assert.That(japanese.IsValidForSelection(), Is.True);
            Assert.That(simplifiedChinese.IsValidForSelection(), Is.True);
            Assert.That(traditionalChinese.IsValidForSelection(), Is.True);
            Assert.That(korean.IsValidForSelection(), Is.True);
            Assert.That(cyrillic.IsValidForSelection(), Is.True);
        });
    }

    [Test]
    public void ExposesRfc4647ExactParentEnglishAndFirstRecordProvenance()
    {
        FontFace face = LoadFace();
        FontNames exactNames = face.GetLocalizedNames("zh-Hant-HK");
        FontNames parentNames = face.GetLocalizedNames("fr-CA-x-private");
        FontNames fallbackNames = face.GetLocalizedNames("de-DE");
        FontNameValue exact = exactNames.GetFamilyName()!;
        FontNameValue parent = parentNames.GetFamilyName()!;
        FontNameValue fallback = fallbackNames.GetFamilyName()!;
        FontNameValue first = face.GetNames().GetName(3)!;

        Assert.Multiple(() =>
        {
            Assert.That(exactNames.GetRequestedLanguage(), Is.EqualTo("zh-Hant-HK"));
            Assert.That(exact.GetValue(), Is.EqualTo("Traditional"));
            Assert.That(exact.GetLanguageTag(), Is.EqualTo("zh-Hant-HK"));
            Assert.That(exact.GetMatchKind(), Is.EqualTo(FontNameMatchKind.ExactLanguage));
            Assert.That(exact.GetMatchedLanguageRange(), Is.EqualTo("zh-Hant-HK"));
            Assert.That(exact.GetRecord().GetRecordIndex(), Is.EqualTo(4));
            Assert.That(exact.GetRecord().GetLanguageId(), Is.EqualTo(0x8000));
            Assert.That(parent.GetValue(), Is.EqualTo("Français"));
            Assert.That(parent.GetLanguageTag(), Is.EqualTo("fr-FR"));
            Assert.That(parent.GetMatchKind(), Is.EqualTo(FontNameMatchKind.ParentLanguage));
            Assert.That(parent.GetMatchedLanguageRange(), Is.EqualTo("fr"));
            Assert.That(parent.GetRecord().GetRecordIndex(), Is.EqualTo(3));
            Assert.That(fallback.GetValue(), Is.EqualTo("English"));
            Assert.That(fallback.GetLanguageTag(), Is.EqualTo("en-US"));
            Assert.That(fallback.GetMatchKind(), Is.EqualTo(FontNameMatchKind.EnglishFallback));
            Assert.That(fallback.GetMatchedLanguageRange(), Is.EqualTo("en"));
            Assert.That(fallback.GetRecord().GetPlatformId(), Is.EqualTo(3));
            Assert.That(first.GetValue(), Is.EqualTo("Unique"));
            Assert.That(first.GetLanguageTag(), Is.Null);
            Assert.That(first.GetMatchKind(), Is.EqualTo(FontNameMatchKind.FirstValidRecord));
            Assert.That(first.GetMatchedLanguageRange(), Is.Null);
            Assert.That(first.GetRecord().GetNameId(), Is.EqualTo(3));
        });
    }

    static FontFace LoadFace()
    {
        return LoadFace(BuildNameTable());
    }

    static FontFace LoadFace(byte[] name)
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("cmap", new byte[4])
            .AddTable("glyf", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("loca", new byte[4])
            .AddTable("maxp", new byte[4])
            .AddTable("name", name)
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4])
            .Build();
        return new FontLoader()
            .LoadFromBytes(font, FontLoadOptions.CreateCompatible())
            .GetFace(0);
    }

    static byte[] BuildNameTable()
    {
        int[][] records = new int[][]
        {
            Record(0, 3, 0, 3, 0),
            Record(1, 0, 0, 1, 1),
            Record(3, 1, 0x0409, 1, 2),
            Record(3, 1, 0x040C, 1, 3),
            Record(3, 1, 0x8000, 1, 4),
            Record(3, 1, 0x8000, 2, 5),
            Record(3, 6, 0x0412, 1, 6)
        };
        byte[][] strings = new byte[][]
        {
            Utf16("Unique"),
            Encoding.ASCII.GetBytes("Macintosh"),
            Utf16("English"),
            Utf16("Français"),
            Utf16("Traditional"),
            new byte[] { 0xD8, 0x00 },
            new byte[] { 0x84, 0x41 }
        };
        string[] languageTags = new string[] { "zh-Hant-HK" };
        return BuildNameTable(records, strings, languageTags);
    }

    static byte[] BuildLegacyNameTable()
    {
        int[][] records = new int[][]
        {
            Record(1, 7, 32, 1, 0),
            Record(3, 2, 0x0411, 1, 1),
            Record(3, 3, 0x0804, 1, 2),
            Record(3, 4, 0x0404, 1, 3),
            Record(3, 5, 0x0412, 1, 4)
        };
        byte[][] strings = new byte[][]
        {
            new byte[] { 0x80 },
            new byte[] { 0x82, 0xA0 },
            new byte[] { 0xD6, 0xD0 },
            new byte[] { 0xA4, 0x40 },
            new byte[] { 0xB0, 0xA1 }
        };
        return BuildNameTable(records, strings, new string[0]);
    }

    static byte[] BuildNameTable(
        int[][] records,
        byte[][] strings,
        string[] languageTags)
    {
        int storageOffset = 6 + records.Length * 12 + 2 + languageTags.Length * 4;
        int[] stringOffsets = new int[strings.Length];
        int nextOffset = 0;
        int index = 0;

        while (index < strings.Length)
        {
            stringOffsets[index] = nextOffset;
            nextOffset += strings[index].Length;
            index += 1;
        }

        int[] languageTagOffsets = new int[languageTags.Length];
        index = 0;

        while (index < languageTags.Length)
        {
            languageTagOffsets[index] = nextOffset;
            nextOffset += Utf16(languageTags[index]).Length;
            index += 1;
        }

        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16((ushort)records.Length)
            .WriteUInt16((ushort)storageOffset);
        index = 0;

        while (index < records.Length)
        {
            int[] record = records[index];
            byte[] value = strings[record[4]];
            builder
                .WriteUInt16((ushort)record[0])
                .WriteUInt16((ushort)record[1])
                .WriteUInt16((ushort)record[2])
                .WriteUInt16((ushort)record[3])
                .WriteUInt16((ushort)value.Length)
                .WriteUInt16((ushort)stringOffsets[record[4]]);
            index += 1;
        }

        builder.WriteUInt16((ushort)languageTags.Length);
        index = 0;

        while (index < languageTags.Length)
        {
            byte[] encoded = Utf16(languageTags[index]);
            builder
                .WriteUInt16((ushort)encoded.Length)
                .WriteUInt16((ushort)languageTagOffsets[index]);
            index += 1;
        }

        index = 0;

        while (index < strings.Length)
        {
            builder.WriteBytes(strings[index]);
            index += 1;
        }

        index = 0;

        while (index < languageTags.Length)
        {
            builder.WriteBytes(Utf16(languageTags[index]));
            index += 1;
        }

        return builder.CopyBytes();
    }

    static int[] Record(
        int platformId,
        int encodingId,
        int languageId,
        int nameId,
        int stringIndex)
    {
        return new int[]
        {
            platformId,
            encodingId,
            languageId,
            nameId,
            stringIndex
        };
    }

    static byte[] Utf16(string value)
    {
        return Encoding.BigEndianUnicode.GetBytes(value);
    }
}
