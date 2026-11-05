namespace qrcode;

internal class ReedSolomonGenerator
{
    static readonly int[][] CoefficientCache = CreateCoefficientCache();

    internal static int[] CreateCoefficients(int errorCorrectionCodewordCount)
    {
        ValidateDegree(errorCorrectionCodewordCount);
        int[] cached = CoefficientCache[errorCorrectionCodewordCount];
        int[] copy = new int[cached.Length];
        for (int index = 0; index < cached.Length; index++)
        {
            copy[index] = cached[index];
        }

        return copy;
    }

    static int[][] CreateCoefficientCache()
    {
        int[][] cache = new int[GaloisField256.MultiplicativeOrder + 1][];
        cache[0] = new int[] { 1 };
        for (int degree = 1; degree <= GaloisField256.MultiplicativeOrder; degree++)
        {
            int[] previous = cache[degree - 1];
            int[] coefficients = new int[degree + 1];
            int root = GaloisField256.GetExponent(degree - 1);
            for (int index = 0; index < previous.Length; index++)
            {
                coefficients[index] = GaloisField256.Add(
                    coefficients[index],
                    previous[index]);
                coefficients[index + 1] = GaloisField256.Add(
                    coefficients[index + 1],
                    GaloisField256.Multiply(previous[index], root));
            }

            cache[degree] = coefficients;
        }

        return cache;
    }

    static void ValidateDegree(int errorCorrectionCodewordCount)
    {
        if (errorCorrectionCodewordCount <= 0
            || errorCorrectionCodewordCount > GaloisField256.MultiplicativeOrder)
        {
            throw new QrInputException(
                "The error-correction codeword count must be between one and 255.",
                "errorCorrectionCodewordCount");
        }
    }
}
