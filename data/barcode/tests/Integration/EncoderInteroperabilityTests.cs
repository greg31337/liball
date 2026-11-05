namespace barcode.Tests;

public class EncoderInteroperabilityTests
{
    [TestCaseSource(nameof(ReaderObservations))]
    public void IndependentReaderAcceptsExactRenderedEncoderOutput(
        EncoderReaderObservation observation)
    {
        BarcodeSymbol symbol = Encode(observation.Format);
        BarcodeRenderedImage rendered = Render(
            symbol,
            observation.RenderFormat);

        observation.Provenance.ValidateRequiredMetadata();
        observation.Provenance.VerifySha256(rendered.Bytes!);
        Assert.Multiple(() =>
        {
            Assert.That(observation.ObservedFormat,
                Is.EqualTo(ExpectedReaderFormat(
                    observation.Reader,
                    observation.Format)),
                observation.Id);
            Assert.That(observation.ObservedText,
                Is.EqualTo(ExpectedReaderText(
                    observation.Reader,
                    observation.Format)),
                observation.Id);
        });
    }

    [TestCaseSource(nameof(ModuleObservations))]
    public void IndependentEncoderOracleMatchesExactLogicalModules(
        EncoderModuleObservation observation)
    {
        BarcodeSymbol symbol = Encode(observation.Format);

        observation.Provenance.ValidateRequiredMetadata();
        observation.Provenance.VerifySha256(symbol.Modules!);
        Assert.Multiple(() =>
        {
            Assert.That(symbol.WidthModules,
                Is.EqualTo(observation.Width), observation.Id);
            Assert.That(symbol.HeightModules,
                Is.EqualTo(observation.Height), observation.Id);
        });
    }

    [Test]
    public void OfflineObservationsCoverEveryFormatAndEveryRequiredOracle()
    {
        global::System.Collections.Generic.HashSet<BarcodeFormat> formats =
            new global::System.Collections.Generic.HashSet<BarcodeFormat>();
        global::System.Collections.Generic.HashSet<string> readers =
            new global::System.Collections.Generic.HashSet<string>();
        global::System.Collections.Generic.HashSet<string> encoders =
            new global::System.Collections.Generic.HashSet<string>();
        foreach (EncoderReaderObservation observation in ReaderObservations)
        {
            formats.Add(observation.Format);
            readers.Add(observation.Reader);
        }

        foreach (EncoderModuleObservation observation in ModuleObservations)
        {
            formats.Add(observation.Format);
            encoders.Add(observation.Encoder);
        }

        Assert.Multiple(() =>
        {
            Assert.That(formats, Is.EquivalentTo(ConcreteFormats));
            Assert.That(readers, Is.EquivalentTo(new string[]
            {
                "ZXing Java",
                "ZXing.Net",
                "ZBar"
            }));
            Assert.That(encoders, Is.EquivalentTo(new string[]
            {
                "Zint",
                "BWIPP"
            }));
        });
    }

    static BarcodeSymbol Encode(BarcodeFormat format)
    {
        BarcodePayload payload = BarcodeTestBuilder.CreateAsciiPayload(
            DataForFormat(format));
        payload.IsGs1 = format == BarcodeFormat.Gs1128
            || format == BarcodeFormat.Gs1DataMatrix
            || format == BarcodeFormat.Itf14;
        BarcodeEncodeRequest request =
            BarcodeTestBuilder.CreateEncodeRequest(format, payload);
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.TextMode = BarcodeTextMode.Hidden;
        request.Options = options;
        if (format == BarcodeFormat.CompactPdf417)
        {
            Pdf417Options pdf417 = new Pdf417Options();
            pdf417.Variant = Pdf417Variant.Compact;
            pdf417.ErrorCorrectionLevel = 2;
            request.Pdf417 = pdf417;
        }
        else if (format == BarcodeFormat.MacroPdf417)
        {
            StructuredAppendInfo macro =
                BarcodeTestBuilder.CreateStructuredAppend(
                    BarcodeFormat.MacroPdf417,
                    0,
                    2,
                    "147001");
            Pdf417Options pdf417 = new Pdf417Options();
            pdf417.ErrorCorrectionLevel = 2;
            pdf417.Macro = macro;
            request.Pdf417 = pdf417;
        }
        else if (format == BarcodeFormat.Itf14)
        {
            ItfOptions itf = new ItfOptions();
            itf.WideToNarrowRatio = 3.0;
            request.Itf = itf;
        }

        return new BarcodeEncoder().EncodeBarcode(request);
    }

    static BarcodeRenderedImage Render(
        BarcodeSymbol symbol,
        BarcodeRenderFormat format)
    {
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Format = format;
        options.ModuleScale = 5;
        options.QuietZonePolicy = BarcodeQuietZonePolicy.Standard;
        options.TextMode = BarcodeTextMode.Hidden;
        if (symbol.Dimension == BarcodeDimension.Linear)
        {
            BarcodeSizing sizing = new BarcodeSizing();
            sizing.RequestedHeightPixels = 140;
            options.Sizing = sizing;
        }

        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;
        return new BarcodeRenderer().RenderBarcode(request);
    }

    static string ExpectedReaderFormat(string reader, BarcodeFormat format)
    {
        if (reader == "ZBar")
        {
            switch (format)
            {
                case BarcodeFormat.Code128:
                case BarcodeFormat.Gs1128:
                    return "CODE-128";
                case BarcodeFormat.Ean8:
                    return "EAN-8";
                case BarcodeFormat.Ean13:
                case BarcodeFormat.UpcA:
                case BarcodeFormat.UpcE:
                    return "EAN-13";
                case BarcodeFormat.Code39:
                    return "CODE-39";
                case BarcodeFormat.Itf:
                case BarcodeFormat.Itf14:
                    return "I2/5";
                default:
                    return "Codabar";
            }
        }

        switch (format)
        {
            case BarcodeFormat.Code128:
            case BarcodeFormat.Gs1128:
                return "CODE_128";
            case BarcodeFormat.Ean8:
                return "EAN_8";
            case BarcodeFormat.Ean13:
                return "EAN_13";
            case BarcodeFormat.UpcA:
                return "UPC_A";
            case BarcodeFormat.UpcE:
                return "UPC_E";
            case BarcodeFormat.Code39:
                return "CODE_39";
            case BarcodeFormat.Itf:
            case BarcodeFormat.Itf14:
                return "ITF";
            case BarcodeFormat.Codabar:
                return "CODABAR";
            case BarcodeFormat.DataMatrix:
            case BarcodeFormat.Gs1DataMatrix:
                return "DATA_MATRIX";
            default:
                return "PDF_417";
        }
    }

    static string ExpectedReaderText(string reader, BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Ean8:
                return "55123457";
            case BarcodeFormat.Ean13:
                return "4006381333931";
            case BarcodeFormat.UpcA:
                return reader == "ZBar"
                    ? "0036000291452"
                    : "036000291452";
            case BarcodeFormat.UpcE:
                return reader == "ZBar"
                    ? "0012345000058"
                    : "01234558";
            case BarcodeFormat.Itf14:
                return "09501101530010";
            case BarcodeFormat.Codabar:
                return reader == "ZBar" ? "A5678A" : "5678";
            case BarcodeFormat.Gs1DataMatrix:
                return "\u001d0109506000134352";
            default:
                return DataForFormat(format);
        }
    }

    static string DataForFormat(BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Gs1128:
            case BarcodeFormat.Gs1DataMatrix:
                return "0109506000134352";
            case BarcodeFormat.Ean8:
                return "5512345";
            case BarcodeFormat.Ean13:
                return "400638133393";
            case BarcodeFormat.UpcA:
                return "03600029145";
            case BarcodeFormat.UpcE:
                return "123455";
            case BarcodeFormat.EanUpcAddon2:
                return "12";
            case BarcodeFormat.EanUpcAddon5:
                return "51234";
            case BarcodeFormat.Code39:
                return "T147";
            case BarcodeFormat.Itf:
                return "123456";
            case BarcodeFormat.Itf14:
                return "0950110153001";
            case BarcodeFormat.Codabar:
                return "5678";
            case BarcodeFormat.DataMatrix:
                return "T147-DM";
            case BarcodeFormat.Pdf417:
                return "T147-PDF";
            case BarcodeFormat.CompactPdf417:
                return "T147-COMPACT";
            case BarcodeFormat.MacroPdf417:
                return "T147-MACRO";
            default:
                return "T147-128";
        }
    }

    static EncoderReaderObservation Reader(
        string reader,
        string version,
        string reference,
        string license,
        BarcodeRenderFormat renderFormat,
        BarcodeFormat format,
        string observedFormat,
        string observedText,
        string sha256)
    {
        FixtureProvenance provenance = CreateProvenance(
            reader.ToLowerInvariant().Replace(" ", "-") + "-" +
                format.ToString().ToLowerInvariant(),
            format,
            reader,
            version,
            reader + " decoded the exact rendered image",
            reference,
            license,
            sha256);
        EncoderReaderObservation observation =
            new EncoderReaderObservation();
        observation.Id = provenance.FixtureId;
        observation.Reader = reader;
        observation.Format = format;
        observation.RenderFormat = renderFormat;
        observation.ObservedFormat = observedFormat;
        observation.ObservedText = observedText;
        observation.Provenance = provenance;
        return observation;
    }

    static EncoderModuleObservation Module(
        string encoder,
        string version,
        string generator,
        string generatorVersion,
        string reference,
        string license,
        BarcodeFormat format,
        int width,
        int height,
        string sha256)
    {
        FixtureProvenance provenance = CreateProvenance(
            encoder.ToLowerInvariant() + "-" +
                format.ToString().ToLowerInvariant(),
            format,
            encoder,
            version,
            "Exact logical module matrix match",
            reference,
            license,
            sha256);
        provenance.Generator = generator;
        provenance.GeneratorVersion = generatorVersion;
        EncoderModuleObservation observation =
            new EncoderModuleObservation();
        observation.Id = provenance.FixtureId;
        observation.Encoder = encoder;
        observation.Format = format;
        observation.Width = width;
        observation.Height = height;
        observation.Provenance = provenance;
        return observation;
    }

    static FixtureProvenance CreateProvenance(
        string id,
        BarcodeFormat format,
        string source,
        string sourceVersion,
        string oracle,
        string reference,
        string license,
        string sha256)
    {
        FixtureProvenance provenance = new FixtureProvenance();
        provenance.FixtureId = id;
        provenance.Symbology = format.ToString();
        provenance.Origin = "Independent encoder interoperability run";
        provenance.Source = source;
        provenance.SourceVersion = sourceVersion;
        provenance.Oracle = oracle;
        provenance.Reference = reference;
        provenance.License = license;
        provenance.Sha256 = sha256;
        return provenance;
    }

    static EncoderReaderObservation[] ReaderObservations =
        new EncoderReaderObservation[]
        {
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.Code128, "CODE_128", "T147-128", "1aa08d8f6eac34d64063f3bc299da160e8d3871ae33c1f01595a8d65b078f05d"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.Gs1128, "CODE_128", "0109506000134352", "593647ac0bfbd0fe374b9cc7cf470501e6578d3a2c5fb830c497d39a3bdf4cee"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.Ean8, "EAN_8", "55123457", "5abea1a42eb718feff87a6ed5eb95ba01c06d61050170a24ac48360e902f241b"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.Ean13, "EAN_13", "4006381333931", "773e000099afe345876a4d1b5151b60ff9444faad1b7800d982bd28ce65dca1c"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.UpcA, "UPC_A", "036000291452", "1d3eca0a1ecddaa066cc9c728f7e965f393001731cbfa8a35479ce8781281421"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.UpcE, "UPC_E", "01234558", "3f22a0a73ea1290355187aecb9004b3826bc556d9805c69684f7d541a7fa785b"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.Code39, "CODE_39", "T147", "a16d978a6bb65c3cd7dac60c2265045182f2f7bb680a1ab49ff50d6861dbd13d"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.Itf, "ITF", "123456", "46914d74a65ac55fd144802ca30a5ae623de585578e722768ef6f79e3be31a7f"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.Itf14, "ITF", "09501101530010", "2769afcb8c7df3a33f2ae6e86037f4d07ee877d9d7ed42f1962087294907f5b6"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.Codabar, "CODABAR", "5678", "0a22842c50b48bd89101b09aa5fddd44c5b3c01724db7933ac1f1191b13bf536"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.DataMatrix, "DATA_MATRIX", "T147-DM", "30c2f79f7fd863d2674689f4766b7f58b49644fa27f03768309e3a0034a604f3"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.Gs1DataMatrix, "DATA_MATRIX", "\u001d0109506000134352", "6b9caf23abf7266cc66071449daa56b856c94ca06f9fca5409d68d7bb459cbb4"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.Pdf417, "PDF_417", "T147-PDF", "6f781d111e97b34f8bb50be454e3538f713a4c3d26939ce7b71e65cfd34cb5c0"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.CompactPdf417, "PDF_417", "T147-COMPACT", "bd2ff4749b4d052980c7f3869b273da4d46e37ba9053f1e2c08785a61a06a1fb"),
            Reader("ZXing Java", "3.5.4", "specs/ZXing-README.md", "Apache-2.0", BarcodeRenderFormat.Png, BarcodeFormat.MacroPdf417, "PDF_417", "T147-MACRO", "db193c51f30e2d5b651d042d8c61dd902f6dd2a8445dfa7b094756f896cff546"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.Code128, "CODE_128", "T147-128", "15982094bc18f7f649c7186ee59cd6ed9a9c0f98589a386a7de5915fcf9d668a"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.Gs1128, "CODE_128", "0109506000134352", "a52db1af259324885cdbb1d2b34028c2ae59931436a270ab088269340f648558"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.Ean8, "EAN_8", "55123457", "285142cd063cc8ee0341e5cc753c3ffe0f14e2f82d7ce15d0fe2a446ff139291"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.Ean13, "EAN_13", "4006381333931", "33ad115b8931635922eb3832ea6a4c4cb37c5d1f85b88319b679ad10178b3476"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.UpcA, "UPC_A", "036000291452", "e8a81f84226ee7cf16776a5e3e90bd7c3f3258246d70cb4b83a07489f11e9667"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.UpcE, "UPC_E", "01234558", "fa8ffd2324dbb0fa3b9484c417006a3d73b37e83d19d20b1a7e9d86b79e12681"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.Code39, "CODE_39", "T147", "d2ecc4bb566faea33f242890e290e1c4b5c269ebf6bb5918d81ac4c30b42c8a5"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.Itf, "ITF", "123456", "d9ac81c58f16339f831200cf31a086048dc4940de4c2aa977621c2b4b7362123"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.Itf14, "ITF", "09501101530010", "cb9c04d5a98c0237ba52ca72d55b86a6c76f5af11af1315fa2ad643ad70d09c6"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.Codabar, "CODABAR", "5678", "c8b4ab88467107e23092f697d4fd1549985ceb700b3f1b53285687f9df1ca66a"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.DataMatrix, "DATA_MATRIX", "T147-DM", "7fabbdb39fd1811ab1429f4f2143b86b9e24780a53b3ae45ea7e85c12bfbb9e0"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.Gs1DataMatrix, "DATA_MATRIX", "\u001d0109506000134352", "badc124e02f165b054fc4faea0fdbf85401167331b8f7687e0798dcdf1758eb0"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.Pdf417, "PDF_417", "T147-PDF", "271adfc1951d1b3dfd9b9b61f3946e01c5549319e8d282503409ef5807927642"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.CompactPdf417, "PDF_417", "T147-COMPACT", "f5aa708b81effb2b649d473340baf9a5a19774e064be72cf87fbbe12be47ab48"),
            Reader("ZXing.Net", "0.16.11", "specs/ZXing.Net-README.md", "Apache-2.0", BarcodeRenderFormat.Gray8, BarcodeFormat.MacroPdf417, "PDF_417", "T147-MACRO", "26e3ca96626a1200d777873e3041939c4052cd90fc214d0ae426ee223ccafccc"),
            Reader("ZBar", "0.23.93", "specs/ZBar-README.md", "LGPL-2.1-or-later", BarcodeRenderFormat.Gray8, BarcodeFormat.Code128, "CODE-128", "T147-128", "15982094bc18f7f649c7186ee59cd6ed9a9c0f98589a386a7de5915fcf9d668a"),
            Reader("ZBar", "0.23.93", "specs/ZBar-README.md", "LGPL-2.1-or-later", BarcodeRenderFormat.Gray8, BarcodeFormat.Gs1128, "CODE-128", "0109506000134352", "a52db1af259324885cdbb1d2b34028c2ae59931436a270ab088269340f648558"),
            Reader("ZBar", "0.23.93", "specs/ZBar-README.md", "LGPL-2.1-or-later", BarcodeRenderFormat.Gray8, BarcodeFormat.Ean8, "EAN-8", "55123457", "285142cd063cc8ee0341e5cc753c3ffe0f14e2f82d7ce15d0fe2a446ff139291"),
            Reader("ZBar", "0.23.93", "specs/ZBar-README.md", "LGPL-2.1-or-later", BarcodeRenderFormat.Gray8, BarcodeFormat.Ean13, "EAN-13", "4006381333931", "33ad115b8931635922eb3832ea6a4c4cb37c5d1f85b88319b679ad10178b3476"),
            Reader("ZBar", "0.23.93", "specs/ZBar-README.md", "LGPL-2.1-or-later", BarcodeRenderFormat.Gray8, BarcodeFormat.UpcA, "EAN-13", "0036000291452", "e8a81f84226ee7cf16776a5e3e90bd7c3f3258246d70cb4b83a07489f11e9667"),
            Reader("ZBar", "0.23.93", "specs/ZBar-README.md", "LGPL-2.1-or-later", BarcodeRenderFormat.Gray8, BarcodeFormat.UpcE, "EAN-13", "0012345000058", "fa8ffd2324dbb0fa3b9484c417006a3d73b37e83d19d20b1a7e9d86b79e12681"),
            Reader("ZBar", "0.23.93", "specs/ZBar-README.md", "LGPL-2.1-or-later", BarcodeRenderFormat.Gray8, BarcodeFormat.Code39, "CODE-39", "T147", "d2ecc4bb566faea33f242890e290e1c4b5c269ebf6bb5918d81ac4c30b42c8a5"),
            Reader("ZBar", "0.23.93", "specs/ZBar-README.md", "LGPL-2.1-or-later", BarcodeRenderFormat.Gray8, BarcodeFormat.Itf, "I2/5", "123456", "d9ac81c58f16339f831200cf31a086048dc4940de4c2aa977621c2b4b7362123"),
            Reader("ZBar", "0.23.93", "specs/ZBar-README.md", "LGPL-2.1-or-later", BarcodeRenderFormat.Gray8, BarcodeFormat.Itf14, "I2/5", "09501101530010", "cb9c04d5a98c0237ba52ca72d55b86a6c76f5af11af1315fa2ad643ad70d09c6"),
            Reader("ZBar", "0.23.93", "specs/ZBar-README.md", "LGPL-2.1-or-later", BarcodeRenderFormat.Gray8, BarcodeFormat.Codabar, "Codabar", "A5678A", "c8b4ab88467107e23092f697d4fd1549985ceb700b3f1b53285687f9df1ca66a")
        };

    static EncoderModuleObservation[] ModuleObservations =
        new EncoderModuleObservation[]
        {
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.Gs1128, 134, 1, "92a1791849e74d1e9da87b8ab65fa3b156fa476507c92d6f0bf1c4f964dbe4ca"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.Ean8, 67, 1, "f3ab0c844220758ced59750ee7505137321f37e9b34c11dcc76ac17dfe51089d"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.Ean13, 95, 1, "08bae0cce725409c8a7462056ee6abc596041d6f1da2768834d5164ce73833bb"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.UpcA, 95, 1, "539999ba33da092d7db889b5fa558ddcc0dc31df10617b9b59fa914220f45095"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.UpcE, 51, 1, "c53d7556d3fd47d1dd776989b027c3d48511fce8ee1810a343ec5829a470182c"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.EanUpcAddon2, 20, 1, "ec2fdba955ab2efbf3c32330ce4c6002650ca5fd36c94c7b4d23b1f5b77bcb0d"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.EanUpcAddon5, 47, 1, "1d1117b18db168fce9e60823cea007a922208b29c5507b2b27454d707f7052ff"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.Code39, 77, 1, "569f40e8826bba1f59cf09e9cff348029b25754acc3977cb7a00d99eda4754f6"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.Itf, 63, 1, "a855c2c998906c9da89e79fb79ce9cbda3487432167cb2658f1158d0b32750b7"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.Itf14, 135, 1, "e499a81e3aa8b785b739d16ed1e1c97b3d18038cf89c4033b166236ccc951c04"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.DataMatrix, 14, 14, "65240bd36a3d31e33b712312fd931b790b81ff93582e985e2311688df786c4e5"),
            Module("Zint", "2.16.0", "libzint", "2.16.0", "specs/Zint-Manual.txt", "BSD-3-Clause", BarcodeFormat.Gs1DataMatrix, 32, 8, "05b7c68bbfa3b79c3c1cfa6bd929ee962a344fba0d2104d48658786cb9183954"),
            Module("BWIPP", "2026-05-28", "bwip-js", "4.11.4", "specs/bwip-js-README.md", "MIT", BarcodeFormat.Gs1128, 134, 1, "92a1791849e74d1e9da87b8ab65fa3b156fa476507c92d6f0bf1c4f964dbe4ca"),
            Module("BWIPP", "2026-05-28", "bwip-js", "4.11.4", "specs/bwip-js-README.md", "MIT", BarcodeFormat.Ean8, 67, 1, "f3ab0c844220758ced59750ee7505137321f37e9b34c11dcc76ac17dfe51089d"),
            Module("BWIPP", "2026-05-28", "bwip-js", "4.11.4", "specs/bwip-js-README.md", "MIT", BarcodeFormat.Ean13, 95, 1, "08bae0cce725409c8a7462056ee6abc596041d6f1da2768834d5164ce73833bb"),
            Module("BWIPP", "2026-05-28", "bwip-js", "4.11.4", "specs/bwip-js-README.md", "MIT", BarcodeFormat.UpcA, 95, 1, "539999ba33da092d7db889b5fa558ddcc0dc31df10617b9b59fa914220f45095"),
            Module("BWIPP", "2026-05-28", "bwip-js", "4.11.4", "specs/bwip-js-README.md", "MIT", BarcodeFormat.UpcE, 51, 1, "c53d7556d3fd47d1dd776989b027c3d48511fce8ee1810a343ec5829a470182c"),
            Module("BWIPP", "2026-05-28", "bwip-js", "4.11.4", "specs/bwip-js-README.md", "MIT", BarcodeFormat.EanUpcAddon2, 20, 1, "ec2fdba955ab2efbf3c32330ce4c6002650ca5fd36c94c7b4d23b1f5b77bcb0d"),
            Module("BWIPP", "2026-05-28", "bwip-js", "4.11.4", "specs/bwip-js-README.md", "MIT", BarcodeFormat.EanUpcAddon5, 47, 1, "1d1117b18db168fce9e60823cea007a922208b29c5507b2b27454d707f7052ff"),
            Module("BWIPP", "2026-05-28", "bwip-js", "4.11.4", "specs/bwip-js-README.md", "MIT", BarcodeFormat.DataMatrix, 14, 14, "65240bd36a3d31e33b712312fd931b790b81ff93582e985e2311688df786c4e5")
        };

    static BarcodeFormat[] ConcreteFormats = new BarcodeFormat[]
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
}

public class EncoderReaderObservation
{
    public string Id = "";
    public string Reader = "";
    public BarcodeFormat Format;
    public BarcodeRenderFormat RenderFormat;
    public string ObservedFormat = "";
    public string ObservedText = "";
    internal FixtureProvenance Provenance = new FixtureProvenance();

    public override string ToString()
    {
        return Id;
    }
}

public class EncoderModuleObservation
{
    public string Id = "";
    public string Encoder = "";
    public BarcodeFormat Format;
    public int Width;
    public int Height;
    internal FixtureProvenance Provenance = new FixtureProvenance();

    public override string ToString()
    {
        return Id;
    }
}
