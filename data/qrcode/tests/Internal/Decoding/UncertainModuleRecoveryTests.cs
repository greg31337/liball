using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class UncertainModuleRecoveryTests
{
    [TestCase(QrSymbolKind.Model2, 1, 0, 4)]
    [TestCase(QrSymbolKind.Micro, 2, 0, 3)]
    [TestCase(QrSymbolKind.rMQR, 0, 0, 4)]
    [TestCase(QrSymbolKind.LegacyModel1, 1, 0, 4)]
    public void AConfidenceSelectedFlipBringsErrorsWithinVerifiedRsCapacity(QrSymbolKind kind, int version, int level, int errors)
    {
        byte[] original = CodewordErasureRecoveryTests.Words(kind, version, level);
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(original, kind, version, level);
        int flip = -1;
        for (int word = 0; word < errors; word++)
        {
            int cell = Cells(kind, version, level, word)[0];
            Flip(matrix, cell);
            if (word == 0) flip = cell;
        }
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        matrix.Confidence[flip] = 128;
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        QrDecodeOptions options = Options(kind);
        Assert.That(MatrixCodewordReader.Read(matrix, kind, options).Success, Is.False);
        MatrixRecoveryResult recovered = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        Assert.That(recovered.Success, Is.True, recovered.FailureReason.ToString());
        Assert.That(recovered.FlippedModuleIndices, Is.EqualTo(new[] { flip }));
        Assert.That(recovered.Codewords!.Correction!.Codewords, Is.EqualTo(original));
        Assert.That(recovered.Codewords.Correction.BlockResults.All(b => b.Verified), Is.True);
        Assert.That(MatrixDecodeRecovery.Decode(matrix, options).Text, Is.EqualTo("123"));
        Assert.That(MatrixDecodeRecovery.Decode(matrix, options).Warnings, Has.Some.Contains("BCH/RS-verified search"));
        Assert.That(matrix.Values, Is.EqualTo(before));
        Assert.That(matrix.Confidence[flip], Is.EqualTo(128));
        options.EnableRecovery = false;
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(matrix, options).Success, Is.False);
    }

    [Test]
    public void SearchesPairsAfterSinglesAndPublishesOnlyTheVerifiedRepair()
    {
        QrMatrix matrix = Fixture();
        int[] corrupt = Enumerable.Range(0, 5).Select(word => Cells(QrSymbolKind.Model2, 1, 0, word)[0]).ToArray();
        foreach (int cell in corrupt) Flip(matrix, cell);
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        matrix.Confidence[corrupt[0]] = 128;
        matrix.Confidence[corrupt[1]] = 150;
        MatrixRecoveryResult recovered = MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.Model2));
        Assert.That(recovered.Success, Is.True);
        Assert.That(recovered.FlippedModuleIndices, Is.EqualTo(corrupt.Take(2)));
        MatrixRecoveryResult search = new MatrixRecoveryResult();
        Assert.That(UncertainModuleRecovery.Recover(matrix, QrSymbolKind.Model2, 1, Options(QrSymbolKind.Model2), search), Is.True);
        Assert.That(search.AttemptCount, Is.EqualTo(3)); // Two singles, then their pair.
        Assert.That(recovered.AttemptCount, Is.GreaterThan(search.AttemptCount));
        Assert.That(MatrixDecodeRecovery.Decode(matrix, Options(QrSymbolKind.Model2)).Text, Is.EqualTo("123"));
    }

    [Test]
    public void CandidateOrderingUsesConfidenceThenPositionAndExcludesUnprotectedCells()
    {
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(QrSymbolKind.Model2, 2, 0), QrSymbolKind.Model2, 2, 0);
        matrix = MatrixTransformer.AddQuietZone(matrix, QrSymbolKind.Model2, 4);
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        QrModuleRole[] roles = ModuleClassifier.CreateModel2RoleMap(2, 0);
        List<int> eligible = new List<int>();
        foreach (int cell in Enumerable.Range(0, roles.Length).Where(i => roles[i] == QrModuleRole.Data).Take(6))
            eligible.Add((cell / 25 + 4) * matrix.Width + cell % 25 + 4);
        for (int i = 0; i < eligible.Count; i++) matrix.Confidence[eligible[i]] = (byte)(200 - i * 10);
        matrix.Confidence[eligible[0]] = 150; // Equal confidence: lower position first.
        matrix.Confidence[0] = 0; // Quiet zone.
        foreach (int cell in Enumerable.Range(0, roles.Length).Where(i => roles[i] == QrModuleRole.Finder || roles[i] == QrModuleRole.Remainder))
            matrix.Confidence[(cell / 25 + 4) * matrix.Width + cell % 25 + 4] = 0;
        matrix.Roles = Enumerable.Repeat(QrModuleRole.Data, matrix.Values.Length).ToArray();
        Assert.That(UncertainModuleRecovery.GetCandidates(matrix, QrSymbolKind.Model2, 2),
            Is.EqualTo(new[] { eligible[0], eligible[5], eligible[4], eligible[3] }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RepairedFormatStillRequiresRsProof(bool unknown)
    {
        QrMatrix matrix = Fixture();
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        for (int bit = 0; bit < 4; bit++) Flip(matrix, Model2PlacementTable.GetFormatPrimaryRow(bit) * 21 + Model2PlacementTable.GetFormatPrimaryColumn(bit));
        int suspect = Model2PlacementTable.GetFormatPrimaryRow(0) * 21 + Model2PlacementTable.GetFormatPrimaryColumn(0);
        matrix.Confidence[suspect] = 1;
        if (unknown) matrix.Values[suspect] = QrModuleValue.Unknown;
        // Neither copy initially has valid BCH evidence.
        for (int bit = 0; bit < 4; bit++)
        {
            int cell = Model2PlacementTable.GetFormatSecondaryRow(1, bit) * 21 + Model2PlacementTable.GetFormatSecondaryColumn(1, bit);
            Flip(matrix, cell);
        }
        Assert.That(MatrixCodewordReader.Read(matrix, QrSymbolKind.Model2, Options(QrSymbolKind.Model2)).Success, Is.False);
        Assert.That(MatrixDecodeRecovery.Decode(matrix, Options(QrSymbolKind.Model2)).Text, Is.EqualTo("123"));
        foreach (int word in Enumerable.Range(0, 12))
            foreach (int cell in Cells(QrSymbolKind.Model2, 1, 0, word)) Flip(matrix, cell);
        MatrixRecoveryResult failed = MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.Model2));
        Assert.That(failed.Success, Is.False);
        Assert.That(failed.Codewords, Is.Null);
    }

    [Test]
    public void NormalizesRotationMirrorAndPolarityBeforeSelectingUncertainValues()
    {
        QrMatrix matrix = Fixture();
        foreach (int word in Enumerable.Range(0, 4)) Flip(matrix, Cells(QrSymbolKind.Model2, 1, 0, word)[0]);
        int suspect = Cells(QrSymbolKind.Model2, 1, 0, 0)[0];
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        matrix.Confidence[suspect] = 153;
        QrMatrix transformed = MatrixTransformer.InvertPolarity(MatrixTransformer.Rotate(MatrixTransformer.Mirror(matrix), 1));
        QrModuleValue[] before = (QrModuleValue[])transformed.Values.Clone();
        QrDecodeOptions options = Options(QrSymbolKind.Model2);
        options.TryMirrored = true;
        options.TryInverted = true;
        MatrixRecoveryResult result = MatrixDecodeRecovery.RecoverCodewords(transformed, options);
        Assert.That(result.Success, Is.True, result.FailureReason.ToString());
        Assert.That(result.IsMirrored, Is.True);
        Assert.That(result.IsInverted, Is.True);
        Assert.That(result.FlippedModuleIndices, Is.EqualTo(new[] { suspect }));
        Assert.That(MatrixDecodeRecovery.CreateDecodedResult(result, options).Text, Is.EqualTo("123"));
        Assert.That(transformed.Values, Is.EqualTo(before));
    }

    [Test]
    public void ConflictingVerifiedAssignmentsAreRejectedAsAmbiguous()
    {
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(QrSymbolKind.Micro, 2, 0), QrSymbolKind.Micro, 2, 0);
        // Six erased codewords exceed the five parity bytes. Resolving one
        // unknown leaves five erasures, allowing incompatible verified solutions.
        foreach (int word in Enumerable.Range(0, 6)) matrix.Values[Cells(QrSymbolKind.Micro, 2, 0, word)[0]] = QrModuleValue.Unknown;
        MatrixRecoveryResult result = MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.Micro));
        Assert.That(result.Success, Is.False);
        Assert.That(result.HasAmbiguousRepair, Is.True);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
        Assert.That(result.Codewords, Is.Null);
        Assert.That(result.Matrix, Is.Null);
    }

    [Test]
    public void BudgetExhaustionDiscardsAnAlreadyVerifiedTentativeCandidate()
    {
        QrMatrix matrix = Fixture();
        foreach (int word in Enumerable.Range(0, 4)) Flip(matrix, Cells(QrSymbolKind.Model2, 1, 0, word)[0]);
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        int first = Cells(QrSymbolKind.Model2, 1, 0, 0)[0];
        int second = Cells(QrSymbolKind.Model2, 1, 0, 4)[0];
        matrix.Confidence[first] = 128;
        matrix.Confidence[second] = 200;
        QrDecodeOptions options = Options(QrSymbolKind.Model2);
        options.MaximumRecoveryAttempts = 2;
        QrMatrix verified = MatrixTransformer.Rotate(matrix, 0);
        Flip(verified, first);
        verified.Confidence![first] = 255;
        Assert.That(MatrixCodewordReader.Read(verified, QrSymbolKind.Model2, options).Success, Is.True);
        MatrixRecoveryResult result = new MatrixRecoveryResult { AttemptCount = 1 };
        Assert.That(UncertainModuleRecovery.Recover(matrix, QrSymbolKind.Model2, 1, options, result), Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(result.AttemptCount, Is.EqualTo(2));
        Assert.That(result.Codewords, Is.Null);
        Assert.That(result.FlippedModuleIndices, Is.Empty);
    }

    [Test]
    public void DoesNotFlipFullyConfidentModulesOrEnableCorrectionForMicroM1()
    {
        QrMatrix matrix = Fixture();
        foreach (int word in Enumerable.Range(0, 5)) Flip(matrix, Cells(QrSymbolKind.Model2, 1, 0, word)[0]);
        Assert.That(UncertainModuleRecovery.GetCandidates(matrix, QrSymbolKind.Model2, 1), Is.Empty);
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(matrix, Options(QrSymbolKind.Model2)).Success, Is.False);
        QrMatrix micro = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(QrSymbolKind.Micro, 1, -1), QrSymbolKind.Micro, 1, -1);
        int cell = Cells(QrSymbolKind.Micro, 1, -1, 0)[0];
        Flip(micro, cell);
        micro.Confidence = Enumerable.Repeat((byte)255, micro.Values.Length).ToArray();
        micro.Confidence[cell] = 128;
        Assert.That(MatrixDecodeRecovery.RecoverCodewords(micro, Options(QrSymbolKind.Micro)).Success, Is.False);
    }

    static int[] Cells(QrSymbolKind kind, int version, int level, int word) => CodewordErasureRecoveryTests.Cells(kind, version, level, word);
    static QrMatrix Fixture() => CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(QrSymbolKind.Model2, 1, 0), QrSymbolKind.Model2, 1, 0);
    static void Flip(QrMatrix matrix, int index) => matrix.Values[index] = matrix.Values[index] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
    static QrDecodeOptions Options(QrSymbolKind kind) => new QrDecodeOptions
    {
        AllowModel2 = kind == QrSymbolKind.Model2, AllowMicro = kind == QrSymbolKind.Micro,
        AllowRectangularMicro = kind == QrSymbolKind.rMQR, AllowLegacyModel1 = kind == QrSymbolKind.LegacyModel1,
        TryMirrored = false, TryInverted = false, MaximumRecoveryAttempts = 128
    };
}
