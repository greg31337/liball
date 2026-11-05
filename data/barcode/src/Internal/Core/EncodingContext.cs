namespace barcode;

internal class EncodingContext
{
    internal const int DefaultMaximumWork = 1000000;
    internal const int DefaultMaximumBytes = 67108864;
    internal const int DefaultMaximumModules = 16777216;

    internal BarcodeEncodeRequest Request;
    internal BarcodeFormat Format;
    internal BarcodeCodecFamily Family;
    internal ResourceBudget Resources;

    internal EncodingContext(BarcodeEncodeRequest request)
    {
        Format = FormatDispatcher.ResolveEncodingFormat(request);
        Request = request;
        Family = FormatDispatcher.ResolveCodecFamily(Format);
        Resources = new ResourceBudget(DefaultMaximumWork, DefaultMaximumBytes, DefaultMaximumModules);

        BarcodePayload? payload = request.Payload;
        if (payload != null)
        {
            if (payload.RawBytes != null)
            {
                Resources.ReserveBytes(payload.RawBytes.Length);
            }

            if (payload.Text != null)
            {
                Resources.ReserveBytes(CheckedMath.Multiply(payload.Text.Length, 2));
            }
        }
    }

    internal int ConsumeWorkUnits(int amount)
    {
        return Resources.ConsumeWork(amount);
    }

    internal int ReservePayloadBytes(int amount)
    {
        return Resources.ReserveBytes(amount);
    }

    internal int ReserveModuleCount(int amount)
    {
        return Resources.ReserveModules(amount);
    }

    internal BarcodeSymbol CopySymbolResult(BarcodeSymbol symbol)
    {
        if (symbol == null)
        {
            throw new BarcodeEncodingException("Encoded symbol must not be null.");
        }

        return ResultCopier.CopySymbol(symbol);
    }
}
