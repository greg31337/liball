namespace barcode;

internal class Code128EncodedData
{
    internal Code128CodeSet StartCodeSet;
    internal int[] Codewords = new int[0];
    internal int ChecksumValue;
    internal int[] ElementWidths = new int[0];
    internal byte[] Modules = new byte[0];
}

internal static class Code128Encoder
{
    internal const int MaximumPreCheckCodewordCount = 102;
    internal const int MaximumPlannedCodewordCount = MaximumPreCheckCodewordCount - 1;

    internal static Code128EncodedData EncodeBytes(byte[] bytes, Code128Options? options)
    {
        Code128Plan plan = Code128Planner.PlanBytes(bytes, options);
        return AssemblePlan(plan);
    }

    internal static Code128EncodedData EncodeTokens(int[] tokens, Code128Options? options)
    {
        Code128Plan plan = Code128Planner.PlanTokens(tokens, options);
        return AssemblePlan(plan);
    }

    internal static Code128EncodedData AssemblePlan(Code128Plan plan)
    {
        ValidatePlan(plan);
        int startCodeword = Code128Patterns.GetStartCodeword(plan.StartCodeSet);
        int preCheckCount = CheckedMath.Add(plan.Codewords.Length, 1);
        int completeCount = CheckedMath.Add(preCheckCount, 2);
        int[] codewords = new int[completeCount];
        codewords[0] = startCodeword;

        int index = 0;
        while (index < plan.Codewords.Length)
        {
            codewords[index + 1] = plan.Codewords[index];
            index = index + 1;
        }

        int checksumValue = Code128Modulo103.CalculateCheckCodewordForPrefix(codewords, preCheckCount);
        codewords[preCheckCount] = checksumValue;
        codewords[preCheckCount + 1] = Code128Patterns.StopCodeword;

        int ordinaryElementCount = CheckedMath.Multiply(completeCount - 1, Code128Patterns.OrdinaryElementCount);
        int elementCount = CheckedMath.Add(ordinaryElementCount, Code128Patterns.StopElementCount);
        int ordinaryModuleCount = CheckedMath.Multiply(completeCount - 1, Code128Patterns.OrdinaryModuleCount);
        int moduleCount = CheckedMath.Add(ordinaryModuleCount, Code128Patterns.StopModuleCount);
        int[] elementWidths = new int[elementCount];
        byte[] modules = new byte[moduleCount];
        ExpandPatterns(codewords, elementWidths, modules);

        Code128EncodedData encoded = new Code128EncodedData();
        encoded.StartCodeSet = plan.StartCodeSet;
        encoded.Codewords = codewords;
        encoded.ChecksumValue = checksumValue;
        encoded.ElementWidths = elementWidths;
        encoded.Modules = modules;
        return encoded;
    }

    static void ValidatePlan(Code128Plan plan)
    {
        if (plan == null)
        {
            throw new BarcodeValidationException("Code 128 plan must not be null.");
        }

        Code128Patterns.GetStartCodeword(plan.StartCodeSet);
        if (plan.Codewords == null)
        {
            throw new BarcodeValidationException("Code 128 planned codewords must not be null.");
        }

        if (plan.Codewords.Length == 0)
        {
            throw new BarcodeValidationException("Code 128 plan must contain at least one data or function codeword.");
        }

        if (plan.Codewords.Length > MaximumPlannedCodewordCount)
        {
            throw new BarcodeEncodingException("Code 128 plan exceeds the maximum symbol-character capacity.");
        }

        int index = 0;
        while (index < plan.Codewords.Length)
        {
            Code128Modulo103.ValidateDataCodeword(plan.Codewords[index]);
            index = index + 1;
        }
    }

    static void ExpandPatterns(int[] codewords, int[] elementWidths, byte[] modules)
    {
        int elementOffset = 0;
        int moduleOffset = 0;
        int codewordIndex = 0;
        while (codewordIndex < codewords.Length)
        {
            int[] widths = Code128Patterns.GetPatternWidths(codewords[codewordIndex]);
            bool dark = true;
            int widthIndex = 0;
            while (widthIndex < widths.Length)
            {
                int width = widths[widthIndex];
                elementWidths[elementOffset] = width;
                elementOffset = elementOffset + 1;
                int widthOffset = 0;
                while (widthOffset < width)
                {
                    if (dark)
                    {
                        modules[moduleOffset] = 1;
                    }

                    moduleOffset = moduleOffset + 1;
                    widthOffset = widthOffset + 1;
                }

                dark = !dark;
                widthIndex = widthIndex + 1;
            }

            codewordIndex = codewordIndex + 1;
        }
    }
}
