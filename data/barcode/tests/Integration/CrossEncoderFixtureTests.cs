namespace barcode.Tests;

public class CrossEncoderFixtureTests
{
    [TestCaseSource(nameof(Fixtures))]
    public void DecoderAcceptsReferenceEncoderModuleMatrices(
        CrossEncoderFixture fixture)
    {
        BarcodeSymbol symbol = CreateVerifiedSymbol(fixture);
        BarcodeDecodeAttempt attempt = new BarcodeDecoder().TryDecodeBarcode(
            CreateRequest(symbol, fixture.Format));

        Assert.That(attempt.Success, Is.True, fixture.Id);
        Assert.That(attempt.Results, Has.Length.EqualTo(1), fixture.Id);
        Assert.Multiple(() =>
        {
            Assert.That(attempt.Results![0].Format,
                Is.EqualTo(fixture.Format), fixture.Id);
            Assert.That(attempt.Results[0].Payload?.Text,
                Is.EqualTo(fixture.ExpectedText), fixture.Id);
        });
    }

    [TestCaseSource(nameof(Fixtures))]
    public void DecoderRejectsReferenceMatricesWithDestroyedFixedPatterns(
        CrossEncoderFixture fixture)
    {
        BarcodeSymbol symbol = CreateVerifiedSymbol(fixture);
        DestroyFixedPatterns(symbol);

        BarcodeDecodeAttempt attempt = new BarcodeDecoder().TryDecodeBarcode(
            CreateRequest(symbol, fixture.Format));

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.False, fixture.Id);
            Assert.That(attempt.Results, Is.Empty, fixture.Id);
            Assert.That(attempt.FailureCode,
                Is.EqualTo("PURE_SYMBOL_NOT_DECODED"), fixture.Id);
        });
    }

    [Test]
    public void FixtureCatalogCoversEverySupportedConcreteFormatAndIndependentOracles()
    {
        global::System.Collections.Generic.HashSet<BarcodeFormat> formats =
            new global::System.Collections.Generic.HashSet<BarcodeFormat>();
        global::System.Collections.Generic.HashSet<string> sources =
            new global::System.Collections.Generic.HashSet<string>();
        foreach (CrossEncoderFixture fixture in Fixtures)
        {
            formats.Add(fixture.Format);
            sources.Add(fixture.Provenance.Source);
        }

        BarcodeFormat[] expected = new BarcodeFormat[]
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

        Assert.Multiple(() =>
        {
            Assert.That(formats, Is.EquivalentTo(expected));
            Assert.That(sources,
                Is.EquivalentTo(new string[]
                {
                    "Zint",
                    "BWIPP",
                    "ZXing.Net"
                }));
        });
    }

    internal static BarcodeSymbol CreateVerifiedSymbol(CrossEncoderFixture fixture)
    {
        fixture.Provenance.ValidateRequiredMetadata();
        fixture.Provenance.VerifySha256(fixture.PackedModules);
        byte[] rawModules = UnpackModules(
            fixture.PackedModules,
            fixture.Width * fixture.Rows);
        byte[] symbolModules = rawModules;
        int height = fixture.Rows;
        if (fixture.Dimension == BarcodeDimension.StackedLinear)
        {
            height = fixture.Rows * Pdf417Layout.DefaultRowHeightModules;
            symbolModules = RepeatRows(
                rawModules,
                fixture.Width,
                fixture.Rows,
                Pdf417Layout.DefaultRowHeightModules);
        }

        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = fixture.Format;
        symbol.Dimension = fixture.Dimension;
        symbol.WidthModules = fixture.Width;
        symbol.HeightModules = height;
        symbol.Modules = symbolModules;
        return symbol;
    }

    internal static BarcodeDecodeRequest CreateRequest(
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[] { format };
        options.PureSymbol = true;
        options.ParseGs1 = true;
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }

    static byte[] UnpackModules(byte[] packed, int count)
    {
        byte[] modules = new byte[count];
        int index = 0;
        while (index < count)
        {
            int mask = 1 << (7 - (index % 8));
            modules[index] = (packed[index / 8] & mask) == 0
                ? (byte)0
                : (byte)1;
            index++;
        }

        return modules;
    }

    static byte[] RepeatRows(
        byte[] modules,
        int width,
        int rows,
        int repetitions)
    {
        byte[] repeated = new byte[width * rows * repetitions];
        int row = 0;
        while (row < rows)
        {
            int repetition = 0;
            while (repetition < repetitions)
            {
                global::System.Array.Copy(
                    modules,
                    row * width,
                    repeated,
                    ((row * repetitions) + repetition) * width,
                    width);
                repetition++;
            }

            row++;
        }

        return repeated;
    }

    static void DestroyFixedPatterns(BarcodeSymbol symbol)
    {
        byte[] modules = symbol.Modules!;
        int row = 0;
        while (row < symbol.HeightModules)
        {
            int edgeWidth = symbol.Dimension == BarcodeDimension.Matrix
                ? 1
                : global::System.Math.Min(18, symbol.WidthModules / 3);
            int edge = 0;
            while (edge < edgeWidth)
            {
                modules[(row * symbol.WidthModules) + edge] = 0;
                modules[((row + 1) * symbol.WidthModules) - edge - 1] = 0;
                edge++;
            }

            row++;
        }

        if (symbol.Dimension == BarcodeDimension.Matrix)
        {
            int x = 0;
            while (x < symbol.WidthModules)
            {
                modules[x] = 0;
                modules[((symbol.HeightModules - 1) * symbol.WidthModules) + x] = 0;
                x++;
            }
        }
    }

    internal static CrossEncoderFixture[] Fixtures = new CrossEncoderFixture[]
    {
        Zint("zint-code128", BarcodeFormat.Code128, BarcodeDimension.Linear, 79, 1, "Z128", "d21d8a735dee69bdb1d6", "b0a9a80cbc85fe3d70450531fcebe9e9a6243cc45e1fe4d228315dad352beea8"),
        Zint("zint-gs1-128", BarcodeFormat.Gs1128, BarcodeDimension.Linear, 134, 1, "0109506000134352", "d39ebb36648c5ddeb664dcb1db8a2f63ac", "3e2f35cc1c128441ccd80112d30e2a40733d7ac6cda2f3efe7336977201a2ea5"),
        Zint("zint-ean8", BarcodeFormat.Ean8, BarcodeDimension.Linear, 67, 1, "55123457", "ac589926a8572744a0", "49c5a2a2d4f05ae800fa7da7a84388206813ddaa7873bc3040c301c39b9257d0"),
        Zint("zint-ean13", BarcodeFormat.Ean13, BarcodeDimension.Linear, 95, 1, "4006381333931", "a353af7a259aa14285d2166a", "3b0891ad726cde7e8abb9422d896750af14193034fffea8ee2710a1f53c4c015"),
        Zint("zint-upca", BarcodeFormat.UpcA, BarcodeDimension.Linear, 95, 1, "036000291452", "a35eaf1a346ab674cd7276ca", "8203db394c8b10383aafe8ef925b9c845382341565590c065051ce6e3b1d2a2e"),
        Zint("zint-upce", BarcodeFormat.UpcE, BarcodeDimension.Linear, 51, 1, "01234565", "acc9bd3ae57aa0", "8d6df9e15caae3626987cbcb0336c110d55ce9865c61c6437c373048cf341275"),
        Zint("zint-addon2", BarcodeFormat.EanUpcAddon2, BarcodeDimension.Linear, 20, 1, "12", "b32930", "5ce3bb12c8177a08ddca6dc49d4fdd3e7c382885fe2633b620c00dd9a6e0d689"),
        Zint("zint-addon5", BarcodeFormat.EanUpcAddon5, BarcodeDimension.Linear, 47, 1, "51234", "b62994daf53a", "a354a3f77aa5f1d4a8468314c33675ab894778e943a55d2e8263c23fb1e958dc"),
        Zint("zint-code39", BarcodeFormat.Code39, BarcodeDimension.Linear, 64, 1, "Z39", "96d4dab65565a96d", "759e429b2cefe4ae396eacbdd49da483b51cfd4d60547aa9b65ac5c62c74fe4b"),
        Zint("zint-itf", BarcodeFormat.Itf, BarcodeDimension.Linear, 63, 1, "123456", "ae8ae3ba28e8e2ba", "f665c32c6fe2cb1f5b49f8245fdda4e1199dd1ff0cee55785e990d58cf95e7b8"),
        Zint("zint-itf14", BarcodeFormat.Itf14, BarcodeDimension.Linear, 135, 1, "12345678901231", "ae8ae3ba28e8e2a2b8eba38ba2b8e3aa3a", "cf6afc285081cfe7ac399723469af613af6957ce2facfb145bccb497a1e0a78d"),
        Zint("zint-codabar", BarcodeFormat.Codabar, BarcodeDimension.Linear, 62, 1, "1234", "b255952d955a5258", "5c601cce452401d96271121b3e652ea83c4323963281c529e273a5eacba2ea53"),
        Zint("zint-datamatrix", BarcodeFormat.DataMatrix, BarcodeDimension.Matrix, 10, 10, "ZDM", "aaae38bb4be4b239823586bff0", "556714ea2111e3934cb88288b6b58c1ca84548b0e57c0653ad343459c252562b"),
        Zint("zint-gs1-datamatrix", BarcodeFormat.Gs1DataMatrix, BarcodeDimension.Matrix, 32, 8, "0109506000134352", "aaaaaaaacf598b3b8a58b898bf5dfdb5ec74c62cc5fdca1bd18ea612ffffffff", "5ac980d0b54d766a33decb7b144ab5fbe001dcb841b6744734e004086da01e51"),
        Zint("zint-pdf417", BarcodeFormat.Pdf417, BarcodeDimension.StackedLinear, 103, 6, "ZPDF", "ff547abc3d4f1de68f5787f453fea8f5087a73afd61fab8fe8a7fd51d5f8cf5858172a1e1fd14ffaa2bcf18c36d023579e3fa29ff546b822e6fd23c2eb987f453fea8f5e875f227999ebecfe8a40", "74706d86cbb237addbf3d2ceea79cf75a9a8eaa67d194107d1c7d8eb1ee6dd1a"),
        Zint("zint-compact-pdf417", BarcodeFormat.CompactPdf417, BarcodeDimension.StackedLinear, 69, 6, "ZCPDF", "ff547abc3d4f118ecffaa3d421b07ad0e17fd51d5f8a77e5c8c3fea8af3c508428f79ff546b823c845c590ffaa3d7a17cc4a41e4", "8bffb4efee32ca0ed4e4ea46373ea7614d39943fc5dd65ad48add1867d1b6c2e"),
        Zint("zint-macro-pdf417", BarcodeFormat.MacroPdf417, BarcodeDimension.StackedLinear, 120, 7, "ZMACRO", "ff547d5f3ade1ef22fabe7d5f3fa29ff547d46313a13f26d3c37a903fa29ff5454f028c7da0beb8166a3e3fa29ff546bcfb0c29eaf0a1046bcfbfa29ff546b8429e81a3cc9fa67ae73fa29ff547d7b23c85ebc2ebe47af0bfa29ff5474efb24c1471cb1c374efbfa29", "0e6b7ca499ec9e654afb67b026ee3fa85af628f9ef164081e38f7cf4f1045aa1"),
        ZxingNet("zxing-net-macro-pdf417", BarcodeFormat.MacroPdf417, BarcodeDimension.StackedLinear, 120, 8, "NETMACRO", "ff547d5f3acf19a10f70b7d5f3fa29ff547ea3bd48189d09f937a903fa29ff5454f0342f9463edf9a751fbfa29ff546bcfbc8bdb420ca306bcfbfa29ff5475c327e99fab8d3c37ae73fa29ff547d7b28efdcf5c993875f43fa29ff5474efb3ce9bdc2f24f74efbfa29ff547d2c2bc8111e2c2f657ee3fa29", "f33e5bca0d00e8c14dfd3ba12d50ba4998585c63718e3220f5d9a8a3ea8e08d9"),
        Bwipp("bwipp-code128", BarcodeFormat.Code128, BarcodeDimension.Linear, 79, 1, "B128", "d2116273672e993731d6", "3d997a8bc1f94d0998d9cfea8c048d399cb97e10f01c32d9103052bab44243f5"),
        Bwipp("bwipp-gs1-128", BarcodeFormat.Gs1128, BarcodeDimension.Linear, 134, 1, "0109506000134352", "d39ebb36648c5ddeb664dcb1db8a2f63ac", "3e2f35cc1c128441ccd80112d30e2a40733d7ac6cda2f3efe7336977201a2ea5"),
        Bwipp("bwipp-ean8", BarcodeFormat.Ean8, BarcodeDimension.Linear, 67, 1, "55123457", "ac589926a8572744a0", "49c5a2a2d4f05ae800fa7da7a84388206813ddaa7873bc3040c301c39b9257d0"),
        Bwipp("bwipp-ean13", BarcodeFormat.Ean13, BarcodeDimension.Linear, 95, 1, "4006381333931", "a353af7a259aa14285d2166a", "3b0891ad726cde7e8abb9422d896750af14193034fffea8ee2710a1f53c4c015"),
        Bwipp("bwipp-upca", BarcodeFormat.UpcA, BarcodeDimension.Linear, 95, 1, "036000291452", "a35eaf1a346ab674cd7276ca", "8203db394c8b10383aafe8ef925b9c845382341565590c065051ce6e3b1d2a2e"),
        Bwipp("bwipp-upce", BarcodeFormat.UpcE, BarcodeDimension.Linear, 51, 1, "01234565", "acc9bd3ae57aa0", "8d6df9e15caae3626987cbcb0336c110d55ce9865c61c6437c373048cf341275"),
        Bwipp("bwipp-addon2", BarcodeFormat.EanUpcAddon2, BarcodeDimension.Linear, 20, 1, "12", "b32930", "5ce3bb12c8177a08ddca6dc49d4fdd3e7c382885fe2633b620c00dd9a6e0d689"),
        Bwipp("bwipp-addon5", BarcodeFormat.EanUpcAddon5, BarcodeDimension.Linear, 47, 1, "51234", "b62994daf53a", "a354a3f77aa5f1d4a8468314c33675ab894778e943a55d2e8263c23fb1e958dc"),
        Bwipp("bwipp-code39", BarcodeFormat.Code39, BarcodeDimension.Linear, 80, 1, "B39", "8bbaba2eee2ab8ba8bba", "0c84a4dab59b938b91cd2e88fffd6a415d89789c4f76fb5ca87ac2c1e53ee560"),
        Bwipp("bwipp-itf", BarcodeFormat.Itf, BarcodeDimension.Linear, 51, 1, "654321", "a9b2a4d69ab340", "fc5e3d071816c73158e3ce82bfbcc95e3ef7f39ea564b8495bcafe7068868cc5"),
        Bwipp("bwipp-itf14", BarcodeFormat.Itf14, BarcodeDimension.Linear, 107, 1, "12345678901231", "ad2b3694d32a566b4cb4accd5340", "c95fa8b0dd9d8f543d1d3d19e0b03d0647858a9f3cac2c6aa88d2862cb91ce12"),
        Bwipp("bwipp-codabar", BarcodeFormat.Codabar, BarcodeDimension.Linear, 76, 1, "5678", "b88ba8a2ba2ea3aa22e0", "f728f2a043eb66828a8381445bd98df1d808359baeb1b3db30381b7ab39bd7d9"),
        Bwipp("bwipp-datamatrix", BarcodeFormat.DataMatrix, BarcodeDimension.Matrix, 10, 10, "BDM", "aaaed8135bb8393992b3e6bff0", "c4cdc3289f6f10bcfc5500ab88d7f735abb166e3f44485270eb3f29f8c82b93b"),
        Bwipp("bwipp-gs1-datamatrix", BarcodeFormat.Gs1DataMatrix, BarcodeDimension.Matrix, 16, 16, "0109506000134352", "aaaacf0d8a3abf6deca486e7973cb3f1e3fec791ec8ee2e1fe109465c1aaffff", "ba68eed9f4a1e3aa67284fa5abd901f601b47167fef9d50522bbd35ea31845a7"),
        Bwipp("bwipp-pdf417", BarcodeFormat.Pdf417, BarcodeDimension.StackedLinear, 103, 7, "BPDF", "ff547d5f351810422f5787f453fea8fa8c7a503e9b9ea40fe8a7fd51d5f8fa7453bf351f1fd14ffaa35e7da1b8b79c579e3fa29ff546b8438169d9c2f5ce7f453fea8f5e86f582f8edebc2fe8a7fd51d3be8e71625834ef1fd1480", "5e4d5a2c9b8170c8773b838965736fbb08f48dfb6327c572d68e9cc57af49bc0"),
        Bwipp("bwipp-compact-pdf417", BarcodeFormat.CompactPdf417, BarcodeDimension.StackedLinear, 69, 6, "BCPDF", "ff547abc3d4f1af3effaa3d421b07ad0e17fd51d5f8bdb059bc3fea8af3c6c73b0d05ff546b82321c5e504ffaa3d7a19f14bf744", "29fdb6e2fc16df48a2708d1f93e7a64d796a2a662d07191258d3bf0200c96213")
    };

    static CrossEncoderFixture Zint(
        string id,
        BarcodeFormat format,
        BarcodeDimension dimension,
        int width,
        int rows,
        string expectedText,
        string modules,
        string sha256)
    {
        return CreateFixture(
            id,
            format,
            dimension,
            width,
            rows,
            expectedText,
            modules,
            sha256,
            "Zint",
            "2.16.0",
            "libzint",
            "2.16.0",
            "specs/Zint-Manual.txt",
            "BSD-3-Clause");
    }

    static CrossEncoderFixture Bwipp(
        string id,
        BarcodeFormat format,
        BarcodeDimension dimension,
        int width,
        int rows,
        string expectedText,
        string modules,
        string sha256)
    {
        return CreateFixture(
            id,
            format,
            dimension,
            width,
            rows,
            expectedText,
            modules,
            sha256,
            "BWIPP",
            "2026-05-28",
            "bwip-js",
            "4.11.4",
            "specs/bwip-js-README.md",
            "MIT");
    }

    static CrossEncoderFixture ZxingNet(
        string id,
        BarcodeFormat format,
        BarcodeDimension dimension,
        int width,
        int rows,
        string expectedText,
        string modules,
        string sha256)
    {
        return CreateFixture(
            id,
            format,
            dimension,
            width,
            rows,
            expectedText,
            modules,
            sha256,
            "ZXing.Net",
            "0.16.11",
            "ZXing.Net PDF417 encoder",
            "0.16.11",
            "specs/ZXing.Net-README.md",
            "Apache-2.0");
    }

    static CrossEncoderFixture CreateFixture(
        string id,
        BarcodeFormat format,
        BarcodeDimension dimension,
        int width,
        int rows,
        string expectedText,
        string modules,
        string sha256,
        string source,
        string sourceVersion,
        string generator,
        string generatorVersion,
        string reference,
        string license)
    {
        FixtureProvenance provenance = new FixtureProvenance();
        provenance.FixtureId = id;
        provenance.Symbology = format.ToString();
        provenance.Origin = "Reference encoder logical module output";
        provenance.Source = source;
        provenance.SourceVersion = sourceVersion;
        provenance.Generator = generator;
        provenance.GeneratorVersion = generatorVersion;
        provenance.Oracle = source + " encoded module matrix";
        provenance.Reference = reference;
        provenance.License = license;
        provenance.Sha256 = sha256;
        CrossEncoderFixture fixture = new CrossEncoderFixture();
        fixture.Id = id;
        fixture.Format = format;
        fixture.Dimension = dimension;
        fixture.Width = width;
        fixture.Rows = rows;
        fixture.ExpectedText = expectedText;
        fixture.ExpectedRawBytes =
            global::System.Text.Encoding.ASCII.GetBytes(expectedText);
        fixture.ExpectedObservedFormat = format;
        fixture.PackedModules = global::System.Convert.FromHexString(modules);
        fixture.Provenance = provenance;
        return fixture;
    }
}

public class CrossEncoderFixture
{
    public string Id = "";
    public BarcodeFormat Format;
    public BarcodeDimension Dimension;
    public int Width;
    public int Rows;
    public string ExpectedText = "";
    public byte[] ExpectedRawBytes = new byte[0];
    public BarcodeFormat ExpectedObservedFormat;
    public byte[] PackedModules = new byte[0];
    internal FixtureProvenance Provenance = new FixtureProvenance();

    public override string ToString()
    {
        return Id;
    }
}
