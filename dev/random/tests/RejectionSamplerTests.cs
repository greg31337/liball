namespace random.Tests;

public class RejectionSamplerTests
{
    [Test]
    public void ProbabilisticAcceptanceMatchesReferenceTraversalAndDiagnostics()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator expectedGenerator = Create(101UL);
        int actualProposal = 0;
        int expectedProposal = 0;
        RejectionSampler sampler = new RejectionSampler(actualGenerator, () => ++actualProposal, candidate => candidate * 10.0, candidate => 0.25, 100);
        long expectedTotalAttempts = 0L;

        for (int sampleIndex = 0; sampleIndex < 500; sampleIndex++)
        {
            int expectedAttempts = 0;
            double acceptedCandidate = 0.0;
            while (true)
            {
                expectedAttempts++;
                expectedTotalAttempts++;
                expectedProposal++;
                if (expectedGenerator.NextDoubleClosedOpen() < 0.25)
                {
                    acceptedCandidate = expectedProposal;
                    break;
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(sampler.Sample(), Is.EqualTo(acceptedCandidate * 10.0));
                Assert.That(sampler.LastAttemptCount(), Is.EqualTo(expectedAttempts));
                Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(expectedTotalAttempts));
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(actualProposal, Is.EqualTo(expectedProposal));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ProposalAcceptanceAndValueDelegatesUseDocumentedOrder()
    {
        List<string> calls = [];
        RejectionSampler sampler = new RejectionSampler(Create(202UL), () =>
            {
                calls.Add("proposal");
                return 7.0;
            }, candidate =>
            {
                calls.Add("value");
                return candidate + 1.0;
            }, candidate =>
            {
                calls.Add("acceptance");
                return 1.0;
            }, 5);

        Assert.That(sampler.Sample(), Is.EqualTo(8.0));
        Assert.That(calls, Is.EqualTo(new string[] { "proposal", "acceptance", "value" }));
    }

    [Test]
    public void ExhaustionThrowsWithoutCallingValueOrReturningFallback()
    {
        RandomGenerator generator = Create(303UL);
        byte[] before = generator.ExportState().ToBytes();
        int proposals = 0;
        int values = 0;
        RejectionSampler sampler = new RejectionSampler(generator, () => ++proposals, candidate =>
            {
                values++;
                return candidate;
            }, candidate => 0.0, 3);

        Assert.Throws<RandomExhaustedException>(() => sampler.Sample());
        Assert.Multiple(() =>
        {
            Assert.That(proposals, Is.EqualTo(3));
            Assert.That(values, Is.EqualTo(0));
            Assert.That(sampler.MaximumAttempts(), Is.EqualTo(3));
            Assert.That(sampler.LastAttemptCount(), Is.EqualTo(3));
            Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(3L));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });

        Assert.Throws<RandomExhaustedException>(() => sampler.Sample());
        Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(6L));
    }

    [Test]
    public void DeterministicAcceptanceEndpointsDoNotConsumeGeneratorState()
    {
        RandomGenerator acceptGenerator = Create(404UL);
        RandomGenerator rejectGenerator = Create(505UL);
        acceptGenerator.NextBoolean();
        rejectGenerator.NextBoolean();
        byte[] acceptBefore = acceptGenerator.ExportState().ToBytes();
        byte[] rejectBefore = rejectGenerator.ExportState().ToBytes();
        RejectionSampler accepted = RejectionSampler.CreateIdentity(acceptGenerator, () => 17.0, candidate => 1.0, 1);
        RejectionSampler rejected = RejectionSampler.CreateIdentity(rejectGenerator, () => 19.0, candidate => 0.0, 1);

        Assert.That(accepted.Sample(), Is.EqualTo(17.0));
        Assert.Throws<RandomExhaustedException>(() => rejected.Sample());

        Assert.Multiple(() =>
        {
            Assert.That(acceptGenerator.ExportState().ToBytes(), Is.EqualTo(acceptBefore));
            Assert.That(rejectGenerator.ExportState().ToBytes(), Is.EqualTo(rejectBefore));
        });
    }

    [Test]
    public void FillMatchesRepeatedSamplesIncludingDiagnostics()
    {
        RandomGenerator fillGenerator = Create(606UL);
        RandomGenerator repeatedGenerator = Create(606UL);
        int fillProposal = 0;
        int repeatedProposal = 0;
        RejectionSampler filling = RejectionSampler.CreateIdentity(fillGenerator, () => ++fillProposal, candidate => 0.4, 100);
        RejectionSampler repeated = RejectionSampler.CreateIdentity(repeatedGenerator, () => ++repeatedProposal, candidate => 0.4, 100);
        byte[] before = fillGenerator.ExportState().ToBytes();
        filling.Fill(Array.Empty<double>());
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        double[] actual = new double[1000];
        double[] expected = new double[1000];
        filling.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = repeated.Sample();
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillProposal, Is.EqualTo(repeatedProposal));
            Assert.That(filling.LastAttemptCount(), Is.EqualTo(repeated.LastAttemptCount()));
            Assert.That(filling.TotalAttemptCount(), Is.EqualTo(repeated.TotalAttemptCount()));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InvalidAcceptanceProbabilitiesFailOnTheirFirstAttempt()
    {
        double[] invalid = [double.NaN, double.NegativeInfinity, -double.Epsilon, 1.0000000000000002, double.PositiveInfinity];
        foreach (double probability in invalid)
        {
            RandomGenerator generator = Create(707UL);
            byte[] before = generator.ExportState().ToBytes();
            RejectionSampler sampler = RejectionSampler.CreateIdentity(generator, () => 1.0, candidate => probability, 5);

            Assert.Throws<RandomStateException>(() => sampler.Sample());

            Assert.Multiple(() =>
            {
                Assert.That(sampler.LastAttemptCount(), Is.EqualTo(1));
                Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(1L));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            });
        }
    }

    [Test]
    public void DelegateOutputsAndExceptionsArePropagatedAtTheirDefinedStage()
    {
        RejectionSampler nonFiniteValue = new RejectionSampler(Create(808UL), () => double.NaN, candidate => double.PositiveInfinity, candidate => 1.0, 1);
        Assert.That(nonFiniteValue.Sample(), Is.EqualTo(double.PositiveInfinity));

        InvalidOperationException proposalError = new InvalidOperationException("proposal");
        RejectionSampler proposalFailure = RejectionSampler.CreateIdentity(Create(809UL), () => throw proposalError, candidate => 1.0, 1);
        Assert.That(Assert.Throws<InvalidOperationException>(() => proposalFailure.Sample()), Is.SameAs(proposalError));
        Assert.That(proposalFailure.LastAttemptCount(), Is.EqualTo(1));

        InvalidOperationException valueError = new InvalidOperationException("value");
        RejectionSampler valueFailure = new RejectionSampler(Create(810UL), () => 1.0, candidate => throw valueError, candidate => 1.0, 1);
        Assert.That(Assert.Throws<InvalidOperationException>(() => valueFailure.Sample()), Is.SameAs(valueError));
        Assert.That(valueFailure.LastAttemptCount(), Is.EqualTo(1));
    }

    [Test]
    public void InvalidConstructionInputsAreRejectedWithoutConsumption()
    {
        AssertArgument("generator", () => RejectionSampler.CreateIdentity(NullReference.Value<global::random.IRandomSource>(), () => 1.0, candidate => 1.0, 1));
        RandomGenerator generator = Create(909UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        AssertArgument("proposal", () => RejectionSampler.CreateIdentity(generator, NullReference.Value<global::System.Func<double>>(), candidate => 1.0, 1));
        AssertArgument("acceptance", () => RejectionSampler.CreateIdentity(generator, () => 1.0, NullReference.Value<global::System.Func<double, double>>(), 1));
        AssertArgument("value", () => new RejectionSampler(generator, () => 1.0, NullReference.Value<global::System.Func<double, double>>(), candidate => 1.0, 1));
        AssertArgument("maximumAttempts", () => RejectionSampler.CreateIdentity(generator, () => 1.0, candidate => 1.0, 0));
        AssertArgument("maximumAttempts", () => RejectionSampler.CreateIdentity(generator, () => 1.0, candidate => 1.0, -1));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
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
