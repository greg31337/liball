namespace webp;

internal class WebpVp8LTransform
{
    internal int Type;
    internal int WidthBefore;
    internal int HeightBefore;
    internal int WidthAfter;
    internal int HeightAfter;
    internal WebpVp8LPredictorTransform PredictorTransform;
    internal WebpVp8LColorTransform ColorTransform;
    internal WebpVp8LColorIndexingTransform ColorIndexingTransform;

    internal WebpVp8LTransform()
    {
        Type = 0;
        WidthBefore = 0;
        HeightBefore = 0;
        WidthAfter = 0;
        HeightAfter = 0;
        PredictorTransform = new WebpVp8LPredictorTransform();
        ColorTransform = new WebpVp8LColorTransform();
        ColorIndexingTransform = new WebpVp8LColorIndexingTransform();
    }
}
