namespace barcode;

internal class DataMatrixMessagePlan
{
    internal int[] ControlCodewords = new int[0];
    internal DataMatrixEncodingPlan DataPlan = new DataMatrixEncodingPlan();
    internal int[] Codewords = new int[0];
}

internal static class DataMatrixControlCodewordEncoder
{
    internal const int MaximumEciAssignmentNumber = 999999;
    internal const int MaximumStructuredAppendCount = 16;
    internal const int MaximumStructuredAppendFileId = 64516;

    internal static DataMatrixMessagePlan CreateMessagePlan(
        byte[] data,
        DataMatrixOptions? options,
        bool isGs1)
    {
        if (data == null)
        {
            throw new BarcodeEncodingException("Data Matrix message data must not be null.");
        }

        DataMatrixOptions settings = options == null
            ? new DataMatrixOptions()
            : options;
        ValidateOptions(settings, isGs1);
        int[] controls = CreatePrefix(settings, isGs1);
        DataMatrixEncodingPlan dataPlan = isGs1
            ? DataMatrixMinimalEncoder.PlanGs1ElementString(
                data,
                settings.EncodingMode,
                controls.Length + 1)
            : DataMatrixMinimalEncoder.PlanAtCodewordPosition(
                data,
                settings.EncodingMode,
                controls.Length + 1);
        int totalCount = CheckedMath.Add(controls.Length, dataPlan.Codewords.Length);
        if (totalCount > DataMatrixMinimalEncoder.MaximumPlannedCodewordCount)
        {
            throw new BarcodeEncodingException("Data Matrix controls and data exceed the maximum standard symbol capacity.");
        }

        int[] codewords = new int[totalCount];
        CopyIntegers(controls, codewords, 0);
        CopyIntegers(dataPlan.Codewords, codewords, controls.Length);
        DataMatrixMessagePlan plan = new DataMatrixMessagePlan();
        plan.ControlCodewords = controls;
        plan.DataPlan = dataPlan;
        plan.Codewords = codewords;
        return plan;
    }

    internal static int[] CreatePrefix(
        DataMatrixOptions options,
        bool isGs1)
    {
        if (options == null)
        {
            throw new BarcodeValidationException("Data Matrix control options must not be null.");
        }

        ValidateOptions(options, isGs1);
        int[] buffer = new int[10];
        int count = 0;
        if (options.StructuredAppend != null)
        {
            int[] append = EncodeStructuredAppend(options.StructuredAppend);
            count = AppendCodewords(buffer, count, append);
        }

        if (options.MacroType != 0)
        {
            int[] macro = EncodeMacro(options.MacroType);
            count = AppendCodewords(buffer, count, macro);
        }

        if (isGs1)
        {
            int[] fnc1 = EncodeFnc1();
            count = AppendCodewords(buffer, count, fnc1);
        }

        if (options.ReaderProgramming)
        {
            int[] readerProgramming = EncodeReaderProgramming();
            count = AppendCodewords(buffer, count, readerProgramming);
        }

        if (options.EciAssignmentNumber > 0)
        {
            int[] eci = EncodeEciAssignment(options.EciAssignmentNumber);
            count = AppendCodewords(buffer, count, eci);
        }

        return CopyIntegerPrefix(buffer, count);
    }

    internal static int[] EncodeFnc1()
    {
        return new int[] { DataMatrixHighLevelEncoder.Fnc1Codeword };
    }

    internal static int[] EncodeReaderProgramming()
    {
        return new int[] { DataMatrixHighLevelEncoder.ReaderProgrammingCodeword };
    }

    internal static int[] EncodeMacro(int macroType)
    {
        if (macroType == 5)
        {
            return new int[] { DataMatrixHighLevelEncoder.Macro05Codeword };
        }

        if (macroType == 6)
        {
            return new int[] { DataMatrixHighLevelEncoder.Macro06Codeword };
        }

        throw new BarcodeEncodingException("Data Matrix Macro type must be 5 or 6.");
    }

    internal static int[] EncodeEciAssignment(int assignmentNumber)
    {
        if (assignmentNumber < 0 || assignmentNumber > MaximumEciAssignmentNumber)
        {
            throw new BarcodeEncodingException("Data Matrix ECI assignment number must be between 0 and 999999.");
        }

        if (assignmentNumber <= 126)
        {
            return new int[]
            {
                DataMatrixHighLevelEncoder.EciCodeword,
                assignmentNumber + 1
            };
        }

        if (assignmentNumber <= 16382)
        {
            int adjusted = assignmentNumber - 127;
            return new int[]
            {
                DataMatrixHighLevelEncoder.EciCodeword,
                (adjusted / 254) + 128,
                (adjusted % 254) + 1
            };
        }

        int largeAdjusted = assignmentNumber - 16383;
        return new int[]
        {
            DataMatrixHighLevelEncoder.EciCodeword,
            (largeAdjusted / 64516) + 192,
            ((largeAdjusted / 254) % 254) + 1,
            (largeAdjusted % 254) + 1
        };
    }

    internal static int[] EncodeStructuredAppend(StructuredAppendInfo append)
    {
        ValidateStructuredAppend(append);
        int[] fileCodewords = EncodeStructuredAppendFileIdentifier(append.FileIdentifier);
        int sequenceIndicator = (append.Index << 4) | (17 - append.Count);
        return new int[]
        {
            DataMatrixHighLevelEncoder.StructuredAppendCodeword,
            sequenceIndicator,
            fileCodewords[0],
            fileCodewords[1]
        };
    }

    internal static int[] EncodeStructuredAppendFileIdentifier(string? fileIdentifier)
    {
        if (fileIdentifier == null || fileIdentifier.Length == 0)
        {
            return new int[] { 1, 1 };
        }

        if (fileIdentifier.Length > 6)
        {
            throw new BarcodeEncodingException("Data Matrix structured-append file identifier must contain at most six decimal digits.");
        }

        if (fileIdentifier.Length == 6)
        {
            int first = ParseDecimalRange(fileIdentifier, 0, 3);
            int second = ParseDecimalRange(fileIdentifier, 3, 3);
            if (first < 1 || first > 254 || second < 1 || second > 254)
            {
                throw new BarcodeEncodingException("Data Matrix structured-append file identifier components must each be between 001 and 254.");
            }

            return new int[] { first, second };
        }

        int value = ParseDecimalRange(fileIdentifier, 0, fileIdentifier.Length);
        if (value < 1 || value > MaximumStructuredAppendFileId)
        {
            throw new BarcodeEncodingException("Data Matrix structured-append file identifier must be between 1 and 64516.");
        }

        return new int[]
        {
            1 + ((value - 1) / 254),
            1 + ((value - 1) % 254)
        };
    }

    static void ValidateOptions(DataMatrixOptions options, bool isGs1)
    {
        if (options.EciAssignmentNumber < 0
            || options.EciAssignmentNumber > MaximumEciAssignmentNumber)
        {
            throw new BarcodeEncodingException("Data Matrix ECI assignment number must be zero or between 1 and 999999.");
        }

        if (options.MacroType != 0
            && options.MacroType != 5
            && options.MacroType != 6)
        {
            throw new BarcodeEncodingException("Data Matrix Macro type must be zero, 5, or 6.");
        }

        if (options.StructuredAppend != null)
        {
            ValidateStructuredAppend(options.StructuredAppend);
        }

        if (options.ReaderProgramming && isGs1)
        {
            throw new BarcodeEncodingException("Data Matrix reader programming cannot be combined with GS1 FNC1.");
        }

        if (options.ReaderProgramming && options.StructuredAppend != null)
        {
            throw new BarcodeEncodingException("Data Matrix reader programming cannot be combined with structured append.");
        }

        if (options.ReaderProgramming && options.MacroType != 0)
        {
            throw new BarcodeEncodingException("Data Matrix reader programming cannot be combined with Macro 05 or Macro 06.");
        }

        if (options.MacroType != 0 && isGs1)
        {
            throw new BarcodeEncodingException("Data Matrix Macro 05 and Macro 06 cannot be combined with GS1 FNC1.");
        }

        if (options.MacroType != 0 && options.StructuredAppend != null)
        {
            throw new BarcodeEncodingException("Data Matrix Macro 05 and Macro 06 cannot be combined with structured append.");
        }
    }

    static void ValidateStructuredAppend(StructuredAppendInfo append)
    {
        if (append == null)
        {
            throw new BarcodeEncodingException("Data Matrix structured-append information must not be null.");
        }

        if (append.Count < 2 || append.Count > MaximumStructuredAppendCount)
        {
            throw new BarcodeEncodingException("Data Matrix structured-append count must be between 2 and 16.");
        }

        if (append.Index < 0 || append.Index >= append.Count)
        {
            throw new BarcodeEncodingException("Data Matrix structured-append index must be zero-based and less than the segment count.");
        }

        if (append.Format != BarcodeFormat.Automatic
            && append.Format != BarcodeFormat.DataMatrix
            && append.Format != BarcodeFormat.Gs1DataMatrix)
        {
            throw new BarcodeEncodingException("Structured-append metadata is not for Data Matrix.");
        }

        EncodeStructuredAppendFileIdentifier(append.FileIdentifier);
    }

    static int ParseDecimalRange(string value, int start, int count)
    {
        int result = 0;
        int index = 0;
        while (index < count)
        {
            int character = value[start + index];
            if (character < 48 || character > 57)
            {
                throw new BarcodeEncodingException("Data Matrix structured-append file identifier must contain decimal digits only.");
            }

            result = CheckedMath.Add(CheckedMath.Multiply(result, 10), character - 48);
            index = index + 1;
        }

        return result;
    }

    static int AppendCodewords(int[] target, int targetCount, int[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            target[targetCount] = values[index];
            targetCount = targetCount + 1;
            index = index + 1;
        }

        return targetCount;
    }

    static int[] CopyIntegerPrefix(int[] source, int count)
    {
        int[] copy = new int[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static void CopyIntegers(int[] source, int[] target, int targetStart)
    {
        int index = 0;
        while (index < source.Length)
        {
            target[targetStart + index] = source[index];
            index = index + 1;
        }
    }
}
