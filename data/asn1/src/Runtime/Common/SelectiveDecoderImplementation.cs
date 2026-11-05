namespace asn1;

internal class SelectiveDecoderImplementation
{
    internal ConfiguredCodec Codec;

    internal SelectiveDecoderImplementation(ConfiguredCodec codec)
    {
        this.Codec = codec == null
            ? throw new System.ArgumentNullException("codec")
            : codec;
    }

    internal AsnDecodeResult DecodeSelectedField(
        AsnDecodeRequest request,
        string[] fieldPath)
    {
        AsnDecodeResult decoded = this.DecodeFramedValue(request);
        if (decoded.Status != AsnDecodeStatus.Done || decoded.Value == null)
        {
            return decoded;
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Codec.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreateSelectionError(
                decoded.ConsumedCount,
                fieldPath,
                "The requested schema type is missing or ambiguous.");
        }

        AsnRuntimeValue? selected = this.SelectValueByPath(
            decoded.Value,
            type,
            fieldPath);
        if (selected == null)
        {
            return this.CreateSelectionError(
                decoded.ConsumedCount,
                fieldPath,
                "The reflected field path does not identify a present value.");
        }

        decoded.Value = selected;
        return decoded;
    }

    internal AsnDecodeResult SkipSelectedField(
        AsnDecodeRequest request,
        string[] fieldPath)
    {
        AsnDecodeResult decoded = this.DecodeFramedValue(request);
        if (decoded.Status != AsnDecodeStatus.Done || decoded.Value == null)
        {
            return decoded;
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Codec.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreateSelectionError(
                decoded.ConsumedCount,
                fieldPath,
                "The requested schema type is missing or ambiguous.");
        }

        if (fieldPath == null)
        {
            throw new System.ArgumentNullException("fieldPath");
        }
        if (fieldPath.Length == 0)
        {
            decoded.Value = null;
            return decoded;
        }

        AsnRuntimeValue? selected = this.SelectValueByPath(
            decoded.Value,
            type,
            fieldPath);
        if (selected == null || !this.RemoveValueByPath(
            decoded.Value,
            type,
            fieldPath))
        {
            return this.CreateSelectionError(
                decoded.ConsumedCount,
                fieldPath,
                "The reflected field path cannot be skipped.");
        }

        return decoded;
    }

    internal AsnDecodeResult DecodeFramedValue(AsnDecodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (request.Data == null)
        {
            throw new System.ArgumentException(
                "The selective decode input data cannot be null.",
                "request");
        }

        int count = request.HasCount
            ? request.Count
            : request.Data.Length - request.Offset;
        if (!CursorMath.IsRangeValid(
            request.Offset,
            count,
            request.Data.Length))
        {
            throw new System.ArgumentOutOfRangeException("request");
        }

        AsnDecodeResult decoded = this.Codec.Decode(request);
        if (decoded.Status == AsnDecodeStatus.Done
            && decoded.ConsumedCount != count)
        {
            return new AsnDecodeResult
            {
                Status = AsnDecodeStatus.Error,
                ConsumedCount = decoded.ConsumedCount,
                Diagnostics =
                [
                    new AsnDiagnostic
                    {
                        Severity = AsnDiagnosticSeverity.Error,
                        Category = AsnDiagnosticCategory.Decoding,
                        Code = "ASNSELECT0002",
                        Message = "Selective decoding requires exactly one framed value with no trailing input."
                    }
                ]
            };
        }
        return decoded;
    }

    internal AsnRuntimeValue? SelectValueByPath(
        AsnRuntimeValue root,
        AsnTypeDescriptor rootType,
        string[] fieldPath)
    {
        if (root == null)
        {
            throw new System.ArgumentNullException("root");
        }
        if (rootType == null)
        {
            throw new System.ArgumentNullException("rootType");
        }
        if (fieldPath == null)
        {
            throw new System.ArgumentNullException("fieldPath");
        }

        AsnRuntimeValue? current = root;
        AsnTypeDescriptor? currentType = rootType;
        int index = 0;
        while (index < fieldPath.Length
            && current != null
            && currentType != null)
        {
            string segment = fieldPath[index];
            if (segment == null || segment.Length == 0)
            {
                return null;
            }

            if (currentType.Kind == AsnTypeKind.SequenceOf
                || currentType.Kind == AsnTypeKind.SetOf)
            {
                int itemIndex = this.ParseCollectionIndex(segment);
                if (itemIndex < 0 || itemIndex >= current.Items.Length)
                {
                    return null;
                }
                current = current.Items[itemIndex];
                currentType = currentType.ElementType;
            }
            else if (currentType.Kind == AsnTypeKind.OpenType)
            {
                if (segment != "value"
                    || current.OpenTypeValue == null
                    || !current.OpenTypeValue.Resolved
                    || current.OpenTypeValue.Value == null)
                {
                    return null;
                }
                currentType = this.ResolveOpenTypeDescriptor(
                    current.OpenTypeValue);
                current = current.OpenTypeValue.Value;
            }
            else
            {
                AsnFieldDescriptor? field = this.FindFieldDescriptor(
                    currentType,
                    segment);
                AsnRuntimeFieldValue? value = this.FindRuntimeField(
                    current,
                    field);
                if (field == null || value == null
                    || !value.Present || value.Value == null)
                {
                    return null;
                }
                current = value.Value;
                currentType = field.Type;
            }
            index = index + 1;
        }
        return index == fieldPath.Length ? current : null;
    }

    internal bool RemoveValueByPath(
        AsnRuntimeValue root,
        AsnTypeDescriptor rootType,
        string[] fieldPath)
    {
        AsnRuntimeValue current = root;
        AsnTypeDescriptor currentType = rootType;
        int index = 0;
        while (index + 1 < fieldPath.Length)
        {
            AsnRuntimeValue? next = this.SelectValueByPath(
                current,
                currentType,
                [fieldPath[index]]);
            AsnTypeDescriptor? nextType = this.SelectTypeBySegment(
                current,
                currentType,
                fieldPath[index]);
            if (next == null || nextType == null)
            {
                return false;
            }
            current = next;
            currentType = nextType;
            index = index + 1;
        }

        string finalSegment = fieldPath[fieldPath.Length - 1];
        if (currentType.Kind == AsnTypeKind.SequenceOf
            || currentType.Kind == AsnTypeKind.SetOf)
        {
            int itemIndex = this.ParseCollectionIndex(finalSegment);
            if (itemIndex < 0 || itemIndex >= current.Items.Length)
            {
                return false;
            }
            AsnRuntimeValue[] remaining = new AsnRuntimeValue[
                current.Items.Length - 1];
            int sourceIndex = 0;
            int targetIndex = 0;
            while (sourceIndex < current.Items.Length)
            {
                if (sourceIndex != itemIndex)
                {
                    remaining[targetIndex] = current.Items[sourceIndex];
                    targetIndex = targetIndex + 1;
                }
                sourceIndex = sourceIndex + 1;
            }
            current.Items = remaining;
            return true;
        }

        if (currentType.Kind == AsnTypeKind.OpenType)
        {
            return false;
        }

        AsnFieldDescriptor? field = this.FindFieldDescriptor(
            currentType,
            finalSegment);
        AsnRuntimeFieldValue? runtime = this.FindRuntimeField(
            current,
            field);
        if (runtime == null || !runtime.Present)
        {
            return false;
        }
        runtime.Present = false;
        runtime.PresentDefault = false;
        runtime.Value = null;
        return true;
    }

    internal AsnTypeDescriptor? SelectTypeBySegment(
        AsnRuntimeValue current,
        AsnTypeDescriptor currentType,
        string segment)
    {
        if (currentType.Kind == AsnTypeKind.SequenceOf
            || currentType.Kind == AsnTypeKind.SetOf)
        {
            return currentType.ElementType;
        }
        if (currentType.Kind == AsnTypeKind.OpenType)
        {
            return current.OpenTypeValue == null
                ? null
                : this.ResolveOpenTypeDescriptor(current.OpenTypeValue);
        }
        AsnFieldDescriptor? field = this.FindFieldDescriptor(
            currentType,
            segment);
        return field == null ? null : field.Type;
    }

    internal AsnFieldDescriptor? FindFieldDescriptor(
        AsnTypeDescriptor type,
        string segment)
    {
        int index = 0;
        while (index < type.Fields.Length)
        {
            AsnFieldDescriptor field = type.Fields[index];
            if (field.Name == segment
                || field.GeneratedName == segment
                || field.StableId == segment)
            {
                return field;
            }
            index = index + 1;
        }
        return null;
    }

    internal AsnRuntimeFieldValue? FindRuntimeField(
        AsnRuntimeValue value,
        AsnFieldDescriptor? field)
    {
        if (field == null)
        {
            return null;
        }
        int index = 0;
        while (index < value.Fields.Length)
        {
            if (value.Fields[index].Name == field.Name)
            {
                return value.Fields[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal int ParseCollectionIndex(string segment)
    {
        int start = 0;
        int end = segment.Length;
        if (segment.Length >= 3
            && segment[0] == '['
            && segment[segment.Length - 1] == ']')
        {
            start = 1;
            end = segment.Length - 1;
        }
        if (start == end)
        {
            return -1;
        }

        int value = 0;
        int index = start;
        while (index < end)
        {
            int digit = segment[index] - '0';
            if (digit < 0 || digit > 9
                || value > (int.MaxValue - digit) / 10)
            {
                return -1;
            }
            value = value * 10 + digit;
            index = index + 1;
        }
        return value;
    }

    internal AsnTypeDescriptor? ResolveOpenTypeDescriptor(
        AsnOpenTypeValue open)
    {
        int index = 0;
        while (index < this.Codec.Catalog.Assignments.Length)
        {
            AsnAssignmentDescriptor assignment =
                this.Codec.Catalog.Assignments[index];
            if (assignment.Type != null
                && (assignment.Type.StableId == open.ResolvedTypeId
                    || assignment.QualifiedName == open.TypeName))
            {
                return assignment.Type;
            }
            index = index + 1;
        }
        return null;
    }

    internal AsnDecodeResult CreateSelectionError(
        int consumedCount,
        string[] fieldPath,
        string message)
    {
        if (fieldPath == null)
        {
            throw new System.ArgumentNullException("fieldPath");
        }
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumedCount,
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Decoding,
                    Code = "ASNSELECT0001",
                    Message = message,
                    ValuePath = this.FormatFieldPath(fieldPath)
                }
            ]
        };
    }

    internal string FormatFieldPath(string[] fieldPath)
    {
        string result = "$";
        int index = 0;
        while (index < fieldPath.Length)
        {
            string segment = fieldPath[index] == null
                ? ""
                : fieldPath[index];
            result = segment.StartsWith(
                "[",
                System.StringComparison.Ordinal)
                ? result + segment
                : result + "." + segment;
            index = index + 1;
        }
        return result;
    }
}
