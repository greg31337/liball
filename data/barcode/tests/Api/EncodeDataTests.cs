namespace barcode.Tests;

public class EncodeDataTests
{
    [Test]
    public void GeneralOptionsAndRequestHoldAllSelections()
    {
        BarcodeSizing sizing = new BarcodeSizing();
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        StructuredAppendInfo append = new StructuredAppendInfo();
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.Sizing = sizing;
        options.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        options.QuietZone = quietZone;
        options.ChecksumMode = BarcodeChecksumMode.Calculate;
        options.TextMode = BarcodeTextMode.Custom;
        options.CustomText = "Display text";
        options.CharacterEncoding = "UTF-8";
        options.EciAssignmentNumber = 26;
        options.StructuredAppend = append;
        BarcodePayload payload = new BarcodePayload();
        Code128Options code128 = new Code128Options();
        EanUpcOptions eanUpc = new EanUpcOptions();
        Code39Options code39 = new Code39Options();
        ItfOptions itf = new ItfOptions();
        CodabarOptions codabar = new CodabarOptions();
        DataMatrixOptions dataMatrix = new DataMatrixOptions();
        Pdf417Options pdf417 = new Pdf417Options();
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = BarcodeFormat.DataMatrix;
        request.Payload = payload;
        request.Options = options;
        request.Code128 = code128;
        request.EanUpc = eanUpc;
        request.Code39 = code39;
        request.Itf = itf;
        request.Codabar = codabar;
        request.DataMatrix = dataMatrix;
        request.Pdf417 = pdf417;

        Assert.Multiple(() =>
        {
            Assert.That(options.Sizing, Is.SameAs(sizing));
            Assert.That(options.QuietZonePolicy, Is.EqualTo(BarcodeQuietZonePolicy.ExactCustom));
            Assert.That(options.QuietZone, Is.SameAs(quietZone));
            Assert.That(options.ChecksumMode, Is.EqualTo(BarcodeChecksumMode.Calculate));
            Assert.That(options.TextMode, Is.EqualTo(BarcodeTextMode.Custom));
            Assert.That(options.CustomText, Is.EqualTo("Display text"));
            Assert.That(options.CharacterEncoding, Is.EqualTo("UTF-8"));
            Assert.That(options.EciAssignmentNumber, Is.EqualTo(26));
            Assert.That(options.StructuredAppend, Is.SameAs(append));
            Assert.That(request.Format, Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(request.Payload, Is.SameAs(payload));
            Assert.That(request.Options, Is.SameAs(options));
            Assert.That(request.Code128, Is.SameAs(code128));
            Assert.That(request.EanUpc, Is.SameAs(eanUpc));
            Assert.That(request.Code39, Is.SameAs(code39));
            Assert.That(request.Itf, Is.SameAs(itf));
            Assert.That(request.Codabar, Is.SameAs(codabar));
            Assert.That(request.DataMatrix, Is.SameAs(dataMatrix));
            Assert.That(request.Pdf417, Is.SameAs(pdf417));
        });
    }

    [Test]
    public void LinearSymbologyOptionsHoldAllSelections()
    {
        Code128Options code128 = new Code128Options();
        code128.CodeSet = Code128CodeSet.B;
        code128.AllowFnc1 = true;
        code128.AllowFnc2 = true;
        code128.AllowFnc3 = true;
        code128.AllowFnc4 = true;
        code128.UseLatin1UpperShift = true;
        int[] addOnLengths = new int[] { 2, 5 };
        EanUpcOptions eanUpc = new EanUpcOptions();
        eanUpc.ChecksumMode = BarcodeChecksumMode.Require;
        eanUpc.AddOn = "12";
        eanUpc.AllowedAddOnLengths = addOnLengths;
        eanUpc.RequireAddOn = true;
        eanUpc.UpcENumberSystem = 1;
        eanUpc.CompressToUpcE = true;
        eanUpc.ExtendGuardBars = true;
        eanUpc.ShowQuietZoneIndicators = true;
        Code39Options code39 = new Code39Options();
        code39.CharacterMode = Code39CharacterMode.FullAscii;
        code39.WideToNarrowRatio = 2.5;
        code39.IntercharacterGapModules = 2;
        code39.ChecksumMode = BarcodeChecksumMode.VerifyKeep;
        code39.TransmitCheckCharacter = true;
        code39.EnableAppend = true;
        int[] allowedLengths = new int[] { 6, 14 };
        ItfOptions itf = new ItfOptions();
        itf.WideToNarrowRatio = 3.0;
        itf.AllowedLengths = allowedLengths;
        itf.ChecksumMode = BarcodeChecksumMode.Calculate;
        itf.UseBearerBars = true;
        itf.UseBearerBox = true;
        itf.BearerBarThicknessModules = 4;
        itf.UseCorrugatedProfile = true;
        CodabarOptions codabar = new CodabarOptions();
        codabar.StartCharacter = "A";
        codabar.StopCharacter = "D";
        codabar.AcceptAliases = true;
        codabar.WideToNarrowRatio = 2.25;
        codabar.IntercharacterGapModules = 1;
        codabar.ChecksumMode = BarcodeChecksumMode.VerifyStrip;
        codabar.TransmitStartStop = true;
        codabar.TransmitCheckCharacter = true;

        addOnLengths[0] = 5;
        allowedLengths[0] = 8;

        Assert.Multiple(() =>
        {
            Assert.That(code128.CodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(code128.AllowFnc1, Is.True);
            Assert.That(code128.AllowFnc2, Is.True);
            Assert.That(code128.AllowFnc3, Is.True);
            Assert.That(code128.AllowFnc4, Is.True);
            Assert.That(code128.UseLatin1UpperShift, Is.True);
            Assert.That(eanUpc.ChecksumMode, Is.EqualTo(BarcodeChecksumMode.Require));
            Assert.That(eanUpc.AddOn, Is.EqualTo("12"));
            Assert.That(eanUpc.AllowedAddOnLengths, Is.SameAs(addOnLengths));
            Assert.That(eanUpc.AllowedAddOnLengths?[0], Is.EqualTo(5));
            Assert.That(eanUpc.RequireAddOn, Is.True);
            Assert.That(eanUpc.UpcENumberSystem, Is.EqualTo(1));
            Assert.That(eanUpc.CompressToUpcE, Is.True);
            Assert.That(eanUpc.ExtendGuardBars, Is.True);
            Assert.That(eanUpc.ShowQuietZoneIndicators, Is.True);
            Assert.That(code39.CharacterMode, Is.EqualTo(Code39CharacterMode.FullAscii));
            Assert.That(code39.WideToNarrowRatio, Is.EqualTo(2.5));
            Assert.That(code39.IntercharacterGapModules, Is.EqualTo(2));
            Assert.That(code39.ChecksumMode, Is.EqualTo(BarcodeChecksumMode.VerifyKeep));
            Assert.That(code39.TransmitCheckCharacter, Is.True);
            Assert.That(code39.EnableAppend, Is.True);
            Assert.That(itf.WideToNarrowRatio, Is.EqualTo(3.0));
            Assert.That(itf.AllowedLengths, Is.SameAs(allowedLengths));
            Assert.That(itf.AllowedLengths?[0], Is.EqualTo(8));
            Assert.That(itf.ChecksumMode, Is.EqualTo(BarcodeChecksumMode.Calculate));
            Assert.That(itf.UseBearerBars, Is.True);
            Assert.That(itf.UseBearerBox, Is.True);
            Assert.That(itf.BearerBarThicknessModules, Is.EqualTo(4));
            Assert.That(itf.UseCorrugatedProfile, Is.True);
            Assert.That(codabar.StartCharacter, Is.EqualTo("A"));
            Assert.That(codabar.StopCharacter, Is.EqualTo("D"));
            Assert.That(codabar.AcceptAliases, Is.True);
            Assert.That(codabar.WideToNarrowRatio, Is.EqualTo(2.25));
            Assert.That(codabar.IntercharacterGapModules, Is.EqualTo(1));
            Assert.That(codabar.ChecksumMode, Is.EqualTo(BarcodeChecksumMode.VerifyStrip));
            Assert.That(codabar.TransmitStartStop, Is.True);
            Assert.That(codabar.TransmitCheckCharacter, Is.True);
        });
    }

    [Test]
    public void MatrixAndStackedOptionsHoldAllSelections()
    {
        StructuredAppendInfo dataMatrixAppend = new StructuredAppendInfo();
        DataMatrixOptions dataMatrix = new DataMatrixOptions();
        dataMatrix.Shape = DataMatrixShape.StandardRectangular;
        dataMatrix.EncodingMode = DataMatrixEncodingMode.Base256;
        dataMatrix.Rows = 16;
        dataMatrix.Columns = 48;
        dataMatrix.EciAssignmentNumber = 26;
        dataMatrix.MacroType = 5;
        dataMatrix.ReaderProgramming = true;
        dataMatrix.StructuredAppend = dataMatrixAppend;
        StructuredAppendInfo macro = new StructuredAppendInfo();
        Pdf417Options pdf417 = new Pdf417Options();
        pdf417.Columns = 8;
        pdf417.Rows = 20;
        pdf417.Variant = Pdf417Variant.Compact;
        pdf417.CompactionMode = Pdf417CompactionMode.Numeric;
        pdf417.ErrorCorrectionLevel = 6;
        pdf417.EciAssignmentNumber = 26;
        pdf417.Macro = macro;
        pdf417.FileName = "manifest.dat";
        pdf417.Timestamp = 1_725_000_000;
        pdf417.Sender = "warehouse";
        pdf417.Addressee = "store";
        pdf417.FileSize = 4_294_967_296;
        pdf417.FileChecksum = 123456;

        Assert.Multiple(() =>
        {
            Assert.That(dataMatrix.Shape, Is.EqualTo(DataMatrixShape.StandardRectangular));
            Assert.That(dataMatrix.EncodingMode, Is.EqualTo(DataMatrixEncodingMode.Base256));
            Assert.That(dataMatrix.Rows, Is.EqualTo(16));
            Assert.That(dataMatrix.Columns, Is.EqualTo(48));
            Assert.That(dataMatrix.EciAssignmentNumber, Is.EqualTo(26));
            Assert.That(dataMatrix.MacroType, Is.EqualTo(5));
            Assert.That(dataMatrix.ReaderProgramming, Is.True);
            Assert.That(dataMatrix.StructuredAppend, Is.SameAs(dataMatrixAppend));
            Assert.That(pdf417.Columns, Is.EqualTo(8));
            Assert.That(pdf417.Rows, Is.EqualTo(20));
            Assert.That(pdf417.Variant, Is.EqualTo(Pdf417Variant.Compact));
            Assert.That(pdf417.CompactionMode, Is.EqualTo(Pdf417CompactionMode.Numeric));
            Assert.That(pdf417.ErrorCorrectionLevel, Is.EqualTo(6));
            Assert.That(pdf417.EciAssignmentNumber, Is.EqualTo(26));
            Assert.That(pdf417.Macro, Is.SameAs(macro));
            Assert.That(pdf417.FileName, Is.EqualTo("manifest.dat"));
            Assert.That(pdf417.Timestamp, Is.EqualTo(1_725_000_000));
            Assert.That(pdf417.Sender, Is.EqualTo("warehouse"));
            Assert.That(pdf417.Addressee, Is.EqualTo("store"));
            Assert.That(pdf417.FileSize, Is.EqualTo(4_294_967_296));
            Assert.That(pdf417.FileChecksum, Is.EqualTo(123456));
        });
    }

    [Test]
    public void SymbolHoldsLogicalModulesLayoutAndMetadata()
    {
        byte[] modules = new byte[] { 1, 0, 1, 0, 1, 0 };
        int[] codewords = new int[] { 104, 33, 34, 102, 106 };
        BarcodePayload payload = new BarcodePayload();
        BarcodeSize size = new BarcodeSize();
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        BarcodeTextLayout textLayout = new BarcodeTextLayout();
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = BarcodeFormat.Code128;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = 6;
        symbol.HeightModules = 1;
        symbol.Modules = modules;
        symbol.Payload = payload;
        symbol.Size = size;
        symbol.RequiredQuietZone = quietZone;
        symbol.TextLayout = textLayout;
        symbol.SymbologyIdentifier = "]C0";
        symbol.Codewords = codewords;
        symbol.HasChecksum = true;
        symbol.ChecksumValue = 102;

        modules[1] = 1;
        codewords[1] = 44;

        Assert.Multiple(() =>
        {
            Assert.That(symbol.Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(symbol.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(symbol.WidthModules, Is.EqualTo(6));
            Assert.That(symbol.HeightModules, Is.EqualTo(1));
            Assert.That(symbol.Modules, Is.SameAs(modules));
            Assert.That(symbol.Modules?[1], Is.EqualTo(1));
            Assert.That(symbol.Payload, Is.SameAs(payload));
            Assert.That(symbol.Size, Is.SameAs(size));
            Assert.That(symbol.RequiredQuietZone, Is.SameAs(quietZone));
            Assert.That(symbol.TextLayout, Is.SameAs(textLayout));
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo("]C0"));
            Assert.That(symbol.Codewords, Is.SameAs(codewords));
            Assert.That(symbol.Codewords?[1], Is.EqualTo(44));
            Assert.That(symbol.HasChecksum, Is.True);
            Assert.That(symbol.ChecksumValue, Is.EqualTo(102));
        });
    }

    [Test]
    public void ConfigurationHoldsEncodeRequestAndSymbol()
    {
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        BarcodeSymbol symbol = new BarcodeSymbol();
        BarcodeConfiguration configuration = new BarcodeConfiguration();
        configuration.EncodeRequest = request;
        configuration.EncodedSymbol = symbol;

        Assert.Multiple(() =>
        {
            Assert.That(configuration.EncodeRequest, Is.SameAs(request));
            Assert.That(configuration.EncodedSymbol, Is.SameAs(symbol));
        });
    }
}
