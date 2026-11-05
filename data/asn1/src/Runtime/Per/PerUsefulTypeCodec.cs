namespace asn1;

internal class PerUsefulTypeCodec
{
    internal AsnResourceLimits Limits;
    internal PerPrimitiveCodec Primitives;
    internal PerAlternativeCodec Alternatives;
    internal PerCollectionCodec Collections;
    internal PerOpenTypeCodec OpenTypes;

    internal PerUsefulTypeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Primitives = new PerPrimitiveCodec(limits);
        this.Alternatives = new PerAlternativeCodec(limits);
        this.Collections = new PerCollectionCodec(limits);
        this.OpenTypes = new PerOpenTypeCodec(limits);
    }

    internal PerUsefulTypeResult WriteAssociatedValue(
        PerBitWriter writer,
        AsnTypeKind kind,
        AsnRuntimeValue value,
        bool aligned)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        string dataName = this.GetAssociatedDataName(kind);
        AsnRuntimeValueKind runtimeKind = this.GetAssociatedRuntimeKind(kind);
        bool supportsDescriptor = kind != AsnTypeKind.CharacterString;
        if (dataName.Length == 0
            || value.Kind != runtimeKind
            || !this.HasOnlyPresentFields(
                value,
                ["identification", "data-value-descriptor", dataName])
            || this.CountPresentField(value, "identification") != 1
            || this.CountPresentField(value, "data-value-descriptor") > 1
            || !supportsDescriptor
                && this.CountPresentField(value, "data-value-descriptor") != 0
            || this.CountPresentField(value, dataName) != 1)
        {
            return this.CreateError(
                "ASNP0072",
                "The associated useful-type value has invalid components.",
                false);
        }

        AsnRuntimeValue? identification = this.GetPresentFieldValue(
            value,
            "identification");
        AsnRuntimeValue? descriptor = this.GetPresentFieldValue(
            value,
            "data-value-descriptor");
        AsnRuntimeValue? data = this.GetPresentFieldValue(value, dataName);
        if (identification == null || data == null
            || data.Kind != AsnRuntimeValueKind.OctetString)
        {
            return this.CreateError(
                "ASNP0072",
                "The associated useful type requires identification and octet data.",
                false);
        }

        if (descriptor != null
            && descriptor.Kind != AsnRuntimeValueKind.CharacterString)
        {
            return this.CreateError(
                "ASNP0072",
                "The data-value-descriptor must be a character string.",
                false);
        }

        int start = writer.GetWrittenBitCount();
        PerCollectionResult preamble = this.Collections.WriteRootPreamble(
            writer,
            false,
            false,
            supportsDescriptor ? [descriptor != null] : []);
        if (!preamble.Success)
        {
            return this.CopyCollectionFailure(preamble);
        }

        PerUsefulTypeResult writtenIdentification = this.WriteIdentification(
            writer,
            identification,
            aligned);
        if (!writtenIdentification.Success)
        {
            return writtenIdentification;
        }

        if (descriptor != null)
        {
            PerPrimitiveResult writtenDescriptor =
                this.Primitives.WriteCharacterString(
                    writer,
                    AsnTypeKind.GraphicString,
                    descriptor.TextValue,
                    aligned);
            if (!writtenDescriptor.Success)
            {
                return this.CopyPrimitiveFailure(writtenDescriptor);
            }
        }

        PerPrimitiveResult writtenData = this.Primitives.WriteOctetString(
            writer,
            data.Octets,
            0,
            false,
            0,
            aligned);
        if (!writtenData.Success)
        {
            return this.CopyPrimitiveFailure(writtenData);
        }

        return new PerUsefulTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerUsefulTypeResult ReadAssociatedValue(
        PerBitReader reader,
        AsnTypeKind kind,
        bool aligned)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        string dataName = this.GetAssociatedDataName(kind);
        AsnRuntimeValueKind runtimeKind = this.GetAssociatedRuntimeKind(kind);
        bool supportsDescriptor = kind != AsnTypeKind.CharacterString;
        if (dataName.Length == 0)
        {
            return this.CreateError(
                "ASNP0072",
                "The requested type has no PER associated type.",
                false);
        }

        int start = reader.Position;
        PerCollectionResult preamble = this.Collections.ReadRootPreamble(
            reader,
            false,
            supportsDescriptor ? 1 : 0);
        if (!preamble.Success)
        {
            return this.HandleCollectionFailure(reader, preamble, start);
        }

        PerUsefulTypeResult identification = this.ReadIdentification(
            reader,
            aligned);
        if (!identification.Success || identification.Value == null)
        {
            if (identification.Status == AsnDecodeStatus.NeedMoreData)
            {
                reader.Position = start;
            }

            return identification;
        }

        System.Collections.Generic.List<AsnRuntimeFieldValue> fields =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        fields.Add(this.CreatePresentField(
            "identification",
            identification.Value));
        if (supportsDescriptor && preamble.RootPresence[0])
        {
            PerPrimitiveResult descriptor =
                this.Primitives.ReadCharacterString(
                    reader,
                    AsnTypeKind.GraphicString,
                    aligned);
            if (!descriptor.Success || descriptor.Value == null)
            {
                return this.HandlePrimitiveFailure(
                    reader,
                    descriptor,
                    start);
            }

            fields.Add(this.CreatePresentField(
                "data-value-descriptor",
                descriptor.Value));
        }

        PerPrimitiveResult data = this.Primitives.ReadOctetString(
            reader,
            0,
            false,
            0,
            aligned);
        if (!data.Success || data.Value == null)
        {
            return this.HandlePrimitiveFailure(reader, data, start);
        }

        fields.Add(this.CreatePresentField(dataName, data.Value));
        return new PerUsefulTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = runtimeKind,
                Fields = fields.ToArray()
            },
            BitCount = reader.Position - start
        };
    }

    internal PerUsefulTypeResult WriteIdentification(
        PerBitWriter writer,
        AsnRuntimeValue value,
        bool aligned)
    {
        string[] names = this.GetIdentificationNames();
        if (value.Kind != AsnRuntimeValueKind.Choice
            || !this.HasOnlyPresentFields(value, names))
        {
            return this.CreateError(
                "ASNP0073",
                "Identification must select one recognized alternative.",
                false);
        }

        int selected = -1;
        int index = 0;
        while (index < names.Length)
        {
            if (this.CountPresentField(value, names[index]) == 1)
            {
                if (selected >= 0)
                {
                    return this.CreateError(
                        "ASNP0073",
                        "Identification must select exactly one alternative.",
                        false);
                }

                selected = index;
            }

            index = index + 1;
        }

        AsnRuntimeValue? selectedValue = selected < 0
            ? null
            : this.GetPresentFieldValue(value, names[selected]);
        if (selectedValue == null)
        {
            return this.CreateError(
                "ASNP0073",
                "Identification must select exactly one valued alternative.",
                false);
        }

        int start = writer.GetWrittenBitCount();
        PerAlternativeResult choice = this.Alternatives.WriteEnumeratedIndex(
            writer,
            false,
            selected,
            names.Length,
            0,
            false,
            aligned);
        if (!choice.Success)
        {
            return this.CopyAlternativeFailure(choice);
        }

        PerUsefulTypeResult payload = this.WriteIdentificationPayload(
            writer,
            selected,
            selectedValue,
            aligned);
        if (!payload.Success)
        {
            return payload;
        }

        return new PerUsefulTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerUsefulTypeResult ReadIdentification(
        PerBitReader reader,
        bool aligned)
    {
        int start = reader.Position;
        string[] names = this.GetIdentificationNames();
        PerAlternativeResult choice = this.Alternatives.ReadEnumeratedIndex(
            reader,
            names.Length,
            0,
            false,
            aligned);
        if (!choice.Success)
        {
            return this.HandleAlternativeFailure(reader, choice, start);
        }

        PerUsefulTypeResult payload = this.ReadIdentificationPayload(
            reader,
            choice.Index,
            aligned,
            start);
        if (!payload.Success || payload.Value == null)
        {
            return payload;
        }

        return new PerUsefulTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Choice,
                Fields = [this.CreatePresentField(
                    names[choice.Index],
                    payload.Value)]
            },
            BitCount = reader.Position - start
        };
    }

    internal PerUsefulTypeResult WriteIdentificationPayload(
        PerBitWriter writer,
        int alternative,
        AsnRuntimeValue value,
        bool aligned)
    {
        if (alternative == 0)
        {
            return this.WriteIdentificationPair(
                writer,
                value,
                "abstract",
                "transfer",
                true,
                aligned);
        }

        if (alternative == 3)
        {
            return this.WriteIdentificationPair(
                writer,
                value,
                "presentation-context-id",
                "transfer-syntax",
                false,
                aligned);
        }

        if (alternative == 1 || alternative == 4)
        {
            if (value.Kind != AsnRuntimeValueKind.ObjectIdentifier
                || value.ObjectIdentifierValue == null)
            {
                return this.CreateError(
                    "ASNP0074",
                    "The identification alternative requires an object identifier.",
                    false);
            }

            return this.CopyPrimitiveSuccess(
                this.Primitives.WriteObjectIdentifier(
                    writer,
                    value.ObjectIdentifierValue,
                    false,
                    aligned),
                value);
        }

        if (alternative == 2)
        {
            if (value.Kind != AsnRuntimeValueKind.Integer
                || !value.HasIntegerValue)
            {
                return this.CreateError(
                    "ASNP0074",
                    "The identification alternative requires an integer.",
                    false);
            }

            return this.CopyPrimitiveSuccess(
                this.Primitives.WriteInteger(
                    writer,
                    value.IntegerValue,
                    false,
                    System.Numerics.BigInteger.Zero,
                    false,
                    System.Numerics.BigInteger.Zero,
                    aligned),
                value);
        }

        if (alternative == 5 && value.Kind == AsnRuntimeValueKind.Null)
        {
            return this.CopyPrimitiveSuccess(
                this.Primitives.WriteNull(writer),
                value);
        }

        return this.CreateError(
            "ASNP0074",
            "The identification alternative has the wrong runtime value.",
            false);
    }

    internal PerUsefulTypeResult ReadIdentificationPayload(
        PerBitReader reader,
        int alternative,
        bool aligned,
        int start)
    {
        if (alternative == 0)
        {
            return this.ReadIdentificationPair(
                reader,
                "abstract",
                "transfer",
                true,
                aligned,
                start);
        }

        if (alternative == 3)
        {
            return this.ReadIdentificationPair(
                reader,
                "presentation-context-id",
                "transfer-syntax",
                false,
                aligned,
                start);
        }

        PerPrimitiveResult decoded;
        if (alternative == 1 || alternative == 4)
        {
            decoded = this.Primitives.ReadObjectIdentifier(
                reader,
                false,
                aligned);
        }
        else if (alternative == 2)
        {
            decoded = this.Primitives.ReadInteger(
                reader,
                false,
                System.Numerics.BigInteger.Zero,
                false,
                System.Numerics.BigInteger.Zero,
                aligned);
        }
        else
        {
            decoded = this.Primitives.ReadNull(reader);
        }

        if (!decoded.Success || decoded.Value == null)
        {
            return this.HandlePrimitiveFailure(reader, decoded, start);
        }

        return new PerUsefulTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = decoded.Value
        };
    }

    internal PerUsefulTypeResult WriteIdentificationPair(
        PerBitWriter writer,
        AsnRuntimeValue value,
        string firstName,
        string secondName,
        bool firstIsObjectIdentifier,
        bool aligned)
    {
        if (value.Kind != AsnRuntimeValueKind.Sequence
            || !this.HasOnlyPresentFields(value, [firstName, secondName])
            || this.CountPresentField(value, firstName) != 1
            || this.CountPresentField(value, secondName) != 1)
        {
            return this.CreateError(
                "ASNP0075",
                "The identification pair requires both components.",
                false);
        }

        AsnRuntimeValue? first = this.GetPresentFieldValue(value, firstName);
        AsnRuntimeValue? second = this.GetPresentFieldValue(value, secondName);
        if (first == null || second == null
            || second.ObjectIdentifierValue == null
            || second.Kind != AsnRuntimeValueKind.ObjectIdentifier)
        {
            return this.CreateError(
                "ASNP0075",
                "The identification pair has invalid component values.",
                false);
        }

        PerPrimitiveResult firstResult;
        if (firstIsObjectIdentifier)
        {
            if (first.Kind != AsnRuntimeValueKind.ObjectIdentifier
                || first.ObjectIdentifierValue == null)
            {
                return this.CreateError(
                    "ASNP0075",
                    "The first identification component requires an object identifier.",
                    false);
            }

            firstResult = this.Primitives.WriteObjectIdentifier(
                writer,
                first.ObjectIdentifierValue,
                false,
                aligned);
        }
        else
        {
            if (first.Kind != AsnRuntimeValueKind.Integer
                || !first.HasIntegerValue)
            {
                return this.CreateError(
                    "ASNP0075",
                    "The first identification component requires an integer.",
                    false);
            }

            firstResult = this.Primitives.WriteInteger(
                writer,
                first.IntegerValue,
                false,
                System.Numerics.BigInteger.Zero,
                false,
                System.Numerics.BigInteger.Zero,
                aligned);
        }

        if (!firstResult.Success)
        {
            return this.CopyPrimitiveFailure(firstResult);
        }

        PerPrimitiveResult secondResult =
            this.Primitives.WriteObjectIdentifier(
                writer,
                second.ObjectIdentifierValue,
                false,
                aligned);
        return this.CopyPrimitiveSuccess(secondResult, value);
    }

    internal PerUsefulTypeResult ReadIdentificationPair(
        PerBitReader reader,
        string firstName,
        string secondName,
        bool firstIsObjectIdentifier,
        bool aligned,
        int start)
    {
        PerPrimitiveResult first = firstIsObjectIdentifier
            ? this.Primitives.ReadObjectIdentifier(reader, false, aligned)
            : this.Primitives.ReadInteger(
                reader,
                false,
                System.Numerics.BigInteger.Zero,
                false,
                System.Numerics.BigInteger.Zero,
                aligned);
        if (!first.Success || first.Value == null)
        {
            return this.HandlePrimitiveFailure(reader, first, start);
        }

        PerPrimitiveResult second = this.Primitives.ReadObjectIdentifier(
            reader,
            false,
            aligned);
        if (!second.Success || second.Value == null)
        {
            return this.HandlePrimitiveFailure(reader, second, start);
        }

        return new PerUsefulTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    this.CreatePresentField(firstName, first.Value),
                    this.CreatePresentField(secondName, second.Value)
                ]
            }
        };
    }

    internal PerUsefulTypeResult WriteInstanceOfValue(
        PerBitWriter writer,
        AsnRuntimeValue value,
        byte[] valueEncoding,
        int valueBitCount,
        bool aligned)
    {
        if (valueEncoding == null)
        {
            throw new System.ArgumentNullException("valueEncoding");
        }

        if (value.Kind != AsnRuntimeValueKind.InstanceOf
            || !this.HasOnlyPresentFields(value, ["type-id", "value"])
            || this.CountPresentField(value, "type-id") != 1
            || this.CountPresentField(value, "value") != 1)
        {
            return this.CreateError(
                "ASNP0076",
                "INSTANCE OF requires type-id and value components.",
                false);
        }

        AsnRuntimeValue? typeId = this.GetPresentFieldValue(value, "type-id");
        if (typeId == null
            || typeId.Kind != AsnRuntimeValueKind.ObjectIdentifier
            || typeId.ObjectIdentifierValue == null)
        {
            return this.CreateError(
                "ASNP0076",
                "INSTANCE OF requires an object-identifier type-id.",
                false);
        }

        PerOpenTypeResult validatedOpen = this.OpenTypes.CompleteEncoding(
            valueEncoding,
            valueBitCount);
        if (!validatedOpen.Success)
        {
            return this.CopyOpenTypeFailure(validatedOpen);
        }

        int start = writer.GetWrittenBitCount();
        PerPrimitiveResult identifier = this.Primitives.WriteObjectIdentifier(
            writer,
            typeId.ObjectIdentifierValue,
            false,
            aligned);
        if (!identifier.Success)
        {
            return this.CopyPrimitiveFailure(identifier);
        }

        PerOpenTypeResult open = this.OpenTypes.WriteOpenType(
            writer,
            valueEncoding,
            valueBitCount,
            aligned);
        if (!open.Success)
        {
            return this.CopyOpenTypeFailure(open);
        }

        return new PerUsefulTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            BitCount = writer.GetWrittenBitCount() - start
        };
    }

    internal PerUsefulTypeResult ReadInstanceOfValue(
        PerBitReader reader,
        bool aligned,
        AsnUnknownDataPolicy policy,
        AsnEncodingRules rules,
        string resolvedTypeId,
        AsnRuntimeValue? decodedValue)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        int start = reader.Position;
        PerPrimitiveResult identifier = this.Primitives.ReadObjectIdentifier(
            reader,
            false,
            aligned);
        if (!identifier.Success || identifier.Value == null)
        {
            return this.HandlePrimitiveFailure(reader, identifier, start);
        }

        PerOpenTypeResult open = this.OpenTypes.ReadOpenType(reader, aligned);
        if (!open.Success)
        {
            if (open.Status == AsnDecodeStatus.NeedMoreData)
            {
                reader.Position = start;
            }

            return this.CopyOpenTypeFailure(open);
        }

        AsnOpenTypeValue openValue;
        if (decodedValue != null && resolvedTypeId.Length > 0)
        {
            openValue = new AsnOpenTypeValue
            {
                Resolved = true,
                ResolvedTypeId = resolvedTypeId,
                TypeName = resolvedTypeId,
                Value = decodedValue,
                DispatchKey = identifier.Value,
                HasEncodingRules = true,
                EncodingRules = rules,
                HasOriginalData = true,
                OriginalData = open.Data
            };
        }
        else
        {
            if (policy == AsnUnknownDataPolicy.Reject)
            {
                return this.CreateError(
                    "ASNP0077",
                    "The INSTANCE OF type-id did not resolve a payload type.",
                    false);
            }

            bool preserve = policy == AsnUnknownDataPolicy.Preserve;
            openValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                DispatchKey = identifier.Value,
                HasEncodingRules = preserve,
                EncodingRules = rules,
                HasOriginalData = preserve,
                OriginalData = preserve ? open.Data : []
            };
        }

        return new PerUsefulTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.InstanceOf,
                Fields =
                [
                    this.CreatePresentField("type-id", identifier.Value),
                    this.CreatePresentField(
                        "value",
                        new AsnRuntimeValue
                        {
                            Kind = AsnRuntimeValueKind.OpenType,
                            OpenTypeValue = openValue
                        })
                ]
            },
            BitCount = reader.Position - start
        };
    }

    internal string GetAssociatedDataName(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.External
            || kind == AsnTypeKind.EmbeddedPdv)
        {
            return "data-value";
        }

        return kind == AsnTypeKind.CharacterString ? "string-value" : "";
    }

    internal AsnRuntimeValueKind GetAssociatedRuntimeKind(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.External)
        {
            return AsnRuntimeValueKind.External;
        }

        if (kind == AsnTypeKind.EmbeddedPdv)
        {
            return AsnRuntimeValueKind.EmbeddedPdv;
        }

        return kind == AsnTypeKind.CharacterString
            ? AsnRuntimeValueKind.CharacterString
            : AsnRuntimeValueKind.Unknown;
    }

    internal string[] GetIdentificationNames()
    {
        return
        [
            "syntaxes",
            "syntax",
            "presentation-context-id",
            "context-negotiation",
            "transfer-syntax",
            "fixed"
        ];
    }

    internal AsnRuntimeValue? GetPresentFieldValue(
        AsnRuntimeValue source,
        string name)
    {
        int index = 0;
        while (index < source.Fields.Length)
        {
            AsnRuntimeFieldValue field = source.Fields[index];
            if (field.Present && field.Name == name && field.Value != null)
            {
                return field.Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal int CountPresentField(AsnRuntimeValue source, string name)
    {
        int count = 0;
        int index = 0;
        while (index < source.Fields.Length)
        {
            if (source.Fields[index].Present
                && source.Fields[index].Name == name)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool HasOnlyPresentFields(
        AsnRuntimeValue source,
        string[] names)
    {
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            source.Fields.Length))
        {
            return false;
        }

        int fieldIndex = 0;
        while (fieldIndex < source.Fields.Length)
        {
            AsnRuntimeFieldValue field = source.Fields[fieldIndex];
            if (field.Present)
            {
                bool found = false;
                int nameIndex = 0;
                while (nameIndex < names.Length)
                {
                    if (field.Name == names[nameIndex])
                    {
                        found = true;
                    }

                    nameIndex = nameIndex + 1;
                }

                if (!found || field.Value == null)
                {
                    return false;
                }
            }

            fieldIndex = fieldIndex + 1;
        }

        return true;
    }

    internal AsnRuntimeFieldValue CreatePresentField(
        string name,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeFieldValue
        {
            Name = name,
            Present = true,
            Value = value
        };
    }

    internal PerUsefulTypeResult CopyPrimitiveSuccess(
        PerPrimitiveResult source,
        AsnRuntimeValue value)
    {
        if (!source.Success)
        {
            return this.CopyPrimitiveFailure(source);
        }

        return new PerUsefulTypeResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            BitCount = source.BitCount
        };
    }

    internal PerUsefulTypeResult HandlePrimitiveFailure(
        PerBitReader reader,
        PerPrimitiveResult source,
        int start)
    {
        if (source.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
        }

        return this.CopyPrimitiveFailure(source);
    }

    internal PerUsefulTypeResult HandleAlternativeFailure(
        PerBitReader reader,
        PerAlternativeResult source,
        int start)
    {
        if (source.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
        }

        return this.CopyAlternativeFailure(source);
    }

    internal PerUsefulTypeResult HandleCollectionFailure(
        PerBitReader reader,
        PerCollectionResult source,
        int start)
    {
        if (source.Status == AsnDecodeStatus.NeedMoreData)
        {
            reader.Position = start;
        }

        return this.CopyCollectionFailure(source);
    }

    internal PerUsefulTypeResult CopyPrimitiveFailure(PerPrimitiveResult source)
    {
        return new PerUsefulTypeResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerUsefulTypeResult CopyAlternativeFailure(
        PerAlternativeResult source)
    {
        return new PerUsefulTypeResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerUsefulTypeResult CopyCollectionFailure(
        PerCollectionResult source)
    {
        return new PerUsefulTypeResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerUsefulTypeResult CopyOpenTypeFailure(PerOpenTypeResult source)
    {
        return new PerUsefulTypeResult
        {
            Status = source.Status,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal PerUsefulTypeResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerUsefulTypeResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
