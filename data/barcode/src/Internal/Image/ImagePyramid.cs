namespace barcode;

internal class ImagePyramidLevel
{
    internal ManagedGrayImage Image = new ManagedGrayImage(1, 1);
    internal int Level;
    internal int Scale;
    internal ImageCoordinateTransform Transform =
        new ImageCoordinateTransform(1.0, 1.0, 0.0, 0.0);
}

internal class ImagePyramid
{
    internal ImagePyramidLevel[] Levels = new ImagePyramidLevel[0];
    internal BarcodeRectangle SourceBounds = new BarcodeRectangle();
}

internal static class ImagePyramidBuilder
{
    internal const int MaximumLevelCount = 16;

    internal static ImagePyramid Build(
        ManagedGrayImage? source,
        BarcodeRectangle? region,
        int maximumLevels,
        int minimumDimension)
    {
        if (maximumLevels <= 0
            || maximumLevels > MaximumLevelCount)
        {
            throw new BarcodeValidationException(
                "The image pyramid level count is outside the supported range.");
        }

        if (minimumDimension <= 0)
        {
            throw new BarcodeValidationException(
                "The image pyramid minimum dimension must be positive.");
        }

        ImageCropResult crop = ImageCropper.CropToRegion(source, region);
        global::System.Collections.Generic.List<ImagePyramidLevel> levels =
            new global::System.Collections.Generic.List<ImagePyramidLevel>();
        ManagedGrayImage current = crop.Image;
        int levelIndex = 0;
        int scale = 1;
        while (levelIndex < maximumLevels)
        {
            ImagePyramidLevel level = new ImagePyramidLevel();
            level.Image = current;
            level.Level = levelIndex;
            level.Scale = scale;
            level.Transform = new ImageCoordinateTransform(
                scale,
                scale,
                crop.PixelBounds.X,
                crop.PixelBounds.Y);
            levels.Add(level);
            if (current.Width <= minimumDimension
                || current.Height <= minimumDimension
                || levelIndex + 1 >= maximumLevels)
            {
                break;
            }

            current = DownsampleByTwo(current);
            scale = CheckedMath.Multiply(scale, 2);
            levelIndex = levelIndex + 1;
        }

        ImagePyramid result = new ImagePyramid();
        result.Levels = levels.ToArray();
        result.SourceBounds = GeometryMath.CopyRectangleValue(
            crop.PixelBounds);
        return result;
    }

    internal static ManagedGrayImage DownsampleByTwo(
        ManagedGrayImage? source)
    {
        HistogramAnalyzer.ValidateGrayImage(source);
        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        int width = (image.Width + 1) / 2;
        int height = (image.Height + 1) / 2;
        ManagedGrayImage result = new ManagedGrayImage(width, height);
        int row = 0;
        while (row < height)
        {
            int sourceTop = row * 2;
            int sourceBottom = sourceTop + 2;
            if (sourceBottom > image.Height)
            {
                sourceBottom = image.Height;
            }

            int column = 0;
            while (column < width)
            {
                int sourceLeft = column * 2;
                int sourceRight = sourceLeft + 2;
                if (sourceRight > image.Width)
                {
                    sourceRight = image.Width;
                }

                int sum = 0;
                int count = 0;
                int sourceRow = sourceTop;
                while (sourceRow < sourceBottom)
                {
                    int sourceColumn = sourceLeft;
                    while (sourceColumn < sourceRight)
                    {
                        sum = sum + image.ReadSample(
                            sourceColumn,
                            sourceRow);
                        count = count + 1;
                        sourceColumn = sourceColumn + 1;
                    }

                    sourceRow = sourceRow + 1;
                }

                result.WriteSample(
                    column,
                    row,
                    (sum + (count / 2)) / count);
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }
}

internal static class ImagePyramidSelector
{
    internal static ImagePyramidLevel SelectLevel(
        ImagePyramid? pyramid,
        double sourceFeaturePixels,
        double minimumLevelFeaturePixels,
        double maximumLevelFeaturePixels)
    {
        ValidateSelection(
            pyramid,
            sourceFeaturePixels,
            minimumLevelFeaturePixels,
            maximumLevelFeaturePixels);
        ImagePyramid imagePyramid = pyramid == null
            ? new ImagePyramid()
            : pyramid;
        double target = (minimumLevelFeaturePixels
            + maximumLevelFeaturePixels) * 0.5;
        int bestIndex = 0;
        double bestDistance = double.MaxValue;
        bool foundInRange = false;
        int index = 0;
        while (index < imagePyramid.Levels.Length)
        {
            ImagePyramidLevel level = imagePyramid.Levels[index];
            double levelFeaturePixels = sourceFeaturePixels
                / level.Scale;
            double distance;
            bool inRange = levelFeaturePixels
                >= minimumLevelFeaturePixels
                && levelFeaturePixels <= maximumLevelFeaturePixels;
            if (levelFeaturePixels < minimumLevelFeaturePixels)
            {
                distance = minimumLevelFeaturePixels
                    - levelFeaturePixels;
            }
            else if (levelFeaturePixels > maximumLevelFeaturePixels)
            {
                distance = levelFeaturePixels
                    - maximumLevelFeaturePixels;
            }
            else
            {
                distance = global::System.Math.Abs(
                    levelFeaturePixels - target);
            }

            if ((inRange && !foundInRange)
                || (inRange == foundInRange
                    && distance < bestDistance))
            {
                bestDistance = distance;
                bestIndex = index;
                foundInRange = inRange;
            }

            index = index + 1;
        }

        return imagePyramid.Levels[bestIndex];
    }

    static void ValidateSelection(
        ImagePyramid? pyramid,
        double sourceFeaturePixels,
        double minimumLevelFeaturePixels,
        double maximumLevelFeaturePixels)
    {
        if (pyramid == null
            || pyramid.Levels == null
            || pyramid.Levels.Length == 0)
        {
            throw new BarcodeValidationException(
                "An image pyramid with at least one level is required.");
        }

        if (!GeometryMath.IsFiniteValue(sourceFeaturePixels)
            || !GeometryMath.IsFiniteValue(minimumLevelFeaturePixels)
            || !GeometryMath.IsFiniteValue(maximumLevelFeaturePixels)
            || sourceFeaturePixels <= 0.0
            || minimumLevelFeaturePixels <= 0.0
            || maximumLevelFeaturePixels < minimumLevelFeaturePixels)
        {
            throw new BarcodeValidationException(
                "Image pyramid feature-size limits are invalid.");
        }

        int index = 0;
        while (index < pyramid.Levels.Length)
        {
            ImagePyramidLevel level = pyramid.Levels[index];
            if (level == null
                || level.Image == null
                || level.Transform == null
                || level.Scale <= 0)
            {
                throw new BarcodeValidationException(
                    "The image pyramid contains an invalid level.");
            }

            index = index + 1;
        }
    }
}
