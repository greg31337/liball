using System.Collections.Generic;

namespace jpeg;

internal class JpegIccProfileChunkParser
{
    internal bool IsIccProfile(byte[]? payload)
    {
        if (payload == null)
        {
            return false;
        }

        if (payload.Length < 12)
        {
            return false;
        }

        return payload[0] == 0x49
            && payload[1] == 0x43
            && payload[2] == 0x43
            && payload[3] == 0x5F
            && payload[4] == 0x50
            && payload[5] == 0x52
            && payload[6] == 0x4F
            && payload[7] == 0x46
            && payload[8] == 0x49
            && payload[9] == 0x4C
            && payload[10] == 0x45
            && payload[11] == 0;
    }

    internal JpegIccProfileChunk Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!IsIccProfile(payload))
        {
            throw JpegInvalidDataException.Create("APP2 payload is not an ICC profile chunk.");
        }

        if (payload.Length < 14)
        {
            throw JpegInvalidDataException.Create("ICC APP2 payload is too short.");
        }

        JpegIccProfileChunk chunk = new JpegIccProfileChunk();
        chunk.IsPresent = true;
        chunk.SequenceNumber = payload[12];
        chunk.ChunkCount = payload[13];
        ValidateChunk(chunk);
        CopyChunkData(chunk, payload);
        return chunk;
    }

    internal void ValidateChunk(JpegIccProfileChunk chunk)
    {
        if (chunk.SequenceNumber < 1)
        {
            throw JpegInvalidDataException.Create("ICC chunk sequence number must be at least 1.");
        }

        if (chunk.ChunkCount < 1)
        {
            throw JpegInvalidDataException.Create("ICC chunk count must be at least 1.");
        }

        if (chunk.SequenceNumber > chunk.ChunkCount)
        {
            throw JpegInvalidDataException.Create("ICC chunk sequence number cannot exceed the chunk count.");
        }
    }

    internal void CopyChunkData(JpegIccProfileChunk chunk, byte[] payload)
    {
        int dataLength = payload.Length - 14;
        chunk.Data = new byte[dataLength];
        if (dataLength > 0)
        {
            System.Array.Copy(payload, 14, chunk.Data, 0, dataLength);
        }
    }

    internal void ValidateSequence(List<JpegIccProfileChunk> chunks)
    {
        if (chunks == null)
        {
            throw new System.ArgumentNullException("chunks");
        }

        if (chunks.Count == 0)
        {
            return;
        }

        ValidateChunk(chunks[0]);
        int expectedCount = chunks[0].ChunkCount;
        bool[] seen = new bool[expectedCount + 1];
        foreach (JpegIccProfileChunk chunk in chunks)
        {
            ValidateChunk(chunk);
            if (chunk.ChunkCount != expectedCount)
            {
                throw JpegInvalidDataException.Create("ICC chunks must agree on the total chunk count.");
            }

            if (seen[chunk.SequenceNumber])
            {
                throw JpegInvalidDataException.Create("ICC chunk sequence contains a duplicate chunk.");
            }

            seen[chunk.SequenceNumber] = true;
        }

        int sequenceNumber = 1;
        while (sequenceNumber <= expectedCount)
        {
            if (!seen[sequenceNumber])
            {
                throw JpegInvalidDataException.Create("ICC chunk sequence is missing a chunk.");
            }

            sequenceNumber = sequenceNumber + 1;
        }
    }
}

internal class JpegIccProfileParser
{
    internal bool IsProfile(byte[]? profile)
    {
        if (profile == null || profile.Length < 132)
        {
            return false;
        }

        long profileSize = JpegByteReader.ReadUInt32(profile, 0);
        if (profileSize != profile.Length)
        {
            return false;
        }

        if (profile[36] != 0x61 || profile[37] != 0x63 || profile[38] != 0x73 || profile[39] != 0x70)
        {
            return false;
        }

        long tagCount = JpegByteReader.ReadUInt32(profile, 128);
        if (tagCount > 65535 || 132 + tagCount * 12 > profileSize)
        {
            return false;
        }

        int index = 0;
        while (index < tagCount)
        {
            int entryOffset = 132 + index * 12;
            long offset = JpegByteReader.ReadUInt32(profile, entryOffset + 4);
            long size = JpegByteReader.ReadUInt32(profile, entryOffset + 8);
            if (offset > profileSize || size > profileSize - offset)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal JpegIccProfile Parse(byte[] profile)
    {
        if (profile == null)
        {
            throw new System.ArgumentNullException("profile");
        }

        if (profile.Length < 132)
        {
            throw JpegInvalidDataException.Create("ICC profile must contain a 128-byte header and tag count.");
        }

        JpegIccProfile icc = new JpegIccProfile();
        icc.IsPresent = true;
        icc.ProfileSize = JpegByteReader.ReadUInt32(profile, 0);
        ValidateProfileSize(icc.ProfileSize, profile.Length);
        icc.CmmType = ReadSignature(profile, 4);
        icc.VersionMajor = profile[8];
        icc.VersionMinor = profile[9] >> 4;
        icc.VersionBugFix = profile[9] & 15;
        icc.ProfileClass = ReadSignature(profile, 12);
        icc.ColorSpace = ReadSignature(profile, 16);
        icc.Pcs = ReadSignature(profile, 20);
        ParseDateTime(icc, profile);
        icc.Signature = ReadSignature(profile, 36);
        ValidateProfileSignature(icc.Signature);
        icc.Platform = ReadSignature(profile, 40);
        icc.Flags = JpegByteReader.ReadUInt32(profile, 44);
        icc.DeviceManufacturer = ReadSignature(profile, 48);
        icc.DeviceModel = ReadSignature(profile, 52);
        icc.RenderingIntent = JpegByteReader.ReadUInt32(profile, 64);
        icc.RenderingIntentKind = ResolveRenderingIntent(icc.RenderingIntent);
        icc.IlluminantX = ReadS15Fixed16(profile, 68);
        icc.IlluminantY = ReadS15Fixed16(profile, 72);
        icc.IlluminantZ = ReadS15Fixed16(profile, 76);
        icc.Creator = ReadSignature(profile, 80);
        System.Array.Copy(profile, 84, icc.ProfileId, 0, 16);
        ParseTagTable(icc, profile);
        ParseCicpTag(icc);
        icc.Conversion = new JpegIccMatrixTrcConverter().DescribeProfile(profile, icc);
        return icc;
    }

    internal void ParseCicpTag(JpegIccProfile icc)
    {
        foreach (JpegIccTag tag in icc.Tags)
        {
            if (tag.Signature == "cicp")
            {
                icc.HasCicp = true;
                icc.Cicp = new JpegIccCicpParser().Parse(tag);
                break;
            }
        }
    }

    internal void ParseDateTime(JpegIccProfile icc, byte[] profile)
    {
        icc.Year = JpegByteReader.ReadUInt16(profile, 24);
        icc.Month = JpegByteReader.ReadUInt16(profile, 26);
        icc.Day = JpegByteReader.ReadUInt16(profile, 28);
        icc.Hour = JpegByteReader.ReadUInt16(profile, 30);
        icc.Minute = JpegByteReader.ReadUInt16(profile, 32);
        icc.Second = JpegByteReader.ReadUInt16(profile, 34);
    }

    internal void ParseTagTable(JpegIccProfile icc, byte[] profile)
    {
        long tagCount = JpegByteReader.ReadUInt32(profile, 128);
        if (tagCount > 65535)
        {
            throw JpegInvalidDataException.Create("ICC tag count is too large.");
        }

        long tagTableLength = 132 + tagCount * 12;
        if (tagTableLength > icc.ProfileSize)
        {
            throw JpegInvalidDataException.Create("ICC tag table exceeds the profile size.");
        }

        icc.TagCount = (int)tagCount;
        int index = 0;
        while (index < icc.TagCount)
        {
            int offset = 132 + index * 12;
            JpegIccTag tag = new JpegIccTag();
            tag.Signature = ReadSignature(profile, offset);
            tag.Offset = JpegByteReader.ReadUInt32(profile, offset + 4);
            tag.Size = JpegByteReader.ReadUInt32(profile, offset + 8);
            ValidateTagBounds(tag, icc.ProfileSize);
            tag.Payload = CopyTagPayload(profile, tag);
            if (tag.Payload.Length >= 4)
            {
                tag.TypeSignature = ReadSignature(tag.Payload, 0);
            }

            icc.Tags.Add(tag);
            index = index + 1;
        }
    }

    internal byte[] CopyTagPayload(byte[] profile, JpegIccTag tag)
    {
        int size = (int)tag.Size;
        byte[] payload = new byte[size];
        if (size > 0)
        {
            System.Array.Copy(profile, (int)tag.Offset, payload, 0, size);
        }

        return payload;
    }

    internal JpegIccRenderingIntent ResolveRenderingIntent(long value)
    {
        if (value == 0)
        {
            return JpegIccRenderingIntent.Perceptual;
        }

        if (value == 1)
        {
            return JpegIccRenderingIntent.MediaRelativeColorimetric;
        }

        if (value == 2)
        {
            return JpegIccRenderingIntent.Saturation;
        }

        if (value == 3)
        {
            return JpegIccRenderingIntent.IccAbsoluteColorimetric;
        }

        return JpegIccRenderingIntent.Unknown;
    }

    internal void ValidateProfileSize(long profileSize, int actualLength)
    {
        if (profileSize != actualLength)
        {
            throw JpegInvalidDataException.Create("ICC profile size does not match the byte length.");
        }

        if (profileSize < 132)
        {
            throw JpegInvalidDataException.Create("ICC profile size is too small.");
        }
    }

    internal void ValidateProfileSignature(string signature)
    {
        if (signature != "acsp")
        {
            throw JpegInvalidDataException.Create("ICC profile signature must be acsp.");
        }
    }

    internal void ValidateTagBounds(JpegIccTag tag, long profileSize)
    {
        if (tag.Offset < 0 || tag.Size < 0)
        {
            throw JpegInvalidDataException.Create("ICC tag offset and size must be non-negative.");
        }

        if (tag.Offset > profileSize || tag.Size > profileSize - tag.Offset)
        {
            throw JpegInvalidDataException.Create("ICC tag data exceeds the profile size.");
        }
    }

    internal string ReadSignature(byte[] data, int offset)
    {
        JpegByteReader.ValidateByteArrayRange(data, offset, 4);
        return System.Text.Encoding.ASCII.GetString(data, offset, 4);
    }

    internal double ReadS15Fixed16(byte[] data, int offset)
    {
        long value = JpegByteReader.ReadUInt32(data, offset);
        if (value >= 2147483648L)
        {
            value = value - 4294967296L;
        }

        return (double)value / 65536.0;
    }
}

internal class JpegIccMatrixTrcConverter
{
    internal JpegIccMatrixTrcProfile Parse(byte[] profile)
    {
        if (profile == null)
        {
            throw new System.ArgumentNullException("profile");
        }

        JpegIccProfileParser parser = new JpegIccProfileParser();
        JpegIccProfile icc = parser.Parse(profile);
        return ParseProfile(profile, icc);
    }

    internal JpegIccMatrixTrcProfile ParseProfile(byte[] profile, JpegIccProfile icc)
    {
        if (profile == null)
        {
            throw new System.ArgumentNullException("profile");
        }

        if (icc == null)
        {
            throw new System.ArgumentNullException("icc");
        }

        if (icc.ColorSpace == "RGB ")
        {
            return ParseRgbProfile(profile, icc);
        }

        if (icc.ColorSpace == "GRAY")
        {
            return ParseGrayProfile(profile, icc);
        }

        if (icc.ColorSpace == "CMYK")
        {
            return ParseCmykProfile(profile, icc);
        }

        throw JpegInvalidDataException.Create("ICC matrix/TRC conversion supports only RGB, Gray, and CMYK profiles.");
    }

    internal bool CanConvert(byte[] profile)
    {
        if (profile == null)
        {
            throw new System.ArgumentNullException("profile");
        }

        JpegIccProfileParser parser = new JpegIccProfileParser();
        JpegIccProfile icc = parser.Parse(profile);
        return CanConvertProfile(profile, icc);
    }

    internal bool CanConvertProfile(byte[] profile, JpegIccProfile icc)
    {
        if (profile == null)
        {
            throw new System.ArgumentNullException("profile");
        }

        if (icc == null)
        {
            throw new System.ArgumentNullException("icc");
        }

        if (icc.ColorSpace == "RGB ")
        {
            return CanConvertRgbProfile(profile, icc);
        }

        if (icc.ColorSpace == "GRAY")
        {
            return CanConvertGrayProfile(profile, icc);
        }

        if (icc.ColorSpace == "CMYK")
        {
            return CanConvertCmykProfile(profile, icc);
        }

        return false;
    }

    internal JpegIccConversionResult DescribeProfile(byte[] profile, JpegIccProfile icc)
    {
        if (profile == null)
        {
            throw new System.ArgumentNullException("profile");
        }

        if (icc == null)
        {
            throw new System.ArgumentNullException("icc");
        }

        JpegIccConversionResult result = new JpegIccConversionResult();
        result.SourceColorSpace = ResolveSourceColorSpace(icc.ColorSpace);
        result.RenderingIntent = icc.RenderingIntentKind;
        if (result.SourceColorSpace == JpegColorSpace.RGB
            || result.SourceColorSpace == JpegColorSpace.Grayscale
            || result.SourceColorSpace == JpegColorSpace.CMYK)
        {
            result.DestinationColorSpace = JpegIccDestinationColorSpace.Xyz;
        }
        else
        {
            AddConversionDiagnostic(result, JpegIccConversionDiagnosticCode.UnsupportedColorSpace, "ICC matrix/TRC conversion supports only RGB, Gray, and CMYK profiles.", "");
        }

        if (icc.RenderingIntentKind == JpegIccRenderingIntent.Unknown)
        {
            AddConversionDiagnostic(result, JpegIccConversionDiagnosticCode.InvalidRenderingIntent, "ICC rendering intent must be in the range zero through three.", "");
        }

        if (result.SourceColorSpace == JpegColorSpace.RGB)
        {
            AddXyzTagDiagnostic(profile, icc, result, "rXYZ");
            AddXyzTagDiagnostic(profile, icc, result, "gXYZ");
            AddXyzTagDiagnostic(profile, icc, result, "bXYZ");
            AddXyzTagDiagnostic(profile, icc, result, "wtpt");
            AddCurveTagDiagnostic(profile, icc, result, "rTRC");
            AddCurveTagDiagnostic(profile, icc, result, "gTRC");
            AddCurveTagDiagnostic(profile, icc, result, "bTRC");
        }

        if (result.SourceColorSpace == JpegColorSpace.Grayscale)
        {
            AddXyzTagDiagnostic(profile, icc, result, "wtpt");
            AddCurveTagDiagnostic(profile, icc, result, "kTRC");
        }

        if (result.SourceColorSpace == JpegColorSpace.CMYK)
        {
            AddXyzTagDiagnostic(profile, icc, result, "wtpt");
            AddCurveTagDiagnostic(profile, icc, result, "cTRC");
            AddCurveTagDiagnostic(profile, icc, result, "mTRC");
            AddCurveTagDiagnostic(profile, icc, result, "yTRC");
            AddCurveTagDiagnostic(profile, icc, result, "kTRC");
        }

        if (CanConvertProfile(profile, icc))
        {
            result.MatrixTrcProfile = ParseProfile(profile, icc);
        }

        if (result.Diagnostics.Count == 0 && result.MatrixTrcProfile.IsPresent)
        {
            result.Status = JpegIccConversionStatus.Supported;
            result.Message = "The ICC profile has a supported matrix/TRC conversion to CIE XYZ.";
        }
        else
        {
            result.Status = JpegIccConversionStatus.Unsupported;
            result.Message = "The ICC profile is not supported by the built-in matrix/TRC conversion path.";
        }

        return result;
    }

    internal JpegColorSpace ResolveSourceColorSpace(string signature)
    {
        if (signature == "RGB ")
        {
            return JpegColorSpace.RGB;
        }

        if (signature == "GRAY")
        {
            return JpegColorSpace.Grayscale;
        }

        if (signature == "CMYK")
        {
            return JpegColorSpace.CMYK;
        }

        return JpegColorSpace.Unknown;
    }

    internal void AddXyzTagDiagnostic(byte[] profile, JpegIccProfile icc, JpegIccConversionResult result, string signature)
    {
        JpegIccTag? tag = FindTagOrNull(icc, signature);
        if (tag == null)
        {
            AddConversionDiagnostic(result, JpegIccConversionDiagnosticCode.MissingRequiredTag, "ICC matrix/TRC conversion requires this XYZ tag.", signature);
            return;
        }

        if (!IsXyzTag(profile, tag))
        {
            AddConversionDiagnostic(result, JpegIccConversionDiagnosticCode.UnsupportedTagType, "ICC matrix/TRC conversion requires an XYZ tag payload.", signature);
        }
    }

    internal void AddCurveTagDiagnostic(byte[] profile, JpegIccProfile icc, JpegIccConversionResult result, string signature)
    {
        JpegIccTag? tag = FindTagOrNull(icc, signature);
        if (tag == null)
        {
            AddConversionDiagnostic(result, JpegIccConversionDiagnosticCode.MissingRequiredTag, "ICC matrix/TRC conversion requires this tone-curve tag.", signature);
            return;
        }

        if (!IsCurveTagSupported(profile, tag))
        {
            AddConversionDiagnostic(result, JpegIccConversionDiagnosticCode.UnsupportedToneCurve, "ICC matrix/TRC conversion supports identity and single-gamma curve payloads.", signature);
        }
    }

    internal void AddConversionDiagnostic(JpegIccConversionResult result, JpegIccConversionDiagnosticCode code, string message, string signature)
    {
        JpegIccConversionDiagnostic diagnostic = new JpegIccConversionDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.TagSignature = signature;
        result.Diagnostics.Add(diagnostic);
    }

    internal bool CanConvertRgbProfile(byte[] profile, JpegIccProfile icc)
    {
        return IsXyzTag(profile, FindTagOrNull(icc, "rXYZ"))
            && IsXyzTag(profile, FindTagOrNull(icc, "gXYZ"))
            && IsXyzTag(profile, FindTagOrNull(icc, "bXYZ"))
            && IsXyzTag(profile, FindTagOrNull(icc, "wtpt"))
            && IsCurveTagSupported(profile, FindTagOrNull(icc, "rTRC"))
            && IsCurveTagSupported(profile, FindTagOrNull(icc, "gTRC"))
            && IsCurveTagSupported(profile, FindTagOrNull(icc, "bTRC"));
    }

    internal bool CanConvertGrayProfile(byte[] profile, JpegIccProfile icc)
    {
        return IsXyzTag(profile, FindTagOrNull(icc, "wtpt"))
            && IsCurveTagSupported(profile, FindTagOrNull(icc, "kTRC"));
    }

    internal bool CanConvertCmykProfile(byte[] profile, JpegIccProfile icc)
    {
        return IsXyzTag(profile, FindTagOrNull(icc, "wtpt"))
            && IsCurveTagSupported(profile, FindTagOrNull(icc, "cTRC"))
            && IsCurveTagSupported(profile, FindTagOrNull(icc, "mTRC"))
            && IsCurveTagSupported(profile, FindTagOrNull(icc, "yTRC"))
            && IsCurveTagSupported(profile, FindTagOrNull(icc, "kTRC"));
    }

    internal JpegIccMatrixTrcProfile ParseRgbProfile(byte[] profile, JpegIccProfile icc)
    {
        JpegIccMatrixTrcProfile matrix = new JpegIccMatrixTrcProfile();
        matrix.IsPresent = true;
        matrix.IsRgb = true;
        matrix.SourceColorSpace = JpegColorSpace.RGB;
        matrix.DestinationColorSpace = JpegIccDestinationColorSpace.Xyz;
        matrix.RenderingIntent = icc.RenderingIntentKind;
        JpegXyzColor red = ReadXyzTag(profile, FindTag(icc, "rXYZ"));
        JpegXyzColor green = ReadXyzTag(profile, FindTag(icc, "gXYZ"));
        JpegXyzColor blue = ReadXyzTag(profile, FindTag(icc, "bXYZ"));
        JpegXyzColor white = ReadXyzTag(profile, FindTag(icc, "wtpt"));
        matrix.RedX = red.X;
        matrix.RedY = red.Y;
        matrix.RedZ = red.Z;
        matrix.GreenX = green.X;
        matrix.GreenY = green.Y;
        matrix.GreenZ = green.Z;
        matrix.BlueX = blue.X;
        matrix.BlueY = blue.Y;
        matrix.BlueZ = blue.Z;
        matrix.WhiteX = white.X;
        matrix.WhiteY = white.Y;
        matrix.WhiteZ = white.Z;
        matrix.RedGamma = ReadCurveGamma(profile, FindTag(icc, "rTRC"));
        matrix.GreenGamma = ReadCurveGamma(profile, FindTag(icc, "gTRC"));
        matrix.BlueGamma = ReadCurveGamma(profile, FindTag(icc, "bTRC"));
        return matrix;
    }

    internal JpegIccMatrixTrcProfile ParseGrayProfile(byte[] profile, JpegIccProfile icc)
    {
        JpegIccMatrixTrcProfile matrix = new JpegIccMatrixTrcProfile();
        matrix.IsPresent = true;
        matrix.IsGray = true;
        matrix.SourceColorSpace = JpegColorSpace.Grayscale;
        matrix.DestinationColorSpace = JpegIccDestinationColorSpace.Xyz;
        matrix.RenderingIntent = icc.RenderingIntentKind;
        JpegXyzColor white = ReadXyzTag(profile, FindTag(icc, "wtpt"));
        matrix.WhiteX = white.X;
        matrix.WhiteY = white.Y;
        matrix.WhiteZ = white.Z;
        matrix.GrayGamma = ReadCurveGamma(profile, FindTag(icc, "kTRC"));
        return matrix;
    }

    internal JpegIccMatrixTrcProfile ParseCmykProfile(byte[] profile, JpegIccProfile icc)
    {
        JpegIccMatrixTrcProfile matrix = new JpegIccMatrixTrcProfile();
        matrix.IsPresent = true;
        matrix.IsCmyk = true;
        matrix.SourceColorSpace = JpegColorSpace.CMYK;
        matrix.DestinationColorSpace = JpegIccDestinationColorSpace.Xyz;
        matrix.RenderingIntent = icc.RenderingIntentKind;
        JpegXyzColor white = ReadXyzTag(profile, FindTag(icc, "wtpt"));
        matrix.WhiteX = white.X;
        matrix.WhiteY = white.Y;
        matrix.WhiteZ = white.Z;
        matrix.CyanGamma = ReadCurveGamma(profile, FindTag(icc, "cTRC"));
        matrix.MagentaGamma = ReadCurveGamma(profile, FindTag(icc, "mTRC"));
        matrix.YellowGamma = ReadCurveGamma(profile, FindTag(icc, "yTRC"));
        matrix.BlackGamma = ReadCurveGamma(profile, FindTag(icc, "kTRC"));
        return matrix;
    }

    internal JpegXyzColor ConvertRgbToXyz(JpegIccMatrixTrcProfile matrix, int red, int green, int blue)
    {
        if (matrix == null)
        {
            throw new System.ArgumentNullException("matrix");
        }

        if (!matrix.IsRgb)
        {
            throw JpegInvalidDataException.Create("ICC matrix/TRC profile is not RGB.");
        }

        double linearRed = ApplyGamma(NormalizeByte(red), matrix.RedGamma);
        double linearGreen = ApplyGamma(NormalizeByte(green), matrix.GreenGamma);
        double linearBlue = ApplyGamma(NormalizeByte(blue), matrix.BlueGamma);
        JpegXyzColor xyz = new JpegXyzColor();
        xyz.X = linearRed * matrix.RedX + linearGreen * matrix.GreenX + linearBlue * matrix.BlueX;
        xyz.Y = linearRed * matrix.RedY + linearGreen * matrix.GreenY + linearBlue * matrix.BlueY;
        xyz.Z = linearRed * matrix.RedZ + linearGreen * matrix.GreenZ + linearBlue * matrix.BlueZ;
        return xyz;
    }

    internal JpegXyzColor ConvertGrayToXyz(JpegIccMatrixTrcProfile matrix, int gray)
    {
        if (matrix == null)
        {
            throw new System.ArgumentNullException("matrix");
        }

        if (!matrix.IsGray)
        {
            throw JpegInvalidDataException.Create("ICC matrix/TRC profile is not Gray.");
        }

        double linearGray = ApplyGamma(NormalizeByte(gray), matrix.GrayGamma);
        JpegXyzColor xyz = new JpegXyzColor();
        xyz.X = linearGray * matrix.WhiteX;
        xyz.Y = linearGray * matrix.WhiteY;
        xyz.Z = linearGray * matrix.WhiteZ;
        return xyz;
    }

    internal JpegXyzColor ConvertCmykToXyz(JpegIccMatrixTrcProfile matrix, int cyan, int magenta, int yellow, int black)
    {
        if (matrix == null)
        {
            throw new System.ArgumentNullException("matrix");
        }

        if (!matrix.IsCmyk)
        {
            throw JpegInvalidDataException.Create("ICC matrix/TRC profile is not CMYK.");
        }

        double linearCyan = ApplyGamma(NormalizeByte(cyan), matrix.CyanGamma);
        double linearMagenta = ApplyGamma(NormalizeByte(magenta), matrix.MagentaGamma);
        double linearYellow = ApplyGamma(NormalizeByte(yellow), matrix.YellowGamma);
        double linearBlack = ApplyGamma(NormalizeByte(black), matrix.BlackGamma);
        double red = (1.0 - linearCyan) * (1.0 - linearBlack);
        double green = (1.0 - linearMagenta) * (1.0 - linearBlack);
        double blue = (1.0 - linearYellow) * (1.0 - linearBlack);
        JpegXyzColor xyz = new JpegXyzColor();
        xyz.X = (red * 0.4360747 + green * 0.3850649 + blue * 0.1430804) * matrix.WhiteX / 0.96422;
        xyz.Y = red * 0.2225045 + green * 0.7168786 + blue * 0.0606169;
        xyz.Z = (red * 0.0139322 + green * 0.0971045 + blue * 0.7141733) * matrix.WhiteZ / 0.82521;
        xyz.Y = xyz.Y * matrix.WhiteY;
        return xyz;
    }

    internal JpegIccTag FindTag(JpegIccProfile icc, string signature)
    {
        foreach (JpegIccTag tag in icc.Tags)
        {
            if (tag.Signature == signature)
            {
                return tag;
            }
        }

        throw JpegInvalidDataException.Create("ICC profile is missing a required matrix/TRC tag.");
    }

    internal JpegIccTag? FindTagOrNull(JpegIccProfile icc, string signature)
    {
        foreach (JpegIccTag tag in icc.Tags)
        {
            if (tag.Signature == signature)
            {
                return tag;
            }
        }

        return null;
    }

    internal bool IsXyzTag(byte[] profile, JpegIccTag? tag)
    {
        if (tag == null)
        {
            return false;
        }

        int offset = (int)tag.Offset;
        if (tag.Size < 20 || offset < 0 || offset > profile.Length - 20)
        {
            return false;
        }

        JpegIccProfileParser parser = new JpegIccProfileParser();
        string type = parser.ReadSignature(profile, offset);
        return type == "XYZ ";
    }

    internal bool IsCurveTagSupported(byte[] profile, JpegIccTag? tag)
    {
        if (tag == null)
        {
            return false;
        }

        int offset = (int)tag.Offset;
        if (tag.Size < 12 || offset < 0 || offset > profile.Length - 12)
        {
            return false;
        }

        JpegIccProfileParser parser = new JpegIccProfileParser();
        string type = parser.ReadSignature(profile, offset);
        if (type != "curv")
        {
            return false;
        }

        long count = JpegByteReader.ReadUInt32(profile, offset + 8);
        if (count == 0)
        {
            return true;
        }

        return count == 1 && tag.Size >= 14 && offset <= profile.Length - 14;
    }

    internal JpegXyzColor ReadXyzTag(byte[] profile, JpegIccTag tag)
    {
        int offset = (int)tag.Offset;
        if (tag.Size < 20 || offset < 0 || offset > profile.Length - 20)
        {
            throw JpegInvalidDataException.Create("ICC XYZ tag is too short.");
        }

        JpegIccProfileParser parser = new JpegIccProfileParser();
        string type = parser.ReadSignature(profile, offset);
        if (type != "XYZ ")
        {
            throw JpegInvalidDataException.Create("ICC matrix tag must use the XYZ type.");
        }

        JpegXyzColor xyz = new JpegXyzColor();
        xyz.X = parser.ReadS15Fixed16(profile, offset + 8);
        xyz.Y = parser.ReadS15Fixed16(profile, offset + 12);
        xyz.Z = parser.ReadS15Fixed16(profile, offset + 16);
        return xyz;
    }

    internal double ReadCurveGamma(byte[] profile, JpegIccTag tag)
    {
        int offset = (int)tag.Offset;
        if (tag.Size < 12 || offset < 0 || offset > profile.Length - 12)
        {
            throw JpegInvalidDataException.Create("ICC curve tag is too short.");
        }

        JpegIccProfileParser parser = new JpegIccProfileParser();
        string type = parser.ReadSignature(profile, offset);
        if (type != "curv")
        {
            throw JpegInvalidDataException.Create("ICC TRC tag must use the curv type.");
        }

        long count = JpegByteReader.ReadUInt32(profile, offset + 8);
        if (count == 0)
        {
            return 1.0;
        }

        if (count != 1)
        {
            throw JpegInvalidDataException.Create("ICC TRC curve tables are not supported by matrix/TRC conversion.");
        }

        if (tag.Size < 14 || offset > profile.Length - 14)
        {
            throw JpegInvalidDataException.Create("ICC single-gamma curve tag is too short.");
        }

        int gamma = JpegByteReader.ReadUInt16(profile, offset + 12);
        return (double)gamma / 256.0;
    }

    internal double NormalizeByte(int value)
    {
        if (value < 0 || value > 255)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        return (double)value / 255.0;
    }

    internal double ApplyGamma(double value, double gamma)
    {
        return System.Math.Pow(value, gamma);
    }
}

internal class JpegIccPolicyEvaluator
{
    internal JpegIccPolicyResult Apply(byte[] profile, JpegIccBehaviorPolicy policy)
    {
        if (profile == null)
        {
            throw new System.ArgumentNullException("profile");
        }

        JpegIccPolicyResult result = new JpegIccPolicyResult();
        result.HasProfile = profile.Length > 0;
        if (!result.HasProfile)
        {
            return result;
        }

        if (policy == JpegIccBehaviorPolicy.Ignore)
        {
            result.Ignored = true;
            return result;
        }

        if (policy == JpegIccBehaviorPolicy.Preserve)
        {
            result.Preserved = true;
            result.PreservedProfile = CopyProfile(profile);
            return result;
        }

        JpegIccProfileParser parser = new JpegIccProfileParser();
        JpegIccProfile icc = parser.Parse(profile);
        result.Icc = icc;
        result.ConversionAttempted = true;
        if (icc.Conversion.Status == JpegIccConversionStatus.Supported)
        {
            result.ConversionSupported = true;
            result.MatrixTrcProfile = icc.Conversion.MatrixTrcProfile;
            return result;
        }

        result.Unsupported = true;
        result.Message = icc.Conversion.Message;
        if (policy == JpegIccBehaviorPolicy.FailIfUnsupported)
        {
            result.ShouldFail = true;
            throw JpegInvalidDataException.Create(result.Message);
        }

        result.Preserved = true;
        result.PreservedProfile = CopyProfile(profile);
        return result;
    }

    internal byte[] CopyProfile(byte[] profile)
    {
        byte[] copy = new byte[profile.Length];
        if (profile.Length > 0)
        {
            System.Array.Copy(profile, 0, copy, 0, profile.Length);
        }

        return copy;
    }
}

internal class JpegIccProfileAssembler
{
    internal byte[] Reassemble(List<JpegIccProfileChunk> chunks)
    {
        if (chunks == null)
        {
            throw new System.ArgumentNullException("chunks");
        }

        JpegIccProfileChunkParser parser = new JpegIccProfileChunkParser();
        parser.ValidateSequence(chunks);
        if (chunks.Count == 0)
        {
            return new byte[0];
        }

        int totalLength = GetTotalLength(chunks);
        byte[] profile = new byte[totalLength];
        int offset = 0;
        int sequenceNumber = 1;
        while (sequenceNumber <= chunks[0].ChunkCount)
        {
            JpegIccProfileChunk chunk = FindChunk(chunks, sequenceNumber);
            System.Array.Copy(chunk.Data, 0, profile, offset, chunk.Data.Length);
            offset = offset + chunk.Data.Length;
            sequenceNumber = sequenceNumber + 1;
        }

        return profile;
    }

    internal int GetTotalLength(List<JpegIccProfileChunk> chunks)
    {
        int totalLength = 0;
        foreach (JpegIccProfileChunk chunk in chunks)
        {
            ValidateChunkData(chunk);
            totalLength = totalLength + chunk.Data.Length;
        }

        return totalLength;
    }

    internal JpegIccProfileChunk FindChunk(List<JpegIccProfileChunk> chunks, int sequenceNumber)
    {
        foreach (JpegIccProfileChunk chunk in chunks)
        {
            if (chunk.SequenceNumber == sequenceNumber)
            {
                ValidateChunkData(chunk);
                return chunk;
            }
        }

        throw JpegInvalidDataException.Create("ICC chunk sequence is missing a chunk.");
    }

    internal void ValidateChunkData(JpegIccProfileChunk chunk)
    {
        if (chunk.Data == null)
        {
            throw JpegInvalidDataException.Create("ICC chunk data cannot be null.");
        }
    }
}

internal class JpegIccProfileChunkWriter
{
    internal List<JpegIccProfileChunk> CreateChunks(byte[] profile)
    {
        ValidateProfile(profile);
        int chunkCount = CalculateChunkCount(profile.Length);
        List<JpegIccProfileChunk> chunks = new List<JpegIccProfileChunk>();
        int offset = 0;
        int sequenceNumber = 1;
        while (sequenceNumber <= chunkCount)
        {
            int chunkLength = profile.Length - offset;
            if (chunkLength > JpegConstants.MaximumIccProfileChunkDataLength)
            {
                chunkLength = JpegConstants.MaximumIccProfileChunkDataLength;
            }

            JpegIccProfileChunk chunk = new JpegIccProfileChunk();
            chunk.IsPresent = true;
            chunk.SequenceNumber = sequenceNumber;
            chunk.ChunkCount = chunkCount;
            chunk.Data = new byte[chunkLength];
            System.Array.Copy(profile, offset, chunk.Data, 0, chunkLength);
            chunks.Add(chunk);
            offset = offset + chunkLength;
            sequenceNumber = sequenceNumber + 1;
        }

        return chunks;
    }

    internal byte[] WritePayload(JpegIccProfileChunk chunk)
    {
        ValidateChunk(chunk);
        byte[] payload = new byte[JpegConstants.IccProfileChunkHeaderLength + chunk.Data.Length];
        WriteIccSignature(payload);
        payload[12] = (byte)chunk.SequenceNumber;
        payload[13] = (byte)chunk.ChunkCount;
        System.Array.Copy(chunk.Data, 0, payload, JpegConstants.IccProfileChunkHeaderLength, chunk.Data.Length);
        return payload;
    }

    internal void WriteMarkerSegments(JpegMarkerSegmentWriter segmentWriter, byte[] profile)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        List<JpegIccProfileChunk> chunks = CreateChunks(profile);
        foreach (JpegIccProfileChunk chunk in chunks)
        {
            byte[] payload = WritePayload(chunk);
            segmentWriter.WriteMarkerSegment(JpegMarker.App2, payload);
        }
    }

    internal void ValidateProfile(byte[] profile)
    {
        if (profile == null)
        {
            throw new System.ArgumentNullException("profile");
        }

        CalculateChunkCount(profile.Length);
    }

    internal int CalculateChunkCount(int profileLength)
    {
        if (profileLength < 1)
        {
            throw JpegWriteValidationException.Create("ICC profile data cannot be empty.");
        }

        int chunkCount = profileLength / JpegConstants.MaximumIccProfileChunkDataLength;
        if (profileLength % JpegConstants.MaximumIccProfileChunkDataLength != 0)
        {
            chunkCount = chunkCount + 1;
        }

        if (chunkCount > JpegConstants.MaximumIccProfileChunkCount)
        {
            throw JpegWriteValidationException.Create("ICC profile requires more than 255 APP2 chunks.");
        }

        return chunkCount;
    }

    internal void ValidateChunk(JpegIccProfileChunk chunk)
    {
        if (chunk == null)
        {
            throw new System.ArgumentNullException("chunk");
        }

        if (chunk.SequenceNumber < 1 || chunk.SequenceNumber > 255)
        {
            throw JpegWriteValidationException.Create("ICC chunk sequence number must fit in one byte and be at least 1.");
        }

        if (chunk.ChunkCount < 1 || chunk.ChunkCount > 255)
        {
            throw JpegWriteValidationException.Create("ICC chunk count must fit in one byte and be at least 1.");
        }

        if (chunk.SequenceNumber > chunk.ChunkCount)
        {
            throw JpegWriteValidationException.Create("ICC chunk sequence number cannot exceed the chunk count.");
        }

        if (chunk.Data == null)
        {
            throw JpegWriteValidationException.Create("ICC chunk data cannot be null.");
        }

        if (chunk.Data.Length > JpegConstants.MaximumIccProfileChunkDataLength)
        {
            throw JpegWriteValidationException.Create("ICC chunk data exceeds the maximum APP2 payload space.");
        }
    }

    internal void WriteIccSignature(byte[] payload)
    {
        payload[0] = 0x49;
        payload[1] = 0x43;
        payload[2] = 0x43;
        payload[3] = 0x5F;
        payload[4] = 0x50;
        payload[5] = 0x52;
        payload[6] = 0x4F;
        payload[7] = 0x46;
        payload[8] = 0x49;
        payload[9] = 0x4C;
        payload[10] = 0x45;
        payload[11] = 0;
    }
}
