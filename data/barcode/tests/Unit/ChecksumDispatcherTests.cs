namespace barcode.Tests;

public class ChecksumDispatcherTests
{
    [TestCase("GS1 modulo-10", 1)]
    [TestCase("Gs1Modulo10", 1)]
    [TestCase("Modulo10", 1)]
    [TestCase("Code 128 modulo-103", 2)]
    [TestCase("Code128Modulo103", 2)]
    [TestCase("Modulo103", 2)]
    [TestCase("Code 39 modulo-43", 3)]
    [TestCase("Code39Modulo43", 3)]
    [TestCase("Modulo43", 3)]
    [TestCase("Codabar modulo-16", 4)]
    [TestCase("CodabarModulo16", 4)]
    [TestCase("Modulo16", 4)]
    public void EveryDocumentedAlgorithmAliasResolves(
        string name,
        int expected)
    {
        Assert.That(
            (int)ChecksumDispatcher.ResolveNamedAlgorithm(name),
            Is.EqualTo(expected));
    }

    [Test]
    public void InternalDispatcherHelpersRejectMissingSourcesAndUnknownKinds()
    {
        BarcodeChecksumRequest gs1 = new BarcodeChecksumRequest();
        BarcodeChecksumRequest codabar = new BarcodeChecksumRequest();
        ChecksumAlgorithmKind unknown = (ChecksumAlgorithmKind)999;

        Assert.Multiple(() =>
        {
            Assert.That(() => ChecksumDispatcher.ValidateRequest(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ChecksumDispatcher.CalculateGs1Value(gs1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ChecksumDispatcher.CalculateCodabarValue(codabar), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ChecksumDispatcher.ValidateGs1TextLength(gs1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ChecksumDispatcher.CalculateValue(gs1, unknown), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ChecksumDispatcher.GetAlgorithmName(unknown), Throws.TypeOf<BarcodeValidationException>());
        });
    }
}
