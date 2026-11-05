namespace sdp.Tests;

public static class SdpFixtureRoundTripAssert
{
    public static void AssertCanonical(string input, SdpParseOptions parseOptions)
    {
        ISdpParser parser = new SdpParser();
        ISdpWriter writer = new SdpWriter();
        SdpParseResult parsed = parser.ParseText(input, parseOptions);

        Assert.That(parsed.Success, Is.True, ParseDiagnostics(parsed));

        SdpWriteResult firstWrite = writer.WriteText(parsed.Description!, new SdpWriteOptions());

        Assert.That(firstWrite.Success, Is.True, WriteDiagnostics(firstWrite));

        SdpParseResult reparsed = parser.ParseText(firstWrite.Text, parseOptions);

        Assert.That(reparsed.Success, Is.True, ParseDiagnostics(reparsed));

        SdpComparisonResult comparison = new SdpComparer().CompareDescriptions(
            parsed.Description!,
            reparsed.Description!,
            new SdpComparisonOptions
            {
                CompareSemanticValues = true,
                IgnoreInsignificantOrdering = true,
                IgnoreRawFormatting = true
            });
        SdpWriteResult secondWrite = writer.WriteText(reparsed.Description!, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(comparison.Equal, Is.True, DifferenceText(comparison));
            Assert.That(secondWrite.Success, Is.True, WriteDiagnostics(secondWrite));
            Assert.That(secondWrite.Text, Is.EqualTo(firstWrite.Text));
        });
    }

    static string ParseDiagnostics(SdpParseResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string WriteDiagnostics(SdpWriteResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string DifferenceText(SdpComparisonResult result)
    {
        return string.Join("|", result.Differences.Select(difference => difference.Path.FieldName + ":" + difference.Detail));
    }
}
