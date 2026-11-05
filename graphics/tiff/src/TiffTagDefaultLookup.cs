namespace tiff;

internal class TiffTagDefaultLookup
{
    internal static bool ContainsDefault(int tagId)
    {
        switch (tagId)
        {
            case TiffTagId.NewSubfileType:
            case TiffTagId.BitsPerSample:
            case TiffTagId.Compression:
            case TiffTagId.Thresholding:
            case TiffTagId.FillOrder:
            case TiffTagId.Orientation:
            case TiffTagId.SamplesPerPixel:
            case TiffTagId.RowsPerStrip:
            case TiffTagId.MinSampleValue:
            case TiffTagId.GrayResponseUnit:
            case TiffTagId.T4Options:
            case TiffTagId.T6Options:
            case TiffTagId.ResolutionUnit:
            case TiffTagId.PlanarConfiguration:
            case TiffTagId.Predictor:
            case TiffTagId.CleanFaxData:
            case TiffTagId.InkSet:
            case TiffTagId.NumberOfInks:
            case TiffTagId.SampleFormat:
            case TiffTagId.ImageDepth:
            case TiffTagId.TileDepth:
            case TiffTagId.JPEGProc:
            case TiffTagId.YCbCrCoefficients:
            case TiffTagId.YCbCrSubSampling:
            case TiffTagId.YCbCrPositioning:
            case TiffTagId.CFALayout:
            case TiffTagId.BlackLevelRepeatDim:
            case TiffTagId.BlackLevel:
            case TiffTagId.DefaultScale:
            case TiffTagId.DefaultCropOrigin:
            case TiffTagId.BaselineExposure:
            case TiffTagId.BaselineNoise:
            case TiffTagId.BaselineSharpness:
            case TiffTagId.LinearResponseLimit:
            case TiffTagId.AntiAliasStrength:
            case TiffTagId.ShadowScale:
            case TiffTagId.MakerNoteSafety:
            case TiffTagId.BestQualityScale:
                return true;
            default:
                return false;
        }
    }

    internal static TiffTagDefaultResult Find(int tagId)
    {
        TiffTagValue value = CreateDefaultValue(tagId);
        if (value.TagId == 0)
        {
            return new TiffTagDefaultResult();
        }

        return TiffTagDefaultResult.Create(true, true, value);
    }

    internal static TiffTagDefaultResult FindValueOrDefault(TiffTagValue[] values, int tagId)
    {
        TiffTagValue? value = FindValue(values, tagId);
        if (value != null)
        {
            return TiffTagDefaultResult.Create(true, false, value);
        }

        return Find(tagId);
    }

    internal static TiffTagValue? FindValue(TiffTagValue[]? values, int tagId)
    {
        if (values == null)
        {
            return null;
        }

        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null && values[index].TagId == tagId)
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static TiffTagValue CreateDefaultValue(int tagId)
    {
        switch (tagId)
        {
            case TiffTagId.NewSubfileType:
                return CreateLongDefault(tagId, 0);
            case TiffTagId.BitsPerSample:
                return CreateShortDefault(tagId, 1);
            case TiffTagId.Compression:
                return CreateShortDefault(tagId, (ulong)TiffCompression.None);
            case TiffTagId.Thresholding:
                return CreateShortDefault(tagId, 1);
            case TiffTagId.FillOrder:
                return CreateShortDefault(tagId, 1);
            case TiffTagId.Orientation:
                return CreateShortDefault(tagId, (ulong)TiffOrientation.TopLeft);
            case TiffTagId.SamplesPerPixel:
                return CreateShortDefault(tagId, 1);
            case TiffTagId.RowsPerStrip:
                return TiffTagValue.CreateUnsignedScalar(tagId, TiffDataType.Long, 4294967295);
            case TiffTagId.MinSampleValue:
                return CreateShortDefault(tagId, 0);
            case TiffTagId.GrayResponseUnit:
                return CreateShortDefault(tagId, 2);
            case TiffTagId.T4Options:
            case TiffTagId.T6Options:
                return CreateLongDefault(tagId, 0);
            case TiffTagId.ResolutionUnit:
                return CreateShortDefault(tagId, 2);
            case TiffTagId.PlanarConfiguration:
                return CreateShortDefault(tagId, (ulong)TiffPlanarConfiguration.Contiguous);
            case TiffTagId.Predictor:
                return CreateShortDefault(tagId, 1);
            case TiffTagId.CleanFaxData:
                return CreateShortDefault(tagId, 0);
            case TiffTagId.InkSet:
                return CreateShortDefault(tagId, 1);
            case TiffTagId.NumberOfInks:
                return CreateShortDefault(tagId, 4);
            case TiffTagId.SampleFormat:
                return CreateShortDefault(tagId, 1);
            case TiffTagId.ImageDepth:
            case TiffTagId.TileDepth:
                return CreateLongDefault(tagId, 1);
            case TiffTagId.JPEGProc:
                return CreateShortDefault(tagId, 1);
            case TiffTagId.YCbCrCoefficients:
                return CreateYCbCrCoefficientsDefault();
            case TiffTagId.YCbCrSubSampling:
                return CreateShortArrayDefault(tagId, 2, 2);
            case TiffTagId.YCbCrPositioning:
                return CreateShortDefault(tagId, 1);
            case TiffTagId.CFALayout:
                return CreateShortDefault(tagId, 1);
            case TiffTagId.BlackLevelRepeatDim:
                return CreateShortArrayDefault(tagId, 1, 1);
            case TiffTagId.BlackLevel:
                return CreateRationalDefault(tagId, TiffDataType.Rational, 0, 1);
            case TiffTagId.BaselineExposure:
                return CreateRationalDefault(tagId, TiffDataType.SRational, 0, 1);
            case TiffTagId.DefaultScale:
                return CreateRationalPairDefault(tagId, 1, 1, 1, 1);
            case TiffTagId.DefaultCropOrigin:
                return CreateRationalPairDefault(tagId, 0, 1, 0, 1);
            case TiffTagId.BaselineNoise:
            case TiffTagId.BaselineSharpness:
            case TiffTagId.LinearResponseLimit:
            case TiffTagId.AntiAliasStrength:
            case TiffTagId.ShadowScale:
            case TiffTagId.BestQualityScale:
                return CreateRationalDefault(tagId, TiffDataType.Rational, 1, 1);
            case TiffTagId.MakerNoteSafety:
                return CreateShortDefault(tagId, 0);
            default:
                return new TiffTagValue();
        }
    }

    internal static TiffTagValue CreateShortDefault(int tagId, ulong value)
    {
        return TiffTagValue.CreateUnsignedScalar(tagId, TiffDataType.Short, value);
    }

    internal static TiffTagValue CreateLongDefault(int tagId, ulong value)
    {
        return TiffTagValue.CreateUnsignedScalar(tagId, TiffDataType.Long, value);
    }

    internal static TiffTagValue CreateRationalDefault(int tagId, TiffDataType dataType, long numerator, long denominator)
    {
        return TiffTagValue.CreateRationalScalar(tagId, dataType, numerator, denominator);
    }

    internal static TiffTagValue CreateRationalPairDefault(int tagId, long firstNumerator, long firstDenominator, long secondNumerator, long secondDenominator)
    {
        TiffRational[] values = new TiffRational[2];
        values[0] = TiffRational.Create(firstNumerator, firstDenominator);
        values[1] = TiffRational.Create(secondNumerator, secondDenominator);
        return TiffTagValue.CreateRationalArray(tagId, TiffDataType.Rational, values);
    }

    internal static TiffTagValue CreateShortArrayDefault(int tagId, ulong first, ulong second)
    {
        ulong[] values = new ulong[2];
        values[0] = first;
        values[1] = second;
        return TiffTagValue.CreateUnsignedArray(tagId, TiffDataType.Short, values);
    }

    internal static TiffTagValue CreateYCbCrCoefficientsDefault()
    {
        TiffRational[] values = new TiffRational[3];
        values[0] = TiffRational.Create(299, 1000);
        values[1] = TiffRational.Create(587, 1000);
        values[2] = TiffRational.Create(114, 1000);
        return TiffTagValue.CreateRationalArray(TiffTagId.YCbCrCoefficients, TiffDataType.Rational, values);
    }
}
