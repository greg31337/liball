namespace barcode.Tests;

internal static class BarcodeTestBuilder
{
    internal static BarcodePayload CreatePayload(
        string? text,
        byte[]? rawBytes,
        string? characterEncoding,
        bool isGs1)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = CopyBytes(rawBytes);
        payload.CharacterEncoding = characterEncoding;
        payload.IsGs1 = isGs1;
        return payload;
    }

    internal static BarcodePayload CreateAsciiPayload(string text)
    {
        byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes(text);
        return CreatePayload(text, bytes, "ASCII", false);
    }

    internal static BarcodePayload CreateBinaryPayload(byte[] bytes)
    {
        return CreatePayload(null, bytes, null, false);
    }

    internal static BarcodeEncodeRequest CreateEncodeRequest(
        BarcodeFormat format,
        BarcodePayload payload)
    {
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = format;
        request.Payload = ResultCopier.CopyPayload(payload);
        return request;
    }

    internal static BarcodeSymbol CreateSymbol(
        BarcodeFormat format,
        BarcodeDimension dimension,
        ModuleMatrix modules,
        BarcodePayload? payload)
    {
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = format;
        symbol.Dimension = dimension;
        symbol.WidthModules = modules.Width;
        symbol.HeightModules = modules.Height;
        symbol.Modules = CopyModuleBytes(modules);
        symbol.Payload = ResultCopier.CopyPayload(payload);
        symbol.RequiredQuietZone = new BarcodeQuietZone();
        return symbol;
    }

    internal static BarcodeSymbol CreateLinearSymbol(
        BarcodeFormat format,
        byte[] modules,
        BarcodePayload? payload)
    {
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = format;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = modules.Length;
        symbol.HeightModules = 1;
        symbol.Modules = CopyRequiredBytes(modules);
        symbol.Payload = ResultCopier.CopyPayload(payload);
        symbol.RequiredQuietZone = new BarcodeQuietZone();
        return symbol;
    }

    internal static BarcodeImage CreateGrayImage(
        int width,
        int height,
        byte value)
    {
        if (width <= 0 || height <= 0)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(width));
        }

        byte[] pixels = new byte[width * height];
        int index = 0;
        while (index < pixels.Length)
        {
            pixels[index] = value;
            index = index + 1;
        }

        BarcodeImage image = new BarcodeImage();
        image.Pixels = pixels;
        image.Width = width;
        image.Height = height;
        image.Stride = width;
        image.PixelFormat = BarcodePixelFormat.Gray8;
        return image;
    }

    internal static BarcodeImage CreateGrayImageFromPixels(
        int width,
        int height,
        byte[] pixels)
    {
        if (width <= 0 || height <= 0 || pixels.Length != width * height)
        {
            throw new global::System.ArgumentException(
                "Gray image dimensions must match the pixel buffer.");
        }

        BarcodeImage image = new BarcodeImage();
        image.Pixels = CopyRequiredBytes(pixels);
        image.Width = width;
        image.Height = height;
        image.Stride = width;
        image.PixelFormat = BarcodePixelFormat.Gray8;
        return image;
    }

    internal static BarcodeDecodeResult CreateDecodeResult(
        BarcodeFormat format,
        BarcodeDimension dimension,
        BarcodePayload payload)
    {
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Format = format;
        result.Dimension = dimension;
        result.Payload = ResultCopier.CopyPayload(payload);
        result.Metadata = new BarcodeDecodeMetadata();
        result.Diagnostics = new BarcodeDiagnostic[0];
        return result;
    }

    internal static StructuredAppendInfo CreateStructuredAppend(
        BarcodeFormat format,
        int index,
        int count,
        string? fileIdentifier)
    {
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = format;
        append.Index = index;
        append.Count = count;
        append.FileIdentifier = fileIdentifier;
        append.IsLastSegment = index == count - 1;
        return append;
    }

    internal static ModuleMatrix CopyMatrix(ModuleMatrix source)
    {
        ModuleMatrix copy = new ModuleMatrix(source.Width, source.Height);
        copy.LoadModules(source.CopyModules());
        return copy;
    }

    internal static BarcodeImage CopyImage(BarcodeImage source)
    {
        BarcodeImage copy = new BarcodeImage();
        copy.Pixels = CopyBytes(source.Pixels);
        copy.Width = source.Width;
        copy.Height = source.Height;
        copy.Stride = source.Stride;
        copy.Offset = source.Offset;
        copy.PixelFormat = source.PixelFormat;
        copy.DpiX = source.DpiX;
        copy.DpiY = source.DpiY;
        return copy;
    }

    internal static byte[]? CopyBytes(byte[]? source)
    {
        if (source == null)
        {
            return null;
        }

        return CopyRequiredBytes(source);
    }

    internal static byte[] CopyRequiredBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        source.CopyTo(copy, 0);
        return copy;
    }

    internal static int[] CopyIntegers(int[] source)
    {
        int[] copy = new int[source.Length];
        source.CopyTo(copy, 0);
        return copy;
    }

    static byte[] CopyModuleBytes(ModuleMatrix source)
    {
        byte[] modules = new byte[source.Width * source.Height];
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                modules[(y * source.Width) + x] =
                    source.GetModule(x, y) ? (byte)1 : (byte)0;
                x = x + 1;
            }

            y = y + 1;
        }

        return modules;
    }
}
