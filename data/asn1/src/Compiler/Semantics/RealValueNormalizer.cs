namespace asn1;

internal class RealValueNormalizer
{
    internal RealNormalizationResult NormalizeRealValue(AsnRealValue? source)
    {
        if (source == null)
        {
            return this.CreateRealFailure("A REAL value has no payload.");
        }

        switch (source.Kind)
        {
            case AsnRealKind.Zero:
            case AsnRealKind.MinusZero:
            case AsnRealKind.PositiveInfinity:
            case AsnRealKind.NegativeInfinity:
            case AsnRealKind.NotANumber:
                return this.NormalizeSpecialRealValue(source.Kind);
            case AsnRealKind.FiniteDecimal:
                return this.NormalizeDecimalRealValue(source);
            case AsnRealKind.FiniteBinary:
                return this.NormalizeBinaryRealValue(source);
            default:
                return this.CreateRealFailure("The REAL value category is not recognized.");
        }
    }

    internal RealNormalizationResult NormalizeSpecialRealValue(AsnRealKind kind)
    {
        return new RealNormalizationResult
        {
            Success = true,
            Value = new AsnRealValue
            {
                Kind = kind,
                Normalized = true
            }
        };
    }

    internal RealNormalizationResult NormalizeDecimalRealValue(AsnRealValue source)
    {
        if (!source.HasDecimalValue || source.DecimalText.Length == 0)
        {
            return this.CreateRealFailure("A finite decimal REAL value has no decimal text.");
        }

        DecimalRealParseResult parsed = this.ParseDecimalRealText(source.DecimalText);
        if (!parsed.Success)
        {
            return this.CreateRealFailure(
                "The finite decimal REAL value is not valid decimal notation.");
        }

        if (parsed.Coefficient == System.Numerics.BigInteger.Zero)
        {
            return this.NormalizeSpecialRealValue(
                parsed.Negative ? AsnRealKind.MinusZero : AsnRealKind.Zero);
        }

        System.Numerics.BigInteger coefficient = parsed.Coefficient;
        System.Numerics.BigInteger exponent = parsed.Exponent;
        System.Numerics.BigInteger ten = new System.Numerics.BigInteger(10);
        while (coefficient % ten == System.Numerics.BigInteger.Zero)
        {
            coefficient = coefficient / ten;
            exponent = exponent + System.Numerics.BigInteger.One;
        }

        return new RealNormalizationResult
        {
            Success = true,
            Value = new AsnRealValue
            {
                Kind = AsnRealKind.FiniteDecimal,
                Normalized = true,
                HasDecimalValue = true,
                DecimalText = this.FormatNormalizedDecimal(coefficient, exponent),
                DecimalCoefficient = coefficient,
                DecimalExponent = exponent
            }
        };
    }

    internal DecimalRealParseResult ParseDecimalRealText(string text)
    {
        int position = 0;
        bool negative = false;
        if (position < text.Length && (text[position] == '+' || text[position] == '-'))
        {
            negative = text[position] == '-';
            position = position + 1;
        }

        System.Text.StringBuilder digits = new System.Text.StringBuilder();
        int integerDigits = this.AppendDecimalDigits(text, position, digits);
        position = position + integerDigits;
        int fractionDigits = 0;
        if (position < text.Length && text[position] == '.')
        {
            position = position + 1;
            fractionDigits = this.AppendDecimalDigits(text, position, digits);
            position = position + fractionDigits;
        }

        if (digits.Length == 0)
        {
            return new DecimalRealParseResult();
        }

        bool exponentNegative = false;
        System.Numerics.BigInteger lexicalExponent = System.Numerics.BigInteger.Zero;
        if (position < text.Length && (text[position] == 'E' || text[position] == 'e'))
        {
            position = position + 1;
            if (position < text.Length && (text[position] == '+' || text[position] == '-'))
            {
                exponentNegative = text[position] == '-';
                position = position + 1;
            }

            int exponentStart = position;
            while (position < text.Length && this.IsDecimalDigit(text[position]))
            {
                position = position + 1;
            }

            if (position == exponentStart)
            {
                return new DecimalRealParseResult();
            }

            lexicalExponent = System.Numerics.BigInteger.Parse(
                text.Substring(exponentStart, position - exponentStart),
                System.Globalization.CultureInfo.InvariantCulture);
            if (exponentNegative)
            {
                lexicalExponent = -lexicalExponent;
            }
        }

        if (position != text.Length)
        {
            return new DecimalRealParseResult();
        }

        System.Numerics.BigInteger coefficient = System.Numerics.BigInteger.Parse(
            digits.ToString(),
            System.Globalization.CultureInfo.InvariantCulture);
        if (negative)
        {
            coefficient = -coefficient;
        }

        return new DecimalRealParseResult
        {
            Success = true,
            Negative = negative,
            Coefficient = coefficient,
            Exponent = lexicalExponent - new System.Numerics.BigInteger(fractionDigits)
        };
    }

    internal int AppendDecimalDigits(
        string text,
        int position,
        System.Text.StringBuilder digits)
    {
        int start = position;
        while (position < text.Length && this.IsDecimalDigit(text[position]))
        {
            digits.Append(text[position]);
            position = position + 1;
        }

        return position - start;
    }

    internal bool IsDecimalDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal string FormatNormalizedDecimal(
        System.Numerics.BigInteger coefficient,
        System.Numerics.BigInteger exponent)
    {
        bool negative = coefficient < System.Numerics.BigInteger.Zero;
        string digits = System.Numerics.BigInteger.Abs(coefficient).ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        System.Numerics.BigInteger scientificExponent = exponent
            + new System.Numerics.BigInteger(digits.Length - 1);
        string text = negative ? "-" : "";
        text = text + digits.Substring(0, 1);
        if (digits.Length > 1)
        {
            text = text + "." + digits.Substring(1);
        }

        if (scientificExponent != System.Numerics.BigInteger.Zero)
        {
            text = text + "E";
            if (scientificExponent > System.Numerics.BigInteger.Zero)
            {
                text = text + "+";
            }

            text = text + scientificExponent.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        return text;
    }

    internal RealNormalizationResult NormalizeBinaryRealValue(AsnRealValue source)
    {
        if (!source.HasBinaryValue
            || (source.Radix != 2 && source.Radix != 8 && source.Radix != 16))
        {
            return this.CreateRealFailure(
                "A finite binary REAL value requires base 2, 8, or 16.");
        }

        if (source.Mantissa == System.Numerics.BigInteger.Zero)
        {
            return this.NormalizeSpecialRealValue(AsnRealKind.Zero);
        }

        int exponentFactor = source.Radix == 2 ? 1 : source.Radix == 8 ? 3 : 4;
        System.Numerics.BigInteger mantissa = source.Mantissa;
        System.Numerics.BigInteger exponent = source.Exponent
            * new System.Numerics.BigInteger(exponentFactor);
        System.Numerics.BigInteger two = new System.Numerics.BigInteger(2);
        while (mantissa % two == System.Numerics.BigInteger.Zero)
        {
            mantissa = mantissa / two;
            exponent = exponent + System.Numerics.BigInteger.One;
        }

        return new RealNormalizationResult
        {
            Success = true,
            Value = new AsnRealValue
            {
                Kind = AsnRealKind.FiniteBinary,
                Normalized = true,
                HasBinaryValue = true,
                Mantissa = mantissa,
                Radix = 2,
                Exponent = exponent
            }
        };
    }

    internal RealNormalizationResult CreateRealFailure(string message)
    {
        return new RealNormalizationResult
        {
            ErrorMessage = message
        };
    }
}
