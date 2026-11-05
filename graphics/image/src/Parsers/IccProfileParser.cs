using System;
using System.Collections.Generic;
using System.Text;

namespace image;

internal class IccProfileParser
{
    internal const int HeaderLength = 128;
    internal const int TagTableOffset = 128;

    internal static void ParseProfile(ImageMetadataDocument document, MetadataProfile profile, ImageMetadataReaderOptions? options, string pathPrefix, string blockType)
    {
        if (document == null || profile == null || profile.Data == null || profile.Data.Length < HeaderLength)
        {
            return;
        }

        if (options != null && !options.ParseIcc)
        {
            return;
        }

        int maxTagCount = options == null ? 65536 : options.MaxTagCount;
        Parse(document, profile, pathPrefix, blockType, maxTagCount);
    }

    internal static void Parse(ImageMetadataDocument document, MetadataProfile profile, string pathPrefix, string blockType, int maxTagCount)
    {
        byte[] data = profile.Data;
        ulong declaredSize = ReadUnsigned32(data, 0);
        if (declaredSize < HeaderLength)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "icc.size.invalid", "ICC profile declared a size smaller than its header.", blockType, profile.Offset, profile.Length);
            return;
        }

        if (ReadSignature(data, 36) != "acsp")
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "icc.signature.invalid", "ICC profile header signature was not acsp.", blockType, profile.Offset + 36, 4);
            return;
        }

        int profileLength = data.Length;
        if (declaredSize < (ulong)profileLength)
        {
            profileLength = (int)declaredSize;
        }
        else if (declaredSize > (ulong)data.Length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "icc.profile.truncated", "ICC profile ended before its declared size.", blockType, profile.Offset, (long)declaredSize);
        }

        IccParserState state = new IccParserState(document, profile);
        state.Data = data;
        state.ProfileLength = profileLength;
        state.PathPrefix = pathPrefix == null || pathPrefix.Length == 0 ? "ICC" : pathPrefix;
        state.BlockType = blockType == null || blockType.Length == 0 ? "ICC" : blockType;
        state.MaxTagCount = maxTagCount;
        ParseHeader(state, declaredSize);
        ParseTagTable(state);
    }

    internal static void ParseHeader(IccParserState state, ulong declaredSize)
    {
        MetadataDirectory directory = AddDirectory(state, "Header", "ICC Profile Header", state.PathPrefix + "/Header", state.Profile.Offset, HeaderLength);
        AddTag(state, directory, "Icc.Header.ProfileSize", "Profile Size", MetadataValue.FromUnsignedInteger(declaredSize), 0, 4, declaredSize.ToString());
        AddSignatureTag(state, directory, "Icc.Header.PreferredCmm", "Preferred CMM", 4, "");
        AddTag(state, directory, "Icc.Header.Version", "Profile Version", MetadataValue.FromString(VersionText(state.Data)), 8, 4, VersionText(state.Data));
        AddSignatureTag(state, directory, "Icc.Header.DeviceClass", "Device Class", 12, DeviceClassName(ReadSignature(state.Data, 12)));
        AddSignatureTag(state, directory, "Icc.Header.ColorSpace", "Data Color Space", 16, ColorSpaceName(ReadSignature(state.Data, 16)));
        AddSignatureTag(state, directory, "Icc.Header.ConnectionSpace", "Profile Connection Space", 20, ColorSpaceName(ReadSignature(state.Data, 20)));
        string created = DateText(state.Data);
        AddTag(state, directory, "Icc.Header.CreationDate", "Creation Date", MetadataValue.FromDateText(created), 24, 12, created);
        AddSignatureTag(state, directory, "Icc.Header.Signature", "Profile Signature", 36, "ICC profile");
        AddSignatureTag(state, directory, "Icc.Header.Platform", "Primary Platform", 40, PlatformName(ReadSignature(state.Data, 40)));
        AddTag(state, directory, "Icc.Header.Flags", "Profile Flags", MetadataValue.FromUnsignedInteger(ReadUnsigned32(state.Data, 44)), 44, 4, "");
        AddSignatureTag(state, directory, "Icc.Header.Manufacturer", "Device Manufacturer", 48, "");
        AddSignatureTag(state, directory, "Icc.Header.Model", "Device Model", 52, "");
        AddTag(state, directory, "Icc.Header.Attributes", "Device Attributes", MetadataValue.FromUnsignedInteger(ReadUnsigned64(state.Data, 56)), 56, 8, "");
        ulong intent = ReadUnsigned32(state.Data, 64);
        AddTag(state, directory, "Icc.Header.RenderingIntent", "Rendering Intent", MetadataValue.FromUnsignedInteger(intent), 64, 4, RenderingIntentName(intent));
        AddFixedTag(state, directory, "Icc.Header.IlluminantX", "PCS Illuminant X", 68);
        AddFixedTag(state, directory, "Icc.Header.IlluminantY", "PCS Illuminant Y", 72);
        AddFixedTag(state, directory, "Icc.Header.IlluminantZ", "PCS Illuminant Z", 76);
        AddSignatureTag(state, directory, "Icc.Header.Creator", "Profile Creator", 80, "");
        AddTag(state, directory, "Icc.Header.ProfileId", "Profile ID", MetadataValue.FromString(Hex(state.Data, 84, 16)), 84, 16, Hex(state.Data, 84, 16));
    }

    internal static void ParseTagTable(IccParserState state)
    {
        if (state.ProfileLength == HeaderLength)
        {
            return;
        }

        if (state.ProfileLength < TagTableOffset + 4)
        {
            MetadataDiagnostic.AddTruncated(state.Document.Diagnostics, "icc.tag-table.truncated", "ICC tag table count was truncated.", state.BlockType, state.Profile.Offset + TagTableOffset, 4);
            return;
        }

        ulong countValue = ReadUnsigned32(state.Data, TagTableOffset);
        MetadataDirectory table = AddDirectory(state, "TagTable", "ICC Tag Table", state.PathPrefix + "/TagTable", state.Profile.Offset + TagTableOffset, state.ProfileLength - TagTableOffset);
        AddTag(state, table, "Icc.TagTable.Count", "Tag Count", MetadataValue.FromUnsignedInteger(countValue), TagTableOffset, 4, countValue.ToString());
        ulong tableLength = 4 + countValue * 12;
        if (countValue > 178956970 || tableLength > (ulong)(state.ProfileLength - TagTableOffset))
        {
            MetadataDiagnostic.AddTruncated(state.Document.Diagnostics, "icc.tag-table.entries.truncated", "ICC tag table entries exceeded the profile data.", state.BlockType, state.Profile.Offset + TagTableOffset, state.ProfileLength - TagTableOffset);
            return;
        }

        int count = (int)countValue;
        int index = 0;
        while (index < count)
        {
            int entryOffset = TagTableOffset + 4 + index * 12;
            IccTagEntry entry = ReadEntry(state, entryOffset);
            ParseEntry(state, table, entry, entryOffset, index);
            index = index + 1;
        }
    }

    internal static IccTagEntry ReadEntry(IccParserState state, int entryOffset)
    {
        IccTagEntry entry = new IccTagEntry();
        entry.Signature = ReadSignature(state.Data, entryOffset);
        ulong offset = ReadUnsigned32(state.Data, entryOffset + 4);
        ulong size = ReadUnsigned32(state.Data, entryOffset + 8);
        if (offset <= 2147483647)
        {
            entry.Offset = (int)offset;
        }
        else
        {
            entry.Offset = -1;
        }

        if (size <= 2147483647)
        {
            entry.Size = (int)size;
        }
        else
        {
            entry.Size = -1;
        }

        if (entry.Offset >= 0 && entry.Size >= 4 && IsRange(state, entry.Offset, entry.Size))
        {
            entry.Type = ReadSignature(state.Data, entry.Offset);
        }

        return entry;
    }

    internal static void ParseEntry(IccParserState state, MetadataDirectory table, IccTagEntry entry, int entryOffset, int index)
    {
        string safeSignature = SafeSignature(entry.Signature);
        MetadataDirectory directory = table.AddChildWithName(MetadataFamily.Icc, entry.Signature, TagName(entry.Signature));
        directory.Path = table.Path + "/" + safeSignature;
        directory.Offset = state.Profile.Offset + entry.Offset;
        directory.Length = entry.Size < 0 ? 0 : entry.Size;
        directory.RepeatIndex = index;
        string prefix = "Icc.Tag." + safeSignature;
        AddTag(state, directory, prefix + ".Signature", "Tag Signature", MetadataValue.FromString(entry.Signature), entryOffset, 4, TagName(entry.Signature));
        AddTag(state, directory, prefix + ".Offset", "Data Offset", MetadataValue.FromInteger(entry.Offset), entryOffset + 4, 4, entry.Offset.ToString());
        AddTag(state, directory, prefix + ".Size", "Data Size", MetadataValue.FromInteger(entry.Size), entryOffset + 8, 4, entry.Size.ToString());
        if (entry.Offset < 0 || entry.Size < 0 || !IsRange(state, entry.Offset, entry.Size))
        {
            MetadataDiagnostic.AddTruncated(state.Document.Diagnostics, "icc.tag.range.invalid", "ICC tag data range exceeded the profile.", state.BlockType, state.Profile.Offset + entryOffset, 12);
            return;
        }

        AddTag(state, directory, prefix + ".Type", "Data Type", MetadataValue.FromString(entry.Type), entry.Offset, entry.Size < 4 ? entry.Size : 4, TypeName(entry.Type));
        if (entry.Signature == "desc")
        {
            ParseTextTag(state, directory, entry, "Icc.desc.Description", "Profile Description");
        }
        else if (entry.Signature == "cprt")
        {
            ParseTextTag(state, directory, entry, "Icc.cprt.Copyright", "Copyright");
        }
        else if (entry.Signature == "wtpt")
        {
            ParseWhitePoint(state, directory, entry);
        }
    }

    internal static void ParseTextTag(IccParserState state, MetadataDirectory directory, IccTagEntry entry, string key, string name)
    {
        if (entry.Type == "desc")
        {
            string text = ReadDescription(state.Data, entry.Offset, entry.Size);
            if (text.Length > 0)
            {
                AddTag(state, directory, key, name, MetadataValue.FromString(text), entry.Offset, entry.Size, text);
            }
        }
        else if (entry.Type == "text")
        {
            string text = ReadAsciiNullTerminated(state.Data, entry.Offset + 8, entry.Size - 8);
            if (text.Length > 0)
            {
                AddTag(state, directory, key, name, MetadataValue.FromString(text), entry.Offset, entry.Size, text);
            }
        }
        else if (entry.Type == "mluc")
        {
            ParseMultilingualText(state, directory, entry, key, name);
        }
    }

    internal static void ParseMultilingualText(IccParserState state, MetadataDirectory directory, IccTagEntry entry, string key, string name)
    {
        if (entry.Size < 16)
        {
            MetadataDiagnostic.AddTruncated(state.Document.Diagnostics, "icc.mluc.truncated", "ICC multilingual text header was truncated.", state.BlockType, state.Profile.Offset + entry.Offset, entry.Size);
            return;
        }

        ulong countValue = ReadUnsigned32(state.Data, entry.Offset + 8);
        ulong recordSizeValue = ReadUnsigned32(state.Data, entry.Offset + 12);
        if (countValue > 178956970 || recordSizeValue < 12 || recordSizeValue > 2147483647 || 16 + countValue * recordSizeValue > (ulong)entry.Size)
        {
            MetadataDiagnostic.AddTruncated(state.Document.Diagnostics, "icc.mluc.records.invalid", "ICC multilingual text records exceeded the tag data.", state.BlockType, state.Profile.Offset + entry.Offset, entry.Size);
            return;
        }

        List<string> languages = new List<string>();
        List<string> values = new List<string>();
        int count = (int)countValue;
        int recordSize = (int)recordSizeValue;
        int index = 0;
        while (index < count)
        {
            int recordOffset = entry.Offset + 16 + index * recordSize;
            string language = TwoAscii(state.Data, recordOffset) + "-" + TwoAscii(state.Data, recordOffset + 2);
            ulong lengthValue = ReadUnsigned32(state.Data, recordOffset + 4);
            ulong textOffsetValue = ReadUnsigned32(state.Data, recordOffset + 8);
            if (lengthValue <= 2147483647 && textOffsetValue <= 2147483647)
            {
                int length = (int)lengthValue;
                int textOffset = (int)textOffsetValue;
                if ((length & 1) == 0 && IsRange(state, textOffset, length))
                {
                    languages.Add(language);
                    values.Add(Encoding.BigEndianUnicode.GetString(state.Data, textOffset, length));
                }
                else
                {
                    MetadataDiagnostic.AddTruncated(state.Document.Diagnostics, "icc.mluc.text.range.invalid", "ICC multilingual text value exceeded the profile.", state.BlockType, state.Profile.Offset + textOffset, length);
                }
            }

            index = index + 1;
        }

        if (values.Count == 0)
        {
            return;
        }

        int selected = PreferredLanguageIndex(languages);
        AddTag(state, directory, key, name, MetadataValue.FromString(values[selected]), entry.Offset, entry.Size, values[selected]);
        index = 0;
        while (index < values.Count)
        {
            string language = SafeLanguage(languages[index]);
            AddTag(state, directory, key + "." + language, name + " [" + languages[index] + "]", MetadataValue.FromString(values[index]), entry.Offset, entry.Size, values[index]);
            index = index + 1;
        }
    }

    internal static void ParseWhitePoint(IccParserState state, MetadataDirectory directory, IccTagEntry entry)
    {
        if (entry.Type != "XYZ " || entry.Size < 20)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(state.Document.Diagnostics, "icc.wtpt.type.invalid", "ICC media white point did not contain one XYZ value.", state.BlockType, state.Profile.Offset + entry.Offset, entry.Size);
            return;
        }

        AddFixedTag(state, directory, "Icc.wtpt.X", "Media White Point X", entry.Offset + 8);
        AddFixedTag(state, directory, "Icc.wtpt.Y", "Media White Point Y", entry.Offset + 12);
        AddFixedTag(state, directory, "Icc.wtpt.Z", "Media White Point Z", entry.Offset + 16);
    }

    internal static MetadataDirectory AddDirectory(IccParserState state, string group, string name, string path, long offset, long length)
    {
        MetadataDirectory directory = state.Document.AddDirectoryWithName(MetadataFamily.Icc, group, name);
        directory.Path = path;
        directory.Offset = offset;
        directory.Length = length;
        return directory;
    }

    internal static void AddSignatureTag(IccParserState state, MetadataDirectory directory, string key, string name, int offset, string display)
    {
        string signature = ReadSignature(state.Data, offset);
        AddTag(state, directory, key, name, MetadataValue.FromString(signature), offset, 4, display.Length == 0 ? signature.Trim() : display);
    }

    internal static void AddFixedTag(IccParserState state, MetadataDirectory directory, string key, string name, int offset)
    {
        long raw = ReadSigned32(state.Data, offset);
        AddTag(state, directory, key, name, MetadataValue.FromSignedRational(raw, 65536), offset, 4, FixedText(raw));
    }

    internal static void AddTag(IccParserState state, MetadataDirectory directory, string key, string name, MetadataValue value, int relativeOffset, long length, string display)
    {
        if (state.MaxTagCount <= 0 || state.TagCount >= state.MaxTagCount)
        {
            if (!state.TagLimitReported)
            {
                MetadataDiagnostic.AddLimitExceeded(state.Document.Diagnostics, "icc.tag.limit", "ICC tag count exceeded the configured limit.", state.BlockType, state.Profile.Offset, state.Profile.Length);
                state.TagLimitReported = true;
            }

            return;
        }

        MetadataTag tag = MetadataTag.Create(key, name, MetadataFamily.Icc, value);
        tag.Group = directory.Group;
        tag.Offset = state.Profile.Offset + relativeOffset;
        tag.Length = length;
        tag.Confidence = 100;
        tag.DisplayValue = display;
        directory.AddTag(tag);
        state.TagCount = state.TagCount + 1;
    }

    internal static bool IsRange(IccParserState state, int offset, int length)
    {
        return offset >= 0 && length >= 0 && offset <= state.ProfileLength && length <= state.ProfileLength - offset;
    }

    internal static ulong ReadUnsigned32(byte[] data, int offset)
    {
        return ((ulong)data[offset] << 24) | ((ulong)data[offset + 1] << 16) | ((ulong)data[offset + 2] << 8) | data[offset + 3];
    }

    internal static ulong ReadUnsigned64(byte[] data, int offset)
    {
        return (ReadUnsigned32(data, offset) << 32) | ReadUnsigned32(data, offset + 4);
    }

    internal static long ReadSigned32(byte[] data, int offset)
    {
        ulong value = ReadUnsigned32(data, offset);
        return value >= 2147483648 ? (long)value - 4294967296 : (long)value;
    }

    internal static string ReadSignature(byte[] data, int offset)
    {
        return Encoding.ASCII.GetString(data, offset, 4);
    }

    internal static string ReadDescription(byte[] data, int offset, int size)
    {
        if (size < 12)
        {
            return "";
        }

        ulong countValue = ReadUnsigned32(data, offset + 8);
        if (countValue == 0 || countValue > 2147483647)
        {
            return "";
        }

        int count = (int)countValue;
        if (count > size - 12)
        {
            count = size - 12;
        }

        return ReadAsciiNullTerminated(data, offset + 12, count);
    }

    internal static string ReadAsciiNullTerminated(byte[] data, int offset, int length)
    {
        if (length <= 0)
        {
            return "";
        }

        int count = 0;
        while (count < length && data[offset + count] != 0)
        {
            count = count + 1;
        }

        return Encoding.ASCII.GetString(data, offset, count);
    }

    internal static int PreferredLanguageIndex(List<string> languages)
    {
        int index = 0;
        while (index < languages.Count)
        {
            if (string.Equals(languages[index], "en-US", StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }

            index = index + 1;
        }

        index = 0;
        while (index < languages.Count)
        {
            if (languages[index].StartsWith("en-", StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static string TwoAscii(byte[] data, int offset)
    {
        return Encoding.ASCII.GetString(data, offset, 2);
    }

    internal static string VersionText(byte[] data)
    {
        int major = data[8];
        int minor = data[9] >> 4;
        int bugfix = data[9] & 15;
        return major.ToString() + "." + minor.ToString() + "." + bugfix.ToString();
    }

    internal static string DateText(byte[] data)
    {
        return ReadUnsigned16(data, 24).ToString("D4") + "-" + ReadUnsigned16(data, 26).ToString("D2") + "-" + ReadUnsigned16(data, 28).ToString("D2") + "T" + ReadUnsigned16(data, 30).ToString("D2") + ":" + ReadUnsigned16(data, 32).ToString("D2") + ":" + ReadUnsigned16(data, 34).ToString("D2");
    }

    internal static int ReadUnsigned16(byte[] data, int offset)
    {
        return (data[offset] << 8) | data[offset + 1];
    }

    internal static string Hex(byte[] data, int offset, int length)
    {
        string text = "";
        int index = 0;
        while (index < length)
        {
            text = text + data[offset + index].ToString("x2");
            index = index + 1;
        }

        return text;
    }

    internal static string FixedText(long raw)
    {
        long whole = raw / 65536;
        long fraction = raw % 65536;
        if (fraction < 0)
        {
            fraction = -fraction;
        }

        long decimalPart = fraction * 1000000 / 65536;
        string sign = raw < 0 && whole == 0 ? "-" : "";
        return sign + whole.ToString() + "." + decimalPart.ToString("D6").TrimEnd('0').PadRight(1, '0');
    }

    internal static string SafeSignature(string signature)
    {
        if (signature == null || signature.Length == 0)
        {
            return "unknown";
        }

        char[] characters = signature.ToCharArray();
        int index = 0;
        while (index < characters.Length)
        {
            if (!char.IsLetterOrDigit(characters[index]))
            {
                characters[index] = '_';
            }

            index = index + 1;
        }

        return new string(characters);
    }

    internal static string SafeLanguage(string language)
    {
        if (language == null || language.Length == 0)
        {
            return "und";
        }

        char[] characters = language.ToCharArray();
        int index = 0;
        while (index < characters.Length)
        {
            if (!char.IsLetterOrDigit(characters[index]) && characters[index] != '-')
            {
                characters[index] = '_';
            }

            index = index + 1;
        }

        return new string(characters);
    }

    internal static string DeviceClassName(string signature)
    {
        if (signature == "scnr") return "Input device";
        if (signature == "mntr") return "Display device";
        if (signature == "prtr") return "Output device";
        if (signature == "link") return "Device link";
        if (signature == "spac") return "Color space";
        if (signature == "abst") return "Abstract";
        if (signature == "nmcl") return "Named color";
        return signature.Trim();
    }

    internal static string ColorSpaceName(string signature)
    {
        if (signature == "XYZ ") return "CIE XYZ";
        if (signature == "Lab ") return "CIELAB";
        if (signature == "Luv ") return "CIELUV";
        if (signature == "YCbr") return "YCbCr";
        if (signature == "Yxy ") return "CIE Yxy";
        if (signature == "RGB ") return "RGB";
        if (signature == "GRAY") return "Gray";
        if (signature == "HSV ") return "HSV";
        if (signature == "HLS ") return "HLS";
        if (signature == "CMYK") return "CMYK";
        if (signature == "CMY ") return "CMY";
        return signature.Trim();
    }

    internal static string PlatformName(string signature)
    {
        if (signature == "APPL") return "Apple";
        if (signature == "MSFT") return "Microsoft";
        if (signature == "SGI ") return "Silicon Graphics";
        if (signature == "SUNW") return "Sun Microsystems";
        return signature.Trim();
    }

    internal static string RenderingIntentName(ulong intent)
    {
        if (intent == 0) return "Perceptual";
        if (intent == 1) return "Media-relative colorimetric";
        if (intent == 2) return "Saturation";
        if (intent == 3) return "ICC-absolute colorimetric";
        return "Unknown";
    }

    internal static string TagName(string signature)
    {
        if (signature == "desc") return "Profile Description";
        if (signature == "cprt") return "Copyright";
        if (signature == "wtpt") return "Media White Point";
        return signature.Trim();
    }

    internal static string TypeName(string signature)
    {
        if (signature == "desc") return "Profile description";
        if (signature == "mluc") return "Multi-localized Unicode";
        if (signature == "text") return "Text";
        if (signature == "XYZ ") return "XYZ";
        return signature.Trim();
    }
}
