namespace random.Tests;

public class HmacDrbgCavpTests
{
    [Test]
    public void NoReseedVectorMatchesSecondGenerateResult()
    {
        string[] lines = ReadVector("hmac-drbg-sha256-no-reseed.txt");
        HmacDrbgSha256Engine engine = Instantiate(lines);
        byte[][] additionalInput = Fields(lines, "AdditionalInput");
        byte[] discarded = new byte[128];
        byte[] actual = new byte[128];

        engine.GenerateWithAdditionalInput(discarded, additionalInput[0]);
        engine.GenerateWithAdditionalInput(actual, additionalInput[1]);

        Assert.That(actual, Is.EqualTo(Field(lines, "ReturnedBits")));
    }

    [Test]
    public void ExplicitReseedVectorMatchesSecondGenerateResult()
    {
        string[] lines = ReadVector("hmac-drbg-sha256-reseed.txt");
        HmacDrbgSha256Engine engine = Instantiate(lines);
        byte[][] additionalInput = Fields(lines, "AdditionalInput");
        byte[] discarded = new byte[128];
        byte[] actual = new byte[128];

        engine.Reseed(Field(lines, "EntropyInputReseed"), Field(lines, "AdditionalInputReseed"));
        engine.GenerateWithAdditionalInput(discarded, additionalInput[0]);
        engine.GenerateWithAdditionalInput(actual, additionalInput[1]);

        Assert.That(actual, Is.EqualTo(Field(lines, "ReturnedBits")));
    }

    [Test]
    public void PredictionResistanceVectorMatchesSecondGenerateResult()
    {
        string[] lines = ReadVector("hmac-drbg-sha256-prediction-resistance.txt");
        HmacDrbgSha256Engine engine = Instantiate(lines);
        byte[][] additionalInput = Fields(lines, "AdditionalInput");
        byte[][] predictionResistanceEntropy = Fields(lines, "EntropyInputPR");
        byte[] discarded = new byte[128];
        byte[] actual = new byte[128];

        engine.GeneratePredictionResistant(discarded, additionalInput[0], predictionResistanceEntropy[0]);
        engine.GeneratePredictionResistant(actual, additionalInput[1], predictionResistanceEntropy[1]);

        Assert.That(actual, Is.EqualTo(Field(lines, "ReturnedBits")));
    }

    static HmacDrbgSha256Engine Instantiate(string[] lines)
    {
        return new HmacDrbgSha256Engine(
            Field(lines, "EntropyInput"),
            Field(lines, "Nonce"),
            Field(lines, "PersonalizationString"));
    }

    static byte[] Field(string[] lines, string name)
    {
        byte[][] values = Fields(lines, name);
        Assert.That(values, Has.Length.EqualTo(1), $"Expected one {name} field.");
        return values[0];
    }

    static byte[][] Fields(string[] lines, string name)
    {
        string prefix = name + " = ";
        return lines
            .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
            .Select(line => Convert.FromHexString(line.Substring(prefix.Length).Trim()))
            .ToArray();
    }

    static string[] ReadVector(string fileName)
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "TestData",
            fileName));
        return File.ReadAllLines(path);
    }
}
