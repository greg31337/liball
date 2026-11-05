namespace ttf;

internal class CffDictOperand
{
    bool integer;
    int integerValue;
    double numberValue;

    CffDictOperand(bool integer, int integerValue, double numberValue)
    {
        this.integer = integer;
        this.integerValue = integerValue;
        this.numberValue = numberValue;
    }

    internal static CffDictOperand CreateInteger(int value)
    {
        return new CffDictOperand(true, value, value);
    }

    internal static CffDictOperand CreateReal(double value)
    {
        return new CffDictOperand(false, 0, value);
    }

    internal bool IsInteger()
    {
        return integer;
    }

    internal int GetInteger()
    {
        if (!integer)
        {
            throw FontFormatException.Create(
                "A CFF DICT operand must be an integer.");
        }

        return integerValue;
    }

    internal double GetNumber()
    {
        return numberValue;
    }
}

internal class CffDictEntry
{
    int operatorCode;
    CffDictOperand[] operands;

    internal CffDictEntry(
        int operatorCode,
        CffDictOperand[] operands)
    {
        this.operatorCode = operatorCode;
        this.operands = operands;
    }

    internal int GetOperator()
    {
        return operatorCode;
    }

    internal bool IsEscapedOperator()
    {
        return operatorCode >= 0x0C00;
    }

    internal int GetOperandCount()
    {
        return operands.Length;
    }

    internal CffDictOperand GetOperand(int index)
    {
        if (index < 0 || index >= operands.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return operands[index];
    }

    internal double[] CopyNumbers()
    {
        double[] values = new double[operands.Length];
        int index = 0;

        while (index < values.Length)
        {
            values[index] = operands[index].GetNumber();
            index += 1;
        }

        return values;
    }

    internal double[] CopyDeltaValues()
    {
        double[] values = new double[operands.Length];
        double previous = 0;
        int index = 0;

        while (index < values.Length)
        {
            previous += operands[index].GetNumber();
            values[index] = previous;
            index += 1;
        }

        return values;
    }
}

internal class CffDictionary
{
    CffDictEntry[] entries;

    internal CffDictionary(CffDictEntry[] entries)
    {
        this.entries = entries;
    }

    internal int GetCount()
    {
        return entries.Length;
    }

    internal CffDictEntry GetEntry(int index)
    {
        if (index < 0 || index >= entries.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return entries[index];
    }

    internal CffDictEntry? FindEntry(int operatorCode)
    {
        CffDictEntry? found = null;
        int index = 0;

        while (index < entries.Length)
        {
            if (entries[index].GetOperator() == operatorCode)
            {
                if (found != null)
                {
                    throw FontFormatException.Create(
                        "A CFF DICT contains a duplicate operator.");
                }

                found = entries[index];
            }

            index += 1;
        }

        return found;
    }

    internal int GetEntryCount(int operatorCode)
    {
        int count = 0;
        int index = 0;

        while (index < entries.Length)
        {
            if (entries[index].GetOperator() == operatorCode)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }
}

internal static class CffDictOperator
{
    internal const int Version = 0;
    internal const int Notice = 1;
    internal const int FullName = 2;
    internal const int FamilyName = 3;
    internal const int Weight = 4;
    internal const int FontBoundingBox = 5;
    internal const int BlueValues = 6;
    internal const int OtherBlues = 7;
    internal const int FamilyBlues = 8;
    internal const int FamilyOtherBlues = 9;
    internal const int StandardHorizontalWidth = 10;
    internal const int StandardVerticalWidth = 11;
    internal const int UniqueIdentifier = 13;
    internal const int ExtendedUniqueIdentifier = 14;
    internal const int Charset = 15;
    internal const int Encoding = 16;
    internal const int CharStrings = 17;
    internal const int Private = 18;
    internal const int LocalSubroutines = 19;
    internal const int DefaultWidthX = 20;
    internal const int NominalWidthX = 21;
    internal const int Copyright = 0x0C00;
    internal const int IsFixedPitch = 0x0C01;
    internal const int ItalicAngle = 0x0C02;
    internal const int UnderlinePosition = 0x0C03;
    internal const int UnderlineThickness = 0x0C04;
    internal const int PaintType = 0x0C05;
    internal const int CharstringType = 0x0C06;
    internal const int FontMatrix = 0x0C07;
    internal const int StrokeWidth = 0x0C08;
    internal const int BlueScale = 0x0C09;
    internal const int BlueShift = 0x0C0A;
    internal const int BlueFuzz = 0x0C0B;
    internal const int StemSnapHorizontal = 0x0C0C;
    internal const int StemSnapVertical = 0x0C0D;
    internal const int ForceBold = 0x0C0E;
    internal const int ForceBoldThreshold = 0x0C0F;
    internal const int LenIv = 0x0C10;
    internal const int LanguageGroup = 0x0C11;
    internal const int ExpansionFactor = 0x0C12;
    internal const int InitialRandomSeed = 0x0C13;
    internal const int SyntheticBase = 0x0C14;
    internal const int PostScript = 0x0C15;
    internal const int BaseFontName = 0x0C16;
    internal const int BaseFontBlend = 0x0C17;
    internal const int RegistryOrderingSupplement = 0x0C1E;
    internal const int CidFontVersion = 0x0C1F;
    internal const int CidFontRevision = 0x0C20;
    internal const int CidFontType = 0x0C21;
    internal const int CidCount = 0x0C22;
    internal const int UniqueIdentifierBase = 0x0C23;
    internal const int FontDictionaryArray = 0x0C24;
    internal const int FontDictionarySelect = 0x0C25;
    internal const int FontName = 0x0C26;
    internal const int VariationStore = 24;
    internal const int VariationStoreDataIndex = 22;
    internal const int Blend = 23;

    internal static int CreateEscaped(int secondByte)
    {
        if (secondByte < 0 || secondByte > 255)
        {
            throw new System.ArgumentOutOfRangeException("secondByte");
        }

        return 0x0C00 | secondByte;
    }
}

internal static class CffDictParser
{
    internal static CffDictionary Parse(FontDataSlice data)
    {
        return Parse(data, 48, 21, false);
    }

    internal static CffDictionary ParseCff2(FontDataSlice data)
    {
        return Parse(data, 513, 27, true);
    }

    static CffDictionary Parse(
        FontDataSlice data,
        int maximumOperandCount,
        int maximumOperatorByte,
        bool preserveBlendOperands)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        BigEndianReader reader = new BigEndianReader(data);
        System.Collections.Generic.List<CffDictEntry> entries =
            new System.Collections.Generic.List<CffDictEntry>();
        System.Collections.Generic.List<CffDictOperand> operands =
            new System.Collections.Generic.List<CffDictOperand>();

        while (reader.GetRemainingLength() != 0)
        {
            int firstByte = reader.ReadUInt8();

            if (firstByte <= maximumOperatorByte)
            {
                int operatorCode = firstByte;

                if (firstByte == 12)
                {
                    operatorCode = CffDictOperator.CreateEscaped(
                        reader.ReadUInt8());
                }

                entries.Add(new CffDictEntry(
                    operatorCode,
                    operands.ToArray()));

                if (!preserveBlendOperands ||
                    operatorCode != CffDictOperator.Blend)
                {
                    operands.Clear();
                }

                continue;
            }

            if (firstByte == 28)
            {
                AddOperand(
                    operands,
                    CffDictOperand.CreateInteger(reader.ReadInt16()),
                    maximumOperandCount);
                continue;
            }

            if (firstByte == 29)
            {
                AddOperand(
                    operands,
                    CffDictOperand.CreateInteger(reader.ReadInt32()),
                    maximumOperandCount);
                continue;
            }

            if (firstByte == 30)
            {
                CffRealDecoder decoder = new CffRealDecoder();
                AddOperand(
                    operands,
                    CffDictOperand.CreateReal(decoder.Read(reader)),
                    maximumOperandCount);
                continue;
            }

            if (firstByte >= 32 && firstByte <= 246)
            {
                AddOperand(
                    operands,
                    CffDictOperand.CreateInteger(firstByte - 139),
                    maximumOperandCount);
                continue;
            }

            if (firstByte >= 247 && firstByte <= 250)
            {
                int value = (firstByte - 247) * 256 +
                    reader.ReadUInt8() + 108;
                AddOperand(
                    operands,
                    CffDictOperand.CreateInteger(value),
                    maximumOperandCount);
                continue;
            }

            if (firstByte >= 251 && firstByte <= 254)
            {
                int value = -((firstByte - 251) * 256) -
                    reader.ReadUInt8() - 108;
                AddOperand(
                    operands,
                    CffDictOperand.CreateInteger(value),
                    maximumOperandCount);
                continue;
            }

            throw FontFormatException.Create(
                "A CFF DICT contains a reserved operand or operator byte.");
        }

        if (operands.Count != 0)
        {
            throw FontFormatException.Create(
                "A CFF DICT ends with operands that have no operator.");
        }

        return new CffDictionary(entries.ToArray());
    }

    static void AddOperand(
        System.Collections.Generic.List<CffDictOperand> operands,
        CffDictOperand operand,
        int maximumOperandCount)
    {
        if (operands.Count >= maximumOperandCount)
        {
            throw FontLimitException.Create(
                "A CFF DICT operand stack exceeds its format limit.");
        }

        operands.Add(operand);
    }
}

internal class CffRealDecoder
{
    bool negative;
    bool signSeen;
    bool decimalSeen;
    bool exponentSeen;
    bool exponentNegative;
    bool mantissaDigitSeen;
    bool exponentDigitSeen;
    double mantissa;
    double fractionScale;
    int exponentValue;

    internal CffRealDecoder()
    {
        fractionScale = 0.1;
    }

    internal double Read(BigEndianReader reader)
    {
        bool ended = false;

        while (!ended)
        {
            int value = reader.ReadUInt8();
            int high = value >> 4;
            int low = value & 15;
            ended = ReadNibble(high);

            if (ended)
            {
                if (low != 15)
                {
                    throw FontFormatException.Create(
                        "A CFF DICT real has invalid termination padding.");
                }

                break;
            }

            ended = ReadNibble(low);
        }

        if (!mantissaDigitSeen ||
            (exponentSeen && !exponentDigitSeen))
        {
            throw FontFormatException.Create(
                "A CFF DICT real has invalid numeric syntax.");
        }

        double valueResult = mantissa;

        if (exponentSeen)
        {
            int signedExponent = exponentNegative ?
                -exponentValue : exponentValue;
            valueResult *= System.Math.Pow(10, signedExponent);
        }

        if (negative)
        {
            valueResult = -valueResult;
        }

        if (!double.IsFinite(valueResult))
        {
            throw FontFormatException.Create(
                "A CFF DICT real is outside the supported numeric range.");
        }

        return valueResult;
    }

    bool ReadNibble(int nibble)
    {
        if (nibble <= 9)
        {
            ReadDigit(nibble);
            return false;
        }

        if (nibble == 10)
        {
            if (decimalSeen || exponentSeen)
            {
                throw FontFormatException.Create(
                    "A CFF DICT real has more than one decimal point.");
            }

            decimalSeen = true;
            return false;
        }

        if (nibble == 11 || nibble == 12)
        {
            if (exponentSeen || !mantissaDigitSeen)
            {
                throw FontFormatException.Create(
                    "A CFF DICT real has an invalid exponent.");
            }

            exponentSeen = true;
            exponentNegative = nibble == 12;
            return false;
        }

        if (nibble == 13)
        {
            throw FontFormatException.Create(
                "A CFF DICT real contains the reserved nibble 13.");
        }

        if (nibble == 14)
        {
            if (signSeen || decimalSeen || exponentSeen || mantissaDigitSeen)
            {
                throw FontFormatException.Create(
                    "A CFF DICT real has a misplaced minus sign.");
            }

            negative = true;
            signSeen = true;
            return false;
        }

        return true;
    }

    void ReadDigit(int digit)
    {
        if (exponentSeen)
        {
            exponentDigitSeen = true;

            if (exponentValue < 10000)
            {
                exponentValue = exponentValue * 10 + digit;
            }

            return;
        }

        mantissaDigitSeen = true;

        if (decimalSeen)
        {
            mantissa += digit * fractionScale;
            fractionScale *= 0.1;
        }
        else
        {
            mantissa = mantissa * 10 + digit;
        }
    }
}
