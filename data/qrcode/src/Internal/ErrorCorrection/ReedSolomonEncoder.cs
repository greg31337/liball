namespace qrcode;

internal class ReedSolomonEncoder
{
    internal static byte[] CreateParity(byte[] data, int errorCorrectionCodewordCount)
    {
        if (data == null)
        {
            throw new QrInputException("The data codewords cannot be null.", "data");
        }

        if (errorCorrectionCodewordCount <= 0
            || errorCorrectionCodewordCount > GaloisField256.MultiplicativeOrder)
        {
            throw new QrInputException(
                "The error-correction codeword count must be between one and 255.",
                "errorCorrectionCodewordCount");
        }

        CheckedDimensions.CalculateSum(
            data.Length,
            errorCorrectionCodewordCount,
            GaloisField256.MultiplicativeOrder,
            "ReedSolomonBlockLength");
        int[] generator = ReedSolomonGenerator.CreateCoefficients(errorCorrectionCodewordCount);
        byte[] parity = new byte[errorCorrectionCodewordCount];
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex++)
        {
            int factor = data[dataIndex] ^ parity[0];
            for (int parityIndex = 0;
                parityIndex < errorCorrectionCodewordCount - 1;
                parityIndex++)
            {
                parity[parityIndex] = (byte)(parity[parityIndex + 1]
                    ^ GaloisField256.Multiply(generator[parityIndex + 1], factor));
            }

            parity[errorCorrectionCodewordCount - 1] = (byte)GaloisField256.Multiply(
                generator[errorCorrectionCodewordCount],
                factor);
        }

        return parity;
    }
}
