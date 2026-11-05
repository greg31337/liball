using System.Net.Http.Headers;
using System.Xml.Linq;

namespace svg.Tests;

public class SvgConstantsTests
{
    [Test]
    public void NamespaceElementAndAttributeConstantsBuildNamespacedSvg()
    {
        XNamespace svg = SvgNamespaces.Svg;
        XNamespace xlink = SvgNamespaces.XLink;
        XNamespace xml = SvgNamespaces.Xml;
        XElement image = new XElement(
            svg + SvgElementNames.Image,
            new XAttribute(SvgAttributeNames.Href, "current.png"),
            new XAttribute(xlink + SvgAttributeNames.Href, "legacy.png"));
        XElement root = new XElement(
            svg + SvgElementNames.Svg,
            new XAttribute(xml + SvgAttributeNames.Lang, "en"),
            image);

        Assert.Multiple(() =>
        {
            Assert.That(root.Name.NamespaceName, Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(root.Name.LocalName, Is.EqualTo("svg"));
            Assert.That(root.Attribute(xml + SvgAttributeNames.Lang).Value, Is.EqualTo("en"));
            Assert.That(image.Attribute(SvgAttributeNames.Href).Value, Is.EqualTo("current.png"));
            Assert.That(image.Attribute(xlink + SvgAttributeNames.Href).Value, Is.EqualTo("legacy.png"));
        });
    }

    [TestCase(SvgMimeTypes.Svg, "image", "svg+xml")]
    [TestCase(SvgMimeTypes.Png, "image", "png")]
    [TestCase(SvgMimeTypes.Css, "text", "css")]
    [TestCase(SvgMimeTypes.Woff2, "font", "woff2")]
    [TestCase(SvgMimeTypes.Gzip, "application", "gzip")]
    public void MimeTypeConstantsAreValidAndCanonical(string value, string type, string subtype)
    {
        MediaTypeHeaderValue parsed = MediaTypeHeaderValue.Parse(value);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.MediaType, Is.EqualTo(value));
            Assert.That(value, Does.StartWith(type + "/"));
            Assert.That(value, Does.EndWith("/" + subtype));
        });
    }

    [Test]
    public void ElementCatalogIncludesModernFilterAnimationAndLegacyNames()
    {
        string[] names =
        [
            SvgElementNames.FeDropShadow,
            SvgElementNames.FeTurbulence,
            SvgElementNames.AnimateMotion,
            SvgElementNames.Discard,
            SvgElementNames.FontFace,
            SvgElementNames.MissingGlyph
        ];

        Assert.That(names, Is.EqualTo(new[]
        {
            "feDropShadow",
            "feTurbulence",
            "animateMotion",
            "discard",
            "font-face",
            "missing-glyph"
        }));
    }

    [Test]
    public void CssPropertyAndInitialValueConstantsFormCanonicalDeclarations()
    {
        string[] declarations =
        [
            SvgCssPropertyNames.Display + ":" + SvgInitialValues.Display,
            SvgCssPropertyNames.Fill + ":" + SvgInitialValues.Fill,
            SvgCssPropertyNames.Stroke + ":" + SvgInitialValues.Stroke,
            SvgCssPropertyNames.StrokeWidth + ":" + SvgInitialValues.StrokeWidth,
            SvgCssPropertyNames.ColorInterpolationFilters + ":" + SvgInitialValues.ColorInterpolationFilters,
            SvgCssPropertyNames.TransformBox + ":" + SvgInitialValues.TransformBox,
            SvgCssPropertyNames.Width + ":" + SvgInitialValues.Width
        ];

        Assert.That(declarations, Is.EqualTo(new[]
        {
            "display:inline",
            "fill:black",
            "stroke:none",
            "stroke-width:1px",
            "color-interpolation-filters:linearRGB",
            "transform-box:view-box",
            "width:auto"
        }));
    }

    [Test]
    public void FeatureIdentifiersHaveStableNamespacesAndNoDuplicates()
    {
        string[] profiles =
        [
            SvgFeatureIdentifiers.ProfileSvg2,
            SvgFeatureIdentifiers.ProfileSvg11Compatibility,
            SvgFeatureIdentifiers.ProfileLegacyPreservation,
            SvgFeatureIdentifiers.ProfileHostCallback
        ];
        string[] features =
        [
            SvgFeatureIdentifiers.SourceMapping,
            SvgFeatureIdentifiers.Diagnostics,
            SvgFeatureIdentifiers.SecurityLimits,
            SvgFeatureIdentifiers.CapabilityReporting,
            SvgFeatureIdentifiers.Loading,
            SvgFeatureIdentifiers.EditableDocument,
            SvgFeatureIdentifiers.DocumentMutation,
            SvgFeatureIdentifiers.CssParsing,
            SvgFeatureIdentifiers.CssSelectors,
            SvgFeatureIdentifiers.CssCascade,
            SvgFeatureIdentifiers.Shapes,
            SvgFeatureIdentifiers.Paths,
            SvgFeatureIdentifiers.PathProcessing,
            SvgFeatureIdentifiers.PaintServers,
            SvgFeatureIdentifiers.Markers,
            SvgFeatureIdentifiers.Clipping,
            SvgFeatureIdentifiers.Masking,
            SvgFeatureIdentifiers.Filters,
            SvgFeatureIdentifiers.Images,
            SvgFeatureIdentifiers.PngCodec,
            SvgFeatureIdentifiers.HostImageCodec,
            SvgFeatureIdentifiers.TextLayout,
            SvgFeatureIdentifiers.OpenType,
            SvgFeatureIdentifiers.HostTextShaping,
            SvgFeatureIdentifiers.SmilAnimation,
            SvgFeatureIdentifiers.CssAnimation,
            SvgFeatureIdentifiers.Interaction,
            SvgFeatureIdentifiers.Accessibility,
            SvgFeatureIdentifiers.Scene,
            SvgFeatureIdentifiers.Rasterization,
            SvgFeatureIdentifiers.ForeignContent,
            SvgFeatureIdentifiers.ExternalResources,
            SvgFeatureIdentifiers.Validation,
            SvgFeatureIdentifiers.Sanitization,
            SvgFeatureIdentifiers.Processing,
            SvgFeatureIdentifiers.Generation,
            SvgFeatureIdentifiers.Writing,
            SvgFeatureIdentifiers.ScriptPreservation,
            SvgFeatureIdentifiers.ScriptExecution,
            SvgFeatureIdentifiers.LegacyPreservation
        ];

        Assert.Multiple(() =>
        {
            Assert.That(profiles, Has.All.StartsWith("svg.profile."));
            Assert.That(features, Has.All.StartsWith("svg.feature."));
            Assert.That(profiles.Distinct().Count(), Is.EqualTo(profiles.Length));
            Assert.That(features.Distinct().Count(), Is.EqualTo(features.Length));
        });
    }
}
