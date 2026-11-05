using System;
using System.Collections.Generic;

namespace avif;

internal class ColorMetadataResolution
{
    internal AvifResultCode Code;
    internal ParsedColourInformation? Nclx;
    internal ParsedColourInformation? Icc;

    internal ColorMetadataResolution(
        AvifResultCode code,
        ParsedColourInformation? nclx,
        ParsedColourInformation? icc)
    {
        Code = code;
        Nclx = nclx;
        Icc = icc;
    }
}

internal static class ColorMetadataResolver
{
    internal static AvifResultCode Validate(
        List<ParsedColourInformation> properties)
    {
        return Resolve(properties).Code;
    }

    internal static AvifResultCode Apply(
        AvifImage image,
        ParsedItem item,
        AvifSecurityLimits limits)
    {
        if (image == null || item == null || limits == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        List<ParsedColourInformation> properties =
            new List<ParsedColourInformation>();
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedColourInformation? property = (item
                .GetPropertyAssociation(index) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty()
                .GetColourInformation();
            if (property != null)
            {
                properties.Add(property);
            }
            index++;
        }
        return Apply(image, properties, limits);
    }

    internal static AvifResultCode Apply(
        AvifImage image,
        List<ParsedColourInformation> properties,
        AvifSecurityLimits limits)
    {
        if (image == null || properties == null || limits == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        ColorMetadataResolution resolution = Resolve(properties);
        AvifResultCode code = resolution.Code;
        if (code != AvifResultCode.Success)
        {
            return code;
        }

        ParsedColourInformation? nclx = resolution.Nclx;
        if (nclx != null)
        {
            if (nclx.GetColourPrimaries() > byte.MaxValue
                || nclx.GetTransferCharacteristics() > byte.MaxValue
                || nclx.GetMatrixCoefficients() > byte.MaxValue)
            {
                return AvifResultCode.UnsupportedFeature;
            }
            code = image.SetColorInfo(AvifColorInfo.Create(
                (AvifColorPrimaries)nclx.GetColourPrimaries(),
                (AvifTransferCharacteristics)nclx.GetTransferCharacteristics(),
                (AvifMatrixCoefficients)nclx.GetMatrixCoefficients(),
                nclx.IsFullRange() ? AvifRange.Full : AvifRange.Limited));
            if (code != AvifResultCode.Success)
            {
                return code;
            }
        }

        ParsedColourInformation? icc = resolution.Icc;
        if (icc != null)
        {
            AvifIccProfile profile = AvifIccProfile.Create(icc.GetData(), limits);
            if (profile.GetCode() == AvifResultCode.Success)
            {
                code = image.SetIccProfile(profile);
                if (code != AvifResultCode.Success)
                {
                    return code;
                }
            }
        }
        return AvifResultCode.Success;
    }

    static ColorMetadataResolution Resolve(
        List<ParsedColourInformation> properties)
    {
        if (properties == null)
        {
            return new ColorMetadataResolution(
                AvifResultCode.InvalidArgument,
                null,
                null);
        }

        ParsedColourInformation? nclx = null;
        ParsedColourInformation? icc = null;
        int index = 0;
        while (index < properties.Count)
        {
            ParsedColourInformation candidate = properties[index];
            if (candidate == null)
            {
                return new ColorMetadataResolution(
                    AvifResultCode.InvalidArgument,
                    null,
                    null);
            }
            if (candidate.GetColourType() == "nclx")
            {
                if (nclx != null && !NclxValuesEqual(nclx, candidate))
                {
                    return new ColorMetadataResolution(
                        AvifResultCode.NonConformingData,
                        null,
                        null);
                }
                nclx = candidate;
            }
            else if (candidate.IsIccProfile())
            {
                if (icc != null && !IccValuesEqual(icc, candidate))
                {
                    return new ColorMetadataResolution(
                        AvifResultCode.NonConformingData,
                        null,
                        null);
                }
                icc = candidate;
            }
            index++;
        }
        return new ColorMetadataResolution(
            AvifResultCode.Success,
            nclx,
            icc);
    }

    static bool NclxValuesEqual(
        ParsedColourInformation first,
        ParsedColourInformation second)
    {
        return first.GetColourPrimaries() == second.GetColourPrimaries()
            && first.GetTransferCharacteristics()
                == second.GetTransferCharacteristics()
            && first.GetMatrixCoefficients() == second.GetMatrixCoefficients()
            && first.IsFullRange() == second.IsFullRange();
    }

    static bool IccValuesEqual(
        ParsedColourInformation first,
        ParsedColourInformation second)
    {
        if (first.GetColourType() != second.GetColourType())
        {
            return false;
        }
        byte[] firstData = first.GetData();
        byte[] secondData = second.GetData();
        if (firstData.Length != secondData.Length)
        {
            return false;
        }
        int index = 0;
        while (index < firstData.Length)
        {
            if (firstData[index] != secondData[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }
}
