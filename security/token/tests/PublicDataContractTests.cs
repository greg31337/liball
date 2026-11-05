using System;
using System.Reflection;
using token;

namespace token.Tests;

public class PublicDataContractTests
{
    static readonly Type[] CompletionContractTypes =
    {
        typeof(AesCbcHmacContentEncryptionResult),
        typeof(AesGcmContentEncryptionResult),
        typeof(AesGcmKeyWrapResult),
        typeof(AlgorithmMetadata),
        typeof(CborMapSortResult),
        typeof(ClaimMappingResult),
        typeof(ClientAssertionJwtValidationPolicy),
        typeof(CoseAesCcmResult),
        typeof(CoseAesGcmResult),
        typeof(CoseCounterSignatureTarget),
        typeof(CoseDetachedEncryptionResult),
        typeof(CoseDirectKeyDistributionResult),
        typeof(CoseEcdhResult),
        typeof(CoseKdfContextParameters),
        typeof(CoseKdfPartyInfo),
        typeof(CoseKeyResult),
        typeof(CwtProtectionKind),
        typeof(CwtProtectionLayer),
        typeof(CwtValidationPolicy),
        typeof(CwtValidationResult),
        typeof(DirectKeyManagementResult),
        typeof(DpopProofValidationPolicy),
        typeof(DpopProofValidationResult),
        typeof(EcdhKeyManagementResult),
        typeof(JoseCompactParseResult),
        typeof(JoseCompactScanResult),
        typeof(JoseCompactSegments),
        typeof(JoseCompactSerialization),
        typeof(JweCompactResult),
        typeof(JweCompactToken),
        typeof(JweFlattenedResult),
        typeof(JweFlattenedToken),
        typeof(JweGeneralRecipient),
        typeof(JweGeneralResult),
        typeof(JweGeneralToken),
        typeof(JwsCompactResult),
        typeof(JwsCompactToken),
        typeof(JwsFlattenedResult),
        typeof(JwsFlattenedToken),
        typeof(JwsGeneralResult),
        typeof(JwsGeneralSignature),
        typeof(JwsGeneralToken),
        typeof(JwtNumericDateResult),
        typeof(JwtSecuredAuthorizationRequestValidationPolicy),
        typeof(JwtSecuredAuthorizationResponseValidationPolicy),
        typeof(JwtValidationPolicy),
        typeof(JwtValidationResult),
        typeof(NestedJoseResult),
        typeof(NestedJoseToken),
        typeof(OpenIdConnectIdTokenValidationPolicy),
        typeof(Pbes2KeyWrapResult),
        typeof(PemBlock),
        typeof(PemReadResult),
        typeof(RsaKeyManagementResult),
        typeof(SecurityEventTokenValidationPolicy),
        typeof(SelectiveDisclosure),
        typeof(SelectiveDisclosureJwtResult),
        typeof(SelectiveDisclosureJwtValidationPolicy),
        typeof(TokenHeaderInspection),
        typeof(TokenHeaderProtection),
        typeof(TokenHeaderRole),
        typeof(TokenInspectionResult),
        typeof(TokenKindResult),
        typeof(TokenValidationDiagnostic),
        typeof(TokenValidationDiagnosticStage),
        typeof(TokenValidationResult)
    };

    [Test]
    public void CompletionContractsAreExportedByTheLibrary()
    {
        Assert.Multiple(() =>
        {
            foreach (Type type in CompletionContractTypes)
            {
                Assert.That(type.IsPublic, Is.True, type.FullName);
                Assert.That(type.Namespace, Is.EqualTo("token"), type.FullName);
            }
        });
    }

    [Test]
    public void SerializationContractsStartWithoutAuthenticatedContent()
    {
        JwsCompactToken compactJws = new JwsCompactToken();
        JwsGeneralToken generalJws = new JwsGeneralToken();
        JweCompactToken compactJwe = new JweCompactToken();
        JweGeneralToken generalJwe = new JweGeneralToken();

        Assert.Multiple(() =>
        {
            Assert.That(compactJws.Kind, Is.EqualTo(TokenKind.JwsCompact));
            Assert.That(compactJws.PayloadAvailable, Is.False);
            Assert.That(compactJws.SignatureVerified, Is.False);
            Assert.That(generalJws.Signatures, Is.Empty);
            Assert.That(generalJws.PayloadAvailable, Is.False);
            Assert.That(compactJwe.Kind, Is.EqualTo(TokenKind.JweCompact));
            Assert.That(compactJwe.PlaintextAvailable, Is.False);
            Assert.That(compactJwe.AuthenticationTagVerified, Is.False);
            Assert.That(generalJwe.Recipients, Is.Empty);
            Assert.That(generalJwe.PlaintextAvailable, Is.False);
            Assert.That(generalJwe.AuthenticationTagVerified, Is.False);
        });
    }

    [Test]
    public void UniversalContractsStartEmptyAndUnauthenticated()
    {
        TokenInspectionResult inspection = new TokenInspectionResult();
        TokenValidationResult validation = new TokenValidationResult();
        TokenValidationDiagnostic diagnostic = new TokenValidationDiagnostic();

        Assert.Multiple(() =>
        {
            Assert.That(inspection.Success, Is.False);
            Assert.That(inspection.Kind, Is.EqualTo(TokenKind.Unknown));
            Assert.That(inspection.Headers, Is.Empty);
            Assert.That(inspection.HeadersAuthenticated, Is.False);
            Assert.That(inspection.ClaimsAuthenticated, Is.False);
            Assert.That(validation.Success, Is.False);
            Assert.That(validation.Diagnostics, Is.Empty);
            Assert.That(validation.Headers, Is.Empty);
            Assert.That(validation.SignatureValidated, Is.False);
            Assert.That(validation.EncryptionValidated, Is.False);
            Assert.That(diagnostic.Stage,
                Is.EqualTo(TokenValidationDiagnosticStage.Validation));
            Assert.That(diagnostic.ParticipantIndex, Is.EqualTo(-1));
        });
    }

    [Test]
    public void EveryParameterlessDataContractInitializesNonNullableFields()
    {
        NullabilityInfoContext nullability =
            new NullabilityInfoContext();
        Type[] types = typeof(TokenResult).Assembly.GetExportedTypes();

        Assert.Multiple(() =>
        {
            foreach (Type type in types)
            {
                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Public | BindingFlags.Instance);
                ConstructorInfo? constructor = type.GetConstructor(
                    Type.EmptyTypes);
                if (!type.IsClass
                    || type.IsAbstract
                    || fields.Length == 0
                    || constructor == null)
                {
                    continue;
                }

                object instance = constructor.Invoke(null);
                foreach (FieldInfo field in fields)
                {
                    if (!field.FieldType.IsValueType
                        && nullability.Create(field).ReadState
                            == NullabilityState.NotNull)
                    {
                        Assert.That(
                            field.GetValue(instance),
                            Is.Not.Null,
                            type.Name + "." + field.Name);
                    }
                }
            }
        });
    }

    [Test]
    public void EveryParameterlessResultStartsAsAnEmptyFailure()
    {
        Type[] types = typeof(TokenResult).Assembly.GetExportedTypes();

        Assert.Multiple(() =>
        {
            foreach (Type type in types)
            {
                FieldInfo? success = type.GetField("Success");
                ConstructorInfo? constructor = type.GetConstructor(
                    Type.EmptyTypes);
                if (success == null
                    || success.FieldType != typeof(bool)
                    || constructor == null)
                {
                    continue;
                }

                object instance = constructor.Invoke(null);
                Assert.That(
                    success.GetValue(instance),
                    Is.EqualTo(false),
                    type.Name + ".Success");
                FieldInfo? error = type.GetField("Error");
                if (error != null)
                {
                    Assert.That(
                        error.GetValue(instance),
                        Is.Null,
                        type.Name + ".Error");
                }
            }
        });
    }

    [Test]
    public void EveryPublicEnumDefinesItsDefaultValue()
    {
        Type[] types = typeof(TokenResult).Assembly.GetExportedTypes();

        Assert.Multiple(() =>
        {
            foreach (Type type in types)
            {
                if (type.IsEnum)
                {
                    Assert.That(
                        Enum.IsDefined(type, Activator.CreateInstance(type)!),
                        Is.True,
                        type.Name);
                }
            }
        });
    }

    [Test]
    public void EveryPublicConstantRegistryValueIsReadable()
    {
        Type[] types = typeof(TokenResult).Assembly.GetExportedTypes();
        int inspected = 0;

        Assert.Multiple(() =>
        {
            foreach (Type type in types)
            {
                if (!type.IsAbstract || !type.IsSealed)
                {
                    continue;
                }

                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Public | BindingFlags.Static);
                foreach (FieldInfo field in fields)
                {
                    inspected += 1;
                    object? value = field.GetValue(null);
                    Assert.That(
                        value,
                        Is.Not.Null,
                        type.Name + "." + field.Name);
                    if (value is string text)
                    {
                        Assert.That(
                            text,
                            Is.Not.Empty,
                            type.Name + "." + field.Name);
                    }
                }
            }
        });

        Assert.That(inspected, Is.GreaterThan(0));
    }

    [Test]
    public void PreviouslyUnreferencedPublicContractsAreUsable()
    {
        ICoseEncStructureEncoder encoder =
            new CoseEncStructureEncoder();
        TokenBytesResult encoded = encoder.Encode(
            CoseEncStructureContext.Encrypt0,
            Array.Empty<byte>(),
            new byte[] { 1, 2 },
            new TokenPolicy());
        MethodInfo[] codecMethods = typeof(ITokenCodecOperations)
            .GetMethods();
        ITokenCodecOperations codec = new CodecContractProbe();
        JoseCompactScanResult scanned = codec.ScanJoseCompact(
            "e30.e30.",
            new TokenPolicy());
        JoseCompactParseResult parsed = codec.ParseJoseCompact(
            "e30.e30.",
            new TokenPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Error, Is.Null);
            Assert.That(encoded.Bytes, Is.Not.Empty);
            Assert.That(codecMethods, Has.Length.EqualTo(6));
            Assert.That(scanned.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(CoseDirectKeyDistribution.MaximumKeySizeInBits,
                Is.EqualTo(65536));
            Assert.That(RsaKeyImporter.MinimumKeySizeInBits,
                Is.EqualTo(2048));
            Assert.That(default(CoseEncStructureContext),
                Is.EqualTo(CoseEncStructureContext.Unknown));
            Assert.That(
                () => encoder.Encode(
                    CoseEncStructureContext.Encrypt,
                    null!,
                    Array.Empty<byte>(),
                    new TokenPolicy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => encoder.Encode(
                    CoseEncStructureContext.Unknown,
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    new TokenPolicy()),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    class CodecContractProbe : ITokenCodecOperations
    {
        public CborMapSortResult SortCborMap(
            CborMapEntry[] entries)
        {
            return CborMapSorter.Sort(entries);
        }

        public PemReadResult ReadPemBlock(string text)
        {
            return PemCodec.Read(text);
        }

        public CoseKeyResult ReadCoseKey(
            byte[] cbor,
            TokenPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException("policy");
            }

            return CoseKey.Read(cbor);
        }

        public JoseCompactScanResult ScanJoseCompact(
            string text,
            TokenPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException("policy");
            }

            return JoseCompact.Scan(text);
        }

        public JoseCompactParseResult ParseJoseCompact(
            string text,
            TokenPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException("policy");
            }

            return JoseCompact.Parse(text);
        }

        public TokenKindResult DetectTextToken(
            string text,
            TokenPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException("policy");
            }

            return TokenDetector.Detect(text);
        }
    }
}
