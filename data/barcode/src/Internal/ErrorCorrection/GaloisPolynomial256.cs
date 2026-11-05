namespace barcode;

internal class GaloisPolynomial256
{
    int[] Coefficients;

    internal GaloisPolynomial256(int[] coefficients)
    {
        if (coefficients == null)
        {
            throw new BarcodeValidationException("GF(256) polynomial coefficients must not be null.");
        }

        if (coefficients.Length == 0)
        {
            throw new BarcodeValidationException("GF(256) polynomial must contain at least one coefficient.");
        }

        int index = 0;
        while (index < coefficients.Length)
        {
            GaloisField256.ValidateElement(coefficients[index]);
            index = index + 1;
        }

        int firstNonzero = 0;
        while (firstNonzero < coefficients.Length - 1 && coefficients[firstNonzero] == 0)
        {
            firstNonzero = firstNonzero + 1;
        }

        int length = coefficients.Length - firstNonzero;
        Coefficients = new int[length];
        index = 0;
        while (index < length)
        {
            Coefficients[index] = coefficients[firstNonzero + index];
            index = index + 1;
        }
    }

    internal int[] CopyCoefficients()
    {
        return GaloisField256.CopyTable(Coefficients);
    }

    internal int GetDegree()
    {
        return Coefficients.Length - 1;
    }

    internal bool IsZero()
    {
        return Coefficients[0] == 0;
    }

    internal int GetCoefficient(int degree)
    {
        if (degree < 0 || degree > GetDegree())
        {
            throw new BarcodeValidationException("GF(256) polynomial degree is outside the available coefficients.");
        }

        return Coefficients[Coefficients.Length - 1 - degree];
    }

    internal int EvaluateAt(int element)
    {
        GaloisField256.ValidateElement(element);
        if (element == 0)
        {
            return GetCoefficient(0);
        }

        if (element == 1)
        {
            int sum = 0;
            int sumIndex = 0;
            while (sumIndex < Coefficients.Length)
            {
                sum = GaloisField256.AddOrSubtract(sum, Coefficients[sumIndex]);
                sumIndex = sumIndex + 1;
            }

            return sum;
        }

        GaloisField256 field = GaloisField256.DataMatrix;
        int result = Coefficients[0];
        int index = 1;
        while (index < Coefficients.Length)
        {
            result = GaloisField256.AddOrSubtract(field.Multiply(element, result), Coefficients[index]);
            index = index + 1;
        }

        return result;
    }

    internal GaloisPolynomial256 AddOrSubtractPolynomial(GaloisPolynomial256 other)
    {
        if (other == null)
        {
            throw new BarcodeValidationException("GF(256) polynomial must not be null.");
        }

        if (IsZero())
        {
            return other;
        }

        if (other.IsZero())
        {
            return this;
        }

        int[] smaller = Coefficients;
        int[] larger = other.Coefficients;
        if (smaller.Length > larger.Length)
        {
            int[] temporary = smaller;
            smaller = larger;
            larger = temporary;
        }

        int[] sum = new int[larger.Length];
        int difference = larger.Length - smaller.Length;
        int index = 0;
        while (index < difference)
        {
            sum[index] = larger[index];
            index = index + 1;
        }

        while (index < larger.Length)
        {
            sum[index] = GaloisField256.AddOrSubtract(smaller[index - difference], larger[index]);
            index = index + 1;
        }

        return new GaloisPolynomial256(sum);
    }

    internal GaloisPolynomial256 MultiplyPolynomial(GaloisPolynomial256 other)
    {
        if (other == null)
        {
            throw new BarcodeValidationException("GF(256) polynomial must not be null.");
        }

        GaloisField256 field = GaloisField256.DataMatrix;
        if (IsZero() || other.IsZero())
        {
            return field.CreateZeroPolynomial();
        }

        int productLength = CheckedMath.Subtract(CheckedMath.Add(Coefficients.Length, other.Coefficients.Length), 1);
        int[] product = new int[productLength];
        int leftIndex = 0;
        while (leftIndex < Coefficients.Length)
        {
            int rightIndex = 0;
            while (rightIndex < other.Coefficients.Length)
            {
                int productIndex = leftIndex + rightIndex;
                int term = field.Multiply(Coefficients[leftIndex], other.Coefficients[rightIndex]);
                product[productIndex] = GaloisField256.AddOrSubtract(product[productIndex], term);
                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }

        return new GaloisPolynomial256(product);
    }

    internal GaloisPolynomial256 MultiplyByScalar(int scalar)
    {
        GaloisField256.ValidateElement(scalar);
        GaloisField256 field = GaloisField256.DataMatrix;
        if (scalar == 0)
        {
            return field.CreateZeroPolynomial();
        }

        if (scalar == 1)
        {
            return this;
        }

        int[] product = new int[Coefficients.Length];
        int index = 0;
        while (index < Coefficients.Length)
        {
            product[index] = field.Multiply(Coefficients[index], scalar);
            index = index + 1;
        }

        return new GaloisPolynomial256(product);
    }

    internal GaloisPolynomial256 MultiplyByMonomial(int degree, int coefficient)
    {
        if (degree < 0)
        {
            throw new BarcodeValidationException("GF(256) monomial degree must not be negative.");
        }

        GaloisField256.ValidateElement(coefficient);
        GaloisField256 field = GaloisField256.DataMatrix;
        if (coefficient == 0)
        {
            return field.CreateZeroPolynomial();
        }

        int[] product = new int[CheckedMath.Add(Coefficients.Length, degree)];
        int index = 0;
        while (index < Coefficients.Length)
        {
            product[index] = field.Multiply(Coefficients[index], coefficient);
            index = index + 1;
        }

        return new GaloisPolynomial256(product);
    }

    internal GaloisPolynomial256[] DivideByPolynomial(GaloisPolynomial256 divisor)
    {
        if (divisor == null)
        {
            throw new BarcodeValidationException("GF(256) divisor must not be null.");
        }

        if (divisor.IsZero())
        {
            throw new BarcodeValidationException("GF(256) polynomial division by zero is not defined.");
        }

        GaloisField256 field = GaloisField256.DataMatrix;
        GaloisPolynomial256 quotient = field.CreateZeroPolynomial();
        GaloisPolynomial256 remainder = this;
        int leadingCoefficient = divisor.GetCoefficient(divisor.GetDegree());
        int inverseLeadingCoefficient = field.Inverse(leadingCoefficient);
        while (remainder.GetDegree() >= divisor.GetDegree() && !remainder.IsZero())
        {
            int degreeDifference = remainder.GetDegree() - divisor.GetDegree();
            int scale = field.Multiply(remainder.GetCoefficient(remainder.GetDegree()), inverseLeadingCoefficient);
            GaloisPolynomial256 term = divisor.MultiplyByMonomial(degreeDifference, scale);
            GaloisPolynomial256 quotientTerm = field.BuildMonomial(degreeDifference, scale);
            quotient = quotient.AddOrSubtractPolynomial(quotientTerm);
            remainder = remainder.AddOrSubtractPolynomial(term);
        }

        return new GaloisPolynomial256[] { quotient, remainder };
    }

    internal GaloisPolynomial256 CalculateFormalDerivative()
    {
        int degree = GetDegree();
        if (degree == 0)
        {
            return GaloisField256.DataMatrix.CreateZeroPolynomial();
        }

        int[] derivative = new int[degree];
        int coefficientIndex = 0;
        while (coefficientIndex < Coefficients.Length - 1)
        {
            int coefficientDegree = degree - coefficientIndex;
            if (coefficientDegree % 2 == 1)
            {
                derivative[coefficientIndex] = Coefficients[coefficientIndex];
            }

            coefficientIndex = coefficientIndex + 1;
        }

        return new GaloisPolynomial256(derivative);
    }
}
