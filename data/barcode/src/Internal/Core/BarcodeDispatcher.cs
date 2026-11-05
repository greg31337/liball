namespace barcode;

internal class BarcodeDispatcher
{
    internal BarcodeSymbol EncodeBarcode(BarcodeEncodeRequest request)
    {
        BarcodeValidationReport report = ValidateEncodeRequest(request);
        if (!report.IsConformant)
        {
            throw new BarcodeEncodingException(
                ResolveValidationMessage(
                    report,
                    "Barcode encode request is invalid or unsupported."));
        }

        EncodingContext context = new EncodingContext(request);
        return BarcodeEncodingPipeline.Encode(context);
    }

    internal BarcodeSymbol? TryEncodeBarcode(BarcodeEncodeRequest request)
    {
        BarcodeValidationReport report = ValidateEncodeRequest(request);
        if (!report.IsConformant)
        {
            return null;
        }

        EncodingContext context = new EncodingContext(request);
        return BarcodeEncodingPipeline.Encode(context);
    }

    static BarcodeValidationReport ValidateEncodeRequest(
        BarcodeEncodeRequest request)
    {
        BarcodeValidationRequest validation = new BarcodeValidationRequest();
        validation.Data = request;
        validation.Format = BarcodeFormat.Automatic;
        validation.Mode = BarcodeValidationMode.Strict;
        return PreEncodeValidator.Validate(validation);
    }

    static string ResolveValidationMessage(
        BarcodeValidationReport report,
        string fallback)
    {
        if (report.Issues != null)
        {
            int index = 0;
            while (index < report.Issues.Length)
            {
                string? message = report.Issues[index].Message;
                if (message != null && message.Length > 0)
                {
                    return message;
                }

                index = index + 1;
            }
        }

        return fallback;
    }

    internal BarcodeDecodeResult[] DecodeBarcode(BarcodeDecodeRequest request)
    {
        if (!CanCreateDecodingContext(request))
        {
            throw new BarcodeDecodingException(
                "Barcode decode request is invalid or unsupported.");
        }

        BarcodeDecodeAttempt attempt = BarcodeDecodePipeline.TryDecode(request);
        if (!attempt.Success || attempt.Results == null)
        {
            string message = attempt.FailureMessage == null
                ? "The barcode input could not be decoded."
                : attempt.FailureMessage;
            throw new BarcodeDecodingException(message);
        }

        BarcodeDecodeResult[]? results = ResultCopier.CopyDecodeResultArray(
            attempt.Results);
        return results == null ? new BarcodeDecodeResult[0] : results;
    }

    internal BarcodeDecodeAttempt TryDecodeBarcode(BarcodeDecodeRequest request)
    {
        if (!CanCreateDecodingContext(request))
        {
            return CreateDecodeFailure(
                "INVALID_REQUEST",
                "Barcode decode request is invalid or unsupported.");
        }

        return ResultCopier.CopyDecodeAttempt(
            BarcodeDecodePipeline.TryDecode(request));
    }

    internal bool CanCreateEncodingContext(BarcodeEncodeRequest request)
    {
        if (request == null)
        {
            return false;
        }

        if (request.Format == BarcodeFormat.Automatic && request.Payload == null)
        {
            return false;
        }

        if (request.Format != BarcodeFormat.Automatic && !FormatDispatcher.IsSupportedFormat(request.Format))
        {
            return false;
        }

        BarcodePayload? payload = request.Payload;
        if (payload != null)
        {
            long requestedBytes = 0;
            if (payload.RawBytes != null)
            {
                requestedBytes = payload.RawBytes.Length;
            }

            if (payload.Text != null)
            {
                requestedBytes = requestedBytes + ((long)payload.Text.Length * 2L);
            }

            if (requestedBytes > EncodingContext.DefaultMaximumBytes)
            {
                return false;
            }
        }

        return true;
    }

    internal bool CanCreateDecodingContext(BarcodeDecodeRequest request)
    {
        if (request == null)
        {
            return false;
        }

        BarcodeDecodeOptions? options = request.Options;
        if (ImageInputValidator.FindDecodeOptionsError(
            options,
            request.Image) != null)
        {
            return false;
        }

        if (request.Image != null)
        {
            if (ImageInputValidator.FindImageError(request.Image) != null)
            {
                return false;
            }
        }

        if (request.Symbol != null &&
            ImageInputValidator.FindSymbolError(request.Symbol) != null)
        {
            return false;
        }

        return true;
    }

    internal BarcodeDecodeAttempt CreateDecodeFailure(string code, string message)
    {
        BarcodeDecodeAttempt attempt = new BarcodeDecodeAttempt();
        attempt.Success = false;
        attempt.Results = new BarcodeDecodeResult[0];
        attempt.Diagnostics = new BarcodeDiagnostic[0];
        attempt.FailureCode = code;
        attempt.FailureMessage = message;
        return attempt;
    }

    internal BarcodeDetectionResult DetectBarcodes(BarcodeDetectionRequest request)
    {
        if (request == null || request.Image == null)
        {
            throw new BarcodeDetectionException(
                "A barcode detection request with an image is required.");
        }

        string? imageError = ImageInputValidator.FindImageError(request.Image);
        if (imageError != null)
        {
            throw new BarcodeDetectionException(imageError);
        }

        string? optionsError = ImageInputValidator.FindDetectionOptionsError(
            request.Options,
            request.Image);
        if (optionsError != null)
        {
            throw new BarcodeDetectionException(optionsError);
        }

        return ResultCopier.CopyDetectionResult(
            BarcodeDetectionPipeline.Detect(request));
    }

    internal BarcodeRenderedImage RenderBarcode(BarcodeRenderRequest request)
    {
        if (request == null)
        {
            throw new BarcodeRenderingException(
                "A barcode render request is required.");
        }

        BarcodeRenderOptions options = request.Options == null
            ? new BarcodeRenderOptions()
            : request.Options;
        ValidateRenderFormat(options.Format);
        RenderPlan plan = RenderPlanner.CreatePlan(request);
        BarcodeRenderedImage result = CreateRenderedImage(plan, options);
        PopulateRenderMetadata(result, plan);
        result.Diagnostics = CreateRenderDiagnostics(plan);
        return ResultCopier.CopyRenderedImage(result);
    }

    static void ValidateRenderFormat(BarcodeRenderFormat format)
    {
        int value = (int)format;
        if (value < (int)BarcodeRenderFormat.Raw1
            || value > (int)BarcodeRenderFormat.Svg)
        {
            throw new BarcodeRenderingException(
                "The requested barcode render format is invalid.");
        }
    }

    static BarcodeRenderedImage CreateRenderedImage(
        RenderPlan plan,
        BarcodeRenderOptions options)
    {
        BarcodeRenderedImage result = new BarcodeRenderedImage();
        result.Format = options.Format;
        switch (options.Format)
        {
            case BarcodeRenderFormat.Raw1:
                RasterCanvas raw = RasterCanvas.CreateRaw1(plan, options);
                result.Bytes = raw.Pixels;
                result.MediaType = BarcodeMediaTypes.Raw1;
                result.Stride = raw.Stride;
                result.PixelFormat = raw.PixelFormat;
                break;
            case BarcodeRenderFormat.Gray8:
                RasterCanvas gray = RasterCanvas.CreateGray8(plan, options);
                result.Bytes = gray.Pixels;
                result.MediaType = BarcodeMediaTypes.Gray8;
                result.Stride = gray.Stride;
                result.PixelFormat = gray.PixelFormat;
                break;
            case BarcodeRenderFormat.Png:
                PngWriteResult png = PngWriter.Write(plan, options);
                result.Bytes = png.Bytes;
                result.MediaType = BarcodeMediaTypes.Png;
                break;
            case BarcodeRenderFormat.Bmp:
                BmpWriteResult bmp = BmpWriter.Write(plan, options);
                result.Bytes = bmp.Bytes;
                result.MediaType = BarcodeMediaTypes.Bmp;
                break;
            case BarcodeRenderFormat.Svg:
                SvgWriteResult svg = SvgWriter.Write(plan, options);
                result.Text = svg.Text;
                result.MediaType = BarcodeMediaTypes.Svg;
                break;
            default:
                throw new BarcodeRenderingException(
                    "The requested barcode render format is invalid.");
        }

        return result;
    }

    static void PopulateRenderMetadata(
        BarcodeRenderedImage result,
        RenderPlan plan)
    {
        result.WidthPixels = plan.Size.WidthPixels;
        result.HeightPixels = plan.Size.HeightPixels;
        result.WidthMillimetres = plan.Size.WidthMillimetres;
        result.HeightMillimetres = plan.Size.HeightMillimetres;
        result.DpiX = plan.Size.DotsPerInch;
        result.DpiY = plan.Size.DotsPerInch;
        result.Size = ResultCopier.CopySize(plan.Size);
        result.TextLayout = ResultCopier.CopyTextLayout(plan.TextLayout);
    }

    static BarcodeDiagnostic[] CreateRenderDiagnostics(RenderPlan plan)
    {
        BarcodeDiagnostic scale = CreateNumericRenderDiagnostic(
            "MODULE_SCALE",
            "The render plan selected an integral pixel scale per module.",
            plan.ModuleScale,
            1.0);
        BarcodeDiagnostic width = CreateNumericRenderDiagnostic(
            "ACTUAL_WIDTH_MILLIMETRES",
            "The render plan resolved the actual output width in millimetres.",
            plan.Size.WidthMillimetres,
            0.0);
        BarcodeDiagnostic height = CreateNumericRenderDiagnostic(
            "ACTUAL_HEIGHT_MILLIMETRES",
            "The render plan resolved the actual output height in millimetres.",
            plan.Size.HeightMillimetres,
            0.0);
        BarcodeDiagnostic bounds = new BarcodeDiagnostic();
        bounds.Stage = "RenderPlan";
        bounds.Code = "OUTPUT_BOUNDS";
        bounds.Message = "The render plan resolved the output pixel bounds.";
        bounds.Region = new BarcodeRectangle();
        bounds.Region.Width = plan.Size.WidthPixels;
        bounds.Region.Height = plan.Size.HeightPixels;
        return new BarcodeDiagnostic[] { scale, width, height, bounds };
    }

    static BarcodeDiagnostic CreateNumericRenderDiagnostic(
        string code,
        string message,
        double value,
        double threshold)
    {
        BarcodeDiagnostic diagnostic = new BarcodeDiagnostic();
        diagnostic.Stage = "RenderPlan";
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.HasNumericEvidence = true;
        diagnostic.Value = value;
        diagnostic.Threshold = threshold;
        return diagnostic;
    }

    internal BarcodeValidationReport ValidateBarcodeData(BarcodeValidationRequest request)
    {
        return ResultCopier.CopyValidationReport(
            PreEncodeValidator.Validate(request));
    }

    internal BarcodeValidationReport ValidateBarcodeSymbol(BarcodeValidationRequest request)
    {
        return ResultCopier.CopyValidationReport(
            LogicalSymbolValidator.ValidateSymbolRequest(request));
    }

    internal BarcodeValidationReport ValidateBarcodeRender(BarcodeValidationRequest request)
    {
        return ResultCopier.CopyValidationReport(
            RenderPlanValidator.ValidateRenderRequest(request));
    }

    internal BarcodeValidationReport ValidateBarcodeImage(BarcodeValidationRequest request)
    {
        return ResultCopier.CopyValidationReport(
            ImageQualityValidator.ValidateImageRequest(request));
    }

    internal BarcodeChecksumResult CalculateBarcodeChecksum(BarcodeChecksumRequest request)
    {
        return ResultCopier.CopyChecksumResult(
            ChecksumDispatcher.CalculateChecksum(request));
    }

    internal BarcodeChecksumResult VerifyBarcodeChecksum(BarcodeChecksumRequest request)
    {
        return ResultCopier.CopyChecksumResult(
            ChecksumDispatcher.VerifyChecksum(request));
    }

    internal Gs1ProcessResult ProcessGs1Data(Gs1ProcessRequest request)
    {
        return ResultCopier.CopyGs1ProcessResult(
            Gs1DataProcessing.Process(request));
    }

    internal Gs1ApplicationIdentifierInfo? LookupGs1ApplicationIdentifier(string applicationIdentifier)
    {
        return ResultCopier.CopyGs1ApplicationIdentifierInfo(
            Gs1DataProcessing.Lookup(applicationIdentifier));
    }

    internal StructuredAppendResult AssembleStructuredAppend(StructuredAppendRequest request)
    {
        if (request == null)
        {
            throw new BarcodeValidationException(
                "A structured-append assembly request is required.");
        }

        StructuredAppendResult result;
        switch (request.Format)
        {
            case BarcodeFormat.DataMatrix:
            case BarcodeFormat.Gs1DataMatrix:
                result = DataMatrixStructuredAppendAssembler.AssembleDataMatrixSegments(
                    request);
                break;
            case BarcodeFormat.MacroPdf417:
                result = Pdf417MacroAssembler.AssembleMacroSegments(request);
                break;
            default:
                throw new BarcodeValidationException(
                    "Structured-append assembly supports Data Matrix, GS1 DataMatrix, and Macro PDF417 only.");
        }

        return ResultCopier.CopyStructuredAppendResult(result);
    }
}
