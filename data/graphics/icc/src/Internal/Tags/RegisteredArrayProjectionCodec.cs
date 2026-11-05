namespace icc;

internal static class RegisteredArrayProjectionCodec
{
    internal static string ProjectDecoded(TagDecodeState state, IccTagData data)
    {
        if (data.TagArray == null)
        {
            return "A registered array projection requires generic tag-array data.";
        }

        IccTagArray array = data.TagArray;
        string error = ValidateArray(array);
        if (error.Length != 0)
        {
            return error;
        }

        switch (array.ArraySignature)
        {
            case IccArraySignatures.NamedColor:
                return ProjectNamedColors(state, array, data);
            case IccArraySignatures.ProfileInformation:
                return ProjectProfileInformation(array, data);
            case IccArraySignatures.MultiplexType:
                return ProjectMultiplexTypes(state, array, data);
            default:
                return string.Empty;
        }
    }

    internal static void ValidateForWrite(IccTagArray array)
    {
        string error = ValidateArray(array);
        if (error.Length != 0)
        {
            throw new IccProfileWriteException(error);
        }
    }

    internal static string ProjectMultiplexDefaults(TagDecodeState state, IccTagData data)
    {
        if (state.TagSignature != IccTagSignatures.MultiplexDefaultValues)
        {
            return string.Empty;
        }

        uint typeSignature = data.RawData.TypeSignature;
        double[] values;
        if (data.Kind == IccTagDataKind.IntegerArray)
        {
            double divisor;
            if (typeSignature == IccTypeSignatures.UInt8Array)
            {
                divisor = byte.MaxValue;
            }
            else if (typeSignature == IccTypeSignatures.UInt16Array)
            {
                divisor = ushort.MaxValue;
            }
            else
            {
                return "A multiplexDefaultValuesTag integer payload shall use uInt8ArrayType or uInt16ArrayType.";
            }

            values = new double[data.IntegerValues.Length];
            int integerIndex = 0;
            while (integerIndex < values.Length)
            {
                values[integerIndex] = data.IntegerValues[integerIndex] / divisor;
                integerIndex = integerIndex + 1;
            }
        }
        else if (data.Kind == IccTagDataKind.FloatingPointArray)
        {
            if (typeSignature != IccTypeSignatures.Float16Array && typeSignature != IccTypeSignatures.Float32Array)
            {
                return "A multiplexDefaultValuesTag floating-point payload shall use float16ArrayType or float32ArrayType.";
            }

            values = CloneDoubles(data.FloatingPointValues);
        }
        else
        {
            return "A multiplexDefaultValuesTag shall contain an integer or floating-point array.";
        }

        string countError = ValidateMcsCount(state, values.Length, "multiplexDefaultValuesTag");
        if (countError.Length != 0)
        {
            return countError;
        }

        IccMultiplexChannels projection = new IccMultiplexChannels();
        projection.DefaultValues = values;
        projection.Order = CreateOrder(values.Length);
        data.MultiplexChannels = projection;
        return string.Empty;
    }

    internal static void ProjectColorEncodingName(TagDecodeState state, IccTagData data)
    {
        if ((state.TagSignature != IccTagSignatures.ReferenceName && state.TagSignature != IccTagSignatures.ColorSpaceName)
            || data.Text == null || data.Text.TypeSignature != IccTypeSignatures.Utf8)
        {
            return;
        }

        IccColorEncodingParameters projection = new IccColorEncodingParameters();
        projection.Name = data.Text.Text;
        data.ColorEncodingParameters = projection;
    }

    internal static string ValidateArray(IccTagArray array)
    {
        if (array.Values == null)
        {
            return "Tag-array values cannot be null.";
        }

        if (array.ArraySignature == IccArraySignatures.NamedColor)
        {
            return ValidateNamedColorArray(array);
        }

        if (array.ArraySignature == IccArraySignatures.ProfileInformation)
        {
            return ValidateStructureArray(array, IccStructureSignatures.ProfileInformation, "profileInfoArray");
        }

        if (array.ArraySignature == IccArraySignatures.Generic)
        {
            return ValidateGenericStructureArray(array);
        }

        if (array.ArraySignature == IccArraySignatures.MultiplexType)
        {
            return ValidateMultiplexTypeArray(array);
        }

        return string.Empty;
    }

    internal static string ValidateNamedColorArray(IccTagArray array)
    {
        if (array.Values.Length < 2)
        {
            return "A namedColorArray shall contain one tintZeroStructure followed by at least one namedColorStructure.";
        }

        System.Collections.Generic.Dictionary<string, bool> names = new System.Collections.Generic.Dictionary<string, bool>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < array.Values.Length)
        {
            IccTagData item = array.Values[index];
            uint expectedSignature = index == 0 ? IccStructureSignatures.TintZero : IccStructureSignatures.NamedColor;
            if (item == null || item.Kind != IccTagDataKind.TagStructure || item.TagStructure == null
                || item.TagStructure.StructureSignature != expectedSignature)
            {
                return index == 0
                    ? "The first namedColorArray element shall be a tintZeroStructure."
                    : $"NamedColorArray element {index} shall be a namedColorStructure.";
            }

            if (index != 0)
            {
                string name = GetNamedColorName(item);
                if (name.Length != 0 && names.ContainsKey(name))
                {
                    return $"NamedColorArray element {index} duplicates the case-sensitive name '{name}'.";
                }

                if (name.Length != 0)
                {
                    names.Add(name, true);
                }
            }

            index = index + 1;
        }

        return string.Empty;
    }

    internal static string ValidateStructureArray(IccTagArray array, uint expectedSignature, string description)
    {
        int index = 0;
        while (index < array.Values.Length)
        {
            IccTagData item = array.Values[index];
            if (item == null || item.Kind != IccTagDataKind.TagStructure || item.TagStructure == null
                || item.TagStructure.StructureSignature != expectedSignature)
            {
                return $"Every {description} element shall be a tagStructType with structure signature '{FourCcCodec.Format(expectedSignature)}'.";
            }

            index = index + 1;
        }

        return string.Empty;
    }

    internal static string ValidateGenericStructureArray(IccTagArray array)
    {
        uint structureSignature = 0;
        int index = 0;
        while (index < array.Values.Length)
        {
            IccTagData item = array.Values[index];
            if (item == null || item.Kind != IccTagDataKind.TagStructure || item.TagStructure == null)
            {
                return "Every tagStructArray element shall use tagStructType.";
            }

            if (index == 0)
            {
                structureSignature = item.TagStructure.StructureSignature;
            }
            else if (item.TagStructure.StructureSignature != structureSignature)
            {
                return "All tagStructArray elements shall have the same structure type signature.";
            }

            index = index + 1;
        }

        return string.Empty;
    }

    internal static string ValidateMultiplexTypeArray(IccTagArray array)
    {
        System.Collections.Generic.Dictionary<string, bool> names = new System.Collections.Generic.Dictionary<string, bool>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < array.Values.Length)
        {
            IccTagData item = array.Values[index];
            if (item == null || item.Kind != IccTagDataKind.Text || item.Text == null
                || item.Text.TypeSignature != IccTypeSignatures.Utf8 || item.Text.Text.Length == 0)
            {
                return $"MultiplexTypeArray element {index} shall contain a non-empty utf8Type channel name.";
            }

            if (names.ContainsKey(item.Text.Text))
            {
                return $"Multiplex channel type name '{item.Text.Text}' is duplicated; matching is case-sensitive.";
            }

            names.Add(item.Text.Text, true);
            index = index + 1;
        }

        return string.Empty;
    }

    internal static string ProjectNamedColors(TagDecodeState state, IccTagArray array, IccTagData data)
    {
        IccNamedColor[] colors = new IccNamedColor[array.Values.Length - 1];
        int index = 1;
        while (index < array.Values.Length)
        {
            IccNamedColorSet? nested = array.Values[index].NamedColors;
            if (nested == null || nested.Colors.Length != 1)
            {
                return $"NamedColorArray element {index} did not produce one named-colour record.";
            }

            colors[index - 1] = nested.Colors[0];
            index = index + 1;
        }

        IccNamedColorSet projection = new IccNamedColorSet();
        projection.DeviceChannelCount = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        IccNamedColorSet? tintZero = array.Values[0].NamedColors;
        if (tintZero == null || tintZero.Colors.Length != 1)
        {
            return "The namedColorArray tintZeroStructure did not produce one zero-tint record.";
        }

        projection.TintZero = tintZero.Colors[0];
        projection.Colors = colors;
        data.NamedColors = projection;
        return string.Empty;
    }

    internal static string ProjectProfileInformation(IccTagArray array, IccTagData data)
    {
        IccProfileSequenceEntry[] entries = new IccProfileSequenceEntry[array.Values.Length];
        int index = 0;
        while (index < array.Values.Length)
        {
            if (array.Values[index].ProfileSequence.Length != 1)
            {
                return $"ProfileInfoArray element {index} did not produce one profile-information record.";
            }

            entries[index] = array.Values[index].ProfileSequence[0];
            index = index + 1;
        }

        data.ProfileSequence = entries;
        return string.Empty;
    }

    internal static string ProjectMultiplexTypes(TagDecodeState state, IccTagArray array, IccTagData data)
    {
        string countError = ValidateMcsCount(state, array.Values.Length, "multiplexTypeArrayTag");
        if (countError.Length != 0)
        {
            return countError;
        }

        string[] names = new string[array.Values.Length];
        int index = 0;
        while (index < names.Length)
        {
            IccTextData? text = array.Values[index].Text;
            names[index] = text == null ? string.Empty : text.Text;
            index = index + 1;
        }

        IccMultiplexChannels projection = new IccMultiplexChannels();
        projection.Names = names;
        projection.Order = CreateOrder(names.Length);
        data.MultiplexChannels = projection;
        return string.Empty;
    }

    internal static string ValidateMcsCount(TagDecodeState state, int count, string description)
    {
        if (!state.HasProfileContext)
        {
            return string.Empty;
        }

        uint signature = state.MaterialConnectionSpaceSignature;
        int expected = (int)(signature & 0x0000ffff);
        if (state.MaterialConnectionSpace != IccColorSpaceKind.Multiplex
            || (signature & 0xffff0000) != IccColorSpaceSignatures.MultiplexPrefix || expected == 0)
        {
            return $"A {description} requires a multiplex Material Connection Space in the profile header.";
        }

        if (count != expected)
        {
            return $"The {description} contains {count} entries, but the profile header MCS declares {expected} channels.";
        }

        return string.Empty;
    }

    internal static string GetNamedColorName(IccTagData data)
    {
        if (data.NamedColors != null && data.NamedColors.Colors.Length == 1)
        {
            return data.NamedColors.Colors[0].Name;
        }

        if (data.TagStructure == null)
        {
            return string.Empty;
        }

        IccStructureMember? member = StructuredTagProjectionCodec.FindMember(data.TagStructure, StructuredTagProjectionCodec.Name);
        return member == null || member.Data.Text == null ? string.Empty : member.Data.Text.Text;
    }

    internal static int[] CreateOrder(int count)
    {
        int[] values = new int[count];
        int index = 0;
        while (index < count)
        {
            values[index] = index;
            index = index + 1;
        }

        return values;
    }

    internal static double[] CloneDoubles(double[] source)
    {
        double[] values = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            values[index] = source[index];
            index = index + 1;
        }

        return values;
    }
}
