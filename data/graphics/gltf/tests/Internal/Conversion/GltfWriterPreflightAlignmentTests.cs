namespace gltf.Tests.Internal.Conversion;

[TestFixture]
public class GltfWriterPreflightAlignmentTests
{
    [Test]
    public void ValidateDocument_RejectsAccessorOffsetMisalignedForItsComponent()
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Vector2,
            0,
            1);

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Accessor byte offset /accessors/0/byteOffset value 1 is not aligned to component byte length 2."));
    }

    [Test]
    public void ValidateDocument_RejectsMatrixElementWithMisalignedAbsoluteOffset()
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Matrix3,
            2,
            0);

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Accessor element /accessors/0 starts at absolute byte offset 2, which is not aligned to 4 bytes."));
    }

    [Test]
    public void ValidateDocument_AcceptsIndependentlyAlignedMatrixElement()
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Matrix3,
            2,
            2);

        Assert.DoesNotThrow(() => new GltfWriterPreflight().ValidateDocument(document));
    }

    [Test]
    public void ValidateDocument_RejectsBufferViewExtentBeyondItsBuffer()
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            60,
            0);
        document.BufferViews[0].ByteLength = 8;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Buffer view byte range /bufferViews/0 ends at 68, beyond buffer 0 byteLength 64."));
    }

    [Test]
    public void ValidateDocument_RejectsTightAccessorFinalExtentBeyondItsView()
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            0,
            24);

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Accessor byte range /accessors/0 ends at 36, beyond buffer view 0 byteLength 32."));
    }

    [Test]
    public void ValidateDocument_UsesStrideForInterleavedFinalExtent()
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.Float,
            GltfAccessorType.Vector2,
            0,
            0);
        document.Accessors[0].Count = 3;
        document.BufferViews[0].HasByteStride = true;
        document.BufferViews[0].ByteStride = 16;
        document.BufferViews[0].HasTarget = true;
        document.BufferViews[0].Target = GltfBufferViewTarget.ArrayBuffer;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Accessor byte range /accessors/0 ends at 40, beyond buffer view 0 byteLength 32."));
    }

    [Test]
    public void ValidateDocument_RejectsStrideSmallerThanElement()
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            0,
            0);
        document.BufferViews[0].HasByteStride = true;
        document.BufferViews[0].ByteStride = 8;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Buffer view /bufferViews/0 byteStride 8 is smaller than accessor element byte length 12."));
    }

    [TestCase(2, "Buffer view /bufferViews/0 byteStride 2 is outside the supported range 4 through 252.")]
    [TestCase(256, "Buffer view /bufferViews/0 byteStride 256 is outside the supported range 4 through 252.")]
    [TestCase(6, "Buffer view /bufferViews/0 byteStride 6 is not a multiple of 4.")]
    public void ValidateDocument_RejectsInvalidByteStride(int byteStride, string message)
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            0,
            0);
        document.BufferViews[0].HasByteStride = true;
        document.BufferViews[0].ByteStride = byteStride;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(message));
    }

    [Test]
    public void ValidateDocument_RejectsStrideOnElementArrayTarget()
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Scalar,
            0,
            0);
        document.BufferViews[0].HasByteStride = true;
        document.BufferViews[0].ByteStride = 4;
        document.BufferViews[0].HasTarget = true;
        document.BufferViews[0].Target = GltfBufferViewTarget.ElementArrayBuffer;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Element array buffer view /bufferViews/0 must not define byteStride."));
    }

    [Test]
    public void ValidateDocument_RejectsUnknownBufferViewTarget()
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            0,
            0);
        document.BufferViews[0].HasTarget = true;
        document.BufferViews[0].Target = (GltfBufferViewTarget)1;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Unknown buffer view target 1 at /bufferViews/0/target."));
    }

    [Test]
    public void ValidateDocument_RejectsMisalignedArrayBufferAccessor()
    {
        GltfDocument document = CreateDocument(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Vector2,
            2,
            0);
        document.BufferViews[0].HasTarget = true;
        document.BufferViews[0].Target = GltfBufferViewTarget.ArrayBuffer;

        Assert.That(
            () => new GltfWriterPreflight().ValidateDocument(document),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Vertex accessor element /accessors/0 starts at absolute byte offset 2, which is not aligned to 4 bytes."));
    }

    [Test]
    public void ValidateDocument_AllowsZeroCountAtViewEndButNotBeyondIt()
    {
        GltfDocument atEnd = CreateDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            0,
            32);
        atEnd.Accessors[0].Count = 0;
        GltfDocument beyondEnd = CreateDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            0,
            33);
        beyondEnd.Accessors[0].Count = 0;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => new GltfWriterPreflight().ValidateDocument(atEnd));
            Assert.That(
                () => new GltfWriterPreflight().ValidateDocument(beyondEnd),
                Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                    "Accessor byte range /accessors/0 ends at 33, beyond buffer view 0 byteLength 32."));
        });
    }

    private static GltfDocument CreateDocument(
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        int bufferViewByteOffset,
        int accessorByteOffset)
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 64;
        document.Buffers.Add(buffer);
        GltfBufferView bufferView = new GltfBufferView();
        bufferView.Buffer = 0;
        bufferView.HasByteOffset = true;
        bufferView.ByteOffset = bufferViewByteOffset;
        bufferView.ByteLength = 32;
        document.BufferViews.Add(bufferView);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.HasByteOffset = true;
        accessor.ByteOffset = accessorByteOffset;
        accessor.ComponentType = componentType;
        accessor.Count = 1;
        accessor.Type = accessorType;
        document.Accessors.Add(accessor);
        return document;
    }
}
