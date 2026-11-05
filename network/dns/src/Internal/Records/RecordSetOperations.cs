using System.Collections.Generic;

namespace dns;

internal static class RecordSetOperations
{
    internal static List<DnsResourceRecordSet> GroupRecordSets(
        List<DnsResourceRecord> records)
    {
        if (records == null)
        {
            throw new System.ArgumentNullException("records");
        }

        List<DnsResourceRecordSet> sets = new List<DnsResourceRecordSet>();
        int index = 0;
        while (index < records.Count)
        {
            DnsResourceRecord record = records[index];
            if (record == null)
            {
                throw new System.ArgumentException("A record cannot be null.");
            }

            int typeValue = EffectiveValue(record.RecordTypeValue, (int)record.RecordType);
            int classValue = EffectiveValue(record.ClassValue, (int)record.Class);
            DnsResourceRecordSet? matching = null;
            int setIndex = 0;
            while (setIndex < sets.Count && matching == null)
            {
                DnsResourceRecordSet candidate = sets[setIndex];
                if (candidate.RecordTypeValue == typeValue &&
                    candidate.ClassValue == classValue &&
                    NamesEqual(candidate.Name, record.Name))
                {
                    matching = candidate;
                }

                setIndex = setIndex + 1;
            }

            if (matching == null)
            {
                matching = new DnsResourceRecordSet();
                matching.Name = CopyName(record.Name, true);
                matching.RecordType = (DnsRecordType)typeValue;
                matching.RecordTypeValue = typeValue;
                matching.Class = (DnsClass)classValue;
                matching.ClassValue = classValue;
                matching.TimeToLive = record.TimeToLive;
                sets.Add(matching);
            }

            DnsResourceRecord copy = CopyRecord(record, matching.Name);
            copy.RecordType = matching.RecordType;
            copy.RecordTypeValue = typeValue;
            copy.Class = matching.Class;
            copy.ClassValue = classValue;
            matching.Records.Add(copy);
            if (record.TimeToLive < matching.TimeToLive)
            {
                matching.TimeToLive = record.TimeToLive;
            }

            index = index + 1;
        }

        return sets;
    }

    internal static DnsResourceRecordSet NormalizeRecordSet(
        DnsResourceRecordSet recordSet,
        DnsCanonicalOrderOptions options)
    {
        if (recordSet == null || options == null || recordSet.Records == null)
        {
            throw new System.ArgumentNullException("recordSet");
        }

        int typeValue = EffectiveValue(recordSet.RecordTypeValue, (int)recordSet.RecordType);
        int classValue = EffectiveValue(recordSet.ClassValue, (int)recordSet.Class);
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = CopyName(recordSet.Name, options.LowercaseNames);
        result.RecordType = (DnsRecordType)typeValue;
        result.RecordTypeValue = typeValue;
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.TimeToLive = recordSet.TimeToLive;
        uint minimumTtl = recordSet.TimeToLive;
        int index = 0;
        while (index < recordSet.Records.Count)
        {
            DnsResourceRecord record = recordSet.Records[index];
            if (record == null ||
                EffectiveValue(record.RecordTypeValue, (int)record.RecordType) != typeValue ||
                EffectiveValue(record.ClassValue, (int)record.Class) != classValue ||
                !NamesEqual(recordSet.Name, record.Name))
            {
                throw new System.ArgumentException("A record does not belong to its record set.");
            }

            DnsResourceRecord copy = CopyRecord(record, result.Name);
            copy.RecordType = result.RecordType;
            copy.RecordTypeValue = typeValue;
            copy.Class = result.Class;
            copy.ClassValue = classValue;
            result.Records.Add(copy);
            if (index == 0 || record.TimeToLive < minimumTtl)
            {
                minimumTtl = record.TimeToLive;
            }

            index = index + 1;
        }

        if (options.NormalizeRecordTtl && result.Records.Count > 0)
        {
            result.TimeToLive = minimumTtl;
            index = 0;
            while (index < result.Records.Count)
            {
                result.Records[index].TimeToLive = minimumTtl;
                index = index + 1;
            }
        }

        if (options.SuppressDuplicateRecords)
        {
            result.Records = DeduplicateRecords(result.Records);
        }

        if (options.SortRecords)
        {
            SortRecords(result.Records);
        }

        return result;
    }

    internal static bool RecordSetsEqual(
        DnsResourceRecordSet left,
        DnsResourceRecordSet right,
        DnsCanonicalOrderOptions options)
    {
        DnsCanonicalOrderOptions equalityOptions = CopyCanonicalOptions(options);
        equalityOptions.SortRecords = true;
        DnsResourceRecordSet normalizedLeft = NormalizeRecordSet(left, equalityOptions);
        DnsResourceRecordSet normalizedRight = NormalizeRecordSet(right, equalityOptions);
        if (!SetKeysEqual(normalizedLeft, normalizedRight) ||
            normalizedLeft.TimeToLive != normalizedRight.TimeToLive ||
            normalizedLeft.Records.Count != normalizedRight.Records.Count)
        {
            return false;
        }

        int index = 0;
        while (index < normalizedLeft.Records.Count)
        {
            DnsResourceRecord leftRecord = normalizedLeft.Records[index];
            DnsResourceRecord rightRecord = normalizedRight.Records[index];
            if (!BytesEqual(leftRecord.RawData, rightRecord.RawData) ||
                leftRecord.TimeToLive != rightRecord.TimeToLive)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int CompareRecordSets(
        DnsResourceRecordSet left,
        DnsResourceRecordSet right)
    {
        int comparison = CompareNames(left.Name, right.Name);
        if (comparison != 0)
        {
            return comparison;
        }

        int leftType = EffectiveValue(left.RecordTypeValue, (int)left.RecordType);
        int rightType = EffectiveValue(right.RecordTypeValue, (int)right.RecordType);
        if (leftType != rightType)
        {
            return leftType < rightType ? -1 : 1;
        }

        int leftClass = EffectiveValue(left.ClassValue, (int)left.Class);
        int rightClass = EffectiveValue(right.ClassValue, (int)right.Class);
        if (leftClass != rightClass)
        {
            return leftClass < rightClass ? -1 : 1;
        }

        DnsCanonicalOrderOptions options = new DnsCanonicalOrderOptions();
        options.NormalizeRecordTtl = true;
        DnsResourceRecordSet normalizedLeft = NormalizeRecordSet(left, options);
        DnsResourceRecordSet normalizedRight = NormalizeRecordSet(right, options);
        int index = 0;
        while (index < normalizedLeft.Records.Count &&
            index < normalizedRight.Records.Count)
        {
            comparison = CompareBytes(
                normalizedLeft.Records[index].RawData,
                normalizedRight.Records[index].RawData);
            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        if (normalizedLeft.Records.Count == normalizedRight.Records.Count)
        {
            return 0;
        }

        return normalizedLeft.Records.Count < normalizedRight.Records.Count ? -1 : 1;
    }

    internal static DnsResourceRecordSet SuppressDuplicateRecords(
        DnsResourceRecordSet recordSet)
    {
        DnsCanonicalOrderOptions options = new DnsCanonicalOrderOptions();
        options.LowercaseNames = false;
        options.NormalizeRecordTtl = false;
        options.SortRecords = false;
        options.SuppressDuplicateRecords = true;
        return NormalizeRecordSet(recordSet, options);
    }

    internal static DnsResourceRecordSet IntersectRecordSets(
        DnsResourceRecordSet left,
        DnsResourceRecordSet right)
    {
        return SetOperation(left, right, true);
    }

    internal static DnsResourceRecordSet DifferenceRecordSets(
        DnsResourceRecordSet left,
        DnsResourceRecordSet right)
    {
        return SetOperation(left, right, false);
    }

    internal static List<DnsResourceRecordSet> SortRecordSets(
        List<DnsResourceRecordSet> recordSets)
    {
        if (recordSets == null)
        {
            throw new System.ArgumentNullException("recordSets");
        }

        List<DnsResourceRecordSet> result = new List<DnsResourceRecordSet>();
        int index = 0;
        while (index < recordSets.Count)
        {
            result.Add(recordSets[index]);
            int position = result.Count - 1;
            while (position > 0 &&
                CompareRecordSets(result[position - 1], result[position]) > 0)
            {
                DnsResourceRecordSet swap = result[position - 1];
                result[position - 1] = result[position];
                result[position] = swap;
                position = position - 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal static List<DnsResourceRecordSet> FindRecordSets(
        DnsMessage message,
        DnsMessageSection section,
        DnsName name,
        DnsRecordType recordType,
        int recordTypeValue,
        DnsClass recordClass,
        int classValue)
    {
        if (message == null)
        {
            throw new System.ArgumentNullException("message");
        }

        List<DnsResourceRecord> records = SelectSection(message, section);
        List<DnsResourceRecordSet> grouped = GroupRecordSets(records);
        List<DnsResourceRecordSet> result = new List<DnsResourceRecordSet>();
        int effectiveType = EffectiveValue(recordTypeValue, (int)recordType);
        int effectiveClass = EffectiveValue(classValue, (int)recordClass);
        int index = 0;
        while (index < grouped.Count)
        {
            DnsResourceRecordSet set = grouped[index];
            if (set.RecordTypeValue == effectiveType &&
                set.ClassValue == effectiveClass && NamesEqual(set.Name, name))
            {
                result.Add(set);
            }

            index = index + 1;
        }

        return result;
    }

    internal static DnsResolveResult ExtractAnswerChain(
        DnsMessage message,
        DnsQuestion question,
        int maximumAliasDepth)
    {
        DnsResolveResult result = new DnsResolveResult();
        result.Response = message;
        if (message == null || question == null || maximumAliasDepth < 0)
        {
            return ResolveFailure(result, DnsResolveStatus.ProtocolError,
                "An answer chain requires a message, question, and nonnegative depth.");
        }

        List<DnsResourceRecordSet> sets = GroupRecordSets(message.Answers);
        DnsName current = CopyName(question.Name, false);
        List<DnsName> visited = new List<DnsName>();
        int requestedType = EffectiveValue(question.RecordTypeValue, (int)question.RecordType);
        int requestedClass = EffectiveValue(question.ClassValue, (int)question.Class);
        int depth = 0;
        while (true)
        {
            if (ContainsName(visited, current))
            {
                result.CanonicalName = current;
                return ResolveFailure(result, DnsResolveStatus.Loop,
                    "The answer alias chain contains a loop.");
            }

            visited.Add(CopyName(current, false));
            List<DnsResourceRecordSet> answers = FindAnswerSets(
                sets, current, requestedType, requestedClass);
            if (answers.Count > 0)
            {
                result.Status = DnsResolveStatus.Success;
                result.CanonicalName = current;
                result.RecordSets = answers;
                SetResultTtl(result);
                return result;
            }

            DnsResourceRecordSet? aliasSet = FindCnameSet(sets, current, requestedClass);
            bool dname = false;
            if (aliasSet == null)
            {
                aliasSet = FindDnameSet(sets, current, requestedClass);
                dname = aliasSet != null;
            }

            if (aliasSet == null)
            {
                result.CanonicalName = current;
                result.Status = StatusFromResponse(message.Header);
                return result;
            }

            if (depth >= maximumAliasDepth || aliasSet.Records.Count != 1)
            {
                result.CanonicalName = current;
                if (depth >= maximumAliasDepth)
                {
                    return ResolveFailure(result, DnsResolveStatus.DepthLimitExceeded,
                        "The answer alias chain exceeds its configured depth.");
                }

                return ResolveFailure(result, DnsResolveStatus.ProtocolError,
                    "An alias record set must contain exactly one record.");
            }

            DnsResourceRecord aliasRecord = aliasSet.Records[0];
            DnsName? target = ReadAliasTarget(aliasRecord);
            if (target == null || !target.IsAbsolute)
            {
                result.CanonicalName = current;
                return ResolveFailure(result, DnsResolveStatus.ProtocolError,
                    "An alias record requires an absolute typed target name.");
            }

            DnsName next = CopyName(target, false);
            if (dname)
            {
                DnsNameOperationResult relative = DnsNameOperations.RelativizeName(
                    current, aliasSet.Name);
                if (!relative.Success || relative.Name == null)
                {
                    return ResolveFailure(result, DnsResolveStatus.ProtocolError,
                        "A DNAME owner is not a strict ancestor of the current name.");
                }

                DnsNameOperationResult combined = DnsNameOperations.ConcatenateNames(
                    relative.Name, target);
                if (!combined.Success || combined.Name == null)
                {
                    return ResolveFailure(result, DnsResolveStatus.ProtocolError,
                        "A synthesized DNAME target is invalid or too long.");
                }

                next = combined.Name;
            }

            DnsAliasStep step = new DnsAliasStep();
            step.Owner = CopyName(current, false);
            step.Target = CopyName(next, false);
            step.RecordType = aliasSet.RecordType;
            step.RecordTypeValue = aliasSet.RecordTypeValue;
            step.Synthesized = dname;
            step.Record = aliasRecord;
            result.AliasSteps.Add(step);
            UpdateResultTtl(result, aliasSet.TimeToLive);
            current = next;
            depth = depth + 1;
        }
    }

    internal static DnsResourceRecordSet SetOperation(
        DnsResourceRecordSet left,
        DnsResourceRecordSet right,
        bool intersection)
    {
        DnsCanonicalOrderOptions options = new DnsCanonicalOrderOptions();
        options.NormalizeRecordTtl = true;
        DnsResourceRecordSet normalizedLeft = NormalizeRecordSet(left, options);
        DnsResourceRecordSet normalizedRight = NormalizeRecordSet(right, options);
        DnsResourceRecordSet result = CopySetMetadata(normalizedLeft);
        if (!SetKeysEqual(normalizedLeft, normalizedRight))
        {
            if (!intersection)
            {
                result.Records = normalizedLeft.Records;
            }

            return result;
        }

        int index = 0;
        while (index < normalizedLeft.Records.Count)
        {
            bool contained = ContainsRecord(
                normalizedRight.Records, normalizedLeft.Records[index]);
            if (contained == intersection)
            {
                result.Records.Add(normalizedLeft.Records[index]);
            }

            index = index + 1;
        }

        return result;
    }

    internal static List<DnsResourceRecord> DeduplicateRecords(
        List<DnsResourceRecord> records)
    {
        List<DnsResourceRecord> result = new List<DnsResourceRecord>();
        int index = 0;
        while (index < records.Count)
        {
            if (!ContainsRecord(result, records[index]))
            {
                result.Add(records[index]);
            }

            index = index + 1;
        }

        return result;
    }

    internal static void SortRecords(List<DnsResourceRecord> records)
    {
        int index = 1;
        while (index < records.Count)
        {
            int position = index;
            while (position > 0 &&
                CompareBytes(records[position - 1].RawData,
                    records[position].RawData) > 0)
            {
                DnsResourceRecord swap = records[position - 1];
                records[position - 1] = records[position];
                records[position] = swap;
                position = position - 1;
            }

            index = index + 1;
        }
    }

    internal static List<DnsResourceRecord> SelectSection(
        DnsMessage message,
        DnsMessageSection section)
    {
        if (section == DnsMessageSection.Answer)
        {
            return message.Answers;
        }

        if (section == DnsMessageSection.Authority)
        {
            return message.Authorities;
        }

        if (section == DnsMessageSection.Additional)
        {
            return message.Additionals;
        }

        return new List<DnsResourceRecord>();
    }

    internal static List<DnsResourceRecordSet> FindAnswerSets(
        List<DnsResourceRecordSet> sets,
        DnsName name,
        int requestedType,
        int requestedClass)
    {
        List<DnsResourceRecordSet> result = new List<DnsResourceRecordSet>();
        int index = 0;
        while (index < sets.Count)
        {
            DnsResourceRecordSet set = sets[index];
            bool typeMatches = set.RecordTypeValue == requestedType || requestedType == 255;
            if (typeMatches && set.ClassValue == requestedClass && NamesEqual(set.Name, name))
            {
                result.Add(set);
            }

            index = index + 1;
        }

        return result;
    }

    internal static DnsResourceRecordSet? FindCnameSet(
        List<DnsResourceRecordSet> sets,
        DnsName name,
        int classValue)
    {
        int index = 0;
        while (index < sets.Count)
        {
            DnsResourceRecordSet set = sets[index];
            if (set.RecordTypeValue == 5 && set.ClassValue == classValue &&
                NamesEqual(set.Name, name))
            {
                return set;
            }

            index = index + 1;
        }

        return null;
    }

    internal static DnsResourceRecordSet? FindDnameSet(
        List<DnsResourceRecordSet> sets,
        DnsName name,
        int classValue)
    {
        DnsResourceRecordSet? result = null;
        int resultLabels = -1;
        int index = 0;
        while (index < sets.Count)
        {
            DnsResourceRecordSet set = sets[index];
            DnsNameOperationResult ancestor = DnsNameOperations.IsAncestorName(
                set.Name, name);
            if (set.RecordTypeValue == 39 && set.ClassValue == classValue &&
                ancestor.Success && ancestor.BooleanValue &&
                set.Name.Labels.Count > resultLabels)
            {
                result = set;
                resultLabels = set.Name.Labels.Count;
            }

            index = index + 1;
        }

        return result;
    }

    internal static DnsName? ReadAliasTarget(DnsResourceRecord record)
    {
        if (record.Data == null || record.Data.Name == null)
        {
            return null;
        }

        return record.Data.Name.Name;
    }

    internal static DnsResolveStatus StatusFromResponse(DnsMessageHeader header)
    {
        int code = EffectiveValue(header.ResponseCodeValue, (int)header.ResponseCode);
        if (code == 3)
        {
            return DnsResolveStatus.NameError;
        }

        if (code == 2)
        {
            return DnsResolveStatus.ServerFailure;
        }

        if (code == 5)
        {
            return DnsResolveStatus.Refused;
        }

        if (code == 1)
        {
            return DnsResolveStatus.ProtocolError;
        }

        if (code == 4)
        {
            return DnsResolveStatus.Unsupported;
        }

        return DnsResolveStatus.NoData;
    }

    internal static DnsResolveResult ResolveFailure(
        DnsResolveResult result,
        DnsResolveStatus status,
        string reason)
    {
        result.Status = status;
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.ResponseMismatch;
        diagnostic.Field = "answerChain";
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    internal static void SetResultTtl(DnsResolveResult result)
    {
        int index = 0;
        while (index < result.RecordSets.Count)
        {
            uint ttl = result.RecordSets[index].TimeToLive;
            UpdateResultTtl(result, ttl);

            index = index + 1;
        }
    }

    internal static void UpdateResultTtl(DnsResolveResult result, uint ttl)
    {
        if (!result.HasTimeToLive || ttl < result.TimeToLive)
        {
            result.HasTimeToLive = true;
            result.TimeToLive = ttl;
        }
    }

    internal static DnsCanonicalOrderOptions CopyCanonicalOptions(
        DnsCanonicalOrderOptions source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        DnsCanonicalOrderOptions result = new DnsCanonicalOrderOptions();
        result.LowercaseNames = source.LowercaseNames;
        result.DisableCompression = source.DisableCompression;
        result.SortRecords = source.SortRecords;
        result.NormalizeRecordTtl = source.NormalizeRecordTtl;
        result.SuppressDuplicateRecords = source.SuppressDuplicateRecords;
        return result;
    }

    internal static DnsResourceRecordSet CopySetMetadata(DnsResourceRecordSet source)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = CopyName(source.Name, false);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        result.TimeToLive = source.TimeToLive;
        return result;
    }

    internal static DnsResourceRecord CopyRecord(
        DnsResourceRecord source,
        DnsName owner)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = CopyName(owner, false);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        result.TimeToLive = source.TimeToLive;
        result.Data = source.Data;
        result.UseRawData = source.UseRawData;
        result.RawData = CopyBytes(source.RawData);
        return result;
    }

    internal static DnsName CopyName(DnsName source, bool lowercase)
    {
        RawDnsNameResult raw = RawDnsName.ValidateName(source);
        if (!raw.Success || raw.Name == null)
        {
            throw new System.ArgumentException("A valid DNS name is required.");
        }

        DnsName result = raw.Name;
        if (lowercase)
        {
            int labelIndex = 0;
            while (labelIndex < result.Labels.Count)
            {
                int byteIndex = 0;
                while (byteIndex < result.Labels[labelIndex].Length)
                {
                    byte value = result.Labels[labelIndex][byteIndex];
                    if (value >= 65 && value <= 90)
                    {
                        result.Labels[labelIndex][byteIndex] = (byte)(value + 32);
                    }

                    byteIndex = byteIndex + 1;
                }

                labelIndex = labelIndex + 1;
            }
        }

        return result;
    }

    internal static bool NamesEqual(DnsName left, DnsName right)
    {
        DnsNameOperationResult result = DnsNameOperations.NamesEqual(left, right);
        return result.Success && result.BooleanValue;
    }

    internal static int CompareNames(DnsName left, DnsName right)
    {
        DnsNameOperationResult result = DnsNameOperations.CompareNames(left, right);
        if (!result.Success)
        {
            throw new System.ArgumentException("Valid DNS names are required.");
        }

        return result.Comparison;
    }

    internal static bool SetKeysEqual(
        DnsResourceRecordSet left,
        DnsResourceRecordSet right)
    {
        return left.RecordTypeValue == right.RecordTypeValue &&
            left.ClassValue == right.ClassValue && NamesEqual(left.Name, right.Name);
    }

    internal static bool ContainsName(List<DnsName> names, DnsName name)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (NamesEqual(names[index], name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsRecord(
        List<DnsResourceRecord> records,
        DnsResourceRecord record)
    {
        int index = 0;
        while (index < records.Count)
        {
            if (BytesEqual(records[index].RawData, record.RawData))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool BytesEqual(byte[] left, byte[] right)
    {
        return CompareBytes(left, right) == 0;
    }

    internal static int CompareBytes(byte[] left, byte[] right)
    {
        if (left == null || right == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        int length = left.Length;
        if (right.Length < length)
        {
            length = right.Length;
        }

        int index = 0;
        while (index < length)
        {
            if (left[index] != right[index])
            {
                return left[index] < right[index] ? -1 : 1;
            }

            index = index + 1;
        }

        if (left.Length == right.Length)
        {
            return 0;
        }

        return left.Length < right.Length ? -1 : 1;
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        byte[] result = new byte[source.Length];
        System.Array.Copy(source, 0, result, 0, source.Length);
        return result;
    }

    internal static int EffectiveValue(int rawValue, int enumValue)
    {
        if (rawValue != 0 || enumValue == 0)
        {
            return rawValue;
        }

        return enumValue;
    }
}
