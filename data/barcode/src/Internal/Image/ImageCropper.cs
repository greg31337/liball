namespace barcode;

internal class ImageCropResult
{
    internal ManagedGrayImage Image = new ManagedGrayImage(1, 1);
    internal BarcodeRectangle PixelBounds = new BarcodeRectangle();
    internal ImageCoordinateTransform Transform =
        new ImageCoordinateTransform(1.0, 1.0, 0.0, 0.0);
}

internal static class ImageCropper
{
    internal static ImageCropResult CropToRegion(
        ManagedGrayImage? source,
        BarcodeRectangle? region)
    {
        HistogramAnalyzer.ValidateGrayImage(source);
        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        int left = 0;
        int top = 0;
        int right = image.Width;
        int bottom = image.Height;
        if (region != null)
        {
            ValidateRegion(region, image.Width, image.Height);
            left = (int)global::System.Math.Floor(region.X);
            top = (int)global::System.Math.Floor(region.Y);
            right = (int)global::System.Math.Ceiling(
                region.X + region.Width);
            bottom = (int)global::System.Math.Ceiling(
                region.Y + region.Height);
        }

        int width = right - left;
        int height = bottom - top;
        ManagedGrayImage cropped = new ManagedGrayImage(width, height);
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                cropped.WriteSample(
                    column,
                    row,
                    image.ReadSample(left + column, top + row));
                column = column + 1;
            }

            row = row + 1;
        }

        ImageCropResult result = new ImageCropResult();
        result.Image = cropped;
        result.PixelBounds = GeometryMath.CreateRectangle(
            left,
            top,
            width,
            height);
        result.Transform = new ImageCoordinateTransform(
            1.0,
            1.0,
            left,
            top);
        return result;
    }

    static void ValidateRegion(
        BarcodeRectangle region,
        int width,
        int height)
    {
        GeometryMath.ValidateRectangle(region);
        if (region.Width <= 0.0 || region.Height <= 0.0)
        {
            throw new BarcodeValidationException(
                "The image region of interest must have positive dimensions.");
        }

        double right = region.X + region.Width;
        double bottom = region.Y + region.Height;
        if (region.X < 0.0
            || region.Y < 0.0
            || right > width
            || bottom > height)
        {
            throw new BarcodeValidationException(
                "The image region of interest exceeds the source image.");
        }
    }
}
