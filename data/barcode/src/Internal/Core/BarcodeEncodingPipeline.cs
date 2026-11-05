namespace barcode;

internal static class BarcodeEncodingPipeline
{
    internal static BarcodeSymbol Encode(
        EncodingContext context)
    {
        BarcodeEncodeRequest request = context.Request;
        BarcodePayload payload = RequirePayload(request.Payload);
        BarcodeSymbol symbol;
        switch (context.Format)
        {
            case BarcodeFormat.Code128:
                symbol = EncodeCode128(payload, request);
                break;
            case BarcodeFormat.Gs1128:
                symbol = EncodeGs1128(payload, request);
                break;
            case BarcodeFormat.Ean8:
            case BarcodeFormat.Ean13:
            case BarcodeFormat.UpcA:
            case BarcodeFormat.UpcE:
            case BarcodeFormat.EanUpcAddon2:
            case BarcodeFormat.EanUpcAddon5:
                symbol = EncodeEanUpc(context.Format, payload, request);
                break;
            case BarcodeFormat.Code39:
                symbol = Code39Layout.CreateLayout(
                    RequireText(payload),
                    payload,
                    request.Code39,
                    request.Options).Symbol;
                break;
            case BarcodeFormat.Itf:
                symbol = ItfLayout.CreateLayout(
                    RequireText(payload),
                    payload,
                    request.Itf,
                    request.Options).Symbol;
                break;
            case BarcodeFormat.Itf14:
                symbol = Itf14Profile.CreateLayout(
                    RequireText(payload),
                    payload,
                    request.Itf,
                    request.Options).Symbol;
                break;
            case BarcodeFormat.Codabar:
                symbol = CodabarLayout.CreateLayout(
                    RequireText(payload),
                    payload,
                    request.Codabar,
                    request.Options).Symbol;
                break;
            case BarcodeFormat.DataMatrix:
                symbol = DataMatrixEncoder.Encode(
                    CreatePayloadBytes(payload),
                    payload,
                    request.DataMatrix,
                    request.Options,
                    false).Symbol;
                break;
            case BarcodeFormat.Gs1DataMatrix:
                symbol = EncodeGs1DataMatrix(payload, request);
                break;
            case BarcodeFormat.Pdf417:
            case BarcodeFormat.CompactPdf417:
            case BarcodeFormat.MacroPdf417:
                symbol = Pdf417Encoder.Encode(
                    CreatePayloadBytes(payload),
                    payload,
                    request.Pdf417,
                    request.Options).Symbol;
                break;
            default:
                throw new BarcodeEncodingException(
                    "The requested barcode format is unsupported.");
        }

        context.ConsumeWorkUnits(1);
        context.ReserveModuleCount(CheckedMath.Multiply(
            symbol.WidthModules,
            symbol.HeightModules));
        return context.CopySymbolResult(symbol);
    }

    static BarcodeSymbol EncodeCode128(
        BarcodePayload payload,
        BarcodeEncodeRequest request)
    {
        byte[] bytes = CreatePayloadBytes(payload);
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(
            bytes,
            request.Code128);
        return Code128Layout.CreateSymbol(
            encoded,
            payload,
            request.Options);
    }

    static BarcodeSymbol EncodeGs1128(
        BarcodePayload payload,
        BarcodeEncodeRequest request)
    {
        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementString(RequireText(payload));
        return Gs1128Codec.EncodeElements(
            parsed.Elements,
            request.Code128,
            request.Options).Symbol;
    }

    static BarcodeSymbol EncodeGs1DataMatrix(
        BarcodePayload payload,
        BarcodeEncodeRequest request)
    {
        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementString(RequireText(payload));
        return Gs1DataMatrixCodec.EncodeElements(
            parsed.Elements,
            request.DataMatrix,
            request.Options).Symbol;
    }

    static BarcodeSymbol EncodeEanUpc(
        BarcodeFormat format,
        BarcodePayload payload,
        BarcodeEncodeRequest request)
    {
        string text = RequireText(payload);
        if (format == BarcodeFormat.EanUpcAddon2 ||
            format == BarcodeFormat.EanUpcAddon5)
        {
            EanUpcEncodedData addOn = EanUpcAddon.Encode(text);
            return EanUpcLayout.CreateAddOnSymbol(
                addOn,
                payload,
                request.Options);
        }

        BarcodeChecksumMode checksumMode = ResolveEanChecksumMode(request);
        EanUpcEncodedData encoded;
        if (format == BarcodeFormat.UpcE)
        {
            int numberSystem = request.EanUpc == null
                ? 0
                : request.EanUpc.UpcENumberSystem;
            encoded = EanUpcEncoder.EncodeUpcE(
                text,
                numberSystem,
                checksumMode);
        }
        else
        {
            encoded = EanUpcEncoder.Encode(
                text,
                format,
                checksumMode);
            if (format == BarcodeFormat.UpcA &&
                request.EanUpc != null &&
                request.EanUpc.CompressToUpcE)
            {
                string compressed = UpcECompressor.CompressCompleteUpcA(
                    encoded.Text);
                encoded = EanUpcEncoder.EncodeUpcE(
                    compressed,
                    compressed[0] - '0',
                    BarcodeChecksumMode.VerifyKeep);
            }
        }

        EanUpcAddonAssociation association;
        if (encoded.Format == BarcodeFormat.Ean8)
        {
            association = new EanUpcAddonAssociation();
            association.Main = encoded;
        }
        else
        {
            association = EanUpcAddon.Associate(
                encoded,
                request.EanUpc);
        }
        return EanUpcLayout.CreateSymbol(
            association,
            payload,
            request.Options,
            request.EanUpc);
    }

    static BarcodeChecksumMode ResolveEanChecksumMode(
        BarcodeEncodeRequest request)
    {
        if (request.EanUpc != null)
        {
            return request.EanUpc.ChecksumMode;
        }

        return request.Options == null
            ? BarcodeChecksumMode.None
            : request.Options.ChecksumMode;
    }

    static BarcodePayload RequirePayload(BarcodePayload? payload)
    {
        if (payload == null)
        {
            throw new BarcodeEncodingException(
                "A barcode payload is required.");
        }

        return payload;
    }

    static string RequireText(BarcodePayload payload)
    {
        if (payload.Text == null)
        {
            throw new BarcodeEncodingException(
                "The selected barcode format requires a text payload.");
        }

        return payload.Text;
    }

    static byte[] CreatePayloadBytes(BarcodePayload payload)
    {
        if (payload.RawBytes != null && payload.RawBytes.Length > 0)
        {
            byte[]? copy = ResultCopier.CopyByteArray(payload.RawBytes);
            return copy == null ? new byte[0] : copy;
        }

        string text = RequireText(payload);
        byte[] bytes = new byte[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            bytes[index] = (byte)text[index];
            index = index + 1;
        }

        return bytes;
    }
}
