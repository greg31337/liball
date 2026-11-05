namespace barcode.Tests;

internal static class TestImageTransforms
{
    internal static BarcodeImage RenderModules(
        ModuleMatrix modules,
        int moduleScale,
        int quietZoneModules,
        byte dark,
        byte light)
    {
        if (moduleScale <= 0 || quietZoneModules < 0)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(moduleScale));
        }

        int widthModules = modules.Width + (quietZoneModules * 2);
        int heightModules = modules.Height + (quietZoneModules * 2);
        BarcodeImage image = BarcodeTestBuilder.CreateGrayImage(
            widthModules * moduleScale,
            heightModules * moduleScale,
            light);
        byte[] pixels = image.Pixels!;
        int moduleY = 0;
        while (moduleY < modules.Height)
        {
            int moduleX = 0;
            while (moduleX < modules.Width)
            {
                if (modules.GetModule(moduleX, moduleY))
                {
                    FillModule(
                        pixels,
                        image.Stride,
                        (quietZoneModules + moduleX) * moduleScale,
                        (quietZoneModules + moduleY) * moduleScale,
                        moduleScale,
                        dark);
                }

                moduleX = moduleX + 1;
            }

            moduleY = moduleY + 1;
        }

        return image;
    }

    internal static BarcodeImage Rotate(
        BarcodeImage source,
        BarcodeRotation rotation)
    {
        RequirePackedGrayImage(source);
        if (rotation == BarcodeRotation.None)
        {
            return BarcodeTestBuilder.CopyImage(source);
        }

        if (rotation != BarcodeRotation.Clockwise90 &&
            rotation != BarcodeRotation.Clockwise180 &&
            rotation != BarcodeRotation.Clockwise270)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(rotation));
        }

        int width = rotation == BarcodeRotation.Clockwise180
            ? source.Width
            : source.Height;
        int height = rotation == BarcodeRotation.Clockwise180
            ? source.Height
            : source.Width;
        BarcodeImage rotated = BarcodeTestBuilder.CreateGrayImage(
            width,
            height,
            0);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                int targetX;
                int targetY;
                if (rotation == BarcodeRotation.Clockwise90)
                {
                    targetX = source.Height - 1 - y;
                    targetY = x;
                }
                else if (rotation == BarcodeRotation.Clockwise180)
                {
                    targetX = source.Width - 1 - x;
                    targetY = source.Height - 1 - y;
                }
                else
                {
                    targetX = y;
                    targetY = source.Width - 1 - x;
                }
                rotated.Pixels![(targetY * rotated.Stride) + targetX] =
                    source.Pixels![(y * source.Stride) + x];
                x = x + 1;
            }

            y = y + 1;
        }

        CopyResolution(source, rotated);
        return rotated;
    }

    internal static BarcodeImage ReflectHorizontal(BarcodeImage source)
    {
        RequirePackedGrayImage(source);
        BarcodeImage reflected = BarcodeTestBuilder.CreateGrayImage(
            source.Width,
            source.Height,
            0);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                reflected.Pixels![(y * reflected.Stride) +
                    (source.Width - 1 - x)] =
                    source.Pixels![(y * source.Stride) + x];
                x = x + 1;
            }

            y = y + 1;
        }

        CopyResolution(source, reflected);
        return reflected;
    }

    internal static BarcodeImage Invert(BarcodeImage source)
    {
        RequirePackedGrayImage(source);
        BarcodeImage inverted = BarcodeTestBuilder.CopyImage(source);
        int index = 0;
        while (index < inverted.Pixels!.Length)
        {
            inverted.Pixels[index] = (byte)(255 - inverted.Pixels[index]);
            index = index + 1;
        }

        return inverted;
    }

    internal static BarcodeImage Crop(
        BarcodeImage source,
        int left,
        int top,
        int width,
        int height)
    {
        RequirePackedGrayImage(source);
        if (left < 0 || top < 0 || width <= 0 || height <= 0 ||
            left + width > source.Width || top + height > source.Height)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(left));
        }

        BarcodeImage cropped = BarcodeTestBuilder.CreateGrayImage(
            width,
            height,
            0);
        int y = 0;
        while (y < height)
        {
            global::System.Array.Copy(
                source.Pixels!,
                ((top + y) * source.Stride) + left,
                cropped.Pixels!,
                y * cropped.Stride,
                width);
            y = y + 1;
        }

        CopyResolution(source, cropped);
        return cropped;
    }

    internal static BarcodeImage ResizeNearest(
        BarcodeImage source,
        int width,
        int height)
    {
        RequirePackedGrayImage(source);
        if (width <= 0 || height <= 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(width));
        }

        BarcodeImage resized = BarcodeTestBuilder.CreateGrayImage(
            width,
            height,
            0);
        int y = 0;
        while (y < height)
        {
            int sourceY = (y * source.Height) / height;
            int x = 0;
            while (x < width)
            {
                int sourceX = (x * source.Width) / width;
                resized.Pixels![(y * resized.Stride) + x] =
                    source.Pixels![(sourceY * source.Stride) + sourceX];
                x = x + 1;
            }

            y = y + 1;
        }

        CopyResolution(source, resized);
        return resized;
    }

    static void FillModule(
        byte[] pixels,
        int stride,
        int left,
        int top,
        int scale,
        byte value)
    {
        int y = 0;
        while (y < scale)
        {
            int x = 0;
            while (x < scale)
            {
                pixels[((top + y) * stride) + left + x] = value;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    static void CopyResolution(BarcodeImage source, BarcodeImage target)
    {
        target.DpiX = source.DpiX;
        target.DpiY = source.DpiY;
    }

    static void RequirePackedGrayImage(BarcodeImage image)
    {
        if (image.PixelFormat != BarcodePixelFormat.Gray8 ||
            image.Pixels == null || image.Offset != 0 ||
            image.Stride != image.Width ||
            image.Pixels.Length != image.Width * image.Height)
        {
            throw new global::System.ArgumentException(
                "The test image must be a packed Gray8 image.");
        }
    }
}
