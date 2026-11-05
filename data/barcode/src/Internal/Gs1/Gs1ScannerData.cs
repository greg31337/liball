namespace barcode;

internal class Gs1SymbologyIdentifierInfo
{
    internal bool Recognized;
    internal string Identifier = "";
    internal int Modifier = -1;
    internal BarcodeFormat Format = BarcodeFormat.Automatic;
    internal bool IsGs1;
    internal bool ContainsApplicationIdentifiers;
    internal bool ChecksumVerified;
    internal bool ChecksumTransmitted;
    internal bool FullAscii;
}

internal class Gs1ScannerParseResult
{
    internal bool Success;
    internal Gs1Element[] Elements = new Gs1Element[0];
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
    internal string SymbologyIdentifier = "";
    internal BarcodeFormat Format = BarcodeFormat.Automatic;
}

internal class Gs1ScannerBuildResult
{
    internal bool Success;
    internal string Text = "";
    internal byte[] Bytes = new byte[0];
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
    internal string SymbologyIdentifier = "";
    internal BarcodeFormat Format = BarcodeFormat.Automatic;
}

internal static class Gs1ScannerData
{
    internal const string Ean8Identifier = "]E4";
    internal const string Itf14CheckStrippedIdentifier = "]I3";

    internal static Gs1SymbologyIdentifierInfo InterpretSymbologyIdentifier(
        string? identifier)
    {
        Gs1SymbologyIdentifierInfo info =
            new Gs1SymbologyIdentifierInfo();
        info.Identifier = identifier == null ? "" : identifier;
        if (identifier == null
            || identifier.Length != 3
            || identifier[0] != ']')
        {
            return info;
        }

        char modifierCharacter = identifier[2];
        if (modifierCharacter >= '0' && modifierCharacter <= '9')
        {
            info.Modifier = modifierCharacter - '0';
        }

        switch (identifier)
        {
            case "]C0":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Code128,
                    false,
                    false,
                    false,
                    false,
                    false);
                break;
            case "]C1":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Gs1128,
                    true,
                    true,
                    false,
                    false,
                    false);
                break;
            case "]E0":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Automatic,
                    true,
                    false,
                    true,
                    true,
                    false);
                break;
            case "]E4":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Ean8,
                    true,
                    false,
                    true,
                    true,
                    false);
                break;
            case "]A0":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Code39,
                    false,
                    false,
                    false,
                    false,
                    false);
                break;
            case "]A1":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Code39,
                    false,
                    false,
                    true,
                    true,
                    false);
                break;
            case "]A3":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Code39,
                    false,
                    false,
                    true,
                    false,
                    false);
                break;
            case "]A4":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Code39,
                    false,
                    false,
                    false,
                    false,
                    true);
                break;
            case "]A5":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Code39,
                    false,
                    false,
                    true,
                    true,
                    true);
                break;
            case "]A7":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Code39,
                    false,
                    false,
                    true,
                    false,
                    true);
                break;
            case "]I0":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Itf,
                    false,
                    false,
                    false,
                    false,
                    false);
                break;
            case "]I1":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Itf14,
                    true,
                    false,
                    true,
                    true,
                    false);
                break;
            case "]I3":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Itf14,
                    true,
                    false,
                    true,
                    false,
                    false);
                break;
            case "]F0":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Codabar,
                    false,
                    false,
                    false,
                    false,
                    false);
                break;
            case "]d1":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.DataMatrix,
                    false,
                    false,
                    false,
                    false,
                    false);
                break;
            case "]d2":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Gs1DataMatrix,
                    true,
                    true,
                    false,
                    false,
                    false);
                break;
            case "]L0":
                SetIdentifierInfo(
                    info,
                    BarcodeFormat.Pdf417,
                    false,
                    false,
                    false,
                    false,
                    false);
                break;
            default:
                break;
        }

        return info;
    }

    internal static Gs1ScannerParseResult ParseScannerData(
        byte[]? source)
    {
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (source == null)
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_NULL",
                "The scanner data must not be null.",
                "Message",
                -1,
                "scanner data beginning with a symbology identifier",
                "null");
            return CreateScannerParseResult(
                new Gs1Element[0],
                issues,
                "",
                BarcodeFormat.Automatic);
        }

        if (source.Length < 3 || source[0] != (byte)']')
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_IDENTIFIER_MISSING",
                "Scanner data must begin with a three-character ISO/IEC 15424 symbology identifier.",
                "SymbologyIdentifier",
                0,
                "']' followed by a code character and modifier",
                ReadObservedBytes(source, 0));
            return CreateScannerParseResult(
                new Gs1Element[0],
                issues,
                "",
                BarcodeFormat.Automatic);
        }

        string identifier = ReadIdentifier(source);
        Gs1SymbologyIdentifierInfo info =
            InterpretSymbologyIdentifier(identifier);
        if (!info.Recognized)
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_IDENTIFIER_UNSUPPORTED",
                "The symbology identifier or modifier is not supported.",
                "SymbologyIdentifier",
                0,
                "a supported ISO/IEC 15424 symbology identifier",
                identifier);
            return CreateScannerParseResult(
                new Gs1Element[0],
                issues,
                identifier,
                BarcodeFormat.Automatic);
        }

        if (!info.IsGs1)
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_IDENTIFIER_NOT_GS1",
                "The symbology identifier modifier does not indicate a supported GS1 carrier.",
                "SymbologyIdentifier",
                0,
                "a GS1 symbology modifier",
                identifier);
            return CreateScannerParseResult(
                new Gs1Element[0],
                issues,
                identifier,
                info.Format);
        }

        if (info.ContainsApplicationIdentifiers)
        {
            return ParseAiScannerPayload(source, identifier, info, issues);
        }

        return ParsePlainScannerPayload(source, identifier, info, issues);
    }

    internal static Gs1ScannerBuildResult BuildScannerData(
        Gs1Element[]? elements,
        BarcodeFormat format)
    {
        return BuildScannerRepresentation(elements, format, false);
    }

    internal static Gs1ScannerBuildResult BuildItf14ScannerDataWithoutCheckDigit(
        Gs1Element[]? elements)
    {
        return BuildScannerRepresentation(
            elements,
            BarcodeFormat.Itf14,
            true);
    }

    static Gs1ScannerParseResult ParseAiScannerPayload(
        byte[] source,
        string identifier,
        Gs1SymbologyIdentifierInfo info,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int caretOffset = FindByte(source, (byte)'^', 3);
        if (caretOffset >= 0)
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_ILLEGAL_CARET",
                "A caret is a tooling placeholder and is not permitted in scanner-transmitted GS1 data.",
                "Message",
                caretOffset,
                "ASCII GS for transmitted FNC1 separators",
                "caret");
            return CreateScannerParseResult(
                new Gs1Element[0],
                issues,
                identifier,
                info.Format);
        }

        byte[] payload = CopyPayload(source, 3);
        Gs1UnbracketedParseResult parsed =
            Gs1UnbracketedSyntax.ParseBarcodeMessageBytes(payload);
        OffsetElements(parsed.Elements, 3);
        AddOffsetIssues(issues, parsed.Issues, 3);
        SortScannerIssues(issues);
        return CreateScannerParseResult(
            parsed.Elements,
            issues,
            identifier,
            info.Format);
    }

    static Gs1ScannerParseResult ParsePlainScannerPayload(
        byte[] source,
        string identifier,
        Gs1SymbologyIdentifierInfo info,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        string payload = ConvertByteRangeToText(
            source,
            3,
            source.Length - 3);
        int expectedLength = ResolvePlainPayloadLength(
            identifier,
            payload.Length);
        if (expectedLength < 0 || payload.Length != expectedLength)
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_PRIMARY_LENGTH_INVALID",
                "The primary scanner payload length does not match the symbology identifier modifier.",
                "Message",
                3,
                ResolvePlainLengthExpectation(identifier),
                $"{payload.Length} characters");
            return CreateScannerParseResult(
                new Gs1Element[0],
                issues,
                identifier,
                info.Format);
        }

        int invalidDigitOffset = FindInvalidDigit(payload);
        if (invalidDigitOffset >= 0)
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_PRIMARY_NON_NUMERIC",
                "The primary GS1 carrier payload must contain only ASCII digits.",
                "Message",
                3 + invalidDigitOffset,
                "ASCII digits",
                payload.Substring(invalidDigitOffset, 1));
            return CreateScannerParseResult(
                new Gs1Element[0],
                issues,
                identifier,
                info.Format);
        }

        string completeDigits = payload;
        if (identifier == BarcodeSymbologyIdentifiers.EanUpc
            && payload.Length == 8)
        {
            completeDigits = ExpandScannerUpcE(payload);
            if (completeDigits.Length == 0)
            {
                AddScannerIssue(
                    issues,
                    "GS1_SCANNER_UPCE_NUMBER_SYSTEM_INVALID",
                    "The UPC-E number-system digit must be zero or one.",
                    "Message",
                    3,
                    "UPC-E number system zero or one",
                    payload.Substring(0, 1));
                return CreateScannerParseResult(
                    new Gs1Element[0],
                    issues,
                    identifier,
                    BarcodeFormat.UpcE);
            }

            if (!Gs1Modulo10.VerifyCompleteDigits(completeDigits))
            {
                AddScannerIssue(
                    issues,
                    "GS1_SCANNER_PRIMARY_CHECK_DIGIT_INVALID",
                    "The primary GS1 carrier payload has an incorrect check digit.",
                    "Message",
                    source.Length - 1,
                    "the GS1 modulo-10 check digit over the expanded UPC-A value",
                    payload.Substring(payload.Length - 1, 1));
                return CreateScannerParseResult(
                    new Gs1Element[0],
                    issues,
                    identifier,
                    BarcodeFormat.UpcE);
            }
        }
        else if (identifier == Itf14CheckStrippedIdentifier)
        {
            completeDigits = Gs1Modulo10.AppendCheckDigit(payload);
        }
        else if (!Gs1Modulo10.VerifyCompleteDigits(payload))
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_PRIMARY_CHECK_DIGIT_INVALID",
                "The primary GS1 carrier payload has an incorrect check digit.",
                "Message",
                source.Length - 1,
                "the GS1 modulo-10 check digit",
                payload.Substring(payload.Length - 1, 1));
            return CreateScannerParseResult(
                new Gs1Element[0],
                issues,
                identifier,
                info.Format);
        }

        string gtin = PadWithLeadingZeros(completeDigits, 14);
        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementString("01" + gtin);
        int paddingLength = 14 - completeDigits.Length;
        MapPlainElements(parsed.Elements, payload.Length);
        AddPlainSemanticIssues(
            issues,
            parsed.Issues,
            paddingLength,
            payload.Length);
        BarcodeFormat resolvedFormat = ResolvePlainFormat(
            identifier,
            payload.Length);
        SortScannerIssues(issues);
        return CreateScannerParseResult(
            parsed.Elements,
            issues,
            identifier,
            resolvedFormat);
    }

    static Gs1ScannerBuildResult BuildScannerRepresentation(
        Gs1Element[]? elements,
        BarcodeFormat format,
        bool stripItfCheckDigit)
    {
        if (format == BarcodeFormat.Gs1128
            || format == BarcodeFormat.Gs1DataMatrix)
        {
            return BuildAiScannerCarrier(elements, format);
        }

        if (format == BarcodeFormat.Ean8
            || format == BarcodeFormat.Ean13
            || format == BarcodeFormat.UpcA
            || format == BarcodeFormat.UpcE
            || format == BarcodeFormat.Itf14)
        {
            return BuildPlainScannerCarrier(
                elements,
                format,
                stripItfCheckDigit);
        }

        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        AddScannerIssue(
            issues,
            "GS1_SCANNER_FORMAT_NOT_GS1_CARRIER",
            "The requested barcode format is not a supported GS1 scanner-data carrier.",
            "Format",
            -1,
            "a GS1-128, GS1 DataMatrix, EAN/UPC, or ITF-14 format",
            format.ToString());
        return CreateScannerBuildResult(
            "",
            issues,
            "",
            format);
    }

    static Gs1ScannerBuildResult BuildAiScannerCarrier(
        Gs1Element[]? elements,
        BarcodeFormat format)
    {
        Gs1UnbracketedBuildResult built =
            Gs1UnbracketedSyntax.BuildBarcodeMessage(elements);
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        AddOffsetIssues(issues, built.Issues, 3);
        string identifier = format == BarcodeFormat.Gs1128
            ? BarcodeSymbologyIdentifiers.Gs1128
            : BarcodeSymbologyIdentifiers.Gs1DataMatrix;
        string text = identifier + built.Text;
        SortScannerIssues(issues);
        return CreateScannerBuildResult(
            text,
            issues,
            identifier,
            format);
    }

    static Gs1ScannerBuildResult BuildPlainScannerCarrier(
        Gs1Element[]? elements,
        BarcodeFormat format,
        bool stripItfCheckDigit)
    {
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        string identifier = ResolveBuildIdentifier(
            format,
            stripItfCheckDigit);
        string gtin = ReadBuildGtin(elements, issues);
        if (issues.Count > 0)
        {
            return CreateScannerBuildResult(
                identifier,
                issues,
                identifier,
                format);
        }

        string payload = ResolveBuildPayload(gtin, format, issues);
        if (stripItfCheckDigit && format == BarcodeFormat.Itf14)
        {
            payload = gtin.Substring(0, 13);
        }

        string text = identifier + payload;
        SortScannerIssues(issues);
        return CreateScannerBuildResult(
            text,
            issues,
            identifier,
            format);
    }

    static string ReadBuildGtin(
        Gs1Element[]? elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        if (elements == null || elements.Length != 1)
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_PRIMARY_ELEMENT_REQUIRED",
                "A plain GS1 carrier requires exactly one AI (01) element.",
                "Elements",
                -1,
                "one AI (01) element",
                elements == null ? "null" : $"{elements.Length} elements");
            return "";
        }

        Gs1Element element = elements[0];
        if (element == null || element.ApplicationIdentifier != "01")
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_PRIMARY_ELEMENT_REQUIRED",
                "A plain GS1 carrier requires exactly one AI (01) element.",
                "ApplicationIdentifier",
                -1,
                "AI (01)",
                element == null
                    ? "null"
                    : element.ApplicationIdentifier == null
                        ? "null"
                        : element.ApplicationIdentifier);
            return "";
        }

        string gtin = element.Value == null ? "" : element.Value;
        if (gtin.Length != 14)
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_GTIN_LENGTH_INVALID",
                "AI (01) must contain a complete fourteen-digit GTIN.",
                "01",
                -1,
                "14 digits",
                $"{gtin.Length} characters");
            return "";
        }

        int invalidDigitOffset = FindInvalidDigit(gtin);
        if (invalidDigitOffset >= 0)
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_GTIN_NON_NUMERIC",
                "AI (01) must contain only ASCII digits.",
                "01",
                invalidDigitOffset,
                "ASCII digits",
                gtin.Substring(invalidDigitOffset, 1));
            return "";
        }

        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementString("01" + gtin);
        AddOffsetIssues(issues, parsed.Issues, -2);
        return gtin;
    }

    static string ResolveBuildPayload(
        string gtin,
        BarcodeFormat format,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int leadingZeroCount;
        switch (format)
        {
            case BarcodeFormat.Ean8:
                leadingZeroCount = 6;
                break;
            case BarcodeFormat.Ean13:
                leadingZeroCount = 1;
                break;
            case BarcodeFormat.UpcA:
            case BarcodeFormat.UpcE:
                leadingZeroCount = 2;
                break;
            case BarcodeFormat.Itf14:
                leadingZeroCount = 0;
                break;
            default:
                leadingZeroCount = 0;
                break;
        }

        if (!HasLeadingZeros(gtin, leadingZeroCount))
        {
            AddScannerIssue(
                issues,
                "GS1_SCANNER_GTIN_CARRIER_MISMATCH",
                "The GTIN cannot be represented by the requested carrier without losing significant digits.",
                "01",
                -1,
                $"{leadingZeroCount} leading zero digits",
                gtin);
            return "";
        }

        string payload = gtin.Substring(leadingZeroCount);
        if (format == BarcodeFormat.UpcE)
        {
            if (payload[0] != '0' && payload[0] != '1')
            {
                AddScannerIssue(
                    issues,
                    "GS1_SCANNER_GTIN_NOT_UPCE_COMPRESSIBLE",
                    "The GTIN does not contain a UPC-A value that can be represented as UPC-E.",
                    "01",
                    -1,
                    "UPC-A number system zero or one",
                    payload.Substring(0, 1));
                return "";
            }

            string? compressed =
                UpcECompressor.TryCompressCompleteUpcA(payload);
            if (compressed == null)
            {
                AddScannerIssue(
                    issues,
                    "GS1_SCANNER_GTIN_NOT_UPCE_COMPRESSIBLE",
                    "The GTIN does not contain a UPC-A value that can be represented as UPC-E.",
                    "01",
                    -1,
                    "a UPC-E-compressible UPC-A value",
                    payload);
                return "";
            }

            payload = compressed;
        }

        return payload;
    }

    static string ResolveBuildIdentifier(
        BarcodeFormat format,
        bool stripItfCheckDigit)
    {
        if (format == BarcodeFormat.Gs1128)
        {
            return BarcodeSymbologyIdentifiers.Gs1128;
        }

        if (format == BarcodeFormat.Gs1DataMatrix)
        {
            return BarcodeSymbologyIdentifiers.Gs1DataMatrix;
        }

        if (format == BarcodeFormat.Ean8)
        {
            return Ean8Identifier;
        }

        if (format == BarcodeFormat.Itf14)
        {
            return stripItfCheckDigit
                ? Itf14CheckStrippedIdentifier
                : BarcodeSymbologyIdentifiers.Itf14;
        }

        return BarcodeSymbologyIdentifiers.EanUpc;
    }

    static int ResolvePlainPayloadLength(
        string identifier,
        int observedLength)
    {
        if (identifier == Ean8Identifier)
        {
            return 8;
        }

        if (identifier == BarcodeSymbologyIdentifiers.Itf14)
        {
            return 14;
        }

        if (identifier == Itf14CheckStrippedIdentifier)
        {
            return 13;
        }

        if (identifier == BarcodeSymbologyIdentifiers.EanUpc
            && (observedLength == 8
                || observedLength == 12
                || observedLength == 13))
        {
            return observedLength;
        }

        return -1;
    }

    static string ResolvePlainLengthExpectation(string identifier)
    {
        if (identifier == Ean8Identifier)
        {
            return "8 digits";
        }

        if (identifier == BarcodeSymbologyIdentifiers.Itf14)
        {
            return "14 digits including the check digit";
        }

        if (identifier == Itf14CheckStrippedIdentifier)
        {
            return "13 digits with the verified check digit omitted";
        }

        return "8, 12, or 13 digits";
    }

    static BarcodeFormat ResolvePlainFormat(
        string identifier,
        int payloadLength)
    {
        if (identifier == Ean8Identifier)
        {
            return BarcodeFormat.Ean8;
        }

        if (identifier == BarcodeSymbologyIdentifiers.Itf14
            || identifier == Itf14CheckStrippedIdentifier)
        {
            return BarcodeFormat.Itf14;
        }

        if (payloadLength == 13)
        {
            return BarcodeFormat.Ean13;
        }

        if (payloadLength == 12)
        {
            return BarcodeFormat.UpcA;
        }

        return BarcodeFormat.UpcE;
    }

    static void SetIdentifierInfo(
        Gs1SymbologyIdentifierInfo info,
        BarcodeFormat format,
        bool isGs1,
        bool containsApplicationIdentifiers,
        bool checksumVerified,
        bool checksumTransmitted,
        bool fullAscii)
    {
        info.Recognized = true;
        info.Format = format;
        info.IsGs1 = isGs1;
        info.ContainsApplicationIdentifiers =
            containsApplicationIdentifiers;
        info.ChecksumVerified = checksumVerified;
        info.ChecksumTransmitted = checksumTransmitted;
        info.FullAscii = fullAscii;
    }

    static int FindByte(byte[] source, byte target, int startOffset)
    {
        int index = startOffset;
        while (index < source.Length)
        {
            if (source[index] == target)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int FindInvalidDigit(string source)
    {
        int index = 0;
        while (index < source.Length)
        {
            char character = source[index];
            if (character < '0' || character > '9')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool HasLeadingZeros(string source, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (source[index] != '0')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static string ExpandScannerUpcE(string source)
    {
        char numberSystem = source[0];
        if (numberSystem != '0' && numberSystem != '1')
        {
            return "";
        }

        char terminal = source[6];
        string data;
        switch (terminal)
        {
            case '0':
            case '1':
            case '2':
                data = numberSystem
                    + source.Substring(1, 2)
                    + terminal
                    + "0000"
                    + source.Substring(3, 3);
                break;
            case '3':
                data = numberSystem
                    + source.Substring(1, 3)
                    + "00000"
                    + source.Substring(4, 2);
                break;
            case '4':
                data = numberSystem
                    + source.Substring(1, 4)
                    + "00000"
                    + source[5];
                break;
            default:
                data = numberSystem
                    + source.Substring(1, 5)
                    + "0000"
                    + terminal;
                break;
        }

        return data + source[7];
    }

    static string PadWithLeadingZeros(string source, int length)
    {
        int paddingLength = length - source.Length;
        return new string('0', paddingLength) + source;
    }

    static string ReadIdentifier(byte[] source)
    {
        char[] characters = new char[3];
        characters[0] = (char)source[0];
        characters[1] = (char)source[1];
        characters[2] = (char)source[2];
        return new string(characters);
    }

    static byte[] CopyPayload(byte[] source, int offset)
    {
        byte[] payload = new byte[source.Length - offset];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = source[offset + index];
            index = index + 1;
        }

        return payload;
    }

    static string ConvertByteRangeToText(
        byte[] source,
        int offset,
        int length)
    {
        char[] characters = new char[length];
        int index = 0;
        while (index < length)
        {
            characters[index] = (char)source[offset + index];
            index = index + 1;
        }

        return new string(characters);
    }

    static byte[] ConvertScannerTextToBytes(string source)
    {
        byte[] bytes = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            bytes[index] = (byte)source[index];
            index = index + 1;
        }

        return bytes;
    }

    static string ReadObservedBytes(byte[] source, int offset)
    {
        int length = source.Length - offset;
        if (length > 8)
        {
            length = 8;
        }

        return ConvertByteRangeToText(source, offset, length);
    }

    static void OffsetElements(Gs1Element[] elements, int offset)
    {
        int index = 0;
        while (index < elements.Length)
        {
            elements[index].SourceOffset =
                elements[index].SourceOffset + offset;
            index = index + 1;
        }
    }

    static void MapPlainElements(Gs1Element[] elements, int sourceLength)
    {
        int index = 0;
        while (index < elements.Length)
        {
            elements[index].SourceOffset = 3;
            elements[index].SourceLength = sourceLength;
            index = index + 1;
        }
    }

    static void AddOffsetIssues(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        BarcodeValidationIssue[] source,
        int offset)
    {
        int index = 0;
        while (index < source.Length)
        {
            BarcodeValidationIssue issue = source[index];
            if (issue.Offset >= 0)
            {
                issue.Offset = issue.Offset + offset;
            }

            issues.Add(issue);
            index = index + 1;
        }
    }

    static void AddPlainSemanticIssues(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        BarcodeValidationIssue[] source,
        int paddingLength,
        int payloadLength)
    {
        int index = 0;
        while (index < source.Length)
        {
            BarcodeValidationIssue issue = source[index];
            if (issue.Offset >= 2)
            {
                int valueOffset = issue.Offset - 2 - paddingLength;
                if (valueOffset < 0)
                {
                    valueOffset = 0;
                }

                if (valueOffset >= payloadLength)
                {
                    valueOffset = payloadLength - 1;
                }

                issue.Offset = 3 + valueOffset;
            }
            else if (issue.Offset >= 0)
            {
                issue.Offset = 3;
            }

            issues.Add(issue);
            index = index + 1;
        }
    }

    static void AddScannerIssue(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        string code,
        string message,
        string field,
        int offset,
        string expected,
        string observed)
    {
        BarcodeValidationIssue issue = ValidationIssueFactory.Create(
            BarcodeValidationSeverity.Error,
            code,
            message,
            BarcodeStandards.SymbologyIdentifiers,
            field,
            offset,
            expected,
            observed);
        issues.Add(issue);
    }

    static void SortScannerIssues(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int index = 1;
        while (index < issues.Count)
        {
            BarcodeValidationIssue current = issues[index];
            int previousIndex = index - 1;
            while (previousIndex >= 0
                && issues[previousIndex].Offset > current.Offset)
            {
                issues[previousIndex + 1] = issues[previousIndex];
                previousIndex = previousIndex - 1;
            }

            issues[previousIndex + 1] = current;
            index = index + 1;
        }
    }

    static Gs1ScannerParseResult CreateScannerParseResult(
        Gs1Element[] elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        string identifier,
        BarcodeFormat format)
    {
        Gs1ScannerParseResult result = new Gs1ScannerParseResult();
        result.Success = issues.Count == 0;
        result.Elements = elements;
        result.Issues = issues.ToArray();
        result.SymbologyIdentifier = identifier;
        result.Format = format;
        return result;
    }

    static Gs1ScannerBuildResult CreateScannerBuildResult(
        string text,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        string identifier,
        BarcodeFormat format)
    {
        Gs1ScannerBuildResult result = new Gs1ScannerBuildResult();
        result.Success = issues.Count == 0;
        result.Text = text;
        result.Bytes = issues.Count == 0
            ? ConvertScannerTextToBytes(text)
            : new byte[0];
        result.Issues = issues.ToArray();
        result.SymbologyIdentifier = identifier;
        result.Format = format;
        return result;
    }
}
