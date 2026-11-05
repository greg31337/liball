namespace barcode;

internal class Gs1DataMatrixEncodedData
{
    internal DataMatrixEncodingResult Encoding = new DataMatrixEncodingResult();
    internal BarcodeSymbol Symbol = new BarcodeSymbol();
    internal byte[] ElementStringBytes = new byte[0];
    internal string ElementString = "";
    internal string HumanReadableText = "";
}

internal class Gs1DataMatrixDecodedData
{
    internal DataMatrixPureDecodeResult Decode = new DataMatrixPureDecodeResult();
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal string ElementString = "";
}

internal static class Gs1DataMatrixCodec
{
    internal const int GroupSeparator = 29;
    internal const double NominalModuleSizeMillimetres = 0.495;
    internal const double MinimumModuleSizeMillimetres = 0.396;
    internal const double MaximumModuleSizeMillimetres = 0.990;
    internal const double PhysicalComparisonTolerance = 0.000001;

    internal static Gs1DataMatrixEncodedData EncodeElements(
        Gs1Element[] elements,
        DataMatrixOptions? dataMatrixOptions,
        BarcodeEncodeOptions? encodeOptions)
    {
        Gs1HriResult hri = Gs1HriFormatter.GenerateHri(
            elements,
            false,
            true);
        Gs1Element[] orderedElements = hri.Elements;
        int byteCount = CountElementStringBytes(orderedElements);
        byte[] bytes = new byte[byteCount];
        char[] characters = new char[byteCount];
        WriteElements(orderedElements, bytes, characters);
        if (!IsValidElementString(bytes))
        {
            throw new BarcodeEncodingException("GS1 DataMatrix elements do not form a valid GS1 element string.");
        }

        string elementString = new string(characters);
        string humanReadableText = hri.CompactText;

        BarcodePayload layoutPayload = new BarcodePayload();
        layoutPayload.Text = humanReadableText;
        layoutPayload.RawBytes = bytes;
        layoutPayload.CharacterEncoding = "ISO/IEC 646";
        layoutPayload.IsGs1 = true;
        BarcodeEncodeOptions common = CreateProfileOptions(encodeOptions);
        if (dataMatrixOptions != null && dataMatrixOptions.EciAssignmentNumber != 0)
        {
            throw new BarcodeEncodingException("GS1 DataMatrix element data does not use an ECI assignment.");
        }

        DataMatrixEncodingResult encoding = DataMatrixEncoder.Encode(
            bytes,
            layoutPayload,
            dataMatrixOptions,
            common,
            true);
        ValidateProfileLayout(encoding.Symbol);
        ApplyProfilePayload(encoding.Symbol, elementString, bytes);

        Gs1DataMatrixEncodedData result = new Gs1DataMatrixEncodedData();
        result.Encoding = encoding;
        result.Symbol = encoding.Symbol;
        result.ElementStringBytes = CopyBytes(bytes);
        result.ElementString = elementString;
        result.HumanReadableText = humanReadableText;
        return result;
    }

    internal static Gs1DataMatrixDecodedData? TryDecodePure(
        ModuleMatrix modules,
        BarcodeDecodeOptions? options)
    {
        DataMatrixPureDecodeResult? decoded = DataMatrixPureDecoder.TryDecode(modules, options);
        if (decoded == null
            || decoded.Result.Format != BarcodeFormat.Gs1DataMatrix
            || decoded.Result.Payload == null
            || decoded.Result.Payload.RawBytes == null
            || !IsValidElementString(decoded.Result.Payload.RawBytes))
        {
            return null;
        }

        string elementString = ConvertBytesToString(decoded.Result.Payload.RawBytes);
        if (decoded.Result.Metadata == null)
        {
            decoded.Result.Metadata = new BarcodeDecodeMetadata();
        }

        decoded.Result.Metadata.IsGs1 = true;
        decoded.Result.Metadata.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Gs1DataMatrix;
        decoded.Result.Metadata.Gs1ElementString = elementString;
        Gs1DataMatrixDecodedData result = new Gs1DataMatrixDecodedData();
        result.Decode = decoded;
        result.Result = decoded.Result;
        result.ElementString = elementString;
        return result;
    }

    static int CountElementStringBytes(Gs1Element[] elements)
    {
        ValidateElements(elements);
        int count = 0;
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            string applicationIdentifier = element.ApplicationIdentifier == null
                ? ""
                : element.ApplicationIdentifier;
            string value = element.Value == null ? "" : element.Value;
            count = CheckedMath.Add(count, applicationIdentifier.Length);
            count = CheckedMath.Add(count, value.Length);
            if (Gs1HriFormatter.IsVariableLengthElement(element)
                && index < elements.Length - 1)
            {
                count = CheckedMath.Add(count, 1);
            }

            if (count > DataMatrixMinimalEncoder.MaximumInputByteCount)
            {
                throw new BarcodeEncodingException("GS1 DataMatrix element string exceeds the maximum input length.");
            }

            index = index + 1;
        }

        return count;
    }

    static void ValidateElements(Gs1Element[] elements)
    {
        if (elements == null || elements.Length == 0)
        {
            throw new BarcodeEncodingException("GS1 DataMatrix requires at least one GS1 element.");
        }

        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element == null)
            {
                throw new BarcodeEncodingException("GS1 DataMatrix elements must not contain null entries.");
            }

            ValidateApplicationIdentifier(element.ApplicationIdentifier);
            ValidateValue(element.Value);
            index = index + 1;
        }
    }

    static void ValidateApplicationIdentifier(string? applicationIdentifier)
    {
        if (applicationIdentifier == null
            || applicationIdentifier.Length < 2
            || applicationIdentifier.Length > 4)
        {
            throw new BarcodeEncodingException("A GS1 Application Identifier must contain two through four digits.");
        }

        int index = 0;
        while (index < applicationIdentifier.Length)
        {
            char character = applicationIdentifier[index];
            if (character < '0' || character > '9')
            {
                throw new BarcodeEncodingException("A GS1 Application Identifier must contain only digits.");
            }

            index = index + 1;
        }
    }

    static void ValidateValue(string? value)
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

    static void WriteElements(
        Gs1Element[] elements,
        byte[] bytes,
        char[] characters)
    {
        int byteOffset = 0;
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            string applicationIdentifier = element.ApplicationIdentifier == null
                ? ""
                : element.ApplicationIdentifier;
            string value = element.Value == null ? "" : element.Value;
            WriteText(applicationIdentifier, bytes, characters, byteOffset);
            byteOffset = byteOffset + applicationIdentifier.Length;
            WriteText(value, bytes, characters, byteOffset);
            byteOffset = byteOffset + value.Length;
            if (Gs1HriFormatter.IsVariableLengthElement(element)
                && index < elements.Length - 1)
            {
                bytes[byteOffset] = GroupSeparator;
                characters[byteOffset] = (char)GroupSeparator;
                byteOffset = byteOffset + 1;
            }

            index = index + 1;
        }
    }

    static void WriteText(string value, byte[] bytes, char[] characters, int offset)
    {
        int index = 0;
        while (index < value.Length)
        {
            bytes[offset + index] = (byte)value[index];
            characters[offset + index] = value[index];
            index = index + 1;
        }
    }

    static BarcodeEncodeOptions CreateProfileOptions(BarcodeEncodeOptions? source)
    {
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        if (source != null)
        {
            options.Sizing = CopySizing(source.Sizing);
            options.QuietZonePolicy = source.QuietZonePolicy;
            options.QuietZone = CopyQuietZone(source.QuietZone);
            options.ChecksumMode = source.ChecksumMode;
            options.TextMode = source.TextMode;
            options.CustomText = source.CustomText;
            options.CharacterEncoding = source.CharacterEncoding;
            options.EciAssignmentNumber = source.EciAssignmentNumber;
            options.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(source.StructuredAppend);
        }

        if (options.CharacterEncoding != null
            && options.CharacterEncoding != "ISO/IEC 646"
            && options.CharacterEncoding != "ASCII")
        {
            throw new BarcodeEncodingException("GS1 DataMatrix element data uses ISO/IEC 646 character encoding.");
        }

        options.CharacterEncoding = "ISO/IEC 646";
        if (options.EciAssignmentNumber != 0)
        {
            throw new BarcodeEncodingException("GS1 DataMatrix element data does not use an ECI assignment.");
        }

        if (options.Sizing == null)
        {
            options.Sizing = new BarcodeSizing();
        }

        BarcodeSizing sizing = options.Sizing;
        bool derivesPhysicalModuleSize = sizing.RequestedWidthMillimetres > 0.0
            || sizing.RequestedHeightMillimetres > 0.0
            || ((sizing.RequestedWidthPixels > 0 || sizing.RequestedHeightPixels > 0)
                && sizing.DotsPerInch > 0.0);
        if (sizing.ModuleSizeMillimetres == 0.0 && !derivesPhysicalModuleSize)
        {
            sizing.ModuleSizeMillimetres = NominalModuleSizeMillimetres;
        }

        ValidateProfileQuietZone(options);
        return options;
    }

    static void ValidateProfileQuietZone(BarcodeEncodeOptions options)
    {
        if (options.QuietZonePolicy == BarcodeQuietZonePolicy.Standard)
        {
            return;
        }

        BarcodeQuietZone? quietZone = options.QuietZone;
        if (quietZone == null && options.Sizing != null)
        {
            quietZone = options.Sizing.QuietZone;
        }

        if (quietZone != null
            && (quietZone.LeftModules < 1
                || quietZone.TopModules < 1
                || quietZone.RightModules < 1
                || quietZone.BottomModules < 1))
        {
            throw new BarcodeEncodingException("GS1 DataMatrix quiet zones must be at least one module on every side.");
        }
    }

    static void ValidateProfileLayout(BarcodeSymbol symbol)
    {
        if (symbol.Size == null)
        {
            throw new BarcodeValidationException("GS1 DataMatrix layout did not produce sizing metadata.");
        }

        double moduleSize = symbol.Size.ModuleSizeMillimetres;
        if (moduleSize + PhysicalComparisonTolerance < MinimumModuleSizeMillimetres
            || moduleSize - PhysicalComparisonTolerance > MaximumModuleSizeMillimetres)
        {
            throw new BarcodeEncodingException("GS1 DataMatrix module size is outside the general-distribution profile range.");
        }
    }

    static void ApplyProfilePayload(BarcodeSymbol symbol, string elementString, byte[] bytes)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = elementString;
        payload.RawBytes = CopyBytes(bytes);
        payload.CharacterEncoding = "ISO/IEC 646";
        payload.IsGs1 = true;
        if (symbol.Payload != null)
        {
            payload.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(
                symbol.Payload.StructuredAppend);
        }

        symbol.Payload = payload;
        symbol.Format = BarcodeFormat.Gs1DataMatrix;
        symbol.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Gs1DataMatrix;
    }

    internal static bool IsValidElementString(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return false;
        }

        bool previousWasSeparator = false;
        int index = 0;
        while (index < bytes.Length)
        {
            int value = bytes[index];
            if (value == GroupSeparator)
            {
                if (index == 0 || index == bytes.Length - 1 || previousWasSeparator)
                {
                    return false;
                }

                previousWasSeparator = true;
            }
            else
            {
                if (value < 32 || value > 126)
                {
                    return false;
                }

                previousWasSeparator = false;
            }

            index = index + 1;
        }

        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementBytes(bytes);
        return parsed.Success;
    }

    internal static string ConvertBytesToString(byte[] bytes)
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

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static BarcodeSizing? CopySizing(BarcodeSizing? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeSizing copy = new BarcodeSizing();
        copy.RequestedWidthModules = source.RequestedWidthModules;
        copy.RequestedHeightModules = source.RequestedHeightModules;
        copy.RequestedWidthPixels = source.RequestedWidthPixels;
        copy.RequestedHeightPixels = source.RequestedHeightPixels;
        copy.RequestedWidthMillimetres = source.RequestedWidthMillimetres;
        copy.RequestedHeightMillimetres = source.RequestedHeightMillimetres;
        copy.DotsPerInch = source.DotsPerInch;
        copy.ModuleSizeMillimetres = source.ModuleSizeMillimetres;
        copy.BarHeightMillimetres = source.BarHeightMillimetres;
        copy.TargetAspectRatio = source.TargetAspectRatio;
        copy.QuietZone = CopyQuietZone(source.QuietZone);
        return copy;
    }

    static BarcodeQuietZone? CopyQuietZone(BarcodeQuietZone? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeQuietZone copy = new BarcodeQuietZone();
        copy.LeftModules = source.LeftModules;
        copy.TopModules = source.TopModules;
        copy.RightModules = source.RightModules;
        copy.BottomModules = source.BottomModules;
        copy.LeftPixels = source.LeftPixels;
        copy.TopPixels = source.TopPixels;
        copy.RightPixels = source.RightPixels;
        copy.BottomPixels = source.BottomPixels;
        return copy;
    }
}
