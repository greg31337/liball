namespace json;

internal class JsonPropertySorter
{
    JsonPropertyOrder propertyOrder;
    int maximumObjectMembers;

    internal JsonPropertySorter(JsonWriterOptions options)
    {
        JsonWriterOptions normalized =
            JsonOptionNormalizer.NormalizeWriterOptions(options);
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException(
                "Normalized writer limits are required.");
        propertyOrder = normalized.PropertyOrder;
        maximumObjectMembers = limits.MaximumObjectMembers;
    }

    internal global::System.Collections.Generic.List<JsonMember> SortProperties(
        global::System.Collections.Generic.List<JsonMember> members)
    {
        if (members == null)
        {
            throw CreateWriteFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object member list is required for property ordering.");
        }

        if (members.Count > maximumObjectMembers)
        {
            throw CreateLimitFailure(
                "The configured object member limit was exceeded during property ordering.");
        }

        JsonMember[] source = new JsonMember[members.Count];
        int index = 0;
        while (index < members.Count)
        {
            JsonMember? member = members[index];
            if (member == null || member.Name == null)
            {
                throw CreateWriteFailure(
                    JsonDiagnosticCode.InvalidModel,
                    "Every ordered object member requires a decoded name.");
            }

            if (propertyOrder == JsonPropertyOrder.OrdinalUnicodeScalar ||
                propertyOrder == JsonPropertyOrder.Jcs)
            {
                ValidateScalarName(member.Name);
            }

            source[index] = member;
            index++;
        }

        if (propertyOrder != JsonPropertyOrder.Preserve &&
            source.Length > 1)
        {
            source = StableSort(source);
        }

        global::System.Collections.Generic.List<JsonMember> result =
            new global::System.Collections.Generic.List<JsonMember>(
                source.Length);
        index = 0;
        while (index < source.Length)
        {
            result.Add(source[index]);
            index++;
        }

        return result;
    }

    JsonMember[] StableSort(JsonMember[] original)
    {
        JsonMember[] source = original;
        JsonMember[] destination = new JsonMember[original.Length];
        int width = 1;
        while (width < original.Length)
        {
            int start = 0;
            while (start < original.Length)
            {
                int middle = start + width;
                if (middle > original.Length)
                {
                    middle = original.Length;
                }

                long proposedEnd = (long)start + ((long)width * 2);
                int end = proposedEnd > original.Length
                    ? original.Length
                    : (int)proposedEnd;
                MergeRange(source, destination, start, middle, end);
                start = end;
            }

            JsonMember[] temporary = source;
            source = destination;
            destination = temporary;
            if (width > original.Length / 2)
            {
                width = original.Length;
            }
            else
            {
                width *= 2;
            }
        }

        return source;
    }

    void MergeRange(
        JsonMember[] source,
        JsonMember[] destination,
        int start,
        int middle,
        int end)
    {
        int left = start;
        int right = middle;
        int output = start;
        while (left < middle && right < end)
        {
            string leftName = source[left].Name
                ?? throw new global::System.InvalidOperationException(
                    "A validated member requires a name.");
            string rightName = source[right].Name
                ?? throw new global::System.InvalidOperationException(
                    "A validated member requires a name.");
            if (CompareNames(leftName, rightName) <= 0)
            {
                destination[output] = source[left];
                left++;
            }
            else
            {
                destination[output] = source[right];
                right++;
            }

            output++;
        }

        while (left < middle)
        {
            destination[output] = source[left];
            left++;
            output++;
        }

        while (right < end)
        {
            destination[output] = source[right];
            right++;
            output++;
        }
    }

    int CompareNames(string left, string right)
    {
        if (propertyOrder == JsonPropertyOrder.OrdinalUnicodeScalar)
        {
            return CompareScalarNames(left, right);
        }

        return CompareUtf16Names(left, right);
    }

    int CompareUtf16Names(string left, string right)
    {
        int commonLength = left.Length < right.Length
            ? left.Length
            : right.Length;
        int index = 0;
        while (index < commonLength)
        {
            int difference = left[index] - right[index];
            if (difference != 0)
            {
                return difference;
            }

            index++;
        }

        return left.Length - right.Length;
    }

    int CompareScalarNames(string left, string right)
    {
        int leftIndex = 0;
        int rightIndex = 0;
        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            int leftScalar = GetScalarAt(left, leftIndex);
            int rightScalar = GetScalarAt(right, rightIndex);
            if (leftScalar != rightScalar)
            {
                return leftScalar < rightScalar ? -1 : 1;
            }

            leftIndex += leftScalar > 0xFFFF ? 2 : 1;
            rightIndex += rightScalar > 0xFFFF ? 2 : 1;
        }

        if (leftIndex == left.Length && rightIndex == right.Length)
        {
            return 0;
        }

        return leftIndex == left.Length ? -1 : 1;
    }

    int GetScalarAt(string name, int index)
    {
        int first = name[index];
        if (first < 0xD800 || first > 0xDBFF)
        {
            return first;
        }

        int second = name[index + 1];
        return 0x10000 + ((first - 0xD800) << 10) +
            (second - 0xDC00);
    }

    void ValidateScalarName(string name)
    {
        int index = 0;
        while (index < name.Length)
        {
            int value = name[index];
            if (value >= 0xD800 && value <= 0xDBFF)
            {
                if (index + 1 >= name.Length)
                {
                    throw CreateSurrogateFailure();
                }

                int lowSurrogate = name[index + 1];
                if (lowSurrogate < 0xDC00 || lowSurrogate > 0xDFFF)
                {
                    throw CreateSurrogateFailure();
                }

                index += 2;
                continue;
            }

            if (value >= 0xDC00 && value <= 0xDFFF)
            {
                throw CreateSurrogateFailure();
            }

            index++;
        }
    }

    JsonWriteException CreateSurrogateFailure()
    {
        return CreateWriteFailure(
            JsonDiagnosticCode.InvalidSurrogatePair,
            "Unicode-scalar property ordering requires paired UTF-16 surrogates.");
    }

    JsonWriteException CreateWriteFailure(
        JsonDiagnosticCode code,
        string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ActualToken = "PropertyName";
        return new JsonWriteException(diagnostic);
    }

    JsonLimitException CreateLimitFailure(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.ObjectMemberLimitExceeded;
        diagnostic.Message = message;
        diagnostic.ActualToken = "Object";
        return new JsonLimitException(diagnostic);
    }
}
