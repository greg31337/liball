namespace random.Tests;

public class GenericInverseTransformSamplerTests
{
    const ulong ZeroOutputSeed = 0x61C8864680B583EBUL;
    const ulong UInt64MaximumOutputSeed = 0x31628AF67B2131ABUL;

    [Test]
    public void SamplesApplyTransformToExactOpenUnitInputs()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator expectedGenerator = Create(101UL);
        int calls = 0;
        InverseTransformSampler sampler = new InverseTransformSampler(actualGenerator, unit =>
            {
                calls++;
                Assert.That(unit, Is.GreaterThan(0.0).And.LessThan(1.0));
                return 3.0 + 2.0 * unit;
            }, InverseTransformOutputValidation.RequireFinite);

        for (int index = 0; index < 5000; index++)
        {
            double expected = 3.0 + 2.0 * expectedGenerator.NextDoubleOpenOpen();
            Assert.That(sampler.Sample(), Is.EqualTo(expected));
        }

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(5000));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void RawEndpointWordsStillProduceStrictlyOpenInputs()
    {
        double lowInput = 0.0;
        double highInput = 0.0;
        RandomGenerator lowGenerator = Create(ZeroOutputSeed);
        RandomGenerator lowReference = Create(ZeroOutputSeed);
        RandomGenerator highGenerator = Create(UInt64MaximumOutputSeed);
        RandomGenerator highReference = Create(UInt64MaximumOutputSeed);
        InverseTransformSampler low = new InverseTransformSampler(lowGenerator, unit => Capture(unit, value => lowInput = value), InverseTransformOutputValidation.RequireFinite);
        InverseTransformSampler high = new InverseTransformSampler(highGenerator, unit => Capture(unit, value => highInput = value), InverseTransformOutputValidation.RequireFinite);

        double lowExpected = lowReference.NextDoubleOpenOpen();
        double highExpected = highReference.NextDoubleOpenOpen();
        Assert.That(low.Sample(), Is.EqualTo(lowExpected));
        Assert.That(high.Sample(), Is.EqualTo(highExpected));

        Assert.Multiple(() =>
        {
            Assert.That(lowInput, Is.EqualTo(lowExpected).And.GreaterThan(0.0).And.LessThan(1.0));
            Assert.That(highInput, Is.EqualTo(highExpected).And.GreaterThan(0.0).And.LessThan(1.0));
            Assert.That(lowGenerator.ExportState().ToBytes(), Is.EqualTo(lowReference.ExportState().ToBytes()));
            Assert.That(highGenerator.ExportState().ToBytes(), Is.EqualTo(highReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void OutputValidationPoliciesDistinguishNaNAndInfinity()
    {
        InverseTransformSampler unrestrictedNaN = new InverseTransformSampler(Create(202UL), unit => double.NaN, InverseTransformOutputValidation.None);
        InverseTransformSampler unrestrictedInfinity = new InverseTransformSampler(Create(203UL), unit => double.PositiveInfinity, InverseTransformOutputValidation.None);
        InverseTransformSampler rejectNaNInfinity = new InverseTransformSampler(Create(204UL), unit => double.NegativeInfinity, InverseTransformOutputValidation.RejectNaN);
        InverseTransformSampler rejectNaN = new InverseTransformSampler(Create(205UL), unit => double.NaN, InverseTransformOutputValidation.RejectNaN);
        InverseTransformSampler requireFiniteNaN = new InverseTransformSampler(Create(206UL), unit => double.NaN, InverseTransformOutputValidation.RequireFinite);
        InverseTransformSampler requireFiniteInfinity = new InverseTransformSampler(Create(207UL), unit => double.PositiveInfinity, InverseTransformOutputValidation.RequireFinite);

        Assert.Multiple(() =>
        {
            Assert.That(unrestrictedNaN.OutputValidation(), Is.EqualTo(InverseTransformOutputValidation.None));
            Assert.That(rejectNaN.OutputValidation(), Is.EqualTo(InverseTransformOutputValidation.RejectNaN));
            Assert.That(requireFiniteNaN.OutputValidation(), Is.EqualTo(InverseTransformOutputValidation.RequireFinite));
            Assert.That(double.IsNaN(unrestrictedNaN.Sample()), Is.True);
            Assert.That(unrestrictedInfinity.Sample(), Is.EqualTo(double.PositiveInfinity));
            Assert.That(rejectNaNInfinity.Sample(), Is.EqualTo(double.NegativeInfinity));
            Assert.Throws<RandomStateException>(() => rejectNaN.Sample());
            Assert.Throws<RandomStateException>(() => requireFiniteNaN.Sample());
            Assert.Throws<RandomStateException>(() => requireFiniteInfinity.Sample());
        });
    }

    [Test]
    public void DefaultPolicyRequiresFiniteOutputsAndRejectionConsumesTheDraw()
    {
        RandomGenerator actualGenerator = Create(303UL);
        RandomGenerator expectedGenerator = Create(303UL);
        double seenInput = 0.0;
        InverseTransformSampler sampler = new InverseTransformSampler(actualGenerator, unit =>
            {
                seenInput = unit;
                return double.PositiveInfinity;
            }, InverseTransformOutputValidation.RequireFinite);
        double expectedInput = expectedGenerator.NextDoubleOpenOpen();

        Assert.Throws<RandomStateException>(() => sampler.Sample());

        Assert.Multiple(() =>
        {
            Assert.That(seenInput, Is.EqualTo(expectedInput));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void FillMatchesRepeatedSamplesAndEmptyFillConsumesNothing()
    {
        RandomGenerator fillGenerator = Create(404UL);
        RandomGenerator repeatedGenerator = Create(404UL);
        InverseTransformSampler filling = new InverseTransformSampler(fillGenerator, unit => unit * unit, InverseTransformOutputValidation.RequireFinite);
        InverseTransformSampler repeated = new InverseTransformSampler(repeatedGenerator, unit => unit * unit, InverseTransformOutputValidation.RequireFinite);
        byte[] before = fillGenerator.ExportState().ToBytes();
        filling.Fill(Array.Empty<double>());
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        double[] actual = new double[2000];
        double[] expected = new double[2000];
        filling.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = repeated.Sample();
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void DelegateExceptionsPropagateAfterOneOpenUnitDraw()
    {
        RandomGenerator actualGenerator = Create(505UL);
        RandomGenerator expectedGenerator = Create(505UL);
        InvalidOperationException expectedError = new InvalidOperationException("delegate failure");
        InverseTransformSampler sampler = new InverseTransformSampler(actualGenerator, unit => throw expectedError, InverseTransformOutputValidation.RequireFinite);
        expectedGenerator.NextDoubleOpenOpen();

        InvalidOperationException actualError = Assert.Throws<InvalidOperationException>(() => sampler.Sample());

        Assert.Multiple(() =>
        {
            Assert.That(actualError, Is.SameAs(expectedError));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InvalidConstructionInputsAreRejectedWithoutConsumption()
    {
        AssertArgument("generator", () => new InverseTransformSampler(NullReference.Value<global::random.IRandomSource>(), unit => unit, InverseTransformOutputValidation.RequireFinite));
        RandomGenerator generator = Create(606UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        AssertArgument("inverseCdf", () => new InverseTransformSampler(generator, NullReference.Value<global::System.Func<double, double>>(), InverseTransformOutputValidation.RequireFinite));
        AssertArgument("inverseCdf", () => new InverseTransformSampler(generator, NullReference.Value<global::System.Func<double, double>>(), InverseTransformOutputValidation.None));
        AssertArgument("outputValidation", () => new InverseTransformSampler(generator, unit => unit, (InverseTransformOutputValidation)99));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    static double Capture(double value, Action<double> capture)
    {
        capture(value);
        return value;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
