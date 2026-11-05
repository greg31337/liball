namespace icc;

internal static class NamedColorMath
{
    internal static IccNamedColor? FindByExactName(IccNamedColorSet colorSet, string name)
    {
        ValidateSet(colorSet);
        if (name == null)
        {
            throw new IccTransformException("A named-colour lookup name cannot be null.");
        }

        int index = 0;
        while (index < colorSet.Colors.Length)
        {
            IccNamedColor color = colorSet.Colors[index];
            ValidateColor(color, index);
            string composed = ComposeName(colorSet, color);
            if (System.String.Equals(name, color.Name, System.StringComparison.Ordinal)
                || System.String.Equals(name, composed, System.StringComparison.Ordinal)
                || System.String.Equals(name, color.Description.Name, System.StringComparison.Ordinal))
            {
                return ExtendedTagDataClone.CloneNamedColor(color);
            }

            index = index + 1;
        }

        return null;
    }

    internal static IccNamedColor? FindByNormalizedName(IccNamedColorSet colorSet, string name)
    {
        ValidateSet(colorSet);
        if (name == null)
        {
            throw new IccTransformException("A named-colour lookup name cannot be null.");
        }

        string target = NormalizeName(name);
        int index = 0;
        while (index < colorSet.Colors.Length)
        {
            IccNamedColor color = colorSet.Colors[index];
            ValidateColor(color, index);
            string composed = ComposeName(colorSet, color);
            if (System.String.Equals(target, NormalizeName(color.Name), System.StringComparison.Ordinal)
                || System.String.Equals(target, NormalizeName(composed), System.StringComparison.Ordinal)
                || System.String.Equals(target, NormalizeName(color.Description.Name), System.StringComparison.Ordinal))
            {
                return ExtendedTagDataClone.CloneNamedColor(color);
            }

            index = index + 1;
        }

        return null;
    }

    internal static IccColorDescription EvaluateTint(IccNamedColorSet colorSet, IccNamedColor color, double tint)
    {
        ValidateSet(colorSet);
        ValidateStandaloneColor(color);
        ValidateTint(tint);
        IccColorDescription result = new IccColorDescription();
        result.Name = ComposeName(colorSet, color);
        bool hasCoordinates = false;
        if (color.PcsColor.Components.Length != 0)
        {
            IccColorVector pcs = SelectVector(colorSet, color, tint, true);
            PopulatePcsDescription(result, pcs);
            hasCoordinates = true;
        }
        if (color.DeviceColor.Components.Length != 0)
        {
            result.DeviceColor = SelectVector(colorSet, color, tint, false);
            hasCoordinates = true;
        }

        if (!hasCoordinates)
        {
            throw new IccTransformException("The named colour does not contain PCS or device coordinates to evaluate.");
        }

        return result;
    }

    internal static IccColorVector SelectPcs(IccNamedColorSet colorSet, IccNamedColor color, double tint)
    {
        ValidateSet(colorSet);
        ValidateStandaloneColor(color);
        ValidateTint(tint);
        return SelectVector(colorSet, color, tint, true);
    }

    internal static IccColorVector SelectDevice(IccNamedColorSet colorSet, IccNamedColor color, double tint)
    {
        ValidateSet(colorSet);
        ValidateStandaloneColor(color);
        ValidateTint(tint);
        return SelectVector(colorSet, color, tint, false);
    }

    internal static IccNamedColor? FindNearestByLab(IccNamedColorSet colorSet, double[] lab)
    {
        ValidateSet(colorSet);
        ColorMathCore.ValidateThreeComponents(lab, "named-colour Lab query");
        IccNamedColor? nearest = null;
        double nearestDifference = double.PositiveInfinity;
        int index = 0;
        while (index < colorSet.Colors.Length)
        {
            IccNamedColor color = colorSet.Colors[index];
            ValidateColor(color, index);
            double[]? candidateLab = GetFullTintLab(colorSet, color);
            if (candidateLab != null)
            {
                double difference = ColorDifferenceMath.CalculateCiede2000(lab, candidateLab);
                if (difference < nearestDifference)
                {
                    nearest = color;
                    nearestDifference = difference;
                }
            }

            index = index + 1;
        }

        return nearest == null ? null : ExtendedTagDataClone.CloneNamedColor(nearest);
    }

    internal static IccColorVector SelectVector(IccNamedColorSet colorSet, IccNamedColor color, double tint, bool selectPcs)
    {
        IccColorVector source = selectPcs ? color.PcsColor : color.DeviceColor;
        string representation = selectPcs ? "PCS" : "device";
        if (source == null || source.Components == null || source.Components.Length == 0)
        {
            throw new IccTransformException($"The named colour does not contain {representation} coordinates.");
        }

        ValidateFiniteComponents(source.Components, representation);
        ValidateTintSamples(color.Tints);
        int sampleCount = color.Tints.Length;
        if (sampleCount == 0)
        {
            if (tint == 1.0)
            {
                return CopyVector(source, source.Components);
            }

            double[] zero = SelectZeroComponents(colorSet, source.Components.Length, selectPcs);
            double[] interpolated = InterpolateComponents(zero, source.Components, tint);
            return CopyVector(source, interpolated);
        }

        if (source.Components.Length % sampleCount != 0)
        {
            throw new IccTransformException($"The named colour's {representation} component count is not divisible by its tint sample count.");
        }

        int channelCount = source.Components.Length / sampleCount;
        if (channelCount == 0)
        {
            throw new IccTransformException($"The named colour has no {representation} channels per tint sample.");
        }

        int upperIndex = 0;
        while (upperIndex < sampleCount && tint > color.Tints[upperIndex])
        {
            upperIndex = upperIndex + 1;
        }

        if (upperIndex == sampleCount)
        {
            throw new IccTransformException($"Tint {tint} is above the last available named-colour sample at {color.Tints[sampleCount - 1]}.");
        }

        double[] upper = ExtractSample(source.Components, channelCount, upperIndex);
        if (tint == color.Tints[upperIndex])
        {
            return CopyVector(source, upper);
        }

        double lowerTint;
        double[] lower;
        if (upperIndex == 0)
        {
            lowerTint = 0.0;
            lower = SelectZeroComponents(colorSet, channelCount, selectPcs);
        }
        else
        {
            lowerTint = color.Tints[upperIndex - 1];
            lower = ExtractSample(source.Components, channelCount, upperIndex - 1);
        }

        double fraction = (tint - lowerTint) / (color.Tints[upperIndex] - lowerTint);
        return CopyVector(source, InterpolateComponents(lower, upper, fraction));
    }

    internal static double[] SelectZeroComponents(IccNamedColorSet colorSet, int channelCount, bool selectPcs)
    {
        if (colorSet.TintZero == null)
        {
            throw new IccTransformException("Interpolation below the first named-colour tint sample requires a shared zero-tint endpoint.");
        }

        IccColorVector vector = selectPcs ? colorSet.TintZero.PcsColor : colorSet.TintZero.DeviceColor;
        string representation = selectPcs ? "PCS" : "device";
        if (vector == null || vector.Components == null || vector.Components.Length != channelCount)
        {
            throw new IccTransformException($"The shared zero-tint {representation} coordinate count does not match the named colour.");
        }

        ValidateFiniteComponents(vector.Components, $"zero-tint {representation}");
        return CopyComponents(vector.Components);
    }

    internal static double[] ExtractSample(double[] components, int channelCount, int sampleIndex)
    {
        double[] result = new double[channelCount];
        int offset = sampleIndex * channelCount;
        int index = 0;
        while (index < channelCount)
        {
            result[index] = components[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static double[] InterpolateComponents(double[] lower, double[] upper, double fraction)
    {
        double[] result = new double[lower.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = lower[index] + ((upper[index] - lower[index]) * fraction);
            index = index + 1;
        }

        return result;
    }

    internal static IccColorVector CopyVector(IccColorVector source, double[] components)
    {
        IccColorVector result = new IccColorVector();
        result.ColorSpace = source.ColorSpace;
        result.ColorSpaceSignature = source.ColorSpaceSignature;
        result.Components = CopyComponents(components);
        return result;
    }

    internal static void PopulatePcsDescription(IccColorDescription description, IccColorVector pcs)
    {
        if (pcs.Components.Length != 3)
        {
            throw new IccTransformException("Named-colour colorimetric PCS coordinates must contain exactly three components.");
        }

        if (pcs.ColorSpace == IccColorSpaceKind.Lab || pcs.ColorSpaceSignature == IccColorSpaceSignatures.Lab)
        {
            IccLabColor lab = new IccLabColor();
            lab.L = pcs.Components[0];
            lab.A = pcs.Components[1];
            lab.B = pcs.Components[2];
            description.Lab = lab;
            double[] xyzValues = ColorMathCore.ConvertLabToXyz(
                pcs.Components,
                new double[] { IccStandardColorimetry.D50X, IccStandardColorimetry.D50Y, IccStandardColorimetry.D50Z });
            IccXyzColor xyz = new IccXyzColor();
            xyz.X = xyzValues[0];
            xyz.Y = xyzValues[1];
            xyz.Z = xyzValues[2];
            description.Xyz = xyz;
            NamedColorTagCodec.PrepareColorEquivalents(description);
            return;
        }

        if (pcs.ColorSpace == IccColorSpaceKind.Xyz || pcs.ColorSpaceSignature == IccColorSpaceSignatures.Xyz)
        {
            IccXyzColor xyz = new IccXyzColor();
            xyz.X = pcs.Components[0];
            xyz.Y = pcs.Components[1];
            xyz.Z = pcs.Components[2];
            description.Xyz = xyz;
            description.Lab = NamedColorTagCodec.ConvertXyzToD50Lab(xyz);
            NamedColorTagCodec.PrepareColorEquivalents(description);
            return;
        }

        throw new IccTransformException("Named-colour nearest matching and descriptions require Lab or XYZ PCS coordinates.");
    }

    internal static double[]? GetFullTintLab(IccNamedColorSet colorSet, IccNamedColor color)
    {
        if (color.PcsColor != null && color.PcsColor.Components != null && color.PcsColor.Components.Length != 0)
        {
            IccColorVector pcs = SelectVector(colorSet, color, 1.0, true);
            if (pcs.Components.Length != 3)
            {
                throw new IccTransformException("A named colour used for nearest matching must have three PCS components per tint.");
            }

            if (pcs.ColorSpace == IccColorSpaceKind.Lab || pcs.ColorSpaceSignature == IccColorSpaceSignatures.Lab)
            {
                return pcs.Components;
            }
            if (pcs.ColorSpace == IccColorSpaceKind.Xyz || pcs.ColorSpaceSignature == IccColorSpaceSignatures.Xyz)
            {
                return ColorMathCore.ConvertXyzToLab(
                    pcs.Components,
                    new double[] { IccStandardColorimetry.D50X, IccStandardColorimetry.D50Y, IccStandardColorimetry.D50Z });
            }
        }

        if (color.Description != null && color.Description.Lab != null)
        {
            return new double[] { color.Description.Lab.L, color.Description.Lab.A, color.Description.Lab.B };
        }

        return null;
    }

    internal static string ComposeName(IccNamedColorSet colorSet, IccNamedColor color)
    {
        if (colorSet.Prefix.Length != 0 || colorSet.Suffix.Length != 0)
        {
            return colorSet.Prefix + color.Name + colorSet.Suffix;
        }
        if (color.Description.Name.Length != 0)
        {
            return color.Description.Name;
        }

        return color.Name;
    }

    internal static string NormalizeName(string value)
    {
        string compatible = value.Normalize(System.Text.NormalizationForm.FormKC);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool pendingSpace = false;
        int index = 0;
        while (index < compatible.Length)
        {
            char character = compatible[index];
            if (System.Char.IsWhiteSpace(character))
            {
                if (builder.Length != 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(System.Char.ToUpperInvariant(character));
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static void ValidateSet(IccNamedColorSet colorSet)
    {
        if (colorSet == null)
        {
            throw new IccTransformException("A named-colour collection cannot be null.");
        }
        if (colorSet.Prefix == null || colorSet.Suffix == null || colorSet.Colors == null)
        {
            throw new IccTransformException("A named-colour collection must contain non-null prefix, suffix, and colour values.");
        }
    }

    internal static void ValidateColor(IccNamedColor color, int index)
    {
        if (color == null)
        {
            throw new IccTransformException($"Named colour {index} cannot be null.");
        }

        ValidateStandaloneColor(color);
    }

    internal static void ValidateStandaloneColor(IccNamedColor color)
    {
        if (color == null)
        {
            throw new IccTransformException("A named colour cannot be null.");
        }
        if (color.Name == null || color.PcsColor == null || color.DeviceColor == null
            || color.Tints == null || color.Description == null || color.Description.Name == null)
        {
            throw new IccTransformException("A named colour must contain non-null name, coordinate, tint, and description values.");
        }
        if (color.PcsColor.Components == null || color.DeviceColor.Components == null)
        {
            throw new IccTransformException("Named-colour coordinate components cannot be null.");
        }
    }

    internal static void ValidateTint(double tint)
    {
        if (!double.IsFinite(tint) || tint < 0.0 || tint > 1.0)
        {
            throw new IccTransformException("A named-colour tint must be a finite value from zero through one.");
        }
    }

    internal static void ValidateTintSamples(double[] tints)
    {
        double previous = 0.0;
        int index = 0;
        while (index < tints.Length)
        {
            if (!double.IsFinite(tints[index]) || tints[index] <= previous || tints[index] > 1.0)
            {
                throw new IccTransformException("Named-colour tint samples must be finite, strictly increasing values above zero through one.");
            }

            previous = tints[index];
            index = index + 1;
        }
    }

    internal static void ValidateFiniteComponents(double[] components, string representation)
    {
        int index = 0;
        while (index < components.Length)
        {
            if (!double.IsFinite(components[index]))
            {
                throw new IccTransformException($"Named-colour {representation} component {index} must be finite.");
            }

            index = index + 1;
        }
    }

    internal static double[] CopyComponents(double[] source)
    {
        double[] result = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }
}
