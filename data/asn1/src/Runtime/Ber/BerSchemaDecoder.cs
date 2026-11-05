namespace asn1;

internal class BerSchemaDecoder
{
    internal AsnReflectionCatalog Catalog;

    internal BerSchemaDecoder(AsnReflectionCatalog catalog)
    {
        this.Catalog = catalog == null
            ? throw new System.ArgumentNullException("catalog")
            : catalog;
    }

    internal AsnDecodeResult DecodeRequest(AsnDecodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Data == null || request.TypeName == null)
        {
            throw new System.ArgumentException(
                "The schema decode request contains null data.",
                "request");
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = AsnEncodingRules.Ber }
            : request.Options;
        AsnTypeDescriptor? type = this.ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreatePublicError(
                0,
                "ASND0090",
                "The requested schema type is missing or ambiguous.",
                false);
        }

        int count = request.HasCount
            ? request.Count
            : request.Data.Length - request.Offset;
        if (!CursorMath.IsRangeValid(request.Offset, count, request.Data.Length))
        {
            throw new System.ArgumentOutOfRangeException("request");
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        BerReadResult read = new BerTlvReaderImplementation(limits).Read(
            new BerReadRequest
            {
                Data = request.Data,
                Offset = request.Offset,
                HasCount = true,
                Count = count,
                EncodingRules = options.EncodingRules,
                PreserveEncodedData = true
            });
        if (read.Status != AsnDecodeStatus.Done || read.Tlv == null)
        {
            return new AsnDecodeResult
            {
                Status = read.Status,
                ConsumedCount = read.ConsumedCount,
                Diagnostics = read.Diagnostics
            };
        }

        BerSchemaDecodeResult decoded = this.DecodeTlv(type, read.Tlv, options);
        return new AsnDecodeResult
        {
            Status = decoded.Success
                ? AsnDecodeStatus.Done
                : AsnDecodeStatus.Error,
            Value = decoded.Value,
            ConsumedCount = read.ConsumedCount,
            Diagnostics = decoded.Diagnostics
        };
    }

    internal BerSchemaDecodeResult DecodeTlv(
        AsnTypeDescriptor type,
        BerTlv tlv,
        AsnCodecOptions options)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (tlv == null)
        {
            throw new System.ArgumentNullException("tlv");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (!this.IsBerFamilyRule(options.EncodingRules))
        {
            return this.CreateSchemaError(
                "ASND0091",
                "Schema-aware BER decoding requires BER, DER, or CER rules.",
                false);
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        BerSchemaDecodeResult decoded = this.DecodeTypeNode(
            type,
            tlv,
            new BerSchemaDecodeContext(options, limits));
        if (decoded.Success
            && decoded.Value != null
            && options.ValidateValues)
        {
            AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
                .ValidateRuntimeValue(decoded.Value, type, limits);
            if (diagnostics.Length > 0)
            {
                return new BerSchemaDecodeResult
                {
                    Diagnostics = diagnostics
                };
            }
        }

        return decoded;
    }

    internal BerSchemaDecodeResult DecodeTypeNode(
        AsnTypeDescriptor type,
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        AsnTagDescriptor[] tags = this.BuildTypeTagChain(type);
        BerTlv current = tlv;
        if (tags.Length > 0)
        {
            BerConstructedResult untagged = new BerConstructedCodec(
                context.Limits).RemoveTagChain(tlv, tags);
            if (!untagged.Success || untagged.Tlv == null)
            {
                return this.CopyConstructedFailure(untagged);
            }

            current = untagged.Tlv;
        }

        return this.DecodeTypeCore(type, current, context);
    }

    internal BerSchemaDecodeResult DecodeFieldNode(
        AsnFieldDescriptor field,
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        if (field.Type == null)
        {
            return this.CreateSchemaError(
                "ASND0092",
                "A matched field has no resolved type: " + field.Name,
                false);
        }

        if (field.Tags.Length == 0)
        {
            return this.DecodeTypeNode(field.Type, tlv, context);
        }

        AsnTagDescriptor[] tags = this.BuildFieldTagChain(field);
        BerConstructedResult untagged = new BerConstructedCodec(
            context.Limits).RemoveTagChain(tlv, tags);
        if (!untagged.Success || untagged.Tlv == null)
        {
            return this.CopyConstructedFailure(untagged);
        }

        return this.DecodeTypeCore(field.Type, untagged.Tlv, context);
    }

    internal BerSchemaDecodeResult DecodeTypeCore(
        AsnTypeDescriptor type,
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        ValueAccess valueAccess = new ValueAccess();
        if (valueAccess.IsTransparentTypeKind(type.Kind))
        {
            AsnTypeDescriptor? effectiveType = valueAccess.GetEffectiveType(type);
            if (effectiveType == null)
            {
                return this.CreateSchemaError(
                    "ASND0093",
                    "The transparent schema type has no effective type.",
                    false);
            }

            if (type.Tags.Length == 0 && type.ElementType != null)
            {
                return this.DecodeTypeNode(type.ElementType, tlv, context);
            }

            return this.DecodeTypeCore(effectiveType, tlv, context);
        }

        bool entered = tlv.Constructed;
        if (entered && !context.Resources.TryEnterNesting())
        {
            return this.CreateSchemaError(
                "ASNR0002",
                "Schema decoding exceeds the configured nesting limit.",
                true);
        }

        BerSchemaDecodeResult result;
        if (this.IsPrimitiveType(type.Kind))
        {
            result = this.DecodePrimitiveNode(type.Kind, tlv, context);
        }
        else if (this.IsUsefulType(type.Kind))
        {
            result = this.DecodeUsefulNode(type.Kind, tlv, context);
        }
        else if (type.Kind == AsnTypeKind.Sequence)
        {
            result = this.DecodeSequenceNode(type, tlv, context);
        }
        else if (type.Kind == AsnTypeKind.Set)
        {
            result = this.DecodeSetNode(type, tlv, context);
        }
        else if (type.Kind == AsnTypeKind.Choice)
        {
            result = this.DecodeChoiceNode(type, tlv, context);
        }
        else if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            result = this.DecodeCollectionNode(type, tlv, context);
        }
        else if (type.Kind == AsnTypeKind.OpenType)
        {
            result = this.DecodeOpenTypeNode(type, tlv, context);
        }
        else
        {
            result = this.CreateSchemaError(
                "ASND0093",
                "The schema type is not supported by BER decoding: "
                    + type.Kind,
                false);
        }

        if (entered)
        {
            context.Resources.LeaveNesting();
        }

        return result;
    }

    internal BerSchemaDecodeResult DecodePrimitiveNode(
        AsnTypeKind kind,
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        BerPrimitiveDecodeResult decoded;
        if (this.IsStringType(kind) && tlv.Constructed)
        {
            BerStringResult strings = new BerStringCodec(context.Limits)
                .DecodeConstructedString(
                    kind,
                    tlv,
                    context.Options.EncodingRules);
            if (!strings.Success || strings.Value == null)
            {
                return this.CreateSchemaError(
                    strings.ErrorCode,
                    strings.ErrorMessage,
                    strings.LimitExceeded);
            }

            return this.CreateSchemaSuccess(strings.Value);
        }

        decoded = new BerPrimitiveCodec(context.Limits).DecodePrimitive(
            kind,
            tlv,
            context.Options.EncodingRules);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CopyPrimitiveFailure(decoded);
        }

        return this.CreateSchemaSuccess(decoded.Value);
    }

    internal BerSchemaDecodeResult DecodeUsefulNode(
        AsnTypeKind kind,
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        BerPrimitiveDecodeResult decoded = new BerUsefulTypeCodec(
            context.Limits).DecodeUsefulType(
                kind,
                tlv,
                context.Options.EncodingRules);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CopyPrimitiveFailure(decoded);
        }

        return this.CreateSchemaSuccess(decoded.Value);
    }

    internal BerSchemaDecodeResult DecodeSequenceNode(
        AsnTypeDescriptor type,
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        BerConstructedResult read = new BerConstructedCodec(context.Limits)
            .ReadSequenceValue(tlv, context.Options.EncodingRules);
        if (!read.Success)
        {
            return this.CopyConstructedFailure(read);
        }

        AsnRuntimeFieldValue[] fields = new AsnRuntimeFieldValue[
            type.Fields.Length];
        System.Collections.Generic.List<AsnUnknownExtension> unknown =
            new System.Collections.Generic.List<AsnUnknownExtension>();
        int childIndex = 0;
        int fieldIndex = 0;
        while (childIndex < read.Children.Length)
        {
            BerSchemaMatch match = this.FindSequenceMatch(
                type.Fields,
                fieldIndex,
                read.Children[childIndex]);
            if (match.Count > 1)
            {
                return this.CreateAmbiguityError(
                    "SEQUENCE component",
                    read.Children[childIndex]);
            }

            if (match.Count == 0)
            {
                while (fieldIndex < type.Fields.Length
                    && type.Fields[fieldIndex] != null
                    && !type.Fields[fieldIndex].Extension
                    && (type.Fields[fieldIndex].Optional
                        || type.Fields[fieldIndex].HasDefaultValue))
                {
                    BerSchemaDecodeResult rootAbsent = this.CreateAbsentField(
                        type.Fields[fieldIndex]);
                    if (!rootAbsent.Success || rootAbsent.Value == null)
                    {
                        return rootAbsent;
                    }

                    fields[fieldIndex] = rootAbsent.Value.Fields[0];
                    fieldIndex = fieldIndex + 1;
                }

                if (fieldIndex < type.Fields.Length)
                {
                    AsnFieldDescriptor pending = type.Fields[fieldIndex];
                    if (pending == null)
                    {
                        return this.CreateSchemaError(
                            "ASND0094",
                            "The SEQUENCE descriptor contains a null field.",
                            false);
                    }

                    if (!pending.Extension
                        && !pending.Optional
                        && !pending.HasDefaultValue)
                    {
                        return this.CreateSchemaError(
                            "ASND0098",
                            "A required field is absent: " + pending.Name,
                            false);
                    }
                }

                BerSchemaDecodeResult handled = this.HandleUnknownExtension(
                    type,
                    read.Children[childIndex],
                    unknown.Count,
                    childIndex,
                    context,
                    unknown);
                if (!handled.Success)
                {
                    return handled;
                }

                childIndex = childIndex + 1;
                continue;
            }

            while (fieldIndex < match.Index)
            {
                BerSchemaDecodeResult skipped = this.CreateAbsentField(
                    type.Fields[fieldIndex]);
                if (!skipped.Success || skipped.Value == null)
                {
                    return skipped;
                }

                fields[fieldIndex] = skipped.Value.Fields[0];
                fieldIndex = fieldIndex + 1;
            }

            AsnFieldDescriptor field = type.Fields[fieldIndex];
            BerSchemaDecodeResult decoded = this.DecodeFieldNode(
                field,
                read.Children[childIndex],
                context);
            if (!decoded.Success || decoded.Value == null)
            {
                return decoded;
            }

            fields[fieldIndex] = new AsnRuntimeFieldValue
            {
                Name = field.Name,
                Present = true,
                PresentDefault = this.MatchesDefault(field, decoded.Value),
                Value = decoded.Value
            };
            childIndex = childIndex + 1;
            fieldIndex = fieldIndex + 1;
        }

        while (fieldIndex < type.Fields.Length)
        {
            BerSchemaDecodeResult absent = this.CreateAbsentField(
                type.Fields[fieldIndex]);
            if (!absent.Success || absent.Value == null)
            {
                return absent;
            }

            fields[fieldIndex] = absent.Value.Fields[0];
            fieldIndex = fieldIndex + 1;
        }

        return this.CreateSchemaSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = fields,
            UnknownExtensions = unknown.ToArray()
        });
    }

    internal BerSchemaDecodeResult DecodeSetNode(
        AsnTypeDescriptor type,
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        BerConstructedResult read = new BerConstructedCodec(context.Limits)
            .ReadSetValue(tlv, context.Options.EncodingRules);
        if (!read.Success)
        {
            return this.CopyConstructedFailure(read);
        }

        AsnRuntimeFieldValue[] fields = new AsnRuntimeFieldValue[
            type.Fields.Length];
        bool[] consumed = new bool[type.Fields.Length];
        System.Collections.Generic.List<AsnUnknownExtension> unknown =
            new System.Collections.Generic.List<AsnUnknownExtension>();
        int childIndex = 0;
        while (childIndex < read.Children.Length)
        {
            int match = -1;
            int count = 0;
            int fieldIndex = 0;
            while (fieldIndex < type.Fields.Length)
            {
                AsnFieldDescriptor field = type.Fields[fieldIndex];
                if (!consumed[fieldIndex]
                    && field != null
                    && this.FieldMatchesTag(field, read.Children[childIndex]))
                {
                    match = fieldIndex;
                    count = count + 1;
                }

                fieldIndex = fieldIndex + 1;
            }

            if (count > 1)
            {
                return this.CreateAmbiguityError(
                    "SET component",
                    read.Children[childIndex]);
            }

            if (count == 0)
            {
                BerSchemaDecodeResult handled = this.HandleUnknownExtension(
                    type,
                    read.Children[childIndex],
                    unknown.Count,
                    childIndex,
                    context,
                    unknown);
                if (!handled.Success)
                {
                    return handled;
                }
            }
            else
            {
                AsnFieldDescriptor field = type.Fields[match];
                BerSchemaDecodeResult decoded = this.DecodeFieldNode(
                    field,
                    read.Children[childIndex],
                    context);
                if (!decoded.Success || decoded.Value == null)
                {
                    return decoded;
                }

                consumed[match] = true;
                fields[match] = new AsnRuntimeFieldValue
                {
                    Name = field.Name,
                    Present = true,
                    PresentDefault = this.MatchesDefault(field, decoded.Value),
                    Value = decoded.Value
                };
            }

            childIndex = childIndex + 1;
        }

        int missingIndex = 0;
        while (missingIndex < type.Fields.Length)
        {
            if (!consumed[missingIndex])
            {
                BerSchemaDecodeResult absent = this.CreateAbsentField(
                    type.Fields[missingIndex]);
                if (!absent.Success || absent.Value == null)
                {
                    return absent;
                }

                fields[missingIndex] = absent.Value.Fields[0];
            }

            missingIndex = missingIndex + 1;
        }

        return this.CreateSchemaSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Set,
            Fields = fields,
            UnknownExtensions = unknown.ToArray()
        });
    }

    internal BerSchemaDecodeResult DecodeChoiceNode(
        AsnTypeDescriptor type,
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        int match = -1;
        int count = 0;
        int index = 0;
        while (index < type.Fields.Length)
        {
            AsnFieldDescriptor field = type.Fields[index];
            if (field != null && this.FieldMatchesTag(field, tlv))
            {
                match = index;
                count = count + 1;
            }

            index = index + 1;
        }

        if (count > 1)
        {
            return this.CreateAmbiguityError("CHOICE alternative", tlv);
        }

        if (count == 0)
        {
            return this.CreateSchemaError(
                "ASND0095",
                "No CHOICE alternative matches the encoded tag.",
                false);
        }

        AsnFieldDescriptor selected = type.Fields[match];
        BerSchemaDecodeResult decoded = this.DecodeFieldNode(
            selected,
            tlv,
            context);
        if (!decoded.Success || decoded.Value == null)
        {
            return decoded;
        }

        return this.CreateSchemaSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = selected.Name,
                    Present = true,
                    Value = decoded.Value
                }
            ]
        });
    }

    internal BerSchemaDecodeResult DecodeCollectionNode(
        AsnTypeDescriptor type,
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        if (type.ElementType == null)
        {
            return this.CreateSchemaError(
                "ASND0096",
                "A collection descriptor requires an element type.",
                false);
        }

        BerConstructedCodec constructed = new BerConstructedCodec(
            context.Limits);
        BerConstructedResult read = type.Kind == AsnTypeKind.SequenceOf
            ? constructed.ReadSequenceOfValue(
                tlv,
                context.Options.EncodingRules)
            : constructed.ReadSetOfValue(
                tlv,
                context.Options.EncodingRules);
        if (!read.Success)
        {
            return this.CopyConstructedFailure(read);
        }

        AsnRuntimeValue[] items = new AsnRuntimeValue[read.Children.Length];
        int index = 0;
        while (index < read.Children.Length)
        {
            BerSchemaDecodeResult decoded = this.DecodeTypeNode(
                type.ElementType,
                read.Children[index],
                context);
            if (!decoded.Success || decoded.Value == null)
            {
                return decoded;
            }

            items[index] = decoded.Value;
            index = index + 1;
        }

        return this.CreateSchemaSuccess(new AsnRuntimeValue
        {
            Kind = type.Kind == AsnTypeKind.SequenceOf
                ? AsnRuntimeValueKind.SequenceOf
                : AsnRuntimeValueKind.SetOf,
            Items = items
        });
    }

    internal BerSchemaDecodeResult DecodeOpenTypeNode(
        AsnTypeDescriptor type,
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        AsnObjectTableEntryDescriptor? match = null;
        int count = 0;
        int tableIndex = 0;
        while (tableIndex < type.ObjectTables.Length)
        {
            AsnObjectTableDescriptor table = type.ObjectTables[tableIndex];
            int entryIndex = 0;
            while (entryIndex < table.Entries.Length)
            {
                AsnObjectTableEntryDescriptor entry = table.Entries[entryIndex];
                if (entry != null
                    && entry.Type != null
                    && this.TypeMatchesTag(entry.Type, tlv))
                {
                    match = entry;
                    count = count + 1;
                }

                entryIndex = entryIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        if (count > 1)
        {
            return this.CreateAmbiguityError("open-type table entry", tlv);
        }

        byte[] original = this.GetEncodedData(tlv, context);
        if (count == 0 || match == null || match.Type == null)
        {
            if (context.Options.UnknownDataPolicy == AsnUnknownDataPolicy.Reject)
            {
                return this.CreateSchemaError(
                    "ASND0097",
                    "No open-type table entry matches the encoded tag.",
                    false);
            }

            bool preserve = context.Options.UnknownDataPolicy
                == AsnUnknownDataPolicy.Preserve;
            return this.CreateSchemaSuccess(new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OpenType,
                OpenTypeValue = new AsnOpenTypeValue
                {
                    Unresolved = true,
                    EncodingRules = context.Options.EncodingRules,
                    HasEncodingRules = preserve,
                    HasOriginalData = preserve,
                    OriginalData = preserve ? original : [],
                    OriginalTag = this.CopyTag(tlv.Tag),
                    HasEncodedLocation = preserve && tlv.EncodedOffset >= 0,
                    EncodedOffset = tlv.EncodedOffset,
                    EncodedLength = preserve ? original.Length : 0
                }
            });
        }

        BerSchemaDecodeResult decoded = this.DecodeTypeNode(
            match.Type,
            tlv,
            context);
        if (!decoded.Success || decoded.Value == null)
        {
            return decoded;
        }

        return this.CreateSchemaSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Resolved = true,
                ResolvedTypeId = match.Type.StableId,
                TypeName = match.Type.Name,
                Value = decoded.Value,
                DispatchKey = this.CreateDispatchKey(match.Key),
                EncodingRules = context.Options.EncodingRules,
                HasEncodingRules = true,
                HasOriginalData = true,
                OriginalData = original,
                OriginalTag = this.CopyTag(tlv.Tag),
                HasEncodedLocation = tlv.EncodedOffset >= 0,
                EncodedOffset = tlv.EncodedOffset,
                EncodedLength = original.Length
            }
        });
    }

    internal BerSchemaDecodeResult CreateAbsentField(AsnFieldDescriptor field)
    {
        if (field == null)
        {
            return this.CreateSchemaError(
                "ASND0094",
                "The structured descriptor contains a null field.",
                false);
        }

        if (!field.Optional && !field.HasDefaultValue && !field.Extension)
        {
            return this.CreateSchemaError(
                "ASND0098",
                "A required field is absent: " + field.Name,
                false);
        }

        AsnRuntimeValue? defaultValue = null;
        if (field.HasDefaultValue)
        {
            if (field.Type == null
                || field.DefaultValue == null
                || field.DefaultValue.Value == null)
            {
                return this.CreateSchemaError(
                    "ASND0099",
                    "A DEFAULT field has incomplete schema metadata: "
                        + field.Name,
                    false);
            }

            defaultValue = new ValueAccess().FromSchemaValue(
                field.DefaultValue.Value,
                field.Type,
                this.Catalog);
        }

        return this.CreateSchemaSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = field.Name,
                    Value = defaultValue
                }
            ]
        });
    }

    internal BerSchemaDecodeResult HandleUnknownExtension(
        AsnTypeDescriptor type,
        BerTlv tlv,
        int index,
        int encodedIndex,
        BerSchemaDecodeContext context,
        System.Collections.Generic.List<AsnUnknownExtension> unknown)
    {
        if (!type.Extensible
            || context.Options.UnknownDataPolicy == AsnUnknownDataPolicy.Reject)
        {
            return this.CreateSchemaError(
                "ASND0100",
                "An unknown component is not permitted by the schema or policy.",
                false);
        }

        if (context.Options.UnknownDataPolicy == AsnUnknownDataPolicy.Preserve)
        {
            byte[] data = this.GetEncodedData(tlv, context);
            unknown.Add(new AsnUnknownExtension
            {
                Index = index,
                Tag = this.CopyTag(tlv.Tag),
                HasEncodedIndex = true,
                EncodedIndex = encodedIndex,
                HasEncodedLocation = tlv.EncodedOffset >= 0,
                EncodedOffset = tlv.EncodedOffset,
                EncodedLength = data.Length,
                EncodingRules = context.Options.EncodingRules,
                HasEncodingRules = true,
                HasBinaryData = true,
                BinaryData = data
            });
        }

        return this.CreateSchemaSuccess(new AsnRuntimeValue());
    }

    internal BerSchemaMatch FindSequenceMatch(
        AsnFieldDescriptor[] fields,
        int start,
        BerTlv tlv)
    {
        BerSchemaMatch result = new BerSchemaMatch();
        int index = start;
        bool canContinue = true;
        while (index < fields.Length && canContinue)
        {
            AsnFieldDescriptor field = fields[index];
            if (field != null && this.FieldMatchesTag(field, tlv))
            {
                result.Count = result.Count + 1;
                result.Index = index;
            }

            canContinue = field != null
                && (field.Optional
                    || field.HasDefaultValue
                    || field.Extension);
            index = index + 1;
        }

        return result;
    }

    internal bool FieldMatchesTag(AsnFieldDescriptor field, BerTlv tlv)
    {
        if (field.Type == null)
        {
            return false;
        }

        if (field.Tags.Length > 0)
        {
            AsnTagDescriptor tag = field.Tags[0];
            return tag != null
                && tag.Class == tlv.Tag.Class
                && tag.Number == tlv.Tag.Number;
        }

        return this.TypeMatchesTag(field.Type, tlv);
    }

    internal bool TypeMatchesTag(AsnTypeDescriptor type, BerTlv tlv)
    {
        return this.TypeMatchesTagGuarded(
            type,
            tlv,
            new System.Collections.Generic.List<AsnTypeDescriptor>());
    }

    internal bool TypeMatchesTagGuarded(
        AsnTypeDescriptor type,
        BerTlv tlv,
        System.Collections.Generic.List<AsnTypeDescriptor> path)
    {
        if (path.Contains(type))
        {
            return false;
        }

        path.Add(type);
        bool result = false;
        if (type.Tags.Length > 0)
        {
            AsnTagDescriptor tag = type.Tags[0];
            result = tag != null
                && tag.Class == tlv.Tag.Class
                && tag.Number == tlv.Tag.Number;
        }
        else
        {
            int universal = this.GetUniversalTagNumber(type.Kind);
            if (universal >= 0)
            {
                result = tlv.Tag.Class == AsnTagClass.Universal
                    && tlv.Tag.Number
                        == new System.Numerics.BigInteger(universal);
            }
            else if (type.Kind == AsnTypeKind.Choice)
            {
                int count = 0;
                int index = 0;
                while (index < type.Fields.Length)
                {
                    if (type.Fields[index] != null
                        && this.FieldMatchesTagGuarded(
                            type.Fields[index],
                            tlv,
                            path))
                    {
                        count = count + 1;
                    }

                    index = index + 1;
                }

                result = count > 0;
            }
            else if (type.Kind == AsnTypeKind.OpenType)
            {
                result = true;
            }
            else if (new ValueAccess().IsTransparentTypeKind(type.Kind)
                && type.ElementType != null)
            {
                result = this.TypeMatchesTagGuarded(
                    type.ElementType,
                    tlv,
                    path);
            }
        }

        path.RemoveAt(path.Count - 1);
        return result;
    }

    internal bool FieldMatchesTagGuarded(
        AsnFieldDescriptor field,
        BerTlv tlv,
        System.Collections.Generic.List<AsnTypeDescriptor> path)
    {
        if (field.Type == null)
        {
            return false;
        }

        if (field.Tags.Length > 0)
        {
            AsnTagDescriptor tag = field.Tags[0];
            return tag != null
                && tag.Class == tlv.Tag.Class
                && tag.Number == tlv.Tag.Number;
        }

        return this.TypeMatchesTagGuarded(field.Type, tlv, path);
    }

    internal AsnTagDescriptor[] BuildTypeTagChain(AsnTypeDescriptor type)
    {
        return this.AppendUniversalTag(type.Tags, type.Kind);
    }

    internal AsnTagDescriptor[] BuildFieldTagChain(AsnFieldDescriptor field)
    {
        if (field.Type == null)
        {
            return field.Tags;
        }

        return this.AppendUniversalTag(field.Tags, field.Type.Kind);
    }

    internal AsnTagDescriptor[] AppendUniversalTag(
        AsnTagDescriptor[] tags,
        AsnTypeKind kind)
    {
        int universal = this.GetUniversalTagNumber(kind);
        if (universal < 0)
        {
            return tags;
        }

        if (tags.Length > 0)
        {
            AsnTagDescriptor last = tags[tags.Length - 1];
            if (last != null
                && last.Class == AsnTagClass.Universal
                && last.Number == new System.Numerics.BigInteger(universal))
            {
                return tags;
            }
        }

        AsnTagDescriptor[] result = new AsnTagDescriptor[tags.Length + 1];
        int index = 0;
        while (index < tags.Length)
        {
            result[index] = tags[index];
            index = index + 1;
        }

        result[tags.Length] = new AsnTagDescriptor
        {
            Class = AsnTagClass.Universal,
            Mode = AsnTaggingMode.Implicit,
            Number = universal,
            Universal = true
        };
        return result;
    }

    internal int GetUniversalTagNumber(AsnTypeKind kind)
    {
        int primitive = new BerPrimitiveCodec(new AsnResourceLimits())
            .GetUniversalTagNumber(kind);
        if (primitive >= 0)
        {
            return primitive;
        }

        if (kind == AsnTypeKind.Sequence
            || kind == AsnTypeKind.SequenceOf)
        {
            return AsnUniversalTags.Sequence;
        }

        if (kind == AsnTypeKind.Set || kind == AsnTypeKind.SetOf)
        {
            return AsnUniversalTags.Set;
        }

        if (kind == AsnTypeKind.ObjectDescriptor)
        {
            return AsnUniversalTags.ObjectDescriptor;
        }

        if (kind == AsnTypeKind.External)
        {
            return AsnUniversalTags.External;
        }

        if (kind == AsnTypeKind.EmbeddedPdv)
        {
            return AsnUniversalTags.EmbeddedPdv;
        }

        if (kind == AsnTypeKind.CharacterString)
        {
            return AsnUniversalTags.CharacterString;
        }

        if (kind == AsnTypeKind.InstanceOf)
        {
            return AsnUniversalTags.InstanceOf;
        }

        return new BerTimeCodec(new AsnResourceLimits())
            .GetUniversalTagNumber(kind);
    }

    internal bool MatchesDefault(
        AsnFieldDescriptor field,
        AsnRuntimeValue value)
    {
        return field.HasDefaultValue
            && field.DefaultValue != null
            && field.DefaultValue.Value != null
            && new RuntimeConstraintEvaluator().RuntimeValueMatchesAsnValue(
                value,
                field.DefaultValue.Value);
    }

    internal AsnRuntimeValue? CreateDispatchKey(AsnValueDescriptor? descriptor)
    {
        if (descriptor == null || descriptor.Value == null)
        {
            return null;
        }

        AsnValue value = descriptor.Value;
        AsnRuntimeValue result = new AsnRuntimeValue
        {
            Kind = (AsnRuntimeValueKind)(int)value.Kind,
            HasIntegerValue = value.HasIntegerValue,
            IntegerValue = value.IntegerValue,
            TextValue = value.TextValue,
            Octets = this.CopyBytes(value.Octets)
        };
        if (value.Kind == AsnValueKind.Boolean)
        {
            result.HasBooleanValue = true;
            result.BooleanValue = string.Equals(
                value.TextValue,
                "TRUE",
                System.StringComparison.OrdinalIgnoreCase);
        }

        result.BitStringValue = value.BitStringValue;
        result.ObjectIdentifierValue = value.ObjectIdentifierValue;
        result.RealValue = value.RealValue;
        result.TimeValue = value.TimeValue;
        return result;
    }

    internal byte[] GetEncodedData(
        BerTlv tlv,
        BerSchemaDecodeContext context)
    {
        if (tlv.EncodedData.Length > 0)
        {
            return this.CopyBytes(tlv.EncodedData);
        }

        BerWriteResult written = new BerTlvWriterImplementation(
            context.Limits).Write(new BerWriteRequest
            {
                Tlv = tlv,
                EncodingRules = context.Options.EncodingRules
            });
        return written.Success ? written.Data : [];
    }

    internal AsnTypeDescriptor? ResolveRequestedType(string typeName)
    {
        AsnTypeDescriptor? result = null;
        int index = 0;
        while (index < this.Catalog.Assignments.Length)
        {
            AsnAssignmentDescriptor assignment = this.Catalog.Assignments[index];
            if (assignment.Type != null
                && (assignment.QualifiedName == typeName
                    || assignment.Name == typeName
                    || assignment.GeneratedName == typeName
                    || assignment.StableId == typeName
                    || assignment.Type.Name == typeName
                    || assignment.Type.GeneratedName == typeName
                    || assignment.Type.StableId == typeName))
            {
                if (result != null && result != assignment.Type)
                {
                    return null;
                }

                result = assignment.Type;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool IsPrimitiveType(AsnTypeKind kind)
    {
        return new BerPrimitiveCodec(new AsnResourceLimits())
            .GetUniversalTagNumber(kind) >= 0;
    }

    internal bool IsStringType(AsnTypeKind kind)
    {
        return new BerStringCodec(new AsnResourceLimits())
            .IsSupportedStringKind(kind);
    }

    internal bool IsUsefulType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.ObjectDescriptor
            || kind == AsnTypeKind.External
            || kind == AsnTypeKind.EmbeddedPdv
            || kind == AsnTypeKind.CharacterString
            || kind == AsnTypeKind.InstanceOf
            || kind == AsnTypeKind.UtcTime
            || kind == AsnTypeKind.GeneralizedTime
            || kind == AsnTypeKind.Time
            || kind == AsnTypeKind.Date
            || kind == AsnTypeKind.TimeOfDay
            || kind == AsnTypeKind.DateTime
            || kind == AsnTypeKind.Duration;
    }

    internal bool IsBerFamilyRule(AsnEncodingRules rules)
    {
        return rules == AsnEncodingRules.Ber
            || rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer;
    }

    internal BerSchemaDecodeResult CopyPrimitiveFailure(
        BerPrimitiveDecodeResult result)
    {
        if (result.Diagnostics.Length > 0)
        {
            return new BerSchemaDecodeResult
            {
                Diagnostics = result.Diagnostics
            };
        }

        return this.CreateSchemaError(
            "ASND0101",
            "The BER value could not be decoded.",
            false);
    }

    internal BerSchemaDecodeResult CopyConstructedFailure(
        BerConstructedResult result)
    {
        return this.CreateSchemaError(
            result.ErrorCode,
            result.ErrorMessage,
            result.LimitExceeded);
    }

    internal BerSchemaDecodeResult CreateAmbiguityError(
        string subject,
        BerTlv tlv)
    {
        return this.CreateSchemaError(
            "ASND0102",
            "More than one " + subject + " matches tag "
                + tlv.Tag.Class + ":" + tlv.Tag.Number + ".",
            false);
    }

    internal BerSchemaDecodeResult CreateSchemaSuccess(AsnRuntimeValue value)
    {
        return new BerSchemaDecodeResult
        {
            Success = true,
            Value = value
        };
    }

    internal BerSchemaDecodeResult CreateSchemaError(
        string code,
        string message,
        bool resource)
    {
        return new BerSchemaDecodeResult
        {
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = resource
                        ? AsnDiagnosticCategory.Resource
                        : code == "ASNCAN0011"
                            ? AsnDiagnosticCategory.Canonicality
                        : AsnDiagnosticCategory.Decoding,
                    Code = code,
                    Message = message
                }
            ]
        };
    }

    internal AsnDecodeResult CreatePublicError(
        int consumed,
        string code,
        string message,
        bool resource)
    {
        BerSchemaDecodeResult error = this.CreateSchemaError(
            code,
            message,
            resource);
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Error,
            ConsumedCount = consumed,
            Diagnostics = error.Diagnostics
        };
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Buffer.BlockCopy(source, 0, result, 0, source.Length);
        return result;
    }

    internal AsnTag CopyTag(AsnTag tag)
    {
        return new AsnTag
        {
            Class = tag.Class,
            Number = tag.Number
        };
    }
}
