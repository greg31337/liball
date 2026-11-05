namespace avif.Tests;

public class Av1MotionVectorTests
{
    [Test]
    public void SpatialCandidatesAreDeduplicatedWeightedAndSorted()
    {
        Av1MotionVectorStackRequest request = CreateRequest(
            8, 8, 2, 2, Av1BlockSize.Block8x8,
            Av1ReferenceFrame.Last, Av1ReferenceFrame.None);
        Set(request.CurrentMotionField, 1, 2, 0, Av1ReferenceFrame.Last, 5, 7);
        Set(request.CurrentMotionField, 1, 3, 0, Av1ReferenceFrame.Last, 5, 7);
        Set(request.CurrentMotionField, 2, 1, 0, Av1ReferenceFrame.Last, 9, 11);

        Av1MotionVectorStack stack = Av1MotionVectorStackBuilder.Build(request);

        Assert.Multiple(() =>
        {
            Assert.That(stack.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(stack.Candidates[0].First.Row, Is.EqualTo(5));
            Assert.That(stack.Candidates[0].First.Column, Is.EqualTo(7));
            Assert.That(stack.Candidates[0].Weight, Is.EqualTo(8));
            Assert.That(stack.Candidates[1].First.Row, Is.EqualTo(9));
            Assert.That(stack.Candidates[1].First.Column, Is.EqualTo(11));
            Assert.That(stack.Candidates[1].Weight, Is.EqualTo(4));
            Assert.That(stack.NewMotionVectorContext, Is.EqualTo(5));
            Assert.That(stack.ZeroMotionVectorContext, Is.EqualTo(1));
        });
    }

    [Test]
    public void CompoundCandidatesRequireAndPreserveBothReferenceVectors()
    {
        Av1MotionVectorStackRequest request = CreateRequest(
            8, 8, 2, 2, Av1BlockSize.Block8x8,
            Av1ReferenceFrame.Last, Av1ReferenceFrame.Golden);
        Set(request.CurrentMotionField, 1, 2, 0, Av1ReferenceFrame.Last, 8, 16);
        Set(request.CurrentMotionField, 1, 2, 1, Av1ReferenceFrame.Golden, -8, -16);
        Set(request.CurrentMotionField, 2, 1, 0, Av1ReferenceFrame.Last, 40, 40);

        Av1MotionVectorStack stack = Av1MotionVectorStackBuilder.Build(request);

        Assert.Multiple(() =>
        {
            Assert.That(stack.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(stack.Candidates[0].Compound, Is.True);
            Assert.That(stack.Candidates[0].First.Row, Is.EqualTo(8));
            Assert.That(stack.Candidates[0].First.Column, Is.EqualTo(16));
            Assert.That(stack.Candidates[0].Second.Row, Is.EqualTo(-8));
            Assert.That(stack.Candidates[0].Second.Column, Is.EqualTo(-16));
            Assert.That(stack.Candidates[0].Weight, Is.EqualTo(4));
        });
    }

    [Test]
    public void ProjectionHandlesSignsScalingClippingAndZeroDenominators()
    {
        Av1MotionVector? positive = Av1MotionVectorStackBuilder.Project(
            new Av1MotionVector(80, -40), 2, 5);
        Av1MotionVector? negative = Av1MotionVectorStackBuilder.Project(
            new Av1MotionVector(80, -40), -3, 2);
        Av1MotionVector? clipped = Av1MotionVectorStackBuilder.Project(
            new Av1MotionVector(4095, 4095), 31, 1);
        Av1MotionVector? invalid = Av1MotionVectorStackBuilder.Project(
            new Av1MotionVector(1, 1), 1, 0);

        Assert.Multiple(() =>
        {
            AssertVector(positive, 32, -16);
            AssertVector(negative, -120, 60);
            AssertVector(clipped, 4095, 4095);
            Assert.That(invalid, Is.Null);
        });
    }

    [Test]
    public void OrderHintDistanceWrapsAtTheConfiguredModulus()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Av1MotionVectorStackBuilder.RelativeDistance(1, 31, 5),
                Is.EqualTo(2));
            Assert.That(Av1MotionVectorStackBuilder.RelativeDistance(31, 1, 5),
                Is.EqualTo(-2));
            Assert.That(Av1MotionVectorStackBuilder.RelativeDistance(12, 5, 0),
                Is.Zero);
        });
    }

    [Test]
    public void TemporalCandidateProjectsStoredMotionUsingOrderHints()
    {
        Av1MotionVectorStackRequest request = CreateRequest(
            8, 8, 2, 2, Av1BlockSize.Block8x8,
            Av1ReferenceFrame.Last, Av1ReferenceFrame.None);
        (request.Header ?? throw new InvalidOperationException("Invalid internal state.")).UseReferenceFrameMotionVectors = true;
        request.Header.OrderHint = 10;
        request.Header.ReferenceFrameIndexes[0] = 0;
        Av1DecodedFrame reference = new Av1DecodedFrame();
        reference.OrderHint = 8;
        reference.MotionField = new Av1StoredMotionField(8, 8);
        reference.MotionField.ReferenceFrames[3 * 8 + 3] =
            (int)Av1ReferenceFrame.Last;
        reference.MotionField.MotionVectorRows[3 * 8 + 3] = 40;
        reference.MotionField.MotionVectorColumns[3 * 8 + 3] = -20;
        reference.ReferenceOrderHints = new int[Av1SyntaxConstants.RefsPerFrame];
        reference.ReferenceOrderHints[0] = 3;
        (request.ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state."))[0] = reference;

        Av1MotionVectorStack stack = Av1MotionVectorStackBuilder.Build(request);
        Av1MotionVectorCandidate? temporal = FindTemporal(stack);

        Assert.Multiple(() =>
        {
            Assert.That(temporal, Is.Not.Null);
            AssertVector((temporal ?? throw new InvalidOperationException("Invalid internal state.")).First, -16, 8);
            Assert.That(temporal.Weight, Is.EqualTo(2));
        });
    }

    [Test]
    public void PrecisionLoweringRoundsSignedComponentsAtRequiredGranularity()
    {
        Av1MotionVector? high = Av1MotionVectorStackBuilder.LowerPrecision(
            new Av1MotionVector(5, -5), false, true);
        Av1MotionVector? quarter = Av1MotionVectorStackBuilder.LowerPrecision(
            new Av1MotionVector(5, -5), false, false);
        Av1MotionVector? integer = Av1MotionVectorStackBuilder.LowerPrecision(
            new Av1MotionVector(5, -13), true, false);

        Assert.Multiple(() =>
        {
            AssertVector(high, 5, -5);
            AssertVector(quarter, 4, -4);
            AssertVector(integer, 8, -16);
        });
    }

    [Test]
    public void FrameBoundsClampMotionVectorsWithTheNormativeBorder()
    {
        Av1MotionVector? topLeft = Av1MotionVectorStackBuilder.Clamp(
            new Av1MotionVector(-1000, -1000),
            0, 0, Av1BlockSize.Block8x8, 16, 16);
        Av1MotionVector? bottomRight = Av1MotionVectorStackBuilder.Clamp(
            new Av1MotionVector(1000, 1000),
            14, 14, Av1BlockSize.Block8x8, 16, 16);

        Assert.Multiple(() =>
        {
            AssertVector(topLeft, -192, -192);
            AssertVector(bottomRight, 192, 192);
            Assert.That(Av1MotionVectorStackBuilder.Clamp(
                new Av1MotionVector(0, 0),
                15, 15, Av1BlockSize.Block8x8, 16, 16), Is.Null);
        });
    }

    [Test]
    public void GlobalTranslationAndAffineModelsProduceBlockCenterVectors()
    {
        ParsedAv1FrameHeader header = CreateHeader(16, 16);
        ((header.GlobalMotion ?? throw new InvalidOperationException("Invalid internal state.")).Types ?? throw new InvalidOperationException("Invalid internal state."))[(int)Av1ReferenceFrame.Last] =
            Av1WarpModelType.Translation;
        (header.GlobalMotion.Parameters ?? throw new InvalidOperationException("Invalid internal state."))[(int)Av1ReferenceFrame.Last][0] = 16 << 13;
        header.GlobalMotion.Parameters[(int)Av1ReferenceFrame.Last][1] = -8 << 13;
        Av1MotionVector? translation = Av1MotionVectorStackBuilder.GetGlobalMotionVector(
            header, Av1ReferenceFrame.Last, 4, 4, Av1BlockSize.Block8x8);

        (header.GlobalMotion.Types ?? throw new InvalidOperationException("Invalid internal state."))[(int)Av1ReferenceFrame.Golden] =
            Av1WarpModelType.Affine;
        header.GlobalMotion.Parameters[(int)Av1ReferenceFrame.Golden][2] += 1 << 13;
        Av1MotionVector? affine = Av1MotionVectorStackBuilder.GetGlobalMotionVector(
            header, Av1ReferenceFrame.Golden, 4, 4, Av1BlockSize.Block8x8);

        Assert.Multiple(() =>
        {
            AssertVector(translation, -8, 16);
            AssertVector(affine, 0, 19);
        });
    }

    [Test]
    public void StackIsBoundedAndAlwaysHasGlobalFallbackCandidates()
    {
        Av1MotionVectorStackRequest empty = CreateRequest(
            8, 8, 2, 2, Av1BlockSize.Block8x8,
            Av1ReferenceFrame.Last, Av1ReferenceFrame.None);
        Av1MotionVectorStack fallback = Av1MotionVectorStackBuilder.Build(empty);

        Av1MotionVectorStackRequest populated = CreateRequest(
            16, 16, 5, 5, Av1BlockSize.Block16x16,
            Av1ReferenceFrame.Last, Av1ReferenceFrame.None);
        int row = 0;
        while (row < 16)
        {
            int column = 0;
            while (column < 16)
            {
                Set(populated.CurrentMotionField, row, column, 0,
                    Av1ReferenceFrame.Last, row * 8, column * 8);
                column++;
            }

            row++;
        }

        Av1MotionVectorStack bounded = Av1MotionVectorStackBuilder.Build(populated);

        Assert.Multiple(() =>
        {
            Assert.That(fallback.Candidates, Has.Count.EqualTo(2));
            AssertVector(fallback.Candidates[0].First, 0, 0);
            Assert.That(fallback.Candidates[0].Global, Is.True);
            Assert.That(bounded.Candidates.Count,
                Is.LessThanOrEqualTo(Av1SyntaxConstants.MaxReferenceMotionVectorStackSize));
            Assert.That(WeightsDescend(bounded), Is.True);
        });
    }

    static Av1MotionVectorStackRequest CreateRequest(
        int miRows,
        int miColumns,
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        Av1ReferenceFrame first,
        Av1ReferenceFrame second)
    {
        Av1MotionVectorStackRequest request = new Av1MotionVectorStackRequest();
        request.Sequence = CreateSequence();
        request.Header = CreateHeader(miRows, miColumns);
        request.CurrentMotionField = new Av1BlockMotionField(miRows, miColumns);
        request.ReferenceFrames = new Av1DecodedFrame[Av1SyntaxConstants.StoredReferenceFrames];
        request.MiRow = miRow;
        request.MiColumn = miColumn;
        request.BlockSize = blockSize;
        request.FirstReference = first;
        request.SecondReference = second;
        return request;
    }

    static ParsedAv1FrameHeader CreateHeader(int miRows, int miColumns)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.Size = new Av1FrameSize();
        header.Size.MiRows = miRows;
        header.Size.MiColumns = miColumns;
        header.Size.FrameWidth = miColumns * 4;
        header.Size.FrameHeight = miRows * 4;
        header.AllowHighPrecisionMotionVectors = true;
        header.GlobalMotion = new Av1GlobalMotionParameters();
        header.GlobalMotion.Types = new Av1WarpModelType[Av1SyntaxConstants.TotalRefsPerFrame];
        header.GlobalMotion.Parameters = new int[Av1SyntaxConstants.TotalRefsPerFrame][];
        int index = 0;
        while (index < header.GlobalMotion.Parameters.Length)
        {
            header.GlobalMotion.Parameters[index] =
                Av1FrameHeaderState.CreateIdentityGlobalParameters();
            index++;
        }

        return header;
    }

    static ParsedAv1SequenceHeader CreateSequence()
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
        features.EnableOrderHint = true;
        features.OrderHintBits = 5;
        return new ParsedAv1SequenceHeader(
            0, false, false, null, null, false,
            new Av1OperatingPoint[] { new Av1OperatingPoint(0, 0, 0) },
            64, 64, features, false, false, false, true, true,
            0, 2, 2, 2, true, false, false, false,
            System.Array.Empty<byte>());
    }

    static void Set(
        Av1BlockMotionField? field,
        int row,
        int column,
        int list,
        Av1ReferenceFrame reference,
        int motionRow,
        int motionColumn)
    {
        Assert.That((field ?? throw new InvalidOperationException("Invalid internal state.")).Set(row, column, list, reference, motionRow, motionColumn),
            Is.EqualTo(AvifResultCode.Success));
    }

    static void AssertVector(Av1MotionVector? vector, int row, int column)
    {
        Assert.That(vector, Is.Not.Null);
        Assert.That((vector ?? throw new InvalidOperationException("Invalid internal state.")).Row, Is.EqualTo(row));
        Assert.That(vector.Column, Is.EqualTo(column));
    }

    static bool WeightsDescend(Av1MotionVectorStack stack)
    {
        int index = 1;
        while (index < stack.Candidates.Count)
        {
            if (stack.Candidates[index].Weight
                > stack.Candidates[index - 1].Weight)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static Av1MotionVectorCandidate? FindTemporal(Av1MotionVectorStack stack)
    {
        int index = 0;
        while (index < stack.Candidates.Count)
        {
            if (stack.Candidates[index].Temporal)
            {
                return stack.Candidates[index];
            }

            index++;
        }

        return null;
    }
}
