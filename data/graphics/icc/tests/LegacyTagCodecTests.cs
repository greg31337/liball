using icc;

namespace icc.Tests;

public class LegacyTagCodecTests
{
    [Test]
    public void TextDescriptionDecodesAllUnalignedLegacyTextFields()
    {
        byte[] payload = CreateTextDescription("Test", "\u8272\u5f69", 0x656e5553, 1, "Mac");

        TagDecodeState state = DecodeTextDescription(payload, true);
        IccTextData text = state.Data.Text!;
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Text));
            Assert.That(text.TypeSignature, Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(text.AsciiText, Is.EqualTo("Test"));
            Assert.That(text.UnicodeText, Is.EqualTo("\u8272\u5f69"));
            Assert.That(text.UnicodeLanguageCode, Is.EqualTo(0x656e5553));
            Assert.That(text.ScriptCode, Is.EqualTo(1));
            Assert.That(text.ScriptCodeText, Is.EqualTo("Mac"));
            Assert.That(text.Text, Is.EqualTo("\u8272\u5f69"));
            Assert.That(text.Bytes, Is.EqualTo(payload));
            Assert.That(clone.Text!.AsciiText, Is.EqualTo("Test"));
            Assert.That(clone.Text.UnicodeText, Is.EqualTo("\u8272\u5f69"));
            Assert.That(clone.Text.ScriptCodeText, Is.EqualTo("Mac"));
            Assert.That(clone.Text.Bytes, Is.Not.SameAs(text.Bytes));
        });
    }

    [Test]
    public void TextDescriptionToleratesInconsistentCountsAndRetainsExactBytes()
    {
        byte[] payload = CreateTextDescription("Legacy", "\u56fd\u969b", 0, 0, "Mac");
        WriteUInt32(payload, 8, 6);
        WriteUInt32(payload, 23, 99);
        payload[payload.Length - 68] = 255;

        TagDecodeState state = DecodeTextDescription(payload, true);
        byte[] serialized = LegacyTagCodec.SerializeTextDescription(state.Data.Text!);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("tolerating inconsistent legacy"));
            Assert.That(state.Data.Text!.AsciiText, Is.EqualTo("Legacy"));
            Assert.That(state.Data.Text.UnicodeText, Is.EqualTo("\u56fd\u969b"));
            Assert.That(state.Data.Text.ScriptCodeText, Is.EqualTo("Mac"));
            Assert.That(serialized, Is.EqualTo(payload));
            Assert.That(serialized, Is.Not.SameAs(state.Data.Text.Bytes));
        });
    }

    [Test]
    public void TextDescriptionRecoversFieldsAfterAnInflatedAsciiCount()
    {
        byte[] payload = CreateTextDescription("Legacy", "Local", 0x656e5553, 0, "Mac");
        WriteUInt32(payload, 8, 100);

        TagDecodeState state = DecodeTextDescription(payload, true);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("tolerating inconsistent legacy"));
            Assert.That(state.Data.Text!.AsciiText, Is.EqualTo("Legacy"));
            Assert.That(state.Data.Text.UnicodeText, Is.EqualTo("Local"));
            Assert.That(state.Data.Text.UnicodeLanguageCode, Is.EqualTo(0x656e5553));
            Assert.That(state.Data.Text.ScriptCodeText, Is.EqualTo("Mac"));
        });
    }

    [Test]
    public void TextDescriptionSerializesCanonicalFieldsWhenOriginalBytesAreAbsent()
    {
        IccTextData source = new IccTextData();
        source.TypeSignature = IccTypeSignatures.TextDescription;
        source.Text = "\u56fd\u969b";
        source.AsciiText = "Plain";
        source.UnicodeText = "\u56fd\u969b";
        source.UnicodeLanguageCode = 0x6a61504e;
        source.ScriptCode = 1;
        source.ScriptCodeText = "Kana";

        byte[] payload = LegacyTagCodec.SerializeTextDescription(source);
        TagDecodeState state = DecodeTextDescription(payload, false);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(payload, 0), Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(ReadUInt32(payload, 8), Is.EqualTo(6));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Text!.AsciiText, Is.EqualTo("Plain"));
            Assert.That(state.Data.Text.UnicodeText, Is.EqualTo("\u56fd\u969b"));
            Assert.That(state.Data.Text.UnicodeLanguageCode, Is.EqualTo(0x6a61504e));
            Assert.That(state.Data.Text.ScriptCode, Is.EqualTo(1));
            Assert.That(state.Data.Text.ScriptCodeText, Is.EqualTo("Kana"));
        });
    }

    [Test]
    public void TextDescriptionRejectsStructurallyTruncatedPayload()
    {
        byte[] payload = new byte[89];
        WriteUInt32(payload, 0, IccTypeSignatures.TextDescription);

        TagDecodeState state = DecodeTextDescription(payload, true);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(state.Result.Message, Does.Contain("at least 90 bytes"));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
        });
    }

    [Test]
    public void TextDescriptionCanonicalWriterRejectsUnencodableFields()
    {
        IccTextData nonAscii = new IccTextData();
        nonAscii.AsciiText = "caf\u00e9";
        IccTextData longScript = new IccTextData();
        longScript.ScriptCodeText = new string('x', 67);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => LegacyTagCodec.SerializeTextDescription(nonAscii));
            Assert.Throws<IccProfileWriteException>(() => LegacyTagCodec.SerializeTextDescription(longScript));
        });
    }

    private static TagDecodeState DecodeTextDescription(byte[] payload, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, IccVersionKind.Version2, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static byte[] CreateTextDescription(string ascii, string unicode, uint languageCode, ushort scriptCode, string script)
    {
        int asciiCount = ascii.Length + 1;
        int unicodeCount = unicode.Length == 0 ? 0 : unicode.Length + 1;
        byte[] payload = new byte[90 + asciiCount + (unicodeCount * 2)];
        WriteUInt32(payload, 0, IccTypeSignatures.TextDescription);
        WriteUInt32(payload, 8, (uint)asciiCount);
        int offset = 12;
        int index = 0;
        while (index < ascii.Length)
        {
            payload[offset + index] = (byte)ascii[index];
            index++;
        }

        offset += asciiCount;
        WriteUInt32(payload, offset, languageCode);
        WriteUInt32(payload, offset + 4, (uint)unicodeCount);
        offset += 8;
        index = 0;
        while (index < unicode.Length)
        {
            WriteUInt16(payload, offset + (index * 2), unicode[index]);
            index++;
        }

        offset += unicodeCount * 2;
        WriteUInt16(payload, offset, scriptCode);
        payload[offset + 2] = script.Length == 0 ? (byte)0 : (byte)(script.Length + 1);
        index = 0;
        while (index < script.Length)
        {
            payload[offset + 3 + index] = (byte)script[index];
            index++;
        }

        return payload;
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
    }

    private static void WriteUInt16(byte[] destination, int offset, ushort value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
