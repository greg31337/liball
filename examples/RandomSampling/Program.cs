using System.Globalization;
using random;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var factory = new RandomFactory();
var generator = factory.CreateDefaultGenerator(42, RandomMathMode.Portable);
Console.WriteLine("Repeatable sampling with seed 42");
int[] counts = new int[6];
for (int i = 0; i < 6000; i++)
{
    counts[generator.NextInt32Bounded(6)]++;
}

Console.WriteLine("6,000 dice rolls; each # represents about 50 rolls");
for (int i = 0; i < counts.Length; i++)
{
    Console.WriteLine($"{i + 1}: {counts[i],4} {new string('#', (int)Math.Round(counts[i] / 50.0))}");
}

var normal = factory.CreateStandardNormalSampler(generator);
double[] samples = Enumerable.Range(0, 5000).Select(_ => normal.Sample()).ToArray();
double mean = samples.Average();
double deviation = Math.Sqrt(samples.Average(value => (value - mean) * (value - mean)));
Console.WriteLine($"5,000 standard-normal samples: mean={mean:F3}, standard deviation={deviation:F3}");

// Save the generator state to resume the exact sequence, not just the initial seed.
var state = generator.ExportState();
int next = generator.NextInt32Bounded(1000);
int replay = factory.RestoreGenerator(state, RandomMathMode.Portable).NextInt32Bounded(1000);
if (next != replay)
{
    throw new InvalidOperationException("Generator state did not replay.");
}

Console.WriteLine($"Saved state replay: {next} = {replay}");
string[] names = {
    "Ada",
    "Grace",
    "Linus",
    "Margaret",
    "Ken"
};

new CollectionSampler(generator).ShuffleArray(names);
Console.WriteLine($"Shuffled team: {string.Join(", ", names)}");
