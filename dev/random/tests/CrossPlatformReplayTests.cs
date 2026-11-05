namespace random.Tests;

public class CrossPlatformReplayTests
{
    const double SystemTolerance = 1.0e-12;

    [Test]
    [Category("PortableReplay")]
    public void PortableMathMatchesExactCrossPlatformFixtures()
    {
        AssertExact("exponential", new ExponentialSampler(Create(101UL, RandomMathMode.Portable), 2.0).Sample);
        AssertExact("cauchy", new CauchySampler(Create(202UL, RandomMathMode.Portable), -1.0, 2.0).Sample);
        AssertExact("weibull", new WeibullSampler(Create(303UL, RandomMathMode.Portable), -0.5, 1.75, 1.25).Sample);
        AssertExact("gumbel", new GumbelSampler(Create(404UL, RandomMathMode.Portable), 0.75, 1.5).Sample);
    }

    [Test]
    [Category("SystemMathTolerance")]
    public void SystemMathMatchesCrossPlatformFixturesWithinTolerance()
    {
        AssertWithinTolerance("exponential", new ExponentialSampler(Create(101UL, RandomMathMode.System), 2.0).Sample);
        AssertWithinTolerance("cauchy", new CauchySampler(Create(202UL, RandomMathMode.System), -1.0, 2.0).Sample);
        AssertWithinTolerance("weibull", new WeibullSampler(Create(303UL, RandomMathMode.System), -0.5, 1.75, 1.25).Sample);
        AssertWithinTolerance("gumbel", new GumbelSampler(Create(404UL, RandomMathMode.System), 0.75, 1.5).Sample);
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }

    static void AssertExact(string samplerName, Func<double> sample)
    {
        string[] expected = LoadFixture("portable", samplerName);
        for (int index = 0; index < expected.Length; index++)
        {
            ulong expectedBits = Convert.ToUInt64(expected[index], 16);
            ulong actualBits = BitConverter.DoubleToUInt64Bits(sample());
            Assert.That(actualBits, Is.EqualTo(expectedBits), $"{samplerName} sample {index}");
        }
    }

    static void AssertWithinTolerance(string samplerName, Func<double> sample)
    {
        string[] expected = LoadFixture("system", samplerName);
        for (int index = 0; index < expected.Length; index++)
        {
            double expectedValue = double.Parse(expected[index], System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(
                sample(),
                Is.EqualTo(expectedValue).Within(SystemTolerance),
                $"{samplerName} sample {index}");
        }
    }

    static string[] LoadFixture(string mode, string samplerName)
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "TestData",
            "cross-platform-replay-v1.txt"));
        string[] lines = File.ReadAllLines(path);
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            string[] fields = line.Split('|');
            if (fields.Length == 3 && fields[0] == mode && fields[1] == samplerName)
            {
                return fields[2].Split(',');
            }
        }

        Assert.Fail($"Missing {mode} fixture for {samplerName}.");
        return [];
    }
}
