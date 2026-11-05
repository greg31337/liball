using System.Text;

namespace ttf;

internal static class SfntCoordinateResolver
{
    const int FixedScale = 65536;
    const int NormalizedScale = 16384;

    internal static NormalizedFontCoordinates Resolve(
        FontFace face,
        FontCoordinates coordinates)
    {
        if (face == null)
        {
            throw new System.ArgumentNullException("face");
        }

        if (coordinates == null)
        {
            throw new System.ArgumentNullException("coordinates");
        }

        SfntFvarTable? fvar = face.GetFvarTable();

        if (fvar == null)
        {
            if (face.HasTable(FontTags.Fvar))
            {
                throw FontFormatException.Create(
                    "Coordinate resolution requires a supported fvar table.");
            }

            if (coordinates.GetCount() != 0)
            {
                throw new System.ArgumentException(
                    "The font face does not define variation axes.",
                    "coordinates");
            }

            return CreateEmpty();
        }

        ValidateRequestedAxes(fvar, coordinates);
        int axisCount = fvar.GetAxisCount();
        int[] tags = new int[axisCount];
        int[] userFixedValues = new int[axisCount];
        double[] userValues = new double[axisCount];
        double[] requestedValues = new double[axisCount];
        bool[] explicitlySpecified = new bool[axisCount];
        int[] normalized = new int[axisCount];
        int[] unmappedNormalized = new int[axisCount];
        bool defaultUserLocation = true;
        int axisIndex = 0;

        while (axisIndex < axisCount)
        {
            SfntFvarAxis axis = fvar.GetAxis(axisIndex);
            int tag = axis.GetTag();
            bool specified = coordinates.Contains(tag);
            double value = FindRequestedValue(
                coordinates,
                tag,
                axis.GetDefaultValue());
            requestedValues[axisIndex] = value;
            explicitlySpecified[axisIndex] = specified;
            value = Clamp(
                value,
                axis.GetMinimumValue(),
                axis.GetMaximumValue());
            int fixedValue = ToFixed16Dot16(value);
            tags[axisIndex] = tag;
            userFixedValues[axisIndex] = fixedValue;
            userValues[axisIndex] = fixedValue / (double)FixedScale;
            normalized[axisIndex] = NormalizeAxis(axis, fixedValue);
            unmappedNormalized[axisIndex] = FixedToF2Dot14(
                normalized[axisIndex]);

            if (fixedValue != ToFixed16Dot16(axis.GetDefaultValue()))
            {
                defaultUserLocation = false;
            }

            axisIndex += 1;
        }

        SfntAvarTable? avar = face.GetAvarTable();

        if (avar == null && face.HasTable(FontTags.Avar))
        {
            throw FontFormatException.Create(
                "Coordinate resolution requires a supported avar table.");
        }

        ApplyAvar(avar, normalized);
        double[] normalizedValues = new double[axisCount];
        axisIndex = 0;

        while (axisIndex < axisCount)
        {
            normalizedValues[axisIndex] =
                normalized[axisIndex] / (double)NormalizedScale;
            axisIndex += 1;
        }

        FontCoordinateMapping[] mappings = new FontCoordinateMapping[axisCount];
        axisIndex = 0;

        while (axisIndex < axisCount)
        {
            SfntFvarAxis axis = fvar.GetAxis(axisIndex);
            mappings[axisIndex] = new FontCoordinateMapping(
                tags[axisIndex],
                explicitlySpecified[axisIndex],
                requestedValues[axisIndex],
                axis.GetMinimumValue(),
                axis.GetDefaultValue(),
                axis.GetMaximumValue(),
                userValues[axisIndex],
                unmappedNormalized[axisIndex] / (double)NormalizedScale,
                normalizedValues[axisIndex]);
            axisIndex += 1;
        }

        int namedInstanceIndex = FindNamedInstance(
            fvar,
            userFixedValues);
        NormalizedFontCoordinates result = new NormalizedFontCoordinates(
            tags,
            userValues,
            normalizedValues,
            normalized,
            CreateKey(tags, normalized),
            namedInstanceIndex,
            defaultUserLocation);
        result.SetMappings(mappings);
        return result;
    }

    internal static void ValidateUserAxisTag(int tag)
    {
        uint value = CheckedFontMath.ReinterpretInt32AsUInt32(tag);
        int first = (int)((value >> 24) & 0xFFU);
        bool firstUpper = first >= 0x41 && first <= 0x5A;
        bool firstLower = first >= 0x61 && first <= 0x7A;

        if (!firstUpper && !firstLower)
        {
            throw new System.ArgumentOutOfRangeException("axisTag");
        }

        bool foundSpace = false;
        int index = 0;

        while (index < 4)
        {
            int character = (int)((value >> (24 - index * 8)) & 0xFFU);
            bool upper = character >= 0x41 && character <= 0x5A;
            bool lower = character >= 0x61 && character <= 0x7A;
            bool digit = character >= 0x30 && character <= 0x39;

            if (character == 0x20)
            {
                foundSpace = true;
            }
            else if (foundSpace || !upper && !lower && !digit)
            {
                throw new System.ArgumentOutOfRangeException("axisTag");
            }

            if (firstUpper && !upper && !digit)
            {
                throw new System.ArgumentOutOfRangeException("axisTag");
            }

            index += 1;
        }
    }

    static NormalizedFontCoordinates CreateEmpty()
    {
        return new NormalizedFontCoordinates(
            new int[0],
            new double[0],
            new double[0],
            new int[0],
            "v1",
            -1,
            true);
    }

    static void ValidateRequestedAxes(
        SfntFvarTable fvar,
        FontCoordinates coordinates)
    {
        int index = 0;

        while (index < coordinates.GetCount())
        {
            if (fvar.FindAxis(coordinates.GetAxisTag(index)) == null)
            {
                throw new System.ArgumentException(
                    "A coordinate references an axis not defined by the font.",
                    "coordinates");
            }

            index += 1;
        }
    }

    static double FindRequestedValue(
        FontCoordinates coordinates,
        int tag,
        double defaultValue)
    {
        int index = 0;

        while (index < coordinates.GetCount())
        {
            if (coordinates.GetAxisTag(index) == tag)
            {
                return coordinates.GetValue(index);
            }

            index += 1;
        }

        return defaultValue;
    }

    static double Clamp(double value, double minimum, double maximum)
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

    static int ToFixed16Dot16(double value)
    {
        double scaled = value * FixedScale;
        return (int)System.Math.Floor(scaled + 0.5);
    }

    static int NormalizeAxis(SfntFvarAxis axis, int value)
    {
        int minimum = ToFixed16Dot16(axis.GetMinimumValue());
        int defaultValue = ToFixed16Dot16(axis.GetDefaultValue());
        int maximum = ToFixed16Dot16(axis.GetMaximumValue());
        double normalized = 0;

        if (value < defaultValue)
        {
            normalized = (value - defaultValue) /
                (double)(defaultValue - minimum);
        }
        else if (value > defaultValue)
        {
            normalized = (value - defaultValue) /
                (double)(maximum - defaultValue);
        }

        int fixedNormalized = ToFixed16Dot16(
            Clamp(normalized, -1, 1));
        return fixedNormalized;
    }

    static int FixedToF2Dot14(int value)
    {
        return (value + 2) >> 2;
    }

    static void ApplyAvar(SfntAvarTable? avar, int[] normalized)
    {
        if (avar != null && avar.GetSegmentMapCount() != 0)
        {
            int axisIndex = 0;

            while (axisIndex < normalized.Length)
            {
                normalized[axisIndex] = MapSegment(
                    avar.GetSegmentMap(axisIndex),
                    normalized[axisIndex]);
                axisIndex += 1;
            }
        }

        int conversionIndex = 0;

        while (conversionIndex < normalized.Length)
        {
            normalized[conversionIndex] = FixedToF2Dot14(
                normalized[conversionIndex]);
            conversionIndex += 1;
        }

        if (avar != null && avar.GetMajorVersion() >= 2 &&
            avar.GetVariationStore() != null)
        {
            ApplyAvarVersionTwo(avar, normalized);
        }
    }

    static int MapSegment(SfntAvarSegmentMap map, int encodedValue)
    {
        if (!map.HasRequiredMappings() || map.GetValueMapCount() == 0)
        {
            return encodedValue;
        }

        double value = encodedValue / (double)FixedScale;
        int endIndex = 0;

        while (endIndex < map.GetValueMapCount() &&
            map.GetValueMap(endIndex).GetFromCoordinate() < value)
        {
            endIndex += 1;
        }

        if (endIndex >= map.GetValueMapCount())
        {
            endIndex = map.GetValueMapCount() - 1;
        }

        SfntAvarAxisValueMap end = map.GetValueMap(endIndex);
        double mapped = end.GetToCoordinate();

        if (end.GetFromCoordinate() != value && endIndex != 0)
        {
            SfntAvarAxisValueMap start = map.GetValueMap(endIndex - 1);
            double ratio = (value - start.GetFromCoordinate()) /
                (end.GetFromCoordinate() - start.GetFromCoordinate());
            mapped = start.GetToCoordinate() + ratio *
                (end.GetToCoordinate() - start.GetToCoordinate());
        }

        int fixedMapped = ToFixed16Dot16(Clamp(mapped, -1, 1));
        return fixedMapped;
    }

    static void ApplyAvarVersionTwo(
        SfntAvarTable avar,
        int[] normalized)
    {
        double[] location = new double[normalized.Length];
        int index = 0;

        while (index < location.Length)
        {
            location[index] = normalized[index] / (double)NormalizedScale;
            index += 1;
        }

        SfntItemVariationStore? variationStore = avar.GetVariationStore();
        System.ArgumentNullException.ThrowIfNull(variationStore);
        SfntVariationLocation variationLocation =
            variationStore.CreateLocation(location);
        index = 0;

        while (index < normalized.Length)
        {
            SfntDeltaSetIndex variationIndex =
                avar.GetVariationIndexForAxis(index);
            double delta = variationLocation.CalculateDelta(variationIndex);
            int adjusted = normalized[index] +
                (int)System.Math.Floor(delta + 0.5);

            if (adjusted < -NormalizedScale)
            {
                adjusted = -NormalizedScale;
            }
            else if (adjusted > NormalizedScale)
            {
                adjusted = NormalizedScale;
            }

            normalized[index] = adjusted;
            index += 1;
        }
    }

    static int FindNamedInstance(
        SfntFvarTable fvar,
        int[] userValues)
    {
        int instanceIndex = 0;

        while (instanceIndex < fvar.GetInstanceCount())
        {
            SfntFvarInstance instance = fvar.GetInstance(instanceIndex);
            bool matches = true;
            int axisIndex = 0;

            while (axisIndex < userValues.Length && matches)
            {
                matches = userValues[axisIndex] ==
                    ToFixed16Dot16(instance.GetCoordinate(axisIndex));
                axisIndex += 1;
            }

            if (matches)
            {
                return instanceIndex;
            }

            instanceIndex += 1;
        }

        return -1;
    }

    static string CreateKey(int[] tags, int[] normalized)
    {
        StringBuilder builder = new StringBuilder("v1");
        int index = 0;

        while (index < tags.Length)
        {
            builder.Append(':');
            builder.Append(CheckedFontMath.ReinterpretInt32AsUInt32(
                tags[index]).ToString(
                "X8",
                System.Globalization.CultureInfo.InvariantCulture));
            builder.Append('=');
            builder.Append((normalized[index] & 0xFFFF).ToString(
                "X4",
                System.Globalization.CultureInfo.InvariantCulture));
            index += 1;
        }

        return builder.ToString();
    }
}
