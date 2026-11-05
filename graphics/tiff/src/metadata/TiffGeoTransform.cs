using System;

namespace tiff;

internal class TiffGeoTransform
{
    internal const double Epsilon = 0.000000000001;

    internal static TiffGeoTransformResult ImageToModel(TiffGeoModelMetadata metadata, double rasterI, double rasterJ, double rasterK)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (metadata == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "GeoTIFF model metadata is required for image-to-model transforms.");
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        if (metadata.HasTransformation)
        {
            return ImageToModelWithTransformation(metadata.Transformation, rasterI, rasterJ, rasterK);
        }

        if (metadata.HasPixelScale && metadata.HasTiepoints && metadata.Tiepoints.Length > 0)
        {
            return ImageToModelWithPixelScale(metadata.PixelScale, metadata.Tiepoints[0], rasterI, rasterJ, rasterK);
        }

        diagnostics.AddError(TiffErrorCode.InvalidTagValue, "GeoTIFF metadata does not contain a usable model transform.");
        return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
    }

    internal static TiffGeoTransformResult ModelToImage(TiffGeoModelMetadata metadata, double modelX, double modelY, double modelZ)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (metadata == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "GeoTIFF model metadata is required for model-to-image transforms.");
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        if (metadata.HasTransformation)
        {
            return ModelToImageWithTransformation(metadata.Transformation, modelX, modelY, modelZ);
        }

        if (metadata.HasPixelScale && metadata.HasTiepoints && metadata.Tiepoints.Length > 0)
        {
            return ModelToImageWithPixelScale(metadata.PixelScale, metadata.Tiepoints[0], modelX, modelY, modelZ);
        }

        diagnostics.AddError(TiffErrorCode.InvalidTagValue, "GeoTIFF metadata does not contain a usable model transform.");
        return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
    }

    internal static TiffGeoTransformResult ImagePixelToModel(TiffGeoModelMetadata metadata, TiffGeoRasterType rasterType, double pixelColumn, double pixelRow, double rasterK)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateRasterType(rasterType, diagnostics))
        {
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        double offset = rasterType == TiffGeoRasterType.PixelIsArea ? 0.5 : 0;
        return ImageToModel(metadata, pixelColumn + offset, pixelRow + offset, rasterK);
    }

    internal static TiffGeoTransformResult ModelToImagePixel(TiffGeoModelMetadata metadata, TiffGeoRasterType rasterType, double modelX, double modelY, double modelZ)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateRasterType(rasterType, diagnostics))
        {
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        TiffGeoTransformResult raster = ModelToImage(metadata, modelX, modelY, modelZ);
        if (!raster.Success)
        {
            return raster;
        }

        double offset = rasterType == TiffGeoRasterType.PixelIsArea ? 0.5 : 0;
        TiffGeoCoordinate coordinate = TiffGeoCoordinate.Create(raster.Coordinate.X - offset, raster.Coordinate.Y - offset, raster.Coordinate.Z);
        return TiffGeoTransformResult.Create(coordinate, raster.Diagnostics);
    }

    internal static bool ValidateRasterType(TiffGeoRasterType rasterType, TiffDiagnostics diagnostics)
    {
        if (rasterType != TiffGeoRasterType.PixelIsArea && rasterType != TiffGeoRasterType.PixelIsPoint)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "A resolved RasterPixelIsArea or RasterPixelIsPoint value is required for pixel-aware transforms.", TiffDiagnostic.NoImageIndex, TiffTagId.GeoKeyDirectory);
            return false;
        }

        return true;
    }

    internal static TiffGeoTransformResult ImageToModelWithPixelScale(double[] pixelScale, TiffGeoTiepoint tiepoint, double rasterI, double rasterJ, double rasterK)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidatePixelScale(pixelScale, diagnostics))
        {
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        TiffGeoTiepoint actualTiepoint = NormalizeTiepoint(tiepoint);
        double x = actualTiepoint.ModelX + ((rasterI - actualTiepoint.RasterI) * pixelScale[0]);
        double y = actualTiepoint.ModelY - ((rasterJ - actualTiepoint.RasterJ) * pixelScale[1]);
        double z = actualTiepoint.ModelZ + ((rasterK - actualTiepoint.RasterK) * pixelScale[2]);
        return TiffGeoTransformResult.Create(TiffGeoCoordinate.Create(x, y, z), diagnostics);
    }

    internal static TiffGeoTransformResult ModelToImageWithPixelScale(double[] pixelScale, TiffGeoTiepoint tiepoint, double modelX, double modelY, double modelZ)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidatePixelScale(pixelScale, diagnostics))
        {
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        TiffGeoTiepoint actualTiepoint = NormalizeTiepoint(tiepoint);
        double rasterI = actualTiepoint.RasterI + ((modelX - actualTiepoint.ModelX) / pixelScale[0]);
        double rasterJ = actualTiepoint.RasterJ - ((modelY - actualTiepoint.ModelY) / pixelScale[1]);
        double rasterK = actualTiepoint.RasterK;
        if (Math.Abs(pixelScale[2]) > Epsilon)
        {
            rasterK = actualTiepoint.RasterK + ((modelZ - actualTiepoint.ModelZ) / pixelScale[2]);
        }
        else if (Math.Abs(modelZ - actualTiepoint.ModelZ) > Epsilon)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ModelPixelScaleTag has zero Z scale and cannot invert a different model Z value.", TiffDiagnostic.NoImageIndex, TiffTagId.ModelPixelScale);
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        return TiffGeoTransformResult.Create(TiffGeoCoordinate.Create(rasterI, rasterJ, rasterK), diagnostics);
    }

    internal static TiffGeoTransformResult ImageToModelWithTransformation(double[] transformation, double rasterI, double rasterJ, double rasterK)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateTransformation(transformation, diagnostics))
        {
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        double w = (transformation[12] * rasterI) + (transformation[13] * rasterJ) + (transformation[14] * rasterK) + transformation[15];
        if (Math.Abs(w) <= Epsilon)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ModelTransformationTag produced a zero homogeneous coordinate.", TiffDiagnostic.NoImageIndex, TiffTagId.ModelTransformation);
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        double x = ((transformation[0] * rasterI) + (transformation[1] * rasterJ) + (transformation[2] * rasterK) + transformation[3]) / w;
        double y = ((transformation[4] * rasterI) + (transformation[5] * rasterJ) + (transformation[6] * rasterK) + transformation[7]) / w;
        double z = ((transformation[8] * rasterI) + (transformation[9] * rasterJ) + (transformation[10] * rasterK) + transformation[11]) / w;
        return TiffGeoTransformResult.Create(TiffGeoCoordinate.Create(x, y, z), diagnostics);
    }

    internal static TiffGeoTransformResult ModelToImageWithTransformation(double[] transformation, double modelX, double modelY, double modelZ)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateTransformation(transformation, diagnostics))
        {
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        if (!IsAffineTransformation(transformation))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ModelTransformationTag inverse helper supports affine transformations only.", TiffDiagnostic.NoImageIndex, TiffTagId.ModelTransformation);
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        double a00 = transformation[0];
        double a01 = transformation[1];
        double a02 = transformation[2];
        double a10 = transformation[4];
        double a11 = transformation[5];
        double a12 = transformation[6];
        double a20 = transformation[8];
        double a21 = transformation[9];
        double a22 = transformation[10];
        double dx = modelX - transformation[3];
        double dy = modelY - transformation[7];
        double dz = modelZ - transformation[11];
        double determinant = CalculateDeterminant3(a00, a01, a02, a10, a11, a12, a20, a21, a22);
        if (Math.Abs(determinant) <= Epsilon)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ModelTransformationTag matrix is singular and cannot be inverted.", TiffDiagnostic.NoImageIndex, TiffTagId.ModelTransformation);
            return TiffGeoTransformResult.Create(new TiffGeoCoordinate(), diagnostics);
        }

        double rasterI = CalculateDeterminant3(dx, a01, a02, dy, a11, a12, dz, a21, a22) / determinant;
        double rasterJ = CalculateDeterminant3(a00, dx, a02, a10, dy, a12, a20, dz, a22) / determinant;
        double rasterK = CalculateDeterminant3(a00, a01, dx, a10, a11, dy, a20, a21, dz) / determinant;
        return TiffGeoTransformResult.Create(TiffGeoCoordinate.Create(rasterI, rasterJ, rasterK), diagnostics);
    }

    internal static bool ValidatePixelScale(double[] pixelScale, TiffDiagnostics diagnostics)
    {
        if (pixelScale == null || pixelScale.Length != 3)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "ModelPixelScaleTag requires exactly three values.", TiffDiagnostic.NoImageIndex, TiffTagId.ModelPixelScale);
            return false;
        }

        if (Math.Abs(pixelScale[0]) <= Epsilon || Math.Abs(pixelScale[1]) <= Epsilon)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ModelPixelScaleTag X and Y scale values must be non-zero.", TiffDiagnostic.NoImageIndex, TiffTagId.ModelPixelScale);
            return false;
        }

        return true;
    }

    internal static bool ValidateTransformation(double[] transformation, TiffDiagnostics diagnostics)
    {
        if (transformation == null || transformation.Length != 16)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "ModelTransformationTag requires exactly sixteen values.", TiffDiagnostic.NoImageIndex, TiffTagId.ModelTransformation);
            return false;
        }

        return true;
    }

    internal static bool IsAffineTransformation(double[] transformation)
    {
        return Math.Abs(transformation[12]) <= Epsilon && Math.Abs(transformation[13]) <= Epsilon && Math.Abs(transformation[14]) <= Epsilon && Math.Abs(transformation[15] - 1) <= Epsilon;
    }

    internal static double CalculateDeterminant3(double a00, double a01, double a02, double a10, double a11, double a12, double a20, double a21, double a22)
    {
        return (a00 * ((a11 * a22) - (a12 * a21))) - (a01 * ((a10 * a22) - (a12 * a20))) + (a02 * ((a10 * a21) - (a11 * a20)));
    }

    internal static TiffGeoTiepoint NormalizeTiepoint(TiffGeoTiepoint tiepoint)
    {
        if (tiepoint == null)
        {
            return new TiffGeoTiepoint();
        }

        return tiepoint.Copy();
    }
}
