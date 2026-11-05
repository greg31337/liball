namespace jpeg.Tests;

internal class CoefficientOrderConverterTests
{
    [Test]
    public void Converter_MapsZigZagAndNaturalIndexes()
    {
        global::jpeg.JpegCoefficientOrderConverter converter = new global::jpeg.JpegCoefficientOrderConverter();

        Assert.That(converter.ToNaturalIndex(0), Is.EqualTo(0));
        Assert.That(converter.ToNaturalIndex(2), Is.EqualTo(8));
        Assert.That(converter.ToNaturalIndex(63), Is.EqualTo(63));
        Assert.That(converter.ToZigZagIndex(0), Is.EqualTo(0));
        Assert.That(converter.ToZigZagIndex(8), Is.EqualTo(2));
        Assert.That(converter.ToZigZagIndex(63), Is.EqualTo(63));
    }

    [Test]
    public void Converter_ConvertsNaturalBlockToZigZagOrder()
    {
        global::jpeg.JpegCoefficientOrderConverter converter = new global::jpeg.JpegCoefficientOrderConverter();
        int[] natural = CreateSequentialBlock(100);

        int[] zigZag = converter.NaturalToZigZag(natural);

        Assert.That(zigZag[0], Is.EqualTo(100));
        Assert.That(zigZag[1], Is.EqualTo(101));
        Assert.That(zigZag[2], Is.EqualTo(108));
        Assert.That(zigZag[3], Is.EqualTo(116));
        Assert.That(zigZag[63], Is.EqualTo(163));
    }

    [Test]
    public void Converter_ConvertsZigZagBlockToNaturalOrder()
    {
        global::jpeg.JpegCoefficientOrderConverter converter = new global::jpeg.JpegCoefficientOrderConverter();
        int[] zigZag = CreateSequentialBlock(1);

        int[] natural = converter.ZigZagToNatural(zigZag);

        Assert.That(natural[0], Is.EqualTo(1));
        Assert.That(natural[1], Is.EqualTo(2));
        Assert.That(natural[8], Is.EqualTo(3));
        Assert.That(natural[16], Is.EqualTo(4));
        Assert.That(natural[63], Is.EqualTo(64));
    }

    [Test]
    public void Converter_RoundTripsBetweenOrders()
    {
        global::jpeg.JpegCoefficientOrderConverter converter = new global::jpeg.JpegCoefficientOrderConverter();
        int[] natural = CreateSequentialBlock(-32);

        int[] zigZag = converter.NaturalToZigZag(natural);
        int[] roundTrip = converter.ZigZagToNatural(zigZag);

        Assert.That(roundTrip, Is.EqualTo(natural));
    }

    [Test]
    public void Converter_CopyMethodsWriteOnlyFirstBlock()
    {
        global::jpeg.JpegCoefficientOrderConverter converter = new global::jpeg.JpegCoefficientOrderConverter();
        int[] natural = CreateSequentialBlock(0);
        int[] zigZag = new int[70];
        int[] roundTrip = new int[70];
        zigZag[64] = 777;
        roundTrip[64] = 888;

        converter.CopyNaturalToZigZag(natural, zigZag);
        converter.CopyZigZagToNatural(zigZag, roundTrip);

        Assert.That(zigZag[64], Is.EqualTo(777));
        Assert.That(roundTrip[64], Is.EqualTo(888));
        Assert.That(roundTrip[0], Is.EqualTo(0));
        Assert.That(roundTrip[63], Is.EqualTo(63));
    }

    [Test]
    public void Converter_ConvertsBlocksInPlace()
    {
        global::jpeg.JpegCoefficientOrderConverter converter = new global::jpeg.JpegCoefficientOrderConverter();
        int[] coefficients = CreateSequentialBlock(10);
        int expectedAtNaturalEight = coefficients[8];

        converter.ConvertNaturalToZigZagInPlace(coefficients);

        Assert.That(coefficients[2], Is.EqualTo(expectedAtNaturalEight));

        converter.ConvertZigZagToNaturalInPlace(coefficients);

        Assert.That(coefficients, Is.EqualTo(CreateSequentialBlock(10)));
    }

    [Test]
    public void Converter_RejectsInvalidBlocksAndIndexes()
    {
        global::jpeg.JpegCoefficientOrderConverter converter = new global::jpeg.JpegCoefficientOrderConverter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                converter.NaturalToZigZag(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                converter.ZigZagToNatural(new int[1]);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                converter.ToNaturalIndex(-1);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                converter.ToZigZagIndex(64);
            });
    }

    public int[] CreateSequentialBlock(int start)
    {
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < coefficients.Length)
        {
            coefficients[index] = start + index;
            index = index + 1;
        }

        return coefficients;
    }
}
