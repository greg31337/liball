namespace barcode;

internal class ItfEncodedData
{
    internal string InputText = "";
    internal string DataText = "";
    internal string EncodedText = "";
    internal string TransmittedText = "";
    internal int[] Digits = new int[0];
    internal int[] BarDigitPatternBits = new int[0];
    internal int[] SpaceDigitPatternBits = new int[0];
    internal bool HasChecksum;
    internal int ChecksumValue;
    internal BarcodeChecksumMode ChecksumMode;
}

internal static class ItfEncoder
{
    internal static ItfEncodedData Encode(
        string text,
        BarcodeChecksumMode checksumMode,
        int[]? allowedLengths)
    {
        ValidateChecksumMode(checksumMode);
        ValidateDigits(text);

        string dataText;
        string encodedText;
        string transmittedText;
        bool hasChecksum = checksumMode != BarcodeChecksumMode.None;
        int checksumValue = 0;
        if (checksumMode == BarcodeChecksumMode.None)
        {
            RequireEvenLength(text);
            dataText = text;
            encodedText = text;
            transmittedText = text;
        }
        else if (checksumMode == BarcodeChecksumMode.Calculate)
        {
            if (text.Length % 2 != 1)
            {
                throw new BarcodeEncodingException("ITF data must have odd length before a calculated check digit is appended.");
            }

            checksumValue = Gs1Modulo10.CalculateCheckDigit(text);
            dataText = text;
            encodedText = text + (char)('0' + checksumValue);
            transmittedText = encodedText;
        }
        else
        {
            RequireEvenLength(text);
            if (text.Length < 2)
            {
                throw new BarcodeEncodingException("ITF verification requires data and a supplied check digit.");
            }

            int checkIndex = text.Length - 1;
            checksumValue = text[checkIndex] - '0';
            dataText = text.Substring(0, checkIndex);
            if (!Gs1Modulo10.VerifyCheckDigit(dataText, checksumValue))
            {
                throw new BarcodeEncodingException("The supplied ITF check digit is invalid.");
            }

            encodedText = text;
            transmittedText = checksumMode == BarcodeChecksumMode.VerifyStrip
                ? dataText
                : encodedText;
        }

        ValidateAllowedLengths(encodedText.Length, allowedLengths);
        int[] digits = ConvertDigits(encodedText);
        int pairCount = digits.Length / 2;
        int[] barPatterns = new int[pairCount];
        int[] spacePatterns = new int[pairCount];
        int pairIndex = 0;
        while (pairIndex < pairCount)
        {
            barPatterns[pairIndex] = ItfPatterns.GetDigitPatternBits(digits[pairIndex * 2]);
            spacePatterns[pairIndex] = ItfPatterns.GetDigitPatternBits(digits[(pairIndex * 2) + 1]);
            pairIndex = pairIndex + 1;
        }

        ItfEncodedData encoded = new ItfEncodedData();
        encoded.InputText = text;
        encoded.DataText = dataText;
        encoded.EncodedText = encodedText;
        encoded.TransmittedText = transmittedText;
        encoded.Digits = digits;
        encoded.BarDigitPatternBits = barPatterns;
        encoded.SpaceDigitPatternBits = spacePatterns;
        encoded.HasChecksum = hasChecksum;
        encoded.ChecksumValue = checksumValue;
        encoded.ChecksumMode = checksumMode;
        return encoded;
    }

    static void ValidateChecksumMode(BarcodeChecksumMode checksumMode)
    {
        if (checksumMode != BarcodeChecksumMode.None &&
            checksumMode != BarcodeChecksumMode.Calculate &&
            checksumMode != BarcodeChecksumMode.Require &&
            checksumMode != BarcodeChecksumMode.VerifyKeep &&
            checksumMode != BarcodeChecksumMode.VerifyStrip)
        {
            throw new BarcodeEncodingException("ITF checksum mode is invalid.");
        }
    }

    static void ValidateDigits(string text)
    {
        if (text == null)
        {
            throw new BarcodeEncodingException("ITF text must not be null.");
        }

        if (text.Length == 0)
        {
            throw new BarcodeEncodingException("ITF text must not be empty.");
        }

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                throw new BarcodeEncodingException("ITF text must contain only ASCII digits.");
            }

            index = index + 1;
        }
    }

    static void RequireEvenLength(string text)
    {
        if (text.Length % 2 != 0)
        {
            throw new BarcodeEncodingException("ITF encoded text must contain an even number of digits.");
        }
    }

    static void ValidateAllowedLengths(int encodedLength, int[]? allowedLengths)
    {
        if (allowedLengths == null)
        {
            return;
        }

        if (allowedLengths.Length == 0)
        {
            throw new BarcodeEncodingException("ITF allowed lengths must not be empty when supplied.");
        }

        bool matched = false;
        int index = 0;
        while (index < allowedLengths.Length)
        {
            int allowedLength = allowedLengths[index];
            if (allowedLength <= 0 || allowedLength % 2 != 0)
            {
                throw new BarcodeEncodingException("ITF allowed lengths must contain positive even values.");
            }

            if (allowedLength == encodedLength)
            {
                matched = true;
            }

            index = index + 1;
        }

        if (!matched)
        {
            throw new BarcodeEncodingException("ITF encoded length is not permitted by the selected length policy.");
        }
    }

    static int[] ConvertDigits(string text)
    {
        int[] digits = new int[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            digits[index] = text[index] - '0';
            index = index + 1;
        }

        return digits;
    }
}
