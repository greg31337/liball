using System;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class MetadataValueArrayTests
{
    [Test]
    public void Factory_CreatesTypedArraysAndCopiesSpanInputs()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        long[] signedSource = new long[] { 99, -2, 3, 99 };
        ulong[] unsignedSource = new ulong[] { 4, 5 };
        long[] numeratorSource = new long[] { 1, 3 };
        long[] denominatorSource = new long[] { 2, 4 };
        double[] doubleSource = new double[] { 1.25, -2.5 };
        string[] textSource = new string[] { "one", "two" };
        byte[] byteSource = new byte[] { 7, 8 };

        MetadataValue signed = factory.CreateIntegerArrayValue(new ReadOnlySpan<long>(signedSource, 1, 2));
        MetadataValue unsigned = factory.CreateUnsignedIntegerArrayValue(unsignedSource);
        MetadataValue rational = factory.CreateRationalArrayValue(numeratorSource, denominatorSource);
        MetadataValue signedRational = factory.CreateSignedRationalArrayValue(new long[] { -1 }, new long[] { 2 });
        MetadataValue floating = factory.CreateDoubleArrayValue(doubleSource);
        MetadataValue strings = factory.CreateStringListValue(textSource);
        MetadataValue bytes = factory.CreateBytesValue(byteSource);

        signedSource[1] = 100;
        unsignedSource[0] = 100;
        numeratorSource[0] = 100;
        denominatorSource[0] = 100;
        doubleSource[0] = 100;
        textSource[0] = "changed";
        byteSource[0] = 100;

        Assert.That(signed.Kind, Is.EqualTo(MetadataValueKind.IntegerArray));
        Assert.That(signed.IntegerValues, Is.EqualTo(new long[] { -2, 3 }));
        Assert.That(unsigned.Kind, Is.EqualTo(MetadataValueKind.UnsignedIntegerArray));
        Assert.That(unsigned.UnsignedIntegerValues, Is.EqualTo(new ulong[] { 4, 5 }));
        Assert.That(rational.Kind, Is.EqualTo(MetadataValueKind.RationalArray));
        Assert.That(rational.Numerators, Is.EqualTo(new long[] { 1, 3 }));
        Assert.That(rational.Denominators, Is.EqualTo(new long[] { 2, 4 }));
        Assert.That(signedRational.Kind, Is.EqualTo(MetadataValueKind.SignedRationalArray));
        Assert.That(signedRational.Numerators, Is.EqualTo(new long[] { -1 }));
        Assert.That(signedRational.Denominators, Is.EqualTo(new long[] { 2 }));
        Assert.That(floating.Kind, Is.EqualTo(MetadataValueKind.DoubleArray));
        Assert.That(floating.DoubleValues, Is.EqualTo(new double[] { 1.25, -2.5 }));
        Assert.That(strings.Kind, Is.EqualTo(MetadataValueKind.StringList));
        Assert.That(strings.TextValues, Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(bytes.Bytes, Is.EqualTo(new byte[] { 7, 8 }));
    }

    [Test]
    public void Factory_RejectsMismatchedRationalArrayLengths()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();

        Assert.Throws<ArgumentException>(() => factory.CreateRationalArrayValue(new long[] { 1 }, new long[] { 2, 3 }));
        Assert.Throws<ArgumentException>(() => factory.CreateSignedRationalArrayValue(new long[] { -1, 2 }, new long[] { 3 }));
    }
}
