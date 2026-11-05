namespace icc;

internal static class PcsConverter
{
    internal const double XyzMaximum = 65535.0 / 32768.0;
    internal const double Version2LabMaximum = (65535.0 / 256.0) - 128.0;
    internal const double Version2LightnessNormalizedMaximum = 65280.0 / 65535.0;

    internal static ushort[] EncodePcsUInt16(double[] pcsValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding)
    {
        double[] normalized = NormalizePcs(pcsValues, pcsKind, integerEncoding);
        return new ushort[]
        {
            QuantizeNormalized(normalized[0]),
            QuantizeNormalized(normalized[1]),
            QuantizeNormalized(normalized[2])
        };
    }

    internal static double[] DecodePcsUInt16(ushort[] encodedValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding)
    {
        ValidateIntegerEncoding(integerEncoding);
        ValidatePcsKind(pcsKind);
        if (encodedValues == null || encodedValues.Length != 3)
        {
            throw new IccTransformException("An encoded integer PCS colour must contain exactly three components.");
        }

        if (pcsKind == IccPcsKind.Lab && integerEncoding == IccPcsEncodingKind.Version2 && encodedValues[0] > 65280)
        {
            throw new IccTransformException("A version 2 integer PCSLAB L* component cannot exceed FF00h.");
        }

        double[] normalized = new double[]
        {
            encodedValues[0] / 65535.0,
            encodedValues[1] / 65535.0,
            encodedValues[2] / 65535.0
        };
        return DenormalizePcs(normalized, pcsKind, integerEncoding);
    }

    internal static float[] EncodePcsFloat32(double[] pcsValues, IccPcsKind pcsKind)
    {
        ValidatePcsKind(pcsKind);
        ValidatePcsComponents(pcsValues, "floating-point PCS colour");
        float[] encoded = new float[3];
        int index = 0;
        while (index < encoded.Length)
        {
            encoded[index] = (float)pcsValues[index];
            if (!System.Single.IsFinite(encoded[index]))
            {
                throw new IccTransformException($"PCS component {index} is outside the finite float32 range.");
            }

            index = index + 1;
        }

        return encoded;
    }

    internal static double[] DecodePcsFloat32(float[] encodedValues, IccPcsKind pcsKind)
    {
        ValidatePcsKind(pcsKind);
        if (encodedValues == null || encodedValues.Length != 3)
        {
            throw new IccTransformException("An encoded floating-point PCS colour must contain exactly three components.");
        }

        double[] decoded = new double[3];
        int index = 0;
        while (index < decoded.Length)
        {
            if (!System.Single.IsFinite(encodedValues[index]))
            {
                throw new IccTransformException($"Encoded PCS component {index} must be finite.");
            }

            decoded[index] = encodedValues[index];
            index = index + 1;
        }

        return decoded;
    }

    internal static double[] NormalizePcs(double[] pcsValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding)
    {
        ValidateIntegerEncoding(integerEncoding);
        ValidatePcsKind(pcsKind);
        ValidatePcsComponents(pcsValues, "PCS colour");
        if (pcsKind == IccPcsKind.Xyz)
        {
            return new double[]
            {
                Clamp(pcsValues[0], 0.0, XyzMaximum) / XyzMaximum,
                Clamp(pcsValues[1], 0.0, XyzMaximum) / XyzMaximum,
                Clamp(pcsValues[2], 0.0, XyzMaximum) / XyzMaximum
            };
        }

        if (integerEncoding == IccPcsEncodingKind.Version2)
        {
            return new double[]
            {
                Clamp(pcsValues[0], 0.0, 100.0) * 652.8 / 65535.0,
                (Clamp(pcsValues[1], -128.0, Version2LabMaximum) + 128.0) * 256.0 / 65535.0,
                (Clamp(pcsValues[2], -128.0, Version2LabMaximum) + 128.0) * 256.0 / 65535.0
            };
        }

        return new double[]
        {
            Clamp(pcsValues[0], 0.0, 100.0) / 100.0,
            (Clamp(pcsValues[1], -128.0, 127.0) + 128.0) / 255.0,
            (Clamp(pcsValues[2], -128.0, 127.0) + 128.0) / 255.0
        };
    }

    internal static void NormalizePcsComponents(System.ReadOnlySpan<double> pcsValues, System.Span<double> normalizedValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding)
    {
        ValidateIntegerEncoding(integerEncoding);
        ValidatePcsKind(pcsKind);
        ValidatePcsSpans(pcsValues, normalizedValues, "PCS normalization");
        if (pcsKind == IccPcsKind.Xyz)
        {
            normalizedValues[0] = Clamp(pcsValues[0], 0.0, XyzMaximum) / XyzMaximum;
            normalizedValues[1] = Clamp(pcsValues[1], 0.0, XyzMaximum) / XyzMaximum;
            normalizedValues[2] = Clamp(pcsValues[2], 0.0, XyzMaximum) / XyzMaximum;
            return;
        }

        if (integerEncoding == IccPcsEncodingKind.Version2)
        {
            normalizedValues[0] = Clamp(pcsValues[0], 0.0, 100.0) * 652.8 / 65535.0;
            normalizedValues[1] = (Clamp(pcsValues[1], -128.0, Version2LabMaximum) + 128.0) * 256.0 / 65535.0;
            normalizedValues[2] = (Clamp(pcsValues[2], -128.0, Version2LabMaximum) + 128.0) * 256.0 / 65535.0;
            return;
        }

        normalizedValues[0] = Clamp(pcsValues[0], 0.0, 100.0) / 100.0;
        normalizedValues[1] = (Clamp(pcsValues[1], -128.0, 127.0) + 128.0) / 255.0;
        normalizedValues[2] = (Clamp(pcsValues[2], -128.0, 127.0) + 128.0) / 255.0;
    }

    internal static double[] DenormalizePcs(double[] normalizedValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding)
    {
        ValidateIntegerEncoding(integerEncoding);
        ValidatePcsKind(pcsKind);
        ValidatePcsComponents(normalizedValues, "normalized PCS colour");
        if (pcsKind == IccPcsKind.Xyz)
        {
            return new double[]
            {
                Clamp(normalizedValues[0], 0.0, 1.0) * XyzMaximum,
                Clamp(normalizedValues[1], 0.0, 1.0) * XyzMaximum,
                Clamp(normalizedValues[2], 0.0, 1.0) * XyzMaximum
            };
        }

        if (integerEncoding == IccPcsEncodingKind.Version2)
        {
            return new double[]
            {
                Clamp(normalizedValues[0], 0.0, Version2LightnessNormalizedMaximum) * 65535.0 / 652.8,
                (Clamp(normalizedValues[1], 0.0, 1.0) * 65535.0 / 256.0) - 128.0,
                (Clamp(normalizedValues[2], 0.0, 1.0) * 65535.0 / 256.0) - 128.0
            };
        }

        return new double[]
        {
            Clamp(normalizedValues[0], 0.0, 1.0) * 100.0,
            (Clamp(normalizedValues[1], 0.0, 1.0) * 255.0) - 128.0,
            (Clamp(normalizedValues[2], 0.0, 1.0) * 255.0) - 128.0
        };
    }

    internal static void DenormalizePcsComponents(System.ReadOnlySpan<double> normalizedValues, System.Span<double> pcsValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding)
    {
        ValidateIntegerEncoding(integerEncoding);
        ValidatePcsKind(pcsKind);
        ValidatePcsSpans(normalizedValues, pcsValues, "PCS denormalization");
        if (pcsKind == IccPcsKind.Xyz)
        {
            pcsValues[0] = Clamp(normalizedValues[0], 0.0, 1.0) * XyzMaximum;
            pcsValues[1] = Clamp(normalizedValues[1], 0.0, 1.0) * XyzMaximum;
            pcsValues[2] = Clamp(normalizedValues[2], 0.0, 1.0) * XyzMaximum;
            return;
        }

        if (integerEncoding == IccPcsEncodingKind.Version2)
        {
            pcsValues[0] = Clamp(normalizedValues[0], 0.0, Version2LightnessNormalizedMaximum) * 65535.0 / 652.8;
            pcsValues[1] = (Clamp(normalizedValues[1], 0.0, 1.0) * 65535.0 / 256.0) - 128.0;
            pcsValues[2] = (Clamp(normalizedValues[2], 0.0, 1.0) * 65535.0 / 256.0) - 128.0;
            return;
        }

        pcsValues[0] = Clamp(normalizedValues[0], 0.0, 1.0) * 100.0;
        pcsValues[1] = (Clamp(normalizedValues[1], 0.0, 1.0) * 255.0) - 128.0;
        pcsValues[2] = (Clamp(normalizedValues[2], 0.0, 1.0) * 255.0) - 128.0;
    }

    internal static void ValidatePcsSpans(System.ReadOnlySpan<double> inputs, System.Span<double> outputs, string description)
    {
        if (inputs.Length != 3 || outputs.Length < 3)
        {
            throw new IccTransformException($"{description} requires exactly three input and at least three output components.");
        }

        int index = 0;
        while (index < inputs.Length)
        {
            if (!System.Double.IsFinite(inputs[index]))
            {
                throw new IccTransformException($"{description} input component {index} must be finite.");
            }

            index = index + 1;
        }
    }

    internal static double[] ConvertMediaRelativeToIccAbsolute(double[] mediaRelativePcs, double[] relativeWhite, double[] absoluteWhite)
    {
        ValidateRelativeAbsoluteVectors(mediaRelativePcs, relativeWhite, absoluteWhite, "media-relative PCS");
        double[] result = new double[mediaRelativePcs.Length];
        int index = 0;
        while (index < result.Length)
        {
            double scale = relativeWhite[index] / absoluteWhite[index];
            result[index] = mediaRelativePcs[index] * scale;
            if (!System.Double.IsFinite(scale) || !System.Double.IsFinite(result[index]))
            {
                throw new IccTransformException($"Media-relative to ICC-absolute conversion produced component {index} outside the finite double range.");
            }

            index = index + 1;
        }

        return result;
    }

    internal static double[] ConvertIccAbsoluteToMediaRelative(double[] absolutePcs, double[] relativeWhite, double[] absoluteWhite)
    {
        ValidateRelativeAbsoluteVectors(absolutePcs, relativeWhite, absoluteWhite, "ICC-absolute PCS");
        double[] result = new double[absolutePcs.Length];
        int index = 0;
        while (index < result.Length)
        {
            double scale = absoluteWhite[index] / relativeWhite[index];
            result[index] = absolutePcs[index] * scale;
            if (!System.Double.IsFinite(scale) || !System.Double.IsFinite(result[index]))
            {
                throw new IccTransformException($"ICC-absolute to media-relative conversion produced component {index} outside the finite double range.");
            }

            index = index + 1;
        }

        return result;
    }

    internal static double[] ResolveSpectralAbsoluteWhite(uint spectralPcsSignature, double[] pccIlluminant)
    {
        int channelCount = ColorSpaceChannelCount.Decode(spectralPcsSignature);
        uint prefix = spectralPcsSignature & 0xffff0000;
        if (channelCount <= 0)
        {
            throw new IccTransformException("The spectral PCS signature must declare at least one channel.");
        }

        if (prefix == IccColorSpaceSignatures.ReflectanceSpectralPrefix || prefix == IccColorSpaceSignatures.TransmissionSpectralPrefix)
        {
            return CreateUnitWhiteVector(channelCount);
        }

        if (prefix == IccColorSpaceSignatures.RadiantSpectralPrefix)
        {
            ValidateWhiteVector(pccIlluminant, channelCount, "emissive PCS illuminant");
            return MatrixMath.CopyValues(pccIlluminant);
        }

        if (prefix == IccColorSpaceSignatures.BiSpectralPrefix || prefix == IccColorSpaceSignatures.SparseMatrixSpectralPrefix)
        {
            throw new IccTransformException("Absolute and relative conversion is not defined for a fluorescent spectral PCS.");
        }

        throw new IccTransformException("The signature does not identify a supported reflective, transmissive, or emissive spectral PCS.");
    }

    internal static double[] CreateUnitWhiteVector(int channelCount)
    {
        if (channelCount <= 0)
        {
            throw new IccTransformException("A PCS white vector must contain at least one component.");
        }

        double[] values = new double[channelCount];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = 1.0;
            index = index + 1;
        }

        return values;
    }

    internal static void ValidateRelativeAbsoluteVectors(double[] pcsValues, double[] relativeWhite, double[] absoluteWhite, string description)
    {
        if (pcsValues == null || pcsValues.Length == 0)
        {
            throw new IccTransformException($"The {description} vector must contain at least one component.");
        }

        ValidateFiniteVector(pcsValues, pcsValues.Length, description);
        ValidateWhiteVector(relativeWhite, pcsValues.Length, "relative white");
        ValidateWhiteVector(absoluteWhite, pcsValues.Length, "absolute white");
    }

    internal static void ValidateWhiteVector(double[] values, int expectedLength, string description)
    {
        ValidateFiniteVector(values, expectedLength, description);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] <= 0.0)
            {
                throw new IccTransformException($"The {description} component {index} must be positive for absolute and relative conversion.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateFiniteVector(double[] values, int expectedLength, string description)
    {
        if (values == null || values.Length != expectedLength)
        {
            throw new IccTransformException($"The {description} vector must contain exactly {expectedLength} components.");
        }

        int index = 0;
        while (index < values.Length)
        {
            if (!System.Double.IsFinite(values[index]))
            {
                throw new IccTransformException($"The {description} component {index} must be finite.");
            }

            index = index + 1;
        }
    }

    internal static ushort QuantizeNormalized(double value)
    {
        double rounded = System.Math.Round(Clamp(value, 0.0, 1.0) * 65535.0, System.MidpointRounding.AwayFromZero);
        return (ushort)rounded;
    }

    internal static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }

    internal static void ValidatePcsComponents(double[] values, string description)
    {
        if (values == null || values.Length != 3)
        {
            throw new IccTransformException($"The {description} must contain exactly three components.");
        }

        int index = 0;
        while (index < values.Length)
        {
            if (!System.Double.IsFinite(values[index]))
            {
                throw new IccTransformException($"The {description} component {index} must be finite.");
            }

            index = index + 1;
        }
    }

    internal static void ValidatePcsKind(IccPcsKind pcsKind)
    {
        if (pcsKind != IccPcsKind.Xyz && pcsKind != IccPcsKind.Lab)
        {
            throw new IccTransformException("PCS encoding requires either PCSXYZ or PCSLAB.");
        }
    }

    internal static void ValidateIntegerEncoding(IccPcsEncodingKind integerEncoding)
    {
        if (integerEncoding != IccPcsEncodingKind.Version2 && integerEncoding != IccPcsEncodingKind.Version4)
        {
            throw new IccTransformException("Integer PCS encoding requires version 2 or version 4 encoding.");
        }
    }
}
