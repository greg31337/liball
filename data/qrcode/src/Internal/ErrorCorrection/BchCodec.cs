namespace qrcode;

internal class BchCodec
{
    internal static int CreateCode(
        int data,
        int dataBitCount,
        int parityBitCount,
        int generator,
        int xorMask)
    {
        ValidateBitLayout(dataBitCount, parityBitCount);
        int totalBitCount = dataBitCount + parityBitCount;
        int maximumData = (1 << dataBitCount) - 1;
        int maximumCode = (1 << totalBitCount) - 1;
        if (data < 0 || data > maximumData)
        {
            throw new QrInputException("The BCH data does not fit its bit width.", "data");
        }

        if (generator < (1 << parityBitCount)
            || generator > (1 << (parityBitCount + 1)) - 1)
        {
            throw new QrInputException(
                "The BCH generator does not fit the parity width.",
                "generator");
        }

        if (xorMask < 0 || xorMask > maximumCode)
        {
            throw new QrInputException("The BCH XOR mask does not fit the code width.", "xorMask");
        }

        int remainder = data;
        for (int index = dataBitCount - 1; index >= parityBitCount; index--)
        {
            if ((remainder & (1 << index)) != 0)
            {
                remainder ^= generator << (index - parityBitCount);
            }
        }

        for (int index = 0; index < parityBitCount; index++)
        {
            int topBit = remainder >> (parityBitCount - 1);
            remainder <<= 1;
            if (topBit != 0)
            {
                remainder ^= generator;
            }
        }

        return (data << parityBitCount | remainder) ^ xorMask;
    }

    internal static int CountDifferences(int left, int right, int bitCount)
    {
        if (bitCount <= 0 || bitCount > 30)
        {
            throw new QrInputException("The bit count must be between one and 30.", "bitCount");
        }

        int maximumCode = (1 << bitCount) - 1;
        if (left < 0 || left > maximumCode)
        {
            throw new QrInputException("The left code does not fit the bit width.", "left");
        }

        if (right < 0 || right > maximumCode)
        {
            throw new QrInputException("The right code does not fit the bit width.", "right");
        }

        int differences = left ^ right;
        int count = 0;
        while (differences != 0)
        {
            count += differences & 1;
            differences >>= 1;
        }

        return count;
    }

    internal static BchCorrectionResult CorrectNearestCode(
        int receivedCode,
        int bitCount,
        int[] validCodes,
        int[] validData,
        int correctionLimit)
    {
        ValidateCorrectionInputs(
            receivedCode,
            bitCount,
            validCodes,
            validData,
            correctionLimit);
        int bestIndex = -1;
        int bestDistance = bitCount + 1;
        bool tied = false;
        for (int index = 0; index < validCodes.Length; index++)
        {
            int distance = CountDifferences(receivedCode, validCodes[index], bitCount);
            if (distance < bestDistance)
            {
                bestIndex = index;
                bestDistance = distance;
                tied = false;
            }
            else if (distance == bestDistance
                && bestIndex >= 0
                && validData[index] != validData[bestIndex])
            {
                tied = true;
            }
        }

        if (bestIndex < 0 || bestDistance > correctionLimit || tied)
        {
            return CreateFailure(bestDistance, bestDistance, -1);
        }

        return new BchCorrectionResult
        {
            Success = true,
            Data = validData[bestIndex],
            Code = validCodes[bestIndex],
            Distance = bestDistance,
            FirstDistance = bestDistance,
            SecondDistance = -1,
            SourceCopy = 0,
            CopiesAgree = true
        };
    }

    internal static BchCorrectionResult CorrectRedundantCodes(
        int firstReceivedCode,
        int secondReceivedCode,
        int bitCount,
        int[] firstValidCodes,
        int[] secondValidCodes,
        int[] validData,
        int correctionLimit)
    {
        BchCorrectionResult first = CorrectNearestCode(
            firstReceivedCode,
            bitCount,
            firstValidCodes,
            validData,
            correctionLimit);
        BchCorrectionResult second = CorrectNearestCode(
            secondReceivedCode,
            bitCount,
            secondValidCodes,
            validData,
            correctionLimit);
        if (first.Success && second.Success)
        {
            if (first.Data == second.Data)
            {
                if (first.Distance <= second.Distance)
                {
                    return CreateRedundantSuccess(first, first.Distance, second.Distance, 0, true);
                }

                return CreateRedundantSuccess(second, first.Distance, second.Distance, 1, true);
            }

            if (first.Distance < second.Distance)
            {
                return CreateRedundantSuccess(first, first.Distance, second.Distance, 0, false);
            }

            if (second.Distance < first.Distance)
            {
                return CreateRedundantSuccess(second, first.Distance, second.Distance, 1, false);
            }

            return CreateFailure(first.Distance, first.Distance, second.Distance);
        }

        if (first.Success)
        {
            return CreateRedundantSuccess(first, first.Distance, second.Distance, 0, false);
        }

        if (second.Success)
        {
            return CreateRedundantSuccess(second, first.Distance, second.Distance, 1, false);
        }

        int bestDistance = first.Distance;
        if (second.Distance < bestDistance)
        {
            bestDistance = second.Distance;
        }

        return CreateFailure(bestDistance, first.Distance, second.Distance);
    }

    static BchCorrectionResult CreateRedundantSuccess(
        BchCorrectionResult selected,
        int firstDistance,
        int secondDistance,
        int sourceCopy,
        bool copiesAgree)
    {
        return new BchCorrectionResult
        {
            Success = true,
            Data = selected.Data,
            Code = selected.Code,
            Distance = selected.Distance,
            FirstDistance = firstDistance,
            SecondDistance = secondDistance,
            SourceCopy = sourceCopy,
            CopiesAgree = copiesAgree
        };
    }

    static BchCorrectionResult CreateFailure(
        int distance,
        int firstDistance,
        int secondDistance)
    {
        return new BchCorrectionResult
        {
            Success = false,
            Data = -1,
            Code = -1,
            Distance = distance,
            FirstDistance = firstDistance,
            SecondDistance = secondDistance,
            SourceCopy = -1,
            CopiesAgree = false
        };
    }

    static void ValidateBitLayout(int dataBitCount, int parityBitCount)
    {
        if (dataBitCount <= 0)
        {
            throw new QrInputException("The BCH data bit count must be positive.", "dataBitCount");
        }

        if (parityBitCount <= 0)
        {
            throw new QrInputException(
                "The BCH parity bit count must be positive.",
                "parityBitCount");
        }

        if (dataBitCount > 30 || parityBitCount > 30 - dataBitCount)
        {
            throw new QrInputException(
                "The BCH code width cannot exceed 30 bits.",
                "parityBitCount");
        }
    }

    static void ValidateCorrectionInputs(
        int receivedCode,
        int bitCount,
        int[] validCodes,
        int[] validData,
        int correctionLimit)
    {
        if (validCodes == null || validCodes.Length == 0)
        {
            throw new QrInputException("The valid BCH code set cannot be empty.", "validCodes");
        }

        if (validData == null || validData.Length != validCodes.Length)
        {
            throw new QrInputException(
                "The valid BCH data must correspond to every valid code.",
                "validData");
        }

        if (correctionLimit < 0 || correctionLimit > bitCount)
        {
            throw new QrInputException(
                "The correction limit must fit the BCH code width.",
                "correctionLimit");
        }

        CountDifferences(receivedCode, receivedCode, bitCount);
        for (int index = 0; index < validCodes.Length; index++)
        {
            CountDifferences(validCodes[index], validCodes[index], bitCount);
        }
    }
}
