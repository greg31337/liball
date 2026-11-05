using System.Buffers.Binary;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfAccessorCodecCoverageTests
{
    private static readonly GltfComponentType[] ComponentTypes =
    [
        GltfComponentType.SignedByte,
        GltfComponentType.UnsignedByte,
        GltfComponentType.SignedShort,
        GltfComponentType.UnsignedShort,
        GltfComponentType.UnsignedInt,
        GltfComponentType.Float
    ];

    private static readonly GltfAccessorType[] AccessorTypes =
    [
        GltfAccessorType.Scalar,
        GltfAccessorType.Vector2,
        GltfAccessorType.Vector3,
        GltfAccessorType.Vector4,
        GltfAccessorType.Matrix2,
        GltfAccessorType.Matrix3,
        GltfAccessorType.Matrix4
    ];

    [Test]
    public void InterfaceMethods_RoundTripEverySupportedDenseComponentAndShape()
    {
        IGltfAccessorCodec codec = new GltfAccessorCodec();
        AccessorLayout layout = new AccessorLayout();

        Assert.Multiple(() =>
        {
            int componentTypeIndex = 0;
            while (componentTypeIndex < ComponentTypes.Length)
            {
                GltfComponentType componentType = ComponentTypes[componentTypeIndex];
                int accessorTypeIndex = 0;
                while (accessorTypeIndex < AccessorTypes.Length)
                {
                    GltfAccessorType accessorType = AccessorTypes[accessorTypeIndex];
                    if (componentType != GltfComponentType.UnsignedInt
                        || accessorType == GltfAccessorType.Scalar)
                    {
                        AccessorElementLayout element = layout.CalculateElementLayout(
                            componentType,
                            accessorType);
                        byte[] input = CreateRawElements(element, 2);
                        byte[] expected = input.ToArray();
                        GltfDocument document = new GltfDocument();
                        GltfAccessorWriteRequest write = new GltfAccessorWriteRequest();
                        write.Document = document;
                        write.ComponentType = componentType;
                        write.Type = accessorType;
                        write.RawBytes = input;
                        write.CalculateBounds = false;

                        GltfAccessorData created = codec.CreateDenseAccessor(write);
                        input[0] ^= 0xFF;
                        GltfAccessorReadRequest read = CreateReadRequest(
                            document,
                            created.Accessor);
                        GltfAccessorData raw = codec.ReadRawElements(read);

                        Assert.That(created.Status, Is.EqualTo(GltfOperationStatus.Success));
                        Assert.That(created.HasAccessor, Is.True);
                        Assert.That(created.ComponentType, Is.EqualTo(componentType));
                        Assert.That(created.Type, Is.EqualTo(accessorType));
                        Assert.That(created.Count, Is.EqualTo(2));
                        Assert.That(created.ComponentCount, Is.EqualTo(element.ComponentCount));
                        Assert.That(created.RawBytes, Is.EqualTo(expected));
                        Assert.That(created.RawBytes, Is.Not.SameAs(input));
                        Assert.That(raw.RawBytes, Is.EqualTo(expected));
                        Assert.That(raw.RawBytes, Is.Not.SameAs(document.Buffers[0].ResolvedBytes));
                        Assert.That(document.Accessors[0].Minimum, Is.Null);
                        Assert.That(document.Accessors[0].Maximum, Is.Null);

                        if (IsMatrix(accessorType))
                        {
                            GltfAccessorData matrices = codec.ReadMatrices(read);
                            Assert.That(matrices.Status, Is.EqualTo(GltfOperationStatus.Success));
                            Assert.That(matrices.MatrixValues[0], Is.EqualTo(1.0));
                            Assert.That(
                                matrices.MatrixValues[element.ComponentCount],
                                Is.EqualTo(2.0));
                        }
                        else
                        {
                            GltfAccessorData floating = codec.ReadFloatingValues(read);
                            Assert.That(floating.Status, Is.EqualTo(GltfOperationStatus.Success));
                            Assert.That(floating.FloatingValues[0], Is.EqualTo(1.0));
                            Assert.That(
                                floating.FloatingValues[element.ComponentCount],
                                Is.EqualTo(2.0));

                            if (componentType == GltfComponentType.SignedByte
                                || componentType == GltfComponentType.SignedShort)
                            {
                                GltfAccessorData signed = codec.ReadSignedValues(read);
                                Assert.That(signed.SignedValues[0], Is.EqualTo(1));
                                Assert.That(
                                    signed.SignedValues[element.ComponentCount],
                                    Is.EqualTo(2));
                            }
                            else if (componentType != GltfComponentType.Float)
                            {
                                GltfAccessorData unsigned = codec.ReadUnsignedValues(read);
                                Assert.That(unsigned.UnsignedValues[0], Is.EqualTo(1U));
                                Assert.That(
                                    unsigned.UnsignedValues[element.ComponentCount],
                                    Is.EqualTo(2U));
                                if (accessorType == GltfAccessorType.Scalar)
                                {
                                    GltfAccessorData indices = codec.ReadIndices(read);
                                    Assert.That(indices.Indices, Is.EqualTo(new uint[] { 1U, 2U }));
                                }
                            }
                        }
                    }

                    accessorTypeIndex++;
                }

                componentTypeIndex++;
            }
        });
    }

    [Test]
    public void CreateDenseAccessor_NormalizesExtremaAndRoundsMidpointsAwayFromZero()
    {
        IGltfAccessorCodec codec = new GltfAccessorCodec();
        GltfComponentType[] normalizedTypes =
        [
            GltfComponentType.SignedByte,
            GltfComponentType.UnsignedByte,
            GltfComponentType.SignedShort,
            GltfComponentType.UnsignedShort
        ];

        Assert.Multiple(() =>
        {
            int typeIndex = 0;
            while (typeIndex < normalizedTypes.Length)
            {
                GltfComponentType componentType = normalizedTypes[typeIndex];
                bool signedType = componentType == GltfComponentType.SignedByte
                    || componentType == GltfComponentType.SignedShort;
                int maximum = GetNormalizedMaximum(componentType);
                double[] values;
                if (signedType)
                {
                    values = [-1.0, -0.5 / maximum, 0.5 / maximum, 1.0];
                }
                else
                {
                    values = [0.0, 0.5 / maximum, 0.5, 1.0];
                }

                GltfDocument document = new GltfDocument();
                GltfAccessorWriteRequest write = new GltfAccessorWriteRequest();
                write.Document = document;
                write.ComponentType = componentType;
                write.Type = GltfAccessorType.Vector4;
                write.Normalized = true;
                write.FloatingValues = values;
                GltfAccessorData created = codec.CreateDenseAccessor(write);
                GltfAccessorReadRequest read = CreateReadRequest(document, created.Accessor);
                GltfAccessorData floating = codec.ReadFloatingValues(read);
                read.ApplyNormalization = false;
                GltfAccessorData storedAsFloating = codec.ReadFloatingValues(read);

                Assert.That(created.Status, Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(floating.Normalized, Is.True);
                Assert.That(floating.FloatingValues[0], Is.EqualTo(values[0]));
                Assert.That(floating.FloatingValues[3], Is.EqualTo(1.0));
                Assert.That(storedAsFloating.Normalized, Is.False);
                if (signedType)
                {
                    GltfAccessorData stored = codec.ReadSignedValues(read);
                    Assert.That(
                        stored.SignedValues,
                        Is.EqualTo(new int[] { -maximum, -1, 1, maximum }));
                    Assert.That(
                        storedAsFloating.FloatingValues,
                        Is.EqualTo(new double[] { -maximum, -1.0, 1.0, maximum }));
                    Assert.That(
                        document.Accessors[0].Minimum,
                        Is.EqualTo(new double[] { -maximum, -1.0, 1.0, maximum }));
                    Assert.That(
                        document.Accessors[0].Maximum,
                        Is.EqualTo(new double[] { -maximum, -1.0, 1.0, maximum }));
                }
                else
                {
                    GltfAccessorData stored = codec.ReadUnsignedValues(read);
                    Assert.That(
                        stored.UnsignedValues,
                        Is.EqualTo(new uint[]
                        {
                            0U,
                            1U,
                            (uint)((maximum + 1) / 2),
                            (uint)maximum
                        }));
                    Assert.That(
                        storedAsFloating.FloatingValues,
                        Is.EqualTo(new double[]
                        {
                            0.0,
                            1.0,
                            (maximum + 1) / 2.0,
                            maximum
                        }));
                    Assert.That(
                        document.Accessors[0].Minimum,
                        Is.EqualTo(new double[]
                        {
                            0.0,
                            1.0,
                            (maximum + 1) / 2.0,
                            maximum
                        }));
                    Assert.That(
                        document.Accessors[0].Maximum,
                        Is.EqualTo(document.Accessors[0].Minimum));
                }

                typeIndex++;
            }
        });
    }

    [Test]
    public void ReadMethods_UseCombinedOffsetsAndInterleavedStride()
    {
        byte[] bytes = new byte[28];
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(8, 2), -2);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(10, 2), 3);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(20, 2), 4);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(22, 2), -5);
        GltfDocument document = CreateInterleavedDocument(bytes, 4);
        IGltfAccessorCodec codec = new GltfAccessorCodec();
        GltfAccessorReadRequest read = CreateReadRequest(document, 0);

        GltfAccessorData signed = codec.ReadSignedValues(read);
        GltfAccessorData floating = codec.ReadFloatingValues(read);
        GltfAccessorData raw = codec.ReadRawElements(read);
        document.Accessors[0].ByteOffset = 3;
        GltfAccessorData misaligned = codec.ReadRawElements(read);

        Assert.Multiple(() =>
        {
            Assert.That(signed.SignedValues, Is.EqualTo(new int[] { -2, 3, 4, -5 }));
            Assert.That(floating.FloatingValues, Is.EqualTo(new double[] { -2.0, 3.0, 4.0, -5.0 }));
            Assert.That(
                raw.RawBytes,
                Is.EqualTo(new byte[] { 0xFE, 0xFF, 3, 0, 4, 0, 0xFB, 0xFF }));
            AssertFailure(
                misaligned,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.AccessorOffsetAlignment);
        });
    }

    [Test]
    public void ReadMethods_RejectElementCountAndByteRangeOverflowWithoutAllocating()
    {
        GltfDocument componentOverflow = new GltfDocument();
        GltfAccessor huge = new GltfAccessor();
        huge.ComponentType = GltfComponentType.Float;
        huge.Type = GltfAccessorType.Vector4;
        huge.Count = int.MaxValue;
        componentOverflow.Accessors.Add(huge);
        GltfAccessorReadRequest componentRequest = CreateReadRequest(componentOverflow, 0);
        componentRequest.MaximumElementCount = int.MaxValue;

        GltfDocument byteRangeOverflow = CreateInterleavedDocument(new byte[28], 4);
        byteRangeOverflow.BufferViews[0].ByteOffset = int.MaxValue - 1;
        byteRangeOverflow.BufferViews[0].ByteLength = 4;
        byteRangeOverflow.BufferViews[0].HasByteStride = false;
        byteRangeOverflow.Accessors[0].ByteOffset = 0;
        byteRangeOverflow.Accessors[0].Count = 1;

        IGltfAccessorCodec codec = new GltfAccessorCodec();
        GltfAccessorData componentResult = codec.ReadFloatingValues(componentRequest);
        GltfAccessorData byteRangeResult = codec.ReadRawElements(
            CreateReadRequest(byteRangeOverflow, 0));

        Assert.Multiple(() =>
        {
            AssertFailure(
                componentResult,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.AccessorTooLong);
            AssertFailure(
                byteRangeResult,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.BufferViewTooLong);
        });
    }

    private static byte[] CreateRawElements(AccessorElementLayout element, int count)
    {
        byte[] result = new byte[element.ElementByteLength * count];
        Array.Fill(result, (byte)0xA5);
        int elementIndex = 0;
        while (elementIndex < count)
        {
            int componentIndex = 0;
            while (componentIndex < element.ComponentCount)
            {
                int offset = elementIndex * element.ElementByteLength
                    + element.ComponentByteOffsets[componentIndex];
                WriteComponent(
                    result,
                    offset,
                    element.ComponentType,
                    componentIndex == 0 ? elementIndex + 1 : 0);
                componentIndex++;
            }

            elementIndex++;
        }

        return result;
    }

    private static void WriteComponent(
        byte[] bytes,
        int offset,
        GltfComponentType componentType,
        int value)
    {
        switch (componentType)
        {
            case GltfComponentType.SignedByte:
            case GltfComponentType.UnsignedByte:
                bytes[offset] = (byte)value;
                return;
            case GltfComponentType.SignedShort:
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(offset, 2), (short)value);
                return;
            case GltfComponentType.UnsignedShort:
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset, 2), (ushort)value);
                return;
            case GltfComponentType.UnsignedInt:
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), (uint)value);
                return;
            case GltfComponentType.Float:
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset, 4), value);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(componentType));
        }
    }

    private static GltfAccessorReadRequest CreateReadRequest(
        GltfDocument document,
        int accessor)
    {
        GltfAccessorReadRequest result = new GltfAccessorReadRequest();
        result.Document = document;
        result.Accessor = accessor;
        return result;
    }

    private static GltfDocument CreateInterleavedDocument(byte[] bytes, int accessorOffset)
    {
        GltfDocument result = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = bytes.Length;
        buffer.Resolved = true;
        buffer.ResolvedBytes = bytes;
        result.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.HasByteOffset = true;
        view.ByteOffset = 4;
        view.ByteLength = 24;
        view.HasByteStride = true;
        view.ByteStride = 12;
        result.BufferViews.Add(view);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.HasByteOffset = true;
        accessor.ByteOffset = accessorOffset;
        accessor.ComponentType = GltfComponentType.SignedShort;
        accessor.Count = 2;
        accessor.Type = GltfAccessorType.Vector2;
        result.Accessors.Add(accessor);
        return result;
    }

    private static int GetNormalizedMaximum(GltfComponentType componentType)
    {
        if (componentType == GltfComponentType.SignedByte)
        {
            return 127;
        }

        if (componentType == GltfComponentType.UnsignedByte)
        {
            return 255;
        }

        if (componentType == GltfComponentType.SignedShort)
        {
            return 32767;
        }

        return 65535;
    }

    private static bool IsMatrix(GltfAccessorType type)
    {
        return type == GltfAccessorType.Matrix2
            || type == GltfAccessorType.Matrix3
            || type == GltfAccessorType.Matrix4;
    }

    private static void AssertFailure(
        GltfAccessorData result,
        GltfOperationStatus status,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
    }
}
