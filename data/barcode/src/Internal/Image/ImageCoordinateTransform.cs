namespace barcode;

internal class ImageCoordinateTransform
{
    internal double ScaleX;
    internal double ScaleY;
    internal double OffsetX;
    internal double OffsetY;

    internal ImageCoordinateTransform(
        double scaleX,
        double scaleY,
        double offsetX,
        double offsetY)
    {
        GeometryMath.ValidateCoordinate(scaleX);
        GeometryMath.ValidateCoordinate(scaleY);
        GeometryMath.ValidateCoordinate(offsetX);
        GeometryMath.ValidateCoordinate(offsetY);
        if (scaleX <= 0.0 || scaleY <= 0.0)
        {
            throw new BarcodeValidationException(
                "Image coordinate scales must be positive.");
        }

        ScaleX = scaleX;
        ScaleY = scaleY;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    internal BarcodePoint RestorePoint(BarcodePoint? point)
    {
        GeometryMath.ValidatePoint(point);
        BarcodePoint value = point == null
            ? new BarcodePoint()
            : point;
        return GeometryMath.CreatePoint(
            OffsetX + (value.X * ScaleX),
            OffsetY + (value.Y * ScaleY));
    }

    internal BarcodePoint MapSourcePoint(BarcodePoint? point)
    {
        GeometryMath.ValidatePoint(point);
        BarcodePoint value = point == null
            ? new BarcodePoint()
            : point;
        return GeometryMath.CreatePoint(
            (value.X - OffsetX) / ScaleX,
            (value.Y - OffsetY) / ScaleY);
    }

    internal BarcodeRectangle RestoreRectangle(
        BarcodeRectangle? rectangle)
    {
        if (rectangle == null)
        {
            throw new BarcodeValidationException(
                "An image rectangle is required for coordinate restoration.");
        }

        GeometryMath.ValidateRectangle(rectangle);
        return GeometryMath.CreateRectangle(
            OffsetX + (rectangle.X * ScaleX),
            OffsetY + (rectangle.Y * ScaleY),
            rectangle.Width * ScaleX,
            rectangle.Height * ScaleY);
    }

    internal BarcodeRectangle MapSourceRectangle(
        BarcodeRectangle? rectangle)
    {
        if (rectangle == null)
        {
            throw new BarcodeValidationException(
                "A source rectangle is required for coordinate mapping.");
        }

        GeometryMath.ValidateRectangle(rectangle);
        return GeometryMath.CreateRectangle(
            (rectangle.X - OffsetX) / ScaleX,
            (rectangle.Y - OffsetY) / ScaleY,
            rectangle.Width / ScaleX,
            rectangle.Height / ScaleY);
    }

    internal BarcodeQuadrilateral RestoreQuadrilateral(
        BarcodeQuadrilateral? quadrilateral)
    {
        if (quadrilateral == null)
        {
            throw new BarcodeValidationException(
                "An image quadrilateral is required for coordinate restoration.");
        }

        GeometryMath.ValidateQuadrilateral(quadrilateral);
        return GeometryMath.CreateQuadrilateral(
            RestorePoint(quadrilateral.TopLeft),
            RestorePoint(quadrilateral.TopRight),
            RestorePoint(quadrilateral.BottomRight),
            RestorePoint(quadrilateral.BottomLeft));
    }
}
