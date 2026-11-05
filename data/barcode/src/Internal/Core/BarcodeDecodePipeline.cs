namespace barcode;

internal static class BarcodeDecodePipeline
{
    internal static BarcodeDecodeAttempt TryDecode(BarcodeDecodeRequest request)
    {
        if (request == null)
        {
            return CreateFailure(
                "INVALID_REQUEST",
                "A barcode decode request is required.");
        }

        bool hasImage = request.Image != null;
        bool hasSymbol = request.Symbol != null;
        if (hasImage == hasSymbol)
        {
            return CreateFailure(
                "INVALID_INPUT",
                "Exactly one image or pure symbol must be supplied.");
        }

        BarcodeDecodeOptions options = request.Options == null
            ? CreateDefaultOptions()
            : request.Options;
        if (options.MaximumResults < 0 || options.ExpectedResultCount < 0 ||
            options.MaximumDiagnostics < 0)
        {
            return CreateFailure(
                "INVALID_OPTIONS",
                "Decode result and diagnostic limits must not be negative.");
        }

        if (request.Symbol != null)
        {
            return DecodePureSymbol(request.Symbol, options);
        }

        if (request.Image == null)
        {
            return CreateFailure(
                "INVALID_INPUT",
                "A barcode image is required for image decoding.");
        }

        ImageBarcodeDecodeResult imageResult = ImageBarcodeDecoder.Decode(
            request.Image,
            options);
        if (imageResult.Results.Length == 0)
        {
            BarcodeDecodeAttempt failure = CreateFailure(
                "NO_BARCODE_FOUND",
                "The image did not contain a decodable requested barcode.");
            failure.Diagnostics = imageResult.Diagnostics;
            return failure;
        }

        return CreateSuccess(imageResult.Results, imageResult.Diagnostics);
    }

    static BarcodeDecodeAttempt DecodePureSymbol(
        BarcodeSymbol symbol,
        BarcodeDecodeOptions options)
    {
        BarcodeDecodeResult? decoded = PureSymbolDecoder.TryDecode(
            symbol,
            options);
        if (decoded == null)
        {
            return CreateFailure(
                "PURE_SYMBOL_NOT_DECODED",
                "The pure symbol could not be decoded as a requested format.");
        }

        MultiSymbolAggregationResult aggregation =
            MultiSymbolResultAggregator.Combine(
                new BarcodeDecodeResult[] { decoded },
                options.MaximumResults,
                options.MaximumDiagnostics);
        return CreateSuccess(
            aggregation.Results,
            aggregation.Diagnostics);
    }

    static BarcodeDecodeAttempt CreateSuccess(
        BarcodeDecodeResult[] results,
        BarcodeDiagnostic[] diagnostics)
    {
        BarcodeDecodeAttempt attempt = new BarcodeDecodeAttempt();
        attempt.Success = true;
        BarcodeDecodeResult[]? resultCopies = ResultCopier.CopyDecodeResultArray(
            results);
        attempt.Results = resultCopies == null
            ? new BarcodeDecodeResult[0]
            : resultCopies;
        BarcodeDiagnostic[]? diagnosticCopies = ResultCopier.CopyDiagnosticArray(
            diagnostics);
        attempt.Diagnostics = diagnosticCopies == null
            ? new BarcodeDiagnostic[0]
            : diagnosticCopies;
        return attempt;
    }

    static BarcodeDecodeAttempt CreateFailure(string code, string message)
    {
        BarcodeDecodeAttempt attempt = new BarcodeDecodeAttempt();
        attempt.Success = false;
        attempt.Results = new BarcodeDecodeResult[0];
        attempt.Diagnostics = new BarcodeDiagnostic[0];
        attempt.FailureCode = code;
        attempt.FailureMessage = message;
        return attempt;
    }

    static BarcodeDecodeOptions CreateDefaultOptions()
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.PureSymbol = true;
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;
        options.Effort = BarcodeEffort.Balanced;
        options.BinarizationMode = BarcodeBinarizationMode.Automatic;
        options.LocalizationMode = BarcodeLocalizationMode.Automatic;
        return options;
    }
}
