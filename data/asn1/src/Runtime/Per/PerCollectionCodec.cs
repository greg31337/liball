namespace asn1;

internal class PerCollectionCodec
{
    internal AsnResourceLimits Limits;
    internal PerLengthCodec Lengths;
    internal PerPrimitiveCodec Primitives;
    internal PerOpenTypeCodec OpenTypes;

    internal PerCollectionCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Lengths = new PerLengthCodec(limits);
        this.Primitives = new PerPrimitiveCodec(limits);
        this.OpenTypes = new PerOpenTypeCodec(limits);
    }

    internal PerCollectionResult BuildRootPresence(
        AsnFieldDescriptor[] rootFields,
        AsnRuntimeFieldValue[] values,
        AsnReflectionCatalog catalog)
    {
        if (rootFields == null)
        {
            throw new System.ArgumentNullException("rootFields");
        }

        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (catalog == null)
        {
            throw new System.ArgumentNullException("catalog");
        }

        int optionalCount = 0;
        int descriptorIndex = 0;
        while (descriptorIndex < rootFields.Length)
        {
            if (rootFields[descriptorIndex].Optional
                || rootFields[descriptorIndex].HasDefaultValue)
            {
                optionalCount = optionalCount + 1;
            }

            descriptorIndex = descriptorIndex + 1;
        }

        bool[] presence = new bool[optionalCount];
        int presenceIndex = 0;
        descriptorIndex = 0;
        while (descriptorIndex < rootFields.Length)
        {
            AsnFieldDescriptor descriptor = rootFields[descriptorIndex];
            if (descriptor.Optional || descriptor.HasDefaultValue)
            {
                AsnRuntimeFieldValue? field = this.FindRuntimeField(
                    values,
                    descriptor.Name);
                bool present = false;
                if (field != null && field.Present)
                {
                    if (field.Value == null)
                    {
                        return this.CreateError(
                            "ASNP0055",
                            "A present optional or DEFAULT component requires a value.",
                            false);
                    }

                    present = true;
                    if (descriptor.HasDefaultValue)
                    {
                        present = !this.MatchesDefault(
                            descriptor,
                            field.Value,
                            catalog);
                    }
                }

                presence[presenceIndex] = present;
                presenceIndex = presenceIndex + 1;
            }

            descriptorIndex = descriptorIndex + 1;
        }

        return new PerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            RootPresence = presence
        };
    }

    internal PerCollectionResult WriteRootPreamble(
        PerBitWriter writer,
        bool extensible,
        bool extensionPresent,
        bool[] rootPresence)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (rootPresence == null)
        {
            throw new System.ArgumentNullException("rootPresence");
        }

        if (!extensible && extensionPresent)
        {
            return this.CreateError(
                "ASNP0056",
                "A non-extensible collection cannot carry extension additions.",
                false);
        }

        int start = writer.GetWrittenBitCount();
        if (extensible)
        {
            PerBitWriteResult extension = writer.WriteBit(extensionPresent);
            if (!extension.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        PerCollectionResult bitmap = this.WriteBitmap(
            writer,
            rootPresence);
        if (!bitmap.Success)
        {
            return bitmap;
        }

        bitmap.ExtensionPresent = extensionPresent;
        bitmap.RootPresence = this.CopyBooleans(rootPresence);
        bitmap.BitCount = writer.GetWrittenBitCount() - start;
        return bitmap;
    }

    internal PerCollectionResult ReadRootPreamble(
        PerBitReader reader,
        bool extensible,
        int rootPresenceCount)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        if (rootPresenceCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("rootPresenceCount");
        }

        int start = reader.Position;
        bool extensionPresent = false;
        if (extensible)
        {
            PerBitReadResult extension = reader.ReadBit();
            if (extension.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, extension, start);
            }

            extensionPresent = extension.Value
                != System.Numerics.BigInteger.Zero;
        }

        PerCollectionResult bitmap = this.ReadBitmap(
            reader,
            rootPresenceCount,
            start);
        if (!bitmap.Success)
        {
            return bitmap;
        }

        bitmap.ExtensionPresent = extensionPresent;
        bitmap.RootPresence = bitmap.ExtensionPresence;
        bitmap.ExtensionPresence = [];
        bitmap.BitCount = reader.Position - start;
        return bitmap;
    }

    internal PerCollectionResult BuildExtensionPresence(
        bool[] componentPresence,
        int[] additionIndexes,
        int additionCount)
    {
        if (componentPresence == null)
        {
            throw new System.ArgumentNullException("componentPresence");
        }

        if (additionIndexes == null)
        {
            throw new System.ArgumentNullException("additionIndexes");
        }

        if (componentPresence.Length != additionIndexes.Length
            || additionCount < 0)
        {
            return this.CreateError(
                "ASNP0057",
                "Extension component and addition metadata do not match.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            additionCount))
        {
            return this.CreateError(
                "ASNR0011",
                "The extension-addition count exceeds the configured limit.",
                true);
        }

        bool[] additions = new bool[additionCount];
        int index = 0;
        while (index < componentPresence.Length)
        {
            int addition = additionIndexes[index];
            if (addition < 0 || addition >= additionCount)
            {
                return this.CreateError(
                    "ASNP0058",
                    "An extension component has an invalid addition index.",
                    false);
            }

            if (componentPresence[index])
            {
                additions[addition] = true;
            }

            index = index + 1;
        }

        return new PerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresence = additions,
            ExtensionPresent = this.AnyPresent(additions)
        };
    }

    internal PerCollectionResult WriteExtensionBitmap(
        PerBitWriter writer,
        bool[] extensionPresence,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (extensionPresence == null)
        {
            throw new System.ArgumentNullException("extensionPresence");
        }

        if (extensionPresence.Length < 1)
        {
            return this.CreateError(
                "ASNP0059",
                "An extension bitmap must contain at least one addition.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            extensionPresence.Length))
        {
            return this.CreateError(
                "ASNR0011",
                "The extension bitmap exceeds the configured element limit.",
                true);
        }

        int start = writer.GetWrittenBitCount();
        if (extensionPresence.Length <= 64)
        {
            PerLengthResult length = this.Lengths.WriteNormallySmallLength(
                writer,
                extensionPresence.Length,
                aligned);
            if (!length.Success)
            {
                return this.CopyLengthFailure(length);
            }

            PerCollectionResult bitmap = this.WriteBitmap(
                writer,
                extensionPresence);
            if (!bitmap.Success)
            {
                return bitmap;
            }
        }
        else
        {
            PerBitWriteResult large = writer.WriteBit(true);
            if (!large.Success)
            {
                return this.CopyWriterFailure(writer);
            }

            AsnBitStringValue bits = this.CreateBitString(extensionPresence);
            PerPrimitiveResult bitmap = this.Primitives.WriteBitString(
                writer,
                bits,
                0,
                false,
                0,
                aligned);
            if (!bitmap.Success)
            {
                return this.CopyPrimitiveFailure(bitmap);
            }
        }

        return new PerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresence = this.CopyBooleans(extensionPresence),
            ExtensionPresent = this.AnyPresent(extensionPresence),
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerCollectionResult ReadExtensionBitmap(
        PerBitReader reader,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        int start = reader.Position;
        PerBitReadResult form = reader.ReadBit();
        if (form.Status != AsnDecodeStatus.Done)
        {
            return this.HandleIncompleteRead(reader, form, start);
        }

        bool[] presence;
        if (form.Value == System.Numerics.BigInteger.Zero)
        {
            PerBitReadResult small = reader.ReadBits(6);
            if (small.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, small, start);
            }

            int count = (int)small.Value + 1;
            PerCollectionResult bitmap = this.ReadBitmap(
                reader,
                count,
                start);
            if (!bitmap.Success)
            {
                return bitmap;
            }

            presence = bitmap.ExtensionPresence;
        }
        else
        {
            PerPrimitiveResult bitmap = this.Primitives.ReadBitString(
                reader,
                0,
                false,
                0,
                aligned);
            if (!bitmap.Success || bitmap.Value == null
                || bitmap.Value.BitStringValue == null)
            {
                return this.CopyPrimitiveFailure(bitmap);
            }

            presence = this.ReadBitStringBooleans(
                bitmap.Value.BitStringValue);
            if (presence.Length <= 64)
            {
                return this.CreateError(
                    "ASNP0060",
                    "A small extension bitmap must use the normally-small form.",
                    false);
            }
        }

        return new PerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresence = presence,
            ExtensionPresent = this.AnyPresent(presence),
            BitCount = reader.Position - start
        };
    }

    internal PerCollectionResult WriteExtensionOpenTypes(
        PerBitWriter writer,
        bool[] extensionPresence,
        byte[][] encodings,
        int[] bitCounts,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (extensionPresence == null
            || encodings == null || bitCounts == null)
        {
            throw new System.ArgumentNullException("extensionPresence");
        }

        if (extensionPresence.Length != encodings.Length
            || extensionPresence.Length != bitCounts.Length)
        {
            return this.CreateError(
                "ASNP0061",
                "Extension presence and open-type payload counts do not match.",
                false);
        }

        int start = writer.GetWrittenBitCount();
        int index = 0;
        while (index < extensionPresence.Length)
        {
            if (extensionPresence[index])
            {
                PerOpenTypeResult written = this.OpenTypes.WriteOpenType(
                    writer,
                    encodings[index],
                    bitCounts[index],
                    aligned);
                if (!written.Success)
                {
                    return this.CopyOpenTypeFailure(written);
                }
            }

            index = index + 1;
        }

        return new PerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresence = this.CopyBooleans(extensionPresence),
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerCollectionResult ReadExtensionOpenTypes(
        PerBitReader reader,
        bool[] extensionPresence,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        if (extensionPresence == null)
        {
            throw new System.ArgumentNullException("extensionPresence");
        }

        int start = reader.Position;
        byte[][] data = new byte[extensionPresence.Length][];
        int[] bitOffsets = new int[extensionPresence.Length];
        int[] bitCounts = new int[extensionPresence.Length];
        int[] paddingBitCounts = new int[extensionPresence.Length];
        int index = 0;
        while (index < extensionPresence.Length)
        {
            data[index] = [];
            if (extensionPresence[index])
            {
                bitOffsets[index] = reader.Position;
                PerOpenTypeResult content = this.OpenTypes.ReadOpenType(
                    reader,
                    aligned);
                if (!content.Success)
                {
                    return this.CopyOpenTypeFailure(content);
                }

                data[index] = content.Data;
                bitCounts[index] = content.BitCount;
                paddingBitCounts[index] =
                    content.AlignmentPaddingBitCount;
            }

            index = index + 1;
        }

        return new PerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresence = this.CopyBooleans(extensionPresence),
            ExtensionData = data,
            ExtensionBitOffsets = bitOffsets,
            ExtensionBitCounts = bitCounts,
            ExtensionAlignmentPaddingBitCounts = paddingBitCounts,
            BitCount = reader.Position - start
        };
    }

    internal PerCollectionResult WriteExtensionSection(
        PerBitWriter writer,
        bool[] extensionPresence,
        byte[][] encodings,
        int[] bitCounts,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (extensionPresence == null)
        {
            throw new System.ArgumentNullException("extensionPresence");
        }

        if (!this.AnyPresent(extensionPresence))
        {
            return this.CreateError(
                "ASNP0062",
                "An extension section requires at least one present addition.",
                false);
        }

        int start = writer.GetWrittenBitCount();
        PerCollectionResult bitmap = this.WriteExtensionBitmap(
            writer,
            extensionPresence,
            aligned);
        if (!bitmap.Success)
        {
            return bitmap;
        }

        PerCollectionResult contents = this.WriteExtensionOpenTypes(
            writer,
            extensionPresence,
            encodings,
            bitCounts,
            aligned);
        if (!contents.Success)
        {
            return contents;
        }

        contents.ExtensionPresent = this.AnyPresent(extensionPresence);
        contents.BitCount = writer.GetWrittenBitCount() - start;
        return contents;
    }

    internal PerCollectionResult ReadExtensionSection(
        PerBitReader reader,
        bool aligned)
    {
        int start = reader.Position;
        PerCollectionResult bitmap = this.ReadExtensionBitmap(
            reader,
            aligned);
        if (!bitmap.Success)
        {
            return bitmap;
        }

        if (!bitmap.ExtensionPresent)
        {
            return this.CreateError(
                "ASNP0062",
                "An extension section requires at least one present addition.",
                false);
        }

        PerCollectionResult contents = this.ReadExtensionOpenTypes(
            reader,
            bitmap.ExtensionPresence,
            aligned);
        if (!contents.Success)
        {
            return contents;
        }

        contents.ExtensionPresent = bitmap.ExtensionPresent;
        contents.BitCount = reader.Position - start;
        return contents;
    }

    internal PerCollectionResult WriteBitmap(
        PerBitWriter writer,
        bool[] presence)
    {
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            presence.Length))
        {
            return this.CreateError(
                "ASNR0011",
                "The presence bitmap exceeds the configured element limit.",
                true);
        }

        int index = 0;
        while (index < presence.Length)
        {
            PerBitWriteResult bit = writer.WriteBit(presence[index]);
            if (!bit.Success)
            {
                return this.CopyWriterFailure(writer);
            }

            index = index + 1;
        }

        return new PerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresence = this.CopyBooleans(presence),
            BitCount = presence.Length
        };
    }

    internal PerCollectionResult ReadBitmap(
        PerBitReader reader,
        int count,
        int start)
    {
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(count))
        {
            return this.CreateError(
                "ASNR0011",
                "The presence bitmap exceeds the configured element limit.",
                true);
        }

        if (!new ResourceCounter(this.Limits).TryChargeAllocationBytes(count))
        {
            return this.CreateError(
                "ASNR0005",
                "Allocating the presence bitmap exceeds the configured limit.",
                true);
        }

        bool[] presence = new bool[count];
        int index = 0;
        while (index < count)
        {
            PerBitReadResult bit = reader.ReadBit();
            if (bit.Status != AsnDecodeStatus.Done)
            {
                return this.HandleIncompleteRead(reader, bit, start);
            }

            presence[index] = bit.Value
                != System.Numerics.BigInteger.Zero;
            index = index + 1;
        }

        return new PerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ExtensionPresence = presence,
            BitCount = count
        };
    }

    internal AsnBitStringValue CreateBitString(bool[] presence)
    {
        int byteCount = presence.Length / 8;
        if (presence.Length % 8 != 0)
        {
            byteCount = byteCount + 1;
        }

        byte[] data = new byte[byteCount];
        int index = 0;
        while (index < presence.Length)
        {
            if (presence[index])
            {
                data[index / 8] = (byte)(data[index / 8]
                    | 1 << (7 - index % 8));
            }

            index = index + 1;
        }

        return new AsnBitStringValue
        {
            Bytes = data,
            BitLength = presence.Length
        };
    }

    internal bool[] ReadBitStringBooleans(AsnBitStringValue bits)
    {
        bool[] presence = new bool[bits.BitLength];
        int index = 0;
        while (index < presence.Length)
        {
            presence[index] = ((bits.Bytes[index / 8]
                >> (7 - index % 8)) & 1) != 0;
            index = index + 1;
        }

        return presence;
    }

    internal bool MatchesDefault(
        AsnFieldDescriptor descriptor,
        AsnRuntimeValue value,
        AsnReflectionCatalog catalog)
    {
        if (!descriptor.HasDefaultValue
            || descriptor.Type == null
            || descriptor.DefaultValue == null
            || descriptor.DefaultValue.Value == null)
        {
            return false;
        }

        ValueAccess access = new ValueAccess();
        AsnRuntimeValue actual = access.NormalizeValue(
            value,
            descriptor.Type,
            catalog);
        AsnRuntimeValue expected = access.FromSchemaValue(
            descriptor.DefaultValue.Value,
            descriptor.Type,
            catalog);
        return this.RuntimeValuesEqual(actual, expected);
    }

    internal bool RuntimeValuesEqual(
        AsnRuntimeValue left,
        AsnRuntimeValue right)
    {
        if (left.Kind != right.Kind
            || left.HasBooleanValue != right.HasBooleanValue
            || left.BooleanValue != right.BooleanValue
            || left.HasIntegerValue != right.HasIntegerValue
            || left.IntegerValue != right.IntegerValue
            || left.TextValue != right.TextValue)
        {
            return false;
        }

        RuntimeConstraintEvaluator comparison = new RuntimeConstraintEvaluator();
        if (!this.BitStringsEqual(
                left.BitStringValue,
                right.BitStringValue,
                comparison)
            || !this.ObjectIdentifiersEqual(
                left.ObjectIdentifierValue,
                right.ObjectIdentifierValue,
                comparison)
            || !comparison.ByteArraysEqual(left.Octets, right.Octets)
            || !this.RealValuesEqual(
                left.RealValue,
                right.RealValue,
                comparison)
            || !this.TimeValuesEqual(left.TimeValue, right.TimeValue)
            || left.Fields.Length != right.Fields.Length
            || left.Items.Length != right.Items.Length)
        {
            return false;
        }

        int fieldIndex = 0;
        while (fieldIndex < left.Fields.Length)
        {
            AsnRuntimeFieldValue leftField = left.Fields[fieldIndex];
            AsnRuntimeFieldValue rightField = right.Fields[fieldIndex];
            if (leftField == null || rightField == null
                || leftField.Name != rightField.Name
                || leftField.Present != rightField.Present
                || leftField.PresentDefault != rightField.PresentDefault
                || leftField.Value == null && rightField.Value != null
                || leftField.Value != null && rightField.Value == null
                || leftField.Value != null && rightField.Value != null
                    && !this.RuntimeValuesEqual(
                        leftField.Value,
                        rightField.Value))
            {
                return false;
            }

            fieldIndex = fieldIndex + 1;
        }

        int itemIndex = 0;
        while (itemIndex < left.Items.Length)
        {
            if (left.Items[itemIndex] == null
                || right.Items[itemIndex] == null
                || !this.RuntimeValuesEqual(
                    left.Items[itemIndex],
                    right.Items[itemIndex]))
            {
                return false;
            }

            itemIndex = itemIndex + 1;
        }

        return this.OpenTypesEqual(
            left.OpenTypeValue,
            right.OpenTypeValue,
            comparison);
    }

    internal bool BitStringsEqual(
        AsnBitStringValue? left,
        AsnBitStringValue? right,
        RuntimeConstraintEvaluator comparison)
    {
        return left == null || right == null
            ? left == null && right == null
            : comparison.BitStringsEqual(left, right);
    }

    internal bool ObjectIdentifiersEqual(
        AsnObjectIdentifierValue? left,
        AsnObjectIdentifierValue? right,
        RuntimeConstraintEvaluator comparison)
    {
        return left == null || right == null
            ? left == null && right == null
            : comparison.ObjectIdentifiersEqual(left, right);
    }

    internal bool RealValuesEqual(
        AsnRealValue? left,
        AsnRealValue? right,
        RuntimeConstraintEvaluator comparison)
    {
        return left == null || right == null
            ? left == null && right == null
            : comparison.CompareRealValues(left, right) == 0;
    }

    internal bool TimeValuesEqual(
        AsnTimeValue? left,
        AsnTimeValue? right)
    {
        return left == null || right == null
            ? left == null && right == null
            : left.Kind == right.Kind && left.Text == right.Text;
    }

    internal bool OpenTypesEqual(
        AsnOpenTypeValue? left,
        AsnOpenTypeValue? right,
        RuntimeConstraintEvaluator comparison)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }

        if (left.Resolved != right.Resolved
            || left.Unresolved != right.Unresolved
            || left.GetResolvedTypeIdentity() != right.GetResolvedTypeIdentity()
            || left.HasOriginalData != right.HasOriginalData
            || !comparison.ByteArraysEqual(
                left.OriginalData,
                right.OriginalData))
        {
            return false;
        }

        return left.Value == null || right.Value == null
            ? left.Value == null && right.Value == null
            : this.RuntimeValuesEqual(left.Value, right.Value);
    }

    internal AsnRuntimeFieldValue? FindRuntimeField(
        AsnRuntimeFieldValue[] fields,
        string name)
    {
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index] != null && fields[index].Name == name)
            {
                return fields[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool AnyPresent(bool[] presence)
    {
        int index = 0;
        while (index < presence.Length)
        {
            if (presence[index])
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool[] CopyBooleans(bool[] source)
    {
        bool[] result = new bool[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    internal PerCollectionResult HandleIncompleteRead(
        PerBitReader reader,
        PerBitReadResult input,
        int start)
    {
        if (input.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
            return new PerCollectionResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        return this.CopyReaderFailure(reader);
    }

    internal PerCollectionResult CopyLengthFailure(PerLengthResult source)
    {
        return new PerCollectionResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerCollectionResult CopyOpenTypeFailure(
        PerOpenTypeResult source)
    {
        return new PerCollectionResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerCollectionResult CopyPrimitiveFailure(
        PerPrimitiveResult source)
    {
        return new PerCollectionResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerCollectionResult CopyReaderFailure(PerBitReader reader)
    {
        return new PerCollectionResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = reader.LimitExceeded,
            ErrorCode = reader.ErrorCode,
            ErrorMessage = reader.ErrorMessage
        };
    }

    internal PerCollectionResult CopyWriterFailure(PerBitWriter writer)
    {
        return new PerCollectionResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = writer.LimitExceeded,
            ErrorCode = writer.ErrorCode,
            ErrorMessage = writer.ErrorMessage
        };
    }

    internal PerCollectionResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerCollectionResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
