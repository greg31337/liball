using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class RoleMapRendererTests
{
    [Test]
    public void RoleMapDistinguishesFunctionPayloadEccRemainderDamageAndUnknown()
    {
        QrSymbol symbol = CreateSymbol(
            new QrModuleValue[]
            {
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Dark
            },
            new QrModuleRole[]
            {
                QrModuleRole.Finder,
                QrModuleRole.Data,
                QrModuleRole.ErrorCorrection,
                QrModuleRole.Remainder,
                QrModuleRole.Data,
                QrModuleRole.Unknown
            });
        symbol.Validation = new QrValidationResult
        {
            DamagedModuleIndices = new int[] { 4 },
            DamagedRoles = new QrModuleRole[] { QrModuleRole.Data }
        };
        QrRenderOptions options = new QrRenderOptions
        {
            IncludeQuietZone = false
        };

        QrRaster result = RoleMapRenderer.RenderRoleMap(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(6));
            Assert.That(result.Height, Is.EqualTo(1));
            Assert.That(result.Stride, Is.EqualTo(6));
            Assert.That(result.Pixels, Is.EqualTo(new byte[] { 36, 73, 109, 146, 182, 219 }));
            Assert.That(result.Pixels.Distinct().Count(), Is.EqualTo(6));
        });
    }

    [Test]
    public void UnreadableKnownRolesRemainDistinctFromUnknownRoles()
    {
        QrSymbol symbol = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Unknown, QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Timing, QrModuleRole.Unknown });
        QrRenderOptions options = new QrRenderOptions
        {
            IncludeQuietZone = false,
            DarkValue = 0,
            LightValue = 210
        };

        QrRaster result = RoleMapRenderer.RenderRoleMap(symbol, options);

        Assert.That(result.Pixels, Is.EqualTo(new byte[] { 150, 180 }));
    }

    [Test]
    public void HighlightQuietZoneScaleAndInversionAreAppliedTogether()
    {
        QrSymbol symbol = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark, QrModuleValue.Light },
            new QrModuleRole[] { QrModuleRole.Data, QrModuleRole.Finder });
        QrRenderOptions options = new QrRenderOptions
        {
            Scale = 2,
            QuietZoneModules = 1,
            DarkValue = 10,
            LightValue = 220,
            Invert = true,
            HighlightRole = QrModuleRole.Data
        };

        QrRaster result = RoleMapRenderer.RenderRoleMap(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(8));
            Assert.That(result.Height, Is.EqualTo(6));
            Assert.That(result.Stride, Is.EqualTo(8));
            Assert.That(result.Pixels, Is.EqualTo(new byte[]
            {
                10, 10, 10, 10, 10, 10, 10, 10,
                10, 10, 10, 10, 10, 10, 10, 10,
                10, 10, 220, 220, 190, 190, 10, 10,
                10, 10, 220, 220, 190, 190, 10, 10,
                10, 10, 10, 10, 10, 10, 10, 10,
                10, 10, 10, 10, 10, 10, 10, 10
            }));
        });
    }

    [Test]
    public void ExistingQuietZoneIsReplacedAndDamageIndicesFollowSourceCoordinates()
    {
        QrMatrix matrix = new QrMatrix
        {
            Width = 4,
            Height = 3,
            Values = new QrModuleValue[]
            {
                (QrModuleValue)99, (QrModuleValue)99, (QrModuleValue)99, (QrModuleValue)99,
                (QrModuleValue)99, QrModuleValue.Dark, QrModuleValue.Light, (QrModuleValue)99,
                (QrModuleValue)99, (QrModuleValue)99, (QrModuleValue)99, (QrModuleValue)99
            },
            Roles = new QrModuleRole[]
            {
                (QrModuleRole)99, (QrModuleRole)99, (QrModuleRole)99, (QrModuleRole)99,
                (QrModuleRole)99, QrModuleRole.Data, QrModuleRole.ErrorCorrection, (QrModuleRole)99,
                (QrModuleRole)99, (QrModuleRole)99, (QrModuleRole)99, (QrModuleRole)99
            },
            QuietZoneLeft = 1,
            QuietZoneTop = 1,
            QuietZoneRight = 1,
            QuietZoneBottom = 1
        };
        QrSymbol symbol = new QrSymbol
        {
            SymbolKind = QrSymbolKind.Model2,
            Width = 2,
            Height = 1,
            Matrix = matrix,
            Validation = new QrValidationResult
            {
                DamagedModuleIndices = new int[] { 6, 0 },
                DamagedRoles = new QrModuleRole[]
                {
                    QrModuleRole.ErrorCorrection,
                    QrModuleRole.QuietZone
                }
            }
        };
        QrModuleValue[] originalValues = (QrModuleValue[])matrix.Values.Clone();
        QrModuleRole[] originalRoles = (QrModuleRole[])matrix.Roles.Clone();
        int[] originalDamage = (int[])symbol.Validation.DamagedModuleIndices.Clone();
        QrRenderOptions options = new QrRenderOptions
        {
            QuietZoneModules = 1
        };

        QrRaster result = RoleMapRenderer.RenderRoleMap(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(4));
            Assert.That(result.Height, Is.EqualTo(3));
            Assert.That(result.Pixels, Is.EqualTo(new byte[]
            {
                255, 255, 255, 255,
                255, 73, 182, 255,
                255, 255, 255, 255
            }));
            Assert.That(matrix.Values, Is.EqualTo(originalValues));
            Assert.That(matrix.Roles, Is.EqualTo(originalRoles));
            Assert.That(symbol.Validation.DamagedModuleIndices, Is.EqualTo(originalDamage));
        });
    }

    [Test]
    public void RawRendererDispatchesRoleMapRequests()
    {
        QrSymbol symbol = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Data });
        QrRenderOptions options = new QrRenderOptions
        {
            IncludeQuietZone = false,
            RenderRoleMap = true
        };

        QrRaster direct = RoleMapRenderer.RenderRoleMap(symbol, options);
        QrRaster dispatched = RawMatrixRenderer.RenderSymbol(symbol, options);

        Assert.Multiple(() =>
        {
            Assert.That(dispatched.Width, Is.EqualTo(direct.Width));
            Assert.That(dispatched.Height, Is.EqualTo(direct.Height));
            Assert.That(dispatched.Stride, Is.EqualTo(direct.Stride));
            Assert.That(dispatched.Pixels, Is.EqualTo(direct.Pixels));
        });
    }

    [Test]
    public void InvalidMatrixRoleAndDamageMetadataAreRejected()
    {
        QrSymbol valid = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Data });
        QrSymbol missingMatrix = new QrSymbol();
        QrSymbol missingRoles = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Data });
        missingRoles.Matrix!.Roles = null;
        QrSymbol shortRoles = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Data });
        shortRoles.Matrix!.Roles = new QrModuleRole[0];
        QrSymbol invalidValue = CreateSymbol(
            new QrModuleValue[] { (QrModuleValue)99 },
            new QrModuleRole[] { QrModuleRole.Data });
        QrSymbol invalidRole = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { (QrModuleRole)99 });
        QrSymbol invalidDamageIndex = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Data });
        invalidDamageIndex.Validation = new QrValidationResult
        {
            DamagedModuleIndices = new int[] { 1 },
            DamagedRoles = new QrModuleRole[] { QrModuleRole.Data }
        };
        QrSymbol mismatchedDamageRoles = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Data });
        mismatchedDamageRoles.Validation = new QrValidationResult
        {
            DamagedModuleIndices = new int[] { 0 },
            DamagedRoles = new QrModuleRole[0]
        };
        QrRenderOptions invalidHighlight = new QrRenderOptions
        {
            HighlightRole = (QrModuleRole)99
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(null!, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(valid, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(missingMatrix, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(missingRoles, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(shortRoles, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(invalidValue, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(invalidRole, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(invalidDamageIndex, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(mismatchedDamageRoles, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(valid, invalidHighlight),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void InvalidGeometryOptionsProfileAndOversizedOutputAreRejected()
    {
        QrSymbol invalidProfile = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Data });
        invalidProfile.SymbolKind = (QrSymbolKind)99;
        QrSymbol negativeExtent = CreateSymbol(
            new QrModuleValue[] { QrModuleValue.Dark },
            new QrModuleRole[] { QrModuleRole.Data });
        negativeExtent.Matrix!.QuietZoneLeft = -1;
        QrRenderOptions invalidScale = new QrRenderOptions { Scale = 0 };
        QrRenderOptions invalidQuietZone = new QrRenderOptions { QuietZoneModules = -2 };
        QrRenderOptions oversizedScale = new QrRenderOptions
        {
            IncludeQuietZone = false,
            Scale = QrLimits.DefaultMaximumImageDimension + 1
        };
        QrRenderOptions oversizedQuietZone = new QrRenderOptions
        {
            QuietZoneModules = QrLimits.DefaultMaximumImageDimension
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(invalidProfile, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(negativeExtent, new QrRenderOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(
                    CreateSymbol(
                        new QrModuleValue[] { QrModuleValue.Dark },
                        new QrModuleRole[] { QrModuleRole.Data }),
                    invalidScale),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(
                    CreateSymbol(
                        new QrModuleValue[] { QrModuleValue.Dark },
                        new QrModuleRole[] { QrModuleRole.Data }),
                    invalidQuietZone),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(
                    CreateSymbol(
                        new QrModuleValue[] { QrModuleValue.Dark },
                        new QrModuleRole[] { QrModuleRole.Data }),
                    oversizedScale),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(
                () => RoleMapRenderer.RenderRoleMap(
                    CreateSymbol(
                        new QrModuleValue[] { QrModuleValue.Dark },
                        new QrModuleRole[] { QrModuleRole.Data }),
                    oversizedQuietZone),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static QrSymbol CreateSymbol(QrModuleValue[] values, QrModuleRole[] roles)
    {
        return new QrSymbol
        {
            SymbolKind = QrSymbolKind.Model2,
            Width = values.Length,
            Height = 1,
            Matrix = new QrMatrix
            {
                Width = values.Length,
                Height = 1,
                Values = values,
                Roles = roles
            }
        };
    }
}
