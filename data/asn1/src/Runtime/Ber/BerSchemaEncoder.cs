namespace asn1;

internal class BerSchemaEncoder
{
    internal AsnReflectionCatalog Catalog;

    internal BerSchemaEncoder(AsnReflectionCatalog catalog)
    {
        this.Catalog = catalog == null
            ? throw new System.ArgumentNullException("catalog")
            : catalog;
    }

    internal AsnEncodeResult EncodeRequest(AsnEncodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.TypeName == null)
        {
            throw new System.ArgumentException(
                "The schema encode request contains a null type name.",
                "request");
        }

        if (request.Value == null)
        {
            return this.CreatePublicError(
                "ASNE0071",
                "A schema encode request requires a runtime value.",
                false);
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreatePublicError(
                "ASNE0072",
                "The requested schema type is missing or ambiguous.",
                false);
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = AsnEncodingRules.Ber }
            : request.Options;
        BerSchemaEncodeResult encoded = this.EncodeTlv(
            type,
            request.Value,
            options);
        if (!encoded.Success || encoded.Tlv == null)
        {
            return new AsnEncodeResult
            {
                Diagnostics = encoded.Diagnostics
            };
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        BerWriteResult written = new BerTlvWriterImplementation(limits).Write(
            new BerWriteRequest
            {
                Tlv = encoded.Tlv,
                EncodingRules = options.EncodingRules
            });
        if (!written.Success)
        {
            return new AsnEncodeResult
            {
                Diagnostics = written.Diagnostics
            };
        }

        return new AsnEncodeResult
        {
            Success = true,
            Data = written.Data,
            WrittenCount = written.WrittenCount
        };
    }

    internal AsnEncodeResult EncodeSpan(
        AsnEncodeRequest request,
        System.Span<byte> destination)
    {
        AsnEncodeResult encoded = this.EncodeRequest(request);
        if (!encoded.Success)
        {
            return encoded;
        }

        if (destination.Length < encoded.WrittenCount)
        {
            return this.CreatePublicError(
                "ASNE0073",
                "The destination span is too small for the encoded value.",
                false);
        }

        new System.ReadOnlySpan<byte>(
            encoded.Data,
            0,
            encoded.WrittenCount).CopyTo(destination);
        return new AsnEncodeResult
        {
            Success = true,
            Data = encoded.Data,
            WrittenCount = encoded.WrittenCount,
            StreamProgress = new AsnStreamProgress
            {
                WrittenCount = encoded.WrittenCount,
                HasExpectedCount = true,
                ExpectedCount = encoded.WrittenCount,
                Complete = true
            }
        };
    }

    internal AsnEncodeResult EncodeStream(
        AsnEncodeRequest request,
        System.IO.Stream output)
    {
        if (output == null)
        {
            throw new System.ArgumentNullException("output");
        }

        AsnEncodeResult encoded = this.EncodeRequest(request);
        if (!encoded.Success)
        {
            return encoded;
        }

        int index = 0;
        while (index < encoded.WrittenCount)
        {
            output.Write(encoded.Data, index, 1);
            index = index + 1;
        }
        encoded.StreamProgress = new AsnStreamProgress
        {
            WrittenCount = encoded.WrittenCount,
            HasExpectedCount = true,
            ExpectedCount = encoded.WrittenCount,
            Complete = true
        };
        return encoded;
    }

    internal BerSchemaEncodeResult EncodeTlv(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        AsnCodecOptions options)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (!this.IsBerFamilyRule(options.EncodingRules))
        {
            return this.CreateSchemaError(
                "ASNE0074",
                "Schema-aware BER encoding requires BER, DER, or CER rules.",
                false);
        }

        if (options.UseIndefiniteLengths
            && options.EncodingRules == AsnEncodingRules.Der)
        {
            return this.CreateSchemaError(
                "ASNE0075",
                "DER does not permit selected indefinite lengths.",
                false);
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        BerSchemaEncodeContext context = new BerSchemaEncodeContext(
            options,
            limits);
        BerSchemaEncodeResult encoded = this.EncodeTypeNode(
            type,
            value,
            context);
        if (!encoded.Success || encoded.Tlv == null)
        {
            return encoded;
        }

        if (options.ValidateValues)
        {
            AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
                .ValidateRuntimeValue(value, type, limits);
            if (diagnostics.Length > 0)
            {
                return new BerSchemaEncodeResult
                {
                    Diagnostics = diagnostics
                };
            }
        }

        if (options.UseIndefiniteLengths
            || options.EncodingRules == AsnEncodingRules.Cer)
        {
            BerConstructedResult indefinite = new BerConstructedCodec(limits)
                .MakeIndefiniteRecursively(
                    encoded.Tlv,
                    options.EncodingRules);
            if (!indefinite.Success || indefinite.Tlv == null)
            {
                return this.CopyConstructedFailure(indefinite);
            }

            encoded.Tlv = indefinite.Tlv;
        }

        return encoded;
    }

    internal BerSchemaEncodeResult EncodeTypeNode(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        BerSchemaEncodeContext context)
    {
        BerSchemaEncodeResult encoded = this.EncodeTypeCore(
            type,
            value,
            context);
        if (!encoded.Success || encoded.Tlv == null || type.Tags.Length == 0)
        {
            return encoded;
        }

        BerConstructedResult tagged = new BerConstructedCodec(context.Limits)
            .ApplyTagChain(encoded.Tlv, type.Tags);
        if (!tagged.Success || tagged.Tlv == null)
        {
            return this.CopyConstructedFailure(tagged);
        }

        return this.CreateSchemaSuccess(tagged.Tlv);
    }

    internal BerSchemaEncodeResult EncodeFieldNode(
        AsnFieldDescriptor field,
        AsnRuntimeValue value,
        BerSchemaEncodeContext context)
    {
        if (field.Type == null)
        {
            return this.CreateSchemaError(
                "ASNE0076",
                "A present field has no resolved type: " + field.Name,
                false);
        }

        if (field.Tags.Length == 0)
        {
            return this.EncodeTypeNode(field.Type, value, context);
        }

        BerSchemaEncodeResult encoded = this.EncodeTypeCore(
            field.Type,
            value,
            context);
        if (!encoded.Success || encoded.Tlv == null)
        {
            return encoded;
        }

        BerConstructedResult tagged = new BerConstructedCodec(context.Limits)
            .ApplyTagChain(encoded.Tlv, field.Tags);
        if (!tagged.Success || tagged.Tlv == null)
        {
            return this.CopyConstructedFailure(tagged);
        }

        return this.CreateSchemaSuccess(tagged.Tlv);
    }

    internal BerSchemaEncodeResult EncodeTypeCore(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        BerSchemaEncodeContext context)
    {
        ValueAccess valueAccess = new ValueAccess();
        AsnRuntimeValueKind expected = valueAccess.GetRuntimeKind(type);
        if (expected == AsnRuntimeValueKind.Unknown
            || (value.Kind != AsnRuntimeValueKind.Unknown
                && value.Kind != expected))
        {
            return this.CreateSchemaError(
                "ASNE0077",
                "The runtime value kind does not match the schema type.",
                false);
        }

        AsnRuntimeValue current = value.Kind == AsnRuntimeValueKind.Unknown
            ? this.CopyValueWithKind(value, expected)
            : value;

        if (valueAccess.IsTransparentTypeKind(type.Kind))
        {
            AsnTypeDescriptor? effectiveType = valueAccess.GetEffectiveType(type);
            if (effectiveType == null || type.ElementType == null)
            {
                return this.CreateSchemaError(
                    "ASNE0078",
                    "The transparent schema type has no effective type.",
                    false);
            }

            if (type.Tags.Length == 0)
            {
                return this.EncodeTypeNode(type.ElementType, current, context);
            }

            return this.EncodeTypeCore(effectiveType, current, context);
        }

        bool entered = expected == AsnRuntimeValueKind.Sequence
            || expected == AsnRuntimeValueKind.Set
            || expected == AsnRuntimeValueKind.Choice
            || expected == AsnRuntimeValueKind.SequenceOf
            || expected == AsnRuntimeValueKind.SetOf
            || expected == AsnRuntimeValueKind.OpenType;
        if (entered && !context.Resources.TryEnterNesting())
        {
            return this.CreateSchemaError(
                "ASNR0002",
                "Schema encoding exceeds the configured nesting limit.",
                true);
        }

        BerSchemaEncodeResult result;
        if (this.IsPrimitiveType(type.Kind))
        {
            result = this.EncodePrimitiveNode(type.Kind, current, context);
        }
        else if (this.IsUsefulType(type.Kind))
        {
            result = this.EncodeUsefulNode(type.Kind, current, context);
        }
        else if (type.Kind == AsnTypeKind.Sequence)
        {
            result = this.EncodeStructuredNode(type, current, context, false);
        }
        else if (type.Kind == AsnTypeKind.Set)
        {
            result = this.EncodeStructuredNode(type, current, context, true);
        }
        else if (type.Kind == AsnTypeKind.Choice)
        {
            result = this.EncodeChoiceNode(type, current, context);
        }
        else if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            result = this.EncodeCollectionNode(type, current, context);
        }
        else if (type.Kind == AsnTypeKind.OpenType)
        {
            result = this.EncodeOpenTypeNode(type, current, context);
        }
        else
        {
            result = this.CreateSchemaError(
                "ASNE0078",
                "The schema type is not supported by BER encoding: "
                    + type.Kind,
                false);
        }

        if (entered)
        {
            context.Resources.LeaveNesting();
        }

        return result;
    }

    internal BerSchemaEncodeResult EncodePrimitiveNode(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        BerSchemaEncodeContext context)
    {
        BerPrimitiveEncodeResult encoded = new BerPrimitiveCodec(
            context.Limits).EncodePrimitive(
                kind,
                value,
                context.Options.EncodingRules);
        if (!encoded.Success || encoded.Tlv == null)
        {
            return this.CopyPrimitiveFailure(encoded);
        }

        return this.CreateSchemaSuccess(encoded.Tlv);
    }

    internal BerSchemaEncodeResult EncodeUsefulNode(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        BerSchemaEncodeContext context)
    {
        BerPrimitiveEncodeResult encoded = new BerUsefulTypeCodec(
            context.Limits).EncodeUsefulType(
                kind,
                value,
                context.Options.EncodingRules);
        if (!encoded.Success || encoded.Tlv == null)
        {
            return this.CopyPrimitiveFailure(encoded);
        }

        return this.CreateSchemaSuccess(encoded.Tlv);
    }

    internal BerSchemaEncodeResult EncodeStructuredNode(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        BerSchemaEncodeContext context,
        bool set)
    {
        if (value.Fields == null
            || value.UnknownExtensions == null
            || value.UnknownFields == null)
        {
            return this.CreateSchemaError(
                "ASNE0079",
                "A structured runtime value contains a null collection.",
                false);
        }

        BerSchemaEncodeResult unknownResult = this.ValidateUnknownData(
            value,
            context);
        if (!unknownResult.Success)
        {
            return unknownResult;
        }

        BerComponentValue[] components = new BerComponentValue[
            type.Fields.Length];
        bool[] consumed = new bool[value.Fields.Length];
        int index = 0;
        while (index < type.Fields.Length)
        {
            AsnFieldDescriptor field = type.Fields[index];
            if (field == null)
            {
                return this.CreateSchemaError(
                    "ASNE0080",
                    "The structured descriptor contains a null field.",
                    false);
            }

            BerSchemaFieldLookup lookup = this.FindRuntimeField(
                value.Fields,
                field);
            if (lookup.Count > 1)
            {
                return this.CreateSchemaError(
                    "ASNE0081",
                    "The runtime value contains a duplicate field: "
                        + field.Name,
                    false);
            }

            BerComponentValue component = new BerComponentValue
            {
                Field = field
            };
            if (lookup.Count == 1)
            {
                consumed[lookup.Index] = true;
                AsnRuntimeFieldValue runtimeField = value.Fields[lookup.Index];
                component.Present = runtimeField.Present;
                if (runtimeField.Present)
                {
                    if (runtimeField.Value == null)
                    {
                        return this.CreateSchemaError(
                            "ASNE0082",
                            "A present field has no value: " + field.Name,
                            false);
                    }

                    BerSchemaEncodeResult encoded = this.EncodeFieldNode(
                        field,
                        runtimeField.Value,
                        context);
                    if (!encoded.Success || encoded.Tlv == null)
                    {
                        return encoded;
                    }

                    component.Tlv = encoded.Tlv;
                    component.EqualsDefault = this.MatchesDefault(
                        field,
                        runtimeField.Value);
                }
            }

            components[index] = component;
            index = index + 1;
        }

        BerSchemaEncodeResult extras = this.RequireAllFieldsConsumed(
            value.Fields,
            consumed);
        if (!extras.Success)
        {
            return extras;
        }

        BerConstructedCodec constructed = new BerConstructedCodec(
            context.Limits);
        BerConstructedResult encodedRoot = set
            ? constructed.EncodeSetValue(
                components,
                context.Options.EncodingRules,
                false)
            : constructed.EncodeSequenceValue(
                components,
                context.Options.EncodingRules,
                false);
        if (!encodedRoot.Success || encodedRoot.Tlv == null)
        {
            return this.CopyConstructedFailure(encodedRoot);
        }

        if (context.Options.UnknownDataPolicy == AsnUnknownDataPolicy.Preserve)
        {
            BerSchemaEncodeResult merged = this.MergePreservedUnknownData(
                encodedRoot.Tlv,
                value,
                context);
            if (!merged.Success || merged.Tlv == null)
            {
                return merged;
            }

            encodedRoot.Tlv = merged.Tlv;
        }

        return this.CreateSchemaSuccess(encodedRoot.Tlv);
    }

    internal BerSchemaEncodeResult EncodeChoiceNode(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        BerSchemaEncodeContext context)
    {
        if (value.Fields == null)
        {
            return this.CreateSchemaError(
                "ASNE0083",
                "A CHOICE runtime value has a null field collection.",
                false);
        }

        BerComponentValue[] alternatives = new BerComponentValue[
            type.Fields.Length];
        bool[] consumed = new bool[value.Fields.Length];
        int index = 0;
        while (index < type.Fields.Length)
        {
            AsnFieldDescriptor field = type.Fields[index];
            if (field == null)
            {
                return this.CreateSchemaError(
                    "ASNE0084",
                    "The CHOICE descriptor contains a null alternative.",
                    false);
            }

            BerSchemaFieldLookup lookup = this.FindRuntimeField(
                value.Fields,
                field);
            if (lookup.Count > 1)
            {
                return this.CreateSchemaError(
                    "ASNE0085",
                    "The CHOICE contains a duplicate alternative: "
                        + field.Name,
                    false);
            }

            BerComponentValue alternative = new BerComponentValue
            {
                Field = field
            };
            if (lookup.Count == 1)
            {
                consumed[lookup.Index] = true;
                AsnRuntimeFieldValue runtimeField = value.Fields[lookup.Index];
                alternative.Present = runtimeField.Present;
                if (runtimeField.Present)
                {
                    if (runtimeField.Value == null)
                    {
                        return this.CreateSchemaError(
                            "ASNE0086",
                            "The selected CHOICE alternative has no value.",
                            false);
                    }

                    BerSchemaEncodeResult encoded = this.EncodeFieldNode(
                        field,
                        runtimeField.Value,
                        context);
                    if (!encoded.Success || encoded.Tlv == null)
                    {
                        return encoded;
                    }

                    alternative.Tlv = encoded.Tlv;
                }
            }

            alternatives[index] = alternative;
            index = index + 1;
        }

        BerSchemaEncodeResult extras = this.RequireAllFieldsConsumed(
            value.Fields,
            consumed);
        if (!extras.Success)
        {
            return extras;
        }

        BerConstructedResult selected = new BerConstructedCodec(
            context.Limits).EncodeChoiceValue(alternatives);
        if (!selected.Success || selected.Tlv == null)
        {
            return this.CopyConstructedFailure(selected);
        }

        return this.CreateSchemaSuccess(selected.Tlv);
    }

    internal BerSchemaEncodeResult EncodeCollectionNode(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        BerSchemaEncodeContext context)
    {
        if (type.ElementType == null || value.Items == null)
        {
            return this.CreateSchemaError(
                "ASNE0087",
                "A collection requires an element type and item values.",
                false);
        }

        if (!context.Resources.AllowsCollectionElements(value.Items.Length))
        {
            return this.CreateSchemaError(
                "ASNR0011",
                "The collection exceeds the configured element limit.",
                true);
        }

        BerTlv[] items = new BerTlv[value.Items.Length];
        int index = 0;
        while (index < value.Items.Length)
        {
            if (value.Items[index] == null)
            {
                return this.CreateSchemaError(
                    "ASNE0088",
                    "The collection contains a null item.",
                    false);
            }

            BerSchemaEncodeResult encoded = this.EncodeTypeNode(
                type.ElementType,
                value.Items[index],
                context);
            if (!encoded.Success || encoded.Tlv == null)
            {
                return encoded;
            }

            items[index] = encoded.Tlv;
            index = index + 1;
        }

        BerConstructedCodec constructed = new BerConstructedCodec(
            context.Limits);
        BerConstructedResult root = type.Kind == AsnTypeKind.SequenceOf
            ? constructed.EncodeSequenceOfValue(
                items,
                context.Options.EncodingRules,
                false)
            : constructed.EncodeSetOfValue(
                items,
                context.Options.EncodingRules,
                false);
        if (!root.Success || root.Tlv == null)
        {
            return this.CopyConstructedFailure(root);
        }

        return this.CreateSchemaSuccess(root.Tlv);
    }

    internal BerSchemaEncodeResult EncodeOpenTypeNode(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        BerSchemaEncodeContext context)
    {
        AsnOpenTypeValue? open = value.OpenTypeValue;
        if (open == null || !open.IsValid())
        {
            return this.CreateSchemaError(
                "ASNE0089",
                "An open runtime value has inconsistent metadata.",
                false);
        }

        if (open.Unresolved)
        {
            if (context.Options.UnknownDataPolicy != AsnUnknownDataPolicy.Preserve)
            {
                return this.CreateSchemaError(
                    "ASNE0090",
                    "An unresolved open value requires the preserve policy.",
                    false);
            }

            return this.EncodeUsefulNode(
                AsnTypeKind.OpenType,
                value,
                context);
        }

        if (open.Value == null)
        {
            return this.CreateSchemaError(
                "ASNE0091",
                "A resolved open value has no decoded value.",
                false);
        }

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
                    && this.OpenTypeMatches(open, entry))
                {
                    match = entry;
                    count = count + 1;
                }

                entryIndex = entryIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        if (count != 1 || match == null || match.Type == null)
        {
            return this.CreateSchemaError(
                count > 1 ? "ASNE0092" : "ASNE0093",
                count > 1
                    ? "More than one open-type table entry matches the value."
                    : "No open-type table entry matches the value.",
                false);
        }

        return this.EncodeTypeNode(match.Type, open.Value, context);
    }

    internal bool OpenTypeMatches(
        AsnOpenTypeValue open,
        AsnObjectTableEntryDescriptor entry)
    {
        if (entry.Type == null)
        {
            return false;
        }

        string identity = open.GetResolvedTypeIdentity();
        bool typeMatches = entry.Type.StableId == identity
            || entry.Type.Name == identity
            || entry.Type.GeneratedName == identity;
        if (!typeMatches)
        {
            return false;
        }

        return open.DispatchKey == null
            || entry.Key == null
            || entry.Key.Value == null
            || new RuntimeConstraintEvaluator().RuntimeValueMatchesAsnValue(
                open.DispatchKey,
                entry.Key.Value);
    }

    internal BerSchemaEncodeResult ValidateUnknownData(
        AsnRuntimeValue value,
        BerSchemaEncodeContext context)
    {
        if (value.UnknownExtensions.Length == 0
            && value.UnknownFields.Length == 0)
        {
            return this.CreateSchemaSuccess(new BerTlv());
        }

        if (context.Options.UnknownDataPolicy == AsnUnknownDataPolicy.Skip)
        {
            return this.CreateSchemaSuccess(new BerTlv());
        }

        if (context.Options.UnknownDataPolicy == AsnUnknownDataPolicy.Preserve
            && context.Options.EncodingRules == AsnEncodingRules.Ber)
        {
            return this.CreateSchemaSuccess(new BerTlv());
        }

        return this.CreateSchemaError(
            "ASNE0094",
            "Unknown fields cannot be emitted under the selected policy.",
            false);
    }

    internal BerSchemaEncodeResult MergePreservedUnknownData(
        BerTlv root,
        AsnRuntimeValue value,
        BerSchemaEncodeContext context)
    {
        System.Collections.Generic.List<BerPreservedTlv> preserved =
            new System.Collections.Generic.List<BerPreservedTlv>();
        int extensionIndex = 0;
        while (extensionIndex < value.UnknownExtensions.Length)
        {
            AsnUnknownExtension extension = value.UnknownExtensions[
                extensionIndex];
            if (extension == null || !extension.IsValid())
            {
                return this.CreateSchemaError(
                    "ASNE0099",
                    "A preserved unknown extension has invalid metadata.",
                    false);
            }

            BerSchemaEncodeResult added = this.AddPreservedSlice(
                extension.BinaryData,
                extension.Tag,
                extension.HasBinaryData,
                extension.HasEncodingRules,
                extension.EncodingRules,
                extension.HasEncodedIndex,
                extension.EncodedIndex,
                extension.HasEncodedLocation,
                extension.EncodedOffset,
                extension.EncodedLength,
                context,
                preserved);
            if (!added.Success)
            {
                return added;
            }

            extensionIndex = extensionIndex + 1;
        }

        int fieldIndex = 0;
        while (fieldIndex < value.UnknownFields.Length)
        {
            AsnUnknownField field = value.UnknownFields[fieldIndex];
            if (field == null || !field.IsValid())
            {
                return this.CreateSchemaError(
                    "ASNE0100",
                    "A preserved unknown field has invalid metadata.",
                    false);
            }

            BerSchemaEncodeResult added = this.AddPreservedSlice(
                field.BinaryData,
                field.Tag,
                field.HasBinaryData,
                field.HasEncodingRules,
                field.EncodingRules,
                field.HasEncodedIndex,
                field.EncodedIndex,
                field.HasEncodedLocation,
                field.EncodedOffset,
                field.EncodedLength,
                context,
                preserved);
            if (!added.Success)
            {
                return added;
            }

            fieldIndex = fieldIndex + 1;
        }

        this.SortPreservedSlices(preserved);
        int duplicateIndex = 1;
        while (duplicateIndex < preserved.Count)
        {
            if (preserved[duplicateIndex - 1].EncodedIndex
                == preserved[duplicateIndex].EncodedIndex)
            {
                return this.CreateSchemaError(
                    "ASNE0101",
                    "Preserved unknown values cannot share an encoded position.",
                    false);
            }

            duplicateIndex = duplicateIndex + 1;
        }

        System.Collections.Generic.List<BerTlv> children =
            new System.Collections.Generic.List<BerTlv>(root.Children);
        int index = 0;
        while (index < preserved.Count)
        {
            int position = preserved[index].EncodedIndex;
            if (position > children.Count)
            {
                position = children.Count;
            }

            children.Insert(position, preserved[index].Tlv);
            index = index + 1;
        }

        root.Children = children.ToArray();
        return this.CreateSchemaSuccess(root);
    }

    internal BerSchemaEncodeResult AddPreservedSlice(
        byte[] data,
        AsnTag? tag,
        bool hasBinaryData,
        bool hasEncodingRules,
        AsnEncodingRules rules,
        bool hasEncodedIndex,
        int encodedIndex,
        bool hasEncodedLocation,
        int encodedOffset,
        int encodedLength,
        BerSchemaEncodeContext context,
        System.Collections.Generic.List<BerPreservedTlv> preserved)
    {
        if (!new OpaquePayloadPolicy().CanReuseBinarySlice(
                hasEncodingRules,
                rules,
                context.Options.EncodingRules,
                hasBinaryData,
                data)
            || tag == null
            || !hasEncodedIndex
            || encodedIndex < 0
            || !hasEncodedLocation
            || encodedOffset < 0
            || encodedLength != data.Length)
        {
            return this.CreateSchemaError(
                "ASNE0102",
                "Exact BER re-emission requires a tag, sibling index, byte location, and complete binary slice.",
                false);
        }

        if (!new ResourceCounter(context.Limits).AllowsOpenTypeBytes(
            data.Length))
        {
            return this.CreateSchemaError(
                "ASNR0001",
                "A preserved unknown value exceeds the configured byte limit.",
                true);
        }

        BerReadResult read = new BerTlvReaderImplementation(
            context.Limits).Read(new BerReadRequest
            {
                Data = data,
                HasCount = true,
                Count = data.Length,
                EncodingRules = AsnEncodingRules.Ber,
                PreserveEncodedData = true
            });
        if (read.Status != AsnDecodeStatus.Done
            || read.Tlv == null
            || read.ConsumedCount != data.Length
            || read.Tlv.Tag.Class != tag.Class
            || read.Tlv.Tag.Number != tag.Number)
        {
            return this.CreateSchemaError(
                "ASNE0103",
                "The preserved bytes do not contain the recorded BER value.",
                false);
        }

        read.Tlv.UseEncodedData = true;
        preserved.Add(new BerPreservedTlv
        {
            EncodedIndex = encodedIndex,
            Tlv = read.Tlv
        });
        return this.CreateSchemaSuccess(read.Tlv);
    }

    internal void SortPreservedSlices(
        System.Collections.Generic.List<BerPreservedTlv> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            BerPreservedTlv current = values[index];
            int position = index;
            while (position > 0
                && values[position - 1].EncodedIndex > current.EncodedIndex)
            {
                values[position] = values[position - 1];
                position = position - 1;
            }

            values[position] = current;
            index = index + 1;
        }
    }

    internal BerSchemaFieldLookup FindRuntimeField(
        AsnRuntimeFieldValue[] fields,
        AsnFieldDescriptor descriptor)
    {
        BerSchemaFieldLookup result = new BerSchemaFieldLookup();
        int index = 0;
        while (index < fields.Length)
        {
            AsnRuntimeFieldValue field = fields[index];
            if (field != null
                && field.Name != null
                && (field.Name == descriptor.Name
                    || field.Name == descriptor.GeneratedName
                    || field.Name == descriptor.StableId))
            {
                result.Count = result.Count + 1;
                result.Index = index;
            }

            index = index + 1;
        }

        return result;
    }

    internal BerSchemaEncodeResult RequireAllFieldsConsumed(
        AsnRuntimeFieldValue[] fields,
        bool[] consumed)
    {
        int index = 0;
        while (index < consumed.Length)
        {
            if (fields[index] == null || fields[index].Name == null)
            {
                return this.CreateSchemaError(
                    "ASNE0095",
                    "A runtime field or its name is null.",
                    false);
            }

            if (!consumed[index])
            {
                return this.CreateSchemaError(
                    "ASNE0096",
                    "The runtime field has no schema descriptor: "
                        + fields[index].Name,
                    false);
            }

            index = index + 1;
        }

        return this.CreateSchemaSuccess(new BerTlv());
    }

    internal bool MatchesDefault(
        AsnFieldDescriptor field,
        AsnRuntimeValue value)
    {
        if (!field.HasDefaultValue
            || field.Type == null
            || field.DefaultValue == null
            || field.DefaultValue.Value == null)
        {
            return false;
        }

        ValueAccess access = new ValueAccess();
        AsnRuntimeValue actual = access.NormalizeValue(
            value,
            field.Type,
            this.Catalog);
        AsnRuntimeValue expected = access.FromSchemaValue(
            field.DefaultValue.Value,
            field.Type,
            this.Catalog);
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
        if (!this.OptionalBitStringsEqual(
                left.BitStringValue,
                right.BitStringValue,
                comparison)
            || !this.OptionalObjectIdentifiersEqual(
                left.ObjectIdentifierValue,
                right.ObjectIdentifierValue,
                comparison)
            || !comparison.ByteArraysEqual(left.Octets, right.Octets)
            || !this.OptionalRealValuesEqual(
                left.RealValue,
                right.RealValue,
                comparison)
            || !this.OptionalTimeValuesEqual(
                left.TimeValue,
                right.TimeValue))
        {
            return false;
        }

        if (left.Fields.Length != right.Fields.Length
            || left.Items.Length != right.Items.Length)
        {
            return false;
        }

        int fieldIndex = 0;
        while (fieldIndex < left.Fields.Length)
        {
            AsnRuntimeFieldValue leftField = left.Fields[fieldIndex];
            AsnRuntimeFieldValue rightField = right.Fields[fieldIndex];
            if (leftField == null
                || rightField == null
                || leftField.Name != rightField.Name
                || leftField.Present != rightField.Present
                || leftField.PresentDefault != rightField.PresentDefault
                || leftField.Value == null && rightField.Value != null
                || leftField.Value != null && rightField.Value == null)
            {
                return false;
            }

            if (leftField.Value != null
                && rightField.Value != null
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

        return this.OptionalOpenTypesEqual(
            left.OpenTypeValue,
            right.OpenTypeValue,
            comparison);
    }

    internal bool OptionalBitStringsEqual(
        AsnBitStringValue? left,
        AsnBitStringValue? right,
        RuntimeConstraintEvaluator comparison)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }

        return comparison.BitStringsEqual(left, right);
    }

    internal bool OptionalObjectIdentifiersEqual(
        AsnObjectIdentifierValue? left,
        AsnObjectIdentifierValue? right,
        RuntimeConstraintEvaluator comparison)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }

        return comparison.ObjectIdentifiersEqual(left, right);
    }

    internal bool OptionalRealValuesEqual(
        AsnRealValue? left,
        AsnRealValue? right,
        RuntimeConstraintEvaluator comparison)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }

        return comparison.CompareRealValues(left, right) == 0;
    }

    internal bool OptionalTimeValuesEqual(
        AsnTimeValue? left,
        AsnTimeValue? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }

        return left.Kind == right.Kind && left.Text == right.Text;
    }

    internal bool OptionalOpenTypesEqual(
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

        if (left.Value == null || right.Value == null)
        {
            return left.Value == null && right.Value == null;
        }

        return this.RuntimeValuesEqual(left.Value, right.Value);
    }

    internal AsnRuntimeValue CopyValueWithKind(
        AsnRuntimeValue value,
        AsnRuntimeValueKind kind)
    {
        return new AsnRuntimeValue
        {
            Kind = kind,
            Value = value.Value,
            HasBooleanValue = value.HasBooleanValue,
            BooleanValue = value.BooleanValue,
            HasIntegerValue = value.HasIntegerValue,
            IntegerValue = value.IntegerValue,
            TextValue = value.TextValue,
            BitStringValue = value.BitStringValue,
            Octets = value.Octets,
            ObjectIdentifierValue = value.ObjectIdentifierValue,
            RealValue = value.RealValue,
            TimeValue = value.TimeValue,
            Fields = value.Fields,
            Items = value.Items,
            OpenTypeValue = value.OpenTypeValue,
            UnknownExtensions = value.UnknownExtensions,
            UnknownFields = value.UnknownFields
        };
    }

    internal bool IsPrimitiveType(AsnTypeKind kind)
    {
        return new BerPrimitiveCodec(new AsnResourceLimits())
            .GetUniversalTagNumber(kind) >= 0;
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

    internal BerSchemaEncodeResult CopyPrimitiveFailure(
        BerPrimitiveEncodeResult result)
    {
        if (result.Diagnostics.Length > 0)
        {
            return new BerSchemaEncodeResult
            {
                Diagnostics = result.Diagnostics
            };
        }

        return this.CreateSchemaError(
            "ASNE0097",
            "The BER value could not be encoded.",
            false);
    }

    internal BerSchemaEncodeResult CopyConstructedFailure(
        BerConstructedResult result)
    {
        return this.CreateSchemaError(
            result.ErrorCode,
            result.ErrorMessage,
            result.LimitExceeded);
    }

    internal BerSchemaEncodeResult CreateSchemaSuccess(BerTlv tlv)
    {
        return new BerSchemaEncodeResult
        {
            Success = true,
            Tlv = tlv
        };
    }

    internal BerSchemaEncodeResult CreateSchemaError(
        string code,
        string message,
        bool resource)
    {
        return new BerSchemaEncodeResult
        {
            Diagnostics =
            [
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = resource
                        ? AsnDiagnosticCategory.Resource
                        : AsnDiagnosticCategory.Encoding,
                    Code = code,
                    Message = message
                }
            ]
        };
    }

    internal AsnEncodeResult CreatePublicError(
        string code,
        string message,
        bool resource)
    {
        BerSchemaEncodeResult error = this.CreateSchemaError(
            code,
            message,
            resource);
        return new AsnEncodeResult
        {
            Diagnostics = error.Diagnostics
        };
    }
}
