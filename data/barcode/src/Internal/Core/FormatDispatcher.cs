namespace barcode;

internal enum BarcodeCodecFamily
{
    Code128 = 0,
    EanUpc = 1,
    Code39 = 2,
    Itf = 3,
    Codabar = 4,
    DataMatrix = 5,
    Pdf417 = 6
}

internal static class FormatDispatcher
{
    internal static bool IsSupportedFormat(BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Code128:
            case BarcodeFormat.Gs1128:
            case BarcodeFormat.Ean8:
            case BarcodeFormat.Ean13:
            case BarcodeFormat.UpcA:
            case BarcodeFormat.UpcE:
            case BarcodeFormat.EanUpcAddon2:
            case BarcodeFormat.EanUpcAddon5:
            case BarcodeFormat.Code39:
            case BarcodeFormat.Itf:
            case BarcodeFormat.Itf14:
            case BarcodeFormat.Codabar:
            case BarcodeFormat.DataMatrix:
            case BarcodeFormat.Gs1DataMatrix:
            case BarcodeFormat.Pdf417:
            case BarcodeFormat.CompactPdf417:
            case BarcodeFormat.MacroPdf417:
                return true;
            default:
                return false;
        }
    }

    internal static BarcodeFormat ResolveEncodingFormat(BarcodeEncodeRequest request)
    {
        if (request == null)
        {
            throw new BarcodeEncodingException("Barcode encode request must not be null.");
        }

        if (request.Format != BarcodeFormat.Automatic)
        {
            EnsureSupportedFormat(request.Format);
            return request.Format;
        }

        BarcodePayload? payload = request.Payload;
        if (payload == null)
        {
            throw new BarcodeEncodingException("Automatic format selection requires a payload.");
        }

        bool hasBytes = payload.RawBytes != null && payload.RawBytes.Length > 0;
        if (payload.IsGs1)
        {
            return hasBytes ? BarcodeFormat.Gs1DataMatrix : BarcodeFormat.Gs1128;
        }

        if (hasBytes)
        {
            return BarcodeFormat.DataMatrix;
        }

        return BarcodeFormat.Code128;
    }

    internal static BarcodeFormat[] ResolveDecodingFormats(BarcodeDecodeOptions? options)
    {
        BarcodeFormat[]? requested = null;
        if (options != null)
        {
            requested = options.AllowedFormats;
        }

        if (requested == null || requested.Length == 0)
        {
            return CreateSupportedFormats();
        }

        int index = 0;
        while (index < requested.Length)
        {
            if (requested[index] == BarcodeFormat.Automatic)
            {
                return CreateSupportedFormats();
            }

            EnsureSupportedFormat(requested[index]);
            index = index + 1;
        }

        BarcodeFormat[] buffer = new BarcodeFormat[requested.Length];
        int count = 0;
        index = 0;
        while (index < requested.Length)
        {
            BarcodeFormat format = requested[index];
            bool duplicate = false;
            int existing = 0;
            while (existing < count)
            {
                if (buffer[existing] == format)
                {
                    duplicate = true;
                    break;
                }

                existing = existing + 1;
            }

            if (!duplicate)
            {
                buffer[count] = format;
                count = count + 1;
            }

            index = index + 1;
        }

        BarcodeFormat[] formats = new BarcodeFormat[count];
        index = 0;
        while (index < count)
        {
            formats[index] = buffer[index];
            index = index + 1;
        }

        return formats;
    }

    internal static BarcodeCodecFamily ResolveCodecFamily(BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Code128:
            case BarcodeFormat.Gs1128:
                return BarcodeCodecFamily.Code128;
            case BarcodeFormat.Ean8:
            case BarcodeFormat.Ean13:
            case BarcodeFormat.UpcA:
            case BarcodeFormat.UpcE:
            case BarcodeFormat.EanUpcAddon2:
            case BarcodeFormat.EanUpcAddon5:
                return BarcodeCodecFamily.EanUpc;
            case BarcodeFormat.Code39:
                return BarcodeCodecFamily.Code39;
            case BarcodeFormat.Itf:
            case BarcodeFormat.Itf14:
                return BarcodeCodecFamily.Itf;
            case BarcodeFormat.Codabar:
                return BarcodeCodecFamily.Codabar;
            case BarcodeFormat.DataMatrix:
            case BarcodeFormat.Gs1DataMatrix:
                return BarcodeCodecFamily.DataMatrix;
            case BarcodeFormat.Pdf417:
            case BarcodeFormat.CompactPdf417:
            case BarcodeFormat.MacroPdf417:
                return BarcodeCodecFamily.Pdf417;
            default:
                throw new UnsupportedBarcodeFormatException($"Barcode format value {(int)format} is unsupported.");
        }
    }

    internal static BarcodeDimension ResolveDimension(BarcodeFormat format)
    {
        BarcodeCodecFamily family = ResolveCodecFamily(format);
        switch (family)
        {
            case BarcodeCodecFamily.DataMatrix:
                return BarcodeDimension.Matrix;
            case BarcodeCodecFamily.Pdf417:
                return BarcodeDimension.StackedLinear;
            default:
                return BarcodeDimension.Linear;
        }
    }

    internal static BarcodeFormat[] CreateSupportedFormats()
    {
        return new BarcodeFormat[]
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

    internal static void EnsureSupportedFormat(BarcodeFormat format)
    {
        if (!IsSupportedFormat(format))
        {
            throw new UnsupportedBarcodeFormatException($"Barcode format value {(int)format} is unsupported.");
        }
    }
}
