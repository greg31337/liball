namespace barcode.Tests;

public class DecoderInteroperabilityCorpusTests
{
    [TestCaseSource(nameof(Corpus))]
    public void DecoderPreservesCorpusRawBytesAndExactObservedFormat(
        CrossEncoderFixture fixture)
    {
        BarcodeSymbol symbol =
            CrossEncoderFixtureTests.CreateVerifiedSymbol(fixture);
        BarcodeDecodeRequest request = CrossEncoderFixtureTests.CreateRequest(
            symbol,
            fixture.ExpectedObservedFormat);

        BarcodeDecodeResult[] decoded =
            new BarcodeDecoder().DecodeBarcode(request);

        Assert.That(decoded, Has.Length.EqualTo(1), fixture.Id);
        Assert.Multiple(() =>
        {
            Assert.That(decoded[0].Format,
                Is.EqualTo(fixture.ExpectedObservedFormat), fixture.Id);
            Assert.That(decoded[0].Payload?.RawBytes,
                Is.EqualTo(fixture.ExpectedRawBytes), fixture.Id);
            Assert.That(decoded[0].Payload?.Text,
                Is.EqualTo(fixture.ExpectedText), fixture.Id);
        });
    }

    [Test]
    public void EveryFormatHasVectorsFromAtLeastTwoIndependentEncoders()
    {
        global::System.Collections.Generic.Dictionary<
            BarcodeFormat,
            global::System.Collections.Generic.HashSet<string>> sources =
            new global::System.Collections.Generic.Dictionary<
                BarcodeFormat,
                global::System.Collections.Generic.HashSet<string>>();
        foreach (CrossEncoderFixture fixture in Corpus)
        {
            if (!sources.ContainsKey(fixture.ExpectedObservedFormat))
            {
                sources[fixture.ExpectedObservedFormat] =
                    new global::System.Collections.Generic.HashSet<string>();
            }

            sources[fixture.ExpectedObservedFormat].Add(
                fixture.Provenance.Source);
        }

        BarcodeFormat[] expectedFormats = new BarcodeFormat[]
        {
            BarcodeFormat.Code128,
            BarcodeFormat.Gs1128,
            BarcodeFormat.Ean8,
            BarcodeFormat.Ean13,
            BarcodeFormat.UpcA,
            BarcodeFormat.UpcE,
            BarcodeFormat.EanUpcAddon2,
            BarcodeFormat.EanUpcAddon5,
            BarcodeFormat.Code39,
            BarcodeFormat.Itf,
            BarcodeFormat.Itf14,
            BarcodeFormat.Codabar,
            BarcodeFormat.DataMatrix,
            BarcodeFormat.Gs1DataMatrix,
            BarcodeFormat.Pdf417,
            BarcodeFormat.CompactPdf417,
            BarcodeFormat.MacroPdf417
        };
        foreach (BarcodeFormat format in expectedFormats)
        {
            Assert.That(sources.ContainsKey(format), Is.True, format.ToString());
            Assert.That(sources[format].Count,
                Is.GreaterThanOrEqualTo(2), format.ToString());
        }
    }

    static CrossEncoderFixture[] Corpus = CrossEncoderFixtureTests.Fixtures;
}
