using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GenerateSobolDirectionNumbers;

internal static class Program
{
    const string ExpectedInputSha256 = "68eedd2a4e3b659b9695e7aff0f8ac68718bcf620730fc3d3a8c65df2a067441";
    const int DimensionCount = 21201;
    const int MaximumDegree = 18;
    const int ParameterStride = MaximumDegree + 2;

    static int Main(string[] arguments)
    {
        if (arguments.Length != 2)
        {
            Console.Error.WriteLine("Usage: GenerateSobolDirectionNumbers <joe-kuo-input> <csharp-output>");
            return 1;
        }

        byte[] inputBytes = File.ReadAllBytes(arguments[0]);
        string actualChecksum = Convert.ToHexString(SHA256.HashData(inputBytes)).ToLowerInvariant();
        if (!string.Equals(actualChecksum, ExpectedInputSha256, StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Input SHA-256 mismatch. Expected {ExpectedInputSha256}, received {actualChecksum}.");
            return 1;
        }

        uint[] parameters = ParseParameters(Encoding.ASCII.GetString(inputBytes));
        string output = GenerateSource(parameters);
        string? outputDirectory = Path.GetDirectoryName(Path.GetFullPath(arguments[1]));
        if (outputDirectory != null)
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(arguments[1], output, new UTF8Encoding(false));
        Console.WriteLine($"Generated {arguments[1]} for {DimensionCount} dimensions from SHA-256 {ExpectedInputSha256}.");
        return 0;
    }

    static uint[] ParseParameters(string input)
    {
        string[] lines = input.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        if (lines.Length < 2)
        {
            throw new InvalidDataException("The Joe-Kuo input is empty.");
        }

        string[] header = SplitFields(lines[0]);
        if (header.Length != 4 || header[0] != "d" || header[1] != "s" || header[2] != "a" || header[3] != "m_i")
        {
            throw new InvalidDataException("The Joe-Kuo input header is invalid.");
        }

        int rowCount = DimensionCount - 1;
        uint[] parameters = new uint[rowCount * ParameterStride];
        int row = 0;
        while (row < rowCount)
        {
            int lineIndex = row + 1;
            if (lineIndex >= lines.Length)
            {
                throw new InvalidDataException("The Joe-Kuo input is truncated.");
            }

            string[] fields = SplitFields(lines[lineIndex]);
            int expectedDimension = row + 2;
            if (fields.Length < 4)
            {
                throw new InvalidDataException($"Dimension {expectedDimension} has too few fields.");
            }

            int dimension = ParseInt32(fields[0], expectedDimension, "dimension");
            int degree = ParseInt32(fields[1], expectedDimension, "degree");
            uint coefficient = ParseUInt32(fields[2], expectedDimension, "coefficient");
            if (dimension != expectedDimension)
            {
                throw new InvalidDataException($"Expected dimension {expectedDimension}, received {dimension}.");
            }

            if (degree < 1 || degree > MaximumDegree)
            {
                throw new InvalidDataException($"Dimension {dimension} has an invalid degree.");
            }

            if (fields.Length != degree + 3)
            {
                throw new InvalidDataException($"Dimension {dimension} has an invalid initial-value count.");
            }

            uint coefficientLimit = 1U << (degree - 1);
            if (coefficient >= coefficientLimit)
            {
                throw new InvalidDataException($"Dimension {dimension} has an invalid polynomial coefficient.");
            }

            int offset = row * ParameterStride;
            parameters[offset] = (uint)degree;
            parameters[offset + 1] = coefficient;
            int initialIndex = 0;
            while (initialIndex < degree)
            {
                uint initialValue = ParseUInt32(fields[initialIndex + 3], dimension, "initial value");
                uint initialLimit = 1U << (initialIndex + 1);
                if (initialValue == 0U || initialValue >= initialLimit || (initialValue & 1U) == 0U)
                {
                    throw new InvalidDataException($"Dimension {dimension} has an invalid initial value at position {initialIndex + 1}.");
                }

                parameters[offset + initialIndex + 2] = initialValue;
                initialIndex++;
            }

            row++;
        }

        int remainingLine = rowCount + 1;
        while (remainingLine < lines.Length)
        {
            if (SplitFields(lines[remainingLine]).Length != 0)
            {
                throw new InvalidDataException("The Joe-Kuo input contains unexpected trailing rows.");
            }

            remainingLine++;
        }

        return parameters;
    }

    static string[] SplitFields(string line)
    {
        return line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
    }

    static int ParseInt32(string value, int dimension, string fieldName)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result))
        {
            throw new InvalidDataException($"Dimension {dimension} has an invalid {fieldName}.");
        }

        return result;
    }

    static uint ParseUInt32(string value, int dimension, string fieldName)
    {
        if (!uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out uint result))
        {
            throw new InvalidDataException($"Dimension {dimension} has an invalid {fieldName}.");
        }

        return result;
    }

    static string GenerateSource(uint[] parameters)
    {
        StringBuilder builder = new StringBuilder(parameters.Length * 8);
        builder.Append("// <auto-generated />\n");
        builder.Append("// Generated by tools/GenerateSobolDirectionNumbers from the Joe-Kuo D(6) data.\n");
        builder.Append("namespace random;\n\n");
        builder.Append("internal static class SobolDirectionNumbers\n");
        builder.Append("{\n");
        builder.Append("    const int ParameterStride = 20;\n");
        builder.Append("    static readonly uint[] parameters =\n");
        builder.Append("    [\n");
        int row = 0;
        while (row < DimensionCount - 1)
        {
            builder.Append("        ");
            int column = 0;
            while (column < ParameterStride)
            {
                if (column != 0)
                {
                    builder.Append(' ');
                }

                uint value = parameters[row * ParameterStride + column];
                builder.Append(value.ToString(CultureInfo.InvariantCulture));
                builder.Append("U,");
                column++;
            }

            builder.Append('\n');
            row++;
        }

        builder.Append("    ];\n\n");
        builder.Append("    internal static int DimensionCount()\n");
        builder.Append("    {\n");
        builder.Append("        return 21201;\n");
        builder.Append("    }\n\n");
        builder.Append("    internal static int BitCount()\n");
        builder.Append("    {\n");
        builder.Append("        return 64;\n");
        builder.Append("    }\n\n");
        builder.Append("    internal static void Fill(int dimension, Span<ulong> destination)\n");
        builder.Append("    {\n");
        builder.Append("        if (dimension < 1 || dimension > 21201)\n");
        builder.Append("        {\n");
        builder.Append("            throw new RandomArgumentException(\"dimension\", \"The Sobol dimension must be between one and 21201 inclusive.\");\n");
        builder.Append("        }\n\n");
        builder.Append("        Guard.ExactSpanLength(destination.Length, 64, \"destination\");\n");
        builder.Append("        if (dimension == 1)\n");
        builder.Append("        {\n");
        builder.Append("            int directionIndex = 0;\n");
        builder.Append("            while (directionIndex < 64)\n");
        builder.Append("            {\n");
        builder.Append("                destination[directionIndex] = 1UL << (63 - directionIndex);\n");
        builder.Append("                directionIndex++;\n");
        builder.Append("            }\n\n");
        builder.Append("            return;\n");
        builder.Append("        }\n\n");
        builder.Append("        int parameterOffset = (dimension - 2) * ParameterStride;\n");
        builder.Append("        int degree = (int)parameters[parameterOffset];\n");
        builder.Append("        uint coefficient = parameters[parameterOffset + 1];\n");
        builder.Append("        int index = 0;\n");
        builder.Append("        while (index < degree)\n");
        builder.Append("        {\n");
        builder.Append("            destination[index] = (ulong)parameters[parameterOffset + index + 2] << (63 - index);\n");
        builder.Append("            index++;\n");
        builder.Append("        }\n\n");
        builder.Append("        while (index < 64)\n");
        builder.Append("        {\n");
        builder.Append("            ulong value = destination[index - degree] ^ (destination[index - degree] >> degree);\n");
        builder.Append("            int coefficientIndex = 1;\n");
        builder.Append("            while (coefficientIndex < degree)\n");
        builder.Append("            {\n");
        builder.Append("                int coefficientBit = degree - 1 - coefficientIndex;\n");
        builder.Append("                if (((coefficient >> coefficientBit) & 1U) != 0U)\n");
        builder.Append("                {\n");
        builder.Append("                    value ^= destination[index - coefficientIndex];\n");
        builder.Append("                }\n\n");
        builder.Append("                coefficientIndex++;\n");
        builder.Append("            }\n\n");
        builder.Append("            destination[index] = value;\n");
        builder.Append("            index++;\n");
        builder.Append("        }\n");
        builder.Append("    }\n");
        builder.Append("}\n");
        return builder.ToString();
    }
}
