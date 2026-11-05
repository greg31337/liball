using System.Collections.Generic;

namespace dns;

internal class DnsCandidateSignatureResult
{
    internal bool LimitExceeded;
    internal bool Invalid;
    internal bool Unsupported;
    internal int Attempts;
    internal DnsSecurityResult? SecureResult;
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal static class DnssecRecordSetValidator
{
    internal static DnsSecurityResult ValidateRecordSet(
        DnsResourceRecordSet recordSet,
        List<DnsKeyMaterial> keys,
        List<DnsSignatureData> signatures,
        DnsSecurityPolicy policy)
    {
        if (recordSet == null || keys == null || signatures == null ||
            policy == null)
        {
            return CreateFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "validation",
                "An RRset, candidate keys, signatures, and policy are required.");
        }

        if (policy.MaximumSignatureValidations < 1 ||
            policy.ClockSkewSeconds < 0)
        {
            return CreateFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "policy",
                "DNSSEC validation limits must be positive and clock skew must be nonnegative.");
        }

        if (!policy.ValidateSignatures)
        {
            return CreateStatus(DnsSecurityStatus.Indeterminate);
        }

        int recordTypeValue = EffectiveValue(
            recordSet.RecordTypeValue,
            (int)recordSet.RecordType);
        int classValue = EffectiveValue(
            recordSet.ClassValue,
            (int)recordSet.Class);
        uint validationTime = ValidationTime(policy);
        bool applicable = false;
        bool forbidden = false;
        bool unsupported = false;
        bool invalid = false;
        int validations = 0;
        DnsSecurityResult aggregate =
            CreateStatus(DnsSecurityStatus.Indeterminate);
        int signatureIndex = 0;
        while (signatureIndex < signatures.Count)
        {
            DnsSignatureData signature = signatures[signatureIndex];
            if (signature != null &&
                EffectiveValue(
                    signature.TypeCoveredValue,
                    (int)signature.TypeCovered) == recordTypeValue)
            {
                applicable = true;
                int algorithmValue = EffectiveValue(
                    signature.AlgorithmValue,
                    (int)signature.Algorithm);
                DnsAlgorithmPolicyResult algorithmPolicy =
                    DnssecAlgorithms.ResolveAlgorithmPolicy(
                        policy,
                        algorithmValue);
                if (!algorithmPolicy.Success)
                {
                    AppendDiagnostics(
                        aggregate.Diagnostics,
                        algorithmPolicy.Diagnostics);
                    aggregate.Success = false;
                    return aggregate;
                }

                SetSignatureIdentity(
                    aggregate,
                    signature,
                    algorithmValue);
                if (algorithmPolicy.Policy ==
                    DnsAlgorithmPolicy.Forbidden)
                {
                    forbidden = true;
                    AddDiagnostic(
                        aggregate.Diagnostics,
                        DnsDiagnosticCode.UnsupportedAlgorithm,
                        "rrsig.algorithm",
                        "The RRSIG algorithm is forbidden by local policy.");
                }
                else if (algorithmPolicy.Policy ==
                    DnsAlgorithmPolicy.Unsupported)
                {
                    unsupported = true;
                    AddDiagnostic(
                        aggregate.Diagnostics,
                        DnsDiagnosticCode.UnsupportedAlgorithm,
                        "rrsig.algorithm",
                        "The RRSIG algorithm is unsupported by local policy.");
                }
                else
                {
                    DnsSignatureTimeResult time =
                        DnssecAlgorithms.CheckSignatureTime(
                            signature,
                            validationTime,
                            policy.ClockSkewSeconds);
                    if (!time.Success || !time.Valid)
                    {
                        invalid = true;
                        AppendDiagnostics(
                            aggregate.Diagnostics,
                            time.Diagnostics);
                        if (time.Success)
                        {
                            AddDiagnostic(
                                aggregate.Diagnostics,
                                DnsDiagnosticCode.InvalidRecordData,
                                "rrsig.validity",
                                "The RRSIG is outside its accepted validity interval.");
                        }
                    }
                    else
                    {
                        DnsEncodeResult signedData =
                            CanonicalDnsData.BuildRrsigSignedData(
                                recordSet,
                                signature);
                        if (!signedData.Success)
                        {
                            invalid = true;
                            AppendDiagnostics(
                                aggregate.Diagnostics,
                                signedData.Diagnostics);
                        }
                        else
                        {
                            DnsCandidateSignatureResult verified =
                                TryCandidateKeys(
                                    recordSet,
                                    keys,
                                    signature,
                                    signedData.Bytes,
                                    classValue,
                                    policy.MaximumSignatureValidations,
                                    validations);
                            validations = validations +
                                verified.Attempts;
                            if (verified.LimitExceeded)
                            {
                                DnsSecurityResult limited = CreateFailure(
                                    DnsDiagnosticCode.ResourceLimitExceeded,
                                    "policy.maximumSignatureValidations",
                                    "The DNSSEC signature-validation limit was exceeded.");
                                AppendDiagnostics(
                                    limited.Diagnostics,
                                    verified.Diagnostics);
                                return limited;
                            }

                            if (verified.SecureResult != null)
                            {
                                verified.SecureResult.ValidatedRecordSets.Add(
                                    recordSet);
                                return verified.SecureResult;
                            }

                            invalid = invalid || verified.Invalid;
                            unsupported = unsupported ||
                                verified.Unsupported;

                            AppendDiagnostics(
                                aggregate.Diagnostics,
                                verified.Diagnostics);
                        }
                    }
                }
            }

            signatureIndex = signatureIndex + 1;
        }

        if (!applicable)
        {
            aggregate.Status = DnsSecurityStatus.Insecure;
        }
        else if (forbidden || invalid ||
            (unsupported && policy.TreatUnsupportedAsBogus))
        {
            aggregate.Status = DnsSecurityStatus.Bogus;
        }
        else if (unsupported)
        {
            aggregate.Status = DnsSecurityStatus.UnsupportedAlgorithm;
        }

        if (aggregate.Status == DnsSecurityStatus.Indeterminate &&
            aggregate.Diagnostics.Count == 0)
        {
            AddDiagnostic(
                aggregate.Diagnostics,
                DnsDiagnosticCode.InvalidRecordData,
                "dnskey",
                "No candidate DNSKEY matched an applicable RRSIG.");
        }

        return aggregate;
    }

    static DnsCandidateSignatureResult TryCandidateKeys(
        DnsResourceRecordSet recordSet,
        List<DnsKeyMaterial> keys,
        DnsSignatureData signature,
        byte[] signedData,
        int classValue,
        int maximumValidations,
        int validationsBefore)
    {
        DnsCandidateSignatureResult result =
            new DnsCandidateSignatureResult();
        int algorithmValue = EffectiveValue(
            signature.AlgorithmValue,
            (int)signature.Algorithm);
        int attempts = 0;
        int keyIndex = 0;
        while (keyIndex < keys.Count)
        {
            DnsKeyMaterial material = keys[keyIndex];
            if (KeyMatches(
                    recordSet,
                    material,
                    signature,
                    algorithmValue,
                    classValue))
            {
                attempts = attempts + 1;
                if (validationsBefore + attempts > maximumValidations)
                {
                    result.LimitExceeded = true;
                    result.Attempts = attempts;
                    return result;
                }

                DnsSignatureAdapterResult verified =
                    DnssecSignatureAdapters.VerifySignature(
                        material.Key,
                        signedData,
                        signature.Signature);
                if (verified.Success && verified.Valid)
                {
                    DnsSecurityResult secure =
                        CreateStatus(DnsSecurityStatus.Secure);
                    secure.Algorithm =
                        (DnsDnssecAlgorithm)algorithmValue;
                    secure.AlgorithmValue = algorithmValue;
                    secure.HasKeyTag = true;
                    secure.KeyTag = signature.KeyTag;
                    result.Attempts = attempts;
                    result.SecureResult = secure;
                    return result;
                }

                if (!verified.Supported)
                {
                    result.Unsupported = true;
                }
                else
                {
                    result.Invalid = true;
                    if (verified.Diagnostics.Count == 0)
                    {
                        AddDiagnostic(
                            result.Diagnostics,
                            DnsDiagnosticCode.InvalidRecordData,
                            "rrsig.signature",
                            "The RRSIG cryptographic signature did not verify.");
                    }
                }

                AppendDiagnostics(
                    result.Diagnostics,
                    verified.Diagnostics);
            }

            keyIndex = keyIndex + 1;
        }

        result.Attempts = attempts;
        return result;
    }

    static bool KeyMatches(
        DnsResourceRecordSet recordSet,
        DnsKeyMaterial material,
        DnsSignatureData signature,
        int algorithmValue,
        int classValue)
    {
        if (material == null || material.Key == null ||
            material.Name == null || signature.SignerName == null ||
            EffectiveValue(material.ClassValue, (int)material.Class) !=
                classValue ||
            (material.Key.Flags & 256) == 0 ||
            !RecordSetOperations.NamesEqual(
                material.Name,
                signature.SignerName) ||
            !SignerCoversOwner(signature.SignerName, recordSet.Name) ||
            EffectiveValue(
                material.Key.AlgorithmValue,
                (int)material.Key.Algorithm) != algorithmValue)
        {
            return false;
        }

        DnsDnssecIntegerResult keyTag =
            DnssecAlgorithms.CalculateKeyTag(material.Key);
        return keyTag.Success && keyTag.Value == signature.KeyTag;
    }

    static bool SignerCoversOwner(DnsName signer, DnsName owner)
    {
        if (signer == null || owner == null ||
            !signer.IsAbsolute || !owner.IsAbsolute ||
            signer.Labels.Count > owner.Labels.Count)
        {
            return false;
        }

        int signerIndex = signer.Labels.Count - 1;
        int ownerIndex = owner.Labels.Count - 1;
        while (signerIndex >= 0)
        {
            if (!LabelsEqual(
                    signer.Labels[signerIndex],
                    owner.Labels[ownerIndex]))
            {
                return false;
            }

            signerIndex = signerIndex - 1;
            ownerIndex = ownerIndex - 1;
        }

        return true;
    }

    static bool LabelsEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            int leftValue = AsciiLower(left[index]);
            int rightValue = AsciiLower(right[index]);
            if (leftValue != rightValue)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int AsciiLower(byte value)
    {
        if (value >= 65 && value <= 90)
        {
            return value + 32;
        }

        return value;
    }

    static uint ValidationTime(DnsSecurityPolicy policy)
    {
        if (policy.HasValidationTime)
        {
            return policy.ValidationTime;
        }

        long seconds = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return (uint)seconds;
    }

    static int EffectiveValue(int rawValue, int enumValue)
    {
        if (rawValue != 0 || enumValue == 0)
        {
            return rawValue;
        }

        return enumValue;
    }

    static void SetSignatureIdentity(
        DnsSecurityResult result,
        DnsSignatureData signature,
        int algorithmValue)
    {
        result.Algorithm = (DnsDnssecAlgorithm)algorithmValue;
        result.AlgorithmValue = algorithmValue;
        result.HasKeyTag = true;
        result.KeyTag = signature.KeyTag;
    }

    static DnsSecurityResult CreateStatus(DnsSecurityStatus status)
    {
        DnsSecurityResult result = new DnsSecurityResult();
        result.Success = true;
        result.Status = status;
        return result;
    }

    static DnsSecurityResult CreateFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsSecurityResult result = new DnsSecurityResult();
        result.Status = DnsSecurityStatus.Indeterminate;
        AddDiagnostic(result.Diagnostics, code, field, reason);
        return result;
    }

    static void AddDiagnostic(
        List<DnsDiagnostic> diagnostics,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        diagnostics.Add(diagnostic);
    }

    static void AppendDiagnostics(
        List<DnsDiagnostic> destination,
        List<DnsDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
}
