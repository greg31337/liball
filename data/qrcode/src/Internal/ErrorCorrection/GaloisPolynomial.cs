namespace qrcode;

internal class GaloisPolynomial
{
    internal static int Evaluate(int[] coefficients, int value)
    {
        if (coefficients == null)
        {
            throw new QrInputException("The polynomial coefficients cannot be null.", "coefficients");
        }

        GaloisField256.Add(value, 0);
        int result = 0;
        for (int index = 0; index < coefficients.Length; index++)
        {
            result = GaloisField256.Add(
                GaloisField256.Multiply(result, value),
                coefficients[index]);
        }

        return result;
    }
}
