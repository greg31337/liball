using System.Collections.Generic;

namespace dns;

internal class DnsDenialGenerationResult
{
    internal bool Success;
    internal List<DnsResourceRecordSet> RecordSets =
        new List<DnsResourceRecordSet>();
    internal List<DnsResourceRecord> Records =
        new List<DnsResourceRecord>();
    internal List<DnsDiagnostic> Diagnostics =
        new List<DnsDiagnostic>();
}

internal class DnsNsec3Owner
{
    internal DnsZoneNode Node = new DnsZoneNode();
    internal byte[] Hash = [];
}

internal static class DnssecSigningOperations
{
    internal static DnsSecurityResult SignDnssec(
        DnsSignatureRequest request)
    {
        DnsSecurityResult input = ValidateRequest(request);
        if (!input.Success)
        {
            return input;
        }

        DnsName signerName = SelectSignerName(request);
        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Secure);
        result.Zone = RecordSetOperations.CopyName(signerName, false);
        List<DnsResourceRecordSet> recordSets =
            new List<DnsResourceRecordSet>();
        if (request.Zone != null)
        {
            DnsZoneValidationResult validated =
                DnsZoneValidator.ValidateZone(request.Zone);
            if (!validated.Valid)
            {
                return ZoneFailure(validated);
            }

            List<DnsZoneNode> nodes = ReadAuthoritativeNodes(request.Zone);
            AddZoneRecordSets(
                recordSets,
                nodes,
                request.GenerateDenialRecords);
            if (request.GenerateDenialRecords)
            {
                DnsDenialGenerationResult denial =
                    GenerateDenialRecords(
                        request,
                        request.Zone,
                        nodes);
                if (!denial.Success)
                {
                    return FromDenialFailure(denial);
                }

                AppendRecordSets(recordSets, denial.RecordSets);
                AppendRecords(result.GeneratedRecords, denial.Records);
            }
        }
        else
        {
            AppendRecordSets(recordSets, request.RecordSets);
        }

        int signatureCount = 0;
        int setIndex = 0;
        while (setIndex < recordSets.Count)
        {
            DnsResourceRecordSet set = recordSets[setIndex];
            int typeValue = EffectiveValue(
                set.RecordTypeValue,
                (int)set.RecordType);
            if (typeValue != (int)DnsRecordType.Rrsig)
            {
                bool keySigningSet = typeValue ==
                    (int)DnsRecordType.Dnskey;
                int signaturesBefore = signatureCount;
                int keyIndex = 0;
                while (keyIndex < request.Keys.Count)
                {
                    DnsKeyMaterial key = request.Keys[keyIndex];
                    if (KeySelected(key, keySigningSet))
                    {
                        if (!KeyMatchesZone(key, signerName, set))
                        {
                            return Failure(
                                DnsSecurityStatus.Bogus,
                                DnsDiagnosticCode.InvalidRecordData,
                                "key",
                                "Every selected signing key must match the signer name and RRset class.");
                        }

                        signatureCount = signatureCount + 1;
                        if (signatureCount >
                            request.Policy.MaximumSignatureValidations)
                        {
                            return Failure(
                                DnsSecurityStatus.Indeterminate,
                                DnsDiagnosticCode.ResourceLimitExceeded,
                                "policy.maximumSignatureValidations",
                                "The DNSSEC signature-generation limit was exceeded.");
                        }

                        DnsSecurityResult signed = SignRecordSet(
                            request,
                            set,
                            signerName,
                            key);
                        if (!signed.Success)
                        {
                            return signed;
                        }

                        AppendRecords(
                            result.GeneratedRecords,
                            signed.GeneratedRecords);
                    }

                    keyIndex = keyIndex + 1;
                }

                if (signatureCount == signaturesBefore)
                {
                    return Failure(
                        DnsSecurityStatus.Bogus,
                        DnsDiagnosticCode.InvalidRecordData,
                        keySigningSet ? "keys.keySigningKey" :
                            "keys.zoneSigningKey",
                        keySigningSet ?
                            "A DNSKEY RRset requires a selected key-signing key." :
                            "A zone RRset requires a selected zone-signing key.");
                }

                result.ValidatedRecordSets.Add(set);
            }

            setIndex = setIndex + 1;
        }

        if (result.ValidatedRecordSets.Count == 0)
        {
            return Failure(
                DnsSecurityStatus.Bogus,
                DnsDiagnosticCode.InvalidRecordData,
                "recordSets",
                "At least one non-RRSIG record set is required for signing.");
        }

        result.Success = true;
        return result;
    }

    static DnsSecurityResult SignRecordSet(
        DnsSignatureRequest request,
        DnsResourceRecordSet set,
        DnsName signerName,
        DnsKeyMaterial key)
    {
        int algorithmValue = EffectiveValue(
            key.Key.AlgorithmValue,
            (int)key.Key.Algorithm);
        DnsAlgorithmPolicyResult algorithmPolicy =
            DnssecAlgorithms.ResolveAlgorithmPolicy(
                request.Policy,
                algorithmValue);
        if (!algorithmPolicy.Success)
        {
            return FromAlgorithmPolicyFailure(algorithmPolicy);
        }

        if (algorithmPolicy.Policy != DnsAlgorithmPolicy.Allowed)
        {
            DnsSecurityStatus status =
                algorithmPolicy.Policy == DnsAlgorithmPolicy.Forbidden ?
                    DnsSecurityStatus.Bogus :
                    DnsSecurityStatus.UnsupportedAlgorithm;
            return Failure(
                status,
                DnsDiagnosticCode.UnsupportedAlgorithm,
                "dnskey.algorithm",
                "The DNSSEC algorithm is not allowed for signing by local policy.");
        }

        if (!key.HasPrivateKey || key.PrivateKey == null ||
            key.PrivateKey.Length == 0)
        {
            return Failure(
                DnsSecurityStatus.Bogus,
                DnsDiagnosticCode.InvalidRecordData,
                "privateKey",
                "A selected signing key requires caller-owned private key bytes.");
        }

        DnsDnssecIntegerResult keyTag =
            DnssecAlgorithms.CalculateKeyTag(key.Key);
        if (!keyTag.Success)
        {
            return FromIntegerFailure(keyTag);
        }

        DnsSignatureData signature = new DnsSignatureData();
        int typeValue = EffectiveValue(
            set.RecordTypeValue,
            (int)set.RecordType);
        signature.TypeCovered = (DnsRecordType)typeValue;
        signature.TypeCoveredValue = typeValue;
        signature.Algorithm = (DnsDnssecAlgorithm)algorithmValue;
        signature.AlgorithmValue = algorithmValue;
        signature.Labels = SigningLabelCount(set.Name);
        signature.OriginalTimeToLive = request.HasOriginalTimeToLive ?
            request.OriginalTimeToLive : set.TimeToLive;
        SetSigningTimes(signature, request, key);
        signature.KeyTag = keyTag.Value;
        signature.SignerName = RecordSetOperations.CopyName(
            signerName,
            false);
        DnsSignatureTimeResult time =
            DnssecAlgorithms.CheckSignatureTime(
                signature,
                signature.SignatureInception,
                0);
        if (!time.Success || !time.Valid)
        {
            return FromTimeFailure(time);
        }

        DnsEncodeResult signedData =
            CanonicalDnsData.BuildRrsigSignedData(set, signature);
        if (!signedData.Success)
        {
            return FromEncodeFailure(signedData);
        }

        DnsSignatureAdapterResult signed =
            DnssecSignatureAdapters.SignSignature(
                key,
                signedData.Bytes);
        if (!signed.Success)
        {
            return FromAdapterFailure(signed);
        }

        signature.Signature = signed.Signature;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = RecordSetOperations.CopyName(set.Name, false);
        record.RecordType = DnsRecordType.Rrsig;
        record.RecordTypeValue = (int)DnsRecordType.Rrsig;
        record.Class = set.Class;
        record.ClassValue = EffectiveValue(
            set.ClassValue,
            (int)set.Class);
        record.TimeToLive = set.TimeToLive;
        record.Data = new DnsRecordData();
        record.Data.Signature = signature;
        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Secure);
        result.Success = true;
        result.Algorithm = signature.Algorithm;
        result.AlgorithmValue = algorithmValue;
        result.HasKeyTag = true;
        result.KeyTag = keyTag.Value;
        result.GeneratedRecords.Add(record);
        return result;
    }

    static DnsDenialGenerationResult GenerateDenialRecords(
        DnsSignatureRequest request,
        DnsZone zone,
        List<DnsZoneNode> nodes)
    {
        if (nodes.Count > request.Policy.MaximumProofRecords)
        {
            return DenialFailure(
                DnsDiagnosticCode.ResourceLimitExceeded,
                "policy.maximumProofRecords",
                "The denial chain exceeds the configured proof-record limit.");
        }

        uint timeToLive = ReadNegativeTimeToLive(zone, nodes);
        if (request.UseNsec3)
        {
            return GenerateNsec3Chain(
                request,
                zone,
                nodes,
                timeToLive);
        }

        return GenerateNsecChain(zone, nodes, timeToLive);
    }

    static DnsDenialGenerationResult GenerateNsecChain(
        DnsZone zone,
        List<DnsZoneNode> nodes,
        uint timeToLive)
    {
        DnsDenialGenerationResult result =
            new DnsDenialGenerationResult();
        int index = 0;
        while (index < nodes.Count)
        {
            DnsZoneNode node = nodes[index];
            DnsZoneNode next = nodes[(index + 1) % nodes.Count];
            DnsNsecData nsec = new DnsNsecData();
            nsec.NextDomainName = RecordSetOperations.CopyName(
                next.Name,
                true);
            AddNodeTypes(nsec.RecordTypeValues, node, false);
            AddType(nsec.RecordTypeValues, (int)DnsRecordType.Rrsig);
            AddType(nsec.RecordTypeValues, (int)DnsRecordType.Nsec);
            DnsRecordData data = new DnsRecordData();
            data.Nsec = nsec;
            DnsResourceRecord record = CreateRecord(
                node.Name,
                DnsRecordType.Nsec,
                (int)DnsRecordType.Nsec,
                zone,
                timeToLive,
                data);
            result.Records.Add(record);
            result.RecordSets.Add(CreateRecordSet(record));
            index = index + 1;
        }

        result.Success = true;
        return result;
    }

    static DnsDenialGenerationResult GenerateNsec3Chain(
        DnsSignatureRequest request,
        DnsZone zone,
        List<DnsZoneNode> nodes,
        uint timeToLive)
    {
        DnsNsec3ParamData? parameters = request.Nsec3Parameters;
        if (parameters == null || parameters.Salt == null ||
            parameters.Salt.Length > 255 || parameters.Flags != 0 ||
            parameters.Iterations < 0 || parameters.Iterations > 65535)
        {
            return DenialFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "nsec3Parameters",
                "NSEC3 generation requires valid zero-flag hashing parameters and salt.");
        }

        int algorithmValue = EffectiveValue(
            parameters.HashAlgorithmValue,
            (int)parameters.HashAlgorithm);
        List<DnsNsec3Owner> owners = new List<DnsNsec3Owner>();
        int index = 0;
        while (index < nodes.Count)
        {
            DnsNsec3HashResult hash = Nsec3DenialValidator.HashName(
                nodes[index].Name,
                algorithmValue,
                parameters.Iterations,
                parameters.Salt,
                request.Policy.MaximumNsec3Iterations);
            if (!hash.Success)
            {
                return FromHashFailure(hash);
            }

            DnsNsec3Owner owner = new DnsNsec3Owner();
            owner.Node = nodes[index];
            owner.Hash = hash.Hash;
            InsertNsec3Owner(owners, owner);
            if (HasHashCollision(owners))
            {
                return DenialFailure(
                    DnsDiagnosticCode.InvalidRecordData,
                    "nsec3.hash",
                    "Two zone names produced the same NSEC3 owner hash.");
            }

            index = index + 1;
        }

        DnsDenialGenerationResult result =
            new DnsDenialGenerationResult();
        DnsNsec3ParamData parameterCopy = new DnsNsec3ParamData();
        parameterCopy.HashAlgorithm =
            (DnsNsec3HashAlgorithm)algorithmValue;
        parameterCopy.HashAlgorithmValue = algorithmValue;
        parameterCopy.Flags = 0;
        parameterCopy.Iterations = parameters.Iterations;
        parameterCopy.Salt = RecordSetOperations.CopyBytes(parameters.Salt);
        DnsRecordData parameterData = new DnsRecordData();
        parameterData.Nsec3Param = parameterCopy;
        DnsResourceRecord parameterRecord = CreateRecord(
            zone.Apex,
            DnsRecordType.Nsec3Param,
            (int)DnsRecordType.Nsec3Param,
            zone,
            timeToLive,
            parameterData);
        result.Records.Add(parameterRecord);
        result.RecordSets.Add(CreateRecordSet(parameterRecord));
        index = 0;
        while (index < owners.Count)
        {
            DnsNsec3Owner owner = owners[index];
            DnsNsec3Owner next = owners[(index + 1) % owners.Count];
            DnsNsec3Data nsec3 = new DnsNsec3Data();
            nsec3.HashAlgorithm =
                (DnsNsec3HashAlgorithm)algorithmValue;
            nsec3.HashAlgorithmValue = algorithmValue;
            nsec3.Flags = 0;
            nsec3.Iterations = parameters.Iterations;
            nsec3.Salt = RecordSetOperations.CopyBytes(parameters.Salt);
            nsec3.NextHashedOwnerName = RecordSetOperations.CopyBytes(
                next.Hash);
            AddNodeTypes(nsec3.RecordTypeValues, owner.Node, true);
            if (RecordSetOperations.NamesEqual(
                    owner.Node.Name,
                    zone.Apex))
            {
                AddType(
                    nsec3.RecordTypeValues,
                    (int)DnsRecordType.Nsec3Param);
            }

            DnsRecordData data = new DnsRecordData();
            data.Nsec3 = nsec3;
            DnsName ownerName = CreateHashedOwnerName(
                owner.Hash,
                zone.Apex);
            DnsResourceRecord record = CreateRecord(
                ownerName,
                DnsRecordType.Nsec3,
                (int)DnsRecordType.Nsec3,
                zone,
                timeToLive,
                data);
            result.Records.Add(record);
            result.RecordSets.Add(CreateRecordSet(record));
            index = index + 1;
        }

        result.Success = true;
        return result;
    }

    static DnsSecurityResult ValidateRequest(DnsSignatureRequest request)
    {
        if (request == null || request.RecordSets == null ||
            request.Keys == null || request.Policy == null ||
            request.SignerName == null)
        {
            return Failure(
                DnsSecurityStatus.Bogus,
                DnsDiagnosticCode.InvalidRecordData,
                "request",
                "A complete DNSSEC signing request is required.");
        }

        bool hasZone = request.Zone != null;
        if (hasZone == (request.RecordSets.Count > 0))
        {
            return Failure(
                DnsSecurityStatus.Bogus,
                DnsDiagnosticCode.InvalidRecordData,
                "recordSets",
                "Supply either one complete zone or individual record sets for signing.");
        }

        if (request.Keys.Count == 0 ||
            request.Policy.MaximumSignatureValidations < 1 ||
            request.Policy.MaximumProofRecords < 1 ||
            request.Policy.MaximumNsec3Iterations < 0)
        {
            return Failure(
                DnsSecurityStatus.Bogus,
                DnsDiagnosticCode.ValueOutOfRange,
                "policy",
                "Signing keys and positive DNSSEC work limits are required.");
        }

        if (!hasZone && request.GenerateDenialRecords)
        {
            return Failure(
                DnsSecurityStatus.Bogus,
                DnsDiagnosticCode.InvalidRecordData,
                "zone",
                "Denial chains can only be generated from a complete zone.");
        }

        DnsName signer = SelectSignerName(request);
        RawDnsNameResult validated = RawDnsName.ValidateName(signer);
        if (!validated.Success || validated.Name == null ||
            !validated.Name.IsAbsolute)
        {
            return Failure(
                DnsSecurityStatus.Bogus,
                DnsDiagnosticCode.InvalidLabel,
                "signerName",
                "The DNSSEC signer name must be an absolute DNS name.");
        }

        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Indeterminate);
        result.Success = true;
        return result;
    }

    static DnsName SelectSignerName(DnsSignatureRequest request)
    {
        if (request.Zone != null && !request.SignerName.IsAbsolute &&
            request.SignerName.Labels.Count == 0)
        {
            return request.Zone.Apex;
        }

        return request.SignerName;
    }

    static List<DnsZoneNode> ReadAuthoritativeNodes(DnsZone zone)
    {
        List<DnsZoneNode> all = new List<DnsZoneNode>();
        int index = 0;
        while (index < zone.Nodes.Count)
        {
            AddNodeAndChildren(all, zone.Nodes[index]);
            index = index + 1;
        }

        List<DnsZoneNode> result = new List<DnsZoneNode>();
        index = 0;
        while (index < all.Count)
        {
            if (!BelowDelegation(all[index], all))
            {
                InsertNode(result, all[index]);
            }

            index = index + 1;
        }

        return result;
    }

    static void AddNodeAndChildren(
        List<DnsZoneNode> nodes,
        DnsZoneNode node)
    {
        if (ContainsNodeReference(nodes, node))
        {
            return;
        }

        nodes.Add(node);
        int index = 0;
        while (index < node.Children.Count)
        {
            AddNodeAndChildren(nodes, node.Children[index]);
            index = index + 1;
        }
    }

    static bool BelowDelegation(
        DnsZoneNode node,
        List<DnsZoneNode> nodes)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            DnsZoneNode delegation = nodes[index];
            if (delegation.IsDelegation &&
                node.Name.Labels.Count > delegation.Name.Labels.Count)
            {
                DnsNameOperationResult suffix =
                    DnsNameOperations.HasNameSuffix(
                        node.Name,
                        delegation.Name);
                if (suffix.Success && suffix.BooleanValue)
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    static bool ContainsNodeReference(
        List<DnsZoneNode> nodes,
        DnsZoneNode candidate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void InsertNode(
        List<DnsZoneNode> nodes,
        DnsZoneNode node)
    {
        int index = 0;
        while (index < nodes.Count && RecordSetOperations.CompareNames(
                nodes[index].Name,
                node.Name) < 0)
        {
            index = index + 1;
        }

        nodes.Insert(index, node);
    }

    static void AddZoneRecordSets(
        List<DnsResourceRecordSet> destination,
        List<DnsZoneNode> nodes,
        bool replacingDenial)
    {
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            DnsZoneNode node = nodes[nodeIndex];
            int setIndex = 0;
            while (setIndex < node.RecordSets.Count)
            {
                DnsResourceRecordSet set = node.RecordSets[setIndex];
                int typeValue = EffectiveValue(
                    set.RecordTypeValue,
                    (int)set.RecordType);
                if (typeValue != (int)DnsRecordType.Rrsig &&
                    (!replacingDenial ||
                     (typeValue != (int)DnsRecordType.Nsec &&
                      typeValue != (int)DnsRecordType.Nsec3 &&
                      typeValue != (int)DnsRecordType.Nsec3Param)))
                {
                    destination.Add(set);
                }

                setIndex = setIndex + 1;
            }

            nodeIndex = nodeIndex + 1;
        }
    }

    static void AddNodeTypes(
        List<int> destination,
        DnsZoneNode node,
        bool nsec3)
    {
        int index = 0;
        while (index < node.RecordSets.Count)
        {
            DnsResourceRecordSet set = node.RecordSets[index];
            int typeValue = EffectiveValue(
                set.RecordTypeValue,
                (int)set.RecordType);
            if (typeValue != (int)DnsRecordType.Rrsig &&
                typeValue != (int)DnsRecordType.Nsec &&
                typeValue != (int)DnsRecordType.Nsec3 &&
                (!nsec3 ||
                 typeValue != (int)DnsRecordType.Nsec3Param))
            {
                AddType(destination, typeValue);
            }

            index = index + 1;
        }
    }

    static void AddType(List<int> values, int typeValue)
    {
        if (typeValue < 0 || typeValue > 65535)
        {
            return;
        }

        int index = 0;
        while (index < values.Count && values[index] < typeValue)
        {
            index = index + 1;
        }

        if (index == values.Count || values[index] != typeValue)
        {
            values.Insert(index, typeValue);
        }
    }

    static uint ReadNegativeTimeToLive(
        DnsZone zone,
        List<DnsZoneNode> nodes)
    {
        uint result = zone.HasDefaultTimeToLive ?
            zone.DefaultTimeToLive : 0;
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            DnsZoneNode node = nodes[nodeIndex];
            if (RecordSetOperations.NamesEqual(node.Name, zone.Apex))
            {
                int setIndex = 0;
                while (setIndex < node.RecordSets.Count)
                {
                    DnsResourceRecordSet set = node.RecordSets[setIndex];
                    if (EffectiveValue(
                            set.RecordTypeValue,
                            (int)set.RecordType) ==
                            (int)DnsRecordType.Soa &&
                        set.Records.Count > 0)
                    {
                        result = set.TimeToLive;
                        DnsResourceRecord record = set.Records[0];
                        if (record.Data != null &&
                            record.Data.Soa != null &&
                            record.Data.Soa.Minimum < result)
                        {
                            result = record.Data.Soa.Minimum;
                        }

                        return result;
                    }

                    setIndex = setIndex + 1;
                }
            }

            nodeIndex = nodeIndex + 1;
        }

        return result;
    }

    static DnsResourceRecord CreateRecord(
        DnsName owner,
        DnsRecordType type,
        int typeValue,
        DnsZone zone,
        uint timeToLive,
        DnsRecordData data)
    {
        int classValue = EffectiveValue(
            zone.ClassValue,
            (int)zone.Class);
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = RecordSetOperations.CopyName(owner, true);
        record.RecordType = type;
        record.RecordTypeValue = typeValue;
        record.Class = (DnsClass)classValue;
        record.ClassValue = classValue;
        record.TimeToLive = timeToLive;
        record.Data = data;
        return record;
    }

    static DnsResourceRecordSet CreateRecordSet(
        DnsResourceRecord record)
    {
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = RecordSetOperations.CopyName(record.Name, true);
        set.RecordType = record.RecordType;
        set.RecordTypeValue = record.RecordTypeValue;
        set.Class = record.Class;
        set.ClassValue = record.ClassValue;
        set.TimeToLive = record.TimeToLive;
        set.Records.Add(record);
        return set;
    }

    static DnsName CreateHashedOwnerName(
        byte[] hash,
        DnsName zone)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        result.Labels.Add(System.Text.Encoding.ASCII.GetBytes(
            DnsTextEncoding.FormatBase32Hex(hash)));
        int index = 0;
        while (index < zone.Labels.Count)
        {
            result.Labels.Add(RecordSetOperations.CopyBytes(
                zone.Labels[index]));
            index = index + 1;
        }

        return result;
    }

    static void InsertNsec3Owner(
        List<DnsNsec3Owner> owners,
        DnsNsec3Owner owner)
    {
        int index = 0;
        while (index < owners.Count &&
            RecordSetOperations.CompareBytes(
                owners[index].Hash,
                owner.Hash) < 0)
        {
            index = index + 1;
        }

        owners.Insert(index, owner);
    }

    static bool HasHashCollision(List<DnsNsec3Owner> owners)
    {
        int index = 1;
        while (index < owners.Count)
        {
            if (RecordSetOperations.BytesEqual(
                    owners[index - 1].Hash,
                    owners[index].Hash))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool KeySelected(
        DnsKeyMaterial key,
        bool keySigningSet)
    {
        if (key == null)
        {
            return false;
        }

        return keySigningSet ? key.KeySigningKey : key.ZoneSigningKey;
    }

    static bool KeyMatchesZone(
        DnsKeyMaterial key,
        DnsName signerName,
        DnsResourceRecordSet set)
    {
        int keyClassValue = EffectiveValue(
            key.ClassValue,
            (int)key.Class);
        int setClassValue = EffectiveValue(
            set.ClassValue,
            (int)set.Class);
        return key.Key != null &&
            RecordSetOperations.NamesEqual(key.Name, signerName) &&
            keyClassValue == setClassValue;
    }

    static int SigningLabelCount(DnsName owner)
    {
        int count = owner.Labels.Count;
        if (count > 0 && owner.Labels[0].Length == 1 &&
            owner.Labels[0][0] == 42)
        {
            count = count - 1;
        }

        return count;
    }

    static void SetSigningTimes(
        DnsSignatureData signature,
        DnsSignatureRequest request,
        DnsKeyMaterial key)
    {
        uint now = (uint)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        uint defaultInception = now > 300 ? now - 300 : 0;
        signature.SignatureInception = request.HasInception ?
            request.Inception : key.HasInception ?
                key.Inception : defaultInception;
        signature.SignatureExpiration = request.HasExpiration ?
            request.Expiration : key.HasExpiration ?
                key.Expiration : now + 2592000;
    }

    static DnsSecurityResult CreateStatus(DnsSecurityStatus status)
    {
        DnsSecurityResult result = new DnsSecurityResult();
        result.Status = status;
        return result;
    }

    static DnsSecurityResult Failure(
        DnsSecurityStatus status,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsSecurityResult result = CreateStatus(status);
        AddDiagnostic(result.Diagnostics, code, field, reason);
        return result;
    }

    static DnsDenialGenerationResult DenialFailure(
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDenialGenerationResult result =
            new DnsDenialGenerationResult();
        AddDiagnostic(result.Diagnostics, code, field, reason);
        return result;
    }

    static DnsSecurityResult ZoneFailure(
        DnsZoneValidationResult validation)
    {
        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Bogus);
        string reason = "The complete zone is not structurally valid.";
        if (validation.Diagnostics.Count > 0)
        {
            reason = validation.Diagnostics[0].Reason;
        }

        AddDiagnostic(
            result.Diagnostics,
            DnsDiagnosticCode.InvalidRecordData,
            "zone",
            reason);
        return result;
    }

    static DnsSecurityResult FromDenialFailure(
        DnsDenialGenerationResult source)
    {
        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Bogus);
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsSecurityResult FromAlgorithmPolicyFailure(
        DnsAlgorithmPolicyResult source)
    {
        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Bogus);
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsSecurityResult FromIntegerFailure(
        DnsDnssecIntegerResult source)
    {
        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Bogus);
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsSecurityResult FromTimeFailure(
        DnsSignatureTimeResult source)
    {
        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Bogus);
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsSecurityResult FromEncodeFailure(
        DnsEncodeResult source)
    {
        DnsSecurityResult result = CreateStatus(
            DnsSecurityStatus.Bogus);
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsSecurityResult FromAdapterFailure(
        DnsSignatureAdapterResult source)
    {
        DnsSecurityResult result = CreateStatus(
            source.Supported ? DnsSecurityStatus.Bogus :
                DnsSecurityStatus.UnsupportedAlgorithm);
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static DnsDenialGenerationResult FromHashFailure(
        DnsNsec3HashResult source)
    {
        DnsDenialGenerationResult result =
            new DnsDenialGenerationResult();
        AppendDiagnostics(result.Diagnostics, source.Diagnostics);
        return result;
    }

    static void AppendRecordSets(
        List<DnsResourceRecordSet> destination,
        List<DnsResourceRecordSet> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static void AppendRecords(
        List<DnsResourceRecord> destination,
        List<DnsResourceRecord> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
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

    static int EffectiveValue(int rawValue, int enumValue)
    {
        if (rawValue != 0 || enumValue == 0)
        {
            return rawValue;
        }

        return enumValue;
    }
}
