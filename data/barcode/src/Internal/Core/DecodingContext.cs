namespace barcode;

internal class DecodingContext
{
    internal const int DefaultMaximumWork = 1000000;
    internal const int DefaultMaximumBytes = 67108864;
    internal const int DefaultMaximumModules = 16777216;

    internal BarcodeDecodeRequest Request;
    internal BarcodeFormat[] Formats;
    internal ResourceBudget Resources;

    internal DecodingContext(BarcodeDecodeRequest request)
    {
        if (request == null)
        {
            throw new BarcodeDecodingException("Barcode decode request must not be null.");
        }

        Request = request;
        Formats = FormatDispatcher.ResolveDecodingFormats(request.Options);
        Resources = new ResourceBudget(DefaultMaximumWork, DefaultMaximumBytes, DefaultMaximumModules);

        if (request.Image != null)
        {
            if (request.Image.Pixels != null)
            {
                Resources.ReserveBytes(request.Image.Pixels.Length);
            }

            Resources.ReserveModules(CheckedMath.Multiply(request.Image.Width, request.Image.Height));
        }

        if (request.Symbol != null && request.Symbol.Modules != null)
        {
            Resources.ReserveModules(request.Symbol.Modules.Length);
        }
    }

    internal int ConsumeWorkUnits(int amount)
    {
        return Resources.ConsumeWork(amount);
    }

    internal int ReserveInputBytes(int amount)
    {
        return Resources.ReserveBytes(amount);
    }

    internal int ReserveModuleCount(int amount)
    {
        return Resources.ReserveModules(amount);
    }

    internal bool IsFormatAllowed(BarcodeFormat format)
    {
        int index = 0;
        while (index < Formats.Length)
        {
            if (Formats[index] == format)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal BarcodeDecodeResult[] CopyDecodeResultsResult(BarcodeDecodeResult[] results)
    {
        if (results == null)
        {
            throw new BarcodeDecodingException("Decode results must not be null.");
        }

        BarcodeDecodeResult[]? copy = ResultCopier.CopyDecodeResultArray(results);
        if (copy == null)
        {
            throw new BarcodeDecodingException("Decode results could not be copied.");
        }

        return copy;
    }

    internal BarcodeDecodeAttempt CopyDecodeAttemptResult(BarcodeDecodeAttempt attempt)
    {
        if (attempt == null)
        {
            throw new BarcodeDecodingException("Decode attempt must not be null.");
        }

        return ResultCopier.CopyDecodeAttempt(attempt);
    }
}
