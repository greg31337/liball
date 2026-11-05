using System;

namespace avif;

internal class HdrMetadataParseResult
{
    internal AvifResultCode Code;
    internal AvifHdrMetadata? Metadata;

    internal HdrMetadataParseResult(
        AvifResultCode code,
        AvifHdrMetadata? metadata)
    {
        Code = code;
        Metadata = metadata;
    }
}

internal static class HdrMetadataParser
{
    internal static AvifHdrMetadata ParseContainerProperty(
        ParsedItemProperty property)
    {
        AvifHdrMetadata metadata = new AvifHdrMetadata();
        if (property == null)
        {
            return metadata;
        }

        ParsedContentLightLevel? content = property.GetContentLightLevel();
        if (content != null)
        {
            metadata.AssignContentLightLevel(new AvifContentLightLevel(
                content.GetMaxContentLightLevel(),
                content.GetMaxPictureAverageLightLevel()));
        }

        ParsedMasteringDisplayColourVolume? mastering =
            property.GetMasteringDisplayColourVolume();
        if (mastering != null)
        {
            metadata.AssignMasteringDisplay(CreateMasteringDisplay(mastering));
        }

        ParsedContentColourVolume? color = property.GetContentColourVolume();
        if (color != null)
        {
            int[] primaryX = new int[3];
            int[] primaryY = new int[3];
            int index = 0;
            while (index < 3)
            {
                primaryX[index] = color.GetPrimaryX(index);
                primaryY[index] = color.GetPrimaryY(index);
                index++;
            }

            metadata.AssignContentColorVolume(new AvifContentColorVolume(
                color.HasPrimaries(),
                color.HasMinimumLuminance(),
                color.HasMaximumLuminance(),
                color.HasAverageLuminance(),
                primaryX,
                primaryY,
                color.GetMinimumLuminance(),
                color.GetMaximumLuminance(),
                color.GetAverageLuminance()));
        }

        ParsedAmbientViewingEnvironment? ambient =
            property.GetAmbientViewingEnvironment();
        if (ambient != null)
        {
            metadata.AssignAmbientViewingEnvironment(
                new AvifAmbientViewingEnvironment(
                    ambient.GetIlluminance(),
                    ambient.GetLightX(),
                    ambient.GetLightY()));
        }

        ParsedReferenceViewingEnvironment? reference =
            property.GetReferenceViewingEnvironment();
        if (reference != null)
        {
            metadata.AssignReferenceViewingEnvironment(
                new AvifReferenceViewingEnvironment(
                    reference.GetSurroundLuminance(),
                    reference.GetSurroundLightX(),
                    reference.GetSurroundLightY(),
                    reference.GetPeripheryLuminance(),
                    reference.GetPeripheryLightX(),
                    reference.GetPeripheryLightY()));
        }

        ParsedNominalDiffuseWhite? diffuseWhite = property.GetNominalDiffuseWhite();
        if (diffuseWhite != null)
        {
            metadata.AssignNominalDiffuseWhite(diffuseWhite.GetLuminance());
        }

        ParsedCameraIntrinsicMatrix? intrinsic = property.GetCameraIntrinsicMatrix();
        if (intrinsic != null)
        {
            metadata.AssignCameraIntrinsicMatrix(
                new AvifCameraIntrinsicMatrix(
                    intrinsic.HasFullIntrinsics(),
                    intrinsic.GetDenominatorShift(),
                    intrinsic.GetSkewDenominatorShift(),
                    intrinsic.GetFocalLengthX(),
                    intrinsic.GetPrincipalPointX(),
                    intrinsic.GetPrincipalPointY(),
                    intrinsic.GetFocalLengthY(),
                    intrinsic.GetSkewFactor()));
        }

        ParsedCameraExtrinsicMatrix? extrinsic = property.GetCameraExtrinsicMatrix();
        if (extrinsic != null)
        {
            metadata.AssignCameraExtrinsicMatrix(
                new AvifCameraExtrinsicMatrix(
                    extrinsic.HasPositionX(),
                    extrinsic.HasPositionY(),
                    extrinsic.HasPositionZ(),
                    extrinsic.HasOrientation(),
                    extrinsic.UsesLargeOrientationFields(),
                    extrinsic.HasWorldCoordinateSystemId(),
                    extrinsic.GetPositionX(),
                    extrinsic.GetPositionY(),
                    extrinsic.GetPositionZ(),
                    extrinsic.GetQuaternionXRaw(),
                    extrinsic.GetQuaternionYRaw(),
                    extrinsic.GetQuaternionZRaw(),
                    extrinsic.GetWorldCoordinateSystemId()));
        }

        return metadata;
    }

    internal static HdrMetadataParseResult ParseAv1MetadataObuPayload(
        ReadOnlySpan<byte> payload)
    {
        Av1MetadataParseResult parsed = Av1MetadataParser.ParsePayload(payload);
        if (parsed.Code != AvifResultCode.Success || parsed.Metadata == null)
        {
            return new HdrMetadataParseResult(parsed.Code, null);
        }

        AvifHdrMetadata metadata = new AvifHdrMetadata();
        if (parsed.Metadata.ContentLightLevel != null)
        {
            Av1ContentLightLevelMetadata content = parsed.Metadata.ContentLightLevel;
            metadata.AssignContentLightLevel(new AvifContentLightLevel(
                content.MaximumContentLightLevel,
                content.MaximumFrameAverageLightLevel));
        }

        if (parsed.Metadata.MasteringDisplayColorVolume != null)
        {
            Av1MasteringDisplayColorVolumeMetadata mastering =
                parsed.Metadata.MasteringDisplayColorVolume;
            if (!IsValidMasteringDisplay(mastering))
            {
                return new HdrMetadataParseResult(
                    AvifResultCode.InvalidMetadata,
                    null);
            }

            metadata.AssignMasteringDisplay(new AvifMasteringDisplay(
                mastering.PrimaryChromaticityX,
                mastering.PrimaryChromaticityY,
                mastering.WhitePointChromaticityX,
                mastering.WhitePointChromaticityY,
                mastering.MaximumLuminance,
                mastering.MinimumLuminance));
        }

        if (parsed.Metadata.ItuT35 != null)
        {
            metadata.AssignItuT35(parsed.Metadata.ItuT35);
        }

        return new HdrMetadataParseResult(AvifResultCode.Success, metadata);
    }

    static AvifMasteringDisplay CreateMasteringDisplay(
        ParsedMasteringDisplayColourVolume mastering)
    {
        int[] primaryX = new int[3];
        int[] primaryY = new int[3];
        int index = 0;
        while (index < 3)
        {
            primaryX[index] = mastering.GetPrimaryX(index);
            primaryY[index] = mastering.GetPrimaryY(index);
            index++;
        }

        return new AvifMasteringDisplay(
            primaryX,
            primaryY,
            mastering.GetWhitePointX(),
            mastering.GetWhitePointY(),
            mastering.GetMaximumLuminance(),
            mastering.GetMinimumLuminance());
    }

    static bool IsValidMasteringDisplay(
        Av1MasteringDisplayColorVolumeMetadata mastering)
    {
        int index = 0;
        while (index < 3)
        {
            if (mastering.PrimaryChromaticityX[index] > 50000
                || mastering.PrimaryChromaticityY[index] > 50000)
            {
                return false;
            }

            index++;
        }

        return mastering.WhitePointChromaticityX <= 50000
            && mastering.WhitePointChromaticityY <= 50000
            && mastering.MinimumLuminance < mastering.MaximumLuminance;
    }
}
