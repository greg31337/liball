namespace asn1;

internal class CharacterRepertoireProvider
{
    internal IntegerRangeAlgebra Algebra = new IntegerRangeAlgebra();

    internal IntegerConstraintSet CreateRepertoire(AsnTypeKind kind)
    {
        IntegerConstraintSet unicode = this.CreateUnicodeScalars();
        if (kind == AsnTypeKind.NumericString)
        {
            return this.Algebra.Union([
                this.Algebra.CreateSingleton(new System.Numerics.BigInteger(32)),
                this.Algebra.CreateInterval(
                    true,
                    new System.Numerics.BigInteger(48),
                    true,
                    new System.Numerics.BigInteger(57))]);
        }

        if (kind == AsnTypeKind.PrintableString)
        {
            return this.CreatePrintableString();
        }

        if (kind == AsnTypeKind.Ia5String)
        {
            return this.Algebra.CreateInterval(
                true,
                System.Numerics.BigInteger.Zero,
                true,
                new System.Numerics.BigInteger(127));
        }

        if (kind == AsnTypeKind.VisibleString)
        {
            return this.Algebra.CreateInterval(
                true,
                new System.Numerics.BigInteger(32),
                true,
                new System.Numerics.BigInteger(126));
        }

        if (kind == AsnTypeKind.BmpString)
        {
            return this.Algebra.Intersect([
                unicode,
                this.Algebra.CreateInterval(
                    true,
                    System.Numerics.BigInteger.Zero,
                    true,
                    new System.Numerics.BigInteger(65535))]);
        }

        if (kind == AsnTypeKind.TeletexString
            || kind == AsnTypeKind.VideotexString
            || kind == AsnTypeKind.GeneralString)
        {
            return this.Algebra.CreateInterval(
                true,
                System.Numerics.BigInteger.Zero,
                true,
                new System.Numerics.BigInteger(255));
        }

        if (kind == AsnTypeKind.GraphicString
            || kind == AsnTypeKind.ObjectDescriptor)
        {
            return this.Algebra.Intersect([
                unicode,
                this.Algebra.Union([
                    this.Algebra.CreateInterval(
                        true,
                        new System.Numerics.BigInteger(32),
                        true,
                        new System.Numerics.BigInteger(126)),
                    this.Algebra.CreateInterval(
                        true,
                        new System.Numerics.BigInteger(160),
                        true,
                        new System.Numerics.BigInteger(1114111))])]);
        }

        return unicode;
    }

    internal IntegerConstraintSet CreateUnicodeScalars()
    {
        return this.Algebra.Union([
            this.Algebra.CreateInterval(
                true,
                System.Numerics.BigInteger.Zero,
                true,
                new System.Numerics.BigInteger(55295)),
            this.Algebra.CreateInterval(
                true,
                new System.Numerics.BigInteger(57344),
                true,
                new System.Numerics.BigInteger(1114111))]);
    }

    internal IntegerConstraintSet CreatePrintableString()
    {
        int[] singles = [32, 39, 40, 41, 43, 44, 45, 46, 47, 58, 61, 63];
        IntegerConstraintSet[] sets = new IntegerConstraintSet[singles.Length + 3];
        int index = 0;
        while (index < singles.Length)
        {
            sets[index] = this.Algebra.CreateSingleton(
                new System.Numerics.BigInteger(singles[index]));
            index = index + 1;
        }

        sets[index] = this.Algebra.CreateInterval(
            true,
            new System.Numerics.BigInteger(48),
            true,
            new System.Numerics.BigInteger(57));
        index = index + 1;
        sets[index] = this.Algebra.CreateInterval(
            true,
            new System.Numerics.BigInteger(65),
            true,
            new System.Numerics.BigInteger(90));
        index = index + 1;
        sets[index] = this.Algebra.CreateInterval(
            true,
            new System.Numerics.BigInteger(97),
            true,
            new System.Numerics.BigInteger(122));
        return this.Algebra.Union(sets);
    }
}
