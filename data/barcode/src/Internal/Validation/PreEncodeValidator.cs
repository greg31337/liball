namespace barcode;

internal class PreEncodeValidationState
{
    internal BarcodeValidationMode Mode;
    internal BarcodeFormat Format;
    internal string Standard = "Barcode encoding";
    internal string? Profile;
    internal global::System.Collections.Generic.List<BarcodeValidationIssue> Issues;

    internal PreEncodeValidationState(BarcodeValidationMode mode)
    {
        Mode = mode;
        Issues = new global::System.Collections.Generic.List<BarcodeValidationIssue>();
    }
}

internal static class PreEncodeValidator
{
    internal const int MaximumGs1128DataCharacterCount = 48;

    internal static BarcodeValidationReport Validate(
        BarcodeValidationRequest request)
    {
        if (request == null)
        {
            throw new BarcodeValidationException(
                "A barcode data validation request is required.");
        }

        BarcodeValidationMode mode = request.Mode;
        if (mode != BarcodeValidationMode.Strict &&
            mode != BarcodeValidationMode.Tolerant)
        {
            throw new BarcodeValidationException(
                "Barcode validation mode is invalid.");
        }

        PreEncodeValidationState state = new PreEncodeValidationState(mode);
        BarcodeEncodeRequest? data = request.Data;
        if (data == null)
        {
            AddIssue(
                state,
                "ENCODE_REQUEST_MISSING",
                "An encode request is required for pre-encode validation.",
                "Request",
                -1,
                "a barcode encode request",
                "null");
            return CreateReport(state);
        }

        BarcodeFormat format = ResolveFormat(state, request, data);
        state.Format = format;
        ResolveStandardAndProfile(state, format);
        ValidateFormatAgreement(state, request.Format, data.Format);
        if (!FormatDispatcher.IsSupportedFormat(format))
        {
            AddIssue(
                state,
                "ENCODE_FORMAT_UNSUPPORTED",
                "The requested barcode format is unsupported.",
                "Format",
                -1,
                "a supported barcode format",
                ((int)format).ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
            return CreateReport(state);
        }

        BarcodePayload? payload = data.Payload;
        ValidatePayload(state, payload);
        ValidateCommonOptions(state, data.Options, format);
        ValidateApplicableOptions(state, data, format);
        if (payload != null && HasPayloadData(payload))
        {
            ValidateCharacterEncoding(state, payload, data.Options, format);
            ValidatePayloadEciSegments(state, payload, format);
            DispatchFormatValidation(state, data, payload, format);
        }

        return CreateReport(state);
    }

    static BarcodeFormat ResolveFormat(
        PreEncodeValidationState state,
        BarcodeValidationRequest request,
        BarcodeEncodeRequest data)
    {
        if (request.Format != BarcodeFormat.Automatic)
        {
            return request.Format;
        }

        if (data.Format != BarcodeFormat.Automatic)
        {
            return data.Format;
        }

        BarcodePayload? payload = data.Payload;
        if (payload == null)
        {
            return BarcodeFormat.Code128;
        }

        if (payload.IsGs1)
        {
            return payload.RawBytes != null && payload.RawBytes.Length > 0
                ? BarcodeFormat.Gs1DataMatrix
                : BarcodeFormat.Gs1128;
        }

        return payload.RawBytes != null && payload.RawBytes.Length > 0
            ? BarcodeFormat.DataMatrix
            : BarcodeFormat.Code128;
    }

    static void ResolveStandardAndProfile(
        PreEncodeValidationState state,
        BarcodeFormat format)
    {
        if (format == BarcodeFormat.Code128 || format == BarcodeFormat.Gs1128)
        {
            state.Standard = BarcodeStandards.Code128;
        }
        else if (format == BarcodeFormat.Ean8 || format == BarcodeFormat.Ean13 ||
            format == BarcodeFormat.UpcA || format == BarcodeFormat.UpcE ||
            format == BarcodeFormat.EanUpcAddon2 ||
            format == BarcodeFormat.EanUpcAddon5)
        {
            state.Standard = BarcodeStandards.EanUpc;
        }
        else if (format == BarcodeFormat.Code39)
        {
            state.Standard = BarcodeStandards.Code39;
        }
        else if (format == BarcodeFormat.Itf || format == BarcodeFormat.Itf14)
        {
            state.Standard = BarcodeStandards.Itf;
        }
        else if (format == BarcodeFormat.Codabar)
        {
            state.Standard = BarcodeStandards.Codabar;
        }
        else if (format == BarcodeFormat.DataMatrix ||
            format == BarcodeFormat.Gs1DataMatrix)
        {
            state.Standard = BarcodeStandards.DataMatrix;
        }
        else if (format == BarcodeFormat.Pdf417 ||
            format == BarcodeFormat.CompactPdf417 ||
            format == BarcodeFormat.MacroPdf417)
        {
            state.Standard = BarcodeStandards.Pdf417;
        }
        else
        {
            state.Standard = "Barcode format selection";
        }

        if (format == BarcodeFormat.Gs1128 ||
            format == BarcodeFormat.Gs1DataMatrix ||
            format == BarcodeFormat.Itf14)
        {
            state.Profile = BarcodeStandards.Gs1GeneralSpecifications;
        }
    }

    static void ValidateFormatAgreement(
        PreEncodeValidationState state,
        BarcodeFormat validationFormat,
        BarcodeFormat dataFormat)
    {
        if (validationFormat != BarcodeFormat.Automatic &&
            dataFormat != BarcodeFormat.Automatic &&
            validationFormat != dataFormat)
        {
            AddIssue(
                state,
                "ENCODE_FORMAT_CONFLICT",
                "The validation and encode request formats conflict.",
                "Format",
                -1,
                validationFormat.ToString(),
                dataFormat.ToString());
        }
    }

    static void ValidatePayload(
        PreEncodeValidationState state,
        BarcodePayload? payload)
    {
        if (payload == null)
        {
            AddIssue(
                state,
                "ENCODE_PAYLOAD_MISSING",
                "A barcode payload is required.",
                "Payload",
                -1,
                "text or raw bytes",
                "null");
            return;
        }

        if (!HasPayloadData(payload))
        {
            AddIssue(
                state,
                "ENCODE_PAYLOAD_EMPTY",
                "The barcode payload must not be empty.",
                "Payload",
                0,
                "at least one data value",
                "empty");
        }

        if (payload.StructuredAppend != null &&
            state.Format != BarcodeFormat.DataMatrix &&
            state.Format != BarcodeFormat.Gs1DataMatrix &&
            state.Format != BarcodeFormat.MacroPdf417)
        {
            AddIssue(
                state,
                "ENCODE_PAYLOAD_APPEND_NOT_APPLICABLE",
                "Payload structured-append metadata is not applicable to the selected format.",
                "Payload.StructuredAppend",
                -1,
                "no structured-append metadata",
                state.Format.ToString());
        }
    }

    static void ValidateCommonOptions(
        PreEncodeValidationState state,
        BarcodeEncodeOptions? options,
        BarcodeFormat format)
    {
        if (options == null)
        {
            return;
        }

        if (!IsChecksumModeValid(options.ChecksumMode))
        {
            AddInvalidEnumIssue(
                state,
                "ENCODE_CHECKSUM_MODE_INVALID",
                "ChecksumMode",
                (int)options.ChecksumMode);
        }

        bool configurableChecksum = format == BarcodeFormat.Ean8 ||
            format == BarcodeFormat.Ean13 || format == BarcodeFormat.UpcA ||
            format == BarcodeFormat.UpcE || format == BarcodeFormat.Code39 ||
            format == BarcodeFormat.Itf || format == BarcodeFormat.Itf14 ||
            format == BarcodeFormat.Codabar;
        if (options.ChecksumMode != BarcodeChecksumMode.None &&
            IsChecksumModeValid(options.ChecksumMode) && !configurableChecksum)
        {
            AddIssue(
                state,
                "ENCODE_CHECKSUM_MODE_NOT_APPLICABLE",
                "The selected format has no configurable pre-encode checksum policy.",
                "ChecksumMode",
                -1,
                BarcodeChecksumMode.None.ToString(),
                options.ChecksumMode.ToString());
        }

        if (options.TextMode != BarcodeTextMode.Hidden &&
            options.TextMode != BarcodeTextMode.Standard &&
            options.TextMode != BarcodeTextMode.RawPayload &&
            options.TextMode != BarcodeTextMode.Custom)
        {
            AddInvalidEnumIssue(
                state,
                "ENCODE_TEXT_MODE_INVALID",
                "TextMode",
                (int)options.TextMode);
        }
        else if (options.TextMode == BarcodeTextMode.Custom &&
            options.CustomText == null)
        {
            AddIssue(
                state,
                "ENCODE_CUSTOM_TEXT_MISSING",
                "Custom human-readable text is required by the selected text mode.",
                "CustomText",
                -1,
                "caller-supplied text",
                "null");
        }

        if (options.EciAssignmentNumber < 0)
        {
            AddIssue(
                state,
                "ENCODE_ECI_NEGATIVE",
                "An ECI assignment number must not be negative.",
                "EciAssignmentNumber",
                -1,
                "zero or a supported positive assignment",
                options.EciAssignmentNumber.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        bool supportsEci = format == BarcodeFormat.DataMatrix ||
            format == BarcodeFormat.Pdf417 ||
            format == BarcodeFormat.CompactPdf417 ||
            format == BarcodeFormat.MacroPdf417;
        if (options.EciAssignmentNumber > 0 && !supportsEci)
        {
            AddIssue(
                state,
                "ENCODE_ECI_NOT_APPLICABLE",
                "The selected barcode format does not support an ECI assignment through this API.",
                "EciAssignmentNumber",
                -1,
                "zero",
                options.EciAssignmentNumber.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        bool supportsAppend = format == BarcodeFormat.DataMatrix ||
            format == BarcodeFormat.Gs1DataMatrix ||
            format == BarcodeFormat.MacroPdf417;
        if (options.StructuredAppend != null && !supportsAppend)
        {
            AddIssue(
                state,
                "ENCODE_STRUCTURED_APPEND_NOT_APPLICABLE",
                "Structured append is not applicable to the selected format.",
                "StructuredAppend",
                -1,
                "no structured-append metadata",
                format.ToString());
        }
    }

    static void ValidateApplicableOptions(
        PreEncodeValidationState state,
        BarcodeEncodeRequest data,
        BarcodeFormat format)
    {
        BarcodeCodecFamily family = FormatDispatcher.ResolveCodecFamily(format);
        if (data.Code128 != null && family != BarcodeCodecFamily.Code128)
        {
            AddOptionNotApplicable(state, "Code128", format);
        }

        if (data.EanUpc != null && family != BarcodeCodecFamily.EanUpc)
        {
            AddOptionNotApplicable(state, "EanUpc", format);
        }

        if (data.Code39 != null && family != BarcodeCodecFamily.Code39)
        {
            AddOptionNotApplicable(state, "Code39", format);
        }

        if (data.Itf != null && family != BarcodeCodecFamily.Itf)
        {
            AddOptionNotApplicable(state, "Itf", format);
        }

        if (data.Codabar != null && family != BarcodeCodecFamily.Codabar)
        {
            AddOptionNotApplicable(state, "Codabar", format);
        }

        if (data.DataMatrix != null && family != BarcodeCodecFamily.DataMatrix)
        {
            AddOptionNotApplicable(state, "DataMatrix", format);
        }

        if (data.Pdf417 != null && family != BarcodeCodecFamily.Pdf417)
        {
            AddOptionNotApplicable(state, "Pdf417", format);
        }


        StructuredAppendInfo? commonAppend = data.Options == null
            ? null
            : data.Options.StructuredAppend;
        StructuredAppendInfo? specificAppend = null;
        if (family == BarcodeCodecFamily.DataMatrix && data.DataMatrix != null)
        {
            specificAppend = data.DataMatrix.StructuredAppend;
        }
        else if (family == BarcodeCodecFamily.Pdf417 && data.Pdf417 != null)
        {
            specificAppend = data.Pdf417.Macro;
        }

        if (commonAppend != null && specificAppend != null &&
            !AreAppendValuesEqual(commonAppend, specificAppend))
        {
            AddIssue(
                state,
                "ENCODE_STRUCTURED_APPEND_CONFLICT",
                "Common and symbology-specific structured-append metadata conflict.",
                "StructuredAppend",
                -1,
                "matching metadata in both option blocks",
                "conflicting metadata");
        }

        StructuredAppendInfo? selectedAppend = specificAppend == null
            ? commonAppend
            : specificAppend;
        if (data.Payload != null && data.Payload.StructuredAppend != null &&
            selectedAppend != null &&
            !AreAppendValuesEqual(
                data.Payload.StructuredAppend,
                selectedAppend))
        {
            AddIssue(
                state,
                "ENCODE_PAYLOAD_APPEND_CONFLICT",
                "Payload and encoding-option structured-append metadata conflict.",
                "Payload.StructuredAppend",
                -1,
                "metadata matching the encoding options",
                "conflicting metadata");
        }

        int commonEci = data.Options == null
            ? 0
            : data.Options.EciAssignmentNumber;
        int specificEci = 0;
        if (family == BarcodeCodecFamily.DataMatrix && data.DataMatrix != null)
        {
            specificEci = data.DataMatrix.EciAssignmentNumber;
        }
        else if (family == BarcodeCodecFamily.Pdf417 && data.Pdf417 != null)
        {
            specificEci = data.Pdf417.EciAssignmentNumber;
        }

        if (commonEci != 0 && specificEci != 0 && commonEci != specificEci)
        {
            AddIssue(
                state,
                "ENCODE_ECI_CONFLICT",
                "Common and symbology-specific ECI assignments conflict.",
                "EciAssignmentNumber",
                -1,
                commonEci.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                specificEci.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void DispatchFormatValidation(
        PreEncodeValidationState state,
        BarcodeEncodeRequest data,
        BarcodePayload payload,
        BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Code128:
                ValidateCode128(state, payload, data.Code128);
                break;
            case BarcodeFormat.Gs1128:
                ValidateGs1Payload(state, payload, true);
                ValidateCode128Options(state, data.Code128);
                break;
            case BarcodeFormat.Ean8:
            case BarcodeFormat.Ean13:
            case BarcodeFormat.UpcA:
            case BarcodeFormat.UpcE:
            case BarcodeFormat.EanUpcAddon2:
            case BarcodeFormat.EanUpcAddon5:
                ValidateEanUpc(state, payload, data, format);
                break;
            case BarcodeFormat.Code39:
                ValidateCode39(state, payload, data);
                break;
            case BarcodeFormat.Itf:
            case BarcodeFormat.Itf14:
                ValidateItf(state, payload, data, format);
                break;
            case BarcodeFormat.Codabar:
                ValidateCodabar(state, payload, data);
                break;
            case BarcodeFormat.DataMatrix:
            case BarcodeFormat.Gs1DataMatrix:
                ValidateDataMatrix(state, payload, data, format);
                break;
            case BarcodeFormat.Pdf417:
            case BarcodeFormat.CompactPdf417:
            case BarcodeFormat.MacroPdf417:
                ValidatePdf417(state, payload, data, format);
                break;
        }
    }

    static void ValidateCode128(
        PreEncodeValidationState state,
        BarcodePayload payload,
        Code128Options? options)
    {
        ValidateCode128Options(state, options);
        ValidateCode128Characters(state, payload, options);
        int count = EstimateCode128CodewordCount(state, payload, options);
        if (count > Code128Encoder.MaximumPlannedCodewordCount)
        {
            AddIssue(
                state,
                "CODE128_CAPACITY_EXCEEDED",
                "The payload exceeds Code 128 symbol-character capacity.",
                "Payload",
                -1,
                $"at most {Code128Encoder.MaximumPlannedCodewordCount} planned codewords",
                count.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void ValidateCode128Characters(
        PreEncodeValidationState state,
        BarcodePayload payload,
        Code128Options? options)
    {
        Code128CodeSet codeSet = options == null
            ? Code128CodeSet.Automatic
            : options.CodeSet;
        if (codeSet == Code128CodeSet.C)
        {
            return;
        }

        bool upperShift = options != null && options.AllowFnc4 &&
            options.UseLatin1UpperShift;
        int length = GetPayloadDataLength(payload);
        int index = 0;
        while (index < length)
        {
            int value = GetPayloadDataValue(payload, index);
            bool valid;
            if (codeSet == Code128CodeSet.A)
            {
                valid = value >= 0 && value <= 95;
            }
            else if (codeSet == Code128CodeSet.B)
            {
                valid = value >= 32 && value <= 127;
            }
            else
            {
                valid = value >= 0 && value <= 127;
            }

            if (!valid && upperShift && value >= 128 && value <= 255)
            {
                valid = true;
            }
            if (!valid)
            {
                AddIssue(
                    state,
                    "CODE128_CODE_SET_CHARACTER_INVALID",
                    "The payload contains a value outside the forced Code 128 code set.",
                    "Payload",
                    index,
                    codeSet == Code128CodeSet.A
                        ? "a byte from 0 through 95"
                        : codeSet == Code128CodeSet.B
                            ? "a byte from 32 through 127"
                            : upperShift
                                ? "an ISO-8859-1 byte"
                                : "a seven-bit byte",
                    value.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }

            index = index + 1;
        }
    }

    static void ValidateCode128Options(
        PreEncodeValidationState state,
        Code128Options? options)
    {
        if (options == null)
        {
            return;
        }

        if (options.CodeSet != Code128CodeSet.Automatic &&
            options.CodeSet != Code128CodeSet.A &&
            options.CodeSet != Code128CodeSet.B &&
            options.CodeSet != Code128CodeSet.C)
        {
            AddInvalidEnumIssue(
                state,
                "CODE128_CODE_SET_INVALID",
                "CodeSet",
                (int)options.CodeSet);
        }
    }

    static int EstimateCode128CodewordCount(
        PreEncodeValidationState state,
        BarcodePayload payload,
        Code128Options? options)
    {
        Code128CodeSet codeSet = options == null
            ? Code128CodeSet.Automatic
            : options.CodeSet;
        int length = GetPayloadDataLength(payload);
        if (codeSet == Code128CodeSet.C)
        {
            bool digits = true;
            int index = 0;
            while (index < length)
            {
                int value = GetPayloadDataValue(payload, index);
                if (value < '0' || value > '9')
                {
                    digits = false;
                }

                index = index + 1;
            }

            if (!digits || length % 2 != 0)
            {
                AddIssue(
                    state,
                    "CODE128_CODE_SET_C_DATA_INVALID",
                    "Forced Code Set C requires an even number of ASCII digits.",
                    "Payload",
                    -1,
                    "an even number of ASCII digits",
                    length.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }

            if (digits && length % 2 == 0)
            {
                return length / 2;
            }

            return length + 1;
        }

        if (codeSet == Code128CodeSet.A || codeSet == Code128CodeSet.B)
        {
            return length;
        }

        int count = 0;
        int source = 0;
        while (source < length)
        {
            int digitCount = 0;
            while (source + digitCount < length)
            {
                int value = GetPayloadDataValue(payload, source + digitCount);
                if (value < '0' || value > '9')
                {
                    break;
                }

                digitCount = digitCount + 1;
            }

            if (digitCount >= 4)
            {
                count = count + (digitCount / 2) + (digitCount % 2);
                source = source + digitCount;
            }
            else
            {
                count = count + 1;
                source = source + 1;
            }
        }

        return count;
    }

    static void ValidateGs1Payload(
        PreEncodeValidationState state,
        BarcodePayload payload,
        bool enforceGs1128Capacity)
    {
        string? text = payload.Text;
        if (text == null || text.Length == 0)
        {
            AddIssue(
                state,
                "GS1_ELEMENT_STRING_MISSING",
                "A GS1 element string is required for the selected profile.",
                "Payload.Text",
                0,
                "an unbracketed GS1 element string",
                "missing");
            return;
        }

        if (enforceGs1128Capacity &&
            text.Length > MaximumGs1128DataCharacterCount)
        {
            AddIssue(
                state,
                "GS1128_CAPACITY_EXCEEDED",
                "The GS1-128 element string exceeds its data-character limit.",
                "Payload.Text",
                -1,
                $"at most {MaximumGs1128DataCharacterCount} characters",
                text.Length.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementString(text);
        int index = 0;
        while (index < parsed.Issues.Length)
        {
            state.Issues.Add(parsed.Issues[index]);
            index = index + 1;
        }
    }

    static void ValidateEanUpc(
        PreEncodeValidationState state,
        BarcodePayload payload,
        BarcodeEncodeRequest data,
        BarcodeFormat format)
    {
        string? text = payload.Text;
        if (text == null)
        {
            AddTextRequiredIssue(state, format);
            return;
        }

        ValidateAsciiDigits(state, text, "Payload.Text", "EAN_UPC_NON_DIGIT");
        EanUpcOptions? options = data.EanUpc;
        BarcodeChecksumMode checksumMode = ResolveEanChecksumMode(
            data.Options,
            options);
        if (!IsChecksumModeValid(checksumMode))
        {
            AddInvalidEnumIssue(
                state,
                "EAN_UPC_CHECKSUM_MODE_INVALID",
                "ChecksumMode",
                (int)checksumMode);
        }

        if (format == BarcodeFormat.EanUpcAddon2 ||
            format == BarcodeFormat.EanUpcAddon5)
        {
            int expectedLength = format == BarcodeFormat.EanUpcAddon2 ? 2 : 5;
            ValidateExactLength(state, text, expectedLength, "EAN_UPC_ADDON_LENGTH_INVALID");
            if (options != null && options.AddOn != null)
            {
                AddIssue(
                    state,
                    "EAN_UPC_NESTED_ADDON_INVALID",
                    "A standalone add-on cannot contain another associated add-on.",
                    "EanUpc.AddOn",
                    -1,
                    "null",
                    options.AddOn);
            }

            return;
        }

        int dataLength = 0;
        int completeLength = 0;
        if (format == BarcodeFormat.Ean8)
        {
            dataLength = 7;
            completeLength = 8;
        }
        else if (format == BarcodeFormat.Ean13)
        {
            dataLength = 12;
            completeLength = 13;
        }
        else if (format == BarcodeFormat.UpcA)
        {
            dataLength = 11;
            completeLength = 12;
        }
        else
        {
            ValidateUpcE(state, text, options, checksumMode);
        }

        if (format != BarcodeFormat.UpcE)
        {
            if (text.Length != dataLength && text.Length != completeLength)
            {
                AddIssue(
                    state,
                    "EAN_UPC_LENGTH_INVALID",
                    "The EAN/UPC payload length is invalid for the selected format.",
                    "Payload.Text",
                    -1,
                    $"{dataLength} data digits or {completeLength} complete digits",
                    text.Length.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                ValidateSuppliedGs1CheckDigit(
                    state,
                    text,
                    dataLength,
                    checksumMode,
                    "EAN_UPC");
            }
        }

        ValidateEanAddOn(state, options, format);
        ValidateUpcCompression(state, text, options, format, checksumMode);
    }

    static void ValidateUpcCompression(
        PreEncodeValidationState state,
        string text,
        EanUpcOptions? options,
        BarcodeFormat format,
        BarcodeChecksumMode checksumMode)
    {
        if (options == null || !options.CompressToUpcE)
        {
            return;
        }

        if (format != BarcodeFormat.UpcA)
        {
            AddIssue(
                state,
                "UPCE_COMPRESSION_NOT_APPLICABLE",
                "UPC-E compression may be requested only for a UPC-A payload.",
                "EanUpc.CompressToUpcE",
                -1,
                BarcodeFormat.UpcA.ToString(),
                format.ToString());
            return;
        }

        if (!IsAsciiDigits(text) || (text.Length != 11 && text.Length != 12))
        {
            return;
        }

        string complete = text;
        if (text.Length == 11 && !IsVerificationMode(checksumMode))
        {
            complete = Gs1Modulo10.AppendCheckDigit(text);
        }

        if (complete.Length == 12 && Gs1Modulo10.VerifyCompleteDigits(complete) &&
            (complete[0] == '0' || complete[0] == '1') &&
            UpcECompressor.TryCompressCompleteUpcA(complete) == null)
        {
            AddIssue(
                state,
                "UPCE_COMPRESSION_NOT_LOSSLESS",
                "The UPC-A value cannot be represented losslessly as UPC-E.",
                "Payload.Text",
                -1,
                "a UPC-E-compressible UPC-A value",
                complete);
        }
    }

    static BarcodeChecksumMode ResolveEanChecksumMode(
        BarcodeEncodeOptions? common,
        EanUpcOptions? options)
    {
        return options == null
            ? (common == null ? BarcodeChecksumMode.None : common.ChecksumMode)
            : options.ChecksumMode;
    }

    static void ValidateUpcE(
        PreEncodeValidationState state,
        string text,
        EanUpcOptions? options,
        BarcodeChecksumMode checksumMode)
    {
        int numberSystem = options == null ? 0 : options.UpcENumberSystem;
        if (numberSystem != 0 && numberSystem != 1)
        {
            AddIssue(
                state,
                "UPCE_NUMBER_SYSTEM_INVALID",
                "UPC-E number system must be zero or one.",
                "EanUpc.UpcENumberSystem",
                -1,
                "0 or 1",
                numberSystem.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (text.Length != 6 && text.Length != 7 && text.Length != 8)
        {
            AddIssue(
                state,
                "UPCE_LENGTH_INVALID",
                "UPC-E requires six payload digits, seven core digits, or eight complete digits.",
                "Payload.Text",
                -1,
                "6, 7, or 8 digits",
                text.Length.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        if (text.Length >= 7 && numberSystem >= 0 && numberSystem <= 1 &&
            text[0] != (char)('0' + numberSystem))
        {
            AddIssue(
                state,
                "UPCE_NUMBER_SYSTEM_CONFLICT",
                "UPC-E input number system conflicts with the selected option.",
                "Payload.Text",
                0,
                numberSystem.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                text.Substring(0, 1));
        }

        if (IsVerificationMode(checksumMode) && text.Length < 8)
        {
            AddIssue(
                state,
                "UPCE_CHECK_DIGIT_MISSING",
                "The selected checksum mode requires a complete UPC-E value with check digit.",
                "Payload.Text",
                text.Length,
                "8 complete digits",
                text);
            return;
        }

        ValidateSuppliedGs1CheckDigit(
            state,
            text,
            7,
            checksumMode,
            "UPCE");
    }

    static void ValidateSuppliedGs1CheckDigit(
        PreEncodeValidationState state,
        string text,
        int dataLength,
        BarcodeChecksumMode checksumMode,
        string prefix)
    {
        bool requiresSupplied = checksumMode == BarcodeChecksumMode.Require ||
            checksumMode == BarcodeChecksumMode.VerifyKeep ||
            checksumMode == BarcodeChecksumMode.VerifyStrip;
        if (text.Length == dataLength && requiresSupplied)
        {
            AddIssue(
                state,
                prefix + "_CHECK_DIGIT_MISSING",
                "The selected checksum mode requires a supplied check digit.",
                "Payload.Text",
                text.Length,
                "a complete value with check digit",
                text);
        }
        else if (text.Length == dataLength + 1 && IsAsciiDigits(text) &&
            !Gs1Modulo10.VerifyCompleteDigits(text))
        {
            AddIssue(
                state,
                prefix + "_CHECK_DIGIT_INVALID",
                "The supplied GS1 modulo-10 check digit is invalid.",
                "Payload.Text",
                text.Length - 1,
                Gs1Modulo10.CalculateCheckDigit(
                    text.Substring(0, text.Length - 1)).ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                text.Substring(text.Length - 1, 1));
        }
    }

    static void ValidateEanAddOn(
        PreEncodeValidationState state,
        EanUpcOptions? options,
        BarcodeFormat format)
    {
        if (options == null)
        {
            return;
        }

        if (format != BarcodeFormat.UpcE && options.UpcENumberSystem != 0)
        {
            AddIssue(
                state,
                "UPCE_NUMBER_SYSTEM_NOT_APPLICABLE",
                "The UPC-E number-system option is not applicable to the selected format.",
                "EanUpc.UpcENumberSystem",
                -1,
                "0",
                options.UpcENumberSystem.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (options.AllowedAddOnLengths != null)
        {
            if (options.AllowedAddOnLengths.Length == 0)
            {
                AddIssue(
                    state,
                    "EAN_UPC_ADDON_LENGTH_POLICY_EMPTY",
                    "Allowed add-on lengths must not be empty when supplied.",
                    "EanUpc.AllowedAddOnLengths",
                    -1,
                    "2 and/or 5",
                    "empty");
            }

            int index = 0;
            while (index < options.AllowedAddOnLengths.Length)
            {
                int length = options.AllowedAddOnLengths[index];
                if (length != 2 && length != 5)
                {
                    AddIssue(
                        state,
                        "EAN_UPC_ADDON_LENGTH_POLICY_INVALID",
                        "Allowed add-on lengths may contain only two and five.",
                        "EanUpc.AllowedAddOnLengths",
                        index,
                        "2 or 5",
                        length.ToString(
                            global::System.Globalization.CultureInfo.InvariantCulture));
                }

                index = index + 1;
            }
        }

        bool supportsAddOn = format == BarcodeFormat.Ean13 ||
            format == BarcodeFormat.UpcA || format == BarcodeFormat.UpcE;
        if ((options.AddOn != null || options.RequireAddOn) && !supportsAddOn)
        {
            AddIssue(
                state,
                "EAN_UPC_ADDON_NOT_APPLICABLE",
                "An associated add-on is not permitted for the selected main format.",
                "EanUpc.AddOn",
                -1,
                "no associated add-on",
                format.ToString());
            return;
        }

        if (options.RequireAddOn && options.AddOn == null)
        {
            AddIssue(
                state,
                "EAN_UPC_ADDON_REQUIRED",
                "An EAN/UPC add-on is required by the selected policy.",
                "EanUpc.AddOn",
                -1,
                "two or five digits",
                "null");
        }

        if (options.AddOn != null)
        {
            ValidateAsciiDigits(
                state,
                options.AddOn,
                "EanUpc.AddOn",
                "EAN_UPC_ADDON_NON_DIGIT");
            if (options.AddOn.Length != 2 && options.AddOn.Length != 5)
            {
                AddIssue(
                    state,
                    "EAN_UPC_ADDON_LENGTH_INVALID",
                    "An EAN/UPC add-on must contain exactly two or five digits.",
                    "EanUpc.AddOn",
                    -1,
                    "2 or 5 digits",
                    options.AddOn.Length.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (options.AllowedAddOnLengths != null &&
                !ContainsInteger(
                    options.AllowedAddOnLengths,
                    options.AddOn.Length))
            {
                AddIssue(
                    state,
                    "EAN_UPC_ADDON_LENGTH_NOT_ALLOWED",
                    "The add-on length is not permitted by the selected policy.",
                    "EanUpc.AddOn",
                    -1,
                    "an allowed add-on length",
                    options.AddOn.Length.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    static void ValidateCode39(
        PreEncodeValidationState state,
        BarcodePayload payload,
        BarcodeEncodeRequest data)
    {
        string? text = payload.Text;
        if (text == null)
        {
            AddTextRequiredIssue(state, BarcodeFormat.Code39);
            return;
        }

        Code39Options settings = data.Code39 == null
            ? new Code39Options()
            : data.Code39;
        if (settings.CharacterMode != Code39CharacterMode.Standard &&
            settings.CharacterMode != Code39CharacterMode.FullAscii)
        {
            AddInvalidEnumIssue(
                state,
                "CODE39_CHARACTER_MODE_INVALID",
                "Code39.CharacterMode",
                (int)settings.CharacterMode);
            return;
        }

        if (settings.WideToNarrowRatio != 0.0 &&
            (double.IsNaN(settings.WideToNarrowRatio) ||
             double.IsInfinity(settings.WideToNarrowRatio) ||
             settings.WideToNarrowRatio < 2.0 ||
             settings.WideToNarrowRatio > 3.0))
        {
            AddIssue(
                state,
                "CODE39_RATIO_INVALID",
                "Code 39 wide-to-narrow ratio must be from 2.0 through 3.0.",
                "Code39.WideToNarrowRatio",
                -1,
                "2.0 through 3.0",
                settings.WideToNarrowRatio.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (settings.IntercharacterGapModules < 0)
        {
            AddIssue(
                state,
                "CODE39_GAP_INVALID",
                "Code 39 intercharacter gap must not be negative.",
                "Code39.IntercharacterGapModules",
                -1,
                "zero or a positive module count",
                settings.IntercharacterGapModules.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        BarcodeChecksumMode checksumMode = data.Code39 == null
            ? ResolveCommonChecksumMode(data.Options)
            : settings.ChecksumMode;
        if (!IsChecksumModeValid(checksumMode))
        {
            AddInvalidEnumIssue(
                state,
                "CODE39_CHECKSUM_MODE_INVALID",
                "ChecksumMode",
                (int)checksumMode);
        }

        int dataLength = text.Length;
        bool verify = IsVerificationMode(checksumMode);
        if (verify)
        {
            if (text.Length < 2)
            {
                AddIssue(
                    state,
                    "CODE39_CHECK_CHARACTER_MISSING",
                    "Code 39 verification requires data and a supplied check character.",
                    "Payload.Text",
                    text.Length,
                    "data followed by a check character",
                    text);
                return;
            }

            dataLength = text.Length - 1;
        }

        bool validCharacters = true;
        int index = 0;
        while (index < dataLength)
        {
            char character = text[index];
            bool valid = settings.CharacterMode == Code39CharacterMode.Standard
                ? Code39Patterns.IsStandardDataCharacter(character)
                : character <= 127;
            if (!valid || character == Code39Patterns.StartStopCharacter)
            {
                validCharacters = false;
                AddIssue(
                    state,
                    "CODE39_CHARACTER_INVALID",
                    "The payload contains a character outside the selected Code 39 repertoire.",
                    "Payload.Text",
                    index,
                    settings.CharacterMode == Code39CharacterMode.Standard
                        ? "a standard Code 39 data character"
                        : "a seven-bit ASCII character",
                    ((int)character).ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }

            index = index + 1;
        }

        if (verify && validCharacters)
        {
            string source = text.Substring(0, dataLength);
            Code39EncodedData encoded = Code39Encoder.Encode(
                source,
                settings.CharacterMode);
            char observed = text[text.Length - 1];
            char expected = Code39Modulo43.CalculateCheckCharacter(
                encoded.EncodedText);
            if (observed != expected)
            {
                AddIssue(
                    state,
                    "CODE39_CHECK_CHARACTER_INVALID",
                    "The supplied Code 39 check character is invalid.",
                    "Payload.Text",
                    text.Length - 1,
                    expected.ToString(),
                    observed.ToString());
            }
        }
    }

    static void ValidateItf(
        PreEncodeValidationState state,
        BarcodePayload payload,
        BarcodeEncodeRequest data,
        BarcodeFormat format)
    {
        string? text = payload.Text;
        if (text == null)
        {
            AddTextRequiredIssue(state, format);
            return;
        }

        ValidateAsciiDigits(state, text, "Payload.Text", "ITF_NON_DIGIT");
        ItfOptions settings = data.Itf == null ? new ItfOptions() : data.Itf;
        BarcodeChecksumMode checksumMode = data.Itf == null
            ? ResolveCommonChecksumMode(data.Options)
            : settings.ChecksumMode;
        if (!IsChecksumModeValid(checksumMode))
        {
            AddInvalidEnumIssue(
                state,
                "ITF_CHECKSUM_MODE_INVALID",
                "ChecksumMode",
                (int)checksumMode);
        }

        double minimumRatio = format == BarcodeFormat.Itf14 ? 2.25 : 2.0;
        if (settings.WideToNarrowRatio != 0.0 &&
            (double.IsNaN(settings.WideToNarrowRatio) ||
             double.IsInfinity(settings.WideToNarrowRatio) ||
             settings.WideToNarrowRatio < minimumRatio ||
             settings.WideToNarrowRatio > 3.0))
        {
            AddIssue(
                state,
                "ITF_RATIO_INVALID",
                "The ITF wide-to-narrow ratio is outside the selected profile range.",
                "Itf.WideToNarrowRatio",
                -1,
                $"{minimumRatio} through 3.0",
                settings.WideToNarrowRatio.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (format == BarcodeFormat.Itf14)
        {
            ValidateItf14(state, text, settings, checksumMode);
            return;
        }

        if (settings.UseBearerBars || settings.UseBearerBox ||
            settings.UseCorrugatedProfile ||
            settings.BearerBarThicknessModules != 0)
        {
            AddIssue(
                state,
                "ITF_PROFILE_OPTION_NOT_APPLICABLE",
                "Bearer bars and corrugated sizing require the ITF-14 profile.",
                "Itf",
                -1,
                "generic ITF options",
                "ITF-14 profile options");
        }

        int encodedLength = text.Length;
        if (checksumMode == BarcodeChecksumMode.Calculate)
        {
            if (text.Length % 2 != 1)
            {
                AddIssue(
                    state,
                    "ITF_CALCULATE_LENGTH_INVALID",
                    "ITF data must have odd length before a calculated check digit is appended.",
                    "Payload.Text",
                    -1,
                    "an odd number of digits",
                    text.Length.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }

            encodedLength = text.Length + 1;
        }
        else if (text.Length % 2 != 0)
        {
            AddIssue(
                state,
                "ITF_ENCODED_LENGTH_ODD",
                "ITF encoded text must contain an even number of digits.",
                "Payload.Text",
                -1,
                "an even number of digits",
                text.Length.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (IsVerificationMode(checksumMode) && text.Length < 2)
        {
            AddIssue(
                state,
                "ITF_CHECK_DIGIT_MISSING",
                "ITF verification requires data and a supplied check digit.",
                "Payload.Text",
                text.Length,
                "data and a check digit",
                text);
        }
        else if (IsVerificationMode(checksumMode) && IsAsciiDigits(text) &&
            !Gs1Modulo10.VerifyCompleteDigits(text))
        {
            AddInvalidCheckDigitIssue(state, text, "ITF_CHECK_DIGIT_INVALID");
        }

        ValidateItfAllowedLengths(state, settings.AllowedLengths, encodedLength, false);
    }

    static void ValidateItf14(
        PreEncodeValidationState state,
        string text,
        ItfOptions settings,
        BarcodeChecksumMode checksumMode)
    {
        if (checksumMode == BarcodeChecksumMode.VerifyStrip)
        {
            AddIssue(
                state,
                "ITF14_CHECK_DIGIT_STRIP_INVALID",
                "ITF-14 must retain its mandatory GS1 check digit.",
                "ChecksumMode",
                -1,
                "None, Calculate, Require, or VerifyKeep",
                checksumMode.ToString());
        }

        bool calculate = checksumMode == BarcodeChecksumMode.Calculate ||
            (checksumMode == BarcodeChecksumMode.None && text.Length == 13);
        bool verify = checksumMode == BarcodeChecksumMode.Require ||
            checksumMode == BarcodeChecksumMode.VerifyKeep ||
            (checksumMode == BarcodeChecksumMode.None && text.Length == 14);
        int expectedLength = calculate ? 13 : 14;
        if ((!calculate && !verify) || text.Length != expectedLength)
        {
            AddIssue(
                state,
                "ITF14_LENGTH_INVALID",
                "ITF-14 requires 13 data digits for calculation or a complete 14-digit GTIN.",
                "Payload.Text",
                -1,
                calculate ? "13 digits" : "14 digits",
                text.Length.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (verify && IsAsciiDigits(text) &&
            !Gs1Modulo10.VerifyCompleteDigits(text))
        {
            AddInvalidCheckDigitIssue(state, text, "ITF14_CHECK_DIGIT_INVALID");
        }

        ValidateItfAllowedLengths(state, settings.AllowedLengths, 14, true);
        if (settings.BearerBarThicknessModules < 0)
        {
            AddIssue(
                state,
                "ITF14_BEARER_THICKNESS_INVALID",
                "ITF-14 bearer-bar thickness must not be negative.",
                "Itf.BearerBarThicknessModules",
                -1,
                "zero or a positive module count",
                settings.BearerBarThicknessModules.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void ValidateItfAllowedLengths(
        PreEncodeValidationState state,
        int[]? lengths,
        int encodedLength,
        bool itf14)
    {
        if (lengths == null)
        {
            return;
        }

        if (lengths.Length == 0)
        {
            AddIssue(
                state,
                "ITF_LENGTH_POLICY_EMPTY",
                "ITF allowed lengths must not be empty when supplied.",
                "Itf.AllowedLengths",
                -1,
                itf14 ? "14" : "positive even lengths",
                "empty");
            return;
        }

        bool matched = false;
        int index = 0;
        while (index < lengths.Length)
        {
            int length = lengths[index];
            bool valid = itf14 ? length == 14 : length > 0 && length % 2 == 0;
            if (!valid)
            {
                AddIssue(
                    state,
                    "ITF_LENGTH_POLICY_INVALID",
                    "An ITF allowed length is invalid for the selected profile.",
                    "Itf.AllowedLengths",
                    index,
                    itf14 ? "14" : "a positive even length",
                    length.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }

            if (length == encodedLength)
            {
                matched = true;
            }

            index = index + 1;
        }

        if (!matched)
        {
            AddIssue(
                state,
                "ITF_LENGTH_NOT_ALLOWED",
                "The encoded ITF length is not permitted by the selected policy.",
                "Payload.Text",
                -1,
                "an allowed encoded length",
                encodedLength.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void ValidateCodabar(
        PreEncodeValidationState state,
        BarcodePayload payload,
        BarcodeEncodeRequest data)
    {
        string? text = payload.Text;
        if (text == null)
        {
            AddTextRequiredIssue(state, BarcodeFormat.Codabar);
            return;
        }

        CodabarOptions settings = data.Codabar == null
            ? new CodabarOptions()
            : data.Codabar;
        BarcodeChecksumMode checksumMode = data.Codabar == null
            ? ResolveCommonChecksumMode(data.Options)
            : settings.ChecksumMode;
        if (!IsChecksumModeValid(checksumMode))
        {
            AddInvalidEnumIssue(
                state,
                "CODABAR_CHECKSUM_MODE_INVALID",
                "ChecksumMode",
                (int)checksumMode);
        }

        if (settings.WideToNarrowRatio != 0.0 &&
            (double.IsNaN(settings.WideToNarrowRatio) ||
             double.IsInfinity(settings.WideToNarrowRatio) ||
             settings.WideToNarrowRatio < 2.0 ||
             settings.WideToNarrowRatio > 3.0))
        {
            AddIssue(
                state,
                "CODABAR_RATIO_INVALID",
                "Codabar wide-to-narrow ratio must be from 2.0 through 3.0.",
                "Codabar.WideToNarrowRatio",
                -1,
                "2.0 through 3.0",
                settings.WideToNarrowRatio.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (settings.IntercharacterGapModules < 0)
        {
            AddIssue(
                state,
                "CODABAR_GAP_INVALID",
                "Codabar intercharacter gap must not be negative.",
                "Codabar.IntercharacterGapModules",
                -1,
                "zero or a positive module count",
                settings.IntercharacterGapModules.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        ValidateCodabarTerminal(
            state,
            settings.StartCharacter,
            settings.AcceptAliases,
            "Codabar.StartCharacter");
        ValidateCodabarTerminal(
            state,
            settings.StopCharacter,
            settings.AcceptAliases,
            "Codabar.StopCharacter");

        bool firstTerminal = text.Length > 0 &&
            CodabarPatterns.IsStartStopInput(text[0], settings.AcceptAliases);
        bool lastTerminal = text.Length > 0 &&
            CodabarPatterns.IsStartStopInput(
                text[text.Length - 1],
                settings.AcceptAliases);
        if (firstTerminal != lastTerminal)
        {
            AddIssue(
                state,
                "CODABAR_FRAMING_INCOMPLETE",
                "Codabar text must contain both start and stop characters or neither.",
                "Payload.Text",
                -1,
                "both terminals or neither terminal",
                text);
        }

        int start = firstTerminal && lastTerminal ? 1 : 0;
        int end = firstTerminal && lastTerminal ? text.Length - 1 : text.Length;
        if (firstTerminal && lastTerminal && text.Length < 3)
        {
            AddIssue(
                state,
                "CODABAR_DATA_MISSING",
                "Framed Codabar text requires at least one data character.",
                "Payload.Text",
                1,
                "at least one data character",
                text);
            return;
        }

        int index = start;
        bool validData = true;
        while (index < end)
        {
            if (!CodabarPatterns.IsDataCharacter(text[index]))
            {
                validData = false;
                AddIssue(
                    state,
                    "CODABAR_CHARACTER_INVALID",
                    "The Codabar payload contains a character outside its data alphabet.",
                    "Payload.Text",
                    index,
                    "a Codabar data character",
                    text[index].ToString());
            }

            index = index + 1;
        }

        if (IsVerificationMode(checksumMode) && end - start < 2)
        {
            AddIssue(
                state,
                "CODABAR_CHECK_CHARACTER_MISSING",
                "Codabar verification requires data and a supplied check character.",
                "Payload.Text",
                end,
                "data and a check character",
                text);
        }
        else if (IsVerificationMode(checksumMode) && validData)
        {
            char startCharacter = firstTerminal
                ? CodabarPatterns.NormalizeStartStop(
                    text[0],
                    settings.AcceptAliases)
                : ResolveCodabarTerminalValue(
                    settings.StartCharacter,
                    settings.AcceptAliases);
            char stopCharacter = lastTerminal
                ? CodabarPatterns.NormalizeStartStop(
                    text[text.Length - 1],
                    settings.AcceptAliases)
                : ResolveCodabarTerminalValue(
                    settings.StopCharacter,
                    settings.AcceptAliases);
            string carrierData = text.Substring(start, end - start);
            string complete = startCharacter + carrierData + stopCharacter;
            if (!CodabarModulo16.VerifyCompleteText(complete))
            {
                string dataText = carrierData.Substring(
                    0,
                    carrierData.Length - 1);
                string framedData = startCharacter + dataText + stopCharacter;
                AddIssue(
                    state,
                    "CODABAR_CHECK_CHARACTER_INVALID",
                    "The supplied Codabar check character is invalid.",
                    "Payload.Text",
                    end - 1,
                    CodabarModulo16.CalculateCheckCharacter(
                        framedData).ToString(),
                    carrierData.Substring(carrierData.Length - 1, 1));
            }
        }

        if (firstTerminal && lastTerminal)
        {
            ValidateCodabarTerminalConflict(
                state,
                text[0],
                settings.StartCharacter,
                settings.AcceptAliases,
                "Codabar.StartCharacter",
                0);
            ValidateCodabarTerminalConflict(
                state,
                text[text.Length - 1],
                settings.StopCharacter,
                settings.AcceptAliases,
                "Codabar.StopCharacter",
                text.Length - 1);
        }
    }

    static char ResolveCodabarTerminalValue(
        string? selected,
        bool acceptAliases)
    {
        if (selected == null || selected.Length != 1 ||
            !CodabarPatterns.IsStartStopInput(selected[0], acceptAliases))
        {
            return CodabarEncoder.DefaultStartStopCharacter;
        }

        return CodabarPatterns.NormalizeStartStop(selected[0], acceptAliases);
    }

    static void ValidateCodabarTerminalConflict(
        PreEncodeValidationState state,
        char observed,
        string? selected,
        bool acceptAliases,
        string field,
        int offset)
    {
        if (selected == null || selected.Length != 1 ||
            !CodabarPatterns.IsStartStopInput(selected[0], acceptAliases))
        {
            return;
        }

        char expected = CodabarPatterns.NormalizeStartStop(
            selected[0],
            acceptAliases);
        char actual = CodabarPatterns.NormalizeStartStop(
            observed,
            acceptAliases);
        if (expected != actual)
        {
            AddIssue(
                state,
                "CODABAR_TERMINAL_CONFLICT",
                "The framed Codabar terminal conflicts with the selected option.",
                field,
                offset,
                expected.ToString(),
                actual.ToString());
        }
    }

    static void ValidateCodabarTerminal(
        PreEncodeValidationState state,
        string? terminal,
        bool acceptAliases,
        string field)
    {
        if (terminal == null)
        {
            return;
        }

        if (terminal.Length != 1 ||
            !CodabarPatterns.IsStartStopInput(terminal[0], acceptAliases))
        {
            AddIssue(
                state,
                "CODABAR_TERMINAL_INVALID",
                "A Codabar terminal must be one canonical terminal or an enabled alias.",
                field,
                -1,
                acceptAliases ? "A, B, C, D, T, N, *, or E" : "A, B, C, or D",
                terminal);
        }
    }

    static void ValidateDataMatrix(
        PreEncodeValidationState state,
        BarcodePayload payload,
        BarcodeEncodeRequest data,
        BarcodeFormat format)
    {
        DataMatrixOptions settings = data.DataMatrix == null
            ? new DataMatrixOptions()
            : data.DataMatrix;
        if (settings.Shape != DataMatrixShape.Automatic &&
            settings.Shape != DataMatrixShape.Square &&
            settings.Shape != DataMatrixShape.StandardRectangular)
        {
            AddInvalidEnumIssue(
                state,
                "DATAMATRIX_SHAPE_INVALID",
                "DataMatrix.Shape",
                (int)settings.Shape);
        }

        if (settings.EncodingMode != DataMatrixEncodingMode.Automatic &&
            settings.EncodingMode != DataMatrixEncodingMode.Ascii &&
            settings.EncodingMode != DataMatrixEncodingMode.C40 &&
            settings.EncodingMode != DataMatrixEncodingMode.Text &&
            settings.EncodingMode != DataMatrixEncodingMode.X12 &&
            settings.EncodingMode != DataMatrixEncodingMode.Edifact &&
            settings.EncodingMode != DataMatrixEncodingMode.Base256)
        {
            AddInvalidEnumIssue(
                state,
                "DATAMATRIX_ENCODING_MODE_INVALID",
                "DataMatrix.EncodingMode",
                (int)settings.EncodingMode);
        }

        if (settings.Rows < 0 || settings.Columns < 0)
        {
            AddIssue(
                state,
                "DATAMATRIX_DIMENSIONS_NEGATIVE",
                "Data Matrix requested dimensions must not be negative.",
                "DataMatrix.RowsColumns",
                -1,
                "zero or supported positive dimensions",
                $"{settings.Rows}x{settings.Columns}");
        }
        else if ((settings.Rows == 0) != (settings.Columns == 0))
        {
            AddIssue(
                state,
                "DATAMATRIX_DIMENSIONS_INCOMPLETE",
                "Data Matrix fixed dimensions require both rows and columns.",
                "DataMatrix.RowsColumns",
                -1,
                "both dimensions or neither dimension",
                $"{settings.Rows}x{settings.Columns}");
        }
        else if (settings.Rows > 0 && settings.Columns > 0)
        {
            DataMatrixSymbolInfo? symbol =
                DataMatrixSymbolInfoTable.FindSymbolByDimensions(
                    settings.Rows,
                    settings.Columns);
            if (symbol == null ||
                (settings.Shape == DataMatrixShape.Square && symbol.IsRectangular) ||
                (settings.Shape == DataMatrixShape.StandardRectangular &&
                 !symbol.IsRectangular))
            {
                AddIssue(
                    state,
                    "DATAMATRIX_DIMENSIONS_UNSUPPORTED",
                    "The requested Data Matrix dimensions and shape are unsupported.",
                    "DataMatrix.RowsColumns",
                    -1,
                    "a standard ECC 200 symbol size",
                    $"{settings.Rows}x{settings.Columns}");
            }
        }

        int eci = settings.EciAssignmentNumber != 0
            ? settings.EciAssignmentNumber
            : data.Options == null ? 0 : data.Options.EciAssignmentNumber;
        if (eci < 0 || eci > DataMatrixControlCodewordEncoder.MaximumEciAssignmentNumber)
        {
            AddIssue(
                state,
                "DATAMATRIX_ECI_INVALID",
                "Data Matrix ECI assignment is outside the supported range.",
                "DataMatrix.EciAssignmentNumber",
                -1,
                "0 through 999999",
                eci.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        bool isGs1 = format == BarcodeFormat.Gs1DataMatrix;
        if (isGs1)
        {
            ValidateGs1Payload(state, payload, false);
            if (eci != 0)
            {
                AddIssue(
                    state,
                    "GS1_DATAMATRIX_ECI_INVALID",
                    "GS1 DataMatrix element data does not use an ECI assignment.",
                    "DataMatrix.EciAssignmentNumber",
                    -1,
                    "0",
                    eci.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        if (settings.MacroType != 0 && settings.MacroType != 5 &&
            settings.MacroType != 6)
        {
            AddIssue(
                state,
                "DATAMATRIX_MACRO_TYPE_INVALID",
                "Data Matrix Macro type must be zero, 5, or 6.",
                "DataMatrix.MacroType",
                -1,
                "0, 5, or 6",
                settings.MacroType.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        StructuredAppendInfo? append = settings.StructuredAppend;
        if (append == null && data.Options != null)
        {
            append = data.Options.StructuredAppend;
        }

        if (append == null)
        {
            append = payload.StructuredAppend;
        }

        if (settings.ReaderProgramming && isGs1)
        {
            AddOptionConflict(state, "ReaderProgramming", "GS1 FNC1");
        }

        if (settings.ReaderProgramming && append != null)
        {
            AddOptionConflict(state, "ReaderProgramming", "StructuredAppend");
        }

        if (settings.ReaderProgramming && settings.MacroType != 0)
        {
            AddOptionConflict(state, "ReaderProgramming", "MacroType");
        }

        if (settings.MacroType != 0 && isGs1)
        {
            AddOptionConflict(state, "MacroType", "GS1 FNC1");
        }

        if (settings.MacroType != 0 && append != null)
        {
            AddOptionConflict(state, "MacroType", "StructuredAppend");
        }

        if (append != null)
        {
            ValidateDataMatrixAppend(state, append, format);
        }

        int length = GetPayloadDataLength(payload);
        if (length > DataMatrixMinimalEncoder.MaximumInputByteCount ||
            (settings.EncodingMode == DataMatrixEncodingMode.Base256 &&
             length > DataMatrixModeCodec.MaximumBase256DataCount))
        {
            AddIssue(
                state,
                "DATAMATRIX_CAPACITY_EXCEEDED",
                "The payload exceeds Data Matrix input capacity for the selected mode.",
                "Payload",
                -1,
                settings.EncodingMode == DataMatrixEncodingMode.Base256
                    ? $"at most {DataMatrixModeCodec.MaximumBase256DataCount} bytes"
                    : $"at most {DataMatrixMinimalEncoder.MaximumInputByteCount} bytes",
                length.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        ValidateDataMatrixCapacity(
            state,
            payload,
            settings,
            isGs1,
            append,
            eci);

        ValidateDataMatrixForcedCharacters(state, payload, settings.EncodingMode);
    }

    static void ValidateDataMatrixCapacity(
        PreEncodeValidationState state,
        BarcodePayload payload,
        DataMatrixOptions settings,
        bool isGs1,
        StructuredAppendInfo? append,
        int eciAssignmentNumber)
    {
        int required = EstimateDataMatrixCodewords(
            payload,
            settings.EncodingMode);
        if (isGs1)
        {
            required = required + 1;
        }

        if (append != null)
        {
            required = required + 4;
        }

        if (settings.MacroType != 0 || settings.ReaderProgramming)
        {
            required = required + 1;
        }

        if (eciAssignmentNumber > 0 && eciAssignmentNumber <= 126)
        {
            required = required + 2;
        }
        else if (eciAssignmentNumber > 126 && eciAssignmentNumber <= 16382)
        {
            required = required + 3;
        }
        else if (eciAssignmentNumber > 16382)
        {
            required = required + 4;
        }

        int capacity = DataMatrixSymbolInfoTable.MaximumDataCodewordCapacity;
        if (settings.Shape == DataMatrixShape.StandardRectangular)
        {
            capacity = 49;
        }

        if (settings.Rows > 0 && settings.Columns > 0)
        {
            DataMatrixSymbolInfo? fixedSymbol =
                DataMatrixSymbolInfoTable.FindSymbolByDimensions(
                    settings.Rows,
                    settings.Columns);
            if (fixedSymbol != null)
            {
                capacity = fixedSymbol.DataCodewordCapacity;
            }
        }

        if (required > capacity)
        {
            AddIssue(
                state,
                "DATAMATRIX_SYMBOL_CAPACITY_EXCEEDED",
                "The payload and controls exceed the selected Data Matrix symbol capacity.",
                "Payload",
                -1,
                $"at most {capacity} data codewords",
                required.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static int EstimateDataMatrixCodewords(
        BarcodePayload payload,
        DataMatrixEncodingMode mode)
    {
        int length = GetPayloadDataLength(payload);
        if (mode == DataMatrixEncodingMode.Base256)
        {
            return length + (length <= 249 ? 1 : 2);
        }

        if (mode == DataMatrixEncodingMode.Ascii)
        {
            int count = 0;
            int index = 0;
            while (index < length)
            {
                int first = GetPayloadDataValue(payload, index);
                if (index + 1 < length && first >= '0' && first <= '9')
                {
                    int second = GetPayloadDataValue(payload, index + 1);
                    if (second >= '0' && second <= '9')
                    {
                        count = count + 1;
                        index = index + 2;
                        continue;
                    }
                }

                count = count + (first > 127 ? 2 : 1);
                index = index + 1;
            }

            return count;
        }

        if (mode == DataMatrixEncodingMode.C40 ||
            mode == DataMatrixEncodingMode.Text ||
            mode == DataMatrixEncodingMode.X12)
        {
            return 1 + (((length + 2) / 3) * 2);
        }

        if (mode == DataMatrixEncodingMode.Edifact)
        {
            return 1 + (((length + 3) / 4) * 3);
        }

        return (length + 1) / 2;
    }

    static void ValidateDataMatrixAppend(
        PreEncodeValidationState state,
        StructuredAppendInfo append,
        BarcodeFormat format)
    {
        if (append.Format != BarcodeFormat.Automatic &&
            append.Format != format)
        {
            AddIssue(
                state,
                "DATAMATRIX_APPEND_FORMAT_INVALID",
                "Structured-append metadata is not for Data Matrix.",
                "StructuredAppend.Format",
                -1,
                $"Automatic or {format}",
                append.Format.ToString());
        }

        if (append.Count < 2 ||
            append.Count > DataMatrixControlCodewordEncoder.MaximumStructuredAppendCount)
        {
            AddIssue(
                state,
                "DATAMATRIX_APPEND_COUNT_INVALID",
                "Data Matrix structured-append count must be between 2 and 16.",
                "StructuredAppend.Count",
                -1,
                "2 through 16",
                append.Count.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (append.Index < 0 || append.Index >= append.Count)
        {
            AddIssue(
                state,
                "DATAMATRIX_APPEND_INDEX_INVALID",
                "Data Matrix structured-append index must be within the segment count.",
                "StructuredAppend.Index",
                -1,
                "a zero-based index below Count",
                append.Index.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (append.Count >= 2 &&
            append.Count <= DataMatrixControlCodewordEncoder.MaximumStructuredAppendCount &&
            append.Index >= 0 && append.Index < append.Count &&
            append.IsLastSegment != (append.Index == append.Count - 1))
        {
            AddIssue(
                state,
                "DATAMATRIX_APPEND_LAST_SEGMENT_CONFLICT",
                "Data Matrix last-segment state conflicts with index and count.",
                "StructuredAppend.IsLastSegment",
                -1,
                (append.Index == append.Count - 1).ToString(),
                append.IsLastSegment.ToString());
        }

        ValidateDataMatrixFileIdentifier(state, append.FileIdentifier);
    }

    static void ValidateDataMatrixFileIdentifier(
        PreEncodeValidationState state,
        string? identifier)
    {
        if (identifier == null || identifier.Length == 0)
        {
            return;
        }

        if (identifier.Length > 6 || !IsAsciiDigits(identifier))
        {
            AddIssue(
                state,
                "DATAMATRIX_APPEND_FILE_ID_INVALID",
                "The Data Matrix file identifier must contain at most six decimal digits.",
                "StructuredAppend.FileIdentifier",
                -1,
                "one through six decimal digits",
                identifier);
            return;
        }

        int value = ParseDecimal(identifier);
        bool valid = value >= 1 &&
            value <= DataMatrixControlCodewordEncoder.MaximumStructuredAppendFileId;
        if (identifier.Length == 6)
        {
            int first = ParseDecimal(identifier.Substring(0, 3));
            int second = ParseDecimal(identifier.Substring(3, 3));
            valid = first >= 1 && first <= 254 &&
                second >= 1 && second <= 254;
        }

        if (!valid)
        {
            AddIssue(
                state,
                "DATAMATRIX_APPEND_FILE_ID_RANGE_INVALID",
                "The Data Matrix file identifier is outside its encodable range.",
                "StructuredAppend.FileIdentifier",
                -1,
                "1 through 64516, with 001 through 254 components",
                identifier);
        }
    }

    static void ValidateDataMatrixForcedCharacters(
        PreEncodeValidationState state,
        BarcodePayload payload,
        DataMatrixEncodingMode mode)
    {
        if (mode != DataMatrixEncodingMode.X12 &&
            mode != DataMatrixEncodingMode.Edifact)
        {
            return;
        }

        int length = GetPayloadDataLength(payload);
        int index = 0;
        while (index < length)
        {
            int value = GetPayloadDataValue(payload, index);
            bool valid = mode == DataMatrixEncodingMode.X12
                ? IsDataMatrixX12Value(value)
                : value >= 32 && value <= 94;
            if (!valid)
            {
                AddIssue(
                    state,
                    mode == DataMatrixEncodingMode.X12
                        ? "DATAMATRIX_X12_CHARACTER_INVALID"
                        : "DATAMATRIX_EDIFACT_CHARACTER_INVALID",
                    "The payload contains a byte outside the forced Data Matrix mode repertoire.",
                    "Payload",
                    index,
                    mode == DataMatrixEncodingMode.X12
                        ? "an ANSI X12 character"
                        : "an EDIFACT character from 32 through 94",
                    value.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }

            index = index + 1;
        }
    }

    static void ValidatePdf417(
        PreEncodeValidationState state,
        BarcodePayload payload,
        BarcodeEncodeRequest data,
        BarcodeFormat format)
    {
        Pdf417Options settings = data.Pdf417 == null
            ? CreateDefaultPdf417Options()
            : data.Pdf417;
        if (settings.Columns < 0 || settings.Columns > 30)
        {
            AddIssue(
                state,
                "PDF417_COLUMNS_INVALID",
                "PDF417 columns must be zero or from one through thirty.",
                "Pdf417.Columns",
                -1,
                "0 through 30",
                settings.Columns.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (settings.Rows < 0 || (settings.Rows > 0 && settings.Rows < 3) ||
            settings.Rows > 90)
        {
            AddIssue(
                state,
                "PDF417_ROWS_INVALID",
                "PDF417 rows must be zero or from three through ninety.",
                "Pdf417.Rows",
                -1,
                "0 or 3 through 90",
                settings.Rows.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (settings.Rows > 0 && settings.Columns > 0 &&
            (long)settings.Rows * settings.Columns >
            Pdf417SymbolPlanner.MaximumTotalCodewordCount)
        {
            AddIssue(
                state,
                "PDF417_DIMENSION_CAPACITY_INVALID",
                "Requested PDF417 rows and columns exceed symbol capacity.",
                "Pdf417.RowsColumns",
                -1,
                $"at most {Pdf417SymbolPlanner.MaximumTotalCodewordCount} codeword positions",
                $"{settings.Rows}x{settings.Columns}");
        }

        if (settings.Variant != Pdf417Variant.Standard &&
            settings.Variant != Pdf417Variant.Compact)
        {
            AddInvalidEnumIssue(
                state,
                "PDF417_VARIANT_INVALID",
                "Pdf417.Variant",
                (int)settings.Variant);
        }
        else if (format == BarcodeFormat.CompactPdf417 &&
            settings.Variant != Pdf417Variant.Compact)
        {
            AddIssue(
                state,
                "PDF417_VARIANT_FORMAT_CONFLICT",
                "Compact PDF417 format requires the compact row variant.",
                "Pdf417.Variant",
                -1,
                Pdf417Variant.Compact.ToString(),
                settings.Variant.ToString());
        }
        else if (format == BarcodeFormat.Pdf417 &&
            settings.Variant != Pdf417Variant.Standard)
        {
            AddIssue(
                state,
                "PDF417_VARIANT_FORMAT_CONFLICT",
                "Standard PDF417 format requires the standard row variant.",
                "Pdf417.Variant",
                -1,
                Pdf417Variant.Standard.ToString(),
                settings.Variant.ToString());
        }

        if (settings.CompactionMode != Pdf417CompactionMode.Automatic &&
            settings.CompactionMode != Pdf417CompactionMode.Text &&
            settings.CompactionMode != Pdf417CompactionMode.Byte &&
            settings.CompactionMode != Pdf417CompactionMode.Numeric)
        {
            AddInvalidEnumIssue(
                state,
                "PDF417_COMPACTION_MODE_INVALID",
                "Pdf417.CompactionMode",
                (int)settings.CompactionMode);
        }

        if (settings.ErrorCorrectionLevel < -1 ||
            settings.ErrorCorrectionLevel > 8)
        {
            AddIssue(
                state,
                "PDF417_ECC_LEVEL_INVALID",
                "PDF417 error-correction level must be automatic or zero through eight.",
                "Pdf417.ErrorCorrectionLevel",
                -1,
                "-1 or 0 through 8",
                settings.ErrorCorrectionLevel.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        int eci = settings.EciAssignmentNumber != 0
            ? settings.EciAssignmentNumber
            : data.Options == null ? 0 : data.Options.EciAssignmentNumber;
        if (eci < 0 || eci > Pdf417CompactionPlanner.MaximumEciAssignmentNumber)
        {
            AddIssue(
                state,
                "PDF417_ECI_INVALID",
                "PDF417 ECI assignment is outside the supported range.",
                "Pdf417.EciAssignmentNumber",
                -1,
                "0 through 811799",
                eci.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        int length = GetPayloadDataLength(payload);
        if (length > Pdf417CompactionPlanner.MaximumInputByteCount)
        {
            AddIssue(
                state,
                "PDF417_CAPACITY_EXCEEDED",
                "The payload exceeds the supported PDF417 input range.",
                "Payload",
                -1,
                $"at most {Pdf417CompactionPlanner.MaximumInputByteCount} bytes",
                length.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        ValidatePdf417CompactionCharacters(
            state,
            payload,
            settings.CompactionMode);
        ValidatePdf417Macro(state, data, settings, format);
        ValidatePdf417Capacity(state, payload, settings, eci);
    }

    static void ValidatePdf417Capacity(
        PreEncodeValidationState state,
        BarcodePayload payload,
        Pdf417Options settings,
        int eciAssignmentNumber)
    {
        if (settings.Rows < 0 || settings.Columns < 0 ||
            settings.Rows > 90 || settings.Columns > 30)
        {
            return;
        }

        int length = GetPayloadDataLength(payload);
        int highLevelCount;
        if (settings.CompactionMode == Pdf417CompactionMode.Text)
        {
            highLevelCount = (length + 1) / 2;
        }
        else if (settings.CompactionMode == Pdf417CompactionMode.Numeric)
        {
            highLevelCount = 1 + ((length + 2) / 3);
        }
        else if (settings.CompactionMode == Pdf417CompactionMode.Automatic)
        {
            highLevelCount = (length + 2) / 3;
        }
        else
        {
            highLevelCount = ((length / 6) * 5) + (length % 6) + 1;
        }

        if (eciAssignmentNumber > 0 && eciAssignmentNumber < 900)
        {
            highLevelCount = highLevelCount + 2;
        }
        else if (eciAssignmentNumber >= 900 && eciAssignmentNumber < 810900)
        {
            highLevelCount = highLevelCount + 3;
        }
        else if (eciAssignmentNumber >= 810900)
        {
            highLevelCount = highLevelCount + 2;
        }

        if (settings.Macro != null)
        {
            highLevelCount = highLevelCount + 4;
        }

        int level = settings.ErrorCorrectionLevel < 0 ||
            settings.ErrorCorrectionLevel > 8
            ? 0
            : settings.ErrorCorrectionLevel;
        int errorCount = 2 << level;
        int required = 1 + highLevelCount + errorCount;
        int capacity = Pdf417SymbolPlanner.MaximumTotalCodewordCount;
        if (settings.Rows > 0 && settings.Columns > 0)
        {
            capacity = settings.Rows * settings.Columns;
        }
        if (required > capacity)
        {
            AddIssue(
                state,
                "PDF417_SYMBOL_CAPACITY_EXCEEDED",
                "The payload, controls, and error correction exceed the requested PDF417 dimensions.",
                "Payload",
                -1,
                $"at most {capacity} total codeword positions",
                required.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static Pdf417Options CreateDefaultPdf417Options()
    {
        Pdf417Options options = new Pdf417Options();
        options.ErrorCorrectionLevel = -1;
        return options;
    }

    static void ValidatePdf417CompactionCharacters(
        PreEncodeValidationState state,
        BarcodePayload payload,
        Pdf417CompactionMode mode)
    {
        if (mode != Pdf417CompactionMode.Text &&
            mode != Pdf417CompactionMode.Numeric)
        {
            return;
        }

        int length = GetPayloadDataLength(payload);
        int index = 0;
        while (index < length)
        {
            int value = GetPayloadDataValue(payload, index);
            bool valid = mode == Pdf417CompactionMode.Numeric
                ? value >= '0' && value <= '9'
                : (value >= 32 && value <= 126) || value == 9 ||
                    value == 10 || value == 13;
            if (!valid)
            {
                AddIssue(
                    state,
                    mode == Pdf417CompactionMode.Numeric
                        ? "PDF417_NUMERIC_CHARACTER_INVALID"
                        : "PDF417_TEXT_CHARACTER_INVALID",
                    "The payload contains a byte outside the forced PDF417 compaction repertoire.",
                    "Payload",
                    index,
                    mode == Pdf417CompactionMode.Numeric
                        ? "an ASCII decimal digit"
                        : "printable ASCII, tab, line feed, or carriage return",
                    value.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }

            index = index + 1;
        }
    }

    static void ValidatePdf417Macro(
        PreEncodeValidationState state,
        BarcodeEncodeRequest data,
        Pdf417Options settings,
        BarcodeFormat format)
    {
        StructuredAppendInfo? macro = settings.Macro;
        if (macro == null && data.Options != null)
        {
            macro = data.Options.StructuredAppend;
        }

        if (macro == null && data.Payload != null)
        {
            macro = data.Payload.StructuredAppend;
        }

        bool hasOptional = settings.FileName != null || settings.Timestamp != 0 ||
            settings.Sender != null || settings.Addressee != null ||
            settings.FileSize != 0 || settings.FileChecksum != 0;
        if (format == BarcodeFormat.MacroPdf417 && macro == null)
        {
            AddIssue(
                state,
                "MACRO_PDF417_METADATA_MISSING",
                "Macro PDF417 requires segment metadata.",
                "Pdf417.Macro",
                -1,
                "Macro PDF417 segment metadata",
                "null");
        }

        if (format != BarcodeFormat.MacroPdf417 && macro != null)
        {
            AddIssue(
                state,
                "MACRO_PDF417_FORMAT_CONFLICT",
                "Macro PDF417 metadata requires the Macro PDF417 format.",
                "Pdf417.Macro",
                -1,
                BarcodeFormat.MacroPdf417.ToString(),
                format.ToString());
        }

        if (macro == null)
        {
            if (hasOptional)
            {
                AddIssue(
                    state,
                    "MACRO_PDF417_OPTION_WITHOUT_METADATA",
                    "Macro PDF417 optional fields require segment metadata.",
                    "Pdf417.Macro",
                    -1,
                    "segment metadata",
                    "null");
            }

            return;
        }

        ValidatePdf417MacroText(
            state,
            settings.FileName,
            "Pdf417.FileName");
        ValidatePdf417MacroText(
            state,
            settings.Sender,
            "Pdf417.Sender");
        ValidatePdf417MacroText(
            state,
            settings.Addressee,
            "Pdf417.Addressee");

        if (macro.Format != BarcodeFormat.Automatic &&
            macro.Format != BarcodeFormat.MacroPdf417)
        {
            AddIssue(
                state,
                "MACRO_PDF417_APPEND_FORMAT_INVALID",
                "Structured-append metadata is not for Macro PDF417.",
                "StructuredAppend.Format",
                -1,
                "Automatic or MacroPdf417",
                macro.Format.ToString());
        }

        if (macro.Index < 0 || macro.Index > Pdf417MacroCodec.MaximumSegmentIndex)
        {
            AddIssue(
                state,
                "MACRO_PDF417_INDEX_INVALID",
                "Macro PDF417 segment index is outside its supported range.",
                "StructuredAppend.Index",
                -1,
                "0 through 99998",
                macro.Index.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (macro.Count < 0 || macro.Count > Pdf417MacroCodec.MaximumSegmentCount)
        {
            AddIssue(
                state,
                "MACRO_PDF417_COUNT_INVALID",
                "Macro PDF417 segment count is outside its supported range.",
                "StructuredAppend.Count",
                -1,
                "0 through 99999",
                macro.Count.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (macro.Count > 0 && macro.Index >= macro.Count)
        {
            AddIssue(
                state,
                "MACRO_PDF417_INDEX_COUNT_CONFLICT",
                "Macro PDF417 segment index must be less than its segment count.",
                "StructuredAppend.Index",
                -1,
                $"less than {macro.Count}",
                macro.Index.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (macro.Count > 0 && macro.Index >= 0 &&
            macro.Index < macro.Count &&
            macro.IsLastSegment != (macro.Index == macro.Count - 1))
        {
            AddIssue(
                state,
                "MACRO_PDF417_LAST_SEGMENT_CONFLICT",
                "Macro PDF417 last-segment state conflicts with index and count.",
                "StructuredAppend.IsLastSegment",
                -1,
                (macro.Index == macro.Count - 1).ToString(),
                macro.IsLastSegment.ToString());
        }

        ValidatePdf417FileIdentifier(state, macro.FileIdentifier);
        if (settings.Timestamp < 0 || settings.Timestamp > 99999999999L)
        {
            AddIssue(
                state,
                "MACRO_PDF417_TIMESTAMP_INVALID",
                "Macro PDF417 timestamp is outside its supported range.",
                "Pdf417.Timestamp",
                -1,
                "0 through 99999999999",
                settings.Timestamp.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (settings.FileSize < 0)
        {
            AddIssue(
                state,
                "MACRO_PDF417_FILE_SIZE_INVALID",
                "Macro PDF417 file size must not be negative.",
                "Pdf417.FileSize",
                -1,
                "zero or a positive byte count",
                settings.FileSize.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (settings.FileChecksum < 0 ||
            settings.FileChecksum > Pdf417MacroCodec.MaximumChecksum)
        {
            AddIssue(
                state,
                "MACRO_PDF417_CHECKSUM_INVALID",
                "Macro PDF417 file checksum is outside its supported range.",
                "Pdf417.FileChecksum",
                -1,
                "0 through 65535",
                settings.FileChecksum.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void ValidatePdf417MacroText(
        PreEncodeValidationState state,
        string? text,
        string field)
    {
        if (text == null)
        {
            return;
        }

        if (text.Length == 0)
        {
            AddIssue(
                state,
                "MACRO_PDF417_TEXT_FIELD_EMPTY",
                "A supplied Macro PDF417 text field must not be empty.",
                field,
                0,
                "one or more PDF417 text-compaction characters",
                "empty");
            return;
        }

        int index = 0;
        while (index < text.Length)
        {
            int value = text[index];
            if (!((value >= 32 && value <= 126) || value == 9 ||
                value == 10 || value == 13))
            {
                AddIssue(
                    state,
                    "MACRO_PDF417_TEXT_FIELD_CHARACTER_INVALID",
                    "A Macro PDF417 text field contains a character outside PDF417 text compaction.",
                    field,
                    index,
                    "printable ASCII, tab, line feed, or carriage return",
                    value.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }

            index = index + 1;
        }
    }

    static void ValidatePdf417FileIdentifier(
        PreEncodeValidationState state,
        string? identifier)
    {
        if (identifier == null || identifier.Length == 0 ||
            identifier.Length % 3 != 0 || !IsAsciiDigits(identifier))
        {
            AddIssue(
                state,
                "MACRO_PDF417_FILE_ID_INVALID",
                "Macro PDF417 file identifier must contain one or more three-digit groups.",
                "StructuredAppend.FileIdentifier",
                -1,
                "one or more three-digit codeword groups",
                identifier == null ? "null" : identifier);
            return;
        }

        int offset = 0;
        while (offset < identifier.Length)
        {
            int value = ParseDecimal(identifier.Substring(offset, 3));
            if (value > 899)
            {
                AddIssue(
                    state,
                    "MACRO_PDF417_FILE_ID_GROUP_INVALID",
                    "A Macro PDF417 file identifier group exceeds codeword range.",
                    "StructuredAppend.FileIdentifier",
                    offset,
                    "000 through 899",
                    identifier.Substring(offset, 3));
            }

            offset = offset + 3;
        }
    }

    static void ValidatePayloadEciSegments(
        PreEncodeValidationState state,
        BarcodePayload payload,
        BarcodeFormat format)
    {
        if (payload.EciSegments == null)
        {
            return;
        }

        bool supports = format == BarcodeFormat.DataMatrix ||
            format == BarcodeFormat.Pdf417 ||
            format == BarcodeFormat.CompactPdf417 ||
            format == BarcodeFormat.MacroPdf417;
        int length = GetPayloadDataLength(payload);
        int maximum = format == BarcodeFormat.DataMatrix
            ? DataMatrixControlCodewordEncoder.MaximumEciAssignmentNumber
            : Pdf417CompactionPlanner.MaximumEciAssignmentNumber;
        int index = 0;
        int previousEnd = 0;
        while (index < payload.EciSegments.Length)
        {
            EciSegment? segment = payload.EciSegments[index];
            if (segment == null)
            {
                AddIssue(
                    state,
                    "ENCODE_ECI_SEGMENT_NULL",
                    "Payload ECI segment entries must not be null.",
                    "Payload.EciSegments",
                    index,
                    "an ECI segment",
                    "null");
            }
            else if (!supports)
            {
                AddIssue(
                    state,
                    "ENCODE_ECI_SEGMENT_NOT_APPLICABLE",
                    "Payload ECI segments are not applicable to the selected format.",
                    "Payload.EciSegments",
                    index,
                    "no ECI segments",
                    format.ToString());
            }
            else
            {
                if (segment.AssignmentNumber <= 0 ||
                    segment.AssignmentNumber > maximum)
                {
                    AddIssue(
                        state,
                        "ENCODE_ECI_SEGMENT_ASSIGNMENT_INVALID",
                        "An ECI segment assignment is outside the format range.",
                        "Payload.EciSegments.AssignmentNumber",
                        index,
                        $"1 through {maximum}",
                        segment.AssignmentNumber.ToString(
                            global::System.Globalization.CultureInfo.InvariantCulture));
                }

                if (segment.ByteOffset < 0 || segment.ByteCount <= 0 ||
                    segment.ByteOffset > length ||
                    segment.ByteCount > length - segment.ByteOffset)
                {
                    AddIssue(
                        state,
                        "ENCODE_ECI_SEGMENT_RANGE_INVALID",
                        "An ECI segment byte range exceeds the payload.",
                        "Payload.EciSegments.ByteRange",
                        index,
                        $"a positive range within {length} bytes",
                        $"{segment.ByteOffset}+{segment.ByteCount}");
                }
                else if (index > 0 && segment.ByteOffset < previousEnd)
                {
                    AddIssue(
                        state,
                        "ENCODE_ECI_SEGMENT_ORDER_INVALID",
                        "Payload ECI segments must be ordered and non-overlapping.",
                        "Payload.EciSegments.ByteOffset",
                        index,
                        $"an offset at or after {previousEnd}",
                        segment.ByteOffset.ToString(
                            global::System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    previousEnd = segment.ByteOffset + segment.ByteCount;
                }
            }

            index = index + 1;
        }
    }

    static void ValidateCharacterEncoding(
        PreEncodeValidationState state,
        BarcodePayload payload,
        BarcodeEncodeOptions? options,
        BarcodeFormat format)
    {
        string? encoding = options == null
            ? payload.CharacterEncoding
            : options.CharacterEncoding == null
                ? payload.CharacterEncoding
                : options.CharacterEncoding;
        if (encoding == null)
        {
            return;
        }

        if (encoding.Length == 0)
        {
            AddIssue(
                state,
                "ENCODE_CHARACTER_ENCODING_EMPTY",
                "An explicitly selected character encoding must not be empty.",
                "CharacterEncoding",
                -1,
                "a recognized encoding name",
                "empty");
            return;
        }

        bool fixedAscii = format == BarcodeFormat.Gs1128 ||
            format == BarcodeFormat.Ean8 || format == BarcodeFormat.Ean13 ||
            format == BarcodeFormat.UpcA || format == BarcodeFormat.UpcE ||
            format == BarcodeFormat.EanUpcAddon2 ||
            format == BarcodeFormat.EanUpcAddon5 ||
            format == BarcodeFormat.Code39 || format == BarcodeFormat.Itf ||
            format == BarcodeFormat.Itf14 || format == BarcodeFormat.Codabar ||
            format == BarcodeFormat.Gs1DataMatrix;
        if (fixedAscii && encoding != "ASCII" &&
            encoding != "ISO/IEC 646")
        {
            AddIssue(
                state,
                "ENCODE_CHARACTER_ENCODING_INVALID",
                "The selected format requires the ISO/IEC 646 character repertoire.",
                "CharacterEncoding",
                -1,
                "ASCII or ISO/IEC 646",
                encoding);
        }
    }

    static void AddOptionNotApplicable(
        PreEncodeValidationState state,
        string field,
        BarcodeFormat format)
    {
        AddIssue(
            state,
            "ENCODE_OPTION_NOT_APPLICABLE",
            "A symbology-specific option block is not applicable to the selected format.",
            field,
            -1,
            "no unrelated option block",
            format.ToString());
    }

    static void AddOptionConflict(
        PreEncodeValidationState state,
        string first,
        string second)
    {
        AddIssue(
            state,
            "ENCODE_OPTION_COMBINATION_INVALID",
            "The selected encoding options cannot be combined.",
            first,
            -1,
            $"not combined with {second}",
            $"combined with {second}");
    }

    static void AddTextRequiredIssue(
        PreEncodeValidationState state,
        BarcodeFormat format)
    {
        AddIssue(
            state,
            "ENCODE_TEXT_PAYLOAD_REQUIRED",
            "The selected format requires a text payload.",
            "Payload.Text",
            -1,
            $"text suitable for {format}",
            "null");
    }

    static void ValidateAsciiDigits(
        PreEncodeValidationState state,
        string text,
        string field,
        string code)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                AddIssue(
                    state,
                    code,
                    "The value must contain only ASCII decimal digits.",
                    field,
                    index,
                    "an ASCII decimal digit",
                    text[index].ToString());
            }

            index = index + 1;
        }
    }

    static void ValidateExactLength(
        PreEncodeValidationState state,
        string text,
        int expectedLength,
        string code)
    {
        if (text.Length != expectedLength)
        {
            AddIssue(
                state,
                code,
                "The payload length is invalid for the selected format.",
                "Payload.Text",
                -1,
                expectedLength.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                text.Length.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void AddInvalidCheckDigitIssue(
        PreEncodeValidationState state,
        string text,
        string code)
    {
        string data = text.Substring(0, text.Length - 1);
        AddIssue(
            state,
            code,
            "The supplied GS1 modulo-10 check digit is invalid.",
            "Payload.Text",
            text.Length - 1,
            Gs1Modulo10.CalculateCheckDigit(data).ToString(
                global::System.Globalization.CultureInfo.InvariantCulture),
            text.Substring(text.Length - 1, 1));
    }

    static void AddInvalidEnumIssue(
        PreEncodeValidationState state,
        string code,
        string field,
        int value)
    {
        AddIssue(
            state,
            code,
            "An encoding option contains an undefined enum value.",
            field,
            -1,
            "a defined enum value",
            value.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture));
    }

    static BarcodeChecksumMode ResolveCommonChecksumMode(
        BarcodeEncodeOptions? options)
    {
        return options == null
            ? BarcodeChecksumMode.None
            : options.ChecksumMode;
    }

    static bool IsChecksumModeValid(BarcodeChecksumMode mode)
    {
        return mode == BarcodeChecksumMode.None ||
            mode == BarcodeChecksumMode.Calculate ||
            mode == BarcodeChecksumMode.Require ||
            mode == BarcodeChecksumMode.VerifyKeep ||
            mode == BarcodeChecksumMode.VerifyStrip;
    }

    static bool IsVerificationMode(BarcodeChecksumMode mode)
    {
        return mode == BarcodeChecksumMode.Require ||
            mode == BarcodeChecksumMode.VerifyKeep ||
            mode == BarcodeChecksumMode.VerifyStrip;
    }

    static bool HasPayloadData(BarcodePayload payload)
    {
        return (payload.RawBytes != null && payload.RawBytes.Length > 0) ||
            (payload.Text != null && payload.Text.Length > 0);
    }

    static int GetPayloadDataLength(BarcodePayload payload)
    {
        if (payload.RawBytes != null && payload.RawBytes.Length > 0)
        {
            return payload.RawBytes.Length;
        }

        return payload.Text == null ? 0 : payload.Text.Length;
    }

    static int GetPayloadDataValue(BarcodePayload payload, int index)
    {
        if (payload.RawBytes != null && payload.RawBytes.Length > 0)
        {
            return payload.RawBytes[index];
        }

        string text = payload.Text == null ? "" : payload.Text;
        return text[index];
    }

    static bool IsAsciiDigits(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool ContainsInteger(int[] values, int expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == expected)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool AreAppendValuesEqual(
        StructuredAppendInfo first,
        StructuredAppendInfo second)
    {
        return first.Format == second.Format && first.Index == second.Index &&
            first.Count == second.Count &&
            first.FileIdentifier == second.FileIdentifier &&
            first.IsLastSegment == second.IsLastSegment;
    }

    static bool IsDataMatrixX12Value(int value)
    {
        return value == 13 || value == '*' || value == '>' || value == ' ' ||
            (value >= '0' && value <= '9') ||
            (value >= 'A' && value <= 'Z');
    }

    static int ParseDecimal(string text)
    {
        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            value = (value * 10) + (text[index] - '0');
            index = index + 1;
        }

        return value;
    }

    static void AddIssue(
        PreEncodeValidationState state,
        string code,
        string message,
        string field,
        int offset,
        string? expected,
        string? observed)
    {
        string standard = state.Standard.Length == 0
            ? "Barcode encoding"
            : state.Standard;
        BarcodeValidationIssue issue = ValidationIssueFactory.Create(
            ValidationModePolicy.ResolveSeverity(state.Mode, code),
            code,
            message,
            standard,
            field,
            offset,
            expected,
            observed);
        state.Issues.Add(issue);
    }

    static BarcodeValidationReport CreateReport(
        PreEncodeValidationState state)
    {
        BarcodeValidationIssue[] issues =
            new BarcodeValidationIssue[state.Issues.Count];
        int index = 0;
        while (index < state.Issues.Count)
        {
            issues[index] = ValidationIssueFactory.CopyWithOffset(
                state.Issues[index],
                state.Issues[index].Offset);
            index = index + 1;
        }

        BarcodeValidationReport report = new BarcodeValidationReport();
        report.IsConformant = ValidationModePolicy.IsConformant(issues);
        report.Mode = state.Mode;
        report.Standard = state.Standard;
        report.Profile = state.Profile;
        report.Issues = issues;
        report.Metrics = new BarcodeQualityMetric[0];
        report.Diagnostics = new BarcodeDiagnostic[0];
        return report;
    }
}
