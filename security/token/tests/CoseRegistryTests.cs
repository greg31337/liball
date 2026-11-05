using token;

namespace token.Tests;

public class CoseRegistryTests
{
    [Test]
    public void HeaderRegistrySeparatesGeneralAndAlgorithmParameters()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CoseHeaderLabels.Algorithm, Is.EqualTo(1));
            Assert.That(CoseHeaderLabels.CwtClaims, Is.EqualTo(15));
            Assert.That(CoseHeaderLabelsOperations.GetName(CoseHeaderLabels.X509Chain), Is.EqualTo("x5chain"));
            Assert.That(CoseHeaderLabelsOperations.IsRegistered(CoseHeaderLabels.Receipts), Is.True);
            Assert.That(CoseHeaderLabelsOperations.IsRegistered(CoseHeaderLabels.EphemeralKey), Is.False);
            Assert.That(CoseHeaderLabelsOperations.IsAlgorithmParameter(CoseHeaderLabels.EphemeralKey), Is.True);
            Assert.That(CoseHeaderLabelsOperations.GetAlgorithmParameterName(CoseHeaderLabels.PartyUNonce), Is.EqualTo("PartyU nonce"));
            Assert.That(CoseHeaderLabelsOperations.IsRegistered(0), Is.False);
            Assert.That(CoseHeaderLabelsOperations.GetName(0), Is.Null);
        });
    }

    [Test]
    public void AlgorithmRegistryPreservesPositiveAndNegativeAssignments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CoseAlgorithms.EcdsaSha256, Is.EqualTo(-7));
            Assert.That(CoseAlgorithms.Aes256Gcm, Is.EqualTo(3));
            Assert.That(CoseAlgorithmsOperations.GetName(CoseAlgorithms.TurboShake128), Is.EqualTo("TurboSHAKE128"));
            Assert.That(CoseAlgorithmsOperations.GetName(CoseAlgorithms.KangarooTwelve256), Is.EqualTo("KT256"));
            Assert.That(CoseAlgorithmsOperations.IsRegistered(CoseAlgorithms.Ed25519), Is.True);
            Assert.That(CoseAlgorithmsOperations.IsRegistered(CoseAlgorithms.RsaSha512), Is.True);
            Assert.That(CoseAlgorithmsOperations.GetName(CoseAlgorithms.EcdsaSecp256k1Sha256), Is.EqualTo("ES256K"));
            Assert.That(CoseAlgorithmsOperations.GetName(CoseAlgorithms.InitializationVectorGeneration), Is.EqualTo("IV-GENERATION"));
            Assert.That(CoseAlgorithmsOperations.IsRegistered(0), Is.False);
            Assert.That(CoseAlgorithmsOperations.GetName(int.MaxValue), Is.Null);
        });
    }

    [Test]
    public void EveryRegistrySnapshotLabelHasAName()
    {
        AssertAllHaveNames(CoseHeaderLabelsOperations.GetAll(), CoseHeaderLabelsOperations.GetName);
        AssertAllHaveNames(CoseHeaderLabelsOperations.GetAllAlgorithmParameters(), CoseHeaderLabelsOperations.GetAlgorithmParameterName);
        AssertAllHaveNames(CoseAlgorithmsOperations.GetAll(), CoseAlgorithmsOperations.GetName);
        AssertAllHaveNames(CoseKeyLabelsOperations.GetKeyTypes(), CoseKeyLabelsOperations.GetKeyTypeName);
        AssertAllHaveNames(CoseKeyLabelsOperations.GetOperations(), CoseKeyLabelsOperations.GetOperationName);
        AssertAllHaveNames(CoseKeyLabelsOperations.GetCurves(), CoseKeyLabelsOperations.GetCurveName);
        AssertAllHaveNames(CwtClaimLabelsOperations.GetAll(), CwtClaimLabelsOperations.GetName);

        foreach (int keyType in CoseKeyLabelsOperations.GetKeyTypes())
        {
            foreach (int parameter in CoseKeyLabelsOperations.GetParametersForKeyType(keyType))
            {
                Assert.That(CoseKeyLabelsOperations.GetParameterName(keyType, parameter), Is.Not.Null.And.Not.Empty);
            }
        }
    }

    [Test]
    public void KeyParameterNamesAreResolvedInKeyTypeContext()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CoseKeyLabelsOperations.GetKeyTypeName(CoseKeyLabels.KeyTypeEc2), Is.EqualTo("EC2"));
            Assert.That(CoseKeyLabelsOperations.GetParameterName(CoseKeyLabels.KeyTypeEc2, -3), Is.EqualTo("y"));
            Assert.That(CoseKeyLabelsOperations.GetParameterName(CoseKeyLabels.KeyTypeRsa, -3), Is.EqualTo("d"));
            Assert.That(CoseKeyLabelsOperations.GetParameterName(CoseKeyLabels.KeyTypeSymmetric, -1), Is.EqualTo("k"));
            Assert.That(CoseKeyLabelsOperations.GetParameterName(CoseKeyLabels.KeyTypeOkp, CoseKeyLabels.KeyId), Is.EqualTo("kid"));
            Assert.That(CoseKeyLabelsOperations.IsParameterForKeyType(CoseKeyLabels.KeyTypeOkp, CoseKeyLabels.Y), Is.False);
            Assert.That(CoseKeyLabelsOperations.IsParameterForKeyType(CoseKeyLabels.KeyTypeEc2, CoseKeyLabels.Y), Is.True);
            Assert.That(CoseKeyLabelsOperations.IsParameterForKeyType(999, CoseKeyLabels.KeyId), Is.False);
            Assert.That(CoseKeyLabelsOperations.GetParameterName(999, CoseKeyLabels.KeyId), Is.Null);
        });
    }

    [Test]
    public void KeyOperationAndCurveRegistriesContainCurrentAssignments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CoseKeyLabelsOperations.IsRegisteredOperation(CoseKeyLabels.OperationSign), Is.True);
            Assert.That(CoseKeyLabelsOperations.GetOperationName(CoseKeyLabels.OperationMacVerify), Is.EqualTo("MAC verify"));
            Assert.That(CoseKeyLabelsOperations.IsRegisteredOperation(0), Is.False);
            Assert.That(CoseKeyLabelsOperations.IsRegisteredCurve(CoseKeyLabels.CurveEd25519), Is.True);
            Assert.That(CoseKeyLabelsOperations.GetCurveName(CoseKeyLabels.CurveBrainpoolP512r1), Is.EqualTo("brainpoolP512r1"));
            Assert.That(CoseKeyLabelsOperations.IsRegisteredCurve(0), Is.False);
        });
    }

    [Test]
    public void CwtClaimRegistryContainsCoreAndExtensionClaims()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CwtClaimLabels.Issuer, Is.EqualTo(1));
            Assert.That(CwtClaimLabelsOperations.GetName(CwtClaimLabels.CwtId), Is.EqualTo("cti"));
            Assert.That(CwtClaimLabelsOperations.IsRegistered(CwtClaimLabels.UniversalEntityIdentifier), Is.True);
            Assert.That(CwtClaimLabelsOperations.IsRegistered(CwtClaimLabels.GlobalPlatformComponent), Is.True);
            Assert.That(CwtClaimLabelsOperations.GetName(CwtClaimLabels.StatusList), Is.EqualTo("status_list"));
            Assert.That(CwtClaimLabelsOperations.IsRegistered(0), Is.False);
            Assert.That(CwtClaimLabelsOperations.GetName(0), Is.Null);
        });
    }

    [Test]
    public void RegistrySnapshotsAreCompleteUniqueAndDefensive()
    {
        int[] headers = CoseHeaderLabelsOperations.GetAll();
        int[] algorithmHeaders = CoseHeaderLabelsOperations.GetAllAlgorithmParameters();
        int[] algorithms = CoseAlgorithmsOperations.GetAll();
        int[] keyTypes = CoseKeyLabelsOperations.GetKeyTypes();
        int[] commonParameters = CoseKeyLabelsOperations.GetCommonParameters();
        int[] rsaParameters = CoseKeyLabelsOperations.GetParametersForKeyType(CoseKeyLabels.KeyTypeRsa);
        int[] operations = CoseKeyLabelsOperations.GetOperations();
        int[] curves = CoseKeyLabelsOperations.GetCurves();
        int[] claims = CwtClaimLabelsOperations.GetAll();

        Assert.Multiple(() =>
        {
            Assert.That(headers, Has.Length.EqualTo(45).And.Unique);
            Assert.That(algorithmHeaders, Has.Length.EqualTo(13).And.Unique);
            Assert.That(algorithms, Has.Length.EqualTo(86).And.Unique);
            Assert.That(keyTypes, Has.Length.EqualTo(7).And.Unique);
            Assert.That(commonParameters, Has.Length.EqualTo(5).And.Unique);
            Assert.That(rsaParameters, Has.Length.EqualTo(12).And.Unique);
            Assert.That(operations, Has.Length.EqualTo(10).And.Unique);
            Assert.That(curves, Has.Length.EqualTo(12).And.Unique);
            Assert.That(claims, Has.Length.EqualTo(75).And.Unique);
        });

        headers[0] = 999999;
        algorithmHeaders[0] = 999999;
        algorithms[0] = 999999;
        keyTypes[0] = 999999;
        commonParameters[0] = 999999;
        rsaParameters[0] = 999999;
        operations[0] = 999999;
        curves[0] = 999999;
        claims[0] = 999999;

        Assert.Multiple(() =>
        {
            Assert.That(CoseHeaderLabelsOperations.IsRegistered(999999), Is.False);
            Assert.That(CoseHeaderLabelsOperations.IsAlgorithmParameter(999999), Is.False);
            Assert.That(CoseAlgorithmsOperations.IsRegistered(999999), Is.False);
            Assert.That(CoseKeyLabelsOperations.IsRegisteredKeyType(999999), Is.False);
            Assert.That(CoseKeyLabelsOperations.IsCommonParameter(999999), Is.False);
            Assert.That(CoseKeyLabelsOperations.IsParameterForKeyType(CoseKeyLabels.KeyTypeRsa, 999999), Is.False);
            Assert.That(CoseKeyLabelsOperations.IsRegisteredOperation(999999), Is.False);
            Assert.That(CoseKeyLabelsOperations.IsRegisteredCurve(999999), Is.False);
            Assert.That(CwtClaimLabelsOperations.IsRegistered(999999), Is.False);
        });
    }

    static void AssertAllHaveNames(int[] labels, Func<int, string> getName)
    {
        foreach (int label in labels)
        {
            Assert.That(getName(label), Is.Not.Null.And.Not.Empty, $"Label {label} has no name.");
        }
    }
}
