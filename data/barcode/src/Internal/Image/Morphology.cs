namespace barcode;

internal static class Morphology
{
    internal const int MaximumRadius = 3;

    internal static ModuleMatrix Dilate(
        ModuleMatrix? source,
        int radius)
    {
        ValidateBinarySource(source);
        ValidateRadius(radius);
        ModuleMatrix image = source == null
            ? new ModuleMatrix(1, 1)
            : source;
        ModuleMatrix result = new ModuleMatrix(
            image.Width,
            image.Height);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                result.SetModule(
                    column,
                    row,
                    HasForegroundInDisk(image, column, row, radius));
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    internal static ModuleMatrix Erode(
        ModuleMatrix? source,
        int radius)
    {
        ValidateBinarySource(source);
        ValidateRadius(radius);
        ModuleMatrix image = source == null
            ? new ModuleMatrix(1, 1)
            : source;
        ModuleMatrix result = new ModuleMatrix(
            image.Width,
            image.Height);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                result.SetModule(
                    column,
                    row,
                    HasOnlyForegroundInDisk(
                        image,
                        column,
                        row,
                        radius));
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    internal static ModuleMatrix Open(
        ModuleMatrix? source,
        int radius)
    {
        ModuleMatrix eroded = Erode(source, radius);
        return Dilate(eroded, radius);
    }

    internal static ModuleMatrix Close(
        ModuleMatrix? source,
        int radius)
    {
        ModuleMatrix dilated = Dilate(source, radius);
        return Erode(dilated, radius);
    }

    internal static ModuleMatrix MajorityFilter(
        ModuleMatrix? source,
        int radius)
    {
        ValidateBinarySource(source);
        ValidateRadius(radius);
        ModuleMatrix image = source == null
            ? new ModuleMatrix(1, 1)
            : source;
        ModuleMatrix result = new ModuleMatrix(
            image.Width,
            image.Height);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                int foregroundCount = 0;
                int sampleCount = 0;
                int offsetY = -radius;
                while (offsetY <= radius)
                {
                    int sampleRow = row + offsetY;
                    if (sampleRow >= 0 && sampleRow < image.Height)
                    {
                        int offsetX = -radius;
                        while (offsetX <= radius)
                        {
                            int sampleColumn = column + offsetX;
                            if (sampleColumn >= 0
                                && sampleColumn < image.Width)
                            {
                                sampleCount = sampleCount + 1;
                                if (image.GetModule(
                                    sampleColumn,
                                    sampleRow))
                                {
                                    foregroundCount = foregroundCount + 1;
                                }
                            }

                            offsetX = offsetX + 1;
                        }
                    }

                    offsetY = offsetY + 1;
                }

                result.SetModule(
                    column,
                    row,
                    foregroundCount * 2 >= sampleCount);
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    internal static ModuleMatrix Invert(ModuleMatrix? source)
    {
        ValidateBinarySource(source);
        ModuleMatrix image = source == null
            ? new ModuleMatrix(1, 1)
            : source;
        ModuleMatrix result = new ModuleMatrix(
            image.Width,
            image.Height);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                result.SetModule(
                    column,
                    row,
                    !image.GetModule(column, row));
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    internal static ManagedGrayImage DilateGray(
        ManagedGrayImage? source,
        int radius)
    {
        return ApplyGrayExtremum(source, radius, true);
    }

    internal static ManagedGrayImage ErodeGray(
        ManagedGrayImage? source,
        int radius)
    {
        return ApplyGrayExtremum(source, radius, false);
    }

    static ManagedGrayImage ApplyGrayExtremum(
        ManagedGrayImage? source,
        int radius,
        bool maximum)
    {
        HistogramAnalyzer.ValidateGrayImage(source);
        ValidateRadius(radius);
        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        ManagedGrayImage result = new ManagedGrayImage(
            image.Width,
            image.Height);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                int selected = maximum ? 0 : 255;
                int offsetY = -radius;
                while (offsetY <= radius)
                {
                    int sampleRow = row + offsetY;
                    if (sampleRow >= 0 && sampleRow < image.Height)
                    {
                        int offsetX = -radius;
                        while (offsetX <= radius)
                        {
                            int sampleColumn = column + offsetX;
                            if (sampleColumn >= 0
                                && sampleColumn < image.Width
                                && IsWithinDisk(
                                    offsetX,
                                    offsetY,
                                    radius))
                            {
                                int value = image.ReadSample(
                                    sampleColumn,
                                    sampleRow);
                                if ((maximum && value > selected)
                                    || (!maximum && value < selected))
                                {
                                    selected = value;
                                }
                            }

                            offsetX = offsetX + 1;
                        }
                    }

                    offsetY = offsetY + 1;
                }

                result.WriteSample(column, row, selected);
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    static bool HasForegroundInDisk(
        ModuleMatrix source,
        int column,
        int row,
        int radius)
    {
        int offsetY = -radius;
        while (offsetY <= radius)
        {
            int sampleRow = row + offsetY;
            if (sampleRow >= 0 && sampleRow < source.Height)
            {
                int offsetX = -radius;
                while (offsetX <= radius)
                {
                    int sampleColumn = column + offsetX;
                    if (sampleColumn >= 0
                        && sampleColumn < source.Width
                        && IsWithinDisk(offsetX, offsetY, radius)
                        && source.GetModule(sampleColumn, sampleRow))
                    {
                        return true;
                    }

                    offsetX = offsetX + 1;
                }
            }

            offsetY = offsetY + 1;
        }

        return false;
    }

    static bool HasOnlyForegroundInDisk(
        ModuleMatrix source,
        int column,
        int row,
        int radius)
    {
        int offsetY = -radius;
        while (offsetY <= radius)
        {
            int offsetX = -radius;
            while (offsetX <= radius)
            {
                if (IsWithinDisk(offsetX, offsetY, radius))
                {
                    int sampleColumn = column + offsetX;
                    int sampleRow = row + offsetY;
                    if (sampleColumn < 0
                        || sampleColumn >= source.Width
                        || sampleRow < 0
                        || sampleRow >= source.Height
                        || !source.GetModule(sampleColumn, sampleRow))
                    {
                        return false;
                    }
                }

                offsetX = offsetX + 1;
            }

            offsetY = offsetY + 1;
        }

        return true;
    }

    static bool IsWithinDisk(int offsetX, int offsetY, int radius)
    {
        return (offsetX * offsetX) + (offsetY * offsetY)
            <= radius * radius;
    }

    static void ValidateBinarySource(ModuleMatrix? source)
    {
        if (source == null)
        {
            throw new BarcodeValidationException(
                "A binary image is required for morphology.");
        }

        CheckedMath.ValidateDimension(source.Width);
        CheckedMath.ValidateDimension(source.Height);
        if (source.Rows == null || source.Rows.Length != source.Height)
        {
            throw new BarcodeValidationException(
                "The binary image row buffer does not match its dimensions.");
        }
    }

    static void ValidateRadius(int radius)
    {
        if (radius <= 0 || radius > MaximumRadius)
        {
            throw new BarcodeValidationException(
                "The morphology radius is outside the supported range.");
        }
    }
}
