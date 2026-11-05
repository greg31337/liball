using ttf;

namespace ttf.Tests;

internal class UnicodeDataTests
{
    [Test]
    public void LoadsPinnedVersionFingerprintAndSectionDirectory()
    {
        UnicodeData data = UnicodeData.LoadDefault();

        Assert.Multiple(() =>
        {
            Assert.That(data.GetFormatVersion(), Is.EqualTo(1));
            Assert.That(data.GetUnicodeMajorVersion(), Is.EqualTo(17));
            Assert.That(data.GetUnicodeMinorVersion(), Is.Zero);
            Assert.That(data.GetUnicodePatchVersion(), Is.Zero);
            Assert.That(data.GetSectionCount(), Is.EqualTo(32));
            Assert.That(Convert.ToHexString(data.CopyFingerprint()).ToLowerInvariant(),
                Is.EqualTo(
                    "e363e889dd473ac487aa937e7de592c69acb50b44fcd8bfdff2eb959b786baf0"));
            Assert.That(data.HasSection(UnicodeDataTags.Script), Is.True);
            Assert.That(data.HasSection(UnicodeDataTags.CanonicalDecomposition),
                Is.True);
            Assert.That(data.HasSection(UnicodeDataTags.IndicConjunctBreak),
                Is.True);
            Assert.That(data.HasSection(UnicodeDataTags.EastAsianWidth),
                Is.True);
            Assert.That(data.HasSection(UnicodeDataTags.GeneralCategory),
                Is.True);
            Assert.That(data.HasSection(0x41424344), Is.False);
        });
    }

    [Test]
    public void QueriesScriptBidiJoiningAndScriptExtensions()
    {
        UnicodeData data = UnicodeData.LoadDefault();
        string middleDotExtensions = data.GetPropertyName(
            UnicodeDataTags.ScriptExtensions,
            0x00B7);

        Assert.Multiple(() =>
        {
            Assert.That(Property(data, UnicodeDataTags.Script, 0x0041),
                Is.EqualTo("Latn"));
            Assert.That(Property(data, UnicodeDataTags.Script, 0x03B1),
                Is.EqualTo("Grek"));
            Assert.That(Property(data, UnicodeDataTags.Script, 0x0410),
                Is.EqualTo("Cyrl"));
            Assert.That(Property(data, UnicodeDataTags.Script, 0x0378),
                Is.EqualTo("Zzzz"));
            Assert.That(middleDotExtensions.Split(' '),
                Does.Contain("Grek"));
            Assert.That(middleDotExtensions.Split(' '),
                Does.Contain("Latn"));
            Assert.That(Property(
                data,
                UnicodeDataTags.ScriptExtensions,
                0x0041), Is.Empty);
            Assert.That(Property(data, UnicodeDataTags.Bidi, 0x0041),
                Is.EqualTo("L"));
            Assert.That(Property(data, UnicodeDataTags.Bidi, 0x05D0),
                Is.EqualTo("R"));
            Assert.That(Property(data, UnicodeDataTags.Bidi, 0x0627),
                Is.EqualTo("AL"));
            Assert.That(Property(data, UnicodeDataTags.JoiningType, 0x0628),
                Is.EqualTo("D"));
            Assert.That(Property(data, UnicodeDataTags.JoiningGroup, 0x0628),
                Is.EqualTo("Beh"));
        });
    }

    [Test]
    public void QueriesBreakVerticalIndicAndHangulProperties()
    {
        UnicodeData data = UnicodeData.LoadDefault();

        Assert.Multiple(() =>
        {
            Assert.That(Property(data, UnicodeDataTags.GraphemeBreak, 0x0301),
                Is.EqualTo("Extend"));
            Assert.That(Property(data, UnicodeDataTags.GraphemeBreak, 0x200D),
                Is.EqualTo("ZWJ"));
            Assert.That(Property(data, UnicodeDataTags.LineBreak, 0x0020),
                Is.EqualTo("SP"));
            Assert.That(Property(data, UnicodeDataTags.LineBreak, 0x000A),
                Is.EqualTo("LF"));
            Assert.That(Property(data, UnicodeDataTags.EastAsianWidth, 0x4E00),
                Is.EqualTo("W"));
            Assert.That(Property(data, UnicodeDataTags.GeneralCategory, 0x00AB),
                Is.EqualTo("Pi"));
            Assert.That(Property(
                data,
                UnicodeDataTags.VerticalOrientation,
                0x0041), Is.EqualTo("R"));
            Assert.That(Property(
                data,
                UnicodeDataTags.VerticalOrientation,
                0x4E00), Is.EqualTo("U"));
            Assert.That(Property(
                data,
                UnicodeDataTags.IndicSyllabicCategory,
                0x0915), Is.EqualTo("Consonant"));
            Assert.That(Property(
                data,
                UnicodeDataTags.IndicPositionalCategory,
                0x093F), Is.EqualTo("Left"));
            Assert.That(Property(
                data,
                UnicodeDataTags.IndicConjunctBreak,
                0x0915), Is.EqualTo("Consonant"));
            Assert.That(Property(
                data,
                UnicodeDataTags.IndicConjunctBreak,
                0x094D), Is.EqualTo("Linker"));
            Assert.That(Property(data, UnicodeDataTags.HangulSyllableType, 0x1100),
                Is.EqualTo("L"));
            Assert.That(Property(data, UnicodeDataTags.HangulSyllableType, 0xAC00),
                Is.EqualTo("LV"));
            Assert.That(Property(data, UnicodeDataTags.HangulSyllableType, 0xAC01),
                Is.EqualTo("LVT"));
        });
    }

    [Test]
    public void QueriesNormalizationMappingsAndQuickChecks()
    {
        UnicodeData data = UnicodeData.LoadDefault();

        Assert.Multiple(() =>
        {
            Assert.That(Property(
                data,
                UnicodeDataTags.CanonicalCombiningClass,
                0x0301), Is.EqualTo("230"));
            Assert.That(Property(
                data,
                UnicodeDataTags.CanonicalCombiningClass,
                0x0041), Is.EqualTo("0"));
            Assert.That(data.GetSequenceLength(
                UnicodeDataTags.CanonicalDecomposition,
                0x00C0), Is.EqualTo(2));
            Assert.That(data.GetSequenceValue(
                UnicodeDataTags.CanonicalDecomposition,
                0x00C0,
                0), Is.EqualTo(0x0041));
            Assert.That(data.GetSequenceValue(
                UnicodeDataTags.CanonicalDecomposition,
                0x00C0,
                1), Is.EqualTo(0x0300));
            Assert.That(Property(data, UnicodeDataTags.NfcQuickCheck, 0x0301),
                Is.EqualTo("M"));
            Assert.That(Property(data, UnicodeDataTags.NfdQuickCheck, 0x00C0),
                Is.EqualTo("N"));
            Assert.That(Property(data, UnicodeDataTags.NfkcQuickCheck, 0x00A0),
                Is.EqualTo("N"));
            Assert.That(Property(data, UnicodeDataTags.NfkdQuickCheck, 0x00A0),
                Is.EqualTo("N"));
            Assert.That(Property(
                data,
                UnicodeDataTags.CompositionExclusion,
                0x0340), Is.EqualTo("Y"));
            Assert.That(Property(
                data,
                UnicodeDataTags.CompositionExclusion,
                0x0041), Is.EqualTo("N"));
        });
    }

    [Test]
    public void QueriesBracketsMirrorsIgnorablesAndEmojiProperties()
    {
        UnicodeData data = UnicodeData.LoadDefault();

        Assert.Multiple(() =>
        {
            Assert.That(data.GetMapping(UnicodeDataTags.Bracket, 0x0028),
                Is.EqualTo(0x0029));
            Assert.That(data.GetMappingAuxiliary(
                UnicodeDataTags.Bracket,
                0x0028), Is.EqualTo(1));
            Assert.That(data.GetMappingAuxiliary(
                UnicodeDataTags.Bracket,
                0x0029), Is.EqualTo(2));
            Assert.That(data.GetMapping(UnicodeDataTags.Mirror, 0x0028),
                Is.EqualTo(0x0029));
            Assert.That(data.GetMapping(UnicodeDataTags.Mirror, 0x0041),
                Is.EqualTo(-1));
            Assert.That(Property(data, UnicodeDataTags.DefaultIgnorable, 0x200B),
                Is.EqualTo("Y"));
            Assert.That(Property(data, UnicodeDataTags.DefaultIgnorable, 0x0041),
                Is.EqualTo("N"));
            Assert.That(Property(data, UnicodeDataTags.VariationSelector, 0xFE0F),
                Is.EqualTo("Y"));
            Assert.That(Property(
                data,
                UnicodeDataTags.ModifierCombiningMark,
                0x0654), Is.EqualTo("Y"));
            Assert.That(Property(
                data,
                UnicodeDataTags.ModifierCombiningMark,
                0x0653), Is.EqualTo("N"));
            Assert.That(Property(data, UnicodeDataTags.Emoji, 0x1F600),
                Is.EqualTo("Y"));
            Assert.That(Property(data, UnicodeDataTags.EmojiPresentation, 0x1F600),
                Is.EqualTo("Y"));
            Assert.That(Property(data, UnicodeDataTags.Emoji, 0x2764),
                Is.EqualTo("Y"));
            Assert.That(Property(data, UnicodeDataTags.EmojiPresentation, 0x2764),
                Is.EqualTo("N"));
            Assert.That(Property(data, UnicodeDataTags.EmojiModifier, 0x1F3FB),
                Is.EqualTo("Y"));
            Assert.That(Property(data, UnicodeDataTags.EmojiModifierBase, 0x1F44D),
                Is.EqualTo("Y"));
            Assert.That(Property(data, UnicodeDataTags.EmojiComponent, 0x200D),
                Is.EqualTo("Y"));
            Assert.That(Property(data, UnicodeDataTags.ExtendedPictographic, 0x1F600),
                Is.EqualTo("Y"));
        });
    }

    [Test]
    public void RejectsMalformedArtifactsAndInvalidQueries()
    {
        byte[] valid = UnicodeData.LoadDefault().CopyBytes();
        byte[] badMagic = Copy(valid);
        badMagic[0] = 0;
        byte[] badVersion = Copy(valid);
        badVersion[9] = 2;
        byte[] badDirectoryOffset = Copy(valid);
        WriteUInt32(badDirectoryOffset, 52, 0);
        byte[] badSectionKind = Copy(valid);
        int firstSectionOffset = ReadUInt32(badSectionKind, 52);
        badSectionKind[firstSectionOffset] = 9;
        byte[] badUtf8 = Copy(valid);
        int valueCount = ReadUInt16(badUtf8, firstSectionOffset + 2);
        int namesOffset = firstSectionOffset + 16 + (valueCount + 1) * 4;
        badUtf8[namesOffset] = 0xFF;
        byte[] badRange = Copy(valid);
        int nameLength = ReadUInt32(badRange, firstSectionOffset + 12);
        int rangeOffset = AlignFour(
            firstSectionOffset + 16 + (valueCount + 1) * 4 + nameLength);
        WriteUInt32(badRange, rangeOffset, 0x110000);
        byte[] truncated = valid.AsSpan(0, valid.Length - 1).ToArray();
        UnicodeData data = UnicodeData.LoadDefault();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => new UnicodeData(null!));
            Assert.Throws<FontFormatException>(() => new UnicodeData(new byte[0]));
            Assert.Throws<FontFormatException>(() => new UnicodeData(badMagic));
            Assert.Throws<FontFormatException>(() => new UnicodeData(badVersion));
            Assert.Throws<FontFormatException>(() =>
                new UnicodeData(badDirectoryOffset));
            Assert.Throws<FontFormatException>(() =>
                new UnicodeData(badSectionKind));
            Assert.Throws<FontFormatException>(() => new UnicodeData(badUtf8));
            Assert.Throws<FontFormatException>(() => new UnicodeData(badRange));
            Assert.Throws<FontFormatException>(() => new UnicodeData(truncated));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                data.GetPropertyName(UnicodeDataTags.Script, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                data.GetPropertyName(UnicodeDataTags.Script, 0x110000));
            Assert.Throws<ArgumentException>(() =>
                data.GetPropertyName(0x41424344, 0));
            Assert.Throws<InvalidOperationException>(() =>
                data.GetPropertyName(UnicodeDataTags.Mirror, 0x0028));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                data.GetSequenceValue(
                    UnicodeDataTags.CanonicalDecomposition,
                    0x0041,
                    0));
        });
    }

    static string Property(UnicodeData data, int tag, int codePoint)
    {
        return data.GetPropertyName(tag, codePoint);
    }

    static byte[] Copy(byte[] bytes)
    {
        return bytes.ToArray();
    }

    static int ReadUInt32(byte[] bytes, int offset)
    {
        return bytes[offset] << 24 |
            bytes[offset + 1] << 16 |
            bytes[offset + 2] << 8 |
            bytes[offset + 3];
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    static int AlignFour(int value)
    {
        return value + 3 & ~3;
    }

    static void WriteUInt32(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
