namespace barcode;

internal enum ChecksumAlgorithmKind
{
    Gs1Modulo10 = 1,
    Code128Modulo103 = 2,
    Code39Modulo43 = 3,
    CodabarModulo16 = 4
}

internal static class ChecksumDispatcher
{
    internal static BarcodeChecksumResult CalculateChecksum(BarcodeChecksumRequest request)
    {
        ValidateRequest(request);
        ChecksumAlgorithmKind algorithm = ResolveAlgorithm(request);
        int calculatedValue = CalculateValue(request, algorithm);
        BarcodeChecksumResult result = new BarcodeChecksumResult();
        result.Format = request.Format;
        result.Algorithm = GetAlgorithmName(algorithm);
        result.CalculatedValue = calculatedValue;
        result.HasObservedValue = false;
        result.ObservedValue = 0;
        result.IsValid = false;
        return result;
    }

    internal static BarcodeChecksumResult VerifyChecksum(BarcodeChecksumRequest request)
    {
        ValidateRequest(request);
        ChecksumAlgorithmKind algorithm = ResolveAlgorithm(request);
        if (!request.HasObservedValue)
        {
            throw new BarcodeValidationException("Checksum verification requires an observed value.");
        }

        int calculatedValue = CalculateValue(request, algorithm);
        ValidateObservedValue(request.ObservedValue, algorithm);
        BarcodeChecksumResult result = new BarcodeChecksumResult();
        result.Format = request.Format;
        result.Algorithm = GetAlgorithmName(algorithm);
        result.CalculatedValue = calculatedValue;
        result.HasObservedValue = true;
        result.ObservedValue = request.ObservedValue;
        result.IsValid = calculatedValue == request.ObservedValue;
        return result;
    }

    internal static void ValidateRequest(BarcodeChecksumRequest request)
    {
        if (request == null)
        {
            throw new BarcodeValidationException("Barcode checksum request must not be null.");
        }
    }

    internal static ChecksumAlgorithmKind ResolveAlgorithm(BarcodeChecksumRequest request)
    {
        string? algorithmName = request.Algorithm;
        if (algorithmName != null)
        {
            ChecksumAlgorithmKind namedAlgorithm = ResolveNamedAlgorithm(algorithmName);
            ValidateFormatCompatibility(request.Format, namedAlgorithm);
            return namedAlgorithm;
        }

        return ResolveFormatAlgorithm(request.Format);
    }

    internal static ChecksumAlgorithmKind ResolveNamedAlgorithm(string algorithmName)
    {
        switch (algorithmName)
        {
            case "GS1 modulo-10":
            case "Gs1Modulo10":
            case "Modulo10":
                return ChecksumAlgorithmKind.Gs1Modulo10;
            case "Code 128 modulo-103":
            case "Code128Modulo103":
            case "Modulo103":
                return ChecksumAlgorithmKind.Code128Modulo103;
            case "Code 39 modulo-43":
            case "Code39Modulo43":
            case "Modulo43":
                return ChecksumAlgorithmKind.Code39Modulo43;
            case "Codabar modulo-16":
            case "CodabarModulo16":
            case "Modulo16":
                return ChecksumAlgorithmKind.CodabarModulo16;
            default:
                throw new BarcodeValidationException("The requested checksum algorithm is not supported.");
        }
    }

    internal static ChecksumAlgorithmKind ResolveFormatAlgorithm(BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Ean8:
            case BarcodeFormat.Ean13:
            case BarcodeFormat.UpcA:
            case BarcodeFormat.Itf:
            case BarcodeFormat.Itf14:
                return ChecksumAlgorithmKind.Gs1Modulo10;
            case BarcodeFormat.Code128:
            case BarcodeFormat.Gs1128:
                return ChecksumAlgorithmKind.Code128Modulo103;
            case BarcodeFormat.Code39:
                return ChecksumAlgorithmKind.Code39Modulo43;
            case BarcodeFormat.Codabar:
                return ChecksumAlgorithmKind.CodabarModulo16;
            default:
                throw new BarcodeValidationException("The barcode format does not select a supported checksum algorithm.");
        }
    }

    internal static void ValidateFormatCompatibility(BarcodeFormat format, ChecksumAlgorithmKind algorithm)
    {
        if (format == BarcodeFormat.Automatic)
        {
            return;
        }

        ChecksumAlgorithmKind formatAlgorithm = ResolveFormatAlgorithm(format);
        if (formatAlgorithm != algorithm)
        {
            throw new BarcodeValidationException("The checksum algorithm is not compatible with the barcode format.");
        }
    }

    internal static int CalculateValue(BarcodeChecksumRequest request, ChecksumAlgorithmKind algorithm)
    {
        switch (algorithm)
        {
            case ChecksumAlgorithmKind.Gs1Modulo10:
                return CalculateGs1Value(request);
            case ChecksumAlgorithmKind.Code128Modulo103:
                return CalculateCode128Value(request);
            case ChecksumAlgorithmKind.Code39Modulo43:
                return CalculateCode39Value(request);
            case ChecksumAlgorithmKind.CodabarModulo16:
                return CalculateCodabarValue(request);
            default:
                throw new BarcodeValidationException("The requested checksum algorithm is not supported.");
        }
    }

    internal static int CalculateGs1Value(BarcodeChecksumRequest request)
    {
        string? text = request.Text;
        if (text == null)
        {
            throw new BarcodeValidationException("GS1 checksum calculation requires text digits.");
        }

        ValidateGs1TextLength(request);
        return Gs1Modulo10.CalculateCheckDigit(text);
    }

    internal static int CalculateCode128Value(BarcodeChecksumRequest request)
    {
        int[]? codewords = request.Codewords;
        if (codewords == null)
        {
            throw new BarcodeValidationException("Code 128 checksum calculation requires codewords.");
        }

        return Code128Modulo103.CalculateCheckCodeword(codewords);
    }

    internal static int CalculateCode39Value(BarcodeChecksumRequest request)
    {
        string? text = request.Text;
        if (text == null)
        {
            throw new BarcodeValidationException("Code 39 checksum calculation requires text.");
        }

        return Code39Modulo43.CalculateCheckValue(text);
    }

    internal static int CalculateCodabarValue(BarcodeChecksumRequest request)
    {
        string? text = request.Text;
        if (text == null)
        {
            throw new BarcodeValidationException("Codabar checksum calculation requires framed text.");
        }

        return CodabarModulo16.CalculateCheckValue(text);
    }

    internal static void ValidateGs1TextLength(BarcodeChecksumRequest request)
    {
        string? text = request.Text;
        if (text == null)
        {
            throw new BarcodeValidationException("GS1 checksum calculation requires text digits.");
        }

        int requiredLength = 0;
        switch (request.Format)
        {
            case BarcodeFormat.Ean8:
                requiredLength = 7;
                break;
            case BarcodeFormat.Ean13:
                requiredLength = 12;
                break;
            case BarcodeFormat.UpcA:
                requiredLength = 11;
                break;
            case BarcodeFormat.Itf14:
                requiredLength = 13;
                break;
        }

        if (requiredLength != 0 && text.Length != requiredLength)
        {
            throw new BarcodeValidationException("The checksum source length is invalid for the barcode format.");
        }

        if (request.Format == BarcodeFormat.Itf && text.Length % 2 != 1)
        {
            throw new BarcodeValidationException("ITF checksum source text must become even-length after the check digit is appended.");
        }
    }

    internal static void ValidateObservedValue(int observedValue, ChecksumAlgorithmKind algorithm)
    {
        int maximumValue = 0;
        switch (algorithm)
        {
            case ChecksumAlgorithmKind.Gs1Modulo10:
                maximumValue = 9;
                break;
            case ChecksumAlgorithmKind.Code128Modulo103:
                maximumValue = 102;
                break;
            case ChecksumAlgorithmKind.Code39Modulo43:
                maximumValue = 42;
                break;
            case ChecksumAlgorithmKind.CodabarModulo16:
                maximumValue = 15;
                break;
        }

        if (observedValue < 0 || observedValue > maximumValue)
        {
            throw new BarcodeValidationException("The observed checksum value is outside the algorithm range.");
        }
    }

    internal static string GetAlgorithmName(ChecksumAlgorithmKind algorithm)
    {
        switch (algorithm)
        {
            case ChecksumAlgorithmKind.Gs1Modulo10:
                return Gs1Modulo10.AlgorithmName;
            case ChecksumAlgorithmKind.Code128Modulo103:
                return Code128Modulo103.AlgorithmName;
            case ChecksumAlgorithmKind.Code39Modulo43:
                return Code39Modulo43.AlgorithmName;
            case ChecksumAlgorithmKind.CodabarModulo16:
                return CodabarModulo16.AlgorithmName;
            default:
                throw new BarcodeValidationException("The requested checksum algorithm is not supported.");
        }
    }
}
