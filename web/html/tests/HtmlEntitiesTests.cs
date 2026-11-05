
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlEntitiesTests
{
    [Test]
    public void DecodeTextAppliesNamedNumericAndRecoveryRules()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlEntityResult result = HtmlEntities.DecodeText(
            "A &amp; &#x80; &NotEqualTilde; &bogus; &notit;", limits, CancellationToken.None);

        Assert.That(result.GetString(), Is.EqualTo("A & € ≂̸ &bogus; ¬it;"));
        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.WasLimitExceeded(), Is.False);
        Assert.That(result.GetDiagnostics().Select(value => value.GetCode()), Is.EqualTo(new[]
        {
            HtmlDiagnosticCode.CharacterReferenceInvalid,
            HtmlDiagnosticCode.CharacterReferenceInvalid,
            HtmlDiagnosticCode.CharacterReferenceMissingSemicolon
        }));
        Assert.That(HtmlEntities.DecodeText("&lt;p&gt;&amp;&lt;/p&gt;"), Is.EqualTo("<p>&</p>"));
        Assert.That(HtmlEntities.Decode("&copy;"), Is.EqualTo("©"));
    }

    [Test]
    public void DecodeAttributePreservesHistoricalAmbiguousAmpersands()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlEntityResult result = HtmlEntities.DecodeAttribute(
            "&amp=1 &amp; &notit; &#65;", limits, CancellationToken.None);

        Assert.That(result.GetString(), Is.EqualTo("&amp=1 & &notit; A"));
        Assert.That(result.GetDiagnostics(), Is.Empty);
        Assert.That(HtmlEntities.DecodeAttribute("&quot;x&quot;"), Is.EqualTo("\"x\""));
        Assert.That(HtmlEntities.Decode("&amp=1", HtmlEntityContext.Attribute), Is.EqualTo("&amp=1"));
    }

    [Test]
    public void MinimalModeUsesTheHtmlSerializationEscapeSet()
    {
        string input = "<&>\u00A0\"'";

        Assert.That(HtmlEntities.EncodeText(input, HtmlEntityEncoding.Minimal),
            Is.EqualTo("&lt;&amp;&gt;&nbsp;\"'"));
        Assert.That(HtmlEntities.EncodeAttribute(input, HtmlEntityEncoding.Minimal),
            Is.EqualTo("&lt;&amp;&gt;&nbsp;&quot;'"));
        Assert.That(HtmlEntities.Encode("<&>", HtmlEntityEncoding.Minimal),
            Is.EqualTo("&lt;&amp;&gt;"));
        Assert.That(HtmlEntities.Encode("\"&", HtmlEntityContext.Attribute, HtmlEntityEncoding.Minimal),
            Is.EqualTo("&quot;&amp;"));
    }

    [Test]
    public void ExtendedEncodingModesChooseNamedNumericAndAsciiOnlyForms()
    {
        Assert.That(HtmlEntities.EncodeText("© é fj 💩", HtmlEntityEncoding.Named),
            Is.EqualTo("&copy; &eacute; &fjlig; 💩"));
        Assert.That(HtmlEntities.EncodeText("© & 💩 A", HtmlEntityEncoding.Numeric),
            Is.EqualTo("&#xA9; &#x26; &#x1F4A9; A"));

        string ascii = HtmlEntities.EncodeText("© & 💩 é", HtmlEntityEncoding.AsciiOnly);
        Assert.That(ascii, Is.EqualTo("&copy; &amp; &#x1F4A9; &eacute;"));
        Assert.That(ascii.All(value => value <= 127), Is.True);
    }

    [Test]
    public void EncodedValuesDecodeBackInTheirOriginalContext()
    {
        string text = "<© & ≂̸ 💩>";
        foreach (HtmlEntityEncoding mode in Enum.GetValues<HtmlEntityEncoding>())
        {
            string encoded = HtmlEntities.EncodeText(text, mode);
            Assert.That(HtmlEntities.DecodeText(encoded), Is.EqualTo(text), mode.ToString());
        }

        string attribute = "\"© & ≂̸ 💩>";
        foreach (HtmlEntityEncoding mode in Enum.GetValues<HtmlEntityEncoding>())
        {
            string encoded = HtmlEntities.EncodeAttribute(attribute, mode);
            Assert.That(HtmlEntities.DecodeAttribute(encoded), Is.EqualTo(attribute), mode.ToString());
        }
    }

    [Test]
    public void FullOverloadsEnforceInputAndAtomicOutputLimits()
    {
        HtmlLimits inputLimits = HtmlLimits.CreateDefault();
        inputLimits.SetMaximumInputCharacters(3);
        HtmlEntityResult inputLimited = HtmlEntities.DecodeText(
            "abc&amp;", inputLimits, CancellationToken.None);
        Assert.That(inputLimited.GetString(), Is.EqualTo("abc"));
        Assert.That(inputLimited.IsComplete(), Is.False);
        Assert.That(inputLimited.WasLimitExceeded(), Is.True);
        Assert.That(inputLimited.GetDiagnostics().Single().GetCode(),
            Is.EqualTo(HtmlDiagnosticCode.InputLimitExceeded));

        HtmlLimits outputLimits = HtmlLimits.CreateDefault();
        outputLimits.SetMaximumOutputCharacters(3);
        HtmlEntityResult outputLimited = HtmlEntities.EncodeText(
            "<", HtmlEntityEncoding.Minimal, outputLimits, CancellationToken.None);
        Assert.That(outputLimited.GetString(), Is.Empty);
        Assert.That(outputLimited.IsComplete(), Is.False);
        Assert.That(outputLimited.WasLimitExceeded(), Is.True);
        Assert.That(outputLimited.GetDiagnostics().Single().GetCode(),
            Is.EqualTo(HtmlDiagnosticCode.ResourceLimitExceeded));
    }

    [Test]
    public void DiagnosticsCarrySourceLocationsAndResultsReturnSnapshots()
    {
        HtmlEntityResult result = HtmlEntities.DecodeText(
            "x\r\n&#x80;", HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnostic[] first = result.GetDiagnostics();
        Assert.That(first, Has.Length.EqualTo(1));
        Assert.That(first[0].GetSpan().GetStart().GetLine(), Is.EqualTo(2));
        Assert.That(first[0].GetSpan().GetStart().GetColumn(), Is.EqualTo(1));
        Assert.That(first[0].GetSpan().GetCharacterLength(), Is.EqualTo(6));
        first[0] = null;
        Assert.That(result.GetDiagnostics()[0], Is.Not.Null);
    }

    [Test]
    public void EncodingReplacesUnpairedSurrogatesAndReportsThem()
    {
        HtmlEntityResult minimal = HtmlEntities.EncodeText(
            "a\uD800b", HtmlEntityEncoding.Minimal,
            HtmlLimits.CreateDefault(), CancellationToken.None);
        Assert.That(minimal.GetString(), Is.EqualTo("a�b"));
        Assert.That(minimal.GetDiagnostics().Single().GetCode(),
            Is.EqualTo(HtmlDiagnosticCode.InputInvalidScalar));

        Assert.That(HtmlEntities.EncodeText("\uDC00", HtmlEntityEncoding.AsciiOnly),
            Is.EqualTo("&#xFFFD;"));
    }

    [Test]
    public void FullOverloadsHonorCancellationAndValidateArguments()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        Assert.Throws<OperationCanceledException>(() => HtmlEntities.DecodeText(
            "&amp;", HtmlLimits.CreateDefault(), source.Token));
        Assert.Throws<OperationCanceledException>(() => HtmlEntities.EncodeText(
            "value", HtmlEntityEncoding.Minimal, HtmlLimits.CreateDefault(), source.Token));

        Assert.Throws<ArgumentNullException>(() => HtmlEntities.DecodeText(null));
        Assert.Throws<ArgumentNullException>(() => HtmlEntities.DecodeAttribute(null));
        Assert.Throws<ArgumentNullException>(() => HtmlEntities.EncodeText(null, HtmlEntityEncoding.Minimal));
        Assert.Throws<ArgumentNullException>(() => HtmlEntities.DecodeText(
            "value", null, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlEntities.EncodeText(
            "value", (HtmlEntityEncoding)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlEntities.Decode(
            "value", (HtmlEntityContext)99));

        HtmlEntityResult result = HtmlEntityResult.Create("value", Array.Empty<HtmlDiagnostic>(), true, false, 0, 0);
        Assert.That(result.GetString(), Is.EqualTo("value"));
        Assert.Throws<ArgumentNullException>(() => HtmlEntityResult.Create(null, Array.Empty<HtmlDiagnostic>(), true, false, 0, 0));
        Assert.Throws<ArgumentNullException>(() => HtmlEntityResult.Create("value", null, true, false, 0, 0));
    }
}
