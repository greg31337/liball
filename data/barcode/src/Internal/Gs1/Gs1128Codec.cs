namespace barcode;

internal class Gs1128EncodedData
{
    internal BarcodeSymbol Symbol = new BarcodeSymbol();
    internal int[] Tokens = new int[0];
    internal byte[] ElementStringBytes = new byte[0];
    internal string ElementString = "";
    internal string HumanReadableText = "";
    internal int DataCharacterCount;
}

internal class Gs1128DecodedData
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal int[] Tokens = new int[0];
    internal bool IsReversed;
    internal int StartPosition;
    internal int EndPosition;
}

internal static class Gs1128Codec
{
    internal const int MaximumDataCharacterCount = 48;
    internal const double MaximumPhysicalWidthMillimetres = 165.10;
    internal const int GroupSeparator = 29;

    internal static Gs1128EncodedData EncodeElements(
        Gs1Element[] elements,
        Code128Options? code128Options,
        BarcodeEncodeOptions? encodeOptions)
    {
        Gs1HriResult hri = Gs1HriFormatter.GenerateHri(
            elements,
            false,
            true);
        Gs1Element[] orderedElements = hri.Elements;
        int dataCharacterCount = CountDataCharacters(orderedElements);
        int[] tokens = new int[CheckedMath.Add(dataCharacterCount, 1)];
        byte[] elementStringBytes = new byte[dataCharacterCount];
        char[] elementStringCharacters = new char[dataCharacterCount];

        tokens[0] = Code128Planner.Function1Token;
        int tokenOffset = 1;
        int elementStringOffset = 0;
        int elementIndex = 0;
        while (elementIndex < orderedElements.Length)
        {
            Gs1Element element = orderedElements[elementIndex];
            string applicationIdentifier = element.ApplicationIdentifier == null ? "" : element.ApplicationIdentifier;
            string value = element.Value == null ? "" : element.Value;
            WriteCharacters(applicationIdentifier, tokens, elementStringBytes, elementStringCharacters, tokenOffset, elementStringOffset);
            tokenOffset = CheckedMath.Add(tokenOffset, applicationIdentifier.Length);
            elementStringOffset = CheckedMath.Add(elementStringOffset, applicationIdentifier.Length);
            WriteCharacters(value, tokens, elementStringBytes, elementStringCharacters, tokenOffset, elementStringOffset);
            tokenOffset = CheckedMath.Add(tokenOffset, value.Length);
            elementStringOffset = CheckedMath.Add(elementStringOffset, value.Length);

            if (Gs1HriFormatter.IsVariableLengthElement(element)
                && elementIndex < orderedElements.Length - 1)
            {
                tokens[tokenOffset] = Code128Planner.Function1Token;
                elementStringBytes[elementStringOffset] = GroupSeparator;
                elementStringCharacters[elementStringOffset] = (char)GroupSeparator;
                tokenOffset = tokenOffset + 1;
                elementStringOffset = elementStringOffset + 1;
            }

            elementIndex = elementIndex + 1;
        }

        Code128Options settings = CreateCode128Options(code128Options);
        Code128EncodedData encoded = Code128Encoder.EncodeTokens(tokens, settings);
        string elementString = new string(elementStringCharacters);
        string humanReadableText = hri.CompactText;
        BarcodeSymbol symbol = CreateGs1Symbol(
            encoded,
            elementString,
            humanReadableText,
            elementStringBytes,
            encodeOptions);
        EnforcePhysicalWidth(symbol);

        Gs1128EncodedData result = new Gs1128EncodedData();
        result.Symbol = symbol;
        result.Tokens = tokens;
        result.ElementStringBytes = elementStringBytes;
        result.ElementString = elementString;
        result.HumanReadableText = humanReadableText;
        result.DataCharacterCount = dataCharacterCount;
        return result;
    }

    internal static Gs1128DecodedData? TryDecodeScanline(BitRow row)
    {
        Code128DecodedData? decoded = Code128ScanlineDecoder.TryDecodeRow(row);
        if (decoded == null || decoded.Tokens.Length < 2 || decoded.Tokens[0] != Code128Planner.Function1Token)
        {
            return null;
        }

        byte[]? bytes = TranslateDecodedTokens(decoded.Tokens);
        if (bytes == null || bytes.Length == 0 || bytes.Length > MaximumDataCharacterCount)
        {
            return null;
        }

        string elementString = ConvertBytesToString(bytes);
        BarcodeDecodeResult decodeResult = CreateDecodeResult(decoded, bytes, elementString);
        Gs1128DecodedData result = new Gs1128DecodedData();
        result.Result = decodeResult;
        int[]? copiedTokens = ResultCopier.CopyIntArray(decoded.Tokens);
        result.Tokens = copiedTokens == null ? new int[0] : copiedTokens;
        result.IsReversed = decoded.IsReversed;
        result.StartPosition = decoded.StartPosition;
        result.EndPosition = decoded.EndPosition;
        return result;
    }

    static int CountDataCharacters(Gs1Element[] elements)
    {
        if (elements == null || elements.Length == 0)
        {
            throw new BarcodeEncodingException("GS1-128 requires at least one GS1 element.");
        }

        int count = 0;
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            ValidateElement(element);
            string applicationIdentifier = element.ApplicationIdentifier == null ? "" : element.ApplicationIdentifier;
            string value = element.Value == null ? "" : element.Value;
            count = CheckedMath.Add(count, applicationIdentifier.Length);
            count = CheckedMath.Add(count, value.Length);
            if (Gs1HriFormatter.IsVariableLengthElement(element)
                && index < elements.Length - 1)
            {
                count = CheckedMath.Add(count, 1);
            }

            if (count > MaximumDataCharacterCount)
            {
                throw new BarcodeEncodingException("GS1-128 element data exceeds the 48-character limit.");
            }

            index = index + 1;
        }

        return count;
    }

    static void ValidateElement(Gs1Element element)
    {
        if (element == null)
        {
            throw new BarcodeEncodingException("GS1-128 elements must not contain null entries.");
        }

        ValidateApplicationIdentifier(element.ApplicationIdentifier);
        ValidateElementValue(element.Value);
    }

    static void ValidateApplicationIdentifier(string? applicationIdentifier)
    {
        if (applicationIdentifier == null || applicationIdentifier.Length < 2 || applicationIdentifier.Length > 4)
        {
            throw new BarcodeEncodingException("A GS1 Application Identifier must contain two through four digits.");
        }

        int index = 0;
        while (index < applicationIdentifier.Length)
        {
            char value = applicationIdentifier[index];
            if (value < '0' || value > '9')
            {
                throw new BarcodeEncodingException("A GS1 Application Identifier must contain only digits.");
            }

            index = index + 1;
        }
    }

    static void ValidateElementValue(string? value)
    {
        if (value == null || value.Length == 0)
        {
            throw new BarcodeEncodingException("A GS1 element value must not be empty.");
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < ' ' || character > '~')
            {
                throw new BarcodeEncodingException("A GS1 element value must contain printable ISO/IEC 646 characters.");
            }

            index = index + 1;
        }
    }

    static void WriteCharacters(
        string value,
        int[] tokens,
        byte[] bytes,
        char[] characters,
        int tokenOffset,
        int byteOffset)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            tokens[tokenOffset + index] = character;
            bytes[byteOffset + index] = (byte)character;
            characters[byteOffset + index] = character;
            index = index + 1;
        }
    }

    static Code128Options CreateCode128Options(Code128Options? source)
    {
        Code128Options settings = new Code128Options();
        if (source != null)
        {
            settings.CodeSet = source.CodeSet;
            settings.AllowFnc2 = source.AllowFnc2;
            settings.AllowFnc3 = source.AllowFnc3;
            settings.AllowFnc4 = source.AllowFnc4;
            settings.UseLatin1UpperShift = source.UseLatin1UpperShift;
        }

        settings.AllowFnc1 = true;
        return settings;
    }

    static BarcodeSymbol CreateGs1Symbol(
        Code128EncodedData encoded,
        string elementString,
        string humanReadableText,
        byte[] elementStringBytes,
        BarcodeEncodeOptions? options)
    {
        BarcodePayload layoutPayload = new BarcodePayload();
        layoutPayload.Text = humanReadableText;
        layoutPayload.RawBytes = elementStringBytes;
        layoutPayload.CharacterEncoding = "ISO/IEC 646";
        layoutPayload.IsGs1 = true;
        BarcodeSymbol symbol = Code128Layout.CreateSymbol(encoded, layoutPayload, options);

        BarcodePayload payload = new BarcodePayload();
        payload.Text = elementString;
        payload.RawBytes = ResultCopier.CopyByteArray(elementStringBytes);
        payload.CharacterEncoding = "ISO/IEC 646";
        payload.IsGs1 = true;
        symbol.Format = BarcodeFormat.Gs1128;
        symbol.Payload = payload;
        symbol.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Gs1128;
        return symbol;
    }

    static void EnforcePhysicalWidth(BarcodeSymbol symbol)
    {
        if (symbol.Size != null && symbol.Size.WidthMillimetres > MaximumPhysicalWidthMillimetres)
        {
            throw new BarcodeEncodingException("GS1-128 physical width including quiet zones must not exceed 165.10 millimetres.");
        }
    }

    static byte[]? TranslateDecodedTokens(int[] tokens)
    {
        int outputLength = tokens.Length - 1;
        if (outputLength > MaximumDataCharacterCount)
        {
            return null;
        }

        byte[] bytes = new byte[outputLength];
        bool previousWasSeparator = false;
        int index = 1;
        while (index < tokens.Length)
        {
            int token = tokens[index];
            int outputIndex = index - 1;
            if (token == Code128Planner.Function1Token)
            {
                if (outputIndex == 0 || previousWasSeparator || index == tokens.Length - 1)
                {
                    return null;
                }

                bytes[outputIndex] = GroupSeparator;
                previousWasSeparator = true;
            }
            else
            {
                if (token < 0 || token > 255)
                {
                    return null;
                }

                bytes[outputIndex] = (byte)token;
                previousWasSeparator = false;
            }

            index = index + 1;
        }

        return bytes;
    }

    static BarcodeDecodeResult CreateDecodeResult(
        Code128DecodedData decoded,
        byte[] bytes,
        string elementString)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = elementString;
        payload.RawBytes = ResultCopier.CopyByteArray(bytes);
        payload.CharacterEncoding = "ISO/IEC 646";
        payload.IsGs1 = true;

        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.HasChecksum = true;
        metadata.ChecksumValid = true;
        metadata.ChecksumValue = decoded.ChecksumValue;
        metadata.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Gs1128;
        metadata.Codewords = ResultCopier.CopyIntArray(decoded.Codewords);
        metadata.Confidence = CalculateConfidence(decoded.AverageVariance);
        metadata.IsGs1 = true;
        metadata.Gs1ElementString = elementString;

        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Payload = payload;
        result.Format = BarcodeFormat.Gs1128;
        result.Dimension = BarcodeDimension.Linear;
        result.Polarity = BarcodePolarity.DarkOnLight;
        result.MirrorMode = BarcodeMirrorMode.Normal;
        result.Metadata = metadata;
        result.Diagnostics = new BarcodeDiagnostic[0];
        return result;
    }

    static double CalculateConfidence(double averageVariance)
    {
        double confidence = 1.0 - (averageVariance / Code128ScanlineDecoder.MaximumAverageVariance);
        if (confidence < 0.0)
        {
            return 0.0;
        }

        if (confidence > 1.0)
        {
            return 1.0;
        }

        return confidence;
    }

    static string ConvertBytesToString(byte[] bytes)
    {
        char[] characters = new char[bytes.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            characters[index] = (char)bytes[index];
            index = index + 1;
        }

        return new string(characters);
    }
}
