using webp;

namespace webp.Tests;

[TestFixture]
public class WebpAlphaFilterTests
{
    [Test]
    public void Filter_ProducesPredictorResiduals()
    {
        byte[] alpha = new byte[] { 10, 15, 20, 30, 40, 55 };

        byte[] filtered = WebpAlphaFilter.Filter(alpha, 3, 2, WebpConstants.AlphaFilterHorizontal);

        Assert.That(filtered, Is.EqualTo(new byte[] { 10, 5, 5, 20, 10, 15 }));
    }

    [Test]
    public void Filter_RoundTripsAllPredictorModesWithoutMutatingInput()
    {
        byte[] alpha = new byte[] { 0, 10, 250, 20, 35, 90, 255, 100, 40 };
        byte[] original = new byte[] { 0, 10, 250, 20, 35, 90, 255, 100, 40 };
        byte[] filters = new byte[]
        {
            WebpConstants.AlphaFilterNone,
            WebpConstants.AlphaFilterHorizontal,
            WebpConstants.AlphaFilterVertical,
            WebpConstants.AlphaFilterGradient
        };

        int index = 0;
        while (index < filters.Length)
        {
            byte[] filtered = WebpAlphaFilter.Filter(alpha, 3, 3, filters[index]);
            byte[] unfiltered = WebpAlphaFilter.Unfilter(filtered, 3, 3, filters[index]);

            Assert.That(unfiltered, Is.EqualTo(original));
            index = index + 1;
        }

        Assert.That(alpha, Is.EqualTo(original));
    }

    [Test]
    public void Unfilter_NoneReturnsResidualValues()
    {
        byte[] filtered = new byte[] { 1, 2, 3, 4 };

        byte[] alpha = WebpAlphaFilter.Unfilter(filtered, 2, 2, WebpConstants.AlphaFilterNone);

        Assert.That(alpha, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Unfilter_HorizontalPredictsFromLeftWithLeftColumnAbove()
    {
        byte[] filtered = new byte[] { 10, 5, 5, 20, 10, 15 };

        byte[] alpha = WebpAlphaFilter.Unfilter(filtered, 3, 2, WebpConstants.AlphaFilterHorizontal);

        Assert.That(alpha, Is.EqualTo(new byte[] { 10, 15, 20, 30, 40, 55 }));
    }

    [Test]
    public void Unfilter_VerticalPredictsFromAboveWithTopRowLeft()
    {
        byte[] filtered = new byte[] { 10, 10, 10, 5, 10, 25 };

        byte[] alpha = WebpAlphaFilter.Unfilter(filtered, 3, 2, WebpConstants.AlphaFilterVertical);

        Assert.That(alpha, Is.EqualTo(new byte[] { 10, 20, 30, 15, 30, 55 }));
    }

    [Test]
    public void Unfilter_GradientPredictsFromClippedLeftPlusAboveMinusUpperLeft()
    {
        byte[] filtered = new byte[] { 10, 10, 15, 20, 10, 25, 50, 10, 10 };

        byte[] alpha = WebpAlphaFilter.Unfilter(filtered, 3, 3, WebpConstants.AlphaFilterGradient);

        Assert.That(alpha, Is.EqualTo(new byte[] { 10, 20, 35, 30, 50, 90, 80, 110, 160 }));
    }

    [Test]
    public void Unfilter_GradientClipsPredictorAndWrapsModulo256()
    {
        byte[] filtered = new byte[] { 0, 250, 250, 10 };

        byte[] alpha = WebpAlphaFilter.Unfilter(filtered, 2, 2, WebpConstants.AlphaFilterGradient);

        Assert.That(alpha, Is.EqualTo(new byte[] { 0, 250, 250, 9 }));
    }

    [Test]
    public void Unfilter_RejectsInvalidShapeAndFilter()
    {
        Assert.Throws<ArgumentException>(() => WebpAlphaFilter.Unfilter(new byte[] { 1, 2, 3 }, 2, 2, WebpConstants.AlphaFilterNone));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpAlphaFilter.Unfilter(new byte[] { 1 }, 0, 1, WebpConstants.AlphaFilterNone));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpAlphaFilter.Unfilter(new byte[] { 1 }, 1, 1, 4));
    }
}
