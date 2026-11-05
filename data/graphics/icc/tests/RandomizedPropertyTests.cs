using icc;

namespace icc.Tests;

public class RandomizedPropertyTests
{
    private const int NumericSeed = 164001;
    private const int OffsetSeed = 164002;
    private const int ProfileSeed = 164003;
    private const int InterpolationSeed = 164004;
    private const int MatrixSeed = 164005;
    private const int PermutationSeed = 164006;
    private const int UnknownSeed = 164007;
    private const int MutationSeed = 164008;
    private const uint PrivateTypeSignature = 0x7a7a7a7a;

    [Test]
    public void NumericCodecsRoundTripSeededRepresentableValues()
    {
        Random random = new Random(NumericSeed);
        int iteration = 0;
        while (iteration < 512)
        {
            int signed = (int)random.NextInt64(int.MinValue,
                (long)int.MaxValue + 1L);
            uint unsigned = (uint)random.NextInt64(0L,
                (long)uint.MaxValue + 1L);
            ushort shortValue = (ushort)random.Next(0,
                (int)ushort.MaxValue + 1);
            uint floatBits = (uint)random.NextInt64(0L,
                (long)uint.MaxValue + 1L);
            floatBits = (floatBits & 0x807fffff)
                | ((uint)random.Next(1, 255) << 23);
            float floatValue = BitConverter.Int32BitsToSingle(
                unchecked((int)floatBits));
            byte[] floatBytes = new byte[4];
            BigEndianWriter writer = new BigEndianWriter(floatBytes, 0L);
            IccNumberCodec.WriteFloat32(writer, floatValue);
            float decodedFloat = IccNumberCodec.ReadFloat32(
                new BigEndianReader(floatBytes, 0L));

            Assert.Multiple(() =>
            {
                Assert.That(IccNumberCodec.EncodeS15Fixed16(
                    IccNumberCodec.DecodeS15Fixed16(signed)),
                    Is.EqualTo(signed), Case(NumericSeed, iteration));
                Assert.That(IccNumberCodec.EncodeU16Fixed16(
                    IccNumberCodec.DecodeU16Fixed16(unsigned)),
                    Is.EqualTo(unsigned), Case(NumericSeed, iteration));
                Assert.That(IccNumberCodec.EncodeU1Fixed15(
                    IccNumberCodec.DecodeU1Fixed15(shortValue)),
                    Is.EqualTo(shortValue), Case(NumericSeed, iteration));
                Assert.That(IccNumberCodec.EncodeU8Fixed8(
                    IccNumberCodec.DecodeU8Fixed8(shortValue)),
                    Is.EqualTo(shortValue), Case(NumericSeed, iteration));
                Assert.That(BitConverter.SingleToInt32Bits(decodedFloat),
                    Is.EqualTo(unchecked((int)floatBits)),
                    Case(NumericSeed, iteration));
            });

            iteration++;
        }
    }

    [Test]
    public void CheckedOffsetsCountsAndAlignmentObeySeededBounds()
    {
        Random random = new Random(OffsetSeed);
        int iteration = 0;
        while (iteration < 256)
        {
            int count = random.Next(0, 1001);
            int itemSize = random.Next(0, 1001);
            int expectedBytes = count * itemSize;
            int maximumBytes = expectedBytes + random.Next(0, 1001);
            int size = random.Next(0, 1000001);
            int alignment = random.Next(1, 65);
            int expectedAligned = ((size + alignment - 1) / alignment)
                * alignment;
            long offset = random.NextInt64(0L, 1000000000L);
            long length = random.NextInt64(0L, 1000000L);
            long containerLength = offset + length
                + random.NextInt64(0L, 1000L);

            Assert.Multiple(() =>
            {
                Assert.That(CheckedSize.MultiplyCount(count, itemSize,
                    maximumBytes), Is.EqualTo(expectedBytes),
                    Case(OffsetSeed, iteration));
                Assert.That(CheckedSize.AlignSize(size, alignment,
                    expectedAligned), Is.EqualTo(expectedAligned),
                    Case(OffsetSeed, iteration));
                Assert.That(CheckedSize.ValidateRange(offset, length,
                    containerLength), Is.EqualTo(offset + length),
                    Case(OffsetSeed, iteration));
                if (expectedBytes > 0)
                {
                    Assert.Throws<IccProfileFormatException>(() =>
                        CheckedSize.MultiplyCount(count, itemSize,
                            expectedBytes - 1), Case(OffsetSeed, iteration));
                }

                Assert.Throws<IccProfileFormatException>(() =>
                    CheckedSize.ValidateRange(offset, length + 1,
                        offset + length), Case(OffsetSeed, iteration));
            });

            iteration++;
        }
    }

    [Test]
    public void ParserRejectsSeededOutOfRangeTagOffsetsSizesAndCounts()
    {
        Random random = new Random(OffsetSeed);
        int iteration = 0;
        while (iteration < 90)
        {
            byte[] payload = IccBinaryFixture.CreateTagPayload(
                IccTypeSignatures.Signature, 12);
            byte[] profile = IccBinaryFixture.CreateProfile(
                0x04400000, IccProfileClassSignatures.Display,
                IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, 0,
                new IccBinaryTagFixture(PrivateTagSignature(0), payload));
            int mutation = iteration % 3;
            if (mutation == 0)
            {
                IndependentBigEndian.WriteUInt32(profile, 128,
                    (uint)random.Next(2, 100000));
            }
            else if (mutation == 1)
            {
                IndependentBigEndian.WriteUInt32(profile, 136,
                    (uint)(profile.Length + random.Next(1, 100000)));
            }
            else
            {
                IndependentBigEndian.WriteUInt32(profile, 140,
                    (uint)(profile.Length + random.Next(1, 100000)));
            }

            IccOperationResult result = new IccProfileReader()
                .ProbeProfileBytes(profile, new IccReadOptions());

            Assert.That(result.Status,
                Is.Not.EqualTo(IccOperationStatus.Success),
                Case(OffsetSeed, iteration));
            iteration++;
        }
    }

    [Test]
    public void ProfileParseWriteParseIsIdempotentForSeededPayloads()
    {
        Random random = new Random(ProfileSeed);
        IIccProfileBuilder builder = new IccProfileBuilder();
        IIccProfileWriter writer = new IccProfileWriter();
        IccWriteOptions writeOptions = new IccWriteOptions();
        writeOptions.PreserveOriginalCreationDateTime = false;
        IccReadOptions readOptions = new IccReadOptions();
        readOptions.DecodeTagsLazily = false;
        int iteration = 0;
        while (iteration < 40)
        {
            IccProfile profile = ValidIdentityProfile(builder);
            int tagCount = random.Next(1, 7);
            int tagIndex = 0;
            while (tagIndex < tagCount)
            {
                profile = builder.SetProfileTag(profile, CreatePrivateTag(
                    PrivateTagSignature(tagIndex), RandomBytes(random,
                        random.Next(0, 33))));
                tagIndex++;
            }

            byte[] first = writer.WriteProfileBytes(profile, writeOptions);
            IccProfile parsed = new IccProfileReader().ReadProfileBytes(first,
                readOptions);
            byte[] second = writer.WriteProfileBytes(parsed, writeOptions);
            IccProfile reparsed = new IccProfileReader().ReadProfileBytes(
                second, readOptions);

            Assert.Multiple(() =>
            {
                Assert.That(second, Is.EqualTo(first),
                    Case(ProfileSeed, iteration));
                Assert.That(reparsed.Header.ProfileId.Bytes,
                    Is.EqualTo(parsed.Header.ProfileId.Bytes),
                    Case(ProfileSeed, iteration));
                Assert.That(reparsed.Tags.Select(tag => tag.Signature),
                    Is.EqualTo(parsed.Tags.Select(tag => tag.Signature)),
                    Case(ProfileSeed, iteration));
            });

            iteration++;
        }
    }

    [Test]
    public void CurveAndClutInterpolationPreserveSeededLinearFunctions()
    {
        Random random = new Random(InterpolationSeed);
        int iteration = 0;
        while (iteration < 256)
        {
            int sampleCount = random.Next(2, 18);
            double[] samples = RandomDoubles(random, sampleCount, -2.0, 2.0);
            double input = NextDouble(random, -0.25, 1.25);
            double clipped = System.Math.Max(0.0, System.Math.Min(1.0,
                input));
            double position = clipped * (sampleCount - 1);
            int lower = (int)System.Math.Floor(position);
            double expectedCurve;
            if (lower >= sampleCount - 1)
            {
                expectedCurve = samples[sampleCount - 1];
            }
            else
            {
                double fraction = position - lower;
                expectedCurve = (samples[lower] * (1.0 - fraction))
                    + (samples[lower + 1] * fraction);
            }

            double[] coefficients = RandomDoubles(random, 3, -2.0, 2.0);
            double bias = NextDouble(random, -1.0, 1.0);
            IccClut clut = CreateAffineClut(coefficients, bias);
            double[] clutInputs = RandomDoubles(random, 3, 0.0, 1.0);
            double expectedClut = bias;
            int channel = 0;
            while (channel < clutInputs.Length)
            {
                expectedClut += coefficients[channel] * clutInputs[channel];
                channel++;
            }

            double[] multilinear = new double[1];
            double[] tetrahedral = new double[1];
            ClutInterpolator.InterpolateMultilinear(clut, clutInputs,
                multilinear, new int[6], new double[3]);
            ClutInterpolator.InterpolateTetrahedral(clut, clutInputs,
                tetrahedral, new int[6], new double[3]);

            Assert.Multiple(() =>
            {
                Assert.That(CurveMath.EvaluateSampledCurve(samples, input),
                    Is.EqualTo(expectedCurve).Within(1e-12),
                    Case(InterpolationSeed, iteration));
                Assert.That(multilinear[0],
                    Is.EqualTo(expectedClut).Within(1e-12),
                    Case(InterpolationSeed, iteration));
                Assert.That(tetrahedral[0],
                    Is.EqualTo(expectedClut).Within(1e-12),
                    Case(InterpolationSeed, iteration));
            });

            iteration++;
        }
    }

    [Test]
    public void MatrixInversionProducesSeededLeftAndRightIdentities()
    {
        Random random = new Random(MatrixSeed);
        int iteration = 0;
        while (iteration < 128)
        {
            int size = random.Next(2, 7);
            double[] matrix = CreateDiagonallyDominantMatrix(random, size);
            double[] original = matrix.ToArray();
            double[] inverse = MatrixMath.InvertMatrix(matrix, size);
            double[] left = MatrixMath.MultiplyMatrices(matrix, size, size,
                inverse, size);
            double[] right = MatrixMath.MultiplyMatrices(inverse, size, size,
                matrix, size);
            double[] vector = RandomDoubles(random, size, -2.0, 2.0);
            double[] transformed = MatrixMath.ApplyMatrixToVector(matrix,
                size, size, vector);
            double[] restored = MatrixMath.ApplyMatrixToVector(inverse,
                size, size, transformed);

            Assert.Multiple(() =>
            {
                Assert.That(left, Is.EqualTo(Identity(size)).Within(1e-11),
                    Case(MatrixSeed, iteration));
                Assert.That(right, Is.EqualTo(Identity(size)).Within(1e-11),
                    Case(MatrixSeed, iteration));
                Assert.That(restored, Is.EqualTo(vector).Within(1e-11),
                    Case(MatrixSeed, iteration));
                Assert.That(matrix, Is.EqualTo(original),
                    Case(MatrixSeed, iteration));
            });

            iteration++;
        }
    }

    [Test]
    public void CanonicalBytesIgnoreSeededTagOrderPermutations()
    {
        Random random = new Random(PermutationSeed);
        IIccProfileBuilder builder = new IccProfileBuilder();
        IIccProfileWriter writer = new IccProfileWriter();
        IccProfile source = ValidIdentityProfile(builder);
        List<IccTag> tags = source.Tags.ToList();
        int privateIndex = 0;
        while (privateIndex < 8)
        {
            tags.Add(CreatePrivateTag(PrivateTagSignature(privateIndex),
                new byte[] { (byte)privateIndex, (byte)(privateIndex * 7) }));
            privateIndex++;
        }

        IccWriteOptions options = new IccWriteOptions();
        options.PreserveOriginalCreationDateTime = false;
        options.RejectInvalidProfile = false;
        IccProfile baseline = builder.CloneProfile(source);
        baseline.Tags = tags.ToArray();
        byte[] expected = writer.WriteProfileBytes(baseline, options);
        int iteration = 0;
        while (iteration < 128)
        {
            IccTag[] shuffled = tags.ToArray();
            Shuffle(random, shuffled);
            IccProfile permuted = builder.CloneProfile(source);
            permuted.Tags = shuffled;

            byte[] actual = writer.WriteProfileBytes(permuted, options);

            Assert.That(actual, Is.EqualTo(expected),
                Case(PermutationSeed, iteration));
            iteration++;
        }
    }

    [Test]
    public void UnknownPayloadsRoundTripEverySeededByte()
    {
        Random random = new Random(UnknownSeed);
        int iteration = 0;
        while (iteration < 256)
        {
            byte[] payload = RandomBytes(random, random.Next(8, 129));
            uint typeSignature = UnknownTypeSignature(iteration);
            IndependentBigEndian.WriteUInt32(payload, 0, typeSignature);
            TagDecodeState state = new TagDecodeState(
                new TagDecodeSource(payload), 0, payload.Length, 0L,
                IccVersionKind.Version4, true);

            TagCodecDispatcher.DecodeTagPayload(state);
            IccTag tag = new IccTag();
            tag.Signature = PrivateTagSignature(iteration % 8);
            tag.TableEntry.Signature = tag.Signature;
            tag.Data = state.Data;
            IccWriteOptions options = new IccWriteOptions();
            options.Mode = IccWriteMode.Preservation;
            byte[] serialized = ProfileTagSerializer.SerializeTag(tag,
                new IccProfileHeader(), options);

            Assert.Multiple(() =>
            {
                Assert.That(state.Result.Status,
                    Is.EqualTo(IccOperationStatus.UnsupportedFeature),
                    Case(UnknownSeed, iteration));
                Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw),
                    Case(UnknownSeed, iteration));
                Assert.That(state.Data.RawData.TypeSignature,
                    Is.EqualTo(typeSignature), Case(UnknownSeed, iteration));
                Assert.That(serialized, Is.EqualTo(payload),
                    Case(UnknownSeed, iteration));
            });

            iteration++;
        }
    }

    [Test]
    public void SeededMutationSequencesPreserveModelsAndExpectedTagState()
    {
        Random random = new Random(MutationSeed);
        IIccProfileBuilder builder = new IccProfileBuilder();
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        IccProfile current = ValidIdentityProfile(builder);
        Dictionary<uint, byte[]> expected = new Dictionary<uint, byte[]>();
        int setCount = 0;
        int removeCount = 0;
        int iteration = 0;
        while (iteration < 96)
        {
            IccProfile previous = current;
            byte[] previousBytes = previous.OriginalBytes.ToArray();
            int slot = random.Next(0, 8);
            uint signature = PrivateTagSignature(slot);
            if (random.Next(0, 3) < 2)
            {
                IccTag replacement = CreatePrivateTag(signature,
                    RandomBytes(random, random.Next(1, 25)));
                expected[signature] = replacement.Data.RawData.Bytes.ToArray();
                current = builder.SetProfileTag(current, replacement);
                replacement.Data.RawData.Bytes[8] = (byte)(
                    replacement.Data.RawData.Bytes[8] ^ 0xff);
                setCount++;
            }
            else
            {
                expected.Remove(signature);
                current = builder.RemoveProfileTag(current, signature);
                removeCount++;
            }

            Assert.That(previous.OriginalBytes, Is.EqualTo(previousBytes),
                Case(MutationSeed, iteration));
            Assert.That(current.Header.ProfileId.Bytes,
                Is.EqualTo(calculator.ComputeProfileIdBytes(
                    current.OriginalBytes).Bytes), Case(MutationSeed, iteration));
            Assert.That(current.Tags.Count(tag => IsPrivateTag(tag.Signature)),
                Is.EqualTo(expected.Count), Case(MutationSeed, iteration));
            foreach (KeyValuePair<uint, byte[]> pair in expected)
            {
                Assert.That(current.Tags.Single(tag => tag.Signature
                    == pair.Key).Data.RawData.Bytes, Is.EqualTo(pair.Value),
                    Case(MutationSeed, iteration));
            }

            iteration++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(setCount, Is.GreaterThan(0));
            Assert.That(removeCount, Is.GreaterThan(0));
        });
    }

    private static string Case(int seed, int iteration)
    {
        return $"seed={seed}, iteration={iteration}";
    }

    private static IccProfile ValidIdentityProfile(IIccProfileBuilder builder)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        return builder.CreateXyzIdentityProfile(options);
    }

    private static IccTag CreatePrivateTag(uint signature, byte[] body)
    {
        byte[] payload = new byte[8 + body.Length];
        IndependentBigEndian.WriteUInt32(payload, 0, PrivateTypeSignature);
        System.Array.Copy(body, 0, payload, 8, body.Length);
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = IccTagDataKind.Raw;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = PrivateTypeSignature;
        tag.Data.RawData.Bytes = payload;
        return tag;
    }

    private static uint PrivateTagSignature(int index)
    {
        return 0x70303030u + (uint)index;
    }

    private static bool IsPrivateTag(uint signature)
    {
        return signature >= PrivateTagSignature(0)
            && signature <= PrivateTagSignature(7);
    }

    private static uint UnknownTypeSignature(int index)
    {
        int first = index / (26 * 26);
        int second = (index / 26) % 26;
        int third = index % 26;
        return ((uint)'z' << 24) | ((uint)('a' + first) << 16)
            | ((uint)('a' + second) << 8) | (uint)('a' + third);
    }

    private static byte[] RandomBytes(Random random, int length)
    {
        byte[] values = new byte[length];
        random.NextBytes(values);
        return values;
    }

    private static double[] RandomDoubles(Random random, int length,
        double minimum, double maximum)
    {
        double[] values = new double[length];
        int index = 0;
        while (index < length)
        {
            values[index] = NextDouble(random, minimum, maximum);
            index++;
        }

        return values;
    }

    private static double NextDouble(Random random, double minimum,
        double maximum)
    {
        return minimum + (random.NextDouble() * (maximum - minimum));
    }

    private static IccClut CreateAffineClut(double[] coefficients,
        double bias)
    {
        double[] values = new double[8];
        int vertex = 0;
        while (vertex < 8)
        {
            double value = bias;
            int channel = 0;
            while (channel < 3)
            {
                int bit = 1 << (2 - channel);
                if ((vertex & bit) != 0)
                {
                    value += coefficients[channel];
                }

                channel++;
            }

            values[vertex] = value;
            vertex++;
        }

        IccClut clut = new IccClut();
        clut.InputChannels = 3;
        clut.OutputChannels = 1;
        clut.GridPoints = new int[] { 2, 2, 2 };
        clut.Values = values;
        return clut;
    }

    private static double[] CreateDiagonallyDominantMatrix(Random random,
        int size)
    {
        double[] matrix = new double[size * size];
        int row = 0;
        while (row < size)
        {
            double offDiagonalSum = 0.0;
            int column = 0;
            while (column < size)
            {
                if (row != column)
                {
                    double value = NextDouble(random, -0.25, 0.25);
                    matrix[(row * size) + column] = value;
                    offDiagonalSum += System.Math.Abs(value);
                }

                column++;
            }

            matrix[(row * size) + row] = 1.0 + offDiagonalSum
                + random.NextDouble();
            row++;
        }

        return matrix;
    }

    private static double[] Identity(int size)
    {
        double[] result = new double[size * size];
        int index = 0;
        while (index < size)
        {
            result[(index * size) + index] = 1.0;
            index++;
        }

        return result;
    }

    private static void Shuffle(Random random, IccTag[] values)
    {
        int index = values.Length - 1;
        while (index > 0)
        {
            int other = random.Next(0, index + 1);
            IccTag temporary = values[index];
            values[index] = values[other];
            values[other] = temporary;
            index--;
        }
    }
}
