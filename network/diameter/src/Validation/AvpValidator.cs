using System;
using System.Collections.Generic;

namespace diameter;

internal class AvpValidator
{
    internal DictionaryIndexes Indexes;
    internal List<DiameterAvpDefinition> Avps = new List<DiameterAvpDefinition>();
    internal int TypeCount;
    internal DiameterLimitConfiguration Limits;

    internal AvpValidator(
        DiameterDictionarySnapshot? snapshot,
        DiameterLimitConfiguration? limits)
    {
        Indexes = new DictionaryIndexes(snapshot);
        Limits = new DiameterLimitConfiguration();
        if (limits != null)
        {
            Limits = limits;
        }

        if (snapshot != null)
        {
            Avps = snapshot.Avps;
            TypeCount = snapshot.Types.Count;
        }
    }

    internal AvpValidationResult ValidateMessageAvps(DiameterMessage? message)
    {
        AvpValidationResult result = new AvpValidationResult();
        if (message == null)
        {
            AddFailure(result, "message-required", null, null, -1, 0U, 0U, 0);
            CompleteResult(result);
            return result;
        }

        if (message.Avps.Count > Limits.MaximumAvpCount)
        {
            AvpValidationFailure limitFailure = AddFailure(
                result,
                "message-avp-count-limit",
                null,
                null,
                Limits.MaximumAvpCount,
                0U,
                0U,
                0);
            limitFailure.ExpectedMaximum = Limits.MaximumAvpCount;
            limitFailure.ActualCount = message.Avps.Count;
        }

        int index = 0;
        while (index < message.Avps.Count)
        {
            ValidateAvpAtIndex(message.Avps[index], index, 0U, 0U, 0, result);
            index = index + 1;
        }

        CompleteResult(result);
        return result;
    }

    internal AvpValidationResult ValidateSingleAvp(DiameterAvp? avp)
    {
        AvpValidationResult result = new AvpValidationResult();
        ValidateAvpAtIndex(avp, 0, 0U, 0U, 0, result);
        CompleteResult(result);
        return result;
    }

    internal void ValidateAvpAtIndex(
        DiameterAvp? avp,
        int avpIndex,
        uint parentCode,
        uint parentVendorId,
        int depth,
        AvpValidationResult result)
    {
        if (avp == null)
        {
            AddFailure(
                result,
                "avp-required",
                null,
                null,
                avpIndex,
                parentCode,
                parentVendorId,
                depth);
            return;
        }

        ValidateVendorHeader(avp, avpIndex, parentCode, parentVendorId, depth, result);
        DiameterAvpDefinition? definition = Indexes.FindAvpByCode(avp.VendorId, avp.Code);
        if (definition == null)
        {
            if (FindAvpByCode(avp.Code) != null)
            {
                AddFailure(
                    result,
                    "avp-vendor-mismatch",
                    avp,
                    null,
                    avpIndex,
                    parentCode,
                    parentVendorId,
                    depth);
            }
            else if ((avp.Flags & DiameterAvpFlags.Mandatory) != DiameterAvpFlags.None)
            {
                AddFailure(
                    result,
                    "unsupported-mandatory-avp",
                    avp,
                    null,
                    avpIndex,
                    parentCode,
                    parentVendorId,
                    depth);
            }

            return;
        }

        ValidateFlagMasks(avp, definition, avpIndex, parentCode, parentVendorId, depth, result);
        if (avp.Data == null)
        {
            AddFailure(
                result,
                "avp-data-required",
                avp,
                definition,
                avpIndex,
                parentCode,
                parentVendorId,
                depth);
            return;
        }

        int expectedLength = FindExpectedLength(definition);
        if (expectedLength >= 0 && avp.Data.Length != expectedLength)
        {
            AvpValidationFailure failure = AddFailure(
                result,
                "invalid-avp-size",
                avp,
                definition,
                avpIndex,
                parentCode,
                parentVendorId,
                depth);
            failure.ExpectedLength = expectedLength;
            failure.ActualLength = avp.Data.Length;
            return;
        }

        string valueType = ResolveValueType(definition);
        if (string.IsNullOrEmpty(valueType))
        {
            AddFailure(
                result,
                "invalid-type-hierarchy",
                avp,
                definition,
                avpIndex,
                parentCode,
                parentVendorId,
                depth);
            return;
        }

        if (definition.BaseType == DiameterAvpBaseType.Grouped || valueType == "Grouped")
        {
            ValidateGroupedAvp(avp, definition, avpIndex, depth, result);
            return;
        }

        DiameterValueResult valueResult = DecodeValue(avp.Data, definition, valueType);
        if (valueResult.Status != DiameterCodecStatus.Success)
        {
            AvpValidationFailure failure = AddFailure(
                result,
                "invalid-derived-avp-value",
                avp,
                definition,
                avpIndex,
                parentCode,
                parentVendorId,
                depth);
            if (valueResult.Issues.Count > 0)
            {
                failure.ValueCode = valueResult.Issues[0].Code;
            }

            return;
        }

        if (valueType == "Enumerated")
        {
            ValidateEnumValue(
                avp,
                definition,
                valueResult.Integer32,
                avpIndex,
                parentCode,
                parentVendorId,
                depth,
                result);
        }
    }

    internal void ValidateVendorHeader(
        DiameterAvp avp,
        int avpIndex,
        uint parentCode,
        uint parentVendorId,
        int depth,
        AvpValidationResult result)
    {
        bool vendorFlag =
            (avp.Flags & DiameterAvpFlags.VendorSpecific) != DiameterAvpFlags.None;
        if (vendorFlag && avp.VendorId == 0U)
        {
            AddFailure(
                result,
                "vendor-id-required",
                avp,
                null,
                avpIndex,
                parentCode,
                parentVendorId,
                depth);
        }

        if (!vendorFlag && avp.VendorId != 0U)
        {
            AddFailure(
                result,
                "vendor-flag-required",
                avp,
                null,
                avpIndex,
                parentCode,
                parentVendorId,
                depth);
        }
    }

    internal void ValidateFlagMasks(
        DiameterAvp avp,
        DiameterAvpDefinition definition,
        int avpIndex,
        uint parentCode,
        uint parentVendorId,
        int depth,
        AvpValidationResult result)
    {
        DiameterAvpFlags missing = definition.RequiredFlags & ~avp.Flags;
        if (missing != DiameterAvpFlags.None)
        {
            AvpValidationFailure failure = AddFailure(
                result,
                "required-avp-flags-missing",
                avp,
                definition,
                avpIndex,
                parentCode,
                parentVendorId,
                depth);
            failure.ExpectedFlags = definition.RequiredFlags;
            failure.ActualFlags = avp.Flags;
        }

        DiameterAvpFlags prohibited = definition.ProhibitedFlags & avp.Flags;
        if (prohibited != DiameterAvpFlags.None)
        {
            AvpValidationFailure failure = AddFailure(
                result,
                "prohibited-avp-flags-present",
                avp,
                definition,
                avpIndex,
                parentCode,
                parentVendorId,
                depth);
            failure.ExpectedFlags = definition.ProhibitedFlags;
            failure.ActualFlags = avp.Flags;
        }
    }

    internal int FindExpectedLength(DiameterAvpDefinition definition)
    {
        if (definition.FixedDataLength >= 0)
        {
            return definition.FixedDataLength;
        }

        if (definition.BaseType == DiameterAvpBaseType.Integer32 ||
            definition.BaseType == DiameterAvpBaseType.Unsigned32 ||
            definition.BaseType == DiameterAvpBaseType.Float32)
        {
            return 4;
        }

        if (definition.BaseType == DiameterAvpBaseType.Integer64 ||
            definition.BaseType == DiameterAvpBaseType.Unsigned64 ||
            definition.BaseType == DiameterAvpBaseType.Float64)
        {
            return 8;
        }

        return -1;
    }

    internal string ResolveValueType(DiameterAvpDefinition definition)
    {
        string name = definition.TypeName;
        uint vendorId = definition.VendorId;
        int remaining = TypeCount + 1;
        while (remaining > 0)
        {
            if (IsKnownValueType(name))
            {
                return name;
            }

            DiameterTypeDefinition? type = Indexes.FindTypeByName(vendorId, name);
            if (type == null && vendorId != DiameterVendorIds.Ietf)
            {
                type = Indexes.FindTypeByName(DiameterVendorIds.Ietf, name);
            }

            if (type == null || string.IsNullOrWhiteSpace(type.ParentTypeName))
            {
                return name;
            }

            name = type.ParentTypeName;
            vendorId = type.ParentTypeVendorId;
            remaining = remaining - 1;
        }

        return string.Empty;
    }

    internal bool IsKnownValueType(string name)
    {
        return name == "OctetString" || name == "Integer32" || name == "Integer64" ||
            name == "Unsigned32" || name == "Unsigned64" || name == "Float32" ||
            name == "Float64" || name == "Grouped" || name == "Address" ||
            name == "Time" || name == "UTF8String" || name == "DiameterIdentity" ||
            name == "DiameterURI" || name == "Enumerated" || name == "IPFilterRule" ||
            name == "QoSFilterRule";
    }

    internal DiameterValueResult DecodeValue(
        byte[] data,
        DiameterAvpDefinition definition,
        string valueType)
    {
        if (valueType == "Address")
        {
            return ValueCodecs.DecodeAddress(data);
        }

        if (valueType == "Time")
        {
            return ValueCodecs.DecodeTime(data, DateTimeOffset.UtcNow);
        }

        if (valueType == "UTF8String")
        {
            return ValueCodecs.DecodeUtf8String(data);
        }

        if (valueType == "DiameterIdentity")
        {
            return ValueCodecs.DecodeDiameterIdentity(data);
        }

        if (valueType == "DiameterURI")
        {
            return ValueCodecs.DecodeDiameterUri(data);
        }

        if (valueType == "IPFilterRule")
        {
            return ValueCodecs.DecodeIpFilterRule(data);
        }

        if (valueType == "QoSFilterRule")
        {
            return ValueCodecs.DecodeQosFilterRule(data);
        }

        if (valueType == "Enumerated")
        {
            return ValueCodecs.DecodeEnumerated(data);
        }

        if (definition.BaseType == DiameterAvpBaseType.Integer32)
        {
            return ValueCodecs.DecodeInteger32(data);
        }

        if (definition.BaseType == DiameterAvpBaseType.Integer64)
        {
            return ValueCodecs.DecodeInteger64(data);
        }

        if (definition.BaseType == DiameterAvpBaseType.Unsigned32)
        {
            return ValueCodecs.DecodeUnsigned32(data);
        }

        if (definition.BaseType == DiameterAvpBaseType.Unsigned64)
        {
            return ValueCodecs.DecodeUnsigned64(data);
        }

        if (definition.BaseType == DiameterAvpBaseType.Float32)
        {
            return ValueCodecs.DecodeFloat32(data);
        }

        if (definition.BaseType == DiameterAvpBaseType.Float64)
        {
            return ValueCodecs.DecodeFloat64(data);
        }

        return ValueCodecs.DecodeOctetString(data);
    }

    internal void ValidateEnumValue(
        DiameterAvp avp,
        DiameterAvpDefinition definition,
        int value,
        int avpIndex,
        uint parentCode,
        uint parentVendorId,
        int depth,
        AvpValidationResult result)
    {
        DiameterEnumValueDefinition? enumValue = Indexes.FindEnumValueByAvpValue(
            definition.VendorId,
            definition.Code,
            value);
        if (enumValue == null)
        {
            enumValue = Indexes.FindEnumValueByTypeValue(
                definition.VendorId,
                definition.TypeName,
                value);
        }

        if (enumValue == null && definition.VendorId != DiameterVendorIds.Ietf)
        {
            enumValue = Indexes.FindEnumValueByTypeValue(
                DiameterVendorIds.Ietf,
                definition.TypeName,
                value);
        }

        if (enumValue == null)
        {
            AvpValidationFailure failure = AddFailure(
                result,
                "unknown-enumerated-value",
                avp,
                definition,
                avpIndex,
                parentCode,
                parentVendorId,
                depth);
            failure.ActualCount = value;
        }
    }

    internal void ValidateGroupedAvp(
        DiameterAvp avp,
        DiameterAvpDefinition definition,
        int avpIndex,
        int depth,
        AvpValidationResult result)
    {
        if (depth >= Limits.MaximumGroupedDepth)
        {
            AddFailure(
                result,
                "grouped-depth-limit",
                avp,
                definition,
                avpIndex,
                definition.Code,
                definition.VendorId,
                depth);
            return;
        }

        if (avp.Data.Length > Limits.MaximumGroupedBytes)
        {
            AvpValidationFailure failure = AddFailure(
                result,
                "grouped-byte-limit",
                avp,
                definition,
                avpIndex,
                definition.Code,
                definition.VendorId,
                depth);
            failure.ExpectedMaximum = Limits.MaximumGroupedBytes;
            failure.ActualCount = avp.Data.Length;
            return;
        }

        DiameterValueResult decoded = AvpDecoder.DecodeAvpSequence(avp.Data);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            AvpValidationFailure failure = AddFailure(
                result,
                "invalid-grouped-value",
                avp,
                definition,
                avpIndex,
                definition.Code,
                definition.VendorId,
                depth);
            if (decoded.Issues.Count > 0)
            {
                failure.ValueCode = decoded.Issues[0].Code;
            }

            return;
        }

        if (decoded.Avps.Count > Limits.MaximumGroupedAvpCount)
        {
            AvpValidationFailure failure = AddFailure(
                result,
                "grouped-avp-count-limit",
                avp,
                definition,
                avpIndex,
                definition.Code,
                definition.VendorId,
                depth);
            failure.ExpectedMaximum = Limits.MaximumGroupedAvpCount;
            failure.ActualCount = decoded.Avps.Count;
            return;
        }

        ValidateGroupedRules(decoded.Avps, definition, depth + 1, result);
        int childIndex = 0;
        while (childIndex < decoded.Avps.Count)
        {
            ValidateAvpAtIndex(
                decoded.Avps[childIndex],
                childIndex,
                definition.Code,
                definition.VendorId,
                depth + 1,
                result);
            childIndex = childIndex + 1;
        }
    }

    internal void ValidateGroupedRules(
        List<DiameterAvp> children,
        DiameterAvpDefinition definition,
        int depth,
        AvpValidationResult result)
    {
        int ruleIndex = 0;
        while (ruleIndex < definition.GroupedRules.Count)
        {
            DiameterAvpRuleDefinition rule = definition.GroupedRules[ruleIndex];
            int count = CountOccurrences(children, rule);
            if (count < rule.MinimumCount)
            {
                AvpValidationFailure failure = AddFailure(
                    result,
                    "missing-required-grouped-avp",
                    null,
                    null,
                    -1,
                    definition.Code,
                    definition.VendorId,
                    depth);
                failure.AvpCode = rule.AvpCode;
                failure.AvpVendorId = rule.AvpVendorId;
                failure.ExpectedMinimum = rule.MinimumCount;
                failure.ExpectedMaximum = rule.MaximumCount;
                failure.ActualCount = count;
            }

            if (rule.MaximumCount >= 0 && count > rule.MaximumCount)
            {
                int excessIndex = FindOccurrenceIndex(children, rule, rule.MaximumCount);
                AvpValidationFailure failure = AddFailure(
                    result,
                    "too-many-grouped-avp-occurrences",
                    children[excessIndex],
                    null,
                    excessIndex,
                    definition.Code,
                    definition.VendorId,
                    depth);
                failure.ExpectedMinimum = rule.MinimumCount;
                failure.ExpectedMaximum = rule.MaximumCount;
                failure.ActualCount = count;
            }

            if (rule.FixedHeadPosition >= 0)
            {
                int actualIndex = FindOccurrenceIndex(children, rule, 0);
                if (actualIndex >= 0 && actualIndex != rule.FixedHeadPosition)
                {
                    AvpValidationFailure failure = AddFailure(
                        result,
                        "fixed-head-grouped-avp-misordered",
                        children[actualIndex],
                        null,
                        actualIndex,
                        definition.Code,
                        definition.VendorId,
                        depth);
                    failure.ExpectedPosition = rule.FixedHeadPosition;
                }
            }

            ruleIndex = ruleIndex + 1;
        }

        ValidateGroupedOrdering(children, definition, depth, result);
        ValidateGroupedUnlisted(children, definition, depth, result);
    }

    internal void ValidateGroupedOrdering(
        List<DiameterAvp> children,
        DiameterAvpDefinition definition,
        int depth,
        AvpValidationResult result)
    {
        int greatestGroup = -1;
        int index = 0;
        while (index < children.Count)
        {
            DiameterAvpRuleDefinition? rule = FindRule(
                definition.GroupedRules,
                children[index]);
            if (rule != null && rule.OrderingGroup >= 0)
            {
                if (rule.OrderingGroup < greatestGroup)
                {
                    AddFailure(
                        result,
                        "grouped-avp-ordering-violation",
                        children[index],
                        null,
                        index,
                        definition.Code,
                        definition.VendorId,
                        depth);
                }
                else
                {
                    greatestGroup = rule.OrderingGroup;
                }
            }

            index = index + 1;
        }
    }

    internal void ValidateGroupedUnlisted(
        List<DiameterAvp> children,
        DiameterAvpDefinition definition,
        int depth,
        AvpValidationResult result)
    {
        if (definition.AllowUnlistedAvps)
        {
            return;
        }

        int index = 0;
        while (index < children.Count)
        {
            if (FindRule(definition.GroupedRules, children[index]) == null)
            {
                AddFailure(
                    result,
                    "grouped-avp-not-allowed",
                    children[index],
                    null,
                    index,
                    definition.Code,
                    definition.VendorId,
                    depth);
            }

            index = index + 1;
        }
    }

    internal int CountOccurrences(
        List<DiameterAvp> avps,
        DiameterAvpRuleDefinition rule)
    {
        int count = 0;
        int index = 0;
        while (index < avps.Count)
        {
            if (AvpMatchesRule(avps[index], rule))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int FindOccurrenceIndex(
        List<DiameterAvp> avps,
        DiameterAvpRuleDefinition rule,
        int occurrence)
    {
        int matched = 0;
        int index = 0;
        while (index < avps.Count)
        {
            if (AvpMatchesRule(avps[index], rule))
            {
                if (matched == occurrence)
                {
                    return index;
                }

                matched = matched + 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal DiameterAvpRuleDefinition? FindRule(
        List<DiameterAvpRuleDefinition> rules,
        DiameterAvp avp)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (AvpMatchesRule(avp, rules[index]))
            {
                return rules[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool AvpMatchesRule(DiameterAvp avp, DiameterAvpRuleDefinition rule)
    {
        if (rule.AvpCode == 0U && rule.AvpVendorId == 0U && rule.AvpName == "AVP")
        {
            return true;
        }

        return avp.Code == rule.AvpCode && avp.VendorId == rule.AvpVendorId;
    }

    internal DiameterAvpDefinition? FindAvpByCode(uint code)
    {
        int index = 0;
        while (index < Avps.Count)
        {
            if (Avps[index].Code == code)
            {
                return Avps[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AvpValidationFailure AddFailure(
        AvpValidationResult result,
        string code,
        DiameterAvp? avp,
        DiameterAvpDefinition? definition,
        int avpIndex,
        uint parentCode,
        uint parentVendorId,
        int depth)
    {
        AvpValidationFailure failure = new AvpValidationFailure();
        failure.Code = code;
        failure.Avp = avp;
        failure.Definition = definition;
        failure.AvpIndex = avpIndex;
        failure.ParentAvpCode = parentCode;
        failure.ParentAvpVendorId = parentVendorId;
        failure.Depth = depth;
        if (avp != null)
        {
            failure.AvpCode = avp.Code;
            failure.AvpVendorId = avp.VendorId;
            failure.ActualFlags = avp.Flags;
        }

        result.Failures.Add(failure);
        return failure;
    }

    internal void CompleteResult(AvpValidationResult result)
    {
        result.Status = DiameterValidationStatus.Valid;
        if (result.Failures.Count > 0)
        {
            result.Status = DiameterValidationStatus.Invalid;
        }
    }
}
