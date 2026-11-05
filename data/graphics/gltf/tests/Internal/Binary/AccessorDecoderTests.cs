namespace gltf.Tests.Internal.Binary;

[TestFixture]
public class AccessorDecoderTests
{
    [Test]
    public void DecodeFloatingValues_ReadsAllSixComponentTypesAsLittleEndian()
    {
        AccessorDecoder decoder = new AccessorDecoder();

        Assert.Multiple(() =>
        {
            Assert.That(
                decoder.DecodeFloatingValues(
                    new byte[] { 0x80 },
                    CreateRange(GltfComponentType.SignedByte, GltfAccessorType.Scalar, 1, 0, 0, 0)),
                Is.EqualTo(new double[] { -128.0 }));
            Assert.That(
                decoder.DecodeFloatingValues(
                    new byte[] { 0xfe },
                    CreateRange(GltfComponentType.UnsignedByte, GltfAccessorType.Scalar, 1, 0, 0, 0)),
                Is.EqualTo(new double[] { 254.0 }));
            Assert.That(
                decoder.DecodeFloatingValues(
                    new byte[] { 0x00, 0x80 },
                    CreateRange(GltfComponentType.SignedShort, GltfAccessorType.Scalar, 1, 0, 0, 0)),
                Is.EqualTo(new double[] { -32768.0 }));
            Assert.That(
                decoder.DecodeFloatingValues(
                    new byte[] { 0xfe, 0xff },
                    CreateRange(GltfComponentType.UnsignedShort, GltfAccessorType.Scalar, 1, 0, 0, 0)),
                Is.EqualTo(new double[] { 65534.0 }));
            Assert.That(
                decoder.DecodeFloatingValues(
                    new byte[] { 0x78, 0x56, 0x34, 0x12 },
                    CreateRange(GltfComponentType.UnsignedInt, GltfAccessorType.Scalar, 1, 0, 0, 0)),
                Is.EqualTo(new double[] { 305419896.0 }));
            Assert.That(
                decoder.DecodeFloatingValues(
                    new byte[] { 0x00, 0x00, 0x20, 0xc0 },
                    CreateRange(GltfComponentType.Float, GltfAccessorType.Scalar, 1, 0, 0, 0)),
                Is.EqualTo(new double[] { -2.5 }));
        });
    }

    [Test]
    public void DecodeSignedValues_UsesAccessorOffsetsAndInterleavedStride()
    {
        byte[] source = new byte[10];
        source[2] = 0x00;
        source[3] = 0x80;
        source[6] = 0xff;
        source[7] = 0x7f;
        AccessorByteRange range = CreateRange(
            GltfComponentType.SignedShort,
            GltfAccessorType.Scalar,
            2,
            1,
            1,
            4);

        int[] result = new AccessorDecoder().DecodeSignedValues(source, range);

        Assert.That(result, Is.EqualTo(new int[] { -32768, 32767 }));
    }

    [Test]
    public void DecodeSignedValues_ReadsSignedBytes()
    {
        int[] result = new AccessorDecoder().DecodeSignedValues(
            new byte[] { 0x80, 0x7f },
            CreateRange(
                GltfComponentType.SignedByte,
                GltfAccessorType.Vector2,
                1,
                0,
                0,
                0));

        Assert.That(result, Is.EqualTo(new int[] { -128, 127 }));
    }

    [Test]
    public void DecodeUnsignedValues_PreservesEveryUnsignedWidth()
    {
        AccessorDecoder decoder = new AccessorDecoder();

        Assert.Multiple(() =>
        {
            Assert.That(
                decoder.DecodeUnsignedValues(
                    new byte[] { 0xff },
                    CreateRange(GltfComponentType.UnsignedByte, GltfAccessorType.Scalar, 1, 0, 0, 0)),
                Is.EqualTo(new uint[] { 255U }));
            Assert.That(
                decoder.DecodeUnsignedValues(
                    new byte[] { 0xff, 0xff },
                    CreateRange(GltfComponentType.UnsignedShort, GltfAccessorType.Scalar, 1, 0, 0, 0)),
                Is.EqualTo(new uint[] { 65535U }));
            Assert.That(
                decoder.DecodeUnsignedValues(
                    new byte[] { 0xff, 0xff, 0xff, 0xff },
                    CreateRange(GltfComponentType.UnsignedInt, GltfAccessorType.Scalar, 1, 0, 0, 0)),
                Is.EqualTo(new uint[] { uint.MaxValue }));
        });
    }

    [Test]
    public void DecodeIndices_ReturnsUnsignedScalarValues()
    {
        uint[] result = new AccessorDecoder().DecodeIndices(
            new byte[] { 0x02, 0x01, 0xff, 0xff },
            CreateRange(
                GltfComponentType.UnsignedShort,
                GltfAccessorType.Scalar,
                2,
                0,
                0,
                0));

        Assert.That(result, Is.EqualTo(new uint[] { 258U, 65535U }));
    }

    [Test]
    public void DecodeIndices_RejectsSignedOrVectorData()
    {
        AccessorDecoder decoder = new AccessorDecoder();
        AccessorByteRange signed = CreateRange(
            GltfComponentType.SignedShort,
            GltfAccessorType.Scalar,
            1,
            0,
            0,
            0);
        AccessorByteRange vector = CreateRange(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Vector2,
            1,
            0,
            0,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => decoder.DecodeIndices(new byte[2], signed),
                Throws.ArgumentException.With.Message.StartsWith(
                    "Unsigned decoding requires"));
            Assert.That(
                () => decoder.DecodeIndices(new byte[4], vector),
                Throws.ArgumentException.With.Message.StartsWith(
                    "Index decoding requires"));
        });
    }

    [Test]
    public void DecodeMatrixValues_RemovesColumnPaddingInColumnMajorOrder()
    {
        byte[] source =
        {
            0xaa, 0xbb, 0xcc,
            1, 2, 3, 0xf1,
            4, 5, 6, 0xf2,
            7, 8, 9, 0xf3
        };
        AccessorByteRange range = CreateRange(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Matrix3,
            1,
            2,
            1,
            0);

        double[] result = new AccessorDecoder().DecodeMatrixValues(source, range);

        Assert.That(
            result,
            Is.EqualTo(new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
    }

    [Test]
    public void DecodeRawElements_PacksElementsAndPreservesMatrixPadding()
    {
        byte[] source =
        {
            0xee,
            1, 2, 3, 4, 5, 6, 7, 8,
            0xa1, 0xa2, 0xa3, 0xa4,
            11, 12, 13, 14, 15, 16, 17, 18
        };
        AccessorByteRange range = CreateRange(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Matrix2,
            2,
            1,
            0,
            12);

        byte[] result = new AccessorDecoder().DecodeRawElements(source, range);

        Assert.That(
            result,
            Is.EqualTo(new byte[]
            {
                1, 2, 3, 4, 5, 6, 7, 8,
                11, 12, 13, 14, 15, 16, 17, 18
            }));
    }

    [Test]
    public void DecodeValues_ReturnsOwnedEmptyArraysForZeroCount()
    {
        AccessorByteRange range = CreateRange(
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            0,
            5,
            0,
            0);
        byte[] source = new byte[5];
        AccessorDecoder decoder = new AccessorDecoder();

        double[] values = decoder.DecodeFloatingValues(source, range);
        byte[] raw = decoder.DecodeRawElements(source, range);

        Assert.Multiple(() =>
        {
            Assert.That(values, Is.Empty);
            Assert.That(raw, Is.Empty);
            Assert.That(values, Is.Not.SameAs(decoder.DecodeFloatingValues(source, range)));
            Assert.That(raw, Is.Not.SameAs(decoder.DecodeRawElements(source, range)));
        });
    }

    [Test]
    public void DecodeValues_RejectsTruncatedSourceBeforeReading()
    {
        AccessorByteRange range = CreateRange(
            GltfComponentType.Float,
            GltfAccessorType.Vector2,
            2,
            0,
            0,
            12);

        Assert.That(
            () => new AccessorDecoder().DecodeFloatingValues(new byte[19], range),
            Throws.ArgumentException.With.Message.StartsWith(
                "The accessor byte range is outside the source span."));
    }

    private static AccessorByteRange CreateRange(
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        int count,
        int bufferViewByteOffset,
        int accessorByteOffset,
        int byteStride)
    {
        GltfBufferView bufferView = new GltfBufferView();
        bufferView.Buffer = 0;
        bufferView.HasByteOffset = true;
        bufferView.ByteOffset = bufferViewByteOffset;
        bufferView.ByteLength = 1024;
        if (byteStride > 0)
        {
            bufferView.HasByteStride = true;
            bufferView.ByteStride = byteStride;
        }

        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.HasByteOffset = true;
        accessor.ByteOffset = accessorByteOffset;
        accessor.ComponentType = componentType;
        accessor.Count = count;
        accessor.Type = accessorType;
        AccessorLayout layout = new AccessorLayout();
        return layout.CalculateAccessorByteRange(
            accessor,
            layout.CalculateBufferViewRange(bufferView));
    }
}
