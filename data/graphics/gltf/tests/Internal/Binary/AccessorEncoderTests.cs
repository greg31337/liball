using System.Buffers.Binary;

namespace gltf.Tests.Internal.Binary;

[TestFixture]
public class AccessorEncoderTests
{
    [Test]
    public void EncodeFloatingAccessor_WritesFiniteSinglePrecisionValues()
    {
        AccessorEncodingResult result = new AccessorEncoder().EncodeFloatingAccessor(
            new double[] { 1.5, -2.25, 3.0, 4.5 },
            GltfComponentType.Float,
            GltfAccessorType.Vector2,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.ComponentType, Is.EqualTo(GltfComponentType.Float));
            Assert.That(result.Type, Is.EqualTo(GltfAccessorType.Vector2));
            Assert.That(result.Normalized, Is.False);
            Assert.That(result.IndexData, Is.False);
            Assert.That(ReadFloat(result.Bytes, 0), Is.EqualTo(1.5F));
            Assert.That(ReadFloat(result.Bytes, 4), Is.EqualTo(-2.25F));
            Assert.That(ReadFloat(result.Bytes, 8), Is.EqualTo(3.0F));
            Assert.That(ReadFloat(result.Bytes, 12), Is.EqualTo(4.5F));
        });
    }

    [Test]
    public void EncodeFloatingAccessor_QuantizesNormalizedIntegerValues()
    {
        AccessorEncoder encoder = new AccessorEncoder();

        AccessorEncodingResult signed = encoder.EncodeFloatingAccessor(
            new double[] { -1.0, -0.5, 0.0, 0.5, 1.0 },
            GltfComponentType.SignedByte,
            GltfAccessorType.Scalar,
            true);
        AccessorEncodingResult unsigned = encoder.EncodeFloatingAccessor(
            new double[] { 0.0, 0.5, 1.0 },
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Scalar,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Bytes, Is.EqualTo(new byte[] { 129, 192, 0, 64, 127 }));
            Assert.That(ReadUnsignedShort(unsigned.Bytes, 0), Is.EqualTo(0));
            Assert.That(ReadUnsignedShort(unsigned.Bytes, 2), Is.EqualTo(32768));
            Assert.That(ReadUnsignedShort(unsigned.Bytes, 4), Is.EqualTo(65535));
            Assert.That(unsigned.Normalized, Is.True);
        });
    }

    [Test]
    public void EncodeSignedAccessor_WritesRawSignedComponentsAndChecksRange()
    {
        AccessorEncoder encoder = new AccessorEncoder();
        AccessorEncodingResult result = encoder.EncodeSignedAccessor(
            new int[] { short.MinValue, -1, 0, short.MaxValue },
            GltfComponentType.SignedShort,
            GltfAccessorType.Vector2,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(ReadSignedShort(result.Bytes, 0), Is.EqualTo(short.MinValue));
            Assert.That(ReadSignedShort(result.Bytes, 2), Is.EqualTo(-1));
            Assert.That(ReadSignedShort(result.Bytes, 4), Is.EqualTo(0));
            Assert.That(ReadSignedShort(result.Bytes, 6), Is.EqualTo(short.MaxValue));
            Assert.That(result.Normalized, Is.True);
            Assert.That(
                () => encoder.EncodeSignedAccessor(
                    new int[] { 128 },
                    GltfComponentType.SignedByte,
                    GltfAccessorType.Scalar,
                    false),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void EncodeUnsignedAccessor_WritesAttributeWidthsAndRejectsUnsignedInt()
    {
        AccessorEncoder encoder = new AccessorEncoder();
        AccessorEncodingResult result = encoder.EncodeUnsignedAccessor(
            new uint[] { 0U, 255U, 256U, 65535U },
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Vector2,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Bytes,
                Is.EqualTo(new byte[] { 0, 0, 255, 0, 0, 1, 255, 255 }));
            Assert.That(
                () => encoder.EncodeUnsignedAccessor(
                    new uint[] { 1U },
                    GltfComponentType.UnsignedInt,
                    GltfAccessorType.Scalar,
                    false),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("componentType"));
        });
    }

    [Test]
    public void EncodeIndexAccessor_AllowsEveryUnsignedWidthAndMarksIndexData()
    {
        AccessorEncodingResult result = new AccessorEncoder().EncodeIndexAccessor(
            new uint[] { 0U, 65536U, uint.MaxValue },
            GltfComponentType.UnsignedInt);

        Assert.Multiple(() =>
        {
            Assert.That(result.Type, Is.EqualTo(GltfAccessorType.Scalar));
            Assert.That(result.IndexData, Is.True);
            Assert.That(ReadUnsignedInt(result.Bytes, 0), Is.EqualTo(0U));
            Assert.That(ReadUnsignedInt(result.Bytes, 4), Is.EqualTo(65536U));
            Assert.That(ReadUnsignedInt(result.Bytes, 8), Is.EqualTo(uint.MaxValue));
        });
    }

    [Test]
    public void EncodeMatrixAccessor_InsertsRequiredColumnPadding()
    {
        AccessorEncodingResult result = new AccessorEncoder().EncodeMatrixAccessor(
            new double[]
            {
                0.0,
                1.0 / 255.0,
                2.0 / 255.0,
                3.0 / 255.0,
                4.0 / 255.0,
                5.0 / 255.0,
                6.0 / 255.0,
                7.0 / 255.0,
                8.0 / 255.0
            },
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Matrix3,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(
                result.Bytes,
                Is.EqualTo(new byte[]
                {
                    0, 1, 2, 0,
                    3, 4, 5, 0,
                    6, 7, 8, 0
                }));
        });
    }

    [Test]
    public void EncodeRawAccessor_PreservesOwnedCompleteElements()
    {
        byte[] source =
        {
            1, 2, 0xaa, 0xbb, 3, 4, 0xcc, 0xdd,
            5, 6, 0xee, 0xff, 7, 8, 0x11, 0x22
        };
        AccessorEncodingResult result = new AccessorEncoder().EncodeRawAccessor(
            source,
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Matrix2,
            false,
            false);
        source[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Bytes[0], Is.EqualTo(1));
            Assert.That(result.Bytes, Is.Not.SameAs(source));
        });
    }

    [Test]
    public void CreateAccessorStorage_AppendsLinkedObjectsAndStoredBounds()
    {
        GltfDocument document = CreateDocumentWithExistingStorage();
        AccessorEncoder encoder = new AccessorEncoder();
        AccessorEncodingResult encoding = encoder.EncodeFloatingAccessor(
            new double[] { -1.0, 4.0, 2.0, 3.0, -2.0, 8.0 },
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            false);

        AccessorCreationResult result = encoder.CreateAccessorStorage(
            document,
            encoding,
            true);
        encoding.Bytes[0] = 99;

        GltfAccessor accessor = document.Accessors[result.Accessor];
        GltfBufferView view = document.BufferViews[result.BufferView];
        GltfBuffer buffer = document.Buffers[result.Buffer];
        Assert.Multiple(() =>
        {
            Assert.That(result.Buffer, Is.EqualTo(1));
            Assert.That(result.BufferView, Is.EqualTo(1));
            Assert.That(result.Accessor, Is.EqualTo(1));
            Assert.That(view.Buffer, Is.EqualTo(result.Buffer));
            Assert.That(accessor.BufferView, Is.EqualTo(result.BufferView));
            Assert.That(accessor.Count, Is.EqualTo(2));
            Assert.That(accessor.Minimum, Is.EqualTo(new double[] { -1.0, -2.0, 2.0 }));
            Assert.That(accessor.Maximum, Is.EqualTo(new double[] { 3.0, 4.0, 8.0 }));
            Assert.That(buffer.Resolved, Is.True);
            Assert.That(buffer.ByteLength, Is.EqualTo(24));
            Assert.That(buffer.ResolvedBytes[0], Is.Not.EqualTo(99));
            Assert.DoesNotThrow(() => new GltfWriterPreflight().ValidateDocument(document));
        });
    }

    [Test]
    public void CreateAccessorStorage_AssignsTheElementArrayTargetForIndices()
    {
        GltfDocument document = new GltfDocument();
        AccessorEncoder encoder = new AccessorEncoder();
        AccessorEncodingResult encoding = encoder.EncodeIndexAccessor(
            new uint[] { 0U, 2U, 1U },
            GltfComponentType.UnsignedByte);

        AccessorCreationResult result = encoder.CreateAccessorStorage(
            document,
            encoding,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(document.BufferViews[result.BufferView].HasTarget, Is.True);
            Assert.That(
                document.BufferViews[result.BufferView].Target,
                Is.EqualTo(GltfBufferViewTarget.ElementArrayBuffer));
            Assert.That(document.Accessors[result.Accessor].Minimum, Is.EqualTo(new double[] { 0.0 }));
            Assert.That(document.Accessors[result.Accessor].Maximum, Is.EqualTo(new double[] { 2.0 }));
        });
    }

    [Test]
    public void EncodeTypedAccessors_RejectInvalidShapesCountsAndValues()
    {
        AccessorEncoder encoder = new AccessorEncoder();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => encoder.EncodeFloatingAccessor(
                    new double[] { 1.0 },
                    GltfComponentType.Float,
                    GltfAccessorType.Vector2,
                    false),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("values"));
            Assert.That(
                () => encoder.EncodeFloatingAccessor(
                    new double[] { double.NaN },
                    GltfComponentType.Float,
                    GltfAccessorType.Scalar,
                    false),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => encoder.EncodeFloatingAccessor(
                    new double[] { 1.0 },
                    GltfComponentType.Float,
                    GltfAccessorType.Scalar,
                    true),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("normalized"));
            Assert.That(
                () => encoder.EncodeSignedAccessor(
                    new int[] { 1, 2, 3, 4 },
                    GltfComponentType.SignedShort,
                    GltfAccessorType.Matrix2,
                    false),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("accessorType"));
            Assert.That(
                () => encoder.EncodeMatrixAccessor(
                    new double[] { 1.0 },
                    GltfComponentType.Float,
                    GltfAccessorType.Scalar,
                    false),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("accessorType"));
            Assert.That(
                () => encoder.EncodeRawAccessor(
                    new byte[] { 1, 2, 3 },
                    GltfComponentType.UnsignedShort,
                    GltfAccessorType.Vector2,
                    false,
                    false),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("rawBytes"));
            Assert.That(
                () => encoder.EncodeRawAccessor(
                    new byte[] { 1 },
                    GltfComponentType.UnsignedByte,
                    GltfAccessorType.Scalar,
                    true,
                    true),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("indexData"));
        });
    }

    [Test]
    public void CreateAccessorStorage_RejectsInvalidEncodingWithoutMutation()
    {
        GltfDocument document = new GltfDocument();
        AccessorEncodingResult encoding = new AccessorEncodingResult();
        encoding.Bytes = new byte[] { 1 };
        encoding.ComponentType = GltfComponentType.Float;
        encoding.Type = GltfAccessorType.Scalar;
        encoding.Count = 1;

        Assert.That(
            () => new AccessorEncoder().CreateAccessorStorage(document, encoding, true),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("encoding"));
        Assert.Multiple(() =>
        {
            Assert.That(document.Buffers, Is.Empty);
            Assert.That(document.BufferViews, Is.Empty);
            Assert.That(document.Accessors, Is.Empty);
        });
    }

    private static GltfDocument CreateDocumentWithExistingStorage()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 1;
        buffer.Resolved = true;
        buffer.ResolvedBytes = new byte[] { 0 };
        document.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.ByteLength = 1;
        document.BufferViews.Add(view);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.ComponentType = GltfComponentType.UnsignedByte;
        accessor.Count = 1;
        accessor.Type = GltfAccessorType.Scalar;
        document.Accessors.Add(accessor);
        return document;
    }

    private static float ReadFloat(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(offset, 4));
    }

    private static short ReadSignedShort(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(offset, 2));
    }

    private static ushort ReadUnsignedShort(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2));
    }

    private static uint ReadUnsignedInt(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4));
    }
}
