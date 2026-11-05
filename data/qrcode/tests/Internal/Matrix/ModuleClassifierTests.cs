using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ModuleClassifierTests
{
    [Test]
    public void Model2MapsClassifyEveryVersionAndErrorCorrectionLevel()
    {
        for (int version = Model2VersionTable.MinimumVersion;
            version <= Model2VersionTable.MaximumVersion;
            version++)
        {
            for (int level = EccBlockTable.Low; level <= EccBlockTable.High; level++)
            {
                QrModuleRole[] roles = ModuleClassifier.CreateModel2RoleMap(version, level);
                BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
                int dimension = Model2VersionTable.GetDimension(version);
                int dataBits = EccBlockTable.GetDataCodewordCount(version, level) * 8;
                int eccBits = (Model2VersionTable.GetTotalCodewordCount(version)
                    - EccBlockTable.GetDataCodewordCount(version, level)) * 8;

                Assert.Multiple(() =>
                {
                    Assert.That(roles.Length, Is.EqualTo(dimension * dimension));
                    Assert.That(CountRole(roles, QrModuleRole.Data), Is.EqualTo(dataBits));
                    Assert.That(CountRole(roles, QrModuleRole.ErrorCorrection), Is.EqualTo(eccBits));
                    Assert.That(
                        CountRole(roles, QrModuleRole.Remainder),
                        Is.EqualTo(Model2VersionTable.GetRemainderBitCount(version)));
                    Assert.That(CountRole(roles, QrModuleRole.Unknown), Is.Zero);
                });
                AssertReservedAndPayloadRolesAgree(roles, reserved);
            }
        }
    }

    [Test]
    public void MicroMapsClassifyEverySupportedVersionAndErrorCorrectionLevel()
    {
        int[][] supportedLevels = new int[][]
        {
            new int[] { MicroVersionTable.DetectionOnly },
            new int[] { EccBlockTable.Low, EccBlockTable.Medium },
            new int[] { EccBlockTable.Low, EccBlockTable.Medium },
            new int[] { EccBlockTable.Low, EccBlockTable.Medium, EccBlockTable.Quartile }
        };

        for (int version = MicroVersionTable.MinimumVersion;
            version <= MicroVersionTable.MaximumVersion;
            version++)
        {
            for (int levelIndex = 0;
                levelIndex < supportedLevels[version - 1].Length;
                levelIndex++)
            {
                int level = supportedLevels[version - 1][levelIndex];
                QrModuleRole[] roles = ModuleClassifier.CreateMicroRoleMap(version, level);
                BitMatrixBuffer reserved = FunctionPatternBuilder.CreateMicroReservedMap(version);
                int dimension = MicroVersionTable.GetDimension(version);
                int dataBits = MicroVersionTable.GetDataBitCapacity(version, level);
                int eccBits = MicroVersionTable.GetEccCodewordCount(version, level) * 8;

                Assert.Multiple(() =>
                {
                    Assert.That(roles.Length, Is.EqualTo(dimension * dimension));
                    Assert.That(CountRole(roles, QrModuleRole.Data), Is.EqualTo(dataBits));
                    Assert.That(CountRole(roles, QrModuleRole.ErrorCorrection), Is.EqualTo(eccBits));
                    Assert.That(CountRole(roles, QrModuleRole.Remainder), Is.Zero);
                    Assert.That(CountRole(roles, QrModuleRole.Unknown), Is.Zero);
                });
                AssertReservedAndPayloadRolesAgree(roles, reserved);
            }
        }
    }

    [Test]
    public void RectangularMicroMapsClassifyEverySizeAndErrorCorrectionLevel()
    {
        for (int versionIndex = RectangularMicroVersionTable.MinimumVersionIndex;
            versionIndex <= RectangularMicroVersionTable.MaximumVersionIndex;
            versionIndex++)
        {
            for (int level = RectangularMicroVersionTable.Medium;
                level <= RectangularMicroVersionTable.High;
                level++)
            {
                QrModuleRole[] roles = ModuleClassifier.CreateRectangularMicroRoleMap(
                    versionIndex,
                    level);
                BitMatrixBuffer reserved = FunctionPatternBuilder.CreateRectangularMicroReservedMap(
                    versionIndex);
                int width = RectangularMicroVersionTable.GetWidth(versionIndex);
                int height = RectangularMicroVersionTable.GetHeight(versionIndex);
                int dataBits = RectangularMicroVersionTable.GetDataBitCapacity(
                    versionIndex,
                    level);
                int eccBits = (RectangularMicroVersionTable.GetTotalCodewordCount(versionIndex)
                    - RectangularMicroVersionTable.GetDataCodewordCount(versionIndex, level)) * 8;

                Assert.Multiple(() =>
                {
                    Assert.That(roles.Length, Is.EqualTo(width * height));
                    Assert.That(CountRole(roles, QrModuleRole.Data), Is.EqualTo(dataBits));
                    Assert.That(CountRole(roles, QrModuleRole.ErrorCorrection), Is.EqualTo(eccBits));
                    Assert.That(
                        CountRole(roles, QrModuleRole.Remainder),
                        Is.EqualTo(RectangularMicroVersionTable.GetRemainderBitCount(versionIndex)));
                    Assert.That(CountRole(roles, QrModuleRole.Unknown), Is.Zero);
                });
                AssertReservedAndPayloadRolesAgree(roles, reserved);
            }
        }
    }

    [Test]
    public void FunctionPatternsReceiveTheirSpecificRoles()
    {
        int modelVersion = 7;
        int modelDimension = Model2VersionTable.GetDimension(modelVersion);
        int[] modelAlignment = AlignmentPositionTable.GetPositions(modelVersion);
        QrModuleRole[] model = ModuleClassifier.CreateModel2RoleMap(
            modelVersion,
            EccBlockTable.Low);
        QrModuleRole[] micro = ModuleClassifier.CreateMicroRoleMap(
            4,
            EccBlockTable.Quartile);
        int rectangularVersion = RectangularMicroVersionTable.MaximumVersionIndex;
        int rectangularWidth = RectangularMicroVersionTable.GetWidth(rectangularVersion);
        int rectangularHeight = RectangularMicroVersionTable.GetHeight(rectangularVersion);
        int[] rectangularAlignment = RectangularMicroAlignmentTable.GetCenterColumns(
            rectangularWidth);
        QrModuleRole[] rectangular = ModuleClassifier.CreateRectangularMicroRoleMap(
            rectangularVersion,
            RectangularMicroVersionTable.High);

        Assert.Multiple(() =>
        {
            Assert.That(RoleAt(model, modelDimension, 3, 3), Is.EqualTo(QrModuleRole.Finder));
            Assert.That(RoleAt(model, modelDimension, 7, 7), Is.EqualTo(QrModuleRole.Separator));
            Assert.That(RoleAt(model, modelDimension, 6, 10), Is.EqualTo(QrModuleRole.Timing));
            Assert.That(
                RoleAt(model, modelDimension, modelAlignment[1], modelAlignment[1]),
                Is.EqualTo(QrModuleRole.Alignment));
            Assert.That(
                RoleAt(
                    model,
                    modelDimension,
                    Model2PlacementTable.GetFormatPrimaryRow(0),
                    Model2PlacementTable.GetFormatPrimaryColumn(0)),
                Is.EqualTo(QrModuleRole.Format));
            Assert.That(
                RoleAt(
                    model,
                    modelDimension,
                    Model2PlacementTable.GetVersionPrimaryRow(modelVersion, 0),
                    Model2PlacementTable.GetVersionPrimaryColumn(modelVersion, 0)),
                Is.EqualTo(QrModuleRole.Version));
            Assert.That(
                RoleAt(
                    model,
                    modelDimension,
                    Model2PlacementTable.GetDarkModuleRow(modelVersion),
                    Model2PlacementTable.GetDarkModuleColumn(modelVersion)),
                Is.EqualTo(QrModuleRole.DarkModule));

            Assert.That(RoleAt(micro, 17, 3, 3), Is.EqualTo(QrModuleRole.Finder));
            Assert.That(RoleAt(micro, 17, 7, 7), Is.EqualTo(QrModuleRole.Separator));
            Assert.That(RoleAt(micro, 17, 0, 10), Is.EqualTo(QrModuleRole.Timing));
            Assert.That(RoleAt(micro, 17, 1, 8), Is.EqualTo(QrModuleRole.Format));

            Assert.That(RoleAt(rectangular, rectangularWidth, 3, 3), Is.EqualTo(QrModuleRole.Finder));
            Assert.That(RoleAt(rectangular, rectangularWidth, 3, 7), Is.EqualTo(QrModuleRole.Separator));
            Assert.That(
                RoleAt(rectangular, rectangularWidth, rectangularHeight - 3, rectangularWidth - 3),
                Is.EqualTo(QrModuleRole.Finder));
            Assert.That(
                RoleAt(rectangular, rectangularWidth, rectangularHeight - 1, 0),
                Is.EqualTo(QrModuleRole.Finder));
            Assert.That(
                RoleAt(rectangular, rectangularWidth, 1, rectangularAlignment[0]),
                Is.EqualTo(QrModuleRole.Alignment));
            Assert.That(RoleAt(rectangular, rectangularWidth, 0, 12), Is.EqualTo(QrModuleRole.Timing));
            Assert.That(
                RoleAt(
                    rectangular,
                    rectangularWidth,
                    RectangularMicroPlacementTable.GetFinderSideFormatRow(0),
                    RectangularMicroPlacementTable.GetFinderSideFormatColumn(0)),
                Is.EqualTo(QrModuleRole.Format));
        });
    }

    [Test]
    public void MatrixClassificationRetainsValuesAndConfidenceAndMarksAsymmetricQuietZones()
    {
        int encodingDimension = Model2VersionTable.GetDimension(1);
        QrMatrix matrix = new QrMatrix
        {
            Width = encodingDimension + 6,
            Height = encodingDimension + 7,
            Values = CreateValues((encodingDimension + 6) * (encodingDimension + 7)),
            Roles = new QrModuleRole[1],
            Confidence = CreateConfidence((encodingDimension + 6) * (encodingDimension + 7)),
            QuietZoneLeft = 1,
            QuietZoneTop = 2,
            QuietZoneRight = 5,
            QuietZoneBottom = 5
        };
        QrModuleValue[] originalValues = (QrModuleValue[])matrix.Values.Clone();
        byte[] originalConfidence = (byte[])matrix.Confidence.Clone();

        QrModuleRole[] result = ModuleClassifier.ClassifyModules(
            matrix,
            QrSymbolKind.Model2,
            1,
            EccBlockTable.Low);

        int quietCount = matrix.Width * matrix.Height
            - encodingDimension * encodingDimension;
        Assert.Multiple(() =>
        {
            Assert.That(matrix.Roles, Is.SameAs(result));
            Assert.That(matrix.Values, Is.EqualTo(originalValues));
            Assert.That(matrix.Confidence, Is.EqualTo(originalConfidence));
            Assert.That(CountRole(result, QrModuleRole.QuietZone), Is.EqualTo(quietCount));
            Assert.That(RoleAt(result, matrix.Width, 0, 0), Is.EqualTo(QrModuleRole.QuietZone));
            Assert.That(RoleAt(result, matrix.Width, 2, 1), Is.EqualTo(QrModuleRole.Finder));
            Assert.That(
                RoleAt(result, matrix.Width, matrix.Height - 1, matrix.Width - 1),
                Is.EqualTo(QrModuleRole.QuietZone));
            Assert.That(CountRole(result, QrModuleRole.Unknown), Is.Zero);
        });
    }

    [Test]
    public void InvalidProfilesConfigurationsAndMatrixLayoutsAreRejected()
    {
        QrMatrix valid = CreateMatrix(21, 21);
        QrMatrix wrongDimensions = CreateMatrix(22, 21);
        QrMatrix shortValues = CreateMatrix(21, 21);
        shortValues.Values = new QrModuleValue[440];
        QrMatrix shortConfidence = CreateMatrix(21, 21);
        shortConfidence.Confidence = new byte[440];
        QrMatrix negativeQuietZone = CreateMatrix(21, 21);
        negativeQuietZone.QuietZoneLeft = -1;
        QrMatrix emptyEncodingRegion = CreateMatrix(21, 21);
        emptyEncodingRegion.QuietZoneLeft = 21;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => ModuleClassifier.ClassifyModules(
                    null!,
                    QrSymbolKind.Model2,
                    1,
                    EccBlockTable.Low),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => ModuleClassifier.ClassifyModules(
                    wrongDimensions,
                    QrSymbolKind.Model2,
                    1,
                    EccBlockTable.Low),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => ModuleClassifier.ClassifyModules(
                    shortValues,
                    QrSymbolKind.Model2,
                    1,
                    EccBlockTable.Low),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => ModuleClassifier.ClassifyModules(
                    shortConfidence,
                    QrSymbolKind.Model2,
                    1,
                    EccBlockTable.Low),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => ModuleClassifier.ClassifyModules(
                    negativeQuietZone,
                    QrSymbolKind.Model2,
                    1,
                    EccBlockTable.Low),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => ModuleClassifier.ClassifyModules(
                    emptyEncodingRegion,
                    QrSymbolKind.Model2,
                    1,
                    EccBlockTable.Low),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => ModuleClassifier.CreateEncodingRoleMap(
                    (QrSymbolKind)99,
                    1,
                    EccBlockTable.Low),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => ModuleClassifier.CreateEncodingRoleMap(
                    QrSymbolKind.LegacyModel1,
                    1,
                    EccBlockTable.Low),
                Throws.TypeOf<QrUnsupportedFeatureException>());
            Assert.That(
                () => ModuleClassifier.CreateModel2RoleMap(0, EccBlockTable.Low),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => ModuleClassifier.CreateModel2RoleMap(1, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => ModuleClassifier.CreateMicroRoleMap(1, EccBlockTable.Low),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => ModuleClassifier.CreateRectangularMicroRoleMap(32, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => ModuleClassifier.ClassifyModules(
                    valid,
                    QrSymbolKind.Model2,
                    1,
                    EccBlockTable.Low),
                Throws.Nothing);
        });
    }

    static void AssertReservedAndPayloadRolesAgree(
        QrModuleRole[] roles,
        BitMatrixBuffer reserved)
    {
        int width = reserved.GetWidth();
        int height = reserved.GetHeight();
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                QrModuleRole role = RoleAt(roles, width, row, column);
                bool functionRole = (int)role >= (int)QrModuleRole.Finder
                    && (int)role <= (int)QrModuleRole.DarkModule;
                bool payloadRole = (int)role >= (int)QrModuleRole.Data
                    && (int)role <= (int)QrModuleRole.Remainder;
                if (reserved.GetModule(row, column))
                {
                    Assert.That(
                        functionRole,
                        Is.True,
                        $"Reserved module ({row}, {column}) was classified as {role}.");
                }
                else
                {
                    Assert.That(
                        payloadRole,
                        Is.True,
                        $"Payload module ({row}, {column}) was classified as {role}.");
                }
            }
        }
    }

    static int CountRole(QrModuleRole[] roles, QrModuleRole expected)
    {
        int count = 0;
        for (int index = 0; index < roles.Length; index++)
        {
            if (roles[index] == expected)
            {
                count++;
            }
        }

        return count;
    }

    static QrModuleRole RoleAt(
        QrModuleRole[] roles,
        int width,
        int row,
        int column)
    {
        return roles[row * width + column];
    }

    static QrModuleValue[] CreateValues(int length)
    {
        QrModuleValue[] result = new QrModuleValue[length];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = index % 2 == 0 ? QrModuleValue.Light : QrModuleValue.Dark;
        }

        return result;
    }

    static byte[] CreateConfidence(int length)
    {
        byte[] result = new byte[length];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = (byte)((index * 37) & 255);
        }

        return result;
    }

    static QrMatrix CreateMatrix(int width, int height)
    {
        return new QrMatrix
        {
            Width = width,
            Height = height,
            Values = new QrModuleValue[width * height]
        };
    }
}
