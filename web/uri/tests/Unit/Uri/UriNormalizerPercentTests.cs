namespace uri.Tests;

public class UriNormalizerPercentTests
{
    [TestCase(UriComponent.UserInfo, "User%7eName%2f%af", "User~Name%2F%AF")]
    [TestCase(UriComponent.Path, "/A%2fb%41", "/A%2FbA")]
    [TestCase(UriComponent.Query, "Q=%41%26%7e", "Q=A%26~")]
    [TestCase(UriComponent.Fragment, "F%2f%ff%5a", "F%2F%FFZ")]
    public void ComponentNormalizationDecodesUnreservedAndUppercasesRemainingTriplets(
        UriComponent component,
        string text,
        string expected)
    {
        PercentNormalizeResult result = UriNormalizerCore.NormalizeComponentPercentEncoding(
            text.AsSpan(),
            expected.Length,
            component,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [TestCase("EXAMPLE.COM", "example.com")]
    [TestCase("%45XAMPLE.%43OM", "example.com")]
    [TestCase("EXAMPLE%2fCOM", "example%2Fcom")]
    [TestCase("A%7eB", "a~b")]
    [TestCase("%c3%84.EXAMPLE", "%C3%84.example")]
    [TestCase("%25EXAMPLE", "%25example")]
    public void RegisteredNameNormalizationAppliesPercentRulesBeforeHostCase(
        string host,
        string expected)
    {
        PercentNormalizeResult result = UriNormalizerCore.NormalizeRegisteredName(
            host.AsSpan(),
            expected.Length,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [TestCase(UriComponent.UserInfo, "User%41", "UserA")]
    [TestCase(UriComponent.Path, "/Case%41", "/CaseA")]
    [TestCase(UriComponent.Query, "Query%41", "QueryA")]
    [TestCase(UriComponent.Fragment, "Fragment%41", "FragmentA")]
    public void PercentNormalizationDoesNotFoldCaseSensitiveComponents(
        UriComponent component,
        string text,
        string expected)
    {
        PercentNormalizeResult result = UriNormalizerCore.NormalizeComponentPercentEncoding(
            text.AsSpan(),
            expected.Length,
            component,
            0);

        Assert.That(result.Text, Is.EqualTo(expected));
    }

    [Test]
    public void ReservedCharactersRemainEncodedInEveryGenericComponent()
    {
        const string text = "%3a%2f%3f%23%5b%5d%40%21%24%26%27%28%29%2a%2b%2c%3b%3d";

        PercentNormalizeResult result = UriNormalizerCore.NormalizeComponentPercentEncoding(
            text.AsSpan(),
            text.Length,
            UriComponent.Path,
            0);

        Assert.That(
            result.Text,
            Is.EqualTo("%3A%2F%3F%23%5B%5D%40%21%24%26%27%28%29%2A%2B%2C%3B%3D"));
    }

    [Test]
    public void MalformedTripletDiagnosticKeepsComponentAndAbsoluteOffset()
    {
        PercentNormalizeResult result = UriNormalizerCore.NormalizeComponentPercentEncoding(
            "ok%G0".AsSpan(),
            100,
            UriComponent.Query,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Query));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(23));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void OutputLimitUsesTheDecodedComponentLength()
    {
        PercentNormalizeResult exact = UriNormalizerCore.NormalizeComponentPercentEncoding(
            "%41%42".AsSpan(),
            2,
            UriComponent.Path,
            0);
        PercentNormalizeResult shortResult = UriNormalizerCore.NormalizeComponentPercentEncoding(
            "%41%42".AsSpan(),
            1,
            UriComponent.Path,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Text, Is.EqualTo("AB"));
            Assert.That(shortResult.Success, Is.False);
            Assert.That(shortResult.Text, Is.Empty);
            Assert.That(shortResult.Diagnostic, Is.Not.Null);
            Assert.That(shortResult.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(shortResult.Diagnostic.Component, Is.EqualTo(UriComponent.Output));
            Assert.That(shortResult.Diagnostic.Range.Offset, Is.EqualTo(1));
            Assert.That(shortResult.Diagnostic.Range.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void NormalizationConsumesOnlyTheProvidedSpan()
    {
        const string component = "A%7eb%2fC";
        string padded = "before" + component + "after";

        PercentNormalizeResult result = UriNormalizerCore.NormalizeComponentPercentEncoding(
            padded.AsSpan(6, component.Length),
            component.Length,
            UriComponent.Path,
            0);

        Assert.That(result.Text, Is.EqualTo("A~b%2FC"));
    }

    [TestCase("A%7eb%2fC")]
    [TestCase("%45XAMPLE.%43OM")]
    [TestCase("%c3%84%25")]
    public void IntegratedNormalizationIsIdempotent(string text)
    {
        PercentNormalizeResult first = UriNormalizerCore.NormalizeComponentPercentEncoding(
            text.AsSpan(),
            100,
            UriComponent.Path,
            0);
        PercentNormalizeResult second = UriNormalizerCore.NormalizeComponentPercentEncoding(
            first.Text.AsSpan(),
            100,
            UriComponent.Path,
            0);

        Assert.That(second.Text, Is.EqualTo(first.Text));
    }

    [TestCase("%45XAMPLE.%43OM")]
    [TestCase("A%7eB%2fC")]
    [TestCase("%c3%84.EXAMPLE")]
    public void RegisteredNameNormalizationIsIdempotent(string host)
    {
        PercentNormalizeResult first = UriNormalizerCore.NormalizeRegisteredName(
            host.AsSpan(),
            100,
            0);
        PercentNormalizeResult second = UriNormalizerCore.NormalizeRegisteredName(
            first.Text.AsSpan(),
            100,
            0);

        Assert.That(second.Text, Is.EqualTo(first.Text));
    }
}
