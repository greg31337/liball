using System.Collections.Generic;

namespace dns;

internal class DnsZoneDelegation
{
    internal DnsZoneNode Node = new DnsZoneNode();
    internal List<DnsName> NameServers = new List<DnsName>();
}

internal class DnsZoneValidationState
{
    internal DnsZone Zone = new DnsZone();
    internal DnsZoneValidationResult Result = new DnsZoneValidationResult();
    internal List<DnsZoneNode> Nodes = new List<DnsZoneNode>();
    internal List<DnsZoneNode> VisitedNodes = new List<DnsZoneNode>();
    internal List<DnsZoneNode> DnameNodes = new List<DnsZoneNode>();
    internal List<DnsZoneDelegation> Delegations =
        new List<DnsZoneDelegation>();
    internal int ZoneClass;
    internal int ApexSoaCount;
    internal int ApexNameServerCount;
    internal bool HasApexSerial;
    internal uint ApexSerial;
}

internal static class DnsZoneValidator
{
    internal static DnsZoneValidationResult ValidateZone(DnsZone zone)
    {
        DnsZoneValidationState state = new DnsZoneValidationState();
        state.Zone = zone;
        if (zone == null)
        {
            AddZoneDiagnostic(state, "A zone value is required.");
            return CompleteValidation(state);
        }

        state.ZoneClass = DnsHeaderCodec.ResolveCode(
            zone.ClassValue, (int)zone.Class);
        if (state.ZoneClass < 0 || state.ZoneClass > 65535)
            AddZoneDiagnostic(state, "The zone class must fit in sixteen bits.");

        if (!IsAbsoluteName(zone.Apex))
            AddNameDiagnostic(
                state,
                zone.Apex,
                "The zone apex must be a valid absolute DNS name.");

        if (zone.Nodes == null)
        {
            AddZoneDiagnostic(state, "The zone node collection is required.");
            return CompleteValidation(state);
        }

        int index = 0;
        while (index < zone.Nodes.Count)
        {
            CollectNode(state, zone.Nodes[index]);
            index = index + 1;
        }

        ValidateDuplicateNames(state);
        index = 0;
        while (index < state.Nodes.Count)
        {
            ValidateNode(state, state.Nodes[index]);
            index = index + 1;
        }

        ValidateApex(state);
        ValidateDnameDescendants(state);
        ValidateDelegations(state);
        ValidateJournal(state);
        return CompleteValidation(state);
    }

    static void CollectNode(
        DnsZoneValidationState state,
        DnsZoneNode node)
    {
        if (node == null)
        {
            AddZoneDiagnostic(state, "Zone node collections cannot contain null values.");
            return;
        }

        if (ContainsNodeReference(state.VisitedNodes, node))
            return;

        state.VisitedNodes.Add(node);
        state.Nodes.Add(node);
        if (node.Children == null)
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "A zone node child collection is required.");
            return;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            CollectNode(state, node.Children[index]);
            index = index + 1;
        }
    }

    static bool ContainsNodeReference(
        List<DnsZoneNode> nodes,
        DnsZoneNode candidate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], candidate))
                return true;
            index = index + 1;
        }

        return false;
    }

    static void ValidateDuplicateNames(DnsZoneValidationState state)
    {
        int leftIndex = 0;
        while (leftIndex < state.Nodes.Count)
        {
            int rightIndex = leftIndex + 1;
            while (rightIndex < state.Nodes.Count)
            {
                if (NamesEqual(
                    state.Nodes[leftIndex].Name,
                    state.Nodes[rightIndex].Name))
                {
                    AddNameDiagnostic(
                        state,
                        state.Nodes[rightIndex].Name,
                        "Each owner name must have exactly one zone node.");
                }

                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }
    }

    static void ValidateNode(
        DnsZoneValidationState state,
        DnsZoneNode node)
    {
        bool validName = IsAbsoluteName(node.Name);
        if (!validName)
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "Every zone owner must be a valid absolute DNS name.");
        }
        else if (!HasNameSuffix(node.Name, state.Zone.Apex))
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "Every zone owner must be at or below the zone apex.");
        }

        if (node.RecordSets == null)
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "A zone node record-set collection is required.");
            return;
        }

        if (node.IsEmptyNonTerminal && node.RecordSets.Count != 0)
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "An empty non-terminal cannot contain resource records.");
        }

        bool isApex = validName && NamesEqual(node.Name, state.Zone.Apex);
        int cnameSetCount = CountSetsOfType(node, 5);
        int dnameSetCount = CountSetsOfType(node, 39);
        int nameServerSetCount = CountSetsOfType(node, 2);
        if (cnameSetCount > 0)
            ValidateCnameNode(state, node);
        if (dnameSetCount > 0)
        {
            ValidateDnameNode(state, node, isApex, nameServerSetCount);
            state.DnameNodes.Add(node);
        }

        if (!isApex && nameServerSetCount > 0)
        {
            DnsZoneDelegation delegation = new DnsZoneDelegation();
            delegation.Node = node;
            node.IsDelegation = true;
            state.Delegations.Add(delegation);
        }
        else if (node.IsDelegation && (isApex || nameServerSetCount == 0))
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "A delegation point below the apex must contain an NS RRset.");
        }

        ValidateRecordSets(state, node, isApex);
        if (!isApex && nameServerSetCount > 0)
            CollectDelegationTargets(
                state,
                state.Delegations[state.Delegations.Count - 1]);
    }

    static int CountSetsOfType(DnsZoneNode node, int typeValue)
    {
        int count = 0;
        if (node.RecordSets == null)
            return count;

        int index = 0;
        while (index < node.RecordSets.Count)
        {
            DnsResourceRecordSet set = node.RecordSets[index];
            if (set != null && EffectiveSetType(set) == typeValue)
                count = count + 1;
            index = index + 1;
        }

        return count;
    }

    static void ValidateCnameNode(
        DnsZoneValidationState state,
        DnsZoneNode node)
    {
        int index = 0;
        while (index < node.RecordSets.Count)
        {
            DnsResourceRecordSet set = node.RecordSets[index];
            if (set != null)
            {
                int typeValue = EffectiveSetType(set);
                if (typeValue == 5)
                {
                    if (set.Records == null || set.Records.Count != 1)
                    {
                        AddNameDiagnostic(
                            state,
                            node.Name,
                            "A CNAME RRset must contain exactly one record.");
                    }
                }
                else if (typeValue != 46 && typeValue != 47)
                {
                    AddNameDiagnostic(
                        state,
                        node.Name,
                        "CNAME cannot coexist with other data at its owner name.");
                }
            }

            index = index + 1;
        }
    }

    static void ValidateDnameNode(
        DnsZoneValidationState state,
        DnsZoneNode node,
        bool isApex,
        int nameServerSetCount)
    {
        int index = 0;
        while (index < node.RecordSets.Count)
        {
            DnsResourceRecordSet set = node.RecordSets[index];
            if (set != null && EffectiveSetType(set) == 39 &&
                (set.Records == null || set.Records.Count != 1))
            {
                AddNameDiagnostic(
                    state,
                    node.Name,
                    "A DNAME RRset must contain exactly one record.");
            }

            index = index + 1;
        }

        if (CountSetsOfType(node, 5) > 0)
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "DNAME and CNAME cannot coexist at the same owner name.");
        }

        if (!isApex && nameServerSetCount > 0)
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "DNAME and NS cannot coexist below the zone apex.");
        }
    }

    static void ValidateRecordSets(
        DnsZoneValidationState state,
        DnsZoneNode node,
        bool isApex)
    {
        int setIndex = 0;
        while (setIndex < node.RecordSets.Count)
        {
            DnsResourceRecordSet set = node.RecordSets[setIndex];
            if (set == null)
            {
                AddNameDiagnostic(
                    state,
                    node.Name,
                    "Record-set collections cannot contain null values.");
            }
            else
            {
                ValidateDuplicateSet(state, node, set, setIndex);
                ValidateRecordSet(state, node, set, isApex);
            }

            setIndex = setIndex + 1;
        }
    }

    static void ValidateDuplicateSet(
        DnsZoneValidationState state,
        DnsZoneNode node,
        DnsResourceRecordSet set,
        int setIndex)
    {
        int previousIndex = 0;
        while (previousIndex < setIndex)
        {
            DnsResourceRecordSet previous = node.RecordSets[previousIndex];
            if (previous != null &&
                EffectiveSetType(previous) == EffectiveSetType(set) &&
                EffectiveSetClass(previous) == EffectiveSetClass(set))
            {
                AddNameDiagnostic(
                    state,
                    node.Name,
                    "Records with the same owner, class, and type must share one RRset.");
                return;
            }

            previousIndex = previousIndex + 1;
        }
    }

    static void ValidateRecordSet(
        DnsZoneValidationState state,
        DnsZoneNode node,
        DnsResourceRecordSet set,
        bool isApex)
    {
        int typeValue = EffectiveSetType(set);
        int classValue = EffectiveSetClass(set);
        if (!NamesEqual(set.Name, node.Name))
        {
            AddNameDiagnostic(
                state,
                set.Name,
                "An RRset owner must match its zone node owner.");
        }

        if (typeValue < 0 || typeValue > 65535 ||
            classValue < 0 || classValue > 65535)
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "RRset type and class values must fit in sixteen bits.");
        }
        else if (classValue != state.ZoneClass)
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "Every RRset class must match the zone class.");
        }

        if (set.Records == null || set.Records.Count == 0)
        {
            AddNameDiagnostic(
                state,
                node.Name,
                "An RRset must contain at least one resource record.");
            return;
        }

        if (typeValue == 6)
        {
            if (!isApex)
                AddNameDiagnostic(
                    state,
                    node.Name,
                    "SOA records may occur only at the zone apex.");
        }

        int recordIndex = 0;
        while (recordIndex < set.Records.Count)
        {
            DnsResourceRecord record = set.Records[recordIndex];
            if (record == null)
            {
                AddNameDiagnostic(
                    state,
                    node.Name,
                    "RRsets cannot contain null resource records.");
            }
            else
            {
                ValidateRecord(state, node, set, record, typeValue, classValue);
                if (isApex && typeValue == 6)
                {
                    state.ApexSoaCount = state.ApexSoaCount + 1;
                    if (record.Data != null && record.Data.Soa != null)
                    {
                        state.HasApexSerial = true;
                        state.ApexSerial = record.Data.Soa.Serial;
                    }
                }
                else if (isApex && typeValue == 2)
                    state.ApexNameServerCount =
                        state.ApexNameServerCount + 1;
            }

            recordIndex = recordIndex + 1;
        }
    }

    static void ValidateRecord(
        DnsZoneValidationState state,
        DnsZoneNode node,
        DnsResourceRecordSet set,
        DnsResourceRecord record,
        int setType,
        int setClass)
    {
        int recordType = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue, (int)record.RecordType);
        int recordClass = DnsHeaderCodec.ResolveCode(
            record.ClassValue, (int)record.Class);
        if (!NamesEqual(record.Name, node.Name) ||
            !NamesEqual(record.Name, set.Name))
        {
            AddRecordDiagnostic(
                state,
                node.Name,
                record,
                "A resource-record owner must match its RRset and zone node.");
        }

        if (recordType != setType || recordClass != setClass)
        {
            AddRecordDiagnostic(
                state,
                node.Name,
                record,
                "A resource-record type and class must match its RRset.");
        }

        if (recordClass != state.ZoneClass)
        {
            AddRecordDiagnostic(
                state,
                node.Name,
                record,
                "Every resource-record class must match the zone class.");
        }

        if (record.TimeToLive != set.TimeToLive)
        {
            AddRecordDiagnostic(
                state,
                node.Name,
                record,
                "Every resource record in an RRset must have the same TTL.");
        }

        if (recordType == 2 || recordType == 5 || recordType == 39)
            ValidateTargetName(state, node, record);
        if (recordType == 6 && (record.Data == null || record.Data.Soa == null))
        {
            AddRecordDiagnostic(
                state,
                node.Name,
                record,
                "An SOA record requires typed SOA data.");
        }
    }

    static void ValidateTargetName(
        DnsZoneValidationState state,
        DnsZoneNode node,
        DnsResourceRecord record)
    {
        if (record.Data == null || record.Data.Name == null ||
            !IsAbsoluteName(record.Data.Name.Name))
        {
            AddRecordDiagnostic(
                state,
                node.Name,
                record,
                "NS, CNAME, and DNAME records require an absolute target name.");
        }
    }

    static void ValidateApex(DnsZoneValidationState state)
    {
        state.Result.HasApexSoa = state.ApexSoaCount == 1;
        state.Result.HasApexNameServer = state.ApexNameServerCount > 0;
        if (!state.Result.HasApexSoa)
        {
            AddNameDiagnostic(
                state,
                state.Zone.Apex,
                "A zone must contain exactly one SOA record at its apex.");
        }

        if (!state.Result.HasApexNameServer)
        {
            AddNameDiagnostic(
                state,
                state.Zone.Apex,
                "A zone must contain at least one NS record at its apex.");
        }
    }

    static void ValidateDnameDescendants(DnsZoneValidationState state)
    {
        int dnameIndex = 0;
        while (dnameIndex < state.DnameNodes.Count)
        {
            DnsZoneNode dnameNode = state.DnameNodes[dnameIndex];
            int nodeIndex = 0;
            while (nodeIndex < state.Nodes.Count)
            {
                DnsZoneNode candidate = state.Nodes[nodeIndex];
                if (candidate.RecordSets != null &&
                    candidate.RecordSets.Count > 0 &&
                    IsAncestor(dnameNode.Name, candidate.Name))
                {
                    AddNameDiagnostic(
                        state,
                        candidate.Name,
                        "Resource records cannot occur below a DNAME owner.");
                }

                nodeIndex = nodeIndex + 1;
            }

            dnameIndex = dnameIndex + 1;
        }
    }

    static void CollectDelegationTargets(
        DnsZoneValidationState state,
        DnsZoneDelegation delegation)
    {
        int setIndex = 0;
        while (setIndex < delegation.Node.RecordSets.Count)
        {
            DnsResourceRecordSet set = delegation.Node.RecordSets[setIndex];
            if (set != null && EffectiveSetType(set) == 2 &&
                set.Records != null)
            {
                int recordIndex = 0;
                while (recordIndex < set.Records.Count)
                {
                    DnsResourceRecord record = set.Records[recordIndex];
                    if (record != null && record.Data != null &&
                        record.Data.Name != null &&
                        IsAbsoluteName(record.Data.Name.Name))
                    {
                        delegation.NameServers.Add(record.Data.Name.Name);
                    }

                    recordIndex = recordIndex + 1;
                }
            }

            setIndex = setIndex + 1;
        }
    }

    static void ValidateDelegations(DnsZoneValidationState state)
    {
        int delegationIndex = 0;
        while (delegationIndex < state.Delegations.Count)
        {
            DnsZoneDelegation delegation = state.Delegations[delegationIndex];
            ValidateDelegationData(state, delegation);
            ValidateRequiredGlue(state, delegation);
            delegationIndex = delegationIndex + 1;
        }
    }

    static void ValidateDelegationData(
        DnsZoneValidationState state,
        DnsZoneDelegation delegation)
    {
        int nodeIndex = 0;
        while (nodeIndex < state.Nodes.Count)
        {
            DnsZoneNode node = state.Nodes[nodeIndex];
            bool atCut = NamesEqual(node.Name, delegation.Node.Name);
            bool belowCut = IsAncestor(delegation.Node.Name, node.Name);
            if ((atCut || belowCut) && node.RecordSets != null)
            {
                int setIndex = 0;
                while (setIndex < node.RecordSets.Count)
                {
                    DnsResourceRecordSet set = node.RecordSets[setIndex];
                    if (set != null && !IsPermittedDelegationSet(
                        delegation, node, set, atCut))
                    {
                        AddNameDiagnostic(
                            state,
                            node.Name,
                            "Data at or below a delegation must be delegation data or required address glue.");
                    }

                    setIndex = setIndex + 1;
                }
            }

            nodeIndex = nodeIndex + 1;
        }
    }

    static bool IsPermittedDelegationSet(
        DnsZoneDelegation delegation,
        DnsZoneNode node,
        DnsResourceRecordSet set,
        bool atCut)
    {
        int typeValue = EffectiveSetType(set);
        if (atCut && (typeValue == 2 || typeValue == 43 ||
            typeValue == 46 || typeValue == 47))
        {
            return true;
        }

        return (typeValue == 1 || typeValue == 28) &&
            ContainsName(delegation.NameServers, node.Name);
    }

    static void ValidateRequiredGlue(
        DnsZoneValidationState state,
        DnsZoneDelegation delegation)
    {
        int targetIndex = 0;
        while (targetIndex < delegation.NameServers.Count)
        {
            DnsName target = delegation.NameServers[targetIndex];
            if (HasNameSuffix(target, delegation.Node.Name) &&
                !HasAddressRecords(state.Nodes, target))
            {
                AddNameDiagnostic(
                    state,
                    target,
                    "An in-bailiwick delegation name server requires A or AAAA glue.");
            }

            targetIndex = targetIndex + 1;
        }
    }

    static bool HasAddressRecords(
        List<DnsZoneNode> nodes,
        DnsName name)
    {
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            DnsZoneNode node = nodes[nodeIndex];
            if (NamesEqual(node.Name, name) && node.RecordSets != null)
            {
                int setIndex = 0;
                while (setIndex < node.RecordSets.Count)
                {
                    DnsResourceRecordSet set = node.RecordSets[setIndex];
                    if (set != null && set.Records != null &&
                        set.Records.Count > 0 &&
                        (EffectiveSetType(set) == 1 ||
                        EffectiveSetType(set) == 28))
                    {
                        return true;
                    }

                    setIndex = setIndex + 1;
                }
            }

            nodeIndex = nodeIndex + 1;
        }

        return false;
    }

    static bool ContainsName(List<DnsName> names, DnsName candidate)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (NamesEqual(names[index], candidate))
                return true;
            index = index + 1;
        }

        return false;
    }

    static void ValidateJournal(DnsZoneValidationState state)
    {
        if (state.Zone.JournalEntries == null)
        {
            AddZoneDiagnostic(state, "The zone journal collection is required.");
            return;
        }

        int index = 0;
        while (index < state.Zone.JournalEntries.Count)
        {
            DnsZoneJournalEntry entry = state.Zone.JournalEntries[index];
            if (entry == null)
            {
                AddZoneDiagnostic(state, "Zone journals cannot contain null entries.");
            }
            else
            {
                ValidateJournalEntry(state, entry, index);
            }

            index = index + 1;
        }

        if (state.Zone.JournalEntries.Count > 0 && state.HasApexSerial)
        {
            DnsZoneJournalEntry last =
                state.Zone.JournalEntries[state.Zone.JournalEntries.Count - 1];
            if (last != null && last.ToSerial != state.ApexSerial)
            {
                AddNameDiagnostic(
                    state,
                    state.Zone.Apex,
                    "The last journal serial must equal the apex SOA serial.");
            }
        }
    }

    static void ValidateJournalEntry(
        DnsZoneValidationState state,
        DnsZoneJournalEntry entry,
        int index)
    {
        DnsSerialComparison comparison =
            DnsSerialArithmetic.CompareSerials(entry.ToSerial, entry.FromSerial);
        if (!comparison.Defined || comparison.Comparison != 1)
        {
            AddNameDiagnostic(
                state,
                state.Zone.Apex,
                "Each journal transition must advance its serial by RFC 1982 arithmetic.");
        }

        if (index > 0)
        {
            DnsZoneJournalEntry previous = state.Zone.JournalEntries[index - 1];
            if (previous != null && entry.Sequence <= previous.Sequence)
            {
                AddNameDiagnostic(
                    state,
                    state.Zone.Apex,
                    "Journal sequence values must increase monotonically.");
            }

            if (previous != null && entry.FromSerial != previous.ToSerial)
            {
                AddNameDiagnostic(
                    state,
                    state.Zone.Apex,
                    "Journal serial transitions must form a continuous chain.");
            }
        }

        if (entry.Transaction == null)
        {
            AddNameDiagnostic(
                state,
                state.Zone.Apex,
                "Each journal entry requires a committed transaction.");
            return;
        }

        if (!entry.Transaction.Committed)
        {
            AddNameDiagnostic(
                state,
                state.Zone.Apex,
                "Only committed transactions may appear in a zone journal.");
        }

        if (entry.Transaction.HasExpectedSerial &&
            entry.Transaction.ExpectedSerial != entry.FromSerial)
        {
            AddNameDiagnostic(
                state,
                state.Zone.Apex,
                "A journal transaction expected serial must match its starting serial.");
        }

        if (entry.Transaction.HasResultingSerial &&
            entry.Transaction.ResultingSerial != entry.ToSerial)
        {
            AddNameDiagnostic(
                state,
                state.Zone.Apex,
                "A journal transaction resulting serial must match its ending serial.");
        }
    }

    static int EffectiveSetType(DnsResourceRecordSet set)
    {
        return DnsHeaderCodec.ResolveCode(
            set.RecordTypeValue, (int)set.RecordType);
    }

    static int EffectiveSetClass(DnsResourceRecordSet set)
    {
        return DnsHeaderCodec.ResolveCode(
            set.ClassValue, (int)set.Class);
    }

    static bool IsAbsoluteName(DnsName name)
    {
        if (name == null)
            return false;
        RawDnsNameResult result = RawDnsName.ValidateName(name);
        return result.Success && result.Name != null && result.Name.IsAbsolute;
    }

    static bool NamesEqual(DnsName left, DnsName right)
    {
        if (left == null || right == null)
            return false;
        DnsNameOperationResult result = DnsNameOperations.NamesEqual(left, right);
        return result.Success && result.BooleanValue;
    }

    static bool HasNameSuffix(DnsName name, DnsName suffix)
    {
        if (name == null || suffix == null)
            return false;
        DnsNameOperationResult result =
            DnsNameOperations.HasNameSuffix(name, suffix);
        return result.Success && result.BooleanValue;
    }

    static bool IsAncestor(DnsName ancestor, DnsName name)
    {
        if (ancestor == null || name == null)
            return false;
        DnsNameOperationResult result =
            DnsNameOperations.IsAncestorName(ancestor, name);
        return result.Success && result.BooleanValue;
    }

    static DnsZoneValidationResult CompleteValidation(
        DnsZoneValidationState state)
    {
        state.Result.Valid = state.Result.Diagnostics.Count == 0;
        return state.Result;
    }

    static void AddZoneDiagnostic(
        DnsZoneValidationState state,
        string reason)
    {
        DnsZoneDiagnostic diagnostic = new DnsZoneDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidZone;
        diagnostic.Reason = reason;
        state.Result.Diagnostics.Add(diagnostic);
    }

    static void AddNameDiagnostic(
        DnsZoneValidationState state,
        DnsName name,
        string reason)
    {
        DnsZoneDiagnostic diagnostic = new DnsZoneDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidZone;
        diagnostic.Reason = reason;
        diagnostic.Name = name;
        state.Result.Diagnostics.Add(diagnostic);
    }

    static void AddRecordDiagnostic(
        DnsZoneValidationState state,
        DnsName name,
        DnsResourceRecord record,
        string reason)
    {
        DnsZoneDiagnostic diagnostic = new DnsZoneDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidZone;
        diagnostic.Reason = reason;
        diagnostic.Name = name;
        diagnostic.Record = record;
        state.Result.Diagnostics.Add(diagnostic);
    }
}
