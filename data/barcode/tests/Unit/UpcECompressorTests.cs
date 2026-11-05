namespace barcode.Tests;

public class UpcECompressorTests
{
    [TestCase("034000005673", "03456703")]
    [TestCase("098400000751", "09847531")]
    [TestCase("045670000080", "04567840")]
    [TestCase("012345000058", "01234558")]
    [TestCase("112345000055", "11234555")]
    public void CompressionAndExpansionMatchPublishedRuleFamilies(string upcA, string expectedUpcE)
    {
        string? compressed = UpcECompressor.TryCompressCompleteUpcA(upcA);
        string required = UpcECompressor.CompressCompleteUpcA(upcA);
        string expanded = UpcECompressor.ExpandCompleteUpcE(expectedUpcE);

        Assert.Multiple(() =>
        {
            Assert.That(compressed, Is.EqualTo(expectedUpcE));
            Assert.That(required, Is.EqualTo(expectedUpcE));
            Assert.That(expanded, Is.EqualTo(upcA));
        });
    }

    [TestCase("0123450")]
    [TestCase("0123451")]
    [TestCase("0123452")]
    [TestCase("0984753")]
    [TestCase("0456784")]
    [TestCase("0123455")]
    [TestCase("1123455")]
    public void EveryTerminalCompressionRuleRoundTripsWithoutLoss(string core)
    {
        string completeUpcE = UpcECompressor.AppendCheckDigitToCore(core);
        string expandedUpcA = UpcECompressor.ExpandCompleteUpcE(completeUpcE);
        string compressedAgain = UpcECompressor.CompressCompleteUpcA(expandedUpcA);

        Assert.That(compressedAgain, Is.EqualTo(completeUpcE));
    }

    [Test]
    public void NonCompressibleUpcAReturnsNullOrThrowsForRequiredCompression()
    {
        string? compressed = UpcECompressor.TryCompressCompleteUpcA("012345678905");

        Assert.Multiple(() =>
        {
            Assert.That(compressed, Is.Null);
            Assert.That(
                () => UpcECompressor.CompressCompleteUpcA("012345678905"),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo("The UPC-A value cannot be represented losslessly as UPC-E."));
        });
    }

    [TestCase("0123455", "01234558")]
    [TestCase("1123455", "11234555")]
    [TestCase("0984753", "09847531")]
    public void CheckDigitCalculationUsesExpandedUpcAData(string core, string expectedComplete)
    {
        Assert.That(UpcECompressor.AppendCheckDigitToCore(core), Is.EqualTo(expectedComplete));
    }

    [Test]
    public void ExpansionRejectsMalformedNumberSystemLengthCharactersAndCheckDigit()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => UpcECompressor.ExpandCompleteUpcE(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => UpcECompressor.ExpandCompleteUpcE("0123455"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => UpcECompressor.ExpandCompleteUpcE("012345588"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => UpcECompressor.ExpandCompleteUpcE("21234558"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => UpcECompressor.ExpandCompleteUpcE("01A34558"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => UpcECompressor.ExpandCompleteUpcE("０１２３４５５８"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => UpcECompressor.ExpandCompleteUpcE("01234559"),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo("The supplied UPC-E check digit is invalid."));
        });
    }

    [Test]
    public void CompressionRejectsMalformedNumberSystemLengthCharactersAndCheckDigit()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => UpcECompressor.TryCompressCompleteUpcA(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => UpcECompressor.TryCompressCompleteUpcA("01234500005"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => UpcECompressor.TryCompressCompleteUpcA("0123450000588"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => UpcECompressor.TryCompressCompleteUpcA("212345000058"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => UpcECompressor.TryCompressCompleteUpcA("012345000A58"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => UpcECompressor.TryCompressCompleteUpcA("０１２３４５００００５８"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => UpcECompressor.TryCompressCompleteUpcA("012345000059"),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo("The supplied UPC-A check digit is invalid."));
        });
    }
}
