using System;

namespace avif;

internal static class MetadataResolver
{
    internal static AvifResultCode Merge(
        AvifHdrMetadata target,
        AvifHdrMetadata? candidate)
    {
        if (target == null || candidate == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (HasConflict(target, candidate))
        {
            return AvifResultCode.NonConformingData;
        }

        MergeValues(target, candidate);
        return AvifResultCode.Success;
    }

    static bool HasConflict(
        AvifHdrMetadata target,
        AvifHdrMetadata candidate)
    {
        AvifContentLightLevel? targetContent = target.GetContentLightLevelValue();
        AvifContentLightLevel? candidateContent = candidate.GetContentLightLevelValue();
        if (targetContent != null
            && candidateContent != null
            && !targetContent.ValuesEqual(candidateContent))
        {
            return true;
        }

        AvifMasteringDisplay? targetMastering = target.GetMasteringDisplayValue();
        AvifMasteringDisplay? candidateMastering = candidate.GetMasteringDisplayValue();
        if (targetMastering != null
            && candidateMastering != null
            && !targetMastering.ValuesEqual(candidateMastering))
        {
            return true;
        }

        AvifContentColorVolume? targetColor = target.GetContentColorVolumeValue();
        AvifContentColorVolume? candidateColor = candidate.GetContentColorVolumeValue();
        if (targetColor != null
            && candidateColor != null
            && !targetColor.ValuesEqual(candidateColor))
        {
            return true;
        }

        AvifAmbientViewingEnvironment? targetAmbient =
            target.GetAmbientViewingEnvironmentValue();
        AvifAmbientViewingEnvironment? candidateAmbient =
            candidate.GetAmbientViewingEnvironmentValue();
        if (targetAmbient != null
            && candidateAmbient != null
            && !targetAmbient.ValuesEqual(candidateAmbient))
        {
            return true;
        }

        AvifReferenceViewingEnvironment? targetReference =
            target.GetReferenceViewingEnvironmentValue();
        AvifReferenceViewingEnvironment? candidateReference =
            candidate.GetReferenceViewingEnvironmentValue();
        if (targetReference != null
            && candidateReference != null
            && !targetReference.ValuesEqual(candidateReference))
        {
            return true;
        }

        if (target.HasNominalDiffuseWhiteValue()
            && candidate.HasNominalDiffuseWhiteValue()
            && target.GetNominalDiffuseWhiteValue()
                != candidate.GetNominalDiffuseWhiteValue())
        {
            return true;
        }

        if (target.HasItuT35Value()
            && candidate.HasItuT35Value()
            && !target.ItuT35ValuesEqual(candidate))
        {
            return true;
        }

        return false;
    }

    static void MergeValues(
        AvifHdrMetadata target,
        AvifHdrMetadata candidate)
    {
        if (target.GetContentLightLevelValue() == null
            && candidate.GetContentLightLevelValue() != null)
        {
            target.AssignContentLightLevel(
                (candidate.GetContentLightLevelValue() ?? throw new InvalidOperationException("Invalid internal state.")).CreateCopy());
        }

        if (target.GetMasteringDisplayValue() == null
            && candidate.GetMasteringDisplayValue() != null)
        {
            target.AssignMasteringDisplay(
                (candidate.GetMasteringDisplayValue() ?? throw new InvalidOperationException("Invalid internal state.")).CreateCopy());
        }

        if (target.GetContentColorVolumeValue() == null
            && candidate.GetContentColorVolumeValue() != null)
        {
            target.AssignContentColorVolume(
                (candidate.GetContentColorVolumeValue() ?? throw new InvalidOperationException("Invalid internal state.")).CreateCopy());
        }

        if (target.GetAmbientViewingEnvironmentValue() == null
            && candidate.GetAmbientViewingEnvironmentValue() != null)
        {
            target.AssignAmbientViewingEnvironment(
                (candidate.GetAmbientViewingEnvironmentValue() ?? throw new InvalidOperationException("Invalid internal state.")).CreateCopy());
        }

        if (target.GetReferenceViewingEnvironmentValue() == null
            && candidate.GetReferenceViewingEnvironmentValue() != null)
        {
            target.AssignReferenceViewingEnvironment(
                (candidate.GetReferenceViewingEnvironmentValue() ?? throw new InvalidOperationException("Invalid internal state.")).CreateCopy());
        }

        if (!target.HasNominalDiffuseWhiteValue()
            && candidate.HasNominalDiffuseWhiteValue())
        {
            target.AssignNominalDiffuseWhite(candidate.GetNominalDiffuseWhiteValue());
        }

        if (target.GetCameraIntrinsicMatrixValue() == null
            && candidate.GetCameraIntrinsicMatrixValue() != null)
        {
            target.AssignCameraIntrinsicMatrix(
                (candidate.GetCameraIntrinsicMatrixValue() ?? throw new InvalidOperationException("Invalid internal state.")).CreateCopy());
        }

        if (target.GetCameraExtrinsicMatrixValue() == null
            && candidate.GetCameraExtrinsicMatrixValue() != null)
        {
            target.AssignCameraExtrinsicMatrix(
                (candidate.GetCameraExtrinsicMatrixValue() ?? throw new InvalidOperationException("Invalid internal state.")).CreateCopy());
        }

        if (!target.HasItuT35Value() && candidate.HasItuT35Value())
        {
            target.CopyItuT35From(candidate);
        }
    }
}
