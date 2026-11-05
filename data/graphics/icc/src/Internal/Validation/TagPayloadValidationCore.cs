namespace icc;

internal static class TagPayloadValidationCore
{
    internal static IccValidationIssue[] Validate(IccProfile profile, IccValidationPolicy policy)
    {
        System.Collections.Generic.List<IccValidationIssue> issues =
            new System.Collections.Generic.List<IccValidationIssue>();
        if (profile.Tags == null)
        {
            return issues.ToArray();
        }

        IccVersionKind version = profile.Header == null || profile.Header.Version == null
            ? IccVersionKind.Unknown : profile.Header.Version.Kind;
        long diagnosticBase = profile.Location == null || profile.Location.Offset < 0
            ? -1 : profile.Location.Offset;
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null)
            {
                ValidateTagModel(tag, index, version, diagnosticBase, policy, issues);
            }
            index = index + 1;
        }

        return issues.ToArray();
    }

    internal static void ValidateTagModel(IccTag tag, int index, IccVersionKind version,
        long diagnosticBase, IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        ValidateTagRegistration(tag, index, diagnosticBase, policy, issues);
        if (tag.Data == null)
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Fatal,
                "The tag data model is missing, so its payload cannot be validated or preserved.",
                index, 0, GetPayloadLength(tag), tag,
                "Provide decoded tag data or a complete losslessly retained raw payload."));
            return;
        }

        IccRawData raw = tag.Data.RawData;
        if (raw == null)
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Fatal,
                "The tag raw-data model is missing, so encoded payload safety cannot be established.",
                index, 0, GetPayloadLength(tag), tag,
                "Provide a non-null raw-data model and retain the complete opaque payload when decoding is unavailable."));
            return;
        }

        if (raw.Bytes == null)
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Fatal,
                "The retained raw tag payload byte collection is null.", index, 0,
                GetPayloadLength(tag), tag,
                "Use an empty byte array when no bytes are retained, or provide the complete encoded payload."));
            return;
        }

        System.ReadOnlyMemory<byte> payload = ResolvePayload(tag);
        bool requiresRawPayload = tag.Data.Kind == IccTagDataKind.Raw;
        if (payload.Length == 0)
        {
            if (requiresRawPayload)
            {
                issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                    "An opaque tag representation does not retain its encoded payload bytes.",
                    index, 0, GetPayloadLength(tag), tag,
                    "Retain the complete opaque payload, including its type signature and reserved field."));
            }
            if (raw.HasTypeSignature)
            {
                ValidateTypeRegistration(raw.TypeSignature, tag, index, version, policy, issues);
            }
            ValidateCompressedModel(tag, index, policy, issues);
            return;
        }

        if (payload.Length > policy.ResourceLimits.MaximumTagBytes)
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.SecurityResourceLimit,
                IccValidationSeverity.Fatal,
                $"The retained tag payload contains {payload.Length} bytes, exceeding the configured maximum of {policy.ResourceLimits.MaximumTagBytes} bytes.",
                index, 0, payload.Length, tag,
                "Reduce the payload size or explicitly raise the trusted maximum tag-byte limit."));
        }

        if (tag.TableEntry != null && tag.TableEntry.Size > 0
            && payload.Length != tag.TableEntry.Size)
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                $"The retained payload contains {payload.Length} bytes, but its tag-table entry declares {tag.TableEntry.Size} bytes.",
                index, 0, payload.Length, tag,
                "Retain exactly the complete byte range declared by the tag-table entry."));
        }

        ValidateTypeHeader(tag, index, version, policy, payload, issues);
        ValidateCompressedModel(tag, index, policy, issues);
    }

    internal static void ValidateTagRegistration(IccTag tag, int index, long diagnosticBase,
        IccValidationPolicy policy, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        uint signature = tag.Signature;
        if (IsRegisteredTagSignature(signature))
        {
            return;
        }

        bool printable = HeaderValidationCore.IsPrintableSignature(signature);
        IccValidationSeverity severity = printable && policy.AllowPrivateSignatures
            ? IccValidationSeverity.Information : IccValidationSeverity.Error;
        string message = printable
            ? $"Tag signature '{FourCcCodec.Format(signature)}' (0x{signature:X8}) is private or is not present in the built-in ICC registration tables."
            : $"Tag signature 0x{signature:X8} is neither registered nor a printable private four-character signature.";
        string remediation = printable
            ? policy.AllowPrivateSignatures
                ? "Retain the signature and its complete opaque payload; register a codec when typed interpretation is required."
                : "Use a registered tag signature, or explicitly permit private signatures in the validation policy."
            : "Use a registered or printable private four-character tag signature.";
        issues.Add(CreateTagSignatureIssue(severity, message, index, diagnosticBase,
            tag, remediation));
    }

    internal static void ValidateTypeHeader(IccTag tag, int index, IccVersionKind version,
        IccValidationPolicy policy, System.ReadOnlyMemory<byte> payload,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccRawData raw = tag.Data.RawData;
        if (payload.Length < 8)
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                $"The tag payload contains {payload.Length} bytes; a complete type header requires at least 8 bytes.",
                index, 0, payload.Length, tag,
                "Provide the four-byte type signature followed by four zero reserved bytes."));
            bool hasShortRawMetadata = raw.Bytes.Length > 0 || raw.HasTypeSignature
                || tag.Data.Kind == IccTagDataKind.Raw || tag.IsDecodeAttempted;
            if (hasShortRawMetadata && raw.HasTypeSignature != (payload.Length >= 4))
            {
                issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagTypeCompatibility,
                    IccValidationSeverity.Error,
                    "The raw-data type-signature presence flag does not match the retained payload header.",
                    index, 0, System.Math.Min(payload.Length, 4), tag,
                    "Set the presence flag only when all four type-signature bytes are retained."));
            }
            if (payload.Length >= 4)
            {
                uint shortType = ReadUInt32(payload, 0);
                if (hasShortRawMetadata && raw.HasTypeSignature
                    && raw.TypeSignature != shortType)
                {
                    issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagTypeCompatibility,
                        IccValidationSeverity.Error,
                        "The raw-data type signature does not match the signature encoded in the retained payload.",
                        index, 0, 4, tag,
                        "Set the raw-data type signature from the first four payload bytes."));
                }
                ValidateTypeRegistration(shortType, tag, index, version, policy, issues);
            }
            return;
        }

        uint encodedType = ReadUInt32(payload, 0);
        bool hasRawMetadata = raw.Bytes.Length > 0 || raw.HasTypeSignature
            || tag.Data.Kind == IccTagDataKind.Raw || tag.IsDecodeAttempted;
        if (hasRawMetadata && (!raw.HasTypeSignature || raw.TypeSignature != encodedType))
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagTypeCompatibility,
                IccValidationSeverity.Error,
                "The raw-data type signature does not match the signature encoded in the retained payload.",
                index, 0, 4, tag,
                "Set the raw-data type signature and presence flag from the first four payload bytes."));
        }

        if (!HasZeroReservedField(payload))
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagReserved,
                IccValidationSeverity.Error,
                "The four reserved bytes following the tag type signature are not all zero.",
                index, 4, 4, tag,
                "Set tag payload bytes 4 through 7 to zero."));
        }

        ValidateTypeRegistration(encodedType, tag, index, version, policy, issues);
        ValidateStringEncoding(encodedType, tag, index, version, payload, issues);
    }

    internal static void ValidateTypeRegistration(uint typeSignature, IccTag tag, int index,
        IccVersionKind version, IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool registered = IsRegisteredTypeSignature(typeSignature);
        bool registeredVendorType = typeSignature == IccTypeSignatures.VideoCardGammaTable
            || typeSignature == IccTypeSignatures.MicrosoftHardwareCalibration;
        if (registeredVendorType || (registered
            && TagCodecDispatcher.IsTypeSupportedByGeneration(typeSignature, version)))
        {
            return;
        }

        bool printable = HeaderValidationCore.IsPrintableSignature(typeSignature);
        IccValidationSeverity severity;
        string message;
        string remediation;
        if (registered)
        {
            severity = IccValidationSeverity.Error;
            message = $"Registered tag type '{FourCcCodec.Format(typeSignature)}' (0x{typeSignature:X8}) is not available for {TagCodecDispatcher.DescribeProfileGeneration(version)}.";
            remediation = "Use a tag type registered for the profile generation, or change the profile version consistently.";
        }
        else
        {
            severity = printable && policy.AllowPrivateSignatures
                ? IccValidationSeverity.Information : IccValidationSeverity.Error;
            message = printable
                ? $"Tag type '{FourCcCodec.Format(typeSignature)}' (0x{typeSignature:X8}) is private or is not present in the built-in ICC registration tables."
                : $"Tag type 0x{typeSignature:X8} is neither registered nor a printable private four-character signature.";
            remediation = printable
                ? policy.AllowPrivateSignatures
                    ? "Retain the complete opaque payload; register a codec when typed interpretation is required."
                    : "Use a registered tag type, or explicitly permit private signatures in the validation policy."
                : "Use a registered or printable private four-character type signature.";
        }

        issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagTypeCompatibility, severity,
            message, index, 0, 4, tag, remediation));
    }

    internal static void ValidateStringEncoding(uint typeSignature, IccTag tag, int index,
        IccVersionKind version, System.ReadOnlyMemory<byte> payload,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        System.ReadOnlyMemory<byte> content = payload.Slice(8);
        bool invalid = false;
        string message = string.Empty;
        if (typeSignature == IccTypeSignatures.Text)
        {
            invalid = content.Length == 0 || content.Span[content.Length - 1] != 0
                || !TagCodecSupport.IsSevenBitAscii(content);
            message = "The textType payload is not NULL-terminated 7-bit ASCII.";
        }
        else if (typeSignature == IccTypeSignatures.Utf8)
        {
            int start = Icc2TextTagCodec.HasUtf8Bom(content) ? 3 : 0;
            invalid = !TextTagCodec.IsValidUtf8(content.Slice(start), content.Length - start);
            message = "The utf8Type payload contains an invalid UTF-8 byte sequence.";
        }
        else if (typeSignature == IccTypeSignatures.Utf16)
        {
            int start = content.Length >= 2 && content.Span[0] == 254 && content.Span[1] == 255 ? 2 : 0;
            bool littleEndian = content.Length >= 2 && content.Span[0] == 255 && content.Span[1] == 254;
            System.ReadOnlyMemory<byte> textBytes = content.Slice(start);
            invalid = littleEndian || (textBytes.Length & 1) != 0
                || !LocalizedUnicodeTagCodec.IsValidUtf16BigEndian(textBytes);
            message = "The utf16Type payload is not valid big-endian UTF-16.";
        }
        else if (typeSignature == IccTypeSignatures.Data)
        {
            ValidateDataStringEncoding(tag, index, version, payload, issues);
            return;
        }

        if (invalid)
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TextEncoding,
                IccValidationSeverity.Error, message, index, 8, content.Length, tag,
                "Encode the string with the required character encoding, byte order, and terminator."));
        }
    }

    internal static void ValidateDataStringEncoding(IccTag tag, int index, IccVersionKind version,
        System.ReadOnlyMemory<byte> payload,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (payload.Length < 12)
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "The dataType payload is shorter than its 12-byte header.", index, 0,
                payload.Length, tag,
                "Provide the type header, data flag, and complete data bytes."));
            return;
        }

        uint dataFlag = ReadUInt32(payload, 8);
        if (dataFlag != 0)
        {
            return;
        }

        System.ReadOnlyMemory<byte> content = payload.Slice(12);
        bool invalid = content.Length == 0 || content.Span[content.Length - 1] != 0;
        if (!invalid && version == IccVersionKind.Version5)
        {
            invalid = !TextTagCodec.IsValidUtf8(content, content.Length - 1);
        }
        else if (!invalid)
        {
            invalid = !TagCodecSupport.IsSevenBitAscii(content);
        }

        if (invalid)
        {
            string encoding = version == IccVersionKind.Version5 ? "UTF-8" : "7-bit ASCII";
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.TextEncoding,
                IccValidationSeverity.Error,
                $"Textual dataType content is not NULL-terminated valid {encoding}.",
                index, 12, content.Length, tag,
                $"Encode textual dataType content as NULL-terminated {encoding}."));
        }
    }

    internal static void ValidateCompressedModel(IccTag tag, int index,
        IccValidationPolicy policy, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (tag.Data == null || tag.Data.Text == null || !tag.Data.Text.IsCompressed
            || tag.Data.Text.Bytes == null)
        {
            return;
        }

        int expandedSize = tag.Data.Text.Bytes.Length;
        if (expandedSize > policy.ResourceLimits.MaximumDecompressedBytes)
        {
            issues.Add(CreatePayloadIssue(IccValidationRuleIds.CompressionLimit,
                IccValidationSeverity.Fatal,
                $"The decoded compressed text contains {expandedSize} bytes, exceeding the configured decompression maximum of {policy.ResourceLimits.MaximumDecompressedBytes} bytes.",
                index, 8, GetPayloadLength(tag) - 8, tag,
                "Reduce the expanded content or explicitly raise the trusted decompression limit."));
        }
    }

    internal static System.ReadOnlyMemory<byte> ResolvePayload(IccTag tag)
    {
        if (tag.Data != null && tag.Data.RawData != null && tag.Data.RawData.Bytes != null
            && tag.Data.RawData.Bytes.Length > 0)
        {
            return tag.Data.RawData.Bytes;
        }

        if (tag.DecodeState != null && tag.DecodeState.PayloadOffset >= 0
            && tag.DecodeState.PayloadSize >= 0
            && tag.DecodeState.PayloadOffset <= tag.DecodeState.Source.ProfileBytes.Length
            && tag.DecodeState.PayloadSize <= tag.DecodeState.Source.ProfileBytes.Length - tag.DecodeState.PayloadOffset)
        {
            return tag.DecodeState.Source.ProfileBytes.Slice(tag.DecodeState.PayloadOffset,
                tag.DecodeState.PayloadSize);
        }

        return System.ReadOnlyMemory<byte>.Empty;
    }

    internal static bool IsRegisteredTagSignature(uint signature)
    {
        return SignatureDatabase.Tags.ContainsKey(signature)
            || VendorTagCodec.IsMicrosoftPrivateSignature(signature);
    }

    internal static bool IsRegisteredTypeSignature(uint signature)
    {
        return SignatureDatabase.Types.ContainsKey(signature);
    }

    internal static bool HasZeroReservedField(System.ReadOnlyMemory<byte> payload)
    {
        return payload.Span[4] == 0 && payload.Span[5] == 0
            && payload.Span[6] == 0 && payload.Span[7] == 0;
    }

    internal static uint ReadUInt32(System.ReadOnlyMemory<byte> bytes, int offset)
    {
        System.ReadOnlySpan<byte> values = bytes.Span;
        return ((uint)values[offset] << 24) | ((uint)values[offset + 1] << 16)
            | ((uint)values[offset + 2] << 8) | values[offset + 3];
    }

    internal static int GetPayloadLength(IccTag tag)
    {
        if (tag.TableEntry == null || tag.TableEntry.Size < 0)
        {
            return 0;
        }
        return tag.TableEntry.Size;
    }

    internal static IccValidationIssue CreatePayloadIssue(string ruleId,
        IccValidationSeverity severity, string message, int index, int relativeOffset,
        int length, IccTag tag, string remediation)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = ruleId;
        issue.Severity = severity;
        issue.Message = message;
        issue.Path = relativeOffset == 0 && System.String.Equals(ruleId,
                IccValidationRuleIds.TagTypeCompatibility, System.StringComparison.Ordinal)
                ? $"Tags[{index}].Data.RawData.TypeSignature"
                : relativeOffset == 4 ? $"Tags[{index}].Data.Reserved"
                : $"Tags[{index}].Data";
        issue.Remediation = remediation;
        if (tag.TableEntry != null && tag.TableEntry.Offset >= 0)
        {
            int payloadOffset = relativeOffset < 0 ? 0 : relativeOffset;
            if (payloadOffset <= long.MaxValue - tag.TableEntry.Offset)
            {
                issue.Location.Offset = tag.TableEntry.Offset + payloadOffset;
                issue.Location.Length = length < 0 ? 0 : length;
            }
        }
        return issue;
    }

    internal static IccValidationIssue CreateTagSignatureIssue(IccValidationSeverity severity,
        string message, int index, long diagnosticBase, IccTag tag, string remediation)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.TagTypeCompatibility;
        issue.Severity = severity;
        issue.Message = message;
        issue.Path = $"Tags[{index}].Signature";
        issue.Remediation = remediation;
        int entryIndex = tag.TableEntry != null && tag.TableEntry.OriginalIndex >= 0
            ? tag.TableEntry.OriginalIndex : index;
        if (diagnosticBase >= 0)
        {
            issue.Location.Offset = diagnosticBase + IccConstants.HeaderSize
                + IccConstants.TagCountSize + ((long)entryIndex * IccConstants.TagTableEntrySize);
            issue.Location.Length = 4;
        }
        return issue;
    }
}
