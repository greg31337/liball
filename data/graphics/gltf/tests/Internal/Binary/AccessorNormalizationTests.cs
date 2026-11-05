namespace gltf.Tests.Internal.Binary;

[TestFixture]
public class AccessorNormalizationTests
{
    [Test]
    public void DecodeSignedValues_ClampsSignedMinimaAndUsesGltfDivisors()
    {
        AccessorNormalization normalization = new AccessorNormalization();

        double[] bytes = normalization.DecodeSignedValues(
            new int[] { -128, -127, -64, 0, 63, 127 },
            GltfComponentType.SignedByte);
        double[] shorts = normalization.DecodeSignedValues(
            new int[] { -32768, -32767, -16384, 0, 16383, 32767 },
            GltfComponentType.SignedShort);

        Assert.Multiple(() =>
        {
            Assert.That(
                bytes,
                Is.EqualTo(new double[]
                {
                    -1.0,
                    -1.0,
                    -64.0 / 127.0,
                    0.0,
                    63.0 / 127.0,
                    1.0
                }));
            Assert.That(
                shorts,
                Is.EqualTo(new double[]
                {
                    -1.0,
                    -1.0,
                    -16384.0 / 32767.0,
                    0.0,
                    16383.0 / 32767.0,
                    1.0
                }));
        });
    }

    [Test]
    public void DecodeUnsignedValues_UsesGltfDivisors()
    {
        AccessorNormalization normalization = new AccessorNormalization();

        double[] bytes = normalization.DecodeUnsignedValues(
            new uint[] { 0U, 1U, 127U, 255U },
            GltfComponentType.UnsignedByte);
        double[] shorts = normalization.DecodeUnsignedValues(
            new uint[] { 0U, 1U, 32767U, 65535U },
            GltfComponentType.UnsignedShort);

        Assert.Multiple(() =>
        {
            Assert.That(
                bytes,
                Is.EqualTo(new double[]
                {
                    0.0,
                    1.0 / 255.0,
                    127.0 / 255.0,
                    1.0
                }));
            Assert.That(
                shorts,
                Is.EqualTo(new double[]
                {
                    0.0,
                    1.0 / 65535.0,
                    32767.0 / 65535.0,
                    1.0
                }));
        });
    }

    [Test]
    public void EncodeSignedValues_RoundsMidpointsAwayFromZero()
    {
        AccessorNormalization normalization = new AccessorNormalization();

        int[] bytes = normalization.EncodeSignedValues(
            new double[]
            {
                -1.0,
                -0.5,
                -0.5 / 127.0,
                0.0,
                0.5 / 127.0,
                0.5,
                1.0
            },
            GltfComponentType.SignedByte);
        int[] shorts = normalization.EncodeSignedValues(
            new double[] { -1.0, -0.5, 0.0, 0.5, 1.0 },
            GltfComponentType.SignedShort);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(new int[] { -127, -64, -1, 0, 1, 64, 127 }));
            Assert.That(shorts, Is.EqualTo(new int[] { -32767, -16384, 0, 16384, 32767 }));
        });
    }

    [Test]
    public void EncodeUnsignedValues_RoundsMidpointsAwayFromZero()
    {
        AccessorNormalization normalization = new AccessorNormalization();

        uint[] bytes = normalization.EncodeUnsignedValues(
            new double[] { 0.0, 0.5 / 255.0, 0.5, 1.0 },
            GltfComponentType.UnsignedByte);
        uint[] shorts = normalization.EncodeUnsignedValues(
            new double[] { 0.0, 0.5 / 65535.0, 0.5, 1.0 },
            GltfComponentType.UnsignedShort);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(new uint[] { 0U, 1U, 128U, 255U }));
            Assert.That(shorts, Is.EqualTo(new uint[] { 0U, 1U, 32768U, 65535U }));
        });
    }

    [Test]
    public void EncodeAfterDecode_RoundTripsEveryIntegerExceptSignedMinima()
    {
        AccessorNormalization normalization = new AccessorNormalization();
        int[] signedBytes = new int[256];
        uint[] unsignedBytes = new uint[256];
        int byteIndex = 0;
        while (byteIndex < 256)
        {
            signedBytes[byteIndex] = byteIndex - 128;
            unsignedBytes[byteIndex] = (uint)byteIndex;
            byteIndex++;
        }

        int[] signedShorts = new int[65536];
        uint[] unsignedShorts = new uint[65536];
        int shortIndex = 0;
        while (shortIndex < 65536)
        {
            signedShorts[shortIndex] = shortIndex - 32768;
            unsignedShorts[shortIndex] = (uint)shortIndex;
            shortIndex++;
        }

        int[] encodedSignedBytes = normalization.EncodeSignedValues(
            normalization.DecodeSignedValues(signedBytes, GltfComponentType.SignedByte),
            GltfComponentType.SignedByte);
        uint[] encodedUnsignedBytes = normalization.EncodeUnsignedValues(
            normalization.DecodeUnsignedValues(unsignedBytes, GltfComponentType.UnsignedByte),
            GltfComponentType.UnsignedByte);
        int[] encodedSignedShorts = normalization.EncodeSignedValues(
            normalization.DecodeSignedValues(signedShorts, GltfComponentType.SignedShort),
            GltfComponentType.SignedShort);
        uint[] encodedUnsignedShorts = normalization.EncodeUnsignedValues(
            normalization.DecodeUnsignedValues(unsignedShorts, GltfComponentType.UnsignedShort),
            GltfComponentType.UnsignedShort);
        signedBytes[0] = -127;
        signedShorts[0] = -32767;

        Assert.Multiple(() =>
        {
            Assert.That(encodedSignedBytes, Is.EqualTo(signedBytes));
            Assert.That(encodedUnsignedBytes, Is.EqualTo(unsignedBytes));
            Assert.That(encodedSignedShorts, Is.EqualTo(signedShorts));
            Assert.That(encodedUnsignedShorts, Is.EqualTo(unsignedShorts));
        });
    }

    [Test]
    public void DecodeValues_RejectsEncodedValuesOutsideTheirComponentRange()
    {
        AccessorNormalization normalization = new AccessorNormalization();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => normalization.DecodeSignedValue(-129, GltfComponentType.SignedByte),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => normalization.DecodeSignedValue(32768, GltfComponentType.SignedShort),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => normalization.DecodeUnsignedValue(256U, GltfComponentType.UnsignedByte),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => normalization.DecodeUnsignedValue(65536U, GltfComponentType.UnsignedShort),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void EncodeValues_RejectsNonFiniteOrOutOfRangeNormalizedValues()
    {
        AccessorNormalization normalization = new AccessorNormalization();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => normalization.EncodeSignedValue(-1.0001, GltfComponentType.SignedByte),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => normalization.EncodeSignedValue(double.NaN, GltfComponentType.SignedShort),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => normalization.EncodeUnsignedValue(-0.0001, GltfComponentType.UnsignedByte),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => normalization.EncodeUnsignedValue(double.PositiveInfinity, GltfComponentType.UnsignedShort),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => normalization.EncodeUnsignedValue(1.0001, GltfComponentType.UnsignedShort),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void NormalizeValues_RejectsUnsupportedComponentTypes()
    {
        AccessorNormalization normalization = new AccessorNormalization();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => normalization.DecodeSignedValues([], GltfComponentType.Float),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("componentType"));
            Assert.That(
                () => normalization.DecodeUnsignedValues([], GltfComponentType.UnsignedInt),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("componentType"));
            Assert.That(
                () => normalization.EncodeSignedValues([], GltfComponentType.UnsignedByte),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("componentType"));
            Assert.That(
                () => normalization.EncodeUnsignedValues([], GltfComponentType.SignedShort),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("componentType"));
        });
    }
}
