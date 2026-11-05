namespace asn1;

internal class PerSchemaCodec
{
    internal AsnReflectionCatalog Catalog;
    internal AsnEncodingRules Rules;
    internal bool Aligned;
    internal bool Canonical;

    internal PerSchemaCodec(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules)
    {
        this.Catalog = catalog == null
            ? throw new System.ArgumentNullException("catalog")
            : catalog;
        if (rules != AsnEncodingRules.Aper
            && rules != AsnEncodingRules.Uper
            && rules != AsnEncodingRules.CanonicalAlignedPer
            && rules != AsnEncodingRules.CanonicalUnalignedPer)
        {
            throw new System.ArgumentOutOfRangeException("rules");
        }

        this.Rules = rules;
        this.Aligned = rules == AsnEncodingRules.Aper
            || rules == AsnEncodingRules.CanonicalAlignedPer;
        this.Canonical = rules == AsnEncodingRules.CanonicalAlignedPer
            || rules == AsnEncodingRules.CanonicalUnalignedPer;
    }

    internal AsnEncodeResult EncodeRequest(AsnEncodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Value == null)
        {
            return this.CreatePublicEncodeError(
                "ASNP0080",
                "A BASIC-PER encode request requires a runtime value.",
                false);
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = this.Rules }
            : request.Options;
        if (options.EncodingRules != this.Rules)
        {
            return this.CreatePublicEncodeError(
                "ASNP0081",
                "The requested BASIC-PER rules do not match this codec.",
                false);
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreatePublicEncodeError(
                "ASNP0082",
                "The requested schema type is missing or ambiguous.",
                false);
        }

        if (options.ValidateValues)
        {
            AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
                .ValidateRuntimeValue(
                    request.Value,
                    type,
                    options.ResourceLimits == null
                        ? new AsnResourceLimits()
                        : options.ResourceLimits);
            if (diagnostics.Length > 0)
            {
                return new AsnEncodeResult { Diagnostics = diagnostics };
            }
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        PerBitWriter writer = new PerBitWriter(limits);
        PerSchemaResult encoded = this.EncodeType(
            type,
            request.Value,
            writer,
            options,
            0);
        if (!encoded.Success)
        {
            return this.CopyPublicEncodeFailure(encoded);
        }

        PerBitWriteResult alignment = writer.AlignOutput();
        if (!alignment.Success)
        {
            return this.CopyPublicEncodeWriterFailure(writer);
        }

        PerBitWriteResult output = writer.GetOutput();
        if (!output.Success)
        {
            return this.CopyPublicEncodeWriterFailure(writer);
        }

        return new AsnEncodeResult
        {
            Success = true,
            Data = output.Data,
            WrittenCount = output.Data.Length
        };
    }

    internal AsnDecodeResult DecodeRequest(AsnDecodeRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Data == null)
        {
            throw new System.ArgumentException(
                "The BASIC-PER decode request contains null data.",
                "request");
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = this.Rules }
            : request.Options;
        if (options.EncodingRules != this.Rules)
        {
            return this.CreatePublicDecodeError(
                "ASNP0081",
                "The requested BASIC-PER rules do not match this codec.",
                false);
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreatePublicDecodeError(
                "ASNP0082",
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
        PerBitReader reader = new PerBitReader(limits);
        PerBitReadResult appended = reader.AppendInputBits(
            request.Data,
            request.Offset,
            count * 8,
            true);
        if (appended.Status != AsnDecodeStatus.Done)
        {
            return this.CopyPublicDecodeReaderFailure(reader, count);
        }

        PerSchemaResult decoded = this.DecodeType(
            type,
            reader,
            options,
            0);
        if (!decoded.Success)
        {
            return this.CopyPublicDecodeFailure(decoded, count, reader);
        }

        PerBitReadResult complete = reader.CompleteInput(true);
        if (complete.Status != AsnDecodeStatus.Done)
        {
            return this.CopyPublicDecodeReaderFailure(reader, count);
        }

        if (decoded.Value != null && options.ValidateValues)
        {
            AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
                .ValidateRuntimeValue(
                    decoded.Value,
                    type,
                    options.ResourceLimits == null
                        ? new AsnResourceLimits()
                        : options.ResourceLimits);
            if (diagnostics.Length > 0)
            {
                return new AsnDecodeResult
                {
                    Status = AsnDecodeStatus.Error,
                    ConsumedCount = count,
                    Diagnostics = diagnostics
                };
            }
        }

        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Done,
            Value = decoded.Value,
            ConsumedCount = count
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

        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Value == null)
        {
            return this.CreatePublicEncodeError(
                "ASNP0080",
                "A BASIC-PER encode request requires a runtime value.",
                false);
        }

        AsnCodecOptions options = request.Options == null
            ? new AsnCodecOptions { EncodingRules = this.Rules }
            : request.Options;
        if (options.EncodingRules != this.Rules)
        {
            return this.CreatePublicEncodeError(
                "ASNP0081",
                "The requested BASIC-PER rules do not match this codec.",
                false);
        }

        AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
            .ResolveRequestedType(request.TypeName);
        if (type == null)
        {
            return this.CreatePublicEncodeError(
                "ASNP0082",
                "The requested schema type is missing or ambiguous.",
                false);
        }

        if (options.ValidateValues)
        {
            AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
                .ValidateRuntimeValue(
                    request.Value,
                    type,
                    options.ResourceLimits == null
                        ? new AsnResourceLimits()
                        : options.ResourceLimits);
            if (diagnostics.Length > 0)
            {
                return new AsnEncodeResult { Diagnostics = diagnostics };
            }
        }

        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        PerBitWriter writer = new PerBitWriter(limits);
        writer.SetOutputStream(output);
        PerSchemaResult encoded = this.EncodeType(
            type,
            request.Value,
            writer,
            options,
            0);
        if (!encoded.Success)
        {
            return this.CreateStreamingFailure(
                encoded.ErrorCode,
                encoded.ErrorMessage,
                encoded.LimitExceeded,
                writer);
        }

        PerBitWriteResult alignment = writer.AlignOutput();
        if (!alignment.Success)
        {
            return this.CreateStreamingFailure(
                writer.ErrorCode,
                writer.ErrorMessage,
                writer.LimitExceeded,
                writer);
        }

        int writtenCount = writer.GetEmittedByteCount();
        return new AsnEncodeResult
        {
            Success = true,
            WrittenCount = writtenCount,
            StreamProgress = new AsnStreamProgress
            {
                WrittenCount = writtenCount,
                HasExpectedCount = true,
                ExpectedCount = writtenCount,
                Complete = true
            }
        };
    }

    internal PerSchemaResult EncodeType(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth)
    {
        return this.EncodeTypeBody(
            type,
            value,
            writer,
            options,
            depth,
            true);
    }

    internal PerSchemaResult EncodeTypeBody(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth,
        bool applyInstructions)
    {
        if (!this.AllowsDepth(options, depth))
        {
            return this.CreateError(
                "ASNR0002",
                "BASIC-PER encoding exceeds the configured nesting limit.",
                true);
        }

        if (applyInstructions)
        {
            PerSchemaResult instructions = this.ApplyInstructionHooks(
                type,
                options);
            if (!instructions.Success)
            {
                return instructions;
            }
        }

        if (type.Kind == AsnTypeKind.Boolean)
        {
            return this.CopyPrimitiveWrite(new PerPrimitiveCodec(
                writer.Limits).WriteBoolean(writer, value.BooleanValue));
        }

        if (type.Kind == AsnTypeKind.Null)
        {
            return this.CopyPrimitiveWrite(new PerPrimitiveCodec(
                writer.Limits).WriteNull(writer));
        }

        if (type.Kind == AsnTypeKind.Integer)
        {
            return this.EncodeInteger(type, value, writer);
        }

        if (type.Kind == AsnTypeKind.Enumerated)
        {
            return this.EncodeEnumerated(type, value, writer);
        }

        if (type.Kind == AsnTypeKind.OctetString)
        {
            return this.EncodeOctetString(type, value, writer);
        }

        if (type.Kind == AsnTypeKind.BitString)
        {
            return this.EncodeBitString(type, value, writer);
        }

        if (type.Kind == AsnTypeKind.Real)
        {
            if (value.RealValue == null)
            {
                return this.CreateValueError("REAL");
            }

            return this.CopyPrimitiveWrite(new PerPrimitiveCodec(
                writer.Limits).WriteReal(
                    writer,
                    value.RealValue,
                    this.Aligned));
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifier
            || type.Kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            if (value.ObjectIdentifierValue == null)
            {
                return this.CreateValueError("OBJECT IDENTIFIER");
            }

            return this.CopyPrimitiveWrite(new PerPrimitiveCodec(
                writer.Limits).WriteObjectIdentifier(
                    writer,
                    value.ObjectIdentifierValue,
                    type.Kind == AsnTypeKind.RelativeObjectIdentifier,
                    this.Aligned));
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return this.CopyPrimitiveWrite(new PerPrimitiveCodec(
                writer.Limits).WriteIri(
                    writer,
                    value.TextValue,
                    type.Kind == AsnTypeKind.RelativeObjectIdentifierIri,
                    this.Aligned));
        }

        if (this.IsKnownMultiplierString(type.Kind))
        {
            return this.EncodeKnownString(type, value, writer);
        }

        if (this.IsBaseEncodedString(type.Kind))
        {
            return this.CopyStringWrite(new PerStringCodec(
                writer.Limits).WriteBaseEncodedString(
                    writer,
                    type.Kind,
                    value.TextValue,
                    this.Aligned));
        }

        if (this.IsTimeType(type.Kind))
        {
            if (value.TimeValue == null)
            {
                return this.CreateValueError("time");
            }

            return this.CopyPrimitiveWrite(new PerPrimitiveCodec(
                writer.Limits).WriteTime(
                    writer,
                    type.Kind,
                    value.TimeValue,
                    this.Aligned));
        }

        if (type.Kind == AsnTypeKind.Sequence
            || type.Kind == AsnTypeKind.Set)
        {
            return this.EncodeCollection(type, value, writer, options, depth);
        }

        if (type.Kind == AsnTypeKind.Choice)
        {
            return this.EncodeChoice(type, value, writer, options, depth);
        }

        if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            return this.EncodeCollectionOf(type, value, writer, options, depth);
        }

        if (type.Kind == AsnTypeKind.External
            || type.Kind == AsnTypeKind.EmbeddedPdv
            || type.Kind == AsnTypeKind.CharacterString)
        {
            return this.CopyUsefulWrite(new PerUsefulTypeCodec(
                writer.Limits).WriteAssociatedValue(
                    writer,
                    type.Kind,
                    value,
                    this.Aligned));
        }

        if (type.Kind == AsnTypeKind.InstanceOf)
        {
            return this.EncodeInstanceOf(value, writer, options, depth);
        }

        if (type.Kind == AsnTypeKind.OpenType)
        {
            return this.EncodeOpenType(value, writer, options, depth);
        }

        return this.CreateError(
            "ASNP0083",
            "The schema type is not supported by BASIC-PER encoding: "
                + type.Kind,
            false);
    }

    internal PerSchemaResult DecodeType(
        AsnTypeDescriptor type,
        PerBitReader reader,
        AsnCodecOptions options,
        int depth)
    {
        return this.DecodeTypeBody(
            type,
            reader,
            options,
            depth,
            true);
    }

    internal PerSchemaResult DecodeTypeBody(
        AsnTypeDescriptor type,
        PerBitReader reader,
        AsnCodecOptions options,
        int depth,
        bool applyInstructions)
    {
        if (!this.AllowsDepth(options, depth))
        {
            return this.CreateError(
                "ASNR0002",
                "BASIC-PER decoding exceeds the configured nesting limit.",
                true);
        }

        if (applyInstructions)
        {
            PerSchemaResult instructions = this.ApplyInstructionHooks(
                type,
                options);
            if (!instructions.Success)
            {
                return instructions;
            }
        }

        if (type.Kind == AsnTypeKind.Boolean)
        {
            return this.CopyPrimitiveRead(new PerPrimitiveCodec(
                reader.Limits).ReadBoolean(reader));
        }

        if (type.Kind == AsnTypeKind.Null)
        {
            return this.CopyPrimitiveRead(new PerPrimitiveCodec(
                reader.Limits).ReadNull(reader));
        }

        if (type.Kind == AsnTypeKind.Integer)
        {
            return this.DecodeInteger(type, reader);
        }

        if (type.Kind == AsnTypeKind.Enumerated)
        {
            return this.DecodeEnumerated(type, reader);
        }

        if (type.Kind == AsnTypeKind.OctetString)
        {
            return this.DecodeOctetString(type, reader);
        }

        if (type.Kind == AsnTypeKind.BitString)
        {
            return this.DecodeBitString(type, reader);
        }

        if (type.Kind == AsnTypeKind.Real)
        {
            return this.CopyPrimitiveRead(new PerPrimitiveCodec(
                reader.Limits).ReadReal(reader, this.Aligned));
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifier
            || type.Kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return this.CopyPrimitiveRead(new PerPrimitiveCodec(
                reader.Limits).ReadObjectIdentifier(
                    reader,
                    type.Kind == AsnTypeKind.RelativeObjectIdentifier,
                    this.Aligned));
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return this.CopyPrimitiveRead(new PerPrimitiveCodec(
                reader.Limits).ReadIri(
                    reader,
                    type.Kind == AsnTypeKind.RelativeObjectIdentifierIri,
                    this.Aligned));
        }

        if (this.IsKnownMultiplierString(type.Kind))
        {
            return this.DecodeKnownString(type, reader);
        }

        if (this.IsBaseEncodedString(type.Kind))
        {
            PerStringResult decoded = new PerStringCodec(
                reader.Limits).ReadBaseEncodedString(
                    reader,
                    type.Kind,
                    this.Aligned);
            if (!decoded.Success)
            {
                return this.CopyStringRead(decoded);
            }

            return this.CreateValueSuccess(new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = decoded.Text
            });
        }

        if (this.IsTimeType(type.Kind))
        {
            return this.CopyPrimitiveRead(new PerPrimitiveCodec(
                reader.Limits).ReadTime(reader, type.Kind, this.Aligned));
        }

        if (type.Kind == AsnTypeKind.Sequence
            || type.Kind == AsnTypeKind.Set)
        {
            return this.DecodeCollection(type, reader, options, depth);
        }

        if (type.Kind == AsnTypeKind.Choice)
        {
            return this.DecodeChoice(type, reader, options, depth);
        }

        if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            return this.DecodeCollectionOf(type, reader, options, depth);
        }

        if (type.Kind == AsnTypeKind.External
            || type.Kind == AsnTypeKind.EmbeddedPdv
            || type.Kind == AsnTypeKind.CharacterString)
        {
            return this.CopyUsefulRead(new PerUsefulTypeCodec(
                reader.Limits).ReadAssociatedValue(
                    reader,
                    type.Kind,
                    this.Aligned));
        }

        if (type.Kind == AsnTypeKind.InstanceOf)
        {
            return this.DecodeInstanceOf(reader, options);
        }

        if (type.Kind == AsnTypeKind.OpenType)
        {
            return this.DecodeOpenType(reader, options);
        }

        return this.CreateError(
            "ASNP0083",
            "The schema type is not supported by BASIC-PER decoding: "
                + type.Kind,
            false);
    }

    internal PerSchemaResult EncodeInteger(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        PerBitWriter writer)
    {
        if (!value.HasIntegerValue)
        {
            return this.CreateValueError("INTEGER");
        }

        PerSchemaBounds bounds = this.FindBounds(type, AsnTypeKind.Integer);
        bool extension = bounds.Extensible
            && (!bounds.HasLowerBound || value.IntegerValue < bounds.LowerBound
                || !bounds.HasUpperBound || value.IntegerValue > bounds.UpperBound);
        if (bounds.Extensible)
        {
            PerBitWriteResult marker = writer.WriteBit(extension);
            if (!marker.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        return this.CopyPrimitiveWrite(new PerPrimitiveCodec(
            writer.Limits).WriteInteger(
                writer,
                value.IntegerValue,
                !extension && bounds.HasLowerBound,
                bounds.LowerBound,
                !extension && bounds.HasUpperBound,
                bounds.UpperBound,
                this.Aligned));
    }

    internal PerSchemaResult DecodeInteger(
        AsnTypeDescriptor type,
        PerBitReader reader)
    {
        PerSchemaBounds bounds = this.FindBounds(type, AsnTypeKind.Integer);
        bool extension = false;
        if (bounds.Extensible)
        {
            PerBitReadResult marker = reader.ReadBit();
            if (marker.Status != AsnDecodeStatus.Done)
            {
                return this.CopyReaderFailure(reader);
            }

            extension = marker.Value != System.Numerics.BigInteger.Zero;
        }

        return this.CopyPrimitiveRead(new PerPrimitiveCodec(
            reader.Limits).ReadInteger(
                reader,
                !extension && bounds.HasLowerBound,
                bounds.LowerBound,
                !extension && bounds.HasUpperBound,
                bounds.UpperBound,
                this.Aligned));
    }

    internal PerSchemaResult EncodeEnumerated(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        PerBitWriter writer)
    {
        AsnValueDescriptor[] roots = type.RootNamedValues.Length == 0
            ? type.NamedValues
            : type.RootNamedValues;
        int rootIndex = this.FindNamedValue(roots, value);
        int extensionIndex = this.FindNamedValue(
            type.ExtensionNamedValues,
            value);
        bool extension = rootIndex < 0 && extensionIndex >= 0;
        int index = extension ? extensionIndex : rootIndex;
        if (index < 0)
        {
            return this.CreateValueError("ENUMERATED");
        }

        return this.CopyAlternativeWrite(new PerAlternativeCodec(
            writer.Limits).WriteEnumeratedIndex(
                writer,
                extension,
                index,
                roots.Length,
                type.ExtensionNamedValues.Length,
                type.Extensible,
                this.Aligned));
    }

    internal PerSchemaResult DecodeEnumerated(
        AsnTypeDescriptor type,
        PerBitReader reader)
    {
        AsnValueDescriptor[] roots = type.RootNamedValues.Length == 0
            ? type.NamedValues
            : type.RootNamedValues;
        PerAlternativeResult decoded = new PerAlternativeCodec(
            reader.Limits).ReadEnumeratedIndex(
                reader,
                roots.Length,
                type.ExtensionNamedValues.Length,
                type.Extensible,
                this.Aligned);
        if (!decoded.Success)
        {
            return this.CopyAlternativeRead(decoded);
        }

        AsnValueDescriptor descriptor = decoded.Extension
            ? type.ExtensionNamedValues[decoded.Index]
            : roots[decoded.Index];
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Enumerated,
            TextValue = descriptor.Name
        };
        if (descriptor.Value != null && descriptor.Value.HasIntegerValue)
        {
            value.HasIntegerValue = true;
            value.IntegerValue = descriptor.Value.IntegerValue;
        }

        return this.CreateValueSuccess(value);
    }

    internal PerSchemaResult EncodeOctetString(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        PerBitWriter writer)
    {
        PerSchemaBounds bounds = this.FindBounds(type, AsnTypeKind.OctetString);
        bool extension = this.IsSizeExtension(bounds, value.Octets.Length);
        if (bounds.Extensible)
        {
            PerBitWriteResult marker = writer.WriteBit(extension);
            if (!marker.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        return this.CopyPrimitiveWrite(new PerPrimitiveCodec(
            writer.Limits).WriteOctetString(
                writer,
                value.Octets,
                extension ? 0 : this.GetLowerInt(bounds),
                !extension && bounds.HasUpperBound,
                extension ? 0 : this.GetUpperInt(bounds),
                this.Aligned));
    }

    internal PerSchemaResult DecodeOctetString(
        AsnTypeDescriptor type,
        PerBitReader reader)
    {
        PerSchemaBounds bounds = this.FindBounds(type, AsnTypeKind.OctetString);
        bool extension = this.ReadExtensionMarker(bounds, reader);
        if (reader.Failed)
        {
            return this.CopyReaderFailure(reader);
        }

        return this.CopyPrimitiveRead(new PerPrimitiveCodec(
            reader.Limits).ReadOctetString(
                reader,
                extension ? 0 : this.GetLowerInt(bounds),
                !extension && bounds.HasUpperBound,
                extension ? 0 : this.GetUpperInt(bounds),
                this.Aligned));
    }

    internal PerSchemaResult EncodeBitString(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        PerBitWriter writer)
    {
        if (value.BitStringValue == null)
        {
            return this.CreateValueError("BIT STRING");
        }

        PerSchemaBounds bounds = this.FindBounds(type, AsnTypeKind.BitString);
        bool extension = this.IsSizeExtension(
            bounds,
            value.BitStringValue.BitLength);
        if (bounds.Extensible)
        {
            PerBitWriteResult marker = writer.WriteBit(extension);
            if (!marker.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        return this.CopyPrimitiveWrite(new PerPrimitiveCodec(
            writer.Limits).WriteBitString(
                writer,
                value.BitStringValue,
                extension ? 0 : this.GetLowerInt(bounds),
                !extension && bounds.HasUpperBound,
                extension ? 0 : this.GetUpperInt(bounds),
                this.Aligned));
    }

    internal PerSchemaResult DecodeBitString(
        AsnTypeDescriptor type,
        PerBitReader reader)
    {
        PerSchemaBounds bounds = this.FindBounds(type, AsnTypeKind.BitString);
        bool extension = this.ReadExtensionMarker(bounds, reader);
        if (reader.Failed)
        {
            return this.CopyReaderFailure(reader);
        }

        return this.CopyPrimitiveRead(new PerPrimitiveCodec(
            reader.Limits).ReadBitString(
                reader,
                extension ? 0 : this.GetLowerInt(bounds),
                !extension && bounds.HasUpperBound,
                extension ? 0 : this.GetUpperInt(bounds),
                this.Aligned));
    }

    internal PerSchemaResult EncodeKnownString(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        PerBitWriter writer)
    {
        PerSchemaBounds bounds = this.FindBounds(type, type.Kind);
        return this.CopyStringWrite(new PerStringCodec(
            writer.Limits).WriteKnownMultiplierString(
                writer,
                type.Kind,
                value.TextValue,
                this.GetLowerInt(bounds),
                bounds.HasUpperBound,
                this.GetUpperInt(bounds),
                null,
                bounds.Extensible,
                this.Aligned));
    }

    internal PerSchemaResult DecodeKnownString(
        AsnTypeDescriptor type,
        PerBitReader reader)
    {
        PerSchemaBounds bounds = this.FindBounds(type, type.Kind);
        PerStringResult decoded = new PerStringCodec(
            reader.Limits).ReadKnownMultiplierString(
                reader,
                type.Kind,
                this.GetLowerInt(bounds),
                bounds.HasUpperBound,
                this.GetUpperInt(bounds),
                null,
                bounds.Extensible,
                this.Aligned);
        if (!decoded.Success)
        {
            return this.CopyStringRead(decoded);
        }

        return this.CreateValueSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = decoded.Text
        });
    }

    internal PerSchemaResult EncodeCollection(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth)
    {
        PerCollectionCodec collections = new PerCollectionCodec(writer.Limits);
        AsnFieldDescriptor[] rootFields = this.GetCollectionRootFields(type);
        PerCollectionResult rootPresence = collections.BuildRootPresence(
            rootFields,
            value.Fields,
            this.Catalog);
        if (!rootPresence.Success)
        {
            return this.CopyCollectionWrite(rootPresence);
        }

        int[] additionIndexes = this.GetAdditionIndexes(type.ExtensionFields);
        int additionCount = this.GetAdditionCount(additionIndexes);
        bool[] componentPresence = new bool[type.ExtensionFields.Length];
        int index = 0;
        while (index < componentPresence.Length)
        {
            componentPresence[index] = this.IsFieldPresent(
                type.ExtensionFields[index],
                value.Fields);
            index = index + 1;
        }

        PerCollectionResult extensionPresence = collections.BuildExtensionPresence(
            componentPresence,
            additionIndexes,
            additionCount);
        if (!extensionPresence.Success)
        {
            return this.CopyCollectionWrite(extensionPresence);
        }

        PerSchemaResult unknownValidation = this.ValidateUnknownExtensions(
            value.UnknownExtensions,
            options,
            additionCount,
            type.Extensible);
        if (!unknownValidation.Success)
        {
            return unknownValidation;
        }

        if (options.UnknownDataPolicy == AsnUnknownDataPolicy.Preserve
            && value.UnknownExtensions.Length > 0)
        {
            additionCount = this.GetPreservedAdditionCount(
                value.UnknownExtensions,
                additionCount);
            extensionPresence.ExtensionPresence = this.MergeUnknownPresence(
                extensionPresence.ExtensionPresence,
                value.UnknownExtensions,
                additionCount);
            extensionPresence.ExtensionPresent = true;
        }

        if (this.Canonical && extensionPresence.ExtensionPresent)
        {
            extensionPresence.ExtensionPresence =
                this.TrimExtensionPresence(
                    extensionPresence.ExtensionPresence);
            additionCount = extensionPresence.ExtensionPresence.Length;
        }

        PerCollectionResult preamble = collections.WriteRootPreamble(
            writer,
            type.Extensible,
            extensionPresence.ExtensionPresent,
            rootPresence.RootPresence);
        if (!preamble.Success)
        {
            return this.CopyCollectionWrite(preamble);
        }

        PerSchemaResult roots = this.EncodeRootFields(
            rootFields,
            value.Fields,
            rootPresence.RootPresence,
            writer,
            options,
            depth + 1);
        if (!roots.Success)
        {
            return roots;
        }

        if (extensionPresence.ExtensionPresent)
        {
            byte[][] data = new byte[additionCount][];
            int[] bitCounts = new int[additionCount];
            index = 0;
            while (index < additionCount)
            {
                data[index] = [];
                if (extensionPresence.ExtensionPresence[index])
                {
                    AsnUnknownExtension? unknown = this.FindUnknownExtension(
                        value.UnknownExtensions,
                        index);
                    if (unknown != null
                        && options.UnknownDataPolicy
                            == AsnUnknownDataPolicy.Preserve)
                    {
                        data[index] = unknown.BinaryData;
                        bitCounts[index] = unknown.BinaryData.Length * 8;
                    }
                    else
                    {
                        PerSchemaResult addition = this.EncodeAdditionPayload(
                            type.ExtensionFields,
                            additionIndexes,
                            index,
                            value.Fields,
                            options,
                            depth + 1,
                            writer.Limits);
                        if (!addition.Success)
                        {
                            return addition;
                        }

                        data[index] = addition.Data;
                        bitCounts[index] = addition.BitCount;
                    }
                }

                index = index + 1;
            }

            PerCollectionResult extensions = collections.WriteExtensionSection(
                writer,
                extensionPresence.ExtensionPresence,
                data,
                bitCounts,
                this.Aligned);
            if (!extensions.Success)
            {
                return this.CopyCollectionWrite(extensions);
            }
        }

        return this.CreateWriteSuccess(writer);
    }

    internal PerSchemaResult DecodeCollection(
        AsnTypeDescriptor type,
        PerBitReader reader,
        AsnCodecOptions options,
        int depth)
    {
        PerCollectionCodec collections = new PerCollectionCodec(reader.Limits);
        AsnFieldDescriptor[] rootFields = this.GetCollectionRootFields(type);
        int optionalCount = this.CountOptionalFields(rootFields);
        PerCollectionResult preamble = collections.ReadRootPreamble(
            reader,
            type.Extensible,
            optionalCount);
        if (!preamble.Success)
        {
            return this.CopyCollectionRead(preamble);
        }

        System.Collections.Generic.List<AsnRuntimeFieldValue> fields =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        System.Collections.Generic.List<AsnUnknownExtension> unknown =
            new System.Collections.Generic.List<AsnUnknownExtension>();
        PerSchemaResult roots = this.DecodeRootFields(
            rootFields,
            preamble.RootPresence,
            fields,
            reader,
            options,
            depth + 1);
        if (!roots.Success)
        {
            return roots;
        }

        if (preamble.ExtensionPresent)
        {
            PerCollectionResult extensions = collections.ReadExtensionSection(
                reader,
                this.Aligned);
            if (!extensions.Success)
            {
                return this.CopyCollectionRead(extensions);
            }

            if (this.Canonical
                && !extensions.ExtensionPresence[
                    extensions.ExtensionPresence.Length - 1])
            {
                return this.CreateError(
                    "ASNP0094",
                    "Canonical PER extension bitmaps cannot end with an absent addition.",
                    false);
            }

            int[] additionIndexes = this.GetAdditionIndexes(
                type.ExtensionFields);
            int knownAdditionCount = this.GetAdditionCount(additionIndexes);
            int index = 0;
            while (index < extensions.ExtensionPresence.Length)
            {
                if (extensions.ExtensionPresence[index]
                    && index < knownAdditionCount)
                {
                    PerSchemaResult addition = this.DecodeAdditionPayload(
                        type.ExtensionFields,
                        additionIndexes,
                        index,
                        extensions.ExtensionData[index],
                        fields,
                        options,
                        depth + 1,
                        reader.Limits);
                    if (!addition.Success)
                    {
                        return addition;
                    }
                }
                else if (extensions.ExtensionPresence[index]
                    && options.UnknownDataPolicy == AsnUnknownDataPolicy.Reject)
                {
                    return this.CreateError(
                        "ASNP0084",
                        "The PER value contains an unknown extension addition.",
                        false);
                }
                else if (extensions.ExtensionPresence[index]
                    && options.UnknownDataPolicy == AsnUnknownDataPolicy.Preserve)
                {
                    unknown.Add(new AsnUnknownExtension
                    {
                        Index = index,
                        Present = true,
                        HasEncodedIndex = true,
                        EncodedIndex = index,
                        HasEncodedBitLocation = true,
                        EncodedBitOffset = extensions.ExtensionBitOffsets[index],
                        EncodedBitLength = extensions.ExtensionBitCounts[index],
                        HasAlignmentPaddingBitCount = true,
                        AlignmentPaddingBitCount =
                            extensions.ExtensionAlignmentPaddingBitCounts[index],
                        EncodingRules = this.Rules,
                        HasEncodingRules = true,
                        HasBinaryData = true,
                        BinaryData = extensions.ExtensionData[index]
                    });
                }
                else if (!extensions.ExtensionPresence[index]
                    && index >= knownAdditionCount
                    && options.UnknownDataPolicy == AsnUnknownDataPolicy.Preserve)
                {
                    unknown.Add(new AsnUnknownExtension
                    {
                        Index = index,
                        Present = false,
                        HasEncodedIndex = true,
                        EncodedIndex = index,
                        EncodingRules = this.Rules,
                        HasEncodingRules = true
                    });
                }

                index = index + 1;
            }
        }

        return this.CreateValueSuccess(new AsnRuntimeValue
        {
            Kind = type.Kind == AsnTypeKind.Set
                ? AsnRuntimeValueKind.Set
                : AsnRuntimeValueKind.Sequence,
            Fields = fields.ToArray(),
            UnknownExtensions = unknown.ToArray()
        });
    }

    internal PerSchemaResult EncodeFieldType(
        AsnFieldDescriptor field,
        AsnRuntimeValue value,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth)
    {
        if (field.Type == null)
        {
            return this.CreateMissingFieldType(field.Name);
        }

        if (field.Encodings.Length == 0)
        {
            return this.EncodeType(
                field.Type,
                value,
                writer,
                options,
                depth);
        }

        PerSchemaResult instructions = this.ApplyInstructionHooks(
            this.CreateFieldInstructionType(field),
            options);
        return instructions.Success
            ? this.EncodeTypeBody(
                field.Type,
                value,
                writer,
                options,
                depth,
                false)
            : instructions;
    }

    internal PerSchemaResult DecodeFieldType(
        AsnFieldDescriptor field,
        PerBitReader reader,
        AsnCodecOptions options,
        int depth)
    {
        if (field.Type == null)
        {
            return this.CreateMissingFieldType(field.Name);
        }

        if (field.Encodings.Length == 0)
        {
            return this.DecodeType(
                field.Type,
                reader,
                options,
                depth);
        }

        PerSchemaResult instructions = this.ApplyInstructionHooks(
            this.CreateFieldInstructionType(field),
            options);
        return instructions.Success
            ? this.DecodeTypeBody(
                field.Type,
                reader,
                options,
                depth,
                false)
            : instructions;
    }

    internal AsnTypeDescriptor CreateFieldInstructionType(
        AsnFieldDescriptor field)
    {
        AsnTypeDescriptor source = field.Type
            ?? throw new System.InvalidOperationException();
        return new AsnTypeDescriptor
        {
            StableId = field.StableId,
            Name = source.Name,
            Kind = source.Kind,
            Encodings = field.Encodings,
            Extensible = source.Extensible,
            SourceType = source.SourceType
        };
    }

    internal PerSchemaResult EncodeRootFields(
        AsnFieldDescriptor[] descriptors,
        AsnRuntimeFieldValue[] fields,
        bool[] presence,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth)
    {
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
                AsnRuntimeFieldValue? field = this.FindField(
                    fields,
                    descriptor.Name);
                if (field == null || field.Value == null || !field.Present)
                {
                    return this.CreateError(
                        "ASNP0085",
                        "A required PER component is absent: " + descriptor.Name,
                        false);
                }

                if (descriptor.Type == null)
                {
                    return this.CreateMissingFieldType(descriptor.Name);
                }

                PerSchemaResult encoded = this.EncodeFieldType(
                    descriptor,
                    field.Value,
                    writer,
                    options,
                    depth);
                if (!encoded.Success)
                {
                    return encoded;
                }
            }

            index = index + 1;
        }

        return this.CreateWriteSuccess(writer);
    }

    internal PerSchemaResult DecodeRootFields(
        AsnFieldDescriptor[] descriptors,
        bool[] presence,
        System.Collections.Generic.List<AsnRuntimeFieldValue> fields,
        PerBitReader reader,
        AsnCodecOptions options,
        int depth)
    {
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
                if (descriptor.Type == null)
                {
                    return this.CreateMissingFieldType(descriptor.Name);
                }

                PerSchemaResult decoded = this.DecodeFieldType(
                    descriptor,
                    reader,
                    options,
                    depth);
                if (!decoded.Success)
                {
                    return decoded;
                }

                field.Value = decoded.Value;
                if (this.Canonical
                    && descriptor.HasDefaultValue
                    && decoded.Value != null
                    && new PerCollectionCodec(reader.Limits).MatchesDefault(
                        descriptor,
                        decoded.Value,
                        this.Catalog))
                {
                    return this.CreateError(
                        "ASNP0093",
                        "Canonical PER must omit a component equal to its DEFAULT value: "
                            + descriptor.Name,
                        false);
                }
            }

            fields.Add(field);
            index = index + 1;
        }

        return this.CreateValueSuccess(new AsnRuntimeValue());
    }

    internal PerSchemaResult EncodeAdditionPayload(
        AsnFieldDescriptor[] descriptors,
        int[] additionIndexes,
        int additionIndex,
        AsnRuntimeFieldValue[] fields,
        AsnCodecOptions options,
        int depth,
        AsnResourceLimits limits)
    {
        PerBitWriter nested = new PerBitWriter(limits);
        int memberCount = this.CountAdditionMembers(
            additionIndexes,
            additionIndex);
        int firstMember = this.FindAdditionMember(
            additionIndexes,
            additionIndex,
            0);
        bool grouped = firstMember >= 0
            && descriptors[firstMember].ExtensionGroupIndex >= 0;
        if (memberCount == 1 && !grouped)
        {
            int fieldIndex = firstMember;
            AsnFieldDescriptor descriptor = descriptors[fieldIndex];
            AsnRuntimeFieldValue? field = this.FindField(fields, descriptor.Name);
            if (descriptor.Type == null || field == null || field.Value == null)
            {
                return this.CreateMissingFieldType(descriptor.Name);
            }

            PerSchemaResult encoded = this.EncodeFieldType(
                descriptor,
                field.Value,
                nested,
                options,
                depth);
            if (!encoded.Success)
            {
                return encoded;
            }
        }
        else
        {
            AsnFieldDescriptor[] members = this.GetAdditionMembers(
                descriptors,
                additionIndexes,
                additionIndex);
            PerCollectionCodec collections = new PerCollectionCodec(limits);
            PerCollectionResult presence = collections.BuildRootPresence(
                members,
                fields,
                this.Catalog);
            if (!presence.Success)
            {
                return this.CopyCollectionWrite(presence);
            }

            PerCollectionResult preamble = collections.WriteRootPreamble(
                nested,
                false,
                false,
                presence.RootPresence);
            if (!preamble.Success)
            {
                return this.CopyCollectionWrite(preamble);
            }

            PerSchemaResult encoded = this.EncodeRootFields(
                members,
                fields,
                presence.RootPresence,
                nested,
                options,
                depth);
            if (!encoded.Success)
            {
                return encoded;
            }
        }

        PerBitWriteResult output = nested.GetOutput();
        if (!output.Success)
        {
            return this.CopyWriterFailure(nested);
        }

        return new PerSchemaResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Data = output.Data,
            BitCount = nested.GetWrittenBitCount()
        };
    }

    internal PerSchemaResult DecodeAdditionPayload(
        AsnFieldDescriptor[] descriptors,
        int[] additionIndexes,
        int additionIndex,
        byte[] data,
        System.Collections.Generic.List<AsnRuntimeFieldValue> fields,
        AsnCodecOptions options,
        int depth,
        AsnResourceLimits limits)
    {
        PerBitReader nested = new PerBitReader(limits);
        PerBitReadResult input = nested.AppendInputBits(
            data,
            0,
            data.Length * 8,
            true);
        if (input.Status != AsnDecodeStatus.Done)
        {
            return this.CopyReaderFailure(nested);
        }

        int memberCount = this.CountAdditionMembers(
            additionIndexes,
            additionIndex);
        int firstMember = this.FindAdditionMember(
            additionIndexes,
            additionIndex,
            0);
        bool grouped = firstMember >= 0
            && descriptors[firstMember].ExtensionGroupIndex >= 0;
        if (memberCount == 1 && !grouped)
        {
            int fieldIndex = firstMember;
            AsnFieldDescriptor descriptor = descriptors[fieldIndex];
            if (descriptor.Type == null)
            {
                return this.CreateMissingFieldType(descriptor.Name);
            }

            PerSchemaResult decoded = this.DecodeFieldType(
                descriptor,
                nested,
                options,
                depth);
            if (!decoded.Success)
            {
                return decoded;
            }

            fields.Add(new AsnRuntimeFieldValue
            {
                Name = descriptor.Name,
                Present = true,
                Value = decoded.Value
            });
        }
        else
        {
            AsnFieldDescriptor[] members = this.GetAdditionMembers(
                descriptors,
                additionIndexes,
                additionIndex);
            PerCollectionResult preamble = new PerCollectionCodec(
                limits).ReadRootPreamble(
                    nested,
                    false,
                    this.CountOptionalFields(members));
            if (!preamble.Success)
            {
                return this.CopyCollectionRead(preamble);
            }

            PerSchemaResult decoded = this.DecodeRootFields(
                members,
                preamble.RootPresence,
                fields,
                nested,
                options,
                depth);
            if (!decoded.Success)
            {
                return decoded;
            }
        }

        PerBitReadResult complete = nested.CompleteInput(true);
        return complete.Status == AsnDecodeStatus.Done
            ? this.CreateValueSuccess(new AsnRuntimeValue())
            : this.CopyReaderFailure(nested);
    }

    internal PerSchemaResult EncodeChoice(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth)
    {
        AsnRuntimeFieldValue? selection = this.FindChoiceSelection(value.Fields);
        if (selection == null || selection.Value == null)
        {
            return this.CreateValueError("CHOICE");
        }

        int rootIndex = this.FindFieldIndex(type.RootFields, selection.Name);
        int extensionIndex = this.FindFieldIndex(
            type.ExtensionFields,
            selection.Name);
        bool extension = rootIndex < 0 && extensionIndex >= 0;
        int index = extension ? extensionIndex : rootIndex;
        AsnFieldDescriptor descriptor = extension
            ? type.ExtensionFields[index]
            : index >= 0 ? type.RootFields[index] : new AsnFieldDescriptor();
        if (index < 0 || descriptor.Type == null)
        {
            return this.CreateValueError("CHOICE");
        }

        PerAlternativeResult written = new PerAlternativeCodec(
            writer.Limits).WriteIndex(
                writer,
                extension,
                index,
                type.RootFields.Length,
                type.ExtensionFields.Length,
                type.Extensible,
                this.Aligned);
        if (!written.Success)
        {
            return this.CopyAlternativeWrite(written);
        }

        if (!extension)
        {
            return this.EncodeFieldType(
                descriptor,
                selection.Value,
                writer,
                options,
                depth + 1);
        }

        PerBitWriter nested = new PerBitWriter(writer.Limits);
        PerSchemaResult payload = this.EncodeFieldType(
            descriptor,
            selection.Value,
            nested,
            options,
            depth + 1);
        if (!payload.Success)
        {
            return payload;
        }

        PerBitWriteResult output = nested.GetOutput();
        if (!output.Success)
        {
            return this.CopyWriterFailure(nested);
        }

        PerOpenTypeResult open = new PerOpenTypeCodec(
            writer.Limits).WriteOpenType(
                writer,
                output.Data,
                nested.GetWrittenBitCount(),
                this.Aligned);
        return open.Success
            ? this.CreateWriteSuccess(writer)
            : this.CopyOpenTypeWrite(open);
    }

    internal PerSchemaResult DecodeChoice(
        AsnTypeDescriptor type,
        PerBitReader reader,
        AsnCodecOptions options,
        int depth)
    {
        PerAlternativeResult selected = new PerAlternativeCodec(
            reader.Limits).ReadIndex(
                reader,
                type.RootFields.Length,
                type.ExtensionFields.Length,
                type.Extensible,
                this.Aligned);
        if (!selected.Success)
        {
            return this.CopyAlternativeRead(selected);
        }

        AsnFieldDescriptor descriptor = selected.Extension
            ? type.ExtensionFields[selected.Index]
            : type.RootFields[selected.Index];
        if (descriptor.Type == null)
        {
            return this.CreateMissingFieldType(descriptor.Name);
        }

        PerSchemaResult decoded;
        if (selected.Extension)
        {
            PerOpenTypeResult open = new PerOpenTypeCodec(
                reader.Limits).ReadOpenType(reader, this.Aligned);
            if (!open.Success)
            {
                return this.CopyOpenTypeRead(open);
            }

            PerBitReader nested = new PerBitReader(reader.Limits);
            nested.AppendInputBits(open.Data, 0, open.Data.Length * 8, true);
            decoded = this.DecodeFieldType(
                descriptor,
                nested,
                options,
                depth + 1);
            if (decoded.Success
                && nested.CompleteInput(true).Status != AsnDecodeStatus.Done)
            {
                return this.CopyReaderFailure(nested);
            }
        }
        else
        {
            decoded = this.DecodeFieldType(
                descriptor,
                reader,
                options,
                depth + 1);
        }

        if (!decoded.Success)
        {
            return decoded;
        }

        return this.CreateValueSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = descriptor.Name,
                    Present = true,
                    Value = decoded.Value
                }
            ]
        });
    }

    internal PerSchemaResult EncodeCollectionOf(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth)
    {
        if (type.ElementType == null)
        {
            return this.CreateError(
                "ASNP0086",
                "A PER collection type requires an element type.",
                false);
        }

        if (!new ResourceCounter(writer.Limits).AllowsCollectionElements(
            value.Items.Length))
        {
            return this.CreateError(
                "ASNR0011",
                "The PER collection exceeds the configured element limit.",
                true);
        }

        PerSchemaBounds bounds = this.FindBounds(type, type.Kind);
        bool extension = this.IsSizeExtension(bounds, value.Items.Length);
        if (bounds.Extensible)
        {
            PerBitWriteResult marker = writer.WriteBit(extension);
            if (!marker.Success)
            {
                return this.CopyWriterFailure(writer);
            }
        }

        int lower = extension ? 0 : this.GetLowerInt(bounds);
        bool bounded = !extension && bounds.HasUpperBound;
        int upper = extension ? 0 : this.GetUpperInt(bounds);
        if (this.Canonical && type.Kind == AsnTypeKind.SetOf)
        {
            return this.EncodeCanonicalSetOf(
                type.ElementType,
                value.Items,
                lower,
                bounded,
                upper,
                writer,
                options,
                depth + 1);
        }

        PerLengthCodec lengths = new PerLengthCodec(writer.Limits);
        if (bounded && upper < 65536)
        {
            PerLengthResult length = lengths.WriteConstrainedLength(
                writer,
                value.Items.Length,
                lower,
                upper,
                this.Aligned);
            if (!length.Success)
            {
                return this.CopyLengthWrite(length);
            }

            return this.EncodeItems(
                type.ElementType,
                value.Items,
                0,
                value.Items.Length,
                writer,
                options,
                depth + 1);
        }

        int offset = 0;
        int remaining = value.Items.Length;
        bool more = true;
        while (more)
        {
            PerLengthResult component = lengths.WriteLengthComponent(
                writer,
                remaining,
                this.Aligned);
            if (!component.Success)
            {
                return this.CopyLengthWrite(component);
            }

            PerSchemaResult encoded = this.EncodeItems(
                type.ElementType,
                value.Items,
                offset,
                component.FragmentLength,
                writer,
                options,
                depth + 1);
            if (!encoded.Success)
            {
                return encoded;
            }

            offset = offset + component.FragmentLength;
            remaining = remaining - component.FragmentLength;
            more = component.MoreFragments;
        }

        return this.CreateWriteSuccess(writer);
    }

    internal PerSchemaResult DecodeCollectionOf(
        AsnTypeDescriptor type,
        PerBitReader reader,
        AsnCodecOptions options,
        int depth)
    {
        if (type.ElementType == null)
        {
            return this.CreateError(
                "ASNP0086",
                "A PER collection type requires an element type.",
                false);
        }

        PerSchemaBounds bounds = this.FindBounds(type, type.Kind);
        bool extension = this.ReadExtensionMarker(bounds, reader);
        if (reader.Failed)
        {
            return this.CopyReaderFailure(reader);
        }

        int lower = extension ? 0 : this.GetLowerInt(bounds);
        bool bounded = !extension && bounds.HasUpperBound;
        int upper = extension ? 0 : this.GetUpperInt(bounds);
        PerLengthCodec lengths = new PerLengthCodec(reader.Limits);
        System.Collections.Generic.List<AsnRuntimeValue> items =
            new System.Collections.Generic.List<AsnRuntimeValue>();
        System.Collections.Generic.List<byte[]>? canonicalData =
            this.Canonical && type.Kind == AsnTypeKind.SetOf
                ? new System.Collections.Generic.List<byte[]>()
                : null;
        System.Collections.Generic.List<int>? canonicalBitCounts =
            this.Canonical && type.Kind == AsnTypeKind.SetOf
                ? new System.Collections.Generic.List<int>()
                : null;
        if (bounded && upper < 65536)
        {
            PerLengthResult length = lengths.ReadConstrainedLength(
                reader,
                lower,
                upper,
                this.Aligned);
            if (!length.Success)
            {
                return this.CopyLengthRead(length);
            }

            PerSchemaResult decoded = this.DecodeItems(
                type.ElementType,
                length.Length,
                items,
                reader,
                options,
                depth + 1,
                canonicalData,
                canonicalBitCounts);
            if (!decoded.Success)
            {
                return decoded;
            }
        }
        else
        {
            bool more = true;
            System.Collections.Generic.List<int> fragmentLengths =
                new System.Collections.Generic.List<int>();
            System.Collections.Generic.List<int> fragmentOffsets =
                new System.Collections.Generic.List<int>();
            while (more)
            {
                int fragmentOffset = reader.Position;
                PerLengthResult component = lengths.ReadLengthComponent(
                    reader,
                    this.Aligned);
                if (!component.Success)
                {
                    return this.CopyLengthRead(component);
                }

                fragmentLengths.Add(component.FragmentLength);
                fragmentOffsets.Add(fragmentOffset);

                if (!new ResourceCounter(reader.Limits).AllowsCollectionElements(
                    items.Count + component.FragmentLength))
                {
                    return this.CreateError(
                        "ASNR0011",
                        "The PER collection exceeds the configured element limit.",
                        true);
                }

                PerSchemaResult decoded = this.DecodeItems(
                    type.ElementType,
                    component.FragmentLength,
                    items,
                    reader,
                    options,
                    depth + 1,
                    canonicalData,
                    canonicalBitCounts);
                if (!decoded.Success)
                {
                    return decoded;
                }

                more = component.MoreFragments;
            }

            PerSchemaResult fragments = this.ValidateCollectionFragments(
                fragmentLengths,
                fragmentOffsets,
                items.Count,
                reader);
            if (!fragments.Success)
            {
                return fragments;
            }
        }

        if (canonicalData != null && canonicalBitCounts != null)
        {
            PerCollectionOfResult ordering = new PerCollectionOfCodec(
                reader.Limits).ValidateCanonicalOrder(
                    canonicalData.ToArray(),
                    canonicalBitCounts.ToArray());
            if (!ordering.Success)
            {
                return this.CopyCollectionOfFailure(ordering);
            }
        }

        return this.CreateValueSuccess(new AsnRuntimeValue
        {
            Kind = type.Kind == AsnTypeKind.SetOf
                ? AsnRuntimeValueKind.SetOf
                : AsnRuntimeValueKind.SequenceOf,
            Items = items.ToArray()
        });
    }

    internal PerSchemaResult ValidateCollectionFragments(
        System.Collections.Generic.List<int> lengths,
        System.Collections.Generic.List<int> offsets,
        int totalCount,
        PerBitReader reader)
    {
        int remaining = totalCount;
        int index = 0;
        while (index < lengths.Count)
        {
            int length = lengths[index];
            if (index < lengths.Count - 1)
            {
                int units = remaining / 16384;
                if (units > 4)
                {
                    units = 4;
                }

                if (units < 1 || length != units * 16384)
                {
                    reader.CreateError(
                        "ASNP0071",
                        "A collection fragment does not use the required maximum multiplier.",
                        false,
                        true,
                        offsets[index]);
                    return this.CopyReaderFailure(reader);
                }
            }
            else if (length != remaining)
            {
                reader.CreateError(
                    "ASNP0069",
                    "The final collection fragment has the wrong element count.",
                    false,
                    true,
                    offsets[index]);
                return this.CopyReaderFailure(reader);
            }

            remaining = remaining - length;
            index = index + 1;
        }

        return this.CreateValueSuccess(new AsnRuntimeValue());
    }

    internal PerSchemaResult EncodeItems(
        AsnTypeDescriptor elementType,
        AsnRuntimeValue[] items,
        int offset,
        int count,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth)
    {
        int index = 0;
        while (index < count)
        {
            PerSchemaResult encoded = this.EncodeType(
                elementType,
                items[offset + index],
                writer,
                options,
                depth);
            if (!encoded.Success)
            {
                return encoded;
            }

            index = index + 1;
        }

        return this.CreateWriteSuccess(writer);
    }

    internal PerSchemaResult EncodeCanonicalSetOf(
        AsnTypeDescriptor elementType,
        AsnRuntimeValue[] items,
        int lowerBound,
        bool hasUpperBound,
        int upperBound,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth)
    {
        byte[][] encodings = new byte[items.Length][];
        int[] bitCounts = new int[items.Length];
        int index = 0;
        while (index < items.Length)
        {
            PerBitWriter nested = new PerBitWriter(writer.Limits);
            PerSchemaResult encoded = this.EncodeType(
                elementType,
                items[index],
                nested,
                options,
                depth);
            if (!encoded.Success)
            {
                return encoded;
            }

            PerBitWriteResult output = nested.GetOutput();
            if (!output.Success)
            {
                return this.CopyWriterFailure(nested);
            }

            encodings[index] = output.Data;
            bitCounts[index] = nested.GetWrittenBitCount();
            index = index + 1;
        }

        PerCollectionOfResult written = new PerCollectionOfCodec(
            writer.Limits).WriteCollection(
                writer,
                encodings,
                bitCounts,
                lowerBound,
                hasUpperBound,
                upperBound,
                this.Aligned,
                true);
        return written.Success
            ? this.CreateWriteSuccess(writer)
            : this.CopyCollectionOfFailure(written);
    }

    internal PerSchemaResult DecodeItems(
        AsnTypeDescriptor elementType,
        int count,
        System.Collections.Generic.List<AsnRuntimeValue> items,
        PerBitReader reader,
        AsnCodecOptions options,
        int depth,
        System.Collections.Generic.List<byte[]>? canonicalData,
        System.Collections.Generic.List<int>? canonicalBitCounts)
    {
        int index = 0;
        while (index < count)
        {
            int start = reader.Position;
            PerSchemaResult decoded = this.DecodeType(
                elementType,
                reader,
                options,
                depth);
            if (!decoded.Success || decoded.Value == null)
            {
                return decoded;
            }

            items.Add(decoded.Value);
            if (canonicalData != null && canonicalBitCounts != null)
            {
                int bitCount = reader.Position - start;
                canonicalData.Add(this.CopyInputBits(reader, start, bitCount));
                canonicalBitCounts.Add(bitCount);
            }

            index = index + 1;
        }

        return this.CreateValueSuccess(new AsnRuntimeValue());
    }

    internal byte[] CopyInputBits(
        PerBitReader reader,
        int offset,
        int bitCount)
    {
        int byteCount = bitCount / 8;
        if (bitCount % 8 != 0)
        {
            byteCount = byteCount + 1;
        }

        byte[] result = new byte[byteCount];
        int index = 0;
        while (index < bitCount)
        {
            if (reader.GetBit(offset + index))
            {
                result[index / 8] = (byte)(result[index / 8]
                    | 1 << (7 - index % 8));
            }

            index = index + 1;
        }

        return result;
    }

    internal PerSchemaResult EncodeOpenType(
        AsnRuntimeValue value,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth)
    {
        if (value.OpenTypeValue == null)
        {
            return this.CreateValueError("open type");
        }

        AsnOpenTypeValue open = value.OpenTypeValue;
        byte[] data = open.OriginalData;
        int bitCount = data.Length * 8;
        if (open.Resolved && open.Value != null && open.ResolvedTypeId.Length > 0)
        {
            AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
                .ResolveRequestedType(open.ResolvedTypeId);
            if (type == null)
            {
                return this.CreateError(
                    "ASNP0087",
                    "The resolved open type is missing from the catalog.",
                    false);
            }

            PerBitWriter nested = new PerBitWriter(writer.Limits);
            PerSchemaResult encoded = this.EncodeType(
                type,
                open.Value,
                nested,
                options,
                depth + 1);
            if (!encoded.Success)
            {
                return encoded;
            }

            PerBitWriteResult output = nested.GetOutput();
            if (!output.Success)
            {
                return this.CopyWriterFailure(nested);
            }

            data = output.Data;
            bitCount = nested.GetWrittenBitCount();
        }
        else if (!new OpaquePayloadPolicy().CanReuseOpenType(
            open,
            this.Rules))
        {
            return this.CreateValueError("open type");
        }

        PerOpenTypeResult written = new PerOpenTypeCodec(
            writer.Limits).WriteOpenType(
                writer,
                data,
                bitCount,
                this.Aligned);
        return written.Success
            ? this.CreateWriteSuccess(writer)
            : this.CopyOpenTypeWrite(written);
    }

    internal PerSchemaResult EncodeInstanceOf(
        AsnRuntimeValue value,
        PerBitWriter writer,
        AsnCodecOptions options,
        int depth)
    {
        AsnRuntimeFieldValue? field = this.FindField(value.Fields, "value");
        if (field == null || field.Value == null
            || field.Value.OpenTypeValue == null)
        {
            return this.CreateValueError("INSTANCE OF");
        }

        AsnOpenTypeValue open = field.Value.OpenTypeValue;
        byte[] data = open.OriginalData;
        int bitCount = data.Length * 8;
        if (open.Resolved && open.Value != null && open.ResolvedTypeId.Length > 0)
        {
            AsnTypeDescriptor? type = new BerSchemaDecoder(this.Catalog)
                .ResolveRequestedType(open.ResolvedTypeId);
            if (type == null)
            {
                return this.CreateError(
                    "ASNP0087",
                    "The resolved INSTANCE OF type is missing from the catalog.",
                    false);
            }

            PerBitWriter nested = new PerBitWriter(writer.Limits);
            PerSchemaResult encoded = this.EncodeType(
                type,
                open.Value,
                nested,
                options,
                depth + 1);
            if (!encoded.Success)
            {
                return encoded;
            }

            PerBitWriteResult output = nested.GetOutput();
            if (!output.Success)
            {
                return this.CopyWriterFailure(nested);
            }

            data = output.Data;
            bitCount = nested.GetWrittenBitCount();
        }
        else if (!new OpaquePayloadPolicy().CanReuseOpenType(
            open,
            this.Rules))
        {
            return this.CreateValueError("INSTANCE OF");
        }

        return this.CopyUsefulWrite(new PerUsefulTypeCodec(
            writer.Limits).WriteInstanceOfValue(
                writer,
                value,
                data,
                bitCount,
                this.Aligned));
    }

    internal PerSchemaResult DecodeInstanceOf(
        PerBitReader reader,
        AsnCodecOptions options)
    {
        return this.CopyUsefulRead(new PerUsefulTypeCodec(
            reader.Limits).ReadInstanceOfValue(
                reader,
                this.Aligned,
                options.UnknownDataPolicy,
                this.Rules,
                "",
                null));
    }

    internal PerSchemaResult DecodeOpenType(
        PerBitReader reader,
        AsnCodecOptions options)
    {
        PerOpenTypeResult decoded = new PerOpenTypeCodec(
            reader.Limits).ReadOpenType(reader, this.Aligned);
        if (!decoded.Success)
        {
            return this.CopyOpenTypeRead(decoded);
        }

        if (options.UnknownDataPolicy == AsnUnknownDataPolicy.Reject)
        {
            return this.CreateError(
                "ASNP0088",
                "An unresolved PER open type is not permitted by policy.",
                false);
        }

        bool preserve = options.UnknownDataPolicy == AsnUnknownDataPolicy.Preserve;
        return this.CreateValueSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = preserve,
                EncodingRules = this.Rules,
                HasOriginalData = preserve,
                OriginalData = preserve ? decoded.Data : []
            }
        });
    }

    internal PerSchemaBounds FindBounds(
        AsnTypeDescriptor type,
        AsnTypeKind domainKind)
    {
        PerSchemaBounds result = new PerSchemaBounds();
        int index = 0;
        while (index < type.Constraints.Length)
        {
            this.ApplyBounds(type.Constraints[index], domainKind, result);
            index = index + 1;
        }

        return result;
    }

    internal void ApplyBounds(
        AsnConstraintDescriptor constraint,
        AsnTypeKind domainKind,
        PerSchemaBounds result)
    {
        int applicationIndex = 0;
        while (applicationIndex < constraint.Applications.Length)
        {
            this.ApplyBounds(
                constraint.Applications[applicationIndex],
                domainKind,
                result);
            applicationIndex = applicationIndex + 1;
        }

        if (constraint.DomainKind != domainKind
            || constraint.RootIntervals.Length != 1)
        {
            return;
        }

        AsnIntervalDescriptor interval = constraint.RootIntervals[0];
        result.HasLowerBound = interval.HasLowerBound;
        result.LowerBound = interval.LowerBound;
        result.HasUpperBound = interval.HasUpperBound;
        result.UpperBound = interval.UpperBound;
        result.Extensible = constraint.Extensible;
    }

    internal bool ReadExtensionMarker(
        PerSchemaBounds bounds,
        PerBitReader reader)
    {
        if (!bounds.Extensible)
        {
            return false;
        }

        PerBitReadResult marker = reader.ReadBit();
        return marker.Status == AsnDecodeStatus.Done
            && marker.Value != System.Numerics.BigInteger.Zero;
    }

    internal bool IsSizeExtension(PerSchemaBounds bounds, int count)
    {
        if (!bounds.Extensible)
        {
            return false;
        }

        System.Numerics.BigInteger value = new System.Numerics.BigInteger(count);
        return bounds.HasLowerBound && value < bounds.LowerBound
            || bounds.HasUpperBound && value > bounds.UpperBound;
    }

    internal int GetLowerInt(PerSchemaBounds bounds)
    {
        return bounds.HasLowerBound ? (int)bounds.LowerBound : 0;
    }

    internal int GetUpperInt(PerSchemaBounds bounds)
    {
        return bounds.HasUpperBound ? (int)bounds.UpperBound : 0;
    }

    internal int FindNamedValue(
        AsnValueDescriptor[] descriptors,
        AsnRuntimeValue value)
    {
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnValue? namedValue = descriptors[index].Value;
            if (descriptors[index].Name == value.TextValue)
            {
                return index;
            }

            if (value.HasIntegerValue
                && namedValue != null
                && namedValue.HasIntegerValue
                && namedValue.IntegerValue == value.IntegerValue)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal AsnRuntimeFieldValue? FindField(
        AsnRuntimeFieldValue[] fields,
        string name)
    {
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Name == name)
            {
                return fields[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool IsFieldPresent(
        AsnFieldDescriptor descriptor,
        AsnRuntimeFieldValue[] fields)
    {
        AsnRuntimeFieldValue? field = this.FindField(fields, descriptor.Name);
        return field != null && field.Present && field.Value != null;
    }

    internal AsnRuntimeFieldValue? FindChoiceSelection(
        AsnRuntimeFieldValue[] fields)
    {
        AsnRuntimeFieldValue? result = null;
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Present)
            {
                if (result != null)
                {
                    return null;
                }

                result = fields[index];
            }

            index = index + 1;
        }

        return result;
    }

    internal int FindFieldIndex(
        AsnFieldDescriptor[] fields,
        string name)
    {
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
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

    internal AsnFieldDescriptor[] GetCollectionRootFields(
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

        if (!this.Canonical || type.Kind != AsnTypeKind.Set)
        {
            return result;
        }

        index = 1;
        while (index < result.Length)
        {
            AsnFieldDescriptor field = result[index];
            int insertion = index;
            while (insertion > 0
                && this.CompareFieldTags(field, result[insertion - 1]) < 0)
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

    internal PerSchemaResult ValidateUnknownExtensions(
        AsnUnknownExtension[] extensions,
        AsnCodecOptions options,
        int knownAdditionCount,
        bool extensible)
    {
        if (extensions.Length == 0
            || options.UnknownDataPolicy == AsnUnknownDataPolicy.Skip)
        {
            return new PerSchemaResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done
            };
        }

        if (options.UnknownDataPolicy == AsnUnknownDataPolicy.Reject
            || !extensible)
        {
            return this.CreateError(
                "ASNP0095",
                "Preserved PER extensions are not permitted by the schema or policy.",
                false);
        }

        int index = 0;
        while (index < extensions.Length)
        {
            AsnUnknownExtension extension = extensions[index];
            if (!new OpaquePayloadPolicy().CanReuseUnknownExtension(
                    extension,
                    this.Rules)
                || extension.Index < knownAdditionCount
                || extension.HasTextData)
            {
                return this.CreateError(
                    "ASNP0096",
                    "A preserved PER extension has invalid or incompatible metadata.",
                    false);
            }

            if (extension.Present && !extension.HasBinaryData
                || !extension.Present
                    && (extension.HasBinaryData || extension.HasTextData))
            {
                return this.CreateError(
                    "ASNP0096",
                    "Preserved PER extension presence does not match its payload.",
                    false);
            }

            int duplicateIndex = index + 1;
            while (duplicateIndex < extensions.Length)
            {
                if (extensions[duplicateIndex] == null
                    || extensions[duplicateIndex].Index == extension.Index)
                {
                    return this.CreateError(
                        "ASNP0096",
                        "Preserved PER extension indexes must be unique.",
                        false);
                }

                duplicateIndex = duplicateIndex + 1;
            }

            index = index + 1;
        }

        return new PerSchemaResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal int GetPreservedAdditionCount(
        AsnUnknownExtension[] extensions,
        int knownAdditionCount)
    {
        int result = knownAdditionCount;
        int index = 0;
        while (index < extensions.Length)
        {
            if (extensions[index].Index >= result)
            {
                result = extensions[index].Index + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool[] MergeUnknownPresence(
        bool[] knownPresence,
        AsnUnknownExtension[] extensions,
        int additionCount)
    {
        bool[] result = new bool[additionCount];
        int index = 0;
        while (index < knownPresence.Length)
        {
            result[index] = knownPresence[index];
            index = index + 1;
        }

        index = 0;
        while (index < extensions.Length)
        {
            result[extensions[index].Index] = extensions[index].Present;
            index = index + 1;
        }

        return result;
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

    internal bool[] TrimExtensionPresence(bool[] presence)
    {
        int count = presence.Length;
        while (count > 0 && !presence[count - 1])
        {
            count = count - 1;
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

    internal int CountAdditionMembers(int[] indexes, int additionIndex)
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

        return count;
    }

    internal int FindAdditionMember(
        int[] indexes,
        int additionIndex,
        int memberIndex)
    {
        int found = 0;
        int index = 0;
        while (index < indexes.Length)
        {
            if (indexes[index] == additionIndex)
            {
                if (found == memberIndex)
                {
                    return index;
                }

                found = found + 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal AsnFieldDescriptor[] GetAdditionMembers(
        AsnFieldDescriptor[] fields,
        int[] indexes,
        int additionIndex)
    {
        AsnFieldDescriptor[] result = new AsnFieldDescriptor[
            this.CountAdditionMembers(indexes, additionIndex)];
        int resultIndex = 0;
        int index = 0;
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

    internal bool AllowsDepth(AsnCodecOptions options, int depth)
    {
        AsnResourceLimits limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
        return limits.Unlimited || depth <= limits.MaximumNestingDepth;
    }

    internal bool IsKnownMultiplierString(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.NumericString
            || kind == AsnTypeKind.PrintableString
            || kind == AsnTypeKind.VisibleString
            || kind == AsnTypeKind.Ia5String
            || kind == AsnTypeKind.BmpString
            || kind == AsnTypeKind.UniversalString;
    }

    internal bool IsBaseEncodedString(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.ObjectDescriptor
            || kind == AsnTypeKind.Utf8String
            || kind == AsnTypeKind.TeletexString
            || kind == AsnTypeKind.VideotexString
            || kind == AsnTypeKind.GraphicString
            || kind == AsnTypeKind.GeneralString;
    }

    internal bool IsTimeType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.UtcTime
            || kind == AsnTypeKind.GeneralizedTime
            || kind == AsnTypeKind.Time
            || kind == AsnTypeKind.Date
            || kind == AsnTypeKind.TimeOfDay
            || kind == AsnTypeKind.DateTime
            || kind == AsnTypeKind.Duration;
    }

    internal PerSchemaResult ApplyInstructionHooks(
        AsnTypeDescriptor type,
        AsnCodecOptions options)
    {
        if (this.Aligned)
        {
            return new PerSchemaResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done
            };
        }

        int encodingIndex = 0;
        while (encodingIndex < type.Encodings.Length)
        {
            AsnEncodingDescriptor encoding = type.Encodings[encodingIndex];
            int instructionIndex = 0;
            while (instructionIndex < encoding.Instructions.Length)
            {
                AsnEncodingInstruction instruction =
                    encoding.Instructions[instructionIndex];
                if (instruction.Kind == AsnInstructionKind.PerApplication)
                {
                    PerSchemaResult applied = this.ApplyInstructionHook(
                        type,
                        encoding,
                        instruction,
                        options);
                    if (!applied.Success)
                    {
                        return applied;
                    }
                }

                instructionIndex = instructionIndex + 1;
            }

            encodingIndex = encodingIndex + 1;
        }

        return new PerSchemaResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal PerSchemaResult ApplyInstructionHook(
        AsnTypeDescriptor type,
        AsnEncodingDescriptor encoding,
        AsnEncodingInstruction instruction,
        AsnCodecOptions options)
    {
        IPerInstructionRegistry? registry = options.PerInstructionRegistry;
        if (registry == null)
        {
            return this.CreateError(
                "ASNP0091",
                "Unaligned PER instruction is not supported without a registered handler: "
                    + instruction.Name,
                false);
        }

        IPerInstructionHandler? handler = registry.ResolveHandler(
            instruction.Name);
        AsnPerInstructionRegistration? registration =
            this.FindInstructionRegistration(
                registry.GetRegistrations(),
                instruction.Name);
        if (handler == null || registration == null
            || !registration.SupportsUnaligned)
        {
            return this.CreateError(
                "ASNP0091",
                "Unaligned PER instruction is not supported by a registered handler: "
                    + instruction.Name,
                false);
        }

        AsnType publicType = type.SourceType == null
            ? new AsnType
            {
                Name = type.Name,
                Kind = type.Kind,
                Extensible = type.Extensible
            }
            : type.SourceType;
        AsnDiagnostic[] diagnostics = handler.ValidateInstruction(
            instruction,
            publicType);
        int diagnosticIndex = 0;
        while (diagnosticIndex < diagnostics.Length)
        {
            if (diagnostics[diagnosticIndex].Severity
                == AsnDiagnosticSeverity.Error)
            {
                return this.CreateError(
                    diagnostics[diagnosticIndex].Code,
                    diagnostics[diagnosticIndex].Message,
                    diagnostics[diagnosticIndex].Category
                        == AsnDiagnosticCategory.Resource);
            }

            diagnosticIndex = diagnosticIndex + 1;
        }

        AsnEncodingPlan plan = encoding.Plan == null
            ? new AsnEncodingPlan
            {
                StableId = encoding.StableId + "/uper-hook",
                EncodingRules = this.Rules,
                Instructions = encoding.Instructions
            }
            : new AsnEncodingPlan
            {
                StableId = encoding.Plan.StableId,
                EncodingRules = this.Rules,
                Steps = encoding.Plan.Steps,
                Instructions = encoding.Plan.Instructions,
                PerInstructionRegistrations =
                    encoding.Plan.PerInstructionRegistrations
            };
        AsnEncodingPlanStep[] steps = handler.ApplyInstruction(
            instruction,
            publicType,
            plan);
        if (steps == null)
        {
            return this.CreateError(
                "ASNP0092",
                "An unaligned PER instruction handler returned no plan steps: "
                    + instruction.Name,
                false);
        }

        return new PerSchemaResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal AsnPerInstructionRegistration? FindInstructionRegistration(
        AsnPerInstructionRegistration[] registrations,
        string keyword)
    {
        int index = 0;
        while (index < registrations.Length)
        {
            if (registrations[index].Keyword == keyword)
            {
                return registrations[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal PerSchemaResult CreateWriteSuccess(PerBitWriter writer)
    {
        return new PerSchemaResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            BitCount = writer.GetWrittenBitCount()
        };
    }

    internal PerSchemaResult CreateValueSuccess(AsnRuntimeValue value)
    {
        return new PerSchemaResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value
        };
    }

    internal PerSchemaResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerSchemaResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal PerSchemaResult CreateReadFailure(
        AsnDecodeStatus status,
        string code,
        string message,
        bool limitExceeded)
    {
        return new PerSchemaResult
        {
            Status = status,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal PerSchemaResult CreateValueError(string typeName)
    {
        return this.CreateError(
            "ASNP0089",
            "The runtime value does not contain a valid " + typeName + " value.",
            false);
    }

    internal PerSchemaResult CreateMissingFieldType(string name)
    {
        return this.CreateError(
            "ASNP0090",
            "The PER component has no resolved type: " + name,
            false);
    }

    internal PerSchemaResult CopyPrimitiveWrite(PerPrimitiveResult source)
    {
        return source.Success
            ? new PerSchemaResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                BitCount = source.BitCount
            }
            : this.CreateError(
                source.ErrorCode,
                source.ErrorMessage,
                source.LimitExceeded);
    }

    internal PerSchemaResult CopyPrimitiveRead(PerPrimitiveResult source)
    {
        return source.Success && source.Value != null
            ? this.CreateValueSuccess(source.Value)
            : this.CreateReadFailure(
                source.Status,
                source.ErrorCode,
                source.ErrorMessage,
                source.LimitExceeded);
    }

    internal PerSchemaResult CopyStringWrite(PerStringResult source)
    {
        return source.Success
            ? new PerSchemaResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                BitCount = source.BitCount
            }
            : this.CreateError(
                source.ErrorCode,
                source.ErrorMessage,
                source.LimitExceeded);
    }

    internal PerSchemaResult CopyStringRead(PerStringResult source)
    {
        return this.CreateReadFailure(
            source.Status,
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal PerSchemaResult CopyUsefulWrite(PerUsefulTypeResult source)
    {
        return source.Success
            ? new PerSchemaResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                BitCount = source.BitCount
            }
            : this.CreateError(
                source.ErrorCode,
                source.ErrorMessage,
                source.LimitExceeded);
    }

    internal PerSchemaResult CopyUsefulRead(PerUsefulTypeResult source)
    {
        return source.Success && source.Value != null
            ? this.CreateValueSuccess(source.Value)
            : this.CreateReadFailure(
                source.Status,
                source.ErrorCode,
                source.ErrorMessage,
                source.LimitExceeded);
    }

    internal PerSchemaResult CopyAlternativeWrite(PerAlternativeResult source)
    {
        return source.Success
            ? new PerSchemaResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                BitCount = source.BitCount
            }
            : this.CreateError(
                source.ErrorCode,
                source.ErrorMessage,
                source.LimitExceeded);
    }

    internal PerSchemaResult CopyAlternativeRead(PerAlternativeResult source)
    {
        return this.CreateReadFailure(
            source.Status,
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal PerSchemaResult CopyCollectionWrite(PerCollectionResult source)
    {
        return source.Success
            ? new PerSchemaResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                BitCount = source.BitCount
            }
            : this.CreateError(
                source.ErrorCode,
                source.ErrorMessage,
                source.LimitExceeded);
    }

    internal PerSchemaResult CopyCollectionRead(PerCollectionResult source)
    {
        return this.CreateReadFailure(
            source.Status,
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal PerSchemaResult CopyCollectionOfFailure(
        PerCollectionOfResult source)
    {
        return this.CreateReadFailure(
            source.Status,
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal PerSchemaResult CopyLengthWrite(PerLengthResult source)
    {
        return source.Success
            ? new PerSchemaResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                BitCount = source.BitCount
            }
            : this.CreateError(
                source.ErrorCode,
                source.ErrorMessage,
                source.LimitExceeded);
    }

    internal PerSchemaResult CopyLengthRead(PerLengthResult source)
    {
        return this.CreateReadFailure(
            source.Status,
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal PerSchemaResult CopyOpenTypeWrite(PerOpenTypeResult source)
    {
        return this.CreateError(
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal PerSchemaResult CopyOpenTypeRead(PerOpenTypeResult source)
    {
        return this.CreateReadFailure(
            source.Status,
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal PerSchemaResult CopyWriterFailure(PerBitWriter writer)
    {
        return this.CreateError(
            writer.ErrorCode,
            writer.ErrorMessage,
            writer.LimitExceeded);
    }

    internal PerSchemaResult CopyReaderFailure(PerBitReader reader)
    {
        return this.CreateReadFailure(
            reader.Failed
                ? AsnDecodeStatus.Error
                : AsnDecodeStatus.NeedMoreData,
            reader.ErrorCode,
            reader.ErrorMessage,
            reader.LimitExceeded);
    }

    internal AsnEncodeResult CreatePublicEncodeError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new AsnEncodeResult
        {
            Diagnostics = [this.CreateDiagnostic(code, message, limitExceeded, true)]
        };
    }

    internal AsnDecodeResult CreatePublicDecodeError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Error,
            Diagnostics = [this.CreateDiagnostic(code, message, limitExceeded, false)]
        };
    }

    internal AsnEncodeResult CopyPublicEncodeFailure(PerSchemaResult source)
    {
        return this.CreatePublicEncodeError(
            source.ErrorCode,
            source.ErrorMessage,
            source.LimitExceeded);
    }

    internal AsnEncodeResult CopyPublicEncodeWriterFailure(PerBitWriter writer)
    {
        return this.CreatePublicEncodeError(
            writer.ErrorCode,
            writer.ErrorMessage,
            writer.LimitExceeded);
    }

    internal AsnEncodeResult CreateStreamingFailure(
        string code,
        string message,
        bool limitExceeded,
        PerBitWriter writer)
    {
        AsnEncodeResult result = this.CreatePublicEncodeError(
            code,
            message,
            limitExceeded);
        int writtenCount = writer.GetEmittedByteCount();
        result.WrittenCount = writtenCount;
        result.StreamProgress = new AsnStreamProgress
        {
            WrittenCount = writtenCount,
            Complete = false
        };
        return result;
    }

    internal AsnDecodeResult CopyPublicDecodeFailure(
        PerSchemaResult source,
        int consumedCount,
        PerBitReader reader)
    {
        AsnDiagnostic[] diagnostics = source.ErrorCode.Length == 0
            ? []
            : [this.CreateDiagnostic(
                source.ErrorCode,
                source.ErrorMessage,
                source.LimitExceeded,
                false)];
        AsnDecodeResult result = new AsnDecodeResult
        {
            Status = source.Status,
            ConsumedCount = consumedCount,
            Diagnostics = diagnostics
        };
        if (result.Status == AsnDecodeStatus.Done)
        {
            result.Status = AsnDecodeStatus.Error;
        }
        if (diagnostics.Length > 0)
        {
            this.SetDiagnosticBitOffset(diagnostics[0], reader);
        }
        return result;
    }

    internal AsnDecodeResult CopyPublicDecodeReaderFailure(
        PerBitReader reader,
        int consumedCount)
    {
        AsnDiagnostic[] diagnostics = reader.ErrorCode.Length == 0
            ? []
            : [this.CreateDiagnostic(
                reader.ErrorCode,
                reader.ErrorMessage,
                reader.LimitExceeded,
                false)];
        AsnDecodeResult result = new AsnDecodeResult
        {
            Status = reader.Failed
                ? AsnDecodeStatus.Error
                : AsnDecodeStatus.NeedMoreData,
            ConsumedCount = consumedCount,
            Diagnostics = diagnostics
        };
        if (diagnostics.Length > 0)
        {
            this.SetDiagnosticBitOffset(diagnostics[0], reader);
        }
        return result;
    }

    internal void SetDiagnosticBitOffset(
        AsnDiagnostic diagnostic,
        PerBitReader reader)
    {
        diagnostic.HasBitOffset = true;
        diagnostic.BitOffset = reader.HasViolationBitOffset
            ? reader.ViolationBitOffset
            : reader.Position;
    }

    internal AsnDiagnostic CreateDiagnostic(
        string code,
        string message,
        bool limitExceeded,
        bool encoding)
    {
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = limitExceeded
                ? AsnDiagnosticCategory.Resource
                : encoding
                    ? AsnDiagnosticCategory.Encoding
                    : AsnDiagnosticCategory.Decoding,
            Code = code,
            Message = message
        };
    }
}
