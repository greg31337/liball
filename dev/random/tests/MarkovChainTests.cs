namespace random.Tests;

public class MarkovChainTests
{
    [Test]
    public void PathMatchesIndependentAliasRowTraversalExactly()
    {
        double[] matrix =
        [
            0.1, 0.6, 0.3,
            0.0, 1.0, 0.0,
            0.7, 0.2, 0.1
        ];
        MarkovChain chain = new MarkovChain(matrix, 3);
        AliasTable[] rows =
        [
            new AliasTable([0.1, 0.6, 0.3]),
            new AliasTable([0.0, 1.0, 0.0]),
            new AliasTable([0.7, 0.2, 0.1])
        ];
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        int[] expected = new int[101];
        expected[0] = 0;
        for (int step = 1; step < expected.Length; step++)
        {
            expected[step] = rows[expected[step - 1]].Sample(referenceGenerator);
        }

        int[] actual = chain.Path(actualGenerator, 0, 100);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void TransitionDataIsDefensivelyCopiedAndAliasRowsAreReused()
    {
        double[] matrix = [0.25, 0.75, 0.4, 0.6];
        MarkovChain chain = new MarkovChain(matrix, 2);
        AliasTable first = chain.TransitionTable(0);
        matrix[0] = double.NaN;
        matrix[1] = 0.0;

        Assert.Multiple(() =>
        {
            Assert.That(chain.StateCount(), Is.EqualTo(2));
            Assert.That(chain.TransitionProbability(0, 0), Is.EqualTo(0.25));
            Assert.That(chain.TransitionProbability(0, 1), Is.EqualTo(0.75));
            Assert.That(chain.TransitionProbability(1, 0), Is.EqualTo(0.4));
            Assert.That(chain.TransitionTable(0), Is.SameAs(first));
            Assert.That(first.CategoryCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void DirectNextStateMatchesTheExposedAliasRowExactly()
    {
        IMarkovChain chain = new MarkovChain([0.15, 0.55, 0.3, 0.6, 0.1, 0.3, 0.25, 0.25, 0.5], 3);
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator referenceGenerator = Create(202UL);
        int[] actual = new int[60];
        int[] expected = new int[60];
        int index = 0;
        while (index < actual.Length)
        {
            int currentState = index % chain.StateCount();
            actual[index] = chain.NextState(actualGenerator, currentState);
            expected[index] = chain.TransitionTable(currentState).Sample(referenceGenerator);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void DeterministicTransitionsConsumeNoRandomState()
    {
        MarkovChain chain = new MarkovChain(
        [
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0,
            1.0, 0.0, 0.0
        ], 3);
        RandomGenerator generator = Create(303UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        int[] actual = chain.Path(generator, 0, 8);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(new int[] { 0, 1, 2, 0, 1, 2, 0, 1, 2 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void BatchIsPathMajorAndMatchesRepeatedPathsAndFacade()
    {
        MarkovChain chain = new MarkovChain([0.2, 0.8, 0.65, 0.35], 2);
        const int pathCount = 7;
        const int stepCount = 19;
        RandomGenerator batchGenerator = Create(404UL);
        RandomGenerator repeatedGenerator = Create(404UL);
        RandomGenerator facadeGenerator = Create(404UL);
        int[] expected = new int[pathCount * (stepCount + 1)];
        for (int path = 0; path < pathCount; path++)
        {
            chain.Path(repeatedGenerator, 1, stepCount).CopyTo(expected, path * (stepCount + 1));
        }

        int[] actual = chain.Paths(batchGenerator, 1, pathCount, stepCount);
        int[] facade = RandomProcesses.MarkovChain(facadeGenerator, chain, 1, pathCount, stepCount);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(facade, Is.EqualTo(expected));
            Assert.That(actual.Where((value, index) => index % (stepCount + 1) == 0), Is.All.EqualTo(1));
            Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
            Assert.That(facadeGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void PathsNeverUseZeroProbabilityTransitions()
    {
        MarkovChain chain = new MarkovChain(
        [
            0.0, 0.25, 0.75,
            0.5, 0.0, 0.5,
            1.0, 0.0, 0.0
        ], 3);
        int[] path = chain.Path(Create(505UL), 0, 10000);

        for (int step = 1; step < path.Length; step++)
        {
            Assert.That(chain.TransitionProbability(path[step - 1], path[step]), Is.GreaterThan(0.0));
        }
    }

    [Test]
    public void AllocatingAndDestinationOverloadsReplayExactly()
    {
        MarkovChain chain = new MarkovChain([0.4, 0.6, 0.3, 0.7], 2);
        RandomGenerator allocating = Create(606UL);
        RandomGenerator destinationGenerator = Create(606UL);
        RandomGenerator facadeGenerator = Create(606UL);
        int[] expected = new int[chain.PathBatchOutputLength(4, 13)];
        int[] facade = new int[expected.Length];

        int[] actual = chain.Paths(allocating, 0, 4, 13);
        chain.PathsInto(destinationGenerator, 0, 4, 13, expected);
        RandomProcesses.MarkovChain(facadeGenerator, chain, 0, 4, 13, facade);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(facade, Is.EqualTo(expected));
            Assert.That(allocating.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
            Assert.That(facadeGenerator.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InvalidTransitionMatricesAreRejected()
    {
        AssertArgument("transitionMatrix", () => new MarkovChain(NullReference.Value<double[]>(), 2));
        AssertArgument("stateCount", () => new MarkovChain(Array.Empty<double>(), 0));
        AssertArgument("transitionMatrix", () => new MarkovChain(new double[3], 2));
        AssertArgument("transitionMatrix", () => new MarkovChain([1.0, -double.Epsilon, 0.0, 1.0], 2));
        AssertArgument("transitionMatrix", () => new MarkovChain([1.0, double.NaN, 0.0, 1.0], 2));
        AssertArgument("transitionMatrix", () => new MarkovChain([1.0, double.PositiveInfinity, 0.0, 1.0], 2));
        AssertArgument("transitionMatrix", () => new MarkovChain([1.0, Math.BitIncrement(1.0), 0.0, 1.0], 2));
        AssertArgument("transitionMatrix", () => new MarkovChain([0.4, 0.5, 0.0, 1.0], 2));
        AssertArgument("transitionMatrix", () => new MarkovChain([0.0, 0.0, 0.0, 1.0], 2));
    }

    [Test]
    public void InvalidPathRequestsAreAtomicAndLengthsIncludeInitialState()
    {
        MarkovChain chain = new MarkovChain([0.5, 0.5, 0.25, 0.75], 2);
        RandomGenerator generator = Create(808UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        int[] destination = [2, 3, 5];

        AssertArgument("generator", () => chain.Path(NullReference.Value<global::random.IRandomSource>(), 0, 1));
        AssertArgument("initialState", () => chain.Path(generator, -1, 1));
        AssertArgument("initialState", () => chain.Path(generator, 2, 1));
        AssertArgument("generator", () => chain.NextState(NullReference.Value<global::random.IRandomSource>(), 0));
        AssertArgument("currentState", () => chain.NextState(generator, 2));
        AssertArgument("currentState", () => chain.TransitionProbability(-1, 0));
        AssertArgument("nextState", () => chain.TransitionProbability(0, 2));
        AssertArgument("currentState", () => chain.TransitionTable(2));
        AssertArgument("destination", () => chain.PathInto(generator, 0, 3, destination));
        AssertArgument("chain", () => RandomProcesses.MarkovChain(generator, NullReference.Value<global::random.MarkovChain>(), 0, 1));
        AssertArgument("stepCount", () => chain.PathOutputLength(-1));
        AssertArgument("stepCount", () => chain.PathOutputLength(int.MaxValue));
        AssertArgument("pathCount", () => chain.PathBatchOutputLength(-1, 1));
        AssertArgument("pathCount", () => chain.PathBatchOutputLength(int.MaxValue, 1));

        Assert.Multiple(() =>
        {
            Assert.That(chain.PathOutputLength(7), Is.EqualTo(8));
            Assert.That(chain.PathBatchOutputLength(3, 7), Is.EqualTo(24));
            Assert.That(chain.Path(generator, 1, 0), Is.EqualTo(new int[] { 1 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new int[] { 2, 3, 5 }));
        });
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
