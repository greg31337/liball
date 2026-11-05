namespace barcode.Tests;

internal static class TestModuleTransforms
{
    internal static ModuleMatrix Rotate(
        ModuleMatrix source,
        BarcodeRotation rotation)
    {
        switch (rotation)
        {
            case BarcodeRotation.None:
                return BarcodeTestBuilder.CopyMatrix(source);
            case BarcodeRotation.Clockwise90:
                return source.RotateClockwise90();
            case BarcodeRotation.Clockwise180:
                return source.Rotate180();
            case BarcodeRotation.Clockwise270:
                return source.RotateClockwise270();
            default:
                throw new global::System.ArgumentOutOfRangeException(
                    nameof(rotation));
        }
    }

    internal static ModuleMatrix ReflectHorizontal(ModuleMatrix source)
    {
        return source.ReflectHorizontal();
    }

    internal static ModuleMatrix ReflectVertical(ModuleMatrix source)
    {
        ModuleMatrix reflected = new ModuleMatrix(
            source.Width,
            source.Height);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                reflected.SetModule(
                    x,
                    source.Height - 1 - y,
                    source.GetModule(x, y));
                x = x + 1;
            }

            y = y + 1;
        }

        return reflected;
    }

    internal static ModuleMatrix Invert(ModuleMatrix source)
    {
        ModuleMatrix inverted = new ModuleMatrix(
            source.Width,
            source.Height);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                inverted.SetModule(x, y, !source.GetModule(x, y));
                x = x + 1;
            }

            y = y + 1;
        }

        return inverted;
    }

    internal static ModuleMatrix Scale(
        ModuleMatrix source,
        int horizontalScale,
        int verticalScale)
    {
        if (horizontalScale <= 0 || verticalScale <= 0)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(horizontalScale));
        }

        ModuleMatrix scaled = new ModuleMatrix(
            source.Width * horizontalScale,
            source.Height * verticalScale);
        int y = 0;
        while (y < scaled.Height)
        {
            int x = 0;
            while (x < scaled.Width)
            {
                scaled.SetModule(
                    x,
                    y,
                    source.GetModule(
                        x / horizontalScale,
                        y / verticalScale));
                x = x + 1;
            }

            y = y + 1;
        }

        return scaled;
    }

    internal static ModuleMatrix AddQuietZone(
        ModuleMatrix source,
        int left,
        int top,
        int right,
        int bottom)
    {
        if (left < 0 || top < 0 || right < 0 || bottom < 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(left));
        }

        ModuleMatrix padded = new ModuleMatrix(
            left + source.Width + right,
            top + source.Height + bottom);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                padded.SetModule(
                    left + x,
                    top + y,
                    source.GetModule(x, y));
                x = x + 1;
            }

            y = y + 1;
        }

        return padded;
    }

    internal static ModuleMatrix Crop(
        ModuleMatrix source,
        int left,
        int top,
        int width,
        int height)
    {
        if (left < 0 || top < 0 || width <= 0 || height <= 0 ||
            left + width > source.Width || top + height > source.Height)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(left));
        }

        ModuleMatrix cropped = new ModuleMatrix(width, height);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                cropped.SetModule(
                    x,
                    y,
                    source.GetModule(left + x, top + y));
                x = x + 1;
            }

            y = y + 1;
        }

        return cropped;
    }
}
