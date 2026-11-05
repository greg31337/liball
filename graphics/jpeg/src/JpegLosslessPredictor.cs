namespace jpeg;

internal class JpegLosslessPredictor
{
    internal int Predict(
        JpegPlanarIntComponentBuffer component,
        int x,
        int y,
        int predictorSelection,
        int initialPrediction,
        int pointTransform,
        bool useInitialPrediction)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        ValidateCoordinates(component, x, y);
        ValidatePredictorSelection(predictorSelection);
        ValidatePointTransform(component.Precision, pointTransform);
        if (useInitialPrediction || (x == 0 && y == 0))
        {
            return initialPrediction;
        }

        if (y == 0)
        {
            return ReadLeft(component, x, y, pointTransform);
        }

        if (x == 0)
        {
            return ReadAbove(component, x, y, pointTransform);
        }

        int left = ReadLeft(component, x, y, pointTransform);
        int above = ReadAbove(component, x, y, pointTransform);
        int upperLeft = ReadUpperLeft(component, x, y, pointTransform);
        if (predictorSelection == 1)
        {
            return left;
        }

        if (predictorSelection == 2)
        {
            return above;
        }

        if (predictorSelection == 3)
        {
            return upperLeft;
        }

        if (predictorSelection == 4)
        {
            return left + above - upperLeft;
        }

        if (predictorSelection == 5)
        {
            return left + ((above - upperLeft) / 2);
        }

        if (predictorSelection == 6)
        {
            return above + ((left - upperLeft) / 2);
        }

        return (left + above) / 2;
    }

    internal int GetInitialPrediction(int precision, int pointTransform)
    {
        ValidatePointTransform(precision, pointTransform);
        return 1 << (precision - pointTransform - 1);
    }

    internal int ReadLeft(JpegPlanarIntComponentBuffer component, int x, int y, int pointTransform)
    {
        return ReadSampleForPrediction(component, x - 1, y, pointTransform);
    }

    internal int ReadAbove(JpegPlanarIntComponentBuffer component, int x, int y, int pointTransform)
    {
        return ReadSampleForPrediction(component, x, y - 1, pointTransform);
    }

    internal int ReadUpperLeft(JpegPlanarIntComponentBuffer component, int x, int y, int pointTransform)
    {
        return ReadSampleForPrediction(component, x - 1, y - 1, pointTransform);
    }

    internal int ReadSampleForPrediction(JpegPlanarIntComponentBuffer component, int x, int y, int pointTransform)
    {
        ValidateCoordinates(component, x, y);
        return component.Samples[y * component.Stride + x] >> pointTransform;
    }

    internal void ValidateCoordinates(JpegPlanarIntComponentBuffer component, int x, int y)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        if (x < 0 || x >= component.Width)
        {
            throw new System.ArgumentOutOfRangeException("x");
        }

        if (y < 0 || y >= component.Height)
        {
            throw new System.ArgumentOutOfRangeException("y");
        }

        long minimumLength = (long)component.Stride * (long)component.PaddedHeight;
        if (component.Samples == null || minimumLength < 0 || component.Samples.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("Lossless component sample buffer is too small.");
        }
    }

    internal void ValidatePredictorSelection(int predictorSelection)
    {
        if (predictorSelection < 1 || predictorSelection > 7)
        {
            throw JpegInvalidDataException.Create("Lossless predictor selection must be between 1 and 7.");
        }
    }

    internal void ValidatePointTransform(int precision, int pointTransform)
    {
        if (precision < 2 || precision > 16)
        {
            throw JpegInvalidDataException.Create("Lossless sample precision must be between 2 and 16 bits.");
        }

        if (pointTransform < 0 || pointTransform >= precision)
        {
            throw JpegInvalidDataException.Create("Lossless point transform must be less than the sample precision.");
        }
    }
}
