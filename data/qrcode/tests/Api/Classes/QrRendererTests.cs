using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrRendererTests
{
    [Test]
    public void RendersRawGray8ThroughThePublicContract()
    {
        IQrRenderer renderer = new QrRenderer();
        QrSymbol symbol = CreateSymbol(
            new QrModuleValue[]
            {
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Light,
                QrModuleValue.Dark
            },
            new QrModuleRole[]
            {
                QrModuleRole.Finder,
                QrModuleRole.Data,
                QrModuleRole.ErrorCorrection,
                QrModuleRole.Remainder
            });
        QrRenderOptions options = new QrRenderOptions
        {
            IncludeQuietZone = false,
            Scale = 2,
            DarkValue = 12,
            LightValue = 240
        };

        QrRaster result = renderer.RenderSymbol(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(4));
            Assert.That(result.Height, Is.EqualTo(4));
            Assert.That(result.Stride, Is.EqualTo(4));
            Assert.That(result.Pixels, Is.EqualTo(new byte[]
            {
                12, 12, 240, 240,
                12, 12, 240, 240,
                240, 240, 12, 12,
                240, 240, 12, 12
            }));
        });
    }

    [Test]
    public void RendersRoleMapsThroughTheDedicatedPublicMethod()
    {
        IQrRenderer renderer = new QrRenderer();
        QrSymbol symbol = CreateSymbol(
            new QrModuleValue[]
            {
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Unknown
            },
            new QrModuleRole[]
            {
                QrModuleRole.Finder,
                QrModuleRole.Data,
                QrModuleRole.ErrorCorrection,
                QrModuleRole.Remainder
            });
        symbol.Validation = new QrValidationResult
        {
            DamagedModuleIndices = new int[] { 2 },
            DamagedRoles = new QrModuleRole[] { QrModuleRole.ErrorCorrection }
        };
        QrRenderOptions options = new QrRenderOptions
        {
            IncludeQuietZone = false
        };

        QrRaster result = renderer.RenderRoleMap(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(2));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.Stride, Is.EqualTo(2));
            Assert.That(result.Pixels, Is.EqualTo(new byte[] { 36, 73, 182, 182 }));
        });
    }

    [Test]
    public void RenderSymbolHonorsTheRoleMapOption()
    {
        IQrRenderer renderer = new QrRenderer();
        QrSymbol symbol = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark, QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Finder, QrModuleRole.Data });
        QrRenderOptions options = new QrRenderOptions
        {
            IncludeQuietZone = false,
            RenderRoleMap = true
        };

        QrRaster selected = renderer.RenderSymbol(symbol, options);
        QrRaster explicitMap = renderer.RenderRoleMap(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Pixels, Is.EqualTo(new byte[] { 36, 73 }));
            Assert.That(selected.Pixels, Is.EqualTo(explicitMap.Pixels));
            Assert.That(selected.Pixels, Is.Not.SameAs(explicitMap.Pixels));
        });
    }

    [Test]
    public void ReusableInstancesReturnIndependentRastersWithoutChangingInputs()
    {
        IQrRenderer first = new QrRenderer();
        IQrRenderer second = new QrRenderer();
        QrSymbol symbol = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark, QrModuleValue.Light },
            new QrModuleRole[] { QrModuleRole.Data, QrModuleRole.ErrorCorrection });
        QrModuleValue[] originalValues = (QrModuleValue[])symbol.Matrix!.Values.Clone();
        QrModuleRole[] originalRoles = (QrModuleRole[])symbol.Matrix.Roles!.Clone();
        QrRenderOptions options = new QrRenderOptions
        {
            IncludeQuietZone = false
        };

        QrRaster firstResult = first.RenderSymbol(symbol, options);
        QrRaster secondResult = second.RenderSymbol(symbol, options);
        firstResult.Pixels[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(secondResult.Pixels, Is.EqualTo(new byte[] { 0, 255 }));
            Assert.That(firstResult.Pixels, Is.Not.SameAs(secondResult.Pixels));
            Assert.That(symbol.Matrix.Values, Is.EqualTo(originalValues));
            Assert.That(symbol.Matrix.Roles, Is.EqualTo(originalRoles));
            Assert.That(options.IncludeQuietZone, Is.False);
        });
    }

    [Test]
    public void InvalidPublicInputsUseTypedLibraryExceptions()
    {
        IQrRenderer renderer = new QrRenderer();
        QrSymbol rawSymbol = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Data });
        QrSymbol unknownRawValue = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Unknown },
            new QrModuleRole[] { QrModuleRole.Data });
        QrSymbol missingRoles = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Data });
        missingRoles.Matrix!.Roles = null;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => renderer.RenderSymbol(null!, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => renderer.RenderSymbol(rawSymbol, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => renderer.RenderSymbol(unknownRawValue, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => renderer.RenderRoleMap(null!, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => renderer.RenderRoleMap(rawSymbol, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => renderer.RenderRoleMap(missingRoles, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
        });
    }

    static QrSymbol CreateSymbol(QrModuleValue[] values, QrModuleRole[] roles)
    {
        int width = 2;
        int height = values.Length / 2;
        if (values.Length == 1)
        {
            width = 1;
            height = 1;
        }

        return new QrSymbol
        {
            SymbolKind = QrSymbolKind.Model2,
            Width = width,
            Height = height,
            Matrix = new QrMatrix
            {
                Width = width,
                Height = height,
                Values = values,
                Roles = roles
            }
        };
    }
}
