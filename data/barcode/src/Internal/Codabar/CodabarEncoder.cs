namespace barcode;

internal class CodabarEncodedData
{
    internal string InputText = "";
    internal string DataText = "";
    internal string CarrierDataText = "";
    internal string EncodedText = "";
    internal string TransmittedText = "";
    internal char StartCharacter;
    internal char StopCharacter;
    internal char DisplayStartCharacter;
    internal char DisplayStopCharacter;
    internal char CheckCharacter;
    internal int ChecksumValue;
    internal bool HasChecksum;
    internal bool WasFramedInput;
    internal BarcodeChecksumMode ChecksumMode;
    internal int[] Values = new int[0];
    internal int[] PatternBits = new int[0];
}

internal class CodabarFramingData
{
    internal string DataText = "";
    internal char StartCharacter;
    internal char StopCharacter;
    internal char DisplayStartCharacter;
    internal char DisplayStopCharacter;
    internal bool WasFramedInput;
}

internal static class CodabarEncoder
{
    internal const char DefaultStartStopCharacter = 'A';

    internal static CodabarEncodedData Encode(
        string text,
        CodabarOptions options,
        BarcodeChecksumMode checksumMode)
    {
        if (options == null)
        {
            throw new BarcodeEncodingException("Codabar options must not be null.");
        }

        ValidateChecksumMode(checksumMode);
        CodabarFramingData framing = ResolveFraming(text, options);
        string dataText;
        string carrierDataText;
        char checkCharacter = '\0';
        int checksumValue = 0;
        bool hasChecksum = checksumMode != BarcodeChecksumMode.None;
        if (checksumMode == BarcodeChecksumMode.None)
        {
            CodabarPatterns.ValidateDataText(framing.DataText);
            dataText = framing.DataText;
            carrierDataText = dataText;
        }
        else if (checksumMode == BarcodeChecksumMode.Calculate)
        {
            CodabarPatterns.ValidateDataText(framing.DataText);
            dataText = framing.DataText;
            string framedData = CreateFramedText(
                framing.StartCharacter,
                dataText,
                framing.StopCharacter);
            checkCharacter = CodabarModulo16.CalculateCheckCharacter(framedData);
            checksumValue = CodabarPatterns.GetValue(checkCharacter);
            carrierDataText = dataText + checkCharacter;
        }
        else
        {
            if (framing.DataText.Length < 2)
            {
                throw new BarcodeEncodingException("Codabar verification requires data and a supplied check character.");
            }

            CodabarPatterns.ValidateDataText(framing.DataText);
            int checkIndex = framing.DataText.Length - 1;
            dataText = framing.DataText.Substring(0, checkIndex);
            checkCharacter = framing.DataText[checkIndex];
            string completeText = CreateFramedText(
                framing.StartCharacter,
                framing.DataText,
                framing.StopCharacter);
            if (!CodabarModulo16.VerifyCompleteText(completeText))
            {
                throw new BarcodeEncodingException("The supplied Codabar check character is invalid.");
            }

            checksumValue = CodabarPatterns.GetValue(checkCharacter);
            carrierDataText = framing.DataText;
        }

        bool transmitCheck = IsCheckCharacterTransmitted(checksumMode, options);
        string transmittedData = transmitCheck ? dataText + checkCharacter : dataText;
        string transmittedText = options.TransmitStartStop
            ? framing.DisplayStartCharacter + transmittedData + framing.DisplayStopCharacter
            : transmittedData;
        string encodedText = CreateFramedText(
            framing.StartCharacter,
            carrierDataText,
            framing.StopCharacter);
        int[] values = ConvertTextToValues(encodedText);
        int[] patterns = ConvertValuesToPatterns(values);

        CodabarEncodedData encoded = new CodabarEncodedData();
        encoded.InputText = text;
        encoded.DataText = dataText;
        encoded.CarrierDataText = carrierDataText;
        encoded.EncodedText = encodedText;
        encoded.TransmittedText = transmittedText;
        encoded.StartCharacter = framing.StartCharacter;
        encoded.StopCharacter = framing.StopCharacter;
        encoded.DisplayStartCharacter = framing.DisplayStartCharacter;
        encoded.DisplayStopCharacter = framing.DisplayStopCharacter;
        encoded.CheckCharacter = checkCharacter;
        encoded.ChecksumValue = checksumValue;
        encoded.HasChecksum = hasChecksum;
        encoded.WasFramedInput = framing.WasFramedInput;
        encoded.ChecksumMode = checksumMode;
        encoded.Values = values;
        encoded.PatternBits = patterns;
        return encoded;
    }

    static CodabarFramingData ResolveFraming(string text, CodabarOptions options)
    {
        if (text == null)
        {
            throw new BarcodeEncodingException("Codabar text must not be null.");
        }

        if (text.Length == 0)
        {
            throw new BarcodeEncodingException("Codabar text must not be empty.");
        }

        char requestedStart = ResolveRequestedTerminal(
            options.StartCharacter,
            DefaultStartStopCharacter,
            options.AcceptAliases,
            "start");
        char requestedStop = ResolveRequestedTerminal(
            options.StopCharacter,
            DefaultStartStopCharacter,
            options.AcceptAliases,
            "stop");
        char displayStart = ResolveDisplayTerminal(
            options.StartCharacter,
            DefaultStartStopCharacter);
        char displayStop = ResolveDisplayTerminal(
            options.StopCharacter,
            DefaultStartStopCharacter);
        bool firstIsTerminal = CodabarPatterns.IsStartStopInput(text[0], options.AcceptAliases);
        bool lastIsTerminal = CodabarPatterns.IsStartStopInput(
            text[text.Length - 1],
            options.AcceptAliases);
        if (firstIsTerminal != lastIsTerminal)
        {
            throw new BarcodeEncodingException("Codabar text must contain both start and stop characters or neither.");
        }

        CodabarFramingData framing = new CodabarFramingData();
        if (firstIsTerminal)
        {
            if (text.Length < 3)
            {
                throw new BarcodeEncodingException("Framed Codabar text requires at least one data character.");
            }

            char observedStart = NormalizeTerminal(text[0], options.AcceptAliases);
            char observedStop = NormalizeTerminal(text[text.Length - 1], options.AcceptAliases);
            if (options.StartCharacter != null && observedStart != requestedStart)
            {
                throw new BarcodeEncodingException("Codabar input start character conflicts with the selected start character.");
            }

            if (options.StopCharacter != null && observedStop != requestedStop)
            {
                throw new BarcodeEncodingException("Codabar input stop character conflicts with the selected stop character.");
            }

            framing.DataText = text.Substring(1, text.Length - 2);
            framing.StartCharacter = observedStart;
            framing.StopCharacter = observedStop;
            framing.DisplayStartCharacter = text[0];
            framing.DisplayStopCharacter = text[text.Length - 1];
            framing.WasFramedInput = true;
        }
        else
        {
            framing.DataText = text;
            framing.StartCharacter = requestedStart;
            framing.StopCharacter = requestedStop;
            framing.DisplayStartCharacter = displayStart;
            framing.DisplayStopCharacter = displayStop;
        }

        return framing;
    }

    static char ResolveRequestedTerminal(
        string? selected,
        char defaultCharacter,
        bool acceptAliases,
        string terminalName)
    {
        if (selected == null)
        {
            return defaultCharacter;
        }

        if (selected.Length != 1)
        {
            throw new BarcodeEncodingException("Codabar " + terminalName + " character must contain exactly one character.");
        }

        return NormalizeTerminal(selected[0], acceptAliases);
    }

    static char ResolveDisplayTerminal(string? selected, char defaultCharacter)
    {
        if (selected == null)
        {
            return defaultCharacter;
        }

        return selected[0];
    }

    static char NormalizeTerminal(char character, bool acceptAliases)
    {
        if (!CodabarPatterns.IsStartStopInput(character, acceptAliases))
        {
            throw new BarcodeEncodingException("Codabar terminal must be A, B, C, or D, or an enabled alias.");
        }

        return CodabarPatterns.NormalizeStartStop(character, acceptAliases);
    }

    static bool IsCheckCharacterTransmitted(
        BarcodeChecksumMode checksumMode,
        CodabarOptions options)
    {
        if (checksumMode == BarcodeChecksumMode.VerifyKeep)
        {
            return true;
        }

        if (checksumMode == BarcodeChecksumMode.VerifyStrip ||
            checksumMode == BarcodeChecksumMode.None)
        {
            return false;
        }

        return options.TransmitCheckCharacter;
    }

    static string CreateFramedText(char start, string data, char stop)
    {
        return start + data + stop;
    }

    static int[] ConvertTextToValues(string text)
    {
        int[] values = new int[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            values[index] = CodabarPatterns.GetValue(text[index]);
            index = index + 1;
        }

        return values;
    }

    static int[] ConvertValuesToPatterns(int[] values)
    {
        int[] patterns = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            patterns[index] = CodabarPatterns.GetCharacterPatternBits(
                CodabarPatterns.GetCharacter(values[index]));
            index = index + 1;
        }

        return patterns;
    }

    static void ValidateChecksumMode(BarcodeChecksumMode checksumMode)
    {
        if (checksumMode != BarcodeChecksumMode.None &&
            checksumMode != BarcodeChecksumMode.Calculate &&
            checksumMode != BarcodeChecksumMode.Require &&
            checksumMode != BarcodeChecksumMode.VerifyKeep &&
            checksumMode != BarcodeChecksumMode.VerifyStrip)
        {
            throw new BarcodeEncodingException("Codabar checksum mode is invalid.");
        }
    }
}
