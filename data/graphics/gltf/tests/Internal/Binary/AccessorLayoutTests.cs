namespace gltf.Tests.Internal.Binary;

[TestFixture]
public class AccessorLayoutTests
{
    [TestCase(GltfComponentType.SignedByte, 1)]
    [TestCase(GltfComponentType.UnsignedByte, 1)]
    [TestCase(GltfComponentType.SignedShort, 2)]
    [TestCase(GltfComponentType.UnsignedShort, 2)]
    [TestCase(GltfComponentType.UnsignedInt, 4)]
    [TestCase(GltfComponentType.Float, 4)]
    public void GetComponentByteLength_ReturnsEveryCoreComponentSize(
        GltfComponentType componentType,
        int expected)
    {
        Assert.That(
            new AccessorLayout().GetComponentByteLength(componentType),
            Is.EqualTo(expected));
    }

    [TestCase(GltfAccessorType.Scalar, 1, 1, 1)]
    [TestCase(GltfAccessorType.Vector2, 2, 2, 1)]
    [TestCase(GltfAccessorType.Vector3, 3, 3, 1)]
    [TestCase(GltfAccessorType.Vector4, 4, 4, 1)]
    [TestCase(GltfAccessorType.Matrix2, 4, 2, 2)]
    [TestCase(GltfAccessorType.Matrix3, 9, 3, 3)]
    [TestCase(GltfAccessorType.Matrix4, 16, 4, 4)]
    public void GetComponentCount_ReturnsEveryCoreElementShape(
        GltfAccessorType accessorType,
        int expectedComponentCount,
        int expectedRowCount,
        int expectedColumnCount)
    {
        AccessorLayout calculator = new AccessorLayout();

        Assert.Multiple(() =>
        {
            Assert.That(calculator.GetComponentCount(accessorType),
                Is.EqualTo(expectedComponentCount));
            Assert.That(calculator.GetRowCount(accessorType),
                Is.EqualTo(expectedRowCount));
            Assert.That(calculator.GetColumnCount(accessorType),
                Is.EqualTo(expectedColumnCount));
        });
    }

    [Test]
    public void CalculateElementLayout_CalculatesEveryComponentAndShapeCombination()
    {
        GltfComponentType[] componentTypes =
        [
            GltfComponentType.SignedByte,
            GltfComponentType.UnsignedByte,
            GltfComponentType.SignedShort,
            GltfComponentType.UnsignedShort,
            GltfComponentType.UnsignedInt,
            GltfComponentType.Float
        ];
        GltfAccessorType[] accessorTypes =
        [
            GltfAccessorType.Scalar,
            GltfAccessorType.Vector2,
            GltfAccessorType.Vector3,
            GltfAccessorType.Vector4,
            GltfAccessorType.Matrix2,
            GltfAccessorType.Matrix3,
            GltfAccessorType.Matrix4
        ];
        AccessorLayout calculator = new AccessorLayout();

        Assert.Multiple(() =>
        {
            int componentTypeIndex = 0;
            while (componentTypeIndex < componentTypes.Length)
            {
                int accessorTypeIndex = 0;
                while (accessorTypeIndex < accessorTypes.Length)
                {
                    GltfComponentType componentType = componentTypes[componentTypeIndex];
                    GltfAccessorType accessorType = accessorTypes[accessorTypeIndex];
                    AccessorElementLayout result = calculator.CalculateElementLayout(
                        componentType,
                        accessorType);
                    int componentByteLength = calculator.GetComponentByteLength(componentType);
                    int componentCount = calculator.GetComponentCount(accessorType);

                    Assert.That(result.ComponentType, Is.EqualTo(componentType));
                    Assert.That(result.Type, Is.EqualTo(accessorType));
                    Assert.That(result.ComponentByteLength, Is.EqualTo(componentByteLength));
                    Assert.That(result.ComponentCount, Is.EqualTo(componentCount));
                    Assert.That(result.LogicalColumnByteLength,
                        Is.EqualTo(result.RowCount * componentByteLength));
                    Assert.That(result.LogicalElementByteLength,
                        Is.EqualTo(componentCount * componentByteLength));
                    Assert.That(result.ComponentByteOffsets, Has.Count.EqualTo(componentCount));
                    int componentIndex = 0;
                    while (componentIndex < componentCount)
                    {
                        int columnIndex = componentIndex / result.RowCount;
                        int rowIndex = componentIndex % result.RowCount;
                        Assert.That(result.ComponentByteOffsets[componentIndex],
                            Is.EqualTo(
                                columnIndex * result.ColumnByteLength
                                + rowIndex * componentByteLength));
                        componentIndex++;
                    }

                    accessorTypeIndex++;
                }

                componentTypeIndex++;
            }
        });
    }

    [Test]
    public void CalculateElementLayout_PadsMatrixColumnsToFourByteBoundaries()
    {
        AccessorLayout calculator = new AccessorLayout();
        AccessorElementLayout matrix2Byte = calculator.CalculateElementLayout(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Matrix2);
        AccessorElementLayout matrix3Byte = calculator.CalculateElementLayout(
            GltfComponentType.SignedByte,
            GltfAccessorType.Matrix3);
        AccessorElementLayout matrix3Short = calculator.CalculateElementLayout(
            GltfComponentType.UnsignedShort,
            GltfAccessorType.Matrix3);
        AccessorElementLayout matrix2Short = calculator.CalculateElementLayout(
            GltfComponentType.SignedShort,
            GltfAccessorType.Matrix2);

        Assert.Multiple(() =>
        {
            Assert.That(matrix2Byte.ColumnByteLength, Is.EqualTo(4));
            Assert.That(matrix2Byte.LogicalElementByteLength, Is.EqualTo(4));
            Assert.That(matrix2Byte.ElementByteLength, Is.EqualTo(8));
            Assert.That(matrix2Byte.ComponentByteOffsets, Is.EqualTo(new[] { 0, 1, 4, 5 }));
            Assert.That(matrix2Byte.HasColumnPadding, Is.True);
            Assert.That(matrix3Byte.ColumnByteLength, Is.EqualTo(4));
            Assert.That(matrix3Byte.LogicalElementByteLength, Is.EqualTo(9));
            Assert.That(matrix3Byte.ElementByteLength, Is.EqualTo(12));
            Assert.That(matrix3Byte.ComponentByteOffsets,
                Is.EqualTo(new[] { 0, 1, 2, 4, 5, 6, 8, 9, 10 }));
            Assert.That(matrix3Short.ColumnByteLength, Is.EqualTo(8));
            Assert.That(matrix3Short.LogicalElementByteLength, Is.EqualTo(18));
            Assert.That(matrix3Short.ElementByteLength, Is.EqualTo(24));
            Assert.That(matrix3Short.ComponentByteOffsets,
                Is.EqualTo(new[] { 0, 2, 4, 8, 10, 12, 16, 18, 20 }));
            Assert.That(matrix2Short.ColumnByteLength, Is.EqualTo(4));
            Assert.That(matrix2Short.ElementByteLength, Is.EqualTo(8));
            Assert.That(matrix2Short.HasColumnPadding, Is.False);
        });
    }

    [TestCase(GltfComponentType.UnsignedByte, GltfAccessorType.Vector3, 1)]
    [TestCase(GltfComponentType.UnsignedShort, GltfAccessorType.Vector3, 2)]
    [TestCase(GltfComponentType.Float, GltfAccessorType.Vector3, 4)]
    [TestCase(GltfComponentType.UnsignedByte, GltfAccessorType.Matrix2, 4)]
    [TestCase(GltfComponentType.UnsignedShort, GltfAccessorType.Matrix3, 4)]
    [TestCase(GltfComponentType.Float, GltfAccessorType.Matrix4, 4)]
    public void CalculateElementLayout_ReportsIndependentElementAlignment(
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        int expectedAlignment)
    {
        AccessorLayout calculator = new AccessorLayout();
        AccessorElementLayout result = calculator.CalculateElementLayout(
            componentType,
            accessorType);
        int misalignedOffset = expectedAlignment + 1;
        if (expectedAlignment == 1)
        {
            misalignedOffset = -1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.ElementByteAlignment, Is.EqualTo(expectedAlignment));
            Assert.That(calculator.IsElementOffsetAligned(result, expectedAlignment), Is.True);
            Assert.That(calculator.IsElementOffsetAligned(result, misalignedOffset), Is.False);
        });
    }

    [Test]
    public void LayoutMethods_RejectUnknownEnumValues()
    {
        AccessorLayout calculator = new AccessorLayout();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => calculator.GetComponentByteLength((GltfComponentType)0),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("componentType"));
            Assert.That(
                () => calculator.GetComponentCount((GltfAccessorType)99),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("accessorType"));
            Assert.That(calculator.IsKnownComponentType((GltfComponentType)0), Is.False);
            Assert.That(calculator.IsKnownAccessorType((GltfAccessorType)99), Is.False);
        });
    }
}
