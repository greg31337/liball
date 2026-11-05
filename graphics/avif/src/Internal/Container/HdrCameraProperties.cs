using System;

namespace avif;

internal class HdrCameraPropertyParseResult
{
    internal AvifResultCode Code;
    internal ParsedContentLightLevel? ContentLightLevel;
    internal ParsedMasteringDisplayColourVolume? MasteringDisplayColourVolume;
    internal ParsedContentColourVolume? ContentColourVolume;
    internal ParsedAmbientViewingEnvironment? AmbientViewingEnvironment;
    internal ParsedReferenceViewingEnvironment? ReferenceViewingEnvironment;
    internal ParsedNominalDiffuseWhite? NominalDiffuseWhite;
    internal ParsedCameraIntrinsicMatrix? CameraIntrinsicMatrix;
    internal ParsedCameraExtrinsicMatrix? CameraExtrinsicMatrix;

    internal HdrCameraPropertyParseResult(AvifResultCode code)
    {
        Code = code;
        ContentLightLevel = null;
        MasteringDisplayColourVolume = null;
        ContentColourVolume = null;
        AmbientViewingEnvironment = null;
        ReferenceViewingEnvironment = null;
        NominalDiffuseWhite = null;
        CameraIntrinsicMatrix = null;
        CameraExtrinsicMatrix = null;
    }
}

internal class ParsedContentLightLevel
{
    int maxContentLightLevel;
    int maxPictureAverageLightLevel;

    internal ParsedContentLightLevel(
        int maxContentLightLevel,
        int maxPictureAverageLightLevel)
    {
        this.maxContentLightLevel = maxContentLightLevel;
        this.maxPictureAverageLightLevel = maxPictureAverageLightLevel;
    }

    internal int GetMaxContentLightLevel()
    {
        return maxContentLightLevel;
    }

    internal int GetMaxPictureAverageLightLevel()
    {
        return maxPictureAverageLightLevel;
    }
}

internal class ParsedMasteringDisplayColourVolume
{
    int[] primaryX;
    int[] primaryY;
    int whitePointX;
    int whitePointY;
    uint maximumLuminance;
    uint minimumLuminance;

    internal ParsedMasteringDisplayColourVolume(
        int[] primaryX,
        int[] primaryY,
        int whitePointX,
        int whitePointY,
        uint maximumLuminance,
        uint minimumLuminance)
    {
        this.primaryX = (int[])primaryX.Clone();
        this.primaryY = (int[])primaryY.Clone();
        this.whitePointX = whitePointX;
        this.whitePointY = whitePointY;
        this.maximumLuminance = maximumLuminance;
        this.minimumLuminance = minimumLuminance;
    }

    internal int GetPrimaryX(int index)
    {
        if (index < 0 || index >= primaryX.Length)
        {
            return 0;
        }

        return primaryX[index];
    }

    internal int GetPrimaryY(int index)
    {
        if (index < 0 || index >= primaryY.Length)
        {
            return 0;
        }

        return primaryY[index];
    }

    internal int GetWhitePointX()
    {
        return whitePointX;
    }

    internal int GetWhitePointY()
    {
        return whitePointY;
    }

    internal uint GetMaximumLuminance()
    {
        return maximumLuminance;
    }

    internal uint GetMinimumLuminance()
    {
        return minimumLuminance;
    }
}

internal class ParsedContentColourVolume
{
    bool primariesPresent;
    bool minimumLuminancePresent;
    bool maximumLuminancePresent;
    bool averageLuminancePresent;
    int[] primaryX;
    int[] primaryY;
    uint minimumLuminance;
    uint maximumLuminance;
    uint averageLuminance;

    internal ParsedContentColourVolume(
        bool primariesPresent,
        bool minimumLuminancePresent,
        bool maximumLuminancePresent,
        bool averageLuminancePresent,
        int[] primaryX,
        int[] primaryY,
        uint minimumLuminance,
        uint maximumLuminance,
        uint averageLuminance)
    {
        this.primariesPresent = primariesPresent;
        this.minimumLuminancePresent = minimumLuminancePresent;
        this.maximumLuminancePresent = maximumLuminancePresent;
        this.averageLuminancePresent = averageLuminancePresent;
        this.primaryX = (int[])primaryX.Clone();
        this.primaryY = (int[])primaryY.Clone();
        this.minimumLuminance = minimumLuminance;
        this.maximumLuminance = maximumLuminance;
        this.averageLuminance = averageLuminance;
    }

    internal bool HasPrimaries()
    {
        return primariesPresent;
    }

    internal bool HasMinimumLuminance()
    {
        return minimumLuminancePresent;
    }

    internal bool HasMaximumLuminance()
    {
        return maximumLuminancePresent;
    }

    internal bool HasAverageLuminance()
    {
        return averageLuminancePresent;
    }

    internal int GetPrimaryX(int index)
    {
        if (!primariesPresent || index < 0 || index >= primaryX.Length)
        {
            return 0;
        }

        return primaryX[index];
    }

    internal int GetPrimaryY(int index)
    {
        if (!primariesPresent || index < 0 || index >= primaryY.Length)
        {
            return 0;
        }

        return primaryY[index];
    }

    internal uint GetMinimumLuminance()
    {
        return minimumLuminance;
    }

    internal uint GetMaximumLuminance()
    {
        return maximumLuminance;
    }

    internal uint GetAverageLuminance()
    {
        return averageLuminance;
    }
}

internal class ParsedAmbientViewingEnvironment
{
    uint illuminance;
    int lightX;
    int lightY;

    internal ParsedAmbientViewingEnvironment(uint illuminance, int lightX, int lightY)
    {
        this.illuminance = illuminance;
        this.lightX = lightX;
        this.lightY = lightY;
    }

    internal uint GetIlluminance()
    {
        return illuminance;
    }

    internal int GetLightX()
    {
        return lightX;
    }

    internal int GetLightY()
    {
        return lightY;
    }
}

internal class ParsedReferenceViewingEnvironment
{
    uint surroundLuminance;
    int surroundLightX;
    int surroundLightY;
    uint peripheryLuminance;
    int peripheryLightX;
    int peripheryLightY;

    internal ParsedReferenceViewingEnvironment(
        uint surroundLuminance,
        int surroundLightX,
        int surroundLightY,
        uint peripheryLuminance,
        int peripheryLightX,
        int peripheryLightY)
    {
        this.surroundLuminance = surroundLuminance;
        this.surroundLightX = surroundLightX;
        this.surroundLightY = surroundLightY;
        this.peripheryLuminance = peripheryLuminance;
        this.peripheryLightX = peripheryLightX;
        this.peripheryLightY = peripheryLightY;
    }

    internal uint GetSurroundLuminance()
    {
        return surroundLuminance;
    }

    internal int GetSurroundLightX()
    {
        return surroundLightX;
    }

    internal int GetSurroundLightY()
    {
        return surroundLightY;
    }

    internal uint GetPeripheryLuminance()
    {
        return peripheryLuminance;
    }

    internal int GetPeripheryLightX()
    {
        return peripheryLightX;
    }

    internal int GetPeripheryLightY()
    {
        return peripheryLightY;
    }
}

internal class ParsedNominalDiffuseWhite
{
    uint luminance;

    internal ParsedNominalDiffuseWhite(uint luminance)
    {
        this.luminance = luminance;
    }

    internal uint GetLuminance()
    {
        return luminance;
    }

    internal bool UsesDefaultLuminance()
    {
        return luminance == 0;
    }
}

internal class ParsedCameraIntrinsicMatrix
{
    bool fullIntrinsics;
    int denominatorShift;
    int skewDenominatorShift;
    int focalLengthX;
    int principalPointX;
    int principalPointY;
    int focalLengthY;
    int skewFactor;

    internal ParsedCameraIntrinsicMatrix(
        bool fullIntrinsics,
        int denominatorShift,
        int skewDenominatorShift,
        int focalLengthX,
        int principalPointX,
        int principalPointY,
        int focalLengthY,
        int skewFactor)
    {
        this.fullIntrinsics = fullIntrinsics;
        this.denominatorShift = denominatorShift;
        this.skewDenominatorShift = skewDenominatorShift;
        this.focalLengthX = focalLengthX;
        this.principalPointX = principalPointX;
        this.principalPointY = principalPointY;
        this.focalLengthY = focalLengthY;
        this.skewFactor = skewFactor;
    }

    internal bool HasFullIntrinsics()
    {
        return fullIntrinsics;
    }

    internal int GetDenominatorShift()
    {
        return denominatorShift;
    }

    internal uint GetDenominator()
    {
        return 1U << denominatorShift;
    }

    internal int GetSkewDenominatorShift()
    {
        return skewDenominatorShift;
    }

    internal uint GetSkewDenominator()
    {
        return 1U << skewDenominatorShift;
    }

    internal int GetFocalLengthX()
    {
        return focalLengthX;
    }

    internal int GetPrincipalPointX()
    {
        return principalPointX;
    }

    internal int GetPrincipalPointY()
    {
        return principalPointY;
    }

    internal int GetFocalLengthY()
    {
        if (fullIntrinsics)
        {
            return focalLengthY;
        }

        return 0;
    }

    internal int GetSkewFactor()
    {
        return skewFactor;
    }
}

internal class ParsedCameraExtrinsicMatrix
{
    bool positionXPresent;
    bool positionYPresent;
    bool positionZPresent;
    bool orientationPresent;
    bool largeOrientation;
    bool worldCoordinateSystemIdPresent;
    int positionX;
    int positionY;
    int positionZ;
    int quaternionX;
    int quaternionY;
    int quaternionZ;
    double quaternionW;
    uint worldCoordinateSystemId;

    internal ParsedCameraExtrinsicMatrix(
        bool positionXPresent,
        bool positionYPresent,
        bool positionZPresent,
        bool orientationPresent,
        bool largeOrientation,
        bool worldCoordinateSystemIdPresent,
        int positionX,
        int positionY,
        int positionZ,
        int quaternionX,
        int quaternionY,
        int quaternionZ,
        double quaternionW,
        uint worldCoordinateSystemId)
    {
        this.positionXPresent = positionXPresent;
        this.positionYPresent = positionYPresent;
        this.positionZPresent = positionZPresent;
        this.orientationPresent = orientationPresent;
        this.largeOrientation = largeOrientation;
        this.worldCoordinateSystemIdPresent = worldCoordinateSystemIdPresent;
        this.positionX = positionX;
        this.positionY = positionY;
        this.positionZ = positionZ;
        this.quaternionX = quaternionX;
        this.quaternionY = quaternionY;
        this.quaternionZ = quaternionZ;
        this.quaternionW = quaternionW;
        this.worldCoordinateSystemId = worldCoordinateSystemId;
    }

    internal bool HasPositionX()
    {
        return positionXPresent;
    }

    internal bool HasPositionY()
    {
        return positionYPresent;
    }

    internal bool HasPositionZ()
    {
        return positionZPresent;
    }

    internal bool HasOrientation()
    {
        return orientationPresent;
    }

    internal bool UsesLargeOrientationFields()
    {
        return largeOrientation;
    }

    internal bool HasWorldCoordinateSystemId()
    {
        return worldCoordinateSystemIdPresent;
    }

    internal int GetPositionX()
    {
        return positionX;
    }

    internal int GetPositionY()
    {
        return positionY;
    }

    internal int GetPositionZ()
    {
        return positionZ;
    }

    internal int GetQuaternionXRaw()
    {
        return quaternionX;
    }

    internal int GetQuaternionYRaw()
    {
        return quaternionY;
    }

    internal int GetQuaternionZRaw()
    {
        return quaternionZ;
    }

    internal double GetQuaternionX()
    {
        return quaternionX / GetOrientationDivisor();
    }

    internal double GetQuaternionY()
    {
        return quaternionY / GetOrientationDivisor();
    }

    internal double GetQuaternionZ()
    {
        return quaternionZ / GetOrientationDivisor();
    }

    internal double GetQuaternionW()
    {
        return quaternionW;
    }

    internal uint GetWorldCoordinateSystemId()
    {
        return worldCoordinateSystemId;
    }

    internal double GetOrientationDivisor()
    {
        if (largeOrientation)
        {
            return 1073741824.0;
        }

        return 16384.0;
    }
}

internal static class HdrCameraPropertyBox
{
    internal static bool IsSupportedType(string type)
    {
        return type == "clli"
            || type == "mdcv"
            || type == "cclv"
            || type == "amve"
            || type == "reve"
            || type == "ndwt"
            || type == "cmin"
            || type == "cmex";
    }

    internal static HdrCameraPropertyParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox box)
    {
        if (box == null || !IsSupportedType(box.Type))
        {
            return CreateResult(AvifResultCode.InvalidArgument);
        }

        if (box.Type == "reve"
            || box.Type == "ndwt"
            || box.Type == "cmin"
            || box.Type == "cmex")
        {
            return ParseFullBoxProperty(source, box);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            box.PayloadOffset,
            0,
            box.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return CreateResult(range.Code);
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)box.PayloadOffset,
            (int)box.PayloadSize);
        if (box.Type == "clli")
        {
            return ParseContentLightLevel(payload);
        }

        if (box.Type == "mdcv")
        {
            return ParseMasteringDisplayColourVolume(payload);
        }

        if (box.Type == "cclv")
        {
            return ParseContentColourVolume(payload);
        }

        return ParseAmbientViewingEnvironment(payload);
    }

    internal static HdrCameraPropertyParseResult ParseContentLightLevel(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 4)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        int maximum = reader.ReadUInt16BigEndian(payload);
        int average = reader.ReadUInt16BigEndian(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        HdrCameraPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.ContentLightLevel = new ParsedContentLightLevel(maximum, average);
        return result;
    }

    internal static HdrCameraPropertyParseResult ParseMasteringDisplayColourVolume(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 24)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        int[] primaryX = new int[3];
        int[] primaryY = new int[3];
        int index = 0;
        while (index < 3)
        {
            primaryX[index] = reader.ReadUInt16BigEndian(payload);
            primaryY[index] = reader.ReadUInt16BigEndian(payload);
            index++;
        }

        int whiteX = reader.ReadUInt16BigEndian(payload);
        int whiteY = reader.ReadUInt16BigEndian(payload);
        uint maximum = reader.ReadUInt32BigEndian(payload);
        uint minimum = reader.ReadUInt32BigEndian(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        index = 0;
        while (index < 3)
        {
            if (primaryX[index] > 50000 || primaryY[index] > 50000)
            {
                return CreateResult(AvifResultCode.InvalidItem);
            }

            index++;
        }

        if (whiteX > 50000 || whiteY > 50000 || minimum >= maximum)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        HdrCameraPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.MasteringDisplayColourVolume = new ParsedMasteringDisplayColourVolume(
            primaryX,
            primaryY,
            whiteX,
            whiteY,
            maximum,
            minimum);
        return result;
    }

    internal static HdrCameraPropertyParseResult ParseContentColourVolume(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 1)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        byte flags = reader.ReadUInt8(payload);
        if ((flags & 195) != 0)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        bool primariesPresent = (flags & 32) != 0;
        bool minimumPresent = (flags & 16) != 0;
        bool maximumPresent = (flags & 8) != 0;
        bool averagePresent = (flags & 4) != 0;
        if (!primariesPresent
            && !minimumPresent
            && !maximumPresent
            && !averagePresent)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        int requiredLength = 1;
        if (primariesPresent)
        {
            requiredLength += 24;
        }

        if (minimumPresent)
        {
            requiredLength += 4;
        }

        if (maximumPresent)
        {
            requiredLength += 4;
        }

        if (averagePresent)
        {
            requiredLength += 4;
        }

        if (payload.Length != requiredLength)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        int[] primaryX = new int[3];
        int[] primaryY = new int[3];
        if (primariesPresent)
        {
            int index = 0;
            while (index < 3)
            {
                primaryX[index] = reader.ReadInt32BigEndian(payload);
                primaryY[index] = reader.ReadInt32BigEndian(payload);
                if (primaryX[index] < -5000000
                    || primaryX[index] > 5000000
                    || primaryY[index] < -5000000
                    || primaryY[index] > 5000000)
                {
                    return CreateResult(AvifResultCode.InvalidItem);
                }

                index++;
            }
        }

        uint minimum = 0;
        uint maximum = 0;
        uint average = 0;
        if (minimumPresent)
        {
            minimum = reader.ReadUInt32BigEndian(payload);
        }

        if (maximumPresent)
        {
            maximum = reader.ReadUInt32BigEndian(payload);
        }

        if (averagePresent)
        {
            average = reader.ReadUInt32BigEndian(payload);
        }

        if (reader.Code != AvifResultCode.Success || reader.Remaining() != 0)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        if ((minimumPresent && averagePresent && minimum > average)
            || (averagePresent && maximumPresent && average > maximum)
            || (minimumPresent && maximumPresent && minimum > maximum))
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        HdrCameraPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.ContentColourVolume = new ParsedContentColourVolume(
            primariesPresent,
            minimumPresent,
            maximumPresent,
            averagePresent,
            primaryX,
            primaryY,
            minimum,
            maximum,
            average);
        return result;
    }

    internal static HdrCameraPropertyParseResult ParseAmbientViewingEnvironment(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 8)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        uint illuminance = reader.ReadUInt32BigEndian(payload);
        int lightX = reader.ReadUInt16BigEndian(payload);
        int lightY = reader.ReadUInt16BigEndian(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        if (illuminance == 0 || lightX > 50000 || lightY > 50000)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        HdrCameraPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.AmbientViewingEnvironment = new ParsedAmbientViewingEnvironment(
            illuminance,
            lightX,
            lightY);
        return result;
    }

    internal static HdrCameraPropertyParseResult ParseFullBoxProperty(
        ReadOnlySpan<byte> source,
        IsoBox box)
    {
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return CreateResult(fullBox.Code);
        }

        if (fullBox.Version != 0)
        {
            return CreateResult(AvifResultCode.UnsupportedFeature);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            fullBox.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return CreateResult(range.Code);
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)fullBox.PayloadOffset,
            (int)fullBox.PayloadSize);
        if (box.Type == "reve")
        {
            if (fullBox.Flags != 0)
            {
                return CreateResult(AvifResultCode.InvalidContainer);
            }

            return ParseReferenceViewingEnvironment(payload);
        }

        if (box.Type == "ndwt")
        {
            if (fullBox.Flags != 0)
            {
                return CreateResult(AvifResultCode.InvalidContainer);
            }

            return ParseNominalDiffuseWhite(payload);
        }

        if (box.Type == "cmin")
        {
            return ParseCameraIntrinsicMatrix(payload, fullBox.Flags);
        }

        return ParseCameraExtrinsicMatrix(payload, fullBox.Flags);
    }

    internal static HdrCameraPropertyParseResult ParseReferenceViewingEnvironment(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 16)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        uint surroundLuminance = reader.ReadUInt32BigEndian(payload);
        int surroundX = reader.ReadUInt16BigEndian(payload);
        int surroundY = reader.ReadUInt16BigEndian(payload);
        uint peripheryLuminance = reader.ReadUInt32BigEndian(payload);
        int peripheryX = reader.ReadUInt16BigEndian(payload);
        int peripheryY = reader.ReadUInt16BigEndian(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        if (surroundX > 10000
            || surroundY > 10000
            || peripheryX > 10000
            || peripheryY > 10000)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        HdrCameraPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.ReferenceViewingEnvironment = new ParsedReferenceViewingEnvironment(
            surroundLuminance,
            surroundX,
            surroundY,
            peripheryLuminance,
            peripheryX,
            peripheryY);
        return result;
    }

    internal static HdrCameraPropertyParseResult ParseNominalDiffuseWhite(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 4)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        uint luminance = reader.ReadUInt32BigEndian(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        HdrCameraPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.NominalDiffuseWhite = new ParsedNominalDiffuseWhite(luminance);
        return result;
    }

    internal static HdrCameraPropertyParseResult ParseCameraIntrinsicMatrix(
        ReadOnlySpan<byte> payload,
        int flags)
    {
        if ((flags & 0x00E0E0FE) != 0)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        bool fullIntrinsics = (flags & 1) != 0;
        int requiredLength = fullIntrinsics ? 20 : 12;
        if (payload.Length != requiredLength)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        int denominatorShift = (flags & 7936) >> 8;
        int skewDenominatorShift = (flags & 2031616) >> 16;
        ByteReader reader = new ByteReader(payload.Length);
        int focalLengthX = reader.ReadInt32BigEndian(payload);
        int principalPointX = reader.ReadInt32BigEndian(payload);
        int principalPointY = reader.ReadInt32BigEndian(payload);
        int focalLengthY = focalLengthX;
        int skewFactor = 0;
        if (fullIntrinsics)
        {
            focalLengthY = reader.ReadInt32BigEndian(payload);
            skewFactor = reader.ReadInt32BigEndian(payload);
        }

        if (reader.Code != AvifResultCode.Success || reader.Remaining() != 0)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        HdrCameraPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.CameraIntrinsicMatrix = new ParsedCameraIntrinsicMatrix(
            fullIntrinsics,
            denominatorShift,
            skewDenominatorShift,
            focalLengthX,
            principalPointX,
            principalPointY,
            focalLengthY,
            skewFactor);
        return result;
    }

    internal static HdrCameraPropertyParseResult ParseCameraExtrinsicMatrix(
        ReadOnlySpan<byte> payload,
        int flags)
    {
        if ((flags & 0x00FFFFC0) != 0)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        bool positionXPresent = (flags & 1) != 0;
        bool positionYPresent = (flags & 2) != 0;
        bool positionZPresent = (flags & 4) != 0;
        bool orientationPresent = (flags & 8) != 0;
        bool largeOrientation = (flags & 16) != 0;
        bool idPresent = (flags & 32) != 0;
        if (largeOrientation && !orientationPresent)
        {
            return CreateResult(AvifResultCode.InvalidItem);
        }

        int requiredLength = 0;
        if (positionXPresent)
        {
            requiredLength += 4;
        }

        if (positionYPresent)
        {
            requiredLength += 4;
        }

        if (positionZPresent)
        {
            requiredLength += 4;
        }

        if (orientationPresent)
        {
            requiredLength += largeOrientation ? 12 : 6;
        }

        if (idPresent)
        {
            requiredLength += 4;
        }

        if (payload.Length != requiredLength)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        ByteReader reader = new ByteReader(payload.Length);
        int positionX = 0;
        int positionY = 0;
        int positionZ = 0;
        if (positionXPresent)
        {
            positionX = reader.ReadInt32BigEndian(payload);
        }

        if (positionYPresent)
        {
            positionY = reader.ReadInt32BigEndian(payload);
        }

        if (positionZPresent)
        {
            positionZ = reader.ReadInt32BigEndian(payload);
        }

        int quaternionX = 0;
        int quaternionY = 0;
        int quaternionZ = 0;
        double quaternionW = 1.0;
        if (orientationPresent)
        {
            if (largeOrientation)
            {
                quaternionX = reader.ReadInt32BigEndian(payload);
                quaternionY = reader.ReadInt32BigEndian(payload);
                quaternionZ = reader.ReadInt32BigEndian(payload);
            }
            else
            {
                quaternionX = reader.ReadInt16BigEndian(payload);
                quaternionY = reader.ReadInt16BigEndian(payload);
                quaternionZ = reader.ReadInt16BigEndian(payload);
            }

            double divisor = largeOrientation ? 1073741824.0 : 16384.0;
            double x = quaternionX / divisor;
            double y = quaternionY / divisor;
            double z = quaternionZ / divisor;
            double sum = x * x + y * y + z * z;
            if (sum > 1.0)
            {
                return CreateResult(AvifResultCode.InvalidItem);
            }

            quaternionW = System.Math.Sqrt(1.0 - sum);
        }

        uint worldCoordinateSystemId = 0;
        if (idPresent)
        {
            worldCoordinateSystemId = reader.ReadUInt32BigEndian(payload);
        }

        if (reader.Code != AvifResultCode.Success || reader.Remaining() != 0)
        {
            return CreateResult(AvifResultCode.InvalidContainer);
        }

        HdrCameraPropertyParseResult result = CreateResult(AvifResultCode.Success);
        result.CameraExtrinsicMatrix = new ParsedCameraExtrinsicMatrix(
            positionXPresent,
            positionYPresent,
            positionZPresent,
            orientationPresent,
            largeOrientation,
            idPresent,
            positionX,
            positionY,
            positionZ,
            quaternionX,
            quaternionY,
            quaternionZ,
            quaternionW,
            worldCoordinateSystemId);
        return result;
    }

    internal static HdrCameraPropertyParseResult CreateResult(AvifResultCode code)
    {
        return new HdrCameraPropertyParseResult(code);
    }
}
