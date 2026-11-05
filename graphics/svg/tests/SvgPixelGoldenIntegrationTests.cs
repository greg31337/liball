using System.Security.Cryptography;

namespace svg.Tests;

[Category("Integration")]
[Category("PixelGolden")]
public class SvgPixelGoldenIntegrationTests
{
    [TestCaseSource(nameof(StaticCases))]
    public void RepresentativeStaticSceneMatchesDeterministicPixelGolden(SvgPixelGoldenCase testCase)
    {
        AssertGolden(testCase);
    }

    [TestCaseSource(nameof(AnimationCases))]
    public void FixedAnimationSampleMatchesDeterministicPixelGolden(SvgPixelGoldenCase testCase)
    {
        AssertGolden(testCase);
    }

    static IEnumerable<SvgPixelGoldenCase> StaticCases()
    {
        yield return new SvgPixelGoldenCase(
            "icon-gradient-clip",
            "icon, gradient, clip",
            48,
            48,
            0,
            "<svg xmlns='http://www.w3.org/2000/svg' width='48' height='48' viewBox='0 0 48 48'>" +
            "<defs><linearGradient id='face' gradientUnits='userSpaceOnUse' x1='3' y1='3' x2='45' y2='45'><stop offset='0' stop-color='#66d9ff'/><stop offset='1' stop-color='#3155d9'/></linearGradient>" +
            "<clipPath id='round'><circle cx='24' cy='24' r='21'/></clipPath></defs>" +
            "<rect width='48' height='48' fill='#f4f7fb'/><g clip-path='url(#round)'><rect x='3' y='3' width='42' height='42' fill='url(#face)'/>" +
            "<path d='M13 25L21 33L36 16' fill='none' stroke='white' stroke-width='5' stroke-linecap='round' stroke-linejoin='round'/></g></svg>",
            "c4d26f4d9174e283b744dfb33c66f6738399566d08c17acd9732603e472d5a50");
        yield return new SvgPixelGoldenCase(
            "diagram-markers-filter",
            "diagram, markers, filter",
            96,
            64,
            0,
            "<svg xmlns='http://www.w3.org/2000/svg' width='96' height='64' viewBox='0 0 96 64'>" +
            "<defs><marker id='arrow' markerUnits='userSpaceOnUse' markerWidth='8' markerHeight='8' refX='7' refY='4' orient='auto' viewBox='0 0 8 8'><path d='M0 0L8 4L0 8Z' fill='#394b63'/></marker>" +
            "<filter id='soft' filterUnits='userSpaceOnUse' x='0' y='0' width='96' height='64'><feGaussianBlur stdDeviation='.6'/></filter></defs>" +
            "<rect width='96' height='64' fill='white'/><g fill='none' stroke='#394b63' stroke-width='2px' marker-end='url(#arrow)'>" +
            "<path d='M27 19H66'/><path d='M69 25V43H30'/></g><g filter='url(#soft)' fill='#dcecff' stroke='#315b87' stroke-width='2px'>" +
            "<rect x='5' y='8' width='23' height='22' rx='4'/><rect x='67' y='8' width='24' height='22' rx='4'/><rect x='5' y='39' width='25' height='20' rx='4'/></g></svg>",
            "d04dd28958bb8207c731677bf86ea36440aec74ad079c085d5c584fa23ab2cab");
        yield return new SvgPixelGoldenCase(
            "map-pattern-mask-nested-resources",
            "map, pattern, clip, mask, nested resources",
            96,
            64,
            0,
            "<svg xmlns='http://www.w3.org/2000/svg' width='96' height='64' viewBox='0 0 96 64'>" +
            "<defs><linearGradient id='terrain' gradientUnits='userSpaceOnUse' x1='0' y1='0' x2='0' y2='8'><stop offset='0' stop-color='#a8d46f'/><stop offset='1' stop-color='#4f8f58'/></linearGradient>" +
            "<pattern id='contours' patternUnits='userSpaceOnUse' width='10' height='8'><rect width='10' height='8' fill='url(#terrain)'/><path d='M0 6Q5 2 10 6' fill='none' stroke='#376d45' stroke-width='1'/></pattern>" +
            "<path id='island' d='M10 34L20 14L43 8L73 16L88 33L75 53L48 59L19 51Z'/>" +
            "<clipPath id='coast'><path d='M10 34L20 14L43 8L73 16L88 33L75 53L48 59L19 51Z'/></clipPath>" +
            "<mask id='coast-fade' maskUnits='userSpaceOnUse' maskContentUnits='userSpaceOnUse' x='0' y='0' width='96' height='64'><rect width='96' height='64' fill='white'/><circle cx='70' cy='25' r='9' fill='#888'/></mask></defs>" +
            "<rect width='96' height='64' fill='#b9e6f2'/><g clip-path='url(#coast)' mask='url(#coast-fade)'><rect width='96' height='64' fill='url(#contours)'/>" +
            "<path d='M13 38C32 27 58 29 84 22M20 49C40 39 65 43 80 36' fill='none' stroke='#f5e7a1' stroke-width='2'/></g></svg>",
            "40f2ed74a79c31f5fe473d369361de654b197bd0377ab9f470333a5702ba6140");
        yield return new SvgPixelGoldenCase(
            "multiline-text",
            "text",
            96,
            48,
            0,
            "<svg xmlns='http://www.w3.org/2000/svg' width='96' height='48' viewBox='0 0 96 48'><rect width='96' height='48' fill='#172033'/>" +
            "<text x='8' y='20' font-family='sans-serif' font-size='16' font-weight='700' fill='#f4c95d'>SVG</text>" +
            "<text x='8' y='39' font-family='sans-serif' font-size='13' letter-spacing='1' fill='white'>PIXELS</text></svg>",
            "163f1d28b44932bd3e7188c8e7c484b31dd20cd41015378855b1e2a691e2fac6");
    }

    static IEnumerable<SvgPixelGoldenCase> AnimationCases()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='32' viewBox='0 0 64 32'><rect width='64' height='32' fill='#f6f7f9'/>" +
            "<rect id='animated' x='12' y='6' width='40' height='20' rx='5' fill='#3157d5' opacity='.2'><animate attributeName='opacity' values='.2;1' dur='4s' fill='freeze'/></rect></svg>";
        yield return new SvgPixelGoldenCase("animation-0s", "animation sample time", 64, 32, 0, source, "c6a17049a72357fdd5098f8972dfe8d537b7f3d4fd27050d1ee62692db896798");
        yield return new SvgPixelGoldenCase("animation-1s", "animation sample time", 64, 32, 1, source, "d141377a0ac6b1040857e8008ceee5782b823a32e8b87ba63d8abd2efa687a4f");
        yield return new SvgPixelGoldenCase("animation-2s", "animation sample time", 64, 32, 2, source, "8671599d03527977eaf7e37ff9b722439d91f83b7085ed412a4db9519fa8231b");
        yield return new SvgPixelGoldenCase("animation-4s", "animation sample time", 64, 32, 4, source, "33120d7b2f9e00e4900931cd7f2780fbdeacdf34c421357783abcf6974bd526a");
    }

    static void AssertGolden(SvgPixelGoldenCase testCase)
    {
        SvgDocument document = Load(testCase.Source);
        if (testCase.Name.StartsWith("animation-", StringComparison.Ordinal)) ApplyAnimationSample(document, testCase.AnimationTime);
        SvgRenderResult first = Render(document, testCase);
        SvgRenderResult second = Render(document, testCase);
        Assert.That(first.Success, Is.True, Message(first, testCase));
        Assert.That(second.Success, Is.True, Message(second, testCase));
        AssertResources(first.Scene, testCase);
        string firstHash = PixelHash(first.Pixels);
        string secondHash = PixelHash(second.Pixels);

        Assert.Multiple(() =>
        {
            Assert.That(first.Pixels.Width, Is.EqualTo(testCase.Width));
            Assert.That(first.Pixels.Height, Is.EqualTo(testCase.Height));
            Assert.That(DistinctPixelCount(first.Pixels, 3), Is.EqualTo(3), "The representative scene collapsed to fewer than three pixel colors for " + testCase.Name + ".");
            Assert.That(first.AnimationTime, Is.EqualTo(testCase.AnimationTime));
            Assert.That(secondHash, Is.EqualTo(firstHash), "Repeated pixels changed for " + testCase.Name + ".");
            Assert.That(second.PngBytes, Is.EqualTo(first.PngBytes), "Repeated PNG bytes changed for " + testCase.Name + ".");
            Assert.That(firstHash, Is.EqualTo(testCase.ExpectedHash), "Update only after visually reviewing the intentional golden change for " + testCase.Name + ". Actual hash: " + firstHash);
        });
    }

    static void AssertResources(SvgScene scene, SvgPixelGoldenCase testCase)
    {
        if (testCase.Features.Contains("gradient", StringComparison.Ordinal)) Assert.That(scene.Resources.Any(resource => resource.Kind == SvgSceneResourceKind.GradientPaint), Is.True, "The gradient resource was not retained for " + testCase.Name + ".");
        if (testCase.Features.Contains("pattern", StringComparison.Ordinal)) Assert.That(scene.Resources.Any(resource => resource.Kind == SvgSceneResourceKind.PatternPaint), Is.True, "The pattern resource was not retained for " + testCase.Name + ".");
        if (testCase.Features.Contains("markers", StringComparison.Ordinal)) Assert.That(scene.Resources.Any(resource => resource.Kind == SvgSceneResourceKind.Marker), Is.True, "The marker resource was not retained for " + testCase.Name + ".");
        if (testCase.Features.Contains("clip", StringComparison.Ordinal)) Assert.That(scene.Resources.Any(resource => resource.Kind == SvgSceneResourceKind.Clip), Is.True, "The clip resource was not retained for " + testCase.Name + ".");
        if (testCase.Features.Contains("mask", StringComparison.Ordinal)) Assert.That(scene.Resources.Any(resource => resource.Kind == SvgSceneResourceKind.Mask), Is.True, "The mask resource was not retained for " + testCase.Name + ".");
        if (testCase.Features.Contains("filter", StringComparison.Ordinal)) Assert.That(scene.Resources.Any(resource => resource.Kind == SvgSceneResourceKind.Filter), Is.True, "The filter resource was not retained for " + testCase.Name + ".");
        if (testCase.Features.Contains("nested resources", StringComparison.Ordinal))
        {
            SvgSceneResource pattern = scene.Resources.Single(resource => resource.Kind == SvgSceneResourceKind.PatternPaint);
            Assert.That(pattern.PatternPaint.Content.Children.Any(node => node.Fill != null && node.Fill.Kind == SvgSceneResourceKind.GradientPaint), Is.True, "The nested pattern-to-gradient dependency was not retained for " + testCase.Name + ".");
        }
    }

    static void ApplyAnimationSample(SvgDocument document, double time)
    {
        SvgNode target = document.Index.GetById("animated");
        SvgAnimationState state = new SvgAnimationState(document);
        state.SetBaseValue(new SvgAnimationBaseValue(target, SvgAnimationTargetKind.CssProperty, "opacity", SvgAnimationValue.FromNumber(0.2)));
        SvgAnimationSample sample = state.Sample(time);
        Assert.That(sample.Success, Is.True, sample.Error + ": " + string.Join(" | ", sample.Diagnostics.Select(item => item.Message)));
        SvgAnimationSampleValue value = sample.Find(target, SvgAnimationTargetKind.CssProperty, "opacity");
        Assert.That(value, Is.Not.Null, "The opacity animation did not produce a sample.");
        SvgAttribute current = target.Attributes.Single(attribute => attribute.LocalName == "opacity" && attribute.NamespaceUri.Length == 0);
        SvgAttribute replacement = new SvgDocumentApi().CreateAttribute("opacity", "", "opacity", "", value.AnimatedValue.ToText());
        SvgMutationResult mutation = new SvgNodeApi().ReplaceAttribute(target, current, replacement);
        Assert.That(mutation.Success, Is.True, mutation.Message);
    }

    static SvgRenderResult Render(SvgDocument document, SvgPixelGoldenCase testCase)
    {
        SvgRenderOptions options = new SvgRenderOptions();
        options.ViewportWidth = testCase.Width;
        options.ViewportHeight = testCase.Height;
        options.AnimationTime = testCase.AnimationTime;
        options.SamplesPerAxis = 2;
        options.EncodePng = true;
        return new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, options);
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True, "Golden SVG did not load.");
        return loaded.Document;
    }

    static string PixelHash(SvgPixelBuffer pixels)
    {
        return Convert.ToHexString(SHA256.HashData(pixels.Data)).ToLowerInvariant();
    }

    static int DistinctPixelCount(SvgPixelBuffer pixels, int limit)
    {
        HashSet<int> colors = new HashSet<int>();
        int y = 0;
        while (y < pixels.Height && colors.Count < limit)
        {
            int x = 0;
            while (x < pixels.Width && colors.Count < limit)
            {
                SvgPremultipliedRgba8 pixel = pixels.Read(x, y).Pixel;
                colors.Add((pixel.Red << 24) | (pixel.Green << 16) | (pixel.Blue << 8) | pixel.Alpha);
                x++;
            }
            y++;
        }
        return colors.Count;
    }

    static string Message(SvgRenderResult result, SvgPixelGoldenCase testCase)
    {
        return "Rendering " + testCase.Name + " (" + testCase.Features + ") failed with " + result.Error + ": " + string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}

public class SvgPixelGoldenCase
{
    public string Name;
    public string Features;
    public int Width;
    public int Height;
    public double AnimationTime;
    public string Source;
    public string ExpectedHash;

    public SvgPixelGoldenCase(string name, string features, int width, int height, double animationTime, string source, string expectedHash)
    {
        Name = name;
        Features = features;
        Width = width;
        Height = height;
        AnimationTime = animationTime;
        Source = source;
        ExpectedHash = expectedHash;
    }

    public override string ToString()
    {
        return Name;
    }
}
