using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8CoefficientTokenWriterTests
{
    [Test]
    public void GenerateTokens_EmitsZerosNonZerosCategoriesAndEob()
    {
        int[] coefficients = new int[16];
        coefficients[1] = -1;
        coefficients[3] = 6;
        coefficients[4] = -19;

        List<WebpVp8EncodedCoefficientToken> tokens = WebpVp8CoefficientTokenWriter.GenerateTokens(coefficients, 0, 0);

        Assert.That(tokens.Count, Is.EqualTo(6));
        Assert.That(tokens[0].Token, Is.EqualTo(WebpVp8CoefficientToken.Dct0));
        Assert.That(tokens[1].Token, Is.EqualTo(WebpVp8CoefficientToken.Dct1));
        Assert.That(tokens[1].SignBit, Is.EqualTo(1));
        Assert.That(tokens[2].Token, Is.EqualTo(WebpVp8CoefficientToken.Dct0));
        Assert.That(tokens[3].Token, Is.EqualTo(WebpVp8CoefficientToken.DctCat1));
        Assert.That(tokens[3].ExtraBits, Is.EqualTo(1));
        Assert.That(tokens[4].Token, Is.EqualTo(WebpVp8CoefficientToken.DctCat4));
        Assert.That(tokens[4].SignBit, Is.EqualTo(1));
        Assert.That(tokens[5].Token, Is.EqualTo(WebpVp8CoefficientToken.DctEob));
    }

    [Test]
    public void WriteBlock_RoundTripsThroughCoefficientDecoder()
    {
        int[] coefficients = new int[16];
        coefficients[0] = 0;
        coefficients[1] = -1;
        coefficients[2] = 2;
        coefficients[3] = 6;
        coefficients[4] = -19;
        coefficients[5] = 70;
        WebpVp8CoefficientProbabilityTable probabilities = NewProbabilityTable(173);
        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();

        int endingContext = WebpVp8CoefficientTokenWriter.WriteBlock(writer, probabilities, coefficients, WebpVp8CoefficientDecoder.PlaneYWithoutY2, 0, 0);
        WebpVp8CoefficientBlock decoded = WebpVp8CoefficientDecoder.DecodeBlock(new WebpVp8BoolDecoder(writer.ToBytes()), probabilities, WebpVp8CoefficientDecoder.PlaneYWithoutY2, 0, 0);

        Assert.That(endingContext, Is.EqualTo(1));
        Assert.That(decoded.HasCoefficients, Is.True);
        Assert.That(decoded.EndingContext, Is.EqualTo(1));
        int index = 0;
        while (index < coefficients.Length)
        {
            Assert.That(decoded.Coefficients[index], Is.EqualTo(coefficients[index]));
            index = index + 1;
        }
    }

    [Test]
    public void WriteBlock_RoundTripsAllZeroBlock()
    {
        int[] coefficients = new int[16];
        WebpVp8CoefficientProbabilityTable probabilities = NewProbabilityTable(149);
        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();

        int endingContext = WebpVp8CoefficientTokenWriter.WriteBlock(writer, probabilities, coefficients, WebpVp8CoefficientDecoder.PlaneUv, 0, 0);
        WebpVp8CoefficientBlock decoded = WebpVp8CoefficientDecoder.DecodeBlock(new WebpVp8BoolDecoder(writer.ToBytes()), probabilities, WebpVp8CoefficientDecoder.PlaneUv, 0, 0);

        Assert.That(endingContext, Is.EqualTo(0));
        Assert.That(decoded.HasCoefficients, Is.False);
        Assert.That(decoded.EndingContext, Is.EqualTo(0));
        Assert.That(decoded.Coefficients, Is.All.EqualTo(0));
    }

    [Test]
    public void WriteBlock_RejectsInvalidInputs()
    {
        WebpVp8BoolWriter writer = new WebpVp8BoolWriter();
        WebpVp8CoefficientProbabilityTable probabilities = new WebpVp8CoefficientProbabilityTable();
        int[] coefficients = new int[16];

        Assert.Throws<ArgumentNullException>(() => WebpVp8CoefficientTokenWriter.GenerateTokens(null!, 0, 0));
        Assert.Throws<ArgumentException>(() => WebpVp8CoefficientTokenWriter.GenerateTokens(new int[15], 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientTokenWriter.GenerateTokens(coefficients, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientTokenWriter.GenerateTokens(coefficients, 16, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientTokenWriter.GenerateTokens(coefficients, 0, 3));
        Assert.Throws<ArgumentNullException>(() => WebpVp8CoefficientTokenWriter.WriteBlock(null!, probabilities, coefficients, 0, 0, 0));
        Assert.Throws<ArgumentNullException>(() => WebpVp8CoefficientTokenWriter.WriteBlock(writer, null!, coefficients, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8CoefficientTokenWriter.WriteBlock(writer, probabilities, coefficients, 4, 0, 0));
    }

    static WebpVp8CoefficientProbabilityTable NewProbabilityTable(byte probability)
    {
        WebpVp8CoefficientProbabilityTable table = new WebpVp8CoefficientProbabilityTable();
        int index = 0;
        while (index < table.Values.Length)
        {
            table.Values[index] = probability;
            index = index + 1;
        }

        return table;
    }
}
