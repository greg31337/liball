namespace gltf.Tests.Internal.Binary;

[TestFixture]
public class AccessorByteRangeTests
{
    [Test]
    public void CalculateAccessorByteRange_CalculatesTightlyPackedFinalExtent()
    {
        GltfBufferView bufferView = new GltfBufferView();
        bufferView.Buffer = 2;
        bufferView.HasByteOffset = true;
        bufferView.ByteOffset = 8;
        bufferView.ByteLength = 100;
        GltfAccessor accessor = CreateAccessor(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Vector3,
            3,
            4);
        AccessorLayout calculator = new AccessorLayout();
        BufferViewByteRange viewRange = calculator.CalculateBufferViewRange(bufferView);

        AccessorByteRange result = calculator.CalculateAccessorByteRange(
            accessor,
            viewRange);

        Assert.Multiple(() =>
        {
            Assert.That(viewRange.Buffer, Is.EqualTo(2));
            Assert.That(viewRange.ByteEnd, Is.EqualTo(108));
            Assert.That(result.ElementByteStride, Is.EqualTo(6));
            Assert.That(result.Interleaved, Is.False);
            Assert.That(result.TightlyPacked, Is.True);
            Assert.That(result.HasElements, Is.True);
            Assert.That(result.AccessorByteLength, Is.EqualTo(18));
            Assert.That(result.AccessorByteEnd, Is.EqualTo(22));
            Assert.That(result.FirstElementBufferByteOffset, Is.EqualTo(12));
            Assert.That(result.FinalElementBufferByteOffset, Is.EqualTo(24));
            Assert.That(result.BufferByteEnd, Is.EqualTo(30));
        });
    }

    [Test]
    public void CalculateAccessorByteRange_CalculatesInterleavedFinalExtent()
    {
        GltfBufferView bufferView = new GltfBufferView();
        bufferView.HasByteOffset = true;
        bufferView.ByteOffset = 16;
        bufferView.ByteLength = 80;
        bufferView.HasByteStride = true;
        bufferView.ByteStride = 20;
        bufferView.HasTarget = true;
        bufferView.Target = GltfBufferViewTarget.ArrayBuffer;
        GltfAccessor accessor = CreateAccessor(
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            3,
            4);
        AccessorLayout calculator = new AccessorLayout();

        AccessorByteRange result = calculator.CalculateAccessorByteRange(
            accessor,
            calculator.CalculateBufferViewRange(bufferView));

        Assert.Multiple(() =>
        {
            Assert.That(result.ElementByteStride, Is.EqualTo(20));
            Assert.That(result.Interleaved, Is.True);
            Assert.That(result.TightlyPacked, Is.False);
            Assert.That(result.AccessorByteLength, Is.EqualTo(52));
            Assert.That(result.AccessorByteEnd, Is.EqualTo(56));
            Assert.That(result.FirstElementBufferByteOffset, Is.EqualTo(20));
            Assert.That(result.FinalElementBufferByteOffset, Is.EqualTo(60));
            Assert.That(result.BufferByteEnd, Is.EqualTo(72));
        });
    }

    [Test]
    public void CalculateAccessorByteRange_HandlesZeroCountWithoutStrideUnderflow()
    {
        GltfBufferView bufferView = new GltfBufferView();
        bufferView.HasByteOffset = true;
        bufferView.ByteOffset = 10;
        bufferView.ByteLength = 20;
        bufferView.HasByteStride = true;
        bufferView.ByteStride = 16;
        GltfAccessor accessor = CreateAccessor(
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            0,
            7);
        AccessorLayout calculator = new AccessorLayout();

        AccessorByteRange result = calculator.CalculateAccessorByteRange(
            accessor,
            calculator.CalculateBufferViewRange(bufferView));

        Assert.Multiple(() =>
        {
            Assert.That(result.HasElements, Is.False);
            Assert.That(result.AccessorByteLength, Is.Zero);
            Assert.That(result.AccessorByteEnd, Is.EqualTo(7));
            Assert.That(result.FirstElementBufferByteOffset, Is.EqualTo(17));
            Assert.That(result.FinalElementBufferByteOffset, Is.EqualTo(17));
            Assert.That(result.BufferByteEnd, Is.EqualTo(17));
        });
    }

    [Test]
    public void CalculateBufferViewRange_UsesLongFinalExtent()
    {
        GltfBufferView bufferView = new GltfBufferView();
        bufferView.HasByteOffset = true;
        bufferView.ByteOffset = int.MaxValue;
        bufferView.ByteLength = 4;

        BufferViewByteRange result = new AccessorLayout().CalculateBufferViewRange(bufferView);

        Assert.That(result.ByteEnd, Is.EqualTo((long)int.MaxValue + 4L));
    }

    private static GltfAccessor CreateAccessor(
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        int count,
        int byteOffset)
    {
        GltfAccessor result = new GltfAccessor();
        result.HasByteOffset = true;
        result.ByteOffset = byteOffset;
        result.ComponentType = componentType;
        result.Count = count;
        result.Type = accessorType;
        return result;
    }
}
