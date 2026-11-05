using System.Collections.Generic;

namespace avif.Tests;

public class AuxiliaryAndAlphaTests
{
    [Test]
    public void AttachesAndResizesDecodedAlphaToMasterDimensions()
    {
        AvifImage master = AvifImage.Create(
            4,
            2,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifImage alpha = AvifImage.Create(
            2,
            1,
            8,
            AvifChromaFormat.Monochrome,
            false);
        (alpha.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0] = 0;
        (alpha.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[1] = 255;

        AvifResultCode result = master.AttachAlpha(
            alpha,
            true,
            AvifResamplingFilter.Nearest);
        AvifPlane? attached = master.GetPlane(AvifPlaneKind.Alpha);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(master.GetAlphaMode(),
                Is.EqualTo(AvifAlphaMode.Premultiplied));
            Assert.That((attached ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(4));
            Assert.That(attached.GetHeight(), Is.EqualTo(2));
            Assert.That(attached.GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 0, 0, 255, 255 }));
            Assert.That(attached.GetRowSpan(1).ToArray(),
                Is.EqualTo(new byte[] { 0, 0, 255, 255 }));
        });
    }

    [Test]
    public void AlphaAttachmentRejectsWrongDepthAndNonMonochromeSources()
    {
        AvifImage master = AvifImage.Create(
            2, 2, 8, AvifChromaFormat.Yuv444, false);
        AvifImage wrongDepth = AvifImage.Create(
            2, 2, 10, AvifChromaFormat.Monochrome, false);
        AvifImage color = AvifImage.Create(
            2, 2, 8, AvifChromaFormat.Yuv444, false);

        Assert.Multiple(() =>
        {
            Assert.That(master.AttachAlpha(
                    wrongDepth,
                    false,
                    AvifResamplingFilter.Bilinear),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(master.AttachAlpha(
                    color,
                    false,
                    AvifResamplingFilter.Bilinear),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(master.GetAlphaMode(), Is.EqualTo(AvifAlphaMode.None));
            Assert.That(master.GetPlane(AvifPlaneKind.Alpha), Is.Null);
        });
    }

    [Test]
    public void ConvertsPremultipliedPixelsToStraightWithDefinedZeroAlpha()
    {
        AvifPixelBuffer source = AvifPixelBuffer.Create(
            2, 1, AvifPixelFormat.Rgba, 8, false);
        new byte[] { 64, 32, 16, 128, 50, 40, 30, 0 }
            .CopyTo(source.GetRowSpan(0));

        AvifPixelBuffer straight = source.ConvertAlphaMode(true, false);
        AvifPixelBuffer restored = straight.ConvertAlphaMode(false, true);

        Assert.Multiple(() =>
        {
            Assert.That(straight.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(straight.GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 128, 64, 32, 128, 0, 0, 0, 0 }));
            Assert.That(restored.GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 64, 32, 16, 128, 0, 0, 0, 0 }));
        });
    }

    [Test]
    public void SourceOverCompositionAcceptsStraightAndPremultipliedInputs()
    {
        AvifPixelBuffer straightForeground = CreatePixel(
            new byte[] { 255, 0, 0, 128 });
        AvifPixelBuffer premultipliedForeground = CreatePixel(
            new byte[] { 128, 0, 0, 128 });
        AvifPixelBuffer background = CreatePixel(
            new byte[] { 0, 0, 255, 255 });

        AvifPixelBuffer straightResult = straightForeground.CompositeOver(
            background,
            false,
            false,
            false);
        AvifPixelBuffer premultipliedResult =
            premultipliedForeground.CompositeOver(
                background,
                true,
                false,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(straightResult.GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 128, 0, 127, 255 }));
            Assert.That(premultipliedResult.GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 128, 0, 127, 255 }));
        });
    }

    [Test]
    public void DocumentResolvesAlphaDepthAndArbitraryAuxiliaryAssociations()
    {
        ParsedItem masterItem = CreateParsedItem(1);
        ParsedItem alphaItem = CreateParsedItem(2);
        ParsedItem depthItem = CreateParsedItem(3);
        ParsedItem otherItem = CreateParsedItem(4);
        List<ParsedItem> associated = new List<ParsedItem>
        {
            alphaItem,
            depthItem,
            otherItem
        };
        ParsedItemRole masterRole = CreateRole(
            masterItem,
            string.Empty,
            new List<ParsedItem>(),
            associated);
        AvifImageItem master = CreateImageItem(
            masterItem,
            masterRole,
            4,
            2,
            8,
            new List<AvifItemReference>
            {
                new AvifItemReference(
                    AvifItemReferenceKind.Premultiplied,
                    "prem",
                    1,
                    new List<uint> { 2 })
            });
        AvifImageItem alpha = CreateImageItem(
            alphaItem,
            CreateRole(
                alphaItem,
                "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha",
                new List<ParsedItem> { masterItem },
                new List<ParsedItem>()),
            2,
            1,
            8,
            new List<AvifItemReference>());
        AvifImageItem depth = CreateImageItem(
            depthItem,
            CreateRole(
                depthItem,
                "urn:mpeg:mpegB:cicp:systems:auxiliary:depth",
                new List<ParsedItem> { masterItem },
                new List<ParsedItem>()),
            4,
            2,
            10,
            new List<AvifItemReference>());
        AvifImageItem other = CreateImageItem(
            otherItem,
            CreateRole(
                otherItem,
                "urn:example:confidence",
                new List<ParsedItem> { masterItem },
                new List<ParsedItem>()),
            4,
            2,
            8,
            new List<AvifItemReference>());
        AvifDocument document = CreateDocument(
            new List<AvifImageItem> { master, alpha, depth, other });

        AvifAuxiliaryImage? alphaAssociation = document.GetAuxiliaryImage(1, 0);
        AvifAuxiliaryImage? depthAssociation = document.GetAuxiliaryImage(1, 1);
        AvifAuxiliaryImage? otherAssociation = document.GetAuxiliaryImage(1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetAuxiliaryImageCount(1), Is.EqualTo(3));
            Assert.That((alphaAssociation ?? throw new InvalidOperationException("Invalid internal state.")).GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(alphaAssociation.GetKind(),
                Is.EqualTo(AvifAuxiliaryKind.Alpha));
            Assert.That(alphaAssociation.GetItemId(), Is.EqualTo(2));
            Assert.That(alphaAssociation.GetMasterItemId(), Is.EqualTo(1));
            Assert.That(alphaAssociation.GetWidth(), Is.EqualTo(2));
            Assert.That(alphaAssociation.GetHeight(), Is.EqualTo(1));
            Assert.That(alphaAssociation.GetBitDepth(), Is.EqualTo(8));
            Assert.That(alphaAssociation.GetMasterWidth(), Is.EqualTo(4));
            Assert.That(alphaAssociation.GetMasterHeight(), Is.EqualTo(2));
            Assert.That(alphaAssociation.GetMasterBitDepth(), Is.EqualTo(8));
            Assert.That(alphaAssociation.IsPremultiplied(), Is.True);
            Assert.That(alphaAssociation.RequiresResize(), Is.True);
            Assert.That(alpha.GetAuxiliaryMasterItemId(0), Is.EqualTo(1));
            Assert.That((depthAssociation ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(AvifAuxiliaryKind.Depth));
            Assert.That(depthAssociation.GetBitDepth(), Is.EqualTo(10));
            Assert.That((otherAssociation ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(AvifAuxiliaryKind.Other));
            Assert.That(otherAssociation.GetTypeName(),
                Is.EqualTo("urn:example:confidence"));
            Assert.That(document.GetAuxiliaryImage(1, 3), Is.Null);
        });
    }

    [Test]
    public void ContainerValidationReportsAlphaDepthAndResizeMismatches()
    {
        ParsedItem master = CreateParsedItem(1).WithProperties(
            CreateParsedProperties(4, 2, 8, string.Empty));
        ParsedItem alpha = CreateParsedItem(2).WithProperties(
            CreateParsedProperties(
                2,
                1,
                10,
                "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"));
        ParsedItemReference auxiliaryReference = new ParsedItemReference(
            ParsedItemReferenceKind.Auxiliary,
            "auxl",
            0,
            2,
            new List<uint> { 1 });
        master = master.WithReferences(
            new List<ParsedItemReference>(),
            new List<ParsedItemReference> { auxiliaryReference });
        alpha = alpha.WithReferences(
            new List<ParsedItemReference> { auxiliaryReference },
            new List<ParsedItemReference>());
        ParsedMeta meta = new ParsedMeta(
            "pict",
            string.Empty,
            true,
            1,
            new List<ParsedItem> { master, alpha },
            new List<ParsedItemProperty>(),
            new List<ParsedItemReference> { auxiliaryReference },
            new List<ParsedDataReference>(),
            new List<ParsedEntityGroup>(),
            null,
            new List<ParsedDataBox>(),
            null,
            new AvifSecurityLimits());
        DiagnosticLog diagnostics = new DiagnosticLog();

        AvifContainerValidator.ValidateAuxiliaryItems(
            meta,
            diagnostics,
            new AvifValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Code,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(diagnostics.Diagnostics, Has.Count.EqualTo(2));
            Assert.That((diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Message, Does.Contain("bit depth"));
            Assert.That((diagnostics.Get(1) ?? throw new InvalidOperationException("Invalid internal state.")).Message, Does.Contain("resizing"));
            Assert.That((diagnostics.Get(1) ?? throw new InvalidOperationException("Invalid internal state.")).Severity,
                Is.EqualTo(AvifDiagnosticSeverity.Warning));
        });
    }

    static AvifPixelBuffer CreatePixel(byte[] value)
    {
        AvifPixelBuffer buffer = AvifPixelBuffer.Create(
            1, 1, AvifPixelFormat.Rgba, 8, false);
        value.CopyTo(buffer.GetRowSpan(0));
        return buffer;
    }

    static ParsedItem CreateParsedItem(uint itemId)
    {
        return new ParsedItem(
            itemId,
            2,
            itemId != 1,
            0,
            "av01",
            "item",
            string.Empty,
            string.Empty,
            string.Empty);
    }

    static ParsedItemRole CreateRole(
        ParsedItem item,
        string auxiliaryType,
        List<ParsedItem> masters,
        List<ParsedItem> associated)
    {
        return new ParsedItemRole(
            item,
            item.GetItemId() == 1,
            false,
            false,
            auxiliaryType,
            masters,
            new List<ParsedItem>(),
            associated,
            new List<ParsedItem>());
    }

    static AvifImageItem CreateImageItem(
        ParsedItem item,
        ParsedItemRole role,
        uint width,
        uint height,
        int bitDepth,
        List<AvifItemReference> outgoing)
    {
        List<AvifItemProperty> properties = new List<AvifItemProperty>
        {
            CreateSpatialProperty(width, height),
            CreatePixelProperty(bitDepth)
        };
        return new AvifImageItem(
            item,
            role,
            true,
            new List<AvifItemExtent>(),
            properties,
            outgoing,
            new List<AvifItemReference>(),
            new List<uint>());
    }

    static AvifItemProperty CreateSpatialProperty(uint width, uint height)
    {
        ParsedItemProperty property = CreateProperty("ispe");
        property.SetSpatialExtents(new ParsedImageSpatialExtents(width, height));
        return new AvifItemProperty(new ParsedPropertyAssociation(1, false, property));
    }

    static AvifItemProperty CreatePixelProperty(int bitDepth)
    {
        ParsedItemProperty property = CreateProperty("pixi");
        property.SetPixelInformation(new ParsedPixelInformation(
            false,
            new List<ParsedPixelChannel>
            {
                new ParsedPixelChannel(
                    bitDepth,
                    0,
                    0,
                    false,
                    0,
                    0,
                    string.Empty)
            }));
        return new AvifItemProperty(new ParsedPropertyAssociation(2, false, property));
    }

    static ParsedItemProperty CreateProperty(string type)
    {
        return new ParsedItemProperty(
            1,
            type,
            0,
            false,
            System.Array.Empty<byte>(),
            System.Array.Empty<byte>());
    }

    static List<ParsedPropertyAssociation> CreateParsedProperties(
        uint width,
        uint height,
        int bitDepth,
        string auxiliaryType)
    {
        ParsedItemProperty spatial = CreateProperty("ispe");
        spatial.SetSpatialExtents(new ParsedImageSpatialExtents(width, height));
        ParsedItemProperty pixels = CreateProperty("pixi");
        pixels.SetPixelInformation(new ParsedPixelInformation(
            false,
            new List<ParsedPixelChannel>
            {
                new ParsedPixelChannel(
                    bitDepth,
                    0,
                    0,
                    false,
                    0,
                    0,
                    string.Empty)
            }));
        List<ParsedPropertyAssociation> properties =
            new List<ParsedPropertyAssociation>
            {
                new ParsedPropertyAssociation(1, false, spatial),
                new ParsedPropertyAssociation(2, false, pixels)
            };
        if (auxiliaryType.Length != 0)
        {
            ParsedItemProperty auxiliary = CreateProperty("auxC");
            AdditionalItemPropertyParseResult parsed =
                new AdditionalItemPropertyParseResult(AvifResultCode.Success);
            parsed.AuxiliaryType = new ParsedAuxiliaryType(
                auxiliaryType,
                System.Array.Empty<byte>());
            auxiliary.SetAdditionalProperties(parsed);
            properties.Add(new ParsedPropertyAssociation(3, false, auxiliary));
        }
        return properties;
    }

    static AvifDocument CreateDocument(List<AvifImageItem> items)
    {
        return new AvifDocument(
            AvifResultCode.Success,
            null,
            null,
            new List<AvifBox>(),
            new List<AvifBox>(),
            items,
            new List<AvifItemReference>(),
            new List<AvifEntityGroup>(),
            new List<AvifDiagnostic>());
    }
}
