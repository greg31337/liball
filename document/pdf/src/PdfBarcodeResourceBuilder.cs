using System;

namespace pdf;

internal static class PdfBarcodeResourceBuilder
{
    internal static PdfFormXObject CreateQrForm(PdfDocument document, string text, double moduleSize, PdfQrErrorCorrection errorCorrection, int quietZone)
    {
        ValidateQrDimensions(moduleSize, quietZone);
        PdfQrMatrix matrix = PdfQrEncoder.Encode(text, errorCorrection);
        double width = (matrix.Size + quietZone * 2) * moduleSize;
        PdfFormXObject form = document.AddForm(width, width);
        form.Canvas.SetFillColor(0, 0, 0);
        for (int y = 0; y < matrix.Size; y++)
        {
            int x = 0;
            while (x < matrix.Size)
            {
                if (!matrix.Modules[x, y])
                {
                    x++;
                    continue;
                }

                int start = x;
                while (x < matrix.Size && matrix.Modules[x, y])
                {
                    x++;
                }

                double drawX = (quietZone + start) * moduleSize;
                double drawY = (quietZone + matrix.Size - 1 - y) * moduleSize;
                form.Canvas.FillRectangle(drawX, drawY, (x - start) * moduleSize, moduleSize);
            }
        }

        return form;
    }

    internal static PdfImage CreateQrImage(PdfDocument document, string text, int modulePixels, PdfQrErrorCorrection errorCorrection, int quietZone)
    {
        if (modulePixels <= 0 || modulePixels > 64)
        {
            throw new ArgumentOutOfRangeException("modulePixels", "QR Code image module size must be from one through sixty-four pixels.");
        }

        ValidateQuietZone(quietZone);
        PdfQrMatrix matrix = PdfQrEncoder.Encode(text, errorCorrection);
        int moduleCount = matrix.Size + quietZone * 2;
        if (moduleCount > 10000 / modulePixels)
        {
            throw new ArgumentOutOfRangeException("modulePixels", "QR Code image dimensions are too large.");
        }

        int size = moduleCount * modulePixels;
        byte[] pixels = new byte[size * size];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = 255;
        }

        for (int y = 0; y < matrix.Size; y++)
        {
            for (int x = 0; x < matrix.Size; x++)
            {
                if (matrix.Modules[x, y])
                {
                    PaintModulePixels(pixels, size, (quietZone + x) * modulePixels, (quietZone + y) * modulePixels, modulePixels);
                }
            }
        }

        return document.AddGrayscaleImage(size, size, pixels);
    }

    internal static PdfFormXObject CreateLinearForm(PdfDocument document, string text, PdfBarcodeFormat format, double moduleWidth, double barHeight, bool includeText)
    {
        ValidateLinearDimensions(moduleWidth, barHeight);
        PdfLinearBarcode barcode = PdfLinearBarcodeEncoder.Encode(text, format);
        double width = barcode.Modules.Length * moduleWidth;
        if (double.IsInfinity(width) || width > 1000000)
        {
            throw new ArgumentOutOfRangeException("moduleWidth", "Barcode form width is too large.");
        }

        double textHeight = includeText ? 14 : 0;
        PdfFormXObject form = document.AddForm(width, barHeight + textHeight);
        form.Canvas.SetFillColor(0, 0, 0);
        int index = 0;
        while (index < barcode.Modules.Length)
        {
            if (!barcode.Modules[index])
            {
                index++;
                continue;
            }

            int start = index;
            while (index < barcode.Modules.Length && barcode.Modules[index])
            {
                index++;
            }

            form.Canvas.FillRectangle(start * moduleWidth, textHeight, (index - start) * moduleWidth, barHeight);
        }

        if (includeText)
        {
            DrawHumanReadableText(form.Canvas, barcode.DisplayText, width);
        }

        return form;
    }

    internal static PdfImage CreateLinearImage(PdfDocument document, string text, PdfBarcodeFormat format, int modulePixels, int barHeightPixels)
    {
        if (modulePixels <= 0 || modulePixels > 64)
        {
            throw new ArgumentOutOfRangeException("modulePixels", "Barcode image module width must be from one through sixty-four pixels.");
        }

        if (barHeightPixels <= 0 || barHeightPixels > 10000)
        {
            throw new ArgumentOutOfRangeException("barHeightPixels", "Barcode image height must be from one through ten thousand pixels.");
        }

        PdfLinearBarcode barcode = PdfLinearBarcodeEncoder.Encode(text, format);
        if (barcode.Modules.Length > 1000000 / modulePixels)
        {
            throw new ArgumentOutOfRangeException("modulePixels", "Barcode image width is too large.");
        }

        int width = barcode.Modules.Length * modulePixels;
        if (barHeightPixels > 50000000 / width)
        {
            throw new ArgumentOutOfRangeException("barHeightPixels", "Barcode image contains too many pixels.");
        }

        byte[] pixels = new byte[width * barHeightPixels];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = 255;
        }

        for (int module = 0; module < barcode.Modules.Length; module++)
        {
            if (barcode.Modules[module])
            {
                for (int y = 0; y < barHeightPixels; y++)
                {
                    int row = y * width + module * modulePixels;
                    for (int x = 0; x < modulePixels; x++)
                    {
                        pixels[row + x] = 0;
                    }
                }
            }
        }

        return document.AddGrayscaleImage(width, barHeightPixels, pixels);
    }

    internal static void ValidateQrDimensions(double moduleSize, int quietZone)
    {
        if (moduleSize <= 0 || moduleSize > 100 || double.IsNaN(moduleSize) || double.IsInfinity(moduleSize))
        {
            throw new ArgumentOutOfRangeException("moduleSize", "QR Code module size must be positive, finite, and no more than one hundred points.");
        }

        ValidateQuietZone(quietZone);
    }

    internal static void ValidateQuietZone(int quietZone)
    {
        if (quietZone < 0 || quietZone > 32)
        {
            throw new ArgumentOutOfRangeException("quietZone", "QR Code quiet zone must be from zero through thirty-two modules.");
        }
    }

    internal static void ValidateLinearDimensions(double moduleWidth, double barHeight)
    {
        if (moduleWidth <= 0 || moduleWidth > 100 || double.IsNaN(moduleWidth) || double.IsInfinity(moduleWidth))
        {
            throw new ArgumentOutOfRangeException("moduleWidth", "Barcode module width must be positive, finite, and no more than one hundred points.");
        }

        if (barHeight <= 0 || barHeight > 100000 || double.IsNaN(barHeight) || double.IsInfinity(barHeight))
        {
            throw new ArgumentOutOfRangeException("barHeight", "Barcode height must be positive, finite, and no more than one hundred thousand points.");
        }
    }

    internal static void PaintModulePixels(byte[] pixels, int imageWidth, int startX, int startY, int modulePixels)
    {
        for (int y = 0; y < modulePixels; y++)
        {
            int row = (startY + y) * imageWidth + startX;
            for (int x = 0; x < modulePixels; x++)
            {
                pixels[row + x] = 0;
            }
        }
    }

    internal static void DrawHumanReadableText(PdfCanvas canvas, string text, double width)
    {
        double fontSize = 10;
        double textWidth = PdfCanvas.MeasureText(text, fontSize, PdfFont.Helvetica);
        if (textWidth > width - 2 && textWidth > 0)
        {
            fontSize = fontSize * Math.Max(width * 0.9, 0.01) / textWidth;
            textWidth = PdfCanvas.MeasureText(text, fontSize, PdfFont.Helvetica);
        }

        canvas.DrawTextWithFont(text, (width - textWidth) / 2, 2, fontSize, PdfFont.Helvetica);
    }
}
