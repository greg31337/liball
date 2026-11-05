namespace barcode;

internal class ModulusPolynomial929
{
    int[] Coefficients;

    internal ModulusPolynomial929(int[] coefficients)
    {
        if (coefficients == null)
        {
            throw new BarcodeValidationException("Modulo-929 polynomial coefficients must not be null.");
        }

        if (coefficients.Length == 0)
        {
            throw new BarcodeValidationException("Modulo-929 polynomial must contain at least one coefficient.");
        }

        int index = 0;
        while (index < coefficients.Length)
        {
            ModulusField929.ValidateElement(coefficients[index]);
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
        return ModulusField929.CopyTable(Coefficients);
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
            throw new BarcodeValidationException("Modulo-929 polynomial degree is outside the available coefficients.");
        }

        return Coefficients[Coefficients.Length - 1 - degree];
    }

    internal int EvaluateAt(int element)
    {
        ModulusField929.ValidateElement(element);
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
                sum = ModulusField929.Add(sum, Coefficients[sumIndex]);
                sumIndex = sumIndex + 1;
            }

            return sum;
        }

        ModulusField929 field = ModulusField929.Pdf417;
        int result = Coefficients[0];
        int index = 1;
        while (index < Coefficients.Length)
        {
            result = ModulusField929.Add(field.Multiply(element, result), Coefficients[index]);
            index = index + 1;
        }

        return result;
    }

    internal ModulusPolynomial929 AddPolynomial(ModulusPolynomial929 other)
    {
        if (other == null)
        {
            throw new BarcodeValidationException("Modulo-929 polynomial must not be null.");
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
            sum[index] = ModulusField929.Add(smaller[index - difference], larger[index]);
            index = index + 1;
        }

        return new ModulusPolynomial929(sum);
    }

    internal ModulusPolynomial929 SubtractPolynomial(ModulusPolynomial929 other)
    {
        if (other == null)
        {
            throw new BarcodeValidationException("Modulo-929 polynomial must not be null.");
        }

        if (other.IsZero())
        {
            return this;
        }

        return AddPolynomial(other.NegatePolynomial());
    }

    internal ModulusPolynomial929 NegatePolynomial()
    {
        if (IsZero())
        {
            return this;
        }

        int[] negative = new int[Coefficients.Length];
        int index = 0;
        while (index < Coefficients.Length)
        {
            negative[index] = ModulusField929.Negate(Coefficients[index]);
            index = index + 1;
        }

        return new ModulusPolynomial929(negative);
    }

    internal ModulusPolynomial929 MultiplyPolynomial(ModulusPolynomial929 other)
    {
        if (other == null)
        {
            throw new BarcodeValidationException("Modulo-929 polynomial must not be null.");
        }

        ModulusField929 field = ModulusField929.Pdf417;
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
                product[productIndex] = ModulusField929.Add(product[productIndex], term);
                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }

        return new ModulusPolynomial929(product);
    }

    internal ModulusPolynomial929 MultiplyByScalar(int scalar)
    {
        ModulusField929.ValidateElement(scalar);
        ModulusField929 field = ModulusField929.Pdf417;
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

        return new ModulusPolynomial929(product);
    }

    internal ModulusPolynomial929 MultiplyByMonomial(int degree, int coefficient)
    {
        if (degree < 0)
        {
            throw new BarcodeValidationException("Modulo-929 monomial degree must not be negative.");
        }

        ModulusField929.ValidateElement(coefficient);
        ModulusField929 field = ModulusField929.Pdf417;
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

        return new ModulusPolynomial929(product);
    }

    internal ModulusPolynomial929[] DivideByPolynomial(ModulusPolynomial929 divisor)
    {
        if (divisor == null)
        {
            throw new BarcodeValidationException("Modulo-929 divisor must not be null.");
        }

        if (divisor.IsZero())
        {
            throw new BarcodeValidationException("Modulo-929 polynomial division by zero is not defined.");
        }

        ModulusField929 field = ModulusField929.Pdf417;
        ModulusPolynomial929 quotient = field.CreateZeroPolynomial();
        ModulusPolynomial929 remainder = this;
        int leadingCoefficient = divisor.GetCoefficient(divisor.GetDegree());
        int inverseLeadingCoefficient = field.Inverse(leadingCoefficient);
        while (remainder.GetDegree() >= divisor.GetDegree() && !remainder.IsZero())
        {
            int degreeDifference = remainder.GetDegree() - divisor.GetDegree();
            int scale = field.Multiply(remainder.GetCoefficient(remainder.GetDegree()), inverseLeadingCoefficient);
            ModulusPolynomial929 term = divisor.MultiplyByMonomial(degreeDifference, scale);
            ModulusPolynomial929 quotientTerm = field.BuildMonomial(degreeDifference, scale);
            quotient = quotient.AddPolynomial(quotientTerm);
            remainder = remainder.SubtractPolynomial(term);
        }

        return new ModulusPolynomial929[] { quotient, remainder };
    }

    internal ModulusPolynomial929 CalculateFormalDerivative()
    {
        int degree = GetDegree();
        if (degree == 0)
        {
            return ModulusField929.Pdf417.CreateZeroPolynomial();
        }

        ModulusField929 field = ModulusField929.Pdf417;
        int[] derivative = new int[degree];
        int coefficientIndex = 0;
        while (coefficientIndex < Coefficients.Length - 1)
        {
            int coefficientDegree = degree - coefficientIndex;
            int degreeElement = coefficientDegree % ModulusField929.Modulus;
            derivative[coefficientIndex] = field.Multiply(Coefficients[coefficientIndex], degreeElement);
            coefficientIndex = coefficientIndex + 1;
        }

        return new ModulusPolynomial929(derivative);
    }
}
