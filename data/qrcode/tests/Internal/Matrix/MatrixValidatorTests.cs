using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MatrixValidatorTests
{
    [TestCase(QrSymbolKind.Model2, 1, QrErrorCorrectionLevel.Low, QrMaskPattern.Zero)]
    [TestCase(QrSymbolKind.Model2, 7, QrErrorCorrectionLevel.Quartile, QrMaskPattern.Five)]
    [TestCase(QrSymbolKind.Micro, 1, QrErrorCorrectionLevel.None, QrMaskPattern.Zero)]
    [TestCase(QrSymbolKind.Micro, 4, QrErrorCorrectionLevel.Quartile, QrMaskPattern.Three)]
    [TestCase(QrSymbolKind.rMQR, 0, QrErrorCorrectionLevel.Medium, QrMaskPattern.Four)]
    [TestCase(QrSymbolKind.rMQR, 31, QrErrorCorrectionLevel.High, QrMaskPattern.Four)]
    public void ValidSymbolsPassEveryLogicalCheck(
        QrSymbolKind symbolKind,
        int version,
        QrErrorCorrectionLevel level,
        QrMaskPattern mask)
    {
        QrSymbol symbol = CreateValidSymbol(symbolKind, version, level, mask);

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.True);
            Assert.That(result.FailureReasons, Is.Empty);
            Assert.That(result.Failures, Is.Empty);
            Assert.That(result.DamagedModuleIndices, Is.Empty);
            Assert.That(result.DamagedRoles, Is.Empty);
            Assert.That(result.FixedPatternDamageCount, Is.Zero);
            Assert.That(result.UnknownModuleCount, Is.Zero);
            Assert.That(result.FormatInformationConsistent, Is.True);
            Assert.That(result.VersionInformationConsistent, Is.True);
            Assert.That(result.QuietZoneValid, Is.True);
            Assert.That(result.IsRecoverable, Is.True);
        });
    }

    [Test]
    public void FixedPatternDamageIsLocatedByRoleButDoesNotDestroyPayloadRecoverability()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.Model2,
            2,
            QrErrorCorrectionLevel.Medium,
            QrMaskPattern.Two);
        FlipEncodingModule(symbol.Matrix!, 3, 3);

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.False);
            Assert.That(result.FixedPatternDamageCount, Is.EqualTo(1));
            Assert.That(result.DamagedRoles, Does.Contain(QrModuleRole.Finder));
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.InvalidGeometry));
            Assert.That(result.FormatInformationConsistent, Is.True);
            Assert.That(result.VersionInformationConsistent, Is.True);
            Assert.That(result.IsRecoverable, Is.True);
        });
    }

    [Test]
    public void ConflictingFormatAndVersionCopiesAreReportedSeparately()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.Model2,
            7,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Zero);
        WritePrimaryFormat(symbol.Matrix!, 7, EccBlockTable.High, 6);
        WritePrimaryVersion(symbol.Matrix!, 7, 8);

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.False);
            Assert.That(result.FormatInformationConsistent, Is.False);
            Assert.That(result.VersionInformationConsistent, Is.False);
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.InvalidFormat));
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.InvalidVersion));
            Assert.That(result.DamagedRoles, Does.Contain(QrModuleRole.Format));
            Assert.That(result.DamagedRoles, Does.Contain(QrModuleRole.Version));
            Assert.That(result.IsRecoverable, Is.False);
        });
    }

    [Test]
    public void CorrectableFormatDamageIsRecoverableButNotStrictlyConformant()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.Model2,
            2,
            QrErrorCorrectionLevel.Medium,
            QrMaskPattern.One);
        int row = Model2PlacementTable.GetFormatPrimaryRow(0);
        int column = Model2PlacementTable.GetFormatPrimaryColumn(0);
        FlipEncodingModule(symbol.Matrix!, row, column);

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.False);
            Assert.That(result.FormatInformationConsistent, Is.True);
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.InvalidFormat));
            Assert.That(result.DamagedRoles, Does.Contain(QrModuleRole.Format));
            Assert.That(result.IsRecoverable, Is.True);
        });
    }

    [Test]
    public void QuietZoneSizeAndValuesAreValidatedAndLocated()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.Micro,
            4,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.One);
        symbol.Matrix!.Values[0] = QrModuleValue.Dark;

        QrValidationResult damaged = MatrixValidator.ValidateSymbol(symbol);
        QrMatrix core = MatrixTransformer.RemoveQuietZone(symbol.Matrix);
        symbol.Matrix = core;
        QrValidationResult missing = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(damaged.QuietZoneValid, Is.False);
            Assert.That(damaged.DamagedModuleIndices, Does.Contain(0));
            Assert.That(damaged.DamagedRoles, Does.Contain(QrModuleRole.QuietZone));
            Assert.That(missing.QuietZoneValid, Is.False);
            Assert.That(missing.IsConformant, Is.False);
            Assert.That(missing.FormatInformationConsistent, Is.True);
        });
    }

    [Test]
    public void RoleMetadataMismatchesDoNotChangeModuleValues()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.rMQR,
            10,
            QrErrorCorrectionLevel.High,
            QrMaskPattern.Four);
        QrModuleValue[] original = (QrModuleValue[])symbol.Matrix!.Values.Clone();
        int finderIndex = FindRoleIndex(symbol.Matrix, QrModuleRole.Finder);
        symbol.Matrix.Roles![finderIndex] = QrModuleRole.Data;

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.False);
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.InvalidData));
            Assert.That(result.DamagedModuleIndices, Does.Contain(finderIndex));
            Assert.That(result.DamagedRoles, Does.Contain(QrModuleRole.Finder));
            Assert.That(symbol.Matrix.Values, Is.EqualTo(original));
            Assert.That(result.IsRecoverable, Is.True);
        });
    }

    [Test]
    public void CorrectablePlacementDamageIsNonconformantButRecoverable()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.Model2,
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Three);
        int dataIndex = FindRoleIndex(symbol.Matrix!, QrModuleRole.Data);
        FlipRenderedModule(symbol.Matrix!, dataIndex);

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.False);
            Assert.That(result.DamagedModuleIndices, Does.Contain(dataIndex));
            Assert.That(result.DamagedRoles, Does.Contain(QrModuleRole.Data));
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.InvalidData));
            Assert.That(result.IsRecoverable, Is.True);
        });
    }

    [Test]
    public void ExcessCodewordDamageFailsTheChecksumRecoveryCheck()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.Model2,
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Four);
        byte[] damagedCodewords = (byte[])symbol.Codewords.Clone();
        damagedCodewords[0] ^= 0x80;
        damagedCodewords[1] ^= 0x40;
        damagedCodewords[2] ^= 0x20;
        damagedCodewords[3] ^= 0x10;
        ReplaceEncodingMatrix(symbol, damagedCodewords);

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.False);
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.ChecksumFailure));
            Assert.That(result.IsRecoverable, Is.False);
        });
    }

    [Test]
    public void CorrectableCodewordErrorsAreStillStrictConformanceFailures()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.Model2,
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Five);
        byte[] damagedCodewords = (byte[])symbol.Codewords.Clone();
        damagedCodewords[0] ^= 0x80;
        symbol.Codewords = damagedCodewords;
        ReplaceEncodingMatrix(symbol, damagedCodewords);

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.False);
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.InvalidData));
            Assert.That(result.DamagedRoles, Does.Contain(QrModuleRole.Data));
            Assert.That(result.IsRecoverable, Is.True);
        });
    }

    [Test]
    public void InvalidPaddingIsDetectedEvenWhenParityAndPlacementAreValid()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.Model2,
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Six);
        byte[] data = CodewordFinalizer.CreateDataCodewords(
            new QrSegment[0],
            QrSymbolKind.Model2,
            1,
            EccBlockTable.Low,
            false);
        data[0] = 1;
        byte[] codewords = BlockInterleaver.CreateModel2Codewords(
            data,
            1,
            EccBlockTable.Low);
        symbol.Codewords = codewords;
        ReplaceEncodingMatrix(symbol, codewords);

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.False);
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.InvalidData));
            Assert.That(result.IsRecoverable, Is.True);
            Assert.That(result.DamagedModuleIndices, Is.Empty);
        });
    }

    [Test]
    public void UnknownModulesAndCapacityMetadataFailuresAreReported()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.Micro,
            3,
            QrErrorCorrectionLevel.Medium,
            QrMaskPattern.Two);
        int dataIndex = FindRoleIndex(symbol.Matrix!, QrModuleRole.Data);
        symbol.Matrix!.Values[dataIndex] = QrModuleValue.Unknown;
        symbol.TotalCodewordCount++;
        symbol.UsedDataBitCount = symbol.DataBitCapacity + 1;
        symbol.UnusedDataBitCount = -1;

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.False);
            Assert.That(result.UnknownModuleCount, Is.EqualTo(1));
            Assert.That(result.DamagedModuleIndices, Does.Contain(dataIndex));
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.InvalidData));
        });
    }

    [Test]
    public void EncodingDimensionMismatchReturnsAValidationFailureWithoutTraversal()
    {
        QrSymbol symbol = CreateValidSymbol(
            QrSymbolKind.Model2,
            1,
            QrErrorCorrectionLevel.Medium,
            QrMaskPattern.Zero);
        QrMatrix core = MatrixTransformer.RemoveQuietZone(symbol.Matrix!);
        symbol.Matrix = MatrixTransformer.Crop(core, 0, 0, 20, 21);

        QrValidationResult result = MatrixValidator.ValidateSymbol(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConformant, Is.False);
            Assert.That(result.FailureReasons, Does.Contain(QrFailureReason.InvalidGeometry));
            Assert.That(result.VersionInformationConsistent, Is.False);
            Assert.That(result.IsRecoverable, Is.False);
        });
    }

    [Test]
    public void InvalidStorageConfigurationAndUnsupportedProfilesAreRejected()
    {
        QrSymbol malformed = CreateValidSymbol(
            QrSymbolKind.Model2,
            1,
            QrErrorCorrectionLevel.Low,
            QrMaskPattern.Zero);
        malformed.Matrix!.Values = new QrModuleValue[1];
        QrSymbol legacy = new QrSymbol
        {
            SymbolKind = QrSymbolKind.LegacyModel1,
            Version = 1,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            MaskPattern = QrMaskPattern.Zero,
            Matrix = new QrMatrix
            {
                Width = 21,
                Height = 21,
                Values = new QrModuleValue[441]
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => MatrixValidator.ValidateSymbol(null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixValidator.ValidateSymbol(new QrSymbol()),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixValidator.ValidateSymbol(malformed),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixValidator.ValidateSymbol(legacy),
                Throws.TypeOf<QrUnsupportedFeatureException>());
        });
    }

    internal static QrSymbol CreateValidSymbol(
        QrSymbolKind symbolKind,
        int version,
        QrErrorCorrectionLevel level,
        QrMaskPattern mask)
    {
        int errorCorrectionIndex = GetErrorCorrectionIndex(symbolKind, level);
        byte[] data = CodewordFinalizer.CreateDataCodewords(
            new QrSegment[0],
            symbolKind,
            version,
            errorCorrectionIndex,
            false);
        byte[] codewords = BlockInterleaver.CreateCodewords(
            data,
            symbolKind,
            version,
            errorCorrectionIndex);
        BitMatrixBuffer bits = BuildMatrix(
            codewords,
            symbolKind,
            version,
            errorCorrectionIndex,
            mask);
        QrMatrix core = ConvertMatrix(bits);
        QrMatrix matrix = MatrixTransformer.AddQuietZone(core, symbolKind, -1);
        ModuleClassifier.ClassifyModules(
            matrix,
            symbolKind,
            version,
            errorCorrectionIndex);
        int dataCodewords = data.Length;
        int totalCodewords = codewords.Length;
        int dataBits = GetDataBitCapacity(symbolKind, version, errorCorrectionIndex);
        return new QrSymbol
        {
            SymbolKind = symbolKind,
            Version = version,
            Width = bits.GetWidth(),
            Height = bits.GetHeight(),
            ErrorCorrectionLevel = level,
            MaskPattern = mask,
            Matrix = matrix,
            Codewords = codewords,
            Segments = new QrSegment[0],
            DataCodewordCount = dataCodewords,
            ErrorCorrectionCodewordCount = totalCodewords - dataCodewords,
            TotalCodewordCount = totalCodewords,
            RemainderBitCount = GetRemainderBitCount(symbolKind, version),
            DataBitCapacity = dataBits,
            UsedDataBitCount = 0,
            UnusedDataBitCount = dataBits
        };
    }

    static int GetErrorCorrectionIndex(
        QrSymbolKind symbolKind,
        QrErrorCorrectionLevel level)
    {
        if (symbolKind == QrSymbolKind.rMQR)
        {
            return level == QrErrorCorrectionLevel.Medium
                ? RectangularMicroVersionTable.Medium
                : RectangularMicroVersionTable.High;
        }

        return (int)level;
    }

    static int GetDataBitCapacity(
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return EccBlockTable.GetDataCodewordCount(version, errorCorrectionIndex) * 8;
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroVersionTable.GetDataBitCapacity(version, errorCorrectionIndex);
        }

        return RectangularMicroVersionTable.GetDataBitCapacity(
            version,
            errorCorrectionIndex);
    }

    static int GetRemainderBitCount(QrSymbolKind symbolKind, int version)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return Model2VersionTable.GetRemainderBitCount(version);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroVersionTable.GetRemainderBitCount(version);
        }

        return 0;
    }

    static BitMatrixBuffer BuildMatrix(
        byte[] codewords,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern mask)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return Model2MatrixBuilder.CreateSelectedModel2Matrix(
                codewords,
                version,
                errorCorrectionIndex,
                mask);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroMatrixBuilder.CreateSelectedMicroMatrix(
                codewords,
                version,
                errorCorrectionIndex,
                mask);
        }

        return RectangularMicroMatrixBuilder.CreateSelectedRectangularMicroMatrix(
            codewords,
            version,
            errorCorrectionIndex,
            mask);
    }

    static QrMatrix ConvertMatrix(BitMatrixBuffer bits)
    {
        QrMatrix result = new QrMatrix
        {
            Width = bits.GetWidth(),
            Height = bits.GetHeight(),
            Values = new QrModuleValue[bits.GetWidth() * bits.GetHeight()]
        };
        for (int row = 0; row < result.Height; row++)
        {
            for (int column = 0; column < result.Width; column++)
            {
                result.Values[row * result.Width + column] = bits.GetModule(row, column)
                    ? QrModuleValue.Dark
                    : QrModuleValue.Light;
            }
        }

        return result;
    }

    static void ReplaceEncodingMatrix(QrSymbol symbol, byte[] codewords)
    {
        int errorCorrectionIndex = GetErrorCorrectionIndex(
            symbol.SymbolKind,
            symbol.ErrorCorrectionLevel);
        BitMatrixBuffer bits = BuildMatrix(
            codewords,
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex,
            symbol.MaskPattern);
        QrMatrix core = ConvertMatrix(bits);
        symbol.Matrix = MatrixTransformer.AddQuietZone(
            core,
            symbol.SymbolKind,
            -1);
        ModuleClassifier.ClassifyModules(
            symbol.Matrix,
            symbol.SymbolKind,
            symbol.Version,
            errorCorrectionIndex);
    }

    static void FlipEncodingModule(QrMatrix matrix, int row, int column)
    {
        int renderedRow = row + matrix.QuietZoneTop;
        int renderedColumn = column + matrix.QuietZoneLeft;
        FlipRenderedModule(matrix, renderedRow * matrix.Width + renderedColumn);
    }

    static void FlipRenderedModule(QrMatrix matrix, int index)
    {
        matrix.Values[index] = matrix.Values[index] == QrModuleValue.Dark
            ? QrModuleValue.Light
            : QrModuleValue.Dark;
    }

    static int FindRoleIndex(QrMatrix matrix, QrModuleRole role)
    {
        for (int index = 0; index < matrix.Roles!.Length; index++)
        {
            if (matrix.Roles[index] == role)
            {
                return index;
            }
        }

        throw new AssertionException($"No module has role {role}.");
    }

    static void WritePrimaryFormat(
        QrMatrix matrix,
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        int code = FormatInformationCodec.CreateModel2Code(errorCorrectionIndex, mask);
        for (int bitIndex = 0; bitIndex < 15; bitIndex++)
        {
            SetEncodingModule(
                matrix,
                Model2PlacementTable.GetFormatPrimaryRow(bitIndex),
                Model2PlacementTable.GetFormatPrimaryColumn(bitIndex),
                ((code >> bitIndex) & 1) != 0);
        }
    }

    static void WritePrimaryVersion(QrMatrix matrix, int layoutVersion, int codeVersion)
    {
        int code = VersionInformationCodec.CreateCode(codeVersion);
        for (int bitIndex = 0; bitIndex < 18; bitIndex++)
        {
            SetEncodingModule(
                matrix,
                Model2PlacementTable.GetVersionPrimaryRow(layoutVersion, bitIndex),
                Model2PlacementTable.GetVersionPrimaryColumn(layoutVersion, bitIndex),
                ((code >> bitIndex) & 1) != 0);
        }
    }

    static void SetEncodingModule(QrMatrix matrix, int row, int column, bool dark)
    {
        int index = (row + matrix.QuietZoneTop) * matrix.Width
            + column
            + matrix.QuietZoneLeft;
        matrix.Values[index] = dark ? QrModuleValue.Dark : QrModuleValue.Light;
    }
}
