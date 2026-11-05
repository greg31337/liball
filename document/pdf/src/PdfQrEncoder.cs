using System;
using System.Collections.Generic;
using System.Text;

namespace pdf;

internal class PdfQrMatrix
{
    internal int Size;
    internal bool[,] Modules;
    internal bool[,] FunctionModules;

    internal PdfQrMatrix(int size)
    {
        Size = size;
        Modules = new bool[size, size];
        FunctionModules = new bool[size, size];
    }
}

internal static class PdfQrEncoder
{
    internal static readonly int[][] EccCodewords = new int[][]
    {
        new int[] { 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        new int[] { 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 },
        new int[] { 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        new int[] { 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 }
    };
    internal static readonly int[][] ErrorCorrectionBlocks = new int[][]
    {
        new int[] { 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },
        new int[] { 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },
        new int[] { 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },
        new int[] { 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 }
    };

    internal static PdfQrMatrix Encode(string text, PdfQrErrorCorrection errorCorrection)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length == 0)
        {
            throw new ArgumentException("QR Code text cannot be empty.", "text");
        }

        if (text.Length > 10000)
        {
            throw new ArgumentException("QR Code text exceeds the supported length.", "text");
        }

        int level = ErrorCorrectionIndex(errorCorrection);
        byte[] data = Encoding.UTF8.GetBytes(text);
        bool includeEci = ContainsNonAsciiByte(data);
        int version = ChooseVersion(data.Length, level, includeEci);
        byte[] dataCodewords = CreateDataCodewords(data, version, level);
        byte[] allCodewords = CreateAllCodewords(dataCodewords, version, level);
        int size = version * 4 + 17;
        PdfQrMatrix matrix = new PdfQrMatrix(size);
        DrawFunctionPatterns(matrix, version, level);
        PlaceCodewords(matrix, allCodewords);

        int bestMask = 0;
        int bestPenalty = int.MaxValue;
        for (int mask = 0; mask < 8; mask++)
        {
            ApplyMask(matrix, mask);
            DrawFormatBits(matrix, level, mask);
            int penalty = CalculatePenalty(matrix);
            if (penalty < bestPenalty)
            {
                bestPenalty = penalty;
                bestMask = mask;
            }

            ApplyMask(matrix, mask);
        }

        ApplyMask(matrix, bestMask);
        DrawFormatBits(matrix, level, bestMask);
        return matrix;
    }

    internal static int ErrorCorrectionIndex(PdfQrErrorCorrection errorCorrection)
    {
        int index = (int)errorCorrection;
        if (index < 0 || index > 3)
        {
            throw new ArgumentException("QR Code error-correction level is unsupported.", "errorCorrection");
        }

        return index;
    }

    internal static int ChooseVersion(int byteCount, int level, bool includeEci)
    {
        for (int version = 1; version <= 40; version++)
        {
            int countBits = version <= 9 ? 8 : 16;
            int requiredBits = 4 + countBits + byteCount * 8 + (includeEci ? 12 : 0);
            if (requiredBits <= GetDataCodewordCount(version, level) * 8)
            {
                return version;
            }
        }

        throw new ArgumentException("QR Code text is too long for the supported version range.", "text");
    }

    internal static int GetDataCodewordCount(int version, int level)
    {
        int rawCodewords = GetRawDataModuleCount(version) / 8;
        return rawCodewords - EccCodewords[level][version - 1] * ErrorCorrectionBlocks[level][version - 1];
    }

    internal static int GetRawDataModuleCount(int version)
    {
        int result = (16 * version + 128) * version + 64;
        if (version >= 2)
        {
            int alignmentCount = version / 7 + 2;
            result -= (25 * alignmentCount - 10) * alignmentCount - 55;
        }

        if (version >= 7)
        {
            result -= 36;
        }

        return result;
    }

    internal static byte[] CreateDataCodewords(byte[] data, int version, int level)
    {
        int capacity = GetDataCodewordCount(version, level) * 8;
        List<bool> bits = new List<bool>();
        if (ContainsNonAsciiByte(data))
        {
            AppendBits(bits, 7, 4);
            AppendBits(bits, 26, 8);
        }

        AppendBits(bits, 4, 4);
        AppendBits(bits, data.Length, version <= 9 ? 8 : 16);
        for (int index = 0; index < data.Length; index++)
        {
            AppendBits(bits, data[index], 8);
        }

        int terminator = Math.Min(4, capacity - bits.Count);
        AppendBits(bits, 0, terminator);
        while (bits.Count % 8 != 0)
        {
            bits.Add(false);
        }

        bool firstPad = true;
        while (bits.Count < capacity)
        {
            AppendBits(bits, firstPad ? 236 : 17, 8);
            firstPad = !firstPad;
        }

        byte[] result = new byte[bits.Count / 8];
        for (int index = 0; index < bits.Count; index++)
        {
            if (bits[index])
            {
                result[index >> 3] = (byte)(result[index >> 3] | 1 << (7 - (index & 7)));
            }
        }

        return result;
    }

    internal static bool ContainsNonAsciiByte(byte[] data)
    {
        for (int index = 0; index < data.Length; index++)
        {
            if (data[index] >= 128)
            {
                return true;
            }
        }

        return false;
    }

    internal static void AppendBits(List<bool> bits, int value, int length)
    {
        for (int index = length - 1; index >= 0; index--)
        {
            bits.Add(((value >> index) & 1) != 0);
        }
    }

    internal static byte[] CreateAllCodewords(byte[] data, int version, int level)
    {
        int blockCount = ErrorCorrectionBlocks[level][version - 1];
        int eccLength = EccCodewords[level][version - 1];
        int rawCodewords = GetRawDataModuleCount(version) / 8;
        int shortBlockCount = blockCount - rawCodewords % blockCount;
        int shortBlockLength = rawCodewords / blockCount;
        int shortDataLength = shortBlockLength - eccLength;
        byte[] divisor = CreateReedSolomonDivisor(eccLength);
        byte[][] blocks = new byte[blockCount][];
        int dataPosition = 0;
        for (int block = 0; block < blockCount; block++)
        {
            int dataLength = shortDataLength + (block < shortBlockCount ? 0 : 1);
            byte[] blockData = new byte[dataLength];
            Array.Copy(data, dataPosition, blockData, 0, dataLength);
            dataPosition += dataLength;
            byte[] remainder = CalculateReedSolomonRemainder(blockData, divisor);
            int padding = block < shortBlockCount ? 1 : 0;
            byte[] combined = new byte[dataLength + padding + remainder.Length];
            Array.Copy(blockData, 0, combined, 0, blockData.Length);
            Array.Copy(remainder, 0, combined, blockData.Length + padding, remainder.Length);
            blocks[block] = combined;
        }

        byte[] result = new byte[rawCodewords];
        int resultPosition = 0;
        for (int index = 0; index < blocks[0].Length; index++)
        {
            for (int block = 0; block < blocks.Length; block++)
            {
                if (index != shortDataLength || block >= shortBlockCount)
                {
                    result[resultPosition] = blocks[block][index];
                    resultPosition++;
                }
            }
        }

        return result;
    }

    internal static byte[] CreateReedSolomonDivisor(int degree)
    {
        byte[] result = new byte[degree];
        result[degree - 1] = 1;
        byte root = 1;
        for (int index = 0; index < degree; index++)
        {
            for (int coefficient = 0; coefficient < result.Length; coefficient++)
            {
                result[coefficient] = MultiplyFiniteField(result[coefficient], root);
                if (coefficient + 1 < result.Length)
                {
                    result[coefficient] = (byte)(result[coefficient] ^ result[coefficient + 1]);
                }
            }

            root = MultiplyFiniteField(root, 2);
        }

        return result;
    }

    internal static byte[] CalculateReedSolomonRemainder(byte[] data, byte[] divisor)
    {
        byte[] result = new byte[divisor.Length];
        for (int index = 0; index < data.Length; index++)
        {
            byte factor = (byte)(data[index] ^ result[0]);
            for (int coefficient = 0; coefficient + 1 < result.Length; coefficient++)
            {
                result[coefficient] = result[coefficient + 1];
            }

            result[result.Length - 1] = 0;
            for (int coefficient = 0; coefficient < result.Length; coefficient++)
            {
                result[coefficient] = (byte)(result[coefficient] ^ MultiplyFiniteField(divisor[coefficient], factor));
            }
        }

        return result;
    }

    internal static byte MultiplyFiniteField(byte first, byte second)
    {
        int x = first;
        int y = second;
        int result = 0;
        while (y != 0)
        {
            if ((y & 1) != 0)
            {
                result ^= x;
            }

            y >>= 1;
            x <<= 1;
            if ((x & 256) != 0)
            {
                x ^= 285;
            }
        }

        return (byte)result;
    }

    internal static void DrawFunctionPatterns(PdfQrMatrix matrix, int version, int level)
    {
        for (int index = 0; index < matrix.Size; index++)
        {
            SetFunctionModule(matrix, 6, index, index % 2 == 0);
            SetFunctionModule(matrix, index, 6, index % 2 == 0);
        }

        DrawFinderPattern(matrix, 3, 3);
        DrawFinderPattern(matrix, matrix.Size - 4, 3);
        DrawFinderPattern(matrix, 3, matrix.Size - 4);
        int[] alignments = GetAlignmentPositions(version);
        for (int first = 0; first < alignments.Length; first++)
        {
            for (int second = 0; second < alignments.Length; second++)
            {
                int x = alignments[first];
                int y = alignments[second];
                int last = alignments.Length - 1;
                bool finderCorner = first == 0 && second == 0 || first == 0 && second == last || first == last && second == 0;
                if (!finderCorner)
                {
                    DrawAlignmentPattern(matrix, x, y);
                }
            }
        }

        DrawFormatBits(matrix, level, 0);
        DrawVersionBits(matrix, version);
    }

    internal static void DrawFinderPattern(PdfQrMatrix matrix, int centerX, int centerY)
    {
        for (int yOffset = -4; yOffset <= 4; yOffset++)
        {
            for (int xOffset = -4; xOffset <= 4; xOffset++)
            {
                int x = centerX + xOffset;
                int y = centerY + yOffset;
                if (x >= 0 && x < matrix.Size && y >= 0 && y < matrix.Size)
                {
                    int distance = Math.Max(Math.Abs(xOffset), Math.Abs(yOffset));
                    SetFunctionModule(matrix, x, y, distance != 2 && distance != 4);
                }
            }
        }
    }

    internal static int[] GetAlignmentPositions(int version)
    {
        if (version == 1)
        {
            return new int[0];
        }

        int count = version / 7 + 2;
        int step = (version * 8 + count * 3 + 5) / (count * 4 - 4) * 2;
        int[] result = new int[count];
        result[0] = 6;
        int position = version * 4 + 10;
        for (int index = count - 1; index >= 1; index--)
        {
            result[index] = position;
            position -= step;
        }

        return result;
    }

    internal static void DrawAlignmentPattern(PdfQrMatrix matrix, int centerX, int centerY)
    {
        for (int yOffset = -2; yOffset <= 2; yOffset++)
        {
            for (int xOffset = -2; xOffset <= 2; xOffset++)
            {
                int distance = Math.Max(Math.Abs(xOffset), Math.Abs(yOffset));
                SetFunctionModule(matrix, centerX + xOffset, centerY + yOffset, distance != 1);
            }
        }
    }

    internal static void DrawFormatBits(PdfQrMatrix matrix, int level, int mask)
    {
        int[] formatValues = new int[] { 1, 0, 3, 2 };
        int data = formatValues[level] << 3 | mask;
        int remainder = data;
        for (int index = 0; index < 10; index++)
        {
            remainder = remainder << 1 ^ (remainder >> 9) * 1335;
        }

        int bits = (data << 10 | remainder) ^ 21522;
        for (int index = 0; index <= 5; index++)
        {
            SetFunctionModule(matrix, 8, index, GetBit(bits, index));
        }

        SetFunctionModule(matrix, 8, 7, GetBit(bits, 6));
        SetFunctionModule(matrix, 8, 8, GetBit(bits, 7));
        SetFunctionModule(matrix, 7, 8, GetBit(bits, 8));
        for (int index = 9; index < 15; index++)
        {
            SetFunctionModule(matrix, 14 - index, 8, GetBit(bits, index));
        }

        for (int index = 0; index < 8; index++)
        {
            SetFunctionModule(matrix, matrix.Size - 1 - index, 8, GetBit(bits, index));
        }

        for (int index = 8; index < 15; index++)
        {
            SetFunctionModule(matrix, 8, matrix.Size - 15 + index, GetBit(bits, index));
        }

        SetFunctionModule(matrix, 8, matrix.Size - 8, true);
    }

    internal static void DrawVersionBits(PdfQrMatrix matrix, int version)
    {
        if (version < 7)
        {
            return;
        }

        int remainder = version;
        for (int index = 0; index < 12; index++)
        {
            remainder = remainder << 1 ^ (remainder >> 11) * 7973;
        }

        int bits = version << 12 | remainder;
        for (int index = 0; index < 18; index++)
        {
            bool bit = GetBit(bits, index);
            int first = matrix.Size - 11 + index % 3;
            int second = index / 3;
            SetFunctionModule(matrix, first, second, bit);
            SetFunctionModule(matrix, second, first, bit);
        }
    }

    internal static bool GetBit(int value, int index)
    {
        return ((value >> index) & 1) != 0;
    }

    internal static void SetFunctionModule(PdfQrMatrix matrix, int x, int y, bool dark)
    {
        matrix.Modules[x, y] = dark;
        matrix.FunctionModules[x, y] = true;
    }

    internal static void PlaceCodewords(PdfQrMatrix matrix, byte[] data)
    {
        int bitIndex = 0;
        for (int right = matrix.Size - 1; right >= 1; right -= 2)
        {
            if (right == 6)
            {
                right = 5;
            }

            for (int vertical = 0; vertical < matrix.Size; vertical++)
            {
                int y = ((right + 1) & 2) == 0 ? matrix.Size - 1 - vertical : vertical;
                for (int column = 0; column < 2; column++)
                {
                    int x = right - column;
                    if (!matrix.FunctionModules[x, y] && bitIndex < data.Length * 8)
                    {
                        matrix.Modules[x, y] = GetBit(data[bitIndex >> 3], 7 - (bitIndex & 7));
                        bitIndex++;
                    }
                }
            }
        }
    }

    internal static void ApplyMask(PdfQrMatrix matrix, int mask)
    {
        for (int y = 0; y < matrix.Size; y++)
        {
            for (int x = 0; x < matrix.Size; x++)
            {
                if (!matrix.FunctionModules[x, y] && MaskApplies(mask, x, y))
                {
                    matrix.Modules[x, y] = !matrix.Modules[x, y];
                }
            }
        }
    }

    internal static bool MaskApplies(int mask, int x, int y)
    {
        if (mask == 0)
        {
            return (x + y) % 2 == 0;
        }

        if (mask == 1)
        {
            return y % 2 == 0;
        }

        if (mask == 2)
        {
            return x % 3 == 0;
        }

        if (mask == 3)
        {
            return (x + y) % 3 == 0;
        }

        if (mask == 4)
        {
            return (x / 3 + y / 2) % 2 == 0;
        }

        int product = x * y;
        if (mask == 5)
        {
            return product % 2 + product % 3 == 0;
        }

        if (mask == 6)
        {
            return (product % 2 + product % 3) % 2 == 0;
        }

        if (mask == 7)
        {
            return ((x + y) % 2 + product % 3) % 2 == 0;
        }

        throw new ArgumentException("QR Code mask is unsupported.", "mask");
    }

    internal static int CalculatePenalty(PdfQrMatrix matrix)
    {
        int penalty = 0;
        for (int y = 0; y < matrix.Size; y++)
        {
            bool runColor = false;
            int runLength = 0;
            int[] history = new int[7];
            for (int x = 0; x < matrix.Size; x++)
            {
                if (matrix.Modules[x, y] == runColor)
                {
                    runLength++;
                    if (runLength == 5)
                    {
                        penalty += 3;
                    }
                    else if (runLength > 5)
                    {
                        penalty++;
                    }
                }
                else
                {
                    AddFinderRunHistory(runLength, history, matrix.Size);
                    if (!runColor)
                    {
                        penalty += CountFinderRunPatterns(history) * 40;
                    }

                    runColor = matrix.Modules[x, y];
                    runLength = 1;
                }
            }

            penalty += TerminateFinderRun(runColor, runLength, history, matrix.Size) * 40;
        }

        for (int x = 0; x < matrix.Size; x++)
        {
            bool runColor = false;
            int runLength = 0;
            int[] history = new int[7];
            for (int y = 0; y < matrix.Size; y++)
            {
                if (matrix.Modules[x, y] == runColor)
                {
                    runLength++;
                    if (runLength == 5)
                    {
                        penalty += 3;
                    }
                    else if (runLength > 5)
                    {
                        penalty++;
                    }
                }
                else
                {
                    AddFinderRunHistory(runLength, history, matrix.Size);
                    if (!runColor)
                    {
                        penalty += CountFinderRunPatterns(history) * 40;
                    }

                    runColor = matrix.Modules[x, y];
                    runLength = 1;
                }
            }

            penalty += TerminateFinderRun(runColor, runLength, history, matrix.Size) * 40;
        }

        for (int y = 0; y + 1 < matrix.Size; y++)
        {
            for (int x = 0; x + 1 < matrix.Size; x++)
            {
                bool color = matrix.Modules[x, y];
                if (color == matrix.Modules[x + 1, y] && color == matrix.Modules[x, y + 1] && color == matrix.Modules[x + 1, y + 1])
                {
                    penalty += 3;
                }
            }
        }

        int dark = 0;
        for (int y = 0; y < matrix.Size; y++)
        {
            for (int x = 0; x < matrix.Size; x++)
            {
                if (matrix.Modules[x, y])
                {
                    dark++;
                }
            }
        }

        int total = matrix.Size * matrix.Size;
        int balance = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
        penalty += balance * 10;
        return penalty;
    }

    internal static int CountFinderRunPatterns(int[] history)
    {
        int unit = history[1];
        bool core = unit > 0 && history[2] == unit && history[3] == unit * 3 && history[4] == unit && history[5] == unit;
        int count = 0;
        if (core && history[0] >= unit * 4 && history[6] >= unit)
        {
            count++;
        }

        if (core && history[6] >= unit * 4 && history[0] >= unit)
        {
            count++;
        }

        return count;
    }

    internal static int TerminateFinderRun(bool runColor, int runLength, int[] history, int size)
    {
        if (runColor)
        {
            AddFinderRunHistory(runLength, history, size);
            runLength = 0;
        }

        runLength += size;
        AddFinderRunHistory(runLength, history, size);
        return CountFinderRunPatterns(history);
    }

    internal static void AddFinderRunHistory(int runLength, int[] history, int size)
    {
        if (history[0] == 0)
        {
            runLength += size;
        }

        for (int index = history.Length - 1; index >= 1; index--)
        {
            history[index] = history[index - 1];
        }

        history[0] = runLength;
    }
}
