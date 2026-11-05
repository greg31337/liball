namespace random.Tests;

public class StochasticProcessStatisticalContractTests
{
    [Test]
    public void RandomWalkIncrementsHaveExactSupportAndEndpointMoments()
    {
        const int pathCount = 20000;
        const int stepCount = 16;
        const long initialState = 7L;
        const long stepSize = 3L;
        long[] paths = RandomProcesses.RandomWalks(
            Create(101UL),
            initialState,
            stepSize,
            pathCount,
            stepCount);
        double terminalSum = 0.0;
        double terminalSquareSum = 0.0;
        int positiveIncrements = 0;

        for (int path = 0; path < pathCount; path++)
        {
            int offset = path * (stepCount + 1);
            Assert.That(paths[offset], Is.EqualTo(initialState));
            for (int step = 1; step <= stepCount; step++)
            {
                long increment = paths[offset + step] - paths[offset + step - 1];
                Assert.That(increment == stepSize || increment == -stepSize, Is.True);
                if (increment > 0L)
                {
                    positiveIncrements++;
                }
            }

            double terminal = paths[offset + stepCount];
            terminalSum += terminal;
            terminalSquareSum += terminal * terminal;
        }

        double mean = terminalSum / pathCount;
        double variance = terminalSquareSum / pathCount - mean * mean;
        Assert.Multiple(() =>
        {
            Assert.That(positiveIncrements, Is.InRange(158500, 161500));
            Assert.That(mean, Is.EqualTo((double)initialState).Within(0.35));
            Assert.That(variance, Is.EqualTo((double)(stepCount * stepSize * stepSize)).Within(3.0));
        });
    }

    [Test]
    public void BrownianIncrementsAndMotionMatchIndependentGaussianLaws()
    {
        const int pathCount = 30000;
        const int incrementCount = 4;
        const double timeStep = 0.25;
        double[] increments = RandomProcesses.BrownianIncrements(
            Create(201UL),
            timeStep,
            pathCount,
            incrementCount);
        double incrementSum = 0.0;
        double incrementSquareSum = 0.0;
        double adjacentProductSum = 0.0;
        for (int path = 0; path < pathCount; path++)
        {
            int offset = path * incrementCount;
            for (int step = 0; step < incrementCount; step++)
            {
                double increment = increments[offset + step];
                Assert.That(double.IsFinite(increment), Is.True);
                incrementSum += increment;
                incrementSquareSum += increment * increment;
                if (step != 0)
                {
                    adjacentProductSum += increment * increments[offset + step - 1];
                }
            }
        }

        int incrementSampleCount = pathCount * incrementCount;
        double incrementMean = incrementSum / incrementSampleCount;
        double incrementVariance = incrementSquareSum / incrementSampleCount - incrementMean * incrementMean;
        double adjacentProduct = adjacentProductSum / (pathCount * (incrementCount - 1));

        const int stepCount = 8;
        const double initialState = -1.0;
        const double drift = 0.4;
        const double volatility = 0.8;
        double[] motion = RandomProcesses.BrownianMotion(
            Create(202UL),
            initialState,
            drift,
            volatility,
            timeStep,
            pathCount,
            stepCount);
        double[] terminal = PathTerminalMoments(motion, pathCount, stepCount);
        double horizon = timeStep * stepCount;

        Assert.Multiple(() =>
        {
            Assert.That(incrementMean, Is.EqualTo(0.0).Within(0.004));
            Assert.That(incrementVariance, Is.EqualTo(timeStep).Within(0.004));
            Assert.That(adjacentProduct, Is.EqualTo(0.0).Within(0.004));
            Assert.That(terminal[0], Is.EqualTo(initialState + drift * horizon).Within(0.02));
            Assert.That(terminal[1], Is.EqualTo(volatility * volatility * horizon).Within(0.04));
        });
    }

    [Test]
    public void BridgeGeometricAndMeanRevertingPathsRespectSupportAndEndpointLaws()
    {
        const int pathCount = 30000;
        const int stepCount = 8;
        const double timeStep = 0.25;
        double[] bridges = RandomProcesses.BrownianBridge(
            Create(301UL),
            -2.0,
            3.0,
            1.2,
            timeStep,
            pathCount,
            stepCount);
        double midpointSum = 0.0;
        double midpointSquareSum = 0.0;
        for (int path = 0; path < pathCount; path++)
        {
            int offset = path * (stepCount + 1);
            Assert.That(bridges[offset], Is.EqualTo(-2.0));
            Assert.That(bridges[offset + stepCount], Is.EqualTo(3.0));
            double midpoint = bridges[offset + stepCount / 2];
            midpointSum += midpoint;
            midpointSquareSum += midpoint * midpoint;
        }

        double midpointMean = midpointSum / pathCount;
        double midpointVariance = midpointSquareSum / pathCount - midpointMean * midpointMean;

        const double geometricInitial = 10.0;
        const double geometricDrift = 0.05;
        const double geometricVolatility = 0.3;
        double[] geometric = RandomProcesses.GeometricBrownianMotion(
            Create(302UL),
            geometricInitial,
            geometricDrift,
            geometricVolatility,
            timeStep,
            pathCount,
            stepCount);
        double logReturnSum = 0.0;
        double logReturnSquareSum = 0.0;
        for (int path = 0; path < pathCount; path++)
        {
            int offset = path * (stepCount + 1);
            for (int step = 0; step <= stepCount; step++)
            {
                Assert.That(geometric[offset + step], Is.GreaterThan(0.0));
            }

            double logReturn = Math.Log(geometric[offset + stepCount] / geometricInitial);
            logReturnSum += logReturn;
            logReturnSquareSum += logReturn * logReturn;
        }

        double logReturnMean = logReturnSum / pathCount;
        double logReturnVariance = logReturnSquareSum / pathCount - logReturnMean * logReturnMean;

        const double ornsteinInitial = -3.0;
        const double longTermMean = 2.0;
        const double meanReversion = 0.7;
        const double ornsteinVolatility = 0.4;
        double[] ornstein = RandomProcesses.OrnsteinUhlenbeck(
            Create(303UL),
            ornsteinInitial,
            longTermMean,
            meanReversion,
            ornsteinVolatility,
            timeStep,
            pathCount,
            stepCount);
        double[] ornsteinTerminal = PathTerminalMoments(ornstein, pathCount, stepCount);
        double horizon = timeStep * stepCount;
        double decay = Math.Exp(-meanReversion * horizon);
        double expectedOrnsteinMean = longTermMean + (ornsteinInitial - longTermMean) * decay;
        double expectedOrnsteinVariance = ornsteinVolatility * ornsteinVolatility *
            (1.0 - Math.Exp(-2.0 * meanReversion * horizon)) / (2.0 * meanReversion);

        Assert.Multiple(() =>
        {
            Assert.That(midpointMean, Is.EqualTo(0.5).Within(0.02));
            Assert.That(midpointVariance, Is.EqualTo(0.72).Within(0.03));
            Assert.That(logReturnMean, Is.EqualTo((geometricDrift - 0.5 * geometricVolatility * geometricVolatility) * horizon).Within(0.01));
            Assert.That(logReturnVariance, Is.EqualTo(geometricVolatility * geometricVolatility * horizon).Within(0.01));
            Assert.That(ornsteinTerminal[0], Is.EqualTo(expectedOrnsteinMean).Within(0.01));
            Assert.That(ornsteinTerminal[1], Is.EqualTo(expectedOrnsteinVariance).Within(0.005));
        });
    }

    [Test]
    public void PoissonPathsHaveOrderedSupportAndIndependentIncrementMoments()
    {
        const double rate = 2.5;
        const double horizon = 4.0;
        const int arrivalPathCount = 5000;
        RandomGenerator arrivalGenerator = Create(401UL);
        double arrivalCountSum = 0.0;
        double arrivalCountSquareSum = 0.0;
        for (int path = 0; path < arrivalPathCount; path++)
        {
            double[] arrivals = RandomProcesses.PoissonArrivals(arrivalGenerator, rate, horizon);
            for (int index = 0; index < arrivals.Length; index++)
            {
                Assert.That(arrivals[index], Is.InRange(0.0, Math.BitDecrement(horizon)));
                if (index != 0)
                {
                    Assert.That(arrivals[index], Is.GreaterThanOrEqualTo(arrivals[index - 1]));
                }
            }

            arrivalCountSum += arrivals.Length;
            arrivalCountSquareSum += arrivals.Length * arrivals.Length;
        }

        double arrivalMean = arrivalCountSum / arrivalPathCount;
        double arrivalVariance = arrivalCountSquareSum / arrivalPathCount - arrivalMean * arrivalMean;

        const int countPathCount = 25000;
        const int intervalCount = 4;
        long[] counts = RandomProcesses.PoissonCounts(
            Create(402UL),
            rate,
            horizon,
            countPathCount,
            intervalCount);
        double incrementSum = 0.0;
        double incrementSquareSum = 0.0;
        for (int path = 0; path < countPathCount; path++)
        {
            int offset = path * (intervalCount + 1);
            Assert.That(counts[offset], Is.Zero);
            for (int interval = 1; interval <= intervalCount; interval++)
            {
                long increment = counts[offset + interval] - counts[offset + interval - 1];
                Assert.That(increment, Is.GreaterThanOrEqualTo(0L));
                incrementSum += increment;
                incrementSquareSum += increment * increment;
            }
        }

        int incrementCount = countPathCount * intervalCount;
        double incrementMean = incrementSum / incrementCount;
        double incrementVariance = incrementSquareSum / incrementCount - incrementMean * incrementMean;
        double expectedTotal = rate * horizon;
        double expectedIncrement = expectedTotal / intervalCount;
        Assert.Multiple(() =>
        {
            Assert.That(arrivalMean, Is.EqualTo(expectedTotal).Within(0.10));
            Assert.That(arrivalVariance, Is.EqualTo(expectedTotal).Within(0.40));
            Assert.That(incrementMean, Is.EqualTo(expectedIncrement).Within(0.025));
            Assert.That(incrementVariance, Is.EqualTo(expectedIncrement).Within(0.06));
        });
    }

    [Test]
    public void MarkovPathsUseSupportedEdgesAndMatchEveryTransitionRow()
    {
        const int sampleCount = 40000;
        double[] probabilities =
        [
            0.1, 0.6, 0.3,
            0.25, 0.0, 0.75,
            0.0, 0.4, 0.6
        ];
        MarkovChain chain = new MarkovChain(probabilities, 3);
        RandomGenerator generator = Create(501UL);
        int[,] counts = new int[3, 3];
        for (int current = 0; current < 3; current++)
        {
            for (int sample = 0; sample < sampleCount; sample++)
            {
                counts[current, chain.NextState(generator, current)]++;
            }
        }

        int[] path = chain.Path(Create(502UL), 0, 10000);
        for (int step = 1; step < path.Length; step++)
        {
            Assert.That(chain.TransitionProbability(path[step - 1], path[step]), Is.GreaterThan(0.0));
        }

        Assert.Multiple(() =>
        {
            for (int current = 0; current < 3; current++)
            {
                for (int next = 0; next < 3; next++)
                {
                    double frequency = (double)counts[current, next] / sampleCount;
                    Assert.That(
                        frequency,
                        Is.EqualTo(probabilities[current * 3 + next]).Within(0.012),
                        $"transition {current}->{next}");
                }
            }
        });
    }

    static double[] PathTerminalMoments(double[] paths, int pathCount, int stepCount)
    {
        double sum = 0.0;
        double squareSum = 0.0;
        int statesPerPath = stepCount + 1;
        for (int path = 0; path < pathCount; path++)
        {
            double value = paths[path * statesPerPath + stepCount];
            Assert.That(double.IsFinite(value), Is.True);
            sum += value;
            squareSum += value * value;
        }

        double mean = sum / pathCount;
        return [mean, squareSum / pathCount - mean * mean];
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
