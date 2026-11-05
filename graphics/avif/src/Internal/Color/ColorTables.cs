using System;

namespace avif;

internal class ColorPrimariesDefinition
{
    internal double RedX;
    internal double RedY;
    internal double GreenX;
    internal double GreenY;
    internal double BlueX;
    internal double BlueY;
    internal double WhiteX;
    internal double WhiteY;

    internal ColorPrimariesDefinition(
        double redX,
        double redY,
        double greenX,
        double greenY,
        double blueX,
        double blueY,
        double whiteX,
        double whiteY)
    {
        RedX = redX;
        RedY = redY;
        GreenX = greenX;
        GreenY = greenY;
        BlueX = blueX;
        BlueY = blueY;
        WhiteX = whiteX;
        WhiteY = whiteY;
    }
}

internal static class ColorTables
{
    internal static bool IsSupported(AvifColorPrimaries primaries)
    {
        return primaries == AvifColorPrimaries.Bt709
            || primaries == AvifColorPrimaries.Bt470M
            || primaries == AvifColorPrimaries.Bt470Bg
            || primaries == AvifColorPrimaries.Bt601
            || primaries == AvifColorPrimaries.Smpte240
            || primaries == AvifColorPrimaries.GenericFilm
            || primaries == AvifColorPrimaries.Bt2020
            || primaries == AvifColorPrimaries.Smpte428
            || primaries == AvifColorPrimaries.Smpte431
            || primaries == AvifColorPrimaries.Smpte432
            || primaries == AvifColorPrimaries.EbuTech3213;
    }

    internal static ColorMatrix? GetRgbToXyz(AvifColorPrimaries primaries)
    {
        if (primaries == AvifColorPrimaries.Smpte428)
        {
            return ColorMatrix.Identity();
        }
        ColorPrimariesDefinition? definition = GetDefinition(primaries);
        if (definition == null)
        {
            return null;
        }

        double redX = definition.RedX / definition.RedY;
        double redZ = (1 - definition.RedX - definition.RedY)
            / definition.RedY;
        double greenX = definition.GreenX / definition.GreenY;
        double greenZ = (1 - definition.GreenX - definition.GreenY)
            / definition.GreenY;
        double blueX = definition.BlueX / definition.BlueY;
        double blueZ = (1 - definition.BlueX - definition.BlueY)
            / definition.BlueY;
        ColorMatrix unscaled = new ColorMatrix(new double[]
        {
            redX, greenX, blueX,
            1, 1, 1,
            redZ, greenZ, blueZ
        });
        ColorMatrix? inverse = unscaled.Invert();
        if (inverse == null)
        {
            return null;
        }
        ColorVector? white = GetWhitePoint(primaries);
        ColorVector scale = inverse.Transform(white);
        return new ColorMatrix(new double[]
        {
            scale.First * redX,
            scale.Second * greenX,
            scale.Third * blueX,
            scale.First,
            scale.Second,
            scale.Third,
            scale.First * redZ,
            scale.Second * greenZ,
            scale.Third * blueZ
        });
    }

    internal static ColorMatrix? GetPrimariesTransform(
        AvifColorPrimaries source,
        AvifColorPrimaries destination)
    {
        ColorMatrix? sourceToXyz = GetRgbToXyz(source);
        ColorMatrix? destinationToXyz = GetRgbToXyz(destination);
        if (sourceToXyz == null || destinationToXyz == null)
        {
            return null;
        }
        ColorMatrix? xyzToDestination = destinationToXyz.Invert();
        if (xyzToDestination == null)
        {
            return null;
        }
        ColorMatrix adaptation = GetChromaticAdaptation(
            GetWhitePoint(source), GetWhitePoint(destination));
        return ColorMatrix.Multiply(
            xyzToDestination,
            ColorMatrix.Multiply(adaptation, sourceToXyz));
    }

    internal static ColorVector? ConvertPrimaries(
        ColorVector? linearRgb,
        AvifColorPrimaries source,
        AvifColorPrimaries destination)
    {
        ColorMatrix? transform = GetPrimariesTransform(source, destination);
        return transform == null ? null : transform.Transform(linearRgb);
    }

    internal static ColorVector? ConvertXyzToPrimaries(
        ColorVector xyz,
        ColorVector sourceWhite,
        AvifColorPrimaries destination)
    {
        ColorMatrix? destinationToXyz = GetRgbToXyz(destination);
        if (xyz == null || sourceWhite == null || destinationToXyz == null)
        {
            return null;
        }
        ColorMatrix? inverse = destinationToXyz.Invert();
        if (inverse == null)
        {
            return null;
        }
        ColorMatrix adaptation = GetChromaticAdaptation(
            sourceWhite, GetWhitePoint(destination));
        return inverse.Transform(adaptation.Transform(xyz));
    }

    internal static ColorVector? ConvertPrimariesToXyz(
        ColorVector linearRgb,
        AvifColorPrimaries source,
        ColorVector destinationWhite)
    {
        ColorMatrix? sourceToXyz = GetRgbToXyz(source);
        if (linearRgb == null || destinationWhite == null
            || sourceToXyz == null)
        {
            return null;
        }
        ColorVector xyz = sourceToXyz.Transform(linearRgb);
        ColorMatrix adaptation = GetChromaticAdaptation(
            GetWhitePoint(source), destinationWhite);
        return adaptation.Transform(xyz);
    }

    internal static ColorVector? GetWhitePoint(AvifColorPrimaries primaries)
    {
        if (primaries == AvifColorPrimaries.Smpte428)
        {
            return new ColorVector(1, 1, 1);
        }
        ColorPrimariesDefinition? definition = GetDefinition(primaries);
        if (definition == null)
        {
            return null;
        }
        return new ColorVector(
            definition.WhiteX / definition.WhiteY,
            1,
            (1 - definition.WhiteX - definition.WhiteY)
                / definition.WhiteY);
    }

    static ColorMatrix GetChromaticAdaptation(
        ColorVector? sourceWhite,
        ColorVector? destinationWhite)
    {
        if (System.Math.Abs((sourceWhite ?? throw new InvalidOperationException("Invalid internal state.")).First - (destinationWhite ?? throw new InvalidOperationException("Invalid internal state.")).First) < 1e-12
            && System.Math.Abs(sourceWhite.Second - destinationWhite.Second) < 1e-12
            && System.Math.Abs(sourceWhite.Third - destinationWhite.Third) < 1e-12)
        {
            return ColorMatrix.Identity();
        }
        ColorMatrix bradford = new ColorMatrix(new double[]
        {
            0.8951, 0.2664, -0.1614,
            -0.7502, 1.7135, 0.0367,
            0.0389, -0.0685, 1.0296
        });
        ColorMatrix? inverse = bradford.Invert();
        ColorVector sourceCone = bradford.Transform(sourceWhite);
        ColorVector destinationCone = bradford.Transform(destinationWhite);
        ColorMatrix scale = ColorMatrix.Diagonal(
            destinationCone.First / sourceCone.First,
            destinationCone.Second / sourceCone.Second,
            destinationCone.Third / sourceCone.Third);
        return ColorMatrix.Multiply(
            inverse, ColorMatrix.Multiply(scale, bradford));
    }

    static ColorPrimariesDefinition? GetDefinition(
        AvifColorPrimaries primaries)
    {
        switch (primaries)
        {
            case AvifColorPrimaries.Bt709:
                return new ColorPrimariesDefinition(
                    0.64, 0.33, 0.30, 0.60, 0.15, 0.06,
                    0.3127, 0.3290);
            case AvifColorPrimaries.Bt470M:
                return new ColorPrimariesDefinition(
                    0.67, 0.33, 0.21, 0.71, 0.14, 0.08,
                    0.3100, 0.3160);
            case AvifColorPrimaries.Bt470Bg:
                return new ColorPrimariesDefinition(
                    0.64, 0.33, 0.29, 0.60, 0.15, 0.06,
                    0.3127, 0.3290);
            case AvifColorPrimaries.Bt601:
            case AvifColorPrimaries.Smpte240:
                return new ColorPrimariesDefinition(
                    0.630, 0.340, 0.310, 0.595, 0.155, 0.070,
                    0.3127, 0.3290);
            case AvifColorPrimaries.GenericFilm:
                return new ColorPrimariesDefinition(
                    0.681, 0.319, 0.243, 0.692, 0.145, 0.049,
                    0.3100, 0.3160);
            case AvifColorPrimaries.Bt2020:
                return new ColorPrimariesDefinition(
                    0.708, 0.292, 0.170, 0.797, 0.131, 0.046,
                    0.3127, 0.3290);
            case AvifColorPrimaries.Smpte431:
                return new ColorPrimariesDefinition(
                    0.680, 0.320, 0.265, 0.690, 0.150, 0.060,
                    0.3140, 0.3510);
            case AvifColorPrimaries.Smpte432:
                return new ColorPrimariesDefinition(
                    0.680, 0.320, 0.265, 0.690, 0.150, 0.060,
                    0.3127, 0.3290);
            case AvifColorPrimaries.EbuTech3213:
                return new ColorPrimariesDefinition(
                    0.630, 0.340, 0.295, 0.605, 0.155, 0.077,
                    0.3127, 0.3290);
            default:
                return null;
        }
    }
}
