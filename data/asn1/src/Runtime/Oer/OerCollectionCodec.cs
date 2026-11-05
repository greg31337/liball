namespace asn1;

internal class OerCollectionCodec
{
    internal AsnResourceLimits Limits;

    internal OerCollectionCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerCollectionResult EncodeCollection(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        bool canonical)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        return this.EncodeCollectionAtDepth(
            type,
            value,
            canonical,
            0,
            AsnUnknownDataPolicy.Preserve);
    }

    internal OerCollectionResult DecodeCollection(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!CursorMath.IsRangeValid(offset, count, data.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        return this.DecodeCollectionAtDepth(
            type,
            data,
            offset,
            count,
            canonical,
            0,
            AsnUnknownDataPolicy.Preserve);
    }

    internal OerCollectionResult EncodeCollectionAtDepth(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        if (!this.AllowsDepth(depth))
        {
            return this.CreateCollectionError(
                "ASNR0001",
                "The OER collection exceeds the configured nesting depth.",
                true);
        }

        if (type.Kind != AsnTypeKind.Sequence
            && type.Kind != AsnTypeKind.Set)
        {
            return this.CreateCollectionError(
                "ASNO0055",
                "The requested type is not an OER sequence or set.",
                false);
        }

        AsnRuntimeValueKind expected = type.Kind == AsnTypeKind.Set
            ? AsnRuntimeValueKind.Set
            : AsnRuntimeValueKind.Sequence;
        if (value.Kind != expected)
        {
            return this.CreateCollectionError(
                "ASNO0056",
                "The runtime value does not match the OER collection type.",
                false);
        }

        AsnFieldDescriptor[] roots = this.GetOrderedRootFields(type);
        OerCollectionResult rootValidation = this.ValidateComponentValues(
            roots,
            value.Fields);
        if (!rootValidation.Success)
        {
            return rootValidation;
        }

        OerCollectionResult extensionValidation =
            this.ValidateComponentValues(
                type.ExtensionFields,
                value.Fields);
        if (!extensionValidation.Success)
        {
            return extensionValidation;
        }

        bool[] rootPresence = this.BuildRootPresence(
            roots,
            value.Fields,
            canonical);
        int[] additionIndexes = this.GetAdditionIndexes(
            type.ExtensionFields);
        int knownAdditionCount = this.GetAdditionCount(additionIndexes);
        AsnEncodingRules rules = canonical
            ? AsnEncodingRules.Coer
            : AsnEncodingRules.Oer;
        OerCollectionResult unknownValidation =
            this.ValidateUnknownExtensions(
                value.UnknownExtensions,
                knownAdditionCount,
                type.Extensible,
                rules,
                policy);
        if (!unknownValidation.Success)
        {
            return unknownValidation;
        }

        AsnUnknownExtension[] unknown = policy
            == AsnUnknownDataPolicy.Preserve
            ? value.UnknownExtensions
            : [];
        int additionCount = this.GetEffectiveAdditionCount(
            knownAdditionCount,
            unknown);
        bool[] extensionPresence = new bool[additionCount];
        int index = 0;
        while (index < type.ExtensionFields.Length)
        {
            if (this.ShouldEncodeComponent(
                type.ExtensionFields[index],
                value.Fields,
                canonical))
            {
                extensionPresence[additionIndexes[index]] = true;
            }

            index = index + 1;
        }

        index = 0;
        while (index < unknown.Length)
        {
            extensionPresence[unknown[index].Index] = unknown[index].Present;
            index = index + 1;
        }

        extensionPresence = this.TrimExtensionPresence(
            extensionPresence,
            canonical);

        bool extensionPresent = this.HasPresentBit(extensionPresence);
        if (!type.Extensible && extensionPresent)
        {
            return this.CreateCollectionError(
                "ASNO0050",
                "A non-extensible OER collection cannot contain extension additions.",
                false);
        }

        OerPreambleResult preamble = new OerPreambleCodec(this.Limits)
            .EncodeRootPreamble(
                type.Extensible,
                extensionPresent,
                rootPresence);
        if (!preamble.Success)
        {
            return this.CopyPreambleFailure(preamble);
        }

        System.Collections.Generic.List<byte> output =
            new System.Collections.Generic.List<byte>();
        this.AppendData(output, preamble.Data);
        OerCollectionResult rootResult = this.EncodeRootComponents(
            roots,
            value.Fields,
            rootPresence,
            canonical,
            depth + 1,
            policy);
        if (!rootResult.Success)
        {
            return rootResult;
        }

        this.AppendData(output, rootResult.Data);
        if (extensionPresent)
        {
            OerPreambleResult bitmap = new OerPreambleCodec(this.Limits)
                .EncodeExtensionBitmap(extensionPresence);
            if (!bitmap.Success)
            {
                return this.CopyPreambleFailure(bitmap);
            }

            this.AppendData(output, bitmap.Data);
            index = 0;
            while (index < extensionPresence.Length)
            {
                if (extensionPresence[index])
                {
                    byte[] payloadData;
                    if (index < knownAdditionCount)
                    {
                        OerCollectionResult payload =
                            this.EncodeKnownAddition(
                                type.ExtensionFields,
                                additionIndexes,
                                index,
                                value.Fields,
                                canonical,
                                depth + 1,
                                policy);
                        if (!payload.Success)
                        {
                            return payload;
                        }

                        payloadData = payload.Data;
                    }
                    else
                    {
                        AsnUnknownExtension? preserved =
                            this.FindUnknownExtension(unknown, index);
                        if (preserved == null || !preserved.HasBinaryData)
                        {
                            return this.CreateCollectionError(
                                "ASNO0064",
                                "A present unknown OER extension has no preserved contents.",
                                false);
                        }

                        payloadData = preserved.BinaryData;
                    }

                    OerContentResult wrapped = new OerOpenTypeCodec(
                        this.Limits).EncodeOpenType(payloadData);
                    if (!wrapped.Success)
                    {
                        return this.CopyContentFailure(wrapped);
                    }

                    this.AppendData(output, wrapped.Data);
                }

                index = index + 1;
            }
        }

        if (!new ResourceCounter(this.Limits).TryChargeAllocationBytes(
            output.Count))
        {
            return this.CreateCollectionError(
                "ASNR0005",
                "Allocating the OER collection exceeds the configured limit.",
                true);
        }

        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Data = output.ToArray(),
            ConsumedCount = output.Count
        };
    }

    internal OerCollectionResult DecodeCollectionAtDepth(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        if (!this.AllowsDepth(depth))
        {
            return this.CreateCollectionError(
                "ASNR0001",
                "The OER collection exceeds the configured nesting depth.",
                true);
        }

        if (type.Kind != AsnTypeKind.Sequence
            && type.Kind != AsnTypeKind.Set)
        {
            return this.CreateCollectionError(
                "ASNO0055",
                "The requested type is not an OER sequence or set.",
                false);
        }

        AsnFieldDescriptor[] roots = this.GetOrderedRootFields(type);
        int optionalCount = this.CountOptionalFields(roots);
        OerPreambleResult preamble = new OerPreambleCodec(this.Limits)
            .DecodeRootPreamble(
                data,
                offset,
                count,
                type.Extensible,
                optionalCount);
        if (!preamble.Success)
        {
            return this.CopyPreambleFailure(preamble);
        }

        int consumed = preamble.ConsumedCount;
        System.Collections.Generic.List<AsnRuntimeFieldValue> fields =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        System.Collections.Generic.List<AsnUnknownExtension> unknown =
            new System.Collections.Generic.List<AsnUnknownExtension>();
        OerCollectionResult rootsDecoded = this.DecodeRootComponents(
            roots,
            preamble.Presence,
            fields,
            data,
            offset + consumed,
            count - consumed,
            canonical,
            depth + 1,
            policy);
        if (!rootsDecoded.Success)
        {
            return this.ShiftCollectionFailure(
                rootsDecoded,
                preamble.ConsumedCount);
        }

        consumed = consumed + rootsDecoded.ConsumedCount;
        if (preamble.ExtensionPresent)
        {
            OerPreambleResult bitmap = new OerPreambleCodec(this.Limits)
                .DecodeExtensionBitmap(
                    data,
                    offset + consumed,
                    count - consumed,
                    canonical);
            if (!bitmap.Success)
            {
                return this.ShiftCollectionFailure(
                    this.CopyPreambleFailure(bitmap),
                    consumed);
            }

            consumed = consumed + bitmap.ConsumedCount;
            int[] additionIndexes = this.GetAdditionIndexes(
                type.ExtensionFields);
            int knownAdditionCount = this.GetAdditionCount(additionIndexes);
            int index = 0;
            while (index < bitmap.Presence.Length)
            {
                if (bitmap.Presence[index])
                {
                    int openOffset = offset + consumed;
                    int openRelativeOffset = consumed;
                    OerContentResult open = new OerOpenTypeCodec(this.Limits)
                        .DecodeOpenType(
                            data,
                            offset + consumed,
                            count - consumed,
                            canonical);
                    if (!open.Success)
                    {
                        return this.ShiftCollectionFailure(
                            this.CopyContentFailure(open),
                            openRelativeOffset);
                    }

                    consumed = consumed + open.ConsumedCount;
                    int openContentOffset = consumed - open.Content.Length;
                    if (index < knownAdditionCount)
                    {
                        OerCollectionResult addition =
                            this.DecodeKnownAddition(
                                type.ExtensionFields,
                                additionIndexes,
                                index,
                                open.Content,
                                fields,
                                canonical,
                                depth + 1,
                                policy);
                        if (!addition.Success)
                        {
                            return this.ShiftCollectionFailure(
                                addition,
                                openContentOffset);
                        }

                        if (addition.ConsumedCount != open.Content.Length)
                        {
                            return this.CreateCollectionErrorAt(
                                "ASNO0057",
                                "An OER extension open type contains trailing data.",
                                false,
                                openContentOffset
                                    + addition.ConsumedCount);
                        }
                    }
                    else if (policy == AsnUnknownDataPolicy.Reject)
                    {
                        return this.CreateCollectionErrorAt(
                            "ASNO0066",
                            "An unknown OER extension is not permitted by policy.",
                            false,
                            openContentOffset);
                    }
                    else if (policy == AsnUnknownDataPolicy.Preserve)
                    {
                        unknown.Add(new AsnUnknownExtension
                        {
                            Index = index,
                            Present = true,
                            HasEncodedIndex = true,
                            EncodedIndex = index,
                            HasEncodedLocation = true,
                            EncodedOffset = openOffset + open.ConsumedCount
                                - open.Content.Length,
                            EncodedLength = open.Content.Length,
                            EncodingRules = canonical
                                ? AsnEncodingRules.Coer
                                : AsnEncodingRules.Oer,
                            HasEncodingRules = true,
                            HasBinaryData = true,
                            BinaryData = open.Content
                        });
                    }
                }
                else if (index >= knownAdditionCount
                    && policy == AsnUnknownDataPolicy.Preserve)
                {
                    unknown.Add(new AsnUnknownExtension
                    {
                        Index = index,
                        Present = false,
                        HasEncodedIndex = true,
                        EncodedIndex = index,
                        EncodingRules = canonical
                            ? AsnEncodingRules.Coer
                            : AsnEncodingRules.Oer,
                        HasEncodingRules = true
                    });
                }

                index = index + 1;
            }
        }

        byte[] encoded = new byte[consumed];
        System.Buffer.BlockCopy(data, offset, encoded, 0, consumed);
        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = type.Kind == AsnTypeKind.Set
                    ? AsnRuntimeValueKind.Set
                    : AsnRuntimeValueKind.Sequence,
                Fields = fields.ToArray(),
                UnknownExtensions = unknown.ToArray()
            },
            Data = encoded,
            ConsumedCount = consumed
        };
    }

    internal OerCollectionResult EncodeRootComponents(
        AsnFieldDescriptor[] descriptors,
        AsnRuntimeFieldValue[] fields,
        bool[] presence,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        System.Collections.Generic.List<byte> output =
            new System.Collections.Generic.List<byte>();
        int presenceIndex = 0;
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            bool present = true;
            if (descriptor.Optional || descriptor.HasDefaultValue)
            {
                present = presence[presenceIndex];
                presenceIndex = presenceIndex + 1;
            }

            if (present)
            {
                AsnRuntimeFieldValue? field = this.FindRuntimeField(
                    fields,
                    descriptor.Name);
                if (field == null || !field.Present || field.Value == null)
                {
                    return this.CreateMissingFieldError(descriptor.Name);
                }

                if (descriptor.Type == null)
                {
                    return this.CreateMissingTypeError(descriptor.Name);
                }

                OerCollectionResult encoded = this.EncodeFieldValue(
                    descriptor.Type,
                    field.Value,
                    canonical,
                    depth,
                    policy);
                if (!encoded.Success)
                {
                    return encoded;
                }

                this.AppendData(output, encoded.Data);
            }

            index = index + 1;
        }

        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = output.ToArray(),
            ConsumedCount = output.Count
        };
    }

    internal OerCollectionResult DecodeRootComponents(
        AsnFieldDescriptor[] descriptors,
        bool[] presence,
        System.Collections.Generic.List<AsnRuntimeFieldValue> fields,
        byte[] data,
        int offset,
        int count,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        int consumed = 0;
        int presenceIndex = 0;
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            bool present = true;
            if (descriptor.Optional || descriptor.HasDefaultValue)
            {
                present = presence[presenceIndex];
                presenceIndex = presenceIndex + 1;
            }

            AsnRuntimeFieldValue field = new AsnRuntimeFieldValue
            {
                Name = descriptor.Name,
                Present = present
            };
            if (present)
            {
                int fieldOffset = consumed;
                if (descriptor.Type == null)
                {
                    return this.CreateMissingTypeError(descriptor.Name);
                }

                OerCollectionResult decoded = this.DecodeFieldValue(
                    descriptor.Type,
                    data,
                    offset + consumed,
                    count - consumed,
                    canonical,
                    depth,
                    policy);
                if (!decoded.Success)
                {
                    return this.ShiftCollectionFailure(
                        decoded,
                        fieldOffset);
                }

                field.Value = decoded.Value;
                consumed = consumed + decoded.ConsumedCount;
                if (canonical
                    && descriptor.HasDefaultValue
                    && decoded.Value != null
                    && this.MatchesDefault(descriptor, decoded.Value))
                {
                    return this.CreateCollectionErrorAt(
                        "ASNO0063",
                        "COER must omit a component equal to its DEFAULT value: "
                            + descriptor.Name,
                        false,
                        fieldOffset);
                }
            }

            fields.Add(field);
            index = index + 1;
        }

        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            ConsumedCount = consumed
        };
    }

    internal OerCollectionResult EncodeKnownAddition(
        AsnFieldDescriptor[] descriptors,
        int[] additionIndexes,
        int additionIndex,
        AsnRuntimeFieldValue[] fields,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        AsnFieldDescriptor[] members = this.GetAdditionMembers(
            descriptors,
            additionIndexes,
            additionIndex);
        bool grouped = members.Length > 0
            && members[0].ExtensionGroupIndex >= 0;
        if (members.Length == 1 && !grouped)
        {
            AsnTypeDescriptor? memberType = members[0].Type;
            AsnRuntimeFieldValue? field = this.FindRuntimeField(
                fields,
                members[0].Name);
            if (memberType == null)
            {
                return this.CreateMissingTypeError(members[0].Name);
            }

            if (field == null || !field.Present || field.Value == null)
            {
                return this.CreateMissingFieldError(members[0].Name);
            }

            return this.EncodeFieldValue(
                memberType,
                field.Value,
                canonical,
                depth,
                policy);
        }

        bool[] presence = this.BuildRootPresence(
            members,
            fields,
            canonical);
        OerPreambleResult preamble = new OerPreambleCodec(this.Limits)
            .EncodeRootPreamble(false, false, presence);
        if (!preamble.Success)
        {
            return this.CopyPreambleFailure(preamble);
        }

        OerCollectionResult contents = this.EncodeRootComponents(
            members,
            fields,
            presence,
            canonical,
            depth,
            policy);
        if (!contents.Success)
        {
            return contents;
        }

        byte[] data = new byte[preamble.Data.Length + contents.Data.Length];
        System.Buffer.BlockCopy(
            preamble.Data,
            0,
            data,
            0,
            preamble.Data.Length);
        System.Buffer.BlockCopy(
            contents.Data,
            0,
            data,
            preamble.Data.Length,
            contents.Data.Length);
        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = data,
            ConsumedCount = data.Length
        };
    }

    internal OerCollectionResult DecodeKnownAddition(
        AsnFieldDescriptor[] descriptors,
        int[] additionIndexes,
        int additionIndex,
        byte[] data,
        System.Collections.Generic.List<AsnRuntimeFieldValue> fields,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        AsnFieldDescriptor[] members = this.GetAdditionMembers(
            descriptors,
            additionIndexes,
            additionIndex);
        bool grouped = members.Length > 0
            && members[0].ExtensionGroupIndex >= 0;
        if (members.Length == 1 && !grouped)
        {
            AsnTypeDescriptor? memberType = members[0].Type;
            if (memberType == null)
            {
                return this.CreateMissingTypeError(members[0].Name);
            }

            OerCollectionResult decoded = this.DecodeFieldValue(
                memberType,
                data,
                0,
                data.Length,
                canonical,
                depth,
                policy);
            if (!decoded.Success)
            {
                return decoded;
            }

            if (canonical
                && members[0].HasDefaultValue
                && decoded.Value != null
                && this.MatchesDefault(members[0], decoded.Value))
            {
                return this.CreateCollectionError(
                    "ASNO0063",
                    "COER must omit an extension addition equal to its DEFAULT value: "
                        + members[0].Name,
                    false);
            }

            fields.Add(new AsnRuntimeFieldValue
            {
                Name = members[0].Name,
                Present = true,
                Value = decoded.Value
            });
            return decoded;
        }

        int optionalCount = this.CountOptionalFields(members);
        OerPreambleResult preamble = new OerPreambleCodec(this.Limits)
            .DecodeRootPreamble(
                data,
                0,
                data.Length,
                false,
                optionalCount);
        if (!preamble.Success)
        {
            return this.CopyPreambleFailure(preamble);
        }

        OerCollectionResult decodedMembers = this.DecodeRootComponents(
            members,
            preamble.Presence,
            fields,
            data,
            preamble.ConsumedCount,
            data.Length - preamble.ConsumedCount,
            canonical,
            depth,
            policy);
        if (!decodedMembers.Success)
        {
            return this.ShiftCollectionFailure(
                decodedMembers,
                preamble.ConsumedCount);
        }

        decodedMembers.ConsumedCount = decodedMembers.ConsumedCount
            + preamble.ConsumedCount;
        return decodedMembers;
    }

    internal OerCollectionResult EncodeFieldValue(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        return new OerValueCodec(this.Limits).EncodeValueAtDepth(
            type,
            value,
            canonical,
            depth,
            policy);
    }

    internal OerCollectionResult DecodeFieldValue(
        AsnTypeDescriptor type,
        byte[] data,
        int offset,
        int count,
        bool canonical,
        int depth,
        AsnUnknownDataPolicy policy)
    {
        return new OerValueCodec(this.Limits).DecodeValueAtDepth(
            type,
            data,
            offset,
            count,
            canonical,
            depth,
            policy);
    }

    internal bool[] BuildRootPresence(
        AsnFieldDescriptor[] descriptors,
        AsnRuntimeFieldValue[] fields,
        bool canonical)
    {
        bool[] result = new bool[this.CountOptionalFields(descriptors)];
        int presenceIndex = 0;
        int index = 0;
        while (index < descriptors.Length)
        {
            if (descriptors[index].Optional
                || descriptors[index].HasDefaultValue)
            {
                AsnRuntimeFieldValue? field = this.FindRuntimeField(
                    fields,
                    descriptors[index].Name);
                AsnRuntimeValue? fieldValue = field == null
                    ? null
                    : field.Value;
                bool present = field != null
                    && field.Present
                    && fieldValue != null;
                if (present
                    && canonical
                    && descriptors[index].HasDefaultValue
                    && fieldValue != null
                    && this.MatchesDefault(
                        descriptors[index],
                        fieldValue))
                {
                    present = false;
                }

                result[presenceIndex] = present;
                presenceIndex = presenceIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool MatchesDefault(
        AsnFieldDescriptor descriptor,
        AsnRuntimeValue value)
    {
        return new PerCollectionCodec(this.Limits).MatchesDefault(
            descriptor,
            value,
            new AsnReflectionCatalog());
    }

    internal bool ShouldEncodeComponent(
        AsnFieldDescriptor descriptor,
        AsnRuntimeFieldValue[] fields,
        bool canonical)
    {
        AsnRuntimeFieldValue? field = this.FindRuntimeField(
            fields,
            descriptor.Name);
        if (field == null || !field.Present || field.Value == null)
        {
            return false;
        }

        return !canonical
            || !descriptor.HasDefaultValue
            || !this.MatchesDefault(descriptor, field.Value);
    }

    internal bool[] TrimExtensionPresence(
        bool[] presence,
        bool canonical)
    {
        int count = presence.Length;
        if (canonical)
        {
            while (count > 0 && !presence[count - 1])
            {
                count = count - 1;
            }
        }

        bool[] result = new bool[count];
        int index = 0;
        while (index < count)
        {
            result[index] = presence[index];
            index = index + 1;
        }

        return result;
    }

    internal OerCollectionResult ValidateUnknownExtensions(
        AsnUnknownExtension[] extensions,
        int knownAdditionCount,
        bool extensible,
        AsnEncodingRules rules,
        AsnUnknownDataPolicy policy)
    {
        if (extensions.Length == 0
            || policy == AsnUnknownDataPolicy.Skip)
        {
            return new OerCollectionResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done
            };
        }

        if (policy == AsnUnknownDataPolicy.Reject)
        {
            return this.CreateCollectionError(
                "ASNO0066",
                "Unknown OER extensions are not permitted by policy.",
                false);
        }

        int index = 0;
        while (index < extensions.Length)
        {
            AsnUnknownExtension extension = extensions[index];
            if (extension == null
                || !extension.IsValid()
                || !extensible
                || extension.Index < knownAdditionCount
                || extension.Present && !extension.HasBinaryData
                || !extension.Present && extension.HasBinaryData)
            {
                return this.CreateCollectionError(
                    "ASNO0064",
                    "A preserved unknown OER extension is invalid or conflicts with the schema.",
                    false);
            }

            if (!new OpaquePayloadPolicy().CanReuseUnknownExtension(
                extension,
                rules))
            {
                return this.CreateCollectionError(
                    "ASNO0065",
                    "A preserved unknown OER extension uses incompatible encoding rules.",
                    false);
            }

            int previous = 0;
            while (previous < index)
            {
                if (extensions[previous].Index == extension.Index)
                {
                    return this.CreateCollectionError(
                        "ASNO0064",
                        "Preserved unknown OER extension indexes must be unique.",
                        false);
                }

                previous = previous + 1;
            }

            index = index + 1;
        }

        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal int GetEffectiveAdditionCount(
        int knownAdditionCount,
        AsnUnknownExtension[] extensions)
    {
        int count = knownAdditionCount;
        int index = 0;
        while (index < extensions.Length)
        {
            if (extensions[index].Index >= count)
            {
                count = extensions[index].Index + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal AsnUnknownExtension? FindUnknownExtension(
        AsnUnknownExtension[] extensions,
        int additionIndex)
    {
        int index = 0;
        while (index < extensions.Length)
        {
            if (extensions[index].Index == additionIndex)
            {
                return extensions[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal OerCollectionResult ValidateComponentValues(
        AsnFieldDescriptor[] descriptors,
        AsnRuntimeFieldValue[] fields)
    {
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnRuntimeFieldValue? field = this.FindRuntimeField(
                fields,
                descriptors[index].Name);
            if (field != null && field.Present && field.Value == null)
            {
                return this.CreateCollectionError(
                    "ASNO0062",
                    "A present OER component requires a runtime value: "
                        + descriptors[index].Name,
                    false);
            }

            index = index + 1;
        }

        return new OerCollectionResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal AsnFieldDescriptor[] GetOrderedRootFields(
        AsnTypeDescriptor type)
    {
        AsnFieldDescriptor[] result = new AsnFieldDescriptor[
            type.RootFields.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = type.RootFields[index];
            index = index + 1;
        }

        if (type.Kind != AsnTypeKind.Set)
        {
            return result;
        }

        index = 1;
        while (index < result.Length)
        {
            AsnFieldDescriptor field = result[index];
            int insertion = index;
            while (insertion > 0
                && this.CompareFieldTags(
                    field,
                    result[insertion - 1]) < 0)
            {
                result[insertion] = result[insertion - 1];
                insertion = insertion - 1;
            }

            result[insertion] = field;
            index = index + 1;
        }

        return result;
    }

    internal int CompareFieldTags(
        AsnFieldDescriptor left,
        AsnFieldDescriptor right)
    {
        AsnTagDescriptor? leftTag = this.GetFieldTag(left);
        AsnTagDescriptor? rightTag = this.GetFieldTag(right);
        if (leftTag == null || rightTag == null)
        {
            return left.Index - right.Index;
        }

        int classComparison = (int)leftTag.Class - (int)rightTag.Class;
        return classComparison != 0
            ? classComparison
            : leftTag.Number.CompareTo(rightTag.Number);
    }

    internal AsnTagDescriptor? GetFieldTag(AsnFieldDescriptor field)
    {
        if (field.Tags.Length > 0)
        {
            return field.Tags[0];
        }

        return field.Type != null && field.Type.Tags.Length > 0
            ? field.Type.Tags[0]
            : null;
    }

    internal int CountOptionalFields(AsnFieldDescriptor[] fields)
    {
        int count = 0;
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Optional || fields[index].HasDefaultValue)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int[] GetAdditionIndexes(AsnFieldDescriptor[] fields)
    {
        int[] result = new int[fields.Length];
        int next = 0;
        int previousGroup = -1;
        int previousAddition = -1;
        int index = 0;
        while (index < fields.Length)
        {
            int group = fields[index].ExtensionGroupIndex;
            if (group >= 0 && group == previousGroup)
            {
                result[index] = previousAddition;
            }
            else
            {
                result[index] = next;
                previousAddition = next;
                next = next + 1;
            }

            previousGroup = group;
            index = index + 1;
        }

        return result;
    }

    internal int GetAdditionCount(int[] indexes)
    {
        return indexes.Length == 0 ? 0 : indexes[indexes.Length - 1] + 1;
    }

    internal AsnFieldDescriptor[] GetAdditionMembers(
        AsnFieldDescriptor[] fields,
        int[] indexes,
        int additionIndex)
    {
        int count = 0;
        int index = 0;
        while (index < indexes.Length)
        {
            if (indexes[index] == additionIndex)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        AsnFieldDescriptor[] result = new AsnFieldDescriptor[count];
        int resultIndex = 0;
        index = 0;
        while (index < indexes.Length)
        {
            if (indexes[index] == additionIndex)
            {
                result[resultIndex] = fields[index];
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        return result;
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

    internal bool IsRuntimeFieldPresent(
        AsnRuntimeFieldValue[] fields,
        string name)
    {
        AsnRuntimeFieldValue? field = this.FindRuntimeField(fields, name);
        return field != null && field.Present && field.Value != null;
    }

    internal bool HasPresentBit(bool[] presence)
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

    internal bool AllowsDepth(int depth)
    {
        return this.Limits.Unlimited
            || depth <= this.Limits.MaximumNestingDepth;
    }

    internal void AppendData(
        System.Collections.Generic.List<byte> target,
        byte[] data)
    {
        int index = 0;
        while (index < data.Length)
        {
            target.Add(data[index]);
            index = index + 1;
        }
    }

    internal OerCollectionResult CopyPrimitiveResult(
        OerPrimitiveResult source)
    {
        return new OerCollectionResult
        {
            Success = source.Success,
            Status = source.Status,
            Value = source.Value,
            Data = source.Data,
            ConsumedCount = source.ConsumedCount,
            HasViolationOffset = source.HasViolationOffset,
            ViolationOffset = source.ViolationOffset,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal OerCollectionResult CopyPreambleFailure(
        OerPreambleResult source)
    {
        return new OerCollectionResult
        {
            Status = source.Status,
            ConsumedCount = source.ConsumedCount,
            HasViolationOffset = source.HasViolationOffset,
            ViolationOffset = source.ViolationOffset,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal OerCollectionResult CopyContentFailure(OerContentResult source)
    {
        return new OerCollectionResult
        {
            Status = source.Status,
            ConsumedCount = source.ConsumedCount,
            HasViolationOffset = source.HasViolationOffset,
            ViolationOffset = source.ViolationOffset,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal OerCollectionResult CreateMissingFieldError(string name)
    {
        return this.CreateCollectionError(
            "ASNO0059",
            "A required OER component is absent: " + name,
            false);
    }

    internal OerCollectionResult CreateMissingTypeError(string name)
    {
        return this.CreateCollectionError(
            "ASNO0060",
            "The OER component has no resolved type: " + name,
            false);
    }

    internal OerCollectionResult CreateCollectionError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new OerCollectionResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal OerCollectionResult CreateCollectionErrorAt(
        string code,
        string message,
        bool limitExceeded,
        int violationOffset)
    {
        OerCollectionResult result = this.CreateCollectionError(
            code,
            message,
            limitExceeded);
        result.HasViolationOffset = true;
        result.ViolationOffset = violationOffset;
        return result;
    }

    internal OerCollectionResult ShiftCollectionFailure(
        OerCollectionResult result,
        int prefixCount)
    {
        if (!result.Success)
        {
            result.ConsumedCount = prefixCount + result.ConsumedCount;
            result.ViolationOffset = prefixCount
                + (result.HasViolationOffset
                    ? result.ViolationOffset
                    : 0);
            result.HasViolationOffset = true;
        }

        return result;
    }
}
