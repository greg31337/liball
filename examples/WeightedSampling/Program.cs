using System.Globalization;
using random;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var factory = new RandomFactory();
var generator = factory.CreateDefaultGenerator(123, RandomMathMode.Portable);

// Relative weights 1:3:6 normalize to probabilities 10%, 30%, and 60%.
var sampler = factory.CreateAliasWeightedSampler(generator, new double[] { 1, 3, 6 });
string[] names = {
    "Bronze",
    "Silver",
    "Gold"
};

Console.WriteLine("10,000 weighted draws; expected shares: 10%, 30%, 60%");
int[] counts = new int[3];
for (int i = 0; i < 10000; i++)
{
    counts[sampler.SampleIndex()]++;
}

for (int i = 0; i < counts.Length; i++)
{
    Console.WriteLine($"{names[i],-6} {counts[i],5} ({counts[i] / 100.0:F2}%)");
}

// Setting a weight to zero removes that category from future draws.
sampler.UpdateWeights(new double[] { 0, 0, 1 });
int[] afterUpdate = new int[20];
sampler.FillIndices(afterUpdate);
Console.WriteLine(
    $"After setting weights to [0, 0, 1]: {afterUpdate.Count(i => i == 2)} of 20 draws are Gold.");
if (afterUpdate.Any(i => i != 2))
{
    throw new InvalidOperationException("Zero-weight categories were selected.");
}
