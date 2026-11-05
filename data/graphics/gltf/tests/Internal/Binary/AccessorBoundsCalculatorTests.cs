using System.Buffers.Binary;

namespace gltf.Tests.Internal.Binary;

[TestFixture]
public class AccessorBoundsCalculatorTests
{
    [Test]
    public void CalculateAccessorBounds_ReadsInterleavedVectorComponents()
    {
        byte[] bytes = new byte[32];
        WriteFloat(bytes, 0, 3.0F);
        WriteFloat(bytes, 4, -2.0F);
        WriteFloat(bytes, 8, 5.0F);
        WriteFloat(bytes, 16, -1.0F);
        WriteFloat(bytes, 20, 4.0F);
        WriteFloat(bytes, 24, 2.0F);
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            2,
            bytes,
            16);

        AccessorBoundsResult result = new AccessorBoundsCalculator()
            .CalculateAccessorBounds(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Available, Is.True);
            Assert.That(result.HasValues, Is.True);
            Assert.That(result.ValuesFinite, Is.True);
            Assert.That(result.Minimum, Is.EqualTo(new double[] { -1.0, -2.0, 2.0 }));
            Assert.That(result.Maximum, Is.EqualTo(new double[] { 3.0, 4.0, 5.0 }));
        });
    }

    [Test]
    public void CalculateAccessorBounds_UsesStoredValuesForNormalizedAccessors()
    {
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Scalar,
            3,
            new byte[] { 0, 127, 255 },
            0);
        document.Accessors[0].HasNormalized = true;
        document.Accessors[0].Normalized = true;

        AccessorBoundsResult result = new AccessorBoundsCalculator()
            .CalculateAccessorBounds(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Minimum, Is.EqualTo(new double[] { 0.0 }));
            Assert.That(result.Maximum, Is.EqualTo(new double[] { 255.0 }));
        });
    }

    [Test]
    public void CalculateAccessorBounds_AppliesSparseValuesToAnImplicitZeroBase()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 12;
        buffer.Resolved = true;
        buffer.ResolvedBytes = new byte[]
        {
            1, 3, 0, 0,
            10, 0, 20, 0,
            30, 0, 5, 0
        };
        document.Buffers.Add(buffer);
        GltfBufferView indicesView = new GltfBufferView();
        indicesView.Buffer = 0;
        indicesView.ByteLength = 2;
        document.BufferViews.Add(indicesView);
        GltfBufferView valuesView = new GltfBufferView();
        valuesView.Buffer = 0;
        valuesView.HasByteOffset = true;
        valuesView.ByteOffset = 4;
        valuesView.ByteLength = 8;
        document.BufferViews.Add(valuesView);
        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.UnsignedShort;
        accessor.Count = 4;
        accessor.Type = GltfAccessorType.Vector2;
        accessor.Sparse = new GltfAccessorSparse();
        accessor.Sparse.Count = 2;
        accessor.Sparse.Indices.BufferView = 0;
        accessor.Sparse.Indices.ComponentType = GltfComponentType.UnsignedByte;
        accessor.Sparse.Values.BufferView = 1;
        document.Accessors.Add(accessor);

        AccessorBoundsResult result = new AccessorBoundsCalculator()
            .CalculateAccessorBounds(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Minimum, Is.EqualTo(new double[] { 0.0, 0.0 }));
            Assert.That(result.Maximum, Is.EqualTo(new double[] { 30.0, 20.0 }));
        });
    }

    [Test]
    public void VerifyAccessorBounds_RoundsDeclaredFloatBoundsToStoragePrecision()
    {
        byte[] bytes = new byte[8];
        WriteFloat(bytes, 0, 0.1F);
        WriteFloat(bytes, 4, 0.3F);
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            2,
            bytes,
            0);
        document.Accessors[0].Minimum = new double[] { 0.1 };
        document.Accessors[0].Maximum = new double[] { 0.3 };

        AccessorBoundsVerification result = new AccessorBoundsCalculator()
            .VerifyAccessorBounds(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.MinimumMatches, Is.True);
            Assert.That(result.MaximumMatches, Is.True);
        });
    }

    [Test]
    public void VerifyAccessorBounds_ReportsLengthOrderAndValueFailures()
    {
        GltfDocument lengthDocument = CreateDenseDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Vector2,
            1,
            new byte[] { 2, 8 },
            0);
        lengthDocument.Accessors[0].Minimum = new double[] { 2.0 };
        lengthDocument.Accessors[0].Maximum = new double[] { 2.0, 8.0, 9.0 };
        AccessorBoundsVerification lengthResult = new AccessorBoundsCalculator()
            .VerifyAccessorBounds(lengthDocument, 0);

        GltfDocument valueDocument = CreateDenseDocument(
            GltfComponentType.UnsignedByte,
            GltfAccessorType.Vector2,
            1,
            new byte[] { 2, 8 },
            0);
        valueDocument.Accessors[0].Minimum = new double[] { 3.0, 9.0 };
        valueDocument.Accessors[0].Maximum = new double[] { 2.0, 8.0 };
        AccessorBoundsVerification valueResult = new AccessorBoundsCalculator()
            .VerifyAccessorBounds(valueDocument, 0);

        Assert.Multiple(() =>
        {
            Assert.That(lengthResult.Valid, Is.False);
            Assert.That(lengthResult.MinimumLengthValid, Is.False);
            Assert.That(lengthResult.MaximumLengthValid, Is.False);
            Assert.That(valueResult.Valid, Is.False);
            Assert.That(valueResult.OrderValid, Is.False);
            Assert.That(valueResult.MinimumMatches, Is.False);
            Assert.That(valueResult.MaximumMatches, Is.True);
        });
    }

    [Test]
    public void VerifyPositionAccessorBounds_RequiresVectorThreeAndDeclaredBounds()
    {
        GltfDocument missingBounds = CreateDenseDocument(
            GltfComponentType.Float,
            GltfAccessorType.Vector3,
            1,
            new byte[12],
            0);
        AccessorBoundsVerification missingResult = new AccessorBoundsCalculator()
            .VerifyPositionAccessorBounds(missingBounds, 0);

        GltfDocument wrongShape = CreateDenseDocument(
            GltfComponentType.Float,
            GltfAccessorType.Vector2,
            1,
            new byte[8],
            0);
        wrongShape.Accessors[0].Minimum = new double[] { 0.0, 0.0 };
        wrongShape.Accessors[0].Maximum = new double[] { 0.0, 0.0 };
        AccessorBoundsVerification shapeResult = new AccessorBoundsCalculator()
            .VerifyPositionAccessorBounds(wrongShape, 0);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Valid, Is.False);
            Assert.That(missingResult.RequiredBoundsPresent, Is.False);
            Assert.That(missingResult.PositionShapeValid, Is.True);
            Assert.That(shapeResult.Valid, Is.False);
            Assert.That(shapeResult.RequiredBoundsPresent, Is.True);
            Assert.That(shapeResult.PositionShapeValid, Is.False);
        });
    }

    [Test]
    public void CalculateAccessorBounds_RepresentsEmptyAndUnavailableDataWithoutSentinels()
    {
        GltfDocument emptyDocument = CreateDenseDocument(
            GltfComponentType.Float,
            GltfAccessorType.Scalar,
            0,
            [],
            0);
        AccessorBoundsResult emptyResult = new AccessorBoundsCalculator()
            .CalculateAccessorBounds(emptyDocument, 0);

        GltfDocument unavailableDocument = new GltfDocument();
        GltfAccessor unavailableAccessor = new GltfAccessor();
        unavailableAccessor.ComponentType = GltfComponentType.Float;
        unavailableAccessor.Count = 1;
        unavailableAccessor.Type = GltfAccessorType.Scalar;
        unavailableDocument.Accessors.Add(unavailableAccessor);
        AccessorBoundsResult unavailableResult = new AccessorBoundsCalculator()
            .CalculateAccessorBounds(unavailableDocument, 0);

        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.Available, Is.True);
            Assert.That(emptyResult.HasValues, Is.False);
            Assert.That(emptyResult.Minimum, Is.Empty);
            Assert.That(emptyResult.Maximum, Is.Empty);
            Assert.That(unavailableResult.Available, Is.False);
            Assert.That(unavailableResult.HasValues, Is.False);
            Assert.That(unavailableResult.Minimum, Is.Empty);
            Assert.That(unavailableResult.Maximum, Is.Empty);
        });
    }

    [Test]
    public void VerifyAccessorBounds_RejectsNonFiniteDeclarationsWithoutResolvedData()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Count = 1;
        accessor.Type = GltfAccessorType.Scalar;
        accessor.Minimum = new double[] { double.NaN };
        accessor.Maximum = new double[] { double.PositiveInfinity };
        document.Accessors.Add(accessor);

        AccessorBoundsVerification result = new AccessorBoundsCalculator()
            .VerifyAccessorBounds(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Calculated.Available, Is.False);
            Assert.That(result.MinimumFinite, Is.False);
            Assert.That(result.MaximumFinite, Is.False);
        });
    }

    [Test]
    public void CalculateAccessorBounds_ReportsTheFirstNonFiniteComponent()
    {
        byte[] bytes = new byte[16];
        WriteFloat(bytes, 0, 1.0F);
        WriteFloat(bytes, 4, 2.0F);
        WriteFloat(bytes, 8, float.NaN);
        WriteFloat(bytes, 12, float.PositiveInfinity);
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.Float,
            GltfAccessorType.Vector2,
            2,
            bytes,
            0);

        AccessorBoundsVerification result = new AccessorBoundsCalculator()
            .VerifyAccessorBounds(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Calculated.ValuesFinite, Is.False);
            Assert.That(result.Calculated.HasValues, Is.False);
            Assert.That(result.Calculated.InvalidElement, Is.EqualTo(1));
            Assert.That(result.Calculated.InvalidComponent, Is.EqualTo(0));
        });
    }

    [Test]
    public void CalculateAccessorBounds_RejectsMatrixShapes()
    {
        GltfDocument document = CreateDenseDocument(
            GltfComponentType.Float,
            GltfAccessorType.Matrix2,
            1,
            new byte[16],
            0);

        Assert.That(
            () => new AccessorBoundsCalculator().CalculateAccessorBounds(document, 0),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("accessorType"));
    }

    private static GltfDocument CreateDenseDocument(
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        int count,
        byte[] bytes,
        int byteStride)
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = bytes.Length;
        buffer.Resolved = true;
        buffer.ResolvedBytes = bytes;
        document.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.ByteLength = bytes.Length;
        if (byteStride > 0)
        {
            view.HasByteStride = true;
            view.ByteStride = byteStride;
        }

        document.BufferViews.Add(view);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.ComponentType = componentType;
        accessor.Count = count;
        accessor.Type = accessorType;
        document.Accessors.Add(accessor);
        return document;
    }

    private static void WriteFloat(byte[] bytes, int offset, float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset, 4), value);
    }
}
