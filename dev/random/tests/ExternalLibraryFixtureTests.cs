namespace random.Tests;

public class ExternalLibraryFixtureTests
{
    const double UInt32Unit = 1.0 / 4294967296.0;

    [Test]
    public void ReferenceMt19937WordsMatchMathNetNumericsFive()
    {
        // Math.NET MersenneTwister.NextDouble exposes one reference MT19937 word on a 2^-32 grid.
        // Compare at that genuinely shared algorithm boundary; the libraries' wider transforms differ.
        MathNet.Numerics.Random.MersenneTwister reference = new MathNet.Numerics.Random.MersenneTwister(5489, false);
        RandomGenerator actual = RandomGenerator.CreateMt19937(5489U, RandomMathMode.System);

        for (int index = 0; index < 1000; index++)
        {
            double expected = reference.NextDouble();
            double normalizedWord = (double)actual.NextUInt32() * UInt32Unit;
            Assert.That(normalizedWord, Is.EqualTo(expected), $"MT19937 word {index}");
        }
    }

    [Test]
    public void NumpySeedSequenceMt19937WordsMatchCheckedInFixture()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "TestData",
            "numpy-mt19937-array-seed-2.0.2.txt"));
        string[] lines = File.ReadAllLines(path);
        RandomGenerator actual = RandomGenerator.CreateMt19937Numpy(
            [0x12345678U, 0x9ABCDEF0U],
            RandomMathMode.System);
        int fixtureCount = 0;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            uint expected = Convert.ToUInt32(line, 16);
            Assert.That(actual.NextUInt32(), Is.EqualTo(expected), $"NumPy fixture word {fixtureCount}");
            fixtureCount++;
        }

        Assert.That(fixtureCount, Is.EqualTo(32));
    }
}
